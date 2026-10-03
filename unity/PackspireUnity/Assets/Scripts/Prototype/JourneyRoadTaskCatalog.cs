using System;
using System.Collections.Generic;

namespace Packspire
{
    internal sealed class JourneyRoadTaskDefinition
    {
        public MiniGameKind Kind { get; set; }
        public string PresentationClass { get; set; }
        public string Eyebrow { get; set; }
        public string Title { get; set; }
        public string Note { get; set; }
        public string Objective { get; set; }
        public string InitialStep { get; set; }
        public string Reward { get; set; }
        public string Risk { get; set; }
        public string LeftButton { get; set; }
        public string ActionButton { get; set; }
        public string RightButton { get; set; }
        public string[] Choices { get; set; } = Array.Empty<string>();
        public bool HideSideButtons { get; set; }

        public bool IsChoice => Choices.Length == 3;

        public string ChoiceAt(int index) =>
            Choices.Length == 0 ? string.Empty : Choices[Math.Clamp(index, 0, Choices.Length - 1)];
    }

    /// <summary>
    /// Presentation and biome availability for the six existing roadside tasks.
    /// Gameplay timing and outcome rules remain owned by JourneyMiniGameController.
    /// </summary>
    internal static class JourneyRoadTaskCatalog
    {
        private static readonly MiniGameKind[] DefaultPool =
        {
            MiniGameKind.StampTiming,
            MiniGameKind.CargoBalance,
            MiniGameKind.AddressLabel,
            MiniGameKind.WaxMatch
        };

        private static readonly MiniGameKind[] FloodedArchivePool =
        {
            MiniGameKind.AddressLabel,
            MiniGameKind.RainCover,
            MiniGameKind.CargoBalance
        };

        private static readonly MiniGameKind[] BlackBellPool =
        {
            MiniGameKind.RoadDodge,
            MiniGameKind.WaxMatch,
            MiniGameKind.CargoBalance
        };

        private static readonly Dictionary<MiniGameKind, JourneyRoadTaskDefinition> Definitions =
            new()
            {
                [MiniGameKind.StampTiming] = new JourneyRoadTaskDefinition
                {
                    Kind = MiniGameKind.StampTiming,
                    PresentationClass = "mini--stamp",
                    Eyebrow = "ROADSIDE PICKUP",
                    Title = "落とし物へ受取印を押す",
                    Note = "白い針が中央の金色帯に入った瞬間、SPACE。",
                    Objective = "成功条件：金色帯でSPACE（緑帯でも回収）",
                    InitialStep = "STEP 0 / 1",
                    Reward = "金色　荷物 +1 / 進行短縮",
                    Risk = "外側　見送り",
                    ActionButton = "受取印を押す  [SPACE]",
                    HideSideButtons = true
                },
                [MiniGameKind.CargoBalance] = new JourneyRoadTaskDefinition
                {
                    Kind = MiniGameKind.CargoBalance,
                    PresentationClass = "mini--balance",
                    Eyebrow = "PACK BALANCE",
                    Title = "荷崩れを抑える",
                    Note = "A / Dで針を中央へ戻し、安定時間をためる。",
                    Objective = "成功条件：中央帯に合計2秒",
                    InitialStep = "安定 0.0 / 2.0",
                    Reward = "成功　未整理荷物 +1 / HP +1",
                    Risk = "時間切れ　見送り",
                    ActionButton = "姿勢を整える  [SPACE]"
                },
                [MiniGameKind.AddressLabel] = new JourneyRoadTaskDefinition
                {
                    Kind = MiniGameKind.AddressLabel,
                    PresentationClass = "mini--choice",
                    Eyebrow = "FLYING LABEL",
                    Title = "風で飛ぶ荷札を照合",
                    Note = "表示された宛先と同じ荷札を選ぶ。",
                    Objective = "成功条件：正解を1回選択",
                    InitialStep = "照合 0 / 1",
                    Reward = "成功　未整理荷物 +1",
                    Risk = "誤照合　今回の回収なし",
                    Choices = new[] { "北塔", "灰市場", "水没書庫" }
                },
                [MiniGameKind.WaxMatch] = new JourneyRoadTaskDefinition
                {
                    Kind = MiniGameKind.WaxMatch,
                    PresentationClass = "mini--choice",
                    Eyebrow = "WAX INSPECTION",
                    Title = "見本と同じ封蝋を選ぶ",
                    Note = "見本の印影と同じ紋章を選ぶ。",
                    Objective = "成功条件：正解を2回選択",
                    InitialStep = "照合 0 / 2",
                    Reward = "成功　未整理荷物 +1",
                    Risk = "誤照合　今回の回収なし",
                    Choices = new[] { "鐘", "鍵", "羽根" }
                },
                [MiniGameKind.RoadDodge] = new JourneyRoadTaskDefinition
                {
                    Kind = MiniGameKind.RoadDodge,
                    PresentationClass = "mini--dodge",
                    Eyebrow = "ROAD HAZARD",
                    Title = "轍を避けて荷を守る",
                    Note = "A / Dで安全な車線へ移動。赤帯から離れる。",
                    Objective = "成功条件：3回の障害物を回避",
                    InitialStep = "回避 0 / 3",
                    Reward = "成功　進行短縮 / 荷物 +1",
                    Risk = "接触　今回の回収なし",
                    ActionButton = "現在の車線を維持"
                },
                [MiniGameKind.RainCover] = new JourneyRoadTaskDefinition
                {
                    Kind = MiniGameKind.RainCover,
                    PresentationClass = "mini--sequence",
                    Eyebrow = "RAIN COVER",
                    Title = "雨除け布を順に留める",
                    Note = "表示順どおりに 左・中央・右 の留め具を押す。",
                    Objective = "成功条件：左 → 中央 → 右",
                    InitialStep = "留め具 0 / 3",
                    Reward = "成功　荷濡れ防止 / 荷物 +1",
                    Risk = "順番違い　やり直し",
                    LeftButton = "左を留める  [A]",
                    ActionButton = "中央を留める  [SPACE]",
                    RightButton = "右を留める  [D]"
                }
            };

        public static IReadOnlyCollection<JourneyRoadTaskDefinition> All => Definitions.Values;

        public static JourneyRoadTaskDefinition Get(MiniGameKind kind) => Definitions[kind];

        public static IReadOnlyList<MiniGameKind> PoolForBiome(int biomeIndex) => biomeIndex switch
        {
            1 => FloodedArchivePool,
            2 => BlackBellPool,
            _ => DefaultPool
        };
    }
}
