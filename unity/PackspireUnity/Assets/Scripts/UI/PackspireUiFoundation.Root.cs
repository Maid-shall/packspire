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
  developerAccessButton=PackspireUiFactory.Button("DEV",()=>game.UiToggleDeveloperPanel());
  developerAccessButton.AddToClassList("ps-dev-access-global");
  root.Add(developerAccessButton);

  developerPanelRoot=Container("ps-dev-panel-current");
  developerPanelRoot.Add(PackspireUiFactory.Title("開発者メニュー"));
  developerPanelRoot.Add(PackspireUiFactory.Body("現行UIの画面だけを直接確認します。"));
  var grid=Container("ps-dev-current-grid");
  AddDeveloperDestination(grid,"2.5D拠点",ScreenId.Hub);
  AddDeveloperDestination(grid,"遠征準備",ScreenId.Expedition);
  AddDeveloperDestination(grid,"荷造り",ScreenId.Pack);
  AddDeveloperDestination(grid,"保管庫",ScreenId.Vault);
  AddDeveloperDestination(grid,"役職",ScreenId.Status);
  AddDeveloperDestination(grid,"勢力",ScreenId.Faction);
  AddDeveloperDestination(grid,"図鑑",ScreenId.Compendium);
  AddDeveloperDestination(grid,"人物作成",ScreenId.Character);
  developerPanelRoot.Add(grid);
  var close=PackspireUiFactory.Button("閉じる",()=>game.UiToggleDeveloperPanel());
  close.AddToClassList("ps-dev-current-close");
  developerPanelRoot.Add(close);
  root.Add(developerPanelRoot);
  RefreshDeveloperOverlay();
 }

 void AddDeveloperDestination(VisualElement grid,string label,ScreenId target){
  var button=PackspireUiFactory.Button(label,()=>{
   game.UiNavigate(target);
   if(game.UiDeveloperPanelOpen)game.UiToggleDeveloperPanel();
  });
  button.AddToClassList("ps-dev-current-button");
  grid.Add(button);
 }

 void RefreshDeveloperOverlay(){
  if(developerAccessButton==null||developerPanelRoot==null||game==null)return;
  bool open=game.UiDeveloperPanelOpen;
  developerAccessButton.text=open?"DEV ×":"DEV";
  developerPanelRoot.style.display=open?DisplayStyle.Flex:DisplayStyle.None;
 }

 VisualElement Layer(string classes){var element=new VisualElement{pickingMode=PickingMode.Ignore};foreach(var value in classes.Split(' '))element.AddToClassList(value);return element;}
}
}
