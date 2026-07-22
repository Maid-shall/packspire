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

 public HubFacilityDef(
  string id,string eyebrow,string label,ScreenId screen,HubFacilityKind kind,bool hubCard=false,
  string description="",string iconResource="",string themeKey="default",float mapX=0.5f,float mapY=0.5f,bool unlocked=true){
  this.id=id;this.eyebrow=eyebrow;this.label=label;this.screen=screen;this.kind=kind;this.hubCard=hubCard;
  this.description=description;this.iconResource=iconResource;this.themeKey=themeKey;
  this.mapX=mapX;this.mapY=mapY;this.unlocked=unlocked;
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
   "塔の未踏層へ向かう。","Art/UI/PopDark/btn-card-v1","strata",0.74f,0.32f),
  new("forge","PACK","荷造り",ScreenId.Pack,HubFacilityKind.Workbench,true,
   "装備と術式を整える。","Art/UI/PopDark/btn-card-v1","forge",0.42f,0.48f),
  new("vault","VAULT","保管",ScreenId.Vault,HubFacilityKind.Archive,true,
   "持ち帰った装備を確認する。","Art/UI/PopDark/btn-card-v1","vault",0.28f,0.56f),
  new("codex","INDEX","図鑑",ScreenId.Compendium,HubFacilityKind.Archive,true,
   "遭遇記録と未解明の索引。","Art/UI/PopDark/btn-card-v1","codex",0.56f,0.62f),
  new("guild","RANK","ステータス",ScreenId.Status,HubFacilityKind.Archive,true,
   "習得した役職と遠征者の記録。","Art/UI/PopDark/btn-card-v1","guild",0.36f,0.38f),
  new("embassy","EMBASSY","勢力",ScreenId.Faction,HubFacilityKind.Scene,false,
   "勢力との関係と所属を確認する。","Art/UI/PopDark/btn-card-v1","embassy",0.62f,0.44f),
  new("barracks","ROSTER","キャラ",ScreenId.Character,HubFacilityKind.Archive,false,
   "遠征者の選択と確認。","Art/UI/PopDark/btn-card-v1","roster",0.48f,0.28f),
 };

 public static HubFacilityDef[] HubCards(){
  int n=0;
  for(int i=0;i<All.Length;i++)if(All[i].hubCard)n++;
  var list=new HubFacilityDef[n];
  int w=0;
  for(int i=0;i<All.Length;i++)if(All[i].hubCard)list[w++]=All[i];
  return list;
 }

 /// <summary>Left reel: primary shortcuts only (favorites/recents hook).</summary>
 public static HubFacilityDef[] ReelFacilities()=>HubCards();

 public static HubFacilityDef[] UnlockedFacilities(){
  int n=0;
  for(int i=0;i<All.Length;i++)if(All[i].unlocked)n++;
  var list=new HubFacilityDef[n];
  int w=0;
  for(int i=0;i<All.Length;i++)if(All[i].unlocked)list[w++]=All[i];
  return list;
 }

 public static int IndexOfId(string id){
  for(int i=0;i<All.Length;i++)if(All[i].id==id)return i;
  return 0;
 }

 public static int IndexOfScreen(ScreenId screen){
  for(int i=0;i<All.Length;i++)if(All[i].screen==screen)return i;
  return 0;
 }

 public static HubFacilityDef Get(int index){
  if(index<0||index>=All.Length)return All[0];
  return All[index];
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
}
}
