using NUnit.Framework;
using UnityEditor;
using UnityEngine.UIElements;

public sealed class HeirloomViewContractTests {
 [Test]
 public void HeirloomView_HasCurrentTwoColumnContract(){
  var view=AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
   "Assets/Resources/UI/PackspireHeirloomView.uxml"
  );
  Assert.That(view,Is.Not.Null);
  var root=view.CloneTree();
  var background=root.Q<VisualElement>("heirloom-background");
  var header=root.Q<VisualElement>("heirloom-header");
  var slot=root.Q<VisualElement>("heirloom-slot-host");
  var portrait=root.Q<VisualElement>("heirloom-portrait-column");
  var growth=root.Q<VisualElement>("heirloom-growth-column");
  var scroll=root.Q<ScrollView>("heirloom-growth-scroll");
  var body=root.Q<VisualElement>("heirloom-growth-body");

  Assert.That(background,Is.Not.Null);
  Assert.That(header,Is.Not.Null);
  Assert.That(slot,Is.Not.Null);
  Assert.That(portrait,Is.Not.Null);
  Assert.That(growth,Is.Not.Null);
  Assert.That(scroll,Is.Not.Null);
  Assert.That(body,Is.Not.Null);
  Assert.That(portrait.parent,Is.SameAs(growth.parent));
  Assert.That(portrait.parent.IndexOf(portrait),Is.LessThan(portrait.parent.IndexOf(growth)));
  Assert.That(root.Q<VisualElement>(className:"ps-misprint-header-copy"),Is.Null);
  Assert.That(root.Q<Label>(className:"ps-misprint-header-title"),Is.Null);
 }

 [Test]
 public void HeirloomView_HasDedicatedStyleSheet(){
  var style=AssetDatabase.LoadAssetAtPath<StyleSheet>(
   "Assets/Resources/UI/PackspireHeirloom.uss"
  );
  Assert.That(style,Is.Not.Null);
 }
}
