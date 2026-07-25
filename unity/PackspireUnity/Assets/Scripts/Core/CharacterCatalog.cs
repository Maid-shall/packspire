using System;
using System.Collections.Generic;
using UnityEngine;

namespace Packspire {
[Serializable]
public class CharacterDef {
 public string id,name,title,description;
 public int portraitBody,portraitHair;
 public Sprite portraitAsset,portraitFrontAsset,portraitHubAsset;
 public string portraitResource,portraitFrontResource,portraitHubResource;
 public string traitName,traitText,traitKind;
 public int traitValue;
 public string activeSkillId,activeSkillName,activeSkillText;
 public CharacterSkillKind activeSkillKind;
 public int activeSkillAmount,activeSkillSecondaryAmount;
 public Rect expeditionBannerUv;
 public float portraitFocusX=.5f,portraitFocusY=.28f,portraitZoom=2.2f;
 public float portraitBannerOffsetX,portraitBannerOffsetY;

 public bool HasPortraitAsset=>portraitAsset!=null||!string.IsNullOrEmpty(portraitResource);
 public bool HasFrontPortraitAsset=>portraitFrontAsset!=null||!string.IsNullOrEmpty(portraitFrontResource)||HasPortraitAsset;
 public bool HasHubPortraitAsset=>portraitHubAsset!=null||!string.IsNullOrEmpty(portraitHubResource);
}

public static class CharacterCatalog {
 public const string DefaultId="ren";
 public static readonly Dictionary<string,CharacterDef> All=Build();

 static Dictionary<string,CharacterDef> Build(){
  var result=new Dictionary<string,CharacterDef>();
  foreach(var value in PackspireContent.Data.characters){
   result[value.id]=new CharacterDef{
    id=value.id,name=value.name,title=value.title,description=value.description,
    portraitBody=value.portraitBody,portraitHair=value.portraitHair,
    portraitAsset=value.portrait,portraitFrontAsset=value.portraitFront,portraitHubAsset=value.portraitHub,
    portraitResource=value.legacyPortraitResource,
    portraitFrontResource=value.legacyPortraitFrontResource,
    portraitHubResource=value.legacyPortraitHubResource,
    traitName=value.traitName,traitText=value.traitText,traitKind=value.traitKind,traitValue=value.traitValue,
    activeSkillId=value.activeSkillId,activeSkillName=value.activeSkillName,activeSkillText=value.activeSkillText,
    activeSkillKind=value.activeSkillKind,
    activeSkillAmount=value.activeSkillAmount,
    activeSkillSecondaryAmount=value.activeSkillSecondaryAmount,
    portraitFocusX=Mathf.Clamp01(value.portraitFocusX),
    portraitFocusY=Mathf.Clamp01(value.portraitFocusY),
    portraitZoom=Mathf.Max(1.1f,value.portraitZoom),
    portraitBannerOffsetX=value.portraitBannerOffsetX,
    portraitBannerOffsetY=value.portraitBannerOffsetY
   };
  }
  return result;
 }

 public static CharacterDef Get(string id){
  if(!string.IsNullOrEmpty(id)&&All.TryGetValue(id,out var def))return def;
  return All[DefaultId];
 }

 public static IEnumerable<CharacterDef> Roster=>All.Values;
}
}
