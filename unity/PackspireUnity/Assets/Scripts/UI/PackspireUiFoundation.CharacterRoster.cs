using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire {
public sealed partial class PackspireUiFoundation {
 string selectedCharacterId="";

 void BuildCharacter(){
  var meta=game.UiMeta;
  if(string.IsNullOrEmpty(selectedCharacterId)||!CharacterCatalog.All.ContainsKey(selectedCharacterId))
   selectedCharacterId=meta.selectedCharacterId;
  if(string.IsNullOrEmpty(selectedCharacterId)||!CharacterCatalog.All.ContainsKey(selectedCharacterId))
   selectedCharacterId=CharacterCatalog.DefaultId;

  var shell=Container("ps-dd-roster-overlay");
  screenRoot.Add(shell);

  var bg=HubBackgroundArt();
  if(bg==null)bg=CourtyardArt();
  if(bg!=null)shell.Add(Image(bg,new Rect(0,0,1,1),"ps-dd-roster-bg",ScaleMode.ScaleAndCrop));
  var dim=Container("ps-dd-roster-dim");
  dim.pickingMode=PickingMode.Ignore;
  shell.Add(dim);

  var modal=Container("ps-dd-roster-modal");
  shell.Add(modal);

  var header=Container("ps-dd-roster-header");
  header.Add(ChromeBrand("ROSTER  /  RECRUIT","遠征者の選択"));
  var close=new Button(()=>game.UiNavigate(ScreenId.Hub)){text="×"};
  close.AddToClassList("ps-dd-roster-close");
  close.AddToClassList("ps-hub-chrome-btn");
  header.Add(close);
  modal.Add(header);

  var body=Container("ps-dd-roster-body");
  modal.Add(body);

  var reelHost=Container("ps-dd-roster-name-reel-host");
  var reelScroll=new ScrollView(ScrollViewMode.Vertical);
  reelScroll.AddToClassList("ps-dd-roster-name-reel-scroll");
  var reel=Container("ps-dd-roster-name-reel");
  foreach(var character in CharacterCatalog.Roster){
   var def=character;
   var plate=new Button(()=>{
    selectedCharacterId=def.id;
    game.UiSelectCharacter(def.id);
    BuildCharacterAgain();
   }){text=def.name};
   plate.AddToClassList("ps-dd-roster-name-plate");
   plate.AddToClassList("ps-hub-chrome-btn");
   if(def.id==selectedCharacterId)plate.AddToClassList("ps-selected");
   reel.Add(plate);
  }
  reelScroll.Add(reel);
  reelHost.Add(reelScroll);
  body.Add(reelHost);

  var selected=CharacterCatalog.Get(selectedCharacterId);
  var center=Container("ps-dd-roster-center");
  center.Add(CharacterPortraitFront(selected,"ps-character-preview"));
  body.Add(center);

  var detail=Container("ps-dd-roster-detail");
  detail.Add(PackspireUiFactory.Title($"{selected.name}　{selected.title}"));
  detail.Add(PackspireUiFactory.Body(selected.description));

  var traitBox=Container("ps-character-trait-box");
  traitBox.Add(PackspireUiFactory.Title("特性"));
  var traitName=new Label(selected.traitName){pickingMode=PickingMode.Ignore};
  traitName.AddToClassList("ps-character-trait-name");
  traitBox.Add(traitName);
  traitBox.Add(PackspireUiFactory.Body(selected.traitText));
  detail.Add(traitBox);

  var skillBox=Container("ps-character-skill-box");
  skillBox.Add(PackspireUiFactory.Title("能動スキル"));
  var skillName=new Label(selected.activeSkillName){pickingMode=PickingMode.Ignore};
  skillName.AddToClassList("ps-character-skill-name");
  skillBox.Add(skillName);
  skillBox.Add(PackspireUiFactory.Body(selected.activeSkillText));
  detail.Add(skillBox);

  var originBox=Container("ps-dd-roster-origin-box");
  originBox.Add(PackspireUiFactory.Title("出自"));
  originBox.Add(PackspireUiFactory.Body("—"));
  detail.Add(originBox);

  var actions=Container("ps-dd-roster-actions");
  var start=PackspireUiFactory.Button("このキャラで拠点へ",()=>{
   game.UiSelectCharacter(selectedCharacterId);
   game.UiFinishCharacter();
   ForceRefreshScreen();
  });
  start.AddToClassList("ps-dd-roster-confirm");
  start.AddToClassList("ps-hub-chrome-btn");
  actions.Add(start);
  detail.Add(actions);
  body.Add(detail);
 }

 void BuildCharacterAgain()=>RebuildScreen(BuildCharacter);
}
}
