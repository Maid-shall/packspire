using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Packspire {
[Serializable] public class BattleState { public EnemyDef enemy; public int enemyHp,enemyMaxHp,enemyBlock,move; public string log="戦闘開始"; }
public static class BattleSystem {
 static readonly System.Random Rng=new();
 public static BattleState Begin(RunState run,EnemyDef enemy,float hpScale=1,int bonusDamage=0){run.deck=BackpackSystem.BuildDeck(run);run.draw=Shuffle(run.deck.Select(x=>x.Clone()).ToList());run.discard.Clear();run.hand.Clear();run.energy=3;run.block=0;Draw(run,5);int hp=Mathf.RoundToInt(enemy.hp*hpScale);return new BattleState{enemy=enemy,enemyHp=hp,enemyMaxHp=hp};}
 public static bool Play(RunState run,BattleState battle,int handIndex){if(handIndex<0||handIndex>=run.hand.Count)return false;var c=run.hand[handIndex];if(c.cost>run.energy)return false;run.energy-=c.cost;int damage=Mathf.Max(0,c.damage+run.attackBuff);int dealt=Mathf.Max(0,damage-battle.enemyBlock);battle.enemyBlock=Mathf.Max(0,battle.enemyBlock-damage);battle.enemyHp-=dealt;run.attackBuff=0;run.block+=c.block;run.hp=Mathf.Min(run.maxHp,run.hp+c.heal);run.hp=Mathf.Max(1,run.hp-c.selfDamage);run.attackBuff+=c.buff;run.energy+=c.energy;var item=run.inventory.FirstOrDefault(x=>x.uid==c.sourceItemUid);if(item!=null&&!c.durabilityFree){item.uses++;item.durability=Mathf.Max(0,item.durability-1);}run.hand.RemoveAt(handIndex);if(!c.exhaust){if(c.recycle)run.draw.Add(c);else run.discard.Add(c);}if(c.draw>0)Draw(run,c.draw);battle.log=$"{c.name}：{dealt}ダメージ / {c.block}防御";return battle.enemyHp<=0;}
 public static bool EndTurn(RunState run,BattleState battle,int dungeonDamage=0){run.discard.AddRange(run.hand);run.hand.Clear();int raw=battle.enemy.damages[battle.move%battle.enemy.damages.Length]+dungeonDamage,damage=Mathf.Max(0,raw-run.block);run.hp-=damage;run.block=0;battle.move++;run.energy=3;Draw(run,5);battle.log=$"{battle.enemy.name}の攻撃：{damage}ダメージ";return run.hp<=0;}
 public static void Draw(RunState run,int n){while(n-->0){if(run.draw.Count==0){run.draw=Shuffle(run.discard);run.discard=new();}if(run.draw.Count==0)return;var c=run.draw[^1];run.draw.RemoveAt(run.draw.Count-1);run.hand.Add(c);}}
 static List<T> Shuffle<T>(List<T> list)=>list.OrderBy(_=>Rng.Next()).ToList();
}
}
