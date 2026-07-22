namespace Packspire {
public enum HubFacilityKind { Scene, Archive, Workbench }

public readonly struct HubFacilityDef {
 public readonly string id;
 public readonly string eyebrow;
 public readonly string label;
 public readonly ScreenId screen;
 public readonly HubFacilityKind kind;
 public readonly bool hubCard;
 public readonly string description;
 public readonly string iconResource;
 public readonly string themeKey;
 public readonly float mapX;
 public readonly float mapY;
 public readonly bool unlocked;
 public readonly string seal;

 public HubFacilityDef(
  string id,string eyebrow,string label,ScreenId screen,HubFacilityKind kind,bool hubCard=false,
  string description="",string iconResource="",string themeKey="default",float mapX=0.5f,float mapY=0.5f,bool unlocked=true,string seal=""){
  this.id=id;this.eyebrow=eyebrow;this.label=label;this.screen=screen;this.kind=kind;this.hubCard=hubCard;
  this.description=description;this.iconResource=iconResource;this.themeKey=themeKey;
  this.mapX=mapX;this.mapY=mapY;this.unlocked=unlocked;
  this.seal=string.IsNullOrEmpty(seal)?(eyebrow.Length>0?eyebrow.Substring(0,1):"・"):seal;
 }

 public string CategoryLabel=>kind switch{
  HubFacilityKind.Scene=>"行動",
  HubFacilityKind.Workbench=>"工房",
  _=>"記録"
 };
}

public static class HubFacilityCatalog {
 public static readonly HubFacilityDef[] All={
  new("gate","STRATA","遠征",ScreenId.Expedition,HubFacilityKind.Scene,true,
   "塔の未踏層へ向かう。","Art/UI/PopDark/btn-card-v1","strata",0.74f,0.32f,true,"遠"),
  new("forge","PACK","荷造り",ScreenId.Pack,HubFacilityKind.Workbench,true,
   "装備と術式を整える。","Art/UI/PopDark/btn-card-v1","forge",0.42f,0.48f,true,"荷"),
  new("vault","VAULT","保管庫",ScreenId.Vault,HubFacilityKind.Archive,true,
   "持ち帰った装備を確認する。","Art/UI/PopDark/btn-card-v1","vault",0.28f,0.56f,true,"庫"),
  new("heirloom","HEIRLOOM","家宝",ScreenId.Heirloom,HubFacilityKind.Archive,true,
   "一つの装備を長期育成する。","Art/UI/PopDark/btn-card-v1","heirloom",0.22f,0.42f,true,"宝"),
  new("guild","RANK","ステータス",ScreenId.Status,HubFacilityKind.Archive,true,
   "習得した役職と遠征者の記録。","Art/UI/PopDark/btn-card-v1","guild",0.36f,0.38f,true,"職"),
  new("codex","INDEX","図鑑",ScreenId.Compendium,HubFacilityKind.Archive,true,
   "遭遇記録と未解明の索引。","Art/UI/PopDark/btn-card-v1","codex",0.56f,0.62f,true,"鑑"),
  new("embassy","EMBASSY","勢力",ScreenId.Faction,HubFacilityKind.Scene,true,
   "勢力との関係と所属を確認する。","Art/UI/PopDark/btn-card-v1","embassy",0.62f,0.44f,true,"勢"),
  new("shop","SHOP","商店",ScreenId.Shop,HubFacilityKind.Workbench,true,
   "拠点の商人を訪ねる。","Art/UI/PopDark/btn-card-v1","guild",0.58f,0.52f,true,"商"),
  new("barracks","ROSTER","キャラクター",ScreenId.Character,HubFacilityKind.Archive,true,
   "遠征者の選択と確認。","Art/UI/PopDark/btn-card-v1","roster",0.48f,0.28f,true,"者"),
 };

 public static HubFacilityDef[] HubCards(){
  int n=0;
  for(int i=0;i<All.Length;i++)if(All[i].hubCard)n++;
  var list=new HubFacilityDef[n];
  int w=0;
  for(int i=0;i<All.Length;i++)if(All[i].hubCard)list[w++]=All[i];
  return list;
 }

 /// <summary>Left hub navigation: data-driven facility shortcuts.</summary>
 public static HubFacilityDef[] NavFacilities()=>UnlockedFacilities();

 /// <summary>Legacy reel alias — same as hub cards for dormant street-guide helpers.</summary>
 public static HubFacilityDef[] ReelFacilities()=>HubCards();

 public static HubFacilityDef[] UnlockedFacilities(){
  int n=0;
  for(int i=0;i<All.Length;i++)if(All[i].unlocked)n++;
  var list=new HubFacilityDef[n];
  int w=0;
  for(int i=0;i<All.Length;i++)if(All[i].unlocked)list[w++]=All[i];
  return list;
 }

 public static HubFacilityDef GetReel(int index){
  var reel=ReelFacilities();
  if(reel.Length==0)return All[0];
  if(index<0||index>=reel.Length)return reel[0];
  return reel[index];
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
