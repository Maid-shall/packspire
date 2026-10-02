using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire
{
    public sealed partial class JourneyTravelGameplayPrototype
    {
        private VisualElement travelSealHost;
        private VisualElement dayClock;
        private Label dayNumber;
        private VisualElement dayClockHand;
        private readonly VisualElement[] dayClockTicks = new VisualElement[12];
        private VisualElement bagPanel;
        private Label bagSummary;
        private Label bagProtected;
        private Label bagExposed;
        private bool bagOpen;
        private ExpeditionDayStage? displayedDayStage;

        private void BindTravelHudUi(VisualElement root)
        {
            travelSealHost = root.Q<VisualElement>("journey-travel-seal-host");
            dayClock = root.Q<VisualElement>("journey-day-clock");
            dayNumber = root.Q<Label>("journey-day-number");
            dayClockHand = root.Q<VisualElement>("journey-day-clock-hand");
            for (int index = 0; index < dayClockTicks.Length; index++)
                dayClockTicks[index] =
                    root.Q<VisualElement>($"journey-day-tick-{index:00}");
            bagPanel = root.Q<VisualElement>("journey-bag-panel");
            bagSummary = root.Q<Label>("journey-bag-summary");
            bagProtected = root.Q<Label>("journey-bag-protected");
            bagExposed = root.Q<Label>("journey-bag-exposed");
            root.Q<Button>("journey-bag-open").clicked += ToggleBag;
            root.Q<Button>("journey-bag-close").clicked += CloseBag;
            CloseBag();
        }

        private void RefreshTravelHud()
        {
            if (run == null) return;
            PopulateTravelSealReel();

            ExpeditionLootTally loot =
                ExpeditionCheckpointSystem.CountCurrentLoot(run);
            cargoText.text = loot.TotalCount.ToString();
            bagProtected.text = loot.ProtectedCount.ToString();
            bagExposed.text = loot.ExposedCount.ToString();
            bagSummary.text = loot.TotalCount == 0
                ? "回収した戦利品はありません"
                : $"回収 {loot.TotalCount}個　バッグ内の保護状態";
            UpdateDayClockPresentation();
        }

        private void UpdateDayClockPresentation()
        {
            if (run?.expeditionPlan == null || dayNumber == null) return;
            CourierRouteState route = run.courierRoute;
            bool segmentCommitted =
                route?.travelPending == true &&
                route.travelDayCost > 0;
            bool segmentVisible =
                segmentCommitted &&
                (phase == Phase.Travel ||
                 phase == Phase.MiniGame ||
                 phase == Phase.Choice);
            float progress =
                segmentVisible &&
                phase != Phase.Choice &&
                travelDuration > 0f
                    ? Mathf.Clamp01(travelClock / travelDuration)
                    : 0f;
            JourneyDayClockSample sample = JourneyDayClockSystem.Sample(
                run.expeditionPlan.elapsedDays,
                route?.travelDayCost ?? 0,
                progress,
                segmentVisible);

            dayNumber.text = sample.Day.ToString();
            int filledTicks = sample.Progress <= 0f
                ? 0
                : Mathf.Clamp(
                    Mathf.CeilToInt(sample.Progress * dayClockTicks.Length),
                    0,
                    dayClockTicks.Length);
            for (int index = 0; index < dayClockTicks.Length; index++)
                dayClockTicks[index]?.EnableInClassList(
                    "tick--filled",
                    index < filledTicks);
            if (dayClockHand != null)
                dayClockHand.style.rotate =
                    new Rotate(Angle.Degrees(sample.Progress * 360f));

            ExpeditionDayStage current = ExpeditionRoutePlanSystem.DayStage(
                run.expeditionPlan,
                sample.Day);
            dayClock?.EnableInClassList(
                "day-stage--alert",
                current == ExpeditionDayStage.Alert);
            dayClock?.EnableInClassList(
                "day-stage--pursuit",
                current == ExpeditionDayStage.Pursuit);
            dayClock?.EnableInClassList(
                "day-stage--anomaly",
                current == ExpeditionDayStage.Anomaly);
            if (displayedDayStage.HasValue &&
                displayedDayStage.Value != current)
                ShowToast($"危険段階が「{JourneyDayStageLabel(current)}」へ移行");
            displayedDayStage = current;
        }

        private void PopulateTravelSealReel()
        {
            if (travelSealHost == null) return;
            travelSealHost.Clear();
            List<DeliverySealState> seals =
                (run?.courierRoute?.seals ?? new List<DeliverySealState>())
                .Where(seal => seal != null && seal.available)
                .OrderByDescending(seal => seal.charges > 0)
                .ThenBy(seal => seal.name)
                .ToList();

            if (seals.Count == 0)
            {
                Label empty = new Label("使用できる遠征印なし");
                empty.AddToClassList("ps-journey__travel-seal-empty");
                travelSealHost.Add(empty);
                return;
            }

            foreach (DeliverySealState seal in seals)
            {
                DeliverySealState captured = seal;
                Button entry = new Button(() => OpenTravelSeal(captured.key));
                entry.AddToClassList("ps-journey__travel-seal");
                entry.AddToClassList(TravelSealClass(seal.target));
                entry.SetEnabled(seal.charges > 0);

                Label mark = new Label(TravelSealMark(seal.target));
                mark.AddToClassList("ps-journey__travel-seal-mark");
                Label name = new Label(string.IsNullOrWhiteSpace(seal.name)
                    ? "遠征印"
                    : seal.name);
                name.AddToClassList("ps-journey__travel-seal-name");
                Label charges = new Label(SealChargePips(seal));
                charges.AddToClassList("ps-journey__travel-seal-charges");
                entry.Add(mark);
                entry.Add(name);
                entry.Add(charges);
                travelSealHost.Add(entry);
            }
        }

        private void OpenTravelSeal(string key)
        {
            if (string.IsNullOrEmpty(key)) return;
            if (bagOpen) CloseBag();
            if (!ledgerOpen) ToggleLedger();
            selectedSealKey = key;
            PopulateSealBox();
        }

        private void ToggleBag()
        {
            SetBagOpen(!bagOpen);
        }

        private void CloseBag()
        {
            SetBagOpen(false);
        }

        private void SetBagOpen(bool open)
        {
            bagOpen = open;
            bagPanel?.EnableInClassList("bag--open", open);
            ApplyWorldMotion();
            RefreshPauseSensitiveControls();
        }

        private static string TravelSealClass(string target) =>
            target == DeliverySealSystem.DelayTarget
                ? "seal--delay"
                : target == DeliverySealSystem.SealTarget
                    ? "seal--refill"
                    : "seal--route";

        private static string TravelSealMark(string target) =>
            target == DeliverySealSystem.DelayTarget
                ? "止"
                : target == DeliverySealSystem.SealTarget
                    ? "補"
                    : "進";

        private static string SealChargePips(DeliverySealState seal)
        {
            int maximum = System.Math.Max(1, seal.maxCharges);
            int current = System.Math.Max(0, System.Math.Min(maximum, seal.charges));
            return new string('●', current) + new string('○', maximum - current);
        }
    }
}
