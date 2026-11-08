using System.Collections;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

namespace HostileHorizon
{
    // Fix 6.2: a virtual HOTAS with the axes Dan's T.Flight HOTAS 4 reports (Data/screenshots/fix62/hid_descriptor.txt): the stick (x, y),
    // z (the lever), rz (twist and rocker), slider, dial, rx, ry (never move; slider / dial rest in the middle, rx / ry at the top), the hat
    // and 12 buttons. Values are what the Input System gives a HID axis: -1..1.
    [StructLayout(LayoutKind.Explicit, Size = 40)]
    public struct CheckHotas4State : IInputStateTypeInfo
    {
        public FourCC format => new FourCC('C', 'H', 'K', '4');
        [InputControl(name = "stick", layout = "Stick", usage = "Primary2DMotion")] [FieldOffset(0)] public Vector2 stick;
        [InputControl(name = "rz", layout = "Axis", usage = "Twist")] [FieldOffset(8)] public float rz;
        [InputControl(name = "z", layout = "Axis")] [FieldOffset(12)] public float z;
        [InputControl(name = "slider", layout = "Axis")] [FieldOffset(16)] public float slider;
        [InputControl(name = "dial", layout = "Axis")] [FieldOffset(20)] public float dial;
        [InputControl(name = "rx", layout = "Axis")] [FieldOffset(24)] public float rx;
        [InputControl(name = "ry", layout = "Axis")] [FieldOffset(28)] public float ry;
        [InputControl(name = "hat", layout = "Dpad", usage = "Hatswitch", format = "BIT", sizeInBits = 4, bit = 0)] [FieldOffset(32)] public uint hat;
        [InputControl(name = "trigger", layout = "Button", bit = 0, usage = "PrimaryTrigger")]
        [InputControl(name = "button2", layout = "Button", bit = 1)] [InputControl(name = "button3", layout = "Button", bit = 2)]
        [InputControl(name = "button4", layout = "Button", bit = 3)] [InputControl(name = "button5", layout = "Button", bit = 4)]
        [InputControl(name = "button6", layout = "Button", bit = 5)] [InputControl(name = "button7", layout = "Button", bit = 6)]
        [InputControl(name = "button8", layout = "Button", bit = 7)] [InputControl(name = "button9", layout = "Button", bit = 8)]
        [InputControl(name = "button10", layout = "Button", bit = 9)] [InputControl(name = "button11", layout = "Button", bit = 10)]
        [InputControl(name = "button12", layout = "Button", bit = 11)]
        [FieldOffset(36)] public uint buttons;
    }
    [InputControlLayout(stateType = typeof(CheckHotas4State), displayName = "Check T.Flight HOTAS 4")]
    public class CheckHotas4 : Joystick { }

    // Fix 6.2 check (-exeCheckOnly fix62; fix62pics = the pictures only, for 1280 x 800): resets (and the career file untouched), the
    // lever on each possible axis, Auto, Find throttle lever, the ship's throttle from the first frame at 0 / 50 / 100 %, the calibration
    // steps and cancel, pictures.
    public sealed partial class ExeCheck
    {
        CheckHotas4 h4; CheckHotas4State h4s;
        void H4() => InputSystem.QueueStateEvent(h4, h4s);
        void H4Rest(float lever) { h4s.stick = new Vector2(-.496f, 0); h4s.rz = 0; h4s.z = lever; h4s.slider = 0; h4s.dial = 0; h4s.rx = 1; h4s.ry = 1; h4s.hat = 0; H4(); }
        IEnumerator H4Button(int n) { h4s.buttons |= 1u << (n - 1); H4(); yield return null; yield return null; h4s.buttons &= ~(1u << (n - 1)); H4(); yield return null; yield return null; }
        // invert lever on: full throttle = the axis at -1 (Dan's lever pushed forward)
        static float LeverFor(float throttle) => 1 - 2 * throttle;
        void SetAx(string n, float v) { switch (n) { case "z": h4s.z = v; break; case "slider": h4s.slider = v; break; case "dial": h4s.dial = v; break; case "rx": h4s.rx = v; break; case "ry": h4s.ry = v; break; } }
        float Ax(string n) => h4.TryGetChildControl(n) is UnityEngine.InputSystem.Controls.AxisControl a ? a.ReadUnprocessedValue() : float.NaN;
        string Axes() => string.Join("  ", new[] { "z", "slider", "dial", "rx", "ry", "rz" }.Select(n => $"{n} {Ax(n):+0.00;-0.00; 0.00}"));
        static string FileHash(string p) { if (!File.Exists(p)) return "none"; using (var md5 = System.Security.Cryptography.MD5.Create()) return System.BitConverter.ToString(md5.ComputeHash(File.ReadAllBytes(p))).Replace("-", "").Substring(0, 12); }

        IEnumerator Fix62()
        {
            bool picsOnly = CheckRun.Only == "fix62pics";
            yield return WaitFor(() => G.Now == GameShell.State.Title && G.Menu.Current != null, 60f);
            InputSystem.RegisterLayout<CheckHotas4>();
            h4 = InputSystem.AddDevice<CheckHotas4>("CheckHotas4");
            H4Rest(0); for (int k = 0; k < 4; k++) yield return null;
            Log($"virtual T.Flight HOTAS 4 added; HotasPresent {Controls.HotasPresent}; Auto picks \"{Controls.LeverAxisName}\" (0.6.1 picked \"slider\": the device has one); throttle readout {Controls.ThrottleLever * 100:0}%");

            if (!picsOnly)
            {
                // ---- 1. the axis log: a lever on each possible axis, swept 0 -> 100 -> 0 % (twist and stick moved too: never picked)
                foreach (var ax in Controls.LeverCandidates.Where(n => n != "wheel"))
                {
                    GameSettings.ResetHotas(); Controls.ForgetLeverMoves(); Controls.ApplySettings(); H4Rest(0); yield return null; yield return null;
                    string before = Controls.LeverAxisName;
                    float rest = ax == "rx" || ax == "ry" ? 1 : 0; float t0 = Time.realtimeSinceStartup;
                    while (Time.realtimeSinceStartup - t0 < 1.2f)
                    {
                        float k = (Time.realtimeSinceStartup - t0) / 1.2f; float v = Mathf.Cos(k * Mathf.PI * 2) * (ax == "rx" || ax == "ry" ? 1 : -1);   // rest -> the other end -> rest
                        H4Rest(0); SetAx(ax, v); h4s.rz = Mathf.Sin(k * 9) * .4f; h4s.stick = new Vector2(-.496f + Mathf.Sin(k * 7) * .3f, Mathf.Cos(k * 5) * .3f); H4();
                        yield return null;
                    }
                    H4Rest(0); SetAx(ax, -1f); H4(); yield return null; yield return null;
                    float full = Controls.ThrottleLever;
                    SetAx(ax, 0f); H4(); yield return null; yield return null; float half = Controls.ThrottleLever;
                    SetAx(ax, 1f); H4(); yield return null; yield return null; float none = Controls.ThrottleLever;
                    Log($"lever on {ax}: Auto {before} -> {Controls.LeverAxisName} ({(Controls.LeverAxisName == ax ? "OK" : "FAIL")}); moved {string.Join(" ", Controls.LeverMoves.Select(m => $"{m.name} {m.moved:0.00}"))}; lever at -1 / 0 / +1 reads {full * 100:0} / {half * 100:0} / {none * 100:0} %");
                }
                // 0.6.1's choice on Dan's HOTAS: slider bound, the lever (z) moved: the readout never changes
                GameSettings.ResetHotas(); GameSettings.S.throttleAxisFound = "slider"; Controls.ApplySettings(); H4Rest(0);
                string seen = ""; for (int i = 0; i <= 4; i++) { h4s.z = LeverFor(i / 4f); H4(); yield return null; yield return null; seen += $"{Controls.ThrottleLever * 100:0} "; }
                Log($"0.6.1's binding (slider) with the lever (z) at 0 / 25 / 50 / 75 / 100 %: reads {seen}% (never moves: the cause)");

                // ---- 2. Find throttle lever, through the menu (twist wobbling and the stick moving at the same time)
                GameSettings.ResetHotas(); GameSettings.S.throttleAxisFound = "dial"; Controls.ForgetLeverMoves(); Controls.ApplySettings(); H4Rest(0); yield return null;
                yield return OpenHotasSetup();
                var setup = G.HotasSetupPage;
                setup.Selected = Row("Find throttle lever"); G.Menu.Show(setup, true); yield return Press(GamepadButton.South);
                Log($"Find throttle lever: started ({G.CaptureState}); bound before: {Controls.LeverAxisName}");
                yield return Shot("fix62_find_lever");
                float tf = Time.realtimeSinceStartup;
                while (G.Capturing && Time.realtimeSinceStartup - tf < 8f)
                {
                    float k = Time.realtimeSinceStartup - tf; h4s.z = Mathf.Clamp(Mathf.Sin(k * 2.5f) * 1.3f, -1, 1); h4s.rz = Mathf.Sin(k * 11) * .5f; h4s.stick = new Vector2(-.5f + Mathf.Sin(k * 6) * .4f, Mathf.Sin(k * 4) * .4f); H4();
                    yield return null;
                }
                H4Rest(LeverFor(.64f)); yield return null; yield return null;
                Log($"Find throttle lever: picked \"{Controls.LeverAxisName}\" in {Time.realtimeSinceStartup - tf:0.0} s ({(Controls.LeverAxisName == "z" ? "OK" : "FAIL")}); note \"{G.CaptureNote}\"; lever at 64 %: readout {Controls.ThrottleLever * 100:0} %");
                yield return Shot("fix62_find_lever_done");
                // and cancel: circle (the HOTAS's back button) stops it, nothing changes
                setup.Selected = Row("Find throttle lever"); G.Menu.Show(setup, true); yield return Press(GamepadButton.South);
                yield return new WaitForSecondsRealtime(.3f); yield return H4Button(7); yield return null;
                Log($"Find throttle lever, circle pressed: capturing {G.Capturing}; still \"{Controls.LeverAxisName}\"; note \"{G.CaptureNote}\"");

                // ---- 3. the ship's throttle from the first frame of flight, lever at 0 / 50 / 100 %
                foreach (var want in new[] { 0f, .5f, 1f })
                {
                    G.Menu.Hide(); H4Rest(LeverFor(want)); yield return null;
                    G.StartFlight(1984, CrashLevel.Bounce);
                    yield return WaitFor(() => G.Now == GameShell.State.Flying, 30f);
                    float first = G.Ship.Throttle; yield return null; float second = G.Ship.Throttle;
                    yield return new WaitForSecondsRealtime(1f);
                    Log($"lever {want * 100:0} %: ship throttle on the first frame of flight {first * 100:0} %, next {second * 100:0} %, after 1 s {G.Ship.Throttle * 100:0} % ({(Mathf.Abs(first - want) < .02f ? "OK" : "FAIL")}; 0.6.1: 30 % until the lever moved)");
                    if (want == .5f)
                    {
                        string follow = ""; for (int i = 0; i <= 4; i++) { h4s.z = LeverFor(i / 4f); H4(); yield return new WaitForSecondsRealtime(.1f); follow += $"{G.Ship.Throttle * 100:0} "; }
                        Log($"lever moved 0 / 25 / 50 / 75 / 100 %: ship throttle {follow}%");
                        yield return Shot("fix62_flight_full_throttle");
                        state.rightTrigger = 0; state.leftTrigger = 1; Set(); yield return new WaitForSecondsRealtime(.5f); state.leftTrigger = 0; Set(); yield return null;
                        float afterPad = G.Ship.Throttle; yield return new WaitForSecondsRealtime(.3f); float stays = G.Ship.Throttle;
                        h4s.z = LeverFor(.8f); H4(); yield return new WaitForSecondsRealtime(.1f);
                        Log($"pad LT held 0.5 s with the lever at 100 %: throttle {afterPad * 100:0} %, stays {stays * 100:0} %; then the lever moved to 80 %: {G.Ship.Throttle * 100:0} % (the lever takes it back)");
                    }
                    yield return ToTitleNow();
                }
            }

            // ---- 4. resets, through the menus (No first, then Yes); the career file untouched
            yield return Resets(picsOnly);

            // ---- 5. calibration: asked first, three steps, the edges ticked off, cancel keeps the old one
            yield return Calibration(picsOnly);

            // ---- 6. pictures: Controls (HOTAS), HOTAS setup, identify a few symbols lit
            yield return OpenHotasSetup();
            yield return Shot("fix62_hotas_setup");
            var sp = G.HotasSetupPage;
            sp.Selected = Row("Identify"); G.Menu.Show(sp, true); yield return Press(GamepadButton.South);
            for (int k = 0; k < Controls.HotasSymbols.Length; k++)
            {
                if (k == 0 || k == 2 || k == 3 || k == 6) yield return Shot($"fix62_identify_{k}_{new[] { "R2", "R1", "R3", "L1", "L2", "L3", "square", "cross", "circle", "triangle", "SHARE", "OPTIONS" }[k]}");
                yield return new WaitForSecondsRealtime(.1f); yield return H4Button(Controls.DefaultSymbolButtons[k]);
            }
            Log($"identify with the default buttons: names {string.Join(" ", Enumerable.Range(1, 12).Select(Controls.HotasName))} (buttons 1..12)");
            G.Menu.Show(G.ControlsPage); G.ControlsSheetUi.Showing = Controls.Device.Hotas; yield return null; yield return null;
            yield return Shot("fix62_controls_hotas");
        }

        IEnumerator ToTitleNow()
        {
            if (G.Now == GameShell.State.Flying) yield return PauseToTitle();
            yield return WaitFor(() => G.Now == GameShell.State.Title && G.Menu.Current != null, 30f);
        }

        IEnumerator OpenHotasSetup()
        {
            if (G.Now != GameShell.State.Title) yield return ToTitleNow();
            G.Menu.Show(G.TitlePage); G.Menu.Current.Selected = Row("Settings"); G.Menu.Show(G.Menu.Current, true); yield return Press(GamepadButton.South);
            yield return WaitFor(() => G.Menu.Current?.Title == "SETTINGS", 3f);
            G.Menu.Current.Selected = Row("Controls"); G.Menu.Show(G.Menu.Current, true); yield return Press(GamepadButton.South);
            yield return WaitFor(() => G.Menu.Current == G.ControlsPage, 3f);
            G.Menu.Current.Selected = Row("HOTAS setup"); G.Menu.Show(G.Menu.Current, true); yield return Press(GamepadButton.South);
            yield return WaitFor(() => G.Menu.Current == G.HotasSetupPage, 3f);
            yield return null;
        }

        IEnumerator Resets(bool picsOnly)
        {
            if (G.Now != GameShell.State.Title) yield return ToTitleNow();
            // a career file in the check's save folder, to show the resets never touch it
            Directory.CreateDirectory(Career.Folder);
            if (!Career.Exists) File.WriteAllText(Career.FilePath, "{\"note\":\"Fix 6.2 check: must stay unchanged\"}");
            string hash0 = FileHash(Career.FilePath);
            var s = GameSettings.S;
            string Flight() => $"assist {s.flightAssist} sens {s.sensitivity} autoLevel {s.autoLevel} righting {s.rightingSpeed} invertPitch {s.invertPitch} deadzone {s.deadzone} view {s.cameraDefault} trail {s.routeTrail}";
            string Sound() => $"master {s.volume} music {s.musicVolume} fx {s.effectsVolume} voices {s.voiceVolume} subs {s.subtitles}";
            string Disp() => $"quality {s.quality} deck {s.steamDeck} screen {ScreenSetting.ScreenLabel} {ScreenSetting.ResolutionLabel}";
            string Hotas() => $"cal {s.hotasCalibrated} [{string.Join(",", s.hotasCal.Select(v => v.ToString("0.00")))}] lever {s.throttleAxisFound} forwardIsMin {s.leverForwardIsMin} names [{string.Join(",", s.hotasSymbolButtons)}] jobs [{string.Join(",", s.hotasButtons)}] deadzone {s.deadzone}";
            void Mess()
            {
                s.flightAssist = 0; s.sensitivity = 130; s.autoLevel = false; s.rightingSpeed = 2; s.invertPitch = true; s.deadzone = 20; s.cameraDefault = 2; s.routeTrail = false;
                s.volume = 40; s.musicVolume = 20; s.effectsVolume = 30; s.voiceVolume = 50; s.subtitles = false; s.quality = 0; s.steamDeck = 1;
                s.hotasCal = new[] { -1f, -.46f, .34f, -1, 0, 1 }; s.hotasCalibrated = true; s.throttleAxisFound = "dial"; s.leverForwardIsMin = false;
                s.hotasSymbolButtons = new[] { 1, 3, 2, 10, 9, 4, 5, 6, 7, 8, 11, 12 }; Controls.AssignHotas(Controls.HotasAct.Pause, 4);
            }
            // Settings > Reset this page: No keeps everything, Yes resets only this page
            Mess();
            G.Menu.Show(G.TitlePage); G.Menu.Current.Selected = Row("Settings"); G.Menu.Show(G.Menu.Current, true); yield return Press(GamepadButton.South);
            yield return WaitFor(() => G.Menu.Current?.Title == "SETTINGS", 3f);
            var settings = G.Menu.Current;
            settings.Selected = Row("Reset this page"); G.Menu.Show(settings, true); yield return Press(GamepadButton.South);
            Log($"Settings > Reset this page: \"{G.Menu.Current.Title}\" rows {string.Join(" / ", G.Menu.Current.Rows.Select(r => r.Label()))}, selected \"{G.Menu.Current.Rows[G.Menu.Current.Selected].Label()}\"");
            yield return Shot("fix62_confirm_page");
            string f0 = Flight(); yield return Press(GamepadButton.South);   // No
            Log($"  No: back on \"{G.Menu.Current.Title}\", unchanged {(Flight() == f0 ? "OK" : "FAIL")}");
            settings.Selected = Row("Reset this page"); G.Menu.Show(settings, true); yield return Press(GamepadButton.South); yield return Press(GamepadButton.DpadDown); yield return Press(GamepadButton.South);   // Yes
            Log($"  Yes: flight {Flight()} (defaults: {new GameSettingsProbe().Flight}); sound still {Sound()}; HOTAS still calibrated {s.hotasCalibrated}");
            if (picsOnly) yield return Shot("fix62_settings");
            // Sound and Display pages
            settings.Selected = Row("Sound"); G.Menu.Show(settings, true); yield return Press(GamepadButton.South); yield return null;
            var sound = G.Menu.Current; sound.Selected = Row("Reset this page"); G.Menu.Show(sound, true); yield return Press(GamepadButton.South); yield return Press(GamepadButton.DpadDown); yield return Press(GamepadButton.South);
            Log($"Sound > Reset this page (Yes): {Sound()}; display still {Disp()}");
            yield return Shot("fix62_sound");
            G.Menu.Show(settings, true); settings.Selected = Row("Display"); G.Menu.Show(settings, true); yield return Press(GamepadButton.South); yield return null;
            var disp = G.Menu.Current; disp.Selected = Row("Reset this page"); G.Menu.Show(disp, true); yield return Press(GamepadButton.South); yield return Press(GamepadButton.DpadDown); yield return Press(GamepadButton.South);
            Log($"Display > Reset this page (Yes): {Disp()}; HOTAS still {Hotas()}");
            // HOTAS setup > Reset HOTAS settings
            yield return OpenHotasSetup();
            var setup = G.HotasSetupPage; setup.Selected = Row("Reset HOTAS"); G.Menu.Show(setup, true); yield return Press(GamepadButton.South);
            Log($"HOTAS setup > Reset HOTAS settings: \"{G.Menu.Current.Title}\" — {G.Menu.Current.Body?.Invoke()?.Replace("\n", " ")}");
            yield return Shot("fix62_confirm_hotas");
            yield return Press(GamepadButton.DpadDown); yield return Press(GamepadButton.South);
            Log($"  Yes: {Hotas()}; Pause on {Controls.HotasName(Controls.HotasButtonFor(Controls.HotasAct.Pause))} (default OPTIONS)");
            // Settings > Reset all settings
            Mess();
            G.Menu.Show(settings); settings.Selected = Row("Reset all"); G.Menu.Show(settings, true); yield return Press(GamepadButton.South);
            yield return Press(GamepadButton.DpadDown); yield return Press(GamepadButton.South);
            Log($"Settings > Reset all settings (Yes): {Flight()} | {Sound()} | {Disp()} | {Hotas()}");
            Log($"career file {Career.FilePath}: before {hash0}, after {FileHash(Career.FilePath)} ({(FileHash(Career.FilePath) == hash0 ? "untouched OK" : "CHANGED FAIL")}); the settings file is never written by a check");
        }

        IEnumerator Calibration(bool picsOnly)
        {
            GameSettings.ResetHotas(); Controls.ForgetCalibration(); Controls.ApplySettings(); H4Rest(0);
            yield return OpenHotasSetup();
            var setup = G.HotasSetupPage;
            setup.Selected = Row("Calibrate"); G.Menu.Show(setup, true); yield return Press(GamepadButton.South);
            Log($"Calibrate stick: asks first \"{G.Menu.Current.Title}\" — {G.Menu.Current.Body?.Invoke()}; rows {string.Join(" / ", G.Menu.Current.Rows.Select(r => r.Label()))}");
            yield return Shot("fix62_calibrate_ask");
            yield return Press(GamepadButton.DpadDown); yield return Press(GamepadButton.South);   // Yes
            yield return new WaitForSecondsRealtime(.4f);
            Log($"  step: {G.CaptureState}");
            yield return Shot("fix62_calibrate_step1");
            float t = Time.realtimeSinceStartup; while (Time.realtimeSinceStartup - t < 2f) { h4s.stick = new Vector2(-.496f + Random.Range(-.01f, .01f), Random.Range(-.01f, .01f)); H4(); yield return null; }
            Log($"  after 2 s hands off: {G.CaptureState}");
            // forward, back, left (Dan's left reaches -1), not right yet
            foreach (var p in new[] { new Vector2(-.5f, 1), new Vector2(-.5f, -1), new Vector2(-1, 0), new Vector2(-.5f, 0) }) { h4s.stick = p; H4(); yield return new WaitForSecondsRealtime(.25f); }
            yield return Shot("fix62_calibrate_step2");
            yield return H4Button(1); yield return null;
            Log($"  trigger with right not reached: {G.CaptureState}; \"{G.SetupSheetUi.CalReached.Select(b => b ? "x" : "-").Aggregate((a, b) => a + b)}\" (fwd back left right); report: {G.CaptureNote}");
            h4s.stick = new Vector2(.34f, 0); H4(); yield return new WaitForSecondsRealtime(.25f); h4s.stick = new Vector2(-.496f, 0); H4(); yield return new WaitForSecondsRealtime(.25f);
            yield return Shot("fix62_calibrate_step3");
            yield return H4Button(1); yield return null;
            var c = GameSettings.S.hotasCal;
            Log($"  trigger after all four: calibrated {GameSettings.S.hotasCalibrated}; x {c[0]:+0.00;-0.00} / {c[1]:+0.00;-0.00} / {c[2]:+0.00;-0.00}, y {c[3]:+0.00;-0.00} / {c[4]:+0.00;-0.00} / {c[5]:+0.00;-0.00}; centred stick reads {Controls.HotasStick}; note \"{G.CaptureNote}\"");
            var kept = (float[])c.Clone();
            // cancel at step 2 with circle: the old calibration is kept
            setup.Selected = Row("Calibrate"); G.Menu.Show(setup, true); yield return Press(GamepadButton.South); yield return Press(GamepadButton.DpadDown); yield return Press(GamepadButton.South);
            t = Time.realtimeSinceStartup; while (Time.realtimeSinceStartup - t < 2f) { h4s.stick = new Vector2(.2f, .1f); H4(); yield return null; }
            h4s.stick = new Vector2(.9f, .9f); H4(); yield return null; h4s.stick = new Vector2(-.496f, 0); H4(); yield return null;   // (the stick back to rest: once calibration ends, a moved stick still widens the reach, as in 0.6.1)
            string st = G.CaptureState; yield return H4Button(7); yield return null;
            Log($"  cancel with circle at {st}: capturing {G.Capturing}; calibration kept {(GameSettings.S.hotasCal.SequenceEqual(kept) ? "OK" : "FAIL")}; note \"{G.CaptureNote}\"");
            // and "No" on the question changes nothing
            setup.Selected = Row("Calibrate"); G.Menu.Show(setup, true); yield return Press(GamepadButton.South); yield return Press(GamepadButton.South);
            Log($"  No on the question: back on {G.Menu.Current.Title}, capturing {G.Capturing}");
            h4s.stick = new Vector2(-.496f, 0); H4(); yield return null;
        }

        // The flight page's defaults, for the log
        sealed class GameSettingsProbe
        {
            readonly GameSettings d = new GameSettings();
            public string Flight => $"assist {d.flightAssist} sens {d.sensitivity} autoLevel {d.autoLevel} righting {d.rightingSpeed} invertPitch {d.invertPitch} deadzone {d.deadzone} view {d.cameraDefault} trail {d.routeTrail}";
        }
    }
}
