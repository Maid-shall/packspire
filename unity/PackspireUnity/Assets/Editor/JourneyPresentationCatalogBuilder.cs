using UnityEditor;
using UnityEngine;

namespace Packspire.Editor
{
    internal static class JourneyPresentationCatalogBuilder
    {
        private const string CatalogFolder = "Assets/Resources/Data/Journey";
        private const string CatalogPath = CatalogFolder + "/JourneyPresentationCatalog.asset";
        private const string AshRoot =
            "Assets/Resources/Art/JourneyPrototype/Backgrounds/Ash/";

        [MenuItem("Tools/Packspire/Rebuild Journey Presentation Catalog")]
        private static void RebuildCatalog()
        {
            EnsureFolders();
            JourneyPresentationCatalog catalog =
                AssetDatabase.LoadAssetAtPath<JourneyPresentationCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<JourneyPresentationCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            catalog.schemaVersion = 1;
            catalog.biomes = new[]
            {
                BuildBiome(0, "ash", "灰市外縁"),
                BuildBiome(1, "drowned", "水没書庫"),
                BuildBiome(2, "black-bell", "黒鐘区画")
            };

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = catalog;
            Debug.Log($"[Journey Catalog] Rebuilt schema {catalog.schemaVersion}: {CatalogPath}", catalog);
        }

        private static JourneyPresentationCatalog.BiomeEntry BuildBiome(
            int biomeIndex,
            string id,
            string displayName)
        {
            Sprite skyDay = Load(AshRoot + "ash-sky-day-v1.png");
            Sprite skyDusk = Load(AshRoot + "ash-sky-dusk-v1.png");
            Sprite skyNight = Load(AshRoot + "ash-sky-night-v1.png");
            return new JourneyPresentationCatalog.BiomeEntry
            {
                id = id,
                displayName = displayName,
                skyDay = skyDay,
                skyDusk = skyDusk,
                skyNight = skyNight,
                farA = biomeIndex switch
                {
                    1 => Load("Assets/Resources/Art/JourneyPrototype/Backgrounds/Drowned/drowned-far-silhouette-v1.png"),
                    2 => Load("Assets/Resources/Art/JourneyPrototype/Backgrounds/BlackBell/black-bell-far-silhouette-v1.png"),
                    _ => Load(AshRoot + "ash-far-silhouette-a-v1.png")
                },
                farB = biomeIndex == 0
                    ? Load(AshRoot + "ash-far-silhouette-b-v1.png")
                    : null,
                farTint = biomeIndex switch
                {
                    1 => new Color(.72f, .78f, .82f, .52f),
                    2 => new Color(.74f, .66f, .68f, .52f),
                    _ => new Color(.72f, .68f, .75f, .52f)
                },
                roads = new[]
                {
                    BuildRoad(biomeIndex, JourneyWalkCyclePrototype.RoadProfile.Wide),
                    BuildRoad(biomeIndex, JourneyWalkCyclePrototype.RoadProfile.Standard),
                    BuildRoad(biomeIndex, JourneyWalkCyclePrototype.RoadProfile.Narrow)
                }
            };
        }

        private static JourneyPresentationCatalog.RoadEntry BuildRoad(
            int biomeIndex,
            JourneyWalkCyclePrototype.RoadProfile profile)
        {
            JourneyPresentationCatalog.RoadEntry road = profile switch
            {
                JourneyWalkCyclePrototype.RoadProfile.Wide =>
                    new JourneyPresentationCatalog.RoadEntry
                    {
                        profile = profile,
                        midA = Load(AshRoot + "ash-mid-wide-a-v1.png"),
                        streetBackA = Load(AshRoot + "ash-streetback-wide-a-v1.png"),
                        ground = Load(AshRoot + "ash-ground-wide-tile-v2.png"),
                        midTint = new Color(.72f, .72f, .74f, .84f),
                        streetBackTint = new Color(.82f, .82f, .84f, .92f),
                        groundTint = new Color(.92f, .91f, .9f, 1f),
                        midY = .15f,
                        streetBackY = .62f,
                        groundY = -1.95f,
                        roadsideY = -2.98f,
                        closeForegroundY = -5f,
                        landmarkY = -3.15f
                    },
                JourneyWalkCyclePrototype.RoadProfile.Narrow =>
                    new JourneyPresentationCatalog.RoadEntry
                    {
                        profile = profile,
                        midA = Load(AshRoot + "ash-mid-narrow-a-v1.png"),
                        streetBackA = Load(AshRoot + "ash-streetback-narrow-a-v1.png"),
                        // The old narrow tile depicts the side of a maintenance
                        // viaduct, not a readable walkable surface. Until a dedicated
                        // narrow street is authored, keep the route on clear stone.
                        ground = Load(AshRoot + "ash-ground-standard-tile-v3.png"),
                        midTint = new Color(.72f, .72f, .74f, .84f),
                        streetBackTint = new Color(.82f, .82f, .84f, .92f),
                        groundTint = new Color(.92f, .91f, .9f, 1f),
                        midY = .2f,
                        streetBackY = .55f,
                        groundY = -1.9f,
                        roadsideY = -2.98f,
                        closeForegroundY = -5f,
                        landmarkY = -3.2f
                    },
                _ => new JourneyPresentationCatalog.RoadEntry
                {
                    profile = profile,
                    midA = Load(AshRoot + "ash-mid-architecture-a-v1.png"),
                    midB = Load(AshRoot + "ash-mid-architecture-b-v1.png"),
                    streetBackA = Load(AshRoot + "ash-streetback-a-v1.png"),
                    streetBackB = Load(AshRoot + "ash-streetback-b-v1.png"),
                    ground = Load(AshRoot + "ash-ground-standard-tile-v3.png"),
                    midTint = new Color(.76f, .76f, .78f, 1f),
                    streetBackTint = new Color(.86f, .86f, .88f, 1f),
                    groundTint = new Color(.94f, .93f, .92f, 1f),
                    midY = 0f,
                    streetBackY = .48f,
                    groundY = -1.9f,
                    roadsideY = -2.98f,
                    closeForegroundY = -5f,
                    landmarkY = -3.2f
                }
            };

            if (biomeIndex == 1)
            {
                road.midA = Load(
                    "Assets/Resources/Art/JourneyPrototype/Parallax/journey-parallax-mid-drowned-v1.png");
                road.midB = null;
                road.midY = 0f;
            }
            else if (biomeIndex == 2)
            {
                road.midA = Load(
                    "Assets/Resources/Art/JourneyPrototype/Backgrounds/BlackBell/black-bell-mid-architecture-v1.png");
                road.midB = null;
                road.midY = 0f;
            }

            return road;
        }

        private static Sprite Load(string assetPath)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite == null) Debug.LogError($"[Journey Catalog] Missing Sprite: {assetPath}");
            return sprite;
        }

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources/Data"))
                AssetDatabase.CreateFolder("Assets/Resources", "Data");
            if (!AssetDatabase.IsValidFolder(CatalogFolder))
                AssetDatabase.CreateFolder("Assets/Resources/Data", "Journey");
        }
    }
}
