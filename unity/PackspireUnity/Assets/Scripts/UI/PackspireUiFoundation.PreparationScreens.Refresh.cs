using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
namespace Packspire {
public sealed partial class PackspireUiFoundation {
 string packingPresentedSelectionUid="";
 // Keep the UXML shell and input focus alive; regenerate only data-bound children.
 void BuildPackingAgain(){if(!PackingScreenAlive())RebuildScreen(BuildPacking);else RefreshPackingContent();}
 bool PackingScreenAlive()=>packingRootElement!=null&&packingRootElement.panel!=null;
 void RefreshPackingContent(){
  var run=game.UiRun;
  if(run==null)return;
  CapturePackingScroll();
  if(!run.inventory.Any(x=>x.uid==selectedPackingUid))selectedPackingUid="";
  if(packingPresentedSelectionUid!=selectedPackingUid){packingRightScrollY=0;packingPresentedSelectionUid=selectedPackingUid;}
  var formula=BackpackSystem.Formula(run);
  packingRotation=StorageFormulaSystem.ClampRotation(formula.core.rotation,packingRotation);
  var root=packingRootElement;
  RequireViewElement<Label>(root,"packing-loadout-name").text=game.UiMeta.loadouts.FirstOrDefault(x=>x.id==game.UiMeta.selectedLoadoutId)?.name??"収納盤";
  RequireViewElement<Label>(root,"packing-board-size").text=$"{formula.core.width} × {formula.core.height} ／ {RotationLabel(formula.core.rotation)}";
  RequireViewElement<Label>(root,"packing-formula-summary").text=$"{formula.core.name} ／ {formula.conduit.name} ／ {formula.resonance.name} ／ {formula.stability.name}";
  RequireViewElement<Button>(root,"packing-save").text=game.UiPackingAtBase?"荷造りを保存":"保存して進む";
  UpdatePackingBoardMode();
  RefreshPackingInventory(run,formula);
  var host=RequireViewElement<VisualElement>(root,"packing-board-host");
  host.Clear();
  host.Add(BuildRiteGrid(run,formula));
  ResizePackingBoard();
  root.schedule.Execute(ResizePackingBoard);
  var build=BackpackSystem.Build(run);
  RefreshPackingEffects(run,build);
  packingPopupElement?.RemoveFromHierarchy();
  packingPopupElement=null;
  if(packingFormulaOpen){packingPopupElement=BuildFormulaPopup(run,formula);root.Add(packingPopupElement);}
  if(packingCardsOpen){packingPopupElement=BuildCardsPopup(run,build);root.Add(packingPopupElement);}
  root.EnableInClassList("packing--loadouts",packingFormulaBrowserOpen);
  var loadouts=RequireViewElement<ScrollView>(root,"packing-loadout-scroll");
  loadouts.Clear();
  if(packingFormulaBrowserOpen)BuildFormulaTemplateBrowser(loadouts);
  RestorePackingScroll(packingEquipScrollElement,packingRightScrollElement);
 }
 void RefreshPackingInventory(RunState run,ActiveStorageFormula formula){
  var grid=RequireViewElement<VisualElement>(packingRootElement,"packing-equip-grid");
  grid.Clear();
  var placed=run.placements.Select(x=>x.itemUid).ToHashSet();
  var entries=run.inventory.Where(x=>GameCatalog.Items.ContainsKey(x.templateId))
   .Where(x=>PackingFilterMatch(GameCatalog.Items[x.templateId].type))
   .Where(x=>!packingUnplacedOnly||!placed.Contains(x.uid))
   .Where(x=>string.IsNullOrWhiteSpace(packingSearchText)||GameCatalog.Items[x.templateId].name.IndexOf(packingSearchText.Trim(),System.StringComparison.OrdinalIgnoreCase)>=0);
  entries=packingSortByType?entries.OrderBy(x=>GameCatalog.Items[x.templateId].type).ThenBy(x=>GameCatalog.Items[x.templateId].name):entries.OrderBy(x=>GameCatalog.Items[x.templateId].name);
  var visible=entries.ToList();
  foreach(var item in visible){
   StorageFormulaSystem.EnsureItemRolled(item);
   var def=GameCatalog.Items[item.templateId];
   var tile=new VisualElement{focusable=true,tooltip=def.name,userData=item.uid};
   tile.AddToClassList("ps-rite-equip-tile");
   tile.EnableInClassList("ps-selected",item.uid==selectedPackingUid);
   tile.EnableInClassList("ps-rite-equip-placed",placed.Contains(item.uid));
   tile.Add(VaultItemDisplayArt(item.templateId,"ps-rite-equip-art"));
   var name=new Label(def.name){pickingMode=PickingMode.Ignore};
   name.AddToClassList("ps-rite-equip-name");tile.Add(name);
   var shape=Container("ps-packing-inventory-shape");
   shape.Add(BuildShapePreview(item,0));tile.Add(shape);
   if(placed.Contains(item.uid)){var badge=new Label("配置"){pickingMode=PickingMode.Ignore};badge.AddToClassList("ps-rite-equip-badge");tile.Add(badge);}
   tile.Query<VisualElement>().ForEach(x=>x.pickingMode=PickingMode.Ignore);
   tile.pickingMode=PickingMode.Position;
   BindPackingDragSource(tile,item.uid,formula,true);
   grid.Add(tile);
  }
  if(visible.Count==0)grid.Add(RiteEmptyNote(run.inventory.Count==0?"所持品はありません":"該当する装備はありません"));
  RequireViewElement<Label>(packingRootElement,"packing-inventory-count").text=$"{visible.Count} / {run.inventory.Count} 件";
  RequireViewElement<Button>(packingRootElement,"packing-sort").text=packingSortByType?"種別順":"名前順";
  foreach(var category in new[]{"all","weapon","armor","rune","supply"})
   RequireViewElement<Button>(packingRootElement,"packing-filter-"+category).EnableInClassList("ps-selected",packingEquipFilter==(category=="all"?"":category));
 }
 void CapturePackingScroll(){
  if(packingEquipScrollElement!=null)packingEquipScrollY=packingEquipScrollElement.scrollOffset.y;
  if(packingRightScrollElement!=null)packingRightScrollY=packingRightScrollElement.scrollOffset.y;
  if(packingLinksScrollElement!=null)packingLinksScrollY=packingLinksScrollElement.scrollOffset.y;
  if(packingColorsScrollElement!=null)packingColorsScrollY=packingColorsScrollElement.scrollOffset.y;
  if(packingBoardScrollElement!=null)packingBoardScrollOffset=packingBoardScrollElement.scrollOffset;
 }
 void RestorePackingScroll(ScrollView left,ScrollView right){
  RestorePackingScrollAt(left,new Vector2(0,packingEquipScrollY));
  RestorePackingScrollAt(right,new Vector2(0,packingRightScrollY));
  RestorePackingScrollAt(packingLinksScrollElement,new Vector2(0,packingLinksScrollY));
  RestorePackingScrollAt(packingColorsScrollElement,new Vector2(0,packingColorsScrollY));
  RestorePackingScrollAt(packingBoardScrollElement,packingBoardScrollOffset);
 }
 static void RestorePackingScrollAt(ScrollView scroll,Vector2 offset){
  scroll?.schedule.Execute(()=>{if(scroll.panel!=null)scroll.scrollOffset=offset;});
 }
}
}
