using System.Linq;
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

  var shell=BookShell("遠征者の選択",null);
  shell.AddToClassList("ps-character-roster-shell");
  screenRoot.Add(shell);
  var pages=shell.Q<VisualElement>("book-pages");
  var left=Page("キャラクター一覧");
  var right=Page("詳細");
  pages.Add(left);
  pages.Add(right);

  var grid=Container("ps-character-roster-grid");
  left.Add(grid);
  foreach(var character in CharacterCatalog.Roster){
   var def=character;
   var tile=CharacterPortraitButton(
    def,
    def.id==selectedCharacterId,
    ()=>{selectedCharacterId=def.id;game.UiSelectCharacter(def.id);BuildCharacterAgain();});
   tile.AddToClassList("ps-character-roster-tile");
   grid.Add(tile);
  }

  var selected=CharacterCatalog.Get(selectedCharacterId);
  right.Add(CharacterPortrait(selected,"ps-character-preview"));
  right.Add(PackspireUiFactory.Title($"{selected.name}　{selected.title}"));
  right.Add(PackspireUiFactory.Body(selected.description));

  var traitBox=Container("ps-character-trait-box");
  traitBox.Add(PackspireUiFactory.Title("特性"));
  var traitName=new Label(selected.traitName){pickingMode=PickingMode.Ignore};
  traitName.AddToClassList("ps-character-trait-name");
  traitBox.Add(traitName);
  traitBox.Add(PackspireUiFactory.Body(selected.traitText));
  right.Add(traitBox);

  var skillBox=Container("ps-character-skill-box");
  skillBox.Add(PackspireUiFactory.Title("能動スキル"));
  var skillName=new Label(selected.activeSkillName){pickingMode=PickingMode.Ignore};
  skillName.AddToClassList("ps-character-skill-name");
  skillBox.Add(skillName);
  skillBox.Add(PackspireUiFactory.Body(selected.activeSkillText));
  right.Add(skillBox);

  right.Add(PackspireUiFactory.Body("役職と収納は拠点メニューから選びます。\n遠征ごとに「誰で・何として・何を持つか」を組み合わせてください。"));

  var start=PackspireUiFactory.Button("このキャラで拠点へ",()=>{
   game.UiSelectCharacter(selectedCharacterId);
   game.UiFinishCharacter();
   ForceRefreshScreen();
  });
  start.AddToClassList("ps-primary-action");
  right.Add(start);
 }

 void BuildCharacterAgain(){screenRoot.Clear();BuildCharacter();}
}
}
