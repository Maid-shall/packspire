using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Packspire
{
    /// <summary>
    /// Seamless expedition runtime. The courier, route decisions, roadside tasks,
    /// events and combat remain in one scrolling world. Normal expeditions and menu-driven
    /// developer previews attach the live PackspireGame RunState. Direct scene launches
    /// retain an isolated simulation fallback for editor recovery.
    /// </summary>
    [RequireComponent(typeof(JourneyWalkCyclePrototype))]
    public sealed partial class JourneyTravelGameplayPrototype : MonoBehaviour
    {
        private enum Phase
        {
            Travel,
            Choice,
            MiniGame,
            Event,
            Battle,
            Result
        }

        private const string ViewResource = "UI/PackspireJourneyCompleteView";
        private const string StyleResource = "UI/PackspireJourneyComplete";
        private const string WalkSheetResource = "Art/UI/CourierRoutePrototype/courier-route-walk-sheet-v1";
        private const string DefaultEncounterProfileResource = "Data/Journey/WardenEncounter";
        private const float DefaultEnemyBattleScale = .66f;
        private const float BattleGroundY = -1.23f;
        private static readonly Vector3 EnemyBattleStartPosition = new Vector3(4.2f, BattleGroundY, 0f);
        private const float OpeningTravelDuration = 6.5f;
        private const float RouteTravelDuration = 7.5f;
        private const float MiniGameStart = 0.42f;

        private static readonly string[] StateClasses =
        {
            "state--travel", "state--choice", "state--minigame",
            "state--event", "state--battle", "state--result"
        };

        private readonly List<UnityEngine.Object> runtimeAssets = new List<UnityEngine.Object>();
        private readonly Button[] cardButtons = new Button[10];
        private readonly BattleCardView[] battleCardViews = new BattleCardView[10];
        private readonly List<(Button button, float depth)> battleHandOrder =
            new List<(Button button, float depth)>(10);
        private readonly JourneyMiniGameController miniGameController = new JourneyMiniGameController();
        private static readonly string[] MiniGamePresentationClasses =
        {
            "mini--stamp", "mini--balance", "mini--choice", "mini--dodge", "mini--sequence"
        };

        private JourneyWalkCyclePrototype walker;
        private JourneySceneryController scenery;
        private JourneyEnvironmentController environment;
        [SerializeField] private JourneyBattleEncounterProfile encounterProfile;
        private UIDocument document;
        private VisualElement screen;
        private VisualElement transition;
        private VisualElement ledgerOverlay;
        private VisualElement ledgerPhaseHost;
        private VisualElement sealHost;
        private Label toast;
        private ProgressBar hpBar;
        private ProgressBar routeProgress;
        private VisualElement battlePlayerHpFill;
        private VisualElement battlePlayerBlockBadge;
        private readonly VisualElement[] enemyHudSlots = new VisualElement[3];
        private readonly Label[] enemyNames = new Label[3];
        private readonly VisualElement[] enemyHpFills = new VisualElement[3];
        private readonly Label[] enemyHpTexts = new Label[3];
        private readonly VisualElement[] enemyBlockBadges = new VisualElement[3];
        private readonly Label[] enemyBlocks = new Label[3];
        private readonly VisualElement[] enemyStatusHosts = new VisualElement[3];
        private VisualElement miniGamePanel;
        private VisualElement miniGameNeedle;
        private VisualElement miniGameHazard;
        private Label hpText;
        private Label cargoText;
        private Label sealsText;
        private Label areaText;
        private Label weatherText;
        private Label dayText;
        private Label conditionText;
        private Label conditionNoteText;
        private Label phaseText;
        private Label nextText;
        private Label ledgerSummary;
        private Label ledgerNodeTitle;
        private Label ledgerNodeMeta;
        private Label ledgerNodeBody;
        private Label sealTiming;
        private Label sealDetailName;
        private Label sealDetailEffect;
        private Button sealApply;
        private Label choiceTitle;
        private Label choiceNote;
        private Button choiceA;
        private Button choiceB;
        private VisualElement choiceAArt;
        private VisualElement choiceBArt;
        private Label choiceAIndex;
        private Label choiceATitle;
        private Label choiceAFlavor;
        private Label choiceADays;
        private Label choiceARisk;
        private Label choiceAEncounter;
        private Label choiceACargo;
        private Label choiceASeal;
        private Label choiceBIndex;
        private Label choiceBTitle;
        private Label choiceBFlavor;
        private Label choiceBDays;
        private Label choiceBRisk;
        private Label choiceBEncounter;
        private Label choiceBCargo;
        private Label choiceBSeal;
        private Label eventEyebrow;
        private Label eventTitle;
        private Label eventText;
        private Button eventA;
        private Button eventB;
        private Label miniGameEyebrow;
        private Label miniGameTitle;
        private Label miniGameNote;
        private Label miniGameTimer;
        private Label miniGameReward;
        private Label miniGameRisk;
        private Label miniGameObjective;
        private Label miniGameStep;
        private Label speech;
        private Label damagePopup;
        private Label encounterBanner;
        private Button miniGameLeft;
        private Button miniGameAction;
        private Button miniGameRight;
        private Label battleLog;
        private Label energyText;
        private Label battlePlayerHp;
        private Label battleBlock;
        private VisualElement playerStatusHost;
        private Label drawPileText;
        private Label discardPileText;
        private Button drawPileButton;
        private Button discardPileButton;
        private VisualElement battleHandRoot;
        private JourneyConsumablePresenter consumablePresenter;
        private VisualElement pileOverlay;
        private Label pileTitle;
        private Label pileSummary;
        private Label pileEmpty;
        private VisualElement pileCardHost;
        private Button pileCloseButton;
        private Button pileScrim;
        private Label resultEyebrow;
        private Label resultTitle;
        private Label resultText;
        private Button resultContinue;
        private Button speedButton;
        private Button pauseButton;

        private RunState run;
        private bool usesLiveRun;
        private BattleState battle;
        private ExpeditionRouteNodePlan[] choices = Array.Empty<ExpeditionRouteNodePlan>();
        private ExpeditionRouteNodePlan arrivalExpeditionNode;
        private CourierRouteNodeDef arrivalNode;
        private Phase phase;
        private float travelClock;
        private float travelDuration;
        private float miniGameVisualClock;
        private int miniGameLastRevision = -1;
        private int miniGameLastTimerTenth = -1;
        private int miniGameLastNeedleBucket = -1;
        private float toastClock;
        private float speechClock;
        private float speechVisibleClock;
        private float speedScale = 1f;
        private float defenseClock;
        private float defenseDuration;
        private float defenseInputTime = -1f;
        private int phaseRevision;
        private int battleRevision;
        private bool paused;
        private bool transitionActive;
        private bool miniGameOffered;
        private bool miniGameResolved => miniGameController.IsResolved;
        private bool firstChoicePending = true;
        private bool choiceLocked;
        private bool ledgerOpen;
        private bool defenseActive;
        private bool defenseResolved;
        private bool defenseWindowOpen;
        private bool battleInputLocked;
        private bool pileOverlayOpen;
        private bool encounterIntroActive;
        private bool foundationUiHidden;
        private DefenseAction defenseAction;
        private DefenseInputGrade defenseInputGrade;
        private EnemyActionKind enemyActionKind;
        private bool enemyActionOverhead = true;
        private float enemyImpactHoldRemaining;
        private Color enemyTelegraphColor = Color.white;
        private string selectedSealKey = "";
        private int completedRoadTasks;
        private int biomeIndex;
        private SpriteRenderer enemyRenderer;
        private SpriteRenderer enemyShadowRenderer;
        private SpriteRenderer enemyTelegraphRenderer;
        private Sprite[] enemyBattleFrames = Array.Empty<Sprite>();
        private readonly SpriteRenderer[] battlePreviewEnemyRenderers = new SpriteRenderer[2];
        private readonly SpriteRenderer[] battlePreviewEnemyShadowRenderers = new SpriteRenderer[2];
        private readonly Vector3[] battleEnemySlotPositions = new Vector3[3];
        private readonly float[] battleEnemySlotScales = new float[3];
        private Vector3 enemyBattleBasePosition;
        private float enemyBattleBaseScale = DefaultEnemyBattleScale;
        private float enemyBattleCompositionScale = 1f;
        private float enemyBattleFormationScaleFactor = 1f;
        private int battleLayoutEnemyCount = 1;
        private float battleActorClock;
        private SpriteRenderer parcelRenderer;
        private Coroutine transitionRoutine;
        private Coroutine choiceCommitRoutine;
        private Coroutine encounterRoutine;
        private Coroutine battlePresentationRoutine;
        private bool uiBound;

        private enum DefenseAction
        {
            None,
            Jump,
            Brace
        }

        private enum DefenseInputGrade
        {
            None,
            Success,
            Early,
            Late,
            Wrong
        }

        private enum EnemyActionKind
        {
            Normal,
            JumpReaction,
            BraceReaction
        }

        private enum EnemyBattlePose
        {
            Idle = 0,
            HighAnticipation = 1,
            HighImpact = 2,
            LowAnticipation = 3,
            LowImpact = 4,
            Hit = 5
        }

        private void Awake()
        {
            if (encounterProfile == null)
                encounterProfile = PackspireResources.Load<JourneyBattleEncounterProfile>(DefaultEncounterProfileResource);
            if (encounterProfile == null)
                throw new InvalidOperationException(
                    $"Required journey encounter profile is missing: {DefaultEncounterProfileResource}");
            walker = GetComponent<JourneyWalkCyclePrototype>();
            walker.SetBuiltInForegroundVisible(false);
            scenery = GetComponent<JourneySceneryController>();
            if (scenery == null) scenery = gameObject.AddComponent<JourneySceneryController>();
            scenery.Initialize(walker);
            environment = GetComponent<JourneyEnvironmentController>();
            if (environment == null) environment = gameObject.AddComponent<JourneyEnvironmentController>();
            environment.Initialize(walker);
            AttachRunOrBuildSimulation();
            BuildWorldActors();
            BuildUiDocument();
        }

        private void OnEnable()
        {
            if (document != null)
            {
                document.enabled = true;
            }
            if (uiBound) HideFoundationUi();
        }

        private void OnDisable()
        {
            if (document != null)
            {
                document.enabled = false;
            }
            RestoreFoundationUi();
        }

        private IEnumerator Start()
        {
            int revisionAtStart = phaseRevision;
            for (int frame = 0; frame < 30; frame++)
            {
                if (document != null && document.rootVisualElement != null && document.rootVisualElement.panel != null)
                {
                    break;
                }
                yield return null;
            }

            BindUi();
            HideFoundationUi();
            if (phaseRevision == revisionAtStart)
            {
                if (JourneyDeveloperPreviewController.TryApply(this)) { }
                else if (usesLiveRun && PackspireGame.Instance != null &&
                         PackspireGame.Instance.UiConsumeSeamlessJourneyResumeAfterReward())
                {
                    ResumeLiveJourneyAfterReward();
                }
                else
                {
                    BeginTravel(OpeningTravelDuration, null);
                    ShowToast("第七圏の配達路へ進入。判断地点までは自動で進みます。");
                }
            }
        }

        private void AttachRunOrBuildSimulation()
        {
            PackspireGame game = PackspireGame.Instance;
            usesLiveRun = game != null && game.UiTryGetSeamlessJourneyRun(out run);
            if (!usesLiveRun) BuildSimulationRun();
        }

        private void ResumeLiveJourneyAfterReward()
        {
            RefreshPersistentUi();
            ExpeditionRoutePlan plan = ExpeditionProgressSystem.Ensure(run);
            ExpeditionRouteNodePlan currentNode = ExpeditionRoutePlanSystem.Node(plan, plan.currentNodeId);
            arrivalExpeditionNode = null;
            arrivalNode = null;
            firstChoicePending = false;
            CourierRouteState route = run.courierRoute;
            ApplyWorldForRouteImmediately(ExpeditionJourneySystem.PresentationNode(plan, currentNode));
            if (route.failed || plan.complete)
            {
                ShowResult(
                    route.failed ? "EXPEDITION FAILED" : "EXPEDITION COMPLETE",
                    route.failed ? "遠征続行不能" : "最深部踏破",
                    PackspireGame.Instance?.UiMessage ?? string.Empty,
                    false);
                return;
            }

            ShowChoice();
            ShowToast("戦利品を収め、旅程へ復帰しました。");
        }

        private void BuildSimulationRun()
        {
            run = new RunState
            {
                hp = 42,
                maxHp = 42,
                energy = 3,
                role = PackspireContent.Data.balance.defaultRoleId,
                dungeon = PackspireContent.Data.balance.defaultDungeonId,
                characterId = "mio",
                backpack = PackspireContent.Data.balance.defaultBackpackId
            };
            run.courierRoute = CourierRouteSystem.Create(run, null);
            run.consumables.AddRange(new[]
            {
                "heal", "assault_incense", "ward_seal", "delay_seal"
            });
        }

        private void BuildUiDocument()
        {
            GameObject host = new GameObject("Journey Complete UI");
            host.transform.SetParent(transform, false);
            document = host.AddComponent<UIDocument>();
            document.enabled = false;
            document.panelSettings = PackspireResources.Load<PanelSettings>("UI/PackspirePanelSettings");
            document.sortingOrder = 100f;
            document.visualTreeAsset = PackspireResources.Load<VisualTreeAsset>(ViewResource);
            document.enabled = true;
        }


        private void Update()
        {
            using var performanceScope = PackspirePerformance.JourneyUpdate.Auto();
            if (screen == null)
            {
                return;
            }

            HandleKeyboard();
            float gameplayDelta = paused || ledgerOpen || transitionActive || pileOverlayOpen ? 0f : Time.deltaTime;
            float travelDelta = gameplayDelta * speedScale;
            UpdateToast(gameplayDelta);
            UpdateSpeech(Time.deltaTime);

            if (phase == Phase.Travel)
            {
                UpdateTravel(travelDelta);
            }
            else if (phase == Phase.MiniGame)
            {
                // Fast-forward shortens passive travel only. It must not secretly
                // double the difficulty of an active timing task.
                UpdateMiniGame(gameplayDelta);
            }
            else if (phase == Phase.Battle)
            {
                battleActorClock += gameplayDelta;
                float realtimeTimelineDelta = UpdateRealtimeBattle(gameplayDelta);
                if (enemyImpactHoldRemaining > 0f)
                {
                    float impactDelta = realtimeBattleActive
                        ? realtimeTimelineDelta
                        : gameplayDelta;
                    UpdateRealtimeEnemyImpact(impactDelta);
                }
                else if (defenseActive)
                {
                    float defenseDelta = realtimeBattleActive
                        ? realtimeTimelineDelta
                        : gameplayDelta;
                    UpdateDefense(defenseDelta);
                }
                else UpdateBattleIdleMotion();
                UpdateEnemyPoseRecovery(gameplayDelta);
            }
        }

        private void HandleKeyboard()
        {
            if (Input.GetKeyDown(KeyCode.F10))
            {
                ReturnToDeveloperMenu();
                return;
            }
            if (pileOverlayOpen &&
                (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Tab)))
            {
                ClosePileOverlay();
                return;
            }
            if (!usesLiveRun && Input.GetKeyDown(KeyCode.R))
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                ToggleLedger();
                return;
            }
            if (ledgerOpen) return;
            if (phase == Phase.Choice && Input.GetKeyDown(KeyCode.Alpha1)) SelectChoice(0);
            if (phase == Phase.Choice && Input.GetKeyDown(KeyCode.Alpha2)) SelectChoice(1);
            if (phase == Phase.MiniGame && Input.GetKeyDown(KeyCode.Space)) MiniGameAction();
            if (phase == Phase.MiniGame && (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))) NudgeBalance(-1f);
            if (phase == Phase.MiniGame && (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))) NudgeBalance(1f);
            if (phase == Phase.Battle && defenseActive && Input.GetKeyDown(KeyCode.Space)) ResolveDefenseInput(DefenseAction.Jump);
            if (phase == Phase.Battle && defenseActive && (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift))) ResolveDefenseInput(DefenseAction.Brace);
            if (!usesLiveRun && Input.GetKeyDown(KeyCode.F5)) BeginBattle();
            if (!usesLiveRun && Input.GetKeyDown(KeyCode.F6))
            {
                int previewIndex = completedRoadTasks % 6;
                completedRoadTasks++;
                DevShowMiniGame(previewIndex);
            }
        }

        private void ReturnToDeveloperMenu()
        {
            if (usesLiveRun && PackspireGame.Instance != null)
            {
                PackspireGame.Instance.UiLeaveSeamlessJourneyForDeveloperMenu();
                return;
            }
            PackspireUiFoundation foundation = PackspireUiFoundation.Instance;
            if (foundation != null) foundation.SetJourneyPrototypeVisible(false);
            PackspireGame game = PackspireGame.Instance;
            if (game != null) game.UiOpenDeveloperPanel();
            SceneManager.LoadScene("Main");
        }

        private void BeginTravel(float duration, CourierRouteNodeDef destination)
        {
            arrivalNode = destination;
            travelDuration = Mathf.Max(2f, duration);
            travelClock = 0f;
            miniGameOffered = false;
            scenery.BeginTravel(biomeIndex, !scenery.HasSeenLandmark(biomeIndex));
            SetPhase(Phase.Travel);
            parcelRenderer.enabled = false;
            phaseText.text = destination == null ? $"MOVING / {BiomeLabel(biomeIndex)}" : $"MOVING / {destination.title}へ";
            nextText.text = "景色を見ながら進行中";
        }

        private void UpdateTravel(float delta)
        {
            if (delta <= 0f) return;
            travelClock += delta;
            float progress = Mathf.Clamp01(travelClock / travelDuration);
            routeProgress.value = progress * 100f;
            walker.SetJourneyProgress(progress);
            environment.SetJourneyProgress(progress);
            scenery.SetTravelProgress(progress);
            nextText.text = progress < .35f ? "次の判断まではしばらく" : progress < .74f ? "道中に何か見える" : "判断地点が近い";

            bool canOfferTask = !firstChoicePending && arrivalNode != null && arrivalNode.resolution != CourierResolutionKind.Battle;
            if (canOfferTask && !miniGameOffered && progress >= MiniGameStart)
            {
                miniGameOffered = true;
                BeginMiniGame();
                return;
            }

            if (progress < 1f) return;
            if (firstChoicePending)
            {
                firstChoicePending = false;
                ShowChoice();
            }
            else
            {
                ResolveArrival();
            }
        }

        private void ShowChoice()
        {
            ExpeditionRoutePlan plan = ExpeditionProgressSystem.Ensure(run);
            choices = ExpeditionJourneySystem.Available(run).ToArray();
            if (choices.Length == 0)
            {
                bool complete = plan.complete;
                ShowResult(
                    complete ? "EXPEDITION COMPLETE" : "ROUTE INTERRUPTED",
                    complete ? "最深部踏破" : "進行不能",
                    complete ? "三つの階層を踏破した。" : "次の遠征地点を取得できませんでした。",
                    false);
                return;
            }
            if (choices.Length == 1)
            {
                ContinueAlongSingleRoute(choices[0]);
                return;
            }

            SetPhase(Phase.Choice);
            walker.SetJourneyWalking(false);
            ResetChoiceVisuals();
            int choiceFloor = choices[0].floorIndex + 1;
            choiceTitle.text = string.IsNullOrEmpty(plan.currentNodeId) ? "最初の進路を選ぶ" : $"第{choiceFloor}層・次の分岐";
            choiceNote.text = "選択するのは次の区間です。後の分岐で探索側・突破側を選び直せます。";
            BindChoice(0, choices[0]);
            choiceB.EnableInClassList("is-hidden", choices.Length < 2);
            if (choices.Length > 1) BindChoice(1, choices[1]);
            phaseText.text = "ROUTE DECISION";
            nextText.text = "旅の内容と成果を見比べる";
        }

        private void ContinueAlongSingleRoute(ExpeditionRouteNodePlan node)
        {
            if (!ExpeditionJourneySystem.Select(run, node.id) ||
                !ExpeditionJourneySystem.Commit(run, out ExpeditionRouteNodePlan committed, out string message))
            {
                ShowResult("ROUTE INTERRUPTED", "旅程を継続できない", "航路データを確認してください。", false);
                return;
            }

            ContinueAfterRouteCommit(committed, message);
        }

        private void BindChoice(int index, ExpeditionRouteNodePlan expeditionNode)
        {
            ExpeditionRoutePlan plan = ExpeditionProgressSystem.Ensure(run);
            CourierRouteNodeDef node = ExpeditionJourneySystem.PresentationNode(plan, expeditionNode);
            bool first = index == 0;
            Button button = first ? choiceA : choiceB;
            Label routeIndex = first ? choiceAIndex : choiceBIndex;
            Label title = first ? choiceATitle : choiceBTitle;
            Label flavor = first ? choiceAFlavor : choiceBFlavor;
            Label days = first ? choiceADays : choiceBDays;
            Label risk = first ? choiceARisk : choiceBRisk;
            Label encounter = first ? choiceAEncounter : choiceBEncounter;
            Label cargo = first ? choiceACargo : choiceBCargo;
            Label seal = first ? choiceASeal : choiceBSeal;
            VisualElement art = first ? choiceAArt : choiceBArt;
            button.SetEnabled(node != null);
            title.text = expeditionNode == null ? "---" : ExpeditionJourneySystem.PathTitle(plan, expeditionNode);
            if (node == null) return;
            routeIndex.text = $"BRANCH {(first ? "A" : "B")} / {node.kind} / {JourneyPresentationConfig.GetRoadWidthLabel(node)}";
            flavor.text = RouteFlavor(node);
            int effectiveDays = Mathf.Max(0, node.dayCost - run.courierRoute.nextSegmentDiscount);
            days.text = effectiveDays == node.dayCost
                ? $"{node.dayCost}日"
                : $"{effectiveDays}日（印適用）";
            risk.text = RiskLabel(node.risk);
            encounter.text = ResolutionLabel(node.resolution);
            cargo.text = RouteRewardLabel(node.resolution);
            seal.text = RouteSealLabel(node);
            ApplyRouteArt(art, node);
        }

        private void SelectChoice(int index)
        {
            if (choiceLocked || phase != Phase.Choice || index < 0 || index >= choices.Length) return;
            choiceLocked = true;
            choiceCommitRoutine = StartCoroutine(CommitChoiceRoutine(index));
        }

        private IEnumerator CommitChoiceRoutine(int index)
        {
            ExpeditionRouteNodePlan node = choices[index];
            Button selected = index == 0 ? choiceA : choiceB;
            Button rejected = index == 0 ? choiceB : choiceA;
            selected.AddToClassList("is-stamped");
            if (choices.Length > 1) rejected.AddToClassList("is-rejected");
            choiceA.SetEnabled(false);
            choiceB.SetEnabled(false);
            yield return WaitForJourneySeconds(.44f);

            if (!ExpeditionJourneySystem.Select(run, node.id) ||
                !ExpeditionJourneySystem.Commit(run, out ExpeditionRouteNodePlan committed, out string message))
            {
                ResetChoiceVisuals();
                ShowToast("この経路は現在選べません。");
                choiceCommitRoutine = null;
                yield break;
            }

            ContinueAfterRouteCommit(committed, message);
            choiceCommitRoutine = null;
        }

        private void ContinueAfterRouteCommit(ExpeditionRouteNodePlan expeditionNode, string message)
        {
            RefreshPersistentUi();
            if (run.courierRoute.failed)
            {
                ShowResult("EXPEDITION FAILED", "配達期限を超過", message, false);
                return;
            }

            ShowToast(message);
            ExpeditionRoutePlan plan = ExpeditionProgressSystem.Ensure(run);
            CourierRouteNodeDef node = ExpeditionJourneySystem.PresentationNode(plan, expeditionNode);
            arrivalExpeditionNode = expeditionNode;
            SetWorldForRoute(node);
            BeginTravel(RouteTravelDuration + node.dayCost * .65f, node);
        }

        private void ResetChoiceVisuals()
        {
            choiceLocked = false;
            choiceA.RemoveFromClassList("is-stamped");
            choiceA.RemoveFromClassList("is-rejected");
            choiceB.RemoveFromClassList("is-stamped");
            choiceB.RemoveFromClassList("is-rejected");
            choiceA.SetEnabled(true);
            choiceB.SetEnabled(true);
        }

        private void ResolveArrival()
        {
            if (arrivalNode == null)
            {
                ShowChoice();
                return;
            }

            areaText.text = arrivalNode.title;
            weatherText.text = BiomeWeather(arrivalNode.phase);
            SetWorldForRoute(arrivalNode);

            switch (arrivalNode.resolution)
            {
                case CourierResolutionKind.Battle:
                    PrepareEncounterForArrival();
                    BeginBattle();
                    break;
                case CourierResolutionKind.Event:
                case CourierResolutionKind.Cargo:
                    ShowEvent(arrivalNode);
                    break;
                case CourierResolutionKind.Delivery:
                    ResolveRoute(new CourierLocationOutcome { success = true, message = "受取印を確認。" });
                    break;
                default:
                    ResolveRoute(new CourierLocationOutcome { success = true, message = arrivalNode.resolutionTitle });
                    break;
            }
        }

        private void PrepareEncounterForArrival()
        {
            if (arrivalExpeditionNode == null) return;
            ExpeditionRoutePlan plan = ExpeditionProgressSystem.Ensure(run);
            JourneyBattleEncounterProfile selected = JourneyEncounterSelectionSystem.SelectAndAssign(
                plan,
                arrivalExpeditionNode,
                run.dungeon,
                plan.elapsedDays);
            ApplyEncounterProfile(selected);
        }

        private void ShowEvent(CourierRouteNodeDef node)
        {
            SetPhase(Phase.Event);
            walker.SetJourneyWalking(false);
            eventEyebrow.text = node.resolution == CourierResolutionKind.Cargo ? "RECOVERY NOTICE" : "ROADSIDE EVENT";
            eventTitle.text = node.resolutionTitle;
            eventText.text = node.resolutionText + "\n\n" + (string.IsNullOrWhiteSpace(node.condition) ? "" : node.condition);
            if (node.resolution == CourierResolutionKind.Cargo)
            {
                eventA.text = "荷札を照合して回収する";
                eventB.text = "期限を優先して進む";
            }
            else
            {
                eventA.text = "慎重に手続きを進める";
                eventB.text = "急いで突破する";
            }
        }

        private void ResolveEvent(bool primary)
        {
            if (phase != Phase.Event || arrivalNode == null) return;
            bool cargo = arrivalNode.resolution == CourierResolutionKind.Cargo;
            CourierLocationOutcome outcome = new CourierLocationOutcome
            {
                success = true,
                cargoRecovered = cargo && primary,
                performance = primary ? 2 : 1,
                dayDelta = !cargo && !primary ? 1 : 0,
                message = primary ? "照合に成功した。" : "期限を優先した。"
            };
            ResolveRoute(outcome);
        }

        private void ResolveRoute(CourierLocationOutcome outcome)
        {
            ExpeditionRoutePlan plan = ExpeditionProgressSystem.Ensure(run);
            bool generatedNodePending = arrivalExpeditionNode != null && plan.awaitingResolution;
            string message;
            bool resolved = generatedNodePending
                ? ExpeditionJourneySystem.ResolveCurrent(run, outcome, out message)
                : CourierRouteSystem.ResolveCurrent(run, outcome, out message);
            if (!resolved)
            {
                ShowToast(message);
                return;
            }
            RefreshPersistentUi();
            bool complete = generatedNodePending ? plan.complete : run.courierRoute.complete;
            bool failed = run.courierRoute.failed;
            ShowResult(
                failed ? "EXPEDITION FAILED" : complete ? "EXPEDITION COMPLETE" : "ROUTE CLEARED",
                failed ? "遠征続行不能" : complete ? "最深部踏破" : arrivalNode?.resolutionTitle ?? "地点を突破",
                message,
                !complete && !failed);
        }

        private void ShowResult(string eyebrow, string title, string body, bool canContinue)
        {
            SetPhase(Phase.Result);
            walker.SetJourneyWalking(false);
            SetMainEnemyVisible(false);
            resultEyebrow.text = eyebrow;
            resultTitle.text = title;
            resultText.text = body;
            resultContinue.text = canContinue
                ? "旅程を再開"
                : usesLiveRun ? "遠征結果へ" : "DEV SIMULATIONを再開 [R]";
            resultContinue.userData = canContinue;
        }

        private void ContinueAfterResult()
        {
            bool canContinue = resultContinue.userData is bool value && value;
            if (!canContinue)
            {
                if (usesLiveRun && PackspireGame.Instance != null)
                {
                    bool win = run?.expeditionPlan?.complete == true && run.courierRoute.failed == false;
                    PackspireGame.Instance.UiFinishSeamlessJourney(win);
                    return;
                }
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                return;
            }
            arrivalExpeditionNode = null;
            arrivalNode = null;
            ShowChoice();
        }

        private void RefreshPersistentUi()
        {
            if (screen == null || run == null) return;
            CourierRouteState route = run.courierRoute;
            hpBar.highValue = Mathf.Max(1, run.maxHp);
            hpBar.value = Mathf.Clamp(run.hp, 0, run.maxHp);
            hpText.text = $"{run.hp} / {run.maxHp}";
            cargoText.text = route.recoveredCargoCount.ToString("00");
            sealsText.text = Mathf.Max(0, route.seals?.Sum(seal => seal.charges) ?? 0).ToString("00");
            ExpeditionDayStage dayStage = ExpeditionProgressSystem.CurrentDayStage(run);
            dayText.text = $"DAY {route.daysElapsed} / {JourneyDayStageLabel(dayStage)}";
            conditionText.text = JourneyDayStageLabel(dayStage);
            conditionNoteText.text = JourneyDayStageNote(run, dayStage);
        }

        private static string JourneyDayStageLabel(ExpeditionDayStage stage) => stage switch
        {
            ExpeditionDayStage.Alert => "警戒",
            ExpeditionDayStage.Pursuit => "追跡",
            ExpeditionDayStage.Anomaly => "異常",
            _ => "静穏"
        };

        private static string JourneyDayStageNote(RunState activeRun, ExpeditionDayStage stage)
        {
            int elapsed = activeRun?.expeditionPlan?.elapsedDays ?? 0;
            return stage switch
            {
                ExpeditionDayStage.Quiet => $"警戒まで {Mathf.Max(0, ExpeditionRoutePlanSystem.AlertStartDay - elapsed)}日",
                ExpeditionDayStage.Alert => $"追跡まで {Mathf.Max(0, ExpeditionRoutePlanSystem.PursuitStartDay - elapsed)}日",
                ExpeditionDayStage.Pursuit when activeRun?.expeditionPlan?.fourthDayStageEnabled == true =>
                    $"異常兆候まで {Mathf.Max(0, ExpeditionRoutePlanSystem.AnomalyStartDay - elapsed)}日",
                ExpeditionDayStage.Anomaly => "特殊危険域",
                _ => "高危険・高報酬"
            };
        }

        private void SetPhase(Phase next)
        {
            phase = next;
            phaseRevision++;
            scenery.SetActivity(
                next == Phase.Travel || next == Phase.MiniGame,
                next == Phase.Travel);
            foreach (string className in StateClasses) screen.RemoveFromClassList(className);
            screen.AddToClassList(next switch
            {
                Phase.Choice => "state--choice",
                Phase.MiniGame => "state--minigame",
                Phase.Event => "state--event",
                Phase.Battle => "state--battle",
                Phase.Result => "state--result",
                _ => "state--travel"
            });
            walker.SetBattleStage(next == Phase.Battle);
            environment.SetBattleContext(next == Phase.Battle);
            if (next != Phase.Battle)
            {
                StopRealtimeBattle();
                ClosePileOverlay();
                screen.RemoveFromClassList("battle--telegraph");
                screen.RemoveFromClassList("battle--reaction-jump");
                screen.RemoveFromClassList("battle--reaction-brace");
                screen.RemoveFromClassList("battle--reaction-ready");
                screen.RemoveFromClassList("battle--reaction-committed");
                screen.RemoveFromClassList("battle--enemy-count-2");
                screen.RemoveFromClassList("battle--enemy-count-3");
                screen.RemoveFromClassList("battle--layout-preview");
                SetEnemyTelegraphVisible(false);
                SetMainEnemyVisible(false);
                for (int previewIndex = 0; previewIndex < battlePreviewEnemyRenderers.Length; previewIndex++)
                {
                    if (battlePreviewEnemyRenderers[previewIndex] != null)
                        battlePreviewEnemyRenderers[previewIndex].enabled = false;
                    if (battlePreviewEnemyShadowRenderers[previewIndex] != null)
                        battlePreviewEnemyShadowRenderers[previewIndex].enabled = false;
                }
            }
            ApplyWorldMotion();
            if (next != Phase.Travel && speech != null)
            {
                speechVisibleClock = 0f;
                speech.RemoveFromClassList("speech--visible");
            }
        }

        private float WorldMotionScale()
        {
            if (paused || ledgerOpen || transitionActive) return 0f;
            return phase switch
            {
                Phase.Travel => speedScale,
                Phase.MiniGame when !miniGameResolved => .5f,
                _ => 0f
            };
        }

        private void ApplyWorldMotion()
        {
            float motion = WorldMotionScale();
            walker.SetJourneySpeedScale(motion);
            walker.SetJourneyWalking(motion > .001f);
            environment.SetWorldMotion(motion);
        }

        private void ToggleSpeed()
        {
            speedScale = speedScale < 1.5f ? 2f : 1f;
            speedButton.text = speedScale > 1f ? "×2" : "×1";
            RefreshRealtimeTransport();
            ApplyWorldMotion();
        }

        private void TogglePause()
        {
            SetPaused(!paused);
        }

        private void SetPaused(bool value)
        {
            paused = value;
            pauseButton.text = paused ? "再開" : "一時停止";
            RefreshRealtimeTransport();
            ApplyWorldMotion();
        }

        private void UpdateSpeech(float delta)
        {
            if (speech == null || phase != Phase.Travel || paused)
            {
                return;
            }

            if (speechVisibleClock > 0f)
            {
                speechVisibleClock -= delta;
                if (speechVisibleClock <= 0f) speech.RemoveFromClassList("speech--visible");
                return;
            }

            speechClock -= delta;
            if (speechClock > 0f) return;
            string[] lines = biomeIndex switch
            {
                1 => new[] { "紙の匂いが濃くなってきた。", "水音……荷札を濡らさないように。", "書庫区画は足場が悪いな。" },
                2 => new[] { "黒鐘が近い。", "風が強い……荷を締め直そう。", "ここまで来れば、あと少し。" },
                _ => new[] { "この荷物、思ったより重い。", "次の標識を見落とさないように。", "煤霧が薄いうちに進もう。" }
            };
            speech.text = lines[UnityEngine.Random.Range(0, lines.Length)];
            speech.AddToClassList("speech--visible");
            speechVisibleClock = 2.25f;
            speechClock = UnityEngine.Random.Range(5.5f, 8.5f);
        }

        private void SetWorldForRoute(CourierRouteNodeDef node)
        {
            int desiredBiome = BiomeForPhase(node?.phase ?? 0);
            JourneyWalkCyclePrototype.RoadProfile desiredRoad = JourneyPresentationConfig.GetRoadProfile(node);
            bool biomeChanged = desiredBiome != biomeIndex;
            bool roadChanged = walker != null && desiredRoad != walker.CurrentRoadProfile;
            if (!biomeChanged && !roadChanged) return;

            biomeIndex = desiredBiome;
            PlayTransition(desiredBiome, desiredRoad, node?.title);
        }

        private void ApplyWorldForRouteImmediately(CourierRouteNodeDef node)
        {
            int desiredBiome = BiomeForPhase(node?.phase ?? 0);
            JourneyWalkCyclePrototype.RoadProfile desiredRoad = JourneyPresentationConfig.GetRoadProfile(node);
            biomeIndex = desiredBiome;
            scenery?.ClearAll();
            walker.SetJourneyBiome(desiredBiome);
            environment.SetBiome(desiredBiome);
            walker.SetRoadProfile(desiredRoad);
        }

        private static int BiomeForPhase(int routePhase)
        {
            return routePhase >= 8 ? 2 : routePhase >= 5 ? 1 : 0;
        }

        private static string BiomeLabel(int biome) => biome switch
        {
            1 => "水没書庫",
            2 => "黒鐘区画",
            _ => "灰市外縁"
        };

        private static string BiomeWeatherForIndex(int biome) => biome switch
        {
            1 => "水霧 / 無風",
            2 => "黒鐘雨 / 強風",
            _ => "煤霧 / 微風"
        };

        private void ShowToast(string message)
        {
            toast.text = message;
            toastClock = 3.2f;
            toast.AddToClassList("toast--visible");
        }

        private void UpdateToast(float delta)
        {
            if (toastClock <= 0f) return;
            toastClock -= delta;
            if (toastClock <= 0f) toast.RemoveFromClassList("toast--visible");
        }

        private void PlayTransition(
            int targetBiome,
            JourneyWalkCyclePrototype.RoadProfile targetRoad,
            string routeTitle = null)
        {
            if (transitionRoutine != null) StopCoroutine(transitionRoutine);
            if (transition == null)
            {
                walker.SetJourneyBiome(targetBiome);
                environment.SetBiome(targetBiome);
                walker.SetRoadProfile(targetRoad);
                transitionActive = false;
                transitionRoutine = null;
                return;
            }
            transitionRoutine = StartCoroutine(TransitionRoutine(targetBiome, targetRoad, routeTitle));
        }

        private IEnumerator TransitionRoutine(
            int targetBiome,
            JourneyWalkCyclePrototype.RoadProfile targetRoad,
            string routeTitle)
        {
            transitionActive = true;
            ApplyWorldMotion();
            toastClock = 0f;
            toast?.RemoveFromClassList("toast--visible");
            speechVisibleClock = 0f;
            speech?.RemoveFromClassList("speech--visible");
            transition.AddToClassList("transition--visible");
            // Swap the complete world stack only after the fog has covered it. This
            // prevents the sky, architecture and road from flashing independently.
            yield return WaitForJourneySeconds(.28f);
            scenery?.ClearAll();
            walker.SetJourneyBiome(targetBiome);
            environment.SetBiome(targetBiome);
            walker.SetRoadProfile(targetRoad);
            yield return WaitForJourneySeconds(.12f);
            transition.RemoveFromClassList("transition--visible");
            yield return WaitForJourneySeconds(.36f);
            transitionActive = false;
            ApplyWorldMotion();
            string destination = string.IsNullOrWhiteSpace(routeTitle) ? "次の区画" : routeTitle;
            ShowToast($"霧を抜け、{destination}の{RoadProfileLabel(targetRoad)}へ景色が切り替わった。");
            transitionRoutine = null;
        }

        private static string RoadProfileLabel(JourneyWalkCyclePrototype.RoadProfile profile)
        {
            return profile switch
            {
                JourneyWalkCyclePrototype.RoadProfile.Wide => "広路",
                JourneyWalkCyclePrototype.RoadProfile.Narrow => "狭路",
                _ => "通常路"
            };
        }

        private IEnumerator EncounterRoutine(int revision)
        {
            encounterIntroActive = true;
            encounterBanner.AddToClassList("encounter--visible");
            yield return WaitForBattleSeconds(.72f, revision);
            if (revision != battleRevision || phase != Phase.Battle) yield break;
            encounterBanner.RemoveFromClassList("encounter--visible");
            encounterIntroActive = false;
            if (phase == Phase.Battle && !screen.ClassListContains("battle--layout-preview"))
            {
                battleInputLocked = false;
                RefreshBattleUi();
            }
            encounterRoutine = null;
        }

        private void PlayBattleImpact(int amount, bool playerHit)
        {
            if (amount <= 0) return;
            StartCoroutine(BattleImpactRoutine(amount, playerHit, battleRevision));
        }

        private IEnumerator BattleImpactRoutine(int amount, bool playerHit, int revision)
        {
            if (playerHit)
                walker.SetBattleMotion(JourneyWalkCyclePrototype.BattleMotion.Hit, .38f);
            damagePopup.text = $"-{amount}";
            damagePopup.EnableInClassList("fx--player", playerHit);
            damagePopup.AddToClassList("fx--visible");
            screen.AddToClassList("fx--impact");
            if (!playerHit && enemyRenderer != null)
            {
                SetEnemyBattlePose(EnemyBattlePose.Hit);
                enemyRenderer.color = new Color(1f, .52f, .42f, 1f);
            }
            yield return WaitForBattleSeconds(.09f, revision);
            if (revision != battleRevision || phase != Phase.Battle) yield break;
            screen.RemoveFromClassList("fx--impact");
            if (enemyRenderer != null)
            {
                enemyRenderer.color = Color.white;
                if (!playerHit) SetEnemyBattlePose(EnemyBattlePose.Idle);
            }
            yield return WaitForBattleSeconds(.24f, revision);
            if (revision != battleRevision || phase != Phase.Battle) yield break;
            damagePopup.RemoveFromClassList("fx--visible");
        }

        private IEnumerator PlayerAttackPresentationRoutine(int amount, bool enemyDefeated, int revision)
        {
            battleInputLocked = true;
            RefreshBattleUi();
            walker.SetBattleMotion(JourneyWalkCyclePrototype.BattleMotion.Attack, .42f);
            yield return WaitForBattleSeconds(.13f, revision);
            if (revision != battleRevision || phase != Phase.Battle) yield break;
            PlayBattleImpact(amount, false);
            yield return WaitForBattleSeconds(enemyDefeated ? .38f : .22f, revision);
            if (revision != battleRevision || phase != Phase.Battle) yield break;
            battleInputLocked = false;
            if (enemyDefeated)
            {
                SetMainEnemyVisible(false);
                CompleteJourneyBattleVictory();
            }
            else
            {
                RefreshBattleUi();
            }
            battlePresentationRoutine = null;
        }

        private IEnumerator WaitForBattleSeconds(float duration, int revision)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (revision != battleRevision || phase != Phase.Battle) yield break;
                if (!paused && !ledgerOpen && !transitionActive)
                    elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        private IEnumerator WaitForJourneySeconds(float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (!paused && !ledgerOpen && !pileOverlayOpen)
                    elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        private void OnDestroy()
        {
            RestoreFoundationUi();
            if (scenery != null) scenery.ClearAll();
            foreach (UnityEngine.Object asset in runtimeAssets)
                if (asset != null) Destroy(asset);
            runtimeAssets.Clear();
        }

        private void HideFoundationUi()
        {
            if (foundationUiHidden) return;
            PackspireUiFoundation foundation = PackspireUiFoundation.Instance;
            if (foundation == null) return;
            foundation.SetJourneyPrototypeVisible(true);
            foundationUiHidden = true;
        }

        private void RestoreFoundationUi()
        {
            if (!foundationUiHidden) return;
            PackspireUiFoundation foundation = PackspireUiFoundation.Instance;
            if (foundation != null) foundation.SetJourneyPrototypeVisible(false);
            foundationUiHidden = false;
        }

        private static string RiskLabel(int risk) => risk <= 0 ? "安全" : risk == 1 ? "注意" : risk == 2 ? "危険" : "高危険";
        private static string ResolutionLabel(string resolution) => resolution switch
        {
            CourierResolutionKind.Battle => "戦闘",
            CourierResolutionKind.Event => "イベント",
            CourierResolutionKind.Cargo => "回収",
            CourierResolutionKind.Relay => "中継補給",
            CourierResolutionKind.Delivery => "最終配達",
            _ => "通過"
        };
        private static string BiomeWeather(int routePhase) => routePhase >= 8 ? "黒鐘雨 / 強風" : routePhase >= 5 ? "水霧 / 無風" : "煤霧 / 微風";
    }
}
