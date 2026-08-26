using System.Linq;
using NUnit.Framework;

namespace Packspire.Tests
{
    public sealed class ExpeditionCheckpointFlowTests
    {
        [Test]
        public void ThreeFloorRoute_OffersTwoContinueCheckpointsThenFullClear()
        {
            var run = new RunState
            {
                expeditionPlan = ExpeditionRoutePlanSystem.GenerateDefault("old_spire"),
                courierRoute = new CourierRouteState()
            };
            int floorCheckpointCount = 0;
            int clearCheckpointCount = 0;

            while (!run.expeditionPlan.complete)
            {
                ExpeditionRouteNodePlan next = ExpeditionJourneySystem.Available(run).First();
                Assert.That(ExpeditionJourneySystem.Select(run, next.id), Is.True);
                Assert.That(
                    ExpeditionJourneySystem.Commit(run, out ExpeditionRouteNodePlan committed, out _),
                    Is.True);
                Assert.That(committed.id, Is.EqualTo(next.id));
                Assert.That(
                    ExpeditionJourneySystem.ResolveCurrent(
                        run,
                        new CourierLocationOutcome { success = true },
                        out _),
                    Is.True);

                ExpeditionCheckpointSummary checkpoint = ExpeditionCheckpointSystem.Build(run);
                if (committed.kind != ExpeditionNodeKind.Boss)
                {
                    Assert.That(checkpoint.kind, Is.EqualTo(ExpeditionCheckpointKind.None));
                    continue;
                }

                if (checkpoint.CanContinue)
                {
                    floorCheckpointCount++;
                    Assert.That(
                        ExpeditionJourneySystem.Available(run)
                            .All(node => node.floorIndex == committed.floorIndex + 1),
                        Is.True);
                }
                else
                {
                    clearCheckpointCount++;
                    Assert.That(checkpoint.kind, Is.EqualTo(ExpeditionCheckpointKind.ExpeditionCleared));
                    Assert.That(ExpeditionJourneySystem.Available(run), Is.Empty);
                }
            }

            Assert.That(floorCheckpointCount, Is.EqualTo(2));
            Assert.That(clearCheckpointCount, Is.EqualTo(1));
        }
    }
}
