using System;
using System.Collections.Generic;
using System.Linq;

namespace Packspire
{
    public enum JourneyBattleRewardKind
    {
        Consumable,
        ExpeditionResource,
        Equipment
    }

    public enum JourneyBattleRewardTier
    {
        Normal,
        Danger,
        Elite,
        Boss
    }

    public sealed class JourneyBattleRewardCandidate
    {
        public JourneyBattleRewardKind kind;
        public string contentId = string.Empty;
        public string name = string.Empty;
        public string description = string.Empty;
        public string meta = string.Empty;
        public int amount = 1;
    }

    public sealed class JourneyBattleRewardOffer
    {
        public JourneyBattleRewardTier tier;
        public JourneyBattleRewardCandidate[] candidates = Array.Empty<JourneyBattleRewardCandidate>();
        public bool equipmentOffered;
        public bool pityTriggered;
    }

    /// <summary>
    /// Builds the three compact choices shown after a journey battle. Equipment is
    /// deliberately one possible result, while a usable expedition reward is always
    /// present. Chance values live here so later balance changes do not touch the UI.
    /// </summary>
    public static class JourneyBattleRewardSystem
    {
        public const int NormalEquipmentChancePercent = 35;
        public const int DangerEquipmentChancePercent = 60;
        public const int EquipmentOfferPityBattles = 3;

        private static readonly string[] BasicConsumableIds =
        {
            "heal", "guard", "fire", "energy"
        };

        private static readonly string[] SpecialistConsumableIds =
        {
            "assault_incense", "ward_seal", "delay_seal"
        };

        private const string RouteStampId = "route_stamp";

        public static JourneyBattleRewardTier ResolveTier(
            JourneyEnemyPopulationClass populationClass,
            int routeRisk)
        {
            if (populationClass == JourneyEnemyPopulationClass.Boss)
                return JourneyBattleRewardTier.Boss;
            if (populationClass == JourneyEnemyPopulationClass.Elite)
                return JourneyBattleRewardTier.Elite;
            if (populationClass == JourneyEnemyPopulationClass.Enhanced || routeRisk >= 2)
                return JourneyBattleRewardTier.Danger;
            return JourneyBattleRewardTier.Normal;
        }

        public static JourneyBattleRewardOffer CreateOffer(
            RunState run,
            JourneyBattleRewardTier tier,
            Random random = null)
        {
            if (run == null) throw new ArgumentNullException(nameof(run));
            random ??= new Random(StableSeed(run));

            bool pityTriggered =
                run.journeyBattlesWithoutEquipmentOffer >= EquipmentOfferPityBattles - 1;
            int equipmentChance = tier switch
            {
                JourneyBattleRewardTier.Boss => 100,
                JourneyBattleRewardTier.Elite => 100,
                JourneyBattleRewardTier.Danger => DangerEquipmentChancePercent,
                _ => NormalEquipmentChancePercent
            };
            bool wantsEquipment = pityTriggered || random.Next(100) < equipmentChance;
            string equipmentId = wantsEquipment
                ? SelectEquipmentId(run, tier, random)
                : string.Empty;
            bool equipmentOffered = !string.IsNullOrEmpty(equipmentId);

            int bundleAmount = tier switch
            {
                JourneyBattleRewardTier.Boss => 3,
                JourneyBattleRewardTier.Elite => 2,
                JourneyBattleRewardTier.Danger => 2,
                _ => 1
            };

            var candidates = new List<JourneyBattleRewardCandidate>(3)
            {
                BuildConsumable(PickExisting(BasicConsumableIds, random), bundleAmount),
                BuildConsumable(RouteStampId, tier >= JourneyBattleRewardTier.Elite ? 2 : 1, true)
            };
            candidates.Add(equipmentOffered
                ? BuildEquipment(equipmentId)
                : BuildConsumable(PickExisting(SpecialistConsumableIds, random), bundleAmount));

            if (equipmentOffered)
                run.journeyBattlesWithoutEquipmentOffer = 0;
            else
                run.journeyBattlesWithoutEquipmentOffer++;

            return new JourneyBattleRewardOffer
            {
                tier = tier,
                candidates = candidates.ToArray(),
                equipmentOffered = equipmentOffered,
                pityTriggered = pityTriggered && equipmentOffered
            };
        }

        public static string Grant(
            RunState run,
            JourneyBattleRewardCandidate candidate,
            Random random = null)
        {
            if (run == null || candidate == null) return string.Empty;
            int amount = Math.Max(1, candidate.amount);
            if (candidate.kind == JourneyBattleRewardKind.Equipment)
            {
                if (!GameCatalog.Items.ContainsKey(candidate.contentId)) return string.Empty;
                var item = new ItemInstance(candidate.contentId) { identified = false };
                StorageFormulaSystem.EnsureItemRolled(item, random);
                run.lootBag.Add(item);
                return $"{candidate.name}を戦利品バッグへ収めた。";
            }

            for (int index = 0; index < amount; index++)
                run.consumables.Add(candidate.contentId);
            return amount > 1
                ? $"{candidate.name} ×{amount}を受領した。"
                : $"{candidate.name}を受領した。";
        }

        public static int StableSeed(RunState run)
        {
            unchecked
            {
                int hash = 17;
                string source = $"{run?.dungeon}|{run?.battlesWon}|{run?.expeditionPlan?.currentNodeId}";
                for (int index = 0; index < source.Length; index++)
                    hash = hash * 31 + source[index];
                return hash;
            }
        }

        private static JourneyBattleRewardCandidate BuildConsumable(
            string id,
            int amount,
            bool expeditionResource = false)
        {
            ConsumableContent definition = ConsumableSystem.Definition(id);
            string name = definition?.name ?? id;
            return new JourneyBattleRewardCandidate
            {
                kind = expeditionResource
                    ? JourneyBattleRewardKind.ExpeditionResource
                    : JourneyBattleRewardKind.Consumable,
                contentId = id,
                name = name,
                description = definition?.description ?? "遠征中に使用できる。",
                meta = $"{(expeditionResource ? "遠征資源" : "消耗品")}  ×{Math.Max(1, amount)}",
                amount = Math.Max(1, amount)
            };
        }

        private static JourneyBattleRewardCandidate BuildEquipment(string id)
        {
            ItemDef definition = GameCatalog.Items[id];
            return new JourneyBattleRewardCandidate
            {
                kind = JourneyBattleRewardKind.Equipment,
                contentId = id,
                name = definition.name,
                description = definition.description,
                meta = $"装備  {RarityLabel(definition.rarity)}",
                amount = 1
            };
        }

        private static string PickExisting(string[] ids, Random random)
        {
            string[] existing = ids
                .Where(id => ConsumableSystem.Definition(id) != null)
                .ToArray();
            if (existing.Length == 0) return ids[0];
            return existing[random.Next(existing.Length)];
        }

        private static string SelectEquipmentId(
            RunState run,
            JourneyBattleRewardTier tier,
            Random random)
        {
            string dungeonId = run?.dungeon;
            DungeonContent dungeon = PackspireContent.Data.dungeons
                .FirstOrDefault(value => value.id == dungeonId);
            string poolId = string.IsNullOrEmpty(dungeon?.rewardPoolId)
                ? "standard"
                : dungeon.rewardPoolId;
            RewardPoolContent pool = PackspireContent.Data.rewardPools
                .FirstOrDefault(value => value.id == poolId)
                ?? PackspireContent.Data.rewardPools.FirstOrDefault(value => value.id == "standard");
            string[] available = (pool?.itemIds ?? Array.Empty<string>())
                .Where(GameCatalog.Items.ContainsKey)
                .Distinct()
                .ToArray();
            if (available.Length == 0) return string.Empty;

            int minimumTier = tier switch
            {
                JourneyBattleRewardTier.Boss => 3,
                JourneyBattleRewardTier.Elite => 2,
                _ => 1
            };
            string[] qualified = available
                .Where(id => GameCatalog.Items[id].acquisitionTier >= minimumTier)
                .ToArray();
            if (qualified.Length == 0)
            {
                int highestTier = available.Max(id => GameCatalog.Items[id].acquisitionTier);
                qualified = available
                    .Where(id => GameCatalog.Items[id].acquisitionTier == highestTier)
                    .ToArray();
            }
            return qualified[random.Next(qualified.Length)];
        }

        private static string RarityLabel(ItemRarity rarity) => rarity switch
        {
            ItemRarity.Uncommon => "上質",
            ItemRarity.Rare => "希少",
            ItemRarity.Legendary => "伝説",
            ItemRarity.Cursed => "呪物",
            _ => "標準"
        };
    }
}
