using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Packspire {
public static class SaveSystem {
 public const int CurrentVersion=19;
 const string Key="packspire_unity_save_v19";
 const string StagingKey=Key+"_staging";
 const string BackupKey=Key+"_backup";
 const string LegacyKey="packspire_unity_save_v18";
 const string LegacyKeyV17="packspire_unity_save_v17";
 const string LegacyKeyV16="packspire_unity_save_v16";
 const string LegacyKeyV1="packspire_unity_save_v1";

 public static MetaSave Load(){
  foreach(var key in new[]{StagingKey,Key,BackupKey,LegacyKey,LegacyKeyV17,LegacyKeyV16,LegacyKeyV1}){
   if(!PlayerPrefs.HasKey(key))continue;
   if(TryRead(PlayerPrefs.GetString(key,""),out var save)){
    if(key!=Key)Debug.LogWarning($"PACKSPIRE save recovered from '{key}'.");
    return Migrate(save);
   }
  }
  return Migrate(new MetaSave());
 }

 public static void Save(MetaSave data){
  var normalized=Migrate(data??new MetaSave());
  string json=JsonUtility.ToJson(normalized);
  if(PlayerPrefs.HasKey(Key)){
   string previous=PlayerPrefs.GetString(Key,"");
   if(!string.IsNullOrEmpty(previous))PlayerPrefs.SetString(BackupKey,previous);
  }
  PlayerPrefs.SetString(StagingKey,json);
  PlayerPrefs.Save();
  PlayerPrefs.SetString(Key,json);
  PlayerPrefs.DeleteKey(StagingKey);
  PlayerPrefs.Save();
 }

 public static void Reset(){
  foreach(var key in new[]{Key,StagingKey,BackupKey,LegacyKey,LegacyKeyV17,LegacyKeyV16,LegacyKeyV1})
   PlayerPrefs.DeleteKey(key);
  PlayerPrefs.Save();
 }

 public static string Export(MetaSave data)=>JsonUtility.ToJson(Migrate(data??new MetaSave()),true);

 public static MetaSave Import(string json){
  if(!TryRead(json,out var save))throw new FormatException("Save JSON could not be parsed.");
  return Migrate(save);
 }

 static bool TryRead(string json,out MetaSave save){
  save=null;
  if(string.IsNullOrWhiteSpace(json))return false;
  try{
   save=JsonUtility.FromJson<MetaSave>(json);
   return save!=null;
  }catch(Exception ex){
   Debug.LogWarning($"PACKSPIRE ignored malformed save data: {ex.Message}");
   return false;
  }
 }

 static MetaSave Migrate(MetaSave save){
  save??=new MetaSave();
  int sourceVersion=Mathf.Max(1,save.version);
  if(sourceVersion<16)MigrateTo16(save);
  if(sourceVersion<17)MigrateTo17(save);
  if(sourceVersion<18)MigrateTo18(save);
  if(sourceVersion<19)MigrateTo19(save);
  NormalizeCurrent(save);
  save.version=CurrentVersion;
  return save;
 }

 static void MigrateTo16(MetaSave save){
  save.loadouts??=new List<LoadoutSave>();
  save.selectedLoadoutId=string.IsNullOrEmpty(save.selectedLoadoutId)?"loadout-1":save.selectedLoadoutId;
 }

 static void MigrateTo17(MetaSave save){
  save.selectedCharacterId=string.IsNullOrEmpty(save.selectedCharacterId)
   ?PackspireContent.Data.balance.defaultCharacterId
   :save.selectedCharacterId;
 }

 static void MigrateTo18(MetaSave save){
  save.memoryReactions??=new List<ReactionValueState>();
 }

 static void MigrateTo19(MetaSave save){
  save.roleBranches??=new List<IdInt>();
  RoleFrameworkSystem.MigrateLegacyRole(save);
 }

 static void NormalizeCurrent(MetaSave save){
  save.stash??=new List<ItemInstance>();
  save.consumables??=new List<string>();
  save.dungeonDiscoveries??=new List<string>();
  save.loadouts??=new List<LoadoutSave>();
  save.unlockedRoles??=new List<string>();
  save.discoveredItems??=new List<string>();
  save.discoveredEnemies??=new List<string>();
  save.unlockedSecrets??=new List<string>();
  save.jobLevels??=new List<IdInt>();
  save.factionRep??=new List<IdFloat>();
  save.memoryReactions??=new List<ReactionValueState>();
  save.roleBranches??=new List<IdInt>();
  RoleFrameworkSystem.Normalize(save);

  string defaultCharacter=PackspireContent.Data.balance.defaultCharacterId;
  if(string.IsNullOrEmpty(save.selectedCharacterId)||!CharacterCatalog.All.ContainsKey(save.selectedCharacterId))
   save.selectedCharacterId=defaultCharacter;
  if(save.dungeonsUnlocked<1)save.dungeonsUnlocked=1;
  if(save.consumableCapacity<5)save.consumableCapacity=5;
  if(save.consumables.Count==0)save.consumables.AddRange(new[]{"heal","heal","guard","fire","energy"});

  var ids=new[]{"loadout-1","loadout-2","loadout-3"};
  for(int i=0;i<ids.Length;i++){
   var loadout=save.loadouts.FirstOrDefault(x=>x!=null&&x.id==ids[i]);
   if(loadout==null){
    loadout=new LoadoutSave{
     id=ids[i],
     name=$"編成{i+1}",
     backpack=i==0&&!string.IsNullOrEmpty(save.selectedBackpack)
      ?save.selectedBackpack
      :PackspireContent.Data.balance.defaultBackpackId
    };
    save.loadouts.Add(loadout);
   }
   loadout.slots??=new List<Placement>();
   loadout.deck??=new List<string>();
   LoadoutSystem.EnsureFormulaIds(loadout);
  }
  save.loadouts=ids.Select(id=>save.loadouts.First(x=>x!=null&&x.id==id)).ToList();
  if(!ids.Contains(save.selectedLoadoutId))save.selectedLoadoutId=ids[0];

  foreach(var item in save.stash.Where(x=>x!=null)){
   item.scars??=new List<ScarRecord>();
   item.colors??=new List<Element>();
   item.history??=new HeirloomHistory();
   item.history.dungeons??=new List<IdInt>();
   StorageFormulaSystem.EnsureItemRolled(item);
  }
 }
}
}
