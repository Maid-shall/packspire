#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
namespace Packspire.Editor {
public static class PackspireBuild {
 [MenuItem("PACKSPIRE/Create Bootstrap Scene")]
 public static void CreateScene(){Directory.CreateDirectory("Assets/Scenes");var scene=UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Single);new GameObject("PackspireGame").AddComponent<PackspireGame>();UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene,"Assets/Scenes/Main.unity");EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/Main.unity",true)};AssetDatabase.SaveAssets();}
 [MenuItem("PACKSPIRE/Build Web")]
 public static void BuildWeb(){CreateScene();Directory.CreateDirectory("Builds/Web");PlayerSettings.productName="PACKSPIRE";PlayerSettings.companyName="Maid-shall";PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Gzip;PlayerSettings.WebGL.decompressionFallback=true;var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/Main.unity"},locationPathName="Builds/Web",target=BuildTarget.WebGL,options=BuildOptions.None});if(report.summary.result!=BuildResult.Succeeded)throw new System.Exception("Web build failed");}
 public static void BatchSetup(){CreateScene();}
 public static void ValidateCore(){
  var run=new RunState{role="warrior"};
  var sword=new ItemInstance("sword");run.inventory.Add(sword);
  if(BackpackSystem.BuildDeck(run).Count!=4)throw new System.Exception("Role base deck must contain four cards");
  run.placements.Add(new Placement(sword.uid,0));
  if(!BackpackSystem.Analyze(sword,run.placements[0]).active)throw new System.Exception("Handle on outer edge must activate item");
  if(BackpackSystem.BuildDeck(run).Count!=5)throw new System.Exception("Active equipment must add one card");
  run.placements[0].anchor=8;
  if(BackpackSystem.Analyze(sword,run.placements[0]).active)throw new System.Exception("Interior handle must not activate item");
  if(BackpackSystem.BuildDeck(run).Count!=4)throw new System.Exception("Interior equipment must not add cards");
  run.placements[0].anchor=4;run.placements[0].rotation=1;
  if(!BackpackSystem.CanPlace(run,sword,4,1,sword.uid))throw new System.Exception("Rotated item should fit at tested edge");
  Debug.Log("PACKSPIRE_CORE_VALIDATION_OK");
 }
}
}
#endif
