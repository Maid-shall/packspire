using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire {
public sealed partial class PackspireUiFoundation {
 VisualElement gridBoardRoot,gridBoardGrid,gridBoardActorLayer,gridBoardHandRoot,gridBoardDirRow,gridBoardHpFill,gridBoardShieldFill,gridBoardPortraitHost;
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

 static long CellKey(int x,int y)=>((long)x<<16)| (uint)y;

 void BuildGridCells(GridBoardRunState run){
  gridBoardGrid.Clear();
  gridBoardCells.Clear();
  int n=run.size;
  gridBoardGrid.style.flexDirection=FlexDirection.Column;
  gridBoardGrid.style.flexGrow=0;
  gridBoardGrid.style.flexShrink=0;
  for(int y=0;y<n;y++){
   var row=Container("ps-gboard-row");
   row.style.flexGrow=0;
   row.style.flexShrink=0;
   for(int x=0;x<n;x++){
    int cx=x,cy=y;
    var cell=new Button(()=>OnGridCellClicked(cx,cy));
    cell.AddToClassList("ps-gboard-cell");
    cell.RegisterCallback<PointerEnterEvent>(_=>ShowGridCellDetail(cx,cy));
    cell.RegisterCallback<PointerLeaveEvent>(_=>HideGridCellDetail());
    cell.style.flexGrow=0;
    cell.style.flexShrink=0;
    cell.text="";
    // The face is deliberately separate from the logical cell.  Later the
    // board can contain voids and fogged cells without changing input/pathing.
    var cellFace=Container("ps-gboard-cell-face");
    cellFace.pickingMode=PickingMode.Ignore;
    cell.Add(cellFace);
    var cellSigil=new Label(""){pickingMode=PickingMode.Ignore};
    cellSigil.AddToClassList("ps-gboard-cell-sigil");
    cellSigil.name="sigil";
    cell.Add(cellSigil);
    var cellMark=new Label(""){pickingMode=PickingMode.Ignore};
    cellMark.AddToClassList("ps-gboard-cell-mark");
    cellMark.name="mark";
    cell.Add(cellMark);
    row.Add(cell);
    gridBoardCells[CellKey(x,y)]=cell;
   }
   gridBoardGrid.Add(row);
  }
  ApplyGridZoomVisual();
 }

void OnGridCellClicked(int x,int y){
  if(gridBoardDidPan||game.UiBattle!=null)return;
  HideGridCellDetail();
  var run=game.UiGridBoard;
  if(run==null)return;
  if(run.phase==GridBoardPhase.Place){
   if(!string.IsNullOrEmpty(run.selectedCardUid)){
    if(GridBoardSystem.TryPlace(run,x,y,out var msg))ShowToast(msg);
    else if(!string.IsNullOrEmpty(msg))ShowToast(msg);
   } else {
    if(GridBoardSystem.EnsurePathMode(run)
     &&GridBoardSystem.TrySlideToward(run,x,y,out var pathMsg)
     &&!string.IsNullOrEmpty(pathMsg))ShowToast(pathMsg);
    else if(!string.IsNullOrEmpty(run.message))ShowToast(run.message);
   }
  } else if(run.phase==GridBoardPhase.Path){
   if(GridBoardSystem.TrySlideToward(run,x,y,out var msg)&&!string.IsNullOrEmpty(msg))
    ShowToast(msg);
   else if(!string.IsNullOrEmpty(msg))ShowToast(msg);
  }
  RefreshGridBoard();
}

 void ShowGridCellDetail(int x,int y){
  var run=game.UiGridBoard;
  if(gridBoardCellDetail==null||run==null||gridBoardCombatMode||game.UiBattle!=null||
   !string.IsNullOrEmpty(run.selectedCardUid))return;
  var cell=GridBoardSystem.Cell(run,x,y);
  if(cell==null)return;
  (string tag,string title,string body,string action)=cell.terrain switch{
   "void"=>("VOID","奈落","足場のない裂け目。進入も術式の配置もできない。","盤面の外縁"),
   "blocked"=>("TERRAIN","瓦礫","崩れた障害地形。通行できない。曲がるための壁として扱える。","通行不可"),
   "start"=>("ORIGIN","侵入地点","この区画の探索開始地点。経路はここから伸びる。","現在地の基点"),
   _=>GridCellDetailCopy(cell,run)
  };
  gridBoardCellDetail.Clear();
  var eyebrow=new Label(tag){pickingMode=PickingMode.Ignore};
  eyebrow.AddToClassList("ps-gboard-cell-detail-eyebrow");
  gridBoardCellDetail.Add(eyebrow);
  var head=new Label(title){pickingMode=PickingMode.Ignore};
  head.AddToClassList("ps-gboard-cell-detail-title");
  gridBoardCellDetail.Add(head);
  var rule=new VisualElement{pickingMode=PickingMode.Ignore};
  rule.AddToClassList("ps-gboard-cell-detail-rule");
  gridBoardCellDetail.Add(rule);
  var description=new Label(body){pickingMode=PickingMode.Ignore};
  description.AddToClassList("ps-gboard-cell-detail-body");
  gridBoardCellDetail.Add(description);
  var footer=new Label(action){pickingMode=PickingMode.Ignore};
  footer.AddToClassList("ps-gboard-cell-detail-footer");
  gridBoardCellDetail.Add(footer);
  gridBoardCellDetail.style.display=DisplayStyle.Flex;
  gridBoardCellDetail.BringToFront();
 }

 (string tag,string title,string body,string action) GridCellDetailCopy(GridCellState cell,GridBoardRunState run){
  string growth=cell.grow>0?$"　成長 {cell.grow}/3":"";
  return cell.place switch{
   "lamp"=>("FORMULA","灯",$"安寧の灯。周囲を照らす術式面。通過ごとに育つ。{growth}","通過：成長を進める"),
   "fog"=>("FORMULA","霧",$"防護と攪乱の術式面。通過ごとに成熟へ近づく。{growth}","通過：成長を進める"),
   "seal"=>("FORMULA","封",$"通行を塞ぐ楔。経路を曲げるための壁として働く。{growth}","通行不可・曲がりの起点"),
   "enemy"=>("HOSTILE","敵影","接触すると同一画面で戦闘へ移行する。敵は後に盤面上を移動する可能性がある。","接触：戦闘開始"),
   "event"=>("ANOMALY","異変","正体の知れない現象。踏み込むと選択式のイベントが発生する。","接触：イベントを確認"),
   "next"=>("PASSAGE","次区画","次の区画への裂け目。到達後に進むか選べる。","到達：区画選択"),
   "return"=>("RETURN","帰還点","探索の戦果を持ち帰るための出口。","到達：帰還を確認"),
   _=>("FLOOR","石床","まだ何も刻まれていない石床。導線を伸ばすための余地。","左クリック：経路を選択")
  };
 }

 void HideGridCellDetail(){
  if(gridBoardCellDetail!=null)gridBoardCellDetail.style.display=DisplayStyle.None;
 }

 void EnterGridCombatMode(bool on){
  gridBoardCombatMode=on;
  if(gridBoardViewport!=null)
   // Keep the expedition board visible during an encounter. Combat is a state of
   // the same expedition, rather than a replacement screen.
   gridBoardViewport.style.display=DisplayStyle.Flex;
  if(gridBoardCombatStage!=null)
   gridBoardCombatStage.style.display=on?DisplayStyle.Flex:DisplayStyle.None;
  if(gridBoardRoot!=null)
   gridBoardRoot.EnableInClassList("ps-gboard-in-combat",on);
  if(on){
   gridBoardHandOpen=true;
  }
  ShowGridModeToast(on?"戦闘":"探索");
  SyncGridHandChrome();
 }

 void ShowGridModeToast(string mode){
  if(gridBoardModeToast==null)return;
  gridBoardModeToast.text=$"封印格子\n{mode}";
  gridBoardModeToast.RemoveFromClassList("ps-gboard-mode-toast-out");
  gridBoardModeToast.style.display=DisplayStyle.Flex;
  gridBoardModeToast.schedule.Execute(()=>gridBoardModeToast?.AddToClassList("ps-gboard-mode-toast-out")).StartingIn(1350);
  gridBoardModeToast.schedule.Execute(()=>{
   if(gridBoardModeToast!=null)gridBoardModeToast.style.display=DisplayStyle.None;
  }).StartingIn(1750);
 }

 void OnGridBoardPointerDown(PointerDownEvent evt){
  // Left click selects cells. The board can be grabbed with right or middle
  // mouse, and Space + left drag gives touchpad users an equivalent gesture.
  if(!IsGridBoardPanGesture(evt.button))return;
  if(!IsGridBoardPointerPosition(evt.position))return;
  var board=game.UiGridBoard;
  if(gridBoardCombatMode||(board!=null&&board.phase==GridBoardPhase.Run))return;
  if(gridBoardHandRoot!=null&&gridBoardHandRoot.resolvedStyle.display!=DisplayStyle.None
   &&gridBoardHandRoot.worldBound.Contains(evt.position))return;
  gridBoardPanning=true;
  gridBoardDidPan=false;
  gridBoardPointerStart=evt.position;
  gridBoardPanAtStart=gridBoardPan;
 }

 void OnGridBoardPointerMove(PointerMoveEvent evt){
  // Fallback for controls that report a held mouse button only on move.
  if(!gridBoardPanning&&IsGridBoardPanButtonsHeld(evt.pressedButtons)&&IsGridBoardPointerPosition(evt.position)){
   gridBoardPanning=true;
   gridBoardDidPan=false;
   gridBoardPointerStart=evt.position;
   gridBoardPanAtStart=gridBoardPan;
  }
  if(!gridBoardPanning)return;
  Vector2 delta=(Vector2)evt.position-gridBoardPointerStart;
  if(!gridBoardDidPan&&delta.sqrMagnitude>64f){
   gridBoardDidPan=true;
   gridBoardViewport.CapturePointer(evt.pointerId);
  }
  if(!gridBoardDidPan)return;
  gridBoardPan=gridBoardPanAtStart+delta;
  gridBoardLastLayoutPos=new(float.NaN,float.NaN);
  LayoutGridBoardMap();
  evt.StopPropagation();
 }

 void OnGridBoardPointerUp(PointerUpEvent evt){
  if(!gridBoardPanning)return;
  gridBoardPanning=false;
  if(gridBoardViewport!=null&&gridBoardViewport.HasPointerCapture(evt.pointerId))
   gridBoardViewport.ReleasePointer(evt.pointerId);
  gridBoardViewport?.schedule.Execute(()=>{gridBoardDidPan=false;}).ExecuteLater(1);
 }

 void OnGridBoardWheel(WheelEvent evt){
  if(!IsGridBoardPointerPosition(evt.mousePosition))return;
  var board=game.UiGridBoard;
  if(gridBoardCombatMode||(board!=null&&board.phase==GridBoardPhase.Run))return;
  if(Mathf.Abs(evt.delta.y)<0.01f)return;
  AdjustGridZoom(evt.delta.y>0f?-0.12f:0.12f);
  evt.StopPropagation();
 }

 bool IsGridBoardPointerPosition(Vector2 position){
  return gridBoardGrid!=null&&gridBoardGrid.worldBound.Contains(position);
 }

 bool IsGridBoardPanGesture(int button){
  return button==1||button==2||(button==0&&Input.GetKey(KeyCode.Space));
 }

 bool IsGridBoardPanButtonsHeld(int pressedButtons){
  bool rightOrMiddle=(pressedButtons&(1<<1))!=0||(pressedButtons&(1<<2))!=0;
  bool spaceLeft=Input.GetKey(KeyCode.Space)&&(pressedButtons&1)!=0;
  return rightOrMiddle||spaceLeft;
 }

 void OnGridViewportGeometryChanged(GeometryChangedEvent evt){
  if(Mathf.Approximately(evt.oldRect.width,evt.newRect.width)
   &&Mathf.Approximately(evt.oldRect.height,evt.newRect.height))return;
  QueueGridBoardLayout();
 }

 // GeometryChanged is raised while UI Toolkit is resolving layout.  Updating
 // child geometry inside that callback can schedule another layout immediately
 // (especially when the actor overlay follows the board), so defer it to the
 // next panel pass and coalesce repeated resize notifications.
 void QueueGridBoardLayout(){
  if(gridBoardLayoutQueued||gridBoardViewport==null)return;
  gridBoardLayoutQueued=true;
  gridBoardViewport.schedule.Execute(()=>{
   gridBoardLayoutQueued=false;
   LayoutGridBoardMap();
  }).ExecuteLater(0);
 }

 void AdjustGridZoom(float delta){
  float next=Mathf.Clamp(gridBoardZoom+delta,GridZoomMin,GridZoomMax);
  if(Mathf.Approximately(next,gridBoardZoom))return;
  gridBoardZoom=next;
  ApplyGridZoomVisual();
  gridBoardLastLayoutPos=new(float.NaN,float.NaN);
  LayoutGridBoardMap();
 }

 void ApplyGridZoomVisual(){
  var run=game.UiGridBoard;
  int n=run!=null?Mathf.Max(1,run.size):8;
  int px=Mathf.Max(28,Mathf.RoundToInt(GridCellBasePx*gridBoardZoom));
  float font=Mathf.Clamp(11f*gridBoardZoom,9f,18f);
  const int margin=1;
  int pitch=px+margin*2;
  int pad=16;
  int board=n*pitch+pad;
  if(gridBoardGrid!=null){
   gridBoardGrid.style.width=board;
   gridBoardGrid.style.height=board;
   gridBoardGrid.style.minWidth=board;
   gridBoardGrid.style.minHeight=board;
   gridBoardGrid.style.maxWidth=board;
   gridBoardGrid.style.maxHeight=board;
   foreach(var row in gridBoardGrid.Children()){
    row.style.width=n*pitch;
    row.style.height=pitch;
    row.style.minHeight=pitch;
    row.style.maxHeight=pitch;
    row.style.flexGrow=0;
    row.style.flexShrink=0;
   }
  }
  foreach(var kv in gridBoardCells){
   var ve=kv.Value;
   if(ve==null)continue;
   ve.style.width=px;
   ve.style.height=px;
   ve.style.minWidth=px;
   ve.style.minHeight=px;
   ve.style.maxWidth=px;
   ve.style.maxHeight=px;
   ve.style.marginTop=margin;
   ve.style.marginBottom=margin;
   ve.style.marginLeft=margin;
   ve.style.marginRight=margin;
   ve.style.flexGrow=0;
   ve.style.flexShrink=0;
   var cellMark=ve.Q<Label>("mark");
   if(cellMark!=null)cellMark.style.fontSize=font;
  }
 }

 void LayoutGridBoardMap(){
  if(gridBoardLayoutBusy||gridBoardViewport==null||gridBoardGrid==null)return;
  var vr=gridBoardViewport.contentRect;
  if(vr.width<8f||vr.height<8f)return;
  var run=game.UiGridBoard;
  int n=run!=null?Mathf.Max(1,run.size):8;
 int px=Mathf.Max(28,Mathf.RoundToInt(GridCellBasePx*gridBoardZoom));
 const int margin=1;
 int pitch=px+margin*2;
 float gw=n*pitch+16f;
 float gh=n*pitch+16f;
 // Exploration is a tableau while planning. Once the route resolves, the
 // camera locks onto the explorer so movement reads as traversal, not as a
 // token sliding across a static board.
 bool followExplorer=!gridBoardCombatMode&&run!=null&&run.phase==GridBoardPhase.Run;
 float left;
 float top;
 if(followExplorer){
  var piece=GridBoardSystem.PieceVisual(run);
  float actorX=8f+(piece.x+0.5f)*pitch;
  float actorY=8f+(piece.y+0.5f)*pitch;
  left=vr.width*0.5f-actorX;
  top=vr.height*0.53f-actorY;
 } else {
  left=(vr.width-gw)*0.5f+gridBoardPan.x;
  top=(vr.height-gh)*0.5f+gridBoardPan.y;
 }
  if(!float.IsNaN(gridBoardLastLayoutPos.x)
   &&Mathf.Abs(gridBoardLastLayoutPos.x-left)<0.5f
   &&Mathf.Abs(gridBoardLastLayoutPos.y-top)<0.5f)return;
  gridBoardLayoutBusy=true;
  gridBoardLastLayoutPos=new Vector2(left,top);
  gridBoardGrid.style.position=Position.Absolute;
  gridBoardGrid.style.left=left;
  gridBoardGrid.style.top=top;
  LayoutGridActors(left,top,pitch);
  gridBoardLayoutBusy=false;
 }

 void RefreshGridActors(GridBoardRunState run){
  if(gridBoardActorLayer==null||run==null)return;
  gridBoardActorLayer.Clear();
  // The explorer is an actor too, rather than a terrain decoration.  That
  // keeps future movement, hit reactions and facing animation independent of
  // the logical grid cell.
  var piece=GridBoardSystem.PieceVisual(run);
  var hero=Container("ps-gboard-actor ps-gboard-player-actor");
  hero.pickingMode=PickingMode.Ignore;
  hero.userData=new Vector2Int(Mathf.RoundToInt(piece.x),Mathf.RoundToInt(piece.y));
  var character=CharacterSystem.OfRun(game.UiRun);
  if(character!=null)
   hero.Add(CharacterPortraitFront(character,"ps-gboard-player-portrait"));
  else{
   var glyph=new Label("★"){pickingMode=PickingMode.Ignore};
   glyph.AddToClassList("ps-gboard-actor-glyph");
   hero.Add(glyph);
  }
  gridBoardActorLayer.Add(hero);
  // Enemy cells are spawn anchors only. The visible token deliberately lives
  // here so patrols, knockback and multi-cell enemies can move independently.
  foreach(var cell in run.cells.Where(c=>c.place=="enemy")){
   var actor=Container("ps-gboard-actor ps-gboard-enemy-actor");
   actor.pickingMode=PickingMode.Ignore;
   actor.userData=new Vector2Int(cell.x,cell.y);
   var glyph=new Label("⚔"){pickingMode=PickingMode.Ignore};
   glyph.AddToClassList("ps-gboard-actor-glyph");
   actor.Add(glyph);
   gridBoardActorLayer.Add(actor);
  }
  if(!float.IsNaN(gridBoardLastLayoutPos.x))
   LayoutGridActors(gridBoardLastLayoutPos.x,gridBoardLastLayoutPos.y,
    Mathf.Max(28,Mathf.RoundToInt(GridCellBasePx*gridBoardZoom))+2);
 }

 void LayoutGridActors(float boardLeft,float boardTop,int pitch){
  if(gridBoardActorLayer==null)return;
  gridBoardActorLayer.style.position=Position.Absolute;
  gridBoardActorLayer.style.left=boardLeft;
  gridBoardActorLayer.style.top=boardTop;
  gridBoardActorLayer.style.width=gridBoardGrid?.resolvedStyle.width??0;
  gridBoardActorLayer.style.height=gridBoardGrid?.resolvedStyle.height??0;
  foreach(var actor in gridBoardActorLayer.Children()){
   if(actor.userData is not Vector2Int pos)continue;
   actor.style.left=9+pos.x*pitch;
   actor.style.top=9+pos.y*pitch;
   actor.style.width=pitch-2;
   actor.style.height=pitch-2;
  }
 }

 void SetGridBoardMapHover(bool hover){
  gridBoardMapHover=hover;
  SyncGridBoardHoverChrome();
 }

 void SetGridBoardDockHover(bool hover){
  gridBoardDockHover=hover;
  SyncGridBoardHoverChrome();
 }

 void SetGridBoardHeaderHover(bool hover){
  gridBoardHeaderHover=hover;
  SyncGridBoardHoverChrome();
 }

 void SyncGridBoardHoverChrome(){
  if(gridBoardRoot==null)return;
  gridBoardRoot.EnableInClassList("ps-gboard-inspecting",gridBoardMapHover||gridBoardDockHover);
  gridBoardRoot.EnableInClassList("ps-gboard-header-hover",gridBoardHeaderHover);
 }

 void RefreshGridBoard(){
  var run=game.UiGridBoard;
  if(!gridBoardBuilt||run==null||gridBoardGrid==null)return;

  // Keep stage mode in sync with live battle state (no ScreenId.Battle hop).
  bool inBattle=game.UiBattle!=null;
  if(inBattle&&!gridBoardCombatMode)EnterGridCombatMode(true);
  else if(!inBattle&&gridBoardCombatMode)EnterGridCombatMode(false);

  // Exploration HUD is progressive disclosure. The map remains unobstructed
  // while idle; selecting a card or beginning a route brings in EN and the
  // reading rails needed to make the decision.
  bool planning=inBattle||!string.IsNullOrEmpty(run.pendingGate)||
   !string.IsNullOrEmpty(run.selectedCardUid)||
   run.phase==GridBoardPhase.Path||run.phase==GridBoardPhase.Run;
  gridBoardRoot.EnableInClassList("ps-gboard-planning",planning);
  gridBoardRoot.EnableInClassList("ps-gboard-idle",!planning);

  string phase=GridBoardSystem.PhaseLabel(run.phase);
  string ink=run.phase==GridBoardPhase.Path||run.phase==GridBoardPhase.Run||run.phase==GridBoardPhase.Done
   ?$"曲がり　{run.turnsUsed}/{run.turnsMax}　·　長さ {Mathf.Max(0,run.path.Count-1)}"
   :$"曲がり上限　{run.turnsMax}";
  string hint=string.IsNullOrEmpty(run.message)
   ?"カードを置いてから、マスや矢印で導線を引く。"
   :run.message;

  if(gridBoardPhaseLabel!=null)gridBoardPhaseLabel.text=inBattle?"戦闘":phase;
  if(gridBoardInkLabel!=null)
   gridBoardInkLabel.text=inBattle?$"勝利数 {game.UiRun?.battlesWon??0}":$"{GridBoardSystem.AreaLabel(run)}　·　{ink}　·　{GridBoardSystem.GrowthSummary(run)}";
  if(gridBoardDoomLabel!=null)
   gridBoardDoomLabel.text=inBattle
    ?$"◆ ROUND {Mathf.Max(1,(game.UiBattle?.move??0)+1):00}"
    :$"◆ TURN {Mathf.Max(1,run.doom+1):00}";
  if(gridBoardAreaChipLabel!=null){
   gridBoardAreaChipLabel.text=$"◇ 区画 {run.areaIndex+1}/{Mathf.Max(1,run.areaCount)}";
   gridBoardAreaChipLabel.style.display=inBattle?DisplayStyle.None:DisplayStyle.Flex;
  }
  if(gridBoardCurveChipLabel!=null){
   gridBoardCurveChipLabel.text=$"✦ 曲がり {run.turnsUsed}/{Mathf.Max(1,run.turnsMax)}";
   gridBoardCurveChipLabel.style.display=inBattle?DisplayStyle.None:DisplayStyle.Flex;
  }
  gridBoardMapStats?.EnableInClassList("ps-gboard-map-stats-combat",inBattle);
  if(gridBoardTypeLabel!=null)gridBoardTypeLabel.text=inBattle?"戦闘":"封印格子";
  if(gridBoardTitleLabel!=null)
   gridBoardTitleLabel.text=inBattle?(game.UiBattle.enemy?.name??"交戦中"):(string.IsNullOrEmpty(run.pendingGate)?phase:run.pendingGate=="next"?"次区画":"帰還点");
  if(gridBoardStatusLabel!=null)
   gridBoardStatusLabel.text=inBattle?"戦闘中":(string.IsNullOrEmpty(run.pendingGate)?ink:GridBoardSystem.AreaLabel(run));

  if(inBattle){
   RefreshGridCombatStage();
   if(gridBoardBodyLabel!=null)gridBoardBodyLabel.text=DescribeGridBattleIntentHint();
   if(gridBoardHintLabel!=null)gridBoardHintLabel.text="下の扇からカード。EN / SKILL / END TURN は扇の下。";
   if(gridBoardContextActions!=null)gridBoardContextActions.style.display=DisplayStyle.None;
  } else if(!string.IsNullOrEmpty(run.pendingGate)){
   if(gridBoardBodyLabel!=null)
    gridBoardBodyLabel.text=run.pendingGate=="next"
     ?"裂け目の向こうに次の区画がある。進出するか、まだここで探索するか。"
     :"ここから持ち帰れる。帰還するか、まだ探索を続けるか。";
   if(gridBoardHintLabel!=null)
    gridBoardHintLabel.text="下の選択で決める。マス自体は残るので、あとからでも踏める。";
  } else {
   if(gridBoardBodyLabel!=null)gridBoardBodyLabel.text=hint;
   if(gridBoardHintLabel!=null){
    gridBoardHintLabel.text=run.phase switch{
     GridBoardPhase.Place=>"右下のカードを選びマスへ。カードなしでマスを押すと導線開始。ルート終端で手札・EN補充。",
     GridBoardPhase.Path=>"矢印か同じ行／列のマスで壁まで滑走。",
     GridBoardPhase.Run=>"進行中。敵＝戦闘、異＝イベント、次＝次区画、帰＝帰還。",
     _=>"撤退で拠点へ。帰還点からの帰還が正式な持ち帰り。",
    };
   }
  }

  RefreshGridHero();
  RefreshGridEnergyRail(run);
  RefreshGridConsumables();
  RefreshGridGateChoice(run);

  bool showPathContext=!inBattle&&string.IsNullOrEmpty(run.pendingGate)&&run.phase==GridBoardPhase.Path;
  if(gridBoardRoutePalette!=null)
   gridBoardRoutePalette.style.display=showPathContext?DisplayStyle.Flex:DisplayStyle.None;
  if(gridBoardContextActions!=null)
   gridBoardContextActions.style.display=showPathContext?DisplayStyle.Flex:DisplayStyle.None;

  if(gridBoardDirRow!=null){
   bool showDirs=showPathContext;
   gridBoardDirRow.style.display=showDirs?DisplayStyle.Flex:DisplayStyle.None;
   if(showDirs){
    foreach(var child in gridBoardDirRow.Children()){
     if(child is Button btn&&btn.userData is Vector2Int dir){
      bool ok=GridBoardSystem.CanSlide(run,dir);
      btn.SetEnabled(ok);
      btn.EnableInClassList("ps-gboard-dir-hot",ok);
     }
    }
   }
  }

  var piece=GridBoardSystem.PieceVisual(run);
  int pieceX=Mathf.RoundToInt(piece.x);
  int pieceY=Mathf.RoundToInt(piece.y);
  var pathSet=new HashSet<long>();
  for(int i=0;i<run.path.Count;i++)pathSet.Add(CellKey(run.path[i].x,run.path[i].y));

  var preview=new HashSet<long>();
  if(run.phase==GridBoardPhase.Path){
   foreach(var dir in new[]{BoardUp,BoardDown,BoardLeft,BoardRight}){
    foreach(var p in GridBoardSystem.SlidePreview(run,dir))
     preview.Add(CellKey(p.x,p.y));
   }
  }

  foreach(var cell in run.cells){
   if(!gridBoardCells.TryGetValue(CellKey(cell.x,cell.y),out var ve)||ve==null)continue;
   ve.EnableInClassList("ps-gboard-cell",true);
   ve.EnableInClassList("ps-gboard-void",cell.terrain=="void");
   ve.EnableInClassList("ps-gboard-blocked",cell.terrain=="blocked");
   ve.EnableInClassList("ps-gboard-start",cell.terrain=="start");
   ve.EnableInClassList("ps-gboard-goal",cell.terrain=="goal");
   ve.EnableInClassList("ps-gboard-floor",cell.terrain=="floor");
   ve.EnableInClassList("ps-gboard-lamp",cell.place=="lamp");
   ve.EnableInClassList("ps-gboard-fog",cell.place=="fog");
   ve.EnableInClassList("ps-gboard-seal",cell.place=="seal");
   ve.EnableInClassList("ps-gboard-enemy",cell.place=="enemy");
   ve.EnableInClassList("ps-gboard-event",cell.place=="event");
   ve.EnableInClassList("ps-gboard-next",cell.place=="next");
   ve.EnableInClassList("ps-gboard-return",cell.place=="return");
   ve.EnableInClassList("ps-gboard-path",pathSet.Contains(CellKey(cell.x,cell.y)));
   ve.EnableInClassList("ps-gboard-preview",preview.Contains(CellKey(cell.x,cell.y))&&!pathSet.Contains(CellKey(cell.x,cell.y)));
   ve.EnableInClassList("ps-gboard-piece",cell.x==pieceX&&cell.y==pieceY);
   ve.EnableInClassList("ps-gboard-goal",false);

   var cellMark=ve.Q<Label>("mark");
   var cellSigil=ve.Q<Label>("sigil");
   if(cellSigil!=null){
    cellSigil.text=cell.terrain switch{
     "blocked"=>"✦",
     "start"=>"◈",
     "goal"=>"✧",
     _=>cell.place switch{
      "lamp"=>"✦", "fog"=>"☾", "seal"=>"◇",
      "event"=>"✧", "next"=>"➜", "return"=>"↶", _=>""
     }
    };
   }
   if(cellMark!=null){
    string t=cell.terrain switch{
     "start"=>"入",
     "goal"=>"標",
     "blocked"=>"■",
     _=>GridBoardSystem.PlaceLabel(cell.place),
    };
     if(cell.grow>0&&(cell.place is "lamp" or "fog" or "seal"))t=$"{GridBoardSystem.PlaceLabel(cell.place)}{cell.grow}";
    if(cell.x==pieceX&&cell.y==pieceY)t=string.IsNullOrEmpty(t)?"●":t+"●";
    cellMark.text=t??"";
   }
  }

  RefreshGridActors(run);

  bool shouldFollow=!inBattle&&run.phase==GridBoardPhase.Run;
  if(shouldFollow!=gridBoardFollowingExplorer){
   gridBoardFollowingExplorer=shouldFollow;
   gridBoardZoom=shouldFollow?1.28f:1f;
   gridBoardPan=Vector2.zero;
   gridBoardLastLayoutPos=new(float.NaN,float.NaN);
  }
  ApplyGridZoomVisual();
  LayoutGridBoardMap();
  RebuildGridHand(run);
  RefreshGridSelectedCard(run);
  RefreshGridResolveTray(inBattle);
 }

 void RefreshGridHero(){
  var runState=game.UiRun;
  var character=CharacterSystem.OfRun(runState);
  if(gridBoardHeroNameLabel!=null)
   gridBoardHeroNameLabel.text=character?.name??"探索者";
  if(runState==null){
   if(gridBoardHpLabel!=null)gridBoardHpLabel.text="体力 —";
   if(gridBoardHpFill!=null)gridBoardHpFill.style.width=Length.Percent(0);
   if(gridBoardHpFill!=null)gridBoardHpFill.style.height=Length.Percent(0);
   if(gridBoardShieldLabel!=null)gridBoardShieldLabel.text="SH 0";
   if(gridBoardShieldFill!=null)gridBoardShieldFill.style.height=Length.Percent(0);
   return;
  }
  if(gridBoardHpLabel!=null)
   gridBoardHpLabel.text=$"HP {runState.hp}/{runState.maxHp}";
  if(gridBoardHpFill!=null){
   float t=runState.maxHp>0?Mathf.Clamp01((float)runState.hp/runState.maxHp):0f;
   gridBoardHpFill.style.width=Length.Percent(100);
   gridBoardHpFill.style.height=Length.Percent(t*100f);
  }
  if(gridBoardShieldLabel!=null)gridBoardShieldLabel.text=$"SH {Mathf.Max(0,runState.block)}";
  if(gridBoardShieldFill!=null){
   float shieldT=runState.maxHp>0?Mathf.Clamp01((float)runState.block/runState.maxHp):0f;
   gridBoardShieldFill.style.height=Length.Percent(shieldT*100f);
  }
 }

 void RefreshGridResolveTray(bool inBattle){
  if(gridBoardResolveTray==null)return;
  bool show=inBattle&&gridBoardDiceResultActive&&Time.unscaledTime<gridBoardDiceResultUntil;
  gridBoardCombatActionView?.EnableInClassList("ps-gboard-action-view-rolling",show);
  if(gridBoardCombatCardPreview!=null)
   gridBoardCombatCardPreview.style.display=show?DisplayStyle.None:DisplayStyle.Flex;
  gridBoardResolveTray.style.display=show?DisplayStyle.Flex:DisplayStyle.None;
  if(!show)return;
  if(gridBoardResolveFormula!=null)
   gridBoardResolveFormula.text=$"{gridBoardDiceSource}  /  2D6 {GridDiceModifierText(gridBoardDiceModifier)}";
  if(gridBoardResolveResult!=null)
   gridBoardResolveResult.text=$"{gridBoardDiceTotal} → {gridBoardDiceDamage} DMG";
  if(gridBoardResolveDice==null)return;
  gridBoardResolveDice.Clear();
  foreach(int value in new[]{gridBoardDieOne,gridBoardDieTwo}){
   var die=new Label(value.ToString()){pickingMode=PickingMode.Ignore};
   die.AddToClassList("ps-gboard-die");
   gridBoardResolveDice.Add(die);
  }
  var modifier=new Label(GridDiceModifierText(gridBoardDiceModifier)){pickingMode=PickingMode.Ignore};
  modifier.AddToClassList("ps-gboard-die-mod");
  gridBoardResolveDice.Add(modifier);
 }

 static string GridDiceModifierText(int modifier)=>modifier>0?$"+ {modifier}":modifier<0?$"− {Mathf.Abs(modifier)}":"+ 0";

 void ShowGridDiceResult(BattleActionFx fx){
  if(!gridBoardCombatMode||fx.dieOne<=0||fx.dieTwo<=0)return;
  gridBoardDiceResultActive=true;
  gridBoardDiceResultUntil=Time.unscaledTime+2.4f;
  gridBoardDieOne=fx.dieOne;
  gridBoardDieTwo=fx.dieTwo;
  gridBoardDiceModifier=fx.damageModifier;
  gridBoardDiceTotal=fx.rolledDamage;
  gridBoardDiceDamage=fx.damageToEnemy>0?fx.damageToEnemy:fx.damageToPlayer;
  gridBoardDiceSource=string.IsNullOrEmpty(fx.cardName)?"DAMAGE ROLL":fx.cardName;
  RefreshGridResolveTray(true);
  gridBoardResolveTray?.BringToFront();
  gridBoardResolveTray?.schedule.Execute(()=>RefreshGridResolveTray(game.UiBattle!=null)).StartingIn(2450);
 }

 void SyncGridHandChrome(){
  if(gridBoardHandRoot==null)return;
  bool open=gridBoardHandOpen||gridBoardCombatMode;
  gridBoardHandRoot.EnableInClassList("ps-gboard-hand-open",open);
  gridBoardHandRoot.style.left=StyleKeyword.Auto;
  gridBoardHandRoot.style.right=StyleKeyword.Auto;
  gridBoardHandRoot.style.left=Length.Percent(58);
  gridBoardHandRoot.style.marginLeft=-GridHandWidth*0.5f;
  gridBoardHandRoot.style.bottom=gridBoardCombatMode?-72:(open?54:0);
  gridBoardHandRoot.style.width=GridHandWidth;
  // The visible cards stay fixed inside this compact hit strip. The separate
  // hover preview is picking-disabled, so it cannot feed back into layout.
  gridBoardHandRoot.style.height=gridBoardCombatMode?260:(open?344:96);
  gridBoardHandRoot.style.overflow=Overflow.Visible;
  gridBoardHandRoot.style.backgroundColor=Color.clear;
  gridBoardHandRoot.style.borderLeftWidth=0;
  gridBoardHandRoot.style.borderRightWidth=0;
  gridBoardHandRoot.style.borderTopWidth=0;
  gridBoardHandRoot.style.borderBottomWidth=0;
 }

 void RefreshGridEnergyRail(GridBoardRunState board){
  if(gridBoardEnergyLabel==null)return;
  int en,max;
  if(gridBoardCombatMode&&game.UiRun!=null){
   en=game.UiRun.energy;
   max=3;
  } else {
   en=board?.energy??0;
   max=board!=null?Mathf.Max(1,board.energyMax):3;
  }
  var orbs=new System.Text.StringBuilder("EN ");
  for(int i=0;i<max;i++)orbs.Append(i<en?'●':'○');
  orbs.Append($"  {en}/{max}");
  gridBoardEnergyLabel.text=$"EN      {en}/{max}";
  if(gridBoardCombatRail!=null)
   gridBoardCombatRail.style.display=gridBoardCombatMode?DisplayStyle.Flex:DisplayStyle.None;
  if(gridBoardSkillButton!=null){
   gridBoardSkillButton.text=game.UiActiveSkillAvailable?game.UiActiveSkillLabel:"USED";
   gridBoardSkillButton.tooltip=game.UiActiveSkillTooltip;
   gridBoardSkillButton.SetEnabled(gridBoardCombatMode&&game.UiActiveSkillAvailable&&!battleInputLocked);
  }
  if(gridBoardEndTurnButton!=null){
   gridBoardEndTurnButton.tooltip="手札を捨て、敵の次の行動を解決する";
   gridBoardEndTurnButton.SetEnabled(gridBoardCombatMode&&game.UiBattle!=null&&!battleInputLocked);
  }
 }

 void RefreshGridGateChoice(GridBoardRunState run){
  if(gridBoardGateActions==null)return;
  gridBoardGateActions.Clear();
  if(run==null||string.IsNullOrEmpty(run.pendingGate)||gridBoardCombatMode){
   gridBoardGateActions.style.display=DisplayStyle.None;
   return;
  }
  gridBoardGateActions.style.display=DisplayStyle.Flex;
  bool next=run.pendingGate=="next";
  gridBoardGateActions.EnableInClassList("ps-gboard-gate-next",next);
  gridBoardGateActions.EnableInClassList("ps-gboard-gate-return",!next);
  var dialog=Container("ps-gboard-gate-dialog");
  var eyebrow=new Label(next?"AREA PASSAGE":"EXPEDITION RETURN"){pickingMode=PickingMode.Ignore};
  eyebrow.AddToClassList("ps-gboard-gate-eyebrow");
  dialog.Add(eyebrow);
  var head=new Label(next?"次区画への裂け目":"帰還点"){pickingMode=PickingMode.Ignore};
  head.AddToClassList("ps-gboard-gate-title");
  dialog.Add(head);
  var body=new Label(next
   ?"裂け目の向こうへ進むと、この区画には戻れない。"
   :"ここまでの戦利品を持ち帰り、遠征を終了する。"){pickingMode=PickingMode.Ignore};
  body.AddToClassList("ps-gboard-gate-body");
  dialog.Add(body);
  var choices=Container("ps-gboard-gate-choices");
  if(next){
   choices.Add(MakeGridAction("次の区画へ進む",()=>{
    game.UiAdvanceGridArea();
   }));
  } else {
   choices.Add(MakeGridAction("戦利品を持って帰還する",()=>{
    game.UiConfirmGridReturn();
    ForceRefreshScreen();
   }));
  }
  choices.Add(MakeGridAction("まだ探索する",()=>{
   game.UiDeclineGridGate();
   RefreshGridBoard();
  }));
  dialog.Add(choices);
  gridBoardGateActions.Add(dialog);
  gridBoardGateActions.BringToFront();
 }

 void RefreshGridConsumables(){
  if(gridBoardConsumablesRoot==null)return;
  gridBoardConsumablesRoot.Clear();
  var run=game.UiRun;
  if(!gridBoardCombatMode||run==null||run.consumables==null||run.consumables.Count==0){
   gridBoardConsumablesRoot.style.display=DisplayStyle.None;
   return;
  }
  gridBoardConsumablesRoot.style.display=DisplayStyle.Flex;
  for(int i=0;i<run.consumables.Count;i++){
   int index=i;
   string id=run.consumables[index];
   string label=ConsumableSystem.Name(id);
   var button=MakeGridAction(label,()=>{
    if(battleInputLocked||game.UiBattle==null)return;
    if(!game.UiUseBattleConsumable(index))ShowToast(game.UiMessage);
    RefreshGridBoard();
   });
   button.AddToClassList("ps-gboard-consumable");
   gridBoardConsumablesRoot.Add(button);
  }
 }

 void RefreshGridCombatStage(){
  var battle=game.UiBattle;
  if(battle?.enemy==null)return;
  if(gridBoardCombatTitle!=null)gridBoardCombatTitle.text=battle.enemy.name;
  if(gridBoardCombatHpLabel!=null)
   gridBoardCombatHpLabel.text=$"HP {Mathf.Max(0,battle.enemyHp)} / {battle.enemyMaxHp}";
  if(gridBoardCombatHpFill!=null){
   float ratio=battle.enemyMaxHp>0?Mathf.Clamp01((float)battle.enemyHp/battle.enemyMaxHp):0f;
   gridBoardCombatHpFill.style.width=Length.Percent(ratio*100f);
  }
  if(gridBoardCombatShieldLabel!=null)
   gridBoardCombatShieldLabel.text=$"SH {Mathf.Max(0,battle.enemyBlock)}";
  if(gridBoardCombatShieldFill!=null){
   float shieldRatio=battle.enemyMaxHp>0?Mathf.Clamp01((float)Mathf.Max(0,battle.enemyBlock)/battle.enemyMaxHp):0f;
   gridBoardCombatShieldFill.style.width=Length.Percent(shieldRatio*100f);
  }
  RefreshGridCombatEnemyStatuses(battle.enemyStatuses);
  if(gridBoardCombatIntentLabel!=null)
   gridBoardCombatIntentLabel.text=DescribeGridBattleIntentShort();
  if(gridBoardCombatIntentHintLabel!=null)
   gridBoardCombatIntentHintLabel.text=DescribeGridBattleIntentHint();
  if(gridBoardCombatPortrait!=null){
   if(PackspireGame.LockBattleShowcaseArt&&game.UiShowcaseDragonArt!=null){
    gridBoardCombatPortrait.image=game.UiShowcaseDragonArt;
    gridBoardCombatPortrait.uv=new Rect(0,0,1,1);
   } else if(battle.enemy.HasPortraitAsset){
    gridBoardCombatPortrait.image=game.ResolveEnemyPortrait(battle.enemy);
    gridBoardCombatPortrait.uv=new Rect(0,0,1,1);
   } else {
    gridBoardCombatPortrait.image=game.UiEnemyArt;
    gridBoardCombatPortrait.uv=EnemyUv(battle.enemy.id);
   }
   gridBoardCombatPortrait.style.display=DisplayStyle.Flex;
  }
 }

 void RefreshGridCombatEnemyStatuses(List<StatusState> statuses){
  if(gridBoardCombatEnemyStatuses==null)return;
  gridBoardCombatEnemyStatuses.Clear();
  if(statuses==null||statuses.Count==0)return;
  foreach(var status in statuses.Take(3)){
   var def=ContentDatabase.Status(status.type);
   var chip=new Label($"{(def!=null?def.name:status.type)} {status.amount}"){pickingMode=PickingMode.Ignore};
   chip.AddToClassList("ps-gboard-combat-status-chip");
   gridBoardCombatEnemyStatuses.Add(chip);
  }
 }

 string DescribeGridBattleIntentShort(){
  if(!TryGetGridBattleIntent(out int raw,out bool special,out int unusedBlock,out List<EffectSpec> unusedEffects))return "次の行動　—";
  string extra="";
  if(unusedEffects!=null&&unusedEffects.Count>0){
   var effect=unusedEffects[0];
   var def=ContentDatabase.Status(effect.type);
   extra=$"\n{(def!=null?def.name:effect.type)} +{effect.amount}";
  }
  if(raw>0){
   int modifier=raw-7;
   return $"⚔ 攻撃\n2D6 {GridDiceModifierText(modifier)}　平均 {raw}{extra}";
  }
  return special?$"次の行動\n✦ 特殊行動{extra}":"次の行動\n—";
 }

 string DescribeGridBattleIntentHint(){
  if(!TryGetGridBattleIntent(out int raw,out bool unusedSpecial,out int block,out var effects))
   return "カードを選んで敵を攻める。";
  var bits=new List<string>();
  if(raw>0){
   int after=Mathf.Max(0,raw-Mathf.Max(0,block));
   bits.Add(block>0?$"次の攻撃 被ダメ {after}":$"次の攻撃 被ダメ {raw}");
  }
  if(effects!=null){
   foreach(var effect in effects.Take(2)){
    var def=ContentDatabase.Status(effect.type);
    bits.Add($"+{(def!=null?def.name:effect.type)}{effect.amount}");
   }
  }
  if(bits.Count==0)bits.Add("特殊行動の予兆");
  return string.Join("　·　",bits);
 }

 bool TryGetGridBattleIntent(out int rawDamage,out bool specialMove,out int playerBlock,out List<EffectSpec> effects){
  rawDamage=0;specialMove=false;playerBlock=0;effects=null;
  var run=game.UiRun;
  var battle=game.UiBattle;
  if(run==null||battle?.enemy==null||battle.enemy.damages==null||battle.enemy.damages.Length==0)return false;
  var dungeon=GameCatalog.Dungeons.First(x=>x.id==run.dungeon);
  int moveIndex=battle.move%battle.enemy.damages.Length;
  int baseDamage=battle.enemy.damages[moveIndex];
  int pressure=GridBoardSystem.EnemyDamageBonus(game.UiGridBoard);
  rawDamage=BattleSystem.Damage(baseDamage+dungeon.damage+pressure,battle.enemyStatuses,run.statuses);
  specialMove=baseDamage==0&&dungeon.damage+pressure==0;
  playerBlock=run.block;
  effects=ContentDatabase.EnemyEffects(battle.enemy.name,moveIndex);
  return true;
 }

 void SetGridHandOpen(bool open){
  if(!gridBoardBuilt||gridBoardHandRoot==null)return;
  if(gridBoardCombatMode)open=true;
  if(gridBoardHandOpen==open)return;
  gridBoardHandOpen=open;
  SyncGridHandChrome();
  var run=game.UiGridBoard;
  if(run!=null)RebuildGridHand(run);
 }

 void FocusGridHandCard(Button card){
  if(gridBoardHoverCard==card)return;
  if(gridBoardHoverCard!=null)gridBoardHoverCard.RemoveFromClassList("ps-gboard-fan-focus");
  if(gridBoardHoverPreview!=null){
   gridBoardHoverPreview.RemoveFromHierarchy();
   gridBoardHoverPreview=null;
  }
  gridBoardHoverCard=card;
  gridBoardHoverCard.AddToClassList("ps-gboard-fan-focus");
  if(card.userData is not CardInstance data)return;
  ShowGridExplorationCardPreview(data,false);
 }

 void ClearGridHandFocus(){
  if(gridBoardHoverCard!=null)gridBoardHoverCard.RemoveFromClassList("ps-gboard-fan-focus");
  gridBoardHoverCard=null;
  if(gridBoardHoverPreview!=null){
   gridBoardHoverPreview.RemoveFromHierarchy();
   gridBoardHoverPreview=null;
  }
  if(gridBoardCombatMode)ShowGridCombatCardPreview(null);
  else RefreshGridSelectedCard(game.UiGridBoard);
 }

 void ShowGridCombatCardPreview(CardInstance card,bool affordable=true){
  if(gridBoardCombatCardPreview==null)return;
  gridBoardCombatCardPreview.Clear();
  if(card==null||game.UiRun==null){
   var hint=new Label("手札に触れて\n術式を確認"){pickingMode=PickingMode.Ignore};
   hint.AddToClassList("ps-gboard-card-view-empty");
   gridBoardCombatCardPreview.Add(hint);
   return;
  }
  var preview=Container("ps-battle-card ps-gboard-card-view-card");
  preview.pickingMode=PickingMode.Ignore;
  PopulateBattleCard(preview,card,game.UiRun,affordable);
  gridBoardCombatCardPreview.Add(preview);
 }

 void RefreshGridSelectedCard(GridBoardRunState run){
  if(gridBoardSelectedHost==null)return;
  bool showPreview=run!=null&&run.phase==GridBoardPhase.Place&&!string.IsNullOrEmpty(run.selectedCardUid);
  if(!showPreview||gridBoardCombatMode||game.UiBattle!=null){
   ShowGridExplorationCardPreview(null,false);
   return;
  }
  var card=run!=null&&run.phase==GridBoardPhase.Place?GridBoardSystem.SelectedCard(run):null;
  if(card==null){
   ShowGridExplorationCardPreview(null,false);
   return;
  }
  ShowGridExplorationCardPreview(card,true);
 }

void ShowGridExplorationCardPreview(CardInstance card,bool committed){
  if(gridBoardSelectedHost==null)return;
  if(card!=null)HideGridCellDetail();
  gridBoardSelectedHost.Clear();
  if(card==null){
   gridBoardSelectedHost.style.display=DisplayStyle.None;
   return;
  }
  // Inline size/pos so PackspireBattle.uss (.ps-battle-card 168x236) cannot win.
  const float cardW=252f;
  const float cardH=354f;
  gridBoardSelectedHost.style.display=DisplayStyle.Flex;
  gridBoardSelectedHost.style.backgroundColor=Color.clear;
  gridBoardSelectedHost.style.position=Position.Absolute;
  // Planning preview overlays the right edge of the map; exploration has no
  // persistent dossier column competing for this space.
  gridBoardSelectedHost.style.right=34;
  gridBoardSelectedHost.style.top=82;
  gridBoardSelectedHost.style.width=cardW;
  gridBoardSelectedHost.style.height=cardH;
  VisualElement preview;
  if(committed){
   preview=new Button(()=>{
    if(game.UiGridBoard!=null)game.UiGridBoard.selectedCardUid="";
    RefreshGridBoard();
   });
  } else {
   preview=new VisualElement{pickingMode=PickingMode.Ignore};
  }
  preview.AddToClassList("ps-battle-card");
  preview.AddToClassList("ps-gboard-selected-card");
  preview.AddToClassList("ps-gboard-side-preview");
  preview.style.position=Position.Relative;
  preview.style.width=cardW;
  preview.style.height=cardH;
  preview.style.minWidth=cardW;
  preview.style.minHeight=cardH;
  preview.style.maxWidth=cardW;
  preview.style.maxHeight=cardH;
  PopulateGridPlaceCard(preview,card);
  gridBoardSelectedHost.Add(preview);
 }

 void RebuildGridHand(GridBoardRunState run){
  if(gridBoardHandRoot==null)return;
  ClearGridHandFocus();
  gridBoardHandRoot.Clear();
  EnsureBattleAssets();

  if(gridBoardCombatMode){
   RebuildGridCombatHand();
   return;
  }

  bool show=run.phase==GridBoardPhase.Place&&run.hand!=null&&run.hand.Count>0;
  gridBoardHandRoot.style.display=show?DisplayStyle.Flex:DisplayStyle.None;
  if(!show){
   gridBoardHandOpen=false;
   SyncGridHandChrome();
   return;
  }
  SyncGridHandChrome();

  var cards=run.hand;
  if(cards==null||cards.Count==0){
   var empty=new Label("手札なし — マスを押して導線へ"){pickingMode=PickingMode.Ignore};
   empty.AddToClassList("ps-gboard-hand-empty");
   gridBoardHandRoot.Add(empty);
   return;
  }

  int count=cards.Count;
  float center=(count-1)*0.5f;
  float spreadDeg=count<=5?6.2f:count==6?7.4f:5.0f;
  float radius=count<=5?110f:count==6?205f:155f;
  float horizontalStep=count<=5?112f:count==6?92f:72f;
  bool open=gridBoardHandOpen;
  float sink=open?0f:GridHandPeekSink;
  var handSlots=new List<(Button button,float depth)>(count);
  for(int i=0;i<count;i++){
   var capture=cards[i];
   bool selected=run.selectedCardUid==capture.slotKey;
   Button button=null;
   button=new Button(()=>{
    if(run.phase==GridBoardPhase.Place){
     run.selectedCardUid=capture.slotKey;
     run.message=$"{capture.name} を選択";
    }
    gridBoardHandOpen=false;
    SyncGridHandChrome();
    RefreshGridBoard();
   });
   button.AddToClassList("ps-battle-card");
   button.AddToClassList("ps-gboard-fan-card");
   button.userData=capture;
   if(!open)button.AddToClassList("ps-gboard-fan-peek");
   if(selected)button.AddToClassList("ps-gboard-fan-selected");
   PopulateGridPlaceCard(button,capture);
   float spreadIndex=i-center;
   float angle=open?spreadIndex*spreadDeg:spreadIndex*2.2f;
   float rad=angle*Mathf.Deg2Rad;
   float arcLift=open?radius*(1f-Mathf.Cos(rad)):0f;
   float span=GridHandCardWidth+(count-1)*horizontalStep;
   float outerInset=Mathf.Max(8f,(GridHandWidth-span)*0.5f);
   float baseRight=(count-1-i)*horizontalStep+outerInset;
   float arcShift=open?radius*Mathf.Sin(rad):spreadIndex*6f;
   button.style.position=Position.Absolute;
   button.style.right=baseRight-arcShift;
   button.style.bottom=arcLift-sink;
   button.style.rotate=new Rotate(new Angle(angle,AngleUnit.Degree));
   button.style.transformOrigin=new TransformOrigin(new Length(50,LengthUnit.Percent),new Length(100,LengthUnit.Percent));
   if(!gridBoardCombatMode){
    button.RegisterCallback<PointerEnterEvent>(_=>FocusGridHandCard(button));
   } else if(open)button.RegisterCallback<PointerEnterEvent>(_=>button.BringToFront());
   handSlots.Add((button,Mathf.Abs(spreadIndex)-(selected?10f:0f)));
  }
  foreach(var slot in handSlots.OrderByDescending(x=>x.depth))
   gridBoardHandRoot.Add(slot.button);
 }

 void RebuildGridCombatHand(){
  var run=game.UiRun;
  gridBoardHandOpen=true;
  SyncGridHandChrome();
  gridBoardHandRoot.style.display=DisplayStyle.Flex;
  if(run?.hand==null||run.hand.Count==0){
   var empty=new Label("手札なし"){pickingMode=PickingMode.Ignore};
   empty.AddToClassList("ps-gboard-hand-empty");
   gridBoardHandRoot.Add(empty);
   ShowGridCombatCardPreview(null);
   AttachGridResolveTray();
   return;
  }
  int count=run.hand.Count;
  float center=(count-1)*0.5f;
  float spreadDeg=count<=5?6.2f:count==6?7.4f:count==7?7.0f:5.0f;
  float radius=count<=5?110f:count==6?205f:count==7?198f:155f;
  float horizontalStep=count<=5?112f:count==6?92f:count==7?80f:72f;
  var handSlots=new List<(Button button,float depth)>(count);
  for(int i=0;i<count;i++){
   int index=i;
   var card=run.hand[index];
   bool affordable=card.cost<=run.energy;
   Button button=null;
   button=new Button(()=>{
    if(!affordable||battleInputLocked||button==null||game.UiBattle==null)return;
    battleInputLocked=true;
    button.SetEnabled(false);
    if(!game.UiPlayBattleCard(index))ShowToast(game.UiMessage);
    battleInputLocked=false;
    RefreshGridBoard();
   });
   button.AddToClassList("ps-battle-card");
   button.AddToClassList("ps-gboard-fan-card");
   button.userData=card;
   if(!affordable)button.AddToClassList("ps-battle-card-disabled");
   PopulateBattleCard(button,card,run,affordable);
   float spreadIndex=i-center;
   float angle=spreadIndex*spreadDeg;
   float rad=angle*Mathf.Deg2Rad;
   float arcLift=radius*(1f-Mathf.Cos(rad));
   float span=GridHandCardWidth+(count-1)*horizontalStep;
   float outerInset=Mathf.Max(8f,(GridHandWidth-span)*0.5f);
   float baseRight=(count-1-i)*horizontalStep+outerInset;
   float arcShift=radius*Mathf.Sin(rad);
   button.style.position=Position.Absolute;
   button.style.right=baseRight-arcShift;
   button.style.bottom=arcLift;
   button.style.rotate=new Rotate(new Angle(angle,AngleUnit.Degree));
   button.style.transformOrigin=new TransformOrigin(new Length(50,LengthUnit.Percent),new Length(100,LengthUnit.Percent));
   button.RegisterCallback<PointerEnterEvent>(_=>{
    button.BringToFront();
    ShowGridCombatCardPreview(card,affordable);
   });
   handSlots.Add((button,Mathf.Abs(spreadIndex)));
  }
  foreach(var slot in handSlots.OrderByDescending(x=>x.depth))
   gridBoardHandRoot.Add(slot.button);
  ShowGridCombatCardPreview(null);
  AttachGridResolveTray();
 }

 // Card detail and dice results share one fixed right-side reading lane.
 void AttachGridResolveTray(){
  if(gridBoardResolveTray==null)return;
  var host=gridBoardCombatActionView??gridBoardRoot;
  if(host==null)return;
  if(gridBoardResolveTray.parent!=host)host.Add(gridBoardResolveTray);
  gridBoardResolveTray.BringToFront();
 }

 void PlayGridBattleActionFx(BattleActionFx fx){
  if(gridBoardFxLayer==null||gridBoardCombatStage==null)return;
  ShowGridDiceResult(fx);
  if(fx.damageToEnemy>0&&gridBoardCombatEnemyFocus!=null){
   gridBoardCombatEnemyFocus.AddToClassList("ps-gboard-enemy-hit");
   gridBoardCombatEnemyFocus.schedule.Execute(()=>gridBoardCombatEnemyFocus?.RemoveFromClassList("ps-gboard-enemy-hit")).StartingIn(180);
  }
  if(fx.damageToPlayer>0&&gridBoardRoot!=null){
   gridBoardRoot.AddToClassList("ps-gboard-player-hit");
   gridBoardRoot.schedule.Execute(()=>gridBoardRoot?.RemoveFromClassList("ps-gboard-player-hit")).StartingIn(220);
  }
  EnsureBattleAssets();
  int stagger=0;
  void Spawn(string value,Texture2D icon,string tone){
   var floater=Container("ps-battle-floater "+tone);
   floater.pickingMode=PickingMode.Ignore;
   if(icon!=null){
    var iconImg=new Image{image=icon,scaleMode=ScaleMode.ScaleToFit,pickingMode=PickingMode.Ignore};
    iconImg.AddToClassList("ps-battle-floater-icon-art");
    floater.Add(iconImg);
   }
   var valueLabel=new Label(value){pickingMode=PickingMode.Ignore};
   valueLabel.AddToClassList("ps-battle-floater-value");
   floater.Add(valueLabel);
   gridBoardFxLayer.Add(floater);
   int serial=++gridBoardFloaterSerial;
   float jitterX=(serial%5-2)*22f;
   float jitterY=(serial%3)*12f;
   floater.style.position=Position.Absolute;
   floater.style.left=Length.Percent(42);
   floater.style.top=Length.Percent(28);
   floater.style.translate=new Translate(jitterX,jitterY);
   floater.schedule.Execute(()=>floater.AddToClassList("ps-battle-floater-pop")).StartingIn(Mathf.Max(16,stagger));
   floater.schedule.Execute(()=>{if(floater.parent!=null)floater.AddToClassList("ps-battle-floater-out");}).StartingIn(Mathf.Max(16,stagger)+420);
   floater.schedule.Execute(()=>floater.RemoveFromHierarchy()).StartingIn(Mathf.Max(16,stagger)+980);
   stagger+=70;
  }
  if(fx.damageToEnemy>0)Spawn(fx.damageToEnemy.ToString(),battleIconDamage,"ps-battle-floater-damage");
  if(fx.damageToPlayer>0)Spawn(fx.damageToPlayer.ToString(),battleIconClaw??battleIconDamage,"ps-battle-floater-damage");
  if(fx.blockGained>0)Spawn("+"+fx.blockGained,battleIconBlock,"ps-battle-floater-block");
  if(fx.healGained>0)Spawn("+"+fx.healGained,battleIconHeal,"ps-battle-floater-heal");
  if(fx.energyGained>0)Spawn("+"+fx.energyGained,battleIconEnergy,"ps-battle-floater-energy");
  if(fx.selfDamage>0)Spawn(fx.selfDamage.ToString(),battleIconDamage,"ps-battle-floater-self");
  if(fx.damageToEnemy<=0&&fx.damageToPlayer<=0&&fx.blockGained<=0&&fx.healGained<=0&&fx.energyGained<=0&&fx.selfDamage<=0){
   if(fx.cardType==CardType.Power)Spawn("強化",battleIconEnergy,"ps-battle-floater-power");
   else if(fx.cardType==CardType.Skill)Spawn("発動",battleIconBlock,"ps-battle-floater-skill");
  }
 }

 void PopulateGridPlaceCard(VisualElement slot,CardInstance card){
  // Skill frame (combat-card-01) for place cards — same chrome as battle.
  Texture2D frame=gridBoardCardFrames!=null&&gridBoardCardFrames.Length>1?gridBoardCardFrames[1]:null;
  if(frame==null&&gridBoardCardFrames!=null&&gridBoardCardFrames.Length>0)frame=gridBoardCardFrames[0];
  if(frame!=null){
   slot.style.backgroundImage=new StyleBackground(frame);
   slot.style.unityBackgroundScaleMode=ScaleMode.StretchToFill;
  }
  var cost=new Label(card.cost.ToString()){pickingMode=PickingMode.Ignore};
  cost.AddToClassList("ps-battle-card-cost");
  slot.Add(cost);
  var illustration=Container("ps-battle-card-art");
  var glyph=new Label(GridPlaceGlyph(card)){pickingMode=PickingMode.Ignore};
  glyph.AddToClassList("ps-gboard-card-glyph");
  illustration.Add(glyph);
  slot.Add(illustration);
  var name=new Label(card.name){pickingMode=PickingMode.Ignore};
  name.AddToClassList("ps-battle-card-name");
  slot.Add(name);
  var body=new Label(card.text){pickingMode=PickingMode.Ignore};
  body.AddToClassList("ps-battle-card-text");
  slot.Add(body);
  var foot=Container("ps-battle-card-foot");
  var source=new Label("配置"){pickingMode=PickingMode.Ignore};
  source.AddToClassList("ps-battle-card-source");
  foot.Add(source);
  var tag=new Label("GRID"){pickingMode=PickingMode.Ignore};
  tag.AddToClassList("ps-battle-card-durability");
  foot.Add(tag);
  slot.Add(foot);
 }

 static string GridPlaceGlyph(CardInstance card){
  if(card==null)return "◆";
  if(card.id.Contains("lamp")||card.name.Contains("灯り"))return "灯";
  if(card.id.Contains("fog")||card.name.Contains("霧"))return "霧";
  if(card.id.Contains("seal")||card.name.Contains("封鎖"))return "封";
  return "◆";
 }

 void BuildGridBoardEventOverlay(){
  if(gridBoardRoot==null)return;
  gridBoardEventOverlay=Container("ps-gboard-event-overlay");
  gridBoardEventOverlay.style.display=DisplayStyle.None;
  var panel=Container("ps-gboard-event-popup");
  var eyebrow=new Label("UNKNOWN SIGNAL"){pickingMode=PickingMode.Ignore};
  eyebrow.AddToClassList("ps-gboard-event-eyebrow");
  panel.Add(eyebrow);
  var title=new Label("異変を発見"){pickingMode=PickingMode.Ignore};
  title.AddToClassList("ps-gboard-event-title");
  panel.Add(title);
  var body=new Label("足元の封印が脈打っている。進行を止めて、どう対処するか選べ。"){pickingMode=PickingMode.Ignore};
  body.AddToClassList("ps-gboard-event-body");
  panel.Add(body);
  var choices=Container("ps-gboard-event-choices");
  var risk=MakeGridAction("血を捧げる　HP -6 / 24G",()=>ResolveGridBoardEvent(0));
  risk.AddToClassList("ps-gboard-event-choice");
  choices.Add(risk);
  var repair=MakeGridAction("装備を整える　耐久を回復",()=>ResolveGridBoardEvent(1));
  repair.AddToClassList("ps-gboard-event-choice");
  choices.Add(repair);
  var leave=MakeGridAction("立ち去る",()=>ResolveGridBoardEvent(2));
  leave.AddToClassList("ps-gboard-event-choice");
  choices.Add(leave);
  panel.Add(choices);
  gridBoardEventOverlay.Add(panel);
  gridBoardRoot.Add(gridBoardEventOverlay);
 }

 void OpenGridBoardEventPopup(){
  if(gridBoardRoot==null||gridBoardEventOpen)return;
  if(gridBoardEventOverlay==null)BuildGridBoardEventOverlay();
  if(gridBoardEventOverlay==null)return;
  gridBoardEventOpen=true;
  gridBoardEventOverlay.style.display=DisplayStyle.Flex;
  gridBoardEventOverlay.BringToFront();
 }

 void ResolveGridBoardEvent(int choice){
  if(!gridBoardEventOpen)return;
  gridBoardEventOpen=false;
  if(gridBoardEventOverlay!=null)
   gridBoardEventOverlay.style.display=DisplayStyle.None;
  game.UiResolveEvent(choice);
  // The event resolves without leaving the grid; movement resumes next tick.
  ForceRefreshScreen();
 }

 void TickGridBoard(){
  if(!gridBoardBuilt)return;
  var run=game.UiGridBoard;
  if(run==null)return;
  if(game.UiBattle!=null)return;
  if(gridBoardEventOpen)return;
  bool routeChanged=run.phase==GridBoardPhase.Run
   &&GridBoardSystem.TickRun(run,Time.unscaledDeltaTime);
  if(run.pendingEvent){
   run.pendingEvent=false;
   // The board-local overlay can be hidden behind the grid's visual layers.
   // The established Event route is attached to the screen root and returns
   // here after the player chooses, so the route continues from its next cell.
   game.UiBeginGridEvent();
   return;
  }
  if(run.pendingBattle){
   run.pendingBattle=false;
   game.UiBeginGridEncounter();
   RefreshGridBoard();
   return;
  }
  if(routeChanged)RefreshGridBoard();
 }
}
}
