using UnityEngine;

namespace Packspire {

public enum PuppetMotionKind {
 None,
 Breath,
 Face,
 Blink,
 Hair,
 Ahoge,
 Arm,
 Cloth,
 Legs,
 Door,
 Sign,
 Banner,
 Lamp,
 Awning,
 QuestBoard,
}

public readonly struct PuppetPartDef {
 public readonly string id,resource,parentId;
 public readonly int sortOrder;
 public readonly Vector2 pivot;
 public readonly PuppetMotionKind motion;
 public readonly float rotationAmplitude,positionAmplitude,scaleAmplitude,phase;

 public PuppetPartDef(
  string id,
  string resource,
  string parentId,
  int sortOrder,
  Vector2 pivot,
  PuppetMotionKind motion,
  float rotationAmplitude=0f,
  float positionAmplitude=0f,
  float scaleAmplitude=0f,
  float phase=0f){
  this.id=id;
  this.resource=resource;
  this.parentId=parentId;
  this.sortOrder=sortOrder;
  this.pivot=pivot;
  this.motion=motion;
  this.rotationAmplitude=rotationAmplitude;
  this.positionAmplitude=positionAmplitude;
  this.scaleAmplitude=scaleAmplitude;
  this.phase=phase;
 }
}

}
