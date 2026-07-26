using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire {
public sealed partial class PackspireUiFoundation {
// Exploration/combat hands, previews, card population, and battle feedback.
 void SetGridHandOpen(bool open){
  if(!gridBoardBuilt||gridBoardHandRoot==null)return;
  if(gridBoardCombatMode)open=true;
  if(gridBoardHandOpen==open)return;
  gridBoardHandOpen=open;
  SyncGridHandChrome();
  var run=game.UiGridBoard;
  if(run!=null)RebuildGridHand(run);
 }

 void FocusGridHandCard(Button card){
  if(gridBoardHoverCard==card)return;
  if(gridBoardHoverCard!=null)gridBoardHoverCard.RemoveFromClassList("ps-gboard-fan-focus");
  if(gridBoardHoverPreview!=null){
   gridBoardHoverPreview.RemoveFromHierarchy();
   gridBoardHoverPreview=null;
  }
  gridBoardHoverCard=card;
  gridBoardHoverCard.AddToClassList("ps-gboard-fan-focus");
  if(card.userData is not CardInstance data)return;
  ShowGridExplorationCardPreview(data,false);
 }

 void ClearGridHandFocus(){
  if(gridBoardHoverCard!=null)gridBoardHoverCard.RemoveFromClassList("ps-gboard-fan-focus");
  gridBoardHoverCard=null;
  if(gridBoardHoverPreview!=null){
   gridBoardHoverPreview.RemoveFromHierarchy();
   gridBoardHoverPreview=null;
  }
  if(gridBoardCombatMode)ShowGridCombatCardPreview(null);
  else RefreshGridSelectedCard(game.UiGridBoard);
 }

 void ShowGridCombatCardPreview(CardInstance card,bool affordable=true){
  if(gridBoardCombatCardPreview==null)return;
  gridBoardCombatCardPreview.Clear();
  if(card==null||game.UiRun==null){
   var hint=new Label("手札に触れて\n術式を確認"){pickingMode=PickingMode.Ignore};
   hint.AddToClassList("ps-gboard-card-view-empty");
   gridBoardCombatCardPreview.Add(hint);
   return;
  }
  var preview=Container("ps-battle-card ps-gboard-card-view-card");
  preview.pickingMode=PickingMode.Ignore;
  PopulateBattleCard(preview,card,game.UiRun,affordable);
  gridBoardCombatCardPreview.Add(preview);
 }

 void RefreshGridSelectedCard(GridBoardRunState run){
  if(gridBoardSelectedHost==null)return;
  bool showPreview=run!=null&&run.phase==GridBoardPhase.Place&&!string.IsNullOrEmpty(run.selectedCardUid);
  if(!showPreview||gridBoardCombatMode||game.UiBattle!=null){
   ShowGridExplorationCardPreview(null,false);
   return;
  }
  var card=run!=null&&run.phase==GridBoardPhase.Place?GridBoardSystem.SelectedCard(run):null;
  if(card==null){
   ShowGridExplorationCardPreview(null,false);
   return;
  }
  ShowGridExplorationCardPreview(card,true);
 }

void ShowGridExplorationCardPreview(CardInstance card,bool committed){
  if(gridBoardSelectedHost==null)return;
  if(card!=null)HideGridCellDetail();
  gridBoardSelectedHost.Clear();
  if(card==null){
   gridBoardSelectedHost.style.display=DisplayStyle.None;
   return;
  }
  // Inline size/pos so PackspireBattle.uss (.ps-battle-card 168x236) cannot win.
  const float cardW=252f;
  const float cardH=354f;
  gridBoardSelectedHost.style.display=DisplayStyle.Flex;
  gridBoardSelectedHost.style.backgroundColor=Color.clear;
  gridBoardSelectedHost.style.position=Position.Absolute;
  // Planning preview overlays the right edge of the map; exploration has no
  // persistent dossier column competing for this space.
  gridBoardSelectedHost.style.right=34;
  gridBoardSelectedHost.style.top=82;
  gridBoardSelectedHost.style.width=cardW;
  gridBoardSelectedHost.style.height=cardH;
  VisualElement preview;
  if(committed){
   preview=new Button(()=>{
    if(game.UiGridBoard!=null)game.UiGridBoard.selectedCardUid="";
    RefreshGridBoard();
   });
  } else {
   preview=new VisualElement{pickingMode=PickingMode.Ignore};
  }
  preview.AddToClassList("ps-battle-card");
  preview.AddToClassList("ps-gboard-selected-card");
  preview.AddToClassList("ps-gboard-side-preview");
  preview.style.position=Position.Relative;
  preview.style.width=cardW;
  preview.style.height=cardH;
  preview.style.minWidth=cardW;
  preview.style.minHeight=cardH;
  preview.style.maxWidth=cardW;
  preview.style.maxHeight=cardH;
  PopulateGridPlaceCard(preview,card);
  gridBoardSelectedHost.Add(preview);
 }

 void RebuildGridHand(GridBoardRunState run){
  if(gridBoardHandRoot==null)return;
  ClearGridHandFocus();
  gridBoardHandRoot.Clear();
  EnsureBattleAssets();

  if(gridBoardCombatMode){
   RebuildGridCombatHand();
   return;
  }

  bool show=run.phase==GridBoardPhase.Place&&run.hand!=null&&run.hand.Count>0;
  gridBoardHandRoot.style.display=show?DisplayStyle.Flex:DisplayStyle.None;
  if(!show){
   gridBoardHandOpen=false;
   SyncGridHandChrome();
   return;
  }
  SyncGridHandChrome();

  var cards=run.hand;
  if(cards==null||cards.Count==0){
   var empty=new Label("手札なし — マスを押して導線へ"){pickingMode=PickingMode.Ignore};
   empty.AddToClassList("ps-gboard-hand-empty");
   gridBoardHandRoot.Add(empty);
   return;
  }

  int count=cards.Count;
  float center=(count-1)*0.5f;
  float spreadDeg=count<=5?6.2f:count==6?7.4f:5.0f;
  float radius=count<=5?110f:count==6?205f:155f;
  float horizontalStep=count<=5?112f:count==6?92f:72f;
  bool open=gridBoardHandOpen;
  float sink=open?0f:GridHandPeekSink;
  var handSlots=new List<(Button button,float depth)>(count);
  for(int i=0;i<count;i++){
   var capture=cards[i];
   bool selected=run.selectedCardUid==capture.slotKey;
   Button button=null;
   button=new Button(()=>{
    if(run.phase==GridBoardPhase.Place){
     run.selectedCardUid=capture.slotKey;
     run.message=$"{capture.name} を選択";
    }
    gridBoardHandOpen=false;
    SyncGridHandChrome();
    RefreshGridBoard();
   });
   button.AddToClassList("ps-battle-card");
   button.AddToClassList("ps-gboard-fan-card");
   button.userData=capture;
   if(!open)button.AddToClassList("ps-gboard-fan-peek");
   if(selected)button.AddToClassList("ps-gboard-fan-selected");
   PopulateGridPlaceCard(button,capture);
   float spreadIndex=i-center;
   float angle=open?spreadIndex*spreadDeg:spreadIndex*2.2f;
   float rad=angle*Mathf.Deg2Rad;
   float arcLift=open?radius*(1f-Mathf.Cos(rad)):0f;
   float span=GridHandCardWidth+(count-1)*horizontalStep;
   float outerInset=Mathf.Max(8f,(GridHandWidth-span)*0.5f);
   float baseRight=(count-1-i)*horizontalStep+outerInset;
   float arcShift=open?radius*Mathf.Sin(rad):spreadIndex*6f;
   button.style.position=Position.Absolute;
   button.style.right=baseRight-arcShift;
   button.style.bottom=arcLift-sink;
   button.style.rotate=new Rotate(new Angle(angle,AngleUnit.Degree));
   button.style.transformOrigin=new TransformOrigin(new Length(50,LengthUnit.Percent),new Length(100,LengthUnit.Percent));
   if(!gridBoardCombatMode){
    button.RegisterCallback<PointerEnterEvent>(_=>FocusGridHandCard(button));
   } else if(open)button.RegisterCallback<PointerEnterEvent>(_=>button.BringToFront());
   handSlots.Add((button,Mathf.Abs(spreadIndex)-(selected?10f:0f)));
  }
  foreach(var slot in handSlots.OrderByDescending(x=>x.depth))
   gridBoardHandRoot.Add(slot.button);
 }

 void RebuildGridCombatHand(){
  var run=game.UiRun;
  gridBoardHandOpen=true;
  SyncGridHandChrome();
  gridBoardHandRoot.style.display=DisplayStyle.Flex;
  if(run?.hand==null||run.hand.Count==0){
   var empty=new Label("手札なし"){pickingMode=PickingMode.Ignore};
   empty.AddToClassList("ps-gboard-hand-empty");
   gridBoardHandRoot.Add(empty);
   ShowGridCombatCardPreview(null);
   AttachGridResolveTray();
   return;
  }
  int count=run.hand.Count;
  float center=(count-1)*0.5f;
  float spreadDeg=count<=5?6.2f:count==6?7.4f:count==7?7.0f:5.0f;
  float radius=count<=5?110f:count==6?205f:count==7?198f:155f;
  float horizontalStep=count<=5?112f:count==6?92f:count==7?80f:72f;
  var handSlots=new List<(Button button,float depth)>(count);
  for(int i=0;i<count;i++){
   int index=i;
    var card=run.hand[index];
    bool affordable=card.cost<=run.energy;
   Button button=null;
    button=new Button(()=>{
     if(battleInputLocked||button==null||game.UiBattle==null)return;
     if(!affordable){
      ShowToast("ENが不足している");
      return;
     }
     int handIndex=game.UiRun.hand.FindIndex(value=>value.slotKey==card.slotKey);
     if(handIndex<0)return;
     battleInputLocked=true;
     if(!game.UiPlayBattleCard(handIndex))ShowToast(game.UiMessage);
     battleInputLocked=false;
     RefreshGridBoard();
    });
   button.AddToClassList("ps-battle-card");
   button.AddToClassList("ps-gboard-fan-card");
    button.userData=card;
    if(!affordable)button.AddToClassList("ps-battle-card-disabled");
   PopulateBattleCard(button,card,run,affordable);
   float spreadIndex=i-center;
   float angle=spreadIndex*spreadDeg;
   float rad=angle*Mathf.Deg2Rad;
   float arcLift=radius*(1f-Mathf.Cos(rad));
   float span=GridHandCardWidth+(count-1)*horizontalStep;
   float outerInset=Mathf.Max(8f,(GridHandWidth-span)*0.5f);
   float baseRight=(count-1-i)*horizontalStep+outerInset;
   float arcShift=radius*Mathf.Sin(rad);
   button.style.position=Position.Absolute;
   button.style.right=baseRight-arcShift;
   button.style.bottom=arcLift;
   button.style.rotate=new Rotate(new Angle(angle,AngleUnit.Degree));
   button.style.transformOrigin=new TransformOrigin(new Length(50,LengthUnit.Percent),new Length(100,LengthUnit.Percent));
    button.RegisterCallback<PointerEnterEvent>(_=>{
     button.BringToFront();
     ShowGridCombatCardPreview(card,affordable);
   });
   handSlots.Add((button,Mathf.Abs(spreadIndex)));
  }
  foreach(var slot in handSlots.OrderByDescending(x=>x.depth))
   gridBoardHandRoot.Add(slot.button);
  ShowGridCombatCardPreview(null);
  AttachGridResolveTray();
 }

 // Card detail and dice results share one fixed right-side reading lane.
 void AttachGridResolveTray(){
  if(gridBoardResolveTray==null)return;
  var host=gridBoardCombatActionView??gridBoardRoot;
  if(host==null)return;
  if(gridBoardResolveTray.parent!=host)host.Add(gridBoardResolveTray);
  gridBoardResolveTray.BringToFront();
 }

 void PlayGridBattleActionFx(BattleActionFx fx){
  if(gridBoardFxLayer==null||gridBoardCombatStage==null)return;
  ShowGridDiceResult(fx);
  if(fx.damageToEnemy>0&&gridBoardCombatEnemyFocus!=null){
   gridBoardCombatEnemyFocus.AddToClassList("ps-gboard-enemy-hit");
   gridBoardCombatEnemyFocus.schedule.Execute(()=>gridBoardCombatEnemyFocus?.RemoveFromClassList("ps-gboard-enemy-hit")).StartingIn(180);
  }
  if(fx.damageToPlayer>0&&gridBoardRoot!=null){
   gridBoardRoot.AddToClassList("ps-gboard-player-hit");
   gridBoardRoot.schedule.Execute(()=>gridBoardRoot?.RemoveFromClassList("ps-gboard-player-hit")).StartingIn(220);
  }
  EnsureBattleAssets();
  int stagger=0;
  void Spawn(string value,Texture2D icon,string tone){
   var floater=Container("ps-battle-floater "+tone);
   floater.pickingMode=PickingMode.Ignore;
   if(icon!=null){
    var iconImg=new Image{image=icon,scaleMode=ScaleMode.ScaleToFit,pickingMode=PickingMode.Ignore};
    iconImg.AddToClassList("ps-battle-floater-icon-art");
    floater.Add(iconImg);
   }
   var valueLabel=new Label(value){pickingMode=PickingMode.Ignore};
   valueLabel.AddToClassList("ps-battle-floater-value");
   floater.Add(valueLabel);
   gridBoardFxLayer.Add(floater);
   int serial=++gridBoardFloaterSerial;
   float jitterX=(serial%5-2)*22f;
   float jitterY=(serial%3)*12f;
   floater.style.position=Position.Absolute;
   floater.style.left=Length.Percent(42);
   floater.style.top=Length.Percent(28);
   floater.style.translate=new Translate(jitterX,jitterY);
   floater.schedule.Execute(()=>floater.AddToClassList("ps-battle-floater-pop")).StartingIn(Mathf.Max(16,stagger));
   floater.schedule.Execute(()=>{if(floater.parent!=null)floater.AddToClassList("ps-battle-floater-out");}).StartingIn(Mathf.Max(16,stagger)+420);
   floater.schedule.Execute(()=>floater.RemoveFromHierarchy()).StartingIn(Mathf.Max(16,stagger)+980);
   stagger+=70;
  }
  if(fx.damageToEnemy>0)Spawn(fx.damageToEnemy.ToString(),battleIconDamage,"ps-battle-floater-damage");
  if(fx.damageToPlayer>0){
   var damageIcon=battleIconClaw!=null?battleIconClaw:battleIconDamage;
   Spawn(fx.damageToPlayer.ToString(),damageIcon,"ps-battle-floater-damage");
  }
  if(fx.blockGained>0)Spawn("+"+fx.blockGained,battleIconBlock,"ps-battle-floater-block");
  if(fx.healGained>0)Spawn("+"+fx.healGained,battleIconHeal,"ps-battle-floater-heal");
  if(fx.energyGained>0)Spawn("+"+fx.energyGained,battleIconEnergy,"ps-battle-floater-energy");
  if(fx.selfDamage>0)Spawn(fx.selfDamage.ToString(),battleIconDamage,"ps-battle-floater-self");
  if(fx.damageToEnemy<=0&&fx.damageToPlayer<=0&&fx.blockGained<=0&&fx.healGained<=0&&fx.energyGained<=0&&fx.selfDamage<=0){
   if(fx.cardType==CardType.Power)Spawn("強化",battleIconEnergy,"ps-battle-floater-power");
   else if(fx.cardType==CardType.Skill)Spawn("発動",battleIconBlock,"ps-battle-floater-skill");
  }
 }

 void PopulateGridPlaceCard(VisualElement slot,CardInstance card){
  // Skill frame (combat-card-01) for place cards — same chrome as battle.
  Texture2D frame=gridBoardCardFrames!=null&&gridBoardCardFrames.Length>1?gridBoardCardFrames[1]:null;
  if(frame==null&&gridBoardCardFrames!=null&&gridBoardCardFrames.Length>0)frame=gridBoardCardFrames[0];
  if(frame!=null){
   slot.style.backgroundImage=new StyleBackground(frame);
   PackspireUiFactory.ApplyBackgroundScaleMode(slot,ScaleMode.StretchToFill);
  }
  var cost=new Label(card.cost.ToString()){pickingMode=PickingMode.Ignore};
  cost.AddToClassList("ps-battle-card-cost");
  slot.Add(cost);
  var illustration=Container("ps-battle-card-art");
  var glyph=new Label(GridPlaceGlyph(card)){pickingMode=PickingMode.Ignore};
  glyph.AddToClassList("ps-gboard-card-glyph");
  illustration.Add(glyph);
  slot.Add(illustration);
  var name=new Label(card.name){pickingMode=PickingMode.Ignore};
  name.AddToClassList("ps-battle-card-name");
  slot.Add(name);
  var body=new Label(card.text){pickingMode=PickingMode.Ignore};
  body.AddToClassList("ps-battle-card-text");
  slot.Add(body);
  var foot=Container("ps-battle-card-foot");
  var source=new Label("配置"){pickingMode=PickingMode.Ignore};
  source.AddToClassList("ps-battle-card-source");
  foot.Add(source);
  var tag=new Label("GRID"){pickingMode=PickingMode.Ignore};
  tag.AddToClassList("ps-battle-card-durability");
  foot.Add(tag);
  slot.Add(foot);
 }

 static string GridPlaceGlyph(CardInstance card){
  if(card==null)return "◆";
  if(card.id.Contains("lamp")||card.name.Contains("灯り"))return "灯";
  if(card.id.Contains("fog")||card.name.Contains("霧"))return "霧";
  if(card.id.Contains("seal")||card.name.Contains("封鎖"))return "封";
  return "◆";
 }

}
}
