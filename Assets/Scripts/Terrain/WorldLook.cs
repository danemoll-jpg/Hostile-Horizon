using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HostileHorizon
{
    // Round 1, item 2: the look. The sun (a directional light with shadows), the alien sky, ambient light, the shared haze and the
    // rock material with its two generated textures (tileable rock detail; sediment-layer colours). Also applies the graphics quality
    // and the Steam Deck setting (view distance, shadows, anti-aliasing).
    public static class WorldLook
    {
        public static Light Sun { get; private set; }
        public static Material RockMaterial { get; private set; }
        public static readonly Color Haze = new Color(.80f, .56f, .45f), SunHaze = new Color(1f, .78f, .58f);
        static readonly Vector3 SunDirection = Quaternion.Euler(24f, 230f, 0) * Vector3.back;   // towards the sun

        public static void Create()
        {
            var go = new GameObject("Sun");
            Sun = go.AddComponent<Light>();
            Sun.type = LightType.Directional; Sun.color = new Color(1f, .87f, .72f); Sun.intensity = 1.7f;
            Sun.shadows = LightShadows.Soft; Sun.shadowBias = .6f; Sun.shadowNormalBias = .8f;
            go.transform.rotation = Quaternion.LookRotation(-SunDirection);
            RenderSettings.sun = Sun;

            var sky = new Material(Shader.Find("HostileHorizon/Sky"));
            RenderSettings.skybox = sky;
            var sh = new SphericalHarmonicsL2();
            sh.AddAmbientLight(new Color(.30f, .30f, .34f));
            sh.AddDirectionalLight(Vector3.up, new Color(.25f, .32f, .40f), 1f);
            RenderSettings.ambientMode = AmbientMode.Custom; RenderSettings.ambientProbe = sh;
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear; RenderSettings.fogColor = Haze;

            Shader.SetGlobalColor("_HHFogColor", Haze.linear);
            Shader.SetGlobalColor("_HHSunHaze", SunHaze.linear);
            Shader.SetGlobalVector("_HHSunDir", SunDirection.normalized);

            RockMaterial = new Material(Shader.Find("HostileHorizon/Rock")) { name = "Rock" };
            RockMaterial.SetTexture("_Detail", DetailTexture());
            RockMaterial.SetTexture("_Strata", StrataTexture());
        }

        public struct Quality { public float view, shadow; public int cascades, msaa, shadowLods; }

        public static Quality Current()
        {
            var s = GameSettings.S;
            var q = s.quality == 0 ? new Quality { view = 1800, shadow = 160, cascades = 1, msaa = 1, shadowLods = 1 }
                  : s.quality == 1 ? new Quality { view = 2500, shadow = 260, cascades = 2, msaa = 2, shadowLods = 2 }
                  : new Quality { view = 3200, shadow = 420, cascades = 2, msaa = 4, shadowLods = 2 };
            if (SteamDeck.On) { q.view *= SteamDeck.ViewScale; q.shadow *= SteamDeck.ViewScale; q.msaa = 1; q.cascades = 1; q.shadowLods = 1; }
            return q;
        }

        public static void ApplyQuality(TerrainStreamer terrain, Camera cam)
        {
            var q = Current();
            if (GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset urp)
            {
                urp.shadowDistance = q.shadow; urp.shadowCascadeCount = q.cascades; urp.msaaSampleCount = q.msaa;
            }
            if (terrain) { terrain.ViewDistance = q.view; terrain.ShadowLods = q.shadowLods; }
            if (cam) cam.farClipPlane = q.view + 400f;
            Shader.SetGlobalVector("_HHFog", new Vector4(1f / 2300f, 40f, 90f, q.view));
            RenderSettings.fogStartDistance = q.view * .2f; RenderSettings.fogEndDistance = q.view;
        }

        // ---- generated textures (no third-party art)

        static Texture2D StrataTexture()
        {
            Color[] palette =
            {
                new Color(.55f, .28f, .18f), new Color(.72f, .52f, .34f), new Color(.80f, .69f, .56f), new Color(.46f, .33f, .36f),
                new Color(.36f, .19f, .14f), new Color(.70f, .43f, .32f), new Color(.60f, .47f, .40f),
            };
            const int N = 256; var r = new System.Random(1984); var raw = new Color[N];
            for (int i = 0; i < N;)
            {
                var c = palette[r.Next(palette.Length)] * (float)(0.9 + r.NextDouble() * 0.2); int w = 3 + r.Next(18);
                for (int k = 0; k < w && i < N; k++, i++) raw[i] = c;
            }
            var px = new Color[N];
            for (int i = 0; i < N; i++) { Color sum = Color.clear; for (int k = -2; k <= 2; k++) sum += raw[(i + k + N) % N]; px[i] = sum / 5f; px[i].a = 1; }
            var t = new Texture2D(N, 1, TextureFormat.RGBA32, false) { name = "Strata", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
            t.SetPixels(px); t.Apply(false, true); return t;
        }

        static Texture2D DetailTexture()
        {
            const int N = 512; var px = new Color32[N * N];
            Parallel.For(0, N, y =>
            {
                for (int x = 0; x < N; x++)
                {
                    float u = x / (float)N, v = y / (float)N;
                    float mottle = Fbm(u, v, 4, 4, 11), grain = Fbm(u, v, 32, 3, 23), c = Fbm(u, v, 6, 4, 37);
                    float crack = Noise.Smooth(.015f, .09f, Math.Abs(c - .5f));
                    px[y * N + x] = new Color32((byte)(mottle * 255), (byte)(grain * 255), (byte)(crack * 255), 255);
                }
            });
            var t = new Texture2D(N, N, TextureFormat.RGBA32, true, true) { name = "RockDetail", wrapMode = TextureWrapMode.Repeat, anisoLevel = 4 };
            t.SetPixels32(px); t.Apply(true, true); return t;
        }

        // tileable value-noise fBm on the unit square (period p cells at the first octave), 0..1
        static float Fbm(float u, float v, int p, int octaves, int salt)
        {
            float sum = 0, amp = .5f, norm = 0;
            for (int o = 0; o < octaves; o++) { sum += Value(u * p, v * p, p, salt + o) * amp; norm += amp; amp *= .5f; p *= 2; }
            return sum / norm;
        }

        static float Value(float x, float y, int p, int salt)
        {
            int xi = (int)Math.Floor(x), yi = (int)Math.Floor(y); float fx = x - xi, fy = y - yi;
            fx = fx * fx * (3 - 2 * fx); fy = fy * fy * (3 - 2 * fy);
            float a = H(xi, yi, p, salt), b = H(xi + 1, yi, p, salt), c = H(xi, yi + 1, p, salt), d = H(xi + 1, yi + 1, p, salt);
            return Noise.Lerp(Noise.Lerp(a, b, fx), Noise.Lerp(c, d, fx), fy);
        }

        static float H(int x, int y, int p, int salt)
        {
            x = ((x % p) + p) % p; y = ((y % p) + p) % p;
            unchecked { uint h = (uint)(x * 374761393 + y * 668265263 + salt * 2147483647); h = (h ^ (h >> 13)) * 1274126177u; return ((h ^ (h >> 16)) & 0xffff) / 65535f; }
        }
    }
}
