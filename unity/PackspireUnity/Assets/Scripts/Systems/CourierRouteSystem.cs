using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace Packspire {
public static class CourierResolutionKind {
 public const string Immediate="IMMEDIATE";
 public const string Event="EVENT";
 public const string Battle="BATTLE";
 public const string Cargo="CARGO";
 public const string Relay="RELAY";
 public const string Delivery="DELIVERY";
 public const string MiniGame="MINIGAME";
}

[Serializable]
public sealed class CourierLocationOutcome {
 public bool success=true,cargoRecovered;
 public int performance,dayDelta;
 public string message="";
}

[Serializable]
public sealed class CourierRouteState {
 [FormerlySerializedAs("bell")] public int daysElapsed;
 [FormerlySerializedAs("deadline")] public int deadlineDays=15;
 [FormerlySerializedAs("bellDiscount")] public int nextSegmentDiscount;
 public int delayShield;
 public string currentNodeId="dispatch",selectedNodeId="",pendingResolution="",roleId="warrior",qualificationSealId="";
 [FormerlySerializedAs("deliveryProof")] public int recoveredCargoCount;
 public int roleBranch,clearedSegments;
 public bool complete,failed,awaitingResolution,relayPackingAvailable;
 public string travelFromNodeId="",travelToNodeId="";
 public int travelDayCost;
 public bool travelPending;
 public List<string> resolvedNodeIds=new();
 public List<DeliverySealState> seals=new();
}

public sealed class CourierRouteNodeDef {
 public string id,title,kind,condition,resolution,resolutionTitle,resolutionText;
 public int phase,lane,dayCost,risk;
 public string[] next=Array.Empty<string>();
}

/// <summary>
/// One delivery contract crosses ten route segments. The deadline is the only
/// route-wide pressure; battles and events resolve through their own screens.
/// Future minigames plug into the same outcome contract without changing the map.
/// </summary>
public static class CourierRouteSystem {
 public const int TotalSegments=10;

 static readonly CourierRouteNodeDef[] NodesValue={
  Node("dispatch","出発局","START",0,0,0,0,CourierResolutionKind.Immediate,
   "配達開始","最初の経路を承認する。",Next("ash_market","broken_stair")),

  Node("ash_market","灰市の中継所","RELAY",1,0,2,0,CourierResolutionKind.Relay,
   "中継補給","検札を済ませ、収納術式を整える。",Next("quiet_shaft","bell_bridge"),
   "遠回りだが安全。HP回復と荷物整理を行える。"),
  Node("broken_stair","崩れ階段","EVENT",1,1,1,1,CourierResolutionKind.Event,
   "崩落路を測量","周囲を照合し、崩落区画から帰還する。",Next("bell_bridge"),
   "短いが結果次第で追加日数が発生する。"),

  Node("quiet_shaft","無音昇降路","CARGO",2,0,1,1,CourierResolutionKind.Cargo,
   "中継荷を回収","残された荷札を照合し、回収する荷物を選ぶ。",Next("cinder_tunnel"),
   "回収物を持ち帰れる。戦闘は発生しない。"),
  Node("bell_bridge","鐘楼橋","RELAY",2,1,2,0,CourierResolutionKind.Relay,
   "中継補給","橋の検札所でHPを回復し、収納術式を整える。",Next("cinder_tunnel","sealed_gate"),
   "日数はかかるが次の分岐へ備えられる。"),

  Node("cinder_tunnel","燼火隧道","PURSUIT",3,0,1,3,CourierResolutionKind.Battle,
   "隧道の敵影を排除","配達路を塞ぐ敵影を倒して隧道を突破する。",Next("reliquary_post","gallows_square"),
   "最短経路。強敵との戦闘を避けられない。"),
  Node("sealed_gate","封印関門","EVENT",3,1,2,1,CourierResolutionKind.Event,
   "通関手続き","封印された関門の通行証を照合する。",Next("gallows_square"),
   "安全だが手続きに日数を要する。"),

  Node("reliquary_post","聖遺物郵便局","RELAY",4,0,2,0,CourierResolutionKind.Relay,
   "局内記録を照合","散らばった記録を照らし、配達先を確認する。",Next("drowned_archive"),
   "HP回復と荷物整理ができる中継所。"),
  Node("gallows_square","絞首広場","PURSUIT",4,1,1,2,CourierResolutionKind.Battle,
   "広場の番人を排除","配達路を塞ぐ番人を倒して突破する。",Next("drowned_archive","silent_causeway"),
   "短いが番人が巡回する危険経路。"),

  Node("drowned_archive","水没書庫","CARGO",5,0,1,1,CourierResolutionKind.Cargo,
   "宛先台帳を回収","水没した宛先台帳を照合し、回収物を選ぶ。",Next("ash_hospital","condemned_station"),
   "追加の回収物を得られる可能性がある。"),
  Node("silent_causeway","静寂の高架路","EVENT",5,1,2,0,CourierResolutionKind.Event,
   "高架路を通過","封鎖前に通行手続きを終える。",Next("condemned_station"),
   "安全だが期限を大きく消費する。"),

  Node("ash_hospital","灰療院中継所","RELAY",6,0,2,0,CourierResolutionKind.Relay,
   "診療記録を照合","廃診療棟でHPを回復し、収納術式を整える。",Next("glass_aqueduct"),
   "最後の中継候補。荷物整理も行える。"),
  Node("condemned_station","廃駅処刑場","PURSUIT",6,1,1,3,CourierResolutionKind.Battle,
   "追跡者を退ける","追跡者を倒し、閉鎖駅を抜ける。",Next("glass_aqueduct","infernal_customs"),
   "短いが高危険度の戦闘が発生する。"),

  Node("glass_aqueduct","硝子導水橋","EVENT",7,0,1,2,CourierResolutionKind.Event,
   "導水路を測量","割れた導水路の安全な足場を確認する。",Next("courier_catacomb","black_bell_yard"),
   "成功すれば予定通り、失敗すると追加日数が発生する。"),
  Node("infernal_customs","地獄通関局","PURSUIT",7,1,1,3,CourierResolutionKind.Battle,
   "通関執行官を排除","執行官を倒し、配達許可を通す。",Next("black_bell_yard"),
   "短いが戦闘を避けられない。"),

  Node("courier_catacomb","配達人墓廊","CARGO",8,0,1,2,CourierResolutionKind.Cargo,
   "旧宛先印を回収","墓廊に残る宛先印を確保して出口へ向かう。",Next("final_relay"),
   "最後の回収物を探せる寄り道。"),
  Node("black_bell_yard","黒鐘の中庭","EVENT",8,1,2,1,CourierResolutionKind.Event,
   "黒鐘を停止","鐘が鳴る前に中庭を横断する。",Next("final_relay","pursuer_gate"),
   "安全寄りだが期限を多く消費する。"),

  Node("final_relay","第七中継所","RELAY",9,0,2,0,CourierResolutionKind.Relay,
   "最終検札を通過","収納術式を整え、目的地前の検札を抜ける。",Next("destination"),
   "目的地前で最後のHP回復と荷物整理を行える。"),
  Node("pursuer_gate","追跡者の門","PURSUIT",9,1,1,3,CourierResolutionKind.Battle,
   "追跡者の門を突破","門を塞ぐ追跡者を倒し、目的地へ進む。",Next("destination"),
   "期限は短いが最後の強敵が待つ。"),

  Node("destination","紫晶の封鐘塔","DESTINATION",10,0,1,0,CourierResolutionKind.Delivery,
   "最終配達","受取印へ荷を届け、配達を完了する。",Array.Empty<string>(),
   "期限内に荷を届ければ遠征成功。")
 };

 public static IReadOnlyList<CourierRouteNodeDef> Nodes=>NodesValue;
 public static CourierRouteNodeDef Node(string id)=>NodesValue.FirstOrDefault(x=>x.id==id);

 public static CourierRouteState Create(RunState run,MetaSave meta){
  string roleId=RoleFrameworkSystem.CoreRoleId(run?.role);
  var state=new CourierRouteState{
   roleId=roleId,
   roleBranch=RoleFrameworkSystem.Branch(meta,roleId),
   qualificationSealId=meta?.qualificationSealId??""
  };
  var colors=run==null?null:BackpackSystem.Build(run).colors;
  state.seals=DeliverySealSystem.Build(run,meta,colors);
  return state;
 }

 public static IReadOnlyList<CourierRouteNodeDef> Available(CourierRouteState state){
  var current=Node(state?.currentNodeId);
  if(state==null||current==null||state.complete||state.failed||state.awaitingResolution)
   return Array.Empty<CourierRouteNodeDef>();
  return current.next.Select(Node).Where(x=>x!=null).ToArray();
 }

 public static bool IsRevealed(CourierRouteState state,CourierRouteNodeDef node){
  if(state==null||node==null)return false;
  var current=Node(state.currentNodeId);
  return node.phase<=(current?.phase??0)+1||state.resolvedNodeIds.Contains(node.id)||state.selectedNodeId==node.id;
 }

 public static bool Select(CourierRouteState state,string id){
  if(state==null||!Available(state).Any(x=>x.id==id))return false;
  state.selectedNodeId=id;
  return true;
 }

 public static bool ClearSelection(CourierRouteState state){
  if(state==null||string.IsNullOrEmpty(state.selectedNodeId))return false;
  state.selectedNodeId="";
  return true;
 }

 public static bool UseSeal(CourierRouteState state,string key,out string message){
  message="その配達印は使用できません。";
  var seal=state?.seals?.FirstOrDefault(x=>x.key==key);
  if(seal==null||!seal.available||seal.charges<=0||state.complete||state.failed||state.awaitingResolution)return false;
  switch(seal.target){
   case DeliverySealSystem.DelayTarget:
    state.delayShield++;
    break;
   case DeliverySealSystem.SealTarget:
    var spent=state.seals.FirstOrDefault(x=>x!=seal&&x.available&&x.charges<x.maxCharges);
    if(spent==null){message="再装填できる使用済みの印がありません。";return false;}
    spent.charges++;
    break;
   default:
    state.nextSegmentDiscount++;
    break;
  }
  seal.charges--;
  message=$"{seal.name}を配達台帳へ押印しました。";
  return true;
 }

 public static bool Commit(RunState run,out string message){
  var state=run?.courierRoute;
  var node=Node(state?.selectedNodeId);
  if(state==null||node==null||!Available(state).Any(x=>x.id==node.id)){
   message="次の区間を選択してください。";
   return false;
  }
 int days=Mathf.Max(0,node.dayCost-state.nextSegmentDiscount);
  state.travelFromNodeId=state.currentNodeId;
  state.travelToNodeId=node.id;
  state.travelDayCost=days;
  state.travelPending=true;
  state.daysElapsed+=days;
  state.currentNodeId=node.id;
  state.selectedNodeId="";
  state.pendingResolution=node.resolution;
  state.nextSegmentDiscount=0;
  state.relayPackingAvailable=false;
  state.failed=FailureReached(state);
  state.awaitingResolution=!state.failed;
  message=state.failed
   ?"配達期限を超過しました。今回の遠征は失敗です。"
   :$"{node.title}へ到着。{node.resolutionTitle}を解決します。";
  return true;
 }

 public static bool CompleteTravel(CourierRouteState state){
  if(state==null||!state.travelPending)return false;
  state.travelPending=false;
  state.travelFromNodeId="";
  state.travelToNodeId="";
  state.travelDayCost=0;
  return true;
 }

 public static bool ResolveCurrent(RunState run,CourierLocationOutcome outcome,out string message){
 message="解決待ちの配達地点がありません。";
 var state=run?.courierRoute;
  var node=Node(state?.currentNodeId);
  if(state==null||node==null||state.failed||!state.awaitingResolution)return false;
  if(state.travelPending)CompleteTravel(state);
  outcome??=new CourierLocationOutcome();
  if(!outcome.success){
   state.failed=true;
   state.awaitingResolution=false;
   state.pendingResolution="";
   message=string.IsNullOrEmpty(outcome.message)?$"{node.title}で配達を継続できなくなりました。":outcome.message;
   return true;
  }

  int extraDays=Mathf.Max(0,outcome.dayDelta);
  if(state.delayShield>0){
   extraDays=Mathf.Max(0,extraDays-state.delayShield);
   state.delayShield=0;
  }
  state.daysElapsed+=extraDays;
  if(!state.resolvedNodeIds.Contains(node.id))state.resolvedNodeIds.Add(node.id);
  state.awaitingResolution=false;
  state.pendingResolution="";
  state.clearedSegments++;

  string reward="";
  switch(node.resolution){
   case CourierResolutionKind.Relay:
    int beforeHp=run.hp;
    run.hp=Mathf.Min(run.maxHp,run.hp+3);
    state.relayPackingAvailable=true;
    var refill=state.seals.FirstOrDefault(seal=>seal.charges<seal.maxCharges);
    if(refill!=null)refill.charges++;
    reward=$"中継補給：HP {beforeHp}→{run.hp}。収納術式を組み直せます。";
    break;
   case CourierResolutionKind.Cargo:
    if(outcome.cargoRecovered){
     state.recoveredCargoCount++;
     reward=$"回収物を未整理荷物へ追加しました。回収 {state.recoveredCargoCount}件。";
    } else reward="回収を断念し、期限を優先しました。";
    break;
   case CourierResolutionKind.Delivery:
    state.complete=true;
    int bonus=state.recoveredCargoCount*6;
    run.gold+=bonus;
    reward=bonus>0?$"最終配達を確定。回収物報酬 +{bonus}G。":"最終配達を確定しました。";
    break;
   case CourierResolutionKind.MiniGame:
    reward=outcome.performance>=2?"良好な手際で地点を突破しました。":"地点作業を完了しました。";
    break;
  }

  state.failed=FailureReached(state);
  string suffix=string.Join(" ",new[]{outcome.message,reward}.Where(value=>!string.IsNullOrWhiteSpace(value)));
  message=state.failed
   ?"配達期限を超過しました。今回の遠征は失敗です。"
   :string.IsNullOrEmpty(suffix)?$"{node.title}を解決。次の区間を選択できます。":suffix;
  return true;
 }

 public static bool OpenRelayPacking(RunState run,out int transferred,out string message){
  transferred=0;
  var state=run?.courierRoute;
  if(state==null||!state.relayPackingAvailable||state.awaitingResolution||state.failed||state.complete){
   message="現在地では荷物を組み直せません。";
   return false;
  }
  run.inventory??=new List<ItemInstance>();
  run.lootBag??=new List<ItemInstance>();
  foreach(var cargo in run.lootBag.ToArray()){
   if(run.inventory.All(item=>item.uid!=cargo.uid))run.inventory.Add(cargo);
   transferred++;
  }
  run.lootBag.Clear();
  state.relayPackingAvailable=false;
  message=transferred>0
   ?$"中継所で未整理荷物 {transferred}件を収納術式へ移しました。"
   :"中継所で収納術式を組み直します。";
  return true;
 }

 public static void RefreshPackingEffects(RunState run,MetaSave meta){
  var state=run?.courierRoute;
  if(state==null)return;
  var build=BackpackSystem.Build(run);
  state.seals=DeliverySealSystem.Refresh(state.seals,run,meta,build.colors);
 }

 static bool FailureReached(CourierRouteState state)=>state.daysElapsed>state.deadlineDays;

 static CourierRouteNodeDef Node(string id,string title,string kind,int phase,int lane,int days,int risk,
  string resolution,string resolutionTitle,string resolutionText,string[] next,string condition="")=>new(){
   id=id,title=title,kind=kind,phase=phase,lane=lane,dayCost=days,risk=risk,
   resolution=resolution,resolutionTitle=resolutionTitle,resolutionText=resolutionText,next=next,condition=condition
  };

 static string[] Next(params string[] ids)=>ids;
}
}
