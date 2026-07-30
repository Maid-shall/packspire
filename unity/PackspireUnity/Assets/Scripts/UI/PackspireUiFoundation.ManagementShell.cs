using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire {
public sealed partial class PackspireUiFoundation {
 public enum ManagementLayout { StatusOverview, VaultListDetail, CompendiumReelDetail }

 ScrollView mgmtListScroll;
 VisualElement mgmtOverviewHost;
 VisualElement mgmtDetailHero;
 VisualElement mgmtDetailArtHost;
 VisualElement mgmtDetailSummaryHost;
 ScrollView mgmtDetailScroll;
 VisualElement mgmtListHeader;
 float mgmtListScrollY;
 int statusRoleFilter;

 VisualElement BuildManagementShell(string eyebrow,string title,ManagementLayout layout,out ScrollView listScroll,out ScrollView detailScroll){
  if(TryBuildManagementView(eyebrow,title,layout,out var view,out listScroll,out detailScroll))
   return view;
  if(layout!=ManagementLayout.VaultListDetail)
   throw new InvalidOperationException($"Management UXML is missing for {layout}.");
  var shell=Container("ps-mgmt-screen ps-mgmt-layout-"+LayoutClass(layout)+" ps-dark-surface");
  var backgroundHost=Container("ps-layer-background");
  var bg=HubBackgroundArt();
  if(bg==null)bg=CourtyardArt();
  if(bg!=null)backgroundHost.Add(Image(bg,new Rect(0,0,1,1),"ps-mgmt-bg",ScaleMode.ScaleAndCrop));
  var shade=Container("ps-mgmt-shade");
  shade.pickingMode=PickingMode.Ignore;
  backgroundHost.Add(shade);
  shell.Add(backgroundHost);

  shell.AddToClassList("ps-management-v3");
  var contentHost=Container("ps-layer-content");
  var header=Container("ps-mgmt-header");
  header.Add(ManagementBrand(
   eyebrow,
   title,
   PackspireUiFactory.ManagementChrome.VaultCrest
  ));
  contentHost.Add(header);

  var body=Container("ps-mgmt-body");
  mgmtOverviewHost=null;
  mgmtDetailHero=null;
  mgmtDetailArtHost=null;
  mgmtDetailSummaryHost=null;
  mgmtVaultGrid=null;
  mgmtVaultFooter=null;

  // Kept only as a recovery view when the dedicated vault assets are missing.
   shell.AddToClassList("ps-vault-final");
   body.AddToClassList("ps-vault-main-row");
   body.AddToClassList("ps-vault-v9-main-row");
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
   // Do not let the flexible list surface become an invisible hit target over
   // the fixed sorting footer when the Game view is scaled down.
   listSurface.pickingMode=PickingMode.Ignore;
   listScroll.pickingMode=PickingMode.Position;
   listSurface.Add(listScroll);
   listCol.Add(listSurface);
   mgmtVaultFooter=Container("ps-vault-v7-footer");
   mgmtVaultFooter.pickingMode=PickingMode.Position;
   listCol.Add(mgmtVaultFooter);
   body.Add(listCol);

   var detailCol=Container("ps-mgmt-col-detail ps-vault-v9-record-column");
   detailCol.Add(PackspireUiFactory.SystemOrnament(
    PackspireUiFactory.PopOrnament.VerticalBoundary,
    "ps-vault-v9-divider"
   ));
   mgmtDetailHero=Container("ps-mgmt-detail-hero ps-vault-v9-hero");
    mgmtDetailHero.style.display=DisplayStyle.None;
    mgmtDetailSummaryHost=Container("ps-mgmt-detail-summary-host ps-vault-v9-identity");
    mgmtDetailArtHost=Container("ps-mgmt-detail-art-host ps-vault-v9-hero-art");
    mgmtDetailHero.Add(mgmtDetailArtHost);
    mgmtDetailHero.Add(mgmtDetailSummaryHost);
   detailCol.Add(mgmtDetailHero);
   detailScroll=new ScrollView(ScrollViewMode.Vertical);
   detailScroll.AddToClassList("ps-mgmt-detail-scroll");
   detailScroll.AddToClassList("ps-vault-v9-detail");
   detailScroll.verticalScrollerVisibility=ScrollerVisibility.Hidden;
   detailScroll.horizontalScrollerVisibility=ScrollerVisibility.Hidden;
   StretchMgmtScrollContent(detailScroll,false);
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
   if(button.ClassListContains("ps-codex-v9-index-entry")){
    button.RemoveFromClassList("ps-selected");
    button.RemoveFromClassList("ps-codex-current");
    button.EnableInClassList("ps-codex-v9-current",selected);
    ApplyCodexIndexRowState(button,selected);
   }else{
    button.EnableInClassList("ps-selected",selected);
   }
  }
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

 Button ManagementReelRow(string id,string primary,string secondary,bool selected,System.Action onClick,VisualElement leading=null,bool includeLegacyPlates=true){
  var row=new Button(onClick){userData=id,tooltip=primary};
  row.AddToClassList("ps-list-item");
  row.AddToClassList("ps-mgmt-reel-row");
  if(selected)row.AddToClassList("ps-selected");
  if(includeLegacyPlates){
   row.Add(PackspireUiFactory.ManagementV6Art(
    PackspireUiFactory.ManagementV6Piece.ArchiveTabWeapon,
    "ps-management-v6-bg ps-mgmt-reel-plate ps-mgmt-reel-plate-normal"
   ));
   row.Add(PackspireUiFactory.ManagementV6Art(
    PackspireUiFactory.ManagementV6Piece.ArchiveTabAll,
    "ps-management-v6-bg ps-mgmt-reel-plate ps-mgmt-reel-plate-selected"
   ));
  }
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

 Button CodexIndexRow(string id,string primary,string secondary,bool selected,System.Action onClick,VisualElement leading=null){
  var row=new Button(onClick){userData=id,tooltip=primary};
  // Deliberately do not reuse the legacy codex row classes here. Several old
  // style sheets still contain ornate reel selectors for those names and can
  // re-apply them after an asset refresh.
  row.AddToClassList("ps-codex-v9-index-entry");
  row.EnableInClassList("ps-codex-v9-current",selected);
  row.style.position=Position.Relative;
  row.style.width=Length.Percent(100);
  row.style.height=72;
  row.style.minHeight=72;
  row.style.maxHeight=72;
  row.style.marginBottom=8;
  row.style.paddingLeft=14;
  row.style.paddingRight=14;
  row.style.paddingTop=8;
  row.style.paddingBottom=8;
  row.style.flexDirection=FlexDirection.Row;
  row.style.alignItems=Align.Center;
  row.style.flexShrink=0;
  row.style.backgroundImage=StyleKeyword.None;
  row.style.backgroundColor=selected
   ?new Color(0.12f,0.035f,0.105f,0.98f)
   :new Color(0.025f,0.03f,0.065f,0.96f);
  row.style.borderLeftWidth=selected?3:1;
  row.style.borderRightWidth=1;
  row.style.borderTopWidth=1;
  row.style.borderBottomWidth=1;
  row.style.borderLeftColor=selected
   ?new Color(0.19f,0.94f,1f,1f)
   :new Color(0.39f,0.30f,0.25f,0.8f);
  row.style.borderRightColor=new Color(0.39f,0.30f,0.25f,0.8f);
  row.style.borderTopColor=new Color(0.39f,0.30f,0.25f,0.8f);
  row.style.borderBottomColor=new Color(0.39f,0.30f,0.25f,0.8f);
  row.style.borderTopLeftRadius=8;
  row.style.borderTopRightRadius=8;
  row.style.borderBottomLeftRadius=8;
  row.style.borderBottomRightRadius=8;

  var accent=Container("ps-codex-v9-index-accent");
  accent.pickingMode=PickingMode.Ignore;
  accent.style.position=Position.Absolute;
  accent.style.left=0;
  accent.style.top=8;
  accent.style.bottom=8;
  accent.style.width=3;
  accent.style.backgroundColor=selected
   ?new Color(0.19f,0.94f,1f,1f)
   :Color.clear;
  row.Add(accent);

  if(leading!=null){
   leading.pickingMode=PickingMode.Ignore;
   leading.AddToClassList("ps-codex-v9-index-leading");
   leading.style.width=48;
   leading.style.height=48;
   leading.style.minWidth=48;
   leading.style.minHeight=48;
   leading.style.maxWidth=48;
   leading.style.maxHeight=48;
   leading.style.marginRight=12;
   leading.style.flexShrink=0;
   row.Add(leading);
  }

  var copy=Container("ps-codex-v9-index-copy");
  copy.pickingMode=PickingMode.Ignore;
  copy.style.flexGrow=1;
  copy.style.flexShrink=1;
  copy.style.justifyContent=Justify.Center;
  var name=new Label(primary){pickingMode=PickingMode.Ignore};
  name.AddToClassList("ps-codex-v9-index-name");
  name.style.fontSize=18;
  name.style.color=selected
   ?new Color(1f,0.88f,0.72f,1f)
   :new Color(0.89f,0.84f,0.76f,1f);
  copy.Add(name);
  if(!string.IsNullOrEmpty(secondary)){
   var sub=new Label(secondary){pickingMode=PickingMode.Ignore};
   sub.AddToClassList("ps-codex-v9-index-subtitle");
   sub.style.fontSize=11;
   sub.style.color=new Color(0.56f,0.55f,0.62f,1f);
   copy.Add(sub);
  }
  row.Add(copy);
  return row;
 }

 void ApplyCodexIndexRowState(Button row,bool selected){
  if(row==null)return;
  row.style.backgroundImage=StyleKeyword.None;
  row.style.backgroundColor=selected
   ?new Color(0.12f,0.035f,0.105f,0.98f)
   :new Color(0.025f,0.03f,0.065f,0.96f);
  row.style.borderLeftWidth=selected?3:1;
  row.style.borderLeftColor=selected
   ?new Color(0.19f,0.94f,1f,1f)
   :new Color(0.39f,0.30f,0.25f,0.8f);
  var accent=row.Q<VisualElement>(className:"ps-codex-v9-index-accent");
  if(accent!=null)accent.style.backgroundColor=selected
   ?new Color(0.19f,0.94f,1f,1f)
   :Color.clear;
  var label=row.Q<Label>(className:"ps-codex-v9-index-name");
  if(label!=null)label.style.color=selected
   ?new Color(1f,0.88f,0.72f,1f)
   :new Color(0.89f,0.84f,0.76f,1f);
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
  row.Add(PackspireUiFactory.ManagementV6Art(
   RoleTicketPlate(kind),
   "ps-management-v6-bg ps-status-role-ticket-plate"
  ));
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
  row.Add(PackspireUiFactory.SystemIcon(RoleKindIcon(kind),"ps-status-role-kind-icon"));
  if(equipped){
   var stamp=Container("ps-seal-mark ps-status-role-equip-seal");
   stamp.pickingMode=PickingMode.Ignore;
   stamp.Add(new Label("装"){pickingMode=PickingMode.Ignore});
   row.Add(stamp);
  }
  return row;
 }

 static PackspireUiFactory.PopIcon RoleKindIcon(string kind){
  if(string.IsNullOrEmpty(kind))return PackspireUiFactory.PopIcon.RoleBasic;
  if(kind.Contains("勢力"))return PackspireUiFactory.PopIcon.RoleFaction;
  if(kind.Contains("複合"))return PackspireUiFactory.PopIcon.RoleComposite;
  if(kind.Contains("隠し"))return PackspireUiFactory.PopIcon.RoleHidden;
  if(kind.Contains("上級"))return PackspireUiFactory.PopIcon.RoleAdvanced;
  return PackspireUiFactory.PopIcon.RoleBasic;
 }

 static PackspireUiFactory.ManagementV6Piece RoleTicketPlate(string kind){
  var icon=RoleKindIcon(kind);
  return icon switch{
   PackspireUiFactory.PopIcon.RoleAdvanced=>PackspireUiFactory.ManagementV6Piece.RoleBlue,
   PackspireUiFactory.PopIcon.RoleComposite=>PackspireUiFactory.ManagementV6Piece.RoleGreen,
   PackspireUiFactory.PopIcon.RoleFaction=>PackspireUiFactory.ManagementV6Piece.RoleGold,
   PackspireUiFactory.PopIcon.RoleHidden=>PackspireUiFactory.ManagementV6Piece.RoleViolet,
   _=>PackspireUiFactory.ManagementV6Piece.RoleRed
  };
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
  bool roleBar=labels.Length>0&&labels[0].Contains("基本");
  for(int i=0;i<labels.Length;i++){
   int index=i;
   var button=PackspireUiFactory.Button(labels[i],()=>onPick(index));
   button.AddToClassList("ps-mgmt-filter");
   button.AddToClassList("ps-action-secondary");
   var plate=roleBar
    ?i switch{
     0=>PackspireUiFactory.ManagementV6Piece.RoleTabRed,
     1=>PackspireUiFactory.ManagementV6Piece.RoleTabBlue,
     2=>PackspireUiFactory.ManagementV6Piece.RoleTabGreen,
     3=>PackspireUiFactory.ManagementV6Piece.RoleTabGold,
     _=>PackspireUiFactory.ManagementV6Piece.RoleTabViolet
    }
    :i switch{
     0=>PackspireUiFactory.ManagementV6Piece.ArchiveTabAll,
     1=>PackspireUiFactory.ManagementV6Piece.ArchiveTabWeapon,
     2=>PackspireUiFactory.ManagementV6Piece.ArchiveTabArmor,
     _=>PackspireUiFactory.ManagementV6Piece.ArchiveTabRelic
    };
   button.Add(PackspireUiFactory.ManagementV6Art(
    plate,
    "ps-management-v6-bg ps-mgmt-filter-plate"
   ));
   VisualElement icon=labels[i] switch{
    "使用中"=>PackspireUiFactory.ManagementArt(PackspireUiFactory.ManagementChrome.Confirm,"ps-mgmt-filter-icon ps-management-filter-medallion"),
    "装備"=>PackspireUiFactory.ManagementArt(PackspireUiFactory.ManagementChrome.AllItems,"ps-mgmt-filter-icon ps-management-filter-medallion"),
    "役職"=>PackspireUiFactory.ManagementArt(PackspireUiFactory.ManagementChrome.RoleCrest,"ps-mgmt-filter-icon ps-management-filter-medallion"),
    "敵"=>PackspireUiFactory.ManagementArt(PackspireUiFactory.ManagementChrome.WeaponCategory,"ps-mgmt-filter-icon ps-management-filter-medallion"),
    "すべて"=>PackspireUiFactory.ManagementArt(PackspireUiFactory.ManagementChrome.AllItems,"ps-mgmt-filter-icon ps-management-filter-medallion"),
    "武器"=>PackspireUiFactory.ManagementArt(PackspireUiFactory.ManagementChrome.WeaponCategory,"ps-mgmt-filter-icon ps-management-filter-medallion"),
    "防具"=>PackspireUiFactory.ManagementArt(PackspireUiFactory.ManagementChrome.ArmorCategory,"ps-mgmt-filter-icon ps-management-filter-medallion"),
    "道具"=>PackspireUiFactory.ManagementArt(PackspireUiFactory.ManagementChrome.SupplyCategory,"ps-mgmt-filter-icon ps-management-filter-medallion"),
    "遺物"=>PackspireUiFactory.SystemIcon(PackspireUiFactory.PopIcon.Relic,"ps-mgmt-filter-icon ps-management-filter-medallion"),
    "基本"=>PackspireUiFactory.SystemIcon(PackspireUiFactory.PopIcon.RoleBasic,"ps-mgmt-filter-icon ps-management-filter-medallion"),
    "上級"=>PackspireUiFactory.SystemIcon(PackspireUiFactory.PopIcon.RoleAdvanced,"ps-mgmt-filter-icon ps-management-filter-medallion"),
    "複合"=>PackspireUiFactory.SystemIcon(PackspireUiFactory.PopIcon.RoleComposite,"ps-mgmt-filter-icon ps-management-filter-medallion"),
    "勢力"=>PackspireUiFactory.SystemIcon(PackspireUiFactory.PopIcon.RoleFaction,"ps-mgmt-filter-icon ps-management-filter-medallion"),
    "隠し"=>PackspireUiFactory.SystemIcon(PackspireUiFactory.PopIcon.RoleHidden,"ps-mgmt-filter-icon ps-management-filter-medallion"),
    _=>PackspireUiFactory.ManagementArt(PackspireUiFactory.ManagementChrome.AllItems,"ps-mgmt-filter-icon ps-management-filter-medallion")
   };
   // Keep the atlas plate as the first child so it always paints behind the
   // category icon and label.
   button.Add(icon);
   if(i==selectedIndex)button.AddToClassList("ps-selected");
   bar.Add(button);
  }
  return bar;
 }

 VisualElement ManagementSection(string title,string body,bool dim=false){
  var section=Container(dim?"ps-mgmt-section ps-mgmt-section-dim":"ps-mgmt-section");
  section.Add(PackspireUiFactory.ManagementV6Art(
   title!=null&&title.Contains("LINK")
    ?PackspireUiFactory.ManagementV6Piece.ArchiveLinkHeader
    :PackspireUiFactory.ManagementV6Piece.ArchiveDivider,
   "ps-management-v6-bg ps-mgmt-section-v6-rule"
  ));
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

}
}
