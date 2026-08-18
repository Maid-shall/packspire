using System.Collections.Generic;
using System.Linq;

namespace Packspire {
public static class LoadoutSystem {
 public static LoadoutSave Active(MetaSave meta){
  meta.loadouts??=new();
  var active=meta.loadouts.FirstOrDefault(x=>x.id==meta.selectedLoadoutId)??meta.loadouts.FirstOrDefault();
  if(active==null){active=new LoadoutSave{id="loadout-1",name="編成 1",backpack="standard"};meta.loadouts.Add(active);}
  EnsureFormulaIds(active);
  meta.selectedLoadoutId=active.id;return active;
 }
 public static void Select(MetaSave meta,string id){if(meta.loadouts.Any(x=>x.id==id))meta.selectedLoadoutId=id;}
 public static RunState CreateRun(MetaSave meta,string dungeon){
  var loadout=Active(meta);
  var run=new RunState{role=meta.currentRole,faction=meta.currentFaction,backpack=loadout.backpack,loadoutId=loadout.id,dungeon=dungeon,hp=42,maxHp=42,gold=0,heirloomUid=meta.selectedHeirloomUid};
  StorageFormulaSystem.CopyFormulaToRun(loadout,run);
  run.inventory=meta.stash.Select(CloneItem).ToList();
  foreach(var item in run.inventory)StorageFormulaSystem.EnsureItemRolled(item);
  var ids=run.inventory.Select(x=>x.uid).ToHashSet();
  run.placements=loadout.slots.Where(x=>ids.Contains(x.itemUid)).Select(x=>new Placement(x.itemUid,x.anchor,x.rotation)).ToList();
  run.selectedCardSlots=loadout.deck.ToList();
  run.consumables=meta.consumables.Take(meta.consumableCapacity).ToList();
  CharacterSystem.ApplyTraitToRun(meta,run);
  return run;
 }
 public static void Capture(MetaSave meta,RunState run){
  var loadout=meta.loadouts.First(x=>x.id==run.loadoutId);
  StorageFormulaSystem.ApplyToLoadout(loadout,run);
  loadout.slots=run.placements.Select(x=>new Placement(x.itemUid,x.anchor,x.rotation)).ToList();
  loadout.deck=run.selectedCardSlots.ToList();
  loadout.deckInitialized=true;
 }
 public static ItemInstance CloneItem(ItemInstance item){
  var clone=UnityEngine.JsonUtility.FromJson<ItemInstance>(UnityEngine.JsonUtility.ToJson(item));
  StorageFormulaSystem.EnsureItemRolled(clone);
  return clone;
 }
 public static void EnsureFormulaIds(LoadoutSave loadout){
  if(loadout==null)return;
  if(string.IsNullOrEmpty(loadout.coreId))loadout.coreId=StorageFormulaCatalog.CoreIdFromBackpack(loadout.backpack);
  if(string.IsNullOrEmpty(loadout.conduitId))loadout.conduitId=StorageFormulaCatalog.DefaultConduitId;
  if(string.IsNullOrEmpty(loadout.resonanceId))loadout.resonanceId=StorageFormulaCatalog.DefaultResonanceId;
  if(string.IsNullOrEmpty(loadout.stabilityId))loadout.stabilityId=StorageFormulaCatalog.DefaultStabilityId;
 }
}
}
