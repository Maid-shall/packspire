using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Packspire {
public enum RouteSceneTemplate {
 StraightRoad, LeftRightFork, ThreeWayFork, Gate, BuildingEntrance,
 BreachWall, HiddenPassage, Courtyard, InteriorCorridor, Landmark,
}

public enum RouteExitVisualType {
 Road, Gate, Door, BreachRubble, HiddenCrack, BuildingEntrance, Stairs,
}

/// <summary>Screen-space exit placement. Matched to graph neighbors; never mutates graph state.</summary>
[Serializable]
public class RouteExitPresentation {
 public int targetNodeId=-1; // -1 = bind by slot order to sorted neighbors
 public RouteExitVisualType visualType=RouteExitVisualType.Road;
 public Vector2 worldAnchor=new(1.5f,-0.4f);
 public Vector2 clickSize=new(1.4f,2.2f);
 public Vector2 labelOffset=new(0f,-0.85f);
 public Vector2 popupOffset=new(0f,0.9f);
 public bool requireConfirm;
 public string cueResource="Art/RouteKeyed/placeholders/exit-glow";
}

/// <summary>Cell presentation recipe for 2.5D route (resource paths only — swap art later).</summary>
[Serializable]
public class RouteCellPresentation {
 public int cellId;
 public RouteSceneTemplate template=RouteSceneTemplate.StraightRoad;
 public string skyResource="Art/Hub/hub-sky-band-v1";
 public string farResource="Art/RouteKeyed/far-background-v1";
 public string midResource="Art/Hub/hub-road-v1";
 public string groundResource="Art/RouteKeyed/hub-street-floor-v1";
 public string landmarkResource;
 public string foregroundResource="Art/RouteKeyed/foreground-v1";
 public float landmarkX=1.55f;
 public float landmarkHeight=3.6f;
 public bool flipLandmark;
 public bool keyLandmarkGreen;
 public bool keyMidGreen;
 public bool keyGroundGreen;
 public bool keyFarGreen;
 public bool keyFarBlack;
 public bool keyForegroundBlack;
 public Color grade=new(1f,1f,1f,1f);
 public float fogAlpha=.22f;
 public List<RouteExitPresentation> exits=new();
}

/// <summary>Outer-bailey 2.5D route prototype presentation + separate test map (does not mutate old-spire JSON).</summary>
public static class ExplorationRouteCatalog {
 public const string SliceMapId="route_slice_v1";
 public const float OrthographicSize=3.55f;
 public const float CombatOrtho=3.35f;
 public const float CameraCenterY=-0.1f;
 public const float CombatCameraY=0.12f;
 public const float CharacterAnchorX=-3.05f;
 public const float CharacterCombatX=-2.55f;
 public const float HeroCombatX=CharacterCombatX;
 public const float EnemyCombatX=2.65f;
 public const float CharacterGroundY=-2.85f;
 public const float CharacterDisplayHeight=5.2f;
 public const float EnemyDisplayHeight=4.6f;
 public const float GroundY=-1.45f;
 public const float ExitY=-0.55f;
 public const float MoveScrollDistance=4.2f;
 public static bool ShowRouteDebugGizmos;

 public static readonly int[] PrototypeCellIds={4,23,5,11,10,18,19,6,33,17};
 public static readonly HashSet<int> PrototypeSet=new(PrototypeCellIds);

 public const int CellEntrance=4;
 public const int CellFork=23;
 public const int CellBreachFrom=5;
 public const int CellBreachTo=6;
 public const int CellHiddenFrom=11;
 public const int CellHiddenTo=33;
 public const int CellBattle=10;
 public const int CellBattle2=18;
 public const int CellBattle3=19;
 public const int CellEvent=17;

 public struct RouteLayerDef {
  public string id,resource,fallback;
  public float parallax,y,height;
  public int sort;
  public Color tint;
  public bool cover;
  public RouteLayerDef(string id,string resource,string fallback,float parallax,float y,float height,int sort,Color tint,bool cover=false){
   this.id=id;this.resource=resource;this.fallback=fallback;this.parallax=parallax;this.y=y;this.height=height;this.sort=sort;this.tint=tint;this.cover=cover;
  }
 }

 public static readonly RouteLayerDef[] BaseLayers={
  new("sky","Art/Hub/hub-sky-band-v1","Art/RouteKeyed/far-background-v1",.04f,0f,8.2f,-120,new Color(.86f,.88f,.94f),true),
  new("far","Art/RouteKeyed/far-background-v1",null,.12f,0.35f,6.8f,-110,new Color(.92f,.92f,.95f),true),
  new("mid","Art/Hub/hub-road-v1",null,.28f,0.05f,5.4f,-80,new Color(.78f,.74f,.68f),false),
  new("ground","Art/RouteKeyed/hub-street-floor-v1",null,.55f,-1.35f,2.35f,-50,new Color(.96f,.92f,.84f),false),
  new("foreground","Art/RouteKeyed/foreground-v1",null,.92f,-1.45f,1.55f,60,new Color(1f,1f,1f,.7f),false),
 };

 static readonly Dictionary<int,RouteCellPresentation> Presentations=new(){
  [4]=Scene(4,RouteSceneTemplate.Gate,"Art/RouteKeyed/hub-gate-v1",4.2f,.16f,
   Exit(23,RouteExitVisualType.Road,1.6f,-0.35f,false)),
  [23]=Scene(23,RouteSceneTemplate.ThreeWayFork,null,0f,.2f,
   Exit(4,RouteExitVisualType.Road,-0.2f,-0.35f,false),
   Exit(5,RouteExitVisualType.Road,1.5f,-0.35f,false),
   Exit(11,RouteExitVisualType.Road,3.1f,-0.35f,false)),
  [5]=Scene(5,RouteSceneTemplate.BreachWall,null,0f,.22f,
   Exit(23,RouteExitVisualType.Road,-0.1f,-0.35f,false),
   Exit(11,RouteExitVisualType.Road,1.4f,-0.35f,false),
   Exit(6,RouteExitVisualType.BreachRubble,2.9f,-0.2f,true,"Art/RouteKeyed/placeholders/exit-rubble")),
  [11]=Scene(11,RouteSceneTemplate.LeftRightFork,null,0f,.28f,
   Exit(23,RouteExitVisualType.Road,-0.15f,-0.35f,false),
   Exit(5,RouteExitVisualType.Road,0.7f,-0.35f,false),
   Exit(10,RouteExitVisualType.Road,1.45f,-0.35f,false),
   Exit(18,RouteExitVisualType.Road,2.1f,-0.35f,false),
   Exit(19,RouteExitVisualType.Road,2.65f,-0.35f,false),
   Exit(17,RouteExitVisualType.Road,3.15f,-0.35f,false),
   Exit(33,RouteExitVisualType.HiddenCrack,3.55f,-0.15f,true,"Art/RouteKeyed/placeholders/exit-crack")),
  [10]=Scene(10,RouteSceneTemplate.Courtyard,null,0f,.36f,
   Exit(11,RouteExitVisualType.Road,1.4f,-0.35f,false)),
  [18]=Scene(18,RouteSceneTemplate.Courtyard,null,0f,.34f,
   Exit(11,RouteExitVisualType.Road,1.4f,-0.35f,false)),
  [19]=Scene(19,RouteSceneTemplate.Courtyard,null,0f,.33f,
   Exit(11,RouteExitVisualType.Road,1.4f,-0.35f,false)),
  [6]=Scene(6,RouteSceneTemplate.BuildingEntrance,"Art/RouteKeyed/hub-guild-close-v2",3.8f,.2f,
   Exit(5,RouteExitVisualType.Road,-0.1f,-0.35f,false),
   Exit(6,RouteExitVisualType.BuildingEntrance,1.8f,-0.1f,true,"Art/RouteKeyed/placeholders/exit-building")),
  [33]=Scene(33,RouteSceneTemplate.BuildingEntrance,"Art/RouteKeyed/hub-forge-v1",3.9f,.22f,
   Exit(11,RouteExitVisualType.Road,-0.1f,-0.35f,false),
   Exit(33,RouteExitVisualType.BuildingEntrance,1.8f,-0.1f,true,"Art/RouteKeyed/placeholders/exit-building")),
  [17]=Scene(17,RouteSceneTemplate.Landmark,null,0f,.32f,
   Exit(11,RouteExitVisualType.Road,1.5f,-0.35f,false)),
 };

 static ExplorationMapDef sliceMap;

 public static ExplorationMapDef SliceMap{
  get{
   // Rebuild when the authored slice changes during iteration (editor / domain reload).
   if(sliceMap==null||sliceMap.cells==null||sliceMap.cells.Count<10)sliceMap=BuildSliceMap();
   return sliceMap;
  }
 }

 public static RouteCellPresentation PresentationFor(int cellId){
  if(Presentations.TryGetValue(cellId,out var p))return Clone(p);
  return new RouteCellPresentation{cellId=cellId,exits=DefaultExitSlots(1)};
 }

 public static RouteCellPresentation PresentationFor(int cellId,bool interior){
  var p=PresentationFor(cellId);
  if(interior){
   p.template=RouteSceneTemplate.InteriorCorridor;
   p.farResource=null;
   p.midResource="Art/RouteKeyed/hub-guild-close-v2";
   p.landmarkResource=null;
   p.fogAlpha=.4f;
   p.grade=new Color(.88f,.84f,.78f);
   if(p.exits==null||p.exits.Count==0)p.exits=DefaultExitSlots(2);
  }
  return p;
 }

 /// <summary>Pair graph neighbor ids with presentation exit slots (by targetNodeId, else slot order). Never mutates graph.</summary>
 public static List<(int nodeId,RouteExitPresentation slot)> BindExitsToNeighbors(
  RouteCellPresentation pres,List<int> neighborIds,int currentNodeId=-1){
  var result=new List<(int nodeId,RouteExitPresentation slot)>();
  if(pres?.exits==null||neighborIds==null)return result;
  var usedNeighbors=new HashSet<int>();
  var slots=pres.exits;
  foreach(var slot in slots){
   if(slot.targetNodeId<0)continue;
   if(!neighborIds.Contains(slot.targetNodeId)||usedNeighbors.Contains(slot.targetNodeId))continue;
   usedNeighbors.Add(slot.targetNodeId);
   result.Add((slot.targetNodeId,slot));
  }
  var freeSlots=slots.Where(s=>s.targetNodeId<0).ToList();
  int si=0;
  foreach(int n in neighborIds){
   if(usedNeighbors.Contains(n))continue;
   RouteExitPresentation slot=si<freeSlots.Count?freeSlots[si++]:FallbackSlot(result.Count);
   usedNeighbors.Add(n);
   result.Add((n,slot));
  }
  // Building-enter / self-target presentation slots (same cell id) — not graph neighbors.
  if(currentNodeId>=0){
   foreach(var slot in slots){
    if(slot.targetNodeId!=currentNodeId)continue;
    if(result.Any(r=>r.nodeId==currentNodeId&&r.slot.visualType==slot.visualType))continue;
    result.Add((currentNodeId,slot));
   }
  }
  return result;
 }

 public static string ExpectedChangeLabel(ExplorationLinkKind kind,bool opened,bool investigate,ExplorationCellDef target){
  if(investigate)return "調べると隠し道が開く";
  if(kind==ExplorationLinkKind.Breach&&!opened)return "壁を破壊すると崩壊 +2";
  if(kind==ExplorationLinkKind.Breach)return "崩れた通路を進む";
  if(kind==ExplorationLinkKind.Hidden)return "隠し道を進む";
  if(target==null)return "状態変化は小さい";
  return target.type switch{
   "building_door"=>"建物へ入る",
   "battle" or "boss"=>"初訪で警戒 +1 / 侵蝕 +1",
   "event" or "treasure"=>"初訪で警戒 +1 / 侵蝕 +2",
   "rest"=>"初訪で警戒 +1 / 崩壊 -1",
   _=>"初訪で警戒 +1",
  };
 }

 public static string VisualResource(RouteExitVisualType type)=>type switch{
  RouteExitVisualType.Gate=>"Art/RouteKeyed/placeholders/exit-gate",
  RouteExitVisualType.Door=>"Art/RouteKeyed/placeholders/exit-door",
  RouteExitVisualType.BreachRubble=>"Art/RouteKeyed/placeholders/exit-rubble",
  RouteExitVisualType.HiddenCrack=>"Art/RouteKeyed/placeholders/exit-crack",
  RouteExitVisualType.BuildingEntrance=>"Art/RouteKeyed/placeholders/exit-building",
  RouteExitVisualType.Stairs=>"Art/RouteKeyed/placeholders/exit-road",
  _=>"Art/RouteKeyed/placeholders/exit-road",
 };

 public static string KindLabel(ExplorationLinkKind kind,bool opened,bool investigate)=>kind switch{
  ExplorationLinkKind.Breach=>opened?"崩れた通路":"封鎖された瓦礫",
  ExplorationLinkKind.Hidden=>investigate?"不審な壁":"隠し道",
  _=>"道",
 };

 public static bool NeedsConfirm(ExplorationLinkKind kind,RouteExitPresentation slot,bool investigate){
  if(slot!=null&&slot.requireConfirm)return true;
  if(investigate)return true;
  return kind is ExplorationLinkKind.Breach or ExplorationLinkKind.Hidden;
 }

 static RouteCellPresentation Scene(int id,RouteSceneTemplate template,string landmark,float landmarkH,float fog,params RouteExitPresentation[] exits){
  var p=new RouteCellPresentation{
   cellId=id,template=template,landmarkResource=landmark,landmarkHeight=landmarkH,fogAlpha=fog,
   exits=exits?.ToList()??new List<RouteExitPresentation>(),
  };
  if(template==RouteSceneTemplate.ThreeWayFork||template==RouteSceneTemplate.LeftRightFork)
   p.grade=new Color(.96f,.94f,.9f);
  if(id==10)p.grade=new Color(1f,.88f,.86f);
  if(id==33)p.grade=new Color(1f,.9f,.84f);
  if(id==17)p.grade=new Color(.9f,.88f,.98f);
  return p;
 }

 static RouteExitPresentation Exit(int target,RouteExitVisualType visual,float x,float y,bool confirm,string cue=null){
  var size=visual switch{
   RouteExitVisualType.BreachRubble=>new Vector2(1.8f,1.6f),
   RouteExitVisualType.BuildingEntrance=>new Vector2(1.6f,2.6f),
   RouteExitVisualType.Gate=>new Vector2(1.7f,2.8f),
   RouteExitVisualType.HiddenCrack=>new Vector2(1.1f,2.4f),
   _=>new Vector2(1.35f,2.1f),
  };
  return new RouteExitPresentation{
   targetNodeId=target,visualType=visual,worldAnchor=new Vector2(x,y),clickSize=size,
   labelOffset=new Vector2(0f,visual==RouteExitVisualType.BreachRubble?-0.7f:-0.9f),
   popupOffset=new Vector2(0f,1.05f),requireConfirm=confirm,
   cueResource=string.IsNullOrEmpty(cue)?"Art/RouteKeyed/placeholders/exit-glow":cue,
  };
 }

 static List<RouteExitPresentation> DefaultExitSlots(int count){
  var list=new List<RouteExitPresentation>();
  for(int i=0;i<count;i++){
   float t=count==1?.55f:i/(float)Mathf.Max(1,count-1);
   list.Add(FallbackSlot(i,Mathf.Lerp(-0.1f,3.2f,t)));
  }
  return list;
 }

 public static RouteExitPresentation FallbackSlot(int index,float x=1.5f)=>new(){
  targetNodeId=-1,visualType=RouteExitVisualType.Road,worldAnchor=new Vector2(x,-0.35f),
  clickSize=new Vector2(1.35f,2.1f),labelOffset=new Vector2(0f,-0.9f),popupOffset=new Vector2(0f,1f),
};

 static RouteCellPresentation Clone(RouteCellPresentation src){
  var p=new RouteCellPresentation{
   cellId=src.cellId,template=src.template,skyResource=src.skyResource,farResource=src.farResource,midResource=src.midResource,
   groundResource=src.groundResource,landmarkResource=src.landmarkResource,foregroundResource=src.foregroundResource,
   landmarkX=src.landmarkX,landmarkHeight=src.landmarkHeight,flipLandmark=src.flipLandmark,
   keyLandmarkGreen=src.keyLandmarkGreen,keyMidGreen=src.keyMidGreen,keyGroundGreen=src.keyGroundGreen,
   keyFarGreen=src.keyFarGreen,keyFarBlack=src.keyFarBlack,keyForegroundBlack=src.keyForegroundBlack,
   grade=src.grade,fogAlpha=src.fogAlpha,exits=new List<RouteExitPresentation>(),
  };
  if(src.exits!=null)foreach(var e in src.exits)p.exits.Add(new RouteExitPresentation{
   targetNodeId=e.targetNodeId,visualType=e.visualType,worldAnchor=e.worldAnchor,clickSize=e.clickSize,
   labelOffset=e.labelOffset,popupOffset=e.popupOffset,requireConfirm=e.requireConfirm,cueResource=e.cueResource,
  });
  return p;
 }

 static ExplorationMapDef BuildSliceMap(){
  ExplorationCellDef Cell(int id,string name,string type,float u,float v,bool landmark,string interior,int[] links,string[] kinds)=>new(){
   id=id,name=name,type=type,u=u,v=v,landmark=landmark,interiorMapId=interior??"",
   links=links,linkKinds=kinds,districtId="bailey_outer",locked=false,tileU=.016f,tileV=.012f,
  };
  var cells=new List<ExplorationCellDef>{
   Cell(4,"遠征入口","entrance",0.50f,0.79f,true,null,new[]{23},new[]{"normal"}),
   Cell(23,"分岐の広場","empty",0.50f,0.74f,false,null,new[]{4,5,11},new[]{"normal","normal","normal"}),
   Cell(5,"封鎖前の道","empty",0.48f,0.71f,false,null,new[]{23,11,6},new[]{"normal","normal","breach"}),
   Cell(11,"脇道の角","empty",0.49f,0.68f,false,null,new[]{23,5,10,18,19,33,17},new[]{"normal","normal","normal","normal","normal","hidden","normal"}),
   Cell(10,"中庭の影","battle",0.48f,0.57f,true,null,new[]{11},new[]{"normal"}),
   Cell(18,"崩れた番小屋","battle",0.40f,0.60f,true,null,new[]{11},new[]{"normal"}),
   Cell(19,"見張り台跡","battle",0.56f,0.62f,true,null,new[]{11},new[]{"normal"}),
   Cell(6,"兵舎の扉","building_door",0.57f,0.46f,true,"barracks_interior",new[]{5},new[]{"breach"}),
   Cell(33,"鍛造所の扉","building_door",0.28f,0.43f,true,"forge_interior",new[]{11},new[]{"hidden"}),
   Cell(17,"小さな祠","event",0.34f,0.55f,true,null,new[]{11},new[]{"normal"}),
  };
  return new ExplorationMapDef{
   id=SliceMapId,name="外郭ルート試作",artResource="",mapWidth=20.48f,mapHeight=11.52f,
   districts=new List<ExplorationDistrictDef>{new(){id="bailey_outer",name="外郭地区",locked=false}},
   cells=cells,
  };
 }

 public static bool IsPrototypeCell(int id)=>PrototypeSet.Contains(id);
 public static bool IsSliceMap(string mapId)=>mapId==SliceMapId;

 public static IEnumerable<int> PrototypeNeighbors(ExplorationMapDef def,int id){
  var cell=ExplorationMapSystem.Node(def,id);
  if(cell?.links==null)yield break;
  foreach(int n in cell.links){
   if(def!=null&&IsSliceMap(def.id)){yield return n;continue;}
   if(!IsPrototypeCell(n))continue;
   yield return n;
  }
 }

 public static string LandmarkResource(ExplorationCellDef cell){
  if(cell==null)return null;
  var pres=PresentationFor(cell.id);
  if(!string.IsNullOrEmpty(pres.landmarkResource))return pres.landmarkResource;
  return cell.type switch{
   "entrance"=>"Art/RouteKeyed/hub-gate-v1",
   "building_door"=>cell.interiorMapId=="forge_interior"||(cell.name!=null&&cell.name.Contains("鍛"))
    ?"Art/RouteKeyed/hub-forge-v1":"Art/RouteKeyed/hub-guild-close-v2",
   _=>null,
  };
 }

 /// <summary>Deprecated no-op — display must not mutate outdoor graph.</summary>
 public static void EnsurePrototypeGraph(ExplorationMapDef def){}
}
}
