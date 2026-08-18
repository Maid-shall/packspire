using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire {
public sealed partial class PackspireUiFoundation {
 void BuildRoot(){
  root=document.rootVisualElement;if(root==null||root.panel==null){uiReady=false;Debug.LogWarning("Packspire UI Toolkit root is not ready.");return;}
  root.name="packspire-ui-root";root.AddToClassList("packspire-root");root.pickingMode=PickingMode.Ignore;
  for(int i=root.styleSheets.count-1;i>=0;i--){
   var attached=root.styleSheets[i];
   if(attached!=null&&attached.name.StartsWith("Packspire"))root.styleSheets.Remove(attached);
  }
  // Only foundation chrome belongs at document scope. Screen styling is
  // attached to screenRoot by the router so unrelated views cannot override it.
  AddStyleSheet(root,"UI/PackspireTheme");
  screenRoot=new VisualElement{name="screen-root",pickingMode=PickingMode.Position};screenRoot.AddToClassList("ps-screen-host");root.Add(screenRoot);
  transitionRoot=new VisualElement{name="transition-root",pickingMode=PickingMode.Ignore};transitionRoot.AddToClassList("ps-transition-host");root.Add(transitionRoot);
  dim=Layer("transition-dim");leftPaper=Layer("transition-paper transition-paper-left");rightPaper=Layer("transition-paper transition-paper-right");scrollPaper=Layer("transition-scroll");battleShade=Layer("transition-battle");
  transitionRoot.Add(dim);transitionRoot.Add(leftPaper);transitionRoot.Add(rightPaper);transitionRoot.Add(scrollPaper);transitionRoot.Add(battleShade);HideTransition();toast=Container("ps-toast");toast.style.display=DisplayStyle.None;root.Add(toast);BuildNavHud();BuildDeveloperOverlay();uiReady=true;
 }

 void BuildDeveloperOverlay(){
#if UNITY_EDITOR || DEVELOPMENT_BUILD
  developerAccessButton=PackspireUiFactory.Button("F10 DEV",()=>game.UiToggleDeveloperPanel());
  developerAccessButton.AddToClassList("ps-dev-access-global");
  developerAccessButton.AddToClassList("ps-dev-access-hint");
  root.Add(developerAccessButton);
#else
  developerAccessButton=null;
#endif

  developerPanelRoot=Container("ps-dev-panel-current");
  developerPanelRoot.Add(PackspireUiFactory.Title("開発者メニュー"));
  developerPanelRoot.Add(PackspireUiFactory.Body("F10で開閉。閉じると直前の画面へ戻ります。"));
  var scroll=new ScrollView(ScrollViewMode.Vertical);
  scroll.AddToClassList("ps-dev-current-scroll");
  scroll.verticalScrollerVisibility=ScrollerVisibility.Auto;
  scroll.horizontalScrollerVisibility=ScrollerVisibility.Hidden;
  var grid=Container("ps-dev-current-grid");
  AddDeveloperDestination(grid,"拠点",ScreenId.Hub);
  AddDeveloperDestination(grid,"遠征準備",ScreenId.Expedition);
  AddDeveloperDestination(grid,"荷造り",ScreenId.Pack);
  AddDeveloperDestination(grid,"保管庫",ScreenId.Vault);
  AddDeveloperDestination(grid,"家宝",ScreenId.Heirloom);
  AddDeveloperDestination(grid,"ステータス",ScreenId.Status);
  AddDeveloperDestination(grid,"勢力",ScreenId.Faction);
  AddDeveloperDestination(grid,"図鑑",ScreenId.Compendium);
  AddDeveloperDestination(grid,"キャラ選択",ScreenId.Character);
  AddDevAction(grid,"商店(DEV)",OpenShopPreviewFromDev);
  AddDevAction(grid,"報酬(DEV)",OpenRewardPreviewFromDev);
  AddDevAction(grid,"旧・配達経路台帳(LEGACY)",()=>DevNavigate(ScreenId.Route,()=>game.UiDevOpenCourierRoute()));
  AddDevAction(grid,"シームレス遠征 完成版(DEV)",OpenJourneyAnimationPrototype);
  AddDevAction(grid,"遠征景色・灰市外縁(DEV)",()=>OpenJourneySceneryPreview(0));
  AddDevAction(grid,"遠征景色・水没書庫(DEV)",()=>OpenJourneySceneryPreview(1));
  AddDevAction(grid,"遠征景色・黒鐘区画(DEV)",()=>OpenJourneySceneryPreview(2));
  AddDevAction(grid,"遠征戦闘・1体(DEV)",()=>OpenJourneyBattlePreview(1,false));
  AddDevAction(grid,"遠征戦闘・防御表示(DEV)",()=>OpenJourneyBattlePreview(1,false,0,5));
  AddDevAction(grid,"遠征戦闘・3体レイアウト(DEV)",()=>OpenJourneyBattlePreview(3,false));
  AddDevAction(grid,"遠征戦闘・手札10枚レイアウト(DEV)",()=>OpenJourneyBattlePreview(1,false,10));
  AddDevAction(grid,"遠征戦闘・攻撃予兆(DEV)",()=>OpenJourneyBattlePreview(1,true));
  scroll.Add(grid);
  developerPanelRoot.Add(scroll);
  var close=PackspireUiFactory.Button("閉じる（直前へ戻る）",()=>game.UiToggleDeveloperPanel());
  close.AddToClassList("ps-dev-current-close");
  developerPanelRoot.Add(close);
  root.Add(developerPanelRoot);
  RefreshDeveloperOverlay();
 }

 void AddDevAction(VisualElement grid,string label,System.Action action){
  var button=PackspireUiFactory.Button(label,action);
  button.AddToClassList("ps-dev-current-button");
  grid.Add(button);
 }

 void OpenJourneyAnimationPrototype(){
   JourneyDeveloperPreviewController.Clear();
   game.UiLaunchSeamlessJourneyDeveloperSession();
 }

 void OpenJourneyBattlePreview(int enemyCount,bool startDefense,int handSize=0,int playerBlock=0){
   JourneyDeveloperPreviewController.QueueBattle(enemyCount,startDefense,handSize,playerBlock);
   game.UiLaunchSeamlessJourneyDeveloperSession();
 }

 void OpenJourneySceneryPreview(int biomeIndex){
   JourneyDeveloperPreviewController.QueueScenery(biomeIndex);
   game.UiLaunchSeamlessJourneyDeveloperSession();
 }

 void AddDeveloperDestination(VisualElement grid,string label,ScreenId target){
  var button=PackspireUiFactory.Button(label,()=>DevNavigate(target));
  button.AddToClassList("ps-dev-current-button");
  grid.Add(button);
 }

 /// <summary>QA jump: one navigate + one rebuild. Skips PlayFor and AnimateScreenIn to avoid flash/double build.</summary>
 public void DevNavigate(ScreenId target,System.Action prepare=null){
  prepare?.Invoke();
  skipNextTransition=true;
  skipNextAnimateIn=true;
  game.UiNavigate(target);
  game.UiDevCloseWithoutRestore();
  if(uiReady)RefreshScreen(true);
 }

 /// <summary>QA rebuild of current screen without transition flash (map/battle open helpers).</summary>
 public void DevRefreshCurrent(){
  skipNextTransition=true;
  skipNextAnimateIn=true;
  ForceRefreshScreen();
 }

 void RefreshDeveloperOverlay(){
  if(developerPanelRoot==null||game==null)return;
  bool open=game.UiDeveloperPanelOpen;
  if(developerOverlayStateKnown&&lastDeveloperOverlayOpen==open)return;
  developerOverlayStateKnown=true;
  lastDeveloperOverlayOpen=open;
  if(developerAccessButton!=null){
   developerAccessButton.text=open?"F10 ×":"F10 DEV";
   developerAccessButton.style.display=DisplayStyle.Flex;
  }
  developerPanelRoot.style.display=open?DisplayStyle.Flex:DisplayStyle.None;
  if(open){
   developerPanelRoot.BringToFront();
   if(developerAccessButton!=null)developerAccessButton.BringToFront();
  }
 }

 VisualElement Layer(string classes){var element=new VisualElement{pickingMode=PickingMode.Ignore};foreach(var value in classes.Split(' '))element.AddToClassList(value);return element;}
}
}
