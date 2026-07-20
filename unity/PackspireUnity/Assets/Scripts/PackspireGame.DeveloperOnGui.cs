using UnityEngine;

namespace Packspire {
public partial class PackspireGame : MonoBehaviour {
 /// <summary>IMGUI dev overlay for Battle / legacy Map (UIToolkit sits under OnGUI).</summary>
 void DrawLegacyDeveloperOverlay(){
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
  return;
#endif
  var accessStyle=new GUIStyle(button){fontSize=Fs(13),alignment=TextAnchor.MiddleCenter,padding=new RectOffset(6,6,6,6)};
  if(!developerPanel){
   if(GUI.Button(new Rect(14,Screen.height-Us(50),Us(76),Us(38)),"F10 DEV",accessStyle))
    UiToggleDeveloperPanel();
   return;
  }

  GUI.enabled=true;
  GUI.color=new Color(0f,0f,0f,.48f);
  GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);
  GUI.color=Color.white;

  float panelW=Us(390),panelH=Us(420);
  Rect panel=new(Screen.width-panelW-Us(14),Us(14),panelW,panelH);
  GUI.Box(panel,GUIContent.none,new GUIStyle(GUI.skin.box){padding=new RectOffset(Fs(18),Fs(18),Fs(16),Fs(16))});
  float y=panel.y+Us(18);
  GUI.Label(new Rect(panel.x+Us(18),y,panel.width-Us(36),Us(28)),"開発者メニュー",new GUIStyle(header){fontSize=Fs(20),fontStyle=FontStyle.Bold});
  y+=Us(32);
  GUI.Label(new Rect(panel.x+Us(18),y,panel.width-Us(36),Us(36)),"F10で開閉。閉じると直前の画面へ戻ります。",new GUIStyle(body){fontSize=Fs(13),wordWrap=true});
  y+=Us(44);

  var devButton=new GUIStyle(button){fontSize=Fs(14),alignment=TextAnchor.MiddleCenter};
  float btnW=(panel.width-Us(48))*.5f,btnH=Us(36),x0=panel.x+Us(18),x1=x0+btnW+Us(12);
  if(LegacyDevButton(new Rect(x0,y,btnW,btnH),"拠点",devButton)){UiNavigate(ScreenId.Hub);UiDevCloseWithoutRestore();PackspireUiFoundation.Instance?.ForceRefreshScreen();}
  if(LegacyDevButton(new Rect(x1,y,btnW,btnH),"遠征準備",devButton)){UiNavigate(ScreenId.Expedition);UiDevCloseWithoutRestore();PackspireUiFoundation.Instance?.ForceRefreshScreen();}
  y+=btnH+Us(10);
  if(LegacyDevButton(new Rect(x0,y,btnW,btnH),"遠征マップ",devButton)){UiDevOpenExplorationMap();UiDevCloseWithoutRestore();PackspireUiFoundation.Instance?.ForceRefreshScreen();}
  if(LegacyDevButton(new Rect(x1,y,btnW,btnH),"戦闘（カード）",devButton)){UiDevOpenOldBattle();UiDevCloseWithoutRestore();PackspireUiFoundation.Instance?.ForceRefreshScreen();}
  y+=btnH+Us(10);
  if(LegacyDevButton(new Rect(x0,y,btnW,btnH),"荷造り",devButton)){UiNavigate(ScreenId.Pack);UiDevCloseWithoutRestore();PackspireUiFoundation.Instance?.ForceRefreshScreen();}
  if(LegacyDevButton(new Rect(x1,y,btnW,btnH),"保管庫",devButton)){UiNavigate(ScreenId.Vault);UiDevCloseWithoutRestore();PackspireUiFoundation.Instance?.ForceRefreshScreen();}
  y+=btnH+Us(10);
  if(LegacyDevButton(new Rect(x0,y,btnW,btnH),"役職",devButton)){UiNavigate(ScreenId.Status);UiDevCloseWithoutRestore();PackspireUiFoundation.Instance?.ForceRefreshScreen();}
  if(LegacyDevButton(new Rect(x1,y,btnW,btnH),"勢力",devButton)){UiNavigate(ScreenId.Faction);UiDevCloseWithoutRestore();PackspireUiFoundation.Instance?.ForceRefreshScreen();}
  y+=btnH+Us(10);
  if(LegacyDevButton(new Rect(x0,y,btnW,btnH),"図鑑",devButton)){UiNavigate(ScreenId.Compendium);UiDevCloseWithoutRestore();PackspireUiFoundation.Instance?.ForceRefreshScreen();}
  if(LegacyDevButton(new Rect(x1,y,btnW,btnH),"キャラ選択",devButton)){UiNavigate(ScreenId.Character);UiDevCloseWithoutRestore();PackspireUiFoundation.Instance?.ForceRefreshScreen();}
  y+=btnH+Us(18);
  if(LegacyDevButton(new Rect(panel.x+Us(18),y,panel.width-Us(36),Us(40)),"閉じる（直前へ戻る）",devButton))
   UiToggleDeveloperPanel();
 }

 static bool LegacyDevButton(Rect rect,string label,GUIStyle style)=>GUI.Button(rect,label,style);
}
}
