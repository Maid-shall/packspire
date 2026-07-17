using UnityEngine;

namespace Packspire {
/// <summary>Data-only hub street layout. Add facilities or backdrop layers here as the district grows.</summary>
public static class HubPresentationCatalog {
 public const float OrthographicSize = 2.85f;
 public const float CharacterAnchorX = -2.05f;
 public const float CharacterBaseY = -1.42f;
 public const float CharacterHeight = 3.75f;
 public const float BuildingParallax = 1f;
 public const float EnterTolerance = 0.28f;

 /// <summary>Reserved world positions for future lots (no rendering yet).</summary>
 public static readonly float[] FuturePlotX = { -11.6f, 11.6f };

 public static readonly HubBackdropLayerDef[] BackdropLayers = {
  new("sky","far-background-v1","",0f,.04f,0f,9.5f,-120,false),
  new("distant-ridge","hub-road-v1","old-spire-terrain-v2",0f,.10f,-.15f,8.2f,-110,false),
  new("town-silhouette","hub-road-v1","old-spire-terrain-v2",0f,.18f,-.25f,6.8f,-100,false),
  new("mid-vista","hub-road-v1","old-spire-terrain-v2",0f,.32f,-.55f,5.4f,-85,false),
  new("street-backdrop","stone-road-v1","stone-road-v1",0f,.52f,-.95f,3.2f,-70,false),
  new("road","stone-road-v1","stone-road-v1",0f,.78f,-1.28f,1.55f,-55,true),
  new("curb","foreground-v1","stone-road-v1",0f,.95f,-1.38f,.95f,-45,false),
  new("street-props","foreground-v1","stone-road-v1",0f,1.18f,-1.48f,1.35f,18,false),
 };

 public static readonly HubFacilityDef[] Facilities = {
  new("gate","遠征門",ScreenId.Expedition,-5.8f,new[]{
   new HubBuildingLayerDef("foundation","base",0f,-1.02f,1.05f,-8,false),
   new HubBuildingLayerDef("body","body",0f,-.18f,1f,-4,false),
   new HubBuildingLayerDef("roof","roof",0f,.72f,.98f,-2,false),
   new HubBuildingLayerDef("sign","sign",.18f,.42f,.72f,1,false),
   new HubBuildingLayerDef("entrance","entrance",0f,-.38f,.88f,2,true),
   new HubBuildingLayerDef("prop","fg",.42f,-.72f,.62f,6,false),
  }),
  new("guild","中央広場",ScreenId.Status,0f,new[]{
   new HubBuildingLayerDef("foundation","base",0f,-1.02f,1.08f,-8,false),
   new HubBuildingLayerDef("body","body",0f,-.12f,1.04f,-4,false),
   new HubBuildingLayerDef("roof","roof",0f,.78f,1.02f,-2,false),
   new HubBuildingLayerDef("sign","sign",0f,.48f,.78f,1,false),
   new HubBuildingLayerDef("entrance","entrance",0f,-.34f,.92f,2,true),
   new HubBuildingLayerDef("prop","fg",-.36f,-.68f,.58f,6,false),
  }),
  new("forge","鍛冶場",ScreenId.Vault,5.8f,new[]{
   new HubBuildingLayerDef("foundation","base",0f,-1.02f,1.05f,-8,false),
   new HubBuildingLayerDef("body","body",0f,-.16f,1.02f,-4,false),
   new HubBuildingLayerDef("roof","roof",0f,.68f,.96f,-2,false),
   new HubBuildingLayerDef("sign","sign",-.16f,.38f,.70f,1,false),
   new HubBuildingLayerDef("entrance","entrance",0f,-.36f,.90f,2,true),
   new HubBuildingLayerDef("prop","fg",.38f,-.70f,.64f,6,false),
  }),
 };
}

public readonly struct HubBackdropLayerDef {
 public readonly string id,resource,fallback;
 public readonly float baseX,parallax,y,height;
 public readonly int sortOrder;
 public readonly bool tiled;
 public HubBackdropLayerDef(string id,string resource,string fallback,float baseX,float parallax,float y,float height,int sortOrder,bool tiled){
  this.id=id;this.resource=resource;this.fallback=fallback;this.baseX=baseX;this.parallax=parallax;this.y=y;this.height=height;this.sortOrder=sortOrder;this.tiled=tiled;
 }
}

public readonly struct HubBuildingLayerDef {
 public readonly string partKey,role;
 public readonly float localX,localY,scale;
 public readonly int sortBias;
 public readonly bool entrance;
 public HubBuildingLayerDef(string partKey,string role,float localX,float localY,float scale,int sortBias,bool entrance){
  this.partKey=partKey;this.role=role;this.localX=localX;this.localY=localY;this.scale=scale;this.sortBias=sortBias;this.entrance=entrance;
 }
 public string ArtName(string facilityId)=>$"hub-{facilityId}-v1-{partKey}";
}

public readonly struct HubFacilityDef {
 public readonly string id,label;
 public readonly ScreenId screen;
 public readonly float worldX;
 public readonly HubBuildingLayerDef[] layers;
 public HubFacilityDef(string id,string label,ScreenId screen,float worldX,HubBuildingLayerDef[] layers){
  this.id=id;this.label=label;this.screen=screen;this.worldX=worldX;this.layers=layers;
 }
}
}
