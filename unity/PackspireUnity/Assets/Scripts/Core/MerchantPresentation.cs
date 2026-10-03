namespace Packspire {
/// <summary>Display-only merchant presentation. Does not invent shop rules.</summary>
public sealed class MerchantPresentation {
 public string id="traveler";
 public string displayName="行商人";
 public string characterResource="";
 public string backdropResource="Art/UI/PopDark/hub-bg-v1";
 public string counterResource="";
 public string idleLine="品書きを見てくれ。いいものだけ持ってきた。";
 public string soldOutLine="今は出せる品がない。";
 public string purchaseLine="よし、取引成立だ。";
 public float characterScale=1.35f;
 public float characterOffsetX;
 public float characterOffsetY=18f;
 public float characterViewportHeight=.7f;
 public string affinityNote="";
 public string factionId="";
 public string bargainLine="";
 public string rumorLine="";
 public string specialDealLine="";
}

public static class MerchantCatalog {
 public static MerchantPresentation Default=>Resolve("default");
 public static MerchantPresentation Dungeon=>Resolve("dungeon");
 public static MerchantPresentation Faction=>Resolve("faction");

 public static MerchantPresentation Resolve(string contextId){
  MerchantContent value=null;
  foreach(var candidate in PackspireContent.Data.merchants)
   if(candidate.contextId==contextId){value=candidate;break;}
  if(value==null)
   foreach(var candidate in PackspireContent.Data.merchants)
    if(candidate.id=="traveler"){value=candidate;break;}
  return value==null?new MerchantPresentation():FromContent(value);
 }

 static MerchantPresentation FromContent(MerchantContent value)=>new(){
  id=value.id,
  displayName=value.displayName,
  characterResource=value.legacyCharacterResource,
  backdropResource=value.legacyBackdropResource,
  counterResource=value.legacyCounterResource,
  idleLine=value.idleLine,
  soldOutLine=value.soldOutLine,
  purchaseLine=value.purchaseLine,
  characterScale=value.characterScale,
  characterOffsetX=value.characterOffsetX,
  characterOffsetY=value.characterOffsetY,
  characterViewportHeight=value.characterViewportHeight,
  affinityNote=value.affinityNote,
  factionId=value.factionId,
  bargainLine=value.bargainLine,
  rumorLine=value.rumorLine,
  specialDealLine=value.specialDealLine
 };
}
}
