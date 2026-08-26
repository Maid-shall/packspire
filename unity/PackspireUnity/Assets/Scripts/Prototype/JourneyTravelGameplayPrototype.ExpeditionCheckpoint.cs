using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Packspire
{
    public sealed partial class JourneyTravelGameplayPrototype
    {
        private Label resultSummary;
        private Button resultReturn;
        private ExpeditionCheckpointSummary activeCheckpoint;

        private void BindExpeditionCheckpointUi(VisualElement root)
        {
            resultSummary = root.Q<Label>("journey-result-summary");
            resultReturn = root.Q<Button>("journey-result-return");
            resultReturn.clicked += ReturnFromExpeditionCheckpoint;
            ClearExpeditionCheckpointResult();
        }

        private bool TryShowExpeditionCheckpoint()
        {
            ExpeditionCheckpointSummary summary = ExpeditionCheckpointSystem.Build(run);
            if (!summary.IsCheckpoint) return false;

            SaveJourneyStage(JourneyResumeStage.Checkpoint);
            bool canContinue = summary.CanContinue;
            ShowResult(
                canContinue ? "FLOOR ROUTE SECURED" : "EXPEDITION COMPLETE",
                canContinue
                    ? $"第{summary.clearedFloorNumber}層を踏破"
                    : "最深部踏破",
                canContinue
                    ? "帰還して戦利品を確定するか、次の階層へ進みます。"
                    : "三つの階層を踏破しました。全戦利品を持ち帰ります。",
                canContinue);

            activeCheckpoint = summary;
            resultSummary.text =
                $"経過 {summary.elapsedDays}日　経路 {summary.resolvedRouteNodeCount}地点　" +
                $"戦闘 {summary.battlesWon}回\n" +
                $"回収 {summary.collectedNewItemCount}　印使用 {summary.deliverySealsSpent}　" +
                $"HP {summary.currentHp}/{summary.maximumHp}\n" +
                $"保護済み戦利品 {summary.protectedNewItemCount}　" +
                $"未収納戦利品 {summary.exposedNewItemCount}";
            resultSummary.RemoveFromClassList("is-hidden");
            resultReturn.EnableInClassList("is-hidden", !canContinue);
            if (canContinue)
                resultContinue.text = $"第{summary.clearedFloorNumber + 1}層へ進む";
            return true;
        }

        private void ClearExpeditionCheckpointResult()
        {
            activeCheckpoint = null;
            if (resultSummary != null)
            {
                resultSummary.text = string.Empty;
                resultSummary.AddToClassList("is-hidden");
            }
            resultReturn?.AddToClassList("is-hidden");
        }

        private void ReturnFromExpeditionCheckpoint()
        {
            if (terminalActionLocked || activeCheckpoint?.CanContinue != true) return;
            terminalActionLocked = true;
            if (usesLiveRun && PackspireGame.Instance != null)
            {
                PackspireGame.Instance.UiFinishSeamlessJourney(
                    ExpeditionEndReason.Return);
                return;
            }

            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
