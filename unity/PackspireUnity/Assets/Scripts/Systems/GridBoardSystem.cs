using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Packspire {
public enum GridBoardPhase { Place, Path, Run, Done }

[Serializable]
public class GridCellState {
 public int x,y;
 public string contentId="";
 /// <summary>floor | blocked | void | start | goal. Void supports future irregular boards.</summary>
 public string terrain="floor";
 /// <summary>empty | lamp | fog | seal | enemy | event | next | return</summary>
 public string place="empty";
 public int grow;
 /// <summary>Persistent exploration entity occupying this cell, if any.</summary>
 public string installationUid="";
 /// <summary>True after this cell has entered the explorer's sight at least once.</summary>
 public bool discovered;
 /// <summary>Exploration turn when this cell was most recently visible.</summary>
 public int lastSeenTurn=-1;
}

[Serializable]
public class GridInstallationState {
 public string uid="",cardId="",sourceItemUid="";
 public int x,y,rotation;
 public int placedTurn,age,progress,stageIndex,remainingDuration,charges;
 public bool consumed;
 public Vector2Int Position=>new(x,y);
}

[Serializable]
public class GridTimedExplorationEffectState {
 public string uid="";
 public ExplorationEffectType type;
 public int amount,remainingTurns,x,y;
 public string parameter="";
}

[Serializable]
public class GridEnemyState {
 public string uid="";
 public string contentId="";
 public int x,y;
 public int previousX,previousY;
 /// <summary>
 /// Hostile presence is known from the start, but this coordinate is only a
 /// signature until sight identifies it. It deliberately stops updating while
 /// the hostile is outside sight, so exploration never becomes perfect radar.
 /// </summary>
 public bool signatureInitialized;
 public bool identified;
 public int lastKnownX,lastKnownY;
 public int lastSeenTurn=-1;
 /// <summary>patrol | chase | wait</summary>
 public string behavior="patrol";
 public int sightRange=4;
 public int moveSteps=1;
 public int patrolRadius=3;
 public int originX,originY;
 public bool alerted;
 public int patrolStep;
 public Vector2Int Position=>new(x,y);
}

[Serializable]
public class GridBoardRunState {
 public int size=8;
 /// <summary>Stable seed for every authored/generated area in this expedition.</summary>
 public int generationSeed;
 /// <summary>Completed exploration turns across the whole expedition.</summary>
 public int explorationTurn;
 /// <summary>Completed exploration turns in the current area.</summary>
 public int areaTurn;
 /// <summary>True after movement starts until its route or gate resolves the turn.</summary>
 public bool explorationTurnPending;
 /// <summary>Base Chebyshev sight range. One reveals the surrounding eight cells.</summary>
 public int baseSightRange=1;
 /// <summary>Character, role, equipment and temporary effects add to this value.</summary>
 public int sightRangeBonus;
 /// <summary>When false, sight uses a diamond rather than including diagonal cells.</summary>
 public bool sightIncludesDiagonals=true;
 /// <summary>0 keeps discovered cells forever; positive values enable optional memory decay.</summary>
 public int memoryDecayTurns;
 public GridBoardPhase phase=GridBoardPhase.Place;
 public int energy=3;
 public int energyMax=3;
 public List<GridCellState> cells=new();
 /// <summary>Placed exploration formulae. Cells only retain a stable entity reference.</summary>
 public List<GridInstallationState> installations=new();
 public List<GridTimedExplorationEffectState> timedEffects=new();
 /// <summary>Reverse faces removed for the rest of this expedition.</summary>
 public List<string> removedExplorationSlotKeys=new();
 /// <summary>Movable hostile entities; never baked into terrain or cell places.</summary>
 public List<GridEnemyState> enemies=new();
 public List<Vector2Int> path=new();
 /// <summary>Path index where each slide segment ends (for undo).</summary>
 public List<int> segmentEnds=new();
 public int pathIndex;
 public Vector2Int piece;
 public Vector2Int lastDir;
 public int turnsMax=3;
 public int turnsUsed;
 /// <summary>Expedition-wide pressure from the dungeon's indestructible calamity facility.</summary>
 public int doom;
 public int doomMax=6;
 public int softLengthMax=28;
 /// <summary>Reverse faces emitted by the storage formula; the current hand is drawn from this pool.</summary>
 public List<CardInstance> explorePool=new();
 public List<CardInstance> areaExhaustedExploreCards=new();
 public List<CardInstance> hand=new();
 public string selectedCardUid="";
 public string message="";
 public bool moving;
 public float moveT;
 public Vector2Int moveFrom,moveTo;
 /// <summary>Set when the piece lands on an enemy cell; UI starts a same-screen battle.</summary>
 public bool pendingBattle;
 public string pendingEnemyId="";
 /// <summary>Set when the piece lands on an event cell; UI opens the board-local event overlay.</summary>
 public bool pendingEvent;
 public string pendingEventId="";
 /// <summary>Dungeon id used for area count / flavor.</summary>
 public string dungeonId="old_spire";
 public int areaIndex;
 public int areaCount=3;
 /// <summary>"next" | "return" | "" — gate discovered; await player choice.</summary>
 public string pendingGate="";
}

public sealed class ExplorationTurnResult {
 public int turn;
 public int areaTurn;
 public int growthAdvanced;
 public int growthMatured;
 public int doomBefore;
 public int doomAfter;
 public int enemiesMoved;
 public bool enemyEngaged;
}

/// <summary>Prototype seal board: place cards, then Longcat-style slide path with turn cap.</summary>
public static class GridBoardSystem {
 public const int DefaultSize=8;
 public const int DefaultTurns=3;
 public const int DefaultEnergy=3;
 public const int DefaultAreaCount=3;

 public static GridBoardRunState Create(string dungeonId="old_spire",int generationSeed=0){
  var balance=PackspireContent.Data.balance;
  string resolvedDungeonId=string.IsNullOrEmpty(dungeonId)?balance.defaultDungeonId:dungeonId;
  var run=new GridBoardRunState{
   dungeonId=resolvedDungeonId,
   generationSeed=generationSeed==0?Guid.NewGuid().GetHashCode():generationSeed,
   areaCount=AreaCountForDungeon(resolvedDungeonId),
   areaIndex=0,
   phase=GridBoardPhase.Place,
   energy=balance.baseEnergy,
   energyMax=balance.baseEnergy,
   doomMax=balance.gridDoomMax,
   turnsMax=DefaultTurns,
   turnsUsed=0,
  };
  LoadArea(run,0);
  return run;
 }

 public static int AreaCountForDungeon(string dungeonId){
  var content=DungeonDefinition(dungeonId);
  if(content?.areas!=null&&content.areas.Length>0)return content.areas.Length;
  // Legacy content derives its area count from the old battle budget.
  var def=GameCatalog.Dungeons.FirstOrDefault(x=>x.id==dungeonId);
  if(def==null)return DefaultAreaCount;
  return Mathf.Clamp(Mathf.Max(2,(def.battles+1)/2),2,4);
 }

 static DungeonContent DungeonDefinition(string dungeonId)=>
  PackspireContent.Data.dungeons.FirstOrDefault(value=>value.id==dungeonId);

 static DungeonAreaContent AreaDefinition(GridBoardRunState run){
  var dungeon=DungeonDefinition(run?.dungeonId);
  if(dungeon?.areas==null||run==null||run.areaIndex<0||run.areaIndex>=dungeon.areas.Length)return null;
  return dungeon.areas[run.areaIndex];
 }

 public static bool HasAuthoredLayout(GridBoardRunState run)=>
  AreaDefinition(run)?.layoutRows?.Length>0;

 public static int DoomTier(GridBoardRunState run)=>run==null?0:Mathf.Clamp(run.doom/2,0,3);
 public static float EnemyHpMultiplier(GridBoardRunState run)=>1f+DoomTier(run)*0.12f;
 public static int EnemyDamageBonus(GridBoardRunState run)=>DoomTier(run);

 public static string DoomFacilityName(string dungeonId)=>dungeonId switch{
  "ash_forge"=>"灰熱炉",
  "hollow_archive"=>"抹消機関",
  _=>"黒鐘楼",
 };

 public static string DoomSummary(GridBoardRunState run){
  if(run==null)return "災厄　—";
  int max=Mathf.Max(1,run.doomMax);
  int value=Mathf.Clamp(run.doom,0,max);
  string pips=new string('●',value)+new string('○',Mathf.Max(0,max-value));
  return $"{DoomFacilityName(run.dungeonId)}　{pips}";
 }

 static void AdvanceDoom(GridBoardRunState run){
  if(run==null)return;
  run.doom=Mathf.Clamp(run.doom+1,0,Mathf.Max(1,run.doomMax));
  foreach(var facility in run.cells.Where(cell=>cell.place=="calamity"))
   facility.grow=run.doom;
 }

 public static string GrowthSummary(GridBoardRunState run){
  if(run?.cells==null)return "術式 —";
  int active=run.cells.Count(c=>c.place is "lamp" or "fog" or "seal");
  int mature=run.cells.Count(c=>(c.place is "lamp" or "fog" or "seal")&&c.grow>=PackspireContent.Data.balance.gridGrowthThreshold);
  return active==0?"術式なし":$"術式 {active}　成熟 {mature}";
 }

 /// <summary>Placed field formulae mature once per completed exploration route.</summary>
 static void AdvanceGrowth(GridBoardRunState run,out int advanced,out int matured){
  advanced=0;
  matured=0;
  if(run?.cells==null)return;
  foreach(var installation in (run.installations??new List<GridInstallationState>()).ToList()){
   if(installation.consumed||!GameCatalog.ExplorationCards.TryGetValue(installation.cardId,out var definition))continue;
   installation.age++;
   var cell=Cell(run,installation.x,installation.y);
   var currentStage=InstallationStage(run,cell);
   ApplyExplorationEffects(run,currentStage?.onTurnEffects,cell);
   if(installation.remainingDuration>0&&--installation.remainingDuration<=0){
    RemoveInstallation(run,installation,cell);
    continue;
   }
   if(definition.growthTrigger!=ExplorationGrowthTrigger.TurnsElapsed)continue;
   bool stageChanged;
   AdvanceInstallationProgress(run,installation,definition,cell,1,out stageChanged);
   advanced++;
   if(stageChanged)matured++;
  }
  foreach(var cell in run.cells){
   if(!string.IsNullOrEmpty(cell.installationUid))continue;
   if(cell.place is not ("lamp" or "fog" or "seal")||cell.grow>=PackspireContent.Data.balance.gridGrowthThreshold)continue;
   cell.grow++;
   advanced++;
   if(cell.grow>=PackspireContent.Data.balance.gridGrowthThreshold)matured++;
  }
 }

 public static void LoadArea(GridBoardRunState run,int index){
  if(run==null)return;
  ClearTimedExplorationEffects(run);
  run.areaExhaustedExploreCards??=new();
  run.explorePool??=new();
  run.explorePool.AddRange(run.areaExhaustedExploreCards.Select(card=>card.Clone()));
  run.areaExhaustedExploreCards.Clear();
  run.areaIndex=Mathf.Clamp(index,0,Mathf.Max(0,run.areaCount-1));
  run.areaTurn=0;
  run.explorationTurnPending=false;
  run.size=AreaSize(run);
  run.phase=GridBoardPhase.Place;
  run.pendingBattle=false;
  run.pendingEnemyId="";
  run.pendingEvent=false;
  run.pendingEventId="";
  run.pendingGate="";
  run.selectedCardUid="";
  run.moving=false;
  run.moveT=0f;
  BuildCells(run);
  RefillExploreResources(run);
  run.piece=StartCell(run);
  RevealAround(run,run.piece);
  ResetPath(run);
  bool last=run.areaIndex>=run.areaCount-1;
  var authored=AreaDefinition(run);
  string objective=authored?.objective;
  run.message=!string.IsNullOrWhiteSpace(objective)
   ?$"区画 {run.areaIndex+1}/{run.areaCount}「{authored.name}」— {objective}"
   :last
    ?$"最終区画 {run.areaIndex+1}/{run.areaCount} — 帰還点を見つけて持ち帰れ"
    :$"区画 {run.areaIndex+1}/{run.areaCount} — 次区画への裂け目か、帰還点を探せ";
 }

 public static bool TryAdvanceArea(GridBoardRunState run,out string msg){
  msg="";
  if(run==null){msg="盤がない";return false;}
  if(run.areaIndex>=run.areaCount-1){msg="これ以上の区画はない";return false;}
  ResolveExplorationTurn(run);
  LoadArea(run,run.areaIndex+1);
  msg=run.message;
  return true;
 }

 public static void DeclineGate(GridBoardRunState run){
  if(run==null)return;
  run.pendingGate="";
  run.message="まだこの区画を探索する";
 }

 static int AreaSize(GridBoardRunState run){
  var authored=AreaDefinition(run);
  if(authored!=null){
   int layoutSize=authored.layoutRows?.Length??0;
   return Mathf.Clamp(Mathf.Max(authored.size,layoutSize),5,12);
  }
  return run.areaIndex<=0?7:run.areaIndex==1?8:9;
 }

 static void BuildCells(GridBoardRunState run){
  run.cells.Clear();
  run.enemies.Clear();
  run.installations??=new();
  run.installations.Clear();
  int n=run.size;
  var authored=AreaDefinition(run);
  var rng=new System.Random(AreaGenerationSeed(run));
  bool hasAuthoredLayout=authored?.layoutRows!=null&&authored.layoutRows.Length==n&&
   authored.layoutRows.All(row=>row!=null&&row.Length==n);
  for(int y=0;y<n;y++)for(int x=0;x<n;x++){
   char authoredCell=hasAuthoredLayout?authored.layoutRows[y][x]:'.';
   string terrain=authoredCell switch{
    'X'=>"void",
    '#'=>"blocked",
    'S'=>"start",
    _=>"floor"
   };
   var c=new GridCellState{x=x,y=y,terrain=terrain,place="empty",grow=0};
   run.cells.Add(c);
  }

  var authoredStart=run.cells.FirstOrDefault(cell=>cell.terrain=="start");
  var start=authoredStart!=null
   ?new Vector2Int(authoredStart.x,authoredStart.y)
   :new Vector2Int(0,n/2);
  Cell(run,start.x,start.y).terrain="start";
  var protectedCells=new HashSet<long>{
   CellKey(start.x,start.y),
   CellKey(1,start.y),
   CellKey(Mathf.Min(2,n-1),start.y)
  };

  if(!hasAuthoredLayout){
   // Legacy/generated areas still receive a connected irregular silhouette.
   int voidTarget=Mathf.Clamp(Mathf.RoundToInt(n*n*(0.20f+run.areaIndex*0.015f)),8,n*n/3);
   int interiorTarget=2+run.areaIndex;
   var borderCandidates=Shuffled(run.cells.Where(c=>
    !protectedCells.Contains(CellKey(c.x,c.y))&&
    (c.x==0||c.y==0||c.x==n-1||c.y==n-1)),rng);
   int voidCount=0;
   foreach(var cell in borderCandidates){
    if(voidCount>=voidTarget-interiorTarget)break;
    if(TrySetTerrain(run,cell,"void",start)){voidCount++;}
   }
   var interiorCandidates=Shuffled(run.cells.Where(c=>
    !protectedCells.Contains(CellKey(c.x,c.y))&&
    c.x>1&&c.y>0&&c.x<n-1&&c.y<n-1),rng);
   int interiorVoids=0;
   foreach(var cell in interiorCandidates){
    if(interiorVoids>=interiorTarget||voidCount>=voidTarget)break;
    if(TrySetTerrain(run,cell,"void",start)){interiorVoids++;voidCount++;}
   }
   var remainingVoidCandidates=Shuffled(run.cells.Where(c=>
    c.terrain=="floor"&&!protectedCells.Contains(CellKey(c.x,c.y))),rng);
   foreach(var cell in remainingVoidCandidates){
    if(voidCount>=voidTarget)break;
    if(TrySetTerrain(run,cell,"void",start)){voidCount++;}
   }

   int blockedTarget=Mathf.Clamp(3+run.areaIndex,3,6);
   var wallCandidates=Shuffled(run.cells.Where(c=>
    c.terrain=="floor"&&!protectedCells.Contains(CellKey(c.x,c.y))&&
    c.x>0&&c.x<n-1&&c.y>0&&c.y<n-1),rng);
   int blocked=0;
   foreach(var cell in wallCandidates){
    if(blocked>=blockedTarget)break;
    if(TrySetTerrain(run,cell,"blocked",start)){blocked++;}
   }
  }
  if(!IsAreaTraversable(run,start))
   Debug.LogError($"Dungeon area '{authored?.id??run.dungeonId}' has a disconnected layout.");
  PlaceCalamityFacility(run,start,rng);

  var distances=TerrainDistances(run,start);
  int lampCount=authored!=null?authored.lampCount:1;
  foreach(var lamp in Shuffled(run.cells.Where(c=>c.terrain=="floor"&&c.place=="empty"&&
   distances.TryGetValue(CellKey(c.x,c.y),out int distance)&&distance>=2&&distance<=5),rng)
   .Take(Mathf.Max(0,lampCount))){
   lamp.place="lamp";
   lamp.grow=0;
  }

  // Gates are deliberately far from the entrance. Since terrain connectivity
  // was validated above, every selected anchor is guaranteed reachable.
  if(run.areaIndex<run.areaCount-1)PlaceFarthest(run,"next",distances,rng);
  bool hasBoss=authored!=null&&!string.IsNullOrEmpty(authored.bossEnemyId);
  if(!hasBoss)PlaceFarthest(run,"return",distances,rng);
  int enemies=authored!=null?authored.enemyCount:Mathf.Clamp(1+run.areaIndex,1,4);
  int events=authored!=null?authored.eventCount:run.areaIndex==0?1:2;
  SpawnEnemies(run,enemies,rng,distances,2,authored?.enemyIds);
  PlaceRandom(run,"event",events,rng,distances,2,authored?.eventIds);
  if(hasBoss)SpawnBoss(run,authored.bossEnemyId,rng,distances);
 }

 static int StableHash(string value){
  unchecked{
   uint hash=2166136261;
   foreach(char c in value??""){
    hash^=c;
    hash*=16777619;
   }
   return (int)hash;
  }
 }

 static int AreaGenerationSeed(GridBoardRunState run){
  unchecked{
   int hash=run?.generationSeed??0;
   hash=(hash*397)^StableHash(run?.dungeonId);
   hash=(hash*397)^(run?.areaIndex??0);
   return hash;
  }
 }

 static long CellKey(int x,int y)=>((long)x<<32)|(uint)y;

 /// <summary>Current sight radius after character/equipment/status modifiers.</summary>
 public static int EffectiveSightRange(GridBoardRunState run)=>
  run==null?0:Mathf.Max(0,run.baseSightRange+run.sightRangeBonus);

 public static void ConfigureSight(GridBoardRunState run,int sightRangeBonus,
  bool includeDiagonals=true){
  if(run==null)return;
  run.sightRangeBonus=sightRangeBonus;
  run.sightIncludesDiagonals=includeDiagonals;
  RevealAround(run,run.piece);
 }

 public static bool IsCurrentlyVisible(GridBoardRunState run,GridCellState cell){
  if(run==null||cell==null)return false;
  int dx=Mathf.Abs(cell.x-run.piece.x);
  int dy=Mathf.Abs(cell.y-run.piece.y);
  int distance=run.sightIncludesDiagonals?Mathf.Max(dx,dy):dx+dy;
  return distance<=EffectiveSightRange(run);
 }

 public static bool IsDiscovered(GridBoardRunState run,GridCellState cell){
  if(run==null||cell==null)return false;
  if(IsCurrentlyVisible(run,cell))return true;
  if(!cell.discovered)return false;
  if(run.memoryDecayTurns<=0)return true;
  return run.explorationTurn-cell.lastSeenTurn<=run.memoryDecayTurns;
 }

 /// <summary>
 /// Reveal from one traversal anchor. Keeping this public lets character,
 /// equipment and field effects reveal from alternate origins later.
 /// </summary>
 public static int RevealAround(GridBoardRunState run,Vector2Int origin,int range=-1){
  if(run?.cells==null)return 0;
  int sight=range<0?EffectiveSightRange(run):Mathf.Max(0,range);
  int revealed=0;
  foreach(var cell in run.cells){
   int dx=Mathf.Abs(cell.x-origin.x);
   int dy=Mathf.Abs(cell.y-origin.y);
   int distance=run.sightIncludesDiagonals?Mathf.Max(dx,dy):dx+dy;
   if(distance>sight)continue;
   if(!cell.discovered)revealed++;
   cell.discovered=true;
   cell.lastSeenTurn=run.explorationTurn;
  }
  foreach(var enemy in run.enemies??new List<GridEnemyState>()){
   EnsureEnemySignature(enemy);
   int dx=Mathf.Abs(enemy.x-origin.x);
   int dy=Mathf.Abs(enemy.y-origin.y);
   int distance=run.sightIncludesDiagonals?Mathf.Max(dx,dy):dx+dy;
   if(distance<=sight)ObserveEnemy(run,enemy);
  }
  return revealed;
 }

 static void EnsureEnemySignature(GridEnemyState enemy){
  if(enemy==null||enemy.signatureInitialized)return;
  enemy.signatureInitialized=true;
  enemy.lastKnownX=enemy.x;
  enemy.lastKnownY=enemy.y;
 }

 static void ObserveEnemy(GridBoardRunState run,GridEnemyState enemy){
  if(enemy==null)return;
  EnsureEnemySignature(enemy);
  enemy.identified=true;
  enemy.lastKnownX=enemy.x;
  enemy.lastKnownY=enemy.y;
  enemy.lastSeenTurn=run?.explorationTurn??0;
 }

 public static Vector2Int LastKnownEnemyPosition(GridEnemyState enemy){
  EnsureEnemySignature(enemy);
  return enemy==null?default:new Vector2Int(enemy.lastKnownX,enemy.lastKnownY);
 }

 public static IReadOnlyList<GridEnemyState> EnemySignaturesAt(GridBoardRunState run,int x,int y){
  if(run?.enemies==null)return Array.Empty<GridEnemyState>();
  foreach(var enemy in run.enemies)EnsureEnemySignature(enemy);
  return run.enemies.Where(enemy=>enemy.lastKnownX==x&&enemy.lastKnownY==y).ToArray();
 }

 public static bool IsEnemyCurrentlyRevealed(GridBoardRunState run,GridEnemyState enemy){
  if(run==null||enemy==null)return false;
  EnsureEnemySignature(enemy);
  return IsCurrentlyVisible(run,Cell(run,enemy.x,enemy.y))||
   (enemy.lastSeenTurn==run.explorationTurn&&
    enemy.lastKnownX==enemy.x&&enemy.lastKnownY==enemy.y);
 }

 static List<GridCellState> Shuffled(IEnumerable<GridCellState> source,System.Random rng){
  var values=source.ToList();
  for(int i=values.Count-1;i>0;i--){
   int j=rng.Next(i+1);
   (values[i],values[j])=(values[j],values[i]);
  }
  return values;
 }

 static bool TrySetTerrain(GridBoardRunState run,GridCellState cell,string terrain,Vector2Int start){
  if(cell==null||cell.terrain!="floor")return false;
  string previous=cell.terrain;
  cell.terrain=terrain;
  if(IsAreaTraversable(run,start))return true;
  cell.terrain=previous;
  return false;
 }

 /// <summary>True when every non-void/non-blocked cell belongs to the start component.</summary>
 public static bool IsAreaTraversable(GridBoardRunState run)=>run!=null&&
  IsAreaTraversable(run,StartCell(run));

 static bool IsAreaTraversable(GridBoardRunState run,Vector2Int start){
  if(run?.cells==null||run.cells.Count==0)return false;
  var startCell=Cell(run,start.x,start.y);
  if(startCell==null||!TerrainWalkable(startCell))return false;
  int expected=run.cells.Count(TerrainWalkable);
  return TerrainDistances(run,start).Count==expected;
 }

 static bool TerrainWalkable(GridCellState cell)=>
  cell!=null&&cell.terrain is not ("void" or "blocked");

 static Dictionary<long,int> TerrainDistances(GridBoardRunState run,Vector2Int start){
  var distances=new Dictionary<long,int>();
  var startCell=Cell(run,start.x,start.y);
  if(!TerrainWalkable(startCell))return distances;
  var queue=new Queue<Vector2Int>();
  queue.Enqueue(start);
  distances[CellKey(start.x,start.y)]=0;
  var directions=new[]{Vector2Int.up,Vector2Int.right,Vector2Int.down,Vector2Int.left};
  while(queue.Count>0){
   var current=queue.Dequeue();
   int distance=distances[CellKey(current.x,current.y)];
   foreach(var direction in directions){
    var next=current+direction;
    var cell=Cell(run,next.x,next.y);
    long key=CellKey(next.x,next.y);
    if(!TerrainWalkable(cell)||distances.ContainsKey(key))continue;
    distances[key]=distance+1;
    queue.Enqueue(next);
   }
  }
  return distances;
 }

 static void PlaceFarthest(GridBoardRunState run,string place,
  Dictionary<long,int> distances,System.Random rng){
  var cell=run.cells
   .Where(c=>c.terrain=="floor"&&c.place=="empty"&&
    distances.ContainsKey(CellKey(c.x,c.y)))
   .OrderByDescending(c=>distances[CellKey(c.x,c.y)])
   .ThenBy(_=>rng.Next())
   .FirstOrDefault();
  SetPlace(cell,place);
 }

 static void PlaceRandom(GridBoardRunState run,string place,int count,System.Random rng,
  Dictionary<long,int> distances,int minimumDistance,IReadOnlyList<string> authoredIds=null){
  var pool=Shuffled(run.cells.Where(c=>
   c.terrain=="floor"&&c.place=="empty"&&
   !run.enemies.Any(enemy=>enemy.x==c.x&&enemy.y==c.y)&&
   distances.TryGetValue(CellKey(c.x,c.y),out int distance)&&distance>=minimumDistance),rng)
   .Take(Mathf.Max(0,count));
  foreach(var c in pool){
   SetPlace(c,place);
   if(place=="event")c.contentId=SelectEventId(run,rng,authoredIds);
  }
 }

 static string SelectEventId(GridBoardRunState run,System.Random rng,IReadOnlyList<string> authoredIds=null){
  int tier=DoomTier(run);
  var allowed=authoredIds==null||authoredIds.Count==0?null:authoredIds.ToHashSet();
  var pool=PackspireContent.Data.events
   .Where(content=>content.minimumDoomTier<=tier&&(allowed==null||allowed.Contains(content.id)))
   .ToArray();
  if(pool.Length==0)return PackspireContent.Data.balance.defaultEventId;
  int total=pool.Sum(content=>Mathf.Max(1,content.weight));
  int roll=rng.Next(Mathf.Max(1,total));
  foreach(var content in pool){
   roll-=Mathf.Max(1,content.weight);
   if(roll<0)return content.id;
  }
  return pool[pool.Length-1].id;
 }

 static void PlaceCalamityFacility(GridBoardRunState run,Vector2Int start,System.Random rng){
  var cell=run.cells
   .Where(c=>c.terrain=="blocked"&&c.place=="empty")
   .OrderByDescending(c=>Mathf.Abs(c.x-start.x)+Mathf.Abs(c.y-start.y))
   .ThenBy(_=>rng.Next())
   .FirstOrDefault();
  if(cell==null)return;
  SetPlace(cell,"calamity");
  cell.contentId=run.dungeonId;
  cell.grow=run.doom;
 }

 static void SpawnEnemies(GridBoardRunState run,int count,System.Random rng,
  Dictionary<long,int> distances,int minimumDistance,IReadOnlyList<string> authoredIds=null){
  var candidates=run.cells.Where(c=>
   c.terrain=="floor"&&c.place=="empty"&&
   distances.TryGetValue(CellKey(c.x,c.y),out int distance)&&distance>=minimumDistance)
   .ToList();
  var cells=new List<GridCellState>();
  // Progression is normally watched, not hard-locked. One hostile signature is
  // placed near the onward gate; movement and route noise decide whether it
  // remains there by the time the explorer arrives.
  var onward=run.cells.FirstOrDefault(cell=>cell.place=="next");
  if(count>0&&onward!=null&&candidates.Count>0){
   var guard=candidates
    .OrderBy(cell=>Mathf.Abs(cell.x-onward.x)+Mathf.Abs(cell.y-onward.y))
    .ThenByDescending(cell=>distances[CellKey(cell.x,cell.y)])
    .ThenBy(_=>rng.Next())
    .First();
   cells.Add(guard);
   candidates.Remove(guard);
  }
  cells.AddRange(Shuffled(candidates,rng).Take(Mathf.Max(0,count-cells.Count)));
  int tier=Mathf.Clamp(1+run.areaIndex/2,1,2);
  var allowed=authoredIds==null||authoredIds.Count==0?null:authoredIds.ToHashSet();
  var definitions=GameCatalog.Enemies
   .Where(enemy=>allowed!=null?allowed.Contains(enemy.id):enemy.tier==tier)
   .ToArray();
  if(definitions.Length==0)definitions=GameCatalog.Enemies.ToArray();
  for(int i=0;i<cells.Count;i++){
   var cell=cells[i];
   var definition=definitions.Length==0?null:definitions[rng.Next(definitions.Length)];
   run.enemies.Add(new GridEnemyState{
    uid=$"area-{run.areaIndex}-enemy-{i}",
    contentId=definition?.id??"",
    x=cell.x,y=cell.y,previousX=cell.x,previousY=cell.y,
    signatureInitialized=true,lastKnownX=cell.x,lastKnownY=cell.y,
    originX=cell.x,originY=cell.y,
    behavior=definition?.boardBehavior switch{
     EnemyBoardBehavior.Chase=>"chase",
     EnemyBoardBehavior.Wait=>"wait",
     _=>"patrol"
    },
    sightRange=definition?.boardSightRange??4,
    moveSteps=definition?.boardMoveSteps??1,
    patrolRadius=definition?.boardPatrolRadius??3,
    patrolStep=rng.Next(4)
   });
  }
 }

 static void SpawnBoss(GridBoardRunState run,string bossEnemyId,System.Random rng,
  Dictionary<long,int> distances){
  var definition=GameCatalog.Enemies.FirstOrDefault(enemy=>enemy.id==bossEnemyId);
  if(definition==null)return;
  var cell=run.cells
   .Where(value=>value.terrain=="floor"&&value.place=="empty"&&
    !run.enemies.Any(enemy=>enemy.x==value.x&&enemy.y==value.y)&&
    distances.ContainsKey(CellKey(value.x,value.y)))
   .OrderByDescending(value=>distances[CellKey(value.x,value.y)])
   .ThenBy(_=>rng.Next())
   .FirstOrDefault();
  if(cell==null)return;
  run.enemies.Add(new GridEnemyState{
   uid=$"area-{run.areaIndex}-boss",
   contentId=definition.id,
   x=cell.x,y=cell.y,previousX=cell.x,previousY=cell.y,
   signatureInitialized=true,lastKnownX=cell.x,lastKnownY=cell.y,
   originX=cell.x,originY=cell.y,
   behavior=definition.boardBehavior switch{
    EnemyBoardBehavior.Chase=>"chase",
    EnemyBoardBehavior.Wait=>"wait",
    _=>"patrol"
   },
   sightRange=definition.boardSightRange,
   moveSteps=definition.boardMoveSteps,
   patrolRadius=definition.boardPatrolRadius
  });
 }

 static void SetPlace(GridCellState cell,string place){
  if(cell==null)return;
  cell.place=place;
  cell.contentId=place=="event"?PackspireContent.Data.balance.defaultEventId:"";
  cell.grow=0;
 }

 /// <summary>Build the exploration-side faces from the same placed equipment that builds combat cards.</summary>
 public static void SyncExplorePool(GridBoardRunState board,RunState gameRun){
  if(board==null)return;
  board.explorePool.Clear();
  if(gameRun!=null){
   foreach(var combatCard in BackpackSystem.Build(gameRun).candidates){
    if(board.removedExplorationSlotKeys?.Contains("explore:"+combatCard.slotKey)==true)continue;
    var item=gameRun.inventory.FirstOrDefault(x=>x.uid==combatCard.sourceItemUid);
    if(item==null||!GameCatalog.Items.TryGetValue(item.templateId,out var def))continue;
    var explore=ExploreFace(combatCard,def);
    board.explorePool.Add(explore);
   }
  }
  SeedHand(board);
 }

 static CardInstance ExploreFace(CardInstance combatCard,ItemDef item){
  string id=!string.IsNullOrEmpty(combatCard.explorationCardId)
   ?combatCard.explorationCardId:item.explorationCardId;
  if(!GameCatalog.ExplorationCards.TryGetValue(id,out var def))
   def=GameCatalog.ExplorationCards["gb_lamp"];
  return new CardInstance{
   id=def.id,name=$"{item.name}：{def.name}",text=def.text,cost=def.cost,type=CardType.Skill,source=item.name,
   sourceItemUid=combatCard.sourceItemUid,slotKey="explore:"+combatCard.slotKey
  };
 }

 static void SeedHand(GridBoardRunState run){
  run.hand.Clear();
  if(run.explorePool!=null&&run.explorePool.Count>0){
   int count=Mathf.Min(5,run.explorePool.Count);
   foreach(var card in run.explorePool.OrderBy(_=>UnityEngine.Random.value).Take(count))
    run.hand.Add(card.Clone());
   run.selectedCardUid="";
   return;
  }
  run.hand.Add(MakeCard("gb_lamp"));
  run.hand.Add(MakeCard("gb_fog"));
  run.hand.Add(MakeCard("gb_seal"));
  run.hand.Add(MakeCard("gb_lamp"));
  run.hand.Add(MakeCard("gb_seal"));
  run.selectedCardUid="";
 }

 static CardInstance MakeCard(string id){
  var def=GameCatalog.ExplorationCards[id];
  return new CardInstance{
   id=def.id,name=def.name,cost=def.cost,text=def.text,type=CardType.Skill,
   source="grid-board",slotKey=Guid.NewGuid().ToString("N")
  };
 }

 public static GridCellState Cell(GridBoardRunState run,int x,int y){
  if(run==null)return null;
  return run.cells.FirstOrDefault(c=>c.x==x&&c.y==y);
 }

 public static GridEnemyState EnemyAt(GridBoardRunState run,int x,int y)=>
  run?.enemies?.FirstOrDefault(enemy=>enemy.x==x&&enemy.y==y);

 public static Vector2Int StartCell(GridBoardRunState run){
  var c=run.cells.FirstOrDefault(t=>t.terrain=="start")??run.cells[0];
  return new Vector2Int(c.x,c.y);
 }

 public static Vector2Int GoalCell(GridBoardRunState run){
  var c=run.cells.FirstOrDefault(t=>t.place=="next")
   ??run.cells.FirstOrDefault(t=>t.place=="return")
   ??run.cells.LastOrDefault(TerrainWalkable)
   ??run.cells[run.cells.Count-1];
  return new Vector2Int(c.x,c.y);
 }

 public static bool InBounds(GridBoardRunState run,int x,int y)=>run!=null&&x>=0&&y>=0&&x<run.size&&y<run.size;

 public static bool IsWalkable(GridBoardRunState run,int x,int y){
  var c=Cell(run,x,y);
  if(c==null)return false;
  if(c.terrain is "blocked" or "void")return false;
  if(c.place=="seal")return false;
  return true;
 }

 public static bool OnPath(GridBoardRunState run,int x,int y)=>
  run?.path!=null&&run.path.Any(p=>p.x==x&&p.y==y);

 public static CardInstance SelectedCard(GridBoardRunState run){
  if(run==null||string.IsNullOrEmpty(run.selectedCardUid))return null;
  return run.hand.FirstOrDefault(c=>c.slotKey==run.selectedCardUid);
 }

 public static void SelectCard(GridBoardRunState run,string slotKey){
  if(run==null)return;
  EnsureCanPlace(run);
  if(run.phase!=GridBoardPhase.Place){run.message="いまはカードを選べない";return;}
  run.selectedCardUid=run.selectedCardUid==slotKey?"":slotKey??"";
 }

 public static bool TryPlace(GridBoardRunState run,int x,int y,out string msg){
  msg="";
  if(run==null){msg="盤がない";return false;}
  EnsureCanPlace(run);
  if(run.phase!=GridBoardPhase.Place){msg="いまは配置できない";return false;}
  var card=SelectedCard(run);
  if(card==null){msg="カードを選んでからマスをタップ";return false;}
  if(run.energy<card.cost){msg=$"ENが足りない（{run.energy}/{run.energyMax}）";return false;}
  if(!GameCatalog.ExplorationCards.TryGetValue(card.id,out var exploration)){msg="未知のカード";return false;}
  var cell=Cell(run,x,y);
  if(cell==null){msg="範囲外";return false;}
  if(exploration.kind==ExplorationCardKind.Installation){
   if(cell.terrain is "start" or "goal" or "blocked" or "void"){msg="ここには置けない";return false;}
   if(cell.place!="empty"||EnemyAt(run,x,y)!=null){msg="すでに何かある";return false;}
   if(string.IsNullOrEmpty(exploration.place)){msg="設置種別がない";return false;}
   cell.place=exploration.place;
   cell.grow=0;
   run.installations??=new();
   var installation=new GridInstallationState{
    uid=Guid.NewGuid().ToString("N"),
    cardId=exploration.id,
    sourceItemUid=card.sourceItemUid,
    x=x,y=y,
    placedTurn=run.explorationTurn,
    remainingDuration=exploration.duration,
    stageIndex=ResolveInstallationStageIndex(exploration,0)
   };
   run.installations.Add(installation);
   cell.installationUid=installation.uid;
   ApplyExplorationEffects(run,InstallationStage(run,cell)?.onEnterEffects,cell);
  } else {
   if(exploration.target==ExplorationTargetKind.Installation&&InstallationAt(run,x,y)==null){
    msg="対象となる設置術式がない";return false;
   }
   if(exploration.target==ExplorationTargetKind.Enemy&&EnemyAt(run,x,y)==null){
    msg="対象となる敵がいない";return false;
   }
   ApplyExplorationEffects(run,exploration.effects,cell);
  }
  run.energy=Mathf.Max(0,run.energy-card.cost);
  ConsumeExplorationCard(run,card,exploration.consumeRule);
  run.selectedCardUid="";
  msg=exploration.kind==ExplorationCardKind.Installation
   ?exploration.place=="seal"?$"{card.name}を置いた（曲がる壁） EN{run.energy}":$"{card.name}を置いた EN{run.energy}"
   :$"{card.name}を使用した EN{run.energy}";
  run.message=msg;
  return true;
 }

 public static bool TryUseSelectedCard(GridBoardRunState run,out string msg){
  msg="";
  var card=SelectedCard(run);
  if(card==null||!GameCatalog.ExplorationCards.TryGetValue(card.id,out var definition)){
   msg="カードが選ばれていない";return false;
  }
  if(definition.target!=ExplorationTargetKind.None){
   msg="対象を選ぶ必要がある";return false;
  }
  return TryPlace(run,run.piece.x,run.piece.y,out msg);
 }

 static void ConsumeExplorationCard(GridBoardRunState run,CardInstance card,ExplorationConsumeRule rule){
  if(rule==ExplorationConsumeRule.Persistent)return;
  run.hand.RemoveAll(value=>value.slotKey==card.slotKey);
  if(rule==ExplorationConsumeRule.ExhaustArea){
   run.areaExhaustedExploreCards??=new();
   var source=run.explorePool.FirstOrDefault(value=>value.slotKey==card.slotKey);
   if(source!=null)run.areaExhaustedExploreCards.Add(source.Clone());
  }
  if(rule is ExplorationConsumeRule.ExhaustArea or ExplorationConsumeRule.RemoveExpedition)
   run.explorePool.RemoveAll(value=>value.slotKey==card.slotKey);
  if(rule==ExplorationConsumeRule.RemoveExpedition){
   run.removedExplorationSlotKeys??=new();
   if(!run.removedExplorationSlotKeys.Contains(card.slotKey))
    run.removedExplorationSlotKeys.Add(card.slotKey);
  }
 }

 static void ApplyExplorationEffects(
  GridBoardRunState run,IEnumerable<ExplorationEffectContent> effects,GridCellState target){
  foreach(var effect in effects??Enumerable.Empty<ExplorationEffectContent>()){
   if(effect==null||effect.type==ExplorationEffectType.None)continue;
   ApplyExplorationEffect(run,effect.type,effect.amount,effect.parameter,target);
   if(effect.duration>0&&effect.type is ExplorationEffectType.Sight or ExplorationEffectType.Turns){
    run.timedEffects??=new();
    run.timedEffects.Add(new GridTimedExplorationEffectState{
     uid=Guid.NewGuid().ToString("N"),type=effect.type,amount=effect.amount,
     remainingTurns=effect.duration,parameter=effect.parameter,x=target?.x??run.piece.x,y=target?.y??run.piece.y
    });
   }
  }
 }

 static void ApplyExplorationEffect(
  GridBoardRunState run,ExplorationEffectType type,int amount,string parameter,GridCellState target){
  switch(type){
   case ExplorationEffectType.Draw:{
    int count=Mathf.Max(0,amount);
    foreach(var source in (run.explorePool??new List<CardInstance>())
     .OrderBy(_=>UnityEngine.Random.value).Take(count)){
     var card=source.Clone();
     card.slotKey=$"{source.slotKey}:draw:{Guid.NewGuid():N}";
     run.hand.Add(card);
    }
    break;
   }
   case ExplorationEffectType.Energy:run.energy=Mathf.Max(0,run.energy+amount);break;
   case ExplorationEffectType.Sight:run.sightRangeBonus+=amount;break;
   case ExplorationEffectType.Turns:run.turnsMax=Mathf.Max(0,run.turnsMax+amount);break;
   case ExplorationEffectType.Reveal:
    RevealAround(run,target!=null?new Vector2Int(target.x,target.y):run.piece,Mathf.Max(0,amount));
    break;
   case ExplorationEffectType.Growth:{
    var installation=target==null?null:InstallationAt(run,target.x,target.y);
    if(installation!=null&&GameCatalog.ExplorationCards.TryGetValue(installation.cardId,out var definition)){
     installation.progress=Mathf.Max(0,installation.progress+amount);
     installation.stageIndex=ResolveInstallationStageIndex(definition,installation.progress);
     target.grow=installation.progress;
    }
    break;
   }
   case ExplorationEffectType.Doom:run.doom=Mathf.Clamp(run.doom+amount,0,Mathf.Max(1,run.doomMax));break;
   case ExplorationEffectType.RemoveInstallation:{
    var installation=target==null?null:InstallationAt(run,target.x,target.y);
    if(installation!=null)RemoveInstallation(run,installation,target);
    break;
   }
  }
 }

 static void TickTimedExplorationEffects(GridBoardRunState run){
  if(run?.timedEffects==null)return;
  foreach(var effect in run.timedEffects.ToList()){
   effect.remainingTurns--;
   if(effect.remainingTurns>0)continue;
   if(effect.type==ExplorationEffectType.Sight)run.sightRangeBonus-=effect.amount;
   else if(effect.type==ExplorationEffectType.Turns)run.turnsMax=Mathf.Max(0,run.turnsMax-effect.amount);
   run.timedEffects.Remove(effect);
  }
 }

 static void ClearTimedExplorationEffects(GridBoardRunState run){
  if(run?.timedEffects==null)return;
  foreach(var effect in run.timedEffects){
   if(effect.type==ExplorationEffectType.Sight)run.sightRangeBonus-=effect.amount;
   else if(effect.type==ExplorationEffectType.Turns)run.turnsMax=Mathf.Max(0,run.turnsMax-effect.amount);
  }
  run.timedEffects.Clear();
 }

 public static GridInstallationState InstallationAt(GridBoardRunState run,int x,int y)=>
  run?.installations?.FirstOrDefault(value=>!value.consumed&&value.x==x&&value.y==y);

 public static ExplorationStageDef InstallationStage(GridBoardRunState run,GridCellState cell){
  var installation=cell==null?null:InstallationAt(run,cell.x,cell.y);
  if(installation==null||!GameCatalog.ExplorationCards.TryGetValue(installation.cardId,out var definition))
   return null;
  if(definition.stages==null||definition.stages.Length==0)return null;
  int index=Mathf.Clamp(installation.stageIndex,0,definition.stages.Length-1);
  return definition.stages[index];
 }

 public static bool AdvanceInstallationProgress(
  GridBoardRunState run,int x,int y,ExplorationGrowthTrigger trigger,int amount=1){
  var installation=InstallationAt(run,x,y);
  if(installation==null||!GameCatalog.ExplorationCards.TryGetValue(installation.cardId,out var definition))
   return false;
  if(definition.growthTrigger!=trigger)return false;
  return AdvanceInstallationProgress(run,installation,definition,Cell(run,x,y),amount,out _);
 }

 static bool AdvanceInstallationProgress(
  GridBoardRunState run,GridInstallationState installation,ExplorationCardDef definition,
  GridCellState cell,int amount,out bool stageChanged){
  stageChanged=false;
  if(run==null||installation==null||definition==null||installation.consumed||amount==0)return false;
  int previousIndex=installation.stageIndex;
  var previousStage=definition.stages!=null&&previousIndex>=0&&previousIndex<definition.stages.Length
   ?definition.stages[previousIndex]:null;
  installation.progress=Mathf.Max(0,installation.progress+amount);
  installation.stageIndex=ResolveInstallationStageIndex(definition,installation.progress);
  if(cell!=null)cell.grow=installation.progress;
  stageChanged=installation.stageIndex!=previousIndex;
  if(stageChanged){
   ApplyExplorationEffects(run,previousStage?.onMatureEffects,cell);
   ApplyExplorationEffects(run,InstallationStage(run,cell)?.onEnterEffects,cell);
  }
  return true;
 }

 static void RemoveInstallation(
  GridBoardRunState run,GridInstallationState installation,GridCellState cell){
  if(installation==null)return;
  installation.consumed=true;
  if(cell!=null){
   cell.installationUid="";
   cell.place="empty";
   cell.grow=0;
  }
 }

 static int ResolveInstallationStageIndex(ExplorationCardDef definition,int progress){
  if(definition?.stages==null||definition.stages.Length==0)return 0;
  int resolved=0;
  for(int i=0;i<definition.stages.Length;i++)
   if(definition.stages[i]!=null&&definition.stages[i].minimumProgress<=progress)resolved=i;
  return resolved;
 }

 /// <summary>Start path drawing without a separate "path phase" button.</summary>
 public static bool EnsurePathMode(GridBoardRunState run){
  if(run==null||run.phase==GridBoardPhase.Run||run.phase==GridBoardPhase.Done)return false;
  if(run.phase==GridBoardPhase.Path)return true;
  BeginPathPhase(run);
  return true;
 }

 /// <summary>Return to placing only when not mid-run and path can be discarded.</summary>
 public static void EnsureCanPlace(GridBoardRunState run){
  if(run==null||run.phase==GridBoardPhase.Run||run.phase==GridBoardPhase.Done)return;
  if(run.phase==GridBoardPhase.Place)return;
  // Path started but empty segments → allow place again by clearing path.
  if(run.segmentEnds==null||run.segmentEnds.Count==0){
   run.phase=GridBoardPhase.Place;
   ResetPath(run);
  }
 }

 public static void BeginPathPhase(GridBoardRunState run){
  if(run==null)return;
  run.phase=GridBoardPhase.Path;
  ResetPath(run);
  run.selectedCardUid="";
  run.message="方向へ滑る。壁／封鎖の手前で止まり、そこでだけ曲がれる";
 }

 public static void BeginPlacePhase(GridBoardRunState run){
  if(run==null||run.phase==GridBoardPhase.Run||run.phase==GridBoardPhase.Done)return;
  run.phase=GridBoardPhase.Place;
  ResetPath(run);
  run.message="カードを置ける。封鎖は曲がるための壁";
 }

 public static void ClearPath(GridBoardRunState run){
  if(run==null||run.phase!=GridBoardPhase.Path)return;
  ResetPath(run);
  run.phase=GridBoardPhase.Place;
  run.message="ルートをやめて配置に戻った";
 }

 static void ResetPath(GridBoardRunState run){
  run.path.Clear();
  run.segmentEnds.Clear();
  run.path.Add(run.piece);
  run.pathIndex=0;
  run.turnsUsed=0;
  run.lastDir=Vector2Int.zero;
 }

 public static bool CanSlide(GridBoardRunState run,Vector2Int dir){
  if(run==null||run.phase!=GridBoardPhase.Path)return false;
  if(dir==Vector2Int.zero||(Mathf.Abs(dir.x)+Mathf.Abs(dir.y))!=1)return false;
  return CollectSlide(run,dir).Count>0;
 }

 public static IReadOnlyList<Vector2Int> SlidePreview(GridBoardRunState run,Vector2Int dir){
  if(run==null||run.phase!=GridBoardPhase.Path)return Array.Empty<Vector2Int>();
  if(dir==Vector2Int.zero||(Mathf.Abs(dir.x)+Mathf.Abs(dir.y))!=1)return Array.Empty<Vector2Int>();
  return CollectSlide(run,dir);
 }

 static List<Vector2Int> CollectSlide(GridBoardRunState run,Vector2Int dir){
  var added=new List<Vector2Int>();
  if(run.path==null||run.path.Count==0)return added;
  var cur=run.path[run.path.Count-1];
  bool isTurn=run.lastDir!=Vector2Int.zero&&run.lastDir!=dir;
  if(isTurn&&run.turnsUsed>=run.turnsMax)return added;
  if(run.lastDir!=Vector2Int.zero&&run.lastDir==dir)return added;

  int budget=run.softLengthMax-Mathf.Max(0,run.path.Count-1);
  var curPos=cur;
  while(budget>0){
   var next=curPos+dir;
   if(!InBounds(run,next.x,next.y))break;
   if(!IsWalkable(run,next.x,next.y))break;
   if(OnPath(run,next.x,next.y))break;
   if(added.Any(p=>p.x==next.x&&p.y==next.y))break;
   added.Add(next);
   curPos=next;
   budget--;
  }
  return added;
 }

 public static bool TrySlide(GridBoardRunState run,Vector2Int dir,out string msg){
  msg="";
  if(run==null||run.phase!=GridBoardPhase.Path){msg="ルート描画中ではない";return false;}
  if(dir==Vector2Int.zero||(Mathf.Abs(dir.x)+Mathf.Abs(dir.y))!=1){msg="上下左右のみ";return false;}
  if(run.path.Count==0)run.path.Add(run.piece);

  bool isTurn=run.lastDir!=Vector2Int.zero&&run.lastDir!=dir;
  if(run.lastDir!=Vector2Int.zero&&run.lastDir==dir){
   msg="その向きは壁まで行き着いている。曲がってから";
   return false;
  }
  if(isTurn&&run.turnsUsed>=run.turnsMax){
   msg=$"曲がり上限（{run.turnsMax}）";
   return false;
  }

  var added=CollectSlide(run,dir);
  if(added.Count==0){
   msg="その方向には進めない";
   return false;
  }

  if(isTurn)run.turnsUsed++;
  foreach(var p in added)run.path.Add(p);
  run.segmentEnds.Add(run.path.Count-1);
  run.lastDir=dir;
  msg=$"滑走 {added.Count}マス　曲がり {run.turnsUsed}/{run.turnsMax}";
  run.message=msg;
  return true;
 }

 public static bool TrySlideToward(GridBoardRunState run,int x,int y,out string msg){
  msg="";
  if(run==null||run.phase!=GridBoardPhase.Path){msg="ルート描画中ではない";return false;}
  if(run.path.Count==0)run.path.Add(run.piece);
  var tip=run.path[run.path.Count-1];
  if(x==tip.x&&y==tip.y)return false;
  if(x!=tip.x&&y!=tip.y){msg="先端と同じ行／列を指定";return false;}
  int dx=x==tip.x?0:(x>tip.x?1:-1);
  int dy=y==tip.y?0:(y>tip.y?1:-1);
  return TrySlide(run,new Vector2Int(dx,dy),out msg);
 }

 public static bool UndoSegment(GridBoardRunState run,out string msg){
  msg="";
  if(run==null||run.phase!=GridBoardPhase.Path){msg="いまは戻せない";return false;}
  if(run.segmentEnds==null||run.segmentEnds.Count==0){msg="戻す線分がない";return false;}
  run.segmentEnds.RemoveAt(run.segmentEnds.Count-1);
  int startExclusive=run.segmentEnds.Count==0?0:run.segmentEnds[run.segmentEnds.Count-1];
  if(run.path.Count>startExclusive+1)
   run.path.RemoveRange(startExclusive+1,run.path.Count-(startExclusive+1));
  RecomputeTurnsFromPath(run);
  msg=$"一手戻した　曲がり {run.turnsUsed}/{run.turnsMax}";
  run.message=msg;
  return true;
 }

 static void RecomputeTurnsFromPath(GridBoardRunState run){
  run.turnsUsed=0;
  run.lastDir=Vector2Int.zero;
  if(run.segmentEnds==null||run.segmentEnds.Count==0)return;
  int prevEnd=0;
  for(int s=0;s<run.segmentEnds.Count;s++){
   int end=run.segmentEnds[s];
   if(end<=prevEnd||end>=run.path.Count)continue;
   var a=run.path[prevEnd];
   var b=run.path[prevEnd+1];
   var dir=new Vector2Int(b.x-a.x,b.y-a.y);
   if(run.lastDir!=Vector2Int.zero&&run.lastDir!=dir)run.turnsUsed++;
   run.lastDir=dir;
   prevEnd=end;
  }
 }

 public static bool CanStartRun(GridBoardRunState run)=>
  run!=null&&run.phase==GridBoardPhase.Path&&run.path!=null&&run.path.Count>=2;

 public static bool BeginRun(GridBoardRunState run,out string msg){
  msg="";
  if(!CanStartRun(run)){msg="まだ滑走していない";return false;}
  AlertEnemiesAlongRoute(run);
  run.phase=GridBoardPhase.Run;
  run.explorationTurnPending=true;
  run.pathIndex=0;
  run.piece=run.path[0];
  RevealAround(run,run.piece);
  run.moving=false;
  run.moveT=0f;
  msg="導線に沿って進む";
  run.message=msg;
  return true;
 }

 /// <summary>
 /// Committing a route produces a short-range noise trail. Hostiles close to
 /// that trail begin pursuit even when the explorer never identified them.
 /// Avoiding every battle therefore needs stealth or field control rather than
 /// simply plotting around perfectly tracked markers.
 /// </summary>
 public static int AlertEnemiesAlongRoute(GridBoardRunState run,int hearingRange=-1){
  if(run?.enemies==null||run.path==null||run.path.Count<2)return 0;
  int range=hearingRange<0?1+Mathf.Min(1,DoomTier(run)):Mathf.Max(0,hearingRange);
  int alerted=0;
  foreach(var enemy in run.enemies){
   int nearest=run.path.Min(position=>
    Mathf.Abs(position.x-enemy.x)+Mathf.Abs(position.y-enemy.y));
   if(nearest>range||enemy.alerted)continue;
   enemy.alerted=true;
   alerted++;
  }
  return alerted;
 }

 public static bool TickRun(GridBoardRunState run,float dt){
  if(run==null||run.phase!=GridBoardPhase.Run)return false;
  if(!string.IsNullOrEmpty(run.pendingGate))return false;
  if(run.path==null||run.path.Count==0)return false;
  float speed=PackspireContent.Data.balance.gridMoveSpeed;
  var here=run.moving?run.moveTo:run.piece;
  var cellHere=Cell(run,here.x,here.y);
  if(cellHere!=null&&cellHere.place=="fog")speed=PackspireContent.Data.balance.gridFogMoveSpeed;
  if(run.moving){
   run.moveT+=dt*speed;
   if(run.moveT>=1f){
    run.moving=false;
    run.moveT=0f;
    run.piece=run.moveTo;
    run.pathIndex++;
    OnArrive(run,run.piece);
    return true;
   }
   return true;
  }
  if(run.pathIndex>=run.path.Count-1){
   Finish(run);
   return true;
  }
  run.moveFrom=run.path[run.pathIndex];
  run.moveTo=run.path[run.pathIndex+1];
  run.moving=true;
  run.moveT=0f;
  return true;
 }

 public static Vector2 PieceVisual(GridBoardRunState run){
  if(run==null)return default;
  if(run.moving){
   float t=Mathf.SmoothStep(0f,1f,Mathf.Clamp01(run.moveT));
   return Vector2.Lerp(run.moveFrom,run.moveTo,t);
  }
  return run.piece;
 }

 static void OnArrive(GridBoardRunState run,Vector2Int pos){
  var cell=Cell(run,pos.x,pos.y);
  if(cell==null)return;
  RevealAround(run,pos);
  AdvanceInstallationProgress(run,pos.x,pos.y,ExplorationGrowthTrigger.RoutePasses);
  var hostile=EnemyAt(run,pos.x,pos.y);
  if(hostile!=null){
   EngageEnemy(run,hostile);
   return;
  }
  if(cell.place=="lamp"){
   run.message=cell.grow>=PackspireContent.Data.balance.gridGrowthThreshold?"狼煙の灯りが導線を照らす":"灯りの傍を抜けた";
  } else if(cell.place=="fog"){
   run.message="霧を抜けた";
  } else if(cell.place=="enemy"){
   cell.place="empty";
   run.pendingBattle=true;
   run.moving=false;
   run.moveT=0f;
   run.message="敵影に接触した";
  } else if(cell.place=="event"){
   run.pendingEventId=string.IsNullOrEmpty(cell.contentId)?PackspireContent.Data.balance.defaultEventId:cell.contentId;
   cell.place="empty";
   cell.contentId="";
   run.pendingEvent=true;
   run.moving=false;
   run.moveT=0f;
   run.message="記憶の揺らぎに触れた";
  } else if(cell.place=="next"){
   run.pendingGate="next";
   run.moving=false;
   run.moveT=0f;
   run.message="次の区画への裂け目を見つけた — 進出するか選べ";
  } else if(cell.place=="return"){
   run.pendingGate="return";
   run.moving=false;
   run.moveT=0f;
   run.message="帰還点を見つけた — 持ち帰るかを選べ";
  }
 }

 static void EngageEnemy(GridBoardRunState run,GridEnemyState enemy){
  if(run==null||enemy==null)return;
  run.enemies.Remove(enemy);
  run.pendingEnemyId=enemy.contentId??"";
  run.pendingBattle=true;
  run.moving=false;
  run.moveT=0f;
  run.message="敵影に接触した";
 }

 static bool IsMatureField(GridCellState cell,string place)=>cell!=null&&
  cell.place==place&&cell.grow>=PackspireContent.Data.balance.gridGrowthThreshold;

 static void ApplyMatureFieldEffects(GridBoardRunState run){
  if(run?.cells==null)return;
  foreach(var lamp in run.cells.Where(cell=>IsMatureField(cell,"lamp")))
   RevealAround(run,new Vector2Int(lamp.x,lamp.y),2);
 }

 static bool EnemyCanEnter(GridBoardRunState run,GridCellState cell){
  if(run==null||cell==null||cell.terrain is "void" or "blocked")return false;
  if(cell.place is "seal" or "next" or "return" or "event")return false;
  if(IsMatureField(cell,"fog"))return false;
  return true;
 }

 static Dictionary<long,int> EnemyDistancesToPlayer(GridBoardRunState run){
  var distances=new Dictionary<long,int>();
  if(run==null)return distances;
  var queue=new Queue<Vector2Int>();
  queue.Enqueue(run.piece);
  distances[CellKey(run.piece.x,run.piece.y)]=0;
  var directions=new[]{Vector2Int.up,Vector2Int.right,Vector2Int.down,Vector2Int.left};
  while(queue.Count>0){
   var current=queue.Dequeue();
   int distance=distances[CellKey(current.x,current.y)];
   foreach(var direction in directions){
    var next=current+direction;
    var cell=Cell(run,next.x,next.y);
    long key=CellKey(next.x,next.y);
    if(distances.ContainsKey(key)||!EnemyCanEnter(run,cell))continue;
    distances[key]=distance+1;
    queue.Enqueue(next);
   }
  }
  return distances;
 }

 public static int AdvanceEnemies(GridBoardRunState run,out bool engaged){
  engaged=false;
  if(run?.enemies==null||run.enemies.Count==0)return 0;
  var distances=EnemyDistancesToPlayer(run);
  var occupied=new HashSet<long>(run.enemies.Select(enemy=>CellKey(enemy.x,enemy.y)));
  var directions=new[]{Vector2Int.up,Vector2Int.right,Vector2Int.down,Vector2Int.left};
  int moved=0;
  foreach(var enemy in run.enemies.OrderBy(value=>value.uid).ToList()){
   if(run.pendingBattle)break;
   int steps=Mathf.Max(0,enemy.moveSteps);
   for(int step=0;step<steps;step++){
    long currentKey=CellKey(enemy.x,enemy.y);
    occupied.Remove(currentKey);
    distances.TryGetValue(currentKey,out int playerDistance);
    bool hasDistance=distances.ContainsKey(currentKey);
    bool inSight=IsCurrentlyVisible(run,Cell(run,enemy.x,enemy.y));
    int detectionRange=Mathf.Max(1,enemy.sightRange)+DoomTier(run);
    bool detected=inSight||(hasDistance&&playerDistance<=detectionRange);
    bool chase=enemy.behavior=="chase"||
     (enemy.behavior=="wait"?(enemy.alerted||detected):
      (enemy.alerted||detected));
    if(detected)enemy.alerted=true;

    var candidates=directions
     .Select(direction=>enemy.Position+direction)
     .Where(position=>{
      var cell=Cell(run,position.x,position.y);
      long key=CellKey(position.x,position.y);
      bool withinPatrol=chase||enemy.behavior!="patrol"||
       Mathf.Abs(position.x-enemy.originX)+Mathf.Abs(position.y-enemy.originY)<=Mathf.Max(1,enemy.patrolRadius);
      return withinPatrol&&EnemyCanEnter(run,cell)&&(!occupied.Contains(key)||
       (position.x==run.piece.x&&position.y==run.piece.y));
     })
     .ToList();
    Vector2Int destination=enemy.Position;
    if(chase&&hasDistance){
     destination=candidates
      .Where(position=>distances.TryGetValue(CellKey(position.x,position.y),out int value)&&
       value<playerDistance)
      .OrderBy(position=>distances[CellKey(position.x,position.y)])
      .ThenBy(position=>position.y)
      .ThenBy(position=>position.x)
      .DefaultIfEmpty(enemy.Position)
      .First();
    } else if(enemy.behavior!="wait"&&candidates.Count>0){
     int raw=StableHash(enemy.uid)+run.explorationTurn+enemy.patrolStep;
     int index=(raw&int.MaxValue)%candidates.Count;
     destination=candidates[index];
     enemy.patrolStep++;
    }

    enemy.previousX=enemy.x;
    enemy.previousY=enemy.y;
    if(destination!=enemy.Position){
     enemy.x=destination.x;
     enemy.y=destination.y;
     moved++;
    }
    if(IsCurrentlyVisible(run,Cell(run,enemy.x,enemy.y)))ObserveEnemy(run,enemy);
    occupied.Add(CellKey(enemy.x,enemy.y));
    if(enemy.x==run.piece.x&&enemy.y==run.piece.y){
     EngageEnemy(run,enemy);
     engaged=true;
     break;
    }
    if(destination==enemy.Position)break;
   }
  }
  return moved;
 }

 /// <summary>
 /// Resolve exactly one committed exploration turn. Events and battles may
 /// pause a route, but cannot advance growth or pressure twice.
 /// </summary>
 public static ExplorationTurnResult ResolveExplorationTurn(GridBoardRunState run){
  if(run==null||!run.explorationTurnPending)return null;
  run.explorationTurnPending=false;
  var result=new ExplorationTurnResult{
   turn=++run.explorationTurn,
   areaTurn=++run.areaTurn,
   doomBefore=run.doom
  };
  AdvanceGrowth(run,out result.growthAdvanced,out result.growthMatured);
  ApplyMatureFieldEffects(run);
  TickTimedExplorationEffects(run);
  AdvanceDoom(run);
  result.doomAfter=run.doom;
  return result;
 }

 /// <summary>One move ends when the drawn route finishes: resolve its turn and refill resources.</summary>
 public static void EndRouteMove(GridBoardRunState run){
  if(run==null)return;
  run.moving=false;
  run.moveT=0f;
  run.phase=GridBoardPhase.Place;
  run.path.Clear();
  run.segmentEnds.Clear();
  run.pathIndex=0;
  run.turnsUsed=0;
  run.lastDir=Vector2Int.zero;
  run.selectedCardUid="";
  run.pendingBattle=false;
  run.pendingEvent=false;
  var turn=ResolveExplorationTurn(run);
  int advanced=turn?.growthAdvanced??0;
  int matured=turn?.growthMatured??0;
  if(turn!=null&&string.IsNullOrEmpty(run.pendingGate)){
   turn.enemiesMoved=AdvanceEnemies(run,out bool engaged);
   turn.enemyEngaged=engaged;
  }
  // Keep pendingGate if the route ended on a gate and the player has not chosen yet.
  RefillExploreResources(run);
  if(string.IsNullOrEmpty(run.pendingGate)&&!run.pendingBattle)
   run.message=advanced>0
    ?$"ルート終端。術式が {advanced} 個成長{(matured>0?$"、{matured} 個が成熟":"")} — 手札とENを補充した"
    :"ルート終端。手札とENを補充した — いまの位置からまた配置できる";
 }

 public static void RefillExploreResources(GridBoardRunState run){
  if(run==null)return;
  run.energy=Mathf.Max(1,run.energyMax);
  SeedHand(run);
 }

 static void Finish(GridBoardRunState run)=>EndRouteMove(run);

 public static string PhaseLabel(GridBoardPhase phase)=>phase switch{
  GridBoardPhase.Place=>"探索",
  GridBoardPhase.Path=>"導線",
  GridBoardPhase.Run=>"進行中",
  GridBoardPhase.Done=>"完了",
  _=>"—",
 };

 public static string PlaceLabel(string place)=>place switch{
  "lamp"=>"灯",
  "fog"=>"霧",
  "seal"=>"封",
  "enemy"=>"敵",
  "event"=>"異",
  "next"=>"次",
  "return"=>"帰",
  "calamity"=>"刻",
  _=>"",
 };

 public static string AreaLabel(GridBoardRunState run){
  if(run==null)return "";
  return $"区画 {run.areaIndex+1}/{Mathf.Max(1,run.areaCount)}";
 }
}
}
