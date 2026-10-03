using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire {
public sealed partial class PackspireUiFoundation {
 bool packingFormulaBrowserOpen;
 ScrollView packingBoardScrollElement,packingLinksScrollElement,packingColorsScrollElement;
 bool packingContextHidden,packingLargeContextOpen,packingUnplacedOnly,packingSortByType;
 string packingSearchText="";
 float packingZoom=1f,packingLinksScrollY,packingColorsScrollY;
 Vector2 packingBoardScrollOffset;
 void BuildPacking(){
  if(game.UiRun==null){game.UiNavigate(ScreenId.Hub);return;}
  var root=CloneView("UI/PackspirePackingView","ps-rite");
  if(root==null)return;
  packingRootElement=root;
  screenRoot.Add(root);
  packingDragUid="";
  packingDragging=false;
  packingDragGhost=null;
  RegisterPackingDrag(root);
  packingFilterRowElement=RequireViewElement<VisualElement>(root,"packing-filter-host");
  packingEquipScrollElement=RequireViewElement<ScrollView>(root,"packing-equip-scroll");
  packingRightScrollElement=RequireViewElement<ScrollView>(root,"packing-right-scroll");
  packingKilnElement=RequireViewElement<VisualElement>(root,"packing-kiln");
  packingKilnRailElement=RequireViewElement<VisualElement>(root,"packing-kiln-rail");
  packingBoardScrollElement=RequireViewElement<ScrollView>(root,"packing-board-scroll");
  packingLinksScrollElement=RequireViewElement<ScrollView>(root,"packing-links-scroll");
  packingColorsScrollElement=RequireViewElement<ScrollView>(root,"packing-colors-scroll");
  var search=RequireViewElement<TextField>(root,"packing-search");
  search.SetValueWithoutNotify(packingSearchText);
  search.RegisterValueChangedCallback(evt=>{packingSearchText=evt.newValue;ResetPackingInventoryScroll();BuildPackingAgain();});
  var unplaced=RequireViewElement<Toggle>(root,"packing-unplaced");
  unplaced.SetValueWithoutNotify(packingUnplacedOnly);
  unplaced.RegisterValueChangedCallback(evt=>{packingUnplacedOnly=evt.newValue;ResetPackingInventoryScroll();BuildPackingAgain();});
  PackingAction("packing-sort",()=>{packingSortByType=!packingSortByType;ResetPackingInventoryScroll();BuildPackingAgain();});
  foreach(var category in new[]{"all","weapon","armor","rune","supply"}){
   var id=category=="all"?"":category;
   PackingAction("packing-filter-"+category,()=>{packingEquipFilter=id;ResetPackingInventoryScroll();BuildPackingAgain();});
  }
  PackingAction("packing-zoom-in",()=>ChangePackingZoom(.15f));
  PackingAction("packing-zoom-out",()=>ChangePackingZoom(-.15f));
  PackingAction("packing-fit",FitPackingBoard);
  PackingAction("packing-context-toggle",()=>{var core=BackpackSystem.Formula(game.UiRun).core;if(core.width>6||core.height>4)packingLargeContextOpen=!packingLargeContextOpen;else packingContextHidden=!packingContextHidden;UpdatePackingBoardMode();});
  PackingAction("packing-rotate",()=>RotateSelectedPacking(BackpackSystem.Formula(game.UiRun)));
  PackingAction("packing-remove",()=>{game.UiPackingRemove(selectedPackingUid);BuildPackingAgain();});
  PackingAction("packing-loadouts",()=>{packingFormulaBrowserOpen=true;BuildPackingAgain();});
  PackingAction("packing-loadout-close",()=>{packingFormulaBrowserOpen=false;BuildPackingAgain();});
  PackingAction("packing-formula-edit",()=>{packingFormulaBrowserOpen=false;packingCardsOpen=false;packingFormulaOpen=true;BuildPackingAgain();});
  PackingAction("packing-loadout-add",()=>{
   game.UiPackingCapture();packingFormulaBrowserOpen=false;packingFormulaSection="";packingCardsOpen=false;
   game.UiPackingCreateLoadout();packingFormulaOpen=true;selectedPackingUid="";BuildPackingAgain();
  });
  PackingAction("packing-cards",()=>{packingFormulaOpen=false;packingCardsOpen=true;BuildPackingAgain();});
  PackingAction("packing-save",()=>{game.UiPackingSave();if(game.UiPackingAtBase)ShowToast("荷造りを保存しました");});
  PackingAction("packing-back",NavGoBack);
  packingBoardScrollElement.contentViewport.RegisterCallback<GeometryChangedEvent>(_=>ResizePackingBoard());
  RefreshPackingContent();
 }
 void PackingAction(string name,System.Action action)=>RequireViewElement<Button>(packingRootElement,name).clicked+=action;
 bool TryHandlePackingCancel(){
  if(packingRootElement==null)return false;
  if(packingDragging){
   EndPackingDrag(false);
   if(packingRootElement.HasPointerCapture(PointerId.mousePointerId))packingRootElement.ReleasePointer(PointerId.mousePointerId);
   return true;
  }
  for(var target=packingRootElement.panel?.focusController.focusedElement as VisualElement;target!=null;target=target.parent)
   if(target is TextField){target.Blur();return true;}
  if(packingFormulaOpen){CloseFormulaPopup();return true;}
  if(packingCardsOpen){packingCardsOpen=false;BuildPackingAgain();return true;}
  if(packingFormulaBrowserOpen){packingFormulaBrowserOpen=false;BuildPackingAgain();return true;}
  return false;
 }
 void ResetPackingInventoryScroll(){packingEquipScrollY=0;if(packingEquipScrollElement!=null)packingEquipScrollElement.scrollOffset=Vector2.zero;}
 void ChangePackingZoom(float delta){packingZoom=Mathf.Clamp(packingZoom+delta,.85f,2f);ResizePackingBoard();}
 void FitPackingBoard(){
  var core=BackpackSystem.Formula(game.UiRun).core;
  if(packingGridElement.customStyle.TryGetValue(new CustomStyleProperty<float>("--packing-cell-side"),out var natural)&&packingGridElement.customStyle.TryGetValue(new CustomStyleProperty<float>("--packing-min-cell-side"),out var minimum)){
   var viewport=packingBoardScrollElement.contentViewport.contentRect;
   float x=packingKilnElement.resolvedStyle.paddingLeft+packingKilnElement.resolvedStyle.paddingRight;
   float y=packingKilnElement.resolvedStyle.paddingTop+packingKilnElement.resolvedStyle.paddingBottom;
   float fit=Mathf.Min((viewport.width-x)/core.width,(viewport.height-y)/core.height);
   packingZoom=Mathf.Clamp(fit/natural,minimum/natural,1f);
   if(fit<minimum)ShowToast("マスの見やすさを優先しています。盤内スクロールで全体を確認できます。");
  }else packingZoom=1f;
  packingBoardScrollOffset=Vector2.zero;packingBoardScrollElement.scrollOffset=Vector2.zero;ResizePackingBoard();
 }
 void UpdatePackingBoardMode(){
  var core=BackpackSystem.Formula(game.UiRun).core;
  bool large=core.width>6||core.height>4;
  packingRootElement.EnableInClassList("packing--large-board",large);
  packingRootElement.EnableInClassList("packing--context-hidden",large||packingContextHidden);
  packingRootElement.EnableInClassList("packing--context-peek",large&&packingLargeContextOpen);
  RequireViewElement<Button>(packingRootElement,"packing-context-toggle").text=(large?!packingLargeContextOpen:packingContextHidden)?"選択情報を表示":"選択情報を畳む";
 }
}
}
