using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace Packspire.Tests
{
    public sealed class JourneyBattleRewardTests
    {
        [Test]
        public void OfferAlwaysContainsThreeComparableChoices()
        {
            var run = NewRun();
            JourneyBattleRewardOffer offer = JourneyBattleRewardSystem.CreateOffer(
                run,
                JourneyBattleRewardTier.Normal,
                new System.Random(1201));

            Assert.That(offer.candidates, Has.Length.EqualTo(3));
            Assert.That(offer.candidates[0].kind,
                Is.EqualTo(JourneyBattleRewardKind.Consumable));
            Assert.That(offer.candidates[1].kind,
                Is.EqualTo(JourneyBattleRewardKind.ExpeditionResource));
            Assert.That(offer.candidates.All(candidate =>
                !string.IsNullOrWhiteSpace(candidate.name)), Is.True);
        }

        [Test]
        public void AuthoredBossNodeKeepsBossRewardsWithCommonStatVariant()
        {
            var profile = JourneyEncounterSelectionSystem.ResolveByEncounterId("warden");
            Assert.That(profile, Is.Not.Null);
            var tier = JourneyBattleRewardSystem.ResolveTier(
                profile.PopulationClass, 3, ExpeditionNodeKind.Boss);
            Assert.That(tier, Is.EqualTo(JourneyBattleRewardTier.Boss));
            var offer = JourneyBattleRewardSystem.CreateOffer(NewRun(), tier, new Random(1208));
            Assert.That(offer.equipmentOffered, Is.True);
            Assert.That(offer.candidates[0].amount, Is.EqualTo(3));
        }

        [TestCase(JourneyEnemyPopulationClass.Common, 1, JourneyBattleRewardTier.Normal)]
        [TestCase(JourneyEnemyPopulationClass.Common, 3, JourneyBattleRewardTier.Danger)]
        [TestCase(JourneyEnemyPopulationClass.Elite, 1, JourneyBattleRewardTier.Elite)]
        [TestCase(JourneyEnemyPopulationClass.Boss, 1, JourneyBattleRewardTier.Boss)]
        public void BattleNodesKeepTheirExistingRewardClassification(
            JourneyEnemyPopulationClass population, int risk, JourneyBattleRewardTier expected)
        {
            Assert.That(JourneyBattleRewardSystem.ResolveTier(population, risk, ExpeditionNodeKind.Battle),
                Is.EqualTo(expected));
            Assert.That(JourneyBattleRewardSystem.ResolveTier(population, risk), Is.EqualTo(expected));
        }

        [Test]
        public void ThirdOfferPityForcesEquipmentCandidate()
        {
            var run = NewRun();
            run.journeyBattlesWithoutEquipmentOffer =
                JourneyBattleRewardSystem.EquipmentOfferPityBattles - 1;

            JourneyBattleRewardOffer offer = JourneyBattleRewardSystem.CreateOffer(
                run,
                JourneyBattleRewardTier.Normal,
                new System.Random(1202));

            Assert.That(offer.pityTriggered, Is.True);
            Assert.That(offer.equipmentOffered, Is.True);
            Assert.That(offer.candidates.Any(candidate =>
                candidate.kind == JourneyBattleRewardKind.Equipment), Is.True);
            Assert.That(run.journeyBattlesWithoutEquipmentOffer, Is.Zero);
        }

        [Test]
        public void EliteAndBossAlwaysOfferEquipment()
        {
            foreach (JourneyBattleRewardTier tier in new[]
                     {
                         JourneyBattleRewardTier.Elite,
                         JourneyBattleRewardTier.Boss
                     })
            {
                JourneyBattleRewardOffer offer = JourneyBattleRewardSystem.CreateOffer(
                    NewRun(),
                    tier,
                    new System.Random(1203 + (int)tier));
                Assert.That(offer.equipmentOffered, Is.True, tier.ToString());
            }
        }

        [Test]
        public void GrantRoutesConsumablesAndEquipmentToTheirRunStores()
        {
            var run = NewRun();
            JourneyBattleRewardOffer elite = JourneyBattleRewardSystem.CreateOffer(
                run,
                JourneyBattleRewardTier.Elite,
                new System.Random(1205));
            JourneyBattleRewardCandidate consumable = elite.candidates[0];
            JourneyBattleRewardCandidate equipment = elite.candidates.Single(candidate =>
                candidate.kind == JourneyBattleRewardKind.Equipment);

            int consumablesBefore = run.consumables.Count;
            int lootBefore = run.lootBag.Count;
            JourneyBattleRewardSystem.Grant(run, consumable, new System.Random(1206));
            JourneyBattleRewardSystem.Grant(run, equipment, new System.Random(1207));

            Assert.That(run.consumables.Count,
                Is.EqualTo(consumablesBefore + consumable.amount));
            Assert.That(run.lootBag.Count, Is.EqualTo(lootBefore + 1));
            Assert.That(run.lootBag[^1].identified, Is.False);
        }

        [Test]
        public void JourneyViewContainsFixedRewardPopupContract()
        {
            var asset = PackspireResources.Load<VisualTreeAsset>(
                "UI/PackspireJourneyCompleteView");
            Assert.That(asset, Is.Not.Null);
            VisualElement root = asset.CloneTree();

            Assert.That(root.Q<VisualElement>("journey-reward-overlay"), Is.Not.Null);
            Assert.That(root.Q<Button>("journey-reward-confirm"), Is.Not.Null);
            for (int index = 0; index < 3; index++)
                Assert.That(root.Q<Button>($"journey-reward-candidate-{index}"), Is.Not.Null);
        }

        private static RunState NewRun() => new RunState
        {
            dungeon = PackspireContent.Data.balance.defaultDungeonId
        };
    }
}
