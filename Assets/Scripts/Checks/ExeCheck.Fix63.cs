using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.HID;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;

namespace HostileHorizon
{
    // Fix 6.3 check (-exeCheckOnly fix63): a virtual HOTAS made the way Unity makes Dan's real one: a HID joystick from his HID descriptor
    // (Data/screenshots/fix62/hid_descriptor.txt: y, x 0..1023, dial, slider, ry, rx, rz, z 0..255, a hat, 12 buttons), so Unity's own HID
    // support names its controls (button 1 = "trigger"). Then: what changed Dan's throttle axis and button jobs (0.6.2's rows replayed
    // with a leaning stick), the same stick on 0.6.3's menus (nothing changes), the hat and select changing values, button 1 firing in
    // Free Flight and the Campaign, the recorder, Test view pictures, the sound mix before / after (measured, muted), Test voices.
    public sealed partial class ExeCheck
    {
        InputDevice hid;
        // Fix 6.3: each value written where Unity's own control for it says it lives (the axes and buttons through the controls, the hat's
        // 4 bits at its state block), so what the game reads is exactly what Unity made of a HID report
        void Hid(Vector2 stick, float throttle, float twist = 0, int hat = -1, uint buttons = 0)
        {
            var buf = StateEvent.From(hid, out var ev);
            try
            {
                void Ax(string n, float v) { if (hid.TryGetChildControl(n) is UnityEngine.InputSystem.Controls.AxisControl a) a.WriteValueIntoEvent(Mathf.Clamp(v, -1, 1), ev); }
                Ax("stick/x", stick.x); Ax("stick/y", stick.y); Ax("dial", 0); Ax("slider", 0); Ax("rx", 1); Ax("ry", 1); Ax("rz", twist); Ax("z", 1 - 2 * Mathf.Clamp01(throttle));
                for (int i = 1; i <= 12; i++) if (hid.TryGetChildControl(i == 1 ? "trigger" : "button" + i) is UnityEngine.InputSystem.Controls.ButtonControl bc) bc.WriteValueIntoEvent((buttons & (1u << (i - 1))) != 0 ? 1f : 0f, ev);
                var hc = hid.TryGetChildControl("hat");
                if (hc != null)
                {
                    int size = (int)((hid.stateBlock.sizeInBits + 7) / 8), at = (int)ev.sizeInBytes - size;
                    int bit = (int)(hc.stateBlock.byteOffset - hid.stateBlock.byteOffset) * 8 + (int)hc.stateBlock.bitOffset, v = hat < 0 ? 8 : hat;
                    for (int i = 0; i < (int)hc.stateBlock.sizeInBits; i++) { int bb = bit + i, ix = at + (bb >> 3); if (ix >= buf.Length) break; byte m = (byte)(1 << (bb & 7)); buf[ix] = ((v >> i) & 1) != 0 ? (byte)(buf[ix] | m) : (byte)(buf[ix] & ~m); }
                }
                InputSystem.QueueEvent(ev);
            }
            finally { buf.Dispose(); }
        }
        Vector2 hStick; float hLever = .5f, hTwist; int hHat = -1; uint hButtons;
        void HidNow() => Hid(hStick, hLever, hTwist, hHat, hButtons);
        IEnumerator HidPress(int button, float hold = .1f) { hButtons |= 1u << (button - 1); HidNow(); yield return new WaitForSecondsRealtime(hold); hButtons &= ~(1u << (button - 1)); HidNow(); yield return null; yield return null; }
        IEnumerator HidHatTap(int dir) { hHat = dir; HidNow(); yield return null; yield return null; yield return null; hHat = -1; HidNow(); yield return null; yield return null; }

        InputDevice AddDanHotas()
        {
            var b = new HID.HIDDeviceDescriptorBuilder(HID.GenericDesktop.Joystick).StartReport(HID.HIDReportType.Input)
                .AddElement(HID.GenericDesktop.Y, 16).WithLogicalMinMax(0, 1023).AddElement(HID.GenericDesktop.X, 16).WithLogicalMinMax(0, 1023)
                .AddElement(HID.GenericDesktop.Dial, 8).WithLogicalMinMax(0, 255).AddElement(HID.GenericDesktop.Slider, 8).WithLogicalMinMax(0, 255)
                .AddElement(HID.GenericDesktop.Ry, 8).WithLogicalMinMax(0, 255).AddElement(HID.GenericDesktop.Rx, 8).WithLogicalMinMax(0, 255)
                .AddElement(HID.GenericDesktop.Rz, 8).WithLogicalMinMax(0, 255).AddElement(HID.GenericDesktop.Z, 8).WithLogicalMinMax(0, 255)
                .AddElement(HID.GenericDesktop.HatSwitch, 4).WithLogicalMinMax(0, 7).AddElement((HID.UsagePage)0xff00, 0x21, 4);
            for (int i = 1; i <= 12; i++) b = b.AddElement(HID.UsagePage.Button, i, 1).WithLogicalMinMax(0, 1);
            var d = b.Finish();
            for (int i = 0; i < d.elements.Length; i++) if (d.elements[i].usage == (int)HID.GenericDesktop.HatSwitch && d.elements[i].usagePage == HID.UsagePage.GenericDesktop) d.elements[i].flags |= HID.HIDElementFlags.NullState;   // a real hat: 8 = centred
            d.vendorId = 0x044f; d.productId = 0xb67c; d.inputReportSize = 14;
            return InputSystem.AddDevice(new InputDeviceDescription { interfaceName = "HID", product = "T.Flight Hotas 4", manufacturer = "Thrustmaster", capabilities = d.ToJson() });
        }

        string SettingsNow()
        {
            var s = GameSettings.S;
            return $"lever found \"{s.throttleAxisFound}\" (bound {Controls.LeverAxisName}, forward=min {s.leverForwardIsMin}) jobs [{string.Join(",", s.hotasButtons)}] names [{string.Join(",", s.hotasSymbolButtons)}] assist {s.flightAssist} sens {s.sensitivity} autoLevel {s.autoLevel} righting {s.rightingSpeed} invertPitch {s.invertPitch} deadzone {s.deadzone} view {s.cameraDefault} trail {s.routeTrail}";
        }

        // 0.6.2's HOTAS setup rows (Calibrate, Find, Identify, Throttle axis, Invert lever, the 11 jobs, Reset, Back) and their left / right,
        // the menu's own NavReader fed by 0.6.2's Navigate (the whole calibrated stick) or 0.6.3's (only its up / down)
        static readonly string[] OldAxes = { "Auto", "slider", "z", "rz", "rx", "ry", "dial" };
        (int axis, bool invert, int[] jobs, int row, int sideSteps) Replay062(List<Vector2> trace, bool oldNavigate)
        {
            int axis = 0; bool invert = true; var jobs = Controls.HotasActs.Select(a => a.button).ToArray(); int row = 0, side = 0;
            var nav = new NavReader(); nav.Reset();
            foreach (var p in trace)
            {
                var n = oldNavigate ? p : new Vector2(0, p.y);
                int d = nav.Step(n, 1f / 60);
                if (d == 1) row = Mathf.Max(0, row - 1); else if (d == 2) row = Mathf.Min(17, row + 1);
                else if (d == 3 || d == 4)
                {
                    side++; int s = d == 4 ? 1 : -1;
                    if (row == 3) axis = ((axis + s) % 7 + 7) % 7;
                    else if (row == 4) invert = !invert;
                    else if (row >= 5 && row < 16)
                    {
                        int i = row - 5, n2 = 2 + ((jobs[i] - 2 + s) % 11 + 11) % 11, old = jobs[i];   // 0.6.2's StepAssign: 2..12, an action that had it takes the old one
                        for (int k = 0; k < jobs.Length; k++) if (k != i && jobs[k] == n2) jobs[k] = old;
                        jobs[i] = n2;
                    }
                }
            }
            return (axis, invert, jobs, row, side);
        }

        // A hand on the stick scrolling down HOTAS setup: a push down for each row, and the stick resting a little left between (held
        // there for a moment on some rows), as Dan's hand would. 60 samples a second.
        static List<Vector2> LeaningScroll()
        {
            var t = new List<Vector2>();
            void Hold(Vector2 v, float s) { for (int i = 0; i < Mathf.RoundToInt(s * 60); i++) t.Add(v); }
            void Down() { Hold(new Vector2(-.15f, -.9f), .2f); Hold(Vector2.zero, .2f); }   // y as the menus read it: -1 = down the menu
            void Lean(float x, float s) { Hold(new Vector2(x, .1f), s); Hold(Vector2.zero, .2f); }
            Down(); Down(); Down();            // to Throttle axis
            Lean(-.75f, .45f);                 // resting left: two steps (Auto -> dial -> ry)
            Down(); Lean(-.7f, .45f);          // Invert lever: twice (back to On)
            Down(); Down();                    // Pause, Level out
            Lean(-.8f, 1.2f);                  // held left: eight steps
            Down(); Lean(-.7f, .15f);          // Camera view: one step left
            Down(); Lean(.7f, .15f);           // Shield: one step right
            Down(); Down();
            return t;
        }

        IEnumerator Fix63()
        {
            yield return WaitFor(() => G.Now == GameShell.State.Title && G.Menu.Current != null, 60f);
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            hid = AddDanHotas();
            hStick = new Vector2(-.49f, 0); hLever = .3f; HidNow(); for (int k = 0; k < 4; k++) yield return null;
            Log($"virtual HOTAS from Dan's HID descriptor: \"{hid?.name}\" layout {hid?.layout}, a Joystick {hid is Joystick}; HotasPresent {Controls.HotasPresent}; Unity's control names: {string.Join(" ", hid.allControls.Where(c => c.parent == hid).Select(c => c.name))}");
            { var h0 = hid.TryGetChildControl("hat"); var hu = hid.TryGetChildControl("hat/up"); Log($"  hat state: parent byte {h0.stateBlock.byteOffset - hid.stateBlock.byteOffset} bit {h0.stateBlock.bitOffset} size {h0.stateBlock.sizeInBits}; hat/up byte {hu.stateBlock.byteOffset - hid.stateBlock.byteOffset} bit {hu.stateBlock.bitOffset} size {hu.stateBlock.sizeInBits}"); }
            Log($"  hat at rest reads {Controls.Asset.FindAction("Navigate").ReadValue<Vector2>()}; stick reads {Controls.HotasRaw} (sent x -0.49); lever {Controls.LeverAxisName} at 30 % reads {Controls.ThrottleLever * 100:0} %; Fire bound to {string.Join(", ", Controls.Asset.FindAction("Fire").controls.Where(c => c.device == hid).Select(c => c.path))}; Submit {string.Join(", ", Controls.Asset.FindAction("Submit").controls.Where(c => c.device == hid).Select(c => c.path))}");
            hStick = Vector2.zero; HidNow(); yield return null; yield return null;   // the stick centred (the menus' trace starts here)
            Controls.SetCalibration(new float[] { -1, 0, 1, -1, 0, 1 });   // a known centre, so the stick sent = the stick the menus read
            hStick = new Vector2(0, .9f); HidNow(); yield return null; yield return null; float ySign = Mathf.Sign(Controls.HotasStick.y); hStick = Vector2.zero; HidNow(); yield return null; yield return null;
            Log($"  stick y sent +0.9 reads {ySign:+0;-0} on the menus' axis (so the trace below is sent with that sign)");

            // ---- 2. the cause: 0.6.2's rows replayed with a leaning stick
            Log("--- what changed Dan's settings (item 2)");
            var dan = (axis: 5, invert: true, jobs: new[] { 2, 5, 8, 4, 6, 7, 9, 12, 11, 10, 3 });
            var trace = LeaningScroll();
            var old = Replay062(trace, true); var now = Replay062(trace, false);
            bool same = old.axis == dan.axis && old.invert == dan.invert && old.jobs.SequenceEqual(dan.jobs);
            Log($"  0.6.2's menus, the stick scrolling down HOTAS setup while resting left ({trace.Count / 60f:0.0} s): {old.sideSteps} left / right steps; throttle axis {OldAxes[old.axis]} ({old.axis}), invert {old.invert}, jobs [{string.Join(",", old.jobs)}]");
            Log($"  Dan's settings.json (00:26): throttle axis ry (5), invert true, jobs [{string.Join(",", dan.jobs)}] -> {(same ? "IDENTICAL: reproduced" : "different")}");
            Log($"  the same stick on 0.6.3's menus (up / down only from the stick): {now.sideSteps} left / right steps, ended on row {now.row} (0.6.2: {old.row})");
            // the other candidates: loading old settings, the setup steps, the resets
            var j061 = "{\"version\":2,\"throttleAxis\":0,\"invertThrottle\":true,\"hotasButtons\":[12,2,8,3,5,6,7,11,10,9,4],\"hotasSymbolButtons\":[1,3,2,10,9,4,5,6,7,8,11,12],\"musicVolume\":70}";
            var j062 = "{\"version\":3,\"throttleAxis\":0,\"throttleAxisFound\":\"\",\"invertThrottle\":true,\"hotasButtons\":[12,2,8,3,5,6,7,11,10,9,4],\"hotasSymbolButtons\":[10,3,1,2,9,4,5,6,7,8,11,12],\"musicVolume\":70}";
            var jDan = "{\"version\":3,\"throttleAxis\":5,\"throttleAxisFound\":\"\",\"invertThrottle\":true,\"hotasButtons\":[2,5,8,4,6,7,9,12,11,10,3],\"hotasSymbolButtons\":[10,3,1,2,9,4,5,6,7,8,11,12],\"musicVolume\":70}";
            foreach (var (name, js) in new[] { ("0.6.1 defaults", j061), ("0.6.2 defaults", j062), ("Dan's 00:26 file", jDan) })
            {
                var l = GameSettings.LoadedFrom(js);
                Log($"  loading {name}: version {l.version}, lever found \"{l.throttleAxisFound}\" (no throttle-axis setting any more: Auto), forward=min {l.leverForwardIsMin}, jobs [{string.Join(",", l.hotasButtons)}], names [{string.Join(",", l.hotasSymbolButtons)}] (R1 = button {l.hotasSymbolButtons[1]}), music {l.musicVolume}; saved again: {(JsonUtility.ToJson(l).Contains("throttleAxis\"") ? "still has throttleAxis" : "no throttleAxis / invertThrottle keys")}");
            }
            Log("  loading never changes jobs or picks an axis; Identify changes only names; Assign one job; Calibrate the calibration; Find the lever; the resets give the defaults (each checked below or in fix62). Only left / right on the setup rows gives axis 5 + those jobs.");

            // ---- 3. the stick on 0.6.3's menus: HOTAS setup and Settings, twist and lever moving too
            Log("--- no accidental changes (item 3)");
            yield return OpenHotasSetup();
            var setup = G.HotasSetupPage;
            Log($"  HOTAS setup rows: {string.Join(" / ", setup.Rows.Where(r => r.Visible == null || r.Visible()).Select(r => r.Label()))}");
            string before = SettingsNow(); int sel0 = setup.Selected; int moves = 0; int lastSel = setup.Selected;
            for (int i = 0; i < trace.Count; i++)
            {
                hStick = new Vector2(trace[i].x, trace[i].y * ySign); hTwist = Mathf.Sin(i * .21f) * .6f; hLever = .5f + Mathf.Sin(i * .05f) * .45f; HidNow();
                yield return null;
                if (setup.Selected != lastSel) { moves++; lastSel = setup.Selected; }
                if (G.Menu.Current != setup) { Log($"  (left HOTAS setup to {G.Menu.Current?.Title})"); break; }
            }
            hStick = Vector2.zero; hTwist = 0; HidNow(); yield return null; yield return null;
            Log($"  the trace on HOTAS setup (stick leaning left / right, twist and lever moving): selection moved {moves} times (row {sel0} -> {setup.Selected} \"{setup.Rows[setup.Selected].Label()}\"); settings {(SettingsNow() == before ? "UNCHANGED OK" : "CHANGED FAIL: " + SettingsNow())}");
            // the hat on a job row: left / right does nothing; the stick sideways does nothing
            setup.Selected = Row("Level out"); G.Menu.Show(setup, true); yield return null;
            yield return HidHatTap(6); yield return HidHatTap(2); hStick = new Vector2(-.9f, 0); HidNow(); yield return new WaitForSecondsRealtime(.8f); hStick = Vector2.zero; HidNow(); yield return null;
            Log($"  Level out row, hat left + right and the stick held left 0.8 s: job {Controls.HotasName(Controls.HotasButtonFor(Controls.HotasAct.LevelOut))}; settings {(SettingsNow() == before ? "UNCHANGED OK" : "CHANGED FAIL")}");
            yield return Shot("fix63_hotas_setup");
            // Settings: the stick sideways never steps; the hat does; select steps a row too
            G.Menu.Show(G.TitlePage); G.Menu.Current.Selected = Row("Settings"); G.Menu.Show(G.Menu.Current, true); yield return Press(GamepadButton.South);
            yield return WaitFor(() => G.Menu.Current?.Title == "SETTINGS", 3f);
            var st = G.Menu.Current; st.Selected = Row("Flight assist"); G.Menu.Show(st, true); yield return null;
            string fa0 = GameSettings.AssistNames[GameSettings.S.flightAssist];
            hStick = new Vector2(.9f, 0); HidNow(); yield return new WaitForSecondsRealtime(1f); hStick = Vector2.zero; HidNow(); yield return null;
            string fa1 = GameSettings.AssistNames[GameSettings.S.flightAssist];
            yield return HidHatTap(2); string fa2 = GameSettings.AssistNames[GameSettings.S.flightAssist];
            yield return HidPress(Controls.FireButton); string fa3 = GameSettings.AssistNames[GameSettings.S.flightAssist];
            yield return HidHatTap(6); yield return HidHatTap(6); string fa4 = GameSettings.AssistNames[GameSettings.S.flightAssist];
            Log($"  Settings > Flight assist: {fa0}; stick held right 1 s -> {fa1} ({(fa1 == fa0 ? "no change OK" : "CHANGED FAIL")}); hat right -> {fa2}; R1 (select) -> {fa3}; hat left twice -> {fa4}");
            int r0 = st.Selected; yield return HidHatTap(4); int r1 = st.Selected; hStick = new Vector2(0, -.9f * ySign); HidNow(); yield return new WaitForSecondsRealtime(.2f); hStick = Vector2.zero; HidNow(); yield return null; int r2 = st.Selected;
            Log($"  hat down: row {r0} -> {r1}; stick pulled (down the menu as read): -> {r2}");
            GameSettings.S.flightAssist = 0;   // back to the check pilot's

            // ---- 1 + 6: the Test view and the corrected names (pictures)
            Log("--- names and the Test view (items 1, 6)");
            Log($"  button names 1..12: {string.Join(" ", Enumerable.Range(1, 12).Select(Controls.HotasName))}; Fire / select: {Controls.Btn(Controls.Act.Fire, Controls.Device.Hotas)}; Shield: {Controls.Btn(Controls.Act.Shield, Controls.Device.Hotas)}; menus: {Controls.Btn(Controls.Act.Menus, Controls.Device.Hotas)}");
            yield return OpenHotasSetup();
            yield return Shot("fix63_hotas_setup_test_view");
            G.Menu.Show(G.ControlsPage); G.ControlsSheetUi.Showing = Controls.Device.Hotas; yield return null; yield return null;
            yield return Shot("fix63_controls_hotas");

            // ---- 5. the recorder
            Log("--- input recorder (item 5)");
            yield return OpenHotasSetup();
            setup.Selected = Row("Record my input"); G.Menu.Show(setup, true); yield return Press(GamepadButton.South);
            Log($"  started: {InputRecorder.Active}; \"{InputRecorder.Status}\"");
            float tr = Time.realtimeSinceStartup; bool pressedOnce = false;
            while (InputRecorder.Active && Time.realtimeSinceStartup - tr < 25f)
            {
                float k = Time.realtimeSinceStartup - tr;
                hStick = new Vector2(Mathf.Sin(k * 2) * .8f, Mathf.Cos(k * 1.5f) * .8f); hLever = .5f + Mathf.Sin(k) * .5f; hTwist = Mathf.Sin(k * 3) * .5f; HidNow();
                if (k > 3 && !pressedOnce)
                {
                    pressedOnce = true;
                    // the Test view while recording (buttons don't act on the menu then): R1 held, then R3 held
                    hButtons = 1; hStick = new Vector2(.3f, .5f); hLever = .64f; hTwist = .4f; HidNow(); yield return null; yield return null; yield return null;
                    Log($"  Test view, R1 held (recording): {G.SetupSheetUi.Report?.Invoke()?.Replace("\n", " | ")}");
                    yield return Shot("fix63_test_view_R1_held");
                    hButtons = 1u << 2; HidNow(); yield return null; yield return null; yield return null;
                    Log($"  Test view, button 3 held: held line \"{G.SetupSheetUi.Report?.Invoke()?.Split('\n').Last()}\"; page still {G.Menu.Current?.Title}");
                    yield return Shot("fix63_test_view_R3_held");
                    hButtons = 0; HidNow();
                    yield return HidPress(1, .3f); yield return HidPress(3, .3f); yield return HidHatTap(0); yield return Press(GamepadButton.South);
                }
                yield return null;
            }
            hStick = Vector2.zero; hTwist = 0; HidNow();
            var path = InputRecorder.FilePath; var lines = File.Exists(path) ? File.ReadAllLines(path) : new string[0];
            Log($"  after {Time.realtimeSinceStartup - tr:0.0} s: active {InputRecorder.Active}; \"{InputRecorder.LastNote}\"; {lines.Length} lines; still on HOTAS setup {G.Menu.Current == setup} (the pad's A during it did nothing: {G.Menu.Current == setup && !G.Capturing})");
            foreach (var want in new[] { "/trigger  pressed", "Flight/Fire", "/stick/x", "/z ", "/button3  pressed", "UI/Submit", "HID capabilities" })
                Log($"    contains \"{want}\": {(lines.Any(l => l.Contains(want)) ? "yes" : "NO")}");
            Log("    first change lines: " + string.Join(" || ", lines.SkipWhile(l => !l.StartsWith("changes")).Skip(1).Where(l => l.Contains("trigger") || l.Contains("button3")).Take(4)));
            yield return Shot("fix63_recorded");

            // ---- 4. the trigger fires: Free Flight, then the Campaign
            Log("--- the trigger fires (item 4)");
            G.Menu.Hide(); hLever = .7f; HidNow(); yield return null;
            G.StartFlight(1984, CrashLevel.Bounce);
            yield return WaitFor(() => G.Now == GameShell.State.Flying, 30f);
            yield return new WaitForSecondsRealtime(1f);
            yield return FireTest("Free Flight");
            // ---- 6c / 6d: the mix, measured in Free Flight at cruise (lever 70 %), muted
            yield return MixCheck();
            yield return BackToTitle();
            yield return ToCampaignMenu();
            yield return Nav(GamepadButton.DpadDown, 1); yield return Press(GamepadButton.South);   // New career
            yield return WaitPhase(() => G.Now == GameShell.State.Docked && G.Phase == "docked", 60f, "the hangar");
            yield return LaunchFromHangar();
            yield return new WaitForSecondsRealtime(1f);
            yield return FireTest("Campaign");
            yield return BackToTitle();

            // ---- 6d: Settings > Sound, Test voices (muted: the lines are logged, nothing heard)
            Log("--- Settings > Sound (items 6c, 6d)");
            G.Menu.Show(G.TitlePage); G.Menu.Current.Selected = Row("Settings"); G.Menu.Show(G.Menu.Current, true); yield return Press(GamepadButton.South);
            yield return WaitFor(() => G.Menu.Current?.Title == "SETTINGS", 3f);
            G.Menu.Current.Selected = Row("Sound"); G.Menu.Show(G.Menu.Current, true); yield return Press(GamepadButton.South); yield return null;
            var sound = G.Menu.Current;
            Log($"  rows: {string.Join(" / ", sound.Rows.Select(r => r.Label() + (r.Value != null ? " " + r.Value() : "")))}");
            int vl = Voices.Log.Count;
            sound.Selected = Row("Test voices"); G.Menu.Show(sound, true); yield return Press(GamepadButton.South);
            float tv = Time.realtimeSinceStartup; float compPeak = 0;
            while (Time.realtimeSinceStartup - tv < 16f && (G.Voice.Testing || Time.realtimeSinceStartup - tv < 1f)) { compPeak = Mathf.Max(compPeak, G.Voice.ComputerRms); yield return null; }
            Log($"  Test voices: {string.Join(" || ", Voices.Log.Skip(vl))}; done in {Time.realtimeSinceStartup - tv:0.0} s; output {TestRun.Report()}");
            yield return Shot("fix63_sound_page");
        }

        // from flight, the pause menu, or the crash page: "Return to title" by its name
        IEnumerator BackToTitle()
        {
            if (G.Now == GameShell.State.Flying) { yield return Press(GamepadButton.Start); yield return WaitFor(() => G.Now == GameShell.State.Paused, 3f); }
            if (G.Now != GameShell.State.Title && G.Menu.Current != null && G.Menu.Current.Rows.Any(r => r.Label().ToLowerInvariant().Contains("title")))
            { G.Menu.Current.Selected = G.Menu.Current.Rows.FindIndex(r => r.Label().ToLowerInvariant().Contains("title")); G.Menu.Show(G.Menu.Current, true); yield return Press(GamepadButton.South); }
            yield return WaitFor(() => G.Now == GameShell.State.Title && G.Menu.Current != null, 40f);
            Log($"  back on the title: {G.Now == GameShell.State.Title} ({G.Menu.Current?.Title})");
        }

        IEnumerator FireTest(string where)
        {
            var F = G.Foes; int shots0 = F.PlayerShots; float e0 = G.Ship.Energy;
            bool heldSeen = false;
            hButtons = 1; HidNow(); float t0 = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - t0 < 1f) { heldSeen |= Controls.FireHeld; yield return null; }
            yield return Shot($"fix63_fire_{where.Replace(" ", "_").ToLowerInvariant()}");
            hButtons = 0; HidNow(); yield return new WaitForSecondsRealtime(.6f);
            Log($"  {where}: HID button 1 held 1 s: Fire action held {heldSeen}; shots {F.PlayerShots - shots0}; energy {e0:0.0} -> {G.Ship.Energy:0.0} %; {(F.PlayerShots - shots0 > 0 ? "FIRES OK" : "NO SHOTS FAIL")} (state {G.Now}, landed {G.Ship.Landed})");
        }

        IEnumerator MixCheck()
        {
            Log("--- the sound mix in Free Flight, lever 70 %, muted (RMS: engine / wind from the ship's sound, the whole mix at the listener before it is zeroed; music = mix - engine - wind)");
            var t = Tuning.T; var s = GameSettings.S; var sa = G.ShipSound;
            yield return WaitFor(() => G.Music.Playing != "" && G.Music.PlayingVolume > .95f, 12f);
            Log($"  music: \"{G.Music.Playing}\" gain {G.Music.PlayingVolume:0.00} (tracks loaded {G.Music.LoadedTracks}, folder {G.Music.Folder})");
            float Db(float a, float b) => 20 * Mathf.Log10(Mathf.Max(1e-6f, a) / Mathf.Max(1e-6f, b));
            IEnumerator Measure(string tag)
            {
                yield return new WaitForSecondsRealtime(1.5f);
                float e = 0, w = 0, m = 0; int n = 0; float end = Time.realtimeSinceStartup + 3f;
                float mixSum = 0;
                while (Time.realtimeSinceStartup < end) { Autopilot(); e += sa.EngineRms; w += sa.WindRms; mixSum += TestRun.RecentRms; n++; yield return null; }
                e /= n; w /= n; float mix = mixSum / n; float ew = Mathf.Sqrt(e * e + w * w);
                // the music from its file's own level (the four flight tracks are -15.8 .. -16.3 dB RMS: ffmpeg volumedetect), at the music
                // volume and the deck's gain: the same moment of the track doesn't matter then
                m = Mathf.Pow(10, -16f / 20) * GameSettings.S.musicVolume / 100f * G.Music.PlayingVolume;
                Log($"  {tag}: engine {e:0.0000}  wind {w:0.0000}  engine+wind {ew:0.0000}  music {m:0.0000}  -> music over engine+wind {Db(m, ew):+0.0;-0.0} dB; whole mix {mix:0.0000}  (speed {G.Ship.Speed * 3.6f:0} km/h, throttle {G.Ship.Throttle * 100:0} %)");
                mixLast = (ew, m);
            }
            float em = t.engineMix, wm = t.windMix; int ev = s.engineVolume, mv = s.musicVolume;
            t.engineMix = 1; t.windMix = 1; s.engineVolume = 100; s.musicVolume = 70;
            yield return Measure("0.6.2's mix (engine / wind 1 x Effects 100 %, music 70 %)");
            var before = mixLast;
            t.engineMix = em; t.windMix = wm; s.engineVolume = ev; s.musicVolume = mv;
            yield return Measure($"0.6.3's defaults (engineMix {em}, windMix {wm}, Engine {ev} %, music {mv} %)");
            var after = mixLast;
            Log($"  change: engine+wind {Db(after.Item1, before.Item1):+0.0;-0.0} dB, music {Db(after.Item2, before.Item2):+0.0;-0.0} dB; music over engine+wind {Db(before.Item2, before.Item1):+0.0;-0.0} -> {Db(after.Item2, after.Item1):+0.0;-0.0} dB");
            // a voice line: what ducks
            float e0 = sa.EngineRms, mix0 = TestRun.RecentRms;
            G.Voice.Say("cpu_energy_low", VoiceKind.Computer, 3);
            float tEnd = Time.realtimeSinceStartup + 4f, eMin = 9, mixMax = 0, duckMax = 0, musicDuck = 1;
            while (Time.realtimeSinceStartup < tEnd) { Autopilot(); if (G.Voice.Speaking) { eMin = Mathf.Min(eMin, sa.EngineRms); mixMax = Mathf.Max(mixMax, TestRun.RecentRms); duckMax = Mathf.Max(duckMax, Voices.DuckAmount); musicDuck = Mathf.Min(musicDuck, G.Music.Duck); } yield return null; }
            Log($"  a computer warning (cpu_energy_low, with its chime): duck {duckMax:0.00}; engine {e0:0.0000} -> {eMin:0.0000} ({Db(eMin, e0):+0.0;-0.0} dB), music x{musicDuck:0.00} ({20 * Mathf.Log10(Mathf.Max(.001f, musicDuck)):+0.0;-0.0} dB), effects x{1 - t.voiceDuckEffects:0.00}; the whole mix {mix0:0.0000} -> loudest {mixMax:0.0000} while it speaks ({Db(mixMax, mix0):+0.0;-0.0} dB: the voice on top); voices log: {Voices.Log.LastOrDefault()}");
            Log($"  output level (muted): {TestRun.Report()}");
            state.leftStick = Vector2.zero; Set();
        }
        (float, float) mixLast;
    }
}
