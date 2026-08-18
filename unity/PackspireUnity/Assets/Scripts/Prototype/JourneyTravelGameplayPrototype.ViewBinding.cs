using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire
{
    /// <summary>UI Toolkit binding and event wiring for the journey view contract.</summary>
    public sealed partial class JourneyTravelGameplayPrototype
    {
        private void BindUi()
        {
            if (uiBound) return;
            if (document == null || document.rootVisualElement == null || document.rootVisualElement.panel == null)
                return;

            VisualElement root = document.rootVisualElement;
            JourneyViewContract.Validate(root);
            screen = root.Q<VisualElement>("journey-screen");
            if (screen == null) return;

            StyleSheet style = PackspireResources.Load<StyleSheet>(StyleResource);
            if (style != null) screen.styleSheets.Add(style);
            VisualElement battleHandTheme = root.Q<VisualElement>("journey-hand-scroll");
            StyleSheet battleStyle = PackspireResources.Load<StyleSheet>("UI/PackspireBattle");
            if (battleHandTheme != null && battleStyle != null)
                battleHandTheme.styleSheets.Add(battleStyle);

            transition = root.Q<VisualElement>("journey-transition");
            ledgerOverlay = root.Q<VisualElement>("journey-ledger");
            ledgerPhaseHost = root.Q<VisualElement>("journey-ledger-phases");
            sealHost = root.Q<VisualElement>("journey-seal-host");
            toast = root.Q<Label>("journey-toast");
            hpBar = root.Q<ProgressBar>("journey-hp");
            routeProgress = root.Q<ProgressBar>("journey-progress");
            battlePlayerHpFill = root.Q<VisualElement>("journey-battle-player-hp-fill");
            battlePlayerBlockBadge = root.Q<VisualElement>("journey-battle-player-block-badge");
            for (int enemyIndex = 0; enemyIndex < enemyHudSlots.Length; enemyIndex++)
            {
                enemyHudSlots[enemyIndex] = root.Q<VisualElement>($"journey-enemy-slot-{enemyIndex}");
                enemyNames[enemyIndex] = root.Q<Label>($"journey-enemy-name-{enemyIndex}");
                enemyHpFills[enemyIndex] = root.Q<VisualElement>($"journey-enemy-hp-fill-{enemyIndex}");
                enemyHpTexts[enemyIndex] = root.Q<Label>($"journey-enemy-hp-text-{enemyIndex}");
                enemyBlockBadges[enemyIndex] = root.Q<VisualElement>($"journey-enemy-block-badge-{enemyIndex}");
                enemyBlocks[enemyIndex] = root.Q<Label>($"journey-enemy-block-{enemyIndex}");
                enemyStatusHosts[enemyIndex] = root.Q<VisualElement>($"journey-enemy-statuses-{enemyIndex}");
            }
            miniGamePanel = root.Q<VisualElement>("journey-minigame");
            miniGameNeedle = root.Q<VisualElement>("journey-minigame-needle");
            miniGameHazard = root.Q<VisualElement>("journey-minigame-hazard");
            hpText = root.Q<Label>("journey-hp-text");
            cargoText = root.Q<Label>("journey-cargo");
            sealsText = root.Q<Label>("journey-seals");
            areaText = root.Q<Label>("journey-area");
            weatherText = root.Q<Label>("journey-weather");
            dayText = root.Q<Label>("journey-day");
            conditionText = root.Q<Label>("journey-condition");
            conditionNoteText = root.Q<Label>("journey-condition-note");
            phaseText = root.Q<Label>("journey-phase");
            nextText = root.Q<Label>("journey-next");
            ledgerSummary = root.Q<Label>("journey-ledger-summary");
            ledgerNodeTitle = root.Q<Label>("journey-ledger-node-title");
            ledgerNodeMeta = root.Q<Label>("journey-ledger-node-meta");
            ledgerNodeBody = root.Q<Label>("journey-ledger-node-body");
            sealTiming = root.Q<Label>("journey-seal-timing");
            sealDetailName = root.Q<Label>("journey-seal-detail-name");
            sealDetailEffect = root.Q<Label>("journey-seal-detail-effect");
            sealApply = root.Q<Button>("journey-seal-apply");
            choiceTitle = root.Q<Label>("journey-choice-title");
            choiceNote = root.Q<Label>("journey-choice-note");
            choiceA = root.Q<Button>("journey-choice-a");
            choiceB = root.Q<Button>("journey-choice-b");
            choiceAArt = root.Q<VisualElement>("journey-choice-a-art");
            choiceBArt = root.Q<VisualElement>("journey-choice-b-art");
            choiceAIndex = root.Q<Label>("journey-choice-a-index");
            choiceATitle = root.Q<Label>("journey-choice-a-title");
            choiceAFlavor = root.Q<Label>("journey-choice-a-flavor");
            choiceADays = root.Q<Label>("journey-choice-a-days");
            choiceARisk = root.Q<Label>("journey-choice-a-risk");
            choiceAEncounter = root.Q<Label>("journey-choice-a-encounter");
            choiceACargo = root.Q<Label>("journey-choice-a-cargo");
            choiceASeal = root.Q<Label>("journey-choice-a-seal");
            choiceBIndex = root.Q<Label>("journey-choice-b-index");
            choiceBTitle = root.Q<Label>("journey-choice-b-title");
            choiceBFlavor = root.Q<Label>("journey-choice-b-flavor");
            choiceBDays = root.Q<Label>("journey-choice-b-days");
            choiceBRisk = root.Q<Label>("journey-choice-b-risk");
            choiceBEncounter = root.Q<Label>("journey-choice-b-encounter");
            choiceBCargo = root.Q<Label>("journey-choice-b-cargo");
            choiceBSeal = root.Q<Label>("journey-choice-b-seal");
            eventEyebrow = root.Q<Label>("journey-event-eyebrow");
            eventTitle = root.Q<Label>("journey-event-title");
            eventText = root.Q<Label>("journey-event-text");
            eventA = root.Q<Button>("journey-event-a");
            eventB = root.Q<Button>("journey-event-b");
            miniGameEyebrow = root.Q<Label>("journey-minigame-eyebrow");
            miniGameTitle = root.Q<Label>("journey-minigame-title");
            miniGameNote = root.Q<Label>("journey-minigame-note");
            miniGameTimer = root.Q<Label>("journey-minigame-timer");
            miniGameReward = root.Q<Label>("journey-minigame-reward");
            miniGameRisk = root.Q<Label>("journey-minigame-risk");
            miniGameObjective = root.Q<Label>("journey-minigame-objective");
            miniGameStep = root.Q<Label>("journey-minigame-step");
            speech = root.Q<Label>("journey-speech");
            damagePopup = root.Q<Label>("journey-damage-popup");
            encounterBanner = root.Q<Label>("journey-encounter-banner");
            miniGameLeft = root.Q<Button>("journey-minigame-left");
            miniGameAction = root.Q<Button>("journey-minigame-action");
            miniGameRight = root.Q<Button>("journey-minigame-right");
            battleLog = root.Q<Label>("journey-battle-log");
            energyText = root.Q<Label>("journey-energy");
            battlePlayerHp = root.Q<Label>("journey-battle-player-hp");
            battleBlock = root.Q<Label>("journey-battle-player-block");
            playerStatusHost = root.Q<VisualElement>("journey-player-statuses");
            drawPileText = root.Q<Label>("journey-draw-pile");
            discardPileText = root.Q<Label>("journey-discard-pile");
            drawPileButton = root.Q<Button>("journey-draw-pile-button");
            discardPileButton = root.Q<Button>("journey-discard-pile-button");
            battleHandRoot = battleHandTheme?.Q<VisualElement>(className: "ps-journey__hand");
            consumablePresenter = new JourneyConsumablePresenter(
                root,
                UseJourneyConsumable,
                CanUseJourneyConsumable);
            BindRealtimeBattleUi(root);
            pileOverlay = root.Q<VisualElement>("journey-pile-overlay");
            pileTitle = root.Q<Label>("journey-pile-title");
            pileSummary = root.Q<Label>("journey-pile-summary");
            pileEmpty = root.Q<Label>("journey-pile-empty");
            pileCardHost = root.Q<VisualElement>("journey-pile-card-host");
            pileCloseButton = root.Q<Button>("journey-pile-close");
            pileScrim = root.Q<Button>("journey-pile-scrim");
            VisualElement pileCardTheme = root.Q<VisualElement>("journey-pile-card-scroll");
            if (pileCardTheme != null && battleStyle != null)
                pileCardTheme.styleSheets.Add(battleStyle);
            resultEyebrow = root.Q<Label>("journey-result-eyebrow");
            resultTitle = root.Q<Label>("journey-result-title");
            resultText = root.Q<Label>("journey-result-text");
            resultContinue = root.Q<Button>("journey-result-continue");
            speedButton = root.Q<Button>("journey-speed");
            pauseButton = root.Q<Button>("journey-pause");
            root.Q<Button>("journey-dev-menu").clicked += ReturnToDeveloperMenu;
            root.Q<Button>("journey-ledger-open").clicked += ToggleLedger;
            root.Q<Button>("journey-ledger-close").clicked += ToggleLedger;
            sealApply.clicked += ApplySelectedSeal;

            for (int index = 0; index < cardButtons.Length; index++)
            {
                int captured = index;
                cardButtons[index] = root.Q<Button>($"journey-card-{index}");
                cardButtons[index].text = string.Empty;
                cardButtons[index].AddToClassList("ps-battle-card");
                cardButtons[index].AddToClassList("ps-docket-card");
                cardButtons[index].AddToClassList("ps-docket-combat");
                battleCardViews[index] = new BattleCardView(cardButtons[index]);
                cardButtons[index].clicked += () => PlayCard(captured);
            }

            choiceA.clicked += () => SelectChoice(0);
            choiceB.clicked += () => SelectChoice(1);
            eventA.clicked += () => ResolveEvent(true);
            eventB.clicked += () => ResolveEvent(false);
            miniGameAction.clicked += MiniGameAction;
            miniGameLeft.clicked += () => NudgeBalance(-1f);
            miniGameRight.clicked += () => NudgeBalance(1f);
            drawPileButton.clicked += () => OpenPileOverlay(true);
            discardPileButton.clicked += () => OpenPileOverlay(false);
            pileCloseButton.clicked += ClosePileOverlay;
            pileScrim.clicked += ClosePileOverlay;
            resultContinue.clicked += ContinueAfterResult;
            speedButton.clicked += ToggleSpeed;
            pauseButton.clicked += TogglePause;

            Texture2D walkSheet = PackspireResources.Load<Texture2D>(WalkSheetResource);
            if (walkSheet != null)
            {
                Image portrait = root.Q<Image>("journey-portrait");
                portrait.image = walkSheet;
                portrait.uv = new Rect(0f, 0f, 1f / 6f, 1f);
            }

            RefreshPersistentUi();
            uiBound = true;
        }
    }
}
