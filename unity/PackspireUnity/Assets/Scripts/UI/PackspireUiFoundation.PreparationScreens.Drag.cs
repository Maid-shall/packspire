using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire {
public sealed partial class PackspireUiFoundation {
 int packingPreviewAnchor=int.MinValue;
// Packing pointer, drag/drop, hit testing, and ghost state.
 void RegisterPackingDrag(VisualElement root){
  root.RegisterCallback<PointerMoveEvent>(OnPackingPointerMove);
  root.RegisterCallback<PointerUpEvent>(OnPackingPointerUp);
  root.RegisterCallback<PointerDownEvent>(evt=>{
   if(evt.button!=0||packingDragging||packingFormulaOpen||packingCardsOpen||packingFormulaBrowserOpen||string.IsNullOrEmpty(selectedPackingUid))return;
   for(var target=evt.target as VisualElement;target!=null&&target!=root;target=target.parent){
    if(target is Button||target is Scroller||target is TextField||target is Toggle||target.ClassListContains("ps-packing-preserve-selection")||target.ClassListContains("ps-rite-cell")||target.ClassListContains("ps-rite-equip-tile"))return;
   }
   selectedPackingUid="";
   packingRotation=0;
   BuildPackingAgain();
  });
 }

 void BindPackingDragSource(VisualElement element,string uid,ActiveStorageFormula formula,bool fromEquipList=false,Vector2Int grip=default){
  element.RegisterCallback<PointerDownEvent>(evt=>{
   if(evt.button!=0||packingFormulaOpen||packingCardsOpen||packingFormulaBrowserOpen||packingRootElement==null)return;
   packingDragUid=uid;
   packingDragging=false;
   packingDragFromList=fromEquipList;
   packingDragGrip=grip;
   packingDragStart=evt.position;
   packingTapWasSelected=selectedPackingUid==uid;
   packingRotation=StorageFormulaSystem.ClampRotation(formula.core.rotation,game.UiRun?.placements.FirstOrDefault(x=>x.itemUid==uid)?.rotation??packingRotation);
   if(!fromEquipList){
    selectedPackingUid=uid;
    packingRootElement.CapturePointer(evt.pointerId);
    evt.StopPropagation();
   }
  });
 }

 void OnPackingPointerMove(PointerMoveEvent evt){
  if(string.IsNullOrEmpty(packingDragUid)||packingRootElement==null)return;
  if(!packingDragging){
   Vector2 delta=(Vector2)evt.position-packingDragStart;
   if(delta.magnitude<10f)return;
   if(packingDragFromList&&Mathf.Abs(delta.y)>Mathf.Abs(delta.x)*1.15f){
    packingDragUid="";
    packingDragging=false;
    packingDragFromList=false;
    packingDragGrip=Vector2Int.zero;
    return;
   }
   packingDragging=true;
   selectedPackingUid=packingDragUid;
   if(!packingRootElement.HasPointerCapture(evt.pointerId))
    packingRootElement.CapturePointer(evt.pointerId);
   RefreshPackingDragSelection();
   EnsurePackingGhost();
  }
  if(packingDragGhost==null)return;
  var local=packingRootElement.WorldToLocal(evt.position);
  float side=PackingCellSide();
  packingDragGhost.style.left=local.x-(packingDragGrip.x+.5f)*side;
  packingDragGhost.style.top=local.y-(packingDragGrip.y+.5f)*side;
  UpdatePackingDragPreview(evt.position);
 }

 void OnPackingPointerUp(PointerUpEvent evt){
  if(string.IsNullOrEmpty(packingDragUid))return;
  if(packingRootElement!=null&&packingRootElement.HasPointerCapture(evt.pointerId))
   packingRootElement.ReleasePointer(evt.pointerId);
  if(!packingDragging){
   string uid=packingDragUid;
   packingDragUid="";
   packingDragFromList=false;
   packingDragGrip=Vector2Int.zero;
   if(packingTapWasSelected){selectedPackingUid="";packingRotation=0;}
   else selectedPackingUid=uid;
   BuildPackingAgain();
   return;
  }
  EndPackingDrag(true,evt.position);
 }


 void EndPackingDrag(bool applyDrop,Vector2 panelPosition=default){
  string uid=packingDragUid;
  bool wasDragging=packingDragging;
  Vector2Int grip=packingDragGrip;
  packingDragUid="";
  packingDragging=false;
  packingDragFromList=false;
  packingDragGrip=Vector2Int.zero;
  packingPreviewAnchor=int.MinValue;
  packingRootElement?.EnableInClassList("packing--dragging",false);
  packingRootElement?.EnableInClassList("packing--drop-invalid",false);
  if(packingDragGhost!=null){
   packingDragGhost.RemoveFromHierarchy();
   packingDragGhost=null;
  }
  if(!applyDrop||!wasDragging||string.IsNullOrEmpty(uid)||game.UiRun==null){
   if(wasDragging)BuildPackingAgain();
   return;
  }

  int cell=FindPackingCellAt(panelPosition);
  if(cell>=0){
   int width=BackpackSystem.GridWidth(game.UiRun);
   int x=cell%width-grip.x,y=cell/width-grip.y;
   int anchor=y*width+x;
   if(x<0||y<0||!game.UiPackingPlace(uid,anchor,packingRotation))ShowToast("そこには置けません");
   selectedPackingUid=uid;
  } else if(RequireViewElement<VisualElement>(packingRootElement,"packing-left").worldBound.Contains(panelPosition)){
   game.UiPackingRemove(uid);
   selectedPackingUid="";
   ShowToast("装備欄へ戻した");
  } else {
   selectedPackingUid=uid;
   ShowToast("配置は変更していません");
  }
  BuildPackingAgain();
 }

 void EnsurePackingGhost(){
  if(packingRootElement==null||string.IsNullOrEmpty(packingDragUid)||game.UiRun==null)return;
  var item=game.UiRun.inventory.FirstOrDefault(x=>x.uid==packingDragUid);
  if(item==null)return;
  packingDragGhost=Container("ps-rite-drag-ghost");
  packingDragGhost.pickingMode=PickingMode.Ignore;
  var shape=BuildShapePreview(item,packingRotation);
  float side=PackingCellSide();
  foreach(var cell in shape.Query(className:"ps-rite-shape-cell").ToList()){
   cell.style.width=side;
   cell.style.height=side;
  }
  packingDragGhost.Add(shape);
  packingDragGhost.Query<VisualElement>().ForEach(element=>element.pickingMode=PickingMode.Ignore);
  packingRootElement.Add(packingDragGhost);
 }

 void UpdatePackingDragPreview(Vector2 position){
  int cell=FindPackingCellAt(position);
  int width=BackpackSystem.GridWidth(game.UiRun);
  int x=cell>=0?cell%width-packingDragGrip.x:-1,y=cell>=0?cell/width-packingDragGrip.y:-1;
  int anchor=x>=0&&y>=0?y*width+x:-1;
  if(anchor==packingPreviewAnchor)return;
  packingPreviewAnchor=anchor;
  var item=game.UiRun.inventory.First(i=>i.uid==packingDragUid);
  bool valid=anchor>=0&&BackpackSystem.CanPlace(game.UiRun,item,anchor,packingRotation,item.uid);
  packingRootElement.EnableInClassList("packing--dragging",true);
  packingRootElement.EnableInClassList("packing--drop-invalid",!valid);
  packingDragGhost.EnableInClassList("packing--drop-invalid",!valid);
  var label=RequireViewElement<Label>(packingRootElement,"packing-drag-preview");
  if(!valid){label.text=anchor>=0?"配置不能 — ほかの装備・盤の端を確認":"盤の外 — 一覧へ戻すか、盤へドラッグ";return;}
  // Calculation previews must never write to the player's placements or selected slots.
  var preview=JsonUtility.FromJson<RunState>(JsonUtility.ToJson(game.UiRun));
  var before=BackpackSystem.Build(preview);
  var beforeKeys=PackingEffectRows(preview,before).Select(PackingEffectKey).ToHashSet();
  preview.placements.RemoveAll(p=>p.itemUid==item.uid);
  preview.placements.Add(new Placement(item.uid,anchor,packingRotation));
  var after=BackpackSystem.Build(preview);
  var afterKeys=PackingEffectRows(preview,after).Select(PackingEffectKey).ToHashSet();
  int added=afterKeys.Except(beforeKeys).Count(),lost=beforeKeys.Except(afterKeys).Count();
  int colorDelta=after.colors.Values.Sum()-before.colors.Values.Sum();
  label.text=$"配置可 ／ LINK・札変化 +{added} / −{lost} ／ 色一致 {(colorDelta>=0?"+":"")}{colorDelta}";
 }
 static string PackingEffectKey(PackingEffectRow row)=>row.kind+":"+string.Join(",",row.uids.OrderBy(x=>x))+":"+row.detail;

 void RefreshPackingDragSelection(){
  CapturePackingScroll();
  packingRightScrollY=0;
  packingPresentedSelectionUid=selectedPackingUid;
  var snapshot=JsonUtility.FromJson<RunState>(JsonUtility.ToJson(game.UiRun));
  RefreshPackingEffects(game.UiRun,BackpackSystem.Build(snapshot));
  foreach(var tile in RequireViewElement<VisualElement>(packingRootElement,"packing-equip-grid").Children())
   tile.EnableInClassList("ps-selected",tile.userData is string uid&&uid==selectedPackingUid);
  foreach(var child in packingGridElement.Children()){
   if(child.userData is not int index)continue;
   child.EnableInClassList("ps-related",false);
   child.EnableInClassList("ps-selected",PlacementAt(game.UiRun,index)?.itemUid==selectedPackingUid);
  }
  RestorePackingScroll(packingEquipScrollElement,packingRightScrollElement);
 }

 float PackingCellSide()=>packingGridElement!=null&&packingGridElement.childCount>0?packingGridElement[0].resolvedStyle.width:1f;

 int FindPackingCellAt(Vector2 panelPosition){
  if(packingGridElement==null)return -1;
  if(packingBoardScrollElement==null||!packingBoardScrollElement.contentViewport.worldBound.Contains(panelPosition))return -1;
  if(packingRootElement.ClassListContains("packing--context-peek")&&RequireViewElement<VisualElement>(packingRootElement,"packing-right-shell").worldBound.Contains(panelPosition))return -1;
  if(!packingGridElement.worldBound.Contains(panelPosition))return -1;
  int best=-1;
  float bestDist=float.MaxValue;
  foreach(var child in packingGridElement.Children()){
   if(child.userData is not int index)continue;
   if(child.worldBound.Contains(panelPosition))return index;
   Vector2 center=child.worldBound.center;
   float dist=(center-panelPosition).sqrMagnitude;
   if(dist<bestDist){bestDist=dist;best=index;}
  }
  return best;
 }
}
}
