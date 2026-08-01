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

  rosterShell=CloneView("UI/PackspireCharacterView","ps-roster-screen ps-dark-surface");
  if(rosterShell==null){
   Debug.LogError("Character view could not be created.");
   return;
  }
  RequireViewElement<VisualElement>(rosterShell,"character-background");

  RequireViewElement<VisualElement>(rosterShell,"character-header");

  RequireViewElement<VisualElement>(rosterShell,"character-reel-column");
  rosterReelScroll=RequireViewElement<ScrollView>(rosterShell,"character-reel-scroll");
  rosterReelScroll.verticalScrollerVisibility=ScrollerVisibility.Auto;
  rosterReelScroll.scrollOffset=new Vector2(0,rosterReelScrollY);
  var reel=RequireViewElement<VisualElement>(rosterShell,"character-reel");
  reel.name="roster-reel";
  foreach(var character in CharacterCatalog.Roster){
   var def=character;
   reel.Add(ManagementReelRow(def.id,def.name,"",def.id==selectedCharacterId,()=>SelectRosterCharacter(def.id)));
  }

  rosterArtHost=RequireViewElement<VisualElement>(rosterShell,"character-art-host");

  RequireViewElement<VisualElement>(rosterShell,"character-detail-column");
  rosterDetailScrollHost=RequireViewElement<ScrollView>(rosterShell,"character-detail-scroll");
  rosterDetailScrollHost.verticalScrollerVisibility=ScrollerVisibility.Auto;
  rosterDetailBody=RequireViewElement<VisualElement>(rosterShell,"character-detail-body");
  var footer=RequireViewElement<VisualElement>(rosterShell,"character-footer");
  rosterConfirmButton=PackspireUiFactory.Button(meta.characterMade?"このキャラクターを選ぶ":"このキャラで始める",ConfirmRosterSelection);
  rosterConfirmButton.AddToClassList("ps-primary-action");
  rosterConfirmButton.AddToClassList("ps-chrome-action");
  rosterConfirmButton.AddToClassList("ps-roster-confirm");
  PackspireUiFactory.DecorateActionButton(rosterConfirmButton,true);
  footer.Add(rosterConfirmButton);
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
