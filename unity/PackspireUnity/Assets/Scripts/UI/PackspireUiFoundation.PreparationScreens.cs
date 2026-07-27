using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire {
public sealed partial class PackspireUiFoundation {
// Packing screen construction and primary layout.
 void BuildPacking(){
  var run=game.UiRun;
  if(run==null){game.UiNavigate(ScreenId.Hub);return;}
  if(!string.IsNullOrEmpty(selectedPackingUid)&&!run.inventory.Any(x=>x.uid==selectedPackingUid))selectedPackingUid="";
  packingDragUid="";
  packingDragging=false;
  packingTapWasSelected=false;
  packingDragFromList=false;
  packingDragGrip=Vector2Int.zero;
  packingDragGhost=null;
  packingPopupElement=null;

  var formula=BackpackSystem.Formula(run);
  packingRotation=StorageFormulaSystem.ClampRotation(formula.core.rotation,packingRotation);
  var build=BackpackSystem.Build(run);

  var root=Container("ps-rite");
  root.pickingMode=PickingMode.Position;
  packingRootElement=root;
  screenRoot.Add(root);
  var courtyard=HubBackgroundArt();
  if(courtyard!=null)
   root.Add(Image(courtyard,new Rect(0,0,1,1),"ps-rite-scene-bg",ScaleMode.ScaleAndCrop));
  var shade=Container("ps-rite-scene-shade");
  shade.pickingMode=PickingMode.Ignore;
  root.Add(shade);
  RegisterPackingDrag(root);

  var top=Container("ps-rite-top");
  DressRiteFrame(top);
  var brand=Container("ps-rite-brand");
  var brandMark=Container("ps-rite-brand-mark");
  brandMark.pickingMode=PickingMode.Ignore;
  brandMark.Add(PackspireUiFactory.SystemIcon(PackspireUiFactory.PopIcon.Packing,"ps-rite-brand-icon"));
  brand.Add(brandMark);
  var topTitle=Container("ps-rite-top-title");
  var topEyebrow=new Label("ATELIER  /  FORGE"){pickingMode=PickingMode.Ignore};
  topEyebrow.AddToClassList("ps-rite-top-eyebrow");
  topEyebrow.AddToClassList("ps-chrome-eyebrow");
  topTitle.Add(topEyebrow);
  var topName=new Label("収納術式"){pickingMode=PickingMode.Ignore};
  topName.AddToClassList("ps-rite-top-name");
  topName.AddToClassList("ps-chrome-title");
  topTitle.Add(topName);
  var topSub=new Label("鍛冶場の窯で術装を編む"){pickingMode=PickingMode.Ignore};
  topSub.AddToClassList("ps-rite-top-sub");
  topTitle.Add(topSub);
  brand.Add(topTitle);
  top.Add(brand);
  var topActions=Container("ps-rite-top-actions");
  if(game.UiPackingAtBase){
   var back=PackspireUiFactory.Button("戻る",()=>{
    game.UiPackingCapture();
    packingTemplateCommitted=false;
    game.UiNavigate(ScreenId.Hub);
   });
   back.AddToClassList("ps-rite-chip");
   topActions.Add(back);
  }
  top.Add(topActions);
  root.Add(top);

  var body=Container("ps-rite-body");
  root.Add(body);

  // Left: floating equip tray (header + filters pinned above scroll)
  var left=Container("ps-rite-left");
  DressRiteFrame(left);
  left.Add(PackspireUiFactory.SystemOrnament(PackspireUiFactory.PopOrnament.VerticalBoundary,"ps-rite-column-boundary"));
  var leftHeader=Container("ps-rite-left-header");
  leftHeader.Add(RiteSectionHead("01","術装"));
  packingFilterRowElement=BuildPackingFilterRow();
  leftHeader.Add(packingFilterRowElement);
  left.Add(leftHeader);
  var listScroll=new ScrollView(ScrollViewMode.Vertical);
  packingEquipScrollElement=listScroll;
  listScroll.AddToClassList("ps-rite-equip-scroll");
  listScroll.verticalScrollerVisibility=ScrollerVisibility.Auto;
  listScroll.scrollOffset=new Vector2(0,packingEquipScrollY);
  var grid=Container("ps-rite-equip-grid");
  int cols=Screen.width>=1600?5:Screen.width>=1280?4:3;
  float tilePct=(100f/cols)-1.8f;
  foreach(var item in run.inventory){
   StorageFormulaSystem.EnsureItemRolled(item);
   var def=GameCatalog.Items[item.templateId];
   if(!PackingFilterMatch(def.type))continue;
   var entry=item;
   bool placed=run.placements.Any(x=>x.itemUid==entry.uid);
   bool selectedRow=entry.uid==selectedPackingUid;
   var tile=new VisualElement();
   tile.AddToClassList("ps-rite-equip-tile");
   tile.style.width=Length.Percent(tilePct);
   tile.focusable=true;
   tile.pickingMode=PickingMode.Position;
   tile.tooltip=def.name;
   if(selectedRow)tile.AddToClassList("ps-selected");
   if(placed){
    tile.AddToClassList("ps-rite-equip-placed");
    var badge=new Label("装着"){pickingMode=PickingMode.Ignore};
    badge.AddToClassList("ps-rite-equip-badge");
    tile.Add(badge);
   }
   tile.Add(Atlas(game.UiEquipmentArt,ItemUv(entry.templateId),"ps-rite-equip-art"));
   var name=new Label(def.name){pickingMode=PickingMode.Ignore};
   name.AddToClassList("ps-rite-equip-name");
   tile.Add(name);
   BindPackingDragSource(tile,entry.uid,formula,true);
   grid.Add(tile);
  }
  if(grid.childCount==0)listScroll.Add(RiteEmptyNote(run.inventory.Count==0?"術装がありません":"この分類にはありません"));
  else listScroll.Add(grid);
  left.Add(listScroll);
  body.Add(left);

  // Center: ritual kiln (no admin box)
  var center=Container("ps-rite-center");
  var kilnRow=Container("ps-rite-kiln-row");
  var kiln=Container("ps-rite-kiln");
  packingKilnElement=kiln;
  kiln.AddToClassList("ps-rite-core-"+formula.core.id);
  kiln.Add(BuildMagicCircleLayers(formula));
  var circle=Container("ps-rite-circle");
  circle.pickingMode=PickingMode.Position;
  circle.RegisterCallback<ClickEvent>(OnPackingCircleClick);
  circle.Add(BuildRiteGrid(run,formula));
  kiln.Add(circle);
  if(!string.IsNullOrEmpty(selectedPackingUid))
   kiln.Add(BuildPackingSelectDock(run,formula));
  kilnRow.Add(kiln);
  center.Add(kilnRow);

  var kilnRail=Container("ps-rite-kiln-rail");
  packingKilnRailElement=kilnRail;
  DressRiteFrame(kilnRail);
  kilnRail.Add(BuildPackingColorCounters(build));
  var railSpacer=Container("ps-rite-rail-spacer");
  kilnRail.Add(railSpacer);
  var cardsBtn=PackspireUiFactory.Button($"術式札  {run.selectedCardSlots.Count}",()=>{packingFormulaOpen=false;packingCardsOpen=true;BuildPackingAgain();});
  cardsBtn.AddToClassList("ps-rite-tool");
  cardsBtn.AddToClassList("ps-rite-tool-primary");
  cardsBtn.Insert(0,PackspireUiFactory.SystemIcon(PackspireUiFactory.PopIcon.CardCheck,"ps-rite-tool-icon"));
  kilnRail.Add(cardsBtn);
  center.Add(kilnRail);
  StartPackingCirclePulse(center);
  body.Add(center);

  var rightShell=Container("ps-rite-right-shell");
  DressRiteFrame(rightShell);
  rightShell.Add(PackspireUiFactory.SystemOrnament(PackspireUiFactory.PopOrnament.OpenCorner,"ps-rite-detail-corner"));
  var right=new ScrollView(ScrollViewMode.Vertical);
  packingRightScrollElement=right;
  right.AddToClassList("ps-rite-right");
  right.scrollOffset=new Vector2(0,packingRightScrollY);
  if(!packingTemplateCommitted){
   BuildFormulaTemplateBrowser(right);
  } else {
   right.Add(BuildFormulaTemplateCommitted(formula));
   var selected=run.inventory.FirstOrDefault(x=>x.uid==selectedPackingUid);
   if(selected!=null)BuildPackingItemDetail(right,selected,build);
   else BuildPackingOverview(right,run,build);
  }
  rightShell.Add(right);
  body.Add(rightShell);

  if(packingFormulaOpen){packingPopupElement=BuildFormulaPopup(run,formula);root.Add(packingPopupElement);}
  if(packingCardsOpen){packingPopupElement=BuildCardsPopup(run,build);root.Add(packingPopupElement);}

  RestorePackingScroll(listScroll,right);
 }
}
}
