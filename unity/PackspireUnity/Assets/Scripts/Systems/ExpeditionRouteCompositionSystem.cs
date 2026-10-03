using System;
using System.Collections.Generic;
using System.Linq;

namespace Packspire
{
    public sealed class ExpeditionRouteCompositionReport
    {
        public long routeCount;
        public long battleVisits;
        public long eventVisits;
        public long restVisits;
        public long explorationVisits;
        public int minimumBattles = int.MaxValue;
        public int maximumBattles;
        public int minimumEvents = int.MaxValue;
        public int maximumEvents;
        public int minimumRests = int.MaxValue;
        public int maximumRests;
        public int minimumExploration = int.MaxValue;
        public int maximumExploration;

        public long TotalVisits =>
            battleVisits + eventVisits + restVisits + explorationVisits;
        public double BattleRatio => Ratio(battleVisits);
        public double EventRatio => Ratio(eventVisits);
        public double RestRatio => Ratio(restVisits);
        public double ExplorationRatio => Ratio(explorationVisits);

        private double Ratio(long count) => TotalVisits == 0 ? 0d : (double)count / TotalVisits;
    }

    /// <summary>
    /// Measures authored node composition across every complete route combination.
    /// It reports current content without enforcing or changing balance targets.
    /// </summary>
    public static class ExpeditionRouteCompositionSystem
    {
        public static ExpeditionRouteCompositionReport Analyze(ExpeditionRoutePlan plan)
        {
            var report = new ExpeditionRouteCompositionReport();
            if (plan?.floors == null) return report;

            foreach (ExpeditionFloorPlan floor in plan.floors.Where(floor => floor != null))
            foreach (string startId in floor.startNodeIds ?? new List<string>())
                Collect(floor, startId, new RouteComposition(), new HashSet<string>(), report);

            if (report.routeCount == 0)
            {
                report.minimumBattles = 0;
                report.minimumEvents = 0;
                report.minimumRests = 0;
                report.minimumExploration = 0;
            }
            return report;
        }

        private static void Collect(
            ExpeditionFloorPlan floor,
            string nodeId,
            RouteComposition current,
            HashSet<string> visiting,
            ExpeditionRouteCompositionReport report)
        {
            ExpeditionRouteNodePlan node = floor.nodes?.FirstOrDefault(candidate => candidate?.id == nodeId);
            if (node == null || !visiting.Add(nodeId)) return;

            RouteComposition next = current.Copy();
            next.Add(node.kind);
            if (node.id == floor.bossNodeId)
                Record(next, report);
            else
                foreach (string nextId in node.nextNodeIds ?? new List<string>())
                    Collect(floor, nextId, next, visiting, report);
            visiting.Remove(nodeId);
        }

        private static void Record(
            RouteComposition route,
            ExpeditionRouteCompositionReport report)
        {
            report.routeCount++;
            report.battleVisits += route.battles;
            report.eventVisits += route.events;
            report.restVisits += route.rests;
            report.explorationVisits += route.exploration;
            report.minimumBattles = Math.Min(report.minimumBattles, route.battles);
            report.maximumBattles = Math.Max(report.maximumBattles, route.battles);
            report.minimumEvents = Math.Min(report.minimumEvents, route.events);
            report.maximumEvents = Math.Max(report.maximumEvents, route.events);
            report.minimumRests = Math.Min(report.minimumRests, route.rests);
            report.maximumRests = Math.Max(report.maximumRests, route.rests);
            report.minimumExploration = Math.Min(report.minimumExploration, route.exploration);
            report.maximumExploration = Math.Max(report.maximumExploration, route.exploration);
        }

        private sealed class RouteComposition
        {
            public int battles;
            public int events;
            public int rests;
            public int exploration;

            public void Add(ExpeditionNodeKind kind)
            {
                switch (kind)
                {
                    case ExpeditionNodeKind.Battle: battles++; break;
                    case ExpeditionNodeKind.Event: events++; break;
                    case ExpeditionNodeKind.Rest: rests++; break;
                    case ExpeditionNodeKind.Other: exploration++; break;
                }
            }

            public RouteComposition Copy() => new()
            {
                battles = battles,
                events = events,
                rests = rests,
                exploration = exploration
            };
        }
    }
}
