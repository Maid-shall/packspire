using System.Collections.Generic;
using UnityEngine;

namespace Packspire {
public static class ConsumableSystem {
 static readonly Dictionary<string,string> Names=new(){{"heal","治療薬"},{"guard","硬化薬"},{"fire","火炎瓶"},{"energy","活力薬"}};
 public static string Name(string id)=>Names.TryGetValue(id,out var name)?name:id;
 public static bool Use(RunState run,BattleState battle,int index)=>UseFx(run,battle,index).ok;
 public static BattleActionFx UseFx(RunState run,BattleState battle,int index){
  if(index<0||index>=run.consumables.Count)return BattleActionFx.Fail;
  string id=run.consumables[index];
  var fx=new BattleActionFx{ok=true,cardName=Name(id),cardType=CardType.Skill};
  switch(id){
   case "heal":{
    int before=run.hp;
    run.hp=Mathf.Min(run.maxHp,run.hp+12);
    fx.healGained=run.hp-before;
    break;
   }
   case "guard":
    run.block+=10;
    fx.blockGained=10;
    break;
   case "fire":
    battle.enemyHp-=12;
    fx.damageToEnemy=12;
    fx.cardType=CardType.Attack;
    break;
   case "energy":
    run.energy+=2;
    fx.energyGained=2;
    break;
   default:
    return BattleActionFx.Fail;
  }
  run.consumables.RemoveAt(index);
  battle.log=$"{Name(id)}を使用";
  fx.enemyDefeated=battle.enemyHp<=0;
  return fx;
 }
}
}
