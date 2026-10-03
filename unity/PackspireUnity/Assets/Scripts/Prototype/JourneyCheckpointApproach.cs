using UnityEngine;

namespace Packspire
{
    /// <summary>
    /// Presentation-only approach, measured in the same world units as the road.
    /// Time, HP, rewards and expedition progress are not changed here.
    /// </summary>
    public sealed class JourneyCheckpointApproach
    {
        private const float ArrivalTolerance = .01f;
        private const float BrakingDistance = .55f;

        public float Distance { get; }
        public float TravelledDistance { get; private set; }
        public float Offset => Distance - TravelledDistance;
        public bool HasArrived => Offset <= ArrivalTolerance;

        public JourneyCheckpointApproach(float distance)
        {
            Distance = Mathf.Max(0f, distance);
        }

        public void Advance(float worldDistance)
        {
            // Keep the final frame's overshoot: clamping would make the shrine
            // slide relative to the road on the last step.
            TravelledDistance += Mathf.Max(0f, worldDistance);
        }

        public float MotionScale(float requestedScale)
        {
            if (HasArrived || requestedScale <= 0f) return 0f;
            float stoppingDistance = BrakingDistance * requestedScale * requestedScale;
            return requestedScale * Mathf.Sqrt(Mathf.Clamp01(Offset / stoppingDistance));
        }
    }
}
