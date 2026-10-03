using System.Collections.Generic;
using System.Linq;

namespace Packspire
{
    public enum ExpeditionCheckpointKind
    {
        None,
        FloorCleared,
        ExpeditionCleared
    }

    public sealed class ExpeditionCheckpointSummary
    {
        public ExpeditionCheckpointKind kind;
        public int clearedFloorNumber;
        public int totalFloorCount;
        public int elapsedDays;
        public int resolvedRouteNodeCount;
        public int battlesWon;
        public int collectedNewItemCount;
        public int currentHp;
        public int maximumHp;
        public int protectedNewItemCount;
        public int exposedNewItemCount;
        public int deliverySealsSpent;

        public bool IsCheckpoint => kind != ExpeditionCheckpointKind.None;
        public bool CanContinue => kind == ExpeditionCheckpointKind.FloorCleared;
    }

    public readonly struct ExpeditionLootTally
    {
        public ExpeditionLootTally(int protectedCount, int exposedCount)
        {
            ProtectedCount = protectedCount;
            ExposedCount = exposedCount;
        }

        public int ProtectedCount { get; }
        public int ExposedCount { get; }
        public int TotalCount => ProtectedCount + ExposedCount;
    }

    /// <summary>
    /// Builds the read-only information shown after a resolved floor boss.
    /// Finalizing a run remains ExpeditionLootSystem's responsibility.
    /// </summary>
    public static class ExpeditionCheckpointSystem
    {
        public static ExpeditionCheckpointSummary Build(RunState run)
        {
            var summary = new ExpeditionCheckpointSummary();
            ExpeditionRoutePlan plan = run?.expeditionPlan;
            if (plan == null || string.IsNullOrEmpty(plan.currentNodeId))
                return summary;

            ExpeditionRouteNodePlan node = ExpeditionRoutePlanSystem.Node(
                plan,
                plan.currentNodeId);
            bool resolvedBoss = node?.kind == ExpeditionNodeKind.Boss &&
                                !plan.awaitingResolution &&
                                plan.resolvedNodeIds?.Contains(node.id) == true;
            if (!resolvedBoss) return summary;

            int floorCount = plan.floors?.Count ?? 0;
            bool finalFloor = plan.complete || node.floorIndex >= floorCount - 1;
            summary.kind = finalFloor
                ? ExpeditionCheckpointKind.ExpeditionCleared
                : ExpeditionCheckpointKind.FloorCleared;
            summary.clearedFloorNumber = node.floorIndex + 1;
            summary.totalFloorCount = floorCount;
            summary.elapsedDays = plan.elapsedDays;
            summary.resolvedRouteNodeCount = plan.resolvedNodeIds?.Distinct().Count() ?? 0;
            summary.battlesWon = run.battlesWon;
            summary.currentHp = run.hp;
            summary.maximumHp = run.maxHp;

            ExpeditionLootTally loot = CountCurrentLoot(run);
            summary.protectedNewItemCount = loot.ProtectedCount;
            summary.exposedNewItemCount = loot.ExposedCount;
            summary.collectedNewItemCount = loot.TotalCount;
            summary.deliverySealsSpent = (run.courierRoute?.seals ??
                    new List<DeliverySealState>())
                .Where(seal => seal != null)
                .Sum(seal => System.Math.Max(0, seal.maxCharges - seal.charges));
            return summary;
        }

        public static ExpeditionLootTally CountCurrentLoot(RunState run)
        {
            if (run == null) return new ExpeditionLootTally(0, 0);
            int protectedCount = 0;
            int exposedCount = 0;
            var starting = new HashSet<string>(
                run.startingItemUids ?? new List<string>());
            var packed = new HashSet<string>((run.placements ?? new List<Placement>())
                .Where(placement => placement != null &&
                                    !string.IsNullOrEmpty(placement.itemUid))
                .Select(placement => placement.itemUid));
            var seen = new HashSet<string>();

            foreach (ItemInstance item in (run.inventory ?? new List<ItemInstance>())
                         .Concat(run.lootBag ?? new List<ItemInstance>()))
            {
                if (item == null || string.IsNullOrEmpty(item.uid) ||
                    !seen.Add(item.uid) || starting.Contains(item.uid))
                    continue;

                if (packed.Contains(item.uid)) protectedCount++;
                else exposedCount++;
            }

            return new ExpeditionLootTally(protectedCount, exposedCount);
        }
    }
}
