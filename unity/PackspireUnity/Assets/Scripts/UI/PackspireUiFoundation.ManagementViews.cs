using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire {
public sealed partial class PackspireUiFoundation {
 bool TryBuildManagementView(
  string eyebrow,
  string title,
  ManagementLayout layout,
  out VisualElement shell,
  out ScrollView listScroll,
  out ScrollView detailScroll
 ){
  shell=null;
  listScroll=null;
  detailScroll=null;
  if(layout==ManagementLayout.VaultListDetail)return false;

  string viewPath=layout==ManagementLayout.StatusOverview
   ?"UI/PackspireStatusView"
   :"UI/PackspireCompendiumView";
  string prefix=layout==ManagementLayout.StatusOverview?"status":"compendium";
  string rootClass="ps-mgmt-screen ps-mgmt-layout-"+LayoutClass(layout)+" ps-dark-surface";
  rootClass+=layout==ManagementLayout.StatusOverview
   ?" ps-management-v3 ps-status-v2"
   :" ps-management-v3 ps-codex-v8";
  shell=CloneView(viewPath,rootClass);
  if(shell==null)return false;

  RequireViewElement<VisualElement>(shell,prefix+"-background");

  RequireViewElement<VisualElement>(shell,prefix+"-header");
  RequireViewElement<Label>(shell,prefix+"-header-eyebrow").text=eyebrow;
  RequireViewElement<Label>(shell,prefix+"-header-title").text=title;

  mgmtOverviewHost=null;
  mgmtDetailHero=RequireViewElement<VisualElement>(shell,prefix+"-detail-hero");
  mgmtDetailHero.style.display=DisplayStyle.None;
  mgmtDetailArtHost=RequireViewElement<VisualElement>(shell,prefix+"-detail-art");
  mgmtDetailSummaryHost=RequireViewElement<VisualElement>(shell,prefix+"-detail-summary");
  mgmtListHeader=RequireViewElement<VisualElement>(shell,prefix+"-list-header");
  listScroll=RequireViewElement<ScrollView>(shell,prefix+"-list-scroll");
  detailScroll=RequireViewElement<ScrollView>(shell,prefix+"-detail-scroll");

  if(layout==ManagementLayout.StatusOverview){
   RequireViewElement<VisualElement>(shell,"status-character-column");
   RequireViewElement<VisualElement>(shell,"status-roles-column");
   RequireViewElement<VisualElement>(shell,"status-detail-surface");
   var characterScroll=RequireViewElement<ScrollView>(shell,"status-character-scroll");
   mgmtOverviewHost=RequireViewElement<VisualElement>(shell,"status-character-host");
   statusAppointmentEyebrow=RequireViewElement<Label>(shell,"status-appointment-eyebrow");
   statusAppointmentTitle=RequireViewElement<Label>(shell,"status-appointment-title");
   statusAppointmentDetail=RequireViewElement<Label>(shell,"status-appointment-detail");
   statusAppointmentAction=RequireViewElement<Label>(shell,"status-appointment-action");
   StretchMgmtScrollContent(characterScroll);
   StretchMgmtScrollContent(listScroll);
   StretchMgmtScrollContent(detailScroll,false);
  }else{
   BindCompendiumView(shell);
   RequireViewElement<VisualElement>(shell,"compendium-index-column");
   StretchMgmtScrollContent(listScroll);
   StretchMgmtScrollContent(detailScroll,true);
  }

  mgmtVaultGrid=null;
  mgmtVaultFooter=null;
  mgmtListScroll=listScroll;
  mgmtDetailScroll=detailScroll;
  return true;
 }

 void BindCompendiumView(VisualElement shell){
  compendiumViewRoot=RequireViewElement<VisualElement>(shell,"compendium-layout");
  compendiumItemRecord=RequireViewElement<VisualElement>(shell,"compendium-item-record");
  compendiumGenericRecord=RequireViewElement<VisualElement>(shell,"compendium-generic-record");
  compendiumItemArtHost=RequireViewElement<VisualElement>(shell,"compendium-item-art");
  compendiumItemShapeHost=RequireViewElement<VisualElement>(shell,"compendium-shape-list");
  compendiumItemCardPanel=RequireViewElement<VisualElement>(shell,"compendium-card-panel");
  compendiumItemCardStage=RequireViewElement<VisualElement>(shell,"compendium-card-stage");
  compendiumItemLinkHost=RequireViewElement<VisualElement>(shell,"compendium-link-effect");
  compendiumItemSealAttribute=RequireViewElement<VisualElement>(shell,"compendium-seal-attribute");
  compendiumItemMeta=RequireViewElement<Label>(shell,"compendium-item-meta");
  compendiumItemName=RequireViewElement<Label>(shell,"compendium-item-name");
  compendiumItemDescription=RequireViewElement<Label>(shell,"compendium-item-description");
  compendiumItemShapeCount=RequireViewElement<Label>(shell,"compendium-shape-count");
  compendiumAcquisitionSource=RequireViewElement<Label>(shell,"compendium-acquisition-source");
  compendiumAcquisitionTier=RequireViewElement<Label>(shell,"compendium-acquisition-tier");
  compendiumDiscoveryCount=RequireViewElement<Label>(shell,"compendium-discovery-count");

  compendiumItemTab=RequireViewElement<Button>(shell,"compendium-tab-items");
  compendiumRoleTab=RequireViewElement<Button>(shell,"compendium-tab-roles");
  compendiumEnemyTab=RequireViewElement<Button>(shell,"compendium-tab-enemies");
  var nextPage=RequireViewElement<Button>(shell,"compendium-item-next-page");

  compendiumItemTab.clicked+=()=>SelectCompendiumTab(0);
  compendiumRoleTab.clicked+=()=>SelectCompendiumTab(1);
  compendiumEnemyTab.clicked+=()=>SelectCompendiumTab(2);
  nextPage.clicked+=ShowCompendiumLorePage;

  RequireViewElement<VisualElement>(shell,"compendium-tab-items-icon").Add(
   PackspireUiFactory.SystemIcon(PackspireUiFactory.PopIcon.Weapon)
  );
  RequireViewElement<VisualElement>(shell,"compendium-tab-roles-icon").Add(
   PackspireUiFactory.SystemIcon(PackspireUiFactory.PopIcon.RoleCurrent)
  );
  RequireViewElement<VisualElement>(shell,"compendium-tab-enemies-icon").Add(
   PackspireUiFactory.SystemIcon(PackspireUiFactory.PopIcon.RoleComposite)
  );
 }
 }
}
