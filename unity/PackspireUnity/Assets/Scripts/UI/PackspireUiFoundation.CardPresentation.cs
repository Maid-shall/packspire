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
  bool affordable,bool exploration,int growthStages=0
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
  SetDocketLabel(slot,"docket-receipt-kind",exploration?"ROUTE":"BATTLE");
  SetDocketLabel(slot,"docket-receipt-cost",card.cost.ToString());
  SetDocketLabel(slot,"docket-seal-glyph",glyph);
  SetDocketLabel(slot,"docket-main-code",code);
  SetDocketLabel(slot,"docket-main-name",card.name);
  PopulateDocketEffectText(slot,string.IsNullOrWhiteSpace(body)?DocketActionSentence(card):body);
  SetDocketLabel(slot,"docket-result",DocketResultSummary(card));
  SetDocketLabel(slot,"docket-source",string.IsNullOrEmpty(source)?"REGISTRY ISSUE":source);
  var statusLabel=SetDocketLabel(slot,"docket-status",status);
  if(statusLabel!=null&&!string.IsNullOrEmpty(status)&&
   (status.Contains("LOW")||status.Contains("除外")))statusLabel.AddToClassList("ps-alert");
  SetDocketLabel(slot,"docket-lock",affordable?string.Empty:"LOW EN / 保留");
  slot.EnableInClassList("ps-docket-authorized",affordable);
  slot.EnableInClassList("ps-docket-held",!affordable);
  var artwork=slot.Q<VisualElement>("docket-art");
  var sprite=exploration?null:BattleCardArtwork(card.id);
  if(artwork!=null&&sprite!=null)artwork.style.backgroundImage=new StyleBackground(sprite);
 }

 internal static Sprite BattleCardArtwork(string id){
  string file=id switch {
   "basicStrike"=>"basic-strike-evidence-v1",
   "basicGuard"=>"basic-guard-evidence-v1",
   "basicTactic"=>"basic-tactic-evidence-v1",
   "slash"=>"judgment-stamp-evidence-v1",
   "guard"=>"dead-letter-guard-evidence-v1",
   "spark"=>"address-designation-evidence-v1",
   "mend"=>"emergency-mend-evidence-v1",
   "stab"=>"strikethrough-evidence-v1",
   "brace"=>"brace-evidence-v1",
   "focus"=>"focus-evidence-v1",
   "bomb"=>"bomb-evidence-v1",
   "pierce"=>"pierce-evidence-v1",
   "parry"=>"misdelivery-parry-evidence-v1",
   "acid"=>"acid-evidence-v1",
   "tailwind"=>"tailwind-evidence-v1",
   "devour"=>"devour-evidence-v1",
   "inferno"=>"inferno-evidence-v1",
   "echoWall"=>"echo-wall-evidence-v1",
   "starBomb"=>"star-bomb-evidence-v1",
   _=>string.Empty
  };
  return string.IsNullOrEmpty(file)?null:PackspireResources.Load<Sprite>("Art/Cards/EvidenceCollage/"+file);
 }

 static Label SetDocketLabel(VisualElement root,string name,string value){
  var label=root?.Q<Label>(name);
  if(label!=null)label.text=value??string.Empty;
  return label;
 }

 static void PopulateDocketEffectText(VisualElement root,string text){
  var host=root?.Q<VisualElement>("docket-main-text");
  if(host==null)return;
  host.Clear();
  string normalized=(text??string.Empty).Replace("\r\n","\n").Replace('\r','\n');
  foreach(string line in normalized.Split('\n')){
   var row=new VisualElement{pickingMode=PickingMode.Ignore};
   row.AddToClassList("ps-docket__effect-row");
   var matches=System.Text.RegularExpressions.Regex.Matches(
    line,@"(?:\d+[dD]\d+(?:\s*[+\-−]\s*\d+)?|[+\-−]?\d+(?:[/.]\d+)?)"
   );
   int cursor=0;
   foreach(System.Text.RegularExpressions.Match match in matches){
    AddDocketEffectRun(row,line.Substring(cursor,match.Index-cursor),false);
    AddDocketEffectRun(row,match.Value,true);
    cursor=match.Index+match.Length;
   }
   AddDocketEffectRun(row,line.Substring(cursor),false);
   if(row.childCount==0)AddDocketEffectRun(row," ",false);
   host.Add(row);
  }
 }

 static void AddDocketEffectRun(VisualElement row,string text,bool emphasized){
  if(row==null||string.IsNullOrEmpty(text))return;
  var run=new VisualElement{pickingMode=PickingMode.Ignore};
  run.AddToClassList("ps-docket__effect-run");
  run.AddToClassList(emphasized?"ps-docket__effect-run--value":"ps-docket__effect-run--copy");
  var backing=new Label(text){pickingMode=PickingMode.Ignore};
  backing.AddToClassList("ps-docket__effect-backing");
  var label=new Label(text){pickingMode=PickingMode.Ignore};
  label.AddToClassList(emphasized?"ps-docket__effect-value":"ps-docket__effect-copy");
  run.Add(backing);
  run.Add(label);
  row.Add(run);
 }

 static string DocketTrackingCode(CardInstance card,bool exploration){
  string value=card?.id??card?.name??"UNREGISTERED";
  int checksum=17;
  foreach(char character in value)checksum=(checksum*31+character)%1000;
  return $"INF-{(exploration?"R":"E")}{Mathf.Abs(card?.cost??0):00}-{checksum:000}";
 }

 static string DocketActionSentence(CardInstance card){
  if(card==null)return string.Empty;
  if(card.damage>0)return $"敵に{DocketDiceFormula(card)}ダメージを与える。";
  if(card.block>0)return $"{card.block}ブロックを得る。";
  if(card.heal>0)return $"HPを{card.heal}回復する。";
  if(card.energy>0)return $"ENを{card.energy}回復する。";
  return card.type==CardType.Power?"常在効果を発動する。":"効果を実行する。";
 }

 static string DocketResultSummary(CardInstance card){
  if(card==null)return string.Empty;
  if(card.damage>0)return $"{DocketDiceFormula(card)}  ダメージ";
  if(card.block>0)return $"{card.block}  防護";
  if(card.heal>0)return $"{card.heal}  HP回復";
  if(card.energy>0)return $"+{card.energy}  EN";
  return card.type==CardType.Power?"常在効果":"補助";
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
  // A preview can point at a vault, shop, or codex item. Never add that item
  // to the active run merely to render its battle ticket.
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

  var sealColumn=Container("ps-equipment-card-face ps-equipment-card-face-seal");
  sealColumn.pickingMode=PickingMode.Ignore;
  var sealLabel=new Label("DELIVERY SEAL"){pickingMode=PickingMode.Ignore};
  sealLabel.AddToClassList("ps-equipment-card-face-label");
  sealColumn.Add(sealLabel);
  sealColumn.Add(EquipmentSealAttributeBadge(definition,"ps-equipment-seal-preview"));
  pair.Add(sealColumn);
 return pair;
 }

 VisualElement BuildEquipmentCardElement(ItemInstance item,ItemDef definition,RunState run){
  if(item==null||definition==null)return null;
  string battleId=definition.grantedCards?.FirstOrDefault(
   value=>value!=null&&!string.IsNullOrEmpty(value.battleCardId))?.battleCardId;
  if(string.IsNullOrEmpty(battleId))battleId=definition.cardIds?.FirstOrDefault();
  if(string.IsNullOrEmpty(battleId))battleId=definition.cardId;
  if(string.IsNullOrEmpty(battleId)||!GameCatalog.Cards.TryGetValue(battleId,out var battleDefinition))
   return null;

  var battle=BackpackSystem.FromDef(battleDefinition,definition.name,item.uid);
  var previewRun=new RunState{
   role=run?.role??string.Empty,
   inventory=new System.Collections.Generic.List<ItemInstance>{item}
  };

  var card=new Button(){text=string.Empty,pickingMode=PickingMode.Ignore};
  card.AddToClassList("ps-battle-card");
  card.AddToClassList("ps-docket-expanded");
  card.pickingMode=PickingMode.Ignore;
  PopulateBattleCard(card,battle,previewRun,true);
  return card;
 }

 VisualElement BuildEquipmentCardFacePreview(ItemInstance item,ItemDef definition,RunState run){
  var card=BuildEquipmentCardElement(item,definition,run);
  if(card==null)return null;
  var face=Container("ps-vault-v9-card-face");
  face.pickingMode=PickingMode.Ignore;
  var label=new Label("戦闘配達票"){pickingMode=PickingMode.Ignore};
  label.AddToClassList("ps-vault-v9-card-face-label");
  face.Add(label);
  card.AddToClassList("ps-equipment-card-preview");
  card.AddToClassList("ps-vault-v9-card");
  face.Add(card);
  return face;
 }

 VisualElement EquipmentSealAttributeBadge(ItemDef item,string className){
  var badge=Container("ps-equipment-seal-attribute "+className);
  badge.AddToClassList("ps-seal-"+item.sealAttribute.ToString().ToLowerInvariant());
  var mark=new Label("印"){pickingMode=PickingMode.Ignore};
  mark.AddToClassList("ps-equipment-seal-attribute__mark");
  badge.Add(mark);
  var copy=Container("ps-equipment-seal-attribute__copy");
  var name=new Label(DeliverySealSystem.Name(item.sealAttribute)){pickingMode=PickingMode.Ignore};
  name.AddToClassList("ps-equipment-seal-attribute__name");
  copy.Add(name);
  var effect=new Label(DeliverySealSystem.Effect(item.sealAttribute)){pickingMode=PickingMode.Ignore};
  effect.AddToClassList("ps-equipment-seal-attribute__effect");
  copy.Add(effect);
  badge.Add(copy);
  return badge;
 }

 static string DocketDiceFormula(CardInstance card){
  if(card==null||card.damage<=0)return "";
  if(card.damageMode==DamageResolutionMode.Fixed)return card.damage.ToString();
  int modifier=card.damage-7;
  return $"2D6 {(modifier>=0?"+":"−")} {Mathf.Abs(modifier)}";
 }

}
}
