using System;
using System.Collections.Generic;
using System.Linq;

namespace Packspire {
/// <summary>
/// Compatibility facade over the ScriptableObject content database. Existing
/// systems keep their stable API while authored data stays Inspector-editable.
/// </summary>
public static class GameCatalog {
 static readonly PackspireContentDatabase Source=PackspireContent.Data;

 public static readonly Element[] Board=Source.board.ToArray();
 public static readonly Dictionary<string,ItemDef> Items=Source.items.Select(ToItem).ToDictionary(x=>x.id);
 public static readonly Dictionary<string,CardDef> Cards=Source.cards.Select(ToCard).ToDictionary(x=>x.id);
 public static readonly Dictionary<string,ExplorationCardDef> ExplorationCards=Source.explorationCards.Select(ToExplorationCard).ToDictionary(x=>x.id);
 public static readonly Dictionary<string,RoleDef> Roles=Source.roles.Select(ToRole).ToDictionary(x=>x.id);
 public static readonly EnemyDef[] Enemies=Source.enemies.Select(ToEnemy).ToArray();
 public static readonly DungeonDef[] Dungeons=Source.dungeons.Select(ToDungeon).ToArray();
 public static readonly BackpackDef[] Backpacks=Source.backpacks.Select(ToBackpack).ToArray();
 public static readonly FactionDef[] Factions=Source.factions.Select(ToFaction).ToArray();

 static ItemDef ToItem(ItemContent value){
  var cells=(value.cells??Array.Empty<CellContent>())
   .Select(x=>new CellDef(x.x,x.y,x.element,Math.Max(1,x.value)))
   .ToArray();
  var ids=value.cardIds??Array.Empty<string>();
  var grants=(value.grantedCards??Array.Empty<GrantedCardContent>())
   .Where(x=>x!=null&&!string.IsNullOrEmpty(x.battleCardId))
   .Select(x=>new GrantedCardDef{
    battleCardId=x.battleCardId,
    count=Math.Max(1,x.count)
   })
   .ToArray();
  if(grants.Length==0)
   grants=ids.Select(id=>new GrantedCardDef{
    battleCardId=id,count=1
   }).ToArray();
  var normalizedIds=ids.Length>0
   ?ids.ToArray()
   :grants.SelectMany(grant=>Enumerable.Repeat(grant.battleCardId,Math.Max(1,grant.count))).ToArray();
  var item=new ItemDef(value.id,value.name,value.type,normalizedIds.FirstOrDefault()??"",value.description,cells){
   cardIds=normalizedIds,
   cardId=normalizedIds.FirstOrDefault()??"",
   linkRule=value.linkRule??"",
   sealAttribute=value.sealAttribute,
   grantedCards=grants,
   rarity=value.rarity,
   acquisitionTier=Math.Max(1,value.acquisitionTier),
   baseDurability=Math.Max(0,value.baseDurability),
   resonanceTags=(value.resonanceTags??Array.Empty<string>()).ToArray(),
   reactionContributions=(value.reactionContributions??Array.Empty<ReactionContributionContent>()).ToArray(),
   artwork=value.artwork
  };
  return item;
 }

 static CardDef ToCard(CardContent value)=>new(value.id,value.name,value.type,value.cost,value.text){
  damage=value.damage,
  block=value.block,
  heal=value.heal,
  buff=value.buff,
  energy=value.energy,
  selfDamage=value.selfDamage,
  exhaust=value.exhaust,
  innate=value.innate,
  retain=value.retain,
  ethereal=value.ethereal,
  unplayable=value.unplayable,
  afterUse=value.exhaust&&value.afterUse==BattleCardAfterUse.Discard
   ?BattleCardAfterUse.ExhaustBattle:value.afterUse,
  artwork=value.artwork
 };

 static ExplorationCardDef ToExplorationCard(ExplorationCardContent value)=>new(){
  id=value.id,name=value.name,text=value.text,place=value.place,cost=value.cost,duration=value.duration,
  kind=value.kind,target=value.target,consumeRule=value.consumeRule,growthTrigger=value.growthTrigger,
  effects=(value.effects??Array.Empty<ExplorationEffectContent>()).ToArray(),
  stages=(value.stages??Array.Empty<ExplorationStageContent>()).Where(stage=>stage!=null).Select(stage=>new ExplorationStageDef{
   id=stage.id,name=stage.name,text=stage.text,minimumProgress=stage.minimumProgress,
   passiveEffects=(stage.passiveEffects??Array.Empty<ExplorationEffectContent>()).ToArray(),
   onEnterEffects=(stage.onEnterEffects??Array.Empty<ExplorationEffectContent>()).ToArray(),
   onTurnEffects=(stage.onTurnEffects??Array.Empty<ExplorationEffectContent>()).ToArray(),
   onMatureEffects=(stage.onMatureEffects??Array.Empty<ExplorationEffectContent>()).ToArray(),
   artwork=stage.artwork,boardArtwork=stage.boardArtwork
  }).OrderBy(stage=>stage.minimumProgress).ToArray(),
  tags=(value.tags??Array.Empty<string>()).ToArray(),
  artwork=value.artwork
 };

 static RoleDef ToRole(RoleContent value)=>new(value.id,value.name,value.kind,value.description,value.maxLevel){
  artwork=value.artwork,
  family=value.family,
  milestoneText=value.milestoneText,
  maximumMilestoneText=value.maximumMilestoneText,
  startingCardIds=(value.startingCardIds??Array.Empty<string>()).ToArray(),
  reactionContributions=(value.reactionContributions??Array.Empty<ReactionContributionContent>()).ToArray(),
  currentRoleContributions=(value.currentRoleContributions??Array.Empty<ReactionContributionContent>()).ToArray(),
  unlockRecipes=(value.unlockRecipes??Array.Empty<RoleUnlockRecipeContent>()).ToArray()
 };

 static EnemyDef ToEnemy(EnemyContent value){
 var enemy=new EnemyDef(value.id,value.name,value.tier,value.hp,(value.moves??Array.Empty<EnemyMoveContent>()).Select(x=>x.damage).ToArray()){
   portraitAsset=value.portrait,
   battleFormation=value.battleFormation,
   boardBehavior=value.boardBehavior,
   boardSightRange=Math.Max(1,value.boardSightRange),
   boardMoveSteps=Math.Max(0,value.boardMoveSteps),
   boardPatrolRadius=Math.Max(1,value.boardPatrolRadius)
  };
  if(!string.IsNullOrEmpty(value.legacyPortraitResource))enemy.WithPortrait(value.legacyPortraitResource);
  return enemy;
 }

 static DungeonDef ToDungeon(DungeonContent value)=>
  new(value.id,value.name,value.description,value.battles,value.hpScale,value.damage,value.goldScale){artwork=value.artwork};

 static BackpackDef ToBackpack(BackpackContent value)=>
  new(value.id,value.name,value.description,value.safeCells??Array.Empty<int>()){artwork=value.artwork};

 static FactionDef ToFaction(FactionContent value)=>
  new(value.id,value.name,value.description,value.ranks??Array.Empty<string>()){artwork=value.artwork};

 public static T Find<T>(IEnumerable<T> set,Func<T,string> id,string value)=>set.First(x=>id(x)==value);
}
}
