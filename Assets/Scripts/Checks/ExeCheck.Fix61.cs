using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

namespace HostileHorizon
{
    // A virtual HOTAS for the checks: a joystick with the same control names as the T.Flight HOTAS 4 (stick, twist, slider, hat,
    // trigger, button2..button12). Its stick takes raw values, so a check can play Dan's real (off-centre) stick through the game.
    [StructLayout(LayoutKind.Explicit, Size = 24)]
    public struct CheckHotasState : IInputStateTypeInfo
    {
        public FourCC format => new FourCC('C', 'H', 'K', 'H');
        [InputControl(name = "stick", layout = "Stick", usage = "Primary2DMotion")] [FieldOffset(0)] public Vector2 stick;
        [InputControl(name = "twist", layout = "Axis", usage = "Twist")] [FieldOffset(8)] public float twist;
        [InputControl(name = "slider", layout = "Axis")] [FieldOffset(12)] public float slider;
        [InputControl(name = "hat", layout = "Dpad", usage = "Hatswitch", format = "BIT", sizeInBits = 4, bit = 0)] [FieldOffset(16)] public uint hat;
        [InputControl(name = "trigger", layout = "Button", bit = 0, usage = "PrimaryTrigger")]
        [InputControl(name = "button2", layout = "Button", bit = 1)] [InputControl(name = "button3", layout = "Button", bit = 2)]
        [InputControl(name = "button4", layout = "Button", bit = 3)] [InputControl(name = "button5", layout = "Button", bit = 4)]
        [InputControl(name = "button6", layout = "Button", bit = 5)] [InputControl(name = "button7", layout = "Button", bit = 6)]
        [InputControl(name = "button8", layout = "Button", bit = 7)] [InputControl(name = "button9", layout = "Button", bit = 8)]
        [InputControl(name = "button10", layout = "Button", bit = 9)] [InputControl(name = "button11", layout = "Button", bit = 10)]
        [InputControl(name = "button12", layout = "Button", bit = 11)]
        [FieldOffset(20)] public uint buttons;
    }
    [InputControlLayout(stateType = typeof(CheckHotasState), displayName = "Check HOTAS")]
    public class CheckHotas : Joystick { }

    // Fix 6.1 checks, in the built exe (muted, no window):
    //  -exeCheckOnly input  [-inputSeconds N]: what the Input System reads from a real joystick (a batch-mode run sees none: Dan's HOTAS
    //      was logged through Windows' own joystick and HID calls instead, Data/screenshots/fix61).
    //  -exeCheckOnly fix61 [-hotasTrace <csv>]: the menu with a virtual HOTAS: Dan's recorded up / down (off-centre stick) and a lean /
    //      diagonal trace move the selection without changing a value (and what the old rule did with the same input); left / right
    //      still change values; the hat, twist and lever; calibration; identify / assign; the device in use and its prompts; pictures of
    //      Controls for each device and HOTAS setup. fix61pics = the pictures only (for the Deck's 1280 x 800).
    public sealed partial class ExeCheck
    {
        IEnumerator InputDump()
        {
            yield return WaitFor(() => G.Now == GameShell.State.Title, 60f);
            float secs = 20; var a = LauncherBridge.Argument("-inputSeconds"); if (a != null) float.TryParse(a, out secs);
            var joys = InputSystem.devices.OfType<Joystick>().ToList();
            Log($"joysticks seen by the Input System: {joys.Count}; gamepads: {string.Join(", ", Gamepad.all.Select(g => g.displayName))}");
            foreach (var j in joys)
            {
                Log($"<{j.layout}> \"{j.displayName}\" {j.description.interfaceName} {j.description.product}");
                Log("HID descriptor: " + j.description.capabilities);
                foreach (var c in j.allControls) Log($"  control {c.path.Substring(j.path.Length)} ({c.GetType().Name})");
            }
        }

        CheckHotas hotas; CheckHotasState hs; Keyboard keys;
        void HSet() => InputSystem.QueueStateEvent(hotas, hs);
        IEnumerator HButton(int n) { hs.buttons |= 1u << (n - 1); HSet(); yield return null; yield return null; hs.buttons &= ~(1u << (n - 1)); HSet(); yield return null; yield return null; }
        IEnumerator HHat(uint bit) { hs.hat = bit; HSet(); yield return null; yield return null; hs.hat = 0; HSet(); yield return new WaitForSecondsRealtime(.05f); }
        IEnumerator Key(Key k) { InputSystem.QueueStateEvent(keys, new KeyboardState(k)); yield return null; yield return null; InputSystem.QueueStateEvent(keys, new KeyboardState()); yield return null; yield return null; }

        string Values(MenuPanel.Page p) => string.Join(" | ", p.Rows.Where(r => r.Value != null).Select(r => r.Value()));
        const float RestX = -.496f;   // Dan's stick centred: HID 258 of 0..1023

        // Play a raw stick trace through the virtual HOTAS on the current page; count selection moves and value changes, and what the
        // old rule (0.6.0: |y| > .55 up / down, else |x| > .55 left / right, no centring, no lock) would have done with the same input.
        IEnumerator Replay(string name, List<Vector3> trace)
        {
            var p = G.Menu.Current; string before = Values(p); int moves = 0, changes = 0, sel = p.Selected; string last = before;
            int oldDir = 0, oldSide = 0, oldMoves = 0; float oldTimer = 0;
            float t0 = Time.realtimeSinceStartup; int i = 0;
            while (i < trace.Count)
            {
                float t = Time.realtimeSinceStartup - t0;
                while (i < trace.Count && trace[i].x <= t) { hs.stick = new Vector2(trace[i].y, trace[i].z); i++; }
                HSet(); yield return null;
                if (p.Selected != sel) { moves++; sel = p.Selected; }
                var v = Values(p); if (v != last) { changes++; Log($"  {name}: VALUE CHANGED at {t:0.00} s: {last}  ->  {v}"); last = v; }
                var raw = hs.stick;   // the old rule read the raw stick
                int d = Mathf.Abs(raw.y) > .55f ? (raw.y > 0 ? 1 : 2) : Mathf.Abs(raw.x) > .55f ? (raw.x > 0 ? 4 : 3) : 0;
                bool fire = false;
                if (d != oldDir) { oldDir = d; fire = d != 0; oldTimer = .38f; } else if (d != 0) { oldTimer -= Time.unscaledDeltaTime; if (oldTimer <= 0) { fire = true; oldTimer = .11f; } }
                if (fire) { if (d >= 3) oldSide++; else oldMoves++; }
            }
            hs.stick = new Vector2(RestX, 0); HSet(); yield return null; yield return null;
            Log($"{name}: {trace.Count} samples over {trace[trace.Count - 1].x:0.0} s; now: selection moved {moves} times, values changed {changes} times " +
                $"({(changes == 0 ? "OK" : "FAIL")}); the 0.6.0 rule on the same input: {oldMoves} up / down, {oldSide} left / right (each a value change on an option row)");
        }

        List<Vector3> DanTrace()
        {
            var path = LauncherBridge.Argument("-hotasTrace"); var list = new List<Vector3>();
            if (path == null || !File.Exists(path)) { Log("no -hotasTrace file: Dan's recorded trace skipped"); return list; }
            foreach (var l in File.ReadAllLines(path).Skip(1))
            {
                var f = l.Split(','); if (f.Length < 3) continue;
                list.Add(new Vector3(float.Parse(f[0], System.Globalization.CultureInfo.InvariantCulture), float.Parse(f[1], System.Globalization.CultureInfo.InvariantCulture), float.Parse(f[2], System.Globalization.CultureInfo.InvariantCulture)));
            }
            return list;
        }

        // Synthetic: up / down pushes leaning sideways (0.45 either way, calibrated), each one returning to the centre through a diagonal.
        static List<Vector3> LeanTrace()
        {
            var list = new List<Vector3>(); float t = 0;
            void Add(float x, float y) { t += .02f; list.Add(new Vector3(t, x, y)); }
            for (int n = 0; n < 8; n++)
            {
                float dirY = n % 2 == 0 ? 1 : -1, lean = n % 4 < 2 ? .45f : -.45f;
                for (int k = 0; k <= 10; k++) Add(RestX + lean * k / 10f * (lean > 0 ? .84f : .5f), dirY * k / 10f);   // push out, leaning
                for (int k = 0; k < 10; k++) Add(RestX + lean * (lean > 0 ? .84f : .5f), dirY);                        // hold
                for (int k = 10; k >= 0; k--) Add(RestX + (lean > 0 ? .84f : .5f) * 1.6f * Mathf.Sign(lean) * Mathf.Sin(k / 10f * Mathf.PI) * .5f, dirY * k / 10f);   // back through a diagonal
                for (int k = 0; k < 15; k++) Add(RestX, 0);
            }
            return list;
        }

        IEnumerator Fix61()
        {
            bool picsOnly = CheckRun.Only == "fix61pics";
            yield return WaitFor(() => G.Now == GameShell.State.Title && G.Menu.Current != null, 60f);
            InputSystem.RegisterLayout<CheckHotas>();
            hotas = InputSystem.AddDevice<CheckHotas>("CheckHotas");
            keys = InputSystem.AddDevice<Keyboard>("CheckKeys");
            hs.stick = new Vector2(RestX, 0); hs.slider = .3f; HSet();   // Dan's centred stick, its first report
            for (int k = 0; k < 4; k++) yield return null;
            Log($"virtual HOTAS added: {hotas.displayName}; HotasPresent {Controls.HotasPresent}; stick {Controls.HotasCalState}; raw {Controls.HotasRaw}, used {Controls.HotasStick}");
            Log($"font: {string.Join(" ", new[] { "□", "✕", "○", "△", "▲", "×", "☰", "≡", "⧉", "←", "↓" }.Select(s => s + (UiKit.Font.HasCharacter(s[0]) ? "✓" : "✗")))} -> HOTAS names {string.Join(" ", Enumerable.Range(0, 12).Select(Controls.SymbolText))}; pad Menu \"{Controls.PadMenu}\", View \"{Controls.PadView}\"");
            var rows = G.Menu.Current;
            G.Menu.Current.Selected = Row("Settings"); G.Menu.Show(G.Menu.Current, true); yield return Press(GamepadButton.South);
            yield return WaitFor(() => G.Menu.Current?.Title == "SETTINGS", 3f);
            var settings = G.Menu.Current;

            if (!picsOnly)
            {
                // 1. moving up / down with the HOTAS stick: no value changes
                Log($"Settings values before: {Values(settings)}");
                var dan = DanTrace();
                if (dan.Count > 0) { settings.Selected = 0; yield return Replay("Dan's recorded up / down (2026-11-07, 16 s)", dan); }
                settings.Selected = 0; yield return Replay("up / down leaning sideways, back through a diagonal", LeanTrace());
                Log($"Settings values after: {Values(settings)}");

                // 2. left / right still change values: stick right (Dan's full right is raw +0.34), then left
                settings.Selected = Row("Stick sensitivity"); G.Menu.Show(settings, true); yield return null;
                string v0 = settings.Rows[settings.Selected].Value();
                hs.stick = new Vector2(.34f, 0); HSet(); yield return new WaitForSecondsRealtime(.2f); hs.stick = new Vector2(RestX, 0); HSet(); yield return new WaitForSecondsRealtime(.2f);
                string v1 = settings.Rows[settings.Selected].Value();
                hs.stick = new Vector2(-1f, 0); HSet(); yield return new WaitForSecondsRealtime(.2f); hs.stick = new Vector2(RestX, 0); HSet(); yield return new WaitForSecondsRealtime(.2f);
                string v2 = settings.Rows[settings.Selected].Value();
                Log($"Stick sensitivity with the stick pushed right (raw +0.34): {v0} -> {v1}; then left: {v2} ({(v1 != v0 && v2 == v0 ? "OK" : "FAIL")})");

                // 3. the hat as a D-pad; the twist and the lever never navigate
                int s0 = settings.Selected; yield return HHat(2); yield return HHat(2); int s1 = settings.Selected; yield return HHat(1); int s2 = settings.Selected;
                Log($"hat down, down, up: row {s0} -> {s1} -> {s2} ({(s1 == s0 + 2 && s2 == s0 + 1 ? "OK" : "check")}); hat left / right: change values like the stick");
                string vals = Values(settings); int sel = settings.Selected;
                float t1 = Time.realtimeSinceStartup;
                while (Time.realtimeSinceStartup - t1 < 1.5f) { float k = (Time.realtimeSinceStartup - t1) / 1.5f; hs.twist = Mathf.Sin(k * 6.28f); hs.slider = k; HSet(); yield return null; }
                hs.twist = 0; HSet(); yield return null;
                Log($"twist full left / right and the lever end to end for 1.5 s: selection {(settings.Selected == sel ? "unchanged" : "MOVED")}, values {(Values(settings) == vals ? "unchanged" : "CHANGED")}");

                // 4. flying: the centred HOTAS reads 0; a pad's small stick move isn't hidden by it (0.6.0: the HOTAS's -0.50 won)
                state.leftStick = new Vector2(.3f, 0); Set(); yield return null; yield return null;
                Log($"HOTAS centred, pad left stick x 0.30: Roll {Controls.Roll:+0.00;-0.00;0.00} (pad's), Pitch {Controls.Pitch:0.00}; HOTAS used {Controls.HotasStick}");
                state.leftStick = Vector2.zero; Set(); yield return null; yield return null;
                Log($"both centred: Roll {Controls.Roll:0.00}, Pitch {Controls.Pitch:0.00}");
                hs.stick = new Vector2(.34f, 0); HSet(); yield return null; yield return null; float rr = Controls.Roll;
                hs.stick = new Vector2(-1f, 0); HSet(); yield return null; yield return null; float rl = Controls.Roll;
                hs.stick = new Vector2(RestX, 1f); HSet(); yield return null; yield return null; float pf = Controls.Pitch;
                hs.stick = new Vector2(RestX, 0); HSet(); yield return null; yield return null;
                Log($"HOTAS full right / full left / full forward: Roll {rr:+0.00;-0.00} / {rl:+0.00;-0.00}, Pitch {pf:+0.00;-0.00} (0.6.0 read: right -> +0.34, left -> -1.00, centred -> -0.50)");

                // 5. the device in use and its prompts
                yield return Press(GamepadButton.North); string padKey = Controls.AirlockKey, padDev = Controls.LastDevice.ToString(); yield return Shot("fix61_footer_pad");
                yield return HButton(4); string hKey = Controls.AirlockKey, hDev = Controls.LastDevice.ToString(); yield return Shot("fix61_footer_hotas");
                yield return Key(UnityEngine.InputSystem.Key.C); string kKey = Controls.AirlockKey, kDev = Controls.LastDevice.ToString(); yield return Shot("fix61_footer_keyboard");
                Log($"device in use after a pad Y: {padDev} (airlock \"{padKey}\"); a HOTAS L3: {hDev} (\"{hKey}\"); the C key: {kDev} (\"{kKey}\")");
                yield return Press(GamepadButton.North);
                float tj = Time.realtimeSinceStartup;
                while (Time.realtimeSinceStartup - tj < 1.5f) { hs.stick = new Vector2(RestX + Random.Range(-.03f, .03f), Random.Range(-.03f, .03f)); hs.twist = Random.Range(-.05f, .05f); HSet(); yield return null; }
                hs.stick = new Vector2(RestX, 0); hs.twist = 0; HSet(); yield return null;
                Log($"after the pad, 1.5 s of HOTAS jitter at rest (±0.03 stick, ±0.05 twist): device in use {Controls.LastDevice} ({(Controls.LastDevice == Controls.Device.Gamepad ? "OK" : "FAIL")})");
                Log($"prompts per device: land {Controls.Btn(Controls.Act.Land, Controls.Device.Gamepad)} / {Controls.Btn(Controls.Act.Land, Controls.Device.Keyboard)} / {Controls.Btn(Controls.Act.Land, Controls.Device.Hotas)}; recall {Controls.Btn(Controls.Act.Recall, Controls.Device.Gamepad)} / {Controls.Btn(Controls.Act.Recall, Controls.Device.Keyboard)} / {Controls.Btn(Controls.Act.Recall, Controls.Device.Hotas)}; pause {Controls.Btn(Controls.Act.Pause, Controls.Device.Gamepad)} / {Controls.Btn(Controls.Act.Pause, Controls.Device.Keyboard)} / {Controls.Btn(Controls.Act.Pause, Controls.Device.Hotas)}");
            }

            // 6. Controls page for each device, HOTAS setup
            settings.Selected = Row("Controls"); G.Menu.Show(settings, true); yield return Press(GamepadButton.South);
            yield return WaitFor(() => G.Menu.Current == G.ControlsPage, 3f);
            Log($"Controls page opened from the pad: showing {G.ControlsSheetUi.Showing}");
            foreach (var d in new[] { Controls.Device.Hotas, Controls.Device.Gamepad, Controls.Device.Keyboard })
            { G.ControlsSheetUi.Showing = d; yield return null; yield return null; yield return Shot("fix61_controls_" + d.ToString().ToLowerInvariant()); }
            G.ControlsPage.Selected = Row("HOTAS setup"); G.Menu.Show(G.ControlsPage, true); yield return Press(GamepadButton.South);
            yield return WaitFor(() => G.Menu.Current == G.HotasSetupPage, 3f);
            yield return new WaitForSecondsRealtime(.3f);
            yield return Shot("fix61_hotas_setup");
            if (picsOnly) yield break;

            // 7. identify your buttons: Dan's logged buttons, but L1 / L2 pressed the other way round (the step corrects the names)
            var setup = G.HotasSetupPage;
            setup.Selected = Row("Identify"); G.Menu.Show(setup, true); yield return Press(GamepadButton.South);
            var order = new[] { 1, 3, 2, 9, 10, 4, 5, 6, 7, 8, 11, 12 };
            for (int k = 0; k < order.Length; k++)
            {
                if (k == 6) yield return Shot("fix61_identify_square");
                yield return new WaitForSecondsRealtime(.15f); yield return HButton(order[k]);
            }
            yield return null;
            Log($"identify: pressed {string.Join(",", order)} for {string.Join(" ", Controls.HotasSymbols)}; names now: button 9 = {Controls.HotasName(9)}, 10 = {Controls.HotasName(10)}, 7 = {Controls.HotasName(7)}; capturing {G.Capturing}");
            GameSettings.S.hotasSymbolButtons = (int[])Controls.DefaultSymbolButtons.Clone();

            // 8. assign: Pause to L3 (button 4); the cabin camera (had L3) takes OPTIONS
            setup.Selected = Row("Pause"); G.Menu.Show(setup, true); yield return Press(GamepadButton.South);
            yield return Shot("fix61_assign_wait");
            yield return HButton(4); yield return null;
            Log($"assign Pause -> pressed L3: pause {Controls.HotasName(Controls.HotasButtonFor(Controls.HotasAct.Pause))}, cabin camera {Controls.HotasName(Controls.HotasButtonFor(Controls.HotasAct.CabinCam))}");
            yield return HButton(4); yield return null;
            Log($"L3 pressed in the game: pause pressed {(G.Now == GameShell.State.Title ? "(title: nothing to pause)" : "")}; binding now {Controls.Asset.FindActionMap("Flight")["Menu"].bindings.First(b => b.groups.Contains("HOTAS")).effectivePath}");
            Controls.AssignHotas(Controls.HotasAct.Pause, 12);

            // 9. calibrate: let go, then right round (Dan's reach: x -1..+0.34, y -1..+1), then the trigger
            setup.Selected = Row("Calibrate"); G.Menu.Show(setup, true); yield return Press(GamepadButton.South);
            float tc = Time.realtimeSinceStartup; while (Time.realtimeSinceStartup - tc < 2f) { hs.stick = new Vector2(RestX + Random.Range(-.01f, .01f), Random.Range(-.01f, .01f)); HSet(); yield return null; }
            yield return Shot("fix61_calibrate_range");
            tc = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - tc < 2.5f) { float a = (Time.realtimeSinceStartup - tc) * 5f; float cx = Mathf.Cos(a); hs.stick = new Vector2(cx > 0 ? RestX + cx * (.34f - RestX) : RestX + cx * (1 + RestX), Mathf.Sin(a)); HSet(); yield return null; }
            hs.stick = new Vector2(RestX, 0); HSet(); yield return null;
            yield return HButton(1); yield return null;
            var c = GameSettings.S.hotasCal;
            Log($"calibrated: {GameSettings.S.hotasCalibrated}; x {c[0]:+0.00;-0.00} / {c[1]:+0.00;-0.00} / {c[2]:+0.00;-0.00}, y {c[3]:+0.00;-0.00} / {c[4]:+0.00;-0.00} / {c[5]:+0.00;-0.00}; centred stick reads {Controls.HotasStick}");
            yield return Shot("fix61_hotas_setup_after");

            // 10. the HUD's pause prompt in flight, for the HOTAS and then the pad
            G.Menu.Hide(); G.StartFlight(G.FlightSeed == 0 ? 1984 : G.FlightSeed, CrashLevel.Bounce);
            yield return WaitFor(() => G.Now == GameShell.State.Flying, 30f);
            yield return HButton(9); yield return new WaitForSecondsRealtime(.5f);
            Log($"in flight after HOTAS L2: device {Controls.LastDevice}; HUD pause prompt \"{Controls.PauseKey}: Pause\"");
            yield return Shot("fix61_hud_hotas");
            yield return Press(GamepadButton.DpadRight); yield return new WaitForSecondsRealtime(.5f);
            Log($"after pad D-pad right: device {Controls.LastDevice}; HUD pause prompt \"{Controls.PauseKey}: Pause\"");
            yield return Shot("fix61_hud_pad");
        }
    }
}
