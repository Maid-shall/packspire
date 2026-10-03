using System;
using System.Diagnostics;
using NUnit.Framework;
using UnityEditor;
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

        [Test]
        public void Refresh_ReportsCapacityOverflowOnceUntilItClears()
        {
            VisualTreeAsset asset = PackspireResources.Load<VisualTreeAsset>(
                "UI/PackspireJourneyCompleteView");
            VisualElement root = asset.CloneTree();
            int reports = 0;
            string report = string.Empty;
            var presenter = new JourneyBattleReelPresenter(
                root,
                10d,
                null,
                _ => null,
                message =>
                {
                    reports++;
                    report = message;
                });
            var battle = new RealtimeBattleController();
            battle.Start(0, 3, 20d, 1, 1);
            for (int index = 0;
                 index < JourneyBattleReelPresenter.ThreatSlotCount + 1;
                 index++)
                battle.ScheduleEnemyAction(
                    "enemy",
                    "action-" + index,
                    1d + index * .5d,
                    1,
                    .2d);

            presenter.Refresh(battle);
            presenter.Refresh(battle);

            Assert.That(presenter.ThreatOverflowCount, Is.EqualTo(1));
            Assert.That(reports, Is.EqualTo(1));
            Assert.That(report, Does.Contain("11 actions"));
            Assert.That(presenter.ThreatContentBindCount, Is.EqualTo(10));

            battle.Finish();
            presenter.Refresh(battle);

            Assert.That(presenter.ThreatOverflowCount, Is.Zero);
        }

        [Test]
        public void AuthoredTimelines_FitTheTenSecondReelCapacity()
        {
            string[] guids =
                AssetDatabase.FindAssets("t:RealtimeEnemyTimelineProfile");
            Assert.That(guids, Is.Not.Empty);
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var profile =
                    AssetDatabase.LoadAssetAtPath<RealtimeEnemyTimelineProfile>(path);
                if (profile == null) continue;
                RealtimeEnemyTimelineAuditReport report =
                    RealtimeEnemyTimelineAudit.Analyze(
                        profile.BuildPatterns(),
                        10d);
                Assert.That(
                    report.maximumActionsInWindow,
                    Is.LessThanOrEqualTo(
                        JourneyBattleReelPresenter.ThreatSlotCount),
                    path);
            }
        }

        [Test]
        public void Refresh_UnchangedFullReelMeetsBudgetWithoutGcAllocations()
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
            for (int index = 0;
                 index < JourneyBattleReelPresenter.ThreatSlotCount;
                 index++)
                battle.ScheduleEnemyAction(
                    "enemy",
                    "action-" + index,
                    1d + index * .75d,
                    1,
                    .2d);

            presenter.Refresh(battle);
            for (int index = 0; index < 20; index++)
                presenter.Refresh(battle);

            const int iterations = 500;
            long allocationsBefore = GC.GetAllocatedBytesForCurrentThread();
            long ticksBefore = Stopwatch.GetTimestamp();
            for (int index = 0; index < iterations; index++)
                presenter.Refresh(battle);
            long elapsedTicks = Stopwatch.GetTimestamp() - ticksBefore;
            long allocatedBytes =
                GC.GetAllocatedBytesForCurrentThread() - allocationsBefore;
            double averageMilliseconds =
                elapsedTicks * 1000d / Stopwatch.Frequency / iterations;

            TestContext.WriteLine(
                $"Full reel refresh: {averageMilliseconds:0.0000} ms average, " +
                $"{allocatedBytes / iterations} B/frame across {iterations} iterations.");
            Assert.That(
                averageMilliseconds,
                Is.LessThanOrEqualTo(
                    PackspirePerformance.BattleReelRefreshBudgetMs));
            Assert.That(
                allocatedBytes / iterations,
                Is.LessThanOrEqualTo(PackspirePerformance.UiGcBudgetBytesPerFrame));
        }
    }
}
