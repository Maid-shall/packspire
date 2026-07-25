using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire {
public sealed partial class PackspireUiFoundation {
// Packing screen refresh, state preservation, and scroll restoration.
 void BuildPackingAgain(){
  if(!PackingScreenAlive()){
   RebuildScreen(BuildPacking);
   return;
  }
  RefreshPackingContent();
 }

 bool PackingScreenAlive(){
  return packingRootElement!=null&&packingRootElement.panel!=null&&
   packingFilterRowElement!=null&&packingEquipScrollElement!=null&&
   packingKilnElement!=null&&packingKilnRailElement!=null&&packingRightScrollElement!=null;
 }

 void RefreshPackingContent(){
  CapturePackingScroll();

  var stableRoot=packingRootElement;
  var stableFilter=packingFilterRowElement;
  var stableEquip=packingEquipScrollElement;
  var stableKiln=packingKilnElement;
  var stableRail=packingKilnRailElement;
  var stableRight=packingRightScrollElement;
  var stablePopup=packingPopupElement;

  // Build the next state synchronously, then transplant only the mutable contents.
  // The screen shell and its layout stay alive, so selection changes do not flash or
  // reset the fixed packing layout. Card/formula popups intentionally remain swappable.
  BuildPacking();
  var stagedRoot=packingRootElement;
  var stagedFilter=packingFilterRowElement;
  var stagedEquip=packingEquipScrollElement;
  var stagedKiln=packingKilnElement;
  var stagedRail=packingKilnRailElement;
  var stagedRight=packingRightScrollElement;
  var stagedPopup=packingPopupElement;

  MovePackingChildren(stagedFilter,stableFilter);
  MovePackingChildren(stagedEquip,stableEquip);
  MovePackingChildren(stagedKiln,stableKiln);
  MovePackingChildren(stagedRail,stableRail);
  MovePackingChildren(stagedRight,stableRight);

  stablePopup?.RemoveFromHierarchy();
  if(stagedPopup!=null){
   stagedPopup.RemoveFromHierarchy();
   stableRoot.Add(stagedPopup);
  }
  stagedRoot?.RemoveFromHierarchy();

  packingRootElement=stableRoot;
  packingFilterRowElement=stableFilter;
  packingEquipScrollElement=stableEquip;
  packingKilnElement=stableKiln;
  packingKilnRailElement=stableRail;
  packingRightScrollElement=stableRight;
  packingPopupElement=stagedPopup;
  RestorePackingScroll(stableEquip,stableRight);
 }

 static void MovePackingChildren(VisualElement source,VisualElement destination){
  if(source==null||destination==null)return;
  destination.Clear();
  while(source.childCount>0){
   var child=source[0];
   child.RemoveFromHierarchy();
   destination.Add(child);
  }
 }

 void CapturePackingScroll(){
  if(screenRoot==null)return;
  var left=packingEquipScrollElement??screenRoot.Q<ScrollView>(className:"ps-rite-equip-scroll");
  if(left!=null)packingEquipScrollY=left.scrollOffset.y;
  var right=packingRightScrollElement??screenRoot.Q<ScrollView>(className:"ps-rite-right");
  if(right!=null)packingRightScrollY=right.scrollOffset.y;
 }

 void RestorePackingScroll(ScrollView left,ScrollView right){
  float leftY=packingEquipScrollY;
  float rightY=packingRightScrollY;
  if(left!=null){
   left.schedule.Execute(()=>{
    if(left!=null)left.scrollOffset=new Vector2(0,leftY);
   }).ExecuteLater(0);
  }
  if(right!=null){
   right.schedule.Execute(()=>{
    if(right!=null)right.scrollOffset=new Vector2(0,rightY);
   }).ExecuteLater(0);
  }
 }
}
}
