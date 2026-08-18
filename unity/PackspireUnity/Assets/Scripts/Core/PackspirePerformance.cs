using Unity.Profiling;

namespace Packspire
{
    /// <summary>
    /// Stable profiler marker names and performance budgets shared by runtime UI.
    /// Keep these names stable so saved Profiler captures remain comparable.
    /// </summary>
    public static class PackspirePerformance
    {
        public const double JourneyUpdateBudgetMs = 0.50;
        public const double JourneyEnvironmentBudgetMs = 0.30;
        public const double JourneyMiniGameBudgetMs = 0.20;
        public const double BattleRefreshBudgetMs = 1.00;
        public const long UiGcBudgetBytesPerFrame = 0;

        public static readonly ProfilerMarker FoundationUpdate =
            new ProfilerMarker(ProfilerCategory.Scripts, "Packspire.UI.Foundation.Update");
        public static readonly ProfilerMarker JourneyUpdate =
            new ProfilerMarker(ProfilerCategory.Scripts, "Packspire.Journey.Update");
        public static readonly ProfilerMarker JourneyEnvironment =
            new ProfilerMarker(ProfilerCategory.Scripts, "Packspire.Journey.Environment");
        public static readonly ProfilerMarker JourneyMiniGame =
            new ProfilerMarker(ProfilerCategory.Scripts, "Packspire.Journey.MiniGame");
        public static readonly ProfilerMarker JourneyBattleRefresh =
            new ProfilerMarker(ProfilerCategory.Scripts, "Packspire.Journey.Battle.Refresh");
        public static readonly ProfilerMarker ProductBattleRefresh =
            new ProfilerMarker(ProfilerCategory.Scripts, "Packspire.Battle.Refresh");
        public static readonly ProfilerMarker ProductBattleHandRefresh =
            new ProfilerMarker(ProfilerCategory.Scripts, "Packspire.Battle.Hand.Refresh");
    }
}
