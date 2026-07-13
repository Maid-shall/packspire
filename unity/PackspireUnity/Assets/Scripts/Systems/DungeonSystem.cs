using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Packspire {
[Serializable] public class MapNode {
 public int id,x,y;
 public string type,name;
 public bool revealed,visited,cleared,landmark;
}
[Serializable] public class MapEdge { public int a,b; public bool secret; public MapEdge(int a,int b,bool secret=false){this.a=a;this.b=b;this.secret=secret;} }
[Serializable] public class DungeonZone {
 public int index,seed,current,entrance,exit,requiredBattles=2;
 public string name;
 public bool completed,transitionClaimed;
 public List<MapNode> nodes=new();
 public List<MapEdge> edges=new();
}
[Serializable] public class DungeonMap {
 public List<MapNode> nodes=new();
 public List<MapEdge> edges=new();
 public List<DungeonZone> zones=new();
 public int current,currentZone,entrance,exit,alert,steps;
 public string dungeon;
}

public static class DungeonSystem {
 const int ZoneCount=5,NodeCount=36;

 public static DungeonMap Generate(string dungeon="old_spire"){
  var map=new DungeonMap{dungeon=dungeon};
  int rootSeed=Environment.TickCount^dungeon.GetHashCode()^UnityEngine.Random.Range(1,int.MaxValue);
  for(int zoneIndex=0;zoneIndex<ZoneCount;zoneIndex++)map.zones.Add(GenerateZone(dungeon,zoneIndex,rootSeed+zoneIndex*7919));
  ActivateZone(map,0);
  return map;
 }

 static DungeonZone GenerateZone(string dungeon,int zoneIndex,int seed){
  var zone=new DungeonZone{index=zoneIndex,seed=seed,name=ZoneName(dungeon,zoneIndex),requiredBattles=zoneIndex==4?3:2};
  var rng=new System.Random(seed);
  zone.nodes.Add(new MapNode{id=zoneIndex*100,x=6,y=rng.Next(42,76),type="entrance",name=zoneIndex==0?"遠征入口":"前区域からの道",revealed=true,visited=true,landmark=true});
  zone.nodes.Add(new MapNode{id=zoneIndex*100+1,x=94,y=rng.Next(22,79),type=zoneIndex==4?"boss":"gate",name=zoneIndex==4?"区域主の座":"次区域への門",landmark=true});
  int attempts=0;
  while(zone.nodes.Count<NodeCount){
   int x=rng.Next(9,92),y=rng.Next(10,91);float minimum=attempts<1400?9f:attempts<2600?7f:5f;
   if(zone.nodes.All(n=>(n.x-x)*(n.x-x)+(n.y-y)*(n.y-y)>=minimum*minimum))zone.nodes.Add(new MapNode{id=zoneIndex*100+zone.nodes.Count,x=x,y=y});
   attempts++;
  }
  var types=new List<string>();
  Add(types,"battle",9);Add(types,"event",5);Add(types,"treasure",4);Add(types,"rest",2);Add(types,"shop",1);Add(types,"empty",13);
  while(types.Count<zone.nodes.Count-2)types.Add("empty");
  Shuffle(types,rng);
  for(int i=2;i<zone.nodes.Count;i++){zone.nodes[i].type=types[i-2];zone.nodes[i].name=NodeTitle(zone.nodes[i].type,zoneIndex,i);zone.nodes[i].landmark=zone.nodes[i].type=="rest"||zone.nodes[i].type=="shop";}
  BuildConnectedGraph(zone,rng);
  zone.entrance=zone.nodes[0].id;zone.exit=zone.nodes[1].id;zone.current=zone.entrance;
  RevealNeighbors(zone,zone.entrance);
  return zone;
 }

 static void BuildConnectedGraph(DungeonZone zone,System.Random rng){
  var connected=new HashSet<int>{zone.nodes[0].id};
  while(connected.Count<zone.nodes.Count){
   MapNode bestA=null,bestB=null;float best=float.MaxValue;
   foreach(int id in connected){var a=zone.nodes.FirstOrDefault(n=>n.id==id);if(a==null)continue;foreach(var b in zone.nodes.Where(n=>!connected.Contains(n.id))){float d=Distance(a,b);if(d<best){best=d;bestA=a;bestB=b;}}}
   if(bestA==null||bestB==null){var fallback=zone.nodes.FirstOrDefault(n=>!connected.Contains(n.id));if(fallback==null)break;bestA=zone.nodes[0];bestB=fallback;}
   zone.edges.Add(new MapEdge(bestA.id,bestB.id));connected.Add(bestB.id);
  }
  foreach(var node in zone.nodes){
   var nearest=zone.nodes.Where(n=>n.id!=node.id).OrderBy(n=>Distance(node,n)).Take(4).ToArray();
   foreach(var other in nearest){if(Degree(zone,node.id)>=3)break;if(Distance(node,other)<=27f&&rng.NextDouble()<.58&&!HasEdge(zone,node.id,other.id))zone.edges.Add(new MapEdge(node.id,other.id));}
  }
  EnsureDegree(zone,zone.entrance,2);EnsureDegree(zone,zone.exit,2);
 }

 static void EnsureDegree(DungeonZone zone,int id,int target){
  var node=zone.nodes.FirstOrDefault(n=>n.id==id);if(node==null)return;
  foreach(var other in zone.nodes.Where(n=>n.id!=id).OrderBy(n=>Distance(node,n))){if(Degree(zone,id)>=target)break;if(!HasEdge(zone,id,other.id))zone.edges.Add(new MapEdge(id,other.id));}
 }
 static int Degree(DungeonZone zone,int id)=>zone.edges.Count(e=>e.a==id||e.b==id);
 static bool HasEdge(DungeonZone zone,int a,int b)=>zone.edges.Any(e=>(e.a==a&&e.b==b)||(e.a==b&&e.b==a));
 static float Distance(MapNode a,MapNode b)=>Mathf.Sqrt((a.x-b.x)*(a.x-b.x)+(a.y-b.y)*(a.y-b.y));
 static void Add(List<string> values,string value,int count){for(int i=0;i<count;i++)values.Add(value);}
 static void Shuffle<T>(List<T> values,System.Random rng){for(int i=values.Count-1;i>0;i--){int j=rng.Next(i+1);(values[i],values[j])=(values[j],values[i]);}}

 public static DungeonZone CurrentZone(DungeonMap map)=>map.zones[Mathf.Clamp(map.currentZone,0,map.zones.Count-1)];
 public static void ActivateZone(DungeonMap map,int zoneIndex){
  map.currentZone=Mathf.Clamp(zoneIndex,0,map.zones.Count-1);var zone=CurrentZone(map);
  map.nodes=zone.nodes;map.edges=zone.edges;map.current=zone.current;map.entrance=zone.entrance;map.exit=zone.exit;
 }
 public static bool AdvanceZone(DungeonMap map){
  if(map.currentZone>=map.zones.Count-1||map.current!=map.exit)return false;
  var previous=CurrentZone(map);previous.completed=true;previous.transitionClaimed=true;
  ActivateZone(map,map.currentZone+1);return true;
 }
 public static bool Connected(DungeonMap map,int a,int b)=>map.edges.Any(e=>(e.a==a&&e.b==b)||(e.a==b&&e.b==a));
 public static bool Adjacent(int a,int b)=>Mathf.Abs(a%6-b%6)+Mathf.Abs(a/6-b/6)==1;
 public static IEnumerable<int> Neighbors(DungeonMap map,int id)=>map.edges.Where(e=>e.a==id||e.b==id).Select(e=>e.a==id?e.b:e.a);
 public static int ClearedBattles(DungeonMap map)=>map.nodes.Count(n=>n.type=="battle"&&n.cleared);
 public static bool ExitUnlocked(DungeonMap map)=>ClearedBattles(map)>=CurrentZone(map).requiredBattles;
 public static bool CanMove(DungeonMap map,int target){
  if(!Connected(map,map.current,target))return false;var node=map.nodes.FirstOrDefault(n=>n.id==target);if(node==null)return false;
  if((node.type=="gate"||node.type=="boss")&&!ExitUnlocked(map))return false;return true;
 }
 public static bool Move(DungeonMap map,int target,RunState run=null){
  if(!CanMove(map,target))return false;var node=map.nodes.FirstOrDefault(n=>n.id==target);if(node==null)return false;map.current=target;CurrentZone(map).current=target;node.visited=true;node.revealed=true;RevealNeighbors(CurrentZone(map),target);map.alert++;map.steps++;
  if(run!=null){run.axes??=new();int collapse=node.type=="treasure"?2:node.type=="rest"?-1:0,corruption=node.type=="event"?2:node.type=="battle"?1:0;run.axes.Change(1,collapse,corruption);}return true;
 }
 static void RevealNeighbors(DungeonZone zone,int center){foreach(int id in zone.edges.Where(e=>e.a==center||e.b==center).Select(e=>e.a==center?e.b:e.a)){var node=zone.nodes.FirstOrDefault(n=>n.id==id);if(node!=null)node.revealed=true;}}

 public static string ZoneName(string dungeon,int index){
  string[][] names={
   new[]{"城門外郭","崩落市街","中央城砦","上層回廊","パックスパイア主塔"},
   new[]{"灰積む搬入口","熔鉄坑道","炉心工房","灼熱昇降路","大熔炉中枢"},
   new[]{"忘却前庭","沈黙書架","封印収蔵区","深層記録路","虚ろなる中枢"}
  };
  int row=dungeon=="ash_forge"?1:dungeon=="hollow_archive"?2:0;return names[row][Mathf.Clamp(index,0,4)];
 }
 static string NodeTitle(string type,int zone,int index){string[] ruins={"崩れた回廊","古い見張り台","苔むす広場","封鎖された路地","風化した橋","忘れられた倉庫"};return type=="battle"?"敵影":type=="event"?"異変の気配":type=="treasure"?"埋もれた保管所":type=="rest"?"野営可能地":type=="shop"?"行商人の停泊地":ruins[(zone+index)%ruins.Length];}
}
}
