using UnityEngine;

namespace Packspire
{
    public sealed partial class JourneyTravelGameplayPrototype
    {
        public bool DevShowExpeditionCheckpoint(int floorIndex = 0)
        {
            if (!uiBound) BindUi();
            ExpeditionRoutePlan plan = ExpeditionProgressSystem.Ensure(run);
            if (plan.floors == null || plan.floors.Count == 0) return false;

            int safeFloorIndex = Mathf.Clamp(
                floorIndex,
                0,
                plan.floors.Count - 1);
            ExpeditionFloorPlan floor = plan.floors[safeFloorIndex];
            plan.currentNodeId = floor.bossNodeId;
            plan.currentFloorIndex = safeFloorIndex;
            plan.awaitingResolution = false;
            plan.complete = safeFloorIndex >= plan.floors.Count - 1;
            if (!plan.resolvedNodeIds.Contains(floor.bossNodeId))
                plan.resolvedNodeIds.Add(floor.bossNodeId);
            return TryShowExpeditionCheckpoint();
        }
    }
}
