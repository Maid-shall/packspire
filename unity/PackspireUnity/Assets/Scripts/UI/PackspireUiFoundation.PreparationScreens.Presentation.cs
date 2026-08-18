using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire {
public sealed partial class PackspireUiFoundation {
// Rite-circle, resonance, and filter presentation.

 sealed class RiteCircleFx {
  public string coreId;
  public string conduitId;
  public string resonanceId;
  public string stabilityId;
 }

 VisualElement BuildMagicCircleLayers(ActiveStorageFormula formula){
  var layers=Container("ps-rite-circle-layers");
  layers.pickingMode=PickingMode.Ignore;
  string coreId=string.IsNullOrEmpty(formula.core?.id)?StorageFormulaCatalog.DefaultCoreId:formula.core.id;
  string conduitId=string.IsNullOrEmpty(formula.conduit?.id)?StorageFormulaCatalog.DefaultConduitId:formula.conduit.id;
  string resonanceId=string.IsNullOrEmpty(formula.resonance?.id)?StorageFormulaCatalog.DefaultResonanceId:formula.resonance.id;
  string stabilityId=string.IsNullOrEmpty(formula.stability?.id)?StorageFormulaCatalog.DefaultStabilityId:formula.stability.id;
  layers.AddToClassList("ps-rite-core-"+coreId);
  layers.AddToClassList("ps-rite-conduit-"+conduitId);
  layers.AddToClassList("ps-rite-resonance-"+resonanceId);
  layers.AddToClassList("ps-rite-stability-"+stabilityId);
  layers.userData=new RiteCircleFx{coreId=coreId,conduitId=conduitId,resonanceId=resonanceId,stabilityId=stabilityId};

  Color conduitTint=ConduitTint(conduitId);
  Color conduitGlow=ConduitGlow(conduitId);

  // Glow wash ← 属性導線（色）
  var glow=Container("ps-rite-circle-glow");
  glow.pickingMode=PickingMode.Ignore;
  glow.style.backgroundColor=new StyleColor(conduitGlow);
  layers.Add(glow);

  // Shared base frame, colored by conduit
  var baseTex=PackspireResources.Load<Texture2D>("Art/Rite/rite-circle-base-v1");
  if(baseTex!=null){
   var art=new Image{image=baseTex,scaleMode=ScaleMode.ScaleToFit,pickingMode=PickingMode.Ignore};
   art.AddToClassList("ps-rite-circle-art");
   art.tintColor=conduitTint;
   layers.Add(art);
  }

  // Shape accent ← 収納核（形）
  var shapeTex=PackspireResources.Load<Texture2D>("Art/Rite/rite-accent-"+coreId+"-v1");
  if(shapeTex!=null){
   var shape=new Image{image=shapeTex,scaleMode=ScaleMode.ScaleToFit,pickingMode=PickingMode.Ignore};
   shape.AddToClassList("ps-rite-circle-shape");
   shape.AddToClassList("ps-rite-circle-accent");
   shape.tintColor=Color.Lerp(Color.white,conduitTint,.35f);
   layers.Add(shape);
  }

  // Inner rune band, colored by conduit
  var innerTex=PackspireResources.Load<Texture2D>("Art/Rite/rite-inner-spin-v1");
  if(innerTex!=null){
   var inner=new Image{image=innerTex,scaleMode=ScaleMode.ScaleToFit,pickingMode=PickingMode.Ignore};
   inner.AddToClassList("ps-rite-circle-inner-spin");
   inner.tintColor=conduitTint;
   layers.Add(inner);
  }

  var spinTex=PackspireResources.Load<Texture2D>("Art/Rite/rite-circle-spin-v1");
  if(spinTex!=null){
   var outerSpin=new Image{image=spinTex,scaleMode=ScaleMode.ScaleToFit,pickingMode=PickingMode.Ignore};
   outerSpin.AddToClassList("ps-rite-circle-spin-art");
   outerSpin.tintColor=conduitTint;
   layers.Add(outerSpin);
  }

  // Floating motes ← 共鳴式
  layers.Add(BuildResonanceFloaters(resonanceId,conduitTint));

  // Center crest ← 安定式
  var crestTex=PackspireResources.Load<Texture2D>("Art/Rite/rite-crest-"+stabilityId+"-v1");
  if(crestTex!=null){
   var crest=new Image{image=crestTex,scaleMode=ScaleMode.ScaleToFit,pickingMode=PickingMode.Ignore};
   crest.AddToClassList("ps-rite-circle-crest");
   crest.AddToClassList("ps-rite-stability-"+stabilityId);
   crest.tintColor=Color.Lerp(Color.white,conduitTint,.2f);
   layers.Add(crest);
  }

  var coreLabel=new Label(formula.core.name){pickingMode=PickingMode.Ignore};
  coreLabel.AddToClassList("ps-rite-circle-label");
  layers.Add(coreLabel);
  return layers;
 }

 VisualElement BuildResonanceFloaters(string resonanceId,Color tint){
  var root=Container("ps-rite-floats");
  root.pickingMode=PickingMode.Ignore;
  root.AddToClassList("ps-rite-resonance-"+resonanceId);
  int count=resonanceId switch{
   "silent"=>5,
   "classic"=>16,
   _=>10,
  };
  for(int i=0;i<count;i++){
   bool rune=resonanceId!="silent"&&i%4==0;
   var mote=Container(rune?"ps-rite-float ps-rite-float-rune":"ps-rite-float");
   mote.pickingMode=PickingMode.Ignore;
   mote.userData=i;
   var col=tint;
   col.a=rune?.9f:.75f;
   mote.style.backgroundColor=new StyleColor(col);
   root.Add(mote);
  }
  return root;
 }

 // 属性導線 → 色
 Color ConduitTint(string conduitId)=>conduitId switch{
  "mute"=>new Color(.72f,.78f,.86f,1f),
  "classic"=>new Color(1f,.92f,.72f,1f),
  _=>new Color(1f,.94f,.82f,1f),
 };

 Color ConduitGlow(string conduitId)=>conduitId switch{
  "mute"=>new Color(.35f,.45f,.55f,.22f),
  "classic"=>new Color(.55f,.38f,.12f,.22f),
  _=>new Color(.45f,.32f,.18f,.2f),
 };

 // 収納核 → 形の動き
 float CoreSpinSpeed(string coreId)=>coreId switch{
  "merchant"=>9f,
  "arcane"=>14f,
  "coffin"=>5f,
  "living"=>7f,
  "standard"=>8f,
  _=>8f,
 };

 float CoreBreathSpeed(string coreId)=>coreId switch{
  "arcane"=>2.8f,
  "coffin"=>1.4f,
  "living"=>2.1f,
  "merchant"=>1.9f,
  _=>1.8f,
 };

 void StartPackingCirclePulse(VisualElement kiln){
  if(kiln==null)return;
  kiln.schedule.Execute(()=>{
   if(kiln.panel==null)return;
   float t=Time.realtimeSinceStartup;
   var layersRoot=kiln.Q(className:"ps-rite-circle-layers")??kiln;
   var fx=layersRoot.userData as RiteCircleFx;
   string coreId=fx?.coreId??StorageFormulaCatalog.DefaultCoreId;
   string resonanceId=fx?.resonanceId??StorageFormulaCatalog.DefaultResonanceId;
   string stabilityId=fx?.stabilityId??StorageFormulaCatalog.DefaultStabilityId;
   float spinSpeed=CoreSpinSpeed(coreId);
   float breathSpeed=CoreBreathSpeed(coreId);

   var glow=kiln.Q(className:"ps-rite-circle-glow");
   if(glow!=null)glow.style.opacity=.4f+.4f*(.5f+.5f*Mathf.Sin(t*breathSpeed));
   var art=kiln.Q(className:"ps-rite-circle-art");
   if(art!=null)art.style.opacity=.86f+.14f*(.5f+.5f*Mathf.Sin(t*1.05f));

   // Shape (core)
   var shape=kiln.Q(className:"ps-rite-circle-shape")??kiln.Q(className:"ps-rite-circle-accent");
   if(shape!=null){
    shape.style.opacity=.6f+.35f*(.5f+.5f*Mathf.Sin(t*breathSpeed+.7f));
    shape.style.rotate=new Rotate(Angle.Degrees(Mathf.Sin(t*.35f)*3f));
    float aScale=1f+.02f*Mathf.Sin(t*breathSpeed);
    shape.style.scale=new Scale(new Vector2(aScale,aScale));
   }

   var inner=kiln.Q(className:"ps-rite-circle-inner-spin");
   if(inner!=null){
    inner.style.rotate=new Rotate(Angle.Degrees(t*spinSpeed));
    inner.style.opacity=.5f+.4f*(.5f+.5f*Mathf.Sin(t*2.1f));
   }
   var spin=kiln.Q(className:"ps-rite-circle-spin-art");
   if(spin!=null){
    spin.style.rotate=new Rotate(Angle.Degrees(-t*(spinSpeed*.65f)));
    spin.style.opacity=.4f+.35f*(.5f+.5f*Mathf.Sin(t*1.7f));
   }

   // Crest (stability)
   var crest=kiln.Q(className:"ps-rite-circle-crest");
   if(crest!=null){
    if(stabilityId=="volatile"){
     float shake=Mathf.Sin(t*18f)*1.8f+Mathf.Sin(t*11f)*1.1f;
     crest.style.translate=new Translate(Length.Pixels(shake),Length.Pixels(Mathf.Cos(t*15f)*1.4f));
     crest.style.opacity=.55f+.45f*Mathf.Abs(Mathf.Sin(t*4.2f));
     float cScale=1f+.06f*Mathf.Sin(t*5f);
     crest.style.scale=new Scale(new Vector2(cScale,cScale));
     crest.style.rotate=new Rotate(Angle.Degrees(Mathf.Sin(t*3f)*6f));
    } else {
     crest.style.translate=new Translate(0,Length.Pixels(Mathf.Sin(t*1.4f)*1.5f));
     crest.style.opacity=.72f+.2f*(.5f+.5f*Mathf.Sin(t*1.6f));
     float cScale=1f+.025f*Mathf.Sin(t*1.3f);
     crest.style.scale=new Scale(new Vector2(cScale,cScale));
     crest.style.rotate=new Rotate(Angle.Degrees(t*4f));
    }
   }

   // Floaters (resonance)
   float floatSpeed=resonanceId=="silent"?12f:38f;
   float floatPulse=resonanceId=="silent"?1.4f:3.2f;
   float baseRadius=resonanceId=="silent"?44f:42f;
   int i=0;
   foreach(var mote in kiln.Query(className:"ps-rite-float").ToList()){
    float phase=i*0.62f;
    float a=resonanceId=="silent"
     ?.08f+.22f*Mathf.Max(0f,Mathf.Sin(t*floatPulse+phase))
     :.12f+.88f*Mathf.Max(0f,Mathf.Sin(t*floatPulse+phase));
    mote.style.opacity=a;
    float ang=(t*floatSpeed+i*(resonanceId=="silent"?48f:22.5f))*Mathf.Deg2Rad;
    float radius=baseRadius+(i%3)*4f+5f*Mathf.Sin(t*1.5f+phase);
    if(mote.ClassListContains("ps-rite-float-rune"))radius+=4f;
    mote.style.left=Length.Percent(50f+Mathf.Cos(ang)*radius*.5f);
    mote.style.top=Length.Percent(50f+Mathf.Sin(ang)*radius*.46f);
    mote.style.rotate=new Rotate(Angle.Degrees(t*40f+i*20f));
    i++;
   }

   // Legacy sparks (if any remain)
   int si=0;
   foreach(var spark in kiln.Query(className:"ps-rite-spark").ToList()){
    float phase=si*0.55f;
    spark.style.opacity=.05f+.9f*Mathf.Max(0f,Mathf.Sin(t*3.1f+phase));
    float ang=(t*(28f+spinSpeed)+si*26f)*Mathf.Deg2Rad;
    float radius=40f+(si%3)*3.5f+4f*Mathf.Sin(t*1.7f+phase);
    spark.style.left=Length.Percent(50f+Mathf.Cos(ang)*radius*.48f);
    spark.style.top=Length.Percent(50f+Mathf.Sin(ang)*radius*.45f);
    si++;
   }

   int oi=0;
   foreach(var orb in kiln.Query(className:"ps-rite-orb-live").ToList()){
    float phase=oi*0.37f;
    float breath=.5f+.5f*Mathf.Sin(t*2.4f+phase);
    float scale=orb.ClassListContains("ps-rite-orb-match")
     ?1.02f+.08f*breath
     :orb.ClassListContains("ps-rite-orb-miss")
      ?.88f+.03f*breath
      :1f+.07f*breath;
    orb.style.scale=new Scale(new Vector2(scale,scale));
    float bob=Mathf.Sin(t*2.8f+phase)*2.2f;
    orb.style.translate=new Translate(0,Length.Pixels(bob));
    float opacity=orb.ClassListContains("ps-rite-orb-miss")
     ?.28f+.08f*breath
     :.85f+.15f*breath;
    orb.style.opacity=opacity;
    var wrap=orb.parent;
    var halo=wrap?.Q(className:"ps-rite-orb-halo");
    if(halo!=null){
     float hScale=1.05f+.12f*breath;
     halo.style.scale=new Scale(new Vector2(hScale,hScale));
     halo.style.opacity=orb.ClassListContains("ps-rite-orb-miss")?.1f:.22f+.28f*breath;
     halo.style.translate=new Translate(0,Length.Pixels(bob));
    }
    oi++;
   }
  }).Every(33);
 }

 Texture2D RiteOrbTexture(Element element){
  string key=element switch{
   Element.Fire=>"fire",
   Element.Water=>"water",
   Element.Wind=>"wind",
   Element.Earth=>"earth",
   _=>null,
  };
  if(key==null)return null;
  return PackspireResources.LoadFirst<Texture2D>(
   "Art/Rite/orb-"+key+"-v2",
   "Art/Rite/orb-"+key+"-v1");
 }

 VisualElement BuildRiteOrb(Element element,string extraClass=""){
  var wrap=Container("ps-rite-orb-wrap");
  wrap.pickingMode=PickingMode.Ignore;
  var halo=Container("ps-rite-orb-halo");
  halo.pickingMode=PickingMode.Ignore;
  halo.AddToClassList("ps-element-"+element.ToString().ToLowerInvariant());
  wrap.Add(halo);
  var tex=RiteOrbTexture(element);
  VisualElement orb;
  if(tex!=null){
   orb=new Image{image=tex,scaleMode=ScaleMode.ScaleToFit,pickingMode=PickingMode.Ignore};
  } else {
   orb=Container("ps-rite-orb-fallback");
   orb.pickingMode=PickingMode.Ignore;
   orb.AddToClassList("ps-element-"+element.ToString().ToLowerInvariant());
  }
  orb.AddToClassList("ps-rite-orb");
  orb.AddToClassList("ps-rite-orb-live");
  orb.AddToClassList("ps-element-"+element.ToString().ToLowerInvariant());
  if(!string.IsNullOrEmpty(extraClass))orb.AddToClassList(extraClass);
  wrap.Add(orb);
  return wrap;
 }
}
}
