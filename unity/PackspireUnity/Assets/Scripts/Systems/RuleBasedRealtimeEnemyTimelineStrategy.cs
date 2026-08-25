using System;
using System.Collections.Generic;

namespace Packspire
{
    /// <summary>
    /// Deterministic selector for a five-or-six-pattern enemy loadout. HP only
    /// advances the authored phase; already planned patterns remain untouched.
    /// </summary>
    public sealed class RuleBasedRealtimeEnemyTimelineStrategy : IRealtimeEnemyTimelineStrategy
    {
        private readonly int battleSeed;
        private readonly string openingPatternId;
        private readonly string fallbackPatternId;
        private readonly RealtimeEnemyPhaseContent[] phases;
        private readonly RealtimeEnemyTimelinePattern[] patterns;
        private readonly Dictionary<string, RealtimeEnemyTimelinePattern> patternsById = new();
        private readonly Dictionary<string, int> useCounts = new();
        private readonly Dictionary<string, double> cooldownReadyAt = new();
        private readonly List<string> selectionHistory = new();

        private int currentPhaseIndex;
        private bool phaseInitialized;
        private bool pendingTransition;
        private string previousPatternId = string.Empty;
        private int consecutiveCount;

        public string CurrentPhaseId => phases.Length == 0
            ? string.Empty
            : phases[currentPhaseIndex]?.id ?? string.Empty;
        public int FallbackSelectionCount { get; private set; }
        public IReadOnlyList<string> SelectionHistory => selectionHistory;

        public RuleBasedRealtimeEnemyTimelineStrategy(
            int battleSeed,
            string openingPatternId,
            string fallbackPatternId,
            RealtimeEnemyPhaseContent[] phases,
            params RealtimeEnemyTimelinePattern[] patterns)
        {
            if (patterns == null || patterns.Length == 0)
                throw new ArgumentException("At least one timeline pattern is required.", nameof(patterns));

            this.battleSeed = battleSeed;
            this.openingPatternId = openingPatternId ?? string.Empty;
            this.fallbackPatternId = fallbackPatternId ?? string.Empty;
            this.phases = phases == null
                ? Array.Empty<RealtimeEnemyPhaseContent>()
                : (RealtimeEnemyPhaseContent[])phases.Clone();
            this.patterns = (RealtimeEnemyTimelinePattern[])patterns.Clone();
            Array.Sort(this.patterns, ComparePatternIds);
            for (int index = 0; index < this.patterns.Length; index++)
            {
                RealtimeEnemyTimelinePattern pattern = this.patterns[index] ??
                    throw new ArgumentException("Timeline patterns cannot contain null entries.", nameof(patterns));
                if (string.IsNullOrWhiteSpace(pattern.Id))
                    throw new ArgumentException("Rule-based timeline patterns require ids.", nameof(patterns));
                if (!patternsById.TryAdd(pattern.Id, pattern))
                    throw new ArgumentException($"Timeline pattern id '{pattern.Id}' is duplicated.", nameof(patterns));
            }

            if (string.IsNullOrWhiteSpace(this.fallbackPatternId) ||
                !patternsById.ContainsKey(this.fallbackPatternId))
                throw new ArgumentException("Rule-based timelines require a valid fallback pattern id.", nameof(fallbackPatternId));
            if (!string.IsNullOrWhiteSpace(this.openingPatternId) &&
                !patternsById.ContainsKey(this.openingPatternId))
                throw new ArgumentException($"Opening pattern '{this.openingPatternId}' is not in the loadout.", nameof(openingPatternId));
        }

        public RealtimeEnemyTimelinePattern SelectNext(
            in RealtimeEnemyTimelineDecisionContext context)
        {
            UpdatePhase(context.EnemyHealthRatio);

            if (context.ScheduledPatternCount == 0 &&
                !string.IsNullOrWhiteSpace(openingPatternId))
                return Record(patternsById[openingPatternId], in context);

            if (pendingTransition && phases.Length > 0)
            {
                string transitionId = phases[currentPhaseIndex]?.transitionPatternId;
                pendingTransition = false;
                if (!string.IsNullOrWhiteSpace(transitionId) &&
                    patternsById.TryGetValue(transitionId, out RealtimeEnemyTimelinePattern transition))
                    return Record(transition, in context);
            }

            var candidates = new List<Candidate>(patterns.Length);
            long totalWeight = 0;
            for (int index = 0; index < patterns.Length; index++)
            {
                RealtimeEnemyTimelinePattern pattern = patterns[index];
                if (!IsEligible(pattern, in context)) continue;
                long weight = EffectiveWeight(pattern);
                if (weight <= 0) continue;
                totalWeight += weight;
                candidates.Add(new Candidate(pattern, totalWeight));
            }

            if (candidates.Count == 0 || totalWeight <= 0)
            {
                FallbackSelectionCount++;
                return Record(patternsById[fallbackPatternId], in context);
            }

            uint hash = StableHash(
                $"{battleSeed}|{context.ScheduledPatternCount}|{CurrentPhaseId}|{previousPatternId}");
            long roll = hash % totalWeight;
            for (int index = 0; index < candidates.Count; index++)
                if (roll < candidates[index].CumulativeWeight)
                    return Record(candidates[index].Pattern, in context);
            return Record(candidates[candidates.Count - 1].Pattern, in context);
        }

        public void Reset()
        {
            useCounts.Clear();
            cooldownReadyAt.Clear();
            selectionHistory.Clear();
            currentPhaseIndex = 0;
            phaseInitialized = false;
            pendingTransition = false;
            previousPatternId = string.Empty;
            consecutiveCount = 0;
            FallbackSelectionCount = 0;
        }

        private bool IsEligible(
            RealtimeEnemyTimelinePattern pattern,
            in RealtimeEnemyTimelineDecisionContext context)
        {
            if (pattern.Role == RealtimeEnemyPatternRole.Transition) return false;
            if (pattern.Role == RealtimeEnemyPatternRole.Opening && UseCount(pattern.Id) > 0)
                return false;
            if (pattern.MaximumUsesPerBattle > 0 &&
                UseCount(pattern.Id) >= pattern.MaximumUsesPerBattle) return false;
            if (string.Equals(pattern.Id, previousPatternId, StringComparison.Ordinal) &&
                pattern.MaxConsecutive > 0 && consecutiveCount >= pattern.MaxConsecutive) return false;

            string cooldownKey = CooldownKey(pattern);
            if (cooldownReadyAt.TryGetValue(cooldownKey, out double readyAt) &&
                context.PatternStartsAt + .0001d < readyAt) return false;
            return true;
        }

        private long EffectiveWeight(RealtimeEnemyTimelinePattern pattern)
        {
            int phaseWeight = phases.Length == 0
                ? 100
                : phases[currentPhaseIndex]?.WeightPercent(pattern.Id) ?? 100;
            int previousWeight = pattern.PreviousPatternWeightPercent(previousPatternId);
            return (long)pattern.BaseWeight * phaseWeight * previousWeight;
        }

        private RealtimeEnemyTimelinePattern Record(
            RealtimeEnemyTimelinePattern pattern,
            in RealtimeEnemyTimelineDecisionContext context)
        {
            if (string.Equals(pattern.Id, previousPatternId, StringComparison.Ordinal))
                consecutiveCount++;
            else
                consecutiveCount = 1;
            previousPatternId = pattern.Id;
            useCounts[pattern.Id] = UseCount(pattern.Id) + 1;
            if (pattern.CooldownSeconds > 0d)
                cooldownReadyAt[CooldownKey(pattern)] =
                    context.PatternStartsAt + pattern.Duration + pattern.CooldownSeconds;
            selectionHistory.Add(pattern.Id);
            return pattern;
        }

        private void UpdatePhase(double healthRatio)
        {
            if (phases.Length == 0) return;
            int target = 0;
            for (int index = 0; index < phases.Length; index++)
            {
                RealtimeEnemyPhaseContent phase = phases[index];
                if (phase != null && healthRatio <= phase.enterAtOrBelowHealthRatio + .000001d)
                    target = index;
            }

            if (!phaseInitialized)
            {
                currentPhaseIndex = target;
                phaseInitialized = true;
                return;
            }
            if (target <= currentPhaseIndex) return;
            currentPhaseIndex = target;
            pendingTransition = !string.IsNullOrWhiteSpace(phases[currentPhaseIndex]?.transitionPatternId);
        }

        private int UseCount(string patternId) =>
            useCounts.TryGetValue(patternId, out int count) ? count : 0;

        private static string CooldownKey(RealtimeEnemyTimelinePattern pattern) =>
            string.IsNullOrWhiteSpace(pattern.CooldownGroupId)
                ? pattern.Id
                : pattern.CooldownGroupId;

        private static int ComparePatternIds(
            RealtimeEnemyTimelinePattern left,
            RealtimeEnemyTimelinePattern right) =>
            string.Compare(left?.Id, right?.Id, StringComparison.Ordinal);

        public static int CombineSeed(int runSeed, string encounterId, int battleOrdinal)
        {
            uint hash = StableHash($"{runSeed}|{encounterId}|{battleOrdinal}");
            return unchecked((int)hash);
        }

        private static uint StableHash(string value)
        {
            uint hash = 2166136261u;
            string safe = value ?? string.Empty;
            for (int index = 0; index < safe.Length; index++)
            {
                hash ^= safe[index];
                hash *= 16777619u;
            }
            return hash;
        }

        private readonly struct Candidate
        {
            public readonly RealtimeEnemyTimelinePattern Pattern;
            public readonly long CumulativeWeight;

            public Candidate(RealtimeEnemyTimelinePattern pattern, long cumulativeWeight)
            {
                Pattern = pattern;
                CumulativeWeight = cumulativeWeight;
            }
        }
    }
}
