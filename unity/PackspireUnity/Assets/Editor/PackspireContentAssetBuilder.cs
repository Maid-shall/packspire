#if UNITY_EDITOR
using System;
using System.Linq;
using Packspire;
using UnityEditor;
using UnityEngine;

public static class PackspireContentAssetBuilder {
 const string RootFolder="Assets/Resources/Packspire";
 const string ContentFolder=RootFolder+"/Content";
 const string AssetPath=RootFolder+"/PackspireContentDatabase.asset";
 const string CardPath=ContentFolder+"/CardContent.asset";
 const string ItemPath=ContentFolder+"/ItemContent.asset";
 const string ActorPath=ContentFolder+"/ActorContent.asset";
 const string WorldPath=ContentFolder+"/WorldContent.asset";

 [MenuItem("PACKSPIRE/Content/Create Default Content Database")]
 public static void CreateDefaultFromMenu(){
  if(AssetDatabase.LoadAssetAtPath<PackspireContentDatabase>(AssetPath)!=null){
   EditorUtility.DisplayDialog("PACKSPIRE","Content Database already exists. Existing authored data was not overwritten.","OK");
   Selection.activeObject=AssetDatabase.LoadAssetAtPath<PackspireContentDatabase>(AssetPath);
   return;
  }
  BuildDefaultsForMigration();
 }

 [MenuItem("PACKSPIRE/Content/Validate Content Database")]
 public static void ValidateFromMenu(){
  var asset=AssetDatabase.LoadAssetAtPath<PackspireContentDatabase>(AssetPath);
  if(asset==null){EditorUtility.DisplayDialog("PACKSPIRE","Content Database was not found.","OK");return;}
  var report=PackspireContent.Validate(asset);
  foreach(var warning in report.warnings)Debug.LogWarning(warning,asset);
  foreach(var error in report.errors)Debug.LogError(error,asset);
  EditorUtility.DisplayDialog("PACKSPIRE",report.Summary,"OK");
 }

 /// <summary>One-time migration entry point. Refuses to overwrite authored data.</summary>
 public static void BuildDefaultsForMigration(){
  if(AssetDatabase.LoadAssetAtPath<PackspireContentDatabase>(AssetPath)!=null)
   throw new InvalidOperationException($"Refusing to overwrite existing content database at {AssetPath}.");

  EnsureFolder("Assets/Resources","Packspire");
  EnsureFolder(RootFolder,"Content");
  var cards=ScriptableObject.CreateInstance<PackspireCardContentSet>();
  var items=ScriptableObject.CreateInstance<PackspireItemContentSet>();
  var actors=ScriptableObject.CreateInstance<PackspireActorContentSet>();
  var world=ScriptableObject.CreateInstance<PackspireWorldContentSet>();
  Populate(cards,items,actors,world);
  AssetDatabase.CreateAsset(cards,CardPath);
  AssetDatabase.CreateAsset(items,ItemPath);
  AssetDatabase.CreateAsset(actors,ActorPath);
  AssetDatabase.CreateAsset(world,WorldPath);

  var asset=ScriptableObject.CreateInstance<PackspireContentDatabase>();
  asset.schemaVersion=PackspireContentDatabase.SupportedSchemaVersion;
  asset.cardContent=cards;
  asset.itemContent=items;
  asset.actorContent=actors;
  asset.worldContent=world;
  AssetDatabase.CreateAsset(asset,AssetPath);
  EditorUtility.SetDirty(cards);
  EditorUtility.SetDirty(items);
  EditorUtility.SetDirty(actors);
  EditorUtility.SetDirty(world);
  EditorUtility.SetDirty(asset);
  AssetDatabase.SaveAssets();
  AssetDatabase.Refresh();

  var report=PackspireContent.Validate(asset);
  foreach(var warning in report.warnings)Debug.LogWarning(warning,asset);
  foreach(var error in report.errors)Debug.LogError(error,asset);
  if(!report.IsValid)throw new InvalidOperationException(report.Summary);
  Debug.Log($"Created {AssetPath}. {report.Summary}",asset);
  Selection.activeObject=asset;
 }

 static void Populate(PackspireCardContentSet cards,PackspireItemContentSet items,PackspireActorContentSet actors,PackspireWorldContentSet world){
  items.board=new[]{
   Element.Fire,Element.Wind,Element.Water,Element.Earth,Element.Fire,Element.Water,
   Element.Earth,Element.Fire,Element.Wind,Element.Water,Element.Earth,Element.Wind,
   Element.Water,Element.Earth,Element.Fire,Element.Wind,Element.Water,Element.Fire,
   Element.Wind,Element.Water,Element.Earth,Element.Fire,Element.Wind,Element.Earth
  };
  cards.statuses=Statuses();
  cards.cards=Cards();
  cards.explorationCards=ExplorationCards();
  cards.consumables=Consumables();
  items.items=Items();
  items.backpacks=Backpacks();
  items.storageCores=StorageCores(items.board);
  items.conduits=Conduits();
  items.resonances=Resonances();
  items.stabilities=Stabilities();
  items.colorTraits=ColorTraits();
  actors.roles=Roles();
  actors.enemies=Enemies();
  actors.factions=Factions();
  actors.characters=Characters();
  world.dungeons=Dungeons();
  world.facilities=Facilities();
  world.events=Events();
  world.merchants=Merchants();
  world.rewardPools=RewardPools();
  world.balance=Balance();
 }

 static StatusContent[] Statuses()=>new[]{
  Status("strength","強化","▲","与えるダメージが蓄積数だけ増加する。","buff"),
  Status("weak","弱体","▽","与えるダメージが25%低下する。","debuff"),
  Status("vulnerable","脆弱","◇","受けるダメージが50%増加する。","debuff"),
  Status("poison","毒","●","ターン終了時に蓄積数のダメージを受け、蓄積が1減る。","debuff",true),
  Status("burn","火傷","♨","ターン終了時に蓄積数のダメージを受ける。","debuff"),
  Status("regen","再生","✚","ターン終了時にHPを回復する。","buff"),
  Status("armorBreak","防具破壊","◆","得るブロックが蓄積数だけ減少する。","debuff")
 };

 static CardContent[] Cards()=>new[]{
  Card("basicStrike","基本攻撃",CardType.Attack,1,"敵に2D6-2ダメージ。",damage:5),
  Card("basicGuard","基本防御",CardType.Skill,1,"5ブロック。",block:5),
  Card("basicTactic","基本戦術",CardType.Skill,0,"2ブロック。",block:2),
  Card("slash","斬撃",CardType.Attack,1,"敵に2D6ダメージ。",damage:7),
  Card("guard","防御",CardType.Skill,1,"6ブロック。",block:6),
  Card("spark","火花",CardType.Power,1,"2D6-3ダメージ。次の攻撃+3。火傷2を2ターン与える。",damage:4,buff:3,
   effects:new[]{Effect("burn",EffectTarget.Enemy,2,2)}),
  Card("mend","応急手当",CardType.Skill,1,"HPを4回復。",heal:4),
  Card("stab","刺突",CardType.Attack,0,"敵に2D6-4ダメージ。",damage:3),
  Card("brace","堅守",CardType.Skill,2,"13ブロック。再生2を2ターン得る。",block:13,
   effects:new[]{Effect("regen",EffectTarget.Self,2,2)}),
  Card("focus","整流",CardType.Power,0,"エネルギー+1。",energy:1),
  Card("bomb","爆薬",CardType.Attack,2,"敵に2D6+8ダメージ。廃棄。",damage:15,exhaust:true),
  Card("pierce","貫通突き",CardType.Attack,2,"敵に2D6+4ダメージ。",damage:11),
  Card("parry","受け流し",CardType.Skill,0,"3ブロック。",block:3),
  Card("acid","酸液",CardType.Attack,1,"敵に2D6-1ダメージ。毒3を与える。",damage:6,
   effects:new[]{Effect("poison",EffectTarget.Enemy,3)}),
  Card("tailwind","追い風",CardType.Power,1,"エネルギー+1、4ブロック。",energy:1,block:4),
  Card("devour","貪食斬り",CardType.Attack,1,"敵に2D6+6ダメージ。自身に2ダメージ。",damage:13,selfDamage:2),
  Card("inferno","焔断ち",CardType.Attack,2,"敵に2D6+11ダメージ。",damage:18),
  Card("echoWall","反響障壁",CardType.Skill,1,"12ブロック。",block:12),
  Card("starBomb","星喰い爆薬",CardType.Attack,2,"敵に2D6+17ダメージ。廃棄。",damage:24,exhaust:true)
 };

 static ExplorationCardContent[] ExplorationCards()=>new[]{
  new ExplorationCardContent{id="gb_lamp",name="灯",place="lamp",cost=1,text="マスに灯りを置く。通過するたびに成長し、成熟すると導線を支える。"},
  new ExplorationCardContent{id="gb_fog",name="帳",place="fog",cost=1,text="マスに霧を置く。防護と攪乱に使う探索術式。"},
  new ExplorationCardContent{id="gb_seal",name="楔",place="seal",cost=1,text="マスを封鎖する。曲がるための壁を作る探索術式。"}
 };

 static ConsumableContent[] Consumables()=>new[]{
  new ConsumableContent{id="heal",name="治療薬",description="HPを12回復する。",effect=ConsumableEffectType.Heal,amount=12},
  new ConsumableContent{id="guard",name="硬化薬",description="10ブロックを得る。",effect=ConsumableEffectType.Block,amount=10},
  new ConsumableContent{id="fire",name="火点瓶",description="敵に2D6+5ダメージ。",effect=ConsumableEffectType.Damage,amount=12},
  new ConsumableContent{id="energy",name="活力薬",description="エネルギーを2得る。",effect=ConsumableEffectType.Energy,amount=2}
 };

 static ItemContent[] Items()=>new[]{
  Item("sword","欠けた剣",ItemType.Weapon,new[]{"slash","slash"},"縦2マスの剣。火色一致で攻撃を強化する。",
   new[]{Cell(0,0,Element.Fire),Cell(0,1,Element.Fire)},"盾と隣接: ダメージ+1、ブロック+2。熾火と隣接: 攻撃+2。"),
  Item("shield","旅人の盾",ItemType.Armor,new[]{"guard","guard"},"横2マスの盾。土色一致で防御を強化する。",
   new[]{Cell(0,0,Element.Earth),Cell(1,0,Element.Earth)},"剣と隣接: ブロック+2。結晶と隣接: コスト-1。"),
  Item("ember","熾火のルーン",ItemType.Rune,new[]{"spark"},"火を増幅する1マスルーン。",
   new[]{Cell(0,0,Element.Fire,2)},"武器と隣接: その攻撃+2。"),
  Item("herb","薬草袋",ItemType.Supply,new[]{"mend"},"水属性の回復道具。",
   new[]{Cell(0,0,Element.Water)}),
  Item("dagger","連撃の短剣",ItemType.Weapon,new[]{"stab","stab"},"風属性で軽く扱える短剣。",
   new[]{Cell(0,0,Element.Wind,2)}),
  Item("plate","古い胸当て",ItemType.Armor,new[]{"brace"},"L字3マスの重防具。",
   new[]{Cell(0,0,Element.Earth),Cell(1,0,Element.Earth),Cell(0,1,Element.Water)}),
  Item("crystal","共鳴結晶",ItemType.Rune,new[]{"focus"},"水と風の縦2マス結晶。",
   new[]{Cell(0,0,Element.Water),Cell(0,1,Element.Wind)},"隣接装備のカードコスト-1。"),
  Item("bomb","煤けた爆弾",ItemType.Supply,new[]{"bomb"},"火2点を持つ使い切り爆薬。",
   new[]{Cell(0,0,Element.Fire,2)}),
  Item("spear","折畳み槍",ItemType.Weapon,new[]{"pierce"},"縦3マスの長柄武器。",
   new[]{Cell(0,0,Element.Wind),Cell(0,1,Element.Fire),Cell(0,2,Element.Wind)}),
  Item("buckler","歯車の小盾",ItemType.Armor,new[]{"parry"},"土属性の小型盾。",
   new[]{Cell(0,0,Element.Earth)}),
  Item("flask","錬金フラスコ",ItemType.Supply,new[]{"acid"},"水と火の横2マス道具。",
   new[]{Cell(0,0,Element.Water),Cell(1,0,Element.Fire)}),
  Item("charm","風読みの護符",ItemType.Rune,new[]{"tailwind"},"風2点を持つ護符。",
   new[]{Cell(0,0,Element.Wind,2)}),
  Item("cursed_blade","飢えた呪剣",ItemType.Weapon,new[]{"devour"},"強力だが代償を要求する縦2マス武器。",
   new[]{Cell(0,0,Element.Fire,2),Cell(0,1,Element.Earth)})
 };

 static RoleContent[] Roles()=>new[]{
  Role("warrior","戦士","基本職","武器攻撃を強化する。最大HP+2。"),
  Role("guardian","守護兵","基本職","防御カードを強化する。最大HP+2。"),
  Role("scout","斥候","基本職","短剣・槍と探索に優れる。"),
  Role("artificer","工匠","基本職","道具カードを強化する。"),
  Role("blade_master","剣聖","上級職","剣と短剣を大幅に強化する。"),
  Role("bulwark","城塞騎士","上級職","防御カードを大幅に強化する。"),
  Role("hunter","魔獣狩り","上級職","短剣と槍を大幅に強化する。"),
  Role("grand_artificer","錬装術師","上級職","道具を大幅に強化する。"),
  Role("arsenal_lord","万兵の王","隠し職","すべての武器を強化する。",15),
  Role("pack_saint","不動の聖者","隠し職","防御と回復を強化する。",15),
  Role("rune_weaver","境界の織手","隠し職","ルーン由来カードを0コスト化する。",15),
  Role("grid_dancer","盤上の舞踏家","隠し職","0コストカードを強化する。",15),
  Role("quickblade","迅刃士","複合職","風色一致の攻撃を強化する。",15),
  Role("anchor_knight","定着騎士","複合職","土色一致の防御を強化する。",15),
  Role("siege_channeler","攻城導師","複合職","複数色一致を高火力へ変える。",15),
  Role("right_hand_swordsman","片手剣鬼","配置隠し職","右側の片手武器を強化する。",15),
  Role("iron_vanguard","鉄殻前衛","勢力職","防具を強化する。",15),
  Role("spore_druid","胞子森導師","勢力職","回復を強化する。",15),
  Role("guild_factor","荷造商務官","勢力職","道具を強化する。",15),
  Role("void_apostle","虚無の使徒","勢力職","呪い武器を強化する。",15)
 };

 static EnemyContent[] Enemies()=>new[]{
  Board(Enemy("sentinel","鉄殻の番兵",1,34,
   Move(8),Move(5,Effect("strength",EffectTarget.Self,2,3))),EnemyBoardBehavior.Wait,3,0,1),
  Board(Enemy("rats","洞穴ネズミの群れ",1,29,Move(8),Move(6)),EnemyBoardBehavior.Chase,5,2,5),
  Board(Enemy("porter","錆びた荷運び人形",1,38,Move(10),Move(0)),EnemyBoardBehavior.Patrol,3,1,3),
  Board(Enemy("mage","胞子の魔導師",2,45,
   Move(7,Effect("poison",EffectTarget.Player,3)),Move(11)),EnemyBoardBehavior.Wait,5,1,2),
  Board(Enemy("beast","鎧喰い獣",2,50,
   Move(9,Effect("armorBreak",EffectTarget.Player,2,2)),Move(10)),EnemyBoardBehavior.Chase,4,2,5),
  Board(Enemy("knight","虚ろな騎士",2,54,
   Move(13),Move(6,Effect("vulnerable",EffectTarget.Player,1,2))),EnemyBoardBehavior.Patrol,4,1,4),
  Board(Enemy("dragon","劫火竜",2,62,new[]{Move(14),Move(10),Move(16)},
   LoadSprite("Assets/Resources/Art/Portraits/enemy-dragon-v1.png"),"Art/Portraits/enemy-dragon-v1"),EnemyBoardBehavior.Chase,6,1,6),
  Board(Enemy("boss","荷喰らい",3,72,Move(12),Move(12),Move(17)),EnemyBoardBehavior.Chase,6,2,6)
 };

 static DungeonContent[] Dungeons()=>new[]{
  new DungeonContent{id="old_spire",name="古塔パックスパイア",description="格子盤探索の基準となる古塔。",battles=5,hpScale=1f,damage=0,goldScale=1f},
  new DungeonContent{id="ash_forge",name="灰熱の鋳造坑",description="敵の密度と攻撃性が高い高温坑道。",battles=6,hpScale=1.35f,damage=2,goldScale=1.25f},
  new DungeonContent{id="hollow_archive",name="虚ろなる大記憶庫",description="多数の精鋭が待つ最深記憶域。",battles=8,hpScale=1.7f,damage=4,goldScale=1.55f}
 };

 static BackpackContent[] Backpacks()=>new[]{
  new BackpackContent{id="standard",name="探索者の鞄",description="標準的な6×4の収納術式。"},
  new BackpackContent{id="merchant",name="行商人の鞄",description="交易向けの属性配列を持つ鞄。"},
  new BackpackContent{id="arcane",name="魔導鞄",description="ルーン向けの属性配列を持つ鞄。"},
  new BackpackContent{id="coffin",name="棺型ケース",description="縦長装備をまとめやすい棺型ケース。"},
  new BackpackContent{id="living",name="生きている鞄",description="特殊な変化を起こす生体鞄。"}
 };

 static FactionContent[] Factions()=>new[]{
  new FactionContent{id="iron",name="鉄殻軍",description="防具カードを強化する勢力。",ranks=new[]{"従士","兵士","騎士","鉄将"}},
  new FactionContent{id="spore",name="胞子教団",description="勝利後の回復を強化する勢力。",ranks=new[]{"芽吹き","培養士","森導師","森の代行者"}},
  new FactionContent{id="guild",name="荷造り師組合",description="商店と道具を得意とする勢力。",ranks=new[]{"見習い","組合員","商務官","大番頭"}},
  new FactionContent{id="void",name="虚無の巡礼者",description="獲得ゴールドと呪い装備が増える勢力。",ranks=new[]{"迷い子","巡礼者","使徒","深淵卿"}}
 };

 static CharacterContent[] Characters()=>new[]{
  Character("ren","蓮","鉄鎧の剣士","前線で敵の刃を受け止める遠征者。",0,0,"不屈","最大HP+4","maxHpBonus",4,"ren_rush","突撃","敵に10ダメージ。戦闘中1回。"),
  Character("mio","澪","影走りの斥候","隙を見逃さない探索の専門家。",1,1,"先読み","戦闘開始時に1枚追加ドロー","openingDraw",1,"mio_read","見切り","8ブロック、1枚ドロー。戦闘中1回。"),
  Character("kuro","玄","城壁の守人","最初の一撃を凌ぐ重装の護衛。",2,0,"堅守","戦闘開始時に4ブロック","openingBlock",4,"kuro_bulwark","鉄壁","14ブロック。戦闘中1回。"),
  Character("hina","陽菜","錬装の技師","装備と道具の扱いに長けた工匠。",3,2,"整備","戦闘勝利時の所持金+3","winGold",3,"hina_repair","応急修理","HPを10回復。戦闘中1回。"),
  Character("sena","瀬名","炉脚の闘士","蹴りで戦場を切り開く遠征者。",0,0,"炉脚","最大HP+2","maxHpBonus",2,"sena_kick","蹴旋","敵に14ダメージ。戦闘中1回。",
   LoadSprite("Assets/Resources/Art/Portraits/hero-sena-kick-v1.png"),
   LoadSprite("Assets/Resources/Art/Portraits/DD/hero-sena-front-v1.png"),
   LoadSprite("Assets/Resources/Art/Portraits/DD/hero-sena-hub-v1.png"),
   "Art/Portraits/hero-sena-kick-v1","Art/Portraits/DD/hero-sena-front-v1","Art/Portraits/DD/hero-sena-hub-v1",
   .39f,.22f,2.65f,0f,.02f)
 };

 static FacilityContent[] Facilities()=>new[]{
  Facility("gate","STRATA","遠征",ScreenId.Expedition,HubFacilityKind.Scene,"塔の未知区画へ向かう。","strata",.74f,.32f,"遠"),
  Facility("forge","PACK","荷造り",ScreenId.Pack,HubFacilityKind.Workbench,"装備と収納術式を整える。","forge",.42f,.48f,"荷"),
  Facility("vault","VAULT","保管庫",ScreenId.Vault,HubFacilityKind.Archive,"持ち帰った装備を確認する。","vault",.28f,.56f,"庫"),
  Facility("heirloom","HEIRLOOM","家宝",ScreenId.Heirloom,HubFacilityKind.Archive,"一つの装備を長期育成する。","heirloom",.22f,.42f,"家"),
  Facility("guild","RANK","ステータス",ScreenId.Status,HubFacilityKind.Archive,"習得した役職と遠征者の記録。","guild",.36f,.38f,"職"),
  Facility("codex","INDEX","図鑑",ScreenId.Compendium,HubFacilityKind.Archive,"遭遇記録と未解明の索引。","codex",.56f,.62f,"録"),
  Facility("embassy","EMBASSY","勢力",ScreenId.Faction,HubFacilityKind.Scene,"勢力との関係と所属を確認する。","embassy",.62f,.44f,"勢"),
  Facility("shop","SHOP","商店",ScreenId.Shop,HubFacilityKind.Workbench,"拠点の商人を訪ねる。","guild",.58f,.52f,"商"),
  Facility("barracks","ROSTER","キャラクター",ScreenId.Character,HubFacilityKind.Archive,"遠征者の選択と確認。","roster",.48f,.28f,"者")
 };

 static EventContent[] Events()=>new[]{
  new EventContent{
   id="memory_rift",
   eyebrow="ANOMALY  /  RITE",
   title="記憶の揺らぎ",
   body="空間の奥で、紫の残光がゆっくりと脈動している。触れれば何かが変わる。その代償までは、まだ記されていない。",
   choices=new[]{
    EventChoice("offer","代償を支払う","代償を払い、24Gを得た",
     EventEffect(EventEffectType.Hp,-6),EventEffect(EventEffectType.Gold,24)),
    EventChoice("repair","残響を修復する","残響が装備を修復した",
     EventEffect(EventEffectType.RepairAll,6)),
    EventChoice("leave","立ち去る","黒い靄を振り払い、探索へ戻った")
   }
  }
 };

 static MerchantContent[] Merchants()=>new[]{
  Merchant("traveler","default","行商人","品書きを見てくれ。いいものだけ持ってきた。",
   "今は出せる品がない。","よし、取引成立だ。",1.42f,4f,22f,.68f,
   "Art/Portraits/PopDark/hero-courier-cutout-v1"),
  Merchant("dungeon_peddler","dungeon","地下の露商人","暗がりの中でも、欲しいものなら届ける。",
   "在庫は尽きた。次の層で会おう。","受け取っておけ。役に立つ。",1.38f,0f,20f,.66f,
   "Art/Portraits/PopDark/hero-courier-cutout-v1"),
  Merchant("faction_broker","faction","勢力の取次","この店の品は、関係の証でもある。",
   "今日はこれ以上出せない。","記録しておく。良い取引だった。",1.4f,0f,18f,.68f,
   "Art/Portraits/PopDark/hero-courier-hub-v1")
 };

 static RewardPoolContent[] RewardPools()=>new[]{
  new RewardPoolContent{id="standard",itemIds=new[]{"dagger","plate","crystal","bomb","spear","buckler","flask","charm"}}
 };

 static GameBalanceContent Balance()=>new(){
  baseEnergy=3,
  initialHand=5,
  gridGrowthThreshold=3,
  gridDoomMax=6,
  gridMoveSpeed=2.4f,
  gridFogMoveSpeed=1.2f,
  defaultDungeonId="old_spire",
  defaultCharacterId="ren",
  defaultRoleId="warrior",
  defaultFactionId="iron",
  defaultBackpackId="standard",
  defaultEventId="memory_rift"
 };

 static StorageCoreContent[] StorageCores(Element[] board)=>new[]{
  Core("standard","標準術核","6×4の基本術式。装備を自由に回転できる。",board,RotationCapability.FullTurn),
  Core("merchant","交易術核","交易向けの属性配置。回転は自由。",Shift(board,1),RotationCapability.FullTurn),
  Core("arcane","魔導術核","ルーン向けの属性配置。回転は自由。",Shift(board,2),RotationCapability.FullTurn),
  Core("coffin","棺型術核","縦長配置向け。90度単位で回転できる。",Shift(board,3),RotationCapability.QuarterTurn),
  Core("living","生体術核","反転した属性配置。0度と180度のみ。",board.Reverse().ToArray(),RotationCapability.FlipOnly)
 };

 static ConduitContent[] Conduits()=>new[]{
  new ConduitContent{id="classic",name="古典導線",description="一致色をカード数値へ変換する標準導線。",bonuses=new[]{
   Bonus(Element.Fire,ConduitBonusTarget.Damage),
   Bonus(Element.Water,ConduitBonusTarget.Block),
   Bonus(Element.Water,ConduitBonusTarget.Heal,useHalfWaterHeal:true),
   Bonus(Element.Wind,ConduitBonusTarget.CostReduce,3,1),
   Bonus(Element.Earth,ConduitBonusTarget.Block,2,1,matchDivisor:2)
  }},
  new ConduitContent{id="mute",name="沈黙導線",description="色一致ボーナスを発生させない検証用導線。"}
 };

 static ResonanceContent[] Resonances()=>new[]{
  new ResonanceContent{id="classic",name="古典共鳴",description="隣接装備のLINKと上位カード置換を行う。",links=new[]{
   new ResonanceLinkContent{label="剣×盾",templateA="sword",templateB="shield",damageBonus=1,blockBonus=2},
   new ResonanceLinkContent{label="熾火×武器",templateA="ember",useTypeB=true,typeB=ItemType.Weapon,damageBonus=2},
   new ResonanceLinkContent{label="結晶×装備",templateA="crystal",costReduce=1}
  },upgrades=new[]{
   new ResonanceUpgradeContent{hostTemplate="sword",neighborTemplate="ember",fromCardId="slash",toCardId="inferno"},
   new ResonanceUpgradeContent{hostTemplate="shield",neighborTemplate="crystal",fromCardId="guard",toCardId="echoWall"},
   new ResonanceUpgradeContent{hostTemplate="bomb",neighborTemplate="flask",toCardId="starBomb",replaceAllCards=true}
  }},
  new ResonanceContent{id="silent",name="沈黙共鳴",description="LINKもカード置換も発生させない検証用共鳴。"}
 };

 static StabilityContent[] Stabilities()=>new[]{
  new StabilityContent{id="stable",name="安定式",description="耐久消費は通常。過負荷はほぼ発生しない。",durabilityDrainScale=1f,runawayThreshold=99,runawayCardPenalty=.5f},
  new StabilityContent{id="volatile",name="過負荷式",description="耐久消費が速く、高負荷でカード出力が低下する。",durabilityDrainScale=1.5f,runawayThreshold=8,runawayCardPenalty=.65f}
 };

 static ColorTraitContent[] ColorTraits()=>new[]{
  Trait("fire_dmg_5","焔力・小",Element.Fire,5,ColorTraitEffect.Damage,1),
  Trait("fire_dmg_7","焔力・大",Element.Fire,7,ColorTraitEffect.Damage,2),
  Trait("water_block_5","水防・小",Element.Water,5,ColorTraitEffect.Block,1),
  Trait("water_block_7","水防・大",Element.Water,7,ColorTraitEffect.Block,2),
  Trait("wind_cost_5","風軽・小",Element.Wind,5,ColorTraitEffect.CostReduce,1),
  Trait("wind_cost_7","風軽・大",Element.Wind,7,ColorTraitEffect.CostReduce,1),
  Trait("earth_block_5","土守・小",Element.Earth,5,ColorTraitEffect.Block,1),
  Trait("earth_block_7","土守・大",Element.Earth,7,ColorTraitEffect.Block,2),
  Trait("water_heal_5","水癒・小",Element.Water,5,ColorTraitEffect.Heal,1),
  Trait("water_heal_7","水癒・大",Element.Water,7,ColorTraitEffect.Heal,2),
  Trait("wind_draw_7","風読",Element.Wind,7,ColorTraitEffect.Draw,1),
  Trait("earth_recycle_7","土還",Element.Earth,7,ColorTraitEffect.Recycle,1),
  Trait("fire_free_7","不滅",Element.Fire,7,ColorTraitEffect.DurabilityFree,1)
 };

 static StatusContent Status(string id,string name,string icon,string description,string kind,bool stack=false)=>
  new StatusContent{id=id,name=name,icon=icon,description=description,kind=kind,stack=stack};
 static EffectContent Effect(string id,EffectTarget target,int amount=1,int duration=0)=>
  new EffectContent{statusId=id,target=target,amount=amount,duration=duration};
 static CellContent Cell(int x,int y,Element element,int value=1)=>new CellContent{x=x,y=y,element=element,value=value};
 static CardContent Card(string id,string name,CardType type,int cost,string text,int damage=0,int block=0,int heal=0,int buff=0,int energy=0,int selfDamage=0,bool exhaust=false,EffectContent[] effects=null)=>
  new CardContent{id=id,name=name,type=type,cost=cost,text=text,damage=damage,block=block,heal=heal,buff=buff,energy=energy,selfDamage=selfDamage,exhaust=exhaust,effects=effects??Array.Empty<EffectContent>()};
 static ItemContent Item(string id,string name,ItemType type,string[] cards,string description,CellContent[] cells,string linkRule="")=>
  new ItemContent{id=id,name=name,type=type,cardIds=cards,description=description,cells=cells,linkRule=linkRule,
   explorationCardId=type switch{ItemType.Weapon=>"gb_seal",ItemType.Rune=>"gb_lamp",_=>"gb_fog"}};
 static RoleContent Role(string id,string name,string kind,string description,int max=10){
  string family=new[]{"guardian","bulwark","anchor_knight","iron_vanguard","pack_saint"}.Contains(id)?"guardian"
   :new[]{"scout","hunter","quickblade","grid_dancer","spore_druid"}.Contains(id)?"scout"
   :new[]{"artificer","grand_artificer","rune_weaver","siege_channeler","guild_factor"}.Contains(id)?"artificer"
   :"warrior";
  string[] starting=family=="guardian"?new[]{"basicStrike","basicGuard","basicGuard","basicGuard"}
   :family=="scout"?new[]{"basicStrike","basicStrike","basicStrike","basicTactic"}
   :family=="artificer"?new[]{"basicStrike","basicGuard","basicTactic","basicTactic"}
   :new[]{"basicStrike","basicStrike","basicGuard","basicGuard"};
  string milestone=family=="guardian"?"防御カードを連続使用すると次の防御効果が上昇する。"
   :family=="scout"?"異なる装備由来のカードを続けて使うと追加ドロー。"
   :family=="artificer"?"属性一致が3色以上なら戦闘開始時にエネルギーを得る。"
   :"同じ武器由来のカードを続けて使うと追加ダメージ。";
  string maximum=family=="guardian"?"戦闘開始時に防御を得て、余剰防御を次のターンへ一部持ち越す。"
   :family=="scout"?"各戦闘で最初に使う0コストカードを複製する。"
   :family=="artificer"?"戦闘中に最初に使うルーン・道具カードの耐久を消費しない。"
   :"武器カードを一定回数使うたび、ラン中の攻撃力が成長する。";
  return new RoleContent{id=id,name=name,kind=kind,description=description,maxLevel=max,family=family,
   startingCardIds=starting,milestoneText=milestone,maximumMilestoneText=maximum};
 }
 static EnemyMoveContent Move(int damage,params EffectContent[] effects)=>new EnemyMoveContent{damage=damage,effects=effects??Array.Empty<EffectContent>()};
 static EnemyContent Enemy(string id,string name,int tier,int hp,params EnemyMoveContent[] moves)=>Enemy(id,name,tier,hp,moves,null,"");
 static EnemyContent Enemy(string id,string name,int tier,int hp,EnemyMoveContent[] moves,Sprite portrait,string legacy)=>
  new EnemyContent{id=id,name=name,tier=tier,hp=hp,moves=moves,portrait=portrait,legacyPortraitResource=legacy,
   boardBehavior=EnemyBoardBehavior.Patrol,boardSightRange=4,boardMoveSteps=1,boardPatrolRadius=3};
 static EnemyContent Board(EnemyContent enemy,EnemyBoardBehavior behavior,int sight,int steps,int radius){
  enemy.boardBehavior=behavior;
  enemy.boardSightRange=sight;
  enemy.boardMoveSteps=steps;
  enemy.boardPatrolRadius=radius;
  return enemy;
 }
 static CharacterContent Character(string id,string name,string title,string description,int body,int hair,string traitName,string traitText,string traitKind,int traitValue,string skillId,string skillName,string skillText,
  Sprite portrait=null,Sprite front=null,Sprite hub=null,string legacyPortrait="",string legacyFront="",string legacyHub="",float focusX=.5f,float focusY=.26f,float zoom=2.2f,float offsetX=0f,float offsetY=0f)=>
  new CharacterContent{id=id,name=name,title=title,description=description,portraitBody=body,portraitHair=hair,traitName=traitName,traitText=traitText,traitKind=traitKind,traitValue=traitValue,
   activeSkillId=skillId,activeSkillName=skillName,activeSkillText=skillText,portrait=portrait,portraitFront=front,portraitHub=hub,
   activeSkillKind=skillId=="mio_read"?CharacterSkillKind.BlockAndDraw:skillId=="kuro_bulwark"?CharacterSkillKind.Block:skillId=="hina_repair"?CharacterSkillKind.Heal:CharacterSkillKind.Damage,
   activeSkillAmount=skillId=="ren_rush"?10:skillId=="mio_read"?8:skillId=="kuro_bulwark"?14:skillId=="hina_repair"?10:skillId=="sena_kick"?14:0,
   activeSkillSecondaryAmount=skillId=="mio_read"?1:0,
   legacyPortraitResource=legacyPortrait,legacyPortraitFrontResource=legacyFront,legacyPortraitHubResource=legacyHub,
   portraitFocusX=focusX,portraitFocusY=focusY,portraitZoom=zoom,portraitBannerOffsetX=offsetX,portraitBannerOffsetY=offsetY};
 static FacilityContent Facility(string id,string eyebrow,string label,ScreenId screen,HubFacilityKind kind,string description,string theme,float x,float y,string seal)=>
  new FacilityContent{id=id,eyebrow=eyebrow,label=label,screen=screen,kind=kind,hubCard=true,description=description,
   legacyIconResource="Art/UI/PopDark/btn-card-v1",themeKey=theme,mapX=x,mapY=y,unlocked=true,seal=seal};
 static StorageCoreContent Core(string id,string name,string description,Element[] board,RotationCapability rotation)=>
  new StorageCoreContent{id=id,name=name,description=description,width=6,height=4,board=board,rotation=rotation};
 static ConduitBonusContent Bonus(Element element,ConduitBonusTarget target,int threshold=0,int amount=1,bool useHalfWaterHeal=false,int matchDivisor=1)=>
  new ConduitBonusContent{element=element,target=target,threshold=threshold,amountPerMatch=amount,useHalfWaterHeal=useHalfWaterHeal,matchDivisor=matchDivisor};
 static ColorTraitContent Trait(string id,string name,Element element,int required,ColorTraitEffect effect,int amount)=>
  new ColorTraitContent{id=id,name=name,element=element,requiredMatches=required,effect=effect,amount=amount};
 static EventChoiceContent EventChoice(string id,string label,string result,params EventEffectContent[] effects)=>
  new EventChoiceContent{id=id,label=label,resultText=result,effects=effects??Array.Empty<EventEffectContent>()};
 static EventEffectContent EventEffect(EventEffectType effect,int amount=0)=>new(){effect=effect,amount=amount};
 static MerchantContent Merchant(string id,string context,string name,string idle,string soldOut,string purchase,
  float scale,float offsetX,float offsetY,float viewportHeight,string characterResource)=>new(){
   id=id,contextId=context,displayName=name,idleLine=idle,soldOutLine=soldOut,purchaseLine=purchase,
   characterScale=scale,characterOffsetX=offsetX,characterOffsetY=offsetY,characterViewportHeight=viewportHeight,
   legacyCharacterResource=characterResource,legacyBackdropResource="Art/UI/PopDark/hub-bg-v1"
  };

 static Element[] Shift(Element[] source,int shift){
  var result=new Element[source.Length];
  for(int i=0;i<source.Length;i++)result[i]=source[(i+shift)%source.Length];
  return result;
 }

 static Sprite LoadSprite(string path)=>AssetDatabase.LoadAssetAtPath<Sprite>(path);

 static void EnsureFolder(string parent,string name){
  string path=$"{parent}/{name}";
  if(!AssetDatabase.IsValidFolder(path))AssetDatabase.CreateFolder(parent,name);
 }
}
#endif
