using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire {
public sealed partial class PackspireUiFoundation {
 void BuildEvent(){
  var run=game.UiRun;
  var content=game.UiCurrentEvent;
  var screen=CloneView("UI/PackspireEventView","ps-event-screen");
  if(screen==null){
   Debug.LogError("Event view could not be created.");
   return;
  }
  screenRoot.Add(screen);
  if(game.UiDungeonArt!=null)
   RequireViewElement<VisualElement>(screen,"event-background").Add(
    Atlas(game.UiDungeonArt,DungeonUv(run?.dungeon??"old_spire"),"ps-event-background")
   );
  RequireViewElement<VisualElement>(screen,"event-header").Add(
   ChromeBrand(content?.eyebrow??"ANOMALY  /  RITE",content?.title??"異変",PackspireUiFactory.PopIcon.Objective)
  );
  RequireViewElement<VisualElement>(screen,"event-body").Add(
   PackspireUiFactory.Body(content?.body??"異変は静かに揺らいでいる。")
  );
  var choiceHost=RequireViewElement<VisualElement>(screen,"event-choices");
  var choices=content?.choices??System.Array.Empty<EventChoiceContent>();
  for(int i=0;i<choices.Length;i++){
   int choiceIndex=i;
   var choice=choices[i];
   choiceHost.Add(Choice(choice.label,EffectSummary(choice),()=>game.UiResolveEvent(choiceIndex)));
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
