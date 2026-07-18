using System.Collections.Generic;
using UnityEngine;

namespace Packspire {
/// <summary>2.5D route explorer: fixed character left, modular keyed layers, graph-driven exits.</summary>
public sealed class ExplorationRouteStage : MonoBehaviour {
 const int StageLayer=27;
 const float PixelsPerUnit=100f;

 sealed class LayerInst {
  public ExplorationRouteCatalog.RouteLayerDef def;
  public Transform transform;
  public SpriteRenderer renderer;
  public float baseX;
  public float authoredScaleY=1f;
  public Color baseColor;
 }

 sealed class ExitInst {
  public int nodeId;
  public Transform root;
  public TextMesh label;
  public SpriteRenderer body,cue,lantern,rim;
  public RouteExitPresentation slot;
  public ExplorationLinkKind kind;
  public RouteExitVisualType visualType;
  public bool opened,investigateOnly,requireConfirm;
  public Vector2 worldAnchor,clickSize;
  public Vector3 bodyBaseScale=Vector3.one;
  public float parallax=.7f;
  public Color bodyBaseColor=Color.white;
 }

 public struct RouteExitPick {
  public int nodeId;
  public ExplorationLinkKind kind;
  public RouteExitVisualType visualType;
  public bool opened,investigateOnly,requireConfirm;
  public string destinationName,kindLabel;
  public Vector2 panelAnchor;
 }

 Camera stageCamera;
 RenderTexture renderTarget;
 Transform worldRoot,exitRoot,axisFxRoot;
 RouteCombatActor heroActor,enemyActor;
 readonly List<LayerInst> layers=new();
 readonly List<ExitInst> exits=new();
 readonly List<Sprite> sprites=new();
 readonly List<Texture2D> runtimeTextures=new();

 ExplorationMapDef mapDef;
 ExplorationRunState runState;
 RunState gameRun;
 bool ready,moving,suspended,combatMode;
 float moveT,moveFromScroll,moveToScroll,scrollLive,combatBlend;
 int moveToId=-1;
 int selectedExitId=-1,hoveredExitId=-1;
 Font labelFont;
 Sprite softDiscCached,pathGlowCached,doorGlowCached;
 Vector2 viewPixelSize;

 public System.Action<int,bool> Arrived;
 public RenderTexture RenderTarget=>renderTarget;
 public bool Ready=>ready;
 public bool IsMoving=>moving;
 public bool InCombat=>combatMode;
 public ExplorationMapDef MapDef=>mapDef;

 public void SetSuspended(bool value){suspended=value;}

 public void Bind(ExplorationRunState run,bool preserveScene=false){
  runState=run;
  gameRun=PackspireGame.Instance!=null?PackspireGame.Instance.UiRun:null;
  mapDef=ExplorationMapSystem.Def(run);
  if(!ready)BuildStage();
  if(!preserveScene||!ready)RebuildScene();
  else {
   RefreshExits();
   ApplyScroll(scrollLive);
  }
 }

 public void Tick(float moveX=0f,float moveY=0f){
  if(suspended||!ready)return;
  float dt=Time.unscaledDeltaTime;
  float time=Time.unscaledTime;
  if(combatMode){
   combatBlend=Mathf.MoveTowards(combatBlend,1f,dt*2.2f);
   ApplyCombatFraming(combatBlend);
   heroActor?.Tick(time,dt,false);
   enemyActor?.Tick(time,dt,false);
   RenderStage();
   return;
  }
  if(moving){
   moveT+=dt*1.35f;
   float t=Mathf.SmoothStep(0f,1f,Mathf.Clamp01(moveT));
   scrollLive=Mathf.Lerp(moveFromScroll,moveToScroll,t);
   ApplyScroll(scrollLive);
   heroActor?.Tick(time,dt,true);
   if(t>=1f){
    moving=false;moveT=0f;scrollLive=0f;ApplyScroll(0f);
    int arrived=moveToId;moveToId=-1;
    if(runState!=null&&arrived>=0){
     ExplorationMapSystem.Move(runState,arrived,out bool firstVisit);
     RebuildScene();
     Arrived?.Invoke(arrived,firstVisit);
    }
   }
  } else {
   if(Mathf.Abs(moveX)>.05f)scrollLive=Mathf.Clamp(scrollLive+moveX*dt*1.2f,-0.6f,0.6f);
   else scrollLive=Mathf.MoveTowards(scrollLive,0f,dt*1.8f);
   ApplyScroll(scrollLive);
   heroActor?.Tick(time,dt,false);
  }
  RefreshExitHoverVisuals();
  RenderStage();
 }

 public void SetHoverAt(Vector2 panelPoint,Vector2 panelSize){
  if(combatMode||!TryPickExit(panelPoint,panelSize,out var pick))hoveredExitId=-1;
  else hoveredExitId=pick.nodeId;
 }

 public void EnterCombat(EnemyDef enemy){
  if(!ready)return;
  combatMode=true;combatBlend=0f;moving=false;
  if(exitRoot!=null)exitRoot.gameObject.SetActive(false);
  EnsureCombatActors(enemy);
  heroActor?.ResetCombatVisuals();
  enemyActor?.ResetCombatVisuals();
  heroActor?.SetPose(RouteActorPose.Idle);
  enemyActor?.SetPose(RouteActorPose.Idle);
  if(stageCamera!=null)stageCamera.orthographicSize=ExplorationRouteCatalog.CombatOrtho;
  UpdateCoverLayers();
 }

 public void ExitCombat(){
  combatMode=false;combatBlend=0f;
  if(exitRoot!=null)exitRoot.gameObject.SetActive(true);
  if(enemyActor?.Root!=null)enemyActor.Root.gameObject.SetActive(false);
  if(heroActor?.Root!=null){
   heroActor.Root.localPosition=new Vector3(
    ExplorationRouteCatalog.CharacterAnchorX,
    ExplorationRouteCatalog.CharacterGroundY,0f);
   heroActor.SetPose(RouteActorPose.Idle);
  }
  if(stageCamera!=null){
   stageCamera.orthographicSize=ExplorationRouteCatalog.OrthographicSize;
   stageCamera.transform.position=new Vector3(0f,ExplorationRouteCatalog.CameraCenterY,-10f);
   stageCamera.backgroundColor=new Color(.16f,.18f,.22f);
  }
  foreach(var layer in layers){
   if(layer.renderer!=null)layer.renderer.color=layer.baseColor;
  }
  RefreshExits();
  UpdateCoverLayers();
 }

 public void NotifyHeroAttack()=>heroActor?.SetPose(RouteActorPose.Attack);
 public void NotifyHeroGuard()=>heroActor?.SetPose(RouteActorPose.Guard);
 public void NotifyHeroHit()=>heroActor?.SetPose(RouteActorPose.Hit);
 public void NotifyEnemyAttack()=>enemyActor?.SetPose(RouteActorPose.Attack);
 public void NotifyEnemyHit()=>enemyActor?.SetPose(RouteActorPose.Hit);
 public void NotifyEnemyDown()=>enemyActor?.SetPose(RouteActorPose.Down);

 public void BeginMoveTo(int nodeId){
  if(combatMode||!ready||runState==null||moving||mapDef==null)return;
  if(nodeId==runState.currentNodeId)return;
  if(!ExplorationMapSystem.CanMove(runState,nodeId)){
   if(ExplorationMapSystem.Connected(mapDef,runState.currentNodeId,nodeId)
    &&ExplorationMapSystem.TryUnlockEdge(runState,runState.currentNodeId,nodeId))
    RefreshExits();
   if(!ExplorationMapSystem.CanMove(runState,nodeId))return;
  }
  float dir=ExitWorldX(nodeId)>=0f?1f:-1f;
  moveFromScroll=scrollLive;
  moveToScroll=dir*ExplorationRouteCatalog.MoveScrollDistance;
  moveT=0f;moveToId=nodeId;moving=true;
 }

 public bool TryPickNode(Vector2 panelPoint,Vector2 panelSize,out int nodeId){
  nodeId=-1;
  if(!TryPickExit(panelPoint,panelSize,out var pick))return false;
  nodeId=pick.nodeId;return true;
 }

 public bool TryPickExit(Vector2 panelPoint,Vector2 panelSize,out RouteExitPick pick){
  pick=default;
  if(!ready||stageCamera==null||panelSize.x<2f||combatMode)return false;
  PanelToWorld(panelPoint,panelSize,out float worldX,out float worldY);
  float best=float.MaxValue;ExitInst bestExit=null;
  foreach(var exit in exits){
   float ex=exit.worldAnchor.x-scrollLive*exit.parallax;
   float ey=exit.worldAnchor.y;
   float hx=exit.clickSize.x*.5f,hy=exit.clickSize.y*.5f;
   if(worldX<ex-hx||worldX>ex+hx||worldY<ey-hy*.15f||worldY>ey+hy)continue;
   float d=Vector2.Distance(new Vector2(worldX,worldY),new Vector2(ex,ey+hy*.35f));
   if(d<best){best=d;bestExit=exit;}
  }
  if(bestExit==null)return false;
  var target=ExplorationMapSystem.Node(mapDef,bestExit.nodeId);
  pick=new RouteExitPick{
   nodeId=bestExit.nodeId,kind=bestExit.kind,visualType=bestExit.visualType,
   opened=bestExit.opened,investigateOnly=bestExit.investigateOnly,requireConfirm=bestExit.requireConfirm,
   destinationName=target?.name??"道",
   kindLabel=ExplorationRouteCatalog.KindLabel(bestExit.kind,bestExit.opened,bestExit.investigateOnly),
   panelAnchor=WorldToPanel(
    bestExit.worldAnchor.x-scrollLive*bestExit.parallax,
    bestExit.worldAnchor.y+(bestExit.slot!=null?bestExit.slot.popupOffset.y:1f),
    panelSize),
  };
  return true;
 }

 void PanelToWorld(Vector2 panelPoint,Vector2 panelSize,out float worldX,out float worldY){
  // Same aspect as the camera / RT (viewPixelSize), not an independent Screen ratio.
  float aspect=ViewAspect;
  float ortho=stageCamera!=null?stageCamera.orthographicSize:ExplorationRouteCatalog.OrthographicSize;
  float viewW=ortho*2f*aspect,viewH=ortho*2f;
  float camY=stageCamera!=null?stageCamera.transform.localPosition.y:ExplorationRouteCatalog.CameraCenterY;
  worldX=(panelPoint.x/Mathf.Max(1f,panelSize.x)-.5f)*viewW;
  worldY=(.5f-panelPoint.y/Mathf.Max(1f,panelSize.y))*viewH+camY;
 }

 Vector2 WorldToPanel(float worldX,float worldY,Vector2 panelSize){
  float aspect=ViewAspect;
  float ortho=stageCamera!=null?stageCamera.orthographicSize:ExplorationRouteCatalog.OrthographicSize;
  float viewW=ortho*2f*aspect,viewH=ortho*2f;
  float camY=stageCamera!=null?stageCamera.transform.localPosition.y:ExplorationRouteCatalog.CameraCenterY;
  float nx=worldX/viewW+.5f;
  float ny=.5f-(worldY-camY)/viewH;
  return new Vector2(nx*panelSize.x,ny*panelSize.y);
 }

 public void FocusPiece(){scrollLive=0f;ApplyScroll(0f);}
 public void SetSelectedVisual(int selectedId){selectedExitId=selectedId;RefreshExitHoverVisuals();}

 void RefreshExitHoverVisuals(){
  float t=Time.unscaledTime;
  foreach(var exit in exits){
   bool hover=exit.nodeId==hoveredExitId||exit.nodeId==selectedExitId;
   if(exit.body!=null){
    var c=exit.bodyBaseColor;
    if(hover)c=Color.Lerp(c,new Color(1f,.95f,.75f,c.a),.4f);
    exit.body.color=c;
    exit.body.transform.localScale=exit.bodyBaseScale*(hover?1.06f:1f);
   }
   if(exit.cue!=null){
    float pulse=.55f+.2f*Mathf.Sin(t*2.4f+(exit.nodeId%5));
    exit.cue.color=hover?new Color(1f,.92f,.55f,.75f):new Color(1f,.88f,.55f,pulse*.45f);
    exit.cue.transform.localScale=Vector3.one*(hover?1.25f:1f+Mathf.Sin(t*3f)*.06f);
   }
   if(exit.lantern!=null){
    float flicker=.7f+.3f*Mathf.Sin(t*7f+exit.nodeId);
    exit.lantern.color=hover?new Color(1f,.9f,.55f,1f):new Color(1f,.85f,.5f,flicker*.85f);
   }
   if(exit.rim!=null){
    exit.rim.enabled=hover;
    if(hover){
     // Silhouette glow: brighter outline of the same shape as the exit body.
     exit.rim.color=new Color(1f,.93f,.55f,.55f);
     if(exit.body!=null)
      exit.rim.transform.localScale=exit.bodyBaseScale*(hover?1.1f:1.08f);
    } else exit.rim.color=new Color(1f,.92f,.55f,0f);
   }
   if(exit.label!=null)
    exit.label.color=hover?new Color(.99f,.95f,.82f,.98f):new Color(.9f,.84f,.7f,0f);
  }
 }

 public void BeginPan(Vector2 panelPoint,Vector2 panelSize){}
 public void UpdatePan(Vector2 panelPoint,Vector2 panelSize,bool detach=true){
  if(combatMode||moving||panelSize.x<2f)return;
  float nx=panelPoint.x/panelSize.x-.5f;
  scrollLive=Mathf.Clamp(nx*1.1f,-0.7f,0.7f);
  ApplyScroll(scrollLive);
 }
 public void EndPan(){}
 public void ApplyWheelDelta(float delta){
  if(combatMode||stageCamera==null)return;
  stageCamera.orthographicSize=Mathf.Clamp(stageCamera.orthographicSize-delta*.15f,2.6f,4.6f);
  UpdateCoverLayers();
  RenderStage();
 }

 /// <summary>Match RT / camera frustum / pick math to the live exploration Image pixel size.</summary>
 public void SetViewPixelSize(Vector2 pixels){
  if(pixels.x<64f||pixels.y<64f)return;
  if(Mathf.Abs(viewPixelSize.x-pixels.x)<2f&&Mathf.Abs(viewPixelSize.y-pixels.y)<2f)return;
  viewPixelSize=pixels;
  if(!ready)return;
  EnsureRenderTargetSize();
  UpdateCoverLayers();
  RenderStage();
 }

 public float ViewAspect{
  get{
   if(viewPixelSize.x>1f&&viewPixelSize.y>1f)return viewPixelSize.x/viewPixelSize.y;
   if(renderTarget!=null&&renderTarget.height>0)return (float)renderTarget.width/renderTarget.height;
   return 16f/9f;
  }
 }

 void OnDestroy(){Teardown();}

 void Teardown(){
  ready=false;combatMode=false;
  layers.Clear();exits.Clear();
  heroActor=null;enemyActor=null;
  worldRoot=null;exitRoot=null;axisFxRoot=null;stageCamera=null;
  if(renderTarget!=null){
   if(RenderTexture.active==renderTarget)RenderTexture.active=null;
   renderTarget.Release();Destroy(renderTarget);renderTarget=null;
  }
  for(int i=transform.childCount-1;i>=0;i--){var c=transform.GetChild(i);if(c!=null)Destroy(c.gameObject);}
  foreach(var s in sprites)if(s!=null)Destroy(s);sprites.Clear();
  foreach(var t in runtimeTextures)if(t!=null)Destroy(t);runtimeTextures.Clear();
 }

 void BuildStage(){
  Teardown();
  labelFont=Resources.Load<Font>("Fonts/KleeOne-Regular")??Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
  if(viewPixelSize.x<64f||viewPixelSize.y<64f)
   viewPixelSize=new Vector2(Mathf.Max(1280,Screen.width),Mathf.Max(720,Screen.height));
  int rtW=Mathf.Max(64,Mathf.RoundToInt(viewPixelSize.x));
  int rtH=Mathf.Max(64,Mathf.RoundToInt(viewPixelSize.y));
  renderTarget=new RenderTexture(rtW,rtH,16,RenderTextureFormat.ARGB32){name="PackspireExplorationRoute",filterMode=FilterMode.Bilinear};
  renderTarget.Create();

  var camGo=new GameObject("RouteCamera");
  camGo.transform.SetParent(transform,false);
  SetLayer(camGo);
  stageCamera=camGo.AddComponent<Camera>();
  stageCamera.orthographic=true;
  stageCamera.orthographicSize=ExplorationRouteCatalog.OrthographicSize;
  stageCamera.transform.position=new Vector3(0f,ExplorationRouteCatalog.CameraCenterY,-10f);
  stageCamera.clearFlags=CameraClearFlags.SolidColor;
  stageCamera.backgroundColor=new Color(.16f,.18f,.22f);
  stageCamera.cullingMask=1<<StageLayer;
  stageCamera.targetTexture=renderTarget;
  stageCamera.enabled=false;
  stageCamera.nearClipPlane=.05f;
  stageCamera.farClipPlane=40f;

  worldRoot=new GameObject("World").transform;
  worldRoot.SetParent(transform,false);
  SetLayer(worldRoot.gameObject);
  exitRoot=new GameObject("Exits").transform;
  exitRoot.SetParent(transform,false);
  SetLayer(exitRoot.gameObject);
  axisFxRoot=new GameObject("AxisFx").transform;
  axisFxRoot.SetParent(transform,false);
  SetLayer(axisFxRoot.gameObject);

  heroActor=RouteCombatActor.CreateHero(transform,ExplorationRouteCatalog.CharacterDisplayHeight,StageLayer,sprites,runtimeTextures);
  if(heroActor.Root!=null){
   heroActor.Root.localPosition=new Vector3(
    ExplorationRouteCatalog.CharacterAnchorX,
    ExplorationRouteCatalog.CharacterGroundY,0f);
  }
  ready=true;
 }

 void RebuildScene(){
  if(!ready||mapDef==null||runState==null)return;
  ClearLayers();
  ClearExits();
  ClearAxisFx();
  scrollLive=0f;
  var cell=ExplorationMapSystem.Node(mapDef,runState.currentNodeId);
  var pres=ExplorationRouteCatalog.PresentationFor(runState.currentNodeId,mapDef.IsInterior);
  BuildLayers(cell,pres);
  BuildLandmark(cell,pres);
  ApplyAxisTint(pres);
  RefreshExits();
  ApplyScroll(0f);
 }

 void ClearLayers(){
  layers.Clear();
  if(worldRoot==null)return;
  for(int i=worldRoot.childCount-1;i>=0;i--)Destroy(worldRoot.GetChild(i).gameObject);
 }

 void ClearExits(){
  exits.Clear();hoveredExitId=-1;
  if(exitRoot==null)return;
  for(int i=exitRoot.childCount-1;i>=0;i--)Destroy(exitRoot.GetChild(i).gameObject);
 }

 void ClearAxisFx(){
  if(axisFxRoot==null)return;
  for(int i=axisFxRoot.childCount-1;i>=0;i--)Destroy(axisFxRoot.GetChild(i).gameObject);
 }

 void BuildLayers(ExplorationCellDef cell,RouteCellPresentation pres){
  bool interior=mapDef!=null&&mapDef.IsInterior;
  foreach(var def in ExplorationRouteCatalog.BaseLayers){
   if(interior&&def.id is "far")continue;
   string res=def.resource;
   bool keyGreen=false,keyBlack=false;
   if(def.id=="sky"&&!string.IsNullOrEmpty(pres.skyResource))res=pres.skyResource;
   if(def.id=="far"){
    if(string.IsNullOrEmpty(pres.farResource))continue;
    res=pres.farResource;keyGreen=pres.keyFarGreen;keyBlack=pres.keyFarBlack;
   }
   if(def.id=="mid"){
    if(string.IsNullOrEmpty(pres.midResource))continue;
    res=pres.midResource;keyGreen=pres.keyMidGreen;
   }
   if(def.id=="ground"){
    if(string.IsNullOrEmpty(pres.groundResource))continue;
    res=pres.groundResource;keyGreen=pres.keyGroundGreen;
   }
   if(def.id=="foreground"){
    if(string.IsNullOrEmpty(pres.foregroundResource))continue;
    res=pres.foregroundResource;keyBlack=pres.keyForegroundBlack;
   }
   var tex=LoadKeyed(res,keyGreen,keyBlack)??LoadKeyed(def.fallback,false,true);
   if(tex==null){
    Debug.LogWarning($"[ExplorationRouteStage] Missing/failed layer '{def.id}' ({res}) — skipped.");
    continue;
   }
   SpawnLayer(def,tex,pres.grade,0f);
  }
 }

 void SpawnLayer(ExplorationRouteCatalog.RouteLayerDef def,Texture2D tex,Color grade,float baseX){
  var sp=Sprite.Create(tex,new Rect(0,0,tex.width,tex.height),new Vector2(.5f,.5f),PixelsPerUnit);
  sprites.Add(sp);
  var go=new GameObject(def.id);
  go.transform.SetParent(worldRoot,false);
  SetLayer(go);
  var sr=go.AddComponent<SpriteRenderer>();
  sr.sprite=sp;sr.sortingOrder=def.sort;
  var col=Color.Lerp(def.tint,grade,.28f);
  sr.color=col;
  float worldH=def.height;
  float spriteH=sp.bounds.size.y;
  float spriteW=sp.bounds.size.x;
  float scale=worldH/Mathf.Max(.01f,spriteH);
  if(def.cover&&stageCamera!=null){
   float aspect=CoverAspect();
   float viewW=stageCamera.orthographicSize*2f*aspect;
   float viewH=stageCamera.orthographicSize*2f;
   float cover=Mathf.Max(viewW/Mathf.Max(.01f,spriteW),viewH/Mathf.Max(.01f,spriteH));
   scale=cover*1.05f;
  } else if(def.id=="ground"){
   scale=worldH/Mathf.Max(.01f,spriteH);
   go.transform.localScale=new Vector3(scale*2.8f,scale,1f);
   go.transform.localPosition=new Vector3(baseX,def.y,0f);
   layers.Add(new LayerInst{def=def,transform=go.transform,renderer=sr,baseX=baseX,authoredScaleY=scale,baseColor=col});
   return;
  } else if(def.id=="mid"){
   // Wide strip — not a centered plate on top of sky.
   go.transform.localScale=new Vector3(scale*2.4f,scale,1f);
   go.transform.localPosition=new Vector3(baseX,def.y,0f);
   layers.Add(new LayerInst{def=def,transform=go.transform,renderer=sr,baseX=baseX,authoredScaleY=scale,baseColor=col});
   return;
  }
  go.transform.localScale=new Vector3(scale,scale,1f);
  go.transform.localPosition=new Vector3(baseX,def.y,0f);
  layers.Add(new LayerInst{def=def,transform=go.transform,renderer=sr,baseX=baseX,authoredScaleY=scale,baseColor=col});
 }

 void BuildLandmark(ExplorationCellDef cell,RouteCellPresentation pres){
  string path=!string.IsNullOrEmpty(pres.landmarkResource)
   ?pres.landmarkResource
   :ExplorationRouteCatalog.LandmarkResource(cell);
  if(string.IsNullOrEmpty(path))return;
  var tex=LoadKeyed(path,pres.keyLandmarkGreen,false);
  if(tex==null){
   Debug.LogWarning($"[ExplorationRouteStage] Landmark '{path}' failed chroma — hidden.");
   return;
  }
  var sp=Sprite.Create(tex,new Rect(0,0,tex.width,tex.height),new Vector2(.5f,0f),PixelsPerUnit);
  sprites.Add(sp);
  var go=new GameObject("Landmark");
  go.transform.SetParent(worldRoot,false);
  SetLayer(go);
  var sr=go.AddComponent<SpriteRenderer>();
  var col=Color.Lerp(Color.white,pres.grade,.15f);
  sr.sprite=sp;sr.sortingOrder=-18;sr.color=col;
  float targetH=pres.landmarkHeight>0f?pres.landmarkHeight:3.6f;
  float scale=targetH/Mathf.Max(.01f,sp.bounds.size.y);
  float x=pres.landmarkX!=0f?pres.landmarkX:1.55f;
  go.transform.localScale=new Vector3(scale*(pres.flipLandmark?-1f:1f),scale,1f);
  go.transform.localPosition=new Vector3(x,ExplorationRouteCatalog.GroundY,0f);
  layers.Add(new LayerInst{
   def=new ExplorationRouteCatalog.RouteLayerDef("landmark",path,null,.62f,ExplorationRouteCatalog.GroundY,targetH,-18,Color.white),
   transform=go.transform,renderer=sr,baseX=x,baseColor=col,
  });
 }

 void ApplyAxisTint(RouteCellPresentation pres){
  if(gameRun?.axes==null)return;
  int alert=gameRun.axes.alert,collapse=gameRun.axes.collapse,erosion=gameRun.axes.corruption;
  foreach(var layer in layers){
   if(layer.renderer==null)continue;
   var c=layer.baseColor;
   if(alert>=3)c=Color.Lerp(c,new Color(1f,.55f,.5f),Mathf.Clamp01(alert/12f)*.35f);
   if(collapse>=3&&layer.def.id is "ground" or "mid")
    c=Color.Lerp(c,new Color(.55f,.48f,.4f),Mathf.Clamp01(collapse/12f)*.3f);
   if(erosion>=3&&layer.def.id is "far" or "sky")
    c=Color.Lerp(c,new Color(.65f,.85f,.55f),Mathf.Clamp01(erosion/12f)*.28f);
   layer.renderer.color=c;
   layer.baseColor=c;
  }
  if(ExplorationRouteCatalog.ShowRouteDebugGizmos){
   // Debug-only markers — off by default.
   if(alert>=2)SpawnDebugDot(new Color(.9f,.3f,.25f,.5f),2.1f,2f);
   if(collapse>=2)SpawnDebugDot(new Color(.55f,.45f,.35f,.5f),1.2f,.4f);
   if(erosion>=2)SpawnDebugDot(new Color(.6f,.85f,.5f,.45f),-1.5f,1.1f);
  }
 }

 void SpawnDebugDot(Color c,float x,float y){
  var tex=new Texture2D(4,4,TextureFormat.ARGB32,false);
  var cols=new Color[16];for(int i=0;i<16;i++)cols[i]=Color.white;
  tex.SetPixels(cols);tex.Apply();runtimeTextures.Add(tex);
  var sp=Sprite.Create(tex,new Rect(0,0,4,4),new Vector2(.5f,.5f),16f);sprites.Add(sp);
  var go=new GameObject("dbg");
  go.transform.SetParent(axisFxRoot,false);
  SetLayer(go);
  var sr=go.AddComponent<SpriteRenderer>();
  sr.sprite=sp;sr.sortingOrder=55;sr.color=c;
  go.transform.localScale=new Vector3(.25f,.25f,1f);
  go.transform.localPosition=new Vector3(x,y,0f);
 }

 void EnsureCombatActors(EnemyDef enemy){
  if(heroActor==null||!heroActor.IsValid)
   heroActor=RouteCombatActor.CreateHero(transform,ExplorationRouteCatalog.CharacterDisplayHeight,StageLayer,sprites,runtimeTextures);
  if(heroActor.Root!=null)heroActor.Root.gameObject.SetActive(heroActor.IsValid);
  if(enemyActor==null)
   enemyActor=RouteCombatActor.CreateEnemy(transform,ExplorationRouteCatalog.EnemyDisplayHeight,StageLayer,sprites,runtimeTextures);
  if(enemyActor.Root!=null){
   enemyActor.Root.gameObject.SetActive(enemyActor.IsValid);
   enemyActor.Root.localPosition=new Vector3(
    ExplorationRouteCatalog.EnemyCombatX,
    ExplorationRouteCatalog.CharacterGroundY,0f);
  }
 }

 void ApplyCombatFraming(float blend){
  if(stageCamera==null)return;
  float camY=Mathf.Lerp(ExplorationRouteCatalog.CameraCenterY,ExplorationRouteCatalog.CombatCameraY,blend);
  stageCamera.transform.position=new Vector3(0f,camY,-10f);
  stageCamera.orthographicSize=Mathf.Lerp(ExplorationRouteCatalog.OrthographicSize,ExplorationRouteCatalog.CombatOrtho,blend);
  stageCamera.backgroundColor=Color.Lerp(new Color(.16f,.18f,.22f),new Color(.08f,.07f,.1f),blend*.65f);
  if(heroActor?.Root!=null){
   var from=new Vector3(ExplorationRouteCatalog.CharacterAnchorX,ExplorationRouteCatalog.CharacterGroundY,0f);
   var to=new Vector3(ExplorationRouteCatalog.HeroCombatX,ExplorationRouteCatalog.CharacterGroundY,0f);
   heroActor.Root.localPosition=Vector3.Lerp(from,to,blend);
  }
  if(enemyActor?.Root!=null){
   enemyActor.Root.localPosition=new Vector3(
    ExplorationRouteCatalog.EnemyCombatX,
    ExplorationRouteCatalog.CharacterGroundY,0f);
  }
  foreach(var layer in layers){
   if(layer.renderer==null)continue;
   float mul=Mathf.Lerp(1f,.62f,blend);
   var c=layer.baseColor;
   layer.renderer.color=new Color(c.r*mul,c.g*mul,c.b*mul,c.a);
  }
  UpdateCoverLayers();
 }

 void RefreshExits(){
  ClearExits();
  if(runState==null||mapDef==null||combatMode)return;
  var neighbors=new List<int>();
  if(mapDef.IsInterior){
   foreach(int n in ExplorationMapSystem.Neighbors(mapDef,runState.currentNodeId))neighbors.Add(n);
  } else {
   foreach(int n in ExplorationRouteCatalog.PrototypeNeighbors(mapDef,runState.currentNodeId))neighbors.Add(n);
  }
  neighbors.Sort();
  var pres=ExplorationRouteCatalog.PresentationFor(runState.currentNodeId,mapDef.IsInterior);
  var bound=ExplorationRouteCatalog.BindExitsToNeighbors(pres,neighbors,runState.currentNodeId);
  foreach(var (to,slot) in bound){
   ExplorationLinkKind kind;
   bool investigateOnly=false,opened=true;
   if(to==runState.currentNodeId){
    kind=ExplorationLinkKind.Normal;
    opened=true;
   } else {
    kind=ExplorationMapSystem.LinkKind(mapDef,runState.currentNodeId,to);
    bool hiddenKnown=kind!=ExplorationLinkKind.Hidden||ExplorationMapSystem.IsHiddenKnown(runState,runState.currentNodeId,to);
    investigateOnly=kind==ExplorationLinkKind.Hidden&&!hiddenKnown;
    opened=kind!=ExplorationLinkKind.Breach||ExplorationMapSystem.IsEdgeOpened(runState,runState.currentNodeId,to);
   }
   SpawnExit(to,kind,opened,investigateOnly,slot);
  }
  SetSelectedVisual(runState.selectedNodeId);
 }

 void SpawnExit(int nodeId,ExplorationLinkKind kind,bool opened,bool investigateOnly,RouteExitPresentation slot){
  slot??=ExplorationRouteCatalog.FallbackSlot(exits.Count);
  var visual=slot.visualType;
  if(investigateOnly)visual=RouteExitVisualType.HiddenCrack;
  else if(kind==ExplorationLinkKind.Breach&&!opened)visual=RouteExitVisualType.BreachRubble;
  else if(kind==ExplorationLinkKind.Hidden)visual=RouteExitVisualType.HiddenCrack;

  var target=ExplorationMapSystem.Node(mapDef,nodeId);
  var root=new GameObject($"exit-{nodeId}-{visual}").transform;
  root.SetParent(exitRoot,false);
  SetLayer(root.gameObject);
  var anchor=slot.worldAnchor;
  root.localPosition=new Vector3(anchor.x,anchor.y,0f);

  var bodyTex=LoadKeyed(ExplorationRouteCatalog.VisualResource(visual),false,false);
  SpriteRenderer body=null,cue=null,lantern=null,rim=null;
  Color bodyColor=new Color(.92f,.9f,.86f,.95f);
  Vector3 bodyScale=Vector3.one;
  if(bodyTex!=null){
   body=SpawnExitSprite(root,"body",bodyTex,40,slot.clickSize.y*0.95f);
   if(investigateOnly)bodyColor=new Color(.85f,.88f,.95f,.55f);
   else if(kind==ExplorationLinkKind.Breach&&!opened)bodyColor=new Color(.82f,.72f,.62f,.95f);
   if(body!=null){body.color=bodyColor;bodyScale=body.transform.localScale;}
  }
  var cueTex=LoadKeyed(string.IsNullOrEmpty(slot.cueResource)?"Art/RouteKeyed/placeholders/exit-glow":slot.cueResource,false,false)
   ??LoadKeyed("Art/RouteKeyed/placeholders/exit-glow",false,false);
  if(cueTex!=null){
   float cueH=visual is RouteExitVisualType.Road or RouteExitVisualType.Stairs?0.7f:(investigateOnly?0.9f:1.15f);
   cue=SpawnExitSprite(root,"cue",cueTex,42,cueH);
   if(cue!=null){
    float cueY=visual is RouteExitVisualType.Road or RouteExitVisualType.Stairs
     ?slot.clickSize.y*.12f:slot.clickSize.y*.35f;
    cue.transform.localPosition=new Vector3(0f,cueY,0f);
   }
  }
  if(visual is RouteExitVisualType.Gate or RouteExitVisualType.Door or RouteExitVisualType.BuildingEntrance){
   var lanternTex=LoadKeyed("Art/RouteKeyed/placeholders/exit-lantern",false,false);
   if(lanternTex!=null){
    lantern=SpawnExitSprite(root,"lantern",lanternTex,43,.85f);
    if(lantern!=null)lantern.transform.localPosition=new Vector3(
     visual==RouteExitVisualType.BuildingEntrance?0.35f:0.2f,slot.clickSize.y*.55f,0f);
   }
  }
  // Hover rim follows the exit silhouette (body), not a SoftDisc oval.
  {
   var rimGo=new GameObject("rim");
   rimGo.transform.SetParent(root,false);
   SetLayer(rimGo);
   rim=rimGo.AddComponent<SpriteRenderer>();
   if(body!=null&&body.sprite!=null){
    rim.sprite=body.sprite;
    rimGo.transform.localPosition=body.transform.localPosition;
    rimGo.transform.localScale=body.transform.localScale*1.08f;
   } else {
    rim.sprite=visual is RouteExitVisualType.Road or RouteExitVisualType.Stairs?PathGlowSprite():DoorGlowSprite();
    rimGo.transform.localScale=new Vector3(slot.clickSize.x*.55f,slot.clickSize.y*.2f,1f);
    rimGo.transform.localPosition=new Vector3(0f,slot.clickSize.y*.08f,0f);
   }
   rim.sortingOrder=39;
   rim.enabled=false;
   rim.color=new Color(1f,.92f,.55f,0f);
  }

  var labelGo=new GameObject("label");
  labelGo.transform.SetParent(root,false);
  SetLayer(labelGo);
  labelGo.transform.localPosition=new Vector3(slot.labelOffset.x,slot.labelOffset.y,0f);
  var tm=labelGo.AddComponent<TextMesh>();
  tm.text=ExitLabel(target,kind,opened,investigateOnly);
  tm.characterSize=.038f;tm.fontSize=32;
  tm.anchor=TextAnchor.UpperCenter;tm.alignment=TextAlignment.Center;
  tm.color=new Color(.92f,.86f,.72f,0f);
  if(labelFont!=null){tm.font=labelFont;var mr=labelGo.GetComponent<MeshRenderer>();if(mr!=null&&labelFont.material!=null)mr.sharedMaterial=labelFont.material;}

  bool confirm=ExplorationRouteCatalog.NeedsConfirm(kind,slot,investigateOnly)
   ||visual==RouteExitVisualType.BuildingEntrance;
  // Clickable bounds slightly larger than the drawn silhouette.
  var pickSize=new Vector2(slot.clickSize.x*1.15f,slot.clickSize.y*1.1f);

  exits.Add(new ExitInst{
   nodeId=nodeId,root=root,label=tm,body=body,cue=cue,lantern=lantern,rim=rim,
   slot=slot,kind=kind,visualType=visual,opened=opened,investigateOnly=investigateOnly,
   requireConfirm=confirm,worldAnchor=anchor,clickSize=pickSize,parallax=.7f,
   bodyBaseScale=bodyScale,bodyBaseColor=bodyColor,
  });
 }

 SpriteRenderer SpawnExitSprite(Transform parent,string name,Texture2D tex,int sort,float worldH){
  if(tex==null)return null;
  var sp=Sprite.Create(tex,new Rect(0,0,tex.width,tex.height),new Vector2(.5f,0f),PixelsPerUnit);
  sprites.Add(sp);
  var go=new GameObject(name);
  go.transform.SetParent(parent,false);
  SetLayer(go);
  var sr=go.AddComponent<SpriteRenderer>();
  sr.sprite=sp;sr.sortingOrder=sort;
  float scale=worldH/Mathf.Max(.01f,sp.bounds.size.y);
  go.transform.localScale=new Vector3(scale,scale,1f);
  go.transform.localPosition=Vector3.zero;
  return sr;
 }

 float ExitWorldX(int nodeId){
  foreach(var e in exits)if(e.nodeId==nodeId)return e.worldAnchor.x;
  return 1.5f;
 }

 void ApplyScroll(float scroll){
  foreach(var layer in layers){
   if(layer.transform==null)continue;
   layer.transform.localPosition=new Vector3(layer.baseX-scroll*layer.def.parallax,layer.def.y,0f);
  }
  if(exitRoot!=null){
   foreach(var exit in exits){
    if(exit.root==null)continue;
    exit.root.localPosition=new Vector3(
     exit.worldAnchor.x-scroll*exit.parallax,
     exit.worldAnchor.y,0f);
   }
  }
  UpdateCoverLayers();
 }

 float CoverAspect()=>Mathf.Max(1.2f,ViewAspect);

 /// <summary>
 /// Re-fit cover (and wide strip) layers to the live camera frustum after zoom,
 /// combat framing, parallax scroll, or RT aspect changes. Overscan keeps the
 /// camera clear colour from leaking as blue bands at the edges.
 /// </summary>
 void UpdateCoverLayers(){
  if(stageCamera==null)return;
  EnsureRenderTargetSize();
  float aspect=CoverAspect();
  float viewW=stageCamera.orthographicSize*2f*aspect;
  float viewH=stageCamera.orthographicSize*2f;
  Vector3 cam=stageCamera.transform.localPosition;
  const float overscan=1.05f;
  const float maxPan=0.75f;
  foreach(var layer in layers){
   if(layer.transform==null||layer.renderer==null||layer.renderer.sprite==null)continue;
   var size=layer.renderer.sprite.bounds.size;
   float panX=maxPan*Mathf.Max(layer.def.parallax,.04f);
   float layerX=layer.baseX-scrollLive*layer.def.parallax;
   float dx=Mathf.Abs(layerX-cam.x)+panX;
   float dy=Mathf.Abs(layer.def.y-cam.y)+.35f;
   if(layer.def.cover){
    float requiredW=(viewW+dx*2f)*overscan;
    float requiredH=(viewH+dy*2f)*overscan;
    float scale=Mathf.Max(
     requiredW/Mathf.Max(.01f,size.x),
     requiredH/Mathf.Max(.01f,size.y));
    layer.transform.localScale=new Vector3(scale,scale,1f);
    continue;
   }
   // Mid/ground strips: keep vertical authoring scale, but always span the frustum width.
   if(layer.def.id is "mid" or "ground"){
    float baseScale=layer.authoredScaleY>0.01f?layer.authoredScaleY:layer.def.height/Mathf.Max(.01f,size.y);
    float needW=(viewW+dx*2f)*overscan;
    float scaleX=Mathf.Max(baseScale*(layer.def.id=="ground"?2.8f:2.4f),needW/Mathf.Max(.01f,size.x));
    layer.transform.localScale=new Vector3(scaleX,baseScale,1f);
   }
  }
 }

 void EnsureRenderTargetSize(){
  if(renderTarget==null||stageCamera==null)return;
  int w=viewPixelSize.x>=64f?Mathf.RoundToInt(viewPixelSize.x):Mathf.Max(1280,Screen.width);
  int h=viewPixelSize.y>=64f?Mathf.RoundToInt(viewPixelSize.y):Mathf.Max(720,Screen.height);
  w=Mathf.Clamp(w,64,3840);h=Mathf.Clamp(h,64,2160);
  if(renderTarget.width==w&&renderTarget.height==h)return;
  if(RenderTexture.active==renderTarget)RenderTexture.active=null;
  renderTarget.Release();
  Destroy(renderTarget);
  renderTarget=new RenderTexture(w,h,16,RenderTextureFormat.ARGB32){
   name="PackspireExplorationRoute",filterMode=FilterMode.Bilinear,
  };
  renderTarget.Create();
  stageCamera.targetTexture=renderTarget;
 }

 void RenderStage(){
  if(stageCamera==null||renderTarget==null)return;
  UpdateCoverLayers();
  var prev=RenderTexture.active;
  RenderTexture.active=renderTarget;
  GL.Clear(true,true,stageCamera.backgroundColor);
  RenderTexture.active=prev;
  stageCamera.Render();
 }

 static string ExitLabel(ExplorationCellDef target,ExplorationLinkKind kind,bool opened,bool investigate){
  if(investigate)return "調べる";
  if(kind==ExplorationLinkKind.Breach&&!opened)return "瓦礫・破壊";
  if(kind==ExplorationLinkKind.Hidden)return "隠し道";
  if(target==null)return "道";
  string name=string.IsNullOrEmpty(target.name)?"道":(target.name.Length>8?target.name.Substring(0,8):target.name);
  return name;
 }

 Sprite SoftDiscSprite(){
  if(softDiscCached!=null)return softDiscCached;
  softDiscCached=MakeRadialSprite(32,.5f);return softDiscCached;
 }

 Sprite PathGlowSprite(){
  if(pathGlowCached!=null)return pathGlowCached;
  // Wide low ellipse — path foot glow, not a giant button disc.
  pathGlowCached=MakeEllipseSprite(64,24);return pathGlowCached;
 }

 Sprite DoorGlowSprite(){
  if(doorGlowCached!=null)return doorGlowCached;
  doorGlowCached=MakeEllipseSprite(28,48);return doorGlowCached;
 }

 Sprite MakeRadialSprite(int s,float alpha){
  var tex=new Texture2D(s,s,TextureFormat.ARGB32,false);var cols=new Color[s*s];float c=(s-1)*.5f;
  for(int y=0;y<s;y++)for(int x=0;x<s;x++){
   float d=Vector2.Distance(new Vector2(x,y),new Vector2(c,c))/c;
   cols[y*s+x]=d>1f?Color.clear:new Color(1,1,1,Mathf.Clamp01(1f-d)*alpha);
  }
  tex.SetPixels(cols);tex.Apply();runtimeTextures.Add(tex);
  var sp=Sprite.Create(tex,new Rect(0,0,s,s),new Vector2(.5f,.5f),48f);sprites.Add(sp);return sp;
 }

 Sprite MakeEllipseSprite(int w,int h){
  var tex=new Texture2D(w,h,TextureFormat.ARGB32,false);var cols=new Color[w*h];
  float cx=(w-1)*.5f,cy=(h-1)*.5f;
  for(int y=0;y<h;y++)for(int x=0;x<w;x++){
   float nx=(x-cx)/Mathf.Max(.01f,cx),ny=(y-cy)/Mathf.Max(.01f,cy);
   float d=nx*nx+ny*ny;
   cols[y*w+x]=d>1f?Color.clear:new Color(1,1,1,Mathf.Clamp01(1f-d)*.55f);
  }
  tex.SetPixels(cols);tex.Apply();runtimeTextures.Add(tex);
  var sp=Sprite.Create(tex,new Rect(0,0,w,h),new Vector2(.5f,.5f),48f);sprites.Add(sp);return sp;
 }

 Texture2D LoadKeyed(string path,bool keyGreen,bool keyBlack){
  if(string.IsNullOrEmpty(path))return null;
  var src=Resources.Load<Texture2D>(path);
  if(src==null)return null;
  if(!keyGreen&&!keyBlack)return PackspireChromaKey.Readable(src,runtimeTextures);
  return PackspireChromaKey.Key(src,keyGreen,keyBlack,runtimeTextures);
 }

 void SetLayer(GameObject go){go.layer=StageLayer;foreach(Transform child in go.transform)SetLayer(child.gameObject);}
}
}
