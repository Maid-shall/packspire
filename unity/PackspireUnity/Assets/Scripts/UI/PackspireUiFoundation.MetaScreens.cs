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

  var shell=BuildManagementShell("STATUS  /  RANK","ステータス",ScreenId.Status,out var listScroll,out _,out _);
  screenRoot.Add(shell);

  mgmtListHeader.Clear();
  mgmtListHeader.Add(ManagementCharacterHeader(character,meta));

  PopulateStatusList(learned,meta);
  RefreshStatusArtDetail(character,meta,learned);
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
   string badge=equipped?"装備中":role.kind;
   var icon=SmallAtlasIcon(game.UiRoleArt,RoleUv(role.id));
   var row=ManagementListRow(role.id,icon,role.name,$"Lv.{level.value}/{role.maxLevel}　{badge}",level.id==selectedRoleId,()=>{
    selectedRoleId=role.id;
    UpdateMgmtListSelection(selectedRoleId);
    RefreshStatusArtDetail(CharacterCatalog.Get(meta.selectedCharacterId),meta,learned);
   });
   mgmtListScroll.Add(row);
  }
  RestoreMgmtListScroll();
 }

 void RefreshStatusArtDetail(CharacterDef character,MetaSave meta,System.Collections.Generic.List<IdInt> learned){
  ClearMgmtArt();
  ClearMgmtDetail();

  if(!string.IsNullOrEmpty(selectedRoleId)&&learned.Any(x=>x.id==selectedRoleId)){
   var role=GameCatalog.Roles[selectedRoleId];
   SetMgmtArt(Atlas(game.UiRoleArt,RoleUv(role.id),"ps-mgmt-art-image"),"ps-mgmt-art-square");
  }else SetMgmtArt(CharacterPortraitFront(character,"ps-mgmt-art-image"),"ps-mgmt-art-portrait");

  mgmtDetailScroll.Add(ManagementSection("固有特性",character.traitName+"\n"+character.traitText));
  mgmtDetailScroll.Add(ManagementSection("能動スキル",character.activeSkillName+"\n"+character.activeSkillText));

  if(string.IsNullOrEmpty(selectedRoleId)||!learned.Any(x=>x.id==selectedRoleId))return;
  var selectedLevel=learned.First(x=>x.id==selectedRoleId);
  var selected=GameCatalog.Roles[selectedRoleId];
  mgmtDetailScroll.Add(ManagementSection("役職",selected.name));
  mgmtDetailScroll.Add(ManagementSection("現在レベル",$"Lv.{selectedLevel.value} / {selected.maxLevel}"));
  mgmtDetailScroll.Add(ManagementSection("分類",selected.kind));
  mgmtDetailScroll.Add(ManagementSection("基礎効果",selected.description,selectedLevel.value<1));
  mgmtDetailScroll.Add(ManagementSection("Lv.1 効果",selected.description,selectedLevel.value<1));
  mgmtDetailScroll.Add(ManagementSection("Lv.7 専用効果",game.UiRoleMilestone(selected.id,false),selectedLevel.value<7));
  mgmtDetailScroll.Add(ManagementSection($"Lv.{selected.maxLevel} 最大効果",game.UiRoleMilestone(selected.id,true),selectedLevel.value<selected.maxLevel));
  if(selectedLevel.value<7)
   mgmtDetailScroll.Add(ManagementSection("次の効果","Lv.7 で解放\n"+game.UiRoleMilestone(selected.id,false)));
  else if(selectedLevel.value<selected.maxLevel)
   mgmtDetailScroll.Add(ManagementSection("次の効果",$"Lv.{selected.maxLevel} で解放\n"+game.UiRoleMilestone(selected.id,true)));
  if(selected.id==meta.currentRole)
   mgmtDetailScroll.Add(ManagementSection("状態","● 現在装備中の主役職"));
  else mgmtDetailScroll.Add(ManagementSection("転職","専用イベントまたは施設から変更できます。"));
 }

 void BuildStatusAgain(){RefreshStatusScreen();}
 void RefreshStatusScreen(){
  if(mgmtListScroll==null||renderedScreen!=ScreenId.Status){screenRoot.Clear();BuildStatus();return;}
  var meta=game.UiMeta;
  var character=CharacterCatalog.Get(meta.selectedCharacterId);
  var learned=meta.jobLevels.Where(x=>x.value>0&&GameCatalog.Roles.ContainsKey(x.id)).ToList();
  if(learned.Count>0&&!learned.Any(x=>x.id==selectedRoleId))selectedRoleId=learned[0].id;
  mgmtListHeader.Clear();
  mgmtListHeader.Add(ManagementCharacterHeader(character,meta));
  PopulateStatusList(learned,meta);
  RefreshStatusArtDetail(character,meta,learned);
 }

 void BuildVault(){
  var meta=game.UiMeta;
  var stash=FilteredVaultStash(meta).ToList();
  if(stash.Count==0&&meta.stash.Count>0&&vaultFilter!=0)vaultFilter=0;
  if(string.IsNullOrEmpty(selectedVaultUid)||!meta.stash.Any(x=>x.uid==selectedVaultUid))
   selectedVaultUid=meta.stash.FirstOrDefault()?.uid??"";

  var shell=BuildManagementShell("VAULT  /  ARMORY","保管庫",ScreenId.Vault,out _,out _,out _);
  screenRoot.Add(shell);
  RefreshVaultScreen(true);
 }

 System.Collections.Generic.IEnumerable<ItemInstance> FilteredVaultStash(MetaSave meta){
  if(meta.stash==null)return System.Array.Empty<ItemInstance>();
  if(vaultFilter==1)return meta.stash.Where(x=>x.uid==meta.selectedHeirloomUid);
  if(vaultFilter==2)return meta.stash.Where(x=>VaultItemInLoadout(meta,x.uid));
  return meta.stash;
 }

 void PopulateVaultList(MetaSave meta,System.Collections.Generic.List<ItemInstance> stash){
  SaveMgmtListScroll();
  mgmtListScroll.Clear();
  if(stash.Count==0){
   mgmtListScroll.Add(PackspireUiFactory.EmptyState("該当する装備なし",vaultFilter==0?"遠征から帰還すると装備が記録されます。":"条件を変えると他の装備が表示されます。"));
   RestoreMgmtListScroll();
   return;
  }
  foreach(var item in stash){
   var def=GameCatalog.Items[item.templateId];
   bool heir=item.uid==meta.selectedHeirloomUid;
   bool inUse=VaultItemInLoadout(meta,item.uid);
   string badges=(heir?"★ ":"")+(inUse?"● ":"");
   var icon=SmallAtlasIcon(game.UiEquipmentArt,ItemUv(def.id));
   var row=ManagementListRow(item.uid,icon,def.name,$"{badges}{ItemTypeLabel(def.type)}　+{item.temper}",item.uid==selectedVaultUid,()=>{
    selectedVaultUid=item.uid;
    UpdateMgmtListSelection(selectedVaultUid);
    RefreshVaultArtDetail(meta);
   });
   mgmtListScroll.Add(row);
  }
  RestoreMgmtListScroll();
 }

 void RefreshVaultArtDetail(MetaSave meta){
  ClearMgmtArt();
  ClearMgmtDetail();
  var stash=FilteredVaultStash(meta).ToList();
  var selected=meta.stash.FirstOrDefault(x=>x.uid==selectedVaultUid);
  if(selected==null||!stash.Any(x=>x.uid==selectedVaultUid)){
   mgmtDetailScroll.Add(PackspireUiFactory.EmptyState("装備を選択","左の一覧から装備を選ぶと詳細が表示されます。"));
   return;
  }
  var def=GameCatalog.Items[selected.templateId];
  SetMgmtArt(Atlas(game.UiEquipmentArt,ItemUv(def.id),"ps-mgmt-art-image"),"ps-mgmt-art-square");

  mgmtDetailScroll.Add(ManagementSection("",def.name));
  mgmtDetailScroll.Add(ManagementSection("分類",$"{ItemTypeLabel(def.type)}　{def.cells.Length}マス"));
  mgmtDetailScroll.Add(ManagementSection("基本性能",def.description));
  mgmtDetailScroll.Add(ManagementSection("耐久・使用",$"耐久 {selected.durability}/6　使用 {selected.uses}回　鍛錬 +{selected.temper}"));
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
  if(selected.uid==meta.selectedHeirloomUid){
   mgmtDetailScroll.Add(ManagementSection("家宝","★ 現在の家宝"));
   if(selected.history!=null&&(selected.history.battles>0||selected.history.bosses>0||selected.history.defeats>0))
    mgmtDetailScroll.Add(ManagementSection("戦歴",$"戦闘 {selected.history.battles}　ボス {selected.history.bosses}　敗北 {selected.history.defeats}"));
   if(selected.scars!=null&&selected.scars.Count>0){
    var scarLines=selected.scars.Take(6).Select(s=>$"{s.type}　{s.dungeon} L{s.floor}");
    mgmtDetailScroll.Add(ManagementSection("傷跡",string.Join("\n",scarLines)));
   }
  }
  if(selected.insured||selected.heirloomCertified)
   mgmtDetailScroll.Add(ManagementSection("保護",selected.insured?"保険加入":selected.heirloomCertified?"家宝認証済":""));
  var heirButton=PackspireUiFactory.Button(selected.uid==meta.selectedHeirloomUid?"★ 家宝に設定中":"家宝に設定",()=>{
   game.UiSelectHeirloom(selected.uid);
   RefreshVaultScreen(true);
   ShowToast("家宝として記録しました");
  });
  heirButton.AddToClassList("ps-chrome-action");
  heirButton.AddToClassList("ps-mgmt-action");
  mgmtDetailScroll.Add(heirButton);
 }

 void BuildVaultAgain(){RefreshVaultScreen(false);}
 void RefreshVaultScreen(bool rebuildList){
  if(mgmtListScroll==null||renderedScreen!=ScreenId.Vault){screenRoot.Clear();BuildVault();return;}
  var meta=game.UiMeta;
  var stash=FilteredVaultStash(meta).ToList();
  if(stash.Count==0&&meta.stash.Count>0&&vaultFilter!=0){
   vaultFilter=0;
   stash=meta.stash.ToList();
  }
  if(!stash.Any(x=>x.uid==selectedVaultUid))selectedVaultUid=stash.FirstOrDefault()?.uid??meta.stash.FirstOrDefault()?.uid??"";
  if(rebuildList){
   mgmtListHeader.Clear();
   mgmtListHeader.Add(ManagementFilterBar(new[]{"すべて","家宝","使用中"},vaultFilter,index=>{
    vaultFilter=index;
    RefreshVaultScreen(true);
   }));
   var active=LoadoutSystem.Active(meta);
   mgmtListHeader.Add(PackspireUiFactory.Body($"使用中荷造り　{active.name}　／　保管 {meta.stash.Count}"));
   PopulateVaultList(meta,stash);
  }else UpdateMgmtListSelection(selectedVaultUid);
  RefreshVaultArtDetail(meta);
 }

 void BuildCompendium(){
  var shell=BuildManagementShell("CODEX  /  ARCHIVE","図鑑",ScreenId.Compendium,out _,out _,out _);
  screenRoot.Add(shell);
  RefreshCompendiumScreen(true);
 }

 void RefreshCompendiumScreen(bool rebuildList){
  if(mgmtListScroll==null||renderedScreen!=ScreenId.Compendium){screenRoot.Clear();BuildCompendium();return;}
  var meta=game.UiMeta;
  if(rebuildList){
   mgmtListHeader.Clear();
   mgmtListHeader.Add(ManagementFilterBar(new[]{"装備","役職","敵","ダンジョン"},compendiumTab,tab=>{
    compendiumTab=tab;
    selectedCompendiumId="";
    RefreshCompendiumScreen(true);
   }));
   PopulateCompendiumList(meta);
  }
  RefreshCompendiumArtDetail(meta);
 }

 void PopulateCompendiumList(MetaSave meta){
  SaveMgmtListScroll();
  mgmtListScroll.Clear();
  if(compendiumTab==0)PopulateItemCompendiumList(meta);
  else if(compendiumTab==1)PopulateRoleCompendiumList(meta);
  else if(compendiumTab==2)PopulateEnemyCompendiumList(meta);
  else PopulateDungeonCompendiumList(meta);
  RestoreMgmtListScroll();
 }

 void PopulateItemCompendiumList(MetaSave meta){
  var values=GameCatalog.Items.Values.ToArray();
  if(string.IsNullOrEmpty(selectedCompendiumId)||!GameCatalog.Items.ContainsKey(selectedCompendiumId))selectedCompendiumId=values[0].id;
  foreach(var item in values){
   bool known=meta.discoveredItems.Contains(item.id);
   var icon=SmallAtlasIcon(game.UiEquipmentArt,ItemUv(item.id),!known);
   var row=ManagementListRow(item.id,icon,known?item.name:"？？？",ItemTypeLabel(item.type),item.id==selectedCompendiumId,()=>{
    selectedCompendiumId=item.id;
    UpdateMgmtListSelection(selectedCompendiumId);
    RefreshCompendiumArtDetail(meta);
   });
   if(!known)row.AddToClassList("ps-mgmt-list-unknown");
   mgmtListScroll.Add(row);
  }
 }

 void PopulateRoleCompendiumList(MetaSave meta){
  var values=GameCatalog.Roles.Values.ToArray();
  if(string.IsNullOrEmpty(selectedCompendiumId)||!GameCatalog.Roles.ContainsKey(selectedCompendiumId))selectedCompendiumId=values[0].id;
  foreach(var role in values){
   bool known=meta.jobLevels.Any(x=>x.id==role.id&&x.value>0);
   var icon=SmallAtlasIcon(game.UiRoleArt,RoleUv(role.id),!known);
   var row=ManagementListRow(role.id,icon,known?role.name:"？？？",role.kind,role.id==selectedCompendiumId,()=>{
    selectedCompendiumId=role.id;
    UpdateMgmtListSelection(selectedCompendiumId);
    RefreshCompendiumArtDetail(meta);
   });
   if(!known)row.AddToClassList("ps-mgmt-list-unknown");
   mgmtListScroll.Add(row);
  }
 }

 void PopulateEnemyCompendiumList(MetaSave meta){
  var values=GameCatalog.Enemies;
  if(string.IsNullOrEmpty(selectedCompendiumId)||!values.Any(x=>x.id==selectedCompendiumId))selectedCompendiumId=values[0].id;
  foreach(var enemy in values){
   bool known=meta.discoveredEnemies.Contains(enemy.id);
   var icon=SmallPortraitIcon(EnemyPortrait(enemy,"ps-mgmt-thumb-inner"),!known);
   var row=ManagementListRow(enemy.id,icon,known?enemy.name:"？？？",$"危険度 {enemy.tier}",enemy.id==selectedCompendiumId,()=>{
    selectedCompendiumId=enemy.id;
    UpdateMgmtListSelection(selectedCompendiumId);
    RefreshCompendiumArtDetail(meta);
   });
   if(!known)row.AddToClassList("ps-mgmt-list-unknown");
   mgmtListScroll.Add(row);
  }
 }

 void PopulateDungeonCompendiumList(MetaSave meta){
  var values=GameCatalog.Dungeons;
  if(string.IsNullOrEmpty(selectedCompendiumId)||!values.Any(x=>x.id==selectedCompendiumId))selectedCompendiumId=values[0].id;
  for(int i=0;i<values.Length;i++){
   var dungeon=values[i];
   bool known=i<meta.dungeonsUnlocked||meta.dungeonDiscoveries.Contains(dungeon.id);
   var icon=SmallAtlasIcon(game.UiDungeonArt,DungeonUv(dungeon.id),!known);
   var row=ManagementListRow(dungeon.id,icon,known?dungeon.name:"？？？",known?"探索記録あり":"未踏",dungeon.id==selectedCompendiumId,()=>{
    selectedCompendiumId=dungeon.id;
    UpdateMgmtListSelection(selectedCompendiumId);
    RefreshCompendiumArtDetail(meta);
   });
   if(!known)row.AddToClassList("ps-mgmt-list-unknown");
   mgmtListScroll.Add(row);
  }
 }

 void RefreshCompendiumArtDetail(MetaSave meta){
  ClearMgmtArt();
  ClearMgmtDetail();
  if(compendiumTab==0)RefreshItemCompendiumDetail(meta);
  else if(compendiumTab==1)RefreshRoleCompendiumDetail(meta);
  else if(compendiumTab==2)RefreshEnemyCompendiumDetail(meta);
  else RefreshDungeonCompendiumDetail(meta);
 }

 void RefreshItemCompendiumDetail(MetaSave meta){
  if(!GameCatalog.Items.ContainsKey(selectedCompendiumId))return;
  var selected=GameCatalog.Items[selectedCompendiumId];
  bool known=meta.discoveredItems.Contains(selected.id);
  SetMgmtArt(Atlas(game.UiEquipmentArt,ItemUv(selected.id),"ps-mgmt-art-image"),"ps-mgmt-art-square",!known);
  if(!known){
   mgmtDetailScroll.Add(ManagementSection("未発見","まだ詳細記録がありません。"));
   mgmtDetailScroll.Add(ManagementSection("ヒント","遠征や戦闘で入手すると記録されます。"));
   return;
  }
  mgmtDetailScroll.Add(ManagementSection("",selected.name));
  mgmtDetailScroll.Add(ManagementSection("分類",ItemTypeLabel(selected.type)));
  mgmtDetailScroll.Add(ManagementSection("概要",selected.description));
  mgmtDetailScroll.Add(ManagementSection("形状",$"{selected.cells.Length}マス"));
  if(selected.cells.Length>0)
   mgmtDetailScroll.Add(ManagementSection("属性",string.Join("・",selected.cells.Select(x=>ElementLabel(x.element)))));
  if(!string.IsNullOrEmpty(selected.linkRule))
   mgmtDetailScroll.Add(ManagementSection("LINK",selected.linkRule));
 }

 void RefreshRoleCompendiumDetail(MetaSave meta){
  if(!GameCatalog.Roles.ContainsKey(selectedCompendiumId))return;
  var selected=GameCatalog.Roles[selectedCompendiumId];
  bool known=meta.jobLevels.Any(x=>x.id==selected.id&&x.value>0);
  SetMgmtArt(Atlas(game.UiRoleArt,RoleUv(selected.id),"ps-mgmt-art-image"),"ps-mgmt-art-square",!known);
  if(!known){
   mgmtDetailScroll.Add(ManagementSection("未習得","習得前の役職です。"));
   mgmtDetailScroll.Add(ManagementSection("ヒント",selected.kind+"系の役職。イベントや遠征で習得できます。"));
   return;
  }
  var level=meta.jobLevels.First(x=>x.id==selected.id);
  mgmtDetailScroll.Add(ManagementSection("",selected.name));
  mgmtDetailScroll.Add(ManagementSection("分類",selected.kind));
  mgmtDetailScroll.Add(ManagementSection("概要",selected.description));
  mgmtDetailScroll.Add(ManagementSection("習得レベル",$"Lv.{level.value} / {selected.maxLevel}"));
  mgmtDetailScroll.Add(ManagementSection("Lv.7 効果",game.UiRoleMilestone(selected.id,false),level.value<7));
  mgmtDetailScroll.Add(ManagementSection($"Lv.{selected.maxLevel} 効果",game.UiRoleMilestone(selected.id,true),level.value<selected.maxLevel));
 }

 void RefreshEnemyCompendiumDetail(MetaSave meta){
  var selected=GameCatalog.Enemies.FirstOrDefault(x=>x.id==selectedCompendiumId);
  if(selected==null)return;
  bool known=meta.discoveredEnemies.Contains(selected.id);
  SetMgmtArt(EnemyPortrait(selected,"ps-mgmt-art-image"),"ps-mgmt-art-portrait",!known);
  if(!known){
   mgmtDetailScroll.Add(ManagementSection("未発見","遭遇記録がありません。"));
   mgmtDetailScroll.Add(ManagementSection("ヒント",$"危険度 {selected.tier} 付近で遭遇する可能性があります。"));
   return;
  }
  mgmtDetailScroll.Add(ManagementSection("",selected.name));
  mgmtDetailScroll.Add(ManagementSection("危険度",selected.tier.ToString()));
  mgmtDetailScroll.Add(ManagementSection("基礎HP",selected.hp.ToString()));
  mgmtDetailScroll.Add(ManagementSection("行動",string.Join("・",selected.damages.Select(x=>x==0?"特殊行動":$"攻撃{x}"))));
 }

 void RefreshDungeonCompendiumDetail(MetaSave meta){
  var selected=GameCatalog.Dungeons.FirstOrDefault(x=>x.id==selectedCompendiumId);
  if(selected==null)return;
  int index=System.Array.IndexOf(GameCatalog.Dungeons,selected);
  bool known=index<meta.dungeonsUnlocked||meta.dungeonDiscoveries.Contains(selected.id);
  SetMgmtArt(Atlas(game.UiDungeonArt,DungeonUv(selected.id),"ps-mgmt-art-image"),"ps-mgmt-art-square",!known);
  if(!known){
   mgmtDetailScroll.Add(ManagementSection("未踏","まだ遠征記録がありません。"));
   mgmtDetailScroll.Add(ManagementSection("ヒント","遠征準備で解禁条件を確認できます。"));
   return;
  }
  mgmtDetailScroll.Add(ManagementSection("",selected.name));
  mgmtDetailScroll.Add(ManagementSection("概要",selected.description));
  mgmtDetailScroll.Add(ManagementSection("規模",$"戦闘区画 {selected.battles}　敵強度 x{selected.hpScale:0.##}"));
  mgmtDetailScroll.Add(ManagementSection("報酬倍率",$"x{selected.goldScale:0.##}"));
 }

 void BuildCompendiumAgain(){RefreshCompendiumScreen(false);}

 void BuildFaction(){
  var meta=game.UiMeta;if(string.IsNullOrEmpty(selectedFactionId)||!GameCatalog.Factions.Any(x=>x.id==selectedFactionId))selectedFactionId=meta.currentFaction;
  var desk=TabletopDesk("ps-faction-workspace");screenRoot.Add(desk);var board=Container("ps-expanded-corkboard");desk.Add(board);
  board.Add(ChromeBrand("FACTION  /  LEDGER","勢力関係図"));
  board.Add(PackspireUiFactory.Body("印章を選ぶと、右の記録帳に詳細が開きます。"));
  var nodes=Container("ps-faction-nodes");board.Add(nodes);
  int nodeIndex=0;foreach(var faction in GameCatalog.Factions){float rep=meta.factionRep.FirstOrDefault(x=>x.id==faction.id)?.value??0;var node=new Button(()=>{selectedFactionId=faction.id;BuildFactionAgain();}){tooltip=faction.name};node.AddToClassList("ps-faction-pin");node.AddToClassList("ps-faction-pin-"+(nodeIndex++));if(faction.id==selectedFactionId)node.AddToClassList("ps-selected");node.Add(Atlas(game.UiFactionArt,FactionUv(faction.id),"ps-faction-pin-art"));node.Add(PackspireUiFactory.Title(faction.name));node.Add(PackspireUiFactory.Body($"貢献 {rep:0}"));nodes.Add(node);}
  var selected=GameCatalog.Factions.First(x=>x.id==selectedFactionId);float value=meta.factionRep.FirstOrDefault(x=>x.id==selected.id)?.value??0;int rank=Mathf.Clamp(Mathf.FloorToInt(value/25f),0,selected.ranks.Length-1);
  var journal=Container("ps-detail-journal");desk.Add(journal);
  var detail=Container("ps-detail-journal-copy");journal.Add(detail);
  detail.Add(ChromeBrand("SEAL  /  RANK",selected.name));
  detail.Add(Atlas(game.UiFactionArt,FactionUv(selected.id),"ps-faction-detail-art"));
  detail.Add(PackspireUiFactory.Body(selected.description));
  detail.Add(PackspireUiFactory.Body($"現在階級　{selected.ranks[rank]}\n貢献度　{value:0}"));
  var progress=Container("ps-progress");var fill=Container("ps-progress-fill");fill.style.width=Length.Percent(Mathf.Clamp01(value/75f)*100f);progress.Add(fill);detail.Add(progress);
  var change=PackspireUiFactory.Button(selected.id==meta.currentFaction?"● 現在の所属":"20Gで所属を変更",()=>{if(game.UiChangeFaction(selected.id))ShowToast(selected.name+"へ所属を変更しました");BuildFactionAgain();});
  change.AddToClassList("ps-chrome-action");
  if(selected.id!=meta.currentFaction&&meta.baseGold<20)change.SetEnabled(false);detail.Add(change);
  var requests=Container("ps-request-rail");requests.Add(PackspireUiFactory.Body("依頼書留め　—　新しい依頼は遠征準備へ記録されます"));board.Add(requests);desk.Add(TabletopBack());
 }
 void BuildFactionAgain(){screenRoot.Clear();BuildFaction();}
}
}
