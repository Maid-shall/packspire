#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Packspire;
using UnityEditor;

public sealed class PackspireEditModeTests {
 [Test]
 public void AuthoredContent_IsValid(){
  var database=AssetDatabase.LoadAssetAtPath<PackspireContentDatabase>(
   "Assets/Resources/Packspire/PackspireContentDatabase.asset");
  var report=PackspireContent.Validate(database);
  Assert.That(report.errors,Is.Empty,string.Join("\n",report.errors));
 }

 [Test]
 public void LegacySave_MigratesToCurrentSchema(){
  var save=SaveSystem.Import("{\"version\":1,\"selectedBackpack\":\"standard\"}");
  Assert.That(save.version,Is.EqualTo(SaveSystem.CurrentVersion));
  Assert.That(save.loadouts,Has.Count.EqualTo(3));
  Assert.That(save.selectedLoadoutId,Is.EqualTo("loadout-1"));
  Assert.That(save.selectedCharacterId,Is.Not.Empty);
 }

 [Test]
 public void CombatStatusMath_AppliesWeakAndVulnerable(){
  var attacker=new List<StatusState>{new(){type="weak",amount=1}};
  var defender=new List<StatusState>{new(){type="vulnerable",amount=1}};
  Assert.That(BattleSystem.Damage(12,attacker,defender),Is.EqualTo(14));
 }

 [Test]
 public void GridBoard_UsesAuthoredBalance(){
  var board=GridBoardSystem.Create(PackspireContent.Data.balance.defaultDungeonId);
  Assert.That(board.energyMax,Is.EqualTo(PackspireContent.Data.balance.baseEnergy));
  Assert.That(board.doomMax,Is.EqualTo(PackspireContent.Data.balance.gridDoomMax));
  Assert.That(board.cells,Is.Not.Empty);
 }

 [Test]
 public void GridBoard_PathPreviewDoesNotCommitPlayerPosition(){
  var board=GridBoardSystem.Create(PackspireContent.Data.balance.defaultDungeonId);
  var committed=board.piece;
  GridBoardSystem.BeginPathPhase(board);
  var directions=new[]{
   UnityEngine.Vector2Int.up,UnityEngine.Vector2Int.right,
   UnityEngine.Vector2Int.down,UnityEngine.Vector2Int.left
  };
  var direction=System.Array.Find(directions,value=>GridBoardSystem.CanSlide(board,value));
  Assert.That(direction,Is.Not.EqualTo(UnityEngine.Vector2Int.zero),"No slideable test direction");
  Assert.That(GridBoardSystem.TrySlide(board,direction,out _),Is.True);
  Assert.That(board.path.Count,Is.GreaterThan(1));
  Assert.That(board.piece,Is.EqualTo(committed),"Drawing a route must not move the committed actor");
  Assert.That(GridBoardSystem.UndoSegment(board,out _),Is.True);
  Assert.That(board.piece,Is.EqualTo(committed),"Undoing preview must not move the committed actor");
  GridBoardSystem.ClearPath(board);
  Assert.That(board.piece,Is.EqualTo(committed),"Cancelling preview must preserve the committed actor");
 }

 [Test]
 public void GridBoard_GenerationIsSeededIrregularAndTraversable(){
  const int seed=730241;
  var first=GridBoardSystem.Create(PackspireContent.Data.balance.defaultDungeonId,seed);
  var second=GridBoardSystem.Create(PackspireContent.Data.balance.defaultDungeonId,seed);
  string FirstSignature(GridBoardRunState board)=>string.Join("|",board.cells
   .OrderBy(cell=>cell.y).ThenBy(cell=>cell.x)
   .Select(cell=>$"{cell.x},{cell.y}:{cell.terrain}:{cell.place}:{cell.contentId}"))+
   "#" + string.Join("|",board.enemies.OrderBy(enemy=>enemy.uid)
    .Select(enemy=>$"{enemy.uid}:{enemy.contentId}:{enemy.x},{enemy.y}:{enemy.behavior}"));
  Assert.That(FirstSignature(second),Is.EqualTo(FirstSignature(first)));
  Assert.That(first.cells.Count(cell=>cell.terrain=="void"),Is.GreaterThanOrEqualTo(8));
  Assert.That(first.cells.Any(cell=>cell.terrain=="void"&&cell.x>0&&cell.y>0&&
   cell.x<first.size-1&&cell.y<first.size-1),Is.True,"Board needs a real interior hole");
  Assert.That(GridBoardSystem.IsAreaTraversable(first),Is.True);
  Assert.That(first.cells.Count(cell=>cell.place=="return"),Is.EqualTo(1));
  Assert.That(first.cells.Count(cell=>cell.place=="next"),Is.EqualTo(1));
  Assert.That(first.cells.Count(cell=>cell.place=="calamity"),Is.EqualTo(1));
  Assert.That(first.cells.Single(cell=>cell.place=="calamity").terrain,Is.EqualTo("blocked"));
  Assert.That(first.cells.All(cell=>cell.place!="enemy"),Is.True);
  Assert.That(first.enemies,Has.Count.EqualTo(1));
  var spawned=first.enemies.Single();
  var authored=GameCatalog.Enemies.Single(enemy=>enemy.id==spawned.contentId);
  Assert.That(spawned.behavior,Is.EqualTo(authored.boardBehavior switch{
   EnemyBoardBehavior.Chase=>"chase",
   EnemyBoardBehavior.Wait=>"wait",
   _=>"patrol"
  }));
  Assert.That(spawned.sightRange,Is.EqualTo(authored.boardSightRange));
  Assert.That(spawned.moveSteps,Is.EqualTo(authored.boardMoveSteps));
  Assert.That(spawned.patrolRadius,Is.EqualTo(authored.boardPatrolRadius));
 }

 [Test]
 public void GridBoard_CommittedRouteResolvesExactlyOneExplorationTurn(){
  var board=GridBoardSystem.Create(PackspireContent.Data.balance.defaultDungeonId,9157);
  GridBoardSystem.BeginPathPhase(board);
  var directions=new[]{
   UnityEngine.Vector2Int.up,UnityEngine.Vector2Int.right,
   UnityEngine.Vector2Int.down,UnityEngine.Vector2Int.left
  };
  var direction=System.Array.Find(directions,value=>GridBoardSystem.CanSlide(board,value));
  Assert.That(direction,Is.Not.EqualTo(UnityEngine.Vector2Int.zero),"No slideable test direction");
  Assert.That(GridBoardSystem.TrySlide(board,direction,out _),Is.True);
  board.enemies.Clear();
  foreach(var position in board.path.Skip(1)){
   var cell=GridBoardSystem.Cell(board,position.x,position.y);
   if(cell!=null)cell.place="empty";
  }
  Assert.That(board.explorationTurn,Is.Zero,"Preview is not a turn");
  Assert.That(GridBoardSystem.BeginRun(board,out _),Is.True);
  for(int i=0;i<board.path.Count*3&&board.phase==GridBoardPhase.Run;i++)
   GridBoardSystem.TickRun(board,10f);
  Assert.That(board.phase,Is.EqualTo(GridBoardPhase.Place));
  Assert.That(board.explorationTurn,Is.EqualTo(1));
  Assert.That(board.areaTurn,Is.EqualTo(1));
  Assert.That(board.explorationTurnPending,Is.False);
  Assert.That(GridBoardSystem.ResolveExplorationTurn(board),Is.Null,"Resolved route cannot tick twice");
  Assert.That(board.explorationTurn,Is.EqualTo(1));
 }

 [Test]
 public void GridBoard_SightStartsAtOneCellAndSupportsBonuses(){
  var board=GridBoardSystem.Create(PackspireContent.Data.balance.defaultDungeonId,44019);
  var start=board.piece;
  foreach(var cell in board.cells){
   int distance=System.Math.Max(
    System.Math.Abs(cell.x-start.x),
    System.Math.Abs(cell.y-start.y));
   Assert.That(cell.discovered,Is.EqualTo(distance<=1),
    $"Unexpected initial discovery at {cell.x},{cell.y}");
  }

  board.sightRangeBonus=1;
  GridBoardSystem.RevealAround(board,start);
  Assert.That(GridBoardSystem.Cell(board,start.x+2,start.y).discovered,Is.True);
  Assert.That(GridBoardSystem.EffectiveSightRange(board),Is.EqualTo(2));
 }

 [Test]
 public void GridBoard_WalkingRevealsSightAlongTheCommittedRoute(){
  var board=GridBoardSystem.Create(PackspireContent.Data.balance.defaultDungeonId,88231);
  var hiddenAhead=GridBoardSystem.Cell(board,2,board.piece.y);
  Assert.That(hiddenAhead,Is.Not.Null);
  Assert.That(hiddenAhead.discovered,Is.False);

  GridBoardSystem.BeginPathPhase(board);
  Assert.That(GridBoardSystem.TrySlide(board,UnityEngine.Vector2Int.right,out _),Is.True);
  Assert.That(board.path.Count,Is.GreaterThanOrEqualTo(3));
  board.enemies.Clear();
  foreach(var position in board.path.Skip(1)){
   var cell=GridBoardSystem.Cell(board,position.x,position.y);
   if(cell!=null)cell.place="empty";
  }
  Assert.That(GridBoardSystem.BeginRun(board,out _),Is.True);
  GridBoardSystem.TickRun(board,10f);
  GridBoardSystem.TickRun(board,10f);

  Assert.That(board.piece.x,Is.EqualTo(1));
  Assert.That(hiddenAhead.discovered,Is.True,
   "Arriving at the first route cell must reveal the next one-cell sight ring");
  Assert.That(GridBoardSystem.IsCurrentlyVisible(board,hiddenAhead),Is.True);
 }

 [Test]
 public void GridBoard_ChasingEnemyMovesAndEngagesAsAnIndependentEntity(){
  var board=GridBoardSystem.Create(PackspireContent.Data.balance.defaultDungeonId,27101);
  board.enemies.Clear();
  int y=board.piece.y;
  var adjacent=GridBoardSystem.Cell(board,1,y);
  adjacent.terrain="floor";
  adjacent.place="empty";
  board.enemies.Add(new GridEnemyState{
   uid="test-chaser",contentId="dragon",x=1,y=y,previousX=1,previousY=y,behavior="chase"
  });

  int moved=GridBoardSystem.AdvanceEnemies(board,out bool engaged);

  Assert.That(moved,Is.EqualTo(1));
  Assert.That(engaged,Is.True);
  Assert.That(board.pendingBattle,Is.True);
  Assert.That(board.pendingEnemyId,Is.EqualTo("dragon"));
  Assert.That(board.enemies,Is.Empty);
  Assert.That(board.cells.All(cell=>cell.place!="enemy"),Is.True);
 }

 [Test]
 public void GridBoard_MatureLampRevealsAndMatureFogBlocksHostiles(){
  var board=GridBoardSystem.Create(PackspireContent.Data.balance.defaultDungeonId,67114);
  board.enemies.Clear();
  int threshold=PackspireContent.Data.balance.gridGrowthThreshold;
  var lamp=board.cells.First(cell=>cell.terrain=="floor"&&cell.place=="empty"&&
   cell.x>0&&cell.y>0&&cell.x<board.size-1&&cell.y<board.size-1&&
   System.Math.Max(System.Math.Abs(cell.x-board.piece.x),
    System.Math.Abs(cell.y-board.piece.y))>=3);
  var revealed=GridBoardSystem.Cell(board,lamp.x+1,lamp.y+1);
  lamp.place="lamp";
  lamp.grow=threshold;
  revealed.discovered=false;
  board.explorationTurnPending=true;
  GridBoardSystem.ResolveExplorationTurn(board);
  Assert.That(revealed.discovered,Is.True,"A mature lamp should maintain a larger revealed area");
  Assert.That(board.cells.Single(cell=>cell.place=="calamity").grow,Is.EqualTo(board.doom),
   "The world facility should visibly track the same pressure that strengthens enemies");

  int y=board.piece.y;
  var fog=GridBoardSystem.Cell(board,1,y);
  fog.terrain="floor";
  fog.place="fog";
  fog.grow=threshold;
  board.enemies.Add(new GridEnemyState{
   uid="fog-blocked",contentId="dragon",x=2,y=y,previousX=2,previousY=y,behavior="chase"
  });
  GridBoardSystem.AdvanceEnemies(board,out _);
  Assert.That(board.enemies.Single().Position,Is.Not.EqualTo(new UnityEngine.Vector2Int(1,y)));
  Assert.That(board.pendingBattle,Is.False);
 }

 [Test]
 public void ResourceCache_ReusesLoadedMasterAsset(){
  PackspireResources.ClearCacheForTests();
  var first=PackspireResources.Load<PackspireContentDatabase>("Packspire/PackspireContentDatabase");
  var second=PackspireResources.LoadFirst<PackspireContentDatabase>(
   "Packspire/does-not-exist",
   "Packspire/PackspireContentDatabase");
  Assert.That(first,Is.Not.Null);
  Assert.That(second,Is.SameAs(first));
 }
}
#endif
