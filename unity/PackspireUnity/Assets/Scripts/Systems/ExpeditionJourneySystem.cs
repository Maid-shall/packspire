using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Packspire {
/// <summary>
/// Connects the generated expedition graph to the current seamless journey.
/// ExpeditionRoutePlan owns progression; CourierRouteState temporarily keeps
/// delivery seals and counters that have not yet moved to their final systems.
/// </summary>
public static class ExpeditionJourneySystem {
 public static IReadOnlyList<ExpeditionRouteNodePlan> Available(RunState run)=>
  ExpeditionRoutePlanSystem.Available(ExpeditionProgressSystem.Ensure(run));

 public static bool Select(RunState run,string nodeId)=>
  run!=null&&ExpeditionRoutePlanSystem.Select(ExpeditionProgressSystem.Ensure(run),nodeId);

 public static bool Commit(RunState run,out ExpeditionRouteNodePlan node,out string message){
  node=null;
  message="次の経路を選択してください。";
  if(run==null)return false;
  var plan=ExpeditionProgressSystem.Ensure(run);
  int previousDays=plan.elapsedDays;
  int discount=Math.Max(0,run.courierRoute?.nextSegmentDiscount??0);
  if(!ExpeditionRoutePlanSystem.CommitSelection(run,discount,out node))return false;

  var route=run.courierRoute;
  if(route!=null){
   route.nextSegmentDiscount=0;
   route.awaitingResolution=true;
   route.pendingResolution=ResolutionKind(node.kind);
   route.travelFromNodeId=plan.resolvedNodeIds.LastOrDefault()??"dispatch";
   route.travelToNodeId=node.id;
   route.travelDayCost=Math.Max(0,plan.elapsedDays-previousDays);
   route.travelPending=true;
  }

  var presentation=PresentationNode(plan,node);
  message=$"{presentation.title}へ到着。{presentation.resolutionTitle}を解決します。";
  return true;
 }

 public static bool ResolveCurrent(RunState run,CourierLocationOutcome outcome,out string message){
  message="解決待ちの遠征地点がありません。";
  if(run==null)return false;
  var plan=ExpeditionProgressSystem.Ensure(run);
  var node=ExpeditionRoutePlanSystem.Node(plan,plan.currentNodeId);
  if(node==null||!plan.awaitingResolution)return false;
  outcome??=new CourierLocationOutcome();
  var route=run.courierRoute;

  if(!outcome.success){
   if(route!=null){
    route.failed=true;
    ClearCompatibilityResolution(route);
   }
   plan.awaitingResolution=false;
   message=string.IsNullOrWhiteSpace(outcome.message)
    ?$"{PresentationNode(plan,node).title}で遠征を継続できなくなりました。"
    :outcome.message;
   return true;
  }

  int extraDays=Math.Max(0,outcome.dayDelta);
  if(route?.delayShield>0){
   extraDays=Math.Max(0,extraDays-route.delayShield);
   route.delayShield=0;
  }
  ExpeditionProgressSystem.AdvanceDays(run,extraDays);
  if(!ExpeditionRoutePlanSystem.ResolveCurrent(plan))return false;

  string reward="";
  if(route!=null){
   route.clearedSegments++;
   ClearCompatibilityResolution(route);
  }
  switch(node.kind){
   case ExpeditionNodeKind.Rest:
    int beforeHp=run.hp;
    run.hp=Mathf.Min(run.maxHp,run.hp+3);
    if(route!=null){
     route.relayPackingAvailable=true;
     var refill=route.seals?.FirstOrDefault(seal=>seal.charges<seal.maxCharges);
     if(refill!=null)refill.charges++;
    }
    reward=$"中継補給：HP {beforeHp}→{run.hp}。";
    break;
   case ExpeditionNodeKind.Boss:
    reward=plan.complete?"最深部の封鎖を突破しました。":"階層の封鎖を突破しました。";
    break;
  }

  string suffix=string.Join(" ",new[]{outcome.message,reward}
   .Where(value=>!string.IsNullOrWhiteSpace(value)));
  message=string.IsNullOrEmpty(suffix)
   ?$"{PresentationNode(plan,node).title}を解決。先へ進めます。"
   :suffix;
  return true;
 }

 public static CourierRouteNodeDef PresentationNode(
  ExpeditionRoutePlan plan,
  ExpeditionRouteNodePlan node){
  if(node==null)return null;
  int phase=PresentationPhase(plan,node);
  int days=Math.Max(0,node.dayCost);
  bool exploratory=node.lane<=0;
  bool assault=node.lane>=2;
  int risk=node.kind==ExpeditionNodeKind.Boss?3:assault?2:node.kind==ExpeditionNodeKind.Battle?1:0;
  string resolution=ResolutionKind(node.kind);
  string area=FloorName(node.floorIndex);
  string title=NodeTitle(area,node.kind,node.order);
  string resolutionTitle=ResolutionTitle(node.kind);
  return new CourierRouteNodeDef{
   id=node.id,
   title=title,
   kind=node.kind.ToString().ToUpperInvariant(),
   condition=Condition(node.kind,node.lane),
   resolution=resolution,
   resolutionTitle=resolutionTitle,
   resolutionText=ResolutionText(node.kind,area),
   roadWidth=node.kind==ExpeditionNodeKind.Boss
    ?CourierRoadWidth.Standard
    :assault?CourierRoadWidth.Narrow:exploratory?CourierRoadWidth.Wide:CourierRoadWidth.Standard,
   phase=phase,
   lane=node.lane,
   dayCost=days,
   risk=risk,
   next=node.nextNodeIds?.ToArray()??Array.Empty<string>()
 };
 }

 public static string PathTitle(ExpeditionRoutePlan plan,ExpeditionRouteNodePlan node){
  if(node==null)return "未確認経路";
  if(node.kind==ExpeditionNodeKind.Boss)return "階層封鎖";
  return node.lane switch{
   0=>"探索側の分岐",
   2=>"突破側の分岐",
   _=>"中央の連絡路"
  };
 }

 static void ClearCompatibilityResolution(CourierRouteState route){
  route.awaitingResolution=false;
  route.pendingResolution="";
  route.travelPending=false;
  route.travelFromNodeId="";
  route.travelToNodeId="";
  route.travelDayCost=0;
 }

 static int PresentationPhase(
  ExpeditionRoutePlan plan,
  ExpeditionRouteNodePlan node){
  if(node.kind==ExpeditionNodeKind.Boss)return node.floorIndex switch{0=>4,1=>7,_=>10};
  ExpeditionFloorPlan floor=plan?.floors?.FirstOrDefault(candidate=>candidate.floorIndex==node.floorIndex);
  int lastOrder=Math.Max(1,floor?.nodes?.Where(candidate=>candidate.kind!=ExpeditionNodeKind.Boss)
   .Select(candidate=>candidate.order).DefaultIfEmpty(1).Max()??1);
  float progress=Mathf.Clamp01((float)node.order/lastOrder);
  return node.floorIndex switch{
   0=>1+Mathf.RoundToInt(progress*2f),
   1=>5+Mathf.RoundToInt(progress),
   _=>8+Mathf.RoundToInt(progress)
  };
 }

 static string FloorName(int floorIndex)=>floorIndex switch{
  1=>"水没書庫",
  2=>"黒鐘区画",
  _=>"灰市外縁"
 };

 static string NodeTitle(string area,ExpeditionNodeKind kind,int order)=>kind switch{
  ExpeditionNodeKind.Battle=>$"{area}・巡回封鎖 {order+1:00}",
  ExpeditionNodeKind.Event=>$"{area}・異変地点 {order+1:00}",
  ExpeditionNodeKind.Rest=>$"{area}・中継所 {order+1:00}",
  ExpeditionNodeKind.Other=>$"{area}・探索地点 {order+1:00}",
  ExpeditionNodeKind.Boss=>$"{area}・階層封鎖",
  _=>area
 };

 static string ResolutionKind(ExpeditionNodeKind kind)=>kind switch{
  ExpeditionNodeKind.Battle=>CourierResolutionKind.Battle,
  ExpeditionNodeKind.Boss=>CourierResolutionKind.Battle,
  ExpeditionNodeKind.Rest=>CourierResolutionKind.Relay,
  _=>CourierResolutionKind.Event
 };

 static string ResolutionTitle(ExpeditionNodeKind kind)=>kind switch{
  ExpeditionNodeKind.Battle=>"敵影を排除",
  ExpeditionNodeKind.Boss=>"階層番人を撃破",
  ExpeditionNodeKind.Rest=>"中継補給",
  ExpeditionNodeKind.Other=>"周辺を探索",
  _=>"異変を調査"
 };

 static string ResolutionText(ExpeditionNodeKind kind,string area)=>kind switch{
  ExpeditionNodeKind.Battle=>$"{area}を塞ぐ敵影を退け、奥へ進む。",
  ExpeditionNodeKind.Boss=>$"{area}の最深部を守る番人を倒し、次の階層を開く。",
  ExpeditionNodeKind.Rest=>"傷を整え、次の区画へ進む準備をする。",
  ExpeditionNodeKind.Other=>"周囲を調べ、遠征に役立つものを探す。",
  _=>"現地の状況を確かめ、進み方を決める。"
 };

 static string Condition(ExpeditionNodeKind kind,int lane){
  if(kind==ExpeditionNodeKind.Boss)return "階層の終点。突破後に帰還か続行を判断する。";
  return lane switch{
   0=>"探索と休息に寄った次区間。後の分岐で突破側へ戻ることもできる。",
   2=>"戦闘機会が多い短期区間。後の分岐で探索側へ戻ることもできる。",
   _=>"探索側と突破側をつなぐ合流区間。"
  };
 }
}
}
