using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire {
/// <summary>Retained-mode UI host for all current screens except Map and Battle.</summary>
public sealed partial class PackspireUiFoundation : MonoBehaviour {
 public static PackspireUiFoundation Instance { get; private set; }

 PackspireGame game;
 UIDocument document;
 PanelSettings panelSettings; bool ownsPanelSettings,uiReady;
 VisualElement root,screenRoot,transitionRoot,dim,leftPaper,rightPaper,scrollPaper,battleShade,toast;
 Coroutine transitionRoutine;
 ScreenId renderedScreen;
 bool hasRenderedScreen;
 bool nextPageIsLeft;
 string selectedRoleId="",selectedCompendiumId="";
 string selectedVaultUid="",selectedFactionId="",selectedDungeonId="";
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
 float packingEquipScrollY,packingRightScrollY;
 int compendiumTab;
 Texture2D tabletopDesk;
 PackspirePresentationStage presentationStage;
 VisualElement presentationStageView;
 Button presentationEnterButton,developerAccessButton;
 VisualElement developerPanelRoot;
 Label presentationHintLabel,presentationFacilityLabel;
 bool presentationHubBuilt,presentationEntering;
 Vector2 presentationPointerDownPosition;
 int presentationTapFacility=-1;
 int savedHubFacility=1;
 // exploration fields live in PackspireUiFoundation.ExplorationMap.cs

 void Awake(){
  if(Instance!=null&&Instance!=this){Destroy(this);return;}
  Instance=this;game=GetComponent<PackspireGame>();
  tabletopDesk=Resources.Load<Texture2D>("Art/UI/tabletop-desk-v1");
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
  RefreshDeveloperOverlay();
  if(renderedScreen==ScreenId.Map&&explorationMapBuilt)TickExplorationMap();
 }
 void OnDestroy(){
  if(Instance==this)Instance=null;
  ReleaseExplorationStage();
  if(ownsPanelSettings&&panelSettings!=null)Destroy(panelSettings);
 }

 public bool IsRouteTransitioning=>explorationRouteStage!=null&&explorationRouteStage.IsMoving;
 public bool Handles(ScreenId value){
  if(!uiReady||game==null)return false;
  if(value==ScreenId.Battle)return false;
  if(value!=ScreenId.Map)return true;
  if(game.ShouldDrawLegacyOnGui)return false;
  if(game.UsesRoutePresentation)return true;
  if(game.CurrentRoutePresentationMode==RoutePresentationMode.RiteDebug)return true;
  return game.UiUsesExplorationMap;
 }
 public void ForceRefreshScreen(){hasRenderedScreen=false;presentationHubBuilt=false;explorationMapBuilt=false;if(uiReady)RefreshScreen(true);}
 public void ApplyRoutePresentationVisibility(){ApplyRouteModeVisibility();}
}
}
