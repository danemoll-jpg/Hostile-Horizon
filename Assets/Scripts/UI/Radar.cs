using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace HostileHorizon
{
    // Round 2, item 5: the radar. Bearing and distance to each beacon, more exact the closer you get:
    //  - over 3 km: a wide faint blip on the scope's rim, bearing to the nearest 30°, "far";
    //  - 1 - 3 km: a blip on the rim, bearing to the nearest 10°, distance to half a kilometre;
    //  - within 1 km: the exact spot on the scope (rim = 1 km), distance to 10 m.
    // Up on the scope is the ship's nose. The mothership shows as a blue blip when you can dock. The same scope is drawn on the
    // cockpit's right display and on the flat-screen HUD (VR rule 2).
    public struct RadarContact { public Vector3 position; public bool mothership; public int number; }

    public sealed class RadarScope
    {
        public const float NearRange = 1000f, MidRange = 3000f;
        static Sprite disc, ring;
        readonly RectTransform root; readonly List<Image> blips = new List<Image>(); readonly float radius;

        public RadarScope(Transform parent, Vector2 centre, float size, Vector2 anchor)
        {
            MakeSprites();
            root = UiKit.Rect(parent, "Radar", anchor, anchor, centre - Vector2.one * size / 2, centre + Vector2.one * size / 2);
            radius = size / 2 * .9f;
            Img(root, disc, new Color(.02f, .08f, .07f, .85f), Vector2.zero, size);
            Img(root, ring, new Color(.4f, 1f, .8f, .55f), Vector2.zero, size * .92f);
            Img(root, ring, new Color(.4f, 1f, .8f, .25f), Vector2.zero, size * .46f);
            var h = Img(root, null, new Color(.4f, 1f, .8f, .18f), Vector2.zero, 0); h.rectTransform.sizeDelta = new Vector2(size * .9f, 2);
            var v = Img(root, null, new Color(.4f, 1f, .8f, .18f), Vector2.zero, 0); v.rectTransform.sizeDelta = new Vector2(2, size * .9f);
            var ship = Img(root, null, new Color(1f, .62f, .32f), Vector2.zero, 0); ship.rectTransform.sizeDelta = new Vector2(size * .035f, size * .07f);
        }

        static Image Img(Transform parent, Sprite s, Color c, Vector2 at, float size)
        {
            var i = UiKit.Panel(parent, "Mark", c, new Vector2(.5f, .5f), new Vector2(.5f, .5f), at - Vector2.one * size / 2, at + Vector2.one * size / 2);
            i.sprite = s; return i;
        }

        static void MakeSprites()
        {
            if (disc) return;
            Texture2D T(int n, System.Func<float, float> a)
            {
                var t = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
                var px = new Color32[n * n];
                for (int y = 0; y < n; y++) for (int x = 0; x < n; x++) { float r = new Vector2(x + .5f - n / 2f, y + .5f - n / 2f).magnitude / (n / 2f); px[y * n + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(a(r)) * 255)); }
                t.SetPixels32(px); t.Apply(); return t;
            }
            var d = T(64, r => (1 - r) * 8f); disc = Sprite.Create(d, new Rect(0, 0, 64, 64), Vector2.one * .5f);
            var g = T(256, r => 1 - Mathf.Abs(r - .97f) * 90f); ring = Sprite.Create(g, new Rect(0, 0, 256, 256), Vector2.one * .5f);
        }

        public void Show(bool on) => root.gameObject.SetActive(on);

        public void Draw(Transform ship, List<RadarContact> contacts)
        {
            float pulse = .65f + .35f * Mathf.Sin(Time.time * 5f);
            for (int i = 0; i < contacts.Count; i++)
            {
                if (blips.Count <= i) blips.Add(Img(root, disc, Color.white, Vector2.zero, 0));
                var c = contacts[i]; var b = blips[i]; b.enabled = true;
                Vector3 d = c.position - ship.position; d.y = 0;
                float dist = d.magnitude;
                float bearing = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg - ship.eulerAngles.y;
                float r, size; Color col = c.mothership ? new Color(.45f, .75f, 1f) : new Color(1f, .75f, .3f);
                if (dist > MidRange) { bearing = Mathf.Round(bearing / 30f) * 30f; r = 1; size = radius * .34f; col.a = .45f * pulse; }
                else if (dist > NearRange) { bearing = Mathf.Round(bearing / 10f) * 10f; r = 1; size = radius * .2f; col.a = .8f * pulse; }
                else { r = dist / NearRange; size = radius * .13f; col.a = 1; }
                float a = bearing * Mathf.Deg2Rad;
                var at = new Vector2(Mathf.Sin(a), Mathf.Cos(a)) * r * radius;
                b.color = col; b.rectTransform.offsetMin = at - Vector2.one * size / 2; b.rectTransform.offsetMax = at + Vector2.one * size / 2;
            }
            for (int i = contacts.Count; i < blips.Count; i++) blips[i].enabled = false;
        }

        // The text list beside the scope: the nearest beacons, as exact as the distance allows.
        public static string List(Transform ship, List<RadarContact> contacts, int max)
        {
            var sorted = new List<RadarContact>(contacts);
            sorted.Sort((x, y) => (x.position - ship.position).sqrMagnitude.CompareTo((y.position - ship.position).sqrMagnitude));
            var sb = new StringBuilder();
            int shown = 0;
            foreach (var c in sorted)
            {
                if (shown++ >= max) break;
                Vector3 d = c.position - ship.position; d.y = 0; float dist = d.magnitude;
                float brg = (Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg + 360f) % 360f;
                string name = c.mothership ? "MOTHERSHIP" : $"PILOT {c.number}";
                if (dist > MidRange) sb.AppendLine($"{name}  ~{Mathf.Round(brg / 30f) * 30f % 360:000}°  far");
                else if (dist > NearRange) sb.AppendLine($"{name}  {Mathf.Round(brg / 10f) * 10f % 360:000}°  {Mathf.Round(dist / 500f) * .5f:0.0} km");
                else sb.AppendLine($"{name}  {brg:000}°  {Mathf.Round(dist / 10f) * 10f:0} m");
            }
            return sb.ToString();
        }
    }
}
