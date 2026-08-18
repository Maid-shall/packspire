using System;
using System.Collections.Generic;
using System.Linq;

namespace Packspire {
public enum ExpeditionNodeKind { Battle, Event, Rest, Other, Boss }
public enum ExpeditionDayStage { Quiet, Alert, Pursuit }

[Serializable]
public sealed class ExpeditionRoutePlan {
 public int seed,currentFloorIndex,elapsedDays;
 public int alertStartsOnDay,pursuitStartsOnDay;
 public List<string> startNodeIds=new();
 public List<ExpeditionFloorPlan> floors=new();
}

[Serializable]
public sealed class ExpeditionFloorPlan {
 public int floorIndex;
 public string bossNodeId="";
 public List<ExpeditionPathPlan> paths=new();
 public List<ExpeditionRouteNodePlan> nodes=new();
}

[Serializable]
public sealed class ExpeditionPathPlan {
 public string id="",title="";
 public int battleCount;
 public List<string> nodeIds=new();
}

[Serializable]
public sealed class ExpeditionRouteNodePlan {
 public string id="",pathId="";
 public int floorIndex,order;
 public ExpeditionNodeKind kind;
 public List<string> nextNodeIds=new();
}

[Serializable]
public sealed class ExpeditionPathRule {
 public string id="",title="";
 public int battles,events,rests,others;
 public ExpeditionPathRule(){}
 public ExpeditionPathRule(string id,string title,int battles,int events,int rests,int others){
  this.id=id;this.title=title;this.battles=battles;this.events=events;this.rests=rests;this.others=others;
 }
}

[Serializable]
public sealed class ExpeditionRouteGenerationRules {
 public int floorCount=3;
 public int minimumBattlesPerPath=4,maximumBattlesPerPath=8;
 public int alertStartsOnDay,pursuitStartsOnDay;
 public List<ExpeditionPathRule> paths=new();
}

public sealed class ExpeditionRouteValidationReport {
 public readonly List<string> errors=new();
 public readonly List<string> warnings=new();
 public bool IsValid=>errors.Count==0;
}

/// <summary>
/// Generates the long-form expedition graph independently from its eventual UI.
/// Each authored path reconverges at a floor boss; the boss then branches into
/// the next floor's authored paths. Product thresholds remain data, not code.
/// </summary>
public static class ExpeditionRoutePlanSystem {
 public static ExpeditionRoutePlan Generate(int seed,ExpeditionRouteGenerationRules rules){
  ValidateRules(rules);
  var plan=new ExpeditionRoutePlan{
   seed=seed,
   alertStartsOnDay=rules.alertStartsOnDay,
   pursuitStartsOnDay=rules.pursuitStartsOnDay
  };
  var random=new Random(seed);

  for(int floorIndex=0;floorIndex<rules.floorCount;floorIndex++){
   var floor=new ExpeditionFloorPlan{floorIndex=floorIndex};
   foreach(var pathRule in rules.paths){
    var path=new ExpeditionPathPlan{
     id=$"f{floorIndex+1}-{pathRule.id}",title=pathRule.title,battleCount=pathRule.battles
    };
    var kinds=BuildKinds(pathRule);
    Shuffle(kinds,random);
    for(int order=0;order<kinds.Count;order++){
     var node=new ExpeditionRouteNodePlan{
      id=$"{path.id}-{order+1:00}",pathId=path.id,floorIndex=floorIndex,order=order,kind=kinds[order]
     };
     path.nodeIds.Add(node.id);
     floor.nodes.Add(node);
    }
    LinkSequential(path,floor);
    floor.paths.Add(path);
   }

   var boss=new ExpeditionRouteNodePlan{
    id=$"f{floorIndex+1}-boss",floorIndex=floorIndex,order=int.MaxValue,kind=ExpeditionNodeKind.Boss
   };
   floor.bossNodeId=boss.id;
   floor.nodes.Add(boss);
   foreach(var path in floor.paths){
    var tail=Node(floor,path.nodeIds.Last());
    tail.nextNodeIds.Add(boss.id);
   }
   plan.floors.Add(floor);
  }

  for(int floorIndex=0;floorIndex<plan.floors.Count-1;floorIndex++){
   var current=plan.floors[floorIndex];
   var next=plan.floors[floorIndex+1];
   Node(current,current.bossNodeId).nextNodeIds.AddRange(next.paths.Select(path=>path.nodeIds[0]));
  }
  plan.startNodeIds.AddRange(plan.floors[0].paths.Select(path=>path.nodeIds[0]));
  return plan;
 }

 public static ExpeditionDayStage DayStage(ExpeditionRoutePlan plan,int day){
  if(plan==null)throw new ArgumentNullException(nameof(plan));
  if(day>=plan.pursuitStartsOnDay)return ExpeditionDayStage.Pursuit;
  if(day>=plan.alertStartsOnDay)return ExpeditionDayStage.Alert;
  return ExpeditionDayStage.Quiet;
 }

 public static ExpeditionRouteValidationReport Validate(
  ExpeditionRoutePlan plan,ExpeditionRouteGenerationRules rules){
  var report=new ExpeditionRouteValidationReport();
  if(plan==null){report.errors.Add("Route plan is null.");return report;}
  if(rules==null){report.errors.Add("Generation rules are null.");return report;}
  if(plan.floors==null||plan.floors.Count!=rules.floorCount)
   report.errors.Add($"Expected {rules.floorCount} floors, found {plan.floors?.Count??0}.");
  if(plan.alertStartsOnDay<0||plan.pursuitStartsOnDay<=plan.alertStartsOnDay)
   report.errors.Add("Day-stage thresholds must be ordered Quiet < Alert < Pursuit.");

  var ids=new HashSet<string>();
  foreach(var floor in plan.floors??new List<ExpeditionFloorPlan>()){
   if(floor.paths==null||floor.paths.Count<2)
    report.errors.Add($"Floor {floor.floorIndex+1} needs at least two route choices.");
   var boss=floor.nodes?.FirstOrDefault(node=>node.id==floor.bossNodeId);
   if(boss==null||boss.kind!=ExpeditionNodeKind.Boss)
    report.errors.Add($"Floor {floor.floorIndex+1} has no valid boss node.");
   foreach(var node in floor.nodes??new List<ExpeditionRouteNodePlan>())
    if(string.IsNullOrEmpty(node.id)||!ids.Add(node.id))report.errors.Add($"Duplicate or empty node id '{node.id}'.");
   foreach(var path in floor.paths??new List<ExpeditionPathPlan>()){
    int battles=(path.nodeIds??new List<string>())
     .Select(id=>floor.nodes?.FirstOrDefault(node=>node.id==id))
     .Count(node=>node?.kind==ExpeditionNodeKind.Battle);
    if(battles<rules.minimumBattlesPerPath||battles>rules.maximumBattlesPerPath)
     report.errors.Add($"Path {path.id} has {battles} battles; expected {rules.minimumBattlesPerPath}-{rules.maximumBattlesPerPath}.");
   }
  }
  return report;
 }

 static void ValidateRules(ExpeditionRouteGenerationRules rules){
  if(rules==null)throw new ArgumentNullException(nameof(rules));
  if(rules.floorCount<=0)throw new ArgumentOutOfRangeException(nameof(rules.floorCount));
  if(rules.paths==null||rules.paths.Count<2)throw new InvalidOperationException("At least two route paths are required.");
  if(rules.alertStartsOnDay<0||rules.pursuitStartsOnDay<=rules.alertStartsOnDay)
   throw new InvalidOperationException("Day-stage thresholds must be ordered Quiet < Alert < Pursuit.");
  var ids=new HashSet<string>();
  foreach(var path in rules.paths){
   if(path==null||string.IsNullOrWhiteSpace(path.id)||!ids.Add(path.id))
    throw new InvalidOperationException("Every path needs a unique id.");
   if(path.battles<rules.minimumBattlesPerPath||path.battles>rules.maximumBattlesPerPath)
    throw new InvalidOperationException($"Path {path.id} must contain {rules.minimumBattlesPerPath}-{rules.maximumBattlesPerPath} battles.");
   if(path.events<0||path.rests<0||path.others<0)
    throw new InvalidOperationException($"Path {path.id} has a negative node count.");
  }
 }

 static List<ExpeditionNodeKind> BuildKinds(ExpeditionPathRule rule){
  var kinds=new List<ExpeditionNodeKind>(rule.battles+rule.events+rule.rests+rule.others);
  Add(kinds,ExpeditionNodeKind.Battle,rule.battles);
  Add(kinds,ExpeditionNodeKind.Event,rule.events);
  Add(kinds,ExpeditionNodeKind.Rest,rule.rests);
  Add(kinds,ExpeditionNodeKind.Other,rule.others);
  return kinds;
 }

 static void Add(List<ExpeditionNodeKind> values,ExpeditionNodeKind kind,int count){
  for(int index=0;index<count;index++)values.Add(kind);
 }

 static void Shuffle<T>(IList<T> values,Random random){
  for(int index=values.Count-1;index>0;index--){
   int swap=random.Next(index+1);
   (values[index],values[swap])=(values[swap],values[index]);
  }
 }

 static void LinkSequential(ExpeditionPathPlan path,ExpeditionFloorPlan floor){
  for(int index=0;index<path.nodeIds.Count-1;index++)
   Node(floor,path.nodeIds[index]).nextNodeIds.Add(path.nodeIds[index+1]);
 }

 static ExpeditionRouteNodePlan Node(ExpeditionFloorPlan floor,string id)=>
  floor.nodes.First(node=>node.id==id);
}
}
