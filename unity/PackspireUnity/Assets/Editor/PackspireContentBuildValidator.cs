#if UNITY_EDITOR
using Packspire;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

public sealed class PackspireContentBuildValidator : IPreprocessBuildWithReport {
 public int callbackOrder=>-1000;

 public void OnPreprocessBuild(BuildReport report){
  var database=AssetDatabase.LoadAssetAtPath<PackspireContentDatabase>(
   "Assets/Resources/Packspire/PackspireContentDatabase.asset");
  var validation=PackspireContent.Validate(database);
  if(!validation.IsValid)
   throw new BuildFailedException($"PACKSPIRE content validation failed before build: {string.Join(" | ",validation.errors)}");
 }
}
#endif
