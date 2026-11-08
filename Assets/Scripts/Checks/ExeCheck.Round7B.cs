using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

namespace HostileHorizon
{
    // Round 7 Part B check, in the built exe:  HostileHorizon.exe -exeCheck <dir> -exeCheckOnly round7b | round7bperf [-steamDeck 1]
    //  1. the biomes from the seed (all seven over 2000 seeds), the same seed = the same ground, what each new one is made of (lava /
    //     sea share, pads, crystals and shards, stacks and arches);
    //  2. each new biome in Free Flight through the menu: cockpit pictures flying low, the frame rate (PC and the Deck setting), then
    //     Volcanic: low over the lava (HULL HEAT, the computer, the haze) and into it at Bounce (a crash); Crystal Forest: through a
    //     shard (it breaks, no hit) and into a spire at Bounce (a hit); Ocean Islands: a skim (spray, no hit), a dive at Lasting (a hit)
    //     and through an arch;
    //  3. (round7b only) the Campaign on each: jump, launch (Control names the world), the route trail, a landing and pickup at a pilot
    //     (the pad), a turret in sight, the flight music.
    public sealed partial class ExeCheck
    {
        static readonly Biome[] NewBiomes = { Biome.Volcanic, Biome.Crystal, Biome.Ocean };

        void BiomeFacts()
        {
            var counts = new int[Biomes.Count]; for (int s = 1; s <= 2000; s++) counts[(int)Biomes.ForSeed(s)]++;
            Log($"biomes over seeds 1-2000: {string.Join(", ", Enumerable.Range(0, Biomes.Count).Select(i => $"{Biomes.Names[i]} {counts[i]}"))}; seed 4242 twice: {Biomes.ForSeed(4242)} / {Biomes.ForSeed(4242)}");
            foreach (var b in NewBiomes)
            {
                string Sample(CanyonField f) { var r = new System.Random(9); var sb = new System.Text.StringBuilder(); for (int i = 0; i < 400; i++) sb.Append(f.SurfaceHeight((float)r.NextDouble() * 6000 - 3000, (float)r.NextDouble() * 6000 - 3000).ToString("0.000")); return sb.ToString(); }
                var f1 = new CanyonField(777, b); var f2 = new CanyonField(777, b);
                int pads = 0, liquid = 0, n = 0; float lo = 1e9f, hi = -1e9f; var rr = new System.Random(3);
                for (int j = -5; j <= 5; j++) for (int i = -5; i <= 5; i++) if (f1.PadAt(i, j).ok) pads++;
                for (int k = 0; k < 4000; k++) { float x = (float)rr.NextDouble() * 8000 - 4000, z = (float)rr.NextDouble() * 8000 - 4000; float h = f1.Height(x, z); lo = Mathf.Min(lo, h); hi = Mathf.Max(hi, h); n++; if (f1.OverLiquid(x, z)) liquid++; }
                int spires = 0, shards = 0, bigs = 0; for (int j = 0; j < 40; j++) for (int i = 0; i < 40; i++) { var c = f1.CrystalAt(i, j); if (c.ok) { spires++; if (c.height > 100) bigs++; } shards += f1.ShardsAt(i, j).Length; }
                int vents = 0, arches = 0; for (int j = 0; j < 16; j++) for (int i = 0; i < 16; i++) { if (f1.VentAt(i, j).ok) vents++; if (f1.ArchAt(i, j).ok) arches++; }
                Log($"  {Biomes.Name(b)} (seed 777): same seed, same ground {(Sample(f1) == Sample(f2) ? "identical" : "DIFFERENT")}; ground {lo:0} .. {hi:0} m; {(f1.HasLiquid ? $"{(f1.IsLava ? "lava" : "sea")} covers {liquid * 100f / n:0} % (level {f1.Liquid:0} m); " : "")}pads in 11 x 11 pad cells {pads}" +
                    (b == Biome.Crystal ? $"; in 40 x 40 cells (2.6 km): {spires} spires ({bigs} over 100 m), {shards} shards" : b == Biome.Volcanic ? $"; vents in 16 x 16 prop cells (4 km): {vents}" : $"; arches in 16 x 16 prop cells (4 km): {arches}"));
            }
        }

        // a point of the given kind near the ship (spiral search on a 40 m grid)
        bool FindNear(Func<float, float, bool> ok, out Vector3 at, float reach = 4000f)
        {
            var p0 = G.Ship.transform.position; at = default;
            for (float r = 0; r < reach; r += 40f)
                for (int k = 0, n = Mathf.Max(1, (int)(r / 40f) * 6); k < n; k++)
                {
                    float a = k * Mathf.PI * 2 / n, x = p0.x + Mathf.Cos(a) * r, z = p0.z + Mathf.Sin(a) * r;
                    if (ok(x, z)) { at = new Vector3(x, G.Ship.Field.SurfaceHeight(x, z), z); return true; }
                }
            return false;
        }

        IEnumerator Ready(Vector3 at)
        {
            G.Terrain.SetFocus(at); float wait = Time.realtimeSinceStartup + 12f;
            while (!G.Terrain.ReadyNear(1500f) && Time.realtimeSinceStartup < wait) yield return null;
        }

        IEnumerator NewBiomeFlight(Biome b, bool perfOnly)
        {
            string k = Biomes.Keys[(int)b];
            Log($"--- {Biomes.Name(b)} (Free Flight, seed of the day {Seeds.OfTheDay}, Bounce)");
            yield return StartFreeFlight(b, 1);
            yield return new WaitForSecondsRealtime(2f);
            var f = G.Ship.Field; var s = G.Ship;
            Log($"  world: biome {f.Biome}; {Music1()}; liquid {(f.HasLiquid ? (f.IsLava ? "lava" : "sea") : "none")} ({(G.Sea.gameObject.activeSelf ? "drawn" : "not drawn")}); props: {G.Props.CrystalsDrawn} crystals drawn, {G.Props.VentsNear.Count} vents, fliers {(G.Birds.gameObject.activeSelf ? "on" : "off")}");
            yield return Shot($"r7b_{k}_1_start_cockpit");
            int hits = s.Hits;
            yield return MeasureFps($"{Biomes.Name(b)} flying low{(SteamDeck.On ? " (Steam Deck setting)" : "")}");
            Log($"  hits while flying low: {s.Hits - hits}; crystals drawn {G.Props.CrystalsDrawn}; {(f.IsLava ? $"hull heat seen: {(Voices.Log.Any(l => l.Contains("hull_heat")) ? "yes" : "no")}" : "")}");
            yield return Shot($"r7b_{k}_2_low_cockpit");
            G.Rig.Current = CameraRig.Mode.Chase; G.Rig.Snap(); yield return Fly(2f);
            yield return Shot($"r7b_{k}_3_chase");
            G.Rig.Current = CameraRig.Mode.Cockpit; G.Rig.Snap();
            if (!perfOnly)
            {
                if (b == Biome.Volcanic) yield return LavaTests(f);
                if (b == Biome.Crystal) yield return CrystalTests(f);
                if (b == Biome.Ocean) yield return OceanTests(f);
            }
            if (G.Now == GameShell.State.Flying || G.Now == GameShell.State.Paused) yield return PauseToTitle();
            else { yield return WaitFor(() => G.Menu.Current != null && G.Menu.Current.Title == "SHIP LOST", 6f); yield return Nav(GamepadButton.DpadDown, 1); yield return Press(GamepadButton.South); yield return WaitFor(() => G.Now == GameShell.State.Title, 30f); }
        }

        IEnumerator LavaTests(CanyonField f)
        {
            var s = G.Ship;
            if (!FindNear((x, z) => f.OverLiquid(x, z) && f.OverLiquid(x + 60, z) && f.OverLiquid(x - 60, z) && f.OverLiquid(x, z + 60) && f.OverLiquid(x, z - 60), out var lava)) { Log("  no lava lake found within 4 km"); yield break; }
            Log($"  a lava lake at {lava:F0} ({Flat(lava - s.transform.position).magnitude:0} m from the ship)");
            yield return Ready(lava);
            // low over it: hovering 18 m up
            int v0 = Voices.Log.Count, p0 = PlayLog.Lines.Count;
            s.ResetTo(lava + Vector3.up * 18f, 30f); G.Rig.Snap();
            float t0 = Time.realtimeSinceStartup; while (Time.realtimeSinceStartup - t0 < 4f) { state.leftStick = Vector2.zero; Set(); yield return null; }
            Log($"  hovering {s.AboveGround:0} m over the lava: HULL HEAT {(G.HullHot ? "ON" : "off")}, display \"{G.Displays.HeatWarning}\", the computer: {string.Join(" | ", Voices.Log.Skip(v0).Where(l => l.Contains("hull_heat")).Select(l => l.Substring(l.IndexOf("voice"))).Take(1))}; [Play]: {string.Join(" | ", PlayLog.Lines.Skip(p0).Where(l => l.Contains("heat")).Take(1))}; crashed {s.Crashed}");
            yield return Shot("r7b_volcanic_4_low_over_lava");
            // into it, at Bounce: a crash anyway
            s.ResetTo(lava + Vector3.up * 40f, 30f); G.Rig.Snap(); yield return null;
            yield return SetMode(FlightMode.Flight);
            int h0 = s.Hits; t0 = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - t0 < 8f && !s.Crashed) { state.leftStick = new Vector2(0, 1f); Set(); yield return null; }   // nose down
            state.leftStick = Vector2.zero; Set();
            Log($"  flown into the lava at Bounce: crashed {(s.Crashed ? $"YES ({s.CrashCause})" : "NO")}, state {G.Now}, hits {s.Hits - h0}");
            yield return new WaitForSecondsRealtime(.8f);
            yield return Shot("r7b_volcanic_5_into_lava");
        }

        IEnumerator CrystalTests(CanyonField f)
        {
            var s = G.Ship; var p = s.transform.position; CanyonField.Shard shard = default; CanyonField.Crystal spire = default; bool fs = false, fp = false;
            int ti0 = Mathf.FloorToInt(p.x / CanyonField.TreeCell), tj0 = Mathf.FloorToInt(p.z / CanyonField.TreeCell);
            for (int r = 0; r < 20 && !(fs && fp); r++)
                for (int dj = -r; dj <= r; dj++)
                    for (int di = -r; di <= r; di++)
                    {
                        if (Mathf.Max(Mathf.Abs(di), Mathf.Abs(dj)) != r) continue;
                        if (!fs) foreach (var sh in f.ShardsAt(ti0 + di, tj0 + dj)) if (sh.height > 12f) { shard = sh; fs = true; break; }
                        var c = f.CrystalAt(ti0 + di, tj0 + dj); if (!fp && c.ok && c.height > 60f && c.radius > 5f) { spire = c; fp = true; }
                    }
            if (fs)
            {
                var mid = shard.basePos + shard.axis * shard.height * .35f; var dir = Vector3.ProjectOnPlane(Vector3.forward, Vector3.up);
                yield return Ready(mid);
                s.ResetTo(mid - dir * 90f, 0); G.Rig.Snap(); yield return SetMode(FlightMode.Flight); s.transform.rotation = Quaternion.LookRotation(mid - s.transform.position);
                int h0 = s.Hits, b0 = G.ShardsBroken; float t0 = Time.realtimeSinceStartup; bool pic = false;
                while (Time.realtimeSinceStartup - t0 < 5f) { state.leftStick = Vector2.zero; Set(); if (!pic && G.ShardsBroken > b0) { pic = true; yield return new WaitForSecondsRealtime(.15f); yield return Shot("r7b_crystal_4_shard_breaks"); } yield return null; }
                Log($"  flown through a shard ({shard.height:0} m tall, {shard.radius * 2:0.0} m thick): broken {G.ShardsBroken - b0} (drawn no more: {(f.Broken.ContainsKey(shard.id) ? "yes" : "no")}), hits {s.Hits - h0}, crashed {s.Crashed}; sounds: {string.Join(", ", Sfx.Played.Where(l => l.Contains("crystal")).Take(1))}");
            }
            else Log("  no shard found");
            if (fp && !s.Crashed)
            {
                var mid = spire.basePos + spire.axis * Mathf.Min(40f, spire.height * .3f);
                yield return Ready(mid);
                s.ResetTo(mid - Vector3.forward * 120f, 0); G.Rig.Snap(); yield return SetMode(FlightMode.Flight); s.transform.rotation = Quaternion.LookRotation(mid - s.transform.position);
                yield return new WaitForSecondsRealtime(.2f); yield return Shot("r7b_crystal_5_spire_ahead");
                int h0 = s.Hits; float t0 = Time.realtimeSinceStartup;
                while (Time.realtimeSinceStartup - t0 < 6f && s.Hits == h0) { state.leftStick = Vector2.zero; Set(); yield return null; }
                Log($"  flown into a spire ({spire.height:0} m tall, {spire.radius * 2:0} m wide) at Bounce: hits {s.Hits - h0} (impact {s.LastImpact:0.0} m/s), crashed {s.Crashed} (big spires don't break)");
            }
        }

        IEnumerator OceanTests(CanyonField f)
        {
            var s = G.Ship;
            if (!FindNear((x, z) => f.OverLiquid(x, z) && f.OverLiquid(x + 100, z) && f.OverLiquid(x + 200, z) && f.OverLiquid(x + 100, z + 40) && f.OverLiquid(x + 100, z - 40), out var sea, 8000f)) { Log("  no open water found"); yield break; }
            yield return Ready(sea);
            // a skim: level flight 4 m up, a gentle sink onto the water
            s.Spawn(sea + Vector3.up * 2.5f, Quaternion.Euler(4f, 90f, 0), CrashLevel.Bounce, .4f, 0); G.Rig.Snap();   // level flight 2.5 m up, nose 4 degrees down
            int h0 = s.Hits, w0 = G.WaterTouches; float t0 = Time.realtimeSinceStartup, lowest = 99; int sprays = 0; float d0 = s.Damage; bool pic = false;
            string hitWhere = "";
            while (Time.realtimeSinceStartup - t0 < 3f)
            {
                state.leftStick = Vector2.zero; Set(); lowest = Mathf.Min(lowest, s.transform.position.y);
                if (s.Hits > h0 && hitWhere == "") { var q = s.transform.position; hitWhere = $" (the hit: {s.LastImpact:0.0} m/s into it at {q:F1}, ground there {f.GroundHeight(q.x, q.z):0.0} m, over the sea {f.OverLiquid(q.x, q.z)}, vertical speed {s.Velocity.y:0.0} m/s)"; }
                if (!pic && G.WaterTouches > w0) { pic = true; yield return Shot("r7b_ocean_4_skim"); }
                yield return null;
            }
            sprays = G.WaterTouches - w0;
            Log($"  skimming the sea: lowest {lowest:0.0} m (sea 0), touches of the water {sprays} (spray each), hits {s.Hits - h0}{hitWhere}, crashed {s.Crashed}, damage {d0:0} -> {s.Damage:0} %, speed {s.Speed * 3.6f:0} km/h");
            // a dive at Lasting damage: a hit
            var lvl = s.Level; s.Spawn(sea + Vector3.up * 60f, Quaternion.Euler(55f, 90f, 0), CrashLevel.Lasting, .6f, 0); G.Rig.Snap();
            h0 = s.Hits; t0 = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - t0 < 5f && s.Hits == h0 && !s.Crashed) { state.leftStick = new Vector2(0, .5f); Set(); yield return null; }
            state.leftStick = Vector2.zero; Set();
            Log($"  dived into the sea at Lasting damage: hits {s.Hits - h0} (impact {s.LastImpact:0} m/s), damage {s.Damage:0} %, crashed {s.Crashed} {(s.Crashed ? $"({s.CrashCause})" : "")}");
            yield return new WaitForSecondsRealtime(.6f); yield return Shot("r7b_ocean_5_dive");
            if (s.Crashed) yield break;
            s.Spawn(s.transform.position, s.transform.rotation, lvl, .3f, 0);
            // through an arch
            var p = s.transform.position; CanyonField.Box arch = default; bool found = false;
            int ci = Mathf.FloorToInt(p.x / CanyonField.PropCell), cj = Mathf.FloorToInt(p.z / CanyonField.PropCell);
            for (int r = 0; r < 14 && !found; r++) for (int dj = -r; dj <= r && !found; dj++) for (int di = -r; di <= r && !found; di++) foreach (var bx in f.BoxesAt(ci + di, cj + dj)) if (bx.kind == 2) { arch = bx; found = true; break; }
            if (!found) { Log("  no arch within 3.5 km"); yield break; }
            var across = Quaternion.Euler(0, arch.yaw, 0) * Vector3.right; float under = arch.c.y - arch.half.y;
            var from = arch.c - across * 200f; from.y = under * .45f;
            yield return Ready(arch.c);
            s.Spawn(from, Quaternion.LookRotation(across), CrashLevel.Bounce, .3f, 0); G.Rig.Snap();
            yield return new WaitForSecondsRealtime(1.5f); yield return Shot("r7b_ocean_6_arch_ahead");
            h0 = s.Hits; t0 = Time.realtimeSinceStartup; while (Time.realtimeSinceStartup - t0 < 4f) { state.leftStick = Vector2.zero; Set(); yield return null; }
            Log($"  an arch: slab {arch.half.z * 2:0} m long, {under:0} m over the sea; flown through underneath at {from.y:0} m: hits {s.Hits - h0}, crashed {s.Crashed}");
        }

        IEnumerator NewBiomeSortie(Biome b)
        {
            int seed = SeedFor(b); string k = Biomes.Keys[(int)b];
            Log($"--- Campaign: {Biomes.Name(b)}: the check jumps to seed {seed}");
            G.JumpTo(seed); yield return WaitPhase(() => G.Phase == "docked" && G.Planet.seed == seed, 40f, "the jump");
            Log($"  arrived: {G.Planet.name}, biome \"{G.Planet.biome}\"; Control: {string.Join(" | ", Voices.Log.Where(l => l.Contains("brief_" + k)).Take(1).Select(l => l.Substring(l.IndexOf("voice"))))}; enemies {G.Foes.All.Count(e => e.Kind == EnemyKind.Turret)} turrets, {G.Foes.All.Count(e => e.Kind == EnemyKind.Saucer)} saucers");
            G.CheckSetup(3, new[] { 0, 0, 0 }, new[] { 0, 0, 0 });
            int v0 = Voices.Log.Count;
            yield return LaunchFromHangar();
            yield return new WaitForSecondsRealtime(1f);
            Log($"  launched: {Music1()}; Control: {string.Join(" | ", Voices.Log.Skip(v0).Where(l => l.Contains("ctl_")).Select(l => l.Substring(l.IndexOf("voice") + 6)))}");
            yield return WaitFor(() => G.Route.Path.Count > 1, 15f);
            Log($"  route trail: target {G.RouteTargetName}; {(G.Route.Path.Count > 1 ? PathReport(G.Route.Path) : "no path")}; over the {(G.Ship.Field.HasLiquid ? (G.Ship.Field.IsLava ? "lava" : "sea") : "ground")}: {G.Route.Path.Count(q => G.Ship.Field.OverLiquid(q.x, q.z)) * 100 / Math.Max(1, G.Route.Path.Count)} % of its points");
            yield return Shot($"r7b_{k}_c1_trail");
            var tu = NearestAlive(EnemyKind.Turret);
            if (tu != null) { yield return Sight(tu, 900f, $"r7b_{k}_c2_turret"); Log($"  turret {tu.Index + 1}: on ground {tu.Pos.y - G.Ship.Field.GroundHeight(tu.Pos.x, tu.Pos.z):0.0} m over the drawn ground, {(G.Ship.Field.OverLiquid(tu.Pos.x, tu.Pos.z) ? "OVER THE LIQUID" : "on rock")}, {tu.Pos.y:0} m up"); }
            var p = G.Pilots.Where(x => x.OnFoot).OrderBy(x => x.Index).FirstOrDefault();
            if (p == null) { Log("  no pilot"); yield break; }
            var f = G.Ship.Field; f.NearestPad(p.LandingSpot, out var pad);
            Log($"  pilot {p.Index + 1}: pad at {p.LandingSpot:F0}, {p.LandingSpot.y - (f.HasLiquid ? f.Liquid : 0):0} m over the {(f.HasLiquid ? (f.IsLava ? "lava" : "sea") : "zero")} level; {Flat(p.LandingSpot - G.Ship.transform.position).magnitude:0} m away");
            yield return LandFor(p);
            Log($"  landed {G.Ship.Landed} {Vector3.Distance(G.Ship.transform.position, p.LandingSpot):0} m from the pad's centre; {Music1()}");
            yield return Shot($"r7b_{k}_c3_landed_at_pilot");
            yield return WaitPhase(() => p.AtDoor, 90f, "the knock");
            yield return Press(GamepadButton.East);
            yield return WaitPhase(() => !p.OnFoot, 10f, "boarding");
            yield return new WaitForSecondsRealtime(2f);
            Log($"  pilot {p.Index + 1} aboard: cabin {G.CabinCount}; the mayday heard: {string.Join(" | ", Voices.Log.Skip(v0).Where(l => l.Contains("mayday")).Take(1).Select(l => l.Substring(l.IndexOf("voice") + 6)))}");
            yield return BackToFlight(); yield return Fly(6f);
            Log($"  flying again: {Music1()}");
            yield return HomeByRecall();
        }

        IEnumerator Round7B()
        {
            bool perfOnly = CheckRun.Only == "round7bperf", freeOnly = CheckRun.Only == "round7bfree";
            BiomeFacts();
            var pick = LauncherBridge.Argument("-r7biomes");   // e.g. -r7biomes Ocean
            foreach (var b in NewBiomes) if (string.IsNullOrEmpty(pick) || pick.Contains(b.ToString())) yield return NewBiomeFlight(b, perfOnly);
            GameSettings.S.freeBiome = 0;
            if (perfOnly || freeOnly) yield break;
            Career.CheckSystemSeed = 7272;
            GameSettings.S.difficulty.crashLevel = 1;
            yield return ToCampaignMenu();
            yield return Nav(GamepadButton.DpadDown, 1); yield return Press(GamepadButton.South);   // New career
            yield return WaitPhase(() => G.Now == GameShell.State.Docked && G.Phase == "docked", 60f, "the hangar");
            Log($"new career: first planet {G.Planet.name} ({G.Planet.biome}: must be canyons)");
            foreach (var b in NewBiomes) yield return NewBiomeSortie(b);
            Career.CheckSystemSeed = 0;
            Log("--- music (every change)"); foreach (var l in Music.Log.Where(x => x.Contains("volcanic") || x.Contains("crystal") || x.Contains("ocean"))) Log("  " + l);
            Log($"--- [Play] lines: {PlayLog.Lines.Count}; the new biomes' lines: {string.Join(" | ", PlayLog.Lines.Where(l => l.Contains("Volcanic") || l.Contains("Crystal") || l.Contains("Ocean") || l.Contains("heat") || l.Contains("shard")).Take(20))}");
        }
    }
}
