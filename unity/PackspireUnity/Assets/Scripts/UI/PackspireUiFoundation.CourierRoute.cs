using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire {
public sealed partial class PackspireUiFoundation {
 VisualElement courierRouteShell,courierRouteLinkHost,courierRouteNodeHost,courierRouteNodeDetail,courierRouteSealHost;
 VisualElement courierRoutePortraitHost,courierRouteRecoveredGroup,courierRouteDeadlineMeter,courierRouteSealDrawer;
 Label courierRouteDestination,courierRouteDays,courierRouteHp,courierRouteRecovered;
 Label courierRouteDetailEyebrow,courierRouteDetailTitle,courierRouteDetailSubtitle,courierRouteDetailKeyA,courierRouteDetailValueA;
 Label courierRouteDetailKeyB,courierRouteDetailValueB,courierRouteDetailSection,courierRouteDetailBody;
 Label courierRouteSealDetailName,courierRouteSealDetailEffect;
 ProgressBar courierRouteHpBar;
 ScrollView courierRouteSealScroll;
 Button courierRouteFinish,courierRouteRelayPack,courierRouteSealApply,courierRouteSealLeft,courierRouteSealRight,courierRouteSealDrawerToggle;
 string courierRouteDetailSealKey="";
 string courierRouteInspectedNodeId="";
 bool courierRouteSealDrawerOpen;

 void BuildCourierRoute(){
  var state=game.UiCourierRoute;
  if(state==null){game.UiNavigate(ScreenId.Expedition);return;}
  courierRouteShell=CloneView("UI/PackspireCourierRouteView","ps-courier-route-shell");
  courierRouteLinkHost=RequireViewElement<VisualElement>(courierRouteShell,"route-link-host");
  courierRouteNodeHost=RequireViewElement<VisualElement>(courierRouteShell,"route-node-host");
  courierRouteNodeDetail=RequireViewElement<VisualElement>(courierRouteShell,"route-node-detail");
  courierRouteSealHost=RequireViewElement<VisualElement>(courierRouteShell,"route-seal-host");
  courierRoutePortraitHost=RequireViewElement<VisualElement>(courierRouteShell,"route-portrait-host");
  courierRouteRecoveredGroup=RequireViewElement<VisualElement>(courierRouteShell,"route-recovered-group");
  courierRouteDeadlineMeter=RequireViewElement<VisualElement>(courierRouteShell,"route-deadline-meter");
  courierRouteSealDrawer=RequireViewElement<VisualElement>(courierRouteShell,"route-seal-drawer");
  courierRouteDestination=RequireViewElement<Label>(courierRouteShell,"route-destination");
  courierRouteDays=RequireViewElement<Label>(courierRouteShell,"route-days");
  courierRouteHp=RequireViewElement<Label>(courierRouteShell,"route-hp");
  courierRouteRecovered=RequireViewElement<Label>(courierRouteShell,"route-recovered");
  courierRouteDetailEyebrow=RequireViewElement<Label>(courierRouteShell,"route-detail-eyebrow");
  courierRouteDetailTitle=RequireViewElement<Label>(courierRouteShell,"route-detail-title");
  courierRouteDetailSubtitle=RequireViewElement<Label>(courierRouteShell,"route-detail-subtitle");
  courierRouteDetailKeyA=RequireViewElement<Label>(courierRouteShell,"route-detail-key-a");
  courierRouteDetailValueA=RequireViewElement<Label>(courierRouteShell,"route-detail-value-a");
  courierRouteDetailKeyB=RequireViewElement<Label>(courierRouteShell,"route-detail-key-b");
  courierRouteDetailValueB=RequireViewElement<Label>(courierRouteShell,"route-detail-value-b");
  courierRouteDetailSection=RequireViewElement<Label>(courierRouteShell,"route-detail-section");
  courierRouteDetailBody=RequireViewElement<Label>(courierRouteShell,"route-detail-body");
  courierRouteSealDetailName=RequireViewElement<Label>(courierRouteShell,"route-seal-detail-name");
  courierRouteSealDetailEffect=RequireViewElement<Label>(courierRouteShell,"route-seal-detail-effect");
  courierRouteHpBar=RequireViewElement<ProgressBar>(courierRouteShell,"route-hp-bar");
  courierRouteSealScroll=RequireViewElement<ScrollView>(courierRouteShell,"route-seal-scroll");
  courierRouteFinish=RequireViewElement<Button>(courierRouteShell,"route-finish");
  courierRouteRelayPack=RequireViewElement<Button>(courierRouteShell,"route-relay-pack");
  courierRouteSealApply=RequireViewElement<Button>(courierRouteShell,"route-seal-apply");
  courierRouteSealLeft=RequireViewElement<Button>(courierRouteShell,"route-seal-left");
  courierRouteSealRight=RequireViewElement<Button>(courierRouteShell,"route-seal-right");
  courierRouteSealDrawerToggle=RequireViewElement<Button>(courierRouteShell,"route-seal-drawer-toggle");
  courierRouteFinish.clicked+=FinishCourierRoute;
  courierRouteRelayPack.clicked+=game.UiCourierRouteOpenRelayPacking;
  courierRouteSealApply.clicked+=ApplyCourierRouteSeal;
  courierRouteSealLeft.clicked+=()=>ScrollCourierSeals(-1);
  courierRouteSealRight.clicked+=()=>ScrollCourierSeals(1);
  courierRouteSealDrawerToggle.clicked+=ToggleCourierSealDrawer;
  BuildCourierRouteTravel();
  screenRoot.Add(courierRouteShell);
  BuildCourierRouteMapInteraction();
  PopulateCourierRoute();
  RestoreCourierRouteTravelState();
 }

 void PopulateCourierRoute(){
  var state=game.UiCourierRoute;
  var run=game.UiRun;
  if(state==null||run==null)return;
  var destination=CourierRouteSystem.Node("destination");
  courierRouteDestination.text=$"宛先　{destination?.title??game.UiCurrentDungeon?.name??run.dungeon}";
  int remaining=Mathf.Max(0,state.deadlineDays-state.daysElapsed);
  courierRouteDays.text=$"{remaining:00}日";
  courierRouteHp.text=$"{run.hp:00} / {run.maxHp:00}";
  courierRouteHpBar.lowValue=0;
  courierRouteHpBar.highValue=Mathf.Max(1,run.maxHp);
  courierRouteHpBar.value=Mathf.Clamp(run.hp,0,Mathf.Max(1,run.maxHp));
  PopulateCourierDeadlineMeter(state.deadlineDays,state.daysElapsed);
  int recovered=run.lootBag?.Count??0;
  courierRouteRecovered.text=$"{recovered:00}件";
  courierRouteRecoveredGroup.EnableInClassList("ps-hidden",recovered<=0);
  courierRoutePortraitHost.Clear();
  courierRoutePortraitHost.Add(CharacterPortraitFront(CharacterSystem.OfRun(run),"ps-route-status-portrait-image"));
  PopulateCourierNodes();
  PopulateCourierSeals();
  PopulateCourierDetail();
  courierRouteRelayPack.EnableInClassList("ps-visible",state.relayPackingAvailable);
  courierRouteRelayPack.SetEnabled(state.relayPackingAvailable);
  courierRouteFinish.text=state.failed?"遠征を終了":"配達結果を確認";
  courierRouteFinish.EnableInClassList("ps-visible",state.failed||state.complete);
  courierRouteFinish.SetEnabled(state.failed||state.complete);
 }

 void PopulateCourierDeadlineMeter(int deadlineDays,int daysElapsed){
  courierRouteDeadlineMeter.Clear();
  const int visualSegments=15;
  float elapsedRatio=deadlineDays<=0?1f:Mathf.Clamp01((float)daysElapsed/deadlineDays);
  int spent=Mathf.RoundToInt(visualSegments*elapsedRatio);
  for(int index=0;index<visualSegments;index++){
   var segment=new VisualElement{pickingMode=PickingMode.Ignore};
   segment.AddToClassList("ps-route-deadline-segment");
   segment.EnableInClassList("ps-expired",index<spent);
   segment.EnableInClassList("ps-warning",index>=visualSegments-4&&index>=spent);
   courierRouteDeadlineMeter.Add(segment);
  }
 }

 void PopulateCourierNodes(){
  var state=game.UiCourierRoute;
  courierRouteNodeDetail.RemoveFromHierarchy();
  courierRouteNodeHost.Clear();
  var available=CourierRouteSystem.Available(state).Select(x=>x.id).ToHashSet();
  PopulateCourierLinks(state);
  foreach(var definition in CourierRouteSystem.Nodes){
   var node=definition;
   bool revealed=CourierRouteSystem.IsRevealed(state,node);
   bool resolved=state.resolvedNodeIds.Contains(node.id);
   string visualCurrentId=state.travelPending?state.travelFromNodeId:state.currentNodeId;
   bool current=visualCurrentId==node.id;
   bool travelTarget=state.travelPending&&state.travelToNodeId==node.id;
   bool inspectable=revealed||resolved||current;
   string visualKind=revealed?node.kind.ToLowerInvariant():"unknown";
   var button=new Button(()=>SelectCourierRouteNode(node.id));
   button.userData=node.id;
   button.AddToClassList("ps-route-node");
   button.AddToClassList($"ps-route-node--phase-{node.phase}");
   button.AddToClassList($"ps-route-node--lane-{node.lane}");
   button.AddToClassList($"ps-route-node--{visualKind}");
   var phase=new Label(node.phase==0?"発":node.phase==CourierRouteSystem.TotalSegments?"着":$"{node.phase:00}"){
    pickingMode=PickingMode.Ignore
   };
   phase.AddToClassList("ps-route-node__phase");
    var medallion=new VisualElement{pickingMode=PickingMode.Ignore};
    medallion.AddToClassList("ps-route-node__medallion");
    medallion.AddToClassList($"ps-route-node__stamp--{visualKind}");
    if(!revealed){
     var unknown=new Label("?"){pickingMode=PickingMode.Ignore};
     unknown.AddToClassList("ps-route-node__unknown");
     medallion.Add(unknown);
   }
   var title=new Label(revealed?node.title:$"第{node.phase}区"){pickingMode=PickingMode.Ignore};
   title.AddToClassList("ps-route-node__title");
   var kind=new Label(revealed?CourierRouteKindLabel(node.kind):"未解析"){pickingMode=PickingMode.Ignore};
   kind.AddToClassList("ps-route-node__kind");
   button.Add(phase);
   button.Add(medallion);
   button.Add(title);
   button.Add(kind);
   button.EnableInClassList("ps-available",available.Contains(node.id));
   button.EnableInClassList("ps-selected",state.selectedNodeId==node.id);
   button.EnableInClassList("ps-resolved",resolved);
   button.EnableInClassList("ps-current",current);
   button.EnableInClassList("ps-travel-target",travelTarget);
   button.SetEnabled(inspectable&&!state.travelPending);
   courierRouteNodeHost.Add(button);
  }
  string detailNodeId=state.travelPending?"":(!string.IsNullOrEmpty(state.selectedNodeId)?state.selectedNodeId:courierRouteInspectedNodeId);
  AttachCourierNodeDetail(detailNodeId);
  if(!state.travelPending)QueueCourierRouteMarkerAtCurrent();
 }

 void SelectCourierRouteNode(string nodeId){
  courierRouteDetailSealKey="";
  var state=game.UiCourierRoute;
  if(CourierRouteSystem.Available(state).Any(node=>node.id==nodeId)){
   if(state.selectedNodeId==nodeId){
    BeginCourierRouteTravel();
    return;
   }
   courierRouteInspectedNodeId="";
   game.UiCourierRouteSelect(nodeId);
   return;
  }
  courierRouteInspectedNodeId=nodeId;
  PopulateCourierNodeDetail(CourierRouteSystem.Node(nodeId));
  AttachCourierNodeDetail(nodeId);
  RefreshCourierSealSelection();
 }

 void AttachCourierNodeDetail(string nodeId){
  courierRouteNodeDetail.RemoveFromHierarchy();
  courierRouteNodeDetail.EnableInClassList("ps-visible",false);
  for(int phase=0;phase<=CourierRouteSystem.TotalSegments;phase++)
   courierRouteNodeDetail.RemoveFromClassList($"ps-route-node-detail--phase-{phase}");
  courierRouteNodeDetail.RemoveFromClassList("ps-route-node-detail--lane-0");
  courierRouteNodeDetail.RemoveFromClassList("ps-route-node-detail--lane-1");
  if(string.IsNullOrEmpty(nodeId))return;
  var node=CourierRouteSystem.Node(nodeId);
  if(node==null)return;
  courierRouteNodeDetail.AddToClassList($"ps-route-node-detail--phase-{node.phase}");
  courierRouteNodeDetail.AddToClassList($"ps-route-node-detail--lane-{node.lane}");
  courierRouteNodeDetail.EnableInClassList("ps-route-node-detail--left",node.phase>=8);
  courierRouteNodeDetail.EnableInClassList("ps-route-node-detail--upper",node.lane>0||node.phase==0);
  courierRouteNodeDetail.EnableInClassList("ps-visible",true);
  courierRouteMapContent.Add(courierRouteNodeDetail);
 }

 void PopulateCourierLinks(CourierRouteState state){
 courierRouteLinkHost.Clear();
  var resolvedEdges=new HashSet<string>();
  var history=new List<CourierRouteNodeDef>();
  AddCourierHistoryNode(history,CourierRouteSystem.Node("dispatch"));
  foreach(var id in state.resolvedNodeIds)AddCourierHistoryNode(history,CourierRouteSystem.Node(id));
  AddCourierHistoryNode(history,CourierRouteSystem.Node(state.currentNodeId));
  for(int index=0;index<history.Count-1;index++)
   resolvedEdges.Add(CourierRouteLinkKey(history[index],history[index+1]));

  var current=CourierRouteSystem.Node(state.travelPending?state.travelFromNodeId:state.currentNodeId);
  var available=CourierRouteSystem.Available(state).Select(node=>node.id).ToHashSet();
  foreach(var source in CourierRouteSystem.Nodes)
   foreach(var targetId in source.next){
    var target=CourierRouteSystem.Node(targetId);
    string edgeKey=CourierRouteLinkKey(source,target);
    bool traveling=state.travelPending&&source.id==state.travelFromNodeId&&targetId==state.travelToNodeId;
    string visualState=resolvedEdges.Contains(edgeKey)?"resolved":"future";
    if(traveling)visualState="traveling";
    if(source==current&&available.Contains(targetId))visualState=state.selectedNodeId==targetId?"selected":"available";
    AddCourierLink(source,target,visualState);
   }
 }

 static void AddCourierHistoryNode(List<CourierRouteNodeDef> history,CourierRouteNodeDef node){
  if(node!=null&&(history.Count==0||history[history.Count-1].id!=node.id))history.Add(node);
 }

 static string CourierRouteLinkKey(CourierRouteNodeDef source,CourierRouteNodeDef target)=>
  source==null||target==null?"":$"{source.id}>{target.id}";

 VisualElement AddCourierLink(CourierRouteNodeDef source,CourierRouteNodeDef target,string state){
  if(source==null||target==null||!source.next.Contains(target.id))return null;
  var link=new VisualElement{pickingMode=PickingMode.Ignore};
  link.AddToClassList("ps-route-link");
  link.AddToClassList($"ps-route-link--{source.id}-to-{target.id}");
  link.AddToClassList($"ps-route-link--{state}");
  var stroke=new VisualElement{pickingMode=PickingMode.Ignore};
  stroke.AddToClassList("ps-route-link__stroke");
  var arrow=new VisualElement{pickingMode=PickingMode.Ignore};
  arrow.AddToClassList("ps-route-link__arrow");
  link.Add(stroke);
  link.Add(arrow);
  courierRouteLinkHost.Add(link);
  return link;
 }

 static string CourierRouteKindLabel(string kind)=>kind switch{
  "START"=>"出発局",
  "RELAY"=>"中継所",
  "PURSUIT"=>"戦闘地点",
  "CARGO"=>"回収地点",
  "EVENT"=>"事象地点",
  "DESTINATION"=>"目的地",
  _=>"未解析"
 };

 void PopulateCourierSeals(){
  courierRouteSealHost.Clear();
  var availableSeals=game.UiCourierRoute.seals.Where(seal=>seal.available).ToArray();
  foreach(var value in availableSeals){
   var seal=value;
   var button=new Button(()=>SelectCourierRouteSeal(seal.key));
   button.AddToClassList("ps-route-protocol");
   button.userData=seal.key;
   button.EnableInClassList("ps-spent",seal.charges<=0);
   button.EnableInClassList("ps-selected",courierRouteDetailSealKey==seal.key);
   var mark=new VisualElement{pickingMode=PickingMode.Ignore};
   mark.AddToClassList("ps-route-protocol__mark");
   var icon=new VisualElement{pickingMode=PickingMode.Ignore};
   icon.AddToClassList("ps-route-protocol__icon");
   icon.AddToClassList($"ps-route-protocol__icon--{seal.target.ToLowerInvariant()}");
   var name=new Label(seal.name){pickingMode=PickingMode.Ignore};
   name.AddToClassList("ps-route-protocol__name");
   var charge=new Label($"残 {seal.charges} / {seal.maxCharges}"){pickingMode=PickingMode.Ignore};
   charge.AddToClassList("ps-route-protocol__charge");
   var selection=new VisualElement{pickingMode=PickingMode.Ignore};
   selection.AddToClassList("ps-route-protocol__selection");
   mark.Add(icon);
   button.Add(mark);
   button.Add(name);
   button.Add(charge);
   button.Add(selection);
   button.SetEnabled(!game.UiCourierRoute.complete&&!game.UiCourierRoute.failed&&
    !game.UiCourierRoute.travelPending&&!game.UiCourierRoute.awaitingResolution);
   courierRouteSealHost.Add(button);
  }
  var selected=availableSeals.FirstOrDefault(seal=>seal.key==courierRouteDetailSealKey);
  if(selected!=null)PopulateCourierSealDetail(selected);
  else {
   courierRouteDetailSealKey="";
   courierRouteSealDetailName.text=availableSeals.Length>0?"印章を選択":"配達印なし";
   courierRouteSealDetailEffect.text=availableSeals.Length>0?"効果を確認してから印を押します。":"荷造りの色一致から配達印を生成できます。";
   courierRouteSealApply.EnableInClassList("ps-visible",false);
  }
 }

 void SelectCourierRouteSeal(string key){
  if(game.UiCourierRoute?.travelPending==true||game.UiCourierRoute?.awaitingResolution==true)return;
  courierRouteDetailSealKey=key;
  var seal=game.UiCourierRoute?.seals?.FirstOrDefault(value=>value.key==key&&value.available);
  if(seal!=null)PopulateCourierSealDetail(seal);
  RefreshCourierSealSelection();
 }

 void RefreshCourierSealSelection(){
  if(courierRouteSealHost==null)return;
  foreach(var button in courierRouteSealHost.Query<Button>().ToList())
   button.EnableInClassList("ps-selected",button.userData as string==courierRouteDetailSealKey);
 }

 void PopulateCourierDetail(){
  var state=game.UiCourierRoute;
  string nodeId=!string.IsNullOrEmpty(state?.selectedNodeId)?state.selectedNodeId:courierRouteInspectedNodeId;
  var node=CourierRouteSystem.Node(nodeId);
  if(node!=null)PopulateCourierNodeDetail(node);
  courierRouteNodeDetail.EnableInClassList("ps-visible",node!=null);
 }

 void PopulateCourierNodeDetail(CourierRouteNodeDef node){
  if(node==null)return;
  var state=game.UiCourierRoute;
  var current=CourierRouteSystem.Node(state?.currentNodeId);
  courierRouteDetailEyebrow.text="ROUTE DOSSIER";
  courierRouteDetailTitle.text=node.title;
  courierRouteDetailSubtitle.text=node==current?"現在地":$"{current?.title??"出発局"} からの次区間";
  courierRouteDetailKeyA.text="所要日数";
  courierRouteDetailValueA.text=node.dayCost<=0?"出発地点":$"{node.dayCost}日";
  courierRouteDetailKeyB.text="危険度";
  courierRouteDetailValueB.text=node.risk<=0?"安全":new string('◆',Mathf.Clamp(node.risk,1,3));
  courierRouteDetailSection.text=$"{CourierRouteKindLabel(node.kind)} / {node.resolutionTitle}";
  courierRouteDetailBody.text=string.IsNullOrWhiteSpace(node.condition)?node.resolutionText:$"{node.condition}\n{node.resolutionText}";
 }

 void PopulateCourierSealDetail(DeliverySealState seal){
  string target=seal.target switch{
   DeliverySealSystem.DelayTarget=>"地点処理",
   DeliverySealSystem.SealTarget=>"配達印",
   _=>"次の区間"
  };
  courierRouteSealDetailName.text=$"{seal.name}　残 {seal.charges}/{seal.maxCharges}";
  courierRouteSealDetailEffect.text=$"{target}：{seal.text}";
  courierRouteSealApply.EnableInClassList("ps-visible",true);
  courierRouteSealApply.SetEnabled(seal.charges>0&&!game.UiCourierRoute.complete&&!game.UiCourierRoute.failed&&
   !game.UiCourierRoute.travelPending&&!game.UiCourierRoute.awaitingResolution);
 }

 void ApplyCourierRouteSeal(){
  if(string.IsNullOrEmpty(courierRouteDetailSealKey)||game.UiCourierRoute?.travelPending==true||
   game.UiCourierRoute?.awaitingResolution==true)return;
  game.UiCourierRouteUseSeal(courierRouteDetailSealKey);
 }

 void ScrollCourierSeals(int direction){
  if(courierRouteSealScroll==null)return;
  var offset=courierRouteSealScroll.scrollOffset;
  offset.y=Mathf.Max(0,offset.y+direction*83f);
  courierRouteSealScroll.scrollOffset=offset;
 }

 void ToggleCourierSealDrawer(){
  courierRouteSealDrawerOpen=!courierRouteSealDrawerOpen;
  courierRouteShell.EnableInClassList("ps-seal-drawer-open",courierRouteSealDrawerOpen);
 }

 void FinishCourierRoute(){
  if(game.UiCourierRoute?.complete==true||game.UiCourierRoute?.failed==true)game.UiCourierRouteFinish();
 }

 void ClearCourierRouteReferences(){
  ClearCourierRouteTravel();
  ClearCourierRouteMapInteraction();
  courierRouteShell=courierRouteLinkHost=courierRouteNodeHost=courierRouteNodeDetail=courierRouteSealHost=null;
  courierRoutePortraitHost=courierRouteRecoveredGroup=courierRouteDeadlineMeter=courierRouteSealDrawer=null;
  courierRouteDestination=courierRouteDays=courierRouteHp=courierRouteRecovered=null;
  courierRouteDetailEyebrow=courierRouteDetailTitle=courierRouteDetailSubtitle=courierRouteDetailKeyA=courierRouteDetailValueA=null;
  courierRouteDetailKeyB=courierRouteDetailValueB=courierRouteDetailSection=courierRouteDetailBody=null;
  courierRouteSealDetailName=courierRouteSealDetailEffect=null;
  courierRouteHpBar=null;
  courierRouteSealScroll=null;
  courierRouteFinish=courierRouteRelayPack=courierRouteSealApply=courierRouteSealLeft=courierRouteSealRight=courierRouteSealDrawerToggle=null;
  courierRouteDetailSealKey=courierRouteInspectedNodeId="";
  courierRouteSealDrawerOpen=false;
 }
}
}
