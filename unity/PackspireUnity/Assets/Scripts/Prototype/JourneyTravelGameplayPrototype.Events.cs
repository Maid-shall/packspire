using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire
{
    public sealed partial class JourneyTravelGameplayPrototype
    {
        private VisualElement eventScrim;
        private VisualElement eventPanel;

        private void BindEventUi(VisualElement root)
        {
            eventScrim = root.Q<VisualElement>("journey-event-scrim");
            eventPanel = root.Q<VisualElement>("journey-event");
            ClearEventPresentation();
        }

        private void ShowEvent(CourierRouteNodeDef node)
        {
            SaveJourneyStage(JourneyResumeStage.Location);
            JourneyLocationInteractionDefinition interaction =
                JourneyLocationInteractionCatalog.For(node, arrivalExpeditionNode?.kind);
            SetPhase(Phase.Event);
            walker.SetJourneyWalking(false);
            ApplyEventPresentation(interaction);
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
            parcelRenderer.enabled = false;
            ClearEventPresentation();
            ResolveRoute(interaction.BuildOutcome(primary));
        }

        private void ApplyEventPresentation(
            JourneyLocationInteractionDefinition interaction)
        {
            ClearEventPresentation();
            bool overlay =
                interaction.Presentation == JourneyLocationPresentation.Overlay;
            screen.EnableInClassList("event--overlay", overlay);
            screen.EnableInClassList("event--inline", !overlay);
            eventScrim?.BringToFront();
            eventPanel?.BringToFront();
            parcelRenderer.enabled = !overlay;
            if (!overlay)
                parcelRenderer.transform.position =
                    new Vector3(2.2f, -1.3f, 0f);
        }

        private void ClearEventPresentation()
        {
            screen?.RemoveFromClassList("event--overlay");
            screen?.RemoveFromClassList("event--inline");
        }
    }
}
