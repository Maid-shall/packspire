using System;
using System.Linq;
using NUnit.Framework;

namespace Packspire.Tests
{
    public sealed class JourneyRoadTaskCatalogTests
    {
        [Test]
        public void CatalogDefinesEveryRoadTaskExactlyOnce()
        {
            MiniGameKind[] kinds = Enum.GetValues(typeof(MiniGameKind))
                .Cast<MiniGameKind>()
                .ToArray();

            Assert.That(JourneyRoadTaskCatalog.All.Select(definition => definition.Kind),
                Is.EquivalentTo(kinds));
            Assert.That(JourneyRoadTaskCatalog.All.All(definition =>
                !string.IsNullOrWhiteSpace(definition.PresentationClass) &&
                !string.IsNullOrWhiteSpace(definition.Title) &&
                !string.IsNullOrWhiteSpace(definition.Objective)), Is.True);
        }

        [TestCase(0, "StampTiming")]
        [TestCase(1, "RainCover")]
        [TestCase(2, "RoadDodge")]
        public void BiomePoolsRetainTheirExistingDistinctiveTask(
            int biomeIndex,
            string expectedName)
        {
            Assert.That(JourneyRoadTaskCatalog.PoolForBiome(biomeIndex)
                .Select(kind => kind.ToString()), Does.Contain(expectedName));
        }

        [Test]
        public void ChoiceTasksProvideThreeLabels()
        {
            Assert.That(JourneyRoadTaskCatalog.Get(MiniGameKind.AddressLabel).Choices, Has.Length.EqualTo(3));
            Assert.That(JourneyRoadTaskCatalog.Get(MiniGameKind.WaxMatch).Choices, Has.Length.EqualTo(3));
        }
    }
}
