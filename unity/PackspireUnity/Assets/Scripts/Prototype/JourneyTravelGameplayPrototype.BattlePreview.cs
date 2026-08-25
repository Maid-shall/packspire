using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire
{
    public sealed partial class JourneyTravelGameplayPrototype
    {
        private void SetBattlePreviewEnemyCount(int count)
        {
            int visibleCount = Mathf.Clamp(count, 1, enemyHudSlots.Length);
            bool layoutPreview = visibleCount > 1;
            screen.EnableInClassList("battle--enemy-count-2", visibleCount == 2);
            screen.EnableInClassList("battle--enemy-count-3", visibleCount == 3);
            screen.EnableInClassList("battle--layout-preview", layoutPreview);
            battleInputLocked = encounterIntroActive || layoutPreview;
            ApplyBattleActorLayout(visibleCount);

            for (int enemyIndex = 0; enemyIndex < enemyHudSlots.Length; enemyIndex++)
            {
                bool visible = enemyIndex < visibleCount;
                enemyHudSlots[enemyIndex].EnableInClassList("is-hidden", !visible);
                if (!visible && enemyIndex > 0)
                {
                    RefreshBlockBadge(enemyBlockBadges[enemyIndex], enemyBlocks[enemyIndex], 0);
                    RefreshStatusHost(enemyStatusHosts[enemyIndex], null);
                }
                if (enemyIndex <= 0) continue;

                SpriteRenderer renderer = battlePreviewEnemyRenderers[enemyIndex - 1];
                if (renderer != null) renderer.enabled = visible;
                SpriteRenderer shadow = battlePreviewEnemyShadowRenderers[enemyIndex - 1];
                if (shadow != null) shadow.enabled = visible;
            }

            if (visibleCount > 1) PopulateSecondEnemyPreview();
            if (visibleCount > 2) PopulateThirdEnemyPreview();
            RefreshBattleUi();
        }

        private void PopulateSecondEnemyPreview()
        {
            enemyNames[1].text = "灰路の追跡者";
            RefreshHpMeter(enemyHpFills[1], enemyHpTexts[1], 18, 24);
            RefreshBlockBadge(enemyBlockBadges[1], enemyBlocks[1], 3);
            RefreshStatusHost(enemyStatusHosts[1], new List<StatusState>
            {
                new StatusState { type = "burn", amount = 2, duration = 2 }
            });
        }

        private void PopulateThirdEnemyPreview()
        {
            enemyNames[2].text = "鐘楼の射手";
            RefreshHpMeter(enemyHpFills[2], enemyHpTexts[2], 14, 18);
            RefreshBlockBadge(enemyBlockBadges[2], enemyBlocks[2], 0);
            RefreshStatusHost(enemyStatusHosts[2], new List<StatusState>
            {
                new StatusState { type = "weak", amount = 1, duration = 1 },
                new StatusState { type = "poison", amount = 3, duration = 0 }
            });
        }

        /// <summary>
        /// Shows additional actors and HUDs for layout inspection only. It does not
        /// add those enemies to battle resolution.
        /// </summary>
        public void DevPreviewBattleEnemyCount(int count)
        {
            if (!EnsureBattlePreviewReady()) return;
            SetBattlePreviewEnemyCount(count);
            battleLog.text = count >= 3
                ? "3体編成の構図確認中。カード操作は停止し、人物と体力表示の重なりだけを検査する。"
                : "単体敵レイアウト確認。";
        }

        public void DevPreviewBattleBlock(int value)
        {
            if (!EnsureBattlePreviewReady()) return;
            run.block = Mathf.Max(0, value);
            RefreshBattleUi();
            battleLog.text = value > 0
                ? $"防御表示確認：青い防御膜と防御印を表示中（{value}）。"
                : "防御表示を解除。";
        }

        public void DevPreviewBattleHandSize(int count)
        {
            if (!EnsureBattlePreviewReady()) return;
            int requested = Mathf.Clamp(count, 1, cardButtons.Length);
            if (run.deck == null || run.deck.Count == 0) return;

            while (run.hand.Count < requested)
            {
                CardInstance source = run.deck[run.hand.Count % run.deck.Count];
                run.hand.Add(source.Clone());
            }
            while (run.hand.Count > requested)
                run.hand.RemoveAt(run.hand.Count - 1);

            battleInputLocked = true;
            screen.AddToClassList("battle--layout-preview");
            RefreshBattleUi();
            battleLog.text = $"手札{requested}枚の構図確認中。カード同士の重なり、読める範囲、左右操作欄との干渉を検査する。";
        }

        private bool EnsureBattlePreviewReady()
        {
            if (!uiBound) BindUi();
            if (!uiBound) return false;
            if (phase != Phase.Battle) BeginBattle();
            return true;
        }
    }
}
