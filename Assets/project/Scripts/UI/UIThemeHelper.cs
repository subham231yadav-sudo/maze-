using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    /// <summary>
    /// Shared aesthetic theme, high-resolution procedural sprite factory, and audio synthesizer
    /// that guarantees ultra-sharp, anti-aliased UI rendering with no blurry text or low-res textures.
    /// </summary>
    public static class UIThemeHelper
    {
        // Color Palette
        public static readonly Color GoldAccent = new Color(1f, 0.82f, 0.22f, 1f);
        public static readonly Color GoldGlow = new Color(1f, 0.88f, 0.4f, 0.9f);
        public static readonly Color CyanAccent = new Color(0.2f, 0.85f, 1f, 1f);
        public static readonly Color EmeraldGreen = new Color(0.18f, 0.88f, 0.45f, 1f);
        public static readonly Color RubyRed = new Color(0.95f, 0.22f, 0.25f, 1f);
        public static readonly Color DarkBackdrop = new Color(0.04f, 0.05f, 0.08f, 0.88f);
        public static readonly Color GlassCardBg = new Color(0.08f, 0.10f, 0.16f, 0.90f);
        public static readonly Color GlassBorder = new Color(0.35f, 0.45f, 0.65f, 0.55f);
        public static readonly Color TextPrimary = new Color(0.96f, 0.97f, 1.0f, 1f);
        public static readonly Color TextSecondary = new Color(0.70f, 0.74f, 0.85f, 0.9f);

        // Cached procedural sprites
        private static Sprite cachedRoundedCard;
        private static Sprite cachedPillSprite;
        private static Sprite cachedStarSprite;
        private static Sprite cachedVignetteSprite;
        private static Font cachedFont;
        private static AudioClip cachedVictoryClip;
        private static AudioClip cachedClickClip;
        private static AudioClip cachedHoverClip;

        /// <summary>
        /// Configures a CanvasScaler to render with ultra-crisp dynamic font rasterization,
        /// solving the common Unity UI blurriness issue on scaled displays.
        /// </summary>
        public static void ApplyHighQualityCanvasSettings(CanvasScaler scaler)
        {
            if (scaler == null) return;
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            // 3.5x dynamic pixels per unit guarantees dynamic fonts rasterize at crystal-clear sharpness
            scaler.dynamicPixelsPerUnit = 3.5f;
            scaler.referencePixelsPerUnit = 100f;
        }

        public static Font GetCleanFont()
        {
            if (cachedFont != null) return cachedFont;
            cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ??
                         Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (cachedFont != null && cachedFont.material != null && cachedFont.material.mainTexture != null)
            {
                cachedFont.material.mainTexture.filterMode = FilterMode.Bilinear;
            }
            return cachedFont;
        }

        /// <summary>
        /// Creates or returns a cached 9-sliced rounded rectangle sprite with a crisp border.
        /// </summary>
        public static Sprite GetRoundedCardSprite()
        {
            if (cachedRoundedCard != null) return cachedRoundedCard;

            int size = 128;
            int radius = 24;
            int border = 3;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color insideColor = Color.white;
            Color borderColor = new Color(1f, 1f, 1f, 0.85f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = GetRoundedRectDistance(x, y, size, size, radius);
                    if (dist > 0f)
                    {
                        // Outside rounded rect
                        float alpha = Mathf.Clamp01(1f - dist);
                        tex.SetPixel(x, y, new Color(0, 0, 0, 0));
                    }
                    else if (dist > -border)
                    {
                        // Border line
                        float t = Mathf.InverseLerp(-border, 0f, dist);
                        float edgeAlpha = Mathf.Clamp01(1f - Mathf.Abs(dist));
                        tex.SetPixel(x, y, new Color(borderColor.r, borderColor.g, borderColor.b, borderColor.a * edgeAlpha));
                    }
                    else
                    {
                        // Inner fill
                        tex.SetPixel(x, y, insideColor);
                    }
                }
            }
            tex.Apply();

            Vector4 borderBorder = new Vector4(radius, radius, radius, radius);
            cachedRoundedCard = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, borderBorder);
            return cachedRoundedCard;
        }

        /// <summary>
        /// Creates or returns a cached 9-sliced smooth pill sprite (capsule).
        /// </summary>
        public static Sprite GetPillSprite()
        {
            if (cachedPillSprite != null) return cachedPillSprite;

            int w = 96;
            int h = 48;
            int radius = h / 2;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dist = GetRoundedRectDistance(x, y, w, h, radius);
                    if (dist > 0f)
                    {
                        float alpha = Mathf.Clamp01(1f - dist);
                        tex.SetPixel(x, y, new Color(1, 1, 1, alpha));
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.white);
                    }
                }
            }
            tex.Apply();

            Vector4 borders = new Vector4(radius, radius, radius, radius);
            cachedPillSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, borders);
            return cachedPillSprite;
        }

        /// <summary>
        /// Creates a crisp vector 5-point star sprite for victory ratings.
        /// </summary>
        public static Sprite GetStarSprite()
        {
            if (cachedStarSprite != null) return cachedStarSprite;

            int size = 96;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float outerR = size * 0.44f;
            float innerR = size * 0.19f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 pt = new Vector2(x + 0.5f, y + 0.5f);
                    if (IsInsideStar(pt, center, 5, outerR, innerR))
                    {
                        tex.SetPixel(x, y, Color.white);
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }
            tex.Apply();
            cachedStarSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            return cachedStarSprite;
        }

        /// <summary>
        /// Distance estimator to rounded rectangle for smooth anti-aliased edges.
        /// </summary>
        private static float GetRoundedRectDistance(float x, float y, float w, float h, float radius)
        {
            float halfW = w * 0.5f;
            float halfH = h * 0.5f;
            float qx = Mathf.Abs(x - halfW) - halfW + radius;
            float qy = Mathf.Abs(y - halfH) - halfH + radius;

            float outsideDist = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude;
            float insideDist = Mathf.Min(Mathf.Max(qx, qy), 0f);
            return outsideDist + insideDist - radius;
        }

        private static bool IsInsideStar(Vector2 pt, Vector2 center, int numRays, float rOuter, float rInner)
        {
            Vector2 d = pt - center;
            float dist = d.magnitude;
            if (dist > rOuter) return false;
            if (dist < rInner * 0.8f) return true;

            float angle = Mathf.Atan2(d.y, d.x) + Mathf.PI * 0.5f;
            if (angle < 0f) angle += Mathf.PI * 2f;

            float step = Mathf.PI * 2f / (numRays * 2);
            int idx = Mathf.FloorToInt(angle / step);
            float f = (angle - idx * step) / step;

            float r1 = (idx % 2 == 0) ? rOuter : rInner;
            float r2 = (idx % 2 == 0) ? rInner : rOuter;
            float expectedR = Mathf.Lerp(r1, r2, f);

            return dist <= expectedR;
        }

        // ==========================================
        // SYNTHESIZED PROCEDURAL AUDIO EFFECTS
        // ==========================================

        /// <summary>
        /// Synthesizes a glorious, shimmering victory fanfare chime (C5 - E5 - G5 - C6 arpeggio).
        /// Plays immediately with zero external asset dependencies!
        /// </summary>
        public static AudioClip GetVictoryFanfareClip()
        {
            if (cachedVictoryClip != null) return cachedVictoryClip;

            int sampleRate = 44100;
            float totalDuration = 2.4f;
            int totalSamples = Mathf.FloorToInt(sampleRate * totalDuration);
            float[] data = new float[totalSamples];

            // Note frequencies: C5, E5, G5, C6, G6
            float[] notes = { 523.25f, 659.25f, 783.99f, 1046.50f, 1567.98f };
            float[] startTimes = { 0.0f, 0.18f, 0.36f, 0.56f, 0.82f };
            float[] durations = { 1.2f, 1.2f, 1.4f, 1.8f, 1.6f };

            for (int n = 0; n < notes.Length; n++)
            {
                float freq = notes[n];
                float startTime = startTimes[n];
                float dur = durations[n];
                int startIdx = Mathf.FloorToInt(startTime * sampleRate);
                int count = Mathf.FloorToInt(dur * sampleRate);

                for (int i = 0; i < count; i++)
                {
                    int targetIdx = startIdx + i;
                    if (targetIdx >= totalSamples) break;

                    float t = (float)i / sampleRate;
                    float progress = (float)i / count;

                    // Bell envelope (quick attack, gentle exponential decay)
                    float env = Mathf.Exp(-progress * 4.2f) * (1f - Mathf.Exp(-progress * 40f));

                    // Fundamental + harmonics + shimmering chorus
                    float sample = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.45f
                                 + Mathf.Sin(2f * Mathf.PI * freq * 2f * t) * 0.22f
                                 + Mathf.Sin(2f * Mathf.PI * freq * 3f * t) * 0.10f
                                 + Mathf.Sin(2f * Mathf.PI * (freq + 2f) * t) * 0.15f;

                    data[targetIdx] += sample * env * 0.55f;
                }
            }

            // Normalize
            float max = 0f;
            for (int i = 0; i < totalSamples; i++)
            {
                if (Mathf.Abs(data[i]) > max) max = Mathf.Abs(data[i]);
            }
            if (max > 0.001f)
            {
                for (int i = 0; i < totalSamples; i++) data[i] = (data[i] / max) * 0.85f;
            }

            cachedVictoryClip = AudioClip.Create("VictoryFanfare", totalSamples, 1, sampleRate, false);
            cachedVictoryClip.SetData(data, 0);
            return cachedVictoryClip;
        }

        public static AudioClip GetButtonClickClip()
        {
            if (cachedClickClip != null) return cachedClickClip;

            int sampleRate = 44100;
            float duration = 0.08f;
            int samples = Mathf.FloorToInt(sampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Exp(-t * 50f);
                data[i] = Mathf.Sin(2f * Mathf.PI * 880f * t) * env * 0.5f;
            }

            cachedClickClip = AudioClip.Create("BtnClick", samples, 1, sampleRate, false);
            cachedClickClip.SetData(data, 0);
            return cachedClickClip;
        }

        public static AudioClip GetButtonHoverClip()
        {
            if (cachedHoverClip != null) return cachedHoverClip;

            int sampleRate = 44100;
            float duration = 0.04f;
            int samples = Mathf.FloorToInt(sampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Exp(-t * 90f);
                data[i] = Mathf.Sin(2f * Mathf.PI * 1200f * t) * env * 0.25f;
            }

            cachedHoverClip = AudioClip.Create("BtnHover", samples, 1, sampleRate, false);
            cachedHoverClip.SetData(data, 0);
            return cachedHoverClip;
        }
    }
}
