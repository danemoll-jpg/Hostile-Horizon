using UnityEngine;
using UnityEngine.Rendering;

namespace HostileHorizon
{
    // Round 7 (B2, B4): the sea (Ocean Islands) or the lava (Volcanic): one flat grid at the liquid's level (CanyonField.Liquid)
    // that follows the viewer, snapped to its own cells so the waves don't swim; out to the terrain's view distance. Hidden in the
    // other biomes. Drawn with Shaders/Liquid.shader; what it does to the ship is ShipController's (OverLiquid / Collide).
    public sealed class Liquid : MonoBehaviour
    {
        Material mat; MeshRenderer mr; float size;

        public static Liquid Create(Texture detail)
        {
            var go = new GameObject("Liquid"); var l = go.AddComponent<Liquid>();
            l.mat = new Material(Shader.Find("HostileHorizon/Liquid")) { name = "Liquid" }; l.mat.SetTexture("_Detail", detail);
            go.AddComponent<MeshFilter>().sharedMesh = Grid(48);
            l.mr = go.AddComponent<MeshRenderer>(); l.mr.sharedMaterial = l.mat; l.mr.shadowCastingMode = ShadowCastingMode.Off; l.mr.receiveShadows = true;
            go.SetActive(false);
            return l;
        }

        // a unit grid (-1..1), finer in the middle (where the viewer is)
        static Mesh Grid(int n)
        {
            var v = new Vector3[(n + 1) * (n + 1)]; var t = new int[n * n * 6];
            for (int j = 0; j <= n; j++)
                for (int i = 0; i <= n; i++)
                {
                    float u = i / (float)n * 2 - 1, w = j / (float)n * 2 - 1;
                    v[j * (n + 1) + i] = new Vector3(Mathf.Sign(u) * u * u, 0, Mathf.Sign(w) * w * w);
                }
            int k = 0;
            for (int j = 0; j < n; j++) for (int i = 0; i < n; i++) { int a = j * (n + 1) + i; t[k++] = a; t[k++] = a + n + 1; t[k++] = a + 1; t[k++] = a + 1; t[k++] = a + n + 1; t[k++] = a + n + 2; }
            var m = new Mesh { name = "Liquid grid" }; m.vertices = v; m.triangles = t; m.RecalculateNormals(); m.bounds = new Bounds(Vector3.zero, new Vector3(2, .1f, 2)); return m;
        }

        public void SetBiome(CanyonField f, WorldLook.Look look)
        {
            bool on = f != null && f.HasLiquid; gameObject.SetActive(on); if (!on) return;
            mat.SetFloat("_Mode", f.IsLava ? 1 : 0);
            mat.SetColor("_SkyLow", look.haze); mat.SetColor("_SkyHigh", look.zenith);
            transform.position = new Vector3(0, f.Liquid, 0);
        }

        public void Follow(Vector3 viewer, float view)
        {
            if (!gameObject.activeSelf) return;
            size = view + 600f;
            const float snap = 64f;
            transform.position = new Vector3(Mathf.Round(viewer.x / snap) * snap, transform.position.y, Mathf.Round(viewer.z / snap) * snap);
            transform.localScale = new Vector3(size, 1, size);
        }
    }
}
