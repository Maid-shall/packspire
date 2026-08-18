using System.Linq;
using UnityEngine;

namespace Packspire {
public enum HubFacilityKind { Scene, Archive, Workbench }

public readonly struct HubFacilityDef {
 public readonly string id,eyebrow,label,description,iconResource,themeKey,seal;
 public readonly ScreenId screen;
 public readonly HubFacilityKind kind;
 public readonly bool hubCard,unlocked;
 public readonly Sprite iconAsset;
 public readonly float mapX,mapY;

 public HubFacilityDef(FacilityContent value){
  id=value.id;eyebrow=value.eyebrow;label=value.label;screen=value.screen;kind=value.kind;hubCard=value.hubCard;
  description=value.description;iconAsset=value.icon;iconResource=value.legacyIconResource;themeKey=value.themeKey;
  mapX=value.mapX;mapY=value.mapY;unlocked=value.unlocked;
  seal=string.IsNullOrEmpty(value.seal)?(!string.IsNullOrEmpty(value.eyebrow)?value.eyebrow.Substring(0,1):"・"):value.seal;
 }

 public string CategoryLabel=>kind switch{
  HubFacilityKind.Scene=>"行動",
  HubFacilityKind.Workbench=>"工房",
  _=>"記録"
 };
}

public static class HubFacilityCatalog {
 public static readonly HubFacilityDef[] All=PackspireContent.Data.facilities.Select(x=>new HubFacilityDef(x)).ToArray();

 public static HubFacilityDef[] HubCards()=>All.Where(x=>x.hubCard).ToArray();
 public static HubFacilityDef[] NavFacilities()=>UnlockedFacilities();
 public static HubFacilityDef[] ReelFacilities()=>HubCards();
 public static HubFacilityDef[] UnlockedFacilities()=>All.Where(x=>x.unlocked).ToArray();

 public static HubFacilityDef GetReel(int index){
  var reel=ReelFacilities();
  if(reel.Length==0)return All[0];
  return index>=0&&index<reel.Length?reel[index]:reel[0];
 }

 public static int IndexInReel(string id){
  var reel=ReelFacilities();
  for(int i=0;i<reel.Length;i++)if(reel[i].id==id)return i;
  return -1;
 }

 public static HubFacilityDef Find(string id){
  for(int i=0;i<All.Length;i++)if(All[i].id==id)return All[i];
  return All[0];
 }
}
}
