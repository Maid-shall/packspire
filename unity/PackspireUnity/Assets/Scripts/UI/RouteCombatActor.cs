using System.Collections.Generic;
using UnityEngine;

namespace Packspire {
public enum RouteActorPose { Idle, Run, Attack, Guard, Hit, Down }

/// <summary>2.5D actor: full-body sprite + dedicated oval ground shadow (never a squashed body clone).</summary>
public sealed class RouteCombatActor {
 public Transform Root { get; }
 readonly Transform visualRoot,shadow;
 readonly SpriteRenderer bodySr,shadowSr;
 readonly List<Sprite> sprites;
 readonly int layer;
 readonly bool facingRight;
 readonly float displayHeight;
 float breath,flashT,poseT;
 RouteActorPose pose=RouteActorPose.Idle;
 Vector3 restVisual;
 Vector3 shadowBaseScale;
 bool valid;
 static Sprite sharedOvalShadow;

 public RouteActorPose Pose=>pose;
 public bool IsValid=>valid;

 public static RouteCombatActor CreateHero(Transform parent,float displayHeight,int stageLayer,List<Sprite> spriteSink,List<Texture2D> texSink){
  var full=LoadPlain("Art/RouteKeyed/character-v1")
   ??LoadKeyed("Art/Prototype2_5D/character-v1",false,true,texSink);
  if(full==null){
   Debug.LogWarning("[RouteCombatActor] Hero full-body art missing — actor hidden.");
   return new RouteCombatActor(parent,"HeroActor",stageLayer,spriteSink,null,displayHeight,true,Color.white,false);
  }
  return new RouteCombatActor(parent,"HeroActor",stageLayer,spriteSink,full,displayHeight,true,Color.white,true);
 }

 public static RouteCombatActor CreateEnemy(Transform parent,float displayHeight,int stageLayer,List<Sprite> spriteSink,List<Texture2D> texSink){
  var full=LoadPlain("Art/RouteKeyed/character-v1")
   ??LoadKeyed("Art/Prototype2_5D/character-v1",false,true,texSink);
  if(full==null){
   Debug.LogWarning("[RouteCombatActor] Enemy full-body art missing — actor hidden.");
   return new RouteCombatActor(parent,"EnemyActor",stageLayer,spriteSink,null,displayHeight,false,new Color(.95f,.78f,.74f),false);
  }
  return new RouteCombatActor(parent,"EnemyActor",stageLayer,spriteSink,full,displayHeight,false,new Color(.95f,.78f,.74f),true);
 }

 static Texture2D LoadPlain(string path){
  var src=Resources.Load<Texture2D>(path);
  return PackspireChromaKey.Readable(src,null);
 }
 static Texture2D LoadKeyed(string path,bool keyGreen,bool keyBlack,List<Texture2D> sink){
  var src=Resources.Load<Texture2D>(path);
  if(src==null)return null;
  if(!keyGreen&&!keyBlack)return src;
  return PackspireChromaKey.Key(src,keyGreen,keyBlack,sink);
 }

 RouteCombatActor(
  Transform parent,string name,int stageLayer,List<Sprite> spriteSink,
  Texture2D bodyTex,float height,bool faceRight,Color tint,bool ok){
  layer=stageLayer;sprites=spriteSink;facingRight=faceRight;displayHeight=height;valid=ok;
  Root=new GameObject(name).transform;
  Root.SetParent(parent,false);
  SetLayer(Root.gameObject);

  shadow=new GameObject("Shadow").transform;
  shadow.SetParent(Root,false);
  SetLayer(shadow.gameObject);
  shadowSr=shadow.gameObject.AddComponent<SpriteRenderer>();
  shadowSr.sortingOrder=8;
  shadowSr.sprite=OvalShadowSprite(spriteSink);
  shadowSr.color=new Color(0f,0f,0f,.32f);
  // Feet sit at Root origin; oval lies just under the soles.
  shadow.localPosition=new Vector3(0f,0.02f,0f);
  float shadowW=displayHeight*.42f;
  float shadowH=displayHeight*.09f;
  float sx=shadowW/Mathf.Max(.01f,shadowSr.sprite.bounds.size.x);
  float sy=shadowH/Mathf.Max(.01f,shadowSr.sprite.bounds.size.y);
  shadowBaseScale=new Vector3(sx,sy,1f);
  shadow.localScale=shadowBaseScale;
  shadowSr.enabled=ok;

  visualRoot=new GameObject("VisualRoot").transform;
  visualRoot.SetParent(Root,false);
  SetLayer(visualRoot.gameObject);
  bodySr=visualRoot.gameObject.AddComponent<SpriteRenderer>();
  bodySr.sortingOrder=22;

  if(bodyTex!=null){
   ApplyBody(bodyTex,tint);
  } else {
   bodySr.enabled=false;
   shadowSr.enabled=false;
   Root.gameObject.SetActive(false);
  }

  if(!faceRight&&valid)Root.localScale=new Vector3(-1f,1f,1f);
  restVisual=visualRoot.localPosition;
 }

 void ApplyBody(Texture2D tex,Color tint){
  const float ppu=100f;
  // Pivot at feet so Root.y is the ground contact point.
  var sp=Sprite.Create(tex,new Rect(0,0,tex.width,tex.height),new Vector2(.5f,0f),ppu);
  sprites.Add(sp);
  bodySr.sprite=sp;
  bodySr.color=tint;
  float scale=displayHeight/Mathf.Max(.01f,sp.bounds.size.y);
  visualRoot.localScale=new Vector3(scale,scale,1f);
  visualRoot.localPosition=Vector3.zero;
 }

 public void SetPose(RouteActorPose next){
  if(!valid)return;
  pose=next;poseT=0f;
  if(next==RouteActorPose.Hit)flashT=.35f;
  if(next!=RouteActorPose.Down)visualRoot.localRotation=Quaternion.identity;
 }

 public void ResetCombatVisuals(){
  if(!valid)return;
  pose=RouteActorPose.Idle;poseT=0f;flashT=0f;
  visualRoot.localRotation=Quaternion.identity;
  visualRoot.localPosition=restVisual;
  if(bodySr!=null){var c=bodySr.color;c.a=1f;bodySr.color=c;}
  if(shadow!=null)shadow.localScale=shadowBaseScale;
 }

 public void Tick(float time,float dt,bool movingExplore){
  if(!valid||bodySr==null)return;
  poseT+=dt;
  if(flashT>0f)flashT-=dt;
  float flash=flashT>0f&&Mathf.Sin(flashT*40f)>0f?.35f:1f;
  var col=bodySr.color;col.a=pose==RouteActorPose.Down?.4f:flash;bodySr.color=col;

  if(movingExplore&&pose==RouteActorPose.Idle)pose=RouteActorPose.Run;
  if(!movingExplore&&pose==RouteActorPose.Run)pose=RouteActorPose.Idle;

  breath=Mathf.Sin(time*2.2f)*.03f;
  switch(pose){
   case RouteActorPose.Idle:
    visualRoot.localPosition=restVisual+new Vector3(0f,breath,0f);
    break;
   case RouteActorPose.Run:
    visualRoot.localPosition=restVisual+new Vector3(0f,Mathf.Abs(Mathf.Sin(time*10f))*.07f,0f);
    break;
   case RouteActorPose.Attack:{
    float a=Mathf.Clamp01(poseT/.22f);
    visualRoot.localPosition=restVisual+new Vector3((facingRight?1:-1)*Mathf.Sin(a*Mathf.PI)*.28f,0f,0f);
    if(poseT>.4f)pose=RouteActorPose.Idle;
    break;}
   case RouteActorPose.Guard:
    visualRoot.localPosition=restVisual+new Vector3((facingRight?-1:1)*.06f,0f,0f);
    break;
   case RouteActorPose.Hit:
    visualRoot.localPosition=restVisual+new Vector3((facingRight?-1:1)*Mathf.Sin(Mathf.Clamp01(poseT/.2f)*Mathf.PI)*.22f,0f,0f);
    if(poseT>.35f)pose=RouteActorPose.Idle;
    break;
   case RouteActorPose.Down:
    visualRoot.localRotation=Quaternion.Euler(0,0,facingRight?-55f:55f);
    visualRoot.localPosition=restVisual+new Vector3(0f,-.12f,0f);
    break;
  }
  // Shadow stays planted at feet and gently scales with run bob / attack lean.
  if(shadow!=null){
   float squash=pose==RouteActorPose.Run?1f+Mathf.Abs(Mathf.Sin(time*10f))*.06f:1f;
   if(pose==RouteActorPose.Attack)squash=1.08f;
   if(pose==RouteActorPose.Down)squash=.7f;
   shadow.localScale=new Vector3(shadowBaseScale.x*squash,shadowBaseScale.y*(2f-squash),1f);
   shadow.localPosition=new Vector3(visualRoot.localPosition.x*.15f,0.02f,0f);
  }
 }

 static Sprite OvalShadowSprite(List<Sprite> sink){
  if(sharedOvalShadow!=null)return sharedOvalShadow;
  const int w=64,h=24;
  var tex=new Texture2D(w,h,TextureFormat.ARGB32,false){name="route-oval-shadow"};
  var cols=new Color[w*h];
  float cx=(w-1)*.5f,cy=(h-1)*.5f;
  for(int y=0;y<h;y++)for(int x=0;x<w;x++){
   float nx=(x-cx)/Mathf.Max(.01f,cx),ny=(y-cy)/Mathf.Max(.01f,cy);
   float d=nx*nx+ny*ny;
   cols[y*w+x]=d>1f?Color.clear:new Color(1f,1f,1f,Mathf.Clamp01(1f-d)*.9f);
  }
  tex.SetPixels(cols);tex.Apply(false,true);
  sharedOvalShadow=Sprite.Create(tex,new Rect(0,0,w,h),new Vector2(.5f,.5f),64f);
  sink?.Add(sharedOvalShadow);
  return sharedOvalShadow;
 }

 void SetLayer(GameObject go){go.layer=layer;foreach(Transform c in go.transform)SetLayer(c.gameObject);}
}
}
