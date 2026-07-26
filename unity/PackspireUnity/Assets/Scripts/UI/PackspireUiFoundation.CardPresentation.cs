using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire {
public sealed partial class PackspireUiFoundation {
 Texture2D demonCombatCardFrame,demonCombatDefenseFrame,demonCombatTechniqueFrame,demonCombatConsumableFrame;
 Texture2D demonExploreCardFrame,demonExploreImmediateFrame,demonExploreInstallationFrame,demonExploreCurseFrame;
 Texture2D demonCombatOverlay,demonCombatDefenseOverlay,demonCombatTechniqueOverlay,demonCombatConsumableOverlay;
 Texture2D demonExploreOverlay,demonExploreImmediateOverlay,demonExploreInstallationOverlay,demonExploreCurseOverlay;

 void EnsureDemonCardAssets(){
  if(demonCombatCardFrame==null)
   demonCombatCardFrame=PackspireResources.Load<Texture2D>("Art/UI/Cards/demon-card-combat-v1");
  if(demonCombatDefenseFrame==null)
   demonCombatDefenseFrame=PackspireResources.Load<Texture2D>("Art/UI/Cards/demon-card-combat-defense-v1");
  if(demonCombatTechniqueFrame==null)
   demonCombatTechniqueFrame=PackspireResources.Load<Texture2D>("Art/UI/Cards/demon-card-combat-technique-v1");
  if(demonCombatConsumableFrame==null)
   demonCombatConsumableFrame=PackspireResources.Load<Texture2D>("Art/UI/Cards/demon-card-combat-consumable-v1");
  if(demonCombatOverlay==null)
   demonCombatOverlay=PackspireResources.Load<Texture2D>("Art/UI/Cards/demon-card-combat-v1-overlay");
  if(demonCombatDefenseOverlay==null)
   demonCombatDefenseOverlay=PackspireResources.Load<Texture2D>("Art/UI/Cards/demon-card-combat-defense-v1-overlay");
  if(demonCombatTechniqueOverlay==null)
   demonCombatTechniqueOverlay=PackspireResources.Load<Texture2D>("Art/UI/Cards/demon-card-combat-technique-v1-overlay");
  if(demonCombatConsumableOverlay==null)
   demonCombatConsumableOverlay=PackspireResources.Load<Texture2D>("Art/UI/Cards/demon-card-combat-consumable-v1-overlay");
  if(demonExploreCardFrame==null)
   demonExploreCardFrame=PackspireResources.Load<Texture2D>("Art/UI/Cards/demon-card-explore-v1");
  if(demonExploreImmediateFrame==null)
   demonExploreImmediateFrame=PackspireResources.Load<Texture2D>("Art/UI/Cards/demon-card-explore-immediate-v1");
  if(demonExploreInstallationFrame==null)
   demonExploreInstallationFrame=PackspireResources.Load<Texture2D>("Art/UI/Cards/demon-card-explore-installation-v1");
  if(demonExploreCurseFrame==null)
   demonExploreCurseFrame=PackspireResources.Load<Texture2D>("Art/UI/Cards/demon-card-explore-curse-v1");
  if(demonExploreOverlay==null)
   demonExploreOverlay=PackspireResources.Load<Texture2D>("Art/UI/Cards/demon-card-explore-v1-overlay");
  if(demonExploreImmediateOverlay==null)
   demonExploreImmediateOverlay=PackspireResources.Load<Texture2D>("Art/UI/Cards/demon-card-explore-immediate-v1-overlay");
  if(demonExploreInstallationOverlay==null)
   demonExploreInstallationOverlay=PackspireResources.Load<Texture2D>("Art/UI/Cards/demon-card-explore-installation-v1-overlay");
  if(demonExploreCurseOverlay==null)
   demonExploreCurseOverlay=PackspireResources.Load<Texture2D>("Art/UI/Cards/demon-card-explore-curse-v1-overlay");
 }

 void ApplyBattleCardPresentation(VisualElement card,CardInstance definition,RunState run){
  EnsureDemonCardAssets();
  card.AddToClassList("ps-demon-card");
  card.AddToClassList("ps-demon-card-combat");
  string kind=BattleCardPresentationKind(definition,run);
  card.AddToClassList("ps-demon-card-"+kind);
  var frame=kind switch {
   "defense"=>demonCombatDefenseFrame,
   "technique"=>demonCombatTechniqueFrame,
   "consumable"=>demonCombatConsumableFrame,
   _=>demonCombatCardFrame
  };
  if(frame!=null){
   card.style.backgroundImage=new StyleBackground(frame);
   PackspireUiFactory.ApplyBackgroundScaleMode(card,ScaleMode.StretchToFill);
  }
 }

 void ApplyExplorationCardPresentation(VisualElement card,CardInstance definition){
  EnsureDemonCardAssets();
  card.AddToClassList("ps-demon-card");
  card.AddToClassList("ps-demon-card-explore");
  ExplorationCardDef exploration=null;
  GameCatalog.ExplorationCards.TryGetValue(definition?.id??"",out exploration);
  string kind=ExplorationCardPresentationKind(exploration);
  card.AddToClassList("ps-demon-card-"+kind);
  var frame=kind switch {
   "immediate"=>demonExploreImmediateFrame,
   "installation"=>demonExploreInstallationFrame,
   "curse"=>demonExploreCurseFrame,
   _=>demonExploreCardFrame
  };
  if(frame!=null){
   card.style.backgroundImage=new StyleBackground(frame);
   PackspireUiFactory.ApplyBackgroundScaleMode(card,ScaleMode.StretchToFill);
  }
  if(exploration?.stages!=null&&exploration.stages.Length>1)
   AddDemonCardGrowthTrack(card,exploration.stages.Length,0);
 }

 void AddBattleDemonCardOverlay(VisualElement card,CardInstance definition,RunState run){
  EnsureDemonCardAssets();
  string kind=BattleCardPresentationKind(definition,run);
  var overlay=kind switch {
   "defense"=>demonCombatDefenseOverlay,
   "technique"=>demonCombatTechniqueOverlay,
   "consumable"=>demonCombatConsumableOverlay,
   _=>demonCombatOverlay
  };
  AddDemonCardTypeMark(card,BattleCardPresentationGlyph(kind),kind);
  AddDemonCardOverlay(card,overlay);
 }

 void AddExplorationDemonCardOverlay(VisualElement card,CardInstance definition){
  EnsureDemonCardAssets();
  GameCatalog.ExplorationCards.TryGetValue(definition?.id??"",out var exploration);
  string kind=ExplorationCardPresentationKind(exploration);
  var overlay=kind switch {
   "immediate"=>demonExploreImmediateOverlay,
   "installation"=>demonExploreInstallationOverlay,
   "curse"=>demonExploreCurseOverlay,
   _=>demonExploreOverlay
  };
  AddDemonCardTypeMark(card,ExplorationCardPresentationGlyph(kind),kind);
  AddDemonCardOverlay(card,overlay);
 }

 static void AddDemonCardOverlay(VisualElement card,Texture2D texture){
  if(card==null||texture==null)return;
  var overlay=new VisualElement{pickingMode=PickingMode.Ignore};
  overlay.AddToClassList("ps-demon-card-overlay");
  overlay.style.backgroundImage=new StyleBackground(texture);
  PackspireUiFactory.ApplyBackgroundScaleMode(overlay,ScaleMode.StretchToFill);
  card.Add(overlay);
 }

 static string BattleCardPresentationKind(CardInstance card,RunState run){
  if(card==null)return "technique";
  var sourceItem=run?.inventory?.FirstOrDefault(value=>value.uid==card.sourceItemUid);
  if(sourceItem!=null&&GameCatalog.Items.TryGetValue(sourceItem.templateId,out var item)&&item.type==ItemType.Supply)
   return "consumable";
  if(card.type==CardType.Attack||card.damage>0)return "attack";
  if(card.type==CardType.Power)return "technique";
  if(card.block>0||card.heal>0)return "defense";
  return "technique";
 }

 static string ExplorationCardPresentationKind(ExplorationCardDef card){
  if(card==null)return "support";
  return card.kind switch {
   ExplorationCardKind.Use=>"immediate",
   ExplorationCardKind.Installation=>"installation",
   ExplorationCardKind.Drawback=>"curse",
   _=>"support"
  };
 }

 static string BattleCardPresentationGlyph(string kind)=>kind switch {
  "attack"=>"爪",
  "defense"=>"翼",
  "consumable"=>"牙",
  _=>"眼"
 };

 static string ExplorationCardPresentationGlyph(string kind)=>kind switch {
  "immediate"=>"迅",
  "installation"=>"育",
  "curse"=>"呪",
  _=>"導"
 };

 static void AddDemonCardTypeMark(VisualElement card,string glyph,string kind){
  var pattern=new Label(DemonCardPattern(kind)){pickingMode=PickingMode.Ignore};
  pattern.AddToClassList("ps-demon-card-pattern");
  pattern.AddToClassList("ps-demon-card-pattern-"+kind);
  card.Add(pattern);
  var mark=new Label(glyph){pickingMode=PickingMode.Ignore};
  mark.AddToClassList("ps-demon-card-type");
  mark.AddToClassList("ps-demon-card-type-"+kind);
  card.Add(mark);
 }

 static string DemonCardPattern(string kind)=>kind switch {
  "attack"=>"///",
  "defense"=>")))",
  "technique"=>"◎",
  "consumable"=>"••",
  "immediate"=>"»»",
  "installation"=>"I II III",
  "curse"=>"××",
  _=>"⌁"
 };

 static string DemonCardDiceFormula(CardInstance card){
  if(card==null||card.damage<=0)return "";
  int modifier=card.damage-7;
  return $"2D6 {(modifier>=0?"+":"−")} {Mathf.Abs(modifier)}";
 }

 static void AddDemonCardGrowthTrack(VisualElement card,int stageCount,int currentStage){
  int count=Mathf.Clamp(stageCount,2,4);
  int active=Mathf.Clamp(currentStage,0,count-1);
  var track=new VisualElement{pickingMode=PickingMode.Ignore};
  track.AddToClassList("ps-demon-card-growth");
  for(int i=count-1;i>=0;i--){
   var stage=new Label((i+1).ToString()){pickingMode=PickingMode.Ignore};
   stage.AddToClassList("ps-demon-card-growth-stage");
   if(i<=active)stage.AddToClassList("ps-demon-card-growth-active");
   track.Add(stage);
  }
  card.Add(track);
 }
}
}
