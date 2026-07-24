using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire {
public sealed partial class PackspireUiFoundation {
 VisualElement gridBoardRoot,gridBoardGrid,gridBoardHandRoot,gridBoardDirRow,gridBoardHpFill,gridBoardPortraitHost;
 VisualElement gridBoardViewport,gridBoardStage,gridBoardSelectedHost,gridBoardCombatStage;
 VisualElement gridBoardContextActions,gridBoardEnergyRail,gridBoardCombatRail,gridBoardConsumablesRoot,gridBoardFxLayer;
 VisualElement gridBoardGateActions;
 Label gridBoardPhaseLabel,gridBoardInkLabel,gridBoardHintLabel;
 Label gridBoardTypeLabel,gridBoardTitleLabel,gridBoardStatusLabel,gridBoardBodyLabel;
 Label gridBoardHeroNameLabel,gridBoardHpLabel,gridBoardZoomLabel,gridBoardEnergyLabel,gridBoardCombatTitle;
 Label gridBoardCombatHpLabel,gridBoardCombatIntentLabel;
 Image gridBoardCombatPortrait;
 Button gridBoardSkillButton,gridBoardEndTurnButton;
 readonly Dictionary<long,VisualElement> gridBoardCells=new();
 bool gridBoardBuilt;
 bool gridBoardHandOpen;
 bool gridBoardLayoutBusy;
 bool gridBoardCombatMode;
 float gridBoardZoom=1f;
 Vector2 gridBoardPan;
 bool gridBoardPanning,gridBoardDidPan;
 Vector2 gridBoardPointerStart,gridBoardPanAtStart;
 Vector2 gridBoardLastLayoutPos=new(float.NaN,float.NaN);
 Texture2D[] gridBoardCardFrames;
 int gridBoardFloaterSerial;
 const float GridZoomMin=0.55f;
 const float GridZoomMax=1.9f;
 const float GridCellBasePx=64f;
 const float GridHandPeekSink=188f;
 // Board y=0 is top of screen, so "up" on UI decreases y.
 static readonly Vector2Int BoardUp=new(0,-1);
 static readonly Vector2Int BoardDown=new(0,1);
 static readonly Vector2Int BoardLeft=new(-1,0);
 static readonly Vector2Int BoardRight=new(1,0);

 void SuspendGridBoard(){
  gridBoardBuilt=false;
  gridBoardHandOpen=false;
  gridBoardCombatMode=false;
  gridBoardRoot=null;gridBoardGrid=null;gridBoardHandRoot=null;gridBoardDirRow=null;
  gridBoardHpFill=null;gridBoardPortraitHost=null;
  gridBoardViewport=null;gridBoardStage=null;gridBoardSelectedHost=null;gridBoardCombatStage=null;
  gridBoardContextActions=null;gridBoardEnergyRail=null;gridBoardCombatRail=null;
  gridBoardConsumablesRoot=null;gridBoardFxLayer=null;gridBoardGateActions=null;
  gridBoardPhaseLabel=null;gridBoardInkLabel=null;gridBoardHintLabel=null;
  gridBoardTypeLabel=null;gridBoardTitleLabel=null;gridBoardStatusLabel=null;gridBoardBodyLabel=null;
  gridBoardHeroNameLabel=null;gridBoardHpLabel=null;gridBoardZoomLabel=null;gridBoardEnergyLabel=null;gridBoardCombatTitle=null;
  gridBoardCombatHpLabel=null;gridBoardCombatIntentLabel=null;gridBoardCombatPortrait=null;
  gridBoardSkillButton=null;gridBoardEndTurnButton=null;
  gridBoardPanning=false;gridBoardDidPan=false;
  gridBoardLayoutBusy=false;
  gridBoardLastLayoutPos=new(float.NaN,float.NaN);
  gridBoardCells.Clear();
 }

 void EnsureGridBoardCardFrames(){
  if(gridBoardCardFrames!=null)return;
  gridBoardCardFrames=new Texture2D[3];
  for(int i=0;i<3;i++)
   gridBoardCardFrames[i]=Resources.Load<Texture2D>($"Art/UI/Cards/combat-card-{i:00}");
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
  if(gridBoardZoom<GridZoomMin||gridBoardZoom>GridZoomMax)gridBoardZoom=1f;
  gridBoardPan=Vector2.zero;

  gridBoardRoot=Container("ps-gboard");
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
  gridBoardGrid.style.position=Position.Absolute;
  gridBoardGrid.style.flexGrow=0;
  gridBoardGrid.style.flexShrink=0;
  BuildGridCells(run);
  gridBoardViewport.Add(gridBoardGrid);
  EnsureBattleAssets();
  gridBoardCombatStage=Container("ps-gboard-combat-stage");
  gridBoardCombatStage.style.display=DisplayStyle.None;
  gridBoardCombatPortrait=new Image{scaleMode=ScaleMode.ScaleToFit,pickingMode=PickingMode.Ignore};
  gridBoardCombatPortrait.AddToClassList("ps-gboard-combat-portrait");
  gridBoardCombatStage.Add(gridBoardCombatPortrait);
  gridBoardCombatTitle=new Label("敵"){pickingMode=PickingMode.Ignore};
  gridBoardCombatTitle.AddToClassList("ps-gboard-combat-title");
  gridBoardCombatStage.Add(gridBoardCombatTitle);
  gridBoardCombatHpLabel=new Label("HP —"){pickingMode=PickingMode.Ignore};
  gridBoardCombatHpLabel.AddToClassList("ps-gboard-combat-hp");
  gridBoardCombatStage.Add(gridBoardCombatHpLabel);
  gridBoardCombatIntentLabel=new Label(""){pickingMode=PickingMode.Ignore};
  gridBoardCombatIntentLabel.AddToClassList("ps-gboard-combat-intent");
  gridBoardCombatStage.Add(gridBoardCombatIntentLabel);
  gridBoardFxLayer=Container("ps-gboard-fx-layer");
  gridBoardFxLayer.pickingMode=PickingMode.Ignore;
  gridBoardCombatStage.Add(gridBoardFxLayer);
  gridBoardStage.Add(gridBoardViewport);
  gridBoardStage.Add(gridBoardCombatStage);

  var zoomBar=Container("ps-gboard-zoom");
  zoomBar.Add(MakeGridAction("−",()=>AdjustGridZoom(-0.12f)));
  gridBoardZoomLabel=new Label(""){pickingMode=PickingMode.Ignore};
  gridBoardZoomLabel.AddToClassList("ps-gboard-zoom-label");
  zoomBar.Add(gridBoardZoomLabel);
  zoomBar.Add(MakeGridAction("＋",()=>AdjustGridZoom(0.12f)));
  gridBoardStage.Add(zoomBar);
  gridBoardRoot.Add(gridBoardStage);

  var top=Container("ps-gboard-top");
  DressRiteFrame(top);
  var brand=Container("ps-rite-brand");
  var mark=Container("ps-rite-brand-mark");
  mark.pickingMode=PickingMode.Ignore;
  brand.Add(mark);
  var titleBlock=Container("ps-rite-top-title");
  var eye=new Label("EXPEDITION  /  GRID"){pickingMode=PickingMode.Ignore};
  eye.AddToClassList("ps-rite-top-eyebrow");
  eye.AddToClassList("ps-chrome-eyebrow");
  titleBlock.Add(eye);
  var name=new Label("封印格子"){pickingMode=PickingMode.Ignore};
  name.AddToClassList("ps-rite-top-name");
  titleBlock.Add(name);
  var topSub=new Label("カードを置き、導線を引く"){pickingMode=PickingMode.Ignore};
  topSub.AddToClassList("ps-gboard-top-sub");
  titleBlock.Add(topSub);
  brand.Add(titleBlock);
  top.Add(brand);
  gridBoardPhaseLabel=new Label(""){pickingMode=PickingMode.Ignore};
  gridBoardPhaseLabel.AddToClassList("ps-gboard-phase");
  top.Add(gridBoardPhaseLabel);
  gridBoardInkLabel=new Label(""){pickingMode=PickingMode.Ignore};
  gridBoardInkLabel.AddToClassList("ps-gboard-ink");
  top.Add(gridBoardInkLabel);
  var finishTop=MakeGridAction("撤退",()=>{
   game.UiRetreatFromGrid();
   ForceRefreshScreen();
  });
  finishTop.AddToClassList("ps-gboard-top-finish");
  top.Add(finishTop);
  gridBoardRoot.Add(top);

  var dock=Container("ps-gboard-dock");
  var panel=Container("ps-gboard-panel");
  DressRiteFrame(panel);
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
  panel.Add(gridBoardGateActions);

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

  var hero=Container("ps-gboard-hero");
  var heroRow=Container("ps-gboard-hero-row");
  gridBoardPortraitHost=Container("ps-gboard-portrait");
  var character=CharacterSystem.OfRun(game.UiRun);
  if(character!=null)
   gridBoardPortraitHost.Add(CharacterPortraitFront(character,"ps-gboard-portrait-image"));
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
  heroRow.Add(heroMeta);
  hero.Add(heroRow);
  dock.Add(hero);
  gridBoardRoot.Add(dock);

  // Bottom-right peek fan: hover to expand, click to pick.
  gridBoardHandOpen=false;
  gridBoardHandRoot=Container("ps-battle-hand");
  gridBoardHandRoot.AddToClassList("ps-gboard-hand-fan");
  gridBoardHandRoot.RegisterCallback<PointerEnterEvent>(_=>SetGridHandOpen(true));
  gridBoardHandRoot.RegisterCallback<PointerLeaveEvent>(_=>SetGridHandOpen(false));
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
  gridBoardEndTurnButton=MakeGridAction("END TURN",()=>{
   if(game.UiBattle==null||battleInputLocked){ShowToast("戦闘中ではない");return;}
   if(!game.UiEndBattleTurn())ShowToast(game.UiMessage);
   RefreshGridBoard();
  });
  gridBoardEndTurnButton.AddToClassList("ps-gboard-endturn");
  gridBoardCombatRail.Add(gridBoardEndTurnButton);
  gridBoardEnergyRail.Add(gridBoardCombatRail);
  gridBoardRoot.Add(gridBoardEnergyRail);

  gridBoardSelectedHost=Container("ps-gboard-selected");
  gridBoardSelectedHost.style.display=DisplayStyle.None;
  gridBoardRoot.Add(gridBoardSelectedHost);

  RefreshGridBoard();
 }

 Button MakeGridAction(string label,System.Action onClick){
  var b=PackspireUiFactory.Button(label,onClick);
  b.AddToClassList("ps-gboard-action");
  return b;
 }

 Button MakeDirButton(string label,Vector2Int dir){
  var b=PackspireUiFactory.Button(label,()=>{
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
    cell.style.flexGrow=0;
    cell.style.flexShrink=0;
    cell.text="";
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

 void EnterGridCombatMode(bool on){
  gridBoardCombatMode=on;
  if(gridBoardViewport!=null)
   gridBoardViewport.style.display=on?DisplayStyle.None:DisplayStyle.Flex;
  if(gridBoardCombatStage!=null)
   gridBoardCombatStage.style.display=on?DisplayStyle.Flex:DisplayStyle.None;
  if(on){
   gridBoardHandOpen=true;
   if(gridBoardZoomLabel!=null&&gridBoardZoomLabel.parent!=null)
    gridBoardZoomLabel.parent.style.display=DisplayStyle.None;
  } else if(gridBoardZoomLabel!=null&&gridBoardZoomLabel.parent!=null)
   gridBoardZoomLabel.parent.style.display=DisplayStyle.Flex;
  SyncGridHandChrome();
 }

 void OnGridBoardWheel(WheelEvent evt){
  float step=evt.delta.y<0f?0.1f:-0.1f;
  AdjustGridZoom(step);
  evt.StopPropagation();
 }

 void OnGridBoardPointerDown(PointerDownEvent evt){
  if(evt.button!=0)return;
  if(gridBoardHandRoot!=null&&gridBoardHandRoot.resolvedStyle.display!=DisplayStyle.None
   &&gridBoardHandRoot.worldBound.Contains(evt.position))return;
  gridBoardPanning=true;
  gridBoardDidPan=false;
  gridBoardPointerStart=evt.position;
  gridBoardPanAtStart=gridBoardPan;
 }

 void OnGridBoardPointerMove(PointerMoveEvent evt){
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

 void OnGridViewportGeometryChanged(GeometryChangedEvent evt){
  if(Mathf.Approximately(evt.oldRect.width,evt.newRect.width)
   &&Mathf.Approximately(evt.oldRect.height,evt.newRect.height))return;
  LayoutGridBoardMap();
 }

 void AdjustGridZoom(float delta){
  float next=Mathf.Clamp(gridBoardZoom+delta,GridZoomMin,GridZoomMax);
  if(Mathf.Approximately(next,gridBoardZoom))return;
  gridBoardZoom=next;
  ApplyGridZoomVisual();
  gridBoardLastLayoutPos=new(float.NaN,float.NaN);
  LayoutGridBoardMap();
  if(gridBoardZoomLabel!=null)
   gridBoardZoomLabel.text=$"{Mathf.RoundToInt(gridBoardZoom*100f)}%";
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
  if(gridBoardZoomLabel!=null)
   gridBoardZoomLabel.text=$"{Mathf.RoundToInt(gridBoardZoom*100f)}%";
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
  // Center inside the right-hand stage (left of dock is excluded by stage bounds).
  float left=(vr.width-gw)*0.5f+gridBoardPan.x;
  float top=(vr.height-gh)*0.5f+gridBoardPan.y;
  if(!float.IsNaN(gridBoardLastLayoutPos.x)
   &&Mathf.Abs(gridBoardLastLayoutPos.x-left)<0.5f
   &&Mathf.Abs(gridBoardLastLayoutPos.y-top)<0.5f)return;
  gridBoardLayoutBusy=true;
  gridBoardLastLayoutPos=new Vector2(left,top);
  gridBoardGrid.style.position=Position.Absolute;
  gridBoardGrid.style.left=left;
  gridBoardGrid.style.top=top;
  gridBoardLayoutBusy=false;
 }

 void RefreshGridBoard(){
  var run=game.UiGridBoard;
  if(!gridBoardBuilt||run==null||gridBoardGrid==null)return;

  // Keep stage mode in sync with live battle state (no ScreenId.Battle hop).
  bool inBattle=game.UiBattle!=null;
  if(inBattle&&!gridBoardCombatMode)EnterGridCombatMode(true);
  else if(!inBattle&&gridBoardCombatMode)EnterGridCombatMode(false);

  string phase=GridBoardSystem.PhaseLabel(run.phase);
  string ink=run.phase==GridBoardPhase.Path||run.phase==GridBoardPhase.Run||run.phase==GridBoardPhase.Done
   ?$"曲がり　{run.turnsUsed}/{run.turnsMax}　·　長さ {Mathf.Max(0,run.path.Count-1)}"
   :$"曲がり上限　{run.turnsMax}";
  string hint=string.IsNullOrEmpty(run.message)
   ?"カードを置いてから、マスや矢印で導線を引く。"
   :run.message;

  if(gridBoardPhaseLabel!=null)gridBoardPhaseLabel.text=inBattle?"戦闘":phase;
  if(gridBoardInkLabel!=null)
   gridBoardInkLabel.text=inBattle?$"勝利数 {game.UiRun?.battlesWon??0}":$"{GridBoardSystem.AreaLabel(run)}　·　{ink}";
  if(gridBoardTypeLabel!=null)gridBoardTypeLabel.text=inBattle?"戦闘":"封印格子";
  if(gridBoardTitleLabel!=null)
   gridBoardTitleLabel.text=inBattle?(game.UiBattle.enemy?.name??"交戦中"):(string.IsNullOrEmpty(run.pendingGate)?phase:run.pendingGate=="next"?"次区画":"帰還点");
  if(gridBoardStatusLabel!=null)
   gridBoardStatusLabel.text=inBattle?DescribeGridBattleIntentShort():(string.IsNullOrEmpty(run.pendingGate)?ink:GridBoardSystem.AreaLabel(run));

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
   if(cellMark!=null){
    string t=cell.terrain switch{
     "start"=>"入",
     "goal"=>"標",
     "blocked"=>"■",
     _=>GridBoardSystem.PlaceLabel(cell.place),
    };
    if(cell.place=="lamp"&&cell.grow>0)t=$"灯{cell.grow}";
    if(cell.x==pieceX&&cell.y==pieceY)t=string.IsNullOrEmpty(t)?"●":t+"●";
    cellMark.text=t??"";
   }
  }

  ApplyGridZoomVisual();
  LayoutGridBoardMap();
  RebuildGridHand(run);
  RefreshGridSelectedCard(run);
 }

 void RefreshGridHero(){
  var runState=game.UiRun;
  var character=CharacterSystem.OfRun(runState);
  if(gridBoardHeroNameLabel!=null)
   gridBoardHeroNameLabel.text=character?.name??"探索者";
  if(runState==null){
   if(gridBoardHpLabel!=null)gridBoardHpLabel.text="体力 —";
   if(gridBoardHpFill!=null)gridBoardHpFill.style.width=Length.Percent(0);
   return;
  }
  if(gridBoardHpLabel!=null)
   gridBoardHpLabel.text=$"体力　{runState.hp} / {runState.maxHp}";
  if(gridBoardHpFill!=null){
   float t=runState.maxHp>0?Mathf.Clamp01((float)runState.hp/runState.maxHp):0f;
   gridBoardHpFill.style.width=Length.Percent(t*100f);
  }
 }

 void SyncGridHandChrome(){
  if(gridBoardHandRoot==null)return;
  bool open=gridBoardHandOpen||gridBoardCombatMode;
  gridBoardHandRoot.EnableInClassList("ps-gboard-hand-open",open);
  gridBoardHandRoot.style.left=StyleKeyword.Auto;
  gridBoardHandRoot.style.right=10;
  gridBoardHandRoot.style.bottom=52;
  gridBoardHandRoot.style.width=460;
  gridBoardHandRoot.style.height=open?310:56;
  gridBoardHandRoot.style.overflow=open?Overflow.Visible:Overflow.Hidden;
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
  gridBoardEnergyLabel.text=orbs.ToString();
  if(gridBoardCombatRail!=null)
   gridBoardCombatRail.style.display=gridBoardCombatMode?DisplayStyle.Flex:DisplayStyle.None;
  if(gridBoardSkillButton!=null){
   gridBoardSkillButton.text=game.UiActiveSkillAvailable?game.UiActiveSkillLabel:"USED";
   gridBoardSkillButton.tooltip=game.UiActiveSkillTooltip;
   gridBoardSkillButton.SetEnabled(gridBoardCombatMode&&game.UiActiveSkillAvailable&&!battleInputLocked);
  }
  if(gridBoardEndTurnButton!=null)
   gridBoardEndTurnButton.SetEnabled(gridBoardCombatMode&&game.UiBattle!=null&&!battleInputLocked);
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
  var head=new Label(next?"次区画への裂け目":"帰還点"){pickingMode=PickingMode.Ignore};
  head.AddToClassList("ps-gboard-gate-title");
  gridBoardGateActions.Add(head);
  if(next){
   gridBoardGateActions.Add(MakeGridAction("次の区画へ進む",()=>{
    game.UiAdvanceGridArea();
   }));
  } else {
   gridBoardGateActions.Add(MakeGridAction("帰還する",()=>{
    game.UiConfirmGridReturn();
    ForceRefreshScreen();
   }));
  }
  gridBoardGateActions.Add(MakeGridAction("まだ探索する",()=>{
   game.UiDeclineGridGate();
   RefreshGridBoard();
  }));
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
  if(gridBoardCombatHpLabel!=null){
   string block=battle.enemyBlock>0?$"　·　BLK {battle.enemyBlock}":"";
   gridBoardCombatHpLabel.text=$"HP {Mathf.Max(0,battle.enemyHp)} / {battle.enemyMaxHp}{block}";
  }
  if(gridBoardCombatIntentLabel!=null)
   gridBoardCombatIntentLabel.text=DescribeGridBattleIntentShort();
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

 string DescribeGridBattleIntentShort(){
  if(!TryGetGridBattleIntent(out int raw,out bool special,out int unusedBlock,out List<EffectSpec> unusedEffects))return "意図 —";
  if(raw>0)return $"意図 ATTACK {raw}";
  return special?"意図 SPECIAL":"意図 —";
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
  rawDamage=BattleSystem.Damage(baseDamage+dungeon.damage,battle.enemyStatuses,run.statuses);
  specialMove=baseDamage==0&&dungeon.damage==0;
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

 void RefreshGridSelectedCard(GridBoardRunState run){
  if(gridBoardSelectedHost==null)return;
  gridBoardSelectedHost.Clear();
  if(gridBoardCombatMode||game.UiBattle!=null){
   gridBoardSelectedHost.style.display=DisplayStyle.None;
   return;
  }
  var card=run!=null&&run.phase==GridBoardPhase.Place?GridBoardSystem.SelectedCard(run):null;
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
  gridBoardSelectedHost.style.right=28;
  gridBoardSelectedHost.style.top=96;
  gridBoardSelectedHost.style.width=cardW;
  gridBoardSelectedHost.style.height=cardH;
  var preview=new Button(()=>{
   if(game.UiGridBoard!=null)game.UiGridBoard.selectedCardUid="";
   RefreshGridBoard();
  });
  preview.AddToClassList("ps-battle-card");
  preview.AddToClassList("ps-gboard-selected-card");
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
  float horizontalStep=count<=5?72f:count==6?62f:52f;
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
   if(!open)button.AddToClassList("ps-gboard-fan-peek");
   if(selected)button.AddToClassList("ps-gboard-fan-selected");
   PopulateGridPlaceCard(button,capture);
   float spreadIndex=i-center;
   float angle=open?spreadIndex*spreadDeg:spreadIndex*2.2f;
   float rad=angle*Mathf.Deg2Rad;
   float arcLift=open?radius*(1f-Mathf.Cos(rad)):0f;
   float baseRight=(count-1-i)*horizontalStep+8f;
   float arcShift=open?radius*Mathf.Sin(rad):spreadIndex*6f;
   button.style.position=Position.Absolute;
   button.style.right=baseRight-arcShift;
   button.style.bottom=arcLift-sink;
   button.style.rotate=new Rotate(new Angle(angle,AngleUnit.Degree));
   button.style.transformOrigin=new TransformOrigin(new Length(50,LengthUnit.Percent),new Length(100,LengthUnit.Percent));
   if(open)button.RegisterCallback<PointerEnterEvent>(_=>button.BringToFront());
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
   return;
  }
  int count=run.hand.Count;
  float center=(count-1)*0.5f;
  float spreadDeg=count<=5?6.2f:count==6?7.4f:count==7?7.0f:5.0f;
  float radius=count<=5?110f:count==6?205f:count==7?198f:155f;
  float horizontalStep=count<=5?72f:count==6?62f:52f;
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
   if(!affordable)button.AddToClassList("ps-battle-card-disabled");
   PopulateBattleCard(button,card,run,affordable);
   float spreadIndex=i-center;
   float angle=spreadIndex*spreadDeg;
   float rad=angle*Mathf.Deg2Rad;
   float arcLift=radius*(1f-Mathf.Cos(rad));
   float baseRight=(count-1-i)*horizontalStep+8f;
   float arcShift=radius*Mathf.Sin(rad);
   button.style.position=Position.Absolute;
   button.style.right=baseRight-arcShift;
   button.style.bottom=arcLift;
   button.style.rotate=new Rotate(new Angle(angle,AngleUnit.Degree));
   button.style.transformOrigin=new TransformOrigin(new Length(50,LengthUnit.Percent),new Length(100,LengthUnit.Percent));
   button.RegisterCallback<PointerEnterEvent>(_=>button.BringToFront());
   handSlots.Add((button,Mathf.Abs(spreadIndex)));
  }
  foreach(var slot in handSlots.OrderByDescending(x=>x.depth))
   gridBoardHandRoot.Add(slot.button);
 }

 void PlayGridBattleActionFx(BattleActionFx fx){
  if(gridBoardFxLayer==null||gridBoardCombatStage==null)return;
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

 void TickGridBoard(){
  if(!gridBoardBuilt)return;
  var run=game.UiGridBoard;
  if(run==null)return;
  if(game.UiBattle!=null)return;
  if(run.phase==GridBoardPhase.Run&&GridBoardSystem.TickRun(run,Time.unscaledDeltaTime))
   RefreshGridBoard();
  if(run.pendingEvent){
   run.pendingEvent=false;
   game.UiBeginGridEvent();
   return;
  }
  if(run.pendingBattle){
   run.pendingBattle=false;
   game.UiBeginGridEncounter();
   RefreshGridBoard();
  }
 }
}
}
