 using UnityEngine;
 using UnityEngine.UIElements;

namespace Packspire {
public sealed partial class PackspireUiFoundation {
 void RefreshScreen(bool force){
  if(game==null||screenRoot==null)return;
  if(!Handles(game.UiScreen)){
   screenRoot.style.display=DisplayStyle.None;
   hasRenderedScreen=false;
   return;
  }
  screenRoot.style.display=DisplayStyle.Flex;
  if(!force&&hasRenderedScreen&&renderedScreen==game.UiScreen)return;
  if(hasRenderedScreen&&renderedScreen!=game.UiScreen&&!navSuppressHistory)
   RecordNavHistory(renderedScreen,game.UiScreen);
  if(renderedScreen==ScreenId.GridBoard&&game.UiScreen!=ScreenId.GridBoard)
   SuspendGridBoard();
  if(renderedScreen==ScreenId.Battle&&game.UiScreen!=ScreenId.Battle)
   SuspendBattleUi();
  renderedScreen=game.UiScreen;hasRenderedScreen=true;
  ClearScreenTree();
  ApplyScreenStyleSheets(renderedScreen);
#if UNITY_EDITOR
  qaRefreshScreenBuilds++;
  Debug.Log($"[PackspireQA] RefreshScreen build #{qaRefreshScreenBuilds} screen={renderedScreen}");
#endif
  switch(renderedScreen){
   case ScreenId.Character: BuildCharacter(); break;
   case ScreenId.Hub: BuildHub(); break;
   case ScreenId.Status: BuildStatus(); break;
   case ScreenId.Vault: BuildVault(); break;
   case ScreenId.Heirloom: BuildHeirloom(); break;
   case ScreenId.Faction: BuildFaction(); break;
   case ScreenId.Expedition: BuildExpedition(); break;
   case ScreenId.Pack: BuildPacking(); break;
   case ScreenId.Route: BuildCourierRoute(); break;
   case ScreenId.GridBoard: BuildGridBoard(); break;
   case ScreenId.Battle: BuildBattle(); break;
   case ScreenId.Reward: BuildReward(); break;
   case ScreenId.Shop: BuildShop(); break;
   case ScreenId.Event: BuildEvent(); break;
   case ScreenId.GameOver: BuildGameOver(); break;
   case ScreenId.GameClear: BuildGameClear(); break;
   default: BuildCompendium(); break;
  }
  AnimateScreenIn();
  UpdateNavHud();
 }

 void ClearScreenTree(){
  ClearScreenReferences();
#if UNITY_EDITOR
  qaClearScreenTreeCount++;
#endif
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

