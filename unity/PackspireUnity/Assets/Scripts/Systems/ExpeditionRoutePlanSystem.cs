using System;
using System.Collections.Generic;
using System.Linq;

namespace Packspire {
public enum ExpeditionNodeKind { Battle, Event, Rest, Other, Boss }
public enum ExpeditionDayStage { Quiet, Alert, Pursuit, Anomaly }

[Serializable]
public sealed class ExpeditionRoutePlan {
 public int schemaVersion,seed,currentFloorIndex,elapsedDays;
 public bool fourthDayStageEnabled;
 public bool awaitingResolution,complete;
 public string currentNodeId="",selectedNodeId="";
 public List<string> startNodeIds=new();
 public List<string> resolvedNodeIds=new(),committedNodeIds=new();
 // Retained only so development saves made before graph schema 2 deserialize safely.
 public List<string> committedPathIds=new();
 public List<ExpeditionFloorPlan> floors=new();
}

[Serializable]
public sealed class ExpeditionFloorPlan {
 public int floorIndex;
 public string bossNodeId="";
 public List<string> startNodeIds=new();
 // These are local lane tendencies, not routes locked for an entire floor.
 public List<ExpeditionPathPlan> paths=new();
 public List<ExpeditionRouteNodePlan> nodes=new();
}

[Serializable]
public sealed class ExpeditionPathPlan {
 public string id="",title="";
 public int lane,battleCount,baseDayCost;
 public List<string> nodeIds=new();
}

[Serializable]
public sealed class ExpeditionRouteNodePlan {
 public string id="",pathId="",encounterId="";
 public int floorIndex,order,lane,dayCost;
 public ExpeditionNodeKind kind;
 public List<string> nextNodeIds=new();
}

[Serializable]
public sealed class ExpeditionRouteGenerationRules {
 public int floorCount=3;
 public int columnsPerFloor=8,laneCount=3;
 public int minimumBattlesPerRoute=4,maximumBattlesPerRoute=8;
 public int minimumDaysPerFloor=6,maximumDaysPerFloor=8;
 public int minimumRouteCombinationsPerFloor=16;
 public bool enableFourthDayStage;
}

public sealed class ExpeditionRouteValidationReport {
 public readonly List<string> errors=new();
 public readonly List<string> warnings=new();
 public bool IsValid=>errors.Count==0;
}

public sealed class ExpeditionRoutePacingReport {
 public long routeCombinationCount;
 public int fastestDays,standardLowDays,standardHighDays,slowestDays;
 public ExpeditionDayStage fastestStage,standardLowStage,standardHighStage,slowestStage;
 public readonly List<string> errors=new();
 public readonly List<string> warnings=new();
 public bool IsValid=>errors.Count==0;
}

/// <summary>
/// Generates a layered expedition DAG. Exploration and assault are local lane
/// tendencies: the player may cross between them at later decisions, so neither
/// is a route selected for the whole floor. Every floor reconverges at its boss.
/// </summary>
public static class ExpeditionRoutePlanSystem {
 public const int CurrentSchemaVersion=2;
 public const int AlertStartDay=11;
 public const int PursuitStartDay=21;
 public const int AnomalyStartDay=36;

 public static ExpeditionRouteGenerationRules DefaultRules(string dungeonId="")=>new(){
  floorCount=3,
  columnsPerFloor=8,
  laneCount=3,
  minimumBattlesPerRoute=4,
  maximumBattlesPerRoute=8,
  minimumDaysPerFloor=6,
  maximumDaysPerFloor=8,
  minimumRouteCombinationsPerFloor=16,
  enableFourthDayStage=false
 };

 public static ExpeditionRoutePlan GenerateDefault(string dungeonId,int runOrdinal=0)=>
  Generate(StableSeed(dungeonId,runOrdinal),DefaultRules(dungeonId));

 public static ExpeditionRouteNodePlan Node(ExpeditionRoutePlan plan,string nodeId){
  if(plan?.floors==null||string.IsNullOrEmpty(nodeId))return null;
  return plan.floors.SelectMany(floor=>floor?.nodes??new List<ExpeditionRouteNodePlan>())
   .FirstOrDefault(node=>node.id==nodeId);
 }

 public static ExpeditionPathPlan Path(ExpeditionRoutePlan plan,string pathId){
  if(plan?.floors==null||string.IsNullOrEmpty(pathId))return null;
  return plan.floors.SelectMany(floor=>floor?.paths??new List<ExpeditionPathPlan>())
   .FirstOrDefault(path=>path.id==pathId);
 }

 public static IReadOnlyList<ExpeditionRouteNodePlan> Available(ExpeditionRoutePlan plan){
  if(plan==null||plan.complete||plan.awaitingResolution)return Array.Empty<ExpeditionRouteNodePlan>();
  IEnumerable<string> ids;
  if(string.IsNullOrEmpty(plan.currentNodeId))ids=plan.startNodeIds??new List<string>();
  else ids=Node(plan,plan.currentNodeId)?.nextNodeIds??new List<string>();
  return ids.Select(id=>Node(plan,id)).Where(node=>node!=null).ToArray();
 }

 public static bool Select(ExpeditionRoutePlan plan,string nodeId){
  if(plan==null||!Available(plan).Any(node=>node.id==nodeId))return false;
  plan.selectedNodeId=nodeId;
  return true;
 }

 public static bool CommitSelection(RunState run,out ExpeditionRouteNodePlan node){
  return CommitSelection(run,0,out node);
 }

 /// <summary>
 /// Enters one generated node and charges only that segment. This permits route
 /// switching after every reconnection without double-charging a former path.
 /// </summary>
 public static bool CommitSelection(RunState run,int dayDiscount,out ExpeditionRouteNodePlan node){
  node=null;
  var plan=run==null?null:ExpeditionProgressSystem.Ensure(run);
  if(plan==null||string.IsNullOrEmpty(plan.selectedNodeId))return false;
  node=Available(plan).FirstOrDefault(candidate=>candidate.id==plan.selectedNodeId);
  if(node==null)return false;
  plan.committedNodeIds??=new();
  if(!plan.committedNodeIds.Contains(node.id)){
   ExpeditionProgressSystem.AdvanceDays(run,Math.Max(0,node.dayCost-Math.Max(0,dayDiscount)));
   plan.committedNodeIds.Add(node.id);
  }
  plan.currentNodeId=node.id;
  plan.currentFloorIndex=Math.Max(0,node.floorIndex);
  plan.selectedNodeId="";
  plan.awaitingResolution=true;
  return true;
 }

 public static bool ResolveCurrent(ExpeditionRoutePlan plan){
  var node=Node(plan,plan?.currentNodeId);
  if(plan==null||node==null||!plan.awaitingResolution)return false;
  if(!plan.resolvedNodeIds.Contains(node.id))plan.resolvedNodeIds.Add(node.id);
  plan.awaitingResolution=false;
  bool finalBoss=node.kind==ExpeditionNodeKind.Boss&&node.floorIndex>=plan.floors.Count-1;
  if(finalBoss)plan.complete=true;
  return true;
 }

 public static ExpeditionRoutePlan Generate(int seed,ExpeditionRouteGenerationRules rules){
  ValidateRules(rules);
  var plan=new ExpeditionRoutePlan{
   schemaVersion=CurrentSchemaVersion,
   seed=seed,
   fourthDayStageEnabled=rules.enableFourthDayStage
  };
  var random=new Random(seed);

  for(int floorIndex=0;floorIndex<rules.floorCount;floorIndex++)
   plan.floors.Add(BuildFloor(floorIndex,rules,random));

  for(int floorIndex=0;floorIndex<plan.floors.Count-1;floorIndex++){
   ExpeditionFloorPlan current=plan.floors[floorIndex];
   ExpeditionFloorPlan next=plan.floors[floorIndex+1];
   Node(current,current.bossNodeId).nextNodeIds.AddRange(next.startNodeIds);
  }
  plan.startNodeIds.AddRange(plan.floors[0].startNodeIds);
  return plan;
 }

 public static ExpeditionDayStage DayStage(ExpeditionRoutePlan plan,int day){
  if(plan==null)throw new ArgumentNullException(nameof(plan));
  if(plan.fourthDayStageEnabled&&day>=AnomalyStartDay)return ExpeditionDayStage.Anomaly;
  if(day>=PursuitStartDay)return ExpeditionDayStage.Pursuit;
  if(day>=AlertStartDay)return ExpeditionDayStage.Alert;
  return ExpeditionDayStage.Quiet;
 }

 public static void EnableFourthDayStage(ExpeditionRoutePlan plan){
  if(plan==null)throw new ArgumentNullException(nameof(plan));
  plan.fourthDayStageEnabled=true;
 }

 /// <summary>
 /// Audits every generated route through the actual graph. Runtime event day
 /// changes and consumables are excluded because they are not deterministic.
 /// </summary>
 public static ExpeditionRoutePacingReport AnalyzePacing(ExpeditionRoutePlan plan){
  var report=new ExpeditionRoutePacingReport();
  if(plan==null){report.errors.Add("Route plan is null.");return report;}
  if(plan.floors==null||plan.floors.Count==0){report.errors.Add("Route plan has no floors.");return report;}
  if(plan.startNodeIds==null||plan.startNodeIds.Count==0){report.errors.Add("Route plan has no start nodes.");return report;}

  var memo=new Dictionary<string,SortedDictionary<int,long>>();
  var totals=new SortedDictionary<int,long>();
  foreach(string startId in plan.startNodeIds)
   MergeCounts(totals,RemainingDayTotals(plan,startId,memo),0);
  if(totals.Count==0){report.errors.Add("Route plan has no complete route to its final boss.");return report;}

  report.routeCombinationCount=totals.Values.Aggregate(0L,SaturatingAdd);
  report.fastestDays=totals.First().Key;
  report.slowestDays=totals.Last().Key;
  report.standardLowDays=WeightedDayAt(totals,(report.routeCombinationCount-1)/2);
  report.standardHighDays=WeightedDayAt(totals,report.routeCombinationCount/2);
  report.fastestStage=DayStage(plan,report.fastestDays);
  report.standardLowStage=DayStage(plan,report.standardLowDays);
  report.standardHighStage=DayStage(plan,report.standardHighDays);
  report.slowestStage=DayStage(plan,report.slowestDays);

  if(report.fastestStage!=ExpeditionDayStage.Alert)
   report.warnings.Add($"Fastest route reaches day {report.fastestDays}; target is the second stage (days 11-20).");
  bool standardNearBoundary=report.standardLowDays>=20&&report.standardHighDays<=21;
  if(!standardNearBoundary)
   report.warnings.Add($"Standard routes span days {report.standardLowDays}-{report.standardHighDays}; target is the 20/21-day boundary.");
  if(report.slowestDays<21||report.slowestDays>35)
   report.warnings.Add($"Slowest route reaches day {report.slowestDays}; target is the third stage (days 21-35).");
  return report;
 }

 public static ExpeditionRouteValidationReport Validate(
  ExpeditionRoutePlan plan,ExpeditionRouteGenerationRules rules){
  var report=new ExpeditionRouteValidationReport();
  if(plan==null){report.errors.Add("Route plan is null.");return report;}
  if(rules==null){report.errors.Add("Generation rules are null.");return report;}
  if(plan.schemaVersion!=CurrentSchemaVersion)
   report.errors.Add($"Expected route schema {CurrentSchemaVersion}, found {plan.schemaVersion}.");
  if(plan.floors==null||plan.floors.Count!=rules.floorCount)
   report.errors.Add($"Expected {rules.floorCount} floors, found {plan.floors?.Count??0}.");

  var ids=new HashSet<string>();
  foreach(ExpeditionFloorPlan floor in plan.floors??new List<ExpeditionFloorPlan>())
   ValidateFloor(plan,floor,rules,ids,report);
  ExpeditionRoutePacingReport pacing=AnalyzePacing(plan);
  report.errors.AddRange(pacing.errors);
  report.warnings.AddRange(pacing.warnings);
  return report;
 }

 static ExpeditionFloorPlan BuildFloor(
  int floorIndex,ExpeditionRouteGenerationRules rules,Random random){
  var floor=new ExpeditionFloorPlan{floorIndex=floorIndex};
  string[] laneNames={"探索傾向","均衡傾向","突破傾向"};
  for(int lane=0;lane<rules.laneCount;lane++){
   floor.paths.Add(new ExpeditionPathPlan{
    id=$"f{floorIndex+1}-lane-{lane}",
    title=laneNames[Math.Min(lane,laneNames.Length-1)],
    lane=lane
   });
  }

  var optionalKinds=new[]{
   Shuffled(new[]{ExpeditionNodeKind.Event,ExpeditionNodeKind.Rest,ExpeditionNodeKind.Other,ExpeditionNodeKind.Rest},random),
   Shuffled(new[]{ExpeditionNodeKind.Battle,ExpeditionNodeKind.Event,ExpeditionNodeKind.Battle,ExpeditionNodeKind.Rest},random),
   new[]{ExpeditionNodeKind.Battle,ExpeditionNodeKind.Battle,ExpeditionNodeKind.Battle,ExpeditionNodeKind.Battle}
  };

  for(int order=0;order<rules.columnsPerFloor;order++){
   bool mandatoryBattle=order%2==0;
   int optionalIndex=order/2;
   for(int lane=0;lane<rules.laneCount;lane++){
    if(order==0&&lane==1)continue;
    ExpeditionNodeKind kind=mandatoryBattle
     ?ExpeditionNodeKind.Battle
     :optionalKinds[Math.Min(lane,optionalKinds.Length-1)][optionalIndex];
    var node=new ExpeditionRouteNodePlan{
     id=$"f{floorIndex+1}-c{order+1:00}-l{lane}",
     pathId=$"f{floorIndex+1}-lane-{lane}",
     floorIndex=floorIndex,
     order=order,
     lane=lane,
     dayCost=DayCost(order,lane),
     kind=kind
    };
    floor.nodes.Add(node);
    ExpeditionPathPlan lanePlan=floor.paths[lane];
    lanePlan.nodeIds.Add(node.id);
    if(kind==ExpeditionNodeKind.Battle)lanePlan.battleCount++;
    lanePlan.baseDayCost+=node.dayCost;
   }
  }

  floor.startNodeIds.AddRange(floor.nodes.Where(node=>node.order==0)
   .OrderBy(node=>node.lane).Select(node=>node.id));
  for(int order=0;order<rules.columnsPerFloor-1;order++){
   foreach(ExpeditionRouteNodePlan node in floor.nodes.Where(node=>node.order==order)){
    foreach(int nextLane in NextLanes(node.lane,order,floorIndex)){
     ExpeditionRouteNodePlan next=floor.nodes.FirstOrDefault(candidate=>
      candidate.order==order+1&&candidate.lane==nextLane);
     if(next!=null&&!node.nextNodeIds.Contains(next.id))node.nextNodeIds.Add(next.id);
    }
   }
  }

  var boss=new ExpeditionRouteNodePlan{
   id=$"f{floorIndex+1}-boss",
   floorIndex=floorIndex,
   order=rules.columnsPerFloor,
   lane=1,
   dayCost=0,
   kind=ExpeditionNodeKind.Boss
  };
  floor.bossNodeId=boss.id;
  floor.nodes.Add(boss);
  foreach(ExpeditionRouteNodePlan tail in floor.nodes.Where(node=>node.order==rules.columnsPerFloor-1))
   tail.nextNodeIds.Add(boss.id);
  return floor;
 }

 static int DayCost(int order,int lane){
  if(order%2==0)return 1;
  int optionalIndex=order/2;
  if(optionalIndex<2)return 1;
  if(lane==0)return 1;
  return lane==1&&optionalIndex==2?1:0;
 }

 static IEnumerable<int> NextLanes(int lane,int order,int floorIndex){
  if(order%2!=0)return new[]{lane};
  if(lane<=0)return new[]{0,1};
  if(lane>=2)return new[]{1,2};
  return (order+floorIndex)%4==0?new[]{0,1}:new[]{1,2};
 }

 static void ValidateFloor(
  ExpeditionRoutePlan plan,
  ExpeditionFloorPlan floor,
  ExpeditionRouteGenerationRules rules,
  HashSet<string> ids,
  ExpeditionRouteValidationReport report){
  if(floor==null){report.errors.Add("Route plan contains a null floor.");return;}
  if(floor.startNodeIds==null||floor.startNodeIds.Count!=2)
   report.errors.Add($"Floor {floor.floorIndex+1} needs exactly two opening choices.");
  ExpeditionRouteNodePlan boss=floor.nodes?.FirstOrDefault(node=>node.id==floor.bossNodeId);
  if(boss==null||boss.kind!=ExpeditionNodeKind.Boss)
   report.errors.Add($"Floor {floor.floorIndex+1} has no valid boss node.");
  foreach(ExpeditionRouteNodePlan node in floor.nodes??new List<ExpeditionRouteNodePlan>()){
   if(node==null||string.IsNullOrEmpty(node.id)||!ids.Add(node.id)){
    report.errors.Add($"Duplicate, empty, or null node on floor {floor.floorIndex+1}.");
    continue;
   }
   if(node.kind!=ExpeditionNodeKind.Boss&&(node.nextNodeIds==null||node.nextNodeIds.Count<1||node.nextNodeIds.Count>2))
    report.errors.Add($"Node {node.id} must expose one or two next choices.");
   foreach(string nextId in node.nextNodeIds??new List<string>())
    if(Node(plan,nextId)==null)report.errors.Add($"Node {node.id} links to missing node {nextId}.");
  }

  List<RouteStats> routes=FloorRoutes(floor);
  if(routes.Count<rules.minimumRouteCombinationsPerFloor)
   report.errors.Add($"Floor {floor.floorIndex+1} has only {routes.Count} route combinations.");
  foreach(RouteStats route in routes){
   if(route.battles<rules.minimumBattlesPerRoute||route.battles>rules.maximumBattlesPerRoute)
    report.errors.Add($"Floor {floor.floorIndex+1} route has {route.battles} battles; expected {rules.minimumBattlesPerRoute}-{rules.maximumBattlesPerRoute}.");
   if(route.days<rules.minimumDaysPerFloor||route.days>rules.maximumDaysPerFloor)
    report.errors.Add($"Floor {floor.floorIndex+1} route costs {route.days} days; expected {rules.minimumDaysPerFloor}-{rules.maximumDaysPerFloor}.");
  }
  var reachable=new HashSet<string>(routes.SelectMany(route=>route.nodeIds));
  foreach(ExpeditionRouteNodePlan node in floor.nodes??new List<ExpeditionRouteNodePlan>())
   if(!reachable.Contains(node.id))report.errors.Add($"Node {node.id} is not part of any complete route.");
 }

 static List<RouteStats> FloorRoutes(ExpeditionFloorPlan floor){
  var routes=new List<RouteStats>();
  foreach(string startId in floor.startNodeIds??new List<string>())
   CollectFloorRoutes(floor,startId,new RouteStats(),routes,new HashSet<string>());
  return routes;
 }

 static void CollectFloorRoutes(
  ExpeditionFloorPlan floor,string nodeId,RouteStats current,List<RouteStats> routes,HashSet<string> visiting){
  ExpeditionRouteNodePlan node=Node(floor,nodeId);
  if(node==null||!visiting.Add(nodeId))return;
  var next=current.Copy();
  next.days+=Math.Max(0,node.dayCost);
  if(node.kind==ExpeditionNodeKind.Battle)next.battles++;
  next.nodeIds.Add(node.id);
  if(node.id==floor.bossNodeId)routes.Add(next);
  else foreach(string nextId in node.nextNodeIds)CollectFloorRoutes(floor,nextId,next,routes,visiting);
  visiting.Remove(nodeId);
 }

 static SortedDictionary<int,long> RemainingDayTotals(
  ExpeditionRoutePlan plan,string nodeId,Dictionary<string,SortedDictionary<int,long>> memo){
  if(memo.TryGetValue(nodeId,out SortedDictionary<int,long> cached))return cached;
  ExpeditionRouteNodePlan node=Node(plan,nodeId);
  var totals=new SortedDictionary<int,long>();
  if(node==null)return totals;
  if(node.nextNodeIds==null||node.nextNodeIds.Count==0)totals[Math.Max(0,node.dayCost)]=1;
  else foreach(string nextId in node.nextNodeIds)
   MergeCounts(totals,RemainingDayTotals(plan,nextId,memo),Math.Max(0,node.dayCost));
  memo[nodeId]=totals;
  return totals;
 }

 static void MergeCounts(
  SortedDictionary<int,long> target,SortedDictionary<int,long> source,int dayOffset){
  foreach(var pair in source){
   int day=pair.Key+dayOffset;
   target[day]=target.TryGetValue(day,out long count)
    ?SaturatingAdd(count,pair.Value)
    :pair.Value;
  }
 }

 static void ValidateRules(ExpeditionRouteGenerationRules rules){
  if(rules==null)throw new ArgumentNullException(nameof(rules));
  if(rules.floorCount<=0)throw new ArgumentOutOfRangeException(nameof(rules.floorCount));
  if(rules.columnsPerFloor!=8)throw new InvalidOperationException("The current pacing model requires eight columns per floor.");
  if(rules.laneCount!=3)throw new InvalidOperationException("The current graph model requires three local tendency lanes.");
  if(rules.minimumBattlesPerRoute<0||rules.maximumBattlesPerRoute<rules.minimumBattlesPerRoute)
   throw new InvalidOperationException("Battle limits are invalid.");
  if(rules.minimumDaysPerFloor<0||rules.maximumDaysPerFloor<rules.minimumDaysPerFloor)
   throw new InvalidOperationException("Day limits are invalid.");
 }

 static ExpeditionRouteNodePlan Node(ExpeditionFloorPlan floor,string id)=>
  floor?.nodes?.FirstOrDefault(node=>node.id==id);

 static T[] Shuffled<T>(T[] values,Random random){
  for(int index=values.Length-1;index>0;index--){
   int swap=random.Next(index+1);
   (values[index],values[swap])=(values[swap],values[index]);
  }
  return values;
 }

 static long SaturatingAdd(long left,long right)=>left>long.MaxValue-right?long.MaxValue:left+right;

 static int WeightedDayAt(SortedDictionary<int,long> totals,long targetIndex){
  long passed=0;
  foreach(var pair in totals){
   long next=SaturatingAdd(passed,pair.Value);
   if(targetIndex<next)return pair.Key;
   passed=next;
  }
  return totals.Last().Key;
 }

 static int StableSeed(string dungeonId,int runOrdinal){
  unchecked{
   uint hash=2166136261;
   string value=string.IsNullOrWhiteSpace(dungeonId)?"default":dungeonId;
   foreach(char character in value){hash^=character;hash*=16777619;}
   hash^=(uint)Math.Max(0,runOrdinal);
   hash*=16777619;
   return (int)(hash&0x7fffffff);
  }
 }

 sealed class RouteStats {
  public int battles,days;
  public readonly List<string> nodeIds=new();
  public RouteStats Copy(){
   var copy=new RouteStats{battles=battles,days=days};
   copy.nodeIds.AddRange(nodeIds);
   return copy;
  }
 }
}
}
