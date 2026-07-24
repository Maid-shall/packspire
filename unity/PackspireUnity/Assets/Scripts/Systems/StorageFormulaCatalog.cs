using System.Collections.Generic;
using System.Linq;

namespace Packspire {
/// <summary>Catalog of swappable storage-formula parts. Defaults mirror the current backpack behavior.</summary>
public static class StorageFormulaCatalog {
 public const string DefaultCoreId="standard";
 public const string DefaultConduitId="classic";
 public const string DefaultResonanceId="classic";
 public const string DefaultStabilityId="stable";

 static readonly Element[] ClassicBoard=GameCatalog.Board.ToArray();
 static readonly Element[] MerchantBoard=ShiftBoard(ClassicBoard,1);
 static readonly Element[] ArcaneBoard=ShiftBoard(ClassicBoard,2);
 static readonly Element[] CoffinBoard=ShiftBoard(ClassicBoard,3);
 static readonly Element[] LivingBoard=MirrorBoard(ClassicBoard);

 public static readonly Dictionary<string,StorageCoreDef> Cores=new(){
  ["standard"]=new("standard","標準術核","6×4の基本魔方陣。装備を自由に回転できる。",6,4,ClassicBoard,RotationCapability.FullTurn),
  ["merchant"]=new("merchant","交易術核","流通向きの属性配置。回転はフル。",6,4,MerchantBoard,RotationCapability.FullTurn),
  ["arcane"]=new("arcane","魔導術核","ルーン向けに偏った属性配置。回転はフル。",6,4,ArcaneBoard,RotationCapability.FullTurn),
  ["coffin"]=new("coffin","棺型術核","縦長配置向け。90度単位の回転のみ。",6,4,CoffinBoard,RotationCapability.QuarterTurn),
  ["living"]=new("living","生体術核","反転した属性配置。反転（0°/180°）のみ。",6,4,LivingBoard,RotationCapability.FlipOnly),
 };

 public static readonly Dictionary<string,AttributeConduitDef> Conduits=new(){
  ["classic"]=new("classic","古典導線","一致色を全体ボーナスへ変換する現行ルール。",
   new ConduitBonusRule(Element.Fire,ConduitBonusTarget.Damage),
   new ConduitBonusRule(Element.Water,ConduitBonusTarget.Block),
   new ConduitBonusRule(Element.Water,ConduitBonusTarget.Heal,useHalfWaterHeal:true),
   new ConduitBonusRule(Element.Wind,ConduitBonusTarget.CostReduce,threshold:3,amountPerMatch:1),
   new ConduitBonusRule(Element.Earth,ConduitBonusTarget.Block,threshold:2,amountPerMatch:1,matchDivisor:2)),
  ["mute"]=new("mute","沈黙導線","色一致ボーナスを出さない試験用導線。"),
 };

 public static readonly Dictionary<string,ResonanceFormulaDef> Resonances=new(){
  ["classic"]=new("classic","古典共鳴","隣接装備の現行LINKと上位カード置換。",
   new[]{
    new ResonanceLinkDef("剣×盾",templateA:"sword",templateB:"shield",damageBonus:1,blockBonus:2),
    new ResonanceLinkDef("熾火×武器",templateA:"ember",typeB:ItemType.Weapon,damageBonus:2),
    new ResonanceLinkDef("結晶×装備",templateA:"crystal",costReduce:1),
   },
   new[]{
    new ResonanceUpgradeDef("sword","ember","slash","inferno"),
    new ResonanceUpgradeDef("shield","crystal","guard","echoWall"),
    new ResonanceUpgradeDef("bomb","flask",null,"starBomb",replaceAll:true),
   }),
  ["silent"]=new("silent","沈黙共鳴","LINKもカード置換も起こさない試験用。",System.Array.Empty<ResonanceLinkDef>()),
 };

 public static readonly Dictionary<string,StabilityFormulaDef> Stabilities=new(){
  ["stable"]=new("stable","安定式","耐久減衰は通常。暴走はほぼ起きない。",1f,99,.5f),
  ["volatile"]=new("volatile","過負荷式","耐久が減りやすく、高負荷でカードが減衰する試験用。",1.5f,8,.65f),
 };

 /// <summary>Per-item color traits (色効果). Separate from LINK.</summary>
 public static readonly Dictionary<string,ColorTraitDef> ColorTraits=new(){
  ["fire_dmg_5"]=new("fire_dmg_5","焔撃・小",Element.Fire,5,ColorTraitEffect.Damage,1),
  ["fire_dmg_7"]=new("fire_dmg_7","焔撃・大",Element.Fire,7,ColorTraitEffect.Damage,2),
  ["water_block_5"]=new("water_block_5","水防・小",Element.Water,5,ColorTraitEffect.Block,1),
  ["water_block_7"]=new("water_block_7","水防・大",Element.Water,7,ColorTraitEffect.Block,2),
  ["wind_cost_5"]=new("wind_cost_5","風軽・小",Element.Wind,5,ColorTraitEffect.CostReduce,1),
  ["wind_cost_7"]=new("wind_cost_7","風軽・大",Element.Wind,7,ColorTraitEffect.CostReduce,1),
  ["earth_block_5"]=new("earth_block_5","土守・小",Element.Earth,5,ColorTraitEffect.Block,1),
  ["earth_block_7"]=new("earth_block_7","土守・大",Element.Earth,7,ColorTraitEffect.Block,2),
  ["water_heal_5"]=new("water_heal_5","水癒・小",Element.Water,5,ColorTraitEffect.Heal,1),
  ["water_heal_7"]=new("water_heal_7","水癒・大",Element.Water,7,ColorTraitEffect.Heal,2),
  ["wind_draw_7"]=new("wind_draw_7","風読",Element.Wind,7,ColorTraitEffect.Draw,1),
  ["earth_recycle_7"]=new("earth_recycle_7","土還",Element.Earth,7,ColorTraitEffect.Recycle,1),
  ["fire_free_7"]=new("fire_free_7","不滅焔",Element.Fire,7,ColorTraitEffect.DurabilityFree,1),
 };

 public static readonly string[] ColorTraitPool=ColorTraits.Keys.ToArray();

 public static StorageCoreDef Core(string id)=>Cores.TryGetValue(id??"",out var core)?core:Cores[DefaultCoreId];
 public static AttributeConduitDef Conduit(string id)=>Conduits.TryGetValue(id??"",out var value)?value:Conduits[DefaultConduitId];
 public static ResonanceFormulaDef Resonance(string id)=>Resonances.TryGetValue(id??"",out var value)?value:Resonances[DefaultResonanceId];
 public static StabilityFormulaDef Stability(string id)=>Stabilities.TryGetValue(id??"",out var value)?value:Stabilities[DefaultStabilityId];
 public static ColorTraitDef Trait(string id)=>string.IsNullOrEmpty(id)||!ColorTraits.TryGetValue(id,out var value)?null:value;

 /// <summary>Legacy backpack ids map 1:1 onto storage cores.</summary>
 public static string CoreIdFromBackpack(string backpackId)=>Cores.ContainsKey(backpackId??"")?backpackId:DefaultCoreId;

 static Element[] ShiftBoard(Element[] source,int shift){
  var result=new Element[source.Length];
  for(int i=0;i<source.Length;i++)result[i]=source[(i+shift)%source.Length];
  return result;
 }

 static Element[] MirrorBoard(Element[] source){
  var result=new Element[source.Length];
  for(int i=0;i<source.Length;i++)result[i]=source[source.Length-1-i];
  return result;
 }
}
}
