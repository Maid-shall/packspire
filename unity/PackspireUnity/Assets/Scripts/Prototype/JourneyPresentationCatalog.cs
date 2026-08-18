using System;
using UnityEngine;

namespace Packspire
{
    [CreateAssetMenu(
        fileName = "JourneyPresentationCatalog",
        menuName = "Packspire/Journey/Presentation Catalog")]
    public sealed class JourneyPresentationCatalog : ScriptableObject
    {
        [Serializable]
        public sealed class RoadEntry
        {
            public JourneyWalkCyclePrototype.RoadProfile profile;
            public Sprite midA;
            public Sprite midB;
            public Sprite streetBackA;
            public Sprite streetBackB;
            public Sprite ground;
            public Color midTint = Color.white;
            public Color streetBackTint = Color.white;
            public Color groundTint = Color.white;
            public float midY;
            public float streetBackY;
            public float groundY;
            public float roadsideY;
            public float closeForegroundY;
            public float landmarkY;
        }

        [Serializable]
        public sealed class BiomeEntry
        {
            public string id;
            public string displayName;
            public Sprite skyDay;
            public Sprite skyDusk;
            public Sprite skyNight;
            public Sprite farA;
            public Sprite farB;
            public Color farTint = Color.white;
            public RoadEntry[] roads = Array.Empty<RoadEntry>();

            public RoadEntry FindRoad(JourneyWalkCyclePrototype.RoadProfile profile)
            {
                if (roads == null) return null;
                foreach (RoadEntry road in roads)
                    if (road != null && road.profile == profile) return road;
                return null;
            }
        }

        [Min(1)] public int schemaVersion = 1;
        public BiomeEntry[] biomes = Array.Empty<BiomeEntry>();

        public BiomeEntry GetBiome(int biomeIndex)
        {
            if (biomes == null || biomes.Length == 0) return null;
            return biomes[Mathf.Clamp(biomeIndex, 0, biomes.Length - 1)];
        }
    }
}
