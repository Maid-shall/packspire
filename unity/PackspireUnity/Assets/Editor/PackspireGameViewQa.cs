using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Packspire.Editor {
/// <summary>Editor-only helpers for reviewing the 1280x720 UI reference frame.</summary>
public static class PackspireGameViewQa {
 const string PreviousSizeKey="PACKSPIRE_QA_PREVIOUS_GAME_VIEW_SIZE";
 const BindingFlags AnyInstance=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;

 [MenuItem("Packspire/QA/Game View/Use 1280x720 Reference")]
 public static void UseReferenceResolution(){
  if(!TrySelectResolution(1280,720,out var message))Debug.LogWarning($"[PackspireQA] {message}");
  else Debug.Log($"[PackspireQA] {message}");
 }

 [MenuItem("Packspire/QA/Game View/Restore Previous Size")]
 public static void RestorePreviousSize(){
  var gameView=FindGameView();
  if(gameView==null||!SessionState.GetBool(PreviousSizeKey+"_SET",false))return;
  if(TrySetSelectedSize(gameView,SessionState.GetInt(PreviousSizeKey,0))){
   SessionState.EraseInt(PreviousSizeKey);
   SessionState.EraseBool(PreviousSizeKey+"_SET");
   gameView.Repaint();
  }
 }

 public static bool TrySelectResolution(int width,int height,out string message){
  var gameView=FindGameView();
  if(gameView==null){message="Game Viewが見つかりません。";return false;}
  if(!TryGetSizeGroup(out var group,out message))return false;
  var groupType=group.GetType();
  var countMethod=groupType.GetMethod("GetTotalCount",AnyInstance);
  var sizeMethod=groupType.GetMethod("GetGameViewSize",AnyInstance);
  if(countMethod==null||sizeMethod==null){message="Game Viewサイズ一覧APIが見つかりません。";return false;}
  int count=(int)countMethod.Invoke(group,null);
  int targetIndex=-1;
  for(int index=0;index<count;index++){
   var size=sizeMethod.Invoke(group,new object[]{index});
   if(ReadInt(size,"width")==width&&ReadInt(size,"height")==height){targetIndex=index;break;}
  }
  if(targetIndex<0&&!TryAddFixedSize(group,width,height,out targetIndex,out message))return false;
  int current=ReadSelectedSize(gameView);
  if(!SessionState.GetBool(PreviousSizeKey+"_SET",false)){
   SessionState.SetInt(PreviousSizeKey,current);
   SessionState.SetBool(PreviousSizeKey+"_SET",true);
  }
  if(!TrySetSelectedSize(gameView,targetIndex)){message="Game Viewサイズを変更できませんでした。";return false;}
  gameView.maximized=true;
  gameView.Focus();
  gameView.Repaint();
  message=$"Game Viewを{width}x{height}（index {targetIndex}）へ切り替えました。";
  return true;
 }

 static bool TryAddFixedSize(object group,int width,int height,out int index,out string message){
  index=-1;
  var editorAssembly=typeof(EditorWindow).Assembly;
  var sizeType=editorAssembly.GetType("UnityEditor.GameViewSize");
  var kindType=editorAssembly.GetType("UnityEditor.GameViewSizeType");
  var addMethod=group.GetType().GetMethod("AddCustomSize",AnyInstance);
  if(sizeType==null||kindType==null||addMethod==null){message="固定Game ViewサイズAPIが見つかりません。";return false;}
  try{
   var fixedKind=Enum.Parse(kindType,"FixedResolution");
   var size=Activator.CreateInstance(
    sizeType,AnyInstance,null,new object[]{fixedKind,width,height,$"Packspire {width}x{height}"},null
   );
   addMethod.Invoke(group,new[]{size});
   var countMethod=group.GetType().GetMethod("GetTotalCount",AnyInstance);
   index=(int)countMethod.Invoke(group,null)-1;
   message="";
   return index>=0;
  }catch(Exception exception){
   message=$"{width}x{height}固定サイズを追加できませんでした: {exception.GetBaseException().Message}";
   return false;
  }
 }

 static EditorWindow FindGameView()=>Resources.FindObjectsOfTypeAll<EditorWindow>()
  .FirstOrDefault(window=>window!=null&&window.GetType().FullName=="UnityEditor.GameView");

 static bool TryGetSizeGroup(out object group,out string message){
  group=null;
  var editorAssembly=typeof(EditorWindow).Assembly;
  var sizesType=editorAssembly.GetType("UnityEditor.GameViewSizes");
  if(sizesType==null){message="GameViewSizes APIが見つかりません。";return false;}
  var singletonType=typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
  var instance=singletonType.GetProperty("instance",BindingFlags.Static|BindingFlags.Public)?.GetValue(null);
  var getGroup=sizesType.GetMethod("GetGroup",AnyInstance);
  if(instance==null||getGroup==null){message="Game Viewサイズグループを取得できません。";return false;}
  group=getGroup.Invoke(instance,new object[]{GameViewSizeGroupType.Standalone});
  message=group==null?"Standaloneサイズグループがありません。":"";
  return group!=null;
 }

 static int ReadInt(object target,string name){
  if(target==null)return -1;
  var type=target.GetType();
  var property=type.GetProperty(name,AnyInstance);
  if(property?.GetValue(target) is int propertyValue)return propertyValue;
  var field=type.GetField(name,AnyInstance);
  return field?.GetValue(target) is int fieldValue?fieldValue:-1;
 }

 static int ReadSelectedSize(EditorWindow gameView){
  var type=gameView.GetType();
  var property=type.GetProperty("selectedSizeIndex",AnyInstance);
  if(property?.GetValue(gameView) is int propertyValue)return propertyValue;
  var field=type.GetField("m_SelectedSizeIndex",AnyInstance);
  return field?.GetValue(gameView) is int fieldValue?fieldValue:0;
 }

 static bool TrySetSelectedSize(EditorWindow gameView,int index){
  var type=gameView.GetType();
  var property=type.GetProperty("selectedSizeIndex",AnyInstance);
  if(property?.CanWrite==true){property.SetValue(gameView,index);return true;}
  var field=type.GetField("m_SelectedSizeIndex",AnyInstance);
  if(field==null)return false;
  field.SetValue(gameView,index);
  return true;
 }
}
}
