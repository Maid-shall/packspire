using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire {
public sealed partial class PackspireUiFoundation {
 ExplorationMapStage explorationStage;
 VisualElement explorationView,explorationRoot,explorationMist,explorationFinishDialog,explorationCompassFace;
 VisualElement explorationNeedleAlert,explorationNeedleCollapse,explorationNeedleCorruption;
 Label explorationTitleLabel,explorationTypeLabel,explorationStatusLabel,explorationBodyLabel,explorationHintLabel;
 Label explorationAxisAlert,explorationAxisCollapse,explorationAxisCorruption,explorationMapNameLabel,explorationStatsLabel;
 bool explorationMapBuilt;
 Vector2 explorationPointerDown;
 bool explorationDragging;
 int explorationPointerId=-1;
 int explorationLastAlert,explorationLastCollapse,explorationLastCorruption;
 float explorationAnomalyT;

 void EnsureExplorationStage(){
  if(explorationStage!=null)return;
  var host=new GameObject("ExplorationMapHost");
  host.transform.SetParent(transform,false);
  explorationStage=host.AddComponent<ExplorationMapStage>();
  explorationStage.Arrived+=OnExplorationArrived;
 }

 void SuspendExplorationStage(){
  explorationMapBuilt=false;
  explorationView=null;
  explorationRoot=null;
  explorationMist=null;
  explorationFinishDialog=null;
  explorationCompassFace=null;
  explorationNeedleAlert=null;
  explorationNeedleCollapse=null;
  explorationNeedleCorruption=null;
  explorationTitleLabel=null;
  explorationTypeLabel=null;
  explorationStatusLabel=null;
  explorationBodyLabel=null;
  explorationHintLabel=null;
  explorationAxisAlert=null;
  explorationAxisCollapse=null;
  explorationAxisCorruption=null;
  explorationMapNameLabel=null;
  explorationStatsLabel=null;
  explorationPointerId=-1;
  explorationStage?.SetSuspended(true);
 }

 void ReleaseExplorationStage(){
  SuspendExplorationStage();
  if(explorationStage==null)return;
  explorationStage.Arrived-=OnExplorationArrived;
  Destroy(explorationStage.gameObject);
  explorationStage=null;
 }

 void OnExplorationArrived(int nodeId,bool firstVisit){
  var run=game.UiExploration;
  if(run==null)return;
  var encounter=game.UiExplorationOnArrived(nodeId,firstVisit);
  if(encounter==ExplorationEncounter.EnterBuilding||encounter==ExplorationEncounter.ExitBuilding){
   explorationStage?.Bind(run,false);
   if(explorationMapNameLabel!=null)explorationMapNameLabel.text=ExplorationMapSystem.Breadcrumb(run);
   ShowToast(game.UiMessage);
  } else if(encounter==ExplorationEncounter.Event){
   ShowExplorationMist();
   ShowToast(game.UiMessage);
  } else if(encounter==ExplorationEncounter.Battle){
   ShowToast("敵影と接触した");
  } else if(encounter==ExplorationEncounter.Rest){
   ShowToast(game.UiMessage);
  } else if(!string.IsNullOrEmpty(game.UiMessage)){
   ShowToast(game.UiMessage);
  }
  RefreshExplorationHud();
 }

 void BuildExplorationMap(){
  var run=game.UiExploration;
  if(run==null){
   game.UiOpenExplorationMap();
   run=game.UiExploration;
  }
  bool hadStage=explorationStage!=null;
  EnsureExplorationStage();
  explorationStage.Bind(run,hadStage);
  explorationMapBuilt=true;

  explorationRoot=Container("ps-xmap ps-xmap-rite");
  screenRoot.Add(explorationRoot);

  explorationView=new Image{image=explorationStage.RenderTarget,scaleMode=ScaleMode.StretchToFill,pickingMode=PickingMode.Position};
  explorationView.AddToClassList("ps-xmap-view");
  explorationView.focusable=true;
  explorationView.RegisterCallback<PointerDownEvent>(OnExplorationPointerDown,TrickleDown.TrickleDown);
  explorationView.RegisterCallback<PointerMoveEvent>(OnExplorationPointerMove,TrickleDown.TrickleDown);
  explorationView.RegisterCallback<PointerUpEvent>(OnExplorationPointerUp,TrickleDown.TrickleDown);
  explorationView.RegisterCallback<PointerCaptureOutEvent>(_=>explorationDragging=false);
  explorationView.RegisterCallback<WheelEvent>(OnExplorationWheel,TrickleDown.TrickleDown);
  explorationRoot.Add(explorationView);

  var top=Container("ps-xmap-top");
  DressRiteFrame(top);
  var brand=Container("ps-rite-brand");
  var mark=Container("ps-rite-brand-mark");
  mark.pickingMode=PickingMode.Ignore;
  brand.Add(mark);
  var titleBlock=Container("ps-rite-top-title");
  var eye=new Label("遠征術式"){pickingMode=PickingMode.Ignore};
  eye.AddToClassList("ps-rite-top-eyebrow");
  titleBlock.Add(eye);
  explorationMapNameLabel=new Label(ExplorationMapSystem.Breadcrumb(run)){pickingMode=PickingMode.Ignore};
  explorationMapNameLabel.AddToClassList("ps-rite-top-name");
  titleBlock.Add(explorationMapNameLabel);
  var topSub=new Label("導線を辿り、封印を解け"){pickingMode=PickingMode.Ignore};
  topSub.AddToClassList("ps-xmap-top-sub");
  titleBlock.Add(topSub);
  brand.Add(titleBlock);
  top.Add(brand);
  explorationStatsLabel=new Label(""){pickingMode=PickingMode.Ignore};
  explorationStatsLabel.AddToClassList("ps-xmap-stats");
  top.Add(explorationStatsLabel);
  var topActions=Container("ps-xmap-top-actions");
  var focus=PackspireUiFactory.Button("駒へ戻る",()=>{if(!game.UiExplorationEventActive)explorationStage?.FocusPiece();});
  focus.AddToClassList("ps-rite-chip");
  topActions.Add(focus);
  var end=PackspireUiFactory.Button("遠征を終了",RequestExplorationFinish);
  end.AddToClassList("ps-rite-chip");
  topActions.Add(end);
  top.Add(topActions);
  explorationRoot.Add(top);

  var mapFrame=Container("ps-xmap-view-frame");
  mapFrame.pickingMode=PickingMode.Ignore;
  explorationRoot.Add(mapFrame);

  // Momotetsu-style: left dock = description over instrument, map fills the right.
  var dock=Container("ps-xmap-dock");
  var panel=Container("ps-xmap-panel");
  DressRiteFrame(panel);
  var panelEye=new Label("印の解釈"){pickingMode=PickingMode.Ignore};
  panelEye.AddToClassList("ps-xmap-panel-eye");
  panel.Add(panelEye);
  explorationTypeLabel=new Label(""){pickingMode=PickingMode.Ignore};
  explorationTypeLabel.AddToClassList("ps-xmap-panel-type");
  panel.Add(explorationTypeLabel);
  explorationTitleLabel=new Label(""){pickingMode=PickingMode.Ignore};
  explorationTitleLabel.AddToClassList("ps-xmap-panel-title");
  panel.Add(explorationTitleLabel);
  explorationStatusLabel=new Label(""){pickingMode=PickingMode.Ignore};
  explorationStatusLabel.AddToClassList("ps-xmap-panel-status");
  panel.Add(explorationStatusLabel);
  var rule=Container("ps-xmap-panel-rule");
  rule.pickingMode=PickingMode.Ignore;
  panel.Add(rule);
  explorationBodyLabel=new Label(""){pickingMode=PickingMode.Ignore};
  explorationBodyLabel.AddToClassList("ps-xmap-panel-body");
  panel.Add(explorationBodyLabel);
  explorationHintLabel=new Label(""){pickingMode=PickingMode.Ignore};
  explorationHintLabel.AddToClassList("ps-xmap-panel-hint");
  panel.Add(explorationHintLabel);
  dock.Add(panel);

  var compass=Container("ps-xmap-compass");
  var compassHead=Container("ps-xmap-compass-head");
  var compassTitle=new Label("三軸儀軌"){pickingMode=PickingMode.Ignore};
  compassTitle.AddToClassList("ps-xmap-compass-title");
  compassHead.Add(compassTitle);
  var compassSub=new Label("左 −15　／　頂点 0　／　右 ＋15"){pickingMode=PickingMode.Ignore};
  compassSub.AddToClassList("ps-xmap-compass-sub");
  compassHead.Add(compassSub);
  compass.Add(compassHead);
  explorationCompassFace=Container("ps-xmap-gauge");
  explorationCompassFace.pickingMode=PickingMode.Ignore;
  var dial=Container("ps-xmap-gauge-dial");
  dial.pickingMode=PickingMode.Ignore;
  explorationCompassFace.Add(dial);
  var rim=Container("ps-xmap-gauge-rim");
  rim.pickingMode=PickingMode.Ignore;
  explorationCompassFace.Add(rim);
  for(int tick=-15;tick<=15;tick++){
   bool major=tick%5==0;
   float ang=GaugeAngle(tick);
   var tickMark=Container(major?"ps-xmap-gauge-tick ps-xmap-gauge-tick-major":"ps-xmap-gauge-tick");
   tickMark.pickingMode=PickingMode.Ignore;
   tickMark.style.rotate=new Rotate(Angle.Degrees(ang));
   explorationCompassFace.Add(tickMark);
   if(major){
    var num=new Label(tick>0?"+"+tick:tick.ToString()){pickingMode=PickingMode.Ignore};
    num.AddToClassList("ps-xmap-gauge-num");
    float rad=ang*Mathf.Deg2Rad;
    const float gw=300f,gh=110f,hubY=gh+50f,radius=128f;
    num.style.left=Length.Percent((.5f+Mathf.Sin(rad)*radius/gw)*100f);
    num.style.top=Length.Percent((hubY-Mathf.Cos(rad)*radius)/gh*100f);
    explorationCompassFace.Add(num);
   }
  }
  explorationNeedleAlert=Container("ps-xmap-needle ps-xmap-needle-alert");
  explorationNeedleAlert.pickingMode=PickingMode.Ignore;
  explorationCompassFace.Add(explorationNeedleAlert);
  explorationNeedleCollapse=Container("ps-xmap-needle ps-xmap-needle-collapse");
  explorationNeedleCollapse.pickingMode=PickingMode.Ignore;
  explorationCompassFace.Add(explorationNeedleCollapse);
  explorationNeedleCorruption=Container("ps-xmap-needle ps-xmap-needle-corruption");
  explorationNeedleCorruption.pickingMode=PickingMode.Ignore;
  explorationCompassFace.Add(explorationNeedleCorruption);
  var hub=Container("ps-xmap-gauge-hub");
  hub.pickingMode=PickingMode.Ignore;
  explorationCompassFace.Add(hub);
  compass.Add(explorationCompassFace);

  var axisRow=Container("ps-xmap-axis-row");
  explorationAxisAlert=AddAxisChip(axisRow,"警戒","ps-xmap-axis-alert");
  explorationAxisCollapse=AddAxisChip(axisRow,"崩壊","ps-xmap-axis-collapse");
  explorationAxisCorruption=AddAxisChip(axisRow,"侵蝕","ps-xmap-axis-corruption");
  compass.Add(axisRow);
  dock.Add(compass);
  explorationRoot.Add(dock);
  var axes0=game.UiRun?.axes;
  explorationLastAlert=axes0?.alert??0;
  explorationLastCollapse=axes0?.collapse??0;
  explorationLastCorruption=axes0?.corruption??0;

  if(game.UiExplorationEventActive)ShowExplorationMist();
  RefreshExplorationHud();
 }

 void RequestExplorationFinish(){
  if(game.UiExplorationAtEntrance){game.UiExplorationFinish();return;}
  if(explorationRoot==null||explorationFinishDialog!=null)return;
  explorationFinishDialog=Container("ps-xmap-finish");
  var dialog=PackspireUiFactory.Dialog(
   "途中帰還の確認",
   "入口以外から遠征を終了します。戦利品と所持金は持ち帰れますが、未踏の地点は残ります。",
   "帰還する",
   ()=>{ClearExplorationFinishDialog();game.UiExplorationFinish();},
   "続ける",
   ClearExplorationFinishDialog);
  dialog.AddToClassList("ps-xmap-finish-dialog");
  explorationFinishDialog.Add(dialog);
  explorationRoot.Add(explorationFinishDialog);
 }

 void ClearExplorationFinishDialog(){
  if(explorationFinishDialog==null)return;
  explorationFinishDialog.RemoveFromHierarchy();
  explorationFinishDialog=null;
 }

 void ShowExplorationMist(){
  if(explorationRoot==null||explorationMist!=null)return;
  explorationAnomalyT=1.4f;
  explorationMist=Container("ps-xmap-mist");
  var veil=Container("ps-xmap-mist-veil");
  veil.pickingMode=PickingMode.Ignore;
  explorationMist.Add(veil);
  for(int i=0;i<4;i++){
   var cloud=Container($"ps-xmap-mist-cloud ps-xmap-mist-cloud-{i}");
   cloud.pickingMode=PickingMode.Ignore;
   explorationMist.Add(cloud);
  }
  var dialog=Container("ps-event-panel");
  dialog.AddToClassList("ps-xmap-mist-panel");
  dialog.Add(PackspireUiFactory.Title("記憶の揺らぎ"));
  dialog.Add(PackspireUiFactory.Body("周囲の導線が黒い靄に沈み、まだ記されていない選択だけが浮かび上がる。術式のうえで決断せよ。"));
  dialog.Add(MistChoice("代償を支払う","HP -6　／　24Gを獲得",0));
  dialog.Add(MistChoice("残響を修復する","所持装備の耐久をすべて回復",1));
  dialog.Add(MistChoice("靄を振り払う","何も得ず、術式図へ戻る",2));
  explorationMist.Add(dialog);
  explorationRoot.Add(explorationMist);
 }

 VisualElement MistChoice(string title,string description,int choice){
  var button=PackspireUiFactory.Card(title,description,()=>ResolveExplorationMist(choice));
  button.AddToClassList("ps-event-choice");
  return button;
 }

 void ResolveExplorationMist(int choice){
  game.UiResolveExplorationEvent(choice);
  if(explorationMist!=null){
   explorationMist.RemoveFromHierarchy();
   explorationMist=null;
  }
  if(!string.IsNullOrEmpty(game.UiMessage))ShowToast(game.UiMessage);
  RefreshExplorationHud();
 }

 void TickExplorationMap(){
  if(explorationStage==null||!explorationMapBuilt)return;
  TickExplorationCompass(Time.unscaledDeltaTime);
  if(game.UiExplorationEventActive||explorationFinishDialog!=null){
   explorationStage.Tick(0f,0f);
   return;
  }
  float x=0f,y=0f;
  if(Input.GetKey(KeyCode.A)||Input.GetKey(KeyCode.LeftArrow))x-=1f;
  if(Input.GetKey(KeyCode.D)||Input.GetKey(KeyCode.RightArrow))x+=1f;
  if(Input.GetKey(KeyCode.S)||Input.GetKey(KeyCode.DownArrow))y-=1f;
  if(Input.GetKey(KeyCode.W)||Input.GetKey(KeyCode.UpArrow))y+=1f;
  explorationStage.Tick(x,y);
 }

 Label AddAxisChip(VisualElement row,string name,string toneClass){
  var chip=Container("ps-xmap-axis-chip "+toneClass);
  var swatch=Container("ps-xmap-axis-swatch");
  swatch.pickingMode=PickingMode.Ignore;
  chip.Add(swatch);
  var nameLabel=new Label(name){pickingMode=PickingMode.Ignore};
  nameLabel.AddToClassList("ps-xmap-axis-name");
  chip.Add(nameLabel);
  var value=new Label("0"){pickingMode=PickingMode.Ignore};
  value.AddToClassList("ps-xmap-axis-value");
  chip.Add(value);
  row.Add(chip);
  return value;
 }

 void RefreshExplorationHud(){
  var run=game.UiExploration;
  var gameRun=game.UiRun;
  if(run==null||explorationTitleLabel==null)return;
  var def=ExplorationMapSystem.Def(run);
  var selected=ExplorationMapSystem.Node(def,run.selectedNodeId);
  var current=ExplorationMapSystem.Node(def,run.currentNodeId);
  if(explorationMapNameLabel!=null)explorationMapNameLabel.text=ExplorationMapSystem.Breadcrumb(run);
  var outdoor=ExplorationMapSystem.OutdoorDef(run);
  int playable=ExplorationMapSystem.PlayableCellCount(outdoor);
  int total=ExplorationMapSystem.TotalCellCount(outdoor);
  if(explorationStatsLabel!=null&&gameRun!=null)
   explorationStatsLabel.text=$"HP {gameRun.hp}/{gameRun.maxHp}　·　{gameRun.gold}G　·　戦勝 {gameRun.battlesWon}　·　区画 {playable}/{total}";

  bool canMove=selected!=null&&game.UiExplorationCanMove(selected.id);
  bool cleared=selected!=null&&ExplorationMapSystem.IsCleared(run,selected.id);
  bool sealedBreach=selected!=null&&ExplorationMapSystem.Connected(def,run.currentNodeId,selected.id)
   &&ExplorationMapSystem.LinkKind(def,run.currentNodeId,selected.id)==ExplorationLinkKind.Breach
   &&!ExplorationMapSystem.IsEdgeOpened(run,run.currentNodeId,selected.id);
  bool hiddenLink=selected!=null&&ExplorationMapSystem.Connected(def,run.currentNodeId,selected.id)
   &&ExplorationMapSystem.LinkKind(def,run.currentNodeId,selected.id)==ExplorationLinkKind.Hidden;
  string typeLabel=selected==null?"—":selected.type switch{
   "entrance"=>"遠征入口",
   "exit"=>"建物出口",
   "building_door"=>"建物入口",
   "battle"=>"戦闘の印",
   "event"=>"出来事の印",
   "rest"=>"休憩の印",
   _=>"導線の節",
  };
  explorationTitleLabel.text=selected?.name??current?.name??"遠征中";
  if(explorationTypeLabel!=null)explorationTypeLabel.text=typeLabel+(cleared?" · 踏破済":"")+(sealedBreach?" · 封印":hiddenLink?" · 隠し導線":"");
  string status=selected==null
   ?"印を選んでください"
   :sealedBreach?"封印された導線 — タップで破る"
   :canMove?"隣接しています — タップで辿る"
   :selected.id==run.currentNodeId?"いま立っている印です"
   :selected.locked?"この層はまだ開いていません"
   :"ここへは直接辿れません";
  if(explorationStatusLabel!=null)explorationStatusLabel.text=status;
  if(explorationBodyLabel!=null)
   explorationBodyLabel.text=$"現在地　{current?.name??"—"}\n層　{ExplorationMapSystem.Breadcrumb(run)}\n\n{(selected==null?"術式図の印を選ぶと、解釈がここに現れます。":selected.landmark?"要の印です。到達すると出来事が起きることがあります。":"導線上の節です。隣へ辿って術式を広げましょう。")}";
  if(explorationHintLabel!=null)
   explorationHintLabel.text=game.UiExplorationEventActive
    ?"操作　靄のあいだで選択してください"
    :game.UiExplorationAtEntrance
     ?"操作　入口にいます。遠征終了で持ち帰れます"
     :"操作　ドラッグ / WASD　·　ホイール拡大　·　印タップ　·　封印はタップで破砕";

  var axes=gameRun?.axes;
  if(explorationAxisAlert!=null){
   explorationAxisAlert.text=FormatAxis(axes?.alert??0);
   explorationAxisCollapse.text=FormatAxis(axes?.collapse??0);
   explorationAxisCorruption.text=FormatAxis(axes?.corruption??0);
  }
  explorationStage?.SetSelectedVisual(run.selectedNodeId);
 }

 void TickExplorationCompass(float dt){
  var axes=game.UiRun?.axes;
  if(axes==null||explorationNeedleAlert==null)return;
  if(axes.alert!=explorationLastAlert||axes.collapse!=explorationLastCollapse||axes.corruption!=explorationLastCorruption){
   if(IsAxisThresholdCross(explorationLastAlert,axes.alert)||IsAxisThresholdCross(explorationLastCollapse,axes.collapse)||IsAxisThresholdCross(explorationLastCorruption,axes.corruption))
    explorationAnomalyT=1.25f;
   else explorationAnomalyT=Mathf.Max(explorationAnomalyT,.45f);
   explorationLastAlert=axes.alert;explorationLastCollapse=axes.collapse;explorationLastCorruption=axes.corruption;
  }
  if(explorationAnomalyT>0f)explorationAnomalyT=Mathf.Max(0f,explorationAnomalyT-dt);
  float t=Time.unscaledTime;
  float anomaly=explorationAnomalyT;
  SetNeedle(explorationNeedleAlert,axes.alert,t,1.15f,anomaly,1f);
  SetNeedle(explorationNeedleCollapse,axes.collapse,t*1.13f,.85f,anomaly,.78f);
  SetNeedle(explorationNeedleCorruption,axes.corruption,t*.91f,.55f,anomaly,.58f);
 }

 /// <summary>Bottom-peek circle: −15 left, 0 at top, +15 right (±60° of the visible cap).</summary>
 static float GaugeAngle(float axis){
  float n=Mathf.Clamp(axis,-15f,15f)/15f;
  return n*60f;
 }

 static bool IsAxisThresholdCross(int from,int to){
  int[] marks={-15,-13,-9,-5,5,9,13,15};
  foreach(int m in marks)if((from<m&&to>=m)||(from>m&&to<=m)||Mathf.Abs(to)==15&&from!=to)return true;
  return false;
 }

 static void SetNeedle(VisualElement needle,int axis,float time,float jitterScale,float anomaly,float lengthScale){
  if(needle==null)return;
  float n=Mathf.Clamp(axis,-15,15)/15f;
  float swing=Mathf.Abs(n)*.22f+.05f;
  float jitter=Mathf.Sin(time*5.4f*(.7f+Mathf.Abs(n)))*swing*6f*jitterScale;
  float spike=anomaly>0f?Mathf.Sin(anomaly*26f)*10f*anomaly*jitterScale:0f;
  float angle=Mathf.Clamp(GaugeAngle(axis)+jitter+spike,-65f,65f);
  needle.style.rotate=new Rotate(Angle.Degrees(angle));
  needle.style.scale=new Scale(new Vector2(1f,lengthScale));
 }

 static string FormatAxis(int value)=>value>0?$"+{value}":value.ToString();

 void TryExplorationMoveTo(int nodeId){
  var run=game.UiExploration;
  if(run==null||explorationStage==null||explorationStage.IsMoving||game.UiExplorationEventActive)return;
  game.UiExplorationSelect(nodeId);
  if(nodeId==run.currentNodeId){
   var node=ExplorationMapSystem.Node(ExplorationMapSystem.Def(run),nodeId);
   if(node!=null&&(node.type=="exit"||(node.type=="building_door"&&!string.IsNullOrEmpty(node.interiorMapId)))){
    var encounter=game.UiExplorationOnArrived(nodeId,false);
    if(encounter==ExplorationEncounter.EnterBuilding||encounter==ExplorationEncounter.ExitBuilding){
     explorationStage.Bind(run,false);
     ShowToast(game.UiMessage);
    } else if(node.type=="building_door")ShowToast(string.IsNullOrEmpty(game.UiMessage)?"鍵がかかっている":game.UiMessage);
   }
   RefreshExplorationHud();
   return;
  }
  if(!game.UiExplorationMove(nodeId)){
   RefreshExplorationHud();
   ShowToast("つながる道がありません");
   return;
  }
  explorationStage.BeginMoveTo(nodeId);
  RefreshExplorationHud();
 }

 Vector2 ExplorationPanelSize(){
  if(explorationView==null)return Vector2.zero;
  var rect=explorationView.contentRect.size;
  if(rect.x>=2f&&rect.y>=2f)return rect;
  float w=explorationView.resolvedStyle.width;
  float h=explorationView.resolvedStyle.height;
  if(w>=2f&&h>=2f)return new Vector2(w,h);
  return new Vector2(Screen.width,Screen.height);
 }

 void OnExplorationPointerDown(PointerDownEvent evt){
  if(explorationView==null||explorationStage==null||game.UiExplorationEventActive||explorationFinishDialog!=null)return;
  if(evt.button!=0&&evt.button!=2)return;
  Vector2 local=(Vector2)evt.localPosition;
  explorationPointerDown=local;
  explorationDragging=false;
  explorationPointerId=evt.pointerId;
  explorationStage.BeginPan(local,ExplorationPanelSize());
  explorationView.CapturePointer(evt.pointerId);
  explorationView.Focus();
  evt.StopPropagation();
 }

 void OnExplorationPointerMove(PointerMoveEvent evt){
  if(explorationView==null||explorationStage==null||game.UiExplorationEventActive)return;
  if(explorationPointerId>=0&&evt.pointerId!=explorationPointerId)return;
  Vector2 local=(Vector2)evt.localPosition;
  if((local-explorationPointerDown).sqrMagnitude>12f){
   explorationDragging=true;
   explorationStage.UpdatePan(local,ExplorationPanelSize(),true);
  }
  evt.StopPropagation();
 }

 void OnExplorationPointerUp(PointerUpEvent evt){
  if(explorationView==null||explorationStage==null)return;
  Vector2 local=(Vector2)evt.localPosition;
  if(explorationView.HasPointerCapture(evt.pointerId))
   explorationView.ReleasePointer(evt.pointerId);
  explorationStage.EndPan();
  if(!game.UiExplorationEventActive&&explorationFinishDialog==null&&!explorationDragging){
   if(explorationStage.TryPickNode(local,ExplorationPanelSize(),out int nodeId))
    TryExplorationMoveTo(nodeId);
  }
  explorationDragging=false;
  explorationPointerId=-1;
  evt.StopPropagation();
 }

 void OnExplorationWheel(WheelEvent evt){
  if(explorationStage==null||game.UiExplorationEventActive)return;
  explorationStage.ApplyWheelDelta(evt.delta.y);
  evt.StopPropagation();
 }
}
}
