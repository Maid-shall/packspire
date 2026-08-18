using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire {
public sealed partial class PackspireUiFoundation {
 const int CourierRouteWalkFrames=6;
 const float CourierRouteWalkFrameSeconds=1f/6f;
 VisualElement courierRouteCourierPosition,courierRouteCourierMarker,courierRouteCourierFallback,courierRouteTravelDays;
 Image courierRouteCourierSprite;
 Label courierRouteTravelLabel;
 Texture2D courierRouteWalkSheet;
 IVisualElementScheduledItem courierRouteTravelTick;
 IVisualElementScheduledItem courierRouteMarkerPlacement;
 Vector2 courierRouteTravelFrom,courierRouteTravelTo;
 float courierRouteTravelStartedAt,courierRouteTravelDuration;
 int courierRouteTravelStartElapsed,courierRouteTravelCost,courierRouteTravelShownDay=-1,courierRouteWalkFrame=-1;
 bool courierRouteTraveling,courierRouteUsesWalkSheet,courierRouteTravelMidpointLogged;

 void BuildCourierRouteTravel(){
  courierRouteCourierPosition=RequireViewElement<VisualElement>(courierRouteShell,"route-courier-position");
  courierRouteCourierMarker=RequireViewElement<VisualElement>(courierRouteShell,"route-courier-marker");
  courierRouteCourierSprite=RequireViewElement<Image>(courierRouteShell,"route-courier-sprite");
  courierRouteCourierFallback=RequireViewElement<VisualElement>(courierRouteShell,"route-courier-fallback");
  courierRouteTravelLabel=RequireViewElement<Label>(courierRouteShell,"route-travel-label");
  courierRouteTravelDays=RequireViewElement<VisualElement>(courierRouteShell,"route-travel-days");
  courierRouteWalkSheet=PackspireResources.Load<Texture2D>("Art/UI/CourierRoutePrototype/courier-route-walk-sheet-v1");
  ConfigureCourierRouteMarkerArt();
 }

 void ConfigureCourierRouteMarkerArt(){
  var character=CharacterSystem.OfRun(game.UiRun);
  courierRouteUsesWalkSheet=character?.id=="mio"&&courierRouteWalkSheet!=null;
  courierRouteCourierMarker.EnableInClassList("ps-route-courier-marker--animated",courierRouteUsesWalkSheet);
  courierRouteCourierSprite.image=courierRouteUsesWalkSheet?courierRouteWalkSheet:null;
  courierRouteWalkFrame=-1;
  if(courierRouteUsesWalkSheet)SetCourierRouteWalkFrame(0);
  courierRouteCourierFallback.Clear();
  if(!courierRouteUsesWalkSheet&&character!=null){
   var fallback=CharacterPortraitFront(character,"ps-route-courier-marker__fallback-image");
   fallback.pickingMode=PickingMode.Ignore;
   courierRouteCourierFallback.Add(fallback);
  }
 }

 void RestoreCourierRouteTravelState(){
  var state=game.UiCourierRoute;
  if(state?.travelPending==true){
   courierRouteShell.schedule.Execute(StartCourierRouteTravel).ExecuteLater(0);
   return;
  }
  QueueCourierRouteMarkerAtCurrent();
 }

 void BeginCourierRouteTravel(){
  var state=game.UiCourierRoute;
  if(state==null||state.travelPending||!game.UiCourierRouteBeginTravel())return;
  courierRouteInspectedNodeId="";
  PopulateCourierNodes();
  PopulateCourierSeals();
  PopulateCourierDetail();
  courierRouteShell.schedule.Execute(StartCourierRouteTravel).ExecuteLater(0);
 }

 void StartCourierRouteTravel(){
  var state=game.UiCourierRoute;
  if(state?.travelPending!=true||courierRouteCourierPosition==null||courierRouteCourierMarker==null)return;
  var from=FindCourierRouteNodeButton(state.travelFromNodeId);
  var to=FindCourierRouteNodeButton(state.travelToNodeId);
  if(!CourierRouteTravelNodeLayoutReady(from)||!CourierRouteTravelNodeLayoutReady(to)){
   courierRouteShell?.schedule.Execute(StartCourierRouteTravel).ExecuteLater(16);
   return;
  }

  courierRouteTravelFrom=from.layout.center;
  courierRouteTravelTo=to.layout.center;
  courierRouteTravelCost=Mathf.Max(0,state.travelDayCost);
  courierRouteTravelStartElapsed=Mathf.Max(0,state.daysElapsed-courierRouteTravelCost);
  courierRouteTravelDuration=Mathf.Clamp(Mathf.Max(1,courierRouteTravelCost)*1.35f,2.6f,5.5f);
  courierRouteTravelStartedAt=Time.realtimeSinceStartup;
  courierRouteTravelShownDay=-1;
  courierRouteTravelMidpointLogged=false;
  courierRouteTraveling=true;
  courierRouteMarkerPlacement?.Pause();
  courierRouteMarkerPlacement=null;
  courierRouteShell.AddToClassList("ps-route-traveling");
  courierRouteCourierPosition.AddToClassList("ps-route-courier-position--traveling");
  courierRouteCourierPosition.AddToClassList("ps-visible");
  courierRouteCourierMarker.AddToClassList("ps-route-courier-marker--traveling");
  if(courierRouteUsesWalkSheet){
   courierRouteCourierSprite.image=courierRouteWalkSheet;
   courierRouteWalkFrame=-1;
   SetCourierRouteWalkFrame(0);
  }
#if UNITY_EDITOR
  Debug.Log($"[PackspireQA] Courier travel started from={state.travelFromNodeId} {from.layout} to={state.travelToNodeId} {to.layout} duration={courierRouteTravelDuration:0.00}s animated={courierRouteUsesWalkSheet}");
#endif
  UpdateCourierRouteTravel(0f);
  courierRouteTravelTick?.Pause();
  courierRouteTravelTick=courierRouteShell.schedule.Execute(TickCourierRouteTravel).Every(16);
 }

 void TickCourierRouteTravel(){
  if(!courierRouteTraveling||courierRouteCourierPosition==null||courierRouteCourierMarker==null){courierRouteTravelTick?.Pause();return;}
  float progress=Mathf.Clamp01((Time.realtimeSinceStartup-courierRouteTravelStartedAt)/courierRouteTravelDuration);
  UpdateCourierRouteTravel(progress);
#if UNITY_EDITOR
  if(!courierRouteTravelMidpointLogged&&progress>=0.45f){
   courierRouteTravelMidpointLogged=true;
   Debug.Log($"[PackspireQA] Courier travel anchor panel={courierRouteCourierPosition.panel!=null} parent={courierRouteCourierPosition.parent?.name??"none"} display={courierRouteCourierPosition.resolvedStyle.display} position={courierRouteCourierPosition.resolvedStyle.position} routeFrom={courierRouteTravelFrom} routeTo={courierRouteTravelTo} inline=({courierRouteCourierPosition.style.left.value.value:0.00},{courierRouteCourierPosition.style.top.value.value:0.00}) resolved=({courierRouteCourierPosition.resolvedStyle.left:0.00},{courierRouteCourierPosition.resolvedStyle.top:0.00}) layout={courierRouteCourierPosition.layout} anchorBounds={courierRouteCourierPosition.worldBound} markerBounds={courierRouteCourierMarker.worldBound} sprite={courierRouteCourierSprite?.resolvedStyle.display} image={courierRouteCourierSprite?.image!=null}");
  }
#endif
  if(progress<1f)return;
  courierRouteTraveling=false;
  courierRouteCourierPosition?.RemoveFromClassList("ps-route-courier-position--traveling");
  courierRouteCourierMarker?.RemoveFromClassList("ps-route-courier-marker--traveling");
  courierRouteTravelTick?.Pause();
  courierRouteTravelTick=null;
  game.UiCourierRouteCompleteTravel();
 }

 void UpdateCourierRouteTravel(float progress){
  Vector2 delta=courierRouteTravelTo-courierRouteTravelFrom;
  Vector2 normal=delta.sqrMagnitude<1f?Vector2.zero:new Vector2(-delta.y,delta.x).normalized;
  Vector2 position=Vector2.Lerp(courierRouteTravelFrom,courierRouteTravelTo,progress)+normal*Mathf.Sin(progress*Mathf.PI)*5f;
  float stride=Mathf.Abs(Mathf.Sin((Time.realtimeSinceStartup-courierRouteTravelStartedAt)*Mathf.PI*5f));
  position.y-=stride*4f;
  courierRouteCourierPosition.style.left=position.x;
  courierRouteCourierPosition.style.top=position.y;
  courierRouteCourierMarker.EnableInClassList("ps-route-courier-marker--left",delta.x<0f);
  int day=courierRouteTravelCost<=0?0:Mathf.Min(courierRouteTravelCost,Mathf.RoundToInt(progress*courierRouteTravelCost));
  if(progress>=1f)day=courierRouteTravelCost;
  if(day!=courierRouteTravelShownDay){
   courierRouteTravelShownDay=day;
   courierRouteTravelLabel.text=courierRouteTravelCost<=0?"即時移動":$"移動中 {day}/{courierRouteTravelCost}日";
   PopulateCourierTravelDayPips(day,courierRouteTravelCost);
   int displayedElapsed=courierRouteTravelStartElapsed+day;
   int remaining=Mathf.Max(0,(game.UiCourierRoute?.deadlineDays??0)-displayedElapsed);
   courierRouteDays.text=$"{remaining:00}日";
   PopulateCourierDeadlineMeter(game.UiCourierRoute?.deadlineDays??0,displayedElapsed);
  }
  if(courierRouteUsesWalkSheet){
   int frame=Mathf.FloorToInt((Time.realtimeSinceStartup-courierRouteTravelStartedAt)/CourierRouteWalkFrameSeconds)%CourierRouteWalkFrames;
   SetCourierRouteWalkFrame(frame);
  }
 }

 void PopulateCourierTravelDayPips(int elapsed,int total){
  courierRouteTravelDays.Clear();
  for(int index=0;index<Mathf.Max(1,total);index++){
   var pip=new VisualElement{pickingMode=PickingMode.Ignore};
   pip.AddToClassList("ps-route-courier-marker__day");
   pip.EnableInClassList("ps-spent",index<elapsed);
   courierRouteTravelDays.Add(pip);
  }
 }

 void SetCourierRouteWalkFrame(int frame){
  if(courierRouteCourierSprite==null)return;
  frame=Mathf.Clamp(frame,0,CourierRouteWalkFrames-1);
  if(frame==courierRouteWalkFrame)return;
  courierRouteWalkFrame=frame;
  courierRouteCourierSprite.uv=new Rect(frame/(float)CourierRouteWalkFrames,0.20f,1f/CourierRouteWalkFrames,0.58f);
  courierRouteCourierSprite.MarkDirtyRepaint();
 }

 void QueueCourierRouteMarkerAtCurrent(){
  if(courierRouteCourierPosition==null||courierRouteCourierMarker==null||courierRouteTraveling)return;
  courierRouteMarkerPlacement?.Pause();
  int attempts=0;
  courierRouteMarkerPlacement=courierRouteCourierMarker.schedule.Execute(()=>{
   if(courierRouteTraveling){
    courierRouteMarkerPlacement?.Pause();
    courierRouteMarkerPlacement=null;
    return;
   }
   var current=FindCourierRouteNodeButton(game.UiCourierRoute?.currentNodeId??"");
   bool layoutReady=current!=null&&current.layout.width>1f&&current.layout.height>1f&&
    !float.IsNaN(current.layout.center.x)&&!float.IsNaN(current.layout.center.y);
   if(!layoutReady){
    attempts++;
    if(attempts<90)return;
    courierRouteMarkerPlacement?.Pause();
    courierRouteMarkerPlacement=null;
    return;
   }
   courierRouteCourierPosition.style.left=current.layout.center.x;
   courierRouteCourierPosition.style.top=current.layout.center.y;
   courierRouteCourierPosition.RemoveFromClassList("ps-route-courier-position--traveling");
   courierRouteCourierPosition.AddToClassList("ps-visible");
   courierRouteCourierMarker.RemoveFromClassList("ps-route-courier-marker--traveling");
   courierRouteCourierMarker.RemoveFromClassList("ps-route-courier-marker--left");
   courierRouteTravelLabel.text="現在地";
   courierRouteTravelDays.Clear();
   if(courierRouteUsesWalkSheet)SetCourierRouteWalkFrame(0);
#if UNITY_EDITOR
   Debug.Log($"[PackspireQA] Courier marker visible character={CharacterSystem.OfRun(game.UiRun)?.id??"none"} node={game.UiCourierRoute?.currentNodeId??"none"} position={current.layout.center}");
#endif
   courierRouteMarkerPlacement?.Pause();
   courierRouteMarkerPlacement=null;
  }).Every(16);
 }

 Button FindCourierRouteNodeButton(string nodeId)=>courierRouteNodeHost?.Children().OfType<Button>()
  .FirstOrDefault(button=>(button.userData as string)==nodeId);

 static bool CourierRouteTravelNodeLayoutReady(VisualElement node){
  if(node==null||node.layout.width<1f||node.layout.height<1f)return false;
  Vector2 center=node.layout.center;
  return !float.IsNaN(center.x)&&!float.IsNaN(center.y)&&
   !float.IsInfinity(center.x)&&!float.IsInfinity(center.y);
 }

 void ClearCourierRouteTravel(){
  courierRouteTraveling=false;
  courierRouteTravelTick?.Pause();
  courierRouteTravelTick=null;
  courierRouteMarkerPlacement?.Pause();
  courierRouteMarkerPlacement=null;
  courierRouteCourierPosition=courierRouteCourierMarker=courierRouteCourierFallback=courierRouteTravelDays=null;
  courierRouteCourierSprite=null;
  courierRouteTravelLabel=null;
  courierRouteWalkSheet=null;
  courierRouteWalkFrame=-1;
 }
}
}
