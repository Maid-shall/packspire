using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire {
public sealed partial class PackspireUiFoundation {
// Packing selection, formula details, traits, links, and helper components.
 VisualElement BuildPackingFilterRow(){
  var row=Container("ps-rite-filters");
  void AddFilter(string id,string label){
   var button=PackspireUiFactory.Button(label,()=>{
    if(packingEquipFilter!=id){
     packingEquipFilter=id;
     packingEquipScrollY=0;
    }
    BuildPackingAgain();
   });
   button.AddToClassList("ps-rite-filter");
   if(packingEquipFilter==id)button.AddToClassList("ps-selected");
   row.Add(button);
  }
  AddFilter("","全部");
  AddFilter("weapon","武器");
  AddFilter("armor","防具");
  AddFilter("rune","ルーン");
  AddFilter("supply","道具");
  return row;
 }

 bool PackingFilterMatch(ItemType type)=>packingEquipFilter switch{
  "weapon"=>type==ItemType.Weapon,
  "armor"=>type==ItemType.Armor,
  "rune"=>type==ItemType.Rune,
  "supply"=>type==ItemType.Supply,
  _=>true,
 };

 VisualElement BuildPackingSelectDock(RunState run,ActiveStorageFormula formula){
  var dock=Container("ps-rite-select-dock");
  dock.pickingMode=PickingMode.Position;
  DressRiteFrame(dock);
  var caption=new Label("AUX"){pickingMode=PickingMode.Ignore};
  caption.AddToClassList("ps-rite-select-caption");
  dock.Add(caption);
  var rotate=PackspireUiFactory.Button($"回転\n{packingRotation*90}°",()=>RotateSelectedPacking(formula));
  rotate.AddToClassList("ps-rite-select-btn");
  rotate.AddToClassList("ps-rite-select-btn-primary");
  rotate.focusable=true;
  dock.Add(rotate);
  var clear=PackspireUiFactory.Button("選択解除",()=>{selectedPackingUid="";packingRotation=0;BuildPackingAgain();});
  clear.AddToClassList("ps-rite-select-btn");
  dock.Add(clear);
  bool placed=run.placements.Any(x=>x.itemUid==selectedPackingUid);
  var remove=PackspireUiFactory.Button("外す",()=>{game.UiPackingRemove(selectedPackingUid);selectedPackingUid="";BuildPackingAgain();});
  remove.AddToClassList("ps-rite-select-btn");
  remove.AddToClassList("ps-rite-select-btn-danger");
  remove.SetEnabled(placed);
  dock.Add(remove);
  dock.BringToFront();
  return dock;
 }

 void RotateSelectedPacking(ActiveStorageFormula formula){
  if(string.IsNullOrEmpty(selectedPackingUid)||game.UiRun==null)return;
  int next=StorageFormulaSystem.NextRotation(formula.core.rotation,packingRotation);
  if(next==packingRotation){
   ShowToast(RotationLabel(formula.core.rotation)+"のため、これ以上回せません");
   return;
  }
  if(!TryApplyPackingRotation(selectedPackingUid,next)){
   ShowToast("その向きでは盤に収まりません");
   return;
  }
  BuildPackingAgain();
 }

 bool TryApplyPackingRotation(string uid,int nextRotation){
  var run=game.UiRun;
  if(run==null)return false;
  var item=run.inventory.FirstOrDefault(x=>x.uid==uid);
  if(item==null)return false;
  var placement=run.placements.FirstOrDefault(x=>x.itemUid==uid);
  // Not on the board yet: only the pending orientation changes.
  if(placement==null){
   packingRotation=nextRotation;
   return true;
  }
  if(game.UiPackingPlace(uid,placement.anchor,nextRotation)){
   packingRotation=nextRotation;
   return true;
  }
  // Current anchor cannot hold the rotated shape — search nearby, then whole board.
  int width=BackpackSystem.GridWidth(run);
  int height=BackpackSystem.GridHeight(run);
  int cells=width*height;
  int origin=placement.anchor;
  int ox=origin%width,oy=origin/width;
  for(int dist=0;dist<=width+height;dist++){
   for(int i=0;i<cells;i++){
    int ax=i%width,ay=i/width;
    if(Mathf.Abs(ax-ox)+Mathf.Abs(ay-oy)!=dist)continue;
    if(game.UiPackingPlace(uid,i,nextRotation)){
     packingRotation=nextRotation;
     return true;
    }
  }
  }
  return false;
 }

 void BuildPackingItemDetail(VisualElement right,ItemInstance selected,DeckBuildResult build){
  StorageFormulaSystem.EnsureItemRolled(selected);
  var def=GameCatalog.Items[selected.templateId];
  var run=game.UiRun;
  right.Add(RiteSectionHead("03","選択中の術装"));
  var detailCard=Container("ps-rite-detail-card");
  detailCard.Add(Atlas(game.UiEquipmentArt,ItemUv(selected.templateId),"ps-rite-detail-art"));
  var detailName=new Label(def.name){pickingMode=PickingMode.Ignore};
  detailName.AddToClassList("ps-rite-detail-name");
  detailCard.Add(detailName);
  var detailDesc=new Label(def.description){pickingMode=PickingMode.Ignore};
  detailDesc.AddToClassList("ps-rite-detail-desc");
  detailCard.Add(detailDesc);
  right.Add(detailCard);
  right.Add(RiteSectionHead("","形状"));
  right.Add(BuildShapePreview(selected,packingRotation));
  string elements=string.Join(" · ",def.cells.Select((cell,i)=>ElementLabel(selected.colors!=null&&i<selected.colors.Count?selected.colors[i]:cell.element)));
  right.Add(RiteMetaLine($"属性  {elements}"));
  AddPackingTraitLines(right,run,build,selected);
  AddPackingLinkLines(right,run,build,selected);
 }

 void BuildPackingOverview(VisualElement right,RunState run,DeckBuildResult build){
  right.Add(RiteSectionHead("03","発動効果"));
  AddPackingTraitLines(right,run,build,null);
  AddPackingLinkLines(right,run,build,null);
  if(build.stability!=null&&build.stability.runaway)
   right.Add(RiteEffectCard("安定式", "過負荷", "暴走状態です。安定式を見直してください。", true));
 }

 VisualElement BuildFormulaTemplateCard(ActiveStorageFormula formula)=>BuildFormulaTemplateCommitted(formula);

 void BuildFormulaTemplateBrowser(VisualElement right){
  var head=Container("ps-rite-template-head");
  head.Add(RiteSectionHead("02","術式テンプレート"));
  var add=PackspireUiFactory.Button("新規追加",()=>{
   game.UiPackingCapture();
   packingCardsOpen=false;
   packingFormulaSection="";
   packingTemplateCommitted=false;
   game.UiPackingCreateLoadout();
   packingFormulaOpen=true;
   selectedPackingUid="";
   BuildPackingAgain();
  });
  add.AddToClassList("ps-rite-template-edit");
  head.Add(add);
  right.Add(head);

  var meta=game.UiMeta;
  if(meta?.loadouts==null||meta.loadouts.Count==0){
   right.Add(RiteEmptyNote("テンプレートがありません\n「新規追加」から作成できます"));
   return;
  }
  foreach(var loadout in meta.loadouts){
   LoadoutSystem.EnsureFormulaIds(loadout);
   var entry=loadout;
   var preview=StorageFormulaSystem.Resolve(entry);
   var card=Container("ps-rite-template-list-card");
   if(entry.id==meta.selectedLoadoutId)card.AddToClassList("ps-selected");

   var main=Container("ps-rite-template-list-main");
   var name=new Label(string.IsNullOrEmpty(entry.name)?"無名の術式":entry.name){pickingMode=PickingMode.Ignore};
   name.AddToClassList("ps-rite-template-list-name");
   main.Add(name);
   var sub=new Label($"{preview.core.name} · {preview.conduit.name}"){pickingMode=PickingMode.Ignore};
   sub.AddToClassList("ps-rite-template-list-sub");
   main.Add(sub);
   card.Add(main);

   var actions=Container("ps-rite-template-actions");
   var decide=PackspireUiFactory.Button("決定",()=>{
    game.UiPackingCapture();
    packingTemplateCommitted=true;
    packingFormulaOpen=false;
    selectedPackingUid="";
    game.UiOpenPackingLoadout(entry.id);
    BuildPackingAgain();
   });
   decide.AddToClassList("ps-rite-template-decide");
   actions.Add(decide);
   var edit=PackspireUiFactory.Button("編集",()=>{
    game.UiPackingCapture();
    game.UiOpenPackingLoadout(entry.id);
    packingTemplateCommitted=false;
    packingFormulaSection="";
    packingFormulaOpen=true;
    selectedPackingUid="";
    BuildPackingAgain();
   });
   edit.AddToClassList("ps-rite-template-edit");
   actions.Add(edit);
   card.Add(actions);
   right.Add(card);
  }
 }

 VisualElement BuildFormulaTemplateCommitted(ActiveStorageFormula formula){
  var card=Container("ps-rite-template");
  var head=Container("ps-rite-template-head");
  head.Add(RiteSectionHead("02","使用中の術式"));
  var headActions=Container("ps-rite-template-actions");
  var change=PackspireUiFactory.Button("一覧",()=>{
   game.UiPackingCapture();
   packingTemplateCommitted=false;
   packingFormulaOpen=false;
   BuildPackingAgain();
  });
  change.AddToClassList("ps-rite-template-edit");
  headActions.Add(change);
  var edit=PackspireUiFactory.Button("編集",()=>{
   packingCardsOpen=false;
   packingFormulaSection="";
   packingFormulaOpen=true;
   BuildPackingAgain();
  });
  edit.AddToClassList("ps-rite-template-edit");
  headActions.Add(edit);
  head.Add(headActions);
  card.Add(head);

  var loadoutName=game.UiMeta?.loadouts?.FirstOrDefault(x=>x.id==game.UiMeta.selectedLoadoutId)?.name??"無名の術式";
  var name=new Label(loadoutName){pickingMode=PickingMode.Ignore};
  name.AddToClassList("ps-rite-template-list-name");
  card.Add(name);
  var sub=new Label($"{formula.core.name} · {formula.conduit.name} · {formula.resonance.name} · {formula.stability.name}"){pickingMode=PickingMode.Ignore};
  sub.AddToClassList("ps-rite-template-list-sub");
  card.Add(sub);

  var save=PackspireUiFactory.Button(game.UiPackingAtBase?"この術式を保存":"保存して進む",()=>{
   game.UiPackingSave();
   if(game.UiPackingAtBase){
    ShowToast("術式プリセットを保存しました");
    BuildPackingAgain();
   }
  });
  save.AddToClassList("ps-rite-save");
  save.AddToClassList("ps-rite-template-save");
  card.Add(save);
  return card;
 }

 void CloseFormulaPopup(bool commitTemplate){
  packingFormulaOpen=false;
  packingFormulaSection="";
  if(commitTemplate)packingTemplateCommitted=true;
  game.UiPackingCapture();
  BuildPackingAgain();
 }

 VisualElement BuildFormulaTemplateRow(string kind,string value){
  var row=Container("ps-rite-template-row");
  var kindLabel=new Label(kind){pickingMode=PickingMode.Ignore};
  kindLabel.AddToClassList("ps-rite-template-kind");
  var valueLabel=new Label(value){pickingMode=PickingMode.Ignore};
  valueLabel.AddToClassList("ps-rite-template-value");
  row.Add(kindLabel);
  row.Add(valueLabel);
  return row;
 }

 void AddPackingTraitLines(VisualElement right,RunState run,DeckBuildResult build,ItemInstance focus){
  right.Add(RiteSectionHead("","色特性"));
  var lines=0;
  IEnumerable<ItemInstance> items=focus!=null
   ?new[]{focus}
   :run.placements.Select(p=>run.inventory.FirstOrDefault(x=>x.uid==p.itemUid)).Where(x=>x!=null);
  foreach(var item in items){
   StorageFormulaSystem.EnsureItemRolled(item);
   var trait=StorageFormulaCatalog.Trait(item.traitId);
   if(trait==null){
    if(focus!=null){right.Add(RiteEmptyNote("色特性なし"));lines++;}
    continue;
   }
   int matches=0;
   build.colors.TryGetValue(trait.element,out matches);
   bool placed=run.placements.Any(x=>x.itemUid==item.uid);
   bool active=placed&&matches>=trait.requiredMatches;
   if(focus==null&&!active)continue;
   var def=GameCatalog.Items[item.templateId];
   string head=focus!=null?trait.name:$"{def.name}  /  {trait.name}";
   string state=active?"発動中":placed?$"未発動 {ElementLabel(trait.element)}{matches}/{trait.requiredMatches}":"未配置";
   right.Add(RiteEffectCard(head,state,TraitEffectLabel(trait),active));
   lines++;
  }
  if(lines==0)right.Add(RiteEmptyNote(focus!=null?"色特性なし":"発動中の色特性はありません"));
 }

 void AddPackingLinkLines(VisualElement right,RunState run,DeckBuildResult build,ItemInstance focus){
  right.Add(RiteSectionHead("","隣接 LINK"));
  var formula=build.formula.core!=null?build.formula:BackpackSystem.Formula(run);
  var links=formula.resonance.links??System.Array.Empty<ResonanceLinkDef>();
  var upgrades=formula.resonance.upgrades??System.Array.Empty<ResonanceUpgradeDef>();
  if(links.Length==0&&upgrades.Length==0){
   right.Add(RiteEmptyNote("この共鳴式には隣接LINKがありません"));
   return;
  }

  Placement focusPlacement=focus!=null?run.placements.FirstOrDefault(x=>x.itemUid==focus.uid):null;
  int lines=0;

  foreach(var link in links){
   if(focus!=null&&!LinkOwnedBy(link,focus))continue;
   bool active=focus!=null
    ?IsLinkActiveBeside(run,focus,focusPlacement,link)
    :IsLinkActiveAnywhere(run,link);
   if(focus==null&&!active)continue;
   string state=active?"発動中":"未発動";
   right.Add(RiteEffectCard(link.label,state,LinkEffectLabel(link),active));
   lines++;
  }

  foreach(var upgrade in upgrades){
   // カード変化は host（効果を受ける側）のリンクとしてだけ表示する
   if(focus!=null&&focus.templateId!=upgrade.hostTemplate)continue;
   bool active=focus!=null
    ?IsUpgradeActiveBeside(run,focus,focusPlacement,upgrade)
    :IsUpgradeActiveAnywhere(run,upgrade);
   if(focus==null&&!active)continue;
   string toName=GameCatalog.Cards.TryGetValue(upgrade.toCardId,out var card)?card.name:upgrade.toCardId;
   string hostName=GameCatalog.Items.TryGetValue(upgrade.hostTemplate,out var host)?host.name:upgrade.hostTemplate;
   string neighborName=GameCatalog.Items.TryGetValue(upgrade.neighborTemplate,out var neighbor)?neighbor.name:upgrade.neighborTemplate;
   string state=active?"発動中":"未発動";
   right.Add(RiteEffectCard($"カード変化  {hostName} × {neighborName}",state,$"→ {toName}",active));
   lines++;
  }

  if(lines==0)right.Add(RiteEmptyNote(focus!=null?"この装備が持つ隣接LINKはありません":"発動中の隣接LINKはありません"));
 }

 /// <summary>
 /// LINKの「効果持ち」だけに表示する。
 /// 固有ペア（剣×盾）は双方。templateA×種別／何でも（熾火×武器、結晶×装備）は templateA のみ。
 /// </summary>
 bool LinkOwnedBy(ResonanceLinkDef link,ItemInstance item){
  if(!string.IsNullOrEmpty(link.templateA)&&!string.IsNullOrEmpty(link.templateB))
   return item.templateId==link.templateA||item.templateId==link.templateB;
  if(!string.IsNullOrEmpty(link.templateA))
   return item.templateId==link.templateA;
  return false;
 }

 bool IsLinkActiveBeside(RunState run,ItemInstance focus,Placement placement,ResonanceLinkDef link){
  if(placement==null)return false;
  foreach(var other in run.placements.Where(x=>x.itemUid!=focus.uid&&BackpackSystem.Adjacent(run,placement,x))){
   var neighbor=run.inventory.FirstOrDefault(x=>x.uid==other.itemUid);
   if(neighbor!=null&&LinkPairMatches(link,focus,neighbor))return true;
  }
  return false;
 }

 bool IsLinkActiveAnywhere(RunState run,ResonanceLinkDef link){
  foreach(var a in run.placements)
  foreach(var b in run.placements.Where(x=>string.CompareOrdinal(x.itemUid,a.itemUid)>0&&BackpackSystem.Adjacent(run,a,x))){
   var ia=run.inventory.FirstOrDefault(x=>x.uid==a.itemUid);
   var ib=run.inventory.FirstOrDefault(x=>x.uid==b.itemUid);
   if(ia!=null&&ib!=null&&LinkPairMatches(link,ia,ib))return true;
  }
  return false;
 }

 bool IsUpgradeActiveBeside(RunState run,ItemInstance focus,Placement placement,ResonanceUpgradeDef upgrade){
  if(placement==null)return false;
  foreach(var other in run.placements.Where(x=>x.itemUid!=focus.uid&&BackpackSystem.Adjacent(run,placement,x))){
   var neighbor=run.inventory.FirstOrDefault(x=>x.uid==other.itemUid);
   if(neighbor!=null&&UpgradePairMatches(upgrade,focus.templateId,neighbor.templateId))return true;
  }
  return false;
 }

 bool IsUpgradeActiveAnywhere(RunState run,ResonanceUpgradeDef upgrade){
  foreach(var a in run.placements)
  foreach(var b in run.placements.Where(x=>string.CompareOrdinal(x.itemUid,a.itemUid)>0&&BackpackSystem.Adjacent(run,a,x))){
   var ia=run.inventory.FirstOrDefault(x=>x.uid==a.itemUid);
   var ib=run.inventory.FirstOrDefault(x=>x.uid==b.itemUid);
   if(ia!=null&&ib!=null&&UpgradePairMatches(upgrade,ia.templateId,ib.templateId))return true;
  }
  return false;
 }

 Label RiteStatusLine(string text,bool active){
  var label=new Label(text){pickingMode=PickingMode.Ignore};
  label.AddToClassList("ps-rite-status");
  if(active)label.AddToClassList("ps-rite-status-active");
  return label;
 }

 void DressRiteFrame(VisualElement panel){
  if(panel==null)return;
  panel.Add(RiteTick("ps-rite-tick-tl"));
  panel.Add(RiteTick("ps-rite-tick-tr"));
  panel.Add(RiteTick("ps-rite-tick-bl"));
  panel.Add(RiteTick("ps-rite-tick-br"));
 }

 VisualElement RiteTick(string cornerClass){
  var tick=Container("ps-rite-tick "+cornerClass);
  tick.pickingMode=PickingMode.Ignore;
  return tick;
 }

 VisualElement RiteSectionHead(string index,string title){
  var head=Container("ps-rite-panel-head");
  if(!string.IsNullOrEmpty(index)){
   var idx=new Label(index){pickingMode=PickingMode.Ignore};
   idx.AddToClassList("ps-rite-panel-index");
   head.Add(idx);
  }
  var lab=new Label(title){pickingMode=PickingMode.Ignore};
  lab.AddToClassList("ps-rite-panel-title");
  head.Add(lab);
  var rule=Container("ps-rite-panel-rule");
  rule.pickingMode=PickingMode.Ignore;
  head.Add(rule);
  return head;
 }

 VisualElement RiteEffectCard(string title,string state,string detail,bool active){
  var card=Container("ps-rite-effect");
  if(active)card.AddToClassList("ps-rite-effect-active");
  var top=Container("ps-rite-effect-top");
  var name=new Label(title){pickingMode=PickingMode.Ignore};
  name.AddToClassList("ps-rite-effect-title");
  top.Add(name);
  var badge=new Label(state){pickingMode=PickingMode.Ignore};
  badge.AddToClassList("ps-rite-effect-badge");
  if(active)badge.AddToClassList("ps-rite-effect-badge-on");
  top.Add(badge);
  card.Add(top);
  if(!string.IsNullOrEmpty(detail)){
   var body=new Label(detail){pickingMode=PickingMode.Ignore};
   body.AddToClassList("ps-rite-effect-detail");
   card.Add(body);
  }
  return card;
 }

 VisualElement RiteEmptyNote(string text){
  var label=new Label(text){pickingMode=PickingMode.Ignore};
  label.AddToClassList("ps-rite-empty");
  return label;
 }

 VisualElement RiteMetaLine(string text){
  var label=new Label(text){pickingMode=PickingMode.Ignore};
  label.AddToClassList("ps-rite-meta");
  return label;
 }

 bool LinkPairMatches(ResonanceLinkDef link,ItemInstance a,ItemInstance b){
  var pair=new[]{a.templateId,b.templateId};
  var types=new[]{GameCatalog.Items[pair[0]].type,GameCatalog.Items[pair[1]].type};
  if(!string.IsNullOrEmpty(link.templateA)&&!string.IsNullOrEmpty(link.templateB))
   return pair.Contains(link.templateA)&&pair.Contains(link.templateB);
  if(!string.IsNullOrEmpty(link.templateA)&&link.typeB.HasValue)
   return pair.Contains(link.templateA)&&(types[0]==link.typeB.Value||types[1]==link.typeB.Value);
  if(!string.IsNullOrEmpty(link.templateA))
   return pair.Contains(link.templateA);
  return false;
 }

 bool UpgradePairMatches(ResonanceUpgradeDef upgrade,string templateA,string templateB){
  var pair=new[]{templateA,templateB};
  return pair.Contains(upgrade.hostTemplate)&&pair.Contains(upgrade.neighborTemplate);
 }

 string LinkEffectLabel(ResonanceLinkDef link){
  var parts=new List<string>();
  if(link.damageBonus>0)parts.Add($"攻撃+{link.damageBonus}");
  if(link.blockBonus>0)parts.Add($"防御+{link.blockBonus}");
  if(link.costReduce>0)parts.Add($"コスト-{link.costReduce}");
  return parts.Count==0?"効果あり":string.Join("　",parts);
 }

 VisualElement BuildPackingColorCounters(DeckBuildResult build){
  var bar=Container("ps-rite-color-bar");
  bar.Add(PackingColorChip(Element.Fire,build.colors[Element.Fire]));
  bar.Add(PackingColorChip(Element.Water,build.colors[Element.Water]));
  bar.Add(PackingColorChip(Element.Wind,build.colors[Element.Wind]));
  bar.Add(PackingColorChip(Element.Earth,build.colors[Element.Earth]));
  return bar;
 }

 VisualElement PackingColorChip(Element element,int count){
  var chip=Container("ps-rite-color-chip");
  chip.AddToClassList("ps-element-"+element.ToString().ToLowerInvariant());
  var orbTex=RiteOrbTexture(element);
  if(orbTex!=null){
   var orb=new Image{image=orbTex,scaleMode=ScaleMode.ScaleToFit,pickingMode=PickingMode.Ignore};
   orb.AddToClassList("ps-rite-color-orb");
   orb.AddToClassList("ps-rite-orb-live");
   chip.Add(orb);
  } else {
   var orb=Container("ps-rite-color-orb");
   orb.pickingMode=PickingMode.Ignore;
   chip.Add(orb);
  }
  var value=new Label(count.ToString()){pickingMode=PickingMode.Ignore};
  value.AddToClassList("ps-rite-color-value");
  chip.Add(value);
  return chip;
 }
}
}
