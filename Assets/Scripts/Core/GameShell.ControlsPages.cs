using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HostileHorizon
{
    // Fix 6.1: Settings > Controls (one device at a time: the one used last, switchable; a picture with every mapping and the controls
    // grouped, action | button) and Settings > Controls > HOTAS setup (shown while a HOTAS is plugged in): Calibrate stick, Identify your
    // buttons (press each button lit on the picture: names the buttons by their printed symbols), throttle axis, invert lever and every
    // assignable button ("press a button to assign"), with the stick / twist / lever / hat / buttons live below. No keyboard text on it.
    // Fix 6.2: a big live throttle readout, Find throttle lever, calibration asked first and done in three plain steps (○ / Back cancels),
    // Reset HOTAS settings; the Yes / No page every reset uses (Confirm).
    // Fix 6.3: the Throttle axis and Invert lever rows are gone, and no row changes a button job with left / right (0.6.2's stick
    // leaning sideways while Dan scrolled did exactly that): jobs change only through "press a button"; the page is a live Test view
    // (the picture lights each button held); Record my input (20 s) on this page and on Controls (InputRecorder).
    public sealed partial class GameShell
    {
        MenuPanel.Page controlsPage, hotasSetup, confirmPage;
        ControlsSheet controlsSheet, setupSheet;
        enum Capture { None, CalibCentre, CalibEdges, Identify, Assign, FindLever }
        Capture capture; float captureT; int captureIdx; Controls.HotasAct assignAct; List<int> heldPrev = new List<int>();
        Vector2 calSum; int calN; Vector2 calLast; float[] calWork; string captureNote = ""; float leverDoneAt, leverLast, leverStill;
        readonly bool[] calReached = new bool[4];   // forward, back, left, right

        public static readonly string[] DeviceNames = { "Xbox controller / Steam Deck", "Keyboard and mouse", "HOTAS" };
        public MenuPanel.Page ControlsPage => controlsPage;
        public MenuPanel.Page HotasSetupPage => hotasSetup;
        public MenuPanel.Page ConfirmPage => confirmPage;
        public MenuPanel.Page TitlePage => title;   // checks
        public ControlsSheet ControlsSheetUi => controlsSheet;
        public ControlsSheet SetupSheetUi => setupSheet;
        public bool Capturing => capture != Capture.None;
        public string CaptureState => capture.ToString();
        public string CaptureNote => captureNote;

        void ShowControls() => Menu.Show(controlsPage);

        // Fix 6.2: "Reset ...? Yes / No". No is selected first; either answer goes back to the page it came from, on the same row.
        void Confirm(string question, string body, System.Action yes, MenuPanel.Page back)
        {
            confirmPage.Title = question; confirmPage.Body = () => body; confirmPage.Back = () => Menu.Show(back, true);
            confirmPage.Rows.Clear();
            confirmPage.Add(Action("No", () => Menu.Show(back, true))).Add(Action("Yes", () => { Menu.Show(back, true); yes(); }));
            Menu.Show(confirmPage);
        }
        // A page's "Reset this page to defaults" row
        MenuPanel.Row ResetRow(string what, System.Action reset, System.Func<MenuPanel.Page> page) =>
            Action("Reset this page to defaults", () => Confirm($"Reset {what} to the defaults?", "Your careers, saves and the Field Journal are not changed.",
                () => { reset(); Saved(); }, page()));

        void BuildControlsPages()
        {
            confirmPage = new MenuPanel.Page { Title = "RESET?" };

            controlsSheet = ControlsSheet.Create(Menu.Root, false);
            controlsPage = new MenuPanel.Page { Title = "CONTROLS", Subtitle = "For the device you used last; left / right shows another",
                Back = () => Menu.Show(settings), Sheet = controlsSheet.gameObject, Shown = () => controlsSheet.Showing = Controls.LastDevice,
                CaptureInput = () => InputRecorder.Active, Body = () => InputRecorder.Active || InputRecorder.LastNote != "" ? $"<color=#FFD233>{InputRecorder.Status}</color>" : "" };
            controlsPage.Add(Option("Showing", () => DeviceNames[(int)controlsSheet.Showing], d => controlsSheet.Showing = (Controls.Device)Wrap((int)controlsSheet.Showing + d, 3)))
                .Add(new MenuPanel.Row { Label = () => "HOTAS setup", Submit = () => Menu.Show(hotasSetup), Visible = () => Controls.HotasPresent || HotasProbe.OnDeckOrProton })   // Round 11 (A1): on the Deck / Proton too, with the fix steps
                .Add(Action("Record my input (20 s)", InputRecorder.Begin))
                .Add(Action("Back", () => Menu.Show(settings)));

            setupSheet = ControlsSheet.Create(Menu.Root, true); setupSheet.Showing = Controls.Device.Hotas; setupSheet.Report = SetupReport; setupSheet.Top = () => Controls.HotasPresent ? Controls.ThrottleReadout() : "";
            hotasSetup = new MenuPanel.Page { Title = "HOTAS SETUP", SubtitleFn = () => "Thrustmaster T.Flight HOTAS 4" + (HotasProbe.OnDeckOrProton ? "  ·  arrives as: " + HotasProbe.FormName(HotasProbe.Diagnose(out _)) : ""), Back = () => { EndCapture(); Menu.Show(controlsPage); },
                Sheet = setupSheet.gameObject, CaptureInput = () => InputRecorder.Active || CaptureTick(), RowsShown = 8,
                Hint = () => InputRecorder.Active ? "Recording: buttons don't act on the menu now, so try any of them" : capture == Capture.None ? Controls.MenuHint(true, Controls.Device.Hotas)
                    : capture == Capture.Identify ? "A button you don't have: wait 8 seconds and it is skipped"
                    : capture == Capture.Assign ? "Wait 10 seconds to keep the button it has"
                    : $"{Controls.Btn(Controls.Act.Back, Controls.Device.Hotas)} or Back: stop, nothing changes" };
            hotasSetup.Add(new MenuPanel.Row { Label = () => "Calibrate stick", Value = () => GameSettings.S.hotasCalibrated ? "done" : "not yet",
                    Submit = () => Confirm("Calibrate the stick?", "Takes about 10 seconds. Your current calibration is kept if you stop part way.", StartCalibration, hotasSetup) })
                .Add(Action("Find throttle lever", StartFindLever))
                .Add(Action("Identify your buttons", StartIdentify))
                .Add(Action("Record my input (20 s)", InputRecorder.Begin));
            foreach (var h in Controls.HotasActs)
            {
                var act = h.act;
                hotasSetup.Add(new MenuPanel.Row
                {
                    Label = () => h.label, Value = () => Controls.HotasName(Controls.HotasButtonFor(act)),
                    Submit = () => StartAssign(act),   // Fix 6.3: no left / right: only "press a button" changes a job
                    Visible = () => Controls.HotasPresent,   // Round 11 (A2): without a HOTAS the page shows the Deck steps instead
                });
            }
            hotasSetup.Add(Action("Reset HOTAS settings", () => Confirm("Reset all HOTAS settings?",
                    "Stick calibration, the throttle lever found, button names, button jobs and the deadzone go back to the defaults.\nYour careers, saves and the Field Journal are not changed.",
                    () => { GameSettings.ResetHotas(); Controls.ForgetCalibration(); Controls.ForgetLeverMoves(); Saved(); captureNote = "HOTAS settings are back to the defaults."; }, hotasSetup)))
                .Add(Action("Back", () => { EndCapture(); Menu.Show(controlsPage); }));
        }

        // ---- the setup steps (the page's CaptureInput: true while one runs, so the menu doesn't act on these presses)
        void StartCalibration()
        {
            capture = Capture.CalibCentre; captureT = 0; calSum = Vector2.zero; calN = 0; calLast = Controls.HotasRaw; Controls.Calibrating = true;
            for (int k = 0; k < 4; k++) calReached[k] = false;
            captureNote = ""; setupSheet.CalStep = 1;
        }
        void StartFindLever() { capture = Capture.FindLever; captureT = 0; leverDoneAt = -1; leverStill = 0; leverLast = float.NaN; Controls.FindingLever = true; Controls.ForgetLeverMoves(); captureNote = ""; heldPrev = Controls.HotasButtonsHeld(); }
        int identifyFireWas = 1;
        void StartIdentify() { identifyFireWas = Controls.FireButton; capture = Capture.Identify; captureIdx = 0; captureT = 0; heldPrev = Controls.HotasButtonsHeld(); setupSheet.Highlight = 0; }
        void StartAssign(Controls.HotasAct a) { capture = Capture.Assign; assignAct = a; captureT = 0; heldPrev = Controls.HotasButtonsHeld(); }
        void EndCapture() { capture = Capture.None; Controls.Calibrating = false; Controls.FindingLever = false; setupSheet.Highlight = -1; setupSheet.CalStep = 0; }

        // A HOTAS button pressed since the last frame (0: none)
        int NewHotasPress()
        {
            var now = Controls.HotasButtonsHeld(); int n = now.FirstOrDefault(b => !heldPrev.Contains(b)); heldPrev = now; return n;
        }

        bool CaptureTick()
        {
            if (capture == Capture.None) return false;
            // Calibrate and Find lever: circle (the HOTAS's back button), B or Esc stop them. Identify / assign need every HOTAS button
            // free, so there only the pad / keyboard stops them.
            bool hotasMayCancel = capture == Capture.CalibCentre || capture == Capture.CalibEdges || capture == Capture.FindLever;
            if (Controls.CancelPressed && (hotasMayCancel || Controls.LastDevice != Controls.Device.Hotas))
            {
                captureNote = capture == Capture.FindLever ? "Stopped; the throttle axis is unchanged." : capture == Capture.CalibCentre || capture == Capture.CalibEdges ? "Stopped; the old calibration is kept." : "Stopped; nothing changed.";
                EndCapture(); return true;
            }
            float dt = Time.unscaledDeltaTime; captureT += dt;
            var r = Controls.HotasRaw;
            switch (capture)
            {
                case Capture.CalibCentre:   // step 1: still for 1.5 s, that is the centre
                    if ((r - calLast).magnitude > .03f) { captureT = 0; calSum = Vector2.zero; calN = 0; calLast = r; }
                    calSum += r; calN++;
                    if (captureT > 1.5f)
                    {
                        var c = calSum / Mathf.Max(1, calN);
                        calWork = new[] { c.x, c.x, c.x, c.y, c.y, c.y }; capture = Capture.CalibEdges; captureT = 0; heldPrev = Controls.HotasButtonsHeld();
                        setupSheet.CalStep = 2;
                    }
                    break;
                case Capture.CalibEdges:    // step 2: each edge (forward / back / left / right), step 3: the trigger
                    calWork[0] = Mathf.Min(calWork[0], r.x); calWork[2] = Mathf.Max(calWork[2], r.x); calWork[3] = Mathf.Min(calWork[3], r.y); calWork[5] = Mathf.Max(calWork[5], r.y);
                    calReached[0] |= calWork[5] - calWork[4] > .3f; calReached[1] |= calWork[4] - calWork[3] > .3f;
                    calReached[2] |= calWork[1] - calWork[0] > .3f; calReached[3] |= calWork[2] - calWork[1] > .3f;
                    bool all = calReached.All(b => b);
                    setupSheet.CalStep = all ? 3 : 2;
                    if (NewHotasPress() == Controls.FireButton || Controls.SubmitPressed && Controls.LastDevice != Controls.Device.Hotas)
                    {
                        if (!all) { captureNote = "Not finished: push the stick to " + string.Join(", ", new[] { "forward", "back", "left", "right" }.Where((s, k) => !calReached[k])) + " first."; break; }
                        Controls.SetCalibration(calWork); GameSettings.Save();
                        captureNote = "Stick calibrated.";
                        EndCapture();
                    }
                    break;
                case Capture.FindLever:     // move the lever all the way: the axis that moved furthest (not the stick or the twist) is saved
                    {                       // Fix 6.3: and its direction: it is left fully forward, so the end it rests at is "full"
                        string best = Controls.MostMoved(.5f);
                        bool atEnd = false, atMin = true;
                        if (best != null)
                        {
                            var (lo, hi) = Controls.AxisRange(best); float v = Controls.AxisNow(best), span = Mathf.Max(.01f, hi - lo);
                            atMin = v - lo < span * .12f; atEnd = atMin || hi - v < span * .12f;
                            if (float.IsNaN(leverLast) || Mathf.Abs(v - leverLast) > span * .03f) leverStill = 0; else leverStill += dt;
                            leverLast = v;
                        }
                        if (best != null && Controls.Moved(best) >= 1.6f && leverDoneAt < 0) leverDoneAt = captureT;
                        bool done = leverDoneAt >= 0 && atEnd && leverStill > 1f || NewHotasPress() == Controls.FireButton && best != null;
                        if (done)
                        {
                            Controls.SetFoundLever(best); if (atEnd) GameSettings.S.leverForwardIsMin = atMin; Controls.ApplySettings(); GameSettings.Save();
                            captureNote = $"Throttle lever found ({best} axis); fully forward = full throttle. Move it and watch the throttle readout."; EndCapture();
                        }
                        else if (captureT > 20f) { captureNote = "No lever moved; the throttle axis is unchanged."; EndCapture(); }
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
                                // Fix 6.3: R1 fires and selects: a job on that button moves to the button R1 had before
                                var jobs = GameSettings.S.hotasButtons; for (int k = 0; k < jobs.Length; k++) if (jobs[k] == Controls.FireButton) jobs[k] = identifyFireWas;
                                Controls.ApplySettings(); GameSettings.Save(); captureNote = "Buttons identified: the prompts and the picture use these names."; EndCapture();
                            }
                            else setupSheet.Highlight = captureIdx;
                        }
                    }
                    break;
                case Capture.Assign:
                    {
                        int n = NewHotasPress();
                        if (n == Controls.FireButton) captureNote = $"{Controls.HotasName(n)} (the trigger) fires and selects: pick another button.";
                        else if (n > 0) { Controls.AssignHotas(assignAct, n); GameSettings.Save(); captureNote = $"{Controls.HotasActs[(int)assignAct].label}: {Controls.HotasName(n)}"; EndCapture(); }
                        else if (captureT > 10f) { captureNote = "No button pressed; nothing changed."; EndCapture(); }
                    }
                    break;
            }
            if (capture == Capture.CalibEdges || capture == Capture.CalibCentre)
            {
                var c = calWork != null && capture == Capture.CalibEdges ? new Vector2(calWork[1], calWork[4]) : calLast;
                setupSheet.CalDot = r - c; System.Array.Copy(calReached, setupSheet.CalReached, 4);
            }
            return true;
        }

        string SetupReport()
        {
            string Big(string s) => $"<size=30><color=#FFD233><b>{s}</b></color></size>";
            string trig = $"{Controls.HotasName(Controls.FireButton)} (the trigger)";
            if (InputRecorder.Active || InputRecorder.LastNote != "") return (InputRecorder.Active ? Big(InputRecorder.Status) : $"<size=20><color=#FFD233>{InputRecorder.Status}</color></size>") + "\n" + Controls.HotasReport();
            switch (capture)
            {
                case Capture.CalibCentre: return Big("Step 1 of 3: let go of the stick") + $"\nKeep your hand off it while it reads the centre… {Mathf.Clamp01(captureT / 1.5f) * 100:0}%";
                case Capture.CalibEdges:
                    return calReached.All(b => b)
                        ? Big($"Step 3 of 3: press {trig} to finish") + "\nAll four edges reached. You can keep moving the stick first."
                        : Big("Step 2 of 3: push the stick to each edge") + "\nForward, back, left and right, as far as it goes (or move it slowly in full circles), 2 or 3 times.\nEach marker on the picture turns green when that edge is reached.\n" + (captureNote.StartsWith("Not finished") ? $"<color=#FFD233>{captureNote}</color>" : "");
                case Capture.FindLever:
                    return Big("Move the throttle lever all the way back and forward, then leave it fully forward") +
                        $"\n{(leverDoneAt >= 0 ? "Got it… leave it fully forward for a second." : "It finishes by itself once the lever has gone end to end and rests fully forward.")}";
                case Capture.Identify: return Big($"Press  {Controls.SymbolText(captureIdx)}") + $"\n(lit on the picture; {captureIdx + 1} of {Controls.HotasSymbols.Length}; wait 8 seconds to skip)";
                case Capture.Assign: return Big($"Press a button for: {Controls.HotasActs[(int)assignAct].label}") + "\n" + captureNote;
            }
            // Round 11 (A1, A2): under Proton without the HOTAS as a joystick: what reaches the game instead, and the fix steps
            if (!Controls.HotasPresent && HotasProbe.OnDeckOrProton) { var f = HotasProbe.Diagnose(out var why); return $"<size=18>Found: {why}</size>\n" + HotasProbe.Steps(f); }
            return (captureNote != "" ? $"<color=#FFD233>{captureNote}</color>\n" : "") + Controls.HotasReport();
        }
    }
}
