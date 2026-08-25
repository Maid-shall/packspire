using UnityEngine;

namespace Packspire
{
    /// <summary>
    /// Presentation-only timing for the seamless journey-to-battle handoff.
    /// Combat time remains paused until this sequence completes.
    /// </summary>
    public static class JourneyBattleEntrySequence
    {
        public const float EnemyRevealDelay = .24f;
        public const float EnemyRevealDuration = .22f;
        public const float ApproachDuration = 1.04f;
        public const float EnemyObserveDuration =
            ApproachDuration - EnemyRevealDelay - EnemyRevealDuration;
        public const float EnemyArrivalDelay = ApproachDuration;
        public const float EnemyArrivalDuration = .72f;
        public const float EnemyHudDelay = 1.62f;
        public const float PlayerHudDelay = 1.88f;
        public const float ReelDelay = 2.14f;
        public const float CommandsDelay = 2.40f;
        public const float BattleStartDelay = 2.74f;
        public const float BattleStartEnd = 3.50f;
        public const float Duration = 3.68f;
        public const float EnemyStartOffset = 1.05f;

        public readonly struct Frame
        {
            public Frame(
                float worldMotionScale,
                float enemyOffset,
                float enemyAlpha,
                float enemyStageBlend,
                bool enemyVisible,
                bool battleStageActive,
                bool threatVisible,
                bool enemyHudVisible,
                bool playerHudVisible,
                bool reelVisible,
                bool commandsVisible,
                bool battleStartVisible,
                bool complete)
            {
                WorldMotionScale = worldMotionScale;
                EnemyOffset = enemyOffset;
                EnemyAlpha = enemyAlpha;
                EnemyStageBlend = enemyStageBlend;
                EnemyVisible = enemyVisible;
                BattleStageActive = battleStageActive;
                ThreatVisible = threatVisible;
                EnemyHudVisible = enemyHudVisible;
                PlayerHudVisible = playerHudVisible;
                ReelVisible = reelVisible;
                CommandsVisible = commandsVisible;
                BattleStartVisible = battleStartVisible;
                Complete = complete;
            }

            public float WorldMotionScale { get; }
            public float EnemyOffset { get; }
            public float EnemyAlpha { get; }
            public float EnemyStageBlend { get; }
            public bool EnemyVisible { get; }
            public bool BattleStageActive { get; }
            public bool ThreatVisible { get; }
            public bool EnemyHudVisible { get; }
            public bool PlayerHudVisible { get; }
            public bool ReelVisible { get; }
            public bool CommandsVisible { get; }
            public bool BattleStartVisible { get; }
            public bool Complete { get; }
        }

        public static Frame Sample(float elapsed, float startingWorldMotionScale)
        {
            float time = Mathf.Max(0f, elapsed);
            float approach = Smooth01(time / ApproachDuration);
            float reveal = Smooth01(
                (time - EnemyRevealDelay) / EnemyRevealDuration);
            float arrival = Smooth01(
                (time - EnemyArrivalDelay) / EnemyArrivalDuration);
            bool enemyVisible = time >= EnemyRevealDelay;

            return new Frame(
                Mathf.Lerp(Mathf.Max(0f, startingWorldMotionScale), 0f, approach),
                Mathf.Lerp(EnemyStartOffset, 0f, arrival),
                reveal,
                arrival,
                enemyVisible,
                time >= EnemyArrivalDelay,
                enemyVisible && time < EnemyHudDelay,
                time >= EnemyHudDelay,
                time >= PlayerHudDelay,
                time >= ReelDelay,
                time >= CommandsDelay,
                time >= BattleStartDelay && time < BattleStartEnd,
                time >= Duration);
        }

        private static float Smooth01(float value)
        {
            float t = Mathf.Clamp01(value);
            return t * t * (3f - 2f * t);
        }
    }
}
