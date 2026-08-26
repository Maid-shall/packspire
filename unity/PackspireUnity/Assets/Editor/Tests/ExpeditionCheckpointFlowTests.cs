using System.Collections.Generic;
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

        [Test]
        public void FirstFloorVerticalSlice_KeepsOneRunThroughLocationsRewardsAndBoss()
        {
            var run = new RunState
            {
                dungeon = PackspireContent.Data.balance.defaultDungeonId,
                hp = 30,
                maxHp = 42,
                expeditionPlan = ExpeditionRoutePlanSystem.GenerateDefault(
                    PackspireContent.Data.balance.defaultDungeonId),
                courierRoute = new CourierRouteState()
            };
            var visitedKinds = new HashSet<ExpeditionNodeKind>();
            int grantedBattleRewards = 0;
            ExpeditionCheckpointSummary checkpoint = null;

            while (checkpoint?.IsCheckpoint != true)
            {
                ExpeditionRouteNodePlan next = ExpeditionJourneySystem.Available(run)
                    .OrderBy(node => node.lane == 0 ? 0 : 1)
                    .ThenBy(node => node.lane)
                    .First();
                Assert.That(ExpeditionJourneySystem.Select(run, next.id), Is.True);
                Assert.That(
                    ExpeditionJourneySystem.Commit(
                        run,
                        out ExpeditionRouteNodePlan committed,
                        out _),
                    Is.True);
                visitedKinds.Add(committed.kind);

                CourierLocationOutcome outcome;
                if (committed.kind == ExpeditionNodeKind.Battle ||
                    committed.kind == ExpeditionNodeKind.Boss)
                {
                    run.battlesWon++;
                    outcome = new CourierLocationOutcome
                    {
                        success = true,
                        performance = 2,
                        message = "敵を退けた。"
                    };
                }
                else
                {
                    ExpeditionLocationContentDef content =
                        ExpeditionLocationContentSystem.SelectAndAssign(
                            run.expeditionPlan,
                            committed,
                            run.dungeon);
                    CourierRouteNodeDef presentation =
                        ExpeditionJourneySystem.PresentationNode(
                            run.expeditionPlan,
                            committed);
                    Assert.That(presentation.title, Is.EqualTo(content.title));
                    outcome = JourneyLocationInteractionCatalog
                        .For(presentation, committed.kind)
                        .BuildOutcome(true);
                }

                Assert.That(
                    ExpeditionJourneySystem.ResolveCurrent(run, outcome, out _),
                    Is.True);

                if (committed.kind == ExpeditionNodeKind.Battle ||
                    committed.kind == ExpeditionNodeKind.Boss)
                {
                    JourneyBattleRewardTier tier =
                        committed.kind == ExpeditionNodeKind.Boss
                            ? JourneyBattleRewardTier.Boss
                            : JourneyBattleRewardTier.Normal;
                    JourneyBattleRewardOffer offer =
                        JourneyBattleRewardSystem.CreateOffer(
                            run,
                            tier,
                            new System.Random(3100 + committed.order));
                    Assert.That(offer.candidates, Has.Length.EqualTo(3));
                    JourneyBattleRewardSystem.Grant(
                        run,
                        offer.candidates[0],
                        new System.Random(4100 + committed.order));
                    grantedBattleRewards++;
                }

                checkpoint = ExpeditionCheckpointSystem.Build(run);
            }

            Assert.That(checkpoint.kind, Is.EqualTo(ExpeditionCheckpointKind.FloorCleared));
            Assert.That(checkpoint.clearedFloorNumber, Is.EqualTo(1));
            Assert.That(visitedKinds, Does.Contain(ExpeditionNodeKind.Battle));
            Assert.That(visitedKinds, Does.Contain(ExpeditionNodeKind.Event));
            Assert.That(visitedKinds, Does.Contain(ExpeditionNodeKind.Rest));
            Assert.That(visitedKinds, Does.Contain(ExpeditionNodeKind.Other));
            Assert.That(visitedKinds, Does.Contain(ExpeditionNodeKind.Boss));
            Assert.That(grantedBattleRewards, Is.EqualTo(5));
            Assert.That(run.consumables, Is.Not.Empty);
            Assert.That(
                ExpeditionJourneySystem.Available(run)
                    .All(node => node.floorIndex == 1),
                Is.True);
        }
    }
}
