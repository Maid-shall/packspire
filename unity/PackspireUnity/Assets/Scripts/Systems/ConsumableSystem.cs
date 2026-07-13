using System.Collections.Generic;
using UnityEngine;

namespace Packspire {
public static class ConsumableSystem {
 static readonly Dictionary<string,string> Names=new(){{"heal","治療薬"},{"guard","硬化薬"},{"fire","火炎瓶"},{"energy","活力薬"}};
 public static string Name(string id)=>Names.GetValueOrDefault(id,id);
 public static bool Use(RunState run,BattleState battle,int index){
  if(index<0||index>=run.consumables.Count)return false;string id=run.consumables[index];
  switch(id){case "heal":run.hp=Mathf.Min(run.maxHp,run.hp+12);break;case "guard":run.block+=10;break;case "fire":battle.enemyHp-=12;break;case "energy":run.energy+=2;break;default:return false;}
  run.consumables.RemoveAt(index);battle.log=$"{Name(id)}を使用";return true;
 }
}
}
