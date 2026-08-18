using System;
using System.Collections.Generic;

namespace Packspire
{
    /// <summary>
    /// One action placed at an offset inside an enemy timeline pattern.
    /// The offset is relative to the beginning of the pattern, not the visible reel.
    /// </summary>
    public readonly struct RealtimeEnemyTimelineStep
    {
        public readonly double ExecuteOffset;
        public readonly string ActionId;
        public readonly RealtimeEnemyActionKind Kind;
        public readonly int Damage;
        public readonly double TelegraphLead;
        public readonly int HitCount;
        public readonly double HitSpacing;

        public RealtimeEnemyTimelineStep(
            double executeOffset,
            string actionId,
            int damage,
            double telegraphLead,
            int hitCount = 1,
            double hitSpacing = 0.15d,
            RealtimeEnemyActionKind kind = RealtimeEnemyActionKind.NormalAttack)
        {
            ExecuteOffset = Math.Max(0d, executeOffset);
            ActionId = actionId ?? string.Empty;
            Kind = kind;
            Damage = Math.Max(0, damage);
            TelegraphLead = Math.Max(0d, telegraphLead);
            HitCount = Math.Max(1, hitCount);
            HitSpacing = Math.Max(0.01d, hitSpacing);
        }
    }

    /// <summary>
    /// A phrase in an enemy's long-form action reel. Duration includes deliberate
    /// rests, so patterns can describe both dense bursts and quiet intervals.
    /// </summary>
    public sealed class RealtimeEnemyTimelinePattern
    {
        private readonly RealtimeEnemyTimelineStep[] steps;

        public string Id { get; }
        public double Duration { get; }
        public IReadOnlyList<RealtimeEnemyTimelineStep> Steps => steps;

        public RealtimeEnemyTimelinePattern(
            string id,
            double duration,
            params RealtimeEnemyTimelineStep[] steps)
        {
            if (duration <= 0d)
                throw new ArgumentOutOfRangeException(nameof(duration), "Pattern duration must be positive.");

            Id = id ?? string.Empty;
            Duration = duration;
            this.steps = steps == null
                ? Array.Empty<RealtimeEnemyTimelineStep>()
                : (RealtimeEnemyTimelineStep[])steps.Clone();
            Array.Sort(this.steps, CompareSteps);

            if (this.steps.Length > 0 && this.steps[this.steps.Length - 1].ExecuteOffset > duration)
                throw new ArgumentException("A pattern step cannot execute after the pattern duration.", nameof(steps));
        }

        private static int CompareSteps(RealtimeEnemyTimelineStep left, RealtimeEnemyTimelineStep right)
        {
            return left.ExecuteOffset.CompareTo(right.ExecuteOffset);
        }
    }

    public readonly struct RealtimeEnemyTimelineDecisionContext
    {
        public readonly double CurrentTime;
        public readonly double PatternStartsAt;
        public readonly int ScheduledPatternCount;
        public readonly string PreviousPatternId;

        public RealtimeEnemyTimelineDecisionContext(
            double currentTime,
            double patternStartsAt,
            int scheduledPatternCount,
            string previousPatternId)
        {
            CurrentTime = currentTime;
            PatternStartsAt = patternStartsAt;
            ScheduledPatternCount = Math.Max(0, scheduledPatternCount);
            PreviousPatternId = previousPatternId ?? string.Empty;
        }
    }

    /// <summary>
    /// Chooses the next complete phrase only when the committed plan needs extending.
    /// A future phase-, HP-, cooldown-, or utility-based selector can implement this
    /// interface without changing the timeline executor or reel presenter.
    /// </summary>
    public interface IRealtimeEnemyTimelineStrategy
    {
        RealtimeEnemyTimelinePattern SelectNext(in RealtimeEnemyTimelineDecisionContext context);
        void Reset();
    }

    public sealed class CyclicRealtimeEnemyTimelineStrategy : IRealtimeEnemyTimelineStrategy
    {
        private readonly RealtimeEnemyTimelinePattern[] patterns;
        private int nextPatternIndex;

        public CyclicRealtimeEnemyTimelineStrategy(params RealtimeEnemyTimelinePattern[] patterns)
        {
            if (patterns == null || patterns.Length == 0)
                throw new ArgumentException("At least one timeline pattern is required.", nameof(patterns));

            this.patterns = (RealtimeEnemyTimelinePattern[])patterns.Clone();
            for (int index = 0; index < this.patterns.Length; index++)
            {
                if (this.patterns[index] == null)
                    throw new ArgumentException("Timeline patterns cannot contain null entries.", nameof(patterns));
            }
        }

        public RealtimeEnemyTimelinePattern SelectNext(in RealtimeEnemyTimelineDecisionContext context)
        {
            RealtimeEnemyTimelinePattern pattern = patterns[nextPatternIndex];
            nextPatternIndex = (nextPatternIndex + 1) % patterns.Length;
            return pattern;
        }

        public void Reset()
        {
            nextPatternIndex = 0;
        }
    }

    /// <summary>
    /// Builds a rolling, long-range enemy plan independently from the UI window.
    /// The reel may therefore show zero, one, or many actions depending on the
    /// authored rhythm instead of maintaining a fixed visible action count.
    /// </summary>
    public sealed class RealtimeEnemyTimelinePlanner
    {
        private readonly RealtimeBattleController timeline;
        private readonly string actorId;
        private readonly IRealtimeEnemyTimelineStrategy strategy;
        private string previousPatternId = string.Empty;

        public double PlannedThrough { get; private set; }
        public int ScheduledPatternCount { get; private set; }

        public RealtimeEnemyTimelinePlanner(
            RealtimeBattleController timeline,
            string actorId,
            params RealtimeEnemyTimelinePattern[] patterns)
            : this(timeline, actorId, new CyclicRealtimeEnemyTimelineStrategy(patterns))
        {
        }

        public RealtimeEnemyTimelinePlanner(
            RealtimeBattleController timeline,
            string actorId,
            IRealtimeEnemyTimelineStrategy strategy)
        {
            this.timeline = timeline ?? throw new ArgumentNullException(nameof(timeline));
            this.actorId = actorId ?? string.Empty;
            this.strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
        }

        public void Reset(double startsAt = 0d)
        {
            ScheduledPatternCount = 0;
            previousPatternId = string.Empty;
            strategy.Reset();
            PlannedThrough = Math.Max(timeline.Time, startsAt);
        }

        public void EnsureScheduledThrough(double absoluteTime)
        {
            double target = Math.Max(timeline.Time, absoluteTime);
            while (PlannedThrough < target)
            {
                var context = new RealtimeEnemyTimelineDecisionContext(
                    timeline.Time,
                    PlannedThrough,
                    ScheduledPatternCount,
                    previousPatternId);
                RealtimeEnemyTimelinePattern pattern = strategy.SelectNext(in context);
                if (pattern == null)
                    throw new InvalidOperationException("Timeline strategy returned no pattern.");
                double patternStart = PlannedThrough;
                IReadOnlyList<RealtimeEnemyTimelineStep> steps = pattern.Steps;
                for (int index = 0; index < steps.Count; index++)
                {
                    RealtimeEnemyTimelineStep step = steps[index];
                    timeline.ScheduleEnemyActionAt(
                        actorId,
                        step.ActionId,
                        patternStart + step.ExecuteOffset,
                        step.Damage,
                        step.TelegraphLead,
                        step.HitCount,
                        step.HitSpacing,
                        step.Kind);
                }

                PlannedThrough += pattern.Duration;
                previousPatternId = pattern.Id;
                ScheduledPatternCount++;
            }
        }
    }
}
