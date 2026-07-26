#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using System.Collections.Generic;
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
