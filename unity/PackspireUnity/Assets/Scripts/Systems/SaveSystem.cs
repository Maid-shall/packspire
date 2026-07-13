using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Packspire {
public static class SaveSystem {
 const string Key="packspire_unity_save_v15";
 const string LegacyKey="packspire_unity_save_v1";
 public static MetaSave Load(){try{var json=PlayerPrefs.GetString(Key,PlayerPrefs.GetString(LegacyKey,""));return Migrate(string.IsNullOrEmpty(json)?new MetaSave():JsonUtility.FromJson<MetaSave>(json)??new MetaSave());}catch{return Migrate(new MetaSave());}}
 public static void Save(MetaSave data){PlayerPrefs.SetString(Key,JsonUtility.ToJson(data));PlayerPrefs.Save();}
 public static void Reset(){PlayerPrefs.DeleteKey(Key);PlayerPrefs.DeleteKey(LegacyKey);PlayerPrefs.Save();}
 public static string Export(MetaSave data)=>JsonUtility.ToJson(data,true);
 public static MetaSave Import(string json)=>Migrate(JsonUtility.FromJson<MetaSave>(json)??new MetaSave());
 static MetaSave Migrate(MetaSave save){
  save.version=15;save.stash??=new();save.consumables??=new();save.dungeonDiscoveries??=new();save.loadouts??=new();
  if(save.consumableCapacity<5)save.consumableCapacity=5;
  if(save.consumables.Count==0)save.consumables.AddRange(new[]{"heal","heal","guard","fire","energy"});
  var ids=new[]{"loadout-1","loadout-2","loadout-3"};
  for(int i=0;i<ids.Length;i++){var loadout=save.loadouts.FirstOrDefault(x=>x.id==ids[i]);if(loadout==null){loadout=new LoadoutSave{id=ids[i],name=$"編成 {i+1}",backpack=i==0&& !string.IsNullOrEmpty(save.selectedBackpack)?save.selectedBackpack:"standard"};save.loadouts.Add(loadout);}loadout.slots??=new();loadout.deck??=new();if(loadout.deck.Count>8)loadout.deck=loadout.deck.Take(8).ToList();}
  save.loadouts=ids.Select(id=>save.loadouts.First(x=>x.id==id)).ToList();
  if(!ids.Contains(save.selectedLoadoutId))save.selectedLoadoutId=ids[0];
  foreach(var item in save.stash){item.scars??=new();item.colors??=new();item.history??=new();}
  return save;
 }
}
}
