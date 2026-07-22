using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Packspire {
public partial class PackspireGame : MonoBehaviour {


 public static PackspireGame Instance { get; private set; }
 MetaSave meta; RunState run; ExplorationRunState exploration; BattleState battle; ScreenId screen; string selectedUid="",message=""; bool packingAtBase,developerPanel; Texture2D factionArt,characterArt,equipmentArt,roleArt,enemyArt,dungeonArt,bookSpread;
 Texture2D showcaseHeroArt,showcaseDragonArt;
 Sprite showcaseHeroSprite;
 ScreenId lastVisualScreen; bool visualScreenTracked;
 /// <summary>Temporary art-preview lock: battle always shows 瀬名 + 劫火竜 portraits.</summary>
 public const bool LockBattleShowcaseArt=true;
 public ScreenId UiScreen=>screen; public MetaSave UiMeta=>meta; public bool UiDeveloperPanelOpen=>developerPanel; public Texture2D UiCharacterArt=>characterArt; public Texture2D UiEquipmentArt=>equipmentArt; public Texture2D UiRoleArt=>roleArt; public Texture2D UiEnemyArt=>enemyArt; public Texture2D UiDungeonArt=>dungeonArt; public Texture2D UiFactionArt=>factionArt; public Texture2D UiBookArt=>bookSpread;
 public Texture2D UiShowcaseHeroArt=>showcaseHeroArt; public Sprite UiShowcaseHeroSprite=>showcaseHeroSprite; public Texture2D UiShowcaseDragonArt=>showcaseDragonArt;
 public RunState UiRun=>run; public string UiMessage=>message; public bool UiPackingAtBase=>packingAtBase;
 public ExplorationRunState UiExploration=>exploration;
 public bool UiUsesExplorationMap=>exploration!=null;
 public bool UiExplorationEventActive=>explorationEventActive;
 public int UiExplorationEventNodeId=>explorationEventNodeId;
 public BattleState UiBattle=>battle;
 public ScreenId UiDeveloperReturnScreen=>developerReturnScreen;
 public RoutePresentationMode CurrentRoutePresentationMode=>routePresentationMode;
 string rewardSelectionId="",shopSelectionId="";
 bool explorationEventActive; int explorationEventNodeId=-1;
 RoutePresentationMode routePresentationMode;
 ScreenId developerReturnScreen; bool developerHasReturn;

 public void SetRoutePresentationMode(RoutePresentationMode mode){routePresentationMode=mode;}
 void Awake(){if(Instance!=null&&Instance!=this){Destroy(gameObject);return;}Instance=this;DontDestroyOnLoad(gameObject);meta=SaveSystem.Load();factionArt=Resources.Load<Texture2D>("Art/faction-hub-sheet");characterArt=Resources.Load<Texture2D>("Art/character-creator-sheet");equipmentArt=Resources.Load<Texture2D>("Art/equipment-sheet");roleArt=Resources.Load<Texture2D>("Art/roles-sheet");enemyArt=Resources.Load<Texture2D>("Art/enemy-sheet");dungeonArt=Resources.Load<Texture2D>("Art/dungeon-sheet");showcaseHeroSprite=Resources.Load<Sprite>("Art/Portraits/hero-sena-kick-v1");showcaseHeroArt=showcaseHeroSprite==null?Resources.Load<Texture2D>("Art/Portraits/hero-sena-kick-v1"):null;showcaseDragonArt=Resources.Load<Texture2D>("Art/Portraits/enemy-dragon-v1");screen=meta.characterMade?ScreenId.Hub:ScreenId.Character;Application.targetFrameRate=60;}
 void OnDestroy(){if(Instance==this)Instance=null;}
 void Update(){if(Input.GetKeyDown(KeyCode.F10))UiToggleDeveloperPanel();if(!visualScreenTracked){lastVisualScreen=screen;visualScreenTracked=true;return;}if(lastVisualScreen==screen)return;var previous=lastVisualScreen;lastVisualScreen=screen;PackspireUiFoundation.Instance?.PlayFor(previous,screen);}
 public void UiNavigate(ScreenId target){
  explorationEventActive=false;explorationEventNodeId=-1;
  if(target==ScreenId.Pack)OpenPacking();
  else{
   if(target==ScreenId.Hub||target==ScreenId.Status||target==ScreenId.Vault||target==ScreenId.Heirloom||target==ScreenId.Faction||target==ScreenId.Expedition||target==ScreenId.Compendium){
    run=null;exploration=null;SetRoutePresentationMode(RoutePresentationMode.None);
   }
   screen=target;
  }
 }
 public void UiExplorationSelect(int nodeId){
  if(exploration==null||explorationEventActive)return;
  var def=ExplorationMapSystem.Def(exploration);
  if(ExplorationMapSystem.Node(def,nodeId)==null)return;
  if(!ExplorationMapSystem.IsRevealed(exploration,nodeId))return;
  exploration.selectedNodeId=nodeId;
 }
 public bool UiExplorationCanMove(int nodeId)=>!explorationEventActive&&ExplorationMapSystem.CanMove(exploration,nodeId);
 public bool UiExplorationMove(int nodeId){
  if(!UiExplorationCanMove(nodeId))return false;
  exploration.selectedNodeId=nodeId;
  return true;
 }
 public bool UiExplorationAtEntrance=>ExplorationMapSystem.IsAtEntrance(exploration);
 public void UiExplorationFinish(){
  message=UiExplorationAtEntrance?"遠征入口へ戻り、戦利品を持ち帰った":"外郭の途中から帰還した。戦利品と所持金は持ち帰れる";
  FinishRun(true);
 }
 public ExplorationEncounter UiExplorationOnArrived(int nodeId,bool firstVisit=true){
  if(exploration==null||run==null)return ExplorationEncounter.None;
  var encounter=ExplorationMapSystem.Enter(exploration,run,nodeId,firstVisit);
  if(encounter==ExplorationEncounter.Event){
   explorationEventActive=true;
   explorationEventNodeId=nodeId;
   message="記憶の揺らぎが道を覆った";
  } else if(encounter==ExplorationEncounter.Battle){
   StartBattle(false);
  } else if(encounter==ExplorationEncounter.Rest){
   run.hp=Mathf.Min(run.maxHp,run.hp+12);
   foreach(var item in run.inventory)item.durability=6;
   ExplorationMapSystem.MarkCleared(exploration,nodeId);
   message="休憩室で体を休め、装備を整えた";
  } else if(encounter==ExplorationEncounter.EnterBuilding){
   var def=ExplorationMapSystem.Def(exploration);
   var node=ExplorationMapSystem.Node(def,nodeId);
   if(node!=null&&ExplorationMapSystem.EnterInterior(exploration,node.interiorMapId,nodeId))
    message=$"{ExplorationMapSystem.Def(exploration)?.name}へ入った";
   else {message="扉はまだ開かない";return ExplorationEncounter.None;}
  } else if(encounter==ExplorationEncounter.ExitBuilding){
   if(ExplorationMapSystem.ExitInterior(exploration))message="地上へ戻った";
  } else {
   var node=ExplorationMapSystem.Node(ExplorationMapSystem.Def(exploration),nodeId);
   if(node!=null&&node.type=="building_door")message="鍵がかかっている";
  }
  return encounter;
 }
 public void UiResolveExplorationEvent(int choice){
  if(run==null||exploration==null||!explorationEventActive)return;
  if(choice==0){run.hp=Mathf.Max(1,run.hp-6);run.gold+=24;message="代償を払い、24Gを得た";}
  else if(choice==1){foreach(var item in run.inventory)item.durability=6;message="残響が装備を修復した";}
  else message="黒い靄を振り払い、探索へ戻った";
  ExplorationMapSystem.MarkCleared(exploration,explorationEventNodeId>=0?explorationEventNodeId:exploration.currentNodeId);
  explorationEventActive=false;
  explorationEventNodeId=-1;
  screen=ScreenId.Map;
  SetRoutePresentationMode(RoutePresentationMode.RiteDebug);
 }
 public void UiToggleDeveloperPanel(){
  if(!developerPanel){
   developerReturnScreen=screen;
   developerHasReturn=true;
   developerPanel=true;
  } else {
   developerPanel=false;
   if(developerHasReturn){
    screen=developerReturnScreen;
    developerHasReturn=false;
   }
  }
 }
 public void UiDevCloseWithoutRestore(){developerPanel=false;developerHasReturn=false;}
 public void UiDevOpenOldBattle(){
  if(run==null)run=LoadoutSystem.CreateRun(meta,"old_spire");
  SetRoutePresentationMode(RoutePresentationMode.None);
  StartBattle(false);
  UiDevCloseWithoutRestore();
 }
 public void UiDevOpenExplorationMap(){
  if(run==null)run=LoadoutSystem.CreateRun(meta,"old_spire");
  packingAtBase=false;
  exploration=ExplorationMapSystem.CreateRun(ExplorationMapCatalog.DefaultMapId);
  battle=null;
  explorationEventActive=false;explorationEventNodeId=-1;
  screen=ScreenId.Map;
  message="DEV: 遠征マップ";
  SetRoutePresentationMode(RoutePresentationMode.RiteDebug);
  UiDevCloseWithoutRestore();
 }
 public string UiRoleMilestone(string roleId,bool maximum)=>RoleMilestoneText(roleId,maximum);
 public Texture2D ResolveCharacterPortrait(CharacterDef def){
  if(def!=null&&def.HasPortraitAsset){
   var tex=Resources.Load<Texture2D>(def.portraitResource);
   if(tex!=null)return tex;
  }
  return characterArt;
 }
 public Sprite ResolveCharacterPortraitSprite(CharacterDef def){
  if(def!=null&&def.HasPortraitAsset)
   return Resources.Load<Sprite>(def.portraitResource);
  return null;
 }
 public Texture2D ResolveCharacterPortraitFront(CharacterDef def){
  if(def!=null){
   if(!string.IsNullOrEmpty(def.portraitFrontResource)&&!def.portraitFrontResource.Contains("/DD/")){
    var front=Resources.Load<Texture2D>(def.portraitFrontResource);
    if(front!=null)return front;
   }
   if(!string.IsNullOrEmpty(def.id)){
    var popCutout=Resources.Load<Texture2D>($"Art/Portraits/PopDark/hero-{def.id}-cutout-v1");
    if(popCutout!=null)return popCutout;
    var popFront=Resources.Load<Texture2D>($"Art/Portraits/PopDark/hero-{def.id}-front-v1");
    if(popFront!=null)return popFront;
    var popHub=Resources.Load<Texture2D>($"Art/Portraits/PopDark/hero-{def.id}-hub-v1");
    if(popHub!=null)return popHub;
   }
   var showcase=Resources.Load<Texture2D>("Art/Portraits/PopDark/hero-courier-cutout-v1")
    ??Resources.Load<Texture2D>("Art/Portraits/PopDark/hero-courier-hub-v1")
    ??Resources.Load<Texture2D>("Art/Portraits/hero-courier-hub-v1");
   if(showcase!=null)return showcase;
  }
  return characterArt;
 }
 public Texture2D ResolveCharacterPortraitHub(CharacterDef def){
  if(def!=null){
   if(def.HasHubPortraitAsset&&!def.portraitHubResource.Contains("/DD/")){
    var hub=Resources.Load<Texture2D>(def.portraitHubResource);
    if(hub!=null)return hub;
   }
   return ResolveCharacterPortraitFront(def);
  }
  return characterArt;
 }
 public Texture2D ResolveEnemyPortrait(EnemyDef def){
  if(def!=null&&def.HasPortraitAsset){
   var tex=Resources.Load<Texture2D>(def.portraitResource);
   if(tex!=null)return tex;
  }
  return enemyArt;
 }
 public void UiSelectHeirloom(string uid){if(meta.stash.Any(x=>x.uid==uid)){meta.selectedHeirloomUid=uid;SaveSystem.Save(meta);}}
 public bool UiTemper(string uid){var item=meta.stash.FirstOrDefault(x=>x.uid==uid);if(item==null||item.temper>=5)return false;int price=30*(item.temper+1);if(meta.baseGold<price)return false;meta.baseGold-=price;item.temper++;SaveSystem.Save(meta);return true;}
 public bool UiChangeFaction(string id){if(meta.currentFaction==id)return true;if(!GameCatalog.Factions.Any(x=>x.id==id)||meta.baseGold<20)return false;meta.baseGold-=20;meta.currentFaction=id;SaveSystem.Save(meta);return true;}
 public void UiSelectLoadout(string id){LoadoutSystem.Select(meta,id);SaveSystem.Save(meta);}
 public void UiStartExpedition(string dungeonId){StartRun(dungeonId);}
 public void UiSelectCharacter(string characterId){
  if(!CharacterCatalog.All.ContainsKey(characterId))return;
  meta.selectedCharacterId=characterId;
  var def=CharacterCatalog.Get(characterId);
  meta.body=def.portraitBody;
  meta.hair=def.portraitHair;
 }
 public void UiFinishCharacter(){
  if(string.IsNullOrEmpty(meta.selectedCharacterId)||!CharacterCatalog.All.ContainsKey(meta.selectedCharacterId))
   meta.selectedCharacterId=CharacterCatalog.DefaultId;
  var def=CharacterCatalog.Get(meta.selectedCharacterId);
  meta.body=def.portraitBody;
  meta.hair=def.portraitHair;
  meta.characterMade=true;
  SaveSystem.Save(meta);
  screen=ScreenId.Hub;
 }
 public CharacterDef UiSelectedCharacter=>CharacterSystem.Selected(meta);
 public bool UiUseActiveSkill(){
  if(run==null||battle==null||run.activeSkillUsed)return false;
  var result=CharacterSystem.UseActiveSkill(run,battle);
  if(!result.success)return false;
  PackspireUiFoundation.Instance?.PlayBattleActionFx(result.fx);
  if(result.enemyDefeated){WinBattle();return true;}
  PackspireUiFoundation.Instance?.RefreshBattleUi();
  return true;
 }
 public bool UiPlayBattleCard(int handIndex){
  if(run==null||battle==null)return false;
  var fx=BattleSystem.PlayCard(run,battle,handIndex);
  if(!fx.ok)return false;
  PackspireUiFoundation.Instance?.PlayBattleActionFx(fx);
  if(fx.enemyDefeated){WinBattle();return true;}
  PackspireUiFoundation.Instance?.RefreshBattleUi();
  return true;
 }
 public bool UiEndBattleTurn(){
  if(run==null||battle==null)return false;
  var dungeon=GameCatalog.Dungeons.First(x=>x.id==run.dungeon);
  var fx=BattleSystem.EndTurnFx(run,battle,dungeon.damage);
  PackspireUiFoundation.Instance?.PlayBattleActionFx(fx);
  if(fx.playerDefeated){FinishRun(false);return true;}
  PackspireUiFoundation.Instance?.RefreshBattleUi();
  return true;
 }
 public bool UiUseBattleConsumable(int index){
  if(run==null||battle==null)return false;
  var fx=ConsumableSystem.UseFx(run,battle,index);
  if(!fx.ok)return false;
  PackspireUiFoundation.Instance?.PlayBattleActionFx(fx);
  if(fx.enemyDefeated){WinBattle();return true;}
  PackspireUiFoundation.Instance?.RefreshBattleUi();
  return true;
 }
 public bool UiActiveSkillAvailable=>run!=null&&battle!=null&&!run.activeSkillUsed;
 public string UiActiveSkillLabel=>CharacterSystem.OfRun(run)?.activeSkillName??"スキル";
 public string UiActiveSkillTooltip=>CharacterSystem.ActiveSkillTooltip(run);
 public void UiOpenPackingLoadout(string id){LoadoutSystem.Select(meta,id);OpenPacking();}
 public void UiPackingCreateLoadout(){
  meta.loadouts??=new();
  int n=meta.loadouts.Count+1;
  string id;
  do{id=$"loadout-{n}";n++;}while(meta.loadouts.Any(x=>x.id==id));
  var loadout=new LoadoutSave{id=id,name="新規術式",backpack="standard"};
  LoadoutSystem.EnsureFormulaIds(loadout);
  meta.loadouts.Add(loadout);
  LoadoutSystem.Select(meta,loadout.id);
  SaveSystem.Save(meta);
  OpenPacking();
 }
 public void UiPackingRenameLoadout(string name){
  var loadout=meta?.loadouts?.FirstOrDefault(x=>x.id==meta.selectedLoadoutId);
  if(loadout==null)return;
  name=(name??"").Trim();
  if(string.IsNullOrEmpty(name))name="無名の術式";
  if(name.Length>20)name=name.Substring(0,20);
  if(loadout.name==name)return;
  loadout.name=name;
 }
 public bool UiPackingPlace(string uid,int anchor,int rotation){if(run==null)return false;var item=run.inventory.FirstOrDefault(x=>x.uid==uid);if(item==null||!BackpackSystem.CanPlace(run,item,anchor,rotation,uid))return false;var old=run.placements.FirstOrDefault(x=>x.itemUid==uid);if(old!=null)run.placements.Remove(old);run.placements.Add(new Placement(uid,anchor,rotation));return true;}
 public void UiPackingRemove(string uid){if(run==null)return;var placement=run.placements.FirstOrDefault(x=>x.itemUid==uid);if(placement!=null)run.placements.Remove(placement);}
 public void UiPackingSetBackpack(string id){UiPackingSetCore(id);}
 public void UiPackingSetCore(string id){
  if(run==null||!StorageFormulaCatalog.Cores.ContainsKey(id))return;
  run.coreId=id;
  run.backpack=id;
 }
 public void UiPackingSetConduit(string id){if(run==null||!StorageFormulaCatalog.Conduits.ContainsKey(id))return;run.conduitId=id;}
 public void UiPackingSetResonance(string id){if(run==null||!StorageFormulaCatalog.Resonances.ContainsKey(id))return;run.resonanceId=id;}
 public void UiPackingSetStability(string id){if(run==null||!StorageFormulaCatalog.Stabilities.ContainsKey(id))return;run.stabilityId=id;}
 public void UiPackingToggleCard(string slot){if(run==null)return;if(run.selectedCardSlots.Contains(slot))run.selectedCardSlots.Remove(slot);else run.selectedCardSlots.Add(slot);}
 public void UiPackingCapture(){
  if(run==null)return;
  LoadoutSystem.Capture(meta,run);
  meta.selectedBackpack=run.coreId;
  SaveSystem.Save(meta);
 }
 public void UiPackingSave(){
  UiPackingCapture();
  if(!packingAtBase){
   screen=ScreenId.Map;
   if(exploration!=null)SetRoutePresentationMode(RoutePresentationMode.RiteDebug);
  }
 }
 public void UiTakeReward(string itemId){
  if(run==null||!GameCatalog.Items.ContainsKey(itemId))return;
  var loot=new ItemInstance(itemId){identified=false};
  StorageFormulaSystem.EnsureItemRolled(loot);
  run.lootBag.Add(loot);
  rewardSelectionId="";
  screen=ScreenId.Map;
  if(exploration!=null)SetRoutePresentationMode(RoutePresentationMode.RiteDebug);
 }
 public bool UiBuy(string itemId){if(run==null||!GameCatalog.Items.TryGetValue(itemId,out var item))return false;int price=14+item.cells.Length*4;if(run.gold<price)return false;run.gold-=price;var loot=new ItemInstance(itemId){identified=false};StorageFormulaSystem.EnsureItemRolled(loot);run.lootBag.Add(loot);message=$"購入完了：{item.name}　残金 {run.gold}G";return true;}
 public void UiReturnToMap(){
  screen=ScreenId.Map;
  if(exploration!=null)SetRoutePresentationMode(RoutePresentationMode.RiteDebug);
 }
 public void UiResolveEvent(int choice){if(run==null)return;if(choice==0){run.hp=Mathf.Max(1,run.hp-6);run.gold+=24;}else if(choice==1)foreach(var item in run.inventory)item.durability=6;screen=ScreenId.Map;}
 public void UiReturnToHub(){
  run=null;exploration=null;explorationEventActive=false;explorationEventNodeId=-1;
  SetRoutePresentationMode(RoutePresentationMode.None);screen=ScreenId.Hub;
 }
 void OpenPacking(){run=LoadoutSystem.CreateRun(meta,"");packingAtBase=true;selectedUid="";message="荷造りセットを編集";screen=ScreenId.Pack;}
 void StartRun(string dungeon){try{message="ダンジョンを生成中…";run=LoadoutSystem.CreateRun(meta,dungeon);packingAtBase=false;exploration=ExplorationMapSystem.CreateRun(ExplorationMapCatalog.DefaultMapId);selectedUid="";explorationEventActive=false;explorationEventNodeId=-1;message=$"{ExplorationMapSystem.Def(exploration)?.name??"探索"}へ進入";screen=ScreenId.Map;SetRoutePresentationMode(RoutePresentationMode.RiteDebug);}catch(Exception ex){run=null;exploration=null;screen=ScreenId.Expedition;message="遠征開始エラー："+ex.Message;Debug.LogException(ex);}}
 void StartBattle(bool boss){
  if(run==null)return;
  CharacterSystem.SyncRunCharacter(meta,run);
  var dungeon=GameCatalog.Dungeons.First(x=>x.id==run.dungeon);
  EnemyDef enemy;
  if(LockBattleShowcaseArt){
   enemy=GameCatalog.Enemies.FirstOrDefault(x=>x.id=="dragon")
    ??GameCatalog.Enemies.First(x=>x.tier==(boss?3:Mathf.Min(2,1+run.battlesWon/3)));
  } else {
   var pool=GameCatalog.Enemies.Where(x=>boss?x.tier==3:x.tier==Mathf.Min(2,1+run.battlesWon/3)).ToArray();
   enemy=pool[UnityEngine.Random.Range(0,pool.Length)];
  }
  battle=BattleSystem.Begin(run,enemy,dungeon.hpScale);
  screen=ScreenId.Battle;
  SetRoutePresentationMode(RoutePresentationMode.None);
 }
 void WinBattle(){
  int goldBonus=CharacterSystem.WinGoldBonus(run);
  run.battlesWon++;run.gold+=12+run.battlesWon*3+goldBonus;run.hp=Mathf.Min(run.maxHp,run.hp+3);
  if(goldBonus>0)message=$"勝利　+{goldBonus}G（{CharacterSystem.OfRun(run).traitName}）";
  if(battle!=null&&battle.enemy.tier==3){FinishRun(true);return;}
  if(exploration!=null)ExplorationMapSystem.MarkCleared(exploration,exploration.currentNodeId);
  screen=ScreenId.Reward;
  if(exploration!=null)SetRoutePresentationMode(RoutePresentationMode.RiteDebug);
 }
 void FinishRun(bool win){
  if(run!=null){
   if(win){
    meta.wins++;meta.baseGold+=run.gold;
    foreach(var item in run.inventory.Concat(run.lootBag)){
     var saved=meta.stash.FirstOrDefault(x=>x.uid==item.uid);
     if(saved==null)meta.stash.Add(CloneItem(item));
     else{saved.durability=item.durability;saved.uses=item.uses;saved.temper=item.temper;saved.scars=item.scars;saved.history=item.history;}
    }
   } else {
    var heir=run.inventory.FirstOrDefault(x=>x.uid==run.heirloomUid);
    if(heir!=null){
     heir.history.defeats++;
     heir.scars.Add(new ScarRecord{type="defeat",dungeon=run.dungeon,floor=run.battlesWon,timestamp=DateTimeOffset.UtcNow.ToUnixTimeSeconds()});
     var saved=meta.stash.FirstOrDefault(x=>x.uid==heir.uid);
     if(saved!=null){saved.scars=heir.scars;saved.history=heir.history;}
    }
   }
  }
  meta.runs++;SaveSystem.Save(meta);
  battle=null;
  exploration=null;explorationEventActive=false;explorationEventNodeId=-1;
  SetRoutePresentationMode(RoutePresentationMode.None);
  message=win?"遠征成功。戦利品をすべて保管しました":"探索終了。戦利品と獲得ゴールドは持ち帰れません";
  screen=win?ScreenId.GameClear:ScreenId.GameOver;
 }
 string RoleMilestoneText(string id,bool maximum){if(id.Contains("guardian")||id.Contains("bulwark")||id.Contains("knight"))return maximum?"戦闘開始時に防御を得て、余剰防御を次のターンへ一部持ち越す。":"防御カードを連続使用すると次の防御効果が上昇する。";if(id.Contains("scout")||id.Contains("hunter")||id.Contains("blade")||id.Contains("dancer"))return maximum?"各戦闘で最初に使う0コストカードを複製する。":"異なる装備由来のカードを続けて使うと追加ドロー。";if(id.Contains("artificer")||id.Contains("rune")||id.Contains("channeler"))return maximum?"戦闘中に最初に使うルーン・道具カードの耐久を消費しない。":"属性一致が3色以上なら戦闘開始時にエネルギーを得る。";return maximum?"武器カードを一定回数使うたび、ラン中の攻撃力が成長する。":"同じ武器由来のカードを続けて使うと追加ダメージ。";}
 ItemInstance CloneItem(ItemInstance x)=>JsonUtility.FromJson<ItemInstance>(JsonUtility.ToJson(x));
}
}
