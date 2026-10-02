using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire {
public sealed partial class PackspireUiFoundation {
// Packing screen construction and primary layout.
 bool packingFormulaBrowserOpen;
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

  var root=CloneView("UI/PackspirePackingView","ps-rite");
  if(root==null){
   Debug.LogError("Packing view could not be created.");
   return;
  }
  root.pickingMode=PickingMode.Position;
  root.EnableInClassList("packing--formulas",packingFormulaBrowserOpen);
  root.EnableInClassList("packing--effects",!packingFormulaBrowserOpen);
  packingRootElement=root;
  screenRoot.Add(root);
  RequireViewElement<VisualElement>(root,"packing-background");
  RegisterPackingDrag(root);

  RequireViewElement<VisualElement>(root,"packing-top");
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
   tile.Add(VaultItemDisplayArt(entry.templateId,"ps-rite-equip-art"));
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
  RequireViewElement<Label>(root,"packing-board-size").text=$"{formula.core.width} × {formula.core.height}";
  var circle=RequireViewElement<VisualElement>(root,"packing-board-host");
  circle.pickingMode=PickingMode.Position;
  circle.RegisterCallback<ClickEvent>(OnPackingCircleClick);
  circle.Add(BuildRiteGrid(run,formula));
  BindPackingBoardGeometry(circle,formula);
  if(!string.IsNullOrEmpty(selectedPackingUid))
   kiln.Add(BuildPackingSelectDock(run,formula));
  var kilnRail=RequireViewElement<VisualElement>(root,"packing-kiln-rail");
  packingKilnRailElement=kilnRail;
  kilnRail.Add(BuildPackingSealCounters(run,build));
  var railSpacer=Container("ps-rite-rail-spacer");
  kilnRail.Add(railSpacer);
  var cardsBtn=PackspireUiFactory.Button($"戦闘札・配達印",()=>{packingFormulaOpen=false;packingCardsOpen=true;BuildPackingAgain();});
  cardsBtn.AddToClassList("ps-rite-tool");
  cardsBtn.AddToClassList("ps-rite-tool-primary");
  kilnRail.Add(cardsBtn);


  RequireViewElement<VisualElement>(root,"packing-right-shell");
  var right=RequireViewElement<ScrollView>(root,"packing-right-scroll");
  packingRightScrollElement=right;
  right.scrollOffset=new Vector2(0,packingRightScrollY);
  RequireViewElement<Button>(root,"packing-effects-tab").clicked+=()=>{packingFormulaBrowserOpen=false;BuildPackingAgain();};
  RequireViewElement<Button>(root,"packing-formulas-tab").clicked+=()=>{packingFormulaBrowserOpen=true;BuildPackingAgain();};
  var courier=CharacterCatalog.Get(game.UiMeta.selectedCharacterId);
  RequireViewElement<VisualElement>(root,"packing-courier-art").Add(CharacterPortraitFront(courier,"ps-packing-courier-image"));
  RequireViewElement<Label>(root,"packing-courier-name").text=courier.name;
  RequireViewElement<Label>(root,"packing-courier-role").text=GameCatalog.Roles.TryGetValue(game.UiMeta.currentRole,out var role)?role.name:"配達員";
  RequireViewElement<Button>(root,"packing-save").clicked+=()=>{game.UiPackingSave();ShowToast("荷造りを保存しました");};
  if(packingFormulaBrowserOpen){
   BuildFormulaTemplateBrowser(right);
  } else {

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
