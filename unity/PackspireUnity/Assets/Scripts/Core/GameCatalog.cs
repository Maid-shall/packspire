using System.Collections.Generic;
using System.Linq;

namespace Packspire {
public static class GameCatalog {
 public static readonly Element[] Board={Element.Fire,Element.Wind,Element.Water,Element.Earth,Element.Fire,Element.Water,Element.Earth,Element.Fire,Element.Wind,Element.Water,Element.Earth,Element.Wind,Element.Water,Element.Earth,Element.Fire,Element.Wind,Element.Water,Element.Fire,Element.Wind,Element.Water,Element.Earth,Element.Fire,Element.Wind,Element.Earth};
 public static readonly Dictionary<string,ItemDef> Items=new(){
	  ["sword"]=new("sword","欠けた剣",ItemType.Weapon,"slash","縦2マスの剣。火色一致で攻撃を強化。",new CellDef(0,0,Element.Fire),new CellDef(0,1,Element.Fire)),
  ["shield"]=new("shield","旅人の盾",ItemType.Armor,"guard","横2マスの盾。土色一致で防御を強化。",new CellDef(0,0,Element.Earth),new CellDef(1,0,Element.Earth)),
  ["ember"]=new("ember","熾火のルーン",ItemType.Rune,"spark","火を濃縮する1マスルーン。",new CellDef(0,0,Element.Fire,2)),
  ["herb"]=new("herb","薬草袋",ItemType.Supply,"mend","水と風を持つ回復道具。",new CellDef(0,0,Element.Water)),
  ["dagger"]=new("dagger","連撃の短剣",ItemType.Weapon,"stab","風色で軽くなる短剣。",new CellDef(0,0,Element.Wind,2)),
  ["plate"]=new("plate","古い胸当て",ItemType.Armor,"brace","L字3マスの重装防具。",new CellDef(0,0,Element.Earth),new CellDef(1,0,Element.Earth),new CellDef(0,1,Element.Water)),
  ["crystal"]=new("crystal","共鳴結晶",ItemType.Rune,"focus","水と風の縦2マス結晶。",new CellDef(0,0,Element.Water),new CellDef(0,1,Element.Wind)),
  ["bomb"]=new("bomb","煤けた爆弾",ItemType.Supply,"bomb","火2点の使い切り爆薬。",new CellDef(0,0,Element.Fire,2)),
  ["spear"]=new("spear","折畳み槍",ItemType.Weapon,"pierce","縦3マスの長柄武器。",new CellDef(0,0,Element.Wind),new CellDef(0,1,Element.Fire),new CellDef(0,2,Element.Wind)),
  ["buckler"]=new("buckler","歯車の小盾",ItemType.Armor,"parry","土色の小型盾。",new CellDef(0,0,Element.Earth)),
  ["flask"]=new("flask","錬金フラスコ",ItemType.Supply,"acid","水と火の横2マス道具。",new CellDef(0,0,Element.Water),new CellDef(1,0,Element.Fire)),
  ["charm"]=new("charm","風読みの護符",ItemType.Rune,"tailwind","風2点の護符。",new CellDef(0,0,Element.Wind,2)),
  ["cursed_blade"]=new("cursed_blade","飢えた呪剣",ItemType.Weapon,"devour","強力だが外せない呪い装備。",new CellDef(0,0,Element.Fire,2),new CellDef(0,1,Element.Earth))
 };
 public static readonly Dictionary<string,CardDef> Cards=new(){
  ["basicStrike"]=C("basicStrike","基本攻撃",CardType.Attack,1,"敵に5ダメージ。",damage:5),["basicGuard"]=C("basicGuard","基本防御",CardType.Skill,1,"5ブロック。",block:5),["basicTactic"]=C("basicTactic","基本戦術",CardType.Skill,0,"2ブロック。",block:2),
  ["slash"]=C("slash","斬撃",CardType.Attack,1,"敵に7ダメージ。",damage:7),["guard"]=C("guard","防御",CardType.Skill,1,"6ブロック。",block:6),["spark"]=C("spark","火花",CardType.Power,1,"4ダメージ、次の攻撃+3。",damage:4,buff:3),["mend"]=C("mend","応急手当",CardType.Skill,1,"HPを4回復。",heal:4),["stab"]=C("stab","刺突",CardType.Attack,0,"敵に3ダメージ。",damage:3),["brace"]=C("brace","堅牢",CardType.Skill,2,"13ブロック。",block:13),["focus"]=C("focus","整流",CardType.Power,0,"エネルギー+1。",energy:1),["bomb"]=C("bomb","爆薬",CardType.Attack,2,"敵に15ダメージ。",damage:15,exhaust:true),["pierce"]=C("pierce","貫通突き",CardType.Attack,2,"敵に11ダメージ。",damage:11),["parry"]=C("parry","受け流し",CardType.Skill,0,"3ブロック。",block:3),["acid"]=C("acid","酸液",CardType.Attack,1,"敵に6ダメージ。",damage:6),["tailwind"]=C("tailwind","追い風",CardType.Power,1,"エネルギー+1、4ブロック。",energy:1,block:4),["devour"]=C("devour","貪食斬り",CardType.Attack,1,"13ダメージ、自身に2ダメージ。",damage:13,self:2),["inferno"]=C("inferno","焔断ち",CardType.Attack,2,"敵に18ダメージ。",damage:18),["echoWall"]=C("echoWall","反響障壁",CardType.Skill,1,"12ブロック。",block:12),["starBomb"]=C("starBomb","星喰い爆薬",CardType.Attack,2,"敵に24ダメージ。",damage:24,exhaust:true)
 };
 public static readonly Dictionary<string,RoleDef> Roles=new(){
  ["warrior"]=new("warrior","戦士","基本職","武器攻撃を強化。最大HP+2。"),["guardian"]=new("guardian","守護兵","基本職","防御カードを強化。最大HP+2。"),["scout"]=new("scout","斥候","基本職","短剣・槍と偵察に優れる。"),["artificer"]=new("artificer","工匠","基本職","道具カードを強化。"),
  ["blade_master"]=new("blade_master","剣聖","上級職","剣と短剣を大幅強化。"),["bulwark"]=new("bulwark","城塞騎士","上級職","防御カードを大幅強化。"),["hunter"]=new("hunter","魔獣狩り","上級職","短剣と槍を大幅強化。"),["grand_artificer"]=new("grand_artificer","錬装導師","上級職","道具を大幅強化。"),
  ["arsenal_lord"]=new("arsenal_lord","万刃の王","隠し職","すべての武器を強化。",15),["pack_saint"]=new("pack_saint","不動の聖者","隠し職","防御と回復を強化。",15),["rune_weaver"]=new("rune_weaver","境界の織手","隠し職","ルーンカードを0コスト化。",15),["grid_dancer"]=new("grid_dancer","盤上舞踏家","隠し職","0コストカードを強化。",15),["quickblade"]=new("quickblade","瞬刃士","複合職","風色一致の攻撃を強化。",15),["anchor_knight"]=new("anchor_knight","定錨騎士","複合職","土色一致の防御を強化。",15),["siege_channeler"]=new("siege_channeler","攻城導師","複合職","複数色一致を高出力化。",15),["right_hand_swordsman"]=new("right_hand_swordsman","片手剣鬼","配置隠し職","右側の片手武器を強化。",15),["iron_vanguard"]=new("iron_vanguard","鉄屑前衛","勢力職","防具を強化。",15),["spore_druid"]=new("spore_druid","胞子森導師","勢力職","回復を強化。",15),["guild_factor"]=new("guild_factor","荷造商務官","勢力職","道具を強化。",15),["void_apostle"]=new("void_apostle","虚無の使徒","勢力職","呪い武器を強化。",15)
 };
 public static readonly EnemyDef[] Enemies={new("sentinel","鉄殻の番兵",1,34,8,5),new("rats","洞穴ネズミの群れ",1,29,8,6),new("porter","錆びた荷運び人形",1,38,10,0),new("mage","胞子の魔導師",2,45,7,11),new("beast","鋼喰い獣",2,50,9,10),new("knight","虚ろな騎士",2,54,13,6),new("boss","荷喰らい",3,72,12,12,17)};
 public static readonly DungeonDef[] Dungeons={new("old_spire","古塔パックスパイア","36区画を自由探索する基準ダンジョン。",5,1,0,1),new("ash_forge","灰熱の鋳造坑","敵の密度と攻撃性が高い高熱坑道。",6,1.35f,2,1.25f),new("hollow_archive","虚ろなる大記憶庫","多数の精鋭が徘徊する最深記憶域。",8,1.7f,4,1.55f)};
 public static readonly BackpackDef[] Backpacks={new("standard","探索者の鞄","中央4マスが安全。",8,9,14,15),new("merchant","行商人の鞄","商店価格20%引き。",0,1,6),new("arcane","魔導鞄","四隅が安全。",0,5,18,23),new("coffin","棺型ケース","右端が安全。",5,11,17,23),new("living","生きている鞄","戦闘後に装備が回転。",8,14)};
 public static readonly FactionDef[] Factions={new("iron","鉄殻軍","防具カードを強化。","従士","兵士","騎士","鉄将"),new("spore","胞子教団","勝利後の回復を強化。","芽吹き","培養士","導師","森の代行者"),new("guild","荷造り師組合","商店価格を割引。","見習い","組合員","商務官","大番頭"),new("void","虚無の巡礼者","獲得ゴールドと呪い装備が増加。","迷い子","巡礼者","使徒","深淵卿")};
 static CardDef C(string id,string name,CardType type,int cost,string text,int damage=0,int block=0,int heal=0,int buff=0,int energy=0,int self=0,bool exhaust=false)=>new(id,name,type,cost,text){damage=damage,block=block,heal=heal,buff=buff,energy=energy,selfDamage=self,exhaust=exhaust};
 public static T Find<T>(IEnumerable<T> set,System.Func<T,string> id,string value)=>set.First(x=>id(x)==value);
}
}
