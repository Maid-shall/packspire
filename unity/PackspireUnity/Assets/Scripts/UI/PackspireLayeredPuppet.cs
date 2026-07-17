using System.Collections.Generic;
using UnityEngine;

namespace Packspire {

/// <summary>
/// Lightweight Live2D-style sprite puppet. Every texture shares one canvas;
/// transforms rotate around authored pivots while preserving perfect rest alignment.
/// </summary>
public sealed class PackspireLayeredPuppet {
 sealed class Part {
  public PuppetPartDef def;
  public Transform node;
  public SpriteRenderer renderer;
  public SpriteRenderer pivotMarker;
  public Vector3 restPosition;
  public Vector3 restScale;
  public Quaternion restRotation;
  public Vector2 pivotWorld;
 }

 const float PixelsPerUnit=100f;

 readonly Dictionary<string,Part> parts=new();
 readonly List<Part> orderedParts=new();
 readonly List<Sprite> ownedSprites;
 readonly int stageLayer;
 readonly Sprite pivotSprite;

 float movementAmount;
 float interactionAmount;
 float highlightedAmount;
 float enterAmount;
 float hoverDoorAmount;
 bool hoverDoor;
 float nextBlinkTime=1.7f;
 float blinkStarted=-10f;

 public Transform Root { get; }
 public bool IsValid=>orderedParts.Count>0;

 public PackspireLayeredPuppet(
  Transform parent,
  string name,
  IReadOnlyList<PuppetPartDef> definitions,
  float targetHeight,
  int layer,
  List<Sprite> spriteSink){
  stageLayer=layer;
  ownedSprites=spriteSink;
  Root=new GameObject(name).transform;
  Root.SetParent(parent,false);
  SetLayer(Root.gameObject);

  pivotSprite=Sprite.Create(
   Texture2D.whiteTexture,
   new Rect(0f,0f,Texture2D.whiteTexture.width,Texture2D.whiteTexture.height),
   new Vector2(.5f,.5f),
   PixelsPerUnit);
  ownedSprites.Add(pivotSprite);

  Texture2D reference=null;
  for(int i=0;i<definitions.Count&&reference==null;i++)
   reference=Resources.Load<Texture2D>(definitions[i].resource);
  if(reference==null)return;

  float scale=targetHeight/Mathf.Max(.01f,reference.height/PixelsPerUnit);
  float fullWidth=(reference.width/PixelsPerUnit)*scale;

  foreach(var definition in definitions){
   var texture=Resources.Load<Texture2D>(definition.resource);
   if(texture==null)continue;

   Transform parentNode=Root;
   Vector2 parentPivot=Vector2.zero;
   if(!string.IsNullOrEmpty(definition.parentId)&&parts.TryGetValue(definition.parentId,out var parentPart)){
    parentNode=parentPart.node;
    parentPivot=parentPart.pivotWorld;
   }

   var pivotWorld=new Vector2((definition.pivot.x-.5f)*fullWidth,definition.pivot.y*targetHeight);
   var node=new GameObject(definition.id+"-pivot").transform;
   node.SetParent(parentNode,false);
   node.localPosition=pivotWorld-parentPivot;
   SetLayer(node.gameObject);

   var sprite=Sprite.Create(
    texture,
    new Rect(0f,0f,texture.width,texture.height),
    new Vector2(.5f,0f),
    PixelsPerUnit);
   ownedSprites.Add(sprite);

   var spriteObject=new GameObject(definition.id);
   spriteObject.transform.SetParent(node,false);
   spriteObject.transform.localPosition=-new Vector3(pivotWorld.x,pivotWorld.y,0f);
   spriteObject.transform.localScale=Vector3.one*scale;
   SetLayer(spriteObject);

   var renderer=spriteObject.AddComponent<SpriteRenderer>();
   renderer.sprite=sprite;
   renderer.sortingOrder=definition.sortOrder;

   var markerObject=new GameObject(definition.id+"-pivot-marker");
   markerObject.transform.SetParent(node,false);
   markerObject.transform.localScale=Vector3.one*3.2f;
   SetLayer(markerObject);
   var marker=markerObject.AddComponent<SpriteRenderer>();
   marker.sprite=pivotSprite;
   marker.sortingOrder=110;
   marker.color=definition.motion==PuppetMotionKind.None
    ?new Color(.3f,.8f,1f,.9f)
    :new Color(1f,.35f,.2f,.95f);
   marker.enabled=false;

   var part=new Part{
    def=definition,
    node=node,
    renderer=renderer,
    pivotMarker=marker,
    restPosition=node.localPosition,
    restScale=node.localScale,
    restRotation=node.localRotation,
    pivotWorld=pivotWorld,
   };
   parts[definition.id]=part;
   orderedParts.Add(part);
  }
 }

 public void SetDebugVisible(bool visible){
  foreach(var part in orderedParts)
   if(part.pivotMarker!=null)part.pivotMarker.enabled=visible;
 }

 public void SetInteraction(bool highlighted,bool entering,float immediatePulse=0f){
  highlightedAmount=Mathf.Max(highlightedAmount,immediatePulse);
  if(entering)enterAmount=1f;
 }

 public void SetHover(bool hovered){hoverDoor=hovered;}

 public void Tick(float time,float deltaTime,bool moving,float direction,bool highlighted){
  movementAmount=Mathf.MoveTowards(movementAmount,moving?1f:0f,deltaTime*5.5f);
  highlightedAmount=Mathf.MoveTowards(highlightedAmount,highlighted?1f:0f,deltaTime*4f);
  interactionAmount=Mathf.MoveTowards(interactionAmount,highlightedAmount,deltaTime*5f);
  enterAmount=Mathf.MoveTowards(enterAmount,0f,deltaTime*1.3f);
  hoverDoorAmount=Mathf.MoveTowards(hoverDoorAmount,hoverDoor?1f:0f,deltaTime*(hoverDoor?2.4f:3f));
  UpdateBlink(time);

  float walk=Mathf.Sin(time*8.4f);
  float breathe=Mathf.Sin(time*1.55f);
  float breeze=Mathf.Sin(time*.82f);

  foreach(var part in orderedParts){
   part.node.localPosition=part.restPosition;
   part.node.localRotation=part.restRotation;
   part.node.localScale=part.restScale;
   part.renderer.color=Color.white;

   switch(part.def.motion){
    case PuppetMotionKind.Breath:
     part.node.localScale=new Vector3(
      1f-breathe*part.def.scaleAmplitude*.25f,
      1f+breathe*part.def.scaleAmplitude,
      1f);
     part.node.localPosition+=Vector3.up*(breathe*part.def.positionAmplitude);
     break;
    case PuppetMotionKind.Face:
     part.node.localRotation=Quaternion.Euler(0f,0f,breathe*part.def.rotationAmplitude);
     part.node.localPosition+=new Vector3(
      walk*movementAmount*part.def.positionAmplitude*.35f,
      Mathf.Abs(walk)*movementAmount*part.def.positionAmplitude,
      0f);
     break;
    case PuppetMotionKind.Blink:
     part.node.localScale=new Vector3(1f,BlinkScale(time),1f);
     break;
    case PuppetMotionKind.Hair:
     part.node.localRotation=Quaternion.Euler(
      0f,0f,
      Mathf.Sin(time*1.2f+part.def.phase)*part.def.rotationAmplitude+
      walk*movementAmount*direction*part.def.rotationAmplitude*.45f);
     break;
    case PuppetMotionKind.Ahoge:
     part.node.localRotation=Quaternion.Euler(
      0f,0f,
      Mathf.Sin(time*2.05f+part.def.phase)*part.def.rotationAmplitude+
      walk*movementAmount*direction*part.def.rotationAmplitude*.7f);
     break;
    case PuppetMotionKind.Arm:
     part.node.localRotation=Quaternion.Euler(
      0f,0f,
      walk*movementAmount*part.def.rotationAmplitude*Mathf.Cos(part.def.phase));
     break;
    case PuppetMotionKind.Cloth:
     part.node.localRotation=Quaternion.Euler(
      0f,0f,
      breeze*part.def.rotationAmplitude+
      walk*movementAmount*direction*part.def.rotationAmplitude*.55f);
     break;
    case PuppetMotionKind.Legs:
     part.node.localPosition+=Vector3.up*(Mathf.Abs(walk)*movementAmount*part.def.positionAmplitude);
     break;
    case PuppetMotionKind.Door:
     float open=Mathf.Clamp01(Mathf.Max(hoverDoorAmount,enterAmount));
     float eased=open*open*(3f-2f*open);
     part.node.localRotation=Quaternion.Euler(0f,part.def.phase*78f*eased,0f);
     break;
    case PuppetMotionKind.Sign:
     part.node.localRotation=Quaternion.Euler(
      0f,0f,
      Mathf.Sin(time*1.35f+part.def.phase)*part.def.rotationAmplitude*(.22f+interactionAmount*.78f));
     break;
    case PuppetMotionKind.Banner:
     part.node.localRotation=Quaternion.Euler(
      0f,0f,
      Mathf.Sin(time*1.1f+part.def.phase)*part.def.rotationAmplitude);
     part.node.localPosition+=Vector3.right*(breeze*part.def.positionAmplitude);
     break;
    case PuppetMotionKind.Lamp:
     float glow=.82f+.18f*Mathf.Sin(time*3.8f+part.def.phase);
     glow=Mathf.Clamp01(glow+interactionAmount*.18f);
     part.renderer.color=new Color(1f,.78f+.18f*glow,.42f+.45f*glow,glow);
     part.node.localScale=Vector3.one*(1f+interactionAmount*part.def.scaleAmplitude);
     break;
    case PuppetMotionKind.Awning:
     part.node.localRotation=Quaternion.Euler(
      0f,0f,
      breeze*part.def.rotationAmplitude*(.4f+interactionAmount*.6f));
     break;
    case PuppetMotionKind.QuestBoard:
     part.node.localPosition+=Vector3.up*(
      Mathf.Sin(time*1.45f+part.def.phase)*part.def.positionAmplitude*interactionAmount);
     part.node.localRotation=Quaternion.Euler(
      0f,0f,
      Mathf.Sin(time*1.1f+part.def.phase)*part.def.rotationAmplitude*interactionAmount);
     break;
   }
  }
 }

 void UpdateBlink(float time){
  if(time<nextBlinkTime)return;
  blinkStarted=time;
  float seed=Mathf.Abs(Mathf.Sin(time*12.9898f));
  nextBlinkTime=time+2.2f+seed*2.8f;
 }

 float BlinkScale(float time){
  float elapsed=time-blinkStarted;
  if(elapsed<0f||elapsed>.18f)return 1f;
  float normalized=elapsed/.18f;
  return normalized<.5f
   ?Mathf.Lerp(1f,.06f,normalized*2f)
   :Mathf.Lerp(.06f,1f,(normalized-.5f)*2f);
 }

 void SetLayer(GameObject value){value.layer=stageLayer;}
}
}
