using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Packspire
{
    public sealed partial class JourneyTravelGameplayPrototype
    {
        private Label resultSummary;
        private ExpeditionCheckpointSummary activeCheckpoint;
        private VisualElement checkpointRoot;
        private Label checkpointTitle;
        private Button checkpointReturn;
        private Button checkpointContinue;
        private bool checkpointExitActive;

        private void BindExpeditionCheckpointUi(VisualElement root)
        {
            resultSummary = root.Q<Label>("journey-result-summary");
            checkpointRoot = root.Q<VisualElement>("journey-checkpoint");
            checkpointTitle = root.Q<Label>("journey-checkpoint-title");
            checkpointReturn = root.Q<Button>("journey-checkpoint-return");
            checkpointContinue = root.Q<Button>("journey-checkpoint-continue");
            checkpointReturn.clicked += ReturnFromExpeditionCheckpoint;
            checkpointContinue.clicked += ContinueFromExpeditionCheckpoint;
            ClearExpeditionCheckpointResult();
        }

        private bool TryShowExpeditionCheckpoint()
        {
            ExpeditionCheckpointSummary summary = ExpeditionCheckpointSystem.Build(run);
            if (!summary.IsCheckpoint) return false;

            SaveJourneyStage(JourneyResumeStage.Checkpoint);
            activeCheckpoint = summary;
            if (summary.CanContinue)
            {
                ShowExpeditionCheckpoint(summary);
                return true;
            }

            ShowResult(
                "EXPEDITION COMPLETE",
                "遠征完了",
                $"全戦利品を確定　保護 {summary.protectedNewItemCount}　未収納 {summary.exposedNewItemCount}",
                false);
            return true;
        }

        private void ShowExpeditionCheckpoint(ExpeditionCheckpointSummary summary)
        {
            SetPhase(Phase.Checkpoint);
            terminalActionLocked = false;
            checkpointExitActive = false;
            screen.RemoveFromClassList("checkpoint--leaving");
            checkpointTitle.text =
                $"第{summary.clearedFloorNumber}層　区画突破";
            checkpointReturn.SetEnabled(true);
            checkpointContinue.SetEnabled(true);
            checkpointRoot.BringToFront();
            checkpointReturn.Focus();
        }

        private void ClearExpeditionCheckpointResult()
        {
            activeCheckpoint = null;
            if (resultSummary != null)
            {
                resultSummary.text = string.Empty;
                resultSummary.AddToClassList("is-hidden");
            }
            checkpointExitActive = false;
            screen?.RemoveFromClassList("checkpoint--leaving");
        }

        private void ContinueFromExpeditionCheckpoint()
        {
            if (terminalActionLocked || activeCheckpoint?.CanContinue != true)
                return;
            terminalActionLocked = true;
            StartCoroutine(ContinueFromExpeditionCheckpointRoutine());
        }

        private IEnumerator ContinueFromExpeditionCheckpointRoutine()
        {
            checkpointExitActive = true;
            screen.AddToClassList("checkpoint--leaving");
            checkpointReturn.SetEnabled(false);
            checkpointContinue.SetEnabled(false);
            ApplyWorldMotion();
            yield return WaitForJourneySeconds(.8f);

            checkpointExitActive = false;
            screen.RemoveFromClassList("checkpoint--leaving");
            activeCheckpoint = null;
            arrivalExpeditionNode = null;
            arrivalNode = null;
            terminalActionLocked = false;
            ShowChoice();
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
