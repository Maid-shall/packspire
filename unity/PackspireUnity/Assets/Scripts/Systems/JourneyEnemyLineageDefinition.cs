using UnityEngine;

namespace Packspire
{
    public enum JourneyEnemyOrigin
    {
        RegionOriginal,
        RecurringLineage,
        SpecialReturn
    }

    /// <summary>
    /// Recognition contract shared by regional forms of one recurring enemy.
    /// Regional forms remain separate enemy definitions so art and small action
    /// differences do not turn into code branches.
    /// </summary>
    [CreateAssetMenu(
        fileName = "JourneyEnemyLineageDefinition",
        menuName = "PACKSPIRE/Journey Enemy Lineage Definition")]
    public sealed class JourneyEnemyLineageDefinition : ScriptableObject
    {
        public string lineageId = string.Empty;
        public string displayLabel = string.Empty;

        [Header("Recognition contract")]
        [TextArea] public string sharedSilhouette = string.Empty;
        [TextArea] public string sharedEquipment = string.Empty;
        [TextArea] public string sharedTelegraph = string.Empty;

        [Header("Shared behaviour")]
        public RealtimeEnemyTimelineProfile baseBehaviorProfile;
    }
}
