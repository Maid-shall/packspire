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

  var background=RequireViewElement<VisualElement>(shell,prefix+"-background");
  var backgroundTexture=HubBackgroundArt();
  if(backgroundTexture==null)backgroundTexture=CourtyardArt();
  if(backgroundTexture!=null)
   background.Insert(0,Image(backgroundTexture,new Rect(0,0,1,1),"ps-mgmt-bg",ScaleMode.ScaleAndCrop));

  var header=RequireViewElement<VisualElement>(shell,prefix+"-header");
  header.Add(ManagementBrand(
   eyebrow,
   title,
   PackspireUiFactory.ManagementChrome.RoleCrest
  ));

  mgmtOverviewHost=null;
  mgmtDetailHero=RequireViewElement<VisualElement>(shell,prefix+"-detail-hero");
  mgmtDetailHero.style.display=DisplayStyle.None;
  mgmtDetailArtHost=RequireViewElement<VisualElement>(shell,prefix+"-detail-art");
  mgmtDetailSummaryHost=RequireViewElement<VisualElement>(shell,prefix+"-detail-summary");
  mgmtListHeader=RequireViewElement<VisualElement>(shell,prefix+"-list-header");
  listScroll=RequireViewElement<ScrollView>(shell,prefix+"-list-scroll");
  detailScroll=RequireViewElement<ScrollView>(shell,prefix+"-detail-scroll");

  if(layout==ManagementLayout.StatusOverview){
   var characterColumn=RequireViewElement<VisualElement>(shell,"status-character-column");
   characterColumn.Add(PackspireUiFactory.SystemOrnament(
    PackspireUiFactory.PopOrnament.VerticalBoundary,
    "ps-mgmt-column-boundary"
   ));
   var rolesColumn=RequireViewElement<VisualElement>(shell,"status-roles-column");
   rolesColumn.Add(PackspireUiFactory.SystemOrnament(
    PackspireUiFactory.PopOrnament.VerticalBoundary,
    "ps-mgmt-column-boundary"
   ));
   var detailSurface=RequireViewElement<VisualElement>(shell,"status-detail-surface");
   detailSurface.Insert(0,PackspireUiFactory.ManagementArt(
    PackspireUiFactory.ManagementChrome.DetailCorner,
    "ps-mgmt-open-corner ps-management-detail-corner"
   ));
   var characterScroll=RequireViewElement<ScrollView>(shell,"status-character-scroll");
   mgmtOverviewHost=RequireViewElement<VisualElement>(shell,"status-character-host");
   StretchMgmtScrollContent(characterScroll);
   StretchMgmtScrollContent(listScroll);
   StretchMgmtScrollContent(detailScroll,false);
  }else{
   var indexColumn=RequireViewElement<VisualElement>(shell,"compendium-index-column");
   indexColumn.Add(PackspireUiFactory.SystemOrnament(
    PackspireUiFactory.PopOrnament.VerticalBoundary,
    "ps-mgmt-column-boundary"
   ));
   StretchMgmtScrollContent(listScroll);
   StretchMgmtScrollContent(detailScroll,true);
  }

  mgmtVaultGrid=null;
  mgmtVaultFooter=null;
  mgmtListScroll=listScroll;
  mgmtDetailScroll=detailScroll;
  return true;
 }
}
}
