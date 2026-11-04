using System.Collections.Generic;
using UnityEngine;

namespace HostileHorizon
{
    // Round 2, item 4: where a planet's downed pilots are, from its seed alone (the same seed always gives the same places).
    // Each site: a patch of fairly flat canyon floor to land on, the pilot standing at its edge, their crashed ship beyond.
    // Distance from the planet's start (where the mothership arrives) grows with the planet's tier; sites are kept apart.
    // Fix 2.4: the canyons are narrow now, so sites are the terrain's flat landing pads (CanyonField.PadAt); bare canyon floor only
    // as a last resort.
    public struct PilotSite { public Vector3 landing, pilot, wreck; public float wreckHeading; }

    public static class PlanetSites
    {
        public static List<PilotSite> Find(CanyonField f, int count, int tier)
        {
            var t = Tuning.T; var rng = new SeedRng(f.SeedNumber, 7);
            f.FindStart(out var start, out _);
            float rMin = t.pilotRangeMin + tier * t.pilotRangePerTier * .5f, rMax = t.pilotRangeMax + tier * t.pilotRangePerTier;
            var sites = new List<PilotSite>();
            // strict first, then looser (flatness, floor, spacing) if the planet is short of good spots
            float[] flat = { 1.6f, 2.6f, 4f }, floor = { .02f, .05f, .1f }, gap = { 1f, .7f, .45f };   // pass 0, 1: pads; pass 2: any floor
            for (int pass = 0; pass < 3 && sites.Count < count; pass++)
                for (int attempt = 0; attempt < 3000 && sites.Count < count; attempt++)
                {
                    float a = rng.Value * Mathf.PI * 2, r = Mathf.Lerp(rMin, rMax, Mathf.Sqrt(rng.Value));
                    float x = start.x + Mathf.Cos(a) * r, z = start.z + Mathf.Sin(a) * r;
                    if (pass < 2)
                    {
                        var pad = f.PadAt(Mathf.FloorToInt(x / CanyonField.PadCell), Mathf.FloorToInt(z / CanyonField.PadCell));
                        if (!pad.ok) continue;
                        x = pad.x; z = pad.z;
                    }
                    if (pass == 2 && f.CanyonT(x, z) > floor[pass]) continue;   // (a pad is checked already: Round 4's rooftops aren't canyon floor)
                    if (Bumpiness(f, x, z, 16f) > flat[pass]) continue;
                    bool near = false; foreach (var s in sites) if (new Vector2(s.landing.x - x, s.landing.z - z).magnitude < t.pilotSpacing * gap[pass]) { near = true; break; }
                    if (near) continue;
                    // the pilot 30 m off the landing patch, the wreck 10 m beyond (still on the pad): the way that stays lowest
                    // Round 4: closer on a rooftop (its flat area is smaller): 55 % and 80 % of the pad's size
                    float po = Mathf.Min(30f, f.PadSize * .55f), wo = Mathf.Min(40f, f.PadSize * .8f);
                    float bestDir = 0, bestH = float.MaxValue;
                    for (int k = 0; k < 8; k++) { float d = k * 45f + rng.Value * 20f; var p = Off(x, z, d, wo); float h = f.Height(p.x, p.y) + Bumpiness(f, p.x, p.y, 6f) * 4f; if (h < bestH) { bestH = h; bestDir = d; } }
                    var pp = Off(x, z, bestDir, po); var wp = Off(x, z, bestDir, wo);
                    sites.Add(new PilotSite
                    {
                        landing = new Vector3(x, f.SurfaceHeight(x, z), z),
                        pilot = new Vector3(pp.x, f.SurfaceHeight(pp.x, pp.y), pp.y),
                        wreck = new Vector3(wp.x, f.SurfaceHeight(wp.x, wp.y), wp.y),
                        wreckHeading = rng.Value * 360f,
                    });
                }
            return sites;
        }

        static Vector2 Off(float x, float z, float deg, float m) => new Vector2(x + Mathf.Sin(deg * Mathf.Deg2Rad) * m, z + Mathf.Cos(deg * Mathf.Deg2Rad) * m);

        // Height range over a small patch: low = flat enough to set a ship down.
        public static float Bumpiness(CanyonField f, float x, float z, float radius)
        {
            float lo = float.MaxValue, hi = float.MinValue;
            for (int k = 0; k < 9; k++)
            {
                float px = x, pz = z;
                if (k > 0) { float a = k * Mathf.PI / 4; px += Mathf.Cos(a) * radius; pz += Mathf.Sin(a) * radius; }
                float h = f.Height(px, pz); lo = Mathf.Min(lo, h); hi = Mathf.Max(hi, h);
            }
            return hi - lo;
        }
    }
}
