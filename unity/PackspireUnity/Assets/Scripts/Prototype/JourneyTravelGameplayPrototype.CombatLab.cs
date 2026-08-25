
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire
{
    public sealed partial class JourneyTravelGameplayPrototype
    {
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
        private Coroutine combatLabPreviewRoutine;
        private bool combatLabActive;
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
                    .Where(profile => profile != null && profile.ResolvedTimeline != null)
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
        }

        private void SelectCombatLabBackground(int index)
        {
            if (!combatLabActive) return;
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
            if (!combatLabActive || combatLabEnemies.Count == 0) return;
            combatLabEnemyIndex = Mathf.Clamp(index, 0, combatLabEnemies.Count - 1);
            ApplyEncounterProfile(combatLabEnemies[combatLabEnemyIndex]);
            ResetCombatLabBattle();
            ApplyCombatLabBackground();
            RebuildCombatLabAttacks();
            RefreshCombatLabStatus();
        }

        private void ResetCombatLabBattle()
        {
            StopCombatLabPreview();
            BeginBattle();
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
