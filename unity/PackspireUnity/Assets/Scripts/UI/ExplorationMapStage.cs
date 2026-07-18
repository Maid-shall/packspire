using System.Collections.Generic;
using UnityEngine;

namespace Packspire {
/// <summary>Obsidian–gold expedition rite diagram: conduits + seals (no fort plate).</summary>
public sealed class ExplorationMapStage : MonoBehaviour {
 const int StageLayer=28;
 const float DefaultOrtho=4.8f;
 const float MinOrtho=2.8f;
 const float MaxOrtho=8.5f;
 const float CamDistance=12f;
 const float PanLerp=18f;
 const float FollowLerp=10f;

 Camera stageCamera;
 RenderTexture renderTarget;
 Transform mapRoot,overlayRoot,pieceTransform;
 SpriteRenderer mapRenderer,pieceRenderer,pieceShadow;
 readonly List<Transform> nodeMarkers=new();
 readonly List<LineRenderer> edgeLines=new();
 readonly List<Sprite> sprites=new();
 readonly List<Material> materials=new();
 readonly Dictionary<int,SpriteRenderer> nodeRenderers=new();
 readonly Dictionary<int,SpriteRenderer> nodeRings=new();
 readonly Dictionary<int,LineRenderer> edgeByKey=new();
 readonly Dictionary<int,ExplorationLinkKind> edgeKindByKey=new();
 readonly Dictionary<int,SpriteRenderer> lockVeils=new();

 ExplorationMapDef mapDef;
 ExplorationRunState runState;
 string boundMapId;
 Vector3 cameraTarget,cameraLive;
 bool pointerDragging;
 Vector2 dragStartPanel;
 Vector3 dragStartTarget;
 bool moving,followPiece=true;
 Vector3 moveFrom,moveTo;
 float moveT;
 int moveToId=-1;
 float orthoSize=DefaultOrtho;
 bool ready;
 Font labelFont;
 public System.Action<int,bool> Arrived;
 public bool Suspended{get;private set;}

 public RenderTexture RenderTarget=>renderTarget;
 public bool Ready=>ready;
 public bool IsMoving=>moving;
 public ExplorationMapDef MapDef=>mapDef;

 public void Bind(ExplorationRunState run,bool preserveCamera=false){
  runState=run;
  mapDef=ExplorationMapSystem.Def(run);
  string nextId=mapDef?.id??"";
  bool mapChanged=boundMapId!=nextId;
  if(!ready){BuildStage();boundMapId=nextId;preserveCamera=false;}
  else if(mapChanged){RebuildGraph();boundMapId=nextId;preserveCamera=false;}
  else RefreshMarkers();
  followPiece=true;
  if(!preserveCamera||mapChanged)SnapCameraToCurrent(true);
  else{
   var node=ExplorationMapSystem.Node(mapDef,runState.currentNodeId);
   if(node!=null&&pieceTransform!=null){
    Vector3 pos=ExplorationMapSystem.WorldPos(mapDef,node);pos.z=-0.08f;
    pieceTransform.position=pos;
   }
   ApplyCamera();
  }
  SetSuspended(false);
  RenderStage();
 }

 public void SetSuspended(bool value){
  Suspended=value;
  if(gameObject!=null)gameObject.SetActive(!value);
 }

 public void Tick(float moveX=0f,float moveY=0f){
  if(Suspended||!ready||mapDef==null)return;
  float dt=Time.unscaledDeltaTime;
  if(moving){
   moveT+=dt*2.1f;
   float t=Mathf.SmoothStep(0f,1f,Mathf.Clamp01(moveT));
   if(pieceTransform!=null){
    Vector3 p=Vector3.Lerp(moveFrom,moveTo,t);p.z=-0.08f;
    pieceTransform.position=p;
    if(pieceShadow!=null){var sp=p;sp.z=-0.01f;sp.y-=0.06f;pieceShadow.transform.position=sp;}
   }
   if(followPiece&&pieceTransform!=null)cameraTarget=pieceTransform.position;
   if(t>=1f){
    moving=false;moveT=0f;
    int arrivedId=moveToId;moveToId=-1;
    if(runState!=null&&arrivedId>=0){
     ExplorationMapSystem.Move(runState,arrivedId,out bool firstVisit);
     RefreshMarkers();
     Arrived?.Invoke(arrivedId,firstVisit);
    }
   }
  } else if(Mathf.Abs(moveX)>.01f||Mathf.Abs(moveY)>.01f){
   followPiece=false;
   float speed=orthoSize*2.6f*dt;
   cameraTarget+=new Vector3(moveX*speed,moveY*speed,0f);
  } else if(followPiece&&pieceTransform!=null){
   cameraTarget=Vector3.Lerp(cameraTarget,pieceTransform.position,1f-Mathf.Exp(-FollowLerp*dt));
  }
  ClampCameraTarget();
  if(!pointerDragging)cameraLive=Vector3.Lerp(cameraLive,cameraTarget,1f-Mathf.Exp(-PanLerp*dt));
  ApplyCamera();
  RefreshEdgeStyles();
  RenderStage();
 }

 public void BeginMoveTo(int nodeId){
  if(!ready||runState==null||moving)return;
  if(!ExplorationMapSystem.CanMove(runState,nodeId)){
   // Click-to-break sealed breach / acknowledge hidden when adjacent.
   if(ExplorationMapSystem.Connected(mapDef,runState.currentNodeId,nodeId)
    &&ExplorationMapSystem.TryUnlockEdge(runState,runState.currentNodeId,nodeId)){
    RefreshMarkers();
   }
   if(!ExplorationMapSystem.CanMove(runState,nodeId))return;
  }
  var node=ExplorationMapSystem.Node(mapDef,nodeId);
  if(node==null||pieceTransform==null)return;
  moveFrom=pieceTransform.position;
  moveTo=ExplorationMapSystem.WorldPos(mapDef,node);moveTo.z=-0.08f;
  moveT=0f;moveToId=nodeId;moving=true;followPiece=true;
 }

 public void SnapCameraToCurrent(bool instant=false){
  if(mapDef==null||runState==null)return;
  var node=ExplorationMapSystem.Node(mapDef,runState.currentNodeId);
  if(node==null)return;
  Vector3 pos=ExplorationMapSystem.WorldPos(mapDef,node);pos.z=-0.08f;
  if(pieceTransform!=null)pieceTransform.position=pos;
  cameraTarget=new Vector3(pos.x,pos.y,0f);
  followPiece=true;
  if(instant){cameraLive=cameraTarget;ApplyCamera();}
 }

 public bool TryPickNode(Vector2 panelPoint,Vector2 panelSize,out int nodeId){
  nodeId=-1;
  if(!ready||mapDef==null||stageCamera==null)return false;
  if(!TryPanelSize(panelSize,out panelSize))return false;
  if(!ScreenToMapPoint(panelPoint,panelSize,out Vector3 hit))return false;
  float best=.7f;
  foreach(var cell in mapDef.cells){
   if(cell.locked)continue;
   if(runState!=null&&!ExplorationMapSystem.IsRevealed(runState,cell.id)){
    // Allow picking sealed breach targets that are graph-adjacent even if not revealed yet.
    if(!ExplorationMapSystem.Connected(mapDef,runState.currentNodeId,cell.id))continue;
    if(ExplorationMapSystem.LinkKind(mapDef,runState.currentNodeId,cell.id)!=ExplorationLinkKind.Breach)continue;
   }
   Vector3 p=ExplorationMapSystem.WorldPos(mapDef,cell);
   float d=Vector2.Distance(new Vector2(hit.x,hit.y),new Vector2(p.x,p.y));
   if(d<best){best=d;nodeId=cell.id;}
  }
  return nodeId>=0;
 }

 public void BeginPan(Vector2 panelPoint,Vector2 panelSize){
  pointerDragging=true;dragStartPanel=panelPoint;dragStartTarget=cameraTarget;TryPanelSize(panelSize,out _);
 }

 public void UpdatePan(Vector2 panelPoint,Vector2 panelSize,bool detachFollow=true){
  if(!pointerDragging)return;
  if(!TryPanelSize(panelSize,out panelSize))return;
  if(detachFollow)followPiece=false;
  float viewH=orthoSize*2f;
  float viewW=viewH*(panelSize.x/Mathf.Max(1f,panelSize.y));
  Vector2 delta=panelPoint-dragStartPanel;
  cameraTarget=dragStartTarget+new Vector3(-delta.x/panelSize.x*viewW,delta.y/panelSize.y*viewH,0f);
  ClampCameraTarget(panelSize);
  cameraLive=cameraTarget;
  ApplyCamera();
 }

 public void EndPan(){pointerDragging=false;}

 public void ApplyWheelDelta(float delta){
  orthoSize=Mathf.Clamp(orthoSize-delta*.4f,MinOrtho,MaxOrtho);
  if(stageCamera!=null)stageCamera.orthographicSize=orthoSize;
  ClampCameraTarget();
  ApplyCamera();
 }

 public void FocusPiece(){
  followPiece=true;
  if(pieceTransform!=null)cameraTarget=pieceTransform.position;
 }

 public void SetSelectedVisual(int selectedId){
  if(runState==null||mapDef==null)return;
  foreach(var kv in nodeRenderers){
   int id=kv.Key;
   var cell=ExplorationMapSystem.Node(mapDef,id);
   bool locked=cell!=null&&cell.locked;
   bool revealed=!locked&&ExplorationMapSystem.IsRevealed(runState,id);
   kv.Value.enabled=revealed;
   if(nodeRings.TryGetValue(id,out var ring))ring.enabled=revealed;
   var marker=kv.Value.transform.parent;
   if(marker!=null){
    var label=marker.Find("label");
    if(label!=null)label.gameObject.SetActive(revealed);
    var check=marker.Find("check");
    if(check!=null){
     var csr=check.GetComponent<SpriteRenderer>();
     if(csr!=null)csr.enabled=revealed&&ExplorationMapSystem.IsCleared(runState,id);
    }
   }
   if(lockVeils.TryGetValue(id,out var veil))veil.enabled=locked;
   if(!revealed)continue;
   bool selected=id==selectedId;
   bool current=id==runState.currentNodeId;
   bool reachable=ExplorationMapSystem.CanMove(runState,id);
   bool sealedBreach=ExplorationMapSystem.Connected(mapDef,runState.currentNodeId,id)
    &&ExplorationMapSystem.LinkKind(mapDef,runState.currentNodeId,id)==ExplorationLinkKind.Breach
    &&!ExplorationMapSystem.IsEdgeOpened(runState,runState.currentNodeId,id);
   bool cleared=ExplorationMapSystem.IsCleared(runState,id);
   bool hub=cell!=null&&cell.links!=null&&cell.links.Length>=3;
   Color baseCol=NodeColor(cell?.type);
   if(current)baseCol=new Color(1f,.82f,.38f);
   else if(sealedBreach)baseCol=new Color(.55f,.35f,.48f);
   else if(cleared)baseCol=Color.Lerp(baseCol,new Color(.38f,.34f,.30f),.45f);
   else if(selected)baseCol=Color.Lerp(baseCol,new Color(1f,.92f,.55f),.45f);
   else if(reachable)baseCol=Color.Lerp(baseCol,new Color(.95f,.85f,.45f),.28f);
   kv.Value.color=baseCol;
   float s=current?0.42f:(selected||reachable||sealedBreach)?0.36f:hub?0.32f:0.28f;
   kv.Value.transform.localScale=new Vector3(s,s,s);
   if(ring!=null){
    float rs=current?0.62f:(selected||reachable||sealedBreach)?0.52f:hub?0.48f:0.42f;
    ring.transform.localScale=new Vector3(rs,rs,rs);
    ring.enabled=true;
    ring.color=current?new Color(1f,.82f,.35f,.95f)
     :sealedBreach?new Color(.7f,.4f,.55f,.8f)
     :reachable?new Color(.9f,.74f,.38f,.78f)
     :selected?new Color(1f,.92f,.55f,.55f)
     :new Color(.78f,.66f,.42f,.38f);
   }
  }
 }

 void OnDestroy(){Teardown();}

 void Teardown(){
  ready=false;boundMapId=null;
  nodeMarkers.Clear();edgeLines.Clear();nodeRenderers.Clear();nodeRings.Clear();edgeByKey.Clear();edgeKindByKey.Clear();lockVeils.Clear();
  if(renderTarget!=null){
   if(RenderTexture.active==renderTarget)RenderTexture.active=null;
   renderTarget.Release();Destroy(renderTarget);renderTarget=null;
  }
  stageCamera=null;
  for(int i=transform.childCount-1;i>=0;i--){var c=transform.GetChild(i);if(c!=null)Destroy(c.gameObject);}
  foreach(var s in sprites)if(s!=null)Destroy(s);sprites.Clear();
  foreach(var m in materials)if(m!=null)Destroy(m);materials.Clear();
 }

 void RebuildGraph(){
  ClearOverlays();
  if(mapRoot!=null)for(int i=mapRoot.childCount-1;i>=0;i--)Destroy(mapRoot.GetChild(i).gameObject);
  mapRenderer=null;
  BuildMapSurface();BuildEdges();BuildNodes();BuildPiece();RefreshMarkers();
 }

 void ClearOverlays(){
  nodeMarkers.Clear();edgeLines.Clear();nodeRenderers.Clear();nodeRings.Clear();edgeByKey.Clear();edgeKindByKey.Clear();lockVeils.Clear();
  pieceTransform=null;pieceRenderer=null;pieceShadow=null;
  if(overlayRoot!=null)for(int i=overlayRoot.childCount-1;i>=0;i--)Destroy(overlayRoot.GetChild(i).gameObject);
 }

 void BuildStage(){
  Teardown();
  if(mapDef==null)mapDef=ExplorationMapCatalog.OldSpireIso;
  orthoSize=DefaultOrtho;
  labelFont=Resources.Load<Font>("Fonts/KleeOne-Regular")??Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
  int rtW=Mathf.Max(1280,Screen.width),rtH=Mathf.Max(720,Screen.height);
  renderTarget=new RenderTexture(rtW,rtH,16,RenderTextureFormat.ARGB32){name="PackspireExplorationRite",filterMode=FilterMode.Bilinear};
  renderTarget.Create();

  var cameraGo=new GameObject("ExplorationCamera");
  cameraGo.transform.SetParent(transform,false);
  SetLayer(cameraGo);
  stageCamera=cameraGo.AddComponent<Camera>();
  stageCamera.orthographic=true;
  stageCamera.orthographicSize=orthoSize;
  stageCamera.clearFlags=CameraClearFlags.SolidColor;
  stageCamera.backgroundColor=new Color(.04f,.035f,.045f);
  stageCamera.cullingMask=1<<StageLayer;
  stageCamera.targetTexture=renderTarget;
  stageCamera.enabled=false;
  stageCamera.nearClipPlane=.05f;
  stageCamera.farClipPlane=40f;

  mapRoot=new GameObject("MapRoot").transform;
  mapRoot.SetParent(transform,false);
  SetLayer(mapRoot.gameObject);
  BuildMapSurface();
  overlayRoot=new GameObject("Overlay").transform;
  overlayRoot.SetParent(transform,false);
  SetLayer(overlayRoot.gameObject);
  BuildEdges();BuildNodes();BuildPiece();
  ready=true;
  RefreshMarkers();
  SnapCameraToCurrent(true);
  RenderStage();
 }

 void BuildMapSurface(){
  if(mapRoot==null||mapDef==null)return;
  // Always rite board — fort plate is intentionally unused.
  var tex=mapDef.IsInterior?MakeRiteInteriorTexture(mapDef.interiorTone):MakeRiteBoardTexture();
  var mapSprite=Sprite.Create(tex,new Rect(0,0,tex.width,tex.height),new Vector2(.5f,.5f),100f);
  sprites.Add(mapSprite);
  var mapGo=new GameObject(mapDef.IsInterior?"RiteInterior":"RiteBoard");
  mapGo.transform.SetParent(mapRoot,false);
  SetLayer(mapGo);
  mapRenderer=mapGo.AddComponent<SpriteRenderer>();
  mapRenderer.sprite=mapSprite;
  mapRenderer.sortingOrder=0;
  float spriteW=mapSprite.bounds.size.x,spriteH=mapSprite.bounds.size.y;
  mapGo.transform.localScale=new Vector3(mapDef.mapWidth/Mathf.Max(.01f,spriteW),mapDef.mapHeight/Mathf.Max(.01f,spriteH),1f);
 }

 void BuildEdges(){
  if(mapDef==null)return;
  var seen=new HashSet<int>();
  foreach(var cell in mapDef.cells){
   if(cell.links==null)continue;
   foreach(int to in cell.links){
    int key=EdgeKey(cell.id,to);
    if(!seen.Add(key))continue;
    var other=ExplorationMapSystem.Node(mapDef,to);
    if(other==null)continue;
    if(cell.locked&&other.locked)continue;
    var kind=ExplorationMapSystem.LinkKind(mapDef,cell.id,to);
    edgeKindByKey[key]=kind;
    var go=new GameObject($"conduit-{cell.id}-{to}");
    go.transform.SetParent(overlayRoot,false);
    SetLayer(go);
    var lr=go.AddComponent<LineRenderer>();
    Vector3 pa=ExplorationMapSystem.WorldPos(mapDef,cell);pa.z=-0.02f;
    Vector3 pb=ExplorationMapSystem.WorldPos(mapDef,other);pb.z=-0.02f;
    ConfigureConduit(lr,pa,pb,kind,false,false,false);
    var shader=Shader.Find("Sprites/Default")??Shader.Find("Unlit/Color");
    if(shader!=null){var mat=new Material(shader);materials.Add(mat);lr.material=mat;}
    lr.sortingOrder=2;lr.useWorldSpace=true;lr.numCapVertices=4;lr.numCornerVertices=3;
    edgeLines.Add(lr);edgeByKey[key]=lr;
   }
  }
 }

 void ConfigureConduit(LineRenderer lr,Vector3 pa,Vector3 pb,ExplorationLinkKind kind,bool visible,bool walkable,bool opened){
  // Soft bend for organic rite feel (hidden uses same path, ghost styling below).
  Vector3 mid=(pa+pb)*.5f;
  Vector3 perp=new Vector3(-(pb.y-pa.y),pb.x-pa.x,0f).normalized*((pa-pb).magnitude*.04f);
  mid+=perp;
  lr.positionCount=3;
  lr.SetPosition(0,pa);lr.SetPosition(1,mid);lr.SetPosition(2,pb);
  lr.enabled=visible;
  if(!visible)return;
  if(kind==ExplorationLinkKind.Breach&&!opened){
   lr.startColor=lr.endColor=new Color(.42f,.28f,.38f,.55f);
   lr.startWidth=lr.endWidth=.055f;
  } else if(kind==ExplorationLinkKind.Hidden){
   lr.startColor=lr.endColor=walkable?new Color(.75f,.62f,.95f,.7f):new Color(.55f,.45f,.72f,.4f);
   lr.startWidth=lr.endWidth=.028f;
  } else if(walkable){
   lr.startColor=lr.endColor=new Color(1f,.86f,.48f,.88f);
   lr.startWidth=lr.endWidth=.05f;
  } else {
   lr.startColor=lr.endColor=new Color(.78f,.64f,.36f,.42f);
   lr.startWidth=lr.endWidth=.032f;
  }
 }

 void BuildNodes(){
  foreach(var cell in mapDef.cells){
   var root=new GameObject($"cell-{cell.id}");
   root.transform.SetParent(overlayRoot,false);
   SetLayer(root);
   Vector3 p=ExplorationMapSystem.WorldPos(mapDef,cell);p.z=-0.05f;
   root.transform.position=p;

   if(cell.locked){
    var veilGo=new GameObject("lock");
    veilGo.transform.SetParent(root.transform,false);
    SetLayer(veilGo);
    var veil=veilGo.AddComponent<SpriteRenderer>();
    veil.sprite=CircleSprite();
    veil.sortingOrder=4;
    veil.color=new Color(0.06f,0.05f,0.09f,.45f);
    veilGo.transform.localScale=Vector3.one*.5f;
    lockVeils[cell.id]=veil;
    nodeMarkers.Add(root.transform);
    continue;
   }

   var ringGo=new GameObject("ring");
   ringGo.transform.SetParent(root.transform,false);
   SetLayer(ringGo);
   var ring=ringGo.AddComponent<SpriteRenderer>();
   ring.sprite=RingSprite();ring.sortingOrder=3;ring.color=new Color(.85f,.7f,.4f,.35f);
   ringGo.transform.localScale=Vector3.one*.48f;
   nodeRings[cell.id]=ring;

   var bodyGo=new GameObject("body");
   bodyGo.transform.SetParent(root.transform,false);
   SetLayer(bodyGo);
   var sr=bodyGo.AddComponent<SpriteRenderer>();
   sr.sprite=NodeIconSprite(cell.type);sr.sortingOrder=5;sr.color=NodeColor(cell.type);
   bodyGo.transform.localScale=Vector3.one*.3f;
   nodeRenderers[cell.id]=sr;

   var checkGo=new GameObject("check");
   checkGo.transform.SetParent(root.transform,false);
   SetLayer(checkGo);
   checkGo.transform.localPosition=new Vector3(.16f,.16f,-.02f);
   checkGo.transform.localScale=Vector3.one*.18f;
   var check=checkGo.AddComponent<SpriteRenderer>();
   check.sprite=CircleSprite();check.sortingOrder=7;check.color=new Color(.95f,.8f,.4f,.95f);check.enabled=false;

   if(cell.landmark||cell.type is "entrance" or "battle" or "event" or "building_door" or "rest"){
    var labelGo=new GameObject("label");
    labelGo.transform.SetParent(root.transform,false);
    SetLayer(labelGo);
    labelGo.transform.localPosition=new Vector3(0f,-.34f,-.02f);
    var tm=labelGo.AddComponent<TextMesh>();
    tm.text=ShortLabel(cell);tm.characterSize=.032f;tm.fontSize=40;
    tm.anchor=TextAnchor.UpperCenter;tm.alignment=TextAlignment.Center;
    tm.color=new Color(.96f,.88f,.68f);tm.fontStyle=FontStyle.Bold;
    if(labelFont!=null){tm.font=labelFont;var mr=labelGo.GetComponent<MeshRenderer>();if(mr!=null&&labelFont.material!=null)mr.sharedMaterial=labelFont.material;}
   }
   nodeMarkers.Add(root.transform);
  }
 }

 void BuildPiece(){
  var shadowGo=new GameObject("PieceShadow");
  shadowGo.transform.SetParent(overlayRoot,false);
  SetLayer(shadowGo);
  pieceShadow=shadowGo.AddComponent<SpriteRenderer>();
  pieceShadow.sprite=CircleSprite();pieceShadow.sortingOrder=6;pieceShadow.color=new Color(0,0,0,.5f);
  shadowGo.transform.localScale=new Vector3(.4f,.2f,1f);

  var go=new GameObject("PlayerPiece");
  go.transform.SetParent(overlayRoot,false);
  SetLayer(go);
  pieceTransform=go.transform;
  pieceRenderer=go.AddComponent<SpriteRenderer>();
  pieceRenderer.sprite=PawnSprite();pieceRenderer.sortingOrder=9;
  go.transform.localScale=Vector3.one*.55f;
 }

 void RefreshMarkers(){
  if(runState==null||mapDef==null)return;
  var cur=ExplorationMapSystem.Node(mapDef,runState.currentNodeId);
  if(cur!=null&&pieceTransform!=null&&!moving){
   Vector3 p=ExplorationMapSystem.WorldPos(mapDef,cur);p.z=-0.08f;
   pieceTransform.position=p;
   if(pieceShadow!=null){var sp=p;sp.z=-0.01f;sp.y-=0.06f;pieceShadow.transform.position=sp;}
  }
  SetSelectedVisual(runState.selectedNodeId);
  RefreshEdgeStyles();
 }

 void RefreshEdgeStyles(){
  if(runState==null||mapDef==null)return;
  foreach(var cell in mapDef.cells){
   if(cell.links==null)continue;
   foreach(int to in cell.links){
    if(to<cell.id)continue;
    if(!edgeByKey.TryGetValue(EdgeKey(cell.id,to),out var lr)||lr==null)continue;
    var other=ExplorationMapSystem.Node(mapDef,to);
    if(other==null)continue;
    var kind=edgeKindByKey.TryGetValue(EdgeKey(cell.id,to),out var k)?k:ExplorationMapSystem.LinkKind(mapDef,cell.id,to);
    bool unlocked=!cell.locked&&!other.locked;
    bool aRev=ExplorationMapSystem.IsRevealed(runState,cell.id);
    bool bRev=ExplorationMapSystem.IsRevealed(runState,to);
    bool opened=kind!=ExplorationLinkKind.Breach||ExplorationMapSystem.IsEdgeOpened(runState,cell.id,to);
    bool hiddenKnown=kind!=ExplorationLinkKind.Hidden||ExplorationMapSystem.IsHiddenKnown(runState,cell.id,to);
    // Show conduit if either end revealed (or breach seal adjacent to current), and hidden only when known.
    bool adjacentSeal=kind==ExplorationLinkKind.Breach&&!opened
     &&(runState.currentNodeId==cell.id||runState.currentNodeId==to);
    bool visible=unlocked&&hiddenKnown&&(aRev||bRev||adjacentSeal);
    if(kind==ExplorationLinkKind.Hidden&&!hiddenKnown)visible=false;
    bool fromCurrent=cell.id==runState.currentNodeId||to==runState.currentNodeId;
    bool walkable=fromCurrent&&(ExplorationMapSystem.CanMove(runState,cell.id)||ExplorationMapSystem.CanMove(runState,to));
    Vector3 pa=ExplorationMapSystem.WorldPos(mapDef,cell);pa.z=-0.02f;
    Vector3 pb=ExplorationMapSystem.WorldPos(mapDef,other);pb.z=-0.02f;
    ConfigureConduit(lr,pa,pb,kind,visible,walkable,opened);
   }
  }
 }

 void ApplyCamera(){
  if(stageCamera==null)return;
  stageCamera.orthographicSize=orthoSize;
  Vector3 look=new Vector3(cameraLive.x,cameraLive.y,0f);
  stageCamera.transform.SetPositionAndRotation(look+new Vector3(0f,0f,-CamDistance),Quaternion.identity);
 }

 bool ScreenToMapPoint(Vector2 panelPoint,Vector2 panelSize,out Vector3 hit){
  hit=default;
  if(stageCamera==null||!TryPanelSize(panelSize,out panelSize))return false;
  float vx=Mathf.Clamp01(panelPoint.x/panelSize.x);
  float vy=Mathf.Clamp01(1f-panelPoint.y/panelSize.y);
  float viewH=orthoSize*2f;
  float viewW=viewH*(panelSize.x/panelSize.y);
  hit=new Vector3(cameraLive.x+(vx-.5f)*viewW,cameraLive.y+(vy-.5f)*viewH,0f);
  return true;
 }

 bool TryPanelSize(Vector2 panelSize,out Vector2 size){
  size=panelSize;
  if(size.x>=2f&&size.y>=2f)return true;
  if(renderTarget!=null){size=new Vector2(renderTarget.width,renderTarget.height);return true;}
  return false;
 }

 void ClampCameraTarget(Vector2 panelSize=default){
  if(mapDef==null)return;
  float halfW=mapDef.mapWidth*.5f,halfH=mapDef.mapHeight*.5f;
  float aspect=1.6f;
  if(panelSize.x>=2f&&panelSize.y>=2f)aspect=panelSize.x/panelSize.y;
  else if(renderTarget!=null&&renderTarget.height>0)aspect=(float)renderTarget.width/renderTarget.height;
  float maxX=halfW+orthoSize*aspect*.25f;
  float maxY=halfH+orthoSize*.25f;
  cameraTarget.x=Mathf.Clamp(cameraTarget.x,-maxX,maxX);
  cameraTarget.y=Mathf.Clamp(cameraTarget.y,-maxY,maxY);
  cameraTarget.z=0f;
 }

 void RenderStage(){
  if(stageCamera==null||renderTarget==null)return;
  var prev=RenderTexture.active;
  RenderTexture.active=renderTarget;
  GL.Clear(true,true,stageCamera.backgroundColor);
  RenderTexture.active=prev;
  stageCamera.Render();
 }

 static int EdgeKey(int a,int b)=>a<b?(a*1000+b):(b*1000+a);

 static Color NodeColor(string type)=>type switch{
  "entrance" or "exit"=>new Color(.62f,.78f,.95f),
  "building_door"=>new Color(.86f,.62f,.95f),
  "battle"=>new Color(.95f,.48f,.42f),
  "event"=>new Color(.68f,.55f,.92f),
  "rest"=>new Color(.52f,.82f,.62f),
  _=>new Color(.9f,.78f,.48f),
 };

 static string ShortLabel(ExplorationCellDef cell){
  if(cell==null)return "";
  if(!string.IsNullOrEmpty(cell.name)&&cell.name.Length<=6)return cell.name;
  return cell.type switch{
   "entrance"=>"入口","exit"=>"出口","building_door"=>"建物","battle"=>"戦闘","event"=>"出来事","rest"=>"休憩",
   _=>cell.name.Length>5?cell.name.Substring(0,5):cell.name,
  };
 }

 Sprite CircleSprite(){
  int s=48;var tex=new Texture2D(s,s,TextureFormat.ARGB32,false);var cols=new Color[s*s];float c=(s-1)*.5f;
  for(int y=0;y<s;y++)for(int x=0;x<s;x++){float d=Vector2.Distance(new Vector2(x,y),new Vector2(c,c));cols[y*s+x]=d<=c-1.5f?Color.white:Color.clear;}
  tex.SetPixels(cols);tex.Apply();var sp=Sprite.Create(tex,new Rect(0,0,s,s),new Vector2(.5f,.5f),48f);sprites.Add(sp);return sp;
 }

 Sprite RingSprite(){
  int s=56;var tex=new Texture2D(s,s,TextureFormat.ARGB32,false);var cols=new Color[s*s];float c=(s-1)*.5f;
  for(int y=0;y<s;y++)for(int x=0;x<s;x++){float d=Vector2.Distance(new Vector2(x,y),new Vector2(c,c));cols[y*s+x]=(d>c-6f&&d<c-1f)?Color.white:Color.clear;}
  tex.SetPixels(cols);tex.Apply();var sp=Sprite.Create(tex,new Rect(0,0,s,s),new Vector2(.5f,.5f),48f);sprites.Add(sp);return sp;
 }

 Sprite NodeIconSprite(string type){
  int s=56;var tex=new Texture2D(s,s,TextureFormat.ARGB32,false);var cols=new Color[s*s];
  for(int i=0;i<cols.Length;i++)cols[i]=Color.clear;
  float c=(s-1)*.5f;
  void Disk(float cx,float cy,float r){for(int y=0;y<s;y++)for(int x=0;x<s;x++)if(Vector2.Distance(new Vector2(x,y),new Vector2(cx,cy))<=r)cols[y*s+x]=Color.white;}
  void Rect(int x0,int y0,int x1,int y1){for(int y=y0;y<=y1;y++)for(int x=x0;x<=x1;x++)if(x>=0&&x<s&&y>=0&&y<s)cols[y*s+x]=Color.white;}
  Disk(c,c,c-3f);
  switch(type){
   case "battle":for(int i=-12;i<=12;i++){Rect(28+i,26,28+i,30);Rect(26,28+i,30,28+i);}break;
   case "event":Rect(25,16,31,32);Disk(c,38,5);break;
   case "building_door":Rect(18,16,38,40);Rect(24,26,32,40);break;
   case "rest":Rect(16,30,40,36);Rect(20,20,36,30);break;
   case "entrance":case "exit":Rect(22,14,34,40);Rect(14,28,42,34);break;
   default:Disk(c,c,7f);break;
  }
  tex.SetPixels(cols);tex.Apply();var sp=Sprite.Create(tex,new Rect(0,0,s,s),new Vector2(.5f,.5f),48f);sprites.Add(sp);return sp;
 }

 Sprite PawnSprite(){
  int s=72;var tex=new Texture2D(s,s,TextureFormat.ARGB32,false);var cols=new Color[s*s];
  for(int i=0;i<cols.Length;i++)cols[i]=Color.clear;
  for(int y=8;y<20;y++)for(int x=18;x<54;x++)cols[y*s+x]=new Color(.78f,.56f,.22f);
  for(int y=18;y<44;y++)for(int x=28;x<44;x++)cols[y*s+x]=new Color(.96f,.74f,.32f);
  for(int y=42;y<64;y++)for(int x=0;x<s;x++){float d=Vector2.Distance(new Vector2(x,y),new Vector2(36,52));if(d<12f)cols[y*s+x]=new Color(1f,.86f,.48f);}
  tex.SetPixels(cols);tex.Apply();var sp=Sprite.Create(tex,new Rect(0,0,s,s),new Vector2(.5f,.1f),48f);sprites.Add(sp);return sp;
 }

 Texture2D MakeRiteBoardTexture(){
  int w=1024,h=576;
  var tex=new Texture2D(w,h,TextureFormat.RGB24,false);
  var cols=new Color[w*h];
  float cx=w*.5f,cy=h*.5f;
  for(int y=0;y<h;y++)for(int x=0;x<w;x++){
   float u=x/(float)(w-1),v=y/(float)(h-1);
   float n=((x*12.9898f+y*78.233f)%1f);
   float grain=.02f*Mathf.Sin(x*.07f+y*.05f);
   Color baseCol=Color.Lerp(new Color(.07f,.06f,.09f),new Color(.11f,.09f,.13f),(u+v)*.35f+grain);
   float dx=(x-cx)/w,dy=(y-cy)/h;
   float r=Mathf.Sqrt(dx*dx*2.2f+dy*dy*2.8f);
   // Concentric gold rings (district metaphor)
   float ring=0f;
   for(int i=1;i<=5;i++){
    float target=.08f+i*.09f;
    float d=Mathf.Abs(r-target);
    if(d<.006f)ring=Mathf.Max(ring,1f-d/.006f);
   }
   // Outer frame ticks
   float edge=Mathf.Min(Mathf.Min(u,1f-u),Mathf.Min(v,1f-v));
   float frame=edge<.02f?(.02f-edge)/.02f:0f;
   Color gold=new Color(.72f,.58f,.28f);
   Color c=Color.Lerp(baseCol,gold,ring*.22f);
   c=Color.Lerp(c,gold,frame*.35f);
   // Soft vignette
   c*=Mathf.Lerp(.55f,1f,1f-Mathf.Clamp01((r-.55f)*1.6f));
   c.r+=n*.008f;c.g+=n*.006f;c.b+=n*.01f;
   cols[y*w+x]=c;
  }
  tex.SetPixels(cols);tex.Apply(false,false);
  return tex;
 }

 Texture2D MakeRiteInteriorTexture(string tone){
  int w=768,h=576;
  var tex=new Texture2D(w,h,TextureFormat.RGB24,false);
  var cols=new Color[w*h];
  bool barracks=tone=="barracks";
  Color a=barracks?new Color(.12f,.11f,.16f):new Color(.16f,.10f,.08f);
  Color b=barracks?new Color(.09f,.08f,.12f):new Color(.11f,.07f,.06f);
  Color accent=barracks?new Color(.45f,.4f,.55f):new Color(.55f,.38f,.22f);
  float cx=w*.5f,cy=h*.5f;
  for(int y=0;y<h;y++)for(int x=0;x<w;x++){
   Color c=((x/48)+(y/48))%2==0?a:b;
   float dx=(x-cx)/w,dy=(y-cy)/h;
   float r=Mathf.Sqrt(dx*dx+dy*dy);
   if(Mathf.Abs(r-.28f)<.008f||Mathf.Abs(r-.4f)<.006f)c=Color.Lerp(c,accent,.45f);
   cols[y*w+x]=c;
  }
  tex.SetPixels(cols);tex.Apply(false,false);
  return tex;
 }

 void SetLayer(GameObject go){go.layer=StageLayer;foreach(Transform child in go.transform)SetLayer(child.gameObject);}
}
}
