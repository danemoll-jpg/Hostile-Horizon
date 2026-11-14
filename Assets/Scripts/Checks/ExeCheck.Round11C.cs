using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

namespace HostileHorizon
{
    // Round 11 Part C check (-exeCheckOnly round11c): every lesson played from the title with the virtual pad (the game's real input
    // path; aiming the gun at a target is the check's shortcut, as in round5), a picture of each step, the lesson completing, then:
    // skip a step and a hint after 30 s stuck (a restarted lesson), the VR lesson listed as VR only, and nothing in the career save
    // changed (a career made first; its file compared byte for byte; the career in memory back after training).
    public sealed partial class ExeCheck
    {
        IEnumerator HoldButton(GamepadButton b, float seconds) { state = state.WithButton(b, true); Set(); yield return new WaitForSecondsRealtime(seconds); state = state.WithButton(b, false); Set(); yield return null; yield return null; }
        IEnumerator HoldStick(Vector2 v, float seconds) { state.leftStick = v; Set(); yield return new WaitForSecondsRealtime(seconds); state.leftStick = Vector2.zero; Set(); yield return null; }

        IEnumerator OpenLesson(int k)
        {
            if (G.Menu.Current != G.TrainingPage)
            {
                yield return WaitFor(() => G.Now == GameShell.State.Title && G.Menu.Current != null, 60f);
                G.Menu.Show(G.TitlePage); G.Menu.Current.Selected = Row("Training"); G.Menu.Show(G.Menu.Current, true); yield return Press(GamepadButton.South);
                yield return WaitFor(() => G.Menu.Current == G.TrainingPage, 5f);
            }
            G.Menu.Current.Selected = k; G.Menu.Show(G.Menu.Current, true); yield return Press(GamepadButton.South);
            yield return WaitFor(() => G.Now == GameShell.State.Flying && G.TrainingSteps > 0 && Training.Lesson == k, 60f);
            yield return new WaitForSecondsRealtime(1f);
            Log($"--- lesson {k + 1} ({Training.Names[k]}): flying {G.Now == GameShell.State.Flying}; {G.TrainingSteps} steps; step 1 \"{G.TrainingStepText}\"");
        }

        // a picture each time the lesson moves to the next step
        int lastStepShot = -1;
        IEnumerator StepWatch(int lesson)
        {
            lastStepShot = -1;
            while (Training.Active && Training.Lesson == lesson)
            {
                if (G.Now == GameShell.State.Flying && G.TrainingStep != lastStepShot && G.TrainingStep < G.TrainingSteps)
                {
                    lastStepShot = G.TrainingStep; yield return new WaitForSecondsRealtime(.4f);
                    yield return Shot($"r11c_l{lesson + 1}_step{lastStepShot + 1}");
                }
                yield return null;
            }
        }

        IEnumerator LessonEnd(int lesson, float timeout)
        {
            yield return WaitFor(() => G.Menu.Current == G.TrainingDonePage, timeout);
            bool done = G.Menu.Current == G.TrainingDonePage;
            Log($"  lesson {lesson + 1}: {(done ? "COMPLETE" : $"NOT complete (step {G.TrainingStep + 1}: \"{G.TrainingStepText}\")")}; saved as done {Training.P.done[lesson]}; log: {string.Join(" | ", G.TrainingLog.Skip(logFrom))}");
            logFrom = G.TrainingLog.Count;
            if (done) { yield return Shot($"r11c_l{lesson + 1}_complete"); G.Menu.Current.Selected = Row("Lessons"); G.Menu.Show(G.Menu.Current, true); yield return Press(GamepadButton.South); }
            else { yield return Press(GamepadButton.Start); yield return WaitFor(() => G.Now == GameShell.State.Paused, 3f); G.Menu.Current.Selected = Row("Lessons"); G.Menu.Show(G.Menu.Current, true); yield return Press(GamepadButton.South); }
            yield return WaitFor(() => G.Menu.Current == G.TrainingPage, 30f);
            state = default; Set();
        }
        int logFrom;

        IEnumerator FlyToUntil(Vector3 target, Func<bool> until, float timeout)
        {
            float end = Time.realtimeSinceStartup + timeout;
            while (Time.realtimeSinceStartup < end && G.Now == GameShell.State.Flying && !until())
            {
                var s = G.Ship; var t = s.transform; var to = target - t.position; to.y = 0;
                float err = Mathf.DeltaAngle(s.Heading, Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg), bank = BankOf(t);
                float roll = Mathf.Clamp((Mathf.Clamp(err * 1.5f, -50, 50) - bank) * .04f, -1, 1);
                float high = 0; var dir = to.normalized;
                for (int m = 0; m <= 10; m++) { var q = t.position + dir * (m * 60f); high = Mathf.Max(high, s.Field.Height(q.x, q.z)); }
                float climb = Mathf.Clamp((high + 70f - t.position.y) * .02f, -.45f, .8f), pitch = Mathf.Clamp((climb - t.forward.y) * 3f, -1, 1);
                state.leftStick = new Vector2(Firm(roll), -Firm(pitch)); Set();
                yield return null;
            }
            state.leftStick = Vector2.zero; Set();
        }

        IEnumerator Round11C()
        {
            Log("=== Round 11 C: training");
            // a real career first: its file must not change
            var real = Career.New(); string careerBefore = File.ReadAllText(Career.FilePath);
            Log($"  a career made before training: {real.systemName}, {real.credits} cr; career.json {careerBefore.Length} bytes");
            G.Menu.Show(G.TitlePage); yield return null;
            Log($"  title rows: {string.Join(" / ", G.TitlePage.Rows.Select(r => r.Label()))}");
            G.Menu.Current.Selected = Row("Training"); G.Menu.Show(G.Menu.Current, true); yield return Press(GamepadButton.South);
            yield return WaitFor(() => G.Menu.Current == G.TrainingPage, 5f);
            Log($"  training page rows: {string.Join(" / ", G.TrainingPage.Rows.Select(r => r.Label() + (r.Value != null && r.Value() != "" ? " [" + r.Value() + "]" : "") + (r.Disabled != null && r.Disabled() ? " (disabled)" : "")))}");
            yield return Shot("r11c_0_lessons");
            logFrom = G.TrainingLog.Count;

            // 1. flying
            yield return OpenLesson(0); StartCoroutine(StepWatch(0));
            yield return HoldStick(new Vector2(0, 1), .7f); yield return HoldStick(new Vector2(0, -1), .7f);
            yield return HoldStick(new Vector2(1, 0), .5f); yield return HoldStick(new Vector2(-1, 0), .5f);
            yield return new WaitForSecondsRealtime(.5f);
            yield return HoldButton(GamepadButton.RightShoulder, .7f); yield return HoldButton(GamepadButton.LeftShoulder, .7f);
            state.rightTrigger = 1; Set(); yield return WaitFor(() => G.Ship.Throttle > .97f, 8f); state.rightTrigger = 0; state.leftTrigger = 1; Set(); yield return WaitFor(() => G.Ship.Throttle < .55f, 8f); state.leftTrigger = 0; Set();
            yield return new WaitForSecondsRealtime(.5f);
            yield return HoldStick(new Vector2(1, 0), .6f); yield return WaitFor(() => G.TrainingStep >= 5, 12f);
            yield return HoldStick(new Vector2(1, 0), .4f); yield return Press(GamepadButton.LeftStick);
            yield return LessonEnd(0, 10f);

            // 2. navigation
            yield return OpenLesson(1); StartCoroutine(StepWatch(1));
            var near = G.Pilots.Where(p => p.OnFoot).OrderBy(p => Flat(p.Position - G.Ship.transform.position).magnitude).First();
            yield return FlyToUntil(near.LandingSpot, () => G.TrainingStep >= 1, 120f);
            for (int k = 0; k < 3 && G.TrainingStep == 1; k++) { yield return Press(GamepadButton.DpadRight); yield return new WaitForSecondsRealtime(.3f); }   // a tap: the route target
            int f0 = RadarScope.Filter; yield return HoldButton(GamepadButton.DpadRight, .8f);                                                                  // held: the radar filter
            Log($"  D-pad → held 0.8 s: radar filter {RadarScope.FilterNames[f0]} -> {RadarScope.FilterNames[RadarScope.Filter]}; step now {G.TrainingStep + 1}");
            yield return FlyToUntil(near.LandingSpot, () => G.TrainingStep >= 4, 150f);
            for (int k = 0; k < 8 && G.TrainingStep == 4; k++) { yield return Press(GamepadButton.DpadRight); yield return new WaitForSecondsRealtime(.3f); }
            RadarScope.Filter = 0;
            yield return LessonEnd(1, 10f);

            // 3. landing
            yield return OpenLesson(2); StartCoroutine(StepWatch(2));
            var pad = G.Pilots.First(p => p.Index == 0).LandingSpot;
            yield return FlyToUntil(pad, () => Flat(pad - G.Ship.transform.position).magnitude < 500f, 60f);
            yield return SetMode(FlightMode.Hover);
            var land = StartCoroutine(HoverLand(pad));
            float tl = Time.realtimeSinceStartup;
            while (!G.Ship.Landed && Time.realtimeSinceStartup - tl < 150f) { if (G.TrainingStep == 2 && !G.Belly.On) yield return Press(GamepadButton.DpadDown); yield return null; }
            if (G.TrainingStep == 2 && !G.Belly.On) yield return Press(GamepadButton.DpadDown);
            yield return LessonEnd(2, 20f);

            // 4. the pickup
            yield return OpenLesson(3); StartCoroutine(StepWatch(3));
            pad = G.Pilots.First(p => p.Index == 0).LandingSpot;
            yield return SetMode(FlightMode.Hover); yield return HoverLand(pad);
            yield return new WaitForSecondsRealtime(.5f); yield return Press(GamepadButton.South);   // engines off
            yield return WaitFor(() => G.Pilots.Any(p => p.AtDoor), 90f);
            yield return new WaitForSecondsRealtime(.8f); yield return Press(GamepadButton.East);    // the airlock
            yield return WaitFor(() => G.CabinCount > 0, 20f); yield return new WaitForSecondsRealtime(.5f);
            yield return Press(GamepadButton.DpadUp);                                                // the cabin camera
            yield return LessonEnd(3, 15f);

            // 5. aliens
            yield return OpenLesson(4); StartCoroutine(StepWatch(4));
            var a0 = G.Pilots.First(p => p.Index == 0); var a1 = G.Pilots.FirstOrDefault(p => p.Index == 1);
            Log($"  practice aliens: {a0.Type} (tell shown: {a0.Quirk}) at pad 1{(a1 != null ? $", {a1.Type} (tell {a1.Quirk}) at pad 2, {Flat(a1.LandingSpot - a0.LandingSpot).magnitude:0} m away" : "")}");
            yield return SetMode(FlightMode.Hover); yield return HoverLand(a0.LandingSpot);
            yield return new WaitForSecondsRealtime(.5f); yield return Press(GamepadButton.South);   // engines off
            yield return WaitFor(() => a0.AtDoor, 90f); yield return new WaitForSecondsRealtime(5.5f);
            G.Belly.AirlockView = true; yield return Shot("r11c_l5_airlock_camera");
            yield return Press(GamepadButton.East);   // opens the airlock to it: in training it stays outside
            yield return new WaitForSecondsRealtime(1f);
            Log($"  airlock opened to the practice alien: cabin {G.CabinCount}, phase \"{G.Phase}\", horror \"{G.HorrorBeat}\" (no scare: {G.CabinCount == 0 && G.HorrorBeat == ""}); prompt \"{G.Displays.Prompt}\"");
            yield return Shot("r11c_l5_opened_to_it");
            yield return Press(GamepadButton.South);  // engines on: the exhaust
            yield return WaitFor(() => G.TrainingStep >= 3, 10f);
            if (a1 != null)
            {
                state.rightTrigger = 1; Set(); yield return WaitFor(() => G.Ship.AboveGround > 40f, 15f); state.rightTrigger = 0; Set();
                yield return SetMode(FlightMode.Flight);
                yield return FlyTo(a1.LandingSpot, 350f);
                yield return SetMode(FlightMode.Hover);
                yield return HoverLand(a1.LandingSpot);
                yield return new WaitForSecondsRealtime(.5f); yield return Press(GamepadButton.South);   // engines off
                yield return WaitFor(() => G.CanopyAlien != null, 120f);
                yield return WaitFor(() => G.HorrorBeat == "on the canopy", 10f); yield return new WaitForSecondsRealtime(3f);
                float clock = G.CanopyClock; yield return Shot("r11c_l5_canopy");
                yield return new WaitForSecondsRealtime(Tuning.T.breakInAfter + 2f);
                Log($"  left on the canopy {Tuning.T.breakInAfter + 2f:0} s past the break-in time: still outside {G.CanopyAlien != null && G.Phase != "horror"} (clock {G.CanopyClock:0.0} of {Tuning.T.breakInAfter})");
                yield return Press(GamepadButton.DpadLeft);   // the shield burns it off
            }
            yield return LessonEnd(4, 20f);

            // 6. combat
            yield return OpenLesson(5); StartCoroutine(StepWatch(5));
            Log($"  targets: {string.Join(", ", F.All.Select(e => $"{e.Kind} {Flat(e.Pos - G.Ship.transform.position).magnitude:0} m"))}");
            yield return Press(GamepadButton.DpadLeft);   // shield up
            var turret = F.All[0]; var saucer = F.All[1];
            yield return FlyToUntil(turret.Pos, () => Flat(turret.Pos - G.Ship.transform.position).magnitude < 400f, 60f);
            yield return ShootAt(turret, 40f, _ => { });
            yield return ShootAt(saucer, 40f, _ => { });
            Log($"  hull after the fight {100 - G.Ship.Damage:0} % (training: hits do no damage)");
            yield return LessonEnd(5, 15f);

            // 7. getting home
            yield return OpenLesson(6); StartCoroutine(StepWatch(6));
            Log($"  energy {G.Ship.Energy:0} % (home needs {G.EnergyHome:0} %): {(G.HomeState == 1 ? "RETURN NOW" : G.HomeState == 2 ? "CAN'T REACH" : "fine")}; HOME {Flat(G.Mother.transform.position - G.Ship.transform.position).magnitude / 1000f:0.0} km");
            yield return SetMode(FlightMode.Flight);
            yield return FlyToUntil(G.Mother.transform.position, () => G.TrainingStep >= 1, 90f);
            yield return HoldButton(GamepadButton.Select, 1.4f);   // the recall
            state = state.WithButton(GamepadButton.South, true); Set();   // fast-forward the lift
            yield return LessonEnd(6, 180f);

            // 8. VR: listed as VR only outside the headset
            var vrRow = G.TrainingPage.Rows[7];
            Log($"  VR lesson outside VR: \"{vrRow.Label()}\" [{vrRow.Value()}] disabled {vrRow.Disabled()}");

            // C2: skip a step, a hint after 30 s stuck, leave
            yield return OpenLesson(0);
            yield return Press(GamepadButton.Start); yield return WaitFor(() => G.Now == GameShell.State.Paused, 3f);
            yield return Shot("r11c_pause_menu");
            G.Menu.Current.Selected = Row("Skip this step"); G.Menu.Show(G.Menu.Current, true); yield return Press(GamepadButton.South);
            yield return new WaitForSecondsRealtime(.5f);
            Log($"  pause > Skip this step: now step {G.TrainingStep + 1} \"{G.TrainingStepText}\"");
            yield return new WaitForSecondsRealtime(31f);
            Log($"  31 s without touching the stick: hint shown {G.Displays.Objectives.Contains("Hint:")}; voices {string.Join(", ", Voices.Log.Where(l => l.Contains("ctl_tr")).Take(30))}");
            yield return Shot("r11c_hint");
            yield return Press(GamepadButton.Start); yield return WaitFor(() => G.Now == GameShell.State.Paused, 3f);
            G.Menu.Current.Selected = Row("Leave training"); G.Menu.Show(G.Menu.Current, true); yield return Press(GamepadButton.South);
            yield return WaitFor(() => G.Now == GameShell.State.Title, 30f);

            // C3: nothing leaked into the career save
            string careerAfter = File.Exists(Career.FilePath) ? File.ReadAllText(Career.FilePath) : "";
            Log($"  career.json after training: {(careerAfter == careerBefore ? "UNCHANGED (byte for byte) OK" : "CHANGED FAIL")}; the career in memory is the one from before: {ReferenceEquals(Career.C, real)}; training mode {Career.Training}");
            var tp = Path.Combine(CheckRun.Dir, "training.json");
            Log($"  training.json: {(File.Exists(tp) ? File.ReadAllText(tp).Replace("\n", " ").Replace("  ", "") : "missing")}");
            Log($"  output level (muted): {TestRun.Report()}");
        }
    }
}
