using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire {
public sealed partial class PackspireUiFoundation {
 VisualElement rewardShell;
 VisualElement rewardCandidateList;
 ScrollView rewardCandidateScroll;
 VisualElement rewardDetailArtHost;
 ScrollView rewardDetailScroll;
 Label rewardHeaderType;
 Label rewardHeaderPlace;
 Label rewardHeaderText;
 Label rewardSelectionStatus;
 Button rewardConfirmButton;
 Button rewardReturnButton;
 float rewardCandidateScrollY;
 bool rewardPreviewMode;
#if UNITY_EDITOR
 int rewardBuildCount;
#endif

 string[] RewardIds(){
  string[] pool={"dagger","plate","crystal","bomb","spear","buckler","flask","charm"};
  int start=((game.UiRun?.battlesWon??0)*3)%pool.Length;
  return Enumerable.Range(0,3).Select(i=>pool[(start+i)%pool.Length]).ToArray();
 }

 string[] RewardIdsPreview()=>new[]{"dagger","plate","crystal"};

 string[] ActiveRewardIds()=>rewardPreviewMode?RewardIdsPreview():RewardIds();

 void BuildReward(){
#if UNITY_EDITOR
  rewardBuildCount++;
  Debug.Log($"[PackspireQA] BuildReward count={rewardBuildCount}");
#endif
  if(game.UiRun!=null)rewardPreviewMode=false;
  rewardPreviewMode=rewardPreviewMode||game.UiRun==null;
  var rewards=ActiveRewardIds();
  if(string.IsNullOrEmpty(selectedRewardId)||!rewards.Contains(selectedRewardId))
   selectedRewardId=rewards.Length>0?rewards[0]:"";

  rewardShell=Container("ps-reward-screen ps-dark-surface");
  var backgroundHost=Container("ps-layer-background");
  var bg=HubBackgroundArt()??CourtyardArt();
  if(bg!=null)backgroundHost.Add(Image(bg,new Rect(0,0,1,1),"ps-mgmt-bg",ScaleMode.ScaleAndCrop));
  var shade=Container("ps-reward-shade");
  shade.pickingMode=PickingMode.Ignore;
  backgroundHost.Add(shade);
  rewardShell.Add(backgroundHost);

  var contentHost=Container("ps-layer-content");
  var header=Container("ps-reward-header");
  rewardHeaderType=new Label(rewardPreviewMode?"戦利品（DEV）":"戦利品"){pickingMode=PickingMode.Ignore};
  rewardHeaderType.AddToClassList("ps-reward-header-type");
  header.Add(rewardHeaderType);
  string place=rewardPreviewMode?"試掘の間":ExplorationMapSystem.Breadcrumb(game.UiExploration);
  rewardHeaderPlace=new Label(place){pickingMode=PickingMode.Ignore};
  rewardHeaderPlace.AddToClassList("ps-reward-header-place");
  header.Add(rewardHeaderPlace);
  rewardHeaderText=new Label("暗い卓上に、わずかな光が戦利品だけを照らしている。"){pickingMode=PickingMode.Ignore};
  rewardHeaderText.AddToClassList("ps-reward-header-text");
  header.Add(rewardHeaderText);
  contentHost.Add(header);

  var body=Container("ps-reward-body");
  var candidateCol=Container("ps-reward-col-candidates");
  rewardCandidateScroll=new ScrollView(ScrollViewMode.Vertical);
  rewardCandidateScroll.AddToClassList("ps-reward-candidate-scroll");
  rewardCandidateScroll.verticalScrollerVisibility=ScrollerVisibility.Auto;
  rewardCandidateScroll.scrollOffset=new Vector2(0,rewardCandidateScrollY);
  rewardCandidateList=Container("ps-reward-candidate-list");
  rewardCandidateScroll.Add(rewardCandidateList);
  candidateCol.Add(rewardCandidateScroll);
  body.Add(candidateCol);

  var detailCol=Container("ps-reward-col-detail");
  rewardDetailArtHost=Container("ps-reward-detail-art-host");
  detailCol.Add(rewardDetailArtHost);
  rewardDetailScroll=new ScrollView(ScrollViewMode.Vertical);
  rewardDetailScroll.AddToClassList("ps-reward-detail-scroll");
  rewardDetailScroll.verticalScrollerVisibility=ScrollerVisibility.Auto;
  detailCol.Add(rewardDetailScroll);
  body.Add(detailCol);
  contentHost.Add(body);

  var footer=Container("ps-reward-footer");
  rewardSelectionStatus=new Label(){pickingMode=PickingMode.Ignore};
  rewardSelectionStatus.AddToClassList("ps-reward-selection-status");
  footer.Add(rewardSelectionStatus);
  rewardConfirmButton=PackspireUiFactory.Button("この戦利品を獲得する",ConfirmRewardSelection);
  rewardConfirmButton.AddToClassList("ps-primary-action");
  rewardConfirmButton.AddToClassList("ps-chrome-action");
  footer.Add(rewardConfirmButton);
  rewardReturnButton=PackspireUiFactory.Button(rewardPreviewMode?"プレビューを閉じる":"地図へ戻る",()=>{
   if(rewardPreviewMode)CloseRewardPreview();
   else game.UiReturnToMap();
  });
  rewardReturnButton.AddToClassList("ps-chrome-action");
  footer.Add(rewardReturnButton);
  contentHost.Add(footer);

  rewardShell.Add(contentHost);
  screenRoot.Add(rewardShell);

  RefreshRewardCandidates(true);
  RefreshRewardDetail();
  RefreshRewardFooter();
 }

 void SaveRewardCandidateScroll(){
  if(rewardCandidateScroll!=null)rewardCandidateScrollY=rewardCandidateScroll.scrollOffset.y;
 }

 void RestoreRewardCandidateScroll(){
  if(rewardCandidateScroll!=null)rewardCandidateScroll.scrollOffset=new Vector2(0,rewardCandidateScrollY);
 }

 void RefreshRewardCandidates(bool restoreScroll){
  if(rewardCandidateList==null)return;
  if(restoreScroll)SaveRewardCandidateScroll();
  rewardCandidateList.Clear();
  foreach(var id in ActiveRewardIds()){
   if(!GameCatalog.Items.TryGetValue(id,out var item))continue;
   var rewardId=id;
   var row=new Button(()=>SelectRewardCandidate(rewardId)){userData=rewardId,tooltip=item.name};
   row.AddToClassList("ps-reward-candidate-row");
   if(rewardId==selectedRewardId)row.AddToClassList("ps-selected");
   var art=Container("ps-reward-candidate-art");
   art.pickingMode=PickingMode.Ignore;
   art.Add(Atlas(game.UiEquipmentArt,ItemUv(rewardId),"ps-reward-candidate-image"));
   row.Add(art);
   var copy=Container("ps-reward-candidate-copy");
   copy.pickingMode=PickingMode.Ignore;
   var name=new Label(item.name){pickingMode=PickingMode.Ignore};
   name.AddToClassList("ps-reward-candidate-name");
   copy.Add(name);
   var sub=new Label($"{ItemTypeLabel(item.type)}　／　未鑑定"){pickingMode=PickingMode.Ignore};
   sub.AddToClassList("ps-reward-candidate-sub");
   copy.Add(sub);
   row.Add(copy);
   rewardCandidateList.Add(row);
  }
  if(restoreScroll)RestoreRewardCandidateScroll();
 }

 void SelectRewardCandidate(string rewardId){
  if(selectedRewardId==rewardId)return;
  selectedRewardId=rewardId;
  UpdateRewardCandidateSelection();
  RefreshRewardDetail();
  RefreshRewardFooter();
 }

 void UpdateRewardCandidateSelection(){
  if(rewardCandidateList==null)return;
  foreach(var child in rewardCandidateList.Children()){
   if(child is not Button row||row.userData is not string id)continue;
   row.EnableInClassList("ps-selected",id==selectedRewardId);
  }
 }

 void RefreshRewardDetail(){
  if(rewardDetailArtHost==null||rewardDetailScroll==null)return;
  rewardDetailArtHost.Clear();
  rewardDetailScroll.Clear();
  if(string.IsNullOrEmpty(selectedRewardId)||!GameCatalog.Items.TryGetValue(selectedRewardId,out var item)){
   rewardDetailScroll.Add(PackspireUiFactory.EmptyState("候補を選択","左から戦利品を選んでください。"));
   return;
  }
  rewardDetailArtHost.Add(Atlas(game.UiEquipmentArt,ItemUv(selectedRewardId),"ps-reward-detail-image"));
  rewardDetailScroll.Add(PackspireUiFactory.Title(item.name));
  rewardDetailScroll.Add(RewardDetailBlock("種類",ItemTypeLabel(item.type)));
  if(item.cells!=null&&item.cells.Length>0){
   rewardDetailScroll.Add(RewardDetailBlock("形状",$"{item.cells.Length}マス"));
   rewardDetailScroll.Add(RewardDetailBlock("属性",string.Join("・",item.cells.Select(x=>ElementLabel(x.element)).Distinct())));
  }
  if(!string.IsNullOrEmpty(item.description))
   rewardDetailScroll.Add(RewardDetailBlock("性能",item.description));
  if(!string.IsNullOrEmpty(item.linkRule))
   rewardDetailScroll.Add(RewardDetailBlock("LINK効果",item.linkRule));
  rewardDetailScroll.Add(RewardDetailBlock("鑑定","未鑑定"));
 }

 VisualElement RewardDetailBlock(string title,string body){
  var block=Container("ps-reward-detail-block");
  var head=new Label(title){pickingMode=PickingMode.Ignore};
  head.AddToClassList("ps-reward-detail-label");
  block.Add(head);
  var text=new Label(body){pickingMode=PickingMode.Ignore};
  text.AddToClassList("ps-reward-detail-body");
  block.Add(text);
  return block;
 }

 void RefreshRewardFooter(){
  if(rewardSelectionStatus==null)return;
  int total=ActiveRewardIds().Length;
  string name=GameCatalog.Items.TryGetValue(selectedRewardId,out var item)?item.name:"未選択";
  rewardSelectionStatus.text=$"選択 1 / {total}　：　{name}";
  bool ready=!string.IsNullOrEmpty(selectedRewardId)&&GameCatalog.Items.ContainsKey(selectedRewardId);
  rewardConfirmButton?.SetEnabled(ready&&!rewardPreviewMode);
  if(rewardPreviewMode&&rewardConfirmButton!=null)
   rewardConfirmButton.tooltip="DEVプレビューでは獲得できません";
 }

 void ConfirmRewardSelection(){
  if(rewardPreviewMode||string.IsNullOrEmpty(selectedRewardId))return;
  string name=GameCatalog.Items.TryGetValue(selectedRewardId,out var it)?it.name:selectedRewardId;
  game.UiTakeReward(selectedRewardId);
  ShowToast($"{name}を持ち帰った");
 }

 void CloseRewardPreview(){
  rewardPreviewMode=false;
  DevNavigate(ScreenId.Hub);
 }

 void OpenRewardPreviewFromDev(){
  DevNavigate(ScreenId.Reward,()=>rewardPreviewMode=true);
 }
}
}
