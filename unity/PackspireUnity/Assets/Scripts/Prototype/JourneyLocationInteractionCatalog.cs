namespace Packspire
{
    internal enum JourneyLocationPresentation
    {
        Inline,
        Overlay
    }

    internal sealed class JourneyLocationInteractionDefinition
    {
        public string Eyebrow { get; set; }
        public string PrimaryButton { get; set; }
        public string SecondaryButton { get; set; }
        public string PrimaryMessage { get; set; }
        public string SecondaryMessage { get; set; }
        public bool ShowSecondaryButton { get; set; } = true;
        public bool PrimaryRecoversCargo { get; set; }
        public int PrimaryPerformance { get; set; }
        public int SecondaryPerformance { get; set; }
        public int SecondaryDayDelta { get; set; }
        public JourneyLocationPresentation Presentation { get; set; } =
            JourneyLocationPresentation.Inline;

        public CourierLocationOutcome BuildOutcome(bool primary) => new()
        {
            success = true,
            cargoRecovered = primary && PrimaryRecoversCargo,
            performance = primary ? PrimaryPerformance : SecondaryPerformance,
            dayDelta = primary ? 0 : SecondaryDayDelta,
            message = primary ? PrimaryMessage : SecondaryMessage
        };
    }

    /// <summary>
    /// Existing location-choice copy and outcomes, separated from the journey UI.
    /// Node-specific content can extend this catalog without changing the presenter.
    /// </summary>
    internal static class JourneyLocationInteractionCatalog
    {
        private static readonly JourneyLocationInteractionDefinition Cargo = new()
        {
            Eyebrow = "RECOVERY NOTICE",
            PrimaryButton = "荷札を照合して回収する",
            SecondaryButton = "期限を優先して進む",
            PrimaryMessage = "照合に成功した。",
            SecondaryMessage = "期限を優先した。",
            PrimaryRecoversCargo = true,
            PrimaryPerformance = 2,
            SecondaryPerformance = 1
        };

        private static readonly JourneyLocationInteractionDefinition Event = new()
        {
            Eyebrow = "ROADSIDE EVENT",
            PrimaryButton = "慎重に手続きを進める",
            SecondaryButton = "急いで突破する",
            PrimaryMessage = "照合に成功した。",
            SecondaryMessage = "期限を優先した。",
            PrimaryPerformance = 2,
            SecondaryPerformance = 1,
            SecondaryDayDelta = 1
        };

        private static readonly JourneyLocationInteractionDefinition Rest = new()
        {
            Eyebrow = "RELAY STOP",
            PrimaryButton = "中継補給を行って旅程を再開",
            PrimaryMessage = "中継補給を完了した。",
            ShowSecondaryButton = false,
            PrimaryPerformance = 2,
            Presentation = JourneyLocationPresentation.Overlay
        };

        private static readonly JourneyLocationInteractionDefinition Exploration = new()
        {
            Eyebrow = "EXPLORATION",
            PrimaryButton = "周辺を慎重に調べる",
            SecondaryButton = "探索を切り上げて進む",
            PrimaryMessage = "周辺の探索を完了した。",
            SecondaryMessage = "探索を切り上げ、期限を優先した。",
            PrimaryPerformance = 2,
            SecondaryPerformance = 1,
            SecondaryDayDelta = 1
        };

        public static JourneyLocationInteractionDefinition For(
            CourierRouteNodeDef node,
            ExpeditionNodeKind? nodeKind = null)
        {
            if (nodeKind == ExpeditionNodeKind.Rest ||
                node?.resolution == CourierResolutionKind.Relay)
                return Rest;
            if (nodeKind == ExpeditionNodeKind.Other)
                return Exploration;
            return node?.resolution == CourierResolutionKind.Cargo ? Cargo : Event;
        }
    }
}
