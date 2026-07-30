using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire {
public sealed partial class PackspireUiFoundation {
 float hubFacilityScrollY;
 int hubStreetGuideSelectedIndex;
 string hubStreetGuideCategory="all";
 bool hubStreetGuideOpen;

 VisualElement hubShell;
 ScrollView hubFacilityScroll;
 VisualElement hubCharacterHost;
 VisualElement hubBriefingHost;
 Label hubGoldLabel;
 Button hubStreetGuideEntry;
 VisualElement hubStreetGuideModal;
 VisualElement hubStreetGuideList;
 VisualElement hubStreetGuideDetail;
 ScrollView hubStreetGuideFacilityScroll;
 Texture2D hubHomeLogoArt;
 Texture2D hubHomeButtonArt;
 Texture2D hubHomeStreetButtonArt;
 Texture2D hubHomeChromeArt;
 Texture2D hubHomeObjectivePanelArt;

 enum HubHomeButtonState { Normal, Hover, Selected, Disabled }
 enum HubHomeChromePart { Divider, Currency, ObjectiveCrest }

 void BuildHub(){
  EnsureHubHomeArt();
  hubStreetGuideOpen=false;
  var meta=game.UiMeta;
  var facilities=HubFacilityCatalog.NavFacilities();

  hubShell=CloneView("UI/PackspireHubView","ps-hub-v4");
  if(hubShell==null){
   Debug.LogError("Hub view could not be created.");
   return;
  }
  screenRoot.Add(hubShell);

  var bgLayer=RequireViewElement<VisualElement>(hubShell,"hub-background");
  var bgArtHost=RequireViewElement<VisualElement>(hubShell,"hub-background-art");
  var hubBg=HubBackgroundArt();
  if(hubBg!=null){
   bgArtHost.style.backgroundImage=new StyleBackground(hubBg);
   PackspireUiFactory.ApplyBackgroundScaleMode(bgArtHost,ScaleMode.ScaleAndCrop);
  }
  bgLayer.Add(PackspireUiFactory.SystemIcon(PackspireUiFactory.PopIcon.Home,"ps-hub-nav-watermark"));
  foreach(var child in bgLayer.Children())child.pickingMode=PickingMode.Ignore;

  var hudLayer=RequireViewElement<VisualElement>(hubShell,"hub-columns");
  hudLayer.Add(BuildHubNavColumn(facilities));
  hudLayer.Add(BuildHubCharacterColumn(meta));
  hudLayer.Add(BuildHubBriefingColumn(meta));

  hubStreetGuideModal=BuildHubStreetGuideModal();
  hubStreetGuideModal.style.display=DisplayStyle.None;
  hubStreetGuideModal.pickingMode=PickingMode.Ignore;
  RequireViewElement<VisualElement>(hubShell,"hub-overlay-host").Add(hubStreetGuideModal);

  hubShell.RegisterCallback<KeyDownEvent>(OnHubKeyDown);
  hubShell.focusable=true;
  hubShell.schedule.Execute(()=>hubShell?.Focus()).StartingIn(1);

  RefreshHubGold(meta);
  RefreshHubBriefing(meta);
  hubShell.schedule.Execute(()=>{
   if(hubFacilityScroll!=null&&hubFacilityScrollY>0f)
    hubFacilityScroll.scrollOffset=new Vector2(0,hubFacilityScrollY);
  }).StartingIn(16);
 }

 VisualElement BuildHubNavColumn(HubFacilityDef[] facilities){
  var col=Container("ps-hub-col-nav");
  col.Add(HubHomeChrome(HubHomeChromePart.Divider,"ps-hub-nav-boundary"));
  var header=Container("ps-hub-nav-header");
  header.pickingMode=PickingMode.Ignore;
  if(hubHomeLogoArt!=null)
   header.Add(Image(hubHomeLogoArt,new Rect(0,0,1,1),"ps-hub-home-logo",ScaleMode.ScaleToFit));
  col.Add(header);

  var surface=Container("ps-hub-nav-surface");
  hubFacilityScroll=new ScrollView(ScrollViewMode.Vertical);
  hubFacilityScroll.AddToClassList("ps-hub-nav-scroll");
  hubFacilityScroll.name="hub-nav-scroll";
  hubFacilityScroll.verticalScrollerVisibility=ScrollerVisibility.Auto;
  hubFacilityScroll.horizontalScrollerVisibility=ScrollerVisibility.Hidden;
  StretchMgmtScrollContent(hubFacilityScroll,false);

  for(int i=0;i<facilities.Length;i++){
   var facility=facilities[i];
   var row=BuildHubNavFacilityRow(facility,i==0,()=>EnterHubFacility(facility));
   hubFacilityScroll.Add(row);
  }

  surface.Add(hubFacilityScroll);
  col.Add(surface);

  // Street guide stays at the bottom of the left column, separate from the facility list.
  var guideFooter=Container("ps-hub-guide-footer");
  hubStreetGuideEntry=BuildHubChromeStreetGuide(OpenHubStreetGuide) as Button;
  guideFooter.Add(hubStreetGuideEntry);
  col.Add(guideFooter);
  return col;
 }

 VisualElement BuildHubNavFacilityRow(HubFacilityDef facility,bool selected,System.Action onClick){
  var button=new Button(onClick){name=$"hub-nav-{facility.id}",userData=facility.id,tooltip=facility.description};
  button.AddToClassList("ps-hub-nav-row");
  if(selected)button.AddToClassList("ps-current");
  if(!facility.unlocked){
   button.AddToClassList("ps-locked");
   button.SetEnabled(false);
  }

  AddHubHomeButtonFrames(button);

  var seal=Container("ps-hub-nav-seal");
  seal.pickingMode=PickingMode.Ignore;
  seal.Add(PackspireUiFactory.SystemIcon(HubFacilityIcon(facility.id),"ps-hub-nav-icon"));
  button.Add(seal);

  var copy=Container("ps-hub-nav-copy");
  copy.pickingMode=PickingMode.Ignore;
  var name=new Label(facility.label){pickingMode=PickingMode.Ignore};
  name.AddToClassList("ps-hub-nav-name");
  var desc=new Label(facility.description){pickingMode=PickingMode.Ignore};
  desc.AddToClassList("ps-hub-nav-desc");
  copy.Add(name);
  copy.Add(desc);
  button.Add(copy);
  return button;
 }

 static PackspireUiFactory.PopIcon HubFacilityIcon(string id)=>id switch{
  "gate"=>PackspireUiFactory.PopIcon.Gate,
  "forge"=>PackspireUiFactory.PopIcon.Packing,
  "vault"=>PackspireUiFactory.PopIcon.Vault,
  "heirloom"=>PackspireUiFactory.PopIcon.Heirloom,
  "guild"=>PackspireUiFactory.PopIcon.Guild,
  "codex"=>PackspireUiFactory.PopIcon.Codex,
  "shop"=>PackspireUiFactory.PopIcon.Shop,
  "embassy"=>PackspireUiFactory.PopIcon.RoleFaction,
  "barracks"=>PackspireUiFactory.PopIcon.RoleCurrent,
  _=>PackspireUiFactory.PopIcon.Home
 };

 void EnterHubFacility(HubFacilityDef facility){
  if(!facility.unlocked)return;
  if(hubFacilityScroll!=null)hubFacilityScrollY=hubFacilityScroll.scrollOffset.y;
  game.UiNavigate(facility.screen);
 }

 VisualElement BuildHubCharacterColumn(MetaSave meta){
  var col=Container("ps-hub-col-character");
  var stage=Container("ps-hub-character-stage");
  stage.pickingMode=PickingMode.Ignore;

  hubCharacterHost=Container("ps-hub-character-host");
  hubCharacterHost.pickingMode=PickingMode.Ignore;
  var character=CharacterCatalog.Get(meta.selectedCharacterId);
  hubCharacterHost.Add(BuildHubCharacterPortrait(character));
  stage.Add(hubCharacterHost);

  var ground=Container("ps-hub-character-ground");
  ground.pickingMode=PickingMode.Ignore;
  stage.Add(ground);
  col.Add(stage);
  return col;
 }

 VisualElement BuildHubCharacterPortrait(CharacterDef character){
  var frame=Container("ps-hub-character-portrait-frame");
  frame.pickingMode=PickingMode.Ignore;
  frame.Add(CharacterPortraitFront(character,"ps-hub-character-portrait"));
  return frame;
 }

 VisualElement BuildHubBriefingColumn(MetaSave meta){
  var col=Container("ps-hub-col-briefing");

  var goldChip=Container("ps-hub-gold-chip");
  goldChip.pickingMode=PickingMode.Ignore;
  goldChip.Add(HubHomeChrome(HubHomeChromePart.Currency,"ps-hub-gold-frame"));
  goldChip.Add(PackspireUiFactory.SystemIcon(PackspireUiFactory.PopIcon.Shop,"ps-hub-gold-icon"));
  hubGoldLabel=new Label($"{meta.baseGold} G"){pickingMode=PickingMode.Ignore};
  hubGoldLabel.AddToClassList("ps-hub-gold-label");
  goldChip.Add(hubGoldLabel);
  col.Add(goldChip);

  var spacer=Container("ps-hub-briefing-spacer");
  spacer.pickingMode=PickingMode.Ignore;
  col.Add(spacer);

  var board=Container("ps-hub-briefing-board");
  if(hubHomeObjectivePanelArt!=null)
   board.Add(Image(hubHomeObjectivePanelArt,new Rect(0,0,1,1),"ps-hub-briefing-panel",ScaleMode.StretchToFill));
  var header=Container("ps-hub-briefing-header");
  header.pickingMode=PickingMode.Ignore;
  header.Add(HubHomeChrome(HubHomeChromePart.ObjectiveCrest,"ps-hub-objective-crest"));
  var title=new Label("本日の目標"){pickingMode=PickingMode.Ignore};
  title.AddToClassList("ps-hub-briefing-title");
  header.Add(title);
  board.Add(header);

  hubBriefingHost=Container("ps-hub-briefing-host");
  board.Add(hubBriefingHost);
  col.Add(board);
  return col;
 }

 void RefreshHubGold(MetaSave meta){
  if(hubGoldLabel!=null)hubGoldLabel.text=$"{meta.baseGold} G";
 }

 void RefreshHubBriefing(MetaSave meta){
  if(hubBriefingHost==null)return;
  hubBriefingHost.Clear();
  var items=BuildHubBriefingItems(meta).Take(3).ToList();
  if(items.Count==0){
   var empty=new Label("受注中の依頼はありません"){pickingMode=PickingMode.Ignore};
   empty.AddToClassList("ps-hub-briefing-empty");
   hubBriefingHost.Add(empty);
   var cta=new Button(()=>EnterHubFacility(HubFacilityCatalog.Find("gate"))){text="遠征準備を開く"};
   cta.AddToClassList("ps-hub-briefing-cta");
   cta.AddToClassList("ps-action-secondary");
   hubBriefingHost.Add(cta);
   return;
  }
  foreach(var item in items)
   hubBriefingHost.Add(BuildHubBriefingItem(item));
 }

 readonly struct HubBriefingItem {
  public readonly string kind;
  public readonly string title;
  public readonly string body;
  public readonly string facilityId;
  public HubBriefingItem(string kind,string title,string body,string facilityId=""){
   this.kind=kind;this.title=title;this.body=body;this.facilityId=facilityId;
  }
 }

 IEnumerable<HubBriefingItem> BuildHubBriefingItems(MetaSave meta){
  var active=LoadoutSystem.Active(meta);
  int loadoutCount=active?.slots?.Count??0;
  if(loadoutCount<=0)
   yield return new HubBriefingItem("recommend","遠征準備","荷造りが空です。装備を整えてから門へ向かいましょう。","forge");
  else
   yield return new HubBriefingItem("objective","次の目的",$"荷造り「{active.name}」で遠征準備へ進める。","gate");

  if(string.IsNullOrEmpty(meta.selectedHeirloomUid))
   yield return new HubBriefingItem("notice","家宝未設定","長期育成する装備を家宝画面で選べます。","heirloom");
  else {
   var heir=meta.stash?.FirstOrDefault(x=>x.uid==meta.selectedHeirloomUid);
   if(heir!=null&&GameCatalog.Items.ContainsKey(heir.templateId))
    yield return new HubBriefingItem("notice","家宝",GameCatalog.Items[heir.templateId].name+"を育成中。","heirloom");
  }

  int discovery=meta.discoveredItems.Count+meta.discoveredEnemies.Count;
  if(discovery>0)
   yield return new HubBriefingItem("progress","図鑑の記録",$"装備 {meta.discoveredItems.Count}　／　敵 {meta.discoveredEnemies.Count}","codex");
  else
   yield return new HubBriefingItem("progress","拠点の推奨","ステータスで役職の進捗を確認できます。","guild");
 }

 VisualElement BuildHubBriefingItem(HubBriefingItem item){
  var row=Container("ps-hub-briefing-item");
  row.pickingMode=PickingMode.Ignore;
  var mark=Container("ps-hub-briefing-mark ps-hub-briefing-mark-"+item.kind);
  mark.pickingMode=PickingMode.Ignore;
  mark.Add(PackspireUiFactory.SystemIcon(HubBriefingIcon(item.kind),"ps-hub-briefing-icon"));
  row.Add(mark);
  var copy=Container("ps-hub-briefing-copy");
  copy.pickingMode=PickingMode.Ignore;
  var title=new Label(item.title){pickingMode=PickingMode.Ignore};
  title.AddToClassList("ps-hub-briefing-item-title");
  var body=new Label(item.body){pickingMode=PickingMode.Ignore};
  body.AddToClassList("ps-hub-briefing-item-body");
  copy.Add(title);
  copy.Add(body);
   row.Add(copy);
   if(!string.IsNullOrEmpty(item.facilityId)){
    var link=new Button(()=>EnterHubFacility(HubFacilityCatalog.Find(item.facilityId))){text="➜"};
   link.AddToClassList("ps-hub-briefing-link");
   row.Add(link);
  }
  row.Add(PackspireUiFactory.SystemOrnament(PackspireUiFactory.PopOrnament.Section,"ps-hub-briefing-item-rule"));
  return row;
 }

 static PackspireUiFactory.PopIcon HubBriefingIcon(string kind)=>kind switch{
  "objective"=>PackspireUiFactory.PopIcon.Objective,
  "recommend"=>PackspireUiFactory.PopIcon.Expedition,
  "notice"=>PackspireUiFactory.PopIcon.Heirloom,
  _=>PackspireUiFactory.PopIcon.Codex
 };

 void EnsureHubHomeArt(){
  hubHomeLogoArt=PackspireResources.Load<Texture2D>("Art/UI/PopDark/HubV2/hub-logo-v1");
  hubHomeButtonArt=PackspireResources.Load<Texture2D>("Art/UI/PopDark/HubV2/hub-button-states-v1");
  hubHomeStreetButtonArt=PackspireResources.Load<Texture2D>("Art/UI/PopDark/HubV2/hub-street-button-states-v1");
  hubHomeChromeArt=PackspireResources.Load<Texture2D>("Art/UI/PopDark/HubV2/hub-chrome-v1");
  hubHomeObjectivePanelArt=PackspireResources.Load<Texture2D>("Art/UI/PopDark/HubV2/hub-objective-panel-v3");
 }

 void AddHubHomeButtonFrames(VisualElement host){
  if(host==null||hubHomeButtonArt==null)return;
  var stack=Container("ps-hub-home-frame-stack");
  stack.pickingMode=PickingMode.Ignore;
  foreach(HubHomeButtonState state in System.Enum.GetValues(typeof(HubHomeButtonState))){
   var stateClass=state.ToString().ToLowerInvariant();
   stack.Add(Image(hubHomeButtonArt,HubHomeButtonUv(state),$"ps-hub-home-frame ps-hub-home-frame-{stateClass}",ScaleMode.StretchToFill));
  }
  host.Insert(0,stack);
 }

 void AddHubStreetButtonFrames(VisualElement host){
  if(host==null||hubHomeStreetButtonArt==null)return;
  var stack=Container("ps-hub-home-frame-stack ps-hub-street-frame-stack");
  stack.pickingMode=PickingMode.Ignore;
  foreach(HubHomeButtonState state in System.Enum.GetValues(typeof(HubHomeButtonState))){
   var stateClass=state.ToString().ToLowerInvariant();
   stack.Add(Image(hubHomeStreetButtonArt,HubStreetButtonUv(state),$"ps-hub-home-frame ps-hub-street-frame ps-hub-home-frame-{stateClass}",ScaleMode.StretchToFill));
  }
  host.Insert(0,stack);
 }

 Image HubHomeChrome(HubHomeChromePart part,string className){
  if(hubHomeChromeArt==null)return new Image();
  return Image(hubHomeChromeArt,HubHomeChromeUv(part),className,ScaleMode.StretchToFill);
 }

 static Rect HubHomeButtonUv(HubHomeButtonState state)=>state switch{
  HubHomeButtonState.Hover=>PixelUv(635,227,610,231,1254),
  HubHomeButtonState.Selected=>PixelUv(9,776,612,233,1254),
  HubHomeButtonState.Disabled=>PixelUv(636,777,610,226,1254),
  _=>PixelUv(13,228,607,225,1254)
 };

 static Rect HubStreetButtonUv(HubHomeButtonState state)=>state switch{
  HubHomeButtonState.Hover=>PixelUv(796,112,680,350,1536,1024),
  HubHomeButtonState.Selected=>PixelUv(76,554,674,350,1536,1024),
  HubHomeButtonState.Disabled=>PixelUv(802,564,660,330,1536,1024),
  _=>PixelUv(90,120,660,330,1536,1024)
 };

 static Rect HubHomeChromeUv(HubHomeChromePart part)=>part switch{
  HubHomeChromePart.Currency=>PixelUv(629,244,533,169,1254),
  HubHomeChromePart.ObjectiveCrest=>PixelUv(39,735,596,344,1254),
  _=>PixelUv(258,28,121,605,1254)
 };

 static Rect PixelUv(int x,int y,int width,int height,int textureSize)=>
  new Rect((float)x/textureSize,(float)(textureSize-y-height)/textureSize,(float)width/textureSize,(float)height/textureSize);

 static Rect PixelUv(int x,int y,int width,int height,int textureWidth,int textureHeight)=>
  new Rect((float)x/textureWidth,(float)(textureHeight-y-height)/textureHeight,(float)width/textureWidth,(float)height/textureHeight);

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

 void OpenHubStreetGuide(){
  hubStreetGuideOpen=true;
  if(hubStreetGuideSelectedIndex<0)hubStreetGuideSelectedIndex=0;
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
  var enter=BuildHubChromePrimary("ここへ向かう",()=>{
   CloseHubStreetGuide();
   EnterHubFacility(facility);
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
  if(evt.keyCode==KeyCode.M){
   OpenHubStreetGuide();
   evt.StopPropagation();
  }else if(evt.keyCode==KeyCode.Escape)evt.StopPropagation();
 }
}
}
