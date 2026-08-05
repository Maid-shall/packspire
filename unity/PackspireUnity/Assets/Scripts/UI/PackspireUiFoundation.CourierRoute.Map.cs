using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire {
public sealed partial class PackspireUiFoundation {
 const float CourierRouteZoomMin=0.38f;
 const float CourierRouteZoomMax=1.45f;
 VisualElement courierRouteMapViewport,courierRouteMapContent;
 Button courierRouteFocusCurrent,courierRouteFitAll;
 Label courierRouteZoomLabel;
 Vector2 courierRouteMapPan,courierRouteMapPointerStart,courierRouteMapPanAtStart;
 float courierRouteMapZoom=0.75f;
 bool courierRouteMapPanning,courierRouteMapDidPan,courierRouteMapLayoutQueued,courierRouteMapNeedsInitialFit;
 string courierRouteMapFocusedNodeId="";

 void BuildCourierRouteMapInteraction(){
  courierRouteMapViewport=RequireViewElement<VisualElement>(courierRouteShell,"route-map-viewport");
  courierRouteMapContent=RequireViewElement<VisualElement>(courierRouteShell,"route-map-content");
  courierRouteFocusCurrent=RequireViewElement<Button>(courierRouteShell,"route-focus-current");
  courierRouteFitAll=RequireViewElement<Button>(courierRouteShell,"route-fit-all");
  courierRouteZoomLabel=RequireViewElement<Label>(courierRouteShell,"route-zoom");
  courierRouteMapViewport.RegisterCallback<PointerDownEvent>(OnCourierRoutePointerDown);
  courierRouteMapViewport.RegisterCallback<PointerMoveEvent>(OnCourierRoutePointerMove);
  courierRouteMapViewport.RegisterCallback<PointerUpEvent>(OnCourierRoutePointerUp);
  courierRouteMapViewport.RegisterCallback<PointerCaptureOutEvent>(OnCourierRoutePointerCaptureOut);
  courierRouteMapViewport.RegisterCallback<WheelEvent>(OnCourierRouteWheel);
  courierRouteMapViewport.RegisterCallback<GeometryChangedEvent>(OnCourierRouteMapGeometryChanged);
  courierRouteFocusCurrent.clicked+=FocusCourierRouteCurrent;
  courierRouteFitAll.clicked+=FitCourierRouteMap;
  courierRouteMapNeedsInitialFit=true;
  QueueCourierRouteMapLayout();
 }

 void OnCourierRoutePointerDown(PointerDownEvent evt){
  if(evt.button<0||evt.button>2||CourierRoutePointerHitsButton(evt.target as VisualElement))return;
  courierRouteMapPanning=true;
  courierRouteMapDidPan=false;
  courierRouteMapPointerStart=evt.position;
  courierRouteMapPanAtStart=courierRouteMapPan;
  courierRouteMapViewport.CapturePointer(evt.pointerId);
  evt.StopPropagation();
 }

 void OnCourierRoutePointerMove(PointerMoveEvent evt){
  if(!courierRouteMapPanning)return;
  Vector2 delta=(Vector2)evt.position-courierRouteMapPointerStart;
  if(!courierRouteMapDidPan&&delta.sqrMagnitude>36f)courierRouteMapDidPan=true;
  if(!courierRouteMapDidPan)return;
  courierRouteMapPan=courierRouteMapPanAtStart+delta;
  ApplyCourierRouteMapTransform();
  evt.StopPropagation();
 }

 void OnCourierRoutePointerUp(PointerUpEvent evt){
  if(!courierRouteMapPanning)return;
  bool wasClick=!courierRouteMapDidPan;
  courierRouteMapPanning=false;
  if(courierRouteMapViewport!=null&&courierRouteMapViewport.HasPointerCapture(evt.pointerId))
   courierRouteMapViewport.ReleasePointer(evt.pointerId);
  if(wasClick)ClearCourierRouteSelection();
  courierRouteMapViewport?.schedule.Execute(()=>courierRouteMapDidPan=false).ExecuteLater(1);
  evt.StopPropagation();
 }

 void ClearCourierRouteSelection(){
  courierRouteInspectedNodeId="";
  if(!CourierRouteSystem.ClearSelection(game.UiCourierRoute))return;
  PopulateCourierNodes();
  PopulateCourierDetail();
  ApplyCourierRouteMapTransform();
 }

 void OnCourierRoutePointerCaptureOut(PointerCaptureOutEvent evt){
  courierRouteMapPanning=false;
  courierRouteMapDidPan=false;
 }

 void OnCourierRouteWheel(WheelEvent evt){
  if(courierRouteMapViewport==null||!courierRouteMapViewport.worldBound.Contains(evt.mousePosition))return;
  float next=Mathf.Clamp(courierRouteMapZoom+(evt.delta.y>0f?-0.10f:0.10f),CourierRouteZoomMin,CourierRouteZoomMax);
  if(Mathf.Approximately(next,courierRouteMapZoom))return;
  Vector2 local=courierRouteMapViewport.WorldToLocal(evt.mousePosition);
  Vector2 mapPoint=(local-courierRouteMapPan)/courierRouteMapZoom;
  courierRouteMapZoom=next;
  courierRouteMapPan=local-mapPoint*courierRouteMapZoom;
  ApplyCourierRouteMapTransform();
  evt.StopPropagation();
 }

 static bool CourierRoutePointerHitsButton(VisualElement target){
  for(var element=target;element!=null;element=element.parent)
   if(element is Button button&&button.enabledInHierarchy)return true;
  return false;
 }

 void OnCourierRouteMapGeometryChanged(GeometryChangedEvent evt){
  if(Mathf.Approximately(evt.oldRect.width,evt.newRect.width)&&Mathf.Approximately(evt.oldRect.height,evt.newRect.height))return;
  QueueCourierRouteMapLayout();
 }

 void QueueCourierRouteMapLayout(){
  if(courierRouteMapLayoutQueued||courierRouteMapViewport==null)return;
  courierRouteMapLayoutQueued=true;
  courierRouteMapViewport.schedule.Execute(()=>{
   courierRouteMapLayoutQueued=false;
   string currentId=game.UiCourierRoute?.currentNodeId??"";
   if(courierRouteMapNeedsInitialFit){
    courierRouteMapNeedsInitialFit=false;
    courierRouteMapFocusedNodeId=currentId;
    FitCourierRouteMap();
   } else if(courierRouteMapFocusedNodeId!=currentId){
    courierRouteMapFocusedNodeId=currentId;
    FocusCourierRouteCurrent();
   } else ApplyCourierRouteMapTransform();
  }).ExecuteLater(0);
 }

 void FocusCourierRouteCurrent(){
  if(courierRouteMapViewport==null||courierRouteNodeHost==null)return;
  var current=courierRouteNodeHost.Q<Button>(className:"ps-current");
  if(current==null){FitCourierRouteMap();return;}
  var viewport=courierRouteMapViewport.contentRect;
  if(viewport.width<8f||viewport.height<8f){QueueCourierRouteMapLayout();return;}
  courierRouteMapZoom=0.82f;
  Vector2 center=current.layout.center;
  courierRouteMapPan=new Vector2(viewport.width*0.42f-center.x*courierRouteMapZoom,
   viewport.height*0.54f-center.y*courierRouteMapZoom);
  ApplyCourierRouteMapTransform();
 }

 void FitCourierRouteMap(){
  if(courierRouteMapViewport==null||courierRouteMapContent==null)return;
  var viewport=courierRouteMapViewport.contentRect;
  float contentWidth=courierRouteMapContent.resolvedStyle.width;
  float contentHeight=courierRouteMapContent.resolvedStyle.height;
  if(viewport.width<8f||viewport.height<8f||contentWidth<8f||contentHeight<8f){QueueCourierRouteMapLayout();return;}
  courierRouteMapZoom=Mathf.Clamp(Mathf.Min(viewport.width/contentWidth,viewport.height/contentHeight)*0.94f,
   CourierRouteZoomMin,CourierRouteZoomMax);
  courierRouteMapPan=new Vector2((viewport.width-contentWidth*courierRouteMapZoom)*0.5f,
   (viewport.height-contentHeight*courierRouteMapZoom)*0.5f);
  ApplyCourierRouteMapTransform();
 }

 void ApplyCourierRouteMapTransform(){
  if(courierRouteMapViewport==null||courierRouteMapContent==null)return;
  var viewport=courierRouteMapViewport.contentRect;
  float width=courierRouteMapContent.resolvedStyle.width*courierRouteMapZoom;
  float height=courierRouteMapContent.resolvedStyle.height*courierRouteMapZoom;
  if(viewport.width>0f&&viewport.height>0f){
   float minX=Mathf.Min(0f,viewport.width-width);
   float minY=Mathf.Min(0f,viewport.height-height);
   courierRouteMapPan.x=width<=viewport.width?(viewport.width-width)*0.5f:Mathf.Clamp(courierRouteMapPan.x,minX,0f);
   courierRouteMapPan.y=height<=viewport.height?(viewport.height-height)*0.5f:Mathf.Clamp(courierRouteMapPan.y,minY,0f);
  }
  courierRouteMapContent.style.scale=new Scale(new Vector2(courierRouteMapZoom,courierRouteMapZoom));
  courierRouteMapContent.style.translate=new Translate(Length.Pixels(courierRouteMapPan.x),Length.Pixels(courierRouteMapPan.y));
  if(courierRouteZoomLabel!=null)courierRouteZoomLabel.text=$"{Mathf.RoundToInt(courierRouteMapZoom*100f)}%";
 }

 void ClearCourierRouteMapInteraction(){
  courierRouteMapPanning=false;
  courierRouteMapDidPan=false;
  courierRouteMapLayoutQueued=false;
  courierRouteMapNeedsInitialFit=false;
  courierRouteMapViewport=courierRouteMapContent=null;
  courierRouteFocusCurrent=courierRouteFitAll=null;
  courierRouteZoomLabel=null;
 }
}
}
