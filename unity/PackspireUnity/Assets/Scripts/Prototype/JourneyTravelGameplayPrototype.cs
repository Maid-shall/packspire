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
    /// Playable expedition presentation vertical slice. The courier, route decisions,
    /// roadside tasks, events and combat remain in one scrolling world. This scene uses
    /// a private simulation RunState so developer-menu testing never mutates a real run.
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
        private const string EnemyResource = "Art/JourneyPrototype/Complete/journey-chibi-postal-warden-v1";
        private const string EnemyBattleResource = "Art/JourneyPrototype/Complete/journey-postal-warden-battle-sheet-v1";
        private const float EnemyBattleScale = .66f;
        private const float BattleGroundY = -1.23f;
        private static readonly Vector3 EnemyBattleStartPosition = new Vector3(2.25f, BattleGroundY, 0f);
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
        private readonly JourneyMiniGameController miniGameController = new JourneyMiniGameController();
        private static readonly string[] MiniGamePresentationClasses =
        {
            "mini--stamp", "mini--balance", "mini--choice", "mini--dodge", "mini--sequence"
        };

        private JourneyWalkCyclePrototype walker;
        private JourneySceneryController scenery;
        private JourneyEnvironmentController environment;
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
        private BattleState battle;
        private CourierRouteNodeDef[] choices = Array.Empty<CourierRouteNodeDef>();
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
        private Vector3 enemyBattleBasePosition;
        private float battleActorClock;
        private SpriteRenderer parcelRenderer;
        private Coroutine transitionRoutine;
        private Coroutine choiceCommitRoutine;
        private Coroutine defenseRoutine;
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
            walker = GetComponent<JourneyWalkCyclePrototype>();
            walker.SetBuiltInForegroundVisible(false);
            scenery = GetComponent<JourneySceneryController>();
            if (scenery == null) scenery = gameObject.AddComponent<JourneySceneryController>();
            scenery.Initialize(walker);
            environment = GetComponent<JourneyEnvironmentController>();
            if (environment == null) environment = gameObject.AddComponent<JourneyEnvironmentController>();
            environment.Initialize(walker);
            BuildSimulationRun();
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
                else
                {
                    BeginTravel(OpeningTravelDuration, null);
                    ShowToast("第七圏の配達路へ進入。判断地点までは自動で進みます。");
                }
            }
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

        private void BindUi()
        {
            if (uiBound)
            {
                return;
            }
            if (document == null || document.rootVisualElement == null || document.rootVisualElement.panel == null)
            {
                return;
            }

            VisualElement root = document.rootVisualElement;
            JourneyViewContract.Validate(root);
            screen = root.Q<VisualElement>("journey-screen");
            if (screen == null)
            {
                return;
            }
            StyleSheet style = PackspireResources.Load<StyleSheet>(StyleResource);
            if (style != null)
            {
                screen.styleSheets.Add(style);
            }
            VisualElement battleHandTheme = root.Q<VisualElement>("journey-hand-scroll");
            StyleSheet battleStyle = PackspireResources.Load<StyleSheet>("UI/PackspireBattle");
            if (battleHandTheme != null && battleStyle != null)
            {
                battleHandTheme.styleSheets.Add(battleStyle);
            }

            transition = root.Q<VisualElement>("journey-transition");
            ledgerOverlay = root.Q<VisualElement>("journey-ledger");
            ledgerPhaseHost = root.Q<VisualElement>("journey-ledger-phases");
            sealHost = root.Q<VisualElement>("journey-seal-host");
            toast = root.Q<Label>("journey-toast");
            hpBar = root.Q<ProgressBar>("journey-hp");
            routeProgress = root.Q<ProgressBar>("journey-progress");
            battlePlayerHpFill = root.Q<VisualElement>("journey-battle-player-hp-fill");
            battlePlayerBlockBadge = root.Q<VisualElement>("journey-battle-player-block-badge");
            for (int enemyIndex = 0; enemyIndex < enemyHudSlots.Length; enemyIndex++)
            {
                enemyHudSlots[enemyIndex] = root.Q<VisualElement>($"journey-enemy-slot-{enemyIndex}");
                enemyNames[enemyIndex] = root.Q<Label>($"journey-enemy-name-{enemyIndex}");
                enemyHpFills[enemyIndex] = root.Q<VisualElement>($"journey-enemy-hp-fill-{enemyIndex}");
                enemyHpTexts[enemyIndex] = root.Q<Label>($"journey-enemy-hp-text-{enemyIndex}");
                enemyBlockBadges[enemyIndex] = root.Q<VisualElement>($"journey-enemy-block-badge-{enemyIndex}");
                enemyBlocks[enemyIndex] = root.Q<Label>($"journey-enemy-block-{enemyIndex}");
                enemyStatusHosts[enemyIndex] = root.Q<VisualElement>($"journey-enemy-statuses-{enemyIndex}");
            }
            miniGamePanel = root.Q<VisualElement>("journey-minigame");
            miniGameNeedle = root.Q<VisualElement>("journey-minigame-needle");
            miniGameHazard = root.Q<VisualElement>("journey-minigame-hazard");
            hpText = root.Q<Label>("journey-hp-text");
            cargoText = root.Q<Label>("journey-cargo");
            sealsText = root.Q<Label>("journey-seals");
            areaText = root.Q<Label>("journey-area");
            weatherText = root.Q<Label>("journey-weather");
            dayText = root.Q<Label>("journey-day");
            conditionText = root.Q<Label>("journey-condition");
            conditionNoteText = root.Q<Label>("journey-condition-note");
            phaseText = root.Q<Label>("journey-phase");
            nextText = root.Q<Label>("journey-next");
            ledgerSummary = root.Q<Label>("journey-ledger-summary");
            ledgerNodeTitle = root.Q<Label>("journey-ledger-node-title");
            ledgerNodeMeta = root.Q<Label>("journey-ledger-node-meta");
            ledgerNodeBody = root.Q<Label>("journey-ledger-node-body");
            sealTiming = root.Q<Label>("journey-seal-timing");
            sealDetailName = root.Q<Label>("journey-seal-detail-name");
            sealDetailEffect = root.Q<Label>("journey-seal-detail-effect");
            sealApply = root.Q<Button>("journey-seal-apply");
            choiceTitle = root.Q<Label>("journey-choice-title");
            choiceNote = root.Q<Label>("journey-choice-note");
            choiceA = root.Q<Button>("journey-choice-a");
            choiceB = root.Q<Button>("journey-choice-b");
            choiceAArt = root.Q<VisualElement>("journey-choice-a-art");
            choiceBArt = root.Q<VisualElement>("journey-choice-b-art");
            choiceAIndex = root.Q<Label>("journey-choice-a-index");
            choiceATitle = root.Q<Label>("journey-choice-a-title");
            choiceAFlavor = root.Q<Label>("journey-choice-a-flavor");
            choiceADays = root.Q<Label>("journey-choice-a-days");
            choiceARisk = root.Q<Label>("journey-choice-a-risk");
            choiceAEncounter = root.Q<Label>("journey-choice-a-encounter");
            choiceACargo = root.Q<Label>("journey-choice-a-cargo");
            choiceASeal = root.Q<Label>("journey-choice-a-seal");
            choiceBIndex = root.Q<Label>("journey-choice-b-index");
            choiceBTitle = root.Q<Label>("journey-choice-b-title");
            choiceBFlavor = root.Q<Label>("journey-choice-b-flavor");
            choiceBDays = root.Q<Label>("journey-choice-b-days");
            choiceBRisk = root.Q<Label>("journey-choice-b-risk");
            choiceBEncounter = root.Q<Label>("journey-choice-b-encounter");
            choiceBCargo = root.Q<Label>("journey-choice-b-cargo");
            choiceBSeal = root.Q<Label>("journey-choice-b-seal");
            eventEyebrow = root.Q<Label>("journey-event-eyebrow");
            eventTitle = root.Q<Label>("journey-event-title");
            eventText = root.Q<Label>("journey-event-text");
            eventA = root.Q<Button>("journey-event-a");
            eventB = root.Q<Button>("journey-event-b");
            miniGameEyebrow = root.Q<Label>("journey-minigame-eyebrow");
            miniGameTitle = root.Q<Label>("journey-minigame-title");
            miniGameNote = root.Q<Label>("journey-minigame-note");
            miniGameTimer = root.Q<Label>("journey-minigame-timer");
            miniGameReward = root.Q<Label>("journey-minigame-reward");
            miniGameRisk = root.Q<Label>("journey-minigame-risk");
            miniGameObjective = root.Q<Label>("journey-minigame-objective");
            miniGameStep = root.Q<Label>("journey-minigame-step");
            speech = root.Q<Label>("journey-speech");
            damagePopup = root.Q<Label>("journey-damage-popup");
            encounterBanner = root.Q<Label>("journey-encounter-banner");
            miniGameLeft = root.Q<Button>("journey-minigame-left");
            miniGameAction = root.Q<Button>("journey-minigame-action");
            miniGameRight = root.Q<Button>("journey-minigame-right");
            battleLog = root.Q<Label>("journey-battle-log");
            energyText = root.Q<Label>("journey-energy");
            battlePlayerHp = root.Q<Label>("journey-battle-player-hp");
            battleBlock = root.Q<Label>("journey-battle-player-block");
            playerStatusHost = root.Q<VisualElement>("journey-player-statuses");
            drawPileText = root.Q<Label>("journey-draw-pile");
            discardPileText = root.Q<Label>("journey-discard-pile");
            drawPileButton = root.Q<Button>("journey-draw-pile-button");
            discardPileButton = root.Q<Button>("journey-discard-pile-button");
            battleHandRoot = battleHandTheme?.Q<VisualElement>(className: "ps-journey__hand");
            BindRealtimeBattleUi(root);
            pileOverlay = root.Q<VisualElement>("journey-pile-overlay");
            pileTitle = root.Q<Label>("journey-pile-title");
            pileSummary = root.Q<Label>("journey-pile-summary");
            pileEmpty = root.Q<Label>("journey-pile-empty");
            pileCardHost = root.Q<VisualElement>("journey-pile-card-host");
            pileCloseButton = root.Q<Button>("journey-pile-close");
            pileScrim = root.Q<Button>("journey-pile-scrim");
            VisualElement pileCardTheme = root.Q<VisualElement>("journey-pile-card-scroll");
            if (pileCardTheme != null && battleStyle != null)
            {
                pileCardTheme.styleSheets.Add(battleStyle);
            }
            resultEyebrow = root.Q<Label>("journey-result-eyebrow");
            resultTitle = root.Q<Label>("journey-result-title");
            resultText = root.Q<Label>("journey-result-text");
            resultContinue = root.Q<Button>("journey-result-continue");
            speedButton = root.Q<Button>("journey-speed");
            pauseButton = root.Q<Button>("journey-pause");
            root.Q<Button>("journey-dev-menu").clicked += ReturnToDeveloperMenu;
            root.Q<Button>("journey-ledger-open").clicked += ToggleLedger;
            root.Q<Button>("journey-ledger-close").clicked += ToggleLedger;
            sealApply.clicked += ApplySelectedSeal;

            for (int index = 0; index < cardButtons.Length; index++)
            {
                int captured = index;
                cardButtons[index] = root.Q<Button>($"journey-card-{index}");
                cardButtons[index].clicked += () => PlayCard(captured);
            }

            choiceA.clicked += () => SelectChoice(0);
            choiceB.clicked += () => SelectChoice(1);
            eventA.clicked += () => ResolveEvent(true);
            eventB.clicked += () => ResolveEvent(false);
            miniGameAction.clicked += MiniGameAction;
            miniGameLeft.clicked += () => NudgeBalance(-1f);
            miniGameRight.clicked += () => NudgeBalance(1f);
            drawPileButton.clicked += () => OpenPileOverlay(true);
            discardPileButton.clicked += () => OpenPileOverlay(false);
            pileCloseButton.clicked += ClosePileOverlay;
            pileScrim.clicked += ClosePileOverlay;
            resultContinue.clicked += ContinueAfterResult;
            speedButton.clicked += ToggleSpeed;
            pauseButton.clicked += TogglePause;

            Texture2D walkSheet = PackspireResources.Load<Texture2D>(WalkSheetResource);
            if (walkSheet != null)
            {
                Image portrait = root.Q<Image>("journey-portrait");
                portrait.image = walkSheet;
                portrait.uv = new Rect(0f, 0f, 1f / 6f, 1f);
            }

            RefreshPersistentUi();
            uiBound = true;
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
                if (defenseActive)
                {
                    float defenseDelta = realtimeBattleActive
                        ? realtimeTimelineDelta
                        : gameplayDelta;
                    UpdateDefense(defenseDelta);
                }
                else UpdateBattleIdleMotion();
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
            if (Input.GetKeyDown(KeyCode.R))
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
            if (Input.GetKeyDown(KeyCode.F5)) BeginBattle();
            if (Input.GetKeyDown(KeyCode.F6))
            {
                int previewIndex = completedRoadTasks % 6;
                completedRoadTasks++;
                DevShowMiniGame(previewIndex);
            }
        }

        private void ReturnToDeveloperMenu()
        {
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
            choices = CourierRouteSystem.Available(run.courierRoute).ToArray();
            if (choices.Length == 0)
            {
                ShowResult("ROUTE COMPLETE", "配達完了", "紫晶の封鐘塔へ荷を届けた。", false);
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
            choiceTitle.text = run.courierRoute.currentNodeId == "dispatch" ? "最初の配達路を選ぶ" : "次の主要経路を選ぶ";
            choiceNote.text = "どちらを選んでも目的地へ到着します。変わるのは旅の内容と成果です。";
            BindChoice(0, choices[0]);
            choiceB.EnableInClassList("is-hidden", choices.Length < 2);
            if (choices.Length > 1) BindChoice(1, choices[1]);
            phaseText.text = "ROUTE DECISION";
            nextText.text = "旅の内容と成果を見比べる";
        }

        private void ContinueAlongSingleRoute(CourierRouteNodeDef node)
        {
            if (!CourierRouteSystem.Select(run.courierRoute, node.id) ||
                !CourierRouteSystem.Commit(run, out string message))
            {
                ShowResult("ROUTE INTERRUPTED", "旅程を継続できない", "航路データを確認してください。", false);
                return;
            }

            RefreshPersistentUi();
            ShowToast(message);
            SetWorldForRoute(node);
            BeginTravel(RouteTravelDuration + node.dayCost * .65f, node);
        }

        private void BindChoice(int index, CourierRouteNodeDef node)
        {
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
            title.text = node?.title ?? "---";
            if (node == null) return;
            routeIndex.text = $"ROUTE {(first ? "A" : "B")} / {node.kind} / {JourneyPresentationConfig.GetRoadWidthLabel(node)}";
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
            CourierRouteNodeDef node = choices[index];
            Button selected = index == 0 ? choiceA : choiceB;
            Button rejected = index == 0 ? choiceB : choiceA;
            selected.AddToClassList("is-stamped");
            if (choices.Length > 1) rejected.AddToClassList("is-rejected");
            choiceA.SetEnabled(false);
            choiceB.SetEnabled(false);
            yield return new WaitForSecondsRealtime(.44f);

            if (!CourierRouteSystem.Select(run.courierRoute, node.id) ||
                !CourierRouteSystem.Commit(run, out string message))
            {
                ResetChoiceVisuals();
                ShowToast("この経路は現在選べません。");
                choiceCommitRoutine = null;
                yield break;
            }

            RefreshPersistentUi();
            ShowToast(message);
            SetWorldForRoute(node);
            BeginTravel(RouteTravelDuration + node.dayCost * .65f, node);
            choiceCommitRoutine = null;
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

        private void ToggleLedger()
        {
            if (ledgerOverlay == null) return;
            ledgerOpen = !ledgerOpen;
            ledgerOverlay.EnableInClassList("ledger--open", ledgerOpen);
            screen.EnableInClassList("ledger--active", ledgerOpen);
            if (ledgerOpen) PopulateLedger();
            ApplyWorldMotion();
        }

        private void PopulateLedger()
        {
            PopulateLedgerMap();
            PopulateSealBox();
            CourierRouteState route = run.courierRoute;
            CourierRouteNodeDef current = CourierRouteSystem.Node(route.currentNodeId);
            int remaining = Mathf.Max(0, route.deadlineDays - route.daysElapsed);
            ledgerSummary.text = $"現在地：{current?.title ?? "出発局"}　経過 {route.daysElapsed}日　期限まで {remaining}日　回収 {route.recoveredCargoCount}件";
            ShowLedgerNode(current);
        }

        private void PopulateLedgerMap()
        {
            ledgerPhaseHost.Clear();
            CourierRouteState route = run.courierRoute;
            ILookup<int, CourierRouteNodeDef> byPhase = CourierRouteSystem.Nodes.ToLookup(node => node.phase);
            for (int phaseIndex = 0; phaseIndex <= CourierRouteSystem.TotalSegments; phaseIndex++)
            {
                VisualElement column = new VisualElement();
                column.AddToClassList("ps-journey__ledger-phase");
                Label phaseLabel = new Label(phaseIndex == 0 ? "START" : phaseIndex == CourierRouteSystem.TotalSegments ? "GOAL" : phaseIndex.ToString("00"));
                phaseLabel.AddToClassList("ps-journey__ledger-phase-number");
                column.Add(phaseLabel);

                VisualElement nodes = new VisualElement();
                nodes.AddToClassList("ps-journey__ledger-phase-nodes");
                foreach (CourierRouteNodeDef node in byPhase[phaseIndex])
                {
                    CourierRouteNodeDef captured = node;
                    Button button = new Button(() => ShowLedgerNode(captured));
                    button.AddToClassList("ps-journey__ledger-node");
                    bool revealed = CourierRouteSystem.IsRevealed(route, node);
                    button.text = revealed ? node.title : "未確認";
                    button.EnableInClassList("node--unknown", !revealed);
                    button.EnableInClassList("node--resolved", route.resolvedNodeIds.Contains(node.id));
                    button.EnableInClassList("node--current", route.currentNodeId == node.id);
                    button.EnableInClassList("node--available", choices.Any(choice => choice.id == node.id));
                    button.EnableInClassList("node--destination", node.resolution == CourierResolutionKind.Delivery);
                    nodes.Add(button);
                }
                column.Add(nodes);
                ledgerPhaseHost.Add(column);

                if (phaseIndex < CourierRouteSystem.TotalSegments)
                {
                    Label arrow = new Label("›");
                    arrow.AddToClassList("ps-journey__ledger-arrow");
                    ledgerPhaseHost.Add(arrow);
                }
            }
        }

        private void ShowLedgerNode(CourierRouteNodeDef node)
        {
            if (node == null || !CourierRouteSystem.IsRevealed(run.courierRoute, node))
            {
                ledgerNodeTitle.text = "未確認地点";
                ledgerNodeMeta.text = node == null ? "ROUTE DATA UNAVAILABLE" : $"PHASE {node.phase:00}";
                ledgerNodeBody.text = "この先へ進むと遭遇傾向と所要日数が明らかになります。";
                return;
            }

            ledgerNodeTitle.text = node.title;
            ledgerNodeMeta.text = $"PHASE {node.phase:00} / {ResolutionLabel(node.resolution)} / {node.dayCost}日 / {RiskLabel(node.risk)} / {JourneyPresentationConfig.GetRoadWidthLabel(node)}";
            ledgerNodeBody.text = string.IsNullOrWhiteSpace(node.condition)
                ? node.resolutionText
                : $"{node.condition}\n{node.resolutionText}";
        }

        private void PopulateSealBox()
        {
            sealHost.Clear();
            DeliverySealState[] seals = (run.courierRoute.seals ?? new List<DeliverySealState>())
                .Where(seal => seal != null && seal.available).ToArray();
            bool canUse = CanUseSealNow();
            sealTiming.text = phase == Phase.Choice
                ? "大分岐の確定前です。区間用の印を使用できます。"
                : "台帳の確認は可能です。印は主要経路の確定前に使用します。";

            foreach (DeliverySealState seal in seals)
            {
                DeliverySealState captured = seal;
                Button entry = new Button(() => SelectSeal(captured.key));
                entry.userData = seal.key;
                entry.AddToClassList("ps-journey__seal-entry");
                entry.EnableInClassList("seal--selected", selectedSealKey == seal.key);
                entry.EnableInClassList("seal--spent", seal.charges <= 0);
                entry.SetEnabled(seal.charges > 0);

                VisualElement mark = new VisualElement { pickingMode = PickingMode.Ignore };
                mark.AddToClassList("ps-journey__seal-mark");
                Label name = new Label(seal.name) { pickingMode = PickingMode.Ignore };
                name.AddToClassList("ps-journey__seal-entry-name");
                Label charge = new Label($"残 {seal.charges} / {seal.maxCharges}　{SealTargetLabel(seal.target)}") { pickingMode = PickingMode.Ignore };
                charge.AddToClassList("ps-journey__seal-entry-charge");
                entry.Add(mark);
                entry.Add(name);
                entry.Add(charge);
                sealHost.Add(entry);
            }

            DeliverySealState selected = seals.FirstOrDefault(seal => seal.key == selectedSealKey);
            if (selected == null)
            {
                selectedSealKey = "";
                sealDetailName.text = seals.Length == 0 ? "配達印なし" : "印を選択";
                sealDetailEffect.text = seals.Length == 0 ? "荷造りの色一致から配達印を生成できます。" : "印を選ぶと効果と使用可能なタイミングを確認できます。";
                sealApply.SetEnabled(false);
            }
            else ShowSealDetail(selected, canUse);
        }

        private void SelectSeal(string key)
        {
            selectedSealKey = key;
            PopulateSealBox();
        }

        private void ShowSealDetail(DeliverySealState seal, bool canUse)
        {
            sealDetailName.text = $"{seal.name}　残 {seal.charges}/{seal.maxCharges}";
            sealDetailEffect.text = $"{SealTargetLabel(seal.target)}：{seal.text}";
            sealApply.SetEnabled(canUse && seal.charges > 0);
        }

        private void ApplySelectedSeal()
        {
            if (!CanUseSealNow() || string.IsNullOrEmpty(selectedSealKey)) return;
            if (!CourierRouteSystem.UseSeal(run.courierRoute, selectedSealKey, out string message)) return;
            RefreshPersistentUi();
            PopulateSealBox();
            RefreshChoiceAfterSeal();
            ShowToast(message);
        }

        private bool CanUseSealNow()
        {
            CourierRouteState route = run.courierRoute;
            return phase == Phase.Choice && !choiceLocked && !route.complete && !route.failed &&
                   !route.travelPending && !route.awaitingResolution;
        }

        private void RefreshChoiceAfterSeal()
        {
            if (phase != Phase.Choice) return;
            if (choices.Length > 0) BindChoice(0, choices[0]);
            if (choices.Length > 1) BindChoice(1, choices[1]);
        }

        private static string SealTargetLabel(string target)
        {
            return target switch
            {
                DeliverySealSystem.DelayTarget => "地点事故",
                DeliverySealSystem.SealTarget => "印の再装填",
                _ => "次の区間"
            };
        }

        private static void ApplyRouteArt(VisualElement art, CourierRouteNodeDef node)
        {
            string[] classes = { "route-art--ash", "route-art--danger", "route-art--drowned", "route-art--blackbell" };
            foreach (string className in classes) art.RemoveFromClassList(className);
            string nextClass = node.phase >= 8
                ? "route-art--blackbell"
                : node.phase >= 5
                    ? "route-art--drowned"
                    : node.risk >= 2 ? "route-art--danger" : "route-art--ash";
            art.AddToClassList(nextClass);
        }

        private static string RouteFlavor(CourierRouteNodeDef node)
        {
            return node.resolution switch
            {
                CourierResolutionKind.Relay => "検札灯の残る街道を辿る。遠回りだが、荷と身体を整えられる。",
                CourierResolutionKind.Battle => "追跡者の巡回路を横切る。短いが、戦闘を避けては通れない。",
                CourierResolutionKind.Cargo => "配達記録が途切れた区画へ入る。未回収の荷が残されている。",
                CourierResolutionKind.Event => "古い道標に従う不確かな経路。何が待つかは現地で判明する。",
                CourierResolutionKind.Delivery => "封鐘塔へ続く最後の道。荷を守り、受取印まで届ける。",
                _ => "灰市を抜け、次の中継地点へ向かう主要経路。"
            };
        }

        private static string RouteRewardLabel(string resolution)
        {
            return resolution switch
            {
                CourierResolutionKind.Relay => "整備・回復",
                CourierResolutionKind.Cargo => "回収荷物",
                CourierResolutionKind.Battle => "戦利品",
                CourierResolutionKind.Event => "特殊報酬",
                CourierResolutionKind.Delivery => "配達完了",
                _ => "旅程進行"
            };
        }

        private static string RouteSealLabel(CourierRouteNodeDef node)
        {
            if (node.risk >= 2) return "遅延防止印";
            if (node.dayCost >= 2) return "短縮印";
            if (node.resolution == CourierResolutionKind.Relay) return "補綴印";
            return "任意";
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

        public void DevBeginBattle()
        {
            if (!uiBound)
            {
                BindUi();
            }
            BeginBattle();
        }

        public void DevPreviewScenery(int previewBiome)
        {
            if (!uiBound) BindUi();
            biomeIndex = Mathf.Clamp(previewBiome, 0, 2);
            scenery?.ClearAll();
            walker.SetJourneyBiome(biomeIndex);
            walker.SetRoadProfile(JourneyWalkCyclePrototype.RoadProfile.Standard);
            environment.SetBiome(biomeIndex);
            weatherText.text = BiomeWeatherForIndex(biomeIndex);
            BeginTravel(OpeningTravelDuration, null);
            ShowToast($"DEV景色確認：{BiomeLabel(biomeIndex)}・標準路");
        }

        public void DevStartDefense()
        {
            if (phase != Phase.Battle) DevBeginBattle();
            if (!defenseActive) BeginEnemyTurn();
        }

        public void DevStartReactionPreview()
        {
            if (phase != Phase.Battle) DevBeginBattle();
            if (defenseActive || battle == null) return;
            battle.move = 1;
            RefreshBattleUi();
            BeginEnemyTurn();
        }

        public void DevSetPaused(bool value)
        {
            if (!uiBound) BindUi();
            SetPaused(value);
        }

        public void DevResolveJump()
        {
            ResolveDefenseInput(DefenseAction.Jump);
        }

        public void DevResolveBrace()
        {
            ResolveDefenseInput(DefenseAction.Brace);
        }

        public void DevSkipEncounterIntro()
        {
            encounterBanner?.RemoveFromClassList("encounter--visible");
            encounterIntroActive = false;
            if (phase != Phase.Battle || screen.ClassListContains("battle--layout-preview")) return;
            battleInputLocked = false;
            RefreshBattleUi();
        }

        public void DevBeginMiniGame()
        {
            if (!uiBound) BindUi();
            BeginMiniGame();
        }

        public void DevShowMiniGame(int index)
        {
            if (!uiBound) BindUi();
            MiniGameKind[] all =
            {
                MiniGameKind.StampTiming,
                MiniGameKind.CargoBalance,
                MiniGameKind.AddressLabel,
                MiniGameKind.WaxMatch,
                MiniGameKind.RoadDodge,
                MiniGameKind.RainCover
            };
            BeginMiniGame(all[Mathf.Clamp(index, 0, all.Length - 1)]);
        }

        public void DevShowChoice()
        {
            if (!uiBound) BindUi();
            firstChoicePending = false;
            ShowChoice();
        }

        public void DevToggleLedger()
        {
            if (!uiBound) BindUi();
            ToggleLedger();
        }

        public bool DevUseFirstSeal()
        {
            if (!uiBound) BindUi();
            DeliverySealState seal = run.courierRoute.seals?.FirstOrDefault(value => value.available && value.charges > 0);
            if (seal == null) return false;
            int before = seal.charges;
            selectedSealKey = seal.key;
            PopulateSealBox();
            ApplySelectedSeal();
            return seal.charges < before;
        }

        public void DevShowEvent()
        {
            if (!uiBound) BindUi();
            arrivalNode = CourierRouteSystem.Node("broken_stair");
            ShowEvent(arrivalNode);
        }

        private void ResolveRoute(CourierLocationOutcome outcome)
        {
            CourierRouteSystem.ResolveCurrent(run, outcome, out string message);
            RefreshPersistentUi();
            bool complete = run.courierRoute.complete;
            ShowResult(complete ? "DELIVERY COMPLETE" : "ROUTE CLEARED", complete ? "最終配達完了" : arrivalNode?.resolutionTitle ?? "地点を突破", message, !complete);
        }

        private void ShowResult(string eyebrow, string title, string body, bool canContinue)
        {
            SetPhase(Phase.Result);
            walker.SetJourneyWalking(false);
            SetMainEnemyVisible(false);
            resultEyebrow.text = eyebrow;
            resultTitle.text = title;
            resultText.text = body;
            resultContinue.text = canContinue ? "旅程を再開" : "DEV SIMULATIONを再開 [R]";
            resultContinue.userData = canContinue;
        }

        private void ContinueAfterResult()
        {
            bool canContinue = resultContinue.userData is bool value && value;
            if (!canContinue)
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                return;
            }
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
            dayText.text = $"DAY {route.daysElapsed} / {route.deadlineDays}";
            int remaining = Mathf.Max(0, route.deadlineDays - route.daysElapsed);
            conditionText.text = remaining >= 7 ? "安定" : remaining >= 3 ? "逼迫" : "危険";
            conditionNoteText.text = $"期限まで {remaining}日";
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
            yield return new WaitForSecondsRealtime(.28f);
            scenery?.ClearAll();
            walker.SetJourneyBiome(targetBiome);
            environment.SetBiome(targetBiome);
            walker.SetRoadProfile(targetRoad);
            yield return new WaitForSecondsRealtime(.12f);
            transition.RemoveFromClassList("transition--visible");
            yield return new WaitForSecondsRealtime(.36f);
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
                ResolveRoute(new CourierLocationOutcome { success = true, performance = 2, message = "番人を退けた。" });
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

        private void BuildWorldActors()
        {
            LoadEnemyBattleFrames();
            Sprite enemySprite = enemyBattleFrames.Length == 6
                ? enemyBattleFrames[(int)EnemyBattlePose.Idle]
                : PackspireResources.Load<Sprite>(EnemyResource);
            if (enemySprite != null)
            {
                GameObject enemyObject = new GameObject("Chibi Postal Warden");
                enemyRenderer = enemyObject.AddComponent<SpriteRenderer>();
                enemyRenderer.sprite = enemySprite;
                JourneyPresentationConfig.Sort(
                    enemyRenderer,
                    JourneyPresentationConfig.ActorSortingLayer,
                    10);
                enemyObject.transform.position = EnemyBattleStartPosition;
                enemyObject.transform.localScale = Vector3.one * EnemyBattleScale;
                enemyRenderer.enabled = false;

                Sprite shadowSprite = CreateBattleActorShadowSprite();
                enemyShadowRenderer = CreateBattleActorShadow(
                    "Postal Warden Ground Shadow",
                    shadowSprite,
                    EnemyBattleStartPosition,
                    new Vector3(1.52f, .42f, 1f),
                    8);

                GameObject telegraphObject = new GameObject("Postal Warden Telegraph Silhouette");
                enemyTelegraphRenderer = telegraphObject.AddComponent<SpriteRenderer>();
                enemyTelegraphRenderer.sprite = enemySprite;
                JourneyPresentationConfig.Sort(
                    enemyTelegraphRenderer,
                    JourneyPresentationConfig.ActorSortingLayer,
                    9);
                telegraphObject.transform.position = enemyObject.transform.position;
                telegraphObject.transform.localScale = enemyObject.transform.localScale * 1.09f;
                enemyTelegraphRenderer.color = new Color(1f, 1f, 1f, 0f);
                enemyTelegraphRenderer.enabled = false;

                for (int previewIndex = 0; previewIndex < battlePreviewEnemyRenderers.Length; previewIndex++)
                {
                    GameObject previewEnemy = new GameObject($"Battle Layout Enemy {previewIndex + 2}");
                    SpriteRenderer previewRenderer = previewEnemy.AddComponent<SpriteRenderer>();
                    previewRenderer.sprite = enemySprite;
                    JourneyPresentationConfig.Sort(
                        previewRenderer,
                        JourneyPresentationConfig.ActorSortingLayer,
                        10 - previewIndex);
                    previewEnemy.transform.position = BattlePreviewEnemyPosition(previewIndex);
                    previewEnemy.transform.localScale = Vector3.one * (previewIndex == 0 ? .52f : .45f);
                    previewRenderer.color = previewIndex == 0
                        ? new Color(.84f, .9f, .94f, 1f)
                        : new Color(.72f, .77f, .82f, 1f);
                    previewRenderer.enabled = false;
                    battlePreviewEnemyRenderers[previewIndex] = previewRenderer;
                    battlePreviewEnemyShadowRenderers[previewIndex] = CreateBattleActorShadow(
                        $"Battle Layout Enemy {previewIndex + 2} Shadow",
                        shadowSprite,
                        BattlePreviewEnemyPosition(previewIndex),
                        new Vector3(previewIndex == 0 ? 1.28f : 1.12f, .38f, 1f),
                        7 - previewIndex);
                }
            }
            else
            {
                enemyRenderer = new GameObject("Missing Chibi Enemy").AddComponent<SpriteRenderer>();
                enemyRenderer.enabled = false;
            }

            GameObject parcel = new GameObject("Roadside Parcel");
            parcelRenderer = parcel.AddComponent<SpriteRenderer>();
            parcelRenderer.sprite = CreateParcelSprite();
            JourneyPresentationConfig.Sort(
                parcelRenderer,
                JourneyPresentationConfig.EffectSortingLayer,
                0);
            parcel.transform.localScale = Vector3.one * .72f;
            parcelRenderer.enabled = false;
        }

        private void LoadEnemyBattleFrames()
        {
            Sprite[] loaded = PackspireResources.LoadAll<Sprite>(EnemyBattleResource);
            string[] orderedNames =
            {
                "journey-warden-battle-idle",
                "journey-warden-battle-high-anticipation",
                "journey-warden-battle-high-impact",
                "journey-warden-battle-low-anticipation",
                "journey-warden-battle-low-impact",
                "journey-warden-battle-hit"
            };
            if (loaded == null || loaded.Length < orderedNames.Length)
            {
                enemyBattleFrames = Array.Empty<Sprite>();
                return;
            }

            Sprite[] ordered = new Sprite[orderedNames.Length];
            for (int index = 0; index < orderedNames.Length; index++)
            {
                ordered[index] = loaded.FirstOrDefault(sprite => sprite.name == orderedNames[index]);
                if (ordered[index] == null)
                {
                    enemyBattleFrames = Array.Empty<Sprite>();
                    return;
                }
            }
            enemyBattleFrames = ordered;
        }

        private void SetEnemyBattlePose(EnemyBattlePose pose)
        {
            if (enemyRenderer == null || enemyBattleFrames.Length != 6)
            {
                return;
            }

            Sprite sprite = enemyBattleFrames[(int)pose];
            enemyRenderer.sprite = sprite;
            if (enemyTelegraphRenderer != null)
            {
                enemyTelegraphRenderer.sprite = sprite;
            }
        }

        private static Vector3 BattlePreviewEnemyPosition(int previewIndex)
        {
            return new Vector3(previewIndex == 0 ? 4.35f : 6.35f, BattleGroundY, 0f);
        }

        private SpriteRenderer CreateBattleActorShadow(
            string objectName,
            Sprite sprite,
            Vector3 actorPosition,
            Vector3 scale,
            int sortingOrder)
        {
            GameObject shadowObject = new GameObject(objectName);
            SpriteRenderer renderer = shadowObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = new Color(.02f, .015f, .02f, .42f);
            JourneyPresentationConfig.Sort(
                renderer,
                JourneyPresentationConfig.ActorSortingLayer,
                sortingOrder);
            shadowObject.transform.position = new Vector3(actorPosition.x, BattleGroundY + .025f, 0f);
            shadowObject.transform.localScale = scale;
            renderer.enabled = false;
            return renderer;
        }

        private Sprite CreateBattleActorShadowSprite()
        {
            const int width = 96;
            const int height = 32;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "journey-battle-actor-shadow",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };
            Color32[] pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                float nx = (x + .5f - width * .5f) / (width * .5f);
                float ny = (y + .5f - height * .5f) / (height * .5f);
                float alpha = Mathf.Clamp01((1f - nx * nx - ny * ny) * 1.7f);
                pixels[y * width + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, width, height),
                new Vector2(.5f, .5f),
                100f);
            sprite.name = "journey-battle-actor-shadow-sprite";
            runtimeAssets.Add(sprite);
            runtimeAssets.Add(texture);
            return sprite;
        }

        private void SetMainEnemyVisible(bool visible)
        {
            if (enemyRenderer != null) enemyRenderer.enabled = visible;
            if (enemyShadowRenderer != null) enemyShadowRenderer.enabled = visible;
        }

        private void SyncMainEnemyShadow()
        {
            if (enemyShadowRenderer == null || enemyRenderer == null) return;
            Vector3 actorPosition = enemyRenderer.transform.position;
            enemyShadowRenderer.transform.position = new Vector3(actorPosition.x, BattleGroundY + .025f, 0f);
        }

        private Sprite CreateParcelSprite()
        {
            const int size = 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "journey-roadside-parcel";
            Color clear = new Color(0f, 0f, 0f, 0f);
            Color paper = new Color(.52f, .31f, .17f, 1f);
            Color edge = new Color(.14f, .09f, .07f, 1f);
            Color seal = new Color(.64f, .12f, .08f, 1f);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                bool body = x >= 8 && x <= 55 && y >= 14 && y <= 48;
                bool border = body && (x < 12 || x > 51 || y < 18 || y > 44);
                float dx = x - 32f, dy = y - 31f;
                bool wax = dx * dx + dy * dy < 43f;
                texture.SetPixel(x, y, wax ? seal : border ? edge : body ? paper : clear);
            }
            texture.Apply();
            runtimeAssets.Add(texture);
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 64f);
            runtimeAssets.Add(sprite);
            return sprite;
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
