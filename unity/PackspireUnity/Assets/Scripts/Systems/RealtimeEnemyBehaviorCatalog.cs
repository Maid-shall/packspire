using System;
using System.Collections.Generic;
using UnityEngine;

namespace Packspire
{
    public enum RealtimeEnemyPatternRole
    {
        Standard,
        Opening,
        Pressure,
        Recovery,
        Signature,
        Transition
    }

    public enum RealtimeEnemySelectionMode
    {
        CyclicLegacy,
        RuleBased
    }

    [Serializable]
    public sealed class RealtimeEnemyActionContent
    {
        public string id = string.Empty;
        public string displayName = string.Empty;
        public RealtimeEnemyActionKind kind = RealtimeEnemyActionKind.NormalAttack;
        public RealtimeEnemyMotionLane motionLane = RealtimeEnemyMotionLane.Default;
        [Min(0)] public int value;
        [Min(0f)] public float telegraphLead = .7f;
        [Min(1)] public int hitCount = 1;
        [Min(.01f)] public float hitSpacing = .15f;

        public RealtimeEnemyTimelineStep Build(float executeOffset, float valueMultiplier)
        {
            string runtimeId = string.IsNullOrWhiteSpace(displayName)
                ? id
                : $"{id}:{displayName}";
            return new RealtimeEnemyTimelineStep(
                executeOffset,
                runtimeId,
                Mathf.Max(0, Mathf.RoundToInt(value * Mathf.Max(0f, valueMultiplier))),
                telegraphLead,
                hitCount,
                hitSpacing,
                kind,
                motionLane);
        }

        public RealtimeEnemyTimelineStep Bind(
            RealtimeEnemyTimelineStepContent timing,
            float valueMultiplier,
            float timingScale)
        {
            if (timing == null) throw new ArgumentNullException(nameof(timing));
            float scale = Mathf.Max(.1f, timingScale);
            string runtimeId = string.IsNullOrWhiteSpace(displayName)
                ? id
                : $"{id}:{displayName}";
            return new RealtimeEnemyTimelineStep(
                timing.executeOffset * scale,
                runtimeId,
                Mathf.Max(0, Mathf.RoundToInt(value * Mathf.Max(0f, valueMultiplier))),
                timing.telegraphLead * scale,
                timing.hitCount,
                timing.hitSpacing * scale,
                timing.kind,
                motionLane);
        }
    }

    [Serializable]
    public sealed class RealtimeEnemyPreviousPatternWeightContent
    {
        public string previousPatternId = string.Empty;
        [Range(0, 500)] public int weightPercent = 100;
    }

    [Serializable]
    public sealed class RealtimeEnemyPatternWeightContent
    {
        public string patternId = string.Empty;
        [Range(0, 500)] public int weightPercent = 100;
    }

    [Serializable]
    public sealed class RealtimeEnemyPhaseContent
    {
        public string id = string.Empty;
        [Range(0f, 1f)] public float enterAtOrBelowHealthRatio = 1f;
        public string transitionPatternId = string.Empty;
        public RealtimeEnemyPatternWeightContent[] patternWeights =
            Array.Empty<RealtimeEnemyPatternWeightContent>();

        public int WeightPercent(string patternId)
        {
            if (patternWeights == null) return 100;
            for (int index = 0; index < patternWeights.Length; index++)
            {
                RealtimeEnemyPatternWeightContent entry = patternWeights[index];
                if (entry != null && string.Equals(
                    entry.patternId,
                    patternId,
                    StringComparison.Ordinal)) return Mathf.Max(0, entry.weightPercent);
            }
            return 100;
        }
    }

    /// <summary>
    /// Shared long-form enemy phrases. Individual enemies reference five or six
    /// entries instead of copying the same authored rhythm into every profile.
    /// </summary>
    [CreateAssetMenu(
        fileName = "RealtimeEnemyBehaviorCatalog",
        menuName = "PACKSPIRE/Realtime Enemy Behavior Catalog")]
    public sealed class RealtimeEnemyBehaviorCatalog : ScriptableObject
    {
        [Tooltip("Legacy shared commands. New timing catalogs leave this empty and bind enemy commands from the profile.")]
        public RealtimeEnemyActionContent[] actions = Array.Empty<RealtimeEnemyActionContent>();
        public RealtimeEnemyTimelinePatternContent[] patterns =
            Array.Empty<RealtimeEnemyTimelinePatternContent>();

        public RealtimeEnemyActionContent FindAction(string actionId)
        {
            if (string.IsNullOrWhiteSpace(actionId) || actions == null) return null;
            for (int index = 0; index < actions.Length; index++)
            {
                RealtimeEnemyActionContent action = actions[index];
                if (action != null && string.Equals(action.id, actionId, StringComparison.Ordinal))
                    return action;
            }
            return null;
        }

        public RealtimeEnemyTimelinePatternContent FindPattern(string patternId)
        {
            if (string.IsNullOrWhiteSpace(patternId) || patterns == null) return null;
            for (int index = 0; index < patterns.Length; index++)
            {
                RealtimeEnemyTimelinePatternContent pattern = patterns[index];
                if (pattern != null && string.Equals(pattern.id, patternId, StringComparison.Ordinal))
                    return pattern;
            }
            return null;
        }

        public IReadOnlyList<RealtimeEnemyTimelinePatternContent> ResolvePatterns(
            IReadOnlyList<string> patternIds)
        {
            if (patternIds == null || patternIds.Count == 0)
                return Array.Empty<RealtimeEnemyTimelinePatternContent>();

            var result = new List<RealtimeEnemyTimelinePatternContent>(patternIds.Count);
            for (int index = 0; index < patternIds.Count; index++)
            {
                RealtimeEnemyTimelinePatternContent pattern = FindPattern(patternIds[index]);
                if (pattern == null)
                    throw new InvalidOperationException(
                        $"Realtime behavior catalog '{name}' has no pattern '{patternIds[index]}'.");
                result.Add(pattern);
            }
            return result;
        }
    }
}
