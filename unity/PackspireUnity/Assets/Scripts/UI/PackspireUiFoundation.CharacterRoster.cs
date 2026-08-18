using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire {
public sealed partial class PackspireUiFoundation {
 VisualElement rosterShell;
 ScrollView rosterReelScroll;
 VisualElement rosterReelHost;
 VisualElement rosterArtHost;
 ScrollView rosterDetailScrollHost;
 VisualElement rosterDetailBody;
 VisualElement rosterDossierPortraitHost;
 Button rosterConfirmButton;
 Label rosterArtCaptionName;
 Label rosterArtCaptionTitle;
 Label rosterConfirmLabel;
 float rosterReelScrollY;

 void BuildCharacter(){
  var meta=game.UiMeta;
  if(!string.IsNullOrEmpty(meta.selectedCharacterId)&&CharacterCatalog.All.ContainsKey(meta.selectedCharacterId))
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
  rosterReelHost=RequireViewElement<VisualElement>(rosterShell,"character-reel");
  rosterReelHost.name="roster-reel";
  foreach(var character in CharacterCatalog.Roster)
   rosterReelHost.Add(BuildRosterCharacterRow(character));

  rosterArtHost=RequireViewElement<VisualElement>(rosterShell,"character-art-host");
  rosterArtCaptionName=RequireViewElement<Label>(rosterShell,"character-art-caption-name");
  rosterArtCaptionTitle=RequireViewElement<Label>(rosterShell,"character-art-caption-title");

  RequireViewElement<VisualElement>(rosterShell,"character-detail-column");
  rosterDetailScrollHost=RequireViewElement<ScrollView>(rosterShell,"character-detail-scroll");
  rosterDetailScrollHost.verticalScrollerVisibility=ScrollerVisibility.Auto;
  rosterDetailBody=RequireViewElement<VisualElement>(rosterShell,"character-detail-body");
  rosterDossierPortraitHost=RequireViewElement<VisualElement>(rosterShell,"character-dossier-portrait-host");
  RequireViewElement<VisualElement>(rosterShell,"character-footer");
  rosterConfirmButton=RequireViewElement<Button>(rosterShell,"character-confirm");
  rosterConfirmLabel=RequireViewElement<Label>(rosterShell,"character-confirm-label");
  rosterConfirmLabel.text=meta.characterMade?"この配達人を選ぶ":"この配達人で始める";
  rosterConfirmButton.clicked+=ConfirmRosterSelection;
  screenRoot.Add(rosterShell);

  RefreshRosterArt();
  RefreshRosterDetail();
  rosterReelScroll?.schedule.Execute(()=>{
   if(rosterReelScroll!=null)rosterReelScroll.scrollOffset=new Vector2(0,rosterReelScrollY);
  }).ExecuteLater(0);
 }

 Button BuildRosterCharacterRow(CharacterDef character){
  var row=new Button(()=>SelectRosterCharacter(character.id)){userData=character.id};
  row.AddToClassList("ps-character-roster-row");
  row.AddToClassList("ps-list-item");
  if(character.id==selectedCharacterId)row.AddToClassList("ps-selected");

  row.Add(BuildRosterCharacterPortrait(character,"ps-character-roster-portrait"));
  var copy=Container("ps-character-roster-copy");
  var name=new Label(character.name){pickingMode=PickingMode.Ignore};
  name.AddToClassList("ps-character-roster-name");
  copy.Add(name);
  var title=new Label(character.title){pickingMode=PickingMode.Ignore};
  title.AddToClassList("ps-character-roster-title");
  copy.Add(title);
  row.Add(copy);
  var register=new Label("◇"){pickingMode=PickingMode.Ignore};
  register.AddToClassList("ps-character-roster-register");
  row.Add(register);
  return row;
 }

 Image BuildRosterCharacterPortrait(CharacterDef character,string className){
  var sprite=game.ResolveCharacterPortraitSprite(character);
  if(sprite!=null)
   return SpriteImage(sprite,new Rect(0,0,1,1),className,ScaleMode.ScaleAndCrop);
  var texture=game.ResolveCharacterPortrait(character);
  var uv=texture==game.UiCharacterArt
   ?CharacterUv(character.portraitBody,character.portraitHair)
   :new Rect(0,0,1,1);
  return Image(texture,uv,className,ScaleMode.ScaleAndCrop);
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
  if(rosterDossierPortraitHost!=null){
   rosterDossierPortraitHost.Clear();
   rosterDossierPortraitHost.Add(BuildRosterCharacterPortrait(character,"ps-character-dossier-portrait-image"));
  }
  if(rosterArtCaptionName!=null)rosterArtCaptionName.text=character.name;
  if(rosterArtCaptionTitle!=null)rosterArtCaptionTitle.text=character.title;
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
  string sight=character.explorationSightBonus==0?"補正なし":$"視界補正 +{character.explorationSightBonus}";
  rosterDetailBody.Add(ManagementSection("探索適性",sight));
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
