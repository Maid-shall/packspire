using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire {
public sealed partial class PackspireUiFoundation {
// Packing formula/card popups and rite-grid previews.
 VisualElement BuildFormulaPopup(RunState run,ActiveStorageFormula formula){
  var overlay=CloneView("UI/PackspirePackingFormulaPopup","ps-rite-popup-overlay");
  overlay.RegisterCallback<ClickEvent>(evt=>{if(evt.target==overlay)CloseFormulaPopup();});
  RequireViewElement<Button>(overlay,"packing-formula-close").clicked+=CloseFormulaPopup;
  var field=RequireViewElement<TextField>(overlay,"packing-formula-name");
  field.SetValueWithoutNotify(game.UiMeta.loadouts.FirstOrDefault(x=>x.id==game.UiMeta.selectedLoadoutId)?.name??"新規術式");
  field.RegisterValueChangedCallback(evt=>game.UiPackingRenameLoadout(evt.newValue));
  var rail=RequireViewElement<ScrollView>(overlay,"packing-formula-components");
  rail.Add(BuildFormulaAccordion(
   "core","収納核",formula.core.name,formula.core.description,
   StorageFormulaCatalog.Cores.Values.Select(x=>(x.id,x.name,x.description)),
   id=>{game.UiPackingSetCore(id);BuildPackingAgain();},
   formula.core.id));
  rail.Add(BuildFormulaAccordion(
   "conduit","属性導線",formula.conduit.name,formula.conduit.description,
   StorageFormulaCatalog.Conduits.Values.Select(x=>(x.id,x.name,x.description)),
   id=>{game.UiPackingSetConduit(id);BuildPackingAgain();},
   formula.conduit.id));
  rail.Add(BuildFormulaAccordion(
   "resonance","共鳴式",formula.resonance.name,formula.resonance.description,
   StorageFormulaCatalog.Resonances.Values.Select(x=>(x.id,x.name,x.description)),
   id=>{game.UiPackingSetResonance(id);BuildPackingAgain();},
   formula.resonance.id));
  rail.Add(BuildFormulaAccordion(
   "stability","安定式",formula.stability.name,formula.stability.description,
   StorageFormulaCatalog.Stabilities.Values.Select(x=>(x.id,x.name,x.description)),
   id=>{game.UiPackingSetStability(id);BuildPackingAgain();},
   formula.stability.id));

  RequireViewElement<Label>(overlay,"packing-formula-dimensions").text=$"{formula.core.width} × {formula.core.height} ／ {RotationLabel(formula.core.rotation)}";
  RequireViewElement<Label>(overlay,"packing-formula-core").text=formula.core.description;
  RequireViewElement<Label>(overlay,"packing-formula-conduit").text=formula.conduit.description;
  RequireViewElement<Label>(overlay,"packing-formula-resonance").text=formula.resonance.description;
  RequireViewElement<Label>(overlay,"packing-formula-stability").text=formula.stability.description;
  return overlay;
 }

 VisualElement BuildFormulaAccordion(
  string sectionId,
  string sectionLabel,
  string selectedName,
  string selectedDesc,
  System.Collections.Generic.IEnumerable<(string id,string name,string description)> options,
  System.Action<string> onPick,
  string selectedId){
  bool open=packingFormulaSection==sectionId;
  var block=Container("ps-rite-formula-acc");
  if(open)block.AddToClassList("ps-rite-formula-acc-open");

  var head=new Button(()=>{
   packingFormulaSection=open?"":sectionId;
   BuildPackingAgain();
  });
  head.AddToClassList("ps-rite-formula-acc-head");
  var mark=new Label(open?"▾":"▸"){pickingMode=PickingMode.Ignore};
  mark.AddToClassList("ps-rite-formula-acc-mark");
  var kind=new Label(sectionLabel){pickingMode=PickingMode.Ignore};
  kind.AddToClassList("ps-rite-formula-acc-kind");
  var chosen=new Label(selectedName){pickingMode=PickingMode.Ignore};
  chosen.AddToClassList("ps-rite-formula-acc-chosen");
  head.Add(mark);
  head.Add(kind);
  head.Add(chosen);
  block.Add(head);

  if(open){
   var body=Container("ps-rite-formula-acc-body");
   if(!string.IsNullOrEmpty(selectedDesc)){
    var desc=new Label(selectedDesc){pickingMode=PickingMode.Ignore};
    desc.AddToClassList("ps-rite-formula-acc-desc");
    body.Add(desc);
   }
   foreach(var option in options){
    var entry=option;
    var button=PackspireUiFactory.Button(entry.name,()=>{
     packingFormulaSection=sectionId;
     onPick(entry.id);
    });
    button.AddToClassList("ps-rite-formula-acc-option");
    button.tooltip=entry.description;
    if(entry.id==selectedId)button.AddToClassList("ps-selected");
    body.Add(button);
   }
   block.Add(body);
  }
  return block;
 }

 VisualElement BuildCardsPopup(RunState run,DeckBuildResult build){
  var overlay=CloneView("UI/PackspirePackingCardsPopup","ps-rite-popup-overlay");
  overlay.RegisterCallback<ClickEvent>(evt=>{if(evt.target==overlay){packingCardsOpen=false;BuildPackingAgain();}});
  RequireViewElement<Button>(overlay,"packing-cards-close").clicked+=()=>{packingCardsOpen=false;BuildPackingAgain();};
  var seals=PackingDeliverySeals(run,build);
  RequireViewElement<Label>(overlay,"packing-cards-title").text=$"装備札 {build.candidates.Count} ／ 配達印 {seals.Sum(x=>x.charges)}回";
  var combat=RequireViewElement<ScrollView>(overlay,"packing-cards-combat");
  foreach(var card in build.candidates){
   var stats=new List<string>();
   if(card.damage>0)stats.Add($"攻撃 {card.damage}");
   if(card.block>0)stats.Add($"防御 {card.block}");
   if(card.heal>0)stats.Add($"回復 {card.heal}");
   string detail=card.source+"\n"+(stats.Count>0?"現在: "+string.Join(" ／ ",stats)+"\n基礎効果: ":"")+card.text;
   combat.Add(RiteEffectCard(card.name,$"{card.cost}EN",detail,true));
  }
  if(build.candidates.Count==0)combat.Add(RiteEmptyNote("装備を盤へ配置すると戦闘札が追加されます。"));
  var sealList=RequireViewElement<ScrollView>(overlay,"packing-cards-seals");
  foreach(var seal in seals)sealList.Add(RiteEffectCard(seal.name,$"残り{seal.charges}/{seal.maxCharges}回",seal.source+" ／ "+seal.text,true));
  if(seals.Count==0)sealList.Add(RiteEmptyNote("盤と装備の色を一致させると配達印が追加されます。"));
  return overlay;
 }

 VisualElement BuildRiteGrid(RunState run,ActiveStorageFormula formula){
  int width=formula.core.width,cells=formula.core.CellCount;
  var grid=Container("ps-rite-grid");
  packingGridElement=grid;
  grid.RegisterCallback<CustomStyleResolvedEvent>(_=>ResizePackingBoard());
  for(int index=0;index<cells;index++){
   int cellIndex=index;
   var occupant=PlacementAt(run,index);
   var cell=new VisualElement{userData=index,focusable=true};
   cell.AddToClassList("ps-rite-cell");
   var element=StorageFormulaSystem.BoardAt(formula.core,index);
   var color=occupant!=null?CellElementAt(run,occupant,index):null;
   cell.Add(BuildRiteOrb(element,color.HasValue?(color.Value==element?"ps-rite-orb-match":"ps-rite-orb-miss"):""));
   if(occupant!=null){
    var item=run.inventory.FirstOrDefault(x=>x.uid==occupant.itemUid);
    if(item!=null){
     cell.AddToClassList("ps-occupied");
     cell.EnableInClassList("ps-selected",item.uid==selectedPackingUid);
     cell.tooltip=$"{GameCatalog.Items[item.templateId].name} ／ 盤:{ElementLabel(element)}";
     BindPackingDragSource(cell,item.uid,formula,false,new Vector2Int(index%width-occupant.anchor%width,index/width-occupant.anchor/width));
    }
   }else{
    cell.tooltip="盤の属性: "+ElementLabel(element);
    cell.RegisterCallback<ClickEvent>(_=>{
     if(packingDragging||packingFormulaOpen||packingCardsOpen||packingFormulaBrowserOpen||string.IsNullOrEmpty(selectedPackingUid))return;
     if(!game.UiPackingPlace(selectedPackingUid,cellIndex,packingRotation))ShowToast("そこには置けません");
     BuildPackingAgain();
    });
   }
   grid.Add(cell);
  }
  foreach(var placement in run.placements){
   var item=run.inventory.FirstOrDefault(x=>x.uid==placement.itemUid);
   if(item==null)continue;
   var visual=Container("ps-rite-board-item");
   visual.userData=placement;
   visual.pickingMode=PickingMode.Ignore;
   var art=VaultItemDisplayArt(item.templateId,"ps-rite-board-item-art");
   art.pickingMode=PickingMode.Ignore;
   visual.Add(art);
   visual.Query<VisualElement>().ForEach(x=>x.pickingMode=PickingMode.Ignore);
   grid.Add(visual);
  }
  // Item colors remain readable above the single equipment image.
  for(int index=0;index<cells;index++){
   var occupant=PlacementAt(run,index);
   var color=occupant!=null?CellElementAt(run,occupant,index):null;
   if(!color.HasValue)continue;
   var layer=Container("ps-rite-cell-color-layer");
   layer.userData=new Vector2Int(index%width,index/width);
   layer.pickingMode=PickingMode.Ignore;
   var mark=new Label("◆"){pickingMode=PickingMode.Ignore};
   mark.AddToClassList("ps-rite-cell-color");
   mark.AddToClassList("ps-element-"+color.Value.ToString().ToLowerInvariant());
   mark.tooltip="装備の属性: "+ElementLabel(color.Value);
   layer.Add(mark);grid.Add(layer);
  }
  return grid;
 }

 void ResizePackingBoard(){
  if(packingGridElement==null||game.UiRun==null||packingBoardScrollElement==null)return;
  var formula=BackpackSystem.Formula(game.UiRun);
  var core=formula.core;
  var grid=packingGridElement;
  if(!grid.customStyle.TryGetValue(new CustomStyleProperty<float>("--packing-cell-side"),out var natural))return;
  grid.customStyle.TryGetValue(new CustomStyleProperty<float>("--packing-min-cell-side"),out var minimum);
  float side=Mathf.Max(minimum,Mathf.Floor(natural*packingZoom));
  grid.style.width=side*core.width;
  grid.style.height=side*core.height;
  var viewport=packingBoardScrollElement.contentViewport;
  float frameX=packingKilnElement.resolvedStyle.paddingLeft+packingKilnElement.resolvedStyle.paddingRight;
  float frameY=packingKilnElement.resolvedStyle.paddingTop+packingKilnElement.resolvedStyle.paddingBottom;
  packingRootElement.EnableInClassList("packing--board-overflow-x",side*core.width+frameX>viewport.contentRect.width);
  packingRootElement.EnableInClassList("packing--board-overflow-y",side*core.height+frameY>viewport.contentRect.height);
  foreach(var child in grid.Children()){
   if(child.userData is int){
    child.style.width=side;child.style.height=side;
   }else if(child.userData is Placement placement){
    var item=game.UiRun.inventory.FirstOrDefault(x=>x.uid==placement.itemUid);
    if(item==null)continue;
    var layout=BackpackSystem.Layout(GameCatalog.Items[item.templateId],placement.rotation,item);
    int w=layout.Max(x=>x.pos.x)+1,h=layout.Max(x=>x.pos.y)+1;
    child.style.left=(placement.anchor%core.width)*side;
    child.style.top=(placement.anchor/core.width)*side;
    child.style.width=w*side;child.style.height=h*side;
    // The image is drawn once in the rotated bounding box, not repeated in every cell.
    bool quarter=placement.rotation%2!=0;
    float artWidth=(quarter?h:w)*side,artHeight=(quarter?w:h)*side;
    var art=child[0];
    art.style.width=artWidth;art.style.height=artHeight;
    art.style.left=(w*side-artWidth)/2;
    art.style.top=(h*side-artHeight)/2;
    art.style.rotate=new Rotate(Angle.Degrees(placement.rotation*90));
   }else if(child.userData is Vector2Int position){
    child.style.left=position.x*side;child.style.top=position.y*side;
    child.style.width=side;child.style.height=side;
   }
  }
  RequireViewElement<Label>(packingRootElement,"packing-zoom-value").text=$"{Mathf.RoundToInt(side/natural*100)}%";
 }

 VisualElement BuildShapePreview(ItemInstance item,int rotation){
  var def=GameCatalog.Items[item.templateId];
  var layout=BackpackSystem.Layout(def,rotation,item);
  int maxX=layout.Max(c=>c.pos.x)+1;
  int maxY=layout.Max(c=>c.pos.y)+1;
  var preview=Container("ps-rite-shape");
  for(int y=0;y<maxY;y++){
   var row=Container("ps-rite-shape-row");
   for(int x=0;x<maxX;x++){
    var cell=Container("ps-rite-shape-cell");
    var found=layout.Where(c=>c.pos.x==x&&c.pos.y==y).ToList();
    if(found.Count>0){
     cell.AddToClassList("ps-rite-shape-filled");
     cell.AddToClassList("ps-element-"+found[0].element.ToString().ToLowerInvariant());
    } else {
     cell.AddToClassList("ps-rite-shape-empty");
    }
    row.Add(cell);
   }
   preview.Add(row);
  }
  return preview;
 }

 Placement PlacementAt(RunState run,int index){
  int width=BackpackSystem.GridWidth(run);
  int x=index%width,y=index/width;
  foreach(var placement in run.placements){
   var item=run.inventory.FirstOrDefault(i=>i.uid==placement.itemUid);
   if(item==null)continue;
   int ax=placement.anchor%width,ay=placement.anchor/width;
   if(BackpackSystem.Layout(GameCatalog.Items[item.templateId],placement.rotation,item).Any(c=>ax+c.pos.x==x&&ay+c.pos.y==y))return placement;
  }
  return null;
 }

 Element? CellElementAt(RunState run,Placement placement,int index){
  var item=run.inventory.FirstOrDefault(i=>i.uid==placement.itemUid);
  if(item==null)return null;
  int width=BackpackSystem.GridWidth(run);
  int x=index%width,y=index/width,ax=placement.anchor%width,ay=placement.anchor/width;
  foreach(var cell in BackpackSystem.Layout(GameCatalog.Items[item.templateId],placement.rotation,item))
   if(ax+cell.pos.x==x&&ay+cell.pos.y==y)return cell.element;
  return null;
 }

 string RotationLabel(RotationCapability capability)=>capability switch{
  RotationCapability.FlipOnly=>"反転のみ",
  RotationCapability.QuarterTurn=>"90°単位",
  _=>"フル回転",
 };

 string TraitEffectLabel(ColorTraitDef trait)=>trait.effect switch{
  ColorTraitEffect.Damage=>$"この装備の攻撃 +{trait.amount}",
  ColorTraitEffect.Block=>$"この装備の防御 +{trait.amount}",
  ColorTraitEffect.Heal=>$"この装備の回復 +{trait.amount}",
  ColorTraitEffect.CostReduce=>$"この装備のコスト -{trait.amount}",
  ColorTraitEffect.Draw=>$"ドロー +{trait.amount}",
  ColorTraitEffect.Recycle=>"使用後に山札へ戻る",
  ColorTraitEffect.DurabilityFree=>"耐久を消費しない",
  _=>"",
 };
}
}
