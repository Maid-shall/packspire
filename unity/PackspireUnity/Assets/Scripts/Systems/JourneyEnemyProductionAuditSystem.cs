using System;
using System.Collections.Generic;
using System.Linq;

namespace Packspire
{
    public sealed class JourneyEnemyProductionAuditReport
    {
        public readonly List<string> errors = new();
        public readonly List<string> warnings = new();
        public bool IsValid => errors.Count == 0;
    }

    /// <summary>
    /// Static production checks for the two independent axes: population class
    /// and content origin. It prevents recurring lineages from replacing a
    /// region's original commons or elites as the roster grows.
    /// </summary>
    public static class JourneyEnemyProductionAuditSystem
    {
        public static JourneyEnemyProductionAuditReport Audit(
            IReadOnlyList<JourneyBattleEncounterProfile> profiles,
            IReadOnlyList<string> requiredRegionIds)
        {
            var report = new JourneyEnemyProductionAuditReport();
            JourneyBattleEncounterProfile[] authored = (profiles ??
                    Array.Empty<JourneyBattleEncounterProfile>())
                .Where(profile => profile != null && profile.enemyDefinition != null)
                .ToArray();

            foreach (JourneyBattleEncounterProfile profile in authored)
            {
                JourneyEnemyDefinition enemy = profile.enemyDefinition;
                if (string.IsNullOrWhiteSpace(enemy.enemyId))
                    report.errors.Add($"Enemy definition '{enemy.name}' has no enemy id.");
                if (enemy.origin == JourneyEnemyOrigin.RecurringLineage && enemy.lineage == null)
                    report.errors.Add($"Recurring enemy '{enemy.enemyId}' has no lineage.");
                if (enemy.origin == JourneyEnemyOrigin.RegionOriginal && enemy.lineage != null)
                    report.errors.Add($"Region-original enemy '{enemy.enemyId}' unexpectedly uses a recurring lineage.");
                if (enemy.origin != JourneyEnemyOrigin.SpecialReturn &&
                    string.IsNullOrWhiteSpace(enemy.regionId))
                    report.warnings.Add($"Enemy '{enemy.enemyId}' has no production region.");

                JourneyEnemyBattlePresentationProfile presentation =
                    enemy.battlePresentation;
                if (presentation == null) continue;
                if (string.IsNullOrWhiteSpace(enemy.battleSheetResource))
                    report.errors.Add(
                        $"Enemy '{enemy.enemyId}' has a battle presentation but no battle sheet resource.");
                if (!presentation.HasCompletePoseSet)
                    report.errors.Add(
                        $"Enemy '{enemy.enemyId}' has an incomplete six-pose battle presentation.");
                if (!presentation.AnchorsAreNormalized)
                    report.errors.Add(
                        $"Enemy '{enemy.enemyId}' has battle effect anchors outside the sprite rectangle.");
            }

            foreach (IGrouping<JourneyEnemyLineageDefinition, JourneyBattleEncounterProfile> lineageGroup in
                     authored.Where(profile => profile.enemyDefinition.lineage != null)
                         .GroupBy(profile => profile.enemyDefinition.lineage))
            {
                string[] regions = lineageGroup
                    .Select(profile => profile.enemyDefinition.regionId)
                    .Where(region => !string.IsNullOrWhiteSpace(region))
                    .Distinct(StringComparer.Ordinal)
                    .ToArray();
                if (regions.Length < 2)
                    report.warnings.Add(
                        $"Lineage '{lineageGroup.Key.lineageId}' appears in only {regions.Length} region(s).");
            }

            foreach (string regionId in (requiredRegionIds ?? Array.Empty<string>())
                         .Where(region => !string.IsNullOrWhiteSpace(region))
                         .Distinct(StringComparer.Ordinal))
            {
                JourneyBattleEncounterProfile[] regional = authored
                    .Where(profile => string.Equals(
                        profile.enemyDefinition.regionId,
                        regionId,
                        StringComparison.Ordinal))
                    .ToArray();
                bool hasOriginalCommon = regional.Any(profile =>
                    profile.enemyDefinition.origin == JourneyEnemyOrigin.RegionOriginal &&
                    profile.PopulationClass == JourneyEnemyPopulationClass.Common);
                bool hasOriginalElite = regional.Any(profile =>
                    profile.enemyDefinition.origin == JourneyEnemyOrigin.RegionOriginal &&
                    profile.PopulationClass == JourneyEnemyPopulationClass.Elite);
                if (!hasOriginalCommon)
                    report.errors.Add($"Region '{regionId}' has no region-original common enemy.");
                if (!hasOriginalElite)
                    report.errors.Add($"Region '{regionId}' has no region-original elite enemy.");
            }

            return report;
        }
    }
}
