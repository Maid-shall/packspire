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
  var item=new ItemDef(value.id,value.name,value.type,ids.FirstOrDefault()??"",value.description,cells){
   cardIds=ids.ToArray(),
   cardId=ids.FirstOrDefault()??"",
   linkRule=value.linkRule??"",
   explorationCardId=value.explorationCardId,
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
  artwork=value.artwork
 };

 static ExplorationCardDef ToExplorationCard(ExplorationCardContent value)=>new(){
  id=value.id,name=value.name,text=value.text,place=value.place,cost=value.cost,artwork=value.artwork
 };

 static RoleDef ToRole(RoleContent value)=>new(value.id,value.name,value.kind,value.description,value.maxLevel){
  artwork=value.artwork,
  family=value.family,
  milestoneText=value.milestoneText,
  maximumMilestoneText=value.maximumMilestoneText,
  startingCardIds=(value.startingCardIds??Array.Empty<string>()).ToArray()
 };

 static EnemyDef ToEnemy(EnemyContent value){
  var enemy=new EnemyDef(value.id,value.name,value.tier,value.hp,(value.moves??Array.Empty<EnemyMoveContent>()).Select(x=>x.damage).ToArray()){
   portraitAsset=value.portrait
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
