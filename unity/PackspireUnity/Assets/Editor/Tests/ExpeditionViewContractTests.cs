using NUnit.Framework;
using UnityEditor;
using UnityEngine.UIElements;

public sealed class ExpeditionViewContractTests {
 [Test]
 public void ExpeditionView_HasCurrentDispatchContract(){
  var view=AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
   "Assets/Resources/UI/PackspireExpeditionView.uxml"
  );
  Assert.That(view,Is.Not.Null);
  var root=view.CloneTree();
  Assert.That(root.Q<VisualElement>("expedition-background"),Is.Not.Null);
  Assert.That(root.Q<VisualElement>("expedition-header"),Is.Not.Null);
  Assert.That(root.Q<VisualElement>("expedition-art-host"),Is.Not.Null);
  Assert.That(root.Q<VisualElement>("expedition-destination-column"),Is.Not.Null);
  Assert.That(root.Q<ScrollView>("expedition-destination-scroll"),Is.Not.Null);
  Assert.That(root.Q<VisualElement>("expedition-destination-list"),Is.Not.Null);
  Assert.That(root.Q<VisualElement>("expedition-detail-column"),Is.Not.Null);
  Assert.That(root.Q<ScrollView>("expedition-detail-scroll"),Is.Not.Null);
  Assert.That(root.Q<VisualElement>("expedition-footer"),Is.Not.Null);
  Assert.That(root.Q<VisualElement>(className:"ps-mgmt-header"),Is.Null);
  Assert.That(root.Q<VisualElement>(className:"ps-misprint-header-copy"),Is.Null);
  Assert.That(root.Q<VisualElement>(className:"ps-misprint-mark"),Is.Null);
  Assert.That(root.Q<VisualElement>(className:"ps-misprint-register"),Is.Null);
 }

 [Test]
 public void ExpeditionView_HasDedicatedStyleSheet(){
  var style=AssetDatabase.LoadAssetAtPath<StyleSheet>(
   "Assets/Resources/UI/PackspireExpedition.uss"
  );
  Assert.That(style,Is.Not.Null);
 }
}
