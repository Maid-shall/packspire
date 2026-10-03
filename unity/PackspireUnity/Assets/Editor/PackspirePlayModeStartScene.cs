using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Packspire.Editor
{
    /// <summary>
    /// Keeps Play Mode anchored to the product entry scene. Prototype scenes are
    /// entered explicitly from the in-game developer menu instead of becoming the
    /// next editor startup screen merely because they were inspected last.
    /// </summary>
    [InitializeOnLoad]
    internal static class PackspirePlayModeStartScene
    {
        private const string MainScenePath = "Assets/Scenes/Main.unity";

        static PackspirePlayModeStartScene()
        {
            EditorApplication.delayCall += Apply;
        }

        private static void Apply()
        {
            SceneAsset mainScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(MainScenePath);
            if (mainScene == null)
            {
                Debug.LogError($"Packspire Play Mode start scene is missing: {MainScenePath}");
                return;
            }

            if (EditorSceneManager.playModeStartScene != mainScene)
            {
                EditorSceneManager.playModeStartScene = mainScene;
            }
        }
    }
}
