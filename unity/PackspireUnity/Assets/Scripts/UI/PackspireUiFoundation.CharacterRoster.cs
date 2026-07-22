using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire {
public sealed partial class PackspireUiFoundation {
 VisualElement rosterShell;
 ScrollView rosterReelScroll;
 VisualElement rosterArtHost;
 ScrollView rosterDetailScrollHost;
 VisualElement rosterDetailBody;
 Button rosterConfirmButton;
 float rosterReelScrollY;

 void BuildCharacter(){
  var meta=game.UiMeta;
  if(string.IsNullOrEmpty(selectedCharacterId)||!CharacterCatalog.All.ContainsKey(selectedCharacterId))
   selectedCharacterId=meta.selectedCharacterId;
  if(string.IsNullOrEmpty(selectedCharacterId)||!CharacterCatalog.All.ContainsKey(selectedCharacterId))
   selectedCharacterId=CharacterCatalog.DefaultId;

  rosterShell=Container("ps-roster-screen ps-dark-surface");
  var backgroundHost=Container("ps-layer-background");
  var bg=HubBackgroundArt();
  if(bg==null)bg=CourtyardArt();
  if(bg!=null)backgroundHost.Add(Image(bg,new Rect(0,0,1,1),"ps-mgmt-bg",ScaleMode.ScaleAndCrop));
  var shade=Container("ps-mgmt-shade");
  shade.pickingMode=PickingMode.Ignore;
  backgroundHost.Add(shade);
  rosterShell.Add(backgroundHost);

  var contentHost=Container("ps-layer-content");
  var header=Container("ps-mgmt-header");
  header.Add(ChromeBrand("ROSTER  /  RECRUIT","遠征者の選択"));
  contentHost.Add(header);

  var body=Container("ps-roster-body");
  var reelCol=Container("ps-roster-col-reel");
  var reelHead=new Label("キャラクター"){pickingMode=PickingMode.Ignore};
  reelHead.AddToClassList("ps-roster-reel-heading");
  reelCol.Add(reelHead);
  rosterReelScroll=new ScrollView(ScrollViewMode.Vertical);
  rosterReelScroll.AddToClassList("ps-roster-reel-scroll");
  rosterReelScroll.verticalScrollerVisibility=ScrollerVisibility.Auto;
  rosterReelScroll.scrollOffset=new Vector2(0,rosterReelScrollY);
  var reel=Container("ps-roster-reel");
  reel.name="roster-reel";
  foreach(var character in CharacterCatalog.Roster){
   var def=character;
   reel.Add(ManagementReelRow(def.id,def.name,"",def.id==selectedCharacterId,()=>SelectRosterCharacter(def.id)));
  }
  rosterReelScroll.Add(reel);
  reelCol.Add(rosterReelScroll);
  body.Add(reelCol);

  var artCol=Container("ps-roster-col-art");
  rosterArtHost=Container("ps-roster-art-host");
  artCol.Add(rosterArtHost);
  body.Add(artCol);

  var detailCol=Container("ps-roster-col-detail");
  rosterDetailScrollHost=new ScrollView(ScrollViewMode.Vertical);
  rosterDetailScrollHost.AddToClassList("ps-roster-detail-scroll");
  rosterDetailScrollHost.verticalScrollerVisibility=ScrollerVisibility.Auto;
  rosterDetailBody=Container("ps-roster-detail-body");
  rosterDetailScrollHost.Add(rosterDetailBody);
  detailCol.Add(rosterDetailScrollHost);
  var footer=Container("ps-roster-footer");
  rosterConfirmButton=PackspireUiFactory.Button(meta.characterMade?"このキャラクターを選ぶ":"このキャラで始める",ConfirmRosterSelection);
  rosterConfirmButton.AddToClassList("ps-primary-action");
  rosterConfirmButton.AddToClassList("ps-chrome-action");
  rosterConfirmButton.AddToClassList("ps-roster-confirm");
  footer.Add(rosterConfirmButton);
  detailCol.Add(footer);
  body.Add(detailCol);

  contentHost.Add(body);
  rosterShell.Add(contentHost);
  screenRoot.Add(rosterShell);

  RefreshRosterArt();
  RefreshRosterDetail();
  rosterReelScroll?.schedule.Execute(()=>{
   if(rosterReelScroll!=null)rosterReelScroll.scrollOffset=new Vector2(0,rosterReelScrollY);
  }).ExecuteLater(0);
 }

 void SelectRosterCharacter(string characterId){
  if(string.IsNullOrEmpty(characterId)||characterId==selectedCharacterId)return;
  if(rosterReelScroll!=null)rosterReelScrollY=rosterReelScroll.scrollOffset.y;
  selectedCharacterId=characterId;
  game.UiSelectCharacter(characterId);
  UpdateRosterReelSelection();
  RefreshRosterArt();
  RefreshRosterDetail();
  rosterReelScroll?.schedule.Execute(()=>{
   if(rosterReelScroll!=null)rosterReelScroll.scrollOffset=new Vector2(0,rosterReelScrollY);
  }).ExecuteLater(0);
 }

 void UpdateRosterReelSelection(){
  if(rosterReelScroll==null)return;
  var reel=rosterReelScroll.Q(name:"roster-reel");
  if(reel==null)return;
  foreach(var child in reel.Children()){
   if(child is not Button row||row.userData is not string id)continue;
   row.EnableInClassList("ps-selected",id==selectedCharacterId);
  }
 }

 void RefreshRosterArt(){
  if(rosterArtHost==null)return;
  rosterArtHost.Clear();
  var character=CharacterCatalog.Get(selectedCharacterId);
  rosterArtHost.Add(CharacterPortraitFront(character,"ps-roster-art-image"));
 }

 void RefreshRosterDetail(){
  if(rosterDetailBody==null)return;
  rosterDetailBody.Clear();
  var character=CharacterCatalog.Get(selectedCharacterId);
  rosterDetailBody.Add(PackspireUiFactory.Title(character.name));
  rosterDetailBody.Add(PackspireUiFactory.Body(character.title));
  rosterDetailBody.Add(ManagementSection("説明",character.description));
  rosterDetailBody.Add(ManagementSection("特性",$"{character.traitName}\n{character.traitText}"));
  rosterDetailBody.Add(ManagementSection("能動スキル",$"{character.activeSkillName}\n{character.activeSkillText}"));
  rosterDetailBody.Add(ManagementSection("出自","—"));
 }

 void ConfirmRosterSelection(){
  game.UiSelectCharacter(selectedCharacterId);
  if(!game.UiMeta.characterMade){
   game.UiFinishCharacter();
   navBackStack.Clear();
   ForceRefreshScreen();
   return;
  }
  SaveSystem.Save(game.UiMeta);
  ShowToast(CharacterCatalog.Get(selectedCharacterId).name+"を選択しました");
  NavGoBack();
 }
}
}
