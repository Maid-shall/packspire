using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire
{
    public sealed partial class JourneyTravelGameplayPrototype
    {
        private const float ReactionImpactTime = 1.15f;
        private const float NormalImpactTime = .62f;
        private const float DefenseWindowStart01 = .66f;
        private const float DefenseWindowEnd01 = .96f;
        private static readonly EnemyActionKind[] WardenActionSequence =
        {
            EnemyActionKind.Normal,
            EnemyActionKind.JumpReaction,
            EnemyActionKind.Normal,
            EnemyActionKind.BraceReaction,
            EnemyActionKind.Normal,
            EnemyActionKind.Normal
        };

        private void BeginBattle()
        {
            ClosePileOverlay();
            battleRevision++;
            if (defenseRoutine != null) StopCoroutine(defenseRoutine);
            if (encounterRoutine != null) StopCoroutine(encounterRoutine);
            if (battlePresentationRoutine != null) StopCoroutine(battlePresentationRoutine);
            defenseRoutine = null;
            encounterRoutine = null;
            battlePresentationRoutine = null;
            SetPhase(Phase.Battle);
            scenery.ClearAll();
            defenseActive = false;
            defenseResolved = false;
            defenseWindowOpen = false;
            battleInputLocked = true;
            defenseAction = DefenseAction.None;
            defenseInputGrade = DefenseInputGrade.None;
            defenseInputTime = -1f;
            encounterIntroActive = true;
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
            SetEnemyBattlePose(EnemyBattlePose.Idle);
            enemyBattleBasePosition = EnemyBattleStartPosition;
            enemyRenderer.transform.position = enemyBattleBasePosition;
            enemyRenderer.transform.rotation = Quaternion.identity;
            enemyRenderer.transform.localScale = Vector3.one * EnemyBattleScale;
            enemyRenderer.color = Color.white;
            SetMainEnemyVisible(true);
            SyncMainEnemyShadow();
            EnemyDef enemy = new EnemyDef(
                "journey_postal_warden",
                "封鐘の番人",
                1,
                36,
                8, 7, 9, 10, 8, 7);
            battle = BattleSystem.Begin(run, enemy, 1f);
            SetBattlePreviewEnemyCount(1);
            enemyNames[0].text = enemy.name;
            battleLog.text = "番人が配達路を封鎖した。同じ旅画面のまま迎撃する。";
            RefreshBattleUi();
            encounterRoutine = StartCoroutine(EncounterRoutine(battleRevision));
        }

        private void PlayCard(int index)
        {
            if (phase != Phase.Battle || defenseActive || battleInputLocked || pileOverlayOpen || battle == null || index >= run.hand.Count) return;
            BattleActionFx fx = BattleSystem.PlayCard(run, battle, index);
            if (!fx.ok) return;
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
                SetMainEnemyVisible(false);
                ResolveRoute(new CourierLocationOutcome { success = true, performance = 2, message = "番人を退けた。" });
                return;
            }
            else RefreshBattleUi();
        }

        private void UseBattleSkill()
        {
            if (phase != Phase.Battle || defenseActive || battleInputLocked || pileOverlayOpen || battle == null || run.activeSkillUsed) return;
            CharacterSkillResult result = CharacterSystem.UseActiveSkill(run, battle);
            if (!result.success) return;
            battleLog.text = result.logLine;
            walker.SetBattleMotion(
                result.fx.damageToEnemy > 0
                    ? JourneyWalkCyclePrototype.BattleMotion.Attack
                    : JourneyWalkCyclePrototype.BattleMotion.Brace,
                .42f);
            if (result.fx.damageToEnemy > 0)
            {
                battlePresentationRoutine = StartCoroutine(PlayerAttackPresentationRoutine(
                    result.fx.damageToEnemy,
                    result.enemyDefeated,
                    battleRevision));
                return;
            }
            RefreshBattleUi();
        }

        private void EndTurn()
        {
            if (phase != Phase.Battle || defenseActive || battleInputLocked || pileOverlayOpen || battle == null) return;
            BeginEnemyTurn();
        }

        private void BeginEnemyTurn()
        {
            defenseActive = true;
            enemyActionKind = NextEnemyAction();
            bool reactionRequired = EnemyActionRequiresReaction();
            defenseResolved = !reactionRequired;
            defenseWindowOpen = false;
            defenseAction = DefenseAction.None;
            defenseInputGrade = DefenseInputGrade.None;
            defenseInputTime = -1f;
            defenseDuration = reactionRequired ? ReactionImpactTime : NormalImpactTime;
            defenseClock = 0f;
            bool overhead = enemyActionKind != EnemyActionKind.JumpReaction;
            screen.EnableInClassList("battle--telegraph", reactionRequired);
            screen.EnableInClassList("battle--reaction-jump", enemyActionKind == EnemyActionKind.JumpReaction);
            screen.EnableInClassList("battle--reaction-brace", enemyActionKind == EnemyActionKind.BraceReaction);
            screen.RemoveFromClassList("battle--reaction-ready");
            screen.RemoveFromClassList("battle--reaction-committed");
            endTurnButton.SetEnabled(false);
            skillButton.SetEnabled(false);
            enemyTelegraphColor = enemyActionKind switch
            {
                EnemyActionKind.JumpReaction => new Color(1f, .34f, .08f, 1f),
                EnemyActionKind.BraceReaction => new Color(.12f, .82f, 1f, 1f),
                _ => new Color(1f, .84f, .58f, 1f)
            };
            SetEnemyTelegraphVisible(reactionRequired);
            SetEnemyBattlePose(overhead
                ? EnemyBattlePose.HighAnticipation
                : EnemyBattlePose.LowAnticipation);
            enemyRenderer.transform.position = enemyBattleBasePosition + new Vector3(.18f, 0f, 0f);
            enemyRenderer.transform.rotation = Quaternion.Euler(0f, 0f, overhead ? -1.5f : 1.5f);
            enemyRenderer.transform.localScale = Vector3.one * EnemyBattleScale;
            SyncMainEnemyShadow();
            battleLog.text = enemyActionKind switch
            {
                EnemyActionKind.JumpReaction => "番人が低く構え、橙光が走る。足元への一撃を跳び越える。",
                EnemyActionKind.BraceReaction => "番人が鐘を掲げ、青白く発光する。重撃を踏ん張って受ける。",
                _ => "番人が鐘を振り上げる。これは通常攻撃だ。"
            };
            RefreshBattleUi();
        }

        private void UpdateDefense(float delta)
        {
            if (!defenseActive || delta <= 0f) return;
            defenseClock = Mathf.Min(defenseDuration, defenseClock + delta);
            float anticipation = defenseDuration <= 0f ? 1f : defenseClock / defenseDuration;
            float urgency = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.42f, .95f, anticipation));
            float pulse = .5f + .5f * Mathf.Sin(
                anticipation * Mathf.PI * Mathf.Lerp(5f, 11f, urgency));
            bool reactionRequired = EnemyActionRequiresReaction();
            bool overhead = enemyActionKind != EnemyActionKind.JumpReaction;
            bool windowOpen = reactionRequired &&
                              anticipation >= DefenseWindowStart01 &&
                              anticipation <= DefenseWindowEnd01;
            if (windowOpen != defenseWindowOpen)
            {
                defenseWindowOpen = windowOpen;
                screen.EnableInClassList("battle--reaction-ready", windowOpen && !defenseResolved);
            }
            enemyRenderer.color = Color.Lerp(
                Color.white,
                enemyTelegraphColor,
                urgency * (reactionRequired ? .12f + pulse * .3f : .06f + pulse * .12f));
            enemyRenderer.transform.position = enemyBattleBasePosition + new Vector3(
                Mathf.Lerp(.18f, -.14f, anticipation),
                overhead ? Mathf.Lerp(.035f, -.015f, anticipation) : Mathf.Lerp(-.035f, -.07f, anticipation),
                0f);
            enemyRenderer.transform.rotation = Quaternion.Euler(
                0f,
                0f,
                Mathf.Lerp(overhead ? -1.5f : 1.5f, 0f, anticipation));
            enemyRenderer.transform.localScale = Vector3.one * EnemyBattleScale;
            SyncMainEnemyShadow();
            UpdateEnemyTelegraphPulse(pulse, anticipation);
            if (defenseClock >= defenseDuration && defenseRoutine == null)
            {
                bool correct = (enemyActionKind == EnemyActionKind.JumpReaction && defenseAction == DefenseAction.Jump) ||
                               (enemyActionKind == EnemyActionKind.BraceReaction && defenseAction == DefenseAction.Brace);
                defenseRoutine = StartCoroutine(ResolveEnemyTurnRoutine(
                    reactionRequired && correct && defenseInputGrade == DefenseInputGrade.Success,
                    battleRevision));
            }
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

        private IEnumerator ResolveEnemyTurnRoutine(bool defenseSuccess, int revision)
        {
            yield return WaitForBattleSeconds(.18f, revision);
            if (revision != battleRevision || phase != Phase.Battle) yield break;
            bool overhead = enemyActionKind != EnemyActionKind.JumpReaction;
            SetEnemyBattlePose(overhead
                ? EnemyBattlePose.HighImpact
                : EnemyBattlePose.LowImpact);
            Vector3 impactPosition = enemyBattleBasePosition + new Vector3(-.34f, 0f, 0f);
            Quaternion impactRotation = Quaternion.Euler(0f, 0f, overhead ? 1.5f : -1.5f);
            enemyRenderer.transform.position = impactPosition;
            enemyRenderer.transform.rotation = impactRotation;
            SyncMainEnemyShadow();
            yield return WaitForBattleSeconds(.1f, revision);
            if (revision != battleRevision || phase != Phase.Battle) yield break;

            int temporaryBlock = 0;
            if (defenseSuccess)
            {
                temporaryBlock = enemyActionKind == EnemyActionKind.BraceReaction
                    ? Mathf.Max(2, NextEnemyDamage() / 2)
                    : NextEnemyDamage() + 6;
                run.block += temporaryBlock;
            }

            BattleActionFx fx = BattleSystem.EndTurnFx(run, battle);
            if (defenseSuccess && temporaryBlock > 0)
                battleLog.text = $"{DefenseActionLabel(defenseAction)}成功：被害を{(fx.damageToPlayer <= 0 ? "完全に防いだ" : $"{fx.damageToPlayer}まで軽減した")}。";
            else if (EnemyActionRequiresReaction())
                battleLog.text = defenseInputGrade switch
                {
                    DefenseInputGrade.Early => $"早く動きすぎた。{battle.log}",
                    DefenseInputGrade.Late => $"反応が遅れた。{battle.log}",
                    DefenseInputGrade.Wrong => $"防ぎ方を誤った。{battle.log}",
                    _ => $"反応できなかった。{battle.log}"
                };
            else battleLog.text = battle.log;
            if (fx.damageToPlayer > 0 || fx.statusDamageToPlayer > 0)
                PlayBattleImpact(fx.damageToPlayer + fx.statusDamageToPlayer, true);
            RefreshPersistentUi();
            SetEnemyBattlePose(overhead
                ? EnemyBattlePose.HighAnticipation
                : EnemyBattlePose.LowAnticipation);
            float recoveryClock = 0f;
            const float recoveryDuration = .16f;
            while (recoveryClock < recoveryDuration)
            {
                if (!paused && !ledgerOpen && !transitionActive)
                    recoveryClock += Time.unscaledDeltaTime;
                float recovery = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(recoveryClock / recoveryDuration));
                enemyRenderer.transform.position = Vector3.Lerp(impactPosition, enemyBattleBasePosition, recovery);
                enemyRenderer.transform.rotation = Quaternion.Slerp(impactRotation, Quaternion.identity, recovery);
                SyncMainEnemyShadow();
                yield return null;
                if (revision != battleRevision || phase != Phase.Battle) yield break;
            }
            defenseActive = false;
            screen.RemoveFromClassList("battle--telegraph");
            screen.RemoveFromClassList("battle--reaction-jump");
            screen.RemoveFromClassList("battle--reaction-brace");
            screen.RemoveFromClassList("battle--reaction-ready");
            screen.RemoveFromClassList("battle--reaction-committed");
            SetEnemyTelegraphVisible(false);
            walker.SetBattleMotion(JourneyWalkCyclePrototype.BattleMotion.Idle);
            enemyRenderer.transform.position = enemyBattleBasePosition;
            enemyRenderer.transform.rotation = Quaternion.identity;
            enemyRenderer.transform.localScale = Vector3.one * EnemyBattleScale;
            SyncMainEnemyShadow();
            enemyRenderer.color = Color.white;
            SetEnemyBattlePose(EnemyBattlePose.Idle);
            if (fx.playerDefeated)
            {
                SetMainEnemyVisible(false);
                ShowResult("EXPEDITION FAILED", "配達続行不能", "DEV SIMULATIONを最初からやり直せます。", false);
                defenseRoutine = null;
                yield break;
            }
            if (fx.enemyDefeated)
            {
                SetMainEnemyVisible(false);
                ResolveRoute(new CourierLocationOutcome { success = true, performance = 2, message = "継続効果で番人を退けた。" });
                defenseRoutine = null;
                yield break;
            }
            RefreshBattleUi();
            defenseRoutine = null;
        }

        private static string DefenseActionLabel(DefenseAction action) => action == DefenseAction.Jump ? "ジャンプ" : action == DefenseAction.Brace ? "踏ん張り" : "防御";

        private void RefreshBattleUi()
        {
            if (battle == null) return;
            RefreshHpMeter(enemyHpFills[0], enemyHpTexts[0], battle.enemyHp, battle.enemyMaxHp);
            enemyIntentNames[0].text = NextEnemyActionName();
            enemyIntents[0].text = NextEnemyDamage().ToString();
            RefreshBlockBadge(enemyBlockBadges[0], enemyBlocks[0], battle.enemyBlock);
            RefreshStatusHost(enemyStatusHosts[0], battle.enemyStatuses);
            energyText.text = $"{run.energy} / {PackspireContent.Data.balance.baseEnergy}";
            RefreshHpMeter(battlePlayerHpFill, battlePlayerHp, run.hp, run.maxHp);
            RefreshBlockBadge(battlePlayerBlockBadge, battleBlock, run.block);
            RefreshStatusHost(playerStatusHost, run.statuses);
            drawPileText.text = run.draw.Count.ToString();
            discardPileText.text = run.discard.Count.ToString();
            bool commandAvailable = !defenseActive && !battleInputLocked && !pileOverlayOpen;
            drawPileButton.SetEnabled(commandAvailable);
            discardPileButton.SetEnabled(commandAvailable);
            int visibleCardCount = Mathf.Min(run.hand.Count, cardButtons.Length);
            var fanSlots = new System.Collections.Generic.List<(Button button, float depth)>(visibleCardCount);
            for (int index = 0; index < cardButtons.Length; index++)
            {
                Button button = cardButtons[index];
                bool present = index < run.hand.Count;
                button.EnableInClassList("is-hidden", !present);
                if (!present) continue;
                CardInstance card = run.hand[index];
                bool affordable = !card.unplayable && card.cost <= run.energy;
                JourneyBattleCardPresenter.Populate(button, card, affordable);
                float depth = BattleHandFanLayout.Apply(button, index, visibleCardCount);
                fanSlots.Add((button, depth));
                button.SetEnabled(affordable && commandAvailable);
            }
            if (battleHandRoot != null)
            {
                foreach (var slot in fanSlots.OrderByDescending(slot => slot.depth))
                {
                    battleHandRoot.Add(slot.button);
                }
            }
            CharacterDef character = CharacterSystem.OfRun(run);
            skillName.text = character?.activeSkillName ?? "スキル";
            skillState.text = run.activeSkillUsed ? "使用済み" : "使用可能";
            skillButton.EnableInClassList("skill--spent", run.activeSkillUsed);
            skillButton.tooltip = CharacterSystem.ActiveSkillTooltip(run);
            endTurnButton.SetEnabled(commandAvailable);
            skillButton.SetEnabled(commandAvailable && !run.activeSkillUsed);
            RefreshPersistentUi();
        }

        private static void RefreshHpMeter(VisualElement fill, Label text, int current, int maximum)
        {
            int safeMaximum = Mathf.Max(1, maximum);
            int safeCurrent = Mathf.Clamp(current, 0, safeMaximum);
            if (fill != null)
            {
                fill.style.width = new Length(100f * safeCurrent / safeMaximum, LengthUnit.Percent);
            }
            if (text != null)
            {
                text.text = $"{safeCurrent} / {safeMaximum}";
            }
        }

        private static void RefreshBlockBadge(VisualElement badge, Label count, int value)
        {
            if (badge == null || count == null) return;
            badge.EnableInClassList("is-hidden", value <= 0);
            count.text = Mathf.Max(0, value).ToString();
        }

        private static void RefreshStatusHost(VisualElement host, List<StatusState> statuses)
        {
            if (host == null) return;
            host.Clear();
            if (statuses == null) return;
            foreach (StatusState status in statuses.Take(5))
            {
                var definition = ContentDatabase.Status(status.type);
                var chip = new VisualElement { pickingMode = PickingMode.Position };
                chip.AddToClassList("ps-journey__status-chip");
                chip.EnableInClassList("status--debuff", definition != null && definition.kind == "debuff");
                chip.tooltip = definition != null
                    ? $"{definition.name}\n{definition.description}"
                    : status.type;

                var icon = new Label(definition?.icon ?? "◆") { pickingMode = PickingMode.Ignore };
                icon.AddToClassList("ps-journey__status-icon");
                chip.Add(icon);

                var count = new Label(status.amount.ToString()) { pickingMode = PickingMode.Ignore };
                count.AddToClassList("ps-journey__status-count");
                chip.Add(count);
                host.Add(chip);
            }
        }

        private void OpenPileOverlay(bool drawPile)
        {
            if (phase != Phase.Battle || battle == null || defenseActive || battleInputLocked) return;
            List<CardInstance> cards = drawPile ? run.draw : run.discard;
            pileOverlayOpen = true;
            pileOverlay?.AddToClassList("pile-overlay--open");
            screen?.AddToClassList("battle--pile-open");
            pileTitle.text = drawPile ? "山札" : "捨札";
            pileSummary.text = drawPile
                ? $"{cards.Count}枚・次に引くカードから表示"
                : $"{cards.Count}枚・新しく捨てたカードから表示";
            pileCardHost.Clear();

            foreach (CardInstance card in cards.AsEnumerable().Reverse())
            {
                var preview = new Button
                {
                    focusable = false,
                    tooltip = card.name
                };
                preview.AddToClassList("ps-journey__pile-card");
                preview.AddToClassList("ps-battle-card-standard");
                JourneyBattleCardPresenter.Populate(preview, card, true);
                pileCardHost.Add(preview);
            }

            pileEmpty.EnableInClassList("is-hidden", cards.Count > 0);
            RefreshBattleUi();
        }

        private void ClosePileOverlay()
        {
            pileOverlayOpen = false;
            pileOverlay?.RemoveFromClassList("pile-overlay--open");
            screen?.RemoveFromClassList("battle--pile-open");
            pileCardHost?.Clear();
            if (phase == Phase.Battle && battle != null) RefreshBattleUi();
        }

        private int NextEnemyDamage()
        {
            if (battle?.enemy?.damages == null || battle.enemy.damages.Length == 0) return 0;
            int index = BattleSystem.NextEnemyMoveIndex(battle);
            return battle.enemy.damages[Mathf.Clamp(index, 0, battle.enemy.damages.Length - 1)];
        }

        private EnemyActionKind NextEnemyAction()
        {
            int index = battle == null ? 0 : BattleSystem.NextEnemyMoveIndex(battle);
            return WardenActionSequence[Mathf.Abs(index) % WardenActionSequence.Length];
        }

        private bool EnemyActionRequiresReaction()
        {
            return enemyActionKind == EnemyActionKind.JumpReaction ||
                   enemyActionKind == EnemyActionKind.BraceReaction;
        }

        private string NextEnemyActionName()
        {
            return NextEnemyAction() switch
            {
                EnemyActionKind.JumpReaction => "足払い",
                EnemyActionKind.BraceReaction => "重撃",
                _ => "鐘撃"
            };
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
            }
        }

        private void SetEnemyTelegraphVisible(bool visible)
        {
            if (enemyTelegraphRenderer == null) return;
            enemyTelegraphRenderer.enabled = visible;
            if (visible) SyncEnemyTelegraphTransform(0f, 0f);
        }

        private void UpdateEnemyTelegraphPulse(float pulse, float anticipation)
        {
            if (enemyTelegraphRenderer == null || !enemyTelegraphRenderer.enabled) return;
            SyncEnemyTelegraphTransform(pulse, anticipation);
            enemyTelegraphRenderer.color = new Color(
                enemyTelegraphColor.r,
                enemyTelegraphColor.g,
                enemyTelegraphColor.b,
                Mathf.Lerp(.1f, .48f, pulse));
        }

        private void SyncEnemyTelegraphTransform(float pulse, float anticipation)
        {
            if (enemyTelegraphRenderer == null || enemyRenderer == null) return;
            enemyTelegraphRenderer.transform.position = enemyRenderer.transform.position;
            enemyTelegraphRenderer.transform.rotation = enemyRenderer.transform.rotation;
            float haloScale = 1.075f + pulse * .035f + anticipation * .018f;
            enemyTelegraphRenderer.transform.localScale = enemyRenderer.transform.localScale * haloScale;
        }

        private void SetBattlePreviewEnemyCount(int count)
        {
            int visibleCount = Mathf.Clamp(count, 1, enemyHudSlots.Length);
            bool layoutPreview = visibleCount > 1;
            screen.EnableInClassList("battle--enemy-count-2", visibleCount == 2);
            screen.EnableInClassList("battle--enemy-count-3", visibleCount == 3);
            screen.EnableInClassList("battle--layout-preview", layoutPreview);
            battleInputLocked = encounterIntroActive || layoutPreview;
            for (int enemyIndex = 0; enemyIndex < enemyHudSlots.Length; enemyIndex++)
            {
                bool visible = enemyIndex < visibleCount;
                enemyHudSlots[enemyIndex].EnableInClassList("is-hidden", !visible);
                if (!visible && enemyIndex > 0)
                {
                    RefreshBlockBadge(enemyBlockBadges[enemyIndex], enemyBlocks[enemyIndex], 0);
                    RefreshStatusHost(enemyStatusHosts[enemyIndex], null);
                }
                if (enemyIndex > 0)
                {
                    SpriteRenderer renderer = battlePreviewEnemyRenderers[enemyIndex - 1];
                    if (renderer != null) renderer.enabled = visible;
                    SpriteRenderer shadow = battlePreviewEnemyShadowRenderers[enemyIndex - 1];
                    if (shadow != null) shadow.enabled = visible;
                }
            }

            if (visibleCount > 1)
            {
                enemyNames[1].text = "灰路の追跡者";
                enemyIntentNames[1].text = "斬撃";
                enemyIntents[1].text = "6";
                RefreshHpMeter(enemyHpFills[1], enemyHpTexts[1], 18, 24);
                RefreshBlockBadge(enemyBlockBadges[1], enemyBlocks[1], 3);
                RefreshStatusHost(enemyStatusHosts[1], new List<StatusState>
                {
                    new StatusState { type = "burn", amount = 2, duration = 2 }
                });
            }
            if (visibleCount > 2)
            {
                enemyNames[2].text = "鐘楼の射手";
                enemyIntentNames[2].text = "射撃";
                enemyIntents[2].text = "5";
                RefreshHpMeter(enemyHpFills[2], enemyHpTexts[2], 14, 18);
                RefreshBlockBadge(enemyBlockBadges[2], enemyBlocks[2], 0);
                RefreshStatusHost(enemyStatusHosts[2], new List<StatusState>
                {
                    new StatusState { type = "weak", amount = 1, duration = 1 },
                    new StatusState { type = "poison", amount = 3, duration = 0 }
                });
            }
            RefreshBattleUi();
        }

        /// <summary>
        /// Developer capture hook for the three-enemy layout stress state. It does
        /// not alter battle resolution; only the additional preview actors and HUDs
        /// are enabled so the 1280x720 composition can be inspected before multi-enemy
        /// battle logic is introduced.
        /// </summary>
        public void DevPreviewBattleEnemyCount(int count)
        {
            if (!uiBound) BindUi();
            if (!uiBound) return;
            if (phase != Phase.Battle) BeginBattle();
            SetBattlePreviewEnemyCount(count);
            battleLog.text = count >= 3
                ? "3体編成の構図確認中。カード操作は停止し、人物と体力表示の重なりだけを検査する。"
                : "単体敵レイアウト確認。";
        }

        /// <summary>
        /// Developer-only visual state for checking the defensive membrane and
        /// shield seal without spending a card in the preview battle.
        /// </summary>
        public void DevPreviewBattleBlock(int value)
        {
            if (!uiBound) BindUi();
            if (!uiBound) return;
            if (phase != Phase.Battle) BeginBattle();
            run.block = Mathf.Max(0, value);
            RefreshBattleUi();
            battleLog.text = value > 0
                ? $"防御表示確認：青い防御膜と防御印を表示中（{value}）。"
                : "防御表示を解除。";
        }

        public void DevPreviewBattleHandSize(int count)
        {
            if (!uiBound) BindUi();
            if (!uiBound) return;
            if (phase != Phase.Battle) BeginBattle();
            int requested = Mathf.Clamp(count, 1, cardButtons.Length);
            if (run.deck == null || run.deck.Count == 0) return;
            while (run.hand.Count < requested)
            {
                CardInstance source = run.deck[run.hand.Count % run.deck.Count];
                run.hand.Add(source.Clone());
            }
            while (run.hand.Count > requested)
            {
                run.hand.RemoveAt(run.hand.Count - 1);
            }
            battleInputLocked = true;
            screen.AddToClassList("battle--layout-preview");
            RefreshBattleUi();
            battleLog.text = $"手札{requested}枚の構図確認中。カード同士の重なり、読める範囲、左右操作欄との干渉を検査する。";
        }

    }
}
