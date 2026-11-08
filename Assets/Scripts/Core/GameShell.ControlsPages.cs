using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HostileHorizon
{
    // Fix 6.1: Settings > Controls (one device at a time: the one used last, switchable; a picture with every mapping and the controls
    // grouped, action | button) and Settings > Controls > HOTAS setup (shown while a HOTAS is plugged in): Calibrate stick, Identify your
    // buttons (press each button lit on the picture: names the buttons by their printed symbols), throttle axis, invert lever and every
    // assignable button ("press a button to assign"), with the stick / twist / lever / hat / buttons live below. No keyboard text on it.
    public sealed partial class GameShell
    {
        MenuPanel.Page controlsPage, hotasSetup;
        ControlsSheet controlsSheet, setupSheet;
        enum Capture { None, CalibCentre, CalibRange, Identify, Assign }
        Capture capture; float captureT; int captureIdx; Controls.HotasAct assignAct; List<int> heldPrev = new List<int>();
        Vector2 calSum; int calN; Vector2 calLast; float[] calWork; string captureNote = "";

        public static readonly string[] DeviceNames = { "Xbox controller / Steam Deck", "Keyboard and mouse", "HOTAS" };
        public MenuPanel.Page ControlsPage => controlsPage;
        public MenuPanel.Page HotasSetupPage => hotasSetup;
        public ControlsSheet ControlsSheetUi => controlsSheet;
        public ControlsSheet SetupSheetUi => setupSheet;
        public bool Capturing => capture != Capture.None;

        void ShowControls() => Menu.Show(controlsPage);

        void BuildControlsPages()
        {
            controlsSheet = ControlsSheet.Create(Menu.Root, false);
            controlsPage = new MenuPanel.Page { Title = "CONTROLS", Subtitle = "For the device you used last; left / right shows another",
                Back = () => Menu.Show(settings), Sheet = controlsSheet.gameObject, Shown = () => controlsSheet.Showing = Controls.LastDevice };
            controlsPage.Add(Option("Showing", () => DeviceNames[(int)controlsSheet.Showing], d => controlsSheet.Showing = (Controls.Device)Wrap((int)controlsSheet.Showing + d, 3)))
                .Add(new MenuPanel.Row { Label = () => "HOTAS setup", Submit = () => Menu.Show(hotasSetup), Visible = () => Controls.HotasPresent })
                .Add(Action("Back", () => Menu.Show(settings)));

            setupSheet = ControlsSheet.Create(Menu.Root, true); setupSheet.Showing = Controls.Device.Hotas; setupSheet.Report = SetupReport;
            hotasSetup = new MenuPanel.Page { Title = "HOTAS SETUP", Subtitle = "Thrustmaster T.Flight HOTAS 4", Back = () => { EndCapture(); Menu.Show(controlsPage); },
                Sheet = setupSheet.gameObject, CaptureInput = CaptureTick, RowsShown = 8,
                Hint = () => capture == Capture.None ? Controls.MenuHint(true, Controls.Device.Hotas) : capture == Capture.Identify ? "A button you don't have: wait 8 s and it is skipped" : "" };
            hotasSetup.Add(new MenuPanel.Row { Label = () => "Calibrate stick", Value = () => GameSettings.S.hotasCalibrated ? "done" : "not yet", Submit = StartCalibration })
                .Add(Action("Identify your buttons", StartIdentify))
                .Add(Option("Throttle axis", () => GameSettings.S.throttleAxis == 0 ? $"Auto ({Controls.ThrottleAxisPath.Replace("<Joystick>/", "")})" : GameSettings.ThrottleAxes[GameSettings.S.throttleAxis],
                    d => { GameSettings.S.throttleAxis = Wrap(GameSettings.S.throttleAxis + d, GameSettings.ThrottleAxes.Length); Saved(); }))
                .Add(Option("Invert lever", () => GameSettings.S.invertThrottle ? "On" : "Off", d => { GameSettings.S.invertThrottle = !GameSettings.S.invertThrottle; Saved(); }));
            foreach (var h in Controls.HotasActs)
            {
                var act = h.act;
                hotasSetup.Add(new MenuPanel.Row
                {
                    Label = () => h.label, Value = () => Controls.HotasName(Controls.HotasButtonFor(act)),
                    Submit = () => StartAssign(act),
                    Left = () => StepAssign(act, -1), Right = () => StepAssign(act, 1),
                });
            }
            hotasSetup.Add(Action("Back", () => { EndCapture(); Menu.Show(controlsPage); }));
        }

        void StepAssign(Controls.HotasAct act, int d)
        {
            int n = Controls.HotasButtonFor(act); n = 2 + Wrap(n - 2 + d, Controls.HotasButtonCount - 1);   // 2..12: the trigger stays fire / select
            Controls.AssignHotas(act, n); GameSettings.Save();
        }

        // ---- the setup steps (the page's CaptureInput: true while one runs, so the menu doesn't act on these presses)
        void StartCalibration() { capture = Capture.CalibCentre; captureT = 0; calSum = Vector2.zero; calN = 0; calLast = Controls.HotasRaw; Controls.Calibrating = true; }
        void StartIdentify() { capture = Capture.Identify; captureIdx = 0; captureT = 0; heldPrev = Controls.HotasButtonsHeld(); setupSheet.Highlight = 0; }
        void StartAssign(Controls.HotasAct a) { capture = Capture.Assign; assignAct = a; captureT = 0; heldPrev = Controls.HotasButtonsHeld(); }
        void EndCapture() { capture = Capture.None; Controls.Calibrating = false; setupSheet.Highlight = -1; }

        // A HOTAS button pressed since the last frame (0: none)
        int NewHotasPress()
        {
            var now = Controls.HotasButtonsHeld(); int n = now.FirstOrDefault(b => !heldPrev.Contains(b)); heldPrev = now; return n;
        }

        bool CaptureTick()
        {
            if (capture == Capture.None) return false;
            if (Controls.CancelPressed && Controls.LastDevice != Controls.Device.Hotas) { captureNote = "Stopped; nothing changed."; EndCapture(); return true; }
            float dt = Time.unscaledDeltaTime; captureT += dt;
            var r = Controls.HotasRaw;
            switch (capture)
            {
                case Capture.CalibCentre:   // still for 1.5 s: that is the centre
                    if ((r - calLast).magnitude > .03f) { captureT = 0; calSum = Vector2.zero; calN = 0; calLast = r; }
                    calSum += r; calN++;
                    if (captureT > 1.5f)
                    {
                        var c = calSum / Mathf.Max(1, calN);
                        calWork = new[] { c.x, c.x, c.x, c.y, c.y, c.y }; capture = Capture.CalibRange; captureT = 0; heldPrev = Controls.HotasButtonsHeld();
                    }
                    break;
                case Capture.CalibRange:    // round its full range, then the trigger
                    calWork[0] = Mathf.Min(calWork[0], r.x); calWork[2] = Mathf.Max(calWork[2], r.x); calWork[3] = Mathf.Min(calWork[3], r.y); calWork[5] = Mathf.Max(calWork[5], r.y);
                    if (NewHotasPress() == 1 || Controls.SubmitPressed && Controls.LastDevice != Controls.Device.Hotas)
                    {
                        bool full = calWork[1] - calWork[0] > .2f && calWork[2] - calWork[1] > .2f && calWork[4] - calWork[3] > .2f && calWork[5] - calWork[4] > .2f;
                        if (!full) { captureNote = "The stick didn't go far enough each way: move it right round, then press the trigger."; break; }
                        Controls.SetCalibration(calWork); GameSettings.Save();
                        captureNote = $"Calibrated: centre {calWork[1]:+0.00;-0.00} / {calWork[4]:+0.00;-0.00}, reach {calWork[0]:+0.00;-0.00} to {calWork[2]:+0.00;-0.00}, {calWork[3]:+0.00;-0.00} to {calWork[5]:+0.00;-0.00}.";
                        EndCapture();
                    }
                    break;
                case Capture.Identify:      // each symbol lit in turn: the button pressed is that symbol; 8 s without one skips it
                    {
                        int n = NewHotasPress();
                        if (n > 0 || captureT > 8f)
                        {
                            var map = GameSettings.S.hotasSymbolButtons;
                            if (n > 0) { for (int k = 0; k < map.Length; k++) if (map[k] == n) map[k] = 0; map[captureIdx] = n; }
                            captureIdx++; captureT = 0;
                            if (captureIdx >= Controls.HotasSymbols.Length)
                            {
                                for (int k = 0; k < map.Length; k++) if (map[k] == 0) map[k] = Controls.DefaultSymbolButtons[k];   // skipped: the default
                                GameSettings.Save(); captureNote = "Buttons identified: the prompts and the picture use these names."; EndCapture();
                            }
                            else setupSheet.Highlight = captureIdx;
                        }
                    }
                    break;
                case Capture.Assign:
                    {
                        int n = NewHotasPress();
                        if (n == 1) captureNote = $"{Controls.HotasName(1)} (the trigger) fires and selects: pick another button.";
                        else if (n > 1) { Controls.AssignHotas(assignAct, n); GameSettings.Save(); captureNote = $"{Controls.HotasActs[(int)assignAct].label}: {Controls.HotasName(n)}"; EndCapture(); }
                        else if (captureT > 10f) { captureNote = "No button pressed; nothing changed."; EndCapture(); }
                    }
                    break;
            }
            return true;
        }

        string SetupReport()
        {
            string Big(string s) => $"<size=30><color=#FFD233><b>{s}</b></color></size>";
            switch (capture)
            {
                case Capture.CalibCentre: return Big("Let go of the stick") + $"\nHold still… {Mathf.Clamp01(captureT / 1.5f) * 100:0}%";
                case Capture.CalibRange: return Big("Move the stick right round, all the way, a few times") + $"\nthen press {Controls.HotasName(1)} (the trigger).\n" + (captureNote.StartsWith("The stick") ? captureNote : "");
                case Capture.Identify: return Big($"Press  {Controls.SymbolText(captureIdx)}") + $"\n(lit on the picture; {captureIdx + 1} of {Controls.HotasSymbols.Length}; wait 8 s to skip)";
                case Capture.Assign: return Big($"Press a button for: {Controls.HotasActs[(int)assignAct].label}") + "\n" + captureNote;
            }
            return (captureNote != "" ? $"<color=#FFD233>{captureNote}</color>\n" : "") + Controls.HotasReport();
        }
    }
}
