using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire {
public sealed partial class PackspireUiFoundation {
 static readonly string[] CharacterStyleSheets={
  "UI/PackspirePolish","UI/PackspirePopDark","UI/PackspireManagement",
  "UI/PackspireMeta","UI/PackspireOrnaments","UI/PackspireRoster",
  "UI/PackspireMisprintCommon","UI/PackspireCharacter"
 };
 static readonly string[] HubStyleSheets={
  "UI/PackspireRoster","UI/PackspirePolish","UI/PackspirePopDark",
  "UI/PackspireBattle","UI/PackspireHub","UI/PackspireOrnaments"
 };
 static readonly string[] StatusStyleSheets={
  "UI/PackspirePolish","UI/PackspirePopDark","UI/PackspireManagement",
  "UI/PackspireMeta","UI/PackspireOrnaments","UI/PackspireManagementV3",
  "UI/PackspireMisprintCommon"
 };
 static readonly string[] VaultStyleSheets={
  "UI/PackspirePolish","UI/PackspirePopDark","UI/PackspireManagement",
  "UI/PackspireMeta","UI/PackspireOrnaments","UI/PackspireBattle",
  "UI/PackspireManagementV3"
 };
 static readonly string[] CompendiumStyleSheets={
  "UI/PackspirePolish","UI/PackspirePopDark","UI/PackspireManagement",
  "UI/PackspireMeta","UI/PackspireOrnaments","UI/PackspireManagementV3",
  "UI/PackspireVaultCodexFinal","UI/PackspireMisprintCommon"
 };
 static readonly string[] HeirloomStyleSheets={
  "UI/PackspirePolish","UI/PackspirePopDark","UI/PackspireManagement",
  "UI/PackspireMeta","UI/PackspireOrnaments","UI/PackspireManagementV3",
  "UI/PackspireMisprintCommon"
 };
 static readonly string[] FactionStyleSheets={
  "UI/PackspirePolish","UI/PackspirePopDark","UI/PackspireManagement",
  "UI/PackspireMeta","UI/PackspireOrnaments","UI/PackspireManagementV3",
  "UI/PackspireMisprintCommon"
 };
 static readonly string[] ExpeditionStyleSheets={
  "UI/PackspireRoute","UI/PackspirePolish","UI/PackspirePopDark",
  "UI/PackspireManagement","UI/PackspireMeta","UI/PackspireOrnaments",
  "UI/PackspireManagementV3","UI/PackspireMisprintCommon"
 };
 static readonly string[] PackingStyleSheets={
  "UI/PackspirePolish","UI/PackspirePopDark","UI/PackspireOrnaments",
  "UI/PackspireManagementV3","UI/PackspirePacking",
  "UI/PackspireMisprintCommon"
 };
 static readonly string[] GridBoardStyleSheets={
  "UI/PackspireRoute","UI/PackspireBattle","UI/PackspirePolish",
  "UI/PackspireGridBoard","UI/PackspireMisprintCommon"
 };
 static readonly string[] CourierRouteStyleSheets={
  "UI/PackspirePolish","UI/PackspireMisprintCommon",
  "UI/PackspireDocketCard","UI/PackspireCourierRoute"
 };
 static readonly string[] BattleStyleSheets={
  "UI/PackspireRoute","UI/PackspireGridBoard","UI/PackspirePolish",
  "UI/PackspireManagementV3","UI/PackspireBattle",
  "UI/PackspireMisprintCommon"
 };
 static readonly string[] CommerceStyleSheets={
  "UI/PackspirePolish","UI/PackspirePopDark","UI/PackspireManagement",
  "UI/PackspireMeta","UI/PackspireOrnaments","UI/PackspireManagementV3",
  "UI/PackspireCommerce","UI/PackspireMisprintCommon"
 };
 static readonly string[] EventStyleSheets={
  "UI/PackspirePolish","UI/PackspirePopDark","UI/PackspireRoute",
  "UI/PackspireMisprintCommon"
 };

 void ApplyScreenStyleSheets(ScreenId screen){
  if(screenRoot==null)return;
  RemoveAllStyleSheets(screenRoot);
  foreach(var path in StyleSheetsFor(screen))
   AddStyleSheet(screenRoot,path);
 }

 static IReadOnlyList<string> StyleSheetsFor(ScreenId screen)=>screen switch{
  ScreenId.Character=>CharacterStyleSheets,
  ScreenId.Hub=>HubStyleSheets,
  ScreenId.Status=>StatusStyleSheets,
  ScreenId.Vault=>VaultStyleSheets,
  ScreenId.Compendium=>CompendiumStyleSheets,
  ScreenId.Heirloom=>HeirloomStyleSheets,
  ScreenId.Faction=>FactionStyleSheets,
  ScreenId.Expedition=>ExpeditionStyleSheets,
  ScreenId.Pack=>PackingStyleSheets,
  ScreenId.Route=>CourierRouteStyleSheets,
  ScreenId.GridBoard=>GridBoardStyleSheets,
  ScreenId.Battle=>BattleStyleSheets,
  ScreenId.Reward=>CommerceStyleSheets,
  ScreenId.Shop=>CommerceStyleSheets,
  ScreenId.GameOver=>CommerceStyleSheets,
  ScreenId.GameClear=>CommerceStyleSheets,
  ScreenId.Event=>EventStyleSheets,
  _=>CompendiumStyleSheets
 };

 static StyleSheet AddStyleSheet(VisualElement target,string resourcePath){
  if(target==null||string.IsNullOrEmpty(resourcePath))return null;
#if UNITY_EDITOR
  // UI assets are edited while Play Mode is running during visual iteration.
  // Reload the current imported asset instead of rebuilding from a stale cache entry.
  PackspireResources.Invalidate<StyleSheet>(resourcePath);
#endif
  var sheet=PackspireResources.Load<StyleSheet>(resourcePath);
  if(sheet==null){
   Debug.LogError($"Missing UI style sheet: {resourcePath}");
   return null;
  }
  for(int index=0;index<target.styleSheets.count;index++)
   if(target.styleSheets[index]==sheet)return sheet;
  target.styleSheets.Add(sheet);
  return sheet;
 }

 static void RemoveAllStyleSheets(VisualElement target){
  if(target==null)return;
  for(int index=target.styleSheets.count-1;index>=0;index--)
   target.styleSheets.Remove(target.styleSheets[index]);
 }

 VisualElement CloneView(string resourcePath,string rootClass){
#if UNITY_EDITOR
  PackspireResources.Invalidate<VisualTreeAsset>(resourcePath);
#endif
  var template=PackspireResources.Load<VisualTreeAsset>(resourcePath);
  if(template==null){
   Debug.LogError($"Missing UI view template: {resourcePath}");
   return null;
  }
  var view=Container(rootClass);
  template.CloneTree(view);
  return view;
 }

 T RequireViewElement<T>(VisualElement view,string name) where T:VisualElement{
  var element=view?.Q<T>(name);
  if(element!=null)return element;
  throw new InvalidOperationException(
   $"UI template '{view?.name??"<null>"}' is missing required {typeof(T).Name} '{name}'."
  );
 }
}
}
