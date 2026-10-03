using UnityEngine;

namespace Packspire
{
    /// <summary>
    /// Owns developer-preview requests so the gameplay component contains no static
    /// menu queue or scene-to-scene handoff state.
    /// </summary>
    internal static class JourneyDeveloperPreviewController
    {
        private readonly struct BattleRequest
        {
            public readonly int EnemyCount;
            public readonly bool StartDefense;
            public readonly int HandSize;
            public readonly int PlayerBlock;
            public readonly string EncounterId;

            public BattleRequest(
                int enemyCount,
                bool startDefense,
                int handSize,
                int playerBlock,
                string encounterId)
            {
                EnemyCount = Mathf.Clamp(enemyCount, 1, 3);
                StartDefense = startDefense;
                HandSize = Mathf.Clamp(handSize, 0, 10);
                PlayerBlock = Mathf.Max(0, playerBlock);
                EncounterId = encounterId ?? string.Empty;
            }
        }

        private static BattleRequest queuedBattle;
        private static bool hasQueuedBattle;
        private static int queuedScenery;
        private static bool hasQueuedScenery;
        private static bool hasQueuedCombatLab;

        public static void QueueCombatLab()
        {
            hasQueuedBattle = false;
            hasQueuedScenery = false;
            hasQueuedCombatLab = true;
        }

        public static void QueueBattle(
            int enemyCount,
            bool startDefense,
            int handSize = 0,
            int playerBlock = 0,
            string encounterId = null)
        {
            hasQueuedScenery = false;
            queuedBattle = new BattleRequest(
                enemyCount,
                startDefense,
                handSize,
                playerBlock,
                encounterId);
            hasQueuedBattle = true;
        }

        public static void QueueEncounter(string encounterId)
        {
            QueueBattle(1, false, encounterId: encounterId);
        }

        public static void QueueScenery(int biomeIndex)
        {
            hasQueuedBattle = false;
            queuedScenery = Mathf.Clamp(biomeIndex, 0, 2);
            hasQueuedScenery = true;
        }

        public static void Clear()
        {
            queuedBattle = default;
            hasQueuedBattle = false;
            queuedScenery = 0;
            hasQueuedScenery = false;
            hasQueuedCombatLab = false;
        }

        public static bool TryApply(JourneyTravelGameplayPrototype target)
        {
            if (target == null) return false;
            if (hasQueuedCombatLab)
            {
                Clear();
                Time.timeScale = 1f;
                target.DevBeginCombatLab();
                return true;
            }
            if (hasQueuedScenery)
            {
                int scenery = queuedScenery;
                Clear();
                target.DevPreviewScenery(scenery);
                return true;
            }
            if (!hasQueuedBattle) return false;
            BattleRequest request = queuedBattle;
            Clear();
            // Developer previews must never inherit a frozen global clock from an
            // earlier QA session. The preview itself is paused through gameplay state
            // below so the transport button always reports the truth.
            Time.timeScale = 1f;
            target.DevBeginBattle(request.EncounterId);
            target.DevPreviewBattleEnemyCount(request.EnemyCount);
            if (request.HandSize > 0) target.DevPreviewBattleHandSize(request.HandSize);
            if (request.PlayerBlock > 0) target.DevPreviewBattleBlock(request.PlayerBlock);
            if (request.StartDefense) target.DevStartReactionPreview();
            target.DevSetPaused(true);
            return true;
        }
    }
}
