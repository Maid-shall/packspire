using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire
{
    public sealed partial class JourneyTravelGameplayPrototype
    {
        private const int RealtimeInitialEnergy = 2;
        private const int RealtimeMaximumEnergy = 4;
        private const int RealtimeEnergyPerSupplyPulse = 2;
        private const int RealtimeDrawsPerSupplyPulse = 2;
        private const double RealtimeEnergyInterval = 6.4d;
        private const double RealtimeReelDisplayHorizon = 10d;
        private const double RealtimeActionCommitMargin = 3d;
        private const float RealtimePlanningSlowScale = .55f;
        private const float RealtimePlanningNormalScale = 1f;
        private const float RealtimePlanningScaleTransition = .1f;
        private const float RealtimeNowHoldDuration = .24f;
        private const int RealtimeHandLimit = 8;
        private const string ReelNumeralAtlasResource =
            "Art/Battle/UI/JourneyApproved/journey-reel-numeral-atlas-v1";

        private readonly RealtimeBattleController realtimeBattle = new();
        private Button battlePauseButton;
        private Button battleSpeedButton;
        private bool realtimeEventsBound;
        private bool realtimeBattleActive;
        private int lastSupplyPulseCount;
        private float realtimeTimelineHoldRemaining;
        private float realtimePlanningScale = RealtimePlanningNormalScale;
        private RealtimeEnemyTimelinePlanner realtimeEnemyTimelinePlanner;
        private JourneyBattleReelPresenter realtimeReelPresenter;
        private int lastAttackBoostDisplaySecond = -1;
        private int lastGuardBoostDisplaySecond = -1;

        private void BindRealtimeBattleUi(VisualElement root)
        {
            realtimeReelPresenter = new JourneyBattleReelPresenter(
                root,
                RealtimeReelDisplayHorizon,
                PackspireResources.Load<Texture2D>(ReelNumeralAtlasResource),
                RealtimeActionActorSprite);
            battlePauseButton = root.Q<Button>("journey-battle-pause");
            battleSpeedButton = root.Q<Button>("journey-battle-speed");
            battlePauseButton.clicked += TogglePause;
            battleSpeedButton.clicked += ToggleSpeed;
            RefreshRealtimeTransport();
        }

        private void RefreshRealtimeTransport()
        {
            if (battlePauseButton != null) battlePauseButton.text = paused ? "▶" : "Ⅱ";
            if (battleSpeedButton != null) battleSpeedButton.text = speedScale > 1f ? "×2" : "×1";
        }

        private void StartRealtimeBattle()
        {
            BindRealtimeBattleEvents();
            realtimeBattleActive = true;
            realtimeBattle.Start(
                RealtimeInitialEnergy,
                RealtimeMaximumEnergy,
                RealtimeEnergyInterval,
                RealtimeEnergyPerSupplyPulse,
                RealtimeDrawsPerSupplyPulse);
            run.energy = realtimeBattle.Energy;
            realtimeTimelineHoldRemaining = 0f;
            realtimePlanningScale = RealtimePlanningNormalScale;
            lastAttackBoostDisplaySecond = -1;
            lastGuardBoostDisplaySecond = -1;

            RealtimeEnemyTimelineProfile timelineProfile = encounterProfile.timeline;
            if (timelineProfile == null)
            {
                Debug.LogError($"Journey encounter '{encounterProfile.name}' has no realtime timeline profile.");
                realtimeBattleActive = false;
                realtimeBattle.Finish();
                return;
            }

            realtimeEnemyTimelinePlanner = new RealtimeEnemyTimelinePlanner(
                realtimeBattle,
                timelineProfile.actorId,
                timelineProfile.BuildPatterns());
            realtimeEnemyTimelinePlanner.Reset();
            realtimeEnemyTimelinePlanner.EnsureScheduledThrough(
                RealtimeReelDisplayHorizon + RealtimeActionCommitMargin);
            lastSupplyPulseCount = realtimeBattle.SupplyPulseCount;
            realtimeReelPresenter?.Refresh(realtimeBattle);
        }

        private void BindRealtimeBattleEvents()
        {
            if (realtimeEventsBound) return;
            realtimeBattle.EnemyTelegraphStarted += BeginRealtimeTelegraph;
            realtimeBattle.EnemyActionResolved += ResolveRealtimeEnemyAction;
            realtimeEventsBound = true;
        }

        private void StopRealtimeBattle()
        {
            if (!realtimeBattleActive) return;
            realtimeBattleActive = false;
            realtimeBattle.Finish();
            realtimeTimelineHoldRemaining = 0f;
            realtimePlanningScale = RealtimePlanningNormalScale;
            realtimeEnemyTimelinePlanner = null;
            consumablePresenter?.ClearHover();
            realtimeReelPresenter?.Clear();
        }

        private float UpdateRealtimeBattle(float delta)
        {
            if (!realtimeBattleActive || battle == null) return 0f;
            float planningScale = UpdateRealtimePlanningScale(delta);
            if (delta > 0f && realtimeTimelineHoldRemaining > 0f)
            {
                realtimeTimelineHoldRemaining = Mathf.Max(
                    0f,
                    realtimeTimelineHoldRemaining - delta);
                realtimeBattle.SetPaused(true);
                realtimeReelPresenter?.Refresh(realtimeBattle);
                return 0f;
            }

            // The pile sheet is a reading mode, not a planning slow-down. Keep this
            // explicit even though the journey update also supplies a zero delta.
            bool shouldPause = delta <= 0f || battleInputLocked || pileOverlayOpen;
            realtimeBattle.SetPaused(shouldPause);
            float timelineDelta = 0f;
            if (!shouldPause)
            {
                timelineDelta = delta * speedScale * planningScale;
                realtimeBattle.Tick(timelineDelta);
                realtimeEnemyTimelinePlanner?.EnsureScheduledThrough(
                    realtimeBattle.Time + RealtimeReelDisplayHorizon + RealtimeActionCommitMargin);
            }

            RefillFromSupplyPulses();
            RefreshRealtimeBoostCountdowns();
            realtimeReelPresenter?.Refresh(realtimeBattle);
            return timelineDelta;
        }

        private void RefreshRealtimeBoostCountdowns()
        {
            int attackSecond = Mathf.CeilToInt((float)realtimeBattle.AttackBonusRemaining);
            int guardSecond = Mathf.CeilToInt((float)realtimeBattle.GuardBonusRemaining);
            if (attackSecond == lastAttackBoostDisplaySecond &&
                guardSecond == lastGuardBoostDisplaySecond) return;
            lastAttackBoostDisplaySecond = attackSecond;
            lastGuardBoostDisplaySecond = guardSecond;
            RefreshStatusHost(playerStatusHost, run.statuses);
            AppendRealtimeConsumableStatuses();
        }

        private void RefillFromSupplyPulses()
        {
            if (lastSupplyPulseCount == realtimeBattle.SupplyPulseCount) return;

            int pulses = realtimeBattle.SupplyPulseCount - lastSupplyPulseCount;
            lastSupplyPulseCount = realtimeBattle.SupplyPulseCount;
            run.energy = realtimeBattle.Energy;
            int draws = pulses * realtimeBattle.DrawsPerSupplyPulse;
            while (draws-- > 0 && run.hand.Count < RealtimeHandLimit)
                BattleSystem.Draw(run, 1);
            battleLog.text = "補給線を通過。エナジーと手札を補充した。";
            RefreshBattleUi();
        }

        private float UpdateRealtimePlanningScale(float delta)
        {
            float target = PointerIsOverRealtimeHandCard() ||
                           (consumablePresenter?.IsPointerOverSlot ?? false)
                ? RealtimePlanningSlowScale
                : RealtimePlanningNormalScale;
            if (delta <= 0f) return realtimePlanningScale;

            float range = RealtimePlanningNormalScale - RealtimePlanningSlowScale;
            float changePerSecond = range / Mathf.Max(.01f, RealtimePlanningScaleTransition);
            realtimePlanningScale = Mathf.MoveTowards(
                realtimePlanningScale,
                target,
                changePerSecond * delta);
            return realtimePlanningScale;
        }

        private bool PointerIsOverRealtimeHandCard()
        {
            if (screen?.panel == null) return false;

            Vector2 screenPosition = Input.mousePosition;
            if (screenPosition.x < 0f || screenPosition.x > Screen.width ||
                screenPosition.y < 0f || screenPosition.y > Screen.height)
                return false;

            screenPosition.y = Screen.height - screenPosition.y;
            Vector2 panelPosition = RuntimePanelUtils.ScreenToPanel(screen.panel, screenPosition);
            foreach (Button card in cardButtons)
            {
                if (card == null || card.ClassListContains("is-hidden")) continue;
                if (card.worldBound.Contains(panelPosition)) return true;
            }
            return false;
        }

        private Sprite RealtimeActionActorSprite(RealtimeEnemyActionKind kind)
        {
            if (enemyBattleFrames.Length != 6)
                return enemyRenderer != null ? enemyRenderer.sprite : null;

            EnemyBattlePose pose = kind switch
            {
                RealtimeEnemyActionKind.JumpReaction => EnemyBattlePose.LowAnticipation,
                RealtimeEnemyActionKind.BraceReaction => EnemyBattlePose.HighAnticipation,
                RealtimeEnemyActionKind.ComboAttack => EnemyBattlePose.HighAnticipation,
                _ => EnemyBattlePose.Idle
            };
            return enemyBattleFrames[(int)pose];
        }

        private static bool IsDefenseAction(RealtimeEnemyActionKind kind) =>
            kind == RealtimeEnemyActionKind.Guard;

        private void BeginRealtimeTelegraph(RealtimeEnemyAction action, double remaining)
        {
            if (!realtimeBattleActive || battle == null) return;
            enemyActionKind = action.Kind switch
            {
                RealtimeEnemyActionKind.JumpReaction => EnemyActionKind.JumpReaction,
                RealtimeEnemyActionKind.BraceReaction => EnemyActionKind.BraceReaction,
                _ => EnemyActionKind.Normal
            };
            defenseActive = true;
            defenseResolved = !EnemyActionRequiresReaction();
            defenseWindowOpen = false;
            defenseAction = DefenseAction.None;
            defenseInputGrade = DefenseInputGrade.None;
            defenseInputTime = -1f;
            defenseClock = 0f;
            defenseDuration = Mathf.Max(.2f, (float)remaining);
            bool overhead = enemyActionKind != EnemyActionKind.JumpReaction;
            screen.EnableInClassList("battle--telegraph", EnemyActionRequiresReaction());
            screen.EnableInClassList("battle--reaction-jump", enemyActionKind == EnemyActionKind.JumpReaction);
            screen.EnableInClassList("battle--reaction-brace", enemyActionKind == EnemyActionKind.BraceReaction);
            screen.RemoveFromClassList("battle--reaction-ready");
            screen.RemoveFromClassList("battle--reaction-committed");
            enemyTelegraphColor = enemyActionKind switch
            {
                EnemyActionKind.JumpReaction => new Color(1f, .34f, .08f, 1f),
                EnemyActionKind.BraceReaction => new Color(.12f, .82f, 1f, 1f),
                _ => new Color(1f, .84f, .58f, 1f)
            };
            SetEnemyTelegraphVisible(EnemyActionRequiresReaction());
            SetEnemyBattlePose(overhead
                ? EnemyBattlePose.HighAnticipation
                : EnemyBattlePose.LowAnticipation);
            battleLog.text = action.Kind switch
            {
                RealtimeEnemyActionKind.JumpReaction => "橙の予兆。足払いへ合わせてジャンプする。",
                RealtimeEnemyActionKind.BraceReaction => "青白い予兆。大鐘撃へ合わせて踏ん張る。",
                RealtimeEnemyActionKind.Guard => "番人が鐘壁を展開し、防御を固める。",
                RealtimeEnemyActionKind.ComboAttack => "連鐘が迫る。三連撃の間隔に注意。",
                _ => "鐘撃が迫る。カードはそのまま使用できる。"
            };
        }

        private void ResolveRealtimeEnemyAction(RealtimeEnemyAction action)
        {
            if (!realtimeBattleActive || battle == null) return;
            realtimeTimelineHoldRemaining = Mathf.Max(
                realtimeTimelineHoldRemaining,
                RealtimeNowHoldDuration);
            bool sequenceEnd = action.SequenceIndex >= action.SequenceCount - 1;
            if (IsDefenseAction(action.Kind))
            {
                int gainedBlock = Mathf.Max(0, action.Damage);
                battle.enemyBlock += gainedBlock;
                battle.log = $"{ThreatName(action.ActionId)}：{gainedBlock}ブロック";
                battleLog.text = battle.log;
                RefreshBattleUi();
                if (sequenceEnd) FinishRealtimeTelegraph();
                return;
            }

            bool correctReaction = defenseInputGrade == DefenseInputGrade.Success &&
                ((enemyActionKind == EnemyActionKind.JumpReaction && defenseAction == DefenseAction.Jump) ||
                 (enemyActionKind == EnemyActionKind.BraceReaction && defenseAction == DefenseAction.Brace));
            int effectiveDamage = correctReaction
                ? enemyActionKind == EnemyActionKind.JumpReaction
                    ? 0
                    : Mathf.CeilToInt(action.Damage * .5f)
                : action.Damage;
            string actionName = ThreatName(action.ActionId);
            BattleActionFx fx = BattleSystem.ResolveRealtimeEnemyHit(
                run,
                battle,
                effectiveDamage,
                actionName,
                sequenceEnd);
            if (fx.damageToPlayer > 0) PlayBattleImpact(fx.damageToPlayer, true);
            battleLog.text = correctReaction
                ? $"{DefenseActionLabel(defenseAction)}成功。{actionName}をしのいだ。"
                : battle.log;
            RefreshBattleUi();

            if (!sequenceEnd) return;
            FinishRealtimeTelegraph();
            if (!fx.playerDefeated) return;

            realtimeBattleActive = false;
            realtimeBattle.Finish();
            SetMainEnemyVisible(false);
            ShowResult(
                "EXPEDITION FAILED",
                "配達続行不能",
                usesLiveRun
                    ? "荷を守り切れなかった。遠征結果へ進みます。"
                    : "DEV SIMULATIONを最初からやり直せます。",
                false);
        }

        private void FinishRealtimeTelegraph()
        {
            defenseActive = false;
            defenseResolved = false;
            defenseWindowOpen = false;
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
            enemyRenderer.color = Color.white;
            SetEnemyBattlePose(EnemyBattlePose.Idle);
            SyncMainEnemyShadow();
        }

        public void DevStartReactionPreview()
        {
            if (!uiBound) BindUi();
            if (!uiBound) return;
            if (phase != Phase.Battle) BeginBattle();
            if (battle == null || defenseActive) return;

            BindRealtimeBattleEvents();
            realtimeBattleActive = true;
            BeginRealtimeTelegraph(
                new RealtimeEnemyAction(
                    encounterProfile.timeline.actorId,
                    "preview:足払い",
                    RealtimeEnemyActionKind.JumpReaction,
                    8,
                    0,
                    1),
                1.15d);
            RefreshBattleUi();
        }

        private static string ThreatName(string actionId)
        {
            int separator = actionId?.IndexOf(':') ?? -1;
            return separator >= 0 && separator + 1 < actionId.Length
                ? actionId.Substring(separator + 1)
                : "攻撃";
        }
    }
}
