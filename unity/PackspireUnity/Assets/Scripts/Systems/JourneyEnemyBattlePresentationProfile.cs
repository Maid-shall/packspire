using UnityEngine;

namespace Packspire
{
    /// <summary>
    /// Art-facing battle contract for one enemy. Gameplay timing and damage stay in
    /// realtime behaviour assets; this profile only maps those events to authored
    /// poses and sprite-local effect anchors.
    /// </summary>
    [CreateAssetMenu(
        fileName = "JourneyEnemyBattlePresentationProfile",
        menuName = "PACKSPIRE/Journey Enemy Battle Presentation")]
    public sealed class JourneyEnemyBattlePresentationProfile : ScriptableObject
    {
        [Header("Six-pose battle sheet")]
        public string idlePoseName = string.Empty;
        public string highAnticipationPoseName = string.Empty;
        public string highImpactPoseName = string.Empty;
        public string lowAnticipationPoseName = string.Empty;
        public string lowImpactPoseName = string.Empty;
        public string hitPoseName = string.Empty;


        [Header("Timing cue anchors")]
        [Tooltip("Weapon-tip position in the high anticipation sprite. Normalized from the sprite rectangle's bottom-left corner.")]
        public Vector2 highTimingCueAnchor = new Vector2(.5f, .82f);
        [Tooltip("Weapon-tip position in the low anticipation sprite. Normalized from the sprite rectangle's bottom-left corner.")]
        public Vector2 lowTimingCueAnchor = new Vector2(.5f, .28f);

        [Header("Impact contact anchors")]
        [Tooltip("Contact position in the high impact sprite. Normalized from the sprite rectangle's bottom-left corner.")]
        public Vector2 highImpactContactAnchor = new Vector2(.2f, .14f);
        [Tooltip("Contact position in the low impact sprite. Normalized from the sprite rectangle's bottom-left corner.")]
        public Vector2 lowImpactContactAnchor = new Vector2(.5f, .14f);

        [Header("Cue treatment")]
        [Min(.25f)] public float timingCueScale = 1f;
        [Range(0f, 1f)] public float timingCueBackdropOpacity = .45f;
        [Range(0f, 1f)] public float timingCueRingOpacity = .9f;
        [Min(.1f)] public float impactSparkScale = .34f;

        public string[] OrderedPoseNames => new[]
        {
            idlePoseName,
            highAnticipationPoseName,
            highImpactPoseName,
            lowAnticipationPoseName,
            lowImpactPoseName,
            hitPoseName
        };

        public Vector2 TimingCueAnchor(bool high) =>
            ClampAnchor(high ? highTimingCueAnchor : lowTimingCueAnchor);

        public Vector2 ImpactContactAnchor(bool high) =>
            ClampAnchor(high ? highImpactContactAnchor : lowImpactContactAnchor);

        public bool AnchorsAreNormalized =>
            IsNormalized(highTimingCueAnchor) &&
            IsNormalized(lowTimingCueAnchor) &&
            IsNormalized(highImpactContactAnchor) &&
            IsNormalized(lowImpactContactAnchor);

        public bool HasCompletePoseSet
        {
            get
            {
                string[] names = OrderedPoseNames;
                for (int index = 0; index < names.Length; index++)
                    if (string.IsNullOrWhiteSpace(names[index])) return false;
                return true;
            }
        }

        private static bool IsNormalized(Vector2 anchor) =>
            anchor.x >= 0f && anchor.x <= 1f &&
            anchor.y >= 0f && anchor.y <= 1f;

        private static Vector2 ClampAnchor(Vector2 anchor) => new Vector2(
            Mathf.Clamp01(anchor.x),
            Mathf.Clamp01(anchor.y));
    }
}
