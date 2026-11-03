using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

namespace HostileHorizon
{
    // Fix 2.5 check, in the built exe:  HostileHorizon.exe -exeCheck <dir> -exeCheckOnly fix25
    // (a) Uncommanded movement: Free Flight high up (Ghost), simple stick inputs through the virtual gamepad, and what turned the ship
    //     by source (ShipController.Moved: stick, bank turn, wing levelling, wall bounce, hover body easing); a glancing hit on a
    //     canyon wall (Bounce). (b) The pickup as a player does it: brake at the SLOW DOWN cue, set straight down where the ship
    //     stopped, engines off, and the pilot's run logged until they knock (or don't), then the airlock; three pilots.
    public sealed partial class ExeCheck
    {
        static Vector3 Flat(Vector3 v) { v.y = 0; return v; }
        static float PitchOf(Transform t) => Mathf.Asin(Mathf.Clamp(t.forward.y, -1, 1)) * Mathf.Rad2Deg;
        string MovedText(ShipController s) { var parts = new List<string>(); for (int i = 0; i < s.Moved.Length; i++) parts.Add($"{ShipController.MoveNames[i]} {s.Moved[i]:0.0}°"); return string.Join(", ", parts); }

        // Hold a stick input for a time, then (optionally) centred for a time; logs heading / pitch / bank change and the sources.
        IEnumerator Measure(string name, Vector2 stick, float holdSeconds, float centredSeconds, float rb = 0, bool hover = false)
        {
            var s = G.Ship; var t = s.transform;
            float h0 = s.Heading, p0 = PitchOf(t), b0 = BankOf(t); Quaternion r0 = t.rotation;
            s.ResetMoves();
            float tt = 0;
            while (tt < holdSeconds) { state.leftStick = stick; state = state.WithButton(GamepadButton.RightShoulder, rb > 0); Set(); yield return null; tt += ShipDt; }
            state.leftStick = Vector2.zero; state = state.WithButton(GamepadButton.RightShoulder, false); Set();
            float h1 = s.Heading, p1 = PitchOf(t), b1 = BankOf(t); var during = (float[])s.Moved.Clone(); Quaternion r1 = t.rotation;
            s.ResetMoves(); tt = 0;
            while (tt < centredSeconds) { Set(); yield return null; tt += ShipDt; }
            float h2 = s.Heading, p2 = PitchOf(t), b2 = BankOf(t);
            string D(float[] m) { var parts = new List<string>(); for (int i = 0; i < m.Length; i++) if (m[i] > .05f) parts.Add($"{ShipController.MoveNames[i]} {m[i]:0.0}°"); return parts.Count > 0 ? string.Join(", ", parts) : "nothing"; }
            Log($"  {name}: " + (holdSeconds > 0 ? $"input ({stick.x:0.00}, {stick.y:0.00}){(rb > 0 ? " + RB" : "")} {holdSeconds:0.0} s -> heading {Mathf.DeltaAngle(h0, h1):+0.0;-0.0}°, pitch {p1 - p0:+0.0;-0.0}°, bank {b1 - b0:+0.0;-0.0}° [{D(during)}]; " : "") +
                $"stick centred {centredSeconds:0.0} s -> heading {Mathf.DeltaAngle(h1, h2):+0.0;-0.0}°, pitch {p2 - p1:+0.0;-0.0}°, bank {b2 - b1:+0.0;-0.0}°, total {Quaternion.Angle(r1, t.rotation):0.0}° [{D(s.Moved)}]");
        }

        IEnumerator Uncommanded(string assistName, int assist)
        {
            var s = G.Ship; GameSettings.S.flightAssist = assist; var f = s.CurrentFeel;
            Log($"uncommanded movement, Flight assist {assistName} (rates {f.pitch:0} / {f.roll:0} / {f.yaw:0} °/s, bank turn {f.bankTurn:0}, levelling {f.level:0}, deadzone {GameSettings.S.deadzone}%, stick curve {Tuning.T.stickCurve}):");
            PlaceHigh(0, .6f); yield return null;
            yield return Measure("level flight", Vector2.zero, 0, 15f);
            foreach (float bank in new[] { 15f, 30f, 60f })
            {
                PlaceHigh(0, .6f); s.transform.rotation = Quaternion.Euler(0, 0, -bank); yield return null;
                yield return Measure($"banked {bank:0}°, released", Vector2.zero, 0, 10f);
            }
            PlaceHigh(0, .6f); yield return null;
            yield return Measure("roll right, full stick, then release", new Vector2(1, 0), .3f, 5f);
            PlaceHigh(0, .6f); yield return null;
            yield return Measure("nose up, full stick, then release", new Vector2(0, -1), .4f, 3f);
            PlaceHigh(0, .6f); yield return null;
            yield return Measure("small correction: 15 % roll", new Vector2(.15f, 0), 1f, 3f);
            PlaceHigh(0, .6f); yield return null;
            yield return Measure("small correction: 25 % roll", new Vector2(.25f, 0), 1f, 3f);
            PlaceHigh(0, .6f); yield return null;
            yield return Measure("stick drift (Deck-like, 7 % / 5 %)", new Vector2(.07f, .05f), 10f, 0);
            PlaceHigh(0, .6f); yield return null;
            yield return Measure("stick drift (12 % / 10 %)", new Vector2(.12f, -.10f), 10f, 0);
            PlaceHigh(0, .6f); yield return null;
            yield return Measure("full roll with 20 % pitch leaking in (diagonal)", new Vector2(1f, -.2f), .5f, 3f);
            PlaceHigh(0, .6f); yield return null;
            yield return Measure("yaw RB", Vector2.zero, 1f, 3f, 1f);
            GameSettings.S.flightAssist = 0;
        }

        IEnumerator HoverRelease()
        {
            var s = G.Ship; var p = s.transform.position; p.y = 2600f;
            s.Spawn(p, Quaternion.identity, CrashLevel.Ghost, .3f, 0); s.ResetTo(p, 0); G.Rig.Snap(); yield return null;
            yield return new WaitForSecondsRealtime(1f);
            Log("hover (landing mode), high up:");
            yield return Measure("turn full stick right 2 s, then release", new Vector2(1, 0), 2f, 3f, 0, true);
            yield return Measure("forward full stick 2 s, then release", new Vector2(0, 1), 2f, 3f, 0, true);
            Log($"  hover drift after the forward push: horizontal speed now {new Vector2(s.Velocity.x, s.Velocity.z).magnitude:0.0} m/s");
            // flight -> hover: the body eases from the flight attitude
            PlaceHigh(0, .6f); s.transform.rotation = Quaternion.Euler(-20, 0, -30); yield return null;
            s.ResetMoves(); float h0 = s.Heading;
            yield return Press(GamepadButton.West);
            yield return Measure("air brake from a 20° climb, 30° bank", Vector2.zero, 0, 4f);
        }

        // A glancing hit on a canyon wall (Bounce): does the nose spin?
        IEnumerator WallHit(float into)
        {
            var s = G.Ship; var f = s.Field; f.NearestPad(s.transform.position, out var pad);
            Vector3 c = pad.Position, wall = Vector3.zero, nh = Vector3.zero; bool found = false;
            for (int a = 0; a < 360 && !found; a += 15)
            {
                var dir = Quaternion.Euler(0, a, 0) * Vector3.forward;
                for (float d = 10; d < 300; d += 3)
                {
                    var q = c + dir * d;
                    if (f.SurfaceHeight(q.x, q.z) > c.y + 40f)
                    {
                        wall = q; wall.y = c.y + 18f; f.SurfaceHeight(q.x, q.z, out var n); nh = new Vector3(n.x, 0, n.z).normalized;
                        if (Vector3.Dot(nh, -dir) < .3f) nh = -dir;
                        found = true; break;
                    }
                }
            }
            if (!found) { Log("wall hit: no wall found near the pad"); yield break; }
            var tg = Vector3.Cross(Vector3.up, nh).normalized;
            var fly = (-nh * Mathf.Sin(into * Mathf.Deg2Rad) + tg * Mathf.Cos(into * Mathf.Deg2Rad)).normalized;
            var start = wall + nh * 1f - fly * 90f; start.y = Mathf.Max(wall.y, f.SurfaceHeight(start.x, start.z) + 12f);
            s.Spawn(start, Quaternion.LookRotation(fly), CrashLevel.Bounce, .5f, 0); G.Rig.Snap(); yield return null;
            int hits = s.Hits; s.ResetMoves(); float h0 = s.Heading, t0 = 0;
            while (t0 < 6f && s.Hits == hits) { Set(); yield return null; t0 += ShipDt; }
            float hHit = s.Heading; var atHit = (float[])s.Moved.Clone();
            float tt = 0; while (tt < 1.5f) { Set(); yield return null; tt += ShipDt; }
            Log($"wall hit at {into:0}° into the wall (Bounce), stick centred: {(s.Hits > hits ? $"hit after {t0:0.0} s, {s.Hits - hits} hits" : "NO HIT")}; heading {Mathf.DeltaAngle(h0, s.Heading):+0.0;-0.0}° " +
                $"(the wall's line is {Mathf.DeltaAngle(h0, Mathf.Atan2(tg.x, tg.z) * Mathf.Rad2Deg):+0;-0}° from the start heading); turned by: {MovedText(s)}; now {Vector3.Dot(s.transform.position - wall, nh):0} m off the wall");
        }

        // ---- the pickup, as a player does it

        string PilotLine(DownedPilot p)
        {
            var st = G.Ship.transform; Vector3 door = st.TransformPoint(DownedPilot.DoorOutside); door.y = G.Ship.Field.SurfaceHeight(door.x, door.z);
            var d = door - p.Position;
            var l = st.InverseTransformPoint(p.Position);
            return $"{p.Now}, to the door {new Vector2(d.x, d.z).magnitude:0.0} m level / door {d.y:+0.0;-0.0} m above the pilot (pickup range {Tuning.T.pickupRange:0} m level, climb {Tuning.T.pilotMaxClimb:0} m; can reach {p.CanReach}), pilot at ship x {l.x:+0;-0} z {l.z:+0;-0}, knocks {p.Knocks}";
        }

        IEnumerator WatchPilot(DownedPilot p, float seconds)
        {
            float end = Time.realtimeSinceStartup + seconds, next = 0;
            while (Time.realtimeSinceStartup < end && !p.AtDoor && p.Now != DownedPilot.State.Dead)
            {
                if (Time.realtimeSinceStartup > next) { next = Time.realtimeSinceStartup + 4f; Log($"    {Time.realtimeSinceStartup - end + seconds:0} s: pilot {PilotLine(p)}; advice \"{G.Displays.Prompt}\""); }
                Set(); yield return null;
            }
        }

        IEnumerator PlayerPickup(DownedPilot pilot, int n)
        {
            var sp = G.Ship;
            Log($"pickup {n}: pilot {pilot.Index + 1}, {Vector3.Distance(pilot.LandingSpot, sp.transform.position):0} m away");
            float end = Time.realtimeSinceStartup + 150f;
            while (Time.realtimeSinceStartup < end && G.Now == GameShell.State.Flying && G.Displays.Cue == "")
            {
                var t = sp.transform; var to = pilot.LandingSpot - t.position; to.y = 0;   // to the pad, as the green ring shows
                if (to.magnitude < 30f) break;
                float err = Mathf.DeltaAngle(sp.Heading, Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg), bank = BankOf(t);
                float roll = Mathf.Clamp((Mathf.Clamp(err * 1.5f, -50, 50) - bank) * .04f, -1, 1);
                float high = 0; var dir = to.normalized;
                for (int m = 0; m <= 10; m++) { var q = t.position + dir * (m * 60f); high = Mathf.Max(high, sp.Field.Height(q.x, q.z)); }
                float climb = Mathf.Clamp((high + 70f - t.position.y) * .02f, -.45f, .8f), pitch = Mathf.Clamp((climb - t.forward.y) * 3f, -1, 1);
                state.leftStick = new Vector2(Firm(roll), -Firm(pitch)); state.rightTrigger = to.magnitude > 2500f ? 1 : 0; Set();
                yield return null;
            }
            state.leftStick = Vector2.zero; state.rightTrigger = 0; Set();
            Log($"  cue \"{G.Displays.Cue}\" at {Flat(pilot.Position - sp.transform.position).magnitude:0} m from the pilot");
            yield return Press(GamepadButton.West);   // X, as the cue says
            yield return WaitFor(() => new Vector2(sp.Velocity.x, sp.Velocity.z).magnitude < .3f, 20f);
            Log($"  stopped {Flat(pilot.Position - sp.transform.position).magnitude:0} m from the pilot, {Flat(pilot.LandingSpot - sp.transform.position).magnitude:0} m from the pad's centre, {sp.AboveGround:0} m up");
            Log($"  belly camera in hover: on {G.Belly.On} ({G.Belly.Title}), stop ring shown {G.StopMarkerAt != Vector3.zero}");
            yield return Shot($"p{n}_hover_belly_cam");
            if (n == 1)
            {
                // the landing view (Y cycles: chase, close chase, canyon, landing, cockpit)
                for (int k = 0; k < 4; k++) yield return Press(GamepadButton.North);
                yield return new WaitForSecondsRealtime(.6f); Log($"  view: {CameraRig.ModeNames[(int)G.Rig.Current]}"); yield return Shot("p1_landing_view");
                yield return Press(GamepadButton.North);
            }
            // straight down where it stopped (a player sets down under the ring)
            float t1 = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup < t1 + 90f && !sp.Landed && G.Now == GameShell.State.Flying)
            { state.leftTrigger = sp.AboveGround > 25f ? 1f : .5f; Set(); yield return null; }
            state.leftTrigger = 0; Set();
            sp.Field.SurfaceHeight(sp.transform.position.x, sp.transform.position.z, out var gn);
            Log($"  set straight down: landed {sp.Landed} ({sp.Notice}), ground slope {Vector3.Angle(gn, Vector3.up):0}°, heading {sp.Heading:0}; pilot {PilotLine(pilot)}");
            if (!sp.Landed) { Log("  not landed: moving over the pad as a player would"); yield return HoverLand(pilot.LandingSpot); Log($"  landed {sp.Landed}; pilot {PilotLine(pilot)}"); }
            yield return new WaitForSecondsRealtime(1f);
            if (n == 1 && sp.Landed)
            {
                // landed: look down through the glass floor and the lower canopy (right stick)
                yield return LookAt(sp.transform.position + Vector3.down * 4f + sp.transform.forward * 3f, 1.6f);
                yield return Shot("p1_glass_floor_look_down");
                // what is drawn under the floor: the same view with the glass hidden, then with the whole ship hidden (debug pictures)
                var rs = sp.GetComponentsInChildren<Renderer>(); var glassOnes = new List<Renderer>();
                foreach (var r in rs) if (r.enabled && r.sharedMaterial && r.sharedMaterial.shader.name == "HostileHorizon/Glass") { glassOnes.Add(r); r.enabled = false; }
                yield return Shot("p1_debug_no_glass"); foreach (var r in glassOnes) r.enabled = true;
                var all = new List<Renderer>(); foreach (var r in rs) if (r.enabled && !(r is CanvasRenderer)) { all.Add(r); r.enabled = false; }
                yield return Shot("p1_debug_no_ship"); foreach (var r in all) r.enabled = true;
                string names = ""; foreach (var r in rs) if (r.enabled) names += r.name + "(" + (r.sharedMaterial ? r.sharedMaterial.shader.name : "-") + ") ";
                Log($"  ship renderers: {names}");
                yield return LookAt(sp.transform.position + Vector3.down * 4f - sp.transform.right * 6f + sp.transform.forward * 3f, 1.4f);
                yield return Shot("p1_lower_canopy_look_left");
                state.rightStick = Vector2.zero; Set(); yield return new WaitForSecondsRealtime(1f);
            }
            if (n == 3)
            {
                // a pilot who can't get to the ship says so (made unreachable for a moment: no climb allowed)
                float keep = Tuning.T.pilotMaxClimb; Tuning.T.pilotMaxClimb = -1; yield return null; yield return null;
                Log($"  (unreachable for a moment) advice \"{G.Displays.Prompt}\""); yield return Shot("p3_cant_reach_advice");
                Tuning.T.pilotMaxClimb = keep; yield return null;
            }
            yield return Press(GamepadButton.South);   // engines off (A)
            Log($"  engines off: {!sp.EnginesOn}; advice \"{G.Displays.Prompt}\"; state line \"{G.Displays.StateText}\"; pilot {PilotLine(pilot)}");
            float runFrom = Time.realtimeSinceStartup;
            yield return WatchPilot(pilot, 70f);
            Log($"  pilot ran {Time.realtimeSinceStartup - runFrom:0.0} s to the door ({pilot.Now})");
            if (pilot.Now == DownedPilot.State.Waiting)
            {
                Log($"  the pilot did not come: {PilotLine(pilot)}. Moving closer, as a player would.");
                yield return Press(GamepadButton.South);   // engines on
                state.rightTrigger = 1; Set(); yield return WaitFor(() => sp.AboveGround > 15f, 10f); state.rightTrigger = 0; Set();
                yield return HoverLand(pilot.LandingSpot);
                yield return new WaitForSecondsRealtime(1f);
                yield return Press(GamepadButton.South);
                yield return WatchPilot(pilot, 70f);
            }
            if (pilot.AtDoor)
            {
                yield return new WaitForSecondsRealtime(1.5f);
                var k = pilot.LastKnock; var head = G.Rig.Head.transform.position;
                float dist = k ? Vector3.Distance(k.transform.position, head) : -1;
                Log($"  KNOCKING: {pilot.Knocks} knocks played; last knock source {(k ? $"playing {k.isPlaying}, volume {k.volume}, {dist:0.0} m from the listener, min distance {k.minDistance} -> heard at {k.volume * Mathf.Min(1, k.minDistance / Mathf.Max(dist, .01f)):0.00} of full (before the master volume)" : "gone")}; " +
                    $"player build muted? {(TestRun.Muted ? "this check run is (test flag)" : "no")}; advice \"{G.Displays.Prompt}\"; state line \"{G.Displays.StateText}\"");
                Log($"  can the pilot be seen from the cockpit? eye to pilot {Vector3.Distance(head, pilot.Position):0.0} m, {Vector3.Angle(sp.transform.forward, pilot.Position - head):0}° off the nose");
                Log($"  displays: airlock button lit {G.Displays.AirlockLit}, camera {G.Belly.Title} on {G.Belly.On}, pickup line \"{G.Displays.PickupLine}\"");
                yield return Shot($"p{n}_at_door_cockpit");
                if (n == 1) { yield return Press(GamepadButton.North); yield return Press(GamepadButton.North); yield return Press(GamepadButton.North); yield return Press(GamepadButton.North); yield return new WaitForSecondsRealtime(.5f); yield return Shot("p1_at_door_landing_view"); yield return Press(GamepadButton.North); }
                if (n == 1) yield return MeasureFps("landed, the airlock camera on");
                yield return WaitFor(() => G.Displays.Knocking, 8f);
                Log($"  knock shown on the displays: {G.Displays.Knocking}"); yield return Shot($"p{n}_knock_knock");
                yield return Press(GamepadButton.East);
                yield return WaitPhase(() => pilot.Now == DownedPilot.State.Aboard, 15f, "boarding");
                Log($"  airlock (B): pilot {pilot.Now}; message \"{G.Displays.Prompt}\"");
            }
            else Log($"  NO KNOCK: {PilotLine(pilot)}");
            // up and on to the next
            if (!sp.EnginesOn) yield return Press(GamepadButton.South);
            state.rightTrigger = 1; Set(); yield return WaitFor(() => sp.AboveGround > 80f, 15f); state.rightTrigger = 0; Set();
            yield return SetMode(FlightMode.Flight);
        }

        IEnumerator Fix25()
        {
            StartCoroutine(Progress());
            yield return WaitFor(() => G.Now == GameShell.State.Title, 60f);
            yield return new WaitForSecondsRealtime(1.5f);
            var tu = Tuning.T;
            Log($"tuning: pitch {tu.pitchRate} / roll {tu.rollRate} / yaw {tu.yawRate} °/s; bank turn Full {tu.bankTurnFull} Light {tu.bankTurnLight}; levelling Full {tu.levelRateFull} Light {tu.levelRateLight}; stick curve {tu.stickCurve}; settings deadzone {GameSettings.S.deadzone}%, assist {GameSettings.AssistNames[GameSettings.S.flightAssist]}");

            if (LauncherBridge.Argument("-fix25From") == "c") { yield return FastForwardCheck(true); yield break; }
            // ---- (a) uncommanded movement (-fix25From b: start at the pickups; c: only the fast-forward)
            if (LauncherBridge.Argument("-fix25From") != "b") {
            yield return StartFromTitle(0);   // Ghost
            yield return Fly(1f);
            yield return Uncommanded("Light (default)", 1);
            yield return Uncommanded("Full", 0);
            yield return Uncommanded("Off", 2);
            yield return HoverRelease();
            yield return PauseToTitle(); yield return StartFromTitle(1);   // Bounce
            yield return WallHit(15f); yield return WallHit(35f);
            yield return PauseToTitle();
            }

            // ---- (b) the pickup as a player does it
            yield return Nav(GamepadButton.DpadDown, 1); yield return Press(GamepadButton.South);   // Campaign
            yield return Nav(GamepadButton.DpadDown, 1); yield return Press(GamepadButton.South);   // New career
            yield return WaitPhase(() => G.Now == GameShell.State.Docked, 60f, "the hangar");
            G.Menu.Current.Selected = 0; yield return Press(GamepadButton.South);                  // Launch
            yield return WaitPhase(() => G.Now == GameShell.State.Flying, 40f, "flight after launch");
            var tried = new HashSet<int>();
            for (int i = 1; i <= 3 && G.Now == GameShell.State.Flying; i++)
            {
                DownedPilot p = null; float bd = float.MaxValue;
                foreach (var q in G.Pilots) if (q.OnFoot && !tried.Contains(q.Index)) { float d = Vector3.Distance(q.LandingSpot, G.Ship.transform.position); if (d < bd) { bd = d; p = q; } }
                if (p == null) break;
                tried.Add(p.Index);
                if (i > 1)
                {
                    // between pilots: placed 1.5 km out, 250 m over the pad, heading for it (getting about is not what this measures)
                    var to = Flat(p.LandingSpot - G.Ship.transform.position).normalized; if (to.sqrMagnitude < .5f) to = Vector3.forward;
                    var at = p.LandingSpot - to * 1500f; float top = 0; for (int m = 0; m <= 30; m++) { var q = at + to * (m * 50f); top = Mathf.Max(top, G.Ship.Field.Height(q.x, q.z)); } at.y = top + 120f;   // over the highest ground on the way
                    G.Ship.Spawn(at, Quaternion.LookRotation(to), G.CampaignLevel, .6f, G.Ship.Damage); G.Rig.Snap(); yield return new WaitForSecondsRealtime(1f);
                }
                yield return PlayerPickup(p, i);
            }
            Log($"cabin {G.CabinCount}");

            // ---- docking time, the launch warning
            // up to the docking height in landing mode (straight up: no canyon wall in the way)
            yield return SetMode(FlightMode.Hover); yield return WaitFor(() => !G.Ship.Braking, 10f);
            float upEnd = Time.realtimeSinceStartup + 150f;
            while (Time.realtimeSinceStartup < upEnd && G.Now == GameShell.State.Flying) { state.rightTrigger = 1; Set(); yield return null; }
            state.rightTrigger = 0; Set();
            yield return WaitPhase(() => G.Phase.StartsWith("dock"), 30f, "docking");
            yield return WaitPhase(() => G.Now == GameShell.State.Docked && G.Phase == "docked", 120f, "docked");
            Log($"docking (climbed to the docking height): {G.LiftEnded - G.LiftStarted:0.0} s from the autopilot taking over to the cradle; {G.Debrief.Replace("\n", " / ")}");
            Career.C.Current.energy = 22; G.Ship.SetEnergyAndDamage(22, Career.C.Current.damage);
            yield return null; yield return Shot("h01_hangar_refuel_reminder");
            Log($"hangar at 22 %: \"{string.Join(" | ", Array.FindAll((G.Menu.Current.Body?.Invoke() ?? "").Split('\n'), l => l.Contains("Refuel")))}\"");
            G.Menu.Current.Selected = 0; yield return Press(GamepadButton.South);                  // Launch
            Log($"Launch at 22 %: page \"{G.Menu.Current?.Title}\", body \"{G.Menu.Current?.Body?.Invoke().Replace("\n", " ")}\", rows {string.Join(" / ", G.Menu.Current.Rows.ConvertAll(r => r.Label()))}, selected {G.Menu.Current.Rows[G.Menu.Current.Selected].Label()}");
            yield return Shot("h02_low_energy_question");
            int cr = Career.C.credits;
            yield return Press(GamepadButton.South);                                                  // Refuel and launch (the default)
            yield return WaitPhase(() => G.Now == GameShell.State.Flying, 40f, "flight after launch");
            Log($"Refuel and launch: energy {G.Ship.Energy:0} %, credits {cr} -> {Career.C.credits}, state {G.Now}");

            // ---- the lift: recall from the ground and from the air, the mid-air catch, the tow; then the same with 0.2.4's numbers
            yield return LiftTimes("new");
            SetOldLift(true); yield return LiftTimes("0.2.4's numbers"); SetOldLift(false);
            yield return FastForwardCheck(false);
        }

        // Fix 2.5 (Dan's ask): hold A during docking and the recall lift: it plays live, 4x faster. Timed in real seconds.
        IEnumerator FastForwardCheck(bool fresh)
        {
            var sp = G.Ship;
            if (fresh)
            {
                yield return Nav(GamepadButton.DpadDown, 1); yield return Press(GamepadButton.South);   // Campaign
                yield return Nav(GamepadButton.DpadDown, 1); yield return Press(GamepadButton.South);   // New career
                yield return WaitPhase(() => G.Now == GameShell.State.Docked, 60f, "the hangar");
            }
            else yield return WaitPhase(() => G.Now == GameShell.State.Docked && G.Phase == "docked", 120f, "the hangar");
            G.Menu.Current.Selected = 0; yield return Press(GamepadButton.South);
            if (G.Menu.Current != null && G.Menu.Current.Title == "LOW ENERGY") yield return Press(GamepadButton.South);
            // the launch, holding A
            float r0 = Time.realtimeSinceStartup;
            state = state.WithButton(GamepadButton.South, true); Set();
            yield return WaitPhase(() => G.Now == GameShell.State.Flying, 40f, "flight after launch");
            state = state.WithButton(GamepadButton.South, false); Set(); yield return null; yield return null;
            Log($"fast-forward: launch holding A took {Time.realtimeSinceStartup - r0:0.0} s real (6 s of game time at normal speed); time scale in flight after: {Time.timeScale}");
            // up to the docking height in hover, then hold A through the docking
            yield return SetMode(FlightMode.Hover); yield return WaitFor(() => !sp.Braking, 10f);
            float upEnd = Time.realtimeSinceStartup + 150f;
            while (Time.realtimeSinceStartup < upEnd && G.Now == GameShell.State.Flying) { state.rightTrigger = 1; Set(); yield return null; }
            state.rightTrigger = 0; Set();
            yield return WaitPhase(() => G.Phase.StartsWith("dock"), 30f, "docking");
            StartCoroutine(WatchCamera());
            r0 = Time.realtimeSinceStartup; state = state.WithButton(GamepadButton.South, true); Set();
            yield return new WaitForSecondsRealtime(.5f);
            Log($"  holding A while docking: time scale {Time.timeScale}, prompt \"{G.Displays.Prompt}\""); yield return Shot("ff1_docking_fast_forward");
            yield return WaitPhase(() => G.Now == GameShell.State.Docked && G.Phase == "docked", 120f, "docked");
            state = state.WithButton(GamepadButton.South, false); Set(); yield return null; yield return null;
            Log($"fast-forward: docking holding A took {Time.realtimeSinceStartup - r0:0.0} s real ({G.LiftEnded - G.LiftStarted:0.0} s of game time); time scale after: {Time.timeScale}; {Continuity()}");
            // the recall from the air, holding A through the lift
            G.Menu.Current.Selected = 0; yield return Press(GamepadButton.South);
            if (G.Menu.Current != null && G.Menu.Current.Title == "LOW ENERGY") yield return Press(GamepadButton.South);
            yield return WaitPhase(() => G.Now == GameShell.State.Flying, 40f, "flight after launch");
            yield return new WaitForSecondsRealtime(1f);
            yield return ResetVia("for the recall", 1);
            sp.Field.NearestPad(sp.transform.position, out var pad); yield return HoverLand(pad.Position);
            yield return HoldRecall(1.4f);
            yield return WaitPhase(() => G.Phase == "tow-lift", 120f, "the beam");
            StartCoroutine(WatchCamera());
            r0 = Time.realtimeSinceStartup; state = state.WithButton(GamepadButton.South, true); Set();
            yield return WaitPhase(() => G.Now == GameShell.State.Docked && G.Phase == "docked", 120f, "docked");
            state = state.WithButton(GamepadButton.South, false); Set(); yield return null;
            Log($"fast-forward: recall lift from the ground holding A took {Time.realtimeSinceStartup - r0:0.0} s real ({G.LiftEnded - G.LiftStarted:0.0} s of game time); {Continuity()}");
        }

        void SetOldLift(bool old)
        {
            var t = Tuning.T;
            if (old) { t.motherComeSpeed = 140; t.motherComeAccel = 12; t.beamLiftSpeed = 45; t.beamLiftAccel = 4; t.dockApproachSpeed = 90; t.dockApproachAccel = 15; t.dockFinalSpeed = 70; t.dockFinalAccel = 12; t.bayRiseFrom = 60; t.bayRiseSpeed = 7; t.bayRiseBrake = 1.2f; t.towBeamDelay = 1.5f; }
            else { var d = new Tuning(); t.motherComeSpeed = d.motherComeSpeed; t.motherComeAccel = d.motherComeAccel; t.beamLiftSpeed = d.beamLiftSpeed; t.beamLiftAccel = d.beamLiftAccel; t.dockApproachSpeed = d.dockApproachSpeed; t.dockApproachAccel = d.dockApproachAccel; t.dockFinalSpeed = d.dockFinalSpeed; t.dockFinalAccel = d.dockFinalAccel; t.bayRiseFrom = d.bayRiseFrom; t.bayRiseSpeed = d.bayRiseSpeed; t.bayRiseBrake = d.bayRiseBrake; t.towBeamDelay = d.towBeamDelay; }
        }

        // Times: the call until the beam catches (the mothership coming), and the lift from there into the cradle.
        IEnumerator LiftTimes(string name)
        {
            var sp = G.Ship;
            // recall from the ground: Reset position (60 m over the nearest pad), set down there
            yield return new WaitForSecondsRealtime(1f);
            yield return ResetVia("for the lift test", 1);
            sp.Field.NearestPad(sp.transform.position, out var pad); yield return HoverLand(pad.Position);
            float gnd = sp.Altitude, mDist = Flat(G.Mother.transform.position - sp.transform.position).magnitude;
            StartCoroutine(WatchCamera());
            float t0 = Time.time; yield return HoldRecall(1.4f);
            yield return WaitPhase(() => G.Phase == "tow-lift", 120f, "the beam");
            float caught = Time.time - t0;
            yield return WaitPhase(() => G.Now == GameShell.State.Docked && G.Phase == "docked", 180f, "docked");
            Log($"lift ({name}) recall from the ground ({gnd:0} m up, the mothership {mDist:0} m away level): it came in {caught:0.0} s, lift from the ground into the cradle {G.LiftEnded - G.LiftStarted:0.0} s; {Continuity()}");
            // the mid-air catch: run dry 300 m over the ground
            G.Menu.Current.Selected = 0; yield return Press(GamepadButton.South);
            if (G.Menu.Current != null && G.Menu.Current.Title == "LOW ENERGY") yield return Press(GamepadButton.South);   // Refuel and launch
            yield return WaitPhase(() => G.Now == GameShell.State.Flying, 40f, "flight after launch");
            yield return SetMode(FlightMode.Hover); yield return WaitFor(() => !sp.Braking, 10f);
            float dEnd = Time.realtimeSinceStartup + 90f;
            while (Time.realtimeSinceStartup < dEnd && sp.AboveGround > 320f && G.Now == GameShell.State.Flying) { state.leftTrigger = 1; Set(); yield return null; }
            state.leftTrigger = 0; Set();
            float agl0 = sp.AboveGround; mDist = Flat(G.Mother.transform.position - sp.transform.position).magnitude;
            StartCoroutine(WatchCamera());
            sp.Energy = .05f; t0 = Time.time; float lowest = 9999;
            while (G.Phase != "tow-lift" && Time.time < t0 + 120f && !sp.Crashed && G.Now != GameShell.State.Docked) { lowest = Mathf.Min(lowest, sp.AboveGround); Set(); yield return null; }
            caught = Time.time - t0;
            yield return WaitPhase(() => G.Now == GameShell.State.Docked && G.Phase == "docked" || G.Now == GameShell.State.Crashed, 180f, "docked");
            Log($"lift ({name}) mid-air catch (ran dry {agl0:0} m over the ground, the mothership {mDist:0} m away): caught after {caught:0.0} s at {lowest:0} m over the ground{(sp.Landed ? " (it had landed)" : "")}, lift into the cradle {G.LiftEnded - G.LiftStarted:0.0} s; {Continuity()}");
            // the tow from the ground
            G.Menu.Current.Selected = 0; yield return Press(GamepadButton.South);
            if (G.Menu.Current != null && G.Menu.Current.Title == "LOW ENERGY") yield return Press(GamepadButton.South);   // Refuel and launch
            yield return WaitPhase(() => G.Now == GameShell.State.Flying, 40f, "flight after launch");
            yield return new WaitForSecondsRealtime(1f);
            yield return ResetVia("for the tow test", 1);
            sp.Field.NearestPad(sp.transform.position, out var pad2); yield return HoverLand(pad2.Position);
            sp.Energy = .2f;
            yield return WaitPhase(() => G.Menu.Current != null && G.Menu.Current.Title == "OUT OF ENERGY", 30f, "out of energy on the ground");
            mDist = Flat(G.Mother.transform.position - sp.transform.position).magnitude;
            StartCoroutine(WatchCamera());
            t0 = Time.time; yield return Press(GamepadButton.South);
            yield return WaitPhase(() => G.Phase == "tow-lift", 120f, "the tow beam");
            caught = Time.time - t0;
            yield return WaitPhase(() => G.Now == GameShell.State.Docked && G.Phase == "docked", 180f, "docked after the tow");
            Log($"lift ({name}) tow from the ground (the mothership {mDist:0} m away): it came in {caught:0.0} s, lift into the cradle {G.LiftEnded - G.LiftStarted:0.0} s; {Continuity()}");
            Career.C.Current.energy = 100; G.Ship.SetEnergyAndDamage(100, 0); Career.C.Current.damage = 0;
            G.Menu.Current.Selected = 0; yield return Press(GamepadButton.South);
            if (G.Menu.Current != null && G.Menu.Current.Title == "LOW ENERGY") yield return Press(GamepadButton.South);   // Refuel and launch
            yield return WaitPhase(() => G.Now == GameShell.State.Flying, 40f, "flight after launch");
        }
    }
}
