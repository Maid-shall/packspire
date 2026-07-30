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
  string[] pool=RewardPool();
  if(pool.Length==0)return System.Array.Empty<string>();
  int completed=Mathf.Max(0,(game.UiRun?.battlesWon??1)-1);
  int start=(completed*3)%pool.Length;
  return Enumerable.Range(0,Mathf.Min(3,pool.Length)).Select(i=>pool[(start+i)%pool.Length]).ToArray();
 }

 string[] RewardIdsPreview()=>RewardPool().Take(3).ToArray();

 string[] RewardPool(){
  string dungeonId=game.UiRun?.dungeon;
  var dungeon=PackspireContent.Data.dungeons.FirstOrDefault(value=>value.id==dungeonId);
  string poolId=string.IsNullOrEmpty(dungeon?.rewardPoolId)?"standard":dungeon.rewardPoolId;
  var pool=PackspireContent.Data.rewardPools.FirstOrDefault(x=>x.id==poolId)
   ??PackspireContent.Data.rewardPools.FirstOrDefault(x=>x.id=="standard");
  return pool?.itemIds??System.Array.Empty<string>();
 }

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

  rewardShell=CloneView("UI/PackspireRewardView","ps-reward-screen ps-dark-surface");
  if(rewardShell==null){
   Debug.LogError("Reward view could not be created.");
   return;
  }
  var backgroundHost=RequireViewElement<VisualElement>(rewardShell,"reward-background");
  var bg=HubBackgroundArt();
  if(bg==null)bg=CourtyardArt();
  if(bg!=null)backgroundHost.Insert(0,Image(bg,new Rect(0,0,1,1),"ps-mgmt-bg",ScaleMode.ScaleAndCrop));

  RequireViewElement<VisualElement>(rewardShell,"reward-header").Insert(
   0,
   PackspireUiFactory.SystemIcon(PackspireUiFactory.PopIcon.Reward,"ps-reward-header-icon")
  );
  rewardHeaderType=RequireViewElement<Label>(rewardShell,"reward-header-type");
  rewardHeaderType.text=rewardPreviewMode?"戦利品（DEV）":"戦利品";
  string place=rewardPreviewMode?"試掘の間":"封印格子";
  rewardHeaderPlace=RequireViewElement<Label>(rewardShell,"reward-header-place");
  rewardHeaderPlace.text=place;
  rewardHeaderText=RequireViewElement<Label>(rewardShell,"reward-header-text");
  rewardHeaderText.text="暗い卓上に、わずかな光が戦利品だけを照らしている。";

  var candidateCol=RequireViewElement<VisualElement>(rewardShell,"reward-candidate-column");
  candidateCol.Add(PackspireUiFactory.SystemOrnament(PackspireUiFactory.PopOrnament.VerticalBoundary,"ps-reward-column-boundary"));
  rewardCandidateScroll=RequireViewElement<ScrollView>(rewardShell,"reward-candidate-scroll");
  rewardCandidateScroll.verticalScrollerVisibility=ScrollerVisibility.Auto;
  rewardCandidateScroll.scrollOffset=new Vector2(0,rewardCandidateScrollY);
  rewardCandidateList=RequireViewElement<VisualElement>(rewardShell,"reward-candidate-list");

  var detailCol=RequireViewElement<VisualElement>(rewardShell,"reward-detail-column");
  detailCol.Add(PackspireUiFactory.SystemOrnament(PackspireUiFactory.PopOrnament.OpenCorner,"ps-reward-detail-corner"));
  rewardDetailArtHost=RequireViewElement<VisualElement>(rewardShell,"reward-detail-art");
  rewardDetailScroll=RequireViewElement<ScrollView>(rewardShell,"reward-detail-scroll");
  rewardDetailScroll.verticalScrollerVisibility=ScrollerVisibility.Auto;

  rewardSelectionStatus=RequireViewElement<Label>(rewardShell,"reward-selection-status");
  var actions=RequireViewElement<VisualElement>(rewardShell,"reward-footer");
  rewardConfirmButton=PackspireUiFactory.Button("この戦利品を獲得する",ConfirmRewardSelection);
  rewardConfirmButton.AddToClassList("ps-primary-action");
  rewardConfirmButton.AddToClassList("ps-chrome-action");
  PackspireUiFactory.DecorateActionButton(rewardConfirmButton,true);
  actions.Add(rewardConfirmButton);
  rewardReturnButton=PackspireUiFactory.Button(rewardPreviewMode?"プレビューを閉じる":"地図へ戻る",()=>{
   if(rewardPreviewMode)CloseRewardPreview();
   else game.UiReturnToMap();
  });
  rewardReturnButton.AddToClassList("ps-chrome-action");
  PackspireUiFactory.DecorateActionButton(rewardReturnButton,false);
  actions.Add(rewardReturnButton);
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
