using System;
using System.Collections.Generic;
using UnityEngine;

namespace Packspire
{
    public enum JourneyResumeStage
    {
        Choice,
        Location,
        Battle,
        Reward,
        Checkpoint,
        Defeat
    }

    [Serializable]
    public sealed class JourneySessionSnapshot
    {
        public int schemaVersion = JourneySessionSaveSystem.CurrentSchemaVersion;
        public JourneyResumeStage stage;
        public RunState run;
        public JourneyBattleRewardOffer rewardOffer;
    }

    /// <summary>
    /// Stores only durable journey boundaries. Runtime clocks, coroutines and battle
    /// presentation state are intentionally rebuilt when the journey scene resumes.
    /// </summary>
    public static class JourneySessionSaveSystem
    {
        public const int CurrentSchemaVersion = 1;

        public static bool CanResume(MetaSave meta) =>
            Normalize(Clone(meta?.activeJourney), meta) != null;

        public static bool Capture(
            MetaSave meta,
            RunState run,
            JourneyResumeStage stage,
            JourneyBattleRewardOffer rewardOffer = null)
        {
            if (meta == null || run == null || run.courierRoute == null) return false;
            var snapshot = new JourneySessionSnapshot
            {
                stage = stage,
                run = run,
                rewardOffer = rewardOffer
            };
            meta.activeJourney = Normalize(Clone(snapshot), meta);
            return meta.activeJourney != null;
        }

        public static bool TryRestore(MetaSave meta, out JourneySessionSnapshot snapshot)
        {
            snapshot = Normalize(Clone(meta?.activeJourney), meta);
            return snapshot != null;
        }

        public static void Clear(MetaSave meta)
        {
            if (meta != null) meta.activeJourney = null;
        }

        public static JourneySessionSnapshot Normalize(
            JourneySessionSnapshot snapshot,
            MetaSave meta = null)
        {
            if (snapshot == null ||
                snapshot.schemaVersion > CurrentSchemaVersion ||
                snapshot.run == null ||
                snapshot.run.courierRoute == null)
                return null;

            snapshot.schemaVersion = CurrentSchemaVersion;
            NormalizeRun(snapshot.run);
            ExpeditionProgressSystem.Ensure(snapshot.run, meta);

            if (snapshot.stage == JourneyResumeStage.Reward &&
                (snapshot.rewardOffer?.candidates == null ||
                 snapshot.rewardOffer.candidates.Length == 0))
            {
                snapshot.rewardOffer = null;
                snapshot.stage = ExpeditionCheckpointSystem.Build(snapshot.run).IsCheckpoint
                    ? JourneyResumeStage.Checkpoint
                    : JourneyResumeStage.Choice;
            }
            return snapshot;
        }

        public static string Summary(MetaSave meta)
        {
            if (!TryRestore(meta, out JourneySessionSnapshot snapshot)) return string.Empty;
            ExpeditionRoutePlan plan = snapshot.run.expeditionPlan;
            ExpeditionRouteNodePlan node =
                ExpeditionRoutePlanSystem.Node(plan, plan.currentNodeId);
            int floor = Mathf.Clamp((node?.floorIndex ?? plan.currentFloorIndex) + 1, 1, 3);
            string stage = snapshot.stage switch
            {
                JourneyResumeStage.Location => "地点へ到着",
                JourneyResumeStage.Battle => "戦闘開始",
                JourneyResumeStage.Reward => "戦果を選択",
                JourneyResumeStage.Checkpoint => "階層踏破",
                JourneyResumeStage.Defeat => "遠征結果",
                _ => "進路を選択"
            };
            return $"第{floor}層 / {stage}";
        }

        private static JourneySessionSnapshot Clone(JourneySessionSnapshot source)
        {
            if (source == null) return null;
            return JsonUtility.FromJson<JourneySessionSnapshot>(
                JsonUtility.ToJson(source));
        }

        private static void NormalizeRun(RunState run)
        {
            run.inventory ??= new List<ItemInstance>();
            run.lootBag ??= new List<ItemInstance>();
            run.placements ??= new List<Placement>();
            run.startingItemUids ??= new List<string>();
            run.selectedCardSlots ??= new List<string>();
            run.consumables ??= new List<string>();
            run.removedBattleCardSlots ??= new List<string>();
            run.statuses ??= new List<StatusState>();
            run.deck ??= new List<CardInstance>();
            run.draw ??= new List<CardInstance>();
            run.discard ??= new List<CardInstance>();
            run.hand ??= new List<CardInstance>();
            run.reactionValues ??= new List<ReactionValueState>();
            run.axes ??= new DungeonAxes();
            run.courierRoute.resolvedNodeIds ??= new List<string>();
            run.courierRoute.seals ??= new List<DeliverySealState>();
            foreach (ItemInstance item in run.inventory)
                NormalizeItem(item);
            foreach (ItemInstance item in run.lootBag)
                NormalizeItem(item);
            foreach (CardInstance card in run.deck)
                if (card != null) card.effects ??= new List<EffectSpec>();
            foreach (CardInstance card in run.draw)
                if (card != null) card.effects ??= new List<EffectSpec>();
            foreach (CardInstance card in run.discard)
                if (card != null) card.effects ??= new List<EffectSpec>();
            foreach (CardInstance card in run.hand)
                if (card != null) card.effects ??= new List<EffectSpec>();
        }

        private static void NormalizeItem(ItemInstance item)
        {
            if (item == null) return;
            item.colors ??= new List<Element>();
            item.scars ??= new List<ScarRecord>();
            item.history ??= new HeirloomHistory();
            item.history.dungeons ??= new List<IdInt>();
        }
    }
}
