using System.Collections;
using UnityEngine;

namespace Packspire
{
    public sealed partial class JourneyTravelGameplayPrototype
    {
        private bool battleEntryStageActivated;

        private static readonly string[] BattleEntryClasses =
        {
            "battle-entry--active",
            "battle-entry--enemy-hud",
            "battle-entry--player-hud",
            "battle-entry--reel",
            "battle-entry--commands",
            "battle-entry--start"
        };

        private IEnumerator EncounterRoutine(int revision)
        {
            float elapsed = 0f;
            while (elapsed < JourneyBattleEntrySequence.Duration)
            {
                if (revision != battleRevision || phase != Phase.Battle)
                    yield break;

                if (!paused && !ledgerOpen && !transitionActive)
                {
                    elapsed += Time.unscaledDeltaTime;
                    ApplyBattleEntryFrame(
                        JourneyBattleEntrySequence.Sample(
                            elapsed,
                            battleEntryStartingMotionScale));
                }
                yield return null;
            }

            if (revision != battleRevision || phase != Phase.Battle)
                yield break;

            CompleteBattleEntry(true);
            encounterRoutine = null;
        }

        private void PrepareBattleEntryEnemy()
        {
            battleEntryStageActivated = false;
            if (enemyRenderer == null) return;
            enemyRenderer.transform.position = enemyBattleBasePosition +
                Vector3.right * JourneyBattleEntrySequence.EnemyStartOffset;
            enemyRenderer.transform.localScale = Vector3.one * enemyBattleBaseScale;
            enemyRenderer.color = new Color(1f, 1f, 1f, 0f);
            SetMainEnemyVisible(false);
        }

        private void ApplyBattleEntryFrame(JourneyBattleEntrySequence.Frame frame)
        {
            battleEntryMotionScale = frame.WorldMotionScale;
            ApplyWorldMotion();

            if (frame.EnemyVisible && enemyRenderer != null)
            {
                if (!enemyRenderer.enabled)
                    SetMainEnemyVisible(true);

                if (frame.BattleStageActive && !battleEntryStageActivated)
                {
                    battleEntryStageActivated = true;
                    scenery.SetActivity(false, false);
                    walker.SetBattleStage(true);
                    environment.SetBattleContext(true);
                    walker.SetBattleMotion(JourneyWalkCyclePrototype.BattleMotion.Idle);
                }

                if (frame.BattleStageActive)
                    walker.SetBattleStageBlend(frame.EnemyStageBlend);

                float travelGroundOffset = walker != null
                    ? walker.BattleStageLift * (1f - frame.EnemyStageBlend)
                    : 0f;
                enemyRenderer.transform.position = enemyBattleBasePosition +
                    Vector3.right * frame.EnemyOffset +
                    Vector3.down * travelGroundOffset;
                enemyRenderer.transform.localScale = Vector3.one * enemyBattleBaseScale;
                enemyRenderer.color = new Color(1f, 1f, 1f, frame.EnemyAlpha);
                if (enemyShadowRenderer != null)
                {
                    Color shadow = enemyShadowRenderer.color;
                    shadow.a = .42f * frame.EnemyAlpha;
                    enemyShadowRenderer.color = shadow;
                }
                SyncMainEnemyShadow(
                    BattleGroundY - travelGroundOffset);
            }

            encounterBanner.EnableInClassList(
                "encounter--visible",
                frame.ThreatVisible);
            screen.EnableInClassList(
                "battle-entry--enemy-hud",
                frame.EnemyHudVisible);
            screen.EnableInClassList(
                "battle-entry--player-hud",
                frame.PlayerHudVisible);
            screen.EnableInClassList(
                "battle-entry--reel",
                frame.ReelVisible);
            screen.EnableInClassList(
                "battle-entry--commands",
                frame.CommandsVisible);
            screen.EnableInClassList(
                "battle-entry--start",
                frame.BattleStartVisible);
        }

        private void CompleteBattleEntry(bool unlockInput)
        {
            encounterIntroActive = false;
            battleEntryStageActivated = true;
            battleEntryMotionScale = 0f;
            ClearBattleEntryClasses();
            encounterBanner?.RemoveFromClassList("encounter--visible");
            scenery?.SetActivity(false, false);
            walker?.SetBattleStage(true);
            environment?.SetBattleContext(true);
            ApplyWorldMotion();

            if (enemyRenderer != null)
            {
                enemyRenderer.transform.position = enemyBattleBasePosition;
                enemyRenderer.transform.rotation = Quaternion.identity;
                enemyRenderer.transform.localScale = Vector3.one * enemyBattleBaseScale;
                enemyRenderer.color = Color.white;
                SetMainEnemyVisible(true);
                if (enemyShadowRenderer != null)
                {
                    Color shadow = enemyShadowRenderer.color;
                    shadow.a = .42f;
                    enemyShadowRenderer.color = shadow;
                }
                SyncMainEnemyShadow();
            }

            if (unlockInput && phase == Phase.Battle &&
                !screen.ClassListContains("battle--layout-preview"))
            {
                battleInputLocked = false;
                RefreshBattleUi();
            }
        }

        private void ClearBattleEntryClasses()
        {
            if (screen == null) return;
            foreach (string className in BattleEntryClasses)
                screen.RemoveFromClassList(className);
        }
    }
}
