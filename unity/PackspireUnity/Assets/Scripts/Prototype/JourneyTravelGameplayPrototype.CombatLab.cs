
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire
{
    public sealed partial class JourneyTravelGameplayPrototype
    {
        private const int CombatLabTargetBattlesPerEnemy = 5;

        [Serializable]
        private sealed class CombatLabMeasurementReport
        {
            public string generatedAtUtc;
            public string qaPolicy = "fixed-perfect-reaction-aggressive-v1";
            public List<JourneyCombatMeasurementRecord> records = new();
        }

        private sealed class CombatLabAttackOption
        {
            public string ActionId;
            public RealtimeEnemyActionKind Kind;
            public RealtimeEnemyMotionLane MotionLane;
            public int Value;
            public float TelegraphLead;
            public int HitCount;
            public float HitSpacing;
        }

        private readonly List<JourneyBattleEncounterProfile> combatLabEnemies = new();
        private readonly List<CombatLabAttackOption> combatLabAttacks = new();
        private VisualElement combatLabRoot;
        private DropdownField combatLabBackground;
        private DropdownField combatLabEnemy;
        private DropdownField combatLabAttack;
        private Button combatLabPlay;
        private Label combatLabStatus;
        private Button combatLabMeasure;
        private Button combatLabClear;
        private Label combatLabSummary;
        private Coroutine combatLabPreviewRoutine;
        private Coroutine combatLabAutomationRoutine;
        private readonly JourneyCombatMeasurementSession combatLabMeasurements = new();
        private bool combatLabActive;
        private bool combatLabAutomationActive;
        private bool combatLabPreviousRunInBackground;
        private float combatLabAutoNextCardAt;
        private string combatLabBaselineRunJson;
        private int combatLabBaselineBattleWins;
        private int combatLabBackgroundIndex;
        private int combatLabEnemyIndex;
        private int combatLabAttackIndex;

        private void BindCombatLabUi(VisualElement root)
        {
            combatLabRoot = root.Q<VisualElement>("journey-combat-lab");
            combatLabBackground = root.Q<DropdownField>("journey-combat-lab-background");
            combatLabEnemy = root.Q<DropdownField>("journey-combat-lab-enemy");
            combatLabAttack = root.Q<DropdownField>("journey-combat-lab-attack");
            combatLabPlay = root.Q<Button>("journey-combat-lab-play");
            combatLabStatus = root.Q<Label>("journey-combat-lab-status");
            combatLabMeasure = root.Q<Button>("journey-combat-lab-measure");
            combatLabClear = root.Q<Button>("journey-combat-lab-clear");
            combatLabSummary = root.Q<Label>("journey-combat-lab-summary");

            combatLabBackground.RegisterValueChangedCallback(evt =>
            {
                int index = combatLabBackground.choices.IndexOf(evt.newValue);
                if (index >= 0) SelectCombatLabBackground(index);
            });
            combatLabEnemy.RegisterValueChangedCallback(evt =>
            {
                int index = combatLabEnemy.choices.IndexOf(evt.newValue);
                if (index >= 0) SelectCombatLabEnemy(index);
            });
            combatLabAttack.RegisterValueChangedCallback(evt =>
            {
                int index = combatLabAttack.choices.IndexOf(evt.newValue);
                if (index < 0) return;
                combatLabAttackIndex = index;
                RefreshCombatLabStatus();
            });
            combatLabPlay.clicked += PlayCombatLabAttack;
            combatLabMeasure.clicked += ToggleCombatLabMeasurement;
            combatLabClear.clicked += ClearCombatLabMeasurements;
            root.Q<Button>("journey-combat-lab-close").clicked += ReturnToDeveloperMenu;
        }

        public void DevBeginCombatLab()
        {
            if (!uiBound) BindUi();
            if (!uiBound) return;
            if (combatLabRoot == null && document?.rootVisualElement != null)
                BindCombatLabUi(document.rootVisualElement);
            if (combatLabRoot == null)
            {
                Debug.LogError("Combat lab UI is missing from the journey view.");
                return;
            }

            combatLabActive = true;
            toastClock = 0f;
            toast?.RemoveFromClassList("toast--visible");
            combatLabBaselineRunJson = JsonUtility.ToJson(run);
            combatLabBaselineBattleWins = run?.battlesWon ?? 0;
            combatLabRoot.AddToClassList("combat-lab--visible");
            combatLabBackground.choices = Enumerable.Range(0, 3)
                .Select(BiomeLabel)
                .ToList();
            combatLabBackgroundIndex = Mathf.Clamp(biomeIndex, 0, 2);
            combatLabBackground.SetValueWithoutNotify(
                combatLabBackground.choices[combatLabBackgroundIndex]);

            combatLabEnemies.Clear();
            combatLabEnemies.AddRange(
                PackspireResources.LoadAll<JourneyBattleEncounterProfile>(
                        JourneyEncounterSelectionSystem.ResourceDirectory)
                    .Where(profile => profile != null &&
                                      profile.allowNormalBattle &&
                                      profile.ResolvedTimeline != null)
                    .OrderBy(profile => profile.PopulationClass)
                    .ThenBy(profile => profile.StableEncounterId, StringComparer.Ordinal));
            combatLabEnemy.choices = combatLabEnemies
                .Select(profile => profile.ResolvedDisplayName)
                .ToList();
            combatLabEnemyIndex = Mathf.Max(
                0,
                combatLabEnemies.FindIndex(profile =>
                    profile == encounterProfile ||
                    string.Equals(
                        profile.StableEncounterId,
                        encounterProfile != null ? encounterProfile.StableEncounterId : string.Empty,
                        StringComparison.Ordinal)));
            if (combatLabEnemies.Count > 0)
            {
                combatLabEnemy.SetValueWithoutNotify(
                    combatLabEnemy.choices[combatLabEnemyIndex]);
                ApplyEncounterProfile(combatLabEnemies[combatLabEnemyIndex]);
            }

            ResetCombatLabBattle();
            ApplyCombatLabBackground();
            RebuildCombatLabAttacks();
            RefreshCombatLabStatus();
            RefreshCombatLabMeasurementUi();
        }

        private void SelectCombatLabBackground(int index)
        {
            if (!combatLabActive || combatLabMeasurements.Active) return;
            combatLabBackgroundIndex = Mathf.Clamp(index, 0, 2);
            ApplyCombatLabBackground();
            RefreshCombatLabStatus();
        }

        private void ApplyCombatLabBackground()
        {
            biomeIndex = combatLabBackgroundIndex;
            scenery?.ClearAll();
            walker.SetJourneyBiome(biomeIndex);
            environment.SetBiome(biomeIndex);
            weatherText.text = BiomeWeatherForIndex(biomeIndex);
        }

        private void SelectCombatLabEnemy(int index)
        {
            if (!combatLabActive || combatLabMeasurements.Active || combatLabEnemies.Count == 0) return;
            combatLabEnemyIndex = Mathf.Clamp(index, 0, combatLabEnemies.Count - 1);
            ApplyEncounterProfile(combatLabEnemies[combatLabEnemyIndex]);
            ResetCombatLabBattle();
            ApplyCombatLabBackground();
            RebuildCombatLabAttacks();
            RefreshCombatLabStatus();
            RefreshCombatLabMeasurementUi();
        }

        private void ResetCombatLabBattle()
        {
            StopCombatLabPreview();
            BeginBattle(false);
            StopRealtimeBattle();
            if (encounterRoutine != null)
            {
                StopCoroutine(encounterRoutine);
                encounterRoutine = null;
            }
            encounterIntroActive = false;
            battleInputLocked = true;
            encounterBanner?.RemoveFromClassList("encounter--visible");
            SetPaused(true);
            combatLabRoot.AddToClassList("combat-lab--visible");
            RefreshBattleUi();
        }

        private void RebuildCombatLabAttacks()
        {
            combatLabAttacks.Clear();
            var keys = new HashSet<string>(StringComparer.Ordinal);
            try
            {
                RealtimeEnemyTimelinePattern[] patterns = encounterProfile.BuildTimelinePatterns();
                foreach (RealtimeEnemyTimelinePattern pattern in patterns)
                foreach (RealtimeEnemyTimelineStep step in pattern.Steps)
                {
                    string key = string.Join("|",
                        step.Kind,
                        step.MotionLane,
                        step.TelegraphLead.ToString("F3"),
                        step.HitCount,
                        step.HitSpacing.ToString("F3"));
                    if (!keys.Add(key)) continue;
                    combatLabAttacks.Add(new CombatLabAttackOption
                    {
                        ActionId = step.ActionId,
                        Kind = step.Kind,
                        MotionLane = step.MotionLane,
                        Value = step.Damage,
                        TelegraphLead = Mathf.Max(.2f, (float)step.TelegraphLead),
                        HitCount = Mathf.Max(1, step.HitCount),
                        HitSpacing = Mathf.Max(.01f, (float)step.HitSpacing)
                    });
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Combat lab could not build timeline combinations: {exception.Message}");
            }

            if (combatLabAttacks.Count == 0)
            {
                combatLabAttacks.Add(new CombatLabAttackOption
                {
                    ActionId = "combat-lab:fallback",
                    Kind = RealtimeEnemyActionKind.NormalAttack,
                    MotionLane = RealtimeEnemyMotionLane.Default,
                    Value = 0,
                    TelegraphLead = .8f,
                    HitCount = 1,
                    HitSpacing = .15f
                });
            }

            combatLabAttack.choices = combatLabAttacks
                .Select((option, index) => CombatLabAttackLabel(option, index))
                .ToList();
            combatLabAttackIndex = 0;
            combatLabAttack.SetValueWithoutNotify(combatLabAttack.choices[0]);
            combatLabPlay.SetEnabled(combatLabAttacks.Count > 0);
        }

        private static string CombatLabAttackLabel(
            CombatLabAttackOption option,
            int index)
        {
            string kind = option.Kind switch
            {
                RealtimeEnemyActionKind.ComboAttack => "連撃",
                RealtimeEnemyActionKind.Guard => "防御",
                RealtimeEnemyActionKind.JumpReaction => "ジャンプ対応",
                RealtimeEnemyActionKind.BraceReaction => "踏ん張り対応",
                _ => "攻撃"
            };
            string lane = option.MotionLane switch
            {
                RealtimeEnemyMotionLane.High => "上段",
                RealtimeEnemyMotionLane.Low => "下段",
                _ => "標準"
            };
            string hits = option.HitCount > 1 ? $" / {option.HitCount}連" : string.Empty;
            return $"{index + 1:00}  {kind} / {lane} / 予兆{option.TelegraphLead:0.00}秒{hits}";
        }

        private void RefreshCombatLabStatus()
        {
            if (!combatLabActive || combatLabAttacks.Count == 0) return;
            combatLabAttackIndex = Mathf.Clamp(
                combatLabAttackIndex,
                0,
                combatLabAttacks.Count - 1);
            CombatLabAttackOption option = combatLabAttacks[combatLabAttackIndex];
            float flashAt = option.TelegraphLead * DefenseWindowStart01;
            float reactionWindow =
                option.TelegraphLead * (DefenseWindowEnd01 - DefenseWindowStart01);
            string reaction = option.Kind switch
            {
                RealtimeEnemyActionKind.JumpReaction => "ジャンプ入力",
                RealtimeEnemyActionKind.BraceReaction => "踏ん張り入力",
                _ => "入力なし"
            };
            combatLabStatus.text =
                $"{reaction} / {flashAt:0.00}秒で一度だけ発光 / " +
                $"猶予{reactionWindow:0.00}秒 / 本番ダメージなし";
        }

        private void PlayCombatLabAttack()
        {
            if (!combatLabActive || combatLabAttacks.Count == 0) return;
            StopCombatLabPreview();
            combatLabAttackIndex = Mathf.Clamp(
                combatLabAttackIndex,
                0,
                combatLabAttacks.Count - 1);
            combatLabPreviewRoutine = StartCoroutine(
                CombatLabPreviewRoutine(combatLabAttacks[combatLabAttackIndex]));
        }

        private IEnumerator CombatLabPreviewRoutine(CombatLabAttackOption option)
        {
            var action = new RealtimeEnemyAction(
                encounterProfile.ResolvedEnemyId,
                option.ActionId,
                option.Kind,
                option.Value,
                0,
                option.HitCount,
                option.MotionLane);

            realtimeBattleActive = true;
            BeginRealtimeTelegraph(action, option.TelegraphLead);
            realtimeBattleActive = false;
            combatLabStatus.text =
                $"{CombatLabAttackLabel(option, combatLabAttackIndex)} を再生中";

            while (defenseActive && defenseClock < defenseDuration)
            {
                UpdateDefense(Mathf.Max(.001f, Time.unscaledDeltaTime));
                yield return null;
            }

            FinishRealtimeTelegraph();
            for (int hit = 0; hit < option.HitCount; hit++)
            {
                BeginRealtimeEnemyImpact();
                while (enemyImpactHoldRemaining > 0f)
                {
                    UpdateRealtimeEnemyImpact(Mathf.Max(.001f, Time.unscaledDeltaTime));
                    yield return null;
                }
                if (hit + 1 < option.HitCount)
                {
                    float spacingRemaining = option.HitSpacing;
                    while (spacingRemaining > 0f)
                    {
                        float frameDelta = Mathf.Max(.001f, Time.unscaledDeltaTime);
                        UpdateBattleIdleMotion();
                        UpdateEnemyPoseRecovery(frameDelta);
                        spacingRemaining = Mathf.Max(0f, spacingRemaining - frameDelta);
                        yield return null;
                    }
                }
            }

            while (enemyPoseRecoveryRemaining > 0f)
            {
                float frameDelta = Mathf.Max(.001f, Time.unscaledDeltaTime);
                UpdateBattleIdleMotion();
                UpdateEnemyPoseRecovery(frameDelta);
                yield return null;
            }

            combatLabPreviewRoutine = null;
            RefreshCombatLabStatus();
        }

        public void DevRunCombatLabBatchMeasurement()
        {
            if (!combatLabActive || combatLabAutomationRoutine != null) return;
            if (combatLabMeasurements.Active) CancelCombatLabMeasurement();
            combatLabMeasurements.Clear();
            SaveCombatLabMeasurements();
            combatLabPreviousRunInBackground = Application.runInBackground;
            Application.runInBackground = true;
            combatLabAutomationRoutine = StartCoroutine(
                CombatLabBatchMeasurementRoutine());
        }

        private IEnumerator CombatLabBatchMeasurementRoutine()
        {
            combatLabAutomationActive = true;
            combatLabAutoNextCardAt = 0f;
            int targetTotal =
                combatLabEnemies.Count * CombatLabTargetBattlesPerEnemy;

            for (int enemyIndex = 0;
                 enemyIndex < combatLabEnemies.Count;
                 enemyIndex++)
            {
                SelectCombatLabEnemy(enemyIndex);
                string encounterId = encounterProfile.StableEncounterId;
                while (combatLabMeasurements.CompletedCount(encounterId) <
                       CombatLabTargetBattlesPerEnemy)
                {
                    StartCombatLabMeasurement();
                    float realTimeDeadline = Time.realtimeSinceStartup + 180f;
                    while (combatLabMeasurements.Active)
                    {
                        DriveCombatLabQaPlayer();
                        if (Time.realtimeSinceStartup >= realTimeDeadline)
                        {
                            Debug.LogError(
                                $"Combat measurement timed out: {encounterId}");
                            combatLabMeasurements.Cancel();
                            FinishCombatLabAutomation();
                            SetPaused(true);
                            yield break;
                        }
                        yield return null;
                    }

                    yield return new WaitForSecondsRealtime(.12f);
                }
            }

            FinishCombatLabAutomation();
            SetPaused(true);
            combatLabStatus.text =
                $"固定QAプレイヤーの自動計測完了：" +
                $"{combatLabMeasurements.Records.Count}/{targetTotal}戦";
            RefreshCombatLabMeasurementUi();
            SaveCombatLabMeasurements();
        }

        private void FinishCombatLabAutomation()
        {
            combatLabAutomationActive = false;
            combatLabAutomationRoutine = null;
            Application.runInBackground = combatLabPreviousRunInBackground;
        }

        private void DriveCombatLabQaPlayer()
        {
            if (!combatLabAutomationActive ||
                !combatLabMeasurements.Active ||
                battle == null)
                return;

            if (defenseActive &&
                !defenseResolved &&
                EnemyActionRequiresReaction() &&
                defenseClock >= defenseDuration *
                    ((DefenseWindowStart01 + DefenseWindowEnd01) * .5f))
            {
                ResolveDefenseInput(
                    enemyActionKind == EnemyActionKind.JumpReaction
                        ? DefenseAction.Jump
                        : DefenseAction.Brace);
            }

            if (battleInputLocked ||
                pileOverlayOpen ||
                Time.unscaledTime < combatLabAutoNextCardAt)
                return;

            int cardIndex = FindCombatLabQaCardIndex(true);
            if (cardIndex < 0) cardIndex = FindCombatLabQaCardIndex(false);
            if (cardIndex < 0) return;

            combatLabAutoNextCardAt = Time.unscaledTime + .08f;
            PlayCard(cardIndex);
        }

        private int FindCombatLabQaCardIndex(bool requireDamage)
        {
            if (run?.hand == null) return -1;
            for (int index = 0; index < run.hand.Count; index++)
            {
                CardInstance card = run.hand[index];
                if (card == null ||
                    card.unplayable ||
                    card.cost > run.energy ||
                    (requireDamage && card.damage <= 0))
                    continue;
                return index;
            }
            return -1;
        }

        private void ToggleCombatLabMeasurement()
        {
            if (!combatLabActive || encounterProfile == null) return;
            if (combatLabMeasurements.Active)
            {
                CancelCombatLabMeasurement();
                return;
            }

            StartCombatLabMeasurement();
        }

        private void StartCombatLabMeasurement()
        {
            if (run == null || string.IsNullOrEmpty(combatLabBaselineRunJson)) return;
            string encounterId = encounterProfile.StableEncounterId;
            int completed = combatLabMeasurements.CompletedCount(encounterId);
            if (completed >= CombatLabTargetBattlesPerEnemy) return;

            StopCombatLabPreview();
            JsonUtility.FromJsonOverwrite(combatLabBaselineRunJson, run);
            run.battlesWon = combatLabBaselineBattleWins + completed;
            BeginBattle(false);
            ApplyCombatLabBackground();
            combatLabMeasurements.Begin(
                encounterId,
                encounterProfile.ResolvedDisplayName,
                completed + 1,
                completed,
                run.hp,
                realtimeBattle.Time);
            SetPaused(false);
            combatLabStatus.text =
                $"実戦計測 {completed + 1}/{CombatLabTargetBattlesPerEnemy}：" +
                "通常どおりカード・Space・Shiftを操作してください";
            RefreshCombatLabMeasurementUi();
        }

        private void CancelCombatLabMeasurement()
        {
            combatLabMeasurements.Cancel();
            StopRealtimeBattle();
            JsonUtility.FromJsonOverwrite(combatLabBaselineRunJson, run);
            ResetCombatLabBattle();
            combatLabStatus.text = "実戦計測を中断しました（記録には追加していません）";
            RefreshCombatLabMeasurementUi();
        }

        private void ClearCombatLabMeasurements()
        {
            if (combatLabMeasurements.Active) return;
            combatLabMeasurements.Clear();
            combatLabStatus.text = "実戦計測の記録を消去しました";
            RefreshCombatLabMeasurementUi();
            SaveCombatLabMeasurements();
        }

        private void RefreshCombatLabMeasurementUi()
        {
            if (combatLabMeasure == null || combatLabClear == null ||
                combatLabSummary == null || encounterProfile == null)
                return;

            string encounterId = encounterProfile.StableEncounterId;
            JourneyCombatMeasurementSummary summary =
                combatLabMeasurements.Summarize(encounterId);
            bool measuring = combatLabMeasurements.Active;
            int targetTotal = combatLabEnemies.Count * CombatLabTargetBattlesPerEnemy;

            combatLabMeasure.text = measuring
                ? "計測を中断"
                : summary.BattleCount >= CombatLabTargetBattlesPerEnemy
                    ? "5戦計測済み"
                    : "実戦計測を開始";
            combatLabMeasure.EnableInClassList("measurement--active", measuring);
            combatLabMeasure.SetEnabled(
                measuring || summary.BattleCount < CombatLabTargetBattlesPerEnemy);
            combatLabClear.SetEnabled(!measuring && combatLabMeasurements.Records.Count > 0);
            combatLabBackground.SetEnabled(!measuring);
            combatLabEnemy.SetEnabled(!measuring);
            combatLabAttack.SetEnabled(!measuring);
            combatLabPlay.SetEnabled(!measuring && combatLabAttacks.Count > 0);

            if (summary.BattleCount == 0)
            {
                combatLabSummary.text =
                    $"この敵 0/{CombatLabTargetBattlesPerEnemy}戦 | " +
                    $"全体 {combatLabMeasurements.Records.Count}/{targetTotal}戦";
                return;
            }

            int failurePercent = (int)Math.Round(summary.ReactionFailureRate * 100d);
            combatLabSummary.text =
                $"この敵 {summary.BattleCount}/{CombatLabTargetBattlesPerEnemy}戦・勝{summary.Victories} " +
                $"| 平均HP減 {summary.AverageHpLoss:0.0} " +
                $"| 反応失敗 {summary.ReactionFailures}/{summary.ReactionOpportunities} ({failurePercent}%) " +
                $"| 平均 {summary.AverageDurationSeconds:0.0}秒 " +
                $"| 全体 {combatLabMeasurements.Records.Count}/{targetTotal}戦";
        }

        private void RecordCombatLabEnemyResolution(
            int playerDamage,
            bool sequenceEnded,
            bool reactionRequired,
            bool reactionSucceeded)
        {
            combatLabMeasurements.RecordEnemyResolution(
                playerDamage,
                sequenceEnded,
                reactionRequired,
                reactionSucceeded);
        }

        private bool TryCompleteCombatLabMeasurement(bool victory)
        {
            if (!combatLabMeasurements.Active) return false;

            JourneyCombatMeasurementRecord record = combatLabMeasurements.Complete(
                victory,
                run?.hp ?? 0,
                realtimeBattle.Time);
            StopRealtimeBattle();
            battleInputLocked = true;
            SetPaused(true);
            combatLabStatus.text =
                $"{record.DisplayName} {record.BattleNumber}/{CombatLabTargetBattlesPerEnemy} " +
                $"{(record.Victory ? "勝利" : "敗北")}：" +
                $"HP減{record.NetHpLoss} / 実被害{record.DamageTaken} / " +
                $"反応失敗{record.ReactionFailures}/{record.ReactionOpportunities} / " +
                $"{record.DurationSeconds:0.0}秒";
            RefreshBattleUi();
            RefreshCombatLabMeasurementUi();
            SaveCombatLabMeasurements();
            return true;
        }

        private void SaveCombatLabMeasurements()
        {
            var report = new CombatLabMeasurementReport
            {
                generatedAtUtc = DateTime.UtcNow.ToString("O"),
                records = combatLabMeasurements.Records.ToList()
            };
            string repositoryRoot = Path.GetFullPath(
                Path.Combine(Application.dataPath, "..", "..", ".."));
            string outputDirectory = Path.Combine(repositoryRoot, "Temp");
            Directory.CreateDirectory(outputDirectory);
            File.WriteAllText(
                Path.Combine(outputDirectory, "journey-combat-measurements.json"),
                JsonUtility.ToJson(report, true));
        }

        private void StopCombatLabPreview()
        {
            if (combatLabPreviewRoutine != null)
            {
                StopCoroutine(combatLabPreviewRoutine);
                combatLabPreviewRoutine = null;
            }
            enemyImpactHoldRemaining = 0f;
            if (defenseActive) FinishRealtimeTelegraph();
            SetEnemyTimingGlintVisible(false);
            ResetEnemyImpactContactCue();
            CancelEnemyPoseRecovery();
        }
    }
}
