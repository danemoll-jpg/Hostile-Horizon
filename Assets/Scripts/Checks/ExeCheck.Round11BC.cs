using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Profiling;

namespace HostileHorizon
{
    // Round 11 Part B check (-exeCheckOnly round11b [-steamDeck 1]): the land bridges from the seed (how many, how high, kept off the
    // pads and over the route trail's height, solid, the same seed = the same bridges); in Free Flight on the seed of the day's Canyons:
    // cockpit pictures low in a canyon, a wall up close and a land bridge ahead, each with the Round 10 look (before) and the photo
    // textures (after); a flight under a bridge (no hit) and one into it (a hit); the frame rate before / after in Canyons and after in
    // every other biome; the textures' memory.
    public sealed partial class ExeCheck
    {
        void BridgeFacts(Biome b, int seed)
        {
            var f = new CanyonField(seed, b); var f2 = new CanyonField(seed, b);
            int n = 0, tight = 0; float minClear = 1e9f, maxClear = 0, minMargin = 1e9f, minPad = 1e9f; bool same = true; CanyonField.Box first = default;
            for (int j = -8; j < 8; j++)
                for (int i = -8; i < 8; i++)
                {
                    var a = f.BoxesAt(i, j); var a2 = f2.BoxesAt(i, j);
                    same &= a.Length == a2.Length && a.Zip(a2, (x, y) => x.c == y.c && x.half == y.half).All(z => z);
                    foreach (var x in a.Where(x => x.kind == 3))
                    {
                        if (n == 0) first = x; n++;
                        float bottom = x.c.y - x.half.y, floor = f.Height(x.c.x, x.c.z);
                        // the floor right under the bridge's middle may not be the canyon floor (the box is centred between the walls): the lowest ground under its middle third
                        var across = Quaternion.Euler(0, x.yaw, 0) * Vector3.forward; float lo = 1e9f;
                        for (float q = -x.half.z / 3; q <= x.half.z / 3; q += 4) { var pp = x.c + across * q; lo = Mathf.Min(lo, f.Height(pp.x, pp.z)); }
                        float clear = bottom - lo; minClear = Mathf.Min(minClear, clear); maxClear = Mathf.Max(maxClear, clear); if (clear < 95) tight++;
                        // the route trail's height under it (RouteTrail's rule at the lowest point)
                        if (f.NearestPad(x.c, out var pad)) minPad = Mathf.Min(minPad, new Vector2(pad.x - x.c.x, pad.z - x.c.z).magnitude);
                        float trail = lo + RouteTrail.Clearance; minMargin = Mathf.Min(minMargin, bottom - trail);
                        bool solid = f.SolidPush(x.c, out _, out _) && !f.SolidPush(x.c - Vector3.up * (x.half.y + 3), out _, out _);
                        if (!solid) Log($"    bridge at {x.c:F0}: NOT solid as expected");
                    }
                }
            Log($"  {Biomes.Name(b)} seed {seed}: {n} land bridges in 16 x 16 prop cells (4.1 x 4.1 km); clearance under them {minClear:0}-{maxClear:0} m ({tight} under 95 m: tight); at least {minMargin:0} m over the route trail's 35 m line; nearest pad {minPad:0} m (rule: over 260 m); same seed, same bridges: {same}" +
                (n > 0 ? $"; e.g. {first.half.z * 2:0} m long, {first.half.x * 2:0} m wide, {first.half.y * 2:0} m thick" : ""));
        }

        bool NearestBridge(CanyonField f, Vector3 p, out CanyonField.Box box)
        {
            int ci = Mathf.FloorToInt(p.x / CanyonField.PropCell), cj = Mathf.FloorToInt(p.z / CanyonField.PropCell); box = default;
            for (int r = 0; r < 24; r++) for (int dj = -r; dj <= r; dj++) for (int di = -r; di <= r; di++)
                    { if (Mathf.Max(Mathf.Abs(di), Mathf.Abs(dj)) != r) continue; foreach (var b in f.BoxesAt(ci + di, cj + dj)) if (b.kind == 3) { box = b; return true; } }
            return false;
        }

        // the bridge with the least room under it within about 4 km (the best picture of "tight")
        bool TightestBridge(CanyonField f, Vector3 p, out CanyonField.Box box)
        {
            int ci = Mathf.FloorToInt(p.x / CanyonField.PropCell), cj = Mathf.FloorToInt(p.z / CanyonField.PropCell); box = default; float best = 1e9f;
            for (int dj = -16; dj <= 16; dj++) for (int di = -16; di <= 16; di++)
                    foreach (var b in f.BoxesAt(ci + di, cj + dj)) if (b.kind == 3) { float room = b.c.y - b.half.y - f.Height(b.c.x, b.c.z); if (room < best) { best = room; box = b; } }
            return best < 1e9f;
        }

        IEnumerator BeforeAfter(string name)
        {
            WorldLook.SetPhoto(false); yield return null; yield return null; yield return Shot(name + "_before");
            WorldLook.SetPhoto(true); yield return null; yield return null; yield return Shot(name + "_after");
        }

        void Hold(Vector3 pos, Quaternion rot) { G.Ship.Spawn(pos, rot, CrashLevel.Bounce, 0, 0); G.Rig.Snap(); }

        IEnumerator Round11B()
        {
            Log("=== Round 11 B: rock textures and land bridges");
            Log($"  photo textures loaded: {WorldLook.PhotoTextures}");
            long mem = 0;
            foreach (var t in Resources.LoadAll<Texture2D>("Textures/Terrain")) { long m = (long)(t.width * t.height * (t.format == TextureFormat.DXT1 || t.format == TextureFormat.BC4 ? .5f : 1f) * 4 / 3); /* block-compressed with mips (Profiler sizes are 0 in a release build) */ mem += m; Log($"    {t.name}: {t.width} x {t.height}, {t.format}, mips {t.mipmapCount}, {m / 1048576f:0.0} MB"); }
            Log($"  texture memory for the six: {mem / 1048576f:0.0} MB");
            foreach (var s in new[] { Seeds.OfTheDay, 777, 4242 }) { BridgeFacts(Biome.Canyons, s); BridgeFacts(Biome.Volcanic, s); }

            yield return StartFreeFlight(Biome.Canyons, 1);
            yield return new WaitForSecondsRealtime(2f);
            var f = G.Ship.Field; var ship = G.Ship;
            Log($"--- Canyons, seed of the day {Seeds.OfTheDay}");
            // 1. low in a canyon, looking along it
            var start = ship.transform.position;
            if (FindNear((x, z) => f.CanyonT(x, z) < .01f && f.CanyonT(x + 60, z) < .2f, out var low, 3000f))
            {
                float e = 10f; var g = new Vector2(f.CanyonT(low.x + e, low.z) - f.CanyonT(low.x - e, low.z), f.CanyonT(low.x, low.z + e) - f.CanyonT(low.x, low.z - e));
                var along = g.sqrMagnitude > 1e-8f ? new Vector3(-g.y, 0, g.x).normalized : Vector3.forward;
                yield return Ready(low); Hold(low + Vector3.up * 25f, Quaternion.LookRotation(along + Vector3.up * .05f)); yield return new WaitForSecondsRealtime(1.5f);
                yield return BeforeAfter("r11b_1_canyon_low");
                // 2. a wall up close: across the canyon until the ground is 40 m up, the ship 22 m from it, facing it
                var acrossV = new Vector3(g.x, 0, g.y).normalized; float q = 0;
                for (q = 6; q < 260; q += 3) { var w = low + acrossV * q; if (f.Height(w.x, w.z) > low.y + 45f) break; }
                var wallAt = low + acrossV * q; var eye = low + acrossV * Mathf.Max(0, q - 22f); eye.y = f.SurfaceHeight(eye.x, eye.z) + 20f;
                Hold(eye, Quaternion.LookRotation(acrossV + Vector3.down * .08f)); yield return new WaitForSecondsRealtime(1.2f);
                Log($"  wall up close: {q:0} m across the canyon, the ship {Vector3.Distance(new Vector3(eye.x, 0, eye.z), new Vector3(wallAt.x, 0, wallAt.z)):0} m from it at {eye.y - f.SurfaceHeight(eye.x, eye.z):0} m over the ground");
                yield return BeforeAfter("r11b_2_wall_close");
                Hold(eye + Vector3.up * 30f, Quaternion.LookRotation(acrossV + Vector3.down * .5f)); yield return new WaitForSecondsRealtime(1f);
                yield return BeforeAfter("r11b_3_wall_foot");
            }
            else Log("  no canyon floor found near the start");
            // 3. a land bridge: ahead, then flown under, then into it
            if (TightestBridge(f, start, out var br))
            {
                var alongB = Quaternion.Euler(0, br.yaw, 0) * Vector3.right; float bottom = br.c.y - br.half.y;
                float floor = f.Height(br.c.x, br.c.z);
                var acrossB = Quaternion.Euler(0, br.yaw, 0) * Vector3.forward; float lo = 1e9f; Vector3 lowAt = br.c;
                for (float q = -br.half.z / 3; q <= br.half.z / 3; q += 4) { var pp = br.c + acrossB * q; float h = f.Height(pp.x, pp.z); if (h < lo) { lo = h; lowAt = pp; } }
                var under = new Vector3(lowAt.x, (lo + bottom) / 2, lowAt.z);
                Log($"  nearest land bridge {Vector3.Distance(start, br.c) / 1000f:0.0} km from the start: {br.half.z * 2:0} m long, {br.half.x * 2:0} m wide, its underside {bottom - lo:0} m over the canyon floor; flying under at {under.y - lo:0} m");
                yield return Ready(br.c);
                // ahead, from 300 m along the canyon (the way into it is the canyon's own line)
                var from = under - alongB * 300f; from.y = Mathf.Max(under.y, f.SurfaceHeight(from.x, from.z) + 25f);
                Hold(from, Quaternion.LookRotation(under - from)); yield return new WaitForSecondsRealtime(1.5f);
                yield return BeforeAfter("r11b_4_bridge_ahead");
                var near = under - alongB * 90f; near.y = under.y;
                Hold(near + Vector3.down * 4f, Quaternion.LookRotation(under - near + Vector3.up * 12f)); yield return new WaitForSecondsRealtime(1f);
                yield return BeforeAfter("r11b_5_bridge_close");
                // under it at 30 % throttle along the canyon, level
                var go = under - alongB * (br.half.x + 60f); ship.Spawn(go, Quaternion.LookRotation(alongB), CrashLevel.Bounce, .35f, 0); G.Rig.Snap();
                int h0 = ship.Hits; float t0 = Time.realtimeSinceStartup; bool pic = false; float minUnder = 1e9f;
                while (Time.realtimeSinceStartup - t0 < 9f)
                {
                    state.leftStick = Vector2.zero; Set();
                    var sp = ship.transform.position; float dAlong = Vector3.Dot(sp - under, alongB);
                    if (Mathf.Abs(dAlong) < br.half.x) minUnder = Mathf.Min(minUnder, bottom - sp.y);
                    if (!pic && dAlong > -br.half.x - 10f) { pic = true; yield return Shot("r11b_6_under_bridge"); }
                    yield return null;
                }
                Log($"  flown under it (level, {ship.Speed * 3.6f:0} km/h): hits {ship.Hits - h0}, closest to its underside {minUnder:0} m");
                // into it at Bounce: a hit
                var into = br.c - alongB * 140f; into.y = br.c.y; ship.Spawn(into, Quaternion.LookRotation(alongB), CrashLevel.Bounce, .5f, 0); G.Rig.Snap();
                h0 = ship.Hits; t0 = Time.realtimeSinceStartup;
                while (Time.realtimeSinceStartup - t0 < 5f && ship.Hits == h0) { state.leftStick = Vector2.zero; Set(); yield return null; }
                Log($"  flown into it at Bounce: hits {ship.Hits - h0} (impact {ship.LastImpact:0} m/s) {(ship.Hits > h0 ? "SOLID OK" : "NO HIT FAIL")}");
                yield return new WaitForSecondsRealtime(.4f); yield return Shot("r11b_7_bridge_hit");
            }
            else Log("  no land bridge within 6 km of the start");
            // 4. frame rates: before / after here, after in each biome
            ship.Spawn(start, Quaternion.identity, CrashLevel.Bounce, .3f, 0); G.Rig.Snap(); yield return Ready(start); yield return new WaitForSecondsRealtime(1f);
            WorldLook.SetPhoto(false); yield return MeasureFps($"Canyons BEFORE (Round 10 look){(SteamDeck.On ? " (Steam Deck setting)" : "")}");
            ship.Spawn(start, Quaternion.identity, CrashLevel.Bounce, .3f, 0); G.Rig.Snap(); yield return new WaitForSecondsRealtime(1f);
            WorldLook.SetPhoto(true); yield return MeasureFps($"Canyons AFTER (photo textures){(SteamDeck.On ? " (Steam Deck setting)" : "")}");
            yield return PauseToTitle();
            foreach (var b in new[] { Biome.Volcanic, Biome.Ice, Biome.Ocean, Biome.Forest, Biome.Crystal, Biome.City })
            {
                yield return StartFreeFlight(b, 1); yield return new WaitForSecondsRealtime(2f);
                var p0 = G.Ship.transform.position;
                yield return Fly(6f); G.Ship.Spawn(p0, Quaternion.identity, CrashLevel.Bounce, .3f, 0); G.Rig.Snap(); yield return new WaitForSecondsRealtime(1f);   // warm up (first frames load)
                WorldLook.SetPhoto(false); yield return MeasureFps($"{Biomes.Name(b)} BEFORE{(SteamDeck.On ? " (Steam Deck setting)" : "")}");
                G.Ship.Spawn(p0, Quaternion.identity, CrashLevel.Bounce, .3f, 0); G.Rig.Snap(); yield return new WaitForSecondsRealtime(1f);
                WorldLook.SetPhoto(true); yield return MeasureFps($"{Biomes.Name(b)} AFTER{(SteamDeck.On ? " (Steam Deck setting)" : "")}");
                yield return Shot($"r11b_8_{Biomes.Keys[(int)b]}_after");
                if (b == Biome.Volcanic && NearestBridge(G.Ship.Field, p0, out var vb))
                {
                    var al = Quaternion.Euler(0, vb.yaw, 0) * Vector3.right; var fr = vb.c - al * 260f; fr.y = vb.c.y - vb.half.y - 15f; fr.y = Mathf.Max(fr.y, G.Ship.Field.SurfaceHeight(fr.x, fr.z) + 20f);
                    yield return Ready(vb.c); Hold(fr, Quaternion.LookRotation(vb.c - fr)); yield return new WaitForSecondsRealtime(1.5f);
                    yield return Shot("r11b_9_volcanic_bridge");
                }
                if (G.Now == GameShell.State.Flying) yield return PauseToTitle();
                else { yield return WaitFor(() => G.Menu.Current != null && G.Menu.Current.Title == "SHIP LOST", 6f); yield return Nav(GamepadButton.DpadDown, 1); yield return Press(GamepadButton.South); yield return WaitFor(() => G.Now == GameShell.State.Title, 30f); }
            }
            Log($"  output level (muted): {TestRun.Report()}");
        }

        IEnumerator Round11C() { yield break; }
    }
}
