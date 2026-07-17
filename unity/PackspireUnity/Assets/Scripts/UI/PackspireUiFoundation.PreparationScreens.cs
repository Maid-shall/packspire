using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire {
	// COMPILE_FIX:2 // COMPILE_FIX_20260718
public sealed partial class PackspireUiFoundation {
 void BuildExpedition(){
  var meta=game.UiMeta;int unlocked=Mathf.Clamp(meta.dungeonsUnlocked,1,GameCatalog.Dungeons.Length);if(string.IsNullOrEmpty(selectedDungeonId)||!GameCatalog.Dungeons.Take(unlocked).Any(x=>x.id==selectedDungeonId))selectedDungeonId=GameCatalog.Dungeons[0].id;
  var desk=TabletopDesk("ps-expedition-workspace");screenRoot.Add(desk);var tray=Container("ps-expedition-case");desk.Add(tray);var selected=GameCatalog.Dungeons.First(x=>x.id==selectedDungeonId);tray.Add(PackspireUiFactory.Title("遠征準備"));tray.Add(Atlas(game.UiDungeonArt,DungeonUv(selected.id),"ps-expedition-selected-art"));tray.Add(PackspireUiFactory.Title(selected.name));tray.Add(PackspireUiFactory.Body(selected.description));tray.Add(PackspireUiFactory.Body($"敵HP ×{selected.hpScale:0.00}　追加攻撃 {selected.damage}　報酬 ×{selected.goldScale:0.00}"));
  tray.Add(PackspireUiFactory.Title("使用する荷造り"));var loadouts=Container("ps-loadout-tabs");foreach(var loadout in meta.loadouts){var entry=loadout;var button=PackspireUiFactory.Button(entry.name,()=>{game.UiSelectLoadout(entry.id);BuildExpeditionAgain();});if(entry.id==meta.selectedLoadoutId)button.AddToClassList("ps-selected");loadouts.Add(button);}tray.Add(loadouts);var active=LoadoutSystem.Active(meta);tray.Add(PackspireUiFactory.Body($"{active.name}　配置 {active.slots.Count}　カード {active.deck.Count}"));var launch=PackspireUiFactory.Button("この地図へ遠征する",()=>game.UiStartExpedition(selected.id));launch.AddToClassList("ps-primary-action");tray.Add(launch);
  var rack=new ScrollView();rack.AddToClassList("ps-map-roll-rack");desk.Add(rack);for(int i=0;i<GameCatalog.Dungeons.Length;i++){var dungeon=GameCatalog.Dungeons[i];bool available=i<unlocked;var roll=new Button(()=>{if(available){selectedDungeonId=dungeon.id;BuildExpeditionAgain();}});roll.AddToClassList("ps-map-roll");if(dungeon.id==selectedDungeonId)roll.AddToClassList("ps-selected");if(!available)roll.AddToClassList("ps-locked");roll.Add(Atlas(game.UiDungeonArt,DungeonUv(dungeon.id),"ps-map-roll-seal"));roll.Add(new Label(available?dungeon.name:"封印された地図"));rack.Add(roll);}desk.Add(TabletopBack());
 }
 void BuildExpeditionAgain(){screenRoot.Clear();BuildExpedition();}

 void BuildPacking(){
  var run=game.UiRun;
  if(run==null){game.UiNavigate(ScreenId.Hub);return;}
  if(!string.IsNullOrEmpty(selectedPackingUid)&&!run.inventory.Any(x=>x.uid==selectedPackingUid))selectedPackingUid="";
  packingDragUid="";
  packingDragging=false;
  packingTapWasSelected=false;
  packingDragFromList=false;
  packingDragGhost=null;

  var formula=BackpackSystem.Formula(run);
  packingRotation=StorageFormulaSystem.ClampRotation(formula.core.rotation,packingRotation);
  var build=BackpackSystem.Build(run);

  var root=Container("ps-rite");
  root.pickingMode=PickingMode.Position;
  packingRootElement=root;
  screenRoot.Add(root);
  RegisterPackingDrag(root);

  var top=Container("ps-rite-top");
  top.Add(PackspireUiFactory.Title("収納術式"));
  var loadouts=Container("ps-rite-loadouts");
  foreach(var loadout in game.UiMeta.loadouts){
   var entry=loadout;
   var button=PackspireUiFactory.Button(entry.name,()=>{game.UiOpenPackingLoadout(entry.id);selectedPackingUid="";BuildPackingAgain();});
   button.AddToClassList("ps-rite-chip");
   if(entry.id==game.UiMeta.selectedLoadoutId)button.AddToClassList("ps-selected");
   loadouts.Add(button);
  }
  top.Add(loadouts);
  var topActions=Container("ps-rite-top-actions");
  if(game.UiPackingAtBase){
   var back=PackspireUiFactory.Button("戻る",()=>game.UiNavigate(ScreenId.Hub));
   back.AddToClassList("ps-rite-chip");
   topActions.Add(back);
  }
  var save=PackspireUiFactory.Button("保存する",()=>game.UiPackingSave());
  save.AddToClassList("ps-rite-save");
  topActions.Add(save);
  top.Add(topActions);
  root.Add(top);
  root.Add(BuildPackingColorCounters(build));

  var body=Container("ps-rite-body");
  root.Add(body);

  var left=Container("ps-rite-left");
  left.Add(PackspireUiFactory.Title("装備"));
  left.Add(PackspireUiFactory.Body("ドラッグで魔方陣へ。マス外に離すと外れる。\n選択中をもう一度タップ／マス外タップで解除。"));
  var listScroll=new ScrollView(ScrollViewMode.Vertical);
  listScroll.AddToClassList("ps-rite-equip-scroll");
  listScroll.verticalScrollerVisibility=ScrollerVisibility.Auto;
  listScroll.scrollOffset=new Vector2(0,packingEquipScrollY);
  var list=Container("ps-rite-equip-list");
  foreach(var item in run.inventory){
   StorageFormulaSystem.EnsureItemRolled(item);
   var entry=item;
   bool placed=run.placements.Any(x=>x.itemUid==entry.uid);
   bool selectedRow=entry.uid==selectedPackingUid;
   var def=GameCatalog.Items[entry.templateId];
   var row=new VisualElement();
   row.AddToClassList("ps-rite-equip");
   row.focusable=true;
   row.pickingMode=PickingMode.Position;
   if(selectedRow)row.AddToClassList("ps-selected");
   if(placed)row.AddToClassList("ps-rite-equip-placed");
   row.Add(Atlas(game.UiEquipmentArt,ItemUv(entry.templateId),"ps-rite-equip-art"));
   var copy=Container("ps-rite-equip-copy");
   copy.pickingMode=PickingMode.Ignore;
   var name=new Label((placed?"● ":"")+def.name){pickingMode=PickingMode.Ignore};
   name.AddToClassList("ps-rite-equip-name");
   var meta=new Label($"{ItemTypeLabel(def.type)}　{def.cells.Length}マス"){pickingMode=PickingMode.Ignore};
   meta.AddToClassList("ps-rite-equip-meta");
   copy.Add(name);
   copy.Add(meta);
   row.Add(copy);
   BindPackingDragSource(row,entry.uid,formula,true);
   list.Add(row);
  }
  if(run.inventory.Count==0)listScroll.Add(PackspireUiFactory.Body("装備がありません。保管庫から持ち込みましょう。"));
  listScroll.Add(list);
  left.Add(listScroll);
  body.Add(left);

  var center=Container("ps-rite-center");
  center.Add(PackspireUiFactory.Title(formula.core.name));
  center.Add(PackspireUiFactory.Body($"{formula.conduit.name}　・　{formula.resonance.name}　・　{formula.stability.name}\n回転：{RotationLabel(formula.core.rotation)}"));

  var tools=Container("ps-rite-tools");
  var formulaBtn=PackspireUiFactory.Button("魔法術式",()=>{packingCardsOpen=false;packingFormulaOpen=true;BuildPackingAgain();});
  formulaBtn.AddToClassList("ps-rite-tool");
  tools.Add(formulaBtn);
  var cardsBtn=PackspireUiFactory.Button($"カード採用　{run.selectedCardSlots.Count}/{build.candidates.Count}",()=>{packingFormulaOpen=false;packingCardsOpen=true;BuildPackingAgain();});
  cardsBtn.AddToClassList("ps-rite-tool");
  tools.Add(cardsBtn);
  var rotate=PackspireUiFactory.Button($"回転　{packingRotation*90}°",()=>{
   int next=StorageFormulaSystem.NextRotation(formula.core.rotation,packingRotation);
   if(!string.IsNullOrEmpty(selectedPackingUid)){
    var placement=run.placements.FirstOrDefault(x=>x.itemUid==selectedPackingUid);
    if(placement!=null&&!game.UiPackingPlace(selectedPackingUid,placement.anchor,next)){
     ShowToast("そこでは回転できません");
     return;
    }
   }
   packingRotation=next;
   BuildPackingAgain();
  });
  rotate.AddToClassList("ps-rite-tool");
  tools.Add(rotate);
  var clear=PackspireUiFactory.Button("選択解除",()=>{selectedPackingUid="";packingRotation=0;BuildPackingAgain();});
  clear.AddToClassList("ps-rite-tool");
  tools.Add(clear);
  var remove=PackspireUiFactory.Button("外す",()=>{game.UiPackingRemove(selectedPackingUid);selectedPackingUid="";BuildPackingAgain();});
  remove.AddToClassList("ps-rite-tool");
  remove.SetEnabled(!string.IsNullOrEmpty(selectedPackingUid)&&run.placements.Any(x=>x.itemUid==selectedPackingUid));
  tools.Add(remove);
  center.Add(tools);

  var circle=Container("ps-rite-circle");
  circle.pickingMode=PickingMode.Position;
  circle.RegisterCallback<ClickEvent>(OnPackingCircleClick);
  circle.Add(BuildRiteGrid(run,formula));
  center.Add(circle);
  body.Add(center);

  var right=new ScrollView(ScrollViewMode.Vertical);
  right.AddToClassList("ps-rite-right");
  right.scrollOffset=new Vector2(0,packingRightScrollY);
  var selected=run.inventory.FirstOrDefault(x=>x.uid==selectedPackingUid);
  if(selected!=null)BuildPackingItemDetail(right,selected,build);
  else BuildPackingOverview(right,run,build);
  body.Add(right);

  if(packingFormulaOpen)root.Add(BuildFormulaPopup(run,formula));
  if(packingCardsOpen)root.Add(BuildCardsPopup(run,build));

  RestorePackingScroll(listScroll,right);
 }

 void BuildPackingItemDetail(VisualElement right,ItemInstance selected,DeckBuildResult build){
  StorageFormulaSystem.EnsureItemRolled(selected);
  var def=GameCatalog.Items[selected.templateId];
  var run=game.UiRun;
  right.Add(Atlas(game.UiEquipmentArt,ItemUv(selected.templateId),"ps-rite-detail-art"));
  right.Add(PackspireUiFactory.Title(def.name));
  right.Add(PackspireUiFactory.Body(def.description));
  right.Add(PackspireUiFactory.Title("形状プレビュー"));
  right.Add(BuildShapePreview(selected,packingRotation));
  string elements=string.Join("・",def.cells.Select((cell,i)=>ElementLabel(selected.colors!=null&&i<selected.colors.Count?selected.colors[i]:cell.element)));
  right.Add(PackspireUiFactory.Body($"属性　{elements}"));
  AddPackingTraitLines(right,run,build,selected);
  AddPackingLinkLines(right,run,build,selected);
 }

 void BuildPackingOverview(VisualElement right,RunState run,DeckBuildResult build){
  right.Add(PackspireUiFactory.Title("全体の状態"));
  right.Add(PackspireUiFactory.Body("装備未選択。いま発動中の色特性／LINKだけを表示しています。\nカードは「カード採用」から選びます。"));
  AddPackingTraitLines(right,run,build,null);
  AddPackingLinkLines(right,run,build,null);
  if(build.stability!=null&&build.stability.runaway)right.Add(RiteStatusLine("⚠ 安定式：過負荷",true));
 }

 void AddPackingTraitLines(VisualElement right,RunState run,DeckBuildResult build,ItemInstance focus){
  right.Add(PackspireUiFactory.Title("色特性"));
  var lines=0;
  IEnumerable<ItemInstance> items=focus!=null
   ?new[]{focus}
   :run.placements.Select(p=>run.inventory.FirstOrDefault(x=>x.uid==p.itemUid)).Where(x=>x!=null);
  foreach(var item in items){
   StorageFormulaSystem.EnsureItemRolled(item);
   var trait=StorageFormulaCatalog.Trait(item.traitId);
   if(trait==null){
    if(focus!=null){right.Add(RiteStatusLine("色特性なし",false));lines++;}
    continue;
   }
   int matches=0;
   build.colors.TryGetValue(trait.element,out matches);
   bool placed=run.placements.Any(x=>x.itemUid==item.uid);
   bool active=placed&&matches>=trait.requiredMatches;
   if(focus==null&&!active)continue;
   var def=GameCatalog.Items[item.templateId];
   string head=focus!=null?trait.name:$"{def.name}：{trait.name}";
   string state=active?"発動中":placed?$"未発動　{ElementLabel(trait.element)}{matches}/{trait.requiredMatches}":"未配置";
   right.Add(RiteStatusLine($"・{head}\n　{state}　／　{TraitEffectLabel(trait)}",active));
   lines++;
  }
  if(lines==0)right.Add(RiteStatusLine(focus!=null?"色特性なし":"発動中の色特性はありません",false));
 }

 void AddPackingLinkLines(VisualElement right,RunState run,DeckBuildResult build,ItemInstance focus){
  right.Add(PackspireUiFactory.Title("隣接LINK"));
  var formula=build.formula.core!=null?build.formula:BackpackSystem.Formula(run);
  var links=formula.resonance.links??System.Array.Empty<ResonanceLinkDef>();
  var upgrades=formula.resonance.upgrades??System.Array.Empty<ResonanceUpgradeDef>();
  if(links.Length==0&&upgrades.Length==0){
   right.Add(RiteStatusLine("この共鳴式には隣接LINKがありません",false));
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
   string state=active?"発動中":"未発動（隣接で発動）";
   right.Add(RiteStatusLine($"・{link.label}\n　{state}　／　{LinkEffectLabel(link)}",active));
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
   string state=active?"発動中":"未発動（隣接で発動）";
   right.Add(RiteStatusLine($"・カード変化　{hostName}×{neighborName}\n　{state}　／　→ {toName}",active));
   lines++;
  }

  if(lines==0)right.Add(RiteStatusLine(focus!=null?"この装備が持つ隣接LINKはありません":"発動中の隣接LINKはありません",false));
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
  var label=new Label(text);
  label.AddToClassList("ps-rite-status");
  if(active)label.AddToClassList("ps-rite-status-active");
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
  var value=new Label(count.ToString());
  value.AddToClassList("ps-rite-color-value");
  var label=new Label(ElementLabel(element));
  label.AddToClassList("ps-rite-color-label");
  chip.Add(value);
  chip.Add(label);
  return chip;
 }

 void RegisterPackingDrag(VisualElement root){
  root.RegisterCallback<PointerMoveEvent>(OnPackingPointerMove);
  root.RegisterCallback<PointerUpEvent>(OnPackingPointerUp);
 }

 void BindPackingDragSource(VisualElement element,string uid,ActiveStorageFormula formula,bool fromEquipList=false){
  element.RegisterCallback<PointerDownEvent>(evt=>{
   if(evt.button!=0||packingFormulaOpen||packingCardsOpen||packingRootElement==null)return;
   packingDragUid=uid;
   packingDragging=false;
   packingDragFromList=fromEquipList;
   packingDragStart=evt.position;
   packingTapWasSelected=selectedPackingUid==uid;
   packingRotation=StorageFormulaSystem.ClampRotation(formula.core.rotation,game.UiRun?.placements.FirstOrDefault(x=>x.itemUid==uid)?.rotation??packingRotation);
   if(!fromEquipList){
    selectedPackingUid=uid;
    packingRootElement.CapturePointer(evt.pointerId);
    evt.StopPropagation();
   }
  });
 }

 void OnPackingPointerMove(PointerMoveEvent evt){
  if(string.IsNullOrEmpty(packingDragUid)||packingRootElement==null)return;
  if(!packingDragging){
   Vector2 delta=(Vector2)evt.position-packingDragStart;
   if(delta.magnitude<10f)return;
   if(packingDragFromList&&Mathf.Abs(delta.y)>Mathf.Abs(delta.x)*1.15f){
    packingDragUid="";
    packingDragging=false;
    packingDragFromList=false;
    return;
   }
   packingDragging=true;
   selectedPackingUid=packingDragUid;
   if(!packingRootElement.HasPointerCapture(evt.pointerId))
    packingRootElement.CapturePointer(evt.pointerId);
   EnsurePackingGhost();
  }
  if(packingDragGhost==null)return;
  var local=packingRootElement.WorldToLocal(evt.position);
  packingDragGhost.style.left=local.x-36;
  packingDragGhost.style.top=local.y-36;
 }

 void OnPackingPointerUp(PointerUpEvent evt){
  if(string.IsNullOrEmpty(packingDragUid))return;
  if(packingRootElement!=null&&packingRootElement.HasPointerCapture(evt.pointerId))
   packingRootElement.ReleasePointer(evt.pointerId);
  if(!packingDragging){
   string uid=packingDragUid;
   packingDragUid="";
   packingDragFromList=false;
   if(packingTapWasSelected){selectedPackingUid="";packingRotation=0;}
   else selectedPackingUid=uid;
   BuildPackingAgain();
   return;
  }
  EndPackingDrag(true,evt.position);
 }

 void OnPackingCircleClick(ClickEvent evt){
  if(packingFormulaOpen||packingCardsOpen||packingDragging||string.IsNullOrEmpty(selectedPackingUid))return;
  var target=evt.target as VisualElement;
  while(target!=null){
   if(target.ClassListContains("ps-rite-cell")||target.ClassListContains("ps-rite-grid"))return;
   if(target.ClassListContains("ps-rite-circle"))break;
   target=target.parent;
  }
  selectedPackingUid="";
  packingRotation=0;
  BuildPackingAgain();
 }

 void EndPackingDrag(bool applyDrop,Vector2 panelPosition=default){
  string uid=packingDragUid;
  bool wasDragging=packingDragging;
  packingDragUid="";
  packingDragging=false;
  packingDragFromList=false;
  if(packingDragGhost!=null){
   packingDragGhost.RemoveFromHierarchy();
   packingDragGhost=null;
  }
  if(!applyDrop||!wasDragging||string.IsNullOrEmpty(uid)||game.UiRun==null){
   if(wasDragging)BuildPackingAgain();
   return;
  }

  int cell=FindPackingCellAt(panelPosition);
  if(cell>=0){
   if(!game.UiPackingPlace(uid,cell,packingRotation))ShowToast("そこには置けません");
   selectedPackingUid=uid;
  } else {
   game.UiPackingRemove(uid);
   selectedPackingUid="";
   ShowToast("装備欄へ戻した");
  }
  BuildPackingAgain();
 }

 void EnsurePackingGhost(){
  if(packingRootElement==null||string.IsNullOrEmpty(packingDragUid)||game.UiRun==null)return;
  var item=game.UiRun.inventory.FirstOrDefault(x=>x.uid==packingDragUid);
  if(item==null)return;
  packingDragGhost=Container("ps-rite-drag-ghost");
  packingDragGhost.pickingMode=PickingMode.Ignore;
  packingDragGhost.Add(Atlas(game.UiEquipmentArt,ItemUv(item.templateId),"ps-rite-drag-ghost-art"));
  packingRootElement.Add(packingDragGhost);
 }

 int FindPackingCellAt(Vector2 panelPosition){
  if(packingGridElement==null)return -1;
  if(!packingGridElement.worldBound.Contains(panelPosition))return -1;
  int best=-1;
  float bestDist=float.MaxValue;
  foreach(var child in packingGridElement.Children()){
   if(child.userData is not int index)continue;
   if(child.worldBound.Contains(panelPosition))return index;
   Vector2 center=child.worldBound.center;
   float dist=(center-panelPosition).sqrMagnitude;
   if(dist<bestDist){bestDist=dist;best=index;}
  }
  return best;
 }

 VisualElement BuildFormulaPopup(RunState run,ActiveStorageFormula formula){
  var overlay=Container("ps-rite-popup-overlay");
  overlay.pickingMode=PickingMode.Position;
  overlay.RegisterCallback<ClickEvent>(evt=>{
   if(evt.target==overlay){packingFormulaOpen=false;BuildPackingAgain();}
  });

  var panel=Container("ps-rite-popup");
  panel.pickingMode=PickingMode.Position;
  panel.RegisterCallback<ClickEvent>(evt=>evt.StopPropagation());

  var header=Container("ps-rite-popup-header");
  header.Add(PackspireUiFactory.Title("魔法術式"));
  var close=PackspireUiFactory.Button("閉じる",()=>{packingFormulaOpen=false;BuildPackingAgain();});
  close.AddToClassList("ps-rite-chip");
  header.Add(close);
  panel.Add(header);
  panel.Add(PackspireUiFactory.Body("収納核・属性導線・共鳴式・安定式を組み合わせます。"));
  var body=new ScrollView(ScrollViewMode.Vertical);
  body.AddToClassList("ps-rite-popup-scroll");
  body.Add(BuildFormulaPanel(run,formula));
  panel.Add(body);
  overlay.Add(panel);
  return overlay;
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

  var header=Container("ps-rite-popup-header");
  header.Add(PackspireUiFactory.Title($"カード採用　{run.selectedCardSlots.Count}/{build.candidates.Count}"));
  var close=PackspireUiFactory.Button("閉じる",()=>{packingCardsOpen=false;BuildPackingAgain();});
  close.AddToClassList("ps-rite-chip");
  header.Add(close);
  panel.Add(header);
  panel.Add(PackspireUiFactory.Body("配置した装備から出た候補をデッキに入れます。上限はありません。"));

  var actions=Container("ps-rite-formula-row");
  var all=PackspireUiFactory.Button("すべて採用",()=>{
   foreach(var card in build.candidates)if(!run.selectedCardSlots.Contains(card.slotKey))run.selectedCardSlots.Add(card.slotKey);
   BuildPackingAgain();
  });
  all.AddToClassList("ps-rite-chip");
  actions.Add(all);
  var none=PackspireUiFactory.Button("すべて外す",()=>{run.selectedCardSlots.Clear();BuildPackingAgain();});
  none.AddToClassList("ps-rite-chip");
  actions.Add(none);
  panel.Add(actions);

  var body=new ScrollView(ScrollViewMode.Vertical);
  body.AddToClassList("ps-rite-popup-scroll");
  var cards=Container("ps-rite-cards");
  foreach(var card in build.candidates){
   var entry=card;
   bool chosen=run.selectedCardSlots.Contains(entry.slotKey);
   var button=new Button(()=>{game.UiPackingToggleCard(entry.slotKey);BuildPackingAgain();}){text=$"{(chosen?"●":"○")} {entry.name}　{entry.cost}EN\n{entry.text}"};
   button.AddToClassList("ps-rite-card");
   if(chosen)button.AddToClassList("ps-selected");
   cards.Add(button);
  }
  if(build.candidates.Count==0)body.Add(PackspireUiFactory.Body("装備を魔方陣に置くとカード候補が出ます。"));
  body.Add(cards);
  panel.Add(body);
  overlay.Add(panel);
  return overlay;
 }

 VisualElement BuildFormulaPanel(RunState run,ActiveStorageFormula formula){
  var panel=Container("ps-rite-formula");
  panel.Add(PackspireUiFactory.Body("収納核"));
  var cores=Container("ps-rite-formula-row");
  foreach(var core in StorageFormulaCatalog.Cores.Values){
   var entry=core;
   var button=PackspireUiFactory.Button(entry.name,()=>{game.UiPackingSetCore(entry.id);BuildPackingAgain();});
   button.AddToClassList("ps-rite-chip");
   button.tooltip=entry.description;
   if(entry.id==formula.core.id)button.AddToClassList("ps-selected");
   cores.Add(button);
  }
  panel.Add(cores);

  panel.Add(PackspireUiFactory.Body("属性導線"));
  var conduits=Container("ps-rite-formula-row");
  foreach(var conduit in StorageFormulaCatalog.Conduits.Values){
   var entry=conduit;
   var button=PackspireUiFactory.Button(entry.name,()=>{game.UiPackingSetConduit(entry.id);BuildPackingAgain();});
   button.AddToClassList("ps-rite-chip");
   button.tooltip=entry.description;
   if(entry.id==formula.conduit.id)button.AddToClassList("ps-selected");
   conduits.Add(button);
  }
  panel.Add(conduits);

  panel.Add(PackspireUiFactory.Body("共鳴式"));
  var resonances=Container("ps-rite-formula-row");
  foreach(var resonance in StorageFormulaCatalog.Resonances.Values){
   var entry=resonance;
   var button=PackspireUiFactory.Button(entry.name,()=>{game.UiPackingSetResonance(entry.id);BuildPackingAgain();});
   button.AddToClassList("ps-rite-chip");
   button.tooltip=entry.description;
   if(entry.id==formula.resonance.id)button.AddToClassList("ps-selected");
   resonances.Add(button);
  }
  panel.Add(resonances);

  panel.Add(PackspireUiFactory.Body("安定式"));
  var stabilities=Container("ps-rite-formula-row");
  foreach(var stability in StorageFormulaCatalog.Stabilities.Values){
   var entry=stability;
   var button=PackspireUiFactory.Button(entry.name,()=>{game.UiPackingSetStability(entry.id);BuildPackingAgain();});
   button.AddToClassList("ps-rite-chip");
   button.tooltip=entry.description;
   if(entry.id==formula.stability.id)button.AddToClassList("ps-selected");
   stabilities.Add(button);
  }
  panel.Add(stabilities);
  return panel;
 }

 VisualElement BuildRiteGrid(RunState run,ActiveStorageFormula formula){
  int width=formula.core.width,cells=formula.core.width*formula.core.height;
  var grid=Container("ps-rite-grid");
  packingGridElement=grid;
  float cellPercent=(100f/width)-1.4f;
  for(int index=0;index<cells;index++){
   int cellIndex=index;
   var occupant=PlacementAt(run,index);
   var boardElement=StorageFormulaSystem.BoardAt(formula.core,index);
   var cell=new VisualElement();
   cell.userData=cellIndex;
   cell.AddToClassList("ps-rite-cell");
   cell.AddToClassList("ps-element-"+boardElement.ToString().ToLowerInvariant());
   cell.focusable=true;
   cell.pickingMode=PickingMode.Position;
   cell.style.width=Length.Percent(cellPercent);
   cell.style.height=76;
   if(occupant!=null){
    var item=run.inventory.FirstOrDefault(x=>x.uid==occupant.itemUid);
    if(item!=null){
     cell.tooltip=GameCatalog.Items[item.templateId].name;
     cell.Add(Atlas(game.UiEquipmentArt,ItemUv(item.templateId),"ps-rite-cell-art"));
     var elementColor=CellElementAt(run,occupant,index);
     bool match=elementColor.HasValue&&elementColor.Value==boardElement;
     cell.AddToClassList(match?"ps-rite-cell-match":"ps-rite-cell-miss");
     if(item.uid==selectedPackingUid)cell.AddToClassList("ps-selected");
     BindPackingDragSource(cell,item.uid,formula);
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

 void BuildPackingAgain(){
  CapturePackingScroll();
  screenRoot.Clear();
  BuildPacking();
 }

 void CapturePackingScroll(){
  if(screenRoot==null)return;
  var left=screenRoot.Q<ScrollView>(className:"ps-rite-equip-scroll");
  if(left!=null)packingEquipScrollY=left.scrollOffset.y;
  var right=screenRoot.Q<ScrollView>(className:"ps-rite-right");
  if(right!=null)packingRightScrollY=right.scrollOffset.y;
 }

 void RestorePackingScroll(ScrollView left,ScrollView right){
  float leftY=packingEquipScrollY;
  float rightY=packingRightScrollY;
  if(left!=null){
   left.schedule.Execute(()=>{
    if(left!=null)left.scrollOffset=new Vector2(0,leftY);
   }).ExecuteLater(0);
  }
  if(right!=null){
   right.schedule.Execute(()=>{
    if(right!=null)right.scrollOffset=new Vector2(0,rightY);
   }).ExecuteLater(0);
  }
 }
}
}
