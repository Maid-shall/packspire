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

  var root=CloneView("UI/PackspirePackingView","ps-rite ps-rite-v3");
  if(root==null){
   Debug.LogError("Packing view could not be created.");
   return;
  }
  root.pickingMode=PickingMode.Position;
  packingRootElement=root;
  screenRoot.Add(root);
  RequireViewElement<VisualElement>(root,"packing-background");
  RegisterPackingDrag(root);

  RequireViewElement<VisualElement>(root,"packing-top");
  var topActions=RequireViewElement<VisualElement>(root,"packing-top-actions");
  if(game.UiPackingAtBase){
   var back=PackspireUiFactory.Button("戻る",()=>{
    game.UiPackingCapture();
    packingTemplateCommitted=false;
    game.UiNavigate(ScreenId.Hub);
   });
   back.AddToClassList("ps-rite-chip");
   topActions.Add(back);
  }
  RequireViewElement<VisualElement>(root,"packing-body");

  // Left: floating equip tray (header + filters pinned above scroll)
  RequireViewElement<VisualElement>(root,"packing-left");
  packingFilterRowElement=BuildPackingFilterRow();
  RequireViewElement<VisualElement>(root,"packing-filter-host").Add(packingFilterRowElement);
  var listScroll=RequireViewElement<ScrollView>(root,"packing-equip-scroll");
  packingEquipScrollElement=listScroll;
  listScroll.verticalScrollerVisibility=ScrollerVisibility.Auto;
  listScroll.scrollOffset=new Vector2(0,packingEquipScrollY);
  var grid=RequireViewElement<VisualElement>(root,"packing-equip-grid");
  foreach(var item in run.inventory){
   StorageFormulaSystem.EnsureItemRolled(item);
   var def=GameCatalog.Items[item.templateId];
   if(!PackingFilterMatch(def.type))continue;
   var entry=item;
   bool placed=run.placements.Any(x=>x.itemUid==entry.uid);
   bool selectedRow=entry.uid==selectedPackingUid;
   var tile=new VisualElement();
   tile.AddToClassList("ps-rite-equip-tile");
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
  if(grid.childCount==0)grid.Add(RiteEmptyNote(run.inventory.Count==0?"術装がありません":"この分類にはありません"));

  // Center: ritual kiln (no admin box)
  var center=RequireViewElement<VisualElement>(root,"packing-center");
  var kiln=RequireViewElement<VisualElement>(root,"packing-kiln");
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
  var kilnRail=RequireViewElement<VisualElement>(root,"packing-kiln-rail");
  packingKilnRailElement=kilnRail;
  kilnRail.Add(BuildPackingColorCounters(build));
  var railSpacer=Container("ps-rite-rail-spacer");
  kilnRail.Add(railSpacer);
  var cardsBtn=PackspireUiFactory.Button($"術式札  {run.selectedCardSlots.Count}",()=>{packingFormulaOpen=false;packingCardsOpen=true;BuildPackingAgain();});
  cardsBtn.AddToClassList("ps-rite-tool");
  cardsBtn.AddToClassList("ps-rite-tool-primary");
  cardsBtn.Insert(0,PackspireUiFactory.SystemIcon(PackspireUiFactory.PopIcon.CardCheck,"ps-rite-tool-icon"));
  kilnRail.Add(cardsBtn);
  StartPackingCirclePulse(center);

  RequireViewElement<VisualElement>(root,"packing-right-shell");
  var right=RequireViewElement<ScrollView>(root,"packing-right-scroll");
  packingRightScrollElement=right;
  right.scrollOffset=new Vector2(0,packingRightScrollY);
  if(!packingTemplateCommitted){
   BuildFormulaTemplateBrowser(right);
  } else {
   right.Add(BuildFormulaTemplateCommitted(formula));
   var selected=run.inventory.FirstOrDefault(x=>x.uid==selectedPackingUid);
   if(selected!=null)BuildPackingItemDetail(right,selected,build);
   else BuildPackingOverview(right,run,build);
  }
  if(packingFormulaOpen){packingPopupElement=BuildFormulaPopup(run,formula);root.Add(packingPopupElement);}
  if(packingCardsOpen){packingPopupElement=BuildCardsPopup(run,build);root.Add(packingPopupElement);}

  RestorePackingScroll(listScroll,right);
 }
}
}
