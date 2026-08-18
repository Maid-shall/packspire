using UnityEngine;

namespace Packspire {
public readonly struct CharacterSkillResult {
 public readonly bool success;
 public readonly bool enemyDefeated;
 public readonly string logLine;
 public readonly BattleActionFx fx;
 public CharacterSkillResult(bool success,bool enemyDefeated,string logLine,BattleActionFx fx=default){
  this.success=success;this.enemyDefeated=enemyDefeated;this.logLine=logLine;this.fx=fx;
 }
 public static CharacterSkillResult Fail=>new(false,false,"",BattleActionFx.Fail);
}

public static class CharacterSystem {
 public static CharacterDef Selected(MetaSave meta)=>CharacterCatalog.Get(meta?.selectedCharacterId);
 public static CharacterDef OfRun(RunState run)=>CharacterCatalog.Get(run?.characterId);

 public static void SyncRunCharacter(MetaSave meta,RunState run){
  if(meta==null||run==null)return;
  run.characterId=CharacterCatalog.Get(meta.selectedCharacterId).id;
 }

 /// <summary>Persistent run-start trait (max HP, etc.). Call once per new RunState.</summary>
 public static void ApplyTraitToRun(MetaSave meta,RunState run){
  if(meta==null||run==null)return;
  SyncRunCharacter(meta,run);
  var def=OfRun(run);
  switch(def.traitKind){
   case "maxHpBonus":
    run.maxHp+=def.traitValue;
    run.hp+=def.traitValue;
    break;
  }
 }

 public static void OnBattleBegin(RunState run,BattleState battle){
  if(run==null||battle==null)return;
  var def=OfRun(run);
  string extra="";
  switch(def.traitKind){
   case "openingDraw":
    if(def.traitValue>0){
     BattleSystem.Draw(run,def.traitValue);
     extra=$"{def.traitName}：{def.traitValue}枚ドロー";
    }
    break;
   case "openingBlock":
    run.block+=def.traitValue;
    extra=$"{def.traitName}：防御+{def.traitValue}";
    break;
   case "maxHpBonus":
    extra=$"{def.traitName}：HP上限+{def.traitValue}";
    break;
   case "winGold":
    extra=$"{def.traitName}：勝利時+{def.traitValue}G";
    break;
  }
  if(!string.IsNullOrEmpty(extra))BattleSystem.Record(battle,extra);
 }

 public static int WinGoldBonus(RunState run){
  var def=OfRun(run);
  return def.traitKind=="winGold"?def.traitValue:0;
 }

 public static string WinGoldBonusText(RunState run){
  int bonus=WinGoldBonus(run);
  return bonus>0?$"（{OfRun(run).traitName}+{bonus}G）":"";
 }

 public static CharacterSkillResult UseActiveSkill(RunState run,BattleState battle){
  if(run==null||battle==null||run.activeSkillUsed)
   return CharacterSkillResult.Fail;
  var def=OfRun(run);
  var fx=new BattleActionFx{ok=true,cardName=def.activeSkillName,cardType=CardType.Skill};
  string logLine;
  switch(def.activeSkillKind){
   case CharacterSkillKind.Damage:{
    int dieOne,dieTwo,modifier;
    int rolled=BattleSystem.RollDamage(def.activeSkillAmount,out dieOne,out dieTwo,out modifier);
    int raw=BattleSystem.Damage(rolled,run.statuses,battle.enemyStatuses);
    int dealt=Mathf.Max(0,raw-battle.enemyBlock);
    battle.enemyBlock=Mathf.Max(0,battle.enemyBlock-raw);
    battle.enemyHp-=dealt;
    fx.damageToEnemy=dealt;
    fx.dieOne=dieOne;fx.dieTwo=dieTwo;fx.damageModifier=modifier;fx.rolledDamage=raw;
    fx.cardType=CardType.Attack;
    logLine=$"{def.activeSkillName}：{dealt}ダメージ";
    break;
   }
   case CharacterSkillKind.BlockAndDraw:
    int block=BattleSystem.Block(def.activeSkillAmount,run.statuses);
    run.block+=block;
    BattleSystem.Draw(run,def.activeSkillSecondaryAmount);
    fx.blockGained=block;
    logLine=$"{def.activeSkillName}：{block}ブロック / {def.activeSkillSecondaryAmount}枚ドロー";
    break;
   case CharacterSkillKind.Block:
    int gained=BattleSystem.Block(def.activeSkillAmount,run.statuses);
    run.block+=gained;
    fx.blockGained=gained;
    logLine=$"{def.activeSkillName}：{gained}ブロック";
    break;
   case CharacterSkillKind.Heal:
    int before=run.hp;
    run.hp=Mathf.Min(run.maxHp,run.hp+def.activeSkillAmount);
    fx.healGained=run.hp-before;
    logLine=$"{def.activeSkillName}：HP+{fx.healGained}（{run.hp}/{run.maxHp}）";
    break;
   default:
    return CharacterSkillResult.Fail;
  }
  run.activeSkillUsed=true;
  BattleSystem.Record(battle,logLine);
  fx.enemyDefeated=battle.enemyHp<=0;
  return new CharacterSkillResult(true,fx.enemyDefeated,logLine,fx);
 }

 public static string ActiveSkillTooltip(RunState run){
  var def=OfRun(run);
  if(def==null)return "";
  if(run!=null&&run.activeSkillUsed)return $"{def.activeSkillName}（使用済み）";
  return $"{def.activeSkillName}\n{def.activeSkillText}";
 }
}
}
