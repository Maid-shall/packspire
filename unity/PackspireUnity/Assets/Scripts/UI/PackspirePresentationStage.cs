using System.Collections.Generic;
using UnityEngine;

namespace Packspire {
public sealed class PackspirePresentationStage : MonoBehaviour {
 public struct FacilitySlot {
  public string id,label;
  public float worldX;
  public ScreenId screen;
 }

 sealed class BuildingLayerInstance {
  public HubBuildingLayerDef def;
  public SpriteRenderer renderer;
  public Transform transform;
  public bool entrance;
 }

 sealed class BuildingInstance {
  public HubFacilityDef def;
  public Transform root;
  public readonly List<BuildingLayerInstance> layers=new();
  public BuildingLayerInstance entranceLayer;
 }

 Camera stageCamera;
 RenderTexture renderTarget;
 Transform layerRoot;
 readonly List<Transform> backdropTransforms=new();
 readonly List<BuildingInstance> buildings=new();
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

 public float OrthographicSize=>HubPresentationCatalog.OrthographicSize;
 public static float CharacterAnchorX=>HubPresentationCatalog.CharacterAnchorX;

 public RenderTexture RenderTarget=>renderTarget;
 public IReadOnlyList<FacilitySlot> Facilities=>facilitySlots;
 readonly List<FacilitySlot> facilitySlots=new();

 public int FocusedFacility=>focusedFacility;
 public float Scroll=>scroll;
 public bool IsMoving=>Mathf.Abs(moveInput)>.05f||Mathf.Abs(scroll-targetScroll)>.02f||pointerDragging;

 public bool CanEnterAt(int index){
  float snap=SnapScrollFor(Mathf.Clamp(index,0,facilitySlots.Count-1));
  return Mathf.Abs(scroll-snap)<HubPresentationCatalog.EnterTolerance;
 }

 public void ConfigureCharacter(Texture2D sheet,int body,int hair){characterSheet=sheet;characterBody=body;characterHair=hair;ApplyCharacterSprite();}
 public void SetFocusedFacility(int index){focusedFacility=Mathf.Clamp(index,0,facilitySlots.Count-1);targetScroll=SnapScrollFor(focusedFacility);}
 public void SetFocusedFacilityHighlight(int index){focusedFacility=Mathf.Clamp(index,0,facilitySlots.Count-1);}

 public void SnapToFacility(int index){SetFocusedFacility(index);}
 public void SnapPrevious(){SetFocusedFacility((focusedFacility+facilitySlots.Count-1)%Mathf.Max(1,facilitySlots.Count));}
 public void SnapNext(){SetFocusedFacility((focusedFacility+1)%Mathf.Max(1,facilitySlots.Count));}

 public bool TryPickFacility(Vector2 panelPoint,Vector2 panelSize,out int index){
  index=-1;
  if(!ready||panelSize.x<=1f||panelSize.y<=1f)return false;
  float aspect=panelSize.x/panelSize.y;
  float viewHeight=OrthographicSize*2f;
  float viewWidth=viewHeight*aspect;
  float nx=panelPoint.x/panelSize.x;
  float worldX=-viewWidth*.5f+nx*viewWidth;
  float best=1.8f;
  for(int i=0;i<facilitySlots.Count;i++){
   float buildingX=facilitySlots[i].worldX-scroll;
   float dx=Mathf.Abs(worldX-buildingX);
   if(dx<best){best=dx;index=i;}
  }
  return index>=0;
 }

 public void Tick(){
  if(!ready)return;
  if(Mathf.Abs(moveInput)>.05f)targetScroll=Mathf.Clamp(targetScroll+moveInput*Time.unscaledDeltaTime*3.4f,MinScroll(),MaxScroll());
  scroll=Mathf.Lerp(scroll,targetScroll,1f-Mathf.Exp(-Time.unscaledDeltaTime*9f));
  UpdateSceneLayout();
  UpdateEntranceGlow();
  UpdateCharacterMotion();
  stageCamera?.Render();
 }

 public void SetMoveInput(float value)=>moveInput=value;
 public void BeginPointerDrag(float panelX){pointerDragging=true;pointerDragStartX=panelX;scrollDragStart=targetScroll;}
 public void UpdatePointerDrag(float panelX,float panelWidth){
  if(!pointerDragging||panelWidth<=1f)return;
  float viewWidth=OrthographicSize*2f*(Screen.width/(float)Mathf.Max(1,Screen.height));
  targetScroll=Mathf.Clamp(scrollDragStart-(panelX-pointerDragStartX)/panelWidth*viewWidth*1.15f,MinScroll(),MaxScroll());
 }
 public void EndPointerDrag(){pointerDragging=false;}
 public void ApplyWheelDelta(float delta){targetScroll=Mathf.Clamp(targetScroll-delta*.55f,MinScroll(),MaxScroll());focusedFacility=NearestFacilityIndex(targetScroll);}

 void Awake(){BuildStage();}

 void OnDestroy(){
  if(renderTarget!=null){renderTarget.Release();Destroy(renderTarget);}
  foreach(var cached in generatedTextures)if(cached!=null)Destroy(cached);
  generatedTextures.Clear();
 }

 readonly List<Texture2D> generatedTextures=new();

 void BuildStage(){
  if(ready)return;
  facilitySlots.Clear();
  foreach(var facility in HubPresentationCatalog.Facilities)
   facilitySlots.Add(new FacilitySlot{id=facility.id,label=facility.label,worldX=facility.worldX,screen=facility.screen});

  layerRoot=new GameObject("StageLayers").transform;
  layerRoot.SetParent(transform,false);

  int rtW=Mathf.Max(1280,Screen.width),rtH=Mathf.Max(720,Screen.height);
  renderTarget=new RenderTexture(rtW,rtH,16,RenderTextureFormat.ARGB32){name="PackspireHubStage",filterMode=FilterMode.Bilinear};
  renderTarget.Create();

  var cameraGo=new GameObject("StageCamera");
  cameraGo.transform.SetParent(transform,false);
  cameraGo.transform.localPosition=new Vector3(0f,0f,-10f);
  stageCamera=cameraGo.AddComponent<Camera>();
  stageCamera.orthographic=true;
  stageCamera.orthographicSize=OrthographicSize;
  stageCamera.clearFlags=CameraClearFlags.SolidColor;
  stageCamera.backgroundColor=new Color(.52f,.68f,.82f,1f);
  stageCamera.targetTexture=renderTarget;
  stageCamera.enabled=false;

  foreach(var layer in HubPresentationCatalog.BackdropLayers){
   var go=new GameObject(layer.id);
   go.transform.SetParent(layerRoot,false);
   var renderer=go.AddComponent<SpriteRenderer>();
   renderer.sprite=CreateBackdropSprite(layer);
   renderer.sortingOrder=layer.sortOrder;
   var sprite=renderer.sprite;
   float aspect=sprite.rect.width/sprite.rect.height;
   go.transform.localScale=new Vector3(layer.height*aspect,layer.height,1f);
   backdropTransforms.Add(go.transform);
  }

  foreach(var facility in HubPresentationCatalog.Facilities)BuildFacility(facility);

  var characterGo=new GameObject("StageCharacter");
  characterGo.transform.SetParent(layerRoot,false);
  characterRenderer=characterGo.AddComponent<SpriteRenderer>();
  characterRenderer.sortingOrder=24;
  ApplyCharacterTransform();

  var glowGo=new GameObject("EntranceGlow");
  glowGo.transform.SetParent(layerRoot,false);
  entranceGlow=glowGo.AddComponent<SpriteRenderer>();
  entranceGlow.sprite=CreateGlowSprite();
  entranceGlow.sortingOrder=26;
  entranceGlow.enabled=false;

  focusedFacility=Mathf.Min(1,facilitySlots.Count-1);
  targetScroll=SnapScrollFor(focusedFacility);
  scroll=targetScroll;
  ready=true;
  UpdateSceneLayout();
  ApplyCharacterSprite();
 }

 void BuildFacility(HubFacilityDef facility){
  var instance=new BuildingInstance{def=facility};
  var rootGo=new GameObject("facility-"+facility.id);
  rootGo.transform.SetParent(layerRoot,false);
  instance.root=rootGo.transform;
  foreach(var layerDef in facility.layers){
   var layerGo=new GameObject(layerDef.partKey);
   layerGo.transform.SetParent(instance.root,false);
   var renderer=layerGo.AddComponent<SpriteRenderer>();
   renderer.sprite=CreateBuildingLayerSprite(facility.id,layerDef);
   renderer.sortingOrder=10+layerDef.sortBias;
   layerGo.transform.localScale=Vector3.one*layerDef.scale*5.4f;
   var layerInstance=new BuildingLayerInstance{def=layerDef,renderer=renderer,transform=layerGo.transform,entrance=layerDef.entrance};
   instance.layers.Add(layerInstance);
   if(layerDef.entrance)instance.entranceLayer=layerInstance;
  }
  buildings.Add(instance);
 }

 void UpdateSceneLayout(){
  var backdrop=HubPresentationCatalog.BackdropLayers;
  for(int i=0;i<backdrop.Length&&i<backdropTransforms.Count;i++){
   var layer=backdrop[i];
   backdropTransforms[i].localPosition=new Vector3(layer.baseX-scroll*layer.parallax,layer.y,0f);
  }
  foreach(var building in buildings){
   float rootX=building.def.worldX-scroll*HubPresentationCatalog.BuildingParallax;
   building.root.localPosition=new Vector3(rootX,0f,0f);
   foreach(var layer in building.layers){
    float depthParallax=scroll*(1f-layer.def.sortBias*.012f);
    layer.transform.localPosition=new Vector3(layer.def.localX+(building.def.worldX-scroll-depthParallax)*.012f,layer.def.localY,0f);
    layer.renderer.sortingOrder=10+layer.def.sortBias+Mathf.RoundToInt(Mathf.Clamp01((rootX-CharacterAnchorX+2f)/8f)*4f);
   }
  }
 }

 void UpdateEntranceGlow(){
  if(entranceGlow==null||focusedFacility<0||focusedFacility>=buildings.Count)return;
  bool readyEnter=CanEnterAt(focusedFacility);
  entranceGlow.enabled=readyEnter;
  if(!readyEnter)return;
  var building=buildings[focusedFacility];
  var entrance=building.entranceLayer;
  if(entrance==null)return;
  float x=building.root.localPosition.x+entrance.transform.localPosition.x;
  float y=building.root.localPosition.y+entrance.transform.localPosition.y;
  entranceGlow.transform.localPosition=new Vector3(x,y-.08f,0f);
  float pulse=.85f+.15f*Mathf.Sin(Time.unscaledTime*6f);
  entranceGlow.color=new Color(1f,.88f,.42f,.62f*pulse);
  float scale=1.35f+.07f*Mathf.Sin(Time.unscaledTime*6f);
  entranceGlow.transform.localScale=new Vector3(scale,scale*1.25f,1f);
 }

 void UpdateCharacterMotion(){
  if(characterRenderer==null)return;
  bool moving=IsMoving;
  runPhase+=Time.unscaledDeltaTime*(moving?9f:0f);
  float bob=moving?Mathf.Sin(runPhase)*.045f:Mathf.Sin(Time.unscaledTime*1.6f)*.01f;
  float lean=moving?Mathf.Sin(runPhase)*.018f:0f;
  characterRenderer.transform.localPosition=new Vector3(CharacterAnchorX+lean,HubPresentationCatalog.CharacterBaseY+bob,0f);
  characterRenderer.flipX=moveInput<-.05f;
 }

 void ApplyCharacterTransform(){
  if(characterRenderer==null)return;
  float h=HubPresentationCatalog.CharacterHeight;
  characterRenderer.transform.localScale=new Vector3(h*.68f,h,1f);
  characterRenderer.transform.localPosition=new Vector3(CharacterAnchorX,HubPresentationCatalog.CharacterBaseY,0f);
 }

 void ApplyCharacterSprite(){
  if(characterRenderer==null)return;
  if(characterSheet==null)characterSheet=Resources.Load<Texture2D>("Art/character-creator-sheet");
  if(characterSheet==null){characterRenderer.sprite=CreateFallbackCharacterSprite();return;}
  var uv=CharacterPortraitUv(characterBody,characterHair);
  var rect=new Rect(uv.x*characterSheet.width,uv.y*characterSheet.height,uv.width*characterSheet.width,uv.height*characterSheet.height);
  characterRenderer.sprite=Sprite.Create(characterSheet,rect,new Vector2(.5f,.08f),100f);
 }

 static Rect CharacterPortraitUv(int body,int hair){
  body=Mathf.Clamp(body,0,3);
  hair=Mathf.Clamp(hair,0,2);
  float w=.25f,h=1f/3f,x=body*w,y=(2-hair)/3f*h;
  return new Rect(x,y+h*.04f,w,h*.86f);
 }

 Sprite CreateBackdropSprite(HubBackdropLayerDef layer){
  var texture=LoadArt(layer.resource,layer.fallback);
  if(texture==null)texture=GenerateBackdropTexture(layer.id);
  texture.wrapMode=layer.tiled?TextureWrapMode.Repeat:TextureWrapMode.Clamp;
  return Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f),100f,0,SpriteMeshType.FullRect,Vector4.zero,layer.tiled);
 }

 Sprite CreateBuildingLayerSprite(string facilityId,HubBuildingLayerDef layer){
  var texture=LoadArt(layer.ArtName(facilityId),null);
  if(texture==null)texture=GenerateBuildingLayerTexture(facilityId,layer);
  if(texture!=null&&!layer.role.Equals("fg"))texture=ApplyChromaKey(texture,new Color(.02f,.72f,.18f));
  return Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,0f),100f);
 }

 Sprite CreateGlowSprite(){
  var tex=GenerateGlowTexture();
  return Sprite.Create(tex,new Rect(0,0,tex.width,tex.height),new Vector2(.5f,.5f),100f);
 }

 Sprite CreateFallbackCharacterSprite(){
  var tex=GenerateBandTexture("character-fallback",new Color(.72f,.58f,.44f),128,256);
  return Sprite.Create(tex,new Rect(0,0,tex.width,tex.height),new Vector2(.5f,.08f),100f);
 }

 static Texture2D LoadArt(string primary,string fallback){
  if(!string.IsNullOrEmpty(primary)){
   Texture2D tex=Resources.Load<Texture2D>($"Art/Hub/{primary}");
   if(tex==null)tex=Resources.Load<Texture2D>($"Art/Prototype2_5D/{primary}");
   if(tex!=null)return tex;
  }
  if(!string.IsNullOrEmpty(fallback)){
   Texture2D tex=Resources.Load<Texture2D>($"Art/UI/Map/{fallback}");
   if(tex==null)tex=Resources.Load<Texture2D>($"Art/UI/{fallback}");
   return tex;
  }
  return null;
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

 Texture2D GenerateBackdropTexture(string id){
  if(id=="sky")return GenerateSkyTexture();
  if(id=="distant-ridge")return GenerateRidgeTexture(false);
  if(id=="town-silhouette")return GenerateTownSilhouetteTexture();
  if(id=="mid-vista")return GenerateMidVistaTexture();
  if(id=="street-backdrop")return GenerateStreetBackdropTexture();
  if(id=="road")return GenerateRoadTexture();
  if(id=="curb")return GenerateCurbTexture();
  return GenerateStreetPropsTexture();
 }

 Texture2D GenerateSkyTexture(){
  var tex=new Texture2D(512,256,TextureFormat.RGBA32,false);
  generatedTextures.Add(tex);
  var pixels=new Color[tex.width*tex.height];
  Color top=new(.42f,.58f,.78f),bottom=new(.72f,.80f,.88f);
  for(int y=0;y<tex.height;y++){
   float t=y/(float)(tex.height-1);
   Color row=Color.Lerp(bottom,top,t);
   for(int x=0;x<tex.width;x++)pixels[y*tex.width+x]=row;
  }
  tex.SetPixels(pixels);tex.Apply();return tex;
 }

 Texture2D GenerateRidgeTexture(bool dark){
  var tex=new Texture2D(768,180,TextureFormat.RGBA32,false);
  generatedTextures.Add(tex);
  var pixels=new Color[tex.width*tex.height];
  for(int i=0;i<pixels.Length;i++)pixels[i]=new Color(.38f,.48f,.42f,.15f);
  for(int peak=0;peak<6;peak++){
   int cx=peak*140+40;
   for(int x=0;x<tex.width;x++){
    float d=Mathf.Abs(x-cx)/90f;
    int h=Mathf.RoundToInt(Mathf.Lerp(120,20,d));
    for(int y=0;y<h;y++)pixels[y*tex.width+x]=new Color(.28f,.36f,.34f,.85f);
   }
  }
  tex.SetPixels(pixels);tex.Apply();return tex;
 }

 Texture2D GenerateTownSilhouetteTexture(){
  var tex=new Texture2D(768,200,TextureFormat.RGBA32,false);
  generatedTextures.Add(tex);
  var pixels=new Color[tex.width*tex.height];
  Color fog=new(.46f,.54f,.50f,.35f);
  for(int i=0;i<pixels.Length;i++)pixels[i]=Color.clear;
  int[] slots={40,130,220,310,400,490,580,670};
  for(int peak=0;peak<slots.Length;peak++){
   int sx=slots[peak];
   int w=48+(peak*17)%38,h=52+(peak*23)%58,baseY=48;
   Color tone=new(.34f,.30f,.28f,.72f);
   FillRect(pixels,tex.width,tex.height,sx,baseY,sx+w,baseY+h,tone);
   FillRect(pixels,tex.width,tex.height,sx+8,baseY+h-8,sx+w-8,baseY+h+18,new Color(.24f,.20f,.18f,.65f));
  }
  for(int y=0;y<70;y++)for(int x=0;x<tex.width;x++)pixels[y*tex.width+x]=Color.Lerp(pixels[y*tex.width+x],fog,.25f);
  tex.SetPixels(pixels);tex.Apply();return tex;
 }

 Texture2D GenerateMidVistaTexture(){
  var tex=new Texture2D(768,220,TextureFormat.RGBA32,false);
  generatedTextures.Add(tex);
  var pixels=new Color[tex.width*tex.height];
  for(int i=0;i<pixels.Length;i++)pixels[i]=Color.clear;
  for(int x=20;x<tex.width;x+=90){
   int h=Random.Range(40,95);
   FillRect(pixels,tex.width,tex.height,x,30,x+28,30+h,new Color(.22f,.34f,.24f,.78f));
   FillRect(pixels,tex.width,tex.height,x+34,42,x+78,42+Random.Range(36,72),new Color(.48f,.40f,.34f,.55f));
  }
  tex.SetPixels(pixels);tex.Apply();return tex;
 }

 Texture2D GenerateStreetBackdropTexture(){
  var tex=new Texture2D(768,240,TextureFormat.RGBA32,false);
  generatedTextures.Add(tex);
  var pixels=new Color[tex.width*tex.height];
  for(int i=0;i<pixels.Length;i++)pixels[i]=new Color(.40f,.36f,.32f,.18f);
  for(int block=0;block<5;block++){
   int x=block*150+12,h=Random.Range(90,150);
   FillRect(pixels,tex.width,tex.height,x,50,x+110,50+h,new Color(.52f,.44f,.38f,.62f));
   FillRect(pixels,tex.width,tex.height,x+8,50+h-10,x+102,50+h+8,new Color(.30f,.24f,.20f,.55f));
  }
  tex.SetPixels(pixels);tex.Apply();return tex;
 }

 Texture2D GenerateRoadTexture(){
  var tex=new Texture2D(512,128,TextureFormat.RGBA32,false);
  generatedTextures.Add(tex);
  var pixels=new Color[tex.width*tex.height];
  Color base=new(.46f,.42f,.38f);
  for(int y=0;y<tex.height;y++)for(int x=0;x<tex.width;x++){
   float n=Mathf.PerlinNoise(x*.08f,y*.12f)*.08f;
   float edge=Mathf.Abs(y-tex.height*.58f)/tex.height;
   pixels[y*tex.width+x]=Color.Lerp(base,base*.82f,edge+n);
  }
  for(int x=0;x<tex.width;x+=36)for(int y=tex.height/3;y<tex.height*2/3;y++)pixels[y*tex.width+x]=new Color(.36f,.32f,.28f,.35f);
  tex.SetPixels(pixels);tex.wrapMode=TextureWrapMode.Repeat;tex.Apply();return tex;
 }

 Texture2D GenerateCurbTexture(){
  var tex=new Texture2D(512,64,TextureFormat.RGBA32,false);
  generatedTextures.Add(tex);
  var pixels=new Color[tex.width*tex.height];
  for(int y=0;y<tex.height;y++)for(int x=0;x<tex.width;x++)pixels[y*tex.width+x]=y<28?new Color(.34f,.30f,.26f,.85f):Color.clear;
  tex.SetPixels(pixels);tex.Apply();return tex;
 }

 Texture2D GenerateStreetPropsTexture(){
  var tex=new Texture2D(512,96,TextureFormat.RGBA32,false);
  generatedTextures.Add(tex);
  var pixels=new Color[tex.width*tex.height];
  for(int i=0;i<pixels.Length;i++)pixels[i]=Color.clear;
  for(int x=60;x<tex.width;x+=160){
   FillRect(pixels,tex.width,tex.height,x,8,x+8,72,new Color(.28f,.22f,.18f,.75f));
   FillRect(pixels,tex.width,tex.height,x-10,68,x+18,78,new Color(.18f,.14f,.10f,.65f));
  }
  tex.SetPixels(pixels);tex.Apply();return tex;
 }

 Texture2D GenerateBuildingLayerTexture(string facilityId,HubBuildingLayerDef layer){
  var tex=new Texture2D(192,192,TextureFormat.RGBA32,false);
  generatedTextures.Add(tex);
  var pixels=new Color[tex.width*tex.height];
  Color green=new(.02f,.72f,.18f,1f);
  for(int i=0;i<pixels.Length;i++)pixels[i]=green;
  Color wall=facilityId=="gate"?new Color(.58f,.52f,.46f):facilityId=="guild"?new Color(.62f,.46f,.34f):new Color(.48f,.42f,.38f);
  Color roof=facilityId=="gate"?new Color(.36f,.30f,.28f):facilityId=="guild"?new Color(.52f,.22f,.18f):new Color(.34f,.28f,.24f);
  switch(layer.role){
   case "base":FillRect(pixels,tex.width,tex.height,24,24,168,56,new Color(.38f,.34f,.30f));FillRect(pixels,tex.width,tex.height,18,52,174,68,new Color(.32f,.28f,.24f));break;
   case "body":
    FillRect(pixels,tex.width,tex.height,34,56,158,150,wall);
    FillRect(pixels,tex.width,tex.height,34,56,52,150,wall*.82f);
    FillRect(pixels,tex.width,tex.height,140,56,158,150,wall*.72f);
    break;
   case "roof":
    for(int y=96;y<170;y++){
     float t=(y-96)/74f;
     int left=Mathf.RoundToInt(Mathf.Lerp(28,78,t));
     int right=Mathf.RoundToInt(Mathf.Lerp(164,114,t));
     FillRect(pixels,tex.width,tex.height,left,y,right,y+1,roof);
    }
    break;
   case "sign":FillRect(pixels,tex.width,tex.height,72,108,120,132,new Color(.45f,.30f,.16f));FillRect(pixels,tex.width,tex.height,94,132,98,148,new Color(.28f,.20f,.14f));break;
   case "entrance":FillRect(pixels,tex.width,tex.height,78,62,114,132,new Color(.16f,.11f,.08f,.95f));FillRect(pixels,tex.width,tex.height,82,66,110,128,new Color(.08f,.05f,.04f,.9f));break;
   default:FillRect(pixels,tex.width,tex.height,120,20,168,68,new Color(.34f,.28f,.22f,.9f));FillRect(pixels,tex.width,tex.height,124,68,164,74,new Color(.24f,.20f,.16f));break;
  }
  tex.SetPixels(pixels);tex.Apply();return tex;
 }

 Texture2D GenerateBandTexture(string id,Color color,int w,int h){
  var tex=new Texture2D(w,h,TextureFormat.RGBA32,false);
  generatedTextures.Add(tex);
  var pixels=new Color[w*h];
  for(int y=0;y<h;y++)for(int x=0;x<w;x++){
   float n=(Mathf.PerlinNoise(x*.04f,y*.04f+id.GetHashCode()*.001f)-.5f)*.08f;
   pixels[y*w+x]=color*(1f+n);
  }
  tex.SetPixels(pixels);tex.Apply();return tex;
 }

 Texture2D GenerateGlowTexture(){
  var tex=new Texture2D(64,64,TextureFormat.RGBA32,false);
  generatedTextures.Add(tex);
  var pixels=new Color[64*64];
  Vector2 center=new(31.5f,31.5f);
  for(int y=0;y<64;y++)for(int x=0;x<64;x++){
   float alpha=Mathf.Clamp01(1f-Vector2.Distance(new Vector2(x,y),center)/31.5f);
   pixels[y*64+x]=new Color(1f,.9f,.45f,alpha*alpha);
  }
  tex.SetPixels(pixels);tex.Apply();return tex;
 }

 static void FillRect(Color[] pixels,int width,int height,int x0,int y0,int x1,int y1,Color color){
  for(int y=y0;y<y1;y++)for(int x=x0;x<x1;x++)if(x>=0&&y>=0&&x<width&&y<height){
   var existing=pixels[y*width+x];
   pixels[y*width+x]=existing.a<=0?color:Color.Lerp(existing,color,color.a);
  }
 }

 float SnapScrollFor(int index){
  index=Mathf.Clamp(index,0,facilitySlots.Count-1);
  return facilitySlots[index].worldX+CharacterAnchorX;
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

 float MinScroll(){return SnapScrollFor(0)-.85f;}
 float MaxScroll(){return SnapScrollFor(facilitySlots.Count-1)+.85f;}
}
}
