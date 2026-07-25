using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire {
public sealed partial class PackspireUiFoundation {
// Packing formula/card popups and rite-grid previews.
 VisualElement BuildFormulaPopup(RunState run,ActiveStorageFormula formula){
  var overlay=Container("ps-rite-popup-overlay");
  overlay.pickingMode=PickingMode.Position;
  overlay.RegisterCallback<ClickEvent>(evt=>{
   if(evt.target!=overlay)return;
   CloseFormulaPopup(true);
  });

  var panel=Container("ps-rite-popup");
  panel.AddToClassList("ps-rite-popup-formula");
  panel.pickingMode=PickingMode.Position;
  panel.RegisterCallback<ClickEvent>(evt=>evt.StopPropagation());

  DressRiteFrame(panel);
  var header=Container("ps-rite-popup-header");
  var headerTitle=Container("ps-rite-popup-title-block");
  var headerEye=new Label("FORMULA  /  PRESET"){pickingMode=PickingMode.Ignore};
  headerEye.AddToClassList("ps-rite-top-eyebrow");
  headerTitle.Add(headerEye);
  var headerName=new Label("魔法術式"){pickingMode=PickingMode.Ignore};
  headerName.AddToClassList("ps-rite-top-name");
  headerTitle.Add(headerName);
  header.Add(headerTitle);
  var close=PackspireUiFactory.Button("閉じる",()=>CloseFormulaPopup(true));
  close.AddToClassList("ps-rite-chip");
  header.Add(close);
  panel.Add(header);

  var nameRow=Container("ps-rite-formula-name");
  var nameLabel=new Label("術式名"){pickingMode=PickingMode.Ignore};
  nameLabel.AddToClassList("ps-rite-formula-name-label");
  nameRow.Add(nameLabel);
  var currentName=game.UiMeta?.loadouts?.FirstOrDefault(x=>x.id==game.UiMeta.selectedLoadoutId)?.name??"新規術式";
  var nameField=new TextField{value=currentName};
  nameField.AddToClassList("ps-rite-formula-name-field");
  nameField.RegisterValueChangedCallback(evt=>game.UiPackingRenameLoadout(evt.newValue));
  nameRow.Add(nameField);
  panel.Add(nameRow);

  var body=Container("ps-rite-formula-body");
  var rail=Container("ps-rite-formula-rail");
  rail.RegisterCallback<ClickEvent>(evt=>evt.StopPropagation());
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
  body.Add(rail);

  var stage=Container("ps-rite-formula-stage");
  stage.pickingMode=PickingMode.Position;
  stage.RegisterCallback<ClickEvent>(evt=>{
   evt.StopPropagation();
   if(string.IsNullOrEmpty(packingFormulaSection))return;
   packingFormulaSection="";
   BuildPackingAgain();
  });
  var preview=BuildMagicCircleLayers(formula);
  preview.AddToClassList("ps-rite-circle-preview");
  preview.pickingMode=PickingMode.Ignore;
  stage.Add(preview);
  StartPackingCirclePulse(stage);
  var stageHint=new Label("核＝形　導線＝色　共鳴＝浮遊　安定＝紋章"){pickingMode=PickingMode.Ignore};
  stageHint.AddToClassList("ps-rite-formula-stage-hint");
  stage.Add(stageHint);
  body.Add(stage);

  panel.Add(body);
  overlay.Add(panel);
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
  var overlay=Container("ps-rite-popup-overlay");
  overlay.pickingMode=PickingMode.Position;
  overlay.RegisterCallback<ClickEvent>(evt=>{
   if(evt.target==overlay){packingCardsOpen=false;BuildPackingAgain();}
  });

  var panel=Container("ps-rite-popup");
  panel.AddToClassList("ps-rite-popup-cards");
  panel.pickingMode=PickingMode.Position;
  panel.RegisterCallback<ClickEvent>(evt=>evt.StopPropagation());
  DressRiteFrame(panel);

  var header=Container("ps-rite-popup-header");
  var headerTitle=Container("ps-rite-popup-title-block");
  var headerEye=new Label("DECK  /  COMBAT + EXPLORE"){pickingMode=PickingMode.Ignore};
  headerEye.AddToClassList("ps-rite-top-eyebrow");
  headerTitle.Add(headerEye);
  var headerName=new Label($"術式札  {build.candidates.Count} 枚"){pickingMode=PickingMode.Ignore};
  headerName.AddToClassList("ps-rite-top-name");
  headerTitle.Add(headerName);
  header.Add(headerTitle);
  var close=PackspireUiFactory.Button("閉じる",()=>{packingCardsOpen=false;BuildPackingAgain();});
  close.AddToClassList("ps-rite-chip");
  header.Add(close);
  panel.Add(header);
  panel.Add(RiteMetaLine("術式に置いた装備は全て両面札になる。左＝戦闘面、右＝探索面。個別の採用選択は不要。"));

  var split=Container("ps-rite-deck-split");

  // Left: combat face. Placement into the storage formula is the only adoption rule.
  var combatCol=Container("ps-rite-deck-col");
  var combatHead=new Label("戦闘面"){pickingMode=PickingMode.Ignore};
  combatHead.AddToClassList("ps-rite-deck-col-title");
  combatCol.Add(combatHead);
  var combatScroll=new ScrollView(ScrollViewMode.Vertical);
  combatScroll.AddToClassList("ps-rite-deck-col-scroll");
  var combatCards=Container("ps-rite-cards");
  foreach(var card in build.candidates){
   var button=new Button(()=>{}){text=$"● {card.name}　{card.cost}EN\n{card.text}"};
   button.AddToClassList("ps-rite-card");
   button.pickingMode=PickingMode.Ignore;
   button.AddToClassList("ps-selected");
   combatCards.Add(button);
  }
  if(build.candidates.Count==0)
   combatScroll.Add(PackspireUiFactory.Body("装備を魔方陣に置くと戦闘カード候補が出ます。"));
  combatScroll.Add(combatCards);
  combatCol.Add(combatScroll);
  split.Add(combatCol);

  // Right: the reverse face of the same formula cards. Effects are still provisional.
  var exploreCol=Container("ps-rite-deck-col");
  exploreCol.AddToClassList("ps-rite-deck-col-explore");
  var exploreHead=new Label("探索面（設計中）"){pickingMode=PickingMode.Ignore};
  exploreHead.AddToClassList("ps-rite-deck-col-title");
  exploreCol.Add(exploreHead);
  exploreCol.Add(PackspireUiFactory.Body("左の各札に対応する裏面。盤面術式の種類と成長効果は次の段階で定義する。"));
  var exploreScroll=new ScrollView(ScrollViewMode.Vertical);
  exploreScroll.AddToClassList("ps-rite-deck-col-scroll");
  var exploreCards=Container("ps-rite-cards");
  foreach(var entry in BuildExploreDeckPreview(run,build)){
   var button=new Button(()=>{}){text=$"◇ {entry.name}　{entry.cost}EN\n{entry.text}"};
   button.AddToClassList("ps-rite-card");
   button.AddToClassList("ps-rite-card-explore");
   button.SetEnabled(false);
   exploreCards.Add(button);
  }
  exploreScroll.Add(exploreCards);
  exploreCol.Add(exploreScroll);
  split.Add(exploreCol);

  panel.Add(split);
  overlay.Add(panel);
  return overlay;
 }

 /// <summary>Provisional reverse-face view for every formula card. Effects are defined later.</summary>
 static List<(string name,int cost,string text)> BuildExploreDeckPreview(RunState run,DeckBuildResult build){
  var list=new List<(string name,int cost,string text)>();
  if(build?.candidates==null)return list;
  foreach(var card in build.candidates)
   list.Add(($"探索：{card.source}",1,$"「{card.name}」の裏面。盤面術式の種類・形状・成熟効果は未定義。"));
  return list;
 }

 VisualElement BuildRiteGrid(RunState run,ActiveStorageFormula formula){
  int width=formula.core.width,cells=formula.core.width*formula.core.height;
  var grid=Container("ps-rite-grid");
  packingGridElement=grid;
  // Keep the visual row width identical to the formula's logical width. The old
  // percentage plus per-cell margins could wrap six logical cells into five.
  float cellPercent=100f/width;
  float cellHeight=Mathf.Clamp(560f/Mathf.Max(1,formula.core.height),58f,84f);
  var plateTex=Resources.Load<Texture2D>("Art/Rite/rite-cell-plate-v1");
  for(int index=0;index<cells;index++){
   int cellIndex=index;
   int cellX=index%width,cellY=index/width;
   var occupant=PlacementAt(run,index);
   var boardElement=StorageFormulaSystem.BoardAt(formula.core,index);
   var cell=new VisualElement();
   cell.userData=cellIndex;
   cell.AddToClassList("ps-rite-cell");
   cell.focusable=true;
   cell.pickingMode=PickingMode.Position;
   cell.style.width=Length.Percent(cellPercent);
   cell.style.height=cellHeight;
   cell.style.marginLeft=0;
   cell.style.marginRight=0;
   cell.style.marginTop=0;
   cell.style.marginBottom=0;

   if(plateTex!=null){
    var plate=new Image{image=plateTex,scaleMode=ScaleMode.StretchToFill,pickingMode=PickingMode.Ignore};
    plate.AddToClassList("ps-rite-cell-plate");
    cell.Add(plate);
   }

   string orbExtra="";
   if(occupant!=null){
    var item=run.inventory.FirstOrDefault(x=>x.uid==occupant.itemUid);
    if(item!=null){
     var elementColor=CellElementAt(run,occupant,index);
     bool match=elementColor.HasValue&&elementColor.Value==boardElement;
     orbExtra=match?"ps-rite-orb-match":"ps-rite-orb-miss";
    }
   }
   var orbWrap=BuildRiteOrb(boardElement,orbExtra);
   orbWrap.pickingMode=PickingMode.Ignore;
   if(occupant==null)orbWrap.AddToClassList("ps-rite-orb-wrap-open");
   else orbWrap.AddToClassList("ps-rite-orb-wrap-placed");
   cell.Add(orbWrap);

   if(occupant!=null){
    var item=run.inventory.FirstOrDefault(x=>x.uid==occupant.itemUid);
    if(item!=null){
     cell.tooltip=GameCatalog.Items[item.templateId].name;
      var art=Atlas(game.UiEquipmentArt,ItemUv(item.templateId),"ps-rite-cell-art");
      art.pickingMode=PickingMode.Ignore;
      art.style.rotate=new Rotate(Angle.Degrees(occupant.rotation*90f));
      cell.Add(art);
      if(item.uid==selectedPackingUid)cell.AddToClassList("ps-selected");
      int anchorX=occupant.anchor%width,anchorY=occupant.anchor/width;
      var grip=new Vector2Int(cellX-anchorX,cellY-anchorY);
      BindPackingDragSource(cell,item.uid,formula,false,grip);
    }
   } else {
    cell.RegisterCallback<ClickEvent>(_=>{
     if(packingDragging||packingFormulaOpen||packingCardsOpen||string.IsNullOrEmpty(selectedPackingUid))return;
     if(!game.UiPackingPlace(selectedPackingUid,cellIndex,packingRotation))ShowToast("そこには置けません");
     BuildPackingAgain();
    });
   }
   grid.Add(cell);
  }
  return grid;
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
