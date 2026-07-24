using System;
using System.Collections.Generic;
using UnityEngine;

namespace Packspire {
[Serializable]
public class CharacterDef {
 public string id,name,title,description;
 public int portraitBody,portraitHair;
 /// <summary>Battle/action portrait Resources path (no extension).</summary>
 public string portraitResource;
 /// <summary>Front-facing portrait for roster/select screens.</summary>
 public string portraitFrontResource;
 /// <summary>Side/profile portrait for hub display.</summary>
 public string portraitHubResource;
 public string traitName,traitText;
 public string traitKind;
 public int traitValue;
 public string activeSkillId,activeSkillName,activeSkillText;
 /// <summary>Legacy UV crop (unused by focus banner). Kept for data compatibility.</summary>
 public Rect expeditionBannerUv;
 /// <summary>Banner focus in image space, top-left origin (0-1). Eyes should sit near this point.</summary>
 public float portraitFocusX=0.5f;
 public float portraitFocusY=0.28f;
 /// <summary>How much larger the art image is than the banner clip.</summary>
 public float portraitZoom=2.2f;
 public float portraitBannerOffsetX;
 public float portraitBannerOffsetY;
 public CharacterDef(string id,string name,string title,string description,int portraitBody,int portraitHair,string traitName,string traitText,string traitKind,int traitValue,string activeSkillId,string activeSkillName,string activeSkillText,string portraitResource="",string portraitFrontResource="",string portraitHubResource=""){
  this.id=id;this.name=name;this.title=title;this.description=description;
  this.portraitBody=portraitBody;this.portraitHair=portraitHair;this.portraitResource=portraitResource;
  this.portraitFrontResource=portraitFrontResource;this.portraitHubResource=portraitHubResource;
  this.traitName=traitName;this.traitText=traitText;this.traitKind=traitKind;this.traitValue=traitValue;
  this.activeSkillId=activeSkillId;this.activeSkillName=activeSkillName;this.activeSkillText=activeSkillText;
 }
 public bool HasPortraitAsset=>!string.IsNullOrEmpty(portraitResource);
 public bool HasFrontPortraitAsset=>!string.IsNullOrEmpty(portraitFrontResource)||HasPortraitAsset;
 public bool HasHubPortraitAsset=>!string.IsNullOrEmpty(portraitHubResource);
}

public static class CharacterCatalog {
 public const string DefaultId="ren";
 public static readonly Dictionary<string,CharacterDef> All=new(){
  ["ren"]=new CharacterDef(
   "ren","蓮","鉄壁の剣士","前線で敵の刃を受け止める遠征者。",
   0,0,"不屈","最大HP+4","maxHpBonus",4,
   "ren_rush","突進","敵に10ダメージ（1戦1回）"),
  ["mio"]=new CharacterDef(
   "mio","澪","影走りの斥候","隙を見逃さない探索の専門家。",
   1,1,"先読み","戦闘開始時に1枚追加ドロー","openingDraw",1,
   "mio_read","見切り","8ブロック＋1枚ドロー（1戦1回）"),
  ["kuro"]=new CharacterDef(
   "kuro","玄","城壁の守人","最初の一撃を凌ぐ重装の護衛。",
   2,0,"堅阵","戦闘開始時に4ブロック","openingBlock",4,
   "kuro_bulwark","鉄壁","14ブロック（1戦1回）"),
  ["hina"]=new CharacterDef(
   "hina","陽菜","錬装の技師","装備と道具の扱いに長けた工匠。",
   3,2,"整備","戦闘勝利時の所持金+3","winGold",3,
   "hina_repair","応急修理","HPを10回復（1戦1回）"),
  ["sena"]=new CharacterDef(
   "sena","瀬名","灼脚の闘士","褐色の肌に白髪をなびかせ、蹴りで戦場を割る筋肉質の遠征者。",
   0,0,"灼脚","最大HP+2","maxHpBonus",2,
   "sena_kick","廻旋脚","敵に14ダメージ（1戦1回）",
   "Art/Portraits/hero-sena-kick-v1"),
 };
 static CharacterCatalog(){
  // Top-left focus for banner (measured against hero-sena-kick-v1 eye position).
  SetPortraitFocus("sena",0.39f,0.22f,2.65f,0f,0.02f);
  SetPortraitFocus("ren",0.50f,0.26f,2.2f);
  SetPortraitFocus("mio",0.50f,0.26f,2.2f);
  SetPortraitFocus("kuro",0.50f,0.26f,2.2f);
  SetPortraitFocus("hina",0.50f,0.26f,2.2f);
 }
 static void SetPortraitFocus(string id,float focusX,float focusY,float zoom,float offsetX=0f,float offsetY=0f){
  if(!All.TryGetValue(id,out var def))return;
  def.portraitFocusX=Mathf.Clamp01(focusX);
  def.portraitFocusY=Mathf.Clamp01(focusY);
  def.portraitZoom=Mathf.Max(1.1f,zoom);
  def.portraitBannerOffsetX=offsetX;
  def.portraitBannerOffsetY=offsetY;
 }

 public static CharacterDef Get(string id){
  if(!string.IsNullOrEmpty(id)&&All.TryGetValue(id,out var def))return def;
  return All[DefaultId];
 }

 public static IEnumerable<CharacterDef> Roster=>All.Values;
}
}
