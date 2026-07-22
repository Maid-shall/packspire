using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire {
public sealed partial class PackspireUiFoundation {
 public enum ManagementLayout { StatusOverview, VaultListDetail, CompendiumReelDetail }

 ScrollView mgmtListScroll;
 VisualElement mgmtVaultGrid;
 VisualElement mgmtOverviewHost;
 VisualElement mgmtDetailHero;
 VisualElement mgmtDetailArtHost;
 VisualElement mgmtDetailSummaryHost;
 ScrollView mgmtDetailScroll;
 VisualElement mgmtListHeader;
 float mgmtListScrollY;
 int vaultFilter;

 VisualElement BuildManagementShell(string eyebrow,string title,ManagementLayout layout,out ScrollView listScroll,out ScrollView detailScroll){
  var shell=Container("ps-mgmt-screen ps-mgmt-layout-"+LayoutClass(layout)+" ps-dark-surface");
  var backgroundHost=Container("ps-layer-background");
  var bg=HubBackgroundArt();
  if(bg==null)bg=CourtyardArt();
  if(bg!=null)backgroundHost.Add(Image(bg,new Rect(0,0,1,1),"ps-mgmt-bg",ScaleMode.ScaleAndCrop));
  var shade=Container("ps-mgmt-shade");
  shade.pickingMode=PickingMode.Ignore;
  backgroundHost.Add(shade);
  shell.Add(backgroundHost);

  var contentHost=Container("ps-layer-content");
  var header=Container("ps-mgmt-header");
  header.Add(ChromeBrand(eyebrow,title));
  contentHost.Add(header);

  var body=Container("ps-mgmt-body");
  mgmtOverviewHost=null;
  mgmtDetailHero=null;
  mgmtDetailArtHost=null;
  mgmtDetailSummaryHost=null;

  if(layout==ManagementLayout.StatusOverview){
   var overviewCol=Container("ps-mgmt-col-overview");
   mgmtOverviewHost=Container("ps-mgmt-overview-host");
   overviewCol.Add(mgmtOverviewHost);
   body.Add(overviewCol);

   var listCol=Container("ps-mgmt-col-list");
   mgmtListHeader=Container("ps-mgmt-list-header");
   listCol.Add(mgmtListHeader);
   listScroll=new ScrollView(ScrollViewMode.Vertical);
   listScroll.AddToClassList("ps-mgmt-list-scroll");
   listScroll.verticalScrollerVisibility=ScrollerVisibility.Auto;
   listCol.Add(listScroll);
   body.Add(listCol);
  }else{
   var listCol=Container("ps-mgmt-col-list");
   mgmtListHeader=Container("ps-mgmt-list-header");
   listCol.Add(mgmtListHeader);
   listScroll=new ScrollView(ScrollViewMode.Vertical);
   listScroll.AddToClassList(layout==ManagementLayout.VaultListDetail?"ps-vault-grid-scroll":"ps-mgmt-list-scroll");
   listScroll.verticalScrollerVisibility=ScrollerVisibility.Auto;
   listCol.Add(listScroll);
   if(layout==ManagementLayout.VaultListDetail){
    mgmtVaultGrid=Container("ps-vault-grid");
    listScroll.Add(mgmtVaultGrid);
   }else mgmtVaultGrid=null;
   body.Add(listCol);
  }

  var detailCol=Container("ps-mgmt-col-detail");
  mgmtDetailHero=Container("ps-mgmt-detail-hero");
  mgmtDetailHero.style.display=DisplayStyle.None;
  mgmtDetailArtHost=Container("ps-mgmt-detail-art-host");
  mgmtDetailSummaryHost=Container("ps-mgmt-detail-summary-host");
  mgmtDetailHero.Add(mgmtDetailArtHost);
  mgmtDetailHero.Add(mgmtDetailSummaryHost);
  detailCol.Add(mgmtDetailHero);

  detailScroll=new ScrollView(ScrollViewMode.Vertical);
  detailScroll.AddToClassList("ps-mgmt-detail-scroll");
  detailScroll.verticalScrollerVisibility=ScrollerVisibility.Auto;
  detailCol.Add(detailScroll);
  body.Add(detailCol);

  contentHost.Add(body);
  shell.Add(contentHost);

  mgmtListScroll=listScroll;
  mgmtDetailScroll=detailScroll;
  return shell;
 }

 static string LayoutClass(ManagementLayout layout)=>layout switch{
  ManagementLayout.StatusOverview=>"status",
  ManagementLayout.VaultListDetail=>"vault",
  ManagementLayout.CompendiumReelDetail=>"compendium",
  _=>"vault"
 };

 void SaveMgmtListScroll(){if(mgmtListScroll!=null)mgmtListScrollY=mgmtListScroll.scrollOffset.y;}
 void RestoreMgmtListScroll(){if(mgmtListScroll!=null)mgmtListScroll.scrollOffset=new Vector2(0,mgmtListScrollY);}

 void ClearMgmtOverview(){mgmtOverviewHost?.Clear();}
 void ClearMgmtDetailHero(){
  mgmtDetailArtHost?.Clear();
  mgmtDetailSummaryHost?.Clear();
  if(mgmtDetailHero!=null)mgmtDetailHero.style.display=DisplayStyle.None;
 }
 void ClearMgmtDetail(){ClearMgmtDetailHero();mgmtDetailScroll?.Clear();}

 void SetMgmtDetailHeroArt(VisualElement art,bool unknown=false){
  if(mgmtDetailHero==null||mgmtDetailArtHost==null||art==null)return;
  mgmtDetailArtHost.Clear();
  var frame=Container("ps-mgmt-detail-art-frame"+(unknown?" ps-mgmt-art-unknown":""));
  frame.pickingMode=PickingMode.Ignore;
  art.pickingMode=PickingMode.Ignore;
  frame.Add(art);
  mgmtDetailArtHost.Add(frame);
  mgmtDetailHero.style.display=DisplayStyle.Flex;
 }

 void SetMgmtDetailHeroSummary(params VisualElement[] blocks){
  if(mgmtDetailSummaryHost==null)return;
  mgmtDetailSummaryHost.Clear();
  foreach(var block in blocks){
   if(block==null)continue;
   block.pickingMode=PickingMode.Ignore;
   mgmtDetailSummaryHost.Add(block);
  }
  if(blocks.Length>0&&mgmtDetailHero!=null)mgmtDetailHero.style.display=DisplayStyle.Flex;
 }

 void UpdateMgmtListSelection(string selectedId){
  if(mgmtListScroll==null)return;
  foreach(var child in mgmtListScroll.contentContainer.Children()){
   if(child is not Button button)continue;
   bool selected=button.userData is string id&&id==selectedId;
   button.EnableInClassList("ps-selected",selected);
  }
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
   card.EnableInClassList("ps-vault-heirloom",uid==meta.selectedHeirloomUid);
   card.EnableInClassList("ps-vault-inuse",VaultItemInLoadout(meta,uid));
   var stashItem=meta.stash.FirstOrDefault(x=>x.uid==uid);
   card.EnableInClassList("ps-vault-protected",stashItem!=null&&(stashItem.insured||stashItem.heirloomCertified));
  }
 }

 Button BuildVaultGridCard(MetaSave meta,ItemInstance item,bool selected,System.Action onClick){
  var def=GameCatalog.Items[item.templateId];
  bool heir=item.uid==meta.selectedHeirloomUid;
  bool inUse=VaultItemInLoadout(meta,item.uid);
  bool protectedItem=item.insured||item.heirloomCertified;
  var card=new Button(onClick){userData=item.uid,tooltip=def.name};
  card.AddToClassList("ps-vault-grid-card");
  if(selected)card.AddToClassList("ps-selected");
  if(heir)card.AddToClassList("ps-vault-heirloom");
  if(inUse)card.AddToClassList("ps-vault-inuse");
  if(protectedItem)card.AddToClassList("ps-vault-protected");

  var art=Container("ps-vault-grid-art");
  art.pickingMode=PickingMode.Ignore;
  art.Add(Atlas(game.UiEquipmentArt,ItemUv(def.id),"ps-vault-grid-image"));
  var badges=Container("ps-vault-grid-badges");
  badges.pickingMode=PickingMode.Ignore;
  if(heir)badges.Add(VaultGridBadge("家","ps-vault-badge-heirloom"));
  if(inUse)badges.Add(VaultGridBadge("用","ps-vault-badge-inuse"));
  if(protectedItem)badges.Add(VaultGridBadge("保","ps-vault-badge-protected"));
  art.Add(badges);
  card.Add(art);

  var name=new Label(def.name){pickingMode=PickingMode.Ignore};
  name.AddToClassList("ps-vault-grid-name");
  card.Add(name);
  var sub=new Label($"{ItemTypeLabel(def.type)} +{item.temper}"){pickingMode=PickingMode.Ignore};
  sub.AddToClassList("ps-vault-grid-sub");
  card.Add(sub);
  return card;
 }

 VisualElement VaultGridBadge(string text,string className){
  var badge=Container("ps-vault-grid-badge "+className);
  badge.Add(new Label(text){pickingMode=PickingMode.Ignore});
  return badge;
 }

 Button ManagementListRow(string id,VisualElement leading,string primary,string secondary,bool selected,System.Action onClick){
  var row=new Button(onClick){userData=id};
  row.AddToClassList("ps-mgmt-list-row");
  if(selected)row.AddToClassList("ps-selected");
  if(leading!=null){
   leading.AddToClassList("ps-mgmt-list-icon");
   leading.pickingMode=PickingMode.Ignore;
   row.Add(leading);
  }
  var copy=Container("ps-mgmt-list-copy");
  copy.pickingMode=PickingMode.Ignore;
  var name=new Label(primary);
  name.AddToClassList("ps-mgmt-list-name");
  copy.Add(name);
  if(!string.IsNullOrEmpty(secondary)){
   var sub=new Label(secondary);
   sub.AddToClassList("ps-mgmt-list-sub");
   copy.Add(sub);
  }
  row.Add(copy);
  return row;
 }

 Button ManagementReelRow(string id,string primary,string mark,bool selected,System.Action onClick){
  var row=new Button(onClick){userData=id,tooltip=primary};
  row.AddToClassList("ps-mgmt-reel-row");
  if(selected)row.AddToClassList("ps-selected");
  if(!string.IsNullOrEmpty(mark)){
   var badge=new Label(mark){pickingMode=PickingMode.Ignore};
   badge.AddToClassList("ps-mgmt-reel-mark");
   row.Add(badge);
  }
  var name=new Label(primary){pickingMode=PickingMode.Ignore};
  name.AddToClassList("ps-mgmt-reel-name");
  row.Add(name);
  return row;
 }

 VisualElement ManagementFilterBar(string[] labels,int selectedIndex,System.Action<int> onPick){
  var bar=Container("ps-mgmt-filter-bar");
  for(int i=0;i<labels.Length;i++){
   int index=i;
   var button=PackspireUiFactory.Button(labels[i],()=>onPick(index));
   button.AddToClassList("ps-mgmt-filter");
   if(i==selectedIndex)button.AddToClassList("ps-selected");
   bar.Add(button);
  }
  return bar;
 }

 VisualElement ManagementSection(string title,string body,bool dim=false){
  var section=Container(dim?"ps-mgmt-section ps-mgmt-section-dim":"ps-mgmt-section");
  if(!string.IsNullOrEmpty(title)){
   var heading=PackspireUiFactory.Title(title);
   heading.AddToClassList("ps-mgmt-section-title");
   section.Add(heading);
  }
  if(!string.IsNullOrEmpty(body))section.Add(PackspireUiFactory.Body(body));
  return section;
 }

 VisualElement ManagementCharacterOverview(CharacterDef character,MetaSave meta){
  var learned=meta.jobLevels.Where(x=>x.value>0&&GameCatalog.Roles.ContainsKey(x.id)).ToList();
  int totalLevel=learned.Sum(x=>x.value);
  var currentRole=GameCatalog.Roles.ContainsKey(meta.currentRole)?GameCatalog.Roles[meta.currentRole].name:meta.currentRole;
  var factionName=FactionName(meta.currentFaction);
  var factionRep=meta.factionRep.FirstOrDefault(x=>x.id==meta.currentFaction)?.value??0;
  int rank=0;
  var factionDef=GameCatalog.Factions.FirstOrDefault(x=>x.id==meta.currentFaction);
  if(factionDef!=null)rank=Mathf.Clamp(Mathf.FloorToInt(factionRep/25f),0,factionDef.ranks.Length-1);

  var box=Container("ps-mgmt-char-overview");
  box.Add(CharacterPortraitFront(character,"ps-mgmt-overview-portrait"));
  box.Add(PackspireUiFactory.Title(character.name));
  box.Add(PackspireUiFactory.Body(character.title));
  box.Add(PackspireUiFactory.Body($"合計Lv.{totalLevel}"));
  box.Add(ManagementSection("基礎能力",character.traitText));
  box.Add(ManagementSection("主役職",currentRole));
  box.Add(ManagementSection("所属勢力",$"{factionName}　{factionDef?.ranks[rank]??""}"));
  box.Add(ManagementSection("能動スキル",character.activeSkillName+"\n"+character.activeSkillText));
  return box;
 }

 VisualElement HeirloomMark(){
  var mark=Container("ps-mgmt-heirloom-mark");
  mark.Add(new Label("家"){pickingMode=PickingMode.Ignore});
  return mark;
 }

 VisualElement SmallAtlasIcon(Texture2D texture,Rect uv,bool unknown=false){
  var icon=Atlas(texture,uv,"ps-mgmt-thumb");
  if(unknown)icon.AddToClassList("ps-mgmt-thumb-unknown");
  return icon;
 }

 VisualElement SmallPortraitIcon(VisualElement portrait,bool unknown=false){
  portrait.AddToClassList("ps-mgmt-thumb");
  if(unknown)portrait.AddToClassList("ps-mgmt-thumb-unknown");
  return portrait;
 }

 bool VaultItemInLoadout(MetaSave meta,string uid)=>meta.loadouts!=null&&meta.loadouts.Any(l=>l.slots!=null&&l.slots.Any(s=>s.itemUid==uid));
 string VaultLoadoutName(MetaSave meta,string uid){
  var loadout=meta.loadouts?.FirstOrDefault(l=>l.slots!=null&&l.slots.Any(s=>s.itemUid==uid));
  return loadout?.name??"";
 }
}
}
