using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire.Tests
{
    public sealed class PackingWorkspaceTests
    {
        static VisualElement View(string name)
        {
            var tree = PackspireResources.Load<VisualTreeAsset>("UI/" + name);
            Assert.That(tree, Is.Not.Null);
            return tree.CloneTree();
        }

        [Test]
        public void EffectsHaveIndependentVerticalScrollRegions()
        {
            var view = View("PackspirePackingView");
            var inventory = view.Q<ScrollView>("packing-equip-scroll");
            var context = view.Q<ScrollView>("packing-right-scroll");
            var links = view.Q<ScrollView>("packing-links-scroll");
            var colors = view.Q<ScrollView>("packing-colors-scroll");
            foreach (var scroll in new[] { inventory, context, links, colors })
            {
                Assert.That(scroll, Is.Not.Null);
                Assert.That(scroll.mode, Is.EqualTo(ScrollViewMode.Vertical));
            }
            Assert.That(links.parent, Is.Not.SameAs(colors.parent));
            Assert.That(context.parent.ClassListContains("ps-packing-preserve-selection"), Is.True);
        }

        [Test]
        public void BoardPanIsSeparateFromTheEquipmentAndEffects()
        {
            var view = View("PackspirePackingView");
            var board = view.Q<ScrollView>("packing-board-scroll");
            Assert.That(board.mode, Is.EqualTo(ScrollViewMode.VerticalAndHorizontal));
            Assert.That(board.Contains(view.Q("packing-board-host")), Is.True);
            Assert.That(board.Contains(view.Q("packing-right-scroll")), Is.False);
            Assert.That(board.Contains(view.Q("packing-links-scroll")), Is.False);
            Assert.That(view.Q<Button>("packing-zoom-in"), Is.Not.Null);
            Assert.That(view.Q<Button>("packing-fit"), Is.Not.Null);
        }

        [Test]
        public void PackingDoesNotBindACharacterToTheLoadout()
        {
            var view = View("PackspirePackingView");
            Assert.That(view.Q("packing-courier-art"), Is.Null);
            Assert.That(view.Q("packing-courier-name"), Is.Null);
            Assert.That(view.Q<TextField>("packing-search"), Is.Not.Null);
            Assert.That(view.Q<Toggle>("packing-unplaced"), Is.Not.Null);
            Assert.That(view.Q<Button>("packing-save"), Is.Not.Null);
            Assert.That(view.Q<Button>("packing-loadouts"), Is.Not.Null);
        }

        [Test]
        public void FormulaAndCardPopupsOwnTheirFixedStructureInUxml()
        {
            var formula = View("PackspirePackingFormulaPopup");
            Assert.That(formula.Q<TextField>("packing-formula-name"), Is.Not.Null);
            Assert.That(formula.Q<ScrollView>("packing-formula-components"), Is.Not.Null);
            Assert.That(formula.Q<Button>("packing-formula-close"), Is.Not.Null);
            var cards = View("PackspirePackingCardsPopup");
            Assert.That(cards.Q<ScrollView>("packing-cards-combat"), Is.Not.Null);
            Assert.That(cards.Q<ScrollView>("packing-cards-seals"), Is.Not.Null);
            Assert.That(cards.Q<Button>("packing-cards-close"), Is.Not.Null);
        }

        [Test]
        public void BoardMaterialResourcesAreAvailable()
        {
            Assert.That(PackspireResources.Load<Texture2D>("Art/UI/Packing/packing-board-frame"), Is.Not.Null);
            Assert.That(PackspireResources.Load<Texture2D>("Art/UI/Packing/packing-cell-plate"), Is.Not.Null);
        }
    }
}
