using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire {
public sealed partial class PackspireUiFoundation {
 void BuildStatus(){
  var meta=game.UiMeta;
  var character=CharacterCatalog.Get(meta.selectedCharacterId);
  var learned=meta.jobLevels.Where(x=>x.value>0&&GameCatalog.Roles.ContainsKey(x.id)).ToList();
  if(learned.Count>0){
   if(string.IsNullOrEmpty(selectedRoleId)||!learned.Any(x=>x.id==selectedRoleId))
    selectedRoleId=learned.FirstOrDefault(x=>x.id==meta.currentRole)?.id??learned[0].id;
  }else selectedRoleId="";

  var shell=BuildManagementShell("STATUS  /  RANK","ステータス",ManagementLayout.StatusOverview,out var _,out _);
  screenRoot.Add(shell);

  ClearMgmtOverview();
  mgmtOverviewHost.Add(ManagementCharacterOverview(character,meta));

  mgmtListHeader.Clear();
  mgmtListHeader.Add(SelectiveSectionHead("ROLES","習得役職"));
  PopulateStatusList(learned,meta);
  RefreshStatusDetail(character,meta,learned);
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
  var learned=meta.jobLevels.Where(x=>x.value>0&&GameCatalog.Roles.ContainsKey(x.id)).ToList();
  if(learned.Count>0&&!learned.Any(x=>x.id==selectedRoleId))selectedRoleId=learned[0].id;
  ClearMgmtOverview();
  mgmtOverviewHost.Add(ManagementCharacterOverview(character,meta));
  PopulateStatusList(learned,meta);
  RefreshStatusDetail(character,meta,learned);
 }

 void BuildVault(){
  var meta=game.UiMeta;
  if(vaultFilter!=0&&vaultFilter!=1)vaultFilter=0;
  var stash=FilteredVaultStash(meta).ToList();
  if(stash.Count==0&&meta.stash.Count>0&&vaultFilter!=0)vaultFilter=0;
  if(string.IsNullOrEmpty(selectedVaultUid)||!meta.stash.Any(x=>x.uid==selectedVaultUid))
   selectedVaultUid=meta.stash.FirstOrDefault()?.uid??"";

  var shell=BuildManagementShell("VAULT  /  ARMORY","保管庫",ManagementLayout.VaultListDetail,out _,out _);
  screenRoot.Add(shell);
  RefreshVaultScreen(true);
 }

 System.Collections.Generic.IEnumerable<ItemInstance> FilteredVaultStash(MetaSave meta){
  if(meta.stash==null)return System.Array.Empty<ItemInstance>();
  if(vaultFilter==1)return meta.stash.Where(x=>VaultItemInLoadout(meta,x.uid));
  return meta.stash;
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
  if(mgmtDetailScroll!=null)mgmtDetailScroll.scrollOffset=Vector2.zero;
  var stash=FilteredVaultStash(meta).ToList();
  var selected=meta.stash.FirstOrDefault(x=>x.uid==selectedVaultUid);
  if(selected==null||!stash.Any(x=>x.uid==selectedVaultUid)){
   if(mgmtDetailHero!=null)mgmtDetailHero.style.display=DisplayStyle.None;
   mgmtDetailScroll.Add(PackspireUiFactory.EmptyState("装備を選択","左の一覧から装備を選ぶと詳細が表示されます。"));
   return;
  }
  var def=GameCatalog.Items[selected.templateId];
  SetMgmtDetailHeroArt(Atlas(game.UiEquipmentArt,ItemUv(def.id),"ps-mgmt-detail-art-image"));
  var nameBlock=Container("ps-mgmt-detail-name-row");
  nameBlock.Add(PackspireUiFactory.Title(def.name));
  if(selected.uid==meta.selectedHeirloomUid)nameBlock.Add(HeirloomMark());
  if(VaultItemInLoadout(meta,selected.uid)){
   var use=Container("ps-seal-mark");
   use.pickingMode=PickingMode.Ignore;
   use.Add(new Label("使用中"){pickingMode=PickingMode.Ignore});
   nameBlock.Add(use);
  }
  SetMgmtDetailHeroSummary(
   nameBlock,
   PackspireUiFactory.Body($"{ItemTypeLabel(def.type)}　{def.cells.Length}マス"),
   PackspireUiFactory.Body($"鍛錬 +{selected.temper}　耐久 {selected.durability}/6")
  );

  mgmtDetailScroll.Add(ManagementSection("基本性能",def.description));
  mgmtDetailScroll.Add(ManagementSection("使用状況",$"使用 {selected.uses}回"));
  if(selected.colors!=null&&selected.colors.Count>0)
   mgmtDetailScroll.Add(ManagementSection("色特性",string.Join("・",selected.colors.Select(ElementLabel))));
  var trait=StorageFormulaCatalog.Trait(selected.traitId);
  if(trait!=null)
   mgmtDetailScroll.Add(ManagementSection("個体特性",$"{trait.name}（{ElementLabel(trait.element)} {trait.requiredMatches}一致）\n{TraitEffectLabel(trait)}"));
  if(!string.IsNullOrEmpty(def.linkRule))
   mgmtDetailScroll.Add(ManagementSection("LINK効果",def.linkRule));
  var loadoutName=VaultLoadoutName(meta,selected.uid);
  if(!string.IsNullOrEmpty(loadoutName))
   mgmtDetailScroll.Add(ManagementSection("使用中の荷造り",loadoutName));
  if(selected.insured)
   mgmtDetailScroll.Add(ManagementSection("保護","保険加入"));
  var tail=Container("ps-space-scroll-tail");
  tail.pickingMode=PickingMode.Ignore;
  mgmtDetailScroll.Add(tail);
  mgmtDetailScroll.scrollOffset=Vector2.zero;
  mgmtDetailScroll.schedule.Execute(()=>{if(mgmtDetailScroll!=null)mgmtDetailScroll.scrollOffset=Vector2.zero;}).ExecuteLater(0);
 }

 void BuildVaultAgain(){RefreshVaultScreen(false);}
 void RefreshVaultScreen(bool rebuildList){
  if(mgmtListScroll==null||renderedScreen!=ScreenId.Vault){RebuildScreen(BuildVault);return;}
  var meta=game.UiMeta;
  var stash=FilteredVaultStash(meta).ToList();
  if(vaultFilter!=0&&vaultFilter!=1)vaultFilter=0;
  if(stash.Count==0&&meta.stash.Count>0&&vaultFilter!=0){
   vaultFilter=0;
   stash=meta.stash.ToList();
  }
  if(!stash.Any(x=>x.uid==selectedVaultUid))selectedVaultUid=stash.FirstOrDefault()?.uid??meta.stash.FirstOrDefault()?.uid??"";
  if(rebuildList){
   mgmtListHeader.Clear();
   mgmtListHeader.Add(ManagementFilterBar(new[]{"すべて","使用中"},vaultFilter,index=>{
    vaultFilter=index;
    RefreshVaultScreen(true);
   }));
   var active=LoadoutSystem.Active(meta);
   mgmtListHeader.Add(PackspireUiFactory.Body($"使用中荷造り　{active.name}　／　保管 {meta.stash.Count}"));
   PopulateVaultGrid(meta,stash);
  }else{
   UpdateMgmtVaultGridSelection(selectedVaultUid);
   UpdateMgmtVaultGridBadges(meta);
  }
  RefreshVaultDetail(meta);
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
