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
            Content("grey-overstamped-seal", 0, ExpeditionNodeKind.Event,
                "灰市外縁・重ね押しの配達印", "二重の配達印を照合",
                "検札柱の台帳に、異なる日付の配達印が同じ欄へ重ねて押されている。",
                "古い印を読み分けるか、不審な記録を残して先へ進む。"),
            Content("grey-broken-relay", 0, ExpeditionNodeKind.Rest,
                "灰市外縁・崩れた中継棚", "中継棚で荷を整える",
                "半壊した中継棚に、まだ使える手当用品が残っている。",
                "短い補給を済ませてから次の区画へ向かう。"),
            Content("grey-warm-sorting-room", 0, ExpeditionNodeKind.Rest,
                "灰市外縁・残熱の仕分け室", "仕分け室で息を整える",
                "止まった送風管の奥に、炉の残熱だけが残る小部屋がある。",
                "煤を払い、荷紐と装備を締め直して街道へ戻る。"),
            Content("grey-abandoned-cart", 0, ExpeditionNodeKind.Other,
                "灰市外縁・放棄された荷車", "残された荷を調べる",
                "路肩の荷車に、回収されなかった小包が積まれている。",
                "荷札を慎重に確かめるか、足を止めずに通過する。"),
            Content("grey-rooftop-mailbag", 0, ExpeditionNodeKind.Other,
                "灰市外縁・屋根に掛かった郵袋", "高所の郵袋を確かめる",
                "崩れた庇の先に、配達局の古い郵袋が引っ掛かっている。",
                "足場を探して回収するか、落下の危険を避けて進む。"),
            Content("archive-floating-manifest", 1, ExpeditionNodeKind.Event,
                "水没書庫・浮かぶ未配達票", "濡れた配達票を照合",
                "浅い水路に、文字の残った未配達票が漂っている。",
                "読める箇所を照合するか、水位が上がる前に進む。"),
            Content("archive-reversed-catalog", 1, ExpeditionNodeKind.Event,
                "水没書庫・逆さの目録札", "逆順の目録を照合",
                "沈んだ索引棚だけ、目録札が末尾から逆順に差し替えられている。",
                "並びの意図を読み解くか、巡回音が近づく前に離れる。"),
            Content("archive-dry-reading-room", 1, ExpeditionNodeKind.Rest,
                "水没書庫・乾いた閲覧室", "乾いた部屋で荷を整える",
                "扉の奥に、浸水を免れた小さな閲覧室が残っている。",
                "装備と荷を整え、次の水路へ備える。"),
            Content("archive-clerk-landing", 1, ExpeditionNodeKind.Rest,
                "水没書庫・書記官の踊り場", "踊り場で水気を払う",
                "階段の中程に、水面より高く保たれた書記官用の踊り場がある。",
                "濡れた荷を拭い、足元を整えてから下層へ向かう。"),
            Content("archive-sealed-record-box", 1, ExpeditionNodeKind.Other,
                "水没書庫・封蝋された記録箱", "記録箱を検める",
                "棚の高所に、古い封蝋が残る記録箱が固定されている。",
                "封を照合して調べるか、そのまま奥へ進む。"),
            Content("archive-chained-document-tube", 1, ExpeditionNodeKind.Other,
                "水没書庫・鎖留めの文書筒", "鎖留めの筒を調べる",
                "水路脇の柱に、防水布で包まれた文書筒が細い鎖で留められている。",
                "宛先印を確かめて外すか、封鎖資料として残しておく。"),
            Content("bell-silent-call", 2, ExpeditionNodeKind.Event,
                "黒鐘区画・鳴らない呼出鐘", "呼出記録を照合",
                "鳴らない鐘の下に、受取人を待つ呼出記録が吊られている。",
                "記録をたどるか、番人が戻る前に通過する。"),
            Content("bell-return-slips", 2, ExpeditionNodeKind.Event,
                "黒鐘区画・返送票の束", "返送理由を照合",
                "閉ざされた窓口に、同じ宛先へ戻された返送票が束ねられている。",
                "差戻しの理由を追うか、鐘楼の巡回を避けて先へ進む。"),
            Content("bell-warden-shelter", 2, ExpeditionNodeKind.Rest,
                "黒鐘区画・鐘守の退避所", "退避所で態勢を整える",
                "壁際の退避所には、鐘守が残した補給箱が置かれている。",
                "最低限の補給を行い、最深部へ進む。"),
            Content("bell-soundproof-post", 2, ExpeditionNodeKind.Rest,
                "黒鐘区画・防音された詰所", "静かな詰所で休む",
                "厚い布と鉛板で囲まれた詰所だけ、鐘の振動が遠く感じられる。",
                "短い静寂の間に呼吸と荷姿を整える。"),
            Content("bell-ash-parcel", 2, ExpeditionNodeKind.Other,
                "黒鐘区画・灰をかぶった小包", "灰の小包を検める",
                "鐘楼の影に、差出印だけが読める小包が残されている。",
                "印を確かめて回収するか、封鎖突破を優先する。"),
            Content("bell-unclaimed-satchel", 2, ExpeditionNodeKind.Other,
                "黒鐘区画・未受領の革鞄", "受領印のない鞄を調べる",
                "検鐘台の下に、受領印のない丈夫な革鞄が封鎖札と共に置かれている。",
                "封鎖札を照合して開くか、番人の所有物として残しておく。")
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
