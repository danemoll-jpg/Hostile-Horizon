using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace HostileHorizon
{
    // Round 2, item 5: the radar. Up on the scope is the ship's nose. The same scope is drawn on the cockpit's right display and on
    // the flat-screen HUD (VR rule 2).
    // Fix 2.6, item 1: a true-distance scope that zooms by itself (1, 2, 3, 6 or 9 km at the rim: the smallest that holds the
    // nearest pilot), range rings with their distances, every blip labelled with its distance ("1.2 km", "460 m"), the nearest
    // pilot bigger and brighter with a NEAREST tag, the mothership's blip with its distance too, and the route trail (Fix 2.6, item
    // 2) drawn as a dotted line. Contacts past the rim sit on it. Far away the bearing stays rough (to 30° past 3 km), as Round 2's
    // "more detail closer".
    public struct RadarContact { public Vector3 position; public bool mothership; public int number; public int enemy; public string mark; public string tag; }   // Round 9: mark = a mission site's name (RECORDER, NEST, WRECK); tag = after a pilot's label (the timed rescue's clock)   // Round 5: enemy 1 turret, 2 saucer (once spotted)

    public sealed class RadarScope
    {
        public const float NearRange = 1000f, MidRange = 3000f;
        static readonly float[] Ranges = { 1000, 2000, 3000, 6000, 9000 };
        static Sprite disc, ring, halo;
        readonly RectTransform root; readonly float radius, size; readonly int font;
        readonly List<Image> blips = new List<Image>(), halos = new List<Image>(), dots = new List<Image>();
        readonly List<Text> labels = new List<Text>();
        readonly Image[] rings = new Image[3]; readonly Text[] ringLabels = new Text[3];
        public float Range { get; private set; } = 3000;   // metres at the rim (checks read it)
        public static float ExactRange = MidRange;          // Round 6 (C): exact bearings out to here (the Radar upgrade)
        // Round 11 (D1, D2): the filter (All / Pilots / Enemies / Tasks; the mothership and incoming fire always show) and less clutter
        // on All: smaller blips, labels only on the nearest two of each kind, overlapping blips of a kind merged into one with a count,
        // nothing past the rim (but the mothership). OldStyle = Round 10's drawing (checks' "before").
        public static readonly string[] FilterNames = { "ALL", "PILOTS", "ENEMIES", "TASKS" };
        public static int Filter { get => Mathf.Clamp(GameSettings.S.radarFilter, 0, 3); set => GameSettings.S.radarFilter = (value % 4 + 4) % 4; }
        public static bool OldStyle;
        static int KindOf(RadarContact c) => c.mothership ? 0 : c.enemy != 0 ? 2 : c.mark != null ? 3 : 1;   // 0 mothership, 1 pilot, 2 enemy, 3 task
        public static bool Shows(RadarContact c) => OldStyle || Filter == 0 || c.mothership || KindOf(c) == Filter;
        readonly Text header;
        public static int Bezel;                            // Round 6 (C3): the Radar upgrade's level: a bright outer ring
        readonly Image bezel;

        public RadarScope(Transform parent, Vector2 centre, float size, Vector2 anchor, int labelFont)
        {
            MakeSprites(); this.size = size; font = labelFont;
            root = UiKit.Rect(parent, "Radar", anchor, anchor, centre - Vector2.one * size / 2, centre + Vector2.one * size / 2);
            radius = size / 2 * .9f;
            Img(root, disc, new Color(.02f, .08f, .07f, .85f), Vector2.zero, size);
            bezel = Img(root, ring, new Color(.45f, 1f, .9f, .8f), Vector2.zero, size * 1.07f); bezel.enabled = false;
            for (int i = 0; i < 3; i++)
            {
                rings[i] = Img(root, ring, new Color(.4f, 1f, .8f, i == 2 ? .55f : .22f), Vector2.zero, size);
                ringLabels[i] = UiKit.Label(root, "Ring label", Mathf.RoundToInt(font * .8f), TextAnchor.LowerLeft, new Color(.5f, 1f, .85f, .75f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
                ringLabels[i].GetComponent<Shadow>().enabled = false; ringLabels[i].horizontalOverflow = HorizontalWrapMode.Overflow;
            }
            var h = Img(root, null, new Color(.4f, 1f, .8f, .18f), Vector2.zero, 0); h.rectTransform.sizeDelta = new Vector2(size * .9f, 2);
            var v = Img(root, null, new Color(.4f, 1f, .8f, .18f), Vector2.zero, 0); v.rectTransform.sizeDelta = new Vector2(2, size * .9f);
            var ship = Img(root, null, new Color(1f, .62f, .32f), Vector2.zero, 0); ship.rectTransform.sizeDelta = new Vector2(size * .035f, size * .07f);
            header = UiKit.Label(root, "Radar filter", font, TextAnchor.LowerCenter, new Color(.55f, 1f, .85f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-size / 2, size / 2 + 2), new Vector2(size / 2, size / 2 + font * 1.6f));
            header.horizontalOverflow = HorizontalWrapMode.Overflow;
        }

        static Image Img(Transform parent, Sprite s, Color c, Vector2 at, float size)
        {
            var i = UiKit.Panel(parent, "Mark", c, new Vector2(.5f, .5f), new Vector2(.5f, .5f), at - Vector2.one * size / 2, at + Vector2.one * size / 2);
            i.sprite = s; return i;
        }

        static void Place(Graphic g, Vector2 at, float s) { g.rectTransform.offsetMin = at - Vector2.one * s / 2; g.rectTransform.offsetMax = at + Vector2.one * s / 2; }

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
            var o = T(64, r => 1 - Mathf.Abs(r - .85f) * 12f); halo = Sprite.Create(o, new Rect(0, 0, 64, 64), Vector2.one * .5f);
        }

        public void Show(bool on) => root.gameObject.SetActive(on);

        public static string Distance(float m) => m >= MidRange ? $"{Mathf.Round(m / 500f) * .5f:0.0} km" : m >= NearRange ? $"{m / 1000f:0.0} km" : $"{Mathf.Round(m / 10f) * 10f:0} m";

        // The nearest pilot (not the mothership), or -1.
        public static int Nearest(Transform ship, List<RadarContact> contacts)
        {
            int best = -1; float bd = float.MaxValue;
            for (int i = 0; i < contacts.Count; i++)
            {
                if (contacts[i].mothership || contacts[i].enemy != 0 || contacts[i].mark != null) continue;
                var d = contacts[i].position - ship.position; d.y = 0;
                if (d.sqrMagnitude < bd) { bd = d.sqrMagnitude; best = i; }
            }
            return best;
        }

        readonly List<Image> threatMarks = new List<Image>();
        readonly List<Vector2> labelAt = new List<Vector2>();
        readonly HashSet<int> labelled = new HashSet<int>(); readonly List<(Vector2 at, int kind, int i)> placed = new List<(Vector2, int, int)>(); readonly Dictionary<int, int> merged = new Dictionary<int, int>();
        public void Draw(Transform ship, List<RadarContact> contacts, List<Vector3> route = null, List<(Vector3 from, float until)> threats = null)
        {
            // Round 5 (B6): incoming fire: a red mark on the rim toward each shooter, fading in 2.5 s
            int nt = 0;
            if (threats != null)
                foreach (var th in threats)
                {
                    if (nt >= 8) break;
                    var dv = th.from - ship.position; float br = (Mathf.Atan2(dv.x, dv.z) * Mathf.Rad2Deg - ship.eulerAngles.y) * Mathf.Deg2Rad;
                    if (threatMarks.Count <= nt) threatMarks.Add(Img(root, disc, Color.red, Vector2.zero, 0));
                    var m = threatMarks[nt++]; m.enabled = true; m.transform.SetAsLastSibling();
                    m.color = new Color(1f, .15f, .1f, Mathf.Clamp01((th.until - Time.time) / 2.5f) * (.6f + .4f * Mathf.Sin(Time.time * 14f)));
                    Place(m, new Vector2(Mathf.Sin(br), Mathf.Cos(br)) * radius * .97f, size * .16f);
                }
            for (int i = nt; i < threatMarks.Count; i++) threatMarks[i].enabled = false;
            float pulse = .65f + .35f * Mathf.Sin(Time.time * 5f);
            bezel.enabled = Bezel > 0; if (Bezel > 0) { bezel.color = new Color(.45f, 1f, .9f, .35f + .2f * Bezel); Place(bezel, Vector2.zero, size * (1.04f + .03f * Bezel)); }
            header.enabled = !OldStyle; header.text = $"RADAR: {FilterNames[Filter]}";
            int nearest = Nearest(ship, contacts);
            if (!OldStyle && Filter != 0 && Filter != 1) nearest = -1;   // no NEAREST pilot tag while pilots are filtered out
            // the zoom: the smallest range that holds the nearest pilot (the mothership when no pilot is left); Round 11: on the Enemies /
            // Tasks filters, the nearest of those
            float want = 0;
            int focus = nearest >= 0 ? nearest : contacts.FindIndex(c => c.mothership);
            if (!OldStyle && (Filter == 2 || Filter == 3))
            {
                float bd = float.MaxValue;
                for (int i = 0; i < contacts.Count; i++) if (KindOf(contacts[i]) == Filter) { var d = contacts[i].position - ship.position; d.y = 0; if (d.sqrMagnitude < bd) { bd = d.sqrMagnitude; focus = i; } }
            }
            if (focus >= 0) { var d = contacts[focus].position - ship.position; d.y = 0; want = d.magnitude * 1.1f; }
            Range = Ranges[Ranges.Length - 1]; foreach (var r in Ranges) if (r >= want) { Range = r; break; }
            float step = Range <= 1000 ? 500 : Range <= 3000 ? 1000 : Range / 3f;   // rings: 0.5 / 1, 1 / 2, 1 / 2 / 3, 2 / 4 / 6, 3 / 6 / 9 km
            for (int i = 0; i < 3; i++)
            {
                float rm = step * (i + 1);
                bool on = rm <= Range + 1;
                rings[i].enabled = ringLabels[i].enabled = on;
                if (!on) continue;
                rings[i].color = new Color(.4f, 1f, .8f, rm >= Range - 1 ? .55f : .22f);
                float px = 2f * rm / Range * radius / .97f;   // the ring sprite's line sits at .97 of its half size
                Place(rings[i], Vector2.zero, px);
                ringLabels[i].text = rm >= 1000 ? $"{rm / 1000f:0} km" : $"{rm:0} m";
                var at = new Vector2(.69f, .69f) * (rm / Range * radius);   // on the ring, up and right
                ringLabels[i].rectTransform.offsetMin = at + new Vector2(2, 1); ringLabels[i].rectTransform.offsetMax = at + new Vector2(80, font);
            }
            // Round 11 (D2): on All, labels only on the nearest two of each kind; overlapping blips of a kind merge (a count)
            labelled.Clear(); placed.Clear(); labelAt.Clear();
            // NEAREST and the mothership claim their label spots first
            if (!OldStyle)
                for (int i = 0; i < contacts.Count; i++)
                    if (i == nearest || contacts[i].mothership)
                    {
                        Vector3 d0 = contacts[i].position - ship.position; d0.y = 0; float b0 = (Mathf.Atan2(d0.x, d0.z) * Mathf.Rad2Deg - ship.eulerAngles.y) * Mathf.Deg2Rad;
                        var at0 = new Vector2(Mathf.Sin(b0), Mathf.Cos(b0)) * Mathf.Min(1, d0.magnitude / Range) * radius; float s0 = radius * .15f;
                        var o0 = at0 + new Vector2(0, at0.y < -radius * .55f ? s0 * .55f + font * .7f : -(s0 * .55f + font * .7f)); o0.x = Mathf.Clamp(o0.x, -radius * .55f, radius * .55f);
                        labelAt.Add(o0);
                    }
            if (!OldStyle && Filter == 0)
                for (int k = 1; k <= 3; k++)
                {
                    var idx = new List<int>(); for (int i = 0; i < contacts.Count; i++) if (KindOf(contacts[i]) == k) idx.Add(i);
                    idx.Sort((x, y) => (contacts[x].position - ship.position).sqrMagnitude.CompareTo((contacts[y].position - ship.position).sqrMagnitude));
                    for (int j = 0; j < idx.Count && j < 2; j++) labelled.Add(idx[j]);
                }
            for (int i = 0; i < contacts.Count; i++)
            {
                if (blips.Count <= i)
                {
                    halos.Add(Img(root, halo, Color.white, Vector2.zero, 0));
                    blips.Add(Img(root, disc, Color.white, Vector2.zero, 0));
                    var l = UiKit.Label(root, "Blip label", font, TextAnchor.MiddleLeft, Color.white, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
                    l.horizontalOverflow = HorizontalWrapMode.Overflow; l.GetComponent<Shadow>().effectDistance = new Vector2(1.5f, -1.5f); labels.Add(l);
                }
                var c = contacts[i]; var b = blips[i]; b.enabled = true; labels[i].enabled = true;
                Vector3 dv = c.position - ship.position; dv.y = 0;
                if (!Shows(c) || !OldStyle && Filter == 0 && !c.mothership && dv.magnitude > Range) { b.enabled = false; halos[i].enabled = false; labels[i].enabled = false; continue; }
                float dist = dv.magnitude, bearing = Mathf.Atan2(dv.x, dv.z) * Mathf.Rad2Deg - ship.eulerAngles.y;
                if (dist > ExactRange) bearing = Mathf.Round(bearing / 30f) * 30f;
                bool isNear = i == nearest;
                float r = Mathf.Min(1, dist / Range), sz = radius * (isNear ? .19f : c.enemy != 0 ? .1f : .13f);
                bool site = c.mark != null;
                if (site) sz = radius * .13f;
                Color col = site ? new Color(.45f, 1f, 1f) : c.enemy != 0 ? new Color(1f, .22f, .2f) : c.mothership ? new Color(.45f, .75f, 1f) : isNear ? new Color(1f, .92f, .55f) : new Color(1f, .7f, .28f);
                col.a = dist > Range ? .55f * pulse : isNear ? 1 : .85f;
                float a = bearing * Mathf.Deg2Rad;
                var at = new Vector2(Mathf.Sin(a), Mathf.Cos(a)) * r * radius;
                if (!OldStyle && Filter == 0 && !c.mothership && i != nearest) sz *= .75f;   // D2: smaller on All
                if (!OldStyle && !c.mothership && i != nearest)
                {
                    int k = KindOf(c), into = -1;
                    for (int j = 0; j < placed.Count; j++) if (placed[j].kind == k && (placed[j].at - at).magnitude < sz * .9f) { into = placed[j].i; break; }
                    if (into >= 0) { merged[into] = merged.TryGetValue(into, out var m) ? m + 1 : 2; b.enabled = false; halos[i].enabled = false; labels[i].enabled = false; continue; }
                    placed.Add((at, k, i)); merged[i] = 1;
                }
                b.color = col; Place(b, at, sz);
                halos[i].enabled = isNear; if (isNear) { halos[i].color = new Color(1f, 1f, .8f, .5f + .5f * pulse); Place(halos[i], at, sz * 2.1f); }
                string name = c.mothership ? "MS" : $"P{c.number}";
                labels[i].text = site ? $"{c.mark} {Distance(dist)}" : c.enemy != 0 ? (dist > Range ? "" : $"{(c.enemy == 1 ? "TURRET" : "SAUCER")} {Distance(dist)}") : (isNear ? $"<b>NEAREST {Distance(dist)}</b>" : $"{name} {Distance(dist)}") + (c.tag != null ? $" <color=#FF8A60><b>{c.tag}</b></color>" : "");
                labels[i].color = site ? new Color(.55f, 1f, 1f) : c.enemy != 0 ? new Color(1f, .45f, .4f) : c.mothership ? new Color(.6f, .85f, 1f) : isNear ? new Color(1f, 1f, .75f) : new Color(1f, .82f, .5f);
                labels[i].fontSize = isNear ? font + 1 : font;
                if (!OldStyle && Filter == 0 && !c.mothership && !isNear && !labelled.Contains(i)) labels[i].text = "";   // D2: the nearest two of a kind only
                if (!OldStyle && !c.mothership && !isNear && dist > Range) labels[i].text = "";   // D2: no labels on blips pinned at the rim
                // the label centred under the blip (over it in the scope's lower part), so it stays on the scope
                labels[i].alignment = TextAnchor.MiddleCenter;
                var o = at + new Vector2(0, at.y < -radius * .55f ? sz * .55f + font * .7f : -(sz * .55f + font * .7f));
                o.x = Mathf.Clamp(o.x, -radius * .55f, radius * .55f);   // kept over the scope
                labels[i].rectTransform.offsetMin = o + new Vector2(-110, -font); labels[i].rectTransform.offsetMax = o + new Vector2(110, font);
                // D2: a label that would overlap one already drawn is dropped (NEAREST and the mothership first: drawn in that order below)
                if (!OldStyle && labels[i].text != "")
                {
                    bool keep = isNear || c.mothership;
                    if (!keep) foreach (var q in labelAt) if (Mathf.Abs(q.x - o.x) < 95f && Mathf.Abs(q.y - o.y) < font * 1.15f) { labels[i].text = ""; break; }
                    if (labels[i].text != "") labelAt.Add(o);
                }
            }
            for (int i = contacts.Count; i < blips.Count; i++) { blips[i].enabled = false; halos[i].enabled = false; labels[i].enabled = false; }
            // D2: a merged blip shows how many it holds
            foreach (var kv in merged) if (kv.Value > 1 && kv.Key < labels.Count && labels[kv.Key].enabled) labels[kv.Key].text = (labels[kv.Key].text == "" ? "" : labels[kv.Key].text + " ") + $"<b>×{kv.Value}</b>";
            merged.Clear();
            // the route: a dot every ~1/24 of the range along it, out to the rim
            int nd = 0;
            if (route != null && route.Count > 1)
            {
                float spacing = Range / 24f, carry = 0;
                for (int k = 1; k < route.Count && nd < 60; k++)
                {
                    Vector3 p0 = route[k - 1], p1 = route[k]; float seg = Vector2.Distance(new Vector2(p0.x, p0.z), new Vector2(p1.x, p1.z));
                    for (float s = carry; s < seg && nd < 60; s += spacing)
                    {
                        var p = Vector3.Lerp(p0, p1, s / Mathf.Max(seg, .01f)) - ship.position; p.y = 0;
                        if (p.magnitude > Range) { k = route.Count; break; }
                        float br = (Mathf.Atan2(p.x, p.z) * Mathf.Rad2Deg - ship.eulerAngles.y) * Mathf.Deg2Rad;
                        if (dots.Count <= nd) dots.Add(Img(root, disc, new Color(.55f, 1f, .7f, .9f), Vector2.zero, 0));
                        dots[nd].enabled = true; Place(dots[nd], new Vector2(Mathf.Sin(br), Mathf.Cos(br)) * (p.magnitude / Range * radius), size * .03f); nd++;
                        carry = s + spacing - seg;
                    }
                }
            }
            for (int i = nd; i < dots.Count; i++) dots[i].enabled = false;
        }

        // The text list beside the scope: the nearest beacons, as exact as the distance allows.
        public static string List(Transform ship, List<RadarContact> contacts, int max)
        {
            var sorted = contacts.FindAll(c => c.enemy == 0 && Shows(c));   // Round 5: enemies are on the scope, not in this list; Round 11: the filter
            sorted.Sort((x, y) => (x.position - ship.position).sqrMagnitude.CompareTo((y.position - ship.position).sqrMagnitude));
            var sb = new StringBuilder();
            int shown = 0; bool first = true;
            foreach (var c in sorted)
            {
                if (shown++ >= max) break;
                Vector3 d = c.position - ship.position; d.y = 0; float dist = d.magnitude;
                float brg = (Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg + 360f) % 360f;
                string name = c.mark ?? (c.mothership ? "MOTHERSHIP" : $"PILOT {c.number}");
                string b = dist > ExactRange ? $"~{Mathf.Round(brg / 30f) * 30f % 360:000}°" : dist > NearRange ? $"{Mathf.Round(brg / 10f) * 10f % 360:000}°" : $"{brg:000}°";
                string line = $"{name}  {b}  {Distance(dist)}" + (c.tag != null ? $"  <color=#FF8A60>{c.tag}</color>" : "");
                if (!c.mothership && c.mark == null && first) { line = $"<b>{line}  ◄ NEAREST</b>"; first = false; }
                sb.AppendLine(line);
            }
            return sb.ToString();
        }
    }
}
