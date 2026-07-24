 using UnityEngine;
 using UnityEngine.UIElements;

namespace Packspire {
public sealed partial class PackspireUiFoundation {
 void RefreshScreen(bool force){
  if(game==null||screenRoot==null)return;
  if(!Handles(game.UiScreen)){
   if(game.UiScreen==ScreenId.Battle&&explorationStage!=null)SuspendExplorationStage();
   screenRoot.style.display=DisplayStyle.None;
   hasRenderedScreen=false;
   return;
  }
  screenRoot.style.display=DisplayStyle.Flex;
  if(!force&&hasRenderedScreen&&renderedScreen==game.UiScreen){
   UpdateNavHud();
   return;
  }
  if(hasRenderedScreen&&renderedScreen!=game.UiScreen&&!navSuppressHistory)
   RecordNavHistory(renderedScreen,game.UiScreen);
  if(renderedScreen==ScreenId.Map&&game.UiScreen!=ScreenId.Map){
   if(game.UiScreen==ScreenId.Battle)SuspendExplorationStage();
   else ReleaseExplorationStage();
  }
  if(renderedScreen==ScreenId.GridBoard&&game.UiScreen!=ScreenId.GridBoard)
   SuspendGridBoard();
  if(renderedScreen==ScreenId.Battle&&game.UiScreen!=ScreenId.Battle)
   SuspendBattleUi();
  if(renderedScreen==ScreenId.Reward&&game.UiScreen!=ScreenId.Reward&&game.UiScreen!=ScreenId.Map&&game.UiScreen!=ScreenId.GridBoard&&game.UiScreen!=ScreenId.Battle)
   ReleaseExplorationStage();
  renderedScreen=game.UiScreen;hasRenderedScreen=true;
  ClearScreenTree();
#if UNITY_EDITOR
  qaRefreshScreenBuilds++;
  Debug.Log($"[PackspireQA] RefreshScreen build #{qaRefreshScreenBuilds} screen={renderedScreen}");
#endif
  if(renderedScreen==ScreenId.Character)BuildCharacter();
  else if(renderedScreen==ScreenId.Hub)BuildHub();
  else if(renderedScreen==ScreenId.Status)BuildStatus();
  else if(renderedScreen==ScreenId.Vault)BuildVault();
  else if(renderedScreen==ScreenId.Heirloom)BuildHeirloom();
  else if(renderedScreen==ScreenId.Faction)BuildFaction();
  else if(renderedScreen==ScreenId.Expedition)BuildExpedition();
  else if(renderedScreen==ScreenId.Pack)BuildPacking();
  else if(renderedScreen==ScreenId.Map)BuildExplorationMap();
  else if(renderedScreen==ScreenId.GridBoard)BuildGridBoard();
  else if(renderedScreen==ScreenId.Battle)BuildBattle();
  else if(renderedScreen==ScreenId.Reward)BuildReward();
  else if(renderedScreen==ScreenId.Shop)BuildShop();
  else if(renderedScreen==ScreenId.Event)BuildEvent();
  else if(renderedScreen==ScreenId.GameOver){ReleaseExplorationStage();BuildGameOver();}
  else if(renderedScreen==ScreenId.GameClear){ReleaseExplorationStage();BuildGameClear();}
  else BuildCompendium();
  AnimateScreenIn();
  UpdateNavHud();
 }

 void ClearScreenTree(){
  mgmtListScroll=null;
  mgmtVaultGrid=null;
  mgmtOverviewHost=null;
  mgmtDetailHero=null;
  mgmtDetailArtHost=null;
  mgmtDetailSummaryHost=null;
  mgmtDetailScroll=null;
  mgmtListHeader=null;
  factionShell=null;
  factionGraphHost=null;
  factionGraphEdges=null;
  factionGraphNodes=null;
  factionDetailScroll=null;
  expeditionShell=null;
  expeditionDestScroll=null;
  expeditionDestList=null;
  expeditionArtHost=null;
  expeditionArtImageHost=null;
  expeditionArtLockOverlay=null;
  expeditionArtCaptionName=null;
  expeditionArtCaptionSub=null;
  expeditionDungeonInfoHost=null;
  expeditionCharacterHost=null;
  expeditionTraitHost=null;
  expeditionSkillHost=null;
  expeditionLoadoutHost=null;
  expeditionLoadoutList=null;
  expeditionDetailColumn=null;
  expeditionDetailBody=null;
  expeditionDepartFooter=null;
  expeditionDetailScroll=null;
  expeditionDepartButton=null;
  expeditionDepartReason=null;
  expeditionDepartLabel=null;
  expeditionLoadoutDetailHost=null;
  expeditionLayoutAudited=false;
  heirloomShell=null;
  heirloomSlotButton=null;
  heirloomSlotArt=null;
  heirloomSlotGlyph=null;
  heirloomPortraitHost=null;
  heirloomGrowthScroll=null;
  heirloomGrowthBody=null;
  heirloomModalLayer=null;
  heirloomPickerGrid=null;
  heirloomPickerConfirm=null;
  heirloomPickerOpen=false;
  rosterShell=null;
  rosterReelScroll=null;
  rosterArtHost=null;
  rosterDetailScrollHost=null;
  rosterDetailBody=null;
  rosterConfirmButton=null;
  hubShell=null;
  hubFacilityScroll=null;
  hubCharacterHost=null;
  hubBriefingHost=null;
  hubGoldLabel=null;
  hubStreetGuideEntry=null;
  hubReelScroll=null;
  hubStreetGuideModal=null;
  hubStreetGuideList=null;
  hubStreetGuideDetail=null;
  hubStreetGuideFacilityScroll=null;
  hubStreetGuideOpen=false;
  packingRootElement=null;
  packingGridElement=null;
  packingDragGhost=null;
  packingFilterRowElement=null;
  packingKilnElement=null;
  packingKilnRailElement=null;
  packingPopupElement=null;
  packingEquipScrollElement=null;
  packingRightScrollElement=null;
  shopShell=null;
  shopProductGrid=null;
  shopProductScroll=null;
  shopDetailHost=null;
  shopDetailScroll=null;
  shopMerchantScene=null;
  shopMerchantBackdropLayer=null;
  shopMerchantCharacterViewport=null;
  shopMerchantCharacterImage=null;
  shopMerchantDialogueLayer=null;
  shopMerchantDialogue=null;
  shopMerchantCounterLayer=null;
  shopMerchantTransactionLayer=null;
  shopFutureMerchantActionLayer=null;
  shopGoldLabel=null;
  shopTotalLabel=null;
  shopSelectedNameLabel=null;
  shopPurchaseReason=null;
  shopBuyButton=null;
  shopLeaveButton=null;
  shopFilterHost=null;
  shopDevHintLabel=null;
  shopMerchantCharacterTex=null;
#if UNITY_EDITOR
  qaClearScreenTreeCount++;
#endif
  rewardShell=null;
  rewardCandidateList=null;
  rewardCandidateScroll=null;
  rewardDetailArtHost=null;
  rewardDetailScroll=null;
  rewardHeaderType=null;
  rewardHeaderPlace=null;
  rewardHeaderText=null;
  rewardSelectionStatus=null;
  rewardConfirmButton=null;
  rewardReturnButton=null;
  resultShell=null;
  resultVisualHost=null;
  resultTitleOverlay=null;
  resultTitleLabel=null;
  resultSubtitleLabel=null;
  resultCauseLabel=null;
  resultPrimaryStatsHost=null;
  resultRecordScroll=null;
  resultUnlockHost=null;
  resultHeirloomHost=null;
  resultReturnButton=null;
  screenRoot.Clear();
 }

 void RebuildScreen(System.Action builder){
  if(screenRoot==null||builder==null)return;
  ClearScreenTree();
  builder();
 }

 void AnimateScreenIn(){
  if(screenRoot==null)return;
  if(skipNextAnimateIn){
   skipNextAnimateIn=false;
   screenRoot.style.opacity=1f;
   screenRoot.style.translate=new Translate(0,0,0);
   return;
  }
  float x=renderedScreen==ScreenId.Faction?70f:renderedScreen==ScreenId.Expedition?-70f:renderedScreen==ScreenId.Pack?-45f:0f;
  float y=renderedScreen==ScreenId.Pack?55f:12f;
  screenRoot.style.opacity=.01f;
  screenRoot.style.translate=new Translate(x,y,0);
  screenRoot.schedule.Execute(()=>{
   if(screenRoot==null)return;
   screenRoot.style.opacity=1f;
   screenRoot.style.translate=new Translate(0,0,0);
  }).StartingIn(16);
 }
}
}

