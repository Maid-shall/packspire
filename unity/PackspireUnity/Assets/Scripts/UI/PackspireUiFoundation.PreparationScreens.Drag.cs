using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire {
public sealed partial class PackspireUiFoundation {
// Packing pointer, drag/drop, hit testing, and ghost state.
 void RegisterPackingDrag(VisualElement root){
  root.RegisterCallback<PointerMoveEvent>(OnPackingPointerMove);
  root.RegisterCallback<PointerUpEvent>(OnPackingPointerUp);
 }

 void BindPackingDragSource(VisualElement element,string uid,ActiveStorageFormula formula,bool fromEquipList=false,Vector2Int grip=default){
  element.RegisterCallback<PointerDownEvent>(evt=>{
   if(evt.button!=0||packingFormulaOpen||packingCardsOpen||packingRootElement==null)return;
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
   EnsurePackingGhost();
  }
  if(packingDragGhost==null)return;
  var local=packingRootElement.WorldToLocal(evt.position);
  packingDragGhost.style.left=local.x-36;
  packingDragGhost.style.top=local.y-36;
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

 void OnPackingCircleClick(ClickEvent evt){
  if(packingFormulaOpen||packingCardsOpen||packingDragging||string.IsNullOrEmpty(selectedPackingUid))return;
  var target=evt.target as VisualElement;
  while(target!=null){
   if(target.ClassListContains("ps-rite-cell")||target.ClassListContains("ps-rite-grid"))return;
   if(target.ClassListContains("ps-rite-circle"))break;
   target=target.parent;
  }
  selectedPackingUid="";
  packingRotation=0;
  BuildPackingAgain();
 }

 void EndPackingDrag(bool applyDrop,Vector2 panelPosition=default){
  string uid=packingDragUid;
  bool wasDragging=packingDragging;
  Vector2Int grip=packingDragGrip;
  packingDragUid="";
  packingDragging=false;
  packingDragFromList=false;
  packingDragGrip=Vector2Int.zero;
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
   int anchor=cell-grip.x-grip.y*width;
   if(!game.UiPackingPlace(uid,anchor,packingRotation))ShowToast("そこには置けません");
   selectedPackingUid=uid;
  } else {
   game.UiPackingRemove(uid);
   selectedPackingUid="";
   ShowToast("装備欄へ戻した");
  }
  BuildPackingAgain();
 }

 void EnsurePackingGhost(){
  if(packingRootElement==null||string.IsNullOrEmpty(packingDragUid)||game.UiRun==null)return;
  var item=game.UiRun.inventory.FirstOrDefault(x=>x.uid==packingDragUid);
  if(item==null)return;
  packingDragGhost=Container("ps-rite-drag-ghost");
  packingDragGhost.pickingMode=PickingMode.Ignore;
  packingDragGhost.Add(Atlas(game.UiEquipmentArt,ItemUv(item.templateId),"ps-rite-drag-ghost-art"));
  packingRootElement.Add(packingDragGhost);
 }

 int FindPackingCellAt(Vector2 panelPosition){
  if(packingGridElement==null)return -1;
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
