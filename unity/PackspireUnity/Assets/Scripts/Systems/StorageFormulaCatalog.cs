using System;
using System.Collections.Generic;
using System.Linq;

namespace Packspire {
/// <summary>Runtime facade over storage-formula definitions in the master content asset.</summary>
public static class StorageFormulaCatalog {
 public const string DefaultCoreId="standard";
 public const string DefaultConduitId="classic";
 public const string DefaultResonanceId="classic";
 public const string DefaultStabilityId="stable";

 public static readonly Dictionary<string,StorageCoreDef> Cores=PackspireContent.Data.storageCores
  .Select(x=>new StorageCoreDef(x.id,x.name,x.description,x.width,x.height,x.board.ToArray(),x.rotation))
  .ToDictionary(x=>x.id);

 public static readonly Dictionary<string,AttributeConduitDef> Conduits=PackspireContent.Data.conduits
  .Select(x=>new AttributeConduitDef(x.id,x.name,x.description,(x.bonuses??Array.Empty<ConduitBonusContent>())
   .Select(b=>new ConduitBonusRule(b.element,b.target,b.threshold,b.amountPerMatch,b.useHalfWaterHeal,b.matchDivisor)).ToArray()))
  .ToDictionary(x=>x.id);

 public static readonly Dictionary<string,ResonanceFormulaDef> Resonances=PackspireContent.Data.resonances
  .Select(x=>new ResonanceFormulaDef(
   x.id,x.name,x.description,
   (x.links??Array.Empty<ResonanceLinkContent>()).Select(link=>new ResonanceLinkDef(
    link.label,link.templateA,link.templateB,
    link.useTypeA?(ItemType?)link.typeA:null,
    link.useTypeB?(ItemType?)link.typeB:null,
    link.damageBonus,link.blockBonus,link.costReduce)).ToArray(),
   (x.upgrades??Array.Empty<ResonanceUpgradeContent>()).Select(upgrade=>new ResonanceUpgradeDef(
    upgrade.hostTemplate,upgrade.neighborTemplate,upgrade.fromCardId,upgrade.toCardId,upgrade.replaceAllCards)).ToArray()))
  .ToDictionary(x=>x.id);

 public static readonly Dictionary<string,StabilityFormulaDef> Stabilities=PackspireContent.Data.stabilities
  .Select(x=>new StabilityFormulaDef(x.id,x.name,x.description,x.durabilityDrainScale,x.runawayThreshold,x.runawayCardPenalty))
  .ToDictionary(x=>x.id);

 public static readonly Dictionary<string,ColorTraitDef> ColorTraits=PackspireContent.Data.colorTraits
  .Select(x=>new ColorTraitDef(x.id,x.name,x.element,x.requiredMatches,x.effect,x.amount))
  .ToDictionary(x=>x.id);

 public static readonly string[] ColorTraitPool=ColorTraits.Keys.ToArray();

 public static StorageCoreDef Core(string id)=>Cores.TryGetValue(id??"",out var core)?core:Cores[DefaultCoreId];
 public static AttributeConduitDef Conduit(string id)=>Conduits.TryGetValue(id??"",out var value)?value:Conduits[DefaultConduitId];
 public static ResonanceFormulaDef Resonance(string id)=>Resonances.TryGetValue(id??"",out var value)?value:Resonances[DefaultResonanceId];
 public static StabilityFormulaDef Stability(string id)=>Stabilities.TryGetValue(id??"",out var value)?value:Stabilities[DefaultStabilityId];
 public static ColorTraitDef Trait(string id)=>string.IsNullOrEmpty(id)||!ColorTraits.TryGetValue(id,out var value)?null:value;
 public static string CoreIdFromBackpack(string backpackId)=>Cores.ContainsKey(backpackId??"")?backpackId:DefaultCoreId;
}
}
