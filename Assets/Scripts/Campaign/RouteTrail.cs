using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace HostileHorizon
{
    // Fix 2.6, item 2: the route trail. A path to the current target that follows the canyons (A* over a 30 m grid where the cost
    // is the canyon "openness": canyon floors are cheap, rims and high ground dear), worked out off the main thread from the ship's
    // position, re-planned every few seconds while the ship drifts off it (over 60 m) and at once when it strays (150 m) or the target changes. Shown in the world as glowing chevrons every
    // 100 m at a flyable height (the floor plus 35 m, kept clear of the ground on either side), the nearest few hundred metres bright
    // and fading out ahead (VR rule 4: in the 3D world, seen from the cockpit), and as a dotted line on the radar. For the
    // mothership the trail climbs to the docking height toward it. The last chevron sits over the pilot's pad (the green ring there).
    public sealed class RouteTrail : MonoBehaviour
    {
        public const float GridCell = 30f, Clearance = 35f, MarkerSpacing = 100f, ShowAhead = 700f, BrightAhead = 300f, StrayDistance = 150f, ReplanSeconds = 4f;
        public const int Markers = 8;
        public List<Vector3> Path { get; private set; } = new List<Vector3>();   // world points at flying height, ship first
        public Vector3 Goal { get; private set; }
        public bool ToMothership { get; private set; }
        public int Plans { get; private set; }                // re-plans so far (checks)
        public float LastPlanMs { get; private set; }
        public string LastReplanReason { get; private set; } = "";
        public float OffRoute { get; private set; }           // m from the ship to the path
        CanyonField field; Task<List<Vector3>> job; float nextPlan, lastPlan = -99; const float MinReplan = 1.5f; Vector3 jobGoal; bool jobMother;
        readonly List<Renderer> marks = new List<Renderer>(); readonly List<Material> mats = new List<Material>();
        static Mesh chevron;
        public readonly List<Vector3> Shown = new List<Vector3>();   // where the visible chevrons are (checks)

        public static RouteTrail Create()
        {
            var t = new GameObject("Route trail").AddComponent<RouteTrail>();
            var sh = Shader.Find("HostileHorizon/GlowAdd");
            for (int i = 0; i < Markers; i++)
            {
                var go = new GameObject("Route chevron"); go.transform.SetParent(t.transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = Chevron();
                var r = go.AddComponent<MeshRenderer>(); var m = new Material(sh); r.sharedMaterial = m;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
                t.marks.Add(r); t.mats.Add(m); go.SetActive(false);
            }
            return t;
        }

        // A flat "^" chevron 20 m wide pointing along +z, with a thin frame round it so it reads as a gate from far away.
        static Mesh Chevron()
        {
            if (chevron) return chevron;
            var v = new List<Vector3>(); var tri = new List<int>();
            void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d) { int n = v.Count; v.Add(a); v.Add(b); v.Add(c); v.Add(d); tri.AddRange(new[] { n, n + 1, n + 2, n, n + 2, n + 3 }); }
            void Bar(Vector3 a, Vector3 b, float w) { var s = Vector3.Cross(b - a, Vector3.forward).normalized * w * .5f; Quad(a - s, a + s, b + s, b - s); }
            // the arrow (in the x-y plane, facing the pilot coming along +z), pointing up the way to go is ambiguous: a "^" over the path
            Bar(new Vector3(-10, -4f, 0), new Vector3(0, 4f, 0), 2.2f); Bar(new Vector3(0, 4f, 0), new Vector3(10, -4f, 0), 2.2f);
            // a frame on the ground side: two short posts, so the gate shows its height
            Bar(new Vector3(-10, -4f, 0), new Vector3(-10, -8f, 0), .8f); Bar(new Vector3(10, -4f, 0), new Vector3(10, -8f, 0), .8f);
            chevron = new Mesh { name = "Route chevron" }; chevron.SetVertices(v); chevron.SetTriangles(tri, 0); chevron.RecalculateBounds();
            return chevron;
        }

        public void Clear() { Path.Clear(); job = null; nextPlan = 0; foreach (var m in marks) m.gameObject.SetActive(false); Shown.Clear(); OffRoute = 0; }

        // Called every frame while flying (on = the setting and a target to go to).
        public void Tick(bool on, CanyonField f, Transform ship, Vector3 goal, bool toMother, float dockingHeight)
        {
            if (!on || f == null) { Clear(); return; }
            if (f != field) { field = f; Clear(); }
            Vector3 pos = ship.position;
            // a finished job: take its path
            if (job != null && job.IsCompleted)
            {
                if (job.Status == TaskStatus.RanToCompletion && job.Result != null && job.Result.Count > 1) { Path = job.Result; Goal = jobGoal; ToMothership = jobMother; }
                job = null;
            }
            OffRoute = Path.Count > 1 ? DistanceToPath(pos, out _, out _) : 0;
            bool goalMoved = (Flat(goal - Goal)).magnitude > 60f || toMother != ToMothership;
            string why = Path.Count < 2 ? "first plan" : goalMoved ? "new target" : OffRoute > StrayDistance && Time.time > lastPlan + MinReplan ? $"left the route ({OffRoute:0} m off)" : Time.time > nextPlan && OffRoute > 60f ? "drifting off it" : "";   // on the line: no need to plan again
            if (why != "" && job == null)
            {
                Plans++; LastReplanReason = why; nextPlan = Time.time + ReplanSeconds; lastPlan = Time.time;
                jobGoal = goal; jobMother = toMother;
                if (toMother) { Path = Climb(pos, goal, dockingHeight); Goal = goal; ToMothership = true; }
                else { var fl = field; Vector3 from = pos, to = goal; var sw = System.Diagnostics.Stopwatch.StartNew(); job = Task.Run(() => { var r = Plan(fl, from, to); LastPlanMs = (float)sw.Elapsed.TotalMilliseconds; return r; }); }
            }
            Place(pos);
        }

        static Vector3 Flat(Vector3 v) { v.y = 0; return v; }

        // The level distance from a point to the path, and where along it (metres from its start) the nearest point is.
        public float DistanceToPath(Vector3 p, out float along, out int seg)
        {
            float best = float.MaxValue, acc = 0; along = 0; seg = 0;
            for (int i = 1; i < Path.Count; i++)
            {
                Vector3 a = Flat(Path[i - 1]), b = Flat(Path[i]), q = Flat(p); float len = (b - a).magnitude;
                float t = len > .01f ? Mathf.Clamp01(Vector3.Dot(q - a, b - a) / (len * len)) : 0;
                float d = (a + (b - a) * t - q).magnitude;
                if (d < best) { best = d; along = acc + t * len; seg = i - 1; }
                acc += len;
            }
            return best;
        }

        // The chevrons: every 100 m along the path past the ship, bright for the first 300 m, fading out by 700 m.
        void Place(Vector3 pos)
        {
            Shown.Clear();
            if (Path.Count < 2) { foreach (var m in marks) m.gameObject.SetActive(false); return; }
            DistanceToPath(pos, out float along, out _);
            float first = (Mathf.Floor(along / MarkerSpacing) + 1) * MarkerSpacing;
            if (first - along < 40f) first += MarkerSpacing;   // not right on top of the ship
            int k = 0; float acc = 0; int i = 1;
            for (float s = first; k < Markers && s <= along + ShowAhead; s += MarkerSpacing)
            {
                while (i < Path.Count && acc + Vector3.Distance(Flat(Path[i - 1]), Flat(Path[i])) < s) { acc += Vector3.Distance(Flat(Path[i - 1]), Flat(Path[i])); i++; }
                bool end = i >= Path.Count; if (end && k > 0 && Shown.Count > 0 && Vector3.Distance(Shown[Shown.Count - 1], Path[Path.Count - 1]) < 30f) break;
                Vector3 p, dir;
                if (end) { p = Path[Path.Count - 1]; dir = Flat(Path[Path.Count - 1] - Path[Path.Count - 2]); }
                else { float len = Vector3.Distance(Flat(Path[i - 1]), Flat(Path[i])); p = Vector3.Lerp(Path[i - 1], Path[i], (s - acc) / Mathf.Max(len, .01f)); dir = Path[i] - Path[i - 1]; }
                if (dir.sqrMagnitude < .01f) dir = Vector3.forward;
                var r = marks[k]; r.gameObject.SetActive(true);
                r.transform.SetPositionAndRotation(p, Quaternion.LookRotation(Flat(dir).normalized, Vector3.up));
                float ahead = s - along, fade = ahead < BrightAhead ? 1 : Mathf.Clamp01(1 - (ahead - BrightAhead) / (ShowAhead - BrightAhead));
                mats[k].SetColor("_Color", (end ? new Color(.45f, 1f, .55f) : new Color(.35f, .95f, 1f)) * (1.3f * fade));
                Shown.Add(p); k++;
                if (end) break;
            }
            for (; k < Markers; k++) marks[k].gameObject.SetActive(false);
        }

        // For the mothership: straight toward it, climbing at about 20 degrees to the docking height (and kept 60 m over the ground).
        List<Vector3> Climb(Vector3 from, Vector3 mother, float dockingHeight)
        {
            var pts = new List<Vector3>(); Vector3 to = Flat(mother - from); float dist = to.magnitude; Vector3 dir = dist > 1 ? to / dist : Vector3.forward;
            float rise = Mathf.Max(0, dockingHeight + 40f - from.y), run = Mathf.Max(200f, rise / Mathf.Tan(20f * Mathf.Deg2Rad));
            for (float s = 0; s <= Mathf.Max(run, 400f) + 1; s += 50f)
            {
                var q = from + dir * s; q.y = Mathf.Lerp(from.y, dockingHeight + 40f, Mathf.Clamp01(s / run));
                q.y = Mathf.Max(q.y, field.SurfaceHeight(q.x, q.z) + 60f);
                pts.Add(q);
            }
            return pts;
        }

        // ---- the path-finder (thread-safe: CanyonField only)

        public static List<Vector3> Plan(CanyonField f, Vector3 from, Vector3 to)
        {
            const float pad = 900f; float c = GridCell;
            float x0 = Mathf.Min(from.x, to.x) - pad, z0 = Mathf.Min(from.z, to.z) - pad;
            int nx = Mathf.CeilToInt((Mathf.Abs(from.x - to.x) + 2 * pad) / c) + 1, nz = Mathf.CeilToInt((Mathf.Abs(from.z - to.z) + 2 * pad) / c) + 1;
            var cost = new float[nx * nz]; for (int i = 0; i < cost.Length; i++) cost[i] = -1;
            float Cost(int i, int j)
            {
                int k = j * nx + i; if (cost[k] >= 0) return cost[k];
                float t = f.CanyonT(x0 + i * c, z0 + j * c);
                return cost[k] = 1f + 80f * t * t;   // a canyon floor costs 1 a cell; a rim 81 (a long way round is cheaper than over the top)
            }
            int si = Mathf.Clamp(Mathf.RoundToInt((from.x - x0) / c), 0, nx - 1), sj = Mathf.Clamp(Mathf.RoundToInt((from.z - z0) / c), 0, nz - 1);
            int gi = Mathf.Clamp(Mathf.RoundToInt((to.x - x0) / c), 0, nx - 1), gj = Mathf.Clamp(Mathf.RoundToInt((to.z - z0) / c), 0, nz - 1);
            var g = new float[nx * nz]; var came = new int[nx * nz]; var closed = new bool[nx * nz];
            for (int i = 0; i < g.Length; i++) { g[i] = float.MaxValue; came[i] = -1; }
            var open = new MinHeap(); int s0 = sj * nx + si, goal = gj * nx + gi;
            g[s0] = 0; open.Push(s0, H(si, sj, gi, gj));
            int[] di = { 1, -1, 0, 0, 1, 1, -1, -1 }, dj = { 0, 0, 1, -1, 1, -1, 1, -1 };
            int expanded = 0;
            while (open.Count > 0 && expanded < 400000)
            {
                int cur = open.Pop(); if (closed[cur]) continue; closed[cur] = true; expanded++;
                if (cur == goal) break;
                int ci = cur % nx, cj = cur / nx;
                for (int d = 0; d < 8; d++)
                {
                    int ni = ci + di[d], nj = cj + dj[d]; if (ni < 0 || nj < 0 || ni >= nx || nj >= nz) continue;
                    int n = nj * nx + ni; if (closed[n]) continue;
                    float step = (d < 4 ? 1f : 1.4142f) * .5f * (Cost(ci, cj) + Cost(ni, nj));
                    float ng = g[cur] + step;
                    if (ng < g[n]) { g[n] = ng; came[n] = cur; open.Push(n, ng + H(ni, nj, gi, gj)); }
                }
            }
            if (came[goal] < 0 && goal != s0) return null;
            var cells = new List<Vector2>();
            for (int k = goal; k >= 0; k = came[k]) { cells.Add(new Vector2(x0 + (k % nx) * c, z0 + (k / nx) * c)); if (k == s0) break; }
            cells.Reverse();
            cells[0] = new Vector2(from.x, from.z); cells[cells.Count - 1] = new Vector2(to.x, to.z);
            // shortcut: skip cells while the straight line stays as low (canyon-wise) as the cells it replaces
            var pts = new List<Vector2> { cells[0] };
            for (int i = 0; i < cells.Count - 1;)
            {
                int best = i + 1; float worst = 0;
                for (int j = i + 1; j < cells.Count && j <= i + 25; j++)
                {
                    worst = Mathf.Max(worst, f.CanyonT(cells[j].x, cells[j].y));
                    if (LineMax(f, cells[i], cells[j]) <= worst + .03f) best = j;
                }
                pts.Add(cells[best]); i = best;
            }
            // every 25 m along the line a flyable height (below); then a glide down from the ship's height (at most 25 degrees) and a climb that starts before a
            // ridge (at most 30 degrees), so the gates can be followed at speed
            var flat = new List<Vector2> { pts[0] };
            for (int i = 1; i < pts.Count; i++) { var a = pts[i - 1]; var b = pts[i]; int n = Mathf.Max(1, Mathf.CeilToInt((b - a).magnitude / 25f)); for (int k = 1; k <= n; k++) flat.Add(Vector2.Lerp(a, b, k / (float)n)); }
            var path = new List<Vector3>(); var hs = new float[flat.Count]; var at = new float[flat.Count];
            for (int i = 0; i < flat.Count; i++)
            {
                var dir = (i + 1 < flat.Count ? flat[i + 1] - flat[i] : flat[i] - flat[Mathf.Max(0, i - 1)]); if (dir.sqrMagnitude < .01f) dir = Vector2.up; dir.Normalize();
                var side = new Vector2(-dir.y, dir.x); float h = float.MinValue;
                // 35 m over the floor (and the floor 15 m ahead / behind); raised only where the canyon is narrower than about 70 m at
                // that height (the ground up to 35 m to either side must stay 12 m below the line: room to wander and for the wings)
                for (int u = -1; u <= 1; u++) { var q = flat[i] + dir * (u * 15f); h = Mathf.Max(h, f.SurfaceHeight(q.x, q.y) + Clearance); }
                for (int w = -2; w <= 2; w++) { if (w == 0) continue; var q = flat[i] + side * (w * 17.5f); h = Mathf.Max(h, f.SurfaceHeight(q.x, q.y) + 12f); }
                hs[i] = h; if (i > 0) at[i] = at[i - 1] + (flat[i] - flat[i - 1]).magnitude;
            }
            for (int i = 0; i < flat.Count; i++) hs[i] = Mathf.Max(hs[i], from.y - at[i] * Mathf.Tan(25f * Mathf.Deg2Rad));       // the glide down
            for (int i = flat.Count - 2; i >= 0; i--) hs[i] = Mathf.Max(hs[i], hs[i + 1] - (at[i + 1] - at[i]) * Mathf.Tan(30f * Mathf.Deg2Rad));   // climb early
            hs[flat.Count - 1] = Mathf.Max(f.SurfaceHeight(to.x, to.z) + 25f, hs[flat.Count - 1] - 10f);   // over the pad
            for (int i = 0; i < flat.Count; i++) path.Add(new Vector3(flat[i].x, hs[i], flat[i].y));
            return path;
        }

        static float H(int i, int j, int gi, int gj) { int dx = Mathf.Abs(i - gi), dz = Mathf.Abs(j - gj); return (Mathf.Max(dx, dz) + .4142f * Mathf.Min(dx, dz)); }

        static float LineMax(CanyonField f, Vector2 a, Vector2 b)
        {
            float m = 0; int n = Mathf.Max(1, Mathf.CeilToInt((b - a).magnitude / 15f));
            for (int k = 0; k <= n; k++) { var p = Vector2.Lerp(a, b, k / (float)n); m = Mathf.Max(m, f.CanyonT(p.x, p.y)); }
            return m;
        }

        static Vector3 Height(CanyonField f, Vector2 p, Vector2 dir)
        {
            var side = new Vector2(-dir.y, dir.x).normalized; float h = f.SurfaceHeight(p.x, p.y);
            for (int s = -1; s <= 1; s += 2) h = Mathf.Max(h, f.SurfaceHeight(p.x + side.x * 25f * s, p.y + side.y * 25f * s) - 15f);
            return new Vector3(p.x, h + Clearance, p.y);
        }

        sealed class MinHeap
        {
            readonly List<(int id, float k)> a = new List<(int, float)>();
            public int Count => a.Count;
            public void Push(int id, float k) { a.Add((id, k)); int i = a.Count - 1; while (i > 0) { int p = (i - 1) / 2; if (a[p].k <= a[i].k) break; (a[p], a[i]) = (a[i], a[p]); i = p; } }
            public int Pop()
            {
                var top = a[0].id; a[0] = a[a.Count - 1]; a.RemoveAt(a.Count - 1);
                int i = 0; while (true) { int l = 2 * i + 1, r = l + 1, m = i; if (l < a.Count && a[l].k < a[m].k) m = l; if (r < a.Count && a[r].k < a[m].k) m = r; if (m == i) break; (a[m], a[i]) = (a[i], a[m]); i = m; }
                return top;
            }
        }
    }
}
