using NUnit.Framework;
using UnityEditor;
using UnityEngine.UIElements;

public sealed class PackingViewContractTests {
 [Test]
 public void PackingView_HasCurrentLoadoutContract(){
  var view=AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
   "Assets/Resources/UI/PackspirePackingView.uxml"
  );
  Assert.That(view,Is.Not.Null);
  var root=view.CloneTree();
  Assert.That(root.Q<VisualElement>("packing-background"),Is.Not.Null);
  Assert.That(root.Q<VisualElement>("packing-top"),Is.Not.Null);
  Assert.That(root.Q<VisualElement>("packing-body"),Is.Not.Null);
  Assert.That(root.Q<VisualElement>("packing-left"),Is.Not.Null);
  Assert.That(root.Q<ScrollView>("packing-equip-scroll"),Is.Not.Null);
  Assert.That(root.Q<VisualElement>("packing-equip-grid"),Is.Not.Null);
  Assert.That(root.Q<VisualElement>("packing-center"),Is.Not.Null);
  Assert.That(root.Q<VisualElement>("packing-kiln"),Is.Not.Null);
  Assert.That(root.Q<VisualElement>("packing-right-shell"),Is.Not.Null);
  Assert.That(root.Q<ScrollView>("packing-right-scroll"),Is.Not.Null);
  Assert.That(root.Q<VisualElement>(className:"ps-misprint-mark"),Is.Null);
  Assert.That(root.Q<VisualElement>(className:"ps-misprint-register"),Is.Null);
  Assert.That(root.Q<VisualElement>(className:"ps-misprint-decor"),Is.Null);
 }

 [Test]
 public void PackingView_HasDedicatedStyleSheet(){
  var style=AssetDatabase.LoadAssetAtPath<StyleSheet>(
   "Assets/Resources/UI/PackspirePacking.uss"
  );
  Assert.That(style,Is.Not.Null);
 }
}
