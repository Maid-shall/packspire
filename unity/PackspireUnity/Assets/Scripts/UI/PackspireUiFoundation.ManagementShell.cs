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
  mgmtVaultGrid=null;

  if(layout==ManagementLayout.StatusOverview){
   // Formal 3-column: character | learned roles | role detail (siblings under main row).
   shell.AddToClassList("ps-status-v2");
   body.AddToClassList("ps-status-main-row");

   var characterCol=Container("ps-status-character-column");
   var characterSurface=Container("ps-status-character-surface");
   var characterScroll=new ScrollView(ScrollViewMode.Vertical);
   characterScroll.AddToClassList("ps-status-character-scroll");
   characterScroll.verticalScrollerVisibility=ScrollerVisibility.Auto;
   StretchMgmtScrollContent(characterScroll);
   mgmtOverviewHost=Container("ps-status-character-host");
   characterScroll.Add(mgmtOverviewHost);
   characterSurface.Add(characterScroll);
   characterCol.Add(characterSurface);
   body.Add(characterCol);

   var rolesCol=Container("ps-status-roles-column");
   var rolesSurface=Container("ps-status-roles-surface");
   mgmtListHeader=Container("ps-mgmt-list-header ps-status-roles-header");
   rolesSurface.Add(mgmtListHeader);
   listScroll=new ScrollView(ScrollViewMode.Vertical);
   listScroll.AddToClassList("ps-mgmt-list-scroll");
   listScroll.AddToClassList("ps-status-roles-scroll");
   listScroll.verticalScrollerVisibility=ScrollerVisibility.Auto;
   StretchMgmtScrollContent(listScroll);
   rolesSurface.Add(listScroll);
   rolesCol.Add(rolesSurface);
   body.Add(rolesCol);

   var detailCol=Container("ps-status-role-detail-column");
   var detailSurface=Container("ps-status-role-detail-surface");
   mgmtDetailHero=Container("ps-mgmt-detail-hero ps-status-detail-header");
   mgmtDetailHero.style.display=DisplayStyle.None;
   mgmtDetailArtHost=Container("ps-mgmt-detail-art-host ps-status-role-image ps-art-vignette");
   mgmtDetailSummaryHost=Container("ps-mgmt-detail-summary-host ps-status-role-identity");
   mgmtDetailHero.Add(mgmtDetailArtHost);
   mgmtDetailHero.Add(mgmtDetailSummaryHost);
   detailSurface.Add(mgmtDetailHero);
   detailScroll=new ScrollView(ScrollViewMode.Vertical);
   detailScroll.AddToClassList("ps-mgmt-detail-scroll");
   detailScroll.AddToClassList("ps-status-role-detail-scroll");
   detailScroll.verticalScrollerVisibility=ScrollerVisibility.Auto;
   StretchMgmtScrollContent(detailScroll,false);
   detailSurface.Add(detailScroll);
   detailCol.Add(detailSurface);
   body.Add(detailCol);
  }else if(layout==ManagementLayout.CompendiumReelDetail){
   // Index reel | Specimen art | Record detail (product grammar)
   shell.AddToClassList("ps-codex-v2");
   body.AddToClassList("ps-codex-main-row");
   var listCol=Container("ps-mgmt-col-list ps-codex-index-column");
   mgmtListHeader=Container("ps-mgmt-list-header ps-codex-index-header");
   listCol.Add(mgmtListHeader);
   var listSurface=Container("ps-codex-index-surface");
   listScroll=new ScrollView(ScrollViewMode.Vertical);
   listScroll.AddToClassList("ps-mgmt-list-scroll");
   listScroll.AddToClassList("ps-codex-index-scroll");
   listScroll.verticalScrollerVisibility=ScrollerVisibility.Auto;
   StretchMgmtScrollContent(listScroll);
   listSurface.Add(listScroll);
   listCol.Add(listSurface);
   body.Add(listCol);

   var artCol=Container("ps-mgmt-col-art ps-codex-specimen-column");
   mgmtDetailArtHost=Container("ps-art-vignette ps-mgmt-focal-art ps-codex-specimen-art");
   AddSurfaceOuterCorners(mgmtDetailArtHost);
   artCol.Add(mgmtDetailArtHost);
   body.Add(artCol);

   var detailCol=Container("ps-mgmt-col-detail ps-codex-record-column");
   var detailSurface=Container("ps-codex-record-surface");
   mgmtDetailSummaryHost=Container("ps-mgmt-detail-summary-host ps-codex-record-header");
   detailSurface.Add(mgmtDetailSummaryHost);
   detailScroll=new ScrollView(ScrollViewMode.Vertical);
   detailScroll.AddToClassList("ps-mgmt-detail-scroll");
   detailScroll.AddToClassList("ps-codex-record-scroll");
   detailScroll.verticalScrollerVisibility=ScrollerVisibility.Auto;
   StretchMgmtScrollContent(detailScroll,false);
   detailSurface.Add(detailScroll);
   detailCol.Add(detailSurface);
   mgmtDetailHero=null;
   body.Add(detailCol);
  }else{
   // Vault: compact grid | detail
   shell.AddToClassList("ps-vault-v2");
   body.AddToClassList("ps-vault-main-row");
   var listCol=Container("ps-mgmt-col-list ps-vault-inventory-column");
   mgmtListHeader=Container("ps-mgmt-list-header ps-vault-inventory-header");
   listCol.Add(mgmtListHeader);
   var listSurface=Container("ps-vault-inventory-surface");
   listScroll=new ScrollView(ScrollViewMode.Vertical);
   listScroll.AddToClassList("ps-vault-grid-scroll");
   listScroll.verticalScrollerVisibility=ScrollerVisibility.Auto;
   StretchMgmtScrollContent(listScroll);
   mgmtVaultGrid=Container("ps-vault-grid");
   listScroll.Add(mgmtVaultGrid);
   listSurface.Add(listScroll);
   listCol.Add(listSurface);
   body.Add(listCol);

   var detailCol=Container("ps-mgmt-col-detail ps-vault-item-detail-column");
   var detailSurface=Container("ps-vault-item-detail-surface");
   mgmtDetailHero=Container("ps-mgmt-detail-hero ps-vault-item-header");
   mgmtDetailHero.style.display=DisplayStyle.None;
   mgmtDetailArtHost=Container("ps-mgmt-detail-art-host ps-art-vignette");
   mgmtDetailSummaryHost=Container("ps-mgmt-detail-summary-host");
   mgmtDetailHero.Add(mgmtDetailArtHost);
   mgmtDetailHero.Add(mgmtDetailSummaryHost);
   detailSurface.Add(mgmtDetailHero);
   detailScroll=new ScrollView(ScrollViewMode.Vertical);
   detailScroll.AddToClassList("ps-mgmt-detail-scroll");
   detailScroll.AddToClassList("ps-vault-item-detail-scroll");
   detailScroll.verticalScrollerVisibility=ScrollerVisibility.Auto;
   StretchMgmtScrollContent(detailScroll,false);
   detailSurface.Add(detailScroll);
   detailCol.Add(detailSurface);
   body.Add(detailCol);
  }

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

 static void StretchMgmtScrollContent(ScrollView scroll,bool growContent=true){
  if(scroll==null)return;
  var content=scroll.contentContainer;
  if(content==null)return;
  content.style.width=Length.Percent(100);
  content.style.minWidth=Length.Percent(100);
  content.style.flexGrow=growContent?1:0;
  content.AddToClassList("ps-mgmt-scroll-content");
 }

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
  if(mgmtDetailArtHost==null||art==null)return;
  mgmtDetailArtHost.Clear();
  var frame=Container("ps-mgmt-detail-art-frame"+(unknown?" ps-mgmt-art-unknown":""));
  frame.pickingMode=PickingMode.Ignore;
  art.pickingMode=PickingMode.Ignore;
  frame.Add(art);
  mgmtDetailArtHost.Add(frame);
  if(mgmtDetailHero!=null)mgmtDetailHero.style.display=DisplayStyle.Flex;
 }

 void SetMgmtUnknownFocalArt(string label){
  if(mgmtDetailArtHost==null)return;
  mgmtDetailArtHost.Clear();
  var frame=Container("ps-mgmt-detail-art-frame ps-mgmt-art-unknown");
  frame.pickingMode=PickingMode.Ignore;
  var ghost=new Label(string.IsNullOrEmpty(label)?"？":label){pickingMode=PickingMode.Ignore};
  ghost.AddToClassList("ps-mgmt-art-unknown-glyph");
  frame.Add(ghost);
  mgmtDetailArtHost.Add(frame);
  if(mgmtDetailHero!=null)mgmtDetailHero.style.display=DisplayStyle.Flex;
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
   card.EnableInClassList("ps-vault-protected",stashItem!=null&&stashItem.insured);
  }
 }

 Button BuildVaultGridCard(MetaSave meta,ItemInstance item,bool selected,System.Action onClick){
  var def=GameCatalog.Items[item.templateId];
  bool heir=item.uid==meta.selectedHeirloomUid;
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
  return card;
 }

 VisualElement VaultGridBadge(string text,string className){
  var badge=Container("ps-vault-grid-badge "+className);
  badge.Add(new Label(text){pickingMode=PickingMode.Ignore});
  return badge;
 }

 Button ManagementListRow(string id,VisualElement leading,string primary,string secondary,bool selected,System.Action onClick){
  var row=new Button(onClick){userData=id};
  row.AddToClassList("ps-list-item");
  row.AddToClassList("ps-mgmt-list-row");
  if(selected)row.AddToClassList("ps-selected");
  var accent=Container("ps-list-item-mark");
  accent.pickingMode=PickingMode.Ignore;
  row.Add(accent);
  if(leading!=null){
   leading.AddToClassList("ps-mgmt-list-icon");
   leading.pickingMode=PickingMode.Ignore;
   row.Add(leading);
  }
  var copy=Container("ps-mgmt-list-copy");
  copy.pickingMode=PickingMode.Ignore;
  var name=new Label(primary);
  name.AddToClassList("ps-mgmt-list-name");
  name.AddToClassList("ps-typo-item");
  copy.Add(name);
  if(!string.IsNullOrEmpty(secondary)){
   var sub=new Label(secondary);
   sub.AddToClassList("ps-mgmt-list-sub");
   sub.AddToClassList("ps-typo-secondary");
   copy.Add(sub);
  }
  row.Add(copy);
  return row;
 }

 Button ManagementReelRow(string id,string primary,string secondary,bool selected,System.Action onClick,VisualElement leading=null){
  var row=new Button(onClick){userData=id,tooltip=primary};
  row.AddToClassList("ps-list-item");
  row.AddToClassList("ps-mgmt-reel-row");
  if(selected)row.AddToClassList("ps-selected");
  var accent=Container("ps-list-item-mark");
  accent.pickingMode=PickingMode.Ignore;
  row.Add(accent);
  if(leading!=null){
   leading.pickingMode=PickingMode.Ignore;
   leading.AddToClassList("ps-mgmt-reel-leading");
   row.Add(leading);
  }
  var copy=Container("ps-mgmt-reel-copy");
  copy.pickingMode=PickingMode.Ignore;
  var name=new Label(primary){pickingMode=PickingMode.Ignore};
  name.AddToClassList("ps-mgmt-reel-name");
  name.AddToClassList("ps-typo-item");
  copy.Add(name);
  if(!string.IsNullOrEmpty(secondary)){
   var sub=new Label(secondary){pickingMode=PickingMode.Ignore};
   sub.AddToClassList("ps-mgmt-reel-sub");
   sub.AddToClassList("ps-typo-secondary");
   copy.Add(sub);
  }
  row.Add(copy);
  return row;
 }

 VisualElement CodexIndexMark(string glyph,bool unknown=false){
  var mark=Container(unknown?"ps-seal-mark ps-codex-index-mark ps-codex-index-mark-unknown":"ps-seal-mark ps-codex-index-mark");
  mark.pickingMode=PickingMode.Ignore;
  mark.Add(new Label(glyph){pickingMode=PickingMode.Ignore});
  return mark;
 }

 Button StatusRoleTicket(string id,string roleName,string kind,int level,int maxLevel,bool equipped,bool selected,System.Action onClick){
  var row=new Button(onClick){userData=id,tooltip=roleName};
  row.AddToClassList("ps-list-item");
  row.AddToClassList("ps-mgmt-reel-row");
  row.AddToClassList("ps-status-role-ticket");
  if(selected)row.AddToClassList("ps-selected");
  if(equipped)row.AddToClassList("ps-status-role-equipped");
  var accent=Container("ps-list-item-mark");
  accent.pickingMode=PickingMode.Ignore;
  row.Add(accent);
  var sealHost=Container("ps-status-role-seal-host");
  sealHost.pickingMode=PickingMode.Ignore;
  if(game.UiRoleArt!=null){
   var seal=SmallAtlasIcon(game.UiRoleArt,RoleUv(id));
   seal.AddToClassList("ps-status-role-seal");
   seal.pickingMode=PickingMode.Ignore;
   sealHost.Add(seal);
  }
  row.Add(sealHost);
  var copy=Container("ps-status-role-ticket-copy");
  copy.pickingMode=PickingMode.Ignore;
  var name=new Label(roleName){pickingMode=PickingMode.Ignore};
  name.AddToClassList("ps-status-role-ticket-name");
  name.AddToClassList("ps-typo-item");
  copy.Add(name);
  var meta=Container("ps-status-role-ticket-meta");
  meta.pickingMode=PickingMode.Ignore;
  var kindLabel=new Label(kind){pickingMode=PickingMode.Ignore};
  kindLabel.AddToClassList("ps-status-role-ticket-kind");
  kindLabel.AddToClassList("ps-typo-secondary");
  meta.Add(kindLabel);
  var lv=new Label($"Lv.{level}/{maxLevel}"){pickingMode=PickingMode.Ignore};
  lv.AddToClassList("ps-status-role-ticket-lv");
  lv.AddToClassList("ps-typo-value");
  meta.Add(lv);
  copy.Add(meta);
  row.Add(copy);
  if(equipped){
   var stamp=Container("ps-seal-mark ps-status-role-equip-seal");
   stamp.pickingMode=PickingMode.Ignore;
   stamp.Add(new Label("装"){pickingMode=PickingMode.Ignore});
   row.Add(stamp);
  }
  return row;
 }

 VisualElement StatusLevelTrack(RoleDef role,int currentLevel){
  var track=Container("ps-progress-track ps-status-level-track");
  track.pickingMode=PickingMode.Ignore;
  AddStatusLevelNode(track,"Lv.1","基礎礎効果",role.description,currentLevel>=1,currentLevel>=1&&currentLevel<7,false);
  AddStatusLevelConnector(track,currentLevel>=7);
  AddStatusLevelNode(track,"Lv.7","専用効果",game.UiRoleMilestone(role.id,false),currentLevel>=7,currentLevel>=7&&currentLevel<role.maxLevel,currentLevel<7);
  AddStatusLevelConnector(track,currentLevel>=role.maxLevel);
  AddStatusLevelNode(track,$"Lv.{role.maxLevel}","最大レベル効果",game.UiRoleMilestone(role.id,true),currentLevel>=role.maxLevel,currentLevel>=role.maxLevel,currentLevel<role.maxLevel);
  return track;
 }

 void AddStatusLevelConnector(VisualElement track,bool lit){
  var line=Container(lit?"ps-progress-track-connector ps-progress-track-connector-lit":"ps-progress-track-connector");
  line.pickingMode=PickingMode.Ignore;
  track.Add(line);
 }

 void AddStatusLevelNode(VisualElement track,string level,string title,string body,bool reached,bool current,bool dim){
  var node=Container("ps-progress-track-node");
  if(reached)node.AddToClassList("ps-progress-track-reached");
  if(current)node.AddToClassList("ps-progress-track-current");
  if(dim)node.AddToClassList("ps-progress-track-dim");
  node.pickingMode=PickingMode.Ignore;
  var rail=Container("ps-progress-track-rail");
  rail.pickingMode=PickingMode.Ignore;
  var mark=Container("ps-progress-track-mark");
  mark.pickingMode=PickingMode.Ignore;
  rail.Add(mark);
  node.Add(rail);
  var copy=Container("ps-progress-track-copy");
  copy.pickingMode=PickingMode.Ignore;
  var head=Container("ps-progress-track-head");
  head.pickingMode=PickingMode.Ignore;
  var levelLabel=new Label(level){pickingMode=PickingMode.Ignore};
  levelLabel.AddToClassList("ps-progress-track-level");
  levelLabel.AddToClassList("ps-typo-eyebrow");
  head.Add(levelLabel);
  var titleLabel=new Label(title){pickingMode=PickingMode.Ignore};
  titleLabel.AddToClassList("ps-progress-track-title");
  titleLabel.AddToClassList("ps-typo-section");
  head.Add(titleLabel);
  copy.Add(head);
  if(!string.IsNullOrEmpty(body)){
   var bodyLabel=new Label(body){pickingMode=PickingMode.Ignore};
   bodyLabel.AddToClassList("ps-progress-track-body");
   bodyLabel.AddToClassList("ps-typo-body");
   copy.Add(bodyLabel);
  }
  node.Add(copy);
  track.Add(node);
 }

 VisualElement StatusField(string label,string value){
  var row=Container("ps-field-row ps-status-field");
  row.pickingMode=PickingMode.Ignore;
  var lab=new Label(label){pickingMode=PickingMode.Ignore};
  lab.AddToClassList("ps-field-label");
  lab.AddToClassList("ps-typo-secondary");
  row.Add(lab);
  var val=new Label(value){pickingMode=PickingMode.Ignore};
  val.AddToClassList("ps-field-value");
  val.AddToClassList("ps-typo-body");
  row.Add(val);
  return row;
 }

 VisualElement ManagementFilterBar(string[] labels,int selectedIndex,System.Action<int> onPick){
  var bar=Container("ps-mgmt-filter-bar");
  for(int i=0;i<labels.Length;i++){
   int index=i;
   var button=PackspireUiFactory.Button(labels[i],()=>onPick(index));
   button.AddToClassList("ps-mgmt-filter");
   button.AddToClassList("ps-action-secondary");
   if(i==selectedIndex)button.AddToClassList("ps-selected");
   bar.Add(button);
  }
  return bar;
 }

 VisualElement ManagementSection(string title,string body,bool dim=false){
  var section=Container(dim?"ps-mgmt-section ps-mgmt-section-dim":"ps-mgmt-section");
  if(!string.IsNullOrEmpty(title)){
   var heading=new Label(title){pickingMode=PickingMode.Ignore};
   heading.AddToClassList("ps-mgmt-section-title");
   heading.AddToClassList("ps-typo-section");
   section.Add(heading);
  }
  if(!string.IsNullOrEmpty(body)){
   var copy=new Label(body){pickingMode=PickingMode.Ignore};
   copy.AddToClassList("ps-typo-body");
   copy.AddToClassList("ps-mgmt-section-body");
   section.Add(copy);
  }
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

  var box=Container("ps-status-character-block");
  var art=Container("ps-status-character-art ps-art-vignette");
  AddSurfaceOuterCorners(art);
  var glow=Container("ps-status-character-art-glow");
  glow.pickingMode=PickingMode.Ignore;
  art.Add(glow);
  art.Add(CharacterPortraitFront(character,"ps-status-character-art-image"));
  var artFade=Container("ps-status-character-art-fade");
  artFade.pickingMode=PickingMode.Ignore;
  art.Add(artFade);
  box.Add(art);

  var summary=Container("ps-status-character-summary");
  var name=new Label(character.name){pickingMode=PickingMode.Ignore};
  name.AddToClassList("ps-typo-screen");
  name.AddToClassList("ps-status-character-name");
  summary.Add(name);
  if(!string.IsNullOrEmpty(character.title)){
   var titleLabel=new Label(character.title){pickingMode=PickingMode.Ignore};
   titleLabel.AddToClassList("ps-typo-secondary");
   titleLabel.AddToClassList("ps-status-character-title");
   summary.Add(titleLabel);
  }
  summary.Add(StatusField("現在の役職",currentRole));
  summary.Add(StatusField("所属勢力",$"{factionName}　{factionDef?.ranks[rank]??""}".Trim()));
  summary.Add(StatusField("合計役職レベル",$"Lv.{totalLevel}"));
  string foundation=KnownCharacterFoundation(character);
  if(!string.IsNullOrEmpty(foundation))
   summary.Add(StatusField("基礎礎能力",foundation));
  if(!string.IsNullOrEmpty(character.traitText)){
   var trait=string.IsNullOrEmpty(character.traitName)?character.traitText:$"{character.traitName}\n{character.traitText}";
   summary.Add(StatusField("特性",trait));
  }
  if(!string.IsNullOrEmpty(character.activeSkillName)||!string.IsNullOrEmpty(character.activeSkillText))
   summary.Add(StatusField("能動スキル",character.activeSkillName+"\n"+character.activeSkillText));
  var tail=Container("ps-space-scroll-tail");
  tail.pickingMode=PickingMode.Ignore;
  summary.Add(tail);
  box.Add(summary);
  return box;
 }

 static string KnownCharacterFoundation(CharacterDef character){
  if(character==null||string.IsNullOrEmpty(character.traitKind))return "";
  return character.traitKind switch{
   "maxHpBonus"=>$"最大HP +{character.traitValue}",
   _=>""
  };
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
