using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire {
public sealed partial class PackspireUiFoundation {
 ExplorationRouteStage explorationRouteStage;
 ExplorationMapStage explorationStage;
 Image explorationView;
 VisualElement explorationRoot,explorationMist,explorationFinishDialog,explorationCompassFace;
 VisualElement explorationNeedleAlert,explorationNeedleCollapse,explorationNeedleCorruption;
 VisualElement explorationSketch,explorationDock,explorationHud,explorationHintGuide,explorationTopActions;
 VisualElement explorationMiniHud,explorationAxisMeter,explorationDetailPopup,explorationConfirmPopup,explorationAxisTip;
 Label explorationTitleLabel,explorationTypeLabel,explorationStatusLabel,explorationBodyLabel,explorationHintLabel;
 Label explorationAxisAlert,explorationAxisCollapse,explorationAxisCorruption,explorationMapNameLabel,explorationStatsLabel;
 Label explorationHudTitle,explorationHudStatus,explorationHudAxes,explorationHintGuideLabel;
 Label explorationMiniName,explorationMiniDistrict,explorationMiniBearing,explorationDetailBody;
 Label explorationConfirmTitle,explorationConfirmKind,explorationConfirmBody,explorationConfirmDelta,explorationAxisTipBody;
 Button explorationViewModeButton,explorationFocusButton,explorationFinishButton,explorationInfoButton;
 RouteAxisInstrument routeAxisInstrument;
 RouteDialoguePresenter routeDialogue;
 bool explorationMapBuilt;
 bool explorationUseRiteView;
 Vector2 explorationPointerDown;
 bool explorationDragging;
 int explorationPointerId=-1;
 int explorationLastAlert,explorationLastCollapse,explorationLastCorruption;
 int explorationPendingConfirmId=-1;
 int explorationHoverExitId=-1;
 float explorationAnomalyT,explorationAxisDeltaT;
 string explorationAxisDeltaText="";

 bool ExplorationRouteActive=>!explorationUseRiteView&&explorationRouteStage!=null;
 bool ExplorationAnyMoving=>ExplorationRouteActive?explorationRouteStage.IsMoving:(explorationStage!=null&&explorationStage.IsMoving);

 void EnsureExplorationStage(){
  if(explorationRouteStage==null){
   var routeHost=new GameObject("ExplorationRouteHost");
   routeHost.transform.SetParent(transform,false);
   explorationRouteStage=routeHost.AddComponent<ExplorationRouteStage>();
   explorationRouteStage.Arrived+=OnExplorationArrived;
  }
  if(explorationStage==null){
   var riteHost=new GameObject("ExplorationMapHost");
   riteHost.transform.SetParent(transform,false);
   explorationStage=riteHost.AddComponent<ExplorationMapStage>();
   explorationStage.Arrived+=OnExplorationArrived;
  }
 }

 void SuspendExplorationStage(){
  explorationMapBuilt=false;
  InvalidateRouteOverlayUi();
  explorationHintGuide=null;explorationHintGuideLabel=null;explorationTopActions=null;
  explorationFocusButton=null;explorationFinishButton=null;explorationInfoButton=null;
  explorationView=null;
  explorationRoot=null;
  explorationMist=null;
  explorationFinishDialog=null;
  explorationCompassFace=null;
  explorationSketch=null;
  explorationDock=null;
  explorationHud=null;
  explorationMiniHud=null;explorationAxisMeter=null;explorationDetailPopup=null;explorationConfirmPopup=null;explorationAxisTip=null;
  explorationHudTitle=null;explorationHudStatus=null;explorationHudAxes=null;
  explorationMiniName=null;explorationMiniDistrict=null;explorationMiniBearing=null;explorationDetailBody=null;
  explorationConfirmTitle=null;explorationConfirmKind=null;explorationConfirmBody=null;explorationConfirmDelta=null;explorationAxisTipBody=null;
  explorationViewModeButton=null;
  explorationNeedleAlert=null;
  explorationNeedleCollapse=null;
  explorationNeedleCorruption=null;
  routeAxisInstrument?.Detach();routeAxisInstrument=null;
  routeDialogue?.Detach();routeDialogue=null;
  explorationHoverExitId=-1;
  explorationTitleLabel=null;
  explorationTypeLabel=null;
  explorationStatusLabel=null;
  explorationBodyLabel=null;
  explorationHintLabel=null;
  explorationAxisAlert=null;
  explorationAxisCollapse=null;
  explorationAxisCorruption=null;
  explorationMapNameLabel=null;
  explorationStatsLabel=null;
  explorationPointerId=-1;
  explorationPendingConfirmId=-1;
  explorationRouteStage?.SetSuspended(true);
  explorationStage?.SetSuspended(true);
 }

 void ReleaseExplorationStage(){
  SuspendExplorationStage();
  if(explorationRouteStage!=null){
   explorationRouteStage.Arrived-=OnExplorationArrived;
   Destroy(explorationRouteStage.gameObject);
   explorationRouteStage=null;
  }
  if(explorationStage!=null){
   explorationStage.Arrived-=OnExplorationArrived;
   Destroy(explorationStage.gameObject);
   explorationStage=null;
  }
 }

 void OnExplorationArrived(int nodeId,bool firstVisit){
  var run=game.UiExploration;
  if(run==null)return;
  var encounter=game.UiExplorationOnArrived(nodeId,firstVisit);
  if(encounter==ExplorationEncounter.EnterBuilding||encounter==ExplorationEncounter.ExitBuilding){
   BindActiveExplorationView(run,false);
   if(explorationMapNameLabel!=null)explorationMapNameLabel.text=ExplorationMapSystem.Breadcrumb(run);
   ShowToast(game.UiMessage);
  } else if(encounter==ExplorationEncounter.Event){
   ShowRouteEventDialogue();
   ShowToast(game.UiMessage);
  } else if(encounter==ExplorationEncounter.Battle){
   ShowToast("敵影と接触した");
   if(ExplorationRouteActive&&game.UiRouteBattleActive&&game.UiBattle!=null)
    BeginRouteBattle();
  } else if(encounter==ExplorationEncounter.Rest){
   ShowToast(game.UiMessage);
  } else if(!string.IsNullOrEmpty(game.UiMessage)){
   ShowToast(game.UiMessage);
  }
  RefreshExplorationHud();
  RefreshExplorationSketch();
 }

 void BindActiveExplorationView(ExplorationRunState run,bool preserve){
  bool rite=explorationUseRiteView||game.CurrentRoutePresentationMode==RoutePresentationMode.RiteDebug;
  if(rite){
   explorationRouteStage?.SetSuspended(true);
   explorationStage?.SetSuspended(false);
   explorationStage?.Bind(run,preserve);
   if(explorationView!=null)explorationView.image=explorationStage?.RenderTarget;
  } else {
   explorationStage?.SetSuspended(true);
   explorationRouteStage?.SetSuspended(false);
   explorationRouteStage?.Bind(run,preserve);
   if(explorationView!=null)explorationView.image=explorationRouteStage?.RenderTarget;
  }
 }

 void ApplyRouteModeVisibility(){
  if(explorationRoot==null||game==null)return;
  game.UiSyncRoutePresentationMode();
  var mode=game.CurrentRoutePresentationMode;
  bool route=game.UsesRoutePresentation;
  bool rite=mode==RoutePresentationMode.RiteDebug||explorationUseRiteView;
  bool combat=mode==RoutePresentationMode.RouteCombat;
  bool reward=mode==RoutePresentationMode.RouteReward;
  bool explore=mode==RoutePresentationMode.RouteExploration||mode==RoutePresentationMode.RouteTransition;

  if(explorationRoot!=null){
   if(route&&!rite)explorationRoot.AddToClassList("ps-xmap-route");
   else explorationRoot.RemoveFromClassList("ps-xmap-route");
  }
  // Route: full-bleed stage only. Dock / DEV chips / hint guide stay off normal explore.
  if(explorationDock!=null)
   explorationDock.style.display=rite?DisplayStyle.Flex:DisplayStyle.None;
  if(explorationSketch!=null)
   explorationSketch.style.display=DisplayStyle.None;
  if(explorationHud!=null)
   explorationHud.style.display=DisplayStyle.None;
  if(explorationHintGuide!=null)
   explorationHintGuide.style.display=DisplayStyle.None;
  if(explorationTopActions!=null)
   explorationTopActions.style.display=rite&&!combat&&!reward?DisplayStyle.Flex:DisplayStyle.None;
  if(explorationViewModeButton!=null)
   explorationViewModeButton.style.display=DisplayStyle.None;
  if(explorationFocusButton!=null)
   explorationFocusButton.style.display=rite&&explore?DisplayStyle.Flex:DisplayStyle.None;
  if(explorationFinishButton!=null)
   explorationFinishButton.style.display=explore&&!combat&&!reward?DisplayStyle.Flex:DisplayStyle.None;
  if(explorationMiniHud!=null)
   explorationMiniHud.style.display=route&&explore&&!rite?DisplayStyle.Flex:DisplayStyle.None;
  if(explorationAxisMeter!=null)
   explorationAxisMeter.style.display=DisplayStyle.None;
  if(routeAxisInstrument?.Root!=null){
   // Hide during combat/reward so the instrument never steals card clicks.
   bool showInstrument=route&&!rite&&mode is RoutePresentationMode.RouteExploration
    or RoutePresentationMode.RouteTransition
    or RoutePresentationMode.RouteEvent;
   routeAxisInstrument.Root.style.display=showInstrument?DisplayStyle.Flex:DisplayStyle.None;
   // Root stays Ignore; only the compact hit pad accepts hover (see RouteAxisInstrument).
   routeAxisInstrument.Root.pickingMode=PickingMode.Ignore;
  }
  if(explorationDetailPopup!=null&&!(route&&explore))
   explorationDetailPopup.style.display=DisplayStyle.None;
  if(explorationConfirmPopup!=null&&(combat||reward||rite||!explore))
   ClearExplorationConfirm();
  if(combat){
   if(game.UiBattle!=null&&(!routeBattleUiOpen||!RouteBattleUiAttached))BeginRouteBattle();
   else if(routeBattleRoot!=null&&RouteBattleUiAttached)
    routeBattleRoot.style.display=DisplayStyle.Flex;
  } else if(routeBattleRoot!=null&&RouteBattleUiAttached){
   routeBattleRoot.style.display=DisplayStyle.None;
  }
  if(reward){
   if(!routeRewardUiOpen||!RouteRewardUiAttached)EnterRouteReward();
   else if(routeRewardRoot!=null){
    routeRewardRoot.style.display=DisplayStyle.Flex;
    routeRewardRoot.BringToFront();
   }
  } else if(routeRewardRoot!=null&&RouteRewardUiAttached){
   routeRewardRoot.style.display=DisplayStyle.None;
  }
 }

 void ToggleExplorationViewMode(){
  var run=game.UiExploration;
  if(run==null||ExplorationAnyMoving||game.UiRouteBattleActive||game.UiRouteRewardPending)return;
  explorationUseRiteView=!explorationUseRiteView;
  game.SetRoutePresentationMode(explorationUseRiteView?RoutePresentationMode.RiteDebug:RoutePresentationMode.RouteExploration);
  BindActiveExplorationView(run,false);
  if(explorationViewModeButton!=null)
   explorationViewModeButton.text=explorationUseRiteView?"2.5Dへ":"DEV術式図";
  RefreshExplorationHud();
  RefreshExplorationSketch();
  ApplyRouteModeVisibility();
  ShowToast(explorationUseRiteView?"DEV: 術式図表示":"2.5Dルート表示");
 }

 void BuildExplorationMap(){
  var run=game.UiExploration;
  if(run==null){
   game.UiOpenExplorationMap();
   run=game.UiExploration;
  }
  // screenRoot.Clear() already dropped the previous tree; drop stale overlay refs too.
  InvalidateRouteOverlayUi();
  bool hadStage=explorationRouteStage!=null||explorationStage!=null;
  EnsureExplorationStage();
  game.UiSyncRoutePresentationMode();
  if(game.UiDevPreferRiteView||game.CurrentRoutePresentationMode==RoutePresentationMode.RiteDebug){
   explorationUseRiteView=true;game.UiDevPreferRiteView=false;
  } else if(game.UsesRoutePresentation)explorationUseRiteView=false;
  BindActiveExplorationView(run,hadStage);
  if(game.CurrentRoutePresentationMode==RoutePresentationMode.RouteExploration)
   explorationRouteStage?.ExitCombat();
  explorationMapBuilt=true;

  explorationRoot=Container("ps-xmap");
  screenRoot.Add(explorationRoot);

  explorationView=new Image{
   image=explorationRouteStage!=null?explorationRouteStage.RenderTarget:explorationStage?.RenderTarget,
   // RT aspect tracks this view; StretchToFill is safe when aspects match (no warp).
   scaleMode=ScaleMode.StretchToFill,
   pickingMode=PickingMode.Position,
  };
  explorationView.AddToClassList("ps-xmap-view");
  explorationView.focusable=true;
  explorationView.RegisterCallback<GeometryChangedEvent>(OnExplorationViewGeometryChanged);
  explorationView.RegisterCallback<PointerDownEvent>(OnExplorationPointerDown,TrickleDown.TrickleDown);
  explorationView.RegisterCallback<PointerMoveEvent>(OnExplorationPointerMove,TrickleDown.TrickleDown);
  explorationView.RegisterCallback<PointerUpEvent>(OnExplorationPointerUp,TrickleDown.TrickleDown);
  explorationView.RegisterCallback<PointerCaptureOutEvent>(_=>explorationDragging=false);
  explorationView.RegisterCallback<WheelEvent>(OnExplorationWheel,TrickleDown.TrickleDown);
  explorationRoot.Add(explorationView);
  SyncExplorationViewSize();

  explorationSketch=Container("ps-xmap-sketch");
  explorationSketch.pickingMode=PickingMode.Ignore;
  explorationRoot.Add(explorationSketch);

  var top=Container("ps-xmap-top");
  DressRiteFrame(top);
  var brand=Container("ps-rite-brand");
  var mark=Container("ps-rite-brand-mark");
  mark.pickingMode=PickingMode.Ignore;
  brand.Add(mark);
  var titleBlock=Container("ps-rite-top-title");
  var eye=new Label("遠征"){pickingMode=PickingMode.Ignore};
  eye.AddToClassList("ps-rite-top-eyebrow");
  titleBlock.Add(eye);
  explorationMapNameLabel=new Label(ExplorationMapSystem.Breadcrumb(run)){pickingMode=PickingMode.Ignore};
  explorationMapNameLabel.AddToClassList("ps-rite-top-name");
  titleBlock.Add(explorationMapNameLabel);
  var topSub=new Label("術式図（DEV）"){pickingMode=PickingMode.Ignore};
  topSub.AddToClassList("ps-xmap-top-sub");
  titleBlock.Add(topSub);
  brand.Add(titleBlock);
  top.Add(brand);
  explorationStatsLabel=new Label(""){pickingMode=PickingMode.Ignore};
  explorationStatsLabel.AddToClassList("ps-xmap-stats");
  top.Add(explorationStatsLabel);
  explorationTopActions=Container("ps-xmap-top-actions");
  var topActions=explorationTopActions;
  explorationViewModeButton=PackspireUiFactory.Button("2.5Dへ",ToggleExplorationViewMode);
  explorationViewModeButton.AddToClassList("ps-rite-chip");
  topActions.Add(explorationViewModeButton);
  explorationFocusButton=PackspireUiFactory.Button("現在地へ",()=>{
   if(game.UiExplorationEventActive||game.UiRouteBattleActive||game.UiRouteRewardPending)return;
   if(ExplorationRouteActive)explorationRouteStage.FocusPiece();
   else explorationStage?.FocusPiece();
  });
  explorationFocusButton.AddToClassList("ps-rite-chip");
  topActions.Add(explorationFocusButton);
  top.Add(topActions);
  explorationRoot.Add(top);

  var mapFrame=Container("ps-xmap-view-frame");
  mapFrame.pickingMode=PickingMode.Ignore;
  explorationRoot.Add(mapFrame);

  // Legacy large HUD kept for rite path null-safety; hidden on route.
  explorationHud=Container("ps-xmap-hud");
  explorationHud.style.display=DisplayStyle.None;
  explorationHudTitle=new Label(""){pickingMode=PickingMode.Ignore};
  explorationHud.Add(explorationHudTitle);
  explorationHudStatus=new Label(""){pickingMode=PickingMode.Ignore};
  explorationHud.Add(explorationHudStatus);
  explorationHudAxes=new Label(""){pickingMode=PickingMode.Ignore};
  explorationHud.Add(explorationHudAxes);
  explorationRoot.Add(explorationHud);

  explorationHintGuide=Container("ps-xmap-hint-guide");
  explorationHintGuide.style.display=DisplayStyle.None;
  explorationHintGuideLabel=new Label(""){pickingMode=PickingMode.Ignore};
  explorationHintGuide.Add(explorationHintGuideLabel);
  explorationRoot.Add(explorationHintGuide);

  BuildRouteMiniHud();
  BuildRouteAxisInstrument();
  BuildRouteDialogueLayer();
  BuildRouteDetailPopup();
  BuildRouteConfirmPopup();

  explorationFinishButton=PackspireUiFactory.Button("遠征終了",RequestExplorationFinish);
  explorationFinishButton.AddToClassList("ps-xmap-finish-chip");
  explorationRoot.Add(explorationFinishButton);

  // Dock / large compass only for F10 rite debug.
  explorationDock=Container("ps-xmap-dock");
  explorationDock.style.display=DisplayStyle.None;
  var dock=explorationDock;
  var panel=Container("ps-xmap-panel");
  DressRiteFrame(panel);
  explorationTypeLabel=new Label(""){pickingMode=PickingMode.Ignore};
  panel.Add(explorationTypeLabel);
  explorationTitleLabel=new Label(""){pickingMode=PickingMode.Ignore};
  panel.Add(explorationTitleLabel);
  explorationStatusLabel=new Label(""){pickingMode=PickingMode.Ignore};
  panel.Add(explorationStatusLabel);
  explorationBodyLabel=new Label(""){pickingMode=PickingMode.Ignore};
  panel.Add(explorationBodyLabel);
  explorationHintLabel=new Label(""){pickingMode=PickingMode.Ignore};
  panel.Add(explorationHintLabel);
  dock.Add(panel);
  var compass=Container("ps-xmap-compass");
  explorationCompassFace=Container("ps-xmap-gauge");
  // Needles live on the route mini meter; rite dock only shows axis chips.
  compass.Add(explorationCompassFace);
  var axisRow=Container("ps-xmap-axis-row");
  explorationAxisAlert=AddAxisChip(axisRow,"警戒","ps-xmap-axis-alert");
  explorationAxisCollapse=AddAxisChip(axisRow,"崩壊","ps-xmap-axis-collapse");
  explorationAxisCorruption=AddAxisChip(axisRow,"侵蝕","ps-xmap-axis-corruption");
  compass.Add(axisRow);
  dock.Add(compass);
  explorationRoot.Add(dock);

  var axes0=game.UiRun?.axes;
  explorationLastAlert=axes0?.alert??0;
  explorationLastCollapse=axes0?.collapse??0;
  explorationLastCorruption=axes0?.corruption??0;

  if(game.UiExplorationEventActive)ShowRouteEventDialogue();
  TickRouteBattleSync();
  RefreshExplorationHud();
  RefreshExplorationSketch();
  ApplyRouteModeVisibility();
  routeAxisInstrument?.SetCurrent(game.UiRun?.axes);
 }

 void BuildRouteMiniHud(){
  explorationMiniHud=Container("ps-xmap-mini");
  explorationMiniHud.pickingMode=PickingMode.Position;
  explorationMiniName=new Label(""){pickingMode=PickingMode.Ignore};
  explorationMiniName.AddToClassList("ps-xmap-mini-name");
  explorationMiniHud.Add(explorationMiniName);
  explorationMiniDistrict=new Label(""){pickingMode=PickingMode.Ignore};
  explorationMiniDistrict.AddToClassList("ps-xmap-mini-district");
  explorationMiniHud.Add(explorationMiniDistrict);
  var row=Container("ps-xmap-mini-row");
  explorationMiniBearing=new Label("N"){pickingMode=PickingMode.Ignore};
  explorationMiniBearing.AddToClassList("ps-xmap-mini-bearing");
  row.Add(explorationMiniBearing);
  explorationInfoButton=PackspireUiFactory.Button("ℹ",ToggleExplorationDetailPopup);
  explorationInfoButton.AddToClassList("ps-xmap-mini-info");
  row.Add(explorationInfoButton);
  explorationMiniHud.Add(row);
  explorationMiniHud.RegisterCallback<PointerEnterEvent>(_=>ShowExplorationDetailPopup(true));
  explorationMiniHud.RegisterCallback<PointerLeaveEvent>(_=>{
   if(explorationDetailPopup!=null&&explorationDetailPopup.ClassListContains("ps-xmap-detail-pinned"))return;
   ShowExplorationDetailPopup(false);
  });
  explorationRoot.Add(explorationMiniHud);
 }

 void BuildRouteAxisInstrument(){
  // Keep legacy mini-meter refs null-safe for rite path; route uses the large instrument.
  explorationAxisMeter=null;
  explorationNeedleAlert=null;explorationNeedleCollapse=null;explorationNeedleCorruption=null;
  explorationAxisTip=null;explorationAxisTipBody=null;
  routeAxisInstrument?.Detach();
  routeAxisInstrument=new RouteAxisInstrument();
  routeAxisInstrument.Build(explorationRoot);
  routeAxisInstrument.SetCurrent(game.UiRun?.axes);
 }

 void BuildRouteDialogueLayer(){
  routeDialogue?.Detach();
  routeDialogue=new RouteDialoguePresenter();
  routeDialogue.Build(explorationRoot);
  routeDialogue.SetCallbacks(
   ResolveRouteEventChoice,
   HoverRouteEventChoice,
   ()=>routeAxisInstrument?.ClearPreview());
 }

 void BuildRouteDetailPopup(){
  explorationDetailPopup=Container("ps-xmap-detail");
  explorationDetailPopup.style.display=DisplayStyle.None;
  explorationDetailPopup.pickingMode=PickingMode.Ignore;
  explorationDetailBody=new Label(""){pickingMode=PickingMode.Ignore};
  explorationDetailBody.AddToClassList("ps-xmap-detail-body");
  explorationDetailPopup.Add(explorationDetailBody);
  explorationRoot.Add(explorationDetailPopup);
 }

 void BuildRouteConfirmPopup(){
  explorationConfirmPopup=Container("ps-xmap-confirm");
  explorationConfirmPopup.pickingMode=PickingMode.Position;
  explorationConfirmPopup.style.display=DisplayStyle.None;
  explorationConfirmTitle=new Label(""){pickingMode=PickingMode.Ignore};
  explorationConfirmTitle.AddToClassList("ps-xmap-confirm-title");
  explorationConfirmPopup.Add(explorationConfirmTitle);
  explorationConfirmKind=new Label(""){pickingMode=PickingMode.Ignore};
  explorationConfirmKind.AddToClassList("ps-xmap-confirm-kind");
  explorationConfirmPopup.Add(explorationConfirmKind);
  explorationConfirmBody=new Label(""){pickingMode=PickingMode.Ignore};
  explorationConfirmBody.AddToClassList("ps-xmap-confirm-body");
  explorationConfirmPopup.Add(explorationConfirmBody);
  explorationConfirmDelta=new Label(""){pickingMode=PickingMode.Ignore};
  explorationConfirmDelta.AddToClassList("ps-xmap-confirm-delta");
  explorationConfirmPopup.Add(explorationConfirmDelta);
  var actions=Container("ps-xmap-confirm-actions");
  var go=PackspireUiFactory.Button("進む",ConfirmExplorationPending);
  go.AddToClassList("ps-xmap-confirm-go");
  actions.Add(go);
  var back=PackspireUiFactory.Button("戻る",ClearExplorationConfirm);
  back.AddToClassList("ps-xmap-confirm-back");
  actions.Add(back);
  explorationConfirmPopup.Add(actions);
  explorationRoot.Add(explorationConfirmPopup);
 }

 void ToggleExplorationDetailPopup(){
  if(explorationDetailPopup==null)return;
  bool pinned=explorationDetailPopup.ClassListContains("ps-xmap-detail-pinned");
  if(pinned){
   explorationDetailPopup.RemoveFromClassList("ps-xmap-detail-pinned");
   ShowExplorationDetailPopup(false);
  } else {
   explorationDetailPopup.AddToClassList("ps-xmap-detail-pinned");
   ShowExplorationDetailPopup(true);
  }
 }

 void ShowExplorationDetailPopup(bool show){
  if(explorationDetailPopup==null)return;
  if(!show&&explorationDetailPopup.ClassListContains("ps-xmap-detail-pinned"))return;
  explorationDetailPopup.style.display=show?DisplayStyle.Flex:DisplayStyle.None;
 }

 void ShowAxisTip(bool show){/* RouteAxisInstrument owns hover copy; no numeric tip. */}

 void ClearExplorationConfirm(){
  explorationPendingConfirmId=-1;
  if(explorationConfirmPopup!=null)explorationConfirmPopup.style.display=DisplayStyle.None;
  routeAxisInstrument?.ClearPreview();
 }

 void ShowExplorationConfirm(ExplorationRouteStage.RouteExitPick pick){
  explorationPendingConfirmId=pick.nodeId;
  if(explorationConfirmPopup==null)return;
  var run=game.UiExploration;
  var def=ExplorationMapSystem.Def(run);
  var target=ExplorationMapSystem.Node(def,pick.nodeId);
  explorationConfirmTitle.text=pick.investigateOnly?"調べる":$"{pick.destinationName}へ";
  explorationConfirmKind.text=pick.kindLabel;
  explorationConfirmBody.text=pick.investigateOnly
   ?"壁の違和感を確かめる"
   :pick.kind==ExplorationLinkKind.Breach&&!pick.opened?"瓦礫を壊して通路を開く"
   :target?.type=="building_door"?"建物の入口から中へ進む"
   :"この進路を進む";
  // No numeric axis deltas in normal play — instrument preview carries the forecast.
  if(explorationConfirmDelta!=null){
   explorationConfirmDelta.text="";
   explorationConfirmDelta.style.display=DisplayStyle.None;
  }
  PreviewAxesForExit(pick);
  var size=ExplorationPanelSize();
  // Keep confirm away from the bottom-right instrument (safe area).
  float left=Mathf.Clamp(pick.panelAnchor.x-90f,12f,Mathf.Max(12f,size.x-280f));
  float top=Mathf.Clamp(pick.panelAnchor.y-140f,70f,Mathf.Max(70f,size.y-260f));
  explorationConfirmPopup.style.left=left;
  explorationConfirmPopup.style.top=top;
  explorationConfirmPopup.style.display=DisplayStyle.Flex;
 }

 void PreviewAxesForExit(ExplorationRouteStage.RouteExitPick pick){
  var run=game.UiExploration;
  if(run==null||routeAxisInstrument==null)return;
  var def=ExplorationMapSystem.Def(run);
  var target=ExplorationMapSystem.Node(def,pick.nodeId);
  bool visited=ExplorationMapSystem.IsCleared(run,pick.nodeId);
  var forecast=RouteAxisForecast.ForExit(pick.kind,pick.opened,pick.investigateOnly,target,visited);
  routeAxisInstrument.ShowPreview(forecast);
 }

 void ConfirmExplorationPending(){
  int id=explorationPendingConfirmId;
  ClearExplorationConfirm();
  if(id<0)return;
  ExecuteExplorationPath(id,true);
 }

 void RefreshExplorationSketch(){
  if(explorationSketch==null)return;
  explorationSketch.Clear();
  // Route / combat presentation must not show the miniature graph overlay.
  explorationSketch.style.display=DisplayStyle.None;
 }

 void RequestExplorationFinish(){
  if(game.UiExplorationAtEntrance){game.UiExplorationFinish();return;}
  if(explorationRoot==null||explorationFinishDialog!=null)return;
  explorationFinishDialog=Container("ps-xmap-finish");
  var dialog=PackspireUiFactory.Dialog(
   "途中帰還の確認",
   "入口以外から遠征を終了します。戦利品と所持金は持ち帰れますが、未踏の地点は残ります。",
   "帰還する",
   ()=>{ClearExplorationFinishDialog();game.UiExplorationFinish();},
   "続ける",
   ClearExplorationFinishDialog);
  dialog.AddToClassList("ps-xmap-finish-dialog");
  explorationFinishDialog.Add(dialog);
  explorationRoot.Add(explorationFinishDialog);
 }

 void ClearExplorationFinishDialog(){
  if(explorationFinishDialog==null)return;
  explorationFinishDialog.RemoveFromHierarchy();
  explorationFinishDialog=null;
 }

 void ShowExplorationMist()=>ShowRouteEventDialogue();

 void ShowRouteEventDialogue(){
  if(explorationRoot==null)return;
  // Drop legacy central mist panel if it somehow remains.
  if(explorationMist!=null){
   explorationMist.RemoveFromHierarchy();
   explorationMist=null;
  }
  explorationAnomalyT=1.4f;
  if(routeDialogue==null)BuildRouteDialogueLayer();
  routeDialogue.ShowConversation(
   "記憶の揺らぎ",
   "周囲の道が黒い靄に沈み、まだ記されていない選択だけが浮かび上がる。道のうえで決断せよ。",
   new[]{
    "代償を支払う",
    "残響を修復する",
    "靄を振り払う",
   });
  routeAxisInstrument?.SetDimmed(true);
  routeDialogue.Root?.BringToFront();
  routeAxisInstrument?.Root?.BringToFront();
 }

 void HoverRouteEventChoice(int choice){
  routeAxisInstrument?.ShowPreview(RouteAxisForecast.ForEventChoice(choice));
 }

 void ResolveRouteEventChoice(int choice){
  ResolveExplorationMist(choice);
 }

 void ResolveExplorationMist(int choice){
  game.UiResolveExplorationEvent(choice);
  if(explorationMist!=null){
   explorationMist.RemoveFromHierarchy();
   explorationMist=null;
  }
  routeDialogue?.Hide();
  routeAxisInstrument?.ClearPreview();
  routeAxisInstrument?.SetDimmed(false);
  routeAxisInstrument?.CommitAnimatedValue(game.UiRun?.axes);
  if(!string.IsNullOrEmpty(game.UiMessage))ShowToast(game.UiMessage);
  RefreshExplorationHud();
  RefreshExplorationSketch();
 }

 void TickExplorationMap(){
  if(!explorationMapBuilt)return;
  game.UiSyncRoutePresentationMode();
  TickRouteBattleSync();
  ApplyRouteModeVisibility();
  TickExplorationCompass(Time.unscaledDeltaTime);
  routeDialogue?.Tick(Time.unscaledDeltaTime);
  float x=0f,y=0f;
  var mode=game.CurrentRoutePresentationMode;
  bool dialogueOpen=routeDialogue!=null&&routeDialogue.IsOpen&&!routeDialogue.IsBubbleOnly;
  bool blockInput=mode is RoutePresentationMode.RouteCombat or RoutePresentationMode.RouteReward or RoutePresentationMode.RouteEvent
   ||game.UiExplorationEventActive||dialogueOpen||explorationFinishDialog!=null||explorationPendingConfirmId>=0;
  if(!blockInput){
   if(Input.GetKey(KeyCode.A)||Input.GetKey(KeyCode.LeftArrow))x-=1f;
   if(Input.GetKey(KeyCode.D)||Input.GetKey(KeyCode.RightArrow))x+=1f;
   if(Input.GetKey(KeyCode.S)||Input.GetKey(KeyCode.DownArrow))y-=1f;
   if(Input.GetKey(KeyCode.W)||Input.GetKey(KeyCode.UpArrow))y+=1f;
  }
  if(ExplorationRouteActive){
   explorationRouteStage.Tick(x,y);
   if(explorationView!=null&&explorationRouteStage.RenderTarget!=null
    &&explorationView.image!=explorationRouteStage.RenderTarget)
    explorationView.image=explorationRouteStage.RenderTarget;
  } else explorationStage?.Tick(x,y);
 }

 Label AddAxisChip(VisualElement row,string name,string toneClass){
  var chip=Container("ps-xmap-axis-chip "+toneClass);
  var swatch=Container("ps-xmap-axis-swatch");
  swatch.pickingMode=PickingMode.Ignore;
  chip.Add(swatch);
  var nameLabel=new Label(name){pickingMode=PickingMode.Ignore};
  nameLabel.AddToClassList("ps-xmap-axis-name");
  chip.Add(nameLabel);
  var value=new Label("0"){pickingMode=PickingMode.Ignore};
  value.AddToClassList("ps-xmap-axis-value");
  chip.Add(value);
  row.Add(chip);
  return value;
 }

 void RefreshExplorationHud(){
  var run=game.UiExploration;
  var gameRun=game.UiRun;
  if(run==null)return;
  var def=ExplorationMapSystem.Def(run);
  var selected=ExplorationMapSystem.Node(def,run.selectedNodeId);
  var current=ExplorationMapSystem.Node(def,run.currentNodeId);
  if(explorationMapNameLabel!=null)explorationMapNameLabel.text=ExplorationMapSystem.Breadcrumb(run);
  var outdoor=ExplorationMapSystem.OutdoorDef(run);
  int playable=ExplorationMapSystem.PlayableCellCount(outdoor);
  int total=ExplorationMapSystem.TotalCellCount(outdoor);
  if(explorationStatsLabel!=null&&gameRun!=null){
   explorationStatsLabel.text=$"HP {gameRun.hp}/{gameRun.maxHp}　·　{gameRun.gold}G　·　戦勝 {gameRun.battlesWon}　·　区画 {playable}/{total}";
   explorationStatsLabel.style.display=explorationUseRiteView?DisplayStyle.Flex:DisplayStyle.None;
  }

  bool canMove=selected!=null&&game.UiExplorationCanMove(selected.id);
  bool cleared=selected!=null&&ExplorationMapSystem.IsCleared(run,selected.id);
  bool sealedBreach=selected!=null&&ExplorationMapSystem.Connected(def,run.currentNodeId,selected.id)
   &&ExplorationMapSystem.LinkKind(def,run.currentNodeId,selected.id)==ExplorationLinkKind.Breach
   &&!ExplorationMapSystem.IsEdgeOpened(run,run.currentNodeId,selected.id);
  bool hiddenLink=selected!=null&&ExplorationMapSystem.Connected(def,run.currentNodeId,selected.id)
   &&ExplorationMapSystem.LinkKind(def,run.currentNodeId,selected.id)==ExplorationLinkKind.Hidden;
  string typeLabel=selected==null?"—":selected.type switch{
   "entrance"=>"遠征入口",
   "exit"=>"建物出口",
   "building_door"=>"建物入口",
   "battle"=>"戦闘地点",
   "event"=>"出来事",
   "rest"=>"休憩地点",
   _=>"道",
  };
  if(explorationTitleLabel!=null)
   explorationTitleLabel.text=selected?.name??current?.name??"遠征中";
  if(explorationTypeLabel!=null)explorationTypeLabel.text=typeLabel+(cleared?" · 踏破済":"")+(sealedBreach?" · 封鎖":hiddenLink?" · 隠し道":"");
  string status=selected==null
   ?"出口を選んでください"
   :sealedBreach?"封鎖された出口 — タップで破壊"
   :canMove?"隣接しています — タップで進む"
   :selected.id==run.currentNodeId?"いま立っている地点です"
   :selected.locked?"この地区はまだ開いていません"
   :"ここへは直接進めません";
  if(explorationStatusLabel!=null)explorationStatusLabel.text=status;
  if(explorationBodyLabel!=null)
   explorationBodyLabel.text=$"現在地　{current?.name??"—"}\n地区　{ExplorationMapSystem.Breadcrumb(run)}\n\n{(selected==null?"前方の道・門・脇道を選ぶと、詳細がここに表示されます。":selected.landmark?"目印のある地点です。到着すると出来事が起きることがあります。":"道沿いの地点です。出口を選んで探索を広げましょう。")}";
  if(explorationHintLabel!=null)
   explorationHintLabel.text=game.UiExplorationEventActive?"靄のあいだで選択":"";

  var axes=gameRun?.axes;
  if(explorationAxisAlert!=null){
   explorationAxisAlert.text=FormatAxis(axes?.alert??0);
   explorationAxisCollapse.text=FormatAxis(axes?.collapse??0);
   explorationAxisCorruption.text=FormatAxis(axes?.corruption??0);
  }

  if(explorationMiniName!=null){
   explorationMiniName.text=current?.name??"遠征";
   explorationMiniDistrict.text=ExplorationMapSystem.Breadcrumb(run);
   explorationMiniBearing.text=BearingGlyph(run.currentNodeId);
  }
  if(explorationDetailBody!=null){
   explorationDetailBody.text=
    $"{current?.name??"—"}\n{typeLabel}\n\n{(current?.landmark==true?"目印のある地点。到着で出来事が起きることがあります。":"道沿いの地点。進路の光や入口を選んで進みます。")}\n\n{status}";
  }

  if(ExplorationRouteActive)explorationRouteStage.SetSelectedVisual(run.selectedNodeId);
  else explorationStage?.SetSelectedVisual(run.selectedNodeId);
 }

 static string BearingGlyph(int cellId)=>cellId switch{
  4=>"E",23=>"N",5=>"NE",11=>"SE",10=>"S",6=>"E",33=>"W",17=>"SW",_=>"·",
 };

 void TickExplorationCompass(float dt){
  var axes=game.UiRun?.axes;
  if(axes==null)return;
  if(axes.alert!=explorationLastAlert||axes.collapse!=explorationLastCollapse||axes.corruption!=explorationLastCorruption){
   if(IsAxisThresholdCross(explorationLastAlert,axes.alert)||IsAxisThresholdCross(explorationLastCollapse,axes.collapse)||IsAxisThresholdCross(explorationLastCorruption,axes.corruption))
    explorationAnomalyT=1.25f;
   else explorationAnomalyT=Mathf.Max(explorationAnomalyT,.45f);
   explorationLastAlert=axes.alert;explorationLastCollapse=axes.collapse;explorationLastCorruption=axes.corruption;
   // No numeric delta flash in normal play — animate the instrument only.
   routeAxisInstrument?.CommitAnimatedValue(axes);
  }
  if(explorationAnomalyT>0f)explorationAnomalyT=Mathf.Max(0f,explorationAnomalyT-dt);
  routeAxisInstrument?.Tick(dt);
  bool dim=routeDialogue!=null&&routeDialogue.IsOpen&&!routeDialogue.IsBubbleOnly;
  routeAxisInstrument?.SetDimmed(dim);

  // Rite dock chips still show exact values (DEV / rite path only).
  if(explorationNeedleAlert!=null){
   float t=Time.unscaledTime;
   float anomaly=explorationAnomalyT;
   SetNeedle(explorationNeedleAlert,axes.alert,t,1.15f,anomaly,1f);
   SetNeedle(explorationNeedleCollapse,axes.collapse,t*1.13f,.85f,anomaly,.78f);
   SetNeedle(explorationNeedleCorruption,axes.corruption,t*.91f,.55f,anomaly,.58f);
  }
 }

 static float GaugeAngle(float axis){
  float n=Mathf.Clamp(axis,-15f,15f)/15f;
  return n*60f;
 }

 static bool IsAxisThresholdCross(int from,int to){
  int[] marks={-15,-13,-9,-5,5,9,13,15};
  foreach(int m in marks)if((from<m&&to>=m)||(from>m&&to<=m)||Mathf.Abs(to)==15&&from!=to)return true;
  return false;
 }

 static void SetNeedle(VisualElement needle,int axis,float time,float jitterScale,float anomaly,float lengthScale){
  if(needle==null)return;
  float n=Mathf.Clamp(axis,-15,15)/15f;
  float swing=Mathf.Abs(n)*.22f+.05f;
  float jitter=Mathf.Sin(time*5.4f*(.7f+Mathf.Abs(n)))*swing*6f*jitterScale;
  float spike=anomaly>0f?Mathf.Sin(anomaly*26f)*10f*anomaly*jitterScale:0f;
  float angle=Mathf.Clamp(GaugeAngle(axis)+jitter+spike,-65f,65f);
  needle.style.rotate=new Rotate(Angle.Degrees(angle));
  needle.style.scale=new Scale(new Vector2(1f,lengthScale));
 }

 static string FormatAxis(int value)=>value>0?$"+{value}":value.ToString();

 void TryExplorationMoveTo(int nodeId)=>ExecuteExplorationPath(nodeId,false);

 void ExecuteExplorationPath(int nodeId,bool confirmed){
  var run=game.UiExploration;
  if(run==null||ExplorationAnyMoving||game.UiExplorationEventActive||game.UiRouteBattleActive||game.UiRouteRewardPending)return;
  var def=ExplorationMapSystem.Def(run);

  if(nodeId==run.currentNodeId){
   game.UiExplorationSelect(nodeId);
   var node=ExplorationMapSystem.Node(def,nodeId);
   if(node!=null&&(node.type=="exit"||(node.type=="building_door"&&!string.IsNullOrEmpty(node.interiorMapId)))){
    if(!confirmed){
     ShowExplorationConfirm(new ExplorationRouteStage.RouteExitPick{
      nodeId=nodeId,kind=ExplorationLinkKind.Normal,visualType=RouteExitVisualType.BuildingEntrance,
      opened=true,requireConfirm=true,destinationName=node.name??"建物",kindLabel="建物入口",
      panelAnchor=new Vector2(ExplorationPanelSize().x*.55f,ExplorationPanelSize().y*.45f),
     });
     return;
    }
    var encounter=game.UiExplorationOnArrived(nodeId,false);
    if(encounter==ExplorationEncounter.EnterBuilding||encounter==ExplorationEncounter.ExitBuilding){
     BindActiveExplorationView(run,false);
     ShowToast(game.UiMessage);
    } else if(node.type=="building_door")ShowToast(string.IsNullOrEmpty(game.UiMessage)?"鍵がかかっている":game.UiMessage);
   }
   RefreshExplorationHud();
   RefreshExplorationSketch();
   return;
  }

  var kind=ExplorationMapSystem.Connected(def,run.currentNodeId,nodeId)
   ?ExplorationMapSystem.LinkKind(def,run.currentNodeId,nodeId):ExplorationLinkKind.Normal;
  bool hiddenKnown=kind!=ExplorationLinkKind.Hidden||ExplorationMapSystem.IsHiddenKnown(run,run.currentNodeId,nodeId);
  bool investigate=kind==ExplorationLinkKind.Hidden&&!hiddenKnown;
  bool opened=kind!=ExplorationLinkKind.Breach||ExplorationMapSystem.IsEdgeOpened(run,run.currentNodeId,nodeId);
  bool needsConfirm=ExplorationRouteCatalog.NeedsConfirm(kind,null,investigate)||(!opened&&kind==ExplorationLinkKind.Breach);
  if(needsConfirm&&!confirmed&&ExplorationRouteActive){
   var target=ExplorationMapSystem.Node(def,nodeId);
   ShowExplorationConfirm(new ExplorationRouteStage.RouteExitPick{
    nodeId=nodeId,kind=kind,opened=opened,investigateOnly=investigate,requireConfirm=true,
    destinationName=target?.name??"道",
    kindLabel=ExplorationRouteCatalog.KindLabel(kind,opened,investigate),
    panelAnchor=new Vector2(ExplorationPanelSize().x*.55f,ExplorationPanelSize().y*.42f),
   });
   return;
  }

  if(investigate||(kind==ExplorationLinkKind.Breach&&!opened)){
   if(ExplorationMapSystem.TryUnlockEdge(run,run.currentNodeId,nodeId)){
    if(kind==ExplorationLinkKind.Breach&&game.UiRun?.axes!=null){
     game.UiRun.axes.Change(0,2,0);
     routeAxisInstrument?.CommitAnimatedValue(game.UiRun.axes);
    }
    ShowToast(kind==ExplorationLinkKind.Breach?"封鎖を破った":"隠し道を見つけた");
    if(ExplorationRouteActive)explorationRouteStage.Bind(run,true);
    // Hidden investigate stops here; breach confirm continues into travel.
    if(investigate||kind!=ExplorationLinkKind.Breach){
     RefreshExplorationHud();
     RefreshExplorationSketch();
     return;
    }
    opened=true;
   } else if(investigate||!opened){
    RefreshExplorationHud();
    return;
   }
  }

  string nk=ExplorationMapSystem.NodeKey(run.activeMapId,nodeId);
  if(!run.revealed.Contains(nk))run.revealed.Add(nk);
  game.UiExplorationSelect(nodeId);
  if(!game.UiExplorationMove(nodeId)){
   RefreshExplorationHud();
   ShowToast("つながる道がありません");
   return;
  }
  if(ExplorationRouteActive){
   game.UiSyncRoutePresentationMode();
   explorationRouteStage.BeginMoveTo(nodeId);
  } else explorationStage.BeginMoveTo(nodeId);
  RefreshExplorationHud();
 }

 Vector2 ExplorationPanelSize(){
  if(explorationView==null)return Vector2.zero;
  var rect=explorationView.contentRect.size;
  if(rect.x>=2f&&rect.y>=2f)return rect;
  float w=explorationView.resolvedStyle.width;
  float h=explorationView.resolvedStyle.height;
  if(w>=2f&&h>=2f)return new Vector2(w,h);
  return new Vector2(Screen.width,Screen.height);
 }

 void OnExplorationViewGeometryChanged(GeometryChangedEvent evt)=>SyncExplorationViewSize();

 void SyncExplorationViewSize(){
  if(explorationView==null||explorationRouteStage==null)return;
  var size=ExplorationPanelSize();
  if(size.x<2f||size.y<2f)return;
  float spp=explorationView.panel!=null?explorationView.panel.scaledPixelsPerPoint:1f;
  explorationRouteStage.SetViewPixelSize(new Vector2(size.x*spp,size.y*spp));
  if(explorationView.image!=explorationRouteStage.RenderTarget)
   explorationView.image=explorationRouteStage.RenderTarget;
 }

 void OnExplorationPointerDown(PointerDownEvent evt){
  if(explorationView==null||game.UiRouteBattleActive||game.UiRouteRewardPending||game.UiExplorationEventActive||explorationFinishDialog!=null)return;
  if(routeDialogue!=null&&routeDialogue.IsOpen&&!routeDialogue.IsBubbleOnly)return;
  if(evt.button!=0&&evt.button!=2)return;
  Vector2 local=(Vector2)evt.localPosition;
  explorationPointerDown=local;
  explorationDragging=false;
  explorationPointerId=evt.pointerId;
  if(ExplorationRouteActive)explorationRouteStage.BeginPan(local,ExplorationPanelSize());
  else explorationStage?.BeginPan(local,ExplorationPanelSize());
  explorationView.CapturePointer(evt.pointerId);
  explorationView.Focus();
  evt.StopPropagation();
 }

 void OnExplorationPointerMove(PointerMoveEvent evt){
  if(explorationView==null||game.UiExplorationEventActive||game.UiRouteBattleActive||game.UiRouteRewardPending)return;
  if(routeDialogue!=null&&routeDialogue.IsOpen&&!routeDialogue.IsBubbleOnly)return;
  if(explorationPointerId>=0&&evt.pointerId!=explorationPointerId)return;
  Vector2 local=(Vector2)evt.localPosition;
  if(ExplorationRouteActive){
   explorationRouteStage.SetHoverAt(local,ExplorationPanelSize());
   // Axis preview on exit hover (no numeric labels).
   if(explorationPendingConfirmId<0&&explorationRouteStage.TryPickExit(local,ExplorationPanelSize(),out var hoverPick)){
    if(hoverPick.nodeId!=explorationHoverExitId){
     explorationHoverExitId=hoverPick.nodeId;
     PreviewAxesForExit(hoverPick);
    }
   } else if(explorationHoverExitId>=0&&explorationPendingConfirmId<0){
    explorationHoverExitId=-1;
    routeAxisInstrument?.ClearPreview();
   }
  }
  if((local-explorationPointerDown).sqrMagnitude>12f){
   explorationDragging=true;
   if(ExplorationRouteActive)explorationRouteStage.UpdatePan(local,ExplorationPanelSize(),true);
   else explorationStage?.UpdatePan(local,ExplorationPanelSize(),true);
  }
  evt.StopPropagation();
 }

 void OnExplorationPointerUp(PointerUpEvent evt){
  if(explorationView==null)return;
  Vector2 local=(Vector2)evt.localPosition;
  if(explorationView.HasPointerCapture(evt.pointerId))
   explorationView.ReleasePointer(evt.pointerId);
  if(ExplorationRouteActive)explorationRouteStage.EndPan();
  else explorationStage?.EndPan();
  bool dialogueBlocks=routeDialogue!=null&&routeDialogue.IsOpen&&!routeDialogue.IsBubbleOnly;
  if(!game.UiRouteBattleActive&&!game.UiRouteRewardPending&&!game.UiExplorationEventActive&&!dialogueBlocks&&explorationFinishDialog==null&&!explorationDragging){
   if(ExplorationRouteActive&&explorationRouteStage.TryPickExit(local,ExplorationPanelSize(),out var pick)){
    if(pick.requireConfirm)ShowExplorationConfirm(pick);
    else{ClearExplorationConfirm();ExecuteExplorationPath(pick.nodeId,true);}
   } else if(!ExplorationRouteActive&&explorationStage!=null&&explorationStage.TryPickNode(local,ExplorationPanelSize(),out int nodeId)){
    TryExplorationMoveTo(nodeId);
   } else ClearExplorationConfirm();
  }
  explorationDragging=false;
  explorationPointerId=-1;
  evt.StopPropagation();
 }

 void OnExplorationWheel(WheelEvent evt){
  if(game.UiExplorationEventActive)return;
  if(ExplorationRouteActive)explorationRouteStage.ApplyWheelDelta(evt.delta.y);
  else explorationStage?.ApplyWheelDelta(evt.delta.y);
  evt.StopPropagation();
 }
}
}
