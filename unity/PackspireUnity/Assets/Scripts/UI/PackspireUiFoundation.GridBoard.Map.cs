using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire {
public sealed partial class PackspireUiFoundation {
// Board cells, pointer input, pan/zoom, map layout, and actor placement.
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

}
}
