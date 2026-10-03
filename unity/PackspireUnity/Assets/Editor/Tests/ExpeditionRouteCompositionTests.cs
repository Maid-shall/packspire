using NUnit.Framework;

namespace Packspire.Tests
{
    public sealed class ExpeditionRouteCompositionTests
    {
        [Test]
        public void DefaultPlanReportsEveryCompleteFloorRoute()
        {
            ExpeditionRoutePlan plan = ExpeditionRoutePlanSystem.GenerateDefault("old_spire");

            ExpeditionRouteCompositionReport report =
                ExpeditionRouteCompositionSystem.Analyze(plan);

            Assert.That(report.routeCount, Is.GreaterThan(0));
            Assert.That(report.TotalVisits, Is.EqualTo(report.routeCount * 8));
            Assert.That(report.minimumBattles, Is.EqualTo(4));
            Assert.That(report.maximumBattles, Is.EqualTo(8));
            Assert.That(
                report.BattleRatio + report.EventRatio + report.RestRatio + report.ExplorationRatio,
                Is.EqualTo(1d).Within(0.000001d));
        }

        [Test]
        public void EmptyPlanProducesZeroedReport()
        {
            ExpeditionRouteCompositionReport report =
                ExpeditionRouteCompositionSystem.Analyze(new ExpeditionRoutePlan());

            Assert.That(report.routeCount, Is.Zero);
            Assert.That(report.TotalVisits, Is.Zero);
            Assert.That(report.minimumBattles, Is.Zero);
        }
    }
}
