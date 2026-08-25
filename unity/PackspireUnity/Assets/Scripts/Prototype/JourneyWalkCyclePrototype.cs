using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Packspire
{
    /// <summary>
    /// Isolated comparison of full-proportion frame animation and a small courier whose
    /// limited poses are supported by subtle procedural motion. Travel, body motion and
    /// the ground shadow intentionally live on separate transforms.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class JourneyWalkCyclePrototype : MonoBehaviour
    {
        private const string KainWalkResource = "Art/JourneyPrototype/courier-kain-walk-16-v1";
        private const string MiniWalkResource = "Art/UI/CourierRoutePrototype/courier-route-walk-sheet-v1";
        private const string MiniBattleResource = "Art/JourneyPrototype/Complete/journey-mio-battle-sheet-v2";
        private const int MiniFrameCount = 6;
        private const int MiniBattleFrameCount = 5;

        private enum PlaybackProfile
        {
            KainEight = 0,
            MiniTwoAssisted = 1,
            MiniFourAssisted = 2,
            MiniTwoRaw = 3
        }

        private enum TravelPresentation
        {
            FixedCourierParallax = 0,
            LegacyCrossScreen = 1
        }

        public enum RoadProfile
        {
            Standard = 0,
            Wide = 1,
            Narrow = 2
        }

        public enum BattleMotion
        {
            None = 0,
            Idle = 1,
            Attack = 2,
            Jump = 3,
            Brace = 4,
            Hit = 5
        }

        private static readonly int[] KainPoseIndices = { 0, 1, 3, 5, 8, 9, 11, 13 };
        private static readonly int[] MiniTwoPoseIndices = { 0, 3 };
        private static readonly int[] MiniFourPoseIndices = { 0, 1, 3, 4 };

        [Header("Travel root")]
        [SerializeField] private float travelSpeed = JourneyPresentationConfig.BaseTravelSpeed;
        [SerializeField] private float travelAcceleration = 4.5f;
        [SerializeField] private float leftBoundary = -7.2f;
        [SerializeField] private float rightBoundary = 7.2f;
        [SerializeField] private float fixedCourierX = -3.6f;
        [SerializeField] private float battleCourierX = -2.4f;
        [SerializeField] private float battleStageLift = 1.72f;
        [SerializeField] private TravelPresentation travelPresentation = TravelPresentation.FixedCourierParallax;

        [Header("Programmatic motion")]
        [SerializeField] private float stepsPerSecond = 2.35f;
        [SerializeField] private float bounceHeight = 0.075f;
        [SerializeField] private float squashAmount = 0.032f;
        [SerializeField] private float stretchAmount = 0.014f;
        [SerializeField] private float forwardTiltDegrees = 1.6f;
        [SerializeField] private PlaybackProfile playbackProfile = PlaybackProfile.MiniTwoAssisted;
        [SerializeField] private bool showDebugControls;

        private readonly List<Sprite> kainFrames = new List<Sprite>(16);
        private readonly List<Sprite> miniFrames = new List<Sprite>(MiniFrameCount);
        private readonly List<Sprite> miniBattleFrames = new List<Sprite>(MiniBattleFrameCount);
        private readonly List<UnityEngine.Object> ownedRuntimeAssets = new List<UnityEngine.Object>();

        private Transform motionRoot;
        private JourneyParallaxWorld parallaxWorld;
        private SpriteRenderer bodyRenderer;
        private SpriteRenderer shadowRenderer;
        private float travelPositionX;
        private float groundPositionY;
        private float depthPositionZ;
        private float animationClock;
        private float currentTravelSpeed;
        private float motionBlend = 1f;
        private float journeySpeedScale = 1f;
        private float stageLift;
        private float targetStageLift;
        private float battleDefenseLift;
        private float battleDefenseSquash;
        private float battleDefenseTilt;
        private float battleCompositionScale = 1f;
        private float battleMotionClock;
        private float battleMotionDuration;
        private bool battleActive;
        private BattleMotion battleMotion;
        private int currentFrame = -1;
        private bool walkingRequested = true;
        private GUIStyle overlayBoxStyle;
        private GUIStyle overlayTitleStyle;

        private bool UsesMiniCourier => playbackProfile != PlaybackProfile.KainEight;
        private bool UsesProgrammaticMotion =>
            playbackProfile == PlaybackProfile.MiniTwoAssisted ||
            playbackProfile == PlaybackProfile.MiniFourAssisted;

        public event Action<float> WorldAdvanced;
        public event Action<RoadProfile> RoadProfileChanged;
        public RoadProfile CurrentRoadProfile => parallaxWorld?.CurrentRoadProfile ?? RoadProfile.Standard;
        public JourneySceneryComposition CurrentSceneryComposition =>
            parallaxWorld?.CurrentSceneryComposition ?? JourneySceneryComposition.Standard;
        public float JourneyProgress => parallaxWorld?.JourneyProgress ?? 0f;

        private void Awake()
        {
            SetMainUiVisible(false);
            SpriteRenderer originalRenderer = GetComponent<SpriteRenderer>();
            originalRenderer.enabled = false;

            travelPositionX = transform.position.x;
            groundPositionY = transform.position.y;
            depthPositionZ = transform.position.z;

            BuildMotionHierarchy(originalRenderer.sortingOrder);
            LoadKainFrames();
            LoadMiniFrames();
            LoadMiniBattleFrames();
            parallaxWorld = new JourneyParallaxWorld();
            parallaxWorld.RoadProfileChanged += ForwardRoadProfileChanged;
            currentTravelSpeed = travelSpeed;
            RestartAnimation();
            ApplyPresentationVisibility();
        }

        private void Update()
        {
            HandleTestInput();
            if (AvailableFrameCount() == 0)
            {
                return;
            }

            float delta = Time.deltaTime;
            if (battleActive)
            {
                battleMotionClock += delta;
                if (battleMotion != BattleMotion.Idle && battleMotionDuration > 0f && battleMotionClock >= battleMotionDuration)
                {
                    battleMotion = BattleMotion.Idle;
                    battleMotionClock = 0f;
                    battleMotionDuration = 0f;
                    currentFrame = -1;
                }
            }
            stageLift = Mathf.MoveTowards(stageLift, targetStageLift, delta * 4.8f);
            ApplyBattleStageComposition();
            float targetSpeed = walkingRequested ? travelSpeed * journeySpeedScale : 0f;
            currentTravelSpeed = Mathf.MoveTowards(
                currentTravelSpeed,
                targetSpeed,
                travelAcceleration * delta);
            float requestedBlend = UsesProgrammaticMotion && walkingRequested ? 1f : 0f;
            motionBlend = Mathf.MoveTowards(motionBlend, requestedBlend, delta * 5.5f);

            if (currentTravelSpeed > 0.001f)
            {
                animationClock += delta;
                if (travelPresentation == TravelPresentation.FixedCourierParallax)
                {
                    AdvanceWorld(currentTravelSpeed * delta);
                }
                else
                {
                    travelPositionX += currentTravelSpeed * delta;
                    if (travelPositionX > rightBoundary)
                    {
                        travelPositionX = leftBoundary;
                    }
                }
            }

            ShowCurrentPose();
            ApplyTravelTransform();
            ApplyProgrammaticMotion();
        }

        private void BuildMotionHierarchy(int sortingOrder)
        {
            SortingGroup sortingGroup = gameObject.GetComponent<SortingGroup>();
            if (sortingGroup == null) sortingGroup = gameObject.AddComponent<SortingGroup>();
            sortingGroup.sortingLayerName = JourneyPresentationConfig.ActorSortingLayer;
            sortingGroup.sortingOrder = 0;

            GameObject shadowObject = new GameObject("Ground Shadow");
            shadowObject.layer = gameObject.layer;
            shadowObject.transform.SetParent(transform, false);
            shadowObject.transform.localPosition = new Vector3(0f, 0.035f, 0f);
            shadowRenderer = shadowObject.AddComponent<SpriteRenderer>();
            shadowRenderer.sprite = CreateSoftShadowSprite();
            JourneyPresentationConfig.Sort(shadowRenderer, JourneyPresentationConfig.ActorSortingLayer, 0);
            shadowRenderer.color = new Color(0.02f, 0.015f, 0.02f, 0.48f);

            GameObject motionObject = new GameObject("Motion Root");
            motionObject.layer = gameObject.layer;
            motionObject.transform.SetParent(transform, false);
            motionRoot = motionObject.transform;

            GameObject bodyObject = new GameObject("Courier Sprite");
            bodyObject.layer = gameObject.layer;
            bodyObject.transform.SetParent(motionRoot, false);
            bodyRenderer = bodyObject.AddComponent<SpriteRenderer>();
            JourneyPresentationConfig.Sort(bodyRenderer, JourneyPresentationConfig.ActorSortingLayer, 1);
        }

        private void LoadKainFrames()
        {
            Sprite[] loadedFrames = PackspireResources.LoadAll<Sprite>(KainWalkResource);
            if (loadedFrames == null || loadedFrames.Length == 0)
            {
                Debug.LogError($"Journey prototype walk sheet is missing: Resources/{KainWalkResource}");
                return;
            }

            System.Array.Sort(loadedFrames, (left, right) => string.CompareOrdinal(left.name, right.name));
            kainFrames.AddRange(loadedFrames);
        }

        private void LoadMiniFrames()
        {
            Sprite[] loadedFrames = PackspireResources.LoadAll<Sprite>(MiniWalkResource);
            if (loadedFrames == null || loadedFrames.Length < MiniFrameCount)
            {
                Debug.LogError($"Mini courier walk sheet must contain {MiniFrameCount} imported sprites: Resources/{MiniWalkResource}");
                return;
            }

            miniFrames.AddRange(loadedFrames
                .OrderBy(frame => frame.name, StringComparer.Ordinal)
                .Take(MiniFrameCount));
        }

        private void LoadMiniBattleFrames()
        {
            Sprite[] loadedFrames = PackspireResources.LoadAll<Sprite>(MiniBattleResource);
            if (loadedFrames == null || loadedFrames.Length < MiniBattleFrameCount)
            {
                Debug.LogWarning($"Mio battle sheet must contain {MiniBattleFrameCount} imported sprites: Resources/{MiniBattleResource}");
                return;
            }

            string[] orderedNames =
            {
                "journey-mio-battle-idle",
                "journey-mio-battle-attack",
                "journey-mio-battle-jump",
                "journey-mio-battle-brace",
                "journey-mio-battle-hit"
            };
            for (int index = 0; index < orderedNames.Length; index++)
            {
                Sprite frame = loadedFrames.FirstOrDefault(candidate => candidate.name == orderedNames[index]);
                if (frame == null)
                {
                    miniBattleFrames.Clear();
                    Debug.LogWarning($"Mio battle sheet is missing pose: {orderedNames[index]}");
                    return;
                }
                miniBattleFrames.Add(frame);
            }
        }

        public void SetJourneySpeedScale(float scale)
        {
            journeySpeedScale = Mathf.Clamp(scale, 0f, 2f);
        }

        public void SetJourneyWalking(bool walking)
        {
            walkingRequested = walking;
            if (!walking)
            {
                // A route decision is a true pause, not a cinematic deceleration. Keeping
                // residual velocity here made foreground props drift under stationary UI.
                currentTravelSpeed = 0f;
            }
        }

        public void SetBattleStage(bool battle)
        {
            battleActive = battle;
            targetStageLift = battle ? battleStageLift : 0f;
            // Phase changes can rebuild or cover the lower portion of the world in the
            // same frame. Snap the shared stage baseline before that UI becomes visible;
            // subsequent frame updates still keep the composition synchronized.
            stageLift = targetStageLift;
            ApplyBattleStageComposition();
            ApplyTravelTransform();
            battleMotion = battle ? BattleMotion.Idle : BattleMotion.None;
            battleMotionClock = 0f;
            battleMotionDuration = 0f;
            battleDefenseLift = 0f;
            battleDefenseSquash = 0f;
            battleDefenseTilt = 0f;
            currentFrame = -1;
            ShowCurrentPose();
            ApplyProgrammaticMotion();
        }

        public void SetBattleHorizontalAnchor(float worldX)
        {
            battleCourierX = worldX;
            if (battleActive) ApplyTravelTransform();
        }

        public void SetBattleCompositionScale(float scale)
        {
            battleCompositionScale = Mathf.Clamp(scale, .65f, 1.15f);
            if (battleActive) ApplyProgrammaticMotion();
        }

        public void SetBattleMotion(BattleMotion motion, float duration = 0f)
        {
            if (!battleActive && motion != BattleMotion.None) return;
            battleMotion = motion;
            battleMotionClock = 0f;
            battleMotionDuration = Mathf.Max(0f, duration);
            currentFrame = -1;
            ShowCurrentPose();
            ApplyProgrammaticMotion();
        }

        public void SetBattleDefensePose(float lift, float squash, float tilt)
        {
            battleDefenseLift = Mathf.Max(0f, lift);
            battleDefenseSquash = Mathf.Clamp(squash, -0.2f, 0.2f);
            battleDefenseTilt = Mathf.Clamp(tilt, -12f, 12f);
        }

        public void SetBuiltInForegroundVisible(bool visible)
        {
            parallaxWorld?.SetBuiltInForegroundVisible(visible);
        }

        public void SetJourneyBiome(int biomeIndex)
        {
            parallaxWorld?.SetJourneyBiome(biomeIndex);
        }

        public void SetJourneyProgress(float normalizedProgress)
        {
            parallaxWorld?.SetJourneyProgress(normalizedProgress);
        }

        public void SetRoadProfile(RoadProfile profile)
        {
            parallaxWorld?.SetRoadProfile(profile);
        }

        private void ForwardRoadProfileChanged(RoadProfile profile)
        {
            RoadProfileChanged?.Invoke(profile);
        }

        public void SetSceneryComposition(JourneySceneryComposition composition)
        {
            parallaxWorld?.SetSceneryComposition(composition);
        }

        private void ApplyBattleStageComposition()
        {
            parallaxWorld?.SetStageLift(stageLift);
        }

        private void AdvanceWorld(float distance)
        {
            if (distance <= 0f) return;
            parallaxWorld?.Advance(distance);
            WorldAdvanced?.Invoke(distance);
        }

        /// <summary>
        /// Advances the scenery by an exact amount for developer-menu capture and
        /// seam inspection. It intentionally does not advance gameplay time.
        /// </summary>
        public void AdvanceJourneyPreview(float distance)
        {
            AdvanceWorld(Mathf.Max(0f, distance));
        }

        private Sprite CreateSoftShadowSprite()
        {
            const int width = 128;
            const int height = 64;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "journey-programmatic-shadow",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };
            Color32[] pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float nx = (x + 0.5f - width * 0.5f) / (width * 0.5f);
                    float ny = (y + 0.5f - height * 0.5f) / (height * 0.5f);
                    float distance = nx * nx + ny * ny;
                    float alpha = Mathf.Clamp01((1f - distance) * 1.8f);
                    pixels[y * width + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, width, height),
                new Vector2(0.5f, 0.5f),
                100f);
            sprite.name = "journey-programmatic-shadow-sprite";
            ownedRuntimeAssets.Add(sprite);
            ownedRuntimeAssets.Add(texture);
            return sprite;
        }

        private void ShowCurrentPose()
        {
            Sprite next = ResolveCurrentSprite();
            if (next == null || next.GetInstanceID() == currentFrame)
            {
                return;
            }

            currentFrame = next.GetInstanceID();
            bodyRenderer.sprite = next;
            AlignShadowToCurrentSprite(next);
        }

        private void AlignShadowToCurrentSprite(Sprite sprite)
        {
            if (shadowRenderer == null || sprite == null)
            {
                return;
            }

            Vector3 shadowPosition = shadowRenderer.transform.localPosition;
            // Mini sheets author their custom pivot on the shared foot baseline. Their
            // transparent cell bounds extend well below the painted boots, so using
            // bounds.min would incorrectly push the shadow off the street.
            shadowPosition.y = UsesMiniCourier ? 0.035f : sprite.bounds.min.y + 0.045f;
            shadowRenderer.transform.localPosition = shadowPosition;
        }

        private Sprite ResolveCurrentSprite()
        {
            if (battleActive && UsesMiniCourier && miniBattleFrames.Count >= MiniBattleFrameCount)
            {
                int battleFrame = battleMotion switch
                {
                    BattleMotion.Attack => 1,
                    BattleMotion.Jump => 2,
                    BattleMotion.Brace => 3,
                    BattleMotion.Hit => 4,
                    _ => 0
                };
                return miniBattleFrames[battleFrame];
            }

            if (battleActive && UsesMiniCourier && miniFrames.Count > 0)
            {
                int fallbackFrame = battleMotion switch
                {
                    BattleMotion.Attack => 4,
                    BattleMotion.Jump => 3,
                    BattleMotion.Brace => 0,
                    BattleMotion.Hit => 5,
                    _ => 1
                };
                return miniFrames[Mathf.Clamp(fallbackFrame, 0, miniFrames.Count - 1)];
            }

            if (!walkingRequested && currentTravelSpeed <= 0.001f)
            {
                return UsesMiniCourier
                    ? (miniFrames.Count > 0 ? miniFrames[MiniTwoPoseIndices[0]] : null)
                    : (kainFrames.Count > 0 ? kainFrames[KainPoseIndices[0]] : null);
            }

            if (playbackProfile == PlaybackProfile.KainEight)
            {
                int pose = Mathf.FloorToInt(Mathf.Repeat(animationClock, 1.52f) / 1.52f * KainPoseIndices.Length);
                int source = KainPoseIndices[Mathf.Clamp(pose, 0, KainPoseIndices.Length - 1)];
                return source < kainFrames.Count ? kainFrames[source] : null;
            }

            int[] poses = playbackProfile == PlaybackProfile.MiniFourAssisted
                ? MiniFourPoseIndices
                : MiniTwoPoseIndices;
            float posesPerSecond = playbackProfile == PlaybackProfile.MiniFourAssisted
                ? stepsPerSecond * 2f
                : stepsPerSecond;
            int miniPose = Mathf.FloorToInt(animationClock * posesPerSecond) % poses.Length;
            int miniSource = poses[miniPose];
            return miniSource < miniFrames.Count ? miniFrames[miniSource] : null;
        }

        private void ApplyTravelTransform()
        {
            float displayedX = travelPresentation == TravelPresentation.FixedCourierParallax
                ? battleActive ? battleCourierX : fixedCourierX
                : travelPositionX;
            transform.position = new Vector3(displayedX, groundPositionY + stageLift, depthPositionZ);
        }

        private void ApplyProgrammaticMotion()
        {
            float phase = animationClock * stepsPerSecond * Mathf.PI;
            float lift01 = Mathf.Abs(Mathf.Sin(phase));
            float contact01 = Mathf.Pow(1f - lift01, 5f);
            float peak01 = Mathf.Pow(lift01, 4f);
            float assisted = UsesProgrammaticMotion ? motionBlend : 0f;

            float lift = lift01 * bounceHeight * assisted;
            float scaleX = 1f + contact01 * squashAmount * assisted - peak01 * stretchAmount * assisted;
            float scaleY = 1f - contact01 * squashAmount * assisted + peak01 * stretchAmount * assisted;
            float baseScale = (UsesMiniCourier ? 1.06f : 1f) *
                              (battleActive ? battleCompositionScale : 1f);
            float tilt = -forwardTiltDegrees * assisted;

            if (battleActive)
            {
                float normalized = battleMotionDuration > 0f
                    ? Mathf.Clamp01(battleMotionClock / battleMotionDuration)
                    : 0f;
                float actionArc = Mathf.Sin(normalized * Mathf.PI);
                float idleWave = Mathf.Sin(battleMotionClock * 2.35f);
                lift = idleWave * .012f;
                scaleX = 1f;
                scaleY = 1f;
                tilt = idleWave * .35f;

                switch (battleMotion)
                {
                    case BattleMotion.Attack:
                        lift += actionArc * .035f;
                        tilt = -3.8f * actionArc;
                        scaleX += actionArc * .035f;
                        scaleY -= actionArc * .018f;
                        break;
                    case BattleMotion.Jump:
                        lift += actionArc * (miniBattleFrames.Count >= MiniBattleFrameCount ? .18f : .58f);
                        tilt = -1.5f * actionArc;
                        scaleX -= actionArc * .012f;
                        scaleY += actionArc * .018f;
                        break;
                    case BattleMotion.Brace:
                        lift -= actionArc * .012f;
                        tilt = 1.8f * actionArc;
                        scaleX += actionArc * .035f;
                        scaleY -= actionArc * .025f;
                        break;
                    case BattleMotion.Hit:
                        tilt = 2.5f * actionArc;
                        scaleX -= actionArc * .02f;
                        break;
                }
            }

            motionRoot.localPosition = new Vector3(0f, lift + battleDefenseLift, 0f);
            motionRoot.localRotation = Quaternion.Euler(0f, 0f, tilt + battleDefenseTilt);
            motionRoot.localScale = new Vector3(
                baseScale * (scaleX + battleDefenseSquash),
                baseScale * (scaleY - battleDefenseSquash),
                1f);

            float shadowWidth = (UsesMiniCourier ? 1.35f : 1.65f) *
                                (battleActive ? battleCompositionScale : 1f);
            float shadowCompression = Mathf.Lerp(1f, 0.78f, lift01 * assisted);
            shadowRenderer.transform.localScale = new Vector3(
                shadowWidth * shadowCompression,
                0.32f * shadowCompression,
                1f);
            Color shadowColor = shadowRenderer.color;
            shadowColor.a = Mathf.Lerp(0.48f, 0.31f, lift01 * assisted);
            shadowRenderer.color = shadowColor;
        }

        private void HandleTestInput()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                SetMainUiVisible(true);
                SceneManager.LoadScene("Main");
                return;
            }

            // The playable journey presenter owns Space for roadside tasks. Keep the
            // walk/stop shortcut only in the isolated animation comparison scene.
            if (GetComponent<JourneyTravelGameplayPrototype>() == null && Input.GetKeyDown(KeyCode.Space))
            {
                walkingRequested = !walkingRequested;
            }

            if (Input.GetKeyDown(KeyCode.F1))
            {
                SetTravelPresentation(TravelPresentation.FixedCourierParallax);
            }
            else if (Input.GetKeyDown(KeyCode.F2))
            {
                SetTravelPresentation(TravelPresentation.LegacyCrossScreen);
            }

            if (Input.GetKeyDown(KeyCode.F6))
            {
                SetPlaybackProfile(PlaybackProfile.MiniTwoAssisted);
            }
            else if (Input.GetKeyDown(KeyCode.F7))
            {
                SetPlaybackProfile(PlaybackProfile.MiniFourAssisted);
            }
            else if (Input.GetKeyDown(KeyCode.F8))
            {
                SetPlaybackProfile(PlaybackProfile.MiniTwoRaw);
            }
            else if (Input.GetKeyDown(KeyCode.F9))
            {
                SetPlaybackProfile(PlaybackProfile.KainEight);
            }
        }

        private void OnGUI()
        {
            if (!showDebugControls)
            {
                return;
            }

            EnsureOverlayStyles();
            const float width = 292f;
            const float height = 350f;
            Rect area = new Rect(Screen.width - width - 18f, 18f, width, height);
            GUILayout.BeginArea(area, overlayBoxStyle);
            GUILayout.Label("JOURNEY PRESENTATION TEST", overlayTitleStyle);
            GUILayout.Label($"View: {PresentationLabel(travelPresentation)}");
            if (GUILayout.Button("FIXED + PARALLAX  [F1]", GUILayout.Height(30f)))
            {
                SetTravelPresentation(TravelPresentation.FixedCourierParallax);
            }
            if (GUILayout.Button("CROSS SCREEN  [F2]", GUILayout.Height(30f)))
            {
                SetTravelPresentation(TravelPresentation.LegacyCrossScreen);
            }

            GUILayout.Space(5f);
            GUILayout.Label($"Motion: {ProfileLabel(playbackProfile)}");

            if (GUILayout.Button("MINI 2 + MOTION  [F6]", GUILayout.Height(30f)))
            {
                SetPlaybackProfile(PlaybackProfile.MiniTwoAssisted);
            }
            if (GUILayout.Button("MINI 4 + MOTION  [F7]", GUILayout.Height(30f)))
            {
                SetPlaybackProfile(PlaybackProfile.MiniFourAssisted);
            }
            if (GUILayout.Button("MINI 2 RAW  [F8]", GUILayout.Height(30f)))
            {
                SetPlaybackProfile(PlaybackProfile.MiniTwoRaw);
            }
            if (GUILayout.Button("KAIN 8  [F9]", GUILayout.Height(30f)))
            {
                SetPlaybackProfile(PlaybackProfile.KainEight);
            }
            if (GUILayout.Button(walkingRequested ? "STOP + LAND" : "WALK", GUILayout.Height(28f)))
            {
                walkingRequested = !walkingRequested;
            }

            GUILayout.Label("Esc: return / Space: walk-stop");
            GUILayout.EndArea();
        }

        private void SetPlaybackProfile(PlaybackProfile profile)
        {
            if (playbackProfile == profile)
            {
                return;
            }

            playbackProfile = profile;
            RestartAnimation();
            Debug.Log($"Journey walk playback profile: {playbackProfile}");
        }

        private void SetTravelPresentation(TravelPresentation presentation)
        {
            if (travelPresentation == presentation)
            {
                return;
            }

            travelPresentation = presentation;
            if (travelPresentation == TravelPresentation.LegacyCrossScreen)
            {
                travelPositionX = leftBoundary;
            }
            ApplyPresentationVisibility();
            ApplyTravelTransform();
            Debug.Log($"Journey travel presentation: {travelPresentation}");
        }

        private void ApplyPresentationVisibility()
        {
            bool showParallax = travelPresentation == TravelPresentation.FixedCourierParallax;
            parallaxWorld?.SetVisible(showParallax);
        }

        private void RestartAnimation()
        {
            animationClock = 0f;
            currentFrame = -1;
            motionBlend = UsesProgrammaticMotion && walkingRequested ? 1f : 0f;
            ShowCurrentPose();
            ApplyProgrammaticMotion();
        }

        private int AvailableFrameCount()
        {
            return UsesMiniCourier ? miniFrames.Count : kainFrames.Count;
        }

        private void EnsureOverlayStyles()
        {
            if (overlayBoxStyle != null)
            {
                return;
            }

            overlayBoxStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(12, 12, 10, 10)
            };
            overlayTitleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                fontSize = 13
            };
        }

        private static string ProfileLabel(PlaybackProfile profile)
        {
            switch (profile)
            {
                case PlaybackProfile.MiniTwoAssisted:
                    return "MINI 2 + MOTION";
                case PlaybackProfile.MiniFourAssisted:
                    return "MINI 4 + MOTION";
                case PlaybackProfile.MiniTwoRaw:
                    return "MINI 2 RAW";
                default:
                    return "KAIN 8";
            }
        }

        private static string PresentationLabel(TravelPresentation presentation)
        {
            return presentation == TravelPresentation.FixedCourierParallax
                ? "FIXED + 4-LAYER PARALLAX"
                : "LEGACY CROSS SCREEN";
        }

        private static void SetMainUiVisible(bool visible)
        {
            PackspireUiFoundation foundation = PackspireUiFoundation.Instance;
            if (foundation != null)
            {
                foundation.SetJourneyPrototypeVisible(!visible);
            }
        }

        private void OnDestroy()
        {
            if (parallaxWorld != null)
            {
                parallaxWorld.RoadProfileChanged -= ForwardRoadProfileChanged;
                parallaxWorld.Dispose();
                parallaxWorld = null;
            }
            kainFrames.Clear();
            miniFrames.Clear();
            miniBattleFrames.Clear();
            for (int index = 0; index < ownedRuntimeAssets.Count; index++)
            {
                UnityEngine.Object asset = ownedRuntimeAssets[index];
                if (asset != null)
                {
                    Destroy(asset);
                }
            }
            ownedRuntimeAssets.Clear();
            SetMainUiVisible(true);
        }
    }
}
