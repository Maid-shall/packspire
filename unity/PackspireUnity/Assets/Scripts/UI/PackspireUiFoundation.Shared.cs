using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire {
public sealed partial class PackspireUiFoundation {
 Texture2D sharedCourtyardArt;
 Texture2D sharedHubBackgroundArt;
 Texture2D sharedHubShowcasePortrait;

 Texture2D CourtyardArt(){
  if(sharedCourtyardArt==null)sharedCourtyardArt=Resources.Load<Texture2D>("Art/UI/hub-courtyard-v1");
  return sharedCourtyardArt;
 }

 Texture2D HubBackgroundArt(){
  if(sharedHubBackgroundArt==null)sharedHubBackgroundArt=Resources.Load<Texture2D>("Art/UI/PopDark/hub-bg-v1");
  if(sharedHubBackgroundArt==null)sharedHubBackgroundArt=Resources.Load<Texture2D>("Art/UI/HubDD/hub-bg-v1");
  return sharedHubBackgroundArt;
 }

 Texture2D HubShowcasePortraitArt(){
  if(sharedHubShowcasePortrait==null){
   sharedHubShowcasePortrait=Resources.Load<Texture2D>("Art/Portraits/PopDark/hero-courier-cutout-v1");
   if(sharedHubShowcasePortrait==null)
    sharedHubShowcasePortrait=Resources.Load<Texture2D>("Art/Portraits/PopDark/hero-courier-hub-v1");
   if(sharedHubShowcasePortrait==null)
    sharedHubShowcasePortrait=Resources.Load<Texture2D>("Art/Portraits/hero-courier-hub-v1");
   if(sharedHubShowcasePortrait==null)
    Debug.LogWarning("Packspire: missing hub showcase portrait (PopDark/hero-courier-cutout-v1)");
  }
  return sharedHubShowcasePortrait;
 }

 Texture2D HubPortraitFrameArt()=>Resources.Load<Texture2D>("Art/UI/PopDark/portrait-frame-v1");

 void ShowToast(string message){if(toast==null)return;toast.Clear();toast.Add(new Label(message));toast.style.display=DisplayStyle.Flex;toast.style.opacity=0f;toast.style.translate=new Translate(0,-10,0);toast.schedule.Execute(()=>{if(toast==null)return;toast.style.opacity=1f;toast.style.translate=new Translate(0,0,0);}).StartingIn(16);toast.schedule.Execute(()=>{if(toast==null)return;toast.style.opacity=0f;toast.style.translate=new Translate(0,-8,0);}).StartingIn(1700);toast.schedule.Execute(()=>{if(toast!=null)toast.style.display=DisplayStyle.None;}).StartingIn(1950);}
 VisualElement Milestone(int required,int current,string title,string description){var item=Container(current>=required?"ps-milestone ps-unlocked":"ps-milestone ps-locked");item.Add(PackspireUiFactory.Title($"Lv.{required}　{title}"));item.Add(PackspireUiFactory.Body(description));return item;}

 VisualElement ChromeBrand(string eyebrow,string title){
  var brand=Container("ps-chrome-brand");
  if(!string.IsNullOrEmpty(eyebrow)){
   var eye=new Label(eyebrow){pickingMode=PickingMode.Ignore};
   eye.AddToClassList("ps-chrome-eyebrow");
   brand.Add(eye);
  }
  var heading=PackspireUiFactory.Title(title);
  heading.AddToClassList("ps-chrome-title");
  brand.Add(heading);
  return brand;
 }

 VisualElement ChromeSection(string english,string japanese){
  var section=Container("ps-chrome-section");
  var en=new Label(english){pickingMode=PickingMode.Ignore};
  en.AddToClassList("ps-chrome-section-en");
  section.Add(en);
  var jp=new Label(japanese){pickingMode=PickingMode.Ignore};
  jp.AddToClassList("ps-chrome-section-jp");
  section.Add(jp);
  return section;
 }

 VisualElement BookShell(string title,VisualElement singleContent,bool whitePaper=false,string eyebrow=null){
  nextPageIsLeft=true;
  var shell=Container(whitePaper?"ps-book-screen ps-book-white ps-dark-surface":"ps-book-screen ps-dark-surface");
  var bg=HubBackgroundArt();
  if(bg!=null)shell.Add(Image(bg,new Rect(0,0,1,1),"ps-book-scene-bg",ScaleMode.ScaleAndCrop));
  var shade=Container("ps-book-scene-shade");
  shade.pickingMode=PickingMode.Ignore;
  shell.Add(shade);
  if(!whitePaper&&game.UiBookArt!=null)
   shell.Add(Image(game.UiBookArt,new Rect(0,0,1,1),"ps-book-background",ScaleMode.StretchToFill));
  var frame=Container("ps-book-frame");
  frame.Add(ChromeBrand(string.IsNullOrEmpty(eyebrow)?"ARCHIVE  /  LEDGER":eyebrow,title));
  var pages=Container("ps-book-pages");
  pages.name="book-pages";
  frame.Add(pages);
  shell.Add(frame);
  if(singleContent!=null)pages.Add(singleContent);
  return shell;
 }
 VisualElement Page(string title){var page=new ScrollView();page.AddToClassList("ps-book-page");page.AddToClassList(nextPageIsLeft?"ps-page-left":"ps-page-right");nextPageIsLeft=!nextPageIsLeft;var heading=Container("ps-page-heading");var label=PackspireUiFactory.Title(title);label.AddToClassList("ps-page-heading-title");heading.Add(label);heading.Add(InkRule());page.Add(heading);return page;}
 VisualElement RecordButton(string title,string subtitle,VisualElement art,bool selected,System.Action clicked){var button=new Button(clicked){tooltip=title+"\n"+subtitle};button.AddToClassList("ps-record-button");if(selected)button.AddToClassList("ps-selected");button.Add(art);var copy=Container("ps-record-copy");copy.Add(PackspireUiFactory.Title(title));copy.Add(PackspireUiFactory.Body(subtitle));button.Add(copy);if(selected)button.Add(SelectionBadge());button.Add(InkRule());return button;}
 VisualElement AtlasButton(Texture2D texture,Rect uv,string label,bool selected,System.Action clicked){var button=new Button(clicked){tooltip=label};button.AddToClassList("ps-atlas-button");if(selected)button.AddToClassList("ps-selected");button.Add(Atlas(texture,uv,"ps-atlas-image"));var name=new Label(label);name.AddToClassList("ps-atlas-label");button.Add(name);if(selected)button.Add(SelectionBadge());return button;}

 static Sprite LoadPortraitSprite(string path){
  return string.IsNullOrEmpty(path)?null:Resources.Load<Sprite>(path);
 }

 static Texture2D LoadPortraitTexture(string path){
  if(string.IsNullOrEmpty(path))return null;
  var tex=Resources.Load<Texture2D>(path);
  if(tex!=null)return tex;
  return null;
 }

 void ApplyCharacterPortraitImage(Image target,CharacterDef character){
  if(target==null||character==null)return;
  var sprite=game.ResolveCharacterPortraitSprite(character);
  if(sprite!=null){
   target.sprite=sprite;
   target.uv=new Rect(0,0,1,1);
   return;
  }
  if(character.HasPortraitAsset){
   var tex=LoadPortraitTexture(character.portraitResource);
   if(tex!=null){
    target.image=tex;
    target.uv=new Rect(0,0,1,1);
    return;
   }
  }
  var meta=game.UiMeta;
  target.image=game.UiCharacterArt;
  target.uv=CharacterUv(meta.body,meta.hair);
 }

 Image SpriteImage(Sprite sprite,Rect uv,string className,ScaleMode mode){
  var image=new Image{sprite=sprite,uv=uv,scaleMode=mode,pickingMode=PickingMode.Ignore};
  foreach(var value in className.Split(' '))
   if(!string.IsNullOrEmpty(value))image.AddToClassList(value);
  return image;
 }

 Texture2D PopDarkPortraitArt(CharacterDef def){
  if(def!=null&&!string.IsNullOrEmpty(def.id)){
   var cutout=Resources.Load<Texture2D>($"Art/Portraits/PopDark/hero-{def.id}-cutout-v1");
   if(cutout!=null)return cutout;
   var front=Resources.Load<Texture2D>($"Art/Portraits/PopDark/hero-{def.id}-front-v1");
   if(front!=null)return front;
   var hub=Resources.Load<Texture2D>($"Art/Portraits/PopDark/hero-{def.id}-hub-v1");
   if(hub!=null)return hub;
  }
  if(def!=null&&!string.IsNullOrEmpty(def.portraitFrontResource)&&!def.portraitFrontResource.Contains("/DD/")){
   var legacy=Resources.Load<Texture2D>(def.portraitFrontResource);
   if(legacy!=null)return legacy;
  }
  return HubShowcasePortraitArt();
 }

 void FitPortraitAspect(VisualElement image,float aspect){
  var frame=image.parent;
  if(frame==null||aspect<=0f)return;
  var host=frame.parent;
  var maxH=frame.contentRect.height;
  var maxW=frame.contentRect.width;
  if(host!=null){
   if(maxH<=1f)maxH=host.contentRect.height;
   if(maxW<=1f)maxW=host.contentRect.width;
  }
  if(maxH<=1f||maxW<=1f)return;
  const float inset=0.94f;
  var fitH=Mathf.Min(maxH*inset,(maxW*inset)/aspect);
  image.style.height=fitH;
  image.style.width=fitH*aspect;
 }

 VisualElement BuildPortraitDisplay(Texture2D tex,string className){
  var aspect=(float)tex.width/tex.height;
  var image=Image(tex,new Rect(0,0,1,1),"ps-portrait-display-image",ScaleMode.ScaleToFit);
  if(!string.IsNullOrEmpty(className))
   foreach(var token in className.Split(' '))
    if(!string.IsNullOrEmpty(token))image.AddToClassList(token);
  image.pickingMode=PickingMode.Ignore;
  image.style.flexGrow=0;
  image.style.flexShrink=0;
  image.style.alignSelf=Align.Center;
  image.style.opacity=1f;
  void Sync(){FitPortraitAspect(image,aspect);}
  image.RegisterCallback<GeometryChangedEvent>(_=>Sync());
  image.RegisterCallback<AttachToPanelEvent>(_=>{
   image.schedule.Execute(Sync).ExecuteLater(0);
   image.schedule.Execute(Sync).ExecuteLater(120);
  });
  return image;
 }

 VisualElement CharacterPortrait(CharacterDef def,string className){
  var tex=PopDarkPortraitArt(def);
  if(tex!=null&&tex!=game.UiCharacterArt)return BuildPortraitDisplay(tex,className);
  return Atlas(game.UiCharacterArt,CharacterUv(def?.portraitBody??0,def?.portraitHair??0),className);
 }
 VisualElement CharacterPortraitFront(CharacterDef def,string className){
  var tex=PopDarkPortraitArt(def);
  if(tex!=null&&tex!=game.UiCharacterArt)return BuildPortraitDisplay(tex,className);
  return Atlas(game.UiCharacterArt,CharacterUv(def?.portraitBody??0,def?.portraitHair??0),className);
 }
 VisualElement CharacterPortraitHub(CharacterDef def,string className){
  var tex=PopDarkPortraitArt(def);
  if(tex!=null&&tex!=game.UiCharacterArt)return BuildPortraitDisplay(tex,className);
  return Atlas(game.UiCharacterArt,CharacterUv(def?.portraitBody??0,def?.portraitHair??0),className);
 }
 VisualElement CharacterPortraitButton(CharacterDef def,bool selected,System.Action clicked){
  var tex=PopDarkPortraitArt(def);
  if(tex!=null&&tex!=game.UiCharacterArt)
   return AtlasButton(tex,new Rect(0,0,1,1),def.name,selected,clicked);
  return AtlasButton(game.UiCharacterArt,CharacterUv(def.portraitBody,def.portraitHair),def.name,selected,clicked);
 }
 VisualElement EnemyPortrait(EnemyDef def,string className){
  if(def!=null&&def.HasPortraitAsset){
   var tex=game.ResolveEnemyPortrait(def);
   if(tex!=null&&tex!=game.UiEnemyArt)return Atlas(tex,new Rect(0,0,1,1),className);
  }
  return Atlas(game.UiEnemyArt,EnemyUv(def?.id??""),className);
 }
 VisualElement SelectionBadge(){var badge=new Label("選択");badge.AddToClassList("ps-selection-badge");return badge;}
 VisualElement InkRule(){var rule=Container("ps-ink-rule");rule.pickingMode=PickingMode.Ignore;return rule;}
 VisualElement Container(string classes){var element=new VisualElement();foreach(var value in classes.Split(' '))element.AddToClassList(value);return element;}
 Image Image(Texture2D texture,Rect uv,string className,ScaleMode mode){
  var image=new Image{uv=uv,scaleMode=mode,pickingMode=PickingMode.Ignore};
  if(texture!=null)image.image=texture;
  foreach(var value in className.Split(' '))
   if(!string.IsNullOrEmpty(value))image.AddToClassList(value);
  return image;
 }
 Image Atlas(Texture2D texture,Rect uv,string className)=>Image(texture,uv,className,ScaleMode.ScaleToFit);

 string FactionName(string id)=>GameCatalog.Factions.FirstOrDefault(x=>x.id==id)?.name??id;
 string BackpackName(string id)=>GameCatalog.Backpacks.FirstOrDefault(x=>x.id==id)?.name??id;
 string ItemTypeLabel(ItemType type)=>type==ItemType.Weapon?"武器":type==ItemType.Armor?"防具":type==ItemType.Rune?"ルーン":"消耗品";
 string ElementLabel(Element value)=>value==Element.Fire?"火":value==Element.Water?"水":value==Element.Wind?"風":"土";
 Rect ItemUv(string id){int index=id=="sword"?0:id=="shield"?1:id=="ember"?2:id=="herb"?3:id=="dagger"?4:id=="plate"?5:id=="crystal"?6:id=="bomb"?7:id=="spear"?8:id=="buckler"?9:id=="flask"?10:id=="charm"?11:0;return new Rect(index%4*.25f,(2-index/4)/3f,.25f,1f/3f);}
 Rect RoleUv(string id){int index=id=="warrior"?0:id=="guardian"?1:id=="scout"?2:id=="artificer"?3:id=="blade_master"?4:id=="bulwark"?5:id=="hunter"?6:id=="grand_artificer"?7:id=="arsenal_lord"?8:id=="pack_saint"?9:id=="rune_weaver"?10:id=="grid_dancer"?11:id.Contains("knight")?5:id.Contains("druid")?6:id.Contains("artificer")||id.Contains("channeler")?7:0;return new Rect(index%4*.25f,(2-index/4)/3f,.25f,1f/3f);}
 Rect EnemyUv(string id)=>id=="sentinel"?new Rect(0,.5f,.174f,.5f):id=="rats"?new Rect(.174f,.5f,.172f,.5f):id=="porter"?new Rect(.346f,.5f,.172f,.5f):id=="mage"?new Rect(.518f,.5f,.172f,.5f):id=="beast"?new Rect(0,0,.344f,.5f):id=="knight"?new Rect(.344f,0,.347f,.5f):new Rect(.69f,0,.31f,1);
 Rect CharacterUv(int body,int hair)=>new Rect(Mathf.Clamp(body,0,3)*.25f,(2-Mathf.Clamp(hair,0,2))/3f,.25f,1f/3f);
 Rect FactionUv(string id)=>id=="iron"?new Rect(0,.5f,.5f,.5f):id=="spore"?new Rect(.5f,.5f,.5f,.5f):id=="guild"?new Rect(0,0,.5f,.5f):new Rect(.5f,0,.5f,.5f);
 Rect DungeonUv(string id)=>id=="hollow_archive"?new Rect(.5f,.5f,.5f,.5f):id=="ash_forge"?new Rect(.5f,0,.5f,.5f):new Rect(0,.5f,.5f,.5f);
}
}
