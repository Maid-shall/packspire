namespace Packspire
{
    public sealed partial class JourneyTravelGameplayPrototype
    {
        private void ShowEvent(CourierRouteNodeDef node)
        {
            SaveJourneyStage(JourneyResumeStage.Location);
            JourneyLocationInteractionDefinition interaction =
                JourneyLocationInteractionCatalog.For(node, arrivalExpeditionNode?.kind);
            SetPhase(Phase.Event);
            walker.SetJourneyWalking(false);
            eventEyebrow.text = interaction.Eyebrow;
            eventTitle.text = node.resolutionTitle;
            eventText.text = node.resolutionText + "\n\n" +
                (string.IsNullOrWhiteSpace(node.condition) ? "" : node.condition);
            eventA.text = interaction.PrimaryButton;
            eventB.text = interaction.SecondaryButton;
            eventB.EnableInClassList("is-hidden", !interaction.ShowSecondaryButton);
            eventB.SetEnabled(interaction.ShowSecondaryButton);
            eventA.Focus();
        }

        private void ResolveEvent(bool primary)
        {
            if (phase != Phase.Event || arrivalNode == null) return;
            JourneyLocationInteractionDefinition interaction =
                JourneyLocationInteractionCatalog.For(
                    arrivalNode,
                    arrivalExpeditionNode?.kind);
            if (!primary && !interaction.ShowSecondaryButton) return;
            ResolveRoute(interaction.BuildOutcome(primary));
        }
    }
}
