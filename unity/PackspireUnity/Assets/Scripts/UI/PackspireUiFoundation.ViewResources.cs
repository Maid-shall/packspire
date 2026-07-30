using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire {
public sealed partial class PackspireUiFoundation {
 static readonly string[] CharacterStyleSheets={
  "UI/PackspireRoster","UI/PackspirePolish","UI/PackspirePopDark",
  "UI/PackspireManagement","UI/PackspireMeta","UI/PackspireOrnaments"
 };
 static readonly string[] HubStyleSheets={
  "UI/PackspireRoster","UI/PackspirePolish","UI/PackspirePopDark",
  "UI/PackspireHub","UI/PackspireOrnaments"
 };
 static readonly string[] StatusStyleSheets={
  "UI/PackspirePolish","UI/PackspirePopDark","UI/PackspireManagement",
  "UI/PackspireMeta","UI/PackspireOrnaments","UI/PackspireManagementV3"
 };
 static readonly string[] VaultStyleSheets={
  "UI/PackspirePolish","UI/PackspirePopDark","UI/PackspireManagement",
  "UI/PackspireMeta","UI/PackspireOrnaments","UI/PackspireBattle",
  "UI/PackspireManagementV3"
 };
 static readonly string[] CompendiumStyleSheets={
  "UI/PackspirePolish","UI/PackspirePopDark","UI/PackspireManagement",
  "UI/PackspireMeta","UI/PackspireOrnaments","UI/PackspireManagementV3",
  "UI/PackspireVaultCodexFinal"
 };
 static readonly string[] HeirloomStyleSheets={
  "UI/PackspirePolish","UI/PackspirePopDark","UI/PackspireManagement",
  "UI/PackspireMeta","UI/PackspireOrnaments","UI/PackspireManagementV3"
 };
 static readonly string[] FactionStyleSheets={
  "UI/PackspirePolish","UI/PackspirePopDark","UI/PackspireManagement",
  "UI/PackspireMeta","UI/PackspireOrnaments","UI/PackspireManagementV3"
 };
 static readonly string[] ExpeditionStyleSheets={
  "UI/PackspireRoute","UI/PackspirePolish","UI/PackspirePopDark",
  "UI/PackspireManagement","UI/PackspireMeta","UI/PackspireOrnaments",
  "UI/PackspireManagementV3"
 };
 static readonly string[] PackingStyleSheets={
  "UI/PackspirePacking","UI/PackspirePolish","UI/PackspirePopDark",
  "UI/PackspireOrnaments","UI/PackspireManagementV3"
 };
 static readonly string[] GridBoardStyleSheets={
  "UI/PackspireRoute","UI/PackspireGridBoard","UI/PackspireBattle",
  "UI/PackspirePolish"
 };
 static readonly string[] BattleStyleSheets={
  "UI/PackspireRoute","UI/PackspireGridBoard","UI/PackspireBattle",
  "UI/PackspirePolish","UI/PackspireManagementV3"
 };
 static readonly string[] CommerceStyleSheets={
  "UI/PackspirePolish","UI/PackspirePopDark","UI/PackspireManagement",
  "UI/PackspireMeta","UI/PackspireCommerce","UI/PackspireOrnaments",
  "UI/PackspireManagementV3"
 };
 static readonly string[] EventStyleSheets={
  "UI/PackspireRoute","UI/PackspirePolish","UI/PackspirePopDark"
 };

 void ApplyScreenStyleSheets(ScreenId screen){
  if(screenRoot==null)return;
  RemovePackspireStyleSheets(screenRoot);
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
  ScreenId.GridBoard=>GridBoardStyleSheets,
  ScreenId.Battle=>BattleStyleSheets,
  ScreenId.Reward=>CommerceStyleSheets,
  ScreenId.Shop=>CommerceStyleSheets,
  ScreenId.GameOver=>CommerceStyleSheets,
  ScreenId.GameClear=>CommerceStyleSheets,
  ScreenId.Event=>EventStyleSheets,
  _=>CompendiumStyleSheets
 };

 static void AddStyleSheet(VisualElement target,string resourcePath){
  if(target==null||string.IsNullOrEmpty(resourcePath))return;
  var sheet=PackspireResources.Load<StyleSheet>(resourcePath);
  if(sheet==null){
   Debug.LogError($"Missing UI style sheet: {resourcePath}");
   return;
  }
  for(int index=0;index<target.styleSheets.count;index++)
   if(target.styleSheets[index]==sheet)return;
  target.styleSheets.Add(sheet);
 }

 static void RemovePackspireStyleSheets(VisualElement target){
  if(target==null)return;
  for(int index=target.styleSheets.count-1;index>=0;index--){
   var sheet=target.styleSheets[index];
   if(sheet!=null&&sheet.name.StartsWith("Packspire",StringComparison.Ordinal))
    target.styleSheets.Remove(sheet);
  }
 }

 VisualElement CloneView(string resourcePath,string rootClass){
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
