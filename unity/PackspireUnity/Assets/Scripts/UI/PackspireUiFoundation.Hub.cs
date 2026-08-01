using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire {
public sealed partial class PackspireUiFoundation {
 static readonly string[] PrimaryHubFacilityIds={"forge","vault","heirloom","guild","codex"};

 int hubReelIndex;
 int hubStreetGuideSelectedIndex;
 string hubStreetGuideCategory="all";
 bool hubStreetGuideOpen;

 VisualElement hubShell;
 ScrollView hubFacilityScroll;
 Button[] hubNavButtons;
 VisualElement hubCharacterHost;
 VisualElement hubCharacterStudyFront;
 VisualElement hubCharacterStudyBack;
 VisualElement hubBriefingHost;
 VisualElement hubEquipmentRoleArt;
 VisualElement hubEquipmentHeirloomArt;
 Label hubGoldLabel;
 Label hubCharacterNameLabel;
 Label hubDestinationNameLabel;
 Label hubDestinationCodeLabel;
 Label hubMissionTitleLabel;
 Label hubMissionDestinationLabel;
 Label hubMissionLoadoutLabel;
 Label hubMissionCargoLabel;
 Label hubVaultIndexCountLabel;
 Label hubCodexIndexCountLabel;
 Button hubStreetGuideEntry;
 VisualElement hubStreetGuideModal;
 VisualElement hubStreetGuideDetail;
 ScrollView hubStreetGuideFacilityScroll;

 void BuildHub(){
  hubStreetGuideOpen=false;
  hubReelIndex=0;
  var meta=game.UiMeta;

  hubShell=CloneView("UI/PackspireHubView","ps-hub-misprint");
  if(hubShell==null){
   Debug.LogError("Hub view could not be created.");
   return;
  }
  screenRoot.Add(hubShell);

  BindHubNavigation();
  BindHubCharacter(meta);
  BindHubDossier(meta);
  BindHubMission(meta);
  BindHubStreetGuide();

  hubShell.RegisterCallback<KeyDownEvent>(OnHubKeyDown);
  hubShell.focusable=true;
  hubShell.schedule.Execute(()=>hubShell?.Focus()).StartingIn(1);
 }

 void BindHubNavigation(){
  hubFacilityScroll=RequireViewElement<ScrollView>(hubShell,"hub-nav-scroll");
  hubNavButtons=new Button[PrimaryHubFacilityIds.Length];
  for(int index=0;index<PrimaryHubFacilityIds.Length;index++){
   int facilityIndex=index;
   var facility=HubFacilityCatalog.Find(PrimaryHubFacilityIds[index]);
   var button=RequireViewElement<Button>(hubShell,$"hub-nav-{facility.id}");
   hubNavButtons[index]=button;
   button.tooltip=facility.description;
   button.EnableInClassList("is-locked",!facility.unlocked);
   button.SetEnabled(facility.unlocked);
   button.RegisterCallback<PointerEnterEvent>(_=>SelectHubReel(facilityIndex));
   button.clicked+=()=>{
    hubReelIndex=facilityIndex;
    EnterHubFacility(facility);
   };
  }
  hubFacilityScroll.RegisterCallback<WheelEvent>(OnHubReelWheel,TrickleDown.TrickleDown);
  RefreshHubReel();

  hubStreetGuideEntry=RequireViewElement<Button>(hubShell,"hub-street-entry");
  hubStreetGuideEntry.clicked+=OpenHubStreetGuide;
 }

 void OnHubReelWheel(WheelEvent evt){
  if(hubStreetGuideOpen||Mathf.Approximately(evt.delta.y,0f))return;
  MoveHubReel(evt.delta.y>0f?1:-1);
  evt.StopPropagation();
 }

 void MoveHubReel(int direction){
  if(hubNavButtons==null||hubNavButtons.Length==0)return;
  int next=hubReelIndex;
  for(int attempt=0;attempt<hubNavButtons.Length;attempt++){
   next=(next+direction+hubNavButtons.Length)%hubNavButtons.Length;
   if(HubFacilityCatalog.Find(PrimaryHubFacilityIds[next]).unlocked){SelectHubReel(next);return;}
  }
 }

 void SelectHubReel(int index){
  if(hubNavButtons==null||index<0||index>=hubNavButtons.Length)return;
  if(!HubFacilityCatalog.Find(PrimaryHubFacilityIds[index]).unlocked)return;
  hubReelIndex=index;
  RefreshHubReel();
 }

 void RefreshHubReel(){
  if(hubNavButtons==null||hubNavButtons.Length==0)return;
  hubReelIndex=Mathf.Clamp(hubReelIndex,0,hubNavButtons.Length-1);
 for(int index=0;index<hubNavButtons.Length;index++)
   hubNavButtons[index].EnableInClassList("is-reel-current",index==hubReelIndex);
  hubFacilityScroll.scrollOffset=Vector2.zero;
 }

 void EnterHubFacility(HubFacilityDef facility){
  if(!facility.unlocked)return;
  game.UiNavigate(facility.screen);
 }

 void BindHubCharacter(MetaSave meta){
  hubCharacterHost=RequireViewElement<VisualElement>(hubShell,"hub-character-host");
  hubCharacterStudyFront=RequireViewElement<VisualElement>(hubShell,"hub-character-study-front");
  hubCharacterStudyBack=RequireViewElement<VisualElement>(hubShell,"hub-character-study-back");
  hubCharacterNameLabel=RequireViewElement<Label>(hubShell,"hub-character-name");
  hubDestinationNameLabel=RequireViewElement<Label>(hubShell,"hub-destination-name");
  hubDestinationCodeLabel=RequireViewElement<Label>(hubShell,"hub-destination-code");

  var character=CharacterCatalog.Get(meta.selectedCharacterId);
  hubCharacterHost.Clear();
  hubCharacterHost.Add(CharacterPortraitHub(character,"ps-hub-misprint__character-portrait"));
  PopulateHubCharacterStudy(hubCharacterStudyFront,character,false);
  PopulateHubCharacterStudy(hubCharacterStudyBack,character,true);
  hubCharacterNameLabel.text="配送人 "+character.name;

  var destination=GameCatalog.Dungeons.FirstOrDefault();
  if(destination!=null){
   hubDestinationNameLabel.text=destination.name;
   hubDestinationCodeLabel.text=destination.id.Replace('_',' ').ToUpperInvariant();
  }
 }

 void PopulateHubCharacterStudy(VisualElement host,CharacterDef character,bool back){
  host.Clear();
  var suffix=back?"back":"front";
  var texture=PackspireResources.Load<Texture2D>($"Art/Portraits/ObsidianMisprint/hero-{character.id}-{suffix}-study-v1");
  host.Add(texture!=null
   ?BuildPortraitDisplay(texture,"ps-hub-misprint__study-image")
   :back
    ?CharacterPortraitHub(character,"ps-hub-misprint__study-image")
    :CharacterPortraitFront(character,"ps-hub-misprint__study-image"));
 }

 void BindHubDossier(MetaSave meta){
  hubGoldLabel=RequireViewElement<Label>(hubShell,"hub-gold");
  hubBriefingHost=RequireViewElement<VisualElement>(hubShell,"hub-briefing-host");
  hubEquipmentRoleArt=RequireViewElement<VisualElement>(hubShell,"hub-equipment-role-art");
  hubEquipmentHeirloomArt=RequireViewElement<VisualElement>(hubShell,"hub-equipment-heirloom-art");
  hubVaultIndexCountLabel=RequireViewElement<Label>(hubShell,"hub-vault-index-count");
  hubCodexIndexCountLabel=RequireViewElement<Label>(hubShell,"hub-codex-index-count");

  RequireViewElement<Button>(hubShell,"hub-vault-index").clicked+=()=>EnterHubFacility(HubFacilityCatalog.Find("vault"));
  RequireViewElement<Button>(hubShell,"hub-codex-index").clicked+=()=>EnterHubFacility(HubFacilityCatalog.Find("codex"));
  RefreshHubGold(meta);
  RefreshHubEquipmentRecord(meta);
  RefreshHubBriefing(meta);
  hubVaultIndexCountLabel.text=meta.stash.Count.ToString("D2");
  hubCodexIndexCountLabel.text=(meta.discoveredItems.Count+meta.discoveredEnemies.Count).ToString("D2");
 }

 void RefreshHubGold(MetaSave meta){
  if(hubGoldLabel!=null)hubGoldLabel.text=$"{meta.baseGold:N0} G";
 }

 void RefreshHubEquipmentRecord(MetaSave meta){
  var active=LoadoutSystem.Active(meta);
  var formula=StorageFormulaSystem.Resolve(active);
  SetHubEquipmentText("loadout",formula.core.name,$"{active?.name??"未設定"} / 配置 {active?.slots?.Count??0}件");

  var role=GameCatalog.Roles.TryGetValue(meta.currentRole??"",out var currentRole)?currentRole:null;
  SetHubEquipmentText("role",role?.name??"未設定",role?.kind??"役職を選択");
  hubEquipmentRoleArt.Clear();
  var roleCostumeArt=PackspireResources.Load<Texture2D>("Art/role-costume-sheet");
  if(role!=null&&roleCostumeArt!=null)
   hubEquipmentRoleArt.Add(Atlas(roleCostumeArt,HubRoleCostumeUv(role.id),"ps-hub-misprint__equipment-image"));

  var heir=meta.stash?.FirstOrDefault(value=>value.uid==meta.selectedHeirloomUid);
  var heirDefinition=heir!=null&&GameCatalog.Items.TryGetValue(heir.templateId,out var definition)?definition:null;
  SetHubEquipmentText("heirloom",heirDefinition?.name??"未選択",heirDefinition?.description??"家宝を選択");
  hubEquipmentHeirloomArt.Clear();
  hubEquipmentHeirloomArt.EnableInClassList("has-art",heirDefinition!=null);
  if(heirDefinition!=null)hubEquipmentHeirloomArt.Add(VaultItemArt(heirDefinition.id,"ps-hub-misprint__equipment-image"));
 }

 void SetHubEquipmentText(string id,string name,string detail){
  RequireViewElement<Label>(hubShell,$"hub-equipment-{id}-name").text=name;
  RequireViewElement<Label>(hubShell,$"hub-equipment-{id}-detail").text=detail;
 }

 Rect HubRoleCostumeUv(string roleId){
  var roleUv=RoleUv(roleId);
  return new Rect(roleUv.x,(1f+roleUv.y*3f)/4f,.25f,.25f);
 }

 void RefreshHubBriefing(MetaSave meta){
  if(hubBriefingHost==null)return;
  hubBriefingHost.Clear();
  var items=BuildHubBriefingItems(meta).Take(3).ToList();
  for(int index=0;index<items.Count;index++)
   hubBriefingHost.Add(BuildHubBriefingItem(items[index],index));
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
   yield return new HubBriefingItem("recommend","遠征準備","荷造りが空です。装備を整えてください。","forge");
  else
   yield return new HubBriefingItem("objective","次の目的",$"荷造り「{active.name}」で遠征準備へ進む。","gate");

  if(string.IsNullOrEmpty(meta.selectedHeirloomUid))
   yield return new HubBriefingItem("notice","家宝未設定","長期育成する装備を選べます。","heirloom");
  else {
   var heir=meta.stash?.FirstOrDefault(value=>value.uid==meta.selectedHeirloomUid);
   if(heir!=null&&GameCatalog.Items.TryGetValue(heir.templateId,out var definition))
    yield return new HubBriefingItem("notice","家宝",definition.name+"を育成中。","heirloom");
  }

  int discovery=meta.discoveredItems.Count+meta.discoveredEnemies.Count;
  yield return discovery>0
   ?new HubBriefingItem("progress","図鑑の記録",$"装備 {meta.discoveredItems.Count} / 敵 {meta.discoveredEnemies.Count}","codex")
   :new HubBriefingItem("progress","拠点の推奨","役職の進捗を確認できます。","guild");
 }

 VisualElement BuildHubBriefingItem(HubBriefingItem item,int index){
  bool hasFacility=!string.IsNullOrEmpty(item.facilityId);
  var facility=hasFacility?HubFacilityCatalog.Find(item.facilityId):default;
  var row=new Button(()=>{
   if(hasFacility)EnterHubFacility(facility);
  }){tooltip=item.title};
  row.AddToClassList("ps-hub-misprint__briefing-item");
  row.SetEnabled(!hasFacility||facility.unlocked);
  var ordinal=new Label((index+1).ToString("D2")){pickingMode=PickingMode.Ignore};
  ordinal.AddToClassList("ps-hub-misprint__briefing-index");
  row.Add(ordinal);
  var mark=Container($"ps-hub-misprint__briefing-mark ps-hub-misprint__briefing-mark--{item.kind}");
  mark.pickingMode=PickingMode.Ignore;
  row.Add(mark);

  var copy=Container("ps-hub-misprint__briefing-copy");
  copy.pickingMode=PickingMode.Ignore;
  var title=new Label(item.title){pickingMode=PickingMode.Ignore};
  title.AddToClassList("ps-hub-misprint__briefing-title");
  var body=new Label(item.body){pickingMode=PickingMode.Ignore};
  body.AddToClassList("ps-hub-misprint__briefing-body");
  copy.Add(title);
  copy.Add(body);
  row.Add(copy);
  var tail=Container("ps-hub-misprint__briefing-tail");
  tail.pickingMode=PickingMode.Ignore;
  row.Add(tail);

  return row;
 }

 void BindHubMission(MetaSave meta){
  hubMissionTitleLabel=RequireViewElement<Label>(hubShell,"hub-mission-title");
  hubMissionDestinationLabel=RequireViewElement<Label>(hubShell,"hub-mission-destination");
  hubMissionLoadoutLabel=RequireViewElement<Label>(hubShell,"hub-mission-loadout");
  hubMissionCargoLabel=RequireViewElement<Label>(hubShell,"hub-mission-cargo");
  RequireViewElement<Button>(hubShell,"hub-mission-action").clicked+=()=>EnterHubFacility(HubFacilityCatalog.Find("gate"));

  var active=LoadoutSystem.Active(meta);
  var destination=GameCatalog.Dungeons.FirstOrDefault();
  hubMissionTitleLabel.text=destination!=null
   ?destination.name+"へ荷を届ける。"
   :"遠征の支度を整える。";
  hubMissionDestinationLabel.text=destination?.name??"未指定";
  hubMissionLoadoutLabel.text=active?.name??"未設定";
  hubMissionCargoLabel.text=$"{active?.slots?.Count??0}件";
 }

 void BindHubStreetGuide(){
  hubStreetGuideModal=RequireViewElement<VisualElement>(hubShell,"hub-street-modal");
  hubStreetGuideFacilityScroll=RequireViewElement<ScrollView>(hubShell,"hub-street-list");
  hubStreetGuideDetail=RequireViewElement<VisualElement>(hubShell,"hub-street-detail");
  hubStreetGuideModal.pickingMode=PickingMode.Ignore;

  RequireViewElement<Button>(hubShell,"hub-street-backdrop").clicked+=CloseHubStreetGuide;
  RequireViewElement<Button>(hubShell,"hub-street-close").clicked+=CloseHubStreetGuide;
  BindHubStreetCategory("all");
  BindHubStreetCategory("scene");
  BindHubStreetCategory("workbench");
  BindHubStreetCategory("archive");
 }

 void BindHubStreetCategory(string id){
  RequireViewElement<Button>(hubShell,$"hub-cat-{id}").clicked+=()=>{
   hubStreetGuideCategory=id;
   hubStreetGuideSelectedIndex=0;
   RefreshHubStreetGuideCategories();
   PopulateHubStreetGuideList();
   RefreshHubStreetGuideDetail();
  };
 }

 void OpenHubStreetGuide(){
  hubStreetGuideOpen=true;
  if(hubStreetGuideSelectedIndex<0)hubStreetGuideSelectedIndex=0;
  hubStreetGuideModal.AddToClassList("is-open");
  hubStreetGuideModal.pickingMode=PickingMode.Position;
  RefreshHubStreetGuideCategories();
  PopulateHubStreetGuideList();
  RefreshHubStreetGuideDetail();
 }

 void CloseHubStreetGuide(){
  hubStreetGuideOpen=false;
  if(hubStreetGuideModal!=null){
   hubStreetGuideModal.RemoveFromClassList("is-open");
   hubStreetGuideModal.pickingMode=PickingMode.Ignore;
  }
  hubShell?.Focus();
 }

 void RefreshHubStreetGuideCategories(){
  if(hubStreetGuideModal==null)return;
  foreach(var id in new[]{"all","scene","workbench","archive"}){
   var button=hubStreetGuideModal.Q<Button>($"hub-cat-{id}");
   button?.EnableInClassList("ps-selected",id==hubStreetGuideCategory);
  }
 }

 void PopulateHubStreetGuideList(){
  if(hubStreetGuideFacilityScroll==null)return;
  hubStreetGuideFacilityScroll.contentContainer.Clear();
  var facilities=FilteredStreetGuideFacilities().ToArray();
  if(facilities.Length==0){
   hubStreetGuideSelectedIndex=0;
   return;
  }
  if(hubStreetGuideSelectedIndex>=facilities.Length)hubStreetGuideSelectedIndex=0;
  for(int index=0;index<facilities.Length;index++){
   int selectedIndex=index;
   var facility=facilities[index];
   var row=BuildHubChromeMapPin(facility,index==hubStreetGuideSelectedIndex,()=>{
    hubStreetGuideSelectedIndex=selectedIndex;
    PopulateHubStreetGuideList();
    RefreshHubStreetGuideDetail();
   });
   hubStreetGuideFacilityScroll.Add(row);
  }
 }

 IEnumerable<HubFacilityDef> FilteredStreetGuideFacilities(){
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
  hubStreetGuideDetail.Add(PackspireUiFactory.Body(facility.CategoryLabel+"  "+facility.eyebrow));
  hubStreetGuideDetail.Add(PackspireUiFactory.Body(facility.description));
  var enter=BuildHubChromePrimary("ここへ向かう",()=>{
   CloseHubStreetGuide();
   EnterHubFacility(facility);
  });
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
  }else if(evt.keyCode==KeyCode.UpArrow){
   MoveHubReel(-1);
   evt.StopPropagation();
  }else if(evt.keyCode==KeyCode.DownArrow){
   MoveHubReel(1);
   evt.StopPropagation();
  }else if(evt.keyCode==KeyCode.Return||evt.keyCode==KeyCode.KeypadEnter||evt.keyCode==KeyCode.Space){
   EnterHubFacility(HubFacilityCatalog.Find(PrimaryHubFacilityIds[hubReelIndex]));
   evt.StopPropagation();
  }else if(evt.keyCode==KeyCode.Escape)evt.StopPropagation();
 }
}
}
