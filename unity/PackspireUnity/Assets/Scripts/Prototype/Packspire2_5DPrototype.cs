using UnityEngine;

namespace Packspire {
/// <summary>Standalone 2.5D walk prototype toggled with F8. Separate from the hub UI screen.</summary>
public sealed class Packspire2_5DPrototype : MonoBehaviour {
 public static Packspire2_5DPrototype Instance { get; private set; }

 PackspirePresentationStage stage;
 bool active;

 public bool Active=>active;

 void Awake(){
  if(Instance!=null&&Instance!=this){Destroy(this);return;}
  Instance=this;
  var stageGo=new GameObject("PrototypePresentationStage");
  stageGo.transform.SetParent(transform,false);
  stage=stageGo.AddComponent<PackspirePresentationStage>();
  SetActive(false);
 }

 void OnDestroy(){if(Instance==this)Instance=null;}

 void Update(){
  if(UnityEngine.Input.GetKeyDown(KeyCode.F8))Toggle();
  if(!active)return;
  if(UnityEngine.Input.GetKeyDown(KeyCode.Escape))SetActive(false);
  stage.SetMoveInput(ReadMoveInput());
  stage.Tick();
 }

 public void Toggle(){SetActive(!active);}

 public void SetActive(bool value){
  active=value;
  enabled=active;
  stage.enabled=active;
  if(active){
   var game=GetComponent<PackspireGame>();
   if(game!=null&&game.UiMeta!=null)stage.ConfigureCharacter(game.UiCharacterArt,game.UiMeta.body,game.UiMeta.hair);
  }
 }

 void OnGUI(){
  if(!active||stage.RenderTarget==null)return;
  Rect full=new(0,0,Screen.width,Screen.height);
  GUI.color=Color.white;
  GUI.DrawTexture(full,stage.RenderTarget,ScaleMode.ScaleAndCrop);
  GUI.color=new Color(0,0,0,.55f);
  GUI.DrawTexture(new Rect(18,18,420,92),Texture2D.whiteTexture);
  GUI.color=Color.white;
  GUI.Label(new Rect(34,28,390,28),"2.5D 試作（F8で閉じる）",new GUIStyle(GUI.skin.label){fontSize=22,fontStyle=FontStyle.Bold,normal={textColor=new Color(1f,.88f,.55f)}});
  GUI.Label(new Rect(34,58,390,44),"← → / A D　ドラッグ　ホイール\nEsc で終了",new GUIStyle(GUI.skin.label){fontSize=15,normal={textColor=new Color(.86f,.84f,.74f)}});
 }

 static float ReadMoveInput(){
  float value=0f;
  if(Input.GetKey(KeyCode.RightArrow)||Input.GetKey(KeyCode.D))value+=1f;
  if(Input.GetKey(KeyCode.LeftArrow)||Input.GetKey(KeyCode.A))value-=1f;
  return value;
 }
}
}
