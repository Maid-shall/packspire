using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire {
public sealed partial class PackspireUiFoundation {
// Board-local events and the exploration runtime tick.
 void BuildGridBoardEventOverlay(){
  if(gridBoardRoot==null)return;
  if(gridBoardEventOverlay!=null)
   gridBoardEventOverlay.RemoveFromHierarchy();
  gridBoardEventOverlay=Container("ps-gboard-event-overlay");
  gridBoardEventOverlay.style.display=DisplayStyle.None;
  var content=game.UiCurrentEvent;
  var panel=Container("ps-gboard-event-popup");
  var artColumn=Container("ps-gboard-event-art-column");
  if(content?.artwork!=null){
   var art=new Image{
    sprite=content.artwork,
    scaleMode=ScaleMode.ScaleAndCrop,
    pickingMode=PickingMode.Ignore
   };
   art.AddToClassList("ps-gboard-event-art");
   artColumn.Add(art);
  } else {
   var fallback=new Label("◇"){pickingMode=PickingMode.Ignore};
   fallback.AddToClassList("ps-gboard-event-art-fallback");
   artColumn.Add(fallback);
  }
  panel.Add(artColumn);

  var copy=Container("ps-gboard-event-copy");
  var eyebrow=new Label(content?.eyebrow??"ANOMALY  /  RITE"){pickingMode=PickingMode.Ignore};
  eyebrow.AddToClassList("ps-gboard-event-eyebrow");
  copy.Add(eyebrow);
  var title=new Label(content?.title??"異変を発見"){pickingMode=PickingMode.Ignore};
  title.AddToClassList("ps-gboard-event-title");
  copy.Add(title);
  var rule=Container("ps-gboard-event-rule");
  rule.pickingMode=PickingMode.Ignore;
  copy.Add(rule);
  var body=new Label(content?.body??"足元の封印が脈打っている。どう対処するか選べ。"){
   pickingMode=PickingMode.Ignore
  };
  body.AddToClassList("ps-gboard-event-body");
  copy.Add(body);
  var choices=Container("ps-gboard-event-choices");
  var eventChoices=content?.choices??System.Array.Empty<EventChoiceContent>();
  for(int i=0;i<eventChoices.Length;i++){
   int choiceIndex=i;
   var choice=eventChoices[i];
   var button=MakeGridAction("",()=>ResolveGridBoardEvent(choiceIndex));
   button.AddToClassList("ps-gboard-event-choice");
   var choiceLabel=new Label(choice.label){pickingMode=PickingMode.Ignore};
   choiceLabel.AddToClassList("ps-gboard-event-choice-label");
   button.Add(choiceLabel);
   var summary=new Label(EffectSummary(choice)){pickingMode=PickingMode.Ignore};
   summary.AddToClassList("ps-gboard-event-choice-summary");
   button.Add(summary);
   choices.Add(button);
  }
  copy.Add(choices);
  panel.Add(copy);
  gridBoardEventOverlay.Add(panel);
  gridBoardRoot.Add(gridBoardEventOverlay);
 }

 void OpenGridBoardEventPopup(){
  if(gridBoardRoot==null||gridBoardEventOpen)return;
  // Rebuild from the pending event so authored title, illustration and choices
  // always match the cell that halted the route.
  BuildGridBoardEventOverlay();
  if(gridBoardEventOverlay==null)return;
  gridBoardEventOpen=true;
  gridBoardEventOverlay.style.display=DisplayStyle.Flex;
  gridBoardEventOverlay.BringToFront();
 }

 void ResolveGridBoardEvent(int choice){
  if(!gridBoardEventOpen)return;
  gridBoardEventOpen=false;
  if(gridBoardEventOverlay!=null)
   gridBoardEventOverlay.style.display=DisplayStyle.None;
  game.UiResolveEvent(choice);
  // The event resolves without leaving the grid; movement resumes next tick.
  ForceRefreshScreen();
 }

 void TickGridBoard(){
  if(!gridBoardBuilt)return;
  var run=game.UiGridBoard;
  if(run==null)return;
  if(game.UiBattle!=null)return;
  if(gridBoardEventOpen)return;
  var previousPhase=run.phase;
  int previousPathIndex=run.pathIndex;
  bool previousMoving=run.moving;
  bool routeChanged=run.phase==GridBoardPhase.Run
   &&GridBoardSystem.TickRun(run,Time.unscaledDeltaTime);
  if(run.pendingEvent){
   run.pendingEvent=false;
   game.UiBeginGridEvent();
   OpenGridBoardEventPopup();
   return;
  }
  if(run.pendingBattle){
   run.pendingBattle=false;
   game.UiBeginGridEncounter();
   RefreshGridBoard();
   return;
  }
  if(!routeChanged)return;
  bool presentationChanged=previousPhase!=run.phase
   ||previousPathIndex!=run.pathIndex
   ||previousMoving!=run.moving;
  if(presentationChanged)RefreshGridBoard();
  else UpdateGridBoardMotionVisual(run);
 }
}
}
