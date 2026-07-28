using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire {
public sealed partial class PackspireUiFoundation {
 void BuildStatus(){
  var meta=game.UiMeta;
  var character=CharacterCatalog.Get(meta.selectedCharacterId);
  var allLearned=meta.jobLevels.Where(x=>x.value>0&&GameCatalog.Roles.ContainsKey(x.id)).ToList();
  var learned=FilteredStatusRoles(allLearned);
  if(learned.Count>0){
   if(string.IsNullOrEmpty(selectedRoleId)||!learned.Any(x=>x.id==selectedRoleId))
    selectedRoleId=learned.FirstOrDefault(x=>x.id==meta.currentRole)?.id??learned[0].id;
  }else selectedRoleId="";

  var shell=BuildManagementShell("STATUS  /  RANK","役職記録",ManagementLayout.StatusOverview,out var _,out _);
  screenRoot.Add(shell);

  ClearMgmtOverview();
  mgmtOverviewHost.Add(ManagementCharacterOverview(character,meta));

  PopulateStatusHeader();
  PopulateStatusList(learned,meta);
  RefreshStatusDetail(character,meta,learned);
}

 void PopulateStatusHeader(){
  if(mgmtListHeader==null)return;
  mgmtListHeader.Clear();
  mgmtListHeader.Add(ManagementFilterBar(
   new[]{"基本","上級","複合","勢力","隠し"},
   statusRoleFilter,
   index=>{
    if(statusRoleFilter==index)return;
    statusRoleFilter=index;
    selectedRoleId="";
    RefreshStatusScreen();
   }
  ));
  mgmtListHeader.Add(SelectiveSectionHead("ROLES","習得役職"));
 }

 System.Collections.Generic.List<IdInt> FilteredStatusRoles(System.Collections.Generic.List<IdInt> learned){
  if(statusRoleFilter<0||statusRoleFilter>4)statusRoleFilter=0;
  return learned.Where(level=>RoleMatchesStatusFilter(GameCatalog.Roles[level.id],statusRoleFilter)).ToList();
 }

 static bool RoleMatchesStatusFilter(RoleDef role,int filter){
  if(role==null)return false;
  string kind=role.kind??string.Empty;
  return filter switch{
   1=>kind.Contains("上級"),
   2=>kind.Contains("複合"),
   3=>kind.Contains("勢力"),
   4=>kind.Contains("隠し"),
   _=>!kind.Contains("上級")&&!kind.Contains("複合")&&!kind.Contains("勢力")&&!kind.Contains("隠し")
  };
 }

 void PopulateStatusList(System.Collections.Generic.List<IdInt> learned,MetaSave meta){
  SaveMgmtListScroll();
  mgmtListScroll.Clear();
  if(learned.Count==0){
   mgmtListScroll.Add(PackspireUiFactory.EmptyState("習得役職なし","役職を習得するとここへ記録されます。"));
   RestoreMgmtListScroll();
   return;
  }
  foreach(var level in learned){
   var role=GameCatalog.Roles[level.id];
   bool equipped=level.id==meta.currentRole;
   var row=StatusRoleTicket(role.id,role.name,role.kind,level.value,role.maxLevel,equipped,level.id==selectedRoleId,()=>{
    selectedRoleId=role.id;
    UpdateMgmtListSelection(selectedRoleId);
    RefreshStatusDetail(CharacterCatalog.Get(meta.selectedCharacterId),meta,learned);
   });
   mgmtListScroll.Add(row);
  }
  RestoreMgmtListScroll();
 }

 void RefreshStatusDetail(CharacterDef character,MetaSave meta,System.Collections.Generic.List<IdInt> learned){
  _=character;
  // Keep role-detail-column shell; update fixed header + scroll body only.
  ClearMgmtDetailHero();
  mgmtDetailScroll?.Clear();
  if(mgmtDetailScroll!=null)mgmtDetailScroll.scrollOffset=Vector2.zero;

  if(string.IsNullOrEmpty(selectedRoleId)||!learned.Any(x=>x.id==selectedRoleId)){
   if(mgmtDetailHero!=null)mgmtDetailHero.style.display=DisplayStyle.None;
   mgmtDetailScroll.Add(PackspireUiFactory.EmptyState("役職を選択","中央の一覧から習得済み役職を選ぶと詳細が表示されます。"));
   return;
  }
  var selectedLevel=learned.First(x=>x.id==selectedRoleId);
  var selected=GameCatalog.Roles[selectedRoleId];
  SetMgmtDetailHeroArt(Atlas(game.UiRoleArt,RoleUv(selected.id),"ps-mgmt-detail-art-image"));
  var nameTitle=PackspireUiFactory.Title(selected.name);
  nameTitle.AddToClassList("ps-status-role-detail-name");
  var metaLine=PackspireUiFactory.Body($"{selected.kind}　Lv.{selectedLevel.value}/{selected.maxLevel}");
  metaLine.AddToClassList("ps-typo-secondary");
  metaLine.AddToClassList("ps-status-role-detail-meta");
  VisualElement equipped=null;
  if(selected.id==meta.currentRole){
   var stamp=Container("ps-seal-mark ps-status-equipped-seal");
   stamp.pickingMode=PickingMode.Ignore;
   stamp.Add(new Label("現在装備"){pickingMode=PickingMode.Ignore});
   equipped=stamp;
  }
  SetMgmtDetailHeroSummary(nameTitle,metaLine,equipped);

  var body=Container("ps-status-role-detail-body");
  body.Add(ManagementSection("説明",selected.description));
  body.Add(ManagementSection("現在発動中の効果",selected.description,selectedLevel.value<1));
  var trackHead=SelectiveSectionHead("","成長の軌跡");
  trackHead.AddToClassList("ps-status-track-head");
  body.Add(trackHead);
  body.Add(StatusLevelTrack(selected,selectedLevel.value));
  if(selectedLevel.value<7)
   body.Add(ManagementSection("次に到達する効果","Lv.7 で解放\n"+game.UiRoleMilestone(selected.id,false)));
  else if(selectedLevel.value<selected.maxLevel)
   body.Add(ManagementSection("次に到達する効果",$"Lv.{selected.maxLevel} で解放\n"+game.UiRoleMilestone(selected.id,true)));
  var expand=Container("ps-status-role-expand");
  expand.pickingMode=PickingMode.Ignore;
  expand.Add(ManagementSection("派生・上級職","解放条件が公開されたときに追記されます。",true));
  body.Add(expand);
  if(selected.id!=meta.currentRole)
   body.Add(ManagementSection("転職","専用イベントまたは施設から変更できます。"));
  var tail=Container("ps-space-scroll-tail");
  tail.pickingMode=PickingMode.Ignore;
  body.Add(tail);
  mgmtDetailScroll.Add(body);
  mgmtDetailScroll.scrollOffset=Vector2.zero;
  mgmtDetailScroll.schedule.Execute(()=>{
   if(mgmtDetailScroll!=null)mgmtDetailScroll.scrollOffset=Vector2.zero;
  }).ExecuteLater(0);
 }

 void BuildStatusAgain(){RefreshStatusScreen();}
 void RefreshStatusScreen(){
  if(mgmtListScroll==null||renderedScreen!=ScreenId.Status){RebuildScreen(BuildStatus);return;}
  var meta=game.UiMeta;
  var character=CharacterCatalog.Get(meta.selectedCharacterId);
  var allLearned=meta.jobLevels.Where(x=>x.value>0&&GameCatalog.Roles.ContainsKey(x.id)).ToList();
  var learned=FilteredStatusRoles(allLearned);
  if(learned.Count>0&&!learned.Any(x=>x.id==selectedRoleId))selectedRoleId=learned[0].id;
  if(learned.Count==0)selectedRoleId="";
  ClearMgmtOverview();
  mgmtOverviewHost.Add(ManagementCharacterOverview(character,meta));
  PopulateStatusHeader();
  PopulateStatusList(learned,meta);
  RefreshStatusDetail(character,meta,learned);
 }

 void BuildVault(){
  var meta=game.UiMeta;
  if(vaultFilter<0||vaultFilter>4)vaultFilter=0;
  var stash=SortVaultStash(FilteredVaultStash(meta)).ToList();
  if(stash.Count==0&&meta.stash.Count>0&&vaultFilter!=0)vaultFilter=0;
  if(string.IsNullOrEmpty(selectedVaultUid)||!meta.stash.Any(x=>x.uid==selectedVaultUid))
   selectedVaultUid=meta.stash.FirstOrDefault()?.uid??"";

  var shell=BuildManagementShell("VAULT  /  ARMORY","保管庫",ManagementLayout.VaultListDetail,out _,out _);
  screenRoot.Add(shell);
  RefreshVaultScreen(true);
 }

 System.Collections.Generic.IEnumerable<ItemInstance> FilteredVaultStash(MetaSave meta){
  if(meta.stash==null)return System.Array.Empty<ItemInstance>();
  return vaultFilter switch{
   1=>meta.stash.Where(x=>GameCatalog.Items.TryGetValue(x.templateId,out var def)&&def.type==ItemType.Weapon),
   2=>meta.stash.Where(x=>GameCatalog.Items.TryGetValue(x.templateId,out var def)&&def.type==ItemType.Armor),
   3=>meta.stash.Where(x=>GameCatalog.Items.TryGetValue(x.templateId,out var def)&&def.type==ItemType.Supply),
   4=>meta.stash.Where(x=>GameCatalog.Items.TryGetValue(x.templateId,out var def)&&def.type==ItemType.Rune),
   _=>meta.stash
  };
 }

 void PopulateVaultGrid(MetaSave meta,System.Collections.Generic.List<ItemInstance> stash){
  SaveMgmtListScroll();
  if(mgmtVaultGrid==null)return;
  mgmtVaultGrid.Clear();
  if(stash.Count==0){
   mgmtVaultGrid.Add(PackspireUiFactory.EmptyState("該当する装備なし",vaultFilter==0?"遠征から帰還すると装備が記録されます。":"条件を変えると他の装備が表示されます。"));
   RestoreMgmtListScroll();
   return;
  }
  foreach(var item in stash){
   var uid=item.uid;
   var card=BuildVaultGridCard(meta,item,uid==selectedVaultUid,()=>{
    selectedVaultUid=uid;
    UpdateMgmtVaultGridSelection(selectedVaultUid);
    RefreshVaultDetail(game.UiMeta);
   });
   mgmtVaultGrid.Add(card);
  }
  RestoreMgmtListScroll();
 }

 void RefreshVaultDetail(MetaSave meta){
  ClearMgmtDetailHero();
  mgmtDetailScroll?.Clear();
  var stash=SortVaultStash(FilteredVaultStash(meta)).ToList();
  var selected=meta.stash.FirstOrDefault(x=>x.uid==selectedVaultUid);
  if(selected==null||!stash.Any(x=>x.uid==selectedVaultUid)){
   if(mgmtDetailHero!=null)mgmtDetailHero.style.display=DisplayStyle.None;
   mgmtDetailScroll?.Add(PackspireUiFactory.EmptyState("装備を選択","左の装備棚から記録を選んでください。"));
   return;
  }
  var def=GameCatalog.Items[selected.templateId];
  bool heirloom=selected.uid==meta.selectedHeirloomUid;
  if(!heirloom)vaultRecordPage=0;
  SetMgmtDetailHeroArt(VaultItemArt(def.id,"ps-mgmt-detail-art-image ps-vault-v9-item-art"));

  var identity=Container("ps-vault-v9-identity-copy");
  var pageTabs=Container("ps-vault-v9-page-tabs");
  pageTabs.Add(VaultRecordTab("装備","1/2",vaultRecordPage==0,()=>{
   if(vaultRecordPage==0)return;
   vaultRecordPage=0;
   RefreshVaultDetail(game.UiMeta);
  }));
  if(heirloom){
   pageTabs.Add(VaultRecordTab("家宝","2/2",vaultRecordPage==1,()=>{
    if(vaultRecordPage==1)return;
    vaultRecordPage=1;
    RefreshVaultDetail(game.UiMeta);
   }));
  }
  identity.Add(pageTabs);
  var nameRow=Container("ps-mgmt-detail-name-row ps-vault-v9-name-row");
  var name=PackspireUiFactory.Title(def.name);
  name.AddToClassList("ps-vault-v9-item-name");
  nameRow.Add(name);
  if(heirloom)nameRow.Add(HeirloomMark());
  if(VaultItemInLoadout(meta,selected.uid)){
   var use=Container("ps-seal-mark ps-vault-v9-inuse");
   use.Add(new Label("使用中"){pickingMode=PickingMode.Ignore});
   nameRow.Add(use);
  }
  identity.Add(nameRow);
  var metaLine=PackspireUiFactory.Body($"{ItemTypeLabel(def.type)}　／　RANK {Mathf.Max(1,selected.temper+1)}");
  metaLine.AddToClassList("ps-vault-v9-meta");
  identity.Add(metaLine);
  var facts=Container("ps-vault-v9-identity-facts");
  var factsCopy=Container("ps-vault-v9-identity-copy-column");
  var description=PackspireUiFactory.Body(def.description);
  description.AddToClassList("ps-vault-v9-description");
  factsCopy.Add(description);
  var durability=PackspireUiFactory.Body($"耐久　{selected.durability} / {def.baseDurability}");
  durability.AddToClassList("ps-vault-v9-durability");
  factsCopy.Add(durability);
  facts.Add(factsCopy);
  facts.Add(VaultOccupancyShape(selected,def));
  identity.Add(facts);
  SetMgmtDetailHeroSummary(identity);

  if(vaultRecordPage==1&&heirloom){
   mgmtDetailScroll?.Add(BuildVaultHeirloomPage(selected,def));
   return;
  }

  var lower=Container("ps-vault-v9-lower");
  var cardPanel=Container("ps-vault-v9-card-panel");
  var cardHeader=Container("ps-vault-v9-lower-header");
  var cardHeading=new Label(vaultCardExploration?"探索カード":"戦闘カード"){pickingMode=PickingMode.Ignore};
  cardHeading.AddToClassList("ps-vault-v9-lower-title");
  cardHeader.Add(cardHeading);
  var flip=PackspireUiFactory.Button(vaultCardExploration?"戦闘面へ":"探索面へ",()=>{
   vaultCardExploration=!vaultCardExploration;
   RefreshVaultDetail(game.UiMeta);
  });
  flip.AddToClassList("ps-vault-v9-flip");
  cardHeader.Add(flip);
  cardPanel.Add(cardHeader);
 var face=BuildEquipmentCardFacePreview(selected,def,game.UiRun,vaultCardExploration);
  if(face!=null){
   face.pickingMode=PickingMode.Position;
   face.AddToClassList("ps-vault-v9-card-open");
   face.tooltip="クリックでカードを拡大";
   face.RegisterCallback<ClickEvent>(evt=>{
    evt.StopPropagation();
    ShowVaultCardModal(selected,def,vaultCardExploration);
   });
   cardPanel.Add(face);
  }
  else cardPanel.Add(PackspireUiFactory.EmptyState("カードなし","この装備に対応するカードはありません。"));
  lower.Add(cardPanel);

  var effectPanel=Container("ps-vault-v9-effect-panel");
  var trait=StorageFormulaCatalog.Trait(selected.traitId);
  string colorName=trait?.name??VaultColorEffectName(selected);
  string colorBody=trait!=null
   ?TraitEffectLabel(trait)
   :VaultColorEffectBody(selected);
  effectPanel.Add(VaultEffectRecord("色効果",colorName,colorBody,"ps-vault-v9-color-effect"));
  effectPanel.Add(VaultEffectRecord(
   "LINK効果",
   string.IsNullOrEmpty(def.linkRule)?"固有LINKなし":"装備LINK",
   string.IsNullOrEmpty(def.linkRule)?"隣接による追加効果はありません。":def.linkRule,
   "ps-vault-v9-link-effect"
  ));
  var actions=Container("ps-vault-v9-actions");
  var lockButton=VaultLockAction(
   selected.insured,
   ()=>{
    selected.insured=!selected.insured;
    SaveSystem.Save(meta);
    UpdateMgmtVaultGridBadges(meta);
    RefreshVaultDetail(meta);
   }
  );
  actions.Add(lockButton);
  effectPanel.Add(actions);
  lower.Add(effectPanel);
 mgmtDetailScroll?.Add(lower);
}

 void ShowVaultCardModal(ItemInstance item,ItemDef def,bool exploration){
  CloseVaultCardModal();
  if(screenRoot==null||item==null||def==null)return;
  var overlay=Container("ps-vault-card-modal");
  overlay.pickingMode=PickingMode.Position;
  overlay.RegisterCallback<ClickEvent>(evt=>{
   if(evt.target==overlay)CloseVaultCardModal();
  });

  var stage=Container("ps-vault-card-modal-stage");
  stage.pickingMode=PickingMode.Position;
  stage.RegisterCallback<ClickEvent>(evt=>{
   if(evt.target==stage)CloseVaultCardModal();
  });
  var heading=new Label(exploration?"探索カード":"戦闘カード"){pickingMode=PickingMode.Ignore};
  heading.AddToClassList("ps-vault-card-modal-heading");
  stage.Add(heading);
  var enlarged=BuildEquipmentCardFacePreview(item,def,game.UiRun,exploration);
  if(enlarged!=null){
   enlarged.pickingMode=PickingMode.Position;
   enlarged.AddToClassList("ps-vault-card-modal-face");
   stage.Add(enlarged);
  }
  var hint=new Label("カードの外側を押すと閉じます"){pickingMode=PickingMode.Ignore};
  hint.AddToClassList("ps-vault-card-modal-hint");
  stage.Add(hint);
  overlay.Add(stage);
  screenRoot.Add(overlay);
  vaultCardModal=overlay;
 }

 void CloseVaultCardModal(){
  vaultCardModal?.RemoveFromHierarchy();
  vaultCardModal=null;
 }

 Button VaultRecordTab(string label,string page,bool selected,System.Action onClick){
  var button=PackspireUiFactory.Button("",onClick);
  button.AddToClassList("ps-vault-v9-page-tab");
  button.EnableInClassList("ps-selected",selected);
  button.Add(new Label(label){pickingMode=PickingMode.Ignore});
  var index=new Label(page){pickingMode=PickingMode.Ignore};
  index.AddToClassList("ps-vault-v9-page-index");
  button.Add(index);
  return button;
 }

 VisualElement VaultEffectRecord(string eyebrow,string title,string body,string className){
  var section=Container("ps-vault-v9-effect "+className);
  var label=new Label(eyebrow){pickingMode=PickingMode.Ignore};
  label.AddToClassList("ps-vault-v9-effect-eyebrow");
  section.Add(label);
  var heading=new Label(title){pickingMode=PickingMode.Ignore};
  heading.AddToClassList("ps-vault-v9-effect-name");
  section.Add(heading);
  var copy=new Label(body){pickingMode=PickingMode.Ignore};
  copy.AddToClassList("ps-vault-v9-effect-body");
  section.Add(copy);
  return section;
 }

 string VaultColorEffectName(ItemInstance item){
  if(item?.colors==null||item.colors.Count==0)return "無彩";
  return $"{ElementLabel(item.colors[0])}色共鳴";
 }

 string VaultColorEffectBody(ItemInstance item){
  if(item?.colors==null||item.colors.Count==0)return "色一致による個体効果はありません。";
  var counts=item.colors.GroupBy(x=>x).OrderByDescending(x=>x.Count()).ToList();
  return $"{string.Join("・",counts.Select(x=>$"{ElementLabel(x.Key)}{x.Count()}"))}。同色セルの一致で個体特性が発動します。";
 }

 VisualElement VaultOccupancyShape(ItemInstance item,ItemDef def){
  var block=Container("ps-vault-v9-shape");
  var heading=new Label("占有形状"){pickingMode=PickingMode.Ignore};
  heading.AddToClassList("ps-vault-v9-shape-title");
  block.Add(heading);
  var layout=def?.cells==null||def.cells.Length==0
   ?new System.Collections.Generic.List<(Vector2Int pos,int original,Element element,int value)>()
   :BackpackSystem.Layout(def,0,item);
  if(layout.Count==0)return block;
  int width=layout.Max(cell=>cell.pos.x)+1;
  int height=layout.Max(cell=>cell.pos.y)+1;
  var grid=Container("ps-vault-v9-shape-grid");
  for(int y=0;y<height;y++){
   var row=Container("ps-vault-v9-shape-row");
   for(int x=0;x<width;x++){
    var cell=Container("ps-vault-v9-shape-cell");
    var occupied=layout.FirstOrDefault(value=>value.pos.x==x&&value.pos.y==y);
    bool filled=layout.Any(value=>value.pos.x==x&&value.pos.y==y);
    cell.AddToClassList(filled?"ps-filled":"ps-empty");
    if(filled)cell.AddToClassList("ps-element-"+occupied.element.ToString().ToLowerInvariant());
    row.Add(cell);
   }
   grid.Add(row);
  }
  block.Add(grid);
  return block;
 }

 Button VaultLockAction(bool locked,System.Action onClick){
  var button=new Button(){text=string.Empty};
  if(onClick!=null)button.clicked+=onClick;
  button.Clear();
  button.AddToClassList("ps-vault-v9-lock-action");
  button.EnableInClassList("ps-locked",locked);
  button.tooltip=locked?"クリックでロック解除":"クリックで装備をロック";
  return button;
 }

 VisualElement BuildVaultHeirloomPage(ItemInstance item,ItemDef def){
  var page=Container("ps-vault-v9-heirloom-page");
  var history=item.history??new HeirloomHistory();
  page.Add(VaultEffectRecord(
   "家宝の記録",
   "遠征の記憶",
   $"戦闘 {history.battles}　ボス {history.bosses}　敗北 {history.defeats}",
   "ps-vault-v9-heirloom-history"
  ));
  string scars=item.scars==null||item.scars.Count==0
   ?"まだ傷跡は刻まれていません。"
   :string.Join("\n",item.scars.Take(3).Select(scar=>$"◆ {scar.type}　{scar.dungeon}"));
  page.Add(VaultEffectRecord("傷跡","刻まれた履歴",scars,"ps-vault-v9-heirloom-scars"));
  page.Add(VaultEffectRecord(
   "継承",
   def.name,
   "家宝として蓄積した記憶は、次の遠征でも失われません。",
   "ps-vault-v9-heirloom-legacy"
  ));
  return page;
 }

 void BuildVaultAgain(){RefreshVaultScreen(false);}
 void RefreshVaultScreen(bool rebuildList){
  if(mgmtListScroll==null||renderedScreen!=ScreenId.Vault){RebuildScreen(BuildVault);return;}
  var meta=game.UiMeta;
  var stash=SortVaultStash(FilteredVaultStash(meta)).ToList();
  if(vaultFilter<0||vaultFilter>4)vaultFilter=0;
  if(stash.Count==0&&meta.stash.Count>0&&vaultFilter!=0){
   vaultFilter=0;
   stash=SortVaultStash(meta.stash).ToList();
  }
  if(!stash.Any(x=>x.uid==selectedVaultUid))selectedVaultUid=stash.FirstOrDefault()?.uid??meta.stash.FirstOrDefault()?.uid??"";
  if(rebuildList){
   mgmtListHeader.Clear();
   mgmtListHeader.Add(SelectiveSectionHead("ITEM STORAGE","装備棚"));
   mgmtListHeader.Add(VaultCategoryBar(new[]{"すべて","武器","防具","道具","遺物"},vaultFilter,index=>{
    vaultFilter=index;
    RefreshVaultScreen(true);
   }));
   var active=LoadoutSystem.Active(meta);
   var context=PackspireUiFactory.Body($"使用中荷造り　{active.name}");
   context.AddToClassList("ps-vault-v7-context");
   mgmtListHeader.Add(context);
   if(mgmtVaultFooter!=null){
    mgmtVaultFooter.Clear();
    mgmtVaultFooter.pickingMode=PickingMode.Position;
    var sortWrap=Container("ps-vault-v7-rarity-filter");
    sortWrap.pickingMode=PickingMode.Position;
    var sortLabel=new Label("並べ替え"){pickingMode=PickingMode.Ignore};
    sortLabel.AddToClassList("ps-vault-v8-sort-label");
    sortWrap.Add(sortLabel);
    var sortChoices=new List<string>{
     "レアリティ順","入手段階順","名前順","種類順","占有マス順","鍛錬順","耐久が低い順"
    };
    vaultSortMode=Mathf.Clamp(vaultSortMode,0,sortChoices.Count-1);
    var sortField=new DropdownField(sortChoices,vaultSortMode);
    sortField.pickingMode=PickingMode.Position;
    sortField.SetEnabled(true);
    sortField.AddToClassList("ps-vault-v8-sort");
    sortField.RegisterValueChangedCallback(_=>{
     int next=sortField.index;
     if(next<0||next==vaultSortMode)return;
     vaultSortMode=next;
     RefreshVaultScreen(true);
    });
    sortWrap.Add(sortField);
    mgmtVaultFooter.Add(sortWrap);
    var count=PackspireUiFactory.Body($"{stash.Count} / {Mathf.Max(240,meta.stash.Count)}");
    count.AddToClassList("ps-vault-v7-count");
    mgmtVaultFooter.Add(count);
    mgmtVaultFooter.BringToFront();
   }
   PopulateVaultGrid(meta,stash);
  }else{
   UpdateMgmtVaultGridSelection(selectedVaultUid);
   UpdateMgmtVaultGridBadges(meta);
  }
  RefreshVaultDetail(meta);
 }

 System.Collections.Generic.IEnumerable<ItemInstance> SortVaultStash(System.Collections.Generic.IEnumerable<ItemInstance> items){
  return vaultSortMode switch{
   1=>items.OrderByDescending(x=>GameCatalog.Items[x.templateId].acquisitionTier)
           .ThenByDescending(x=>(int)GameCatalog.Items[x.templateId].rarity)
           .ThenBy(x=>GameCatalog.Items[x.templateId].name),
   2=>items.OrderBy(x=>GameCatalog.Items[x.templateId].name),
   3=>items.OrderBy(x=>(int)GameCatalog.Items[x.templateId].type)
           .ThenBy(x=>GameCatalog.Items[x.templateId].name),
   4=>items.OrderByDescending(x=>GameCatalog.Items[x.templateId].cells?.Length??0)
           .ThenByDescending(x=>(int)GameCatalog.Items[x.templateId].rarity),
   5=>items.OrderByDescending(x=>x.temper)
           .ThenByDescending(x=>(int)GameCatalog.Items[x.templateId].rarity),
   6=>items.OrderBy(x=>x.durability)
           .ThenBy(x=>GameCatalog.Items[x.templateId].name),
   _=>items.OrderByDescending(x=>(int)GameCatalog.Items[x.templateId].rarity)
           .ThenByDescending(x=>GameCatalog.Items[x.templateId].acquisitionTier)
           .ThenBy(x=>GameCatalog.Items[x.templateId].name)
  };
 }

 void BuildCompendium(){
  if(compendiumTab>2)compendiumTab=0;
  var shell=BuildManagementShell("CODEX  /  ARCHIVE","図鑑",ManagementLayout.CompendiumReelDetail,out _,out _);
  screenRoot.Add(shell);
  RefreshCompendiumScreen(true);
 }

 void RefreshCompendiumScreen(bool rebuildList){
  if(mgmtListScroll==null||renderedScreen!=ScreenId.Compendium){RebuildScreen(BuildCompendium);return;}
  if(compendiumTab>2)compendiumTab=0;
  var meta=game.UiMeta;
  if(rebuildList){
   mgmtListHeader.Clear();
   mgmtListHeader.Add(ManagementFilterBar(new[]{"装備","役職","敵"},compendiumTab,tab=>{
    if(compendiumTab==tab)return;
    compendiumTab=tab;
    selectedCompendiumId="";
    RefreshCompendiumScreen(true);
   }));
   PopulateCompendiumList(meta);
  }
  RefreshCompendiumDetail(meta);
 }

 void PopulateCompendiumList(MetaSave meta){
  SaveMgmtListScroll();
  mgmtListScroll.Clear();
  if(compendiumTab==0)PopulateItemCompendiumList(meta);
  else if(compendiumTab==1)PopulateRoleCompendiumList(meta);
  else PopulateEnemyCompendiumList(meta);
  RestoreMgmtListScroll();
 }

 static string CompendiumCategoryMark(int tab,string fallback)=>tab switch{0=>"装",1=>"役",2=>"敵",_=>fallback};

 void PopulateItemCompendiumList(MetaSave meta){
  var values=GameCatalog.Items.Values.ToArray();
  EnsureCompendiumSelection(values.Select(x=>x.id).ToArray());
  foreach(var item in values){
   bool known=meta.discoveredItems.Contains(item.id);
   VisualElement leading=null;
   if(known&&game.UiEquipmentArt!=null)
    leading=SmallAtlasIcon(game.UiEquipmentArt,ItemUv(item.id));
   else
    leading=CodexIndexMark("？",true);
   var row=ManagementReelRow(item.id,known?item.name:"？？？",known?ItemTypeLabel(item.type):"未発見",item.id==selectedCompendiumId,()=>{
    selectedCompendiumId=item.id;
    UpdateMgmtListSelection(selectedCompendiumId);
    RefreshCompendiumDetail(meta);
   },leading);
   row.AddToClassList("ps-codex-index-row");
   if(!known){
    row.AddToClassList("ps-mgmt-list-unknown");
    row.AddToClassList("ps-undiscovered");
   }
   mgmtListScroll.Add(row);
  }
 }

 void PopulateRoleCompendiumList(MetaSave meta){
  var values=GameCatalog.Roles.Values.ToArray();
  EnsureCompendiumSelection(values.Select(x=>x.id).ToArray());
  foreach(var role in values){
   bool known=meta.jobLevels.Any(x=>x.id==role.id&&x.value>0);
   VisualElement leading=null;
   if(known&&game.UiRoleArt!=null)
    leading=SmallAtlasIcon(game.UiRoleArt,RoleUv(role.id));
   else
    leading=CodexIndexMark("役",true);
   var row=ManagementReelRow(role.id,known?role.name:"？？？",known?role.kind:"未習得",role.id==selectedCompendiumId,()=>{
    selectedCompendiumId=role.id;
    UpdateMgmtListSelection(selectedCompendiumId);
    RefreshCompendiumDetail(meta);
   },leading);
   row.AddToClassList("ps-codex-index-row");
   if(!known){
    row.AddToClassList("ps-mgmt-list-unknown");
    row.AddToClassList("ps-undiscovered");
   }
   mgmtListScroll.Add(row);
  }
 }

 void PopulateEnemyCompendiumList(MetaSave meta){
  var values=GameCatalog.Enemies;
  EnsureCompendiumSelection(values.Select(x=>x.id).ToArray());
  foreach(var enemy in values){
   bool known=meta.discoveredEnemies.Contains(enemy.id);
   VisualElement leading=known?CodexIndexMark("敵"):CodexIndexMark("？",true);
   var row=ManagementReelRow(enemy.id,known?enemy.name:"？？？",known?$"危険度 {enemy.tier}":"未遭遇",enemy.id==selectedCompendiumId,()=>{
    selectedCompendiumId=enemy.id;
    UpdateMgmtListSelection(selectedCompendiumId);
    RefreshCompendiumDetail(meta);
   },leading);
   row.AddToClassList("ps-codex-index-row");
   if(!known){
    row.AddToClassList("ps-mgmt-list-unknown");
    row.AddToClassList("ps-undiscovered");
   }
   mgmtListScroll.Add(row);
  }
 }

 void EnsureCompendiumSelection(string[] ids){
  if(ids.Length==0){selectedCompendiumId="";return;}
  if(string.IsNullOrEmpty(selectedCompendiumId)||!ids.Contains(selectedCompendiumId))selectedCompendiumId=ids[0];
 }

 void RefreshCompendiumDetail(MetaSave meta){
  if(mgmtDetailArtHost!=null)mgmtDetailArtHost.Clear();
  if(mgmtDetailSummaryHost!=null)mgmtDetailSummaryHost.Clear();
  mgmtDetailScroll?.Clear();
  if(mgmtDetailScroll!=null)mgmtDetailScroll.scrollOffset=Vector2.zero;
  if(compendiumTab==0)RefreshItemCompendiumDetail(meta);
  else if(compendiumTab==1)RefreshRoleCompendiumDetail(meta);
  else RefreshEnemyCompendiumDetail(meta);
  if(mgmtDetailScroll!=null){
   mgmtDetailScroll.scrollOffset=Vector2.zero;
   mgmtDetailScroll.schedule.Execute(()=>{if(mgmtDetailScroll!=null)mgmtDetailScroll.scrollOffset=Vector2.zero;}).ExecuteLater(0);
  }
 }

 void RefreshItemCompendiumDetail(MetaSave meta){
  if(!GameCatalog.Items.ContainsKey(selectedCompendiumId))return;
  var selected=GameCatalog.Items[selectedCompendiumId];
  bool known=meta.discoveredItems.Contains(selected.id);
  if(!known){
   SetMgmtUnknownFocalArt("？");
   SetMgmtDetailHeroSummary(
    new Label("？？？"){pickingMode=PickingMode.Ignore},
    PackspireUiFactory.Body("未発見")
   );
   mgmtDetailScroll.Add(ManagementSection("記録","遠征や戦闘で入手すると記録されます。"));
   return;
  }
  SetMgmtDetailHeroArt(Atlas(game.UiEquipmentArt,ItemUv(selected.id),"ps-mgmt-detail-art-image"));
  SetMgmtDetailHeroSummary(
   PackspireUiFactory.Title(selected.name),
   PackspireUiFactory.Body(ItemTypeLabel(selected.type)),
   PackspireUiFactory.Body($"{selected.cells.Length}マス")
  );
  mgmtDetailScroll.Add(ManagementSection("概要",selected.description));
  if(selected.cells.Length>0)
   mgmtDetailScroll.Add(ManagementSection("属性",string.Join("・",selected.cells.Select(x=>ElementLabel(x.element)))));
  if(!string.IsNullOrEmpty(selected.linkRule))
   mgmtDetailScroll.Add(ManagementSection("LINK",selected.linkRule));
  var cardPair=BuildEquipmentCardPairPreview(new ItemInstance(selected.id),selected,game.UiRun);
  if(cardPair!=null)mgmtDetailScroll.Add(cardPair);
  var faceLines=new List<string>();
  foreach(var grant in selected.grantedCards??System.Array.Empty<GrantedCardDef>()){
   string battle=GameCatalog.Cards.TryGetValue(grant.battleCardId,out var battleCard)
    ?battleCard.name:grant.battleCardId;
   string exploration=GameCatalog.ExplorationCards.TryGetValue(grant.explorationCardId,out var exploreCard)
    ?exploreCard.name:grant.explorationCardId;
   faceLines.Add($"{battle} ⇄ {exploration}{(grant.count>1?$" ×{grant.count}":"")}");
   if(exploreCard?.stages!=null&&exploreCard.stages.Length>1)
    faceLines.Add("  成長: "+string.Join(" → ",exploreCard.stages.Select(stage=>stage.name)));
  }
  if(faceLines.Count>0)
   mgmtDetailScroll.Add(ManagementSection("カード両面",string.Join("\n",faceLines)));
  mgmtDetailScroll.Add(ManagementSection("入手","遠征や戦闘で入手すると記録されます。"));
}

 void RefreshRoleCompendiumDetail(MetaSave meta){
  if(!GameCatalog.Roles.ContainsKey(selectedCompendiumId))return;
  var selected=GameCatalog.Roles[selectedCompendiumId];
  bool known=meta.jobLevels.Any(x=>x.id==selected.id&&x.value>0);
  if(!known){
   SetMgmtUnknownFocalArt("役");
   SetMgmtDetailHeroSummary(
    new Label("？？？"){pickingMode=PickingMode.Ignore},
    PackspireUiFactory.Body("未習得")
   );
   mgmtDetailScroll.Add(ManagementSection("解放条件","イベントや遠征で習得できます。"));
   return;
  }
  var level=meta.jobLevels.First(x=>x.id==selected.id);
  SetMgmtDetailHeroArt(Atlas(game.UiRoleArt,RoleUv(selected.id),"ps-mgmt-detail-art-image"));
  SetMgmtDetailHeroSummary(
   PackspireUiFactory.Title(selected.name),
   PackspireUiFactory.Body($"{selected.kind}　最大Lv.{selected.maxLevel}"),
   PackspireUiFactory.Body($"習得 Lv.{level.value}")
  );
  mgmtDetailScroll.Add(ManagementSection("概要",selected.description));
  mgmtDetailScroll.Add(ManagementSection("Lv.7 効果",game.UiRoleMilestone(selected.id,false),level.value<7));
  mgmtDetailScroll.Add(ManagementSection($"Lv.{selected.maxLevel} 効果",game.UiRoleMilestone(selected.id,true),level.value<selected.maxLevel));
  mgmtDetailScroll.Add(ManagementSection("解放条件","イベントや遠征で習得できます。"));
}

 void RefreshEnemyCompendiumDetail(MetaSave meta){
  var selected=GameCatalog.Enemies.FirstOrDefault(x=>x.id==selectedCompendiumId);
  if(selected==null){
   SetMgmtUnknownFocalArt("敵");
   mgmtDetailScroll.Add(PackspireUiFactory.EmptyState("記録なし","遭遇した敵がここへ記録されます。"));
   return;
  }
  bool known=meta.discoveredEnemies.Contains(selected.id);
  if(!known){
   SetMgmtUnknownFocalArt("敵");
   SetMgmtDetailHeroSummary(
    new Label("？？？"){pickingMode=PickingMode.Ignore},
    PackspireUiFactory.Body("未遭遇")
   );
   mgmtDetailScroll.Add(ManagementSection("出現","塔の各階層で遭遇する可能性があります。"));
   return;
  }
  SetMgmtDetailHeroArt(EnemyPortrait(selected,"ps-mgmt-detail-art-image"));
  SetMgmtDetailHeroSummary(
   PackspireUiFactory.Title(selected.name),
   PackspireUiFactory.Body($"危険度 {selected.tier}"),
   PackspireUiFactory.Body($"基礎HP {selected.hp}")
  );
  mgmtDetailScroll.Add(ManagementSection("行動",string.Join("・",selected.damages.Select(x=>x==0?"特殊行動":$"攻撃{x}"))));
  mgmtDetailScroll.Add(ManagementSection("出現","塔の各階層で遭遇する可能性があります。"));
}

 void BuildCompendiumAgain(){RefreshCompendiumScreen(false);}
}
}
