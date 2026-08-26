using System;
using System.Collections.Generic;
using System.Linq;

namespace Packspire
{
    public sealed class ExpeditionLocationContentDef
    {
        public string id = string.Empty;
        public int floorIndex;
        public ExpeditionNodeKind kind;
        public string title = string.Empty;
        public string resolutionTitle = string.Empty;
        public string resolutionText = string.Empty;
        public string condition = string.Empty;
    }

    public sealed class ExpeditionLocationContentAudit
    {
        public readonly List<string> errors = new();
        public bool IsValid => errors.Count == 0;
    }

    /// <summary>
    /// Selects presentation content for generated event, rest, and exploration
    /// nodes. Selection is deterministic and persisted on the route node.
    /// Existing outcome values remain owned by ExpeditionJourneySystem.
    /// </summary>
    public static class ExpeditionLocationContentSystem
    {
        private static readonly ExpeditionLocationContentDef[] Definitions =
        {
            Content("grey-lost-address", 0, ExpeditionNodeKind.Event,
                "灰市外縁・宛先の消えた配達票", "消えた宛先を照合",
                "焼けた掲示板に、宛先だけが消えた配達票が残されている。",
                "時間をかけて照合するか、期限を優先して先へ進む。"),
            Content("grey-broken-relay", 0, ExpeditionNodeKind.Rest,
                "灰市外縁・崩れた中継棚", "中継棚で荷を整える",
                "半壊した中継棚に、まだ使える手当用品が残っている。",
                "短い補給を済ませてから次の区画へ向かう。"),
            Content("grey-abandoned-cart", 0, ExpeditionNodeKind.Other,
                "灰市外縁・放棄された荷車", "残された荷を調べる",
                "路肩の荷車に、回収されなかった小包が積まれている。",
                "荷札を慎重に確かめるか、足を止めずに通過する。"),
            Content("archive-floating-manifest", 1, ExpeditionNodeKind.Event,
                "水没書庫・浮かぶ未配達票", "濡れた配達票を照合",
                "浅い水路に、文字の残った未配達票が漂っている。",
                "読める箇所を照合するか、水位が上がる前に進む。"),
            Content("archive-dry-reading-room", 1, ExpeditionNodeKind.Rest,
                "水没書庫・乾いた閲覧室", "乾いた部屋で荷を整える",
                "扉の奥に、浸水を免れた小さな閲覧室が残っている。",
                "装備と荷を整え、次の水路へ備える。"),
            Content("archive-sealed-record-box", 1, ExpeditionNodeKind.Other,
                "水没書庫・封蝋された記録箱", "記録箱を検める",
                "棚の高所に、古い封蝋が残る記録箱が固定されている。",
                "封を照合して調べるか、そのまま奥へ進む。"),
            Content("bell-silent-call", 2, ExpeditionNodeKind.Event,
                "黒鐘区画・鳴らない呼出鐘", "呼出記録を照合",
                "鳴らない鐘の下に、受取人を待つ呼出記録が吊られている。",
                "記録をたどるか、番人が戻る前に通過する。"),
            Content("bell-warden-shelter", 2, ExpeditionNodeKind.Rest,
                "黒鐘区画・鐘守の退避所", "退避所で態勢を整える",
                "壁際の退避所には、鐘守が残した補給箱が置かれている。",
                "最低限の補給を行い、最深部へ進む。"),
            Content("bell-ash-parcel", 2, ExpeditionNodeKind.Other,
                "黒鐘区画・灰をかぶった小包", "灰の小包を検める",
                "鐘楼の影に、差出印だけが読める小包が残されている。",
                "印を確かめて回収するか、封鎖突破を優先する。")
        };

        public static IReadOnlyList<ExpeditionLocationContentDef> All => Definitions;

        public static ExpeditionLocationContentDef SelectAndAssign(
            ExpeditionRoutePlan plan,
            ExpeditionRouteNodePlan node,
            string dungeonId)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            if (node == null) throw new ArgumentNullException(nameof(node));
            if (!Supports(node.kind))
                throw new InvalidOperationException($"Node '{node.id}' does not use location content.");

            ExpeditionLocationContentDef persisted = Resolve(node.locationContentId);
            if (persisted != null) return persisted;

            ExpeditionLocationContentDef[] eligible = Definitions
                .Where(content => content.floorIndex == node.floorIndex && content.kind == node.kind)
                .OrderBy(content => content.id, StringComparer.Ordinal)
                .ToArray();
            if (eligible.Length == 0)
                throw new InvalidOperationException(
                    $"No location content is eligible for '{node.id}' on floor {node.floorIndex + 1}.");

            uint roll = StableHash($"{plan.seed}|{node.id}|{dungeonId}|{node.kind}");
            ExpeditionLocationContentDef selected = eligible[roll % (uint)eligible.Length];
            node.locationContentId = selected.id;
            return selected;
        }

        public static ExpeditionLocationContentDef Resolve(string contentId) =>
            string.IsNullOrWhiteSpace(contentId)
                ? null
                : Definitions.FirstOrDefault(content =>
                    string.Equals(content.id, contentId, StringComparison.Ordinal));

        public static void ApplyPresentation(
            CourierRouteNodeDef presentation,
            ExpeditionLocationContentDef content)
        {
            if (presentation == null || content == null) return;
            presentation.title = content.title;
            presentation.resolutionTitle = content.resolutionTitle;
            presentation.resolutionText = content.resolutionText;
            presentation.condition = content.condition;
        }

        public static ExpeditionLocationContentAudit AuditCoverage(ExpeditionRoutePlan plan)
        {
            var audit = new ExpeditionLocationContentAudit();
            if (plan?.floors == null)
            {
                audit.errors.Add("Location content audit has no expedition plan.");
                return audit;
            }

            foreach (ExpeditionRouteNodePlan node in plan.floors
                         .SelectMany(floor => floor.nodes)
                         .Where(node => node != null && Supports(node.kind)))
                if (!Definitions.Any(content =>
                        content.floorIndex == node.floorIndex && content.kind == node.kind))
                    audit.errors.Add(
                        $"Node '{node.id}' has no {node.kind} content for floor {node.floorIndex + 1}.");
            return audit;
        }

        private static bool Supports(ExpeditionNodeKind kind) =>
            kind == ExpeditionNodeKind.Event ||
            kind == ExpeditionNodeKind.Rest ||
            kind == ExpeditionNodeKind.Other;

        private static ExpeditionLocationContentDef Content(
            string id,
            int floorIndex,
            ExpeditionNodeKind kind,
            string title,
            string resolutionTitle,
            string resolutionText,
            string condition) => new()
        {
            id = id,
            floorIndex = floorIndex,
            kind = kind,
            title = title,
            resolutionTitle = resolutionTitle,
            resolutionText = resolutionText,
            condition = condition
        };

        private static uint StableHash(string value)
        {
            const uint offset = 2166136261;
            const uint prime = 16777619;
            uint hash = offset;
            for (int index = 0; index < value.Length; index++)
            {
                hash ^= value[index];
                hash *= prime;
            }
            return hash;
        }
    }
}
