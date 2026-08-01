using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire {
public sealed partial class PackspireUiFoundation {
 const int CompendiumEntriesPerPage=6;
 const string CompendiumLoreOverlayName="ps-codex-v10-lore-overlay";

 VisualElement compendiumViewRoot;
 VisualElement compendiumItemRecord;
 VisualElement compendiumGenericRecord;
 VisualElement compendiumItemArtHost;
 VisualElement compendiumItemShapeHost;
 VisualElement compendiumItemCardPanel;
 VisualElement compendiumItemCardStage;
 VisualElement compendiumItemLinkHost;
 Button compendiumItemTab;
 Button compendiumRoleTab;
 Button compendiumEnemyTab;
 Button compendiumCombatTab;
 Button compendiumExplorationTab;
 Label compendiumDiscoveryCount;
 Label compendiumItemMeta;
 Label compendiumItemName;
 Label compendiumItemDescription;
 Label compendiumItemShapeCount;
 Label compendiumAcquisitionSource;
 Label compendiumAcquisitionTier;

 sealed class CompendiumEntry {
  public string title;
  public string body;
  public string glyph;
  public bool unknown;

  public CompendiumEntry(string title,string body,string glyph="◆",bool unknown=false){
   this.title=title;
   this.body=body;
   this.glyph=glyph;
   this.unknown=unknown;
  }
 }

 void PrepareCompendiumRecord(string ownerId){
  if(compendiumDetailOwnerId==ownerId)return;
  compendiumDetailOwnerId=ownerId;
  compendiumDetailTab=0;
  compendiumDetailPage=0;
  compendiumCardExploration=false;
 }

 void SelectCompendiumTab(int tab){
  tab=Mathf.Clamp(tab,0,2);
  if(compendiumTab==tab)return;
  compendiumTab=tab;
  selectedCompendiumId="";
  RefreshCompendiumScreen(true);
 }

 void SelectCompendiumCardFace(bool exploration){
  if(compendiumCardExploration==exploration)return;
  compendiumCardExploration=exploration;
  RefreshCompendiumDetail(game.UiMeta);
 }

 void ShowCompendiumLorePage(){
  compendiumDetailPage=1;
  RefreshCompendiumScreen(true);
 }

 void SetCompendiumRecordMode(bool showItemRecord){
  if(compendiumViewRoot==null)return;
  compendiumViewRoot.EnableInClassList("ps-codex-show-item",showItemRecord);
  compendiumViewRoot.EnableInClassList("ps-codex-show-generic",!showItemRecord);
 }

 void UpdateCompendiumTabState(){
  compendiumItemTab?.EnableInClassList("ps-selected",compendiumTab==0);
  compendiumRoleTab?.EnableInClassList("ps-selected",compendiumTab==1);
  compendiumEnemyTab?.EnableInClassList("ps-selected",compendiumTab==2);
 }

 void PrepareCompendiumRecordViewport(){
  if(mgmtDetailScroll==null)return;
  mgmtDetailScroll.RemoveFromClassList("ps-codex-page-two");
  compendiumGenericRecord?.RemoveFromClassList("ps-codex-lore-mode");
  compendiumGenericRecord?.Q<VisualElement>(CompendiumLoreOverlayName)?.RemoveFromHierarchy();
 }

 void BuildCompendiumTwoPageRecord(
 string recordKind,
 string recordName,
 List<CompendiumEntry> facts,
 VisualElement flavorArt,
 string flavorLead,
 string flavorOrigin,
 string flavorLegacy
 ){
  if(mgmtDetailScroll==null)return;
  SetCompendiumRecordMode(false);
  PrepareCompendiumRecordViewport();
  compendiumDetailPage=Mathf.Clamp(compendiumDetailPage,0,1);
  mgmtDetailScroll.EnableInClassList("ps-codex-page-two",compendiumDetailPage==1);
  if(compendiumDetailPage==0){
   var page=Container("ps-codex-v4-facts-page");
   var heading=Container("ps-codex-v4-facts-heading");
   var eyebrow=new Label("FIELD RECORD / FIXED DATA"){pickingMode=PickingMode.Ignore};
   eyebrow.AddToClassList("ps-codex-v4-eyebrow");
   heading.Add(eyebrow);
   var title=new Label("固定記録"){pickingMode=PickingMode.Ignore};
   title.AddToClassList("ps-codex-v4-facts-title");
   heading.Add(title);
   page.Add(heading);

   var grid=Container("ps-codex-v4-facts-grid");
   foreach(var fact in facts.Take(6))grid.Add(CompendiumFact(fact));
   while(grid.childCount<6)
    grid.Add(CompendiumFact(new CompendiumEntry("記録欄","解析待ち","◇",true),true));
   page.Add(grid);
   page.Add(CompendiumPageMark(1));
   page.Add(CompendiumRecordPageArrow(true));
   mgmtDetailScroll.Add(page);
   return;
  }

  compendiumGenericRecord?.AddToClassList("ps-codex-lore-mode");
  var flavor=Container("ps-codex-v10-lore-page");
  flavor.name=CompendiumLoreOverlayName;
  var flavorHeading=Container("ps-codex-v10-lore-heading");
  var flavorEyebrow=new Label($"{recordKind} / ARCHIVE II"){pickingMode=PickingMode.Ignore};
  flavorEyebrow.AddToClassList("ps-codex-v10-lore-eyebrow");
  flavorHeading.Add(flavorEyebrow);
  var flavorTitle=new Label(recordName){pickingMode=PickingMode.Ignore};
  flavorTitle.AddToClassList("ps-codex-v10-lore-title");
  flavorHeading.Add(flavorTitle);
  var subtitle=new Label("出自と逸話"){pickingMode=PickingMode.Ignore};
  subtitle.AddToClassList("ps-codex-v10-lore-subtitle");
  flavorHeading.Add(subtitle);
  flavor.Add(flavorHeading);

  var body=Container("ps-codex-v10-lore-body");
  var copy=Container("ps-codex-v10-lore-copy");
  copy.Add(CompendiumLoreSection("記録断片",flavorLead,0));
  copy.Add(CompendiumLoreSection("出自",flavorOrigin,1));
  copy.Add(CompendiumLoreSection("伝承",flavorLegacy,2));
  body.Add(copy);
  flavor.Add(body);
  flavor.Add(CompendiumPageMark(2));
  flavor.Add(CompendiumRecordPageArrow(false));
  compendiumGenericRecord?.Add(flavor);
 }

 VisualElement CompendiumFact(CompendiumEntry entry,bool empty=false){
  var fact=Container("ps-codex-v4-fact");
  if(entry.unknown)fact.AddToClassList("ps-undiscovered");
  if(empty)fact.AddToClassList("ps-empty");
  var glyph=new Label(entry.glyph){pickingMode=PickingMode.Ignore};
  glyph.AddToClassList("ps-codex-v4-fact-glyph");
  fact.Add(glyph);
  var copy=Container("ps-codex-v4-fact-copy");
  var title=new Label(entry.title){pickingMode=PickingMode.Ignore};
  title.AddToClassList("ps-codex-v4-fact-title");
  copy.Add(title);
  var body=new Label(entry.body){pickingMode=PickingMode.Ignore};
  body.AddToClassList("ps-codex-v4-fact-body");
  copy.Add(body);
  fact.Add(copy);
  return fact;
 }

 VisualElement CompendiumLoreSection(string title,string body,int index){
  var section=Container("ps-codex-v10-lore-section");
  section.EnableInClassList("ps-last",index==2);
  var heading=new Label(title){pickingMode=PickingMode.Ignore};
  heading.AddToClassList("ps-codex-v10-lore-section-title");
  section.Add(heading);
  var text=new Label(string.IsNullOrEmpty(body)?"記録はまだ綴られていない。":body){pickingMode=PickingMode.Ignore};
  text.AddToClassList("ps-codex-v10-lore-section-body");
  section.Add(text);
  return section;
 }

 Button CompendiumRecordPageArrow(bool forward){
  var arrow=new Button(()=>{
   compendiumDetailPage=forward?1:0;
   RefreshCompendiumScreen(true);
  }){text=forward?"›":"‹"};
  arrow.AddToClassList("ps-codex-v4-page-arrow");
  arrow.AddToClassList(forward?"ps-forward":"ps-back");
  arrow.tooltip=forward?"出自と逸話を読む":"固定記録へ戻る";
  return arrow;
 }

 VisualElement CompendiumPageMark(int page){
  var mark=Container("ps-codex-v4-page-mark");
  var current=new Label(page.ToString()){pickingMode=PickingMode.Ignore};
  current.AddToClassList("ps-current");
  mark.Add(current);
  mark.Add(new Label(" / 2"){pickingMode=PickingMode.Ignore});
  return mark;
 }

 static string ItemCardSummary(ItemDef item,bool exploration){
  var names=new List<string>();
  foreach(var grant in item.grantedCards??Array.Empty<GrantedCardDef>()){
   string id=exploration?grant?.explorationCardId:grant?.battleCardId;
   if(string.IsNullOrEmpty(id))continue;
   if(exploration&&GameCatalog.ExplorationCards.TryGetValue(id,out var explore))names.Add(explore.name);
   else if(!exploration&&GameCatalog.Cards.TryGetValue(id,out var battle))names.Add(battle.name);
  }
  if(names.Count==0){
   string id=exploration?item.explorationCardId:item.cardId;
   if(exploration&&GameCatalog.ExplorationCards.TryGetValue(id??"",out var explore))names.Add(explore.name);
   else if(!exploration&&GameCatalog.Cards.TryGetValue(id??"",out var battle))names.Add(battle.name);
  }
  return names.Count==0?"登録なし":string.Join(" / ",names.Distinct());
 }

 string ItemElementSummary(ItemDef item){
  var groups=(item.cells??Array.Empty<CellDef>())
   .GroupBy(cell=>cell.element)
   .Select(group=>$"{ElementLabel(group.Key)} {group.Sum(cell=>Mathf.Max(1,cell.value))}")
   .ToArray();
  return groups.Length==0?"色特性なし":string.Join(" / ",groups);
 }

 static string ItemFixedLinkSummary(ItemDef item){
  if(!string.IsNullOrEmpty(item.linkRule))return item.linkRule;
  var tags=item.resonanceTags??Array.Empty<string>();
  return tags.Length==0?"固有LINKなし":string.Join(" / ",tags);
 }

 static string RoleContributionSummary(ReactionContributionContent[] values,string fallback){
  var summaries=(values??Array.Empty<ReactionContributionContent>())
   .Where(value=>value!=null)
   .Select(value=>$"{value.reactionId} {value.amount:+#;-#;0}{(value.perLevel?" / Lv":"")}")
   .ToArray();
  return summaries.Length==0?fallback:string.Join(" / ",summaries);
 }

 static string RoleUnlockSummary(RoleDef role){
  var recipe=(role.unlockRecipes??Array.Empty<RoleUnlockRecipeContent>()).FirstOrDefault();
  if(recipe==null)return "遠征やイベントで解放";
  if(!recipe.visibleBeforeUnlock)return "条件未発見";
  var requirements=(recipe.requirements??Array.Empty<ReactionRequirementContent>())
   .Select(value=>$"{value.reactionId} {value.minimum}")
   .ToArray();
  return requirements.Length==0
   ?(string.IsNullOrEmpty(recipe.hint)?"特殊条件":recipe.hint)
   :string.Join(" / ",requirements);
 }

 static string EnemyActionSummary(EnemyDef enemy){
  var moves=(enemy.damages??Array.Empty<int>())
   .Select((damage,index)=>damage==0?$"{index+1}: 特殊":$"{index+1}: 攻撃 {damage}")
   .ToArray();
  return moves.Length==0?"行動記録なし":string.Join(" / ",moves);
 }

 VisualElement CompendiumUnknownFlavorArt(string glyph){
  var host=Container("ps-codex-v4-unknown-art");
  var label=new Label(glyph){pickingMode=PickingMode.Ignore};
  label.AddToClassList("ps-codex-v4-unknown-glyph");
  host.Add(label);
  return host;
 }

 void BuildItemCompendiumRecord(ItemDef item){
  if(item==null||mgmtDetailScroll==null)return;
  PrepareCompendiumRecordViewport();
  compendiumDetailPage=Mathf.Clamp(compendiumDetailPage,0,1);
  if(compendiumDetailPage==1){
   BuildCompendiumTwoPageRecord(
   "EQUIPMENT ORIGIN",item.name,
   new List<CompendiumEntry>(),
   VaultItemArt(item.id,"ps-codex-v4-flavor-art-image"),
   string.IsNullOrEmpty(item.description)
     ?$"{item.name}について、遠征者が持ち帰った断片的な記録が残されている。"
     :item.description,
    $"{item.name}は塔の探索で回収された{ItemTypeLabel(item.type)}の一つ。傷や補修跡まで含めて、その来歴が図鑑へ綴られる。",
    "同じ名を持つ品であっても、手にした者と遠征の結末によって語られる逸話は少しずつ異なる。"
   );
   return;
  }

  SetCompendiumRecordMode(true);
  compendiumItemArtHost.Clear();
  compendiumItemArtHost.Add(VaultItemArt(item.id,"ps-codex-item-art-image"));
  compendiumItemMeta.text=$"RANK {item.rarity} / {ItemTypeLabel(item.type)}";
  compendiumItemName.text=item.name;
  compendiumItemDescription.text=string.IsNullOrEmpty(item.description)
   ?"固定説明はまだ記録されていない。"
   :item.description;

  var variants=CompendiumShapeVariants(item);
  compendiumItemShapeCount.text=$"{variants.Count} PATTERN{(variants.Count==1?"":"S")}";
  compendiumItemShapeHost.Clear();
  foreach(var variant in variants)compendiumItemShapeHost.Add(CompendiumNeutralShape(variant));

  compendiumItemCardPanel.EnableInClassList("ps-combat",!compendiumCardExploration);
  compendiumItemCardPanel.EnableInClassList("ps-exploration",compendiumCardExploration);
  compendiumCombatTab.EnableInClassList("ps-selected",!compendiumCardExploration);
  compendiumExplorationTab.EnableInClassList("ps-selected",compendiumCardExploration);
  compendiumItemCardStage.Clear();
  var itemInstance=new ItemInstance(item.id);
  var card=BuildEquipmentCardFacePreview(itemInstance,item,game.UiRun,compendiumCardExploration);
  if(card!=null){
   card.AddToClassList("ps-codex-item-v5-card");
   card.pickingMode=PickingMode.Position;
   card.tooltip="クリックでカードを拡大";
   card.RegisterCallback<ClickEvent>(evt=>{
    evt.StopPropagation();
    ShowVaultCardModal(itemInstance,item,compendiumCardExploration);
   });
   compendiumItemCardStage.Add(card);
  }else{
   compendiumItemCardStage.Add(PackspireUiFactory.EmptyState(
    compendiumCardExploration?"探索カードなし":"戦闘カードなし",
    $"この装備には固定の{(compendiumCardExploration?"探索":"戦闘")}カードが登録されていません。"
   ));
  }

  compendiumItemLinkHost.Clear();
  var linkPanel=VaultEffectRecord(
   "LINK効果",
   string.IsNullOrEmpty(item.linkRule)?"固有LINKなし":"装備LINK",
   ItemFixedLinkSummary(item),
   "ps-vault-v9-link-effect ps-codex-link-effect"
  );
  compendiumItemLinkHost.Add(linkPanel);
  compendiumAcquisitionSource.text=ItemAcquisitionSource(item);
  compendiumAcquisitionTier.text=$"TIER {Mathf.Max(1,item.acquisitionTier)} 以降";
 }

 static string ItemAcquisitionSource(ItemDef item)=>item.type switch{
  ItemType.Supply=>"探索・商店",
  ItemType.Rune=>"遠征報酬・特殊宝箱",
  _=>"遠征報酬・商店"
 };

 List<Vector2Int[]> CompendiumShapeVariants(ItemDef item){
  var source=item?.cells??Array.Empty<CellDef>();
  var variants=new List<Vector2Int[]>();
  var signatures=new HashSet<string>();

  void Add(params Vector2Int[] cells){
   if(cells==null||cells.Length==0)return;
   int minX=cells.Min(cell=>cell.x);
   int minY=cells.Min(cell=>cell.y);
   var normalized=cells
    .Select(cell=>new Vector2Int(cell.x-minX,cell.y-minY))
    .OrderBy(cell=>cell.y).ThenBy(cell=>cell.x)
    .ToArray();
   string signature=string.Join(";",normalized.Select(cell=>$"{cell.x},{cell.y}"));
   if(signatures.Add(signature))variants.Add(normalized);
  }

  if(source.Length==0){
   Add(new Vector2Int(0,0));
   return variants;
  }

  for(int rotation=0;rotation<4;rotation++)
   Add(BackpackSystem.Layout(item,rotation).Select(cell=>cell.pos).ToArray());
  return variants;
 }

 VisualElement CompendiumNeutralShape(Vector2Int[] occupied){
  const int gridSize=3;
  var frame=Container("ps-codex-item-v5-shape");
  var occupiedSet=new HashSet<Vector2Int>(occupied??Array.Empty<Vector2Int>());
  int width=occupiedSet.Count==0?1:occupiedSet.Max(cell=>cell.x)+1;
  int height=occupiedSet.Count==0?1:occupiedSet.Max(cell=>cell.y)+1;
  int offsetX=Mathf.Max(0,(gridSize-width)/2);
  int offsetY=Mathf.Max(0,(gridSize-height)/2);
  for(int y=0;y<gridSize;y++){
   var row=Container("ps-codex-item-v5-shape-row");
   for(int x=0;x<gridSize;x++){
    var cell=Container("ps-codex-item-v5-shape-cell");
    if(occupiedSet.Contains(new Vector2Int(x-offsetX,y-offsetY)))cell.AddToClassList("ps-filled");
    row.Add(cell);
   }
   frame.Add(row);
  }
  return frame;
 }

 void BuildCompendiumTabbedRecord(
  string[] tabs,
  Func<int,List<CompendiumEntry>> entriesForTab,
  Func<int,VisualElement> specialPage=null
 ){
  if(mgmtDetailScroll==null)return;
  compendiumDetailTab=Mathf.Clamp(compendiumDetailTab,0,Mathf.Max(0,tabs.Length-1));

  var root=Container("ps-codex-v3-record");
  var tabBar=Container("ps-codex-v3-tabs");
  for(int index=0;index<tabs.Length;index++){
   int captured=index;
   var tab=new Button(()=>{
    if(compendiumDetailTab==captured)return;
    compendiumDetailTab=captured;
    compendiumDetailPage=0;
    RefreshCompendiumDetail(game.UiMeta);
   }){text=tabs[index]};
   tab.AddToClassList("ps-codex-v3-tab");
   if(index==compendiumDetailTab)tab.AddToClassList("ps-selected");
   tabBar.Add(tab);
  }
  root.Add(tabBar);

  var special=specialPage?.Invoke(compendiumDetailTab);
  if(special!=null){
   special.AddToClassList("ps-codex-v3-page");
   root.Add(special);
   mgmtDetailScroll.Add(root);
   return;
  }

  var entries=entriesForTab?.Invoke(compendiumDetailTab)??new List<CompendiumEntry>();
  int pageCount=Mathf.Max(1,Mathf.CeilToInt(entries.Count/(float)CompendiumEntriesPerPage));
  compendiumDetailPage=Mathf.Clamp(compendiumDetailPage,0,pageCount-1);

  var page=Container("ps-codex-v3-page");
  var grid=Container("ps-codex-v3-grid");
  int start=compendiumDetailPage*CompendiumEntriesPerPage;
  for(int slot=0;slot<CompendiumEntriesPerPage;slot++){
   int entryIndex=start+slot;
   if(entryIndex<entries.Count)grid.Add(CompendiumCandidate(entries[entryIndex]));
   else grid.Add(CompendiumCandidate(new CompendiumEntry("—","記録なし","◇",true),true));
  }
  page.Add(grid);
  page.Add(CompendiumPager(pageCount));
  root.Add(page);
  mgmtDetailScroll.Add(root);
 }

 VisualElement CompendiumCandidate(CompendiumEntry entry,bool empty=false){
  var card=Container("ps-codex-v3-candidate");
  if(entry.unknown)card.AddToClassList("ps-undiscovered");
  if(empty)card.AddToClassList("ps-empty");
  var glyph=new Label(entry.glyph){pickingMode=PickingMode.Ignore};
  glyph.AddToClassList("ps-codex-v3-candidate-glyph");
  card.Add(glyph);
  var copy=Container("ps-codex-v3-candidate-copy");
  copy.Add(new Label(entry.title){pickingMode=PickingMode.Ignore});
  var body=new Label(entry.body){pickingMode=PickingMode.Ignore};
  body.AddToClassList("ps-codex-v3-candidate-body");
  copy.Add(body);
  card.Add(copy);
  return card;
 }

 VisualElement CompendiumPager(int pageCount){
  var pager=Container("ps-codex-v3-pager");
  var previous=new Button(()=>{
   if(compendiumDetailPage<=0)return;
   compendiumDetailPage--;
   RefreshCompendiumDetail(game.UiMeta);
  }){text="‹"};
  previous.AddToClassList("ps-codex-v3-page-button");
  previous.SetEnabled(compendiumDetailPage>0);
  pager.Add(previous);

  var indicators=Container("ps-codex-v3-page-indicators");
  int visible=Mathf.Min(pageCount,8);
  int first=Mathf.Clamp(compendiumDetailPage-visible/2,0,Mathf.Max(0,pageCount-visible));
  for(int index=0;index<visible;index++){
   int page=first+index;
   var indicator=new Label(page==compendiumDetailPage?"◆":"◇"){pickingMode=PickingMode.Ignore};
   indicator.AddToClassList("ps-codex-v3-page-indicator");
   if(page==compendiumDetailPage)indicator.AddToClassList("ps-selected");
   indicators.Add(indicator);
  }
  pager.Add(indicators);
  var count=new Label($"{compendiumDetailPage+1} / {pageCount}"){pickingMode=PickingMode.Ignore};
  count.AddToClassList("ps-codex-v3-page-count");
  pager.Add(count);

  var next=new Button(()=>{
   if(compendiumDetailPage>=pageCount-1)return;
   compendiumDetailPage++;
   RefreshCompendiumDetail(game.UiMeta);
  }){text="›"};
  next.AddToClassList("ps-codex-v3-page-button");
  next.SetEnabled(compendiumDetailPage<pageCount-1);
  pager.Add(next);
  return pager;
 }

 List<CompendiumEntry> ItemCompendiumEntries(ItemDef item,int tab){
  switch(tab){
   case 0:
    return new List<CompendiumEntry>{
     new("分類",ItemTypeLabel(item.type),"装"),
     new("ランク",item.rarity.ToString(),RarityGlyph(item.rarity)),
     new("入手段階",$"TIER {item.acquisitionTier}","階"),
     new("耐久",item.baseDurability.ToString(),"耐"),
     new("カード",GrantedCardCount(item).ToString(),"札"),
     new("概要",string.IsNullOrEmpty(item.description)?"記録なし":item.description,"記")
    };
   case 1:
    return ShapeEntries(item);
   case 2:
    return ElementEntries(item);
   case 3:
    return SkillEntries(item);
   case 4:
    return LinkEntries(item);
   default:
    return new List<CompendiumEntry>();
  }
 }

 List<CompendiumEntry> ShapeEntries(ItemDef item){
  var result=new List<CompendiumEntry>();
  result.Add(new CompendiumEntry("現在の形",ShapeDiagram(item.cells),$"{item.cells?.Length??0}"));
  foreach(var candidate in GameCatalog.Items.Values.Where(value=>value.type==item.type&&value.id!=item.id)){
   result.Add(new CompendiumEntry(
    candidate.name,
    ShapeDiagram(candidate.cells),
    $"{candidate.cells?.Length??0}"
   ));
  }
  return result;
 }

 List<CompendiumEntry> ElementEntries(ItemDef item){
  var result=new List<CompendiumEntry>();
  foreach(var group in (item.cells??Array.Empty<CellDef>()).GroupBy(cell=>cell.element))
   result.Add(new CompendiumEntry(ElementLabel(group.Key),$"寄与値 {group.Sum(cell=>Mathf.Max(1,cell.value))}",ElementGlyph(group.Key)));
  foreach(Element element in Enum.GetValues(typeof(Element))){
   if(result.Any(entry=>entry.title==ElementLabel(element)))continue;
   result.Add(new CompendiumEntry(ElementLabel(element),"未付与",ElementGlyph(element),true));
  }
  return result;
 }

 List<CompendiumEntry> SkillEntries(ItemDef item){
  var result=new List<CompendiumEntry>{
   new("固有効果",string.IsNullOrEmpty(item.description)?"記録なし":item.description,"技")
  };
  foreach(var grant in item.grantedCards??Array.Empty<GrantedCardDef>()){
   if(!string.IsNullOrEmpty(grant.battleCardId)&&GameCatalog.Cards.TryGetValue(grant.battleCardId,out var battle))
    result.Add(new CompendiumEntry(battle.name,battle.text,"戦"));
   if(!string.IsNullOrEmpty(grant.explorationCardId)&&GameCatalog.ExplorationCards.TryGetValue(grant.explorationCardId,out var explore))
    result.Add(new CompendiumEntry(explore.name,explore.text,"探"));
  }
  if(result.Count==1)result.Add(new CompendiumEntry("追加能力","記録なし","◇",true));
  return result;
 }

 List<CompendiumEntry> LinkEntries(ItemDef item){
  var result=new List<CompendiumEntry>{
   new("装備LINK",string.IsNullOrEmpty(item.linkRule)?"固有LINKなし":item.linkRule,"鎖",string.IsNullOrEmpty(item.linkRule))
  };
  foreach(var tag in item.resonanceTags??Array.Empty<string>())
   result.Add(new CompendiumEntry(tag,"共鳴タグ","響"));
  foreach(var contribution in item.reactionContributions??Array.Empty<ReactionContributionContent>())
   result.Add(ReactionEntry(contribution));
  return result;
 }

 VisualElement ItemCardsPage(ItemDef item){
  var page=Container("ps-codex-v3-card-page");
  var head=Container("ps-codex-v3-card-head");
  var label=new Label(compendiumCardExploration?"探索カード":"戦闘カード"){pickingMode=PickingMode.Ignore};
  label.AddToClassList("ps-codex-v3-card-label");
  head.Add(label);
  var flip=new Button(()=>{
   compendiumCardExploration=!compendiumCardExploration;
   RefreshCompendiumDetail(game.UiMeta);
  }){text=compendiumCardExploration?"戦闘面へ":"探索面へ"};
  flip.AddToClassList("ps-codex-v3-flip");
  head.Add(flip);
  page.Add(head);
  var card=BuildEquipmentCardFacePreview(new ItemInstance(item.id),item,game.UiRun,compendiumCardExploration);
  if(card!=null)page.Add(card);
  else page.Add(PackspireUiFactory.EmptyState("カード記録なし","この装備にはカード面が登録されていません。"));
  return page;
 }

 List<CompendiumEntry> RoleCompendiumEntries(RoleDef role,int level,int tab){
  switch(tab){
   case 0:
    return new List<CompendiumEntry>{
     new("系統",string.IsNullOrEmpty(role.family)?"系譜なし":role.family,"系"),
     new("分類",role.kind,"役"),
     new("習得Lv",$"{level} / {role.maxLevel}","Lv"),
     new("現在職",game.UiMeta.currentRole==role.id?"選択中":"待機中","現"),
     new("初期札",$"{role.startingCardIds?.Length??0}枚","札"),
     new("説明",string.IsNullOrEmpty(role.description)?"記録なし":role.description,"記")
    };
   case 1:
    return new List<CompendiumEntry>{
     new("Lv.7",game.UiRoleMilestone(role.id,false),"7",level<7),
     new($"Lv.{role.maxLevel}",game.UiRoleMilestone(role.id,true),"極",level<role.maxLevel),
     new("最大レベル",role.maxLevel.ToString(),"上")
    };
   case 2:
    return ContributionEntries(role.reactionContributions,"重ね効果");
   case 3:
    return ContributionEntries(role.currentRoleContributions,"現職効果");
   case 4:
    return RoleResonanceEntries(role);
   case 5:
    return RoleUnlockEntries(role);
   default:
    return new List<CompendiumEntry>();
  }
 }

 List<CompendiumEntry> ContributionEntries(ReactionContributionContent[] values,string emptyTitle){
  var result=(values??Array.Empty<ReactionContributionContent>()).Select(ReactionEntry).ToList();
  if(result.Count==0)result.Add(new CompendiumEntry(emptyTitle,"登録なし","◇",true));
  return result;
 }

 CompendiumEntry ReactionEntry(ReactionContributionContent value){
  if(value==null)return new CompendiumEntry("未記録","—","◇",true);
  string suffix=value.perLevel?" / Lv":"";
  return new CompendiumEntry(
   string.IsNullOrEmpty(value.reactionId)?"共鳴値":value.reactionId,
   $"{value.amount:+#;-#;0}{suffix}・{value.scope}",
   "響"
  );
 }

 List<CompendiumEntry> RoleResonanceEntries(RoleDef role){
  var result=new List<CompendiumEntry>();
  foreach(var contribution in role.reactionContributions??Array.Empty<ReactionContributionContent>())
   result.Add(ReactionEntry(contribution));
  foreach(var contribution in role.currentRoleContributions??Array.Empty<ReactionContributionContent>())
   result.Add(ReactionEntry(contribution));
  if(result.Count==0)result.Add(new CompendiumEntry("共鳴値","記録なし","◇",true));
  return result;
 }

 List<CompendiumEntry> RoleUnlockEntries(RoleDef role){
  var result=new List<CompendiumEntry>();
  foreach(var recipe in role.unlockRecipes??Array.Empty<RoleUnlockRecipeContent>()){
   if(recipe==null)continue;
   string title=recipe.visibleBeforeUnlock
    ?(string.IsNullOrEmpty(recipe.hint)?"解放条件":recipe.hint)
    :"？？？";
   string body=recipe.visibleBeforeUnlock
    ?string.Join(" / ",(recipe.requirements??Array.Empty<ReactionRequirementContent>())
     .Select(value=>$"{value.reactionId} {value.minimum}"))
    :"条件未発見";
   result.Add(new CompendiumEntry(title,string.IsNullOrEmpty(body)?"特殊な条件で解放":body,"鍵",!recipe.visibleBeforeUnlock));
  }
  if(result.Count==0)result.Add(new CompendiumEntry("解放経路","イベントや遠征で習得","鍵"));
  return result;
 }

 List<CompendiumEntry> EnemyCompendiumEntries(EnemyDef enemy,int tab){
  switch(tab){
   case 0:
    return new List<CompendiumEntry>{
     new("危険度",enemy.tier.ToString(),"危"),
     new("基礎HP",enemy.hp.ToString(),"♥"),
     new("索敵距離",enemy.boardSightRange.ToString(),"眼"),
     new("移動力",enemy.boardMoveSteps.ToString(),"歩"),
     new("巡回半径",enemy.boardPatrolRadius.ToString(),"巡"),
     new("発見記録","遭遇済み","✓")
    };
   case 1:
    return (enemy.damages??Array.Empty<int>()).Select((damage,index)=>
     new CompendiumEntry($"行動 {index+1}",damage==0?"特殊行動":$"攻撃 {damage}","剣")
    ).ToList();
   case 2:
    return new List<CompendiumEntry>{
     new("盤面思考",EnemyBehaviorLabel(enemy.boardBehavior),"思"),
     new("索敵",enemy.boardSightRange>4?"広域":"標準","眼"),
     new("追跡",enemy.boardMoveSteps>1?"高速":"通常","追")
    };
   case 3:
    return new List<CompendiumEntry>{
     new("耐性記録","解析データなし","盾",true),
     new("弱点記録","解析データなし","破",true)
    };
   case 4:
    return EnemyVariantEntries(enemy);
   case 5:
    return new List<CompendiumEntry>{
     new("出現域","塔の各階層","塔"),
     new("盤面行動",EnemyBehaviorLabel(enemy.boardBehavior),"盤"),
     new("遭遇状態","図鑑へ記録済み","✓")
    };
   default:
    return new List<CompendiumEntry>();
  }
 }

 List<CompendiumEntry> EnemyVariantEntries(EnemyDef enemy){
  var result=new List<CompendiumEntry>();
  foreach(var candidate in GameCatalog.Enemies.Where(value=>value.tier==enemy.tier))
   result.Add(new CompendiumEntry(candidate.name,$"危険度 {candidate.tier} / HP {candidate.hp}","変"));
  if(result.Count==0)result.Add(new CompendiumEntry("変異記録","未解析","？",true));
  return result;
 }

 static string ShapeDiagram(CellDef[] cells){
  if(cells==null||cells.Length==0)return "形状なし";
  int minX=cells.Min(cell=>cell.x);
  int maxX=cells.Max(cell=>cell.x);
  int minY=cells.Min(cell=>cell.y);
  int maxY=cells.Max(cell=>cell.y);
  var lines=new List<string>();
  for(int y=minY;y<=maxY;y++){
   var line="";
   for(int x=minX;x<=maxX;x++)
    line+=cells.Any(cell=>cell.x==x&&cell.y==y)?"■":"・";
   lines.Add(line);
  }
  return string.Join(" ",lines);
 }

 static int GrantedCardCount(ItemDef item){
  int count=(item.grantedCards??Array.Empty<GrantedCardDef>()).Sum(value=>value?.count??0);
  if(count>0)return count;
  return Mathf.Max(1,item.cardIds?.Length??0);
 }

 static string RarityGlyph(ItemRarity rarity)=>rarity switch{
  ItemRarity.Legendary=>"★",
  ItemRarity.Rare=>"◆",
  ItemRarity.Uncommon=>"◇",
  ItemRarity.Cursed=>"呪",
  _=>"○"
 };

 static string ElementGlyph(Element element)=>element switch{
  Element.Fire=>"炎",
  Element.Water=>"水",
  Element.Wind=>"風",
  Element.Earth=>"土",
  _=>"◆"
 };

 static string EnemyBehaviorLabel(EnemyBoardBehavior behavior)=>behavior switch{
  EnemyBoardBehavior.Chase=>"追跡",
  EnemyBoardBehavior.Wait=>"待機",
  _=>"巡回"
 };
}
}
