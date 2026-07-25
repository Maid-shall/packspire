using UnityEngine.UIElements;

namespace Packspire {
public sealed partial class PackspireUiFoundation {
 void BuildEvent(){
  var run=game.UiRun;
  var content=game.UiCurrentEvent;
  var screen=Container("ps-event-screen");
  screenRoot.Add(screen);
  if(game.UiDungeonArt!=null)
   screen.Add(Atlas(game.UiDungeonArt,DungeonUv(run?.dungeon??"old_spire"),"ps-event-background"));
  var mist=Container("ps-event-mist");
  screen.Add(mist);
  var dialog=Container("ps-event-panel");
  mist.Add(dialog);
  dialog.Add(ChromeBrand(content?.eyebrow??"ANOMALY  /  RITE",content?.title??"異変"));
  dialog.Add(PackspireUiFactory.Body(content?.body??"異変は静かに揺らいでいる。"));
  var choices=content?.choices??System.Array.Empty<EventChoiceContent>();
  for(int i=0;i<choices.Length;i++){
   int choiceIndex=i;
   var choice=choices[i];
   dialog.Add(Choice(choice.label,EffectSummary(choice),()=>game.UiResolveEvent(choiceIndex)));
  }
 }
 static string EffectSummary(EventChoiceContent choice){
  if(choice?.effects==null||choice.effects.Length==0)return "何も変えず探索へ戻る";
  var parts=new System.Collections.Generic.List<string>();
  foreach(var effect in choice.effects){
   if(effect.effect==EventEffectType.Hp)parts.Add($"HP {effect.amount:+#;-#;0}");
   else if(effect.effect==EventEffectType.Gold)parts.Add($"{effect.amount:+#;-#;0}G");
   else if(effect.effect==EventEffectType.RepairAll)parts.Add("所持装備の耐久を回復");
  }
  return string.Join("　／　",parts);
 }
 VisualElement Choice(string title,string description,System.Action action){
  var button=PackspireUiFactory.Card(title,description,action);
  button.AddToClassList("ps-event-choice");
  return button;
 }
}
}
