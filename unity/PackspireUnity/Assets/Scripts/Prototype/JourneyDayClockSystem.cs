using UnityEngine;

namespace Packspire
{
    public readonly struct JourneyDayClockSample
    {
        public JourneyDayClockSample(int day, float progress)
        {
            Day = day;
            Progress = progress;
        }

        public int Day { get; }
        public float Progress { get; }
    }

    /// <summary>
    /// Converts the already-committed segment day cost into a presentation-only
    /// clock. ExpeditionProgressSystem remains authoritative for actual time.
    /// </summary>
    public static class JourneyDayClockSystem
    {
        public static JourneyDayClockSample Sample(
            int finalElapsedDay,
            int segmentDayCost,
            float normalizedTravel,
            bool segmentVisible)
        {
            int finalDay = Mathf.Max(0, finalElapsedDay);
            int cost = Mathf.Max(0, segmentDayCost);
            if (!segmentVisible || cost == 0)
                return new JourneyDayClockSample(finalDay, 0f);

            float travel = Mathf.Clamp01(normalizedTravel);
            int startDay = Mathf.Max(0, finalDay - cost);
            if (travel >= 1f)
                return new JourneyDayClockSample(finalDay, 0f);

            float dayPosition = startDay + cost * travel;
            int displayedDay = Mathf.Min(
                finalDay,
                Mathf.FloorToInt(dayPosition));
            return new JourneyDayClockSample(
                displayedDay,
                dayPosition - Mathf.Floor(dayPosition));
        }
    }
}
