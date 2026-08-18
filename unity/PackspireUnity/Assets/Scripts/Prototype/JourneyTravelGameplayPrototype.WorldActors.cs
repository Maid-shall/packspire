using System;
using System.Linq;
using UnityEngine;

namespace Packspire
{
    public sealed partial class JourneyTravelGameplayPrototype
    {
        private void BuildWorldActors()
        {
            LoadEnemyBattleFrames();
            Sprite enemySprite = enemyBattleFrames.Length == 6
                ? enemyBattleFrames[(int)EnemyBattlePose.Idle]
                : PackspireResources.Load<Sprite>(encounterProfile.fallbackSpriteResource);
            if (enemySprite != null)
            {
                string actorName = string.IsNullOrWhiteSpace(encounterProfile.displayName)
                    ? "Journey Enemy"
                    : encounterProfile.displayName;
                GameObject enemyObject = new GameObject($"{actorName} Actor");
                enemyRenderer = enemyObject.AddComponent<SpriteRenderer>();
                enemyRenderer.sprite = enemySprite;
                JourneyPresentationConfig.Sort(
                    enemyRenderer,
                    JourneyPresentationConfig.ActorSortingLayer,
                    10);
                enemyObject.transform.position = EnemyBattleStartPosition;
                enemyObject.transform.localScale = Vector3.one * EnemyBattleScale;
                enemyRenderer.enabled = false;

                Sprite shadowSprite = CreateBattleActorShadowSprite();
                enemyShadowRenderer = CreateBattleActorShadow(
                    $"{actorName} Ground Shadow",
                    shadowSprite,
                    EnemyBattleStartPosition,
                    new Vector3(1.52f, .42f, 1f),
                    8);

                GameObject telegraphObject = new GameObject($"{actorName} Telegraph Silhouette");
                enemyTelegraphRenderer = telegraphObject.AddComponent<SpriteRenderer>();
                enemyTelegraphRenderer.sprite = enemySprite;
                JourneyPresentationConfig.Sort(
                    enemyTelegraphRenderer,
                    JourneyPresentationConfig.ActorSortingLayer,
                    9);
                telegraphObject.transform.position = enemyObject.transform.position;
                telegraphObject.transform.localScale = enemyObject.transform.localScale * 1.09f;
                enemyTelegraphRenderer.color = new Color(1f, 1f, 1f, 0f);
                enemyTelegraphRenderer.enabled = false;

                for (int previewIndex = 0; previewIndex < battlePreviewEnemyRenderers.Length; previewIndex++)
                {
                    GameObject previewEnemy = new GameObject($"Battle Layout Enemy {previewIndex + 2}");
                    SpriteRenderer previewRenderer = previewEnemy.AddComponent<SpriteRenderer>();
                    previewRenderer.sprite = enemySprite;
                    JourneyPresentationConfig.Sort(
                        previewRenderer,
                        JourneyPresentationConfig.ActorSortingLayer,
                        10 - previewIndex);
                    previewEnemy.transform.position = BattlePreviewEnemyPosition(previewIndex);
                    previewEnemy.transform.localScale = Vector3.one * (previewIndex == 0 ? .52f : .45f);
                    previewRenderer.color = previewIndex == 0
                        ? new Color(.84f, .9f, .94f, 1f)
                        : new Color(.72f, .77f, .82f, 1f);
                    previewRenderer.enabled = false;
                    battlePreviewEnemyRenderers[previewIndex] = previewRenderer;
                    battlePreviewEnemyShadowRenderers[previewIndex] = CreateBattleActorShadow(
                        $"Battle Layout Enemy {previewIndex + 2} Shadow",
                        shadowSprite,
                        BattlePreviewEnemyPosition(previewIndex),
                        new Vector3(previewIndex == 0 ? 1.28f : 1.12f, .38f, 1f),
                        7 - previewIndex);
                }
            }
            else
            {
                enemyRenderer = new GameObject("Missing Chibi Enemy").AddComponent<SpriteRenderer>();
                enemyRenderer.enabled = false;
            }

            GameObject parcel = new GameObject("Roadside Parcel");
            parcelRenderer = parcel.AddComponent<SpriteRenderer>();
            parcelRenderer.sprite = CreateParcelSprite();
            JourneyPresentationConfig.Sort(
                parcelRenderer,
                JourneyPresentationConfig.EffectSortingLayer,
                0);
            parcel.transform.localScale = Vector3.one * .72f;
            parcelRenderer.enabled = false;
        }

        private void LoadEnemyBattleFrames()
        {
            Sprite[] loaded = PackspireResources.LoadAll<Sprite>(encounterProfile.battleSheetResource);
            string[] orderedNames = encounterProfile.orderedBattlePoseNames;
            if (loaded == null || loaded.Length < orderedNames.Length)
            {
                enemyBattleFrames = Array.Empty<Sprite>();
                return;
            }

            Sprite[] ordered = new Sprite[orderedNames.Length];
            for (int index = 0; index < orderedNames.Length; index++)
            {
                ordered[index] = loaded.FirstOrDefault(sprite => sprite.name == orderedNames[index]);
                if (ordered[index] == null)
                {
                    enemyBattleFrames = Array.Empty<Sprite>();
                    return;
                }
            }
            enemyBattleFrames = ordered;
        }

        private void SetEnemyBattlePose(EnemyBattlePose pose)
        {
            if (enemyRenderer == null || enemyBattleFrames.Length != 6)
            {
                return;
            }

            Sprite sprite = enemyBattleFrames[(int)pose];
            enemyRenderer.sprite = sprite;
            if (enemyTelegraphRenderer != null)
            {
                enemyTelegraphRenderer.sprite = sprite;
            }
        }

        private static Vector3 BattlePreviewEnemyPosition(int previewIndex)
        {
            return new Vector3(previewIndex == 0 ? 4.35f : 6.35f, BattleGroundY, 0f);
        }

        private SpriteRenderer CreateBattleActorShadow(
            string objectName,
            Sprite sprite,
            Vector3 actorPosition,
            Vector3 scale,
            int sortingOrder)
        {
            GameObject shadowObject = new GameObject(objectName);
            SpriteRenderer renderer = shadowObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = new Color(.02f, .015f, .02f, .42f);
            JourneyPresentationConfig.Sort(
                renderer,
                JourneyPresentationConfig.ActorSortingLayer,
                sortingOrder);
            shadowObject.transform.position = new Vector3(actorPosition.x, BattleGroundY + .025f, 0f);
            shadowObject.transform.localScale = scale;
            renderer.enabled = false;
            return renderer;
        }

        private Sprite CreateBattleActorShadowSprite()
        {
            const int width = 96;
            const int height = 32;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "journey-battle-actor-shadow",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };
            Color32[] pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                float nx = (x + .5f - width * .5f) / (width * .5f);
                float ny = (y + .5f - height * .5f) / (height * .5f);
                float alpha = Mathf.Clamp01((1f - nx * nx - ny * ny) * 1.7f);
                pixels[y * width + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, width, height),
                new Vector2(.5f, .5f),
                100f);
            sprite.name = "journey-battle-actor-shadow-sprite";
            runtimeAssets.Add(sprite);
            runtimeAssets.Add(texture);
            return sprite;
        }

        private void SetMainEnemyVisible(bool visible)
        {
            if (enemyRenderer != null) enemyRenderer.enabled = visible;
            if (enemyShadowRenderer != null) enemyShadowRenderer.enabled = visible;
        }

        private void SyncMainEnemyShadow()
        {
            if (enemyShadowRenderer == null || enemyRenderer == null) return;
            Vector3 actorPosition = enemyRenderer.transform.position;
            enemyShadowRenderer.transform.position = new Vector3(actorPosition.x, BattleGroundY + .025f, 0f);
        }

        private Sprite CreateParcelSprite()
        {
            const int size = 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "journey-roadside-parcel";
            Color clear = new Color(0f, 0f, 0f, 0f);
            Color paper = new Color(.52f, .31f, .17f, 1f);
            Color edge = new Color(.14f, .09f, .07f, 1f);
            Color seal = new Color(.64f, .12f, .08f, 1f);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                bool body = x >= 8 && x <= 55 && y >= 14 && y <= 48;
                bool border = body && (x < 12 || x > 51 || y < 18 || y > 44);
                float dx = x - 32f, dy = y - 31f;
                bool wax = dx * dx + dy * dy < 43f;
                texture.SetPixel(x, y, wax ? seal : border ? edge : body ? paper : clear);
            }
            texture.Apply();
            runtimeAssets.Add(texture);
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 64f);
            runtimeAssets.Add(sprite);
            return sprite;
        }


    }
}
