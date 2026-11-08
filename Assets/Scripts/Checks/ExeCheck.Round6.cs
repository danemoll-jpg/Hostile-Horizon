using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

namespace HostileHorizon
{
    // Round 6 check, in the built exe:  HostileHorizon.exe -exeCheck <dir> -exeCheckOnly round6 | round6a | round6b | round6c | round6d | round6e
    // A7: an alien boarding against a real pilot boarding: every sound, sting, music change, voice line, cabin light, message and
    // the HUD / input / cabin state, from the airlock opening to the strike: identical until the strike. Each strike kind once
    // (timing), the canopy leap, the red herrings with real pilots, cockpit pictures at the strike.
    public sealed partial class ExeCheck
    {
        // ---- the trace: game events (Trace.Add in the game) plus the state the player can see, sampled each frame
        bool sampling;
        IEnumerator SampleState()
        {
            sampling = true; var last = new Dictionary<string, string>();
            while (sampling)
            {
                var d = G.Displays;
                var now = new Dictionary<string, string>
                {
                    ["controls"] = G.Ship.InputEnabled && G.Now == GameShell.State.Flying ? "yours" : "taken",
                    ["HUD"] = d.OverlayShown ? "shown" : "hidden",
                    ["displays"] = d.Blackout ? "dark" : "lit",
                    ["prompt"] = Regex.Replace(d.Prompt ?? "", "[0-9]+", "#"),
                    ["pickup line"] = d.PickupLine ?? "",
                    ["cabin count"] = d.Cabin.ToString(),
                    ["cabin lights"] = G.CabinRoom.LightsOn ? "on" : "off",
                    ["music wanted"] = G.Music.Want ?? "",
                    ["menu"] = G.Menu.Current != null && G.Menu.Visible ? G.Menu.Current.Title : "",
                    ["airlock"] = G.Extras.AirlockOpen ? "open" : G.Extras.AirlockShut ? "shut" : "moving",
                    ["creature"] = G.HorrorCreature ? "SHOWN" : "none",
                };
                foreach (var kv in now) if (!last.TryGetValue(kv.Key, out var v) || v != kv.Value) { Trace.Add("state", $"{kv.Key} = {kv.Value}"); last[kv.Key] = kv.Value; }
                yield return null;
            }
        }

        // what the player could notice, from t0 on, before `until` (seconds after t0); times rounded away for the comparison
        static List<string> Seen(List<(float t, string kind, string what)> lines, float t0, float until) =>
            lines.Where(l => l.t >= t0 && l.t - t0 < until && l.kind != "strike").Select(l => $"{l.kind}: {l.what}").ToList();
        static string Timed(List<(float t, string kind, string what)> lines, float t0, float until) =>
            string.Join("\n      ", lines.Where(l => l.t >= t0 && l.t - t0 < until).Select(l => $"{l.t - t0:00.00} {l.kind}: {l.what}"));

        // one boarding at pilot 1's pad: land, wait at the door, press B; then (takeoff) lift off and fly straight on.
        // Returns the trace and the press time.
        IEnumerator Boarding(AlienType type, bool takeoff, float record, Action<List<(float, string, string)>, float> done)
        {
            G.CheckSetup(3, new[] { (int)type, 0, 0 }, new[] { 0, 0, 0 });
            G.Foes.Running = false;   // check shortcut: no turret fire mixed into the comparison (logged)
            var p = G.Pilots.Find(x => x.Index == 0);
            yield return LandFor(p);
            yield return WaitPhase(() => p.AtDoor, 60f, "the door");
            yield return new WaitForSecondsRealtime(1.2f);
            G.CheckReseedRolls();
            StartCoroutine(SampleState()); Trace.Start(); yield return null;
            float t0 = Time.time;
            yield return Press(GamepadButton.East);   // the airlock (B)
            if (takeoff)
            {
                yield return WaitFor(() => p.Now == DownedPilot.State.Aboard && G.Extras.AirlockShut || G.Now != GameShell.State.Flying, 15f);
                yield return new WaitForSecondsRealtime(.5f);
                if (G.Now == GameShell.State.Flying) yield return Press(GamepadButton.South);   // engines on
                state.rightTrigger = 1; Set();
                float end = Time.time + record;
                while (Time.time - t0 < record && G.Now == GameShell.State.Flying) { if (G.Ship.AboveGround > 40f) { state.rightTrigger = .3f; Set(); } yield return null; }
                state.rightTrigger = 0; Set();
            }
            else { float end = Time.time + record; while (Time.time < end && G.Now == GameShell.State.Flying) yield return null; }
            yield return new WaitForSecondsRealtime(.3f);
            sampling = false; Trace.On = false;
            done(new List<(float, string, string)>(Trace.Lines), t0);
        }

        IEnumerator StrikePictures(string tag)
        {
            yield return WaitFor(() => G.StrikeAt > 0 && Time.time - G.StrikeAt > .12f, 90f);
            if (G.StrikeAt < 0) yield break;
            yield return Shot($"r6a_{tag}_1_strike_0.1s"); Log($"    strike +{Time.time - G.StrikeAt:0.00} s: {CreatureWhere()}");
            yield return WaitFor(() => Time.time - G.StrikeAt > .5f, 3f);
            yield return Shot($"r6a_{tag}_2_strike_0.5s"); Log($"    strike +{Time.time - G.StrikeAt:0.00} s: {CreatureWhere()}");
            yield return WaitFor(() => Time.time - G.StrikeAt > 1.0f, 3f);
            yield return Shot($"r6a_{tag}_3_strike_1.0s"); Log($"    strike +{Time.time - G.StrikeAt:0.00} s: {CreatureWhere()}; beat \"{G.HorrorBeat}\", lights {(G.CabinRoom.LightsOn ? "on" : "off")}");
        }

        IEnumerator Compare(GameShell.StrikeKind kind, string tag)
        {
            bool takeoff = kind == GameShell.StrikeKind.Takeoff;
            Log($"--- A7 {tag}: a real pilot boards, then a Knocker (no tell shown) boards the same way at the same pad; the alien strikes {kind}");
            List<(float t, string kind, string what)> real = null, alien = null; float r0 = 0, a0 = 0;
            yield return Boarding(AlienType.None, takeoff, takeoff ? 22f : 14f, (l, t) => { real = l; r0 = t; });
            Log($"  real pilot: cabin {G.CabinCount}, state {G.Now}{(G.Now != GameShell.State.Flying ? " (NOT FLYING: the comparison is off)" : "")}");
            G.CheckStrikeKind = (int)kind; G.StrikeAtReset();
            var pics = StartCoroutine(StrikePictures(tag));
            yield return Boarding(AlienType.Knocker, takeoff, takeoff ? 22f : 14f, (l, t) => { alien = l; a0 = t; });
            G.CheckStrikeKind = -1;
            if (G.StrikeAt < a0) { Log("  THE ALIEN NEVER STRUCK in the recorded time"); yield break; }
            float strike = G.StrikeAt - a0, cut = strike - .05f;
            var rs = Seen(real, r0, cut); var al = Seen(alien, a0, cut);
            int same = 0; while (same < rs.Count && same < al.Count && rs[same] == al[same]) same++;
            bool identical = same == rs.Count && same == al.Count;
            Log($"  the alien struck {strike:0.00} s after B was pressed ({G.LastStrike})");
            Log($"  COMPARISON, B pressed -> {cut:0.00} s: real pilot {rs.Count} events, alien {al.Count} events: {(identical ? "IDENTICAL" : $"DIFFERENT from event {same + 1}: real \"{(same < rs.Count ? rs[same] : "(none)")}\" vs alien \"{(same < al.Count ? al[same] : "(none)")}\"")}");
            Log($"    real pilot, timed:\n      {Timed(real, r0, cut)}");
            Log($"    alien, timed (to the strike and 2 s past it):\n      {Timed(alien, a0, strike + 2f)}");
            float firstDiffT = alien.Where(l => l.t - a0 >= cut).Select(l => l.t - a0).DefaultIfEmpty(-1).First();
            Log($"  first event the alien adds: at {firstDiffT:0.00} s ({strike:0.00} s = the strike)");
            yield return WaitFor(() => G.Phase == "dead" || G.Menu.Current?.Title == "SHIP DESTROYED", 10f);
            yield return DeadToHangar($"r6a_{tag}");
        }

        IEnumerator Round6A()
        {
            var t = Tuning.T;
            Log($"--- Part A tuning: strike weights instant {t.strikeInstantWeight} / take-off {t.strikeTakeoffWeight}; instant {t.strikeInstantMin}-{t.strikeInstantMax} s after the airlock shuts; take-off {t.strikeTakeoffMin}-{t.strikeTakeoffMax} s after lift-off or a turn over {t.strikeHardTurn} °/s; canopy leap {t.canopyLeapChance * 100:0} % at {t.canopyLeapMin}-{t.canopyLeapMax} s (else the climb at {t.canopyClimbAfter} s); herrings flicker {t.herringFlicker} / thump {t.herringThump} / crackle {t.herringCrackle}");
            // the seeded rolls over many planets
            int[] k = new int[2]; float[] dsum = new float[2]; int leaps = 0, n = 0;
            for (int s = 1; s <= 600; s++) for (int i = 0; i < 6; i++) { n++; var kk = GameShell.RollStrike(s * 7919, i, out float d); k[(int)kk]++; dsum[(int)kk] += d; if (GameShell.RollLeap(s * 7919, i, out _)) leaps++; }
            Log($"  600 planets x 6 pilots: instant {k[0] * 100f / n:0} % (mean {dsum[0] / Mathf.Max(1, k[0]):0.0} s), take-off {k[1] * 100f / n:0} % ({dsum[1] / Mathf.Max(1, k[1]):0.0} s); canopy leap {leaps * 100f / n:0} %; the same seed again: {GameShell.RollStrike(7919, 2, out var d1)} {d1:0.0} s / {GameShell.RollStrike(7919, 2, out var d2)} {d2:0.0} s");

            yield return Compare(GameShell.StrikeKind.Instant, "instant");
            yield return Compare(GameShell.StrikeKind.Takeoff, "takeoff");

            // ---- A3: the canopy leap (a Stalker set to leap: its seeded time is used)
            {
                G.CheckSetup(3, new[] { (int)AlienType.Knocker, 0, 0 }, new[] { 0, 0, 0 }); G.Foes.Running = false;
                var p = G.Pilots.Find(x => x.Index == 0);
                bool seeded = p.Leaps; float after = p.ClimbAfter;
                if (!p.Leaps) { p.Leaps = true; p.ClimbAfter = (t.canopyLeapMin + t.canopyLeapMax) / 2; }
                Log($"--- A3 canopy leap: a Knocker left at the door (seeded: {(seeded ? $"leaps at {after:0.0} s" : $"climbs at {after:0} s; set to leap at {p.ClimbAfter:0.0} s for the check")})");
                yield return LandFor(p); yield return WaitPhase(() => p.AtDoor, 60f, "the door");
                float atDoor = Time.time; int s0 = Sfx.Played.Count;
                yield return WaitFor(() => G.HorrorBeat == "leap", 40f);
                Log($"  it leapt {Time.time - atDoor:0.0} s after reaching the door");
                yield return WaitFor(() => G.HorrorBeat == "on the canopy", 2f);
                Log($"  on the canopy {Time.time - atDoor:0.00} s after the door; sounds: {string.Join(", ", Sfx.Played.Skip(s0).Where(x => !x.Contains("knock")))}; view shake {G.Rig.ShakeNow:0.00}");
                yield return Shot("r6a_leap_1_slam");
                yield return new WaitForSecondsRealtime(1.5f); yield return Shot("r6a_leap_2_on_the_glass");
                yield return Press(GamepadButton.South);   // engines: thrown off
                yield return new WaitForSecondsRealtime(2f);
                Log($"  engines on: canopy alien {(G.CanopyAlien == null ? "gone (thrown off)" : "STILL THERE")}, fate {(PilotFate)G.Planet.pilots[0]}");
                yield return BackToFlight();
            }

            // ---- A4: red herrings with a real pilot (all three forced for the check; their seeded rates above)
            {
                int[] h = new int[3]; for (int s = 1; s <= 600; s++) for (int i = 0; i < 6; i++) { var r = new SeedRng(s * 7919 ^ (i * 4049), 67); if (r.Value < t.herringFlicker) h[0]++; if (r.Value < t.herringThump) h[1]++; if (r.Value < t.herringCrackle) h[2]++; }
                Log($"--- A4 red herrings: seeded rates over 3600 boardings: flicker {h[0] / 36f:0.0} %, thump {h[1] / 36f:0.0} %, crackle {h[2] / 36f:0.0} % (the same for real pilots and aliens)");
                G.CheckAllHerrings = true;
                List<(float t, string kind, string what)> lines = null; float h0 = 0;
                yield return Boarding(AlienType.None, false, 8f, (l, t0) => { lines = l; h0 = t0; });
                G.CheckAllHerrings = false;
                Log($"  real pilot with all three: {string.Join(" | ", lines.Where(l => l.kind == "herring" || l.what.Contains("cabin lights") || l.what.StartsWith("power_dip") || l.what.StartsWith("cabin_thump") || l.what.StartsWith("feed_crackle") || l.kind == "cabin lights").Select(l => $"{l.t - h0:0.00} {l.kind}: {l.what}"))}");
                Log($"  after: cabin {G.CabinCount}, lights {(G.CabinRoom.LightsOn ? "on" : "off")}, state {G.Now}, no strike {(G.StrikeAt < h0 ? "(none)" : "STRUCK")}");
            }
            Log("alien events: " + string.Join(" | ", G.AlienEvents));
        }

        // ---- B4: can a first-time player see them? The ship placed `dist` m from an enemy, in its line of sight, nose on it but
        // 8 degrees off (as a player flying past); the ground loaded; then a cockpit picture.
        IEnumerator Sight(Enemy e, float dist, string tag)
        {
            var s = G.Ship; var f = s.Field; bool placed = false;
            for (int a = 0; a < 360 && !placed; a += 15)
            {
                var dir = Quaternion.Euler(0, a, 0) * Vector3.forward; var p = e.Centre + dir * dist;
                p.y = Mathf.Max(f.SurfaceHeight(p.x, p.z) + 60f, e.Kind == EnemyKind.Turret ? e.Pos.y + 30f : e.Pos.y - 40f);
                if (!F.Sees(p, e.Centre, 16f)) continue;
                var look = Quaternion.LookRotation(e.Centre - p) * Quaternion.Euler(0, 8f, 0);
                s.Spawn(p, look, CrashLevel.Ghost, .05f, s.Damage); s.Energy = 100; G.Rig.Snap(); placed = true;
            }
            if (!placed) { Log($"  {tag}: no spot in sight at {dist:0} m"); yield break; }
            G.Terrain.SetFocus(s.transform.position);
            float wait = Time.realtimeSinceStartup + 12f; while (!G.Terrain.ReadyNear(2500f) && Time.realtimeSinceStartup < wait) { s.transform.rotation = Quaternion.LookRotation(e.Centre - s.transform.position) * Quaternion.Euler(0, 8f, 0); yield return null; }
            for (int i = 0; i < 20; i++) { s.transform.rotation = Quaternion.LookRotation(e.Centre - s.transform.position) * Quaternion.Euler(0, 8f, 0); yield return null; }
            // a blink "on" moment for the picture (the light is off 0.5 s of every 1.2 s)
            yield return WaitFor(() => Time.time % 1.2f > .1f && Time.time % 1.2f < .6f, 2f);
            s.transform.rotation = Quaternion.LookRotation(e.Centre - s.transform.position) * Quaternion.Euler(0, 8f, 0);
            yield return Shot($"r6b_{tag}");
            var cam = G.Rig.Head; var sp = cam.WorldToScreenPoint(e.Centre);
            int onRadar = G.Displays.Contacts.Count(c => c.enemy == (e.Kind == EnemyKind.Turret ? 1 : 2) && Flat(c.position - e.Pos).magnitude < 1f);
            Log($"  {tag}: {(e.Kind == EnemyKind.Turret ? "turret" : "saucer")} {e.Index + 1} {Vector3.Distance(cam.transform.position, e.Centre):0} m away, at screen {sp.x / cam.pixelWidth * 100:0} % / {sp.y / cam.pixelHeight * 100:0} %; warning light {(e.Beacon && e.Beacon.gameObject.activeSelf ? $"{e.Beacon.localScale.x:0.0} m" : "off (blink)")}; " +
                $"on the radar {(onRadar > 0 ? "yes" : "NO")} (spotted {e.Spotted}); brackets: {G.Brackets.ShownList}");
        }

        IEnumerator Round6B()
        {
            var t = Tuning.T;
            if (G.Now == GameShell.State.Flying) yield return HomeByRecall();
            Log($"--- Part B:radar reach turrets {t.radarTurretRange} m / saucers {t.radarSaucerRange} m; brackets to {t.bracketRange} m; readable sizes: light {t.beaconScale} / flash {t.flashScale} / bolt glow {t.boltGlowScale} m per m (accuracy, fire rate, aggression unchanged: lead {t.turretLead}, error {t.turretAimError}, interval {t.turretFireInterval})");
            foreach (var b in new[] { Biome.Canyons, Biome.Forest, Biome.City, Biome.Ice })
            {
                string k = Biomes.Keys[(int)b];
                if (G.Planet.biome != Biomes.Keys[(int)b] || !G.Foes.All.Exists(x => x.Kind == EnemyKind.Saucer))
                {
                    int seed = SeedFor(b); Log($"--- {Biomes.Name(b)}: the check jumps to seed {seed}");
                    G.JumpTo(seed); yield return WaitPhase(() => G.Phase == "docked" && G.Planet.seed == seed, 40f, "the jump");
                }
                yield return LaunchFromHangar();
                int tur = G.Foes.All.Count(x => !x.Dead && x.Kind == EnemyKind.Turret), sau = G.Foes.All.Count(x => !x.Dead && x.Kind == EnemyKind.Saucer);
                yield return new WaitForSecondsRealtime(.5f);
                int rT = G.Displays.Contacts.Count(c => c.enemy == 1), rS = G.Displays.Contacts.Count(c => c.enemy == 2);
                Log($"  {Biomes.Name(b)}: {tur} turrets, {sau} saucers; on the radar at launch (none spotted yet: {G.Foes.Spotted}): {rT} turrets, {rS} saucers");
                var tu = NearestAlive(EnemyKind.Turret);
                if (tu != null) { yield return Sight(tu, 2000f, $"{k}_turret_2km"); yield return Sight(tu, 1000f, $"{k}_turret_1km"); }
                // a turret firing at 900 m: the muzzle flash and the bolt coming
                if (tu != null && PlaceInSight(tu, 900f, .3f, CrashLevel.Ghost, 10f))
                {
                    int f0 = F.Flashes; yield return WaitFor(() => F.Flashes > f0 || G.Now != GameShell.State.Flying, 8f);
                    yield return null; yield return Shot($"r6b_{k}_turret_fires_900m");
                    Log($"  {k}: turret firing at 900 m: flashes {F.Flashes - f0}, nearest bolt {F.NearestEnemyBolt(G.Ship.transform.position):0} m");
                }
                var sa = NearestAlive(EnemyKind.Saucer);
                if (sa != null) { yield return Sight(sa, 2000f, $"{k}_saucer_2km"); yield return Sight(sa, 1000f, $"{k}_saucer_1km"); }
                yield return HomeByRecall();
            }
        }

        IEnumerator Round6()
        {
            StartCoroutine(Progress());
            yield return WaitFor(() => G.Now == GameShell.State.Title, 60f);
            yield return new WaitForSecondsRealtime(1.5f);
            string only = CheckRun.Only;
            Career.CheckSystemSeed = 6363;
            yield return ToCampaignMenu();
            yield return Nav(GamepadButton.DpadDown, 1); yield return Press(GamepadButton.South);   // New career
            yield return WaitPhase(() => G.Now == GameShell.State.Docked && G.Phase == "docked", 60f, "the hangar");
            yield return new WaitForSecondsRealtime(1f);
            Log($"new career: {G.Planet.name} ({G.Planet.biome}), {Difficulty.Name(Career.C.difficulty)}");
            GameSettings.S.routeTrail = false;
            if (only == "round6" || only == "round6a")
            {
                yield return LaunchFromHangar();
                yield return Round6A();
            }
            if (only == "round6" || only == "round6b") yield return Round6B();
            if (only == "round6" || only == "round6c") yield return Round6C();
            if (only == "round6" || only == "round6d") yield return Round6D();
            if (only == "round6" || only == "round6e") yield return Round6E();
            Career.CheckSystemSeed = 0;
        }
    }
}
