using System;
using System.Collections.Generic;
using UnityEngine;

namespace Packspire
{
    [Serializable]
    public sealed class RealtimeEnemyTimelineStepContent
    {
        [Min(0f)] public float executeOffset;
        public string actionId = string.Empty;
        public RealtimeEnemyActionKind kind = RealtimeEnemyActionKind.NormalAttack;
        [Min(0)] public int damage;
        [Min(0f)] public float telegraphLead = .7f;
        [Min(1)] public int hitCount = 1;
        [Min(.01f)] public float hitSpacing = .15f;

        public RealtimeEnemyTimelineStep Build(
            float damageMultiplier = 1f,
            RealtimeEnemyActionContent action = null)
        {
            if (action != null) return action.Build(executeOffset, damageMultiplier);
            return new RealtimeEnemyTimelineStep(
                executeOffset,
                actionId,
                Mathf.Max(0, Mathf.RoundToInt(damage * Mathf.Max(0f, damageMultiplier))),
                telegraphLead,
                hitCount,
                hitSpacing,
                kind);
        }
    }

    [Serializable]
    public sealed class RealtimeEnemyTimelinePatternContent
    {
        public string id = string.Empty;
        public RealtimeEnemyPatternRole role = RealtimeEnemyPatternRole.Standard;
        [Min(.01f)] public float duration = 8f;
        [Min(0)] public int baseWeight = 1;
        public string cooldownGroupId = string.Empty;
        [Min(0f)] public float cooldownSeconds;
        [Min(0)] public int maxConsecutive;
        [Min(0)] public int maximumUsesPerBattle;
        public RealtimeEnemyPreviousPatternWeightContent[] previousPatternWeights =
            Array.Empty<RealtimeEnemyPreviousPatternWeightContent>();
        public RealtimeEnemyTimelineStepContent[] steps = Array.Empty<RealtimeEnemyTimelineStepContent>();

        public RealtimeEnemyTimelinePattern Build(
            float damageMultiplier = 1f,
            RealtimeEnemyBehaviorCatalog catalog = null,
            float timingScale = 1f,
            Func<RealtimeEnemyTimelineStepContent, int, RealtimeEnemyActionContent>
                commandResolver = null)
        {
            float scale = Mathf.Max(.1f, timingScale);
            var runtimeSteps = new RealtimeEnemyTimelineStep[steps?.Length ?? 0];
            for (int index = 0; index < runtimeSteps.Length; index++)
            {
                if (steps[index] == null)
                    throw new InvalidOperationException($"Timeline pattern '{id}' contains a null step.");
                if (commandResolver != null)
                {
                    RealtimeEnemyActionContent command = commandResolver(steps[index], index);
                    if (command == null)
                        throw new InvalidOperationException(
                            $"Timeline pattern '{id}' cannot bind step {index} ({steps[index].kind}).");
                    runtimeSteps[index] = command.Bind(steps[index], damageMultiplier, scale);
                }
                else
                {
                    RealtimeEnemyTimelineStep built = steps[index].Build(
                        damageMultiplier,
                        catalog?.FindAction(steps[index].actionId));
                    runtimeSteps[index] = scale == 1f
                        ? built
                        : new RealtimeEnemyTimelineStep(
                            built.ExecuteOffset * scale,
                            built.ActionId,
                            built.Damage,
                            built.TelegraphLead * scale,
                            built.HitCount,
                            built.HitSpacing * scale,
                            built.Kind);
                }
            }

            return new RealtimeEnemyTimelinePattern(
                id,
                duration * scale,
                role,
                baseWeight,
                cooldownGroupId,
                cooldownSeconds * scale,
                maxConsecutive,
                maximumUsesPerBattle,
                previousPatternWeights,
                runtimeSteps);
        }
    }

    /// <summary>
    /// Designer-authored phrases reusable by multiple realtime enemies. The
    /// profile contains rhythm and rests; the encounter supplies the runtime
    /// actor identity and the planner commits complete phrases to the queue.
    /// </summary>
    [CreateAssetMenu(
        fileName = "RealtimeEnemyTimelineProfile",
        menuName = "PACKSPIRE/Realtime Enemy Timeline Profile")]
    public sealed class RealtimeEnemyTimelineProfile : ScriptableObject
    {
        [Tooltip("Legacy authoring label only. Runtime actor identity comes from the encounter enemy definition.")]
        public string actorId = string.Empty;

        [Header("Shared behavior loadout")]
        public RealtimeEnemySelectionMode selectionMode = RealtimeEnemySelectionMode.CyclicLegacy;
        public RealtimeEnemyBehaviorCatalog catalog;
        [Tooltip("Five or six shared long-form patterns normally equipped by this enemy.")]
        public string[] patternIds = Array.Empty<string>();
        [Tooltip("Scales the complete generic rhythm without changing its ordering.")]
        [Range(.25f, 4f)] public float timingScale = 1f;
        [Tooltip("Enemy-owned commands bound to generic timing slots by action kind.")]
        public RealtimeEnemyActionContent[] commands = Array.Empty<RealtimeEnemyActionContent>();
        public string openingPatternId = string.Empty;
        public string fallbackPatternId = string.Empty;
        public RealtimeEnemyPhaseContent[] phases = Array.Empty<RealtimeEnemyPhaseContent>();

        [Header("Legacy inline patterns")]
        public RealtimeEnemyTimelinePatternContent[] patterns =
            Array.Empty<RealtimeEnemyTimelinePatternContent>();

        public RealtimeEnemyTimelinePattern[] BuildPatterns(
            float damageMultiplier = 1f,
            int battleSeed = 0)
        {
            RealtimeEnemyTimelinePatternContent[] source = ResolvePatternContents();
            if (source.Length == 0)
                throw new InvalidOperationException($"Realtime timeline '{name}' has no patterns.");

            bool bindCommands = commands != null && commands.Length > 0;
            var result = new RealtimeEnemyTimelinePattern[source.Length];
            for (int index = 0; index < result.Length; index++)
            {
                if (source[index] == null)
                    throw new InvalidOperationException($"Realtime timeline '{name}' contains a null pattern.");
                RealtimeEnemyTimelinePatternContent pattern = source[index];
                result[index] = pattern.Build(
                    damageMultiplier,
                    catalog,
                    timingScale,
                    bindCommands
                        ? (step, stepIndex) => ResolveCommand(
                            pattern.id,
                            stepIndex,
                            step.kind,
                            battleSeed)
                        : null);
            }

            return result;
        }

        public IRealtimeEnemyTimelineStrategy BuildStrategy(
            int battleSeed,
            float damageMultiplier = 1f)
        {
            RealtimeEnemyTimelinePattern[] runtimePatterns =
                BuildPatterns(damageMultiplier, battleSeed);
            return selectionMode == RealtimeEnemySelectionMode.RuleBased
                ? new RuleBasedRealtimeEnemyTimelineStrategy(
                    battleSeed,
                    openingPatternId,
                    fallbackPatternId,
                    phases,
                    runtimePatterns)
                : new CyclicRealtimeEnemyTimelineStrategy(runtimePatterns);
        }

        public RealtimeEnemyTimelinePatternContent[] ResolvePatternContents()
        {
            if (catalog != null && patternIds != null && patternIds.Length > 0)
            {
                var resolved = catalog.ResolvePatterns(patternIds);
                var result = new RealtimeEnemyTimelinePatternContent[resolved.Count];
                for (int index = 0; index < result.Length; index++) result[index] = resolved[index];
                return result;
            }

            return patterns == null
                ? Array.Empty<RealtimeEnemyTimelinePatternContent>()
                : (RealtimeEnemyTimelinePatternContent[])patterns.Clone();
        }

        private RealtimeEnemyActionContent ResolveCommand(
            string patternId,
            int stepIndex,
            RealtimeEnemyActionKind requiredKind,
            int battleSeed)
        {
            var candidates = new List<RealtimeEnemyActionContent>();
            for (int index = 0; index < commands.Length; index++)
            {
                RealtimeEnemyActionContent command = commands[index];
                if (command != null && command.kind == requiredKind) candidates.Add(command);
            }

            if (candidates.Count == 0)
                throw new InvalidOperationException(
                    $"Realtime timeline '{name}' has no enemy command for {requiredKind}.");

            candidates.Sort((left, right) =>
                string.CompareOrdinal(left.id ?? string.Empty, right.id ?? string.Empty));
            uint hash = StableHash(
                $"{battleSeed}|{actorId}|{patternId}|{stepIndex}|{requiredKind}");
            return candidates[(int)(hash % (uint)candidates.Count)];
        }

        private static uint StableHash(string value)
        {
            unchecked
            {
                uint hash = 2166136261u;
                for (int index = 0; index < value.Length; index++)
                {
                    hash ^= value[index];
                    hash *= 16777619u;
                }
                return hash;
            }
        }
    }
}
