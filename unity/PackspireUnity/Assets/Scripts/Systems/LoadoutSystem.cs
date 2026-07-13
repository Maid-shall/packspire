using System.Collections.Generic;
using System.Linq;

namespace Packspire {
public static class LoadoutSystem {
 public const int EquipmentCardLimit=8;
 public static LoadoutSave Active(MetaSave meta)=>meta.loadouts.First(x=>x.id==meta.selectedLoadoutId);
 public static void Select(MetaSave meta,string id){if(meta.loadouts.Any(x=>x.id==id))meta.selectedLoadoutId=id;}
 public static RunState CreateRun(MetaSave meta,string dungeon){
  var loadout=Active(meta);var run=new RunState{role=meta.currentRole,faction=meta.currentFaction,backpack=loadout.backpack,loadoutId=loadout.id,dungeon=dungeon,hp=42,maxHp=42,gold=0,heirloomUid=meta.selectedHeirloomUid};
  run.inventory=meta.stash.Select(CloneItem).ToList();
  var ids=run.inventory.Select(x=>x.uid).ToHashSet();
  run.placements=loadout.slots.Where(x=>ids.Contains(x.itemUid)).Select(x=>new Placement(x.itemUid,x.anchor,x.rotation)).ToList();
  run.selectedCardSlots=loadout.deck.Take(EquipmentCardLimit).ToList();
  run.consumables=meta.consumables.Take(meta.consumableCapacity).ToList();
  return run;
 }
 public static void Capture(MetaSave meta,RunState run){
  var loadout=meta.loadouts.First(x=>x.id==run.loadoutId);loadout.backpack=run.backpack;
  loadout.slots=run.placements.Select(x=>new Placement(x.itemUid,x.anchor,x.rotation)).ToList();
  loadout.deck=run.selectedCardSlots.Take(EquipmentCardLimit).ToList();loadout.deckInitialized=true;
 }
 public static ItemInstance CloneItem(ItemInstance item)=>UnityEngine.JsonUtility.FromJson<ItemInstance>(UnityEngine.JsonUtility.ToJson(item));
}
}
