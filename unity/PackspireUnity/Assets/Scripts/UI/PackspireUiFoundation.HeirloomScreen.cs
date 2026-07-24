using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire {
public sealed partial class PackspireUiFoundation {
 VisualElement heirloomShell;
 VisualElement heirloomSlotButton;
 VisualElement heirloomSlotArt;
 Label heirloomSlotGlyph;
 VisualElement heirloomPortraitHost;
 ScrollView heirloomGrowthScroll;
 VisualElement heirloomGrowthBody;
 VisualElement heirloomModalLayer;
 VisualElement heirloomPickerGrid;
 Button heirloomPickerConfirm;
 string heirloomPickerSelectedUid="";
 bool heirloomPickerOpen;
 float heirloomGrowthScrollY;
 float heirloomPickerScrollY;

 void BuildHeirloom(){
  heirloomPickerOpen=false;
  heirloomShell=Container("ps-heirloom-screen ps-dark-surface");

  var backgroundHost=Container("ps-layer-background");
  var bg=HubBackgroundArt();
  if(bg==null)bg=CourtyardArt();
  if(bg!=null)backgroundHost.Add(Image(bg,new Rect(0,0,1,1),"ps-mgmt-bg",ScaleMode.ScaleAndCrop));
  var shade=Container("ps-mgmt-shade");
  shade.pickingMode=PickingMode.Ignore;
  backgroundHost.Add(shade);
  heirloomShell.Add(backgroundHost);

  var contentHost=Container("ps-layer-content");
  var header=Container("ps-mgmt-header ps-heirloom-header");
  header.Add(ChromeBrand("HEIRLOOM  /  RELIC","家宝"));
  heirloomSlotButton=BuildHeirloomSlotButton();
  header.Add(heirloomSlotButton);
  contentHost.Add(header);

  var body=Container("ps-heirloom-body");
  var portraitCol=Container("ps-heirloom-col-portrait");
  heirloomPortraitHost=Container("ps-heirloom-portrait-host");
  portraitCol.Add(heirloomPortraitHost);
  body.Add(portraitCol);

  var growthCol=Container("ps-heirloom-col-growth");
  heirloomGrowthScroll=new ScrollView(ScrollViewMode.Vertical);
  heirloomGrowthScroll.AddToClassList("ps-heirloom-growth-scroll");
  heirloomGrowthScroll.verticalScrollerVisibility=ScrollerVisibility.Auto;
  heirloomGrowthScroll.horizontalScrollerVisibility=ScrollerVisibility.Hidden;
  heirloomGrowthScroll.scrollOffset=new Vector2(0,heirloomGrowthScrollY);
  heirloomGrowthBody=Container("ps-heirloom-growth-body");
  heirloomGrowthScroll.Add(heirloomGrowthBody);
  growthCol.Add(heirloomGrowthScroll);
  body.Add(growthCol);

  contentHost.Add(body);
  heirloomShell.Add(contentHost);

  heirloomModalLayer=BuildHeirloomPickerModal();
  heirloomModalLayer.style.display=DisplayStyle.None;
  heirloomModalLayer.pickingMode=PickingMode.Ignore;
  heirloomShell.Add(heirloomModalLayer);

  screenRoot.Add(heirloomShell);
  RefreshHeirloomContent();
 }

 Button BuildHeirloomSlotButton(){
  var button=new Button(OpenHeirloomPicker);
  button.AddToClassList("ps-heirloom-slot");
  button.AddToClassList("ps-chrome-action");
  heirloomSlotArt=Container("ps-heirloom-slot-art");
  heirloomSlotArt.pickingMode=PickingMode.Ignore;
  button.Add(heirloomSlotArt);
  heirloomSlotGlyph=new Label("+"){pickingMode=PickingMode.Ignore};
  heirloomSlotGlyph.AddToClassList("ps-heirloom-slot-glyph");
  button.Add(heirloomSlotGlyph);
  var crest=HeirloomMark();
  crest.AddToClassList("ps-heirloom-slot-crest");
  crest.style.display=DisplayStyle.None;
  button.Add(crest);
  return button;
 }

 VisualElement BuildHeirloomPickerModal(){
  var modal=Container("ps-layer-modal ps-heirloom-modal");
  var backdrop=new Button(CloseHeirloomPicker);
  backdrop.AddToClassList("ps-heirloom-modal-backdrop");
  modal.Add(backdrop);

  var panel=Container("ps-heirloom-picker");
  panel.RegisterCallback<ClickEvent>(evt=>evt.StopPropagation());
  panel.Add(ChromeBrand("CHOOSE  /  RELIC","家宝を選ぶ"));

  var scroll=new ScrollView(ScrollViewMode.Vertical);
  scroll.AddToClassList("ps-heirloom-picker-scroll");
  scroll.verticalScrollerVisibility=ScrollerVisibility.Auto;
  scroll.name="heirloom-picker-scroll";
  heirloomPickerGrid=Container("ps-heirloom-picker-grid");
  scroll.Add(heirloomPickerGrid);
  panel.Add(scroll);

  var footer=Container("ps-heirloom-picker-footer");
  var cancel=PackspireUiFactory.Button("キャンセル",CloseHeirloomPicker);
  cancel.AddToClassList("ps-chrome-action");
  footer.Add(cancel);
  heirloomPickerConfirm=PackspireUiFactory.Button("決定",ConfirmHeirloomPicker);
  heirloomPickerConfirm.AddToClassList("ps-chrome-action");
  heirloomPickerConfirm.AddToClassList("ps-primary-action");
  footer.Add(heirloomPickerConfirm);
  panel.Add(footer);
  modal.Add(panel);
  return modal;
 }

 void RefreshHeirloomContent(){
  if(heirloomPortraitHost==null||heirloomGrowthBody==null)return;
  SaveHeirloomGrowthScroll();
  RefreshHeirloomSlot(game.UiMeta);
  RefreshHeirloomPortrait(game.UiMeta);
  RefreshHeirloomGrowthBoard(game.UiMeta);
  RestoreHeirloomGrowthScroll();
 }

 void SaveHeirloomGrowthScroll(){
  if(heirloomGrowthScroll!=null)heirloomGrowthScrollY=heirloomGrowthScroll.scrollOffset.y;
 }

 void RestoreHeirloomGrowthScroll(){
  if(heirloomGrowthScroll!=null)heirloomGrowthScroll.scrollOffset=new Vector2(0,heirloomGrowthScrollY);
 }

 ItemInstance ResolveSelectedHeirloom(MetaSave meta){
  if(meta==null||string.IsNullOrEmpty(meta.selectedHeirloomUid)||meta.stash==null)return null;
  return meta.stash.FirstOrDefault(x=>x.uid==meta.selectedHeirloomUid);
 }

 void RefreshHeirloomSlot(MetaSave meta){
  if(heirloomSlotButton==null||heirloomSlotArt==null||heirloomSlotGlyph==null)return;
  heirloomSlotArt.Clear();
  var item=ResolveSelectedHeirloom(meta);
  var crest=heirloomSlotButton.Q(className:"ps-heirloom-slot-crest");
  if(item!=null&&GameCatalog.Items.ContainsKey(item.templateId)){
   var def=GameCatalog.Items[item.templateId];
   heirloomSlotArt.Add(Atlas(game.UiEquipmentArt,ItemUv(def.id),"ps-heirloom-slot-image"));
   heirloomSlotGlyph.style.display=DisplayStyle.None;
   if(crest!=null)crest.style.display=DisplayStyle.Flex;
   heirloomSlotButton.tooltip="家宝を選び直す";
   heirloomSlotButton.EnableInClassList("ps-heirloom-slot-empty",false);
  }else{
   heirloomSlotGlyph.text="+";
   heirloomSlotGlyph.style.display=DisplayStyle.Flex;
   if(crest!=null)crest.style.display=DisplayStyle.None;
   heirloomSlotButton.tooltip="家宝を選ぶ";
   heirloomSlotButton.EnableInClassList("ps-heirloom-slot-empty",true);
  }
 }

 void RefreshHeirloomPortrait(MetaSave meta){
  heirloomPortraitHost.Clear();
  var item=ResolveSelectedHeirloom(meta);
  if(item==null||!GameCatalog.Items.ContainsKey(item.templateId)){
   heirloomPortraitHost.Add(PackspireUiFactory.EmptyState(
    "家宝はまだ定まっていない",
    "右上の枠から、共に歩む装備を選んでください。"));
   return;
  }

  var def=GameCatalog.Items[item.templateId];
  var artFrame=Container("ps-heirloom-portrait-art");
  artFrame.pickingMode=PickingMode.Ignore;
  artFrame.Add(Atlas(game.UiEquipmentArt,ItemUv(def.id),"ps-heirloom-portrait-image"));
  heirloomPortraitHost.Add(artFrame);

  var nameRow=Container("ps-heirloom-name-row");
  nameRow.Add(PackspireUiFactory.Title(def.name));
  nameRow.Add(HeirloomMark());
  heirloomPortraitHost.Add(nameRow);

  heirloomPortraitHost.Add(HeirloomStatBlock("段階 / 異名","まだ兆しはない"));
  heirloomPortraitHost.Add(HeirloomStatBlock("傷跡",HeirloomScarCountLabel(item)));
  heirloomPortraitHost.Add(HeirloomStatBlock("使用回数",$"{item.uses} 回"));
  heirloomPortraitHost.Add(HeirloomStatBlock("共に踏破した遠征",HeirloomExpeditionCountLabel(item)));
  heirloomPortraitHost.Add(HeirloomStatBlock("家宝固有能力","まだ兆しはない"));
 }

 void RefreshHeirloomGrowthBoard(MetaSave meta){
  if(heirloomGrowthBody==null)return;
  heirloomGrowthBody.Clear();
  var item=ResolveSelectedHeirloom(meta);

  heirloomGrowthBody.Add(ChromeSection("GROWTH","成長の系譜"));
  heirloomGrowthBody.Add(HeirloomBoardSection("現在の成長段階","まだ兆しはない"));
  heirloomGrowthBody.Add(HeirloomProgressFrame("次段階への進捗"));

  heirloomGrowthBody.Add(ChromeSection("LINEAGE","進化・派生"));
  heirloomGrowthBody.Add(HeirloomCandidateRow("派生候補 A","条件未発見",true));
  heirloomGrowthBody.Add(HeirloomCandidateRow("派生候補 B","条件未発見",true));
  heirloomGrowthBody.Add(HeirloomCandidateRow("派生候補 C","条件未発見",true));

  heirloomGrowthBody.Add(ChromeSection("GIFTS","能力"));
  heirloomGrowthBody.Add(HeirloomBoardSection("解放済みの能力","記録なし"));
  heirloomGrowthBody.Add(HeirloomBoardSection("未解放の能力","条件未発見",true));

  heirloomGrowthBody.Add(ChromeSection("SCARS","傷跡の記録"));
  heirloomGrowthBody.Add(BuildHeirloomScarList(item));

  heirloomGrowthBody.Add(ChromeSection("HISTORY","家宝としての戦歴"));
  heirloomGrowthBody.Add(BuildHeirloomHistoryBlock(item));

  heirloomGrowthBody.Add(ChromeSection("OMEN","進化の兆し"));
  heirloomGrowthBody.Add(HeirloomBoardSection("条件 / ヒント","条件未発見",true));
 }

 VisualElement HeirloomStatBlock(string label,string value){
  var block=Container("ps-heirloom-stat");
  var lab=new Label(label){pickingMode=PickingMode.Ignore};
  lab.AddToClassList("ps-heirloom-stat-label");
  block.Add(lab);
  var val=new Label(value){pickingMode=PickingMode.Ignore};
  val.AddToClassList("ps-heirloom-stat-value");
  block.Add(val);
  return block;
 }

 VisualElement HeirloomBoardSection(string title,string body,bool locked=false){
  var section=Container(locked?"ps-heirloom-board-section ps-heirloom-locked":"ps-heirloom-board-section");
  var heading=new Label(title){pickingMode=PickingMode.Ignore};
  heading.AddToClassList("ps-heirloom-board-title");
  section.Add(heading);
  var text=new Label(body){pickingMode=PickingMode.Ignore};
  text.AddToClassList("ps-heirloom-board-body");
  section.Add(text);
  return section;
 }

 VisualElement HeirloomProgressFrame(string title){
  var frame=Container("ps-heirloom-progress");
  var heading=new Label(title){pickingMode=PickingMode.Ignore};
  heading.AddToClassList("ps-heirloom-board-title");
  frame.Add(heading);
  var track=Container("ps-heirloom-progress-track");
  var fill=Container("ps-heirloom-progress-fill");
  fill.style.width=Length.Percent(0);
  track.Add(fill);
  frame.Add(track);
  var hint=new Label("まだ兆しはない"){pickingMode=PickingMode.Ignore};
  hint.AddToClassList("ps-heirloom-board-body");
  frame.Add(hint);
  return frame;
 }

 VisualElement HeirloomCandidateRow(string title,string hint,bool locked){
  var row=Container(locked?"ps-heirloom-candidate ps-heirloom-locked":"ps-heirloom-candidate");
  var silhouette=Container("ps-heirloom-candidate-silhouette");
  silhouette.pickingMode=PickingMode.Ignore;
  if(locked){
   var lockMark=new Label("鎖"){pickingMode=PickingMode.Ignore};
   lockMark.AddToClassList("ps-heirloom-candidate-lock");
   silhouette.Add(lockMark);
  }
  row.Add(silhouette);
  var copy=Container("ps-heirloom-candidate-copy");
  copy.pickingMode=PickingMode.Ignore;
  var name=new Label(title){pickingMode=PickingMode.Ignore};
  name.AddToClassList("ps-heirloom-board-title");
  copy.Add(name);
  var body=new Label(hint){pickingMode=PickingMode.Ignore};
  body.AddToClassList("ps-heirloom-board-body");
  copy.Add(body);
  row.Add(copy);
  return row;
 }

 VisualElement BuildHeirloomScarList(ItemInstance item){
  var host=Container("ps-heirloom-scar-list");
  if(item?.scars==null||item.scars.Count==0){
   host.Add(HeirloomBoardSection("記録","記録なし"));
   return host;
  }
  foreach(var scar in item.scars.Take(12)){
   var row=Container("ps-heirloom-scar-row");
   var type=new Label(string.IsNullOrEmpty(scar.type)?"傷跡":scar.type){pickingMode=PickingMode.Ignore};
   type.AddToClassList("ps-heirloom-scar-type");
   row.Add(type);
   var detail=new Label($"{scar.dungeon}  L{scar.floor}"){pickingMode=PickingMode.Ignore};
   detail.AddToClassList("ps-heirloom-scar-detail");
   row.Add(detail);
   host.Add(row);
  }
  return host;
 }

 VisualElement BuildHeirloomHistoryBlock(ItemInstance item){
  var host=Container("ps-heirloom-history");
  if(item?.history==null||(item.history.battles<=0&&item.history.bosses<=0&&item.history.defeats<=0&&(item.history.dungeons==null||item.history.dungeons.Count==0))){
   host.Add(HeirloomBoardSection("戦歴","記録なし"));
   return host;
  }
  host.Add(HeirloomStatBlock("戦闘",$"{item.history.battles}"));
  host.Add(HeirloomStatBlock("ボス",$"{item.history.bosses}"));
  host.Add(HeirloomStatBlock("敗北",$"{item.history.defeats}"));
  if(item.history.dungeons!=null&&item.history.dungeons.Count>0){
   var lines=item.history.dungeons.Take(8).Select(d=>{
    var name=GameCatalog.Dungeons.FirstOrDefault(x=>x.id==d.id)?.name??d.id;
    return $"{name} ×{d.value}";
   });
   host.Add(HeirloomBoardSection("踏破記録",string.Join("\n",lines)));
  }
  return host;
 }

 static string HeirloomScarCountLabel(ItemInstance item){
  int count=item.scars?.Count??0;
  return count<=0?"記録なし":$"{count}";
 }

 static string HeirloomExpeditionCountLabel(ItemInstance item){
  int count=item.history?.dungeons?.Count??0;
  return count<=0?"記録なし":$"{count}";
 }

 void OpenHeirloomPicker(){
  if(heirloomModalLayer==null||heirloomPickerOpen)return;
  var meta=game.UiMeta;
  heirloomPickerSelectedUid=meta.selectedHeirloomUid??"";
  if(!string.IsNullOrEmpty(heirloomPickerSelectedUid)&&!meta.stash.Any(x=>x.uid==heirloomPickerSelectedUid))
   heirloomPickerSelectedUid="";
  PopulateHeirloomPicker(meta);
  heirloomPickerOpen=true;
  heirloomModalLayer.style.display=DisplayStyle.Flex;
  heirloomModalLayer.pickingMode=PickingMode.Position;
  heirloomModalLayer.BringToFront();
 }

 void CloseHeirloomPicker(){
  if(heirloomModalLayer==null)return;
  var scroll=heirloomModalLayer.Q<ScrollView>("heirloom-picker-scroll");
  if(scroll!=null)heirloomPickerScrollY=scroll.scrollOffset.y;
  heirloomPickerOpen=false;
  heirloomModalLayer.style.display=DisplayStyle.None;
  heirloomModalLayer.pickingMode=PickingMode.Ignore;
 }

 void ConfirmHeirloomPicker(){
  if(string.IsNullOrEmpty(heirloomPickerSelectedUid))return;
  if(!game.UiMeta.stash.Any(x=>x.uid==heirloomPickerSelectedUid))return;
  game.UiSelectHeirloom(heirloomPickerSelectedUid);
  CloseHeirloomPicker();
  RefreshHeirloomContent();
  ShowToast("家宝として記録しました");
 }

 void PopulateHeirloomPicker(MetaSave meta){
  if(heirloomPickerGrid==null)return;
  heirloomPickerGrid.Clear();
  var scroll=heirloomModalLayer?.Q<ScrollView>("heirloom-picker-scroll");
  if(meta.stash==null||meta.stash.Count==0){
   heirloomPickerGrid.Add(PackspireUiFactory.EmptyState("装備なし","保管庫に装備がありません。"));
   UpdateHeirloomPickerConfirm();
   return;
  }
  foreach(var item in meta.stash){
   if(!GameCatalog.Items.ContainsKey(item.templateId))continue;
   var entry=item;
   var def=GameCatalog.Items[entry.templateId];
   bool current=entry.uid==meta.selectedHeirloomUid;
   bool selected=entry.uid==heirloomPickerSelectedUid;
   var card=new Button(()=>{
    heirloomPickerSelectedUid=entry.uid;
    UpdateHeirloomPickerSelection();
    UpdateHeirloomPickerConfirm();
   }){userData=entry.uid,tooltip=def.name};
   card.AddToClassList("ps-heirloom-picker-card");
   if(selected)card.AddToClassList("ps-selected");
   if(current)card.AddToClassList("ps-heirloom-picker-current");

   var art=Container("ps-heirloom-picker-art");
   art.pickingMode=PickingMode.Ignore;
   art.Add(Atlas(game.UiEquipmentArt,ItemUv(def.id),"ps-heirloom-picker-image"));
   if(current)art.Add(HeirloomMark());
   card.Add(art);

   var name=new Label(def.name){pickingMode=PickingMode.Ignore};
   name.AddToClassList("ps-heirloom-picker-name");
   card.Add(name);
   var sub=new Label(current?"現在の家宝":ItemTypeLabel(def.type)){pickingMode=PickingMode.Ignore};
   sub.AddToClassList("ps-heirloom-picker-sub");
   card.Add(sub);
   heirloomPickerGrid.Add(card);
  }
  if(scroll!=null)scroll.scrollOffset=new Vector2(0,heirloomPickerScrollY);
  UpdateHeirloomPickerConfirm();
 }

 void UpdateHeirloomPickerSelection(){
  if(heirloomPickerGrid==null)return;
  foreach(var child in heirloomPickerGrid.Children()){
   if(child is not Button card||card.userData is not string uid)continue;
   card.EnableInClassList("ps-selected",uid==heirloomPickerSelectedUid);
  }
 }

 void UpdateHeirloomPickerConfirm(){
  if(heirloomPickerConfirm==null)return;
  bool ready=!string.IsNullOrEmpty(heirloomPickerSelectedUid)
   &&game.UiMeta.stash.Any(x=>x.uid==heirloomPickerSelectedUid);
  heirloomPickerConfirm.SetEnabled(ready);
 }

 bool TryCloseHeirloomPickerFromInput(){
  if(!heirloomPickerOpen)return false;
  CloseHeirloomPicker();
  return true;
 }
}
}
