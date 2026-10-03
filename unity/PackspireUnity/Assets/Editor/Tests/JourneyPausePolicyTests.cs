using NUnit.Framework;

namespace Packspire.Tests
{
    public sealed class JourneyPausePolicyTests
    {
        [TestCase(false, false, false, true)]
        [TestCase(true, false, false, false)]
        [TestCase(false, true, false, false)]
        [TestCase(false, false, true, false)]
        public void Gameplay_IsBlockedByPauseAndReadingOverlays(
            bool paused,
            bool ledgerOpen,
            bool pileOpen,
            bool canAdvance)
        {
            Assert.That(
                JourneyPausePolicy.IsGameplayBlocked(
                    paused,
                    ledgerOpen,
                    pileOpen),
                Is.EqualTo(!canAdvance));
        }

        [TestCase(false, true, false, true)]
        [TestCase(false, false, true, true)]
        [TestCase(false, false, false, false)]
        [TestCase(true, false, false, true)]
        public void PauseToggle_IsAvailableForActiveOrCommittedSequences(
            bool alreadyPaused,
            bool activePhase,
            bool choiceCommitActive,
            bool expected)
        {
            Assert.That(
                JourneyPausePolicy.CanTogglePause(
                    alreadyPaused,
                    activePhase,
                    choiceCommitActive),
                Is.EqualTo(expected));
        }
    }
}
