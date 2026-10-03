using System.Collections;
using UnityEngine;
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
        private JourneyCheckpointApproach checkpointApproach;
        private float checkpointArrivalHold;
        private float checkpointDepartureDistance;

        private void BindExpeditionCheckpointUi(VisualElement root)
        {
            resultSummary = root.Q<Label>("journey-result-summary");
            checkpointRoot = root.Q<VisualElement>("journey-checkpoint");
            checkpointTitle = root.Q<Label>("journey-checkpoint-title");
            checkpointReturn = root.Q<Button>("journey-checkpoint-return");
            checkpointContinue = root.Q<Button>("journey-checkpoint-continue");
            checkpointReturn.clicked += ReturnFromExpeditionCheckpoint;
            checkpointContinue.clicked += ContinueFromExpeditionCheckpoint;
            walker.WorldAdvanced -= AdvanceCheckpointScenery;
            walker.WorldAdvanced += AdvanceCheckpointScenery;
            ClearExpeditionCheckpointResult();
        }

        private void PrepareCheckpointApproach()
        {
            ClearCheckpointApproach();
            ExpeditionCheckpointSummary summary = ExpeditionCheckpointSystem.Build(run);
            if (!summary.IsCheckpoint || !summary.CanContinue) return;

            checkpointApproach = new JourneyCheckpointApproach(
                PostBattleRecoveryDuration * JourneyPresentationConfig.BaseTravelSpeed);
            screen.AddToClassList("checkpoint--approaching");
            checkpointReturn.SetEnabled(false);
            checkpointContinue.SetEnabled(false);
            checkpointReturn.pickingMode = PickingMode.Ignore;
            checkpointContinue.pickingMode = PickingMode.Ignore;
            checkpointRoot.BringToFront();
            ApplyCheckpointSceneryPosition();
        }

        private void AdvanceCheckpointScenery(float distance)
        {
            if (checkpointApproach != null && postBattleRecoveryActive)
                checkpointApproach.Advance(distance);
            else if (checkpointExitActive)
                checkpointDepartureDistance += distance;
            else
                return;
            ApplyCheckpointSceneryPosition();
        }

        private void ApplyCheckpointSceneryPosition()
        {
            Camera camera = Camera.main;
            if (camera == null || screen == null || checkpointReturn == null) return;
            float pixelsPerWorldUnit = screen.resolvedStyle.width *
                (camera.WorldToViewportPoint(Vector3.right).x -
                 camera.WorldToViewportPoint(Vector3.zero).x);
            float offset = (checkpointApproach?.Offset ?? 0f) - checkpointDepartureDistance;
            checkpointReturn.style.translate = new Translate(offset * pixelsPerWorldUnit, 0f);
        }

        private void ClearCheckpointApproach()
        {
            checkpointApproach = null;
            checkpointArrivalHold = 0f;
            checkpointDepartureDistance = 0f;
            screen?.RemoveFromClassList("checkpoint--approaching");
            if (checkpointReturn != null)
                checkpointReturn.style.translate = StyleKeyword.Null;
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
            screen.RemoveFromClassList("checkpoint--approaching");
            screen.RemoveFromClassList("checkpoint--leaving");
            checkpointTitle.text =
                $"第{summary.clearedFloorNumber}層　区画突破";
            checkpointReturn.SetEnabled(true);
            checkpointContinue.SetEnabled(true);
            checkpointReturn.pickingMode = PickingMode.Position;
            checkpointContinue.pickingMode = PickingMode.Position;
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
            ClearCheckpointApproach();
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
            checkpointDepartureDistance = 0f;
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
