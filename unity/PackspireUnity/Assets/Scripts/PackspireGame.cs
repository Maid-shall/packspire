using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Packspire {
public partial class PackspireGame : MonoBehaviour {


 public static PackspireGame Instance { get; private set; }
 MetaSave meta; RunState run; DungeonMap map; ExplorationRunState exploration; BattleState battle; ScreenId screen; Vector2 scroll; string selectedUid="",message="",hubHover="",selectedRoleId="",selectedFactionId="",selectedDungeonId="",selectedCompendiumId=""; int selectedRotation,compendiumTab,packingDetailTab,selectedMapNodeId=-1,mapEventNodeId=-1,mapLoadoutTab; bool packingAtBase,illustratedStylesApplied,mapEventOverlay,mapLoadoutOverlay,developerPanel; GUIStyle title,header,body,button,card,hotspot,hubLabel,hubLabelHover,topChip,cellButton,navButton,navSelected,screenTitle,screenSubtitle,badge,centerBody,bookEntryStyle,bookEntryHoverStyle,bookEntrySelectedStyle; Texture2D factionArt,hubArt,characterArt,equipmentArt,roleArt,enemyArt,dungeonArt,menuBackdrop,panelTex,buttonTex,hoverTex,cellClearTex,navTex,navSelectedTex,badgeTex,homeTileTex,homeTileHoverTex,homePrimaryTex,homePrimaryHoverTex,uiActionNormal,uiActionHover,uiActionDanger,uiBackCard,uiNavNormal,uiNavSelected,uiTabCard,uiChipCard,minimalButtonTex,minimalHoverTex,infoChipTex,homeBarPrimary,homeBarSecondary,mapTerrain,mapRoad,bookSpread,bookClearTex,bookHoverTex,bookSelectedTex,bookChipTex,scrollMapUi,battleTableUi,backpackLeatherTex,backpackPocketTex,mapNodeCircleTex; Texture2D[] homeCardArt,navIconArt,combatCardFrames; Font uiFont,titleFont,gameFont;
 readonly Color bg=new(.09f,.12f,.095f),panel=new(.16f,.20f,.165f),gold=new(.94f,.75f,.35f),ink=new(.97f,.95f,.88f);
 ScreenId lastVisualScreen; bool visualScreenTracked;
 Texture2D bookButtonTex,bookButtonHoverTex,bookButtonSelectedTex,bookRuleTex,bookScrollTrackTex,bookScrollThumbTex,battleMeterTrackTex,battleHpTex,battleBlockTex,battleStatusBuffTex,battleStatusDebuffTex;
 public ScreenId UiScreen=>screen; public MetaSave UiMeta=>meta; public bool UiDeveloperPanelOpen=>developerPanel; public Texture2D UiHubArt=>hubArt; public Texture2D UiCharacterArt=>characterArt; public Texture2D UiEquipmentArt=>equipmentArt; public Texture2D UiRoleArt=>roleArt; public Texture2D UiEnemyArt=>enemyArt; public Texture2D UiDungeonArt=>dungeonArt; public Texture2D UiFactionArt=>factionArt; public Texture2D UiBookArt=>bookSpread;
 public RunState UiRun=>run; public string UiMessage=>message; public bool UiPackingAtBase=>packingAtBase;
 public ExplorationRunState UiExploration=>exploration;
 public bool UiUsesExplorationMap=>exploration!=null;
 public bool UiExplorationEventActive=>explorationEventActive;
 public int UiExplorationEventNodeId=>explorationEventNodeId;
 public BattleState UiBattle=>battle;
 public bool UiRouteBattleActive=>routeBattleActive;
 public bool UiRouteRewardPending=>routeRewardPending;
 public ScreenId UiDeveloperReturnScreen=>developerReturnScreen;
 public RoutePresentationMode CurrentRoutePresentationMode=>routePresentationMode;
 public bool UsesRoutePresentation=>
  routePresentationMode is RoutePresentationMode.RouteExploration
   or RoutePresentationMode.RouteTransition
   or RoutePresentationMode.RouteCombat
   or RoutePresentationMode.RouteReward
   or RoutePresentationMode.RouteEvent;
 public bool ShouldDrawLegacyOnGui{
  get{
   if(screen==ScreenId.Battle)return true;
   if(UsesRoutePresentation)return false;
   if(routePresentationMode==RoutePresentationMode.RiteDebug)return false;
   if(screen==ScreenId.Map&&(routePresentationMode==RoutePresentationMode.LegacyMap||exploration==null))return true;
   return false;
  }
 }
 string rewardSelectionId="",shopSelectionId="";
 bool explorationEventActive; int explorationEventNodeId=-1;
 bool routeBattleActive,routeRewardPending;
 RoutePresentationMode routePresentationMode;
 ScreenId developerReturnScreen; bool developerHasReturn;
 public bool UiDevPreferRiteView;

 public void SetRoutePresentationMode(RoutePresentationMode mode){routePresentationMode=mode;}
 public void UiSyncRoutePresentationMode(){
  if(routePresentationMode==RoutePresentationMode.LegacyMap)return;
  if(screen==ScreenId.Battle&&exploration!=null)return;
  if(routePresentationMode==RoutePresentationMode.RiteDebug&&exploration!=null)return;
  if(exploration==null){routePresentationMode=RoutePresentationMode.None;return;}
  if(routeRewardPending){routePresentationMode=RoutePresentationMode.RouteReward;return;}
  if(routeBattleActive){routePresentationMode=RoutePresentationMode.RouteCombat;return;}
  if(explorationEventActive){routePresentationMode=RoutePresentationMode.RouteEvent;return;}
  if(PackspireUiFoundation.Instance!=null&&PackspireUiFoundation.Instance.IsRouteTransitioning)
   routePresentationMode=RoutePresentationMode.RouteTransition;
  else if(routePresentationMode!=RoutePresentationMode.RiteDebug)
   routePresentationMode=RoutePresentationMode.RouteExploration;
 }
 void Awake(){if(Instance!=null&&Instance!=this){Destroy(gameObject);return;}Instance=this;DontDestroyOnLoad(gameObject);meta=SaveSystem.Load();factionArt=Resources.Load<Texture2D>("Art/faction-hub-sheet");hubArt=Resources.Load<Texture2D>("Art/UI/hub-courtyard-v1");menuBackdrop=Resources.Load<Texture2D>("Art/UI/fantasy-menu-backdrop-v1");characterArt=Resources.Load<Texture2D>("Art/character-creator-sheet");equipmentArt=Resources.Load<Texture2D>("Art/equipment-sheet");roleArt=Resources.Load<Texture2D>("Art/roles-sheet");enemyArt=Resources.Load<Texture2D>("Art/enemy-sheet");dungeonArt=Resources.Load<Texture2D>("Art/dungeon-sheet");uiFont=Resources.Load<Font>("Fonts/KleeOne-Regular");titleFont=Resources.Load<Font>("Fonts/KleeOne-SemiBold");screen=meta.characterMade?ScreenId.Hub:ScreenId.Character;Application.targetFrameRate=60;}
 void OnDestroy(){if(Instance==this)Instance=null;}
 void Update(){if(Input.GetKeyDown(KeyCode.F10))UiToggleDeveloperPanel();if(!visualScreenTracked){lastVisualScreen=screen;visualScreenTracked=true;return;}if(lastVisualScreen==screen)return;var previous=lastVisualScreen;lastVisualScreen=screen;PackspireUiFoundation.Instance?.PlayFor(previous,screen);}
 public void UiNavigate(ScreenId target){
  mapEventOverlay=false;mapLoadoutOverlay=false;explorationEventActive=false;explorationEventNodeId=-1;
  AbortRouteBattle();
  PackspireUiFoundation.Instance?.ResetRouteEncounterPresentation();
  if(target==ScreenId.Pack)OpenPacking();
  else{
   if(target==ScreenId.Hub||target==ScreenId.Status||target==ScreenId.Vault||target==ScreenId.Faction||target==ScreenId.Expedition||target==ScreenId.Compendium){
    run=null;exploration=null;SetRoutePresentationMode(RoutePresentationMode.None);
   }
   screen=target;scroll=Vector2.zero;
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
   SetRoutePresentationMode(RoutePresentationMode.RouteEvent);
  } else if(encounter==ExplorationEncounter.Battle){
   StartBattle(false,preferRoutePresentation:false);
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
 public void UiOpenExplorationMap(){
  if(run==null)run=LoadoutSystem.CreateRun(meta,"old_spire");
  packingAtBase=false;
  exploration=ExplorationMapSystem.CreateRun(ExplorationRouteCatalog.SliceMapId);
  map=null;
  selectedMapNodeId=-1;
  explorationEventActive=false;
  explorationEventNodeId=-1;
  routeBattleActive=false;routeRewardPending=false;
  message="外郭ルート試作を開始";
  screen=ScreenId.Map;
  scroll=Vector2.zero;
  SetRoutePresentationMode(RoutePresentationMode.RouteExploration);
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
 /// <summary>Move to a cell only — does not unlock edges or mark the whole slice visited.</summary>
 public void UiDevJumpExplorationCell(int cellId,bool enterInterior=false){
  EnsureRouteSliceRun(fresh:false);
  AbortRouteBattle();
  PackspireUiFoundation.Instance?.ResetRouteEncounterPresentation();
  map=null;
  explorationEventActive=false;explorationEventNodeId=-1;
  var outdoor=ExplorationMapSystem.OutdoorDef(exploration);
  if(enterInterior){
   var door=ExplorationMapSystem.Node(outdoor,cellId);
   if(door!=null&&!string.IsNullOrEmpty(door.interiorMapId))
    ExplorationMapSystem.EnterInterior(exploration,door.interiorMapId,cellId);
   else DevPlaceAt(cellId);
  } else {
   if(outdoor!=null&&exploration.activeMapId!=outdoor.id)ExplorationMapSystem.ExitInterior(exploration);
   DevPlaceAt(cellId);
  }
  message=$"DEV: 地点 {cellId}";
  screen=ScreenId.Map;
  SetRoutePresentationMode(RoutePresentationMode.RouteExploration);
  UiDevCloseWithoutRestore();
 }
 public void UiDevFreshRouteSlice(){
  EnsureRouteSliceRun(fresh:true);
  message="DEV: 新規ルート試作を開始";
  screen=ScreenId.Map;
  SetRoutePresentationMode(RoutePresentationMode.RouteExploration);
  UiDevCloseWithoutRestore();
 }
 public void UiDevOpenBreach(){
  EnsureRouteSliceRun(fresh:false);
  ExplorationMapSystem.TryUnlockEdge(exploration,ExplorationRouteCatalog.CellBreachFrom,ExplorationRouteCatalog.CellBreachTo);
  message="DEV: breachを開通";
  screen=ScreenId.Map;
  SetRoutePresentationMode(RoutePresentationMode.RouteExploration);
  UiDevCloseWithoutRestore();
 }
 public void UiDevRevealHidden(){
  EnsureRouteSliceRun(fresh:false);
  ExplorationMapSystem.RevealHiddenEdge(exploration,ExplorationRouteCatalog.CellHiddenFrom,ExplorationRouteCatalog.CellHiddenTo);
  string nk=ExplorationMapSystem.NodeKey(exploration.activeMapId,ExplorationRouteCatalog.CellHiddenTo);
  if(!exploration.revealed.Contains(nk))exploration.revealed.Add(nk);
  message="DEV: hiddenを発見";
  screen=ScreenId.Map;
  SetRoutePresentationMode(RoutePresentationMode.RouteExploration);
  UiDevCloseWithoutRestore();
 }
 public void UiDevOpenOldBattle(){
  if(run==null)run=LoadoutSystem.CreateRun(meta,"old_spire");
  routeBattleActive=false;routeRewardPending=false;
  SetRoutePresentationMode(RoutePresentationMode.None);
  StartBattle(false,preferRoutePresentation:false);
  UiDevCloseWithoutRestore();
 }
 public void UiDevOpenRouteBattle(){
  EnsureRouteSliceRun(fresh:false);
  DevPlaceAt(ExplorationRouteCatalog.CellBattle);
  BeginRouteBattle(false);
  UiDevCloseWithoutRestore();
 }
 public void UiDevOpenOldMap(){
  if(run==null)run=LoadoutSystem.CreateRun(meta,"old_spire");
  packingAtBase=false;routeBattleActive=false;routeRewardPending=false;battle=null;
  exploration=null;map=DungeonSystem.Generate(run.dungeon);
  screen=ScreenId.Map;
  message="DEV: 旧探索地図";
  SetRoutePresentationMode(RoutePresentationMode.LegacyMap);
  UiDevCloseWithoutRestore();
 }
 public void UiDevOpenExplorationMap(){
  if(run==null)run=LoadoutSystem.CreateRun(meta,"old_spire");
  packingAtBase=false;
  exploration=ExplorationMapSystem.CreateRun(ExplorationMapCatalog.DefaultMapId);
  map=null;routeBattleActive=false;routeRewardPending=false;battle=null;
  explorationEventActive=false;explorationEventNodeId=-1;
  screen=ScreenId.Map;
  message="DEV: 遠征マップ";
  SetRoutePresentationMode(RoutePresentationMode.RiteDebug);
  UiDevCloseWithoutRestore();
 }
 public void UiDevOpenRiteDebug()=>UiDevOpenExplorationMap();
 public void UiClearRouteBattle(){routeBattleActive=false;}
 public void UiClearRouteReward(){routeRewardPending=false;SetRoutePresentationMode(RoutePresentationMode.RouteExploration);}

 /// <summary>Idempotent: start (or restart) a route-presented battle and sync UI.</summary>
 public void BeginRouteBattle(bool boss=false){
  if(run==null||exploration==null)return;
  StartBattle(boss,preferRoutePresentation:true);
  PackspireUiFoundation.Instance?.BeginRouteBattle();
 }

 /// <summary>Idempotent: enter reward overlay after a route win.</summary>
 public void EnterRouteReward(){
  if(exploration==null)return;
  routeBattleActive=false;
  routeRewardPending=true;
  screen=ScreenId.Map;
  SetRoutePresentationMode(RoutePresentationMode.RouteReward);
  message="戦利品を選んでください";
  PackspireUiFoundation.Instance?.EnterRouteReward();
 }

 /// <summary>Idempotent: claim loot, clear encounter flags, restore exploration presentation.</summary>
 public void FinishRouteReward(string itemId){
  if(run==null||!GameCatalog.Items.ContainsKey(itemId))return;
  var loot=new ItemInstance(itemId){identified=false};
  StorageFormulaSystem.EnsureItemRolled(loot);
  run.lootBag.Add(loot);
  rewardSelectionId="";
  if(exploration!=null)ExplorationMapSystem.MarkCleared(exploration,exploration.currentNodeId);
  routeRewardPending=false;
  routeBattleActive=false;
  battle=null;
  screen=ScreenId.Map;
  SetRoutePresentationMode(RoutePresentationMode.RouteExploration);
 }

 /// <summary>Idempotent: cancel combat/reward without loot (defeat, DEV jump, leave map).</summary>
 public void AbortRouteBattle(){
  routeBattleActive=false;
  routeRewardPending=false;
  battle=null;
  if(exploration!=null&&screen==ScreenId.Map)
   SetRoutePresentationMode(RoutePresentationMode.RiteDebug);
 }

 void EnsureRouteSliceRun(bool fresh){
  if(run==null||fresh)run=LoadoutSystem.CreateRun(meta,"old_spire");
  packingAtBase=false;
  if(exploration==null||fresh||!ExplorationRouteCatalog.IsSliceMap(exploration.mapId))
   exploration=ExplorationMapSystem.CreateRun(ExplorationRouteCatalog.SliceMapId);
  map=null;
 }
 void DevPlaceAt(int cellId){
  if(exploration==null)return;
  exploration.currentNodeId=cellId;
  exploration.selectedNodeId=cellId;
  string ck=ExplorationMapSystem.NodeKey(exploration.activeMapId,cellId);
  if(!exploration.revealed.Contains(ck))exploration.revealed.Add(ck);
  if(!exploration.visited.Contains(ck))exploration.visited.Add(ck);
  // Reveal immediate neighbors for navigation visibility only (not unlocks).
  var def=ExplorationMapSystem.Def(exploration);
  foreach(int n in ExplorationMapSystem.RevealNeighbors(exploration,def,cellId)){
   string nk=ExplorationMapSystem.NodeKey(exploration.activeMapId,n);
   if(!exploration.revealed.Contains(nk))exploration.revealed.Add(nk);
  }
 }
 public string UiRoleMilestone(string roleId,bool maximum)=>RoleMilestoneText(roleId,maximum);
 public void UiSelectHeirloom(string uid){if(meta.stash.Any(x=>x.uid==uid)){meta.selectedHeirloomUid=uid;SaveSystem.Save(meta);}}
 public bool UiTemper(string uid){var item=meta.stash.FirstOrDefault(x=>x.uid==uid);if(item==null||item.temper>=5)return false;int price=30*(item.temper+1);if(meta.baseGold<price)return false;meta.baseGold-=price;item.temper++;SaveSystem.Save(meta);return true;}
 public bool UiChangeFaction(string id){if(meta.currentFaction==id)return true;if(!GameCatalog.Factions.Any(x=>x.id==id)||meta.baseGold<20)return false;meta.baseGold-=20;meta.currentFaction=id;SaveSystem.Save(meta);return true;}
 public void UiSelectLoadout(string id){LoadoutSystem.Select(meta,id);SaveSystem.Save(meta);}
 public void UiStartExpedition(string dungeonId){StartRun(dungeonId);}
 public void UiSetAppearance(int bodyValue,int hairValue){meta.body=Mathf.Clamp(bodyValue,0,3);meta.hair=Mathf.Clamp(hairValue,0,2);}
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
  scroll=Vector2.zero;
 }
 public CharacterDef UiSelectedCharacter=>CharacterSystem.Selected(meta);
 public bool UiUseActiveSkill(){
  if(run==null||battle==null||run.activeSkillUsed)return false;
  var result=CharacterSystem.UseActiveSkill(run,battle);
  if(!result.success)return false;
  if(result.enemyDefeated){WinBattle();return true;}
  return true;
 }
 public bool UiRouteBattleUseActiveSkill(){
  if(!routeBattleActive||run==null||battle==null||run.activeSkillUsed)return false;
  var result=CharacterSystem.UseActiveSkill(run,battle);
  if(!result.success)return false;
  if(result.enemyDefeated){WinBattle();return true;}
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
  if(routeRewardPending&&exploration!=null){FinishRouteReward(itemId);return;}
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
  AbortRouteBattle();
  PackspireUiFoundation.Instance?.ResetRouteEncounterPresentation();
  run=null;exploration=null;map=null;explorationEventActive=false;explorationEventNodeId=-1;
  SetRoutePresentationMode(RoutePresentationMode.None);screen=ScreenId.Hub;scroll=Vector2.zero;
 }
 void OpenPacking(){run=LoadoutSystem.CreateRun(meta,"");packingAtBase=true;selectedUid="";message="荷造りセットを編集";screen=ScreenId.Pack;}
 void StartRun(string dungeon){try{message="ダンジョンを生成中…";run=LoadoutSystem.CreateRun(meta,dungeon);packingAtBase=false;exploration=ExplorationMapSystem.CreateRun(ExplorationMapCatalog.DefaultMapId);map=null;selectedMapNodeId=-1;selectedUid="";explorationEventActive=false;explorationEventNodeId=-1;routeBattleActive=false;routeRewardPending=false;scroll=Vector2.zero;message=$"{ExplorationMapSystem.Def(exploration)?.name??"探索"}へ進入";screen=ScreenId.Map;SetRoutePresentationMode(RoutePresentationMode.RiteDebug);}catch(Exception ex){run=null;map=null;exploration=null;screen=ScreenId.Expedition;message="遠征開始エラー："+ex.Message;Debug.LogException(ex);}}
 void EnterNode(MapNode n){if(n.cleared&&n.type!="shop")return;if(n.type=="battle"||n.type=="boss"){StartBattle(n.type=="boss");return;}if(n.type=="gate"){message="区域門へ到達した";return;}if(n.type=="shop"){screen=ScreenId.Shop;return;}if(n.type=="event"||n.type=="treasure"){n.cleared=true;mapEventNodeId=n.id;mapEventOverlay=true;message=n.type=="treasure"?"封じられた戦利品を発見":"記憶の揺らぎが道を覆った";return;}if(n.type=="rest"){n.cleared=true;run.hp=Mathf.Min(run.maxHp,run.hp+12);foreach(var i in run.inventory)i.durability=6;message="野営して回復・修理した";}else n.cleared=true;}
 void StartBattle(bool boss,bool preferRoutePresentation=false){
  if(run==null)return;
  CharacterSystem.SyncRunCharacter(meta,run);
  var dungeon=GameCatalog.Dungeons.First(x=>x.id==run.dungeon);
  var pool=GameCatalog.Enemies.Where(x=>boss?x.tier==3:x.tier==Mathf.Min(2,1+run.battlesWon/3)).ToArray();
  battle=BattleSystem.Begin(run,pool[UnityEngine.Random.Range(0,pool.Length)],dungeon.hpScale);
  if(preferRoutePresentation&&exploration!=null){
   routeBattleActive=true;
   routeRewardPending=false;
   screen=ScreenId.Map;
   message="戦闘開始";
   SetRoutePresentationMode(RoutePresentationMode.RouteCombat);
  } else {
   routeBattleActive=false;
   routeRewardPending=false;
   screen=ScreenId.Battle;
   SetRoutePresentationMode(RoutePresentationMode.None);
  }
 }
 public bool UiRouteBattlePlayCard(int handIndex){
  if(!routeBattleActive||run==null||battle==null)return false;
  bool won=BattleSystem.Play(run,battle,handIndex);
  if(won){WinBattle();return true;}
  return false;
 }
 public bool UiRouteBattleEndTurn(){
  if(!routeBattleActive||run==null||battle==null)return false;
  var dungeon=GameCatalog.Dungeons.First(x=>x.id==run.dungeon);
  if(BattleSystem.EndTurn(run,battle,dungeon.damage)){FinishRun(false);return true;}
  return false;
 }
 public bool UiRouteBattleUseConsumable(int index){
  if(!routeBattleActive||run==null||battle==null)return false;
  if(!ConsumableSystem.Use(run,battle,index))return false;
  if(battle.enemyHp<=0){WinBattle();return true;}
  return false;
 }
 void WinBattle(){
  int goldBonus=CharacterSystem.WinGoldBonus(run);
  run.battlesWon++;run.gold+=12+run.battlesWon*3+goldBonus;run.hp=Mathf.Min(run.maxHp,run.hp+3);
  if(goldBonus>0)message=$"勝利　+{goldBonus}G（{CharacterSystem.OfRun(run).traitName}）";
  if(map!=null){var fought=map.nodes.FirstOrDefault(n=>n.id==map.current);if(fought!=null)fought.cleared=true;}
  bool routeWin=UsesRoutePresentation||routeBattleActive||routePresentationMode==RoutePresentationMode.RouteCombat;
  if(battle!=null&&battle.enemy.tier==3){FinishRun(true);return;}
  if(routeWin&&exploration!=null){EnterRouteReward();return;}
  routeBattleActive=false;
  if(exploration!=null){
   ExplorationMapSystem.MarkCleared(exploration,exploration.currentNodeId);
   SetRoutePresentationMode(RoutePresentationMode.RiteDebug);
  }
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
  AbortRouteBattle();
  PackspireUiFoundation.Instance?.ResetRouteEncounterPresentation();
  exploration=null;explorationEventActive=false;explorationEventNodeId=-1;
  SetRoutePresentationMode(RoutePresentationMode.None);
  message=win?"遠征成功。戦利品をすべて保管しました":"探索終了。戦利品と獲得ゴールドは持ち帰れません";
  screen=ScreenId.GameOver;
 }
 void AdvanceZone(){if(DungeonSystem.AdvanceZone(map)){message=$"区域 {map.currentZone+1}：{DungeonSystem.CurrentZone(map).name}へ進入";scroll=Vector2.zero;}}
 string RoleMilestoneText(string id,bool maximum){if(id.Contains("guardian")||id.Contains("bulwark")||id.Contains("knight"))return maximum?"戦闘開始時に防御を得て、余剰防御を次のターンへ一部持ち越す。":"防御カードを連続使用すると次の防御効果が上昇する。";if(id.Contains("scout")||id.Contains("hunter")||id.Contains("blade")||id.Contains("dancer"))return maximum?"各戦闘で最初に使う0コストカードを複製する。":"異なる装備由来のカードを続けて使うと追加ドロー。";if(id.Contains("artificer")||id.Contains("rune")||id.Contains("channeler"))return maximum?"戦闘中に最初に使うルーン・道具カードの耐久を消費しない。":"属性一致が3色以上なら戦闘開始時にエネルギーを得る。";return maximum?"武器カードを一定回数使うたび、ラン中の攻撃力が成長する。":"同じ武器由来のカードを続けて使うと追加ダメージ。";}
 ItemInstance CloneItem(ItemInstance x)=>JsonUtility.FromJson<ItemInstance>(JsonUtility.ToJson(x));
}
}
