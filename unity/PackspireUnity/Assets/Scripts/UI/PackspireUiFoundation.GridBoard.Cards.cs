using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire {
public sealed partial class PackspireUiFoundation {
// Same-screen battle hand, preview, and battle feedback.
 void ClearGridHandFocus(){
  if(gridBoardCombatMode)ShowGridCombatCardPreview(null);
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
  preview.AddToClassList("ps-docket-expanded");
  preview.pickingMode=PickingMode.Ignore;
  PopulateBattleCard(preview,card,game.UiRun,affordable);
  gridBoardCombatCardPreview.Add(preview);
 }

 void RebuildGridHand(GridBoardRunState run){
  if(gridBoardHandRoot==null)return;
  ClearGridHandFocus();
  gridBoardHandRoot.Clear();
  if(gridBoardCombatMode){
   EnsureBattleAssets();
   RebuildGridCombatHand();
   return;
  }
  gridBoardHandOpen=false;
  gridBoardHandRoot.style.display=DisplayStyle.None;
  SyncGridHandChrome();
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
    bool affordable=!card.unplayable&&card.cost<=run.energy;
   Button button=null;
    button=new Button(()=>{
     if(battleInputLocked||button==null||game.UiBattle==null)return;
     if(!affordable){
      ShowToast(card.unplayable?"このカードは直接使用できない":"ENが不足している");
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

}
}
