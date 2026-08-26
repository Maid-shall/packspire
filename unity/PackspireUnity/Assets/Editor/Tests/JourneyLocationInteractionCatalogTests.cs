using NUnit.Framework;

namespace Packspire.Tests
{
    public sealed class JourneyLocationInteractionCatalogTests
    {
        [Test]
        public void CargoChoiceRetainsRecoveryWithoutExtraDay()
        {
            var node = new CourierRouteNodeDef { resolution = CourierResolutionKind.Cargo };
            JourneyLocationInteractionDefinition definition =
                JourneyLocationInteractionCatalog.For(node);

            CourierLocationOutcome primary = definition.BuildOutcome(true);
            CourierLocationOutcome secondary = definition.BuildOutcome(false);

            Assert.That(primary.cargoRecovered, Is.True);
            Assert.That(primary.dayDelta, Is.Zero);
            Assert.That(secondary.cargoRecovered, Is.False);
            Assert.That(secondary.dayDelta, Is.Zero);
        }

        [Test]
        public void EventChoiceRetainsCarefulOrFastOutcome()
        {
            var node = new CourierRouteNodeDef { resolution = CourierResolutionKind.Event };
            JourneyLocationInteractionDefinition definition =
                JourneyLocationInteractionCatalog.For(node);

            CourierLocationOutcome primary = definition.BuildOutcome(true);
            CourierLocationOutcome secondary = definition.BuildOutcome(false);

            Assert.That(primary.performance, Is.EqualTo(2));
            Assert.That(primary.dayDelta, Is.Zero);
            Assert.That(secondary.performance, Is.EqualTo(1));
            Assert.That(secondary.dayDelta, Is.EqualTo(1));
        }
    }
}
