using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire
{
    public sealed partial class JourneyTravelGameplayPrototype
    {
        private void BeginMiniGame(MiniGameKind? forcedKind = null)
        {
            MiniGameKind[] biomePool = biomeIndex switch
            {
                1 => new[] { MiniGameKind.AddressLabel, MiniGameKind.RainCover, MiniGameKind.CargoBalance },
                2 => new[] { MiniGameKind.RoadDodge, MiniGameKind.WaxMatch, MiniGameKind.CargoBalance },
                _ => new[] { MiniGameKind.StampTiming, MiniGameKind.CargoBalance, MiniGameKind.AddressLabel, MiniGameKind.WaxMatch }
            };
            miniGameKind = forcedKind ?? biomePool[completedRoadTasks % biomePool.Length];
            miniGameClock = 0f;
            balanceHold = 0f;
            balanceVelocity = 0f;
            miniGameStage = 0;
            miniGameTarget = UnityEngine.Random.Range(0, 3);
            miniGameLane = 1;
            miniGameResolved = false;
            SetPhase(Phase.MiniGame);
            parcelRenderer.enabled = miniGameKind == MiniGameKind.StampTiming;
            foreach (string className in new[] { "mini--stamp", "mini--balance", "mini--choice", "mini--dodge", "mini--sequence" })
                miniGamePanel.RemoveFromClassList(className);
            miniGamePanel.AddToClassList(miniGameKind switch
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
            switch (miniGameKind)
            {
                case MiniGameKind.StampTiming:
                    miniGameEyebrow.text = "ROADSIDE PICKUP";
                    miniGameTitle.text = "落とし物へ受取印を押す";
                    miniGameNote.text = "白い針が中央の金色帯に入った瞬間、SPACE。";
                    miniGameObjective.text = "成功条件：金色帯でSPACE（緑帯でも回収）";
                    miniGameStep.text = "STEP 0 / 1";
                    miniGameReward.text = "金色　荷物 +1 / 進行短縮";
                    miniGameRisk.text = "外側　見送り";
                    miniGameAction.text = "受取印を押す  [SPACE]";
                    miniGameLeft.EnableInClassList("is-hidden", true);
                    miniGameRight.EnableInClassList("is-hidden", true);
                    break;
                case MiniGameKind.CargoBalance:
                    miniGameEyebrow.text = "PACK BALANCE";
                    miniGameTitle.text = "荷崩れを抑える";
                    miniGameNote.text = "A / Dで針を中央へ戻し、安定時間をためる。";
                    miniGameObjective.text = "成功条件：中央帯に合計2秒";
                    miniGameStep.text = "安定 0.0 / 2.0";
                    miniGameReward.text = "成功　未整理荷物 +1 / HP +1";
                    miniGameRisk.text = "時間切れ　見送り";
                    miniGameAction.text = "姿勢を整える  [SPACE]";
                    miniGameValue = UnityEngine.Random.Range(22f, 78f);
                    break;
                case MiniGameKind.AddressLabel:
                    SetupChoiceMiniGame("FLYING LABEL", "風で飛ぶ荷札を照合", "表示された宛先と同じ荷札を選ぶ。", "正解を1回選択", "北塔", "灰市場", "水没書庫");
                    break;
                case MiniGameKind.WaxMatch:
                    SetupChoiceMiniGame("WAX INSPECTION", "見本と同じ封蝋を選ぶ", "見本の印影と同じ紋章を選ぶ。", "正解を2回選択", "鐘", "鍵", "羽根");
                    break;
                case MiniGameKind.RoadDodge:
                    miniGameEyebrow.text = "ROAD HAZARD";
                    miniGameTitle.text = "轍を避けて荷を守る";
                    miniGameNote.text = "A / Dで安全な車線へ移動。赤帯から離れる。";
                    miniGameObjective.text = "成功条件：3回の障害物を回避";
                    miniGameStep.text = "回避 0 / 3";
                    miniGameReward.text = "成功　進行短縮 / 荷物 +1";
                    miniGameRisk.text = "接触　今回の回収なし";
                    miniGameAction.text = "現在の車線を維持";
                    miniGameValue = 50f;
                    SetRoadHazard();
                    break;
                case MiniGameKind.RainCover:
                    miniGameEyebrow.text = "RAIN COVER";
                    miniGameTitle.text = "雨除け布を順に留める";
                    miniGameNote.text = "表示順どおりに 左・中央・右 の留め具を押す。";
                    miniGameObjective.text = "成功条件：左 → 中央 → 右";
                    miniGameStep.text = "留め具 0 / 3";
                    miniGameReward.text = "成功　荷濡れ防止 / 荷物 +1";
                    miniGameRisk.text = "順番違い　やり直し";
                    miniGameLeft.text = "左を留める  [A]";
                    miniGameAction.text = "中央を留める  [SPACE]";
                    miniGameRight.text = "右を留める  [D]";
                    break;
            }
            phaseText.text = "ROADSIDE TASK";
            nextText.text = "短い寄り道 / ノーペナルティ";
        }

        private void SetupChoiceMiniGame(string eyebrow, string title, string note, string objective, string left, string center, string right)
        {
            miniGameEyebrow.text = eyebrow;
            miniGameTitle.text = title;
            miniGameNote.text = $"{note}　見本：{(miniGameTarget == 0 ? left : miniGameTarget == 1 ? center : right)}";
            miniGameObjective.text = $"成功条件：{objective}";
            miniGameStep.text = miniGameKind == MiniGameKind.WaxMatch ? "照合 0 / 2" : "照合 0 / 1";
            miniGameReward.text = "成功　未整理荷物 +1";
            miniGameRisk.text = "誤照合　今回の回収なし";
            miniGameLeft.text = left;
            miniGameAction.text = center;
            miniGameRight.text = right;
        }

        private void UpdateMiniGame(float delta)
        {
            if (miniGameResolved) return;
            miniGameClock += delta;
            if (miniGameKind == MiniGameKind.StampTiming)
            {
                miniGameValue = (Mathf.Sin(miniGameClock * 3.7f) * .5f + .5f) * 100f;
                parcelRenderer.transform.position = new Vector3(2.25f, -1.35f + Mathf.Sin(miniGameClock * 2.6f) * .12f, 0f);
            }
            else if (miniGameKind == MiniGameKind.CargoBalance)
            {
                balanceVelocity += Mathf.Sin(miniGameClock * 1.9f) * 9f * delta;
                balanceVelocity = Mathf.Clamp(balanceVelocity, -17f, 17f);
                miniGameValue = Mathf.Clamp(miniGameValue + balanceVelocity * delta, 0f, 100f);
                if (Mathf.Abs(miniGameValue - 50f) < 10f) balanceHold += delta;
                else balanceHold = Mathf.Max(0f, balanceHold - delta * .65f);
                miniGameNote.text = $"A / Dで中央へ　安定 {balanceHold:0.0} / 2.0秒";
                miniGameStep.text = $"安定 {balanceHold:0.0} / 2.0";
                if (balanceHold >= 2f)
                {
                    FinishMiniGame(true, "荷姿が安定した。封印片を1つ回収。 ");
                    return;
                }
            }
            else if (miniGameKind == MiniGameKind.RoadDodge)
            {
                miniGameValue = miniGameLane * 50f;
                miniGameStep.text = $"回避 {miniGameStage} / 3";
            }

            miniGameNeedle.style.left = Length.Percent(Mathf.Clamp(miniGameValue, 1f, 99f));
            miniGameTimer.text = $"残り {Mathf.Max(0f, 7f - miniGameClock):0.0}秒";
            if (miniGameClock >= 7f)
            {
                FinishMiniGame(false, "見送り。旅程へのペナルティはありません。");
            }
        }

        private void MiniGameAction()
        {
            if (phase != Phase.MiniGame || miniGameResolved) return;
            if (miniGameKind == MiniGameKind.StampTiming)
            {
                float distance = Mathf.Abs(miniGameValue - 50f);
                bool success = distance <= 18f;
                bool perfect = distance <= 7f;
                if (perfect) travelClock = Mathf.Min(travelDuration, travelClock + .75f);
                FinishMiniGame(success, perfect
                    ? "PERFECT　未整理荷物 +1、次の判断地点まで少し短縮。"
                    : success ? "GOOD　未整理荷物 +1。" : "受取印がずれ、回収を見送った。");
            }
            else if (miniGameKind == MiniGameKind.CargoBalance)
            {
                balanceVelocity *= .35f;
                miniGameValue = Mathf.Lerp(miniGameValue, 50f, .28f);
            }
            else if (miniGameKind == MiniGameKind.AddressLabel || miniGameKind == MiniGameKind.WaxMatch)
            {
                ResolveChoiceMiniGame(1);
            }
            else if (miniGameKind == MiniGameKind.RoadDodge)
            {
                ResolveRoadHazard();
            }
            else if (miniGameKind == MiniGameKind.RainCover)
            {
                ResolveRainCoverInput(1);
            }
        }

        private void NudgeBalance(float direction)
        {
            if (phase != Phase.MiniGame || miniGameResolved) return;
            int side = direction < 0f ? 0 : 2;
            if (miniGameKind == MiniGameKind.CargoBalance)
            {
                balanceVelocity += direction * 10f;
            }
            else if (miniGameKind == MiniGameKind.AddressLabel || miniGameKind == MiniGameKind.WaxMatch)
            {
                ResolveChoiceMiniGame(side);
            }
            else if (miniGameKind == MiniGameKind.RoadDodge)
            {
                miniGameLane = side;
                miniGameValue = miniGameLane * 50f;
                miniGameNeedle.style.left = Length.Percent(Mathf.Clamp(miniGameValue, 1f, 99f));
            }
            else if (miniGameKind == MiniGameKind.RainCover)
            {
                ResolveRainCoverInput(side);
            }
        }

        private void ResolveChoiceMiniGame(int choice)
        {
            if (choice != miniGameTarget)
            {
                FinishMiniGame(false, "照合が一致しない。今回は荷物を見送った。");
                return;
            }
            miniGameStage++;
            int goal = miniGameKind == MiniGameKind.WaxMatch ? 2 : 1;
            miniGameStep.text = $"照合 {miniGameStage} / {goal}";
            if (miniGameStage >= goal)
            {
                FinishMiniGame(true, miniGameKind == MiniGameKind.WaxMatch ? "封蝋照合完了。正しい小包を確保した。" : "宛先照合完了。飛散荷札を回収した。");
                return;
            }
            miniGameTarget = UnityEngine.Random.Range(0, 3);
            miniGameNote.text = $"次の見本：{(miniGameTarget == 0 ? "鐘" : miniGameTarget == 1 ? "鍵" : "羽根")}";
        }

        private void SetRoadHazard()
        {
            miniGameTarget = UnityEngine.Random.Range(0, 3);
            miniGameHazard.style.left = Length.Percent(miniGameTarget * 33.333f);
            miniGameHazard.style.width = Length.Percent(33.333f);
            miniGameNote.text = $"赤い{LaneLabel(miniGameTarget)}に障害物。A / Dで安全車線へ。";
        }

        private void ResolveRoadHazard()
        {
            if (miniGameLane == miniGameTarget)
            {
                FinishMiniGame(false, "轍に接触。荷を守るため回収を断念した。");
                return;
            }
            miniGameStage++;
            miniGameStep.text = $"回避 {miniGameStage} / 3";
            if (miniGameStage >= 3)
            {
                travelClock = Mathf.Min(travelDuration, travelClock + .65f);
                FinishMiniGame(true, "障害物をすべて回避。進行を少し短縮した。");
                return;
            }
            SetRoadHazard();
        }

        private void ResolveRainCoverInput(int input)
        {
            int[] sequence = { 0, 1, 2 };
            if (input != sequence[miniGameStage])
            {
                miniGameStage = 0;
                miniGameStep.text = "順番違い / 0 / 3";
                miniGameNote.text = "順番が外れた。左 → 中央 → 右 で留め直す。";
                return;
            }
            miniGameStage++;
            miniGameStep.text = $"留め具 {miniGameStage} / 3";
            if (miniGameStage >= 3) FinishMiniGame(true, "雨除け布を固定。荷濡れを防いだ。");
        }

        private static string LaneLabel(int lane) => lane == 0 ? "左車線" : lane == 1 ? "中央車線" : "右車線";

        private void FinishMiniGame(bool success, string message)
        {
            if (miniGameResolved) return;
            miniGameResolved = true;
            ApplyWorldMotion();
            completedRoadTasks++;
            parcelRenderer.enabled = false;
            if (success)
            {
                run.courierRoute.recoveredCargoCount++;
                if (miniGameKind == MiniGameKind.CargoBalance) run.hp = Mathf.Min(run.maxHp, run.hp + 1);
            }
            RefreshPersistentUi();
            StartCoroutine(FinishMiniGameRoutine(success, message));
        }

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

