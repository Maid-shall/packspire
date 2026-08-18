using System.Collections.Generic;
using UnityEngine;

namespace Packspire
{
    /// <summary>
    /// Lightweight weather presentation built only from SpriteRenderers. Ambient rain,
    /// mist and local light continue while choices are open; travel-relative motes stop
    /// with the road so a paused scene never appears to keep scrolling.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class JourneyEnvironmentController : MonoBehaviour
    {
        private sealed class AmbientSprite
        {
            public SpriteRenderer Renderer;
            public Vector2 Velocity;
            public float Phase;
            public float BaseY;
        }

        private const int RainPoolSize = 60;
        private const int MistPoolSize = 8;
        private const int MotePoolSize = 18;
        private const float LeftEdge = -10.5f;
        private const float RightEdge = 10.5f;
        private const float BottomEdge = -5.8f;
        private const float TopEdge = 5.8f;

        private readonly List<Object> ownedAssets = new List<Object>();
        private readonly List<AmbientSprite> rain = new List<AmbientSprite>(RainPoolSize);
        private readonly List<AmbientSprite> mist = new List<AmbientSprite>(MistPoolSize);
        private readonly List<AmbientSprite> windMotes = new List<AmbientSprite>(MotePoolSize);

        private GameObject environmentRoot;
        private SpriteRenderer glowLeft;
        private SpriteRenderer glowRight;
        private int biomeIndex;
        private float journeyProgress;
        private float worldMotion;
        private float environmentClock;
        private bool battleContext;
        private bool initialized;

        public int BiomeIndex => biomeIndex;
        public bool WindMotesActive => worldMotion > .001f && ActiveMoteCount() > 0;

        public void Initialize(JourneyWalkCyclePrototype walker)
        {
            if (initialized) return;
            initialized = true;

            environmentRoot = new GameObject("Journey Environment Effects");
            environmentRoot.transform.SetParent(transform, false);

            Sprite rainSprite = CreateSprite("journey-rain-streak", CreateRainTexture(), 100f);
            Sprite mistSprite = CreateSprite("journey-mist-wisp", CreateSoftDiscTexture(96), 100f);
            Sprite moteSprite = CreateSprite("journey-wind-mote", CreateMoteTexture(), 100f);
            Sprite glowSprite = CreateSprite("journey-local-light-glow", CreateSoftDiscTexture(128), 100f);

            BuildRainPool(rainSprite);
            BuildMistPool(mistSprite);
            BuildMotePool(moteSprite);
            BuildLocalLights(glowSprite);
            SetBiome(0);
            SetWorldMotion(0f);
        }

        public void SetBiome(int targetBiome)
        {
            biomeIndex = Mathf.Clamp(targetBiome, 0, 2);
            if (!initialized) return;
            ApplyPaletteAndVisibility();
        }

        public void SetWorldMotion(float scale)
        {
            worldMotion = Mathf.Clamp(scale, 0f, 2f);
            if (!initialized) return;
            ApplyPaletteAndVisibility();
        }

        public void SetJourneyProgress(float normalizedProgress)
        {
            float next = Mathf.Clamp01(normalizedProgress);
            if (Mathf.Abs(next - journeyProgress) < .001f) return;
            journeyProgress = next;
            if (!initialized) return;
            ApplyPaletteAndVisibility();
        }

        public void SetBattleContext(bool battle)
        {
            battleContext = battle;
            if (initialized) ApplyPaletteAndVisibility();
        }

        private void Update()
        {
            using var performanceScope = PackspirePerformance.JourneyEnvironment.Auto();
            AdvanceEnvironment(Time.unscaledDeltaTime);
        }

        private void AdvanceEnvironment(float delta)
        {
            if (!initialized || delta <= 0f) return;
            environmentClock += delta;

            int rainCount = ActiveRainCount();
            for (int index = 0; index < rainCount; index++)
            {
                AmbientSprite drop = rain[index];
                Vector3 position = drop.Renderer.transform.localPosition;
                position.x += drop.Velocity.x * delta;
                position.y += drop.Velocity.y * delta;
                if (position.y < BottomEdge)
                {
                    position.y = TopEdge + Mathf.Repeat(index * 1.73f, 1.2f);
                    position.x = Mathf.Lerp(LeftEdge, RightEdge, Hash01(index * 19 + Mathf.FloorToInt(environmentClock)));
                }
                if (position.x < LeftEdge) position.x = RightEdge;
                drop.Renderer.transform.localPosition = position;
            }

            int mistCount = ActiveMistCount();
            for (int index = 0; index < mistCount; index++)
            {
                AmbientSprite wisp = mist[index];
                Vector3 position = wisp.Renderer.transform.localPosition;
                position.x += wisp.Velocity.x * delta;
                position.y = wisp.BaseY + Mathf.Sin(environmentClock * .22f + wisp.Phase) * .22f;
                if (position.x < LeftEdge - 4f) position.x = RightEdge + 4f;
                wisp.Renderer.transform.localPosition = position;
            }

            if (worldMotion > .001f)
            {
                int moteCount = ActiveMoteCount();
                for (int index = 0; index < moteCount; index++)
                {
                    AmbientSprite mote = windMotes[index];
                    Vector3 position = mote.Renderer.transform.localPosition;
                    position.x += mote.Velocity.x * delta * Mathf.Lerp(.8f, 1.35f, worldMotion * .5f);
                    position.y += (mote.Velocity.y + Mathf.Sin(environmentClock * 1.4f + mote.Phase) * .08f) * delta;
                    if (position.x < LeftEdge)
                    {
                        position.x = RightEdge + Mathf.Repeat(index * .41f, 1.8f);
                        position.y = Mathf.Lerp(-2.4f, 4.8f, Hash01(index * 31 + Mathf.FloorToInt(environmentClock)));
                    }
                    mote.Renderer.transform.localPosition = position;
                }
            }

            float pulse = .88f + Mathf.Sin(environmentClock * 2.1f) * .08f + Mathf.Sin(environmentClock * 4.7f) * .04f;
            ApplyGlowAlpha(glowLeft, pulse);
            ApplyGlowAlpha(glowRight, 1.88f - pulse);
        }

        private void BuildRainPool(Sprite sprite)
        {
            for (int index = 0; index < RainPoolSize; index++)
            {
                SpriteRenderer renderer = CreateRenderer($"Rain {index:00}", sprite, 45);
                renderer.transform.localPosition = new Vector3(
                    Mathf.Lerp(LeftEdge, RightEdge, Hash01(index * 17 + 3)),
                    Mathf.Lerp(BottomEdge, TopEdge, Hash01(index * 29 + 7)),
                    0f);
                float scale = Mathf.Lerp(.62f, 1.05f, Hash01(index * 11 + 5));
                renderer.transform.localScale = new Vector3(scale, scale, 1f);
                renderer.transform.localRotation = Quaternion.Euler(0f, 0f, -7f);
                rain.Add(new AmbientSprite
                {
                    Renderer = renderer,
                    Velocity = new Vector2(-1.05f, -8.7f - Hash01(index * 13) * 2.2f)
                });
            }
        }

        private void BuildMistPool(Sprite sprite)
        {
            for (int index = 0; index < MistPoolSize; index++)
            {
                SpriteRenderer renderer = CreateRenderer($"Mist {index:00}", sprite, 12);
                float baseY = Mathf.Lerp(-3.2f, .2f, Hash01(index * 23 + 2));
                renderer.transform.localPosition = new Vector3(
                    Mathf.Lerp(LeftEdge - 3f, RightEdge + 3f, Hash01(index * 37 + 9)),
                    baseY,
                    0f);
                renderer.transform.localScale = new Vector3(
                    Mathf.Lerp(5.2f, 8.5f, Hash01(index * 7 + 1)),
                    Mathf.Lerp(1.1f, 2.2f, Hash01(index * 5 + 4)),
                    1f);
                mist.Add(new AmbientSprite
                {
                    Renderer = renderer,
                    Velocity = new Vector2(-.12f - Hash01(index * 3) * .08f, 0f),
                    Phase = Hash01(index * 41) * Mathf.PI * 2f,
                    BaseY = baseY
                });
            }
        }

        private void BuildMotePool(Sprite sprite)
        {
            for (int index = 0; index < MotePoolSize; index++)
            {
                SpriteRenderer renderer = CreateRenderer($"Wind Mote {index:00}", sprite, 36);
                renderer.transform.localPosition = new Vector3(
                    Mathf.Lerp(LeftEdge, RightEdge, Hash01(index * 43 + 6)),
                    Mathf.Lerp(-2.4f, 4.8f, Hash01(index * 47 + 8)),
                    0f);
                float scale = Mathf.Lerp(.45f, 1.1f, Hash01(index * 13 + 3));
                renderer.transform.localScale = new Vector3(scale * 1.8f, scale * .7f, 1f);
                renderer.transform.localRotation = Quaternion.Euler(0f, 0f, Hash01(index * 17) * 30f - 15f);
                windMotes.Add(new AmbientSprite
                {
                    Renderer = renderer,
                    Velocity = new Vector2(-2.4f - Hash01(index * 19) * 1.3f, Mathf.Lerp(-.12f, .2f, Hash01(index * 31))),
                    Phase = Hash01(index * 53) * Mathf.PI * 2f
                });
            }
        }

        private SpriteRenderer CreateRenderer(string name, Sprite sprite, int sortingOrder)
        {
            GameObject host = new GameObject(name);
            host.transform.SetParent(environmentRoot.transform, false);
            SpriteRenderer renderer = host.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            JourneyPresentationConfig.Sort(renderer, JourneyPresentationConfig.EffectSortingLayer, sortingOrder);
            return renderer;
        }

        private void BuildLocalLights(Sprite glowSprite)
        {
            glowLeft = CreateGlow("Local Light / Left", glowSprite, new Vector3(-3.2f, .7f, 0f));
            glowRight = CreateGlow("Local Light / Right", glowSprite, new Vector3(4.1f, 1.1f, 0f));
        }

        private SpriteRenderer CreateGlow(string name, Sprite sprite, Vector3 position)
        {
            GameObject host = new GameObject(name);
            host.transform.SetParent(environmentRoot.transform, false);
            host.transform.localPosition = position;
            host.transform.localScale = new Vector3(4.8f, 3.2f, 1f);
            SpriteRenderer renderer = host.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            JourneyPresentationConfig.Sort(renderer, JourneyPresentationConfig.BackgroundSortingLayer, -18);
            return renderer;
        }

        private void ApplyPaletteAndVisibility()
        {
            float nightFactor = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.42f, 1f, journeyProgress));
            float weatherVisibility = Mathf.Lerp(1f, .76f, nightFactor);
            int rainCount = ActiveRainCount();
            Color rainColor = biomeIndex == 1
                ? new Color(.58f, .88f, 1f, battleContext ? .22f : .34f)
                : new Color(1f, .42f, .34f, battleContext ? .1f : .16f);
            rainColor.a *= weatherVisibility;
            for (int index = 0; index < rain.Count; index++)
            {
                rain[index].Renderer.enabled = index < rainCount;
                rain[index].Renderer.color = rainColor;
            }

            int mistCount = ActiveMistCount();
            Color mistColor = biomeIndex switch
            {
                1 => new Color(.46f, .82f, .9f, battleContext ? .035f : .052f),
                2 => new Color(.58f, .16f, .18f, battleContext ? .03f : .042f),
                _ => new Color(.58f, .52f, .48f, battleContext ? .018f : .026f)
            };
            mistColor.a *= Mathf.Lerp(1f, .82f, nightFactor);
            for (int index = 0; index < mist.Count; index++)
            {
                mist[index].Renderer.enabled = index < mistCount;
                mist[index].Renderer.color = mistColor;
            }

            int moteCount = ActiveMoteCount();
            Color moteColor = biomeIndex switch
            {
                1 => new Color(.64f, .9f, 1f, .36f),
                2 => new Color(1f, .28f, .08f, .58f),
                _ => new Color(.78f, .63f, .44f, .4f)
            };
            moteColor.a *= Mathf.Lerp(1f, .86f, nightFactor);
            for (int index = 0; index < windMotes.Count; index++)
            {
                windMotes[index].Renderer.enabled = worldMotion > .001f && index < moteCount;
                windMotes[index].Renderer.color = moteColor;
            }

            Color glowColor = biomeIndex switch
            {
                1 => new Color(.16f, .8f, .92f, .075f),
                2 => new Color(1f, .16f, .08f, .085f),
                _ => new Color(1f, .55f, .18f, .065f)
            };
            glowColor.a *= Mathf.Lerp(.72f, 1.55f, nightFactor);
            if (glowLeft != null) glowLeft.color = glowColor;
            if (glowRight != null) glowRight.color = glowColor;
        }

        private int ActiveRainCount() => biomeIndex switch
        {
            1 => 56,
            2 => 14,
            _ => 0
        };

        private int ActiveMistCount() => biomeIndex switch
        {
            1 => 8,
            2 => 5,
            _ => 3
        };

        private int ActiveMoteCount() => biomeIndex switch
        {
            1 => 6,
            2 => 18,
            _ => 10
        };

        private void ApplyGlowAlpha(SpriteRenderer renderer, float multiplier)
        {
            if (renderer == null) return;
            Color color = renderer.color;
            float baseAlpha = biomeIndex == 2 ? .085f : biomeIndex == 1 ? .075f : .065f;
            float nightFactor = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.42f, 1f, journeyProgress));
            color.a = baseAlpha * Mathf.Lerp(.72f, 1.55f, nightFactor) *
                multiplier * (battleContext ? .72f : 1f);
            renderer.color = color;
        }

        private Sprite CreateSprite(string name, Texture2D texture, float pixelsPerUnit)
        {
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(.5f, .5f),
                pixelsPerUnit);
            sprite.name = name;
            ownedAssets.Add(sprite);
            return sprite;
        }

        private Texture2D CreateRainTexture()
        {
            const int width = 8;
            const int height = 48;
            Texture2D texture = CreateTexture("journey-rain-streak", width, height);
            Color32[] pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                float vertical = Mathf.Sin((y + .5f) / height * Mathf.PI);
                for (int x = 0; x < width; x++)
                {
                    float horizontal = 1f - Mathf.Abs((x + .5f) / width * 2f - 1f);
                    byte alpha = (byte)(Mathf.Clamp01(vertical * horizontal * horizontal) * 255f);
                    pixels[y * width + x] = new Color32(255, 255, 255, alpha);
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            return texture;
        }

        private Texture2D CreateMoteTexture()
        {
            const int size = 24;
            Texture2D texture = CreateTexture("journey-wind-mote", size, size);
            Color32[] pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float nx = (x + .5f - size * .5f) / (size * .5f);
                float ny = (y + .5f - size * .5f) / (size * .5f);
                float alpha = Mathf.Clamp01(1f - nx * nx * 2.2f - ny * ny * 7f);
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            return texture;
        }

        private Texture2D CreateSoftDiscTexture(int size)
        {
            Texture2D texture = CreateTexture("journey-soft-disc", size, size);
            Color32[] pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float nx = (x + .5f - size * .5f) / (size * .5f);
                float ny = (y + .5f - size * .5f) / (size * .5f);
                float alpha = Mathf.Pow(Mathf.Clamp01(1f - nx * nx - ny * ny), 2.3f);
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            return texture;
        }

        private Texture2D CreateTexture(string name, int width, int height)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };
            ownedAssets.Add(texture);
            return texture;
        }

        private static float Hash01(int value)
        {
            uint hash = (uint)value;
            hash ^= hash >> 16;
            hash *= 0x7feb352d;
            hash ^= hash >> 15;
            hash *= 0x846ca68b;
            hash ^= hash >> 16;
            return (hash & 0x00ffffff) / 16777215f;
        }

        private void OnDestroy()
        {
            foreach (Object asset in ownedAssets)
                if (asset != null) Destroy(asset);
            ownedAssets.Clear();
            if (environmentRoot != null) Destroy(environmentRoot);
        }
    }
}
