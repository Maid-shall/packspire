using System;
using System.Collections.Generic;

namespace Packspire
{
    public sealed class RealtimeEnemyBehaviorAuditReport
    {
        public readonly List<string> errors = new();
        public readonly List<string> warnings = new();
        public int catalogPatternCount;
        public int equippedPatternCount;
        public int commandCount;
        public bool IsValid => errors.Count == 0;
    }

    public static class RealtimeEnemyBehaviorAudit
    {
        public static RealtimeEnemyBehaviorAuditReport Analyze(
            RealtimeEnemyTimelineProfile profile)
        {
            var report = new RealtimeEnemyBehaviorAuditReport();
            if (profile == null)
            {
                report.errors.Add("Realtime behavior audit has no profile.");
                return report;
            }

            if (profile.catalog != null)
                AuditCatalog(profile.catalog, report);

            RealtimeEnemyTimelinePatternContent[] patterns;
            try
            {
                patterns = profile.ResolvePatternContents();
            }
            catch (Exception exception)
            {
                report.errors.Add(exception.Message);
                return report;
            }

            report.equippedPatternCount = patterns.Length;
            if (profile.timingScale <= 0f)
                report.errors.Add($"Profile '{profile.name}' has a non-positive timing scale.");
            if (profile.selectionMode == RealtimeEnemySelectionMode.RuleBased &&
                (patterns.Length < 5 || patterns.Length > 6))
                report.warnings.Add(
                    $"Rule-based profile '{profile.name}' equips {patterns.Length} patterns; the normal target is 5-6.");

            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < patterns.Length; index++)
            {
                RealtimeEnemyTimelinePatternContent pattern = patterns[index];
                if (pattern == null)
                {
                    report.errors.Add($"Profile '{profile.name}' contains a null pattern.");
                    continue;
                }
                if (string.IsNullOrWhiteSpace(pattern.id))
                    report.errors.Add($"Profile '{profile.name}' contains a pattern without an id.");
                else if (!ids.Add(pattern.id))
                    report.errors.Add($"Profile '{profile.name}' equips pattern '{pattern.id}' more than once.");
                if (pattern.duration <= 0f)
                    report.errors.Add($"Pattern '{pattern.id}' has a non-positive duration.");
                if (pattern.steps == null) continue;
                for (int stepIndex = 0; stepIndex < pattern.steps.Length; stepIndex++)
                {
                    RealtimeEnemyTimelineStepContent step = pattern.steps[stepIndex];
                    if (step == null)
                    {
                        report.errors.Add($"Pattern '{pattern.id}' contains a null step.");
                        continue;
                    }
                    if (step.executeOffset > pattern.duration)
                        report.errors.Add($"Pattern '{pattern.id}' executes '{step.actionId}' after its duration.");
                }
            }

            AuditEnemyCommands(profile, patterns, report);

            if (profile.selectionMode != RealtimeEnemySelectionMode.RuleBased) return report;
            if (string.IsNullOrWhiteSpace(profile.fallbackPatternId) ||
                !ids.Contains(profile.fallbackPatternId))
                report.errors.Add($"Rule-based profile '{profile.name}' has no valid fallback pattern.");
            if (!string.IsNullOrWhiteSpace(profile.openingPatternId) &&
                !ids.Contains(profile.openingPatternId))
                report.errors.Add(
                    $"Rule-based profile '{profile.name}' opening '{profile.openingPatternId}' is not equipped.");

            float previousThreshold = 1.0001f;
            RealtimeEnemyPhaseContent[] phases = profile.phases ?? Array.Empty<RealtimeEnemyPhaseContent>();
            for (int index = 0; index < phases.Length; index++)
            {
                RealtimeEnemyPhaseContent phase = phases[index];
                if (phase == null)
                {
                    report.errors.Add($"Profile '{profile.name}' contains a null phase.");
                    continue;
                }
                if (string.IsNullOrWhiteSpace(phase.id))
                    report.errors.Add($"Profile '{profile.name}' contains a phase without an id.");
                if (phase.enterAtOrBelowHealthRatio >= previousThreshold)
                    report.errors.Add($"Profile '{profile.name}' phase thresholds must descend in authored order.");
                previousThreshold = phase.enterAtOrBelowHealthRatio;
                if (!string.IsNullOrWhiteSpace(phase.transitionPatternId) &&
                    !ids.Contains(phase.transitionPatternId))
                    report.errors.Add(
                        $"Phase '{phase.id}' transition '{phase.transitionPatternId}' is not equipped.");
                var weightedIds = new HashSet<string>(StringComparer.Ordinal);
                RealtimeEnemyPatternWeightContent[] weights = phase.patternWeights ??
                    Array.Empty<RealtimeEnemyPatternWeightContent>();
                for (int weightIndex = 0; weightIndex < weights.Length; weightIndex++)
                {
                    RealtimeEnemyPatternWeightContent weight = weights[weightIndex];
                    if (weight == null)
                    {
                        report.errors.Add($"Phase '{phase.id}' contains a null pattern weight.");
                        continue;
                    }
                    if (!ids.Contains(weight.patternId))
                        report.errors.Add(
                            $"Phase '{phase.id}' weights unequipped pattern '{weight.patternId}'.");
                    if (!weightedIds.Add(weight.patternId))
                        report.errors.Add(
                            $"Phase '{phase.id}' weights pattern '{weight.patternId}' more than once.");
                }
                bool hasCandidate = false;
                foreach (string id in ids)
                    if (phase.WeightPercent(id) > 0) hasCandidate = true;
                if (!hasCandidate)
                    report.errors.Add($"Phase '{phase.id}' disables every equipped pattern.");
            }
            return report;
        }

        private static void AuditCatalog(
            RealtimeEnemyBehaviorCatalog catalog,
            RealtimeEnemyBehaviorAuditReport report)
        {
            RealtimeEnemyTimelinePatternContent[] patterns = catalog.patterns ??
                Array.Empty<RealtimeEnemyTimelinePatternContent>();
            report.catalogPatternCount = patterns.Length;
            var patternIds = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < patterns.Length; index++)
            {
                RealtimeEnemyTimelinePatternContent pattern = patterns[index];
                if (pattern == null)
                {
                    report.errors.Add($"Catalog '{catalog.name}' contains a null pattern.");
                    continue;
                }
                if (string.IsNullOrWhiteSpace(pattern.id) || !patternIds.Add(pattern.id))
                    report.errors.Add($"Catalog '{catalog.name}' has a missing or duplicate pattern id '{pattern.id}'.");
            }

            var actionIds = new HashSet<string>(StringComparer.Ordinal);
            RealtimeEnemyActionContent[] actions = catalog.actions ?? Array.Empty<RealtimeEnemyActionContent>();
            for (int index = 0; index < actions.Length; index++)
            {
                RealtimeEnemyActionContent action = actions[index];
                if (action == null)
                {
                    report.errors.Add($"Catalog '{catalog.name}' contains a null action.");
                    continue;
                }
                if (string.IsNullOrWhiteSpace(action.id) || !actionIds.Add(action.id))
                    report.errors.Add($"Catalog '{catalog.name}' has a missing or duplicate action id '{action.id}'.");
            }
        }

        private static void AuditEnemyCommands(
            RealtimeEnemyTimelineProfile profile,
            RealtimeEnemyTimelinePatternContent[] patterns,
            RealtimeEnemyBehaviorAuditReport report)
        {
            RealtimeEnemyActionContent[] commands =
                profile.commands ?? Array.Empty<RealtimeEnemyActionContent>();
            report.commandCount = commands.Length;
            if (commands.Length == 0) return;

            var ids = new HashSet<string>(StringComparer.Ordinal);
            var kinds = new HashSet<RealtimeEnemyActionKind>();
            for (int index = 0; index < commands.Length; index++)
            {
                RealtimeEnemyActionContent command = commands[index];
                if (command == null)
                {
                    report.errors.Add($"Profile '{profile.name}' contains a null enemy command.");
                    continue;
                }
                if (string.IsNullOrWhiteSpace(command.id) || !ids.Add(command.id))
                    report.errors.Add(
                        $"Profile '{profile.name}' has a missing or duplicate command id '{command.id}'.");
                kinds.Add(command.kind);
            }

            bool warnedConcreteTiming = false;
            for (int patternIndex = 0; patternIndex < patterns.Length; patternIndex++)
            {
                RealtimeEnemyTimelinePatternContent pattern = patterns[patternIndex];
                if (pattern?.steps == null) continue;
                for (int stepIndex = 0; stepIndex < pattern.steps.Length; stepIndex++)
                {
                    RealtimeEnemyTimelineStepContent step = pattern.steps[stepIndex];
                    if (step == null) continue;
                    if (!kinds.Contains(step.kind))
                        report.errors.Add(
                            $"Profile '{profile.name}' has no command for {step.kind} required by '{pattern.id}'.");
                    if (!warnedConcreteTiming &&
                        (!string.IsNullOrWhiteSpace(step.actionId) || step.damage != 0))
                    {
                        report.warnings.Add(
                            $"Profile '{profile.name}' binds commands to a timing catalog that still contains concrete action data.");
                        warnedConcreteTiming = true;
                    }
                }
            }
        }
    }
}
