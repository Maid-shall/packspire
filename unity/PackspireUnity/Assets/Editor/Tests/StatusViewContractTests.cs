using NUnit.Framework;
using UnityEditor;
using UnityEngine.UIElements;

public sealed class StatusViewContractTests {
 [Test]
 public void StatusView_HasCurrentThreeColumnContract(){
  var view=AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
   "Assets/Resources/UI/PackspireStatusView.uxml"
  );
  Assert.That(view,Is.Not.Null);
  var root=view.CloneTree();
  var layout=root.Q<VisualElement>("status-layout");
  var character=root.Q<VisualElement>("status-character-column");
  var roles=root.Q<VisualElement>("status-roles-column");
  var detail=root.Q<VisualElement>("status-detail-column");
  var appointment=root.Q<VisualElement>("status-appointment");

  Assert.That(layout,Is.Not.Null);
  Assert.That(layout.ClassListContains("ps-status-view"),Is.True);
  Assert.That(layout.ClassListContains("ps-management-v3"),Is.False);
  Assert.That(layout.ClassListContains("ps-status-v2"),Is.False);
  Assert.That(character,Is.Not.Null);
  Assert.That(roles,Is.Not.Null);
  Assert.That(detail,Is.Not.Null);
  Assert.That(appointment,Is.Not.Null);

  var workspaceTop=roles.parent;
  Assert.That(workspaceTop,Is.SameAs(detail.parent));
  Assert.That(workspaceTop.IndexOf(roles),Is.LessThan(workspaceTop.IndexOf(detail)));
 }

 [Test]
 public void StatusView_HasDedicatedStyleSheet(){
  var style=AssetDatabase.LoadAssetAtPath<StyleSheet>(
   "Assets/Resources/UI/PackspireStatus.uss"
  );
  Assert.That(style,Is.Not.Null);
 }
}
