using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire {
public sealed partial class PackspireUiFoundation {
 void BuildHub(){
  ReleasePresentationStage();
  var meta=game.UiMeta;
  var shell=Container("ps-hub-home");
  screenRoot.Add(shell);

  var row=Container("ps-hub-home-row");
  shell.Add(row);

  var stage=Container("ps-hub-home-stage");
  stage.Add(BuildHubCharacterPortrait());
  row.Add(stage);

  var character=CharacterCatalog.Get(meta.selectedCharacterId);
  var menu=Container("ps-hub-home-menu");
  var title=PackspireUiFactory.Title("拠点");
  title.AddToClassList("ps-hub-home-title");
  menu.Add(title);
  menu.Add(HubMetaLine($"{character.name}　{character.title}"));
  menu.Add(HubMetaLine($"{GameCatalog.Roles[meta.currentRole].name}　／　{FactionName(meta.currentFaction)}"));
  menu.Add(HubMetaLine($"{meta.baseGold}G　　遠征 {meta.wins}勝 / {meta.runs}回"));

  (string label,ScreenId screen)[] entries={
   ("遠征",ScreenId.Expedition),
   ("荷造り",ScreenId.Pack),
   ("保管",ScreenId.Vault),
   ("役職",ScreenId.Status),
   ("勢力",ScreenId.Faction),
   ("図鑑",ScreenId.Compendium),
   ("キャラ変更",ScreenId.Character),
  };
  foreach(var entry in entries){
   var button=PackspireUiFactory.Button(entry.label,()=>game.UiNavigate(entry.screen));
   button.AddToClassList("ps-hub-home-btn");
   menu.Add(button);
  }
  row.Add(menu);
 }

 VisualElement HubMetaLine(string text){
  var label=PackspireUiFactory.Body(text);
  label.AddToClassList("ps-hub-home-meta");
  return label;
 }

 VisualElement BuildHubCharacterPortrait(){
  var meta=game.UiMeta;
  var character=CharacterCatalog.Get(meta.selectedCharacterId);
  var strip=Container("ps-hub-home-character");
  strip.pickingMode=PickingMode.Ignore;
  strip.Add(Atlas(game.UiCharacterArt,CharacterUv(character.portraitBody,character.portraitHair),"ps-hub-home-character-portrait"));
  return strip;
 }

 void ReleasePresentationStage(){
  presentationHubBuilt=false;
  presentationStageView=null;
  presentationEnterButton=null;
  presentationHintLabel=null;
  presentationFacilityLabel=null;
  presentationEntering=false;
  if(presentationStage!=null)savedHubFacility=presentationStage.FocusedFacility;
  if(presentationStage==null)return;
  var host=presentationStage.gameObject;
  presentationStage=null;
  if(host!=null)Destroy(host);
 }

 void EnsurePresentationStage(){
  if(presentationStage!=null)return;
  var stale=game.transform.Find("HubPresentationStage");
  if(stale!=null)Destroy(stale.gameObject);
  var hostGo=new GameObject("HubPresentationStage");
  hostGo.transform.SetParent(game.transform,false);
  presentationStage=hostGo.AddComponent<PackspirePresentationStage>();
 }
}
}
