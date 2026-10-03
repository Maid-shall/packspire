using System;
using System.Collections.Generic;
using System.Linq;

namespace Packspire
{
    [Serializable]
    public sealed class JourneyCombatMeasurementRecord
    {
        public string EncounterId;
        public string DisplayName;
        public int BattleNumber;
        public int SeedOrdinal;
        public bool Victory;
        public int StartingHp;
        public int EndingHp;
        public int DamageTaken;
        public int EnemySequences;
        public int ReactionOpportunities;
        public int ReactionFailures;
        public double DurationSeconds;

        public int NetHpLoss => Math.Max(0, StartingHp - EndingHp);
    }

    public readonly struct JourneyCombatMeasurementSummary
    {
        public readonly string EncounterId;
        public readonly string DisplayName;
        public readonly int BattleCount;
        public readonly int Victories;
        public readonly int TotalHpLoss;
        public readonly int TotalDamageTaken;
        public readonly int ReactionOpportunities;
        public readonly int ReactionFailures;
        public readonly double TotalDurationSeconds;

        public JourneyCombatMeasurementSummary(
            string encounterId,
            string displayName,
            int battleCount,
            int victories,
            int totalHpLoss,
            int totalDamageTaken,
            int reactionOpportunities,
            int reactionFailures,
            double totalDurationSeconds)
        {
            EncounterId = encounterId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            BattleCount = Math.Max(0, battleCount);
            Victories = Math.Max(0, victories);
            TotalHpLoss = Math.Max(0, totalHpLoss);
            TotalDamageTaken = Math.Max(0, totalDamageTaken);
            ReactionOpportunities = Math.Max(0, reactionOpportunities);
            ReactionFailures = Math.Max(0, reactionFailures);
            TotalDurationSeconds = Math.Max(0d, totalDurationSeconds);
        }

        public double AverageHpLoss => BattleCount == 0 ? 0d : (double)TotalHpLoss / BattleCount;
        public double AverageDamageTaken => BattleCount == 0 ? 0d : (double)TotalDamageTaken / BattleCount;
        public double AverageDurationSeconds => BattleCount == 0 ? 0d : TotalDurationSeconds / BattleCount;
        public double ReactionFailureRate => ReactionOpportunities == 0
            ? 0d
            : (double)ReactionFailures / ReactionOpportunities;
    }

    /// <summary>
    /// Collects developer-only playtest observations. It records outcomes but never
    /// changes combat values, timing windows, enemy selection, or battle resolution.
    /// </summary>
    public sealed class JourneyCombatMeasurementSession
    {
        private readonly List<JourneyCombatMeasurementRecord> records = new();
        private string activeEncounterId = string.Empty;
        private string activeDisplayName = string.Empty;
        private int activeBattleNumber;
        private int activeSeedOrdinal;
        private int startingHp;
        private int damageTaken;
        private int enemySequences;
        private int reactionOpportunities;
        private int reactionFailures;
        private double startedAt;

        public bool Active { get; private set; }
        public IReadOnlyList<JourneyCombatMeasurementRecord> Records => records;

        public int CompletedCount(string encounterId)
        {
            if (string.IsNullOrEmpty(encounterId)) return 0;
            return records.Count(record => string.Equals(
                record.EncounterId,
                encounterId,
                StringComparison.Ordinal));
        }

        public void Begin(
            string encounterId,
            string displayName,
            int battleNumber,
            int seedOrdinal,
            int initialHp,
            double timelineTime)
        {
            if (string.IsNullOrWhiteSpace(encounterId))
                throw new ArgumentException("Encounter id is required.", nameof(encounterId));
            if (Active)
                throw new InvalidOperationException("A combat measurement is already active.");

            activeEncounterId = encounterId;
            activeDisplayName = displayName ?? encounterId;
            activeBattleNumber = Math.Max(1, battleNumber);
            activeSeedOrdinal = Math.Max(0, seedOrdinal);
            startingHp = Math.Max(0, initialHp);
            damageTaken = 0;
            enemySequences = 0;
            reactionOpportunities = 0;
            reactionFailures = 0;
            startedAt = Math.Max(0d, timelineTime);
            Active = true;
        }

        public void RecordEnemyResolution(
            int playerDamage,
            bool sequenceEnded,
            bool reactionRequired,
            bool reactionSucceeded)
        {
            if (!Active) return;
            damageTaken += Math.Max(0, playerDamage);
            if (!sequenceEnded) return;

            enemySequences++;
            if (!reactionRequired) return;
            reactionOpportunities++;
            if (!reactionSucceeded) reactionFailures++;
        }

        public JourneyCombatMeasurementRecord Complete(
            bool victory,
            int endingHp,
            double timelineTime)
        {
            if (!Active)
                throw new InvalidOperationException("No combat measurement is active.");

            var record = new JourneyCombatMeasurementRecord
            {
                EncounterId = activeEncounterId,
                DisplayName = activeDisplayName,
                BattleNumber = activeBattleNumber,
                SeedOrdinal = activeSeedOrdinal,
                Victory = victory,
                StartingHp = startingHp,
                EndingHp = Math.Max(0, endingHp),
                DamageTaken = damageTaken,
                EnemySequences = enemySequences,
                ReactionOpportunities = reactionOpportunities,
                ReactionFailures = reactionFailures,
                DurationSeconds = Math.Max(0d, timelineTime - startedAt)
            };
            records.Add(record);
            ResetActive();
            return record;
        }

        public void Cancel()
        {
            if (!Active) return;
            ResetActive();
        }

        public void Clear()
        {
            Cancel();
            records.Clear();
        }

        public JourneyCombatMeasurementSummary Summarize(string encounterId)
        {
            JourneyCombatMeasurementRecord[] selected = records
                .Where(record => string.Equals(
                    record.EncounterId,
                    encounterId,
                    StringComparison.Ordinal))
                .ToArray();
            string displayName = selected.Length == 0
                ? string.Empty
                : selected[selected.Length - 1].DisplayName;
            return new JourneyCombatMeasurementSummary(
                encounterId,
                displayName,
                selected.Length,
                selected.Count(record => record.Victory),
                selected.Sum(record => record.NetHpLoss),
                selected.Sum(record => record.DamageTaken),
                selected.Sum(record => record.ReactionOpportunities),
                selected.Sum(record => record.ReactionFailures),
                selected.Sum(record => record.DurationSeconds));
        }

        private void ResetActive()
        {
            Active = false;
            activeEncounterId = string.Empty;
            activeDisplayName = string.Empty;
            activeBattleNumber = 0;
            activeSeedOrdinal = 0;
            startingHp = 0;
            damageTaken = 0;
            enemySequences = 0;
            reactionOpportunities = 0;
            reactionFailures = 0;
            startedAt = 0d;
        }
    }
}
