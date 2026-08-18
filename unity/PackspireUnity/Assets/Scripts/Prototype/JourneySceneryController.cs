using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Packspire
{
    /// <summary>
    /// Owns route-side props and landmark passages. Movement is driven exclusively by
    /// the exact world distance emitted by JourneyWalkCyclePrototype; this component
    /// never estimates background movement from Time.deltaTime on its own.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class JourneySceneryController : MonoBehaviour
    {
        private enum ScenicDepth
        {
            GroundAccent,
            Roadside,
            CloseForeground
        }

        private sealed class ScenicPropInstance
        {
            public SpriteRenderer Renderer;
            public ScenicDepth Depth;
            public float DistanceFactor;
            public float AnchorOffsetY;
        }

        private const string PropsResource = "Art/JourneyPrototype/Complete/journey-foreground-props-v1";
        private const string LandmarksResource = "Art/JourneyPrototype/Complete/journey-route-landmarks-v1";
        private const float RoadsideDistanceFactor = 1.08f;
        private const float CloseForegroundDistanceFactor = 1.88f;
        private const float LandmarkZoneStart = .12f;
        private const float LandmarkZoneEnd = .9f;
        private const float LandmarkQuietMargin = .08f;
        private const float InitialCloseForegroundDistance = 22f * JourneyPresentationConfig.BaseTravelSpeed;
        private const float GroundAccentDistanceFactor = 1f;

        private readonly List<ScenicPropInstance> activeProps = new List<ScenicPropInstance>();
        private readonly bool[] landmarkSeenByBiome = new bool[3];
        private readonly List<UnityEngine.Object> ownedRuntimeAssets = new List<UnityEngine.Object>();

        private JourneyWalkCyclePrototype walker;
        private GameObject sceneryRoot;
        private Sprite[] propSprites = Array.Empty<Sprite>();
        private Sprite[] landmarkSprites = Array.Empty<Sprite>();
        private Sprite drownedPuddleSprite;
        private Sprite blackBellFissureSprite;
        private SpriteRenderer landmarkRenderer;
        private JourneyWalkCyclePrototype.RoadProfile roadProfile;
        private JourneyPresentationConfig.RoadDefinition road;
        private float roadsideDistanceRemaining;
        private float closeForegroundDistanceRemaining = InitialCloseForegroundDistance;
        private float groundAccentDistanceRemaining;
        private int nextRoadsidePropIndex;
        private int nextCloseForegroundPropIndex;
        private int nextGroundAccentIndex;
        private int biomeIndex;
        private JourneySceneryComposition composition = JourneySceneryComposition.Standard;
        private bool allowRoadside;
        private bool allowCloseForeground;
        private bool landmarkScheduled;
        private bool landmarkZoneActive;

        public JourneySceneryComposition CurrentComposition => composition;

        public void Initialize(JourneyWalkCyclePrototype worldWalker)
        {
            if (walker != null) return;
            walker = worldWalker;
            roadProfile = walker != null
                ? walker.CurrentRoadProfile
                : JourneyWalkCyclePrototype.RoadProfile.Standard;
            road = JourneyPresentationConfig.GetRoad(0, roadProfile);
            composition = JourneyPresentationConfig.GetComposition(roadProfile);

            sceneryRoot = new GameObject("Journey Dynamic Scenery");
            LoadSprites();
            BuildBiomeAccentSprites();
            BuildLandmarkRenderer();

            if (walker == null) return;
            walker.WorldAdvanced += AdvanceWorld;
            walker.RoadProfileChanged += SetRoadProfile;
            walker.SetSceneryComposition(composition);
        }

        public void BeginTravel(int targetBiome, bool scheduleLandmark)
        {
            biomeIndex = Mathf.Max(0, targetBiome);
            road = JourneyPresentationConfig.GetRoad(biomeIndex, roadProfile);
            ReanchorActiveScenery();
            landmarkScheduled = scheduleLandmark &&
                biomeIndex < landmarkSeenByBiome.Length &&
                !landmarkSeenByBiome[biomeIndex];
            landmarkZoneActive = false;
            if (landmarkRenderer != null) landmarkRenderer.enabled = false;
            if (landmarkScheduled) ClearOrdinaryProps();
            ResetCompositionSpacing();
        }

        public bool HasSeenLandmark(int targetBiome)
        {
            return targetBiome >= 0 &&
                targetBiome < landmarkSeenByBiome.Length &&
                landmarkSeenByBiome[targetBiome];
        }

        public void SetActivity(bool roadside, bool closeForeground)
        {
            allowRoadside = roadside;
            allowCloseForeground = closeForeground;
            if (!closeForeground) ClearScenery(ScenicDepth.CloseForeground);
        }

        public void SetTravelProgress(float normalizedProgress)
        {
            if (!allowRoadside) return;
            UpdateLandmarkZone(Mathf.Clamp01(normalizedProgress));
        }

        public void SetRoadProfile(JourneyWalkCyclePrototype.RoadProfile profile)
        {
            roadProfile = profile;
            road = JourneyPresentationConfig.GetRoad(biomeIndex, profile);
            SetComposition(JourneyPresentationConfig.GetComposition(profile));
            ReanchorActiveScenery();
        }

        private void SetComposition(JourneySceneryComposition next)
        {
            composition = next;
            walker?.SetSceneryComposition(next);
            ResetCompositionSpacing();
        }

        private void LoadSprites()
        {
            propSprites = PackspireResources.LoadAll<Sprite>(PropsResource)
                .OrderBy(sprite => sprite.name, StringComparer.Ordinal)
                .ToArray();
            landmarkSprites = PackspireResources.LoadAll<Sprite>(LandmarksResource)
                .OrderBy(sprite => sprite.name, StringComparer.Ordinal)
                .ToArray();

            if (propSprites.Length < 4)
                Debug.LogError($"Journey prop sheet must contain four imported sprites: Resources/{PropsResource}");
            if (landmarkSprites.Length < 3)
                Debug.LogError($"Journey landmark sheet must contain three imported sprites: Resources/{LandmarksResource}");
        }

        private void BuildLandmarkRenderer()
        {
            GameObject landmark = new GameObject("Route Landmark");
            landmark.transform.SetParent(sceneryRoot.transform, false);
            landmarkRenderer = landmark.AddComponent<SpriteRenderer>();
            JourneyPresentationConfig.Sort(
                landmarkRenderer,
                JourneyPresentationConfig.RoadsideSortingLayer,
                -10);
            landmarkRenderer.enabled = false;
        }

        private void AdvanceWorld(float distance)
        {
            if (distance <= 0f) return;

            for (int index = activeProps.Count - 1; index >= 0; index--)
            {
                ScenicPropInstance prop = activeProps[index];
                if (prop?.Renderer == null)
                {
                    activeProps.RemoveAt(index);
                    continue;
                }

                prop.Renderer.transform.position += Vector3.left * distance * prop.DistanceFactor;
                if (prop.Renderer.transform.position.x >= -12f) continue;
                Destroy(prop.Renderer.gameObject);
                activeProps.RemoveAt(index);
            }

            if (!allowRoadside || propSprites.Length < 4) return;

            bool landmarkQuietZone = landmarkScheduled &&
                walker != null &&
                walker.JourneyProgress >= Mathf.Max(0f, LandmarkZoneStart - LandmarkQuietMargin) &&
                walker.JourneyProgress <= Mathf.Min(1f, LandmarkZoneEnd + LandmarkQuietMargin);
            if (landmarkQuietZone || landmarkZoneActive) return;

            roadsideDistanceRemaining -= distance;
            closeForegroundDistanceRemaining -= distance;
            groundAccentDistanceRemaining -= distance;

            if (biomeIndex > 0 && groundAccentDistanceRemaining <= 0f &&
                CountScenery(ScenicDepth.GroundAccent) < GroundAccentLimit())
            {
                SpawnGroundAccent(10.8f);
                groundAccentDistanceRemaining = UnityEngine.Random.Range(
                    GroundAccentGapMin(),
                    GroundAccentGapMax());
            }

            if (roadsideDistanceRemaining <= 0f &&
                CountScenery(ScenicDepth.Roadside) < RoadsideLimit())
            {
                SpawnRoadsideProp(10.5f);
                roadsideDistanceRemaining = UnityEngine.Random.Range(RoadsideGapMin(), RoadsideGapMax());
            }

            if (allowCloseForeground &&
                closeForegroundDistanceRemaining <= 0f &&
                CountScenery(ScenicDepth.CloseForeground) < CloseForegroundLimit())
            {
                SpawnCloseForegroundProp(11f);
                closeForegroundDistanceRemaining = UnityEngine.Random.Range(ForegroundGapMin(), ForegroundGapMax());
            }
        }

        private int CountScenery(ScenicDepth depth)
        {
            return activeProps.Count(prop => prop != null && prop.Renderer != null && prop.Depth == depth);
        }

        private void SpawnRoadsideProp(float worldX, float? forcedScale = null)
        {
            if (propSprites.Length < 3) return;
            // Road furniture belongs behind the courier. Broken direction signs are
            // reserved for authored route decisions; repeating ambient pieces use
            // only lamps and the occasional cart.
            int[] roadsideSpriteIndices = { 2, 1, 2 };
            int propIndex = roadsideSpriteIndices[nextRoadsidePropIndex % roadsideSpriteIndices.Length];
            nextRoadsidePropIndex++;

            SpriteRenderer renderer = CreateRenderer("Roadside Scenery");
            renderer.sprite = propSprites[propIndex];
            JourneyPresentationConfig.Sort(renderer, JourneyPresentationConfig.RoadsideSortingLayer, 0);
            renderer.color = biomeIndex switch
            {
                1 => new Color(.64f, .83f, .88f, .96f),
                2 => new Color(.76f, .59f, .57f, .96f),
                _ => new Color(.78f, .82f, .84f, .96f)
            };
            renderer.transform.position = new Vector3(worldX, road.RoadsideY, 0f);
            renderer.transform.localScale = Vector3.one * (forcedScale ?? RoadsideScale(propIndex));
            activeProps.Add(new ScenicPropInstance
            {
                Renderer = renderer,
                Depth = ScenicDepth.Roadside,
                DistanceFactor = RoadsideDistanceFactor
            });
        }

        private void SpawnGroundAccent(float worldX, float? forcedScale = null)
        {
            Sprite sprite = biomeIndex == 1 ? drownedPuddleSprite : blackBellFissureSprite;
            if (sprite == null || biomeIndex == 0) return;

            SpriteRenderer renderer = CreateRenderer(
                biomeIndex == 1 ? "Drowned Road Puddle" : "Black Bell Road Fissure");
            renderer.sprite = sprite;
            JourneyPresentationConfig.Sort(renderer, JourneyPresentationConfig.RoadsideSortingLayer, -20);
            renderer.color = biomeIndex == 1
                ? new Color(.38f, .84f, .94f, .58f)
                : new Color(1f, .2f, .055f, .62f);
            renderer.flipX = nextGroundAccentIndex % 2 == 1;
            float baseScale = forcedScale ?? (biomeIndex == 1
                ? UnityEngine.Random.Range(1.12f, 1.58f)
                : UnityEngine.Random.Range(.82f, 1.18f));
            renderer.transform.position = new Vector3(worldX, road.RoadsideY - .13f, 0f);
            renderer.transform.localScale = biomeIndex == 1
                ? new Vector3(baseScale, baseScale * .72f, 1f)
                : new Vector3(baseScale, baseScale, 1f);
            renderer.transform.localRotation = Quaternion.Euler(
                0f,
                0f,
                biomeIndex == 1 ? 0f : (nextGroundAccentIndex % 3 - 1) * 3.5f);
            nextGroundAccentIndex++;
            activeProps.Add(new ScenicPropInstance
            {
                Renderer = renderer,
                Depth = ScenicDepth.GroundAccent,
                DistanceFactor = GroundAccentDistanceFactor
            });
        }

        private void SpawnCloseForegroundProp(float worldX, float? forcedScale = null)
        {
            if (propSprites.Length < 4) return;

            // Close foreground is a separate visual language from readable road
            // furniture: large, dark shapes that briefly cross in front of the
            // courier and sell parallax without looking like repeated decorations.
            // The cart source contains faint rectangular edge contamination that
            // becomes visible when heavily darkened, so only clean silhouettes are
            // eligible for this layer.
            int[] foregroundSpriteIndices = { 2, 3 };
            int propIndex = foregroundSpriteIndices[
                nextCloseForegroundPropIndex % foregroundSpriteIndices.Length];
            nextCloseForegroundPropIndex++;
            SpriteRenderer renderer = CreateRenderer("Close Foreground Silhouette");
            renderer.sprite = propSprites[propIndex];
            JourneyPresentationConfig.Sort(renderer, JourneyPresentationConfig.ForegroundSortingLayer, 0);
            renderer.color = biomeIndex switch
            {
                1 => new Color(.045f, .085f, .095f, .66f),
                2 => new Color(.1f, .045f, .05f, .7f),
                _ => new Color(.065f, .07f, .075f, .68f)
            };
            float anchorOffsetY = propIndex == 2 ? -.42f : -.22f;
            renderer.transform.position = new Vector3(
                worldX,
                road.CloseForegroundY + anchorOffsetY,
                0f);
            renderer.transform.localScale = Vector3.one * (forcedScale ?? ForegroundScale(propIndex));
            activeProps.Add(new ScenicPropInstance
            {
                Renderer = renderer,
                Depth = ScenicDepth.CloseForeground,
                DistanceFactor = CloseForegroundDistanceFactor,
                AnchorOffsetY = anchorOffsetY
            });
        }

        private static float RoadsideScale(int propIndex)
        {
            return propIndex == 1
                ? UnityEngine.Random.Range(.44f, .52f)
                : UnityEngine.Random.Range(.48f, .56f);
        }

        private static float ForegroundScale(int propIndex)
        {
            return propIndex == 3
                ? UnityEngine.Random.Range(.9f, 1f)
                : UnityEngine.Random.Range(.86f, .96f);
        }

        private SpriteRenderer CreateRenderer(string objectName)
        {
            GameObject scenicObject = new GameObject(objectName);
            scenicObject.transform.SetParent(sceneryRoot.transform, false);
            return scenicObject.AddComponent<SpriteRenderer>();
        }

        private void UpdateLandmarkZone(float travelProgress)
        {
            if (!landmarkScheduled) return;

            if (!landmarkZoneActive && travelProgress >= LandmarkZoneStart)
            {
                landmarkZoneActive = true;
                landmarkSeenByBiome[Mathf.Clamp(biomeIndex, 0, landmarkSeenByBiome.Length - 1)] = true;
                ClearOrdinaryProps();
                ShowLandmark();
            }

            if (!landmarkZoneActive || landmarkRenderer == null) return;

            float passage = Mathf.InverseLerp(LandmarkZoneStart, LandmarkZoneEnd, travelProgress);
            landmarkRenderer.transform.position = new Vector3(
                Mathf.Lerp(11f, -11f, Mathf.SmoothStep(0f, 1f, passage)),
                road.LandmarkY,
                0f);

            if (travelProgress < LandmarkZoneEnd) return;
            landmarkRenderer.enabled = false;
            landmarkZoneActive = false;
            landmarkScheduled = false;
            roadsideDistanceRemaining = Mathf.Max(
                roadsideDistanceRemaining,
                2.5f * JourneyPresentationConfig.BaseTravelSpeed);
        }

        private void ShowLandmark()
        {
            if (landmarkRenderer == null || landmarkSprites.Length == 0) return;
            landmarkRenderer.sprite = landmarkSprites[Mathf.Clamp(biomeIndex, 0, landmarkSprites.Length - 1)];
            landmarkRenderer.transform.position = new Vector3(11f, road.LandmarkY, 0f);
            landmarkRenderer.transform.localScale = Vector3.one * (biomeIndex == 1 ? .62f : .72f);
            landmarkRenderer.enabled = true;
        }

        private void ReanchorActiveScenery()
        {
            foreach (ScenicPropInstance prop in activeProps)
            {
                if (prop?.Renderer == null) continue;
                Vector3 position = prop.Renderer.transform.position;
                position.y = prop.Depth switch
                {
                    ScenicDepth.GroundAccent => road.RoadsideY - .13f,
                    ScenicDepth.Roadside => road.RoadsideY,
                    _ => road.CloseForegroundY + prop.AnchorOffsetY
                };
                prop.Renderer.transform.position = position;
            }

            if (landmarkRenderer != null && landmarkRenderer.enabled)
            {
                Vector3 position = landmarkRenderer.transform.position;
                position.y = road.LandmarkY;
                landmarkRenderer.transform.position = position;
            }
        }

        private void ResetCompositionSpacing()
        {
            roadsideDistanceRemaining = composition switch
            {
                JourneySceneryComposition.Open => 6f * JourneyPresentationConfig.BaseTravelSpeed,
                JourneySceneryComposition.Dense => .8f * JourneyPresentationConfig.BaseTravelSpeed,
                _ => 2.5f * JourneyPresentationConfig.BaseTravelSpeed
            };
            closeForegroundDistanceRemaining = composition switch
            {
                JourneySceneryComposition.Open => 30f * JourneyPresentationConfig.BaseTravelSpeed,
                JourneySceneryComposition.Dense => 16f * JourneyPresentationConfig.BaseTravelSpeed,
                _ => 22f * JourneyPresentationConfig.BaseTravelSpeed
            };
            groundAccentDistanceRemaining = biomeIndex == 0
                ? float.PositiveInfinity
                : composition == JourneySceneryComposition.Dense
                    ? 1.6f * JourneyPresentationConfig.BaseTravelSpeed
                    : 3.2f * JourneyPresentationConfig.BaseTravelSpeed;
        }

        private int RoadsideLimit() => 1;

        private int CloseForegroundLimit() => 1;

        private int GroundAccentLimit() => composition == JourneySceneryComposition.Open ? 1 : 2;

        private float GroundAccentGapMin() => composition switch
        {
            JourneySceneryComposition.Open => 15f * JourneyPresentationConfig.BaseTravelSpeed,
            JourneySceneryComposition.Dense => 5f * JourneyPresentationConfig.BaseTravelSpeed,
            _ => 8f * JourneyPresentationConfig.BaseTravelSpeed
        };

        private float GroundAccentGapMax() => composition switch
        {
            JourneySceneryComposition.Open => 22f * JourneyPresentationConfig.BaseTravelSpeed,
            JourneySceneryComposition.Dense => 8f * JourneyPresentationConfig.BaseTravelSpeed,
            _ => 13f * JourneyPresentationConfig.BaseTravelSpeed
        };

        private float RoadsideGapMin() => composition switch
        {
            JourneySceneryComposition.Open => 30f * JourneyPresentationConfig.BaseTravelSpeed,
            JourneySceneryComposition.Dense => 18f * JourneyPresentationConfig.BaseTravelSpeed,
            _ => 24f * JourneyPresentationConfig.BaseTravelSpeed
        };

        private float RoadsideGapMax() => composition switch
        {
            JourneySceneryComposition.Open => 42f * JourneyPresentationConfig.BaseTravelSpeed,
            JourneySceneryComposition.Dense => 28f * JourneyPresentationConfig.BaseTravelSpeed,
            _ => 36f * JourneyPresentationConfig.BaseTravelSpeed
        };

        private float ForegroundGapMin() => composition switch
        {
            JourneySceneryComposition.Open => 36f * JourneyPresentationConfig.BaseTravelSpeed,
            JourneySceneryComposition.Dense => 18f * JourneyPresentationConfig.BaseTravelSpeed,
            _ => 24f * JourneyPresentationConfig.BaseTravelSpeed
        };

        private float ForegroundGapMax() => composition switch
        {
            JourneySceneryComposition.Open => 52f * JourneyPresentationConfig.BaseTravelSpeed,
            JourneySceneryComposition.Dense => 28f * JourneyPresentationConfig.BaseTravelSpeed,
            _ => 36f * JourneyPresentationConfig.BaseTravelSpeed
        };

        public void ClearAll()
        {
            ClearOrdinaryProps();
            if (landmarkRenderer != null) landmarkRenderer.enabled = false;
            landmarkZoneActive = false;
        }

        private void ClearScenery(ScenicDepth depth)
        {
            for (int index = activeProps.Count - 1; index >= 0; index--)
            {
                ScenicPropInstance prop = activeProps[index];
                if (prop == null || prop.Depth != depth) continue;
                if (prop.Renderer != null) Destroy(prop.Renderer.gameObject);
                activeProps.RemoveAt(index);
            }
        }

        private void ClearOrdinaryProps()
        {
            foreach (ScenicPropInstance prop in activeProps)
                if (prop?.Renderer != null) Destroy(prop.Renderer.gameObject);
            activeProps.Clear();
        }

        private void BuildBiomeAccentSprites()
        {
            drownedPuddleSprite = CreateRuntimeSprite(
                "journey-drowned-puddle",
                CreatePuddleTexture(),
                100f);
            blackBellFissureSprite = CreateRuntimeSprite(
                "journey-black-bell-fissure",
                CreateFissureTexture(),
                100f);
        }

        private Sprite CreateRuntimeSprite(string name, Texture2D texture, float pixelsPerUnit)
        {
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(.5f, .5f),
                pixelsPerUnit);
            sprite.name = name;
            ownedRuntimeAssets.Add(sprite);
            return sprite;
        }

        private Texture2D CreatePuddleTexture()
        {
            const int width = 256;
            const int height = 80;
            Texture2D texture = CreateRuntimeTexture("journey-drowned-puddle", width, height);
            Color32[] pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                float nx = (x + .5f - width * .5f) / (width * .5f);
                float ny = (y + .5f - height * .5f) / (height * .5f);
                float ellipse = Mathf.Clamp01(1f - nx * nx - ny * ny);
                float edge = Mathf.Pow(ellipse, .72f);
                float streak = Mathf.Abs(ny) < .09f && Mathf.Sin(x * .17f) > .35f ? .36f : 0f;
                byte alpha = (byte)(Mathf.Clamp01(edge * .48f + streak * ellipse) * 255f);
                pixels[y * width + x] = new Color32(210, 250, 255, alpha);
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            return texture;
        }

        private Texture2D CreateFissureTexture()
        {
            const int width = 256;
            const int height = 72;
            Texture2D texture = CreateRuntimeTexture("journey-black-bell-fissure", width, height);
            Color32[] pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                float center = height * .5f + Mathf.Sin(x * .075f) * 7f + Mathf.Sin(x * .19f) * 3f;
                float distance = Mathf.Abs(y - center);
                float main = Mathf.Clamp01(1f - distance / 3.2f);
                float branchCenter = center + Mathf.Sign(Mathf.Sin(x * .055f)) * Mathf.Repeat(x, 39f) * .22f;
                float branch = Mathf.Repeat(x, 39f) < 17f
                    ? Mathf.Clamp01(1f - Mathf.Abs(y - branchCenter) / 1.5f)
                    : 0f;
                float glow = Mathf.Clamp01(main + branch * .72f);
                pixels[y * width + x] = new Color32(255, 184, 72, (byte)(glow * 225f));
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            return texture;
        }

        private Texture2D CreateRuntimeTexture(string name, int width, int height)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };
            ownedRuntimeAssets.Add(texture);
            return texture;
        }

        private void OnDestroy()
        {
            if (walker != null)
            {
                walker.WorldAdvanced -= AdvanceWorld;
                walker.RoadProfileChanged -= SetRoadProfile;
            }
            foreach (UnityEngine.Object asset in ownedRuntimeAssets)
                if (asset != null) Destroy(asset);
            ownedRuntimeAssets.Clear();
            if (sceneryRoot != null) Destroy(sceneryRoot);
        }
    }
}
