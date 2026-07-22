using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire {
public sealed partial class PackspireUiFoundation {
 VisualElement navHudRoot;
 VisualElement navMenuDrawer;
 VisualElement navMenuBackdrop;
 Button navBackButton;
 Button navMenuButton;
 bool navMenuOpen;
 bool navSuppressHistory;
 readonly Stack<ScreenId> navBackStack=new();

 static readonly (ScreenId id,string label)[] NavDestinations={
  (ScreenId.Hub,"拠点"),
  (ScreenId.Expedition,"遠征準備"),
  (ScreenId.Pack,"荷造り"),
  (ScreenId.Vault,"保管庫"),
  (ScreenId.Heirloom,"家宝"),
  (ScreenId.Status,"ステータス"),
  (ScreenId.Faction,"勢力"),
  (ScreenId.Compendium,"図鑑"),
  (ScreenId.Character,"キャラクター"),
 };

 void BuildNavHud(){
  navHudRoot=Container("ps-nav-hud ps-layer-navigation");
  navHudRoot.pickingMode=PickingMode.Ignore;
  navHudRoot.style.display=DisplayStyle.None;

  navBackButton=new Button(NavGoBack){text="← 戻る"};
  navBackButton.AddToClassList("ps-nav-back");
  navBackButton.AddToClassList("ps-chrome-action");
  navBackButton.pickingMode=PickingMode.Position;
  navHudRoot.Add(navBackButton);

  navMenuButton=new Button(ToggleNavMenu){text="☰"};
  navMenuButton.AddToClassList("ps-nav-menu-btn");
  navMenuButton.AddToClassList("ps-chrome-action");
  navMenuButton.tooltip="管理メニュー";
  navMenuButton.pickingMode=PickingMode.Position;
  navHudRoot.Add(navMenuButton);

  navMenuBackdrop=Container("ps-nav-menu-backdrop");
  navMenuBackdrop.pickingMode=PickingMode.Position;
  navMenuBackdrop.style.display=DisplayStyle.None;
  navMenuBackdrop.RegisterCallback<ClickEvent>(evt=>{
   if(evt.target==navMenuBackdrop)CloseNavMenu();
  });
  navHudRoot.Add(navMenuBackdrop);

  navMenuDrawer=Container("ps-nav-menu-drawer");
  navMenuDrawer.pickingMode=PickingMode.Position;
  navMenuDrawer.style.display=DisplayStyle.None;
  navMenuDrawer.RegisterCallback<ClickEvent>(evt=>evt.StopPropagation());
  var menuTitle=new Label("管理メニュー"){pickingMode=PickingMode.Ignore};
  menuTitle.AddToClassList("ps-nav-menu-title");
  navMenuDrawer.Add(menuTitle);
  foreach(var dest in NavDestinations){
   var entry=dest;
   var item=new Button(()=>NavMenuGo(entry.id)){text=entry.label,userData=entry.id};
   item.AddToClassList("ps-nav-menu-item");
   item.AddToClassList("ps-chrome-action");
   navMenuDrawer.Add(item);
  }
  navHudRoot.Add(navMenuDrawer);
  root.Add(navHudRoot);
 }

 void HandleNavInput(){
  if(!uiReady||game==null||!ShouldShowNavHud())return;
  if(Input.GetKeyDown(KeyCode.Escape)){
   if(TryCloseHeirloomPickerFromInput())return;
   NavGoBack();
  }
 }

 void RecordNavHistory(ScreenId from,ScreenId to){
  if(navSuppressHistory||from==to)return;
  if(to==ScreenId.Hub){
   navBackStack.Clear();
   CloseNavMenu();
   return;
  }
  if(!IsNavHistoryScreen(from)||!IsNavHistoryScreen(to))return;
  if(navBackStack.Count>0&&navBackStack.Peek()==to)return;
  navBackStack.Push(from);
 }

 bool IsNavHistoryScreen(ScreenId id){
  if(id==ScreenId.Pack)return game!=null&&game.UiPackingAtBase;
  return id is ScreenId.Hub or ScreenId.Status or ScreenId.Vault or ScreenId.Heirloom or ScreenId.Faction
   or ScreenId.Expedition or ScreenId.Compendium or ScreenId.Character;
 }

 bool IsManagementNavScreen(ScreenId id)=>IsNavHistoryScreen(id)&&id!=ScreenId.Hub;

 bool ShouldShowNavHud(){
  if(game==null)return false;
  if(game.UiScreen==ScreenId.Hub)return false;
  if(!game.UiMeta.characterMade&&game.UiScreen==ScreenId.Character)return false;
  if(game.UiExploration!=null)return false;
  if(game.UiExplorationEventActive)return false;
  switch(game.UiScreen){
   case ScreenId.Map:
   case ScreenId.Battle:
   case ScreenId.Reward:
   case ScreenId.Shop:
   case ScreenId.Event:
   case ScreenId.GameOver:
    return false;
   case ScreenId.Pack:
    return game.UiPackingAtBase;
   default:
    return IsManagementNavScreen(game.UiScreen);
  }
 }

 void UpdateNavHud(){
  if(navHudRoot==null)return;
  bool show=ShouldShowNavHud();
  navHudRoot.style.display=show?DisplayStyle.Flex:DisplayStyle.None;
  root?.EnableInClassList("ps-nav-visible",show);
  if(!show){
   CloseNavMenu();
   return;
  }
  navHudRoot.pickingMode=PickingMode.Ignore;
  navBackButton.SetEnabled(navBackStack.Count>0);
  foreach(var child in navMenuDrawer.Children()){
   if(child is not Button item||item.userData is not ScreenId id)continue;
   bool current=id==game.UiScreen;
   item.EnableInClassList("ps-selected",current);
   item.SetEnabled(NavDestinationAvailable(id));
  }
  if(show)navHudRoot.BringToFront();
 }

 bool NavDestinationAvailable(ScreenId id){
  if(id==ScreenId.Character&&!game.UiMeta.characterMade)return false;
  if(id==ScreenId.Pack)return game.UiPackingAtBase||game.UiScreen==ScreenId.Hub||game.UiRun==null;
  return true;
 }

 void NavGoBack(){
  if(navMenuOpen){CloseNavMenu();return;}
  if(TryCloseHeirloomPickerFromInput())return;
  if(navBackStack.Count==0)return;
  NavNavigate(navBackStack.Pop(),true);
 }

 void ToggleNavMenu(){
  if(navMenuOpen)CloseNavMenu();
  else OpenNavMenu();
 }

 void OpenNavMenu(){
  navMenuOpen=true;
  navMenuDrawer.style.display=DisplayStyle.Flex;
  navMenuBackdrop.style.display=DisplayStyle.Flex;
  navMenuBackdrop.pickingMode=PickingMode.Position;
  navMenuDrawer.BringToFront();
  navMenuButton.text="×";
 }

 void CloseNavMenu(){
  navMenuOpen=false;
  if(navMenuDrawer!=null)navMenuDrawer.style.display=DisplayStyle.None;
  if(navMenuBackdrop!=null){
   navMenuBackdrop.style.display=DisplayStyle.None;
   navMenuBackdrop.pickingMode=PickingMode.Ignore;
  }
  if(navMenuButton!=null)navMenuButton.text="☰";
 }

 void NavMenuGo(ScreenId target){
  CloseNavMenu();
  if(target==game.UiScreen)return;
  if(!NavDestinationAvailable(target))return;
  NavNavigate(target,false);
 }

 void NavNavigate(ScreenId target,bool suppressHistory){
  if(game.UiScreen==ScreenId.Pack&&game.UiPackingAtBase)game.UiPackingCapture();
  navSuppressHistory=suppressHistory;
  game.UiNavigate(target);
  hasRenderedScreen=false;
  RefreshScreen(true);
  navSuppressHistory=false;
 }

 VisualElement BuildScreenLayerShell(string screenClass,System.Action<VisualElement> buildContent){
  var shell=Container(screenClass+" ps-dark-surface");
  var backgroundHost=Container("ps-layer-background");
  var bg=HubBackgroundArt();
  if(bg==null)bg=CourtyardArt();
  if(bg!=null)backgroundHost.Add(Image(bg,new Rect(0,0,1,1),"ps-mgmt-bg",ScaleMode.ScaleAndCrop));
  var shade=Container("ps-mgmt-shade");
  shade.pickingMode=PickingMode.Ignore;
  backgroundHost.Add(shade);
  shell.Add(backgroundHost);
  var contentHost=Container("ps-layer-content");
  buildContent?.Invoke(contentHost);
  shell.Add(contentHost);
  return shell;
 }
}
}
