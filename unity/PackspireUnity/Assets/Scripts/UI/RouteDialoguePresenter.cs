using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire {
/// <summary>
/// Bottom gradient dialogue / choice layer for route events.
/// Keeps the 2.5D stage full-bleed; no central popup takeover.
/// </summary>
public sealed class RouteDialoguePresenter {
 public VisualElement Root{get;private set;}
 public bool IsOpen{get;private set;}
 public bool IsBubbleOnly{get;private set;}

 VisualElement veil,panel,choiceHost,bubble;
 Label speakerLabel,bodyLabel,bubbleLabel;
 readonly List<VisualElement> choiceRows=new();
 int highlighted=-1;
 float animT;
 Action<int> onChoice;
 Action<int> onChoiceHover;
 Action onChoiceHoverClear;

 public void Build(VisualElement parent){
  Root=new VisualElement{name="route-dialogue",pickingMode=PickingMode.Ignore};
  Root.AddToClassList("ps-route-dialogue");
  Root.style.display=DisplayStyle.None;

  veil=new VisualElement{pickingMode=PickingMode.Ignore};
  veil.AddToClassList("ps-route-dialogue-veil");
  Root.Add(veil);

  bubble=new VisualElement{pickingMode=PickingMode.Ignore};
  bubble.AddToClassList("ps-route-bubble");
  bubble.style.display=DisplayStyle.None;
  bubbleLabel=new Label(""){pickingMode=PickingMode.Ignore};
  bubbleLabel.AddToClassList("ps-route-bubble-text");
  bubble.Add(bubbleLabel);
  Root.Add(bubble);

  panel=new VisualElement{pickingMode=PickingMode.Position};
  panel.AddToClassList("ps-route-dialogue-panel");
  speakerLabel=new Label(""){pickingMode=PickingMode.Ignore};
  speakerLabel.AddToClassList("ps-route-dialogue-speaker");
  panel.Add(speakerLabel);
  bodyLabel=new Label(""){pickingMode=PickingMode.Ignore};
  bodyLabel.AddToClassList("ps-route-dialogue-body");
  panel.Add(bodyLabel);
  choiceHost=new VisualElement{pickingMode=PickingMode.Position};
  choiceHost.AddToClassList("ps-route-dialogue-choices");
  panel.Add(choiceHost);
  Root.Add(panel);

  parent.Add(Root);
 }

 public void Detach(){
  Root?.RemoveFromHierarchy();
  Root=null;veil=null;panel=null;choiceHost=null;bubble=null;
  speakerLabel=null;bodyLabel=null;bubbleLabel=null;
  choiceRows.Clear();
  IsOpen=false;
 }

 public void SetCallbacks(Action<int> choice,Action<int> hover,Action hoverClear){
  onChoice=choice;onChoiceHover=hover;onChoiceHoverClear=hoverClear;
 }

 public void ShowConversation(string speaker,string body,IReadOnlyList<string> choices){
  if(Root==null)return;
  IsOpen=true;IsBubbleOnly=false;
  bubble.style.display=DisplayStyle.None;
  panel.style.display=DisplayStyle.Flex;
  speakerLabel.text=string.IsNullOrEmpty(speaker)?"":speaker;
  speakerLabel.style.display=string.IsNullOrEmpty(speaker)?DisplayStyle.None:DisplayStyle.Flex;
  bodyLabel.text=body??"";
  RebuildChoices(choices);
  Root.style.display=DisplayStyle.Flex;
  Root.pickingMode=PickingMode.Ignore;
  panel.pickingMode=PickingMode.Position;
  animT=.28f;
  Root.RemoveFromClassList("ps-route-dialogue-out");
  Root.AddToClassList("ps-route-dialogue-in");
 }

 public void ShowBubble(string text,float seconds=2.4f){
  if(Root==null)return;
  IsOpen=true;IsBubbleOnly=true;
  panel.style.display=DisplayStyle.None;
  bubble.style.display=DisplayStyle.Flex;
  bubbleLabel.text=text??"";
  Root.style.display=DisplayStyle.Flex;
  Root.pickingMode=PickingMode.Ignore;
  animT=seconds;
  ClearChoices();
 }

 public void Hide(){
  if(Root==null)return;
  IsOpen=false;IsBubbleOnly=false;
  onChoiceHoverClear?.Invoke();
  ClearChoices();
  Root.style.display=DisplayStyle.None;
  bubble.style.display=DisplayStyle.None;
  panel.style.display=DisplayStyle.None;
  Root.RemoveFromClassList("ps-route-dialogue-in");
 }

 public void Tick(float dt){
  if(!IsOpen||Root==null)return;
  if(animT>0f){
   animT=Mathf.Max(0f,animT-dt);
   if(IsBubbleOnly&&animT<=0f)Hide();
  }
 }

 void RebuildChoices(IReadOnlyList<string> choices){
  ClearChoices();
  if(choiceHost==null||choices==null||choices.Count==0)return;
  for(int i=0;i<choices.Count;i++){
   int index=i;
   var row=new VisualElement{pickingMode=PickingMode.Position};
   row.AddToClassList("ps-route-choice-row");
   var mark=new Label(i==0?"▶":" "){pickingMode=PickingMode.Ignore};
   mark.AddToClassList("ps-route-choice-mark");
   var text=new Label(choices[i]){pickingMode=PickingMode.Ignore};
   text.AddToClassList("ps-route-choice-text");
   row.Add(mark);row.Add(text);
   row.RegisterCallback<PointerEnterEvent>(_=>Highlight(index));
   row.RegisterCallback<PointerLeaveEvent>(_=>Unhighlight(index));
   row.RegisterCallback<ClickEvent>(_=>onChoice?.Invoke(index));
   choiceHost.Add(row);
   choiceRows.Add(row);
  }
  highlighted=-1;
 }

 void ClearChoices(){
  choiceHost?.Clear();
  choiceRows.Clear();
  highlighted=-1;
 }

 void Highlight(int index){
  highlighted=index;
  for(int i=0;i<choiceRows.Count;i++){
   bool on=i==index;
   choiceRows[i].EnableInClassList("ps-route-choice-hot",on);
   if(choiceRows[i].childCount>0&&choiceRows[i][0] is Label mark)
    mark.text=on?"▶":" ";
  }
  onChoiceHover?.Invoke(index);
 }

 void Unhighlight(int index){
  if(highlighted!=index)return;
  highlighted=-1;
  if(index>=0&&index<choiceRows.Count){
   choiceRows[index].EnableInClassList("ps-route-choice-hot",false);
   if(choiceRows[index].childCount>0&&choiceRows[index][0] is Label mark)mark.text=" ";
  }
  onChoiceHoverClear?.Invoke();
 }
}
}
