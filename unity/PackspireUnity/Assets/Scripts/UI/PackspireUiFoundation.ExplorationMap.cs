using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire {
public sealed partial class PackspireUiFoundation {
 ExplorationMapStage explorationStage;
 Image explorationView;
 VisualElement explorationRoot,explorationMist,explorationFinishDialog,explorationCompassFace;
 VisualElement explorationNeedleAlert,explorationNeedleCollapse,explorationNeedleCorruption;
 VisualElement explorationSketch,explorationDock,explorationHud,explorationTopActions;
 VisualElement explorationDetailPopup,explorationConfirmPopup;
 Label explorationTitleLabel,explorationTypeLabel,explorationStatusLabel,explorationBodyLabel,explorationHintLabel;
 Label explorationAxisAlert,explorationAxisCollapse,explorationAxisCorruption,explorationMapNameLabel,explorationStatsLabel;
 Label explorationConfirmTitle,explorationConfirmKind,explorationConfirmBody;
 Button explorationFocusButton,explorationFinishButton;
 bool explorationMapBuilt;
 Vector2 explorationPointerDown;
 bool explorationDragging;
 int explorationPointerId=-1;
 int explorationLastAlert,explorationLastCollapse,explorationLastCorruption;
 int explorationPendingConfirmId=-1;
 float explorationAnomalyT;

 bool ExplorationAnyMoving=>explorationStage!=null&&explorationStage.IsMoving;

 void EnsureExplorationStage(){
  if(explorationStage!=null)return;
  var host=new GameObject("ExplorationMapHost");
  host.transform.SetParent(transform,false);
  explorationStage=host.AddComponent<ExplorationMapStage>();
  explorationStage.Arrived+=OnExplorationArrived;
 }

 void SuspendExplorationStage(){
  explorationMapBuilt=false;
  explorationTopActions=null;
  explorationFocusButton=null;explorationFinishButton=null;
  explorationView=null;explorationRoot=null;explorationMist=null;explorationFinishDialog=null;
  explorationCompassFace=null;explorationSketch=null;explorationDock=null;explorationHud=null;
  explorationDetailPopup=null;explorationConfirmPopup=null;
  explorationTitleLabel=null;explorationTypeLabel=null;explorationStatusLabel=null;
  explorationBodyLabel=null;explorationHintLabel=null;
  explorationAxisAlert=null;explorationAxisCollapse=null;explorationAxisCorruption=null;
  explorationMapNameLabel=null;explorationStatsLabel=null;
  explorationConfirmTitle=null;explorationConfirmKind=null;explorationConfirmBody=null;
  explorationNeedleAlert=null;explorationNeedleCollapse=null;explorationNeedleCorruption=null;
  explorationPointerId=-1;explorationPendingConfirmId=-1;
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
   ShowExplorationEventDialog();
   ShowToast(game.UiMessage);
  } else if(encounter==ExplorationEncounter.Battle){
   ShowToast("敵影と接触した");
  } else if(encounter==ExplorationEncounter.Rest){
   ShowToast(game.UiMessage);
  } else if(!string.IsNullOrEmpty(game.UiMessage)){
   ShowToast(game.UiMessage);
  }
  RefreshExplorationHud();
  RefreshExplorationSketch();
 }

 void SyncExplorationViewImage(){
  if(explorationView==null||explorationStage==null)return;
  var rt=explorationStage.RenderTarget;
  if(rt!=null&&explorationView.image!=rt)explorationView.image=rt;
 }

 void BuildExplorationMap(){
  var run=game.UiExploration;
  if(run==null){
   game.UiDevOpenExplorationMap();
   run=game.UiExploration;
  }
  bool hadStage=explorationStage!=null;
  EnsureExplorationStage();
  explorationStage.SetSuspended(false);
  explorationStage.Bind(run,hadStage);
  explorationMapBuilt=true;

  explorationRoot=Container("ps-xmap");
  screenRoot.Add(explorationRoot);

  explorationView=new Image{
   image=explorationStage.RenderTarget,
   scaleMode=ScaleMode.StretchToFill,
   pickingMode=PickingMode.Position,
  };
  explorationView.AddToClassList("ps-xmap-view");
  explorationView.focusable=true;
  explorationView.RegisterCallback<GeometryChangedEvent>(_=>SyncExplorationViewSize());
  explorationView.RegisterCallback<PointerDownEvent>(OnExplorationPointerDown,TrickleDown.TrickleDown);
  explorationView.RegisterCallback<PointerMoveEvent>(OnExplorationPointerMove,TrickleDown.TrickleDown);
  explorationView.RegisterCallback<PointerUpEvent>(OnExplorationPointerUp,TrickleDown.TrickleDown);
  explorationView.RegisterCallback<PointerCaptureOutEvent>(_=>explorationDragging=false);
  explorationView.RegisterCallback<WheelEvent>(OnExplorationWheel,TrickleDown.TrickleDown);
  explorationRoot.Add(explorationView);

  explorationSketch=Container("ps-xmap-sketch");
  explorationSketch.pickingMode=PickingMode.Ignore;
  explorationRoot.Add(explorationSketch);

  var top=Container("ps-xmap-top");
  DressRiteFrame(top);
  var brand=Container("ps-rite-brand");
  var mark=Container("ps-rite-brand-mark");
  mark.pickingMode=PickingMode.Ignore;
  brand.Add(mark);
  var titleBlock=Container("ps-rite-top-title");
  var eye=new Label("EXPEDITION  /  MAP"){pickingMode=PickingMode.Ignore};
  eye.AddToClassList("ps-rite-top-eyebrow");
  eye.AddToClassList("ps-chrome-eyebrow");
  titleBlock.Add(eye);
  explorationMapNameLabel=new Label(ExplorationMapSystem.Breadcrumb(run)){pickingMode=PickingMode.Ignore};
  explorationMapNameLabel.AddToClassList("ps-rite-top-name");
  titleBlock.Add(explorationMapNameLabel);
  var topSub=new Label("探索地図"){pickingMode=PickingMode.Ignore};
  topSub.AddToClassList("ps-xmap-top-sub");
  titleBlock.Add(topSub);
  brand.Add(titleBlock);
  top.Add(brand);
  explorationStatsLabel=new Label(""){pickingMode=PickingMode.Ignore};
  explorationStatsLabel.AddToClassList("ps-xmap-stats");
  top.Add(explorationStatsLabel);
  explorationTopActions=Container("ps-xmap-top-actions");
  explorationFocusButton=PackspireUiFactory.Button("現在地へ",()=>{
   if(game.UiExplorationEventActive)return;
   explorationStage?.FocusPiece();
  });
  explorationFocusButton.AddToClassList("ps-rite-chip");
  explorationTopActions.Add(explorationFocusButton);
  top.Add(explorationTopActions);
  explorationRoot.Add(top);

  var mapFrame=Container("ps-xmap-view-frame");
  mapFrame.pickingMode=PickingMode.Ignore;
  explorationRoot.Add(mapFrame);

  explorationHud=Container("ps-xmap-hud");
  explorationHud.style.display=DisplayStyle.None;
  explorationRoot.Add(explorationHud);

  BuildExplorationConfirmPopup();

  explorationFinishButton=PackspireUiFactory.Button("遠征終了",RequestExplorationFinish);
  explorationFinishButton.AddToClassList("ps-xmap-finish-chip");
  explorationRoot.Add(explorationFinishButton);

  explorationDock=Container("ps-xmap-dock");
  var dock=explorationDock;
  var panel=Container("ps-xmap-panel");
  DressRiteFrame(panel);
  explorationTypeLabel=new Label(""){pickingMode=PickingMode.Ignore};
  explorationTypeLabel.AddToClassList("ps-xmap-panel-type");
  panel.Add(explorationTypeLabel);
  explorationTitleLabel=new Label(""){pickingMode=PickingMode.Ignore};
  explorationTitleLabel.AddToClassList("ps-xmap-panel-title");
  panel.Add(explorationTitleLabel);
  explorationStatusLabel=new Label(""){pickingMode=PickingMode.Ignore};
  explorationStatusLabel.AddToClassList("ps-xmap-panel-status");
  panel.Add(explorationStatusLabel);
  explorationBodyLabel=new Label(""){pickingMode=PickingMode.Ignore};
  explorationBodyLabel.AddToClassList("ps-xmap-panel-body");
  panel.Add(explorationBodyLabel);
  explorationHintLabel=new Label(""){pickingMode=PickingMode.Ignore};
  explorationHintLabel.AddToClassList("ps-xmap-panel-hint");
  panel.Add(explorationHintLabel);
  dock.Add(panel);
  var compass=Container("ps-xmap-compass");
  explorationCompassFace=Container("ps-xmap-gauge");
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

  if(game.UiExplorationEventActive)ShowExplorationEventDialog();
  SyncExplorationViewImage();
  SyncExplorationViewSize();
  RefreshExplorationHud();
  RefreshExplorationSketch();
 }

 void BuildExplorationConfirmPopup(){
  explorationConfirmPopup=Container("ps-xmap-confirm");
  explorationConfirmPopup.pickingMode=PickingMode.Position;
  explorationConfirmPopup.style.display=DisplayStyle.None;
  explorationConfirmTitle=new Label(""){pickingMode=PickingMode.Ignore};
  explorationConfirmTitle.AddToClassList("ps-xmap-confirm-title");
  explorationConfirmPopup.Add(explorationConfirmTitle);
  explorationConfirmKind=new Label(""){pickingMode=PickingMode.Ignore};
  explorationConfirmKind.AddToClassList("ps-xmap-confirm-kind");
  explorationConfirmPopup.Add(explorationConfirmKind);
  explorationConfirmBody=new Label(""){pickingMode=PickingMode.Ignore};
  explorationConfirmBody.AddToClassList("ps-xmap-confirm-body");
  explorationConfirmPopup.Add(explorationConfirmBody);
  var actions=Container("ps-xmap-confirm-actions");
  var go=PackspireUiFactory.Button("進む",ConfirmExplorationPending);
  go.AddToClassList("ps-xmap-confirm-go");
  actions.Add(go);
  var back=PackspireUiFactory.Button("戻る",ClearExplorationConfirm);
  back.AddToClassList("ps-xmap-confirm-back");
  actions.Add(back);
  explorationConfirmPopup.Add(actions);
  explorationRoot.Add(explorationConfirmPopup);
 }

 void ClearExplorationConfirm(){
  explorationPendingConfirmId=-1;
  if(explorationConfirmPopup!=null)explorationConfirmPopup.style.display=DisplayStyle.None;
 }

 static string LinkKindLabel(ExplorationLinkKind kind,bool opened,bool investigate){
  if(investigate)return "隠し道";
  return kind switch{
   ExplorationLinkKind.Breach=>opened?"瓦礫の道":"封鎖",
   ExplorationLinkKind.Hidden=>"隠し道",
   _=>"道",
  };
 }

 void ShowExplorationConfirm(int nodeId,ExplorationLinkKind kind,bool opened,bool investigate){
  explorationPendingConfirmId=nodeId;
  if(explorationConfirmPopup==null)return;
  var run=game.UiExploration;
  var def=ExplorationMapSystem.Def(run);
  var target=ExplorationMapSystem.Node(def,nodeId);
  explorationConfirmTitle.text=investigate?"調べる":$"{target?.name??"道"}へ";
  explorationConfirmKind.text=LinkKindLabel(kind,opened,investigate);
  explorationConfirmBody.text=investigate?"壁の違和感を確かめる"
   :kind==ExplorationLinkKind.Breach&&!opened?"瓦礫を壊して通路を開く"
   :target?.type=="building_door"?"建物の入口から中へ進む"
   :"この進路を進む";
  explorationConfirmPopup.style.left=Length.Percent(50);
  explorationConfirmPopup.style.top=Length.Percent(42);
  explorationConfirmPopup.style.translate=new Translate(Length.Percent(-50),Length.Percent(-50));
  explorationConfirmPopup.style.display=DisplayStyle.Flex;
 }

 void ConfirmExplorationPending(){
  int id=explorationPendingConfirmId;
  ClearExplorationConfirm();
  if(id<0)return;
  ExecuteExplorationPath(id,true);
 }

 void RefreshExplorationSketch(){
  if(explorationSketch==null)return;
  explorationSketch.Clear();
  explorationSketch.style.display=DisplayStyle.None;
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

 void ShowExplorationEventDialog(){
  if(explorationRoot==null||explorationMist!=null)return;
  explorationAnomalyT=1.4f;
  explorationMist=Container("ps-xmap-mist");
  var panel=Container("ps-xmap-mist-panel");
  DressRiteFrame(panel);
  var title=new Label("記憶の揺らぎ"){pickingMode=PickingMode.Ignore};
  title.AddToClassList("ps-xmap-mist-title");
  panel.Add(title);
  var body=new Label("周囲の道が黒い靄に沈み、まだ記されていない選択だけが浮かび上がる。"){pickingMode=PickingMode.Ignore};
  body.AddToClassList("ps-xmap-mist-body");
  panel.Add(body);
  var actions=Container("ps-xmap-mist-actions");
  actions.Add(MakeEventChoiceButton("代償を支払う",0));
  actions.Add(MakeEventChoiceButton("残響を修復する",1));
  actions.Add(MakeEventChoiceButton("靄を振り払う",2));
  panel.Add(actions);
  explorationMist.Add(panel);
  explorationRoot.Add(explorationMist);
  explorationMist.BringToFront();
 }

 Button MakeEventChoiceButton(string label,int choice){
  var button=PackspireUiFactory.Button(label,()=>ResolveExplorationEvent(choice));
  button.AddToClassList("ps-xmap-mist-choice");
  return button;
 }

 void ResolveExplorationEvent(int choice){
  game.UiResolveExplorationEvent(choice);
  if(explorationMist!=null){
   explorationMist.RemoveFromHierarchy();
   explorationMist=null;
  }
  if(!string.IsNullOrEmpty(game.UiMessage))ShowToast(game.UiMessage);
  RefreshExplorationHud();
  RefreshExplorationSketch();
 }

 void TickExplorationMap(){
  if(!explorationMapBuilt)return;
  TickExplorationCompass(Time.unscaledDeltaTime);
  bool blockInput=game.UiExplorationEventActive||explorationFinishDialog!=null||explorationPendingConfirmId>=0||explorationMist!=null;
  float x=0f,y=0f;
  if(!blockInput){
   if(Input.GetKey(KeyCode.A)||Input.GetKey(KeyCode.LeftArrow))x-=1f;
   if(Input.GetKey(KeyCode.D)||Input.GetKey(KeyCode.RightArrow))x+=1f;
   if(Input.GetKey(KeyCode.S)||Input.GetKey(KeyCode.DownArrow))y-=1f;
   if(Input.GetKey(KeyCode.W)||Input.GetKey(KeyCode.UpArrow))y+=1f;
  }
  explorationStage?.Tick(x,y);
  SyncExplorationViewImage();
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
  if(run==null)return;
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
   "battle"=>"戦闘地点",
   "event"=>"出来事",
   "rest"=>"休憩地点",
   _=>"道",
  };
  if(explorationTitleLabel!=null)
   explorationTitleLabel.text=selected?.name??current?.name??"遠征中";
  if(explorationTypeLabel!=null)
   explorationTypeLabel.text=typeLabel+(cleared?" · 踏破済":"")+(sealedBreach?" · 封鎖":hiddenLink?" · 隠し道":"");
  string status=selected==null
   ?"出口を選んでください"
   :sealedBreach?"封鎖された出口 — タップで破壊"
   :canMove?"隣接しています — タップで進む"
   :selected.id==run.currentNodeId?"いま立っている地点です"
   :selected.locked?"この地区はまだ開いていません"
   :"ここへは直接進めません";
  if(explorationStatusLabel!=null)explorationStatusLabel.text=status;
  if(explorationBodyLabel!=null)
   explorationBodyLabel.text=$"現在地　{current?.name??"—"}\n地区　{ExplorationMapSystem.Breadcrumb(run)}\n\n{(selected==null?"前方の道・門・脇道を選ぶと、詳細がここに表示されます。":selected.landmark?"目印のある地点です。到着すると出来事が起きることがあります。":"道沿いの地点です。出口を選んで探索を広げましょう。")}";
  if(explorationHintLabel!=null)
   explorationHintLabel.text=game.UiExplorationEventActive?"靄のあいだで選択":"";

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
  if(axes==null)return;
  if(axes.alert!=explorationLastAlert||axes.collapse!=explorationLastCollapse||axes.corruption!=explorationLastCorruption){
   if(IsAxisThresholdCross(explorationLastAlert,axes.alert)||IsAxisThresholdCross(explorationLastCollapse,axes.collapse)||IsAxisThresholdCross(explorationLastCorruption,axes.corruption))
    explorationAnomalyT=1.25f;
   else explorationAnomalyT=Mathf.Max(explorationAnomalyT,.45f);
   explorationLastAlert=axes.alert;explorationLastCollapse=axes.collapse;explorationLastCorruption=axes.corruption;
  }
  if(explorationAnomalyT>0f)explorationAnomalyT=Mathf.Max(0f,explorationAnomalyT-dt);
  if(explorationNeedleAlert!=null){
   float t=Time.unscaledTime;
   float anomaly=explorationAnomalyT;
   SetNeedle(explorationNeedleAlert,axes.alert,t,1.15f,anomaly,1f);
   SetNeedle(explorationNeedleCollapse,axes.collapse,t*1.13f,.85f,anomaly,.78f);
   SetNeedle(explorationNeedleCorruption,axes.corruption,t*.91f,.55f,anomaly,.58f);
  }
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

 static float GaugeAngle(float axis){
  float n=Mathf.Clamp(axis,-15f,15f)/15f;
  return n*60f;
 }

 static string FormatAxis(int value)=>value>0?$"+{value}":value.ToString();

 void TryExplorationMoveTo(int nodeId)=>ExecuteExplorationPath(nodeId,false);

 void ExecuteExplorationPath(int nodeId,bool confirmed){
  var run=game.UiExploration;
  if(run==null||ExplorationAnyMoving||game.UiExplorationEventActive)return;
  var def=ExplorationMapSystem.Def(run);

  if(nodeId==run.currentNodeId){
   game.UiExplorationSelect(nodeId);
   var node=ExplorationMapSystem.Node(def,nodeId);
   if(node!=null&&(node.type=="exit"||(node.type=="building_door"&&!string.IsNullOrEmpty(node.interiorMapId)))){
    if(!confirmed){
     ShowExplorationConfirm(nodeId,ExplorationLinkKind.Normal,true,false);
     return;
    }
    var encounter=game.UiExplorationOnArrived(nodeId,false);
    if(encounter==ExplorationEncounter.EnterBuilding||encounter==ExplorationEncounter.ExitBuilding){
     explorationStage?.Bind(run,false);
     ShowToast(game.UiMessage);
    } else if(node.type=="building_door")ShowToast(string.IsNullOrEmpty(game.UiMessage)?"鍵がかかっている":game.UiMessage);
   }
   RefreshExplorationHud();
   RefreshExplorationSketch();
   return;
  }

  var kind=ExplorationMapSystem.Connected(def,run.currentNodeId,nodeId)
   ?ExplorationMapSystem.LinkKind(def,run.currentNodeId,nodeId):ExplorationLinkKind.Normal;
  bool hiddenKnown=kind!=ExplorationLinkKind.Hidden||ExplorationMapSystem.IsHiddenKnown(run,run.currentNodeId,nodeId);
  bool investigate=kind==ExplorationLinkKind.Hidden&&!hiddenKnown;
  bool opened=kind!=ExplorationLinkKind.Breach||ExplorationMapSystem.IsEdgeOpened(run,run.currentNodeId,nodeId);
  bool needsConfirm=investigate||(!opened&&kind==ExplorationLinkKind.Breach);
  if(needsConfirm&&!confirmed){
   ShowExplorationConfirm(nodeId,kind,opened,investigate);
   return;
  }

  if(investigate||(kind==ExplorationLinkKind.Breach&&!opened)){
   if(ExplorationMapSystem.TryUnlockEdge(run,run.currentNodeId,nodeId)){
    if(kind==ExplorationLinkKind.Breach&&game.UiRun?.axes!=null)
     game.UiRun.axes.Change(0,2,0);
    ShowToast(kind==ExplorationLinkKind.Breach?"封鎖を破った":"隠し道を見つけた");
    explorationStage?.Bind(run,true);
    if(investigate||kind!=ExplorationLinkKind.Breach){
     RefreshExplorationHud();
     RefreshExplorationSketch();
     return;
    }
    opened=true;
   } else if(investigate||!opened){
    RefreshExplorationHud();
    return;
   }
  }

  string nk=ExplorationMapSystem.NodeKey(run.activeMapId,nodeId);
  if(!run.revealed.Contains(nk))run.revealed.Add(nk);
  game.UiExplorationSelect(nodeId);
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

 void SyncExplorationViewSize(){
  if(explorationView==null||explorationStage==null)return;
  var size=ExplorationPanelSize();
  if(size.x<2f||size.y<2f)return;
  float spp=explorationView.panel!=null?explorationView.panel.scaledPixelsPerPoint:1f;
  explorationStage.SetViewPixelSize(new Vector2(size.x*spp,size.y*spp));
  SyncExplorationViewImage();
 }

 void OnExplorationPointerDown(PointerDownEvent evt){
  if(explorationView==null||game.UiExplorationEventActive||explorationFinishDialog!=null||explorationMist!=null)return;
  if(evt.button!=0&&evt.button!=2)return;
  Vector2 local=(Vector2)evt.localPosition;
  explorationPointerDown=local;
  explorationDragging=false;
  explorationPointerId=evt.pointerId;
  explorationStage?.BeginPan(local,ExplorationPanelSize());
  explorationView.CapturePointer(evt.pointerId);
  explorationView.Focus();
  evt.StopPropagation();
 }

 void OnExplorationPointerMove(PointerMoveEvent evt){
  if(explorationView==null||game.UiExplorationEventActive)return;
  if(explorationPointerId>=0&&evt.pointerId!=explorationPointerId)return;
  Vector2 local=(Vector2)evt.localPosition;
  if((local-explorationPointerDown).sqrMagnitude>12f){
   explorationDragging=true;
   explorationStage?.UpdatePan(local,ExplorationPanelSize(),true);
  }
  evt.StopPropagation();
 }

 void OnExplorationPointerUp(PointerUpEvent evt){
  if(explorationView==null)return;
  Vector2 local=(Vector2)evt.localPosition;
  if(explorationView.HasPointerCapture(evt.pointerId))
   explorationView.ReleasePointer(evt.pointerId);
  explorationStage?.EndPan();
  if(!game.UiExplorationEventActive&&explorationFinishDialog==null&&explorationMist==null&&!explorationDragging){
   if(explorationStage!=null&&explorationStage.TryPickNode(local,ExplorationPanelSize(),out int nodeId))
    TryExplorationMoveTo(nodeId);
   else ClearExplorationConfirm();
  }
  explorationDragging=false;
  explorationPointerId=-1;
  evt.StopPropagation();
 }

 void OnExplorationWheel(WheelEvent evt){
  if(game.UiExplorationEventActive)return;
  explorationStage?.ApplyWheelDelta(evt.delta.y);
  evt.StopPropagation();
 }
}
}
