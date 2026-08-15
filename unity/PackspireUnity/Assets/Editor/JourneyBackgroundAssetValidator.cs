using UnityEditor;
using UnityEngine;

namespace Packspire.Editor
{
    internal static class JourneyBackgroundAssetValidator
    {
        private const string BackgroundRoot =
            "Assets/Resources/Art/JourneyPrototype/Backgrounds";
        private const int CanonicalWidth = 1920;
        private const int CanonicalHeight = 1080;
        private const float CanonicalPixelsPerUnit = 100f;

        [MenuItem("Tools/Packspire/Validate Journey Background Assets")]
        private static void ValidateAll()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { BackgroundRoot });
            int validated = 0;
            int legacyCanvasCount = 0;
            int errorCount = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null || importer.textureType != TextureImporterType.Sprite)
                    continue;

                validated++;
                if (importer.spriteImportMode != SpriteImportMode.Single)
                {
                    Debug.LogError($"[Journey BG] Sprite Mode must be Single: {path}");
                    errorCount++;
                }
                if (Mathf.Abs(importer.spritePixelsPerUnit - CanonicalPixelsPerUnit) > .01f)
                {
                    Debug.LogError($"[Journey BG] PPU must be 100: {path}");
                    errorCount++;
                }
                if ((importer.spritePivot - new Vector2(.5f, .5f)).sqrMagnitude > .0001f)
                {
                    Debug.LogError($"[Journey BG] Pivot must be centered: {path}");
                    errorCount++;
                }
                if (importer.filterMode != FilterMode.Bilinear)
                {
                    Debug.LogError($"[Journey BG] Filter Mode must be Bilinear: {path}");
                    errorCount++;
                }
                if (importer.wrapMode != TextureWrapMode.Clamp)
                {
                    Debug.LogError($"[Journey BG] Wrap Mode must be Clamp: {path}");
                    errorCount++;
                }
                if (importer.maxTextureSize < 2048)
                {
                    Debug.LogError($"[Journey BG] Max Size must be at least 2048: {path}");
                    errorCount++;
                }

                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (texture != null &&
                    (texture.width != CanonicalWidth || texture.height != CanonicalHeight))
                {
                    legacyCanvasCount++;
                }
            }

            string summary =
                $"[Journey BG] validated={validated}, legacyCanvas={legacyCanvasCount}, errors={errorCount}. " +
                $"Canonical canvas is {CanonicalWidth}x{CanonicalHeight}.";
            if (errorCount > 0) Debug.LogError(summary);
            else Debug.Log(summary);
        }
    }
}
