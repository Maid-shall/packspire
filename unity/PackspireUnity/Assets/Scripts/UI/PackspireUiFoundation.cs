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
  if(presentationStage!=null&&renderedScreen==ScreenId.Hub&&presentationHubBuilt){
   presentationStage.SetMoveInput(ReadMoveInput());
   presentationStage.Tick();
   RefreshPresentationHud();
   if((Input.GetKeyDown(KeyCode.Return)||Input.GetKeyDown(KeyCode.KeypadEnter)||Input.GetKeyDown(KeyCode.E))&&presentationStage.CanEnterAt(NearestPresentationFacility()))
    EnterFocusedBuilding();
  }
 }
 void OnDestroy(){if(Instance==this)Instance=null;if(ownsPanelSettings&&panelSettings!=null)Destroy(panelSettings);}

 public bool Handles(ScreenId value)=>uiReady&&value!=ScreenId.Map&&value!=ScreenId.Battle;
 public void ForceRefreshScreen(){hasRenderedScreen=false;presentationHubBuilt=false;if(uiReady)RefreshScreen(true);}
}
}
