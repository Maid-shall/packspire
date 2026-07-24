using UnityEngine.UIElements;

namespace Packspire {
public sealed partial class PackspireUiFoundation {
 void BuildEvent(){
  var run=game.UiRun;
  var screen=Container("ps-event-screen");
  screenRoot.Add(screen);
  if(game.UiDungeonArt!=null)
   screen.Add(Atlas(game.UiDungeonArt,DungeonUv(run?.dungeon??"old_spire"),"ps-event-background"));
  var mist=Container("ps-event-mist");
  screen.Add(mist);
  var dialog=Container("ps-event-panel");
  mist.Add(dialog);
  dialog.Add(ChromeBrand("ANOMALY  /  RITE","記憶の揺らぎ"));
  dialog.Add(PackspireUiFactory.Body("空間の奥で、紫の残光がゆっくりと鼓動している。触れれば何かが変わる。その代償までは、まだ記されていない。"));
  dialog.Add(Choice("代償を支払う","HP -6　／　24Gを獲得",()=>game.UiResolveEvent(0)));
  dialog.Add(Choice("残響を修復する","所持装備の耐久をすべて回復",()=>game.UiResolveEvent(1)));
  dialog.Add(Choice("立ち去る","何も変えず探索へ戻る",()=>game.UiResolveEvent(2)));
 }
 VisualElement Choice(string title,string description,System.Action action){
  var button=PackspireUiFactory.Card(title,description,action);
  button.AddToClassList("ps-event-choice");
  return button;
 }
}
}
