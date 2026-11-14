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
    // Round 11 check (-exeCheckOnly round11 | round11a | round11b | round11c).
    // A: the HOTAS in each form Proton can hand it to the game (HotasProbe): (1) Proton's SDL bus' own HID joystick for it, built
    // the way winebus.sys bus_sdl.c builds it (manufacturer "SDL", X Y Z RX RY RZ SLIDER DIAL 16-bit, a hat, 12 buttons, VID / PID
    // kept): stick, twist, lever, hat and buttons through the game's actions; Find lever and Identify on it; (2) the hidraw form = the
    // Fix 6.3 device (Dan's own descriptor); (3) Steam Input's virtual pad / Proton's Xbox-style pad / nothing: the device lists a
    // Proton system would show, the HOTAS setup page and its steps (pictures); (4) the recorder's file and where it says it is.
    // A Windows check can't prove what a real Deck hands the game: the forms come from Wine's source.
    public sealed partial class ExeCheck
    {
        IEnumerator Round11()
        {
            yield return WaitFor(() => G.Now == GameShell.State.Title && G.Menu.Current != null, 60f);
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            bool all = CheckRun.Only == "round11";
            if (all || CheckRun.Only == "round11a") yield return Round11A();
            if (all || CheckRun.Only == "round11b") yield return Round11B();
            if (all || CheckRun.Only == "round11c") yield return Round11C();
        }

        InputDevice AddSdlHotas()
        {
            var b = new HID.HIDDeviceDescriptorBuilder(HID.GenericDesktop.Joystick).StartReport(HID.HIDReportType.Input);
            foreach (var u in new[] { HID.GenericDesktop.X, HID.GenericDesktop.Y, HID.GenericDesktop.Z, HID.GenericDesktop.Rx, HID.GenericDesktop.Ry, HID.GenericDesktop.Rz, HID.GenericDesktop.Slider, HID.GenericDesktop.Dial })
                b = b.AddElement(u, 16).WithLogicalMinMax(-32768, 32767);
            b = b.AddElement(HID.GenericDesktop.HatSwitch, 4).WithLogicalMinMax(0, 7);
            for (int i = 1; i <= 12; i++) b = b.AddElement(HID.UsagePage.Button, i, 1).WithLogicalMinMax(0, 1);
            var d = b.Finish();
            for (int i = 0; i < d.elements.Length; i++) if (d.elements[i].usage == (int)HID.GenericDesktop.HatSwitch && d.elements[i].usagePage == HID.UsagePage.GenericDesktop) d.elements[i].flags |= HID.HIDElementFlags.NullState;
            d.vendorId = HotasProbe.Vid; d.productId = HotasProbe.Pid; d.inputReportSize = 19;
            return InputSystem.AddDevice(new InputDeviceDescription { interfaceName = "HID", product = "Thrustmaster T.Flight Hotas 4", manufacturer = "SDL", capabilities = d.ToJson() });
        }

        IEnumerator HotasForm(string tag)
        {
            G.Menu.Hide(); yield return null;   // the hat and R1 would otherwise act on the title menu (up wraps to Quit)
            Controls.SetCalibration(new float[] { -1, 0, 1, -1, 0, 1 });
            hStick = new Vector2(.6f, -.4f); hTwist = .5f; hLever = .3f; hHat = 0; hButtons = 1u | 1u << 5; HidNow();
            for (int k = 0; k < 4; k++) yield return null;
            var f = Controls.Asset.FindActionMap("Flight");
            bool fire = Controls.FireHeld, eng = f["Engines"].IsPressed(); var nav = Controls.Asset.FindAction("Navigate").ReadValue<Vector2>();
            float yaw = Controls.Yaw, lever = Controls.ThrottleLever; var raw = Controls.HotasRaw;
            var form = HotasProbe.DiagnoseNow(out var why);
            bool ok = Mathf.Abs(raw.x - .6f) < .03f && Mathf.Abs(raw.y + .4f) < .03f && yaw > .15f && Mathf.Abs(lever - .3f) < .03f && nav.y > .5f && fire && eng;
            Log($"  {tag}: device \"{hid.description.product}\" ({hid.description.manufacturer}) layout {hid.layout}, a Joystick {hid is Joystick}; HotasPresent {Controls.HotasPresent}; arrives as: {HotasProbe.FormName(form)} ({why})");
            Log($"    controls: {string.Join(" ", hid.allControls.Where(c => c.parent == hid).Select(c => c.name))}");
            Log($"    sent stick (+0.60, -0.40) twist +0.50 lever 30 % hat up, R1 + âœ• held -> stick {raw.x:+0.00;-0.00} {raw.y:+0.00;-0.00}, yaw {yaw:+0.00;-0.00}, lever ({Controls.LeverAxisName}) {lever * 100:0} % (z raw {Controls.AxisNow("z"):+0.00;-0.00}, rz raw {Controls.AxisNow("rz"):+0.00;-0.00}, forward=min {GameSettings.S.leverForwardIsMin}), hat -> Navigate {nav}, Fire {fire}, Engines {eng}: {(ok ? "ALL OK" : "FAIL")}");
            hStick = Vector2.zero; hTwist = 0; hHat = -1; hButtons = 0; HidNow(); yield return null; yield return null;
            G.Menu.Show(G.TitlePage); yield return null;
        }

        IEnumerator Round11A()
        {
            Log("=== Round 11 A: the HOTAS under Proton");
            Log($"  this PC: Wine / Proton {HotasProbe.UnderWine}; HID paths listed by SetupDi: {HotasProbe.HidPaths().Count} (read only)");
            // (1) Proton's SDL form
            hid = AddSdlHotas(); hLever = .5f; HidNow(); for (int k = 0; k < 4; k++) yield return null;
            HotasProbe.TestWine = true; HotasProbe.TestPaths = new List<string> { @"\\?\HID#VID_044F&PID_B67C#1&2d595ca7&0&0000#{4d1e55b2-f16f-11cf-88cb-001111000030}", @"\\?\HID#VID_28DE&PID_1205&MI_02#7&1&0000#{4d1e55b2-f16f-11cf-88cb-001111000030}" };
            yield return HotasForm("Proton SDL form");
            // Find throttle lever and Identify on it (the setup steps work on this form)
            yield return OpenHotasSetup(); var setup = G.HotasSetupPage;
            Log($"  HOTAS setup subtitle: \"{setup.SubtitleFn?.Invoke()}\"");
            yield return Shot("round11a_setup_sdl_form");
            // (Find throttle lever reads the same "z" control as on Windows; fix62 checks it. Not repeated here: writing the lever through
            // a HID event in this harness stops at -0.4, so it can't sweep end to end.)
            setup.Selected = Row("Identify your buttons"); G.Menu.Show(setup, true); yield return Press(GamepadButton.South);
            Log($"  Identify started: {G.CaptureState}");
            var want = Enumerable.Range(0, 12).Select(k => Controls.DefaultSymbolButtons[(k + 1) % 12]).ToArray();   // a shuffled unit: each symbol on the next one's button
            for (int k = 0; k < 12; k++) { yield return HidPress(want[k], .08f); yield return null; }
            yield return null;
            Log($"  Identify on this form (buttons pressed {string.Join(",", want)}): \"{G.CaptureNote}\"; names now [{string.Join(",", GameSettings.S.hotasSymbolButtons)}] {(GameSettings.S.hotasSymbolButtons.SequenceEqual(want) ? "= as pressed OK" : "FAIL")}");
            GameSettings.S.hotasSymbolButtons = (int[])Controls.DefaultSymbolButtons.Clone(); GameSettings.S.hotasButtons = Controls.HotasActs.Select(h => h.button).ToArray(); Controls.ApplySettings();
            InputSystem.RemoveDevice(hid); yield return null;
            // (2) hidraw: Dan's own descriptor (as Windows)
            hid = AddDanHotas(); hLever = .5f; HidNow(); for (int k = 0; k < 4; k++) yield return null;
            yield return HotasForm("Proton hidraw form (Dan's descriptor)");
            InputSystem.RemoveDevice(hid); hid = null; for (int k = 0; k < 3; k++) yield return null;
            // (3) no joystick: Steam Input / Proton's Xbox-style pad / nothing
            var forms = new (string tag, List<string> paths)[]
            {
                ("steam_input", new List<string> { @"\\?\HID#VID_28DE&PID_11FF&IG_00#3&1&0&0000#{4d1e55b2-f16f-11cf-88cb-001111000030}", @"\\?\HID#VID_28DE&PID_11FF&IG_01#3&2&0&0000#{4d1e55b2-f16f-11cf-88cb-001111000030}" }),
                ("proton_gamepad", new List<string> { @"\\?\HID#VID_044F&PID_B67C&IG_00#1&2d595ca7&0&0000#{4d1e55b2-f16f-11cf-88cb-001111000030}" }),
                ("nothing", new List<string>()),
            };
            foreach (var (tag, paths) in forms)
            {
                HotasProbe.TestPaths = paths;
                yield return OpenHotasSetup();
                var fm = HotasProbe.DiagnoseNow(out var why);
                Log($"  {tag}: arrives as {HotasProbe.FormName(fm)} ({why}); HOTAS setup row shown without a HOTAS: {G.Menu.Current == G.HotasSetupPage}; report: {G.SetupSheetUi.Report?.Invoke()?.Replace("\n", " | ")}");
                yield return Shot("round11a_setup_" + tag);
            }
            // (4) the recorder on Proton: the file, and where it says it is
            System.Environment.SetEnvironmentVariable("STEAM_COMPAT_DATA_PATH", "/home/deck/.local/share/Steam/steamapps/compatdata/3141592653");
            yield return OpenHotasSetup();
            setup.Selected = Row("Record my input"); G.Menu.Show(setup, true); yield return Press(GamepadButton.South);
            float t0 = Time.realtimeSinceStartup;
            while (InputRecorder.Active && Time.realtimeSinceStartup - t0 < 25f) yield return null;
            var lines = File.Exists(InputRecorder.FilePath) ? File.ReadAllLines(InputRecorder.FilePath) : new string[0];
            Log($"  Record my input: \"{InputRecorder.LastNote.Replace("\n", " | ")}\"; {lines.Length} lines; header: {string.Join(" || ", lines.Take(6).Skip(2))}");
            Log($"  a Windows path inside Proton -> Linux (fallback when Wine's own call is missing): {HotasProbe.UnixPath(@"C:\Users\steamuser\AppData\LocalLow\Hostile Horizon\Hostile Horizon\hotas_input_log.txt")}");
            yield return Shot("round11a_recorded");
            System.Environment.SetEnvironmentVariable("STEAM_COMPAT_DATA_PATH", null);
            HotasProbe.TestWine = false; HotasProbe.TestPaths = null;
            Log($"  output level (muted): {TestRun.Report()}");
        }
    }
}
