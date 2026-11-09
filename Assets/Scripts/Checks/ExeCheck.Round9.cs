using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

namespace HostileHorizon
{
    // Round 9 check, in the built exe:  HostileHorizon.exe -exeCheck <dir> -exeCheckOnly round9[a|b|c|d|e|f|g]
    // Part A (Fix 8.3): the ship's rear from the chase / close chase / landing views and from behind and below, with 0.8.2's cabin
    // (drawn for every camera: the grey box) and with the fix (drawn only inside the hull); the mothership's two openings from the
    // approach, from below, during the recall beam and from inside the bay; the Steam art copy (into a stand-in launcher folder).
    public sealed partial class ExeCheck
    {
        IEnumerator Round9()
        {
            string o = CheckRun.Only;
            bool all = o == "round9";
            if (all || o == "round9a") yield return Round9A();
            if (all || o == "round9b") yield return Round9B();
            if (all || o == "round9c" || o == "round9d" || o == "round9e") yield return Round9CD();   // C, D and E in one career
            if (all || o == "round9f") yield return Round9F();
            if (all || o == "round9g") yield return Round9G();
        }

        IEnumerator Round9A()
        {
            yield return WaitFor(() => G.Now == GameShell.State.Title, 60f);
            yield return new WaitForSecondsRealtime(1f);
            Log("--- A2: the ship's rear (Free Flight, Canyons, Bounce)");
            Log($"  steam art at start: {SteamArt.Result}");
            yield return StartFreeFlight(Biome.Canyons, 1);
            yield return Fly(3f);
            var cabin = G.Ship.GetComponentInChildren<Cabin>(true);
            Log($"  cabin under the ship: {(cabin ? cabin.name + (cabin.gameObject.activeInHierarchy ? " (active)" : " (inactive)") : "none")}");
            foreach (bool old in new[] { true, false })
            {
                Cabin.CheckShowOutside = old; string tag = old ? "before" : "after";
                foreach (var (m, n) in new[] { (CameraRig.Mode.Chase, "chase"), (CameraRig.Mode.CloseChase, "close_chase"), (CameraRig.Mode.Landing, "landing_view") })
                { G.Rig.Current = m; G.Rig.Snap(); yield return Fly(1.5f); yield return Shot($"r9a_{tag}_{n}"); }
                var t = G.Ship.transform;
                yield return ModelShot(t.TransformPoint(0, 0, -2.5f), t.TransformPoint(3, 2.2f, -13), $"r9a_{tag}_rear_34");
                yield return ModelShot(t.TransformPoint(0, -.4f, -2.5f), t.TransformPoint(-4, -4, -11), $"r9a_{tag}_rear_below");
                yield return ModelShot(t.TransformPoint(0, 0, -1.5f), t.TransformPoint(-12, -.5f, -2), $"r9a_{tag}_side");
            }
            Cabin.CheckShowOutside = false;
            G.Rig.Current = CameraRig.Mode.Cockpit; G.Rig.Snap();
            yield return Fly(1f);
            yield return LookShot(new Vector2(-1, 0), 1.45f, "r9a_cockpit_back_left");   // the cabin is still drawn from inside
            G.Pause(); yield return new WaitForSecondsRealtime(.5f);
            G.Menu.Current.Selected = Row("Return to title"); yield return Press(GamepadButton.South);
            yield return WaitFor(() => G.Now == GameShell.State.Title, 30f); yield return new WaitForSecondsRealtime(1f);

            Log("--- A1: the mothership's openings (Campaign, new career)");
            Career.CheckSystemSeed = 7171;
            yield return NewCareer9();
            yield return WaitPhase(() => G.Now == GameShell.State.Docked && G.Phase == "docked", 60f, "the hangar");
            yield return new WaitForSecondsRealtime(1.5f);
            var page = G.Menu.Current; G.Menu.Hide(); G.Displays.SetVisible(false);
            var M = G.Mother.transform;
            yield return ModelShot(M.TransformPoint(0, 1, 0), M.TransformPoint(0, 2.5f, -24), "r9a_bay_inside_to_ship");
            yield return ModelShot(M.TransformPoint(0, 1, -40), M.TransformPoint(0, 1.5f, -6), "r9a_bay_inside_to_stern");
            yield return ModelShot(M.TransformPoint(0, -6, 0), M.TransformPoint(-11, 7, -16), "r9a_bay_inside_floor_opening");
            G.Menu.Show(page);
            G.Menu.Current.Selected = 0; yield return Press(GamepadButton.South);   // Launch
            yield return new WaitForSecondsRealtime(2.5f);
            yield return ModelShot(M.TransformPoint(0, -10, 0), M.TransformPoint(30, -60, -40), "r9a_launch_floor_opening");
            yield return WaitPhase(() => G.Now == GameShell.State.Flying, 40f, "flight after launch");
            yield return Fly(3f);
            M = G.Mother.transform;
            yield return ModelShot(M.TransformPoint(0, 0, -40), M.TransformPoint(25, -8, -120), "r9a_approach_stern");
            yield return ModelShot(M.TransformPoint(0, 0, -38), M.TransformPoint(0, 1, -90), "r9a_stern_straight");
            yield return ModelShot(M.TransformPoint(0, -16, 0), M.TransformPoint(-35, -70, 30), "r9a_below_floor_opening");
            Log("  the recall: the beam lifts the ship through the floor opening");
            yield return HoldRecall(1.4f);
            yield return WaitPhase(() => G.Phase == "tow-lift" || G.Phase.Contains("lift"), 90f, "the beam");
            float seen = 0; int k = 0;
            while (G.Phase != "docked" && seen < 150f)
            {
                M = G.Mother.transform; var rel = M.InverseTransformPoint(G.Ship.transform.position);
                if (k == 0 && rel.y < -60) { yield return ModelShot(G.Ship.transform.position + Vector3.up * 20, M.TransformPoint(45, -50, -60), "r9a_docking_1_far"); k++; }
                else if (k == 1 && rel.y > -30) { yield return ModelShot(M.TransformPoint(0, -14, 0), M.TransformPoint(35, -45, -45), "r9a_docking_2_near"); k++; }
                else if (k == 2 && rel.y > -12) { yield return ModelShot(M.TransformPoint(0, -10, 0), M.TransformPoint(-22, -30, 20), "r9a_docking_3_entering"); k++; }
                seen += Time.unscaledDeltaTime; yield return null;
            }
            Log($"  docking pictures: {k} of 3; phase {G.Phase}");
            Career.CheckSystemSeed = 0;

            Log("--- 2b: the Steam art copy into a stand-in launcher folder");
            string fake = System.IO.Path.Combine(dir, "launcher_root");
            System.IO.Directory.CreateDirectory(fake);
            System.IO.File.WriteAllText(System.IO.Path.Combine(fake, "HostileHorizon-Cover.png"), "old cover");
            Log($"  game folder SteamArt/: {string.Join(", ", System.Array.ConvertAll(System.IO.Directory.Exists(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Application.dataPath), "SteamArt")) ? System.IO.Directory.GetFiles(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Application.dataPath), "SteamArt")) : new string[0], System.IO.Path.GetFileName))}");
            if (LauncherBridge.Argument("-steamArtRoot") != null)
            {
                System.IO.File.WriteAllText(System.IO.Path.Combine(LauncherBridge.Argument("-steamArtRoot"), "HostileHorizon-Cover.png"), "an older cover");
                SteamArt.CopyUp(); Log($"  again with an older cover there: {SteamArt.Result}");
                SteamArt.CopyUp(); Log($"  and once more: {SteamArt.Result}");
            }
        }

        // from the title: Campaign > New career (rows picked by label; a career already there is replaced after the Yes page)
        IEnumerator NewCareer9()
        {
            yield return WaitFor(() => G.Now == GameShell.State.Title && G.Menu.Current != null, 30f);
            G.Menu.Current.Selected = Row("Campaign"); yield return Press(GamepadButton.South); yield return new WaitForSecondsRealtime(.5f);
            G.Menu.Current.Selected = Row("New career"); yield return Press(GamepadButton.South); yield return new WaitForSecondsRealtime(.5f);
            if (G.Menu.Current?.Title == "NEW CAREER?") { G.Menu.Current.Selected = Row("Yes"); yield return Press(GamepadButton.South); }
        }

        // land at a spot (40 m over it, then the landing helper), engines off
        IEnumerator LandAt(Vector3 spot, float heading = 0)
        {
            G.Ship.Energy = 100;
            G.Ship.ResetTo(spot + Vector3.up * 40f, heading); G.Rig.Snap();
            yield return new WaitForSecondsRealtime(.5f);
            yield return HoverLand(spot);
            yield return new WaitForSecondsRealtime(.6f);
            if (G.Ship.EnginesOn) yield return Press(GamepadButton.South);
        }

        // hold the airlock button (B) for so long, as a player does
        IEnumerator HoldAirlock(float seconds, string shotHalfway = null)
        {
            state = state.WithButton(GamepadButton.East, true); Set();
            // by game time, as the game counts it (a batch check's frames are slow: its game time runs behind the real clock)
            float end = Time.time + seconds, half = Time.time + seconds / 2, cap = Time.realtimeSinceStartup + seconds * 4; bool shot = false;
            while (Time.time < end && Time.realtimeSinceStartup < cap && G.Now == GameShell.State.Flying) { Set(); if (!shot && Time.time > half) { shot = true; Log($"    (holding B: airlock held {Controls.AirlockHeld}, landed {G.Ship.Landed}, engines {G.Ship.EnginesOn}, input {G.Ship.InputEnabled}, state {G.Now})"); if (shotHalfway != null) yield return Shot(shotHalfway); } yield return null; }
            state = state.WithButton(GamepadButton.East, false); Set(); yield return null;
        }

        // straight up in landing mode to the docking height; the docking runs by itself
        IEnumerator ClimbAndDock(string tag)
        {
            if (!G.Ship.EnginesOn) yield return Press(GamepadButton.South);
            state.rightTrigger = 1; Set();
            yield return WaitFor(() => G.Now != GameShell.State.Flying || G.Ship.Altitude > Tuning.T.dockingHeight + 30f, 150f);
            state.rightTrigger = 0; Set();
            yield return WaitPhase(() => G.Phase == "docked", 180f, "docked");
            yield return new WaitForSecondsRealtime(1f);
            Log($"  {tag}: docked; debrief \"{G.Debrief.Replace("\n", " | ")}\"");
        }

        // home on the recall beam (out of the turrets' reach while it lifts you)
        IEnumerator RecallHome(string tag)
        {
            yield return HoldRecall(1.4f);
            yield return WaitPhase(() => G.Phase == "docked" && G.Now == GameShell.State.Docked, 180f, "docked after the recall");
            yield return new WaitForSecondsRealtime(1f);
            Log($"  {tag}: recalled and docked; debrief \"{G.Debrief.Replace("\n", " | ")}\"");
        }

        string MissionList() => G.Planet.missions == null ? "-" : string.Join("; ", G.Planet.missions.ConvertAll(m => $"{Missions.Names[m.kind]} {(MissionState)m.state}" + (m.Kind == MissionKind.Timed ? $" (pilot {m.pilot + 1}, {m.timeLeft:0} s)" : m.Kind == MissionKind.Turrets ? $" ({m.count})" : m.Kind == MissionKind.Recorder ? $" (site {m.x:0}, {m.z:0}{(m.aboard ? ", aboard" : "")})" : "")));

        IEnumerator Round9B()
        {
            yield return WaitFor(() => G.Now == GameShell.State.Title, 60f);
            yield return new WaitForSecondsRealtime(1f);
            var t = Tuning.T;
            Log($"--- B: side missions. Rewards (x pay): recorder {t.missionRecorderReward}, nest {t.missionTurretsReward}, timed {t.missionTimedReward} ({t.missionTimedSeconds} s), clean {t.missionCleanReward}; recorder hold {t.missionRecorderHold} s within {t.missionRecorderRange} m");
            var counts = new List<string>(); for (int s = 1; s <= 8; s++) counts.Add($"seed {s * 1111}: {new SeedRng(s * 1111, 91).Next(3) + 1}");
            Log("  missions per planet from the seed (1-3): " + string.Join(", ", counts));
            Missions.CheckAll = true; Career.CheckSystemSeed = 9191;
            yield return NewCareer9();
            yield return WaitPhase(() => G.Now == GameShell.State.Docked && G.Phase == "docked", 60f, "the hangar");
            yield return new WaitForSecondsRealtime(1f);
            Log($"planet 1 {G.Planet.name} (seed {G.Planet.seed}): missions {MissionList()}");
            Log($"  nest placement: {string.Join(" | ", G.Foes.Placement)}");
            yield return Shot("r9b_hangar_missions");
            yield return LaunchFromHangar();
            yield return Fly(2f);
            Log($"  launch: objectives \"{G.Displays.Objectives.Replace("\n", " | ")}\"");
            yield return Shot("r9b_flight_radar_objectives");

            // (1) the recorder
            var rec = G.MissionOf(MissionKind.Recorder);
            if (rec != null)
            {
                var site = rec.Site(G.Terrain.Field);
                G.Ship.ResetTo(site + new Vector3(-120, 60, -120), 45); G.Rig.Snap(); yield return new WaitForSecondsRealtime(1.5f);
                yield return Shot("r9b_recorder_site_approach");
                yield return LandAt(site + new Vector3(-12, 0, -8));
                Log($"  recorder: landed {G.Ship.Landed} {Flat(G.Ship.transform.position - site).magnitude:0} m from the site, engines {(G.Ship.EnginesOn ? "on" : "off")}; prompt \"{G.Displays.Prompt}\"");
                yield return HoldAirlock(3f);
                Log($"  held 3 s then let go: aboard {rec.aboard}, hold now {G.RecorderHold:0.0} s");
                yield return HoldAirlock(t.missionRecorderHold + .8f, "r9b_recorder_pulling");
                Log($"  held {t.missionRecorderHold + .8f:0.0} s: aboard {rec.aboard}; objectives \"{G.Displays.Objectives.Replace("\n", " | ")}\"");
            }
            else Log("  recorder: not on this planet");

            // (2) the turret nest (the gun is Round 5's: the check destroys them directly)
            var nest = G.MissionOf(MissionKind.Turrets);
            if (nest != null)
            {
                var ns = G.Foes.All.FindAll(e => e.Nest); var vs = nest.Site(G.Terrain.Field);
                Log($"  nest: {ns.Count} turrets: {string.Join(", ", ns.ConvertAll(e => $"{Flat(e.Pos - vs).magnitude:0} m out, {e.Pos.y - vs.y:0} m up, sees the valley {G.Foes.Sees(e.Pos + Vector3.up * 3.3f, vs + Vector3.up * 4f, 16f)}"))}");
                if (ns.Count > 0) yield return ModelShot(ns[0].Pos + Vector3.up * 2.4f, ns[0].Pos + new Vector3(12, 6, 16), "r9b_nest_turret");
                yield return BackToFlight();
                foreach (var e in ns) G.Foes.Kill(e);
                yield return null; yield return null;
                Log($"  nest destroyed: mission {(MissionState)nest.state}; enemies down saved {G.Planet.enemiesDown.Count}");
            }
            else Log("  nest: not on this planet");

            // (3) the timed rescue: picked up as a player does
            var timed = G.MissionOf(MissionKind.Timed);
            if (timed != null)
            {
                var p = G.Pilots.Find(x => x.Index == timed.pilot);
                yield return BackToFlight(); yield return null;
                Log($"  timed: pilot {timed.pilot + 1}, {timed.timeLeft:0} s left; radar tag \"{G.Displays.Contacts.Find(c => c.number == timed.pilot + 1 && c.mark == null && !c.mothership).tag}\"");
                yield return LandFor(p);
                yield return WaitPhase(() => p.AtDoor, 90f, "the knock");
                yield return Press(GamepadButton.East);
                yield return WaitPhase(() => !p.OnFoot, 15f, "boarding");
                yield return new WaitForSecondsRealtime(2f);
                Log($"  timed pilot aboard: cabin {G.CabinCount}, {timed.timeLeft:0} s left");
            }
            // (4) the clean run: everyone else put aboard (check shortcut), then home without a scratch
            foreach (var q in G.Pilots.FindAll(x => x.OnFoot)) G.CheckBoard(q);
            int cr = Career.C.credits;
            yield return RecallHome("planet 1");
            Log($"  credits {cr} -> {Career.C.credits}; cleared {G.Planet.cleared}; missions {MissionList()}");
            yield return Shot("r9b_hangar_after");
            G.Menu.Current.Selected = Row("Star map"); yield return Press(GamepadButton.South); yield return new WaitForSecondsRealtime(.6f);
            G.Menu.Current.Selected = 1; yield return new WaitForSecondsRealtime(.6f); yield return Shot("r9b_star_map_missions");
            yield return Press(GamepadButton.East); yield return new WaitForSecondsRealtime(.4f);

            // (5) a second planet: the timed pilot lost when the clock runs out; the clean run broken by a hit
            G.Menu.Current.Selected = Row("Jump to a new planet"); yield return Press(GamepadButton.South);
            yield return WaitPhase(() => G.Phase == "docked" && G.Now == GameShell.State.Docked && G.Menu.Current?.Title == "HANGAR", 60f, "the jump");
            yield return new WaitForSecondsRealtime(1f);
            Log($"planet 2 {G.Planet.name}: missions {MissionList()}");
            yield return LaunchFromHangar();
            var t2 = G.MissionOf(MissionKind.Timed);
            if (t2 != null)
            {
                t2.timeLeft = 6f;
                yield return Fly(2f); yield return Shot("r9b_timed_clock_radar");
                yield return Fly(6f);
                var p2 = G.Pilots.Find(x => x.Index == t2.pilot);
                Log($"  timed: clock run out: mission {(MissionState)t2.state}, pilot {t2.pilot + 1} fate {(PilotFate)G.Planet.pilots[t2.pilot]}, on foot {p2 != null && p2.OnFoot}; message \"{G.Displays.Prompt}\"");
            }
            var c2 = G.MissionOf(MissionKind.Clean);
            if (c2 != null)
            {
                G.Ship.SetEnergyAndDamage(G.Ship.Energy, G.Ship.Damage + 6f); yield return Fly(1f);
                Log($"  clean run after a 6 % hit: {(MissionState)c2.state}");
            }
            Log($"  objectives now \"{G.Displays.Objectives.Replace("\n", " | ")}\"");
            G.Pause(); yield return new WaitForSecondsRealtime(.4f);
            G.Menu.Current.Selected = Row("Save and return to title"); yield return Press(GamepadButton.South);
            yield return WaitFor(() => G.Now == GameShell.State.Title, 30f);
            Missions.CheckAll = false; Career.CheckSystemSeed = 0;
        }
        // dive into the ground (Lasting damage on Normal: a hard hit is a crash), then the death page
        IEnumerator CrashHere(string tag)
        {
            if (G.Ship.Landed || G.Ship.Mode != FlightMode.Flight) { yield return BackToFlight(); yield return SetMode(FlightMode.Flight); }
            // check shortcut: hull at 5 %, then a 10 % hit (the game's own "shot down" path: the explosion, the wreck, the death page)
            G.Ship.SetEnergyAndDamage(G.Ship.Energy, 95f); yield return Fly(1f); G.Ship.TakeDamage(10f);
            Log($"  {tag}: check shortcut: hull set to 5 %, then a 10 % hit, at {G.Ship.AboveGround:0} m over the ground");
            state = default; Set();
            yield return WaitPhase(() => G.Menu.Current != null && G.Menu.Current.Title == "SHIP DESTROYED", 15f, "the death page");
            yield return new WaitForSecondsRealtime(1f);
            var c = Career.C;
            Log($"  {tag}: crashed; own ship wrecked {c.ship.wrecked}, wreck on seed {c.wreckSeed} at ({c.wreckX:0}, {c.wreckZ:0}), wreck insured {c.wreckInsured}, flying the loaner {c.flyingLoaner}");
            if (G.Menu.Current?.Body != null) Log($"  death page: \"{G.Menu.Current.Body().Replace("\n", " | ")}\"");
        }

        IEnumerator WakeAndPick(string row, string tag)
        {
            yield return Press(GamepadButton.South);   // Wake up
            yield return WaitPhase(() => G.Phase == "regen", 40f, "the regeneration bay");
            yield return new WaitForSecondsRealtime(1f);
            var c = Career.C;
            Log($"  {tag} regeneration bay: rows {string.Join(" / ", G.Menu.Current.Rows.FindAll(r => r.Visible == null || r.Visible()).ConvertAll(r => r.Label()))}; fee {c.RecoveryFee} (full {c.FullRecoveryFee}), credits {c.credits}");
            yield return Shot($"r9c_regen_{tag}");
            G.Menu.Current.Selected = Row(row); yield return Press(GamepadButton.South);
            yield return WaitPhase(() => G.Phase == "docked" && G.Now == GameShell.State.Docked, 90f, "the hangar");
            yield return new WaitForSecondsRealtime(1f);
        }

        IEnumerator Round9CD()
        {
            yield return WaitFor(() => G.Now == GameShell.State.Title, 60f);
            yield return new WaitForSecondsRealtime(1f);
            var t = Tuning.T;
            Log($"--- C / D / E: salvage {t.salvageHold} s within {t.salvageRange} m, comes back at {100 - t.salvageHullDamage:0} % hull, ambush chance {t.salvageAmbushChance}; insurance {t.insuranceShareOfShipValue * 100:0} % of the ship's value, recovery x{t.insuredRecoveryShare}");
            Career.CheckSystemSeed = 9393;
            yield return NewCareer9();
            yield return WaitPhase(() => G.Now == GameShell.State.Docked && G.Phase == "docked", 60f, "the hangar");
            yield return new WaitForSecondsRealtime(1f);
            var c = Career.C;
            // D: insure, cancel, insure again
            int cr0 = c.credits;
            G.Menu.Current.Selected = Row("Insure this flight"); yield return Press(GamepadButton.South); yield return new WaitForSecondsRealtime(.3f);
            Log($"D: insured {c.insured}: credits {cr0} -> {c.credits} (fee {c.InsuranceFee}, ship value {c.ShipValue}); row \"{G.Menu.Current.Rows[G.Menu.Current.Selected].Label()}\"");
            yield return Press(GamepadButton.South); yield return new WaitForSecondsRealtime(.3f);
            Log($"  cancelled: insured {c.insured}, credits {c.credits}");
            yield return Press(GamepadButton.South); yield return new WaitForSecondsRealtime(.3f);
            Log($"  insured again {c.insured}, credits {c.credits}; hangar \"{G.Menu.Current.Body().Split('\n')[2]}\" …");
            yield return Shot("r9d_hangar_insured");
            yield return LaunchFromHangar(); yield return Fly(3f);
            Log($"  launch: insured flights {c.stats.insuredFlights}");
            yield return CrashHere("insured crash (own ship)");
            yield return Shot("r9d_death_page_insured");
            Log($"D: recovery fee insured {c.RecoveryFee} cr vs {c.FullRecoveryFee} cr uninsured");

            // C: recover it yourself; first a death on the way (the loaner lost, the wreck stays)
            yield return WakeAndPick("Recover it yourself", "first");
            float wx = c.wreckX, wz = c.wreckZ;
            Log($"C: flying the loaner {c.flyingLoaner}; hangar: \"{G.Menu.Current.Body().Replace("\n", " | ")}\"");
            yield return LaunchFromHangar(); yield return Fly(2f);
            Log($"  launch: salvage here {G.SalvageHere}; route target \"{G.RouteTargetName}\"; radar wreck {G.Displays.Contacts.Exists(x => x.mark == "WRECK")}; objectives \"{G.Displays.Objectives.Replace("\n", " | ")}\"");
            yield return Shot("r9c_loaner_route_to_wreck");
            yield return CrashHere("a death on the way (the loaner)");
            Log($"  the wreck after the loaner went down: same place {Mathf.Approximately(wx, c.wreckX) && Mathf.Approximately(wz, c.wreckZ)}, still wrecked {c.ship.wrecked}, salvaged {c.salvaged}");
            yield return WakeAndPick("Recover it yourself", "second");
            yield return LaunchFromHangar(); yield return Fly(1f);
            var at = G.WreckPos;
            yield return ModelShot(at + Vector3.up * 2f, at + new Vector3(16, 9, -18), "r9c_wreck_close");
            yield return LandAt(at + new Vector3(18, 0, 14));
            Log($"  landed {G.Ship.Landed} {Flat(G.Ship.transform.position - at).magnitude:0} m from the wreck; prompt \"{G.Displays.Prompt}\"; saucers now {G.Foes.All.FindAll(e => e.Kind == EnemyKind.Saucer && !e.Dead).Count}");
            yield return Shot("r9c_landed_at_wreck");
            yield return HoldAirlock(4f);
            Log($"  held 4 s, let go: salvaged {c.salvaged}");
            yield return HoldAirlock(t.salvageHold + .8f, "r9c_hooking_up");
            Log($"  held {t.salvageHold + .8f:0.0} s: salvaged {c.salvaged}; marker gone {GameObject.Find("Your wreck") == null}");
            // E: everyone aboard (check shortcut), dock: the own ship back, the planet cleared, its best run
            foreach (var q in G.Pilots.FindAll(x => x.OnFoot)) G.CheckBoard(q);
            yield return RecallHome("salvage sortie");
            Log($"C: own ship back {!c.flyingLoaner && !c.ship.wrecked}, hull {100 - c.ship.damage:0} %, upgrades {c.ship.upgrades.Count}; salvages {c.stats.salvages}");
            var p1 = G.Planet;
            Log($"E: planet 1 {p1.name}: clears {p1.clears}, best: {(p1.clears > 0 ? $"{Missions.Clock(p1.bestClear)}, {p1.bestCredits} cr, {p1.bestHits} hits, no-death {p1.clearedNoDeath}" : "-")}; run reset {p1.runSeconds:0} s");
            yield return Shot("r9c_hangar_own_ship_back");
            // E: a second planet cleared without dying
            G.Menu.Current.Selected = Row("Jump to a new planet"); yield return Press(GamepadButton.South);
            yield return WaitPhase(() => G.Phase == "docked" && G.Now == GameShell.State.Docked && G.Menu.Current?.Title == "HANGAR", 60f, "the jump");
            yield return LaunchFromHangar(); yield return Fly(20f);
            foreach (var q in G.Pilots.FindAll(x => x.OnFoot)) G.CheckBoard(q);
            yield return RecallHome("planet 2");
            var p2 = G.Planet;
            Log($"E: planet 2 {p2.name}: clears {p2.clears}, best: {Missions.Clock(p2.bestClear)}, {p2.bestCredits} cr, {p2.bestHits} hits, no-death {p2.clearedNoDeath}");
            Log($"E: career totals: {c.stats.flightSeconds:0} s flown, {c.stats.hullHits} hull hits, {c.stats.cleanClears} cleared without dying, {c.stats.missionsDone} missions, {c.stats.salvages} salvages, {c.stats.insuredFlights} insured flights");
            G.Menu.Current.Selected = Row("Star map"); yield return Press(GamepadButton.South); yield return new WaitForSecondsRealtime(.6f);
            for (int r = 1; r <= 2; r++) { G.Menu.Current.Selected = r; yield return new WaitForSecondsRealtime(.6f); yield return Shot($"r9e_star_map_best_{r}"); Log($"  star map row {r}: \"{G.Menu.Current.Body().Replace("\n", " | ")}\""); }
            yield return Press(GamepadButton.East); yield return new WaitForSecondsRealtime(.4f);
            Career.CheckSystemSeed = 0;
        }
        // fly the time trial's course as a player following the gates would (aim ahead along its line), until the finish
        IEnumerator FlyCourse(string tag, float seconds, bool shots)
        {
            var s = G.Ship; var path = G.TrialPath; var at = new float[path.Count];
            for (int i = 1; i < path.Count; i++) at[i] = at[i - 1] + Vector3.Distance(path[i - 1], path[i]);
            int near = 0, hits0 = s.Hits, shot = 0; float end = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < end && G.Now == GameShell.State.Flying && !G.TrialFinished)
            {
                var t = s.transform;
                float bd = float.MaxValue; for (int i = Mathf.Max(0, near - 5); i < Mathf.Min(path.Count, near + 40); i++) { float d = (path[i] - t.position).sqrMagnitude; if (d < bd) { bd = d; near = i; } }
                float want = at[near] + Mathf.Max(90f, s.Speed * 1.8f); int k = near; while (k < path.Count - 1 && at[k] < want) k++;
                var aim = path[k]; var to = aim - t.position;
                float err = Mathf.DeltaAngle(s.Heading, Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg), bank = BankOf(t);
                float roll = Mathf.Clamp((Mathf.Clamp(err * 2f, -65, 65) - bank) * .05f, -1, 1);
                float ground = 0; for (int m = 0; m <= 6; m++) for (int w = -1; w <= 1; w++) { var q = t.position + t.forward * (m * 25f) + t.right * (w * 7f); ground = Mathf.Max(ground, s.Field.SurfaceHeight(q.x, q.y)); }
                float climb = Mathf.Clamp((aim.y - t.position.y) / Mathf.Max(60f, Flat(to).magnitude), -.3f, .6f);
                if (t.position.y + Mathf.Min(0, s.Velocity.y) * 2f < ground + 22f) climb = Mathf.Max(climb, .55f);
                float pitch = Mathf.Clamp((climb - t.forward.y) * 3f, -1, 1) + Pull(t), thr = .38f;
                state.leftStick = new Vector2(Firm(roll), -Firm(Mathf.Clamp(pitch, -1, 1))); state.rightTrigger = s.Throttle < thr - .05f ? .6f : 0; state.leftTrigger = s.Throttle > thr + .05f ? .6f : 0; Set();
                if (shots && shot == 0 && G.NextGate >= 3) { shot++; yield return Shot($"r9f_{tag}_cockpit_gates"); }
                if (shots && shot == 1 && G.NextGate >= 6) { shot++; G.Rig.Current = CameraRig.Mode.Chase; G.Rig.Snap(); yield return new WaitForSecondsRealtime(.6f); yield return Shot($"r9f_{tag}_chase{(G.Ghost ? "_ghost" : "")}"); G.Rig.Current = CameraRig.Mode.Cockpit; G.Rig.Snap(); }
                yield return null;
            }
            state.leftStick = Vector2.zero; state.rightTrigger = 0; state.leftTrigger = 0; Set();
            Log($"  {tag}: finished {G.TrialFinished} in {G.TrialResult:0.0} s (flying {G.TrialTime:0.0} s, {G.TrialMissed} missed{(G.MissBy.Count > 0 ? ": by " + string.Join(", ", G.MissBy.ConvertAll(m => m.ToString("0"))) + " m from the centre" : "")}); gates {G.NextGate} / {G.Gates.Count}; wall / ground hits {s.Hits - hits0}");
        }

        IEnumerator Round9F()
        {
            yield return WaitFor(() => G.Now == GameShell.State.Title, 60f);
            yield return new WaitForSecondsRealtime(1f);
            var t = Tuning.T;
            Log($"--- F: time trial: course {t.trialLength / 1000f:0} km, a gate every {t.trialGateSpacing} m (radius {t.trialGateRadius} m), a missed gate +{t.trialMissPenalty} s");
            GameSettings.S.seedMode = 2; GameSettings.S.customSeed = 4711; GameSettings.S.freeBiome = 0; GameSettings.S.crashLevel = 1; GameSettings.S.trialGhost = true;   // Bounce: a scrape doesn't end the run
            G.Menu.Current.Selected = Row("Free Flight"); yield return Press(GamepadButton.South); yield return new WaitForSecondsRealtime(.5f);
            Log($"  Free Flight page rows: {string.Join(" / ", G.Menu.Current.Rows.ConvertAll(r => r.Label()))}");
            yield return Shot("r9f_free_flight_page");
            G.Menu.Current.Selected = Row("Time trial"); yield return Press(GamepadButton.South);
            yield return WaitFor(() => G.Now == GameShell.State.Flying, 60f);
            var gates1 = G.Gates.ConvertAll(g => g.at);
            float len = 0; for (int i = 1; i < G.TrialPath.Count; i++) len += Vector3.Distance(G.TrialPath[i - 1], G.TrialPath[i]);
            Log($"run 1: {Biomes.Name(G.WorldBiome)}, seed {G.FlightSeed}: course {len / 1000f:0.0} km, {G.Gates.Count} gates, first at {gates1[0].ToString("0")}, last at {gates1[gates1.Count - 1].ToString("0")}; best before {(G.TrialBest != null ? G.TrialBest.best.ToString("0.0") : "none")}, ghost {(G.Ghost ? "shown" : "none")}");
            yield return new WaitForSecondsRealtime(1f); yield return Shot("r9f_run1_start");
            yield return FlyCourse("run 1", 420f, true);
            yield return WaitFor(() => G.Menu.Current?.Title == "TIME TRIAL", 5f);
            if (G.Menu.Current?.Title == "TIME TRIAL") { Log($"  result page: \"{G.Menu.Current.Body().Replace("\n", " | ")}\""); yield return Shot("r9f_run1_result"); }
            var rec = G.BestFor(4711, G.WorldBiome);
            Log($"  trials.json: best {(rec != null ? rec.best.ToString("0.0") : "none")}, {rec?.splits.Count} splits, ghost {(rec != null ? rec.ghost.Count / 7 : 0)} samples");
            // run 2: Race again: the same course, the ghost of run 1 alongside
            G.Menu.Current.Selected = Row("Race again"); yield return Press(GamepadButton.South);
            yield return WaitFor(() => G.Now == GameShell.State.Flying && G.Gates.Count > 0, 60f);
            var gates2 = G.Gates.ConvertAll(g => g.at); bool same = gates1.Count == gates2.Count; for (int i = 0; same && i < gates1.Count; i++) same = (gates1[i] - gates2[i]).sqrMagnitude < .01f;
            Log($"run 2: same course {same} ({gates2.Count} gates); ghost {(G.Ghost ? "shown" : "MISSING")}, best {G.TrialBest?.best:0.0}");
            StartCoroutine(GhostGap());
            yield return FlyCourse("run 2", 420f, true);
            yield return WaitFor(() => G.Menu.Current?.Title == "TIME TRIAL", 5f);
            if (G.Menu.Current?.Title == "TIME TRIAL") { Log($"  result page: \"{G.Menu.Current.Body().Replace("\n", " | ")}\""); yield return Shot("r9f_run2_result"); }
            rec = G.BestFor(4711, G.WorldBiome);
            Log($"  trials.json now: best {(rec != null ? rec.best.ToString("0.0") : "none")} ({rec?.date})");
            G.Menu.Current.Selected = Row("Return to title"); yield return Press(GamepadButton.South);
            yield return WaitFor(() => G.Now == GameShell.State.Title, 30f);
        }

        // during run 2: how far the ghost is from the ship at a few moments (it flies run 1's line)
        IEnumerator GhostGap()
        {
            foreach (float at in new[] { 10f, 30f, 60f })
            {
                yield return WaitFor(() => G.TrialTime >= at || G.TrialFinished || G.Now != GameShell.State.Flying, 300f);
                if (G.Ghost && G.TrialRunning) Log($"    ghost at {at:0} s: {Vector3.Distance(G.Ghost.position, G.Ship.transform.position):0} m from the ship, shown {G.Ghost.gameObject.activeSelf}");
            }
        }
        IEnumerator Round9G()
        {
            yield return WaitFor(() => G.Now == GameShell.State.Title, 60f);
            yield return new WaitForSecondsRealtime(1f);
            var t = Tuning.T; string[] lv = { "Easy (Low)", "Normal", "Hard (High)", "Nightmare (Brutal)" };
            Log("--- G: enemies by difficulty. Easy / Normal must be Round 5's numbers: lead .9, aim error 14 m at 1 km (/ aggression), hit radius 3.5 m, no intercept, no hunting");
            Career.CheckSystemSeed = 9494;
            yield return NewCareer9();
            yield return WaitPhase(() => G.Now == GameShell.State.Docked && G.Phase == "docked", 60f, "the hangar");
            var c = Career.C; int keep = c.difficulty.aggression;
            for (int i = 0; i < 4; i++)
            {
                c.difficulty.aggression = i; var (nt, ns) = Enemies.Estimate(3, false);
                Log($"  {lv[i]}: aggression x{Difficulty.Aggression}, lead {Difficulty.TurretLead}, intercept {Difficulty.TurretIntercept}, aim error {Difficulty.TurretAimError} / {Difficulty.Aggression} = {Difficulty.TurretAimError / Difficulty.Aggression:0.0} m at 1 km, hit radius {Difficulty.BoltHitRadius} m, fire every {t.turretFireInterval / Difficulty.Aggression:0.00} s, lock {t.turretLockTime / Difficulty.Aggression:0.00} s; saucers hunt {Difficulty.SaucersHunt}, pairs {Difficulty.SaucerPairs}; a tier-3 planet: {nt} turrets, {ns} saucers");
            }
            c.difficulty.aggression = keep;
            // a second planet (saucers), launched
            G.Menu.Current.Selected = Row("Jump to a new planet"); yield return Press(GamepadButton.South);
            yield return WaitPhase(() => G.Phase == "docked" && G.Now == GameShell.State.Docked && G.Menu.Current?.Title == "HANGAR", 60f, "the jump");
            yield return LaunchFromHangar(); yield return Fly(2f);

            // ---- the hit rate on a straight crossing at 650 m (the check moves the ship along the line at 100 m/s; turrets may shoot it)
            var tur = G.Foes.All.Find(e => e.Kind == EnemyKind.Turret && !e.Dead);
            if (tur == null) { Log("  no turret on this planet"); yield break; }
            // the crossing line with the clearest view from the turret
            Vector3 bestPerp = Vector3.right; int bestSeen = -1; float h = tur.Eye.y + 60f;
            for (int a = 0; a < 16; a++)
            {
                var perp = Quaternion.Euler(0, a * 22.5f, 0) * Vector3.forward; var along = Vector3.Cross(Vector3.up, perp); int seen = 0;
                for (float s = -900; s <= 900; s += 100) { var p = tur.Pos + perp * 650f + along * s; p.y = h; if (G.Foes.Sees(tur.Eye, p)) seen++; }
                if (seen > bestSeen) { bestSeen = seen; bestPerp = perp; }
            }
            var dirAlong = Vector3.Cross(Vector3.up, bestPerp);
            Log($"  crossing: turret {tur.Index + 1} at {tur.Pos.ToString("0")}; line 650 m out, {h - tur.Eye.y:0} m above its gun, 100 m/s; seen on {bestSeen} of 19 points");
            int shots = 0, hits = 0;
            System.Action<Enemy> fired = e => { if (e == tur) shots++; };
            System.Action<Enemy, float, bool, Vector3> hit = (e, d, b, p) => { if (e == tur && !b) hits++; };
            G.Foes.FiredAtShip += fired; G.Foes.HitShip += hit;
            Enemies.CheckTargetKinematic = true; G.Ship.MakeKinematic(false);
            var results = new List<string>();
            foreach (int level in new[] { 1, 0, 2, 3 })
            {
                c.difficulty.aggression = level; shots = hits = 0;
                for (int pass = 0; pass < 6; pass++)
                {
                    int dirSign = pass % 2 == 0 ? 1 : -1;
                    for (float s = -1000; s <= 1000; s += 100f * Time.deltaTime)
                    {
                        var p = tur.Pos + bestPerp * 650f + dirAlong * (s * dirSign); p.y = h;
                        G.Ship.SetPose(p, Quaternion.LookRotation(dirAlong * dirSign), Time.deltaTime, false);
                        if (G.Ship.Damage > 50) G.Ship.SetEnergyAndDamage(100, 0);
                        yield return null;
                        if (G.Now != GameShell.State.Flying) break;
                    }
                    for (float w = 0; w < 3f; w += Time.deltaTime) yield return null;   // the bolts in the air land
                    G.Ship.SetEnergyAndDamage(100, 0);
                }
                string r = $"{lv[level]}: {hits} hits of {shots} shots ({(shots > 0 ? hits * 100f / shots : 0):0} %{(hits > 0 ? $", 1 in {shots / (float)hits:0.0}" : "")})";
                results.Add(r); Log("  hit rate, 650 m straight crossing, 6 passes: " + r);
            }
            G.Foes.FiredAtShip -= fired; G.Foes.HitShip -= hit;

            // ---- saucers: searching (Hard) and pairs (Nightmare), against Normal: the mean distance to the ship over 40 s, the ship
            // held still 300 m up where it was (the check moves it, so it never lands)
            var hold = G.Ship.transform.position; hold.y = G.Terrain.Field.SurfaceHeight(hold.x, hold.z) + 300f;
            foreach (int level in new[] { 1, 2, 3 })
            {
                c.difficulty.aggression = level;
                foreach (var e in G.Foes.All) if (e.Kind == EnemyKind.Saucer && !e.Dead) { e.Pos = e.Home; e.Chasing = false; e.WaypointUntil = 0; }
                int hunts0 = G.Foes.Hunts, pairs0 = G.Foes.Pairings; float d0 = MeanSaucerDistance(hold);
                for (float w = 0; w < 40f; w += Time.deltaTime) { G.Ship.SetPose(hold, Quaternion.identity, Time.deltaTime, false); if (G.Ship.Damage > 50) G.Ship.SetEnergyAndDamage(100, 0); yield return null; }
                Log($"  saucers on {lv[level]}: mean distance to the ship {d0:0} -> {MeanSaucerDistance(hold):0} m in 40 s; chasing {G.Foes.All.FindAll(e => e.Kind == EnemyKind.Saucer && e.Chasing).Count} of {G.Foes.All.FindAll(e => e.Kind == EnemyKind.Saucer).Count}; search points {G.Foes.Hunts - hunts0}, pairings {G.Foes.Pairings - pairs0}");
            }
            // pairs: two saucers near the ship (check-made), one in sight: on Nightmare the other is brought in; on Hard it is not
            foreach (int level in new[] { 2, 3 })
            {
                c.difficulty.aggression = level;
                foreach (var e in G.Foes.All) if (e.Kind == EnemyKind.Saucer) e.Chasing = false;
                var near = hold + new Vector3(500, 40, 0); var far = hold + new Vector3(-1800, 40, 900);
                var a = G.Foes.CheckSaucer(near); var b = G.Foes.CheckSaucer(far); int p0 = G.Foes.Pairings;
                for (float w = 0; w < 6f; w += Time.deltaTime) { G.Ship.SetPose(hold, Quaternion.identity, Time.deltaTime, false); if (G.Ship.Damage > 50) G.Ship.SetEnergyAndDamage(100, 0); yield return null; }
                Log($"  pairs on {lv[level]}: the near saucer chasing {a.Chasing}, the far one (1.9 km, out of sight) chasing {b.Chasing}; pairings {G.Foes.Pairings - p0}");
                G.Foes.Kill(a); G.Foes.Kill(b);
            }
            yield return Shot("r9g_saucers");
            Enemies.CheckTargetKinematic = false; c.difficulty.aggression = keep;
            Log("  summary: " + string.Join(" | ", results));
            Career.CheckSystemSeed = 0;
        }

        float MeanSaucerDistance(Vector3 at) { float s = 0; int n = 0; foreach (var e in G.Foes.All) if (e.Kind == EnemyKind.Saucer && !e.Dead) { s += Vector3.Distance(e.Pos, at); n++; } return n > 0 ? s / n : 0; }
    }
}
