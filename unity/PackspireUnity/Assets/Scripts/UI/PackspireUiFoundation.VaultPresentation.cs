using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire {
public sealed partial class PackspireUiFoundation {
 static readonly string[] VaultCategoryLabels={"すべて","武器","防具","道具","遺物"};
 static readonly List<string> VaultSortChoices=new(){
  "レアリティ順","入手段階順","名前順","種類順","占有マス順","鍛錬順","耐久が低い順"
 };

 VisualElement mgmtVaultGrid;
 VisualElement mgmtVaultFooter;
 int vaultFilter;
 int vaultSortMode;
 bool vaultCardExploration;
 int vaultRecordPage;
 VisualElement vaultCardModal;

 List<ItemInstance> CurrentVaultStash(MetaSave meta){
  return SortVaultStash(FilteredVaultStash(meta)).ToList();
 }

 bool VaultItemInLoadout(MetaSave meta,string uid){
  return meta.loadouts!=null&&meta.loadouts.Any(loadout=>
   loadout.slots!=null&&loadout.slots.Any(slot=>slot.itemUid==uid)
  );
 }

 bool IsVaultHeirloom(MetaSave meta,ItemInstance item){
  if(item==null)return false;
  return item.heirloomCertified||item.uid==meta?.selectedHeirloomUid;
 }

 void UpdateMgmtVaultGridSelection(string selectedUid){
  if(mgmtVaultGrid==null)return;
  foreach(var child in mgmtVaultGrid.Children()){
   if(child is not Button card||card.userData is not string uid)continue;
   card.EnableInClassList("ps-selected",uid==selectedUid);
  }
 }

 void UpdateMgmtVaultGridBadges(MetaSave meta){
  if(mgmtVaultGrid==null)return;
  foreach(var child in mgmtVaultGrid.Children()){
   if(child is not Button card||card.userData is not string uid)continue;
   card.EnableInClassList("ps-vault-heirloom",IsVaultHeirloom(meta,meta.stash.FirstOrDefault(x=>x.uid==uid)));
   card.EnableInClassList("ps-vault-inuse",VaultItemInLoadout(meta,uid));
   var stashItem=meta.stash.FirstOrDefault(x=>x.uid==uid);
   card.EnableInClassList("ps-vault-protected",stashItem!=null&&stashItem.insured);
  }
 }

 Button BuildVaultGridCard(MetaSave meta,ItemInstance item,bool selected,System.Action onClick){
  var def=GameCatalog.Items[item.templateId];
  bool heir=IsVaultHeirloom(meta,item);
  bool inUse=VaultItemInLoadout(meta,item.uid);
  bool protectedItem=item.insured;
  var card=new Button(onClick){userData=item.uid,tooltip=def.name};
  card.AddToClassList("ps-vault-grid-card");
  if(selected)card.AddToClassList("ps-selected");
  if(heir)card.AddToClassList("ps-vault-heirloom");
  if(inUse)card.AddToClassList("ps-vault-inuse");
  if(protectedItem)card.AddToClassList("ps-vault-protected");

  var art=Container("ps-vault-grid-art");
  art.pickingMode=PickingMode.Ignore;
  art.Add(VaultItemArt(def.id,"ps-vault-grid-image"));
  var badges=Container("ps-vault-grid-badges");
  badges.pickingMode=PickingMode.Ignore;
  if(heir)badges.Add(VaultGridBadge("家","ps-vault-badge-heirloom"));
  if(inUse)badges.Add(VaultGridBadge("用","ps-vault-badge-inuse"));
  art.Add(badges);
  var lockMark=Container("ps-vault-grid-lock");
  lockMark.pickingMode=PickingMode.Ignore;
  lockMark.Add(PackspireUiFactory.SystemIcon(PackspireUiFactory.PopIcon.Locked,"ps-vault-grid-lock-icon"));
  art.Add(lockMark);
  card.Add(art);

  var name=new Label(def.name){pickingMode=PickingMode.Ignore};
  name.AddToClassList("ps-vault-grid-name");
  card.Add(name);
  var rarity=new Label("◆"){pickingMode=PickingMode.Ignore};
  rarity.AddToClassList("ps-vault-grid-rarity");
  if(heir)rarity.AddToClassList("ps-vault-grid-rarity-heirloom");
  card.Add(rarity);
  return card;
 }

 VisualElement VaultItemArt(string itemId,string className){
  var texture=PackspireResources.Load<Texture2D>($"Art/UI/Vault/Items/{VaultItemArtAsset(itemId)}");
  if(texture==null)return Atlas(game.UiEquipmentArt,ItemUv(itemId),className);
  return Atlas(texture,new Rect(0f,0f,1f,1f),className);
 }

 static string VaultItemArtAsset(string itemId){
  return itemId switch{
   "sword" or "dagger" or "spear" or "ember"=>"vault-item-sword-v1",
   "shield" or "buckler" or "charm"=>"vault-item-gear-v1",
   "herb"=>"vault-item-herb-v1",
   "plate"=>"vault-item-plate-v1",
   "crystal"=>"vault-item-crystal-v1",
   "flask" or "bomb"=>"vault-item-flask-v1",
   _=>"vault-item-sword-v1"
  };
 }

 VisualElement VaultCategoryBar(string[] labels,int selectedIndex,System.Action<int> onPick){
  var bar=Container("ps-vault-v7-category-bar");
  for(int i=0;i<labels.Length;i++){
   int index=i;
   var button=PackspireUiFactory.Button("",()=>onPick(index));
   button.AddToClassList("ps-vault-v7-category");
   if(i==selectedIndex)button.AddToClassList("ps-selected");
   VisualElement icon=i switch{
    0=>PackspireUiFactory.ManagementArt(PackspireUiFactory.ManagementChrome.AllItems,"ps-vault-v7-category-icon"),
    1=>PackspireUiFactory.ManagementArt(PackspireUiFactory.ManagementChrome.WeaponCategory,"ps-vault-v7-category-icon"),
    2=>PackspireUiFactory.ManagementArt(PackspireUiFactory.ManagementChrome.ArmorCategory,"ps-vault-v7-category-icon"),
    3=>PackspireUiFactory.ManagementArt(PackspireUiFactory.ManagementChrome.SupplyCategory,"ps-vault-v7-category-icon"),
    _=>PackspireUiFactory.SystemIcon(PackspireUiFactory.PopIcon.Relic,"ps-vault-v7-category-icon")
   };
   icon.pickingMode=PickingMode.Ignore;
   button.Add(icon);
   var label=new Label(labels[i]){pickingMode=PickingMode.Ignore};
   label.AddToClassList("ps-vault-v7-category-label");
   button.Add(label);
   var glow=Container("ps-vault-v7-category-glow");
   glow.pickingMode=PickingMode.Ignore;
   button.Add(glow);
   bar.Add(button);
  }
  return bar;
 }

 VisualElement VaultGridBadge(string text,string className){
  var badge=Container("ps-vault-grid-badge "+className);
  badge.Add(new Label(text){pickingMode=PickingMode.Ignore});
  return badge;
 }
}
}
