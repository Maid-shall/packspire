using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire {
public sealed partial class PackspireUiFoundation {
 void BuildRoot(){
  root=document.rootVisualElement;if(root==null||root.panel==null){uiReady=false;Debug.LogWarning("Packspire UI Toolkit root is not ready; legacy UI remains active.");return;}
  root.name="packspire-ui-root";root.AddToClassList("packspire-root");root.pickingMode=PickingMode.Ignore;
  var theme=Resources.Load<StyleSheet>("UI/PackspireTheme");if(theme!=null)root.styleSheets.Add(theme);
  screenRoot=new VisualElement{name="screen-root",pickingMode=PickingMode.Position};screenRoot.AddToClassList("ps-screen-host");root.Add(screenRoot);
  transitionRoot=new VisualElement{name="transition-root",pickingMode=PickingMode.Ignore};transitionRoot.AddToClassList("ps-transition-host");root.Add(transitionRoot);
  dim=Layer("transition-dim");leftPaper=Layer("transition-paper transition-paper-left");rightPaper=Layer("transition-paper transition-paper-right");scrollPaper=Layer("transition-scroll");battleShade=Layer("transition-battle");
  transitionRoot.Add(dim);transitionRoot.Add(leftPaper);transitionRoot.Add(rightPaper);transitionRoot.Add(scrollPaper);transitionRoot.Add(battleShade);HideTransition();toast=Container("ps-toast");toast.style.display=DisplayStyle.None;root.Add(toast);BuildDeveloperOverlay();uiReady=true;
 }

 void BuildDeveloperOverlay(){
  // Normal play: F10 only. Dev builds keep a tiny corner hint (not a DEV dock).
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
  developerPanelRoot.Add(PackspireUiFactory.Body("F10で開閉。閉じると直前の画面へ戻ります。ジャンプは状態を勝手に全解放しません。"));
  var grid=Container("ps-dev-current-grid");
  AddDeveloperDestination(grid,"2.5D拠点",ScreenId.Hub);
  AddDeveloperDestination(grid,"遠征準備",ScreenId.Expedition);
  AddDevAction(grid,"新規ルート試作",()=>{game.UiDevFreshRouteSlice();ForceRefreshScreen();});
  AddDevAction(grid,"地点・入口",()=>DevJumpRoute(ExplorationRouteCatalog.CellEntrance));
  AddDevAction(grid,"地点・分岐",()=>DevJumpRoute(ExplorationRouteCatalog.CellFork));
  AddDevAction(grid,"地点・breach前",()=>DevJumpRoute(ExplorationRouteCatalog.CellBreachFrom));
  AddDevAction(grid,"地点・hidden前",()=>DevJumpRoute(ExplorationRouteCatalog.CellHiddenFrom));
  AddDevAction(grid,"地点・戦闘1",()=>DevJumpRoute(ExplorationRouteCatalog.CellBattle));
  AddDevAction(grid,"地点・戦闘2",()=>DevJumpRoute(ExplorationRouteCatalog.CellBattle2));
  AddDevAction(grid,"地点・戦闘3",()=>DevJumpRoute(ExplorationRouteCatalog.CellBattle3));
  AddDevAction(grid,"地点・建物内部",()=>DevJumpRoute(ExplorationRouteCatalog.CellBreachTo,true));
  AddDevAction(grid,"操作・breach開通",()=>{game.UiDevOpenBreach();ForceRefreshScreen();});
  AddDevAction(grid,"操作・hidden発見",()=>{game.UiDevRevealHidden();ForceRefreshScreen();});
  AddDevAction(grid,"操作・戦闘開始",()=>{game.UiDevOpenRouteBattle();ForceRefreshScreen();});
  AddDevAction(grid,"軸・警戒+3",()=>DevAxisNudge(3,0,0));
  AddDevAction(grid,"軸・崩壊+3",()=>DevAxisNudge(0,3,0));
  AddDevAction(grid,"軸・侵蝕+3",()=>DevAxisNudge(0,0,3));
  AddDevAction(grid,"軸・最低へ",()=>DevAxisSet(-15,-15,-15));
  AddDevAction(grid,"軸・中央へ",()=>DevAxisSet(0,0,0));
  AddDevAction(grid,"軸・限界直前",()=>DevAxisSet(13,13,13));
  AddDevAction(grid,"軸・限界へ",()=>DevAxisSet(15,15,15));
  AddDevAction(grid,"軸・予測表示",()=>DevAxisPreview(false));
  AddDevAction(grid,"軸・予測不能",()=>DevAxisPreview(true));
  AddDevAction(grid,"会話・通常",()=>DevRouteDialogue(3));
  AddDevAction(grid,"会話・吹き出し",()=>DevRouteBubble());
  AddDevAction(grid,"会話・2択",()=>DevRouteDialogue(2));
  AddDevAction(grid,"会話・5択",()=>DevRouteDialogue(5));
  AddDevAction(grid,"表示・旧探索地図",()=>{game.UiDevOpenOldMap();ForceRefreshScreen();});
  AddDevAction(grid,"表示・術式図",()=>{game.UiDevOpenRiteDebug();ForceRefreshScreen();});
  AddDevAction(grid,"表示・旧戦闘",()=>{game.UiDevOpenOldBattle();ForceRefreshScreen();});
  AddDeveloperDestination(grid,"荷造り",ScreenId.Pack);
  AddDeveloperDestination(grid,"保管庫",ScreenId.Vault);
  AddDeveloperDestination(grid,"役職",ScreenId.Status);
  AddDeveloperDestination(grid,"勢力",ScreenId.Faction);
  AddDeveloperDestination(grid,"図鑑",ScreenId.Compendium);
  AddDeveloperDestination(grid,"人物作成",ScreenId.Character);
  developerPanelRoot.Add(grid);
  var close=PackspireUiFactory.Button("閉じる（直前へ戻る）",()=>game.UiToggleDeveloperPanel());
  close.AddToClassList("ps-dev-current-close");
  developerPanelRoot.Add(close);
  root.Add(developerPanelRoot);
  RefreshDeveloperOverlay();
 }

 void DevJumpRoute(int cellId,bool interior=false){
  game.UiDevJumpExplorationCell(cellId,interior);
  ForceRefreshScreen();
 }

 void DevAxisNudge(int alert,int collapse,int corruption){
  if(game.UiRun?.axes==null)return;
  game.UiRun.axes.Change(alert,collapse,corruption);
  DevRefreshAxes();
 }

 void DevAxisSet(int alert,int collapse,int corruption){
  if(game.UiRun?.axes==null)return;
  var ax=game.UiRun.axes;
  ax.alert=ax.Clamp(alert);
  ax.collapse=ax.Clamp(collapse);
  ax.corruption=ax.Clamp(corruption);
  DevRefreshAxes();
 }

 void DevRefreshAxes(){
  var ax=game.UiRun?.axes;
  routeAxisInstrument?.CommitAnimatedValue(ax);
  RefreshExplorationHud();
  if(ax!=null)
   ShowToast($"DEV軸 警戒{ax.alert} 崩壊{ax.collapse} 侵蝕{ax.corruption}");
 }

 void DevAxisPreview(bool unknown){
  if(routeAxisInstrument==null){ShowToast("遠征画面で開いてください");return;}
  routeAxisInstrument.ShowPreview(unknown
   ?RouteAxisForecast.Values.Of(0,0,0,true)
   :RouteAxisForecast.Values.Of(3,2,1));
  ShowToast(unknown?"DEV: 予測不能":"DEV: 予測表示");
 }

 void DevRouteDialogue(int choiceCount){
  if(routeDialogue==null){ShowToast("遠征画面で開いてください");return;}
  choiceCount=Mathf.Clamp(choiceCount,2,5);
  var lines=new string[choiceCount];
  for(int i=0;i<choiceCount;i++)lines[i]=$"選択肢 {i+1} — 観測器の反応を確認する";
  routeDialogue.SetCallbacks(
   i=>{
    routeDialogue.Hide();
    routeAxisInstrument?.ClearPreview();
    routeAxisInstrument?.SetDimmed(false);
    // Restore live event callbacks after DEV probe.
    routeDialogue.SetCallbacks(ResolveRouteEventChoice,HoverRouteEventChoice,()=>routeAxisInstrument?.ClearPreview());
    ShowToast($"DEV選択 {i+1}");
   },
   i=>routeAxisInstrument?.ShowPreview(i==choiceCount-1
    ?RouteAxisForecast.Values.Of(0,0,0,true)
    :RouteAxisForecast.ForEventChoice(i%3)),
   ()=>routeAxisInstrument?.ClearPreview());
  routeDialogue.ShowConversation("DEV会話",$"通常会話レイヤー（{choiceCount}択）。数値は出さず、三軸の予測だけが動きます。",lines);
  routeAxisInstrument?.SetDimmed(true);
  routeDialogue.Root?.BringToFront();
  routeAxisInstrument?.Root?.BringToFront();
 }

 void DevRouteBubble(){
  if(routeDialogue==null){ShowToast("遠征画面で開いてください");return;}
  routeDialogue.ShowBubble("…この先で何かが動いた。");
 }

 void AddDevAction(VisualElement grid,string label,System.Action action){
  var button=PackspireUiFactory.Button(label,action);
  button.AddToClassList("ps-dev-current-button");
  grid.Add(button);
 }

 void AddDeveloperDestination(VisualElement grid,string label,ScreenId target){
  var button=PackspireUiFactory.Button(label,()=>{
   game.UiNavigate(target);
   game.UiDevCloseWithoutRestore();
   ForceRefreshScreen();
  });
  button.AddToClassList("ps-dev-current-button");
  grid.Add(button);
 }

 void RefreshDeveloperOverlay(){
  if(developerPanelRoot==null||game==null)return;
  bool open=game.UiDeveloperPanelOpen;
  if(developerAccessButton!=null){
   developerAccessButton.text=open?"F10 ×":"F10 DEV";
   developerAccessButton.style.display=DisplayStyle.Flex;
  }
  developerPanelRoot.style.display=open?DisplayStyle.Flex:DisplayStyle.None;
 }

 VisualElement Layer(string classes){var element=new VisualElement{pickingMode=PickingMode.Ignore};foreach(var value in classes.Split(' '))element.AddToClassList(value);return element;}
}
}
