using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire {
public sealed partial class PackspireUiFoundation {
 VisualElement expeditionShell;
 ScrollView expeditionDestScroll;
 VisualElement expeditionDestList;
 VisualElement expeditionArtHost;
 VisualElement expeditionArtImageHost;
 VisualElement expeditionArtLockOverlay;
 Label expeditionArtCaptionName;
 Label expeditionArtCaptionSub;
 VisualElement expeditionDungeonInfoHost;
 VisualElement expeditionCharacterHost;
 VisualElement expeditionLoadoutHost;
 VisualElement expeditionLoadoutList;
 VisualElement expeditionLoadoutDetailHost;
 ScrollView expeditionDetailScroll;
 Button expeditionDepartButton;
 Label expeditionDepartReason;
 float expeditionDestScrollY;
 float expeditionDetailScrollY;

#if UNITY_EDITOR
 const bool showAllDungeonsForLayoutPreview=true;
#else
 const bool showAllDungeonsForLayoutPreview=false;
#endif

 void BuildExpedition(){
  var meta=game.UiMeta;
  EnsureExpeditionDungeonSelection(meta);

  expeditionShell=Container("ps-expedition-screen ps-dark-surface");
  var backgroundHost=Container("ps-layer-background");
  var bg=HubBackgroundArt();
  if(bg==null)bg=CourtyardArt();
  if(bg!=null)backgroundHost.Add(Image(bg,new Rect(0,0,1,1),"ps-mgmt-bg",ScaleMode.ScaleAndCrop));
  var shade=Container("ps-mgmt-shade");
  shade.pickingMode=PickingMode.Ignore;
  backgroundHost.Add(shade);
  expeditionShell.Add(backgroundHost);

  var contentHost=Container("ps-layer-content");
  var header=Container("ps-mgmt-header");
  header.Add(ChromeBrand("EXPEDITION  /  BRIEF","遠征準備"));
  contentHost.Add(header);

  var body=Container("ps-expedition-body");

  var destCol=Container("ps-exp-col-dest");
  var destHead=new Label("遠征先"){pickingMode=PickingMode.Ignore};
  destHead.AddToClassList("ps-exp-dest-heading");
  destCol.Add(destHead);
  expeditionDestScroll=new ScrollView(ScrollViewMode.Vertical);
  expeditionDestScroll.AddToClassList("ps-exp-dest-scroll");
  expeditionDestScroll.verticalScrollerVisibility=ScrollerVisibility.Auto;
  expeditionDestScroll.scrollOffset=new Vector2(0,expeditionDestScrollY);
  expeditionDestList=Container("ps-exp-dest-list");
  expeditionDestScroll.Add(expeditionDestList);
  destCol.Add(expeditionDestScroll);
  body.Add(destCol);

  var artCol=Container("ps-exp-col-art");
  expeditionArtHost=Container("ps-exp-art-host");
  expeditionArtImageHost=Container("ps-exp-art-image-host");
  expeditionArtHost.Add(expeditionArtImageHost);
  expeditionArtLockOverlay=Container("ps-exp-art-lock");
  expeditionArtLockOverlay.pickingMode=PickingMode.Ignore;
  expeditionArtLockOverlay.style.display=DisplayStyle.None;
  expeditionArtHost.Add(expeditionArtLockOverlay);
  var caption=Container("ps-exp-art-caption");
  caption.pickingMode=PickingMode.Ignore;
  expeditionArtCaptionName=new Label(){pickingMode=PickingMode.Ignore};
  expeditionArtCaptionName.AddToClassList("ps-exp-art-caption-name");
  expeditionArtCaptionSub=new Label(){pickingMode=PickingMode.Ignore};
  expeditionArtCaptionSub.AddToClassList("ps-exp-art-caption-sub");
  caption.Add(expeditionArtCaptionName);
  caption.Add(expeditionArtCaptionSub);
  expeditionArtHost.Add(caption);
  artCol.Add(expeditionArtHost);
  body.Add(artCol);

  var detailCol=Container("ps-exp-col-detail ps-expedition-detail-column");
  expeditionDetailScroll=new ScrollView(ScrollViewMode.Vertical);
  expeditionDetailScroll.AddToClassList("ps-exp-detail-scroll ps-expedition-detail-scroll");
  expeditionDetailScroll.verticalScrollerVisibility=ScrollerVisibility.Auto;
  expeditionDetailScroll.scrollOffset=new Vector2(0,expeditionDetailScrollY);
  var detailBody=Container("ps-exp-detail-body");
  expeditionDungeonInfoHost=Container("ps-exp-section destination-section");
  expeditionCharacterHost=Container("ps-exp-section character-section");
  expeditionLoadoutHost=Container("ps-exp-section loadout-section");
  detailBody.Add(expeditionDungeonInfoHost);
  detailBody.Add(expeditionCharacterHost);
  detailBody.Add(expeditionLoadoutHost);
  expeditionDetailScroll.Add(detailBody);
  detailCol.Add(expeditionDetailScroll);

  var footer=Container("ps-exp-footer departure-footer");
  expeditionDepartReason=new Label(){pickingMode=PickingMode.Ignore};
  expeditionDepartReason.AddToClassList("ps-exp-depart-reason");
  footer.Add(expeditionDepartReason);
  expeditionDepartButton=PackspireUiFactory.Button("遠征を開始",()=>{
   if(!IsDungeonUnlocked(meta,selectedDungeonId))return;
   game.UiStartExpedition(selectedDungeonId);
  });
  expeditionDepartButton.AddToClassList("ps-primary-action");
  expeditionDepartButton.AddToClassList("ps-chrome-action");
  expeditionDepartButton.AddToClassList("ps-exp-depart-btn");
  footer.Add(expeditionDepartButton);
  detailCol.Add(footer);
  body.Add(detailCol);

  contentHost.Add(body);
  expeditionShell.Add(contentHost);
  screenRoot.Add(expeditionShell);

  PopulateExpeditionDestList(meta);
  RefreshExpeditionArt(meta);
  RefreshExpeditionDungeonInfo(meta);
  RefreshExpeditionCharacter(meta);
  RefreshExpeditionLoadout(meta);
  RefreshExpeditionDepart(meta);
  RestoreExpeditionScroll();
 }

 VisualElement ExpeditionInfoRow(string label,string value){
  var row=Container("ps-exp-info-row");
  var labelEl=new Label(label){pickingMode=PickingMode.Ignore};
  labelEl.AddToClassList("ps-exp-info-label");
  var valueEl=new Label(value){pickingMode=PickingMode.Ignore};
  valueEl.AddToClassList("ps-exp-info-value");
  row.Add(labelEl);
  row.Add(valueEl);
  return row;
 }

 VisualElement ExpeditionTextBlock(string className,string text){
  var block=new Label(text){pickingMode=PickingMode.Ignore};
  block.AddToClassList(className);
  return block;
 }

 VisualElement ExpeditionInfoStack(string label,string value){
  var block=Container("ps-exp-info-stack");
  var labelEl=new Label(label){pickingMode=PickingMode.Ignore};
  labelEl.AddToClassList("ps-exp-info-stack-label");
  var valueEl=new Label(value){pickingMode=PickingMode.Ignore};
  valueEl.AddToClassList("ps-exp-info-stack-value");
  block.Add(labelEl);
  block.Add(valueEl);
  return block;
 }

 void EnsureExpeditionDungeonSelection(MetaSave meta){
  int unlocked=UnlockedDungeonCount(meta);
  if(string.IsNullOrEmpty(selectedDungeonId)||!IsDungeonUnlocked(meta,selectedDungeonId))
   selectedDungeonId=GameCatalog.Dungeons[0].id;
  if(!ExpeditionDungeonVisible(meta,selectedDungeonId))
   selectedDungeonId=VisibleExpeditionDungeons(meta).FirstOrDefault()?.id??GameCatalog.Dungeons[0].id;
 }

 int UnlockedDungeonCount(MetaSave meta)=>Mathf.Clamp(meta.dungeonsUnlocked,1,GameCatalog.Dungeons.Length);

 int DungeonIndex(string dungeonId){
  for(int i=0;i<GameCatalog.Dungeons.Length;i++)
   if(GameCatalog.Dungeons[i].id==dungeonId)return i;
  return -1;
 }

 bool IsDungeonUnlocked(MetaSave meta,string dungeonId){
  int index=DungeonIndex(dungeonId);
  return index>=0&&index<UnlockedDungeonCount(meta);
 }

 bool ExpeditionLayoutPreviewEnabled(){
#if UNITY_EDITOR
  return showAllDungeonsForLayoutPreview;
#else
  return showAllDungeonsForLayoutPreview||Debug.isDebugBuild;
#endif
 }

 bool ExpeditionDungeonVisible(MetaSave meta,string dungeonId){
  int index=DungeonIndex(dungeonId);
  return index>=0;
 }

 IEnumerable<DungeonDef> VisibleExpeditionDungeons(MetaSave meta){
  foreach(var dungeon in GameCatalog.Dungeons)
   yield return dungeon;
 }

 void SaveExpeditionScroll(){
  if(expeditionDestScroll!=null)expeditionDestScrollY=expeditionDestScroll.scrollOffset.y;
  if(expeditionDetailScroll!=null)expeditionDetailScrollY=expeditionDetailScroll.scrollOffset.y;
 }

 void RestoreExpeditionScroll(){
  float destY=expeditionDestScrollY;
  float detailY=expeditionDetailScrollY;
  expeditionDestScroll?.schedule.Execute(()=>{
   if(expeditionDestScroll!=null)expeditionDestScroll.scrollOffset=new Vector2(0,destY);
  }).ExecuteLater(0);
  expeditionDetailScroll?.schedule.Execute(()=>{
   if(expeditionDetailScroll!=null)expeditionDetailScroll.scrollOffset=new Vector2(0,detailY);
  }).ExecuteLater(0);
 }

 void PopulateExpeditionDestList(MetaSave meta){
  if(expeditionDestList==null)return;
  expeditionDestList.Clear();
  foreach(var dungeon in VisibleExpeditionDungeons(meta)){
   bool unlocked=IsDungeonUnlocked(meta,dungeon.id);
   bool previewLocked=ExpeditionLayoutPreviewEnabled()&&!unlocked;
   var row=new Button(()=>{
    if(!IsDungeonUnlocked(meta,dungeon.id))return;
    SelectExpeditionDungeon(dungeon.id);
   }){userData=dungeon.id,tooltip=dungeon.name};
   row.AddToClassList("ps-exp-dest-row");
   if(dungeon.id==selectedDungeonId)row.AddToClassList("ps-selected");
   if(!unlocked)row.AddToClassList("ps-exp-dest-locked");
   if(previewLocked)row.AddToClassList("ps-exp-dest-preview");

   var thumb=Atlas(game.UiDungeonArt,DungeonUv(dungeon.id),"ps-exp-dest-thumb");
   row.Add(thumb);

   var copy=Container("ps-exp-dest-copy");
   var name=new Label(unlocked?dungeon.name:"封印された地図"){pickingMode=PickingMode.Ignore};
   name.AddToClassList("ps-exp-dest-name");
   copy.Add(name);
   var sub=new Label(ExpeditionDestSubline(dungeon,unlocked)){pickingMode=PickingMode.Ignore};
   sub.AddToClassList("ps-exp-dest-sub");
   copy.Add(sub);
   row.Add(copy);

   var state=new Label(unlocked?"解放":"未解放"){pickingMode=PickingMode.Ignore};
   state.AddToClassList("ps-exp-dest-state");
   row.Add(state);
   expeditionDestList.Add(row);
  }
 }

 string ExpeditionDestSubline(DungeonDef dungeon,bool unlocked){
  if(!unlocked)return "到達前";
  return $"規模 {dungeon.battles}　{DungeonDifficultyLabel(dungeon)}";
 }

 string DungeonDifficultyLabel(DungeonDef dungeon){
  if(dungeon.hpScale>=1.5f)return "難度高";
  if(dungeon.hpScale>=1.2f)return "難度中";
  return "難度基準";
 }

 void SelectExpeditionDungeon(string dungeonId){
  if(string.IsNullOrEmpty(dungeonId)||selectedDungeonId==dungeonId){
   RefreshExpeditionDepart(game.UiMeta);
   return;
  }
  SaveExpeditionScroll();
  selectedDungeonId=dungeonId;
  UpdateExpeditionDestSelection();
  RefreshExpeditionArt(game.UiMeta);
  RefreshExpeditionDungeonInfo(game.UiMeta);
  RefreshExpeditionLoadoutSummary(game.UiMeta);
  RefreshExpeditionDepart(game.UiMeta);
  RestoreExpeditionScroll();
 }

 void SelectExpeditionLoadout(string loadoutId){
  SaveExpeditionScroll();
  game.UiSelectLoadout(loadoutId);
  UpdateExpeditionLoadoutSelection();
  RefreshExpeditionLoadoutSummary(game.UiMeta);
  RefreshExpeditionDepart(game.UiMeta);
  RestoreExpeditionScroll();
 }

 void UpdateExpeditionDestSelection(){
  if(expeditionDestList==null)return;
  var meta=game.UiMeta;
  foreach(var child in expeditionDestList.Children()){
   if(child is not Button row||row.userData is not string id)continue;
   bool unlocked=IsDungeonUnlocked(meta,id);
   row.EnableInClassList("ps-selected",id==selectedDungeonId);
   row.SetEnabled(unlocked);
  }
 }

 void UpdateExpeditionLoadoutSelection(){
  if(expeditionLoadoutList==null)return;
  var meta=game.UiMeta;
  foreach(var child in expeditionLoadoutList.Children()){
   if(child is not Button row||row.userData is not string id)continue;
   row.EnableInClassList("ps-selected",id==meta.selectedLoadoutId);
  }
 }

 void RefreshExpeditionArt(MetaSave meta){
  if(expeditionArtImageHost==null||expeditionArtLockOverlay==null)return;
  var dungeon=GameCatalog.Dungeons.FirstOrDefault(x=>x.id==selectedDungeonId)??GameCatalog.Dungeons[0];
  bool unlocked=IsDungeonUnlocked(meta,dungeon.id);
  expeditionArtImageHost.Clear();
  expeditionArtImageHost.Add(Atlas(game.UiDungeonArt,DungeonUv(dungeon.id),"ps-exp-art-image"));
  expeditionArtCaptionName.text=dungeon.name;
  expeditionArtCaptionSub.text=dungeon.description.Length>28?dungeon.description.Substring(0,28)+"…":dungeon.description;
  expeditionArtLockOverlay.Clear();
  if(unlocked){
   expeditionArtLockOverlay.style.display=DisplayStyle.None;
   expeditionArtHost?.RemoveFromClassList("ps-exp-art-locked");
  } else {
   expeditionArtLockOverlay.style.display=DisplayStyle.Flex;
   expeditionArtHost?.AddToClassList("ps-exp-art-locked");
   var lockTitle=new Label("未解放"){pickingMode=PickingMode.Ignore};
   lockTitle.AddToClassList("ps-exp-art-lock-title");
   expeditionArtLockOverlay.Add(lockTitle);
   var hint=ExpeditionTextBlock("ps-exp-info-value",ExpeditionUnlockHint(meta,dungeon));
   expeditionArtLockOverlay.Add(hint);
  }
 }

 string ExpeditionUnlockHint(MetaSave meta,DungeonDef dungeon){
  int index=DungeonIndex(dungeon.id);
  if(index<=0)return "拠点の進行で解放されます";
  var prev=GameCatalog.Dungeons[index-1];
  return $"「{prev.name}」クリア後に解放";
 }

 void RefreshExpeditionDungeonInfo(MetaSave meta){
  if(expeditionDungeonInfoHost==null)return;
  expeditionDungeonInfoHost.Clear();
  var dungeon=GameCatalog.Dungeons.FirstOrDefault(x=>x.id==selectedDungeonId)??GameCatalog.Dungeons[0];
  expeditionDungeonInfoHost.Add(ChromeSection("DESTINATION","ダンジョン情報"));
  expeditionDungeonInfoHost.Add(ExpeditionTextBlock("ps-exp-dungeon-name",dungeon.name));
  expeditionDungeonInfoHost.Add(ExpeditionTextBlock("ps-exp-dungeon-desc",dungeon.description));
  expeditionDungeonInfoHost.Add(ExpeditionInfoRow("推奨難度",DungeonDifficultyLabel(dungeon)));
  expeditionDungeonInfoHost.Add(ExpeditionInfoRow("敵HP倍率",$"×{dungeon.hpScale:0.00}"));
  if(dungeon.damage>0)
   expeditionDungeonInfoHost.Add(ExpeditionInfoRow("敵追加攻撃",$"+{dungeon.damage}"));
  expeditionDungeonInfoHost.Add(ExpeditionInfoRow("報酬倍率",$"×{dungeon.goldScale:0.00}"));
  expeditionDungeonInfoHost.Add(ExpeditionInfoRow("探索規模",$"{dungeon.battles} 区画"));
 }

 void RefreshExpeditionCharacter(MetaSave meta){
  if(expeditionCharacterHost==null)return;
  expeditionCharacterHost.Clear();
  var character=CharacterCatalog.Get(meta.selectedCharacterId);
  expeditionCharacterHost.Add(ChromeSection("PARTY","出撃キャラクター"));
  expeditionCharacterHost.Add(BuildExpeditionCharacterBanner(character));
  var traitBlock=Container("ps-exp-character-trait-block");
  traitBlock.Add(ExpeditionInfoStack("特性",$"{character.traitName}：{character.traitText}"));
  expeditionCharacterHost.Add(traitBlock);
  var skillBlock=Container("ps-exp-character-skill-block");
  skillBlock.Add(ExpeditionInfoStack("スキル",$"{character.activeSkillName}：{character.activeSkillText}"));
  expeditionCharacterHost.Add(skillBlock);
 }

 static readonly Rect DefaultExpeditionBannerUv=new(0.10f,0.72f,0.80f,0.22f);

 static Rect ResolveExpeditionBannerUv(CharacterDef def){
  if(def.expeditionBannerUv.width>0.001f)return def.expeditionBannerUv;
  return DefaultExpeditionBannerUv;
 }

 static void PositionExpeditionBannerCrop(VisualElement layer,Rect eyeUv){
  var zoomW=100f/Mathf.Max(eyeUv.width,0.01f);
  var zoomH=100f/Mathf.Max(eyeUv.height,0.01f);
  layer.style.width=Length.Percent(zoomW);
  layer.style.height=Length.Percent(zoomH);
  layer.style.left=Length.Percent(-eyeUv.x/eyeUv.width*100f);
  layer.style.top=Length.Percent(-eyeUv.y/eyeUv.height*100f);
 }

 Sprite ResolveExpeditionBannerSprite(CharacterDef character){
  if(character==null)return null;
  if(character.HasPortraitAsset){
   var sprite=LoadPortraitSprite(character.portraitResource);
   if(sprite!=null)return sprite;
  }
  if(!string.IsNullOrEmpty(character.portraitFrontResource)&&!character.portraitFrontResource.Contains("/DD/")){
   var sprite=LoadPortraitSprite(character.portraitFrontResource);
   if(sprite!=null)return sprite;
  }
  return null;
 }

 Texture2D ResolveExpeditionBannerTexture(CharacterDef character){
  if(character==null)return null;
  if(character.HasPortraitAsset){
   var tex=LoadPortraitTexture(character.portraitResource);
   if(tex!=null)return tex;
  }
  if(!string.IsNullOrEmpty(character.portraitFrontResource)&&!character.portraitFrontResource.Contains("/DD/")){
   var tex=LoadPortraitTexture(character.portraitFrontResource);
   if(tex!=null)return tex;
  }
  var pop=PopDarkPortraitArt(character);
  if(pop!=null&&pop!=game.UiCharacterArt)return pop;
  var front=game.ResolveCharacterPortraitFront(character);
  if(front!=null&&front!=game.UiCharacterArt)return front;
  return null;
 }

 VisualElement BuildExpeditionBannerEyeBackground(CharacterDef character){
  var frame=Container("ps-exp-character-eye-background");
  frame.style.overflow=Overflow.Hidden;
  var eyeUv=ResolveExpeditionBannerUv(character);
  Image image;
  var sprite=ResolveExpeditionBannerSprite(character);
  var tex=ResolveExpeditionBannerTexture(character);
  if(sprite!=null)
   image=SpriteImage(sprite,new Rect(0,0,1,1),"ps-exp-character-eye-image",ScaleMode.ScaleAndCrop);
  else if(tex!=null)
   image=Image(tex,new Rect(0,0,1,1),"ps-exp-character-eye-image",ScaleMode.ScaleAndCrop);
  else
   image=Image(
    game.UiCharacterArt,
    CharacterUv(character.portraitBody,character.portraitHair),
    "ps-exp-character-eye-image",
    ScaleMode.ScaleToFit);
  image.tintColor=new Color(0.59f,0.55f,0.76f,1f);
  image.style.position=Position.Absolute;
  if(sprite!=null||tex!=null)
   PositionExpeditionBannerCrop(image,eyeUv);
  else {
   image.style.width=Length.Percent(100f);
   image.style.height=Length.Percent(100f);
  }
  frame.Add(image);
  return frame;
 }

 VisualElement BuildExpeditionCharacterBanner(CharacterDef character){
  var banner=Container("ps-exp-character-banner");
  banner.pickingMode=PickingMode.Ignore;
  banner.Add(BuildExpeditionBannerEyeBackground(character));

  var gradient=Container("ps-exp-character-banner-gradient");
  gradient.pickingMode=PickingMode.Ignore;
  banner.Add(gradient);

  var copy=Container("ps-exp-character-banner-copy");
  copy.pickingMode=PickingMode.Ignore;
  copy.Add(ExpeditionTextBlock("ps-exp-character-name",character.name));
  copy.Add(ExpeditionTextBlock("ps-exp-character-title",character.title));
  banner.Add(copy);
  return banner;
 }

 void RefreshExpeditionLoadout(MetaSave meta){
  if(expeditionLoadoutHost==null)return;
  expeditionLoadoutHost.Clear();
  expeditionLoadoutHost.Add(ChromeSection("LOADOUT","使用する荷造り"));
  expeditionLoadoutList=Container("ps-exp-loadout-list");
  foreach(var loadout in meta.loadouts){
   var entry=loadout;
   LoadoutSystem.EnsureFormulaIds(entry);
   var preview=StorageFormulaSystem.Resolve(entry);
   var button=new Button(()=>SelectExpeditionLoadout(entry.id)){userData=entry.id,tooltip=entry.name};
   button.AddToClassList("ps-exp-loadout-row");
   if(entry.id==meta.selectedLoadoutId)button.AddToClassList("ps-selected");
   var name=new Label(string.IsNullOrEmpty(entry.name)?"無名の荷造り":entry.name){pickingMode=PickingMode.Ignore};
   name.AddToClassList("ps-exp-loadout-name");
   button.Add(name);
   var sub=new Label($"{preview.core.name} · 配置 {entry.slots.Count}"){pickingMode=PickingMode.Ignore};
   sub.AddToClassList("ps-exp-loadout-sub");
   button.Add(sub);
   expeditionLoadoutList.Add(button);
  }
  expeditionLoadoutHost.Add(expeditionLoadoutList);
  expeditionLoadoutDetailHost=Container("ps-exp-loadout-detail");
  expeditionLoadoutHost.Add(expeditionLoadoutDetailHost);
  RefreshExpeditionLoadoutSummary(meta);
  var edit=PackspireUiFactory.Button("荷造りを編集",()=>game.UiOpenPackingLoadout(meta.selectedLoadoutId));
  edit.AddToClassList("ps-chrome-action");
  edit.AddToClassList("ps-exp-loadout-edit");
  expeditionLoadoutHost.Add(edit);
 }

 void RefreshExpeditionLoadoutSummary(MetaSave meta){
  if(expeditionLoadoutDetailHost==null)return;
  expeditionLoadoutDetailHost.Clear();
  var active=LoadoutSystem.Active(meta);
  var formula=StorageFormulaSystem.Resolve(active);
  expeditionLoadoutDetailHost.Add(ExpeditionTextBlock("ps-exp-loadout-detail-heading",$"選択中：{active.name}"));
  expeditionLoadoutDetailHost.Add(ExpeditionInfoRow("収納術式",formula.core.name));
  expeditionLoadoutDetailHost.Add(ExpeditionInfoRow("配置装備",$"{active.slots.Count} 件"));
  expeditionLoadoutDetailHost.Add(ExpeditionInfoRow("採用カード",$"{active.deck.Count} 枚"));
  var build=BuildExpeditionLoadoutPreview(meta,out var run);
  expeditionLoadoutDetailHost.Add(ExpeditionInfoStack("属性充足",ExpeditionColorSummary(build.colors)));
  expeditionLoadoutDetailHost.Add(ExpeditionInfoStack("発動LINK",ExpeditionLinkSummary(run,build)));
 }

 DeckBuildResult BuildExpeditionLoadoutPreview(MetaSave meta,out RunState run){
  run=LoadoutSystem.CreateRun(meta,selectedDungeonId);
  return BackpackSystem.Build(run);
 }

 string ExpeditionColorSummary(Dictionary<Element,int> colors){
  if(colors==null||colors.Count==0)return "—";
  var parts=new List<string>();
  foreach(var pair in colors)
   if(pair.Value>0)parts.Add($"{ElementLabel(pair.Key)} {pair.Value}");
  return parts.Count>0?string.Join(" · ",parts):"一致なし";
 }

 string ExpeditionLinkSummary(RunState run,DeckBuildResult build){
  var labels=new List<string>();
  var links=build.formula.resonance.links??System.Array.Empty<ResonanceLinkDef>();
  foreach(var link in links)
   if(IsLinkActiveAnywhere(run,link))labels.Add(link.label);
  var upgrades=build.formula.resonance.upgrades??System.Array.Empty<ResonanceUpgradeDef>();
  foreach(var upgrade in upgrades)
   if(IsUpgradeActiveAnywhere(run,upgrade)){
    string toName=GameCatalog.Cards.TryGetValue(upgrade.toCardId,out var card)?card.name:upgrade.toCardId;
    labels.Add($"変化→{toName}");
   }
  if(labels.Count==0)return "発動中なし";
  if(labels.Count<=2)return string.Join(" · ",labels);
  return string.Join(" · ",labels.Take(2))+$" ほか {labels.Count-2}";
 }

 void RefreshExpeditionDepart(MetaSave meta){
  if(expeditionDepartButton==null||expeditionDepartReason==null)return;
  bool unlocked=IsDungeonUnlocked(meta,selectedDungeonId);
  expeditionDepartButton.SetEnabled(unlocked);
  expeditionDepartReason.text=unlocked?"":ExpeditionUnlockHint(meta,GameCatalog.Dungeons.First(x=>x.id==selectedDungeonId));
 }
}
}
