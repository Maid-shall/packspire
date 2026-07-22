using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire {
public sealed partial class PackspireUiFoundation {
 const int HubReelSlotHeight=68;
 const int HubFacilityVisibleSlots=5;

 int facilityReelIndex;
 int hubReelIndex;
 bool hubStreetGuideOpen;
 string hubStreetGuideCategory="all";
 int hubStreetGuideSelectedIndex;
 float hubReelScrollY;

 VisualElement hubShell;
 ScrollView hubReelScroll;
 VisualElement hubCenterBg;
 VisualElement hubCenterCharacterHost;
 VisualElement hubCenterInfo;
 Label hubCenterFacilityName;
 Label hubCenterFacilityDesc;
 Label hubCenterHint;
 Label hubCenterMeta;
 Button hubStreetGuideEntry;
 VisualElement hubStreetGuideModal;
 VisualElement hubStreetGuideList;
 VisualElement hubStreetGuideDetail;
 ScrollView hubStreetGuideFacilityScroll;

 void BuildHub(){
  hubStreetGuideOpen=false;
  var meta=game.UiMeta;
  var facilities=HubFacilityCatalog.ReelFacilities();
  if(hubReelIndex<0||hubReelIndex>=facilities.Length)hubReelIndex=0;

  hubShell=Container("ps-hub-v3");
  screenRoot.Add(hubShell);

  var bgLayer=Container("ps-hub-layer-bg");
  bgLayer.pickingMode=PickingMode.Ignore;
  var hubBg=HubBackgroundArt();
  if(hubBg!=null)bgLayer.Add(Image(hubBg,new Rect(0,0,1,1),"ps-hub-v3-bg",ScaleMode.ScaleAndCrop));
  bgLayer.Add(Container("ps-hub-v3-shade"));
  foreach(var child in bgLayer.Children())child.pickingMode=PickingMode.Ignore;
  hubShell.Add(bgLayer);

  var hudLayer=Container("ps-hub-layer-hud ps-hub-v3-columns");
  var leftCol=BuildHubLeftColumn(facilities);
  var stageCol=BuildHubStageColumn(meta);
  hudLayer.Add(leftCol);
  hudLayer.Add(stageCol);
  hubShell.Add(hudLayer);

  hubStreetGuideModal=BuildHubStreetGuideModal();
  hubStreetGuideModal.style.display=DisplayStyle.None;
  hubStreetGuideModal.pickingMode=PickingMode.Ignore;
  hubShell.Add(hubStreetGuideModal);

  hubShell.RegisterCallback<KeyDownEvent>(OnHubKeyDown);
  hubShell.focusable=true;
  hubShell.schedule.Execute(()=>hubShell?.Focus()).StartingIn(1);

  UpdateHubReelVisuals();
  UpdateHubCenterPanel();
 }

 VisualElement BuildHubLeftColumn(HubFacilityDef[] facilities){
  var col=Container("ps-hub-col-left");

  var reelStack=Container("ps-hub-reel-stack");
  var reelWrap=Container("ps-hub-reel-wrap");
  var fadeTop=Container("ps-hub-reel-fade ps-hub-reel-fade-top");
  fadeTop.pickingMode=PickingMode.Ignore;
  var fadeBottom=Container("ps-hub-reel-fade ps-hub-reel-fade-inner-bottom");
  fadeBottom.pickingMode=PickingMode.Ignore;

  hubReelScroll=new ScrollView(ScrollViewMode.Vertical);
  hubReelScroll.AddToClassList("ps-hub-reel-scroll");
  hubReelScroll.name="hub-reel-scroll";
  hubReelScroll.verticalScrollerVisibility=ScrollerVisibility.Hidden;
  hubReelScroll.RegisterCallback<WheelEvent>(evt=>{evt.StopPropagation();hubShell?.schedule.Execute(UpdateHubReelFade);});
  hubReelScroll.RegisterCallback<GeometryChangedEvent>(_=>UpdateHubReelFade());

  for(int i=0;i<facilities.Length;i++){
   int index=i;
   var facility=facilities[i];
   var row=BuildHubChromeFacility(facility,index,index==hubReelIndex,()=>OnHubReelRowClick(index));
   row.AddToClassList("ps-hub-reel-slot");
   hubReelScroll.Add(row);
  }

  reelWrap.Add(fadeTop);
  reelWrap.Add(hubReelScroll);
  reelWrap.Add(fadeBottom);
  reelStack.Add(reelWrap);

  var enter=BuildHubChromePrimary("施設へ入る",ConfirmHubReelSelection);
  enter.AddToClassList("ps-hub-reel-enter");
  reelStack.Add(enter);
  col.Add(reelStack);

  var guideFooter=Container("ps-hub-guide-footer");
  hubStreetGuideEntry=BuildHubChromeStreetGuide(OpenHubStreetGuide) as Button;
  guideFooter.Add(hubStreetGuideEntry);
  col.Add(guideFooter);

  hubShell?.schedule.Execute(()=>{
   ScrollHubReelToSelection();
   if(hubReelScroll!=null&&hubReelScrollY>0f)
    hubReelScroll.scrollOffset=new Vector2(0,hubReelScrollY);
   UpdateHubReelFade();
  }).StartingIn(32);
  return col;
 }

 void OnHubReelRowClick(int index){
  if(index==hubReelIndex)ConfirmHubReelSelection();
  else SelectHubReel(index);
 }

 VisualElement BuildHubStageColumn(MetaSave meta){
  var col=Container("ps-hub-col-stage ps-hub-layer-world");
  hubCenterBg=Container("ps-hub-center-bg");
  hubCenterBg.pickingMode=PickingMode.Ignore;
  col.Add(hubCenterBg);

  var metaChip=Container("ps-hub-stage-meta-chip");
  metaChip.pickingMode=PickingMode.Ignore;
  var character=CharacterCatalog.Get(meta.selectedCharacterId);
  var goldLabel=new Label($"{meta.baseGold} G"){pickingMode=PickingMode.Ignore};
  goldLabel.AddToClassList("ps-hub-stage-gold");
  metaChip.Add(goldLabel);
  col.Add(metaChip);

  hubCenterCharacterHost=Container("ps-hub-center-character");
  hubCenterCharacterHost.pickingMode=PickingMode.Ignore;
  hubCenterCharacterHost.Add(BuildHubCenterPortrait(character));
  col.Add(hubCenterCharacterHost);

  hubCenterMeta=PackspireUiFactory.Body("");
  hubCenterMeta.AddToClassList("ps-hub-center-meta");
  hubCenterMeta.pickingMode=PickingMode.Ignore;
  col.Add(hubCenterMeta);

  hubCenterInfo=Container("ps-hub-center-info");
  hubCenterFacilityName=new Label(""){pickingMode=PickingMode.Ignore};
  hubCenterFacilityName.AddToClassList("ps-hub-center-facility-name");
  hubCenterFacilityDesc=new Label(""){pickingMode=PickingMode.Ignore};
  hubCenterFacilityDesc.AddToClassList("ps-hub-center-facility-desc");
  hubCenterHint=new Label(""){pickingMode=PickingMode.Ignore};
  hubCenterHint.AddToClassList("ps-hub-center-hint");
  hubCenterInfo.Add(hubCenterFacilityName);
  hubCenterInfo.Add(hubCenterFacilityDesc);
  hubCenterInfo.Add(hubCenterHint);
  col.Add(hubCenterInfo);

  RefreshHubCenterMeta(meta,character);
  return col;
 }

 VisualElement BuildHubCenterPortrait(CharacterDef character){
  var frame=Container("ps-hub-center-portrait-frame");
  var portrait=CharacterPortraitFront(character,"ps-hub-center-portrait");
  frame.Add(portrait);
  return frame;
 }

 void RefreshHubCenterMeta(MetaSave meta,CharacterDef character){
  if(hubCenterMeta==null)return;
  var roleName=GameCatalog.Roles.ContainsKey(meta.currentRole)?GameCatalog.Roles[meta.currentRole].name:meta.currentRole;
  hubCenterMeta.text=$"{character.name}　{character.title}　|　{roleName}　|　Week {meta.runs+1}";
 }

 VisualElement BuildHubStreetGuideModal(){
  var modal=Container("ps-hub-layer-modal");
  var backdrop=new Button(CloseHubStreetGuide);
  backdrop.AddToClassList("ps-hub-modal-backdrop");
  modal.Add(backdrop);

  var panel=Container("ps-hub-street-guide");
  var header=Container("ps-hub-street-guide-header");
  header.Add(PackspireUiFactory.Title("街案内"));
  var close=BuildHubChromeSecondary("閉じる",CloseHubStreetGuide);
  close.AddToClassList("ps-hub-street-guide-close");
  header.Add(close);
  panel.Add(header);

  var body=Container("ps-hub-street-guide-body");
  var categories=Container("ps-hub-street-guide-categories");
  categories.name="hub-street-categories";
  AddStreetGuideCategoryButton(categories,"all","すべて");
  AddStreetGuideCategoryButton(categories,"scene","行動");
  AddStreetGuideCategoryButton(categories,"workbench","工房");
  AddStreetGuideCategoryButton(categories,"archive","記録");
  body.Add(categories);

  hubStreetGuideFacilityScroll=new ScrollView(ScrollViewMode.Vertical);
  hubStreetGuideFacilityScroll.AddToClassList("ps-hub-street-guide-list");
  hubStreetGuideList=hubStreetGuideFacilityScroll.contentContainer;
  body.Add(hubStreetGuideFacilityScroll);

  hubStreetGuideDetail=Container("ps-hub-street-guide-detail");
  body.Add(hubStreetGuideDetail);

  panel.Add(body);
  modal.Add(panel);
  return modal;
 }

 void AddStreetGuideCategoryButton(VisualElement host,string id,string label){
  var button=PackspireUiFactory.Button(label,()=>{
   hubStreetGuideCategory=id;
   hubStreetGuideSelectedIndex=0;
   RefreshHubStreetGuideCategories();
   PopulateHubStreetGuideList();
   RefreshHubStreetGuideDetail();
  });
  button.name=$"hub-cat-{id}";
  button.AddToClassList("ps-hub-street-guide-cat");
  if(hubStreetGuideCategory==id)button.AddToClassList("ps-selected");
  host.Add(button);
 }

 void SelectHubReel(int index){
  if(index==hubReelIndex)return;
  SaveHubReelScroll();
  hubReelIndex=index;
  UpdateHubReelVisuals();
  UpdateHubCenterPanel();
  ScrollHubReelToSelection();
 }

 void SaveHubReelScroll(){if(hubReelScroll!=null)hubReelScrollY=hubReelScroll.scrollOffset.y;}

 void UpdateHubReelVisuals(){
  if(hubReelScroll==null)return;
  int i=0;
  foreach(var child in hubReelScroll.Children()){
   if(child is not Button row)continue;
   bool selected=i==hubReelIndex;
   int distance=Mathf.Abs(i-hubReelIndex);
   row.EnableInClassList("ps-selected",selected);
   row.EnableInClassList("ps-hub-reel-near",!selected&&distance==1);
   row.EnableInClassList("ps-hub-reel-far",distance>=2);
   i++;
  }
 }

 void ScrollHubReelToSelection(){
  if(hubReelScroll==null)return;
  if(hubReelIndex<0||hubReelIndex>=hubReelScroll.childCount)return;
  var target=hubReelScroll.ElementAt(hubReelIndex);
  hubReelScroll.ScrollTo(target);
  UpdateHubReelFade();
 }

 void UpdateHubReelFade(){
  if(hubShell==null)return;
  var wrap=hubShell.Q(className:"ps-hub-reel-wrap");
  if(wrap==null)return;
  float maxScroll=Mathf.Max(0,hubReelScroll.contentContainer.layout.height-hubReelScroll.layout.height);
  float y=hubReelScroll.scrollOffset.y;
  wrap.EnableInClassList("ps-hub-reel-has-above",y>4f);
  wrap.EnableInClassList("ps-hub-reel-has-below",y<maxScroll-4f);
  wrap.EnableInClassList("ps-hub-reel-has-inner-bottom",y<maxScroll-4f);
 }

 void UpdateHubCenterPanel(){
  var facility=HubFacilityCatalog.GetReel(hubReelIndex);
  if(hubCenterBg!=null){
   string[] themes={"default","strata","forge","vault","codex","guild","embassy","roster"};
   foreach(var theme in themes)
    hubCenterBg.EnableInClassList("ps-hub-theme-"+theme,theme==facility.themeKey);
  }
  if(hubCenterFacilityName!=null)hubCenterFacilityName.text=facility.label;
  if(hubCenterFacilityDesc!=null)hubCenterFacilityDesc.text=string.IsNullOrEmpty(facility.description)?facility.eyebrow:facility.description;
  if(hubCenterHint!=null)hubCenterHint.text=facility.unlocked?"Enter / 再クリックで入室":"未解放";
 }

 void ConfirmHubReelSelection(){
  var facility=HubFacilityCatalog.GetReel(hubReelIndex);
  if(!facility.unlocked)return;
  SaveHubReelScroll();
  game.UiNavigate(facility.screen);
 }

 void OpenHubStreetGuide(){
  hubStreetGuideOpen=true;
  if(hubStreetGuideSelectedIndex<0||hubStreetGuideSelectedIndex>=HubFacilityCatalog.All.Length)
   hubStreetGuideSelectedIndex=hubReelIndex;
  hubStreetGuideModal.style.display=DisplayStyle.Flex;
  hubStreetGuideModal.pickingMode=PickingMode.Position;
  RefreshHubStreetGuideCategories();
  PopulateHubStreetGuideList();
  RefreshHubStreetGuideDetail();
 }

 void CloseHubStreetGuide(){
  hubStreetGuideOpen=false;
  if(hubStreetGuideModal!=null){
   hubStreetGuideModal.style.display=DisplayStyle.None;
   hubStreetGuideModal.pickingMode=PickingMode.Ignore;
  }
  hubShell?.Focus();
  UpdateHubReelFade();
 }

 void RefreshHubStreetGuideCategories(){
  if(hubStreetGuideModal==null)return;
  var host=hubStreetGuideModal.Q("hub-street-categories");
  if(host==null)return;
  foreach(var child in host.Children()){
   if(child is not Button button)continue;
   bool selected=button.name==$"hub-cat-{hubStreetGuideCategory}";
   button.EnableInClassList("ps-selected",selected);
  }
 }

 void PopulateHubStreetGuideList(){
  if(hubStreetGuideFacilityScroll==null)return;
  hubStreetGuideFacilityScroll.Clear();
  var facilities=FilteredStreetGuideFacilities().ToArray();
  if(facilities.Length==0){
   hubStreetGuideSelectedIndex=0;
   return;
  }
  if(hubStreetGuideSelectedIndex>=facilities.Length)hubStreetGuideSelectedIndex=0;
  for(int i=0;i<facilities.Length;i++){
   int index=i;
   var facility=facilities[i];
   var row=BuildHubChromeMapPin(facility,index==hubStreetGuideSelectedIndex,()=>{
    hubStreetGuideSelectedIndex=index;
    PopulateHubStreetGuideList();
    RefreshHubStreetGuideDetail();
   });
   hubStreetGuideFacilityScroll.Add(row);
  }
 }

 System.Collections.Generic.IEnumerable<HubFacilityDef> FilteredStreetGuideFacilities(){
  foreach(var facility in HubFacilityCatalog.UnlockedFacilities()){
   if(hubStreetGuideCategory=="all"){yield return facility;continue;}
   if(hubStreetGuideCategory=="scene"&&facility.kind==HubFacilityKind.Scene)yield return facility;
   else if(hubStreetGuideCategory=="workbench"&&facility.kind==HubFacilityKind.Workbench)yield return facility;
   else if(hubStreetGuideCategory=="archive"&&facility.kind==HubFacilityKind.Archive)yield return facility;
  }
 }

 void RefreshHubStreetGuideDetail(){
  if(hubStreetGuideDetail==null)return;
  hubStreetGuideDetail.Clear();
  var facilities=FilteredStreetGuideFacilities().ToArray();
  if(facilities.Length==0){
   hubStreetGuideDetail.Add(PackspireUiFactory.EmptyState("施設なし","この区分に表示できる施設がありません。"));
   return;
  }
  var facility=facilities[Mathf.Clamp(hubStreetGuideSelectedIndex,0,facilities.Length-1)];
  hubStreetGuideDetail.Add(PackspireUiFactory.Title(facility.label));
  hubStreetGuideDetail.Add(PackspireUiFactory.Body(facility.CategoryLabel+"　"+facility.eyebrow));
  hubStreetGuideDetail.Add(PackspireUiFactory.Body(facility.description));
  hubStreetGuideDetail.Add(PackspireUiFactory.Body($"地図座標　{facility.mapX:0.00}, {facility.mapY:0.00}"));
  var enter=BuildHubChromePrimary("ここへ向かう",()=>{
   int reelIdx=HubFacilityCatalog.IndexInReel(facility.id);
   if(reelIdx>=0)hubReelIndex=reelIdx;
   CloseHubStreetGuide();
   if(reelIdx>=0){
    UpdateHubReelVisuals();
    UpdateHubCenterPanel();
    ScrollHubReelToSelection();
   }
   game.UiNavigate(facility.screen);
  });
  enter.AddToClassList("ps-hub-street-guide-enter");
  hubStreetGuideDetail.Add(enter);
 }

 void OnHubKeyDown(KeyDownEvent evt){
  if(renderedScreen!=ScreenId.Hub)return;
  if(hubStreetGuideOpen){
   if(evt.keyCode==KeyCode.Escape){CloseHubStreetGuide();evt.StopPropagation();}
   return;
  }
  int n=HubFacilityCatalog.ReelFacilities().Length;
  if(n<=0)return;
  if(evt.keyCode==KeyCode.UpArrow||evt.keyCode==KeyCode.W){
   SelectHubReel((hubReelIndex-1+n)%n);
   evt.StopPropagation();
  }else if(evt.keyCode==KeyCode.DownArrow||evt.keyCode==KeyCode.S){
   SelectHubReel((hubReelIndex+1)%n);
   evt.StopPropagation();
  }else if(evt.keyCode==KeyCode.Return||evt.keyCode==KeyCode.KeypadEnter){
   ConfirmHubReelSelection();
   evt.StopPropagation();
  }else if(evt.keyCode==KeyCode.M){
   OpenHubStreetGuide();
   evt.StopPropagation();
  }
 }

 VisualElement ArchiveShell(string eyebrow,string title,ScreenId current,out VisualElement gridHost,out VisualElement detailHost){
  var shell=Container("ps-archive-screen");
  var bg=HubBackgroundArt();
  if(bg==null)bg=CourtyardArt();
  if(bg!=null)shell.Add(Image(bg,new Rect(0,0,1,1),"ps-archive-bg",ScaleMode.ScaleAndCrop));
  var shade=Container("ps-archive-shade");
  shade.pickingMode=PickingMode.Ignore;
  shell.Add(shade);

  var top=Container("ps-archive-top");
  top.Add(ChromeBrand(eyebrow,title));
  var back=new Button(()=>game.UiNavigate(ScreenId.Hub)){text="拠点へ戻る"};
  back.AddToClassList("ps-archive-back");
  back.AddToClassList("ps-chrome-action");
  top.Add(back);
  shell.Add(top);

  var body=Container("ps-archive-body");
  gridHost=Container("ps-archive-grid-host");
  detailHost=Container("ps-archive-detail-host");
  body.Add(gridHost);
  body.Add(detailHost);
  shell.Add(body);

  return shell;
 }

 VisualElement BuildFacilityReel(ScreenId current){
  var facilities=HubFacilityCatalog.All;
  if(facilities.Length==0)return Container("ps-facility-reel");
  facilityReelIndex=HubFacilityCatalog.IndexOfScreen(current);

  var root=Container("ps-facility-reel");
  var prev=new Button(()=>RotateFacilityReel(root,-1)){text="‹"};
  prev.AddToClassList("ps-facility-reel-arrow");
  root.Add(prev);

  var viewport=Container("ps-facility-reel-viewport");
  viewport.name="facility-reel-viewport";
  root.Add(viewport);

  var next=new Button(()=>RotateFacilityReel(root,1)){text="›"};
  next.AddToClassList("ps-facility-reel-arrow");
  root.Add(next);

  viewport.RegisterCallback<WheelEvent>(evt=>{
   float dy=evt.delta.y;
   if(Mathf.Abs(dy)<0.01f)return;
   RotateFacilityReel(root,dy>0?1:-1);
   evt.StopPropagation();
  });

  RefreshFacilityReel(root);
  return root;
 }

 void RotateFacilityReel(VisualElement root,int delta){
  int n=HubFacilityCatalog.All.Length;
  if(n<=0)return;
  facilityReelIndex=((facilityReelIndex+delta)%n+n)%n;
  RefreshFacilityReel(root);
 }

 void RefreshFacilityReel(VisualElement root){
  var viewport=root.Q<VisualElement>("facility-reel-viewport");
  if(viewport==null)return;
  viewport.Clear();
  var facilities=HubFacilityCatalog.All;
  int n=facilities.Length;
  int slots=Mathf.Min(HubFacilityVisibleSlots,n);
  int half=slots/2;
  for(int slot=0;slot<slots;slot++){
   int index=((facilityReelIndex-half+slot)%n+n)%n;
   var entry=facilities[index];
   int offset=slot-half;
   bool center=offset==0;
   var card=new Button(()=>{
    if(center)game.UiNavigate(entry.screen);
    else{
     facilityReelIndex=index;
     RefreshFacilityReel(root);
    }
   });
   card.AddToClassList("ps-facility-reel-card");
   if(center)card.AddToClassList("ps-facility-reel-card-center");
   if(entry.screen==ScreenId.Expedition)card.AddToClassList("ps-facility-reel-card-primary");
   if(Mathf.Abs(offset)>=2)card.AddToClassList("ps-facility-reel-card-far");
   var eye=new Label(entry.eyebrow){pickingMode=PickingMode.Ignore};
   eye.AddToClassList("ps-facility-reel-eye");
   card.Add(eye);
   var name=new Label(entry.label){pickingMode=PickingMode.Ignore};
   name.AddToClassList("ps-facility-reel-name");
   card.Add(name);
   viewport.Add(card);
  }
 }
}
}
