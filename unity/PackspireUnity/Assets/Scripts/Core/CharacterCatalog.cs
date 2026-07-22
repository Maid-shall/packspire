using System;
using System.Collections.Generic;

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

 public static CharacterDef Get(string id){
  if(!string.IsNullOrEmpty(id)&&All.TryGetValue(id,out var def))return def;
  return All[DefaultId];
 }

 public static IEnumerable<CharacterDef> Roster=>All.Values;
}
}
