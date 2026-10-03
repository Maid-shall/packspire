using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire
{
    public sealed partial class JourneyTravelGameplayPrototype
    {
        private const float MiniGameVisualInterval = 1f / 30f;

        private void BeginMiniGame(MiniGameKind? forcedKind = null)
        {
            var biomePool = JourneyRoadTaskCatalog.PoolForBiome(biomeIndex);
            MiniGameKind kind = forcedKind ?? biomePool[completedRoadTasks % biomePool.Count];
            JourneyRoadTaskDefinition definition = JourneyRoadTaskCatalog.Get(kind);
            miniGameController.Start(kind);
            miniGameVisualClock = MiniGameVisualInterval;
            miniGameLastRevision = -1;
            miniGameLastTimerTenth = -1;
            SetPhase(Phase.MiniGame);
            parcelRenderer.enabled = kind == MiniGameKind.StampTiming;

            foreach (string className in MiniGamePresentationClasses)
                miniGamePanel.RemoveFromClassList(className);
            miniGamePanel.AddToClassList(definition.PresentationClass);
            miniGamePanel.RemoveFromClassList("mini--success");
            miniGamePanel.RemoveFromClassList("mini--failure");
            SetMiniGameControlsEnabled(true);
            miniGameLeft.EnableInClassList("is-hidden", false);
            miniGameRight.EnableInClassList("is-hidden", false);
            miniGameLeft.text = "◀";
            miniGameRight.text = "▶";
            ConfigureMiniGamePresentation(kind);
            RefreshMiniGameDynamic(true);
            phaseText.text = "ROADSIDE TASK";
            nextText.text = "短い寄り道 / ノーペナルティ";
        }

        private void ConfigureMiniGamePresentation(MiniGameKind kind)
        {
            JourneyRoadTaskDefinition definition = JourneyRoadTaskCatalog.Get(kind);
            string note = definition.IsChoice
                ? $"{definition.Note}　見本：{definition.ChoiceAt(miniGameController.Target)}"
                : definition.Note;
            SetMiniGameCopy(
                definition.Eyebrow,
                definition.Title,
                note,
                definition.Objective,
                definition.InitialStep,
                definition.Reward,
                definition.Risk);
            miniGameLeft.EnableInClassList("is-hidden", definition.HideSideButtons);
            miniGameRight.EnableInClassList("is-hidden", definition.HideSideButtons);
            if (!string.IsNullOrEmpty(definition.LeftButton)) miniGameLeft.text = definition.LeftButton;
            if (!string.IsNullOrEmpty(definition.ActionButton)) miniGameAction.text = definition.ActionButton;
            if (!string.IsNullOrEmpty(definition.RightButton)) miniGameRight.text = definition.RightButton;
            if (definition.IsChoice)
            {
                miniGameLeft.text = definition.ChoiceAt(0);
                miniGameAction.text = definition.ChoiceAt(1);
                miniGameRight.text = definition.ChoiceAt(2);
            }
        }

        private void SetMiniGameCopy(string eyebrow, string title, string note, string objective,
            string step, string reward, string risk)
        {
            miniGameEyebrow.text = eyebrow;
            miniGameTitle.text = title;
            miniGameNote.text = note;
            miniGameObjective.text = objective;
            miniGameStep.text = step;
            miniGameReward.text = reward;
            miniGameRisk.text = risk;
        }

        private void UpdateMiniGame(float delta)
        {
            using var performanceScope = PackspirePerformance.JourneyMiniGame.Auto();
            if (miniGameController.IsResolved) return;
            miniGameController.Tick(delta);
            if (miniGameController.Kind == MiniGameKind.StampTiming)
            {
                float clock = miniGameController.Clock;
                parcelRenderer.transform.position = new Vector3(2.25f, -1.35f + Mathf.Sin(clock * 2.6f) * .12f, 0f);
            }

            miniGameVisualClock += delta;
            if (miniGameVisualClock >= MiniGameVisualInterval)
            {
                miniGameVisualClock %= MiniGameVisualInterval;
                RefreshMiniGameDynamic(false);
            }
            ConsumeMiniGameOutcome();
        }

        private void MiniGameAction()
        {
            if (GameplayInputBlocked || phase != Phase.MiniGame ||
                miniGameController.IsResolved) return;
            miniGameController.Input(JourneyMiniGameInput.Action);
            RefreshMiniGameDynamic(true);
            ConsumeMiniGameOutcome();
        }

        private void NudgeBalance(float direction)
        {
            if (GameplayInputBlocked || phase != Phase.MiniGame ||
                miniGameController.IsResolved) return;
            miniGameController.Input(direction < 0f ? JourneyMiniGameInput.Left : JourneyMiniGameInput.Right);
            RefreshMiniGameDynamic(true);
            ConsumeMiniGameOutcome();
        }

        private void RefreshMiniGameDynamic(bool force)
        {
            float value = Mathf.Clamp(miniGameController.Value, 1f, 99f);
            int needleBucket = Mathf.RoundToInt(value * 4f);
            if (force || needleBucket != miniGameLastNeedleBucket)
            {
                miniGameLastNeedleBucket = needleBucket;
                miniGameNeedle.style.left = Length.Percent(needleBucket * .25f);
            }

            int timerTenth = Mathf.CeilToInt(miniGameController.SecondsRemaining * 10f);
            if (force || timerTenth != miniGameLastTimerTenth)
            {
                miniGameLastTimerTenth = timerTenth;
                miniGameTimer.text = $"残り {timerTenth / 10f:0.0}秒";
            }

            if (!force && miniGameLastRevision == miniGameController.Revision &&
                miniGameController.Kind != MiniGameKind.CargoBalance) return;
            miniGameLastRevision = miniGameController.Revision;

            switch (miniGameController.Kind)
            {
                case MiniGameKind.CargoBalance:
                    float hold = miniGameController.BalanceHold;
                    miniGameNote.text = $"A / Dで中央へ　安定 {hold:0.0} / 2.0秒";
                    miniGameStep.text = $"安定 {hold:0.0} / 2.0";
                    break;
                case MiniGameKind.AddressLabel:
                    miniGameStep.text = $"照合 {miniGameController.Stage} / 1";
                    break;
                case MiniGameKind.WaxMatch:
                    miniGameStep.text = $"照合 {miniGameController.Stage} / 2";
                    if (miniGameController.Stage > 0)
                    {
                        string sample = JourneyRoadTaskCatalog.Get(MiniGameKind.WaxMatch)
                            .ChoiceAt(miniGameController.Target);
                        miniGameNote.text = $"次の見本：{sample}";
                    }
                    break;
                case MiniGameKind.RoadDodge:
                    miniGameStep.text = $"回避 {miniGameController.Stage} / 3";
                    miniGameHazard.style.left = Length.Percent(miniGameController.Target * 33.333f);
                    miniGameHazard.style.width = Length.Percent(33.333f);
                    miniGameNote.text = $"赤い{LaneLabel(miniGameController.Target)}に障害物。A / Dで安全車線へ。";
                    break;
                case MiniGameKind.RainCover:
                    miniGameStep.text = miniGameController.LastInputRejected
                        ? "順番違い / 0 / 3"
                        : $"留め具 {miniGameController.Stage} / 3";
                    if (miniGameController.LastInputRejected)
                        miniGameNote.text = "順番が外れた。左 → 中央 → 右 で留め直す。";
                    break;
            }
        }

        private void ConsumeMiniGameOutcome()
        {
            if (!miniGameController.TryConsumeOutcome(out JourneyMiniGameOutcome outcome)) return;
            ApplyWorldMotion();
            completedRoadTasks++;
            parcelRenderer.enabled = false;
            if (outcome.Success)
            {
                run.courierRoute.recoveredCargoCount++;
                if (outcome.HealCourier) run.hp = Mathf.Min(run.maxHp, run.hp + 1);
            }
            if (outcome.TravelAdvance > 0f)
                travelClock = Mathf.Min(travelDuration, travelClock + outcome.TravelAdvance);
            RefreshPersistentUi();
            StartCoroutine(FinishMiniGameRoutine(outcome.Success, outcome.Message));
        }

        private static string LaneLabel(int lane) => lane == 0 ? "左車線" : lane == 1 ? "中央車線" : "右車線";

        private void SetMiniGameControlsEnabled(bool requested)
        {
            bool enabled = requested && !GameplayInputBlocked;
            miniGameAction?.SetEnabled(enabled);
            miniGameLeft?.SetEnabled(enabled);
            miniGameRight?.SetEnabled(enabled);
        }

        private IEnumerator FinishMiniGameRoutine(bool success, string message)
        {
            miniGamePanel.EnableInClassList(success ? "mini--success" : "mini--failure", true);
            SetMiniGameControlsEnabled(false);
            miniGameTitle.text = success ? "回収成功" : "回収を見送った";
            miniGameNote.text = message;
            miniGameTimer.text = success ? "RESULT / SECURED" : "RESULT / MISSED";
            yield return WaitForJourneySeconds(.85f);
            ShowToast(message);
            SetPhase(Phase.Travel);
        }
    }
}
