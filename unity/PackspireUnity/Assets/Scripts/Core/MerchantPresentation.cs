using UnityEngine;

namespace Packspire {
/// <summary>Display-only merchant presentation. Does not invent shop rules.</summary>
public sealed class MerchantPresentation {
 public string id="traveler";
 public string displayName="行商人";
 /// <summary>Optional Texture2D Resources path for character art. Empty = runtime fallback.</summary>
 public string characterResource="";
 public string backdropResource="Art/UI/PopDark/hub-bg-v1";
 /// <summary>Optional counter Texture2D path. Empty = styled procedural counter (no button art).</summary>
 public string counterResource="";
 public string idleLine="品書きを見てくれ。いいものだけ持ってきた。";
 public string soldOutLine="今は出せる品がない。";
 public string purchaseLine="よし、取引成立だ。";
 public float characterScale=1.35f;
 public float characterOffsetX=0f;
 public float characterOffsetY=18f;
 public float characterViewportHeight=0.70f;
 // Reserved for future systems (not shown until implemented):
 public string affinityNote="";
 public string factionId="";
 public string bargainLine="";
 public string rumorLine="";
 public string specialDealLine="";
}

public static class MerchantCatalog {
 public static readonly MerchantPresentation Default=new(){
  id="traveler",
  displayName="行商人",
  characterResource="Art/Portraits/PopDark/hero-courier-cutout-v1",
  backdropResource="Art/UI/PopDark/hub-bg-v1",
  counterResource="",
  characterScale=1.42f,
  characterOffsetX=4f,
  characterOffsetY=22f,
  characterViewportHeight=0.68f,
 };
 public static readonly MerchantPresentation Dungeon=new(){
  id="dungeon_peddler",
  displayName="地下の露商人",
  characterResource="Art/Portraits/PopDark/hero-courier-cutout-v1",
  backdropResource="Art/UI/PopDark/hub-bg-v1",
  counterResource="",
  idleLine="暗がりの中でも、欲しいものなら届ける。",
  soldOutLine="在庫は尽きた。次の層で会おう。",
  purchaseLine="受け取っておけ。早く戻れ。",
  characterScale=1.38f,
  characterOffsetY=20f,
  characterViewportHeight=0.66f,
 };
 public static readonly MerchantPresentation Faction=new(){
  id="faction_broker",
  displayName="勢力の取次",
  characterResource="Art/Portraits/PopDark/hero-courier-hub-v1",
  backdropResource="Art/UI/PopDark/hub-bg-v1",
  counterResource="",
  idleLine="この店の品は、関係の証でもある。",
  soldOutLine="今日はこれ以上出せない。",
  purchaseLine="記録しておく。良い取引だった。",
  characterScale=1.4f,
  characterOffsetY=18f,
  characterViewportHeight=0.68f,
 };

 public static MerchantPresentation Resolve(string contextId){
  if(contextId=="dungeon")return Dungeon;
  if(contextId=="faction")return Faction;
  return Default;
 }
}
}
