using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire {
/// <summary>
/// Large analog three-axis instrument (警戒 / 崩壊 / 侵蝕).
/// Pivot sits in the bottom-right; needles sweep into the on-screen (upper-left) fan.
/// </summary>
public sealed class RouteAxisInstrument {
 public VisualElement Root{get;private set;}
 public bool IsHovering{get;private set;}

 VisualElement hitArea,body,ringOuter,ringMid,ringInner,marks;
 VisualElement needleAlert,needleCollapse,needleCorruption;
 VisualElement ghostAlert,ghostCollapse,ghostCorruption;
 VisualElement glowAlert,glowCollapse,glowCorruption;
 VisualElement tip;
 Label tipBody;
 VisualElement crackA,crackB,noise;

 RouteAxisForecast.Values current;
 RouteAxisForecast.Values preview;
 bool previewActive,previewUnknown;
 float commitT,anomalyT,idleT;
 float shownAlert,shownCollapse,shownCorruption;
 bool dimmed;

 public System.Action OnDriveSoundHook; // reserved for future SFX

 // Visible fan: needles grow toward screen center (up-left), never into the clipped SE.
 // UITK rotate: 0 = up, negative = counter-clockwise (toward left / screen center).
 const float AngleAtMin=-12f;
 const float AngleAtMax=-108f;
 const float AlertBias=0f;
 const float CollapseBias=10f;
 const float CorruptionBias=-10f;

 public void Build(VisualElement parent){
  // Root ignores picks so the large clipped frame / tip never steal dialogue clicks.
  Root=new VisualElement{name="route-axis-instrument",pickingMode=PickingMode.Ignore};
  Root.AddToClassList("ps-axis-instrument");

  body=Ve("ps-axis-instrument-body");
  Root.Add(body);

  ringOuter=Ve("ps-axis-ring ps-axis-ring-outer");body.Add(ringOuter);
  ringMid=Ve("ps-axis-ring ps-axis-ring-mid");body.Add(ringMid);
  ringInner=Ve("ps-axis-ring ps-axis-ring-inner");body.Add(ringInner);

  marks=Ve("ps-axis-marks");body.Add(marks);
  // Tick marks only in the inward (visible) fan.
  for(int i=0;i<7;i++){
   float a=Mathf.Lerp(AngleAtMin,AngleAtMax,i/6f);
   var tick=Ve("ps-axis-tick");
   tick.style.rotate=new Rotate(Angle.Degrees(a));
   marks.Add(tick);
  }

  body.Add(Stamp("ps-axis-stamp ps-axis-stamp-alert","∧"));
  body.Add(Stamp("ps-axis-stamp ps-axis-stamp-collapse","◇"));
  body.Add(Stamp("ps-axis-stamp ps-axis-stamp-corruption","※"));

  glowAlert=Ve("ps-axis-glow ps-axis-glow-alert");body.Add(glowAlert);
  glowCollapse=Ve("ps-axis-glow ps-axis-glow-collapse");body.Add(glowCollapse);
  glowCorruption=Ve("ps-axis-glow ps-axis-glow-corruption");body.Add(glowCorruption);

  ghostAlert=Needle("ps-axis-ghost ps-axis-needle-alert",true);
  ghostCollapse=Needle("ps-axis-ghost ps-axis-needle-collapse",true);
  ghostCorruption=Needle("ps-axis-ghost ps-axis-needle-corruption",true);
  needleAlert=Needle("ps-axis-needle ps-axis-needle-alert",false);
  needleCollapse=Needle("ps-axis-needle ps-axis-needle-collapse",false);
  needleCorruption=Needle("ps-axis-needle ps-axis-needle-corruption",false);

  crackA=Ve("ps-axis-crack ps-axis-crack-a");body.Add(crackA);
  crackB=Ve("ps-axis-crack ps-axis-crack-b");body.Add(crackB);
  noise=Ve("ps-axis-noise");body.Add(noise);

  tip=Ve("ps-axis-instrument-tip");
  tip.pickingMode=PickingMode.Ignore;
  tip.style.display=DisplayStyle.None;
  tipBody=new Label(""){pickingMode=PickingMode.Ignore};
  tipBody.AddToClassList("ps-axis-instrument-tip-body");
  tip.Add(tipBody);
  Root.Add(tip);

  // Compact hit pad on the visible NW sector — last so it sits above Ignore children.
  hitArea=new VisualElement{pickingMode=PickingMode.Position};
  hitArea.AddToClassList("ps-axis-instrument-hit");
  Root.Add(hitArea);

  // Tooltip ONLY on this pad — never when preview comes from dialogue choices.
  hitArea.RegisterCallback<PointerEnterEvent>(_=>{
   IsHovering=true;
   RefreshTip();
  });
  hitArea.RegisterCallback<PointerLeaveEvent>(_=>{
   IsHovering=false;
   HideTip();
  });

  SetGhostVisible(false);
  parent.Add(Root);
  ApplyNeedlePose(needleAlert,0,0f,0f,1f,AlertBias);
  ApplyNeedlePose(needleCollapse,0,0f,0f,.9f,CollapseBias);
  ApplyNeedlePose(needleCorruption,0,0f,0f,.8f,CorruptionBias);
 }

 public void Detach(){
  Root?.RemoveFromHierarchy();
  Root=null;hitArea=null;body=null;tip=null;tipBody=null;
  needleAlert=needleCollapse=needleCorruption=null;
  ghostAlert=ghostCollapse=ghostCorruption=null;
  IsHovering=false;
 }

 public void SetDimmed(bool value){
  dimmed=value;
  if(Root==null)return;
  Root.EnableInClassList("ps-axis-instrument-dim",value&&!previewActive);
  Root.EnableInClassList("ps-axis-instrument-preview-focus",previewActive);
 }

 public void SetCurrent(DungeonAxes axes,bool animateCommit=false){
  if(axes==null)return;
  var next=RouteAxisForecast.Values.Of(axes.alert,axes.collapse,axes.corruption);
  bool changed=next.alert!=current.alert||next.collapse!=current.collapse||next.corruption!=current.corruption;
  current=next;
  if(!changed&&!animateCommit)return;
  if(animateCommit||changed){
   commitT=animateCommit?0.55f:0.35f;
   if(IsThreshold(Mathf.RoundToInt(shownAlert),current.alert)
    ||IsThreshold(Mathf.RoundToInt(shownCollapse),current.collapse)
    ||IsThreshold(Mathf.RoundToInt(shownCorruption),current.corruption)){
    anomalyT=1.2f;
    OnDriveSoundHook?.Invoke();
   } else anomalyT=Mathf.Max(anomalyT,.4f);
  }
  ClearPreview();
 }

 public void ShowPreview(RouteAxisForecast.Values values){
  preview=values;
  previewActive=values.any||values.unknown;
  previewUnknown=values.unknown;
  if(Root==null)return;
  SetGhostVisible(previewActive);
  SetDimmed(dimmed);
  Root.EnableInClassList("ps-axis-instrument-preview-focus",previewActive);
  UpdateGlow(preview);
  // Preview must NEVER open the explanation tip.
  if(!IsHovering)HideTip();
 }

 public void ClearPreview(){
  previewActive=false;
  previewUnknown=false;
  preview=default;
  SetGhostVisible(false);
  UpdateGlow(RouteAxisForecast.Values.None);
  if(Root!=null){
   Root.EnableInClassList("ps-axis-instrument-preview-focus",false);
   Root.EnableInClassList("ps-axis-instrument-dim",dimmed);
  }
  if(!IsHovering)HideTip();
 }

 public void CommitAnimatedValue(DungeonAxes axes){
  SetCurrent(axes,true);
 }

 public void Tick(float dt){
  if(Root==null)return;
  idleT+=dt;
  if(commitT>0f)commitT=Mathf.Max(0f,commitT-dt);
  if(anomalyT>0f)anomalyT=Mathf.Max(0f,anomalyT-dt);

  float blend=commitT>0f?Mathf.Clamp01(1f-commitT/.55f):1f;
  float speed=Mathf.Max(.15f,blend)*.18f;
  shownAlert=Mathf.Lerp(shownAlert,current.alert,speed);
  shownCollapse=Mathf.Lerp(shownCollapse,current.collapse,speed);
  shownCorruption=Mathf.Lerp(shownCorruption,current.corruption,speed);
  if(Mathf.Abs(shownAlert-current.alert)<.05f)shownAlert=current.alert;
  if(Mathf.Abs(shownCollapse-current.collapse)<.05f)shownCollapse=current.collapse;
  if(Mathf.Abs(shownCorruption-current.corruption)<.05f)shownCorruption=current.corruption;

  float t=Time.unscaledTime;
  int ia=Mathf.RoundToInt(shownAlert),ic=Mathf.RoundToInt(shownCollapse),ie=Mathf.RoundToInt(shownCorruption);
  float stress=Stress(ia,ic,ie);
  ApplyNeedlePose(needleAlert,ia,t,anomalyT,1f+stress*.08f,AlertBias);
  ApplyNeedlePose(needleCollapse,ic,t*1.13f,anomalyT,.9f+stress*.06f,CollapseBias);
  ApplyNeedlePose(needleCorruption,ie,t*.91f,anomalyT,.8f+stress*.06f,CorruptionBias);

  if(previewActive){
   if(previewUnknown){
    float wobble=Mathf.Sin(t*11f)*6f+Mathf.Sin(t*17f)*4f;
    ApplyNeedlePose(ghostAlert,ia,t,0f,1f,AlertBias+wobble);
    ApplyNeedlePose(ghostCollapse,ic,t,0f,.9f,CollapseBias-wobble*.7f);
    ApplyNeedlePose(ghostCorruption,ie,t,0f,.8f,CorruptionBias+wobble*.5f);
   } else {
    ApplyNeedlePose(ghostAlert,Clamp(ia+preview.alert),t,0f,1f,AlertBias);
    ApplyNeedlePose(ghostCollapse,Clamp(ic+preview.collapse),t,0f,.9f,CollapseBias);
    ApplyNeedlePose(ghostCorruption,Clamp(ie+preview.corruption),t,0f,.8f,CorruptionBias);
   }
  }

  float ringSpin=idleT*(6f+stress*10f);
  if(ringOuter!=null)ringOuter.style.rotate=new Rotate(Angle.Degrees(ringSpin*.15f));
  if(ringMid!=null)ringMid.style.rotate=new Rotate(Angle.Degrees(-ringSpin*.22f));
  if(ringInner!=null)ringInner.style.rotate=new Rotate(Angle.Degrees(ringSpin*.31f));
  float crackOp=Mathf.Clamp01((stress-.55f)/.45f)*.55f+(anomalyT*.25f);
  if(crackA!=null)crackA.style.opacity=crackOp;
  if(crackB!=null)crackB.style.opacity=crackOp*.8f;
  if(noise!=null)noise.style.opacity=Mathf.Clamp01(stress-.4f)*.35f+anomalyT*.2f;

  // Tip stays tied to direct hover only.
  if(!IsHovering&&tip!=null&&tip.style.display==DisplayStyle.Flex)HideTip();
 }

 void RefreshTip(){
  if(tip==null||tipBody==null||!IsHovering)return;
  tipBody.text=
   $"警戒 — {RouteAxisForecast.QualitativeBand(Mathf.RoundToInt(shownAlert))}\n敵が遠征隊の存在を察知している度合い\n\n"+
   $"崩壊 — {RouteAxisForecast.QualitativeBand(Mathf.RoundToInt(shownCollapse))}\n区域や建造物が不安定になっている度合い\n\n"+
   $"侵蝕 — {RouteAxisForecast.QualitativeBand(Mathf.RoundToInt(shownCorruption))}\nダンジョン固有の異常に染まっている度合い";
  tip.style.display=DisplayStyle.Flex;
 }

 void HideTip(){
  if(tip!=null)tip.style.display=DisplayStyle.None;
 }

 void UpdateGlow(RouteAxisForecast.Values v){
  bool danger=RouteAxisForecast.Classify(v,ToAxes(current))==RouteAxisForecast.Magnitude.IntoDanger;
  SetGlow(glowAlert,v.alert!=0||v.unknown,danger&&(v.alert!=0||v.unknown));
  SetGlow(glowCollapse,v.collapse!=0||v.unknown,danger&&(v.collapse!=0||v.unknown));
  SetGlow(glowCorruption,v.corruption!=0||v.unknown,danger&&(v.corruption!=0||v.unknown));
  if(Root!=null)Root.EnableInClassList("ps-axis-instrument-danger",danger&&previewActive);
 }

 static DungeonAxes ToAxes(RouteAxisForecast.Values v)=>new(){alert=v.alert,collapse=v.collapse,corruption=v.corruption};

 static void SetGlow(VisualElement el,bool on,bool danger){
  if(el==null)return;
  el.EnableInClassList("ps-axis-glow-on",on);
  el.EnableInClassList("ps-axis-glow-danger",danger);
  el.style.opacity=on?(danger?.75f:.55f):0f;
 }

 void SetGhostVisible(bool on){
  void G(VisualElement g){if(g!=null)g.style.display=on?DisplayStyle.Flex:DisplayStyle.None;}
  G(ghostAlert);G(ghostCollapse);G(ghostCorruption);
 }

 static VisualElement Ve(string classes){
  var el=new VisualElement{pickingMode=PickingMode.Ignore};
  foreach(var c in classes.Split(' '))if(!string.IsNullOrEmpty(c))el.AddToClassList(c);
  return el;
 }

 VisualElement Needle(string classes,bool ghost){
  var n=Ve(classes);
  body.Add(n);
  if(ghost)n.style.display=DisplayStyle.None;
  return n;
 }

 static VisualElement Stamp(string classes,string glyph){
  var el=Ve(classes);
  var lab=new Label(glyph){pickingMode=PickingMode.Ignore};
  lab.AddToClassList("ps-axis-stamp-glyph");
  el.Add(lab);
  return el;
 }

 /// <summary>
 /// Map axis [-15,15] onto the inward fan [AngleAtMin, AngleAtMax].
 /// Rising values move the tip toward screen center (up-left), never into the clipped SE.
 /// </summary>
 static float AngleForAxis(float axis,float bias){
  float n=Mathf.Clamp(axis,-15f,15f)/15f; // -1..1
  float t=(n+1f)*.5f;                     // 0..1
  return Mathf.Lerp(AngleAtMin,AngleAtMax,t)+bias;
 }

 static void ApplyNeedlePose(VisualElement needle,int axis,float time,float anomaly,float lengthScale,float bias){
  if(needle==null)return;
  float n=Mathf.Abs(Mathf.Clamp(axis,-15,15))/15f;
  float swing=n*.12f+.03f;
  // Keep jitter small so tips stay inside the inward fan.
  float jitter=Mathf.Sin(time*5.2f*(.7f+n))*swing*4f;
  float spike=anomaly>0f?Mathf.Sin(anomaly*26f)*5f*anomaly:0f;
  float lo=Mathf.Min(AngleAtMin,AngleAtMax)-6f;
  float hi=Mathf.Max(AngleAtMin,AngleAtMax)+6f;
  float angle=Mathf.Clamp(AngleForAxis(axis,bias)+jitter+spike,lo,hi);
  needle.style.rotate=new Rotate(Angle.Degrees(angle));
  needle.style.scale=new Scale(new Vector2(1f,lengthScale));
 }

 static float Stress(int a,int c,int e){
  float m=Mathf.Max(Mathf.Abs(a),Mathf.Max(Mathf.Abs(c),Mathf.Abs(e)))/15f;
  return Mathf.Clamp01(m);
 }

 static int Clamp(int v)=>Mathf.Clamp(v,-15,15);

 static bool IsThreshold(int from,int to){
  int[] marks={-15,-13,-9,9,13,15};
  foreach(int m in marks)if((from<m&&to>=m)||(from>m&&to<=m)||(Mathf.Abs(to)==15&&from!=to))return true;
  return false;
 }
}
}
