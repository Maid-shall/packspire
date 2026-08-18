using UnityEngine.SceneManagement;

namespace Packspire
{
    public partial class PackspireGame
    {
        private const string SeamlessJourneyScene = "JourneyAnimationPrototype";

        private bool seamlessJourneySessionActive;
        private bool seamlessJourneyBattleRewardPending;
        private bool seamlessJourneyResumeAfterReward;

        /// <summary>
        /// True while either a normal expedition or a connected developer session owns
        /// the journey scene. Both paths operate on the PackspireGame RunState.
        /// </summary>
        public bool UiSeamlessJourneySessionActive => seamlessJourneySessionActive;

        public bool UiTryGetSeamlessJourneyRun(out RunState journeyRun)
        {
            journeyRun = seamlessJourneySessionActive ? run : null;
            return journeyRun != null && journeyRun.courierRoute != null;
        }

        private void LaunchSeamlessJourney()
        {
            if (run == null || run.courierRoute == null)
            {
                seamlessJourneySessionActive = false;
                return;
            }

            seamlessJourneySessionActive = true;
            seamlessJourneyBattleRewardPending = false;
            seamlessJourneyResumeAfterReward = false;
            JourneyDeveloperPreviewController.Clear();
            developerPanel = false;
            developerHasReturn = false;
            SceneManager.LoadScene(SeamlessJourneyScene);
        }

        public void UiFinishSeamlessJourney(bool win)
        {
            if (!seamlessJourneySessionActive || run == null) return;

            seamlessJourneySessionActive = false;
            seamlessJourneyBattleRewardPending = false;
            seamlessJourneyResumeAfterReward = false;
            FinishRun(win);
            SceneManager.LoadScene("Main");
        }

        /// <summary>
        /// Resolves the defeated route encounter, then opens the existing product reward
        /// screen without detaching the live run. Reward confirmation returns to the same
        /// route session.
        /// </summary>
        public void UiOpenSeamlessJourneyBattleReward()
        {
            if (!seamlessJourneySessionActive || run == null) return;

            ApplyBattleVictoryRewards();
            battle = null;

            if (run.courierRoute?.awaitingResolution == true)
            {
                CourierRouteSystem.ResolveCurrent(
                    run,
                    new CourierLocationOutcome
                    {
                        success = true,
                        performance = 2,
                        message = "番人を退けた。"
                    },
                    out message);
            }

            seamlessJourneyBattleRewardPending = true;
            seamlessJourneyResumeAfterReward = false;
            screen = ScreenId.Reward;
            SceneManager.LoadScene("Main");
        }

        public bool UiConsumeSeamlessJourneyResumeAfterReward()
        {
            if (!seamlessJourneySessionActive || !seamlessJourneyResumeAfterReward) return false;
            seamlessJourneyResumeAfterReward = false;
            return true;
        }

        public void UiLaunchSeamlessJourneyDeveloperSession()
        {
            run = LoadoutSystem.CreateRun(meta, PackspireContent.Data.balance.defaultDungeonId);
            RoleFrameworkSystem.Normalize(meta);
            run.role = meta.currentRole;
            run.courierRoute = CourierRouteSystem.Create(run, meta);
            battle = null;
            gridBoard = null;
            packingAtBase = false;
            packingAtRelay = false;
            courierBattleNodeId = courierEventNodeId = courierCargoNodeId = "";
            seamlessJourneySessionActive = true;
            seamlessJourneyBattleRewardPending = false;
            seamlessJourneyResumeAfterReward = false;
            developerPanel = false;
            developerHasReturn = false;
            screen = ScreenId.Route;
            SceneManager.LoadScene(SeamlessJourneyScene);
        }

        private bool TryResumeSeamlessJourneyAfterReward()
        {
            if (!seamlessJourneySessionActive || !seamlessJourneyBattleRewardPending || run == null)
                return false;

            seamlessJourneyBattleRewardPending = false;
            seamlessJourneyResumeAfterReward = true;
            screen = ScreenId.Route;
            SceneManager.LoadScene(SeamlessJourneyScene);
            return true;
        }

        public void UiLeaveSeamlessJourneyForDeveloperMenu()
        {
            seamlessJourneySessionActive = false;
            seamlessJourneyBattleRewardPending = false;
            seamlessJourneyResumeAfterReward = false;
            run = null;
            battle = null;
            gridBoard = null;
            packingAtBase = false;
            packingAtRelay = false;
            screen = ScreenId.Expedition;
            message = "本編遠征を終了し、開発者メニューへ戻りました。";
            UiOpenDeveloperPanel();
            SceneManager.LoadScene("Main");
        }
    }
}
