using System;
using UnityEngine;

namespace HostileHorizon
{
    // The world's ground, from one seed (Round 1, item 2). A height function h(x, z) in metres (1 unit = 1 m):
    //  - a network of winding canyons: the zero lines of two domain-warped noise fields (|n| small = canyon floor), so canyons
    //    wind, fork and join;
    //  - open basins where a slow noise widens the canyon floors;
    //  - layered walls: the climb from floor to rim goes in terraces (ledges and steep risers), 150-350 m tall;
    //  - ridged high ground between canyons, and small rock detail everywhere.
    // Pure C# apart from Vector3 / Mathf (thread-safe), so chunk jobs can run it off the main thread.
    // SurfaceHeight is the height of the drawn ground (the 4 m triangle grid of the nearest chunks), used for collisions.
    public sealed class CanyonField
    {
        public const float Cell = 4f;          // finest mesh spacing (LOD 0) and the collision grid
        public readonly int SeedNumber;        // the number the player sees
        readonly Noise warpA, warpB, chanA, chanB, basin, plateau, ridge, floorN, detail;

        public CanyonField(int seedNumber)
        {
            SeedNumber = seedNumber;
            int s = Hash(seedNumber);
            warpA = new Noise(s + 1); warpB = new Noise(s + 2); chanA = new Noise(s + 3); chanB = new Noise(s + 4); basin = new Noise(s + 5);
            plateau = new Noise(s + 6); ridge = new Noise(s + 7); floorN = new Noise(s + 8); detail = new Noise(s + 9);
        }

        static int Hash(int v) { unchecked { uint x = (uint)v * 2654435761u; x ^= x >> 15; x *= 2246822519u; x ^= x >> 13; return (int)(x & 0x7fffffff); } }

        // 0 on a canyon floor, 1 on the rim and above (no terraces): the canyon "openness" at a point.
        public float CanyonT(float x, float z)
        {
            float wx = x + 380f * warpA.Fbm(x / 1900f, z / 1900f, 2);
            float wz = z + 380f * warpB.Fbm(x / 1900f + 31.7f, z / 1900f + 11.3f, 2);
            float n1 = chanA.Get(wx / 1500f, wz / 1500f);
            float n2 = chanB.Get(wx / 2600f + 7.1f, wz / 2600f - 3.3f);
            float d = Math.Min(Math.Abs(n1), Math.Abs(n2) * 1.35f);
            float b = Noise.Smooth(.15f, .55f, basin.Get(x / 4200f, z / 4200f));
            float floorHalf = Noise.Lerp(.05f, .17f, b);
            return Noise.Smooth(floorHalf, floorHalf + .13f, d);
        }

        public float Height(float x, float z)
        {
            float t = CanyonT(x, z);
            // terraces: four ledges with steep risers between them, mixed with the plain slope so they look worn
            float s = t * 4f; float i = (float)Math.Floor(s); float f = s - i;
            float terr = (i + Noise.Smooth(.45f, 1f, f)) / 4f;
            float tt = Noise.Lerp(t, terr, .65f);
            float top = 210f + 70f * plateau.Fbm(x / 1700f, z / 1700f, 3) + 110f * ridge.Ridged(x / 1150f, z / 1150f, 2) * Noise.Smooth(.4f, 1f, t);
            float bottom = 14f * floorN.Fbm(x / 520f, z / 520f, 2);
            float h = bottom + (top - bottom) * tt;
            h += (3f + 6f * t) * detail.Fbm(x / 85f, z / 85f, 3);
            return h;
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
        public void FindStart(out Vector3 position, out float heading)
        {
            Vector2 best = Vector2.zero; float bestT = 2f;
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
