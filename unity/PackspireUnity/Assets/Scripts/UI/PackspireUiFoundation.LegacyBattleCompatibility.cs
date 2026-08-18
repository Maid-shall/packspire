using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire
{
    /// <summary>
    /// Compile-time boundary for the retired standalone battle screen. Shared combat
    /// simulation remains available, while the former product UI is excluded unless
    /// PACKSPIRE_LEGACY_BATTLE_UI is explicitly enabled for archival QA.
    /// </summary>
    public sealed partial class PackspireUiFoundation
    {
        private bool battleUiBuilt;
        private bool battleInputLocked;
        private Texture2D battleIconDamage;
        private Texture2D battleIconBlock;
        private Texture2D battleIconHeal;
        private Texture2D battleIconEnergy;
        private Texture2D battleIconClaw;

        private void EnsureBattleAssets()
        {
            if (battleIconDamage != null) return;
            battleIconDamage = PackspireResources.Load<Texture2D>("Art/Battle/Icons/icon-damage");
            battleIconBlock = PackspireResources.Load<Texture2D>("Art/Battle/Icons/icon-block");
            battleIconHeal = PackspireResources.Load<Texture2D>("Art/Battle/Icons/icon-heal");
            battleIconEnergy = PackspireResources.Load<Texture2D>("Art/Battle/Icons/icon-energy");
            battleIconClaw = PackspireResources.Load<Texture2D>("Art/Battle/Icons/icon-claw");
        }

        private void PopulateBattleCard(
            VisualElement slot,
            CardInstance card,
            RunState run,
            bool affordable)
        {
            ApplyBattleCardPresentation(slot, card, run);
            ItemInstance sourceItem = run.inventory.FirstOrDefault(x => x.uid == card.sourceItemUid);
            string sourceName = card.source;
            if (sourceItem != null &&
                GameCatalog.Items.TryGetValue(sourceItem.templateId, out ItemDef itemDefinition))
                sourceName = itemDefinition.name;
            int maximumDurability = sourceItem != null &&
                GameCatalog.Items.TryGetValue(sourceItem.templateId, out ItemDef durabilityItem)
                    ? durabilityItem.baseDurability
                    : 6;
            string durability = sourceItem != null
                ? $"DUR {sourceItem.durability}/{maximumDurability}"
                : card.roleCard ? "ROLE" : "BASIC";
            PopulateDocketCard(
                slot,
                card,
                BattleCardDisplayTextWithKeywords(card),
                sourceName,
                durability,
                affordable,
                false);
        }

        private static string BattleCardDisplayTextWithKeywords(CardInstance card)
        {
            if (card == null) return string.Empty;
            string text = BattleCardDisplayText(card);
            var keywords = new List<string>();
            if (card.innate) keywords.Add("開始手札");
            if (card.retain) keywords.Add("保持");
            if (card.ethereal) keywords.Add("揮発");
            if (card.unplayable) keywords.Add("使用不可");
            if (card.afterUse == BattleCardAfterUse.ExhaustBattle) keywords.Add("戦闘除外");
            if (card.afterUse == BattleCardAfterUse.RemoveExpedition) keywords.Add("遠征除外");
            return keywords.Count == 0 ? text : $"{text}\n〈{string.Join("・", keywords)}〉";
        }

        private static string BattleCardDisplayText(CardInstance card)
        {
            if (card == null || card.damage <= 0 || card.damageMode == DamageResolutionMode.Fixed)
                return card?.text ?? string.Empty;
            int modifier = card.damage - 7;
            string formula = $"2D6 {(modifier >= 0 ? "+ " : "− ")}{Mathf.Abs(modifier)} ダメージ";
            return card.text.Replace($"{card.damage}ダメージ", formula);
        }

        private void BuildBattle()
        {
            battleUiBuilt = true;
            battleInputLocked = true;
            Debug.LogWarning(
                "Standalone battle UI is retired. Battles run inside the seamless journey scene.");

            VisualElement notice = Container("ps-retired-battle");
            notice.Add(new Label("戦闘はシームレス遠征へ統合されました。"));
            screenRoot?.Add(notice);
        }

        private void SuspendBattleUi()
        {
            battleInputLocked = false;
        }

        public void RefreshBattleUi()
        {
            // Kept only for legacy PackspireGame command compatibility.
        }

        public void PlayBattleActionFx(BattleActionFx fx)
        {
            // Retired standalone presentation. The seamless journey owns battle FX.
        }
    }
}
