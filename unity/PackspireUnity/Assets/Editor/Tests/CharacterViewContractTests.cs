using NUnit.Framework;
using UnityEditor;
using UnityEngine.UIElements;

public sealed class CharacterViewContractTests {
 [Test]
 public void CharacterView_HasCurrentRosterContract(){
  var view=AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
   "Assets/Resources/UI/PackspireCharacterView.uxml"
  );
  Assert.That(view,Is.Not.Null);
  var root=view.CloneTree();
  Assert.That(root.Q<VisualElement>("character-background"),Is.Not.Null);
  Assert.That(root.Q<VisualElement>("character-header"),Is.Not.Null);
  Assert.That(root.Q<ScrollView>("character-reel-scroll"),Is.Not.Null);
  Assert.That(root.Q<VisualElement>("character-reel"),Is.Not.Null);
  Assert.That(root.Q<VisualElement>("character-art-host"),Is.Not.Null);
  Assert.That(root.Q<VisualElement>("character-detail-column"),Is.Not.Null);
  Assert.That(root.Q<ScrollView>("character-detail-scroll"),Is.Not.Null);
  Assert.That(root.Q<Button>("character-confirm"),Is.Not.Null);
  Assert.That(root.Q<VisualElement>(className:"ps-misprint-header-copy"),Is.Null);
  Assert.That(root.Q<Label>(className:"ps-misprint-header-title"),Is.Null);
  Assert.That(root.Q<VisualElement>(className:"ps-misprint-register"),Is.Null);
 }

 [Test]
 public void CharacterView_HasDedicatedStyleSheet(){
  var style=AssetDatabase.LoadAssetAtPath<StyleSheet>(
   "Assets/Resources/UI/PackspireCharacter.uss"
  );
  Assert.That(style,Is.Not.Null);
 }
}
