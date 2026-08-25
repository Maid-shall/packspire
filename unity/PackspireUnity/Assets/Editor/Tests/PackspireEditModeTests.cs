#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using System;
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
  Assert.That(run.expeditionPlan,Is.Not.Null);
  Assert.That(run.expeditionPlan.floors,Has.Count.EqualTo(3));
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
  Assert.That(run.expeditionPlan.elapsedDays,Is.EqualTo(0));
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
  Assert.That(run.expeditionPlan.elapsedDays,Is.EqualTo(4));
 }

 [Test]
 public void CourierRoute_DaysOverLegacyDeadlineChangeStageWithoutFailingRun(){
  var run=new RunState{role="scout",hp=42,maxHp=42,dungeon="old_spire"};
  run.courierRoute=CourierRouteSystem.Create(run,new MetaSave{currentRole="scout"});

  ExpeditionProgressSystem.AdvanceDays(run,21);

  Assert.That(run.courierRoute.daysElapsed,Is.EqualTo(21));
  Assert.That(run.expeditionPlan.elapsedDays,Is.EqualTo(21));
  Assert.That(run.courierRoute.failed,Is.False);
  Assert.That(ExpeditionProgressSystem.CurrentDayStage(run),Is.EqualTo(ExpeditionDayStage.Pursuit));
 }

 [Test]
 public void ExpeditionProgress_AdoptsHigherLegacySaveDayWithoutDoubleCounting(){
  var run=new RunState{
   dungeon="old_spire",
   courierRoute=new CourierRouteState{daysElapsed=13},
   expeditionPlan=ExpeditionRoutePlanSystem.GenerateDefault("old_spire")
  };
  run.expeditionPlan.elapsedDays=8;

  ExpeditionProgressSystem.Ensure(run);
  ExpeditionProgressSystem.AdvanceDays(run,2);

  Assert.That(run.expeditionPlan.elapsedDays,Is.EqualTo(15));
  Assert.That(run.courierRoute.daysElapsed,Is.EqualTo(15));
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
   telegraphLead:.8d,hitCount:3,hitSpacing:.2d,
   kind:RealtimeEnemyActionKind.ComboAttack,
   motionLane:RealtimeEnemyMotionLane.Low);
  timeline.GetUpcomingActions(previews);
  Assert.That(previews,Has.Count.EqualTo(1));
  Assert.That(previews[0].Damage,Is.EqualTo(4));
  Assert.That(previews[0].HitCount,Is.EqualTo(3));
  Assert.That(previews[0].MotionLane,Is.EqualTo(RealtimeEnemyMotionLane.Low));
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
   Assert.That(action.MotionLane,Is.EqualTo(RealtimeEnemyMotionLane.High));
  };
  timeline.ScheduleEnemyAction("warden","bell-combo",delay:1d,damage:4,
   telegraphLead:.4d,hitCount:3,hitSpacing:.2d,
   motionLane:RealtimeEnemyMotionLane.High);
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
  var patterns=profile.BuildPatterns(battleSeed:17);
  Assert.That(patterns,Has.Length.EqualTo(6));
  Assert.That(patterns[0].Duration,Is.EqualTo(8d).Within(.0001d));
  Assert.That(patterns[0].Steps[0].ActionId,Is.EqualTo("warden-normal"));
  Assert.That(patterns[2].Steps[1].Kind,Is.EqualTo(RealtimeEnemyActionKind.JumpReaction));
  Assert.That(patterns[3].Steps[1].Kind,Is.EqualTo(RealtimeEnemyActionKind.BraceReaction));
 }

 [Test]
 public void RealtimeEnemyBehaviorPilot_UsesFortyGenericTimingsAndEnemyCommandSets(){
  var catalog=AssetDatabase.LoadAssetAtPath<RealtimeEnemyBehaviorCatalog>(
   "Assets/Resources/Data/Journey/Timelines/SharedEnemyBehaviorCatalog.asset");
  Assert.That(catalog,Is.Not.Null);
  Assert.That(catalog.patterns,Has.Length.EqualTo(40));
  Assert.That(catalog.actions,Is.Empty);
  string root="Assets/Resources/Data/Journey/Timelines/";
  string[] paths={
   "Assets/Resources/Data/Journey/WardenRealtimeTimeline.asset",
   root+"AshScavengerTimeline.asset",root+"AshBailiffTimeline.asset",
   root+"DrownedIndexerTimeline.asset",root+"DrownedCustodianTimeline.asset",
   root+"AshBellWatchmanTimeline.asset",root+"DrownedBellWatchmanTimeline.asset"
  };
  foreach(string path in paths){
   var profile=AssetDatabase.LoadAssetAtPath<RealtimeEnemyTimelineProfile>(path);
   Assert.That(profile,Is.Not.Null,path);
   Assert.That(profile.catalog,Is.SameAs(catalog),path);
   Assert.That(profile.patternIds,Has.Length.EqualTo(6),path);
   Assert.That(profile.commands,Has.Length.EqualTo(5),path);
   Assert.That(profile.timingScale,Is.EqualTo(1f),path);
   Assert.That(profile.selectionMode,Is.EqualTo(RealtimeEnemySelectionMode.RuleBased),path);
   var report=RealtimeEnemyBehaviorAudit.Analyze(profile);
   Assert.That(report.errors,Is.Empty,$"{path}\n{string.Join("\n",report.errors)}");
   Assert.That(report.commandCount,Is.EqualTo(5),path);
  }
 }

 [Test]
 public void RealtimeEnemyBehaviorCatalog_StoresOnlyGenericTimingSlots(){
  var catalog=AssetDatabase.LoadAssetAtPath<RealtimeEnemyBehaviorCatalog>(
   "Assets/Resources/Data/Journey/Timelines/SharedEnemyBehaviorCatalog.asset");
  foreach(var pattern in catalog.patterns)
   foreach(var step in pattern.steps){
    Assert.That(step.actionId,Is.Empty,pattern.id);
    Assert.That(step.damage,Is.Zero,pattern.id);
   }
 }

 [Test]
 public void RealtimeEnemyTimelineProfile_BindsCommandsAndScalesTheWholeTiming(){
  var catalog=UnityEngine.ScriptableObject.CreateInstance<RealtimeEnemyBehaviorCatalog>();
  catalog.patterns=new[]{
   new RealtimeEnemyTimelinePatternContent{
    id="timing-test",duration=10f,
    steps=new[]{new RealtimeEnemyTimelineStepContent{
     executeOffset=2f,kind=RealtimeEnemyActionKind.JumpReaction,
     telegraphLead=1f,hitCount=1,hitSpacing=.2f
    }}
   }
  };
  var profile=UnityEngine.ScriptableObject.CreateInstance<RealtimeEnemyTimelineProfile>();
  profile.catalog=catalog;
  profile.patternIds=new[]{"timing-test"};
  profile.timingScale=1.25f;
  profile.commands=new[]{new RealtimeEnemyActionContent{
   id="enemy-jump",kind=RealtimeEnemyActionKind.JumpReaction,
   motionLane=RealtimeEnemyMotionLane.High,value=7
  }};

  var pattern=profile.BuildPatterns(battleSeed:3)[0];

  Assert.That(pattern.Duration,Is.EqualTo(12.5d).Within(.0001d));
  Assert.That(pattern.Steps[0].ExecuteOffset,Is.EqualTo(2.5d).Within(.0001d));
  Assert.That(pattern.Steps[0].TelegraphLead,Is.EqualTo(1.25d).Within(.0001d));
  Assert.That(pattern.Steps[0].HitSpacing,Is.EqualTo(.25d).Within(.0001d));
 Assert.That(pattern.Steps[0].ActionId,Is.EqualTo("enemy-jump"));
 Assert.That(pattern.Steps[0].Damage,Is.EqualTo(7));
 Assert.That(pattern.Steps[0].MotionLane,Is.EqualTo(RealtimeEnemyMotionLane.High));
 }

 [Test]
 public void RealtimeEnemyBehaviorAudit_RejectsMissingCommandKinds(){
  var catalog=UnityEngine.ScriptableObject.CreateInstance<RealtimeEnemyBehaviorCatalog>();
  catalog.patterns=new[]{
   new RealtimeEnemyTimelinePatternContent{
    id="needs-brace",duration=4f,
    steps=new[]{new RealtimeEnemyTimelineStepContent{
     executeOffset=2f,kind=RealtimeEnemyActionKind.BraceReaction
    }}
   }
  };
  var profile=UnityEngine.ScriptableObject.CreateInstance<RealtimeEnemyTimelineProfile>();
  profile.catalog=catalog;
  profile.patternIds=new[]{"needs-brace"};
  profile.commands=new[]{new RealtimeEnemyActionContent{
   id="normal-only",kind=RealtimeEnemyActionKind.NormalAttack,value=1
  }};

  var report=RealtimeEnemyBehaviorAudit.Analyze(profile);

  Assert.That(report.errors.Any(error=>error.Contains("no command for BraceReaction")),Is.True);
 }

 [Test]
 public void RuleBasedRealtimeEnemyStrategy_AdvancesHpPhaseWithoutReturningAfterHealing(){
  var calm=RulePattern("calm",1);
  var rage=RulePattern("rage",1);
  var phases=new[]{
   new RealtimeEnemyPhaseContent{id="steady",enterAtOrBelowHealthRatio=1f,
    patternWeights=new[]{Weight("calm",100),Weight("rage",0)}},
   new RealtimeEnemyPhaseContent{id="rage",enterAtOrBelowHealthRatio=.5f,
    patternWeights=new[]{Weight("calm",0),Weight("rage",100)}}
  };
  var strategy=new RuleBasedRealtimeEnemyTimelineStrategy(17,"","calm",phases,calm,rage);

  Assert.That(strategy.SelectNext(new RealtimeEnemyTimelineDecisionContext(0,0,0,"",100,100)).Id,
   Is.EqualTo("calm"));
  Assert.That(strategy.SelectNext(new RealtimeEnemyTimelineDecisionContext(1,2,1,"calm",40,100)).Id,
   Is.EqualTo("rage"));
  Assert.That(strategy.SelectNext(new RealtimeEnemyTimelineDecisionContext(2,4,2,"rage",90,100)).Id,
   Is.EqualTo("rage"));
  Assert.That(strategy.CurrentPhaseId,Is.EqualTo("rage"));
 }

 [Test]
 public void RuleBasedRealtimeEnemyStrategy_SharedCooldownBlocksDifferentReactionPatterns(){
  var reactionA=RulePattern("reaction-a",100,"reaction",8d);
  var reactionB=RulePattern("reaction-b",100,"reaction",8d);
  var normal=RulePattern("normal",1);
  var strategy=new RuleBasedRealtimeEnemyTimelineStrategy(
   9,"reaction-a","normal",Array.Empty<RealtimeEnemyPhaseContent>(),
   reactionA,reactionB,normal);

  Assert.That(strategy.SelectNext(new RealtimeEnemyTimelineDecisionContext(0,0,0,"",10,10)).Id,
   Is.EqualTo("reaction-a"));
  Assert.That(strategy.SelectNext(new RealtimeEnemyTimelineDecisionContext(0,2,1,"reaction-a",10,10)).Id,
   Is.EqualTo("normal"));
 }

 [Test]
 public void RuleBasedRealtimeEnemyStrategy_IsStableWhenLoadoutOrderChanges(){
  var alpha=RulePattern("alpha",1);
  var beta=RulePattern("beta",3);
  var first=new RuleBasedRealtimeEnemyTimelineStrategy(
   42,"","alpha",Array.Empty<RealtimeEnemyPhaseContent>(),alpha,beta);
  var second=new RuleBasedRealtimeEnemyTimelineStrategy(
   42,"","alpha",Array.Empty<RealtimeEnemyPhaseContent>(),beta,alpha);

  for(int index=0;index<20;index++){
   var context=new RealtimeEnemyTimelineDecisionContext(index,index*2,index,"",10,10);
   Assert.That(first.SelectNext(context).Id,Is.EqualTo(second.SelectNext(context).Id));
  }
 }

 [Test]
 public void JourneyEncounterProfile_BuildsEnemyAndReferencesAuthoredTimeline(){
  var profile=AssetDatabase.LoadAssetAtPath<JourneyBattleEncounterProfile>(
   "Assets/Resources/Data/Journey/WardenEncounter.asset");
  Assert.That(profile,Is.Not.Null);
  var enemy=profile.BuildEnemy();
  Assert.That(enemy.id,Is.EqualTo("journey_postal_warden"));
  Assert.That(enemy.hp,Is.EqualTo(36));
  Assert.That(profile.enemyDefinition,Is.Not.Null);
  Assert.That(profile.enemyVariant,Is.Not.Null);
  Assert.That(profile.ResolvedTimeline,Is.Not.Null);
  Assert.That(profile.BuildTimelinePatterns(),Has.Length.EqualTo(6));
  Assert.That(profile.StableEncounterId,Is.EqualTo("warden"));
  Assert.That(profile.allowNormalBattle,Is.False);
  Assert.That(profile.allowBossBattle,Is.True);
 }

 [Test]
 public void WardenBattlePresentation_ResolvesSixPosesAndAuthoredEffectAnchors(){
  var encounter=AssetDatabase.LoadAssetAtPath<JourneyBattleEncounterProfile>(
   "Assets/Resources/Data/Journey/WardenEncounter.asset");
  Assert.That(encounter,Is.Not.Null);
  var presentation=encounter.ResolvedBattlePresentation;
  Assert.That(presentation,Is.Not.Null);
  Assert.That(presentation.HasCompletePoseSet,Is.True);
  Assert.That(presentation.AnchorsAreNormalized,Is.True);
  Assert.That(presentation.TimingCueAnchor(true),Is.EqualTo(new UnityEngine.Vector2(.42f,.79f)));
  Assert.That(presentation.TimingCueAnchor(false),Is.EqualTo(new UnityEngine.Vector2(.83f,.48f)));
  Assert.That(presentation.ImpactContactAnchor(true),Is.EqualTo(new UnityEngine.Vector2(.1f,.145f)));
  Assert.That(presentation.ImpactContactAnchor(false),Is.EqualTo(new UnityEngine.Vector2(.52f,.14f)));
  var sprites=PackspireResources.LoadAll<UnityEngine.Sprite>(encounter.ResolvedBattleSheetResource);
  Assert.That(sprites,Is.Not.Null);
  foreach(string poseName in presentation.OrderedPoseNames)
   Assert.That(sprites.Any(sprite=>sprite.name==poseName),Is.True,poseName);
 }

 [Test]
 public void JourneyEnemyComposition_ReusesIdentityAndTimelineAcrossPopulationClasses(){
  var source=AssetDatabase.LoadAssetAtPath<JourneyBattleEncounterProfile>(
   "Assets/Resources/Data/Journey/WardenEncounter.asset");
  string root="Assets/Resources/Data/Journey/EnemyVariants/";
  var variants=new[]{
   AssetDatabase.LoadAssetAtPath<JourneyEnemyVariantDefinition>(root+"CommonEnemy.asset"),
   AssetDatabase.LoadAssetAtPath<JourneyEnemyVariantDefinition>(root+"EnhancedEnemy.asset"),
   AssetDatabase.LoadAssetAtPath<JourneyEnemyVariantDefinition>(root+"EliteEnemy.asset"),
   AssetDatabase.LoadAssetAtPath<JourneyEnemyVariantDefinition>(root+"BossEnemy.asset")
  };
  int[] expectedHealth={36,43,54,72};
  int[] expectedFirstDamage={8,9,10,11};
  for(int index=0;index<variants.Length;index++){
   Assert.That(variants[index],Is.Not.Null);
   var composed=UnityEngine.Object.Instantiate(source);
   try{
    composed.enemyVariant=variants[index];
    var enemy=composed.BuildEnemy();
    var patterns=composed.BuildTimelinePatterns();
    Assert.That(composed.PopulationClass,Is.EqualTo((JourneyEnemyPopulationClass)index));
    Assert.That(enemy.id,Is.EqualTo("journey_postal_warden"));
    Assert.That(enemy.hp,Is.EqualTo(expectedHealth[index]));
    Assert.That(patterns[0].Steps[0].Damage,Is.EqualTo(expectedFirstDamage[index]));
   }
   finally{
    UnityEngine.Object.DestroyImmediate(composed);
   }
  }
 }

 [Test]
 public void JourneyEnemyPopulationTargets_UseApprovedTwoHundredEnemyRatio(){
  Assert.That(JourneyEnemyPopulationTargets.Common,Is.EqualTo(100));
  Assert.That(JourneyEnemyPopulationTargets.Enhanced,Is.EqualTo(50));
  Assert.That(JourneyEnemyPopulationTargets.Elite,Is.EqualTo(30));
  Assert.That(JourneyEnemyPopulationTargets.Boss,Is.EqualTo(20));
  Assert.That(JourneyEnemyPopulationTargets.Total,Is.EqualTo(200));
 }

 [Test]
 public void JourneyEncounterProfile_LegacyInlineDataStillBuildsDuringMigration(){
  var source=AssetDatabase.LoadAssetAtPath<JourneyBattleEncounterProfile>(
   "Assets/Resources/Data/Journey/WardenEncounter.asset");
  var legacy=UnityEngine.Object.Instantiate(source);
  try{
   legacy.enemyDefinition=null;
   legacy.enemyVariant=null;
   legacy.behaviorProfile=null;
   var enemy=legacy.BuildEnemy();
   Assert.That(enemy.id,Is.EqualTo("journey_postal_warden"));
   Assert.That(enemy.hp,Is.EqualTo(36));
   Assert.That(legacy.BuildTimelinePatterns(),Has.Length.EqualTo(6));
  }
  finally{
   UnityEngine.Object.DestroyImmediate(legacy);
  }
 }

 [Test]
 public void JourneyEncounterSelection_FiltersByNodeRoleAndPersistsTheChoice(){
  var normal=CreateEncounterSelectionProfile("normal",allowNormal:true,allowBoss:false);
  var boss=CreateEncounterSelectionProfile("boss",allowNormal:false,allowBoss:true);
  try{
   var plan=ExpeditionRoutePlanSystem.GenerateDefault("old_spire");
   var battleNode=plan.floors[0].nodes.First(node=>node.kind==ExpeditionNodeKind.Battle);
   var selected=JourneyEncounterSelectionSystem.SelectAndAssign(
    plan,battleNode,"old_spire",0,new[]{normal,boss});

   Assert.That(selected,Is.SameAs(normal));
   Assert.That(battleNode.encounterId,Is.EqualTo("normal"));
   Assert.That(JourneyEncounterSelectionSystem.SelectAndAssign(
    plan,battleNode,"old_spire",25,new[]{normal,boss}),Is.SameAs(normal));

   var bossNode=plan.floors[0].nodes.Single(node=>node.kind==ExpeditionNodeKind.Boss);
   Assert.That(JourneyEncounterSelectionSystem.SelectAndAssign(
    plan,bossNode,"old_spire",10,new[]{normal,boss}),Is.SameAs(boss));
  }
  finally{
   UnityEngine.Object.DestroyImmediate(normal);
   UnityEngine.Object.DestroyImmediate(boss);
  }
 }

 [Test]
 public void JourneyEncounterSelection_IsDeterministicAcrossEquivalentPlans(){
  var firstProfile=CreateEncounterSelectionProfile("alpha",true,false,1);
  var secondProfile=CreateEncounterSelectionProfile("beta",true,false,3);
  try{
   var firstPlan=ExpeditionRoutePlanSystem.GenerateDefault("old_spire",4);
   var secondPlan=ExpeditionRoutePlanSystem.GenerateDefault("old_spire",4);
   var firstNode=firstPlan.floors[0].nodes.First(node=>node.kind==ExpeditionNodeKind.Battle);
   var secondNode=secondPlan.floors[0].nodes.Single(node=>node.id==firstNode.id);

   var first=JourneyEncounterSelectionSystem.SelectAndAssign(
    firstPlan,firstNode,"old_spire",12,new[]{secondProfile,firstProfile});
   var second=JourneyEncounterSelectionSystem.SelectAndAssign(
    secondPlan,secondNode,"old_spire",12,new[]{firstProfile,secondProfile});

   Assert.That(first.StableEncounterId,Is.EqualTo(second.StableEncounterId));
   Assert.That(firstNode.encounterId,Is.EqualTo(secondNode.encounterId));
  }
  finally{
   UnityEngine.Object.DestroyImmediate(firstProfile);
   UnityEngine.Object.DestroyImmediate(secondProfile);
  }
 }

 [Test]
 public void JourneyEncounterSelection_AuthoredRosterCoversEveryCurrentBattleNode(){
  var plan=ExpeditionRoutePlanSystem.GenerateDefault("old_spire");
  var profiles=PackspireResources.LoadAll<JourneyBattleEncounterProfile>("Data/Journey");

  var report=JourneyEncounterSelectionSystem.AuditCoverage(
   plan,"old_spire",profiles);

  Assert.That(report.errors,Is.Empty,string.Join("\n",report.errors));
  Assert.That(report.warnings,Is.Empty,string.Join("\n",report.warnings));
 }

 [Test]
 public void JourneyEnemyProductionPilot_HasRecurringLineageAndRegionalOriginals(){
  var profiles=PackspireResources.LoadAll<JourneyBattleEncounterProfile>("Data/Journey");

  var report=JourneyEnemyProductionAuditSystem.Audit(
   profiles,new[]{"ash_outskirts","drowned_archive"});

  Assert.That(report.errors,Is.Empty,string.Join("\n",report.errors));
  Assert.That(report.warnings,Is.Empty,string.Join("\n",report.warnings));
  var authored=profiles.Where(profile=>profile!=null&&profile.enemyDefinition!=null).ToArray();
  Assert.That(authored.Count(profile=>profile.enemyDefinition.origin==JourneyEnemyOrigin.RecurringLineage),
   Is.EqualTo(2));
  Assert.That(authored.Any(profile=>profile.StableEncounterId=="ash_furnace_bailiff_elite"&&
   profile.PopulationClass==JourneyEnemyPopulationClass.Elite),Is.True);
  Assert.That(authored.Any(profile=>profile.StableEncounterId=="drowned_archive_custodian_elite"&&
   profile.PopulationClass==JourneyEnemyPopulationClass.Elite),Is.True);
  var ashWatchman=authored.Single(profile=>profile.StableEncounterId=="ash_bell_watchman");
  var drownedWatchman=authored.Single(profile=>profile.StableEncounterId=="drowned_bell_watchman");
  Assert.That(ashWatchman.ResolvedTimeline,Is.Not.SameAs(drownedWatchman.ResolvedTimeline));
  Assert.That(ashWatchman.ResolvedTimeline.catalog,
   Is.SameAs(drownedWatchman.ResolvedTimeline.catalog));
  Assert.That(ashWatchman.ResolvedTimeline.patternIds.Intersect(
   drownedWatchman.ResolvedTimeline.patternIds).Count(),Is.EqualTo(5));
  Assert.That(ashWatchman.ResolvedTimeline.patternIds,Does.Contain("timing-p31"));
  Assert.That(drownedWatchman.ResolvedTimeline.patternIds,Does.Contain("timing-p32"));
 Assert.That(drownedWatchman.BuildTimelinePatterns()[0].Steps[0].Damage,
   Is.GreaterThan(ashWatchman.BuildTimelinePatterns()[0].Steps[0].Damage));
 }

 [Test]
 public void JourneyBattleFormationLayout_PreservesLegacyActorHeightRatios(){
  var normal=JourneyBattleFormationLayout.Resolve(BattleFormationScale.Normal,1);
  var small=JourneyBattleFormationLayout.Resolve(BattleFormationScale.Small,1);
  var large=JourneyBattleFormationLayout.Resolve(BattleFormationScale.Large,1);
  var boss=JourneyBattleFormationLayout.Resolve(BattleFormationScale.Boss,1);
  var multiple=JourneyBattleFormationLayout.Resolve(BattleFormationScale.Boss,3);

  Assert.That(normal.PlayerScale,Is.EqualTo(1f).Within(.001f));
  Assert.That(normal.EnemyScale,Is.EqualTo(1f).Within(.001f));
  Assert.That(small.PlayerScale,Is.EqualTo(360f/350f).Within(.001f));
  Assert.That(small.EnemyScale,Is.EqualTo(250f/390f).Within(.001f));
  Assert.That(large.PlayerScale,Is.EqualTo(315f/350f).Within(.001f));
  Assert.That(large.EnemyScale,Is.EqualTo(420f/390f).Within(.001f));
  Assert.That(boss.PlayerScale,Is.EqualTo(280f/350f).Within(.001f));
  Assert.That(boss.EnemyScale,Is.EqualTo(440f/390f).Within(.001f));
  Assert.That(multiple.PlayerScale,Is.EqualTo(286f/350f).Within(.001f));
  Assert.That(multiple.EnemyScale,Is.EqualTo(238f/390f).Within(.001f));
 }

 [TestCase("AshBellWatchman","ash-bell-watchman-v1")]
 [TestCase("DrownedBellWatchman","drowned-bell-watchman-v1")]
 [TestCase("AshChimneyScavenger","ash-chimney-scavenger-v1")]
 [TestCase("AshFurnaceBailiff","ash-furnace-bailiff-v1")]
 [TestCase("DrownedArchiveIndexer","drowned-archive-indexer-v1")]
 [TestCase("DrownedArchiveCustodian","drowned-archive-custodian-v1")]
 public void JourneyEnemyProductionPilot_ArtImportsAsGroundedSprite(
  string definitionName,string spriteName){
  var enemy=AssetDatabase.LoadAssetAtPath<JourneyEnemyDefinition>(
   $"Assets/Resources/Data/Journey/EnemyDefinitions/{definitionName}.asset");
  var sprite=AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>(
   $"Assets/Resources/Art/JourneyPrototype/Enemies/{spriteName}.png");

  Assert.That(enemy,Is.Not.Null);
  Assert.That(sprite,Is.Not.Null);
  Assert.That(sprite.pivot.y/sprite.rect.height,Is.EqualTo(.08f).Within(.01f));
   Assert.That(enemy.battleTargetHeight,Is.InRange(4f,5.5f));
   Assert.That(enemy.battleScale*sprite.bounds.size.y,
    Is.EqualTo(enemy.battleTargetHeight).Within(.03f));
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

 [Test]
 public void ExpeditionDefeat_KeepsStartingAndPackedNewItemsOnly(){
  var starting=new ItemInstance("sword"){uid="starting"};
  var packed=new ItemInstance("shield"){uid="packed-new"};
  var loose=new ItemInstance("herb"){uid="loose-new"};
  var unclaimed=new ItemInstance("ember"){uid="unclaimed-new"};
  var meta=new MetaSave{baseGold=10,stash=new(){LoadoutSystem.CloneItem(starting)}};
  var run=new RunState{
   gold=25,
   inventory=new(){starting,packed,loose},
   lootBag=new(){unclaimed},
   placements=new(){new Placement(packed.uid,0)},
   startingItemUids=new(){starting.uid}
  };

  var summary=ExpeditionLootSystem.Finalize(meta,run,ExpeditionEndReason.Defeat,1234);

  Assert.That(meta.stash.Select(item=>item.uid),Is.EquivalentTo(new[]{"starting","packed-new"}));
  Assert.That(summary.retainedNewItemUids,Is.EquivalentTo(new[]{"packed-new"}));
  Assert.That(summary.lostNewItemUids,Is.EquivalentTo(new[]{"loose-new","unclaimed-new"}));
  Assert.That(meta.baseGold,Is.EqualTo(10));
  Assert.That(meta.runs,Is.EqualTo(1));
  Assert.That(meta.wins,Is.EqualTo(0));
 }

 [Test]
 public void ExpeditionDefeat_RecordsAndPersistsHeirloomScar(){
  var heirloom=new ItemInstance("sword"){uid="heirloom"};
  var meta=new MetaSave{stash=new(){LoadoutSystem.CloneItem(heirloom)}};
  var run=new RunState{
   dungeon="old_spire",battlesWon=4,heirloomUid=heirloom.uid,
   inventory=new(){heirloom},startingItemUids=new(){heirloom.uid}
  };

  ExpeditionLootSystem.Finalize(meta,run,ExpeditionEndReason.Defeat,5678);

  var saved=meta.stash.Single(item=>item.uid==heirloom.uid);
  Assert.That(saved.history.defeats,Is.EqualTo(1));
  Assert.That(saved.scars,Has.Count.EqualTo(1));
  Assert.That(saved.scars[0].timestamp,Is.EqualTo(5678));
 }

 [TestCase(ExpeditionEndReason.Return,0)]
 [TestCase(ExpeditionEndReason.Clear,1)]
 public void SuccessfulExpedition_SecuresAllLootAndGold(ExpeditionEndReason reason,int expectedWins){
  var inventoryLoot=new ItemInstance("shield"){uid="inventory-new"};
  var bagLoot=new ItemInstance("ember"){uid="bag-new"};
  var meta=new MetaSave{baseGold=7,stash=new()};
  var run=new RunState{gold=13,inventory=new(){inventoryLoot},lootBag=new(){bagLoot}};

  var summary=ExpeditionLootSystem.Finalize(meta,run,reason);

  Assert.That(meta.stash.Select(item=>item.uid),Is.EquivalentTo(new[]{"inventory-new","bag-new"}));
  Assert.That(summary.retainedNewItemCount,Is.EqualTo(2));
  Assert.That(summary.lostNewItemCount,Is.EqualTo(0));
  Assert.That(meta.baseGold,Is.EqualTo(20));
  Assert.That(meta.runs,Is.EqualTo(1));
  Assert.That(meta.wins,Is.EqualTo(expectedWins));
 }

 [Test]
 public void ExpeditionRoutePlan_GeneratesThreeFloorsAndReconvergingBosses(){
  var rules=TestExpeditionRouteRules();

  var plan=ExpeditionRoutePlanSystem.Generate(1708,rules);
  var report=ExpeditionRoutePlanSystem.Validate(plan,rules);

  Assert.That(report.errors,Is.Empty,string.Join("\n",report.errors));
  Assert.That(plan.floors,Has.Count.EqualTo(3));
  Assert.That(plan.startNodeIds,Has.Count.EqualTo(2));
  foreach(var floor in plan.floors){
   Assert.That(floor.paths,Has.Count.EqualTo(3));
   Assert.That(floor.startNodeIds,Has.Count.EqualTo(2));
   Assert.That(floor.nodes.Count(node=>node.order==0),Is.EqualTo(2));
   Assert.That(floor.nodes.Count(node=>node.order>0&&node.kind!=ExpeditionNodeKind.Boss),
    Is.EqualTo(21));
   Assert.That(floor.nodes.Single(node=>node.id==floor.bossNodeId).kind,Is.EqualTo(ExpeditionNodeKind.Boss));
   Assert.That(floor.nodes.Where(node=>node.kind!=ExpeditionNodeKind.Boss)
    .All(node=>node.nextNodeIds.Count>=1&&node.nextNodeIds.Count<=2),Is.True);
   Assert.That(floor.nodes.Where(node=>node.order==7)
    .All(node=>node.nextNodeIds.SequenceEqual(new[]{floor.bossNodeId})),Is.True);
  }
  Assert.That(plan.floors[0].nodes.Single(node=>node.id==plan.floors[0].bossNodeId).nextNodeIds,
   Is.EquivalentTo(plan.floors[1].startNodeIds));
 }

 [Test]
 public void ExpeditionRoutePlan_RuntimeTraversalCanChangeTendencyAtEveryBranch(){
  var run=new RunState{dungeon="old_spire"};
  run.expeditionPlan=ExpeditionRoutePlanSystem.GenerateDefault(run.dungeon);

  while(!run.expeditionPlan.complete){
   var choices=ExpeditionRoutePlanSystem.Available(run.expeditionPlan);
   Assert.That(choices.Count,Is.InRange(1,2));
   var next=choices.OrderByDescending(node=>node.lane).First();
   Assert.That(ExpeditionRoutePlanSystem.Select(run.expeditionPlan,next.id),Is.True);
   Assert.That(ExpeditionRoutePlanSystem.CommitSelection(run,out _),Is.True);
   Assert.That(ExpeditionRoutePlanSystem.ResolveCurrent(run.expeditionPlan),Is.True);
  }

  Assert.That(run.expeditionPlan.elapsedDays,Is.EqualTo(18));
  Assert.That(run.expeditionPlan.committedNodeIds,Has.Count.EqualTo(27));
  Assert.That(run.expeditionPlan.complete,Is.True);
  Assert.That(run.expeditionPlan.currentFloorIndex,Is.EqualTo(2));
  Assert.That(ExpeditionRoutePlanSystem.Available(run.expeditionPlan),Is.Empty);
 }

 [Test]
 public void ExpeditionRoutePlan_DoesNotAdvanceOrResolveFromInvalidSelection(){
  var run=new RunState{dungeon="old_spire"};
  run.expeditionPlan=ExpeditionRoutePlanSystem.GenerateDefault(run.dungeon);

  Assert.That(ExpeditionRoutePlanSystem.Select(run.expeditionPlan,"not-a-node"),Is.False);
  Assert.That(ExpeditionRoutePlanSystem.CommitSelection(run,out _),Is.False);
  Assert.That(ExpeditionRoutePlanSystem.ResolveCurrent(run.expeditionPlan),Is.False);
  Assert.That(run.expeditionPlan.elapsedDays,Is.Zero);
 }

 [Test]
 public void ExpeditionJourney_CommitsGeneratedPathAndKeepsLegacyStateAsCompatibilityOnly(){
  var run=new RunState{dungeon="old_spire",hp=20,maxHp=20};
  run.expeditionPlan=ExpeditionRoutePlanSystem.GenerateDefault(run.dungeon);
  run.courierRoute=new CourierRouteState{nextSegmentDiscount=1};
  var first=ExpeditionJourneySystem.Available(run)
   .Single(node=>node.lane==0);

  Assert.That(ExpeditionJourneySystem.Select(run,first.id),Is.True);
  Assert.That(ExpeditionJourneySystem.Commit(run,out var committed,out string message),Is.True);

  Assert.That(committed.id,Is.EqualTo(first.id));
  Assert.That(run.expeditionPlan.elapsedDays,Is.Zero);
  Assert.That(run.courierRoute.daysElapsed,Is.Zero);
  Assert.That(run.courierRoute.currentNodeId,Is.EqualTo("dispatch"));
  Assert.That(run.courierRoute.awaitingResolution,Is.True);
  Assert.That(run.courierRoute.nextSegmentDiscount,Is.Zero);
  Assert.That(message,Does.Contain("到着"));

  Assert.That(ExpeditionJourneySystem.ResolveCurrent(
   run,new CourierLocationOutcome{message="地点を突破した。"},out _),Is.True);
  Assert.That(run.expeditionPlan.awaitingResolution,Is.False);
  Assert.That(run.courierRoute.awaitingResolution,Is.False);
  Assert.That(ExpeditionJourneySystem.Available(run),Has.Count.EqualTo(2));
 }

 [Test]
 public void ExpeditionJourney_PresentationMapsFloorsAndNodeKindsWithoutChangingGraph(){
  var plan=ExpeditionRoutePlanSystem.GenerateDefault("old_spire");
  var rest=plan.floors[1].nodes.First(node=>node.kind==ExpeditionNodeKind.Rest);
  var other=plan.floors[2].nodes.First(node=>node.kind==ExpeditionNodeKind.Other);
  var boss=plan.floors[2].nodes.Single(node=>node.kind==ExpeditionNodeKind.Boss);

  var restView=ExpeditionJourneySystem.PresentationNode(plan,rest);
  var otherView=ExpeditionJourneySystem.PresentationNode(plan,other);
  var bossView=ExpeditionJourneySystem.PresentationNode(plan,boss);

  Assert.That(restView.resolution,Is.EqualTo(CourierResolutionKind.Relay));
  Assert.That(restView.phase,Is.InRange(5,6));
  Assert.That(otherView.resolution,Is.EqualTo(CourierResolutionKind.Event));
  Assert.That(otherView.phase,Is.InRange(8,9));
  Assert.That(bossView.resolution,Is.EqualTo(CourierResolutionKind.Battle));
  Assert.That(bossView.phase,Is.EqualTo(10));
  Assert.That(plan.currentNodeId,Is.Empty);
 }

 [Test]
 public void ExpeditionRoutePlan_PacingSpansSecondStageBoundaryWithoutForcingThirdStageMaximum(){
  var rules=TestExpeditionRouteRules();
  var plan=ExpeditionRoutePlanSystem.Generate(1708,rules);

  var pacing=ExpeditionRoutePlanSystem.AnalyzePacing(plan);

  Assert.That(pacing.errors,Is.Empty,string.Join("\n",pacing.errors));
  Assert.That(pacing.warnings,Is.Empty,string.Join("\n",pacing.warnings));
  Assert.That(pacing.routeCombinationCount,Is.EqualTo(32768));
  Assert.That(pacing.fastestDays,Is.EqualTo(18));
  Assert.That(pacing.standardLowDays,Is.EqualTo(21));
  Assert.That(pacing.standardHighDays,Is.EqualTo(21));
  Assert.That(pacing.slowestDays,Is.EqualTo(24));
  Assert.That(pacing.fastestStage,Is.EqualTo(ExpeditionDayStage.Alert));
  Assert.That(pacing.standardLowStage,Is.EqualTo(ExpeditionDayStage.Alert));
  Assert.That(pacing.standardHighStage,Is.EqualTo(ExpeditionDayStage.Pursuit));
  Assert.That(pacing.slowestStage,Is.EqualTo(ExpeditionDayStage.Pursuit));
 }

 [Test]
 public void ExpeditionRoutePlan_ValidationRejectsARequestedDayRangeTheGraphCannotMeet(){
  var rules=TestExpeditionRouteRules();
  rules.minimumDaysPerFloor=7;

  var report=ExpeditionRoutePlanSystem.Validate(
   ExpeditionRoutePlanSystem.Generate(1708,rules),rules);

  Assert.That(report.errors.Any(error=>error.Contains("costs 6 days")),Is.True);
 }

 [Test]
 public void ExpeditionRoutePlan_IsDeterministicAndUsesSharedDayThresholds(){
  var rules=TestExpeditionRouteRules();
  var first=ExpeditionRoutePlanSystem.Generate(42,rules);
  var second=ExpeditionRoutePlanSystem.Generate(42,rules);

  string FirstKinds(ExpeditionRoutePlan plan)=>string.Join(",",plan.floors
   .SelectMany(floor=>floor.nodes.OrderBy(node=>node.id))
   .Select(node=>$"{node.id}:{node.kind}:{node.dayCost}:{string.Join("|",node.nextNodeIds)}"));

  Assert.That(FirstKinds(first),Is.EqualTo(FirstKinds(second)));
  Assert.That(ExpeditionRoutePlanSystem.DayStage(first,0),Is.EqualTo(ExpeditionDayStage.Quiet));
  Assert.That(ExpeditionRoutePlanSystem.DayStage(first,10),Is.EqualTo(ExpeditionDayStage.Quiet));
  Assert.That(ExpeditionRoutePlanSystem.DayStage(first,11),Is.EqualTo(ExpeditionDayStage.Alert));
  Assert.That(ExpeditionRoutePlanSystem.DayStage(first,20),Is.EqualTo(ExpeditionDayStage.Alert));
  Assert.That(ExpeditionRoutePlanSystem.DayStage(first,21),Is.EqualTo(ExpeditionDayStage.Pursuit));
  Assert.That(ExpeditionRoutePlanSystem.DayStage(first,35),Is.EqualTo(ExpeditionDayStage.Pursuit));
  Assert.That(ExpeditionRoutePlanSystem.DayStage(first,36),Is.EqualTo(ExpeditionDayStage.Pursuit));
 }

 [Test]
 public void ExpeditionRoutePlan_FourthStageRequiresDungeonOrEventEnablement(){
  var standard=ExpeditionRoutePlanSystem.Generate(42,TestExpeditionRouteRules());
  Assert.That(ExpeditionRoutePlanSystem.DayStage(standard,99),Is.EqualTo(ExpeditionDayStage.Pursuit));

  ExpeditionRoutePlanSystem.EnableFourthDayStage(standard);
  Assert.That(ExpeditionRoutePlanSystem.DayStage(standard,35),Is.EqualTo(ExpeditionDayStage.Pursuit));
  Assert.That(ExpeditionRoutePlanSystem.DayStage(standard,36),Is.EqualTo(ExpeditionDayStage.Anomaly));

  var specialRules=TestExpeditionRouteRules();
  specialRules.enableFourthDayStage=true;
  var specialDungeon=ExpeditionRoutePlanSystem.Generate(42,specialRules);
  Assert.That(ExpeditionRoutePlanSystem.DayStage(specialDungeon,36),Is.EqualTo(ExpeditionDayStage.Anomaly));
 }

 [Test]
 public void WardenRealtimeTimeline_AuditMeasuresPressureAndReactionLoad(){
  var profile=AssetDatabase.LoadAssetAtPath<RealtimeEnemyTimelineProfile>(
   "Assets/Resources/Data/Journey/WardenRealtimeTimeline.asset");

  var report=RealtimeEnemyTimelineAudit.Analyze(profile.BuildPatterns(),10d);

  Assert.That(report.cycleDuration,Is.EqualTo(69d).Within(.0001d));
  Assert.That(report.actionCount,Is.EqualTo(17));
  Assert.That(report.totalPotentialDamage,Is.EqualTo(151));
  Assert.That(report.reactionActionCount,Is.EqualTo(6));
  Assert.That(report.minimumTelegraphLead,Is.EqualTo(.7d).Within(.0001d));
  Assert.That(report.maximumActionsInWindow,Is.GreaterThan(0));
  Assert.That(report.maximumDamageInWindow,Is.GreaterThanOrEqualTo(12));
 Assert.That(report.maximumQuietSeconds,Is.GreaterThan(0d));
 }

 static RealtimeEnemyTimelinePattern RulePattern(
  string id,int weight,string cooldownGroup="",double cooldownSeconds=0d)=>
  new RealtimeEnemyTimelinePattern(
   id,2d,RealtimeEnemyPatternRole.Standard,weight,cooldownGroup,cooldownSeconds,
   0,0,Array.Empty<RealtimeEnemyPreviousPatternWeightContent>(),
   new RealtimeEnemyTimelineStep(1d,id+":action",1,.5d));

 static RealtimeEnemyPatternWeightContent Weight(string patternId,int percent)=>new(){
  patternId=patternId,
  weightPercent=percent
 };

 static JourneyBattleEncounterProfile CreateEncounterSelectionProfile(
  string encounterId,bool allowNormal,bool allowBoss,int weight=1){
  var profile=UnityEngine.ScriptableObject.CreateInstance<JourneyBattleEncounterProfile>();
  profile.encounterId=encounterId;
  profile.enemyId=encounterId+"-enemy";
  profile.displayName=encounterId;
  profile.minimumFloor=1;
  profile.maximumFloor=3;
  profile.minimumDayStage=ExpeditionDayStage.Quiet;
  profile.maximumDayStage=ExpeditionDayStage.Anomaly;
  profile.minimumLane=0;
  profile.maximumLane=2;
  profile.allowNormalBattle=allowNormal;
  profile.allowBossBattle=allowBoss;
  profile.selectionWeight=weight;
  return profile;
 }

 static ExpeditionRouteGenerationRules TestExpeditionRouteRules()=>new(){
  floorCount=3,
  columnsPerFloor=8,
  laneCount=3,
  minimumBattlesPerRoute=4,
  maximumBattlesPerRoute=8,
  minimumDaysPerFloor=6,
  maximumDaysPerFloor=8,
  minimumRouteCombinationsPerFloor=16
 };
}

#endif
