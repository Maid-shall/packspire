using UnityEngine.SceneManagement;

namespace Packspire
{
    public partial class PackspireGame
    {
        private const string SeamlessJourneyScene = "JourneyAnimationPrototype";

        private bool seamlessJourneySessionActive;
        private bool seamlessJourneyDeveloperSession;
        private bool seamlessJourneyBattleRewardPending;
        private bool seamlessJourneyResumeAfterReward;
        private JourneySessionSnapshot pendingJourneyRestore;

        /// <summary>
        /// True while either a normal expedition or a connected developer session owns
        /// the journey scene. Both paths operate on the PackspireGame RunState.
        /// </summary>
        public bool UiSeamlessJourneySessionActive => seamlessJourneySessionActive;
        public bool UiSeamlessJourneyDeveloperSession => seamlessJourneyDeveloperSession;
        public bool UiCanResumeSeamlessJourney =>
            JourneySessionSaveSystem.CanResume(meta);
        public string UiSeamlessJourneyResumeSummary =>
            JourneySessionSaveSystem.Summary(meta);

        public bool UiTryGetSeamlessJourneyRun(out RunState journeyRun)
        {
            journeyRun = seamlessJourneySessionActive ? run : null;
            if (journeyRun == null || journeyRun.courierRoute == null) return false;
            ExpeditionProgressSystem.Ensure(journeyRun, meta);
            return true;
        }

        private void LaunchSeamlessJourney()
        {
            if (run == null || run.courierRoute == null)
            {
                seamlessJourneySessionActive = false;
                return;
            }

            ExpeditionProgressSystem.Ensure(run, meta);

            seamlessJourneySessionActive = true;
            seamlessJourneyDeveloperSession = false;
            seamlessJourneyBattleRewardPending = false;
            seamlessJourneyResumeAfterReward = false;
            pendingJourneyRestore = null;
            JourneySessionSaveSystem.Capture(
                meta,
                run,
                JourneyResumeStage.Choice);
            SaveSystem.Save(meta);
            JourneyDeveloperPreviewController.Clear();
            developerPanel = false;
            developerHasReturn = false;
            SceneManager.LoadScene(SeamlessJourneyScene);
        }

        public void UiFinishSeamlessJourney(bool win)
        {
            UiFinishSeamlessJourney(
                win ? ExpeditionEndReason.Clear : ExpeditionEndReason.Defeat);
        }

        public void UiFinishSeamlessJourney(ExpeditionEndReason reason)
        {
            if (!seamlessJourneySessionActive || run == null) return;
            if (seamlessJourneyDeveloperSession)
            {
                UiLeaveSeamlessJourneyForDeveloperMenu();
                return;
            }

            seamlessJourneySessionActive = false;
            seamlessJourneyDeveloperSession = false;
            seamlessJourneyBattleRewardPending = false;
            seamlessJourneyResumeAfterReward = false;
            pendingJourneyRestore = null;
            JourneySessionSaveSystem.Clear(meta);
            FinishRun(reason);
            SceneManager.LoadScene("Main");
        }

        public void UiSaveSeamlessJourneyCheckpoint(
            JourneyResumeStage stage,
            JourneyBattleRewardOffer rewardOffer = null)
        {
            if (!seamlessJourneySessionActive ||
                seamlessJourneyDeveloperSession ||
                run == null)
                return;
            if (!JourneySessionSaveSystem.Capture(meta, run, stage, rewardOffer)) return;
            SaveSystem.Save(meta);
        }

        public void UiResumeSeamlessJourney()
        {
            if (!JourneySessionSaveSystem.TryRestore(
                    meta,
                    out JourneySessionSnapshot snapshot))
                return;

            run = snapshot.run;
            ExpeditionProgressSystem.Ensure(run, meta);
            battle = null;
            gridBoard = null;
            packingAtBase = false;
            packingAtRelay = false;
            courierBattleNodeId = courierEventNodeId = courierCargoNodeId = "";
            seamlessJourneySessionActive = true;
            seamlessJourneyDeveloperSession = false;
            seamlessJourneyBattleRewardPending = false;
            seamlessJourneyResumeAfterReward = false;
            pendingJourneyRestore = snapshot;
            JourneyDeveloperPreviewController.Clear();
            developerPanel = false;
            developerHasReturn = false;
            screen = ScreenId.Route;
            SceneManager.LoadScene(SeamlessJourneyScene);
        }

        public bool UiConsumeSeamlessJourneyRestore(
            out JourneySessionSnapshot snapshot)
        {
            snapshot = pendingJourneyRestore;
            pendingJourneyRestore = null;
            return seamlessJourneySessionActive && snapshot != null;
        }

        /// <summary>
        /// Compatibility entry point for older callers. The journey now resolves the
        /// encounter in place and owns its compact reward popup without a scene hop.
        /// </summary>
        public void UiOpenSeamlessJourneyBattleReward()
        {
            UiResolveSeamlessJourneyBattleVictory();
        }

        public bool UiResolveSeamlessJourneyBattleVictory()
        {
            if (!seamlessJourneySessionActive || run == null) return false;

            ExpeditionRoutePlan plan = ExpeditionProgressSystem.Ensure(run, meta);
            bool expeditionPending = plan.awaitingResolution;
            bool courierPending = run.courierRoute?.awaitingResolution == true;
            if (!expeditionPending && !courierPending) return false;

            ApplyBattleVictoryRewards();
            battle = null;
            var outcome = new CourierLocationOutcome
            {
                success = true,
                performance = 2,
                message = "敵を退けた。"
            };
            bool resolved = expeditionPending
                ? ExpeditionJourneySystem.ResolveCurrent(run, outcome, out message)
                : CourierRouteSystem.ResolveCurrent(run, outcome, out message);
            seamlessJourneyBattleRewardPending = false;
            seamlessJourneyResumeAfterReward = false;
            return resolved;
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
            seamlessJourneyDeveloperSession = true;
            seamlessJourneyBattleRewardPending = false;
            seamlessJourneyResumeAfterReward = false;
            pendingJourneyRestore = null;
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
            if (!seamlessJourneyDeveloperSession)
            {
                seamlessJourneySessionActive = false;
                seamlessJourneyBattleRewardPending = false;
                seamlessJourneyResumeAfterReward = false;
                pendingJourneyRestore = null;
                run = null;
                battle = null;
                gridBoard = null;
                packingAtBase = false;
                packingAtRelay = false;
                screen = ScreenId.Expedition;
                SceneManager.LoadScene("Main");
                return;
            }

            seamlessJourneySessionActive = false;
            seamlessJourneyDeveloperSession = false;
            seamlessJourneyBattleRewardPending = false;
            seamlessJourneyResumeAfterReward = false;
            pendingJourneyRestore = null;
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
