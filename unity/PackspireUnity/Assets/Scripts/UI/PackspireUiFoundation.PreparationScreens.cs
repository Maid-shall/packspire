using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire {
	// COMPILE_FIX:2 // COMPILE_FIX_20260718
public sealed partial class PackspireUiFoundation {
 void BuildExpedition(){
  var meta=game.UiMeta;int unlocked=Mathf.Clamp(meta.dungeonsUnlocked,1,GameCatalog.Dungeons.Length);if(string.IsNullOrEmpty(selectedDungeonId)||!GameCatalog.Dungeons.Take(unlocked).Any(x=>x.id==selectedDungeonId))selectedDungeonId=GameCatalog.Dungeons[0].id;
  var desk=TabletopDesk("ps-expedition-workspace");screenRoot.Add(desk);var tray=Container("ps-expedition-case");desk.Add(tray);var selected=GameCatalog.Dungeons.First(x=>x.id==selectedDungeonId);
  var character=CharacterCatalog.Get(meta.selectedCharacterId);
  tray.Add(PackspireUiFactory.Title("遠征準備"));
  tray.Add(PackspireUiFactory.Body($"{character.name}　{character.title}"));
  tray.Add(PackspireUiFactory.Body($"特性 {character.traitName}：{character.traitText}"));
  tray.Add(PackspireUiFactory.Body($"スキル {character.activeSkillName}：{character.activeSkillText}"));
  tray.Add(Atlas(game.UiDungeonArt,DungeonUv(selected.id),"ps-expedition-selected-art"));tray.Add(PackspireUiFactory.Title(selected.name));tray.Add(PackspireUiFactory.Body(selected.description));tray.Add(PackspireUiFactory.Body($"敵HP ×{selected.hpScale:0.00}　追加攻撃 {selected.damage}　報酬 ×{selected.goldScale:0.00}"));
  tray.Add(PackspireUiFactory.Title("使用する荷造り"));var loadouts=Container("ps-loadout-tabs");foreach(var loadout in meta.loadouts){var entry=loadout;var button=PackspireUiFactory.Button(entry.name,()=>{game.UiSelectLoadout(entry.id);BuildExpeditionAgain();});if(entry.id==meta.selectedLoadoutId)button.AddToClassList("ps-selected");loadouts.Add(button);}tray.Add(loadouts);var active=LoadoutSystem.Active(meta);tray.Add(PackspireUiFactory.Body($"{active.name}　配置 {active.slots.Count}　カード {active.deck.Count}"));var launch=PackspireUiFactory.Button("この地図へ遠征する",()=>game.UiStartExpedition(selected.id));launch.AddToClassList("ps-primary-action");tray.Add(launch);
  var rack=new ScrollView();rack.AddToClassList("ps-map-roll-rack");desk.Add(rack);for(int i=0;i<GameCatalog.Dungeons.Length;i++){var dungeon=GameCatalog.Dungeons[i];bool available=i<unlocked;var roll=new Button(()=>{if(available){selectedDungeonId=dungeon.id;BuildExpeditionAgain();}});roll.AddToClassList("ps-map-roll");if(dungeon.id==selectedDungeonId)roll.AddToClassList("ps-selected");if(!available)roll.AddToClassList("ps-locked");roll.Add(Atlas(game.UiDungeonArt,DungeonUv(dungeon.id),"ps-map-roll-seal"));roll.Add(new Label(available?dungeon.name:"封印された地図"));rack.Add(roll);}desk.Add(TabletopBack());
 }
 void BuildExpeditionAgain(){screenRoot.Clear();BuildExpedition();}

 void BuildPacking(){
  var run=game.UiRun;
  if(run==null){game.UiNavigate(ScreenId.Hub);return;}
  if(!string.IsNullOrEmpty(selectedPackingUid)&&!run.inventory.Any(x=>x.uid==selectedPackingUid))selectedPackingUid="";
  packingDragUid="";
  packingDragging=false;
  packingTapWasSelected=false;
  packingDragFromList=false;
  packingDragGhost=null;

  var formula=BackpackSystem.Formula(run);
  packingRotation=StorageFormulaSystem.ClampRotation(formula.core.rotation,packingRotation);
  var build=BackpackSystem.Build(run);

  var root=Container("ps-rite");
  root.pickingMode=PickingMode.Position;
  packingRootElement=root;
  screenRoot.Add(root);
  RegisterPackingDrag(root);

  var top=Container("ps-rite-top");
  DressRiteFrame(top);
  var brand=Container("ps-rite-brand");
  var brandMark=Container("ps-rite-brand-mark");
  brandMark.pickingMode=PickingMode.Ignore;
  brand.Add(brandMark);
  var topTitle=Container("ps-rite-top-title");
  var topEyebrow=new Label("ATELIER  /  FORGE"){pickingMode=PickingMode.Ignore};
  topEyebrow.AddToClassList("ps-rite-top-eyebrow");
  topTitle.Add(topEyebrow);
  var topName=new Label("収納術式"){pickingMode=PickingMode.Ignore};
  topName.AddToClassList("ps-rite-top-name");
  topTitle.Add(topName);
  var topSub=new Label("鍛冶場の窯で術装を編む"){pickingMode=PickingMode.Ignore};
  topSub.AddToClassList("ps-rite-top-sub");
  topTitle.Add(topSub);
  brand.Add(topTitle);
  top.Add(brand);
  var topActions=Container("ps-rite-top-actions");
  if(game.UiPackingAtBase){
   var back=PackspireUiFactory.Button("戻る",()=>{
    game.UiPackingCapture();
    packingTemplateCommitted=false;
    game.UiNavigate(ScreenId.Hub);
   });
   back.AddToClassList("ps-rite-chip");
   topActions.Add(back);
  }
  top.Add(topActions);
  root.Add(top);

  var body=Container("ps-rite-body");
  root.Add(body);

  // Left: floating equip tray (header + filters pinned above scroll)
  var left=Container("ps-rite-left");
  DressRiteFrame(left);
  var leftHeader=Container("ps-rite-left-header");
  leftHeader.Add(RiteSectionHead("01","術装"));
  leftHeader.Add(BuildPackingFilterRow());
  left.Add(leftHeader);
  var listScroll=new ScrollView(ScrollViewMode.Vertical);
  listScroll.AddToClassList("ps-rite-equip-scroll");
  listScroll.verticalScrollerVisibility=ScrollerVisibility.Auto;
  listScroll.scrollOffset=new Vector2(0,packingEquipScrollY);
  var grid=Container("ps-rite-equip-grid");
  int cols=Screen.width>=1600?5:Screen.width>=1280?4:3;
  float tilePct=(100f/cols)-1.8f;
  foreach(var item in run.inventory){
   StorageFormulaSystem.EnsureItemRolled(item);
   var def=GameCatalog.Items[item.templateId];
   if(!PackingFilterMatch(def.type))continue;
   var entry=item;
   bool placed=run.placements.Any(x=>x.itemUid==entry.uid);
   bool selectedRow=entry.uid==selectedPackingUid;
   var tile=new VisualElement();
   tile.AddToClassList("ps-rite-equip-tile");
   tile.style.width=Length.Percent(tilePct);
   tile.focusable=true;
   tile.pickingMode=PickingMode.Position;
   tile.tooltip=def.name;
   if(selectedRow)tile.AddToClassList("ps-selected");
   if(placed){
    tile.AddToClassList("ps-rite-equip-placed");
    var badge=new Label("装着"){pickingMode=PickingMode.Ignore};
    badge.AddToClassList("ps-rite-equip-badge");
    tile.Add(badge);
   }
   tile.Add(Atlas(game.UiEquipmentArt,ItemUv(entry.templateId),"ps-rite-equip-art"));
   var name=new Label(def.name){pickingMode=PickingMode.Ignore};
   name.AddToClassList("ps-rite-equip-name");
   tile.Add(name);
   BindPackingDragSource(tile,entry.uid,formula,true);
   grid.Add(tile);
  }
  if(grid.childCount==0)listScroll.Add(RiteEmptyNote(run.inventory.Count==0?"術装がありません":"この分類にはありません"));
  else listScroll.Add(grid);
  left.Add(listScroll);
  body.Add(left);

  // Center: ritual kiln (no admin box)
  var center=Container("ps-rite-center");
  var kilnRow=Container("ps-rite-kiln-row");
  var kiln=Container("ps-rite-kiln");
  kiln.AddToClassList("ps-rite-core-"+formula.core.id);
  kiln.Add(BuildMagicCircleLayers(formula));
  var circle=Container("ps-rite-circle");
  circle.pickingMode=PickingMode.Position;
  circle.RegisterCallback<ClickEvent>(OnPackingCircleClick);
  circle.Add(BuildRiteGrid(run,formula));
  kiln.Add(circle);
  if(!string.IsNullOrEmpty(selectedPackingUid))
   kiln.Add(BuildPackingSelectDock(run,formula));
  kilnRow.Add(kiln);
  center.Add(kilnRow);

  var kilnRail=Container("ps-rite-kiln-rail");
  DressRiteFrame(kilnRail);
  kilnRail.Add(BuildPackingColorCounters(build));
  var railSpacer=Container("ps-rite-rail-spacer");
  kilnRail.Add(railSpacer);
  var cardsBtn=PackspireUiFactory.Button($"カード採用  {run.selectedCardSlots.Count}",()=>{packingFormulaOpen=false;packingCardsOpen=true;BuildPackingAgain();});
  cardsBtn.AddToClassList("ps-rite-tool");
  cardsBtn.AddToClassList("ps-rite-tool-primary");
  kilnRail.Add(cardsBtn);
  center.Add(kilnRail);
  StartPackingCirclePulse(center);
  body.Add(center);

  var rightShell=Container("ps-rite-right-shell");
  DressRiteFrame(rightShell);
  var right=new ScrollView(ScrollViewMode.Vertical);
  right.AddToClassList("ps-rite-right");
  right.scrollOffset=new Vector2(0,packingRightScrollY);
  if(!packingTemplateCommitted){
   BuildFormulaTemplateBrowser(right);
  } else {
   right.Add(BuildFormulaTemplateCommitted(formula));
   var selected=run.inventory.FirstOrDefault(x=>x.uid==selectedPackingUid);
   if(selected!=null)BuildPackingItemDetail(right,selected,build);
   else BuildPackingOverview(right,run,build);
  }
  rightShell.Add(right);
  body.Add(rightShell);

  if(packingFormulaOpen)root.Add(BuildFormulaPopup(run,formula));
  if(packingCardsOpen)root.Add(BuildCardsPopup(run,build));

  RestorePackingScroll(listScroll,right);
 }

 VisualElement BuildPackingCharacterLayers(){
  var strip=Container("ps-rite-character");
  strip.pickingMode=PickingMode.Ignore;
  (string path,string cls)[] layers={
   ("Art/HubRig/Character/character-legs","ps-rite-character-layer ps-rite-char-far"),
   ("Art/HubRig/Character/character-torso","ps-rite-character-layer ps-rite-char-mid"),
   ("Art/HubRig/Character/character-back-hair","ps-rite-character-layer ps-rite-char-mid"),
   ("Art/HubRig/Character/character-face","ps-rite-character-layer ps-rite-char-near"),
   ("Art/HubRig/Character/character-eyes","ps-rite-character-layer ps-rite-char-near"),
   ("Art/HubRig/Character/character-front-hair","ps-rite-character-layer ps-rite-char-near"),
   ("Art/HubRig/Character/character-ahoge","ps-rite-character-layer ps-rite-char-front"),
   ("Art/HubRig/Character/character-arm-left","ps-rite-character-layer ps-rite-char-near"),
   ("Art/HubRig/Character/character-arm-right","ps-rite-character-layer ps-rite-char-front"),
   ("Art/HubRig/Character/character-cloth","ps-rite-character-layer ps-rite-char-front"),
  };
  foreach(var layer in layers){
   var tex=Resources.Load<Texture2D>(layer.path);
   if(tex==null)continue;
   var image=new Image{image=tex,uv=new Rect(0,0,1,1),scaleMode=ScaleMode.ScaleToFit,pickingMode=PickingMode.Ignore};
   foreach(var cls in layer.cls.Split(' '))if(!string.IsNullOrEmpty(cls))image.AddToClassList(cls);
   strip.Add(image);
  }
  return strip;
 }

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
  var baseTex=Resources.Load<Texture2D>("Art/Rite/rite-circle-base-v1");
  if(baseTex!=null){
   var art=new Image{image=baseTex,scaleMode=ScaleMode.ScaleToFit,pickingMode=PickingMode.Ignore};
   art.AddToClassList("ps-rite-circle-art");
   art.tintColor=conduitTint;
   layers.Add(art);
  }

  // Shape accent ← 収納核（形）
  var shapeTex=Resources.Load<Texture2D>("Art/Rite/rite-accent-"+coreId+"-v1");
  if(shapeTex!=null){
   var shape=new Image{image=shapeTex,scaleMode=ScaleMode.ScaleToFit,pickingMode=PickingMode.Ignore};
   shape.AddToClassList("ps-rite-circle-shape");
   shape.AddToClassList("ps-rite-circle-accent");
   shape.tintColor=Color.Lerp(Color.white,conduitTint,.35f);
   layers.Add(shape);
  }

  // Inner rune band, colored by conduit
  var innerTex=Resources.Load<Texture2D>("Art/Rite/rite-inner-spin-v1");
  if(innerTex!=null){
   var inner=new Image{image=innerTex,scaleMode=ScaleMode.ScaleToFit,pickingMode=PickingMode.Ignore};
   inner.AddToClassList("ps-rite-circle-inner-spin");
   inner.tintColor=conduitTint;
   layers.Add(inner);
  }

  var spinTex=Resources.Load<Texture2D>("Art/Rite/rite-circle-spin-v1");
  if(spinTex!=null){
   var outerSpin=new Image{image=spinTex,scaleMode=ScaleMode.ScaleToFit,pickingMode=PickingMode.Ignore};
   outerSpin.AddToClassList("ps-rite-circle-spin-art");
   outerSpin.tintColor=conduitTint;
   layers.Add(outerSpin);
  }

  // Floating motes ← 共鳴式
  layers.Add(BuildResonanceFloaters(resonanceId,conduitTint));

  // Center crest ← 安定式
  var crestTex=Resources.Load<Texture2D>("Art/Rite/rite-crest-"+stabilityId+"-v1");
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
  return Resources.Load<Texture2D>("Art/Rite/orb-"+key+"-v2")
   ??Resources.Load<Texture2D>("Art/Rite/orb-"+key+"-v1");
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

 VisualElement BuildPackingFilterRow(){
  var row=Container("ps-rite-filters");
  void AddFilter(string id,string label){
   var button=PackspireUiFactory.Button(label,()=>{
    if(packingEquipFilter!=id){
     packingEquipFilter=id;
     packingEquipScrollY=0;
    }
    BuildPackingAgain();
   });
   button.AddToClassList("ps-rite-filter");
   if(packingEquipFilter==id)button.AddToClassList("ps-selected");
   row.Add(button);
  }
  AddFilter("","全部");
  AddFilter("weapon","武器");
  AddFilter("armor","防具");
  AddFilter("rune","ルーン");
  AddFilter("supply","道具");
  return row;
 }

 bool PackingFilterMatch(ItemType type)=>packingEquipFilter switch{
  "weapon"=>type==ItemType.Weapon,
  "armor"=>type==ItemType.Armor,
  "rune"=>type==ItemType.Rune,
  "supply"=>type==ItemType.Supply,
  _=>true,
 };

 VisualElement BuildPackingSelectDock(RunState run,ActiveStorageFormula formula){
  var dock=Container("ps-rite-select-dock");
  dock.pickingMode=PickingMode.Position;
  DressRiteFrame(dock);
  var caption=new Label("AUX"){pickingMode=PickingMode.Ignore};
  caption.AddToClassList("ps-rite-select-caption");
  dock.Add(caption);
  var rotate=PackspireUiFactory.Button($"回転\n{packingRotation*90}°",()=>{
   int next=StorageFormulaSystem.NextRotation(formula.core.rotation,packingRotation);
   var placement=run.placements.FirstOrDefault(x=>x.itemUid==selectedPackingUid);
   if(placement!=null&&!game.UiPackingPlace(selectedPackingUid,placement.anchor,next)){
    ShowToast("そこでは回転できません");
    return;
   }
   packingRotation=next;
   BuildPackingAgain();
  });
  rotate.AddToClassList("ps-rite-select-btn");
  rotate.AddToClassList("ps-rite-select-btn-primary");
  dock.Add(rotate);
  var clear=PackspireUiFactory.Button("選択解除",()=>{selectedPackingUid="";packingRotation=0;BuildPackingAgain();});
  clear.AddToClassList("ps-rite-select-btn");
  dock.Add(clear);
  bool placed=run.placements.Any(x=>x.itemUid==selectedPackingUid);
  var remove=PackspireUiFactory.Button("外す",()=>{game.UiPackingRemove(selectedPackingUid);selectedPackingUid="";BuildPackingAgain();});
  remove.AddToClassList("ps-rite-select-btn");
  remove.AddToClassList("ps-rite-select-btn-danger");
  remove.SetEnabled(placed);
  dock.Add(remove);
  return dock;
 }

 void BuildPackingItemDetail(VisualElement right,ItemInstance selected,DeckBuildResult build){
  StorageFormulaSystem.EnsureItemRolled(selected);
  var def=GameCatalog.Items[selected.templateId];
  var run=game.UiRun;
  right.Add(RiteSectionHead("03","選択中の術装"));
  var detailCard=Container("ps-rite-detail-card");
  detailCard.Add(Atlas(game.UiEquipmentArt,ItemUv(selected.templateId),"ps-rite-detail-art"));
  var detailName=new Label(def.name){pickingMode=PickingMode.Ignore};
  detailName.AddToClassList("ps-rite-detail-name");
  detailCard.Add(detailName);
  var detailDesc=new Label(def.description){pickingMode=PickingMode.Ignore};
  detailDesc.AddToClassList("ps-rite-detail-desc");
  detailCard.Add(detailDesc);
  right.Add(detailCard);
  right.Add(RiteSectionHead("","形状"));
  right.Add(BuildShapePreview(selected,packingRotation));
  string elements=string.Join(" · ",def.cells.Select((cell,i)=>ElementLabel(selected.colors!=null&&i<selected.colors.Count?selected.colors[i]:cell.element)));
  right.Add(RiteMetaLine($"属性  {elements}"));
  AddPackingTraitLines(right,run,build,selected);
  AddPackingLinkLines(right,run,build,selected);
 }

 void BuildPackingOverview(VisualElement right,RunState run,DeckBuildResult build){
  right.Add(RiteSectionHead("03","発動効果"));
  AddPackingTraitLines(right,run,build,null);
  AddPackingLinkLines(right,run,build,null);
  if(build.stability!=null&&build.stability.runaway)
   right.Add(RiteEffectCard("安定式", "過負荷", "暴走状態です。安定式を見直してください。", true));
 }

 VisualElement BuildFormulaTemplateCard(ActiveStorageFormula formula)=>BuildFormulaTemplateCommitted(formula);

 void BuildFormulaTemplateBrowser(VisualElement right){
  var head=Container("ps-rite-template-head");
  head.Add(RiteSectionHead("02","術式テンプレート"));
  var add=PackspireUiFactory.Button("新規追加",()=>{
   game.UiPackingCapture();
   packingCardsOpen=false;
   packingFormulaSection="";
   packingTemplateCommitted=false;
   game.UiPackingCreateLoadout();
   packingFormulaOpen=true;
   selectedPackingUid="";
   BuildPackingAgain();
  });
  add.AddToClassList("ps-rite-template-edit");
  head.Add(add);
  right.Add(head);

  var meta=game.UiMeta;
  if(meta?.loadouts==null||meta.loadouts.Count==0){
   right.Add(RiteEmptyNote("テンプレートがありません\n「新規追加」から作成できます"));
   return;
  }
  foreach(var loadout in meta.loadouts){
   LoadoutSystem.EnsureFormulaIds(loadout);
   var entry=loadout;
   var preview=StorageFormulaSystem.Resolve(entry);
   var card=Container("ps-rite-template-list-card");
   if(entry.id==meta.selectedLoadoutId)card.AddToClassList("ps-selected");

   var main=Container("ps-rite-template-list-main");
   var name=new Label(string.IsNullOrEmpty(entry.name)?"無名の術式":entry.name){pickingMode=PickingMode.Ignore};
   name.AddToClassList("ps-rite-template-list-name");
   main.Add(name);
   var sub=new Label($"{preview.core.name} · {preview.conduit.name}"){pickingMode=PickingMode.Ignore};
   sub.AddToClassList("ps-rite-template-list-sub");
   main.Add(sub);
   card.Add(main);

   var actions=Container("ps-rite-template-actions");
   var decide=PackspireUiFactory.Button("決定",()=>{
    game.UiPackingCapture();
    packingTemplateCommitted=true;
    packingFormulaOpen=false;
    selectedPackingUid="";
    game.UiOpenPackingLoadout(entry.id);
    BuildPackingAgain();
   });
   decide.AddToClassList("ps-rite-template-decide");
   actions.Add(decide);
   var edit=PackspireUiFactory.Button("編集",()=>{
    game.UiPackingCapture();
    game.UiOpenPackingLoadout(entry.id);
    packingTemplateCommitted=false;
    packingFormulaSection="";
    packingFormulaOpen=true;
    selectedPackingUid="";
    BuildPackingAgain();
   });
   edit.AddToClassList("ps-rite-template-edit");
   actions.Add(edit);
   card.Add(actions);
   right.Add(card);
  }
 }

 VisualElement BuildFormulaTemplateCommitted(ActiveStorageFormula formula){
  var card=Container("ps-rite-template");
  var head=Container("ps-rite-template-head");
  head.Add(RiteSectionHead("02","使用中の術式"));
  var headActions=Container("ps-rite-template-actions");
  var change=PackspireUiFactory.Button("一覧",()=>{
   game.UiPackingCapture();
   packingTemplateCommitted=false;
   packingFormulaOpen=false;
   BuildPackingAgain();
  });
  change.AddToClassList("ps-rite-template-edit");
  headActions.Add(change);
  var edit=PackspireUiFactory.Button("編集",()=>{
   packingCardsOpen=false;
   packingFormulaSection="";
   packingFormulaOpen=true;
   BuildPackingAgain();
  });
  edit.AddToClassList("ps-rite-template-edit");
  headActions.Add(edit);
  head.Add(headActions);
  card.Add(head);

  var loadoutName=game.UiMeta?.loadouts?.FirstOrDefault(x=>x.id==game.UiMeta.selectedLoadoutId)?.name??"無名の術式";
  var name=new Label(loadoutName){pickingMode=PickingMode.Ignore};
  name.AddToClassList("ps-rite-template-list-name");
  card.Add(name);
  var sub=new Label($"{formula.core.name} · {formula.conduit.name} · {formula.resonance.name} · {formula.stability.name}"){pickingMode=PickingMode.Ignore};
  sub.AddToClassList("ps-rite-template-list-sub");
  card.Add(sub);

  var save=PackspireUiFactory.Button(game.UiPackingAtBase?"この術式を保存":"保存して進む",()=>{
   game.UiPackingSave();
   if(game.UiPackingAtBase){
    ShowToast("術式プリセットを保存しました");
    BuildPackingAgain();
   }
  });
  save.AddToClassList("ps-rite-save");
  save.AddToClassList("ps-rite-template-save");
  card.Add(save);
  return card;
 }

 void CloseFormulaPopup(bool commitTemplate){
  packingFormulaOpen=false;
  packingFormulaSection="";
  if(commitTemplate)packingTemplateCommitted=true;
  game.UiPackingCapture();
  BuildPackingAgain();
 }

 VisualElement BuildFormulaTemplateRow(string kind,string value){
  var row=Container("ps-rite-template-row");
  var kindLabel=new Label(kind){pickingMode=PickingMode.Ignore};
  kindLabel.AddToClassList("ps-rite-template-kind");
  var valueLabel=new Label(value){pickingMode=PickingMode.Ignore};
  valueLabel.AddToClassList("ps-rite-template-value");
  row.Add(kindLabel);
  row.Add(valueLabel);
  return row;
 }

 void AddPackingTraitLines(VisualElement right,RunState run,DeckBuildResult build,ItemInstance focus){
  right.Add(RiteSectionHead("","色特性"));
  var lines=0;
  IEnumerable<ItemInstance> items=focus!=null
   ?new[]{focus}
   :run.placements.Select(p=>run.inventory.FirstOrDefault(x=>x.uid==p.itemUid)).Where(x=>x!=null);
  foreach(var item in items){
   StorageFormulaSystem.EnsureItemRolled(item);
   var trait=StorageFormulaCatalog.Trait(item.traitId);
   if(trait==null){
    if(focus!=null){right.Add(RiteEmptyNote("色特性なし"));lines++;}
    continue;
   }
   int matches=0;
   build.colors.TryGetValue(trait.element,out matches);
   bool placed=run.placements.Any(x=>x.itemUid==item.uid);
   bool active=placed&&matches>=trait.requiredMatches;
   if(focus==null&&!active)continue;
   var def=GameCatalog.Items[item.templateId];
   string head=focus!=null?trait.name:$"{def.name}  /  {trait.name}";
   string state=active?"発動中":placed?$"未発動 {ElementLabel(trait.element)}{matches}/{trait.requiredMatches}":"未配置";
   right.Add(RiteEffectCard(head,state,TraitEffectLabel(trait),active));
   lines++;
  }
  if(lines==0)right.Add(RiteEmptyNote(focus!=null?"色特性なし":"発動中の色特性はありません"));
 }

 void AddPackingLinkLines(VisualElement right,RunState run,DeckBuildResult build,ItemInstance focus){
  right.Add(RiteSectionHead("","隣接 LINK"));
  var formula=build.formula.core!=null?build.formula:BackpackSystem.Formula(run);
  var links=formula.resonance.links??System.Array.Empty<ResonanceLinkDef>();
  var upgrades=formula.resonance.upgrades??System.Array.Empty<ResonanceUpgradeDef>();
  if(links.Length==0&&upgrades.Length==0){
   right.Add(RiteEmptyNote("この共鳴式には隣接LINKがありません"));
   return;
  }

  Placement focusPlacement=focus!=null?run.placements.FirstOrDefault(x=>x.itemUid==focus.uid):null;
  int lines=0;

  foreach(var link in links){
   if(focus!=null&&!LinkOwnedBy(link,focus))continue;
   bool active=focus!=null
    ?IsLinkActiveBeside(run,focus,focusPlacement,link)
    :IsLinkActiveAnywhere(run,link);
   if(focus==null&&!active)continue;
   string state=active?"発動中":"未発動";
   right.Add(RiteEffectCard(link.label,state,LinkEffectLabel(link),active));
   lines++;
  }

  foreach(var upgrade in upgrades){
   // カード変化は host（効果を受ける側）のリンクとしてだけ表示する
   if(focus!=null&&focus.templateId!=upgrade.hostTemplate)continue;
   bool active=focus!=null
    ?IsUpgradeActiveBeside(run,focus,focusPlacement,upgrade)
    :IsUpgradeActiveAnywhere(run,upgrade);
   if(focus==null&&!active)continue;
   string toName=GameCatalog.Cards.TryGetValue(upgrade.toCardId,out var card)?card.name:upgrade.toCardId;
   string hostName=GameCatalog.Items.TryGetValue(upgrade.hostTemplate,out var host)?host.name:upgrade.hostTemplate;
   string neighborName=GameCatalog.Items.TryGetValue(upgrade.neighborTemplate,out var neighbor)?neighbor.name:upgrade.neighborTemplate;
   string state=active?"発動中":"未発動";
   right.Add(RiteEffectCard($"カード変化  {hostName} × {neighborName}",state,$"→ {toName}",active));
   lines++;
  }

  if(lines==0)right.Add(RiteEmptyNote(focus!=null?"この装備が持つ隣接LINKはありません":"発動中の隣接LINKはありません"));
 }

 /// <summary>
 /// LINKの「効果持ち」だけに表示する。
 /// 固有ペア（剣×盾）は双方。templateA×種別／何でも（熾火×武器、結晶×装備）は templateA のみ。
 /// </summary>
 bool LinkOwnedBy(ResonanceLinkDef link,ItemInstance item){
  if(!string.IsNullOrEmpty(link.templateA)&&!string.IsNullOrEmpty(link.templateB))
   return item.templateId==link.templateA||item.templateId==link.templateB;
  if(!string.IsNullOrEmpty(link.templateA))
   return item.templateId==link.templateA;
  return false;
 }

 bool IsLinkActiveBeside(RunState run,ItemInstance focus,Placement placement,ResonanceLinkDef link){
  if(placement==null)return false;
  foreach(var other in run.placements.Where(x=>x.itemUid!=focus.uid&&BackpackSystem.Adjacent(run,placement,x))){
   var neighbor=run.inventory.FirstOrDefault(x=>x.uid==other.itemUid);
   if(neighbor!=null&&LinkPairMatches(link,focus,neighbor))return true;
  }
  return false;
 }

 bool IsLinkActiveAnywhere(RunState run,ResonanceLinkDef link){
  foreach(var a in run.placements)
  foreach(var b in run.placements.Where(x=>string.CompareOrdinal(x.itemUid,a.itemUid)>0&&BackpackSystem.Adjacent(run,a,x))){
   var ia=run.inventory.FirstOrDefault(x=>x.uid==a.itemUid);
   var ib=run.inventory.FirstOrDefault(x=>x.uid==b.itemUid);
   if(ia!=null&&ib!=null&&LinkPairMatches(link,ia,ib))return true;
  }
  return false;
 }

 bool IsUpgradeActiveBeside(RunState run,ItemInstance focus,Placement placement,ResonanceUpgradeDef upgrade){
  if(placement==null)return false;
  foreach(var other in run.placements.Where(x=>x.itemUid!=focus.uid&&BackpackSystem.Adjacent(run,placement,x))){
   var neighbor=run.inventory.FirstOrDefault(x=>x.uid==other.itemUid);
   if(neighbor!=null&&UpgradePairMatches(upgrade,focus.templateId,neighbor.templateId))return true;
  }
  return false;
 }

 bool IsUpgradeActiveAnywhere(RunState run,ResonanceUpgradeDef upgrade){
  foreach(var a in run.placements)
  foreach(var b in run.placements.Where(x=>string.CompareOrdinal(x.itemUid,a.itemUid)>0&&BackpackSystem.Adjacent(run,a,x))){
   var ia=run.inventory.FirstOrDefault(x=>x.uid==a.itemUid);
   var ib=run.inventory.FirstOrDefault(x=>x.uid==b.itemUid);
   if(ia!=null&&ib!=null&&UpgradePairMatches(upgrade,ia.templateId,ib.templateId))return true;
  }
  return false;
 }

 Label RiteStatusLine(string text,bool active){
  var label=new Label(text){pickingMode=PickingMode.Ignore};
  label.AddToClassList("ps-rite-status");
  if(active)label.AddToClassList("ps-rite-status-active");
  return label;
 }

 void DressRiteFrame(VisualElement panel){
  if(panel==null)return;
  panel.Add(RiteTick("ps-rite-tick-tl"));
  panel.Add(RiteTick("ps-rite-tick-tr"));
  panel.Add(RiteTick("ps-rite-tick-bl"));
  panel.Add(RiteTick("ps-rite-tick-br"));
 }

 VisualElement RiteTick(string cornerClass){
  var tick=Container("ps-rite-tick "+cornerClass);
  tick.pickingMode=PickingMode.Ignore;
  return tick;
 }

 VisualElement RiteSectionHead(string index,string title){
  var head=Container("ps-rite-panel-head");
  if(!string.IsNullOrEmpty(index)){
   var idx=new Label(index){pickingMode=PickingMode.Ignore};
   idx.AddToClassList("ps-rite-panel-index");
   head.Add(idx);
  }
  var lab=new Label(title){pickingMode=PickingMode.Ignore};
  lab.AddToClassList("ps-rite-panel-title");
  head.Add(lab);
  var rule=Container("ps-rite-panel-rule");
  rule.pickingMode=PickingMode.Ignore;
  head.Add(rule);
  return head;
 }

 VisualElement RiteEffectCard(string title,string state,string detail,bool active){
  var card=Container("ps-rite-effect");
  if(active)card.AddToClassList("ps-rite-effect-active");
  var top=Container("ps-rite-effect-top");
  var name=new Label(title){pickingMode=PickingMode.Ignore};
  name.AddToClassList("ps-rite-effect-title");
  top.Add(name);
  var badge=new Label(state){pickingMode=PickingMode.Ignore};
  badge.AddToClassList("ps-rite-effect-badge");
  if(active)badge.AddToClassList("ps-rite-effect-badge-on");
  top.Add(badge);
  card.Add(top);
  if(!string.IsNullOrEmpty(detail)){
   var body=new Label(detail){pickingMode=PickingMode.Ignore};
   body.AddToClassList("ps-rite-effect-detail");
   card.Add(body);
  }
  return card;
 }

 VisualElement RiteEmptyNote(string text){
  var label=new Label(text){pickingMode=PickingMode.Ignore};
  label.AddToClassList("ps-rite-empty");
  return label;
 }

 VisualElement RiteMetaLine(string text){
  var label=new Label(text){pickingMode=PickingMode.Ignore};
  label.AddToClassList("ps-rite-meta");
  return label;
 }

 bool LinkPairMatches(ResonanceLinkDef link,ItemInstance a,ItemInstance b){
  var pair=new[]{a.templateId,b.templateId};
  var types=new[]{GameCatalog.Items[pair[0]].type,GameCatalog.Items[pair[1]].type};
  if(!string.IsNullOrEmpty(link.templateA)&&!string.IsNullOrEmpty(link.templateB))
   return pair.Contains(link.templateA)&&pair.Contains(link.templateB);
  if(!string.IsNullOrEmpty(link.templateA)&&link.typeB.HasValue)
   return pair.Contains(link.templateA)&&(types[0]==link.typeB.Value||types[1]==link.typeB.Value);
  if(!string.IsNullOrEmpty(link.templateA))
   return pair.Contains(link.templateA);
  return false;
 }

 bool UpgradePairMatches(ResonanceUpgradeDef upgrade,string templateA,string templateB){
  var pair=new[]{templateA,templateB};
  return pair.Contains(upgrade.hostTemplate)&&pair.Contains(upgrade.neighborTemplate);
 }

 string LinkEffectLabel(ResonanceLinkDef link){
  var parts=new List<string>();
  if(link.damageBonus>0)parts.Add($"攻撃+{link.damageBonus}");
  if(link.blockBonus>0)parts.Add($"防御+{link.blockBonus}");
  if(link.costReduce>0)parts.Add($"コスト-{link.costReduce}");
  return parts.Count==0?"効果あり":string.Join("　",parts);
 }

 VisualElement BuildPackingColorCounters(DeckBuildResult build){
  var bar=Container("ps-rite-color-bar");
  bar.Add(PackingColorChip(Element.Fire,build.colors[Element.Fire]));
  bar.Add(PackingColorChip(Element.Water,build.colors[Element.Water]));
  bar.Add(PackingColorChip(Element.Wind,build.colors[Element.Wind]));
  bar.Add(PackingColorChip(Element.Earth,build.colors[Element.Earth]));
  return bar;
 }

 VisualElement PackingColorChip(Element element,int count){
  var chip=Container("ps-rite-color-chip");
  chip.AddToClassList("ps-element-"+element.ToString().ToLowerInvariant());
  var orbTex=RiteOrbTexture(element);
  if(orbTex!=null){
   var orb=new Image{image=orbTex,scaleMode=ScaleMode.ScaleToFit,pickingMode=PickingMode.Ignore};
   orb.AddToClassList("ps-rite-color-orb");
   orb.AddToClassList("ps-rite-orb-live");
   chip.Add(orb);
  } else {
   var orb=Container("ps-rite-color-orb");
   orb.pickingMode=PickingMode.Ignore;
   chip.Add(orb);
  }
  var value=new Label(count.ToString()){pickingMode=PickingMode.Ignore};
  value.AddToClassList("ps-rite-color-value");
  chip.Add(value);
  return chip;
 }

 void RegisterPackingDrag(VisualElement root){
  root.RegisterCallback<PointerMoveEvent>(OnPackingPointerMove);
  root.RegisterCallback<PointerUpEvent>(OnPackingPointerUp);
 }

 void BindPackingDragSource(VisualElement element,string uid,ActiveStorageFormula formula,bool fromEquipList=false){
  element.RegisterCallback<PointerDownEvent>(evt=>{
   if(evt.button!=0||packingFormulaOpen||packingCardsOpen||packingRootElement==null)return;
   packingDragUid=uid;
   packingDragging=false;
   packingDragFromList=fromEquipList;
   packingDragStart=evt.position;
   packingTapWasSelected=selectedPackingUid==uid;
   packingRotation=StorageFormulaSystem.ClampRotation(formula.core.rotation,game.UiRun?.placements.FirstOrDefault(x=>x.itemUid==uid)?.rotation??packingRotation);
   if(!fromEquipList){
    selectedPackingUid=uid;
    packingRootElement.CapturePointer(evt.pointerId);
    evt.StopPropagation();
   }
  });
 }

 void OnPackingPointerMove(PointerMoveEvent evt){
  if(string.IsNullOrEmpty(packingDragUid)||packingRootElement==null)return;
  if(!packingDragging){
   Vector2 delta=(Vector2)evt.position-packingDragStart;
   if(delta.magnitude<10f)return;
   if(packingDragFromList&&Mathf.Abs(delta.y)>Mathf.Abs(delta.x)*1.15f){
    packingDragUid="";
    packingDragging=false;
    packingDragFromList=false;
    return;
   }
   packingDragging=true;
   selectedPackingUid=packingDragUid;
   if(!packingRootElement.HasPointerCapture(evt.pointerId))
    packingRootElement.CapturePointer(evt.pointerId);
   EnsurePackingGhost();
  }
  if(packingDragGhost==null)return;
  var local=packingRootElement.WorldToLocal(evt.position);
  packingDragGhost.style.left=local.x-36;
  packingDragGhost.style.top=local.y-36;
 }

 void OnPackingPointerUp(PointerUpEvent evt){
  if(string.IsNullOrEmpty(packingDragUid))return;
  if(packingRootElement!=null&&packingRootElement.HasPointerCapture(evt.pointerId))
   packingRootElement.ReleasePointer(evt.pointerId);
  if(!packingDragging){
   string uid=packingDragUid;
   packingDragUid="";
   packingDragFromList=false;
   if(packingTapWasSelected){selectedPackingUid="";packingRotation=0;}
   else selectedPackingUid=uid;
   BuildPackingAgain();
   return;
  }
  EndPackingDrag(true,evt.position);
 }

 void OnPackingCircleClick(ClickEvent evt){
  if(packingFormulaOpen||packingCardsOpen||packingDragging||string.IsNullOrEmpty(selectedPackingUid))return;
  var target=evt.target as VisualElement;
  while(target!=null){
   if(target.ClassListContains("ps-rite-cell")||target.ClassListContains("ps-rite-grid"))return;
   if(target.ClassListContains("ps-rite-circle"))break;
   target=target.parent;
  }
  selectedPackingUid="";
  packingRotation=0;
  BuildPackingAgain();
 }

 void EndPackingDrag(bool applyDrop,Vector2 panelPosition=default){
  string uid=packingDragUid;
  bool wasDragging=packingDragging;
  packingDragUid="";
  packingDragging=false;
  packingDragFromList=false;
  if(packingDragGhost!=null){
   packingDragGhost.RemoveFromHierarchy();
   packingDragGhost=null;
  }
  if(!applyDrop||!wasDragging||string.IsNullOrEmpty(uid)||game.UiRun==null){
   if(wasDragging)BuildPackingAgain();
   return;
  }

  int cell=FindPackingCellAt(panelPosition);
  if(cell>=0){
   if(!game.UiPackingPlace(uid,cell,packingRotation))ShowToast("そこには置けません");
   selectedPackingUid=uid;
  } else {
   game.UiPackingRemove(uid);
   selectedPackingUid="";
   ShowToast("装備欄へ戻した");
  }
  BuildPackingAgain();
 }

 void EnsurePackingGhost(){
  if(packingRootElement==null||string.IsNullOrEmpty(packingDragUid)||game.UiRun==null)return;
  var item=game.UiRun.inventory.FirstOrDefault(x=>x.uid==packingDragUid);
  if(item==null)return;
  packingDragGhost=Container("ps-rite-drag-ghost");
  packingDragGhost.pickingMode=PickingMode.Ignore;
  packingDragGhost.Add(Atlas(game.UiEquipmentArt,ItemUv(item.templateId),"ps-rite-drag-ghost-art"));
  packingRootElement.Add(packingDragGhost);
 }

 int FindPackingCellAt(Vector2 panelPosition){
  if(packingGridElement==null)return -1;
  if(!packingGridElement.worldBound.Contains(panelPosition))return -1;
  int best=-1;
  float bestDist=float.MaxValue;
  foreach(var child in packingGridElement.Children()){
   if(child.userData is not int index)continue;
   if(child.worldBound.Contains(panelPosition))return index;
   Vector2 center=child.worldBound.center;
   float dist=(center-panelPosition).sqrMagnitude;
   if(dist<bestDist){bestDist=dist;best=index;}
  }
  return best;
 }

 VisualElement BuildFormulaPopup(RunState run,ActiveStorageFormula formula){
  var overlay=Container("ps-rite-popup-overlay");
  overlay.pickingMode=PickingMode.Position;
  overlay.RegisterCallback<ClickEvent>(evt=>{
   if(evt.target!=overlay)return;
   CloseFormulaPopup(true);
  });

  var panel=Container("ps-rite-popup");
  panel.AddToClassList("ps-rite-popup-formula");
  panel.pickingMode=PickingMode.Position;
  panel.RegisterCallback<ClickEvent>(evt=>evt.StopPropagation());

  DressRiteFrame(panel);
  var header=Container("ps-rite-popup-header");
  var headerTitle=Container("ps-rite-popup-title-block");
  var headerEye=new Label("FORMULA  /  PRESET"){pickingMode=PickingMode.Ignore};
  headerEye.AddToClassList("ps-rite-top-eyebrow");
  headerTitle.Add(headerEye);
  var headerName=new Label("魔法術式"){pickingMode=PickingMode.Ignore};
  headerName.AddToClassList("ps-rite-top-name");
  headerTitle.Add(headerName);
  header.Add(headerTitle);
  var close=PackspireUiFactory.Button("閉じる",()=>CloseFormulaPopup(true));
  close.AddToClassList("ps-rite-chip");
  header.Add(close);
  panel.Add(header);

  var nameRow=Container("ps-rite-formula-name");
  var nameLabel=new Label("術式名"){pickingMode=PickingMode.Ignore};
  nameLabel.AddToClassList("ps-rite-formula-name-label");
  nameRow.Add(nameLabel);
  var currentName=game.UiMeta?.loadouts?.FirstOrDefault(x=>x.id==game.UiMeta.selectedLoadoutId)?.name??"新規術式";
  var nameField=new TextField{value=currentName};
  nameField.AddToClassList("ps-rite-formula-name-field");
  nameField.RegisterValueChangedCallback(evt=>game.UiPackingRenameLoadout(evt.newValue));
  nameRow.Add(nameField);
  panel.Add(nameRow);

  var body=Container("ps-rite-formula-body");
  var rail=Container("ps-rite-formula-rail");
  rail.RegisterCallback<ClickEvent>(evt=>evt.StopPropagation());
  rail.Add(BuildFormulaAccordion(
   "core","収納核",formula.core.name,formula.core.description,
   StorageFormulaCatalog.Cores.Values.Select(x=>(x.id,x.name,x.description)),
   id=>{game.UiPackingSetCore(id);BuildPackingAgain();},
   formula.core.id));
  rail.Add(BuildFormulaAccordion(
   "conduit","属性導線",formula.conduit.name,formula.conduit.description,
   StorageFormulaCatalog.Conduits.Values.Select(x=>(x.id,x.name,x.description)),
   id=>{game.UiPackingSetConduit(id);BuildPackingAgain();},
   formula.conduit.id));
  rail.Add(BuildFormulaAccordion(
   "resonance","共鳴式",formula.resonance.name,formula.resonance.description,
   StorageFormulaCatalog.Resonances.Values.Select(x=>(x.id,x.name,x.description)),
   id=>{game.UiPackingSetResonance(id);BuildPackingAgain();},
   formula.resonance.id));
  rail.Add(BuildFormulaAccordion(
   "stability","安定式",formula.stability.name,formula.stability.description,
   StorageFormulaCatalog.Stabilities.Values.Select(x=>(x.id,x.name,x.description)),
   id=>{game.UiPackingSetStability(id);BuildPackingAgain();},
   formula.stability.id));
  body.Add(rail);

  var stage=Container("ps-rite-formula-stage");
  stage.pickingMode=PickingMode.Position;
  stage.RegisterCallback<ClickEvent>(evt=>{
   evt.StopPropagation();
   if(string.IsNullOrEmpty(packingFormulaSection))return;
   packingFormulaSection="";
   BuildPackingAgain();
  });
  var preview=BuildMagicCircleLayers(formula);
  preview.AddToClassList("ps-rite-circle-preview");
  preview.pickingMode=PickingMode.Ignore;
  stage.Add(preview);
  StartPackingCirclePulse(stage);
  var stageHint=new Label("核＝形　導線＝色　共鳴＝浮遊　安定＝紋章"){pickingMode=PickingMode.Ignore};
  stageHint.AddToClassList("ps-rite-formula-stage-hint");
  stage.Add(stageHint);
  body.Add(stage);

  panel.Add(body);
  overlay.Add(panel);
  return overlay;
 }

 VisualElement BuildFormulaAccordion(
  string sectionId,
  string sectionLabel,
  string selectedName,
  string selectedDesc,
  System.Collections.Generic.IEnumerable<(string id,string name,string description)> options,
  System.Action<string> onPick,
  string selectedId){
  bool open=packingFormulaSection==sectionId;
  var block=Container("ps-rite-formula-acc");
  if(open)block.AddToClassList("ps-rite-formula-acc-open");

  var head=new Button(()=>{
   packingFormulaSection=open?"":sectionId;
   BuildPackingAgain();
  });
  head.AddToClassList("ps-rite-formula-acc-head");
  var mark=new Label(open?"▾":"▸"){pickingMode=PickingMode.Ignore};
  mark.AddToClassList("ps-rite-formula-acc-mark");
  var kind=new Label(sectionLabel){pickingMode=PickingMode.Ignore};
  kind.AddToClassList("ps-rite-formula-acc-kind");
  var chosen=new Label(selectedName){pickingMode=PickingMode.Ignore};
  chosen.AddToClassList("ps-rite-formula-acc-chosen");
  head.Add(mark);
  head.Add(kind);
  head.Add(chosen);
  block.Add(head);

  if(open){
   var body=Container("ps-rite-formula-acc-body");
   if(!string.IsNullOrEmpty(selectedDesc)){
    var desc=new Label(selectedDesc){pickingMode=PickingMode.Ignore};
    desc.AddToClassList("ps-rite-formula-acc-desc");
    body.Add(desc);
   }
   foreach(var option in options){
    var entry=option;
    var button=PackspireUiFactory.Button(entry.name,()=>{
     packingFormulaSection=sectionId;
     onPick(entry.id);
    });
    button.AddToClassList("ps-rite-formula-acc-option");
    button.tooltip=entry.description;
    if(entry.id==selectedId)button.AddToClassList("ps-selected");
    body.Add(button);
   }
   block.Add(body);
  }
  return block;
 }

 VisualElement BuildCardsPopup(RunState run,DeckBuildResult build){
  var overlay=Container("ps-rite-popup-overlay");
  overlay.pickingMode=PickingMode.Position;
  overlay.RegisterCallback<ClickEvent>(evt=>{
   if(evt.target==overlay){packingCardsOpen=false;BuildPackingAgain();}
  });

  var panel=Container("ps-rite-popup");
  panel.AddToClassList("ps-rite-popup-cards");
  panel.pickingMode=PickingMode.Position;
  panel.RegisterCallback<ClickEvent>(evt=>evt.StopPropagation());
  DressRiteFrame(panel);

  var header=Container("ps-rite-popup-header");
  var headerTitle=Container("ps-rite-popup-title-block");
  var headerEye=new Label("DECK  /  ADOPT"){pickingMode=PickingMode.Ignore};
  headerEye.AddToClassList("ps-rite-top-eyebrow");
  headerTitle.Add(headerEye);
  var headerName=new Label($"カード採用  {run.selectedCardSlots.Count}/{build.candidates.Count}"){pickingMode=PickingMode.Ignore};
  headerName.AddToClassList("ps-rite-top-name");
  headerTitle.Add(headerName);
  header.Add(headerTitle);
  var close=PackspireUiFactory.Button("閉じる",()=>{packingCardsOpen=false;BuildPackingAgain();});
  close.AddToClassList("ps-rite-chip");
  header.Add(close);
  panel.Add(header);
  panel.Add(RiteMetaLine("配置した装備から出た候補をデッキへ入れます。上限はありません。"));

  var actions=Container("ps-rite-formula-row");
  var all=PackspireUiFactory.Button("すべて採用",()=>{
   foreach(var card in build.candidates)if(!run.selectedCardSlots.Contains(card.slotKey))run.selectedCardSlots.Add(card.slotKey);
   BuildPackingAgain();
  });
  all.AddToClassList("ps-rite-chip");
  actions.Add(all);
  var none=PackspireUiFactory.Button("すべて外す",()=>{run.selectedCardSlots.Clear();BuildPackingAgain();});
  none.AddToClassList("ps-rite-chip");
  actions.Add(none);
  panel.Add(actions);

  var body=new ScrollView(ScrollViewMode.Vertical);
  body.AddToClassList("ps-rite-popup-scroll");
  var cards=Container("ps-rite-cards");
  foreach(var card in build.candidates){
   var entry=card;
   bool chosen=run.selectedCardSlots.Contains(entry.slotKey);
   var button=new Button(()=>{game.UiPackingToggleCard(entry.slotKey);BuildPackingAgain();}){text=$"{(chosen?"●":"○")} {entry.name}　{entry.cost}EN\n{entry.text}"};
   button.AddToClassList("ps-rite-card");
   if(chosen)button.AddToClassList("ps-selected");
   cards.Add(button);
  }
  if(build.candidates.Count==0)body.Add(PackspireUiFactory.Body("装備を魔方陣に置くとカード候補が出ます。"));
  body.Add(cards);
  panel.Add(body);
  overlay.Add(panel);
  return overlay;
 }

 VisualElement BuildRiteGrid(RunState run,ActiveStorageFormula formula){
  int width=formula.core.width,cells=formula.core.width*formula.core.height;
  var grid=Container("ps-rite-grid");
  packingGridElement=grid;
  float cellPercent=(100f/width)-1.4f;
  float cellHeight=Mathf.Clamp(560f/Mathf.Max(1,formula.core.height),58f,84f);
  var plateTex=Resources.Load<Texture2D>("Art/Rite/rite-cell-plate-v1");
  for(int index=0;index<cells;index++){
   int cellIndex=index;
   var occupant=PlacementAt(run,index);
   var boardElement=StorageFormulaSystem.BoardAt(formula.core,index);
   var cell=new VisualElement();
   cell.userData=cellIndex;
   cell.AddToClassList("ps-rite-cell");
   cell.focusable=true;
   cell.pickingMode=PickingMode.Position;
   cell.style.width=Length.Percent(cellPercent);
   cell.style.height=cellHeight;

   if(plateTex!=null){
    var plate=new Image{image=plateTex,scaleMode=ScaleMode.StretchToFill,pickingMode=PickingMode.Ignore};
    plate.AddToClassList("ps-rite-cell-plate");
    cell.Add(plate);
   }

   string orbExtra="";
   if(occupant!=null){
    var item=run.inventory.FirstOrDefault(x=>x.uid==occupant.itemUid);
    if(item!=null){
     var elementColor=CellElementAt(run,occupant,index);
     bool match=elementColor.HasValue&&elementColor.Value==boardElement;
     orbExtra=match?"ps-rite-orb-match":"ps-rite-orb-miss";
    }
   }
   var orbWrap=BuildRiteOrb(boardElement,orbExtra);
   if(occupant==null)orbWrap.AddToClassList("ps-rite-orb-wrap-open");
   else orbWrap.AddToClassList("ps-rite-orb-wrap-placed");
   cell.Add(orbWrap);

   if(occupant!=null){
    var item=run.inventory.FirstOrDefault(x=>x.uid==occupant.itemUid);
    if(item!=null){
     cell.tooltip=GameCatalog.Items[item.templateId].name;
     cell.Add(Atlas(game.UiEquipmentArt,ItemUv(item.templateId),"ps-rite-cell-art"));
     if(item.uid==selectedPackingUid)cell.AddToClassList("ps-selected");
     BindPackingDragSource(cell,item.uid,formula);
    }
   } else {
    cell.RegisterCallback<ClickEvent>(_=>{
     if(packingDragging||packingFormulaOpen||packingCardsOpen||string.IsNullOrEmpty(selectedPackingUid))return;
     if(!game.UiPackingPlace(selectedPackingUid,cellIndex,packingRotation))ShowToast("そこには置けません");
     BuildPackingAgain();
    });
   }
   grid.Add(cell);
  }
  return grid;
 }

 VisualElement BuildShapePreview(ItemInstance item,int rotation){
  var def=GameCatalog.Items[item.templateId];
  var layout=BackpackSystem.Layout(def,rotation,item);
  int maxX=layout.Max(c=>c.pos.x)+1;
  int maxY=layout.Max(c=>c.pos.y)+1;
  var preview=Container("ps-rite-shape");
  for(int y=0;y<maxY;y++){
   var row=Container("ps-rite-shape-row");
   for(int x=0;x<maxX;x++){
    var cell=Container("ps-rite-shape-cell");
    var found=layout.Where(c=>c.pos.x==x&&c.pos.y==y).ToList();
    if(found.Count>0){
     cell.AddToClassList("ps-rite-shape-filled");
     cell.AddToClassList("ps-element-"+found[0].element.ToString().ToLowerInvariant());
    } else {
     cell.AddToClassList("ps-rite-shape-empty");
    }
    row.Add(cell);
   }
   preview.Add(row);
  }
  return preview;
 }

 Placement PlacementAt(RunState run,int index){
  int width=BackpackSystem.GridWidth(run);
  int x=index%width,y=index/width;
  foreach(var placement in run.placements){
   var item=run.inventory.FirstOrDefault(i=>i.uid==placement.itemUid);
   if(item==null)continue;
   int ax=placement.anchor%width,ay=placement.anchor/width;
   if(BackpackSystem.Layout(GameCatalog.Items[item.templateId],placement.rotation,item).Any(c=>ax+c.pos.x==x&&ay+c.pos.y==y))return placement;
  }
  return null;
 }

 Element? CellElementAt(RunState run,Placement placement,int index){
  var item=run.inventory.FirstOrDefault(i=>i.uid==placement.itemUid);
  if(item==null)return null;
  int width=BackpackSystem.GridWidth(run);
  int x=index%width,y=index/width,ax=placement.anchor%width,ay=placement.anchor/width;
  foreach(var cell in BackpackSystem.Layout(GameCatalog.Items[item.templateId],placement.rotation,item))
   if(ax+cell.pos.x==x&&ay+cell.pos.y==y)return cell.element;
  return null;
 }

 string RotationLabel(RotationCapability capability)=>capability switch{
  RotationCapability.FlipOnly=>"反転のみ",
  RotationCapability.QuarterTurn=>"90°単位",
  _=>"フル回転",
 };

 string TraitEffectLabel(ColorTraitDef trait)=>trait.effect switch{
  ColorTraitEffect.Damage=>$"この装備の攻撃 +{trait.amount}",
  ColorTraitEffect.Block=>$"この装備の防御 +{trait.amount}",
  ColorTraitEffect.Heal=>$"この装備の回復 +{trait.amount}",
  ColorTraitEffect.CostReduce=>$"この装備のコスト -{trait.amount}",
  ColorTraitEffect.Draw=>$"ドロー +{trait.amount}",
  ColorTraitEffect.Recycle=>"使用後に山札へ戻る",
  ColorTraitEffect.DurabilityFree=>"耐久を消費しない",
  _=>"",
 };

 void BuildPackingAgain(){
  CapturePackingScroll();
  screenRoot.Clear();
  BuildPacking();
 }

 void CapturePackingScroll(){
  if(screenRoot==null)return;
  var left=screenRoot.Q<ScrollView>(className:"ps-rite-equip-scroll");
  if(left!=null)packingEquipScrollY=left.scrollOffset.y;
  var right=screenRoot.Q<ScrollView>(className:"ps-rite-right");
  if(right!=null)packingRightScrollY=right.scrollOffset.y;
 }

 void RestorePackingScroll(ScrollView left,ScrollView right){
  float leftY=packingEquipScrollY;
  float rightY=packingRightScrollY;
  if(left!=null){
   left.schedule.Execute(()=>{
    if(left!=null)left.scrollOffset=new Vector2(0,leftY);
   }).ExecuteLater(0);
  }
  if(right!=null){
   right.schedule.Execute(()=>{
    if(right!=null)right.scrollOffset=new Vector2(0,rightY);
   }).ExecuteLater(0);
  }
 }
}
}
