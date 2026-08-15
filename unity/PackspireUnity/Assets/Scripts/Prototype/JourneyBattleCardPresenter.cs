using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire
{
    /// <summary>
    /// Thin journey adapter for the product battle-card visual. The card artwork and
    /// docket template stay shared with the main battle screen; only the hand-slot
    /// scale and interaction belong to the journey presentation.
    /// </summary>
    internal static class JourneyBattleCardPresenter
    {
        private static VisualTreeAsset template;

        public static void Populate(Button slot, CardInstance card, bool affordable)
        {
            if (slot == null || card == null)
            {
                return;
            }

            RemoveStateClasses(slot);
            slot.Clear();
            slot.text = string.Empty;
            slot.AddToClassList("ps-battle-card");
            slot.AddToClassList("ps-docket-card");
            slot.AddToClassList("ps-docket-combat");
            slot.AddToClassList("ps-docket-" + PresentationKind(card));
            slot.EnableInClassList("ps-docket-authorized", affordable);
            slot.EnableInClassList("ps-docket-held", !affordable);
            slot.EnableInClassList("ps-battle-card-disabled", !affordable);

            if (template == null)
            {
                template = Resources.Load<VisualTreeAsset>("UI/PackspireDocketCard");
            }

            if (template == null)
            {
                return;
            }

            template.CloneTree(slot);
            SetLabel(slot, "docket-main-name", card.name);
            SetLabel(slot, "docket-receipt-cost", card.cost.ToString());
            SetLabel(slot, "docket-lock", affordable ? string.Empty : "LOW EN");

            VisualElement effectHost = slot.Q<VisualElement>("docket-main-text");
            if (effectHost != null)
            {
                effectHost.Clear();
                Label effect = new Label(string.IsNullOrWhiteSpace(card.text) ? "効果なし" : card.text)
                {
                    pickingMode = PickingMode.Ignore
                };
                effect.AddToClassList("ps-docket__effect-copy");
                effectHost.Add(effect);
            }

            VisualElement artwork = slot.Q<VisualElement>("docket-art");
            Sprite sprite = PackspireUiFoundation.BattleCardArtwork(card.id);
            if (artwork != null && sprite != null)
            {
                artwork.style.backgroundImage = new StyleBackground(sprite);
            }
        }

        private static void SetLabel(VisualElement root, string name, string value)
        {
            Label label = root.Q<Label>(name);
            if (label != null)
            {
                label.text = value ?? string.Empty;
            }
        }

        private static string PresentationKind(CardInstance card)
        {
            if (card.type == CardType.Attack || card.damage > 0)
            {
                return "attack";
            }

            if (card.block > 0 || card.heal > 0)
            {
                return "defense";
            }

            return "technique";
        }

        private static void RemoveStateClasses(VisualElement slot)
        {
            slot.RemoveFromClassList("ps-docket-attack");
            slot.RemoveFromClassList("ps-docket-defense");
            slot.RemoveFromClassList("ps-docket-technique");
            slot.RemoveFromClassList("ps-docket-consumable");
            slot.RemoveFromClassList("ps-docket-authorized");
            slot.RemoveFromClassList("ps-docket-held");
            slot.RemoveFromClassList("ps-battle-card-disabled");
        }
    }
}
