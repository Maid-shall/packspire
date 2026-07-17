using System.Collections.Generic;
using UnityEngine;

namespace Packspire {
/// <summary>Parallax hub street with puppet buildings/character and close-up framing.</summary>
public sealed class PackspirePresentationStage : MonoBehaviour {
 public struct FacilitySlot {
  public string id,label;
  public float worldX;
  public ScreenId screen;
 }

 sealed class HubBuilding {
  public int zone;
  public float worldX;
  public Transform root,highlight;
  public SpriteRenderer flatRenderer,highlightRenderer;
  public PackspireLayeredPuppet puppet;
 }

 sealed class BackdropInstance {
  public HubBackdropLayerDef def;
  public Transform transform;
  public SpriteRenderer renderer;
 }

 const int StageLayer = 29;
 const float PixelsPerUnit = 100f;

 Camera stageCamera;
 PackspireGame hostGame;
 RenderTexture renderTarget;
 Transform actorLayer;
 float coverWidth,coverHeight;
 readonly List<BackdropInstance> backdrops=new();
 readonly List<HubBuilding> buildings=new();
 readonly List<Sprite> sprites=new();
 readonly List<Texture2D> runtimeTextures=new();
 readonly Dictionary<Texture2D,Texture2D> chromaCache=new();
 readonly List<FacilitySlot> facilitySlots=new();

 Transform characterRig;
 PackspireLayeredPuppet characterPuppet;
 float scrollX,targetScrollX,scrollVelocity;
 float scrollMin,scrollMax;
 float moveInput;
 int focusedFacility,hoveredFacility=-1;
 bool pointerDragging;
 float pointerDragStartX,scrollDragStart;
 bool ready;
 readonly List<HubFacilityDef> activeFacilities=new();

 public float OrthographicSize=>HubPresentationCatalog.OrthographicSize;
 public static float CharacterAnchorX=>HubPresentationCatalog.CharacterAnchorX;
 public RenderTexture RenderTarget=>renderTarget;
 public IReadOnlyList<FacilitySlot> Facilities=>facilitySlots;
 public int FocusedFacility=>focusedFacility;
 public float Scroll=>scrollX;
 public bool IsMoving=>Mathf.Abs(moveInput)>.05f||Mathf.Abs(scrollX-targetScrollX)>.03f||Mathf.Abs(scrollVelocity)>.04f||pointerDragging;

 public bool CanEnterAt(int index){
  index=Mathf.Clamp(index,0,facilitySlots.Count-1);
  return Mathf.Abs(scrollX-SnapScrollFor(index))<HubPresentationCatalog.EnterTolerance;
 }

 public void SetFocusedFacility(int index){
  focusedFacility=Mathf.Clamp(index,0,facilitySlots.Count-1);
  targetScrollX=Mathf.Clamp(SnapScrollFor(focusedFacility),scrollMin,scrollMax);
 }

 public void SetFocusedFacilityHighlight(int index){focusedFacility=Mathf.Clamp(index,0,facilitySlots.Count-1);}

 public void SnapToFacility(int index){SetFocusedFacility(index);scrollX=targetScrollX;scrollVelocity=0f;ApplyScroll(scrollX);}

 public void SnapPrevious(){SetFocusedFacility((focusedFacility+facilitySlots.Count-1)%Mathf.Max(1,facilitySlots.Count));scrollX=targetScrollX;scrollVelocity=0f;ApplyScroll(scrollX);}

 public void SnapNext(){SetFocusedFacility((focusedFacility+1)%Mathf.Max(1,facilitySlots.Count));scrollX=targetScrollX;scrollVelocity=0f;ApplyScroll(scrollX);}

 public bool TryPickFacility(Vector2 panelPoint,Vector2 panelSize,out int index){
  index=-1;
  if(!ready||panelSize.x<=1f||panelSize.y<=1f)return false;
  float aspect=panelSize.x/panelSize.y;
  float viewWidth=OrthographicSize*2f*aspect;
  float worldX=(panelPoint.x/panelSize.x-.5f)*viewWidth;
  float best=2.5f;
  for(int i=0;i<facilitySlots.Count;i++){
   float buildingX=facilitySlots[i].worldX-scrollX*HubPresentationCatalog.BuildingParallax;
   if(Mathf.Abs(worldX-buildingX)<best){best=Mathf.Abs(worldX-buildingX);index=i;}
  }
  return index>=0;
 }

 public void RestoreFocus(int zone){
  zone=Mathf.Clamp(zone,0,facilitySlots.Count-1);
  focusedFacility=zone;
  scrollX=SnapScrollFor(zone);
  targetScrollX=scrollX;
  scrollVelocity=0f;
  if(ready){ApplyScroll(scrollX);RenderStage();}
 }

 public void Tick(){
  if(!ready)return;
  float dt=Time.unscaledDeltaTime;
  float time=Time.unscaledTime;

  if(Mathf.Abs(moveInput)>.05f)
   targetScrollX=Mathf.Clamp(targetScrollX+moveInput*dt*4.2f,scrollMin,scrollMax);
  else
   moveInput=Mathf.MoveTowards(moveInput,0f,dt*7f);

  int nearest=NearestFacilityIndex(scrollX);
  float snapTarget=SnapScrollFor(nearest);
  if(Mathf.Abs(targetScrollX-snapTarget)<1.05f&&Mathf.Abs(scrollVelocity)<.35f&&Mathf.Abs(moveInput)<.01f)
   targetScrollX=Mathf.Lerp(targetScrollX,snapTarget,dt*2.4f);

  scrollX=Mathf.SmoothDamp(scrollX,targetScrollX,ref scrollVelocity,.28f,16f,dt);
  focusedFacility=NearestFacilityIndex(scrollX);
  ApplyScroll(scrollX);
  UpdateCharacterMotion(time);
  float direction=Mathf.Abs(moveInput)>.01f?Mathf.Sign(moveInput):Mathf.Sign(scrollVelocity);
  characterPuppet?.Tick(time,dt,IsMoving,direction,false);
  UpdateBuildingHighlights(time,dt);
  bool debugRig=hostGame!=null&&hostGame.UiDeveloperPanelOpen;
  characterPuppet?.SetDebugVisible(debugRig);
  foreach(var building in buildings)building.puppet?.SetDebugVisible(debugRig);
  RenderStage();
 }

 void RenderStage(){
  if(stageCamera==null||renderTarget==null)return;
  var prev=RenderTexture.active;
  RenderTexture.active=renderTarget;
  GL.Clear(true,true,stageCamera.backgroundColor);
  RenderTexture.active=prev;
  stageCamera.Render();
 }

 public void SetMoveInput(float value)=>moveInput=value;
 public void BeginPointerDrag(float panelX){pointerDragging=true;pointerDragStartX=panelX;scrollDragStart=targetScrollX;}
 public void UpdatePointerDrag(float panelX,float panelWidth){
  if(!pointerDragging||panelWidth<=1f)return;
  float viewWidth=OrthographicSize*2f*(panelWidth/Mathf.Max(1f,720f));
  targetScrollX=Mathf.Clamp(scrollDragStart-(panelX-pointerDragStartX)/panelWidth*viewWidth*1.08f,scrollMin,scrollMax);
 }
 public void EndPointerDrag(){pointerDragging=false;}
 public void ApplyWheelDelta(float delta){targetScrollX=Mathf.Clamp(targetScrollX-delta*2.6f,scrollMin,scrollMax);}

 public void TriggerFacilityInteraction(int index,bool entering=false){
  index=Mathf.Clamp(index,0,buildings.Count-1);
  var building=buildings[index];
  building.puppet?.SetInteraction(true,entering,1f);
 }

 public void SetHoveredFacility(int index){
  hoveredFacility=index>=0&&index<buildings.Count?index:-1;
 }

 void Awake(){hostGame=GetComponentInParent<PackspireGame>();BuildStage();}
 void OnDestroy(){TeardownStageContent();}

 void TeardownStageContent(){
  ready=false;
  actorLayer=null;
  backdrops.Clear();
  buildings.Clear();
  facilitySlots.Clear();
  activeFacilities.Clear();
  characterRig=null;
  characterPuppet=null;
  stageCamera=null;
  if(renderTarget!=null){
   if(RenderTexture.active==renderTarget)RenderTexture.active=null;
   renderTarget.Release();
   Destroy(renderTarget);
   renderTarget=null;
  }
  for(int i=transform.childCount-1;i>=0;i--){
   var child=transform.GetChild(i);
   if(child!=null)Destroy(child.gameObject);
  }
  foreach(var sprite in sprites)if(sprite!=null)Destroy(sprite);
  sprites.Clear();
  foreach(var texture in runtimeTextures)if(texture!=null)Destroy(texture);
  runtimeTextures.Clear();
  chromaCache.Clear();
 }

 void BuildStage(){
  TeardownStageContent();

  var meta=hostGame!=null?hostGame.UiMeta:null;
  foreach(var facility in HubPresentationCatalog.Facilities){
   if(!HubPresentationCatalog.IsFacilityUnlocked(facility,meta))continue;
   activeFacilities.Add(facility);
   facilitySlots.Add(new FacilitySlot{id=facility.id,label=facility.label,worldX=facility.worldX,screen=facility.screen});
  }
  scrollMin=HubPresentationCatalog.ScrollMinFor(activeFacilities);
  scrollMax=HubPresentationCatalog.ScrollMaxFor(activeFacilities);

  int rtW=Mathf.Max(1280,Screen.width),rtH=Mathf.Max(720,Screen.height);
  float aspect=(float)rtW/rtH;
  coverWidth=OrthographicSize*2f*aspect*HubPresentationCatalog.CoverWidthScale;
  coverHeight=OrthographicSize*2f*HubPresentationCatalog.CoverHeightScale;

  renderTarget=new RenderTexture(rtW,rtH,16,RenderTextureFormat.ARGB32){name="PackspireHubStage",filterMode=FilterMode.Bilinear};
  renderTarget.Create();

  var cameraGo=new GameObject("StageCamera");
  cameraGo.transform.SetParent(transform,false);
  cameraGo.layer=StageLayer;
  stageCamera=cameraGo.AddComponent<Camera>();
  stageCamera.orthographic=true;
  stageCamera.orthographicSize=OrthographicSize;
  stageCamera.transform.position=new Vector3(0f,HubPresentationCatalog.CameraCenterY,-10f);
  stageCamera.clearFlags=CameraClearFlags.SolidColor;
  stageCamera.backgroundColor=new Color(.24f,.30f,.40f);
  stageCamera.cullingMask=1<<StageLayer;
  stageCamera.targetTexture=renderTarget;
  stageCamera.enabled=false;
  stageCamera.allowHDR=false;

  BuildBackdrops();
  BuildBuildings();
  actorLayer=NewLayer("Actor layer");
  AddCharacter();

  focusedFacility=facilitySlots.Count==0?0:Mathf.Clamp(facilitySlots.Count/2,0,facilitySlots.Count-1);
  scrollX=SnapScrollFor(focusedFacility);
  targetScrollX=scrollX;
  scrollVelocity=0f;
  ApplyScroll(scrollX);
  ready=true;
  RenderStage();
 }

 void BuildBackdrops(){
  foreach(var layer in HubPresentationCatalog.BackdropLayers){
   var root=NewLayer("backdrop-"+layer.id);
   var texture=LoadArt(layer.resource,layer.fallback);
   if(texture==null)continue;
   if(layer.keyGreen||layer.keyBlack)texture=KeyEnvironment(texture,layer.keyGreen,layer.keyBlack);
   float layerCoverW=layer.tiled?Mathf.Max(coverWidth,coverWidth*1.25f):coverWidth;
   float layerCoverH=Mathf.Max(layer.coverHeight,coverHeight*.92f);
   var renderer=AddCoverSprite(root,texture,layer.sortOrder,layerCoverW,layerCoverH,new Vector2(.5f,.5f));
   renderer.transform.localPosition=new Vector3(layer.baseX,layer.y,0f);
   renderer.color=layer.tint;
   if(layer.tiled)renderer.sprite.texture.wrapMode=TextureWrapMode.Repeat;
   backdrops.Add(new BackdropInstance{def=layer,transform=root,renderer=renderer});
  }
 }

 void BuildBuildings(){
  foreach(var def in activeFacilities){
   var building=new HubBuilding{zone=buildings.Count,worldX=def.worldX};
   building.root=new GameObject("facility-"+def.id).transform;
   building.root.SetParent(transform,false);
   SetLayer(building.root.gameObject);
   if(def.HasPuppet){
    building.puppet=new PackspireLayeredPuppet(
     building.root,
     def.id+"-puppet",
     def.puppetParts,
     def.height,
     StageLayer,
     sprites);
   } else {
    var source=LoadArt(def.texturePath);
    if(source!=null){
     var keyed=KeyEnvironment(source,true,true);
     building.flatRenderer=AddSprite(building.root,keyed,24,def.height,true,new Vector2(.5f,0f));
    }
   }

   var highlightTexture=CreateGlowTexture(96);
   building.highlightRenderer=AddSprite(building.root,highlightTexture,14,1.45f,false,new Vector2(.5f,.5f));
   building.highlight=building.highlightRenderer.transform;
   building.highlight.localPosition=new Vector3(0f,def.height*.28f,0f);
   building.highlightRenderer.color=new Color(1f,.84f,.42f,0f);
   buildings.Add(building);
  }
 }

 void AddCharacter(){
  characterPuppet=new PackspireLayeredPuppet(
   actorLayer,
   "Character rig",
   HubPresentationCatalog.CharacterPuppet,
   HubPresentationCatalog.CharacterDisplayHeight,
   StageLayer,
   sprites);
  characterRig=characterPuppet.Root;
  characterRig.localPosition=new Vector3(CharacterAnchorX,HubPresentationCatalog.CharacterGroundY,0f);
 }

 void ApplyScroll(float scroll){
  foreach(var backdrop in backdrops){
   if(backdrop.transform==null)continue;
   backdrop.transform.localPosition=new Vector3(backdrop.def.baseX-scroll*backdrop.def.parallax,backdrop.def.y,0f);
  }

  float buildingScroll=scroll*HubPresentationCatalog.BuildingParallax;
  foreach(var building in buildings){
   float rootX=building.worldX-buildingScroll;
   building.root.position=new Vector3(rootX,HubPresentationCatalog.GroundY,0f);
  }
 }

 void UpdateCharacterMotion(float time){
  if(characterRig==null)return;
  bool moving=IsMoving;
  float runBob=moving?Mathf.Abs(Mathf.Sin(time*11.5f))*.045f:0f;
  float runLean=moving?Mathf.Sin(time*11.5f)*1.6f:0f;
  characterRig.localPosition=new Vector3(CharacterAnchorX,HubPresentationCatalog.CharacterGroundY+runBob,0f);
  characterRig.localRotation=Quaternion.Euler(0f,0f,runLean);
 }

 void UpdateBuildingHighlights(float time,float deltaTime){
  for(int i=0;i<buildings.Count;i++){
   var building=buildings[i];
   bool active=building.zone==focusedFacility;
   bool near=CanEnterAt(building.zone);
   building.puppet?.SetHover(i==hoveredFacility);
   building.puppet?.Tick(time,deltaTime,false,0f,false);
   float pulse=.55f+.45f*Mathf.Sin(time*3.2f);
   if(building.highlightRenderer!=null){
    building.highlightRenderer.color=new Color(1f,.88f,.42f,active&&near?(.20f+.16f*pulse):0f);
    building.highlight.localScale=Vector3.one*(1f+(active&&near?pulse*.04f:0f));
   }
   if(building.flatRenderer!=null)
    building.flatRenderer.color=Color.Lerp(new Color(.92f,.94f,.98f),Color.white,active&&near?.14f:0f);
  }
 }

 float SnapScrollFor(int index){
  index=Mathf.Clamp(index,0,facilitySlots.Count-1);
  return (facilitySlots[index].worldX-HubPresentationCatalog.EntranceScreenX)/HubPresentationCatalog.BuildingParallax;
 }

 int NearestFacilityIndex(float value){
  int best=0;
  float bestDist=float.MaxValue;
  for(int i=0;i<facilitySlots.Count;i++){
   float dist=Mathf.Abs(value-SnapScrollFor(i));
   if(dist<bestDist){bestDist=dist;best=i;}
  }
  return best;
 }

 Transform NewLayer(string name){
  var value=new GameObject(name).transform;
  value.SetParent(transform,false);
  SetLayer(value.gameObject);
  return value;
 }

 SpriteRenderer AddCoverSprite(Transform parent,Texture2D texture,int order,float targetWidth,float targetHeight,Vector2 pivot){
  var sprite=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),pivot,PixelsPerUnit);
  sprites.Add(sprite);
  var go=new GameObject(texture.name);
  go.transform.SetParent(parent,false);
  SetLayer(go);
  var renderer=go.AddComponent<SpriteRenderer>();
  renderer.sprite=sprite;
  renderer.sortingOrder=order;
  float sx=targetWidth/Mathf.Max(.01f,sprite.bounds.size.x);
  float sy=targetHeight/Mathf.Max(.01f,sprite.bounds.size.y);
  go.transform.localScale=Vector3.one*Mathf.Max(sx,sy);
  return renderer;
 }

 SpriteRenderer AddSprite(Transform parent,Texture2D texture,int order,float targetHeight,bool byHeight,Vector2 pivot){
  var sprite=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),pivot,PixelsPerUnit);
  sprites.Add(sprite);
  var go=new GameObject(texture.name);
  go.transform.SetParent(parent,false);
  SetLayer(go);
  var renderer=go.AddComponent<SpriteRenderer>();
  renderer.sprite=sprite;
  renderer.sortingOrder=order;
  float size=byHeight?sprite.bounds.size.y:sprite.bounds.size.x;
  go.transform.localScale=Vector3.one*(targetHeight/Mathf.Max(.01f,size));
  return renderer;
 }

 static Texture2D LoadArt(string primary,string fallback=null){
  var texture=Resources.Load<Texture2D>(primary);
  if(texture==null&&!string.IsNullOrEmpty(fallback))texture=Resources.Load<Texture2D>(fallback);
  return texture;
 }

 Texture2D KeyEnvironment(Texture2D source,bool keyGreen,bool keyBlack){
  if(source==null)return null;
  if(chromaCache.TryGetValue(source,out var cached))return cached;
  var readable=EnsureReadable(source);
  var tex=new Texture2D(readable.width,readable.height,TextureFormat.RGBA32,false);
  var pixels=readable.GetPixels();
  for(int i=0;i<pixels.Length;i++){
   var c=pixels[i];
   if(keyBlack&&c.r<.07f&&c.g<.07f&&c.b<.07f)c=Color.clear;
   else if(keyGreen){
    float competing=Mathf.Max(c.r,c.b);
    float dominance=c.g-competing;
    float strength=Mathf.InverseLerp(.015f,.09f,dominance)*Mathf.InverseLerp(.22f,.5f,c.g);
    if(strength>0f){
     c.a*=1f-Mathf.Clamp01(strength);
     c.g=Mathf.Min(c.g,competing*1.06f);
     if(strength>.98f)c.r=c.g=c.b=0f;
    }
   }
   pixels[i]=c;
  }
  tex.SetPixels(pixels);
  tex.Apply(false,true);
  if(readable!=source)Destroy(readable);
  chromaCache[source]=tex;
  runtimeTextures.Add(tex);
  return tex;
 }

 static Texture2D EnsureReadable(Texture2D source){
  if(source.isReadable)return source;
  var rt=RenderTexture.GetTemporary(source.width,source.height,0,RenderTextureFormat.ARGB32);
  var prev=RenderTexture.active;
  try{
   Graphics.Blit(source,rt);
   RenderTexture.active=rt;
   var tex=new Texture2D(source.width,source.height,TextureFormat.RGBA32,false);
   tex.ReadPixels(new Rect(0,0,source.width,source.height),0,0);
   tex.Apply();
   return tex;
  } finally {
   RenderTexture.active=prev;
   RenderTexture.ReleaseTemporary(rt);
  }
 }

 Texture2D CreateGlowTexture(int size){
  var texture=new Texture2D(size,size,TextureFormat.RGBA32,false);
  runtimeTextures.Add(texture);
  var pixels=new Color[size*size];
  for(int y=0;y<size;y++)for(int x=0;x<size;x++){
   float dx=(x+.5f)/size-.5f,dy=(y+.5f)/size-.5f,d=Mathf.Sqrt(dx*dx+dy*dy);
   float alpha=(1f-Mathf.SmoothStep(.18f,.5f,d))*.85f;
   pixels[y*size+x]=new Color(1f,.84f,.42f,alpha);
  }
  texture.SetPixels(pixels);
  texture.Apply();
  return texture;
 }

 void SetLayer(GameObject value){value.layer=StageLayer;}
}
}
