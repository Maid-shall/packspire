using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire
{
    public sealed partial class JourneyTravelGameplayPrototype
    {
        private const float DefenseWindowStart01 = .66f;
        private const float DefenseWindowEnd01 = .96f;

        private void BeginBattle()
        {
            ClosePileOverlay();
            battleRevision++;
            if (encounterRoutine != null) StopCoroutine(encounterRoutine);
            if (battlePresentationRoutine != null) StopCoroutine(battlePresentationRoutine);
            encounterRoutine = null;
            battlePresentationRoutine = null;
            SetPhase(Phase.Battle);
            scenery.ClearAll();
            defenseActive = false;
            enemyImpactHoldRemaining = 0f;
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
            CancelEnemyPoseRecovery();
            SetEnemyBattlePose(EnemyBattlePose.Idle);
            ApplyBattleActorLayout(1);
            enemyRenderer.transform.rotation = Quaternion.identity;
            enemyRenderer.transform.localScale = Vector3.one * enemyBattleBaseScale;
            enemyRenderer.color = Color.white;
            SetMainEnemyVisible(true);
            SyncMainEnemyShadow();
            EnemyDef enemy = encounterProfile.BuildEnemy();
            battle = BattleSystem.Begin(run, enemy, 1f);
            StartRealtimeBattle();
            SetBattlePreviewEnemyCount(1);
            enemyNames[0].text = enemy.name;
            battleLog.text = $"{enemy.name}が進路を塞いだ。";
            RefreshBattleUi();
            encounterRoutine = StartCoroutine(EncounterRoutine(battleRevision));
        }

        private void PlayCard(int index)
        {
            if (phase != Phase.Battle || battleInputLocked || pileOverlayOpen || battle == null || index >= run.hand.Count) return;
            CardInstance playedCard = run.hand[index];
            int attackBuffBeforePlay = run.attackBuff;
            if (playedCard.damage > 0 && realtimeBattle.AttackBonus > 0)
                run.attackBuff += realtimeBattle.AttackBonus;
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
            realtimeBattle.SetEnergy(run.energy);
            run.energy = realtimeBattle.Energy;
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
                CompleteJourneyBattleVictory();
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
            if (usesLiveRun && PackspireGame.Instance != null)
            {
                PackspireGame.Instance.UiOpenSeamlessJourneyBattleReward();
                return;
            }
            ResolveRoute(new CourierLocationOutcome
            {
                success = true,
                performance = 2,
                message = $"{encounterProfile.ResolvedDisplayName}を退けた。"
            });
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

        private void RefreshBattleUi()
        {
            using var performanceScope = PackspirePerformance.JourneyBattleRefresh.Auto();
            if (battle == null) return;
            RefreshHpMeter(enemyHpFills[0], enemyHpTexts[0], battle.enemyHp, battle.enemyMaxHp);
            RefreshBlockBadge(enemyBlockBadges[0], enemyBlocks[0], battle.enemyBlock);
            RefreshStatusHost(enemyStatusHosts[0], battle.enemyStatuses);
            int maximumEnergy = realtimeBattleActive
                ? realtimeBattle.MaximumEnergy
                : PackspireContent.Data.balance.baseEnergy;
            energyText.text = $"{run.energy} / {maximumEnergy}";
            RefreshHpMeter(battlePlayerHpFill, battlePlayerHp, run.hp, run.maxHp);
            RefreshBlockBadge(battlePlayerBlockBadge, battleBlock, run.block);
            RefreshStatusHost(playerStatusHost, run.statuses);
            AppendRealtimeConsumableStatuses();
            drawPileText.text = run.draw.Count.ToString();
            discardPileText.text = run.discard.Count.ToString();
            bool commandAvailable = !battleInputLocked && !pileOverlayOpen;
            drawPileButton.SetEnabled(commandAvailable);
            discardPileButton.SetEnabled(commandAvailable);
            consumablePresenter?.Refresh(run, commandAvailable);
            int visibleCardCount = Mathf.Min(run.hand.Count, cardButtons.Length);
            battleHandOrder.Clear();
            for (int index = 0; index < cardButtons.Length; index++)
            {
                Button button = cardButtons[index];
                bool present = index < run.hand.Count;
                button.EnableInClassList("is-hidden", !present);
                if (!present) continue;
                CardInstance card = run.hand[index];
                bool affordable = !card.unplayable && card.cost <= run.energy;
                battleCardViews[index].Bind(BuildJourneyBattleCardModel(card, affordable));
                float depth = BattleHandFanLayout.Apply(button, index, visibleCardCount);
                battleHandOrder.Add((button, depth));
                button.SetEnabled(affordable && commandAvailable);
            }
            if (battleHandRoot != null)
            {
                battleHandOrder.Sort(static (left, right) => right.depth.CompareTo(left.depth));
                foreach (var slot in battleHandOrder)
                    battleHandRoot.Add(slot.button);
            }
            RefreshPersistentUi();
        }

        private static BattleCardViewModel BuildJourneyBattleCardModel(
            CardInstance card,
            bool affordable)
        {
            return new BattleCardViewModel(
                card.name,
                card.cost.ToString(),
                card.text,
                JourneyBattleCardKind(card),
                affordable ? string.Empty : "LOW EN",
                PackspireUiFoundation.BattleCardArtwork(card.id),
                affordable);
        }

        private static string JourneyBattleCardKind(CardInstance card)
        {
            if (card.type == CardType.Attack || card.damage > 0) return "attack";
            if (card.block > 0 || card.heal > 0) return "defense";
            return "technique";
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
            int visibleCount = Mathf.Min(5, statuses.Count);
            for (int index = 0; index < visibleCount; index++)
            {
                StatusState status = statuses[index];
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
            if (phase != Phase.Battle || battle == null || battleInputLocked) return;
            List<CardInstance> cards = drawPile ? run.draw : run.discard;
            consumablePresenter?.ClearHover();
            pileOverlayOpen = true;
            pileOverlay?.AddToClassList("pile-overlay--open");
            screen?.AddToClassList("battle--pile-open");
            pileTitle.text = drawPile ? "山札" : "捨札";
            pileSummary.text = drawPile
                ? $"{cards.Count}枚・次に引くカードから表示"
                : $"{cards.Count}枚・新しく捨てたカードから表示";
            pileCardHost.Clear();

            for (int index = cards.Count - 1; index >= 0; index--)
            {
                CardInstance card = cards[index];
                var preview = new Button
                {
                    focusable = false,
                    tooltip = card.name
                };
                preview.AddToClassList("ps-journey__pile-card");
                preview.AddToClassList("ps-battle-card-standard");
                new BattleCardView(preview).Bind(BuildJourneyBattleCardModel(card, true));
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
