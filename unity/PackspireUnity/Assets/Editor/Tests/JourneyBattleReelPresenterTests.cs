using NUnit.Framework;
using UnityEngine.UIElements;

namespace Packspire.Tests
{
    public sealed class JourneyBattleReelPresenterTests
    {
        [Test]
        public void Refresh_MovesClockWithoutRebindingUnchangedSlotContent()
        {
            VisualTreeAsset asset = PackspireResources.Load<VisualTreeAsset>(
                "UI/PackspireJourneyCompleteView");
            Assert.That(asset, Is.Not.Null);
            VisualElement root = asset.CloneTree();
            var presenter = new JourneyBattleReelPresenter(
                root,
                10d,
                null,
                _ => null);
            var battle = new RealtimeBattleController();
            battle.Start(0, 3, 3d, 1, 1);
            battle.ScheduleEnemyAction(
                "enemy",
                "strike",
                6d,
                7,
                1d);

            presenter.Refresh(battle);
            int threatBinds = presenter.ThreatContentBindCount;
            int supplyBinds = presenter.SupplyContentBindCount;

            battle.Tick(.1d);
            presenter.Refresh(battle);

            Assert.That(threatBinds, Is.EqualTo(1));
            Assert.That(supplyBinds, Is.GreaterThan(0));
            Assert.That(presenter.ThreatContentBindCount, Is.EqualTo(threatBinds));
            Assert.That(presenter.SupplyContentBindCount, Is.EqualTo(supplyBinds));
            Assert.That(
                root.Q<VisualElement>("journey-threat-slot-0")
                    .ClassListContains("slot--occupied"),
                Is.True);
        }

        [Test]
        public void Refresh_BindsOnlyNewActionThenClearsVacatedSlots()
        {
            VisualTreeAsset asset = PackspireResources.Load<VisualTreeAsset>(
                "UI/PackspireJourneyCompleteView");
            VisualElement root = asset.CloneTree();
            var presenter = new JourneyBattleReelPresenter(
                root,
                10d,
                null,
                _ => null);
            var battle = new RealtimeBattleController();
            battle.Start(0, 3, 20d, 1, 1);
            battle.ScheduleEnemyAction("enemy", "first", 3d, 4, .5d);
            presenter.Refresh(battle);
            int initialBinds = presenter.ThreatContentBindCount;

            battle.ScheduleEnemyAction("enemy", "second", 7d, 8, 1d);
            presenter.Refresh(battle);

            Assert.That(
                presenter.ThreatContentBindCount,
                Is.EqualTo(initialBinds + 1));
            Assert.That(
                root.Q<VisualElement>("journey-threat-slot-1")
                    .ClassListContains("slot--occupied"),
                Is.True);

            battle.Finish();
            presenter.Refresh(battle);

            Assert.That(
                root.Q<VisualElement>("journey-threat-slot-0")
                    .ClassListContains("slot--occupied"),
                Is.False);
            Assert.That(
                root.Q<VisualElement>("journey-threat-slot-1")
                    .ClassListContains("slot--occupied"),
                Is.False);
        }
    }
}
