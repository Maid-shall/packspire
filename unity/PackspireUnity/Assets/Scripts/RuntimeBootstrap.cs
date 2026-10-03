using UnityEngine;

namespace Packspire {
public static class RuntimeBootstrap {
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
 static void Start(){
  EnsureRuntimeCamera();
  var game=Object.FindFirstObjectByType<PackspireGame>();
  if(game==null)game=new GameObject("PackspireGame").AddComponent<PackspireGame>();
  if(Object.FindFirstObjectByType<PackspireUiFoundation>()==null)
   game.gameObject.AddComponent<PackspireUiFoundation>();
 }

 static void EnsureRuntimeCamera(){
  if(Object.FindFirstObjectByType<Camera>()!=null)return;
  var cameraObject=new GameObject("Packspire Runtime Camera"){
   hideFlags=HideFlags.HideAndDontSave
  };
  Object.DontDestroyOnLoad(cameraObject);
  var camera=cameraObject.AddComponent<Camera>();
  camera.clearFlags=CameraClearFlags.Nothing;
  camera.cullingMask=0;
  camera.depth=-100;
  camera.allowHDR=false;
  camera.allowMSAA=false;
  camera.useOcclusionCulling=false;
 }
}
}
