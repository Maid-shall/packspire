using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire {
public sealed partial class PackspireUiFoundation {
 enum BattleFormationPreview {
  Auto,
  Normal,
  Small,
  Large,
  Boss,
  Multiple
 }

 sealed class BattleEnemyPreviewCopy {
  public VisualElement root;
  public VisualElement hpFill;
  public Label hpLabel;
  public VisualElement intentList;
  public Image portrait;
 }

 static readonly string[] BattleFormationClasses={
  "is-formation-normal",
  "is-formation-small",
  "is-formation-large",
  "is-formation-boss",
  "is-formation-multiple"
 };

 readonly List<BattleEnemyPreviewCopy> battleEnemyPreviewCopies=new();
 BattleFormationPreview battleFormationPreview=BattleFormationPreview.Auto;

 void OpenBattleFormationPreview(BattleFormationPreview preview){
  battleFormationPreview=preview;
  DevNavigate(ScreenId.Battle,()=>game.UiDevOpenBattle());
 }

 public void DevOpenBattleFormationPreview(string preview){
  if(!System.Enum.TryParse(preview,true,out BattleFormationPreview parsed))
   parsed=BattleFormationPreview.Normal;
  OpenBattleFormationPreview(parsed);
 }

 BattleFormationPreview ResolveBattleFormationPreview(){
  if(battleFormationPreview!=BattleFormationPreview.Auto)return battleFormationPreview;
  switch(game?.UiBattle?.enemy?.battleFormation??BattleFormationScale.Normal){
   case BattleFormationScale.Small:return BattleFormationPreview.Small;
   case BattleFormationScale.Large:return BattleFormationPreview.Large;
   case BattleFormationScale.Boss:return BattleFormationPreview.Boss;
   default:return BattleFormationPreview.Normal;
  }
 }

 void RefreshBattleFormationPresentation(int rawIntent,bool specialMove,List<EffectSpec> effects,EnemyMoveContent move){
  if(battleRoot==null||battleEnemyLine==null||battleEnemyPortrait==null)return;
  var preview=ResolveBattleFormationPreview();
  foreach(var className in BattleFormationClasses)battleRoot.RemoveFromClassList(className);
  battleRoot.AddToClassList(FormationClass(preview));

  int requiredCopies=preview==BattleFormationPreview.Multiple?2:0;
  EnsureBattleEnemyPreviewCopies(requiredCopies);
  foreach(var copy in battleEnemyPreviewCopies){
   CopyBattlePortrait(battleEnemyPortrait,copy.portrait);
   int hp=Mathf.Max(0,game.UiBattle.enemyHp);
   SetMeter(copy.hpFill,copy.hpLabel,hp,game.UiBattle.enemyMaxHp,$"{hp}/{game.UiBattle.enemyMaxHp}",false);
   RefreshBattlePreviewIntent(copy.intentList,rawIntent,specialMove,effects,move);
  }
 }

 static string FormationClass(BattleFormationPreview preview){
  switch(preview){
   case BattleFormationPreview.Small:return "is-formation-small";
   case BattleFormationPreview.Large:return "is-formation-large";
   case BattleFormationPreview.Boss:return "is-formation-boss";
   case BattleFormationPreview.Multiple:return "is-formation-multiple";
   default:return "is-formation-normal";
  }
 }

 void EnsureBattleEnemyPreviewCopies(int count){
  while(battleEnemyPreviewCopies.Count>count){
   int last=battleEnemyPreviewCopies.Count-1;
   battleEnemyPreviewCopies[last].root.RemoveFromHierarchy();
   battleEnemyPreviewCopies.RemoveAt(last);
  }
  while(battleEnemyPreviewCopies.Count<count){
   var copy=CreateBattleEnemyPreviewCopy();
   battleEnemyPreviewCopies.Add(copy);
   battleEnemyLine.Add(copy.root);
  }
 }

 BattleEnemyPreviewCopy CreateBattleEnemyPreviewCopy(){
  var copy=new BattleEnemyPreviewCopy();
  copy.root=Container("ps-battle-combatant ps-battle-combatant-enemy ps-battle-combatant-preview");
  copy.root.pickingMode=PickingMode.Ignore;

  var hud=Container("ps-battle-hud ps-battle-hud-enemy ps-battle-hud-preview");
  hud.pickingMode=PickingMode.Ignore;
  var vitals=Container("ps-battle-vitals-frame");
  vitals.pickingMode=PickingMode.Ignore;
  var art=Container("ps-battle-vitals-art");
  art.pickingMode=PickingMode.Ignore;
  var track=Container("ps-battle-vitals-track");
  track.pickingMode=PickingMode.Ignore;
  copy.hpFill=Container("ps-battle-vitals-fill");
  copy.hpFill.pickingMode=PickingMode.Ignore;
  copy.hpLabel=new Label{pickingMode=PickingMode.Ignore};
  copy.hpLabel.AddToClassList("ps-battle-vitals-value");
  track.Add(copy.hpFill);
  track.Add(copy.hpLabel);
  vitals.Add(art);
  vitals.Add(track);
  hud.Add(vitals);
  hud.Add(Container("ps-battle-status-row"));
  copy.root.Add(hud);

  copy.intentList=Container("ps-battle-intent-list");
  copy.intentList.pickingMode=PickingMode.Ignore;
  copy.root.Add(copy.intentList);

  var actor=Container("ps-battle-actor ps-battle-actor-enemy");
  actor.pickingMode=PickingMode.Ignore;
  actor.Add(Container("ps-battle-actor-sigil"));
  actor.Add(Container("ps-battle-actor-shadow"));
  copy.portrait=new Image{scaleMode=ScaleMode.ScaleToFit,pickingMode=PickingMode.Ignore};
  copy.portrait.AddToClassList("ps-battle-actor-image");
  actor.Add(copy.portrait);
  copy.root.Add(actor);
  return copy;
 }

 static void CopyBattlePortrait(Image source,Image destination){
  if(source.sprite!=null)destination.sprite=source.sprite;
  else if(source.vectorImage!=null)destination.vectorImage=source.vectorImage;
  else destination.image=source.image;
  destination.uv=source.uv;
  destination.scaleMode=ScaleMode.ScaleToFit;
 }

 void RefreshBattlePreviewIntent(VisualElement host,int rawDamage,bool specialMove,List<EffectSpec> effects,EnemyMoveContent move){
  host.Clear();
  if(rawDamage>0)
   host.Add(CreateBattleIntentEntry(battleIconClaw!=null?battleIconClaw:battleIconDamage,rawDamage.ToString(),"is-attack"));
  else if(specialMove)
   host.Add(CreateBattleIntentEntry(battleIconEnergy,"◆","is-special"));
  if((move?.block??0)>0)
   host.Add(CreateBattleIntentEntry(battleIconBlock,move.block.ToString(),"is-guard"));
  if((move?.heal??0)>0)
   host.Add(CreateBattleIntentEntry(battleIconHeal,move.heal.ToString(),"is-heal"));
  if(effects!=null)foreach(var effect in effects)
   host.Add(CreateBattleIntentEntry(battleIconEnergy,effect.amount.ToString(),"is-status"));
  if(host.childCount==0)
   host.Add(CreateBattleIntentEntry(battleIconEnergy,"?","is-special"));
 }

 VisualElement CreateBattleIntentEntry(Texture2D icon,string value,string toneClass){
  var entry=Container("ps-battle-intent-entry "+toneClass);
  if(icon!=null){
   var image=new Image{image=icon,scaleMode=ScaleMode.ScaleToFit,pickingMode=PickingMode.Ignore};
   image.AddToClassList("ps-battle-intent-entry__icon");
   entry.Add(image);
  }
  var label=new Label(value){pickingMode=PickingMode.Ignore};
  label.AddToClassList("ps-battle-intent-entry__value");
  entry.Add(label);
  return entry;
 }

 void ResetBattleFormationPresentation(){
  foreach(var copy in battleEnemyPreviewCopies)copy.root?.RemoveFromHierarchy();
  battleEnemyPreviewCopies.Clear();
  battleFormationPreview=BattleFormationPreview.Auto;
 }
}
}
