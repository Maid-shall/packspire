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
        [Header("Enemy")]
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

        public EnemyDef BuildEnemy()
        {
            if (string.IsNullOrWhiteSpace(enemyId))
                throw new InvalidOperationException($"Journey encounter '{name}' has no enemy id.");
            if (string.IsNullOrWhiteSpace(displayName))
                throw new InvalidOperationException($"Journey encounter '{name}' has no display name.");
            if (timeline == null)
                throw new InvalidOperationException($"Journey encounter '{name}' has no realtime timeline.");

            int[] damages = legacyTurnDamages == null || legacyTurnDamages.Length == 0
                ? new[] { 0 }
                : (int[])legacyTurnDamages.Clone();
            return new EnemyDef(enemyId, displayName, Mathf.Max(1, tier), Mathf.Max(1, maximumHealth), damages);
        }
    }
}
