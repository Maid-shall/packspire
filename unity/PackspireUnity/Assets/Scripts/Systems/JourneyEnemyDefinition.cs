using System;
using UnityEngine;

namespace Packspire
{
    /// <summary>
    /// Stable enemy identity and presentation. Behaviour and rank modifiers live
    /// in separate assets so a large roster does not duplicate timeline data.
    /// </summary>
    [CreateAssetMenu(
        fileName = "JourneyEnemyDefinition",
        menuName = "PACKSPIRE/Journey Enemy Definition")]
    public sealed class JourneyEnemyDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string enemyId = string.Empty;
        public string displayName = string.Empty;
        [Min(1)] public int tier = 1;
        [Min(1)] public int maximumHealth = 1;
        public int[] legacyTurnDamages = Array.Empty<int>();

        [Header("Production origin")]
        public JourneyEnemyOrigin origin = JourneyEnemyOrigin.RegionOriginal;
        public string regionId = string.Empty;
        public JourneyEnemyLineageDefinition lineage;
        [Tooltip("Optional regional action override. Empty uses the lineage behaviour.")]
        public RealtimeEnemyTimelineProfile regionalBehaviorProfile;
        [Tooltip("Regional power growth applied without duplicating the shared action rhythm.")]
        [Min(.1f)] public float regionalDamageMultiplier = 1f;

        [Header("Presentation")]
        public string fallbackSpriteResource = string.Empty;
        public string battleSheetResource = string.Empty;
        [Tooltip("Optional authored pose/effect mapping. Empty keeps legacy pose names and generic effect anchors.")]
        public JourneyEnemyBattlePresentationProfile battlePresentation;
        public string[] orderedBattlePoseNames = Array.Empty<string>();
        [Tooltip("Battle composition inherited from the legacy battle-stage presets.")]
        public BattleFormationScale battleFormation = BattleFormationScale.Normal;
        [Tooltip("Optional world-space height for the complete battle sprite. Zero keeps the legacy scale multiplier.")]
        [Min(0f)] public float battleTargetHeight;
        [Min(.05f)] public float battleScale = .66f;
    }
}
