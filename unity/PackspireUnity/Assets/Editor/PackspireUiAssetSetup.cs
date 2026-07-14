using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire.Editor {
[InitializeOnLoad]
public static class PackspireUiAssetSetup {
 const string Folder="Assets/Resources/UI";
 const string PanelPath=Folder+"/PackspirePanelSettings.asset";

 static PackspireUiAssetSetup(){EditorApplication.delayCall+=EnsureAssets;}

 [MenuItem("Packspire/Repair UI Toolkit Assets")]
 public static void EnsureAssets(){
  if(!AssetDatabase.IsValidFolder("Assets/Resources"))AssetDatabase.CreateFolder("Assets","Resources");
  if(!AssetDatabase.IsValidFolder(Folder))AssetDatabase.CreateFolder("Assets/Resources","UI");
  var settings=AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelPath);
  if(settings==null){
   settings=ScriptableObject.CreateInstance<PanelSettings>();
   settings.name="PackspirePanelSettings";
   settings.scaleMode=PanelScaleMode.ScaleWithScreenSize;
   settings.referenceResolution=new Vector2Int(1280,720);
   settings.match=.5f;
   settings.sortingOrder=120;
   AssetDatabase.CreateAsset(settings,PanelPath);
  }else{
   settings.scaleMode=PanelScaleMode.ScaleWithScreenSize;
   settings.referenceResolution=new Vector2Int(1280,720);
   settings.match=.5f;
   settings.sortingOrder=120;
   EditorUtility.SetDirty(settings);
  }
  AssetDatabase.SaveAssets();
 }
}
}
