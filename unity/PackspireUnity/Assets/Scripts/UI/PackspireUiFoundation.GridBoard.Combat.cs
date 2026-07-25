using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire {
public sealed partial class PackspireUiFoundation {
// Enemy presentation and combat intent calculation.
 void RefreshGridCombatStage(){
  var battle=game.UiBattle;
  if(battle?.enemy==null)return;
  if(gridBoardCombatTitle!=null)gridBoardCombatTitle.text=battle.enemy.name;
  if(gridBoardCombatHpLabel!=null)
   gridBoardCombatHpLabel.text=$"HP {Mathf.Max(0,battle.enemyHp)} / {battle.enemyMaxHp}";
  if(gridBoardCombatHpFill!=null){
   float ratio=battle.enemyMaxHp>0?Mathf.Clamp01((float)battle.enemyHp/battle.enemyMaxHp):0f;
   gridBoardCombatHpFill.style.width=Length.Percent(ratio*100f);
  }
  if(gridBoardCombatShieldLabel!=null)
   gridBoardCombatShieldLabel.text=$"SH {Mathf.Max(0,battle.enemyBlock)}";
  if(gridBoardCombatShieldFill!=null){
   float shieldRatio=battle.enemyMaxHp>0?Mathf.Clamp01((float)Mathf.Max(0,battle.enemyBlock)/battle.enemyMaxHp):0f;
   gridBoardCombatShieldFill.style.width=Length.Percent(shieldRatio*100f);
  }
  RefreshGridCombatEnemyStatuses(battle.enemyStatuses);
  if(gridBoardCombatIntentLabel!=null)
   gridBoardCombatIntentLabel.text=DescribeGridBattleIntentShort();
  if(gridBoardCombatIntentHintLabel!=null)
   gridBoardCombatIntentHintLabel.text=DescribeGridBattleIntentHint();
  if(gridBoardCombatPortrait!=null){
   if(PackspireGame.LockBattleShowcaseArt&&game.UiShowcaseDragonArt!=null){
    gridBoardCombatPortrait.image=game.UiShowcaseDragonArt;
    gridBoardCombatPortrait.uv=new Rect(0,0,1,1);
   } else if(battle.enemy.HasPortraitAsset){
    gridBoardCombatPortrait.image=game.ResolveEnemyPortrait(battle.enemy);
    gridBoardCombatPortrait.uv=new Rect(0,0,1,1);
   } else {
    gridBoardCombatPortrait.image=game.UiEnemyArt;
    gridBoardCombatPortrait.uv=EnemyUv(battle.enemy.id);
   }
   gridBoardCombatPortrait.style.display=DisplayStyle.Flex;
  }
 }

 void RefreshGridCombatEnemyStatuses(List<StatusState> statuses){
  if(gridBoardCombatEnemyStatuses==null)return;
  gridBoardCombatEnemyStatuses.Clear();
  if(statuses==null||statuses.Count==0)return;
  foreach(var status in statuses.Take(3)){
   var def=ContentDatabase.Status(status.type);
   var chip=new Label($"{(def!=null?def.name:status.type)} {status.amount}"){pickingMode=PickingMode.Ignore};
   chip.AddToClassList("ps-gboard-combat-status-chip");
   gridBoardCombatEnemyStatuses.Add(chip);
  }
 }

 string DescribeGridBattleIntentShort(){
  if(!TryGetGridBattleIntent(out int raw,out bool special,out int unusedBlock,out List<EffectSpec> unusedEffects))return "次の行動　—";
  string extra="";
  if(unusedEffects!=null&&unusedEffects.Count>0){
   var effect=unusedEffects[0];
   var def=ContentDatabase.Status(effect.type);
   extra=$"\n{(def!=null?def.name:effect.type)} +{effect.amount}";
  }
  if(raw>0){
   int modifier=raw-7;
   return $"⚔ 攻撃\n2D6 {GridDiceModifierText(modifier)}　平均 {raw}{extra}";
  }
  return special?$"次の行動\n✦ 特殊行動{extra}":"次の行動\n—";
 }

 string DescribeGridBattleIntentHint(){
  if(!TryGetGridBattleIntent(out int raw,out bool unusedSpecial,out int block,out var effects))
   return "カードを選んで敵を攻める。";
  var bits=new List<string>();
  if(raw>0){
   int after=Mathf.Max(0,raw-Mathf.Max(0,block));
   bits.Add(block>0?$"次の攻撃 被ダメ {after}":$"次の攻撃 被ダメ {raw}");
  }
  if(effects!=null){
   foreach(var effect in effects.Take(2)){
    var def=ContentDatabase.Status(effect.type);
    bits.Add($"+{(def!=null?def.name:effect.type)}{effect.amount}");
   }
  }
  if(bits.Count==0)bits.Add("特殊行動の予兆");
  return string.Join("　·　",bits);
 }

 bool TryGetGridBattleIntent(out int rawDamage,out bool specialMove,out int playerBlock,out List<EffectSpec> effects){
  rawDamage=0;specialMove=false;playerBlock=0;effects=null;
  var run=game.UiRun;
  var battle=game.UiBattle;
  if(run==null||battle?.enemy==null||battle.enemy.damages==null||battle.enemy.damages.Length==0)return false;
  var dungeon=GameCatalog.Dungeons.First(x=>x.id==run.dungeon);
  int moveIndex=battle.move%battle.enemy.damages.Length;
  int baseDamage=battle.enemy.damages[moveIndex];
  int pressure=GridBoardSystem.EnemyDamageBonus(game.UiGridBoard);
  rawDamage=BattleSystem.Damage(baseDamage+dungeon.damage+pressure,battle.enemyStatuses,run.statuses);
  specialMove=baseDamage==0&&dungeon.damage+pressure==0;
  playerBlock=run.block;
  effects=ContentDatabase.EnemyEffects(battle.enemy.id,moveIndex);
  return true;
 }

}
}
