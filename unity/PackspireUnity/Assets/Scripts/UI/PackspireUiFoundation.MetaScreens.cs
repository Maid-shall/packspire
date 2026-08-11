using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire {
public sealed partial class PackspireUiFoundation {
 #region Status

 void BuildStatus(){
  var meta=game.UiMeta;
  var character=CharacterCatalog.Get(meta.selectedCharacterId);
  var allLearned=RoleFrameworkSystem.CoreRoleIds
   .Where(GameCatalog.Roles.ContainsKey)
   .Select(id=>new IdInt(id,Mathf.Max(1,meta.jobLevels.FirstOrDefault(x=>x.id==id)?.value??1)))
   .ToList();
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
  mgmtListHeader.Add(SelectiveSectionHead("FOUR CORE ROLES","基礎役職"));
 }

 System.Collections.Generic.List<IdInt> FilteredStatusRoles(System.Collections.Generic.List<IdInt> learned){
  return learned.Where(level=>RoleFrameworkSystem.CoreRoleIds.Contains(level.id)).ToList();
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
   ClearStatusAppointment();
   if(mgmtDetailHero!=null)mgmtDetailHero.style.display=DisplayStyle.None;
   mgmtDetailScroll.Add(PackspireUiFactory.EmptyState("役職を選択","中央の一覧から習得済み役職を選ぶと詳細が表示されます。"));
   return;
  }
  var selectedLevel=learned.First(x=>x.id==selectedRoleId);
  var selected=GameCatalog.Roles[selectedRoleId];
  RefreshStatusAppointment(selected,selectedLevel.value,selected.id==meta.currentRole);
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
  var appoint=PackspireUiFactory.Button(selected.id==meta.currentRole?"現役職":"この役職を任命",()=>{
   game.UiSetActiveRole(selected.id);
   RefreshStatusScreen();
  });
  appoint.AddToClassList("ps-status-appoint-action");
  appoint.SetEnabled(selected.id!=meta.currentRole);
  body.Add(appoint);
  var framework=RoleFrameworkSystem.Get(selected.id);
  body.Add(ManagementSection("探索固有印",$"{framework.sealName}\n{framework.sealText}"));
  var branchIndex=RoleFrameworkSystem.Branch(meta,selected.id);
  var branches=Container("ps-status-branch-choices");
  for(int index=0;index<framework.branches.Length;index++){
   int choice=index;
   var branch=framework.branches[index];
   var button=PackspireUiFactory.Button($"{branch.name}\n{branch.text}",()=>{
    game.UiSetRoleBranch(selected.id,choice);
    RefreshStatusScreen();
   });
   button.AddToClassList("ps-status-branch-choice");
   button.EnableInClassList("ps-selected",index==branchIndex);
   branches.Add(button);
  }
  body.Add(SelectiveSectionHead("BRANCH","役職分岐"));
  body.Add(branches);
  var qualification=Container("ps-status-qualification");
  qualification.Add(SelectiveSectionHead("QUALIFICATION SEAL","資格印（一枠）"));
  var clearQualification=PackspireUiFactory.Button("資格印を外す",()=>{
   game.UiSetQualificationSeal("");
   RefreshStatusScreen();
  });
  clearQualification.EnableInClassList("ps-selected",string.IsNullOrEmpty(meta.qualificationSealId));
  qualification.Add(clearQualification);
  foreach(var roleId in meta.unlockedRoles.Where(id=>GameCatalog.Roles.ContainsKey(id)&&!RoleFrameworkSystem.CoreRoleIds.Contains(id)).Take(6)){
   string id=roleId;
   var option=PackspireUiFactory.Button(GameCatalog.Roles[id].name,()=>{
    game.UiSetQualificationSeal(id);
    RefreshStatusScreen();
   });
   option.EnableInClassList("ps-selected",meta.qualificationSealId==id);
   qualification.Add(option);
  }
  body.Add(qualification);
  var tail=Container("ps-space-scroll-tail");
  tail.pickingMode=PickingMode.Ignore;
  body.Add(tail);
  mgmtDetailScroll.Add(body);
  mgmtDetailScroll.scrollOffset=Vector2.zero;
  mgmtDetailScroll.schedule.Execute(()=>{
   if(mgmtDetailScroll!=null)mgmtDetailScroll.scrollOffset=Vector2.zero;
  }).ExecuteLater(0);
 }

 void RefreshStatusAppointment(RoleDef role,int level,bool isCurrent){
  if(statusAppointmentEyebrow==null)return;
  statusAppointmentEyebrow.text="PACK 03 / APPOINTMENT ORDER";
  statusAppointmentTitle.text="役職任命書";
  statusAppointmentDetail.text=$"［{role.name}］  Lv.{level}/{role.maxLevel}";
  statusAppointmentAction.text=isCurrent?"現役職":"任命候補";
 }

 void ClearStatusAppointment(){
  if(statusAppointmentEyebrow==null)return;
  statusAppointmentEyebrow.text="PACK 03 / APPOINTMENT ORDER";
  statusAppointmentTitle.text="役職任命書";
  statusAppointmentDetail.text="登録された役職はありません";
  statusAppointmentAction.text="記録待ち";
 }

 void BuildStatusAgain(){RefreshStatusScreen();}
 void RefreshStatusScreen(){
  if(mgmtListScroll==null||renderedScreen!=ScreenId.Status){RebuildScreen(BuildStatus);return;}
  var meta=game.UiMeta;
  var character=CharacterCatalog.Get(meta.selectedCharacterId);
  var allLearned=RoleFrameworkSystem.CoreRoleIds
   .Where(GameCatalog.Roles.ContainsKey)
   .Select(id=>new IdInt(id,Mathf.Max(1,meta.jobLevels.FirstOrDefault(x=>x.id==id)?.value??1)))
   .ToList();
  var learned=FilteredStatusRoles(allLearned);
  if(learned.Count>0&&!learned.Any(x=>x.id==selectedRoleId))selectedRoleId=learned[0].id;
  if(learned.Count==0)selectedRoleId="";
  ClearMgmtOverview();
  mgmtOverviewHost.Add(ManagementCharacterOverview(character,meta));
  PopulateStatusHeader();
  PopulateStatusList(learned,meta);
 RefreshStatusDetail(character,meta,learned);
 }

 #endregion
 #region Vault

 void BuildVault(){
  var meta=game.UiMeta;
  if(vaultFilter<0||vaultFilter>4)vaultFilter=0;
  var stash=CurrentVaultStash(meta);
  if(stash.Count==0&&meta.stash.Count>0&&vaultFilter!=0)vaultFilter=0;
  if(string.IsNullOrEmpty(selectedVaultUid)||!meta.stash.Any(x=>x.uid==selectedVaultUid))
   selectedVaultUid=meta.stash.FirstOrDefault()?.uid??"";

  var shell=BuildVaultFixedView("VAULT  /  ARMORY","保管庫");
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
  if(vaultFixedRoot!=null){
   RefreshVaultFixedDetail(meta);
   return;
  }
  ClearMgmtDetailHero();
  mgmtDetailScroll?.Clear();
  var stash=CurrentVaultStash(meta);
  var selected=meta.stash.FirstOrDefault(x=>x.uid==selectedVaultUid);
  if(selected==null||!stash.Any(x=>x.uid==selectedVaultUid)){
   if(mgmtDetailHero!=null)mgmtDetailHero.style.display=DisplayStyle.None;
   mgmtDetailScroll?.Add(PackspireUiFactory.EmptyState("装備を選択","左の装備棚から記録を選んでください。"));
   return;
  }
  var def=GameCatalog.Items[selected.templateId];
  bool heirloom=IsVaultHeirloom(meta,selected);
  if(!heirloom)vaultRecordPage=0;

  if(vaultRecordPage==1&&heirloom){
   if(mgmtDetailHero!=null)mgmtDetailHero.style.display=DisplayStyle.None;
   mgmtDetailScroll?.Add(BuildVaultHeirloomPage(selected,def));
   return;
  }

  var identity=Container("ps-vault-v11-equipment-hero");
  var metaLine=PackspireUiFactory.Body($"RANK {Mathf.Max(1,selected.temper+1)}　／　{ItemTypeLabel(def.type)}");
  metaLine.AddToClassList("ps-vault-v9-meta");
  metaLine.AddToClassList("ps-vault-v11-meta");
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
  var description=PackspireUiFactory.Body(def.description);
  description.AddToClassList("ps-vault-v9-description");

  var showcase=Container("ps-vault-v11-showcase");
  var artColumn=Container("ps-vault-v11-art-column");
  var artFrame=Container("ps-vault-v11-art-frame");
  artFrame.Add(VaultItemDisplayArt(def.id,"ps-vault-v9-item-art ps-vault-v11-item-art"));
  artColumn.Add(artFrame);
  var durability=PackspireUiFactory.Body($"耐久　{selected.durability} / {def.baseDurability}");
  durability.AddToClassList("ps-vault-v9-durability");
  durability.AddToClassList("ps-vault-v11-durability");
  artColumn.Add(durability);
  showcase.Add(artColumn);
  var infoColumn=Container("ps-vault-v17-info-column");
  infoColumn.Add(metaLine);
  infoColumn.Add(nameRow);
  infoColumn.Add(description);
  infoColumn.Add(VaultOccupancyShape(selected,def));
  showcase.Add(infoColumn);
  identity.Add(showcase);
  SetMgmtDetailHeroSummary(identity);
  identity.pickingMode=PickingMode.Position;
  if(mgmtDetailSummaryHost!=null)mgmtDetailSummaryHost.pickingMode=PickingMode.Position;

  var lower=Container("ps-vault-v9-lower");
  var cardPanel=Container("ps-vault-v9-card-panel");
  var cardHeader=Container("ps-vault-v9-lower-header");
  var cardHeading=new Label("戦闘配達票"){pickingMode=PickingMode.Ignore};
  cardHeading.AddToClassList("ps-vault-v9-lower-title");
  cardHeader.Add(cardHeading);
  cardHeader.Add(EquipmentSealAttributeBadge(def,"ps-vault-v9-seal-attribute"));
  cardPanel.Add(cardHeader);
 var face=BuildEquipmentCardFacePreview(selected,def,game.UiRun);
  if(face!=null){
   face.pickingMode=PickingMode.Position;
   face.AddToClassList("ps-vault-v9-card-open");
   face.tooltip="クリックでカードを拡大";
   face.RegisterCallback<ClickEvent>(evt=>{
    evt.StopPropagation();
    ShowVaultCardModal(selected,def);
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
  if(heirloom){
   var recordButton=VaultPageArrow(true,()=>{
    vaultRecordPage=1;
    RefreshVaultDetail(meta);
   });
   recordButton.AddToClassList("ps-vault-v16-next-item");
   recordButton.tooltip="家宝の記録を見る";
   effectPanel.Add(recordButton);
  }
  effectPanel.Add(actions);
  lower.Add(effectPanel);
 mgmtDetailScroll?.Add(lower);
}

 void ShowVaultCardModal(ItemInstance item,ItemDef def){
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
  var heading=new Label("戦闘配達票"){pickingMode=PickingMode.Ignore};
  heading.AddToClassList("ps-vault-card-modal-heading");
  stage.Add(heading);
  var enlarged=BuildEquipmentCardFacePreview(item,def,game.UiRun);
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
  int minX=layout.Count==0?0:layout.Min(cell=>cell.pos.x);
  int minY=layout.Count==0?0:layout.Min(cell=>cell.pos.y);
  int shapeWidth=layout.Count==0?0:layout.Max(cell=>cell.pos.x)-minX+1;
  int shapeHeight=layout.Count==0?0:layout.Max(cell=>cell.pos.y)-minY+1;
  int offsetX=Mathf.Max(0,(3-shapeWidth)/2);
  int offsetY=Mathf.Max(0,(3-shapeHeight)/2);
  var grid=Container("ps-vault-v9-shape-grid");
  for(int y=0;y<3;y++){
   var row=Container("ps-vault-v9-shape-row");
   for(int x=0;x<3;x++){
    var cell=Container("ps-vault-v9-shape-cell");
    int sourceX=x-offsetX+minX;
    int sourceY=y-offsetY+minY;
    var occupied=layout.FirstOrDefault(value=>value.pos.x==sourceX&&value.pos.y==sourceY);
    bool filled=layout.Any(value=>value.pos.x==sourceX&&value.pos.y==sourceY);
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

 Button VaultPageArrow(bool forward,System.Action onClick){
  var button=PackspireUiFactory.Button("",onClick);
  button.style.scale=new Scale(new Vector3(forward?1f:-1f,1f,1f));
  button.AddToClassList("ps-vault-v11-page-arrow");
  button.AddToClassList(forward?"ps-forward":"ps-back");
  button.tooltip=forward?"家宝の記録を見る":"装備の記録へ戻る";
  return button;
 }

 VisualElement BuildVaultHeirloomPage(ItemInstance item,ItemDef def){
  var page=Container("ps-vault-v9-heirloom-page ps-vault-v11-heirloom-page");
  var pageArrow=VaultPageArrow(false,()=>{
   vaultRecordPage=0;
   RefreshVaultDetail(game.UiMeta);
  });
  var heading=Container("ps-vault-v11-heirloom-heading");
  var eyebrow=new Label("HEIRLOOM  /  LEGACY"){pickingMode=PickingMode.Ignore};
  eyebrow.AddToClassList("ps-vault-v11-heirloom-eyebrow");
  heading.Add(eyebrow);
  var title=PackspireUiFactory.Title(def.name);
  title.AddToClassList("ps-vault-v11-heirloom-title");
  heading.Add(title);
  var subtitle=PackspireUiFactory.Body("遠征を越えて受け継がれた家宝の全記録");
  subtitle.AddToClassList("ps-vault-v11-heirloom-subtitle");
  heading.Add(subtitle);
  page.Add(heading);

  var hero=Container("ps-vault-v11-heirloom-hero");
  var art=Container("ps-vault-v11-heirloom-art");
  art.Add(VaultItemDisplayArt(def.id,"ps-vault-v11-heirloom-art-image"));
  hero.Add(art);
  var history=item.history??new HeirloomHistory();
  var records=Container("ps-vault-v11-heirloom-records");
  records.Add(VaultEffectRecord(
   "家宝の記録",
   "遠征の記憶",
   $"戦闘 {history.battles}　ボス {history.bosses}　敗北 {history.defeats}",
   "ps-vault-v9-heirloom-history"
  ));
  string scars=item.scars==null||item.scars.Count==0
   ?"まだ傷跡は刻まれていません。"
   :string.Join("\n",item.scars.Take(5).Select(scar=>$"◆ {scar.type}　{scar.dungeon}　F{scar.floor}"));
  records.Add(VaultEffectRecord("傷跡","刻まれた履歴",scars,"ps-vault-v9-heirloom-scars"));
  records.Add(VaultEffectRecord(
   "継承",
   def.name,
   "家宝として蓄積した記憶は、次の遠征でも失われません。",
   "ps-vault-v9-heirloom-legacy"
  ));
  hero.Add(records);
  page.Add(hero);
  page.Add(pageArrow);
  return page;
 }

 void BuildVaultAgain(){RefreshVaultScreen(false);}
 void RefreshVaultScreen(bool rebuildList){
  if(mgmtListScroll==null||renderedScreen!=ScreenId.Vault){RebuildScreen(BuildVault);return;}
  CloseVaultSortMenu();
  var meta=game.UiMeta;
  var stash=CurrentVaultStash(meta);
  if(vaultFilter<0||vaultFilter>4)vaultFilter=0;
  if(stash.Count==0&&meta.stash.Count>0&&vaultFilter!=0){
   vaultFilter=0;
   stash=SortVaultStash(meta.stash).ToList();
  }
  if(!stash.Any(x=>x.uid==selectedVaultUid))selectedVaultUid=stash.FirstOrDefault()?.uid??meta.stash.FirstOrDefault()?.uid??"";
  if(rebuildList){
   mgmtListHeader.Clear();
   mgmtListHeader.Add(SelectiveSectionHead("ITEM STORAGE","装備棚"));
   mgmtListHeader.Add(VaultCategoryBar(VaultCategoryLabels,vaultFilter,index=>{
    vaultFilter=index;
    RefreshVaultScreen(true);
   }));
   var active=LoadoutSystem.Active(meta);
   var context=PackspireUiFactory.Body($"使用中荷造り　{active.name}");
   context.AddToClassList("ps-vault-v7-context");
   mgmtListHeader.Add(context);
   if(mgmtVaultFooter!=null){
    mgmtVaultFooter.pickingMode=PickingMode.Position;
    vaultSortMode=Mathf.Clamp(vaultSortMode,0,VaultSortChoices.Count-1);
    UpdateVaultSortMenuSelection();
    if(vaultFixedCount!=null)
     vaultFixedCount.text=$"{stash.Count} / {Mathf.Max(240,meta.stash.Count)}";
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

 #endregion
 #region Compendium

 void BuildCompendium(){
  if(compendiumTab>2)compendiumTab=0;
  var shell=BuildManagementShell("CODEX  /  ARCHIVE","図鑑",ManagementLayout.CompendiumReelDetail,out _,out _);
  screenRoot.Add(shell);
  RefreshCompendiumScreen(true);
 }

 void RefreshCompendiumScreen(bool _){
  if(mgmtListScroll==null||renderedScreen!=ScreenId.Compendium){RebuildScreen(BuildCompendium);return;}
  if(compendiumTab>2)compendiumTab=0;
  var meta=game.UiMeta;
  UpdateCompendiumTabState();
  UpdateCompendiumDiscoveryCount(meta);
  PopulateCompendiumList(meta);
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

 void UpdateCompendiumDiscoveryCount(MetaSave meta){
  if(compendiumDiscoveryCount==null)return;
  int known;
  int total;
  if(compendiumTab==0){
   total=GameCatalog.Items.Count;
   known=GameCatalog.Items.Keys.Count(id=>meta.discoveredItems.Contains(id));
  }else if(compendiumTab==1){
   total=GameCatalog.Roles.Count;
   known=GameCatalog.Roles.Keys.Count(id=>meta.jobLevels.Any(level=>level.id==id&&level.value>0));
  }else{
   total=GameCatalog.Enemies.Count();
   known=GameCatalog.Enemies.Count(enemy=>meta.discoveredEnemies.Contains(enemy.id));
  }
  compendiumDiscoveryCount.text=$"{known} / {total}";
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
   var row=CodexIndexRow(item.id,known?item.name:"？？？",known?ItemTypeLabel(item.type):"未発見",item.id==selectedCompendiumId,()=>{
    selectedCompendiumId=item.id;
    RefreshCompendiumScreen(true);
   },leading);
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
   var row=CodexIndexRow(role.id,known?role.name:"？？？",known?role.kind:"未習得",role.id==selectedCompendiumId,()=>{
    selectedCompendiumId=role.id;
    RefreshCompendiumScreen(true);
   },leading);
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
   var row=CodexIndexRow(enemy.id,known?enemy.name:"？？？",known?$"危険度 {enemy.tier}":"未遭遇",enemy.id==selectedCompendiumId,()=>{
    selectedCompendiumId=enemy.id;
    RefreshCompendiumScreen(true);
   },leading);
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
  if(mgmtDetailHero!=null)mgmtDetailHero.RemoveFromClassList("ps-codex-item-v5-hero");
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
  PrepareCompendiumRecord("item:"+selected.id);
  bool known=meta.discoveredItems.Contains(selected.id);
  if(!known){
   SetMgmtUnknownFocalArt("？");
   SetMgmtDetailHeroSummary(
    new Label("？？？"){pickingMode=PickingMode.Ignore},
    PackspireUiFactory.Body("未発見")
   );
   BuildCompendiumTwoPageRecord(
    "UNKNOWN ITEM","？？？",
    new List<CompendiumEntry>{
     new("未発見","遠征や戦闘で入手すると固定記録が開示されます。","？",true)
    },
    CompendiumUnknownFlavorArt("？"),
    "黒く塗り潰された頁だけが残っている。",
    "入手後に出自の記録が開示される。",
    "塔のどこかに、この品を知る者がいるらしい。"
   );
   return;
  }
  BuildItemCompendiumRecord(selected);
}

 void RefreshRoleCompendiumDetail(MetaSave meta){
  if(!GameCatalog.Roles.ContainsKey(selectedCompendiumId))return;
  var selected=GameCatalog.Roles[selectedCompendiumId];
  PrepareCompendiumRecord("role:"+selected.id);
  bool known=meta.jobLevels.Any(x=>x.id==selected.id&&x.value>0);
  if(!known){
   SetMgmtUnknownFocalArt("役");
   SetMgmtDetailHeroSummary(
    new Label("？？？"){pickingMode=PickingMode.Ignore},
    PackspireUiFactory.Body("未習得")
   );
   BuildCompendiumTwoPageRecord(
    "UNKNOWN ROLE","？？？",
    new List<CompendiumEntry>{
     new("解放の手掛かり",RoleUnlockSummary(selected),"鍵"),
     new("未習得","習得後に効果と成長記録が開示されます。","？",true)
    },
    CompendiumUnknownFlavorArt("役"),
    "名を失った役職の頁。系譜だけがかすかに残る。",
    "条件を満たした遠征者だけが、その技法の起源を知る。",
    "まだ誰も完全な形では語り継いでいない。"
   );
   return;
  }
  var level=meta.jobLevels.First(x=>x.id==selected.id);
  SetMgmtDetailHeroArt(Atlas(game.UiRoleArt,RoleUv(selected.id),"ps-mgmt-detail-art-image"));
  SetMgmtDetailHeroSummary(
   PackspireUiFactory.Title(selected.name),
   PackspireUiFactory.Body($"{selected.kind}　最大Lv.{selected.maxLevel}"),
   PackspireUiFactory.Body($"習得 Lv.{level.value}")
  );
  BuildCompendiumTwoPageRecord(
   "ROLE CHRONICLE",selected.name,
   new List<CompendiumEntry>{
    new("役職概要",string.IsNullOrEmpty(selected.description)?"固定説明なし":selected.description,"記"),
    new("重ね効果",RoleContributionSummary(selected.reactionContributions,"登録なし"),"重"),
    new("現職効果",RoleContributionSummary(selected.currentRoleContributions,"登録なし"),"現"),
    new("成長記録",$"Lv.7: {game.UiRoleMilestone(selected.id,false)} / 最大: {game.UiRoleMilestone(selected.id,true)}","成"),
    new("初期カード",selected.startingCardIds?.Length>0?string.Join(" / ",selected.startingCardIds):"登録なし","札"),
    new("解放経路",RoleUnlockSummary(selected),"鍵")
   },
   Atlas(game.UiRoleArt,RoleUv(selected.id),"ps-codex-v4-flavor-art-image"),
   string.IsNullOrEmpty(selected.description)
    ?$"{selected.name}の技法を記した古い手引きが残されている。"
    :selected.description,
   $"{selected.name}は単独の流派ではなく、遠征者たちが積み重ねた経験から形作られた役職である。",
   "習得者ごとに重ねた技法が異なるため、同じ役職名でも戦い方には大きな違いが現れるという。"
  );
}

 void RefreshEnemyCompendiumDetail(MetaSave meta){
  var selected=GameCatalog.Enemies.FirstOrDefault(x=>x.id==selectedCompendiumId);
  if(selected==null){
   SetMgmtUnknownFocalArt("敵");
   mgmtDetailScroll.Add(PackspireUiFactory.EmptyState("記録なし","遭遇した敵がここへ記録されます。"));
   return;
  }
  PrepareCompendiumRecord("enemy:"+selected.id);
  bool known=meta.discoveredEnemies.Contains(selected.id);
  if(!known){
   SetMgmtUnknownFocalArt("敵");
   SetMgmtDetailHeroSummary(
    new Label("？？？"){pickingMode=PickingMode.Ignore},
    PackspireUiFactory.Body("未遭遇")
   );
   BuildCompendiumTwoPageRecord(
    "UNKNOWN HOSTILE","？？？",
    new List<CompendiumEntry>{
     new("出現域","塔の各階層","塔"),
     new("未遭遇","遭遇後に行動と生態の記録が開示されます。","？",true)
    },
    CompendiumUnknownFlavorArt("敵"),
    "目撃者の証言だけが残り、姿はまだ記録されていない。",
    "塔の暗部から現れるという以外、確かな出自は不明。",
    "遭遇から帰還した者が、新しい頁を書き足すことになる。"
   );
   return;
  }
  SetMgmtDetailHeroArt(EnemyPortrait(selected,"ps-mgmt-detail-art-image"));
  SetMgmtDetailHeroSummary(
   PackspireUiFactory.Title(selected.name),
   PackspireUiFactory.Body($"危険度 {selected.tier}"),
   PackspireUiFactory.Body($"基礎HP {selected.hp}")
  );
  BuildCompendiumTwoPageRecord(
   "HOSTILE ARCHIVE",selected.name,
   new List<CompendiumEntry>{
    new("行動順",EnemyActionSummary(selected),"剣"),
    new("盤面思考",EnemyBehaviorLabel(selected.boardBehavior),"思"),
    new("索敵と移動",$"視界 {selected.boardSightRange} / 移動 {selected.boardMoveSteps}","眼"),
    new("巡回範囲",$"半径 {selected.boardPatrolRadius}マス","巡"),
    new("耐性と弱点","追加解析待ち","盾",true),
    new("出現記録","塔の各階層 / 遭遇済み","塔")
   },
   EnemyPortrait(selected,"ps-codex-v4-flavor-art-image"),
   $"{selected.name}との遭遇から回収された観察記録。戦闘時の行動だけでなく、盤面上の習性も追記されている。",
   $"危険度{selected.tier}に分類される塔の生物。行動様式は「{EnemyBehaviorLabel(selected.boardBehavior)}」として記録された。",
   "古い遠征記録には異なる姿の目撃談もある。すべてが同種なのか、塔が生んだ変異なのかは判明していない。"
  );
}

 void BuildCompendiumAgain(){RefreshCompendiumScreen(true);}

 #endregion
}
}
