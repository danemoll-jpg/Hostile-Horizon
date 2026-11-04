using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace HostileHorizon
{
    // Round 4 (A2-A4, A6): the biome's props around the viewer, from CanyonField (seed only): the Giant Forest's trees, the Dead
    // City's sky bridges, the Ice's snow bridges, and shafts of light into the forest clearings.
    // Performance: trees are GPU-instanced (Graphics.RenderMeshInstanced, Resources/Materials/Prop.mat, instancing on) in two levels
    // of detail: the near ones (NearRange) with the detailed trunk and crown and shadows, the rest a low mesh without shadows, out to
    // the terrain's view distance (shorter on the Steam Deck setting). Prop cells (256 m) are worked out on worker threads; the draw
    // lists are rebuilt when the viewer crosses a cell. Bridges are one mesh per cell in the rock material (the biome's look).
    // Collisions don't use these objects: CanyonField.SolidPush answers from the seed, so the Deck's thinner drawing never changes play.
    public sealed class BiomeProps : MonoBehaviour
    {
        public CanyonField Field { get; private set; }
        public float NearRange = 650f;
        public Material RockMaterial;
        public int TreesDrawn { get; private set; }
        public int NearTrees { get; private set; }
        Material treeBark, treeLeaf, shaft;
        Mesh trunkHi, trunkLo, crownHi, crownLo, shaftMesh;
        Vector3 focus; float view = 3000f; Vector2Int lastCell = new Vector2Int(int.MinValue, 0); bool dirty;

        sealed class Cell { public List<Matrix4x4> trunks = new List<Matrix4x4>(), crowns = new List<Matrix4x4>(); public List<CanyonField.Box> boxes = new List<CanyonField.Box>(); public List<Vector3> shafts = new List<Vector3>(); public GameObject go; }
        readonly Dictionary<Vector2Int, Cell> cells = new Dictionary<Vector2Int, Cell>();
        readonly Dictionary<Vector2Int, Task<Cell>> jobs = new Dictionary<Vector2Int, Task<Cell>>();
        readonly List<Matrix4x4> nearTrunks = new List<Matrix4x4>(), nearCrowns = new List<Matrix4x4>(), farTrunks = new List<Matrix4x4>(), farCrowns = new List<Matrix4x4>();
        readonly List<Vector2Int> scratch = new List<Vector2Int>();

        public static BiomeProps Create(Material rock)
        {
            var p = new GameObject("Biome props").AddComponent<BiomeProps>(); p.RockMaterial = rock;
            var baseMat = Resources.Load<Material>("Materials/Prop");
            if (!baseMat) baseMat = new Material(Shader.Find("HostileHorizon/Prop"));
            p.treeBark = new Material(baseMat) { name = "Bark", enableInstancing = true };
            p.treeLeaf = new Material(baseMat) { name = "Leaves", enableInstancing = true };
            p.treeLeaf.SetFloat("_Translucent", 1f);
            p.treeBark.SetFloat("_Glow", 1f); p.treeBark.SetColor("_Vary", new Color(.12f, .1f, .08f));
            p.treeLeaf.SetColor("_Vary", new Color(.35f, .25f, .3f));
            p.shaft = new Material(Shader.Find("HostileHorizon/Shaft")); p.shaft.SetColor("_Color", new Color(.07f, .065f, .035f));
            p.trunkHi = Trunk(12, true); p.trunkLo = Trunk(6, false); p.crownHi = Crown(2, 3); p.crownLo = Crown(1, 0); p.shaftMesh = Shaft();
            return p;
        }

        public void SetDetail(Texture detail) { treeBark.SetTexture("_Detail", detail); treeLeaf.SetTexture("_Detail", detail); }

        public void SetField(CanyonField f)
        {
            foreach (var c in cells.Values) if (c.go) Destroy(c.go);
            cells.Clear(); jobs.Clear(); Field = f; lastCell = new Vector2Int(int.MinValue, 0); dirty = true;
            nearTrunks.Clear(); nearCrowns.Clear(); farTrunks.Clear(); farCrowns.Clear(); TreesDrawn = 0;
        }

        public void SetFocus(Vector3 p, float viewDistance) { focus = p; view = viewDistance; }

        // look per biome (WorldLook.ApplyBiome)
        public void SetColours(Color bark, Color leaf, Color sky, Color ground)
        {
            treeBark.SetColor("_Tint", bark); treeLeaf.SetColor("_Tint", leaf);
            foreach (var m in new[] { treeBark, treeLeaf }) { m.SetColor("_AmbientSky", sky); m.SetColor("_AmbientGround", ground); }
        }

        void Update()
        {
            if (Field == null || Field.Biome == Biome.Canyons) return;
            int r = Mathf.CeilToInt(view / CanyonField.PropCell);
            var fc = new Vector2Int(Mathf.FloorToInt(focus.x / CanyonField.PropCell), Mathf.FloorToInt(focus.z / CanyonField.PropCell));
            // finished jobs
            scratch.Clear();
            foreach (var kv in jobs) if (kv.Value.IsCompleted) scratch.Add(kv.Key);
            foreach (var k in scratch)
            {
                var t = jobs[k]; jobs.Remove(k);
                if (t.IsFaulted) { Debug.LogException(t.Exception); continue; }
                var c = t.Result; BuildObjects(k, c); cells[k] = c; dirty = true;
            }
            // new cells, nearest first (a few jobs at a time)
            if (fc != lastCell || jobs.Count == 0)
            {
                for (int ring = 0; ring <= r && jobs.Count < 8; ring++)
                    for (int dz = -ring; dz <= ring && jobs.Count < 8; dz++)
                        for (int dx = -ring; dx <= ring && jobs.Count < 8; dx++)
                        {
                            if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dz)) != ring) continue;
                            var k = new Vector2Int(fc.x + dx, fc.y + dz);
                            if (cells.ContainsKey(k) || jobs.ContainsKey(k) || Dist(k) > view) continue;
                            var f = Field; jobs[k] = Task.Run(() => Make(f, k));
                        }
                // drop far cells
                scratch.Clear();
                foreach (var kv in cells) if (Dist(kv.Key) > view + CanyonField.PropCell) scratch.Add(kv.Key);
                foreach (var k in scratch) { if (cells[k].go) Destroy(cells[k].go); cells.Remove(k); dirty = true; }
            }
            if (fc != lastCell) { lastCell = fc; dirty = true; }
            if (dirty) Rebuild();
            Draw();
        }

        float Dist(Vector2Int k)
        {
            float cx = (k.x + .5f) * CanyonField.PropCell, cz = (k.y + .5f) * CanyonField.PropCell;
            float dx = Mathf.Max(0, Mathf.Abs(focus.x - cx) - CanyonField.PropCell / 2), dz = Mathf.Max(0, Mathf.Abs(focus.z - cz) - CanyonField.PropCell / 2);
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        static Cell Make(CanyonField f, Vector2Int k)
        {
            var c = new Cell(); int per = (int)(CanyonField.PropCell / CanyonField.TreeCell);
            if (f.Biome == Biome.Forest)
            {
                for (int j = 0; j < per; j++)
                    for (int i = 0; i < per; i++)
                    {
                        var t = f.TreeAt(k.x * per + i, k.y * per + j); if (!t.ok) continue;
                        c.trunks.Add(Matrix4x4.TRS(t.basePos, Quaternion.Euler(0, t.yaw, 0), new Vector3(t.radius, t.height, t.radius)));
                        c.crowns.Add(Matrix4x4.TRS(t.crown, Quaternion.Euler(0, t.yaw, 0), t.crownSize));
                    }
                // shafts of light into the clearings
                var pad = f.PadAt(Mathf.FloorToInt((k.x + .5f) * CanyonField.PropCell / CanyonField.PadCell), Mathf.FloorToInt((k.y + .5f) * CanyonField.PropCell / CanyonField.PadCell));
                if (pad.ok && Mathf.FloorToInt(pad.x / CanyonField.PropCell) == k.x && Mathf.FloorToInt(pad.z / CanyonField.PropCell) == k.y) c.shafts.Add(pad.Position);
            }
            c.boxes.AddRange(f.BoxesAt(k.x, k.y));
            return c;
        }

        void BuildObjects(Vector2Int k, Cell c)
        {
            if (c.boxes.Count == 0 && c.shafts.Count == 0) return;
            c.go = new GameObject($"Props {k.x},{k.y}"); c.go.transform.SetParent(transform, false);
            foreach (var b in c.boxes)
            {
                var g = new GameObject(b.kind == 0 ? "Sky bridge" : "Snow bridge"); g.transform.SetParent(c.go.transform, false);
                g.transform.SetPositionAndRotation(b.c, Quaternion.Euler(0, b.yaw, 0));
                g.AddComponent<MeshFilter>().sharedMesh = BoxMesh(b.half, b.kind == 1, Mathf.RoundToInt(b.c.x * 3 + b.c.z));
                var mr = g.AddComponent<MeshRenderer>(); mr.sharedMaterial = RockMaterial; mr.shadowCastingMode = ShadowCastingMode.On;
            }
            foreach (var s in c.shafts)
            {
                var sun = WorldLook.Sun ? -WorldLook.Sun.transform.forward : Vector3.up;
                for (int i = 0; i < 3; i++)
                {
                    var g = new GameObject("Light shaft"); g.transform.SetParent(c.go.transform, false);
                    var at = s + new Vector3((i - 1) * 16f, -2f, (i % 2) * 14f - 7f);
                    g.transform.SetPositionAndRotation(at, Quaternion.FromToRotation(Vector3.up, Vector3.Slerp(Vector3.up, sun, .5f)));
                    g.transform.localScale = new Vector3(5f + i * 2.5f, 130f, 5f + i * 2.5f);
                    g.AddComponent<MeshFilter>().sharedMesh = shaftMesh;
                    var mr = g.AddComponent<MeshRenderer>(); mr.sharedMaterial = shaft; mr.shadowCastingMode = ShadowCastingMode.Off;
                }
            }
        }

        void Rebuild()
        {
            dirty = false;
            nearTrunks.Clear(); nearCrowns.Clear(); farTrunks.Clear(); farCrowns.Clear();
            float near2 = NearRange * NearRange;
            foreach (var kv in cells)
            {
                bool nearCell = Dist(kv.Key) < NearRange + CanyonField.PropCell;
                var c = kv.Value;
                for (int i = 0; i < c.trunks.Count; i++)
                {
                    var p = c.trunks[i].GetColumn(3);
                    float dx = p.x - focus.x, dz = p.z - focus.z;
                    if (nearCell && dx * dx + dz * dz < near2) { nearTrunks.Add(c.trunks[i]); nearCrowns.Add(c.crowns[i]); }
                    else { farTrunks.Add(c.trunks[i]); farCrowns.Add(c.crowns[i]); }
                }
            }
            NearTrees = nearTrunks.Count; TreesDrawn = nearTrunks.Count + farTrunks.Count;
        }

        void Draw()
        {
            if (TreesDrawn == 0) return;
            var bounds = new Bounds(focus, Vector3.one * (view * 2 + 400));
            DrawList(trunkHi, treeBark, nearTrunks, true, bounds); DrawList(crownHi, treeLeaf, nearCrowns, true, bounds);
            DrawList(trunkLo, treeBark, farTrunks, false, bounds); DrawList(crownLo, treeLeaf, farCrowns, false, bounds);
        }

        static void DrawList(Mesh m, Material mat, List<Matrix4x4> list, bool shadows, Bounds b)
        {
            if (list.Count == 0) return;
            var rp = new RenderParams(mat) { shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off, receiveShadows = true, worldBounds = b };
            for (int s = 0; s < list.Count; s += 1000) Graphics.RenderMeshInstanced(rp, m, 0, list, Mathf.Min(1000, list.Count - s), s);
        }

        // ---- meshes (unit sized: the instance scale gives each tree its size)

        // A trunk of radius 1 and height 1 (base at 0), the flare and taper of CanyonField.TrunkRadius, buttress ribs near the roots.
        static Mesh Trunk(int sides, bool ribs)
        {
            float[] us = ribs ? new[] { 0f, .02f, .05f, .09f, .15f, .25f, .4f, .6f, .8f, .92f, 1f } : new[] { 0f, .06f, .2f, .6f, 1f };
            var v = new List<Vector3>(); var col = new List<Color>(); var tri = new List<int>();
            for (int r = 0; r < us.Length; r++)
            {
                float u = us[r];
                for (int s = 0; s <= sides; s++)
                {
                    float a = s * Mathf.PI * 2 / sides;
                    float rad = 1f + .7f * Mathf.Exp(-u * 14f) - .25f * u;
                    if (ribs) rad *= 1f + .28f * Mathf.Exp(-u * 9f) * Mathf.Max(0, Mathf.Cos(a * 5f + 1.3f));
                    float bend = .25f * u * u;   // a slight lean (in radii; the trunk is several metres wide)
                    v.Add(new Vector3(Mathf.Cos(a) * rad + bend, u, Mathf.Sin(a) * rad));
                    float dark = Mathf.Lerp(.55f, 1f, Mathf.Clamp01(u * 4f));
                    col.Add(new Color(.30f * dark, .25f * dark, .20f * dark, Mathf.Lerp(.3f, 1f, Mathf.Clamp01(u * 3f))));
                }
            }
            int w = sides + 1;
            for (int r = 0; r < us.Length - 1; r++)
                for (int s = 0; s < sides; s++) { int a = r * w + s, b = a + 1, c = a + w, d = c + 1; tri.Add(a); tri.Add(c); tri.Add(b); tri.Add(b); tri.Add(c); tri.Add(d); }
            var m = new Mesh { name = "Trunk" }; m.SetVertices(v); m.SetColors(col); m.SetTriangles(tri, 0); m.RecalculateNormals(); m.RecalculateBounds(); return m;
        }

        // A crown of radius 1: a lumpy blob (an icosphere pushed out by noise, flattened underneath) and a few smaller lobes.
        static Mesh Crown(int subdiv, int lobes)
        {
            var v = new List<Vector3>(); var col = new List<Color>(); var tri = new List<int>();
            var noise = new Noise(77); var rng = new System.Random(5);
            void Blob(Vector3 c, float scale, int sd)
            {
                Ico(sd, out var bv, out var bt); int o = v.Count;
                foreach (var p0 in bv)
                {
                    var p = p0;
                    float n = noise.Fbm(p.x * 2.1f + c.x * 3 + 5, p.z * 2.1f + p.y * 1.7f + c.z * 3, 3);
                    p *= 1f + .22f * n;
                    if (p.y < -.6f) p.y = Mathf.Lerp(p.y, -.6f, .5f);
                    var q = c + p * scale; v.Add(q);
                    float top = Mathf.Clamp01(q.y * .5f + .5f), gold = Mathf.Clamp01(.5f + n * 1.4f);
                    col.Add(new Color(Mathf.Lerp(.16f, .42f, gold) * Mathf.Lerp(.7f, 1.15f, top), Mathf.Lerp(.30f, .40f, gold) * Mathf.Lerp(.7f, 1.15f, top), Mathf.Lerp(.10f, .12f, gold), Mathf.Lerp(.25f, 1f, top)));
                }
                foreach (var t in bt) tri.Add(o + t);
            }
            Blob(Vector3.zero, .92f, subdiv);
            for (int i = 0; i < lobes; i++)
            {
                float a = (float)rng.NextDouble() * Mathf.PI * 2;
                Blob(new Vector3(Mathf.Cos(a) * .5f, -.25f + (float)rng.NextDouble() * .5f, Mathf.Sin(a) * .5f), .55f, Mathf.Max(1, subdiv - 1));
            }
            var m = new Mesh { name = "Crown" }; m.SetVertices(v); m.SetColors(col); m.SetTriangles(tri, 0); m.RecalculateNormals(); m.RecalculateBounds(); return m;
        }

        static void Ico(int sd, out List<Vector3> verts, out List<int> tris)
        {
            float t = (1f + Mathf.Sqrt(5f)) / 2f;
            verts = new List<Vector3> { new Vector3(-1, t, 0), new Vector3(1, t, 0), new Vector3(-1, -t, 0), new Vector3(1, -t, 0), new Vector3(0, -1, t), new Vector3(0, 1, t), new Vector3(0, -1, -t), new Vector3(0, 1, -t), new Vector3(t, 0, -1), new Vector3(t, 0, 1), new Vector3(-t, 0, -1), new Vector3(-t, 0, 1) };
            for (int i = 0; i < verts.Count; i++) verts[i] = verts[i].normalized;
            tris = new List<int> { 0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11, 1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8, 3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9, 4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1 };
            for (int s = 0; s < sd; s++)
            {
                var mid = new Dictionary<long, int>(); var nt = new List<int>(); var vv = verts;
                int Mid(int a, int b) { long k = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a; if (mid.TryGetValue(k, out int i)) return i; vv.Add(((vv[a] + vv[b]) * .5f).normalized); mid[k] = vv.Count - 1; return vv.Count - 1; }
                for (int i = 0; i < tris.Count; i += 3)
                {
                    int a = tris[i], b = tris[i + 1], c = tris[i + 2], ab = Mid(a, b), bc = Mid(b, c), ca = Mid(c, a);
                    nt.AddRange(new[] { a, ab, ca, b, bc, ab, c, ca, bc, ab, bc, ca });
                }
                tris = nt;
            }
        }

        // a soft beam: an open cone, wide at the top, drawn additively
        static Mesh Shaft()
        {
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var nr = new List<Vector3>(); var tri = new List<int>(); int n = 12, rings = 6;
            for (int r = 0; r <= rings; r++)
                for (int i = 0; i <= n; i++)
                {
                    float a = i * Mathf.PI * 2 / n, y = r / (float)rings, rad = Mathf.Lerp(.7f, 1.5f, y);
                    v.Add(new Vector3(Mathf.Cos(a) * rad, y, Mathf.Sin(a) * rad)); nr.Add(new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a))); uv.Add(new Vector2(i / (float)n, y));
                }
            for (int r = 0; r < rings; r++) for (int i = 0; i < n; i++) { int a = r * (n + 1) + i, b = a + n + 1; tri.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 }); }
            var m = new Mesh { name = "Shaft" }; m.SetVertices(v); m.SetNormals(nr); m.SetUVs(0, uv); m.SetTriangles(tri, 0); m.RecalculateBounds(); return m;
        }

        // A box (the collision box's size); a snow bridge's underside is bumpy (pushed up into the box only, so it never hangs below
        // what collides) and its top a little uneven.
        static Mesh BoxMesh(Vector3 h, bool snow, int seed)
        {
            int seg = snow ? 10 : 1; var v = new List<Vector3>(); var tri = new List<int>(); var nz = new Noise(seed);
            void Face(Vector3 o, Vector3 u, Vector3 w, int which)
            {
                int start = v.Count;
                for (int j = 0; j <= seg; j++)
                    for (int i = 0; i <= seg; i++)
                    {
                        var p = o + u * (i / (float)seg) + w * (j / (float)seg);
                        if (snow)
                        {
                            float n = nz.Fbm(p.x / 18f + 3, p.z / 18f - 2, 3);
                            bool edgeX = i == 0 || i == seg, edgeZ = j == 0 || j == seg;
                            if (which == 1 && !(edgeX || edgeZ)) p.y += Mathf.Abs(n) * 14f + 2f;   // underside: icicle hollows
                            if (which == 0 && !(edgeX || edgeZ)) p.y += n * 1.5f;               // top: drifts
                        }
                        v.Add(p);
                    }
                for (int j = 0; j < seg; j++)
                    for (int i = 0; i < seg; i++) { int a = start + j * (seg + 1) + i, b = a + 1, c = a + seg + 1, d = c + 1; tri.Add(a); tri.Add(b); tri.Add(c); tri.Add(b); tri.Add(d); tri.Add(c); }
            }
            Vector3 X = new Vector3(2 * h.x, 0, 0), Y = new Vector3(0, 2 * h.y, 0), Z = new Vector3(0, 0, 2 * h.z);
            Face(new Vector3(-h.x, h.y, -h.z), Z, X, 0);    // top (up)
            Face(new Vector3(-h.x, -h.y, -h.z), X, Z, 1);   // bottom (down)
            Face(new Vector3(-h.x, -h.y, -h.z), Y, X, 2);   // -z
            Face(new Vector3(-h.x, -h.y, h.z), X, Y, 2);    // +z
            Face(new Vector3(-h.x, -h.y, -h.z), Z, Y, 2);   // -x
            Face(new Vector3(h.x, -h.y, -h.z), Y, Z, 2);    // +x
            var m = new Mesh { name = snow ? "Snow bridge" : "Sky bridge", indexFormat = IndexFormat.UInt32 }; m.SetVertices(v); m.SetTriangles(tri, 0); m.RecalculateNormals(); m.RecalculateBounds(); return m;
        }
    }
}
