using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire {
public enum TabletopTransitionKind { PageTurn, ScrollUnfurl, BattleTable, Fade }

public sealed partial class PackspireUiFoundation {
 public void PlayFor(ScreenId from,ScreenId to){if(transitionRoot==null||from==to)return;if(transitionRoutine!=null)StopCoroutine(transitionRoutine);transitionRoutine=StartCoroutine(Play(Resolve(from,to)));}
 TabletopTransitionKind Resolve(ScreenId from,ScreenId to){
  if(from==ScreenId.Hub&&to!=ScreenId.Map&&to!=ScreenId.Battle)return TabletopTransitionKind.Fade;
  if(to==ScreenId.Hub&&from!=ScreenId.Map&&from!=ScreenId.Battle)return TabletopTransitionKind.Fade;
  if(to==ScreenId.Map)return TabletopTransitionKind.ScrollUnfurl;
  if(to==ScreenId.Battle)return TabletopTransitionKind.Fade;
  if(IsBook(from)&&IsBook(to))return TabletopTransitionKind.PageTurn;
  return TabletopTransitionKind.Fade;
 }
 bool IsBook(ScreenId value)=>value==ScreenId.Character||value==ScreenId.Status||value==ScreenId.Vault||value==ScreenId.Reward||value==ScreenId.Shop||value==ScreenId.Event||value==ScreenId.Compendium||value==ScreenId.GameOver;

 IEnumerator Play(TabletopTransitionKind kind){transitionRoot.style.display=DisplayStyle.Flex;ResetLayers();const float duration=.54f;float elapsed=0f;while(elapsed<duration){elapsed+=Time.unscaledDeltaTime;float t=Mathf.Clamp01(elapsed/duration),ease=1f-Mathf.Pow(1f-t,3f);ApplyFrame(kind,ease);yield return null;}HideTransition();transitionRoutine=null;}
 void ResetLayers(){dim.style.display=DisplayStyle.Flex;dim.style.opacity=.68f;leftPaper.style.display=DisplayStyle.None;rightPaper.style.display=DisplayStyle.None;scrollPaper.style.display=DisplayStyle.None;battleShade.style.display=DisplayStyle.None;}
 void ApplyFrame(TabletopTransitionKind kind,float t){dim.style.opacity=Mathf.Lerp(.68f,0f,t);if(kind==TabletopTransitionKind.PageTurn){leftPaper.style.display=DisplayStyle.Flex;rightPaper.style.display=DisplayStyle.Flex;float remaining=1f-t;leftPaper.style.width=Length.Percent(50f*remaining);leftPaper.style.left=Length.Percent(50f-50f*remaining);rightPaper.style.width=Length.Percent(50f*remaining);rightPaper.style.left=Length.Percent(50f);leftPaper.style.opacity=remaining;rightPaper.style.opacity=remaining;}else if(kind==TabletopTransitionKind.ScrollUnfurl){scrollPaper.style.display=DisplayStyle.Flex;float remaining=1f-t;scrollPaper.style.width=Length.Percent(88f);scrollPaper.style.left=Length.Percent(6f);scrollPaper.style.height=Length.Percent(82f*remaining);scrollPaper.style.top=Length.Percent(9f+41f*t);scrollPaper.style.opacity=remaining;}else if(kind==TabletopTransitionKind.BattleTable){battleShade.style.display=DisplayStyle.Flex;battleShade.style.opacity=1f-t;battleShade.style.left=Length.Percent(48f*t);battleShade.style.right=Length.Percent(48f*t);}}
 void HideTransition(){if(transitionRoot!=null)transitionRoot.style.display=DisplayStyle.None;}
}
}
