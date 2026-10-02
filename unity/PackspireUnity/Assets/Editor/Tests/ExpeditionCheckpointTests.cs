using NUnit.Framework;
using UnityEngine.UIElements;

namespace Packspire.Tests
{
    public sealed class ExpeditionCheckpointTests
    {
        [Test]
        public void ResolvedNonFinalBossOffersReturnOrContinue()
        {
            RunState run = BuildRunAtResolvedBoss(0, false);
            ItemInstance starting = new ItemInstance("checkpoint-start");
            ItemInstance protectedLoot = new ItemInstance("checkpoint-protected");
            ItemInstance exposedLoot = new ItemInstance("checkpoint-exposed");
            run.inventory.Add(starting);
            run.inventory.Add(protectedLoot);
            run.lootBag.Add(exposedLoot);
            run.startingItemUids.Add(starting.uid);
            run.placements.Add(new Placement(protectedLoot.uid, 0));
            run.courierRoute = new CourierRouteState
            {
                seals = new()
                {
                    new DeliverySealState { maxCharges = 3, charges = 1 },
                    new DeliverySealState { maxCharges = 1, charges = 1 }
                }
            };

            ExpeditionCheckpointSummary summary = ExpeditionCheckpointSystem.Build(run);

            Assert.That(summary.kind, Is.EqualTo(ExpeditionCheckpointKind.FloorCleared));
            Assert.That(summary.CanContinue, Is.True);
            Assert.That(summary.clearedFloorNumber, Is.EqualTo(1));
            Assert.That(summary.resolvedRouteNodeCount, Is.EqualTo(1));
            Assert.That(summary.collectedNewItemCount, Is.EqualTo(2));
            Assert.That(summary.protectedNewItemCount, Is.EqualTo(1));
            Assert.That(summary.exposedNewItemCount, Is.EqualTo(1));
            Assert.That(summary.deliverySealsSpent, Is.EqualTo(2));
        }

        [Test]
        public void ResolvedFinalBossCreatesClearOnlyCheckpoint()
        {
            RunState run = BuildRunAtResolvedBoss(2, true);

            ExpeditionCheckpointSummary summary = ExpeditionCheckpointSystem.Build(run);

            Assert.That(summary.kind, Is.EqualTo(ExpeditionCheckpointKind.ExpeditionCleared));
            Assert.That(summary.CanContinue, Is.False);
            Assert.That(summary.clearedFloorNumber, Is.EqualTo(3));
        }

        [Test]
        public void UnresolvedBossDoesNotCreateCheckpoint()
        {
            RunState run = BuildRunAtResolvedBoss(0, false);
            run.expeditionPlan.awaitingResolution = true;

            ExpeditionCheckpointSummary summary = ExpeditionCheckpointSystem.Build(run);

            Assert.That(summary.kind, Is.EqualTo(ExpeditionCheckpointKind.None));
        }

        [Test]
        public void JourneyViewContainsDiegeticCheckpointContract()
        {
            var asset = PackspireResources.Load<VisualTreeAsset>(
                "UI/PackspireJourneyCompleteView");
            Assert.That(asset, Is.Not.Null);
            VisualElement root = asset.CloneTree();

            Assert.That(root.Q<VisualElement>("journey-checkpoint"), Is.Not.Null);
            Assert.That(root.Q<Label>("journey-checkpoint-title"), Is.Not.Null);
            Assert.That(root.Q<Button>("journey-checkpoint-return"), Is.Not.Null);
            Assert.That(root.Q<Button>("journey-checkpoint-continue"), Is.Not.Null);
            Assert.That(root.Q<Button>("journey-result-return"), Is.Null);
        }

        private static RunState BuildRunAtResolvedBoss(int floorIndex, bool complete)
        {
            var run = new RunState
            {
                hp = 31,
                maxHp = 42,
                battlesWon = 6,
                expeditionPlan = ExpeditionRoutePlanSystem.GenerateDefault("old_spire")
            };
            ExpeditionFloorPlan floor = run.expeditionPlan.floors[floorIndex];
            run.expeditionPlan.currentNodeId = floor.bossNodeId;
            run.expeditionPlan.currentFloorIndex = floorIndex;
            run.expeditionPlan.resolvedNodeIds.Add(floor.bossNodeId);
            run.expeditionPlan.awaitingResolution = false;
            run.expeditionPlan.complete = complete;
            run.expeditionPlan.elapsedDays = 8 + floorIndex * 7;
            return run;
        }
    }
}
