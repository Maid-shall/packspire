using System;
using System.Collections.Generic;

namespace Packspire
{
    public static class RealtimeCombatRules
    {
        public const double GuardGraceSeconds = 1d;
        public const double GuardDecayPerSecond = 3d;
        public const double CounterWindowSeconds = 1.6d;
        public const double CounterDamageMultiplier = .25d;
        public const int CounterDamagePercent = 25;
        public const double StatusSecondsPerDurationUnit = 6.4d;
        public const double StatusPulseSeconds = 6.4d;

        public static void ArmStatus(StatusState status, int durationUnits)
        {
            if (status == null) return;
            if (durationUnits > 0)
            {
                status.remainingSeconds = Math.Max(
                    status.remainingSeconds,
                    durationUnits * StatusSecondsPerDurationUnit);
            }
            if (IsPeriodicStatus(status.type) && status.nextPulseSeconds <= 0d)
                status.nextPulseSeconds = StatusPulseSeconds;
        }

        public static bool IsPeriodicStatus(string type) =>
            string.Equals(type, "burn", StringComparison.Ordinal) ||
            string.Equals(type, "poison", StringComparison.Ordinal) ||
            string.Equals(type, "regen", StringComparison.Ordinal);
    }

    public readonly struct RealtimeStatusTickResult
    {
        public readonly int PlayerDamage;
        public readonly int EnemyDamage;
        public readonly int PlayerHealing;
        public readonly int EnemyHealing;
        public readonly bool DisplayChanged;

        public RealtimeStatusTickResult(
            int playerDamage,
            int enemyDamage,
            int playerHealing,
            int enemyHealing,
            bool displayChanged)
        {
            PlayerDamage = playerDamage;
            EnemyDamage = enemyDamage;
            PlayerHealing = playerHealing;
            EnemyHealing = enemyHealing;
            DisplayChanged = displayChanged;
        }
    }

    /// <summary>
    /// UI-independent combat-clock rules shared by the seamless realtime battle.
    /// Enemy guard never grants a universal counter or guard-break reward.
    /// </summary>
    public sealed class RealtimeCombatTimingState
    {
        private sealed class GuardClock
        {
            public int Observed;
            public double GraceRemaining;
            public double DecayFraction;
        }

        private readonly GuardClock playerGuard = new();
        private readonly GuardClock enemyGuard = new();
        private int sequenceHpDamage;
        private bool sequenceThreatening;

        public double CounterRemaining { get; private set; }
        public bool CounterActive => CounterRemaining > 0d;

        public void Reset(int playerBlock, int enemyBlock)
        {
            ResetGuard(playerGuard, playerBlock);
            ResetGuard(enemyGuard, enemyBlock);
            sequenceHpDamage = 0;
            sequenceThreatening = false;
            CounterRemaining = 0d;
        }

        public void NotifyPlayerGuardChanged(int currentBlock) =>
            ObserveGuardGain(playerGuard, currentBlock);

        public void NotifyEnemyGuardChanged(int currentBlock) =>
            ObserveGuardGain(enemyGuard, currentBlock);

        public bool TickGuards(double deltaSeconds, ref int playerBlock, ref int enemyBlock)
        {
            if (deltaSeconds <= 0d) return false;
            bool playerChanged = TickGuard(playerGuard, deltaSeconds, ref playerBlock);
            bool enemyChanged = TickGuard(enemyGuard, deltaSeconds, ref enemyBlock);
            return playerChanged || enemyChanged;
        }

        public bool TickCounter(double deltaSeconds)
        {
            if (deltaSeconds <= 0d || CounterRemaining <= 0d) return false;
            int previousDisplay = (int)Math.Ceiling(CounterRemaining);
            CounterRemaining = Math.Max(0d, CounterRemaining - deltaSeconds);
            return previousDisplay != (int)Math.Ceiling(CounterRemaining);
        }

        public void BeginEnemyAttackSequence()
        {
            sequenceHpDamage = 0;
            sequenceThreatening = false;
        }

        public void RecordEnemyHit(int hpDamage, int authoredDamage)
        {
            sequenceHpDamage += Math.Max(0, hpDamage);
            if (authoredDamage > 0) sequenceThreatening = true;
        }

        public bool CompleteEnemyAttackSequence()
        {
            bool completeDefense = sequenceThreatening && sequenceHpDamage == 0;
            sequenceHpDamage = 0;
            sequenceThreatening = false;
            if (completeDefense)
                CounterRemaining = RealtimeCombatRules.CounterWindowSeconds;
            return completeDefense;
        }

        public int ConsumeCounterBonus(int baseCardDamage)
        {
            if (!CounterActive || baseCardDamage <= 0) return 0;
            CounterRemaining = 0d;
            return Math.Max(
                1,
                (int)Math.Ceiling(
                    baseCardDamage * RealtimeCombatRules.CounterDamageMultiplier));
        }

        public RealtimeStatusTickResult TickStatuses(
            double deltaSeconds,
            List<StatusState> playerStatuses,
            List<StatusState> enemyStatuses,
            ref int playerHp,
            int playerMaximumHp,
            ref int enemyHp,
            int enemyMaximumHp)
        {
            if (deltaSeconds <= 0d)
                return new RealtimeStatusTickResult(0, 0, 0, 0, false);

            int playerDamage = 0;
            int enemyDamage = 0;
            int playerHealing = 0;
            int enemyHealing = 0;
            bool displayChanged = TickStatusList(
                deltaSeconds,
                playerStatuses,
                ref playerHp,
                playerMaximumHp,
                ref playerDamage,
                ref playerHealing);
            displayChanged |= TickStatusList(
                deltaSeconds,
                enemyStatuses,
                ref enemyHp,
                enemyMaximumHp,
                ref enemyDamage,
                ref enemyHealing);
            return new RealtimeStatusTickResult(
                playerDamage,
                enemyDamage,
                playerHealing,
                enemyHealing,
                displayChanged);
        }

        private static void ResetGuard(GuardClock clock, int value)
        {
            clock.Observed = Math.Max(0, value);
            clock.GraceRemaining = value > 0
                ? RealtimeCombatRules.GuardGraceSeconds
                : 0d;
            clock.DecayFraction = 0d;
        }

        private static void ObserveGuardGain(GuardClock clock, int currentBlock)
        {
            int safeCurrent = Math.Max(0, currentBlock);
            if (safeCurrent > clock.Observed)
            {
                clock.GraceRemaining = RealtimeCombatRules.GuardGraceSeconds;
                clock.DecayFraction = 0d;
            }
            clock.Observed = safeCurrent;
            if (safeCurrent <= 0)
            {
                clock.GraceRemaining = 0d;
                clock.DecayFraction = 0d;
            }
        }

        private static bool TickGuard(GuardClock clock, double deltaSeconds, ref int value)
        {
            int safeValue = Math.Max(0, value);
            if (safeValue != clock.Observed) ObserveGuardGain(clock, safeValue);
            if (safeValue <= 0) return false;

            double decayDelta = deltaSeconds;
            if (clock.GraceRemaining > 0d)
            {
                double consumed = Math.Min(clock.GraceRemaining, decayDelta);
                clock.GraceRemaining -= consumed;
                decayDelta -= consumed;
            }
            if (decayDelta <= 0d) return false;

            clock.DecayFraction +=
                decayDelta * RealtimeCombatRules.GuardDecayPerSecond;
            int decay = Math.Min(safeValue, (int)Math.Floor(clock.DecayFraction));
            if (decay <= 0) return false;

            clock.DecayFraction -= decay;
            value = safeValue - decay;
            clock.Observed = value;
            if (value <= 0) ResetGuard(clock, 0);
            return true;
        }

        private static bool TickStatusList(
            double deltaSeconds,
            List<StatusState> statuses,
            ref int hp,
            int maximumHp,
            ref int damage,
            ref int healing)
        {
            if (statuses == null || statuses.Count == 0) return false;
            bool changed = false;
            for (int index = statuses.Count - 1; index >= 0; index--)
            {
                StatusState status = statuses[index];
                if (status == null)
                {
                    statuses.RemoveAt(index);
                    changed = true;
                    continue;
                }

                if (status.duration > 0 && status.remainingSeconds <= 0d)
                    RealtimeCombatRules.ArmStatus(status, status.duration);
                bool timed = status.duration > 0;
                double activeDelta = timed
                    ? Math.Min(deltaSeconds, status.remainingSeconds)
                    : deltaSeconds;
                int previousSecond = timed
                    ? (int)Math.Ceiling(status.remainingSeconds)
                    : 0;

                if (RealtimeCombatRules.IsPeriodicStatus(status.type))
                {
                    if (status.nextPulseSeconds <= 0d)
                        status.nextPulseSeconds = RealtimeCombatRules.StatusPulseSeconds;
                    status.nextPulseSeconds -= activeDelta;
                    while (status.nextPulseSeconds <= .000001d && status.amount > 0)
                    {
                        ApplyStatusPulse(status, ref hp, maximumHp, ref damage, ref healing);
                        status.nextPulseSeconds += RealtimeCombatRules.StatusPulseSeconds;
                        changed = true;
                    }
                }

                if (timed)
                {
                    status.remainingSeconds = Math.Max(
                        0d,
                        status.remainingSeconds - deltaSeconds);
                    if (previousSecond != (int)Math.Ceiling(status.remainingSeconds))
                        changed = true;
                }

                if (status.amount > 0 && (!timed || status.remainingSeconds > 0d))
                    continue;
                statuses.RemoveAt(index);
                changed = true;
            }
            return changed;
        }

        private static void ApplyStatusPulse(
            StatusState status,
            ref int hp,
            int maximumHp,
            ref int damage,
            ref int healing)
        {
            int amount = Math.Max(0, status.amount);
            if (string.Equals(status.type, "regen", StringComparison.Ordinal))
            {
                int gained = Math.Min(amount, Math.Max(0, maximumHp - hp));
                hp = Math.Min(maximumHp, hp + amount);
                healing += gained;
                return;
            }

            if (!string.Equals(status.type, "burn", StringComparison.Ordinal) &&
                !string.Equals(status.type, "poison", StringComparison.Ordinal))
                return;
            int applied = Math.Min(amount, Math.Max(0, hp));
            hp = Math.Max(0, hp - amount);
            damage += applied;
            if (string.Equals(status.type, "poison", StringComparison.Ordinal))
                status.amount = Math.Max(0, status.amount - 1);
        }
    }
}
