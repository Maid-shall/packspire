using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire {
public enum TabletopTransitionKind { PageTurn, ScrollUnfurl, BattleTable, Fade }

/// <summary>Retained-mode UI host. Legacy screens remain active until they are migrated here.</summary>
public sealed class PackspireUiFoundation : MonoBehaviour {
 public static PackspireUiFoundation Instance { get; private set; }

 PackspireGame game;
 UIDocument document;
 PanelSettings panelSettings; bool ownsPanelSettings,uiReady;
 VisualElement root,screenRoot,transitionRoot,dim,leftPaper,rightPaper,scrollPaper,battleShade,toast;
 Coroutine transitionRoutine;
 ScreenId renderedScreen;
 bool hasRenderedScreen;
 bool nextPageIsLeft;
 string selectedRoleId="",selectedCompendiumId="";
 string selectedVaultUid="",selectedFactionId="",selectedDungeonId="";
 string selectedPackingUid="",selectedRewardId="",selectedShopId="";
 int packingRotation;
 int compendiumTab;
 Texture2D tabletopDesk;

 void Awake(){
  if(Instance!=null&&Instance!=this){Destroy(this);return;}
  Instance=this;game=GetComponent<PackspireGame>();
  tabletopDesk=Resources.Load<Texture2D>("Art/UI/tabletop-desk-v1");
  document=gameObject.GetComponent<UIDocument>()??gameObject.AddComponent<UIDocument>();
  panelSettings=Resources.Load<PanelSettings>("UI/PackspirePanelSettings");
  if(panelSettings==null){panelSettings=ScriptableObject.CreateInstance<PanelSettings>();panelSettings.name="Packspire Runtime UI Fallback";panelSettings.scaleMode=PanelScaleMode.ScaleWithScreenSize;panelSettings.referenceResolution=new Vector2Int(1280,720);panelSettings.match=.5f;panelSettings.sortingOrder=120;ownsPanelSettings=true;}
  var tree=Resources.Load<VisualTreeAsset>("UI/PackspireRoot");
  document.enabled=false;document.panelSettings=panelSettings;document.visualTreeAsset=tree;document.enabled=true;
 }

 IEnumerator Start(){for(int frame=0;frame<30;frame++){if(document!=null&&document.rootVisualElement!=null&&document.rootVisualElement.panel!=null)break;yield return null;}BuildRoot();if(uiReady)RefreshScreen(true);}
 void Update(){if(root!=null)RefreshScreen(false);}
 void OnDestroy(){if(Instance==this)Instance=null;if(ownsPanelSettings&&panelSettings!=null)Destroy(panelSettings);}

 public bool Handles(ScreenId value)=>uiReady&&value!=ScreenId.Map&&value!=ScreenId.Battle;

 void BuildRoot(){
  root=document.rootVisualElement;if(root==null||root.panel==null){uiReady=false;Debug.LogWarning("Packspire UI Toolkit root is not ready; legacy UI remains active.");return;}
  root.name="packspire-ui-root";root.AddToClassList("packspire-root");root.pickingMode=PickingMode.Ignore;
  var theme=Resources.Load<StyleSheet>("UI/PackspireTheme");if(theme!=null)root.styleSheets.Add(theme);
  screenRoot=new VisualElement{name="screen-root",pickingMode=PickingMode.Position};screenRoot.AddToClassList("ps-screen-host");root.Add(screenRoot);
  transitionRoot=new VisualElement{name="transition-root",pickingMode=PickingMode.Ignore};transitionRoot.AddToClassList("ps-transition-host");root.Add(transitionRoot);
  dim=Layer("transition-dim");leftPaper=Layer("transition-paper transition-paper-left");rightPaper=Layer("transition-paper transition-paper-right");scrollPaper=Layer("transition-scroll");battleShade=Layer("transition-battle");
  transitionRoot.Add(dim);transitionRoot.Add(leftPaper);transitionRoot.Add(rightPaper);transitionRoot.Add(scrollPaper);transitionRoot.Add(battleShade);HideTransition();toast=Container("ps-toast");toast.style.display=DisplayStyle.None;root.Add(toast);uiReady=true;
 }

 VisualElement Layer(string classes){var element=new VisualElement{pickingMode=PickingMode.Ignore};foreach(var value in classes.Split(' '))element.AddToClassList(value);return element;}

 void RefreshScreen(bool force){
  if(game==null||screenRoot==null)return;
  if(!Handles(game.UiScreen)){screenRoot.style.display=DisplayStyle.None;hasRenderedScreen=false;return;}
  screenRoot.style.display=DisplayStyle.Flex;
  if(!force&&hasRenderedScreen&&renderedScreen==game.UiScreen)return;
  renderedScreen=game.UiScreen;hasRenderedScreen=true;screenRoot.Clear();
  if(renderedScreen==ScreenId.Character)BuildCharacter();
  else if(renderedScreen==ScreenId.Hub)BuildHome();
  else if(renderedScreen==ScreenId.Status)BuildStatus();
  else if(renderedScreen==ScreenId.Vault)BuildVault();
  else if(renderedScreen==ScreenId.Faction)BuildFaction();
  else if(renderedScreen==ScreenId.Expedition)BuildExpedition();
  else if(renderedScreen==ScreenId.Pack)BuildPacking();
  else if(renderedScreen==ScreenId.Reward)BuildReward();
  else if(renderedScreen==ScreenId.Shop)BuildShop();
  else if(renderedScreen==ScreenId.Event)BuildEvent();
  else if(renderedScreen==ScreenId.GameOver)BuildGameOver();
  else BuildCompendium();
  AnimateScreenIn();
 }

 void AnimateScreenIn(){if(screenRoot==null)return;float x=renderedScreen==ScreenId.Faction?70f:renderedScreen==ScreenId.Expedition?-70f:renderedScreen==ScreenId.Pack?-45f:0f;float y=renderedScreen==ScreenId.Pack?55f:12f;screenRoot.style.opacity=.01f;screenRoot.style.translate=new Translate(x,y,0);screenRoot.schedule.Execute(()=>{if(screenRoot==null)return;screenRoot.style.opacity=1f;screenRoot.style.translate=new Translate(0,0,0);}).StartingIn(16);}

 void BuildCharacter(){
  var meta=game.UiMeta;var shell=BookShell("探索者の作成",null);screenRoot.Add(shell);var pages=shell.Q<VisualElement>("book-pages");var left=Page("探索者の肖像");var right=Page("最初の一頁");pages.Add(left);pages.Add(right);
  left.Add(Atlas(game.UiCharacterArt,CharacterUv(meta.body,meta.hair),"ps-character-preview"));left.Add(PackspireUiFactory.Body($"{(meta.body<2?"男性":"女性")}タイプ {(meta.body%2)+1}　／　{new[]{"短髪","ミディアム","結び髪"}[meta.hair]}"));
  right.Add(PackspireUiFactory.Title("体格"));var bodies=Container("ps-choice-grid");for(int i=0;i<4;i++){int value=i;var choice=AtlasButton(game.UiCharacterArt,CharacterUv(value,meta.hair),$"{(value<2?"男性":"女性")} {(value%2)+1}",value==meta.body,()=>{game.UiSetAppearance(value,meta.hair);BuildCharacterAgain();});bodies.Add(choice);}right.Add(bodies);
  right.Add(PackspireUiFactory.Title("髪型"));var hairs=Container("ps-choice-row");string[] names={"短髪","ミディアム","結び髪"};for(int i=0;i<3;i++){int value=i;var choice=AtlasButton(game.UiCharacterArt,CharacterUv(meta.body,value),names[value],value==meta.hair,()=>{game.UiSetAppearance(meta.body,value);BuildCharacterAgain();});hairs.Add(choice);}right.Add(hairs);
  var start=PackspireUiFactory.Button("この姿で物語を始める",()=>game.UiFinishCharacter());start.AddToClassList("ps-primary-action");right.Add(start);
 }
 void BuildCharacterAgain(){screenRoot.Clear();BuildCharacter();}

 void BuildHome(){
  var meta=game.UiMeta;var desk=TabletopDesk("ps-tabletop-home");screenRoot.Add(desk);
  var book=Container("ps-tabletop-book ps-home-journal");if(game.UiBookArt!=null)book.Add(Image(game.UiBookArt,new Rect(0,0,1,1),"ps-tabletop-book-art",ScaleMode.StretchToFill));desk.Add(book);
  var left=Container("ps-tabletop-page ps-tabletop-page-left");var right=Container("ps-tabletop-page ps-tabletop-page-right");book.Add(left);book.Add(right);
  left.Add(PackspireUiFactory.Title(GameCatalog.Roles[meta.currentRole].name));left.Add(PackspireUiFactory.Body(FactionName(meta.currentFaction)+"所属"));if(game.UiCharacterArt!=null)left.Add(Image(game.UiCharacterArt,CharacterUv(meta.body,meta.hair),"ps-tabletop-character",ScaleMode.ScaleToFit));
  var loadout=LoadoutSystem.Active(meta);left.Add(PackspireUiFactory.Body($"使用中　{loadout.name}\n{BackpackName(loadout.backpack)}"));
  right.Add(PackspireUiFactory.Title("旅人の記録"));right.Add(PackspireUiFactory.Body($"所持金 {meta.baseGold}G　　遠征 {meta.wins}/{meta.runs}"));right.Add(InkRule());
  AddJournalLink(right,"役職と成長",ScreenId.Status);AddJournalLink(right,"保管庫と家宝",ScreenId.Vault);AddJournalLink(right,"冒険図鑑",ScreenId.Compendium);
  desk.Add(EdgeObject("勢力記録",ScreenId.Faction,"ps-edge-corkboard"));desk.Add(EdgeObject("遠征準備",ScreenId.Expedition,"ps-edge-expedition"));desk.Add(EdgeObject("荷造り",ScreenId.Pack,"ps-edge-packing"));
 }

 VisualElement TabletopDesk(string extraClass){var desk=Container("ps-tabletop-desk "+extraClass);if(tabletopDesk!=null)desk.Add(Image(tabletopDesk,new Rect(0,0,1,1),"ps-tabletop-desk-art",ScaleMode.ScaleAndCrop));return desk;}
 VisualElement EdgeObject(string label,ScreenId target,string classes){var button=new Button(()=>game.UiNavigate(target)){tooltip=label};button.AddToClassList("ps-edge-object");button.AddToClassList(classes);var text=new Label(label);text.AddToClassList("ps-edge-label");button.Add(text);return button;}
 void AddJournalLink(VisualElement parent,string label,ScreenId target){var button=new Button(()=>game.UiNavigate(target));button.AddToClassList("ps-journal-link");button.Add(new Label(label));button.Add(new Label("›"));parent.Add(button);}
 Button TabletopBack(){var button=new Button(()=>game.UiNavigate(ScreenId.Hub)){text="机の中央へ戻る"};button.AddToClassList("ps-tabletop-back");return button;}

 void AddHomeEntry(VisualElement parent,string title,string description,ScreenId target,int iconIndex){
  var button=new Button(()=>game.UiNavigate(target)){tooltip=description};button.AddToClassList("ps-home-entry");
  var marginMark=new VisualElement();marginMark.AddToClassList("ps-home-entry-margin-mark");button.Add(marginMark);
  var copy=Container("ps-home-entry-copy");var heading=PackspireUiFactory.Title(title);heading.AddToClassList("ps-home-entry-title");copy.Add(heading);copy.Add(PackspireUiFactory.Body(description));button.Add(copy);
  var arrow=new Label("›");arrow.AddToClassList("ps-home-entry-arrow");button.Add(arrow);button.Add(InkRule());parent.Add(button);
 }

 void BuildStatus(){
  var meta=game.UiMeta;var learned=meta.jobLevels.Where(x=>x.value>0&&GameCatalog.Roles.ContainsKey(x.id)).ToList();
  if(learned.Count==0){screenRoot.Add(BookShell("役職記録",PackspireUiFactory.EmptyState("役職記録なし","役職を習得するとここへ記録されます。")));return;}
  if(string.IsNullOrEmpty(selectedRoleId)||!learned.Any(x=>x.id==selectedRoleId))selectedRoleId=learned.FirstOrDefault(x=>x.id==meta.currentRole)?.id??learned[0].id;
  var shell=BookShell("役職記録",null);screenRoot.Add(shell);var pages=shell.Q<VisualElement>("book-pages");var left=Page("習得役職");var right=Page("選択中の役職");pages.Add(left);pages.Add(right);
  var list=Container("ps-record-list");left.Add(list);
  foreach(var level in learned){var role=GameCatalog.Roles[level.id];var row=RecordButton(role.name,$"Lv.{level.value}/{role.maxLevel}　{role.kind}",Atlas(game.UiRoleArt,RoleUv(role.id),"ps-record-thumb"),level.id==selectedRoleId,()=>{selectedRoleId=level.id;BuildStatusAgain();});list.Add(row);}
  var selectedLevel=learned.First(x=>x.id==selectedRoleId);var selected=GameCatalog.Roles[selectedRoleId];right.Add(Atlas(game.UiRoleArt,RoleUv(selected.id),"ps-detail-art"));right.Add(PackspireUiFactory.Title($"{selected.name}　Lv.{selectedLevel.value}/{selected.maxLevel}"));right.Add(PackspireUiFactory.Body(selected.kind+"\n"+selected.description));
  var milestones=Container("ps-milestones");milestones.Add(Milestone(1,selectedLevel.value,"基礎効果",selected.description));milestones.Add(Milestone(7,selectedLevel.value,"専用効果",game.UiRoleMilestone(selected.id,false)));milestones.Add(Milestone(selected.maxLevel,selectedLevel.value,"最大レベル効果",game.UiRoleMilestone(selected.id,true)));right.Add(milestones);
  right.Add(PackspireUiFactory.Body(selected.id==meta.currentRole?"● 現在の役職":"転職は専用イベントまたは施設から行います"));AddBookTabs(shell,ScreenId.Status);
 }

 void BuildStatusAgain(){screenRoot.Clear();BuildStatus();}

 void BuildVault(){
  var meta=game.UiMeta;if(string.IsNullOrEmpty(selectedVaultUid)||!meta.stash.Any(x=>x.uid==selectedVaultUid))selectedVaultUid=meta.stash.FirstOrDefault()?.uid??"";
  var shell=BookShell("保管庫",null);screenRoot.Add(shell);var pages=shell.Q<VisualElement>("book-pages");var left=Page("所持装備");var right=Page("装備の記録");pages.Add(left);pages.Add(right);
  var active=LoadoutSystem.Active(meta);left.Add(PackspireUiFactory.Body($"使用中　{active.name}　／　{BackpackName(active.backpack)}\n保管数　{meta.stash.Count}　　所持金　{meta.baseGold}G"));
  var grid=Container("ps-vault-grid");left.Add(grid);
  foreach(var item in meta.stash){var def=GameCatalog.Items[item.templateId];bool heir=item.uid==meta.selectedHeirloomUid;var tile=AtlasButton(game.UiEquipmentArt,ItemUv(def.id),(heir?"★ ":"")+def.name,item.uid==selectedVaultUid,()=>{selectedVaultUid=item.uid;BuildVaultAgain();});grid.Add(tile);}
  var selected=meta.stash.FirstOrDefault(x=>x.uid==selectedVaultUid);if(selected==null)right.Add(PackspireUiFactory.EmptyState("装備はまだありません","遠征から帰還すると、持ち帰った装備がここへ記録されます。"));else{
   var def=GameCatalog.Items[selected.templateId];right.Add(Atlas(game.UiEquipmentArt,ItemUv(def.id),"ps-detail-art"));right.Add(PackspireUiFactory.Title(def.name));right.Add(PackspireUiFactory.Body(def.description));
   right.Add(PackspireUiFactory.Body($"{ItemTypeLabel(def.type)}　{def.cells.Length}マス\n耐久　{selected.durability}/6　　鍛錬　+{selected.temper}\n使用　{selected.uses}回　　傷跡　{selected.scars.Count}"));
   if(!string.IsNullOrEmpty(def.linkRule)){right.Add(PackspireUiFactory.Title("LINK効果"));right.Add(PackspireUiFactory.Body(def.linkRule));}
   var actions=Container("ps-detail-actions");var heirButton=PackspireUiFactory.Button(selected.uid==meta.selectedHeirloomUid?"★ 家宝に設定中":"家宝に設定",()=>{game.UiSelectHeirloom(selected.uid);BuildVaultAgain();ShowToast("家宝として記録しました");});actions.Add(heirButton);
   int price=30*(selected.temper+1);var temper=PackspireUiFactory.Button(selected.temper>=5?"鍛錬上限":"鍛錬する　"+price+"G",()=>{if(game.UiTemper(selected.uid))ShowToast("鍛錬が完了しました");BuildVaultAgain();});if(selected.temper>=5||meta.baseGold<price)temper.SetEnabled(false);actions.Add(temper);right.Add(actions);
  }
  AddBookTabs(shell,ScreenId.Vault);
 }
 void BuildVaultAgain(){screenRoot.Clear();BuildVault();}

 void BuildFaction(){
  var meta=game.UiMeta;if(string.IsNullOrEmpty(selectedFactionId)||!GameCatalog.Factions.Any(x=>x.id==selectedFactionId))selectedFactionId=meta.currentFaction;
  var desk=TabletopDesk("ps-faction-workspace");screenRoot.Add(desk);var board=Container("ps-expanded-corkboard");desk.Add(board);board.Add(PackspireUiFactory.Title("勢力関係図"));board.Add(PackspireUiFactory.Body("印章を選ぶと、右下の記録帳に詳細が開きます。"));var nodes=Container("ps-faction-nodes");board.Add(nodes);
  int nodeIndex=0;foreach(var faction in GameCatalog.Factions){float rep=meta.factionRep.FirstOrDefault(x=>x.id==faction.id)?.value??0;var node=new Button(()=>{selectedFactionId=faction.id;BuildFactionAgain();}){tooltip=faction.name};node.AddToClassList("ps-faction-pin");node.AddToClassList("ps-faction-pin-"+(nodeIndex++));if(faction.id==selectedFactionId)node.AddToClassList("ps-selected");node.Add(Atlas(game.UiFactionArt,FactionUv(faction.id),"ps-faction-pin-art"));node.Add(PackspireUiFactory.Title(faction.name));node.Add(PackspireUiFactory.Body($"貢献 {rep:0}"));nodes.Add(node);}
  var selected=GameCatalog.Factions.First(x=>x.id==selectedFactionId);float value=meta.factionRep.FirstOrDefault(x=>x.id==selected.id)?.value??0;int rank=Mathf.Clamp(Mathf.FloorToInt(value/25f),0,selected.ranks.Length-1);
  var journal=Container("ps-detail-journal");desk.Add(journal);if(game.UiBookArt!=null)journal.Add(Image(game.UiBookArt,new Rect(0,0,1,1),"ps-detail-journal-art",ScaleMode.StretchToFill));var detail=Container("ps-detail-journal-copy");journal.Add(detail);detail.Add(Atlas(game.UiFactionArt,FactionUv(selected.id),"ps-faction-detail-art"));detail.Add(PackspireUiFactory.Title(selected.name));detail.Add(PackspireUiFactory.Body(selected.description));detail.Add(PackspireUiFactory.Body($"現在階級　{selected.ranks[rank]}\n貢献度　{value:0}"));
  var progress=Container("ps-progress");var fill=Container("ps-progress-fill");fill.style.width=Length.Percent(Mathf.Clamp01(value/75f)*100f);progress.Add(fill);detail.Add(progress);
  var change=PackspireUiFactory.Button(selected.id==meta.currentFaction?"● 現在の所属":"20Gで所属を変更",()=>{if(game.UiChangeFaction(selected.id))ShowToast(selected.name+"へ所属を変更しました");BuildFactionAgain();});if(selected.id!=meta.currentFaction&&meta.baseGold<20)change.SetEnabled(false);detail.Add(change);
  var requests=Container("ps-request-rail");requests.Add(PackspireUiFactory.Body("依頼書留め　—　新しい依頼は遠征準備へ記録されます"));board.Add(requests);desk.Add(TabletopBack());
 }
 void BuildFactionAgain(){screenRoot.Clear();BuildFaction();}

 void BuildExpedition(){
  var meta=game.UiMeta;int unlocked=Mathf.Clamp(meta.dungeonsUnlocked,1,GameCatalog.Dungeons.Length);if(string.IsNullOrEmpty(selectedDungeonId)||!GameCatalog.Dungeons.Take(unlocked).Any(x=>x.id==selectedDungeonId))selectedDungeonId=GameCatalog.Dungeons[0].id;
  var desk=TabletopDesk("ps-expedition-workspace");screenRoot.Add(desk);var tray=Container("ps-expedition-case");desk.Add(tray);var selected=GameCatalog.Dungeons.First(x=>x.id==selectedDungeonId);tray.Add(PackspireUiFactory.Title("遠征準備"));tray.Add(Atlas(game.UiDungeonArt,DungeonUv(selected.id),"ps-expedition-selected-art"));tray.Add(PackspireUiFactory.Title(selected.name));tray.Add(PackspireUiFactory.Body(selected.description));tray.Add(PackspireUiFactory.Body($"敵HP ×{selected.hpScale:0.00}　追加攻撃 {selected.damage}　報酬 ×{selected.goldScale:0.00}"));
  tray.Add(PackspireUiFactory.Title("使用する荷造り"));var loadouts=Container("ps-loadout-tabs");foreach(var loadout in meta.loadouts){var entry=loadout;var button=PackspireUiFactory.Button(entry.name,()=>{game.UiSelectLoadout(entry.id);BuildExpeditionAgain();});if(entry.id==meta.selectedLoadoutId)button.AddToClassList("ps-selected");loadouts.Add(button);}tray.Add(loadouts);var active=LoadoutSystem.Active(meta);tray.Add(PackspireUiFactory.Body($"{BackpackName(active.backpack)}　配置 {active.slots.Count}　カード {active.deck.Count}"));var launch=PackspireUiFactory.Button("この地図へ遠征する",()=>game.UiStartExpedition(selected.id));launch.AddToClassList("ps-primary-action");tray.Add(launch);
  var rack=new ScrollView();rack.AddToClassList("ps-map-roll-rack");desk.Add(rack);for(int i=0;i<GameCatalog.Dungeons.Length;i++){var dungeon=GameCatalog.Dungeons[i];bool available=i<unlocked;var roll=new Button(()=>{if(available){selectedDungeonId=dungeon.id;BuildExpeditionAgain();}});roll.AddToClassList("ps-map-roll");if(dungeon.id==selectedDungeonId)roll.AddToClassList("ps-selected");if(!available)roll.AddToClassList("ps-locked");roll.Add(Atlas(game.UiDungeonArt,DungeonUv(dungeon.id),"ps-map-roll-seal"));roll.Add(new Label(available?dungeon.name:"封印された地図"));rack.Add(roll);}desk.Add(TabletopBack());
 }
 void BuildExpeditionAgain(){screenRoot.Clear();BuildExpedition();}

 void BuildPacking(){
  var run=game.UiRun;if(run==null){game.UiNavigate(ScreenId.Hub);return;}if(!string.IsNullOrEmpty(selectedPackingUid)&&!run.inventory.Any(x=>x.uid==selectedPackingUid))selectedPackingUid="";
  var desk=TabletopDesk("ps-packing-workspace");screenRoot.Add(desk);var left=Container("ps-canvas-packing-mat ps-packing-page");var right=new ScrollView();right.AddToClassList("ps-equipment-tray");right.AddToClassList("ps-packing-detail-page");desk.Add(left);desk.Add(right);left.Add(PackspireUiFactory.Title("荷造り布"));
  var loadouts=Container("ps-loadout-tabs");foreach(var loadout in game.UiMeta.loadouts){var entry=loadout;var button=PackspireUiFactory.Button(entry.name,()=>{game.UiOpenPackingLoadout(entry.id);selectedPackingUid="";BuildPackingAgain();});if(entry.id==game.UiMeta.selectedLoadoutId)button.AddToClassList("ps-selected");loadouts.Add(button);}left.Add(loadouts);
  var bags=Container("ps-bag-tabs");foreach(var bag in GameCatalog.Backpacks){var entry=bag;var button=PackspireUiFactory.Button(entry.name,()=>{game.UiPackingSetBackpack(entry.id);BuildPackingAgain();});if(entry.id==run.backpack)button.AddToClassList("ps-selected");bags.Add(button);}left.Add(bags);
  var tools=Container("ps-packing-tools");tools.Add(PackspireUiFactory.Button("↻ 回転",()=>{packingRotation=(packingRotation+1)%4;BuildPackingAgain();}));tools.Add(PackspireUiFactory.Button("選択解除",()=>{selectedPackingUid="";packingRotation=0;BuildPackingAgain();}));var remove=PackspireUiFactory.Button("バッグから外す",()=>{game.UiPackingRemove(selectedPackingUid);BuildPackingAgain();});remove.SetEnabled(!string.IsNullOrEmpty(selectedPackingUid)&&run.placements.Any(x=>x.itemUid==selectedPackingUid));tools.Add(remove);left.Add(tools);
  var packingHint=PackspireUiFactory.Body("装備を選び、置きたいマスを押してください。薄い色は収納面の属性です。");packingHint.AddToClassList("ps-packing-hint");left.Add(packingHint);left.Add(BuildBackpackGrid(run));
  right.Add(PackspireUiFactory.Title("所持装備"));var inventory=Container("ps-packing-inventory");foreach(var item in run.inventory){var entry=item;bool placed=run.placements.Any(x=>x.itemUid==entry.uid);var tile=AtlasButton(game.UiEquipmentArt,ItemUv(entry.templateId),(placed?"● ":"")+GameCatalog.Items[entry.templateId].name,entry.uid==selectedPackingUid,()=>{selectedPackingUid=entry.uid;packingRotation=run.placements.FirstOrDefault(x=>x.itemUid==entry.uid)?.rotation??0;BuildPackingAgain();});inventory.Add(tile);}right.Add(inventory);
  var selected=run.inventory.FirstOrDefault(x=>x.uid==selectedPackingUid);
  if(selected!=null){
   var def=GameCatalog.Items[selected.templateId];
   string elements=string.Join("・",def.cells.Select((cell,i)=>ElementLabel(selected.colors!=null&&i<selected.colors.Count?selected.colors[i]:cell.element)));
   right.Add(PackspireUiFactory.Title(def.name));
   right.Add(PackspireUiFactory.Body(def.description+"\n属性　"+elements));
   if(!string.IsNullOrEmpty(def.linkRule))right.Add(PackspireUiFactory.Body("LINK　"+def.linkRule));
  }
  var build=BackpackSystem.Build(run);right.Add(PackspireUiFactory.Title($"戦闘へ持ち込むカード　{run.selectedCardSlots.Count}/{LoadoutSystem.EquipmentCardLimit}"));var cards=Container("ps-card-choice-list");foreach(var card in build.candidates){var entry=card;bool chosen=run.selectedCardSlots.Contains(entry.slotKey);var button=PackspireUiFactory.Card($"{(chosen?"●":"○")} {entry.name}　{entry.cost}EN",entry.text,()=>{game.UiPackingToggleCard(entry.slotKey);BuildPackingAgain();});if(chosen)button.AddToClassList("ps-selected");if(!chosen&&run.selectedCardSlots.Count>=LoadoutSystem.EquipmentCardLimit)button.SetEnabled(false);cards.Add(button);}right.Add(cards);
  right.Add(PackspireUiFactory.Title("発動中の効果"));right.Add(PackspireUiFactory.Body($"火 {build.colors[Element.Fire]}　水 {build.colors[Element.Water]}　風 {build.colors[Element.Wind]}　土 {build.colors[Element.Earth]}\n"+(build.links.Count==0?"LINKなし":string.Join("／",build.links))));
  var save=PackspireUiFactory.Button("この荷造りを保存",()=>game.UiPackingSave());save.AddToClassList("ps-primary-action");right.Add(save);if(game.UiPackingAtBase)desk.Add(TabletopBack());
 }
 VisualElement BuildBackpackGrid(RunState run){var grid=Container("ps-backpack-grid");for(int index=0;index<24;index++){int cellIndex=index;var occupant=PlacementAt(run,index);var cell=new Button(()=>{if(!string.IsNullOrEmpty(selectedPackingUid)){if(!game.UiPackingPlace(selectedPackingUid,cellIndex,packingRotation))ShowToast("その位置には配置できません");}else if(occupant!=null){selectedPackingUid=occupant.itemUid;packingRotation=occupant.rotation;}BuildPackingAgain();});cell.AddToClassList("ps-backpack-cell");cell.AddToClassList("ps-element-"+GameCatalog.Board[index].ToString().ToLowerInvariant());if(occupant!=null){var item=run.inventory.FirstOrDefault(x=>x.uid==occupant.itemUid);if(item!=null){cell.tooltip=GameCatalog.Items[item.templateId].name;cell.Add(Atlas(game.UiEquipmentArt,ItemUv(item.templateId),"ps-cell-item"));var element=CellElementAt(run,occupant,index);bool match=element.HasValue&&element.Value==GameCatalog.Board[index];cell.AddToClassList(match?"ps-cell-match":"ps-cell-mismatch");if(match){var mark=new Label("◆");mark.AddToClassList("ps-match-mark");cell.Add(mark);}if(item.uid==selectedPackingUid)cell.AddToClassList("ps-cell-selected");}}grid.Add(cell);}return grid;}
 Placement PlacementAt(RunState run,int index){int x=index%6,y=index/6;foreach(var placement in run.placements){var item=run.inventory.FirstOrDefault(i=>i.uid==placement.itemUid);if(item==null)continue;int ax=placement.anchor%6,ay=placement.anchor/6;if(BackpackSystem.Layout(GameCatalog.Items[item.templateId],placement.rotation,item).Any(c=>ax+c.pos.x==x&&ay+c.pos.y==y))return placement;}return null;}
 Element? CellElementAt(RunState run,Placement placement,int index){var item=run.inventory.FirstOrDefault(i=>i.uid==placement.itemUid);if(item==null)return null;int x=index%6,y=index/6,ax=placement.anchor%6,ay=placement.anchor/6;foreach(var cell in BackpackSystem.Layout(GameCatalog.Items[item.templateId],placement.rotation,item))if(ax+cell.pos.x==x&&ay+cell.pos.y==y)return cell.element;return null;}
 void BuildPackingAgain(){screenRoot.Clear();BuildPacking();}

 string[] RewardIds(){string[] pool={"dagger","plate","crystal","bomb","spear","buckler","flask","charm"};int start=((game.UiRun?.battlesWon??0)*3)%pool.Length;return Enumerable.Range(0,3).Select(i=>pool[(start+i)%pool.Length]).ToArray();}
 void BuildReward(){var rewards=RewardIds();if(string.IsNullOrEmpty(selectedRewardId)||!rewards.Contains(selectedRewardId))selectedRewardId=rewards[0];var shell=BookShell("戦闘報酬",null);screenRoot.Add(shell);var pages=shell.Q<VisualElement>("book-pages");var left=Page("獲得候補");var right=Page("戦利品の記録");pages.Add(left);pages.Add(right);foreach(var id in rewards){string value=id;var item=GameCatalog.Items[value];left.Add(RecordButton(item.name,$"{ItemTypeLabel(item.type)}　{item.cells.Length}マス　／　未鑑定",Atlas(game.UiEquipmentArt,ItemUv(value),"ps-record-thumb"),value==selectedRewardId,()=>{selectedRewardId=value;BuildRewardAgain();}));}AddItemDetail(right,selectedRewardId);var take=PackspireUiFactory.Button("この戦利品を獲得する",()=>game.UiTakeReward(selectedRewardId));take.AddToClassList("ps-primary-action");right.Add(take);}
 void BuildRewardAgain(){screenRoot.Clear();BuildReward();}

 void BuildShop(){string[] stock=GameCatalog.Items.Keys.Take(5).ToArray();if(string.IsNullOrEmpty(selectedShopId)||!stock.Contains(selectedShopId))selectedShopId=stock[0];var shell=BookShell("行商人",null);screenRoot.Add(shell);var pages=shell.Q<VisualElement>("book-pages");var left=Page("本日の品書き");var right=Page("商品の記録");pages.Add(left);pages.Add(right);left.Add(PackspireUiFactory.Body($"所持金　{game.UiRun?.gold??0}G"));foreach(var id in stock){string value=id;var item=GameCatalog.Items[value];int price=14+item.cells.Length*4;left.Add(RecordButton(item.name,$"{ItemTypeLabel(item.type)}　{item.cells.Length}マス　／　{price}G",Atlas(game.UiEquipmentArt,ItemUv(value),"ps-record-thumb"),value==selectedShopId,()=>{selectedShopId=value;BuildShopAgain();}));}AddItemDetail(right,selectedShopId);var selected=GameCatalog.Items[selectedShopId];int cost=14+selected.cells.Length*4;var buy=PackspireUiFactory.Button((game.UiRun?.gold??0)>=cost?$"{cost}Gで購入する":"所持金が足りません",()=>{if(game.UiBuy(selectedShopId))ShowToast(selected.name+"を購入しました");BuildShopAgain();});buy.SetEnabled((game.UiRun?.gold??0)>=cost);buy.AddToClassList("ps-primary-action");right.Add(buy);right.Add(PackspireUiFactory.Button("地図へ戻る",()=>game.UiReturnToMap()));}
 void BuildShopAgain(){screenRoot.Clear();BuildShop();}
 void AddItemDetail(VisualElement page,string id){var item=GameCatalog.Items[id];page.Add(Atlas(game.UiEquipmentArt,ItemUv(id),"ps-detail-art"));page.Add(PackspireUiFactory.Title(item.name));page.Add(PackspireUiFactory.Body(item.description));page.Add(PackspireUiFactory.Body($"{ItemTypeLabel(item.type)}　{item.cells.Length}マス\n属性　{string.Join("・",item.cells.Select(x=>ElementLabel(x.element)))}"));if(!string.IsNullOrEmpty(item.linkRule))page.Add(PackspireUiFactory.Body("LINK　"+item.linkRule));}

 void BuildEvent(){var run=game.UiRun;var screen=Container("ps-event-screen");screenRoot.Add(screen);if(game.UiDungeonArt!=null)screen.Add(Atlas(game.UiDungeonArt,DungeonUv(run?.dungeon??"old_spire"),"ps-event-background"));var mist=Container("ps-event-mist");screen.Add(mist);var dialog=Container("ps-event-panel");mist.Add(dialog);dialog.Add(PackspireUiFactory.Title("記憶の揺らぎ"));dialog.Add(PackspireUiFactory.Body("空間の奥で、紫の残光がゆっくりと鼓動している。触れれば何かが変わる。その代償までは、まだ記されていない。"));dialog.Add(Choice("代償を支払う","HP -6　／　24Gを獲得",()=>game.UiResolveEvent(0)));dialog.Add(Choice("残響を修復する","所持装備の耐久をすべて回復",()=>game.UiResolveEvent(1)));dialog.Add(Choice("立ち去る","何も変えず探索へ戻る",()=>game.UiResolveEvent(2)));}
 VisualElement Choice(string title,string description,System.Action action){var button=PackspireUiFactory.Card(title,description,action);button.AddToClassList("ps-event-choice");return button;}

 void BuildGameOver(){var run=game.UiRun;bool success=run!=null&&run.hp>0;var shell=BookShell(success?"遠征完了":"探索終了",null);screenRoot.Add(shell);var pages=shell.Q<VisualElement>("book-pages");var left=Page(success?"帰還の章":"断章");var right=Page("遠征の記録");pages.Add(left);pages.Add(right);left.Add(Atlas(game.UiDungeonArt,DungeonUv(run?.dungeon??"old_spire"),"ps-result-art"));left.Add(PackspireUiFactory.Title(success?"QUEST CLEAR":"EXPEDITION END"));left.Add(PackspireUiFactory.Body(success?"冒険者は戦利品とともに、次の頁へ続く経験を持ち帰った。":"物語は途中で閉じられた。それでも残された記録が次の遠征へ繋がる。"));right.Add(PackspireUiFactory.Body(string.IsNullOrEmpty(game.UiMessage)?"遠征の記録を閉じます。":game.UiMessage));right.Add(ResultRow("戦闘勝利",(run?.battlesWon??0).ToString()));right.Add(ResultRow("獲得ゴールド",$"{run?.gold??0}G"));right.Add(ResultRow("戦利品",$"{run?.lootBag.Count??0}個"));var home=PackspireUiFactory.Button("拠点へ戻る",()=>game.UiReturnToHub());home.AddToClassList("ps-primary-action");right.Add(home);}
 VisualElement ResultRow(string label,string value){var row=Container("ps-result-row");row.Add(PackspireUiFactory.Body(label));row.Add(PackspireUiFactory.Title(value));return row;}
 void ShowToast(string message){if(toast==null)return;toast.Clear();toast.Add(new Label(message));toast.style.display=DisplayStyle.Flex;toast.style.opacity=0f;toast.style.translate=new Translate(0,-10,0);toast.schedule.Execute(()=>{if(toast==null)return;toast.style.opacity=1f;toast.style.translate=new Translate(0,0,0);}).StartingIn(16);toast.schedule.Execute(()=>{if(toast==null)return;toast.style.opacity=0f;toast.style.translate=new Translate(0,-8,0);}).StartingIn(1700);toast.schedule.Execute(()=>{if(toast!=null)toast.style.display=DisplayStyle.None;}).StartingIn(1950);}

 VisualElement Milestone(int required,int current,string title,string description){var item=Container(current>=required?"ps-milestone ps-unlocked":"ps-milestone ps-locked");item.Add(PackspireUiFactory.Title($"Lv.{required}　{title}"));item.Add(PackspireUiFactory.Body(description));return item;}

 void BuildCompendium(){
  var shell=BookShell("図鑑",null);screenRoot.Add(shell);var pages=shell.Q<VisualElement>("book-pages");var left=Page("記録一覧");var right=Page("選択中の記録");pages.Add(left);pages.Add(right);
  var tabs=Container("ps-category-tabs");string[] names={"装備","役職","敵"};for(int i=0;i<3;i++){int tab=i;var b=PackspireUiFactory.Button(names[i],()=>{compendiumTab=tab;selectedCompendiumId="";BuildCompendiumAgain();});if(i==compendiumTab)b.AddToClassList("ps-selected");b.Add(InkRule());tabs.Add(b);}left.Add(tabs);
  var gridBody=Container("ps-compendium-grid");left.Add(gridBody);
  if(compendiumTab==0)BuildItemCompendium(gridBody,right);else if(compendiumTab==1)BuildRoleCompendium(gridBody,right);else BuildEnemyCompendium(gridBody,right);
  AddBookTabs(shell,ScreenId.Compendium);
 }

 void BuildCompendiumAgain(){screenRoot.Clear();BuildCompendium();}

 void BuildItemCompendium(VisualElement grid,VisualElement detail){var values=GameCatalog.Items.Values.ToArray();if(string.IsNullOrEmpty(selectedCompendiumId)||!GameCatalog.Items.ContainsKey(selectedCompendiumId))selectedCompendiumId=values[0].id;foreach(var item in values)grid.Add(AtlasButton(game.UiEquipmentArt,ItemUv(item.id),item.name,item.id==selectedCompendiumId,()=>{selectedCompendiumId=item.id;BuildCompendiumAgain();}));var selected=GameCatalog.Items[selectedCompendiumId];detail.Add(Atlas(game.UiEquipmentArt,ItemUv(selected.id),"ps-detail-art"));detail.Add(PackspireUiFactory.Title(selected.name));detail.Add(PackspireUiFactory.Body(selected.description));detail.Add(PackspireUiFactory.Body($"分類　{ItemTypeLabel(selected.type)}\n形状　{selected.cells.Length}マス\n属性　{string.Join("・",selected.cells.Select(x=>ElementLabel(x.element)))}"));if(!string.IsNullOrEmpty(selected.linkRule)){detail.Add(PackspireUiFactory.Title("LINK効果"));detail.Add(PackspireUiFactory.Body(selected.linkRule));}}

 void BuildRoleCompendium(VisualElement grid,VisualElement detail){var values=GameCatalog.Roles.Values.ToArray();if(string.IsNullOrEmpty(selectedCompendiumId)||!GameCatalog.Roles.ContainsKey(selectedCompendiumId))selectedCompendiumId=values[0].id;foreach(var role in values){bool known=game.UiMeta.jobLevels.Any(x=>x.id==role.id&&x.value>0);var tile=AtlasButton(game.UiRoleArt,RoleUv(role.id),known?role.name:"？？？",role.id==selectedCompendiumId,()=>{selectedCompendiumId=role.id;BuildCompendiumAgain();});if(!known)tile.AddToClassList("ps-unknown");grid.Add(tile);}var selected=GameCatalog.Roles[selectedCompendiumId];detail.Add(Atlas(game.UiRoleArt,RoleUv(selected.id),"ps-detail-art"));detail.Add(PackspireUiFactory.Title(selected.name));detail.Add(PackspireUiFactory.Body(selected.kind+"\n"+selected.description));detail.Add(PackspireUiFactory.Body($"最大レベル　{selected.maxLevel}\nLv.7　{game.UiRoleMilestone(selected.id,false)}\nLv.{selected.maxLevel}　{game.UiRoleMilestone(selected.id,true)}"));}

 void BuildEnemyCompendium(VisualElement grid,VisualElement detail){var values=GameCatalog.Enemies;if(string.IsNullOrEmpty(selectedCompendiumId)||!values.Any(x=>x.id==selectedCompendiumId))selectedCompendiumId=values[0].id;foreach(var enemy in values)grid.Add(AtlasButton(game.UiEnemyArt,EnemyUv(enemy.id),enemy.name,enemy.id==selectedCompendiumId,()=>{selectedCompendiumId=enemy.id;BuildCompendiumAgain();}));var selected=values.First(x=>x.id==selectedCompendiumId);detail.Add(Atlas(game.UiEnemyArt,EnemyUv(selected.id),"ps-detail-art"));detail.Add(PackspireUiFactory.Title(selected.name));detail.Add(PackspireUiFactory.Body($"危険度　{selected.tier}\n基礎HP　{selected.hp}\n行動候補　{string.Join("・",selected.damages.Select(x=>x==0?"特殊行動":$"攻撃{x}"))}"));}

 VisualElement BookShell(string title,VisualElement singleContent){nextPageIsLeft=true;var shell=Container("ps-book-screen");if(game.UiBookArt!=null)shell.Add(Image(game.UiBookArt,new Rect(0,0,1,1),"ps-book-background",ScaleMode.StretchToFill));var heading=new Label(title);heading.AddToClassList("ps-book-heading");shell.Add(heading);var pages=Container("ps-book-pages");pages.name="book-pages";shell.Add(pages);if(singleContent!=null)pages.Add(singleContent);return shell;}
 VisualElement Page(string title){var page=new ScrollView();page.AddToClassList("ps-book-page");page.AddToClassList(nextPageIsLeft?"ps-page-left":"ps-page-right");nextPageIsLeft=!nextPageIsLeft;var heading=Container("ps-page-heading");var label=PackspireUiFactory.Title(title);label.AddToClassList("ps-page-heading-title");heading.Add(label);heading.Add(InkRule());page.Add(heading);return page;}

 void AddBookTabs(VisualElement shell,ScreenId current){var tabs=Container("ps-book-tabs");(string,ScreenId)[] values={("拠点",ScreenId.Hub),("遠征",ScreenId.Expedition),("荷造り",ScreenId.Pack),("保管",ScreenId.Vault),("役職",ScreenId.Status),("勢力",ScreenId.Faction),("図鑑",ScreenId.Compendium)};foreach(var entry in values){var target=entry.Item2;var button=new Button(()=>game.UiNavigate(target)){tooltip=entry.Item1};button.AddToClassList("ps-book-tab");var label=new Label(entry.Item1);label.AddToClassList("ps-book-tab-label");button.Add(label);if(target==current){button.AddToClassList("ps-selected");button.SetEnabled(false);}tabs.Add(button);}shell.Add(tabs);}

 VisualElement RecordButton(string title,string subtitle,VisualElement art,bool selected,System.Action clicked){var button=new Button(clicked){tooltip=title+"\n"+subtitle};button.AddToClassList("ps-record-button");if(selected)button.AddToClassList("ps-selected");button.Add(art);var copy=Container("ps-record-copy");copy.Add(PackspireUiFactory.Title(title));copy.Add(PackspireUiFactory.Body(subtitle));button.Add(copy);if(selected)button.Add(SelectionBadge());button.Add(InkRule());return button;}
 VisualElement AtlasButton(Texture2D texture,Rect uv,string label,bool selected,System.Action clicked){var button=new Button(clicked){tooltip=label};button.AddToClassList("ps-atlas-button");if(selected)button.AddToClassList("ps-selected");button.Add(Atlas(texture,uv,"ps-atlas-image"));var name=new Label(label);name.AddToClassList("ps-atlas-label");button.Add(name);if(selected)button.Add(SelectionBadge());return button;}
 VisualElement SelectionBadge(){var badge=new Label("選択");badge.AddToClassList("ps-selection-badge");return badge;}
 VisualElement InkRule(){var rule=Container("ps-ink-rule");rule.pickingMode=PickingMode.Ignore;return rule;}

 VisualElement Container(string classes){var element=new VisualElement();foreach(var value in classes.Split(' '))element.AddToClassList(value);return element;}
 Image Image(Texture2D texture,Rect uv,string className,ScaleMode mode){var image=new Image{image=texture,uv=uv,scaleMode=mode,pickingMode=PickingMode.Ignore};image.AddToClassList(className);return image;}
 Image Atlas(Texture2D texture,Rect uv,string className)=>Image(texture,uv,className,ScaleMode.ScaleToFit);

 string FactionName(string id)=>GameCatalog.Factions.FirstOrDefault(x=>x.id==id)?.name??id;
 string BackpackName(string id)=>GameCatalog.Backpacks.FirstOrDefault(x=>x.id==id)?.name??id;
 string ItemTypeLabel(ItemType type)=>type==ItemType.Weapon?"武器":type==ItemType.Armor?"防具":type==ItemType.Rune?"ルーン":"消耗品";
 string ElementLabel(Element value)=>value==Element.Fire?"火":value==Element.Water?"水":value==Element.Wind?"風":"土";
 Rect ItemUv(string id){int index=id=="sword"?0:id=="shield"?1:id=="ember"?2:id=="herb"?3:id=="dagger"?4:id=="plate"?5:id=="crystal"?6:id=="bomb"?7:id=="spear"?8:id=="buckler"?9:id=="flask"?10:id=="charm"?11:0;return new Rect(index%4*.25f,(2-index/4)/3f,.25f,1f/3f);}
 Rect RoleUv(string id){int index=id=="warrior"?0:id=="guardian"?1:id=="scout"?2:id=="artificer"?3:id=="blade_master"?4:id=="bulwark"?5:id=="hunter"?6:id=="grand_artificer"?7:id=="arsenal_lord"?8:id=="pack_saint"?9:id=="rune_weaver"?10:id=="grid_dancer"?11:id.Contains("knight")?5:id.Contains("druid")?6:id.Contains("artificer")||id.Contains("channeler")?7:0;return new Rect(index%4*.25f,(2-index/4)/3f,.25f,1f/3f);}
 Rect EnemyUv(string id)=>id=="sentinel"?new Rect(0,.5f,.174f,.5f):id=="rats"?new Rect(.174f,.5f,.172f,.5f):id=="porter"?new Rect(.346f,.5f,.172f,.5f):id=="mage"?new Rect(.518f,.5f,.172f,.5f):id=="beast"?new Rect(0,0,.344f,.5f):id=="knight"?new Rect(.344f,0,.347f,.5f):new Rect(.69f,0,.31f,1);
 Rect CharacterUv(int body,int hair)=>new Rect(Mathf.Clamp(body,0,3)*.25f,(2-Mathf.Clamp(hair,0,2))/3f,.25f,1f/3f);
 Rect FactionUv(string id)=>id=="iron"?new Rect(0,.5f,.5f,.5f):id=="spore"?new Rect(.5f,.5f,.5f,.5f):id=="guild"?new Rect(0,0,.5f,.5f):new Rect(.5f,0,.5f,.5f);
 Rect DungeonUv(string id)=>id=="hollow_archive"?new Rect(.5f,.5f,.5f,.5f):id=="ash_forge"?new Rect(.5f,0,.5f,.5f):new Rect(0,.5f,.5f,.5f);

 public void PlayFor(ScreenId from,ScreenId to){if(transitionRoot==null||from==to)return;if(transitionRoutine!=null)StopCoroutine(transitionRoutine);transitionRoutine=StartCoroutine(Play(Resolve(from,to)));}
 TabletopTransitionKind Resolve(ScreenId from,ScreenId to){if(to==ScreenId.Map)return TabletopTransitionKind.ScrollUnfurl;if(to==ScreenId.Battle)return TabletopTransitionKind.BattleTable;if(IsBook(from)&&IsBook(to))return TabletopTransitionKind.PageTurn;return TabletopTransitionKind.Fade;}
 bool IsBook(ScreenId value)=>value==ScreenId.Character||value==ScreenId.Status||value==ScreenId.Vault||value==ScreenId.Reward||value==ScreenId.Shop||value==ScreenId.Event||value==ScreenId.Compendium||value==ScreenId.GameOver;

 IEnumerator Play(TabletopTransitionKind kind){transitionRoot.style.display=DisplayStyle.Flex;ResetLayers();const float duration=.54f;float elapsed=0f;while(elapsed<duration){elapsed+=Time.unscaledDeltaTime;float t=Mathf.Clamp01(elapsed/duration),ease=1f-Mathf.Pow(1f-t,3f);ApplyFrame(kind,ease);yield return null;}HideTransition();transitionRoutine=null;}
 void ResetLayers(){dim.style.display=DisplayStyle.Flex;dim.style.opacity=.68f;leftPaper.style.display=DisplayStyle.None;rightPaper.style.display=DisplayStyle.None;scrollPaper.style.display=DisplayStyle.None;battleShade.style.display=DisplayStyle.None;}
 void ApplyFrame(TabletopTransitionKind kind,float t){dim.style.opacity=Mathf.Lerp(.68f,0f,t);if(kind==TabletopTransitionKind.PageTurn){leftPaper.style.display=DisplayStyle.Flex;rightPaper.style.display=DisplayStyle.Flex;float remaining=1f-t;leftPaper.style.width=Length.Percent(50f*remaining);leftPaper.style.left=Length.Percent(50f-50f*remaining);rightPaper.style.width=Length.Percent(50f*remaining);rightPaper.style.left=Length.Percent(50f);leftPaper.style.opacity=remaining;rightPaper.style.opacity=remaining;}else if(kind==TabletopTransitionKind.ScrollUnfurl){scrollPaper.style.display=DisplayStyle.Flex;float remaining=1f-t;scrollPaper.style.width=Length.Percent(88f);scrollPaper.style.left=Length.Percent(6f);scrollPaper.style.height=Length.Percent(82f*remaining);scrollPaper.style.top=Length.Percent(9f+41f*t);scrollPaper.style.opacity=remaining;}else if(kind==TabletopTransitionKind.BattleTable){battleShade.style.display=DisplayStyle.Flex;battleShade.style.opacity=1f-t;battleShade.style.left=Length.Percent(48f*t);battleShade.style.right=Length.Percent(48f*t);}}
 void HideTransition(){if(transitionRoot!=null)transitionRoot.style.display=DisplayStyle.None;}
}
}
