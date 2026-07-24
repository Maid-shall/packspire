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
  battle.log=string.IsNullOrEmpty(extra)?"戦闘開始":$"戦闘開始　／　{extra}";
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
  switch(def.activeSkillId){
   case "ren_rush":{
    int raw=BattleSystem.Damage(10,run.statuses,battle.enemyStatuses);
    int dealt=Mathf.Max(0,raw-battle.enemyBlock);
    battle.enemyBlock=Mathf.Max(0,battle.enemyBlock-raw);
    battle.enemyHp-=dealt;
    fx.damageToEnemy=dealt;
    fx.cardType=CardType.Attack;
    logLine=$"{def.activeSkillName}：{dealt}ダメージ";
    break;
   }
   case "mio_read":
    run.block+=BattleSystem.Block(8,run.statuses);
    BattleSystem.Draw(run,1);
    fx.blockGained=8;
    logLine=$"{def.activeSkillName}：8ブロック / 1枚ドロー";
    break;
   case "kuro_bulwark":
    run.block+=BattleSystem.Block(14,run.statuses);
    fx.blockGained=14;
    logLine=$"{def.activeSkillName}：14ブロック";
    break;
   case "hina_repair":
    int before=run.hp;
    run.hp=Mathf.Min(run.maxHp,run.hp+10);
    fx.healGained=run.hp-before;
    logLine=$"{def.activeSkillName}：HP+10（{run.hp}/{run.maxHp}）";
    break;
   case "sena_kick":{
    int raw=BattleSystem.Damage(14,run.statuses,battle.enemyStatuses);
    int dealt=Mathf.Max(0,raw-battle.enemyBlock);
    battle.enemyBlock=Mathf.Max(0,battle.enemyBlock-raw);
    battle.enemyHp-=dealt;
    fx.damageToEnemy=dealt;
    fx.cardType=CardType.Attack;
    logLine=$"{def.activeSkillName}：{dealt}ダメージ";
    break;
   }
   default:
    return CharacterSkillResult.Fail;
  }
  run.activeSkillUsed=true;
  battle.log=logLine;
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
