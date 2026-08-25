using System.Linq;
using UnityEngine;

namespace Packspire
{
    public sealed partial class JourneyTravelGameplayPrototype
    {
        public void DevBeginBattle()
        {
            DevBeginBattle(null);
        }

        public void DevBeginBattle(string encounterId)
        {
            if (!uiBound)
            {
                BindUi();
            }
            if (!string.IsNullOrWhiteSpace(encounterId))
            {
                JourneyBattleEncounterProfile profile =
                    JourneyEncounterSelectionSystem.ResolveByEncounterId(encounterId);
                if (profile == null)
                    throw new System.ArgumentException(nameof(encounterId));
                ApplyEncounterProfile(profile);
            }
            BeginBattle();
        }

        public void DevPreviewScenery(int previewBiome)
        {
            if (!uiBound) BindUi();
            biomeIndex = Mathf.Clamp(previewBiome, 0, 2);
            scenery?.ClearAll();
            walker.SetJourneyBiome(biomeIndex);
            walker.SetRoadProfile(JourneyWalkCyclePrototype.RoadProfile.Standard);
            environment.SetBiome(biomeIndex);
            weatherText.text = BiomeWeatherForIndex(biomeIndex);
            BeginTravel(OpeningTravelDuration, null);
            ShowToast($"DEV景色確認：{BiomeLabel(biomeIndex)}・標準路");
        }

        public void DevSetPaused(bool value)
        {
            if (!uiBound) BindUi();
            SetPaused(value);
        }

        public void DevSkipEncounterIntro()
        {
            encounterBanner?.RemoveFromClassList("encounter--visible");
            encounterIntroActive = false;
            if (phase != Phase.Battle || screen.ClassListContains("battle--layout-preview")) return;
            battleInputLocked = false;
            RefreshBattleUi();
        }

        public void DevBeginMiniGame()
        {
            if (!uiBound) BindUi();
            BeginMiniGame();
        }

        public void DevShowMiniGame(int index)
        {
            if (!uiBound) BindUi();
            MiniGameKind[] all =
            {
                MiniGameKind.StampTiming,
                MiniGameKind.CargoBalance,
                MiniGameKind.AddressLabel,
                MiniGameKind.WaxMatch,
                MiniGameKind.RoadDodge,
                MiniGameKind.RainCover
            };
            BeginMiniGame(all[Mathf.Clamp(index, 0, all.Length - 1)]);
        }

        public void DevShowChoice()
        {
            if (!uiBound) BindUi();
            firstChoicePending = false;
            ShowChoice();
        }

        public void DevToggleLedger()
        {
            if (!uiBound) BindUi();
            ToggleLedger();
        }

        public bool DevUseFirstSeal()
        {
            if (!uiBound) BindUi();
            DeliverySealState seal = run.courierRoute.seals?.FirstOrDefault(value => value.available && value.charges > 0);
            if (seal == null) return false;
            int before = seal.charges;
            selectedSealKey = seal.key;
            PopulateSealBox();
            ApplySelectedSeal();
            return seal.charges < before;
        }

        public void DevShowEvent()
        {
            if (!uiBound) BindUi();
            arrivalNode = CourierRouteSystem.Node("broken_stair");
            ShowEvent(arrivalNode);
        }
    }
}
