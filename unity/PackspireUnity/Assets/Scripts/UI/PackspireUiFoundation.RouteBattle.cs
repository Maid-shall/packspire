using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire {
public sealed partial class PackspireUiFoundation {
 VisualElement routeBattleRoot,routeBattleHand,routeBattleConsumables,routeRewardRoot;
 Label routeBattleLog,routeBattleHeroHud,routeBattleEnemyHud,routeBattleIntent,routeBattleEnergy;
 bool routeBattleUiOpen,routeRewardUiOpen;
 int routeBattleHandSig=int.MinValue,routeBattleConsSig=int.MinValue;

 /// <summary>Drop overlay refs that no longer belong to the live explorationRoot (e.g. after screenRoot.Clear).</summary>
 void InvalidateRouteOverlayUi(){
  routeBattleUiOpen=false;
  routeRewardUiOpen=false;
  routeBattleHandSig=int.MinValue;routeBattleConsSig=int.MinValue;
  routeBattleRoot=null;routeBattleHand=null;routeBattleConsumables=null;routeRewardRoot=null;
  routeBattleLog=null;routeBattleHeroHud=null;routeBattleEnemyHud=null;
  routeBattleIntent=null;routeBattleEnergy=null;
 }

 bool RouteBattleUiAttached=>
  routeBattleRoot!=null
  &&explorationRoot!=null
  &&routeBattleRoot.parent==explorationRoot
  &&routeBattleHand!=null
  &&routeBattleHand.parent!=null;

 bool RouteRewardUiAttached=>
  routeRewardRoot!=null
  &&explorationRoot!=null
  &&routeRewardRoot.parent==explorationRoot;

 // --- Idempotent route encounter presentation API ---

 /// <summary>Attach combat actors + battle HUD to the current explorationRoot. Safe to call repeatedly.</summary>
 public void BeginRouteBattle(){
  if(explorationRoot==null||game==null)return;
  if(game.UiBattle==null)return;
  bool fresh=!RouteBattleUiAttached;
  EnsureRouteBattleUi();
  HideRouteRewardUi();
  explorationRouteStage?.EnterCombat(game.UiBattle.enemy);
  routeBattleUiOpen=true;
  if(routeBattleRoot!=null){
   routeBattleRoot.style.display=DisplayStyle.Flex;
   if(fresh)routeBattleRoot.BringToFront();
  }
  // Full rebuild only on (re)connect or battle start — never every frame.
  RefreshRouteBattleLabels();
  RebuildRouteBattleHand(force:true);
  RebuildRouteConsumables(force:true);
 }

 /// <summary>Show reward overlay on the same 2.5D stage. Safe to call repeatedly.</summary>
 public void EnterRouteReward(){
  if(explorationRoot==null||game==null)return;
  bool needRebuild=!RouteRewardUiAttached||!routeRewardUiOpen;
  EnsureRouteRewardUi();
  routeBattleUiOpen=false;
  if(routeBattleRoot!=null)routeBattleRoot.style.display=DisplayStyle.None;
  explorationRouteStage?.NotifyEnemyDown();
  routeRewardUiOpen=true;
  if(routeRewardRoot==null)return;
  routeRewardRoot.style.display=DisplayStyle.Flex;
  if(needRebuild){
   routeRewardRoot.BringToFront();
   RebuildRouteRewardPanel();
  }
 }

 /// <summary>Apply loot via game, then reset presentation for further exploration.</summary>
 public void FinishRouteReward(string itemId){
  if(game==null)return;
  string name=GameCatalog.Items.TryGetValue(itemId,out var item)?item.name:itemId;
  game.FinishRouteReward(itemId);
  ResetRouteEncounterPresentation();
  ShowToast($"{name}を持ち帰った");
  RefreshExplorationHud();
  RefreshExplorationSketch();
 }

 /// <summary>Cancel combat/reward presentation without claiming loot (defeat, DEV jump, teardown).</summary>
 public void AbortRouteBattle(){
  game?.AbortRouteBattle();
  ResetRouteEncounterPresentation();
 }

 /// <summary>
 /// Hide overlays, exit stage combat framing, drop orphaned VisualElements.
 /// Does not mutate graph / run loot. Idempotent.
 /// </summary>
 public void ResetRouteEncounterPresentation(){
  routeBattleUiOpen=false;
  routeRewardUiOpen=false;
  routeBattleHandSig=int.MinValue;routeBattleConsSig=int.MinValue;
  if(routeBattleRoot!=null){
   if(explorationRoot==null||routeBattleRoot.parent!=explorationRoot){
    routeBattleRoot.RemoveFromHierarchy();
    routeBattleRoot=null;routeBattleHand=null;routeBattleConsumables=null;
    routeBattleLog=null;routeBattleHeroHud=null;routeBattleEnemyHud=null;
    routeBattleIntent=null;routeBattleEnergy=null;
   } else routeBattleRoot.style.display=DisplayStyle.None;
  }
  if(routeRewardRoot!=null){
   if(explorationRoot==null||routeRewardRoot.parent!=explorationRoot){
    routeRewardRoot.RemoveFromHierarchy();
    routeRewardRoot=null;
   } else routeRewardRoot.style.display=DisplayStyle.None;
  }
  explorationRouteStage?.ExitCombat();
  if(explorationMapBuilt)ApplyRouteModeVisibility();
 }

 void EnsureRouteBattleUi(){
  if(explorationRoot==null)return;
  if(RouteBattleUiAttached)return;
  routeBattleUiOpen=false;
  routeBattleHandSig=int.MinValue;routeBattleConsSig=int.MinValue;
  routeBattleRoot?.RemoveFromHierarchy();
  routeBattleRoot=null;routeBattleHand=null;routeBattleConsumables=null;
  routeBattleLog=null;routeBattleHeroHud=null;routeBattleEnemyHud=null;
  routeBattleIntent=null;routeBattleEnergy=null;

  // Root ignores picks so the full-screen battle layer does not steal clicks;
  // only the bottom rail / cards accept input.
  routeBattleRoot=Container("ps-route-battle");
  routeBattleRoot.pickingMode=PickingMode.Ignore;
  routeBattleRoot.style.display=DisplayStyle.None;

  routeBattleHeroHud=new Label(""){pickingMode=PickingMode.Ignore};
  routeBattleHeroHud.AddToClassList("ps-route-battle-hero-hud");
  routeBattleRoot.Add(routeBattleHeroHud);

  routeBattleEnemyHud=new Label(""){pickingMode=PickingMode.Ignore};
  routeBattleEnemyHud.AddToClassList("ps-route-battle-enemy-hud");
  routeBattleRoot.Add(routeBattleEnemyHud);

  routeBattleIntent=new Label(""){pickingMode=PickingMode.Ignore};
  routeBattleIntent.AddToClassList("ps-route-battle-intent");
  routeBattleRoot.Add(routeBattleIntent);

  routeBattleLog=new Label(""){pickingMode=PickingMode.Ignore};
  routeBattleLog.AddToClassList("ps-route-battle-log");
  routeBattleRoot.Add(routeBattleLog);

  var bottom=Container("ps-route-battle-bottom");
  bottom.pickingMode=PickingMode.Position;
  routeBattleConsumables=Container("ps-route-battle-consumables");
  routeBattleConsumables.pickingMode=PickingMode.Position;
  bottom.Add(routeBattleConsumables);
  routeBattleHand=Container("ps-route-battle-hand");
  routeBattleHand.pickingMode=PickingMode.Position;
  bottom.Add(routeBattleHand);
  var rail=Container("ps-route-battle-rail");
  rail.pickingMode=PickingMode.Position;
  routeBattleEnergy=new Label(""){pickingMode=PickingMode.Ignore};
  routeBattleEnergy.AddToClassList("ps-route-battle-energy");
  rail.Add(routeBattleEnergy);
  var end=PackspireUiFactory.Button("ターン終了",()=>{
   explorationRouteStage?.NotifyEnemyAttack();
   explorationRouteStage?.NotifyHeroHit();
   if(game.UiRouteBattleEndTurn()){
    ResetRouteEncounterPresentation();
    RefreshExplorationHud();
   } else {
    RefreshRouteBattleLabels();
    RebuildRouteBattleHand(force:true);
    RebuildRouteConsumables(force:true);
   }
  });
  end.AddToClassList("ps-route-battle-end");
  rail.Add(end);
  bottom.Add(rail);
  routeBattleRoot.Add(bottom);
  explorationRoot.Add(routeBattleRoot);
  routeBattleRoot.BringToFront();
 }

 void EnsureRouteRewardUi(){
  if(explorationRoot==null)return;
  if(RouteRewardUiAttached)return;
  routeRewardUiOpen=false;
  routeRewardRoot?.RemoveFromHierarchy();
  routeRewardRoot=null;
  routeRewardRoot=Container("ps-route-reward");
  routeRewardRoot.pickingMode=PickingMode.Ignore;
  routeRewardRoot.style.display=DisplayStyle.None;
  explorationRoot.Add(routeRewardRoot);
  routeRewardRoot.BringToFront();
 }

 void RebuildRouteRewardPanel(){
  if(routeRewardRoot==null)return;
  routeRewardRoot.Clear();
  var panel=Container("ps-route-reward-panel");
  panel.pickingMode=PickingMode.Position;
  panel.Add(new Label("戦闘報酬"){pickingMode=PickingMode.Ignore});
  panel.Add(new Label("同じ地点で戦利品を選んでください"){pickingMode=PickingMode.Ignore});
  foreach(var id in RewardIds()){
   string value=id;
   var item=GameCatalog.Items[value];
   var btn=PackspireUiFactory.Button($"{item.name}\n{ItemTypeLabel(item.type)} / {item.cells.Length}マス",()=>FinishRouteReward(value));
   btn.AddToClassList("ps-route-reward-choice");
   panel.Add(btn);
  }
  routeRewardRoot.Add(panel);
 }

 /// <summary>Labels / meters only — safe every frame if needed. Does not rebuild buttons.</summary>
 void RefreshRouteBattleLabels(){
  if(!RouteBattleUiAttached)return;
  var run=game?.UiRun;
  var battle=game?.UiBattle;
  if(run==null||battle==null)return;
  var dungeon=GameCatalog.Dungeons.First(x=>x.id==run.dungeon);
  int intent=battle.enemy.damages[battle.move%battle.enemy.damages.Length]+dungeon.damage;
  string statuses=FormatStatuses(run.statuses);
  string enemySt=FormatStatuses(battle.enemyStatuses);
  if(routeBattleHeroHud!=null)routeBattleHeroHud.text=$"HP {run.hp}/{run.maxHp}\n防御 {run.block}\n{statuses}";
  if(routeBattleEnemyHud!=null)routeBattleEnemyHud.text=$"{battle.enemy.name}\nHP {Mathf.Max(0,battle.enemyHp)}/{battle.enemyMaxHp}\n防御 {battle.enemyBlock}\n{enemySt}";
  if(routeBattleIntent!=null)routeBattleIntent.text=$"次行動  攻撃 {intent}";
  if(routeBattleLog!=null)routeBattleLog.text=battle.log??"";
  if(routeBattleEnergy!=null)routeBattleEnergy.text=$"EN {run.energy}/3   山札 {run.draw.Count}   捨て札 {run.discard.Count}";
 }

 void RebuildRouteBattleHand(bool force=false){
  if(!RouteBattleUiAttached||routeBattleHand==null)return;
  var run=game?.UiRun;
  if(run==null)return;
  int sig=HandSignature(run);
  if(!force&&sig==routeBattleHandSig)return;
  routeBattleHandSig=sig;
  routeBattleHand.Clear();
  for(int i=0;i<run.hand.Count;i++){
   int index=i;
   var card=run.hand[i];
   bool ok=card.cost<=run.energy;
   string body=card.text??"";
   if(body.Length>42)body=body.Substring(0,42)+"…";
   var btn=PackspireUiFactory.Button($"[{card.cost}] {card.name}\n{body}",()=>{
    if(!ok)return;
    bool attack=card.type==CardType.Attack;
    bool guard=card.type==CardType.Skill&&card.block>0;
    if(attack){explorationRouteStage?.NotifyHeroAttack();explorationRouteStage?.NotifyEnemyHit();}
    else if(guard)explorationRouteStage?.NotifyHeroGuard();
    if(game.UiRouteBattlePlayCard(index))EnterRouteReward();
    else {
     RefreshRouteBattleLabels();
     RebuildRouteBattleHand(force:true);
     RebuildRouteConsumables(force:true);
    }
   });
   btn.AddToClassList("ps-route-battle-card");
   if(!ok)btn.SetEnabled(false);
   routeBattleHand.Add(btn);
  }
 }

 void RebuildRouteConsumables(bool force=false){
  if(!RouteBattleUiAttached||routeBattleConsumables==null)return;
  var run=game?.UiRun;
  if(run==null)return;
  int sig=ConsumableSignature(run);
  if(!force&&sig==routeBattleConsSig)return;
  routeBattleConsSig=sig;
  routeBattleConsumables.Clear();
  var cap=new Label("消耗品"){pickingMode=PickingMode.Ignore};
  cap.AddToClassList("ps-route-battle-cons-cap");
  routeBattleConsumables.Add(cap);
  for(int i=0;i<run.consumables.Count;i++){
   int index=i;
   string id=run.consumables[i];
   var btn=PackspireUiFactory.Button(ConsumableSystem.Name(id),()=>{
    if(game.UiRouteBattleUseConsumable(index))EnterRouteReward();
    else {
     explorationRouteStage?.NotifyEnemyHit();
     RefreshRouteBattleLabels();
     RebuildRouteBattleHand(force:true);
     RebuildRouteConsumables(force:true);
    }
   });
   btn.AddToClassList("ps-route-battle-cons");
   routeBattleConsumables.Add(btn);
  }
 }

 /// <summary>Compatibility: rebuild labels + hands after a state-changing action.</summary>
 void RefreshRouteBattleUi(){
  if(!routeBattleUiOpen&&game?.CurrentRoutePresentationMode!=RoutePresentationMode.RouteCombat)return;
  if(!RouteBattleUiAttached){
   EnsureRouteBattleUi();
   if(!RouteBattleUiAttached)return;
   routeBattleRoot.style.display=DisplayStyle.Flex;
   routeBattleRoot.BringToFront();
  }
  RefreshRouteBattleLabels();
  RebuildRouteBattleHand(force:true);
  RebuildRouteConsumables(force:true);
 }

 static int HandSignature(RunState run){
  unchecked{
   int h=run.hand.Count*397^run.energy*17;
   for(int i=0;i<run.hand.Count;i++){
    var c=run.hand[i];
    h=h*31+(c?.name?.GetHashCode()??0)^(c?.cost??0)*13^i;
   }
   return h;
  }
 }

 static int ConsumableSignature(RunState run){
  unchecked{
   int h=run.consumables.Count*223;
   for(int i=0;i<run.consumables.Count;i++)h=h*31+(run.consumables[i]?.GetHashCode()??0)^i;
   return h;
  }
 }

 static string FormatStatuses(System.Collections.Generic.List<StatusState> list){
  if(list==null||list.Count==0)return "状態なし";
  return string.Join(" ",list.Take(4).Select(s=>$"{s.type}:{s.amount}"));
 }

 /// <summary>
 /// Per-frame sync: reconnect / visibility / actors only.
 /// Never rebuilds card buttons here (that kills in-flight clicks).
 /// </summary>
 void TickRouteBattleSync(){
  if(!explorationMapBuilt||explorationRoot==null||game==null)return;
  game.UiSyncRoutePresentationMode();
  var mode=game.CurrentRoutePresentationMode;
  if(mode==RoutePresentationMode.RouteCombat){
   if(!routeBattleUiOpen||!RouteBattleUiAttached)BeginRouteBattle();
   else {
    if(explorationRouteStage!=null&&!explorationRouteStage.InCombat&&game.UiBattle!=null)
     explorationRouteStage.EnterCombat(game.UiBattle.enemy);
    if(routeBattleRoot.style.display!=DisplayStyle.Flex)
     routeBattleRoot.style.display=DisplayStyle.Flex;
    // Labels every frame; hand/consumables only when their signatures change
    // (never Clear mid-click while content is unchanged).
    RefreshRouteBattleLabels();
    RebuildRouteBattleHand(force:false);
    RebuildRouteConsumables(force:false);
   }
  } else if(mode==RoutePresentationMode.RouteReward){
   if(!routeRewardUiOpen||!RouteRewardUiAttached)EnterRouteReward();
   else if(routeRewardRoot.style.display!=DisplayStyle.Flex)
    routeRewardRoot.style.display=DisplayStyle.Flex;
  } else {
   if(routeBattleUiOpen&&RouteBattleUiAttached)routeBattleRoot.style.display=DisplayStyle.None;
   routeBattleUiOpen=false;
   if(routeRewardUiOpen&&RouteRewardUiAttached)routeRewardRoot.style.display=DisplayStyle.None;
   routeRewardUiOpen=false;
  }
 }

 void ShowRouteBattleUi()=>BeginRouteBattle();
 void ShowRouteRewardUi()=>EnterRouteReward();
 void HideRouteRewardUi(){
  routeRewardUiOpen=false;
  if(routeRewardRoot!=null)routeRewardRoot.style.display=DisplayStyle.None;
 }
}
}
