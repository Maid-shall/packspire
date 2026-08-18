using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Packspire {
[Serializable] public class BattleState {
 public EnemyDef enemy;
 public int enemyHp,enemyMaxHp,enemyBlock,move;
 public int enemyPhaseThreshold=int.MinValue,enemyPhaseMove;
 public List<StatusState> enemyStatuses=new();
 public List<string> logLines=new();
 public string log="戦闘開始";
}

public struct BattleActionFx {
 public bool ok,enemyDefeated,playerDefeated;
 public int damageToEnemy,damageToPlayer,blockGained,healGained,energyGained,selfDamage;
 public int statusDamageToEnemy,statusDamageToPlayer;
 public int enemyBlockGained,enemyHealGained;
 public EnemyMoveKind enemyMoveKind;
 public int dieOne,dieTwo,damageModifier,rolledDamage;
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
  var innate=run.draw.Where(card=>card.innate).ToList();
  foreach(var card in innate)run.draw.Remove(card);
  run.hand.AddRange(innate);
  Draw(run,Mathf.Max(0,PackspireContent.Data.balance.initialHand-run.hand.Count));
  int hp=Mathf.RoundToInt(enemy.hp*hpScale);
  var battle=new BattleState{enemy=enemy,enemyHp=hp,enemyMaxHp=hp,enemyBlock=0,move=0,enemyStatuses=new(),log="戦闘開始"};
  Record(battle,"戦闘開始");
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
  run.energy=PackspireContent.Data.balance.baseEnergy;
  run.block=0;
  run.attackBuff=0;
  run.activeSkillUsed=false;
 }

 public static bool Play(RunState run,BattleState battle,int handIndex)=>PlayCard(run,battle,handIndex).enemyDefeated;

 public static BattleActionFx PlayCard(RunState run,BattleState battle,int handIndex){
  if(handIndex<0||handIndex>=run.hand.Count)return BattleActionFx.Fail;
  var c=run.hand[handIndex];
  if(c.unplayable||c.cost>run.energy)return BattleActionFx.Fail;
  run.energy-=c.cost;
  int dieOne=0,dieTwo=0,modifier=0;
  int rolled=c.damage>0?RollDamage(c.damage,out dieOne,out dieTwo,out modifier):0;
  int raw=Damage(rolled+run.attackBuff,run.statuses,battle.enemyStatuses);
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
  bool exhaust=c.exhaust||c.afterUse!=BattleCardAfterUse.Discard;
  if(c.afterUse==BattleCardAfterUse.RemoveExpedition&&!string.IsNullOrEmpty(c.slotKey)){
   run.removedBattleCardSlots??=new();
   if(!run.removedBattleCardSlots.Contains(c.slotKey))
    run.removedBattleCardSlots.Add(c.slotKey);
  }
  if(!exhaust){if(c.recycle)run.draw.Add(c);else run.discard.Add(c);}
  if(c.draw>0)Draw(run,c.draw);
  var details=new List<string>();
  if(dealt>0)details.Add($"{dealt}ダメージ");
  if(gainedBlock>0)details.Add($"{gainedBlock}ブロック");
  if(healed>0)details.Add($"HP+{healed}");
  if(energyGain!=0)details.Add($"EN{(energyGain>0?"+":"")}{energyGain}");
  if(c.draw>0)details.Add($"{c.draw}枚ドロー");
  if(self>0)details.Add($"自傷{self}");
  if(exhaust)details.Add(c.afterUse==BattleCardAfterUse.RemoveExpedition?"遠征中除外":"廃棄");
  string effects=EffectText(c.effects);
  if(!string.IsNullOrEmpty(effects))details.Add(effects);
  Record(battle,$"{c.name}：{(details.Count>0?string.Join(" / ",details):"効果なし")}");
  return new BattleActionFx{
   ok=true,
   enemyDefeated=battle.enemyHp<=0,
   damageToEnemy=dealt,
   blockGained=gainedBlock,
   healGained=healed,
   energyGained=energyGain,
   selfDamage=self,
   dieOne=dieOne,
   dieTwo=dieTwo,
   damageModifier=modifier,
   rolledDamage=raw,
   cardType=c.type,
   cardName=c.name
  };
 }

 public static BattleActionFx EndTurnFx(RunState run,BattleState battle,int dungeonDamage=0){
  var retained=new List<CardInstance>();
  foreach(var card in run.hand){
   if(card.ethereal)continue;
   if(card.retain)retained.Add(card);
   else run.discard.Add(card);
  }
  run.hand=retained;
  int enemyStatusDamage=Tick(battle.enemyStatuses,ref battle.enemyHp,battle.enemyMaxHp);
  if(battle.enemyHp<=0){
   Record(battle,$"{battle.enemy.name}は継続ダメージで倒れた");
   return new BattleActionFx{
    ok=true,enemyDefeated=true,statusDamageToEnemy=enemyStatusDamage,
    cardName=battle.enemy.name
   };
  }
  int moveIndex=NextEnemyMoveIndex(battle);
  var activePhase=ContentDatabase.EnemyPhase(battle.enemy.id,battle.enemyHp,battle.enemyMaxHp);
  if(activePhase!=null&&battle.enemyPhaseThreshold!=activePhase.minimumHpPercent){
   battle.enemyPhaseThreshold=activePhase.minimumHpPercent;
   battle.enemyPhaseMove=0;
   Record(battle,$"{battle.enemy.name}は「{activePhase.name}」へ移行");
  }
  var move=ContentDatabase.EnemyMove(battle.enemy.id,moveIndex);
  int baseDamage=move?.damage??battle.enemy.damages[moveIndex];
  int dieOne=0,dieTwo=0,modifier=0;
  int rolled=baseDamage>0?RollDamage(baseDamage+dungeonDamage,out dieOne,out dieTwo,out modifier):0;
  int raw=baseDamage>0?Damage(rolled,battle.enemyStatuses,run.statuses):0;
  int damage=Mathf.Max(0,raw-run.block);
  run.hp-=damage;
  run.block=0;
  int playerStatusDamage=Tick(run.statuses,ref run.hp,run.maxHp);
  int enemyBlock=Mathf.Max(0,move?.block??0);
  battle.enemyBlock+=enemyBlock;
  int enemyHeal=Mathf.Min(Mathf.Max(0,move?.heal??0),Mathf.Max(0,battle.enemyMaxHp-battle.enemyHp));
  battle.enemyHp+=enemyHeal;
  var effects=ContentDatabase.EnemyEffects(battle.enemy.id,moveIndex);
  ApplyEffects(run,battle,effects,true);
  battle.move++;
  if(activePhase!=null)battle.enemyPhaseMove++;
  run.energy=PackspireContent.Data.balance.baseEnergy;
  Draw(run,Mathf.Max(0,PackspireContent.Data.balance.initialHand-run.hand.Count));
  var details=new List<string>();
  if(damage>0)details.Add($"{damage}ダメージ");
  if(enemyBlock>0)details.Add($"{enemyBlock}ブロック");
  if(enemyHeal>0)details.Add($"HP+{enemyHeal}");
  if(enemyStatusDamage>0)details.Add($"敵継続ダメージ{enemyStatusDamage}");
  if(playerStatusDamage>0)details.Add($"継続ダメージ{playerStatusDamage}");
  string effectText=EffectText(effects);
  if(!string.IsNullOrEmpty(effectText))details.Add(effectText);
  if(details.Count==0)details.Add("特殊行動");
  string moveName=!string.IsNullOrEmpty(move?.name)?move.name:"攻撃";
  Record(battle,$"{battle.enemy.name}の{moveName}：{string.Join(" / ",details)}");
  return new BattleActionFx{
   ok=true,
   playerDefeated=run.hp<=0,
   damageToPlayer=damage,
   statusDamageToEnemy=enemyStatusDamage,
   statusDamageToPlayer=playerStatusDamage,
   enemyBlockGained=enemyBlock,
   enemyHealGained=enemyHeal,
   enemyMoveKind=move?.kind??EnemyMoveKind.Attack,
   dieOne=dieOne,
   dieTwo=dieTwo,
   damageModifier=modifier,
   rolledDamage=raw,
   cardName=moveName
  };
 }

 public static int NextEnemyMoveIndex(BattleState battle){
  if(battle?.enemy?.damages==null||battle.enemy.damages.Length==0)return 0;
  var phase=ContentDatabase.EnemyPhase(battle.enemy.id,battle.enemyHp,battle.enemyMaxHp);
  var indices=phase?.moveIndices?
   .Where(index=>index>=0&&index<battle.enemy.damages.Length)
   .ToArray();
  if(indices!=null&&indices.Length>0){
   int phaseMove=battle.enemyPhaseThreshold==phase.minimumHpPercent?battle.enemyPhaseMove:0;
   return indices[((phaseMove%indices.Length)+indices.Length)%indices.Length];
  }
  return ((battle.move%battle.enemy.damages.Length)+battle.enemy.damages.Length)%battle.enemy.damages.Length;
 }

 public static string EnemyPhaseName(BattleState battle)=>
  battle?.enemy==null?string.Empty:
  ContentDatabase.EnemyPhase(battle.enemy.id,battle.enemyHp,battle.enemyMaxHp)?.name??string.Empty;

 public static bool EndTurn(RunState run,BattleState battle,int dungeonDamage=0)=>EndTurnFx(run,battle,dungeonDamage).playerDefeated;
 public static BattleActionFx ResolveRealtimeEnemyHit(RunState run,BattleState battle,int baseDamage,string actionName,bool sequenceEnd){
  if(run==null||battle==null)return BattleActionFx.Fail;
  int raw=baseDamage>0?Damage(baseDamage,battle.enemyStatuses,run.statuses):0;
  int absorbed=Mathf.Min(run.block,raw);
  run.block=Mathf.Max(0,run.block-raw);
  int damage=Mathf.Max(0,raw-absorbed);
  run.hp=Mathf.Max(0,run.hp-damage);
  if(sequenceEnd)battle.move++;
  string label=string.IsNullOrEmpty(actionName)?"攻撃":actionName;
  Record(battle,$"{battle.enemy.name}の{label}：{damage}ダメージ");
  return new BattleActionFx{
   ok=true,playerDefeated=run.hp<=0,enemyDefeated=battle.enemyHp<=0,
   damageToPlayer=damage,rolledDamage=raw,cardName=label
  };
 }
 public static void Draw(RunState run,int n){while(n-->0){if(run.draw.Count==0){run.draw=Shuffle(run.discard);run.discard=new();}if(run.draw.Count==0)return;var c=run.draw[^1];run.draw.RemoveAt(run.draw.Count-1);run.hand.Add(c);}}
 public static int Status(List<StatusState> statuses,string type)=>statuses.FirstOrDefault(x=>x.type==type)?.amount??0;
 public static int RollDamage(int expected,out int dieOne,out int dieTwo,out int modifier){
  dieOne=Rng.Next(1,7);
  dieTwo=Rng.Next(1,7);
  modifier=expected-7;
  return Mathf.Max(0,dieOne+dieTwo+modifier);
 }
 public static int Damage(int value,List<StatusState> attacker,List<StatusState> defender){value+=Status(attacker,"strength");if(Status(attacker,"weak")>0)value=Mathf.FloorToInt(value*.75f);if(Status(defender,"vulnerable")>0)value=Mathf.CeilToInt(value*1.5f);return Mathf.Max(0,value);}
 public static int Block(int value,List<StatusState> statuses)=>Mathf.Max(0,value-Status(statuses,"armorBreak"));
 public static void Apply(List<StatusState> statuses,EffectSpec effect){if(ContentDatabase.Status(effect.type)==null)return;var current=statuses.FirstOrDefault(x=>x.type==effect.type);if(current==null){current=new StatusState{type=effect.type};statuses.Add(current);}current.amount+=Mathf.Max(1,effect.amount);current.duration=Mathf.Max(current.duration,effect.duration);}
 static void ApplyEffects(RunState run,BattleState battle,List<EffectSpec> effects,bool enemySource){foreach(var effect in effects){var target=effect.target=="enemy"?battle.enemyStatuses:effect.target=="player"?run.statuses:effect.target=="self"?(enemySource?battle.enemyStatuses:run.statuses):battle.enemyStatuses;Apply(target,effect);}}
 static int Tick(List<StatusState> statuses,ref int hp,int maxHp){
  int before=hp;
  foreach(var status in statuses.ToList()){
   bool timed=status.duration>0;
   if(status.type=="poison"){
    hp=Mathf.Max(0,hp-status.amount);
    status.amount--;
   }
   if(status.type=="burn")hp=Mathf.Max(0,hp-status.amount);
   if(status.type=="regen")hp=Mathf.Min(maxHp,hp+status.amount);
   if(timed)status.duration--;
   if(status.amount<=0||(timed&&status.duration<=0))statuses.Remove(status);
  }
  return Mathf.Max(0,before-hp);
 }
 static string EffectText(List<EffectSpec> effects)=>effects.Count==0?"":string.Join("・",effects.Select(x=>$"{ContentDatabase.Status(x.type)?.name}{x.amount}"));
 public static void Record(BattleState battle,string line){
  if(battle==null||string.IsNullOrEmpty(line))return;
  battle.log=line;
  battle.logLines??=new();
  battle.logLines.Add(line);
  const int maximumLines=24;
  if(battle.logLines.Count>maximumLines)
   battle.logLines.RemoveRange(0,battle.logLines.Count-maximumLines);
 }
 static List<T> Shuffle<T>(List<T> list)=>list.OrderBy(_=>Rng.Next()).ToList();
}
}
