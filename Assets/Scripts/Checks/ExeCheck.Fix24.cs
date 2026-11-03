using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

namespace HostileHorizon
{
    // Fix 2.4 check, in the built exe:  HostileHorizon.exe -exeCheck <dir> -exeCheckOnly fix24
    // Free Flight (Ghost, so rock never gets in the way of a measurement): turn and response numbers with 0.2.3's handling and with
    // each Flight assist; the air brakes from top speed (0.2.3's brake and the new one) against the predicted stop point; hover descent
    // from 1000 m (0.2.3's sink and the new); pause with Start and a tap of View; a simulated focus loss; Reset position from high up
    // and from low in a canyon; cockpit pictures and the frame rate flying the new canyons. Campaign (save in <dir>/save): pause during
    // the launch; to a pilot with the stop ring and the SLOW DOWN cue; brake, hover, set down (the landing states shown); Reset refused
    // while the pilot runs; pause during docking, the recall lift and the tow. Every press goes through the virtual gamepad.
    public sealed partial class ExeCheck
    {
        static float BankOf(Transform t) => Mathf.Asin(Mathf.Clamp(-t.right.y, -1, 1)) * Mathf.Rad2Deg;

        void PlaceHigh(float heading, float throttle)
        {
            var s = G.Ship; var p = s.transform.position; p.y = 2600f;
            s.Spawn(p, Quaternion.Euler(0, heading, 0), CrashLevel.Ghost, throttle, 0);
            G.Rig.Snap();
        }

        // Hold an input until a condition or a time limit; seconds of game time taken.
        IEnumerator Hold(Func<Vector2> stick, Func<bool> done, float limit, Action<float> result, float rb = 0)
        {
            float t = 0; bool ok = false;
            while (t < limit && !(ok = done()))
            {
                state.leftStick = stick(); state = state.WithButton(GamepadButton.RightShoulder, rb > 0); Set();
                yield return null; t += ShipDt;
            }
            state.leftStick = Vector2.zero; state = state.WithButton(GamepadButton.RightShoulder, false); Set(); yield return null;
            result(ok ? t : -1);
        }
        // the ship's own clock: it never steps more than 1/20 s a frame (ShipController), so a slow frame can't skew a timing
        static float ShipDt => Mathf.Min(Time.deltaTime, 1f / 20f);
        static string T(float t) => t < 0 ? "never" : $"{t:0.00} s";

        IEnumerator Maneuvers(string name, ShipController.Feel? over, int assist)
        {
            var s = G.Ship; GameSettings.S.flightAssist = assist; s.FeelOverride = over; var f = s.CurrentFeel;
            float roll60 = 0, turnRoll = 0, turnPull = 0, pitch30 = 0, level = 0, yawRate, h0 = 0, turned = 0, last = 0;
            Func<bool> Turned90 = () => { turned += Mathf.Abs(Mathf.DeltaAngle(last, s.Heading)); last = s.Heading; return turned >= 90f; };
            PlaceHigh(0, .6f); yield return null;
            yield return Hold(() => new Vector2(1, 0), () => BankOf(s.transform) >= 60f, 5f, r => roll60 = r);
            PlaceHigh(0, .6f); yield return null; turned = 0; last = s.Heading;
            // the assisted turn: bank to 60 degrees and hold it, no pull (the bank turn alone)
            yield return Hold(() => { float b = BankOf(s.transform); return new Vector2(Mathf.Clamp((60f - b) * .06f, -1, 1), 0); }, Turned90, 20f, r => turnRoll = r);
            PlaceHigh(0, .6f); yield return null; turned = 0; last = s.Heading;
            // a pilot's turn: roll to about 75 degrees of bank and pull
            yield return Hold(() => { float b = BankOf(s.transform); return new Vector2(Mathf.Clamp((75f - b) * .06f, -1, 1), b > 50f ? -1f : 0f); }, Turned90, 20f, r => turnPull = r);
            PlaceHigh(0, .6f); yield return null; h0 = s.Heading;
            yield return Hold(() => Vector2.zero, () => false, 2f, r => { }, 1f);   // RB: yaw right 2 s
            yawRate = Mathf.Abs(Mathf.DeltaAngle(h0, s.Heading)) / 2f;
            PlaceHigh(0, .6f); yield return null;
            yield return Hold(() => new Vector2(0, -1), () => Mathf.Asin(Mathf.Clamp(s.transform.forward.y, -1, 1)) * Mathf.Rad2Deg >= 30f, 5f, r => pitch30 = r);
            PlaceHigh(0, .6f); s.transform.rotation = Quaternion.Euler(0, 0, -45f); yield return null;
            yield return Hold(() => Vector2.zero, () => Mathf.Abs(BankOf(s.transform)) < 5f, 10f, r => level = r);
            Log($"flight {name}: rates pitch {f.pitch:0} / roll {f.roll:0} / yaw {f.yaw:0} °/s, bank turn {f.bankTurn:0}, levelling {f.level:0} °/s -> " +
                $"roll to 60° {T(roll60)}; 90° turn, 60° bank held, no pull {T(turnRoll)}; 90° turn, bank and pull {T(turnPull)}; yaw (RB) {yawRate:0} °/s; nose up 30° {T(pitch30)}; wings level from 45° {T(level)}");
            s.FeelOverride = null; GameSettings.S.flightAssist = 0;
        }

        // From top speed, high up: X, then until stopped. Predicted stop point (at the press) against where it stopped.
        IEnumerator BrakeTest(string name)
        {
            var s = G.Ship; PlaceHigh(90, 1f);
            yield return Hold(() => Vector2.zero, () => false, .5f, r => { });
            Vector3 p0 = s.transform.position, predicted = s.StopPoint; float v0 = s.Speed;
            float t = 0, tHover = -1;
            state = state.WithButton(GamepadButton.West, true); Set(); yield return null; t += ShipDt;
            state = state.WithButton(GamepadButton.West, false); Set();
            while (t < 30f && new Vector2(s.Velocity.x, s.Velocity.z).magnitude > .3f)
            {
                if (tHover < 0 && new Vector2(s.Velocity.x, s.Velocity.z).magnitude <= Tuning.T.hoverMaxSpeed + 1f) tHover = t;
                yield return null; t += ShipDt;
            }
            Vector3 p1 = s.transform.position; float run = new Vector2(p1.x - p0.x, p1.z - p0.z).magnitude, err = new Vector2(p1.x - predicted.x, p1.z - predicted.z).magnitude;
            Log($"brake {name} (air brake {Tuning.T.airBrake:0} m/s², hover brake {Tuning.T.hoverBrake:0}): from {v0 * 3.6f:0} km/h, mode {s.Mode} -> hover speed in {T(tHover)}, stopped in {t:0.0} s after {run:0} m; predicted stop point {err:0.0} m from where it stopped ({(err <= 50f ? "within 50 m" : "OFF BY MORE THAN 50 m")})");
            yield return SetMode(FlightMode.Flight);
        }

        // Over the nearest pad, 1000 m up, hover, full down (LT) until landed (or 5 m over the ground for 0.2.3's: it would hit hard).
        IEnumerator DescentTest(string name, bool old)
        {
            var s = G.Ship; var f = s.Field; f.NearestPad(s.transform.position, out var pad);
            s.Spawn(pad.Position + Vector3.up * 1000f, Quaternion.identity, CrashLevel.Ghost, .3f, 0); s.ResetTo(pad.Position + Vector3.up * 1000f, 0); G.Rig.Snap();
            yield return null;
            float t = 0, fastest = 0, touch = 0;
            while (t < 180f && !s.Landed && !(old && s.AboveGround < 5f))
            {
                state.leftTrigger = 1; Set(); touch = -s.Velocity.y; fastest = Mathf.Max(fastest, touch);
                yield return null; t += ShipDt;
            }
            state.leftTrigger = 0; Set();
            Log($"hover descent {name}: 1000 m to {(s.Landed ? "landed" : $"{s.AboveGround:0} m over the ground")} in {t:0.0} s; fastest sink {fastest:0.0} m/s, at the ground {touch:0.0} m/s (safe touchdown {Tuning.T.safeTouchdownSpeed} m/s); {s.Notice}; hits {s.Hits}");
            yield return new WaitForSecondsRealtime(.5f);
        }

        void SetOldLanding(bool old)
        {
            var t = Tuning.T;
            if (old) { t.airBrake = t.hoverBrake; t.hoverSinkHigh = t.hoverSinkMid = t.hoverSinkLow = t.hoverClimbRate; t.hoverVerticalAccel = 10; }
            else { t.airBrake = 42; t.hoverSinkHigh = 30; t.hoverSinkMid = 14; t.hoverSinkLow = 3; t.hoverVerticalAccel = 15; }
        }

        string Where => $"pos ({G.Ship.transform.position.x:0}, {G.Ship.transform.position.y:0}, {G.Ship.transform.position.z:0}), gnd {G.Ship.AboveGround:0} m, {G.Ship.Mode}, speed {G.Ship.Speed:0.0} m/s, engines {(G.Ship.EnginesOn ? "on" : "off")}, energy {G.Ship.Energy:0.0}%";

        IEnumerator ResetVia(string what, int row)
        {
            yield return Press(GamepadButton.Start); yield return WaitFor(() => G.Now == GameShell.State.Paused, 3f);
            string before = Where;
            yield return Nav(GamepadButton.DpadDown, row); yield return Press(GamepadButton.South);   // Reset position
            yield return WaitFor(() => G.Now == GameShell.State.Flying, 3f);
            G.Ship.Field.NearestPad(G.Ship.transform.position, out var pad);
            float off = new Vector2(G.Ship.transform.position.x - pad.x, G.Ship.transform.position.z - pad.z).magnitude;
            Log($"reset position from {what}: before {before} -> after {Where}; {off:0} m from the nearest pad's centre; state {G.Now}");
        }

        // Pause during an automatic part: the ship (and the mothership) must not move while paused, and the part goes on after.
        IEnumerator PauseDuring(string what, string shot, Func<bool> phase, float wait)
        {
            yield return WaitPhase(phase, wait, what);
            yield return Press(GamepadButton.Start);
            yield return WaitFor(() => G.Now == GameShell.State.Paused, 3f);
            Vector3 a = G.Ship.transform.position, m = G.Mother.transform.position; string phaseName = G.Phase;
            yield return new WaitForSecondsRealtime(1.5f);
            if (shot != null) yield return Shot(shot);
            float moved = Vector3.Distance(a, G.Ship.transform.position), mMoved = Vector3.Distance(m, G.Mother.transform.position);
            Log($"pause during {what}: state {G.Now} (from {G.PausedFrom}, phase {phaseName}); in 1.5 s the ship moved {moved:0.00} m, the mothership {mMoved:0.00} m; menu \"{G.Menu.Current?.Title}\"");
            yield return Press(GamepadButton.Start);
            yield return null; yield return null;
            Log($"  resumed: state {G.Now}, phase {G.Phase}");
        }

        IEnumerator Fix24()
        {
            StartCoroutine(Progress());
            yield return WaitFor(() => G.Now == GameShell.State.Title, 60f);
            yield return new WaitForSecondsRealtime(1.5f);
            var tu = Tuning.T;
            Log($"tuning: pitch {tu.pitchRate} / roll {tu.rollRate} / yaw {tu.yawRate} °/s; bank turn Full {tu.bankTurnFull} Light {tu.bankTurnLight}; levelling Full {tu.levelRateFull} Light {tu.levelRateLight}; stick curve {tu.stickCurve}; air brake {tu.airBrake} m/s²; hover sink {tu.hoverSinkHigh} (above {tu.hoverSinkHighAbove} m) / {tu.hoverSinkMid} ({tu.hoverSinkMidAbove} m) / {tu.hoverSinkLow} (below {tu.hoverSinkLowBelow} m); reset {tu.resetHeight} m");
            Log($"settings (defaults): Flight assist {GameSettings.AssistNames[GameSettings.S.flightAssist]}, Stick sensitivity {GameSettings.S.sensitivity}%, HOTAS pause button {GameSettings.S.hotasPauseButton}");
            Determinism(); HeightMap(Seeds.OfTheDay);
            var f0 = new CanyonField(Seeds.OfTheDay); int pads = 0, cells = 0;
            for (int j = -10; j < 10; j++) for (int i = -10; i < 10; i++) { cells++; if (f0.PadAt(i, j).ok) pads++; }
            Log($"landing pads: {pads} of {cells} cells of {CanyonField.PadCell:0} m (10.4 km square) have one");

            // ---- Free Flight
            yield return StartFromTitle(0);   // Ghost
            yield return Fly(1f);
            yield return Shot("f01_cockpit_pause_hint");
            Log($"pause hint shown in the first minute: {G.Displays.PauseHint}");
            yield return Maneuvers("0.2.3 (before)", ShipController.Feel023, 0);
            yield return Maneuvers("Full (new rates)", null, 0);
            yield return Maneuvers("Light (default)", null, 1);
            yield return Maneuvers("Off", null, 2);
            SetOldLanding(true); yield return BrakeTest("0.2.3 (before)"); SetOldLanding(false);
            yield return BrakeTest("new");
            SetOldLanding(true); yield return DescentTest("0.2.3 (before)", true); SetOldLanding(false);
            yield return DescentTest("new", false);
            yield return Shot("f02_landed_free_flight");
            // pause: Start, and a tap of View
            yield return Press(GamepadButton.Start); yield return WaitFor(() => G.Now == GameShell.State.Paused, 3f);
            yield return Shot("f03_pause_free_flight");
            Log($"Start: {G.Now}, menu rows: {string.Join(" / ", G.Menu.Current.Rows.ConvertAll(r => r.Label()))}");
            yield return Press(GamepadButton.East); Log($"B: {G.Now}");
            yield return Press(GamepadButton.Select); Log($"tap of View: {G.Now}");
            yield return Press(GamepadButton.Select); Log($"View again: {G.Now}");
            G.FocusLost(); yield return null; Log($"focus lost (simulated, as a player's build): {G.Now}");
            yield return Press(GamepadButton.Start); Log($"Start: {G.Now}");
            // Reset position: from high up, then from low in a canyon
            G.Ship.Spawn(G.Ship.transform.position + Vector3.up * 1800f, Quaternion.Euler(-20, 40, 0), CrashLevel.Ghost, .8f, 0); yield return null;
            yield return ResetVia("high up", 1);
            yield return Shot("f04_after_reset_high");
            yield return Press(GamepadButton.Start); yield return WaitFor(() => G.Now == GameShell.State.Paused, 3f);
            yield return Nav(GamepadButton.DpadDown, 2); yield return Press(GamepadButton.South);   // Restart (same seed): the canyon at the start
            yield return WaitFor(() => G.Now == GameShell.State.Flying, 30f);
            yield return Fly(6f);
            Log($"in the canyon: {Where}");
            yield return ResetVia("low in a canyon", 1);
            // the new canyons, flown low (Bounce)
            yield return PauseToTitle(); yield return StartFromTitle(1);
            yield return Fly(5f);
            yield return Shot("f05_canyon_cockpit");
            yield return MeasureFps("cockpit, flying low in the new canyons");
            yield return Press(GamepadButton.North); yield return Fly(2.5f); yield return Shot("f06_canyon_chase");
            yield return Press(GamepadButton.North); yield return Press(GamepadButton.North); yield return Fly(2.5f); yield return Shot("f07_canyon_low_view");
            yield return Press(GamepadButton.North); yield return Press(GamepadButton.North);   // Fix 2.5: the Landing view, then the cockpit
            yield return PauseToTitle();

            // ---- Campaign
            yield return Nav(GamepadButton.DpadDown, 1); yield return Press(GamepadButton.South);   // Campaign
            yield return Nav(GamepadButton.DpadDown, 1); yield return Press(GamepadButton.South);   // New career
            yield return WaitPhase(() => G.Now == GameShell.State.Docked, 60f, "the hangar");
            Log($"new career: planet {G.Planet.name}, {G.Pilots.Count} pilots, landing spot to the nearest pad's centre: {string.Join(", ", G.Pilots.ConvertAll(p => { G.Terrain.Field.NearestPad(p.LandingSpot, out var pd); return $"{new Vector2(p.LandingSpot.x - pd.x, p.LandingSpot.z - pd.z).magnitude:0} m"; }))}");
            G.Menu.Current.Selected = 0; yield return Press(GamepadButton.South);                  // Launch
            yield return PauseDuring("the launch", "c01_pause_launch", () => G.Phase == "launch" && G.Mother.DoorsOpen > .98f, 20f);
            yield return WaitPhase(() => G.Now == GameShell.State.Flying, 30f, "flight after launch");

            // to the nearest pilot: the stop ring and the cue, brake, hover, set down
            var pilot = NearestWaiting();
            Log($"to pilot {pilot.Index + 1}: {Vector3.Distance(pilot.LandingSpot, G.Ship.transform.position):0} m");
            float end = Time.realtimeSinceStartup + 150f; bool ringSeen = false;
            while (Time.realtimeSinceStartup < end && G.Now == GameShell.State.Flying && G.Displays.Cue == "")
            {
                var s = G.Ship; var t = s.transform; var to = pilot.Position - t.position; to.y = 0;
                float err = Mathf.DeltaAngle(s.Heading, Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg), bank = BankOf(t);
                float roll = Mathf.Clamp((Mathf.Clamp(err * 1.5f, -50, 50) - bank) * .04f, -1, 1);
                float high = 0; var dir = to.normalized;
                for (int m = 0; m <= 10; m++) { var p = t.position + dir * (m * 60f); high = Mathf.Max(high, s.Field.Height(p.x, p.z)); }
                float climb = Mathf.Clamp((high + 70f - t.position.y) * .02f, -.45f, .8f), pitch = Mathf.Clamp((climb - t.forward.y) * 3f, -1, 1);
                state.leftStick = new Vector2(Firm(roll), -Firm(pitch)); state.rightTrigger = to.magnitude > 2500f ? 1 : 0; Set();
                if (to.magnitude < 120f) break;
                if (!ringSeen && G.StopMarkerAt != Vector3.zero) { ringSeen = true; Log($"  stop ring shown at {to.magnitude:0} m from the pilot"); }
                yield return null;
            }
            state.leftStick = Vector2.zero; state.rightTrigger = 0; Set();
            var sp = G.Ship; float dPilot = new Vector2(pilot.Position.x - sp.transform.position.x, pilot.Position.z - sp.transform.position.z).magnitude;
            Vector3 ring = G.StopMarkerAt, predicted = sp.StopPoint;
            Log($"  cue \"{G.Displays.Cue}\" at {dPilot:0} m from the pilot, {sp.Speed * 3.6f:0} km/h, {sp.AboveGround:0} m over the ground; ring {Vector3.Distance(ring, predicted):0.0} m from the predicted stop point, which is {new Vector2(predicted.x - pilot.Position.x, predicted.z - pilot.Position.z).magnitude:0} m from the pilot; advice \"{G.Displays.Prompt}\"");
            yield return Shot("c02_cue_and_ring");
            var seen = new List<string>(); string lastSeen = "";
            IEnumerator Watch() { while (G.Now == GameShell.State.Flying || G.Now == GameShell.State.Paused) { var st = G.Displays.StateText; string k = new string(st.Where(c => !char.IsDigit(c)).ToArray()); if (st != "" && k != lastSeen) { seen.Add(st.Replace("<b>", "").Replace("</b>", "")); lastSeen = k; } yield return null; } }
            StartCoroutine(Watch());
            ring = G.StopMarkerAt; predicted = sp.StopPoint;
            yield return Press(GamepadButton.West);                                                   // X: brake
            yield return new WaitForSecondsRealtime(.8f);
            yield return Shot("c03_braking");
            yield return Press(GamepadButton.North); yield return new WaitForSecondsRealtime(.4f); yield return Shot("c03b_braking_chase_ring");
            yield return Press(GamepadButton.North); yield return Press(GamepadButton.North); yield return Press(GamepadButton.North); yield return Press(GamepadButton.North);   // back to the cockpit
            yield return WaitFor(() => new Vector2(sp.Velocity.x, sp.Velocity.z).magnitude < .3f, 20f);
            float off = new Vector2(sp.transform.position.x - predicted.x, sp.transform.position.z - predicted.z).magnitude;
            Log($"  braked: stopped {off:0.0} m from the ring's point ({(off <= 50f ? "within 50 m" : "MORE THAN 50 m")}), {new Vector2(pilot.Position.x - sp.transform.position.x, pilot.Position.z - sp.transform.position.z).magnitude:0} m from the pilot, {sp.AboveGround:0} m over the ground");
            yield return Shot("c04_hover");
            float t0 = Time.realtimeSinceStartup;
            yield return HoverLand(pilot.LandingSpot);
            Log($"  set down in {Time.realtimeSinceStartup - t0:0} s (move over the spot and descend): landed {sp.Landed} {Vector3.Distance(sp.transform.position, pilot.LandingSpot):0} m from the spot; {sp.Notice}");
            yield return Shot("c05_touchdown");
            yield return new WaitForSecondsRealtime(2.8f);
            yield return Shot("c06_landed");
            Log($"  landing states shown: {string.Join(" -> ", seen)}");
            // View tap on the ground pauses (it is the recall only when held)
            yield return Press(GamepadButton.Select); yield return null; yield return null;
            Log($"  tap of View on the ground: {G.Now}"); yield return Press(GamepadButton.Start); Log($"  Start: {G.Now}");
            // engines off: the pilot runs; Reset refused meanwhile
            yield return Press(GamepadButton.South);
            yield return WaitPhase(() => pilot.Now == DownedPilot.State.Running, 20f, "the pilot running");
            yield return Press(GamepadButton.Start); yield return WaitFor(() => G.Now == GameShell.State.Paused, 3f);
            Log($"  paused while pilot {pilot.Index + 1} runs: Reset position {(G.CanReset ? "OFFERED (wrong)" : "greyed out")}");
            yield return Nav(GamepadButton.DpadDown, 1); yield return Press(GamepadButton.South);
            Log($"  pressing it anyway: {G.Now}");
            yield return Press(GamepadButton.Start);
            yield return BoardHere(pilot);
            // docking with the pilot aboard: pause during the approach
            yield return TakeOffAndClimb(1100f);
            yield return PauseDuring("docking", "c07_pause_docking", () => G.Phase == "dock-approach" || G.Phase == "dock-final", 90f);
            yield return WaitPhase(() => G.Now == GameShell.State.Docked && G.Phase == "docked", 120f, "docked");
            Log($"  docked: {G.Debrief.Replace("\n", " / ")}");
            // recall from the air: pause during the lift
            G.Menu.Current.Selected = 0; yield return Press(GamepadButton.South);                  // Launch
            yield return WaitPhase(() => G.Now == GameShell.State.Flying, 40f, "flight after launch");
            yield return new WaitForSecondsRealtime(1f);
            state = state.WithButton(GamepadButton.Select, true); Set();   // hold View (the recall) 2 s, stick centred
            yield return new WaitForSecondsRealtime(2f);
            state = state.WithButton(GamepadButton.Select, false); Set(); yield return null; yield return null;
            Log($"recall held 2 s: calling {G.Calling}, state after release {G.Now} (a held View is the recall, not a pause)");
            yield return PauseDuring("the recall lift", "c08_pause_recall", () => G.Phase == "tow-lift", 120f);
            yield return WaitPhase(() => G.Now == GameShell.State.Docked && G.Phase == "docked", 120f, "docked");
            Log($"  docked: {G.Debrief.Replace("\n", " / ")}");
            // the tow: land, run dry, call the tow, pause while it comes
            G.Menu.Current.Selected = 0; yield return Press(GamepadButton.South);
            yield return WaitPhase(() => G.Now == GameShell.State.Flying, 40f, "flight after launch");
            yield return SetMode(FlightMode.Hover);
            yield return WaitFor(() => !G.Ship.Braking, 10f);
            G.Terrain.Field.NearestPad(G.Ship.transform.position, out var pd2);
            yield return HoverLand(pd2.Position);
            G.Ship.Energy = .2f;
            yield return WaitPhase(() => G.Menu.Current != null && G.Menu.Current.Title == "OUT OF ENERGY", 30f, "out of energy on the ground");
            yield return Press(GamepadButton.South);                                                  // Call the mothership tow
            yield return PauseDuring("the tow", "c09_pause_tow", () => G.Phase == "tow-wait" || G.Phase == "tow-lift", 20f);
            yield return WaitPhase(() => G.Now == GameShell.State.Docked && G.Phase == "docked", 240f, "docked after the tow");
            Log($"  docked: {G.Debrief.Replace("\n", " / ")}");
        }
    }

}
