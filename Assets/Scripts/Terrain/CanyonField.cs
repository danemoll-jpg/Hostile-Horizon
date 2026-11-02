using System;
using System.Collections.Concurrent;
using UnityEngine;

namespace HostileHorizon
{
    // The world's ground, from one seed (Round 1, item 2). A height function h(x, z) in metres (1 unit = 1 m):
    //  - a network of winding canyons: the zero lines of two domain-warped noise fields (|n| small = canyon floor), so canyons
    //    wind, fork and join;
    //  - open basins where a slow noise widens the canyon floors;
    //  - layered walls: the climb from floor to rim goes in terraces (ledges and steep risers), 150-350 m tall;
    //  - ridged high ground between canyons, and small rock detail everywhere.
    // Fix 2.4: deeper and more compact: three canyon systems at closer spacing (more branching), narrow floors (median about 50 m),
    // steeper walls 300-700 m tall in five ledges, basins rarer and smaller. Landing pads: one candidate per 520 m cell, kept where it
    // lies on a canyon floor, flattened to a 45 m-radius clearing (cut into the walls where they come close); pilots are placed on pads.
    // Pure C# apart from Vector3 / Mathf (thread-safe), so chunk jobs can run it off the main thread.
    // SurfaceHeight is the height of the drawn ground (the 4 m triangle grid of the nearest chunks), used for collisions.
    public sealed class CanyonField
    {
        public const float Cell = 4f;          // finest mesh spacing (LOD 0) and the collision grid
        public readonly int SeedNumber;        // the number the player sees
        readonly Noise warpA, warpB, chanA, chanB, chanC, basin, plateau, ridge, floorN, detail;
        public const float PadCell = 520f, PadRadius = 45f, PadBlend = 40f;
        public struct Pad { public bool ok; public float x, z, h; public Vector3 Position => new Vector3(x, h, z); }
        readonly ConcurrentDictionary<long, Pad> pads = new ConcurrentDictionary<long, Pad>();

        public CanyonField(int seedNumber)
        {
            SeedNumber = seedNumber;
            int s = Hash(seedNumber);
            warpA = new Noise(s + 1); warpB = new Noise(s + 2); chanA = new Noise(s + 3); chanB = new Noise(s + 4); basin = new Noise(s + 5);
            plateau = new Noise(s + 6); ridge = new Noise(s + 7); floorN = new Noise(s + 8); detail = new Noise(s + 9);
            chanC = new Noise(s + 10);
        }

        static int Hash(int v) { unchecked { uint x = (uint)v * 2654435761u; x ^= x >> 15; x *= 2246822519u; x ^= x >> 13; return (int)(x & 0x7fffffff); } }

        // 0 on a canyon floor, 1 on the rim and above (no terraces): the canyon "openness" at a point.
        public float CanyonT(float x, float z)
        {
            float wx = x + 240f * warpA.Fbm(x / 1100f, z / 1100f, 2);
            float wz = z + 240f * warpB.Fbm(x / 1100f + 31.7f, z / 1100f + 11.3f, 2);
            float n1 = chanA.Get(wx / 820f, wz / 820f);
            float n2 = chanB.Get(wx / 1350f + 7.1f, wz / 1350f - 3.3f);
            float n3 = chanC.Get(wx / 1050f - 4.2f, wz / 1050f + 9.6f);
            float d = Math.Min(Math.Min(Math.Abs(n1), Math.Abs(n2) * 1.3f), Math.Abs(n3) * 1.8f);
            float b = Noise.Smooth(.35f, .7f, basin.Get(x / 4200f, z / 4200f));
            float floorHalf = Noise.Lerp(.018f, .045f, b);
            return Noise.Smooth(floorHalf, floorHalf + .09f, d);
        }

        // The ground before the landing pads.
        float BaseHeight(float x, float z)
        {
            float t = CanyonT(x, z);
            // terraces: five ledges with steep risers between them, mixed with the plain slope so they look worn
            float s = t * 5f; float i = (float)Math.Floor(s); float f = s - i;
            float terr = (i + Noise.Smooth(.45f, 1f, f)) / 5f;
            float tt = Noise.Lerp(t, terr, .6f);
            float top = 340f + 130f * plateau.Fbm(x / 1700f, z / 1700f, 3) + 200f * ridge.Ridged(x / 900f, z / 900f, 2) * Noise.Smooth(.4f, 1f, t);
            float bottom = 10f * floorN.Fbm(x / 520f, z / 520f, 2);
            float h = bottom + (top - bottom) * tt;
            h += (3f + 8f * t) * detail.Fbm(x / 85f, z / 85f, 3);
            return h;
        }

        public float Height(float x, float z)
        {
            float h = BaseHeight(x, z);
            var p = PadAt((int)Math.Floor(x / PadCell), (int)Math.Floor(z / PadCell));
            if (p.ok)
            {
                float dx = x - p.x, dz = z - p.z, d = (float)Math.Sqrt(dx * dx + dz * dz);
                if (d < PadRadius + PadBlend) h = Noise.Lerp(p.h, h, Noise.Smooth(PadRadius, PadRadius + PadBlend, d));
            }
            return h;
        }

        // The landing pad of a cell (kept away from the cell's edges, so a point only ever needs its own cell's pad): the lowest of
        // a few seeded spots, used only if it lies on a canyon floor. Worked out once per cell, from the seed alone.
        public Pad PadAt(int ci, int cj)
        {
            long key = ((long)ci << 32) ^ (uint)cj;
            if (pads.TryGetValue(key, out var p)) return p;
            uint r = (uint)Hash(SeedNumber ^ (ci * 73856093) ^ (cj * 19349663)) | 1u;
            float margin = PadRadius + PadBlend + 10f, span = PadCell - 2 * margin, bestT = 9f, bx = 0, bz = 0;
            for (int k = 0; k < 12; k++)
            {
                r ^= r << 13; r ^= r >> 17; r ^= r << 5; float u = (r & 0xffff) / 65536f;
                r ^= r << 13; r ^= r >> 17; r ^= r << 5; float v = (r & 0xffff) / 65536f;
                float x = ci * PadCell + margin + u * span, z = cj * PadCell + margin + v * span, t = CanyonT(x, z);
                if (t < bestT) { bestT = t; bx = x; bz = z; }
            }
            p = new Pad { ok = bestT < .02f, x = bx, z = bz };
            if (p.ok) p.h = BaseHeight(bx, bz);
            pads[key] = p;
            return p;
        }

        // The nearest landing pad to a point (searching out to about 3 km), or false.
        public bool NearestPad(Vector3 near, out Pad pad)
        {
            int ci = (int)Math.Floor(near.x / PadCell), cj = (int)Math.Floor(near.z / PadCell);
            float best = float.MaxValue; pad = default;
            for (int dj = -6; dj <= 6; dj++)
                for (int di = -6; di <= 6; di++)
                {
                    var p = PadAt(ci + di, cj + dj); if (!p.ok) continue;
                    float d = (p.x - near.x) * (p.x - near.x) + (p.z - near.z) * (p.z - near.z);
                    if (d < best) { best = d; pad = p; }
                }
            return best < float.MaxValue;
        }

        // The drawn ground: the 4 m grid's triangles (diagonal from (i, j) to (i + 1, j + 1)), as TerrainChunk lays them.
        public float SurfaceHeight(float x, float z) => SurfaceHeight(x, z, out _);

        public float SurfaceHeight(float x, float z, out Vector3 normal)
        {
            float gx = x / Cell, gz = z / Cell;
            float i = Mathf.Floor(gx), j = Mathf.Floor(gz);
            float fx = gx - i, fz = gz - j;
            float x0 = i * Cell, z0 = j * Cell;
            float a = Height(x0, z0), b = Height(x0 + Cell, z0), c = Height(x0, z0 + Cell), d = Height(x0 + Cell, z0 + Cell);
            float h;
            if (fz > fx) { h = a + (d - c) * fx + (c - a) * fz; normal = new Vector3(-(d - c), Cell, -(c - a)).normalized; }
            else { h = a + (b - a) * fx + (d - b) * fz; normal = new Vector3(-(b - a), Cell, -(d - b)).normalized; }
            return h;
        }

        // A start on a canyon floor near the origin, facing along the canyon (the heading whose next 400 m stay lowest).
        // Fix 2.4: over the landing pad nearest the origin when there is one (a clearing: room to start in the narrow canyons).
        public void FindStart(out Vector3 position, out float heading)
        {
            Vector2 best = Vector2.zero; float bestT = 2f;
            if (NearestPad(Vector3.zero, out var pad)) { best = new Vector2(pad.x, pad.z); bestT = 0; }
            for (int ring = 0; ring <= 40 && bestT > .02f; ring++)
            {
                int n = Math.Max(1, ring * 8);
                for (int k = 0; k < n; k++)
                {
                    float a = k * Mathf.PI * 2 / n, r = ring * 60f;
                    float x = Mathf.Cos(a) * r, z = Mathf.Sin(a) * r;
                    float t = CanyonT(x, z);
                    if (t < bestT) { bestT = t; best = new Vector2(x, z); }
                }
            }
            heading = 0; float bestCost = float.MaxValue;
            for (int k = 0; k < 24; k++)
            {
                float a = k * 15f * Mathf.Deg2Rad, cost = 0;
                for (int m = 1; m <= 8; m++) cost = Math.Max(cost, Height(best.x + Mathf.Sin(a) * m * 50f, best.y + Mathf.Cos(a) * m * 50f));
                if (cost < bestCost) { bestCost = cost; heading = k * 15f; }
            }
            position = new Vector3(best.x, SurfaceHeight(best.x, best.y) + 45f, best.y);
        }
    }
}
