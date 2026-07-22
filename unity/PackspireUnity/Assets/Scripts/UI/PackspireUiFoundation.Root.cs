using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire {
public sealed partial class PackspireUiFoundation {
 void BuildRoot(){
  root=document.rootVisualElement;if(root==null||root.panel==null){uiReady=false;Debug.LogWarning("Packspire UI Toolkit root is not ready.");return;}
  root.name="packspire-ui-root";root.AddToClassList("packspire-root");root.pickingMode=PickingMode.Ignore;
  string[] styleSheetPaths={
   "UI/PackspireTheme",
   "UI/PackspirePacking",
   "UI/PackspireRoute",
   "UI/PackspireRoster",
   "UI/PackspireBattle",
   "UI/PackspirePolish",
   "UI/PackspireHub"
  };
  foreach(var path in styleSheetPaths){
   var sheet=Resources.Load<StyleSheet>(path);
   if(sheet!=null)root.styleSheets.Add(sheet);
   else Debug.LogWarning($"Missing Packspire style sheet: {path}");
  }
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
  var grid=Container("ps-dev-current-grid");
  AddDeveloperDestination(grid,"拠点",ScreenId.Hub);
  AddDeveloperDestination(grid,"遠征準備",ScreenId.Expedition);
  AddDeveloperDestination(grid,"荷造り",ScreenId.Pack);
  AddDeveloperDestination(grid,"保管庫",ScreenId.Vault);
  AddDeveloperDestination(grid,"ステータス",ScreenId.Status);
  AddDeveloperDestination(grid,"勢力",ScreenId.Faction);
  AddDeveloperDestination(grid,"図鑑",ScreenId.Compendium);
  AddDeveloperDestination(grid,"キャラ選択",ScreenId.Character);
  developerPanelRoot.Add(grid);
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
  if(open){
   developerPanelRoot.BringToFront();
   if(developerAccessButton!=null)developerAccessButton.BringToFront();
  }
 }

 VisualElement Layer(string classes){var element=new VisualElement{pickingMode=PickingMode.Ignore};foreach(var value in classes.Split(' '))element.AddToClassList(value);return element;}
}
}
