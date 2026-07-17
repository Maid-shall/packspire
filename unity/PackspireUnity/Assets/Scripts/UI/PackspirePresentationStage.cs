using System.Collections.Generic;
using UnityEngine;

namespace Packspire {
public sealed class PackspirePresentationStage : MonoBehaviour {
 public const float OrthographicSize = 4.15f;
 public const float CharacterAnchorX = -6.35f;
 public const float CharacterHeight = 5.35f;

 public struct FacilitySlot {
  public string id,label,buildingArt;
  public float worldX;
  public ScreenId screen;
 }

 static readonly FacilitySlot[] DefaultFacilities = {
  new(){id="gate",label="遠征門",buildingArt="hub-gate-v1",worldX=-9.2f,screen=ScreenId.Expedition},
  new(){id="guild",label="中央広場",buildingArt="hub-guild-v1",worldX=0f,screen=ScreenId.Status},
  new(){id="forge",label="鍛冶場",buildingArt="hub-forge-v1",worldX=9.2f,screen=ScreenId.Vault},
 };

 struct LayerSlot {
  public string resource,fallbackResource;
  public float parallax,y,height,baseX;
  public int sortOrder;
  public bool tiled;
 }

 static readonly LayerSlot[] DefaultLayers = {
  new(){resource="far-background-v1",fallbackResource="old-spire-terrain-v2",parallax=.15f,y=-.35f,height=7.5f,baseX=0f,sortOrder=-50},
  new(){resource="hub-road-v1",fallbackResource="old-spire-terrain-v2",parallax=.35f,y=-.55f,height=5.8f,baseX=0f,sortOrder=-40},
  new(){resource="stone-road-v1",fallbackResource="stone-road-v1",parallax=.55f,y=-1.05f,height=1.35f,baseX=0f,sortOrder=-30,tiled=true},
  new(){resource="foreground-v1",fallbackResource="stone-road-v1",parallax=1.3f,y=-1.15f,height=1.8f,baseX=0f,sortOrder=10},
 };

 Camera stageCamera;
 RenderTexture renderTarget;
 Transform layerRoot,buildingRoot;
 readonly List<Transform> layerTransforms=new();
 readonly List<SpriteRenderer> buildingRenderers=new();
 SpriteRenderer characterRenderer,entranceGlow;
 float scroll,targetScroll;
 float moveInput;
 bool pointerDragging;
 float pointerDragStartX,scrollDragStart;
 int focusedFacility;
 float runPhase;
 bool ready;
 Texture2D characterSheet;
 int characterBody,characterHair;

 public RenderTexture RenderTarget=>renderTarget;
 public IReadOnlyList<FacilitySlot> Facilities=>DefaultFacilities;
 public int FocusedFacility=>focusedFacility;
 public float Scroll=>scroll;
 public bool IsMoving=>Mathf.Abs(moveInput)>.05f||Mathf.Abs(scroll-targetScroll)>.02f||pointerDragging;

 public bool CanEnterAt(int index){
  float snap=SnapScrollFor(Mathf.Clamp(index,0,DefaultFacilities.Length-1));
  return Mathf.Abs(scroll-snap)<.35f;
 }

 public void ConfigureCharacter(Texture2D sheet,int body,int hair){characterSheet=sheet;characterBody=body;characterHair=hair;ApplyCharacterSprite();}
 public void SetFocusedFacility(int index){focusedFacility=Mathf.Clamp(index,0,DefaultFacilities.Length-1);targetScroll=SnapScrollFor(focusedFacility);}
 public void SetFocusedFacilityHighlight(int index){focusedFacility=Mathf.Clamp(index,0,DefaultFacilities.Length-1);}

 public void AddScrollInput(float delta){targetScroll=Mathf.Clamp(targetScroll+delta,MinScroll(),MaxScroll());}
 public void SnapToFacility(int index){SetFocusedFacility(index);}
 public void SnapPrevious(){SetFocusedFacility((focusedFacility+DefaultFacilities.Length-1)%DefaultFacilities.Length);}
 public void SnapNext(){SetFocusedFacility((focusedFacility+1)%DefaultFacilities.Length);}

 public bool TryPickFacility(Vector2 panelPoint,Vector2 panelSize,out int index){
  index=-1;
  if(!ready||panelSize.x<=1f||panelSize.y<=1f)return false;
  float aspect=panelSize.x/panelSize.y;
  float viewHeight=OrthographicSize*2f;
  float viewWidth=viewHeight*aspect;
  float nx=panelPoint.x/panelSize.x;
  float ny=1f-panelPoint.y/panelSize.y;
  float worldX=-viewWidth*.5f+nx*viewWidth;
  float worldY=-viewHeight*.5f+ny*viewHeight;
  float best=1.4f;
  for(int i=0;i<DefaultFacilities.Length;i++){
   float buildingX=DefaultFacilities[i].worldX-scroll;
   float dx=Mathf.Abs(worldX-buildingX);
   float dy=Mathf.Abs(worldY-(-.15f));
   float score=dx+dy*.35f;
   if(score<best){best=score;index=i;}
  }
  return index>=0;
 }

 public void Tick(){
  if(!ready)return;
  float step=moveInput;
  if(Mathf.Abs(step)>.05f)targetScroll=Mathf.Clamp(targetScroll+step*Time.unscaledDeltaTime*4.2f,MinScroll(),MaxScroll());
  scroll=Mathf.Lerp(scroll,targetScroll,1f-Mathf.Exp(-Time.unscaledDeltaTime*9f));
  UpdateLayerPositions();
  UpdateEntranceGlow();
  UpdateCharacterMotion();
  if(stageCamera!=null)stageCamera.Render();
 }

 public void SetMoveInput(float value)=>moveInput=value;

 public void BeginPointerDrag(float panelX){
  pointerDragging=true;
  pointerDragStartX=panelX;
  scrollDragStart=targetScroll;
 }

 public void UpdatePointerDrag(float panelX,float panelWidth){
  if(!pointerDragging||panelWidth<=1f)return;
  float viewWidth=OrthographicSize*2f*(Screen.width/(float)Mathf.Max(1,Screen.height));
  float delta=(panelX-pointerDragStartX)/panelWidth*viewWidth*1.35f;
  targetScroll=Mathf.Clamp(scrollDragStart-delta,MinScroll(),MaxScroll());
 }

 public void EndPointerDrag(){pointerDragging=false;}

 public void ApplyWheelDelta(float delta){
  targetScroll=Mathf.Clamp(targetScroll-delta*.8f,MinScroll(),MaxScroll());
  focusedFacility=NearestFacilityIndex(targetScroll);
 }

 void Awake(){BuildStage();}

 void OnDestroy(){
  if(renderTarget!=null){renderTarget.Release();Destroy(renderTarget);}
  foreach(var cached in generatedTextures)if(cached!=null)Destroy(cached);
  generatedTextures.Clear();
 }

 readonly List<Texture2D> generatedTextures=new();

 void BuildStage(){
  if(ready)return;
  layerRoot=new GameObject("StageLayers").transform;
  layerRoot.SetParent(transform,false);
  buildingRoot=new GameObject("StageBuildings").transform;
  buildingRoot.SetParent(layerRoot,false);

  int rtW=Mathf.Max(1280,Screen.width);
  int rtH=Mathf.Max(720,Screen.height);
  renderTarget=new RenderTexture(rtW,rtH,16,RenderTextureFormat.ARGB32){name="PackspireHubStage",antiAliasing=1,filterMode=FilterMode.Bilinear};
  renderTarget.Create();

  var cameraGo=new GameObject("StageCamera");
  cameraGo.transform.SetParent(transform,false);
  cameraGo.transform.localPosition=new Vector3(0f,0f,-10f);
  stageCamera=cameraGo.AddComponent<Camera>();
  stageCamera.orthographic=true;
  stageCamera.orthographicSize=OrthographicSize;
  stageCamera.clearFlags=CameraClearFlags.SolidColor;
  stageCamera.backgroundColor=new Color(.42f,.58f,.72f,1f);
  stageCamera.cullingMask=1<<LayerMask.NameToLayer("Default");
  stageCamera.targetTexture=renderTarget;
  stageCamera.depth=-10f;
  stageCamera.useOcclusionCulling=false;
  stageCamera.enabled=false;

  foreach(var layer in DefaultLayers){
   var go=new GameObject(layer.resource);
   go.transform.SetParent(layerRoot,false);
   var renderer=go.AddComponent<SpriteRenderer>();
   renderer.sprite=CreateLayerSprite(layer);
   renderer.sortingOrder=layer.sortOrder;
   go.transform.localPosition=new Vector3(layer.baseX,layer.y,0f);
   go.transform.localScale=new Vector3(layer.height*(renderer.sprite.rect.width/renderer.sprite.rect.height),layer.height,1f);
   layerTransforms.Add(go.transform);
  }

  for(int i=0;i<DefaultFacilities.Length;i++){
   var facility=DefaultFacilities[i];
   var go=new GameObject(facility.id);
   go.transform.SetParent(buildingRoot,false);
   var renderer=go.AddComponent<SpriteRenderer>();
   renderer.sprite=CreateBuildingSprite(facility);
   renderer.sortingOrder=-20+i;
   go.transform.localPosition=new Vector3(facility.worldX,-.05f,0f);
   go.transform.localScale=new Vector3(4.6f,4.6f,1f);
   buildingRenderers.Add(renderer);
  }

  var characterGo=new GameObject("StageCharacter");
  characterGo.transform.SetParent(layerRoot,false);
  characterRenderer=characterGo.AddComponent<SpriteRenderer>();
  characterRenderer.sortingOrder=20;
  characterGo.transform.localPosition=new Vector3(CharacterAnchorX,-.95f,0f);
  characterGo.transform.localScale=new Vector3(CharacterHeight*.72f,CharacterHeight,1f);

  var glowGo=new GameObject("EntranceGlow");
  glowGo.transform.SetParent(layerRoot,false);
  entranceGlow=glowGo.AddComponent<SpriteRenderer>();
  entranceGlow.sprite=CreateGlowSprite();
  entranceGlow.sortingOrder=15;
  entranceGlow.color=new Color(1f,.86f,.35f,.85f);
  glowGo.transform.localScale=new Vector3(1.8f,2.4f,1f);
  entranceGlow.enabled=false;

  focusedFacility=1;
  targetScroll=SnapScrollFor(focusedFacility);
  scroll=targetScroll;
  ready=true;
  UpdateLayerPositions();
  ApplyCharacterSprite();
 }

 void UpdateLayerPositions(){
  for(int i=0;i<DefaultLayers.Length&&i<layerTransforms.Count;i++){
   var layer=DefaultLayers[i];
   float x=layer.baseX-scroll*layer.parallax;
   layerTransforms[i].localPosition=new Vector3(x,layer.y,0f);
  }
  for(int i=0;i<DefaultFacilities.Length&&i<buildingRenderers.Count;i++){
   float x=DefaultFacilities[i].worldX-scroll;
   buildingRenderers[i].transform.localPosition=new Vector3(x,-.05f,0f);
  }
 }

 void UpdateEntranceGlow(){
  if(entranceGlow==null)return;
  bool readyEnter=CanEnterAt(focusedFacility);
  entranceGlow.enabled=readyEnter;
  if(!readyEnter)return;
  float x=DefaultFacilities[focusedFacility].worldX-scroll;
  entranceGlow.transform.localPosition=new Vector3(x,-.55f,0f);
  float pulse=.85f+.15f*Mathf.Sin(Time.unscaledTime*6f);
  entranceGlow.color=new Color(1f,.86f,.35f,.55f*pulse);
  var scale=1.6f+.08f*Mathf.Sin(Time.unscaledTime*6f);
  entranceGlow.transform.localScale=new Vector3(scale,scale*1.35f,1f);
 }

 void UpdateCharacterMotion(){
  if(characterRenderer==null)return;
  bool moving=IsMoving;
  runPhase+=Time.unscaledDeltaTime*(moving?9f:0f);
  float bob=moving?Mathf.Sin(runPhase)*.08f:Mathf.Sin(Time.unscaledTime*1.6f)*.015f;
  float lean=moving?Mathf.Sin(runPhase)*.03f:0f;
  characterRenderer.transform.localPosition=new Vector3(CharacterAnchorX+lean,-.95f+bob,0f);
  characterRenderer.flipX=moveInput<-.05f;
 }

 void ApplyCharacterSprite(){
  if(characterRenderer==null)return;
  if(characterSheet==null)characterSheet=Resources.Load<Texture2D>("Art/character-creator-sheet");
  if(characterSheet==null){characterRenderer.sprite=CreateFallbackCharacterSprite();return;}
  var uv=CharacterUv(characterBody,characterHair);
  characterRenderer.sprite=Sprite.Create(characterSheet,new Rect(uv.x*characterSheet.width,uv.y*characterSheet.height,uv.width*characterSheet.width,uv.height*characterSheet.height),new Vector2(.5f,0f),100f);
 }

 static Rect CharacterUv(int body,int hair){
  body=Mathf.Clamp(body,0,3);
  hair=Mathf.Clamp(hair,0,2);
  return new Rect(body*.25f,(2-hair)/3f,.25f,1f/3f);
 }

 Sprite CreateLayerSprite(LayerSlot layer){
  var texture=LoadArt(layer.resource,layer.fallbackResource);
  if(texture==null)texture=GenerateBandTexture(layer.resource,layer.sortOrder==-50?new Color(.55f,.72f,.86f):layer.sortOrder==-40?new Color(.48f,.66f,.52f):layer.sortOrder==-30?new Color(.58f,.54f,.48f):new Color(.32f,.42f,.28f,.35f));
  texture.wrapMode=layer.tiled?TextureWrapMode.Repeat:TextureWrapMode.Clamp;
  return Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f),100f,0,SpriteMeshType.FullRect,Vector4.zero,layer.tiled);
 }

 Sprite CreateBuildingSprite(FacilitySlot facility){
  var texture=LoadArt(facility.buildingArt,null);
  if(texture==null)texture=GenerateBuildingTexture(facility);
  var keyed=ApplyChromaKey(texture,new Color(.02f,.72f,.18f));
  return Sprite.Create(keyed,new Rect(0,0,keyed.width,keyed.height),new Vector2(.5f,0f),100f);
 }

 Sprite CreateGlowSprite(){
  var tex=GenerateGlowTexture();
  return Sprite.Create(tex,new Rect(0,0,tex.width,tex.height),new Vector2(.5f,.5f),100f);
 }

 Sprite CreateFallbackCharacterSprite(){
  var tex=GenerateBandTexture("character-fallback",new Color(.72f,.58f,.44f));
  return Sprite.Create(tex,new Rect(0,0,tex.width,tex.height),new Vector2(.5f,0f),100f);
 }

 static Texture2D LoadArt(string primary,string fallback){
  Texture2D tex=Resources.Load<Texture2D>($"Art/Hub/{primary}");
  if(tex==null)tex=Resources.Load<Texture2D>($"Art/Prototype2_5D/{primary}");
  if(tex==null&&!string.IsNullOrEmpty(fallback))tex=Resources.Load<Texture2D>($"Art/UI/Map/{fallback}");
  if(tex==null&&!string.IsNullOrEmpty(fallback))tex=Resources.Load<Texture2D>($"Art/UI/{fallback}");
  return tex;
 }

 Texture2D ApplyChromaKey(Texture2D source,Color key){
  var tex=new Texture2D(source.width,source.height,TextureFormat.RGBA32,false);
  generatedTextures.Add(tex);
  var pixels=source.GetPixels();
  for(int i=0;i<pixels.Length;i++){
   var pixel=pixels[i];
   float dist=Mathf.Abs(pixel.r-key.r)+Mathf.Abs(pixel.g-key.g)+Mathf.Abs(pixel.b-key.b);
   if(dist<.28f)pixel.a=0f;
   pixels[i]=pixel;
  }
  tex.SetPixels(pixels);
  tex.Apply();
  return tex;
 }

 Texture2D GenerateBandTexture(string id,Color color){
  var tex=new Texture2D(512,128,TextureFormat.RGBA32,false);
  generatedTextures.Add(tex);
  var pixels=new Color[tex.width*tex.height];
  for(int y=0;y<tex.height;y++){
   float t=y/(float)(tex.height-1);
   for(int x=0;x<tex.width;x++){
    float n=(Mathf.PerlinNoise(x*.04f,y*.04f+id.GetHashCode()*.001f)-.5f)*.08f;
    pixels[y*tex.width+x]=Color.Lerp(color,color*1.08f,t+.08f+n);
   }
  }
  tex.SetPixels(pixels);
  tex.Apply();
  return tex;
 }

 Texture2D GenerateBuildingTexture(FacilitySlot facility){
  var tex=new Texture2D(256,320,TextureFormat.RGBA32,false);
  generatedTextures.Add(tex);
  Color wall=facility.id=="gate"?new Color(.58f,.52f,.46f):facility.id=="guild"?new Color(.62f,.46f,.34f):new Color(.48f,.42f,.38f);
  Color roof=facility.id=="gate"?new Color(.36f,.30f,.28f):facility.id=="guild"?new Color(.52f,.22f,.18f):new Color(.34f,.28f,.24f);
  Color green=new Color(.02f,.72f,.18f,1f);
  var pixels=new Color[tex.width*tex.height];
  for(int i=0;i<pixels.Length;i++)pixels[i]=green;
  FillRect(pixels,tex.width,tex.height,48,96,208,300,wall);
  FillRect(pixels,tex.width,tex.height,36,170,220,300,roof);
  FillRect(pixels,tex.width,tex.height,108,118,148,190,new Color(.18f,.12f,.08f,.95f));
  tex.SetPixels(pixels);
  tex.Apply();
  return tex;
 }

 Texture2D GenerateGlowTexture(){
  var tex=new Texture2D(64,64,TextureFormat.RGBA32,false);
  generatedTextures.Add(tex);
  var pixels=new Color[64*64];
  Vector2 center=new(31.5f,31.5f);
  for(int y=0;y<64;y++)for(int x=0;x<64;x++){
   float dist=Vector2.Distance(new Vector2(x,y),center)/31.5f;
   float alpha=Mathf.Clamp01(1f-dist);
   alpha*=alpha;
   pixels[y*64+x]=new Color(1f,.9f,.45f,alpha);
  }
  tex.SetPixels(pixels);
  tex.Apply();
  return tex;
 }

 static void FillRect(Color[] pixels,int width,int height,int x0,int y0,int x1,int y1,Color color){
  for(int y=y0;y<y1;y++)for(int x=x0;x<x1;x++)if(x>=0&&y>=0&&x<width&&y<height)pixels[y*width+x]=color;
 }

 float SnapScrollFor(int index){
  index=Mathf.Clamp(index,0,DefaultFacilities.Length-1);
  return DefaultFacilities[index].worldX+CharacterAnchorX;
 }

 int NearestFacilityIndex(float value){
  int best=0;
  float bestDist=float.MaxValue;
  for(int i=0;i<DefaultFacilities.Length;i++){
   float dist=Mathf.Abs(value-SnapScrollFor(i));
   if(dist<bestDist){bestDist=dist;best=i;}
  }
  return best;
 }

 float MinScroll(){return SnapScrollFor(0)-1.2f;}
 float MaxScroll(){return SnapScrollFor(DefaultFacilities.Length-1)+1.2f;}
}
}
