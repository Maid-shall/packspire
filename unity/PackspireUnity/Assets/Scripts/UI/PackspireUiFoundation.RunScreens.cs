using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire {
public sealed partial class PackspireUiFoundation {
 void BuildEvent(){
  var content=game.UiCurrentEvent;
  var screen=CloneView("UI/PackspireEventView","ps-event-screen");
  if(screen==null){
   Debug.LogError("Event view could not be created.");
   return;
  }
  screenRoot.Add(screen);
  RequireViewElement<VisualElement>(screen,"event-background");
  RequireViewElement<VisualElement>(screen,"event-header");
  RequireViewElement<Label>(screen,"event-header-eyebrow").text=content?.eyebrow??"ANOMALY / RITE";
  RequireViewElement<Label>(screen,"event-header-title").text=content?.title??"異変";
  RequireViewElement<VisualElement>(screen,"event-body");
  RequireViewElement<Label>(screen,"event-body-copy").text=content?.body??"異変は静かに揺らいでいる。";
  var choiceHost=RequireViewElement<VisualElement>(screen,"event-choices");
  var choices=content?.choices??System.Array.Empty<EventChoiceContent>();
  for(int i=0;i<choices.Length;i++){
   int choiceIndex=i;
   var choice=choices[i];
   choiceHost.Add(Choice(i,choice.label,EffectSummary(choice),()=>game.UiResolveEvent(choiceIndex)));
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
 VisualElement Choice(int index,string title,string description,System.Action action){
  var button=PackspireUiFactory.Card(title,description,action);
  button.AddToClassList("ps-event-choice");
  var number=new Label($"{index+1:00}"){pickingMode=PickingMode.Ignore};
  number.AddToClassList("ps-event-choice-index");
  button.Insert(0,number);
  return button;
 }
}
}
