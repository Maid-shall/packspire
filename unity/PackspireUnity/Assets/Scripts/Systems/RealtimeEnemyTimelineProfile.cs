using System;
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

        public RealtimeEnemyTimelineStep Build()
        {
            return new RealtimeEnemyTimelineStep(
                executeOffset,
                actionId,
                damage,
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
        [Min(.01f)] public float duration = 8f;
        public RealtimeEnemyTimelineStepContent[] steps = Array.Empty<RealtimeEnemyTimelineStepContent>();

        public RealtimeEnemyTimelinePattern Build()
        {
            var runtimeSteps = new RealtimeEnemyTimelineStep[steps?.Length ?? 0];
            for (int index = 0; index < runtimeSteps.Length; index++)
            {
                if (steps[index] == null)
                    throw new InvalidOperationException($"Timeline pattern '{id}' contains a null step.");
                runtimeSteps[index] = steps[index].Build();
            }

            return new RealtimeEnemyTimelinePattern(id, duration, runtimeSteps);
        }
    }

    /// <summary>
    /// Designer-authored phrases for one realtime enemy. The profile contains
    /// rhythm and rests; the planner decides when another complete phrase must
    /// be committed to the deterministic battle event queue.
    /// </summary>
    [CreateAssetMenu(
        fileName = "RealtimeEnemyTimelineProfile",
        menuName = "PACKSPIRE/Realtime Enemy Timeline Profile")]
    public sealed class RealtimeEnemyTimelineProfile : ScriptableObject
    {
        public string actorId = string.Empty;
        public RealtimeEnemyTimelinePatternContent[] patterns =
            Array.Empty<RealtimeEnemyTimelinePatternContent>();

        public RealtimeEnemyTimelinePattern[] BuildPatterns()
        {
            if (patterns == null || patterns.Length == 0)
                throw new InvalidOperationException($"Realtime timeline '{name}' has no patterns.");

            var result = new RealtimeEnemyTimelinePattern[patterns.Length];
            for (int index = 0; index < result.Length; index++)
            {
                if (patterns[index] == null)
                    throw new InvalidOperationException($"Realtime timeline '{name}' contains a null pattern.");
                result[index] = patterns[index].Build();
            }

            return result;
        }
    }
}
