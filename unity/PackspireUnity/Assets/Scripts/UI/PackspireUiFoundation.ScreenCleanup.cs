namespace Packspire {
public sealed partial class PackspireUiFoundation {
 void ClearScreenReferences(){
  ClearVaultFixedViewFields();
  ClearManagementReferences();
  ClearFactionReferences();
  ClearExpeditionReferences();
  ClearHeirloomReferences();
  ClearRosterReferences();
  ClearHubReferences();
  ClearPackingReferences();
  ClearCourierRouteReferences();
  ClearShopReferences();
  ClearRewardReferences();
  ClearResultReferences();
 }

 void ClearManagementReferences(){
  mgmtListScroll=null;
  mgmtVaultGrid=null;
  mgmtOverviewHost=null;
  mgmtDetailHero=null;
  mgmtDetailArtHost=null;
  mgmtDetailSummaryHost=null;
  mgmtDetailScroll=null;
  mgmtListHeader=null;
  statusAppointmentEyebrow=null;
  statusAppointmentTitle=null;
  statusAppointmentDetail=null;
  statusAppointmentAction=null;
 }

 void ClearFactionReferences(){
  factionShell=null;
  factionGraphHost=null;
  factionGraphEdges=null;
  factionGraphNodes=null;
  factionEmissaryStudyFront=null;
  factionEmissaryStudyBack=null;
  factionEmissaryHost=null;
  factionEmissaryName=null;
  factionEmissaryTitle=null;
  factionDetailHeader=null;
  factionDetailScroll=null;
 }

 void ClearExpeditionReferences(){
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
  expeditionAuthDestination=null;
  expeditionAuthCourier=null;
  expeditionAuthLoadout=null;
  expeditionLoadoutDetailHost=null;
  expeditionLayoutAudited=false;
 }

 void ClearHeirloomReferences(){
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
 }

 void ClearRosterReferences(){
  rosterShell=null;
  rosterReelScroll=null;
  rosterReelHost=null;
  rosterArtHost=null;
  rosterDetailScrollHost=null;
  rosterDetailBody=null;
  rosterDossierPortraitHost=null;
  rosterConfirmButton=null;
  rosterArtCaptionName=null;
  rosterArtCaptionTitle=null;
  rosterConfirmLabel=null;
 }

 void ClearHubReferences(){
  hubShell=null;
  hubFacilityScroll=null;
  hubNavButtons=null;
  hubCharacterHost=null;
  hubCharacterStudyFront=null;
  hubCharacterStudyBack=null;
  hubBriefingHost=null;
  hubEquipmentRoleArt=null;
  hubEquipmentHeirloomArt=null;
  hubGoldLabel=null;
  hubCharacterNameLabel=null;
  hubDestinationNameLabel=null;
  hubDestinationCodeLabel=null;
  hubMissionTitleLabel=null;
  hubMissionDestinationLabel=null;
  hubMissionLoadoutLabel=null;
  hubMissionCargoLabel=null;
  hubVaultIndexCountLabel=null;
  hubCodexIndexCountLabel=null;
  hubStreetGuideEntry=null;
  hubStreetGuideModal=null;
  hubStreetGuideDetail=null;
  hubStreetGuideFacilityScroll=null;
  hubStreetGuideOpen=false;
 }

 void ClearPackingReferences(){
  packingRootElement=null;
  packingGridElement=null;
  packingDragGhost=null;
  packingFilterRowElement=null;
  packingKilnElement=null;
  packingKilnRailElement=null;
  packingPopupElement=null;
  packingEquipScrollElement=null;
  packingRightScrollElement=null;
 }

 void ClearShopReferences(){
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
  shopHeaderGoldLabel=null;
  shopGoldLabel=null;
  shopTotalLabel=null;
  shopSelectedNameLabel=null;
  shopPurchaseReason=null;
  shopBuyButton=null;
  shopLeaveButton=null;
  shopFilterHost=null;
  shopDevHintLabel=null;
  shopMerchantCharacterTex=null;
 }

 void ClearRewardReferences(){
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
 }

 void ClearResultReferences(){
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
 }
}
}
