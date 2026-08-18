using System;
using System.Collections.Generic;
using UnityEngine;
using RoadProfile = Packspire.JourneyWalkCyclePrototype.RoadProfile;

namespace Packspire
{
    /// <summary>
    /// Owns the seamless journey's layered background, road profiles and time-of-day
    /// presentation. Character motion remains in JourneyWalkCyclePrototype.
    /// </summary>
    internal sealed class JourneyParallaxWorld : IDisposable
    {
        private const string ParallaxFrontResource = "Art/JourneyPrototype/Parallax/journey-parallax-front-v1";

        private sealed class ScrollingLayer
        {
            public Transform Root;
            public readonly List<SpriteRenderer> Renderers = new List<SpriteRenderer>(3);
            public readonly Sprite[] BiomeSprites = new Sprite[3];
            public readonly Sprite[] BiomeVariationSprites = new Sprite[3];
            public readonly Color[] BiomeTints = new Color[3];
            public readonly float[] BiomeBaseY = new float[3];
            public Sprite PrimarySprite;
            public Sprite AppliedPrimarySprite;
            public Sprite AppliedVariationSprite;
            public float AppliedBaseY = float.NaN;
            public Color PrimaryTint = Color.white;
            public float SpeedFactor;
            public float TileWidth;
            public float Offset;
            public float BaseY;
            public int CycleTileCount = 2;
        }

        private sealed class RoadLayerSet
        {
            public GameObject Root;
            public readonly List<ScrollingLayer> Layers = new List<ScrollingLayer>(3);
        }

        private readonly List<UnityEngine.Object> ownedRuntimeAssets = new List<UnityEngine.Object>();
        private readonly List<ScrollingLayer> scrollingLayers = new List<ScrollingLayer>(4);
        private readonly Dictionary<RoadProfile, RoadLayerSet> roadLayerSets =
            new Dictionary<RoadProfile, RoadLayerSet>();
        private readonly Sprite[] skyDaySprites = new Sprite[3];
        private readonly Sprite[] skyDuskSprites = new Sprite[3];
        private readonly Sprite[] skyNightSprites = new Sprite[3];

        private GameObject parallaxRoot;
        private SpriteRenderer originalBackgroundRenderer;
        private SpriteRenderer atmosphereRenderer;
        private SpriteRenderer skyDayRenderer;
        private SpriteRenderer skyDuskRenderer;
        private SpriteRenderer skyNightRenderer;
        private int journeyBiome;
        private float journeyProgress;
        private float stageLift;
        private RoadProfile roadProfile = RoadProfile.Standard;
        private JourneySceneryComposition sceneryComposition = JourneySceneryComposition.Standard;

        public event Action<RoadProfile> RoadProfileChanged;
        public RoadProfile CurrentRoadProfile => roadProfile;
        public JourneySceneryComposition CurrentSceneryComposition => sceneryComposition;
        public float JourneyProgress => journeyProgress;

        public JourneyParallaxWorld()
        {
            BuildParallaxWorld();
        }

        private void BuildParallaxWorld()
        {
            GameObject originalBackground = GameObject.Find("Journey City Background");
            if (originalBackground != null)
            {
                originalBackgroundRenderer = originalBackground.GetComponent<SpriteRenderer>();
            }

            parallaxRoot = new GameObject("Fixed Courier Parallax World");
            // Each Ash City image shares the same full-frame registration. Keeping the
            // layers on that common canvas prevents the detached ground/prop look that
            // appeared when unrelated crops were scaled independently.
            BuildSkyLayers();
            JourneyPresentationConfig.BiomeDefinition ashBiome = JourneyPresentationConfig.GetBiome(0);
            ScrollingLayer farLayer = BuildScrollingLayer(
                "Far Skyline",
                ashBiome.FarSprite,
                -50,
                0.08f,
                0f,
                null,
                ashBiome.FarVariationSprite,
                ashBiome.FarTint);
            for (int biomeIndex = 1; biomeIndex <= 2; biomeIndex++)
            {
                JourneyPresentationConfig.BiomeDefinition biome = JourneyPresentationConfig.GetBiome(biomeIndex);
                SetBiomeSprite(
                    farLayer,
                    biomeIndex,
                    biome.FarSprite,
                    null,
                    biome.FarVariationSprite,
                    biome.FarTint);
            }
            EnsureRoadLayerSet(roadProfile);
            BuildAtmosphereLayer();
            BuildScrollingLayer(
                "Front Props",
                PackspireResources.Load<Sprite>(ParallaxFrontResource),
                30,
                1.35f);
            ApplyRoadProfileVisibility();
        }

        private void EnsureRoadLayerSet(RoadProfile profile)
        {
            if (roadLayerSets.ContainsKey(profile)) return;
            BuildRoadLayerSet(profile, JourneyPresentationConfig.GetRoad(0, profile));
        }

        private void BuildRoadLayerSet(
            RoadProfile profile,
            JourneyPresentationConfig.RoadDefinition definition)
        {
            GameObject root = new GameObject($"{profile} Road Profile");
            root.transform.SetParent(parallaxRoot.transform, false);
            RoadLayerSet set = new RoadLayerSet { Root = root };
            roadLayerSets[profile] = set;

            ScrollingLayer midLayer = BuildScrollingLayer(
                $"{profile} Mid Architecture",
                definition.MidSprite,
                -40,
                0.22f,
                definition.MidY,
                root.transform,
                definition.MidVariationSprite,
                definition.MidTint);
            ScrollingLayer streetBackLayer = BuildScrollingLayer(
                $"{profile} Street Back",
                definition.StreetBackSprite,
                -30,
                0.48f,
                definition.StreetBackY,
                root.transform,
                definition.StreetBackVariationSprite,
                definition.StreetBackTint);
            ScrollingLayer groundLayer = BuildScrollingLayer(
                $"{profile} Ground",
                definition.GroundSprite,
                -10,
                1f,
                definition.GroundY,
                root.transform,
                null,
                definition.GroundTint);
            for (int biomeIndex = 1; biomeIndex <= 2; biomeIndex++)
            {
                JourneyPresentationConfig.RoadDefinition biomeRoad =
                    JourneyPresentationConfig.GetRoad(biomeIndex, profile);
                SetBiomeSprite(
                    midLayer,
                    biomeIndex,
                    biomeRoad.MidSprite,
                    biomeRoad.MidY,
                    biomeRoad.MidVariationSprite,
                    biomeRoad.MidTint);
                SetBiomeSprite(
                    streetBackLayer,
                    biomeIndex,
                    biomeRoad.StreetBackSprite,
                    biomeRoad.StreetBackY,
                    biomeRoad.StreetBackVariationSprite,
                    biomeRoad.StreetBackTint);
                SetBiomeSprite(
                    groundLayer,
                    biomeIndex,
                    biomeRoad.GroundSprite,
                    biomeRoad.GroundY,
                    null,
                    biomeRoad.GroundTint);
            }
            set.Layers.Add(midLayer);
            set.Layers.Add(streetBackLayer);
            set.Layers.Add(groundLayer);
        }

        private ScrollingLayer BuildScrollingLayer(
            string layerName,
            Sprite sprite,
            int sortingOrder,
            float speedFactor,
            float verticalOffset = 0f,
            Transform parent = null,
            Sprite variationSprite = null,
            Color? primaryTint = null)
        {
            if (sprite == null)
            {
                Debug.LogError($"Journey parallax layer Sprite is missing: {layerName}");
                return null;
            }
            GameObject layerObject = new GameObject(layerName);
            layerObject.transform.SetParent(parent != null ? parent : parallaxRoot.transform, false);
            float cameraHeight = Camera.main != null ? Camera.main.orthographicSize * 2f : 10.125f;
            float cameraWidth = Camera.main != null ? cameraHeight * Camera.main.aspect : 18f;
            float layerScale = Mathf.Max(
                cameraHeight / sprite.bounds.size.y,
                cameraWidth / sprite.bounds.size.x);
            float tileWidth = sprite.bounds.size.x * layerScale;

            Color resolvedTint = primaryTint ?? Color.white;
            ScrollingLayer scrollingLayer = new ScrollingLayer
            {
                Root = layerObject.transform,
                PrimarySprite = sprite,
                PrimaryTint = resolvedTint,
                SpeedFactor = speedFactor,
                TileWidth = tileWidth,
                Offset = 0f,
                BaseY = verticalOffset,
                CycleTileCount = 2
            };
            scrollingLayer.BiomeSprites[0] = sprite;
            scrollingLayer.BiomeVariationSprites[0] = variationSprite;
            for (int biome = 0; biome < scrollingLayer.BiomeBaseY.Length; biome++)
            {
                scrollingLayer.BiomeBaseY[biome] = verticalOffset;
                scrollingLayer.BiomeTints[biome] = resolvedTint;
            }

            layerObject.transform.localPosition = new Vector3(0f, verticalOffset, 0f);

            // A variation needs a complete A-B cycle on both sides of the camera.
            // Three tiles were enough for a single repeated image, but left no tile
            // to cover the right edge once a two-tile A-B cycle approached wrapping.
            int lastTileIndex = 2;
            for (int tileIndex = -1; tileIndex <= lastTileIndex; tileIndex++)
            {
                GameObject tile = new GameObject($"{layerName} Tile {tileIndex + 2}");
                tile.transform.SetParent(layerObject.transform, false);
                tile.transform.localPosition = new Vector3(tileIndex * tileWidth, 0f, 0f);
                tile.transform.localScale = new Vector3(layerScale, layerScale, 1f);
                SpriteRenderer renderer = tile.AddComponent<SpriteRenderer>();
                renderer.sprite = variationSprite != null && tileIndex % 2 == 0
                    ? variationSprite
                    : sprite;
                renderer.flipX = variationSprite == null &&
                    AllowsMirroredFallback(layerName) &&
                    tileIndex % 2 == 0;
                JourneyPresentationConfig.Sort(
                    renderer,
                    layerName == "Front Props"
                        ? JourneyPresentationConfig.ForegroundSortingLayer
                        : JourneyPresentationConfig.BackgroundSortingLayer,
                    sortingOrder);
                scrollingLayer.Renderers.Add(renderer);
            }

            scrollingLayers.Add(scrollingLayer);
            return scrollingLayer;
        }

        private void SetBiomeSprite(
            ScrollingLayer layer,
            int biome,
            Sprite sprite,
            float? baseY = null,
            Sprite variationSprite = null,
            Color? tint = null)
        {
            if (layer == null || biome < 0 || biome >= layer.BiomeSprites.Length || sprite == null)
                return;

            layer.BiomeSprites[biome] = sprite;
            layer.BiomeVariationSprites[biome] = variationSprite;
            if (baseY.HasValue) layer.BiomeBaseY[biome] = baseY.Value;
            if (tint.HasValue) layer.BiomeTints[biome] = tint.Value;
        }

        private void BuildSkyLayers()
        {
            GameObject skyRoot = new GameObject("Journey Time Sky");
            skyRoot.transform.SetParent(parallaxRoot.transform, false);
            for (int biomeIndex = 0; biomeIndex < 3; biomeIndex++)
            {
                JourneyPresentationConfig.BiomeDefinition biome = JourneyPresentationConfig.GetBiome(biomeIndex);
                skyDaySprites[biomeIndex] = biome.SkyDaySprite;
                skyDuskSprites[biomeIndex] = biome.SkyDuskSprite;
                skyNightSprites[biomeIndex] = biome.SkyNightSprite;
            }
            skyDayRenderer = BuildFixedSky("Day Sky", skyDaySprites[0], -72, skyRoot.transform);
            skyDuskRenderer = BuildFixedSky("Dusk Sky", skyDuskSprites[0], -71, skyRoot.transform);
            skyNightRenderer = BuildFixedSky("Night Sky", skyNightSprites[0], -70, skyRoot.transform);
            ApplySkyBlend();
        }

        private SpriteRenderer BuildFixedSky(string name, Sprite sprite, int sortingOrder, Transform parent)
        {
            if (sprite == null)
            {
                Debug.LogError($"Journey sky Sprite is missing: {name}");
                return null;
            }
            GameObject sky = new GameObject(name);
            sky.transform.SetParent(parent, false);
            float cameraHeight = Camera.main != null ? Camera.main.orthographicSize * 2f : 10.125f;
            float cameraWidth = Camera.main != null ? cameraHeight * Camera.main.aspect : 18f;
            float scale = Mathf.Max(cameraHeight / sprite.bounds.size.y, cameraWidth / sprite.bounds.size.x);
            sky.transform.localScale = new Vector3(scale, scale, 1f);
            SpriteRenderer renderer = sky.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            JourneyPresentationConfig.Sort(renderer, JourneyPresentationConfig.BackgroundSortingLayer, sortingOrder);
            return renderer;
        }


        public void SetBuiltInForegroundVisible(bool visible)
        {
            if (parallaxRoot == null)
            {
                return;
            }

            Transform foreground = parallaxRoot.transform.Find("Front Props");
            if (foreground != null)
            {
                foreground.gameObject.SetActive(visible);
            }
        }

        public void SetJourneyBiome(int biomeIndex)
        {
            journeyBiome = Mathf.Max(0, biomeIndex);
            ApplyLayerPresentation();
        }

        public void SetJourneyProgress(float normalizedProgress)
        {
            journeyProgress = Mathf.Clamp01(normalizedProgress);
            ApplyLayerPresentation();
        }

        public void SetRoadProfile(RoadProfile profile)
        {
            if (roadProfile == profile)
            {
                return;
            }

            roadProfile = profile;
            ApplyRoadProfileVisibility();
            RoadProfileChanged?.Invoke(profile);
        }

        public void SetSceneryComposition(JourneySceneryComposition composition)
        {
            if (sceneryComposition == composition) return;
            sceneryComposition = composition;
            ApplyLayerPresentation();
        }

        private void ApplyRoadProfileVisibility()
        {
            EnsureRoadLayerSet(roadProfile);
            foreach (KeyValuePair<RoadProfile, RoadLayerSet> entry in roadLayerSets)
            {
                if (entry.Value?.Root != null)
                {
                    entry.Value.Root.SetActive(entry.Key == roadProfile);
                }
            }
        }

        private void ApplyBattleStageComposition()
        {
            // The battle crop raises the playable street, not only the courier. Keeping
            // the authored Mid/StreetBack/Ground group together preserves its shared
            // baseline and prevents the actors from appearing to stand in the sky.
            foreach (KeyValuePair<RoadProfile, RoadLayerSet> entry in roadLayerSets)
            {
                GameObject roadRoot = entry.Value?.Root;
                if (roadRoot == null)
                {
                    continue;
                }

                Vector3 localPosition = roadRoot.transform.localPosition;
                localPosition.y = stageLift;
                roadRoot.transform.localPosition = localPosition;
            }
        }

        private void ApplyLayerPresentation()
        {
            int activeBiome = Mathf.Clamp(journeyBiome, 0, 2);
            Color timeTint = activeBiome switch
            {
                1 => Color.Lerp(new Color(.84f, .93f, .98f, 1f), new Color(.66f, .78f, .9f, 1f), journeyProgress * .55f),
                2 => Color.Lerp(new Color(.93f, .82f, .78f, 1f), new Color(.69f, .56f, .62f, 1f), journeyProgress * .68f),
                _ => Color.Lerp(Color.white, new Color(.93f, .84f, .82f, 1f), journeyProgress * .42f)
            };
            for (int layerIndex = 0; layerIndex < scrollingLayers.Count; layerIndex++)
            {
                ScrollingLayer layer = scrollingLayers[layerIndex];
                Sprite biomeSprite = layer.BiomeSprites[activeBiome];
                Sprite primary = biomeSprite != null ? biomeSprite : layer.PrimarySprite;
                Sprite variation = layer.BiomeVariationSprites[activeBiome];
                ApplyLayerGeometry(layer, primary, variation, activeBiome);
                Color biomeGrade = activeBiome switch
                {
                    1 => new Color(.9f, .97f, 1f, 1f),
                    2 => new Color(1f, .9f, .88f, 1f),
                    _ => Color.white
                };
                Color tint = Multiply(layer.BiomeTints[activeBiome], biomeGrade);
                for (int rendererIndex = 0; rendererIndex < layer.Renderers.Count; rendererIndex++)
                {
                    int tileIndex = rendererIndex - 1;
                    Sprite sprite = variation != null && tileIndex % 2 == 0 ? variation : primary;
                    layer.Renderers[rendererIndex].sprite = sprite;
                    layer.Renderers[rendererIndex].flipX = variation == null &&
                        AllowsMirroredFallback(layer.Root.name) &&
                        tileIndex % 2 == 0;
                    Color layerColor = Multiply(tint, timeTint);
                    layerColor.a *= CompositionAlpha(layer);
                    layer.Renderers[rendererIndex].color = layerColor;
                }
            }

            ApplySkyBlend();

            if (atmosphereRenderer != null)
            {
                Color atmosphere = journeyBiome switch
                {
                    1 => new Color(.18f, .48f, .53f, Mathf.Lerp(.06f, .15f, journeyProgress)),
                    2 => new Color(.4f, .08f, .1f, Mathf.Lerp(.1f, .22f, journeyProgress)),
                    _ => new Color(.58f, .18f, .11f, Mathf.Lerp(.02f, .12f, journeyProgress))
                };
                atmosphereRenderer.color = atmosphere;
            }
        }

        private float CompositionAlpha(ScrollingLayer layer)
        {
            if (layer?.Root == null) return 1f;
            string layerName = layer.Root.name;
            if (layerName.Contains("Ground", StringComparison.Ordinal)) return 1f;

            bool midArchitecture = layerName.Contains("Mid Architecture", StringComparison.Ordinal);
            bool streetBack = layerName.Contains("Street Back", StringComparison.Ordinal);
            if (!midArchitecture && !streetBack) return 1f;

            return sceneryComposition switch
            {
                JourneySceneryComposition.Open => midArchitecture ? .58f : .7f,
                JourneySceneryComposition.Dense => 1f,
                _ => midArchitecture ? .88f : .94f
            };
        }

        private static bool AllowsMirroredFallback(string layerName)
        {
            // Mirroring is safe for distant silhouettes. Architecture, road furniture
            // and ground retain their authored light direction until a real B variant
            // is supplied by the biome catalog.
            return string.Equals(layerName, "Far Skyline", StringComparison.Ordinal);
        }

        private void ApplyLayerGeometry(ScrollingLayer layer, Sprite primary, Sprite variation, int activeBiome)
        {
            float baseY = layer != null
                ? layer.BiomeBaseY[Mathf.Clamp(activeBiome, 0, layer.BiomeBaseY.Length - 1)]
                : 0f;
            if (layer == null || primary == null ||
                (layer.AppliedPrimarySprite == primary &&
                 layer.AppliedVariationSprite == variation &&
                 Mathf.Approximately(layer.AppliedBaseY, baseY)))
                return;

            float previousCycleWidth = Mathf.Max(.001f, layer.TileWidth * layer.CycleTileCount);
            float normalizedOffset = -Mathf.Repeat(-layer.Offset, previousCycleWidth) / previousCycleWidth;
            float cameraHeight = Camera.main != null ? Camera.main.orthographicSize * 2f : 10.125f;
            float cameraWidth = Camera.main != null ? cameraHeight * Camera.main.aspect : 18f;
            float layerScale = Mathf.Max(
                cameraHeight / primary.bounds.size.y,
                cameraWidth / primary.bounds.size.x);

            layer.TileWidth = primary.bounds.size.x * layerScale;
            layer.CycleTileCount = 2;
            float cycleWidth = layer.TileWidth * layer.CycleTileCount;
            layer.Offset = normalizedOffset * cycleWidth;
            layer.AppliedPrimarySprite = primary;
            layer.AppliedVariationSprite = variation;
            layer.AppliedBaseY = baseY;
            layer.BaseY = baseY;

            for (int rendererIndex = 0; rendererIndex < layer.Renderers.Count; rendererIndex++)
            {
                Transform tile = layer.Renderers[rendererIndex].transform;
                tile.localPosition = new Vector3((rendererIndex - 1) * layer.TileWidth, 0f, 0f);
                tile.localScale = new Vector3(layerScale, layerScale, 1f);
            }
            layer.Root.localPosition = new Vector3(layer.Offset, layer.BaseY, 0f);
        }

        private void ApplySkyBlend()
        {
            float phase = Mathf.Clamp01(journeyProgress) * 2f;
            float dusk = phase <= 1f ? phase : 2f - phase;
            float night = Mathf.Clamp01(phase - 1f);
            float day = 1f - Mathf.Clamp01(phase);
            Color biomeTint = journeyBiome switch
            {
                1 => new Color(.72f, .88f, .96f, 1f),
                2 => new Color(.82f, .62f, .65f, 1f),
                _ => Color.white
            };
            int activeBiome = Mathf.Clamp(journeyBiome, 0, 2);
            if (skyDayRenderer != null && skyDaySprites[activeBiome] != null)
                skyDayRenderer.sprite = skyDaySprites[activeBiome];
            if (skyDuskRenderer != null && skyDuskSprites[activeBiome] != null)
                skyDuskRenderer.sprite = skyDuskSprites[activeBiome];
            if (skyNightRenderer != null && skyNightSprites[activeBiome] != null)
                skyNightRenderer.sprite = skyNightSprites[activeBiome];
            if (skyDayRenderer != null) skyDayRenderer.color = new Color(biomeTint.r, biomeTint.g, biomeTint.b, day);
            if (skyDuskRenderer != null) skyDuskRenderer.color = new Color(biomeTint.r, biomeTint.g, biomeTint.b, dusk);
            if (skyNightRenderer != null) skyNightRenderer.color = new Color(biomeTint.r, biomeTint.g, biomeTint.b, night);
        }

        private static Color Multiply(Color left, Color right) => new Color(
            left.r * right.r,
            left.g * right.g,
            left.b * right.b,
            left.a * right.a);

        private void BuildAtmosphereLayer()
        {
            GameObject veil = new GameObject("Journey Atmosphere Veil");
            veil.transform.SetParent(parallaxRoot.transform, false);
            atmosphereRenderer = veil.AddComponent<SpriteRenderer>();
            atmosphereRenderer.sprite = CreateSolidSprite();
            JourneyPresentationConfig.Sort(atmosphereRenderer, JourneyPresentationConfig.BackgroundSortingLayer, -20);
            veil.transform.localScale = new Vector3(22f, 12f, 1f);
            atmosphereRenderer.color = new Color(.58f, .18f, .11f, .02f);
        }

        private Sprite CreateSolidSprite()
        {
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
            {
                name = "journey-atmosphere-veil",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };
            texture.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
            texture.Apply(false, false);
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), new Vector2(.5f, .5f), 1f);
            ownedRuntimeAssets.Add(sprite);
            ownedRuntimeAssets.Add(texture);
            return sprite;
        }

        private void AdvanceParallax(float distance)
        {
            for (int index = 0; index < scrollingLayers.Count; index++)
            {
                ScrollingLayer layer = scrollingLayers[index];
                layer.Offset -= distance * layer.SpeedFactor;
                float cycleWidth = layer.TileWidth * layer.CycleTileCount;
                if (layer.Offset <= -cycleWidth)
                {
                    layer.Offset += cycleWidth;
                }

                layer.Root.localPosition = new Vector3(layer.Offset, layer.BaseY, 0f);
            }
        }

        public void SetStageLift(float value)
        {
            stageLift = value;
            ApplyBattleStageComposition();
        }

        public void SetVisible(bool visible)
        {
            if (parallaxRoot != null) parallaxRoot.SetActive(visible);
            if (originalBackgroundRenderer != null) originalBackgroundRenderer.enabled = !visible;
        }

        public void Advance(float distance)
        {
            if (distance > 0f) AdvanceParallax(distance);
        }

        public void Dispose()
        {
            if (originalBackgroundRenderer != null) originalBackgroundRenderer.enabled = true;
            if (parallaxRoot != null)
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(parallaxRoot);
                else UnityEngine.Object.DestroyImmediate(parallaxRoot);
            }

            for (int index = 0; index < ownedRuntimeAssets.Count; index++)
            {
                UnityEngine.Object asset = ownedRuntimeAssets[index];
                if (asset == null) continue;
                if (Application.isPlaying) UnityEngine.Object.Destroy(asset);
                else UnityEngine.Object.DestroyImmediate(asset);
            }
            ownedRuntimeAssets.Clear();
            scrollingLayers.Clear();
            roadLayerSets.Clear();
        }
    }
}
