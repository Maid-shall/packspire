using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire {
/// <summary>Shared retained-mode controls. Screen code should use these instead of one-off styling.</summary>
public static class PackspireUiFactory {
 public static Label Title(string text){var label=new Label(text);label.AddToClassList("ps-title");return label;}
 public static Label Body(string text){var label=new Label(text);label.AddToClassList("ps-body");return label;}
 public static Button Button(string text,Action clicked){var button=new Button(clicked){text=text};button.AddToClassList("ps-button");return button;}
 public static void ApplyBackgroundScaleMode(VisualElement target,ScaleMode mode){
  if(target==null)return;
  var position=BackgroundPropertyHelper.ConvertScaleModeToBackgroundPosition(mode);
  target.style.backgroundPositionX=position;
  target.style.backgroundPositionY=position;
  target.style.backgroundRepeat=BackgroundPropertyHelper.ConvertScaleModeToBackgroundRepeat(mode);
  target.style.backgroundSize=BackgroundPropertyHelper.ConvertScaleModeToBackgroundSize(mode);
 }

 public static Button PrimaryButton(string text,Action clicked){
  var button=Button(text,clicked);
  button.AddToClassList("ps-action-primary");
  return button;
 }
 public static Button SecondaryButton(string text,Action clicked){
  var button=Button(text,clicked);
  button.AddToClassList("ps-action-secondary");
  return button;
 }
 public static Button TertiaryButton(string text,Action clicked){
  var button=Button(text,clicked);
  button.AddToClassList("ps-action-tertiary");
  return button;
 }
 public static Button DangerButton(string text,Action clicked){
  var button=Button(text,clicked);
  button.AddToClassList("ps-action-danger");
  return button;
 }

 // ── PopDark product chrome (Phase 1) ─────────────────────────────

 public enum PortraitFrameTier { Premium, Standard, Wide }
 public enum StateBadgeKind { New, Locked, InUse, Heirloom, Undiscovered, Selected, Danger, Rare }
 public enum PopIcon {
  Home, Gate, Packing, Vault,
  Heirloom, Guild, Codex, Shop,
  Weapon, Armor, Supply, Relic,
  Back, Search, Filter, Compare,
  RoleBasic, RoleAdvanced, RoleComposite, RoleFaction,
  RoleHidden, RoleCurrent, Selected, Locked,
  Rotate, Remove, AutoArrange, Undo,
  CardCheck, Expedition, Reward, Objective
 }
 public enum PopOrnament { PageTitle, Section, VerticalBoundary, OpenCorner }

 static Texture2D popPrimaryPlate;
 static Texture2D popSecondaryPlate;
 static Texture2D popNavCardPlate;
 static Texture2D popPortraitPremium;
 static Texture2D popPlaquePlate;
 static Texture2D popCoreIcons;
 static Texture2D popRoleControlIcons;
 static Texture2D popOrnaments;

 static void EnsurePopArt(){
  if(popPrimaryPlate==null)
   popPrimaryPlate=LoadPopTex("Art/UI/PopDark/buttons/primary-action-v1");
  if(popSecondaryPlate==null)
   popSecondaryPlate=LoadPopTex("Art/UI/PopDark/buttons/secondary-action-v1");
  if(popNavCardPlate==null)
   popNavCardPlate=LoadPopTex("Art/UI/PopDark/cards/nav-card-v1","Art/UI/PopDark/btn-card-v1");
  if(popPortraitPremium==null)
   popPortraitPremium=LoadPopTex("Art/UI/PopDark/frames/portrait-premium-v1","Art/UI/PopDark/portrait-frame-v1");
  if(popPlaquePlate==null)
   popPlaquePlate=LoadPopTex("Art/UI/PopDark/frames/plaque-v1","Art/UI/PopDark/plaque-v1");
  if(popCoreIcons==null)
   popCoreIcons=LoadPopTex("Art/UI/PopDark/SystemV2/core-icons-v1");
  if(popRoleControlIcons==null)
   popRoleControlIcons=LoadPopTex("Art/UI/PopDark/SystemV2/role-controls-v1");
  if(popOrnaments==null)
   popOrnaments=LoadPopTex("Art/UI/PopDark/SystemV2/ornaments-v1");
 }

 static Texture2D LoadPopTex(params string[] paths){
  foreach(var path in paths){
   if(string.IsNullOrEmpty(path))continue;
   var tex=PackspireResources.Load<Texture2D>(path);
   if(tex!=null)return tex;
   var sprite=PackspireResources.Load<Sprite>(path);
   if(sprite!=null&&sprite.texture!=null)return sprite.texture;
  }
  return null;
 }

 /// <summary>Battle-identical plate apply: clear defaults, set StyleBackground on the control.</summary>
 static void ApplyPopPlate(VisualElement target,Texture2D plate,Color fallbackFill,Color fallbackBorder){
  if(target==null)return;
  target.style.backgroundColor=Color.clear;
  target.style.borderTopWidth=0;
  target.style.borderRightWidth=0;
  target.style.borderBottomWidth=0;
  target.style.borderLeftWidth=0;
  target.style.borderTopLeftRadius=0;
  target.style.borderTopRightRadius=0;
  target.style.borderBottomLeftRadius=0;
  target.style.borderBottomRightRadius=0;
  target.style.paddingTop=0;
  target.style.paddingRight=0;
  target.style.paddingBottom=0;
  target.style.paddingLeft=0;
  target.style.unityBackgroundImageTintColor=Color.white;
  if(plate!=null){
   target.style.backgroundImage=new StyleBackground(plate);
   ApplyBackgroundScaleMode(target,ScaleMode.StretchToFill);
   // Keep slices at 0 in code too — USS can be overridden by importer borders.
   target.style.unitySliceLeft=0;
   target.style.unitySliceRight=0;
   target.style.unitySliceTop=0;
   target.style.unitySliceBottom=0;
   return;
  }
  target.style.backgroundImage=StyleKeyword.None;
  target.style.backgroundColor=fallbackFill;
  target.style.borderTopWidth=2;
  target.style.borderRightWidth=2;
  target.style.borderBottomWidth=2;
  target.style.borderLeftWidth=2;
  target.style.borderTopColor=fallbackBorder;
  target.style.borderRightColor=fallbackBorder;
  target.style.borderBottomColor=fallbackBorder;
  target.style.borderLeftColor=fallbackBorder;
 }

 static void AddSegmentedPlate(Button target,Texture2D plate){
  if(target==null||plate==null)return;
  target.style.backgroundImage=StyleKeyword.None;
  target.style.backgroundColor=Color.clear;
  var host=Ve("ps-pop-button-plate");
  host.pickingMode=PickingMode.Ignore;
  host.Add(PlateSegment(plate,new Rect(0f,0f,.17f,1f),"ps-pop-button-cap ps-pop-button-cap-left"));
  host.Add(PlateSegment(plate,new Rect(.17f,0f,.66f,1f),"ps-pop-button-center"));
  host.Add(PlateSegment(plate,new Rect(.83f,0f,.17f,1f),"ps-pop-button-cap ps-pop-button-cap-right"));
  target.Add(host);
 }

 static Image PlateSegment(Texture2D plate,Rect uv,string classes){
  var image=new Image{
   image=plate,
   uv=uv,
   scaleMode=ScaleMode.StretchToFill,
   pickingMode=PickingMode.Ignore
  };
  foreach(var value in classes.Split(new[]{' '},StringSplitOptions.RemoveEmptyEntries))
   image.AddToClassList(value);
  return image;
 }

 public static Button PrimaryActionButton(string text,Action clicked){
  EnsurePopArt();
  var button=new Button(clicked){text=""};
  button.Clear();
  button.AddToClassList("ps-pop-primary");
  if(popPrimaryPlate!=null)AddSegmentedPlate(button,popPrimaryPlate);
  else ApplyPopPlate(button,null,new Color(0.08f,0.06f,0.16f,0.96f),new Color(1f,0.47f,0.55f,0.9f));
  var label=new Label(text){pickingMode=PickingMode.Ignore,name="ps-pop-primary-label"};
  label.AddToClassList("ps-pop-primary-label");
  button.Add(label);
  return button;
 }

 public static void SetPrimaryActionLabel(Button button,string text){
  SetActionLabel(button,text);
 }

 public static void SetActionLabel(Button button,string text){
  if(button==null)return;
  var label=button.Q<Label>(name:"ps-pop-action-label")??button.Q<Label>(name:"ps-pop-primary-label");
  if(label!=null)label.text=text;
  else button.text=text;
 }

 public static void DecorateActionButton(Button button,bool primary){
  if(button==null)return;
  EnsurePopArt();
  string text=button.text;
  button.text="";
  button.Clear();
  button.AddToClassList(primary?"ps-pop-primary":"ps-pop-secondary");
  var plate=primary?popPrimaryPlate:popSecondaryPlate;
  if(plate!=null)AddSegmentedPlate(button,plate);
  else ApplyPopPlate(button,null,
   primary?new Color(0.08f,0.06f,0.16f,0.96f):new Color(0.07f,0.06f,0.14f,0.92f),
   primary?new Color(1f,0.47f,0.55f,0.9f):new Color(0.35f,0.82f,0.77f,0.65f));
  var label=new Label(text){pickingMode=PickingMode.Ignore,name="ps-pop-action-label"};
  label.AddToClassList(primary?"ps-pop-primary-label":"ps-pop-secondary-label");
  button.Add(label);
 }

 public static Button SecondaryActionButton(string text,Action clicked){
  EnsurePopArt();
  var button=new Button(clicked){text=""};
  button.Clear();
  button.AddToClassList("ps-pop-secondary");
  if(popSecondaryPlate!=null)AddSegmentedPlate(button,popSecondaryPlate);
  else ApplyPopPlate(button,null,new Color(0.07f,0.06f,0.14f,0.92f),new Color(0.35f,0.82f,0.77f,0.65f));
  var label=new Label(text){pickingMode=PickingMode.Ignore,name="ps-pop-action-label"};
  label.AddToClassList("ps-pop-secondary-label");
  button.Add(label);
  return button;
 }

 public static Button DangerActionButton(string text,Action clicked){
  EnsurePopArt();
  var button=new Button(clicked){text=""};
  button.Clear();
  button.AddToClassList("ps-pop-danger");
  if(popPrimaryPlate!=null)AddSegmentedPlate(button,popPrimaryPlate);
  else ApplyPopPlate(button,null,new Color(0.18f,0.06f,0.08f,0.96f),new Color(0.86f,0.31f,0.27f,0.95f));
  var label=new Label(text){pickingMode=PickingMode.Ignore,name="ps-pop-action-label"};
  label.AddToClassList("ps-pop-danger-label");
  button.Add(label);
  return button;
 }

 public static Image SystemIcon(PopIcon icon,string classes=""){
  EnsurePopArt();
  int index=(int)icon;
  var texture=index<16?popCoreIcons:popRoleControlIcons;
  int atlasIndex=index<16?index:index-16;
  var image=new Image{
   image=texture,
   uv=AtlasUv(atlasIndex,4,4),
   scaleMode=ScaleMode.ScaleToFit,
   pickingMode=PickingMode.Ignore
  };
  image.AddToClassList("ps-system-icon");
  AddClasses(image,classes);
  return image;
 }

 public static Image SystemOrnament(PopOrnament ornament,string classes=""){
  EnsurePopArt();
  var image=new Image{
   image=popOrnaments,
   // Ornament art is intentionally sparse inside the 2x2 source atlas.
   // Crop to the painted pixels so a 1080p divider keeps a real 3-5 px
   // silhouette instead of shrinking the entire quadrant below one pixel.
   uv=OrnamentUv(ornament),
   scaleMode=ScaleMode.StretchToFill,
   pickingMode=PickingMode.Ignore
  };
  image.AddToClassList("ps-system-ornament");
  image.AddToClassList(ornament switch{
   PopOrnament.PageTitle=>"ps-system-ornament-title",
   PopOrnament.Section=>"ps-system-ornament-section",
   PopOrnament.VerticalBoundary=>"ps-system-ornament-vertical",
   _=>"ps-system-ornament-corner"
  });
  AddClasses(image,classes);
  return image;
 }

 public static VisualElement IconTitle(PopIcon icon,string eyebrow,string title,string classes=""){
  var host=Ve("ps-icon-title "+classes);
  var iconHost=Ve("ps-icon-title-mark");
  iconHost.pickingMode=PickingMode.Ignore;
  iconHost.Add(SystemIcon(icon,"ps-icon-title-image"));
  host.Add(iconHost);
  var copy=Ve("ps-icon-title-copy");
  copy.pickingMode=PickingMode.Ignore;
  if(!string.IsNullOrEmpty(eyebrow)){
   var eye=new Label(eyebrow){pickingMode=PickingMode.Ignore};
   eye.AddToClassList("ps-icon-title-eyebrow");
   copy.Add(eye);
  }
  var name=new Label(title){pickingMode=PickingMode.Ignore};
  name.AddToClassList("ps-icon-title-name");
  copy.Add(name);
  host.Add(copy);
  return host;
 }

 static Rect AtlasUv(int index,int columns,int rows){
  int col=Mathf.Clamp(index%columns,0,columns-1);
  int row=Mathf.Clamp(index/columns,0,rows-1);
  float width=1f/columns;
  float height=1f/rows;
  return new Rect(col*width,(rows-1-row)*height,width,height);
 }

 static Rect OrnamentUv(PopOrnament ornament)=>ornament switch{
  // Pixel-tight crops for the fixed 1280x1280 ornament atlas, with a small
  // transparent safety margin retained around every painted silhouette.
  PopOrnament.PageTitle=>new Rect(16f/1280f,913f/1280f,608f/1280f,95f/1280f),
  PopOrnament.Section=>new Rect(656f/1280f,920f/1280f,608f/1280f,81f/1280f),
  PopOrnament.VerticalBoundary=>new Rect(283f/1280f,16f/1280f,73f/1280f,608f/1280f),
  _=>new Rect(703f/1280f,16f/1280f,513f/1280f,608f/1280f)
 };

 static void AddClasses(VisualElement target,string classes){
  if(target==null||string.IsNullOrEmpty(classes))return;
  foreach(var value in classes.Split(new[]{' '},StringSplitOptions.RemoveEmptyEntries))
   target.AddToClassList(value);
 }

 public static Button NavigationCard(string eyebrow,string title,string subtitle,Action clicked,bool selected=false){
  EnsurePopArt();
  var button=new Button(clicked){text="",tooltip=title};
  button.Clear();
  button.AddToClassList("ps-pop-nav-card");
  if(selected)button.AddToClassList("ps-selected");
  ApplyPopPlate(button,popNavCardPlate,new Color(0.07f,0.05f,0.14f,0.94f),new Color(1f,0.47f,0.55f,0.75f));
  var accentL=Ve("ps-pop-nav-card-accent-l");
  accentL.pickingMode=PickingMode.Ignore;
  button.Add(accentL);
  var accentR=Ve("ps-pop-nav-card-accent-r");
  accentR.pickingMode=PickingMode.Ignore;
  button.Add(accentR);
  var copy=Ve("ps-pop-nav-card-copy");
  copy.pickingMode=PickingMode.Ignore;
  if(!string.IsNullOrEmpty(eyebrow)){
   var eye=new Label(eyebrow){pickingMode=PickingMode.Ignore};
   eye.AddToClassList("ps-pop-nav-card-eyebrow");
   copy.Add(eye);
  }
  var name=new Label(title){pickingMode=PickingMode.Ignore};
  name.AddToClassList("ps-pop-nav-card-title");
  copy.Add(name);
  if(!string.IsNullOrEmpty(subtitle)){
   var sub=new Label(subtitle){pickingMode=PickingMode.Ignore};
   sub.AddToClassList("ps-pop-nav-card-sub");
   copy.Add(sub);
  }
  button.Add(copy);
  return button;
 }

 public static Button ListRow(string id,string primary,string secondary,bool selected,Action onClick,VisualElement leading=null,StateBadgeKind? badge=null){
  var row=new Button(onClick){userData=id,tooltip=primary,text=""};
  row.Clear();
  row.AddToClassList("ps-list-item");
  row.AddToClassList("ps-pop-list-row");
  if(selected)row.AddToClassList("ps-selected");
  // Quiet rows stay USS-only (no plate art) so lists stay dense.
  row.style.backgroundColor=Color.clear;
  row.style.borderTopWidth=0;
  row.style.borderRightWidth=0;
  row.style.borderBottomWidth=0;
  var accent=Ve("ps-list-item-mark");
  accent.pickingMode=PickingMode.Ignore;
  row.Add(accent);
  var gem=Ve("ps-pop-list-row-gem");
  gem.pickingMode=PickingMode.Ignore;
  row.Add(gem);
  if(leading!=null){
   leading.pickingMode=PickingMode.Ignore;
   leading.AddToClassList("ps-pop-list-row-leading");
   row.Add(leading);
  }
  var copy=Ve("ps-pop-list-row-copy");
  copy.pickingMode=PickingMode.Ignore;
  var name=new Label(primary){pickingMode=PickingMode.Ignore};
  name.AddToClassList("ps-pop-list-row-name");
  name.AddToClassList("ps-typo-item");
  copy.Add(name);
  if(!string.IsNullOrEmpty(secondary)){
   var sub=new Label(secondary){pickingMode=PickingMode.Ignore};
   sub.AddToClassList("ps-pop-list-row-sub");
   sub.AddToClassList("ps-typo-secondary");
   copy.Add(sub);
  }
  row.Add(copy);
  if(badge.HasValue)row.Add(StateBadge(badge.Value));
  return row;
 }

 public static VisualElement PortraitFrame(PortraitFrameTier tier,VisualElement content){
  EnsurePopArt();
  string tierClass=tier switch{
   PortraitFrameTier.Premium=>"ps-pop-portrait-premium",
   PortraitFrameTier.Wide=>"ps-pop-portrait-wide",
   _=>"ps-pop-portrait-standard"
  };
  var frame=Ve("ps-pop-portrait-frame "+tierClass);
  var plate=Ve("ps-pop-portrait-plate");
  plate.pickingMode=PickingMode.Ignore;
  Texture2D plateTex=tier==PortraitFrameTier.Wide?popPlaquePlate
   :tier==PortraitFrameTier.Premium?popPortraitPremium
   :popNavCardPlate;
  ApplyPopPlate(plate,plateTex,new Color(0.06f,0.05f,0.12f,0.55f),new Color(1f,0.47f,0.55f,0.55f));
  frame.Add(plate);
  var viewport=Ve("ps-pop-portrait-viewport");
  if(content!=null){
   content.pickingMode=PickingMode.Ignore;
   viewport.Add(content);
  }
  frame.Add(viewport);
  var corners=Ve("ps-pop-portrait-corners");
  corners.pickingMode=PickingMode.Ignore;
  corners.Add(Ve("ps-pop-corner ps-pop-corner-tl"));
  corners.Add(Ve("ps-pop-corner ps-pop-corner-tr"));
  corners.Add(Ve("ps-pop-corner ps-pop-corner-bl"));
  corners.Add(Ve("ps-pop-corner ps-pop-corner-br"));
  frame.Add(corners);
  return frame;
 }

 public static VisualElement InformationPlaque(string eyebrow,string title,params string[] lines){
  EnsurePopArt();
  var plaque=Ve("ps-pop-plaque");
  var plate=Ve("ps-pop-plaque-plate");
  plate.pickingMode=PickingMode.Ignore;
  ApplyPopPlate(plate,popPlaquePlate,new Color(0.06f,0.05f,0.12f,0.72f),new Color(0.35f,0.82f,0.77f,0.55f));
  plaque.Add(plate);
  var head=Ve("ps-pop-plaque-head");
  head.pickingMode=PickingMode.Ignore;
  if(!string.IsNullOrEmpty(eyebrow)){
   var eye=new Label(eyebrow){pickingMode=PickingMode.Ignore};
   eye.AddToClassList("ps-pop-plaque-eyebrow");
   head.Add(eye);
  }
  if(!string.IsNullOrEmpty(title)){
   var name=new Label(title){pickingMode=PickingMode.Ignore};
   name.AddToClassList("ps-pop-plaque-title");
   head.Add(name);
  }
  plaque.Add(head);
  if(lines!=null){
   foreach(var line in lines){
    if(string.IsNullOrEmpty(line))continue;
    var body=new Label(line){pickingMode=PickingMode.Ignore};
    body.AddToClassList("ps-pop-plaque-line");
    plaque.Add(body);
   }
  }
  return plaque;
 }

 public static VisualElement StateBadge(StateBadgeKind kind){
  string text=kind switch{
   StateBadgeKind.New=>"NEW",
   StateBadgeKind.Locked=>"封印",
   StateBadgeKind.InUse=>"使用中",
   StateBadgeKind.Heirloom=>"家宝",
   StateBadgeKind.Undiscovered=>"？",
   StateBadgeKind.Selected=>"選択",
   StateBadgeKind.Danger=>"危険",
   StateBadgeKind.Rare=>"稀少",
   _=>""
  };
  string cls=kind switch{
   StateBadgeKind.New=>"ps-pop-badge-new",
   StateBadgeKind.Locked=>"ps-pop-badge-locked",
   StateBadgeKind.InUse=>"ps-pop-badge-inuse",
   StateBadgeKind.Heirloom=>"ps-pop-badge-heirloom",
   StateBadgeKind.Undiscovered=>"ps-pop-badge-undiscovered",
   StateBadgeKind.Selected=>"ps-pop-badge-selected",
   StateBadgeKind.Danger=>"ps-pop-badge-danger",
   StateBadgeKind.Rare=>"ps-pop-badge-rare",
   _=>"ps-pop-badge"
  };
  var badge=Ve("ps-pop-badge "+cls);
  badge.pickingMode=PickingMode.Ignore;
  badge.Add(new Label(text){pickingMode=PickingMode.Ignore});
  return badge;
 }

 public static VisualElement CornerDecorationHost(){
  var host=Ve("ps-pop-corner-host");
  host.pickingMode=PickingMode.Ignore;
  host.Add(Ve("ps-pop-corner ps-pop-corner-tl"));
  host.Add(Ve("ps-pop-corner ps-pop-corner-tr"));
  host.Add(Ve("ps-pop-corner ps-pop-corner-bl"));
  host.Add(Ve("ps-pop-corner ps-pop-corner-br"));
  return host;
 }

 public static VisualElement EmptyState(string title,string description){
  var state=new VisualElement();
  state.AddToClassList("ps-card");
  state.AddToClassList("ps-empty-state");
  state.Add(Title(title));
  state.Add(Body(description));
  return state;
 }

 public static VisualElement Card(string title,string description,Action clicked=null){
  var card=new VisualElement();
  card.AddToClassList("ps-card");
  card.Add(Title(title));
  card.Add(Body(description));
  if(clicked!=null){card.focusable=true;card.RegisterCallback<ClickEvent>(_=>clicked());}
  return card;
 }

 public static VisualElement Dialog(string title,string description,string confirmLabel,Action confirm,string cancelLabel,Action cancel){
  var dialog=new VisualElement();
  dialog.AddToClassList("ps-dialog");
  dialog.Add(Title(title));
  dialog.Add(Body(description));
  var actions=new VisualElement();
  actions.AddToClassList("ps-dialog-actions");
  actions.Add(TertiaryButton(cancelLabel,cancel));
  actions.Add(PrimaryActionButton(confirmLabel,confirm));
  dialog.Add(actions);
  return dialog;
 }

 static VisualElement Ve(string classes){
  var element=new VisualElement();
  foreach(var value in classes.Split(new[]{' '},StringSplitOptions.RemoveEmptyEntries))
   element.AddToClassList(value);
  return element;
 }
}
}
