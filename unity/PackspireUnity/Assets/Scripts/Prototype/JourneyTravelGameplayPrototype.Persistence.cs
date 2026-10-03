namespace Packspire
{
    public sealed partial class JourneyTravelGameplayPrototype
    {
        private void SaveJourneyStage(
            JourneyResumeStage stage,
            JourneyBattleRewardOffer offer = null)
        {
            if (!usesLiveRun || PackspireGame.Instance == null) return;
            PackspireGame.Instance.UiSaveSeamlessJourneyCheckpoint(stage, offer);
        }

        private void RestoreSavedJourney(JourneySessionSnapshot snapshot)
        {
            if (snapshot?.run == null)
            {
                BeginTravel(OpeningTravelDuration, null);
                return;
            }

            RefreshPersistentUi();
            ExpeditionRoutePlan plan = ExpeditionProgressSystem.Ensure(run);
            ExpeditionRouteNodePlan currentNode =
                ExpeditionRoutePlanSystem.Node(plan, plan.currentNodeId);
            CourierRouteNodeDef presentation =
                ExpeditionJourneySystem.PresentationNode(plan, currentNode);
            firstChoicePending = false;
            arrivalExpeditionNode = currentNode;
            arrivalNode = presentation;
            ApplyWorldForRouteImmediately(presentation);

            switch (snapshot.stage)
            {
                case JourneyResumeStage.Location:
                    ResolveArrival();
                    break;
                case JourneyResumeStage.Battle:
                    if (currentNode == null)
                    {
                        ShowChoice();
                        break;
                    }
                    PrepareEncounterForArrival();
                    BeginBattle();
                    break;
                case JourneyResumeStage.Reward:
                    RestoreJourneyBattleReward(snapshot.rewardOffer);
                    break;
                case JourneyResumeStage.Checkpoint:
                    if (!TryShowExpeditionCheckpoint()) ShowChoice();
                    break;
                case JourneyResumeStage.Defeat:
                    ShowResult(
                        "EXPEDITION FAILED",
                        "配送続行不能",
                        "荷を守り切れなかった。遠征結果へ進みます。",
                        false);
                    break;
                default:
                    arrivalExpeditionNode = null;
                    arrivalNode = null;
                    ShowChoice();
                    break;
            }
        }
    }
}
