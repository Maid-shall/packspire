using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Packspire {
public enum EffectTarget { Self, Enemy, Player }
public enum ConsumableEffectType { Heal, Block, Damage, Energy }
public enum CharacterSkillKind { None, Damage, Block, BlockAndDraw, Heal }
public enum EventEffectType { None, Hp, Gold, RepairAll }

[Serializable] public class CellContent {
 public int x,y,value=1;
 public Element element;
}

[Serializable] public class EffectContent {
 public string statusId;
 public EffectTarget target;
 public int amount=1,duration;
}

[Serializable] public class ItemContent {
 public string id,name,description,linkRule,explorationCardId;
 public ItemType type;
 public string[] cardIds=Array.Empty<string>();
 public CellContent[] cells=Array.Empty<CellContent>();
 public Sprite artwork;
}

[Serializable] public class ExplorationCardContent {
 public string id,name,text,place;
 public int cost=1;
 public Sprite artwork;
}

[Serializable] public class CardContent {
 public string id,name,text;
 public CardType type;
 public int cost,damage,block,heal,buff,energy,selfDamage;
 public bool exhaust;
 public EffectContent[] effects=Array.Empty<EffectContent>();
 public Sprite artwork;
}

[Serializable] public class RoleContent {
 public string id,name,kind,description,family,milestoneText,maximumMilestoneText;
 public string[] startingCardIds=Array.Empty<string>();
 public int maxLevel=10;
 public Sprite artwork;
}

[Serializable] public class EnemyMoveContent {
 public int damage;
 public EffectContent[] effects=Array.Empty<EffectContent>();
}

[Serializable] public class EnemyContent {
 public string id,name;
 public int tier,hp;
 public EnemyMoveContent[] moves=Array.Empty<EnemyMoveContent>();
 public Sprite portrait;
 public string legacyPortraitResource;
}

[Serializable] public class DungeonContent {
 public string id,name,description;
 public int battles,damage;
 public float hpScale=1f,goldScale=1f;
 public Sprite artwork;
}

[Serializable] public class BackpackContent {
 public string id,name,description;
 public int[] safeCells=Array.Empty<int>();
 public Sprite artwork;
}

[Serializable] public class FactionContent {
 public string id,name,description;
 public string[] ranks=Array.Empty<string>();
 public Sprite artwork;
}

[Serializable] public class CharacterContent {
 public string id,name,title,description;
 public int portraitBody,portraitHair;
 public Sprite portrait,portraitFront,portraitHub;
 public string legacyPortraitResource,legacyPortraitFrontResource,legacyPortraitHubResource;
 public string traitName,traitText,traitKind;
 public int traitValue;
 public string activeSkillId,activeSkillName,activeSkillText;
 public CharacterSkillKind activeSkillKind;
 public int activeSkillAmount,activeSkillSecondaryAmount;
 public float portraitFocusX=.5f,portraitFocusY=.26f,portraitZoom=2.2f;
 public float portraitBannerOffsetX,portraitBannerOffsetY;
}

[Serializable] public class FacilityContent {
 public string id,eyebrow,label,description,themeKey="default",seal;
 public ScreenId screen;
 public HubFacilityKind kind;
 public bool hubCard,unlocked=true;
 public Sprite icon;
 public string legacyIconResource;
 public float mapX=.5f,mapY=.5f;
}

[Serializable] public class StatusContent {
 public string id,name,icon,description,kind;
 public bool stack;
 public Sprite artwork;
}

[Serializable] public class ConsumableContent {
 public string id,name,description;
 public ConsumableEffectType effect;
 public int amount;
 public Sprite artwork;
}

[Serializable] public class EventChoiceContent {
 public string id,label,resultText;
 public EventEffectContent[] effects=Array.Empty<EventEffectContent>();
}

[Serializable] public class EventEffectContent {
 public EventEffectType effect;
 public int amount;
}

[Serializable] public class EventContent {
 public string id,eyebrow,title,body;
 public EventChoiceContent[] choices=Array.Empty<EventChoiceContent>();
 public Sprite artwork;
}

[Serializable] public class MerchantContent {
 public string id,contextId,displayName;
 public Sprite character,backdrop,counter;
 public string legacyCharacterResource,legacyBackdropResource,legacyCounterResource;
 public string idleLine,soldOutLine,purchaseLine;
 public float characterScale=1.35f,characterOffsetX,characterOffsetY=18f,characterViewportHeight=.7f;
 public string affinityNote,factionId,bargainLine,rumorLine,specialDealLine;
}

[Serializable] public class RewardPoolContent {
 public string id;
 public string[] itemIds=Array.Empty<string>();
}

[Serializable] public class GameBalanceContent {
 public int baseEnergy=3,initialHand=5,gridGrowthThreshold=3,gridDoomMax=6;
 public float gridMoveSpeed=2.4f,gridFogMoveSpeed=1.2f;
 public string defaultDungeonId="old_spire",defaultCharacterId="ren",defaultRoleId="warrior";
 public string defaultFactionId="iron",defaultBackpackId="standard",defaultEventId="memory_rift";
}

[Serializable] public class StorageCoreContent {
 public string id,name,description;
 public int width=6,height=4;
 public Element[] board=Array.Empty<Element>();
 public RotationCapability rotation;
 public Sprite artwork;
}

[Serializable] public class ConduitBonusContent {
 public Element element;
 public ConduitBonusTarget target;
 public int threshold,amountPerMatch=1,matchDivisor=1;
 public bool useHalfWaterHeal;
}

[Serializable] public class ConduitContent {
 public string id,name,description;
 public ConduitBonusContent[] bonuses=Array.Empty<ConduitBonusContent>();
 public Sprite artwork;
}

[Serializable] public class ResonanceLinkContent {
 public string label,templateA,templateB;
 public bool useTypeA,useTypeB;
 public ItemType typeA,typeB;
 public int damageBonus,blockBonus,costReduce;
}

[Serializable] public class ResonanceUpgradeContent {
 public string hostTemplate,neighborTemplate,fromCardId,toCardId;
 public bool replaceAllCards;
}

[Serializable] public class ResonanceContent {
 public string id,name,description;
 public ResonanceLinkContent[] links=Array.Empty<ResonanceLinkContent>();
 public ResonanceUpgradeContent[] upgrades=Array.Empty<ResonanceUpgradeContent>();
 public Sprite artwork;
}

[Serializable] public class StabilityContent {
 public string id,name,description;
 public float durabilityDrainScale=1f,runawayCardPenalty=.5f;
 public int runawayThreshold=99;
 public Sprite artwork;
}

[Serializable] public class ColorTraitContent {
 public string id,name;
 public Element element;
 public int requiredMatches,amount=1;
 public ColorTraitEffect effect;
}

[CreateAssetMenu(fileName="PackspireContentDatabase",menuName="PACKSPIRE/Content Database")]
public sealed class PackspireContentDatabase : ScriptableObject {
 public const int SupportedSchemaVersion=2;
 public int schemaVersion=SupportedSchemaVersion;
 public PackspireCardContentSet cardContent;
 public PackspireItemContentSet itemContent;
 public PackspireActorContentSet actorContent;
 public PackspireWorldContentSet worldContent;

 public Element[] board=>itemContent!=null?itemContent.board:Array.Empty<Element>();
 public ItemContent[] items=>itemContent!=null?itemContent.items:Array.Empty<ItemContent>();
 public CardContent[] cards=>cardContent!=null?cardContent.cards:Array.Empty<CardContent>();
 public ExplorationCardContent[] explorationCards=>cardContent!=null?cardContent.explorationCards:Array.Empty<ExplorationCardContent>();
 public ConsumableContent[] consumables=>cardContent!=null?cardContent.consumables:Array.Empty<ConsumableContent>();
 public StatusContent[] statuses=>cardContent!=null?cardContent.statuses:Array.Empty<StatusContent>();
 public RoleContent[] roles=>actorContent!=null?actorContent.roles:Array.Empty<RoleContent>();
 public EnemyContent[] enemies=>actorContent!=null?actorContent.enemies:Array.Empty<EnemyContent>();
 public FactionContent[] factions=>actorContent!=null?actorContent.factions:Array.Empty<FactionContent>();
 public CharacterContent[] characters=>actorContent!=null?actorContent.characters:Array.Empty<CharacterContent>();
 public DungeonContent[] dungeons=>worldContent!=null?worldContent.dungeons:Array.Empty<DungeonContent>();
 public FacilityContent[] facilities=>worldContent!=null?worldContent.facilities:Array.Empty<FacilityContent>();
 public EventContent[] events=>worldContent!=null?worldContent.events:Array.Empty<EventContent>();
 public MerchantContent[] merchants=>worldContent!=null?worldContent.merchants:Array.Empty<MerchantContent>();
 public RewardPoolContent[] rewardPools=>worldContent!=null?worldContent.rewardPools:Array.Empty<RewardPoolContent>();
 public GameBalanceContent balance=>worldContent!=null&&worldContent.balance!=null?worldContent.balance:new GameBalanceContent();
 public BackpackContent[] backpacks=>itemContent!=null?itemContent.backpacks:Array.Empty<BackpackContent>();
 public StorageCoreContent[] storageCores=>itemContent!=null?itemContent.storageCores:Array.Empty<StorageCoreContent>();
 public ConduitContent[] conduits=>itemContent!=null?itemContent.conduits:Array.Empty<ConduitContent>();
 public ResonanceContent[] resonances=>itemContent!=null?itemContent.resonances:Array.Empty<ResonanceContent>();
 public StabilityContent[] stabilities=>itemContent!=null?itemContent.stabilities:Array.Empty<StabilityContent>();
 public ColorTraitContent[] colorTraits=>itemContent!=null?itemContent.colorTraits:Array.Empty<ColorTraitContent>();
}

public sealed class ContentValidationReport {
 public readonly List<string> errors=new();
 public readonly List<string> warnings=new();
 public bool IsValid=>errors.Count==0;
 public string Summary=>$"content validation: {errors.Count} error(s), {warnings.Count} warning(s)";
}

/// <summary>
/// Loads the authored ScriptableObject database. Save and run state remain JSON and
/// refer to this database only through stable string ids.
/// </summary>
public static class PackspireContent {
 public const string ResourcePath="Packspire/PackspireContentDatabase";
 static PackspireContentDatabase data;
 static ContentValidationReport validation;

 public static PackspireContentDatabase Data {
  get {
   if(data!=null)return data;
   data=PackspireResources.Load<PackspireContentDatabase>(ResourcePath);
   if(data==null){
    Debug.LogError($"PACKSPIRE content database was not found at Resources/{ResourcePath}.asset.");
    data=ScriptableObject.CreateInstance<PackspireContentDatabase>();
   }
   validation=Validate(data);
   foreach(var warning in validation.warnings)Debug.LogWarning($"Packspire data: {warning}");
   foreach(var error in validation.errors)Debug.LogError($"Packspire data: {error}");
   Debug.Log(validation.Summary);
   return data;
  }
 }

 public static ContentValidationReport Validation {
  get {
   _=Data;
   return validation;
  }
 }

 public static ContentValidationReport Validate(PackspireContentDatabase value){
  var result=new ContentValidationReport();
  if(value==null){result.errors.Add("Content database is null.");return result;}
  if(value.schemaVersion!=PackspireContentDatabase.SupportedSchemaVersion)
   result.errors.Add($"Unsupported schemaVersion {value.schemaVersion}; expected {PackspireContentDatabase.SupportedSchemaVersion}.");
  if(value.cardContent==null)result.errors.Add("Card content set is missing.");
  if(value.itemContent==null)result.errors.Add("Item content set is missing.");
  if(value.actorContent==null)result.errors.Add("Actor content set is missing.");
  if(value.worldContent==null)result.errors.Add("World content set is missing.");

  NormalizeNullCollections(value);
  ValidateIds(value.items.Select(x=>x.id),"item",result);
  ValidateIds(value.cards.Select(x=>x.id),"card",result);
  ValidateIds(value.explorationCards.Select(x=>x.id),"exploration card",result);
  ValidateIds(value.consumables.Select(x=>x.id),"consumable",result);
  ValidateIds(value.roles.Select(x=>x.id),"role",result);
  ValidateIds(value.enemies.Select(x=>x.id),"enemy",result);
  ValidateIds(value.dungeons.Select(x=>x.id),"dungeon",result);
  ValidateIds(value.backpacks.Select(x=>x.id),"backpack",result);
  ValidateIds(value.factions.Select(x=>x.id),"faction",result);
  ValidateIds(value.characters.Select(x=>x.id),"character",result);
  ValidateIds(value.facilities.Select(x=>x.id),"facility",result);
  ValidateIds(value.events.Select(x=>x.id),"event",result);
  ValidateIds(value.merchants.Select(x=>x.id),"merchant",result);
  ValidateIds(value.rewardPools.Select(x=>x.id),"reward pool",result);
  ValidateIds(value.statuses.Select(x=>x.id),"status",result);
  ValidateIds(value.storageCores.Select(x=>x.id),"storage core",result);
  ValidateIds(value.conduits.Select(x=>x.id),"conduit",result);
  ValidateIds(value.resonances.Select(x=>x.id),"resonance",result);
  ValidateIds(value.stabilities.Select(x=>x.id),"stability",result);
  ValidateIds(value.colorTraits.Select(x=>x.id),"color trait",result);

  if(value.board.Length!=24)result.errors.Add($"Base storage board has {value.board.Length} cells; expected 24.");
  var itemIds=value.items.Select(x=>x.id).ToHashSet();
  var cardIds=value.cards.Select(x=>x.id).ToHashSet();
  var explorationCardIds=value.explorationCards.Select(x=>x.id).ToHashSet();
  var statusIds=value.statuses.Select(x=>x.id).ToHashSet();
  var eventIds=value.events.Select(x=>x.id).ToHashSet();

  foreach(var item in value.items){
   if(item.cells==null||item.cells.Length==0)result.errors.Add($"Item '{item.id}' has no occupied cells.");
   foreach(var cardId in item.cardIds??Array.Empty<string>())
    if(!cardIds.Contains(cardId))result.errors.Add($"Item '{item.id}' references missing card '{cardId}'.");
   if(!explorationCardIds.Contains(item.explorationCardId))
    result.errors.Add($"Item '{item.id}' references missing exploration card '{item.explorationCardId}'.");
  }
  foreach(var exploration in value.explorationCards){
   if(string.IsNullOrWhiteSpace(exploration.place))
    result.errors.Add($"Exploration card '{exploration.id}' has no placement type.");
   if(exploration.cost<0)result.errors.Add($"Exploration card '{exploration.id}' has a negative cost.");
  }
  foreach(var consumable in value.consumables)
   if(consumable.amount<0)result.errors.Add($"Consumable '{consumable.id}' has a negative amount.");
  foreach(var card in value.cards)ValidateEffects(card.effects,$"card '{card.id}'",statusIds,result);
  foreach(var role in value.roles)
   foreach(var cardId in role.startingCardIds??Array.Empty<string>())
    if(!cardIds.Contains(cardId))result.errors.Add($"Role '{role.id}' references missing starting card '{cardId}'.");
  foreach(var character in value.characters){
   if(character.activeSkillKind==CharacterSkillKind.None)
    result.errors.Add($"Character '{character.id}' has no active skill kind.");
   if(character.activeSkillAmount<0)
    result.errors.Add($"Character '{character.id}' has a negative active skill amount.");
  }
  foreach(var enemy in value.enemies)
   foreach(var move in enemy.moves??Array.Empty<EnemyMoveContent>())
    ValidateEffects(move.effects,$"enemy '{enemy.id}'",statusIds,result);
  foreach(var core in value.storageCores)
   if(core.board==null||core.board.Length!=core.width*core.height)
    result.errors.Add($"Storage core '{core.id}' board size does not match {core.width}x{core.height}.");
  foreach(var resonance in value.resonances){
   foreach(var link in resonance.links??Array.Empty<ResonanceLinkContent>()){
    ValidateOptionalItem(link.templateA,$"resonance '{resonance.id}'",itemIds,result);
    ValidateOptionalItem(link.templateB,$"resonance '{resonance.id}'",itemIds,result);
   }
   foreach(var upgrade in resonance.upgrades??Array.Empty<ResonanceUpgradeContent>()){
    ValidateOptionalItem(upgrade.hostTemplate,$"resonance '{resonance.id}'",itemIds,result);
    ValidateOptionalItem(upgrade.neighborTemplate,$"resonance '{resonance.id}'",itemIds,result);
    if(!string.IsNullOrEmpty(upgrade.fromCardId)&&!cardIds.Contains(upgrade.fromCardId))
     result.errors.Add($"Resonance '{resonance.id}' references missing card '{upgrade.fromCardId}'.");
    if(!cardIds.Contains(upgrade.toCardId))
     result.errors.Add($"Resonance '{resonance.id}' references missing card '{upgrade.toCardId}'.");
   }
  }
  foreach(var rewardPool in value.rewardPools)
   foreach(var itemId in rewardPool.itemIds??Array.Empty<string>())
    if(!itemIds.Contains(itemId))result.errors.Add($"Reward pool '{rewardPool.id}' references missing item '{itemId}'.");
  foreach(var eventContent in value.events){
   if(eventContent.choices==null||eventContent.choices.Length==0)
    result.errors.Add($"Event '{eventContent.id}' has no choices.");
   else ValidateIds(eventContent.choices.Select(x=>x.id),$"event choice in '{eventContent.id}'",result);
  }
  foreach(var merchant in value.merchants)
   if(string.IsNullOrWhiteSpace(merchant.contextId))
    result.errors.Add($"Merchant '{merchant.id}' has no context id.");
  if(value.worldContent?.balance==null)result.errors.Add("Game balance settings are missing.");
  else{
   if(!value.dungeons.Any(x=>x.id==value.balance.defaultDungeonId))
    result.errors.Add($"Balance references missing default dungeon '{value.balance.defaultDungeonId}'.");
   if(!value.characters.Any(x=>x.id==value.balance.defaultCharacterId))
    result.errors.Add($"Balance references missing default character '{value.balance.defaultCharacterId}'.");
   if(!value.roles.Any(x=>x.id==value.balance.defaultRoleId))
    result.errors.Add($"Balance references missing default role '{value.balance.defaultRoleId}'.");
   if(!value.factions.Any(x=>x.id==value.balance.defaultFactionId))
    result.errors.Add($"Balance references missing default faction '{value.balance.defaultFactionId}'.");
   if(!value.backpacks.Any(x=>x.id==value.balance.defaultBackpackId))
    result.errors.Add($"Balance references missing default backpack '{value.balance.defaultBackpackId}'.");
   if(!eventIds.Contains(value.balance.defaultEventId))
    result.errors.Add($"Balance references missing default event '{value.balance.defaultEventId}'.");
  }
  Require(value.items.Any(x=>x.id=="sword"),"Default item 'sword' is missing.",result);
  Require(value.cards.Any(x=>x.id=="basicStrike"),"Default card 'basicStrike' is missing.",result);
  Require(value.explorationCards.Any(x=>x.id=="gb_lamp"),"Default exploration card 'gb_lamp' is missing.",result);
  Require(value.explorationCards.Any(x=>x.id=="gb_fog"),"Default exploration card 'gb_fog' is missing.",result);
  Require(value.explorationCards.Any(x=>x.id=="gb_seal"),"Default exploration card 'gb_seal' is missing.",result);
  Require(value.consumables.Any(x=>x.id=="heal"),"Default consumable 'heal' is missing.",result);
  Require(value.consumables.Any(x=>x.id=="guard"),"Default consumable 'guard' is missing.",result);
  Require(value.consumables.Any(x=>x.id=="fire"),"Default consumable 'fire' is missing.",result);
  Require(value.consumables.Any(x=>x.id=="energy"),"Default consumable 'energy' is missing.",result);
  Require(value.roles.Any(x=>x.id=="warrior"),"Default role 'warrior' is missing.",result);
  Require(value.dungeons.Any(x=>x.id=="old_spire"),"Default dungeon 'old_spire' is missing.",result);
  Require(value.characters.Any(x=>x.id=="ren"),"Default character 'ren' is missing.",result);
  Require(value.events.Any(x=>x.id=="memory_rift"),"Default event 'memory_rift' is missing.",result);
  Require(value.merchants.Any(x=>x.id=="traveler"),"Default merchant 'traveler' is missing.",result);
  Require(value.rewardPools.Any(x=>x.id=="standard"),"Default reward pool 'standard' is missing.",result);
  Require(value.storageCores.Any(x=>x.id=="standard"),"Default storage core 'standard' is missing.",result);
  Require(value.conduits.Any(x=>x.id=="classic"),"Default conduit 'classic' is missing.",result);
  Require(value.resonances.Any(x=>x.id=="classic"),"Default resonance 'classic' is missing.",result);
  Require(value.stabilities.Any(x=>x.id=="stable"),"Default stability 'stable' is missing.",result);
  return result;
 }

 static void NormalizeNullCollections(PackspireContentDatabase value){
  if(value.cardContent!=null){
   value.cardContent.cards??=Array.Empty<CardContent>();
   value.cardContent.explorationCards??=Array.Empty<ExplorationCardContent>();
   value.cardContent.consumables??=Array.Empty<ConsumableContent>();
   value.cardContent.statuses??=Array.Empty<StatusContent>();
  }
  if(value.itemContent!=null){
   value.itemContent.board??=Array.Empty<Element>();
   value.itemContent.items??=Array.Empty<ItemContent>();
   value.itemContent.backpacks??=Array.Empty<BackpackContent>();
   value.itemContent.storageCores??=Array.Empty<StorageCoreContent>();
   value.itemContent.conduits??=Array.Empty<ConduitContent>();
   value.itemContent.resonances??=Array.Empty<ResonanceContent>();
   value.itemContent.stabilities??=Array.Empty<StabilityContent>();
   value.itemContent.colorTraits??=Array.Empty<ColorTraitContent>();
  }
  if(value.actorContent!=null){
   value.actorContent.roles??=Array.Empty<RoleContent>();
   value.actorContent.enemies??=Array.Empty<EnemyContent>();
   value.actorContent.factions??=Array.Empty<FactionContent>();
   value.actorContent.characters??=Array.Empty<CharacterContent>();
  }
  if(value.worldContent!=null){
   value.worldContent.dungeons??=Array.Empty<DungeonContent>();
   value.worldContent.facilities??=Array.Empty<FacilityContent>();
   value.worldContent.events??=Array.Empty<EventContent>();
   value.worldContent.merchants??=Array.Empty<MerchantContent>();
   value.worldContent.rewardPools??=Array.Empty<RewardPoolContent>();
   value.worldContent.balance??=new GameBalanceContent();
  }
 }

 static void ValidateEffects(IEnumerable<EffectContent> effects,string owner,HashSet<string> statusIds,ContentValidationReport result){
  foreach(var effect in effects??Enumerable.Empty<EffectContent>())
   if(!statusIds.Contains(effect.statusId))result.errors.Add($"{owner} references missing status '{effect.statusId}'.");
 }

 static void ValidateOptionalItem(string id,string owner,HashSet<string> itemIds,ContentValidationReport result){
  if(!string.IsNullOrEmpty(id)&&!itemIds.Contains(id))result.errors.Add($"{owner} references missing item '{id}'.");
 }

 static void ValidateIds(IEnumerable<string> ids,string category,ContentValidationReport result){
  var seen=new HashSet<string>();
  foreach(var id in ids){
   if(string.IsNullOrWhiteSpace(id)){result.errors.Add($"{category} has an empty id.");continue;}
   if(!seen.Add(id))result.errors.Add($"Duplicate {category} id '{id}'.");
  }
 }

 static void Require(bool condition,string error,ContentValidationReport result){
  if(!condition)result.errors.Add(error);
 }
}
}
