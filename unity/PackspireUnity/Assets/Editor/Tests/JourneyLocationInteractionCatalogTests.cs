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

        [Test]
        public void RestNodeShowsOneConfirmationWithoutChangingItsOutcomeValues()
        {
            var node = new CourierRouteNodeDef { resolution = CourierResolutionKind.Relay };
            JourneyLocationInteractionDefinition definition =
                JourneyLocationInteractionCatalog.For(node, ExpeditionNodeKind.Rest);

            CourierLocationOutcome outcome = definition.BuildOutcome(true);

            Assert.That(definition.Eyebrow, Is.EqualTo("RELAY STOP"));
            Assert.That(definition.ShowSecondaryButton, Is.False);
            Assert.That(outcome.success, Is.True);
            Assert.That(outcome.dayDelta, Is.Zero);
            Assert.That(outcome.cargoRecovered, Is.False);
        }

        [Test]
        public void ExplorationNodeUsesDistinctCopyAndRetainsExistingEventOutcome()
        {
            var node = new CourierRouteNodeDef { resolution = CourierResolutionKind.Event };
            JourneyLocationInteractionDefinition definition =
                JourneyLocationInteractionCatalog.For(node, ExpeditionNodeKind.Other);

            CourierLocationOutcome primary = definition.BuildOutcome(true);
            CourierLocationOutcome secondary = definition.BuildOutcome(false);

            Assert.That(definition.Eyebrow, Is.EqualTo("EXPLORATION"));
            Assert.That(definition.ShowSecondaryButton, Is.True);
            Assert.That(primary.performance, Is.EqualTo(2));
            Assert.That(secondary.performance, Is.EqualTo(1));
            Assert.That(secondary.dayDelta, Is.EqualTo(1));
        }
    }
}
