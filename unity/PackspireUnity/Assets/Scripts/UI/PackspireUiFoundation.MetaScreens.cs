using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire {
public sealed partial class PackspireUiFoundation {
 void BuildStatus(){
  var meta=game.UiMeta;var learned=meta.jobLevels.Where(x=>x.value>0&&GameCatalog.Roles.ContainsKey(x.id)).ToList();
  if(learned.Count==0){screenRoot.Add(BookShell("役職記録",PackspireUiFactory.EmptyState("役職記録なし","役職を習得するとここへ記録されます。")));return;}
  if(string.IsNullOrEmpty(selectedRoleId)||!learned.Any(x=>x.id==selectedRoleId))selectedRoleId=learned.FirstOrDefault(x=>x.id==meta.currentRole)?.id??learned[0].id;
  var shell=BookShell("役職記録",null);screenRoot.Add(shell);var pages=shell.Q<VisualElement>("book-pages");var left=Page("習得役職");var right=Page("選択中の役職");pages.Add(left);pages.Add(right);
  var list=Container("ps-record-list");left.Add(list);
  foreach(var level in learned){var role=GameCatalog.Roles[level.id];var row=RecordButton(role.name,$"Lv.{level.value}/{role.maxLevel}　{role.kind}",Atlas(game.UiRoleArt,RoleUv(role.id),"ps-record-thumb"),level.id==selectedRoleId,()=>{selectedRoleId=level.id;BuildStatusAgain();});list.Add(row);}
  var selectedLevel=learned.First(x=>x.id==selectedRoleId);var selected=GameCatalog.Roles[selectedRoleId];right.Add(Atlas(game.UiRoleArt,RoleUv(selected.id),"ps-detail-art"));right.Add(PackspireUiFactory.Title($"{selected.name}　Lv.{selectedLevel.value}/{selected.maxLevel}"));right.Add(PackspireUiFactory.Body(selected.kind+"\n"+selected.description));
  var milestones=Container("ps-milestones");milestones.Add(Milestone(1,selectedLevel.value,"基礎効果",selected.description));milestones.Add(Milestone(7,selectedLevel.value,"専用効果",game.UiRoleMilestone(selected.id,false)));milestones.Add(Milestone(selected.maxLevel,selectedLevel.value,"最大レベル効果",game.UiRoleMilestone(selected.id,true)));right.Add(milestones);
  right.Add(PackspireUiFactory.Body(selected.id==meta.currentRole?"● 現在の役職":"転職は専用イベントまたは施設から行います"));AddBookTabs(shell,ScreenId.Status);
 }
 void BuildStatusAgain(){screenRoot.Clear();BuildStatus();}

 void BuildVault(){
  var meta=game.UiMeta;if(string.IsNullOrEmpty(selectedVaultUid)||!meta.stash.Any(x=>x.uid==selectedVaultUid))selectedVaultUid=meta.stash.FirstOrDefault()?.uid??"";
  var shell=BookShell("保管庫",null);screenRoot.Add(shell);var pages=shell.Q<VisualElement>("book-pages");var left=Page("所持装備");var right=Page("装備の記録");pages.Add(left);pages.Add(right);
  var active=LoadoutSystem.Active(meta);left.Add(PackspireUiFactory.Body($"使用中　{active.name}\n保管数　{meta.stash.Count}　　所持金　{meta.baseGold}G"));
  var grid=Container("ps-vault-grid");left.Add(grid);
  foreach(var item in meta.stash){var def=GameCatalog.Items[item.templateId];bool heir=item.uid==meta.selectedHeirloomUid;var tile=AtlasButton(game.UiEquipmentArt,ItemUv(def.id),(heir?"★ ":"")+def.name,item.uid==selectedVaultUid,()=>{selectedVaultUid=item.uid;BuildVaultAgain();});grid.Add(tile);}
  var selected=meta.stash.FirstOrDefault(x=>x.uid==selectedVaultUid);if(selected==null)right.Add(PackspireUiFactory.EmptyState("装備はまだありません","遠征から帰還すると、持ち帰った装備がここへ記録されます。"));else{
   var def=GameCatalog.Items[selected.templateId];right.Add(Atlas(game.UiEquipmentArt,ItemUv(def.id),"ps-detail-art"));right.Add(PackspireUiFactory.Title(def.name));right.Add(PackspireUiFactory.Body(def.description));
   right.Add(PackspireUiFactory.Body($"{ItemTypeLabel(def.type)}　{def.cells.Length}マス\n耐久　{selected.durability}/6　　鍛錬　+{selected.temper}\n使用　{selected.uses}回　　傷跡　{selected.scars.Count}"));
   if(!string.IsNullOrEmpty(def.linkRule)){right.Add(PackspireUiFactory.Title("LINK効果"));right.Add(PackspireUiFactory.Body(def.linkRule));}
   var actions=Container("ps-detail-actions");var heirButton=PackspireUiFactory.Button(selected.uid==meta.selectedHeirloomUid?"★ 家宝に設定中":"家宝に設定",()=>{game.UiSelectHeirloom(selected.uid);BuildVaultAgain();ShowToast("家宝として記録しました");});actions.Add(heirButton);
   int price=30*(selected.temper+1);var temper=PackspireUiFactory.Button(selected.temper>=5?"鍛錬上限":"鍛錬する　"+price+"G",()=>{if(game.UiTemper(selected.uid))ShowToast("鍛錬が完了しました");BuildVaultAgain();});if(selected.temper>=5||meta.baseGold<price)temper.SetEnabled(false);actions.Add(temper);right.Add(actions);
  }
  AddBookTabs(shell,ScreenId.Vault);
 }
 void BuildVaultAgain(){screenRoot.Clear();BuildVault();}

 void BuildFaction(){
  var meta=game.UiMeta;if(string.IsNullOrEmpty(selectedFactionId)||!GameCatalog.Factions.Any(x=>x.id==selectedFactionId))selectedFactionId=meta.currentFaction;
  var desk=TabletopDesk("ps-faction-workspace");screenRoot.Add(desk);var board=Container("ps-expanded-corkboard");desk.Add(board);board.Add(PackspireUiFactory.Title("勢力関係図"));board.Add(PackspireUiFactory.Body("印章を選ぶと、右下の記録帳に詳細が開きます。"));var nodes=Container("ps-faction-nodes");board.Add(nodes);
  int nodeIndex=0;foreach(var faction in GameCatalog.Factions){float rep=meta.factionRep.FirstOrDefault(x=>x.id==faction.id)?.value??0;var node=new Button(()=>{selectedFactionId=faction.id;BuildFactionAgain();}){tooltip=faction.name};node.AddToClassList("ps-faction-pin");node.AddToClassList("ps-faction-pin-"+(nodeIndex++));if(faction.id==selectedFactionId)node.AddToClassList("ps-selected");node.Add(Atlas(game.UiFactionArt,FactionUv(faction.id),"ps-faction-pin-art"));node.Add(PackspireUiFactory.Title(faction.name));node.Add(PackspireUiFactory.Body($"貢献 {rep:0}"));nodes.Add(node);}
  var selected=GameCatalog.Factions.First(x=>x.id==selectedFactionId);float value=meta.factionRep.FirstOrDefault(x=>x.id==selected.id)?.value??0;int rank=Mathf.Clamp(Mathf.FloorToInt(value/25f),0,selected.ranks.Length-1);
  var journal=Container("ps-detail-journal");desk.Add(journal);if(game.UiBookArt!=null)journal.Add(Image(game.UiBookArt,new Rect(0,0,1,1),"ps-detail-journal-art",ScaleMode.StretchToFill));var detail=Container("ps-detail-journal-copy");journal.Add(detail);detail.Add(Atlas(game.UiFactionArt,FactionUv(selected.id),"ps-faction-detail-art"));detail.Add(PackspireUiFactory.Title(selected.name));detail.Add(PackspireUiFactory.Body(selected.description));detail.Add(PackspireUiFactory.Body($"現在階級　{selected.ranks[rank]}\n貢献度　{value:0}"));
  var progress=Container("ps-progress");var fill=Container("ps-progress-fill");fill.style.width=Length.Percent(Mathf.Clamp01(value/75f)*100f);progress.Add(fill);detail.Add(progress);
  var change=PackspireUiFactory.Button(selected.id==meta.currentFaction?"● 現在の所属":"20Gで所属を変更",()=>{if(game.UiChangeFaction(selected.id))ShowToast(selected.name+"へ所属を変更しました");BuildFactionAgain();});if(selected.id!=meta.currentFaction&&meta.baseGold<20)change.SetEnabled(false);detail.Add(change);
  var requests=Container("ps-request-rail");requests.Add(PackspireUiFactory.Body("依頼書留め　—　新しい依頼は遠征準備へ記録されます"));board.Add(requests);desk.Add(TabletopBack());
 }
 void BuildFactionAgain(){screenRoot.Clear();BuildFaction();}

 void BuildCompendium(){
  var shell=BookShell("図鑑",null);screenRoot.Add(shell);var pages=shell.Q<VisualElement>("book-pages");var left=Page("記録一覧");var right=Page("選択中の記録");pages.Add(left);pages.Add(right);
  var tabs=Container("ps-category-tabs");string[] names={"装備","役職","敵"};for(int i=0;i<3;i++){int tab=i;var b=PackspireUiFactory.Button(names[i],()=>{compendiumTab=tab;selectedCompendiumId="";BuildCompendiumAgain();});if(i==compendiumTab)b.AddToClassList("ps-selected");b.Add(InkRule());tabs.Add(b);}left.Add(tabs);
  var gridBody=Container("ps-compendium-grid");left.Add(gridBody);
  if(compendiumTab==0)BuildItemCompendium(gridBody,right);else if(compendiumTab==1)BuildRoleCompendium(gridBody,right);else BuildEnemyCompendium(gridBody,right);
  AddBookTabs(shell,ScreenId.Compendium);
 }
 void BuildCompendiumAgain(){screenRoot.Clear();BuildCompendium();}
 void BuildItemCompendium(VisualElement grid,VisualElement detail){var values=GameCatalog.Items.Values.ToArray();if(string.IsNullOrEmpty(selectedCompendiumId)||!GameCatalog.Items.ContainsKey(selectedCompendiumId))selectedCompendiumId=values[0].id;foreach(var item in values)grid.Add(AtlasButton(game.UiEquipmentArt,ItemUv(item.id),item.name,item.id==selectedCompendiumId,()=>{selectedCompendiumId=item.id;BuildCompendiumAgain();}));var selected=GameCatalog.Items[selectedCompendiumId];detail.Add(Atlas(game.UiEquipmentArt,ItemUv(selected.id),"ps-detail-art"));detail.Add(PackspireUiFactory.Title(selected.name));detail.Add(PackspireUiFactory.Body(selected.description));detail.Add(PackspireUiFactory.Body($"分類　{ItemTypeLabel(selected.type)}\n形状　{selected.cells.Length}マス\n属性　{string.Join("・",selected.cells.Select(x=>ElementLabel(x.element)))}"));if(!string.IsNullOrEmpty(selected.linkRule)){detail.Add(PackspireUiFactory.Title("LINK効果"));detail.Add(PackspireUiFactory.Body(selected.linkRule));}}
 void BuildRoleCompendium(VisualElement grid,VisualElement detail){var values=GameCatalog.Roles.Values.ToArray();if(string.IsNullOrEmpty(selectedCompendiumId)||!GameCatalog.Roles.ContainsKey(selectedCompendiumId))selectedCompendiumId=values[0].id;foreach(var role in values){bool known=game.UiMeta.jobLevels.Any(x=>x.id==role.id&&x.value>0);var tile=AtlasButton(game.UiRoleArt,RoleUv(role.id),known?role.name:"？？？",role.id==selectedCompendiumId,()=>{selectedCompendiumId=role.id;BuildCompendiumAgain();});if(!known)tile.AddToClassList("ps-unknown");grid.Add(tile);}var selected=GameCatalog.Roles[selectedCompendiumId];detail.Add(Atlas(game.UiRoleArt,RoleUv(selected.id),"ps-detail-art"));detail.Add(PackspireUiFactory.Title(selected.name));detail.Add(PackspireUiFactory.Body(selected.kind+"\n"+selected.description));detail.Add(PackspireUiFactory.Body($"最大レベル　{selected.maxLevel}\nLv.7　{game.UiRoleMilestone(selected.id,false)}\nLv.{selected.maxLevel}　{game.UiRoleMilestone(selected.id,true)}"));}
 void BuildEnemyCompendium(VisualElement grid,VisualElement detail){var values=GameCatalog.Enemies;if(string.IsNullOrEmpty(selectedCompendiumId)||!values.Any(x=>x.id==selectedCompendiumId))selectedCompendiumId=values[0].id;foreach(var enemy in values)grid.Add(AtlasButton(game.UiEnemyArt,EnemyUv(enemy.id),enemy.name,enemy.id==selectedCompendiumId,()=>{selectedCompendiumId=enemy.id;BuildCompendiumAgain();}));var selected=values.First(x=>x.id==selectedCompendiumId);detail.Add(Atlas(game.UiEnemyArt,EnemyUv(selected.id),"ps-detail-art"));detail.Add(PackspireUiFactory.Title(selected.name));detail.Add(PackspireUiFactory.Body($"危険度　{selected.tier}\n基礎HP　{selected.hp}\n行動候補　{string.Join("・",selected.damages.Select(x=>x==0?"特殊行動":$"攻撃{x}"))}"));}
}
}
