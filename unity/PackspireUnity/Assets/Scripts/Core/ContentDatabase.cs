using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Packspire {
[Serializable] public class StatusDefinition { public string id,name,icon,description,kind; public bool stack; }
[Serializable] public class CardEffectEntry { public string cardId; public List<EffectSpec> effects=new(); }
[Serializable] public class EnemyMoveEffectEntry { public string enemyName; public int moveIndex; public List<EffectSpec> effects=new(); }
[Serializable] public class SharedContent { public int schemaVersion=1; public List<StatusDefinition> statuses=new(); public List<CardEffectEntry> cardEffects=new(); public List<EnemyMoveEffectEntry> enemyMoveEffects=new(); }

public static class ContentDatabase {
 static SharedContent data;
 public static SharedContent Data=>data??=Load();
 static SharedContent Load(){var asset=Resources.Load<TextAsset>("Packspire/content");if(asset==null){Debug.LogWarning("Packspire shared content was not found; using empty content.");return new SharedContent();}try{return JsonUtility.FromJson<SharedContent>(asset.text)??new SharedContent();}catch(Exception error){Debug.LogError($"Packspire content load failed: {error.Message}");return new SharedContent();}}
 public static List<EffectSpec> CardEffects(string cardId)=>Data.cardEffects.FirstOrDefault(x=>x.cardId==cardId)?.effects.ConvertAll(Clone)??new();
 public static List<EffectSpec> EnemyEffects(string enemyName,int moveIndex)=>Data.enemyMoveEffects.FirstOrDefault(x=>x.enemyName==enemyName&&x.moveIndex==moveIndex)?.effects.ConvertAll(Clone)??new();
 public static StatusDefinition Status(string id)=>Data.statuses.FirstOrDefault(x=>x.id==id);
 static EffectSpec Clone(EffectSpec value)=>new(){type=value.type,target=value.target,amount=value.amount,duration=value.duration};
}
}
