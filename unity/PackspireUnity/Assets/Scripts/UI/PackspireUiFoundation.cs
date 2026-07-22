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
 VisualElement packingRootElement,packingGridElement,packingDragGhost;
 VisualElement packingFilterRowElement,packingKilnElement,packingKilnRailElement,packingPopupElement;
 ScrollView packingEquipScrollElement,packingRightScrollElement;
 float packingEquipScrollY,packingRightScrollY;
 int compendiumTab;
 Button developerAccessButton;
 VisualElement developerPanelRoot;
 // exploration fields live in PackspireUiFoundation.ExplorationMap.cs
 // battle fields live in PackspireUiFoundation.Battle.cs

 void Awake(){
  if(Instance!=null&&Instance!=this){Destroy(this);return;}
  Instance=this;game=GetComponent<PackspireGame>();
  document=gameObject.GetComponent<UIDocument>()??gameObject.AddComponent<UIDocument>();
  panelSettings=Resources.Load<PanelSettings>("UI/PackspirePanelSettings");
  if(panelSettings==null){
   panelSettings=ScriptableObject.CreateInstance<PanelSettings>();
   panelSettings.name="Packspire Runtime UI Fallback";
   panelSettings.scaleMode=PanelScaleMode.ScaleWithScreenSize;
   panelSettings.referenceResolution=new Vector2Int(1280,720);
   panelSettings.match=.5f;
   panelSettings.sortingOrder=120;
   ownsPanelSettings=true;
  }
  var tree=Resources.Load<VisualTreeAsset>("UI/PackspireRoot");
  document.enabled=false;document.panelSettings=panelSettings;document.visualTreeAsset=tree;document.enabled=true;
 }

 IEnumerator Start(){for(int frame=0;frame<30;frame++){if(document!=null&&document.rootVisualElement!=null&&document.rootVisualElement.panel!=null)break;yield return null;}BuildRoot();if(uiReady)RefreshScreen(true);}
 void Update(){
  if(root!=null)RefreshScreen(false);
  HandleNavInput();
  RefreshDeveloperOverlay();
  if(renderedScreen==ScreenId.Map&&explorationMapBuilt)TickExplorationMap();
 }
 void OnDestroy(){
  if(Instance==this)Instance=null;
  ReleaseExplorationStage();
  if(ownsPanelSettings&&panelSettings!=null)Destroy(panelSettings);
 }

 public bool Handles(ScreenId value){
  if(!uiReady||game==null)return false;
  if(value==ScreenId.Map)return game.UiUsesExplorationMap;
  return true;
 }
 public void ForceRefreshScreen(){
  hasRenderedScreen=false;
  explorationMapBuilt=false;
  battleUiBuilt=false;
  if(uiReady)RefreshScreen(true);
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
