using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Packspire
{
    public sealed partial class JourneyTravelGameplayPrototype
    {
        private const float BattleReelFallbackWidthRatio = 248f / 1280f;
        private const float BattleStageLeftPadding = .28f;
        private const float BattleStageRightPadding = .5f;
        private const float BattleEnemyGap = .5f;

        private void BuildWorldActors()
        {
            LoadEnemyBattleFrames();
            Sprite enemySprite = enemyBattleFrames.Length == 6
                ? enemyBattleFrames[(int)EnemyBattlePose.Idle]
                : PackspireResources.Load<Sprite>(encounterProfile.ResolvedFallbackSpriteResource);
            if (enemySprite != null)
            {
                string actorName = string.IsNullOrWhiteSpace(encounterProfile.ResolvedDisplayName)
                    ? "Journey Enemy"
                    : encounterProfile.ResolvedDisplayName;
                GameObject enemyObject = new GameObject($"{actorName} Actor");
                enemyRenderer = enemyObject.AddComponent<SpriteRenderer>();
                enemyRenderer.sprite = enemySprite;
                JourneyPresentationConfig.Sort(
                    enemyRenderer,
                    JourneyPresentationConfig.ActorSortingLayer,
                    10);
                enemyObject.transform.position = EnemyBattleStartPosition;
                enemyObject.transform.localScale = Vector3.one * ResolveEnemyBattleScale(enemySprite);
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
                BuildEnemyTimingGlint();
    
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
                    float previewScale = ResolveEnemyBattleScale(enemySprite) *
                                         (previewIndex == 0 ? .79f : .68f);
                    previewEnemy.transform.localScale = Vector3.one * previewScale;
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

                ApplyBattleActorLayout(1);
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
            Sprite[] loaded = PackspireResources.LoadAll<Sprite>(encounterProfile.ResolvedBattleSheetResource);
            string[] orderedNames = encounterProfile.ResolvedOrderedBattlePoseNames;
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

        private void ApplyEncounterProfile(JourneyBattleEncounterProfile profile)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            encounterProfile = profile;
            RefreshEnemyActorPresentation();
        }

        private float CurrentEnemyBattleScale => encounterProfile != null
            ? encounterProfile.ResolvedBattleScale
            : DefaultEnemyBattleScale;

        private float ResolveEnemyBattleScale(Sprite sprite)
        {
            float targetHeight = encounterProfile != null
                ? encounterProfile.ResolvedBattleTargetHeight
                : 0f;
            float sourceHeight = sprite != null ? sprite.bounds.size.y : 0f;
            if (targetHeight > .05f && sourceHeight > .01f)
                return targetHeight / sourceHeight;

            return CurrentEnemyBattleScale;
        }

        private void ApplyBattleActorLayout(int requestedEnemyCount)
        {
            if (enemyRenderer == null || enemyRenderer.sprite == null) return;

            int enemyCount = Mathf.Clamp(requestedEnemyCount, 1, battleEnemySlotPositions.Length);
            battleLayoutEnemyCount = enemyCount;

            Camera battleCamera = Camera.main;
            float halfWidth = battleCamera != null && battleCamera.orthographic
                ? battleCamera.orthographicSize * battleCamera.aspect
                : 9f;
            float cameraX = battleCamera != null ? battleCamera.transform.position.x : 0f;
            float viewportLeft = cameraX - halfWidth;
            float viewportRight = cameraX + halfWidth;
            float viewportWidth = Mathf.Max(1f, viewportRight - viewportLeft);

            float reelWidthRatio = ResolveBattleReelWidthRatio();
            float stageLeft = viewportLeft + viewportWidth * reelWidthRatio + BattleStageLeftPadding;
            float stageRight = viewportRight - BattleStageRightPadding;
            float stageWidth = Mathf.Max(5f, stageRight - stageLeft);
            JourneyBattleFormationPreset formation =
                JourneyBattleFormationLayout.Resolve(
                    encounterProfile != null
                        ? encounterProfile.ResolvedBattleFormation
                        : BattleFormationScale.Normal,
                    enemyCount);
            walker?.SetBattleHorizontalAnchor(
                stageLeft + stageWidth * formation.PlayerAnchorRatio);
            walker?.SetBattleCompositionScale(formation.PlayerScale);
            enemyBattleCompositionScale = formation.EnemyScale;

            float enemyZoneLeft =
                stageLeft + stageWidth * formation.EnemyZoneStartRatio;
            float enemyZoneWidth = Mathf.Max(2.5f, stageRight - enemyZoneLeft);
            float sourceWidth = Mathf.Max(.1f, enemyRenderer.sprite.bounds.size.x);
            float resolvedScale =
                ResolveEnemyBattleScale(enemyRenderer.sprite) * formation.EnemyScale;
            float mainWidth = sourceWidth * resolvedScale;
            float secondWidth = mainWidth;
            float thirdWidth = mainWidth;
            float requestedWidth = mainWidth;
            if (enemyCount >= 2) requestedWidth += BattleEnemyGap + secondWidth;
            if (enemyCount >= 3) requestedWidth += BattleEnemyGap + thirdWidth;

            enemyBattleFormationScaleFactor = Mathf.Min(1f, enemyZoneWidth / requestedWidth);
            mainWidth *= enemyBattleFormationScaleFactor;
            secondWidth *= enemyBattleFormationScaleFactor;
            thirdWidth *= enemyBattleFormationScaleFactor;
            float actualGap = BattleEnemyGap * enemyBattleFormationScaleFactor;
            float formationWidth = mainWidth;
            if (enemyCount >= 2) formationWidth += actualGap + secondWidth;
            if (enemyCount >= 3) formationWidth += actualGap + thirdWidth;

            float formationLeft = enemyZoneLeft + (enemyZoneWidth - formationWidth) * .5f;
            battleEnemySlotPositions[0] = new Vector3(
                formationLeft + mainWidth * .5f,
                BattleGroundY,
                0f);
            battleEnemySlotScales[0] = resolvedScale * enemyBattleFormationScaleFactor;

            float cursor = formationLeft + mainWidth + actualGap;
            if (enemyCount >= 2)
            {
                battleEnemySlotPositions[1] = new Vector3(
                    cursor + secondWidth * .5f,
                    BattleGroundY,
                    0f);
                battleEnemySlotScales[1] =
                    resolvedScale * enemyBattleFormationScaleFactor;
                cursor += secondWidth + actualGap;
            }
            if (enemyCount >= 3)
            {
                battleEnemySlotPositions[2] = new Vector3(
                    cursor + thirdWidth * .5f,
                    BattleGroundY,
                    0f);
                battleEnemySlotScales[2] =
                    resolvedScale * enemyBattleFormationScaleFactor;
            }

            enemyBattleBasePosition = battleEnemySlotPositions[0];
            enemyBattleBaseScale = battleEnemySlotScales[0];
            enemyRenderer.transform.position = enemyBattleBasePosition;
            enemyRenderer.transform.localScale = Vector3.one * enemyBattleBaseScale;
            SyncMainEnemyShadow();

            if (enemyTelegraphRenderer != null)
            {
                enemyTelegraphRenderer.transform.position = enemyBattleBasePosition;
                enemyTelegraphRenderer.transform.localScale =
                    Vector3.one * enemyBattleBaseScale * 1.09f;
            }

            for (int previewIndex = 0; previewIndex < battlePreviewEnemyRenderers.Length; previewIndex++)
            {
                SpriteRenderer preview = battlePreviewEnemyRenderers[previewIndex];
                if (preview == null) continue;
                int slotIndex = previewIndex + 1;
                if (slotIndex >= enemyCount) continue;
                preview.transform.position = battleEnemySlotPositions[slotIndex];
                preview.transform.localScale = Vector3.one * battleEnemySlotScales[slotIndex];
                SyncBattleActorShadow(battlePreviewEnemyShadowRenderers[previewIndex], preview);
            }
        }

        private float ResolveBattleReelWidthRatio()
        {
            if (screen != null)
            {
                UnityEngine.UIElements.VisualElement reel =
                    screen.Q<UnityEngine.UIElements.VisualElement>("journey-threat-reel");
                float screenWidth = screen.resolvedStyle.width;
                float reelWidth = reel != null ? reel.resolvedStyle.width : float.NaN;
                if (screenWidth > 1f && !float.IsNaN(reelWidth) && reelWidth > 1f)
                    return Mathf.Clamp(reelWidth / screenWidth, .12f, .32f);
            }

            return BattleReelFallbackWidthRatio;
        }

        private void RefreshEnemyActorPresentation()
        {
            if (encounterProfile == null || enemyRenderer == null) return;
            CancelEnemyPoseRecovery();
            LoadEnemyBattleFrames();
            Sprite sprite = enemyBattleFrames.Length == 6
                ? enemyBattleFrames[(int)EnemyBattlePose.Idle]
                : PackspireResources.Load<Sprite>(encounterProfile.ResolvedFallbackSpriteResource);
            if (sprite == null)
                throw new InvalidOperationException(
                    $"Journey encounter '{encounterProfile.name}' has no usable enemy sprite.");

            string actorName = string.IsNullOrWhiteSpace(encounterProfile.ResolvedDisplayName)
                ? "Journey Enemy"
                : encounterProfile.ResolvedDisplayName;
            enemyRenderer.gameObject.name = $"{actorName} Actor";
            enemyRenderer.sprite = sprite;
            if (enemyTelegraphRenderer != null)
            {
                enemyTelegraphRenderer.gameObject.name = $"{actorName} Telegraph Silhouette";
                enemyTelegraphRenderer.sprite = sprite;
            }
            for (int index = 0; index < battlePreviewEnemyRenderers.Length; index++)
                if (battlePreviewEnemyRenderers[index] != null)
                {
                    battlePreviewEnemyRenderers[index].sprite = sprite;
                }
            ApplyBattleActorLayout(battleLayoutEnemyCount);
        }

        private void SetEnemyBattlePose(EnemyBattlePose pose)
        {
            if (enemyRenderer == null || enemyBattleFrames.Length != 6)
            {
                return;
            }

            Sprite sprite = enemyBattleFrames[(int)pose];
            enemyRenderer.sprite = sprite;
            enemyBattleBaseScale =
                ResolveEnemyBattleScale(sprite) *
                enemyBattleCompositionScale *
                enemyBattleFormationScaleFactor;
            enemyRenderer.transform.localScale = Vector3.one * enemyBattleBaseScale;
            if (enemyTelegraphRenderer != null)
            {
                enemyTelegraphRenderer.sprite = sprite;
                enemyTelegraphRenderer.transform.localScale =
                    Vector3.one * enemyBattleBaseScale * 1.09f;
            }
        }

        private Vector3 BattlePreviewEnemyPosition(int previewIndex)
        {
            int slotIndex = Mathf.Clamp(
                previewIndex + 1,
                1,
                battleEnemySlotPositions.Length - 1);
            return battleEnemySlotPositions[slotIndex];
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
            if (!visible) CancelEnemyPoseRecovery();
            if (enemyRenderer != null) enemyRenderer.enabled = visible;
            if (enemyShadowRenderer != null) enemyShadowRenderer.enabled = visible;
        }

        private void SyncMainEnemyShadow()
        {
            SyncBattleActorShadow(enemyShadowRenderer, enemyRenderer);
        }

        private void SyncMainEnemyShadow(float groundY)
        {
            SyncBattleActorShadow(enemyShadowRenderer, enemyRenderer, groundY);
        }

        private static void SyncBattleActorShadow(
            SpriteRenderer shadow,
            SpriteRenderer actor,
            float groundY = BattleGroundY)
        {
            if (shadow == null || actor == null) return;
            Vector3 actorPosition = actor.transform.position;
            shadow.transform.position =
                new Vector3(actorPosition.x, groundY + .025f, 0f);
            float actorWidth = actor.sprite != null
                ? actor.sprite.bounds.size.x * Mathf.Abs(actor.transform.localScale.x)
                : 1.5f;
            float shadowWidth = shadow.sprite != null
                ? shadow.sprite.bounds.size.x
                : 1f;
            float widthScale = Mathf.Clamp(
                actorWidth * .68f / Mathf.Max(.01f, shadowWidth),
                .9f,
                2.4f);
            shadow.transform.localScale = new Vector3(widthScale, .4f, 1f);
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
