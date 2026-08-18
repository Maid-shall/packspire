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
        private const int RealtimeReelSlotCount = 10;
        private const int RealtimeSupplySlotCount = 4;
        private const float RealtimeReelCardHeight = 68f;
        private const float RealtimeSupplyMarkerHeight = 52f;
        private const float RealtimeReelFallbackTravel = 470f;
        private const string WardenTimelineResource = "Data/Journey/WardenRealtimeTimeline";

        private sealed class ThreatSlotView
        {
            public readonly VisualElement Root;
            public readonly Image Art;
            public readonly VisualElement Icon;
            public readonly VisualElement Value;
            public readonly Image[] ValueGlyphs;
            public readonly Image BundleCount;
            public bool Occupied;

            public ThreatSlotView(VisualElement root)
            {
                Root = root;
                Art = root.Q<Image>(className: "ps-journey__reel-card-art");
                Icon = root.Q<VisualElement>(className: "ps-journey__reel-card-icon");
                Value = root.Q<VisualElement>(className: "ps-journey__reel-card-value");
                ValueGlyphs = new[]
                {
                    root.Q<Image>(className: "ps-journey__reel-value-glyph--0"),
                    root.Q<Image>(className: "ps-journey__reel-value-glyph--1"),
                    root.Q<Image>(className: "ps-journey__reel-value-glyph--2")
                };
                BundleCount = root.Q<Image>(className: "ps-journey__reel-bundle-count");

                // The oversized printed value is a background motif in layout C.
                // Keep the action seal above it even when their bounds overlap.
                if (Value != null && Icon != null)
                    Value.PlaceBehind(Icon);
            }
        }

        private sealed class SupplySlotView
        {
            public readonly VisualElement Root;
            public readonly Label EnergyCount;
            public readonly Label DrawCount;

            public SupplySlotView(VisualElement root)
            {
                Root = root;
                EnergyCount = root.Q<Label>(className: "ps-journey__reel-resource-energy-count");
                DrawCount = root.Q<Label>(className: "ps-journey__reel-resource-draw-count");
            }
        }

        private readonly RealtimeBattleController realtimeBattle = new RealtimeBattleController();
        private readonly System.Collections.Generic.List<RealtimeEnemyActionPreview> realtimeActionPreviews =
            new System.Collections.Generic.List<RealtimeEnemyActionPreview>(8);
        private readonly System.Collections.Generic.List<RealtimeSupplyPulsePreview> realtimeSupplyPreviews =
            new System.Collections.Generic.List<RealtimeSupplyPulsePreview>(4);
        private readonly ThreatSlotView[] threatSlots = new ThreatSlotView[RealtimeReelSlotCount];
        private readonly SupplySlotView[] supplySlots = new SupplySlotView[RealtimeSupplySlotCount];
        private VisualElement reelChainTrack;
        private VisualElement reelSlotsViewport;
        private VisualElement reelNow;
        private Texture2D reelNumeralAtlas;
        private Button battlePauseButton;
        private Button battleSpeedButton;
        private bool realtimeEventsBound;
        private bool realtimeBattleActive;
        private int lastSupplyPulseCount;
        private float realtimeTimelineHoldRemaining;
        private float realtimePlanningScale = RealtimePlanningNormalScale;
        private RealtimeEnemyTimelinePlanner realtimeEnemyTimelinePlanner;

        private void BindRealtimeBattleUi(VisualElement root)
        {
            for (int index = 0; index < RealtimeReelSlotCount; index++)
                threatSlots[index] = new ThreatSlotView(root.Q<VisualElement>($"journey-threat-slot-{index}"));
            for (int index = 0; index < RealtimeSupplySlotCount; index++)
            {
                supplySlots[index] = new SupplySlotView(root.Q<VisualElement>($"journey-supply-slot-{index}"));
                // Supply bands share the time axis with action tickets. Keep the
                // bands readable through gaps while tickets always remain the foreground.
                supplySlots[index].Root.SendToBack();
            }
            reelChainTrack = root.Q<VisualElement>("journey-reel-chain-track");
            reelSlotsViewport = root.Q<VisualElement>("journey-reel-slots");
            reelNow = root.Q<VisualElement>("journey-reel-now");
            reelNumeralAtlas = PackspireResources.Load<Texture2D>("Art/Battle/UI/JourneyApproved/journey-reel-numeral-atlas-v1");
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
            if (!realtimeEventsBound)
            {
                realtimeBattle.EnemyTelegraphStarted += BeginRealtimeTelegraph;
                realtimeBattle.EnemyActionResolved += ResolveRealtimeEnemyAction;
                realtimeEventsBound = true;
            }

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
            RealtimeEnemyTimelineProfile timelineProfile =
                PackspireResources.Load<RealtimeEnemyTimelineProfile>(WardenTimelineResource);
            if (timelineProfile == null)
            {
                Debug.LogError($"Required realtime timeline profile is missing: {WardenTimelineResource}");
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
            RefreshRealtimeTimeline();
        }

        private void StopRealtimeBattle()
        {
            if (!realtimeBattleActive) return;
            realtimeBattleActive = false;
            realtimeBattle.Finish();
            realtimeTimelineHoldRemaining = 0f;
            realtimePlanningScale = RealtimePlanningNormalScale;
            realtimeEnemyTimelinePlanner = null;
            ClearRealtimeSlots();
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
                RefreshRealtimeTimeline();
                return 0f;
            }

            bool shouldPause = delta <= 0f || battleInputLocked;
            realtimeBattle.SetPaused(shouldPause);
            float timelineDelta = 0f;
            if (!shouldPause)
            {
                timelineDelta = delta * speedScale * planningScale;
                realtimeBattle.Tick(timelineDelta);
                realtimeEnemyTimelinePlanner?.EnsureScheduledThrough(
                    realtimeBattle.Time + RealtimeReelDisplayHorizon + RealtimeActionCommitMargin);
            }

            if (lastSupplyPulseCount != realtimeBattle.SupplyPulseCount)
            {
                int pulses = realtimeBattle.SupplyPulseCount - lastSupplyPulseCount;
                lastSupplyPulseCount = realtimeBattle.SupplyPulseCount;
                run.energy = realtimeBattle.Energy;
                int draws = pulses * realtimeBattle.DrawsPerSupplyPulse;
                while (draws-- > 0 && run.hand.Count < RealtimeHandLimit)
                    BattleSystem.Draw(run, 1);
                battleLog.text = "補給線を通過。エナジーと手札を補充した。";
                RefreshBattleUi();
            }

            RefreshRealtimeTimeline();
            return timelineDelta;
        }

        private float UpdateRealtimePlanningScale(float delta)
        {
            float target = PointerIsOverRealtimeHandCard()
                ? RealtimePlanningSlowScale
                : RealtimePlanningNormalScale;
            if (delta <= 0f)
                return realtimePlanningScale;

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
            Vector2 panelPosition = RuntimePanelUtils.ScreenToPanel(
                screen.panel,
                screenPosition);
            for (int index = 0; index < cardButtons.Length; index++)
            {
                Button card = cardButtons[index];
                if (card == null || card.ClassListContains("is-hidden")) continue;
                if (card.worldBound.Contains(panelPosition)) return true;
            }

            return false;
        }

        private void RefreshRealtimeTimeline()
        {
            if (threatSlots[0] == null) return;
            float reelTravel = RealtimeReelFallbackTravel;
            if (reelSlotsViewport != null && reelNow != null &&
                reelSlotsViewport.resolvedStyle.height > RealtimeReelCardHeight)
            {
                float measuredTravel = reelNow.worldBound.yMin -
                    reelSlotsViewport.worldBound.yMin - RealtimeReelCardHeight;
                if (measuredTravel > 0f) reelTravel = measuredTravel;
            }
            if (reelChainTrack != null)
            {
                const float chainTileStep = 53f;
                float pixelsPerSecond = reelTravel / (float)RealtimeReelDisplayHorizon;
                float chainOffset = (float)(realtimeBattle.Time * pixelsPerSecond % chainTileStep);
                reelChainTrack.style.translate = new Translate(
                    new Length(0f, LengthUnit.Pixel),
                    new Length(chainOffset, LengthUnit.Pixel));
            }
            ClearRealtimeSlots();
            realtimeBattle.GetUpcomingActions(realtimeActionPreviews);

            int viewIndex = 0;
            foreach (RealtimeEnemyActionPreview preview in realtimeActionPreviews)
            {
                if (viewIndex >= threatSlots.Length) break;
                if (preview.TimeUntilStart > RealtimeReelDisplayHorizon) continue;
                ThreatSlotView slot = threatSlots[viewIndex++];
                PlaceTimelineSlot(slot, preview.TimeUntilStart, reelTravel);
                PopulateThreatAction(slot, preview);
            }

            realtimeBattle.GetUpcomingSupplyPulses(
                realtimeSupplyPreviews,
                RealtimeReelDisplayHorizon);
            int supplyIndex = 0;
            foreach (RealtimeSupplyPulsePreview preview in realtimeSupplyPreviews)
            {
                if (supplyIndex >= supplySlots.Length) break;
                SupplySlotView slot = supplySlots[supplyIndex++];
                PlaceSupplySlot(slot, preview.TimeUntil, reelTravel);
                PopulateSupplySlot(slot, preview);
            }
        }

        private void ClearRealtimeSlots()
        {
            for (int index = 0; index < threatSlots.Length; index++)
            {
                ThreatSlotView slot = threatSlots[index];
                if (slot == null) continue;
                slot.Occupied = false;
                slot.Root.RemoveFromClassList("slot--occupied");
                slot.Root.RemoveFromClassList("slot--attack");
                slot.Root.RemoveFromClassList("slot--reaction-attack");
                slot.Root.RemoveFromClassList("slot--defense");
                slot.Root.RemoveFromClassList("slot--energy");
                slot.Root.RemoveFromClassList("slot--imminent");
                slot.Root.RemoveFromClassList("slot--with-energy");
                slot.Root.RemoveFromClassList("slot--bundle");
                ClearReelValue(slot);
                slot.BundleCount.image = null;
                slot.BundleCount.style.display = DisplayStyle.None;
                slot.Art.sprite = null;
                slot.Art.image = null;
                slot.Root.style.top = 0f;
            }

            for (int index = 0; index < supplySlots.Length; index++)
            {
                SupplySlotView slot = supplySlots[index];
                if (slot == null) continue;
                slot.Root.RemoveFromClassList("slot--occupied");
                slot.Root.RemoveFromClassList("slot--energy");
                slot.Root.RemoveFromClassList("slot--draw");
                slot.Root.RemoveFromClassList("slot--energy-stack");
                slot.Root.RemoveFromClassList("slot--draw-stack");
                slot.EnergyCount.text = string.Empty;
                slot.DrawCount.text = string.Empty;
                slot.Root.style.top = 0f;
            }
        }

        private static void PlaceTimelineSlot(ThreatSlotView slot, double timeUntil, float travel)
        {
            float pixelsPerSecond = travel / (float)RealtimeReelDisplayHorizon;
            float top = travel - (float)timeUntil * pixelsPerSecond;
            slot.Root.style.top = Mathf.Clamp(top, 0f, travel + RealtimeReelCardHeight * 1.35f);
        }

        private static void PlaceSupplySlot(SupplySlotView slot, double timeUntil, float travel)
        {
            float pixelsPerSecond = travel / (float)RealtimeReelDisplayHorizon;
            float top = travel - (float)timeUntil * pixelsPerSecond +
                RealtimeReelCardHeight - RealtimeSupplyMarkerHeight * .5f;
            slot.Root.style.top = Mathf.Clamp(
                top,
                -RealtimeSupplyMarkerHeight,
                travel + RealtimeReelCardHeight);
        }

        private void PopulateThreatAction(ThreatSlotView slot, RealtimeEnemyActionPreview preview)
        {
            bool defenseAction = IsDefenseAction(preview.Kind);
            slot.Occupied = true;
            slot.Root.AddToClassList("slot--occupied");
            slot.Root.EnableInClassList("slot--attack", !defenseAction);
            slot.Root.EnableInClassList("slot--reaction-attack", IsReactionAttack(preview.Kind));
            slot.Root.EnableInClassList("slot--defense", defenseAction);
            slot.Root.EnableInClassList("slot--imminent", preview.Telegraphing || preview.TimeUntil <= 1.1d);
            PopulateThreatActorArt(slot, preview.Kind);
            if (preview.HitCount > 1)
            {
                slot.Root.AddToClassList("slot--bundle");
                SetReelValue(slot, preview.Damage.ToString());
                SetReelGlyph(slot.BundleCount, preview.HitCount.ToString()[0]);
            }
            else if (defenseAction)
                SetReelValue(slot, preview.Damage.ToString());
            else
                SetReelValue(slot, preview.Damage.ToString());
        }

        private static void PopulateSupplySlot(
            SupplySlotView slot,
            RealtimeSupplyPulsePreview preview)
        {
            slot.Root.AddToClassList("slot--occupied");
            slot.Root.EnableInClassList("slot--energy", preview.EnergyDelta > 0);
            slot.Root.EnableInClassList("slot--draw", preview.DrawCount > 0);
            slot.Root.EnableInClassList("slot--energy-stack", preview.EnergyDelta > 1);
            slot.Root.EnableInClassList("slot--draw-stack", preview.DrawCount > 1);
            slot.EnergyCount.text = SupplyAmount("エナジー", preview.EnergyDelta);
            slot.DrawCount.text = SupplyAmount("手札", preview.DrawCount);
        }

        private static string SupplyAmount(string label, int amount) =>
            amount > 0 ? $"{label}  +{amount}" : string.Empty;

        private void ClearReelValue(ThreatSlotView slot)
        {
            slot.Value.RemoveFromClassList("value--single");
            slot.Value.RemoveFromClassList("value--double");
            slot.Value.RemoveFromClassList("value--triple");
            slot.Value.tooltip = string.Empty;
            foreach (Image glyph in slot.ValueGlyphs)
            {
                glyph.image = null;
                glyph.style.display = DisplayStyle.None;
            }
        }

        private void SetReelValue(ThreatSlotView slot, string value)
        {
            ClearReelValue(slot);
            if (string.IsNullOrEmpty(value)) return;

            int glyphCount = Mathf.Min(value.Length, slot.ValueGlyphs.Length);
            slot.Value.AddToClassList(glyphCount switch
            {
                1 => "value--single",
                2 => "value--double",
                _ => "value--triple"
            });
            slot.Value.tooltip = value;
            for (int index = 0; index < glyphCount; index++)
                SetReelGlyph(slot.ValueGlyphs[index], value[index]);
        }

        private void SetReelGlyph(Image glyph, char character)
        {
            int atlasIndex;
            if (character >= '0' && character <= '9')
            {
                atlasIndex = character - '0';
            }
            else
            {
                atlasIndex = character switch
                {
                    '+' => 10,
                    '-' => 11,
                    '×' => 12,
                    _ => -1
                };
            }
            if (reelNumeralAtlas == null || atlasIndex < 0)
            {
                glyph.image = null;
                glyph.style.display = DisplayStyle.None;
                return;
            }

            int column = atlasIndex % 4;
            int row = atlasIndex / 4;
            const int atlasColumns = 4;
            const int atlasRows = 4;
            const int pixelInset = 2;
            int xMin = Mathf.RoundToInt(column * reelNumeralAtlas.width / (float)atlasColumns) + pixelInset;
            int xMax = Mathf.RoundToInt((column + 1) * reelNumeralAtlas.width / (float)atlasColumns) - pixelInset;
            int yMin = reelNumeralAtlas.height
                - Mathf.RoundToInt((row + 1) * reelNumeralAtlas.height / (float)atlasRows)
                + pixelInset;
            int yMax = reelNumeralAtlas.height
                - Mathf.RoundToInt(row * reelNumeralAtlas.height / (float)atlasRows)
                - pixelInset;
            glyph.image = reelNumeralAtlas;
            glyph.uv = new Rect(
                xMin / (float)reelNumeralAtlas.width,
                yMin / (float)reelNumeralAtlas.height,
                (xMax - xMin) / (float)reelNumeralAtlas.width,
                (yMax - yMin) / (float)reelNumeralAtlas.height);
            glyph.scaleMode = ScaleMode.ScaleToFit;
            glyph.style.display = DisplayStyle.Flex;
        }

        private void PopulateThreatActorArt(ThreatSlotView slot, RealtimeEnemyActionKind kind)
        {
            slot.Art.scaleMode = ScaleMode.ScaleAndCrop;
            if (enemyBattleFrames.Length != 6)
            {
                slot.Art.sprite = enemyRenderer != null ? enemyRenderer.sprite : null;
                return;
            }
            EnemyBattlePose pose = kind switch
            {
                RealtimeEnemyActionKind.JumpReaction => EnemyBattlePose.LowAnticipation,
                RealtimeEnemyActionKind.BraceReaction => EnemyBattlePose.HighAnticipation,
                RealtimeEnemyActionKind.ComboAttack => EnemyBattlePose.HighAnticipation,
                _ => EnemyBattlePose.Idle
            };
            slot.Art.sprite = enemyBattleFrames[(int)pose];
        }

        private static bool IsDefenseAction(RealtimeEnemyActionKind kind)
        {
            return kind == RealtimeEnemyActionKind.Guard;
        }

        private static bool IsReactionAttack(RealtimeEnemyActionKind kind)
        {
            return kind == RealtimeEnemyActionKind.JumpReaction ||
                kind == RealtimeEnemyActionKind.BraceReaction;
        }

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
            SetEnemyBattlePose(overhead ? EnemyBattlePose.HighAnticipation : EnemyBattlePose.LowAnticipation);
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
            // Freeze only the simulation clock at NOW. Presentation coroutines use
            // unscaled time, so the impact remains crisp while the schedule holds.
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
                if (sequenceEnd)
                {
                    FinishRealtimeTelegraph();
                }
                return;
            }

            bool correctReaction = defenseInputGrade == DefenseInputGrade.Success &&
                ((enemyActionKind == EnemyActionKind.JumpReaction && defenseAction == DefenseAction.Jump) ||
                 (enemyActionKind == EnemyActionKind.BraceReaction && defenseAction == DefenseAction.Brace));
            int effectiveDamage = correctReaction
                ? enemyActionKind == EnemyActionKind.JumpReaction ? 0 : Mathf.CeilToInt(action.Damage * .5f)
                : action.Damage;
            string actionName = ThreatName(action.ActionId);
            BattleActionFx fx = BattleSystem.ResolveRealtimeEnemyHit(run, battle, effectiveDamage, actionName, sequenceEnd);
            if (fx.damageToPlayer > 0) PlayBattleImpact(fx.damageToPlayer, true);
            battleLog.text = correctReaction
                ? $"{DefenseActionLabel(defenseAction)}成功。{actionName}をしのいだ。"
                : battle.log;
            RefreshBattleUi();

            if (!sequenceEnd) return;
            FinishRealtimeTelegraph();
            if (fx.playerDefeated)
            {
                realtimeBattleActive = false;
                realtimeBattle.Finish();
                SetMainEnemyVisible(false);
                ShowResult("EXPEDITION FAILED", "配達続行不能", "DEV SIMULATIONを最初からやり直せます。", false);
            }
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

        private static string ThreatName(string actionId)
        {
            int separator = actionId?.IndexOf(':') ?? -1;
            return separator >= 0 && separator + 1 < actionId.Length
                ? actionId.Substring(separator + 1)
                : "攻撃";
        }
    }
}
