using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Packspire {
public enum GridBoardPhase { Place, Path, Run, Done }

[Serializable]
public class GridCellState {
 public int x,y;
 /// <summary>floor | blocked | start | goal</summary>
 public string terrain="floor";
 /// <summary>empty | lamp | fog | seal | enemy | event | next | return</summary>
 public string place="empty";
 public int grow;
}

[Serializable]
public class GridBoardRunState {
 public int size=8;
 public GridBoardPhase phase=GridBoardPhase.Place;
 public int energy=3;
 public int energyMax=3;
 public List<GridCellState> cells=new();
 public List<Vector2Int> path=new();
 /// <summary>Path index where each slide segment ends (for undo).</summary>
 public List<int> segmentEnds=new();
 public int pathIndex;
 public Vector2Int piece;
 public Vector2Int lastDir;
 public int turnsMax=3;
 public int turnsUsed;
 public int softLengthMax=28;
 public List<CardInstance> hand=new();
 public string selectedCardUid="";
 public string message="";
 public bool moving;
 public float moveT;
 public Vector2Int moveFrom,moveTo;
 /// <summary>Set when the piece lands on an enemy cell; UI starts a same-screen battle.</summary>
 public bool pendingBattle;
 /// <summary>Set when the piece lands on an event cell; UI opens Event screen.</summary>
 public bool pendingEvent;
 /// <summary>Dungeon id used for area count / flavor.</summary>
 public string dungeonId="old_spire";
 public int areaIndex;
 public int areaCount=3;
 /// <summary>"next" | "return" | "" — gate discovered; await player choice.</summary>
 public string pendingGate="";
}

/// <summary>Prototype seal board: place cards, then Longcat-style slide path with turn cap.</summary>
public static class GridBoardSystem {
 public const int DefaultSize=8;
 public const int DefaultTurns=3;
 public const int DefaultEnergy=3;
 public const int DefaultAreaCount=3;

 public static GridBoardRunState Create(string dungeonId="old_spire"){
  var run=new GridBoardRunState{
   dungeonId=string.IsNullOrEmpty(dungeonId)?"old_spire":dungeonId,
   areaCount=AreaCountForDungeon(dungeonId),
   areaIndex=0,
   phase=GridBoardPhase.Place,
   energy=DefaultEnergy,
   energyMax=DefaultEnergy,
   turnsMax=DefaultTurns,
   turnsUsed=0,
  };
  LoadArea(run,0);
  return run;
 }

 public static int AreaCountForDungeon(string dungeonId){
  // Proto: 2–3 areas from dungeon battle budget; always at least 2.
  var def=GameCatalog.Dungeons.FirstOrDefault(x=>x.id==dungeonId);
  if(def==null)return DefaultAreaCount;
  return Mathf.Clamp(Mathf.Max(2,(def.battles+1)/2),2,4);
 }

 public static void LoadArea(GridBoardRunState run,int index){
  if(run==null)return;
  run.areaIndex=Mathf.Clamp(index,0,Mathf.Max(0,run.areaCount-1));
  run.size=AreaSize(run.areaIndex);
  run.phase=GridBoardPhase.Place;
  run.pendingBattle=false;
  run.pendingEvent=false;
  run.pendingGate="";
  run.selectedCardUid="";
  run.moving=false;
  run.moveT=0f;
  BuildCells(run);
  RefillExploreResources(run);
  run.piece=StartCell(run);
  ResetPath(run);
  bool last=run.areaIndex>=run.areaCount-1;
  run.message=last
   ?$"最終区画 {run.areaIndex+1}/{run.areaCount} — 帰還点を見つけて持ち帰れ"
   :$"区画 {run.areaIndex+1}/{run.areaCount} — 次区画への裂け目か、帰還点を探せ";
 }

 public static bool TryAdvanceArea(GridBoardRunState run,out string msg){
  msg="";
  if(run==null){msg="盤がない";return false;}
  if(run.areaIndex>=run.areaCount-1){msg="これ以上の区画はない";return false;}
  LoadArea(run,run.areaIndex+1);
  msg=run.message;
  return true;
 }

 public static void DeclineGate(GridBoardRunState run){
  if(run==null)return;
  run.pendingGate="";
  run.message="まだこの区画を探索する";
 }

 static int AreaSize(int areaIndex)=>areaIndex<=0?7:areaIndex==1?8:9;

 static void BuildCells(GridBoardRunState run){
  run.cells.Clear();
  int n=run.size;
  for(int y=0;y<n;y++)for(int x=0;x<n;x++){
   var c=new GridCellState{x=x,y=y,terrain="floor",place="empty",grow=0};
   if(x==0&&y==n/2)c.terrain="start";
   else if(BlockedPattern(n,x,y))c.terrain="blocked";
   run.cells.Add(c);
  }
  var lamp=Cell(run,2,n/2);
  if(lamp!=null&&lamp.terrain=="floor"&&lamp.place=="empty"){lamp.place="lamp";lamp.grow=0;}
  int enemies=Mathf.Clamp(1+run.areaIndex,1,4);
  int events=run.areaIndex==0?1:2;
  PlaceRandom(run,"enemy",enemies);
  PlaceRandom(run,"event",events);
  if(run.areaIndex<run.areaCount-1)PlaceRandom(run,"next",1);
  PlaceRandom(run,"return",1);
 }

 static bool BlockedPattern(int n,int x,int y){
  // Sparse walls that scale with board size.
  if(n<=7)return (x==3&&y==2)||(x==4&&y==5)||(x==2&&y==5)||(x==5&&y==1);
  if(n==8)return (x==3&&y==2)||(x==4&&y==5)||(x==2&&y==6)||(x==5&&y==1)||(x==6&&y==4);
  return (x==3&&y==2)||(x==4&&y==5)||(x==2&&y==7)||(x==5&&y==1)||(x==7&&y==4)||(x==6&&y==6);
 }

 static void PlaceRandom(GridBoardRunState run,string place,int count){
  var pool=run.cells
   .Where(c=>c.terrain=="floor"&&c.place=="empty")
   .OrderBy(_=>UnityEngine.Random.value)
   .Take(Mathf.Max(0,count))
   .ToList();
  foreach(var c in pool){c.place=place;c.grow=0;}
 }

 static void SeedHand(GridBoardRunState run){
  run.hand.Clear();
  run.hand.Add(MakeCard("gb_lamp","灯り",1,"マスに灯り。通過すると育つ。"));
  run.hand.Add(MakeCard("gb_fog","霧",1,"マスに霧。通過時に一瞬遅れる。"));
  run.hand.Add(MakeCard("gb_seal","封鎖",1,"マスを封鎖＝曲がるための壁。"));
  run.hand.Add(MakeCard("gb_lamp","灯り",1,"マスに灯り。通過すると育つ。"));
  run.hand.Add(MakeCard("gb_seal","封鎖",1,"マスを封鎖＝曲がるための壁。"));
  run.selectedCardUid="";
 }

 static CardInstance MakeCard(string id,string name,int cost,string text)=>new(){
  id=id,name=name,cost=cost,text=text,type=CardType.Skill,source="grid-board",slotKey=Guid.NewGuid().ToString("N")
 };

 public static GridCellState Cell(GridBoardRunState run,int x,int y){
  if(run==null)return null;
  return run.cells.FirstOrDefault(c=>c.x==x&&c.y==y);
 }

 public static Vector2Int StartCell(GridBoardRunState run){
  var c=run.cells.FirstOrDefault(t=>t.terrain=="start")??run.cells[0];
  return new Vector2Int(c.x,c.y);
 }

 public static Vector2Int GoalCell(GridBoardRunState run){
  var c=run.cells.FirstOrDefault(t=>t.terrain=="goal")??run.cells[run.cells.Count-1];
  return new Vector2Int(c.x,c.y);
 }

 public static bool InBounds(GridBoardRunState run,int x,int y)=>run!=null&&x>=0&&y>=0&&x<run.size&&y<run.size;

 public static bool IsWalkable(GridBoardRunState run,int x,int y){
  var c=Cell(run,x,y);
  if(c==null)return false;
  if(c.terrain=="blocked")return false;
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
  var cell=Cell(run,x,y);
  if(cell==null){msg="範囲外";return false;}
  if(cell.terrain is "start" or "goal" or "blocked"){msg="ここには置けない";return false;}
  if(cell.place!="empty"&&cell.place!="enemy"){msg="すでに何かある";return false;}
  string place=card.id switch{
   "gb_lamp"=>"lamp",
   "gb_fog"=>"fog",
   "gb_seal"=>"seal",
   _=>"",
  };
  if(string.IsNullOrEmpty(place)){msg="未知のカード";return false;}
  cell.place=place;
  cell.grow=0;
  run.energy=Mathf.Max(0,run.energy-card.cost);
  run.hand.RemoveAll(c=>c.slotKey==card.slotKey);
  run.selectedCardUid="";
  msg=place=="seal"?$"{card.name}を置いた（曲がる壁） EN{run.energy}":$"{card.name}を置いた EN{run.energy}";
  run.message=msg;
  return true;
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
  if(run.path.Count==0)run.path.Add(StartCell(run));

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
  run.piece=run.path[run.path.Count-1];
  msg=$"滑走 {added.Count}マス　曲がり {run.turnsUsed}/{run.turnsMax}";
  run.message=msg;
  return true;
 }

 public static bool TrySlideToward(GridBoardRunState run,int x,int y,out string msg){
  msg="";
  if(run==null||run.phase!=GridBoardPhase.Path){msg="ルート描画中ではない";return false;}
  if(run.path.Count==0)run.path.Add(StartCell(run));
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
  run.piece=run.path[run.path.Count-1];
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
  run.phase=GridBoardPhase.Run;
  run.pathIndex=0;
  run.piece=run.path[0];
  run.moving=false;
  run.moveT=0f;
  msg="導線に沿って進む";
  run.message=msg;
  return true;
 }

 public static bool TickRun(GridBoardRunState run,float dt){
  if(run==null||run.phase!=GridBoardPhase.Run)return false;
  if(!string.IsNullOrEmpty(run.pendingGate))return false;
  if(run.path==null||run.path.Count==0)return false;
  float speed=2.4f;
  var here=run.moving?run.moveTo:run.piece;
  var cellHere=Cell(run,here.x,here.y);
  if(cellHere!=null&&cellHere.place=="fog")speed=1.2f;
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
  if(cell.place=="lamp"){
   cell.grow=Mathf.Min(3,cell.grow+1);
   run.message=cell.grow>=3?"灯りが狼煙になった":$"灯りが育った（{cell.grow}/3）";
  } else if(cell.place=="fog"){
   run.message="霧を抜けた";
  } else if(cell.place=="enemy"){
   cell.place="empty";
   run.pendingBattle=true;
   run.moving=false;
   run.moveT=0f;
   run.message="敵影に接触した";
  } else if(cell.place=="event"){
   cell.place="empty";
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

 /// <summary>One move ends when the drawn route finishes: refill explore resources and return to Place.</summary>
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
  // Keep pendingGate if the route ended on a gate and the player has not chosen yet.
  RefillExploreResources(run);
  if(string.IsNullOrEmpty(run.pendingGate))
   run.message="ルート終端。手札とENを補充した — いまの位置からまた配置できる";
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
  _=>"",
 };

 public static string AreaLabel(GridBoardRunState run){
  if(run==null)return "";
  return $"区画 {run.areaIndex+1}/{Mathf.Max(1,run.areaCount)}";
 }
}
}
