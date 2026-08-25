using UnityEngine;

namespace Packspire
{
    /// <summary>
    /// Presentation-only timing for the normal-enemy defeat handoff.
    /// Rewards and route resolution remain owned by PackspireGame.
    /// </summary>
    public static class JourneyBattleExitSequence
    {
        public const float EnemyDefeatDuration = .68f;
        public const float RouteClearDelay = .34f;
        public const float FogDelay = .86f;
        public const float Duration = 1.18f;

        public readonly struct Frame
        {
            public Frame(
                float enemyOffsetX,
                float enemyOffsetY,
                float enemyRotation,
                float enemyScale,
                float enemyAlpha,
                float shadowAlpha,
                bool routeClearVisible,
                bool fogVisible,
                bool complete)
            {
                EnemyOffsetX = enemyOffsetX;
                EnemyOffsetY = enemyOffsetY;
                EnemyRotation = enemyRotation;
                EnemyScale = enemyScale;
                EnemyAlpha = enemyAlpha;
                ShadowAlpha = shadowAlpha;
                RouteClearVisible = routeClearVisible;
                FogVisible = fogVisible;
                Complete = complete;
            }

            public float EnemyOffsetX { get; }
            public float EnemyOffsetY { get; }
            public float EnemyRotation { get; }
            public float EnemyScale { get; }
            public float EnemyAlpha { get; }
            public float ShadowAlpha { get; }
            public bool RouteClearVisible { get; }
            public bool FogVisible { get; }
            public bool Complete { get; }
        }

        public static Frame Sample(float elapsed)
        {
            float time = Mathf.Max(0f, elapsed);
            float defeat = Smooth01(time / EnemyDefeatDuration);
            float fade = Smooth01((time - .18f) / .42f);
            float shadowFade = Smooth01((time - .08f) / .42f);
            return new Frame(
                Mathf.Lerp(0f, .62f, defeat),
                Mathf.Lerp(0f, -.13f, defeat),
                Mathf.Lerp(0f, -6f, defeat),
                Mathf.Lerp(1f, .95f, defeat),
                1f - fade,
                1f - shadowFade,
                time >= RouteClearDelay,
                time >= FogDelay,
                time >= Duration);
        }

        private static float Smooth01(float value)
        {
            float t = Mathf.Clamp01(value);
            return t * t * (3f - 2f * t);
        }
    }
}
