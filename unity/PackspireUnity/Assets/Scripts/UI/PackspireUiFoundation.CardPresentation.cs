using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire {
public sealed partial class PackspireUiFoundation {
 VisualTreeAsset docketCardTemplate;

 VisualTreeAsset DocketCardTemplate(){
  if(docketCardTemplate==null)
   docketCardTemplate=PackspireResources.Load<VisualTreeAsset>("UI/PackspireDocketCard");
  return docketCardTemplate;
 }

 void ApplyBattleCardPresentation(VisualElement card,CardInstance definition,RunState run){
  card.AddToClassList("ps-docket-card");
  card.AddToClassList("ps-docket-combat");
  string kind=BattleCardPresentationKind(definition,run);
  card.AddToClassList("ps-docket-"+kind);
 }

 void ApplyExplorationCardPresentation(VisualElement card,CardInstance definition){
  card.AddToClassList("ps-docket-card");
  card.AddToClassList("ps-docket-explore");
  ExplorationCardDef exploration=null;
  GameCatalog.ExplorationCards.TryGetValue(definition?.id??"",out exploration);
  string kind=ExplorationCardPresentationKind(exploration);
  card.AddToClassList("ps-docket-"+kind);
 }

 void PopulateDocketCard(
  VisualElement slot,CardInstance card,string body,string source,string status,
  string formula,bool affordable,bool exploration,int growthStages=0
 ){
  var template=DocketCardTemplate();
  if(slot==null||card==null||template==null)return;
  template.CloneTree(slot);
  ExplorationCardDef route=null;
  if(exploration)GameCatalog.ExplorationCards.TryGetValue(card.id,out route);
  string kind=slot.ClassListContains("ps-docket-attack")?"attack":
   slot.ClassListContains("ps-docket-defense")?"defense":
   slot.ClassListContains("ps-docket-consumable")?"consumable":
   slot.ClassListContains("ps-docket-immediate")?"immediate":
   slot.ClassListContains("ps-docket-installation")?"installation":
   slot.ClassListContains("ps-docket-curse")?"curse":
   exploration?ExplorationCardPresentationKind(route):"technique";
  string code=DocketTrackingCode(card,exploration);
  string glyph=exploration?ExplorationCardPresentationGlyph(kind):BattleCardPresentationGlyph(kind);
  SetDocketLabel(slot,"docket-receipt-kind",exploration?"経路控":"執行控");
  SetDocketLabel(slot,"docket-receipt-cost",card.cost.ToString());
  SetDocketLabel(slot,"docket-receipt-name",card.name);
  SetDocketLabel(slot,"docket-receipt-code",code);
  SetDocketLabel(slot,"docket-seal-glyph",glyph);
  SetDocketLabel(slot,"docket-main-kind",exploration?"ROUTE / 探索票":"EXECUTION / 戦闘票");
  SetDocketLabel(slot,"docket-main-code",code);
  SetDocketLabel(slot,"docket-main-name",card.name);
  SetDocketLabel(slot,"docket-main-cost",card.cost.ToString());
  SetDocketLabel(slot,"docket-main-formula",string.IsNullOrEmpty(formula)?DocketActionSummary(card,exploration):formula);
  SetDocketLabel(slot,"docket-main-text",body);
  SetDocketLabel(slot,"docket-source",string.IsNullOrEmpty(source)?"REGISTRY ISSUE":source);
  var statusLabel=SetDocketLabel(slot,"docket-status",status);
  if(statusLabel!=null&&!string.IsNullOrEmpty(status)&&
   (status.Contains("LOW")||status.Contains("除外")))statusLabel.AddToClassList("ps-alert");
  SetDocketLabel(slot,"docket-lock",affordable?string.Empty:"LOW EN / 保留");
  slot.EnableInClassList("ps-docket-authorized",affordable);
  slot.EnableInClassList("ps-docket-held",!affordable);
  PopulateDocketGrowth(slot.Q<VisualElement>("docket-growth"),growthStages);
 }

 static Label SetDocketLabel(VisualElement root,string name,string value){
  var label=root?.Q<Label>(name);
  if(label!=null)label.text=value??string.Empty;
  return label;
 }

 static string DocketTrackingCode(CardInstance card,bool exploration){
  string value=card?.id??card?.name??"UNREGISTERED";
  int checksum=17;
  foreach(char character in value)checksum=(checksum*31+character)%1000;
  return $"INF-{(exploration?"R":"E")}{Mathf.Abs(card?.cost??0):00}-{checksum:000}";
 }

 static string DocketActionSummary(CardInstance card,bool exploration){
  if(exploration)return "ROUTE AUTH";
  if(card.damage>0)return DocketDiceFormula(card);
  if(card.block>0)return $"BLOCK {card.block}";
  if(card.heal>0)return $"HEAL {card.heal}";
  if(card.energy>0)return $"ENERGY +{card.energy}";
  return card.type==CardType.Power?"PROTOCOL":"EXECUTE";
 }

 static void PopulateDocketGrowth(VisualElement host,int stageCount){
  if(host==null)return;
  host.Clear();
  host.EnableInClassList("ps-empty",stageCount<2);
  if(stageCount<2)return;
  int count=Mathf.Clamp(stageCount,2,4);
  for(int index=0;index<count;index++){
   var stage=new Label((index+1).ToString()){pickingMode=PickingMode.Ignore};
   stage.AddToClassList("ps-docket__growth-stage");
   if(index==0)stage.AddToClassList("ps-selected");
   host.Add(stage);
  }
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

 VisualElement BuildEquipmentCardPairPreview(ItemInstance item,ItemDef definition,RunState run){
  if(item==null||definition==null)return null;
  string battleId=definition.grantedCards?.FirstOrDefault(
   value=>value!=null&&!string.IsNullOrEmpty(value.battleCardId))?.battleCardId;
  if(string.IsNullOrEmpty(battleId))battleId=definition.cardIds?.FirstOrDefault();
  if(string.IsNullOrEmpty(battleId))battleId=definition.cardId;
  if(string.IsNullOrEmpty(battleId)||!GameCatalog.Cards.TryGetValue(battleId,out var battleDefinition))
   return null;

  var battle=BackpackSystem.FromDef(battleDefinition,definition.name,item.uid);
  string explorationId=definition.grantedCards?.FirstOrDefault(
   value=>value!=null&&value.battleCardId==battleId&&!string.IsNullOrEmpty(value.explorationCardId))?.explorationCardId;
  if(string.IsNullOrEmpty(explorationId))explorationId=definition.explorationCardId;
  battle.explorationCardId=explorationId;

  // A preview can point at a vault or codex item. Never add that item to the
  // active run merely to render its two card faces.
  var previewRun=new RunState {
   role=run?.role??string.Empty,
   inventory=new System.Collections.Generic.List<ItemInstance>{item}
  };

  var pair=Container("ps-equipment-card-pair");
  pair.pickingMode=PickingMode.Ignore;
  pair.Add(PackspireUiFactory.ManagementV6Art(
   PackspireUiFactory.ManagementV6Piece.ArchiveCardTray,
   "ps-management-v6-bg ps-equipment-card-pair-tray"
  ));

  var combatColumn=Container("ps-equipment-card-face ps-equipment-card-face-combat");
  combatColumn.pickingMode=PickingMode.Ignore;
  var combatLabel=new Label("COMBAT"){pickingMode=PickingMode.Ignore};
  combatLabel.AddToClassList("ps-equipment-card-face-label");
  combatColumn.Add(combatLabel);
  var combatCard=Container("ps-battle-card ps-equipment-card-preview");
  combatCard.AddToClassList("ps-docket-expanded");
  combatCard.pickingMode=PickingMode.Ignore;
  PopulateBattleCard(combatCard,battle,previewRun,true);
  combatColumn.Add(combatCard);
  pair.Add(combatColumn);

  var exploreColumn=Container("ps-equipment-card-face ps-equipment-card-face-explore");
  exploreColumn.pickingMode=PickingMode.Ignore;
  var exploreLabel=new Label("EXPLORE"){pickingMode=PickingMode.Ignore};
  exploreLabel.AddToClassList("ps-equipment-card-face-label");
  exploreColumn.Add(exploreLabel);
  var exploreCard=Container("ps-battle-card ps-equipment-card-preview");
  exploreCard.AddToClassList("ps-docket-expanded");
  exploreCard.pickingMode=PickingMode.Ignore;
  var exploreDefinition=BuildEquipmentExplorationCard(explorationId,battle,definition,item.uid);
  PopulateEquipmentExplorationCard(exploreCard,exploreDefinition,item);
  exploreColumn.Add(exploreCard);
  pair.Add(exploreColumn);
 return pair;
 }

 VisualElement BuildEquipmentCardElement(ItemInstance item,ItemDef definition,RunState run,bool exploration){
  if(item==null||definition==null)return null;
  string battleId=definition.grantedCards?.FirstOrDefault(
   value=>value!=null&&!string.IsNullOrEmpty(value.battleCardId))?.battleCardId;
  if(string.IsNullOrEmpty(battleId))battleId=definition.cardIds?.FirstOrDefault();
  if(string.IsNullOrEmpty(battleId))battleId=definition.cardId;
  if(string.IsNullOrEmpty(battleId)||!GameCatalog.Cards.TryGetValue(battleId,out var battleDefinition))
   return null;

  var battle=BackpackSystem.FromDef(battleDefinition,definition.name,item.uid);
  string explorationId=definition.grantedCards?.FirstOrDefault(
   value=>value!=null&&value.battleCardId==battleId&&!string.IsNullOrEmpty(value.explorationCardId))?.explorationCardId;
  if(string.IsNullOrEmpty(explorationId))explorationId=definition.explorationCardId;
  battle.explorationCardId=explorationId;
  var previewRun=new RunState{
   role=run?.role??string.Empty,
   inventory=new System.Collections.Generic.List<ItemInstance>{item}
  };

  var card=new Button(){text=string.Empty,pickingMode=PickingMode.Ignore};
  card.AddToClassList("ps-battle-card");
  card.AddToClassList("ps-docket-expanded");
  card.pickingMode=PickingMode.Ignore;
  if(exploration){
   var explorationCard=BuildEquipmentExplorationCard(explorationId,battle,definition,item.uid);
   PopulateEquipmentExplorationCard(card,explorationCard,item);
  }else{
   PopulateBattleCard(card,battle,previewRun,true);
  }
  return card;
 }

 VisualElement BuildEquipmentCardFacePreview(ItemInstance item,ItemDef definition,RunState run,bool exploration){
  var card=BuildEquipmentCardElement(item,definition,run,exploration);
  if(card==null)return null;
  var face=Container("ps-vault-v9-card-face");
  face.pickingMode=PickingMode.Ignore;
  var label=new Label(exploration?"探索カード":"戦闘カード"){pickingMode=PickingMode.Ignore};
  label.AddToClassList("ps-vault-v9-card-face-label");
  face.Add(label);
  card.AddToClassList("ps-equipment-card-preview");
  card.AddToClassList("ps-vault-v9-card");
  face.Add(card);
  return face;
 }

 static CardInstance BuildEquipmentExplorationCard(string explorationId,CardInstance battle,ItemDef item,string itemUid){
  if(!string.IsNullOrEmpty(explorationId)&&GameCatalog.ExplorationCards.TryGetValue(explorationId,out var definition))
   return new CardInstance{
    id=definition.id,
    name=definition.name,
    text=definition.text,
    cost=definition.cost,
    source=item.name,
    sourceItemUid=itemUid,
    explorationCardId=definition.id
   };
  return new CardInstance{
   id=string.IsNullOrEmpty(explorationId)?battle.id:explorationId,
   name=battle.name,
   text=battle.text,
   cost=battle.cost,
   source=item.name,
   sourceItemUid=itemUid,
   explorationCardId=explorationId
  };
 }

 void PopulateEquipmentExplorationCard(VisualElement slot,CardInstance card,ItemInstance item){
  ApplyExplorationCardPresentation(slot,card);
  GameCatalog.ExplorationCards.TryGetValue(card.id,out var definition);
  int stages=definition?.stages?.Length??0;
  PopulateDocketCard(
   slot,card,card.text,card.source,"GRID / 携行",string.Empty,true,true,stages
  );
 }

 static string DocketDiceFormula(CardInstance card){
  if(card==null||card.damage<=0)return "";
  int modifier=card.damage-7;
  return $"2D6 {(modifier>=0?"+":"−")} {Mathf.Abs(modifier)}";
 }

}
}
