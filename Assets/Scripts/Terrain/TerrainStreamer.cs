using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace HostileHorizon
{
    // Round 1, item 2: chunked terrain meshes streamed around a point (the camera rig). 256 m chunks, finer near the viewer
    // (4 / 8 / 16 / 32 m spacing), with skirts hanging down from every edge so neighbours of different detail never show gaps.
    // Heights are made on worker threads (CanyonField is thread-safe); meshes are built on the main thread, a few per frame.
    public sealed class TerrainStreamer : MonoBehaviour
    {
        public const float ChunkSize = 256f;
        static readonly int[] Cells = { 64, 32, 16, 8 };            // cells per chunk side for LOD 0..3
        static readonly float[] LodRange = { 520f, 1150f, 2100f };   // beyond the last: LOD 3

        public CanyonField Field { get; private set; }
        public float ViewDistance = 3000f;
        public int ShadowLods = 2;                                     // LODs that cast shadows
        public Material Material;
        public int Pending => jobs.Count;
        public int Built => chunks.Count;

        sealed class Chunk { public GameObject go; public Mesh mesh; public int lod; }
        sealed class Job { public Vector2Int key; public int lod; public Task<Vector3[][]> task; }

        readonly Dictionary<Vector2Int, Chunk> chunks = new Dictionary<Vector2Int, Chunk>();
        readonly Dictionary<Vector2Int, Job> jobs = new Dictionary<Vector2Int, Job>();
        readonly Dictionary<int, int[]> triangles = new Dictionary<int, int[]>();
        readonly List<Vector2Int> scratch = new List<Vector2Int>();
        Vector3 focus;
        const int MaxJobs = 6, MaxBuildsPerFrame = 4;

        public void SetField(CanyonField field)
        {
            foreach (var c in chunks.Values) { Destroy(c.mesh); Destroy(c.go); }
            chunks.Clear(); jobs.Clear();   // running tasks finish and are dropped
            Field = field;
        }

        public void SetFocus(Vector3 p) => focus = p;

        static int LodFor(float d) { for (int i = 0; i < LodRange.Length; i++) if (d < LodRange[i]) return i; return 3; }

        float DistanceTo(Vector2Int k)
        {
            float cx = (k.x + .5f) * ChunkSize, cz = (k.y + .5f) * ChunkSize;
            float dx = Mathf.Max(0, Mathf.Abs(focus.x - cx) - ChunkSize / 2), dz = Mathf.Max(0, Mathf.Abs(focus.z - cz) - ChunkSize / 2);
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        void Update()
        {
            if (Field == null) return;
            // finished jobs -> meshes (nearest first is good enough: jobs are started nearest first)
            int built = 0;
            scratch.Clear();
            foreach (var kv in jobs) if (kv.Value.task.IsCompleted) scratch.Add(kv.Key);
            foreach (var key in scratch)
            {
                if (built >= MaxBuildsPerFrame) break;
                var job = jobs[key]; jobs.Remove(key);
                if (job.task.IsFaulted) { Debug.LogException(job.task.Exception); continue; }
                Build(job.key, job.lod, job.task.Result); built++;
            }
            // drop far chunks
            scratch.Clear();
            foreach (var kv in chunks) if (DistanceTo(kv.Key) > ViewDistance + ChunkSize) scratch.Add(kv.Key);
            foreach (var key in scratch) { var c = chunks[key]; Destroy(c.mesh); Destroy(c.go); chunks.Remove(key); }
            // start jobs for missing / wrong-detail chunks, nearest first
            if (jobs.Count >= MaxJobs) return;
            int r = Mathf.CeilToInt(ViewDistance / ChunkSize);
            int fx = Mathf.FloorToInt(focus.x / ChunkSize), fz = Mathf.FloorToInt(focus.z / ChunkSize);
            Vector2Int bestKey = default; float bestD = float.MaxValue; int bestLod = 0;
            for (int pass = 0; pass < MaxJobs - jobs.Count; pass++)
            {
                bestD = float.MaxValue;
                for (int x = fx - r; x <= fx + r; x++)
                    for (int z = fz - r; z <= fz + r; z++)
                    {
                        var k = new Vector2Int(x, z);
                        float d = DistanceTo(k);
                        if (d > ViewDistance || d >= bestD || jobs.ContainsKey(k)) continue;
                        int lod = LodFor(d);
                        if (chunks.TryGetValue(k, out var c) && c.lod == lod) continue;
                        bestD = d; bestKey = k; bestLod = lod;
                    }
                if (bestD == float.MaxValue) break;
                var field = Field; var key = bestKey; int l = bestLod;
                jobs[key] = new Job { key = key, lod = l, task = Task.Run(() => Heights(field, key, l)) };
            }
        }

        // [0] positions (chunk-local, grid then skirt), [1] normals
        static Vector3[][] Heights(CanyonField f, Vector2Int key, int lod)
        {
            int n = Cells[lod]; float step = ChunkSize / n; int v = n + 1;
            float ox = key.x * ChunkSize, oz = key.y * ChunkSize;
            // heights with a one-sample border for normals
            var h = new float[(v + 2) * (v + 2)];
            for (int j = -1; j <= v; j++)
                for (int i = -1; i <= v; i++)
                    h[(j + 1) * (v + 2) + i + 1] = f.Height(ox + i * step, oz + j * step);
            int skirt = 4 * n;
            var pos = new Vector3[v * v + skirt]; var nor = new Vector3[pos.Length];
            for (int j = 0; j < v; j++)
                for (int i = 0; i < v; i++)
                {
                    float hc = h[(j + 1) * (v + 2) + i + 1];
                    float hl = h[(j + 1) * (v + 2) + i], hr = h[(j + 1) * (v + 2) + i + 2], hd = h[j * (v + 2) + i + 1], hu = h[(j + 2) * (v + 2) + i + 1];
                    pos[j * v + i] = new Vector3(i * step, hc, j * step);
                    nor[j * v + i] = new Vector3(hl - hr, 2 * step, hd - hu).normalized;
                }
            // skirt: the edge ring again, dropped
            float drop = 6f + step * 2f; int s = v * v;
            foreach (int idx in EdgeRing(n)) { var p = pos[idx]; pos[s] = new Vector3(p.x, p.y - drop, p.z); nor[s] = nor[idx]; s++; }
            return new[] { pos, nor };
        }

        // the edge vertices in order around the chunk (4n of them)
        static IEnumerable<int> EdgeRing(int n)
        {
            int v = n + 1;
            for (int i = 0; i < n; i++) yield return i;                    // bottom, left to right
            for (int j = 0; j < n; j++) yield return j * v + n;            // right, bottom to top
            for (int i = n; i > 0; i--) yield return n * v + i;            // top, right to left
            for (int j = n; j > 0; j--) yield return j * v;                // left, top to bottom
        }

        int[] Triangles(int lod)
        {
            if (triangles.TryGetValue(lod, out var t)) return t;
            int n = Cells[lod], v = n + 1; var list = new List<int>(n * n * 6 + n * 24);
            for (int j = 0; j < n; j++)
                for (int i = 0; i < n; i++)
                {
                    int a = j * v + i, b = a + 1, c = a + v, d = c + 1;
                    list.Add(a); list.Add(c); list.Add(d);   // the diagonal a-d, as CanyonField.SurfaceHeight
                    list.Add(a); list.Add(d); list.Add(b);
                }
            var ring = new List<int>(EdgeRing(n)); int s = v * v, m = ring.Count;
            for (int k = 0; k < m; k++)
            {
                int top0 = ring[k], top1 = ring[(k + 1) % m], low0 = s + k, low1 = s + (k + 1) % m;
                // both windings, so the skirt shows from either side
                list.Add(top0); list.Add(low0); list.Add(top1); list.Add(top1); list.Add(low0); list.Add(low1);
                list.Add(top0); list.Add(top1); list.Add(low0); list.Add(top1); list.Add(low1); list.Add(low0);
            }
            t = list.ToArray(); triangles[lod] = t; return t;
        }

        void Build(Vector2Int key, int lod, Vector3[][] data)
        {
            if (DistanceTo(key) > ViewDistance + ChunkSize) return;
            if (!chunks.TryGetValue(key, out var c))
            {
                c = new Chunk { go = new GameObject($"Chunk {key.x},{key.y}") };
                c.go.transform.SetParent(transform, false);
                c.go.transform.position = new Vector3(key.x * ChunkSize, 0, key.y * ChunkSize);
                c.go.AddComponent<MeshFilter>(); c.go.AddComponent<MeshRenderer>().sharedMaterial = Material;
                chunks[key] = c;
            }
            if (c.mesh) Destroy(c.mesh);
            c.mesh = new Mesh { name = c.go.name, indexFormat = IndexFormat.UInt16 };
            c.mesh.SetVertices(data[0]); c.mesh.SetNormals(data[1]); c.mesh.SetTriangles(Triangles(lod), 0, true);
            c.go.GetComponent<MeshFilter>().sharedMesh = c.mesh;
            var mr = c.go.GetComponent<MeshRenderer>();
            mr.shadowCastingMode = lod < ShadowLods ? ShadowCastingMode.On : ShadowCastingMode.Off;
            c.lod = lod;
        }

        // Everything within `radius` of the focus at its final detail and nothing waiting (used before the title shows / a flight starts).
        public bool ReadyNear(float radius)
        {
            if (Field == null) return false;
            int r = Mathf.CeilToInt(radius / ChunkSize);
            int fx = Mathf.FloorToInt(focus.x / ChunkSize), fz = Mathf.FloorToInt(focus.z / ChunkSize);
            for (int x = fx - r; x <= fx + r; x++)
                for (int z = fz - r; z <= fz + r; z++)
                {
                    var k = new Vector2Int(x, z); float d = DistanceTo(k);
                    if (d > radius) continue;
                    if (!chunks.TryGetValue(k, out var c) || c.lod != LodFor(d)) return false;
                }
            return true;
        }
    }
}
