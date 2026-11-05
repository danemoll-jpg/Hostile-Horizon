using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

namespace HostileHorizon
{
    // Round 5 Part B check, in the built exe:  HostileHorizon.exe -exeCheck <dir> -exeCheckOnly round5 | round5perf
    // B12: each preset's numbers (placement, aliens and tells over many planets); a new career's first planet (ease-in: a few
    // turrets, no saucers; pads out of turret sight on Normal, landed at a pad under no fire); then per biome (Canyons on the first
    // planet, Giant Forest, Dead City, Ice jumped to): a turret tracks, locks and fires, the ship flies straight then weaves (hits
    // counted), the shield blocks (energy drain), the gun destroys the turret (bounty); a saucer chase and kill; frame rates with the
    // enemies active. Then the shield on with an alien (and a real pilot) at the door, and the hull shot to 0: the death flow.
    // Cockpit pictures at the moments that matter (bolts coming in, LOCKED ON, the shield, a kill). Positioning shortcuts (the ship
    // placed in a turret's sight, the nose pointed at a target while firing) are the check's, logged; input goes through the pad.
    public sealed partial class ExeCheck
    {
        Enemies F => G.Foes;
        Enemy NearestAlive(EnemyKind k) => F.All.Where(e => !e.Dead && e.Kind == k).OrderBy(e => Flat(e.Pos - G.Ship.transform.position).magnitude).FirstOrDefault();

        // the ship placed `dist` m from a turret where the turret sees it, flying across its view (the turret a little right of the nose)
        bool PlaceInSight(Enemy e, float dist, float throttle, CrashLevel level, float offNose = 25f)
        {
            var s = G.Ship; var f = s.Field;
            for (int a = 0; a < 360; a += 20)
            {
                var dir = Quaternion.Euler(0, a, 0) * Vector3.forward;
                var p = e.Pos + dir * dist; float g = 0; for (int k = -2; k <= 2; k++) { var q = p + Quaternion.Euler(0, a + 90, 0) * Vector3.forward * (k * 60f); g = Mathf.Max(g, f.SurfaceHeight(q.x, q.z)); }
                p.y = Mathf.Max(g + 70f, e.Pos.y + 40f);
                if (!F.Sees(e.Eye, p)) continue;
                var to = Flat(e.Pos - p).normalized;
                var fwd = Quaternion.Euler(0, -offNose, 0) * to;
                s.Spawn(p, Quaternion.LookRotation(fwd), level, throttle, s.Damage); s.Energy = 100; G.Rig.Snap();
                return true;
            }
            return false;
        }

        IEnumerator FlyFor(float seconds, Func<float, Vector2> stick = null)
        {
            float tt = 0;
            while (tt < seconds && G.Now == GameShell.State.Flying) { state.leftStick = stick != null ? stick(tt) : Vector2.zero; Set(); yield return null; tt += Time.deltaTime; }
            state.leftStick = Vector2.zero; Set();
        }

        // hold the gun (A) with the nose kept on the target (a check shortcut: a player aims with the stick); seconds taken or -1
        IEnumerator ShootAt(Enemy e, float limit, Action<float> result)
        {
            var s = G.Ship; float tt = 0; int shots0 = F.PlayerShots, hits0 = F.PlayerHits;
            while (tt < limit && !e.Dead && G.Now == GameShell.State.Flying)
            {
                var c = e.Centre + (e.Kind == EnemyKind.Saucer ? e.Vel * (Vector3.Distance(e.Centre, s.transform.position) / Tuning.T.gunBoltSpeed) : Vector3.zero);
                s.transform.rotation = Quaternion.LookRotation(c - s.transform.TransformPoint(ShipLayout.Eye));
                state = state.WithButton(GamepadButton.South, true); Set(); yield return null; tt += Time.deltaTime;
            }
            state = state.WithButton(GamepadButton.South, false); Set(); yield return null;
            Log($"    gun: {F.PlayerShots - shots0} shots, {F.PlayerHits - hits0} hits, {(e.Dead ? $"destroyed after {tt:0.0} s" : $"NOT destroyed in {limit:0} s (hp {e.Hp})")}");
            result(e.Dead ? tt : -1);
        }

        IEnumerator ShotWhenBoltNear(string name, float within, float limit)
        {
            float end = Time.realtimeSinceStartup + limit;
            while (Time.realtimeSinceStartup < end && F.NearestEnemyBolt(G.Ship.transform.position) > within && G.Now == GameShell.State.Flying) { Set(); yield return null; }
            yield return Shot(name);
            Log($"    (picture: nearest enemy bolt {F.NearestEnemyBolt(G.Ship.transform.position):0} m; LOCKED ON on the displays {G.Displays.LockedOn}; threat marks {F.Threats.Count})");
        }

        // B1, B3, B4: one turret: track / lock / fire; straight vs weaving; shield; the gun
        IEnumerator TurretTest(string tag)
        {
            var e = NearestAlive(EnemyKind.Turret);
            if (e == null) { Log($"  {tag}: no turret alive"); yield break; }
            var s = G.Ship; var c = Career.C;
            if (!PlaceInSight(e, 650f, .5f, CrashLevel.Ghost)) { Log($"  {tag}: turret {e.Index + 1}: NO PLACE IN ITS SIGHT found at 650 m"); yield break; }
            Log($"  {tag}: turret {e.Index + 1} at {e.Pos:F0} ({(e.OnStump ? "on a stump above the canopy, " : "")}{e.Pos.y - s.Field.Height(e.Pos.x + 80, e.Pos.z):+0;-0} m vs 80 m east of it); ship placed 650 m off, in its sight, flying across");
            float t0 = Time.time; int shots0 = F.EnemyShots, hits0 = F.EnemyHits; float hull0 = s.Damage;
            yield return WaitFor(() => e.Locked || G.Now != GameShell.State.Flying, 6f);
            yield return null; yield return null;   // the displays catch up a frame later
            Log($"    locked on after {Time.time - t0:0.00} s (lock time {Tuning.T.turretLockTime / Difficulty.Aggression:0.00} s); LOCKED ON shown {G.Displays.LockedOn}");
            yield return ShotWhenBoltNear($"r5_{tag}_1_incoming", 140f, 6f);
            // straight and level
            PlaceInSight(e, 650f, .5f, CrashLevel.Ghost); shots0 = F.EnemyShots; hits0 = F.EnemyHits; hull0 = s.Damage;
            yield return FlyFor(9f);
            int sStraight = F.EnemyShots - shots0, hStraight = F.EnemyHits - hits0;
            // weaving (bank and pull one way, then the other, about every 1.2 s)
            PlaceInSight(e, 650f, .8f, CrashLevel.Ghost); shots0 = F.EnemyShots; hits0 = F.EnemyHits;
            yield return FlyFor(9f, tt => new Vector2(Mathf.Sign(Mathf.Sin(tt * 2.6f)) * .9f, -.55f));
            int sWeave = F.EnemyShots - shots0, hWeave = F.EnemyHits - hits0;
            Log($"    flying straight 9 s: {sStraight} shots, {hStraight} hits; weaving 9 s: {sWeave} shots, {hWeave} hits; hull {100 - s.Damage:0} % (each hit {Tuning.T.turretDamage:0} %)");
            // the shield
            PlaceInSight(e, 650f, .5f, CrashLevel.Ghost); int b0 = F.Blocked; float en0 = s.Energy; float d0 = s.Damage; shots0 = F.EnemyShots;
            yield return Press(GamepadButton.DpadLeft);
            Log($"    shield (D-pad left): {(G.ShieldOn ? "ON" : "off")}");
            yield return ShotWhenBoltNear($"r5_{tag}_2_shield_block", 30f, 6f);
            yield return FlyFor(5f);
            Log($"    shield on: {F.EnemyShots - shots0} shots at it, {F.Blocked - b0} blocked, hull {d0:0.0} -> {s.Damage:0.0} % damage; energy {en0:0.0} -> {s.Energy:0.0} % (drain {Tuning.T.shieldDrain} %/s + flight)");
            yield return Press(GamepadButton.DpadLeft);
            Log($"    shield off again: {(G.ShieldOn ? "STILL ON" : "off")}");
            // the gun
            int cr0 = c.credits;
            PlaceInSight(e, 600f, .2f, CrashLevel.Ghost, 0);
            yield return new WaitForSecondsRealtime(.3f);
            float took = -1; yield return ShootAt(e, 10f, v => took = v);
            yield return new WaitForSecondsRealtime(.4f);
            yield return Shot($"r5_{tag}_3_turret_destroyed");
            Log($"    bounty: credits {cr0} -> {c.credits} (+{c.credits - cr0}); the prompt \"{G.Displays.Prompt}\"");
        }

        // B2: a saucer: it sees the ship, chases and makes passes; then the gun
        IEnumerator SaucerTest(string tag)
        {
            var e = NearestAlive(EnemyKind.Saucer);
            if (e == null) { Log($"  {tag}: no saucer here"); yield break; }
            var s = G.Ship;
            // the ship 800 m from it, a little below, in its sight
            var p = e.Pos + Flat(s.transform.position - e.Pos).normalized * 800f; p.y = Mathf.Max(s.Field.SurfaceHeight(p.x, p.z) + 80f, e.Pos.y - 60f);
            s.Spawn(p, Quaternion.LookRotation(Flat(e.Pos - p)), CrashLevel.Ghost, .5f, s.Damage); s.Energy = 100; G.Rig.Snap();
            float t0 = Time.time; int sh0 = F.EnemyShots, h0 = F.EnemyHits;
            yield return WaitFor(() => e.Chasing || G.Now != GameShell.State.Flying, 6f);
            Log($"  {tag}: saucer {e.Index + 1} {(e.Chasing ? $"chasing after {Time.time - t0:0.0} s" : "NOT CHASING")} (sight {e.Los}); computer: {string.Join(" | ", Voices.Log.Where(l => l.Contains("cpu_saucer")).Skip(0).Take(1))}");
            yield return FlyFor(10f, tt => new Vector2(Mathf.Sin(tt * .8f) * .5f, -.2f));
            Log($"    10 s of its passes (the ship banking gently): {F.EnemyShots - sh0} shots, {F.EnemyHits - h0} hits; pass state {e.Pass}; saucer {Vector3.Distance(e.Pos, s.transform.position):0} m away, {e.Pos.y - s.Field.SurfaceHeight(e.Pos.x, e.Pos.z):0} m over the ground");
            yield return ShotWhenBoltNear($"r5_{tag}_4_saucer_attack", 200f, 8f);
            float took = -1; yield return ShootAt(e, 15f, v => took = v);
            yield return new WaitForSecondsRealtime(.3f);
            yield return Shot($"r5_{tag}_5_saucer_destroyed");
        }

        IEnumerator FightFps(string tag)
        {
            var e = NearestAlive(EnemyKind.Turret);
            if (e != null) PlaceInSight(e, 700f, .6f, CrashLevel.Ghost);
            G.Terrain.SetFocus(G.Ship.transform.position);
            float wait = Time.realtimeSinceStartup + 12f;
            while (!G.Terrain.ReadyNear(1500f) && Time.realtimeSinceStartup < wait) { Autopilot(); yield return null; }   // the check placed the ship: the ground there loads first
            yield return FlyFor(2f);
            var times = new List<float>(); int maxBolts = 0;
            float tt = 0; while (tt < 12f && G.Now == GameShell.State.Flying) { Autopilot(); yield return null; times.Add(Time.unscaledDeltaTime); tt += Time.unscaledDeltaTime; maxBolts = Mathf.Max(maxBolts, F.LiveBolts); }
            state.leftStick = Vector2.zero; Set();
            if (times.Count < 10) { Log($"  fps {tag}: not measured"); yield break; }
            times.Sort(); float avg = times.Count / times.Sum(), p99 = 1f / times[(int)(times.Count * .99f)];
            Log($"  fps {tag}, enemies active ({F.All.Count(x => !x.Dead)} alive, up to {maxBolts} bolts in flight): average {avg:0.0}, 1% low {p99:0.0}, worst frame {times[times.Count - 1] * 1000:0} ms over {times.Count} frames");
        }

        int SeedFor(Biome b) { for (int s0 = 201; s0 < 6000; s0++) if (Biomes.ForSeed(s0) == b && !Career.C.planets.Exists(p => p.seed == s0)) return s0; return 0; }

        IEnumerator LaunchFromHangar()
        {
            G.Menu.Current.Selected = 0; yield return Press(GamepadButton.South);   // Launch
            yield return WaitPhase(() => G.Now == GameShell.State.Flying, 40f, "flight after launch");
            yield return new WaitForSecondsRealtime(.5f);
        }

        // home: the recall (hold View 1.3 s): the mothership comes and its beam lifts the ship into the bay
        IEnumerator HomeByRecall()
        {
            if (G.Now != GameShell.State.Flying) yield break;
            G.Ship.Energy = 100;
            state = state.WithButton(GamepadButton.Select, true); Set();
            yield return new WaitForSecondsRealtime(1.4f);
            state = state.WithButton(GamepadButton.Select, false); Set();
            yield return WaitPhase(() => G.Phase == "docked", 150f, "docked after the recall");
            yield return new WaitForSecondsRealtime(1f);
        }

        IEnumerator Round5()
        {
            bool perfOnly = CheckRun.Only == "round5perf";
            StartCoroutine(Progress());
            yield return WaitFor(() => G.Now == GameShell.State.Title, 60f);
            yield return new WaitForSecondsRealtime(1.5f);
            var t = Tuning.T;
            Log($"tuning: turrets first planet {t.turretsFirstPlanet}, base {t.turretsBase} + {t.turretsPerTier}/tier (max {t.turretsMax}); range {t.turretRange} m, lock {t.turretLockTime} s, every {t.turretFireInterval} s, bolt {t.turretBoltSpeed} m/s, lead {t.turretLead}, error {t.turretAimError} m at 1 km, damage {t.turretDamage} %, hp {t.turretHp}; " +
                $"saucers {t.saucersBase} + {t.saucersPerTier}/tier (max {t.saucersMax}), {t.saucerSpeed} m/s, turn {t.saucerTurnRate} °/s, burst {t.saucerBurst}, bolt {t.saucerBoltSpeed} m/s, damage {t.saucerDamage} %, hp {t.saucerHp}; gun {t.gunRate}/s, {t.gunEnergy} %/shot, {t.gunBoltSpeed} m/s; shield {t.shieldDrain} %/s; bounty turret {t.turretBounty} / saucer {t.saucerBounty}");

            if (!perfOnly)
            {
                // ---- each preset's numbers (the same test planet, tier 1, not a first planet)
                Log("--- presets");
                var f = new CanyonField(4242, Biome.Canyons); var sites = PlanetSites.Find(f, 5, 1); f.FindStart(out var st, out _);
                var pads = sites.Select(x => x.landing).ToList();
                for (int i = 0; i < 4; i++)
                {
                    GameSettings.S.difficulty.ApplyPreset(i);
                    F.Setup(f, G.Ship, 4242, 1, false, pads, st);
                    int aliens = 0, pilots = 0, tells = 0, falseT = 0;
                    for (int sd = 1; sd <= 300; sd++) { var ps = new PlanetState { seed = sd, tier = 1, pilots = new int[6] }; Aliens.Assign(ps, false); for (int k = 0; k < 6; k++) { pilots++; if (ps.alien[k] != 0) { aliens++; if (ps.quirk[k] != 0) tells++; } else if (ps.quirk[k] != 0) falseT++; } }
                    Log($"  {Difficulty.PresetNames[i]}: {Difficulty.Summary(GameSettings.S.difficulty)}; aggression x{Difficulty.Aggression} (lock {t.turretLockTime / Difficulty.Aggression:0.00} s, fire every {t.turretFireInterval / Difficulty.Aggression:0.00} s, aim error {t.turretAimError / Difficulty.Aggression:0.0} m at 1 km)");
                    Log($"    placement (seed 4242, tier 1): {string.Join("; ", F.Placement.Skip(1))}");
                    Log($"    aliens over 300 planets x 6 pilots (tier 1): {aliens * 100f / pilots:0.0} % aliens, tells shown {(aliens > 0 ? tells * 100f / aliens : 0):0} %, false tells on real pilots {falseT * 100f / (pilots - aliens):0.0} %; pay x{Difficulty.Pay}");
                }
                F.Clear(); GameSettings.S.difficulty.ApplyPreset(1);
                if (CheckRun.Only == "round5presets") yield break;
            }

            // ---- a new career (Normal): the first planet
            Career.CheckSystemSeed = 6262;
            yield return ToCampaignMenu();
            Log($"campaign page rows: {string.Join(" / ", G.Menu.Current.Rows.Select(r => r.Label() + (r.Value != null ? " = " + r.Value() : "")))}");
            yield return Nav(GamepadButton.DpadDown, 1); yield return Press(GamepadButton.South);   // New career
            yield return WaitPhase(() => G.Now == GameShell.State.Docked && G.Phase == "docked", 60f, "the hangar");
            yield return new WaitForSecondsRealtime(1.5f);
            Log($"new career: {G.Planet.name} ({G.Planet.biome}), difficulty {Difficulty.Name(Career.C.difficulty)}, crash level {G.CampaignLevel}");
            foreach (var l in G.Foes.Placement) Log("  placed: " + l);
            Log($"  hangar: {G.Menu.Current.Body().Split('\n').FirstOrDefault(l => l.Contains("Defences")) ?? "(no defences line)"}");
            yield return Shot("r5_a_hangar_first_planet");
            int v0 = Voices.Log.Count;
            yield return LaunchFromHangar();
            Log($"launched: Control said {string.Join(" | ", Voices.Log.Skip(v0).Where(l => l.Contains("ctl_")).Select(l => l.Substring(l.IndexOf("voice ") + 6)))}");
            if (!perfOnly)
            {
                // B7: land at a real pilot's pad and wait for the pickup: under no turret fire on Normal
                var p = G.Pilots.Where(x => x.OnFoot).OrderBy(x => x.Index).FirstOrDefault();
                if (p != null)
                {
                    int sh0 = F.EnemyShots; bool anySees = false;
                    foreach (var e in F.All) if (!e.Dead && Flat(e.Pos - p.LandingSpot).magnitude < t.turretRange && F.Sees(e.Eye, p.LandingSpot + Vector3.up * 4f)) anySees = true;
                    yield return LandFor(p);
                    yield return WaitPhase(() => p.AtDoor, 90f, "the knock");
                    yield return Press(GamepadButton.East);
                    yield return WaitPhase(() => !p.OnFoot, 10f, "boarding");
                    Log($"B7 Normal: landed at pilot {p.Index + 1}'s pad and picked them up: turret shots meanwhile {F.EnemyShots - sh0}; a turret in range sees the pad: {anySees}");
                    yield return BackToFlight();
                }
                yield return TurretTest("canyons");
            }
            yield return FightFps("canyons");
            yield return HomeByRecall();

            // ---- the other biomes (second, third, fourth planets: saucers too)
            foreach (var b in new[] { Biome.Forest, Biome.City, Biome.Ice })
            {
                int seed = SeedFor(b); string k = Biomes.Keys[(int)b];
                Log($"--- {Biomes.Name(b)}: the check jumps to seed {seed}");
                G.JumpTo(seed);
                yield return WaitPhase(() => G.Phase == "docked" && G.Planet.seed == seed, 40f, "the jump");
                foreach (var l in G.Foes.Placement) Log("  placed: " + l);
                yield return LaunchFromHangar();
                if (!perfOnly) { yield return TurretTest(k); yield return SaucerTest(k); }
                yield return FightFps(k);
                yield return HomeByRecall();
            }
            if (perfOnly) { Career.CheckSystemSeed = 0; yield break; }

            // ---- B4: the shield with something at the door (an alien, then a real pilot)
            Log("--- the shield at the airlock");
            foreach (var (type, tag) in new[] { (AlienType.Mimic, "a Mimic"), (AlienType.None, "a real pilot") })
            {
                G.CheckSetup(3, new[] { (int)type, 0, 0 }, new[] { 0, 0, 0 });
                yield return LaunchFromHangar();
                var p = G.Pilots.Find(x => x.Index == 0); int cr0 = Career.C.credits;
                yield return LandFor(p);
                yield return WaitPhase(() => p.AtDoor, 90f, "at the door");
                yield return Press(GamepadButton.DpadLeft);   // shield on
                yield return new WaitForSecondsRealtime(2.5f);
                Log($"  {tag} at the door, shield on: fate {(PilotFate)G.Planet.pilots[0]}, credits {cr0} -> {Career.C.credits}; prompt \"{G.Displays.Prompt}\"");
                if (type != AlienType.None) yield return Shot("r5_shield_burns_alien");
                yield return Press(GamepadButton.DpadLeft);
                yield return BackToFlight();
                yield return HomeByRecall();
            }

            // ---- B5: the hull shot to 0: the death flow
            Log("--- hull to 0");
            yield return LaunchFromHangar();
            {
                var e = NearestAlive(EnemyKind.Turret) ?? NearestAlive(EnemyKind.Saucer);
                float end = Time.realtimeSinceStartup + 60f;
                while (G.Now == GameShell.State.Flying && Time.realtimeSinceStartup < end)
                {
                    // straight and level across a turret's view, 450 m out, the hull nearly gone (placed again if it loses sight)
                    if (e != null && e.Kind == EnemyKind.Turret && (!e.Los || Vector3.Distance(e.Pos, G.Ship.transform.position) > 900f)) { float d = G.Ship.Damage; PlaceInSight(e, 450f, .3f, CrashLevel.Ghost); G.Ship.SetEnergyAndDamage(100, Mathf.Max(d, 85)); yield return new WaitForSecondsRealtime(.3f); }
                    if (G.Ship.Damage < 85) G.Ship.SetEnergyAndDamage(100, 85);
                    Set(); yield return null;
                }
                Log($"  {(G.Ship.Crashed ? $"shot down (killer: {G.LastKiller})" : "NOT SHOT DOWN in 40 s")}; state {G.Now}, phase {G.Phase}");
                yield return WaitFor(() => G.Menu.Current != null && G.Menu.Current.Title == "SHIP DESTROYED", 10f);
                Log($"  page {G.Menu.Current?.Title}: \"{(G.Menu.Current?.Body != null ? G.Menu.Current.Body().Replace("\n", " | ") : "")}\"");
                if (G.Phase != "dead") { Log("  (no death: the death flow not checked)"); yield break; }
                yield return Shot("r5_dead_page");
                yield return Press(GamepadButton.South);   // Wake up
                yield return WaitPhase(() => G.Phase == "regen", 60f, "the regeneration pod");
                G.Menu.Current.Selected = Row("loaner"); G.Menu.Show(G.Menu.Current, true); yield return Press(GamepadButton.South);   // Take the loaner
                yield return WaitPhase(() => G.Phase == "docked", 90f, "the hangar");
                Log($"  back in the hangar: {G.Debrief.Replace("\n", " | ")}");
            }
            Career.CheckSystemSeed = 0;

            Log("--- the fight, in order"); foreach (var l in G.FightLog) Log("  " + l);
            Log($"--- sounds started (Round 5's): {string.Join(" | ", Sfx.Played.Where(l => new[] { "gun_fire", "turret_fire", "saucer_fire", "bolt_hit", "shield", "enemy_explosion", "saucer_hum", "lock_warning" }.Any(l.Contains)).GroupBy(l => l.Substring(l.IndexOf(' ') + 1)).Select(g => $"{g.Key} x{g.Count()}"))}");
            Log($"--- voices (Round 5's): {string.Join(" | ", Voices.Log.Where(l => new[] { "locked_on", "cpu_saucer", "shield_", "target_down", "ctl_hot" }.Any(l.Contains)).Select(l => l.Substring(l.IndexOf("voice ") + 6)).Distinct())}");
        }
    }
}
