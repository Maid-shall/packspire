using System;
using System.Collections.Generic;
using UnityEngine;

namespace Packspire {
/// <summary>How equipment may be rotated on a storage core.</summary>
public enum RotationCapability {
 /// <summary>0° and 180° only.</summary>
 FlipOnly,
 /// <summary>0° / 90° / 180° / 270°.</summary>
 QuarterTurn,
 /// <summary>Same discrete turns as QuarterTurn for now; reserved for mirrors / free orbit later.</summary>
 FullTurn,
}

public enum ColorTraitEffect {
 Damage,
 Block,
 Heal,
 CostReduce,
 Draw,
 Recycle,
 DurabilityFree,
}

public enum ConduitBonusTarget {
 Damage,
 Block,
 Heal,
 CostReduce,
}

/// <summary>Grid size, cell colors, and rotation rules for the magic circle.</summary>
[Serializable]
public class StorageCoreDef {
 public string id,name,description;
 public int width,height;
 public Element[] board;
 public RotationCapability rotation;
 public StorageCoreDef(string id,string name,string description,int width,int height,Element[] board,RotationCapability rotation=RotationCapability.FullTurn){
  this.id=id;this.name=name;this.description=description;this.width=width;this.height=height;this.board=board;this.rotation=rotation;
 }
 public int CellCount=>width*height;
}

/// <summary>How matched attribute colors grant global bonuses.</summary>
[Serializable]
public class ConduitBonusRule {
 public Element element;
 public ConduitBonusTarget target;
 public int threshold;
 public int amountPerMatch;
 public int matchDivisor;
 public bool useHalfWaterHeal;
 public ConduitBonusRule(Element element,ConduitBonusTarget target,int threshold=0,int amountPerMatch=1,bool useHalfWaterHeal=false,int matchDivisor=1){
  this.element=element;this.target=target;this.threshold=threshold;this.amountPerMatch=amountPerMatch;this.useHalfWaterHeal=useHalfWaterHeal;this.matchDivisor=Mathf.Max(1,matchDivisor);
 }
}

[Serializable]
public class AttributeConduitDef {
 public string id,name,description;
 public ConduitBonusRule[] bonuses;
 public AttributeConduitDef(string id,string name,string description,params ConduitBonusRule[] bonuses){
  this.id=id;this.name=name;this.description=description;this.bonuses=bonuses??Array.Empty<ConduitBonusRule>();
 }
}

/// <summary>Card upgrade when two templates are adjacent (e.g. sword+ember → inferno).</summary>
[Serializable]
public class ResonanceUpgradeDef {
 public string hostTemplate,neighborTemplate,fromCardId,toCardId;
 public bool replaceAllCards;
 public ResonanceUpgradeDef(string host,string neighbor,string fromCard,string toCard,bool replaceAll=false){
  hostTemplate=host;neighborTemplate=neighbor;fromCardId=fromCard;toCardId=toCard;replaceAllCards=replaceAll;
 }
}

[Serializable]
public class ResonanceLinkDef {
 public string label;
 public string templateA,templateB;
 public ItemType? typeA,typeB;
 public int damageBonus,blockBonus,costReduce;
 public ResonanceLinkDef(string label,string templateA=null,string templateB=null,ItemType? typeA=null,ItemType? typeB=null,int damageBonus=0,int blockBonus=0,int costReduce=0){
  this.label=label;this.templateA=templateA;this.templateB=templateB;this.typeA=typeA;this.typeB=typeB;this.damageBonus=damageBonus;this.blockBonus=blockBonus;this.costReduce=costReduce;
 }
}

[Serializable]
public class ResonanceFormulaDef {
 public string id,name,description;
 public ResonanceLinkDef[] links;
 public ResonanceUpgradeDef[] upgrades;
 public ResonanceFormulaDef(string id,string name,string description,ResonanceLinkDef[] links,ResonanceUpgradeDef[] upgrades=null){
  this.id=id;this.name=name;this.description=description;this.links=links??Array.Empty<ResonanceLinkDef>();this.upgrades=upgrades??Array.Empty<ResonanceUpgradeDef>();
 }
}

/// <summary>Durability drain and runaway risk hooks. Numbers are placeholders until balance.</summary>
[Serializable]
public class StabilityFormulaDef {
 public string id,name,description;
 public float durabilityDrainScale;
 public int runawayThreshold;
 public float runawayCardPenalty;
 public StabilityFormulaDef(string id,string name,string description,float durabilityDrainScale=1f,int runawayThreshold=99,float runawayCardPenalty=.5f){
  this.id=id;this.name=name;this.description=description;this.durabilityDrainScale=durabilityDrainScale;this.runawayThreshold=runawayThreshold;this.runawayCardPenalty=runawayCardPenalty;
 }
}

[Serializable]
public class ColorTraitDef {
 public string id,name;
 public Element element;
 public int requiredMatches;
 public ColorTraitEffect effect;
 public int amount;
 public ColorTraitDef(string id,string name,Element element,int required,ColorTraitEffect effect,int amount=1){
  this.id=id;this.name=name;this.element=element;requiredMatches=required;this.effect=effect;this.amount=amount;
 }
}

/// <summary>Resolved four-part storage formula for the current loadout/run.</summary>
public readonly struct ActiveStorageFormula {
 public readonly StorageCoreDef core;
 public readonly AttributeConduitDef conduit;
 public readonly ResonanceFormulaDef resonance;
 public readonly StabilityFormulaDef stability;
 public ActiveStorageFormula(StorageCoreDef core,AttributeConduitDef conduit,ResonanceFormulaDef resonance,StabilityFormulaDef stability){
  this.core=core;this.conduit=conduit;this.resonance=resonance;this.stability=stability;
 }
}

public sealed class StabilityEval {
 public int strain;
 public bool runaway;
 public float durabilityDrainScale=1f;
 public float cardPenalty=1f;
}
}
