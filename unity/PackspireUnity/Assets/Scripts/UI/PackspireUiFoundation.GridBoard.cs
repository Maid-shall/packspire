using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire {
public sealed partial class PackspireUiFoundation {
 // GridBoard is split by responsibility to keep the screen safe to extend:
 // Map = board/input/camera, Presentation = shared HUD, Combat = enemy data,
 // Cards = hand/previews, Events = modal encounters and the screen tick.
 VisualElement gridBoardRoot,gridBoardGrid,gridBoardActorLayer,gridBoardHeroActor,gridBoardHandRoot,gridBoardDirRow,gridBoardHpFill,gridBoardShieldFill,gridBoardPortraitHost;
 VisualElement gridBoardViewport,gridBoardStage,gridBoardSelectedHost,gridBoardCombatStage,gridBoardCombatHpTrack,gridBoardCombatHpFill,gridBoardCombatShieldTrack,gridBoardCombatShieldFill;
 VisualElement gridBoardCombatEnemyFocus,gridBoardCombatVitals,gridBoardCombatIntentPanel,gridBoardCombatEnemyStatuses;
 VisualElement gridBoardCombatActionView,gridBoardCombatCardPreview;
 VisualElement gridBoardContextActions,gridBoardRoutePalette,gridBoardEnergyRail,gridBoardCombatRail,gridBoardConsumablesRoot,gridBoardFxLayer;
 VisualElement gridBoardPlayerHud,gridBoardMapStats;
 VisualElement gridBoardGateActions,gridBoardResolveTray,gridBoardResolveDice;
 VisualElement gridBoardHoverPreview,gridBoardCellDetail,gridBoardEventOverlay;
 Label gridBoardPhaseLabel,gridBoardInkLabel,gridBoardHintLabel;
 Label gridBoardDoomLabel,gridBoardAreaChipLabel,gridBoardCurveChipLabel,gridBoardShieldLabel,gridBoardModeToast;
 Label gridBoardTypeLabel,gridBoardTitleLabel,gridBoardStatusLabel,gridBoardBodyLabel;
 Label gridBoardHeroNameLabel,gridBoardHpLabel,gridBoardEnergyLabel,gridBoardCombatTitle;
 Label gridBoardCombatHpLabel,gridBoardCombatShieldLabel,gridBoardCombatIntentLabel,gridBoardCombatIntentHintLabel,gridBoardResolveFormula,gridBoardResolveResult;
 Image gridBoardCombatPortrait;
 Button gridBoardSkillButton,gridBoardEndTurnButton;
 Button gridBoardHoverCard;
 readonly Dictionary<long,VisualElement> gridBoardCells=new();
 bool gridBoardBuilt;
 bool gridBoardHandOpen;
 bool gridBoardLayoutBusy;
 bool gridBoardLayoutQueued;
 bool gridBoardCombatMode;
 bool gridBoardEventOpen;
 bool gridBoardDiceResultActive;
 float gridBoardDiceResultUntil;
 int gridBoardDieOne,gridBoardDieTwo,gridBoardDiceModifier,gridBoardDiceTotal,gridBoardDiceDamage;
 string gridBoardDiceSource="";
 bool gridBoardFollowingExplorer;
 float gridBoardZoom=1f;
 Vector2 gridBoardPan;
 bool gridBoardPanning,gridBoardDidPan;
 bool gridBoardMapHover,gridBoardDockHover,gridBoardHeaderHover;
 Vector2 gridBoardPointerStart,gridBoardPanAtStart;
 Vector2 gridBoardLastLayoutPos=new(float.NaN,float.NaN);
 Texture2D[] gridBoardCardFrames;
 Texture2D gridBoardMakaiBackground,gridBoardBoardSurface,gridBoardHudTop,gridBoardEnergyRailArt,gridBoardInfoHeaderArt,gridBoardPlayerHudArt;
 int gridBoardFloaterSerial;
 const float GridZoomMin=0.55f;
 const float GridZoomMax=1.9f;
 const float GridCellBasePx=64f;
 // A closed hand should still show enough of every card to read the identity.
 // It is a deliberate composition element at the bottom of the expedition,
 // not a hidden drawer.
 const float GridHandPeekSink=132f;
 const float GridHandWidth=760f;
 const float GridHandCardWidth=168f;
 // Board y=0 is top of screen, so "up" on UI decreases y.
 static readonly Vector2Int BoardUp=new(0,-1);
 static readonly Vector2Int BoardDown=new(0,1);
 static readonly Vector2Int BoardLeft=new(-1,0);
 static readonly Vector2Int BoardRight=new(1,0);

 void SuspendGridBoard(){
  gridBoardBuilt=false;
  gridBoardHandOpen=false;
  gridBoardCombatMode=false;
  gridBoardRoot=null;gridBoardGrid=null;gridBoardActorLayer=null;gridBoardHandRoot=null;gridBoardDirRow=null;
  gridBoardHpFill=null;gridBoardShieldFill=null;gridBoardPortraitHost=null;
  gridBoardViewport=null;gridBoardStage=null;gridBoardSelectedHost=null;gridBoardCombatStage=null;gridBoardCombatHpTrack=null;gridBoardCombatHpFill=null;gridBoardCombatShieldTrack=null;gridBoardCombatShieldFill=null;
  gridBoardCombatEnemyFocus=null;gridBoardCombatVitals=null;gridBoardCombatIntentPanel=null;gridBoardCombatEnemyStatuses=null;
  gridBoardCombatActionView=null;gridBoardCombatCardPreview=null;
  gridBoardContextActions=null;gridBoardRoutePalette=null;gridBoardEnergyRail=null;gridBoardCombatRail=null;
  gridBoardPlayerHud=null;gridBoardMapStats=null;
  gridBoardConsumablesRoot=null;gridBoardFxLayer=null;gridBoardGateActions=null;gridBoardResolveTray=null;gridBoardResolveDice=null;
  gridBoardHoverPreview=null;gridBoardCellDetail=null;gridBoardEventOverlay=null;
  gridBoardPhaseLabel=null;gridBoardInkLabel=null;gridBoardHintLabel=null;gridBoardDoomLabel=null;
  gridBoardAreaChipLabel=null;gridBoardCurveChipLabel=null;gridBoardShieldLabel=null;gridBoardModeToast=null;
  gridBoardTypeLabel=null;gridBoardTitleLabel=null;gridBoardStatusLabel=null;gridBoardBodyLabel=null;
  gridBoardHeroNameLabel=null;gridBoardHpLabel=null;gridBoardEnergyLabel=null;gridBoardCombatTitle=null;
  gridBoardCombatHpLabel=null;gridBoardCombatShieldLabel=null;gridBoardCombatIntentLabel=null;gridBoardCombatIntentHintLabel=null;gridBoardResolveFormula=null;gridBoardResolveResult=null;gridBoardCombatPortrait=null;
  gridBoardSkillButton=null;gridBoardEndTurnButton=null;
  gridBoardHoverCard=null;
  gridBoardPanning=false;gridBoardDidPan=false;
  gridBoardMapHover=false;gridBoardDockHover=false;gridBoardHeaderHover=false;
  gridBoardLayoutBusy=false;
  gridBoardLayoutQueued=false;
  gridBoardFollowingExplorer=false;
  gridBoardEventOpen=false;
  gridBoardDiceResultActive=false;
  gridBoardDiceResultUntil=0f;
  gridBoardDiceSource="";
  gridBoardLastLayoutPos=new(float.NaN,float.NaN);
  gridBoardCells.Clear();
 }

 void EnsureGridBoardCardFrames(){
  if(gridBoardCardFrames!=null)return;
  gridBoardCardFrames=new Texture2D[3];
  for(int i=0;i<3;i++)
   gridBoardCardFrames[i]=Resources.Load<Texture2D>($"Art/UI/Cards/combat-card-{i:00}");
 }

 void EnsureGridBoardEnvironmentArt(){
  if(gridBoardMakaiBackground==null)
   gridBoardMakaiBackground=Resources.Load<Texture2D>("Art/UI/Product/dungeon-makai-bg-01");
  if(gridBoardBoardSurface==null)
   gridBoardBoardSurface=Resources.Load<Texture2D>("Art/UI/Product/dungeon-board-surface-01");
  if(gridBoardHudTop==null)
   gridBoardHudTop=Resources.Load<Texture2D>("Art/UI/Product/dungeon-hud-top-compact-v1")
    ??Resources.Load<Texture2D>("Art/UI/Product/dungeon-hud-top-layout-v2");
  if(gridBoardEnergyRailArt==null)
   gridBoardEnergyRailArt=Resources.Load<Texture2D>("Art/UI/Product/dungeon-energy-simple-layout-v3");
  if(gridBoardInfoHeaderArt==null)
   gridBoardInfoHeaderArt=Resources.Load<Texture2D>("Art/UI/Product/dungeon-info-header-layout-v2");
  if(gridBoardPlayerHudArt==null)
   gridBoardPlayerHudArt=Resources.Load<Texture2D>("Art/UI/Product/dungeon-player-status-cluster-v2");
 }

 void BuildGridBoard(){
  var run=game.UiGridBoard;
  if(run==null){
   game.UiDevOpenGridBoard();
   run=game.UiGridBoard;
  }
  SuspendGridBoard();
  gridBoardBuilt=true;
  EnsureGridBoardCardFrames();
  EnsureGridBoardEnvironmentArt();
  // Zoom controls are intentionally not exposed in this layout pass.
  gridBoardZoom=1f;
  gridBoardPan=Vector2.zero;

  gridBoardRoot=Container("ps-gboard");
  if(gridBoardMakaiBackground!=null){
   gridBoardRoot.style.backgroundImage=new StyleBackground(gridBoardMakaiBackground);
   gridBoardRoot.style.unityBackgroundScaleMode=ScaleMode.StretchToFill;
  }
  screenRoot.Add(gridBoardRoot);

  // Map stage is the right pane (dock stays on the left).
  gridBoardStage=Container("ps-gboard-stage");
  gridBoardViewport=Container("ps-gboard-viewport");
  gridBoardViewport.RegisterCallback<WheelEvent>(OnGridBoardWheel,TrickleDown.TrickleDown);
  gridBoardViewport.RegisterCallback<PointerDownEvent>(OnGridBoardPointerDown,TrickleDown.TrickleDown);
  gridBoardViewport.RegisterCallback<PointerMoveEvent>(OnGridBoardPointerMove,TrickleDown.TrickleDown);
  gridBoardViewport.RegisterCallback<PointerUpEvent>(OnGridBoardPointerUp,TrickleDown.TrickleDown);
  gridBoardViewport.RegisterCallback<PointerCaptureOutEvent>(_=>{gridBoardPanning=false;});
  gridBoardViewport.RegisterCallback<GeometryChangedEvent>(OnGridViewportGeometryChanged);

  gridBoardGrid=Container("ps-gboard-grid");
  if(gridBoardBoardSurface!=null){
   gridBoardGrid.style.backgroundImage=new StyleBackground(gridBoardBoardSurface);
   gridBoardGrid.style.unityBackgroundScaleMode=ScaleMode.StretchToFill;
  }
  gridBoardGrid.style.position=Position.Absolute;
  gridBoardGrid.style.flexGrow=0;
  gridBoardGrid.style.flexShrink=0;
  // Pan and zoom begin on the stone board itself, never on the surrounding
  // dungeon backdrop or the HUD.
  BuildGridCells(run);
  gridBoardViewport.Add(gridBoardGrid);
  gridBoardActorLayer=Container("ps-gboard-actors");
  gridBoardActorLayer.pickingMode=PickingMode.Ignore;
  gridBoardViewport.Add(gridBoardActorLayer);
  EnsureBattleAssets();
  gridBoardCombatStage=Container("ps-gboard-combat-stage");
  gridBoardCombatStage.style.display=DisplayStyle.None;
  gridBoardCombatEnemyFocus=Container("ps-gboard-enemy-focus");
  var enemyEyebrow=new Label("HOSTILE SIGNATURE"){pickingMode=PickingMode.Ignore};
  enemyEyebrow.AddToClassList("ps-gboard-enemy-eyebrow");
  gridBoardCombatEnemyFocus.Add(enemyEyebrow);
  gridBoardCombatPortrait=new Image{scaleMode=ScaleMode.ScaleToFit,pickingMode=PickingMode.Ignore};
  gridBoardCombatPortrait.AddToClassList("ps-gboard-combat-portrait");
  gridBoardCombatEnemyFocus.Add(gridBoardCombatPortrait);
  gridBoardCombatStage.Add(gridBoardCombatEnemyFocus);

  // Enemy durability sits directly below the portrait. Intent is deliberately
  // separate on the left so the persistent player towers never cover it.
  gridBoardCombatVitals=Container("ps-gboard-combat-vitals");
  var vitalityCaption=new Label("VITALITY"){pickingMode=PickingMode.Ignore};
  vitalityCaption.AddToClassList("ps-gboard-combat-caption");
  gridBoardCombatVitals.Add(vitalityCaption);
  gridBoardCombatTitle=new Label("敵"){pickingMode=PickingMode.Ignore};
  gridBoardCombatTitle.AddToClassList("ps-gboard-combat-title");
  gridBoardCombatVitals.Add(gridBoardCombatTitle);
  gridBoardCombatHpLabel=new Label("HP —"){pickingMode=PickingMode.Ignore};
  gridBoardCombatHpLabel.AddToClassList("ps-gboard-combat-hp");
  gridBoardCombatVitals.Add(gridBoardCombatHpLabel);
  gridBoardCombatHpTrack=Container("ps-gboard-combat-hp-track");
  gridBoardCombatHpTrack.pickingMode=PickingMode.Ignore;
  gridBoardCombatHpFill=Container("ps-gboard-combat-hp-fill");
  gridBoardCombatHpFill.pickingMode=PickingMode.Ignore;
  gridBoardCombatHpTrack.Add(gridBoardCombatHpFill);
  gridBoardCombatVitals.Add(gridBoardCombatHpTrack);
  gridBoardCombatShieldLabel=new Label("SH —"){pickingMode=PickingMode.Ignore};
  gridBoardCombatShieldLabel.AddToClassList("ps-gboard-combat-shield");
  gridBoardCombatVitals.Add(gridBoardCombatShieldLabel);
  gridBoardCombatShieldTrack=Container("ps-gboard-combat-shield-track");
  gridBoardCombatShieldTrack.pickingMode=PickingMode.Ignore;
  gridBoardCombatShieldFill=Container("ps-gboard-combat-shield-fill");
  gridBoardCombatShieldFill.pickingMode=PickingMode.Ignore;
  gridBoardCombatShieldTrack.Add(gridBoardCombatShieldFill);
  gridBoardCombatVitals.Add(gridBoardCombatShieldTrack);
  gridBoardCombatEnemyStatuses=Container("ps-gboard-combat-statuses");
  gridBoardCombatVitals.Add(gridBoardCombatEnemyStatuses);
  gridBoardCombatStage.Add(gridBoardCombatVitals);

  gridBoardCombatIntentPanel=Container("ps-gboard-combat-intent-panel");
  var intentCaption=new Label("NEXT ACTION"){pickingMode=PickingMode.Ignore};
  intentCaption.AddToClassList("ps-gboard-combat-caption");
  gridBoardCombatIntentPanel.Add(intentCaption);
  gridBoardCombatIntentLabel=new Label(""){pickingMode=PickingMode.Ignore};
  gridBoardCombatIntentLabel.AddToClassList("ps-gboard-combat-intent");
  gridBoardCombatIntentPanel.Add(gridBoardCombatIntentLabel);
  gridBoardCombatIntentHintLabel=new Label(""){pickingMode=PickingMode.Ignore};
  gridBoardCombatIntentHintLabel.AddToClassList("ps-gboard-combat-intent-hint");
  gridBoardCombatIntentPanel.Add(gridBoardCombatIntentHintLabel);
  gridBoardCombatStage.Add(gridBoardCombatIntentPanel);
  gridBoardFxLayer=Container("ps-gboard-fx-layer");
  gridBoardFxLayer.pickingMode=PickingMode.Ignore;
  gridBoardCombatStage.Add(gridBoardFxLayer);

  // The right side is the player's context surface. Hovered cards and dice
  // resolution replace one another here without ever covering the enemy art.
  gridBoardCombatActionView=Container("ps-gboard-combat-action-view");
  var actionCaption=new Label("CARD / DICE"){pickingMode=PickingMode.Ignore};
  actionCaption.AddToClassList("ps-gboard-action-view-caption");
  gridBoardCombatActionView.Add(actionCaption);
  gridBoardCombatCardPreview=Container("ps-gboard-combat-card-preview");
  gridBoardCombatActionView.Add(gridBoardCombatCardPreview);
  gridBoardCombatStage.Add(gridBoardCombatActionView);
  gridBoardStage.Add(gridBoardViewport);
  gridBoardStage.RegisterCallback<PointerEnterEvent>(_=>SetGridBoardMapHover(true));
  gridBoardStage.RegisterCallback<PointerLeaveEvent>(_=>SetGridBoardMapHover(false));

  gridBoardRoot.Add(gridBoardStage);
  // The enemy dossier belongs to the persistent screen shell, not to the
  // panned board. It therefore stays fixed at the right edge in battle.
  gridBoardRoot.Add(gridBoardCombatStage);

  // The board owns the entire screen. Only compact, decision-relevant chips
  // remain in the upper-right; the old full-width banner is intentionally gone.
  gridBoardMapStats=Container("ps-gboard-map-stats");
  gridBoardDoomLabel=new Label(""){pickingMode=PickingMode.Ignore};
  gridBoardDoomLabel.AddToClassList("ps-gboard-stat-chip");
  gridBoardDoomLabel.AddToClassList("ps-gboard-stat-turn");
  gridBoardMapStats.Add(gridBoardDoomLabel);
  gridBoardAreaChipLabel=new Label(""){pickingMode=PickingMode.Ignore};
  gridBoardAreaChipLabel.AddToClassList("ps-gboard-stat-chip");
  gridBoardAreaChipLabel.AddToClassList("ps-gboard-stat-area");
  gridBoardMapStats.Add(gridBoardAreaChipLabel);
  gridBoardCurveChipLabel=new Label(""){pickingMode=PickingMode.Ignore};
  gridBoardCurveChipLabel.AddToClassList("ps-gboard-stat-chip");
  gridBoardCurveChipLabel.AddToClassList("ps-gboard-stat-curve");
  gridBoardMapStats.Add(gridBoardCurveChipLabel);
  var finishTop=MakeGridAction("撤退",()=>{
   game.UiRetreatFromGrid();
   ForceRefreshScreen();
  });
  finishTop.AddToClassList("ps-gboard-top-finish");
  gridBoardMapStats.Add(finishTop);
  gridBoardRoot.Add(gridBoardMapStats);

  gridBoardModeToast=new Label("封印格子\n探索"){pickingMode=PickingMode.Ignore};
  gridBoardModeToast.AddToClassList("ps-gboard-mode-toast");
  gridBoardRoot.Add(gridBoardModeToast);
  gridBoardModeToast.schedule.Execute(()=>gridBoardModeToast?.AddToClassList("ps-gboard-mode-toast-out")).StartingIn(1350);
  gridBoardModeToast.schedule.Execute(()=>{
   if(gridBoardModeToast!=null)gridBoardModeToast.style.display=DisplayStyle.None;
  }).StartingIn(1750);

  var dock=Container("ps-gboard-dock");
  var panel=Container("ps-gboard-panel");
  if(gridBoardInfoHeaderArt!=null){
   var ornament=Container("ps-gboard-panel-ornament");
   ornament.pickingMode=PickingMode.Ignore;
   ornament.style.backgroundImage=new StyleBackground(gridBoardInfoHeaderArt);
   ornament.style.unityBackgroundScaleMode=ScaleMode.StretchToFill;
   panel.Add(ornament);
  }
  // The right dock is deliberately an open reading column.  The environment
  // and the important controls carry the ornamental weight; framing this too
  // would make the expedition screen feel like a stack of windows.
  gridBoardTypeLabel=new Label("封印格子"){pickingMode=PickingMode.Ignore};
  gridBoardTypeLabel.AddToClassList("ps-gboard-panel-type");
  panel.Add(gridBoardTypeLabel);
  gridBoardTitleLabel=new Label(""){pickingMode=PickingMode.Ignore};
  gridBoardTitleLabel.AddToClassList("ps-gboard-panel-title");
  panel.Add(gridBoardTitleLabel);
  gridBoardStatusLabel=new Label(""){pickingMode=PickingMode.Ignore};
  gridBoardStatusLabel.AddToClassList("ps-gboard-panel-status");
  panel.Add(gridBoardStatusLabel);
  gridBoardBodyLabel=new Label(""){pickingMode=PickingMode.Ignore};
  gridBoardBodyLabel.AddToClassList("ps-gboard-panel-body");
  panel.Add(gridBoardBodyLabel);
  gridBoardHintLabel=new Label(""){pickingMode=PickingMode.Ignore};
  gridBoardHintLabel.AddToClassList("ps-gboard-panel-hint");
  panel.Add(gridBoardHintLabel);
  gridBoardConsumablesRoot=Container("ps-gboard-consumables");
  gridBoardConsumablesRoot.style.display=DisplayStyle.None;
  panel.Add(gridBoardConsumablesRoot);

  gridBoardGateActions=Container("ps-gboard-gate");
  gridBoardGateActions.style.display=DisplayStyle.None;

  // Path-only contextual controls (hidden until a route is being drawn).
  gridBoardContextActions=Container("ps-gboard-context");
  gridBoardDirRow=Container("ps-gboard-dirs");
  gridBoardDirRow.Add(MakeDirButton("↑",BoardUp));
  gridBoardDirRow.Add(MakeDirButton("←",BoardLeft));
  gridBoardDirRow.Add(MakeDirButton("→",BoardRight));
  gridBoardDirRow.Add(MakeDirButton("↓",BoardDown));
  gridBoardContextActions.Add(gridBoardDirRow);
  var pathActions=Container("ps-gboard-actions");
  pathActions.Add(MakeGridAction("一手戻す",()=>{
   if(GridBoardSystem.UndoSegment(run,out var msg))ShowToast(msg);
   else ShowToast(msg);
   RefreshGridBoard();
  }));
  pathActions.Add(MakeGridAction("ルートやめる",()=>{
   GridBoardSystem.ClearPath(run);
   RefreshGridBoard();
  }));
  pathActions.Add(MakeGridAction("導線を進む",()=>{
   if(GridBoardSystem.BeginRun(run,out var msg))ShowToast(msg);
   else ShowToast(msg);
   RefreshGridBoard();
  }));
  gridBoardContextActions.Add(pathActions);
  panel.Add(gridBoardContextActions);
  dock.Add(panel);
  dock.RegisterCallback<PointerEnterEvent>(_=>SetGridBoardDockHover(true));
  dock.RegisterCallback<PointerLeaveEvent>(_=>SetGridBoardDockHover(false));

  gridBoardPlayerHud=Container("ps-gboard-player-hud");
  gridBoardPlayerHud.pickingMode=PickingMode.Ignore;
  var playerHudOrnamentClip=Container("ps-gboard-player-hud-ornament-clip");
  var playerHudOrnament=Container("ps-gboard-player-hud-ornament");
  if(gridBoardPlayerHudArt!=null){
   playerHudOrnament.style.backgroundImage=new StyleBackground(gridBoardPlayerHudArt);
   playerHudOrnament.style.unityBackgroundScaleMode=ScaleMode.ScaleToFit;
  }
  playerHudOrnamentClip.Add(playerHudOrnament);
  gridBoardPlayerHud.Add(playerHudOrnamentClip);
  var hero=Container("ps-gboard-hero");
  var heroRow=Container("ps-gboard-hero-row");
  gridBoardPortraitHost=Container("ps-gboard-portrait");
  var character=CharacterSystem.OfRun(game.UiRun);
  if(character!=null){
   var portraitImage=CharacterPortraitFront(character,"ps-gboard-portrait-image");
   // The source is a full-body cutout. Crop its upper square so the compact
   // diamond HUD reads as a portrait instead of a miniature character.
   if(portraitImage is Image portrait){
    portrait.uv=new Rect(0.04f,0.58f,0.92f,0.42f);
    portrait.scaleMode=ScaleMode.ScaleAndCrop;
   }
   gridBoardPortraitHost.Add(portraitImage);
  }
  else{
   var blank=Container("ps-gboard-portrait-blank");
   blank.pickingMode=PickingMode.Ignore;
   gridBoardPortraitHost.Add(blank);
  }
  heroRow.Add(gridBoardPortraitHost);
  var heroMeta=Container("ps-gboard-hero-meta");
  gridBoardHeroNameLabel=new Label(character?.name??"探索者"){pickingMode=PickingMode.Ignore};
  gridBoardHeroNameLabel.AddToClassList("ps-gboard-hero-name");
  heroMeta.Add(gridBoardHeroNameLabel);
  gridBoardHpLabel=new Label(""){pickingMode=PickingMode.Ignore};
  gridBoardHpLabel.AddToClassList("ps-gboard-hp-label");
  heroMeta.Add(gridBoardHpLabel);
  var hpTrack=Container("ps-gboard-hp-track");
  gridBoardHpFill=Container("ps-gboard-hp-fill");
  hpTrack.Add(gridBoardHpFill);
  heroMeta.Add(hpTrack);
  gridBoardShieldLabel=new Label(""){pickingMode=PickingMode.Ignore};
  gridBoardShieldLabel.AddToClassList("ps-gboard-shield-label");
  heroMeta.Add(gridBoardShieldLabel);
  var shieldTrack=Container("ps-gboard-shield-track");
  gridBoardShieldFill=Container("ps-gboard-shield-fill");
  shieldTrack.Add(gridBoardShieldFill);
  heroMeta.Add(shieldTrack);
  heroRow.Add(heroMeta);
  hero.Add(heroRow);
  // The top-edge reveal strip owns header hover. The portrait itself should
  // never reserve an invisible click-blocking rectangle over the map.
  hero.pickingMode=PickingMode.Ignore;
  gridBoardRoot.Add(dock);
  // Vital information never moves between exploration and combat.
  gridBoardPlayerHud.Add(hero);
  gridBoardRoot.Add(gridBoardPlayerHud);

  // Path drawing gets its own compact board overlay. These are deliberately
  // separate from the retired exploration dossier so they remain reachable
  // while the whole map stays visible.
  gridBoardRoutePalette=Container("ps-gboard-route-palette");
  gridBoardRoutePalette.style.display=DisplayStyle.None;
  var undoRoute=MakeGridAction("↶",()=>{
   if(GridBoardSystem.UndoSegment(run,out var msg))ShowToast(msg);
   else ShowToast(msg);
   RefreshGridBoard();
  });
  undoRoute.tooltip="一手戻す";
  undoRoute.AddToClassList("ps-gboard-route-icon");
  gridBoardRoutePalette.Add(undoRoute);
  var beginRoute=MakeGridAction("▶",()=>{
   if(GridBoardSystem.BeginRun(run,out var msg))ShowToast(msg);
   else ShowToast(msg);
   RefreshGridBoard();
  });
  beginRoute.tooltip="導線を進む";
  beginRoute.AddToClassList("ps-gboard-route-icon");
  beginRoute.AddToClassList("ps-gboard-route-start");
  gridBoardRoutePalette.Add(beginRoute);
  var clearRoute=MakeGridAction("×",()=>{
   GridBoardSystem.ClearPath(run);
   RefreshGridBoard();
  });
  clearRoute.tooltip="ルートをやめる";
  clearRoute.AddToClassList("ps-gboard-route-icon");
  clearRoute.AddToClassList("ps-gboard-route-cancel");
  gridBoardRoutePalette.Add(clearRoute);
  gridBoardRoot.Add(gridBoardRoutePalette);

  // The exploration hand is deliberately quiet: cards individually rise on
  // hover, rather than opening a full wall across the dungeon.
  gridBoardHandOpen=false;
  gridBoardHandRoot=Container("ps-battle-hand");
  gridBoardHandRoot.AddToClassList("ps-gboard-hand-fan");
  gridBoardHandRoot.RegisterCallback<PointerLeaveEvent>(_=>ClearGridHandFocus());
  SyncGridHandChrome();
  gridBoardRoot.Add(gridBoardHandRoot);

  // Shared explore/combat energy rail under the fan.
  gridBoardEnergyRail=Container("ps-gboard-en-rail");
  gridBoardEnergyLabel=new Label(""){pickingMode=PickingMode.Ignore};
  gridBoardEnergyLabel.AddToClassList("ps-gboard-en-label");
  gridBoardEnergyRail.Add(gridBoardEnergyLabel);
  gridBoardCombatRail=Container("ps-gboard-combat-rail");
  gridBoardCombatRail.style.display=DisplayStyle.None;
  gridBoardSkillButton=MakeGridAction("SKILL",()=>{
   if(game.UiBattle==null||battleInputLocked){ShowToast("戦闘中ではない");return;}
   if(!game.UiUseActiveSkill())ShowToast(game.UiMessage);
   RefreshGridBoard();
  });
  gridBoardSkillButton.AddToClassList("ps-gboard-skill");
  gridBoardCombatRail.Add(gridBoardSkillButton);
  gridBoardEndTurnButton=MakeGridAction("ターン終了",()=>{
   if(game.UiBattle==null||battleInputLocked){ShowToast("戦闘中ではない");return;}
   if(!game.UiEndBattleTurn())ShowToast(game.UiMessage);
   RefreshGridBoard();
  });
  gridBoardEndTurnButton.AddToClassList("ps-gboard-endturn");
  gridBoardCombatRail.Add(gridBoardEndTurnButton);
  gridBoardPlayerHud?.Add(gridBoardEnergyRail);
  // Battle actions need their own lower-right dock. Keeping them inside the
  // EN rail made both groups cramped and prevented independent composition.
  gridBoardRoot.Add(gridBoardCombatRail);

  // A stable home for future dice resolution. It sits above the combat hand so
  // card → roll → result reads without covering the grid.
  gridBoardResolveTray=Container("ps-gboard-resolve");
  gridBoardResolveTray.style.display=DisplayStyle.None;
  var resolveHead=Container("ps-gboard-resolve-head");
  gridBoardResolveFormula=new Label("DICE  /  READY"){pickingMode=PickingMode.Ignore};
  gridBoardResolveFormula.AddToClassList("ps-gboard-resolve-formula");
  resolveHead.Add(gridBoardResolveFormula);
  gridBoardResolveResult=new Label("—"){pickingMode=PickingMode.Ignore};
  gridBoardResolveResult.AddToClassList("ps-gboard-resolve-result");
  resolveHead.Add(gridBoardResolveResult);
  gridBoardResolveTray.Add(resolveHead);
  gridBoardResolveDice=Container("ps-gboard-resolve-dice");
  gridBoardResolveTray.Add(gridBoardResolveDice);
  if(gridBoardCombatActionView!=null)gridBoardCombatActionView.Add(gridBoardResolveTray);

  gridBoardSelectedHost=Container("ps-gboard-selected");
  gridBoardSelectedHost.style.display=DisplayStyle.None;
  gridBoardRoot.Add(gridBoardSelectedHost);
  gridBoardCellDetail=Container("ps-gboard-cell-detail");
  gridBoardCellDetail.pickingMode=PickingMode.Ignore;
  gridBoardCellDetail.style.display=DisplayStyle.None;
  gridBoardRoot.Add(gridBoardCellDetail);
  // Gate decisions must not live in the retired right dossier: that column is
  // hidden during exploration. Keep them at the screen root as a real modal.
  gridBoardRoot.Add(gridBoardGateActions);
  BuildGridBoardEventOverlay();

  RefreshGridBoard();
 }

 Button MakeGridAction(string label,System.Action onClick){
  var b=PackspireUiFactory.Button(label,onClick);
  b.AddToClassList("ps-gboard-action");
  return b;
 }

 Button MakeDirButton(string label,Vector2Int dir){
  var b=PackspireUiFactory.SecondaryActionButton(label,()=>{
   var board=game.UiGridBoard;
   if(board==null)return;
   if(!GridBoardSystem.EnsurePathMode(board))return;
   if(GridBoardSystem.TrySlide(board,dir,out var msg))ShowToast(msg);
   else if(!string.IsNullOrEmpty(msg))ShowToast(msg);
   RefreshGridBoard();
  });
  b.AddToClassList("ps-gboard-dir");
  b.userData=dir;
  return b;
 }

}
}
