using System;
using System.Collections.Generic;

namespace Packspire
{
    public enum RealtimeBattlePhase
    {
        Stopped,
        Running,
        Paused,
        Finished
    }

    public enum RealtimeEnemyActionKind
    {
        NormalAttack,
        ComboAttack,
        Guard,
        JumpReaction,
        BraceReaction
    }

    public enum RealtimeEnemyMotionLane
    {
        Default,
        High,
        Low
    }

    public readonly struct RealtimeEnemyAction
    {
        public readonly string ActorId;
        public readonly string ActionId;
        public readonly RealtimeEnemyActionKind Kind;
        public readonly RealtimeEnemyMotionLane MotionLane;
        public readonly int Damage;
        public readonly int SequenceIndex;
        public readonly int SequenceCount;

        public RealtimeEnemyAction(
            string actorId,
            string actionId,
            RealtimeEnemyActionKind kind,
            int damage,
            int sequenceIndex,
            int sequenceCount,
            RealtimeEnemyMotionLane motionLane = RealtimeEnemyMotionLane.Default)
        {
            ActorId = actorId ?? string.Empty;
            ActionId = actionId ?? string.Empty;
            Kind = kind;
            MotionLane = motionLane;
            Damage = Math.Max(0, damage);
            SequenceIndex = sequenceIndex;
            SequenceCount = sequenceCount;
        }
    }

    public readonly struct RealtimeEnemyActionPreview
    {
        public readonly long Serial;
        public readonly string ActorId;
        public readonly string ActionId;
        public readonly RealtimeEnemyActionKind Kind;
        public readonly RealtimeEnemyMotionLane MotionLane;
        public readonly int Damage;
        public readonly int HitCount;
        public readonly double TimeUntil;
        public readonly double TimeUntilStart;
        public readonly bool Telegraphing;

        public RealtimeEnemyActionPreview(
            long serial,
            string actorId,
            string actionId,
            RealtimeEnemyActionKind kind,
            int damage,
            int hitCount,
            double timeUntil,
            double timeUntilStart,
            bool telegraphing,
            RealtimeEnemyMotionLane motionLane = RealtimeEnemyMotionLane.Default)
        {
            Serial = serial;
            ActorId = actorId ?? string.Empty;
            ActionId = actionId ?? string.Empty;
            Kind = kind;
            MotionLane = motionLane;
            Damage = Math.Max(0, damage);
            HitCount = Math.Max(1, hitCount);
            TimeUntil = Math.Max(0d, timeUntil);
            TimeUntilStart = timeUntilStart;
            Telegraphing = telegraphing;
        }
    }

    public readonly struct RealtimeSupplyPulsePreview
    {
        public readonly double TimeUntil;
        public readonly int EnergyDelta;
        public readonly int DrawCount;

        public RealtimeSupplyPulsePreview(double timeUntil, int energyDelta, int drawCount)
        {
            TimeUntil = Math.Max(0d, timeUntil);
            EnergyDelta = Math.Max(0, energyDelta);
            DrawCount = Math.Max(0, drawCount);
        }
    }

    /// <summary>
    /// UI-independent timeline for the next combat model. Cards may be used whenever
    /// energy is available; enemy telegraphs, multi-hit attacks, and energy recovery
    /// are emitted as deterministic timeline events.
    /// </summary>
    public sealed class RealtimeBattleController
    {
        private sealed class ScheduledAction
        {
            public string ActorId;
            public string ActionId;
            public RealtimeEnemyActionKind Kind;
            public RealtimeEnemyMotionLane MotionLane;
            public int Damage;
            public int RemainingHits;
            public int TotalHits;
            public int ResolvedHits;
            public double ExecuteAt;
            public double HitSpacing;
            public double TelegraphLead;
            public double TelegraphAt;
            public bool TelegraphSent;
            public long Serial;
        }

        private readonly List<ScheduledAction> actions = new List<ScheduledAction>();
        private long nextSerial;
        private double nextEnergyAt;

        public RealtimeBattlePhase Phase { get; private set; } = RealtimeBattlePhase.Stopped;
        public double Time { get; private set; }
        public int Energy { get; private set; }
        public int MaximumEnergy { get; private set; }
        public double EnergyInterval { get; private set; }
        public int EnergyPerSupplyPulse { get; private set; }
        public int DrawsPerSupplyPulse { get; private set; }
        public int SupplyPulseCount { get; private set; }
        public int AttackBonus { get; private set; }
        public int GuardBonus { get; private set; }
        public double AttackBonusRemaining { get; private set; }
        public double GuardBonusRemaining { get; private set; }
        public bool HasDelayableActions
        {
            get
            {
                foreach (ScheduledAction action in actions)
                {
                    if (action.RemainingHits > 0 && !action.TelegraphSent) return true;
                }
                return false;
            }
        }
        public double NextEnergyIn => Phase == RealtimeBattlePhase.Running || Phase == RealtimeBattlePhase.Paused
            ? Math.Max(0d, nextEnergyAt - Time)
            : 0d;

        public event Action<int, int> EnergyChanged;
        public event Action<RealtimeEnemyAction, double> EnemyTelegraphStarted;
        public event Action<RealtimeEnemyAction> EnemyActionResolved;

        public void Start(
            int initialEnergy,
            int maximumEnergy,
            double energyInterval,
            int energyPerSupplyPulse = 1,
            int drawsPerSupplyPulse = 1)
        {
            MaximumEnergy = Math.Max(1, maximumEnergy);
            Energy = Math.Clamp(initialEnergy, 0, MaximumEnergy);
            EnergyInterval = Math.Max(0.05d, energyInterval);
            EnergyPerSupplyPulse = Math.Max(0, energyPerSupplyPulse);
            DrawsPerSupplyPulse = Math.Max(0, drawsPerSupplyPulse);
            Time = 0d;
            nextEnergyAt = EnergyInterval;
            SupplyPulseCount = 0;
            AttackBonus = 0;
            GuardBonus = 0;
            AttackBonusRemaining = 0d;
            GuardBonusRemaining = 0d;
            nextSerial = 0;
            actions.Clear();
            Phase = RealtimeBattlePhase.Running;
            EnergyChanged?.Invoke(Energy, MaximumEnergy);
        }

        public void DelayFirstSupplyUntil(double absoluteTime)
        {
            if (SupplyPulseCount > 0 ||
                (Phase != RealtimeBattlePhase.Running &&
                 Phase != RealtimeBattlePhase.Paused))
                return;
            nextEnergyAt = Math.Max(
                nextEnergyAt,
                Math.Max(Time, absoluteTime));
        }

        public void SetPaused(bool paused)
        {
            if (Phase == RealtimeBattlePhase.Finished || Phase == RealtimeBattlePhase.Stopped) return;
            Phase = paused ? RealtimeBattlePhase.Paused : RealtimeBattlePhase.Running;
        }

        public void Finish()
        {
            Phase = RealtimeBattlePhase.Finished;
            actions.Clear();
            AttackBonus = 0;
            GuardBonus = 0;
            AttackBonusRemaining = 0d;
            GuardBonusRemaining = 0d;
        }

        public bool TrySpendEnergy(int amount)
        {
            amount = Math.Max(0, amount);
            if (Phase != RealtimeBattlePhase.Running || Energy < amount) return false;
            Energy -= amount;
            EnergyChanged?.Invoke(Energy, MaximumEnergy);
            return true;
        }

        public void SetEnergy(int value)
        {
            int clamped = Math.Clamp(value, 0, MaximumEnergy);
            if (Energy == clamped) return;
            Energy = clamped;
            EnergyChanged?.Invoke(Energy, MaximumEnergy);
        }

        public void AddEnergy(int amount)
        {
            if (amount <= 0) return;
            SetEnergy(Energy + amount);
        }

        public void ApplyAttackBoost(int amount, double duration)
        {
            if (amount <= 0 || duration <= 0d) return;
            AttackBonus = Math.Max(AttackBonus, amount);
            AttackBonusRemaining = Math.Max(AttackBonusRemaining, duration);
        }

        public void ApplyGuardBoost(int amount, double duration)
        {
            if (amount <= 0 || duration <= 0d) return;
            GuardBonus = Math.Max(GuardBonus, amount);
            GuardBonusRemaining = Math.Max(GuardBonusRemaining, duration);
        }

        /// <summary>
        /// Delays actions which have not begun telegraphing. An already committed
        /// attack keeps its visual timing so the reel and enemy animation cannot disagree.
        /// </summary>
        public int DelayUpcomingActions(double seconds)
        {
            if (seconds <= 0d) return 0;
            int delayed = 0;
            foreach (ScheduledAction action in actions)
            {
                if (action.RemainingHits <= 0 || action.TelegraphSent) continue;
                action.ExecuteAt += seconds;
                action.TelegraphAt += seconds;
                delayed++;
            }
            if (delayed > 0) actions.Sort(CompareActions);
            return delayed;
        }

        public void GetUpcomingActions(List<RealtimeEnemyActionPreview> buffer)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            buffer.Clear();
            foreach (ScheduledAction action in actions)
            {
                if (action.RemainingHits <= 0) continue;
                buffer.Add(new RealtimeEnemyActionPreview(
                    action.Serial,
                    action.ActorId,
                    action.ActionId,
                    action.Kind,
                    action.Damage,
                    action.RemainingHits,
                    action.ExecuteAt - Time,
                    action.TelegraphAt - Time,
                    action.TelegraphSent,
                    action.MotionLane));
            }
        }

        public void GetUpcomingSupplyPulses(
            List<RealtimeSupplyPulsePreview> buffer,
            double horizon)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            buffer.Clear();
            if (Phase != RealtimeBattlePhase.Running && Phase != RealtimeBattlePhase.Paused)
                return;

            double limit = Math.Max(0d, horizon);
            double timeUntil = NextEnergyIn;
            while (timeUntil <= limit + .0001d)
            {
                buffer.Add(new RealtimeSupplyPulsePreview(
                    timeUntil,
                    EnergyPerSupplyPulse,
                    DrawsPerSupplyPulse));
                timeUntil += EnergyInterval;
            }
        }

        public void ScheduleEnemyAction(
            string actorId,
            string actionId,
            double delay,
            int damage,
            double telegraphLead,
            int hitCount = 1,
            double hitSpacing = 0.15d,
            RealtimeEnemyActionKind kind = RealtimeEnemyActionKind.NormalAttack,
            RealtimeEnemyMotionLane motionLane = RealtimeEnemyMotionLane.Default)
        {
            ScheduleEnemyActionAt(
                actorId,
                actionId,
                Time + Math.Max(0d, delay),
                damage,
                telegraphLead,
                hitCount,
                hitSpacing,
                kind,
                motionLane);
        }

        public void ScheduleEnemyActionAt(
            string actorId,
            string actionId,
            double executeAt,
            int damage,
            double telegraphLead,
            int hitCount = 1,
            double hitSpacing = 0.15d,
            RealtimeEnemyActionKind kind = RealtimeEnemyActionKind.NormalAttack,
            RealtimeEnemyMotionLane motionLane = RealtimeEnemyMotionLane.Default)
        {
            if (Phase == RealtimeBattlePhase.Finished) return;
            var action = new ScheduledAction
            {
                ActorId = actorId ?? string.Empty,
                ActionId = actionId ?? string.Empty,
                Kind = kind,
                MotionLane = motionLane,
                Damage = Math.Max(0, damage),
                RemainingHits = Math.Max(1, hitCount),
                TotalHits = Math.Max(1, hitCount),
                ExecuteAt = Math.Max(Time, executeAt),
                HitSpacing = Math.Max(0.01d, hitSpacing),
                TelegraphLead = Math.Max(0d, telegraphLead),
                Serial = nextSerial++
            };
            action.TelegraphAt = action.ExecuteAt - action.TelegraphLead;
            actions.Add(action);
            actions.Sort(CompareActions);
        }

        public void Tick(double deltaTime)
        {
            if (Phase != RealtimeBattlePhase.Running || deltaTime <= 0d) return;
            Time += deltaTime;
            AdvanceTimedBoosts(deltaTime);

            while (Time >= nextEnergyAt)
            {
                nextEnergyAt += EnergyInterval;
                SupplyPulseCount++;
                if (Energy >= MaximumEnergy || EnergyPerSupplyPulse <= 0) continue;
                Energy = Math.Min(MaximumEnergy, Energy + EnergyPerSupplyPulse);
                EnergyChanged?.Invoke(Energy, MaximumEnergy);
            }

            for (int index = 0; index < actions.Count; index++)
            {
                ScheduledAction action = actions[index];
                if (!action.TelegraphSent && Time >= action.ExecuteAt - action.TelegraphLead)
                {
                    action.TelegraphSent = true;
                    EnemyTelegraphStarted?.Invoke(ToEvent(action), Math.Max(0d, action.ExecuteAt - Time));
                }

                while (action.RemainingHits > 0 && Time >= action.ExecuteAt)
                {
                    EnemyActionResolved?.Invoke(ToEvent(action));
                    action.RemainingHits--;
                    action.ResolvedHits++;
                    action.ExecuteAt += action.HitSpacing;
                }
            }

            actions.RemoveAll(static action => action.RemainingHits <= 0);
            actions.Sort(CompareActions);
        }

        private void AdvanceTimedBoosts(double deltaTime)
        {
            if (AttackBonusRemaining > 0d)
            {
                AttackBonusRemaining = Math.Max(0d, AttackBonusRemaining - deltaTime);
                if (AttackBonusRemaining <= 0d) AttackBonus = 0;
            }
            if (GuardBonusRemaining > 0d)
            {
                GuardBonusRemaining = Math.Max(0d, GuardBonusRemaining - deltaTime);
                if (GuardBonusRemaining <= 0d) GuardBonus = 0;
            }
        }

        private static RealtimeEnemyAction ToEvent(ScheduledAction action)
        {
            return new RealtimeEnemyAction(
                action.ActorId,
                action.ActionId,
                action.Kind,
                action.Damage,
                action.ResolvedHits,
                action.TotalHits,
                action.MotionLane);
        }

        private static int CompareActions(ScheduledAction left, ScheduledAction right)
        {
            int time = left.ExecuteAt.CompareTo(right.ExecuteAt);
            return time != 0 ? time : left.Serial.CompareTo(right.Serial);
        }
    }
}
