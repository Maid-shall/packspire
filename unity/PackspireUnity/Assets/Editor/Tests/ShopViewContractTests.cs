using NUnit.Framework;
using UnityEditor;
using UnityEngine.UIElements;

public sealed class ShopViewContractTests {
 [Test]
 public void ShopView_HasCurrentCommerceContract(){
  var view=AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
   "Assets/Resources/UI/PackspireShopView.uxml"
  );
  Assert.That(view,Is.Not.Null);
  var root=view.CloneTree();
  Assert.That(root.Q<VisualElement>("shop-background"),Is.Not.Null);
  Assert.That(root.Q<VisualElement>("shop-header"),Is.Not.Null);
  Assert.That(root.Q<VisualElement>("shop-merchant-column"),Is.Not.Null);
  Assert.That(root.Q<VisualElement>("shop-list-column"),Is.Not.Null);
  Assert.That(root.Q<ScrollView>("shop-product-scroll"),Is.Not.Null);
  Assert.That(root.Q<VisualElement>("shop-product-grid"),Is.Not.Null);
  Assert.That(root.Q<VisualElement>("shop-detail-column"),Is.Not.Null);
  Assert.That(root.Q<ScrollView>("shop-detail-scroll"),Is.Not.Null);
  Assert.That(root.Q<VisualElement>("shop-transaction-column"),Is.Not.Null);
  Assert.That(root.Q<VisualElement>(className:"ps-misprint-header-copy"),Is.Null);
  Assert.That(root.Q<VisualElement>(className:"ps-misprint-mark"),Is.Null);
  Assert.That(root.Q<VisualElement>(className:"ps-misprint-register"),Is.Null);
 }

 [Test]
 public void ShopView_HasDedicatedStyleSheet(){
  var style=AssetDatabase.LoadAssetAtPath<StyleSheet>(
   "Assets/Resources/UI/PackspireShop.uss"
  );
  Assert.That(style,Is.Not.Null);
 }
}
