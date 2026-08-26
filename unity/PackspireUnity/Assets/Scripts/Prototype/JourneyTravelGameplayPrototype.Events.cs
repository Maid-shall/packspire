namespace Packspire
{
    public sealed partial class JourneyTravelGameplayPrototype
    {
        private void ShowEvent(CourierRouteNodeDef node)
        {
            JourneyLocationInteractionDefinition interaction =
                JourneyLocationInteractionCatalog.For(node);
            SetPhase(Phase.Event);
            walker.SetJourneyWalking(false);
            eventEyebrow.text = interaction.Eyebrow;
            eventTitle.text = node.resolutionTitle;
            eventText.text = node.resolutionText + "\n\n" +
                (string.IsNullOrWhiteSpace(node.condition) ? "" : node.condition);
            eventA.text = interaction.PrimaryButton;
            eventB.text = interaction.SecondaryButton;
        }

        private void ResolveEvent(bool primary)
        {
            if (phase != Phase.Event || arrivalNode == null) return;
            JourneyLocationInteractionDefinition interaction =
                JourneyLocationInteractionCatalog.For(arrivalNode);
            ResolveRoute(interaction.BuildOutcome(primary));
        }
    }
}
