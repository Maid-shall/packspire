using System.Collections;
using UnityEngine;

namespace Packspire
{
    public sealed partial class JourneyTravelGameplayPrototype
    {
        private static readonly string[] BattleExitClasses =
        {
            "battle-exit--active",
            "battle-exit--clear"
        };

        private IEnumerator JourneyBattleVictoryPresentationRoutine(int revision)
        {
            if (combatLabMeasurements.Active)
            {
                SetMainEnemyVisible(false);
                CompleteJourneyBattleVictory();
                yield break;
            }

            PrepareBattleExitPresentation();
            float elapsed = 0f;
            while (elapsed < JourneyBattleExitSequence.Duration)
            {
                if (revision != battleRevision || phase != Phase.Battle)
                    yield break;

                if (!paused && !ledgerOpen && !pileOverlayOpen)
                    elapsed += Time.unscaledDeltaTime;
                ApplyBattleExitFrame(JourneyBattleExitSequence.Sample(elapsed));
                yield return null;
            }

            if (revision != battleRevision || phase != Phase.Battle)
                yield break;

            ApplyBattleExitFrame(
                JourneyBattleExitSequence.Sample(JourneyBattleExitSequence.Duration));
            SetMainEnemyVisible(false);
            if (!usesLiveRun)
                ClearBattleExitPresentation();
            CompleteJourneyBattleVictory();
        }

        private void PrepareBattleExitPresentation()
        {
            battleInputLocked = true;
            StopRealtimeBattle();
            defenseActive = false;
            defenseResolved = true;
            defenseWindowOpen = false;
            SetEnemyTelegraphVisible(false);
            ResetEnemyTimingCue();
            ResetEnemyImpactContactCue();
            CancelEnemyPoseRecovery();
            SetEnemyBattlePose(EnemyBattlePose.Hit);
            walker.SetBattleMotion(JourneyWalkCyclePrototype.BattleMotion.Idle);
            battleStartBanner.text = "ROUTE CLEARED";
            screen.AddToClassList("battle-exit--active");
            screen.RemoveFromClassList("battle-exit--clear");
            transition?.RemoveFromClassList("transition--visible");
        }

        private void ApplyBattleExitFrame(JourneyBattleExitSequence.Frame frame)
        {
            screen.EnableInClassList("battle-exit--clear", frame.RouteClearVisible);
            if (transition != null)
                transition.EnableInClassList("transition--visible", frame.FogVisible);
            transitionActive = frame.FogVisible;
            ApplyWorldMotion();

            if (enemyRenderer == null) return;
            enemyRenderer.transform.position = enemyBattleBasePosition + new Vector3(
                frame.EnemyOffsetX,
                frame.EnemyOffsetY,
                0f);
            enemyRenderer.transform.rotation = Quaternion.Euler(
                0f,
                0f,
                frame.EnemyRotation);
            enemyRenderer.transform.localScale =
                Vector3.one * enemyBattleBaseScale * frame.EnemyScale;
            enemyRenderer.color = new Color(1f, 1f, 1f, frame.EnemyAlpha);
            if (enemyShadowRenderer != null)
            {
                Color shadow = enemyShadowRenderer.color;
                shadow.a = .42f * frame.ShadowAlpha;
                enemyShadowRenderer.color = shadow;
            }
            SyncMainEnemyShadow();
        }

        private void ClearBattleExitPresentation()
        {
            if (screen != null)
                foreach (string className in BattleExitClasses)
                    screen.RemoveFromClassList(className);
            transition?.RemoveFromClassList("transition--visible");
            transitionActive = false;
            if (battleStartBanner != null)
                battleStartBanner.text = "BATTLE START";
        }

        private IEnumerator ResumeLiveJourneyAfterRewardRoutine()
        {
            if (transition == null)
            {
                ShowChoice();
                ShowToast("戦利品を収め、旅程へ復帰しました。");
                yield break;
            }

            transitionActive = true;
            transition.AddToClassList("transition--visible");
            ApplyWorldMotion();
            yield return null;

            ShowChoice();
            yield return WaitForJourneySeconds(.12f);
            transition.RemoveFromClassList("transition--visible");
            yield return WaitForJourneySeconds(.36f);
            transitionActive = false;
            ApplyWorldMotion();
            ShowToast("戦利品を収め、現在地点から旅程へ復帰しました。");
        }
    }
}
