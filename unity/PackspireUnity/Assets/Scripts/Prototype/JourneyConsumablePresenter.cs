using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Packspire
{
    /// <summary>
    /// Projects the expedition consumable inventory into four compact quick slots.
    /// Fixed structure and appearance remain in UXML/USS; this presenter only binds
    /// inventory data, semantic effect classes, input, and hover copy.
    /// </summary>
    public sealed class JourneyConsumablePresenter
    {
        public const int SlotCount = 4;

        private static readonly string[] EffectClasses =
        {
            "consumable--heal", "consumable--guard", "consumable--damage",
            "consumable--energy", "consumable--attack-boost",
            "consumable--guard-boost", "consumable--delay", "consumable--route"
        };

        private readonly Button[] buttons = new Button[SlotCount];
        private readonly Label[] counts = new Label[SlotCount];
        private readonly string[] boundIds = new string[SlotCount];
        private readonly Action<string> use;
        private readonly Func<ConsumableContent, bool> canUse;
        private readonly VisualElement detail;
        private readonly Label detailName;
        private readonly Label detailEffect;
        private readonly List<string> distinctIds = new List<string>(SlotCount);
        private readonly List<int> distinctCounts = new List<int>(SlotCount);
        private int hoveredIndex = -1;

        public bool IsPointerOverSlot { get; private set; }

        public void ClearHover()
        {
            hoveredIndex = -1;
            IsPointerOverSlot = false;
            detail.RemoveFromClassList("is-visible");
        }

        public JourneyConsumablePresenter(
            VisualElement root,
            Action<string> use,
            Func<ConsumableContent, bool> canUse)
        {
            this.use = use ?? throw new ArgumentNullException(nameof(use));
            this.canUse = canUse ?? throw new ArgumentNullException(nameof(canUse));
            detail = Require<VisualElement>(root, "journey-consumable-detail");
            detailName = Require<Label>(root, "journey-consumable-detail-name");
            detailEffect = Require<Label>(root, "journey-consumable-detail-effect");

            for (int index = 0; index < SlotCount; index++)
            {
                int captured = index;
                buttons[index] = Require<Button>(root, $"journey-consumable-{index}");
                counts[index] = Require<Label>(root, $"journey-consumable-count-{index}");
                buttons[index].clicked += () => Use(captured);
                buttons[index].RegisterCallback<PointerEnterEvent>(_ => ShowDetail(captured));
                buttons[index].RegisterCallback<PointerLeaveEvent>(_ => HideDetail(captured));
            }
        }

        public void Refresh(RunState run, bool commandsAvailable)
        {
            BuildDistinctInventory(run?.consumables);
            for (int index = 0; index < SlotCount; index++)
            {
                ClearEffectClasses(buttons[index]);
                bool present = index < distinctIds.Count;
                buttons[index].EnableInClassList("is-empty", !present);
                buttons[index].EnableInClassList("is-unusable", false);
                buttons[index].SetEnabled(present);
                buttons[index].tooltip = string.Empty;
                boundIds[index] = present ? distinctIds[index] : string.Empty;
                counts[index].text = present && distinctCounts[index] > 1
                    ? $"×{distinctCounts[index]}"
                    : string.Empty;
                counts[index].EnableInClassList(
                    "is-hidden",
                    !present || distinctCounts[index] <= 1);
                if (!present) continue;

                ConsumableContent definition = ConsumableSystem.Definition(boundIds[index]);
                if (definition == null) continue;
                buttons[index].AddToClassList(EffectClass(definition.effect));
                buttons[index].tooltip = $"{definition.name}\n{definition.description}";
                buttons[index].EnableInClassList(
                    "is-unusable",
                    !commandsAvailable || !canUse(definition));
            }

            if (hoveredIndex >= 0)
            {
                if (hoveredIndex < distinctIds.Count) ShowDetail(hoveredIndex);
                else HideDetail(hoveredIndex);
            }
        }

        private void BuildDistinctInventory(List<string> inventory)
        {
            distinctIds.Clear();
            distinctCounts.Clear();
            if (inventory == null) return;
            for (int itemIndex = 0; itemIndex < inventory.Count; itemIndex++)
            {
                string id = inventory[itemIndex];
                int existing = distinctIds.IndexOf(id);
                if (existing >= 0)
                {
                    distinctCounts[existing]++;
                    continue;
                }
                if (distinctIds.Count >= SlotCount) continue;
                distinctIds.Add(id);
                distinctCounts.Add(1);
            }
        }

        private void Use(int index)
        {
            if (index < 0 || index >= SlotCount || string.IsNullOrEmpty(boundIds[index])) return;
            use(boundIds[index]);
        }

        private void ShowDetail(int index)
        {
            if (index < 0 || index >= SlotCount || string.IsNullOrEmpty(boundIds[index])) return;
            ConsumableContent definition = ConsumableSystem.Definition(boundIds[index]);
            if (definition == null)
            {
                HideDetail(index);
                return;
            }
            hoveredIndex = index;
            IsPointerOverSlot = true;
            detailName.text = definition.name;
            detailEffect.text = definition.description;
            detail.AddToClassList("is-visible");
        }

        private void HideDetail(int index)
        {
            if (hoveredIndex != index) return;
            ClearHover();
        }

        private static string EffectClass(ConsumableEffectType effect)
        {
            int index = (int)effect;
            return index >= 0 && index < EffectClasses.Length
                ? EffectClasses[index]
                : "consumable--unknown";
        }

        private static void ClearEffectClasses(VisualElement element)
        {
            foreach (string className in EffectClasses) element.RemoveFromClassList(className);
            element.RemoveFromClassList("consumable--unknown");
        }

        private static T Require<T>(VisualElement root, string name) where T : VisualElement
        {
            T element = root?.Q<T>(name);
            if (element == null)
                throw new InvalidOperationException($"Journey consumable view is missing '{name}'.");
            return element;
        }
    }
}
