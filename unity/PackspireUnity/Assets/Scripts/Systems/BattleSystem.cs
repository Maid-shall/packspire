using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Packspire {
[Serializable] public class BattleState { public EnemyDef enemy; public int enemyHp,enemyMaxHp,enemyBlock,move; public List<StatusState> enemyStatuses=new(); public string log="戦闘開始"; }

public struct BattleActionFx {
 public bool ok,enemyDefeated,playerDefeated;
 public int damageToEnemy,damageToPlayer,blockGained,healGained,energyGained,selfDamage;
 public CardType cardType;
 public string cardName;
 public static BattleActionFx Fail=>new();
}

public static class BattleSystem {
 static readonly System.Random Rng=new();

 /// <summary>
 /// Start a combat. Clears battle-ephemeral run fields; keeps expedition-persistent fields.
 /// Persistent across battles: hp, maxHp, gold, inventory, lootBag, axes, battlesWon, consumables, role/dungeon/loadout ids.
 /// Battle-ephemeral (reset here): hand, draw, discard, deck rebuild, energy, block, attackBuff, statuses, selectedCardSlots combat use.
 /// </summary>
 public static BattleState Begin(RunState run,EnemyDef enemy,float hpScale=1,int bonusDamage=0){
  ResetBattleEphemeral(run);
  run.deck=BackpackSystem.BuildDeck(run);
  run.draw=Shuffle(run.deck.Select(x=>x.Clone()).ToList());
  Draw(run,5);
  int hp=Mathf.RoundToInt(enemy.hp*hpScale);
  var battle=new BattleState{enemy=enemy,enemyHp=hp,enemyMaxHp=hp,enemyBlock=0,move=0,enemyStatuses=new(),log="戦闘開始"};
  CharacterSystem.OnBattleBegin(run,battle);
  return battle;
 }

 /// <summary>Idempotent wipe of values that must not leak between battles.</summary>
 public static void ResetBattleEphemeral(RunState run){
  if(run==null)return;
  run.statuses??=new();
  run.statuses.Clear();
  run.hand??=new();run.hand.Clear();
  run.draw??=new();run.draw.Clear();
  run.discard??=new();run.discard.Clear();
  run.energy=3;
  run.block=0;
  run.attackBuff=0;
  run.activeSkillUsed=false;
 }

 public static bool Play(RunState run,BattleState battle,int handIndex)=>PlayCard(run,battle,handIndex).enemyDefeated;

 public static BattleActionFx PlayCard(RunState run,BattleState battle,int handIndex){
  if(handIndex<0||handIndex>=run.hand.Count)return BattleActionFx.Fail;
  var c=run.hand[handIndex];
  if(c.cost>run.energy)return BattleActionFx.Fail;
  run.energy-=c.cost;
  int raw=Damage(c.damage+run.attackBuff,run.statuses,battle.enemyStatuses);
  int dealt=Mathf.Max(0,raw-battle.enemyBlock);
  battle.enemyBlock=Mathf.Max(0,battle.enemyBlock-raw);
  battle.enemyHp-=dealt;
  run.attackBuff=0;
  int gainedBlock=Block(c.block,run.statuses);
  run.block+=gainedBlock;
  int healed=Mathf.Min(c.heal,Mathf.Max(0,run.maxHp-run.hp));
  run.hp=Mathf.Min(run.maxHp,run.hp+c.heal);
  int self=Mathf.Min(c.selfDamage,Mathf.Max(0,run.hp-1));
  run.hp=Mathf.Max(1,run.hp-c.selfDamage);
  run.attackBuff+=c.buff;
  int energyGain=c.energy;
  run.energy+=c.energy;
  ApplyEffects(run,battle,c.effects,false);
  var item=run.inventory.FirstOrDefault(x=>x.uid==c.sourceItemUid);
  if(item!=null&&!c.durabilityFree){
   if(item.uid==run.heirloomUid)item.uses++;
   item.durability=Mathf.Max(0,item.durability-1);
  }
  run.hand.RemoveAt(handIndex);
  if(!c.exhaust){if(c.recycle)run.draw.Add(c);else run.discard.Add(c);}
  if(c.draw>0)Draw(run,c.draw);
  battle.log=$"{c.name}：{dealt}ダメージ / {gainedBlock}防御{EffectText(c.effects)}";
  return new BattleActionFx{
   ok=true,
   enemyDefeated=battle.enemyHp<=0,
   damageToEnemy=dealt,
   blockGained=gainedBlock,
   healGained=healed,
   energyGained=energyGain,
   selfDamage=self,
   cardType=c.type,
   cardName=c.name
  };
 }

 public static BattleActionFx EndTurnFx(RunState run,BattleState battle,int dungeonDamage=0){
  run.discard.AddRange(run.hand);
  run.hand.Clear();
  int moveIndex=battle.move%battle.enemy.damages.Length;
  int raw=Damage(battle.enemy.damages[moveIndex]+dungeonDamage,battle.enemyStatuses,run.statuses);
  int damage=Mathf.Max(0,raw-run.block);
  run.hp-=damage;
  run.block=0;
  Tick(battle.enemyStatuses,ref battle.enemyHp,battle.enemyMaxHp);
  Tick(run.statuses,ref run.hp,run.maxHp);
  var effects=ContentDatabase.EnemyEffects(battle.enemy.name,moveIndex);
  ApplyEffects(run,battle,effects,true);
  battle.move++;
  run.energy=3;
  Draw(run,5);
  battle.log=$"{battle.enemy.name}の攻撃：{damage}ダメージ{EffectText(effects)}";
  return new BattleActionFx{
   ok=true,
   playerDefeated=run.hp<=0,
   damageToPlayer=damage,
   cardName=battle.enemy.name
  };
 }

 public static bool EndTurn(RunState run,BattleState battle,int dungeonDamage=0)=>EndTurnFx(run,battle,dungeonDamage).playerDefeated;
 public static void Draw(RunState run,int n){while(n-->0){if(run.draw.Count==0){run.draw=Shuffle(run.discard);run.discard=new();}if(run.draw.Count==0)return;var c=run.draw[^1];run.draw.RemoveAt(run.draw.Count-1);run.hand.Add(c);}}
 public static int Status(List<StatusState> statuses,string type)=>statuses.FirstOrDefault(x=>x.type==type)?.amount??0;
 public static int Damage(int value,List<StatusState> attacker,List<StatusState> defender){value+=Status(attacker,"strength");if(Status(attacker,"weak")>0)value=Mathf.FloorToInt(value*.75f);if(Status(defender,"vulnerable")>0)value=Mathf.CeilToInt(value*1.5f);return Mathf.Max(0,value);}
 public static int Block(int value,List<StatusState> statuses)=>Mathf.Max(0,value-Status(statuses,"armorBreak"));
 public static void Apply(List<StatusState> statuses,EffectSpec effect){if(ContentDatabase.Status(effect.type)==null)return;var current=statuses.FirstOrDefault(x=>x.type==effect.type);if(current==null){current=new StatusState{type=effect.type};statuses.Add(current);}current.amount+=Mathf.Max(1,effect.amount);current.duration=Mathf.Max(current.duration,effect.duration);}
 static void ApplyEffects(RunState run,BattleState battle,List<EffectSpec> effects,bool enemySource){foreach(var effect in effects){var target=effect.target=="enemy"?battle.enemyStatuses:effect.target=="player"?run.statuses:effect.target=="self"?(enemySource?battle.enemyStatuses:run.statuses):battle.enemyStatuses;Apply(target,effect);}}
 static void Tick(List<StatusState> statuses,ref int hp,int maxHp){foreach(var status in statuses.ToList()){if(status.type=="poison"){hp=Mathf.Max(0,hp-status.amount);status.amount--;}if(status.type=="burn")hp=Mathf.Max(0,hp-status.amount);if(status.type=="regen")hp=Mathf.Min(maxHp,hp+status.amount);if(status.duration>0)status.duration--;var def=ContentDatabase.Status(status.type);if(status.amount<=0||status.duration==0&&def!=null&&!def.stack)statuses.Remove(status);}}
 static string EffectText(List<EffectSpec> effects)=>effects.Count==0?"":" / "+string.Join("・",effects.Select(x=>$"{ContentDatabase.Status(x.type)?.name}{x.amount}"));
 static List<T> Shuffle<T>(List<T> list)=>list.OrderBy(_=>Rng.Next()).ToList();
}
}
