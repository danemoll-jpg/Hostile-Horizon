using UnityEngine;

namespace HostileHorizon
{
    // Round 7 (B4): the Ocean Islands' fliers: a handful of harmless gull-like creatures (two long, thin, flapping wing blades and a
    // body, code-made) wheeling round the island tops near the viewer. No collision, no game rules; seeded per planet.
    public sealed class Fliers : MonoBehaviour
    {
        const int N = 7;
        readonly Transform[] birds = new Transform[N], wingL = new Transform[N], wingR = new Transform[N];
        readonly Vector3[] centre = new Vector3[N]; readonly float[] radius = new float[N], speed = new float[N], phase = new float[N], height = new float[N];
        bool on; float clock;

        public static Fliers Create()
        {
            var f = new GameObject("Fliers").AddComponent<Fliers>();
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); mat.SetColor("_BaseColor", new Color(.82f, .80f, .74f)); mat.SetFloat("_Smoothness", .2f);
            var wingMat = new Material(mat); wingMat.SetColor("_BaseColor", new Color(.22f, .22f, .24f));
            for (int i = 0; i < N; i++)
            {
                var b = new GameObject("Flier " + i).transform; b.SetParent(f.transform, false);
                var body = GameObject.CreatePrimitive(PrimitiveType.Capsule); Destroy(body.GetComponent<Collider>()); body.transform.SetParent(b, false);
                body.transform.localScale = new Vector3(.25f, .55f, .25f); body.transform.localRotation = Quaternion.Euler(90, 0, 0); body.GetComponent<Renderer>().sharedMaterial = mat;
                for (int s = -1; s <= 1; s += 2)
                {
                    var pivot = new GameObject(s < 0 ? "WingL" : "WingR").transform; pivot.SetParent(b, false);
                    var w = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(w.GetComponent<Collider>()); w.transform.SetParent(pivot, false);
                    w.transform.localScale = new Vector3(1.5f, .04f, .4f); w.transform.localPosition = new Vector3(s * .8f, 0, 0); w.GetComponent<Renderer>().sharedMaterial = wingMat;
                    if (s < 0) f.wingL[i] = pivot; else f.wingR[i] = pivot;
                }
                foreach (var r in b.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                f.birds[i] = b;
            }
            f.gameObject.SetActive(false);
            return f;
        }

        public void SetBiome(Biome b, int seed)
        {
            on = b == Biome.Ocean; gameObject.SetActive(on); if (!on) return;
            var r = new SeedRng(seed, 707);
            for (int i = 0; i < N; i++) { radius[i] = Mathf.Lerp(40f, 140f, r.Value); speed[i] = Mathf.Lerp(.12f, .3f, r.Value) * (r.Value < .5f ? -1 : 1); phase[i] = r.Value * 6.3f; height[i] = Mathf.Lerp(25f, 70f, r.Value); centre[i] = new Vector3(1e9f, 0, 0); }
        }

        // wheel round points near the viewer (picked again when the viewer has moved on)
        public void Tick(Vector3 viewer, CanyonField f, float dt)
        {
            if (!on || f == null) return;
            clock += dt;
            for (int i = 0; i < N; i++)
            {
                if ((Flat(centre[i] - viewer)).magnitude > 900f)
                {
                    float a = phase[i] + clock * .01f; var c = viewer + new Vector3(Mathf.Cos(a * 3.1f), 0, Mathf.Sin(a * 2.3f)) * Mathf.Lerp(250f, 650f, (i % 3) / 2f);
                    c.y = f.SurfaceHeight(c.x, c.z) + height[i]; centre[i] = c;
                }
                float t = phase[i] + clock * speed[i];
                var p = centre[i] + new Vector3(Mathf.Cos(t) * radius[i], Mathf.Sin(t * 2.1f) * 4f, Mathf.Sin(t) * radius[i]);
                var dir = new Vector3(-Mathf.Sin(t), 0, Mathf.Cos(t)) * Mathf.Sign(speed[i]);
                birds[i].SetPositionAndRotation(p, Quaternion.LookRotation(dir) * Quaternion.Euler(0, 0, -Mathf.Sign(speed[i]) * 25f));
                float flap = Mathf.Sin(clock * 6f + i) * (Mathf.Sin(clock * .7f + i) > .2f ? 30f : 4f);   // flaps, then glides
                wingL[i].localRotation = Quaternion.Euler(0, 0, flap); wingR[i].localRotation = Quaternion.Euler(0, 0, -flap);
            }
        }

        static Vector3 Flat(Vector3 v) { v.y = 0; return v; }
    }
}
