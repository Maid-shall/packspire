using UnityEngine;

namespace Packspire {
/// <summary>
/// Presentation-only forecast of DungeonAxes deltas. Does not mutate RunState.
/// Numbers stay internal — UI must never surface "+2" style labels in normal play.
/// </summary>
public static class RouteAxisForecast {
 public struct Values {
  public int alert,collapse,corruption;
  public bool unknown;
  public bool any=>alert!=0||collapse!=0||corruption!=0||unknown;
  public static Values Of(int a,int c,int e,bool unknown=false)=>new(){alert=a,collapse=c,corruption=e,unknown=unknown};
  public static Values None=>default;
 }

 public enum Magnitude { None, Slight, Large, IntoDanger, Unknown }

 /// <summary>Forecast for a world exit / path choice (first-visit heuristics only).</summary>
 public static Values ForExit(ExplorationLinkKind kind,bool opened,bool investigate,ExplorationCellDef target,bool alreadyVisited){
  if(investigate)return Values.Of(0,0,0);
  if(kind==ExplorationLinkKind.Breach&&!opened)return Values.Of(0,2,0);
  if(alreadyVisited)return Values.None;
  if(target==null)return Values.Of(1,0,0);
  return target.type switch{
   "battle" or "boss"=>Values.Of(1,0,1),
   "event" or "treasure"=>Values.Of(1,0,2),
   "rest"=>Values.Of(1,-1,0),
   "building_door"=>Values.None,
   _=>Values.Of(1,0,0),
  };
 }

 public static Values ForEventChoice(int choiceIndex){
  // Mirrors UiResolveExplorationEvent side-effects that touch axes (none today),
  // plus presentation flavour so DEV / future events can preview.
  return choiceIndex switch{
   0=>Values.Of(1,0,1),
   1=>Values.Of(0,-1,0),
   2=>Values.Of(0,0,0),
   _=>Values.Of(0,0,0,unknown:true),
  };
 }

 public static Magnitude Classify(Values v,DungeonAxes current){
  if(v.unknown)return Magnitude.Unknown;
  if(!v.any)return Magnitude.None;
  int peak=Mathf.Max(Mathf.Abs(v.alert),Mathf.Max(Mathf.Abs(v.collapse),Mathf.Abs(v.corruption)));
  bool intoDanger=
   WouldCrossDanger(current?.alert??0,v.alert)
   ||WouldCrossDanger(current?.collapse??0,v.collapse)
   ||WouldCrossDanger(current?.corruption??0,v.corruption);
  if(intoDanger)return Magnitude.IntoDanger;
  if(peak>=3)return Magnitude.Large;
  return Magnitude.Slight;
 }

 static bool WouldCrossDanger(int cur,int delta){
  if(delta==0)return false;
  int next=Mathf.Clamp(cur+delta,-15,15);
  return Mathf.Abs(cur)<13&&Mathf.Abs(next)>=13;
 }

 public static string HoverBlurb(string axisId)=>axisId switch{
  "alert"=>"警戒\n敵が遠征隊の存在を察知している度合い",
  "collapse"=>"崩壊\n区域や建造物が不安定になっている度合い",
  "corruption"=>"侵蝕\nダンジョン固有の異常に染まっている度合い",
  _=>"観測軸",
 };

 public static string QualitativeBand(int value){
  int a=Mathf.Abs(Mathf.Clamp(value,-15,15));
  if(value<=-15)return "最低値";
  if(value>=15)return "限界";
  if(a>=13)return "限界直前";
  if(a>=8)return "高い";
  if(a>=4)return "中程度";
  return "低い";
 }
}
}
