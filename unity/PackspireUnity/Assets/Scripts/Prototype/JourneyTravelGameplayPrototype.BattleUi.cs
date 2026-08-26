using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire
{
    public sealed partial class JourneyTravelGameplayPrototype
    {
        private void RefreshBattleUi()
        {
            using var performanceScope = PackspirePerformance.JourneyBattleRefresh.Auto();
            if (battle == null) return;
            RefreshHpMeter(enemyHpFills[0], enemyHpTexts[0], battle.enemyHp, battle.enemyMaxHp);
            RefreshBlockBadge(enemyBlockBadges[0], enemyBlocks[0], battle.enemyBlock);
            RefreshStatusHost(enemyStatusHosts[0], battle.enemyStatuses);
            int maximumEnergy = realtimeBattleActive
                ? realtimeBattle.MaximumEnergy
                : PackspireContent.Data.balance.baseEnergy;
            energyText.text = $"{run.energy} / {maximumEnergy}";
            RefreshHpMeter(battlePlayerHpFill, battlePlayerHp, run.hp, run.maxHp);
            RefreshBlockBadge(battlePlayerBlockBadge, battleBlock, run.block);
            RefreshStatusHost(playerStatusHost, run.statuses);
            AppendRealtimeConsumableStatuses();
            drawPileText.text = run.draw.Count.ToString();
            discardPileText.text = run.discard.Count.ToString();
            bool commandAvailable = !GameplayInputBlocked && !battleInputLocked;
            drawPileButton.SetEnabled(commandAvailable);
            discardPileButton.SetEnabled(commandAvailable);
            consumablePresenter?.Refresh(run, commandAvailable);
            int visibleCardCount = Mathf.Min(run.hand.Count, cardButtons.Length);
            battleHandOrder.Clear();
            for (int index = 0; index < cardButtons.Length; index++)
            {
                Button button = cardButtons[index];
                bool present = index < run.hand.Count;
                button.EnableInClassList("is-hidden", !present);
                if (!present) continue;
                CardInstance card = run.hand[index];
                bool affordable = !card.unplayable && card.cost <= run.energy;
                battleCardViews[index].Bind(BuildJourneyBattleCardModel(card, affordable));
                float depth = BattleHandFanLayout.Apply(button, index, visibleCardCount);
                battleHandOrder.Add((button, depth));
                button.SetEnabled(affordable && commandAvailable);
            }
            if (battleHandRoot != null)
            {
                battleHandOrder.Sort(static (left, right) => right.depth.CompareTo(left.depth));
                foreach (var slot in battleHandOrder)
                    battleHandRoot.Add(slot.button);
            }
            RefreshPersistentUi();
        }

        private static BattleCardViewModel BuildJourneyBattleCardModel(
            CardInstance card,
            bool affordable)
        {
            return new BattleCardViewModel(
                card.name,
                card.cost.ToString(),
                JourneyRealtimeCardText(card),
                JourneyBattleCardKind(card),
                affordable ? string.Empty : "LOW EN",
                PackspireUiFoundation.BattleCardArtwork(card.id),
                affordable);
        }

        private static string JourneyRealtimeCardText(CardInstance card)
        {
            if (card == null || string.IsNullOrEmpty(card.text) ||
                card.effects == null || card.effects.Count == 0)
                return card?.text ?? string.Empty;

            string text = card.text;
            foreach (EffectSpec effect in card.effects)
            {
                if (effect == null || effect.duration <= 0) continue;
                double seconds =
                    effect.duration * RealtimeCombatRules.StatusSecondsPerDurationUnit;
                text = text.Replace(
                    $"{effect.duration}ターン",
                    $"{seconds:0.#}秒");
            }
            return text;
        }

        private static string JourneyBattleCardKind(CardInstance card)
        {
            if (card.type == CardType.Attack || card.damage > 0) return "attack";
            if (card.block > 0 || card.heal > 0) return "defense";
            return "technique";
        }

        private static void RefreshHpMeter(VisualElement fill, Label text, int current, int maximum)
        {
            int safeMaximum = Mathf.Max(1, maximum);
            int safeCurrent = Mathf.Clamp(current, 0, safeMaximum);
            if (fill != null)
            {
                fill.style.width = new Length(100f * safeCurrent / safeMaximum, LengthUnit.Percent);
            }
            if (text != null)
            {
                text.text = $"{safeCurrent} / {safeMaximum}";
            }
        }

        private static void RefreshBlockBadge(VisualElement badge, Label count, int value)
        {
            if (badge == null || count == null) return;
            badge.EnableInClassList("is-hidden", value <= 0);
            count.text = Mathf.Max(0, value).ToString();
        }

        private static void RefreshStatusHost(VisualElement host, List<StatusState> statuses)
        {
            if (host == null) return;
            host.Clear();
            if (statuses == null) return;
            int visibleCount = Mathf.Min(5, statuses.Count);
            for (int index = 0; index < visibleCount; index++)
            {
                StatusState status = statuses[index];
                var definition = ContentDatabase.Status(status.type);
                var chip = new VisualElement { pickingMode = PickingMode.Position };
                chip.AddToClassList("ps-journey__status-chip");
                chip.EnableInClassList("status--debuff", definition != null && definition.kind == "debuff");
                string description = definition != null
                    ? definition.description.Replace(
                        "ターン終了時",
                        $"{RealtimeCombatRules.StatusPulseSeconds:0.#}秒ごと")
                    : status.type;
                string remaining = status.remainingSeconds > 0d
                    ? $"\n残り {status.remainingSeconds:0.0}秒"
                    : string.Empty;
                chip.tooltip = definition != null
                    ? $"{definition.name}\n{description}{remaining}"
                    : $"{status.type}{remaining}";

                var icon = new Label(definition?.icon ?? "◆") { pickingMode = PickingMode.Ignore };
                icon.AddToClassList("ps-journey__status-icon");
                chip.Add(icon);

                string countText = status.remainingSeconds > 0d
                    ? $"{status.amount} / {System.Math.Ceiling(status.remainingSeconds):0}s"
                    : status.amount.ToString();
                var count = new Label(countText) { pickingMode = PickingMode.Ignore };
                count.AddToClassList("ps-journey__status-count");
                chip.Add(count);
                host.Add(chip);
            }
        }

        private void OpenPileOverlay(bool drawPile)
        {
            if (GameplayInputBlocked || phase != Phase.Battle ||
                battle == null || battleInputLocked) return;
            List<CardInstance> cards = drawPile ? run.draw : run.discard;
            consumablePresenter?.ClearHover();
            pileOverlayOpen = true;
            pileOverlay?.AddToClassList("pile-overlay--open");
            screen?.AddToClassList("battle--pile-open");
            pileTitle.text = drawPile ? "山札" : "捨札";
            pileSummary.text = drawPile
                ? $"{cards.Count}枚・次に引くカードから表示"
                : $"{cards.Count}枚・新しく捨てたカードから表示";
            pileCardHost.Clear();

            for (int index = cards.Count - 1; index >= 0; index--)
            {
                CardInstance card = cards[index];
                var preview = new Button
                {
                    focusable = false,
                    tooltip = card.name
                };
                preview.AddToClassList("ps-journey__pile-card");
                preview.AddToClassList("ps-battle-card-standard");
                new BattleCardView(preview).Bind(BuildJourneyBattleCardModel(card, true));
                pileCardHost.Add(preview);
            }

            pileEmpty.EnableInClassList("is-hidden", cards.Count > 0);
            RefreshBattleUi();
        }

        private void ClosePileOverlay()
        {
            pileOverlayOpen = false;
            pileOverlay?.RemoveFromClassList("pile-overlay--open");
            screen?.RemoveFromClassList("battle--pile-open");
            pileCardHost?.Clear();
            if (phase == Phase.Battle && battle != null) RefreshBattleUi();
        }

    }
}
