using System;
using System.Collections.Generic;

namespace Packspire {
public sealed class RealtimeEnemyTimelineAuditReport {
 public double cycleDuration,averageDamagePerSecond,minimumTelegraphLead,maximumQuietSeconds,windowSeconds;
 public int actionCount,totalPotentialDamage,reactionActionCount,maximumActionsInWindow,maximumDamageInWindow;
 public readonly List<RealtimeEnemyPatternAudit> patterns=new();
}

public sealed class RealtimeEnemyPatternAudit {
 public string id="";
 public double duration,averageDamagePerSecond,minimumTelegraphLead,maximumQuietSeconds;
 public int actionCount,totalPotentialDamage,reactionActionCount;
}

/// <summary>
/// Static balance instrumentation for authored realtime enemy phrases. It does
/// not simulate player cards; it measures the readable pressure authored into
/// the enemy timeline so encounter balance can be reviewed before playtests.
/// </summary>
public static class RealtimeEnemyTimelineAudit {
 readonly struct Occurrence {
  public readonly double time;
  public readonly int damage;
  public Occurrence(double time,int damage){this.time=time;this.damage=damage;}
 }

 public static RealtimeEnemyTimelineAuditReport Analyze(
  IReadOnlyList<RealtimeEnemyTimelinePattern> patterns,double windowSeconds=10d){
  if(patterns==null||patterns.Count==0)throw new ArgumentException("At least one pattern is required.",nameof(patterns));
  if(windowSeconds<=0d)throw new ArgumentOutOfRangeException(nameof(windowSeconds));

  var report=new RealtimeEnemyTimelineAuditReport{
   windowSeconds=windowSeconds,
   minimumTelegraphLead=double.PositiveInfinity
  };
  var cycleOccurrences=new List<Occurrence>();
  double cursor=0d;
  for(int index=0;index<patterns.Count;index++){
   var pattern=patterns[index]??throw new ArgumentException("Patterns cannot contain null entries.",nameof(patterns));
   var patternReport=AnalyzePattern(pattern);
   report.patterns.Add(patternReport);
   report.cycleDuration+=pattern.Duration;
   report.actionCount+=patternReport.actionCount;
   report.totalPotentialDamage+=patternReport.totalPotentialDamage;
   report.reactionActionCount+=patternReport.reactionActionCount;
   report.minimumTelegraphLead=Math.Min(report.minimumTelegraphLead,patternReport.minimumTelegraphLead);
   foreach(var step in pattern.Steps)
    cycleOccurrences.Add(new Occurrence(cursor+step.ExecuteOffset,PotentialDamage(step)));
   cursor+=pattern.Duration;
  }

  report.averageDamagePerSecond=report.cycleDuration<=0d?0d:report.totalPotentialDamage/report.cycleDuration;
  if(double.IsPositiveInfinity(report.minimumTelegraphLead))report.minimumTelegraphLead=0d;
  report.maximumQuietSeconds=MaximumCyclicQuiet(cycleOccurrences,report.cycleDuration);
  MeasureWindow(cycleOccurrences,report.cycleDuration,windowSeconds,
   out report.maximumActionsInWindow,out report.maximumDamageInWindow);
  return report;
 }

 static RealtimeEnemyPatternAudit AnalyzePattern(RealtimeEnemyTimelinePattern pattern){
  var report=new RealtimeEnemyPatternAudit{
   id=pattern.Id,duration=pattern.Duration,actionCount=pattern.Steps.Count,
   minimumTelegraphLead=double.PositiveInfinity
  };
  var occurrences=new List<Occurrence>(pattern.Steps.Count);
  foreach(var step in pattern.Steps){
   int damage=PotentialDamage(step);
   report.totalPotentialDamage+=damage;
   if(IsReaction(step.Kind))report.reactionActionCount++;
   report.minimumTelegraphLead=Math.Min(report.minimumTelegraphLead,step.TelegraphLead);
   occurrences.Add(new Occurrence(step.ExecuteOffset,damage));
  }
  report.averageDamagePerSecond=report.duration<=0d?0d:report.totalPotentialDamage/report.duration;
  if(double.IsPositiveInfinity(report.minimumTelegraphLead))report.minimumTelegraphLead=0d;
  report.maximumQuietSeconds=MaximumLinearQuiet(occurrences,report.duration);
  return report;
 }

 static int PotentialDamage(RealtimeEnemyTimelineStep step)=>step.Damage*step.HitCount;
 static bool IsReaction(RealtimeEnemyActionKind kind)=>
  kind==RealtimeEnemyActionKind.JumpReaction||kind==RealtimeEnemyActionKind.BraceReaction;

 static double MaximumLinearQuiet(List<Occurrence> occurrences,double duration){
  if(occurrences.Count==0)return duration;
  occurrences.Sort((left,right)=>left.time.CompareTo(right.time));
  double maximum=occurrences[0].time;
  for(int index=1;index<occurrences.Count;index++)
   maximum=Math.Max(maximum,occurrences[index].time-occurrences[index-1].time);
  return Math.Max(maximum,duration-occurrences[^1].time);
 }

 static double MaximumCyclicQuiet(List<Occurrence> occurrences,double duration){
  if(occurrences.Count==0)return duration;
  occurrences.Sort((left,right)=>left.time.CompareTo(right.time));
  double maximum=0d;
  for(int index=1;index<occurrences.Count;index++)
   maximum=Math.Max(maximum,occurrences[index].time-occurrences[index-1].time);
  return Math.Max(maximum,duration-occurrences[^1].time+occurrences[0].time);
 }

 static void MeasureWindow(
  List<Occurrence> cycle,double cycleDuration,double window,
  out int maximumActions,out int maximumDamage){
  maximumActions=0;
  maximumDamage=0;
  if(cycle.Count==0||cycleDuration<=0d)return;
  cycle.Sort((left,right)=>left.time.CompareTo(right.time));
  int repeatCount=Math.Max(2,(int)Math.Ceiling(window/cycleDuration)+2);
  var expanded=new List<Occurrence>(cycle.Count*repeatCount);
  for(int repeat=0;repeat<repeatCount;repeat++)
   foreach(var occurrence in cycle)
    expanded.Add(new Occurrence(occurrence.time+repeat*cycleDuration,occurrence.damage));

  int end=0,damage=0;
  for(int start=0;start<cycle.Count;start++){
   if(end<start){end=start;damage=0;}
   while(end<expanded.Count&&expanded[end].time-expanded[start].time<=window){
    damage+=expanded[end].damage;
    end++;
   }
   maximumActions=Math.Max(maximumActions,end-start);
   maximumDamage=Math.Max(maximumDamage,damage);
   damage-=expanded[start].damage;
  }
 }
}
}
