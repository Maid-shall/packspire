using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire
{
    public sealed partial class JourneyTravelGameplayPrototype
    {
        private const float PostBattleRecoveryDuration = 3f;

        private VisualElement rewardOverlay;
        private Label rewardTierLabel;
        private Label rewardDetailName;
        private Label rewardDetailText;
        private Label rewardSelectionNote;
        private Button rewardConfirmButton;
        private readonly Button[] rewardCandidateButtons = new Button[3];
        private readonly Label[] rewardCandidateKinds = new Label[3];
        private readonly Label[] rewardCandidateNames = new Label[3];
        private readonly Label[] rewardCandidateMetas = new Label[3];
        private JourneyBattleRewardOffer rewardOffer;
        private int selectedRewardIndex = -1;
        private bool postBattleRecoveryActive;

        private void BindJourneyRewardUi(VisualElement root)
        {
            rewardOverlay = root.Q<VisualElement>("journey-reward-overlay");
            rewardTierLabel = root.Q<Label>("journey-reward-tier");
            rewardDetailName = root.Q<Label>("journey-reward-detail-name");
            rewardDetailText = root.Q<Label>("journey-reward-detail-text");
            rewardSelectionNote = root.Q<Label>("journey-reward-selection-note");
            rewardConfirmButton = root.Q<Button>("journey-reward-confirm");

            for (int index = 0; index < rewardCandidateButtons.Length; index++)
            {
                int captured = index;
                rewardCandidateButtons[index] = root.Q<Button>($"journey-reward-candidate-{index}");
                rewardCandidateKinds[index] = root.Q<Label>($"journey-reward-kind-{index}");
                rewardCandidateNames[index] = root.Q<Label>($"journey-reward-name-{index}");
                rewardCandidateMetas[index] = root.Q<Label>($"journey-reward-meta-{index}");
                rewardCandidateButtons[index].clicked += () => SelectJourneyReward(captured);
            }

            rewardConfirmButton.clicked += ConfirmJourneyReward;
            rewardConfirmButton.SetEnabled(false);
        }

        private void ShowJourneyBattleReward()
        {
            if (!uiBound) BindUi();
            if (!uiBound || rewardCandidateButtons[0] == null)
            {
                Debug.LogError("Journey reward UI is not ready.", this);
                return;
            }

            JourneyBattleRewardTier tier = JourneyBattleRewardSystem.ResolveTier(
                encounterProfile.PopulationClass,
                arrivalNode?.risk ?? 0);

            if (usesLiveRun && PackspireGame.Instance != null)
                PackspireGame.Instance.UiResolveSeamlessJourneyBattleVictory();
            else
                ResolveSimulatedBattleForReward();

            rewardOffer = JourneyBattleRewardSystem.CreateOffer(run, tier);
            selectedRewardIndex = -1;
            BindJourneyRewardOffer();
            ClearBattleExitPresentation();
            screen.AddToClassList("reward--open");
            walker.SetJourneyWalking(false);
            ApplyWorldMotion();
        }

        private void ResolveSimulatedBattleForReward()
        {
            ExpeditionRoutePlan plan = ExpeditionProgressSystem.Ensure(run);
            bool generatedNodePending = arrivalExpeditionNode != null && plan.awaitingResolution;
            var outcome = new CourierLocationOutcome
            {
                success = true,
                performance = 2,
                message = $"{encounterProfile.ResolvedDisplayName}を退けた。"
            };
            bool resolved = generatedNodePending
                ? ExpeditionJourneySystem.ResolveCurrent(run, outcome, out _)
                : CourierRouteSystem.ResolveCurrent(run, outcome, out _);
            if (resolved) run.battlesWon++;
            RefreshPersistentUi();
        }

        private void BindJourneyRewardOffer()
        {
            if (rewardOffer == null || rewardOffer.candidates == null) return;
            rewardTierLabel.text = rewardOffer.tier switch
            {
                JourneyBattleRewardTier.Boss => "BOSS REWARD",
                JourneyBattleRewardTier.Elite => "ELITE REWARD",
                JourneyBattleRewardTier.Danger => "DANGER REWARD",
                _ => "BATTLE REWARD"
            };

            for (int index = 0; index < rewardCandidateButtons.Length; index++)
            {
                bool available = index < rewardOffer.candidates.Length;
                rewardCandidateButtons[index].EnableInClassList("is-hidden", !available);
                if (!available) continue;
                JourneyBattleRewardCandidate candidate = rewardOffer.candidates[index];
                rewardCandidateKinds[index].text = candidate.kind switch
                {
                    JourneyBattleRewardKind.Equipment => "EQUIPMENT",
                    JourneyBattleRewardKind.ExpeditionResource => "EXPEDITION",
                    _ => "CONSUMABLE"
                };
                rewardCandidateNames[index].text = candidate.name;
                rewardCandidateMetas[index].text = candidate.meta;
                rewardCandidateButtons[index].RemoveFromClassList("is-selected");
            }

            rewardDetailName.text = "戦果をひとつ選ぶ";
            rewardDetailText.text = "候補を選ぶと、ここに効果が表示されます。";
            rewardSelectionNote.text = "未選択";
            rewardConfirmButton.SetEnabled(false);
        }

        private void SelectJourneyReward(int index)
        {
            if (rewardOffer?.candidates == null ||
                index < 0 ||
                index >= rewardOffer.candidates.Length) return;

            selectedRewardIndex = index;
            for (int candidateIndex = 0;
                 candidateIndex < rewardCandidateButtons.Length;
                 candidateIndex++)
                rewardCandidateButtons[candidateIndex].EnableInClassList(
                    "is-selected",
                    candidateIndex == selectedRewardIndex);

            JourneyBattleRewardCandidate candidate = rewardOffer.candidates[index];
            rewardDetailName.text = candidate.name;
            rewardDetailText.text = candidate.description;
            rewardSelectionNote.text = candidate.meta;
            rewardConfirmButton.SetEnabled(true);
        }

        private void ConfirmJourneyReward()
        {
            if (rewardOffer?.candidates == null ||
                selectedRewardIndex < 0 ||
                selectedRewardIndex >= rewardOffer.candidates.Length) return;

            JourneyBattleRewardCandidate selected = rewardOffer.candidates[selectedRewardIndex];
            string message = JourneyBattleRewardSystem.Grant(run, selected);
            rewardOffer = null;
            selectedRewardIndex = -1;
            battle = null;
            screen.RemoveFromClassList("reward--open");
            RefreshPersistentUi();
            BeginPostBattleRecoveryTravel();
            ShowToast(message);
        }

        private void BeginPostBattleRecoveryTravel()
        {
            ClearBattleExitPresentation();
            arrivalExpeditionNode = null;
            arrivalNode = null;
            firstChoicePending = false;
            postBattleRecoveryActive = true;
            travelClock = 0f;
            travelDuration = PostBattleRecoveryDuration;
            miniGameOffered = false;
            routeProgress.value = 100f;
            SetPhase(Phase.Travel);
            phaseText.text = "MOVING / 戦闘地点を離脱";
            nextText.text = "戦利品を収め、次の判断地点へ";
        }

        private void UpdatePostBattleRecovery(float delta)
        {
            if (delta <= 0f) return;
            travelClock += delta;
            float progress = Mathf.Clamp01(travelClock / PostBattleRecoveryDuration);
            nextText.text = progress < .55f
                ? "戦利品を収め、戦闘地点を離脱中"
                : "次の判断地点を確認中";
            if (progress < 1f) return;

            postBattleRecoveryActive = false;
            ShowChoice();
            ShowToast("戦果を携え、旅程へ復帰しました。");
        }
    }
}
