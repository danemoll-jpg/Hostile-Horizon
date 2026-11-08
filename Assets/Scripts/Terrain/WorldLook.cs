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
        public static Material SkyMaterial { get; private set; }
        public static Texture DetailTex { get; private set; }
        public static Color Haze = new Color(.80f, .56f, .45f), SunHaze = new Color(1f, .78f, .58f);
        static Vector3 SunDirection = Quaternion.Euler(24f, 230f, 0) * Vector3.back;   // towards the sun
        public static Biome Shown { get; private set; } = (Biome)(-1);
        static float fogDensity = 1f / 2300f;     // Round 4: per biome
        public static float Weather = 0;           // Round 4: 0 clear .. 1 a full blizzard (ice): thicker, whiter haze

        // Round 4, item A5: each biome's sky, sun, haze, light and ground. Palettes are generated (no third-party art).
        public struct Look
        {
            public Color haze, sunHaze, sun, zenith, planetA, planetB, ambient, ambientSky, dust, rockSky, rockGround, deep, hot;
            public float sunPitch, sunYaw, sunIntensity, overcast, fog, mode, flatFrom, flatTo; public Vector3 planetDir; public Color[] strata;
        }

        public static Look LookFor(Biome b)
        {
            switch (b)
            {
                // Round 7 (B2): a dark red sky, smoke overhead, black basalt, grey ash; the lava's light (hot) on the walls
                case Biome.Volcanic:
                    // (0.7.0 check, first look: too dark to read from the cockpit; brighter rock, sun and haze)
                    return new Look { haze = new Color(.55f, .30f, .22f), sunHaze = new Color(1f, .55f, .30f), sun = new Color(1f, .66f, .46f), sunIntensity = 1.6f, sunPitch = 28f, sunYaw = 215f,
                        zenith = new Color(.36f, .12f, .08f), planetA = new Color(.70f, .36f, .24f), planetB = new Color(.34f, .13f, .10f), planetDir = new Vector3(.5f, .3f, -.8f), overcast = .35f,
                        ambient = new Color(.34f, .24f, .22f), ambientSky = new Color(.50f, .30f, .24f), rockSky = new Color(.56f, .40f, .36f), rockGround = new Color(.55f, .24f, .12f),
                        dust = new Color(.30f, .28f, .27f), deep = Color.white, hot = new Color(1f, .40f, .10f), fog = 1f / 2000f, mode = 4, flatFrom = .7f, flatTo = .88f,
                        strata = new[] { new Color(.22f, .20f, .20f), new Color(.30f, .27f, .25f), new Color(.18f, .17f, .18f), new Color(.36f, .28f, .24f), new Color(.26f, .23f, .22f) } };
                // Round 7 (B3): a pale violet sky, dark ground, moss that glows cold teal
                case Biome.Crystal:
                    return new Look { haze = new Color(.62f, .56f, .74f), sunHaze = new Color(.95f, .86f, 1f), sun = new Color(.92f, .88f, 1f), sunIntensity = 1.35f, sunPitch = 30f, sunYaw = 120f,
                        zenith = new Color(.40f, .32f, .62f), planetA = new Color(.80f, .72f, .92f), planetB = new Color(.46f, .40f, .66f), planetDir = new Vector3(-.3f, .5f, .8f), overcast = .05f,
                        ambient = new Color(.22f, .20f, .30f), ambientSky = new Color(.44f, .40f, .60f), rockSky = new Color(.44f, .40f, .58f), rockGround = new Color(.12f, .10f, .16f),
                        dust = new Color(.11f, .10f, .13f), deep = Color.white, hot = new Color(.25f, .95f, .85f), fog = 1f / 1900f, mode = 5, flatFrom = .6f, flatTo = .82f,
                        strata = new[] { new Color(.14f, .12f, .17f), new Color(.20f, .17f, .24f), new Color(.10f, .09f, .12f), new Color(.24f, .20f, .28f) } };
                // Round 7 (B4): a bright sea sky, grey-brown cliffs
                case Biome.Ocean:
                    return new Look { haze = new Color(.66f, .76f, .82f), sunHaze = new Color(1f, .94f, .82f), sun = new Color(1f, .95f, .84f), sunIntensity = 1.6f, sunPitch = 34f, sunYaw = 200f,
                        zenith = new Color(.20f, .42f, .66f), planetA = new Color(.85f, .80f, .70f), planetB = new Color(.50f, .55f, .62f), planetDir = new Vector3(.6f, .25f, .7f), overcast = .05f,
                        ambient = new Color(.30f, .34f, .38f), ambientSky = new Color(.44f, .54f, .64f), rockSky = new Color(.52f, .60f, .68f), rockGround = new Color(.30f, .32f, .30f),
                        dust = new Color(.36f, .40f, .26f), deep = Color.white, hot = new Color(1f, .4f, .1f), fog = 1f / 2400f, mode = 6, flatFrom = .7f, flatTo = .88f,
                        strata = new[] { new Color(.40f, .36f, .32f), new Color(.52f, .48f, .42f), new Color(.30f, .28f, .27f), new Color(.46f, .40f, .34f), new Color(.36f, .34f, .33f) } };
                case Biome.Forest:
                    return new Look { haze = new Color(.52f, .58f, .40f), sunHaze = new Color(1f, .88f, .58f), sun = new Color(1f, .9f, .66f), sunIntensity = 1.5f, sunPitch = 38f, sunYaw = 205f,
                        zenith = new Color(.26f, .46f, .52f), planetA = new Color(.62f, .70f, .62f), planetB = new Color(.30f, .40f, .38f), planetDir = new Vector3(.6f, .35f, .7f), overcast = .1f,
                        ambient = new Color(.22f, .26f, .18f), ambientSky = new Color(.40f, .48f, .32f), rockSky = new Color(.42f, .50f, .34f), rockGround = new Color(.18f, .16f, .09f),
                        dust = new Color(.24f, .30f, .12f), deep = Color.white, fog = 1f / 1500f, mode = 1, flatFrom = .55f, flatTo = .8f,
                        strata = new[] { new Color(.26f, .22f, .16f), new Color(.34f, .30f, .20f), new Color(.22f, .25f, .16f), new Color(.40f, .34f, .24f), new Color(.18f, .17f, .13f) } };
                case Biome.City:
                    return new Look { haze = new Color(.50f, .45f, .42f), sunHaze = new Color(.92f, .62f, .40f), sun = new Color(1f, .72f, .50f), sunIntensity = 1.35f, sunPitch = 16f, sunYaw = 250f,
                        zenith = new Color(.22f, .21f, .24f), planetA = new Color(.55f, .45f, .40f), planetB = new Color(.30f, .24f, .24f), planetDir = new Vector3(-.5f, .3f, .8f), overcast = .45f,
                        ambient = new Color(.26f, .26f, .28f), ambientSky = new Color(.38f, .38f, .42f), rockSky = new Color(.50f, .50f, .55f), rockGround = new Color(.26f, .23f, .21f),
                        dust = new Color(.16f, .15f, .15f), deep = new Color(.75f, .74f, .78f), fog = 1f / 1700f, mode = 2, flatFrom = .8f, flatTo = .95f,
                        strata = new[] { new Color(.28f, .27f, .26f), new Color(.34f, .32f, .30f), new Color(.24f, .24f, .25f), new Color(.30f, .28f, .25f) } };
                case Biome.Ice:
                    return new Look { haze = new Color(.70f, .78f, .86f), sunHaze = new Color(1f, .96f, .88f), sun = new Color(1f, .97f, .92f), sunIntensity = 1.55f, sunPitch = 18f, sunYaw = 160f,
                        zenith = new Color(.22f, .36f, .58f), planetA = new Color(.80f, .82f, .90f), planetB = new Color(.48f, .55f, .70f), planetDir = new Vector3(-.4f, .45f, -.75f), overcast = 0,
                        ambient = new Color(.34f, .38f, .46f), ambientSky = new Color(.48f, .56f, .70f), rockSky = new Color(.56f, .64f, .80f), rockGround = new Color(.40f, .46f, .58f),
                        dust = new Color(.90f, .93f, .98f), deep = new Color(.62f, .74f, .96f), fog = 1f / 2600f, mode = 3, flatFrom = .62f, flatTo = .82f,
                        strata = new[] { new Color(.62f, .76f, .90f), new Color(.80f, .88f, .96f), new Color(.42f, .60f, .84f), new Color(.92f, .95f, .99f), new Color(.52f, .70f, .90f) } };
                default:
                    return new Look { haze = new Color(.80f, .56f, .45f), sunHaze = new Color(1f, .78f, .58f), sun = new Color(1f, .87f, .72f), sunIntensity = 1.7f, sunPitch = 24f, sunYaw = 230f,
                        zenith = new Color(.17f, .38f, .48f), planetA = new Color(.72f, .62f, .52f), planetB = new Color(.42f, .30f, .30f), planetDir = new Vector3(-.55f, .42f, .72f), overcast = 0,
                        ambient = new Color(.30f, .30f, .34f), ambientSky = new Color(.25f, .32f, .40f), rockSky = new Color(.56f, .64f, .72f), rockGround = new Color(.46f, .34f, .30f),
                        dust = new Color(.62f, .48f, .44f), deep = Color.white, fog = 1f / 2300f, mode = 0, flatFrom = .74f, flatTo = .9f,
                        strata = new[] { new Color(.55f, .28f, .18f), new Color(.72f, .52f, .34f), new Color(.80f, .69f, .56f), new Color(.46f, .33f, .36f), new Color(.36f, .19f, .14f), new Color(.70f, .43f, .32f), new Color(.60f, .47f, .40f) } };
            }
        }

        public static void ApplyBiome(Biome b, BiomeProps props = null)
        {
            var L = LookFor(b);
            if (Shown != b)
            {
                Shown = b;
                if (RockMaterial) RockMaterial.SetTexture("_Strata", StrataTexture(L.strata, 1984 + (int)b));
            }
            Haze = L.haze; SunHaze = L.sunHaze; fogDensity = L.fog;
            SunDirection = Quaternion.Euler(L.sunPitch, L.sunYaw, 0) * Vector3.back;
            if (Sun) { Sun.color = L.sun; Sun.intensity = L.sunIntensity; Sun.transform.rotation = Quaternion.LookRotation(-SunDirection); }
            if (SkyMaterial)
            {
                SkyMaterial.SetColor("_Zenith", L.zenith); SkyMaterial.SetColor("_PlanetA", L.planetA); SkyMaterial.SetColor("_PlanetB", L.planetB);
                SkyMaterial.SetVector("_PlanetDir", L.planetDir.normalized); SkyMaterial.SetFloat("_Overcast", L.overcast);
            }
            var sh = new SphericalHarmonicsL2(); sh.AddAmbientLight(L.ambient); sh.AddDirectionalLight(Vector3.up, L.ambientSky * .8f, 1f);
            RenderSettings.ambientProbe = sh;
            if (RockMaterial)
            {
                RockMaterial.SetColor("_Dust", L.dust); RockMaterial.SetColor("_AmbientSky", L.rockSky); RockMaterial.SetColor("_AmbientGround", L.rockGround);
                RockMaterial.SetFloat("_Mode", L.mode); RockMaterial.SetVector("_Flat", new Vector4(L.flatFrom, L.flatTo, 0, 0)); RockMaterial.SetColor("_Deep", L.deep);
                RockMaterial.SetColor("_Hot", L.hot);
            }
            if (props) props.SetColours(Color.white, Color.white, L.ambientSky, L.rockGround);
            Weather = 0;
            ApplyHaze();
        }

        // the haze colours and density, with the weather mixed in (a blizzard: white and thick)
        public static void ApplyHaze()
        {
            var white = new Color(.86f, .90f, .95f);
            var haze = Color.Lerp(Haze, white, Weather * .8f); var sunHaze = Color.Lerp(SunHaze, white, Weather * .8f);
            RenderSettings.fogColor = haze;
            Shader.SetGlobalColor("_HHFogColor", haze.linear);
            Shader.SetGlobalColor("_HHSunHaze", sunHaze.linear);
            Shader.SetGlobalVector("_HHSunDir", SunDirection.normalized);
            var q = Current();
            float density = fogDensity * (1f + Weather * 9f);
            float end = Mathf.Lerp(q.view, Mathf.Min(q.view, 450f), Weather);
            Shader.SetGlobalVector("_HHFog", new Vector4(density, 40f, 90f, end));
            RenderSettings.fogStartDistance = end * Mathf.Lerp(.2f, .02f, Weather); RenderSettings.fogEndDistance = end;
        }

        public static void Create()
        {
            var go = new GameObject("Sun");
            Sun = go.AddComponent<Light>();
            Sun.type = LightType.Directional; Sun.color = new Color(1f, .87f, .72f); Sun.intensity = 1.7f;
            Sun.shadows = LightShadows.Soft; Sun.shadowBias = .6f; Sun.shadowNormalBias = .8f;
            go.transform.rotation = Quaternion.LookRotation(-SunDirection);
            RenderSettings.sun = Sun;

            var sky = new Material(Shader.Find("HostileHorizon/Sky"));
            RenderSettings.skybox = sky; SkyMaterial = sky;
            var sh = new SphericalHarmonicsL2();
            sh.AddAmbientLight(new Color(.30f, .30f, .34f));
            sh.AddDirectionalLight(Vector3.up, new Color(.25f, .32f, .40f), 1f);
            RenderSettings.ambientMode = AmbientMode.Custom; RenderSettings.ambientProbe = sh;
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear; RenderSettings.fogColor = Haze;

            Shader.SetGlobalColor("_HHFogColor", Haze.linear);
            Shader.SetGlobalColor("_HHSunHaze", SunHaze.linear);
            Shader.SetGlobalVector("_HHSunDir", SunDirection.normalized);

            RockMaterial = new Material(Shader.Find("HostileHorizon/Rock")) { name = "Rock" };
            DetailTex = DetailTexture();
            RockMaterial.SetTexture("_Detail", DetailTex);
            RockMaterial.SetTexture("_Strata", StrataTexture(LookFor(Biome.Canyons).strata, 1984));
        }

        public struct Quality { public float view, shadow; public int cascades, msaa, shadowLods; }

        public static Quality Current()
        {
            var s = GameSettings.S;
            var q = s.quality == 0 ? new Quality { view = 1800, shadow = 160, cascades = 1, msaa = 1, shadowLods = 1 }
                  : s.quality == 1 ? new Quality { view = 2500, shadow = 260, cascades = 2, msaa = 2, shadowLods = 2 }
                  : new Quality { view = 3200, shadow = 420, cascades = 2, msaa = 4, shadowLods = 2 };
            if (SteamDeck.On) { q.view *= SteamDeck.ViewScale; q.shadow *= SteamDeck.ViewScale; q.msaa = 1; q.cascades = 1; q.shadowLods = 1; }
            // Round 8 (C7): the VR quality preset (two eyes at the headset's rate on a 1660 Ti): a shorter view, one short shadow cascade,
            // 2x MSAA (edges matter in a headset), render scale 0.9 (ApplyQuality)
            if (VrMode.Active) { q.view = Mathf.Min(q.view, 2200); q.shadow = Mathf.Min(q.shadow, 150); q.cascades = 1; q.msaa = 2; q.shadowLods = 1; }
            return q;
        }

        public static float VrRenderScale = .9f;
        public static void ApplyQuality(TerrainStreamer terrain, Camera cam)
        {
            var q = Current();
            if (GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset urp)
            {
                urp.shadowDistance = q.shadow; urp.shadowCascadeCount = q.cascades; urp.msaaSampleCount = q.msaa;
                urp.renderScale = VrMode.Active ? VrRenderScale : 1f;   // Round 8 (C7)
            }
            if (terrain) { terrain.ViewDistance = q.view; terrain.ShadowLods = q.shadowLods; }
            if (cam) cam.farClipPlane = q.view + 400f;
            ApplyHaze();
        }

        // ---- generated textures (no third-party art)

        static Texture2D StrataTexture(Color[] palette, int seed)
        {
            const int N = 256; var r = new System.Random(seed); var raw = new Color[N];
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
