using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Packspire {
/// <summary>Map-layer enemy standing on a node (prototype MapDeck combat).</summary>
[Serializable]
public class MapEnemyState {
 public string id,name;
 public int nodeId,hp,maxHp;
}

/// <summary>
/// Prototype MapDeck combat: hop-range strikes on the exploration graph.
/// Battle hand/deck are untouched. Enable via <see cref="ExplorationRunState.mapCombatProto"/>.
/// </summary>
public static class MapCombatSystem {
 public const int DefaultMapEnergy=2;

 public static void EnsureProto(ExplorationRunState run){
  if(run==null||run.mapCombatSeeded)return;
  run.mapCombatSeeded=true;
  run.mapCombatProto=true;
  run.mapEnergy=DefaultMapEnergy;
  run.selectedMapCardUid="";
  run.mapHand??=new();
  run.mapEnemies??=new();
  run.mapHand.Clear();
  run.mapEnemies.Clear();
  run.mapHand.Add(MakeCard("map_stonebow","石弓",1,"射程2・4ダメージ。道のホップで届く敵を射る。",4));
  run.mapHand.Add(MakeCard("map_throw","投石",1,"射程1・3ダメージ。隣の地点の敵。",3));
  run.mapHand.Add(MakeCard("map_stonebow","石弓",1,"射程2・4ダメージ。道のホップで届く敵を射る。",4));
  // Place foes on battle / landmark nodes that exist on the active map.
  var def=ExplorationMapSystem.Def(run)??ExplorationMapCatalog.Get(run.mapId);
  int battleNode=def?.cells?.FirstOrDefault(c=>c.type=="battle"&&!c.locked)?.id
   ??def?.cells?.FirstOrDefault(c=>c.type!="entrance"&&!c.locked)?.id
   ??-1;
  int spurNode=def?.cells?.FirstOrDefault(c=>c.id!=battleNode&&(c.type=="building_door"||c.type=="rest")&&!c.locked)?.id??-1;
  if(battleNode>=0)
   run.mapEnemies.Add(new MapEnemyState{id="proto-yard",name="巡回兵",nodeId=battleNode,hp=8,maxHp=8});
  if(spurNode>=0)
   run.mapEnemies.Add(new MapEnemyState{id="proto-spur",name="見張り",nodeId=spurNode,hp=6,maxHp=6});
 }

 static CardInstance MakeCard(string id,string name,int cost,string text,int damage)=>new(){
  id=id,name=name,cost=cost,text=text,damage=damage,type=CardType.Attack,source="map-proto",slotKey=Guid.NewGuid().ToString("N")
 };

 public static int RangeFor(CardInstance card)=>card?.id switch{
  "map_stonebow"=>2,
  "map_throw"=>1,
  _=>0,
 };

 public static CardInstance SelectedCard(ExplorationRunState run){
  if(run==null||string.IsNullOrEmpty(run.selectedMapCardUid)||run.mapHand==null)return null;
  return run.mapHand.FirstOrDefault(c=>c.slotKey==run.selectedMapCardUid);
 }

 public static void SelectCard(ExplorationRunState run,string slotKey){
  if(run==null)return;
  run.selectedMapCardUid=run.selectedMapCardUid==slotKey?"":slotKey??"";
 }

 public static MapEnemyState EnemyAt(ExplorationRunState run,int nodeId){
  if(run?.mapEnemies==null)return null;
  return run.mapEnemies.FirstOrDefault(e=>e!=null&&e.nodeId==nodeId&&e.hp>0);
 }

 public static bool HasLivingEnemy(ExplorationRunState run,int nodeId)=>EnemyAt(run,nodeId)!=null;

 public static IEnumerable<MapEnemyState> LivingEnemies(ExplorationRunState run){
  if(run?.mapEnemies==null)yield break;
  foreach(var e in run.mapEnemies)if(e!=null&&e.hp>0)yield return e;
 }

 /// <summary>Shortest hop count using traversable edges only. -1 if unreachable.</summary>
 public static int HopDistance(ExplorationRunState run,int from,int to,int maxRange=8){
  if(run==null||from==to)return from==to?0:-1;
  var def=ExplorationMapSystem.Def(run);
  if(def==null)return -1;
  var dist=new Dictionary<int,int>{{from,0}};
  var q=new Queue<int>();
  q.Enqueue(from);
  while(q.Count>0){
   int u=q.Dequeue();
   int du=dist[u];
   if(du>=maxRange)continue;
   foreach(int v in ExplorationMapSystem.Neighbors(def,u)){
    if(!ExplorationMapSystem.EdgeTraversable(run,def,u,v))continue;
    if(dist.ContainsKey(v))continue;
    dist[v]=du+1;
    if(v==to)return dist[v];
    q.Enqueue(v);
   }
  }
  return -1;
 }

 /// <summary>True if a shortest path of length &lt;= range exists with no living enemy on intermediate nodes.</summary>
 public static bool HasClearShot(ExplorationRunState run,int from,int to,int range){
  if(run==null||range<=0)return false;
  int d=HopDistance(run,from,to,range);
  if(d<1||d>range)return false;
  var path=ShortestPath(run,from,to,range);
  if(path==null||path.Count<2)return false;
  for(int i=1;i<path.Count-1;i++){
   if(HasLivingEnemy(run,path[i]))return false;
  }
  return true;
 }

 public static List<int> ShortestPath(ExplorationRunState run,int from,int to,int maxRange=8){
  if(run==null)return null;
  if(from==to)return new List<int>{from};
  var def=ExplorationMapSystem.Def(run);
  if(def==null)return null;
  var prev=new Dictionary<int,int>();
  var dist=new Dictionary<int,int>{{from,0}};
  var q=new Queue<int>();
  q.Enqueue(from);
  while(q.Count>0){
   int u=q.Dequeue();
   int du=dist[u];
   if(du>=maxRange)continue;
   foreach(int v in ExplorationMapSystem.Neighbors(def,u)){
    if(!ExplorationMapSystem.EdgeTraversable(run,def,u,v))continue;
    if(dist.ContainsKey(v))continue;
    dist[v]=du+1;
    prev[v]=u;
    if(v==to){
     var path=new List<int>{to};
     int cur=to;
     while(cur!=from){cur=prev[cur];path.Add(cur);}
     path.Reverse();
     return path;
    }
    q.Enqueue(v);
   }
  }
  return null;
 }

 public static HashSet<int> NodesInStrikeRange(ExplorationRunState run,int range){
  var set=new HashSet<int>();
  if(run==null||range<=0)return set;
  int from=run.currentNodeId;
  foreach(var enemy in LivingEnemies(run)){
   if(HasClearShot(run,from,enemy.nodeId,range))set.Add(enemy.nodeId);
  }
  // Also paint intermediate path nodes for the first valid target (UI hint).
  foreach(int target in set.ToList()){
   var path=ShortestPath(run,from,target,range);
   if(path==null)continue;
   foreach(int n in path)set.Add(n);
  }
  return set;
 }

 public static bool TryPlayStrike(ExplorationRunState run,int targetNodeId,out string message){
  message="";
  if(run==null||!run.mapCombatProto){message="マップ戦闘プロトが無効";return false;}
  var card=SelectedCard(run);
  if(card==null){message="マップカードを選んでください";return false;}
  int range=RangeFor(card);
  if(range<=0){message="そのカードは射撃ではない";return false;}
  if(run.mapEnergy<card.cost){message="マップENが足りない";return false;}
  var enemy=EnemyAt(run,targetNodeId);
  if(enemy==null){message="そこには敵がいない";return false;}
  if(!HasClearShot(run,run.currentNodeId,targetNodeId,range)){message="射程外、または射線が塞がれている";return false;}

  run.mapEnergy-=card.cost;
  enemy.hp=Mathf.Max(0,enemy.hp-Mathf.Max(0,card.damage));
  run.mapHand.Remove(card);
  run.selectedMapCardUid="";
  if(enemy.hp<=0){
   run.mapEnemies.Remove(enemy);
   ExplorationMapSystem.MarkCleared(run,targetNodeId);
   message=$"{enemy.name}をマップ上で撃破した（軽報酬なし・試作）";
  } else {
   message=$"{enemy.name}に{card.damage}ダメージ（残り{enemy.hp}/{enemy.maxHp}）";
  }
  return true;
 }

 public static void RestockTurn(ExplorationRunState run){
  if(run==null||!run.mapCombatProto)return;
  run.mapEnergy=DefaultMapEnergy;
  // Keep remaining hand; refill up to 3 with basic stonebow if empty-ish.
  run.mapHand??=new();
  while(run.mapHand.Count<3)
   run.mapHand.Add(MakeCard("map_stonebow","石弓",1,"射程2・4ダメージ。道のホップで届く敵を射る。",4));
  run.selectedMapCardUid="";
 }
}
}
