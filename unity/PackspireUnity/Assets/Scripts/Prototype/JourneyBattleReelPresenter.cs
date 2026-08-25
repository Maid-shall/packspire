using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire
{
    /// <summary>
    /// Owns the retained UI state of the seamless-battle reel. Battle scheduling
    /// remains in RealtimeBattleController; this class only projects that state
    /// into the fixed UXML slots.
    /// </summary>
    internal sealed class JourneyBattleReelPresenter
    {
        internal const int ThreatSlotCount = 10;
        internal const int SupplySlotCount = 4;

        private const float CardHeight = 68f;
        private const float SupplyMarkerHeight = 52f;
        private const float FallbackTravel = 470f;
        private const float ChainTileStep = 53f;

        private sealed class ThreatSlot
        {
            public readonly VisualElement Root;
            public readonly Image Art;
            public readonly VisualElement Icon;
            public readonly VisualElement Value;
            public readonly Image[] ValueGlyphs;
            public readonly Image BundleCount;

            public ThreatSlot(VisualElement root)
            {
                Root = root ?? throw new ArgumentNullException(nameof(root));
                Art = RequireClass<Image>(root, "ps-journey__reel-card-art");
                Icon = RequireClass<VisualElement>(root, "ps-journey__reel-card-icon");
                Value = RequireClass<VisualElement>(root, "ps-journey__reel-card-value");
                ValueGlyphs = new[]
                {
                    RequireClass<Image>(root, "ps-journey__reel-value-glyph--0"),
                    RequireClass<Image>(root, "ps-journey__reel-value-glyph--1"),
                    RequireClass<Image>(root, "ps-journey__reel-value-glyph--2")
                };
                BundleCount = RequireClass<Image>(root, "ps-journey__reel-bundle-count");

                // The oversized printed value is a background motif in layout C.
                Value.PlaceBehind(Icon);
            }
        }

        private sealed class SupplySlot
        {
            public readonly VisualElement Root;
            public readonly Label EnergyCount;
            public readonly Label DrawCount;

            public SupplySlot(VisualElement root)
            {
                Root = root ?? throw new ArgumentNullException(nameof(root));
                EnergyCount = RequireClass<Label>(root, "ps-journey__reel-resource-energy-count");
                DrawCount = RequireClass<Label>(root, "ps-journey__reel-resource-draw-count");
            }
        }

        private readonly double displayHorizon;
        private readonly Func<RealtimeEnemyActionPreview, Sprite> actorSprite;
        private readonly Texture2D numeralAtlas;
        private readonly List<RealtimeEnemyActionPreview> actionPreviews = new(8);
        private readonly List<RealtimeSupplyPulsePreview> supplyPreviews = new(4);
        private readonly ThreatSlot[] threatSlots = new ThreatSlot[ThreatSlotCount];
        private readonly SupplySlot[] supplySlots = new SupplySlot[SupplySlotCount];
        private readonly VisualElement chainTrack;
        private readonly VisualElement slotsViewport;
        private readonly VisualElement nowMarker;

        public JourneyBattleReelPresenter(
            VisualElement root,
            double displayHorizon,
            Texture2D numeralAtlas,
            Func<RealtimeEnemyActionPreview, Sprite> actorSprite)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));
            this.displayHorizon = Math.Max(.1d, displayHorizon);
            this.numeralAtlas = numeralAtlas;
            this.actorSprite = actorSprite ?? throw new ArgumentNullException(nameof(actorSprite));

            chainTrack = RequireName<VisualElement>(root, "journey-reel-chain-track");
            slotsViewport = RequireName<VisualElement>(root, "journey-reel-slots");
            nowMarker = RequireName<VisualElement>(root, "journey-reel-now");

            for (int index = 0; index < threatSlots.Length; index++)
                threatSlots[index] = new ThreatSlot(
                    RequireName<VisualElement>(root, $"journey-threat-slot-{index}"));

            for (int index = 0; index < supplySlots.Length; index++)
            {
                supplySlots[index] = new SupplySlot(
                    RequireName<VisualElement>(root, $"journey-supply-slot-{index}"));
                // Supply bands share the time axis with tickets but stay behind them.
                supplySlots[index].Root.SendToBack();
            }
        }

        public void Refresh(RealtimeBattleController battle)
        {
            if (battle == null) throw new ArgumentNullException(nameof(battle));

            float travel = MeasureTravel();
            float pixelsPerSecond = travel / (float)displayHorizon;
            float chainOffset = (float)(battle.Time * pixelsPerSecond % ChainTileStep);
            chainTrack.style.translate = new Translate(
                new Length(0f, LengthUnit.Pixel),
                new Length(chainOffset, LengthUnit.Pixel));

            Clear();
            battle.GetUpcomingActions(actionPreviews);
            int threatIndex = 0;
            foreach (RealtimeEnemyActionPreview preview in actionPreviews)
            {
                if (threatIndex >= threatSlots.Length) break;
                if (preview.TimeUntilStart > displayHorizon) continue;
                ThreatSlot slot = threatSlots[threatIndex++];
                PlaceThreat(slot, preview.TimeUntilStart, travel);
                BindThreat(slot, preview);
            }

            battle.GetUpcomingSupplyPulses(supplyPreviews, displayHorizon);
            int supplyIndex = 0;
            foreach (RealtimeSupplyPulsePreview preview in supplyPreviews)
            {
                if (supplyIndex >= supplySlots.Length) break;
                SupplySlot slot = supplySlots[supplyIndex++];
                PlaceSupply(slot, preview.TimeUntil, travel);
                BindSupply(slot, preview);
            }
        }

        public void Clear()
        {
            foreach (ThreatSlot slot in threatSlots)
            {
                slot.Root.RemoveFromClassList("slot--occupied");
                slot.Root.RemoveFromClassList("slot--attack");
                slot.Root.RemoveFromClassList("slot--reaction-attack");
                slot.Root.RemoveFromClassList("slot--defense");
                slot.Root.RemoveFromClassList("slot--energy");
                slot.Root.RemoveFromClassList("slot--imminent");
                slot.Root.RemoveFromClassList("slot--with-energy");
                slot.Root.RemoveFromClassList("slot--bundle");
                ClearValue(slot);
                slot.BundleCount.image = null;
                slot.BundleCount.style.display = DisplayStyle.None;
                slot.Art.sprite = null;
                slot.Art.image = null;
                slot.Root.style.top = 0f;
            }

            foreach (SupplySlot slot in supplySlots)
            {
                slot.Root.RemoveFromClassList("slot--occupied");
                slot.Root.RemoveFromClassList("slot--energy");
                slot.Root.RemoveFromClassList("slot--draw");
                slot.Root.RemoveFromClassList("slot--energy-stack");
                slot.Root.RemoveFromClassList("slot--draw-stack");
                slot.EnergyCount.text = string.Empty;
                slot.DrawCount.text = string.Empty;
                slot.Root.style.top = 0f;
            }
        }

        private float MeasureTravel()
        {
            if (slotsViewport.resolvedStyle.height <= CardHeight) return FallbackTravel;
            float measured = nowMarker.worldBound.yMin - slotsViewport.worldBound.yMin - CardHeight;
            return measured > 0f ? measured : FallbackTravel;
        }

        private void PlaceThreat(ThreatSlot slot, double timeUntil, float travel)
        {
            float pixelsPerSecond = travel / (float)displayHorizon;
            float top = travel - (float)timeUntil * pixelsPerSecond;
            slot.Root.style.top = Mathf.Clamp(top, 0f, travel + CardHeight * 1.35f);
        }

        private void PlaceSupply(SupplySlot slot, double timeUntil, float travel)
        {
            float pixelsPerSecond = travel / (float)displayHorizon;
            float top = travel - (float)timeUntil * pixelsPerSecond +
                CardHeight - SupplyMarkerHeight * .5f;
            slot.Root.style.top = Mathf.Clamp(top, -SupplyMarkerHeight, travel + CardHeight);
        }

        private void BindThreat(ThreatSlot slot, RealtimeEnemyActionPreview preview)
        {
            bool defense = preview.Kind == RealtimeEnemyActionKind.Guard;
            bool reaction = preview.Kind == RealtimeEnemyActionKind.JumpReaction ||
                preview.Kind == RealtimeEnemyActionKind.BraceReaction;
            slot.Root.AddToClassList("slot--occupied");
            slot.Root.EnableInClassList("slot--attack", !defense);
            slot.Root.EnableInClassList("slot--reaction-attack", reaction);
            slot.Root.EnableInClassList("slot--defense", defense);
            slot.Root.EnableInClassList(
                "slot--imminent",
                preview.Telegraphing || preview.TimeUntil <= 1.1d);
            slot.Art.scaleMode = ScaleMode.ScaleAndCrop;
            slot.Art.sprite = actorSprite(preview);
            SetValue(slot, preview.Damage.ToString());

            if (preview.HitCount <= 1) return;
            slot.Root.AddToClassList("slot--bundle");
            SetGlyph(slot.BundleCount, preview.HitCount.ToString()[0]);
        }

        private static void BindSupply(SupplySlot slot, RealtimeSupplyPulsePreview preview)
        {
            slot.Root.AddToClassList("slot--occupied");
            slot.Root.EnableInClassList("slot--energy", preview.EnergyDelta > 0);
            slot.Root.EnableInClassList("slot--draw", preview.DrawCount > 0);
            slot.Root.EnableInClassList("slot--energy-stack", preview.EnergyDelta > 1);
            slot.Root.EnableInClassList("slot--draw-stack", preview.DrawCount > 1);
            slot.EnergyCount.text = Amount("エナジー", preview.EnergyDelta);
            slot.DrawCount.text = Amount("手札", preview.DrawCount);
        }

        private static string Amount(string label, int amount) =>
            amount > 0 ? $"{label}  +{amount}" : string.Empty;

        private void SetValue(ThreatSlot slot, string value)
        {
            ClearValue(slot);
            if (string.IsNullOrEmpty(value)) return;

            int glyphCount = Mathf.Min(value.Length, slot.ValueGlyphs.Length);
            slot.Value.AddToClassList(glyphCount switch
            {
                1 => "value--single",
                2 => "value--double",
                _ => "value--triple"
            });
            slot.Value.tooltip = value;
            for (int index = 0; index < glyphCount; index++)
                SetGlyph(slot.ValueGlyphs[index], value[index]);
        }

        private static void ClearValue(ThreatSlot slot)
        {
            slot.Value.RemoveFromClassList("value--single");
            slot.Value.RemoveFromClassList("value--double");
            slot.Value.RemoveFromClassList("value--triple");
            slot.Value.tooltip = string.Empty;
            foreach (Image glyph in slot.ValueGlyphs)
            {
                glyph.image = null;
                glyph.style.display = DisplayStyle.None;
            }
        }

        private void SetGlyph(Image glyph, char character)
        {
            int atlasIndex = character >= '0' && character <= '9'
                ? character - '0'
                : character switch
                {
                    '+' => 10,
                    '-' => 11,
                    '×' => 12,
                    _ => -1
                };
            if (numeralAtlas == null || atlasIndex < 0)
            {
                glyph.image = null;
                glyph.style.display = DisplayStyle.None;
                return;
            }

            const int atlasColumns = 4;
            const int atlasRows = 4;
            const int pixelInset = 2;
            int column = atlasIndex % atlasColumns;
            int row = atlasIndex / atlasColumns;
            int xMin = Mathf.RoundToInt(column * numeralAtlas.width / (float)atlasColumns) + pixelInset;
            int xMax = Mathf.RoundToInt((column + 1) * numeralAtlas.width / (float)atlasColumns) - pixelInset;
            int yMin = numeralAtlas.height -
                Mathf.RoundToInt((row + 1) * numeralAtlas.height / (float)atlasRows) + pixelInset;
            int yMax = numeralAtlas.height -
                Mathf.RoundToInt(row * numeralAtlas.height / (float)atlasRows) - pixelInset;
            glyph.image = numeralAtlas;
            glyph.uv = new Rect(
                xMin / (float)numeralAtlas.width,
                yMin / (float)numeralAtlas.height,
                (xMax - xMin) / (float)numeralAtlas.width,
                (yMax - yMin) / (float)numeralAtlas.height);
            glyph.scaleMode = ScaleMode.ScaleToFit;
            glyph.style.display = DisplayStyle.Flex;
        }

        private static T RequireName<T>(VisualElement root, string name) where T : VisualElement
        {
            T element = root.Q<T>(name);
            return element ?? throw new InvalidOperationException(
                $"Journey battle reel is missing required {typeof(T).Name} '{name}'.");
        }

        private static T RequireClass<T>(VisualElement root, string className) where T : VisualElement
        {
            T element = root.Q<T>(className: className);
            return element ?? throw new InvalidOperationException(
                $"Journey battle reel slot is missing required {typeof(T).Name} '.{className}'.");
        }
    }
}
