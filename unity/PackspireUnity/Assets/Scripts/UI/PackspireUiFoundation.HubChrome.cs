using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire {
public enum HubChromeKind { Primary, Secondary, Facility, MapPin, Locked, Notice }

public sealed partial class PackspireUiFoundation {
 VisualElement BuildHubChromeButton(HubChromeKind kind,string name,string emblem,string title,string subtitle,Action onClick,bool selected=false,bool locked=false,bool notice=false){
  var button=new Button(onClick){name=name,userData=kind};
  button.AddToClassList("ps-hub-chrome");
  button.AddToClassList(HubChromeClass(kind));
  if(selected)button.AddToClassList("ps-selected");
  if(locked)button.AddToClassList("ps-locked");
  if(notice)button.AddToClassList("ps-notice");
  if(locked)button.SetEnabled(false);

  var frame=Container("ps-hub-chrome-frame");
  frame.pickingMode=PickingMode.Ignore;
  var glow=Container("ps-hub-chrome-glow");
  glow.pickingMode=PickingMode.Ignore;
  var face=Container("ps-hub-chrome-face");
  face.pickingMode=PickingMode.Ignore;
  var emblemBox=Container("ps-hub-chrome-emblem");
  emblemBox.pickingMode=PickingMode.Ignore;
  if(!string.IsNullOrEmpty(emblem)){
   var emblemLabel=new Label(emblem){pickingMode=PickingMode.Ignore};
   emblemLabel.AddToClassList("ps-hub-chrome-emblem-text");
   emblemBox.Add(emblemLabel);
  }
  var copy=Container("ps-hub-chrome-copy");
  copy.pickingMode=PickingMode.Ignore;
  if(!string.IsNullOrEmpty(title)){
   var titleLabel=new Label(title){pickingMode=PickingMode.Ignore};
   titleLabel.AddToClassList("ps-hub-chrome-title");
   copy.Add(titleLabel);
  }
  if(!string.IsNullOrEmpty(subtitle)){
   var subLabel=new Label(subtitle){pickingMode=PickingMode.Ignore};
   subLabel.AddToClassList("ps-hub-chrome-sub");
   copy.Add(subLabel);
  }
  face.Add(emblemBox);
  face.Add(copy);
  frame.Add(glow);
  frame.Add(face);
  button.Add(frame);
  return button;
 }

 static string HubChromeClass(HubChromeKind kind)=>kind switch{
  HubChromeKind.Primary=>"ps-hub-chrome-primary",
  HubChromeKind.Secondary=>"ps-hub-chrome-secondary",
  HubChromeKind.MapPin=>"ps-hub-chrome-mappin",
  HubChromeKind.Locked=>"ps-hub-chrome-locked",
  HubChromeKind.Notice=>"ps-hub-chrome-notice",
  _=>"ps-hub-chrome-facility"
 };

 VisualElement BuildHubChromeFacility(HubFacilityDef facility,int index,bool selected,Action onClick){
  var kind=facility.unlocked?HubChromeKind.Facility:HubChromeKind.Locked;
  return BuildHubChromeButton(kind,$"hub-reel-{index}",facility.eyebrow,facility.label,facility.CategoryLabel,onClick,selected,!facility.unlocked);
 }

 VisualElement BuildHubChromeMapPin(HubFacilityDef facility,bool selected,Action onClick){
  var kind=facility.unlocked?HubChromeKind.MapPin:HubChromeKind.Locked;
  return BuildHubChromeButton(kind,$"hub-map-{facility.id}",facility.eyebrow,facility.label,facility.CategoryLabel,onClick,selected,!facility.unlocked);
 }

 VisualElement BuildHubChromePrimary(string title,Action onClick)=>BuildHubChromeButton(HubChromeKind.Primary,"hub-primary","",title,"",onClick);
 VisualElement BuildHubChromeSecondary(string title,Action onClick)=>BuildHubChromeButton(HubChromeKind.Secondary,"hub-secondary","",title,"",onClick);
 VisualElement BuildHubChromeStreetGuide(Action onClick){
  var button=BuildHubChromeButton(HubChromeKind.Secondary,"hub-street-entry","MAP","街案内","塔内の施設",onClick);
  button.AddToClassList("ps-hub-street-entry");
  return button;
 }
}
}
