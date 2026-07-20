using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Packspire {
/// <summary>Link presentation / travel gate. Parallel to <see cref="ExplorationCellDef.links"/> via <see cref="ExplorationCellDef.linkKinds"/>.</summary>
public enum ExplorationLinkKind {
 Normal=0,
 /// <summary>Sealed until opened (wall break).</summary>
 Breach=1,
 /// <summary>Invisible until revealed (hidden path).</summary>
 Hidden=2,
}

[Serializable]
public class ExplorationCellDef {
 public int id;
 public string districtId,name,type;
 public int gx,gy;
 public float u,v;
 public float tileU,tileV;
 public bool landmark,locked;
 public string interiorMapId;
 public int[] links=Array.Empty<int>();
 /// <summary>Optional parallel to links: "normal" | "breach" | "hidden". Missing/short = normal.</summary>
 public string[] linkKinds=Array.Empty<string>();
}

[Serializable]
public class ExplorationDistrictDef {
 public string id,name;
 public bool locked;
}

[Serializable]
public class ExplorationMapDef {
 public string id,name,artResource;
 public float mapWidth=20.48f,mapHeight=11.52f;
 public string outdoorMapId;
 public string interiorTone;
 public List<ExplorationDistrictDef> districts=new();
 public List<ExplorationCellDef> cells=new();
 // legacy aliases used by interiors
 public List<ExplorationCellDef> nodes{get=>cells;set=>cells=value;}
 public bool IsInterior=>!string.IsNullOrEmpty(outdoorMapId);
}

[Serializable]
public class ExplorationRunState {
 public string mapId="old_spire_iso";
 public string activeMapId="old_spire_iso";
 public int currentNodeId,selectedNodeId;
 public int outdoorNodeId=-1;
 public List<string> visited=new();
 public List<string> revealed=new();
 public List<string> cleared=new();
 /// <summary>Opened breach edges as "mapId:lo:hi".</summary>
 public List<string> openedEdges=new();
 /// <summary>Known hidden edges as "mapId:lo:hi".</summary>
 public List<string> knownHiddenEdges=new();
}

public enum ExplorationEncounter {
 None,
 Event,
 Battle,
 Rest,
 EnterBuilding,
 ExitBuilding,
}

[Serializable]
class ExplorationCellsFile {
 public string id,name,artResource;
 public float mapWidth=20.48f,mapHeight=11.52f;
 public ExplorationDistrictDef[] districts;
 public ExplorationCellDef[] cells;
}

public static class ExplorationMapCatalog {
 public const string DefaultMapId="old_spire_iso";
 public const string ForgeInteriorId="forge_interior";
 public const string BarracksInteriorId="barracks_interior";
 public const string IsoMapResource="Art/Map/old-spire-iso-v1";
 public const string CellsResource="Data/old-spire-cells";

 static ExplorationMapDef cachedOutdoor;

 public static ExplorationMapDef OldSpireIso=>cachedOutdoor??=LoadOutdoor();

 // backward-compatible name used elsewhere
 public static ExplorationMapDef OldSpireWhite=>OldSpireIso;

 public static readonly ExplorationMapDef ForgeInterior=new(){
  id=ForgeInteriorId,
  name="鍛造所",
  artResource="",
  outdoorMapId=DefaultMapId,
  interiorTone="forge",
  mapWidth=10.24f,
  mapHeight=7.68f,
  cells=new List<ExplorationCellDef>{
   C(0,"鍛造所入口","exit",0.18f,0.55f,true,null,1),
   C(1,"作業通路","empty",0.48f,0.50f,false,null,0,2),
   C(2,"炉前の小部屋","event",0.78f,0.42f,true,null,1),
  },
 };

 public static readonly ExplorationMapDef BarracksInterior=new(){
  id=BarracksInteriorId,
  name="兵舎",
  artResource="",
  outdoorMapId=DefaultMapId,
  interiorTone="barracks",
  mapWidth=10.24f,
  mapHeight=7.68f,
  cells=new List<ExplorationCellDef>{
   C(0,"兵舎入口","exit",0.18f,0.55f,true,null,1),
   C(1,"寝台の列","empty",0.48f,0.48f,false,null,0,2),
   C(2,"休憩室","rest",0.78f,0.42f,true,null,1),
  },
 };

 static ExplorationCellDef C(int id,string name,string type,float u,float v,bool landmark,string interior,params int[] links)=>new(){
  id=id,name=name,type=type,u=u,v=v,landmark=landmark,interiorMapId=interior,links=links??Array.Empty<int>(),districtId="interior",locked=false,
 };

 static ExplorationMapDef LoadOutdoor(){
  var text=Resources.Load<TextAsset>(CellsResource);
  if(text!=null){
   try{
    var file=JsonUtility.FromJson<ExplorationCellsFile>(text.text);
    if(file?.cells!=null&&file.cells.Length>0){
     var def=new ExplorationMapDef{
      id=string.IsNullOrEmpty(file.id)?DefaultMapId:file.id,
      name=string.IsNullOrEmpty(file.name)?"古塔パックスパイア":file.name,
      // Rite diagram does not use the fort plate; keep empty so stage builds an obsidian board.
      artResource="",
      mapWidth=file.mapWidth>0?file.mapWidth:20.48f,
      mapHeight=file.mapHeight>0?file.mapHeight:11.52f,
      districts=file.districts?.ToList()??new List<ExplorationDistrictDef>(),
      cells=file.cells.ToList(),
     };
     SeedDemoLinkKinds(def);
     return def;
    }
   } catch(Exception ex){Debug.LogWarning("Failed to parse old-spire-cells.json: "+ex.Message);}
  }
  return BuildFallbackOutdoor();
 }

 /// <summary>Marks a few outer edges as breach/hidden so the rite diagram can show link kinds without full authoring yet.</summary>
 static void SeedDemoLinkKinds(ExplorationMapDef def){
  if(def?.cells==null||def.cells.Count==0)return;
  var outer=def.cells.Where(c=>!c.locked&&c.links!=null&&c.links.Length>0).ToList();
  if(outer.Count<4)return;
  // Seal a landmark spur (not the entrance spine) as a breach — click-to-break opens it.
  var breachLeaf=outer.FirstOrDefault(c=>(c.type=="building_door"||c.type=="battle")&&c.links.Length>=1)
   ??outer.FirstOrDefault(c=>c.type!="entrance"&&c.links.Length==1);
  int breachA=-1,breachB=-1;
  if(breachLeaf!=null){
   breachA=breachLeaf.id;breachB=breachLeaf.links[0];
   SetLinkKindBoth(def,breachA,breachB,ExplorationLinkKind.Breach);
  }
  // Hidden spur elsewhere for ghost-conduit presentation.
  var hiddenLeaf=outer.FirstOrDefault(c=>c.id!=breachA&&c.id!=breachB&&(c.type=="rest"||c.type=="event")&&c.links.Length>=1)
   ??outer.FirstOrDefault(c=>c.id!=breachA&&c.id!=breachB&&c.type!="entrance"&&c.links.Length==1);
  if(hiddenLeaf!=null&&hiddenLeaf.links.Length>0){
   int other=hiddenLeaf.links[0];
   if(other!=breachA&&other!=breachB)SetLinkKindBoth(def,hiddenLeaf.id,other,ExplorationLinkKind.Hidden);
  }
 }

 static void SetLinkKindBoth(ExplorationMapDef def,int a,int b,ExplorationLinkKind kind){
  SetLinkKindOne(def.cells.FirstOrDefault(c=>c.id==a),b,kind);
  SetLinkKindOne(def.cells.FirstOrDefault(c=>c.id==b),a,kind);
 }

 static void SetLinkKindOne(ExplorationCellDef cell,int to,ExplorationLinkKind kind){
  if(cell?.links==null)return;
  int idx=Array.IndexOf(cell.links,to);
  if(idx<0)return;
  if(cell.linkKinds==null||cell.linkKinds.Length!=cell.links.Length){
   cell.linkKinds=new string[cell.links.Length];
   for(int i=0;i<cell.linkKinds.Length;i++)cell.linkKinds[i]="normal";
  }
  cell.linkKinds[idx]=kind switch{
   ExplorationLinkKind.Breach=>"breach",
   ExplorationLinkKind.Hidden=>"hidden",
   _=>"normal",
  };
 }

 static ExplorationMapDef BuildFallbackOutdoor(){
  // Minimal free-path fallback if JSON missing: winding chain, not a lattice.
  var cells=new List<ExplorationCellDef>();
  Vector2[] pts={
   new(.40f,.82f),new(.42f,.74f),new(.45f,.66f),new(.48f,.58f),new(.50f,.50f),new(.52f,.44f),
   new(.38f,.62f),new(.32f,.58f),new(.28f,.52f),new(.26f,.46f),
   new(.54f,.56f),new(.58f,.52f),new(.60f,.46f),
   new(.56f,.48f),new(.62f,.46f),new(.66f,.42f),new(.70f,.40f),
   new(.36f,.70f),new(.34f,.64f),new(.44f,.38f),
  };
  for(int i=0;i<pts.Length;i++){
   var links=new List<int>();
   if(i>0)links.Add(i-1);
   if(i+1<pts.Length)links.Add(i+1);
   cells.Add(new ExplorationCellDef{
    id=i,districtId="bailey_outer",name=i==0?"遠征入口":$"外郭{i}",
    type=i==0?"entrance":"empty",u=pts[i].x,v=pts[i].y,tileU=.018f,tileV=.014f,
    landmark=i==0,locked=false,links=links.ToArray(),
   });
  }
  var fallback=new ExplorationMapDef{id=DefaultMapId,name="古塔パックスパイア",artResource="",cells=cells};
  SeedDemoLinkKinds(fallback);
  return fallback;
 }

 public static ExplorationMapDef Get(string id){
  if(id==ForgeInteriorId)return ForgeInterior;
  if(id==BarracksInteriorId)return BarracksInterior;
  return OldSpireIso;
 }
}

public static class ExplorationMapSystem {
 public static ExplorationRunState CreateRun(string mapId=null){
  var def=ExplorationMapCatalog.Get(mapId??ExplorationMapCatalog.DefaultMapId);
  var entrance=def.cells.FirstOrDefault(c=>!c.locked&&c.type=="entrance")??def.cells.First(c=>!c.locked);
  var run=new ExplorationRunState{
   mapId=def.id,
   activeMapId=def.id,
   currentNodeId=entrance.id,
   selectedNodeId=entrance.id,
   outdoorNodeId=-1,
  };
  MarkVisited(run,entrance.id);
  Reveal(run,def,entrance.id);
  return run;
 }

 /// <summary>Open a sealed breach or acknowledge a hidden edge so travel becomes legal.</summary>
 public static bool TryUnlockEdge(ExplorationRunState run,int from,int to){
  if(run==null)return false;
  var def=Def(run);
  if(!Connected(def,from,to))return false;
  var kind=LinkKind(def,from,to);
  if(kind==ExplorationLinkKind.Breach)OpenBreach(run,from,to);
  else if(kind==ExplorationLinkKind.Hidden)RevealHiddenEdge(run,from,to);
  else return false;
  string fk=NodeKey(run.activeMapId,from),tk=NodeKey(run.activeMapId,to);
  if(!run.revealed.Contains(fk))run.revealed.Add(fk);
  if(!run.revealed.Contains(tk))run.revealed.Add(tk);
  return true;
 }

 public static ExplorationMapDef Def(ExplorationRunState run)=>ExplorationMapCatalog.Get(run?.activeMapId??run?.mapId);

 public static ExplorationMapDef OutdoorDef(ExplorationRunState run)=>ExplorationMapCatalog.Get(run?.mapId??ExplorationMapCatalog.DefaultMapId);

 public static ExplorationCellDef Node(ExplorationMapDef def,int id)=>def?.cells.FirstOrDefault(c=>c.id==id);

 public static ExplorationCellDef Cell(ExplorationMapDef def,int id)=>Node(def,id);

 public static Vector3 WorldPos(ExplorationMapDef def,ExplorationCellDef cell){
  if(def==null||cell==null)return Vector3.zero;
  float x=(cell.u-.5f)*def.mapWidth;
  float y=(.5f-cell.v)*def.mapHeight;
  return new Vector3(x,y,0f);
 }

 public static string NodeKey(string mapId,int nodeId)=>$"{mapId}:{nodeId}";

 public static bool IsVisited(ExplorationRunState run,int nodeId)=>run!=null&&run.visited.Contains(NodeKey(run.activeMapId,nodeId));
 public static bool IsRevealed(ExplorationRunState run,int nodeId)=>run!=null&&run.revealed.Contains(NodeKey(run.activeMapId,nodeId));
 public static bool IsCleared(ExplorationRunState run,int nodeId)=>run!=null&&run.cleared.Contains(NodeKey(run.activeMapId,nodeId));

 public static void MarkVisited(ExplorationRunState run,int nodeId){
  if(run==null)return;
  string key=NodeKey(run.activeMapId,nodeId);
  if(!run.visited.Contains(key))run.visited.Add(key);
 }

 public static void MarkCleared(ExplorationRunState run,int nodeId){
  if(run==null)return;
  string key=NodeKey(run.activeMapId,nodeId);
  if(!run.cleared.Contains(key))run.cleared.Add(key);
 }

 public static bool Connected(ExplorationMapDef def,int from,int to){
  var a=Node(def,from);
  return a!=null&&a.links!=null&&a.links.Contains(to);
 }

 public static ExplorationLinkKind ParseLinkKind(string raw)=>raw switch{
  "breach" or "break" or "wall"=>ExplorationLinkKind.Breach,
  "hidden" or "secret"=>ExplorationLinkKind.Hidden,
  _=>ExplorationLinkKind.Normal,
 };

 public static ExplorationLinkKind LinkKindFromCell(ExplorationCellDef cell,int to){
  if(cell?.links==null)return ExplorationLinkKind.Normal;
  int idx=Array.IndexOf(cell.links,to);
  if(idx<0)return ExplorationLinkKind.Normal;
  if(cell.linkKinds==null||idx>=cell.linkKinds.Length||string.IsNullOrEmpty(cell.linkKinds[idx]))
   return ExplorationLinkKind.Normal;
  return ParseLinkKind(cell.linkKinds[idx]);
 }

 public static ExplorationLinkKind LinkKind(ExplorationMapDef def,int from,int to){
  var ka=LinkKindFromCell(Node(def,from),to);
  var kb=LinkKindFromCell(Node(def,to),from);
  if(ka==ExplorationLinkKind.Hidden||kb==ExplorationLinkKind.Hidden)return ExplorationLinkKind.Hidden;
  if(ka==ExplorationLinkKind.Breach||kb==ExplorationLinkKind.Breach)return ExplorationLinkKind.Breach;
  return ExplorationLinkKind.Normal;
 }

 public static string EdgeKey(string mapId,int a,int b){
  int lo=Mathf.Min(a,b),hi=Mathf.Max(a,b);
  return $"{mapId}:{lo}:{hi}";
 }

 public static bool IsEdgeOpened(ExplorationRunState run,int a,int b){
  if(run==null)return false;
  run.openedEdges??=new();
  return run.openedEdges.Contains(EdgeKey(run.activeMapId,a,b));
 }

 public static bool IsHiddenKnown(ExplorationRunState run,int a,int b){
  if(run==null)return false;
  run.knownHiddenEdges??=new();
  return run.knownHiddenEdges.Contains(EdgeKey(run.activeMapId,a,b));
 }

 public static void OpenBreach(ExplorationRunState run,int a,int b){
  if(run==null)return;
  run.openedEdges??=new();
  string key=EdgeKey(run.activeMapId,a,b);
  if(!run.openedEdges.Contains(key))run.openedEdges.Add(key);
 }

 public static void RevealHiddenEdge(ExplorationRunState run,int a,int b){
  if(run==null)return;
  run.knownHiddenEdges??=new();
  string key=EdgeKey(run.activeMapId,a,b);
  if(!run.knownHiddenEdges.Contains(key))run.knownHiddenEdges.Add(key);
 }

 public static bool EdgeTraversable(ExplorationRunState run,ExplorationMapDef def,int from,int to){
  if(!Connected(def,from,to))return false;
  var kind=LinkKind(def,from,to);
  if(kind==ExplorationLinkKind.Breach)return IsEdgeOpened(run,from,to);
  if(kind==ExplorationLinkKind.Hidden)return IsHiddenKnown(run,from,to);
  return true;
 }

 public static bool CanMove(ExplorationRunState run,int to){
  if(run==null)return false;
  var def=Def(run);
  var target=Node(def,to);
  if(target==null||target.locked)return false;
  return EdgeTraversable(run,def,run.currentNodeId,to);
 }

 public static IEnumerable<int> Neighbors(ExplorationMapDef def,int id){
  var cell=Node(def,id);
  if(cell?.links==null)yield break;
  foreach(int n in cell.links)yield return n;
 }

 /// <summary>Neighbors that should appear when revealing from a cell (hides unknown secret paths).</summary>
 public static IEnumerable<int> RevealNeighbors(ExplorationRunState run,ExplorationMapDef def,int id){
  foreach(int n in Neighbors(def,id)){
   var cell=Node(def,n);
   if(cell==null||cell.locked)continue;
   var kind=LinkKind(def,id,n);
   if(kind==ExplorationLinkKind.Hidden&&!IsHiddenKnown(run,id,n))continue;
   yield return n;
  }
 }

 public static bool Move(ExplorationRunState run,int to,out bool firstVisit){
  firstVisit=false;
  if(!CanMove(run,to))return false;
  firstVisit=!IsVisited(run,to);
  run.currentNodeId=to;
  run.selectedNodeId=to;
  MarkVisited(run,to);
  Reveal(run,Def(run),to);
  return true;
 }

 public static bool Move(ExplorationRunState run,int to)=>Move(run,to,out _);

 public static ExplorationEncounter Enter(ExplorationRunState run,RunState gameRun,int nodeId,bool firstVisit){
  if(run==null)return ExplorationEncounter.None;
  var def=Def(run);
  var node=Node(def,nodeId);
  if(node==null||node.locked)return ExplorationEncounter.None;
  if(firstVisit)ApplyAxes(gameRun,node);
  if(node.type=="exit")return ExplorationEncounter.ExitBuilding;
  if(node.type=="building_door"){
   if(!string.IsNullOrEmpty(node.interiorMapId))return ExplorationEncounter.EnterBuilding;
   return ExplorationEncounter.None;
  }
  if(IsCleared(run,nodeId))return ExplorationEncounter.None;
  return node.type switch{
   "event" or "treasure"=>ExplorationEncounter.Event,
   "battle" or "boss"=>ExplorationEncounter.Battle,
   "rest"=>ExplorationEncounter.Rest,
   _=>ExplorationEncounter.None,
  };
 }

 public static void ApplyAxes(RunState gameRun,ExplorationCellDef node){
  if(gameRun==null||node==null)return;
  gameRun.axes??=new();
  int collapse=node.type=="treasure"?2:node.type=="rest"?-1:0;
  int corruption=node.type=="event"?2:node.type=="battle"?1:0;
  gameRun.axes.Change(1,collapse,corruption);
 }

 public static bool EnterInterior(ExplorationRunState run,string interiorMapId,int fromOutdoorNodeId){
  if(run==null||string.IsNullOrEmpty(interiorMapId))return false;
  var interior=ExplorationMapCatalog.Get(interiorMapId);
  if(interior==null||!interior.IsInterior)return false;
  run.outdoorNodeId=fromOutdoorNodeId;
  run.activeMapId=interior.id;
  var entrance=interior.cells.FirstOrDefault(n=>n.type=="exit")??interior.cells[0];
  run.currentNodeId=entrance.id;
  run.selectedNodeId=entrance.id;
  MarkVisited(run,entrance.id);
  Reveal(run,interior,entrance.id);
  return true;
 }

 public static bool ExitInterior(ExplorationRunState run){
  if(run==null)return false;
  var current=Def(run);
  if(current==null||!current.IsInterior)return false;
  // Prefer the run's outdoor map (slice / full) so interiors don't yank players onto another graph.
  string outdoorId=!string.IsNullOrEmpty(run.mapId)?run.mapId:(current.outdoorMapId??ExplorationMapCatalog.DefaultMapId);
  int doorNode=run.outdoorNodeId;
  run.activeMapId=outdoorId;
  var outdoor=ExplorationMapCatalog.Get(outdoorId);
  if(doorNode<0||Node(outdoor,doorNode)==null){
   var door=outdoor.cells.FirstOrDefault(n=>n.type=="building_door"&&n.interiorMapId==current.id)
    ??outdoor.cells.FirstOrDefault(n=>n.type=="entrance")
    ??outdoor.cells.First(c=>!c.locked);
   doorNode=door.id;
  }
  run.currentNodeId=doorNode;
  run.selectedNodeId=doorNode;
  run.outdoorNodeId=-1;
  MarkVisited(run,doorNode);
  Reveal(run,outdoor,doorNode);
  return true;
 }

 public static bool IsAtEntrance(ExplorationRunState run){
  if(run==null)return false;
  var def=Def(run);
  if(def==null||def.IsInterior)return false;
  var node=Node(def,run.currentNodeId);
  return node!=null&&node.type=="entrance";
 }

 public static string Breadcrumb(ExplorationRunState run){
  if(run==null)return "探索中";
  var def=Def(run);
  if(def!=null&&def.IsInterior){
   var outdoor=OutdoorDef(run);
   return $"{outdoor?.name??"地上"} ／ {def.name}";
  }
  var cell=Node(def,run.currentNodeId);
  var dist=def?.districts?.FirstOrDefault(d=>d.id==cell?.districtId);
  if(dist!=null)return $"{def.name} ／ {dist.name}";
  return def?.name??"古塔";
 }

 public static int PlayableCellCount(ExplorationMapDef def)=>def?.cells.Count(c=>!c.locked)??0;
 public static int TotalCellCount(ExplorationMapDef def)=>def?.cells.Count??0;

 static void Reveal(ExplorationRunState run,ExplorationMapDef def,int id){
  string mapId=run.activeMapId;
  string key=NodeKey(mapId,id);
  if(!run.revealed.Contains(key))run.revealed.Add(key);
  foreach(int n in RevealNeighbors(run,def,id)){
   string nk=NodeKey(mapId,n);
   if(!run.revealed.Contains(nk))run.revealed.Add(nk);
  }
 }
}
}
