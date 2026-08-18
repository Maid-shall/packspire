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
  Assert.That(save.memoryReactions,Is.Not.Null);
  Assert.That(save.roleBranches,Has.Count.EqualTo(4));
  Assert.That(RoleFrameworkSystem.CoreRoleIds,Does.Contain(save.currentRole));
 }

 [Test]
 public void LegacyAdvancedRole_BecomesOneQualificationSeal(){
  var advanced=GameCatalog.Roles.Values.First(role=>
   !RoleFrameworkSystem.CoreRoleIds.Contains(role.id)&&RoleFrameworkSystem.CoreRoleIds.Contains(role.family));
  var save=new MetaSave{currentRole=advanced.id,unlockedRoles=new(){advanced.id}};

  RoleFrameworkSystem.MigrateLegacyRole(save);
  RoleFrameworkSystem.Normalize(save);

  Assert.That(save.currentRole,Is.EqualTo(advanced.family));
  Assert.That(save.qualificationSealId,Is.EqualTo(advanced.id));
 }

 [Test]
 public void CourierRoute_HasTenSegmentsWithTwoMeaningfulBranchesPerPhase(){
  var meta=new MetaSave{currentRole="scout"};
  RoleFrameworkSystem.Normalize(meta);
  var run=new RunState{role="scout"};
  run.courierRoute=CourierRouteSystem.Create(run,meta);

  var available=CourierRouteSystem.Available(run.courierRoute);

  Assert.That(available.Count,Is.EqualTo(2));
  Assert.That(available.Select(x=>x.kind),Is.EquivalentTo(new[]{"RELAY","EVENT"}));
  Assert.That(Enumerable.Range(1,9).All(phase=>CourierRouteSystem.Nodes.Count(node=>node.phase==phase)==2),Is.True);
  Assert.That(CourierRouteSystem.Node("destination").phase,Is.EqualTo(CourierRouteSystem.TotalSegments));
  Assert.That(run.courierRoute.seals.Any(x=>x.roleSignature&&x.name=="先読印"),Is.True);
 }

 [Test]
 public void CourierRoute_SealAndCommitAdvancePressureDeterministically(){
  var meta=new MetaSave{currentRole="scout"};
  RoleFrameworkSystem.Normalize(meta);
  var run=new RunState{role="scout"};
  run.courierRoute=CourierRouteSystem.Create(run,meta);

  Assert.That(CourierRouteSystem.UseSeal(run.courierRoute,"role-signature",out _),Is.True);
  Assert.That(CourierRouteSystem.Select(run.courierRoute,"broken_stair"),Is.True);
  Assert.That(CourierRouteSystem.Commit(run,out _),Is.True);

  Assert.That(run.courierRoute.currentNodeId,Is.EqualTo("broken_stair"));
  Assert.That(run.courierRoute.daysElapsed,Is.EqualTo(0));
  Assert.That(run.courierRoute.travelPending,Is.True);
  Assert.That(run.courierRoute.travelFromNodeId,Is.EqualTo("dispatch"));
  Assert.That(run.courierRoute.travelToNodeId,Is.EqualTo("broken_stair"));
  Assert.That(CourierRouteSystem.CompleteTravel(run.courierRoute),Is.True);
  Assert.That(run.hp,Is.EqualTo(42));
  Assert.That(run.courierRoute.awaitingResolution,Is.True);
  Assert.That(run.courierRoute.pendingResolution,Is.EqualTo(CourierResolutionKind.Event));
  Assert.That(CourierRouteSystem.Available(run.courierRoute),Is.Empty,
   "A location must resolve through its own handler before another route branch can be chosen.");
  Assert.That(CourierRouteSystem.ResolveCurrent(run,new CourierLocationOutcome(),out _),Is.True);
  Assert.That(run.courierRoute.awaitingResolution,Is.False);
  Assert.That(run.courierRoute.travelPending,Is.False);
  Assert.That(run.courierRoute.travelFromNodeId,Is.Empty);
  Assert.That(run.courierRoute.travelToNodeId,Is.Empty);
 }

 [Test]
 public void CourierRoute_RequiresTenResolvedLocationsBeforeDeliveryCompletes(){
  var meta=new MetaSave{currentRole="guardian"};
  RoleFrameworkSystem.Normalize(meta);
  var run=new RunState{role="guardian",hp=42,maxHp=42};
  run.courierRoute=CourierRouteSystem.Create(run,meta);
  string[] route={
   "ash_market","quiet_shaft","cinder_tunnel","reliquary_post","drowned_archive",
   "ash_hospital","glass_aqueduct","courier_catacomb","final_relay","destination"
  };

  foreach(string nodeId in route){
   Assert.That(CourierRouteSystem.Select(run.courierRoute,nodeId),Is.True,nodeId);
   Assert.That(CourierRouteSystem.Commit(run,out _),Is.True,nodeId);
   Assert.That(run.courierRoute.complete,Is.False,"Arrival alone is not a completed delivery.");
   Assert.That(CourierRouteSystem.ResolveCurrent(run,new CourierLocationOutcome(),out _),Is.True,nodeId);
  }

  Assert.That(run.courierRoute.clearedSegments,Is.EqualTo(CourierRouteSystem.TotalSegments));
  Assert.That(run.courierRoute.complete,Is.True);
  Assert.That(run.courierRoute.resolvedNodeIds,Is.EquivalentTo(route));
 }

 [Test]
 public void CourierRoute_SeparatesLocationRolesAndReservesMinigameExtension(){
  Assert.That(CourierRouteSystem.Node("broken_stair").resolution,Is.EqualTo(CourierResolutionKind.Event));
  Assert.That(CourierRouteSystem.Node("cinder_tunnel").resolution,Is.EqualTo(CourierResolutionKind.Battle));
  Assert.That(CourierRouteSystem.Node("quiet_shaft").resolution,Is.EqualTo(CourierResolutionKind.Cargo));
  Assert.That(CourierRouteSystem.Node("ash_market").resolution,Is.EqualTo(CourierResolutionKind.Relay));
  Assert.That(CourierRouteSystem.Node("destination").resolution,Is.EqualTo(CourierResolutionKind.Delivery));
  Assert.That(CourierRouteSystem.Nodes.Any(node=>node.resolution==CourierResolutionKind.MiniGame),Is.False,
   "Minigame is an extension contract only; no route location should require it yet.");
 }

 [Test]
 public void CourierRoute_LocationOutcomeCanChangeDeliveryPressure(){
  var meta=new MetaSave{currentRole="scout"};
  RoleFrameworkSystem.Normalize(meta);
  var run=new RunState{role="scout",hp=42,maxHp=42};
  run.courierRoute=CourierRouteSystem.Create(run,meta);
  CourierRouteSystem.Select(run.courierRoute,"ash_market");
  CourierRouteSystem.Commit(run,out _);

  CourierRouteSystem.ResolveCurrent(run,new CourierLocationOutcome{dayDelta=2},out _);

  Assert.That(run.courierRoute.daysElapsed,Is.EqualTo(4));
 }

 [Test]
 public void CourierRoute_CargoOutcomeBuildsDeliveryProof(){
  var meta=new MetaSave{currentRole="scout"};
  RoleFrameworkSystem.Normalize(meta);
  var run=new RunState{role="scout",hp=42,maxHp=42};
  run.courierRoute=CourierRouteSystem.Create(run,meta);
  CourierRouteSystem.Select(run.courierRoute,"ash_market");
  CourierRouteSystem.Commit(run,out _);
  CourierRouteSystem.ResolveCurrent(run,new CourierLocationOutcome(),out _);
  CourierRouteSystem.Select(run.courierRoute,"quiet_shaft");
  CourierRouteSystem.Commit(run,out _);
  run.lootBag.Add(new ItemInstance("sword"));
  CourierRouteSystem.ResolveCurrent(run,new CourierLocationOutcome{cargoRecovered=true},out _);

  Assert.That(run.courierRoute.recoveredCargoCount,Is.EqualTo(1));
  Assert.That(run.lootBag,Has.Count.EqualTo(1));
 }

 [Test]
 public void CourierRoute_RecoveredCargoCanOnlyEnterStorageAtRelay(){
  var meta=new MetaSave{currentRole="scout"};
  RoleFrameworkSystem.Normalize(meta);
  var run=new RunState{role="scout",hp=42,maxHp=42};
  run.courierRoute=CourierRouteSystem.Create(run,meta);

  CourierRouteSystem.Select(run.courierRoute,"ash_market");
  CourierRouteSystem.Commit(run,out _);
  CourierRouteSystem.ResolveCurrent(run,new CourierLocationOutcome(),out _);
  Assert.That(CourierRouteSystem.OpenRelayPacking(run,out _,out _),Is.True);

  CourierRouteSystem.Select(run.courierRoute,"quiet_shaft");
  CourierRouteSystem.Commit(run,out _);
  run.lootBag.Add(new ItemInstance("sword"));
  CourierRouteSystem.ResolveCurrent(run,new CourierLocationOutcome{cargoRecovered=true},out _);
  Assert.That(run.lootBag,Has.Count.EqualTo(1));
  Assert.That(CourierRouteSystem.OpenRelayPacking(run,out _,out _),Is.False);

  CourierRouteSystem.Select(run.courierRoute,"cinder_tunnel");
  CourierRouteSystem.Commit(run,out _);
  CourierRouteSystem.ResolveCurrent(run,new CourierLocationOutcome(),out _);
  CourierRouteSystem.Select(run.courierRoute,"reliquary_post");
  CourierRouteSystem.Commit(run,out _);
  run.lootBag.Add(new ItemInstance("shield"));
  CourierRouteSystem.ResolveCurrent(run,new CourierLocationOutcome(),out _);

  Assert.That(CourierRouteSystem.OpenRelayPacking(run,out int transferred,out _),Is.True);
  Assert.That(transferred,Is.EqualTo(2));
  Assert.That(run.lootBag,Is.Empty);
  Assert.That(run.inventory,Has.Count.EqualTo(2));
 }

 [Test]
 public void DeliverySeals_AreDerivedFromPackingColorsAndPreserveSpentCharges(){
  var meta=new MetaSave{currentRole="scout"};
  RoleFrameworkSystem.Normalize(meta);
  var run=new RunState{role="scout"};
  var colors=new Dictionary<Element,int>{
   [Element.Fire]=3,[Element.Water]=0,[Element.Wind]=2,[Element.Earth]=1
  };

  var seals=DeliverySealSystem.Build(run,meta,colors);

  Assert.That(seals.Any(seal=>seal.roleSignature),Is.True);
  Assert.That(seals.Single(seal=>seal.key=="color-fire").maxCharges,Is.EqualTo(2));
  Assert.That(seals.Single(seal=>seal.key=="color-wind").target,Is.EqualTo(DeliverySealSystem.RouteTarget));
  Assert.That(seals.Single(seal=>seal.key=="color-earth").target,Is.EqualTo(DeliverySealSystem.DelayTarget));
  run.courierRoute=new CourierRouteState{seals=seals};
  Assert.That(CourierRouteSystem.UseSeal(run.courierRoute,"color-earth",out _),Is.True);
  Assert.That(run.courierRoute.delayShield,Is.EqualTo(1));
  seals.Single(seal=>seal.key=="color-fire").charges--;

  var refreshed=DeliverySealSystem.Refresh(seals,run,meta,colors);
  Assert.That(refreshed.Single(seal=>seal.key=="color-fire").charges,Is.EqualTo(1));
  var withoutFire=new Dictionary<Element,int>(colors){[Element.Fire]=0};
  var retired=DeliverySealSystem.Refresh(refreshed,run,meta,withoutFire);
  Assert.That(retired.Single(seal=>seal.key=="color-fire").available,Is.False);
  var restored=DeliverySealSystem.Refresh(retired,run,meta,colors);
  Assert.That(restored.Single(seal=>seal.key=="color-fire").charges,Is.EqualTo(1));
 }

 [Test]
 public void ReactionValues_CombineRoleLevelsAndPlacedEquipmentWithBreakdown(){
  var meta=new MetaSave{
   jobLevels=new(){new IdInt("warrior",8)},
   unlockedRoles=new(){"warrior","guardian","scout","artificer"}
  };
  var sword=new ItemInstance("sword");
  var run=new RunState{role="warrior",inventory=new(){sword},placements=new(){new Placement(sword.uid,0)}};

  var snapshot=ReactionSystem.Build(meta,run);

  Assert.That(snapshot.Total("blade_extreme"),Is.EqualTo(10));
  Assert.That(snapshot.Total("blade_extreme",ReactionScopeMask.Mastery),Is.EqualTo(8));
  Assert.That(snapshot.Total("blade_extreme",ReactionScopeMask.Loadout),Is.EqualTo(2));
  Assert.That(snapshot.SourceCount("blade_extreme"),Is.EqualTo(2));
  Assert.That(snapshot.Breakdown("blade_extreme").Count,Is.EqualTo(2));
 }

 [Test]
 public void ReactionValues_UnlockThroughAlternativeMixedSourcesAndRemainUnlocked(){
  var meta=new MetaSave{
   jobLevels=new(){new IdInt("warrior",8)},
   unlockedRoles=new(){"warrior","guardian","scout","artificer"}
  };
  var sword=new ItemInstance("sword");
  var run=new RunState{role="warrior",inventory=new(){sword},placements=new(){new Placement(sword.uid,0)}};

  var discovered=ReactionSystem.DiscoverRoles(meta,run);

  Assert.That(discovered,Does.Contain("blade_master"));
  Assert.That(meta.unlockedRoles,Does.Contain("blade_master"));
  run.placements.Clear();
  Assert.That(ReactionSystem.Build(meta,run).Total("blade_extreme"),Is.EqualTo(8));
  Assert.That(meta.unlockedRoles,Does.Contain("blade_master"),"Discovery must not be revoked when a loadout changes.");
 }

 [Test]
 public void ReactionValues_EventGrantUsesItsAuthoredLifetime(){
  var meta=new MetaSave();
  var run=new RunState();
  var offer=PackspireContent.Data.events.Single(value=>value.id=="memory_rift")
   .choices.Single(value=>value.id=="offer");

  foreach(var contribution in offer.reactionContributions)
   ReactionSystem.Grant(meta,run,contribution,"event:memory_rift:offer");

  Assert.That(ReactionSystem.Build(meta,run).Total("sacrifice",ReactionScopeMask.Expedition),Is.EqualTo(3));
  Assert.That(ReactionSystem.Build(meta,null).Total("sacrifice"),Is.Zero,
   "Expedition reactions must not leak into permanent mastery.");
 }

 [Test]
 public void CombatStatusMath_AppliesWeakAndVulnerable(){
  var attacker=new List<StatusState>{new(){type="weak",amount=1}};
  var defender=new List<StatusState>{new(){type="vulnerable",amount=1}};
  Assert.That(BattleSystem.Damage(12,attacker,defender),Is.EqualTo(14));
 }

 [Test]
 public void CombatTurn_UsesAuthoredEnergyAndHandValues(){
  var run=new RunState{hp=42,maxHp=42,role="warrior"};
  var sword=new ItemInstance("sword");
  run.inventory.Add(sword);
  run.placements.Add(new Placement(sword.uid,0));
  var enemy=GameCatalog.Enemies.First();
  var battle=BattleSystem.Begin(run,enemy);
  run.energy=0;
  run.discard.AddRange(run.hand);
  run.hand.Clear();
  BattleSystem.EndTurnFx(run,battle);
  Assert.That(run.energy,Is.EqualTo(PackspireContent.Data.balance.baseEnergy));
  Assert.That(run.hand.Count,Is.EqualTo(PackspireContent.Data.balance.initialHand));
 }

 [TestCase("focus")]
 [TestCase("tailwind")]
 public void EnergySupplyCard_IncreasesEnergyAfterPayingItsCost(string cardId){
  var definition=GameCatalog.Cards[cardId];
  var run=new RunState{hp=42,maxHp=42,energy=2};
  run.hand.Add(BackpackSystem.FromDef(definition,"test","test-"+cardId));
  var enemy=new EnemyDef("test","Test",1,99,1);
  var battle=new BattleState{
   enemy=enemy,enemyHp=enemy.hp,enemyMaxHp=enemy.hp,enemyStatuses=new()
  };

  var result=BattleSystem.PlayCard(run,battle,0);

  Assert.That(result.ok,Is.True);
  Assert.That(run.energy,Is.EqualTo(3),
   $"{cardId} must visibly recover one energy after its play cost is paid.");
 }

 [Test]
 public void BattleCard_FixedDamageUsesAuthoredValueWithoutDice(){
  var run=new RunState{hp=42,maxHp=42,energy=3};
  run.hand.Add(new CardInstance{
   id="fixed",name="Fixed",type=CardType.Attack,cost=1,damage=5,
   damageMode=DamageResolutionMode.Fixed
  });
  var enemy=new EnemyDef("test","Test",1,30,1);
  var battle=new BattleState{
   enemy=enemy,enemyHp=enemy.hp,enemyMaxHp=enemy.hp,enemyStatuses=new()
  };

  var result=BattleSystem.PlayCard(run,battle,0);

  Assert.That(result.damageToEnemy,Is.EqualTo(5));
  Assert.That(result.dieOne,Is.Zero);
  Assert.That(result.dieTwo,Is.Zero);
  Assert.That(battle.enemyHp,Is.EqualTo(25));
 }

 [Test]
 public void BattleCard_TwoD6RemainsAvailableAsAnExplicitSpecialMode(){
  var card=new CardInstance{
   id="special-die",name="Special Die",type=CardType.Attack,damage=7,
   damageMode=DamageResolutionMode.TwoD6
  };

  int damage=BattleSystem.ResolveCardDamage(card,out int dieOne,out int dieTwo,out int modifier);

  Assert.That(dieOne,Is.InRange(1,6));
  Assert.That(dieTwo,Is.InRange(1,6));
  Assert.That(modifier,Is.Zero);
  Assert.That(damage,Is.EqualTo(dieOne+dieTwo));
 }

 [Test]
 public void CombatStatus_UntimedStrengthPersistsAcrossTurns(){
  var run=new RunState{hp=42,maxHp=42};
  var enemy=GameCatalog.Enemies.First();
  var battle=BattleSystem.Begin(run,enemy);
  BattleSystem.Apply(run.statuses,new EffectSpec{type="strength",target="self",amount=2,duration=0});
  BattleSystem.EndTurnFx(run,battle);
  Assert.That(BattleSystem.Status(run.statuses,"strength"),Is.EqualTo(2));
 }

 [Test]
 public void CombatStatus_LethalPoisonDefeatsEnemyBeforeItsAction(){
  var run=new RunState{hp=42,maxHp=42};
  var enemy=new EnemyDef("test","Test",1,1,99);
  var battle=BattleSystem.Begin(run,enemy);
  BattleSystem.Apply(battle.enemyStatuses,new EffectSpec{type="poison",target="enemy",amount=1});
  var result=BattleSystem.EndTurnFx(run,battle);
  Assert.That(result.enemyDefeated,Is.True);
  Assert.That(result.statusDamageToEnemy,Is.EqualTo(1));
  Assert.That(run.hp,Is.EqualTo(42),"Defeated enemy must not take its action");
 }

 [Test]
 public void EnemyMove_GuardAndEmpowerUseTheAuthoredIntent(){
  var run=new RunState{hp=42,maxHp=42};
  var enemy=GameCatalog.Enemies.Single(value=>value.id=="sentinel");
  var battle=BattleSystem.Begin(run,enemy);
  battle.move=1;
  var move=ContentDatabase.EnemyMove(enemy.id,battle.move);
  Assert.That(move.kind,Is.EqualTo(EnemyMoveKind.Guard));
  var result=BattleSystem.EndTurnFx(run,battle);
  Assert.That(result.enemyMoveKind,Is.EqualTo(EnemyMoveKind.Guard));
  Assert.That(result.enemyBlockGained,Is.EqualTo(9));
  Assert.That(battle.enemyBlock,Is.EqualTo(9));
  Assert.That(BattleSystem.Status(battle.enemyStatuses,"strength"),Is.EqualTo(2));
  Assert.That(run.hp,Is.EqualTo(42));
 }

 [Test]
 public void BossPhase_ChangesTheMoveSequenceAtHalfHealth(){
  var run=new RunState{hp=42,maxHp=42};
  var enemy=GameCatalog.Enemies.Single(value=>value.id=="boss");
  var battle=BattleSystem.Begin(run,enemy);
  Assert.That(BattleSystem.EnemyPhaseName(battle),Is.EqualTo("捕食"));
  Assert.That(BattleSystem.NextEnemyMoveIndex(battle),Is.EqualTo(0));
  battle.enemyHp=battle.enemyMaxHp/2;
  battle.move=3;
  Assert.That(BattleSystem.EnemyPhaseName(battle),Is.EqualTo("暴食"));
  Assert.That(BattleSystem.NextEnemyMoveIndex(battle),Is.EqualTo(1));
  BattleSystem.EndTurnFx(run,battle);
  Assert.That(battle.enemyPhaseThreshold,Is.EqualTo(0));
  Assert.That(BattleSystem.NextEnemyMoveIndex(battle),Is.EqualTo(2));
 }

 [Test]
 public void BattleCardLifecycle_RetainsAndRemovesEtherealCards(){
  var run=new RunState{hp=42,maxHp=42};
  var enemy=GameCatalog.Enemies.First();
  var battle=BattleSystem.Begin(run,enemy);
  run.hand.Clear();
  run.discard.Clear();
  run.hand.Add(new CardInstance{id="retain",name="Retain",retain=true});
  run.hand.Add(new CardInstance{id="ethereal",name="Ethereal",ethereal=true});
  run.hand.Add(new CardInstance{id="normal",name="Normal"});
  BattleSystem.EndTurnFx(run,battle);
  Assert.That(run.hand.Any(card=>card.id=="retain"),Is.True);
  Assert.That(run.hand.Any(card=>card.id=="ethereal"),Is.False);
  Assert.That(run.discard.Concat(run.hand).Any(card=>card.id=="normal"),Is.True);
  Assert.That(run.discard.Any(card=>card.id=="ethereal"),Is.False);
 }

 [Test]
 public void BattleCardLifecycle_RemoveExpeditionDoesNotReturnToTheDeck(){
  var run=new RunState{role="warrior",backpack="standard",energy=3};
  var sword=new ItemInstance("sword");
  run.inventory.Add(sword);
  run.placements.Add(new Placement(sword.uid,0));
  var emitted=BackpackSystem.BuildDeck(run).First(card=>card.sourceItemUid==sword.uid);
  emitted.afterUse=BattleCardAfterUse.RemoveExpedition;
  run.hand.Add(emitted);
  var enemy=GameCatalog.Enemies.First();
  var battle=new BattleState{enemy=enemy,enemyHp=enemy.hp,enemyMaxHp=enemy.hp,enemyStatuses=new()};
  Assert.That(BattleSystem.PlayCard(run,battle,0).ok,Is.True);
  Assert.That(run.removedBattleCardSlots,Does.Contain(emitted.slotKey));
  Assert.That(BackpackSystem.BuildDeck(run).Any(card=>card.slotKey==emitted.slotKey),Is.False);
 }

 [Test]
 public void EquipmentCardsAndSealAttributeRemainLinkedWhenTheDeckIsBuilt(){
  var run=new RunState{role="warrior",backpack="standard"};
  var sword=new ItemInstance("sword");
  run.inventory.Add(sword);
  run.placements.Add(new Placement(sword.uid,0));
  var built=BackpackSystem.Build(run);
  var swordCards=built.candidates.Where(card=>card.sourceItemUid==sword.uid).ToArray();
  Assert.That(swordCards,Has.Length.EqualTo(2));
  Assert.That(swordCards.All(card=>card.id=="slash"),Is.True);
  Assert.That(GameCatalog.Items["sword"].sealAttribute,Is.EqualTo(DeliverySealAttribute.Incineration));
 }

 [Test]
 public void GridBoard_UsesAuthoredBalance(){
  var board=GridBoardSystem.Create(PackspireContent.Data.balance.defaultDungeonId);
  Assert.That(board.energyMax,Is.EqualTo(PackspireContent.Data.balance.baseEnergy));
  Assert.That(board.doomMax,Is.EqualTo(PackspireContent.Data.balance.gridDoomMax));
  Assert.That(board.cells,Is.Not.Empty);
 }

 [Test]
 public void GridBoard_StartsInPathModeWithoutExplorationCards(){
  var board=GridBoardSystem.Create(PackspireContent.Data.balance.defaultDungeonId,41821);
  GridBoardSystem.SyncExplorePool(board,new RunState{role="warrior",backpack="standard"});
  Assert.That(board.hand,Is.Empty);
  Assert.That(board.phase,Is.EqualTo(GridBoardPhase.Path));
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
 public void CompletedDungeon_UsesAuthoredAreasAndEndsAtItsBoss(){
  var dungeon=PackspireContent.Data.dungeons.Single(value=>value.id=="old_spire");
  Assert.That(dungeon.areas,Has.Length.EqualTo(4));
  Assert.That(dungeon.rewardPoolId,Is.EqualTo("old_spire_trials"));

  var board=GridBoardSystem.Create(dungeon.id,19277);
  Assert.That(board.areaCount,Is.EqualTo(dungeon.areas.Length));
  Assert.That(board.size,Is.EqualTo(dungeon.areas[0].size));
  Assert.That(board.enemies.Select(enemy=>enemy.contentId),
   Is.SubsetOf(dungeon.areas[0].enemyIds));
  Assert.That(board.cells.Where(cell=>cell.place=="event").Select(cell=>cell.contentId),
   Is.SubsetOf(dungeon.areas[0].eventIds));

  GridBoardSystem.LoadArea(board,dungeon.areas.Length-1);
  Assert.That(board.size,Is.EqualTo(dungeon.areas[^1].size));
  Assert.That(board.cells.Any(cell=>cell.place=="return"),Is.False,
   "The completed slice must require its final boss instead of exposing an early return gate");
  Assert.That(board.enemies.Count(enemy=>enemy.contentId=="boss"),Is.EqualTo(1));
  Assert.That(GameCatalog.Enemies.Single(enemy=>enemy.id=="boss").tier,Is.EqualTo(3),
   "Tier 3 is what routes victory through the expedition completion flow");
 }

 [Test]
 public void CompletedDungeon_RewardPoolCoversEveryEquipmentDirection(){
  var dungeon=PackspireContent.Data.dungeons.Single(value=>value.id=="old_spire");
  var pool=PackspireContent.Data.rewardPools.Single(value=>value.id==dungeon.rewardPoolId);
  var items=pool.itemIds.Select(id=>GameCatalog.Items[id]).ToArray();
  Assert.That(items.Any(item=>item.type==ItemType.Weapon),Is.True);
  Assert.That(items.Any(item=>item.type==ItemType.Armor),Is.True);
  Assert.That(items.Any(item=>item.type==ItemType.Rune),Is.True);
  Assert.That(items.Any(item=>item.type==ItemType.Supply),Is.True);
  Assert.That(items.Any(item=>item.rarity==ItemRarity.Cursed),Is.True);
 }

 [Test]
 public void CompletedDungeon_UsesConnectedAuthoredSilhouettes(){
  var dungeon=PackspireContent.Data.dungeons.Single(value=>value.id=="old_spire");
  Assert.That(dungeon.areas.Select(area=>string.Join("/",area.layoutRows)).Distinct().Count(),
   Is.EqualTo(dungeon.areas.Length),"Every completed area should have its own silhouette");
  foreach(var area in dungeon.areas){
   Assert.That(area.layoutRows,Has.Length.EqualTo(area.size));
   Assert.That(area.layoutRows.All(row=>row.Length==area.size),Is.True);
   Assert.That(area.layoutRows.Sum(row=>row.Count(cell=>cell=='S')),Is.EqualTo(1));
   Assert.That(area.layoutRows.Any(row=>row.Contains('X')),Is.True,
    $"{area.id} should not render as a full square");
  }

  var board=GridBoardSystem.Create(dungeon.id,72841);
  for(int areaIndex=0;areaIndex<dungeon.areas.Length;areaIndex++){
   GridBoardSystem.LoadArea(board,areaIndex);
   Assert.That(GridBoardSystem.HasAuthoredLayout(board),Is.True);
   Assert.That(GridBoardSystem.IsAreaTraversable(board),Is.True,
    $"{dungeon.areas[areaIndex].id} must remain one connected playable fragment");
  }
 }

 [Test]
 public void GridBoard_HostileIntelKeepsOnlyTheLastObservedPosition(){
  var board=GridBoardSystem.Create(PackspireContent.Data.balance.defaultDungeonId,48219);
  board.enemies.Clear();
  var hiddenCells=board.cells.Where(cell=>cell.terrain=="floor"&&
   System.Math.Max(System.Math.Abs(cell.x-board.piece.x),
    System.Math.Abs(cell.y-board.piece.y))>=3).Take(2).ToArray();
  Assert.That(hiddenCells,Has.Length.EqualTo(2));
  var enemy=new GridEnemyState{
   uid="hidden-signature",contentId="dragon",
   x=hiddenCells[0].x,y=hiddenCells[0].y,
   previousX=hiddenCells[0].x,previousY=hiddenCells[0].y
  };
  board.enemies.Add(enemy);

  Assert.That(GridBoardSystem.LastKnownEnemyPosition(enemy),
   Is.EqualTo(new UnityEngine.Vector2Int(hiddenCells[0].x,hiddenCells[0].y)));
  Assert.That(enemy.identified,Is.False);
  enemy.x=hiddenCells[1].x;
  enemy.y=hiddenCells[1].y;
  Assert.That(GridBoardSystem.LastKnownEnemyPosition(enemy),
   Is.EqualTo(new UnityEngine.Vector2Int(hiddenCells[0].x,hiddenCells[0].y)),
   "Hidden movement must not turn the signature into perfect radar");

  GridBoardSystem.RevealAround(board,enemy.Position,0);
  Assert.That(enemy.identified,Is.True);
  Assert.That(GridBoardSystem.LastKnownEnemyPosition(enemy),Is.EqualTo(enemy.Position));
 }

 [Test]
 public void GridBoard_CommittedRouteAlertsNearbyHostiles(){
  var board=GridBoardSystem.Create(PackspireContent.Data.balance.defaultDungeonId,62114);
  board.enemies.Clear();
  GridBoardSystem.BeginPathPhase(board);
  var direction=new[]{
   UnityEngine.Vector2Int.up,UnityEngine.Vector2Int.right,
   UnityEngine.Vector2Int.down,UnityEngine.Vector2Int.left
  }.First(value=>GridBoardSystem.CanSlide(board,value));
  Assert.That(GridBoardSystem.TrySlide(board,direction,out _),Is.True);
  var routeCell=board.path[1];
  var hostileCell=board.cells.First(cell=>cell.terrain=="floor"&&
   System.Math.Abs(cell.x-routeCell.x)+System.Math.Abs(cell.y-routeCell.y)<=1&&
   !board.path.Contains(new UnityEngine.Vector2Int(cell.x,cell.y)));
  var enemy=new GridEnemyState{
   uid="route-listener",contentId="sentinel",x=hostileCell.x,y=hostileCell.y
  };
  board.enemies.Add(enemy);

  Assert.That(GridBoardSystem.AlertEnemiesAlongRoute(board,1),Is.EqualTo(1));
  Assert.That(enemy.alerted,Is.True);
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
  Assert.That(board.phase,Is.EqualTo(GridBoardPhase.Path));
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

 [Test]
 public void RealtimeBattle_RecoversEnergyAtTimelineBoundaries(){
  var timeline=new RealtimeBattleController();
  timeline.Start(initialEnergy:1,maximumEnergy:3,energyInterval:2d);
  Assert.That(timeline.TrySpendEnergy(1),Is.True);
  timeline.Tick(1.99d);
  Assert.That(timeline.Energy,Is.EqualTo(0));
  timeline.Tick(.01d);
  Assert.That(timeline.Energy,Is.EqualTo(1));
 Assert.That(timeline.NextEnergyIn,Is.EqualTo(2d).Within(.0001d));
 }

 [Test]
 public void RealtimeBattle_SupplyPulseOccursEvenAtMaximumEnergy(){
  var timeline=new RealtimeBattleController();
  timeline.Start(initialEnergy:3,maximumEnergy:3,energyInterval:2d);
  timeline.Tick(2d);
  Assert.That(timeline.Energy,Is.EqualTo(3));
 Assert.That(timeline.SupplyPulseCount,Is.EqualTo(1));
 }

 [Test]
 public void RealtimeBattle_ExposesTypedSupplyCheckpointsWithinHorizon(){
  var timeline=new RealtimeBattleController();
  var previews=new List<RealtimeSupplyPulsePreview>();
  timeline.Start(
   initialEnergy:1,
   maximumEnergy:3,
   energyInterval:2d,
   energyPerSupplyPulse:1,
   drawsPerSupplyPulse:2);
  timeline.GetUpcomingSupplyPulses(previews,5d);
  Assert.That(previews,Has.Count.EqualTo(2));
  Assert.That(previews[0].TimeUntil,Is.EqualTo(2d).Within(.0001d));
  Assert.That(previews[0].EnergyDelta,Is.EqualTo(1));
  Assert.That(previews[0].DrawCount,Is.EqualTo(2));
  Assert.That(previews[1].TimeUntil,Is.EqualTo(4d).Within(.0001d));
 }

 [Test]
 public void RealtimeBattle_ExposesStableUpcomingActionPreviews(){
  var timeline=new RealtimeBattleController();
  var previews=new List<RealtimeEnemyActionPreview>();
  timeline.Start(initialEnergy:1,maximumEnergy:3,energyInterval:2d);
  timeline.ScheduleEnemyAction("warden","combo:連鐘",delay:4d,damage:4,
   telegraphLead:.8d,hitCount:3,hitSpacing:.2d);
  timeline.GetUpcomingActions(previews);
  Assert.That(previews,Has.Count.EqualTo(1));
  Assert.That(previews[0].Damage,Is.EqualTo(4));
  Assert.That(previews[0].HitCount,Is.EqualTo(3));
  Assert.That(previews[0].TimeUntil,Is.EqualTo(4d).Within(.0001d));
 }

 [Test]
 public void RealtimeBattle_MultiHitTelegraphsOnceAndResolvesEveryHit(){
  var timeline=new RealtimeBattleController();
  int telegraphs=0;
  int hits=0;
  timeline.Start(initialEnergy:0,maximumEnergy:3,energyInterval:5d);
  timeline.EnemyTelegraphStarted+=(_,__)=>telegraphs++;
  timeline.EnemyActionResolved+=action=>{
   hits++;
   Assert.That(action.SequenceCount,Is.EqualTo(3));
  };
  timeline.ScheduleEnemyAction("warden","bell-combo",delay:1d,damage:4,
   telegraphLead:.4d,hitCount:3,hitSpacing:.2d);
  timeline.Tick(.6d);
  Assert.That(telegraphs,Is.EqualTo(1));
  timeline.Tick(.8d);
  Assert.That(hits,Is.EqualTo(3));
 }

 [Test]
 public void RealtimeBattle_DelayMovesOnlyUncommittedActions(){
  var timeline=new RealtimeBattleController();
  var previews=new List<RealtimeEnemyActionPreview>();
  timeline.Start(initialEnergy:0,maximumEnergy:3,energyInterval:20d);
  timeline.ScheduleEnemyAction("warden","soon",delay:2d,damage:4,telegraphLead:1d);
  timeline.ScheduleEnemyAction("warden","later",delay:5d,damage:7,telegraphLead:1d);
  timeline.Tick(1.1d);
  Assert.That(timeline.DelayUpcomingActions(2d),Is.EqualTo(1));
  timeline.GetUpcomingActions(previews);
  Assert.That(previews[0].ActionId,Is.EqualTo("soon"));
  Assert.That(previews[0].TimeUntil,Is.EqualTo(.9d).Within(.0001d));
  Assert.That(previews[1].ActionId,Is.EqualTo("later"));
  Assert.That(previews[1].TimeUntil,Is.EqualTo(5.9d).Within(.0001d));
 }

 [Test]
 public void RealtimeBattle_TimedBoostsExpireOnTimelineTime(){
  var timeline=new RealtimeBattleController();
  timeline.Start(initialEnergy:0,maximumEnergy:3,energyInterval:20d);
  timeline.ApplyAttackBoost(2,5d);
  timeline.ApplyGuardBoost(3,4d);
  timeline.Tick(3d);
  Assert.That(timeline.AttackBonus,Is.EqualTo(2));
  Assert.That(timeline.GuardBonus,Is.EqualTo(3));
  timeline.Tick(1.1d);
  Assert.That(timeline.AttackBonus,Is.EqualTo(2));
  Assert.That(timeline.GuardBonus,Is.Zero);
  timeline.Tick(1d);
  Assert.That(timeline.AttackBonus,Is.Zero);
 }

 [Test]
 public void RealtimeTimelinePlanner_PreservesAuthoredBurstsAndRests(){
  var timeline=new RealtimeBattleController();
  var previews=new List<RealtimeEnemyActionPreview>();
  timeline.Start(initialEnergy:0,maximumEnergy:3,energyInterval:20d);
  var quietThenBurst=new RealtimeEnemyTimelinePattern(
   "charge-burst",12d,
   new RealtimeEnemyTimelineStep(2d,"charge:first",0,.2d),
   new RealtimeEnemyTimelineStep(5d,"charge:second",0,.2d),
   new RealtimeEnemyTimelineStep(6d,"attack:burst-a",4,.5d),
   new RealtimeEnemyTimelineStep(6.8d,"attack:burst-b",4,.5d),
   new RealtimeEnemyTimelineStep(7.6d,"attack:burst-c",4,.5d));
  var planner=new RealtimeEnemyTimelinePlanner(timeline,"warden",quietThenBurst);
  planner.Reset();
  planner.EnsureScheduledThrough(12d);
  timeline.GetUpcomingActions(previews);
  Assert.That(previews,Has.Count.EqualTo(5));
  Assert.That(previews[0].TimeUntil,Is.EqualTo(2d).Within(.0001d));
  Assert.That(previews[1].TimeUntil,Is.EqualTo(5d).Within(.0001d));
  Assert.That(previews[2].TimeUntil,Is.EqualTo(6d).Within(.0001d));
  Assert.That(previews[3].TimeUntil,Is.EqualTo(6.8d).Within(.0001d));
  Assert.That(previews[4].TimeUntil,Is.EqualTo(7.6d).Within(.0001d));
  Assert.That(planner.PlannedThrough,Is.EqualTo(12d).Within(.0001d));
 }

 [Test]
 public void RealtimeTimelinePlanner_ExtendsByPatternsInsteadOfVisibleActionCount(){
  var timeline=new RealtimeBattleController();
  var previews=new List<RealtimeEnemyActionPreview>();
  timeline.Start(initialEnergy:0,maximumEnergy:3,energyInterval:20d);
  var sparse=new RealtimeEnemyTimelinePattern(
   "sparse",10d,
   new RealtimeEnemyTimelineStep(1d,"attack:first",5,.5d));
  var dense=new RealtimeEnemyTimelinePattern(
   "dense",8d,
   new RealtimeEnemyTimelineStep(1d,"attack:a",3,.4d),
   new RealtimeEnemyTimelineStep(2d,"attack:b",3,.4d),
   new RealtimeEnemyTimelineStep(3d,"attack:c",3,.4d));
  var planner=new RealtimeEnemyTimelinePlanner(timeline,"warden",sparse,dense);
  planner.Reset();
  planner.EnsureScheduledThrough(13d);
  timeline.GetUpcomingActions(previews);
  Assert.That(previews,Has.Count.EqualTo(4));
  Assert.That(previews[0].TimeUntil,Is.EqualTo(1d).Within(.0001d));
  Assert.That(previews[1].TimeUntil,Is.EqualTo(11d).Within(.0001d));
  Assert.That(previews[2].TimeUntil,Is.EqualTo(12d).Within(.0001d));
  Assert.That(previews[3].TimeUntil,Is.EqualTo(13d).Within(.0001d));
  Assert.That(planner.ScheduledPatternCount,Is.EqualTo(2));
 Assert.That(planner.PlannedThrough,Is.EqualTo(18d).Within(.0001d));
 }

 [Test]
 public void WardenRealtimeTimeline_IsAuthoredDataAndBuildsValidPatterns(){
  var profile=AssetDatabase.LoadAssetAtPath<RealtimeEnemyTimelineProfile>(
   "Assets/Resources/Data/Journey/WardenRealtimeTimeline.asset");
  Assert.That(profile,Is.Not.Null);
  Assert.That(profile.actorId,Is.EqualTo("warden"));
  var patterns=profile.BuildPatterns();
  Assert.That(patterns,Has.Length.EqualTo(4));
  Assert.That(patterns[0].Duration,Is.EqualTo(9.5d).Within(.0001d));
  Assert.That(patterns[1].Steps[0].Kind,Is.EqualTo(RealtimeEnemyActionKind.ComboAttack));
  Assert.That(patterns[3].Steps[1].Kind,Is.EqualTo(RealtimeEnemyActionKind.JumpReaction));
 }

 [Test]
 public void JourneyEncounterProfile_BuildsEnemyAndReferencesAuthoredTimeline(){
  var profile=AssetDatabase.LoadAssetAtPath<JourneyBattleEncounterProfile>(
   "Assets/Resources/Data/Journey/WardenEncounter.asset");
  Assert.That(profile,Is.Not.Null);
  var enemy=profile.BuildEnemy();
  Assert.That(enemy.id,Is.EqualTo("journey_postal_warden"));
  Assert.That(enemy.hp,Is.EqualTo(36));
  Assert.That(profile.timeline.BuildPatterns(),Has.Length.EqualTo(4));
 }

 [Test]
 public void JourneyMiniGame_TimingFailureResolvesWithoutUi(){
  var miniGame=new JourneyMiniGameController();
  miniGame.Start(MiniGameKind.StampTiming);
  miniGame.Tick(7.1f);
  Assert.That(miniGame.IsResolved,Is.True);
  Assert.That(miniGame.TryConsumeOutcome(out var outcome),Is.True);
  Assert.That(outcome.Success,Is.False);
 }
}
#endif
