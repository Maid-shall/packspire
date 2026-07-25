using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire {
public sealed partial class PackspireUiFoundation {
// Board-local events and the exploration runtime tick.
 void BuildGridBoardEventOverlay(){
  if(gridBoardRoot==null)return;
  gridBoardEventOverlay=Container("ps-gboard-event-overlay");
  gridBoardEventOverlay.style.display=DisplayStyle.None;
  var panel=Container("ps-gboard-event-popup");
  var eyebrow=new Label("UNKNOWN SIGNAL"){pickingMode=PickingMode.Ignore};
  eyebrow.AddToClassList("ps-gboard-event-eyebrow");
  panel.Add(eyebrow);
  var title=new Label("異変を発見"){pickingMode=PickingMode.Ignore};
  title.AddToClassList("ps-gboard-event-title");
  panel.Add(title);
  var body=new Label("足元の封印が脈打っている。進行を止めて、どう対処するか選べ。"){pickingMode=PickingMode.Ignore};
  body.AddToClassList("ps-gboard-event-body");
  panel.Add(body);
  var choices=Container("ps-gboard-event-choices");
  var risk=MakeGridAction("血を捧げる　HP -6 / 24G",()=>ResolveGridBoardEvent(0));
  risk.AddToClassList("ps-gboard-event-choice");
  choices.Add(risk);
  var repair=MakeGridAction("装備を整える　耐久を回復",()=>ResolveGridBoardEvent(1));
  repair.AddToClassList("ps-gboard-event-choice");
  choices.Add(repair);
  var leave=MakeGridAction("立ち去る",()=>ResolveGridBoardEvent(2));
  leave.AddToClassList("ps-gboard-event-choice");
  choices.Add(leave);
  panel.Add(choices);
  gridBoardEventOverlay.Add(panel);
  gridBoardRoot.Add(gridBoardEventOverlay);
 }

 void OpenGridBoardEventPopup(){
  if(gridBoardRoot==null||gridBoardEventOpen)return;
  if(gridBoardEventOverlay==null)BuildGridBoardEventOverlay();
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
   // The board-local overlay can be hidden behind the grid's visual layers.
   // The established Event route is attached to the screen root and returns
   // here after the player chooses, so the route continues from its next cell.
   game.UiBeginGridEvent();
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
