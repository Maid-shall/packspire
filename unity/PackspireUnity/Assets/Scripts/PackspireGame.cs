using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Packspire {
public partial class PackspireGame : MonoBehaviour {


 public static PackspireGame Instance { get; private set; }
 MetaSave meta; RunState run; GridBoardRunState gridBoard; BattleState battle; ScreenId screen; string message=""; bool packingAtBase,developerPanel; Texture2D factionArt,characterArt,equipmentArt,roleArt,enemyArt,dungeonArt,bookSpread;
 Texture2D showcaseHeroArt,showcaseDragonArt;
 Sprite showcaseHeroSprite;
 ScreenId lastVisualScreen; bool visualScreenTracked;
 /// <summary>Temporary art-preview lock: battle always shows 瀬名 + 劫火竜 portraits.</summary>
 public static readonly bool LockBattleShowcaseArt=true;
 public ScreenId UiScreen=>screen; public MetaSave UiMeta=>meta; public bool UiDeveloperPanelOpen=>developerPanel; public Texture2D UiCharacterArt=>characterArt; public Texture2D UiEquipmentArt=>equipmentArt; public Texture2D UiRoleArt=>roleArt; public Texture2D UiEnemyArt=>enemyArt; public Texture2D UiDungeonArt=>dungeonArt; public Texture2D UiFactionArt=>factionArt; public Texture2D UiBookArt=>bookSpread;
 public Texture2D UiShowcaseHeroArt=>showcaseHeroArt; public Sprite UiShowcaseHeroSprite=>showcaseHeroSprite; public Texture2D UiShowcaseDragonArt=>showcaseDragonArt;
 public RunState UiRun=>run; public string UiMessage=>message; public bool UiPackingAtBase=>packingAtBase;
 public GridBoardRunState UiGridBoard=>gridBoard;
 public bool UiUsesGridBoard=>gridBoard!=null;
 public BattleState UiBattle=>battle;
 public EventContent UiCurrentEvent{
  get{
   string id=gridBoard?.pendingEventId;
   if(string.IsNullOrEmpty(id))id=PackspireContent.Data.balance.defaultEventId;
   return PackspireContent.Data.events.FirstOrDefault(x=>x.id==id);
  }
 }
 public ScreenId UiDeveloperReturnScreen=>developerReturnScreen;
 ScreenId developerReturnScreen; bool developerHasReturn;

 void Awake(){
  if(Instance!=null&&Instance!=this){Destroy(gameObject);return;}
  Instance=this;
  DontDestroyOnLoad(gameObject);
  meta=SaveSystem.Load();
  factionArt=PackspireResources.Load<Texture2D>("Art/faction-hub-sheet");
  characterArt=PackspireResources.Load<Texture2D>("Art/character-creator-sheet");
  equipmentArt=PackspireResources.Load<Texture2D>("Art/equipment-sheet");
  roleArt=PackspireResources.Load<Texture2D>("Art/roles-sheet");
  enemyArt=PackspireResources.Load<Texture2D>("Art/enemy-sheet");
  dungeonArt=PackspireResources.Load<Texture2D>("Art/dungeon-sheet");
  showcaseHeroSprite=PackspireResources.Load<Sprite>("Art/Portraits/hero-sena-kick-v1");
  if(showcaseHeroSprite==null)
   showcaseHeroArt=PackspireResources.Load<Texture2D>("Art/Portraits/hero-sena-kick-v1");
  showcaseDragonArt=PackspireResources.Load<Texture2D>("Art/Portraits/enemy-dragon-v1");
  screen=meta.characterMade?ScreenId.Hub:ScreenId.Character;
  Application.targetFrameRate=60;
 }
 void OnDestroy(){if(Instance==this)Instance=null;}
 void Update(){
  if(PackspireInput.DeveloperTogglePressed())UiToggleDeveloperPanel();
  if(!visualScreenTracked){lastVisualScreen=screen;visualScreenTracked=true;return;}
  if(lastVisualScreen==screen)return;
  var previous=lastVisualScreen;
  lastVisualScreen=screen;
  var ui=PackspireUiFoundation.Instance;
  if(ui!=null)ui.PlayFor(previous,screen);
 }
 public void UiNavigate(ScreenId target){
  if(target==ScreenId.Pack)OpenPacking();
  else{
  // Also clear grid board when returning to hub meta screens
   if(target==ScreenId.Hub||target==ScreenId.Status||target==ScreenId.Vault||target==ScreenId.Heirloom||target==ScreenId.Faction||target==ScreenId.Expedition||target==ScreenId.Compendium||target==ScreenId.Character){
    run=null;gridBoard=null;
   }
   screen=target;
  }
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
  StartBattle(false);
  UiDevCloseWithoutRestore();
 }
 public void UiDevOpenGridBoard(){
  if(run==null)run=LoadoutSystem.CreateRun(meta,"old_spire");
  packingAtBase=false;
  battle=null;
  gridBoard=GridBoardSystem.Create(run.dungeon);
  GridBoardSystem.SyncExplorePool(gridBoard,run);
  screen=ScreenId.GridBoard;
  message="DEV: 封印格子盤（配置→一筆→進行）";
  UiDevCloseWithoutRestore();
 }
 public void UiResetGridBoard(){
  if(run==null)return;
  battle=null;
  gridBoard=GridBoardSystem.Create(run.dungeon);
  GridBoardSystem.SyncExplorePool(gridBoard,run);
  screen=ScreenId.GridBoard;
  message="封印格子をやり直した";
 }
 public void UiFinishExpedition(bool win=true)=>FinishRun(win);
 public void UiRetreatFromGrid(){
  message="途中撤退した。戦利品は持ち帰れる";
  FinishRun(true);
 }
 public void UiAdvanceGridArea(){
  if(gridBoard==null)return;
  if(!GridBoardSystem.TryAdvanceArea(gridBoard,out var msg)){message=msg;return;}
  message=msg;
  var ui=PackspireUiFoundation.Instance;
  if(ui!=null)ui.ForceRefreshScreen();
 }
 public void UiConfirmGridReturn(){
  if(gridBoard==null)return;
  gridBoard.pendingGate="";
  message=$"区画 {gridBoard.areaIndex+1} の帰還点から持ち帰った";
  FinishRun(true);
 }
 public void UiDeclineGridGate(){
  if(gridBoard==null)return;
  GridBoardSystem.DeclineGate(gridBoard);
 }
 public string UiRoleMilestone(string roleId,bool maximum)=>RoleMilestoneText(roleId,maximum);
 public Texture2D ResolveCharacterPortrait(CharacterDef def){
  if(def!=null&&def.HasPortraitAsset){
   if(def.portraitAsset!=null)return def.portraitAsset.texture;
   var tex=PackspireResources.Load<Texture2D>(def.portraitResource);
   if(tex!=null)return tex;
  }
  return characterArt;
 }
 public Sprite ResolveCharacterPortraitSprite(CharacterDef def){
  if(def!=null&&def.HasPortraitAsset){
   if(def.portraitAsset!=null)return def.portraitAsset;
   return PackspireResources.Load<Sprite>(def.portraitResource);
  }
  return null;
 }
 public Texture2D ResolveCharacterPortraitFront(CharacterDef def){
  if(def!=null){
   if(def.portraitFrontAsset!=null)return def.portraitFrontAsset.texture;
   if(!string.IsNullOrEmpty(def.portraitFrontResource)&&!def.portraitFrontResource.Contains("/DD/")){
    var front=PackspireResources.Load<Texture2D>(def.portraitFrontResource);
    if(front!=null)return front;
   }
   if(!string.IsNullOrEmpty(def.id)){
    var popCutout=PackspireResources.Load<Texture2D>($"Art/Portraits/PopDark/hero-{def.id}-cutout-v1");
    if(popCutout!=null)return popCutout;
    var popFront=PackspireResources.Load<Texture2D>($"Art/Portraits/PopDark/hero-{def.id}-front-v1");
    if(popFront!=null)return popFront;
    var popHub=PackspireResources.Load<Texture2D>($"Art/Portraits/PopDark/hero-{def.id}-hub-v1");
    if(popHub!=null)return popHub;
   }
   var showcase=PackspireResources.LoadFirst<Texture2D>(
    "Art/Portraits/PopDark/hero-courier-cutout-v1",
    "Art/Portraits/PopDark/hero-courier-hub-v1",
    "Art/Portraits/hero-courier-hub-v1");
   if(showcase!=null)return showcase;
  }
  return characterArt;
 }
 public Texture2D ResolveCharacterPortraitHub(CharacterDef def){
  if(def!=null){
   if(def.portraitHubAsset!=null)return def.portraitHubAsset.texture;
   if(def.HasHubPortraitAsset&&!def.portraitHubResource.Contains("/DD/")){
    var hub=PackspireResources.Load<Texture2D>(def.portraitHubResource);
    if(hub!=null)return hub;
   }
   return ResolveCharacterPortraitFront(def);
  }
  return characterArt;
 }
 public Texture2D ResolveEnemyPortrait(EnemyDef def){
  if(def!=null&&def.HasPortraitAsset){
   if(def.portraitAsset!=null)return def.portraitAsset.texture;
   var tex=PackspireResources.Load<Texture2D>(def.portraitResource);
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
  var ui=PackspireUiFoundation.Instance;
  if(ui!=null)ui.PlayBattleActionFx(result.fx);
  if(result.enemyDefeated){WinBattle();return true;}
  if(ui!=null)ui.RefreshBattleUi();
  return true;
 }
 public bool UiPlayBattleCard(int handIndex){
  if(run==null||battle==null)return false;
  var fx=BattleSystem.PlayCard(run,battle,handIndex);
  if(!fx.ok)return false;
  var ui=PackspireUiFoundation.Instance;
  if(ui!=null)ui.PlayBattleActionFx(fx);
  if(fx.enemyDefeated){WinBattle();return true;}
  if(ui!=null)ui.RefreshBattleUi();
  return true;
 }
 public bool UiEndBattleTurn(){
  if(run==null||battle==null)return false;
  var dungeon=GameCatalog.Dungeons.First(x=>x.id==run.dungeon);
  var gridPressure=GridBoardSystem.EnemyDamageBonus(gridBoard);
  var fx=BattleSystem.EndTurnFx(run,battle,dungeon.damage+gridPressure);
  var ui=PackspireUiFoundation.Instance;
  if(ui!=null)ui.PlayBattleActionFx(fx);
  if(fx.playerDefeated){FinishRun(false);return true;}
  if(ui!=null)ui.RefreshBattleUi();
  return true;
 }
 public bool UiUseBattleConsumable(int index){
  if(run==null||battle==null)return false;
  var fx=ConsumableSystem.UseFx(run,battle,index);
  if(!fx.ok)return false;
  var ui=PackspireUiFoundation.Instance;
  if(ui!=null)ui.PlayBattleActionFx(fx);
  if(fx.enemyDefeated){WinBattle();return true;}
  if(ui!=null)ui.RefreshBattleUi();
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
  if(!packingAtBase)ReturnToExpeditionScreen();
 }
 public void UiTakeReward(string itemId){
  if(run==null||!GameCatalog.Items.ContainsKey(itemId))return;
  var loot=new ItemInstance(itemId){identified=false};
  StorageFormulaSystem.EnsureItemRolled(loot);
  run.lootBag.Add(loot);
  ReturnToExpeditionScreen();
 }
 public bool UiBuy(string itemId){if(run==null||!GameCatalog.Items.TryGetValue(itemId,out var item))return false;int price=14+item.cells.Length*4;if(run.gold<price)return false;run.gold-=price;var loot=new ItemInstance(itemId){identified=false};StorageFormulaSystem.EnsureItemRolled(loot);run.lootBag.Add(loot);message=$"購入完了：{item.name}　残金 {run.gold}G";return true;}
 public void UiReturnToMap()=>ReturnToExpeditionScreen();
 public void UiResolveEvent(int choice){
  if(run==null)return;
  var current=UiCurrentEvent;
  if(current==null||choice<0||choice>=current.choices.Length){
   message="異変は静かに消えた";
  } else {
   var selected=current.choices[choice];
   foreach(var effect in selected.effects??Array.Empty<EventEffectContent>()){
    switch(effect.effect){
     case EventEffectType.Hp:
      run.hp=effect.amount<0?Mathf.Max(1,run.hp+effect.amount):Mathf.Min(run.maxHp,run.hp+effect.amount);
      break;
     case EventEffectType.Gold:
      run.gold=Mathf.Max(0,run.gold+effect.amount);
      break;
     case EventEffectType.RepairAll:
      foreach(var item in run.inventory)item.durability=Mathf.Max(item.durability,effect.amount);
      break;
    }
   }
   message=selected.resultText;
  }
  if(gridBoard!=null)gridBoard.pendingEventId="";
  ReturnToExpeditionScreen();
 }
 public void UiBeginGridEvent(){
  if(run==null||gridBoard==null)return;
  message="記憶の揺らぎに触れた";
  screen=ScreenId.Event;
 }
 public void UiBeginGridEncounter(){
  if(run==null||gridBoard==null||battle!=null)return;
  StartBattle(false);
 }
 public void UiReturnToHub(){
  run=null;gridBoard=null;screen=ScreenId.Hub;
 }
 void ReturnToExpeditionScreen(){
  screen=gridBoard!=null?ScreenId.GridBoard:ScreenId.Expedition;
 }
 void OpenPacking(){run=LoadoutSystem.CreateRun(meta,"");packingAtBase=true;message="荷造りセットを編集";screen=ScreenId.Pack;}
 void StartRun(string dungeon){
  try{
   message="ダンジョンを生成中…";
   run=LoadoutSystem.CreateRun(meta,dungeon);
   packingAtBase=false;
   battle=null;
   gridBoard=GridBoardSystem.Create(run.dungeon);
   GridBoardSystem.SyncExplorePool(gridBoard,run);
   message="封印格子を展開した";
   screen=ScreenId.GridBoard;
  }catch(Exception ex){
   run=null;gridBoard=null;screen=ScreenId.Expedition;
   message="遠征開始エラー："+ex.Message;
   Debug.LogException(ex);
  }
 }
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
  float pressureScale=GridBoardSystem.EnemyHpMultiplier(gridBoard);
  battle=BattleSystem.Begin(run,enemy,dungeon.hpScale*pressureScale);
  // Same-screen combat when on the seal grid; legacy full battle screen otherwise.
  screen=gridBoard!=null?ScreenId.GridBoard:ScreenId.Battle;
 }
 void WinBattle(){
  int goldBonus=CharacterSystem.WinGoldBonus(run);
  run.battlesWon++;run.gold+=12+run.battlesWon*3+goldBonus;run.hp=Mathf.Min(run.maxHp,run.hp+3);
  if(goldBonus>0)message=$"勝利　+{goldBonus}G（{CharacterSystem.OfRun(run).traitName}）";
  bool boss=battle!=null&&battle.enemy.tier==3;
  battle=null;
  if(boss){FinishRun(true);return;}
  screen=ScreenId.Reward;
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
  gridBoard=null;
  message=win?"遠征成功。戦利品をすべて保管しました":"探索終了。戦利品と獲得ゴールドは持ち帰れません";
  screen=win?ScreenId.GameClear:ScreenId.GameOver;
 }
 string RoleMilestoneText(string id,bool maximum){
  if(!GameCatalog.Roles.TryGetValue(id,out var role))return "";
  return maximum?role.maximumMilestoneText:role.milestoneText;
 }
 ItemInstance CloneItem(ItemInstance x)=>JsonUtility.FromJson<ItemInstance>(JsonUtility.ToJson(x));
}
}
