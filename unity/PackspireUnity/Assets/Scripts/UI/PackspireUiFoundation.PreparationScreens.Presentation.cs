using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire {
public sealed partial class PackspireUiFoundation {
// Static elemental markers shared by the packing board and effects.
 Texture2D RiteOrbTexture(Element element){
  string key=element switch{
   Element.Fire=>"fire",
   Element.Water=>"water",
   Element.Wind=>"wind",
   Element.Earth=>"earth",
   _=>null,
  };
  if(key==null)return null;
  return PackspireResources.LoadFirst<Texture2D>(
   "Art/Rite/orb-"+key+"-v2",
   "Art/Rite/orb-"+key+"-v1");
 }

 VisualElement BuildRiteOrb(Element element,string extraClass=""){
  var wrap=Container("ps-rite-orb-wrap");
  wrap.pickingMode=PickingMode.Ignore;
  var halo=Container("ps-rite-orb-halo");
  halo.pickingMode=PickingMode.Ignore;
  halo.AddToClassList("ps-element-"+element.ToString().ToLowerInvariant());
  wrap.Add(halo);
  var tex=RiteOrbTexture(element);
  VisualElement orb;
  if(tex!=null){
   orb=new Image{image=tex,scaleMode=ScaleMode.ScaleToFit,pickingMode=PickingMode.Ignore};
  } else {
   orb=Container("ps-rite-orb-fallback");
   orb.pickingMode=PickingMode.Ignore;
   orb.AddToClassList("ps-element-"+element.ToString().ToLowerInvariant());
  }
  orb.AddToClassList("ps-rite-orb");
  orb.AddToClassList("ps-rite-orb-live");
  if(!string.IsNullOrEmpty(extraClass))orb.AddToClassList(extraClass);
  wrap.Add(orb);
  return wrap;
 }
}
}
