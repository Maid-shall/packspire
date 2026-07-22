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
 VisualElement expeditionTraitHost;
 VisualElement expeditionSkillHost;
 VisualElement expeditionLoadoutHost;
 VisualElement expeditionLoadoutList;
 VisualElement expeditionLoadoutDetailHost;
 VisualElement expeditionDetailColumn;
 VisualElement expeditionDetailBody;
 VisualElement expeditionDepartFooter;
 ScrollView expeditionDetailScroll;
 Button expeditionDepartButton;
 Label expeditionDepartReason;
 Label expeditionDepartLabel;
 float expeditionDestScrollY;
 float expeditionDetailScrollY;
 bool expeditionLayoutAudited;
 static readonly HashSet<string> expeditionBannerWarned=new();

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
  var header=Container("ps-mgmt-header ps-exp-header");
  header.Add(ChromeBrand("EXPEDITION  /  BRIEF","遠征準備"));
  contentHost.Add(header);

  var body=Container("ps-expedition-body");

  var destCol=Container("ps-exp-col-dest");
  var destHead=new Label("遠征先"){pickingMode=PickingMode.Ignore};
  destHead.AddToClassList("ps-typo-section");
  destHead.AddToClassList("ps-exp-dest-heading");
  destCol.Add(destHead);
  var destFrame=Container("ps-surface-outer ps-exp-dest-frame");
  expeditionDestScroll=new ScrollView(ScrollViewMode.Vertical);
  expeditionDestScroll.AddToClassList("ps-exp-dest-scroll");
  expeditionDestScroll.verticalScrollerVisibility=ScrollerVisibility.Auto;
  expeditionDestScroll.scrollOffset=new Vector2(0,expeditionDestScrollY);
  expeditionDestList=Container("ps-exp-dest-list");
  expeditionDestScroll.Add(expeditionDestList);
  destFrame.Add(expeditionDestScroll);
  AddSurfaceOuterCorners(destFrame);
  destCol.Add(destFrame);
  body.Add(destCol);

  var artCol=Container("ps-exp-col-art");
  expeditionArtHost=Container("ps-frame-focal ps-exp-art-host");
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
  var vignette=Container("ps-exp-art-vignette");
  vignette.pickingMode=PickingMode.Ignore;
  expeditionArtHost.Add(vignette);
  expeditionArtHost.Add(PackspireUiFactory.CornerDecorationHost());
  AddSurfaceOuterCorners(expeditionArtHost);
  artCol.Add(expeditionArtHost);
  body.Add(artCol);

  expeditionDetailColumn=Container("ps-exp-col-detail ps-expedition-detail-column ps-surface-quiet");
  expeditionDetailColumn.style.minHeight=0;
  expeditionDetailColumn.style.flexGrow=1;
  expeditionDetailColumn.style.flexShrink=1;
  expeditionDetailColumn.style.overflow=Overflow.Hidden;

  expeditionDetailScroll=new ScrollView(ScrollViewMode.Vertical);
  expeditionDetailScroll.AddToClassList("ps-expedition-detail-scroll");
  expeditionDetailScroll.verticalScrollerVisibility=ScrollerVisibility.Auto;
  expeditionDetailScroll.horizontalScrollerVisibility=ScrollerVisibility.Hidden;
  expeditionDetailScroll.scrollOffset=new Vector2(0,expeditionDetailScrollY);
  expeditionDetailScroll.style.flexGrow=1;
  expeditionDetailScroll.style.flexShrink=1;
  expeditionDetailScroll.style.minHeight=0;
  expeditionDetailScroll.style.overflow=Overflow.Hidden;

  expeditionDetailBody=Container("ps-expedition-detail-body");
  expeditionDungeonInfoHost=Container("ps-exp-section destination-section");
  expeditionCharacterHost=Container("ps-exp-section character-section");
  expeditionTraitHost=Container("ps-exp-section trait-section");
  expeditionSkillHost=Container("ps-exp-section skill-section");
  expeditionLoadoutHost=Container("ps-exp-section loadout-section");
  expeditionDetailBody.Add(expeditionDungeonInfoHost);
  expeditionDetailBody.Add(expeditionCharacterHost);
  expeditionDetailBody.Add(expeditionTraitHost);
  expeditionDetailBody.Add(expeditionSkillHost);
  expeditionDetailBody.Add(expeditionLoadoutHost);
  expeditionDetailScroll.Add(expeditionDetailBody);
  expeditionDetailColumn.Add(expeditionDetailScroll);

  expeditionDepartFooter=Container("ps-expedition-departure-footer");
  expeditionDepartFooter.style.flexGrow=0;
  expeditionDepartFooter.style.flexShrink=0;
  expeditionDepartReason=new Label(){pickingMode=PickingMode.Ignore};
  expeditionDepartReason.AddToClassList("ps-exp-depart-reason");
  expeditionDepartReason.AddToClassList("departure-disabled-reason");
  expeditionDepartFooter.Add(expeditionDepartReason);
  expeditionDepartButton=BuildExpeditionDeparturePrimaryButton(meta);
  expeditionDepartFooter.Add(expeditionDepartButton);
  expeditionDetailColumn.Add(expeditionDepartFooter);
  body.Add(expeditionDetailColumn);

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
  ScheduleExpeditionLayoutAudit();
}

 Button BuildExpeditionDeparturePrimaryButton(MetaSave meta){
  var button=PackspireUiFactory.PrimaryActionButton("遠征を開始",()=>{
   if(!IsDungeonUnlocked(meta,selectedDungeonId))return;
   game.UiStartExpedition(selectedDungeonId);
  });
  button.AddToClassList("ps-exp-depart-primary");
  expeditionDepartLabel=button.Q<Label>(name:"ps-pop-primary-label");
  return button;
 }

 void ScheduleExpeditionLayoutAudit(){
#if UNITY_EDITOR
  expeditionLayoutAudited=false;
  expeditionDetailColumn?.RegisterCallback<GeometryChangedEvent>(OnExpeditionDetailGeometryOnce);
#endif
}

#if UNITY_EDITOR
 void OnExpeditionDetailGeometryOnce(GeometryChangedEvent evt){
  if(expeditionLayoutAudited)return;
  if(expeditionDetailColumn==null||expeditionDetailColumn.worldBound.height<8f)return;
  expeditionLayoutAudited=true;
  expeditionDetailColumn.UnregisterCallback<GeometryChangedEvent>(OnExpeditionDetailGeometryOnce);
  AuditExpeditionLayoutBounds();
}

 void AuditExpeditionLayoutBounds(){
  Rect col=expeditionDetailColumn.worldBound;
  Rect scroll=expeditionDetailScroll!=null?expeditionDetailScroll.worldBound:default;
  Rect bodyRect=expeditionDetailBody!=null?expeditionDetailBody.worldBound:default;
  Rect dest=expeditionDungeonInfoHost!=null?expeditionDungeonInfoHost.worldBound:default;
  Rect character=expeditionCharacterHost!=null?expeditionCharacterHost.worldBound:default;
  Rect loadout=expeditionLoadoutHost!=null?expeditionLoadoutHost.worldBound:default;
  Rect footer=expeditionDepartFooter!=null?expeditionDepartFooter.worldBound:default;
  Rect button=expeditionDepartButton!=null?expeditionDepartButton.worldBound:default;
  bool footerBelowScroll=footer.yMin>=scroll.yMax-2f;
  bool buttonInFooter=button.yMin>=footer.yMin-1f&&button.yMax<=footer.yMax+1f;
  bool destInBody=IsDescendantOf(expeditionDungeonInfoHost,expeditionDetailBody);
  bool characterInBody=IsDescendantOf(expeditionCharacterHost,expeditionDetailBody);
  bool loadoutInBody=IsDescendantOf(expeditionLoadoutHost,expeditionDetailBody);
  Debug.Log(
   "[PackspireQA] Expedition layout bounds\n"+
   $" detailColumn y={col.yMin:0.#}-{col.yMax:0.#} h={col.height:0.#}\n"+
   $" detailScroll  y={scroll.yMin:0.#}-{scroll.yMax:0.#} h={scroll.height:0.#}\n"+
   $" detailBody    y={bodyRect.yMin:0.#}-{bodyRect.yMax:0.#} h={bodyRect.height:0.#}\n"+
   $" destination   y={dest.yMin:0.#}-{dest.yMax:0.#}\n"+
   $" character     y={character.yMin:0.#}-{character.yMax:0.#}\n"+
   $" loadout       y={loadout.yMin:0.#}-{loadout.yMax:0.#}\n"+
   $" footer        y={footer.yMin:0.#}-{footer.yMax:0.#}\n"+
   $" button        y={button.yMin:0.#}-{button.yMax:0.#}\n"+
   $" footerBelowScroll={footerBelowScroll} buttonInFooter={buttonInFooter}\n"+
   $" destInBody={destInBody} characterInBody={characterInBody} loadoutInBody={loadoutInBody}");
}

 static bool IsDescendantOf(VisualElement node,VisualElement ancestor){
  if(node==null||ancestor==null)return false;
  for(var cur=node;cur!=null;cur=cur.parent)
   if(cur==ancestor)return true;
  return false;
}
#endif

 void AddSurfaceOuterCorners(VisualElement host){
  if(host==null)return;
  string[] corners={
   "ps-surface-outer-corner ps-surface-outer-corner-tl",
   "ps-surface-outer-corner ps-surface-outer-corner-tr",
   "ps-surface-outer-corner ps-surface-outer-corner-bl",
   "ps-surface-outer-corner ps-surface-outer-corner-br"
  };
  foreach(var classes in corners){
   var corner=Container(classes);
   corner.pickingMode=PickingMode.Ignore;
   host.Add(corner);
  }
 }

 VisualElement SelectiveSectionHead(string english,string japanese){
  var section=Container("ps-section-head");
  if(!string.IsNullOrEmpty(english)){
   var en=new Label(english){pickingMode=PickingMode.Ignore};
   en.AddToClassList("ps-typo-eyebrow");
   section.Add(en);
  }
  var jp=new Label(japanese){pickingMode=PickingMode.Ignore};
  jp.AddToClassList("ps-typo-section");
  section.Add(jp);
  return section;
 }

 VisualElement ExpeditionInfoRow(string label,string value,bool danger=false){
  var row=Container("ps-exp-info-row");
  var labelEl=new Label(label){pickingMode=PickingMode.Ignore};
  labelEl.AddToClassList("ps-exp-info-label");
  labelEl.AddToClassList("ps-typo-secondary");
  var valueEl=new Label(value){pickingMode=PickingMode.Ignore};
  valueEl.AddToClassList("ps-exp-info-value");
  valueEl.AddToClassList("ps-typo-value");
  if(danger)valueEl.AddToClassList("ps-typo-value-danger");
  row.Add(labelEl);
  row.Add(valueEl);
  return row;
 }

 VisualElement ExpeditionDifficultyRow(DungeonDef dungeon){
  var row=Container("ps-exp-info-row");
  var labelEl=new Label("推奨難度"){pickingMode=PickingMode.Ignore};
  labelEl.AddToClassList("ps-exp-info-label");
  labelEl.AddToClassList("ps-typo-secondary");
  row.Add(labelEl);
  var pips=Container("ps-exp-diff-pips");
  int filled=DungeonDifficultyPips(dungeon);
  for(int i=0;i<5;i++){
   var pip=new Label(i<filled?"◆":"◇"){pickingMode=PickingMode.Ignore};
   pip.AddToClassList("ps-exp-diff-pip");
   if(i<filled)pip.AddToClassList("ps-exp-diff-pip-on");
   pips.Add(pip);
  }
  row.Add(pips);
  return row;
 }

 int DungeonDifficultyPips(DungeonDef dungeon){
  if(dungeon.hpScale>=1.5f)return 4;
  if(dungeon.hpScale>=1.2f)return 3;
  return 2;
 }

 VisualElement ExpeditionTextBlock(string className,string text){
  var block=new Label(text){pickingMode=PickingMode.Ignore};
  foreach(var value in className.Split(' '))
   if(!string.IsNullOrEmpty(value))block.AddToClassList(value);
  return block;
 }

 VisualElement ExpeditionInfoStack(string label,string value){
  var block=Container("ps-exp-info-stack");
  var labelEl=new Label(label){pickingMode=PickingMode.Ignore};
  labelEl.AddToClassList("ps-exp-info-stack-label");
  labelEl.AddToClassList("ps-typo-secondary");
  var valueEl=new Label(value){pickingMode=PickingMode.Ignore};
  valueEl.AddToClassList("ps-exp-info-stack-value");
  valueEl.AddToClassList("ps-typo-body");
  block.Add(labelEl);
  block.Add(valueEl);
  return block;
 }

 void EnsureExpeditionDungeonSelection(MetaSave meta){
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

 bool ExpeditionDungeonVisible(MetaSave meta,string dungeonId)=>DungeonIndex(dungeonId)>=0;

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
   var thumb=Atlas(game.UiDungeonArt,DungeonUv(dungeon.id),"ps-exp-dest-thumb");
   PackspireUiFactory.StateBadgeKind? badge=null;
   if(!unlocked)badge=PackspireUiFactory.StateBadgeKind.Locked;
   var row=PackspireUiFactory.ListRow(
    dungeon.id,
    unlocked?dungeon.name:"封印された地図",
    ExpeditionDestSubline(dungeon,unlocked),
    dungeon.id==selectedDungeonId,
    ()=>{
     if(!IsDungeonUnlocked(meta,dungeon.id))return;
     SelectExpeditionDungeon(dungeon.id);
    },
    thumb,
    badge);
   row.AddToClassList("ps-exp-dest-row");
   if(!unlocked){
    row.AddToClassList("ps-locked");
    row.AddToClassList("ps-exp-dest-locked");
    row.SetEnabled(false);
   }
   if(previewLocked)row.AddToClassList("ps-exp-dest-preview");
   expeditionDestList.Add(row);
  }
 }

 string ExpeditionDestSubline(DungeonDef dungeon,bool unlocked){
  if(!unlocked)return "到達前";
  return DungeonDifficultyLabel(dungeon);
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
  var art=Atlas(game.UiDungeonArt,DungeonUv(dungeon.id),"ps-exp-art-image");
  if(art is Image artImage)artImage.scaleMode=ScaleMode.ScaleAndCrop;
  expeditionArtImageHost.Add(art);
  expeditionArtCaptionName.text=dungeon.name;
  expeditionArtCaptionSub.text=dungeon.description.Length>36?dungeon.description.Substring(0,36)+"…":dungeon.description;
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
   var hint=ExpeditionTextBlock("ps-typo-secondary",ExpeditionUnlockHint(meta,dungeon));
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
  expeditionDungeonInfoHost.Add(SelectiveSectionHead("DESTINATION","ダンジョン情報"));
  expeditionDungeonInfoHost.Add(ExpeditionTextBlock("ps-exp-dungeon-name",dungeon.name));
  expeditionDungeonInfoHost.Add(ExpeditionTextBlock("ps-exp-dungeon-desc ps-typo-body",dungeon.description));
  expeditionDungeonInfoHost.Add(ExpeditionDifficultyRow(dungeon));
  expeditionDungeonInfoHost.Add(ExpeditionInfoRow("敵HP倍率",$"{Mathf.RoundToInt(dungeon.hpScale*100f)}%",true));
  if(dungeon.damage>0)
   expeditionDungeonInfoHost.Add(ExpeditionInfoRow("敵追加攻撃",$"+{dungeon.damage}",true));
  expeditionDungeonInfoHost.Add(ExpeditionInfoRow("報酬倍率",$"{Mathf.RoundToInt(dungeon.goldScale*100f)}%"));
  expeditionDungeonInfoHost.Add(ExpeditionInfoRow("探索規模",ExpeditionScaleLabel(dungeon),dungeon.battles>=4));
 }

 string ExpeditionScaleLabel(DungeonDef dungeon){
  if(dungeon.battles>=5)return "特大";
  if(dungeon.battles>=4)return "大";
  if(dungeon.battles>=3)return "中";
  return "小";
 }

 void RefreshExpeditionCharacter(MetaSave meta){
  if(expeditionCharacterHost==null)return;
  expeditionCharacterHost.Clear();
  var character=CharacterCatalog.Get(meta.selectedCharacterId);
  expeditionCharacterHost.Add(SelectiveSectionHead("PARTY","出撃キャラクター"));
  expeditionCharacterHost.Add(BuildExpeditionCharacterBanner(character));
  if(expeditionTraitHost!=null){
   expeditionTraitHost.Clear();
   expeditionTraitHost.Add(ExpeditionInfoStack("特性",character.traitText));
  }
  if(expeditionSkillHost!=null){
   expeditionSkillHost.Clear();
   expeditionSkillHost.Add(ExpeditionInfoStack("スキル",character.activeSkillText));
   var skillRow=Container("ps-exp-skill-icons");
   skillRow.Add(ExpeditionSkillIcon(character.activeSkillName,character.activeSkillText));
   expeditionSkillHost.Add(skillRow);
  }
 }

 VisualElement ExpeditionSkillIcon(string name,string detail){
  var icon=new Button(()=>{}){tooltip=string.IsNullOrEmpty(detail)?name:$"{name}\n{detail}"};
  icon.AddToClassList("ps-exp-skill-icon");
  icon.focusable=false;
  var glyph=new Label(string.IsNullOrEmpty(name)?"技":name.Substring(0,1)){pickingMode=PickingMode.Ignore};
  glyph.AddToClassList("ps-exp-skill-icon-glyph");
  icon.Add(glyph);
  return icon;
 }

 void WarnExpeditionBannerOnce(CharacterDef character,string reason){
  string id=character?.id??"?";
  if(!expeditionBannerWarned.Add(id))return;
  Debug.LogWarning($"Packspire expedition banner fallback for '{id}': {reason}");
 }

 // Prefer dedicated portraitResource Sprite (e.g. Sena kick). Never assign sprite.texture to Image.image.
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
  return null;
 }

 static void ApplyExpeditionPortraitFocus(Image image,VisualElement clip,CharacterDef character,float sourceW,float sourceH){
  if(image==null||clip==null||character==null)return;
  float clipW=clip.resolvedStyle.width;
  float clipH=clip.resolvedStyle.height;
  if(clipW<2f||clipH<2f||sourceW<1f||sourceH<1f)return;
  float zoom=Mathf.Max(1.1f,character.portraitZoom);
  float cover=zoom*Mathf.Max(clipW/sourceW,clipH/sourceH);
  float imgW=sourceW*cover;
  float imgH=sourceH*cover;
  float focusX=Mathf.Clamp01(character.portraitFocusX);
  float focusY=Mathf.Clamp01(character.portraitFocusY);
  float left=clipW*0.5f-imgW*focusX+character.portraitBannerOffsetX*clipW;
  float top=clipH*0.5f-imgH*focusY+character.portraitBannerOffsetY*clipH;
  image.style.position=Position.Absolute;
  image.style.width=imgW;
  image.style.height=imgH;
  image.style.left=left;
  image.style.top=top;
  image.style.right=StyleKeyword.Auto;
  image.style.bottom=StyleKeyword.Auto;
 }

 VisualElement BuildExpeditionBannerArt(CharacterDef character){
  var clip=Container("character-art-clip ps-exp-character-art-clip");
  clip.style.overflow=Overflow.Hidden;
  Image image=null;
  float sourceW=1f;
  float sourceH=1f;
  var sprite=ResolveExpeditionBannerSprite(character);
  if(sprite!=null){
   // Persistent Sprite only — never sprite.texture on Image.image.
   image=SpriteImage(sprite,new Rect(0,0,1,1),"character-art-image ps-exp-character-art-image",ScaleMode.StretchToFill);
   sourceW=Mathf.Max(1f,sprite.rect.width);
   sourceH=Mathf.Max(1f,sprite.rect.height);
  } else {
   var tex=ResolveExpeditionBannerTexture(character);
   if(tex!=null){
    image=Image(tex,new Rect(0,0,1,1),"character-art-image ps-exp-character-art-image",ScaleMode.StretchToFill);
    sourceW=Mathf.Max(1f,tex.width);
    sourceH=Mathf.Max(1f,tex.height);
   } else if(game.UiCharacterArt!=null){
    WarnExpeditionBannerOnce(character,"using character atlas upper-body crop");
    image=Image(
     game.UiCharacterArt,
     CharacterUv(character.portraitBody,character.portraitHair),
     "character-art-image ps-exp-character-art-image",
     ScaleMode.StretchToFill);
    var uv=CharacterUv(character.portraitBody,character.portraitHair);
    sourceW=Mathf.Max(1f,game.UiCharacterArt.width*uv.width);
    sourceH=Mathf.Max(1f,game.UiCharacterArt.height*uv.height);
   } else {
    WarnExpeditionBannerOnce(character,"no portrait asset; quiet dark banner");
   }
  }
  if(image!=null){
   image.tintColor=new Color(0.94f,0.92f,0.95f,1f);
   image.pickingMode=PickingMode.Ignore;
   clip.Add(image);
   void LayoutFocus(GeometryChangedEvent _){
    ApplyExpeditionPortraitFocus(image,clip,character,sourceW,sourceH);
   }
   clip.RegisterCallback<GeometryChangedEvent>(LayoutFocus);
   image.schedule.Execute(()=>ApplyExpeditionPortraitFocus(image,clip,character,sourceW,sourceH)).ExecuteLater(0);
  }
  return clip;
 }

 VisualElement BuildExpeditionCharacterBanner(CharacterDef character){
  var art=BuildExpeditionBannerArt(character);
  var framed=PackspireUiFactory.PortraitFrame(PackspireUiFactory.PortraitFrameTier.Wide,art);
  framed.AddToClassList("ps-exp-character-portrait-frame");

  var banner=Container("character-banner ps-exp-character-banner");
  banner.pickingMode=PickingMode.Ignore;
  banner.Add(framed);

  var veilBottom=Container("ps-exp-character-veil-bottom");
  veilBottom.pickingMode=PickingMode.Ignore;
  banner.Add(veilBottom);

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
  expeditionLoadoutHost.Add(SelectiveSectionHead("LOADOUT","使用する荷造り"));
  expeditionLoadoutList=Container("ps-exp-loadout-list");
  foreach(var loadout in meta.loadouts){
   var entry=loadout;
   LoadoutSystem.EnsureFormulaIds(entry);
   var preview=StorageFormulaSystem.Resolve(entry);
   var button=PackspireUiFactory.ListRow(
    entry.id,
    string.IsNullOrEmpty(entry.name)?"無名の荷造り":entry.name,
    $"{preview.core.name} · 配置 {entry.slots.Count}",
    entry.id==meta.selectedLoadoutId,
    ()=>SelectExpeditionLoadout(entry.id),
    null,
    entry.id==meta.selectedLoadoutId?PackspireUiFactory.StateBadgeKind.Selected:null);
   button.AddToClassList("ps-exp-loadout-row");
   expeditionLoadoutList.Add(button);
  }
  expeditionLoadoutHost.Add(expeditionLoadoutList);
  expeditionLoadoutDetailHost=Container("ps-exp-loadout-detail");
  expeditionLoadoutHost.Add(expeditionLoadoutDetailHost);
  RefreshExpeditionLoadoutSummary(meta);
  var edit=PackspireUiFactory.SecondaryActionButton("荷造りを編集",()=>game.UiOpenPackingLoadout(meta.selectedLoadoutId));
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
