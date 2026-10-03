#if UNITY_EDITOR
using Packspire;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PackspireContentDatabase))]
public sealed class PackspireContentDatabaseEditor : Editor {
 public override void OnInspectorGUI(){
  serializedObject.Update();
  EditorGUILayout.HelpBox(
   "ゲームのマスターデータです。カテゴリ別アセットを編集し、セーブデータからは安定したIDだけを参照します。",
   MessageType.Info);

  using(new EditorGUI.DisabledScope(true))
   EditorGUILayout.PropertyField(serializedObject.FindProperty("schemaVersion"));
  EditorGUILayout.PropertyField(serializedObject.FindProperty("cardContent"),new GUIContent("カード・効果"));
  EditorGUILayout.PropertyField(serializedObject.FindProperty("itemContent"),new GUIContent("装備・収納"));
  EditorGUILayout.PropertyField(serializedObject.FindProperty("actorContent"),new GUIContent("キャラクター・敵"));
  EditorGUILayout.PropertyField(serializedObject.FindProperty("worldContent"),new GUIContent("ダンジョン・施設"));
  serializedObject.ApplyModifiedProperties();

  EditorGUILayout.Space(8);
  if(!GUILayout.Button("データ検証"))return;
  var database=(PackspireContentDatabase)target;
  var report=PackspireContent.Validate(database);
  foreach(var warning in report.warnings)Debug.LogWarning(warning,database);
  foreach(var error in report.errors)Debug.LogError(error,database);
  EditorUtility.DisplayDialog("PACKSPIRE",report.Summary,"OK");
 }
}
#endif
