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
            MiniGameKind[] biomePool = biomeIndex switch
            {
                1 => new[] { MiniGameKind.AddressLabel, MiniGameKind.RainCover, MiniGameKind.CargoBalance },
                2 => new[] { MiniGameKind.RoadDodge, MiniGameKind.WaxMatch, MiniGameKind.CargoBalance },
                _ => new[] { MiniGameKind.StampTiming, MiniGameKind.CargoBalance, MiniGameKind.AddressLabel, MiniGameKind.WaxMatch }
            };
            MiniGameKind kind = forcedKind ?? biomePool[completedRoadTasks % biomePool.Length];
            miniGameController.Start(kind);
            miniGameVisualClock = MiniGameVisualInterval;
            miniGameLastRevision = -1;
            miniGameLastTimerTenth = -1;
            SetPhase(Phase.MiniGame);
            parcelRenderer.enabled = kind == MiniGameKind.StampTiming;

            foreach (string className in MiniGamePresentationClasses)
                miniGamePanel.RemoveFromClassList(className);
            miniGamePanel.AddToClassList(kind switch
            {
                MiniGameKind.StampTiming => "mini--stamp",
                MiniGameKind.CargoBalance => "mini--balance",
                MiniGameKind.RoadDodge => "mini--dodge",
                MiniGameKind.RainCover => "mini--sequence",
                _ => "mini--choice"
            });
            miniGamePanel.RemoveFromClassList("mini--success");
            miniGamePanel.RemoveFromClassList("mini--failure");
            miniGameAction.SetEnabled(true);
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
            switch (kind)
            {
                case MiniGameKind.StampTiming:
                    SetMiniGameCopy("ROADSIDE PICKUP", "落とし物へ受取印を押す",
                        "白い針が中央の金色帯に入った瞬間、SPACE。",
                        "成功条件：金色帯でSPACE（緑帯でも回収）", "STEP 0 / 1",
                        "金色　荷物 +1 / 進行短縮", "外側　見送り");
                    miniGameAction.text = "受取印を押す  [SPACE]";
                    miniGameLeft.EnableInClassList("is-hidden", true);
                    miniGameRight.EnableInClassList("is-hidden", true);
                    break;
                case MiniGameKind.CargoBalance:
                    SetMiniGameCopy("PACK BALANCE", "荷崩れを抑える",
                        "A / Dで針を中央へ戻し、安定時間をためる。",
                        "成功条件：中央帯に合計2秒", "安定 0.0 / 2.0",
                        "成功　未整理荷物 +1 / HP +1", "時間切れ　見送り");
                    miniGameAction.text = "姿勢を整える  [SPACE]";
                    break;
                case MiniGameKind.AddressLabel:
                    SetupChoiceMiniGame("FLYING LABEL", "風で飛ぶ荷札を照合",
                        "表示された宛先と同じ荷札を選ぶ。", "正解を1回選択", "北塔", "灰市場", "水没書庫");
                    break;
                case MiniGameKind.WaxMatch:
                    SetupChoiceMiniGame("WAX INSPECTION", "見本と同じ封蝋を選ぶ",
                        "見本の印影と同じ紋章を選ぶ。", "正解を2回選択", "鐘", "鍵", "羽根");
                    break;
                case MiniGameKind.RoadDodge:
                    SetMiniGameCopy("ROAD HAZARD", "轍を避けて荷を守る",
                        "A / Dで安全な車線へ移動。赤帯から離れる。",
                        "成功条件：3回の障害物を回避", "回避 0 / 3",
                        "成功　進行短縮 / 荷物 +1", "接触　今回の回収なし");
                    miniGameAction.text = "現在の車線を維持";
                    break;
                case MiniGameKind.RainCover:
                    SetMiniGameCopy("RAIN COVER", "雨除け布を順に留める",
                        "表示順どおりに 左・中央・右 の留め具を押す。",
                        "成功条件：左 → 中央 → 右", "留め具 0 / 3",
                        "成功　荷濡れ防止 / 荷物 +1", "順番違い　やり直し");
                    miniGameLeft.text = "左を留める  [A]";
                    miniGameAction.text = "中央を留める  [SPACE]";
                    miniGameRight.text = "右を留める  [D]";
                    break;
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

        private void SetupChoiceMiniGame(string eyebrow, string title, string note, string objective,
            string left, string center, string right)
        {
            string sample = miniGameController.Target == 0 ? left : miniGameController.Target == 1 ? center : right;
            SetMiniGameCopy(eyebrow, title, $"{note}　見本：{sample}", $"成功条件：{objective}",
                miniGameController.Kind == MiniGameKind.WaxMatch ? "照合 0 / 2" : "照合 0 / 1",
                "成功　未整理荷物 +1", "誤照合　今回の回収なし");
            miniGameLeft.text = left;
            miniGameAction.text = center;
            miniGameRight.text = right;
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
            if (phase != Phase.MiniGame || miniGameController.IsResolved) return;
            miniGameController.Input(JourneyMiniGameInput.Action);
            RefreshMiniGameDynamic(true);
            ConsumeMiniGameOutcome();
        }

        private void NudgeBalance(float direction)
        {
            if (phase != Phase.MiniGame || miniGameController.IsResolved) return;
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
                        string sample = miniGameController.Target == 0 ? "鐘" : miniGameController.Target == 1 ? "鍵" : "羽根";
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

        private IEnumerator FinishMiniGameRoutine(bool success, string message)
        {
            miniGamePanel.EnableInClassList(success ? "mini--success" : "mini--failure", true);
            miniGameAction.SetEnabled(false);
            miniGameTitle.text = success ? "回収成功" : "回収を見送った";
            miniGameNote.text = message;
            miniGameTimer.text = success ? "RESULT / SECURED" : "RESULT / MISSED";
            yield return new WaitForSecondsRealtime(.85f);
            ShowToast(message);
            SetPhase(Phase.Travel);
        }
    }
}
