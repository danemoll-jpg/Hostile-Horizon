using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

namespace HostileHorizon
{
    // Round 6 check, Parts C (upgrade shop), D (star map), E (Field Journal viewer): -exeCheckOnly round6c | round6d | round6e (or round6).
    public sealed partial class ExeCheck
    {
        // ---- C: the numbers that show each upgrade in flight
        struct FlightNumbers { public float top, climb, drain, shield, hit, shots, radarT, radarS, onRadar; public int cabin; }

        IEnumerator Measure(Action<FlightNumbers> done)
        {
            var n = new FlightNumbers(); var s = G.Ship;
            yield return LaunchFromHangar();
            G.Foes.Running = false;   // check shortcut: no fire while measuring
            // top speed: full throttle, level, 14 s (high up: nothing to hit)
            var p = s.transform.position; p.y = s.Field.SurfaceHeight(p.x, p.z) + 900f;
            s.Spawn(p, Quaternion.Euler(0, s.Heading, 0), CrashLevel.Ghost, 1f, 0); s.Energy = 100; G.Rig.Snap();
            float e0 = s.Energy, tt = 0;
            while (tt < 14f && G.Now == GameShell.State.Flying) { state.rightTrigger = 1; state.leftStick = Vector2.zero; Set(); yield return null; tt += Time.deltaTime; }
            state.rightTrigger = 0; Set();
            n.top = s.Speed * 3.6f; n.drain = (e0 - s.Energy) / tt;
            // the shield's extra drain: 5 s on vs the flight's
            float f0 = s.Energy; tt = 0; while (tt < 4f) { yield return null; tt += Time.deltaTime; } float flight = (f0 - s.Energy) / tt;
            yield return Press(GamepadButton.DpadLeft); f0 = s.Energy; tt = 0; while (tt < 4f) { yield return null; tt += Time.deltaTime; } n.shield = (f0 - s.Energy) / tt - flight; yield return Press(GamepadButton.DpadLeft);
            // the gun: shots in 3 s held
            int sh0 = G.Foes.PlayerShots; tt = 0; while (tt < 3f) { state = state.WithButton(GamepadButton.South, true); Set(); yield return null; tt += Time.deltaTime; }
            state = state.WithButton(GamepadButton.South, false); Set(); n.shots = (G.Foes.PlayerShots - sh0) / 3f;
            // a 10 % hit
            float d0 = s.Damage; s.TakeDamage(10); n.hit = s.Damage - d0;
            // hover climb at full lift
            yield return SetMode(FlightMode.Hover); float y0 = s.transform.position.y; tt = 0;
            while (tt < 4f) { state.rightTrigger = 1; Set(); yield return null; tt += Time.deltaTime; }
            state.rightTrigger = 0; Set(); n.climb = (s.transform.position.y - y0) / tt;
            n.radarT = G.RadarTurretRange; n.radarS = G.RadarSaucerRange; n.cabin = G.CabinSize;
            n.onRadar = G.Displays.Contacts.Count(c => c.enemy != 0);
            done(n);
        }

        static string Numbers(FlightNumbers n) => $"top speed {n.top:0} km/h, hover climb {n.climb:0.0} m/s, flight drain {n.drain:0.000} %/s, shield drain {n.shield:0.00} %/s, a 10 % hit does {n.hit:0.0} %, gun {n.shots:0.0} shots/s, enemies' radar reach {n.radarT:0} / {n.radarS:0} m ({n.onRadar:0} on the radar), cabin {n.cabin}";

        IEnumerator Round6C()
        {
            var t = Tuning.T; var c = Career.C;
            if (G.Now == GameShell.State.Flying) yield return HomeByRecall();
            Log("--- Part C: prices (cr per level): " + string.Join("; ", Enumerable.Range(0, Upgrades.Count).Select(i => $"{Upgrades.Names[i]} {string.Join(" / ", Upgrades.Prices((Upg)i))}")));
            // C4: about two planets on Normal: planet 1 (3 pilots) + planet 2 (4 pilots, tier 1), every pilot rescued, the clear bonuses, 2 sorties
            // a planet at 40 % energy each; no bounties
            int p1 = 3 * t.pilotPay + t.clearBonus, p2 = (t.pilotsFirstPlanet + t.pilotsPerTier) * t.pilotPay + t.clearBonus, fuel = Mathf.RoundToInt(4 * 40 * t.fuelPricePerPercent);
            int after1 = t.startingCredits + p1 - fuel / 2, after2 = t.startingCredits + p1 + p2 - fuel; int cheapest = Enumerable.Range(0, Upgrades.Count).Min(i => Upgrades.Prices((Upg)i)[0]);
            Log($"  C4 balance (Normal, no bounties, 4 sorties at 40 % energy = {fuel} cr fuel): start {t.startingCredits}, after planet 1 {after1}, after planet 2 {after2}; cheapest first level {cheapest} cr: affordable after {(after1 >= cheapest ? "ONE planet" : after2 >= cheapest ? "two planets" : "MORE than two planets")}");
            Log($"  ship value {c.ShipValue} cr, recovery fee {c.RecoveryFee} cr (before any upgrade)");
            FlightNumbers before = default, after = default;
            yield return Measure(n => before = n);
            Log($"  no upgrades: {Numbers(before)}");
            yield return HomeByRecall();
            // the shop: credits for the check (logged), then every level of every upgrade bought through the menu (A on each row)
            c.Earn(200000); Log("  check shortcut: +200 000 cr to buy everything");
            G.Menu.Current.Selected = Row("Upgrades"); yield return Press(GamepadButton.South);
            yield return new WaitForSecondsRealtime(.4f); yield return Shot("r6c_1_shop_before");
            for (int i = 0; i < Upgrades.Count; i++)
                for (int l = 0; l < Upgrades.Max((Upg)i); l++)
                {
                    G.Menu.Current.Selected = i; int cr = c.credits; yield return Press(GamepadButton.South); yield return null;
                    Log($"  bought {Upgrades.Names[i]} level {Upgrades.Level(c.ship, (Upg)i)}: -{cr - c.credits} cr; \"{G.Debrief}\"");
                }
            yield return new WaitForSecondsRealtime(.4f); yield return Shot("r6c_2_shop_after");
            Log($"  saved: ship.upgrades = [{string.Join(", ", c.ship.upgrades)}]; loaner: [{string.Join(", ", c.loaner.upgrades)}] (none); ship value {c.ShipValue} cr, recovery fee {c.RecoveryFee} cr");
            yield return Press(GamepadButton.East);   // back to the hangar
            yield return Measure(n => after = n);
            Log($"  every upgrade: {Numbers(after)}");
            Log($"  effect: top speed x{after.top / Mathf.Max(1, before.top):0.00}, climb x{after.climb / Mathf.Max(.1f, before.climb):0.00}, flight drain x{after.drain / Mathf.Max(.001f, before.drain):0.00}, shield drain x{after.shield / Mathf.Max(.001f, before.shield):0.00}, hit x{after.hit / Mathf.Max(.1f, before.hit):0.00}, gun x{after.shots / Mathf.Max(.1f, before.shots):0.00}, radar x{after.radarT / before.radarT:0.00}, cabin {before.cabin} -> {after.cabin}");
            // C3: what shows: the cockpit (radar ring, reticle rings, whiter nozzles), the cabin with 10 seats on the cabin camera
            yield return SetMode(FlightMode.Flight); yield return new WaitForSecondsRealtime(.6f);
            yield return Shot("r6c_3_cockpit_upgraded");
            G.CheckSetup(10, new int[10], new int[10]); for (int i = 0; i < 10; i++) G.CheckBoard(G.Pilots.Find(x => x.Index == i));
            G.Displays.CabinPip = true; yield return new WaitForSecondsRealtime(.8f);
            SaveRT(G.CabinRoom.Feed, "r6c_4_cabin_10_seats"); Log($"  cabin: {G.CabinCount} aboard of {G.CabinSize}; cushions shown {G.CabinRoom.Capacity}");
            G.Displays.CabinPip = false;
            yield return HomeByRecall();
        }

        // ---- D: the star map
        IEnumerator MapShot(string name)
        {
            yield return new WaitForSecondsRealtime(.5f); yield return Shot(name);
            Log($"  {name}: selected \"{G.Menu.Current?.Rows[G.Menu.Current.Selected].Label()}\"; body: {G.Menu.Current?.Body?.Invoke().Replace("\n", " | ")}");
        }

        IEnumerator Round6D()
        {
            var c = Career.C; var t = Tuning.T;
            if (G.Now == GameShell.State.Flying) yield return HomeByRecall();
            Log($"--- Part D: distress every {t.distressEveryHours} h of real time, chance {t.distressChance}, {t.distressMin}-{t.distressMax} pilots (cap {t.distressPilotCap} on a planet)");
            // a few planets in the system: this one cleared (check shortcut: its pilots marked rescued), two new ones by the map's first row
            int home = G.Planet.seed;
            for (int i = 0; i < G.Planet.pilots.Length; i++) G.Planet.pilots[i] = (int)PilotFate.Rescued;
            Log($"  check shortcut: every pilot on {G.Planet.name} marked rescued");
            for (int k = 0; k < 2; k++)
            {
                G.Menu.Current.Selected = Row("Star map"); yield return Press(GamepadButton.South);
                G.Menu.Current.Selected = 0; yield return Press(GamepadButton.South);   // Jump to a new planet
                yield return WaitPhase(() => G.Phase == "docked" && G.Planet.seed != home && c.planets.Count == 2 + k, 40f, "the jump");
            }
            var cleared = c.planets.Find(p => p.seed == home);
            Log($"  planets: {string.Join("; ", c.planets.Select(p => $"{p.name} ({p.biome}, {(p.cleared ? "cleared" : $"{p.Count(PilotFate.Waiting)} waiting")})"))}");
            G.Menu.Current.Selected = Row("Star map"); yield return Press(GamepadButton.South);
            yield return MapShot("r6d_1_star_map");
            yield return Nav(GamepadButton.DpadDown, 2); yield return MapShot("r6d_2_star_map_row3");
            // D1: jump to a visited planet from the map (its row, A)
            int row = Enumerable.Range(1, 8).FirstOrDefault(i => G.Menu.Current.Rows[i].Label().Contains(cleared.name));
            G.Menu.Current.Selected = row; yield return Press(GamepadButton.South);
            yield return WaitPhase(() => G.Phase == "docked" && G.Planet.seed == home, 40f, "the jump back");
            Log($"  jumped from the map to {G.Planet.name} (seed {G.Planet.seed}; visited, cleared {G.Planet.cleared})");
            // D2: coordinates: type a seed with the D-pad (A to edit, up on the last digit four times, left, up twice, A), then Jump
            GameSettings.S.customSeed = 0;
            G.Menu.Current.Selected = Row("Star map"); yield return Press(GamepadButton.South);
            G.Menu.Current.Selected = Row("Coordinates"); yield return Press(GamepadButton.South);
            yield return Nav(GamepadButton.DpadUp, 4); yield return Nav(GamepadButton.DpadLeft, 1); yield return Nav(GamepadButton.DpadUp, 2);
            yield return MapShot("r6d_3_coordinates_typing");
            yield return Press(GamepadButton.South);
            int typed = GameSettings.S.customSeed; Log($"  typed seed {typed}");
            G.Menu.Current.Selected = Row("Go to the coordinates"); yield return MapShot("r6d_4_coordinates_selected"); yield return Press(GamepadButton.South);
            yield return WaitPhase(() => G.Phase == "docked" && G.Planet.seed == typed, 40f, "the jump to the typed seed");
            Log($"  arrived at {G.Planet.name} (seed {G.Planet.seed}, {G.Planet.biome}); on the map now: {c.planets.Exists(p => p.seed == typed)}");
            // D3: a distress call: the clock moved on 30 h (check), then a sortie and home: Control says so at the dock
            Career.CheckHoursAhead = 0; c.CheckDistress(G.Planet.seed);   // starts the clock on the cleared planet
            Log($"  {cleared.name}: cleared {cleared.cleared}, distress clock {cleared.distressCheck}");
            Career.CheckHoursAhead = 30; Log("  check shortcut: the clock moved on 30 hours");
            int v0 = Voices.Log.Count;
            yield return LaunchFromHangar(); yield return HomeByRecall();
            yield return WaitFor(() => Voices.Log.Skip(v0).Any(l => l.Contains("ctl_distress")), 20f);
            Log($"  docked: {cleared.name}: distress {cleared.distress} ({cleared.distressNew} new pilots, {cleared.distressRolls} rolls), pilots now {cleared.pilots.Length}, cleared {cleared.cleared}; Control: {string.Join(" | ", Voices.Log.Skip(v0).Where(l => l.Contains("ctl_distress")))}; debrief \"{G.Debrief.Replace("\n", " | ")}\"");
            yield return Shot("r6d_5_hangar_distress");
            G.Menu.Current.Selected = Row("Star map"); yield return Press(GamepadButton.South);
            int drow = Enumerable.Range(1, 8).FirstOrDefault(i => G.Menu.Current.Rows[i].Label().Contains(cleared.name));
            G.Menu.Current.Selected = drow; yield return MapShot("r6d_6_star_map_distress");
            yield return Press(GamepadButton.South);
            yield return WaitPhase(() => G.Phase == "docked" && G.Planet.seed == home, 40f, "the jump to the distress call");
            Log($"  there: {G.Pilots.Count(p => p.OnFoot)} pilots waiting on {G.Planet.name}; distress mark {G.Planet.distress}");
            Career.CheckHoursAhead = 0;
        }

        // ---- E: the Field Journal viewer
        IEnumerator Round6E()
        {
            var c = Career.C;
            if (G.Now == GameShell.State.Flying) yield return HomeByRecall();
            c.Meet(AlienType.Knocker, G.Planet.name); c.Meet(AlienType.Knocker, G.Planet.name, true); c.Meet(AlienType.Hollow, G.Planet.name); c.Meet(AlienType.Stalker, G.Planet.name); c.Save();
            Log("--- Part E: check shortcut: met the Knocker twice (one killed you), the Hollow and the Stalker once; the Mimic and the Sleeper not met");
            G.Menu.Current.Selected = Row("Field Journal"); yield return Press(GamepadButton.South);
            yield return new WaitForSecondsRealtime(.4f); yield return Shot("r6e_1_journal_index");
            G.Menu.Current.Selected = 2; yield return Press(GamepadButton.South);   // the Knocker
            yield return new WaitForSecondsRealtime(1.2f); yield return Shot("r6e_2_knocker_default");
            Log($"  Knocker page: model {(G.ViewRig ? G.ViewRig.Model : "code-made")}, clip {G.ViewRig?.Current}; body: {G.Menu.Current.Body().Replace("\n", " | ")}");
            // turn with the right stick 1.2 s, then zoom in with the right trigger 1.5 s
            float y0 = G.ViewYaw; state.rightStick = new Vector2(1, 0); Set(); yield return new WaitForSecondsRealtime(1.2f); state.rightStick = Vector2.zero; Set();
            Log($"  right stick 1.2 s: turned {Mathf.DeltaAngle(y0, G.ViewYaw):0}°"); yield return Shot("r6e_3_knocker_turned");
            float d0 = G.ViewDist; state.rightTrigger = 1; Set(); yield return new WaitForSecondsRealtime(1.5f); state.rightTrigger = 0; Set();
            Log($"  right trigger 1.5 s: zoom {d0:0.0} -> {G.ViewDist:0.0} m"); yield return Shot("r6e_4_knocker_zoomed");
            // the animations: the Animation row, right
            G.Menu.Current.Selected = 0;
            for (int i = 1; i < 6; i++)
            {
                yield return Press(GamepadButton.DpadRight); yield return new WaitForSecondsRealtime(.7f);
                Log($"  animation: \"{G.Menu.Current.Rows[0].Value()}\" (clip playing: {G.ViewRig?.Current})");
                if (i == 3 || i == 4) yield return Shot($"r6e_5_knocker_anim_{G.Menu.Current.Rows[0].Value()}");
            }
            // an unmet type: the silhouette
            G.CheckOpenJournalOf(AlienType.Mimic); yield return new WaitForSecondsRealtime(1f);
            yield return Shot("r6e_6_unknown_silhouette"); Log($"  unmet page: {G.Menu.Current.Title}; body: {G.Menu.Current.Body().Replace("\n", " | ")}");
            G.CheckOpenJournalOf(AlienType.Hollow); yield return null; yield return null; G.CheckViewInput(160, 12, 1.3f); yield return new WaitForSecondsRealtime(1f);
            yield return Shot("r6e_7_hollow_closeup"); Log($"  Hollow (met once): body: {G.Menu.Current.Body().Replace("\n", " | ")}");
            yield return Press(GamepadButton.East); yield return Press(GamepadButton.East);
            // E3: from the pause menu, in flight
            yield return LaunchFromHangar();
            yield return Press(GamepadButton.Start); yield return WaitFor(() => G.Now == GameShell.State.Paused, 3f);
            G.Menu.Current.Selected = Row("Field Journal"); yield return Press(GamepadButton.South);
            G.Menu.Current.Selected = 3; yield return Press(GamepadButton.South);   // the Stalker
            state.rightStick = new Vector2(-1, 0); Set(); yield return new WaitForSecondsRealtime(1f); state.rightStick = Vector2.zero; Set();
            yield return Shot("r6e_8_pause_menu_stalker"); Log($"  from the pause menu (time stopped: {Time.timeScale}): {G.Menu.Current.Title}, clip {G.ViewRig?.Current}, yaw {G.ViewYaw:0}");
            yield return Press(GamepadButton.East); yield return Press(GamepadButton.East); yield return Press(GamepadButton.East);
            yield return WaitFor(() => G.Now == GameShell.State.Flying, 3f);
            Log($"  back in flight: {G.Now}");
        }
    }
}
