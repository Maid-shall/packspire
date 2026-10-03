using System;

namespace Packspire {
/// <summary>
/// Owns cumulative expedition time while the legacy courier route remains the
/// presentation bridge. ExpeditionRoutePlan is authoritative; the courier value
/// is mirrored so old saves and views continue to work during migration.
/// </summary>
public static class ExpeditionProgressSystem {
 public static ExpeditionRoutePlan Ensure(RunState run,MetaSave meta=null){
  if(run==null)throw new ArgumentNullException(nameof(run));
  if(run.expeditionPlan==null||run.expeditionPlan.schemaVersion!=ExpeditionRoutePlanSystem.CurrentSchemaVersion){
   int elapsedDays=Math.Max(0,run.expeditionPlan?.elapsedDays??0);
   bool fourthStage=run.expeditionPlan?.fourthDayStageEnabled??false;
   run.expeditionPlan=ExpeditionRoutePlanSystem.GenerateDefault(run.dungeon,meta?.runs??0);
   run.expeditionPlan.elapsedDays=elapsedDays;
   run.expeditionPlan.fourthDayStageEnabled|=fourthStage;
  }
  Normalize(run.expeditionPlan);
  SynchronizeElapsedDays(run);
  return run.expeditionPlan;
 }

 public static int AdvanceDays(RunState run,int dayDelta){
  if(run==null)throw new ArgumentNullException(nameof(run));
  var plan=Ensure(run);
  int current=SynchronizeElapsedDays(run);
  int delta=Math.Max(0,dayDelta);
  int next=current>int.MaxValue-delta?int.MaxValue:current+delta;
  plan.elapsedDays=next;
  if(run.courierRoute!=null)run.courierRoute.daysElapsed=next;
  return next;
 }

 public static ExpeditionDayStage CurrentDayStage(RunState run){
  var plan=Ensure(run);
  return ExpeditionRoutePlanSystem.DayStage(plan,plan.elapsedDays);
 }

 public static void EnableFourthDayStage(RunState run){
  ExpeditionRoutePlanSystem.EnableFourthDayStage(Ensure(run));
 }

 static int SynchronizeElapsedDays(RunState run){
  int planDays=Math.Max(0,run.expeditionPlan?.elapsedDays??0);
  int legacyDays=Math.Max(0,run.courierRoute?.daysElapsed??0);
  int elapsed=Math.Max(planDays,legacyDays);
  if(run.expeditionPlan!=null)run.expeditionPlan.elapsedDays=elapsed;
  if(run.courierRoute!=null)run.courierRoute.daysElapsed=elapsed;
  return elapsed;
 }

 static void Normalize(ExpeditionRoutePlan plan){
  plan.startNodeIds??=new();
  plan.resolvedNodeIds??=new();
  plan.committedNodeIds??=new();
  plan.committedPathIds??=new();
  plan.floors??=new();
  foreach(var floor in plan.floors){
   if(floor==null)continue;
   floor.startNodeIds??=new();
   floor.paths??=new();
   floor.nodes??=new();
   foreach(var path in floor.paths)if(path!=null)path.nodeIds??=new();
   foreach(var node in floor.nodes)if(node!=null)node.nextNodeIds??=new();
  }
 }
}
}
