using UnityEngine;

namespace Packspire
{
    public enum JourneySceneryComposition
    {
        Open = 0,
        Standard = 1,
        Dense = 2
    }

    /// <summary>
    /// Shared visual registration for the seamless journey world. Background layers,
    /// roadside scenery and gameplay actors read from this catalog so a road-profile
    /// change cannot leave props floating above a differently aligned ground plate.
    /// </summary>
    internal static class JourneyPresentationConfig
    {
        private const string CatalogResource = "Data/Journey/JourneyPresentationCatalog";
        private const string AshSkyDay = "Art/JourneyPrototype/Backgrounds/Ash/ash-sky-day-v1";
        private const string AshSkyDusk = "Art/JourneyPrototype/Backgrounds/Ash/ash-sky-dusk-v1";
        private const string AshSkyNight = "Art/JourneyPrototype/Backgrounds/Ash/ash-sky-night-v1";

        private static JourneyPresentationCatalog catalog;
        private static bool catalogLoadAttempted;

        public const string BackgroundSortingLayer = "Journey Background";
        public const string RoadsideSortingLayer = "Journey Roadside";
        public const string ActorSortingLayer = "Journey Actors";
        public const string ForegroundSortingLayer = "Journey Foreground";
        public const string EffectSortingLayer = "Journey Effects";

        public const float BaseTravelSpeed = 1.25f;

        internal readonly struct RoadDefinition
        {
            public readonly Sprite MidSprite;
            public readonly Sprite StreetBackSprite;
            public readonly Sprite GroundSprite;
            public readonly Sprite MidVariationSprite;
            public readonly Sprite StreetBackVariationSprite;
            public readonly Color MidTint;
            public readonly Color StreetBackTint;
            public readonly Color GroundTint;
            public readonly float MidY;
            public readonly float StreetBackY;
            public readonly float GroundY;
            public readonly float RoadsideY;
            public readonly float CloseForegroundY;
            public readonly float LandmarkY;

            public RoadDefinition(
                Sprite midSprite,
                Sprite streetBackSprite,
                Sprite groundSprite,
                Sprite midVariationSprite,
                Sprite streetBackVariationSprite,
                Color midTint,
                Color streetBackTint,
                Color groundTint,
                float midY,
                float streetBackY,
                float groundY,
                float roadsideY,
                float closeForegroundY,
                float landmarkY)
            {
                MidSprite = midSprite;
                StreetBackSprite = streetBackSprite;
                GroundSprite = groundSprite;
                MidVariationSprite = midVariationSprite;
                StreetBackVariationSprite = streetBackVariationSprite;
                MidTint = midTint;
                StreetBackTint = streetBackTint;
                GroundTint = groundTint;
                MidY = midY;
                StreetBackY = streetBackY;
                GroundY = groundY;
                RoadsideY = roadsideY;
                CloseForegroundY = closeForegroundY;
                LandmarkY = landmarkY;
            }
        }

        internal readonly struct BiomeDefinition
        {
            public readonly Sprite SkyDaySprite;
            public readonly Sprite SkyDuskSprite;
            public readonly Sprite SkyNightSprite;
            public readonly Sprite FarSprite;
            public readonly Sprite FarVariationSprite;
            public readonly Color FarTint;

            public BiomeDefinition(
                Sprite skyDaySprite,
                Sprite skyDuskSprite,
                Sprite skyNightSprite,
                Sprite farSprite,
                Sprite farVariationSprite,
                Color farTint)
            {
                SkyDaySprite = skyDaySprite;
                SkyDuskSprite = skyDuskSprite;
                SkyNightSprite = skyNightSprite;
                FarSprite = farSprite;
                FarVariationSprite = farVariationSprite;
                FarTint = farTint;
            }
        }

        public static JourneyPresentationCatalog Catalog
        {
            get
            {
                if (!catalogLoadAttempted)
                {
                    catalogLoadAttempted = true;
                    catalog = PackspireResources.Load<JourneyPresentationCatalog>(CatalogResource);
                }
                return catalog;
            }
        }

        public static BiomeDefinition GetBiome(int biomeIndex)
        {
            JourneyPresentationCatalog.BiomeEntry entry = Catalog?.GetBiome(biomeIndex);
            if (entry != null && entry.farA != null)
            {
                return new BiomeDefinition(
                    entry.skyDay,
                    entry.skyDusk,
                    entry.skyNight,
                    entry.farA,
                    entry.farB,
                    entry.farTint);
            }

            Sprite skyDay = LoadSprite(AshSkyDay);
            Sprite skyDusk = LoadSprite(AshSkyDusk);
            Sprite skyNight = LoadSprite(AshSkyNight);
            return Mathf.Clamp(biomeIndex, 0, 2) switch
            {
                1 => new BiomeDefinition(
                    skyDay,
                    skyDusk,
                    skyNight,
                    LoadSprite("Art/JourneyPrototype/Backgrounds/Drowned/drowned-far-silhouette-v1"),
                    null,
                    new Color(.72f, .78f, .82f, .52f)),
                2 => new BiomeDefinition(
                    skyDay,
                    skyDusk,
                    skyNight,
                    LoadSprite("Art/JourneyPrototype/Backgrounds/BlackBell/black-bell-far-silhouette-v1"),
                    null,
                    new Color(.74f, .66f, .68f, .52f)),
                _ => new BiomeDefinition(
                    skyDay,
                    skyDusk,
                    skyNight,
                    LoadSprite("Art/JourneyPrototype/Backgrounds/Ash/ash-far-silhouette-a-v1"),
                    LoadSprite("Art/JourneyPrototype/Backgrounds/Ash/ash-far-silhouette-b-v1"),
                    new Color(.72f, .68f, .75f, .52f))
            };
        }

        public static RoadDefinition GetRoad(JourneyWalkCyclePrototype.RoadProfile profile)
        {
            return GetRoad(0, profile);
        }

        public static RoadDefinition GetRoad(
            int biomeIndex,
            JourneyWalkCyclePrototype.RoadProfile profile)
        {
            JourneyPresentationCatalog.RoadEntry entry = Catalog?.GetBiome(biomeIndex)?.FindRoad(profile);
            if (entry != null && entry.midA != null && entry.streetBackA != null && entry.ground != null)
            {
                return new RoadDefinition(
                    entry.midA,
                    entry.streetBackA,
                    entry.ground,
                    entry.midB,
                    entry.streetBackB,
                    entry.midTint,
                    entry.streetBackTint,
                    entry.groundTint,
                    entry.midY,
                    entry.streetBackY,
                    entry.groundY,
                    entry.roadsideY,
                    entry.closeForegroundY,
                    entry.landmarkY);
            }

            RoadDefinition baseRoad = GetFallbackRoad(profile);
            Sprite biomeMid = Mathf.Clamp(biomeIndex, 0, 2) switch
            {
                1 => LoadSprite("Art/JourneyPrototype/Parallax/journey-parallax-mid-drowned-v1"),
                2 => LoadSprite("Art/JourneyPrototype/Backgrounds/BlackBell/black-bell-mid-architecture-v1"),
                _ => null
            };
            if (biomeMid == null) return baseRoad;
            return new RoadDefinition(
                biomeMid,
                baseRoad.StreetBackSprite,
                baseRoad.GroundSprite,
                null,
                baseRoad.StreetBackVariationSprite,
                baseRoad.MidTint,
                baseRoad.StreetBackTint,
                baseRoad.GroundTint,
                0f,
                baseRoad.StreetBackY,
                baseRoad.GroundY,
                baseRoad.RoadsideY,
                baseRoad.CloseForegroundY,
                baseRoad.LandmarkY);
        }

        private static RoadDefinition GetFallbackRoad(JourneyWalkCyclePrototype.RoadProfile profile)
        {
            return profile switch
            {
                JourneyWalkCyclePrototype.RoadProfile.Wide => new RoadDefinition(
                    LoadSprite("Art/JourneyPrototype/Backgrounds/Ash/ash-mid-wide-a-v1"),
                    LoadSprite("Art/JourneyPrototype/Backgrounds/Ash/ash-streetback-wide-a-v1"),
                    LoadSprite("Art/JourneyPrototype/Backgrounds/Ash/ash-ground-wide-tile-v2"),
                    null,
                    null,
                    new Color(.72f, .72f, .74f, .84f),
                    new Color(.82f, .82f, .84f, .92f),
                    new Color(.92f, .91f, .9f, 1f),
                    .15f,
                    .62f,
                    -1.95f,
                    -2.98f,
                    -5f,
                    -3.15f),
                JourneyWalkCyclePrototype.RoadProfile.Narrow => new RoadDefinition(
                    LoadSprite("Art/JourneyPrototype/Backgrounds/Ash/ash-mid-narrow-a-v1"),
                    LoadSprite("Art/JourneyPrototype/Backgrounds/Ash/ash-streetback-narrow-a-v1"),
                    LoadSprite("Art/JourneyPrototype/Backgrounds/Ash/ash-ground-standard-tile-v3"),
                    null,
                    null,
                    new Color(.72f, .72f, .74f, .84f),
                    new Color(.82f, .82f, .84f, .92f),
                    new Color(.92f, .91f, .9f, 1f),
                    .2f,
                    .55f,
                    -1.9f,
                    -2.98f,
                    -5f,
                    -3.2f),
                _ => new RoadDefinition(
                    LoadSprite("Art/JourneyPrototype/Backgrounds/Ash/ash-mid-architecture-a-v1"),
                    LoadSprite("Art/JourneyPrototype/Backgrounds/Ash/ash-streetback-a-v1"),
                    LoadSprite("Art/JourneyPrototype/Backgrounds/Ash/ash-ground-standard-tile-v3"),
                    LoadSprite("Art/JourneyPrototype/Backgrounds/Ash/ash-mid-architecture-b-v1"),
                    LoadSprite("Art/JourneyPrototype/Backgrounds/Ash/ash-streetback-b-v1"),
                    new Color(.76f, .76f, .78f, 1f),
                    new Color(.86f, .86f, .88f, 1f),
                    new Color(.94f, .93f, .92f, 1f),
                    0f,
                    .48f,
                    -1.9f,
                    -2.98f,
                    -5f,
                    -3.2f)
            };
        }

        private static Sprite LoadSprite(string resourcePath)
        {
            return string.IsNullOrEmpty(resourcePath) ? null : PackspireResources.Load<Sprite>(resourcePath);
        }

        /// <summary>
        /// Route data owns the semantic road width. Presentation code translates it
        /// once here so UI, scenery and the scrolling world cannot disagree.
        /// </summary>
        public static JourneyWalkCyclePrototype.RoadProfile GetRoadProfile(CourierRouteNodeDef node)
        {
            return node?.roadWidth switch
            {
                CourierRoadWidth.Wide => JourneyWalkCyclePrototype.RoadProfile.Wide,
                CourierRoadWidth.Narrow => JourneyWalkCyclePrototype.RoadProfile.Narrow,
                _ => JourneyWalkCyclePrototype.RoadProfile.Standard
            };
        }

        public static JourneySceneryComposition GetComposition(
            JourneyWalkCyclePrototype.RoadProfile profile)
        {
            return profile switch
            {
                JourneyWalkCyclePrototype.RoadProfile.Wide => JourneySceneryComposition.Open,
                JourneyWalkCyclePrototype.RoadProfile.Narrow => JourneySceneryComposition.Dense,
                _ => JourneySceneryComposition.Standard
            };
        }

        public static JourneyWalkCyclePrototype.RoadProfile GetRoadProfile(
            JourneySceneryComposition composition)
        {
            return composition switch
            {
                JourneySceneryComposition.Open => JourneyWalkCyclePrototype.RoadProfile.Wide,
                JourneySceneryComposition.Dense => JourneyWalkCyclePrototype.RoadProfile.Narrow,
                _ => JourneyWalkCyclePrototype.RoadProfile.Standard
            };
        }

        public static string GetCompositionLabel(JourneySceneryComposition composition)
        {
            return composition switch
            {
                JourneySceneryComposition.Open => "開放区画",
                JourneySceneryComposition.Dense => "密集区画",
                _ => "標準区画"
            };
        }

        public static string GetRoadWidthLabel(CourierRouteNodeDef node)
        {
            return GetRoadProfile(node) switch
            {
                JourneyWalkCyclePrototype.RoadProfile.Wide => "広路",
                JourneyWalkCyclePrototype.RoadProfile.Narrow => "狭路",
                _ => "通常路"
            };
        }

        public static void Sort(SpriteRenderer renderer, string layerName, int order = 0)
        {
            if (renderer == null) return;
            renderer.sortingLayerName = layerName;
            renderer.sortingOrder = order;
        }
    }
}
