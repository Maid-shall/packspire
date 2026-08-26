using UnityEngine;

namespace Packspire
{
    public sealed partial class JourneyTravelGameplayPrototype
    {
        private const float DefenseWindowStart01 = .66f;
        private const float DefenseWindowEnd01 = .96f;

        private void BeginBattle(bool playEncounterIntro = true)
        {
            ClosePileOverlay();
            battleRevision++;
            if (encounterRoutine != null) StopCoroutine(encounterRoutine);
            if (battlePresentationRoutine != null) StopCoroutine(battlePresentationRoutine);
            encounterRoutine = null;
            battlePresentationRoutine = null;
            encounterIntroActive = playEncounterIntro;
            battleEntryStartingMotionScale = Mathf.Max(.5f, speedScale);
            battleEntryMotionScale = playEncounterIntro
                ? battleEntryStartingMotionScale
                : 0f;
            ClearBattleEntryClasses();
            ClearBattleExitPresentation();
            if (playEncounterIntro)
                screen.AddToClassList("battle-entry--active");
            SetPhase(Phase.Battle);
            if (playEncounterIntro)
                scenery.SetActivity(true, false);
            else
                scenery.ClearAll();
            defenseActive = false;
            enemyImpactHoldRemaining = 0f;
            defenseResolved = false;
            defenseWindowOpen = false;
            battleInputLocked = true;
            defenseAction = DefenseAction.None;
            defenseInputGrade = DefenseInputGrade.None;
            defenseInputTime = -1f;
            screen.RemoveFromClassList("battle--telegraph");
            screen.RemoveFromClassList("battle--reaction-ready");
            screen.RemoveFromClassList("battle--reaction-committed");
            screen.RemoveFromClassList("battle--reaction-jump");
            screen.RemoveFromClassList("battle--reaction-brace");
            screen.RemoveFromClassList("fx--impact");
            damagePopup?.RemoveFromClassList("fx--visible");
            damagePopup?.RemoveFromClassList("fx--player");
            encounterBanner?.RemoveFromClassList("encounter--visible");
            SetEnemyTelegraphVisible(false);
            walker.SetBattleMotion(JourneyWalkCyclePrototype.BattleMotion.Idle);
            CancelEnemyPoseRecovery();
            SetEnemyBattlePose(EnemyBattlePose.Idle);
            ApplyBattleActorLayout(1);
            enemyRenderer.transform.rotation = Quaternion.identity;
            enemyRenderer.transform.localScale = Vector3.one * enemyBattleBaseScale;
            enemyRenderer.color = Color.white;
            SetMainEnemyVisible(!playEncounterIntro);
            SyncMainEnemyShadow();
            EnemyDef enemy = encounterProfile.BuildEnemy();
            SaveJourneyStage(JourneyResumeStage.Battle);
            battle = BattleSystem.Begin(run, enemy, 1f);
            StartRealtimeBattle();
            SetBattlePreviewEnemyCount(1);
            enemyNames[0].text = enemy.name;
            battleLog.text = $"{enemy.name}が進路を塞いだ。";
            encounterBanner.text = $"ROUTE INTERCEPT  /  {enemy.name}";
            RefreshBattleUi();
            if (playEncounterIntro)
            {
                PrepareBattleEntryEnemy();
                encounterRoutine = StartCoroutine(EncounterRoutine(battleRevision));
            }
            else
            {
                CompleteBattleEntry(true);
            }
        }

        private void PlayCard(int index)
        {
            if (phase != Phase.Battle || battleInputLocked || pileOverlayOpen || battle == null || index >= run.hand.Count) return;
            CardInstance playedCard = run.hand[index];
            if (playedCard.unplayable || playedCard.cost > run.energy) return;
            int attackBuffBeforePlay = run.attackBuff;
            int counterBonus = 0;
            if (playedCard.damage > 0)
            {
                if (realtimeBattle.AttackBonus > 0)
                    run.attackBuff += realtimeBattle.AttackBonus;
                counterBonus = realtimeCombatTiming.ConsumeCounterBonus(
                    playedCard.damage);
                run.attackBuff += counterBonus;
            }
            BattleActionFx fx = BattleSystem.PlayCard(run, battle, index);
            if (!fx.ok)
            {
                run.attackBuff = attackBuffBeforePlay;
                return;
            }
            if (playedCard.block > 0 && fx.blockGained > 0 && realtimeBattle.GuardBonus > 0)
            {
                run.block += realtimeBattle.GuardBonus;
                fx.blockGained += realtimeBattle.GuardBonus;
            }
            if (fx.blockGained > 0)
                realtimeCombatTiming.NotifyPlayerGuardChanged(run.block);
            realtimeBattle.SetEnergy(run.energy);
            run.energy = realtimeBattle.Energy;
            if (counterBonus > 0)
                battle.log += $" / 反撃+{counterBonus}";
            battleLog.text = battle.log;
            if (fx.damageToEnemy > 0)
            {
                battlePresentationRoutine = StartCoroutine(PlayerAttackPresentationRoutine(
                    fx.damageToEnemy,
                    fx.enemyDefeated,
                    battleRevision));
            }
            else if (fx.enemyDefeated)
            {
                battlePresentationRoutine = StartCoroutine(
                    JourneyBattleVictoryPresentationRoutine(battleRevision));
                return;
            }
            else RefreshBattleUi();
        }

        private void UpdateDefense(float delta)
        {
            if (!defenseActive || delta <= 0f) return;
            defenseClock = Mathf.Min(defenseDuration, defenseClock + delta);
            float anticipation = defenseDuration <= 0f ? 1f : defenseClock / defenseDuration;
            float urgency = Mathf.SmoothStep(
                0f,
                1f,
                Mathf.InverseLerp(.42f, .95f, anticipation));
            bool reactionRequired = EnemyActionRequiresReaction();
            bool overhead = enemyActionOverhead;
            bool timingCueReached = anticipation >= DefenseWindowStart01;
            if (!enemyTimingCueFired && timingCueReached)
                BeginEnemyTimingCue();
            bool windowOpen = reactionRequired &&
                              anticipation >= DefenseWindowStart01 &&
                              anticipation <= DefenseWindowEnd01;
            if (windowOpen != defenseWindowOpen)
            {
                defenseWindowOpen = windowOpen;
                screen.EnableInClassList(
                    "battle--reaction-ready",
                    windowOpen && !defenseResolved);
            }

            UpdateEnemyTimingCue(delta, anticipation);
            float flash = EnemyTimingFlashStrength;
            enemyRenderer.color = Color.Lerp(
                Color.white,
                enemyTelegraphColor,
                Mathf.Clamp01(.025f + urgency * .055f + flash * .10f));
            enemyRenderer.transform.position = enemyBattleBasePosition + new Vector3(
                Mathf.Lerp(.18f, -.14f, anticipation),
                overhead
                    ? Mathf.Lerp(.035f, -.015f, anticipation)
                    : Mathf.Lerp(-.035f, -.07f, anticipation),
                0f);
            enemyRenderer.transform.rotation = Quaternion.Euler(
                0f,
                0f,
                Mathf.Lerp(overhead ? -1.5f : 1.5f, 0f, anticipation));
            enemyRenderer.transform.localScale =
                Vector3.one * enemyBattleBaseScale;
            SyncMainEnemyShadow();
        }

        private void CompleteJourneyBattleVictory()
        {
            if (TryCompleteCombatLabMeasurement(true)) return;
            ShowJourneyBattleReward();
        }

        private void ResolveDefenseInput(DefenseAction action)
        {
            if (!defenseActive || defenseResolved || !EnemyActionRequiresReaction()) return;
            defenseResolved = true;
            defenseAction = action;
            defenseInputTime = defenseClock;
            float input01 = defenseDuration <= 0f ? 1f : defenseInputTime / defenseDuration;
            bool correct = (enemyActionKind == EnemyActionKind.JumpReaction && action == DefenseAction.Jump) ||
                           (enemyActionKind == EnemyActionKind.BraceReaction && action == DefenseAction.Brace);
            defenseInputGrade = !correct
                ? DefenseInputGrade.Wrong
                : input01 < DefenseWindowStart01
                    ? DefenseInputGrade.Early
                    : input01 > DefenseWindowEnd01
                        ? DefenseInputGrade.Late
                        : DefenseInputGrade.Success;
            screen.RemoveFromClassList("battle--reaction-ready");
            screen.AddToClassList("battle--reaction-committed");
            if (action == DefenseAction.Jump)
                walker.SetBattleMotion(JourneyWalkCyclePrototype.BattleMotion.Jump, .58f);
            else if (action == DefenseAction.Brace)
                walker.SetBattleMotion(JourneyWalkCyclePrototype.BattleMotion.Brace, .58f);
            battleLog.text = defenseInputGrade switch
            {
                DefenseInputGrade.Success => $"{DefenseActionLabel(action)}。攻撃へ合わせた。",
                DefenseInputGrade.Early => $"{DefenseActionLabel(action)}が早い。体勢が戻る前に攻撃が来る。",
                DefenseInputGrade.Late => $"{DefenseActionLabel(action)}が遅い。攻撃が先に届く。",
                _ => $"読み違えた。{DefenseActionLabel(action)}では受けられない。"
            };
        }

        private static string DefenseActionLabel(DefenseAction action) => action == DefenseAction.Jump ? "ジャンプ" : action == DefenseAction.Brace ? "踏ん張り" : "防御";

        private bool EnemyActionRequiresReaction()
        {
            return enemyActionKind == EnemyActionKind.JumpReaction ||
                   enemyActionKind == EnemyActionKind.BraceReaction;
        }

        private void UpdateBattleIdleMotion()
        {
            if (enemyRenderer == null || !enemyRenderer.enabled) return;
            float idleWave = Mathf.Sin(battleActorClock * 2.2f);
            enemyRenderer.transform.position = enemyBattleBasePosition + new Vector3(0f, idleWave * .025f, 0f);
            enemyRenderer.transform.rotation = Quaternion.Euler(0f, 0f, idleWave * .45f);
            SyncMainEnemyShadow();

            for (int previewIndex = 0; previewIndex < battlePreviewEnemyRenderers.Length; previewIndex++)
            {
                SpriteRenderer renderer = battlePreviewEnemyRenderers[previewIndex];
                if (renderer == null || !renderer.enabled) continue;
                Vector3 basePosition = BattlePreviewEnemyPosition(previewIndex);
                float phase = battleActorClock * (1.9f + previewIndex * .17f) + previewIndex * 1.4f;
                renderer.transform.position = basePosition + new Vector3(0f, Mathf.Sin(phase) * .02f, 0f);
                renderer.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Sin(phase) * .35f);
                SyncBattleActorShadow(battlePreviewEnemyShadowRenderers[previewIndex], renderer);
            }
        }

        private void SetEnemyTelegraphVisible(bool visible)
        {
            if (enemyTelegraphRenderer == null) return;
            enemyTelegraphRenderer.enabled = visible;
            if (visible) SyncEnemyTelegraphTransform(0f, 0f);
            else SetEnemyTimingGlintVisible(false);
        }

        private void SyncEnemyTelegraphTransform(float pulse, float anticipation)
        {
            if (enemyTelegraphRenderer == null || enemyRenderer == null) return;
            enemyTelegraphRenderer.transform.position = enemyRenderer.transform.position;
            enemyTelegraphRenderer.transform.rotation = enemyRenderer.transform.rotation;
            float haloScale = 1.075f + pulse * .035f + anticipation * .018f;
            enemyTelegraphRenderer.transform.localScale = enemyRenderer.transform.localScale * haloScale;
        }

    }
}
