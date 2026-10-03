using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;

namespace Packspire.Tests
{
    public sealed class ExpeditionLocationContentTests
    {
        [Test]
        public void DefaultRouteHasContentForEveryNonCombatLocation()
        {
            ExpeditionRoutePlan plan = ExpeditionRoutePlanSystem.GenerateDefault("old_spire");

            ExpeditionLocationContentAudit audit =
                ExpeditionLocationContentSystem.AuditCoverage(plan);

            Assert.That(audit.errors, Is.Empty, string.Join("\n", audit.errors));
        }

        [Test]
        public void EveryFloorAndLocationKindHasAtLeastTwoPresentations()
        {
            foreach (int floorIndex in Enumerable.Range(0, 3))
            foreach (ExpeditionNodeKind kind in new[]
                     {
                         ExpeditionNodeKind.Event,
                         ExpeditionNodeKind.Rest,
                         ExpeditionNodeKind.Other
                     })
                Assert.That(
                    ExpeditionLocationContentSystem.All.Count(content =>
                        content.floorIndex == floorIndex && content.kind == kind),
                    Is.GreaterThanOrEqualTo(2),
                    $"Floor {floorIndex + 1} {kind} needs multiple presentations.");
        }

        [Test]
        public void AllLocationPresentationsAreReachableAcrossGeneratedRuns()
        {
            var selectedIds = new HashSet<string>();
            for (int runOrdinal = 0; runOrdinal < 64; runOrdinal++)
            {
                ExpeditionRoutePlan plan = ExpeditionRoutePlanSystem.GenerateDefault(
                    "old_spire",
                    runOrdinal);
                foreach (ExpeditionRouteNodePlan node in plan.floors
                             .SelectMany(floor => floor.nodes)
                             .Where(node => node.kind == ExpeditionNodeKind.Event ||
                                            node.kind == ExpeditionNodeKind.Rest ||
                                            node.kind == ExpeditionNodeKind.Other))
                    selectedIds.Add(
                        ExpeditionLocationContentSystem.SelectAndAssign(
                            plan,
                            node,
                            "old_spire").id);
            }

            Assert.That(
                selectedIds,
                Is.EquivalentTo(ExpeditionLocationContentSystem.All.Select(content => content.id)));
        }

        [Test]
        public void AssignedContentIdSurvivesLaterSeedChanges()
        {
            ExpeditionRoutePlan plan = ExpeditionRoutePlanSystem.GenerateDefault("old_spire");
            ExpeditionRouteNodePlan node = plan.floors
                .SelectMany(floor => floor.nodes)
                .First(candidate => candidate.kind == ExpeditionNodeKind.Event);

            ExpeditionLocationContentDef first = ExpeditionLocationContentSystem.SelectAndAssign(
                plan, node, "old_spire");
            string assignedId = node.locationContentId;
            plan.seed++;
            ExpeditionLocationContentDef restored = ExpeditionLocationContentSystem.SelectAndAssign(
                plan, node, "another_dungeon");

            Assert.That(assignedId, Is.Not.Empty);
            Assert.That(restored.id, Is.EqualTo(first.id));
            Assert.That(node.locationContentId, Is.EqualTo(assignedId));
        }

        [Test]
        public void ApplyingContentChangesCopyWithoutChangingRouteValues()
        {
            ExpeditionRoutePlan plan = ExpeditionRoutePlanSystem.GenerateDefault("old_spire");
            ExpeditionRouteNodePlan node = plan.floors[1].nodes
                .First(candidate => candidate.kind == ExpeditionNodeKind.Rest);
            CourierRouteNodeDef presentation = ExpeditionJourneySystem.PresentationNode(plan, node);
            int originalDays = presentation.dayCost;
            int originalRisk = presentation.risk;

            ExpeditionLocationContentDef content = ExpeditionLocationContentSystem.SelectAndAssign(
                plan, node, "old_spire");
            ExpeditionLocationContentSystem.ApplyPresentation(presentation, content);

            Assert.That(presentation.title, Is.EqualTo(content.title));
            Assert.That(presentation.dayCost, Is.EqualTo(originalDays));
            Assert.That(presentation.risk, Is.EqualTo(originalRisk));
        }

        [TestCase(ExpeditionNodeKind.Event)]
        [TestCase(ExpeditionNodeKind.Rest)]
        [TestCase(ExpeditionNodeKind.Other)]
        public void AssignedContentIsUsedByTheRuntimePresentation(ExpeditionNodeKind kind)
        {
            ExpeditionRoutePlan plan = ExpeditionRoutePlanSystem.GenerateDefault("old_spire");
            ExpeditionRouteNodePlan node = plan.floors
                .SelectMany(floor => floor.nodes)
                .First(candidate => candidate.kind == kind);
            ExpeditionLocationContentDef content =
                ExpeditionLocationContentSystem.SelectAndAssign(plan, node, "old_spire");

            CourierRouteNodeDef presentation =
                ExpeditionJourneySystem.PresentationNode(plan, node);

            Assert.That(presentation.title, Is.EqualTo(content.title));
            Assert.That(presentation.resolutionTitle, Is.EqualTo(content.resolutionTitle));
            Assert.That(presentation.resolutionText, Is.EqualTo(content.resolutionText));
            Assert.That(presentation.condition, Is.EqualTo(content.condition));
        }
    }
}
