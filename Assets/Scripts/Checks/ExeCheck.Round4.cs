using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

namespace HostileHorizon
{
    // Round 4 check, in the built exe:  HostileHorizon.exe -exeCheck <dir> -exeCheckOnly round4 | round4perf
    //  1. biomes from the seed: the spread over 2000 seeds, the same seed = the same biome and the same ground, pads per biome
    //     (rooftops in the Dead City);
    //  2. each biome in Free Flight through the menu (Planet type): flying low (fps, hits), cockpit and chase pictures, trees drawn,
    //     a trunk / bridge hit (solid), the forest canopy brush, an Ice blizzard;
    //  3. (round4 only) the Campaign: a new career's first planet is Canyons; then a Giant Forest, a Dead City and an Ice planet
    //     (the check jumps there): launch, the route trail, a landing at a pilot (clearing / rooftop / ice pad), the run, the knock,
    //     the door voice, B, aboard, docking; on the Dead City a Mimic first (its radio call and door voice, then the exhaust);
    //  4. the music by state, the stingers and the voice lines in order with their subtitles (logs: nothing is heard, the run is
    //     muted); the Mimic's voice sign over many planets (only some of the time).
    public sealed partial class ExeCheck
    {
        IEnumerator StartFreeFlight(Biome b, int crashLevel)
        {
            yield return WaitFor(() => G.Now == GameShell.State.Title && G.Menu.Current != null, 30f);
            G.Menu.Current.Selected = 0; yield return Press(GamepadButton.South);   // Free Flight
            yield return new WaitForSecondsRealtime(.3f);
            G.Menu.Current.Selected = Row("Planet type");
            for (int k = 0; k < Biomes.Count + 2 && GameSettings.S.freeBiome != (int)b + 1; k++) yield return Press(GamepadButton.DpadRight);
            G.Menu.Current.Selected = Row("Crash level");
            for (int k = 0; k < 6 && GameSettings.S.crashLevel != crashLevel; k++) yield return Press(GamepadButton.DpadRight);
            G.Menu.Current.Selected = Row("Start flight"); yield return Press(GamepadButton.South);
            yield return WaitFor(() => G.Now == GameShell.State.Flying, 30f);
        }

        string Music1() => $"music \"{(G.Music.Playing == "" ? "(silence)" : G.Music.Playing)}\" want \"{G.Music.Want}\" gain {G.Music.PlayingVolume:0.00}{(G.Music.Tension ? " +tension" : "")}";

        IEnumerator BiomeFlight(Biome b)
        {
            Log($"--- {Biomes.Name(b)} (Free Flight, seed of the day {Seeds.OfTheDay}, Bounce)");
            yield return StartFreeFlight(b, 1);
            yield return new WaitForSecondsRealtime(2f);
            var f = G.Ship.Field;
            Log($"  world: biome {f.Biome}; {Music1()}; weather particles on; props: {G.Props.TreesDrawn} trees drawn ({G.Props.NearTrees} near, detailed)");
            yield return Shot($"b_{Biomes.Keys[(int)b]}_1_start_cockpit");
            int hits = G.Ship.Hits;
            yield return MeasureFps($"{Biomes.Name(b)} flying low{(SteamDeck.On ? " (Steam Deck setting)" : "")}");
            Log($"  hits while flying low: {G.Ship.Hits - hits}; trees drawn {G.Props.TreesDrawn} ({G.Props.NearTrees} near)");
            yield return Shot($"b_{Biomes.Keys[(int)b]}_2_low_cockpit");
            G.Rig.Current = CameraRig.Mode.Chase; G.Rig.Snap(); yield return Fly(2f);
            yield return Shot($"b_{Biomes.Keys[(int)b]}_3_chase");
            G.Rig.Current = CameraRig.Mode.Cockpit; G.Rig.Snap();
            if (b == Biome.Forest) yield return ForestTests(f);
            if (b == Biome.City || b == Biome.Ice) yield return BridgeTest(f, b);
            if (b == Biome.Ice)
            {
                G.Weather.StartBlizzard(); float t0 = Time.realtimeSinceStartup;
                yield return Fly(9f);
                Log($"  blizzard: weather {G.Weather.Blizzard:0.00} after {Time.realtimeSinceStartup - t0:0} s (haze end {Shader.GetGlobalVector("_HHFog").w:0} m); the computer: {string.Join(" | ", Voices.Log.Where(l => l.Contains("whiteout")).Take(1))}");
                yield return Shot("b_ice_4_blizzard");
                G.Weather.EndWeather();
            }
            yield return PauseToTitle();
        }

        // a trunk, flown into at 40 m/s (Bounce: a hit, not a crash), and the canopy brushed through
        IEnumerator ForestTests(CanyonField f)
        {
            var s = G.Ship; var p = s.transform.position; CanyonField.Tree tree = default; bool found = false;
            for (int r = 0; r < 12 && !found; r++)
                for (int dj = -r; dj <= r && !found; dj++)
                    for (int di = -r; di <= r && !found; di++)
                    { var t = f.TreeAt(Mathf.FloorToInt(p.x / 64f) + di, Mathf.FloorToInt(p.z / 64f) + dj); if (t.ok) { tree = t; found = true; } }
            if (!found) { Log("  no tree found near the ship"); yield break; }
            Log($"  a tree: trunk {tree.radius * 2:0} m wide, {tree.height:0} m tall, crown {tree.crownSize.x * 2:0} x {tree.crownSize.y * 2:0} m at {tree.crown.y - tree.basePos.y:0} m");
            // fly at the trunk, 25 m up
            float y = tree.basePos.y + 25f; var from = new Vector3(tree.basePos.x, y, tree.basePos.z - 120f);
            s.Spawn(from, 0, CrashLevel.Bounce); G.Rig.Snap();
            int hits = s.Hits; float end = Time.realtimeSinceStartup + 6f;
            while (Time.realtimeSinceStartup < end && s.Hits == hits) { state.leftStick = Vector2.zero; Set(); yield return null; }
            var d = new Vector2(s.transform.position.x - tree.basePos.x, s.transform.position.z - tree.basePos.z).magnitude;
            Log($"  flown at the trunk: hits {s.Hits - hits} (impact {s.LastImpact:0.0} m/s), crashed {s.Crashed}; after: {d:0.0} m from the trunk's axis (radius there {CanyonField.TrunkRadius(tree, y):0.0} m)");
            yield return Shot("b_forest_4_trunk_hit");
            // through the crown
            int leavesBefore = Sfx.Played.Count(l => l.Contains("rustle"));
            s.Spawn(tree.crown + new Vector3(tree.crownSize.x * .5f, 0, -(tree.crownSize.z + 40f)), 0, CrashLevel.Bounce); G.Rig.Snap();   // beside the trunk
            float e2 = Time.realtimeSinceStartup + 4f; float inside = 0;
            while (Time.realtimeSinceStartup < e2) { state.leftStick = Vector2.zero; Set(); inside = Mathf.Max(inside, f.InCanopy(s.transform.position)); yield return null; }
            Log($"  flown through a crown: deepest {inside:0.00} (1 = its middle), rustles {Sfx.Played.Count(l => l.Contains("rustle")) - leavesBefore}, crashed {s.Crashed}, hits from leaves {(s.Hits - hits > 1 ? "some (trunk?)" : "none")}");
            yield return Shot("b_forest_5_through_crown");
        }

        IEnumerator BridgeTest(CanyonField f, Biome b)
        {
            var s = G.Ship; var p = s.transform.position; CanyonField.Box box = default; bool found = false; int count = 0;
            int ci = Mathf.FloorToInt(p.x / CanyonField.PropCell), cj = Mathf.FloorToInt(p.z / CanyonField.PropCell);
            for (int r = 0; r < 14; r++)
                for (int dj = -r; dj <= r; dj++)
                    for (int di = -r; di <= r; di++)
                    {
                        if (Mathf.Max(Mathf.Abs(di), Mathf.Abs(dj)) != r) continue;
                        foreach (var x in f.BoxesAt(ci + di, cj + dj)) { count++; if (!found) { box = x; found = true; } }
                    }
            Log($"  {(b == Biome.City ? "sky bridges" : "snow bridges (ice caves)")} within ~3.5 km: {count}");
            if (!found) yield break;
            Log($"  nearest: {box.half.z * 2:0} m long across, {box.half.x * 2:0} m wide, {box.half.y * 2:0} m thick, bottom {box.c.y - box.half.y - f.Height(box.c.x, box.c.z):0} m over the ground under it");
            // under it (an ice cave) or into it (a bridge's side)
            var dir = Quaternion.Euler(0, box.yaw, 0) * Vector3.right;
            float yUnder = box.c.y - box.half.y - (b == Biome.Ice ? 30f : -box.half.y);
            var from = box.c - dir * 160f; from.y = yUnder;
            s.Spawn(from, Quaternion.LookRotation(dir).eulerAngles.y, CrashLevel.Bounce); G.Rig.Snap();
            int hits = s.Hits; float end = Time.realtimeSinceStartup + 5f;
            yield return new WaitForSecondsRealtime(1.6f);
            yield return Shot(b == Biome.Ice ? "b_ice_5_under_snow_bridge" : "b_city_4_at_sky_bridge");
            while (Time.realtimeSinceStartup < end) { state.leftStick = Vector2.zero; Set(); yield return null; }
            Log($"  {(b == Biome.Ice ? "flown under the snow bridge (ice cave)" : "flown into a sky bridge")}: hits {s.Hits - hits}, crashed {s.Crashed}");
        }

        // the Campaign in one biome: jump there, launch, trail, land at a pilot, pick up, dock
        IEnumerator BiomeSortie(Biome b, bool mimicFirst)
        {
            int seed = 0;
            for (int s0 = 101; s0 < 5000 && seed == 0; s0++)
                if (Biomes.ForSeed(s0) == b && !Career.C.planets.Exists(p => p.seed == s0))
                {
                    if (b != Biome.City) { seed = s0; break; }
                    // a Dead City planet whose first pilot is on a roof
                    var f = new CanyonField(s0, Biome.City); var sites = PlanetSites.Find(f, 3, 0);
                    if (sites.Count > 0 && f.NearestPad(sites[0].landing, out var pad) && pad.roof && Vector2.Distance(new Vector2(pad.x, pad.z), new Vector2(sites[0].landing.x, sites[0].landing.z)) < 1f) seed = s0;
                }
            Log($"--- Campaign: {Biomes.Name(b)}: the check jumps to seed {seed} (as the Planets list would)");
            G.JumpTo(seed);
            yield return WaitPhase(() => G.Phase == "docked" && G.Planet.seed == seed, 40f, "the jump");
            Log($"  arrived: {G.Planet.name}, biome \"{G.Planet.biome}\" (from the seed: {Biomes.ForSeed(seed)}); hangar: \"{G.Menu.Current.Body().Split('\n').FirstOrDefault(l => l.Contains("seed")) ?? ""}\"; {Music1()}");
            yield return Shot($"c_{Biomes.Keys[(int)b]}_0_hangar");
            G.CheckSetup(3, mimicFirst ? new[] { (int)AlienType.Mimic, 0, 0 } : new[] { 0, 0, 0 }, mimicFirst ? new[] { (int)AlienType.Mimic, 0, 0 } : new[] { 0, 0, 0 });
            int v0 = Voices.Log.Count, m0 = Music.Log.Count;
            G.Menu.Current.Selected = 0; yield return Press(GamepadButton.South);   // Launch
            yield return WaitPhase(() => G.Now == GameShell.State.Flying, 40f, "flight after launch");
            yield return new WaitForSecondsRealtime(1f);
            Log($"  launched: {Music1()}; voices so far: {string.Join(" | ", Voices.Log.Skip(v0).Select(l => l.Substring(l.IndexOf("voice ") + 6, Math.Min(18, l.Length - l.IndexOf("voice ") - 6))))}");
            yield return WaitFor(() => G.Route.Path.Count > 1, 15f);
            Log($"  route trail: target {G.RouteTargetName}; {(G.Route.Path.Count > 1 ? PathReport(G.Route.Path) : "no path")}");
            yield return Shot($"c_{Biomes.Keys[(int)b]}_1_after_launch_trail");
            if (mimicFirst)
            {
                var m = G.Pilots.Find(x => x.Index == 0);
                Log($"  pilot 1 is a Mimic; its voice sign this time: {(G.MimicVoiceSign(m) ? "ON" : "off")} (shadow tell shown)");
                yield return LandFor(m);
                yield return WaitPhase(() => m.AtDoor, 90f, "the Mimic at the door");
                yield return WaitFor(() => Voices.Log.Skip(v0).Any(l => l.Contains("door")), 15f);
                yield return Shot("c_city_2_mimic_at_door");
                Log($"  the Mimic's lines: {string.Join(" | ", Voices.Log.Skip(v0).Where(l => l.Contains(" p")).Select(l => l.Substring(l.IndexOf("voice ") + 6)))}");
                yield return Press(GamepadButton.South);   // engines on: the exhaust
                yield return new WaitForSecondsRealtime(4f);
                Log($"  engines started on it: fate {(PilotFate)G.Planet.pilots[0]}; Control: {string.Join(" | ", Voices.Log.Skip(v0).Where(l => l.Contains("ctl_alien")).Take(1))}");
                yield return BackToFlight();
            }
            var p2 = G.Pilots.Where(x => x.OnFoot && G.Planet.alien[x.Index] == 0).OrderBy(x => x.Index).FirstOrDefault();
            if (p2 == null) { Log("  no real pilot waiting"); yield break; }
            G.Ship.Field.NearestPad(p2.LandingSpot, out var pp);
            Log($"  pilot {p2.Index + 1}: pad at {p2.LandingSpot:F0} ({(pp.roof ? "a ROOFTOP" : b == Biome.Forest ? "a forest clearing" : b == Biome.Ice ? "an ice pad" : "the ground")}, {p2.LandingSpot.y - G.Ship.Field.Height(p2.LandingSpot.x + 60, p2.LandingSpot.z):+0;-0} m vs 60 m east)");
            int vp = Voices.Log.Count;
            yield return LandFor(p2);
            Log($"  landed {G.Ship.Landed} {Vector3.Distance(G.Ship.transform.position, p2.LandingSpot):0} m from the pad's centre; {Music1()}");
            yield return Shot($"c_{Biomes.Keys[(int)b]}_3_landed_at_pilot");
            yield return WaitPhase(() => p2.AtDoor, 90f, "the knock");
            yield return WaitFor(() => Voices.Log.Skip(vp).Any(l => l.Contains("door")), 12f);
            yield return Shot($"c_{Biomes.Keys[(int)b]}_4_pilot_at_door_subtitle");
            yield return Press(GamepadButton.East);                                  // airlock
            yield return WaitPhase(() => !p2.OnFoot, 10f, "boarding");
            yield return new WaitForSecondsRealtime(3f);
            Log($"  pilot {p2.Index + 1} aboard; the lines since landing: {string.Join(" | ", Voices.Log.Skip(vp).Select(l => l.Substring(l.IndexOf("voice ") + 6)))}");
            // home: climb and dock
            if (!G.Ship.EnginesOn) yield return Press(GamepadButton.South);
            state.rightTrigger = 1; Set();
            yield return WaitFor(() => G.Now != GameShell.State.Flying || G.Ship.Altitude > Tuning.T.dockingHeight + 30f, 150f);
            state.rightTrigger = 0; Set();
            yield return WaitPhase(() => G.Phase == "docked", 150f, "docked");
            yield return new WaitForSecondsRealtime(2f);
            Log($"  docked: {Music1()}; debrief \"{G.Debrief.Replace("\n", " | ")}\"");
            Log($"  music changes this sortie: {string.Join(" | ", Music.Log.Skip(m0))}");
        }

        IEnumerator Round4()
        {
            bool perfOnly = CheckRun.Only == "round4perf";
            StartCoroutine(Progress());
            yield return WaitFor(() => G.Now == GameShell.State.Title, 60f);
            yield return new WaitForSecondsRealtime(2f);
            Log($"title: {G.Menu.Current.Body()}; {Music1()}; music folder {G.Music.Folder}");
            yield return Shot("a_title");

            // ---- 1. biomes from the seed
            var counts = new int[4]; for (int s = 1; s <= 2000; s++) counts[(int)Biomes.ForSeed(s)]++;
            Log($"biomes over seeds 1-2000: " + string.Join(", ", Enumerable.Range(0, 4).Select(i => $"{Biomes.Names[i]} {counts[i]}")) + $"; today ({Seeds.OfTheDay}): {Biomes.ForSeed(Seeds.OfTheDay)}; again: {Biomes.ForSeed(Seeds.OfTheDay)}");
            foreach (Biome b in Enum.GetValues(typeof(Biome)))
            {
                string Sample(CanyonField f) { var r = new System.Random(9); var sb = new System.Text.StringBuilder(); for (int i = 0; i < 400; i++) sb.Append(f.Height((float)r.NextDouble() * 6000 - 3000, (float)r.NextDouble() * 6000 - 3000).ToString("0.000")); return sb.ToString(); }
                var f1 = new CanyonField(777, b); var f2 = new CanyonField(777, b);
                int pads = 0, roofs = 0; float lo = float.MaxValue, hi = float.MinValue;
                for (int j = -5; j <= 5; j++) for (int i = -5; i <= 5; i++) { var pd = f1.PadAt(i, j); if (pd.ok) { pads++; if (pd.roof) roofs++; } }
                var rr = new System.Random(3); for (int k = 0; k < 3000; k++) { float h = f1.Height((float)rr.NextDouble() * 8000 - 4000, (float)rr.NextDouble() * 8000 - 4000); lo = Mathf.Min(lo, h); hi = Mathf.Max(hi, h); }
                int trees = 0; for (int j = 0; j < 40; j++) for (int i = 0; i < 40; i++) if (f1.TreeAt(i, j).ok) trees++;
                int boxes = 0; for (int j = 0; j < 12; j++) for (int i = 0; i < 12; i++) boxes += f1.BoxesAt(i, j).Length;
                Log($"  {Biomes.Name(b)} (seed 777): same seed, same ground: {(Sample(f1) == Sample(f2) ? "identical" : "DIFFERENT")}; heights {lo:0} .. {hi:0} m; pads in 11 x 11 pad cells: {pads}{(b == Biome.City ? $" ({roofs} on roofs)" : "")}; trees in 40 x 40 tree cells: {trees}; bridges in 12 x 12 prop cells: {boxes}");
            }
            // the Mimic's voice sign: some of the time (tellReliability), over 400 planets
            int on = 0; for (int s = 1; s <= 400; s++) { var r = new SeedRng(s ^ 0, 77); if (r.Value < Tuning.T.tellReliability) on++; }
            Log($"the Mimic's voice sign over 400 planets (pilot 1 a Mimic): on {on} times ({on / 4f:0} %; tell reliability {Tuning.T.tellReliability * 100:0} %): only some of the time");

            // ---- 2. each biome in Free Flight
            foreach (Biome b in new[] { Biome.Canyons, Biome.Forest, Biome.City, Biome.Ice }) yield return BiomeFlight(b);
            GameSettings.S.freeBiome = 0;
            if (perfOnly) yield break;

            // ---- 3. the Campaign in each biome
            Career.CheckSystemSeed = 5151;
            GameSettings.S.difficulty.crashLevel = 1; GameSettings.S.difficulty.enemies = 0;   // Bounce: hits counted, not the end of the run (Round 5: no enemies in this older check)
            yield return ToCampaignMenu();
            yield return Nav(GamepadButton.DpadDown, 1); yield return Press(GamepadButton.South);   // New career
            yield return WaitPhase(() => G.Now == GameShell.State.Docked && G.Phase == "docked", 60f, "the hangar");
            yield return new WaitForSecondsRealtime(2f);
            Log($"new career: first planet {G.Planet.name}, biome \"{G.Planet.biome}\" (must be canyons; today's seed would give {Biomes.ForSeed(Seeds.OfTheDay)}); {Music1()}; Control: {string.Join(" | ", Voices.Log.Where(l => l.Contains("brief")).Take(1))}");
            yield return Shot("c_new_career_hangar");
            G.Menu.Current.Selected = Row("Planets"); yield return Press(GamepadButton.South); yield return new WaitForSecondsRealtime(4.5f);
            Log($"Planets list: {string.Join(" / ", G.Menu.Current.Rows.Where(r => r.Visible == null || r.Visible()).Select(r => r.Label()))}; {Music1()}");
            yield return Shot("c_planets_list");
            yield return Press(GamepadButton.East); yield return new WaitForSecondsRealtime(4.5f);
            Log($"back in the hangar: {Music1()}");
            yield return BiomeSortie(Biome.Forest, false);
            yield return BiomeSortie(Biome.City, true);
            yield return BiomeSortie(Biome.Ice, false);
            Career.CheckSystemSeed = 0;

            // ---- 4. the logs
            Log("--- music (every change, muted run)"); foreach (var l in Music.Log) Log("  " + l);
            Log("--- voices (every line started, in order)"); foreach (var l in Voices.Log) Log("  " + l);
            Log($"--- stingers: {string.Join(" | ", Sfx.Played.Where(l => l.Contains("sting")))}");
            Log($"music: {G.Music.LoadedTracks} tracks loaded from {G.Music.Folder}");
        }
    }
}
