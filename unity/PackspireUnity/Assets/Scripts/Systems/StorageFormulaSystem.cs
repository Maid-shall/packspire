using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Packspire {
/// <summary>Resolves and applies the four-part storage formula (core / conduit / resonance / stability).</summary>
public static class StorageFormulaSystem {
 public static ActiveStorageFormula Resolve(RunState run){
  string coreId=FirstNonEmpty(run?.coreId,StorageFormulaCatalog.CoreIdFromBackpack(run?.backpack));
  string conduitId=FirstNonEmpty(run?.conduitId,StorageFormulaCatalog.DefaultConduitId);
  string resonanceId=FirstNonEmpty(run?.resonanceId,StorageFormulaCatalog.DefaultResonanceId);
  string stabilityId=FirstNonEmpty(run?.stabilityId,StorageFormulaCatalog.DefaultStabilityId);
  return new ActiveStorageFormula(
   StorageFormulaCatalog.Core(coreId),
   StorageFormulaCatalog.Conduit(conduitId),
   StorageFormulaCatalog.Resonance(resonanceId),
   StorageFormulaCatalog.Stability(stabilityId));
 }

 public static ActiveStorageFormula Resolve(LoadoutSave loadout){
  string coreId=FirstNonEmpty(loadout?.coreId,StorageFormulaCatalog.CoreIdFromBackpack(loadout?.backpack));
  return new ActiveStorageFormula(
   StorageFormulaCatalog.Core(coreId),
   StorageFormulaCatalog.Conduit(FirstNonEmpty(loadout?.conduitId,StorageFormulaCatalog.DefaultConduitId)),
   StorageFormulaCatalog.Resonance(FirstNonEmpty(loadout?.resonanceId,StorageFormulaCatalog.DefaultResonanceId)),
   StorageFormulaCatalog.Stability(FirstNonEmpty(loadout?.stabilityId,StorageFormulaCatalog.DefaultStabilityId)));
 }

 public static void SyncCoreFromBackpack(RunState run){
  if(run==null)return;
  if(string.IsNullOrEmpty(run.coreId))run.coreId=StorageFormulaCatalog.CoreIdFromBackpack(run.backpack);
  else run.backpack=run.coreId;
  if(string.IsNullOrEmpty(run.conduitId))run.conduitId=StorageFormulaCatalog.DefaultConduitId;
  if(string.IsNullOrEmpty(run.resonanceId))run.resonanceId=StorageFormulaCatalog.DefaultResonanceId;
  if(string.IsNullOrEmpty(run.stabilityId))run.stabilityId=StorageFormulaCatalog.DefaultStabilityId;
 }

 public static void ApplyToLoadout(LoadoutSave loadout,RunState run){
  if(loadout==null||run==null)return;
  loadout.backpack=run.backpack;
  loadout.coreId=FirstNonEmpty(run.coreId,StorageFormulaCatalog.CoreIdFromBackpack(run.backpack));
  loadout.conduitId=FirstNonEmpty(run.conduitId,StorageFormulaCatalog.DefaultConduitId);
  loadout.resonanceId=FirstNonEmpty(run.resonanceId,StorageFormulaCatalog.DefaultResonanceId);
  loadout.stabilityId=FirstNonEmpty(run.stabilityId,StorageFormulaCatalog.DefaultStabilityId);
 }

 public static void CopyFormulaToRun(LoadoutSave loadout,RunState run){
  if(loadout==null||run==null)return;
  run.backpack=loadout.backpack;
  run.coreId=FirstNonEmpty(loadout.coreId,StorageFormulaCatalog.CoreIdFromBackpack(loadout.backpack));
  run.conduitId=FirstNonEmpty(loadout.conduitId,StorageFormulaCatalog.DefaultConduitId);
  run.resonanceId=FirstNonEmpty(loadout.resonanceId,StorageFormulaCatalog.DefaultResonanceId);
  run.stabilityId=FirstNonEmpty(loadout.stabilityId,StorageFormulaCatalog.DefaultStabilityId);
 }

 public static bool IsRotationAllowed(RotationCapability capability,int rotation){
  int value=((rotation%4)+4)%4;
  return capability switch{
   RotationCapability.FlipOnly=>value==0||value==2,
   RotationCapability.QuarterTurn=>true,
   RotationCapability.FullTurn=>true,
   _=>true,
  };
 }

 public static int ClampRotation(RotationCapability capability,int rotation){
  int value=((rotation%4)+4)%4;
  if(IsRotationAllowed(capability,value))return value;
  return capability==RotationCapability.FlipOnly?(value>=2?2:0):0;
 }

 public static int NextRotation(RotationCapability capability,int rotation){
  int value=ClampRotation(capability,rotation);
  for(int step=0;step<4;step++){
   value=(value+1)%4;
   if(IsRotationAllowed(capability,value))return value;
  }
  return ClampRotation(capability,rotation);
 }

 public static Element BoardAt(StorageCoreDef core,int index){
  if(core?.board==null||index<0||index>=core.board.Length)return Element.Fire;
  return core.board[index];
 }

 public static StabilityEval EvaluateStability(RunState run,ActiveStorageFormula formula){
  var eval=new StabilityEval{
   durabilityDrainScale=formula.stability.durabilityDrainScale,
   cardPenalty=1f,
  };
  int placed=run?.placements?.Count??0;
  int damaged=run?.inventory?.Count(x=>x.durability<=2)??0;
  eval.strain=placed+damaged*2;
  eval.runaway=eval.strain>=formula.stability.runawayThreshold;
  if(eval.runaway)eval.cardPenalty=Mathf.Clamp(formula.stability.runawayCardPenalty,.2f,1f);
  return eval;
 }

 public static void ApplyConduitBonuses(AttributeConduitDef conduit,Dictionary<Element,int> colors,List<CardInstance> cards){
  if(conduit?.bonuses==null||cards==null)return;
  foreach(var card in cards){
   foreach(var rule in conduit.bonuses){
    int matches=0;
    colors.TryGetValue(rule.element,out matches);
    if(matches<rule.threshold)continue;
    int scaled=rule.useHalfWaterHeal?(matches+1)/2:matches/Mathf.Max(1,rule.matchDivisor)*Mathf.Max(1,rule.amountPerMatch);
    switch(rule.target){
     case ConduitBonusTarget.Damage:
      if(card.damage>0)card.damage+=scaled;
      break;
     case ConduitBonusTarget.Block:
      if(card.block>0)card.block+=scaled;
      break;
     case ConduitBonusTarget.Heal:
      if(card.heal>0)card.heal+=scaled;
      break;
     case ConduitBonusTarget.CostReduce:
      card.cost=Mathf.Max(0,card.cost-Mathf.Max(1,rule.amountPerMatch));
      break;
    }
   }
  }
 }

 public static void ApplyColorTraits(RunState run,Dictionary<Element,int> colors,List<CardInstance> cards){
  if(run==null||cards==null)return;
  foreach(var card in cards){
   if(string.IsNullOrEmpty(card.sourceItemUid))continue;
   var item=run.inventory.FirstOrDefault(x=>x.uid==card.sourceItemUid);
   if(item==null)continue;
   var trait=StorageFormulaCatalog.Trait(item.traitId);
   if(trait==null)continue;
   int matches=0;
   colors.TryGetValue(trait.element,out matches);
   if(matches<trait.requiredMatches)continue;
   switch(trait.effect){
    case ColorTraitEffect.Damage:if(card.damage>0)card.damage+=trait.amount;break;
    case ColorTraitEffect.Block:if(card.block>0)card.block+=trait.amount;break;
    case ColorTraitEffect.Heal:if(card.heal>0)card.heal+=trait.amount;break;
    case ColorTraitEffect.CostReduce:card.cost=Mathf.Max(0,card.cost-trait.amount);break;
    case ColorTraitEffect.Draw:card.draw+=trait.amount;break;
    case ColorTraitEffect.Recycle:card.recycle=true;break;
    case ColorTraitEffect.DurabilityFree:card.durabilityFree=true;break;
   }
  }
 }

 public static string[] ResolveCardIds(ItemDef def,List<string> neighborTemplates,ResonanceFormulaDef resonance){
  var ids=(def.cardIds?.Length>0?def.cardIds:new[]{def.cardId}).ToArray();
  if(resonance?.upgrades==null)return ids;
  foreach(var upgrade in resonance.upgrades){
   if(upgrade.hostTemplate!=def.id||!neighborTemplates.Contains(upgrade.neighborTemplate))continue;
   if(upgrade.replaceAllCards&&!string.IsNullOrEmpty(upgrade.toCardId))return new[]{upgrade.toCardId};
   if(string.IsNullOrEmpty(upgrade.fromCardId))continue;
   ids=ids.Select(x=>x==upgrade.fromCardId?upgrade.toCardId:x).ToArray();
  }
  return ids;
 }

 public static void ApplyResonanceLinks(RunState run,ResonanceFormulaDef resonance,List<CardInstance> cards,List<string> log){
  if(resonance?.links==null||run==null)return;
  foreach(var a in run.placements)
  foreach(var b in run.placements.Where(x=>string.CompareOrdinal(x.itemUid,a.itemUid)>0&&BackpackSystem.Adjacent(run,a,x))){
   var ia=run.inventory.First(x=>x.uid==a.itemUid);
   var ib=run.inventory.First(x=>x.uid==b.itemUid);
   foreach(var link in resonance.links){
    if(!TryApplyLink(link,ia,ib,cards))continue;
    if(!string.IsNullOrEmpty(link.label)&&!log.Contains(link.label))log.Add(link.label);
   }
  }
 }

 static bool TryApplyLink(ResonanceLinkDef link,ItemInstance ia,ItemInstance ib,List<CardInstance> cards){
  var pair=new[]{ia.templateId,ib.templateId};
  var types=new[]{GameCatalog.Items[pair[0]].type,GameCatalog.Items[pair[1]].type};
  IEnumerable<CardInstance> Own(string uid)=>cards.Where(x=>x.sourceItemUid==uid);

  // Exact template pair (e.g. sword × shield)
  if(!string.IsNullOrEmpty(link.templateA)&&!string.IsNullOrEmpty(link.templateB)){
   if(!(pair.Contains(link.templateA)&&pair.Contains(link.templateB)))return false;
   string uidA=pair[0]==link.templateA?ia.uid:ib.uid;
   string uidB=pair[0]==link.templateB?ia.uid:ib.uid;
   foreach(var c in Own(uidA))if(link.damageBonus>0&&c.damage>0)c.damage+=link.damageBonus;
   foreach(var c in Own(uidB))if(link.blockBonus>0&&c.block>0)c.block+=link.blockBonus;
   if(link.costReduce>0){
    foreach(var c in Own(uidA))c.cost=Mathf.Max(0,c.cost-link.costReduce);
    foreach(var c in Own(uidB))c.cost=Mathf.Max(0,c.cost-link.costReduce);
   }
   return true;
  }

  // Template × item type (e.g. ember × weapon)
  if(!string.IsNullOrEmpty(link.templateA)&&link.typeB.HasValue){
   if(!pair.Contains(link.templateA))return false;
   if(types[0]!=link.typeB.Value&&types[1]!=link.typeB.Value)return false;
   string targetUid=types[0]==link.typeB.Value?ia.uid:ib.uid;
   foreach(var c in Own(targetUid)){
    if(link.damageBonus>0&&c.damage>0)c.damage+=link.damageBonus;
    if(link.blockBonus>0&&c.block>0)c.block+=link.blockBonus;
    if(link.costReduce>0)c.cost=Mathf.Max(0,c.cost-link.costReduce);
   }
   return true;
  }

  // Template × any other equipment (e.g. crystal)
  if(!string.IsNullOrEmpty(link.templateA)){
   if(!pair.Contains(link.templateA))return false;
   string otherUid=pair[0]==link.templateA?ib.uid:ia.uid;
   foreach(var c in Own(otherUid)){
    if(link.damageBonus>0&&c.damage>0)c.damage+=link.damageBonus;
    if(link.blockBonus>0&&c.block>0)c.block+=link.blockBonus;
    if(link.costReduce>0)c.cost=Mathf.Max(0,c.cost-link.costReduce);
   }
   return true;
  }
  return false;
 }

 public static void EnsureItemRolled(ItemInstance item,System.Random rng=null){
  if(item==null||!GameCatalog.Items.TryGetValue(item.templateId,out var def))return;
  item.colors??=new List<Element>();
  if(item.colors.Count==0){
   rng??=new System.Random();
   foreach(var cell in def.cells)item.colors.Add(cell.element);
  }
  if(string.IsNullOrEmpty(item.traitId)&&StorageFormulaCatalog.ColorTraitPool.Length>0){
   rng??=new System.Random();
   item.traitId=StorageFormulaCatalog.ColorTraitPool[rng.Next(StorageFormulaCatalog.ColorTraitPool.Length)];
  }
 }

 static string FirstNonEmpty(params string[] values){
  foreach(var value in values)if(!string.IsNullOrEmpty(value))return value;
  return "";
 }
}
}
