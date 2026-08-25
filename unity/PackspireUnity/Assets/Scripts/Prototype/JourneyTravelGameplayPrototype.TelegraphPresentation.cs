using UnityEngine;

namespace Packspire
{
    public sealed partial class JourneyTravelGameplayPrototype
    {
        private const float EnemyTimingFlashDuration = .28f;
        private const float EnemyImpactContactFlashDuration = .38f;
        private const int EnemyTimingGlintTextureSize = 128;
        private SpriteRenderer enemyTimingGlintBackdropRenderer;
        private SpriteRenderer enemyTimingGlintRenderer;
        private SpriteRenderer enemyTimingGlintRingRenderer;
        private SpriteRenderer enemyTimingGlintCoreRenderer;
        private SpriteRenderer enemyImpactContactRenderer;
        private float enemyTimingFlashRemaining;
        private float enemyImpactContactFlashRemaining;
        private Vector3 enemyImpactContactPosition;
        private bool enemyTimingCueFired;
        private bool enemyTimingGlintAllowed;

        private float EnemyTimingFlashStrength =>
            EnemyTimingFlashDuration <= 0f
                ? 0f
                : Mathf.Clamp01(enemyTimingFlashRemaining / EnemyTimingFlashDuration);

        private void BuildEnemyTimingGlint()
        {
            Sprite glintSprite = CreateEnemyTimingGlintSprite();
            Material glintMaterial = CreateEnemyTimingGlintMaterial();

            GameObject backdropObject =
                new GameObject("Enemy Timing Weapon Tip Contrast");
            enemyTimingGlintBackdropRenderer =
                backdropObject.AddComponent<SpriteRenderer>();
            enemyTimingGlintBackdropRenderer.sprite =
                CreateEnemyTimingBackdropSprite();
            JourneyPresentationConfig.Sort(
                enemyTimingGlintBackdropRenderer,
                JourneyPresentationConfig.EffectSortingLayer,
                5);
            enemyTimingGlintBackdropRenderer.enabled = false;

            GameObject glintObject =
                new GameObject("Enemy Timing Weapon Tip Glint");
            enemyTimingGlintRenderer =
                glintObject.AddComponent<SpriteRenderer>();
            enemyTimingGlintRenderer.sprite = glintSprite;
            enemyTimingGlintRenderer.sharedMaterial = glintMaterial;
            JourneyPresentationConfig.Sort(
                enemyTimingGlintRenderer,
                JourneyPresentationConfig.EffectSortingLayer,
                6);
            enemyTimingGlintRenderer.enabled = false;

            GameObject ringObject =
                new GameObject("Enemy Timing Weapon Tip Ring");
            enemyTimingGlintRingRenderer =
                ringObject.AddComponent<SpriteRenderer>();
            enemyTimingGlintRingRenderer.sprite =
                CreateEnemyTimingRingSprite();
            enemyTimingGlintRingRenderer.sharedMaterial = glintMaterial;
            JourneyPresentationConfig.Sort(
                enemyTimingGlintRingRenderer,
                JourneyPresentationConfig.EffectSortingLayer,
                7);
            enemyTimingGlintRingRenderer.enabled = false;

            GameObject coreObject =
                new GameObject("Enemy Timing Weapon Tip Glint Core");
            enemyTimingGlintCoreRenderer =
                coreObject.AddComponent<SpriteRenderer>();
            enemyTimingGlintCoreRenderer.sprite = glintSprite;
            enemyTimingGlintCoreRenderer.sharedMaterial = glintMaterial;
            JourneyPresentationConfig.Sort(
                enemyTimingGlintCoreRenderer,
                JourneyPresentationConfig.EffectSortingLayer,
                8);
            enemyTimingGlintCoreRenderer.enabled = false;

            GameObject impactObject =
                new GameObject("Enemy Impact Contact Spark");
            enemyImpactContactRenderer =
                impactObject.AddComponent<SpriteRenderer>();
            enemyImpactContactRenderer.sprite = glintSprite;
            enemyImpactContactRenderer.sharedMaterial = glintMaterial;
            JourneyPresentationConfig.Sort(
                enemyImpactContactRenderer,
                JourneyPresentationConfig.EffectSortingLayer,
                8);
            enemyImpactContactRenderer.enabled = false;
        }

        private Material CreateEnemyTimingGlintMaterial()
        {
            Shader shader = Shader.Find(
                "Legacy Shaders/Particles/Additive");
            if (shader == null)
                shader = Shader.Find("Mobile/Particles/Additive");
            if (shader == null) return null;

            Material material = new Material(shader)
            {
                name = "journey-enemy-timing-additive-material",
                hideFlags = HideFlags.DontSave
            };
            runtimeAssets.Add(material);
            return material;
        }

        private Sprite CreateEnemyTimingGlintSprite()
        {
            Texture2D texture = CreateEnemyTimingTexture(
                "journey-enemy-timing-weapon-tip-glint");
            Color32[] pixels = new Color32[
                EnemyTimingGlintTextureSize * EnemyTimingGlintTextureSize];
            float center = (EnemyTimingGlintTextureSize - 1f) * .5f;
            float radius = EnemyTimingGlintTextureSize * .5f;
            for (int y = 0; y < EnemyTimingGlintTextureSize; y++)
            for (int x = 0; x < EnemyTimingGlintTextureSize; x++)
            {
                float dx = Mathf.Abs((x - center) / radius);
                float dy = Mathf.Abs((y - center) / radius);
                float horizontal = Mathf.Pow(
                    Mathf.Clamp01(1f - dx),
                    1.15f) * Mathf.Pow(
                    Mathf.Clamp01(1f - dy / .075f),
                    1.15f);
                float vertical = Mathf.Pow(
                    Mathf.Clamp01(1f - dy),
                    1.15f) * Mathf.Pow(
                    Mathf.Clamp01(1f - dx / .075f),
                    1.15f);
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                float core = Mathf.Pow(
                    Mathf.Clamp01(1f - distance / .22f),
                    1.5f);
                float alpha = Mathf.Clamp01(
                    Mathf.Max(horizontal, vertical) * 1.6f + core);
                pixels[y * EnemyTimingGlintTextureSize + x] =
                    new Color32(255, 255, 255, (byte)(alpha * 255f));
            }
            return FinishEnemyTimingSprite(
                texture,
                pixels,
                "journey-enemy-timing-weapon-tip-glint-sprite");
        }

        private Sprite CreateEnemyTimingRingSprite()
        {
            Texture2D texture = CreateEnemyTimingTexture(
                "journey-enemy-timing-weapon-tip-ring");
            Color32[] pixels = new Color32[
                EnemyTimingGlintTextureSize * EnemyTimingGlintTextureSize];
            float center = (EnemyTimingGlintTextureSize - 1f) * .5f;
            float radius = EnemyTimingGlintTextureSize * .5f;
            for (int y = 0; y < EnemyTimingGlintTextureSize; y++)
            for (int x = 0; x < EnemyTimingGlintTextureSize; x++)
            {
                float dx = (x - center) / radius;
                float dy = (y - center) / radius;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                float ring = Mathf.Pow(
                    Mathf.Clamp01(
                        1f - Mathf.Abs(distance - .70f) / .055f),
                    1.2f);
                pixels[y * EnemyTimingGlintTextureSize + x] =
                    new Color32(255, 255, 255, (byte)(ring * 255f));
            }
            return FinishEnemyTimingSprite(
                texture,
                pixels,
                "journey-enemy-timing-weapon-tip-ring-sprite");
        }

        private Sprite CreateEnemyTimingBackdropSprite()
        {
            Texture2D texture = CreateEnemyTimingTexture(
                "journey-enemy-timing-weapon-tip-contrast");
            Color32[] pixels = new Color32[
                EnemyTimingGlintTextureSize * EnemyTimingGlintTextureSize];
            float center = (EnemyTimingGlintTextureSize - 1f) * .5f;
            float radius = EnemyTimingGlintTextureSize * .5f;
            for (int y = 0; y < EnemyTimingGlintTextureSize; y++)
            for (int x = 0; x < EnemyTimingGlintTextureSize; x++)
            {
                float dx = (x - center) / radius;
                float dy = (y - center) / radius;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Pow(
                    Mathf.Clamp01(1f - distance),
                    1.8f);
                pixels[y * EnemyTimingGlintTextureSize + x] =
                    new Color32(255, 255, 255, (byte)(alpha * 255f));
            }
            return FinishEnemyTimingSprite(
                texture,
                pixels,
                "journey-enemy-timing-weapon-tip-contrast-sprite");
        }

        private static Texture2D CreateEnemyTimingTexture(string name)
        {
            return new Texture2D(
                EnemyTimingGlintTextureSize,
                EnemyTimingGlintTextureSize,
                TextureFormat.RGBA32,
                false)
            {
                name = name,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };
        }

        private Sprite FinishEnemyTimingSprite(
            Texture2D texture,
            Color32[] pixels,
            string spriteName)
        {
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(
                    0f,
                    0f,
                    EnemyTimingGlintTextureSize,
                    EnemyTimingGlintTextureSize),
                new Vector2(.5f, .5f),
                100f);
            sprite.name = spriteName;
            runtimeAssets.Add(sprite);
            runtimeAssets.Add(texture);
            return sprite;
        }

        private void ResetEnemyTimingCue()
        {
            enemyTimingFlashRemaining = 0f;
            enemyTimingCueFired = false;
            SetEnemyTimingGlintVisible(false);
        }

        private void SetEnemyTimingGlintAllowed(bool allowed)
        {
            enemyTimingGlintAllowed = allowed;
            if (!allowed) SetEnemyTimingGlintVisible(false);
        }

        private void BeginEnemyTimingCue()
        {
            enemyTimingCueFired = true;
            enemyTimingFlashRemaining = EnemyTimingFlashDuration;
            if (enemyTimingGlintAllowed)
                SetEnemyTimingGlintVisible(true);
        }

        private void UpdateEnemyTimingCue(float delta, float anticipation)
        {
            float flash = EnemyTimingFlashStrength;
            if (enemyTelegraphRenderer != null &&
                enemyTelegraphRenderer.enabled)
            {
                SyncEnemyTelegraphTransform(flash * .18f, anticipation);
                enemyTelegraphRenderer.color = new Color(
                    enemyTelegraphColor.r,
                    enemyTelegraphColor.g,
                    enemyTelegraphColor.b,
                    Mathf.Lerp(.06f, .28f, flash));
            }

            if (enemyTimingGlintRenderer != null &&
                enemyTimingGlintRenderer.enabled)
            {
                UpdateEnemyTimingGlintTransform(flash);
                Color.RGBToHSV(
                    enemyTelegraphColor,
                    out float hue,
                    out float saturation,
                    out _);
                float opacity = Mathf.SmoothStep(0f, 1f, flash);
                Color actionColor = Color.HSVToRGB(
                    hue,
                    Mathf.Max(.88f, saturation),
                    1f);

                Color rayColor = actionColor;
                rayColor.a = opacity;
                enemyTimingGlintRenderer.color = rayColor;

                if (enemyTimingGlintRingRenderer != null)
                {
                    Color ringColor = Color.Lerp(
                        actionColor,
                        Color.white,
                        .22f);
                    JourneyEnemyBattlePresentationProfile presentation =
                        encounterProfile != null
                            ? encounterProfile.ResolvedBattlePresentation
                            : null;
                    float ringOpacity = presentation != null
                        ? presentation.timingCueRingOpacity
                        : .90f;
                    ringColor.a = opacity * ringOpacity;
                    enemyTimingGlintRingRenderer.color = ringColor;
                }

                if (enemyTimingGlintCoreRenderer != null)
                {
                    Color coreColor = Color.white;
                    coreColor.a = opacity;
                    enemyTimingGlintCoreRenderer.color = coreColor;
                }

                if (enemyTimingGlintBackdropRenderer != null)
                {
                    JourneyEnemyBattlePresentationProfile presentation =
                        encounterProfile != null
                            ? encounterProfile.ResolvedBattlePresentation
                            : null;
                    float backdropOpacity = presentation != null
                        ? presentation.timingCueBackdropOpacity
                        : .45f;
                    enemyTimingGlintBackdropRenderer.color =
                        new Color(0f, 0f, 0f, opacity * backdropOpacity);
                }
            }

            enemyTimingFlashRemaining = Mathf.Max(
                0f,
                enemyTimingFlashRemaining - Mathf.Max(0f, delta));
            if (enemyTimingFlashRemaining <= 0f)
                SetEnemyTimingGlintVisible(false);
        }

        private void SetEnemyTimingGlintVisible(bool visible)
        {
            if (enemyTimingGlintRenderer == null) return;
            enemyTimingGlintRenderer.enabled = visible;
            if (enemyTimingGlintBackdropRenderer != null)
                enemyTimingGlintBackdropRenderer.enabled = visible;
            if (enemyTimingGlintRingRenderer != null)
                enemyTimingGlintRingRenderer.enabled = visible;
            if (enemyTimingGlintCoreRenderer != null)
                enemyTimingGlintCoreRenderer.enabled = visible;
            if (visible) UpdateEnemyTimingGlintTransform(1f);
        }

        private void UpdateEnemyTimingGlintTransform(float flash)
        {
            if (enemyTimingGlintRenderer == null ||
                enemyRenderer == null) return;

            Bounds bounds = enemyRenderer.bounds;
            JourneyEnemyBattlePresentationProfile presentation =
                encounterProfile != null
                    ? encounterProfile.ResolvedBattlePresentation
                    : null;
            Vector3 tip;
            float cueScale = 1f;
            if (presentation != null)
            {
                tip = ResolveEnemySpriteAnchor(
                    presentation.TimingCueAnchor(enemyActionOverhead));
                cueScale = Mathf.Max(.25f, presentation.timingCueScale);
            }
            else
            {
                float facing = walker != null &&
                               walker.transform.position.x < bounds.center.x
                    ? -1f
                    : 1f;
                float horizontalAnchor =
                    enemyActionOverhead ? .48f : .68f;
                tip = bounds.center + new Vector3(
                    facing * bounds.extents.x * horizontalAnchor,
                    enemyActionOverhead
                        ? bounds.extents.y * .62f
                        : -bounds.extents.y * .08f,
                    0f);
            }

            float progress = 1f - Mathf.Clamp01(flash);
            float diameter = Mathf.Clamp(
                Mathf.Max(bounds.size.x, bounds.size.y) * .82f,
                1.32f,
                2.16f) * cueScale;
            float spriteWorldSize =
                EnemyTimingGlintTextureSize / 100f;
            float rayOpening = Mathf.Lerp(
                .85f,
                1.35f,
                Mathf.SmoothStep(0f, 1f, progress));
            float rayScale =
                diameter / spriteWorldSize * rayOpening;
            SetEnemyTimingRendererTransform(
                enemyTimingGlintRenderer,
                tip,
                rayScale);

            SetEnemyTimingRendererTransform(
                enemyTimingGlintCoreRenderer,
                tip,
                rayScale * .52f);

            float ringOpening = Mathf.Lerp(
                .62f,
                1.48f,
                Mathf.SmoothStep(0f, 1f, progress));
            SetEnemyTimingRendererTransform(
                enemyTimingGlintRingRenderer,
                tip,
                diameter / spriteWorldSize * ringOpening);

            SetEnemyTimingRendererTransform(
                enemyTimingGlintBackdropRenderer,
                tip,
                diameter / spriteWorldSize * 1.42f);
        }

        private Vector3 ResolveEnemySpriteAnchor(Vector2 normalizedAnchor)
        {
            if (enemyRenderer == null || enemyRenderer.sprite == null)
                return enemyRenderer != null
                    ? enemyRenderer.transform.position
                    : Vector3.zero;

            Bounds spriteBounds = enemyRenderer.sprite.bounds;
            float normalizedX = enemyRenderer.flipX
                ? 1f - normalizedAnchor.x
                : normalizedAnchor.x;
            Vector3 localAnchor = new Vector3(
                Mathf.Lerp(spriteBounds.min.x, spriteBounds.max.x, normalizedX),
                Mathf.Lerp(spriteBounds.min.y, spriteBounds.max.y, normalizedAnchor.y),
                0f);
            return enemyRenderer.transform.TransformPoint(localAnchor);
        }

        private void BeginEnemyImpactContactCue()
        {
            if (!enemyTimingGlintAllowed || enemyImpactContactRenderer == null)
                return;
            enemyImpactContactFlashRemaining = EnemyImpactContactFlashDuration;
            JourneyEnemyBattlePresentationProfile presentation =
                encounterProfile != null
                    ? encounterProfile.ResolvedBattlePresentation
                    : null;
            enemyImpactContactPosition = presentation != null
                ? ResolveEnemySpriteAnchor(
                    presentation.ImpactContactAnchor(enemyActionOverhead))
                : enemyRenderer.bounds.center + new Vector3(
                    enemyActionOverhead
                        ? -enemyRenderer.bounds.extents.x * .72f
                        : 0f,
                    -enemyRenderer.bounds.extents.y * .86f,
                    0f);
            enemyImpactContactRenderer.enabled = true;
            UpdateEnemyImpactContactCue(0f);
        }

        private void UpdateEnemyImpactContactCue(float delta)
        {
            if (enemyImpactContactRenderer == null ||
                !enemyImpactContactRenderer.enabled) return;

            float strength = EnemyImpactContactFlashDuration <= 0f
                ? 0f
                : Mathf.Clamp01(
                    enemyImpactContactFlashRemaining /
                    EnemyImpactContactFlashDuration);
            JourneyEnemyBattlePresentationProfile presentation =
                encounterProfile != null
                    ? encounterProfile.ResolvedBattlePresentation
                    : null;
            Vector3 contact = enemyImpactContactPosition;
            float baseDiameter = Mathf.Clamp(
                Mathf.Max(enemyRenderer.bounds.size.x, enemyRenderer.bounds.size.y) * .82f,
                1.32f,
                2.16f);
            float scaleMultiplier = presentation != null
                ? Mathf.Max(.1f, presentation.impactSparkScale)
                : .28f;
            float spriteWorldSize = EnemyTimingGlintTextureSize / 100f;
            float opening = Mathf.Lerp(.72f, 1.08f, 1f - strength);
            SetEnemyTimingRendererTransform(
                enemyImpactContactRenderer,
                contact,
                baseDiameter * scaleMultiplier / spriteWorldSize * opening);

            Color contactColor = Color.Lerp(enemyTelegraphColor, Color.white, .62f);
            contactColor.a = Mathf.SmoothStep(0f, 1f, strength);
            enemyImpactContactRenderer.color = contactColor;

            enemyImpactContactFlashRemaining = Mathf.Max(
                0f,
                enemyImpactContactFlashRemaining - Mathf.Max(0f, delta));
            if (enemyImpactContactFlashRemaining <= 0f)
                enemyImpactContactRenderer.enabled = false;
        }

        private void ResetEnemyImpactContactCue()
        {
            enemyImpactContactFlashRemaining = 0f;
            if (enemyImpactContactRenderer != null)
                enemyImpactContactRenderer.enabled = false;
        }

        private static void SetEnemyTimingRendererTransform(
            SpriteRenderer renderer,
            Vector3 position,
            float scale)
        {
            if (renderer == null) return;
            renderer.transform.position = position;
            renderer.transform.rotation = Quaternion.identity;
            renderer.transform.localScale =
                new Vector3(scale, scale, 1f);
        }
    }
}
