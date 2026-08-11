using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Packspire {
public enum EffectTarget { Self, Enemy, Player }
public enum ConsumableEffectType { Heal, Block, Damage, Energy }
public enum CharacterSkillKind { None, Damage, Block, BlockAndDraw, Heal }
public enum EventEffectType { None, Hp, Gold, RepairAll }
public enum ItemRarity { Common, Uncommon, Rare, Legendary, Cursed }
public enum BattleFormationScale { Normal, Small, Large, Boss }
public enum DeliverySealAttribute { Incineration, Cooling, Silence, Mending }
public enum BattleCardAfterUse { Discard, ExhaustBattle, RemoveExpedition }
public enum ExplorationCardKind { Support, Use, Installation, Drawback }
public enum ExplorationTargetKind { None, Cell, Route, Installation, Enemy }
public enum ExplorationConsumeRule { Discard, ExhaustArea, RemoveExpedition, Persistent }
public enum ExplorationGrowthTrigger { None, TurnsElapsed, RoutePasses, Activations, AdjacentElement, Custom }
public enum ReactionScope { Mastery, Loadout, Expedition, Area, Moment, Memory }
[Flags] public enum ReactionScopeMask {
 None=0, Mastery=1<<0, Loadout=1<<1, Expedition=1<<2,
 Area=1<<3, Moment=1<<4, Memory=1<<5,
 Permanent=Mastery|Memory, Active=Loadout|Expedition|Area|Moment,
 All=Mastery|Loadout|Expedition|Area|Moment|Memory
}
public enum ReactionStackRule { Add, Highest, UniqueSource }
public enum ExplorationEffectType {
 None,Draw,Energy,Sight,Turns,Reveal,Growth,Doom,RemoveInstallation,MoveEnemy,Custom
}

[Serializable] public class CellContent {
 public int x,y,value=1;
 public Element element;
}

[Serializable] public class EffectContent {
 public string statusId;
 public EffectTarget target;
 public int amount=1,duration;
}

[Serializable] public class GrantedCardContent {
 public string battleCardId;
 [Min(1)] public int count=1;
}

[Serializable] public class ReactionValueContent {
 public string id,name,description,category;
}

[Serializable] public class ReactionContributionContent {
 public string reactionId;
 public int amount=1;
 public ReactionScope scope=ReactionScope.Mastery;
 public ReactionStackRule stackRule=ReactionStackRule.Add;
 public bool perLevel;
}

[Serializable] public class ReactionRequirementContent {
 public string reactionId;
 [Min(1)] public int minimum=1;
 public ReactionScopeMask acceptedScopes=ReactionScopeMask.Permanent;
 [Min(0)] public int minimumSources;
}

[Serializable] public class RoleUnlockRecipeContent {
 public string id,hint;
 public bool visibleBeforeUnlock=true;
 public ReactionRequirementContent[] requirements=Array.Empty<ReactionRequirementContent>();
}

[Serializable] public class ItemContent {
 public string id,name,description,linkRule;
 public DeliverySealAttribute sealAttribute;
 public ItemType type;
 public ItemRarity rarity;
 [Min(1)] public int acquisitionTier=1;
 [Min(0)] public int baseDurability=6;
 public string[] resonanceTags=Array.Empty<string>();
 public ReactionContributionContent[] reactionContributions=Array.Empty<ReactionContributionContent>();
 public GrantedCardContent[] grantedCards=Array.Empty<GrantedCardContent>();
 // Legacy fields remain readable while existing authored assets migrate.
 public string[] cardIds=Array.Empty<string>();
 public CellContent[] cells=Array.Empty<CellContent>();
 public Sprite artwork;
}

[Serializable] public class ExplorationEffectContent {
 public ExplorationEffectType type;
 public int amount=1,duration;
 public string parameter;
}

[Serializable] public class ExplorationStageContent {
 public string id,name,text;
 [Min(0)] public int minimumProgress;
 public ExplorationEffectContent[] passiveEffects=Array.Empty<ExplorationEffectContent>();
 public ExplorationEffectContent[] onEnterEffects=Array.Empty<ExplorationEffectContent>();
 public ExplorationEffectContent[] onTurnEffects=Array.Empty<ExplorationEffectContent>();
 public ExplorationEffectContent[] onMatureEffects=Array.Empty<ExplorationEffectContent>();
 public Sprite artwork,boardArtwork;
}

[Serializable] public class ExplorationCardContent {
 public string id,name,text,place;
 public int cost=1;
 public ExplorationCardKind kind=ExplorationCardKind.Installation;
 public ExplorationTargetKind target=ExplorationTargetKind.Cell;
 public ExplorationConsumeRule consumeRule=ExplorationConsumeRule.Discard;
 public ExplorationGrowthTrigger growthTrigger=ExplorationGrowthTrigger.TurnsElapsed;
 [Min(0)] public int duration;
 public ExplorationEffectContent[] effects=Array.Empty<ExplorationEffectContent>();
 public ExplorationStageContent[] stages=Array.Empty<ExplorationStageContent>();
 public string[] tags=Array.Empty<string>();
 public Sprite artwork;
}

[Serializable] public class CardContent {
 public string id,name,text;
 public CardType type;
 public int cost,damage,block,heal,buff,energy,selfDamage;
 public bool exhaust;
 public bool innate,retain,ethereal,unplayable;
 public BattleCardAfterUse afterUse;
 public EffectContent[] effects=Array.Empty<EffectContent>();
 public Sprite artwork;
}

[Serializable] public class RoleContent {
 public string id,name,kind,description,family,milestoneText,maximumMilestoneText;
 public string[] startingCardIds=Array.Empty<string>();
 public ReactionContributionContent[] reactionContributions=Array.Empty<ReactionContributionContent>();
 public ReactionContributionContent[] currentRoleContributions=Array.Empty<ReactionContributionContent>();
 public RoleUnlockRecipeContent[] unlockRecipes=Array.Empty<RoleUnlockRecipeContent>();
 public int maxLevel=10;
 public Sprite artwork;
}

public enum EnemyMoveKind { Attack, Guard, Empower, Disrupt, Special }

[Serializable] public class EnemyMoveContent {
 public string name="攻撃";
 public EnemyMoveKind kind=EnemyMoveKind.Attack;
 public int damage;
 public int block;
 public int heal;
 public EffectContent[] effects=Array.Empty<EffectContent>();
}

[Serializable] public class EnemyPhaseContent {
 public string name="通常";
 [Range(0,100)] public int minimumHpPercent;
 public int[] moveIndices=Array.Empty<int>();
}

public enum EnemyBoardBehavior {
 Patrol,
 Chase,
 Wait
}

[Serializable] public class EnemyContent {
 public string id,name;
 public int tier,hp;
 public BattleFormationScale battleFormation=BattleFormationScale.Normal;
 [Header("Exploration board")]
 public EnemyBoardBehavior boardBehavior=EnemyBoardBehavior.Patrol;
 [Min(1)] public int boardSightRange=4;
 [Min(0)] public int boardMoveSteps=1;
 [Min(1)] public int boardPatrolRadius=3;
 public EnemyMoveContent[] moves=Array.Empty<EnemyMoveContent>();
 public EnemyPhaseContent[] phases=Array.Empty<EnemyPhaseContent>();
 public Sprite portrait;
 public string legacyPortraitResource;
}

[Serializable] public class DungeonContent {
 public string id,name,description;
 public int battles,damage;
 public float hpScale=1f,goldScale=1f;
 public string rewardPoolId="standard";
 public DungeonAreaContent[] areas=Array.Empty<DungeonAreaContent>();
 public Sprite artwork;
}

[Serializable] public class DungeonAreaContent {
 public string id,name,objective;
 [Range(5,12)] public int size=7;
 /// <summary>
 /// Optional authored square mask. '.' is floor, 'S' is the entrance,
 /// '#' is blocking terrain and 'X' is outside the playable fragment.
 /// Keeping the mask square preserves the board coordinate model while the
 /// visible dungeon itself can have any connected silhouette.
 /// </summary>
 public string[] layoutRows=Array.Empty<string>();
 [Min(0)] public int enemyCount=1;
 public string[] enemyIds=Array.Empty<string>();
 [Min(0)] public int eventCount=1;
 public string[] eventIds=Array.Empty<string>();
 [Min(0)] public int lampCount=1;
 /// <summary>
 /// Tier-3 enemy that replaces the ordinary return gate. Defeating it completes
 /// the expedition through the normal boss victory flow.
 /// </summary>
 public string bossEnemyId="";
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
 /// <summary>Added to the default one-cell exploration sight radius.</summary>
 public int explorationSightBonus;
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
 public ReactionContributionContent[] reactionContributions=Array.Empty<ReactionContributionContent>();
}

[Serializable] public class EventEffectContent {
 public EventEffectType effect;
 public int amount;
}

[Serializable] public class EventContent {
 public string id,eyebrow,title,body;
 /// <summary>Allows the event pool to change as dungeon pressure rises.</summary>
 public int minimumDoomTier;
 public int weight=1;
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
 public const int SupportedSchemaVersion=6;
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
 public ReactionValueContent[] reactionValues=>actorContent!=null?actorContent.reactionValues:Array.Empty<ReactionValueContent>();
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
  ValidateIds(value.reactionValues.Select(x=>x.id),"reaction value",result);

  if(value.board.Length!=24)result.errors.Add($"Base storage board has {value.board.Length} cells; expected 24.");
  var itemIds=value.items.Select(x=>x.id).ToHashSet();
  var cardIds=value.cards.Select(x=>x.id).ToHashSet();
  var statusIds=value.statuses.Select(x=>x.id).ToHashSet();
  var eventIds=value.events.Select(x=>x.id).ToHashSet();
  var enemyIds=value.enemies.Select(x=>x.id).ToHashSet();
  var rewardPoolIds=value.rewardPools.Select(x=>x.id).ToHashSet();
  var reactionIds=value.reactionValues.Select(x=>x.id).ToHashSet();

  foreach(var item in value.items){
   ValidateReactions(item.reactionContributions,$"item '{item.id}'",reactionIds,result);
   if(item.cells==null||item.cells.Length==0)result.errors.Add($"Item '{item.id}' has no occupied cells.");
   if(item.acquisitionTier<1)result.errors.Add($"Item '{item.id}' has an invalid acquisition tier.");
   if(item.baseDurability<0)result.errors.Add($"Item '{item.id}' has negative base durability.");
   var grants=item.grantedCards??Array.Empty<GrantedCardContent>();
   if(grants.Length>0){
    foreach(var grant in grants){
     if(grant==null){result.errors.Add($"Item '{item.id}' has a null card grant.");continue;}
     if(grant.count<1)result.errors.Add($"Item '{item.id}' has a card grant with invalid count.");
     if(!cardIds.Contains(grant.battleCardId))
      result.errors.Add($"Item '{item.id}' references missing battle card '{grant.battleCardId}'.");
    }
   } else {
    result.warnings.Add($"Item '{item.id}' still uses the legacy cardIds field.");
    foreach(var cardId in item.cardIds??Array.Empty<string>())
     if(!cardIds.Contains(cardId))result.errors.Add($"Item '{item.id}' references missing card '{cardId}'.");
   }
  }
  foreach(var exploration in value.explorationCards){
   if(exploration.kind==ExplorationCardKind.Installation&&string.IsNullOrWhiteSpace(exploration.place))
    result.errors.Add($"Exploration card '{exploration.id}' has no placement type.");
   if(exploration.cost<0)result.errors.Add($"Exploration card '{exploration.id}' has a negative cost.");
   if(exploration.duration<0)result.errors.Add($"Exploration card '{exploration.id}' has a negative duration.");
   var stages=exploration.stages??Array.Empty<ExplorationStageContent>();
   if(stages.Length>0){
    if(stages.Any(stage=>stage==null))
     result.errors.Add($"Exploration card '{exploration.id}' has a null stage.");
    var validStages=stages.Where(stage=>stage!=null).ToArray();
    ValidateIds(validStages.Select(stage=>stage.id),$"stage in exploration card '{exploration.id}'",result);
    if(validStages.Any(stage=>stage.minimumProgress<0))
     result.errors.Add($"Exploration card '{exploration.id}' has a stage with negative progress.");
    if(validStages.GroupBy(stage=>stage.minimumProgress).Any(group=>group.Count()>1))
     result.errors.Add($"Exploration card '{exploration.id}' has duplicate stage progress thresholds.");
    if(validStages.Length>0&&validStages.Min(stage=>stage.minimumProgress)!=0)
     result.errors.Add($"Exploration card '{exploration.id}' must begin with a stage at progress 0.");
   }
  }
  foreach(var consumable in value.consumables)
   if(consumable.amount<0)result.errors.Add($"Consumable '{consumable.id}' has a negative amount.");
  foreach(var card in value.cards){
   ValidateEffects(card.effects,$"card '{card.id}'",statusIds,result);
   if(card.exhaust&&card.afterUse==BattleCardAfterUse.RemoveExpedition)
    result.warnings.Add($"Card '{card.id}' uses legacy exhaust together with RemoveExpedition.");
  }
  foreach(var role in value.roles){
   ValidateReactions(role.reactionContributions,$"role '{role.id}'",reactionIds,result);
   ValidateReactions(role.currentRoleContributions,$"current role '{role.id}'",reactionIds,result);
   foreach(var cardId in role.startingCardIds??Array.Empty<string>())
    if(!cardIds.Contains(cardId))result.errors.Add($"Role '{role.id}' references missing starting card '{cardId}'.");
   var recipeIds=new HashSet<string>();
   foreach(var recipe in role.unlockRecipes??Array.Empty<RoleUnlockRecipeContent>()){
    if(recipe==null){result.errors.Add($"Role '{role.id}' has a null unlock recipe.");continue;}
    if(string.IsNullOrWhiteSpace(recipe.id)||!recipeIds.Add(recipe.id))
     result.errors.Add($"Role '{role.id}' has an empty or duplicate unlock recipe id '{recipe.id}'.");
    if(recipe.requirements==null||recipe.requirements.Length==0)
     result.errors.Add($"Role '{role.id}' unlock recipe '{recipe.id}' has no requirements.");
    foreach(var requirement in recipe.requirements??Array.Empty<ReactionRequirementContent>()){
     if(requirement==null){result.errors.Add($"Role '{role.id}' recipe '{recipe.id}' has a null requirement.");continue;}
     if(!reactionIds.Contains(requirement.reactionId))
      result.errors.Add($"Role '{role.id}' recipe '{recipe.id}' references missing reaction '{requirement.reactionId}'.");
     if(requirement.minimum<1)
      result.errors.Add($"Role '{role.id}' recipe '{recipe.id}' has a non-positive requirement.");
     if(requirement.acceptedScopes==ReactionScopeMask.None)
      result.errors.Add($"Role '{role.id}' recipe '{recipe.id}' accepts no reaction scopes.");
    }
   }
  }
  foreach(var character in value.characters){
   if(character.activeSkillKind==CharacterSkillKind.None)
    result.errors.Add($"Character '{character.id}' has no active skill kind.");
   if(character.activeSkillAmount<0)
    result.errors.Add($"Character '{character.id}' has a negative active skill amount.");
  }
  foreach(var enemy in value.enemies){
   if(enemy.moves==null||enemy.moves.Length==0)
    result.errors.Add($"Enemy '{enemy.id}' has no moves.");
   foreach(var move in enemy.moves??Array.Empty<EnemyMoveContent>()){
    if(string.IsNullOrWhiteSpace(move.name))
     result.errors.Add($"Enemy '{enemy.id}' has an unnamed move.");
   if(move.damage<0||move.block<0||move.heal<0)
     result.errors.Add($"Enemy '{enemy.id}' move '{move.name}' has a negative value.");
    ValidateEffects(move.effects,$"enemy '{enemy.id}'",statusIds,result);
   }
   foreach(var phase in enemy.phases??Array.Empty<EnemyPhaseContent>()){
    if(string.IsNullOrWhiteSpace(phase.name))
     result.errors.Add($"Enemy '{enemy.id}' has an unnamed phase.");
    if(phase.minimumHpPercent<0||phase.minimumHpPercent>100)
     result.errors.Add($"Enemy '{enemy.id}' phase '{phase.name}' has an invalid HP threshold.");
    if(phase.moveIndices==null||phase.moveIndices.Length==0)
     result.errors.Add($"Enemy '{enemy.id}' phase '{phase.name}' has no moves.");
    else if(phase.moveIndices.Any(index=>index<0||index>=enemy.moves.Length))
     result.errors.Add($"Enemy '{enemy.id}' phase '{phase.name}' references an invalid move.");
   }
  }
  foreach(var dungeon in value.dungeons){
   if(!string.IsNullOrEmpty(dungeon.rewardPoolId)&&!rewardPoolIds.Contains(dungeon.rewardPoolId))
    result.errors.Add($"Dungeon '{dungeon.id}' references missing reward pool '{dungeon.rewardPoolId}'.");
   var areaIds=new HashSet<string>();
   foreach(var area in dungeon.areas??Array.Empty<DungeonAreaContent>()){
    if(area==null){result.errors.Add($"Dungeon '{dungeon.id}' has a null area.");continue;}
    if(string.IsNullOrWhiteSpace(area.id)||!areaIds.Add(area.id))
     result.errors.Add($"Dungeon '{dungeon.id}' has an empty or duplicate area id '{area.id}'.");
    if(area.size<5||area.size>12)
     result.errors.Add($"Dungeon '{dungeon.id}' area '{area.id}' has invalid size {area.size}.");
    if(area.layoutRows!=null&&area.layoutRows.Length>0){
     if(area.layoutRows.Length!=area.size||area.layoutRows.Any(row=>row==null||row.Length!=area.size))
      result.errors.Add($"Dungeon '{dungeon.id}' area '{area.id}' layout must be {area.size} rows of {area.size} cells.");
     else{
      const string allowedLayoutCells=".S#X";
      if(area.layoutRows.SelectMany(row=>row).Any(cell=>!allowedLayoutCells.Contains(cell)))
       result.errors.Add($"Dungeon '{dungeon.id}' area '{area.id}' layout contains an unsupported cell.");
      if(area.layoutRows.Sum(row=>row.Count(cell=>cell=='S'))!=1)
       result.errors.Add($"Dungeon '{dungeon.id}' area '{area.id}' layout must contain exactly one entrance 'S'.");
     }
    }
    foreach(var enemyId in area.enemyIds??Array.Empty<string>())
     if(!enemyIds.Contains(enemyId))
      result.errors.Add($"Dungeon '{dungeon.id}' area '{area.id}' references missing enemy '{enemyId}'.");
    foreach(var eventId in area.eventIds??Array.Empty<string>())
     if(!eventIds.Contains(eventId))
      result.errors.Add($"Dungeon '{dungeon.id}' area '{area.id}' references missing event '{eventId}'.");
    if(!string.IsNullOrEmpty(area.bossEnemyId)){
     var boss=value.enemies.FirstOrDefault(enemy=>enemy.id==area.bossEnemyId);
     if(boss==null)result.errors.Add($"Dungeon '{dungeon.id}' area '{area.id}' references missing boss '{area.bossEnemyId}'.");
     else if(boss.tier<3)result.warnings.Add($"Dungeon '{dungeon.id}' area '{area.id}' boss '{area.bossEnemyId}' is below tier 3 and will not finish the run.");
    }
   }
  }
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
   else {
    ValidateIds(eventContent.choices.Select(x=>x.id),$"event choice in '{eventContent.id}'",result);
    foreach(var choice in eventContent.choices)
     ValidateReactions(choice.reactionContributions,$"event '{eventContent.id}' choice '{choice.id}'",reactionIds,result);
   }
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
   value.actorContent.reactionValues??=Array.Empty<ReactionValueContent>();
   value.actorContent.roles??=Array.Empty<RoleContent>();
   value.actorContent.enemies??=Array.Empty<EnemyContent>();
   value.actorContent.factions??=Array.Empty<FactionContent>();
   value.actorContent.characters??=Array.Empty<CharacterContent>();
  }
  if(value.worldContent!=null){
   value.worldContent.dungeons??=Array.Empty<DungeonContent>();
   foreach(var dungeon in value.worldContent.dungeons){
    if(dungeon==null)continue;
    dungeon.areas??=Array.Empty<DungeonAreaContent>();
    foreach(var area in dungeon.areas){
     if(area==null)continue;
     area.layoutRows??=Array.Empty<string>();
     area.enemyIds??=Array.Empty<string>();
     area.eventIds??=Array.Empty<string>();
    }
   }
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

 static void ValidateReactions(IEnumerable<ReactionContributionContent> contributions,string owner,
  HashSet<string> reactionIds,ContentValidationReport result){
  foreach(var contribution in contributions??Enumerable.Empty<ReactionContributionContent>()){
   if(contribution==null){result.errors.Add($"{owner} has a null reaction contribution.");continue;}
   if(!reactionIds.Contains(contribution.reactionId))
    result.errors.Add($"{owner} references missing reaction '{contribution.reactionId}'.");
   if(contribution.amount==0)result.warnings.Add($"{owner} contributes zero '{contribution.reactionId}'.");
  }
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
