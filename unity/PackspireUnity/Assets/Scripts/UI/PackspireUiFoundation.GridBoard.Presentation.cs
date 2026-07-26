using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire {
public sealed partial class PackspireUiFoundation {
// Exploration HUD refresh, energy, dice presentation, gates, and consumables.
 void RefreshGridBoard(){
  var run=game.UiGridBoard;
  if(!gridBoardBuilt||run==null||gridBoardGrid==null)return;

  // Keep stage mode in sync with live battle state (no ScreenId.Battle hop).
  bool inBattle=game.UiBattle!=null;
  if(inBattle&&!gridBoardCombatMode)EnterGridCombatMode(true);
  else if(!inBattle&&gridBoardCombatMode)EnterGridCombatMode(false);

  // Exploration HUD is progressive disclosure. The map remains unobstructed
  // while idle; selecting a card or beginning a route brings in EN and the
  // reading rails needed to make the decision.
  bool planning=inBattle||!string.IsNullOrEmpty(run.pendingGate)||
   !string.IsNullOrEmpty(run.selectedCardUid)||
   run.phase==GridBoardPhase.Path||run.phase==GridBoardPhase.Run;
  gridBoardRoot.EnableInClassList("ps-gboard-planning",planning);
  gridBoardRoot.EnableInClassList("ps-gboard-idle",!planning);

  string phase=GridBoardSystem.PhaseLabel(run.phase);
  string ink=run.phase==GridBoardPhase.Path||run.phase==GridBoardPhase.Run||run.phase==GridBoardPhase.Done
   ?$"曲がり　{run.turnsUsed}/{run.turnsMax}　·　長さ {Mathf.Max(0,run.path.Count-1)}"
   :$"曲がり上限　{run.turnsMax}";
  string hint=string.IsNullOrEmpty(run.message)
   ?"カードを置いてから、マスや矢印で導線を引く。"
   :run.message;

  if(gridBoardPhaseLabel!=null)gridBoardPhaseLabel.text=inBattle?"戦闘":phase;
  if(gridBoardInkLabel!=null)
   gridBoardInkLabel.text=inBattle?$"勝利数 {game.UiRun?.battlesWon??0}":$"{GridBoardSystem.AreaLabel(run)}　·　{ink}　·　{GridBoardSystem.GrowthSummary(run)}";
  if(gridBoardDoomLabel!=null)
   gridBoardDoomLabel.text=inBattle
    ?$"◆ ROUND {Mathf.Max(1,(game.UiBattle?.move??0)+1):00}"
    :$"◆ TURN {Mathf.Max(1,run.explorationTurn+1):00}";
  if(gridBoardAreaChipLabel!=null){
   gridBoardAreaChipLabel.text=$"◇ 区画 {run.areaIndex+1}/{Mathf.Max(1,run.areaCount)}";
   gridBoardAreaChipLabel.style.display=inBattle?DisplayStyle.None:DisplayStyle.Flex;
  }
  if(gridBoardCurveChipLabel!=null){
   gridBoardCurveChipLabel.text=$"✦ 曲がり {run.turnsUsed}/{Mathf.Max(1,run.turnsMax)}";
   gridBoardCurveChipLabel.style.display=inBattle?DisplayStyle.None:DisplayStyle.Flex;
  }
  gridBoardMapStats?.EnableInClassList("ps-gboard-map-stats-combat",inBattle);
  if(gridBoardTypeLabel!=null)gridBoardTypeLabel.text=inBattle?"戦闘":"封印格子";
  if(gridBoardTitleLabel!=null)
   gridBoardTitleLabel.text=inBattle?(game.UiBattle.enemy?.name??"交戦中"):(string.IsNullOrEmpty(run.pendingGate)?phase:run.pendingGate=="next"?"次区画":"帰還点");
  if(gridBoardStatusLabel!=null)
   gridBoardStatusLabel.text=inBattle?"戦闘中":(string.IsNullOrEmpty(run.pendingGate)?ink:GridBoardSystem.AreaLabel(run));

  if(inBattle){
   RefreshGridCombatStage();
   if(gridBoardBodyLabel!=null)gridBoardBodyLabel.text=DescribeGridBattleIntentHint();
   if(gridBoardHintLabel!=null)gridBoardHintLabel.text="下の扇からカード。EN / SKILL / END TURN は扇の下。";
   if(gridBoardContextActions!=null)gridBoardContextActions.style.display=DisplayStyle.None;
  } else if(!string.IsNullOrEmpty(run.pendingGate)){
   if(gridBoardBodyLabel!=null)
    gridBoardBodyLabel.text=run.pendingGate=="next"
     ?"裂け目の向こうに次の区画がある。進出するか、まだここで探索するか。"
     :"ここから持ち帰れる。帰還するか、まだ探索を続けるか。";
   if(gridBoardHintLabel!=null)
    gridBoardHintLabel.text="下の選択で決める。マス自体は残るので、あとからでも踏める。";
  } else {
   if(gridBoardBodyLabel!=null)gridBoardBodyLabel.text=hint;
   if(gridBoardHintLabel!=null){
    gridBoardHintLabel.text=run.phase switch{
     GridBoardPhase.Place=>"右下のカードを選びマスへ。カードなしでマスを押すと導線開始。ルート終端で手札・EN補充。",
     GridBoardPhase.Path=>"矢印か同じ行／列のマスで壁まで滑走。",
     GridBoardPhase.Run=>"進行中。敵＝戦闘、異＝イベント、次＝次区画、帰＝帰還。",
     _=>"撤退で拠点へ。帰還点からの帰還が正式な持ち帰り。",
    };
   }
  }

  RefreshGridHero();
  RefreshGridEnergyRail(run);
  RefreshGridConsumables();
  RefreshGridGateChoice(run);

  bool showPathContext=!inBattle&&string.IsNullOrEmpty(run.pendingGate)&&run.phase==GridBoardPhase.Path;
  if(gridBoardRoutePalette!=null)
   gridBoardRoutePalette.style.display=showPathContext?DisplayStyle.Flex:DisplayStyle.None;
  if(gridBoardContextActions!=null)
   gridBoardContextActions.style.display=showPathContext?DisplayStyle.Flex:DisplayStyle.None;

  if(gridBoardDirRow!=null){
   bool showDirs=showPathContext;
   gridBoardDirRow.style.display=showDirs?DisplayStyle.Flex:DisplayStyle.None;
   if(showDirs){
    foreach(var child in gridBoardDirRow.Children()){
     if(child is Button btn&&btn.userData is Vector2Int dir){
      bool ok=GridBoardSystem.CanSlide(run,dir);
      btn.SetEnabled(ok);
      btn.EnableInClassList("ps-gboard-dir-hot",ok);
     }
    }
   }
  }

  var piece=GridBoardSystem.PieceVisual(run);
  int pieceX=Mathf.RoundToInt(piece.x);
  int pieceY=Mathf.RoundToInt(piece.y);
  var pathSet=new HashSet<long>();
  for(int i=0;i<run.path.Count;i++)pathSet.Add(CellKey(run.path[i].x,run.path[i].y));

  var preview=new HashSet<long>();
  if(run.phase==GridBoardPhase.Path){
   foreach(var dir in new[]{BoardUp,BoardDown,BoardLeft,BoardRight}){
    foreach(var p in GridBoardSystem.SlidePreview(run,dir))
     preview.Add(CellKey(p.x,p.y));
   }
  }

  foreach(var cell in run.cells){
   if(!gridBoardCells.TryGetValue(CellKey(cell.x,cell.y),out var ve)||ve==null)continue;
   bool visible=GridBoardSystem.IsCurrentlyVisible(run,cell);
   bool discovered=GridBoardSystem.IsDiscovered(run,cell);
   var enemy=GridBoardSystem.EnemyAt(run,cell.x,cell.y);
   string displayPlace=visible&&enemy!=null?"enemy":cell.place;
   bool showPlace=visible||(discovered&&displayPlace!="enemy");
   ve.EnableInClassList("ps-gboard-cell",true);
   ve.EnableInClassList("ps-gboard-visible",visible);
   ve.EnableInClassList("ps-gboard-memory",discovered&&!visible);
   ve.EnableInClassList("ps-gboard-unseen",!discovered);
   ve.EnableInClassList("ps-gboard-void",cell.terrain=="void");
   ve.EnableInClassList("ps-gboard-blocked",cell.terrain=="blocked");
   ve.EnableInClassList("ps-gboard-start",cell.terrain=="start");
   ve.EnableInClassList("ps-gboard-goal",cell.terrain=="goal");
   ve.EnableInClassList("ps-gboard-floor",cell.terrain=="floor");
   ve.EnableInClassList("ps-gboard-lamp",showPlace&&displayPlace=="lamp");
   ve.EnableInClassList("ps-gboard-fog",showPlace&&displayPlace=="fog");
   ve.EnableInClassList("ps-gboard-seal",showPlace&&displayPlace=="seal");
   ve.EnableInClassList("ps-gboard-enemy",visible&&displayPlace=="enemy");
   ve.EnableInClassList("ps-gboard-event",showPlace&&displayPlace=="event");
   ve.EnableInClassList("ps-gboard-next",showPlace&&displayPlace=="next");
   ve.EnableInClassList("ps-gboard-return",showPlace&&displayPlace=="return");
   ve.EnableInClassList("ps-gboard-calamity",showPlace&&displayPlace=="calamity");
   int calamityTier=GridBoardSystem.DoomTier(run);
   ve.EnableInClassList("ps-gboard-calamity-tier-1",showPlace&&displayPlace=="calamity"&&calamityTier==1);
   ve.EnableInClassList("ps-gboard-calamity-tier-2",showPlace&&displayPlace=="calamity"&&calamityTier==2);
   ve.EnableInClassList("ps-gboard-calamity-tier-3",showPlace&&displayPlace=="calamity"&&calamityTier>=3);
   ve.EnableInClassList("ps-gboard-mature",showPlace&&cell.grow>=PackspireContent.Data.balance.gridGrowthThreshold&&
    displayPlace is "lamp" or "fog" or "seal");
   ve.EnableInClassList("ps-gboard-path",pathSet.Contains(CellKey(cell.x,cell.y)));
   ve.EnableInClassList("ps-gboard-preview",preview.Contains(CellKey(cell.x,cell.y))&&!pathSet.Contains(CellKey(cell.x,cell.y)));
   ve.EnableInClassList("ps-gboard-piece",cell.x==pieceX&&cell.y==pieceY);
   ve.EnableInClassList("ps-gboard-goal",false);

   var cellMark=ve.Q<Label>("mark");
   var cellSigil=ve.Q<Label>("sigil");
   if(cellSigil!=null){
    cellSigil.text=!discovered?"":displayPlace=="calamity"?"◆":cell.terrain switch{
     "blocked"=>"✦",
     "start"=>"◈",
     "goal"=>"✧",
     _=>!showPlace?"":displayPlace switch{
      "lamp"=>"✦", "fog"=>"☾", "seal"=>"◇",
      "event"=>"✧", "next"=>"➜", "return"=>"↶", _=>""
     }
    };
   }
   if(cellMark!=null){
    string t=!discovered?"":displayPlace=="calamity"?"刻":cell.terrain switch{
     "start"=>"入",
     "goal"=>"標",
     "blocked"=>"■",
     _=>showPlace?GridBoardSystem.PlaceLabel(displayPlace):"",
    };
     if(showPlace&&cell.grow>0&&(displayPlace is "lamp" or "fog" or "seal"))t=$"{GridBoardSystem.PlaceLabel(displayPlace)}{cell.grow}";
    if(showPlace&&displayPlace=="calamity")t=$"刻{run.doom}";
    if(cell.x==pieceX&&cell.y==pieceY)t=string.IsNullOrEmpty(t)?"●":t+"●";
    cellMark.text=t??"";
   }
  }

  RefreshGridActors(run);

  bool shouldFollow=!inBattle&&run.phase==GridBoardPhase.Run;
  if(shouldFollow!=gridBoardFollowingExplorer){
   gridBoardFollowingExplorer=shouldFollow;
   gridBoardZoom=shouldFollow?1.28f:1f;
   gridBoardPan=Vector2.zero;
   gridBoardLastLayoutPos=new(float.NaN,float.NaN);
  }
  ApplyGridZoomVisual();
  LayoutGridBoardMap();
  RebuildGridHand(run);
  RefreshGridSelectedCard(run);
  RefreshGridResolveTray(inBattle);
 }

 void RefreshGridHero(){
  var runState=game.UiRun;
  var character=CharacterSystem.OfRun(runState);
  if(gridBoardHeroNameLabel!=null)
   gridBoardHeroNameLabel.text=character?.name??"探索者";
  if(runState==null){
   if(gridBoardHpLabel!=null)gridBoardHpLabel.text="体力 —";
   if(gridBoardHpFill!=null)gridBoardHpFill.style.width=Length.Percent(0);
   if(gridBoardHpFill!=null)gridBoardHpFill.style.height=Length.Percent(0);
   if(gridBoardShieldLabel!=null)gridBoardShieldLabel.text="SH 0";
   if(gridBoardShieldFill!=null)gridBoardShieldFill.style.height=Length.Percent(0);
   return;
  }
  if(gridBoardHpLabel!=null)
   gridBoardHpLabel.text=$"HP {runState.hp}/{runState.maxHp}";
  if(gridBoardHpFill!=null){
   float t=runState.maxHp>0?Mathf.Clamp01((float)runState.hp/runState.maxHp):0f;
   gridBoardHpFill.style.width=Length.Percent(100);
   gridBoardHpFill.style.height=Length.Percent(t*100f);
  }
  if(gridBoardShieldLabel!=null)gridBoardShieldLabel.text=$"SH {Mathf.Max(0,runState.block)}";
  if(gridBoardShieldFill!=null){
   float shieldT=runState.maxHp>0?Mathf.Clamp01((float)runState.block/runState.maxHp):0f;
   gridBoardShieldFill.style.height=Length.Percent(shieldT*100f);
  }
 }

 void RefreshGridResolveTray(bool inBattle){
  if(gridBoardResolveTray==null)return;
  bool show=inBattle&&gridBoardDiceResultActive&&Time.unscaledTime<gridBoardDiceResultUntil;
  gridBoardCombatActionView?.EnableInClassList("ps-gboard-action-view-rolling",show);
  if(gridBoardCombatCardPreview!=null)
   gridBoardCombatCardPreview.style.display=show?DisplayStyle.None:DisplayStyle.Flex;
  gridBoardResolveTray.style.display=show?DisplayStyle.Flex:DisplayStyle.None;
  if(!show)return;
  float elapsed=Time.unscaledTime-gridBoardDiceResultStarted;
  int stage=elapsed<.22f?0:elapsed<.46f?1:elapsed<.72f?2:3;
  if(gridBoardResolveFormula!=null)
   gridBoardResolveFormula.text=stage==0
    ?$"{gridBoardDiceSource}  /  ROLLING"
    :$"{gridBoardDiceSource}  /  2D6 {(stage>=3?GridDiceModifierText(gridBoardDiceModifier):"+ ?")}";
  if(gridBoardResolveResult!=null)
   gridBoardResolveResult.text=stage>=3?$"{gridBoardDiceTotal} → {gridBoardDiceDamage} DMG":"判定中…";
  if(gridBoardResolveDice==null)return;
  gridBoardResolveDice.Clear();
  int[] values={gridBoardDieOne,gridBoardDieTwo};
  for(int index=0;index<values.Length;index++){
   bool revealed=stage>=index+1;
   var die=new Label(revealed?values[index].ToString():"?"){pickingMode=PickingMode.Ignore};
   die.AddToClassList("ps-gboard-die");
   if(stage>=2&&gridBoardDieOne==6&&gridBoardDieTwo==6)die.AddToClassList("ps-gboard-die-critical");
   if(stage>=2&&gridBoardDieOne==1&&gridBoardDieTwo==1)die.AddToClassList("ps-gboard-die-fumble");
   gridBoardResolveDice.Add(die);
  }
  var modifier=new Label(stage>=3?GridDiceModifierText(gridBoardDiceModifier):"+ ?"){pickingMode=PickingMode.Ignore};
  modifier.AddToClassList("ps-gboard-die-mod");
  gridBoardResolveDice.Add(modifier);
 }

 static string GridDiceModifierText(int modifier)=>modifier>0?$"+ {modifier}":modifier<0?$"− {Mathf.Abs(modifier)}":"+ 0";

 void ShowGridDiceResult(BattleActionFx fx){
  if(!gridBoardCombatMode||fx.dieOne<=0||fx.dieTwo<=0)return;
  gridBoardDiceResultActive=true;
  gridBoardDiceResultStarted=Time.unscaledTime;
  gridBoardDiceResultUntil=Time.unscaledTime+1.8f;
  int sequence=++gridBoardDiceSequence;
  gridBoardDieOne=fx.dieOne;
  gridBoardDieTwo=fx.dieTwo;
  gridBoardDiceModifier=fx.damageModifier;
  gridBoardDiceTotal=fx.rolledDamage;
  gridBoardDiceDamage=fx.damageToEnemy>0?fx.damageToEnemy:fx.damageToPlayer;
  gridBoardDiceSource=string.IsNullOrEmpty(fx.cardName)?"DAMAGE ROLL":fx.cardName;
  RefreshGridResolveTray(true);
  gridBoardResolveTray?.BringToFront();
  foreach(int delay in new[]{230,470,730,1850})
   gridBoardResolveTray?.schedule.Execute(()=>{
    if(sequence==gridBoardDiceSequence)RefreshGridResolveTray(game.UiBattle!=null);
   }).StartingIn(delay);
 }

 void SyncGridHandChrome(){
  if(gridBoardHandRoot==null)return;
  bool open=gridBoardHandOpen||gridBoardCombatMode;
  gridBoardHandRoot.EnableInClassList("ps-gboard-hand-open",open);
  gridBoardHandRoot.style.left=StyleKeyword.Auto;
  gridBoardHandRoot.style.right=StyleKeyword.Auto;
  gridBoardHandRoot.style.left=Length.Percent(58);
  gridBoardHandRoot.style.marginLeft=-GridHandWidth*0.5f;
  gridBoardHandRoot.style.bottom=gridBoardCombatMode?-72:(open?54:0);
  gridBoardHandRoot.style.width=GridHandWidth;
  // The visible cards stay fixed inside this compact hit strip. The separate
  // hover preview is picking-disabled, so it cannot feed back into layout.
  gridBoardHandRoot.style.height=gridBoardCombatMode?260:(open?344:96);
  gridBoardHandRoot.style.overflow=Overflow.Visible;
  gridBoardHandRoot.style.backgroundColor=Color.clear;
  gridBoardHandRoot.style.borderLeftWidth=0;
  gridBoardHandRoot.style.borderRightWidth=0;
  gridBoardHandRoot.style.borderTopWidth=0;
  gridBoardHandRoot.style.borderBottomWidth=0;
 }

 void RefreshGridEnergyRail(GridBoardRunState board){
  if(gridBoardEnergyLabel==null)return;
  int en,max;
  if(gridBoardCombatMode&&game.UiRun!=null){
   en=game.UiRun.energy;
   max=3;
  } else {
   en=board?.energy??0;
   max=board!=null?Mathf.Max(1,board.energyMax):3;
  }
  var orbs=new System.Text.StringBuilder("EN ");
  for(int i=0;i<max;i++)orbs.Append(i<en?'●':'○');
  orbs.Append($"  {en}/{max}");
  gridBoardEnergyLabel.text=$"EN      {en}/{max}";
  if(gridBoardCombatRail!=null)
   gridBoardCombatRail.style.display=gridBoardCombatMode?DisplayStyle.Flex:DisplayStyle.None;
  if(gridBoardSkillButton!=null){
   gridBoardSkillButton.text=game.UiActiveSkillAvailable?game.UiActiveSkillLabel:"USED";
   gridBoardSkillButton.tooltip=game.UiActiveSkillTooltip;
   gridBoardSkillButton.SetEnabled(gridBoardCombatMode&&game.UiActiveSkillAvailable&&!battleInputLocked);
  }
  if(gridBoardEndTurnButton!=null){
   gridBoardEndTurnButton.tooltip="手札を捨て、敵の次の行動を解決する";
   gridBoardEndTurnButton.SetEnabled(gridBoardCombatMode&&game.UiBattle!=null&&!battleInputLocked);
  }
 }

 void RefreshGridGateChoice(GridBoardRunState run){
  if(gridBoardGateActions==null)return;
  gridBoardGateActions.Clear();
  if(run==null||string.IsNullOrEmpty(run.pendingGate)||gridBoardCombatMode){
   gridBoardGateActions.style.display=DisplayStyle.None;
   return;
  }
  gridBoardGateActions.style.display=DisplayStyle.Flex;
  bool next=run.pendingGate=="next";
  gridBoardGateActions.EnableInClassList("ps-gboard-gate-next",next);
  gridBoardGateActions.EnableInClassList("ps-gboard-gate-return",!next);
  var dialog=Container("ps-gboard-gate-dialog");
  var eyebrow=new Label(next?"AREA PASSAGE":"EXPEDITION RETURN"){pickingMode=PickingMode.Ignore};
  eyebrow.AddToClassList("ps-gboard-gate-eyebrow");
  dialog.Add(eyebrow);
  var head=new Label(next?"次区画への裂け目":"帰還点"){pickingMode=PickingMode.Ignore};
  head.AddToClassList("ps-gboard-gate-title");
  dialog.Add(head);
  var body=new Label(next
   ?"裂け目の向こうへ進むと、この区画には戻れない。"
   :"ここまでの戦利品を持ち帰り、遠征を終了する。"){pickingMode=PickingMode.Ignore};
  body.AddToClassList("ps-gboard-gate-body");
  dialog.Add(body);
  var choices=Container("ps-gboard-gate-choices");
  if(next){
   choices.Add(MakeGridAction("次の区画へ進む",()=>{
    game.UiAdvanceGridArea();
   }));
  } else {
   choices.Add(MakeGridAction("戦利品を持って帰還する",()=>{
    game.UiConfirmGridReturn();
    ForceRefreshScreen();
   }));
  }
  choices.Add(MakeGridAction("まだ探索する",()=>{
   game.UiDeclineGridGate();
   RefreshGridBoard();
  }));
  dialog.Add(choices);
  gridBoardGateActions.Add(dialog);
  gridBoardGateActions.BringToFront();
 }

 void RefreshGridConsumables(){
  if(gridBoardConsumablesRoot==null)return;
  gridBoardConsumablesRoot.Clear();
  var run=game.UiRun;
  if(!gridBoardCombatMode||run==null||run.consumables==null||run.consumables.Count==0){
   gridBoardConsumablesRoot.style.display=DisplayStyle.None;
   return;
  }
  gridBoardConsumablesRoot.style.display=DisplayStyle.Flex;
  for(int i=0;i<run.consumables.Count;i++){
   int index=i;
   string id=run.consumables[index];
   string label=ConsumableSystem.Name(id);
   var button=MakeGridAction(label,()=>{
    if(battleInputLocked||game.UiBattle==null)return;
    if(!game.UiUseBattleConsumable(index))ShowToast(game.UiMessage);
    RefreshGridBoard();
   });
   button.AddToClassList("ps-gboard-consumable");
   gridBoardConsumablesRoot.Add(button);
  }
 }

}
}
