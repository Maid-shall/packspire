using UnityEngine.UIElements;

namespace Packspire
{
    public sealed partial class JourneyTravelGameplayPrototype
    {
        private static readonly UxmlViewContract JourneyViewContract = BuildJourneyViewContract();

        private static UxmlViewContract BuildJourneyViewContract()
        {
            var contract = new UxmlViewContract("PackspireJourneyCompleteView")
                .Require<VisualElement>("journey-screen")
                .Require<VisualElement>("journey-hand-scroll")
                .Require<VisualElement>("journey-transition")
                .Require<VisualElement>("journey-ledger")
                .Require<VisualElement>("journey-ledger-phases")
                .Require<VisualElement>("journey-seal-host")
                .Require<Label>("journey-toast")
                .Require<ProgressBar>("journey-hp")
                .Require<ProgressBar>("journey-progress")
                .Require<VisualElement>("journey-battle-player-hp-fill")
                .Require<VisualElement>("journey-battle-player-block-badge")
                .Require<VisualElement>("journey-minigame")
                .Require<VisualElement>("journey-minigame-needle")
                .Require<VisualElement>("journey-minigame-hazard")
                .Require<Label>("journey-hp-text")
                .Require<Label>("journey-cargo")
                .Require<Label>("journey-seals")
                .Require<Label>("journey-area")
                .Require<Label>("journey-weather")
                .Require<Label>("journey-day")
                .Require<Label>("journey-condition")
                .Require<Label>("journey-condition-note")
                .Require<Label>("journey-phase")
                .Require<Label>("journey-next")
                .Require<Label>("journey-ledger-summary")
                .Require<Label>("journey-ledger-node-title")
                .Require<Label>("journey-ledger-node-meta")
                .Require<Label>("journey-ledger-node-body")
                .Require<Label>("journey-seal-timing")
                .Require<Label>("journey-seal-detail-name")
                .Require<Label>("journey-seal-detail-effect")
                .Require<Button>("journey-seal-apply")
                .Require<Label>("journey-choice-title")
                .Require<Label>("journey-choice-note")
                .Require<Button>("journey-choice-a")
                .Require<Button>("journey-choice-b")
                .Require<VisualElement>("journey-choice-a-art")
                .Require<VisualElement>("journey-choice-b-art")
                .Require<Label>("journey-choice-a-index")
                .Require<Label>("journey-choice-a-title")
                .Require<Label>("journey-choice-a-flavor")
                .Require<Label>("journey-choice-a-days")
                .Require<Label>("journey-choice-a-risk")
                .Require<Label>("journey-choice-a-encounter")
                .Require<Label>("journey-choice-a-cargo")
                .Require<Label>("journey-choice-a-seal")
                .Require<Label>("journey-choice-b-index")
                .Require<Label>("journey-choice-b-title")
                .Require<Label>("journey-choice-b-flavor")
                .Require<Label>("journey-choice-b-days")
                .Require<Label>("journey-choice-b-risk")
                .Require<Label>("journey-choice-b-encounter")
                .Require<Label>("journey-choice-b-cargo")
                .Require<Label>("journey-choice-b-seal")
                .Require<Label>("journey-event-eyebrow")
                .Require<Label>("journey-event-title")
                .Require<Label>("journey-event-text")
                .Require<Button>("journey-event-a")
                .Require<Button>("journey-event-b")
                .Require<Label>("journey-minigame-eyebrow")
                .Require<Label>("journey-minigame-title")
                .Require<Label>("journey-minigame-note")
                .Require<Label>("journey-minigame-timer")
                .Require<Label>("journey-minigame-reward")
                .Require<Label>("journey-minigame-risk")
                .Require<Label>("journey-minigame-objective")
                .Require<Label>("journey-minigame-step")
                .Require<Button>("journey-minigame-left")
                .Require<Button>("journey-minigame-action")
                .Require<Button>("journey-minigame-right")
                .Require<Label>("journey-speech")
                .Require<Label>("journey-damage-popup")
                .Require<Label>("journey-encounter-banner")
                .Require<Label>("journey-battle-log")
                .Require<Label>("journey-energy")
                .Require<Label>("journey-battle-player-hp")
                .Require<Label>("journey-battle-player-block")
                .Require<VisualElement>("journey-player-statuses")
                .Require<Label>("journey-draw-pile")
                .Require<Label>("journey-discard-pile")
                .Require<Button>("journey-draw-pile-button")
                .Require<Button>("journey-discard-pile-button")
                .Require<VisualElement>("journey-consumables")
                .Require<VisualElement>("journey-consumable-detail")
                .Require<Label>("journey-consumable-detail-name")
                .Require<Label>("journey-consumable-detail-effect")
                .Require<VisualElement>("journey-threat-reel")
                .Require<VisualElement>("journey-reel-chain-track")
                .Require<VisualElement>("journey-reel-slots")
                .Require<VisualElement>("journey-reel-now")
                .Require<Button>("journey-battle-pause")
                .Require<Button>("journey-battle-speed")
                .Require<VisualElement>("journey-pile-overlay")
                .Require<Label>("journey-pile-title")
                .Require<Label>("journey-pile-summary")
                .Require<Label>("journey-pile-empty")
                .Require<VisualElement>("journey-pile-card-host")
                .Require<Button>("journey-pile-close")
                .Require<Button>("journey-pile-scrim")
                .Require<VisualElement>("journey-pile-card-scroll")
                .Require<Label>("journey-result-eyebrow")
                .Require<Label>("journey-result-title")
                .Require<Label>("journey-result-text")
                .Require<Button>("journey-result-continue")
                .Require<Button>("journey-speed")
                .Require<Button>("journey-pause")
                .Require<Button>("journey-dev-menu")
                .Require<Button>("journey-ledger-open")
                .Require<Button>("journey-ledger-close")
                .Require<Image>("journey-portrait");

            for (int index = 0; index < 3; index++)
            {
                contract
                    .Require<VisualElement>($"journey-enemy-slot-{index}")
                    .Require<Label>($"journey-enemy-name-{index}")
                    .Require<VisualElement>($"journey-enemy-hp-fill-{index}")
                    .Require<Label>($"journey-enemy-hp-text-{index}")
                    .Require<VisualElement>($"journey-enemy-block-badge-{index}")
                    .Require<Label>($"journey-enemy-block-{index}")
                    .Require<VisualElement>($"journey-enemy-statuses-{index}");
            }

            for (int index = 0; index < 10; index++)
                contract.Require<Button>($"journey-card-{index}");

            for (int index = 0; index < JourneyConsumablePresenter.SlotCount; index++)
            {
                contract
                    .Require<Button>($"journey-consumable-{index}")
                    .Require<VisualElement>($"journey-consumable-icon-{index}")
                    .Require<Label>($"journey-consumable-count-{index}");
            }

            for (int index = 0; index < JourneyBattleReelPresenter.ThreatSlotCount; index++)
                contract.Require<VisualElement>($"journey-threat-slot-{index}");

            for (int index = 0; index < JourneyBattleReelPresenter.SupplySlotCount; index++)
                contract.Require<VisualElement>($"journey-supply-slot-{index}");

            return contract;
        }
    }
}
