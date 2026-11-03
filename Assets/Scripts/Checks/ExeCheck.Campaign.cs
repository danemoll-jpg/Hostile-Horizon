using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

namespace HostileHorizon
{
    // Round 2 check, in the built exe:  HostileHorizon.exe -exeCheck <dir> -exeCheckOnly campaign
    // Plays the Campaign from the title with the virtual gamepad, as a player would: new career, the hangar, launch (live), fly to a
    // pilot by the radar, landing mode, set down, engines off, the pilot runs and knocks, airlock, take off, climb, docking (live),
    // pay; a second pilot killed by starting the engines beside them; running out of energy and the tow; a pilot aboard and a
    // crash (explosion, wreck, death page over the live scene), the trip to the regeneration pod, the loaner, recovering the own
    // ship; quit to the title and Continue. The camera's travel is logged for every sequence (a cut or fade-free check: no frame
    // may jump). The career is saved in <dir>/save (never Dan's).
    public sealed partial class ExeCheck
    {
        Vector3 lastCam; Quaternion lastCamRot; float maxJump, maxTurn; bool watching;

        IEnumerator WatchCamera()
        {
            watching = true; maxJump = 0; maxTurn = 0; lastCam = G.Rig.Head.transform.position; lastCamRot = G.Rig.Head.transform.rotation;
            var eof = new WaitForEndOfFrame();
            while (watching)
            {
                yield return eof;   // after this frame's camera move, which used this frame's deltaTime
                var p = G.Rig.Head.transform.position; var r = G.Rig.Head.transform.rotation; float dt = Mathf.Max(Time.deltaTime, 1e-4f);
                maxJump = Mathf.Max(maxJump, Vector3.Distance(p, lastCam) / dt); maxTurn = Mathf.Max(maxTurn, Quaternion.Angle(r, lastCamRot) / dt);
                lastCam = p; lastCamRot = r;
            }
        }
        // A cut or teleport shows as thousands of m/s in one frame; live flight stays under about 150 m/s and a few hundred °/s.
        string Continuity() { watching = false; return $"camera: fastest {maxJump:0} m/s, fastest turn {maxTurn:0}°/s ({(maxJump < 400f && maxTurn < 720f ? "continuous: no cut" : "JUMP")})"; }

        IEnumerator WaitPhase(Func<bool> f, float seconds, string what)
        {
            float t0 = Time.realtimeSinceStartup;
            yield return WaitFor(f, seconds);
            if (!f()) Log($"  TIMED OUT waiting for {what} after {seconds:0} s (state {G.Now}, phase {G.Phase})");
        }

        DownedPilot NearestWaiting()
        {
            DownedPilot best = null; float bd = float.MaxValue;
            foreach (var p in G.Pilots) if (p.OnFoot) { float d = Vector3.Distance(p.LandingSpot, G.Ship.transform.position); if (d < bd) { bd = d; best = p; } }
            return best;
        }

        // Flight mode towards a point: bank to the bearing, hold 70 m over the highest ground ahead on the way.
        IEnumerator FlyTo(Vector3 target, float stopAt)
        {
            float end = Time.realtimeSinceStartup + 120f;
            while (Time.realtimeSinceStartup < end && G.Now == GameShell.State.Flying)
            {
                var s = G.Ship; var t = s.transform; var to = target - t.position; to.y = 0;
                if (to.magnitude < stopAt) break;
                float want = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg, err = Mathf.DeltaAngle(s.Heading, want);
                float bank = Mathf.Asin(Mathf.Clamp(-t.right.y, -1, 1)) * Mathf.Rad2Deg;
                float roll = Mathf.Clamp((Mathf.Clamp(err * 1.5f, -50, 50) - bank) * .04f, -1, 1);
                float high = 0; var dir = to.normalized;
                for (int m = 0; m <= 10; m++) { var p = t.position + dir * (m * 60f); high = Mathf.Max(high, s.Field.Height(p.x, p.z)); }
                float climb = Mathf.Clamp((high + 70f - t.position.y) * .02f, -.45f, .8f);
                float pitch = Mathf.Clamp((climb - t.forward.y) * 3f, -1, 1);
                state.leftStick = new Vector2(Firm(roll), -Firm(pitch)); state.leftTrigger = 0; state.rightTrigger = 0; Set();
                yield return null;
            }
            state.leftStick = Vector2.zero; Set();
        }

        // Landing mode: turn to the spot, move over it, hold 35 m up, then set straight down gently.
        IEnumerator HoverLand(Vector3 spot)
        {
            float end = Time.realtimeSinceStartup + 200f;
            while (Time.realtimeSinceStartup < end && G.Now == GameShell.State.Flying && !G.Ship.Landed)
            {
                var s = G.Ship; var t = s.transform; var to = spot - t.position; to.y = 0; float d = to.magnitude;
                float err = Mathf.DeltaAngle(s.Heading, Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg);
                float hs = new Vector2(s.Velocity.x, s.Velocity.z).magnitude;
                // firm stick commands, like a player's: the pad's own deadzone (12.5 %) and the game's (8 %) swallow small ones
                float turn = d > 4f && Mathf.Abs(err) > 3f ? Mathf.Sign(err) * Mathf.Clamp(Mathf.Abs(err) / 30f, .35f, 1f) : 0;
                float fwd = d > 4f && Mathf.Abs(err) < 40f ? Mathf.Clamp(d / 40f, .35f, 1f) : 0;
                if (hs > 4f && d < hs * hs / 16f) fwd = 0;   // brake in time
                float agl = s.AboveGround, lift;
                if (d > 10f) lift = Mathf.Clamp((35f - agl) * .12f, -1, 1);
                else lift = agl > 22f ? -.85f : agl > 8f ? -.35f : -.17f;
                state.leftStick = new Vector2(turn, fwd);
                state.rightTrigger = Mathf.Max(0, lift); state.leftTrigger = Mathf.Max(0, -lift); Set();
                yield return null;
            }
            state.leftStick = Vector2.zero; state.leftTrigger = 0; state.rightTrigger = 0; Set();
        }

        IEnumerator TakeOffAndClimb(float toAltitude)
        {
            state.rightTrigger = 1; Set();
            yield return WaitFor(() => G.Ship.AboveGround > 45f, 15f);
            state.rightTrigger = 0; Set();
            yield return SetMode(FlightMode.Flight);                        // back to flight mode
            float end = Time.realtimeSinceStartup + 60f;
            while (Time.realtimeSinceStartup < end && G.Now == GameShell.State.Flying && G.Ship.Altitude < toAltitude)
            {
                var t = G.Ship.transform; float bank = Mathf.Asin(Mathf.Clamp(-t.right.y, -1, 1)) * Mathf.Rad2Deg;
                float pitch = Mathf.Clamp((.75f - t.forward.y) * 3f, -1, 1);
                state.leftStick = new Vector2(Mathf.Clamp(-bank * .04f, -1, 1), -pitch); state.rightTrigger = 1; Set();
                yield return null;
            }
            state.leftStick = Vector2.zero; state.rightTrigger = 0; Set();
        }

        IEnumerator LaunchFromHangar(string shot)
        {
            yield return WaitPhase(() => G.Now == GameShell.State.Docked && G.Phase == "docked", 60f, "the hangar");
            G.Menu.Current.Selected = 0;
            StartCoroutine(WatchCamera());
            yield return Press(GamepadButton.South);                        // Launch
            yield return new WaitForSecondsRealtime(3.2f);
            if (shot != null) yield return Shot(shot);
            yield return WaitPhase(() => G.Now == GameShell.State.Flying, 30f, "flight after launch");
            Log($"launch: doors, drop, engines; handed over at {G.Ship.Altitude:0} m, {G.Ship.Speed * 3.6f:0} km/h; " + Continuity());
        }

        // Press the landing-mode button until the ship is in the wanted mode (a press right after a hand-over is ignored on purpose).
        IEnumerator SetMode(FlightMode want)
        {
            for (int i = 0; i < 5 && G.Ship.Mode != want && G.Now == GameShell.State.Flying; i++) { yield return Press(GamepadButton.West); yield return new WaitForSecondsRealtime(.2f); }
        }

        IEnumerator ToPilotAndLand(DownedPilot p, string shot)
        {
            Log($"  to pilot {p.Index + 1}: {Vector3.Distance(p.LandingSpot, G.Ship.transform.position):0} m; radar says: {RadarScope.List(G.Ship.transform, G.Displays.Contacts, 1).Trim()}");
            yield return FlyTo(p.LandingSpot, 420f);
            Log($"  near: radar says: {RadarScope.List(G.Ship.transform, G.Displays.Contacts, 1).Trim()}");
            yield return SetMode(FlightMode.Hover);                          // landing mode
            Log($"  landing mode: {G.Ship.Mode}, prompt \"{G.Displays.Prompt}\"");
            int hits = G.Ship.Hits;
            yield return HoverLand(p.LandingSpot);
            Log($"  landed {G.Ship.Landed} at {Vector3.Distance(G.Ship.transform.position, p.LandingSpot):0} m from the spot, {G.Ship.Notice}; hits {G.Ship.Hits - hits}, hull {100 - G.Ship.Damage:0}%, energy {G.Ship.Energy:0}%");
            if (shot != null) yield return Shot(shot);
        }

        // Every 10 s: frame rate and where the ship is (so a slow run can be told from a wrong one).
        IEnumerator Progress()
        {
            while (true)
            {
                int f0 = Time.frameCount; float t0 = Time.realtimeSinceStartup;
                yield return new WaitForSecondsRealtime(10f);
                var s = G.Ship;
                Log($"    [{(Time.frameCount - f0) / (Time.realtimeSinceStartup - t0):0} fps] {G.Now}/{G.Phase} {s.Mode}{(s.Landed ? " landed" : "")} alt {s.Altitude:0} gnd {s.AboveGround:0} spd {s.Speed:0} hdg {s.Heading:0} energy {s.Energy:0}");
            }
        }

        IEnumerator Campaign()
        {
            StartCoroutine(Progress());
            yield return WaitFor(() => G.Now == GameShell.State.Title, 60f);
            yield return new WaitForSecondsRealtime(1.5f);
            Log($"save folder: {Career.Folder} (a check never touches Dan's career)");
            // planet determinism: the same seed twice gives the same name and pilot spots
            var f1 = new CanyonField(Seeds.OfTheDay); var a = PlanetSites.Find(f1, 3, 0); var b = PlanetSites.Find(new CanyonField(Seeds.OfTheDay), 3, 0);
            bool same = a.Count == b.Count; for (int i = 0; i < a.Count && same; i++) same = a[i].landing == b[i].landing;
            f1.FindStart(out var st, out _);
            Log($"today's planet: {Names.Planet(Seeds.OfTheDay)} (twice: {Names.Planet(Seeds.OfTheDay)}), seed {Seeds.OfTheDay}; next day's {Names.Planet(Seeds.OfTheDay + 1)}; pilot spots twice {(same ? "identical" : "DIFFERENT")}: " +
                string.Join(", ", a.ConvertAll(s => $"{Vector2.Distance(new Vector2(s.landing.x, s.landing.z), new Vector2(st.x, st.z)):0} m out, bumps {PlanetSites.Bumpiness(f1, s.landing.x, s.landing.z, 16f):0.0} m")));

            // ---- title -> Campaign -> New career
            yield return Nav(GamepadButton.DpadDown, 1); yield return Press(GamepadButton.South);
            yield return Shot("20_campaign_menu");
            yield return Nav(GamepadButton.DpadDown, 1); yield return Press(GamepadButton.South);   // New career (no save yet: starts at once)
            yield return WaitPhase(() => G.Now == GameShell.State.Docked, 60f, "the hangar");
            yield return new WaitForSecondsRealtime(1.5f);
            var c = Career.C;
            Log($"new career: {c.systemName} system, {c.credits} cr; planet {G.Planet.name}, {G.Planet.pilots.Length} pilots, {G.Pilots.Count} placed; save written: {File.Exists(Career.FilePath)}");
            yield return Shot("21_hangar");
            G.Menu.Canvas.gameObject.SetActive(false); yield return Shot("21b_hangar_bay_view"); G.Menu.Canvas.gameObject.SetActive(true);

            // ---- sortie 1: rescue a pilot and dock
            yield return LaunchFromHangar("22_launch_drop");
            yield return new WaitForSecondsRealtime(1f); yield return Shot("23_after_launch");
            var p1 = NearestWaiting();
            yield return ToPilotAndLand(p1, "24_landed");
            int cr0 = Career.C.credits;
            yield return Press(GamepadButton.South);                         // engines off
            Log($"  engines off: {!G.Ship.EnginesOn}; pilot {p1.Index + 1}: {p1.Now}");
            yield return WaitPhase(() => p1.Now == DownedPilot.State.Running, 10f, "the pilot to run");
            yield return new WaitForSecondsRealtime(1.5f);
            yield return Press(GamepadButton.North); yield return new WaitForSecondsRealtime(.6f);
            yield return Shot("25_pilot_running_chase");
            yield return Press(GamepadButton.North); yield return Press(GamepadButton.North); yield return Press(GamepadButton.North); yield return Press(GamepadButton.North);   // back to cockpit
            float tr = Time.realtimeSinceStartup;
            yield return WaitPhase(() => p1.AtDoor, 90f, "the knock");
            Log($"  pilot at the hull, knocking, after {Time.realtimeSinceStartup - tr:0} s; prompt \"{G.Displays.Prompt}\"");
            yield return new WaitForSecondsRealtime(1.2f);
            yield return Shot("26_knocking");
            yield return Press(GamepadButton.East);                          // airlock
            yield return WaitPhase(() => G.CabinCount == 1, 10f, "boarding");
            Log($"  airlock: pilot {p1.Index + 1} {p1.Now}, cabin {G.CabinCount}");
            yield return Shot("27_aboard");
            yield return TakeOffAndClimb(Tuning.T.dockingHeight + 30f);
            yield return WaitPhase(() => G.Now == GameShell.State.Auto, 30f, "docking to start");
            Log($"docking started at {G.Ship.Altitude:0} m ({G.Phase})");
            StartCoroutine(WatchCamera());
            yield return WaitPhase(() => G.Phase == "dock-final", 90f, "the final approach");
            yield return Shot("28_docking_approach");
            state.rightStick = new Vector2(-1, .3f); Set(); yield return new WaitForSecondsRealtime(1.2f);
            Log($"  looking round while docking: view {Vector3.Angle(G.Rig.Head.transform.forward, G.Ship.transform.forward):0}° off the nose");
            yield return Shot("29_docking_look_round");
            state.rightStick = Vector2.zero; Set();
            yield return WaitPhase(() => G.Phase == "dock-rise", 90f, "the rise into the bay");
            yield return new WaitForSecondsRealtime(3f);
            yield return Shot("30_rising_into_bay");
            yield return WaitPhase(() => G.Phase == "docked", 60f, "docked");
            Log($"docked: {Continuity()}");
            Log($"  credits {cr0} -> {Career.C.credits}; debrief: {G.Debrief.Replace("\n", " | ")}");
            Log($"  planet: rescued {G.Planet.Count(PilotFate.Rescued)}, lost {G.Planet.Count(PilotFate.Lost)}, waiting {G.Planet.Count(PilotFate.Waiting)}; save {File.GetLastWriteTime(Career.FilePath):HH:mm:ss}");
            yield return new WaitForSecondsRealtime(1f);
            yield return Shot("31_hangar_after_dock");

            // ---- sortie 2: start the engines with a pilot running beside the ship
            yield return LaunchFromHangar(null);
            var p2 = NearestWaiting();
            yield return ToPilotAndLand(p2, null);
            yield return Press(GamepadButton.South);                         // engines off
            yield return WaitPhase(() => p2.Now == DownedPilot.State.Running && Vector3.Distance(p2.Position, G.Ship.transform.position) < Tuning.T.exhaustKillRange - 4f, 60f, "the pilot to come close");
            int cr1 = Career.C.credits;
            yield return Press(GamepadButton.South);                         // engines on
            yield return null;
            Log($"engines started with pilot {p2.Index + 1} {Vector3.Distance(p2.Position, G.Ship.transform.position):0} m away: pilot {p2.Now}, planet says {(PilotFate)G.Planet.pilots[p2.Index]}, credits {cr1} -> {Career.C.credits}; message \"{G.Displays.Prompt}\"");
            yield return new WaitForSecondsRealtime(.8f);
            state.rightStick = new Vector2(-1, -.5f); Set(); yield return new WaitForSecondsRealtime(.8f);
            yield return Shot("32_exhaust_killed");
            state.rightStick = Vector2.zero; Set();

            // ---- out of energy over the last pilot's landing spot: down, the tow, live
            var p3 = NearestWaiting();
            state.rightTrigger = 1; Set(); yield return WaitFor(() => G.Ship.AboveGround > 45f, 10f); state.rightTrigger = 0; Set();
            yield return SetMode(FlightMode.Flight);
            yield return FlyTo(p3.LandingSpot, 420f);
            yield return SetMode(FlightMode.Hover);
            yield return WaitFor(() => G.Ship.Speed < 8f, 20f);
            float hoverEnd = Time.realtimeSinceStartup + 90f;
            var hl = StartCoroutine(HoverLand(p3.LandingSpot));
            yield return WaitFor(() => new Vector2(G.Ship.transform.position.x - p3.LandingSpot.x, G.Ship.transform.position.z - p3.LandingSpot.z).magnitude < 15f, 60f);
            G.Ship.Energy = .5f;   // check shortcut: the drain itself is the same code as any other flight, only faster to reach
            Log($"energy set to 0.5 % over pilot {p3.Index + 1}'s spot ({G.Ship.AboveGround:0} m up) to see the end of energy");
            yield return WaitPhase(() => G.Ship.Landed || G.Now != GameShell.State.Flying, 60f, "coming down powerless");
            StopCoroutine(hl); state = default; Set();
            Log($"  powerless: {G.Ship.Notice}; landed {G.Ship.Landed}, hull {100 - G.Ship.Damage:0}%, page {G.Menu.Current?.Title}");
            yield return new WaitForSecondsRealtime(.5f);
            yield return Shot("33_out_of_energy");
            int cr2 = Career.C.credits;
            StartCoroutine(WatchCamera());
            yield return Press(GamepadButton.South);                         // call the tow
            yield return WaitPhase(() => G.Phase == "tow-lift", 90f, "the tow lift");
            yield return new WaitForSecondsRealtime(4f);
            yield return Shot("34_tow_lift");
            yield return WaitPhase(() => G.Phase == "docked", 120f, "docked after the tow");
            Log($"tow: {Continuity()}; credits {cr2} -> {Career.C.credits}; debrief: {G.Debrief.Replace("\n", " | ")}");

            // ---- sortie 3: a pilot aboard, then the crash
            yield return LaunchFromHangar(null);
            var p4 = NearestWaiting();
            if (p4 != null)
            {
                yield return ToPilotAndLand(p4, null);
                yield return Press(GamepadButton.South);
                yield return WaitPhase(() => p4.AtDoor, 90f, "the knock");
                yield return Press(GamepadButton.East);
                yield return WaitPhase(() => G.CabinCount == 1, 10f, "boarding");
                state.rightTrigger = 1; Set(); yield return WaitFor(() => G.Ship.AboveGround > 60f, 10f); state.rightTrigger = 0; Set();
            }
            yield return SetMode(FlightMode.Flight);
            yield return new WaitForSecondsRealtime(2f);
            Log($"crash run: {G.CabinCount} pilot aboard, {CrashLevels.Names[(int)G.Ship.Level]}, hull {100 - G.Ship.Damage:0}%, credits {Career.C.credits}, deaths {Career.C.stats.deaths}");
            float diveEnd = Time.realtimeSinceStartup + 40f;
            while (Time.realtimeSinceStartup < diveEnd && G.Now == GameShell.State.Flying) { state.leftStick = new Vector2(0, 1); state.rightTrigger = 1; Set(); yield return null; }
            state = default; Set();
            StartCoroutine(WatchCamera());
            yield return new WaitForSecondsRealtime(.25f); yield return Shot("35_explosion");
            yield return new WaitForSecondsRealtime(1.5f); yield return Shot("36_explosion_debris");
            yield return WaitPhase(() => G.Menu.Current != null && G.Menu.Current.Title == "SHIP DESTROYED", 10f, "the death page");
            yield return new WaitForSecondsRealtime(2f);
            yield return Shot("37_wreck_burning_death_page");
            Log($"crash: state {G.Now}, impact {G.Ship.LastImpact * 3.6f:0} km/h, wreck kept {G.Ship.gameObject.activeSelf}, explosion {(G.Blast ? "in the scene" : "MISSING")}; page \"{G.Menu.Current?.Title}\" over the live scene; {Continuity()}");
            Log($"  career: deaths {Career.C.stats.deaths}, pilots lost {Career.C.stats.pilotsLost}, own ship wrecked {Career.C.ship.wrecked}; planet pilots {string.Join(",", G.Planet.pilots)}");
            G.Menu.Canvas.gameObject.SetActive(false); yield return Shot("38_wreck_no_menu"); G.Menu.Canvas.gameObject.SetActive(true);

            // ---- regeneration: the trip to the pod, the loaner, then buying the own ship back
            StartCoroutine(WatchCamera());
            yield return Press(GamepadButton.South);                         // Wake up
            yield return new WaitForSecondsRealtime(4f); yield return Shot("39_regen_trip");
            yield return WaitPhase(() => G.Phase == "regen", 30f, "the regeneration bay");
            Log($"regeneration trip: {Continuity()}");
            yield return new WaitForSecondsRealtime(1f);
            yield return Shot("40_regeneration_bay");
            yield return Nav(GamepadButton.DpadDown, 1); yield return Press(GamepadButton.South);   // Take the loaner
            yield return new WaitForSecondsRealtime(4f); yield return Shot("41_loaner_arriving");
            yield return WaitPhase(() => G.Phase == "docked", 60f, "the hangar with the loaner");
            Log($"loaner: flying it {Career.C.flyingLoaner}, own ship wrecked {Career.C.ship.wrecked}, credits {Career.C.credits}, recovery fee {Career.C.RecoveryFee}");
            yield return Shot("42_hangar_loaner");
            if (Career.C.credits < Career.C.RecoveryFee)
            {
                Log($"check shortcut: credits {Career.C.credits} -> {Career.C.RecoveryFee + 500} so the recovery from the hangar can be tried");
                Career.C.credits = Career.C.RecoveryFee + 500;
            }
            {
                G.Menu.Current.Selected = 3; G.Menu.Show(G.Menu.Current, true);
                int cr3 = Career.C.credits;
                yield return Press(GamepadButton.South);                     // Recover your own ship
                yield return WaitPhase(() => G.Phase == "recovery", 5f, "the swap");
                yield return WaitPhase(() => G.Phase == "docked", 90f, "the own ship back");
                Log($"recovered own ship: loaner {Career.C.flyingLoaner}, wrecked {Career.C.ship.wrecked}, credits {cr3} -> {Career.C.credits}");
                yield return Shot("43_own_ship_back");
            }

            // ---- quit to the title and Continue
            string before = $"{Career.C.credits} cr, rescued {Career.C.stats.pilotsRescued}, lost {Career.C.stats.pilotsLost}, deaths {Career.C.stats.deaths}, planet {string.Join(",", G.Planet.pilots)}, loaner {Career.C.flyingLoaner}";
            G.Menu.Current.Selected = G.Menu.Current.Rows.Count - 1; G.Menu.Show(G.Menu.Current, true);
            yield return Press(GamepadButton.South);                         // Back to title (saves)
            yield return WaitPhase(() => G.Now == GameShell.State.Title, 60f, "the title");
            yield return Nav(GamepadButton.DpadDown, 1); yield return Press(GamepadButton.South);   // Campaign
            yield return Press(GamepadButton.South);                         // Continue
            yield return WaitPhase(() => G.Now == GameShell.State.Docked, 60f, "the hangar after Continue");
            string after = $"{Career.C.credits} cr, rescued {Career.C.stats.pilotsRescued}, lost {Career.C.stats.pilotsLost}, deaths {Career.C.stats.deaths}, planet {string.Join(",", G.Planet.pilots)}, loaner {Career.C.flyingLoaner}";
            Log($"quit and Continue: before [{before}] after [{after}] ({(before == after ? "same" : "DIFFERENT")})");
            yield return new WaitForSecondsRealtime(1f);
            yield return Shot("44_continue_hangar");

            // ---- frame rate: the hangar, then flying the planet low
            var times = new List<float>(); float e1 = Time.realtimeSinceStartup + 6f;
            while (Time.realtimeSinceStartup < e1) { times.Add(Time.unscaledDeltaTime); yield return null; }
            times.Sort(); Log($"fps in the hangar: average {times.Count / times.Sum():0}, 1% low {1f / times[(int)(times.Count * .99f)]:0}");
            yield return LaunchFromHangar(null);
            yield return Fly(4f);
            yield return MeasureFps("Campaign, flying low with the beacons and the mothership in the world");
            File.Copy(Career.FilePath, Path.Combine(dir, "career_after_check.json"), true);
            Log("career save copied to career_after_check.json");
        }
    }

}
