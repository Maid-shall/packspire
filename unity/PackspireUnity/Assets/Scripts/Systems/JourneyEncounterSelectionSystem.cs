using System;
using System.Collections.Generic;
using System.Linq;

namespace Packspire
{
    public sealed class JourneyEncounterSelectionReport
    {
        public readonly List<string> errors = new();
        public readonly List<string> warnings = new();
        public bool IsValid => errors.Count == 0;
    }

    /// <summary>
    /// Resolves authored encounter profiles for generated expedition nodes.
    /// Selection is deterministic and the result is persisted on the node, so a
    /// save/load or scene reload cannot silently replace the enemy.
    /// </summary>
    public static class JourneyEncounterSelectionSystem
    {
        public const string ResourceDirectory = "Data/Journey";

        public static JourneyBattleEncounterProfile SelectAndAssign(
            ExpeditionRoutePlan plan,
            ExpeditionRouteNodePlan node,
            string dungeonId,
            int elapsedDays)
        {
            return SelectAndAssign(
                plan,
                node,
                dungeonId,
                elapsedDays,
                PackspireResources.LoadAll<JourneyBattleEncounterProfile>(ResourceDirectory));
        }

        public static JourneyBattleEncounterProfile SelectAndAssign(
            ExpeditionRoutePlan plan,
            ExpeditionRouteNodePlan node,
            string dungeonId,
            int elapsedDays,
            IReadOnlyList<JourneyBattleEncounterProfile> profiles)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            if (node == null) throw new ArgumentNullException(nameof(node));
            if (node.kind != ExpeditionNodeKind.Battle && node.kind != ExpeditionNodeKind.Boss)
                throw new InvalidOperationException($"Node '{node.id}' is not a battle encounter.");

            JourneyBattleEncounterProfile persisted = ResolveByEncounterId(node.encounterId, profiles);
            if (persisted != null) return persisted;

            ExpeditionDayStage stage = ExpeditionRoutePlanSystem.DayStage(plan, elapsedDays);
            JourneyBattleEncounterProfile[] eligible = (profiles ?? Array.Empty<JourneyBattleEncounterProfile>())
                .Where(profile => profile != null && profile.IsEligible(
                    dungeonId,
                    node.floorIndex,
                    stage,
                    node.kind,
                    node.lane))
                .OrderBy(profile => profile.StableEncounterId, StringComparer.Ordinal)
                .ToArray();
            if (eligible.Length == 0)
                throw new InvalidOperationException(
                    $"No journey encounter is eligible for '{node.id}' " +
                    $"(floor {node.floorIndex + 1}, {stage}, {node.kind}, lane {node.lane}).");

            int totalWeight = eligible.Sum(profile => Math.Max(1, profile.selectionWeight));
            uint roll = StableHash($"{plan.seed}|{node.id}|{dungeonId}|{stage}") % (uint)totalWeight;
            JourneyBattleEncounterProfile selected = eligible[eligible.Length - 1];
            for (int index = 0; index < eligible.Length; index++)
            {
                int weight = Math.Max(1, eligible[index].selectionWeight);
                if (roll < weight)
                {
                    selected = eligible[index];
                    break;
                }
                roll -= (uint)weight;
            }

            node.encounterId = selected.StableEncounterId;
            return selected;
        }

        public static JourneyBattleEncounterProfile ResolveByEncounterId(string encounterId)
        {
            return ResolveByEncounterId(
                encounterId,
                PackspireResources.LoadAll<JourneyBattleEncounterProfile>(ResourceDirectory));
        }

        public static JourneyBattleEncounterProfile ResolveByEncounterId(
            string encounterId,
            IReadOnlyList<JourneyBattleEncounterProfile> profiles)
        {
            if (string.IsNullOrWhiteSpace(encounterId) || profiles == null) return null;
            for (int index = 0; index < profiles.Count; index++)
            {
                JourneyBattleEncounterProfile profile = profiles[index];
                if (profile != null && string.Equals(
                    profile.StableEncounterId,
                    encounterId,
                    StringComparison.Ordinal)) return profile;
            }
            return null;
        }

        public static JourneyEncounterSelectionReport AuditCoverage(
            ExpeditionRoutePlan plan,
            string dungeonId,
            IReadOnlyList<JourneyBattleEncounterProfile> profiles)
        {
            var report = new JourneyEncounterSelectionReport();
            if (plan == null)
            {
                report.errors.Add("Encounter audit has no expedition plan.");
                return report;
            }

            JourneyBattleEncounterProfile[] available = (profiles ?? Array.Empty<JourneyBattleEncounterProfile>())
                .Where(profile => profile != null)
                .ToArray();
            foreach (IGrouping<string, JourneyBattleEncounterProfile> duplicate in available
                         .GroupBy(profile => profile.StableEncounterId, StringComparer.Ordinal)
                         .Where(group => string.IsNullOrWhiteSpace(group.Key) || group.Count() > 1))
                report.errors.Add(string.IsNullOrWhiteSpace(duplicate.Key)
                    ? "A journey encounter profile has no stable encounter id."
                    : $"Journey encounter id '{duplicate.Key}' is duplicated.");

            ExpeditionDayStage[] stages = plan.fourthDayStageEnabled
                ? new[]
                {
                    ExpeditionDayStage.Quiet,
                    ExpeditionDayStage.Alert,
                    ExpeditionDayStage.Pursuit,
                    ExpeditionDayStage.Anomaly
                }
                : new[]
                {
                    ExpeditionDayStage.Quiet,
                    ExpeditionDayStage.Alert,
                    ExpeditionDayStage.Pursuit
                };
            foreach (ExpeditionRouteNodePlan node in plan.floors
                         .SelectMany(floor => floor.nodes)
                         .Where(node => node.kind == ExpeditionNodeKind.Battle || node.kind == ExpeditionNodeKind.Boss))
            foreach (ExpeditionDayStage stage in stages)
                if (!available.Any(profile => profile.IsEligible(
                        dungeonId,
                        node.floorIndex,
                        stage,
                        node.kind,
                        node.lane)))
                    report.errors.Add(
                        $"Node '{node.id}' has no eligible encounter for {stage}.");

            int authoredIdentities = available
                .Select(profile => profile.StableEncounterId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.Ordinal)
                .Count();
            if (authoredIdentities < 3)
                report.warnings.Add(
                    $"Only {authoredIdentities} journey encounter profile(s) are authored; product target is at least 3.");
            return report;
        }

        private static uint StableHash(string value)
        {
            const uint offset = 2166136261;
            const uint prime = 16777619;
            uint hash = offset;
            for (int index = 0; index < value.Length; index++)
            {
                hash ^= value[index];
                hash *= prime;
            }
            return hash;
        }
    }
}
