using UnityEngine;

namespace Packspire
{
    public enum JourneyEnemyPopulationClass
    {
        Common,
        Enhanced,
        Elite,
        Boss
    }

    /// <summary>
    /// Reusable numerical treatment applied to an enemy identity. It deliberately
    /// contains no art or action sequence, keeping rank tuning independent from
    /// the roughly two hundred planned enemy identities.
    /// </summary>
    [CreateAssetMenu(
        fileName = "JourneyEnemyVariantDefinition",
        menuName = "PACKSPIRE/Journey Enemy Variant Definition")]
    public sealed class JourneyEnemyVariantDefinition : ScriptableObject
    {
        public string variantId = string.Empty;
        public JourneyEnemyPopulationClass populationClass = JourneyEnemyPopulationClass.Common;
        [Min(0)] public int tierOffset;
        [Min(.1f)] public float healthMultiplier = 1f;
        [Min(0f)] public float damageMultiplier = 1f;
        public string displayNamePrefix = string.Empty;
        public string displayNameSuffix = string.Empty;

        public string ApplyDisplayName(string baseName) =>
            $"{displayNamePrefix}{baseName}{displayNameSuffix}";
    }

    public static class JourneyEnemyPopulationTargets
    {
        public const int Common = 100;
        public const int Enhanced = 50;
        public const int Elite = 30;
        public const int Boss = 20;
        public const int Total = Common + Enhanced + Elite + Boss;

        public static int For(JourneyEnemyPopulationClass populationClass) => populationClass switch
        {
            JourneyEnemyPopulationClass.Common => Common,
            JourneyEnemyPopulationClass.Enhanced => Enhanced,
            JourneyEnemyPopulationClass.Elite => Elite,
            JourneyEnemyPopulationClass.Boss => Boss,
            _ => 0
        };
    }
}
