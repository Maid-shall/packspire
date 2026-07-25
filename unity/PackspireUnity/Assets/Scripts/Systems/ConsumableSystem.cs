using System.Linq;
using UnityEngine;

namespace Packspire {
public static class ConsumableSystem {
 static ConsumableContent Definition(string id)=>
  PackspireContent.Data.consumables.FirstOrDefault(x=>x.id==id);

 public static string Name(string id)=>Definition(id)?.name??id;
 public static bool Use(RunState run,BattleState battle,int index)=>UseFx(run,battle,index).ok;

 public static BattleActionFx UseFx(RunState run,BattleState battle,int index){
  if(index<0||index>=run.consumables.Count)return BattleActionFx.Fail;
  string id=run.consumables[index];
  var consumable=Definition(id);
  if(consumable==null)return BattleActionFx.Fail;

  var fx=new BattleActionFx{ok=true,cardName=consumable.name,cardType=CardType.Skill};
  switch(consumable.effect){
   case ConsumableEffectType.Heal:{
    int before=run.hp;
    run.hp=Mathf.Min(run.maxHp,run.hp+consumable.amount);
    fx.healGained=run.hp-before;
    break;
   }
   case ConsumableEffectType.Block:
    run.block+=consumable.amount;
    fx.blockGained=consumable.amount;
    break;
   case ConsumableEffectType.Damage:{
    int dieOne,dieTwo,modifier;
    int rolled=BattleSystem.RollDamage(consumable.amount,out dieOne,out dieTwo,out modifier);
    battle.enemyHp-=rolled;
    fx.damageToEnemy=rolled;
    fx.dieOne=dieOne;fx.dieTwo=dieTwo;fx.damageModifier=modifier;fx.rolledDamage=rolled;
    fx.cardType=CardType.Attack;
    break;
   }
   case ConsumableEffectType.Energy:
    run.energy+=consumable.amount;
    fx.energyGained=consumable.amount;
    break;
   default:
    return BattleActionFx.Fail;
  }

  run.consumables.RemoveAt(index);
  battle.log=$"{consumable.name}を使用";
  fx.enemyDefeated=battle.enemyHp<=0;
  return fx;
 }
}
}
