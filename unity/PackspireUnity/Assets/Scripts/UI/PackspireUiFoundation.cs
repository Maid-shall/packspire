using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire {
/// <summary>Retained-mode UI host for all current screens.</summary>
public sealed partial class PackspireUiFoundation : MonoBehaviour {
 public static PackspireUiFoundation Instance { get; private set; }

 PackspireGame game;
 UIDocument document;
 PanelSettings panelSettings; bool ownsPanelSettings,uiReady;
 VisualElement root,screenRoot,transitionRoot,dim,leftPaper,rightPaper,scrollPaper,battleShade,toast;
 Coroutine transitionRoutine;
 Coroutine startupRoutine;
 bool startCalled;
 ScreenId renderedScreen;
 bool hasRenderedScreen;
 bool skipNextTransition;
 bool skipNextAnimateIn;
 bool nextPageIsLeft;
#if UNITY_EDITOR
 int qaRefreshScreenBuilds;
 int qaClearScreenTreeCount;
#endif
 string selectedRoleId="",selectedCompendiumId="";
 string selectedVaultUid="",selectedFactionId="",selectedDungeonId="",selectedCharacterId="";
 string selectedPackingUid="",selectedRewardId="",selectedShopId="";
 int packingRotation;
 bool packingFormulaOpen,packingCardsOpen;
 string packingFormulaSection="";
 bool packingTemplateCommitted;
 string packingDragUid="";
 string packingEquipFilter="";
 bool packingDragging,packingTapWasSelected,packingDragFromList;
 Vector2 packingDragStart;
 Vector2Int packingDragGrip;
 VisualElement packingRootElement,packingGridElement,packingDragGhost;
 VisualElement packingFilterRowElement,packingKilnElement,packingKilnRailElement,packingPopupElement;
 ScrollView packingEquipScrollElement,packingRightScrollElement;
 float packingEquipScrollY,packingRightScrollY;
 int compendiumTab;
 int compendiumDetailTab;
 int compendiumDetailPage;
 string compendiumDetailOwnerId="";
 Button developerAccessButton;
 VisualElement developerPanelRoot;
 bool developerOverlayStateKnown;
 bool lastDeveloperOverlayOpen;
 bool journeyPrototypeVisibilityKnown;
 bool journeyPrototypeVisible;
 // battle fields live in PackspireUiFoundation.Battle.cs

 void Awake(){
  if(Instance!=null&&Instance!=this){Destroy(this);return;}
  Instance=this;game=GetComponent<PackspireGame>();
  document=gameObject.GetComponent<UIDocument>();
  if(document==null)document=gameObject.AddComponent<UIDocument>();
  panelSettings=PackspireResources.Load<PanelSettings>("UI/PackspirePanelSettings");
  if(panelSettings==null){
   panelSettings=ScriptableObject.CreateInstance<PanelSettings>();
   panelSettings.name="Packspire Runtime UI Fallback";
   panelSettings.scaleMode=PanelScaleMode.ScaleWithScreenSize;
   panelSettings.referenceResolution=new Vector2Int(1280,720);
   panelSettings.match=.5f;
   panelSettings.sortingOrder=120;
   ownsPanelSettings=true;
  }
  var tree=PackspireResources.Load<VisualTreeAsset>("UI/PackspireRoot");
  document.enabled=false;document.panelSettings=panelSettings;document.visualTreeAsset=tree;document.enabled=true;
 }

 void Start(){
  startCalled=true;
  BeginUiInitialization();
 }
 void OnEnable(){
  if(startCalled&&!uiReady&&startupRoutine==null)BeginUiInitialization();
 }
 void BeginUiInitialization(){
  if(startupRoutine==null)startupRoutine=StartCoroutine(InitializeUi());
 }
 IEnumerator InitializeUi(){
  for(int frame=0;frame<30;frame++){
   if(document!=null&&document.rootVisualElement!=null&&document.rootVisualElement.panel!=null)break;
   yield return null;
  }
  startupRoutine=null;
  BuildRoot();
  if(uiReady)RefreshScreen(true);
 }
 void Update(){
  using var performanceScope=PackspirePerformance.FoundationUpdate.Auto();
  if(root!=null&&(!hasRenderedScreen||renderedScreen!=game.UiScreen))RefreshScreen(false);
  HandleNavInput();
  RefreshDeveloperOverlay();
  if(renderedScreen==ScreenId.GridBoard&&gridBoardBuilt)TickGridBoard();
 }
 void OnDisable(){
  if(startupRoutine!=null){StopCoroutine(startupRoutine);startupRoutine=null;}
  if(transitionRoutine!=null){StopCoroutine(transitionRoutine);transitionRoutine=null;}
  HideTransition();
 }
 void OnDestroy(){
  if(Instance==this)Instance=null;
  if(ownsPanelSettings&&panelSettings!=null)Destroy(panelSettings);
 }

 public bool Handles(ScreenId value){
  if(!uiReady||game==null)return false;
  if(value==ScreenId.GridBoard)return game.UiUsesGridBoard;
  return true;
 }
 public void ForceRefreshScreen(){
  hasRenderedScreen=false;
  gridBoardBuilt=false;
  battleUiBuilt=false;
  if(uiReady)RefreshScreen(true);
 }

 public void SetJourneyPrototypeVisible(bool prototypeVisible){
  if(root==null){journeyPrototypeVisibilityKnown=false;return;}
  if(journeyPrototypeVisibilityKnown&&journeyPrototypeVisible==prototypeVisible)return;
  journeyPrototypeVisibilityKnown=true;
  journeyPrototypeVisible=prototypeVisible;
  root.style.display=prototypeVisible?DisplayStyle.None:DisplayStyle.Flex;
 }

#if UNITY_EDITOR
 public void QaResetBuildCounters(){
  qaRefreshScreenBuilds=0;
  qaClearScreenTreeCount=0;
  shopBuildCount=0;
  shopDetailRefreshCount=0;
  rewardBuildCount=0;
  resultBuildCount=0;
 }
#endif
}
}
