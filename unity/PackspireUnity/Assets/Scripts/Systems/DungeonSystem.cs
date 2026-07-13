using System;
using System.Collections.Generic;
using UnityEngine;
namespace Packspire {
[Serializable] public class MapNode { public int id,x,y; public string type; public bool revealed,visited; }
[Serializable] public class DungeonMap { public List<MapNode> nodes=new(); public int current; public int alert; }
public static class DungeonSystem {
 public static DungeonMap Generate(){var map=new DungeonMap();string[] types={"empty","battle","event","rest","treasure","shop"};var rng=new System.Random();for(int y=0;y<6;y++)for(int x=0;x<6;x++)map.nodes.Add(new MapNode{id=y*6+x,x=x,y=y,type=types[rng.Next(types.Length)],revealed=x<=1});map.nodes[12].type="entrance";map.nodes[12].visited=true;map.nodes[23].type="boss";map.current=12;return map;}
 public static bool Adjacent(int a,int b)=>Mathf.Abs(a%6-b%6)+Mathf.Abs(a/6-b/6)==1;
 public static bool Move(DungeonMap map,int target,RunState run=null){if(!Adjacent(map.current,target))return false;map.current=target;var node=map.nodes[target];node.visited=true;foreach(var n in map.nodes)if(Adjacent(target,n.id))n.revealed=true;map.alert++;if(run!=null){run.axes??=new();int collapse=node.type=="treasure"?2:node.type=="rest"?-1:0,corruption=node.type=="event"?2:node.type=="battle"?1:0;run.axes.Change(1,collapse,corruption);}return true;}
}
}
