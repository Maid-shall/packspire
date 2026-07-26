using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Packspire {
[Serializable]
public class StatusDefinition {
 public string id,name,icon,description,kind;
 public bool stack;
 public Sprite artwork;
}

/// <summary>Runtime status/effect view backed by the master ScriptableObject.</summary>
public static class ContentDatabase {
 static Dictionary<string,StatusDefinition> statuses;

 static Dictionary<string,StatusDefinition> Statuses=>statuses??=PackspireContent.Data.statuses
  .Select(x=>new StatusDefinition{
   id=x.id,name=x.name,icon=x.icon,description=x.description,kind=x.kind,stack=x.stack,artwork=x.artwork
  })
  .ToDictionary(x=>x.id);

 public static List<EffectSpec> CardEffects(string cardId){
  var card=PackspireContent.Data.cards.FirstOrDefault(x=>x.id==cardId);
  return Convert(card?.effects);
 }

 public static List<EffectSpec> EnemyEffects(string enemyId,int moveIndex){
  return Convert(EnemyMove(enemyId,moveIndex)?.effects);
 }

 public static EnemyMoveContent EnemyMove(string enemyId,int moveIndex){
  var enemy=PackspireContent.Data.enemies.FirstOrDefault(x=>x.id==enemyId);
  if(enemy?.moves==null||enemy.moves.Length==0)return null;
  int index=((moveIndex%enemy.moves.Length)+enemy.moves.Length)%enemy.moves.Length;
  return enemy.moves[index];
 }

 public static EnemyPhaseContent EnemyPhase(string enemyId,int hp,int maxHp){
  var enemy=PackspireContent.Data.enemies.FirstOrDefault(x=>x.id==enemyId);
  if(enemy?.phases==null||enemy.phases.Length==0)return null;
  int percent=maxHp>0?Mathf.CeilToInt(Mathf.Max(0,hp)*100f/maxHp):0;
  return enemy.phases
   .Where(x=>x!=null&&x.minimumHpPercent<=percent)
   .OrderByDescending(x=>x.minimumHpPercent)
   .FirstOrDefault()
   ??enemy.phases.Where(x=>x!=null).OrderBy(x=>x.minimumHpPercent).FirstOrDefault();
 }

 public static StatusDefinition Status(string id)=>
  !string.IsNullOrEmpty(id)&&Statuses.TryGetValue(id,out var value)?value:null;

 static List<EffectSpec> Convert(IEnumerable<EffectContent> values)=>
  (values??Enumerable.Empty<EffectContent>()).Select(x=>new EffectSpec{
   type=x.statusId,
   target=x.target switch{
    EffectTarget.Self=>"self",
    EffectTarget.Player=>"player",
    _=>"enemy"
   },
   amount=x.amount,
   duration=x.duration
  }).ToList();
}
}
