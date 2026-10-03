using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
namespace Packspire {
public sealed partial class PackspireUiFoundation {
 bool PackingFilterMatch(ItemType type)=>packingEquipFilter switch{
  "weapon"=>type==ItemType.Weapon,"armor"=>type==ItemType.Armor,
  "rune"=>type==ItemType.Rune,"supply"=>type==ItemType.Supply,_=>true,
 };
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


 void BuildFormulaTemplateBrowser(VisualElement right){
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
    packingFormulaBrowserOpen=false;
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
    packingFormulaBrowserOpen=false;
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


 void CloseFormulaPopup(){
  packingFormulaOpen=false;packingFormulaSection="";
  game.UiPackingCapture();BuildPackingAgain();
 }

 sealed class PackingEffectRow {
  public string title,detail,kind="LINK";
  public string[] uids;
  public bool active=true;
 }
 List<PackingEffectRow> PackingEffectRows(RunState run,DeckBuildResult build){
  var result=new List<PackingEffectRow>();
  var formula=build.formula;
  foreach(var a in run.placements)
  foreach(var b in run.placements.Where(x=>string.CompareOrdinal(x.itemUid,a.itemUid)>0&&BackpackSystem.Adjacent(run,a,x))){
   var ia=run.inventory.First(x=>x.uid==a.itemUid);
   var ib=run.inventory.First(x=>x.uid==b.itemUid);
   foreach(var rule in formula.resonance.links.Where(x=>LinkPairMatches(x,ia,ib))){
    result.Add(new PackingEffectRow{
     title=PackingItemName(ia)+" × "+PackingItemName(ib),uids=new[]{ia.uid,ib.uid},
     detail=PackingScopedLinkDetail(rule,ia,ib)
    });
   }
  }
  // Card replacement is applied once per host/rule, not once per neighboring copy.
  foreach(var hostPlacement in run.placements){
   var host=run.inventory.First(x=>x.uid==hostPlacement.itemUid);
   foreach(var upgrade in formula.resonance.upgrades.Where(x=>x.hostTemplate==host.templateId)){
    var neighbors=run.placements.Where(x=>x.itemUid!=host.uid&&BackpackSystem.Adjacent(run,hostPlacement,x))
     .Select(x=>run.inventory.First(i=>i.uid==x.itemUid)).Where(x=>x.templateId==upgrade.neighborTemplate).ToList();
    if(neighbors.Count==0)continue;
    bool emitted=build.candidates.Any(x=>x.sourceItemUid==host.uid&&x.id==upgrade.toCardId);
    var card=GameCatalog.Cards.TryGetValue(upgrade.toCardId,out var definition)?definition.name:upgrade.toCardId;
    result.Add(new PackingEffectRow{
     title=PackingItemName(host)+" → "+card,kind="札変化",active=emitted,
     uids=new[]{host.uid}.Concat(neighbors.Select(x=>x.uid)).ToArray(),
     detail=PackingItemName(neighbors[0])+"の隣接"+(neighbors.Count>1?$"（{neighbors.Count}箇所、変化は1回）":"")+
      (emitted?"":" ／ 現在の生成札には含まれません")
    });
   }
  }
  return result;
 }
 static string PackingItemName(ItemInstance item)=>GameCatalog.Items[item.templateId].name;
 string PackingScopedLinkDetail(ResonanceLinkDef rule,ItemInstance a,ItemInstance b){
  if(!string.IsNullOrEmpty(rule.templateB)){
   var sourceA=a.templateId==rule.templateA?a:b;
   var sourceB=a.templateId==rule.templateB?a:b;
   var parts=new List<string>();
   if(rule.damageBonus>0)parts.Add(PackingItemName(sourceA)+$"の攻撃札 +{rule.damageBonus}");
   if(rule.blockBonus>0)parts.Add(PackingItemName(sourceB)+$"の防御札 +{rule.blockBonus}");
   if(rule.costReduce>0)parts.Add($"両装備の札 コスト −{rule.costReduce}");
   return string.Join(" ／ ",parts);
  }
  var target=rule.typeB.HasValue
   ?(GameCatalog.Items[a.templateId].type==rule.typeB.Value?a:b)
   :(a.templateId==rule.templateA?b:a);
  return PackingItemName(target)+"の札: "+LinkEffectLabel(rule);
 }
 void RefreshPackingEffects(RunState run,DeckBuildResult build){
  var selected=run.inventory.FirstOrDefault(x=>x.uid==selectedPackingUid);
  var rows=PackingEffectRows(run,build);
  var root=packingRootElement;
  var shape=RequireViewElement<VisualElement>(root,"packing-selected-shape");
  shape.Clear();packingRightScrollElement.Clear();
  bool placed=selected!=null&&run.placements.Any(x=>x.itemUid==selected.uid);
  RequireViewElement<Label>(root,"packing-selected-name").text=selected==null?"装備を選択":PackingItemName(selected);
  RequireViewElement<Label>(root,"packing-selected-status").text=selected==null?"一覧か盤上の装備をクリック":$"{(placed?"配置中":"未配置")} ／ {GameCatalog.Items[selected.templateId].cells.Length}マス";
  var description=RequireViewElement<Label>(root,"packing-selected-description");
  description.text=selected==null?"":GameCatalog.Items[selected.templateId].description;
  description.tooltip=description.text;
  RequireViewElement<Label>(root,"packing-selected-rotation").text=selected==null?"":$"{packingRotation*90}° ／ {RotationLabel(build.formula.core.rotation)}";
  RequireViewElement<Button>(root,"packing-rotate").SetEnabled(selected!=null);
  RequireViewElement<Button>(root,"packing-remove").SetEnabled(placed);
  if(selected!=null){
   shape.Add(BuildShapePreview(selected,packingRotation));
   var placement=run.placements.FirstOrDefault(x=>x.itemUid==selected.uid);
   if(placement!=null){
    foreach(var pair in BackpackSystem.Analyze(selected,placement,run).matches.Where(x=>x.Value>0))
     packingRightScrollElement.Add(RiteEffectCard(ElementLabel(pair.Key)+$"一致 +{pair.Value}","寄与",DeliverySealSystem.Name(pair.Key)+"の回数に寄与",true));
   }
   var related=rows.Where(x=>x.uids.Contains(selected.uid)).ToList();
   foreach(var row in related)packingRightScrollElement.Add(PackingEffectView(row));
   foreach(var rule in build.formula.resonance.links){
    if(!PackingRuleRelevant(rule,selected))continue;
    if(placed&&run.placements.Any(x=>x.itemUid!=selected.uid&&BackpackSystem.Adjacent(run,placement,x)&&LinkPairMatches(rule,selected,run.inventory.First(i=>i.uid==x.itemUid))))continue;
    packingRightScrollElement.Add(RiteEffectCard(PackingInactiveTitle(rule,selected),"未成立",PackingInactiveDetail(rule,selected),false));
   }
   if(packingRightScrollElement.childCount==0)packingRightScrollElement.Add(RiteEmptyNote("この装備の関連効果はありません"));
  }else packingRightScrollElement.Add(RiteEmptyNote("選択すると、形状とその装備に関係する効果を確認できます。全体の結果は下に表示します。"));

  packingLinksScrollElement.Clear();
  foreach(var row in rows)packingLinksScrollElement.Add(PackingEffectView(row));
  if(build.stability.runaway)packingLinksScrollElement.Add(RiteEffectCard("安定式","過負荷","現在の生成札には過負荷の補正が適用されています。",true));
  if(rows.Count==0&&!build.stability.runaway)packingLinksScrollElement.Add(RiteEmptyNote("成立中のLINK・札変化はありません"));
  RequireViewElement<Label>(root,"packing-link-summary").text=$"LINK {rows.Count(x=>x.kind=="LINK")}箇所 ／ 札変化 {rows.Count(x=>x.kind=="札変化")} ／ 生成札 {build.candidates.Count}";
  packingColorsScrollElement.Clear();
  var seals=PackingDeliverySeals(run,build);
  foreach(Element element in System.Enum.GetValues(typeof(Element))){
   build.colors.TryGetValue(element,out int matches);
   var seal=seals.FirstOrDefault(x=>x.key=="color-"+element.ToString().ToLowerInvariant());
   int charges=seal?.charges??0,max=seal?.maxCharges??0;
   var card=RiteEffectCard(ElementLabel(element)+$"一致 {matches}マス",charges>0?$"{charges}回":"0回",
    DeliverySealSystem.Name(element)+(max>0?$" ／ 残り{charges}/{max}回":" ／ 未成立")+"\n"+DeliverySealSystem.Effect(element),matches>0);
   card.RegisterCallback<PointerEnterEvent>(_=>HighlightPackingColor(element));
   card.RegisterCallback<PointerLeaveEvent>(_=>HighlightPackingItems(System.Array.Empty<string>()));
   packingColorsScrollElement.Add(card);
  }
  RequireViewElement<Label>(root,"packing-color-summary").text=$"色一致 {build.colors.Values.Sum()}マス ／ 配達印 {seals.Sum(x=>x.charges)}回";
 }
 bool PackingRuleRelevant(ResonanceLinkDef rule,ItemInstance focus)=>focus.templateId==rule.templateA||focus.templateId==rule.templateB||
  (rule.typeB.HasValue&&GameCatalog.Items[focus.templateId].type==rule.typeB.Value)||(!rule.typeB.HasValue&&string.IsNullOrEmpty(rule.templateB));
 string PackingInactiveTitle(ResonanceLinkDef rule,ItemInstance focus){
  string a=GameCatalog.Items.TryGetValue(rule.templateA??"",out var def)?def.name:"装備";
  string b=GameCatalog.Items.TryGetValue(rule.templateB??"",out var other)?other.name:rule.typeB.HasValue?PackingTypeLabel(rule.typeB.Value):"隣の装備";
  return a+" × "+b;
 }
 string PackingInactiveDetail(ResonanceLinkDef rule,ItemInstance focus)=>"隣接すると "+LinkEffectLabel(rule);
 static string PackingTypeLabel(ItemType type)=>type switch{ItemType.Weapon=>"武器",ItemType.Armor=>"防具",ItemType.Rune=>"ルーン",_=>"道具"};
 VisualElement PackingEffectView(PackingEffectRow row){
  var view=RiteEffectCard(row.title,row.kind,row.detail,row.active);
  view.userData=row.uids;
  view.RegisterCallback<PointerEnterEvent>(_=>HighlightPackingItems(row.uids));
  view.RegisterCallback<PointerLeaveEvent>(_=>HighlightPackingItems(System.Array.Empty<string>()));
  view.RegisterCallback<ClickEvent>(evt=>{
   var item=game.UiRun.inventory.FirstOrDefault(x=>x.uid==row.uids[0]);
   if(item==null)return;
   selectedPackingUid=item.uid;
   packingRotation=game.UiRun.placements.FirstOrDefault(x=>x.itemUid==item.uid)?.rotation??0;
   packingRightScrollY=0;BuildPackingAgain();evt.StopPropagation();
  });
  return view;
 }
 void HighlightPackingItems(IEnumerable<string> uids){
  if(packingGridElement==null||game.UiRun==null)return;
  var indices=new HashSet<int>();
  foreach(var p in game.UiRun.placements.Where(x=>uids.Contains(x.itemUid))){
   var item=game.UiRun.inventory.First(x=>x.uid==p.itemUid);
   foreach(var pos in BackpackSystem.Analyze(item,p,game.UiRun).cells)indices.Add(pos.y*BackpackSystem.GridWidth(game.UiRun)+pos.x);
  }
  foreach(var child in packingGridElement.Children())
   if(child.userData is int index)child.EnableInClassList("ps-related",indices.Contains(index));
 }
 void HighlightPackingColor(Element element){
  if(packingGridElement==null||game.UiRun==null)return;
  foreach(var child in packingGridElement.Children()){
   if(child.userData is not int index)continue;
   var placement=PlacementAt(game.UiRun,index);
   child.EnableInClassList("ps-related",placement!=null&&CellElementAt(game.UiRun,placement,index)==element&&StorageFormulaSystem.BoardAt(BackpackSystem.Formula(game.UiRun).core,index)==element);
  }
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



 bool IsLinkActiveAnywhere(RunState run,ResonanceLinkDef link){
  foreach(var a in run.placements)
  foreach(var b in run.placements.Where(x=>string.CompareOrdinal(x.itemUid,a.itemUid)>0&&BackpackSystem.Adjacent(run,a,x))){
   var ia=run.inventory.FirstOrDefault(x=>x.uid==a.itemUid);
   var ib=run.inventory.FirstOrDefault(x=>x.uid==b.itemUid);
   if(ia!=null&&ib!=null&&LinkPairMatches(link,ia,ib))return true;
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


 List<DeliverySealState> PackingDeliverySeals(RunState run,DeckBuildResult build){
  var current=run?.courierRoute?.seals;
  var all=current==null?DeliverySealSystem.Build(run,game.UiMeta,build.colors):DeliverySealSystem.Refresh(current,run,game.UiMeta,build.colors);
  return all.Where(x=>x.available&&!x.roleSignature).ToList();
 }
}
}
