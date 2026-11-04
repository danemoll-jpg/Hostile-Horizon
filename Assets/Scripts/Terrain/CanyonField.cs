using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

namespace HostileHorizon
{
    // Round 4, item A1: a planet's biome, from its seed (same planet = same biome). A new career's first planet is always Canyons.
    public enum Biome { Canyons = 0, Forest = 1, City = 2, Ice = 3 }

    public static class Biomes
    {
        public static readonly string[] Keys = { "canyons", "forest", "city", "ice" };
        public static readonly string[] Names = { "Canyons", "Giant Forest", "Dead City", "Ice" };
        public static Biome ForSeed(int seed) { unchecked { uint x = (uint)seed * 2246822519u ^ 0x27d4eb2fu; x ^= x >> 15; x *= 2654435761u; x ^= x >> 13; return (Biome)(x % 4u); } }
        public static Biome FromKey(string k) { int i = Array.IndexOf(Keys, k); return i < 0 ? Biome.Canyons : (Biome)i; }
        public static string Name(Biome b) => Names[(int)b];
        public static string Name(string key) => Names[(int)FromKey(key)];
    }

    // The world's ground, from one seed (Round 1, item 2). A height function h(x, z) in metres (1 unit = 1 m):
    //  - a network of winding canyons: the zero lines of two domain-warped noise fields (|n| small = canyon floor), so canyons
    //    wind, fork and join;
    //  - open basins where a slow noise widens the canyon floors;
    //  - layered walls: the climb from floor to rim goes in terraces (ledges and steep risers), 150-350 m tall;
    //  - ridged high ground between canyons, and small rock detail everywhere.
    // Fix 2.4: deeper and more compact: three canyon systems at closer spacing (more branching), narrow floors (median about 50 m),
    // steeper walls 300-700 m tall in five ledges, basins rarer and smaller. Landing pads: one candidate per 520 m cell, kept where it
    // lies on a canyon floor, flattened to a 45 m-radius clearing (cut into the walls where they come close); pilots are placed on pads.
    // Round 4 (A2-A4): the same field in four biomes. "Canyon" below means the biome's open ways (CanyonT 0 = open floor), which the
    // route trail follows:
    //  - Canyons: as before;
    //  - Giant Forest: rolling ground with wide valleys (60-120 m deep); giant trees (trunks 5-15 m wide, 60-150 m tall, one per 64 m
    //    cell where it falls off the valley floors and out of the clearings). Trunks are solid (Solid); the canopy is soft (InCanopy);
    //  - Dead City: a street grid (32 m streets, 64 m avenues every third block) between towers 60-500 m tall, made right in the height
    //    field so roofs are real ground (rooftop pads, collisions, the pilots' run); broken tops, collapsed blocks (rubble), plazas.
    //    Sky bridges between towers are solid boxes;
    //  - Ice: snow plains (about 230 m up, stepped by ice cliffs) cut by narrow, deep crevasses (the canyons); some crevasses are
    //    roofed over by snow bridges you fly under (ice caves; solid boxes).
    // Pure C# apart from Vector3 / Mathf (thread-safe), so chunk jobs can run it off the main thread.
    // SurfaceHeight is the height of the drawn ground (the 4 m triangle grid of the nearest chunks), used for collisions.
    public sealed class CanyonField
    {
        public const float Cell = 4f;          // finest mesh spacing (LOD 0) and the collision grid
        public readonly int SeedNumber;        // the number the player sees
        public readonly Biome Biome;
        readonly Noise warpA, warpB, chanA, chanB, chanC, basin, plateau, ridge, floorN, detail;
        public const float PadCell = 520f, PadRadius = 45f, PadBlend = 40f;
        public float PadSize => Biome == Biome.City ? 26f : PadRadius;   // the flat area's radius (a rooftop is smaller)
        public struct Pad { public bool ok; public float x, z, h; public bool roof; public Vector3 Position => new Vector3(x, h, z); }
        readonly ConcurrentDictionary<long, Pad> pads = new ConcurrentDictionary<long, Pad>();
        readonly int hashSeed;

        public CanyonField(int seedNumber, Biome biome = Biome.Canyons)
        {
            SeedNumber = seedNumber; Biome = biome;
            int s = Hash(seedNumber); hashSeed = s;
            warpA = new Noise(s + 1); warpB = new Noise(s + 2); chanA = new Noise(s + 3); chanB = new Noise(s + 4); basin = new Noise(s + 5);
            plateau = new Noise(s + 6); ridge = new Noise(s + 7); floorN = new Noise(s + 8); detail = new Noise(s + 9);
            chanC = new Noise(s + 10);
        }

        static int Hash(int v) { unchecked { uint x = (uint)v * 2654435761u; x ^= x >> 15; x *= 2246822519u; x ^= x >> 13; return (int)(x & 0x7fffffff); } }
        uint H2(int a, int b, int salt) { unchecked { uint x = (uint)(a * 73856093) ^ (uint)(b * 19349663) ^ (uint)(salt * 83492791) ^ (uint)hashSeed; x ^= x >> 15; x *= 2246822519u; x ^= x >> 13; x *= 3266489917u; x ^= x >> 16; return x; } }
        float R01(int a, int b, int salt) => (H2(a, b, salt) & 0xffffff) / 16777216f;

        // 0 on the open floor (canyon floor, valley floor, street, crevasse floor), 1 on the rim and above: the "openness" at a point.
        public float CanyonT(float x, float z)
        {
            switch (Biome)
            {
                case Biome.City: return CityT(x, z);
                case Biome.Forest: return Channels(x, z, .05f, .09f, .22f, 1.35f);
                case Biome.Ice: return Channels(x, z, .014f, .032f, .05f, 1f);
                default: return Channels(x, z, .018f, .045f, .09f, 1f);
            }
        }

        // the canyon network: |noise| small = floor; floorHalf grows in basins, the walls rise over `wall`
        float Channels(float x, float z, float floorLo, float floorHi, float wall, float scale)
        {
            float wx = x + 240f * warpA.Fbm(x / 1100f, z / 1100f, 2);
            float wz = z + 240f * warpB.Fbm(x / 1100f + 31.7f, z / 1100f + 11.3f, 2);
            float n1 = chanA.Get(wx / (820f * scale), wz / (820f * scale));
            float n2 = chanB.Get(wx / (1350f * scale) + 7.1f, wz / (1350f * scale) - 3.3f);
            float n3 = chanC.Get(wx / (1050f * scale) - 4.2f, wz / (1050f * scale) + 9.6f);
            float d = Math.Min(Math.Min(Math.Abs(n1), Math.Abs(n2) * 1.3f), Math.Abs(n3) * 1.8f);
            float b = Noise.Smooth(.35f, .7f, basin.Get(x / 4200f, z / 4200f));
            float floorHalf = Noise.Lerp(floorLo, floorHi, b);
            return Noise.Smooth(floorHalf, floorHalf + wall, d);
        }

        // The ground before the landing pads.
        float BaseHeight(float x, float z)
        {
            switch (Biome)
            {
                case Biome.Forest: return ForestHeight(x, z);
                case Biome.City: return CityHeight(x, z);
                case Biome.Ice: return IceHeight(x, z);
            }
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

        // ---- Giant Forest: rolling ground, wide valleys
        float ForestHeight(float x, float z)
        {
            float t = CanyonT(x, z);
            float top = 85f + 45f * plateau.Fbm(x / 1500f, z / 1500f, 3);
            float hills = 30f * ridge.Fbm(x / 520f, z / 520f, 2) * t;
            float bottom = 5f * floorN.Fbm(x / 420f, z / 420f, 2);
            float s = t * t * (3 - 2 * t);
            return bottom + (top - bottom) * s + hills + 1.6f * detail.Fbm(x / 40f, z / 40f, 2);
        }

        // ---- Ice: snow plains stepped by ice cliffs, narrow deep crevasses
        float IceHeight(float x, float z)
        {
            float t = CanyonT(x, z);
            float plain = 210f + 45f * plateau.Fbm(x / 2200f, z / 2200f, 3);
            // ice cliffs: the plain in 70 m steps with sharp risers
            float c = ridge.Fbm(x / 1600f + 3.3f, z / 1600f - 1.7f, 2) * 2.2f + 1.5f; float ci = (float)Math.Floor(c), cf = c - ci;
            plain += 70f * (ci + Noise.Smooth(.82f, .9f, cf)) - 105f;
            float floor = 8f * floorN.Fbm(x / 500f, z / 500f, 2);
            float wall = (float)Math.Pow(t, .55f);   // steep from the floor: a crevasse, not a valley
            float h = floor + (plain - floor) * wall;
            h += (1f + 5f * Noise.Smooth(.1f, .6f, t) * (1 - Noise.Smooth(.85f, 1f, t))) * detail.Ridged(x / 60f, z / 60f, 2);   // rough ice in the walls
            h += 1.2f * detail.Fbm(x / 140f, z / 140f, 2) * t;   // snow drifts
            return h;
        }

        // ---- Dead City: blocks of 160 m (a 32 m street on their low sides, 64 m for an avenue every third block), split into 1-4
        // lots; each lot one building; some blocks are plazas, some collapsed into rubble. Edges on a 16 m grid (the mesh's LOD 0-2
        // grid lines), so walls stay sharp at a distance.
        const float Block = 160f;
        struct Lot { public int kind; public float x0, z0, x1, z1, h, broken; }   // kind 0 street, 1 building, 2 plaza, 3 rubble

        float Street(int bi) => (bi % 3 == 0) ? 64f : 32f;

        Lot LotAt(float x, float z)
        {
            int bi = (int)Math.Floor(x / Block), bj = (int)Math.Floor(z / Block);
            float bx = bi * Block, bz = bj * Block, sx = Street(((bi % 3) + 3) % 3 == 0 ? 0 : 1), sz = Street(((bj % 3) + 3) % 3 == 0 ? 0 : 1);
            float lx = x - bx, lz = z - bz;
            var lot = new Lot { kind = 0 };
            if (lx < sx || lz < sz) return lot;
            float r = R01(bi, bj, 1);
            float ground = CityGround(bx + Block / 2, bz + Block / 2);
            if (r < .09f) { lot.kind = 2; lot.x0 = bx + sx; lot.z0 = bz + sz; lot.x1 = bx + Block; lot.z1 = bz + Block; lot.h = ground + .5f; return lot; }
            if (r < .19f) { lot.kind = 3; lot.x0 = bx + sx; lot.z0 = bz + sz; lot.x1 = bx + Block; lot.z1 = bz + Block; lot.h = ground; return lot; }
            // split: 1, 2 (along x or z) or 4 lots, cut on the 16 m grid
            int split = (int)(R01(bi, bj, 2) * 4f);
            float cx = bx + sx + 16f * (float)Math.Floor((Block - sx) / 32f), cz = bz + sz + 16f * (float)Math.Floor((Block - sz) / 32f);
            int li = 0, lj = 0;
            lot.x0 = bx + sx; lot.x1 = bx + Block; lot.z0 = bz + sz; lot.z1 = bz + Block;
            if (split == 1 || split == 3) { if (x < cx) lot.x1 = cx; else { lot.x0 = cx; li = 1; } }
            if (split == 2 || split == 3) { if (z < cz) lot.z1 = cz; else { lot.z0 = cz; lj = 1; } }
            // a lot's building stands back from the lot edge on two sides sometimes (gaps between towers)
            int k = li + lj * 2 + 3;
            if (R01(bi, bj, k * 5) < .3f) { lot.x1 -= 16f; }
            if (R01(bi, bj, k * 7) < .3f) { lot.z1 -= 16f; }
            if (x >= lot.x1 || z >= lot.z1) return new Lot { kind = 0 };
            float down = Noise.Smooth(-.25f, .55f, plateau.Fbm(bx / 2600f, bz / 2600f, 2));   // downtown: the tall towers
            float hr = R01(bi, bj, k * 11);
            lot.kind = 1;
            lot.h = ground + Noise.Lerp(60f, 120f, hr) + 380f * down * down * hr * hr + 60f * R01(bi, bj, k * 13) * down;
            lot.broken = R01(bi, bj, k * 17) < .3f ? Noise.Lerp(20f, 90f, R01(bi, bj, k * 19)) : 0;
            return lot;
        }

        float CityGround(float x, float z) => 6f + 10f * floorN.Fbm(x / 1800f, z / 1800f, 2);

        float CityHeight(float x, float z)
        {
            var lot = LotAt(x, z);
            float g = CityGround(x, z);
            switch (lot.kind)
            {
                case 1:
                    float h = lot.h;
                    if (lot.broken > 0) h -= lot.broken * Noise.Smooth(.25f, .75f, ridge.Ridged(x / 26f, z / 26f, 2) * .7f + detail.Get(x / 37f, z / 37f) * .3f + .15f);
                    return Math.Max(g, h);
                case 2: return lot.h;
                case 3:
                    float cx = (lot.x0 + lot.x1) / 2, cz = (lot.z0 + lot.z1) / 2;
                    float d = Math.Max(Math.Abs(x - cx) / (lot.x1 - lot.x0), Math.Abs(z - cz) / (lot.z1 - lot.z0)) * 2;
                    return g + (1 - Noise.Smooth(.6f, 1f, d)) * (18f + 30f * Math.Abs(detail.Fbm(x / 30f, z / 30f, 3)));
            }
            return g + .6f * detail.Fbm(x / 9f, z / 9f, 1);   // the street: cracked, a little uneven
        }

        float CityT(float x, float z)
        {
            var lot = LotAt(x, z);
            if (lot.kind == 1) return 1f;
            if (lot.kind == 3) return .6f;
            if (lot.kind == 2) return 0f;
            // the street: open, a little less so right by the walls
            float e = 99f;
            for (int k = 0; k < 4; k++)
            {
                float dx = (k & 1) == 0 ? 16f : -16f, dz = 0; if (k >= 2) { dz = dx; dx = 0; }
                if (LotAt(x + dx, z + dz).kind == 1) e = Math.Min(e, 0);
            }
            return e < 1 ? .12f : 0f;
        }

        public float Height(float x, float z)
        {
            float h = BaseHeight(x, z);
            if (Biome == Biome.City) return h;   // roofs and plazas are flat already
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
        // Round 4: in the Dead City a pad is the middle of a flat roof (lots at least 64 m across; about half the pads) or a plaza.
        public Pad PadAt(int ci, int cj)
        {
            long key = ((long)ci << 32) ^ (uint)cj;
            if (pads.TryGetValue(key, out var p)) return p;
            uint r = (uint)Hash(SeedNumber ^ (ci * 73856093) ^ (cj * 19349663)) | 1u;
            float margin = PadRadius + PadBlend + 10f, span = PadCell - 2 * margin, bestT = 9f, bx = 0, bz = 0;
            if (Biome == Biome.City)
            {
                p = new Pad();
                bool wantRoof = (r & 4) != 0;
                for (int pass = 0; pass < 2 && !p.ok; pass++)
                    for (int k = 0; k < 16 && !p.ok; k++)
                    {
                        r ^= r << 13; r ^= r >> 17; r ^= r << 5; float u = (r & 0xffff) / 65536f;
                        r ^= r << 13; r ^= r >> 17; r ^= r << 5; float v = (r & 0xffff) / 65536f;
                        var lot = LotAt(ci * PadCell + 30 + u * (PadCell - 60), cj * PadCell + 30 + v * (PadCell - 60));
                        bool roof = lot.kind == 1 && lot.broken == 0 && lot.x1 - lot.x0 >= 64 && lot.z1 - lot.z0 >= 64 && lot.h < 330;
                        bool plaza = lot.kind == 2;
                        if (!(roof || plaza) || pass == 0 && roof != wantRoof) continue;
                        float px = (lot.x0 + lot.x1) / 2, pz = (lot.z0 + lot.z1) / 2;
                        if (Math.Floor(px / PadCell) != ci || Math.Floor(pz / PadCell) != cj) continue;
                        p = new Pad { ok = true, x = px, z = pz, h = lot.h, roof = roof };
                    }
                pads[key] = p; return p;
            }
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
            Vector2 best = Vector2.zero; float bestT = 2f; float baseH = 0;
            if (NearestPad(Vector3.zero, out var pad)) { best = new Vector2(pad.x, pad.z); bestT = 0; baseH = pad.h; }
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
            if (bestT > 0 || pad.ok) baseH = SurfaceHeight(best.x, best.y);
            for (int k = 0; k < 24; k++)
            {
                float a = k * 15f * Mathf.Deg2Rad, cost = 0;
                for (int m = 1; m <= 8; m++) cost = Math.Max(cost, Height(best.x + Mathf.Sin(a) * m * 50f, best.y + Mathf.Cos(a) * m * 50f) - baseH);
                if (cost < bestCost) { bestCost = cost; heading = k * 15f; }
            }
            position = new Vector3(best.x, SurfaceHeight(best.x, best.y) + 45f, best.y);
        }

        // ---- Round 4: solid props (tree trunks, sky bridges, snow bridges) and the soft canopy, all from the seed alone

        public const float TreeCell = 64f, PropCell = 256f;
        public struct Tree { public bool ok; public Vector3 basePos; public float radius, height, yaw; public Vector3 crown, crownSize; public float trunkTop; }
        public struct Box { public Vector3 c, half; public float yaw; public int kind; }   // kind 0 sky bridge, 1 snow bridge
        readonly ConcurrentDictionary<long, Tree> trees = new ConcurrentDictionary<long, Tree>();
        readonly ConcurrentDictionary<long, Box[]> boxes = new ConcurrentDictionary<long, Box[]>();

        public Tree TreeAt(int ti, int tj)
        {
            long key = ((long)ti << 32) ^ (uint)tj;
            if (trees.TryGetValue(key, out var t)) return t;
            t = new Tree();
            if (Biome == Biome.Forest && R01(ti, tj, 41) < .86f)
            {
                float x = (ti + .5f) * TreeCell + (R01(ti, tj, 42) - .5f) * 2 * 22f, z = (tj + .5f) * TreeCell + (R01(ti, tj, 43) - .5f) * 2 * 22f;
                float open = CanyonT(x, z);
                bool clearing = false;
                var p = PadAt((int)Math.Floor(x / PadCell), (int)Math.Floor(z / PadCell));
                if (p.ok) { float dx = x - p.x, dz = z - p.z; clearing = dx * dx + dz * dz < (PadRadius + 40f) * (PadRadius + 40f); }
                // near the start (the mothership's lift) too
                if (!clearing && open > .07f)   // the very floor of a valley stays open (the route trail's way)
                {
                    float big = R01(ti, tj, 44), r = Noise.Lerp(2.5f, 7.5f, big * big), h = Noise.Lerp(60f, 150f, R01(ti, tj, 45) * .5f + big * .5f);
                    float g = Math.Min(Math.Min(Height(x - r, z), Height(x + r, z)), Math.Min(Height(x, z - r), Height(x, z + r)));
                    float cw = Noise.Lerp(26f, 42f, R01(ti, tj, 46)) * Noise.Lerp(.85f, 1.15f, big);
                    float ch = Noise.Lerp(.3f, .42f, R01(ti, tj, 48)) * h;   // a deep crown: a third of the tree or more
                    t = new Tree { ok = true, basePos = new Vector3(x, g - 2f, z), radius = r, height = h, yaw = R01(ti, tj, 47) * 360f,
                                   crown = new Vector3(x, g + h - ch * .55f, z), crownSize = new Vector3(cw, ch * .6f, cw * Noise.Lerp(.8f, 1.2f, R01(ti, tj, 49))),
                                   trunkTop = g + h - ch * .5f };
                }
            }
            trees[key] = t; return t;
        }

        // sky bridges (Dead City) and snow bridges over crevasses (Ice) in a 256 m cell
        public Box[] BoxesAt(int ci, int cj)
        {
            long key = ((long)ci << 32) ^ (uint)cj;
            if (boxes.TryGetValue(key, out var list)) return list;
            var l = new List<Box>();
            if (Biome == Biome.City)
            {
                for (int k = 0; k < 2; k++)
                {
                    if (R01(ci, cj, 60 + k) > .45f) continue;
                    // a street crossing in the cell: look for two towers either side of a street at a seeded spot
                    float x = ci * PropCell + 40 + R01(ci, cj, 62 + k) * (PropCell - 80), z = cj * PropCell + 40 + R01(ci, cj, 64 + k) * (PropCell - 80);
                    bool alongX = R01(ci, cj, 66 + k) < .5f;
                    var here = LotAt(x, z); if (here.kind != 0) continue;
                    // find the walls on both sides
                    float a = 0, b = 0; int ia = 0, ib = 0;
                    for (int s = 1; s <= 20 && ia == 0; s++) { float q = s * 4f; if ((alongX ? LotAt(x - q, z) : LotAt(x, z - q)).kind == 1) { a = q; ia = 1; } }
                    for (int s = 1; s <= 20 && ib == 0; s++) { float q = s * 4f; if ((alongX ? LotAt(x + q, z) : LotAt(x, z + q)).kind == 1) { b = q; ib = 1; } }
                    if (ia == 0 || ib == 0 || a + b > 90) continue;
                    var la = alongX ? LotAt(x - a, z) : LotAt(x, z - a); var lb = alongX ? LotAt(x + b, z) : LotAt(x, z + b);
                    float top = Math.Min(la.h - la.broken, lb.h - lb.broken);
                    float ground = CityGround(x, z);
                    float y = ground + Noise.Lerp(.35f, .7f, R01(ci, cj, 68 + k)) * (top - ground);
                    if (y - ground < 75f || top - y < 15f) continue;
                    float len = a + b + 12f, mid = (b - a) / 2;
                    var c = alongX ? new Vector3(x + mid, y, z) : new Vector3(x, y, z + mid);
                    l.Add(new Box { kind = 0, c = c, half = new Vector3(5f, 3.5f, len / 2), yaw = alongX ? 90f : 0f });
                }
            }
            else if (Biome == Biome.Ice && R01(ci, cj, 70) < .42f)
            {
                float x = (ci + .5f) * PropCell + (R01(ci, cj, 71) - .5f) * 120f, z = (cj + .5f) * PropCell + (R01(ci, cj, 72) - .5f) * 120f;
                if (CanyonT(x, z) < .05f)
                {
                    var p = PadAt((int)Math.Floor(x / PadCell), (int)Math.Floor(z / PadCell));
                    bool nearPad = p.ok && new Vector2(x - p.x, z - p.z).magnitude < 220f;
                    // the crevasse's direction: across the gradient of the openness
                    float e = 12f, gx = CanyonT(x + e, z) - CanyonT(x - e, z), gz = CanyonT(x, z + e) - CanyonT(x, z - e);
                    if (!nearPad && gx * gx + gz * gz > 1e-6f)
                    {
                        float across = Mathf.Atan2(gx, gz) * Mathf.Rad2Deg;   // z axis of the box across the crevasse
                        float floor = Height(x, z);
                        float rim = Math.Min(Height(x + Mathf.Sin(across * Mathf.Deg2Rad) * 70f, z + Mathf.Cos(across * Mathf.Deg2Rad) * 70f),
                                             Height(x - Mathf.Sin(across * Mathf.Deg2Rad) * 70f, z - Mathf.Cos(across * Mathf.Deg2Rad) * 70f));
                        float bottom = floor + Noise.Lerp(75f, 110f, R01(ci, cj, 73));
                        if (rim - bottom > 25f)
                        {
                            float len = Noise.Lerp(70f, 150f, R01(ci, cj, 74));   // along the crevasse
                            l.Add(new Box { kind = 1, c = new Vector3(x, (bottom + rim + 3f) / 2, z), half = new Vector3(len / 2, (rim + 3f - bottom) / 2, 75f), yaw = across + 90f });
                        }
                    }
                }
            }
            list = l.ToArray(); boxes[key] = list; return list;
        }

        // A point inside something solid that is not the ground (a trunk, a bridge): the way out and how far. Checks the point's own
        // tree cell (a trunk never crosses its cell) and the bridges of the 3 x 3 prop cells round it.
        public bool SolidPush(Vector3 p, out Vector3 normal, out float depth)
        {
            normal = Vector3.up; depth = 0;
            if (Biome == Biome.Forest)
            {
                var t = TreeAt((int)Math.Floor(p.x / TreeCell), (int)Math.Floor(p.z / TreeCell));
                if (t.ok && p.y < t.trunkTop && p.y > t.basePos.y)
                {
                    float dx = p.x - t.basePos.x, dz = p.z - t.basePos.z, d = (float)Math.Sqrt(dx * dx + dz * dz);
                    float r = TrunkRadius(t, p.y);
                    if (d < r) { depth = r - d; normal = d > .01f ? new Vector3(dx / d, 0, dz / d) : Vector3.forward; return true; }
                }
                return false;
            }
            if (Biome != Biome.City && Biome != Biome.Ice) return false;
            int ci = (int)Math.Floor(p.x / PropCell), cj = (int)Math.Floor(p.z / PropCell);
            for (int dj = -1; dj <= 1; dj++)
                for (int di = -1; di <= 1; di++)
                    foreach (var b in BoxesAt(ci + di, cj + dj))
                    {
                        var q = Quaternion.Euler(0, -b.yaw, 0) * (p - b.c);
                        float ox = b.half.x - Math.Abs(q.x), oy = b.half.y - Math.Abs(q.y), oz = b.half.z - Math.Abs(q.z);
                        if (ox <= 0 || oy <= 0 || oz <= 0) continue;
                        Vector3 n; float dd;
                        if (ox < oy && ox < oz) { n = new Vector3(Math.Sign(q.x), 0, 0); dd = ox; }
                        else if (oy < oz) { n = new Vector3(0, Math.Sign(q.y), 0); dd = oy; }
                        else { n = new Vector3(0, 0, Math.Sign(q.z)); dd = oz; }
                        if (dd > depth) { depth = dd; normal = Quaternion.Euler(0, b.yaw, 0) * n; }
                    }
            return depth > 0;
        }

        // the trunk flares out at the roots (buttresses) and tapers a little to the crown; the same shape as Props' trunk mesh
        public static float TrunkRadius(Tree t, float y) { float u = Mathf.Clamp01((y - t.basePos.y) / t.height); return t.radius * (1f + .7f * Mathf.Exp(-u * 14f) - .25f * u); }

        // Inside a tree's crown (soft: leaves, no crash). 0 outside, up to 1 at its middle.
        public float InCanopy(Vector3 p)
        {
            if (Biome != Biome.Forest) return 0;
            int ti = (int)Math.Floor(p.x / TreeCell), tj = (int)Math.Floor(p.z / TreeCell); float best = 0;
            for (int dj = -1; dj <= 1; dj++)
                for (int di = -1; di <= 1; di++)
                {
                    var t = TreeAt(ti + di, tj + dj); if (!t.ok) continue;
                    var d = p - t.crown; float k = (d.x * d.x) / (t.crownSize.x * t.crownSize.x) + (d.y * d.y) / (t.crownSize.y * t.crownSize.y) + (d.z * d.z) / (t.crownSize.z * t.crownSize.z);
                    if (k < 1) best = Math.Max(best, 1 - k);
                }
            return best;
        }
    }
}
