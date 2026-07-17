using UnityEngine;

namespace Packspire {
/// <summary>Hub street layout with layered parallax and close-up framing.</summary>
public static class HubPresentationCatalog {
 public const float OrthographicSize = 3.62f;
 public const float CameraCenterY = -0.12f;
 public const float CharacterAnchorX = -3.15f;
 public const float CharacterGroundY = -8.85f;
 public const float CharacterDisplayHeight = 12.40f;
 public const float GroundY = -1.34f;
 public const float EntranceScreenX = 1.12f;
 public const float EnterTolerance = 0.40f;
 public const float BuildingParallax = 0.66f;
 public const float CoverWidthScale = 1.62f;
 public const float CoverHeightScale = 1.38f;
 public const float ScrollPad = 2.4f;

 public static readonly HubBackdropLayerDef[] BackdropLayers = {
  new("sky","Art/Hub/hub-sky-band-v1","Art/Prototype2_5D/far-background-v1",0f,.05f,0.62f,8.4f,-130,false,false,false,new Color(.88f,.90f,.96f)),
  new("vista","Art/Hub/hub-vista-mid-v1","Art/Prototype2_5D/midground-v1",0f,.16f,0.12f,7.2f,-112,true,true,true,new Color(.96f,.97f,1f)),
  new("district","Art/Hub/hub-road-v1",null,0f,.30f,-0.02f,6.8f,-98,false,false,false,new Color(1f,1f,1f)),
  new("floor","Art/Hub/hub-street-floor-v1",null,0f,.56f,-0.92f,2.35f,-72,true,true,true,new Color(1f,1f,1f)),
  new("curb","Art/Prototype2_5D/foreground-v1",null,0f,.90f,-1.02f,1.55f,-36,true,true,true,new Color(1f,1f,1f,.72f)),
 };

 public static readonly PuppetPartDef[] CharacterPuppet = {
  new("legs","Art/HubRig/Character/character-legs",null,61,new Vector2(.5f,.39f),PuppetMotionKind.Legs,positionAmplitude:.035f),
  new("torso","Art/HubRig/Character/character-torso",null,63,new Vector2(.5f,.48f),PuppetMotionKind.Breath,positionAmplitude:.012f,scaleAmplitude:.012f),
  new("back-hair","Art/HubRig/Character/character-back-hair","torso",62,new Vector2(.47f,.80f),PuppetMotionKind.Hair,rotationAmplitude:2.4f,phase:.6f),
  new("face","Art/HubRig/Character/character-face","torso",66,new Vector2(.51f,.79f),PuppetMotionKind.Face,rotationAmplitude:.65f,positionAmplitude:.018f),
  new("eyes","Art/HubRig/Character/character-eyes","face",68,new Vector2(.51f,.82f),PuppetMotionKind.Blink),
  new("front-hair","Art/HubRig/Character/character-front-hair","face",69,new Vector2(.50f,.82f),PuppetMotionKind.Hair,rotationAmplitude:1.8f,phase:1.2f),
  new("ahoge","Art/HubRig/Character/character-ahoge","front-hair",71,new Vector2(.50f,.92f),PuppetMotionKind.Ahoge,rotationAmplitude:4.8f,phase:.35f),
  new("arm-left","Art/HubRig/Character/character-arm-left","torso",65,new Vector2(.34f,.71f),PuppetMotionKind.Arm,rotationAmplitude:2.2f),
  new("arm-right","Art/HubRig/Character/character-arm-right","torso",70,new Vector2(.66f,.71f),PuppetMotionKind.Arm,rotationAmplitude:2.2f,phase:3.14159f),
  new("cloth","Art/HubRig/Character/character-cloth","torso",72,new Vector2(.50f,.52f),PuppetMotionKind.Cloth,rotationAmplitude:1.9f,phase:.8f),
 };

 public static readonly PuppetPartDef[] GuildPuppet = {
  new("underpaint","Art/HubRig/Guild/guild-underpaint",null,15,new Vector2(.5f,.4f),PuppetMotionKind.None),
  new("body","Art/HubRig/Guild/guild-body",null,17,new Vector2(.5f,.32f),PuppetMotionKind.None),
  new("foundation","Art/HubRig/Guild/guild-foundation",null,18,new Vector2(.5f,.10f),PuppetMotionKind.None),
  new("roof","Art/HubRig/Guild/guild-roof",null,19,new Vector2(.5f,.62f),PuppetMotionKind.None),
  new("columns","Art/HubRig/Guild/guild-columns",null,21,new Vector2(.5f,.34f),PuppetMotionKind.None),
  new("doorway","Art/HubRig/Guild/guild-doorway",null,24,new Vector2(.50f,.18f),PuppetMotionKind.None),
  new("door-left","Art/HubRig/Guild/guild-door-left",null,25,new Vector2(.445f,.68f),PuppetMotionKind.Door,phase:-1f),
  new("door-right","Art/HubRig/Guild/guild-door-right",null,26,new Vector2(.575f,.68f),PuppetMotionKind.Door,phase:1f),
  new("sign","Art/HubRig/Guild/guild-sign",null,27,new Vector2(.50f,.70f),PuppetMotionKind.Sign,rotationAmplitude:1.8f,phase:.4f),
  new("awning","Art/HubRig/Guild/guild-awning",null,28,new Vector2(.67f,.38f),PuppetMotionKind.Awning,rotationAmplitude:1.2f,phase:.9f),
  new("banners","Art/HubRig/Guild/guild-banners",null,29,new Vector2(.50f,.55f),PuppetMotionKind.Banner,rotationAmplitude:1.5f,positionAmplitude:.018f,phase:1.4f),
  new("lamps","Art/HubRig/Guild/guild-lamps",null,31,new Vector2(.50f,.44f),PuppetMotionKind.Lamp,scaleAmplitude:.05f,phase:.2f),
  new("questboard","Art/HubRig/Guild/guild-questboard",null,34,new Vector2(.20f,.12f),PuppetMotionKind.QuestBoard,rotationAmplitude:.9f,positionAmplitude:.025f,phase:.7f),
 };

 public static readonly PuppetPartDef[] GatePuppet = {
  new("underpaint","Art/HubRig/Gate/gate-underpaint",null,15,new Vector2(.5f,.4f),PuppetMotionKind.None),
  new("body","Art/HubRig/Gate/gate-body",null,17,new Vector2(.5f,.34f),PuppetMotionKind.None),
  new("foundation","Art/HubRig/Gate/gate-foundation",null,18,new Vector2(.5f,.10f),PuppetMotionKind.None),
  new("roof","Art/HubRig/Gate/gate-roof",null,19,new Vector2(.5f,.68f),PuppetMotionKind.None),
  new("doorway","Art/HubRig/Gate/gate-doorway",null,24,new Vector2(.50f,.18f),PuppetMotionKind.None),
  new("door-left","Art/HubRig/Gate/gate-door-left",null,25,new Vector2(.42f,.62f),PuppetMotionKind.Door,phase:-1f),
  new("door-right","Art/HubRig/Gate/gate-door-right",null,26,new Vector2(.58f,.62f),PuppetMotionKind.Door,phase:1f),
  new("banners","Art/HubRig/Gate/gate-banners",null,28,new Vector2(.50f,.58f),PuppetMotionKind.Banner,rotationAmplitude:1.4f,positionAmplitude:.014f,phase:1.1f),
  new("lamps","Art/HubRig/Gate/gate-lamps",null,30,new Vector2(.50f,.46f),PuppetMotionKind.Lamp,scaleAmplitude:.04f,phase:.3f),
 };

 public static readonly PuppetPartDef[] ForgePuppet = {
  new("underpaint","Art/HubRig/Forge/forge-underpaint",null,15,new Vector2(.5f,.4f),PuppetMotionKind.None),
  new("body","Art/HubRig/Forge/forge-body",null,17,new Vector2(.5f,.34f),PuppetMotionKind.None),
  new("foundation","Art/HubRig/Forge/forge-foundation",null,18,new Vector2(.5f,.10f),PuppetMotionKind.None),
  new("roof","Art/HubRig/Forge/forge-roof",null,19,new Vector2(.5f,.66f),PuppetMotionKind.None),
  new("doorway","Art/HubRig/Forge/forge-doorway",null,24,new Vector2(.50f,.18f),PuppetMotionKind.None),
  new("door-left","Art/HubRig/Forge/forge-door-left",null,25,new Vector2(.43f,.60f),PuppetMotionKind.Door,phase:-1f),
  new("door-right","Art/HubRig/Forge/forge-door-right",null,26,new Vector2(.57f,.60f),PuppetMotionKind.Door,phase:1f),
  new("sign","Art/HubRig/Forge/forge-sign",null,27,new Vector2(.50f,.72f),PuppetMotionKind.Sign,rotationAmplitude:1.6f,phase:.5f),
  new("lamps","Art/HubRig/Forge/forge-lamps",null,30,new Vector2(.50f,.44f),PuppetMotionKind.Lamp,scaleAmplitude:.06f,phase:.8f),
  new("awning","Art/HubRig/Forge/forge-awning",null,31,new Vector2(.68f,.40f),PuppetMotionKind.Awning,rotationAmplitude:1.1f,phase:1.0f),
 };

 public static readonly HubFacilityDef[] Facilities = {
  new("gate","遠征門",ScreenId.Expedition,"Art/Hub/hub-gate-v1",-8.6f,5.45f,true,GatePuppet),
  new("guild","中央広場",ScreenId.Status,"Art/Hub/hub-guild-close-v2",0f,8.80f,true,GuildPuppet),
  new("forge","鍛冶場",ScreenId.Vault,"Art/Hub/hub-forge-v1",8.6f,5.35f,true,ForgePuppet),
 };

 /// <summary>Hook for future save-driven unlocks. Currently returns startsUnlocked.</summary>
 public static bool IsFacilityUnlocked(HubFacilityDef facility,MetaSave meta)=>facility.startsUnlocked;

 public static float ScrollMinFor(System.Collections.Generic.IReadOnlyList<HubFacilityDef> active){
  if(active==null||active.Count==0)return -ScrollPad;
  float min=active[0].worldX;
  for(int i=1;i<active.Count;i++)min=Mathf.Min(min,active[i].worldX);
  return (min-EntranceScreenX)/BuildingParallax-ScrollPad;
 }

 public static float ScrollMaxFor(System.Collections.Generic.IReadOnlyList<HubFacilityDef> active){
  if(active==null||active.Count==0)return ScrollPad;
  float max=active[0].worldX;
  for(int i=1;i<active.Count;i++)max=Mathf.Max(max,active[i].worldX);
  return (max-EntranceScreenX)/BuildingParallax+ScrollPad;
 }
}

public readonly struct HubBackdropLayerDef {
 public readonly string id,resource,fallback;
 public readonly float baseX,parallax,y,coverHeight;
 public readonly int sortOrder;
 public readonly bool tiled,keyBlack,keyGreen;
 public readonly Color tint;
 public HubBackdropLayerDef(string id,string resource,string fallback,float baseX,float parallax,float y,float coverHeight,int sortOrder,bool tiled,bool keyBlack,bool keyGreen,Color tint){
  this.id=id;this.resource=resource;this.fallback=fallback;this.baseX=baseX;this.parallax=parallax;this.y=y;this.coverHeight=coverHeight;this.sortOrder=sortOrder;this.tiled=tiled;this.keyBlack=keyBlack;this.keyGreen=keyGreen;this.tint=tint;
 }
}

public readonly struct HubFacilityDef {
 public readonly string id,label,texturePath;
 public readonly ScreenId screen;
 public readonly float worldX,height;
 public readonly bool startsUnlocked;
 public readonly PuppetPartDef[] puppetParts;
 public HubFacilityDef(string id,string label,ScreenId screen,string texturePath,float worldX,float height,bool startsUnlocked=true,PuppetPartDef[] puppetParts=null){
  this.id=id;this.label=label;this.screen=screen;this.texturePath=texturePath;this.worldX=worldX;this.height=height;this.startsUnlocked=startsUnlocked;this.puppetParts=puppetParts;
 }
 public bool HasPuppet=>puppetParts!=null&&puppetParts.Length>0;
}
}
