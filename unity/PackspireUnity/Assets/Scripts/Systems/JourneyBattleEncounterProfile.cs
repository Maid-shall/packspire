using System;
using UnityEngine;

namespace Packspire
{
    /// <summary>
    /// Authored identity, presentation and realtime rhythm for one seamless-journey
    /// encounter. The journey runtime consumes this profile without knowing which
    /// enemy or sprite sheet it represents.
    /// </summary>
    [CreateAssetMenu(
        fileName = "JourneyBattleEncounterProfile",
        menuName = "PACKSPIRE/Journey Battle Encounter Profile")]
    public sealed class JourneyBattleEncounterProfile : ScriptableObject
    {
        [Header("Encounter selection")]
        [Tooltip("Stable encounter identity stored in the expedition plan. Falls back to Enemy Id for old assets.")]
        public string encounterId = string.Empty;
        public string[] dungeonIds = Array.Empty<string>();
        [Min(1)] public int minimumFloor = 1;
        [Min(1)] public int maximumFloor = 3;
        public ExpeditionDayStage minimumDayStage = ExpeditionDayStage.Quiet;
        public ExpeditionDayStage maximumDayStage = ExpeditionDayStage.Anomaly;
        [Min(0)] public int minimumLane = 0;
        [Min(0)] public int maximumLane = 2;
        public bool allowNormalBattle = true;
        public bool allowBossBattle;
        [Min(1)] public int selectionWeight = 1;

        [Header("Composed enemy (preferred)")]
        [Tooltip("Identity, base statistics and art shared independently from behaviour.")]
        public JourneyEnemyDefinition enemyDefinition;
        [Tooltip("Reusable population/rank treatment. Empty keeps legacy values unchanged.")]
        public JourneyEnemyVariantDefinition enemyVariant;
        [Tooltip("Shared realtime behaviour. Falls back to the legacy Timeline field.")]
        public RealtimeEnemyTimelineProfile behaviorProfile;

        [Header("Legacy inline enemy (migration fallback)")]
        public string enemyId = string.Empty;
        public string displayName = string.Empty;
        [Min(1)] public int tier = 1;
        [Min(1)] public int maximumHealth = 1;
        public int[] legacyTurnDamages = Array.Empty<int>();

        [Header("Realtime timeline")]
        public RealtimeEnemyTimelineProfile timeline;

        [Header("Presentation")]
        public string fallbackSpriteResource = string.Empty;
        public string battleSheetResource = string.Empty;
        public string[] orderedBattlePoseNames = Array.Empty<string>();

        public string ResolvedEnemyId => enemyDefinition != null
            ? enemyDefinition.enemyId
            : enemyId;

        public string ResolvedDisplayName
        {
            get
            {
                string baseName = enemyDefinition != null
                    ? enemyDefinition.displayName
                    : displayName;
                return enemyVariant != null
                    ? enemyVariant.ApplyDisplayName(baseName)
                    : baseName;
            }
        }

        public RealtimeEnemyTimelineProfile ResolvedTimeline
        {
            get
            {
                if (behaviorProfile != null) return behaviorProfile;
                if (enemyDefinition != null)
                {
                    if (enemyDefinition.regionalBehaviorProfile != null)
                        return enemyDefinition.regionalBehaviorProfile;
                    if (enemyDefinition.lineage != null &&
                        enemyDefinition.lineage.baseBehaviorProfile != null)
                        return enemyDefinition.lineage.baseBehaviorProfile;
                }
                return timeline;
            }
        }

        public string ResolvedFallbackSpriteResource => enemyDefinition != null
            ? enemyDefinition.fallbackSpriteResource
            : fallbackSpriteResource;

        public string ResolvedBattleSheetResource => enemyDefinition != null
            ? enemyDefinition.battleSheetResource
            : battleSheetResource;

        public string[] ResolvedOrderedBattlePoseNames => enemyDefinition != null
            ? enemyDefinition.battlePresentation != null &&
              enemyDefinition.battlePresentation.HasCompletePoseSet
                ? enemyDefinition.battlePresentation.OrderedPoseNames
                : enemyDefinition.orderedBattlePoseNames
            : orderedBattlePoseNames;

        public JourneyEnemyBattlePresentationProfile ResolvedBattlePresentation =>
            enemyDefinition != null ? enemyDefinition.battlePresentation : null;

        public float ResolvedBattleScale => enemyDefinition != null &&
                                            enemyDefinition.battleScale >= .05f
            ? enemyDefinition.battleScale
            : .66f;

        public float ResolvedBattleTargetHeight => enemyDefinition != null
            ? Mathf.Max(0f, enemyDefinition.battleTargetHeight)
            : 0f;

        public JourneyEnemyPopulationClass PopulationClass => enemyVariant != null
            ? enemyVariant.populationClass
            : allowBossBattle && !allowNormalBattle
                ? JourneyEnemyPopulationClass.Boss
                : JourneyEnemyPopulationClass.Common;

        public BattleFormationScale ResolvedBattleFormation
        {
            get
            {
                if (allowBossBattle && !allowNormalBattle)
                    return BattleFormationScale.Boss;
                if (enemyVariant != null)
                {
                    if (enemyVariant.populationClass == JourneyEnemyPopulationClass.Boss)
                        return BattleFormationScale.Boss;
                    if (enemyVariant.populationClass == JourneyEnemyPopulationClass.Elite)
                        return BattleFormationScale.Large;
                }

                return enemyDefinition != null
                    ? enemyDefinition.battleFormation
                    : BattleFormationScale.Normal;
            }
        }

        public string StableEncounterId => string.IsNullOrWhiteSpace(encounterId)
            ? ResolvedEnemyId
            : encounterId;

        public bool IsEligible(
            string dungeonId,
            int floorIndex,
            ExpeditionDayStage dayStage,
            ExpeditionNodeKind nodeKind,
            int lane)
        {
            bool battleKindAllowed = nodeKind == ExpeditionNodeKind.Battle
                ? allowNormalBattle
                : nodeKind == ExpeditionNodeKind.Boss && allowBossBattle;
            if (!battleKindAllowed) return false;

            int floorNumber = floorIndex + 1;
            if (floorNumber < Mathf.Max(1, minimumFloor) ||
                floorNumber > Mathf.Max(minimumFloor, maximumFloor)) return false;
            if (dayStage < minimumDayStage || dayStage > maximumDayStage) return false;
            if (lane < minimumLane || lane > Mathf.Max(minimumLane, maximumLane)) return false;
            if (dungeonIds == null || dungeonIds.Length == 0) return true;

            for (int index = 0; index < dungeonIds.Length; index++)
                if (string.Equals(dungeonIds[index], dungeonId, StringComparison.Ordinal)) return true;
            return false;
        }

        public EnemyDef BuildEnemy()
        {
            string resolvedEnemyId = ResolvedEnemyId;
            string resolvedDisplayName = ResolvedDisplayName;
            RealtimeEnemyTimelineProfile resolvedTimeline = ResolvedTimeline;
            if (string.IsNullOrWhiteSpace(resolvedEnemyId))
                throw new InvalidOperationException($"Journey encounter '{name}' has no enemy id.");
            if (string.IsNullOrWhiteSpace(resolvedDisplayName))
                throw new InvalidOperationException($"Journey encounter '{name}' has no display name.");
            if (resolvedTimeline == null)
                throw new InvalidOperationException($"Journey encounter '{name}' has no realtime timeline.");

            int baseTier = enemyDefinition != null ? enemyDefinition.tier : tier;
            int baseHealth = enemyDefinition != null ? enemyDefinition.maximumHealth : maximumHealth;
            int[] baseDamages = enemyDefinition != null
                ? enemyDefinition.legacyTurnDamages
                : legacyTurnDamages;
            float healthMultiplier = enemyVariant != null ? enemyVariant.healthMultiplier : 1f;
            float damageMultiplier = EnemyDamageMultiplier;
            int tierOffset = enemyVariant != null ? enemyVariant.tierOffset : 0;
            int[] damages = baseDamages == null || baseDamages.Length == 0
                ? new[] { 0 }
                : ScaleDamages(baseDamages, damageMultiplier);
            return new EnemyDef(
                resolvedEnemyId,
                resolvedDisplayName,
                Mathf.Max(1, baseTier + tierOffset),
                Mathf.Max(1, Mathf.RoundToInt(baseHealth * Mathf.Max(.1f, healthMultiplier))),
                damages);
        }

        public RealtimeEnemyTimelinePattern[] BuildTimelinePatterns()
        {
            RealtimeEnemyTimelineProfile resolvedTimeline = ResolvedTimeline;
            if (resolvedTimeline == null)
                throw new InvalidOperationException($"Journey encounter '{name}' has no realtime timeline.");
            return resolvedTimeline.BuildPatterns(EnemyDamageMultiplier);
        }

        public IRealtimeEnemyTimelineStrategy BuildTimelineStrategy(int battleSeed)
        {
            RealtimeEnemyTimelineProfile resolvedTimeline = ResolvedTimeline;
            if (resolvedTimeline == null)
                throw new InvalidOperationException($"Journey encounter '{name}' has no realtime timeline.");
            return resolvedTimeline.BuildStrategy(battleSeed, EnemyDamageMultiplier);
        }

        private float EnemyDamageMultiplier =>
            (enemyVariant != null ? enemyVariant.damageMultiplier : 1f) *
            (enemyDefinition != null
                ? Mathf.Max(.1f, enemyDefinition.regionalDamageMultiplier)
                : 1f);

        private static int[] ScaleDamages(int[] source, float multiplier)
        {
            var result = new int[source.Length];
            float safeMultiplier = Mathf.Max(0f, multiplier);
            for (int index = 0; index < result.Length; index++)
                result[index] = Mathf.Max(0, Mathf.RoundToInt(source[index] * safeMultiplier));
            return result;
        }
    }
}
