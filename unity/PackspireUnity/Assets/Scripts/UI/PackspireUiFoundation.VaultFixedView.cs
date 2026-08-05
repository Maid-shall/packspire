using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire {
public sealed partial class PackspireUiFoundation {
 VisualElement vaultFixedRoot;
 VisualElement vaultFixedStandardDetail;
 VisualElement vaultFixedEmptyDetail;
 VisualElement vaultFixedArtStage;
 VisualElement vaultFixedInfoStage;
 VisualElement vaultFixedCardStage;
 VisualElement vaultFixedCardHost;
 Label vaultFixedCardTitle;
 Button vaultFixedCardFlip;
 Label vaultFixedCardCombatLabel;
 Label vaultFixedCardExplorationLabel;
 VisualElement vaultFixedColorEffect;
 VisualElement vaultFixedLinkEffect;
 VisualElement vaultFixedActions;
 ScrollView vaultFixedHeirloomDetail;
 Button vaultFixedBackButton;
 VisualElement vaultFixedSortMenu;
 Button vaultFixedSortButton;
 Label vaultFixedSortValue;
 Label vaultFixedSortArrow;
 Label vaultFixedCount;
 readonly List<Button> vaultFixedSortOptions=new();
 readonly List<Label> vaultFixedSortOptionMarks=new();
 readonly List<Label> vaultFixedSortOptionLabels=new();
 bool vaultFixedSortMenuOpen;

 VisualElement BuildVaultFixedView(string eyebrow,string title){
  var shell=CloneView("UI/PackspireVaultView","ps-vault-fixed");
  if(shell==null)
   throw new InvalidOperationException("Vault view template is missing.");
  vaultFixedRoot=shell;

  RequireViewElement<VisualElement>(shell,"vault-background");

  var backHost=RequireViewElement<VisualElement>(shell,"vault-back-host");
  vaultFixedBackButton=PackspireUiFactory.Button("",NavGoBack);
  vaultFixedBackButton.AddToClassList("ps-vault-fixed__back");
  vaultFixedBackButton.tooltip="前の画面へ戻る";
  vaultFixedBackButton.SetEnabled(navBackStack.Count>0);
  vaultFixedBackButton.Add(PackspireUiFactory.ManagementArt(
   PackspireUiFactory.ManagementChrome.Back,
   "ps-vault-fixed__back-icon"
  ));
  var backLabel=new Label("戻る"){pickingMode=PickingMode.Ignore};
  backLabel.AddToClassList("ps-vault-fixed__back-label");
  vaultFixedBackButton.Add(backLabel);
  backHost.Add(vaultFixedBackButton);

  RequireViewElement<VisualElement>(shell,"vault-brand-host");
  RequireViewElement<Label>(shell,"vault-header-eyebrow").text=eyebrow;
  RequireViewElement<Label>(shell,"vault-header-title").text=title;

  mgmtListHeader=shell.Q<VisualElement>("vault-list-header");
  mgmtListScroll=shell.Q<ScrollView>("vault-list-scroll");
  mgmtVaultGrid=shell.Q<VisualElement>("vault-grid");
  mgmtVaultFooter=shell.Q<VisualElement>("vault-footer");
  vaultFixedSortMenu=RequireViewElement<VisualElement>(shell,"vault-sort-menu");
  vaultFixedSortButton=RequireViewElement<Button>(shell,"vault-sort-button");
  vaultFixedSortValue=RequireViewElement<Label>(shell,"vault-sort-value");
  vaultFixedSortArrow=RequireViewElement<Label>(shell,"vault-sort-arrow");
  vaultFixedCount=RequireViewElement<Label>(shell,"vault-count");
  vaultFixedSortButton.clicked+=ToggleVaultSortMenu;
  vaultFixedSortOptions.Clear();
  vaultFixedSortOptionMarks.Clear();
  vaultFixedSortOptionLabels.Clear();
  for(int index=0;index<VaultSortChoices.Count;index++){
   int selectedIndex=index;
   var option=RequireViewElement<Button>(shell,$"vault-sort-option-{index}");
   option.clicked+=()=>{
    vaultSortMode=selectedIndex;
    CloseVaultSortMenu();
    RefreshVaultScreen(true);
   };
   vaultFixedSortOptions.Add(option);
   vaultFixedSortOptionMarks.Add(RequireViewElement<Label>(shell,$"vault-sort-option-mark-{index}"));
   vaultFixedSortOptionLabels.Add(RequireViewElement<Label>(shell,$"vault-sort-option-label-{index}"));
  }
  UpdateVaultSortMenuSelection();
  CloseVaultSortMenu();
  mgmtListScroll.pickingMode=PickingMode.Position;
  mgmtListScroll.verticalScrollerVisibility=ScrollerVisibility.Auto;
  mgmtListScroll.horizontalScrollerVisibility=ScrollerVisibility.Hidden;
  StretchMgmtScrollContent(mgmtListScroll);

  vaultFixedStandardDetail=shell.Q<VisualElement>("vault-standard-detail");
  vaultFixedEmptyDetail=shell.Q<VisualElement>("vault-empty-detail");
  vaultFixedArtStage=shell.Q<VisualElement>("vault-art-stage");
  vaultFixedInfoStage=shell.Q<VisualElement>("vault-info-stage");
  vaultFixedCardStage=RequireViewElement<VisualElement>(shell,"vault-card-stage");
  vaultFixedCardHost=RequireViewElement<VisualElement>(shell,"vault-card-host");
  vaultFixedCardTitle=RequireViewElement<Label>(shell,"vault-card-title");
  vaultFixedCardFlip=RequireViewElement<Button>(shell,"vault-card-flip");
  vaultFixedCardCombatLabel=RequireViewElement<Label>(shell,"vault-card-combat-label");
  vaultFixedCardExplorationLabel=RequireViewElement<Label>(shell,"vault-card-exploration-label");
  vaultFixedCardFlip.clicked+=()=>{
   vaultCardExploration=!vaultCardExploration;
   RefreshVaultFixedDetail(game.UiMeta);
  };
  vaultFixedCardHost.RegisterCallback<ClickEvent>(evt=>{
   var selected=game.UiMeta.stash.FirstOrDefault(item=>item.uid==selectedVaultUid);
   if(selected==null||!GameCatalog.Items.TryGetValue(selected.templateId,out var definition))return;
   evt.StopPropagation();
   ShowVaultCardModal(selected,definition,vaultCardExploration);
  });
  vaultFixedColorEffect=shell.Q<VisualElement>("vault-color-effect");
  vaultFixedLinkEffect=shell.Q<VisualElement>("vault-link-effect");
  vaultFixedActions=shell.Q<VisualElement>("vault-actions");
  vaultFixedHeirloomDetail=shell.Q<ScrollView>("vault-heirloom-detail");
  vaultFixedHeirloomDetail.verticalScrollerVisibility=ScrollerVisibility.Hidden;
  vaultFixedHeirloomDetail.horizontalScrollerVisibility=ScrollerVisibility.Hidden;
  StretchMgmtScrollContent(vaultFixedHeirloomDetail,false);

  mgmtDetailHero=vaultFixedStandardDetail;
  mgmtDetailArtHost=null;
  mgmtDetailSummaryHost=null;
  mgmtDetailScroll=vaultFixedHeirloomDetail;
  return shell;
 }

 void ClearVaultFixedViewFields(){
  vaultFixedRoot=null;
  vaultFixedStandardDetail=null;
  vaultFixedEmptyDetail=null;
  vaultFixedArtStage=null;
  vaultFixedInfoStage=null;
  vaultFixedCardStage=null;
  vaultFixedCardHost=null;
  vaultFixedCardTitle=null;
  vaultFixedCardFlip=null;
  vaultFixedCardCombatLabel=null;
  vaultFixedCardExplorationLabel=null;
  vaultFixedColorEffect=null;
  vaultFixedLinkEffect=null;
  vaultFixedActions=null;
  vaultFixedHeirloomDetail=null;
  vaultFixedBackButton=null;
  vaultFixedSortMenu=null;
  vaultFixedSortButton=null;
  vaultFixedSortValue=null;
  vaultFixedSortArrow=null;
  vaultFixedCount=null;
  vaultFixedSortOptions.Clear();
  vaultFixedSortOptionMarks.Clear();
  vaultFixedSortOptionLabels.Clear();
  vaultFixedSortMenuOpen=false;
 }

 void ToggleVaultSortMenu(){
  if(vaultFixedSortMenuOpen){
   CloseVaultSortMenu();
   return;
  }
  OpenVaultSortMenu();
 }

 void OpenVaultSortMenu(){
  if(vaultFixedSortMenu==null)return;
  vaultFixedSortMenuOpen=true;
  UpdateVaultSortMenuSelection();
  vaultFixedSortMenu.AddToClassList("ps-open");
  vaultFixedSortButton?.AddToClassList("ps-open");
  if(vaultFixedSortArrow!=null)vaultFixedSortArrow.text="▲";
  vaultFixedSortMenu.BringToFront();
 }

 void CloseVaultSortMenu(){
  vaultFixedSortMenuOpen=false;
  vaultFixedSortMenu?.RemoveFromClassList("ps-open");
  vaultFixedSortButton?.RemoveFromClassList("ps-open");
  if(vaultFixedSortArrow!=null)vaultFixedSortArrow.text="▼";
 }

 void UpdateVaultSortMenuSelection(){
  vaultSortMode=Mathf.Clamp(vaultSortMode,0,VaultSortChoices.Count-1);
  if(vaultFixedSortValue!=null)vaultFixedSortValue.text=VaultSortChoices[vaultSortMode];
  for(int index=0;index<vaultFixedSortOptions.Count;index++){
   bool selected=index==vaultSortMode;
   vaultFixedSortOptions[index].EnableInClassList("ps-selected",selected);
   vaultFixedSortOptions[index].tooltip=VaultSortChoices[index];
   vaultFixedSortOptionMarks[index].text=selected?"◆":"◇";
   vaultFixedSortOptionLabels[index].text=VaultSortChoices[index];
  }
 }

 void RefreshVaultFixedDetail(MetaSave meta){
  if(vaultFixedRoot==null)return;
  vaultFixedArtStage.Clear();
  vaultFixedInfoStage.Clear();
  vaultFixedCardHost.Clear();
  vaultFixedColorEffect.Clear();
  vaultFixedLinkEffect.Clear();
  vaultFixedActions.Clear();
  vaultFixedEmptyDetail.Clear();
  vaultFixedHeirloomDetail.Clear();

  var stash=CurrentVaultStash(meta);
  var selected=meta.stash.FirstOrDefault(item=>item.uid==selectedVaultUid);
  if(selected==null||!stash.Any(item=>item.uid==selectedVaultUid)){
   vaultFixedStandardDetail.style.display=DisplayStyle.None;
   vaultFixedHeirloomDetail.style.display=DisplayStyle.None;
   vaultFixedEmptyDetail.style.display=DisplayStyle.Flex;
   vaultFixedEmptyDetail.Add(PackspireUiFactory.EmptyState(
    "装備を選択",
    "左の装備棚から記録を選んでください。"
   ));
   return;
  }

  var def=GameCatalog.Items[selected.templateId];
  bool heirloom=IsVaultHeirloom(meta,selected);
  if(!heirloom)vaultRecordPage=0;
  if(vaultRecordPage==1&&heirloom){
   vaultFixedStandardDetail.style.display=DisplayStyle.None;
   vaultFixedEmptyDetail.style.display=DisplayStyle.None;
   vaultFixedHeirloomDetail.style.display=DisplayStyle.Flex;
   vaultFixedHeirloomDetail.Add(BuildVaultHeirloomPage(selected,def));
   return;
  }

  vaultFixedStandardDetail.style.display=DisplayStyle.Flex;
  vaultFixedEmptyDetail.style.display=DisplayStyle.None;
  vaultFixedHeirloomDetail.style.display=DisplayStyle.None;
  PopulateVaultFixedArt(selected,def);
  PopulateVaultFixedInfo(meta,selected,def,heirloom);
  PopulateVaultFixedCard(selected,def);
  PopulateVaultFixedEffects(meta,selected,def,heirloom);
 }

 void PopulateVaultFixedArt(ItemInstance selected,ItemDef def){
  vaultFixedArtStage.Add(VaultItemDisplayArt(def.id,"ps-vault-fixed__hero-art"));
  var durability=PackspireUiFactory.Body($"耐久  {selected.durability} / {def.baseDurability}");
  durability.AddToClassList("ps-vault-fixed__durability");
  vaultFixedArtStage.Add(durability);
 }

 void PopulateVaultFixedInfo(MetaSave meta,ItemInstance selected,ItemDef def,bool heirloom){
  var metaLine=PackspireUiFactory.Body($"RANK {Mathf.Max(1,selected.temper+1)}  /  {ItemTypeLabel(def.type)}");
  metaLine.AddToClassList("ps-vault-fixed__meta");
  vaultFixedInfoStage.Add(metaLine);

  var nameRow=Container("ps-vault-fixed__name-row");
  var name=PackspireUiFactory.Title(def.name);
  name.AddToClassList("ps-vault-fixed__name");
  nameRow.Add(name);
  if(heirloom)nameRow.Add(HeirloomMark());
  if(VaultItemInLoadout(meta,selected.uid)){
   var inUse=Container("ps-seal-mark ps-vault-v9-inuse");
   inUse.Add(new Label("使用中"){pickingMode=PickingMode.Ignore});
   nameRow.Add(inUse);
  }
  vaultFixedInfoStage.Add(nameRow);

  var description=PackspireUiFactory.Body(def.description);
  description.AddToClassList("ps-vault-fixed__description");
  vaultFixedInfoStage.Add(description);
  vaultFixedInfoStage.Add(BuildVaultFixedShape(selected,def));
 }

 VisualElement BuildVaultFixedShape(ItemInstance item,ItemDef def){
  var block=Container("ps-vault-fixed__shape-block");
  var heading=Container("ps-vault-fixed__shape-heading");
  var title=new Label("占有形状"){pickingMode=PickingMode.Ignore};
  title.AddToClassList("ps-vault-fixed__shape-title");
  heading.Add(title);
  var count=new Label("1 PATTERN"){pickingMode=PickingMode.Ignore};
  count.AddToClassList("ps-vault-fixed__shape-count");
  heading.Add(count);
  block.Add(heading);

  var row=Container("ps-vault-fixed__shape-row");
  var layout=def?.cells==null||def.cells.Length==0
   ?new List<(Vector2Int pos,int original,Element element,int value)>()
   :BackpackSystem.Layout(def,0,item);
  row.Add(BuildVaultFixedShapeGrid(layout));
  block.Add(row);
  return block;
 }

 VisualElement BuildVaultFixedShapeGrid(List<(Vector2Int pos,int original,Element element,int value)> layout){
  const int gridSize=3;
  var grid=Container("ps-vault-fixed__shape-grid");
  layout??=new List<(Vector2Int pos,int original,Element element,int value)>();
  int minX=layout.Count==0?0:layout.Min(cell=>cell.pos.x);
  int minY=layout.Count==0?0:layout.Min(cell=>cell.pos.y);
  int width=layout.Count==0?1:layout.Max(cell=>cell.pos.x)-minX+1;
  int height=layout.Count==0?1:layout.Max(cell=>cell.pos.y)-minY+1;
  int offsetX=Mathf.Max(0,(gridSize-width)/2);
  int offsetY=Mathf.Max(0,(gridSize-height)/2);
  for(int y=0;y<gridSize;y++){
   var row=Container("ps-vault-fixed__shape-grid-row");
   for(int x=0;x<gridSize;x++){
    var cell=Container("ps-vault-fixed__shape-cell");
    int sourceX=x-offsetX+minX;
    int sourceY=y-offsetY+minY;
    var occupied=layout.FirstOrDefault(value=>value.pos.x==sourceX&&value.pos.y==sourceY);
    bool filled=layout.Any(value=>value.pos.x==sourceX&&value.pos.y==sourceY);
    if(filled){
     cell.AddToClassList("ps-filled");
     cell.AddToClassList("ps-element-"+occupied.element.ToString().ToLowerInvariant());
    }
    row.Add(cell);
   }
   grid.Add(row);
  }
  return grid;
 }

 void PopulateVaultFixedCard(ItemInstance selected,ItemDef def){
  vaultFixedCardTitle.text=vaultCardExploration?"探索カード":"戦闘カード";
  vaultFixedCardCombatLabel.EnableInClassList("ps-selected",!vaultCardExploration);
  vaultFixedCardExplorationLabel.EnableInClassList("ps-selected",vaultCardExploration);
  var card=BuildEquipmentCardElement(selected,def,game.UiRun,vaultCardExploration);
  if(card==null){
   vaultFixedCardHost.Add(PackspireUiFactory.EmptyState(
    "カードなし",
    "この装備に対応するカードはありません。"
   ));
   return;
  }
  vaultFixedCardHost.tooltip="クリックでカードを拡大";
  vaultFixedCardHost.Add(card);
 }

 void PopulateVaultFixedEffects(MetaSave meta,ItemInstance selected,ItemDef def,bool heirloom){
  var trait=StorageFormulaCatalog.Trait(selected.traitId);
  string colorName=trait?.name??VaultColorEffectName(selected);
  string colorBody=trait!=null?TraitEffectLabel(trait):VaultColorEffectBody(selected);
  vaultFixedColorEffect.Add(VaultEffectRecord(
   "色効果",
   colorName,
   colorBody,
   "ps-vault-fixed__effect ps-vault-fixed__effect--color"
  ));
  vaultFixedLinkEffect.Add(VaultEffectRecord(
   "LINK効果",
   string.IsNullOrEmpty(def.linkRule)?"固定LINKなし":"装備LINK",
   string.IsNullOrEmpty(def.linkRule)?"隣接による追加効果はありません。":def.linkRule,
   "ps-vault-fixed__effect ps-vault-fixed__effect--link"
  ));

  var lockButton=VaultLockAction(selected.insured,()=>{
   selected.insured=!selected.insured;
   SaveSystem.Save(meta);
   UpdateMgmtVaultGridBadges(meta);
   RefreshVaultFixedDetail(meta);
  });
  vaultFixedActions.Add(lockButton);

  if(heirloom){
   var recordButton=VaultPageArrow(true,()=>{
    vaultRecordPage=1;
    RefreshVaultFixedDetail(meta);
   });
   recordButton.AddToClassList("ps-vault-v16-next-item");
   recordButton.tooltip="家宝の記録を見る";
   vaultFixedActions.Add(recordButton);
  }
 }
}
}
