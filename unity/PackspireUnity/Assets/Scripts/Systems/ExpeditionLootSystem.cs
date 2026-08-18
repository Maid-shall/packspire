using System;
using System.Collections.Generic;
using System.Linq;

namespace Packspire {
public enum ExpeditionEndReason { Defeat, Return, Clear }

public sealed class ExpeditionFinalizationSummary {
 public ExpeditionEndReason reason;
 public int retainedNewItemCount,lostNewItemCount,retainedGold;
 public readonly List<string> retainedNewItemUids=new();
 public readonly List<string> lostNewItemUids=new();
 public bool IsSuccessful=>reason!=ExpeditionEndReason.Defeat;
}

/// <summary>
/// Owns the boundary between transient expedition loot and the permanent vault.
/// Starting items always remain owned. New items survive defeat only after they
/// have actually been placed in the backpack; return and clear secure everything.
/// </summary>
public static class ExpeditionLootSystem {
 public static ExpeditionFinalizationSummary Finalize(
  MetaSave meta,RunState run,ExpeditionEndReason reason,long defeatTimestamp=0){
  if(meta==null)throw new ArgumentNullException(nameof(meta));
  var summary=new ExpeditionFinalizationSummary{reason=reason};
  meta.stash??=new List<ItemInstance>();

  if(run==null){
   meta.runs++;
   if(reason==ExpeditionEndReason.Clear)meta.wins++;
   return summary;
  }

  EnsureStartingItems(run,meta);
  if(reason==ExpeditionEndReason.Defeat)
   RecordHeirloomDefeat(run,defeatTimestamp);

  var starting=new HashSet<string>(run.startingItemUids??new List<string>());
  var packed=new HashSet<string>((run.placements??new List<Placement>())
   .Where(placement=>placement!=null&&!string.IsNullOrEmpty(placement.itemUid))
   .Select(placement=>placement.itemUid));

  foreach(var item in DistinctItems(run)){
   bool isNew=!starting.Contains(item.uid);
   bool retain=reason!=ExpeditionEndReason.Defeat||!isNew||packed.Contains(item.uid);
   if(retain){
    Upsert(meta,item);
    if(isNew){
     summary.retainedNewItemCount++;
     summary.retainedNewItemUids.Add(item.uid);
    }
   } else if(isNew){
    summary.lostNewItemCount++;
    summary.lostNewItemUids.Add(item.uid);
   }
  }

  if(reason!=ExpeditionEndReason.Defeat){
   summary.retainedGold=Math.Max(0,run.gold);
   meta.baseGold+=summary.retainedGold;
  }
  if(reason==ExpeditionEndReason.Clear)meta.wins++;
  meta.runs++;
  return summary;
 }

 public static bool IsProtectedOnDefeat(RunState run,ItemInstance item){
  if(run==null||item==null||string.IsNullOrEmpty(item.uid))return false;
  if(run.startingItemUids?.Contains(item.uid)==true)return true;
  return run.placements?.Any(placement=>placement?.itemUid==item.uid)==true;
 }

 static void EnsureStartingItems(RunState run,MetaSave meta){
  run.startingItemUids??=new List<string>();
  if(run.startingItemUids.Count>0)return;
  var current=DistinctItems(run).Select(item=>item.uid).ToHashSet();
  run.startingItemUids=meta.stash
   .Where(item=>item!=null&&!string.IsNullOrEmpty(item.uid)&&current.Contains(item.uid))
   .Select(item=>item.uid)
   .Distinct()
   .ToList();
 }

 static IEnumerable<ItemInstance> DistinctItems(RunState run){
  var seen=new HashSet<string>();
  foreach(var item in (run.inventory??new List<ItemInstance>())
   .Concat(run.lootBag??new List<ItemInstance>())){
   if(item==null||string.IsNullOrEmpty(item.uid)||!seen.Add(item.uid))continue;
   yield return item;
  }
 }

 static void RecordHeirloomDefeat(RunState run,long timestamp){
  if(string.IsNullOrEmpty(run.heirloomUid))return;
  var heir=DistinctItems(run).FirstOrDefault(item=>item.uid==run.heirloomUid);
  if(heir==null)return;
  heir.history??=new HeirloomHistory();
  heir.scars??=new List<ScarRecord>();
  heir.history.defeats++;
  heir.scars.Add(new ScarRecord{
   type="defeat",dungeon=run.dungeon,floor=run.battlesWon,
   timestamp=timestamp>0?timestamp:DateTimeOffset.UtcNow.ToUnixTimeSeconds()
  });
 }

 static void Upsert(MetaSave meta,ItemInstance item){
  int index=meta.stash.FindIndex(saved=>saved?.uid==item.uid);
  var clone=LoadoutSystem.CloneItem(item);
  if(index<0)meta.stash.Add(clone);
  else meta.stash[index]=clone;
 }
}
}
