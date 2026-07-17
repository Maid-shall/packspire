using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire {
public sealed partial class PackspireUiFoundation {
 void BuildPresentationHome(){
  var meta=game.UiMeta;
  EnsurePresentationStage();
  presentationStage.RestoreFocus(savedHubFacility);
  var shell=Container("ps-v2-home");screenRoot.Add(shell);

  presentationStageView=new VisualElement{name="hub-stage-view"};
  presentationStageView.AddToClassList("ps-v2-stage-view");
  presentationStageView.pickingMode=PickingMode.Position;
  if(presentationStage.RenderTarget!=null){
   var stageImage=new Image{image=presentationStage.RenderTarget,scaleMode=ScaleMode.StretchToFill,pickingMode=PickingMode.Ignore};
   stageImage.AddToClassList("ps-v2-stage-image");
   presentationStageView.Add(stageImage);
  }
  var stageFade=new VisualElement();stageFade.AddToClassList("ps-v2-stage-fade");stageFade.pickingMode=PickingMode.Ignore;
  presentationStageView.Add(stageFade);
  RegisterPresentationInput(presentationStageView);
  shell.Add(presentationStageView);

  var profile=Container("ps-v2-home-profile");
  profile.pickingMode=PickingMode.Ignore;
  profile.Add(PackspireUiFactory.Title(GameCatalog.Roles[meta.currentRole].name));
  profile.Add(PackspireUiFactory.Body(FactionName(meta.currentFaction)+"所属"));
  profile.Add(PackspireUiFactory.Body($"{meta.baseGold}G　・　遠征 {meta.wins}/{meta.runs}"));
  shell.Add(profile);

  presentationFacilityLabel=new Label("拠点");
  presentationFacilityLabel.AddToClassList("ps-v2-facility-banner");
  presentationFacilityLabel.pickingMode=PickingMode.Ignore;
  shell.Add(presentationFacilityLabel);

  var dock=Container("ps-v2-home-dock");
  var prev=PackspireUiFactory.Button("◀",()=>{presentationStage.SnapPrevious();RefreshPresentationHud();});prev.AddToClassList("ps-v2-home-nav");
  var next=PackspireUiFactory.Button("▶",()=>{presentationStage.SnapNext();RefreshPresentationHud();});next.AddToClassList("ps-v2-home-nav");
  dock.Add(prev);
  var facilityRail=Container("ps-v2-facility-rail");
  for(int i=0;i<presentationStage.Facilities.Count;i++){
   int index=i;
   var facility=presentationStage.Facilities[index];
   var button=new Button(()=>{presentationStage.SnapToFacility(index);presentationStage.TriggerFacilityInteraction(index);savedHubFacility=index;RefreshPresentationHud();}){text=facility.label,tooltip=facility.label};
   button.AddToClassList("ps-v2-facility-button");
   button.name="facility-"+facility.id;
   facilityRail.Add(button);
  }
  dock.Add(facilityRail);
  dock.Add(next);
  shell.Add(dock);

  presentationEnterButton=null;

  presentationHintLabel=new Label("街を歩く");
  presentationHintLabel.AddToClassList("ps-v2-home-hint");
  presentationHintLabel.pickingMode=PickingMode.Ignore;
  shell.Add(presentationHintLabel);

  var tabs=Container("ps-v2-home-tabs");
  AddPresentationTab(tabs,"荷造り",ScreenId.Pack);
  AddPresentationTab(tabs,"図鑑",ScreenId.Compendium);
  AddPresentationTab(tabs,"勢力",ScreenId.Faction);
  shell.Add(tabs);
  presentationHubBuilt=true;
  RefreshPresentationHud();
 }

 void ReleasePresentationStage(){
  presentationHubBuilt=false;
  presentationStageView=null;
  presentationEnterButton=null;
  presentationHintLabel=null;
  presentationFacilityLabel=null;
  presentationEntering=false;
  if(presentationStage!=null)savedHubFacility=presentationStage.FocusedFacility;
  if(presentationStage==null)return;
  var host=presentationStage.gameObject;
  presentationStage=null;
  if(host!=null)Destroy(host);
 }

 void EnsurePresentationStage(){
  if(presentationStage!=null)return;
  var stale=game.transform.Find("HubPresentationStage");
  if(stale!=null)Destroy(stale.gameObject);
  var hostGo=new GameObject("HubPresentationStage");
  hostGo.transform.SetParent(game.transform,false);
  presentationStage=hostGo.AddComponent<PackspirePresentationStage>();
 }

 void RegisterPresentationInput(VisualElement stageView){
  stageView.RegisterCallback<PointerDownEvent>(evt=>{
   if(evt.button!=(int)MouseButton.LeftMouse)return;
   stageView.CapturePointer(evt.pointerId);
   presentationPointerDownPosition=evt.localPosition;
   presentationTapFacility=-1;
   presentationStage.BeginPointerDrag(evt.localPosition.x);
   if(presentationStage.TryPickFacility(evt.localPosition,stageView.layout.size,out int index)){
    presentationTapFacility=index;
    presentationStage.SnapToFacility(index);
    presentationStage.TriggerFacilityInteraction(index);
   }
   RefreshPresentationHud();
  });
  stageView.RegisterCallback<PointerMoveEvent>(evt=>{
   presentationStage.SetHoveredFacility(
    presentationStage.TryPickFacility(evt.localPosition,stageView.layout.size,out int hoverIndex)
     ?hoverIndex
     :-1);
   if(!stageView.HasPointerCapture(evt.pointerId))return;
   if(Vector2.Distance(evt.localPosition,presentationPointerDownPosition)>10f)presentationTapFacility=-1;
   presentationStage.UpdatePointerDrag(evt.localPosition.x,stageView.layout.width);
   presentationStage.SetFocusedFacilityHighlight(NearestPresentationFacility());
   RefreshPresentationHud();
  });
  stageView.RegisterCallback<PointerLeaveEvent>(_=>presentationStage.SetHoveredFacility(-1));
  stageView.RegisterCallback<PointerUpEvent>(evt=>{
   if(!stageView.HasPointerCapture(evt.pointerId))return;
   int tappedFacility=presentationTapFacility;
   presentationTapFacility=-1;
   stageView.ReleasePointer(evt.pointerId);
   presentationStage.EndPointerDrag();
   presentationStage.SetFocusedFacilityHighlight(NearestPresentationFacility());
   RefreshPresentationHud();
   if(tappedFacility>=0){
    presentationStage.SnapToFacility(tappedFacility);
    presentationStage.TriggerFacilityInteraction(tappedFacility);
    RefreshPresentationHud();
    EnterFocusedBuilding();
   }
  });
  stageView.RegisterCallback<WheelEvent>(evt=>{
   presentationStage.ApplyWheelDelta(evt.delta.y*.01f);
   presentationStage.SetFocusedFacilityHighlight(NearestPresentationFacility());
   RefreshPresentationHud();
   evt.StopPropagation();
  });
 }

 int NearestPresentationFacility(){
  if(presentationStage==null||presentationStage.Facilities.Count==0)return 0;
  float scroll=presentationStage.Scroll;
  int best=0;
  float bestDist=float.MaxValue;
  for(int i=0;i<presentationStage.Facilities.Count;i++){
   float snap=(presentationStage.Facilities[i].worldX-HubPresentationCatalog.EntranceScreenX)/HubPresentationCatalog.BuildingParallax;
   float dist=Mathf.Abs(scroll-snap);
   if(dist<bestDist){bestDist=dist;best=i;}
  }
  return best;
 }

 float ReadMoveInput(){
  float value=0f;
  if(Input.GetKey(KeyCode.RightArrow)||Input.GetKey(KeyCode.D))value+=1f;
  if(Input.GetKey(KeyCode.LeftArrow)||Input.GetKey(KeyCode.A))value-=1f;
  return value;
 }

 void RefreshPresentationHud(){
  if(presentationStage==null||!presentationHubBuilt||presentationStage.Facilities.Count==0)return;
  int focused=NearestPresentationFacility();
  presentationStage.SetFocusedFacilityHighlight(focused);
  bool ready=presentationStage.CanEnterAt(focused);
  if(presentationEnterButton!=null){
   presentationEnterButton.SetEnabled(ready);
   presentationEnterButton.EnableInClassList("ps-v2-enter-ready",ready);
  }
  if(presentationHintLabel!=null){
   var facility=presentationStage.Facilities[focused];
   presentationHintLabel.text=ready?$"{facility.label}をタップで入る":"← → で街を歩く";
  }
  if(presentationFacilityLabel!=null){
   var facility=presentationStage.Facilities[focused];
   presentationFacilityLabel.text=facility.label;
   presentationFacilityLabel.EnableInClassList("ps-v2-facility-banner-ready",ready);
  }
  var rail=screenRoot.Q<VisualElement>(className:"ps-v2-facility-rail");
  if(rail==null)return;
  int index=0;
  foreach(var child in rail.Children()){
   if(child is not Button button){index++;continue;}
   button.EnableInClassList("ps-v2-facility-hotspot-ready",ready&&index==focused);
   button.EnableInClassList("ps-selected",index==focused);
   index++;
  }
 }

 void EnterFocusedBuilding(){
  if(presentationStage==null||presentationEntering)return;
  int focused=NearestPresentationFacility();
  if(!presentationStage.CanEnterAt(focused))return;
  var target=presentationStage.Facilities[focused].screen;
  presentationEntering=true;
  presentationStage.TriggerFacilityInteraction(focused,true);
  screenRoot.schedule.Execute(()=>{
   presentationEntering=false;
   game.UiNavigate(target);
  }).StartingIn(220);
 }

 void AddPresentationTab(VisualElement tabs,string label,ScreenId screen){
  var button=PackspireUiFactory.Button(label,()=>game.UiNavigate(screen));
  button.AddToClassList("ps-v2-home-tab");
  tabs.Add(button);
 }
}
}
