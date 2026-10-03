using NUnit.Framework;
using UnityEditor;
using UnityEngine.UIElements;

public sealed class FactionViewContractTests {
 [Test]
 public void FactionView_HasCurrentRegistryContract(){
  var view=AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
   "Assets/Resources/UI/PackspireFactionView.uxml"
  );
  Assert.That(view,Is.Not.Null);
  var root=view.CloneTree();
  var background=root.Q<VisualElement>("faction-background");
  var header=root.Q<VisualElement>("faction-header");
  var emissary=root.Q<VisualElement>("faction-emissary-host");
  var registry=root.Q<VisualElement>("faction-graph-column");
  var nodes=root.Q<VisualElement>("faction-graph-nodes");
  var detail=root.Q<VisualElement>("faction-detail-surface");
  var scroll=root.Q<ScrollView>("faction-detail-scroll");

  Assert.That(background,Is.Not.Null);
  Assert.That(header,Is.Not.Null);
  Assert.That(emissary,Is.Not.Null);
  Assert.That(registry,Is.Not.Null);
  Assert.That(nodes,Is.Not.Null);
  Assert.That(detail,Is.Not.Null);
  Assert.That(scroll,Is.Not.Null);
  Assert.That(root.Q<VisualElement>(className:"ps-misprint-header-copy"),Is.Null);
  Assert.That(root.Q<Label>(className:"ps-misprint-header-title"),Is.Null);
  Assert.That(root.Q<VisualElement>(className:"ps-misprint-register"),Is.Null);
 }

 [Test]
 public void FactionView_HasDedicatedStyleSheet(){
  var style=AssetDatabase.LoadAssetAtPath<StyleSheet>(
   "Assets/Resources/UI/PackspireFaction.uss"
  );
  Assert.That(style,Is.Not.Null);
 }
}
