namespace Packspire
{
    internal static class JourneyPausePolicy
    {
        public static bool IsGameplayBlocked(
            bool paused,
            bool ledgerOpen,
            bool pileOverlayOpen) =>
            paused || ledgerOpen || pileOverlayOpen;

        public static bool CanTogglePause(
            bool alreadyPaused,
            bool activePhase,
            bool choiceCommitActive) =>
            alreadyPaused || activePhase || choiceCommitActive;
    }
}
