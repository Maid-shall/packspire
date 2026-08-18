using System.Runtime.CompilerServices;
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
        private static readonly ConditionalWeakTable<VisualElement, BattleCardView> Views = new();

        public static void Populate(Button slot, CardInstance card, bool affordable)
        {
            if (slot == null || card == null)
            {
                return;
            }

            slot.text = string.Empty;
            slot.AddToClassList("ps-battle-card");
            slot.AddToClassList("ps-docket-card");
            slot.AddToClassList("ps-docket-combat");
            BattleCardView view = Views.GetValue(slot, static element => new BattleCardView(element));
            view.Bind(new BattleCardViewModel(
                card.name,
                card.cost.ToString(),
                card.text,
                PresentationKind(card),
                affordable ? string.Empty : "LOW EN",
                PackspireUiFoundation.BattleCardArtwork(card.id),
                affordable));
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
    }
}
