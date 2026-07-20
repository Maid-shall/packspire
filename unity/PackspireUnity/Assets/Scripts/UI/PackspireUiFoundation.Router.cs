using UnityEngine.UIElements;

namespace Packspire {
public sealed partial class PackspireUiFoundation {
 void RefreshScreen(bool force){
  if(game==null||screenRoot==null)return;
  game.UiSyncRoutePresentationMode();
  if(!Handles(game.UiScreen)){
   if(game.UiScreen==ScreenId.Battle&&(explorationStage!=null||explorationRouteStage!=null))SuspendExplorationStage();
   screenRoot.style.display=DisplayStyle.None;
   hasRenderedScreen=false;
   return;
  }
  screenRoot.style.display=DisplayStyle.Flex;
  // RouteReward stays on Map — do not tear down the 2.5D stage for book Reward.
  if(!force&&hasRenderedScreen&&renderedScreen==game.UiScreen){
   if(renderedScreen==ScreenId.Map)ApplyRouteModeVisibility();
   return;
  }
  if(renderedScreen==ScreenId.Hub&&game.UiScreen!=ScreenId.Hub)ReleasePresentationStage();
  if(renderedScreen==ScreenId.Pack&&game.UiScreen!=ScreenId.Pack)ReleasePresentationStage();
  if(renderedScreen==ScreenId.Map&&game.UiScreen!=ScreenId.Map){
   if(game.UiScreen==ScreenId.Battle)SuspendExplorationStage();
   else if(game.UiScreen==ScreenId.Reward&&game.UsesRoutePresentation){/* keep stage */}
   else if(game.UiScreen==ScreenId.Reward)SuspendExplorationStage();
   else ReleaseExplorationStage();
  }
  if(renderedScreen==ScreenId.Reward&&game.UiScreen!=ScreenId.Reward&&game.UiScreen!=ScreenId.Map&&game.UiScreen!=ScreenId.Battle)
   ReleaseExplorationStage();
  renderedScreen=game.UiScreen;hasRenderedScreen=true;screenRoot.Clear();
  if(renderedScreen==ScreenId.Character)BuildCharacter();
  else if(renderedScreen==ScreenId.Hub)BuildHub();
  else if(renderedScreen==ScreenId.Status)BuildStatus();
  else if(renderedScreen==ScreenId.Vault)BuildVault();
  else if(renderedScreen==ScreenId.Faction)BuildFaction();
  else if(renderedScreen==ScreenId.Expedition)BuildExpedition();
  else if(renderedScreen==ScreenId.Pack)BuildPacking();
  else if(renderedScreen==ScreenId.Map)BuildExplorationMap();
  else if(renderedScreen==ScreenId.Reward)BuildReward();
  else if(renderedScreen==ScreenId.Shop)BuildShop();
  else if(renderedScreen==ScreenId.Event)BuildEvent();
  else if(renderedScreen==ScreenId.GameOver){ReleaseExplorationStage();BuildGameOver();}
  else BuildCompendium();
  AnimateScreenIn();
 }

 void AnimateScreenIn(){if(screenRoot==null)return;float x=renderedScreen==ScreenId.Faction?70f:renderedScreen==ScreenId.Expedition?-70f:renderedScreen==ScreenId.Pack?-45f:0f;float y=renderedScreen==ScreenId.Pack?55f:12f;screenRoot.style.opacity=.01f;screenRoot.style.translate=new Translate(x,y,0);screenRoot.schedule.Execute(()=>{if(screenRoot==null)return;screenRoot.style.opacity=1f;screenRoot.style.translate=new Translate(0,0,0);}).StartingIn(16);}
}
}
