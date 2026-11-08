using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace HostileHorizon
{
    // Round 1, item 5: all input goes through one Input Actions asset (Resources/Input/Controls.inputactions) with a binding set per
    // device: HOTAS (T.Flight HOTAS 4 as a HID joystick), Gamepad (Xbox layout, Steam Deck), Keyboard + mouse. Nothing here reads a
    // device directly for play (VR rule 6); the HOTAS setup page lists the joystick's buttons held only so Dan can see them.
    // Fix 6.1: the HOTAS stick has its own action (HotasStick), calibrated here: Dan's HOTAS reads its stick's left / right at -0.50
    // when centred (HID 258 of 0..1023) and only reaches +0.34 to the right, so the raw value both rolled the ship and pushed the
    // menus left. Calibration = centre + each side's reach (Settings > Controls > HOTAS setup > Calibrate stick, or learnt as it goes:
    // centred on the stick's first report, the reach grows as the stick is moved). The HOTAS buttons are named by the symbol printed on
    // them (PS4 T.Flight HOTAS 4: R2 trigger, R1, R3, L1-L3, square, cross, circle, triangle, SHARE, OPTIONS) and all but the trigger
    // can be assigned. Prompts name the button of the device last used.
    public static class Controls
    {
        public static InputActionAsset Asset { get; private set; }
        static InputAction pitch, roll, yaw, throttleAxis, throttleChange, look, mouseLook, mouseHold, camera, menu, land, engines, airlock, recall, view, bellyCam, routeTarget, cabinCam, levelOut, fire, shield, navigate, submit, cancel, hotasStick;
        public static string ThrottleAxisPath { get; private set; } = "";

        // ---- the HOTAS layout (Fix 6.1)
        public enum HotasAct { Pause, LevelOut, Camera, Shield, Land, Engines, Airlock, Recall, BellyCam, RouteTarget, CabinCam }
        // Defaults mirror the pad: cross = A (engines), circle = B (airlock, menu back), square = X (land), triangle = Y (view),
        // SHARE = View (hold: recall), OPTIONS = Menu (pause); the shoulders take what the D-pad does on a pad.
        public static readonly (HotasAct act, string action, string label, int button)[] HotasActs =
        {
            (HotasAct.Pause, "Menu", "Pause", 12),
            (HotasAct.LevelOut, "LevelOut", "Level out", 2),
            (HotasAct.Camera, "Camera", "Camera view", 8),
            (HotasAct.Shield, "Shield", "Shield", 3),
            (HotasAct.Land, "Land", "Land / air brakes", 5),
            (HotasAct.Engines, "Engines", "Engines on / off", 6),
            (HotasAct.Airlock, "Airlock", "Airlock · menu back", 7),
            (HotasAct.Recall, "Recall", "Recall (hold)", 11),
            (HotasAct.BellyCam, "BellyCam", "Belly camera", 10),
            (HotasAct.RouteTarget, "RouteTarget", "Route target", 9),
            (HotasAct.CabinCam, "CabinCam", "Cabin camera", 4),
        };
        // The symbols printed on the PS4 T.Flight HOTAS 4 and the HID button each one sends (Dan's unit, logged 2026-11-07: trigger 1,
        // R3 2, R1 3, L3 4, square 5, cross 6, circle 7, triangle 8, L1 10, SHARE 11, OPTIONS 12; L2 was not pressed: 9 assumed).
        // "Identify your buttons" (HOTAS setup) corrects it.
        public static readonly string[] HotasSymbols = { "R2", "R1", "R3", "L1", "L2", "L3", "□", "✕", "○", "△", "SHARE", "OPTIONS" };
        public static readonly int[] DefaultSymbolButtons = { 1, 3, 2, 10, 9, 4, 5, 6, 7, 8, 11, 12 };
        public const int HotasButtonCount = 12;

        public static void Init()
        {
            Asset = Resources.Load<InputActionAsset>("Input/Controls");
            var f = Asset.FindActionMap("Flight"); var u = Asset.FindActionMap("UI");
            pitch = f["Pitch"]; roll = f["Roll"]; yaw = f["Yaw"]; throttleAxis = f["ThrottleAxis"]; throttleChange = f["ThrottleChange"];
            look = f["Look"]; mouseLook = f["MouseLook"]; mouseHold = f["MouseLookHold"]; camera = f["Camera"]; menu = f["Menu"];
            land = f["Land"]; engines = f["Engines"]; airlock = f["Airlock"]; recall = f["Recall"]; view = f["View"]; bellyCam = f["BellyCam"];
            routeTarget = f["RouteTarget"]; cabinCam = f["CabinCam"];   // Fix 2.6 / Round 3
            levelOut = f["LevelOut"];   // Round 5 (A2)
            fire = f["Fire"]; shield = f["Shield"];   // Round 5 (B3, B4)
            hotasStick = f["HotasStick"];   // Fix 6.1
            navigate = u["Navigate"]; submit = u["Submit"]; cancel = u["Cancel"];
            f.actionTriggered += Noted; u.actionTriggered += Noted;
            LastDevice = Gamepad.all.Count > 0 ? Device.Gamepad : Joystick.all.Count > 0 ? Device.Hotas : Device.Keyboard;
            ApplySettings();
            Asset.Enable();
            InputSystem.onDeviceChange += (d, c) => { if (d is Joystick && (c == InputDeviceChange.Added || c == InputDeviceChange.Removed)) ApplySettings(); };
        }

        static string ButtonPath(int n) => n <= 1 ? "<Joystick>/trigger" : "<Joystick>/button" + n;
        static int HotasBinding(InputAction a) => a.bindings.IndexOf(b => b.groups != null && b.groups.Contains("HOTAS"));

        // Re-apply the throttle-axis choice (also when a joystick is plugged in) and the HOTAS buttons.
        public static void ApplySettings()
        {
            var s = GameSettings.S;
            int i = s.throttleAxis;
            string axis = i > 0 && i < GameSettings.ThrottleAxes.Length ? GameSettings.ThrottleAxes[i] : null;
            if (axis == null) axis = Joystick.current != null && Joystick.current.TryGetChildControl("slider") != null ? "slider" : "z";
            ThrottleAxisPath = "<Joystick>/" + axis;
            throttleAxis.ApplyBindingOverride(0, ThrottleAxisPath);
            throttleRange = 0;
            if (s.hotasButtons == null) return;
            var f = Asset.FindActionMap("Flight");
            foreach (var h in HotasActs) { var a = f[h.action]; int b = HotasBinding(a); if (b >= 0) a.ApplyBindingOverride(b, ButtonPath(HotasButtonFor(h.act))); }
            int cb = HotasBinding(cancel); if (cb >= 0) cancel.ApplyBindingOverride(cb, ButtonPath(HotasButtonFor(HotasAct.Airlock)));   // menu back = the airlock button
        }

        public static int HotasButtonFor(HotasAct a) { var b = GameSettings.S.hotasButtons; return b != null && (int)a < b.Length ? b[(int)a] : HotasActs[(int)a].button; }
        // Assign a HOTAS button; an action that had it takes this action's old button (no button does two things).
        public static void AssignHotas(HotasAct a, int button)
        {
            var b = GameSettings.S.hotasButtons; int old = b[(int)a];
            for (int k = 0; k < b.Length; k++) if (k != (int)a && b[k] == button) b[k] = old;
            b[(int)a] = button; ApplySettings();
        }

        // ---- the device in use (Fix 2.5; Fix 6.1: only a clear push counts, so a resting HOTAS or a drifting stick doesn't take over)
        public enum Device { Gamepad, Keyboard, Hotas }
        public static Device LastDevice { get; private set; } = Device.Gamepad;
        static void Noted(InputAction.CallbackContext c)
        {
            var a = c.action; var ctl = c.control; if (ctl == null) return;
            if (a == hotasStick) { if (HotasStick.magnitude > .5f) LastDevice = Device.Hotas; return; }
            if (a == throttleAxis || a == mouseLook) return;
            if (ctl.EvaluateMagnitude() < .5f) return;
            var d = ctl.device;
            if (d is Gamepad) LastDevice = Device.Gamepad;
            else if (d is Joystick) LastDevice = Device.Hotas;
            else if (d is Keyboard || d is Mouse) LastDevice = Device.Keyboard;
        }

        // ---- button names per device (Fix 6.1: only the device in use; Xbox names and colours on the pad)
        public enum Act { Fire, Select, Back, Engines, Airlock, Land, Camera, Recall, Pause, Shield, LevelOut, BellyCam, RouteTarget, CabinCam, FastForward, Look, Throttle, Yaw, PitchRoll, Menus }
        static readonly Dictionary<string, string> glyphs = new Dictionary<string, string>();
        // A symbol the UI font can draw, else a plain stand-in.
        public static string Glyph(string want, string alt)
        {
            if (glyphs.TryGetValue(want, out var g)) return g;
            bool ok = true; foreach (var ch in want) ok &= ch < 128 || UiKit.Font.HasCharacter(ch);
            return glyphs[want] = ok ? want : alt;
        }
        static string Pad(string s, string hex) => $"<color=#{hex}><b>{s}</b></color>";
        public static string PadA => Pad("A", "6CC24A"); public static string PadB => Pad("B", "E8473D");
        public static string PadX => Pad("X", "3D8FE8"); public static string PadY => Pad("Y", "F2C12E");
        public static string PadMenu => Glyph("☰", "≡") + " Menu"; public static string PadView => "View";   // the font draws ⧉ blank (Fix 6.1 check)
        // A HOTAS button's printed symbol (or its number if it isn't one of the twelve).
        public static string HotasName(int button)
        {
            var m = GameSettings.S.hotasSymbolButtons ?? DefaultSymbolButtons;
            for (int k = 0; k < m.Length; k++) if (m[k] == button) return SymbolText(k);
            return "button " + button;
        }
        public static string SymbolText(int k)
        {
            var s = HotasSymbols[k];
            // the font draws square, circle and triangle at about half its letters' height: drawn larger (Fix 6.1 pictures)
            string Big(string g, string alt) { var x = Glyph(g, alt); return x == g ? $"<size=36>{g}</size>" : x; }
            return s == "□" ? Big("□", "Square") : s == "✕" ? Glyph("✕", Glyph("×", "Cross")) : s == "○" ? Big("○", "Circle") : s == "△" ? Big("△", "Triangle") : s;
        }
        static string H(HotasAct a) => HotasName(HotasButtonFor(a));

        public static string Btn(Act a) => Btn(a, LastDevice);
        public static string Btn(Act a, Device d)
        {
            if (d == Device.Keyboard)
                switch (a)
                {
                    case Act.Fire: return "Space"; case Act.Select: return "Enter"; case Act.Back: return "Esc"; case Act.Engines: return "K";
                    case Act.Airlock: return "O"; case Act.Land: return "G"; case Act.Camera: return "C"; case Act.Recall: return "hold H";
                    case Act.Pause: return "Esc"; case Act.Shield: return "Z"; case Act.LevelOut: return "L"; case Act.BellyCam: return "V";
                    case Act.RouteTarget: return "T"; case Act.CabinCam: return "N"; case Act.FastForward: return "hold Space";
                    case Act.Look: return "arrows / right mouse"; case Act.Throttle: return "Shift / Ctrl"; case Act.Yaw: return "Q / E";
                    case Act.PitchRoll: return "W S / A D"; case Act.Menus: return "arrows";
                }
            if (d == Device.Hotas)
                switch (a)
                {
                    case Act.Fire: case Act.Select: return HotasName(1); case Act.Back: return H(HotasAct.Airlock);
                    case Act.Engines: return H(HotasAct.Engines); case Act.Airlock: return H(HotasAct.Airlock); case Act.Land: return H(HotasAct.Land);
                    case Act.Camera: return H(HotasAct.Camera); case Act.Recall: return "hold " + H(HotasAct.Recall); case Act.Pause: return H(HotasAct.Pause);
                    case Act.Shield: return H(HotasAct.Shield); case Act.LevelOut: return H(HotasAct.LevelOut); case Act.BellyCam: return H(HotasAct.BellyCam);
                    case Act.RouteTarget: return H(HotasAct.RouteTarget); case Act.CabinCam: return H(HotasAct.CabinCam);
                    case Act.FastForward: return "hold " + HotasName(1); case Act.Look: return "hat"; case Act.Throttle: return "throttle lever";
                    case Act.Yaw: return "twist"; case Act.PitchRoll: return "stick"; case Act.Menus: return "hat / stick";
                }
            switch (a)
            {
                case Act.Fire: case Act.Select: case Act.Engines: return PadA; case Act.Back: case Act.Airlock: return PadB;
                case Act.Land: return PadX; case Act.Camera: return PadY; case Act.Recall: return "hold " + PadView.Trim(); case Act.Pause: return PadMenu;
                case Act.Shield: return "D-pad ←"; case Act.LevelOut: return "left-stick click"; case Act.BellyCam: return "D-pad ↓";
                case Act.RouteTarget: return "D-pad →"; case Act.CabinCam: return "D-pad ↑"; case Act.FastForward: return "hold " + PadA;
                case Act.Look: return "right stick"; case Act.Throttle: return "RT / LT"; case Act.Yaw: return "LB / RB"; case Act.PitchRoll: return "left stick";
                case Act.Menus: return "D-pad / left stick";
            }
            return "";
        }
        // The menus' footer, for the device in use.
        public static string MenuHint(bool back, Device? d = null)
        {
            var dev = d ?? LastDevice;
            return $"{Btn(Act.Menus, dev)}: choose and change     {Btn(Act.Select, dev)}: select" + (back ? $"     {Btn(Act.Back, dev)}: back" : "");
        }

        public static string AirlockKey => Btn(Act.Airlock);
        public static string EngineKey => Btn(Act.Engines);
        public static string LandKey => Btn(Act.Land);
        public static string BellyKey => Btn(Act.BellyCam);
        public static string RouteTargetKey => Btn(Act.RouteTarget);
        public static string RecallKey => Btn(Act.Recall);
        public static string FireKey => Btn(Act.Fire);
        public static string ShieldKey => Btn(Act.Shield);
        public static string LevelOutKey => Btn(Act.LevelOut);
        public static string CabinCamKey => Btn(Act.CabinCam);
        public static string PauseKey => Btn(Act.Pause);
        public static string FastForwardKey => Btn(Act.FastForward);

        public static bool BellyCamPressed => bellyCam.WasPressedThisFrame();
        // Fix 2.6: cycle the route trail's target (next pilot / mothership). Round 3: the cabin camera on the HUD.
        public static bool RouteTargetPressed => routeTarget.WasPressedThisFrame();
        public static bool CabinCamPressed => cabinCam.WasPressedThisFrame();
        // Round 5 (A2): level out at once
        public static bool LevelOutPressed => levelOut.WasPressedThisFrame();
        // Round 5 (B3, B4): the gun (held) and the shield toggle
        public static bool FireHeld => fire.IsPressed();
        public static bool ShieldPressed => shield.WasPressedThisFrame();

        // ---- the HOTAS stick (Fix 6.1)
        public static bool HotasPresent => hotasStick != null && hotasStick.controls.Count > 0;
        public static Vector2 HotasRaw => HotasPresent && hotasStick.controls[0] is InputControl<Vector2> c ? c.ReadValue() : Vector2.zero;
        public static bool HotasCentred { get; private set; }
        public static string HotasCalState => !HotasPresent ? "no HOTAS" : GameSettings.S.hotasCalibrated ? "calibrated" : HotasCentred ? "centred by itself (calibrate for the best feel)" : "waiting for the stick's first move";
        static InputDevice seenDevice; static double seenTime;

        // Every frame (GameShell.Update): centre an uncalibrated stick on its first report, grow the reach as it is moved.
        public static void Tick()
        {
            var d = HotasPresent ? hotasStick.controls[0].device : null;
            if (d != seenDevice) { seenDevice = d; seenTime = d != null ? d.lastUpdateTime : 0; HotasCentred = GameSettings.S.hotasCalibrated; }
            if (d == null || Calibrating) return;
            var r = HotasRaw; var c = GameSettings.S.hotasCal;
            if (!HotasCentred)
            {
                if (d.lastUpdateTime <= seenTime) return;   // the HOTAS reports only on change: nothing yet
                float ex = Mathf.Min(r.x + 1, 1 - r.x), ey = Mathf.Min(r.y + 1, 1 - r.y);
                c[0] = r.x - ex; c[1] = r.x; c[2] = r.x + ex; c[3] = r.y - ey; c[4] = r.y; c[5] = r.y + ey;
                HotasCentred = true;
            }
            c[0] = Mathf.Min(c[0], r.x); c[2] = Mathf.Max(c[2], r.x); c[3] = Mathf.Min(c[3], r.y); c[5] = Mathf.Max(c[5], r.y);
        }
        public static bool Calibrating;   // the HOTAS setup page's calibration: it sets hotasCal itself
        public static void SetCalibration(float[] c) { GameSettings.S.hotasCal = c; GameSettings.S.hotasCalibrated = true; HotasCentred = true; }
        // Reset to "learn it again" (the check, or after a recalibration of the HOTAS itself)
        public static void ForgetCalibration() { GameSettings.S.hotasCalibrated = false; HotasCentred = false; seenDevice = null; }

        static float Side(float v, float mn, float c, float mx) =>
            v >= c ? (mx - c < .05f ? 0 : Mathf.Clamp01((v - c) / (mx - c))) : (c - mn < .05f ? 0 : -Mathf.Clamp01((c - v) / (c - mn)));
        // The calibrated stick, -1..1 each way (up = +y); zero until it has been centred.
        public static Vector2 HotasStick
        {
            get
            {
                if (!HotasPresent || !HotasCentred) return Vector2.zero;
                var r = HotasRaw; var c = GameSettings.S.hotasCal;
                return new Vector2(Side(r.x, c[0], c[1], c[2]), Side(r.y, c[3], c[4], c[5]));
            }
        }

        static float Dead(float v, float dz) { float a = Mathf.Abs(v); return a <= dz ? 0 : Mathf.Sign(v) * (a - dz) / (1 - dz); }
        static float DZ => GameSettings.S.deadzone / 100f;
        static float HotasDZ => Mathf.Max(DZ, .06f);   // Fix 6.1: Dan's stick rests within about 0.05 of its centre after calibration
        static float Pick(float a, float b) => Mathf.Abs(b) > Mathf.Abs(a) ? b : a;

        // Fix 2.4: a response curve (tuning stickCurve): small moves stay fine, full stick is still full.
        static float Curve(float v) { float k = Mathf.Clamp01(Tuning.T.stickCurve); return v * (1 - k) + v * v * v * k; }
        // Fix 2.5: gamepad / Deck sticks get at least tuning gamepadDeadzone (Deck sticks drift); HOTAS keeps the Settings deadzone
        static float DZFor(InputAction a) => a.activeControl?.device is Gamepad ? Mathf.Max(DZ, Tuning.T.gamepadDeadzone) : DZ;
        // Fix 6.1: the pad / keyboard and the calibrated HOTAS stick: whichever is pushed further
        public static float Pitch => Pick(Curve(Dead(pitch.ReadValue<float>(), DZFor(pitch))), Curve(Dead(-HotasStick.y, HotasDZ))) * (GameSettings.S.invertPitch ? -1 : 1);
        public static float Roll => Pick(Curve(Dead(roll.ReadValue<float>(), DZFor(roll))), Curve(Dead(HotasStick.x, HotasDZ)));
        public static float Yaw => Curve(Dead(yaw.ReadValue<float>(), Mathf.Max(DZ, .12f)));   // twist grips are loose: a little more
        public static float ThrottleChange => throttleChange.ReadValue<float>();
        public static Vector2 Look => look.ReadValue<Vector2>();
        public static Vector2 MouseLook => mouseHold.IsPressed() ? mouseLook.ReadValue<Vector2>() : Vector2.zero;
        public static bool CameraPressed => camera.WasPressedThisFrame();
        public static bool MenuPressed => menu.WasPressedThisFrame();
        // Round 2: landing mode (hover), engines on / off, the airlock
        public static bool LandPressed => land.WasPressedThisFrame();
        public static bool EnginesPressed => engines.WasPressedThisFrame();
        public static bool AirlockPressed => airlock.WasPressedThisFrame();
        // Fix 2.3: call the mothership to you (held: GameShell.Campaign times the hold)
        public static bool RecallHeld => recall.IsPressed();
        // Fix 2.4: the gamepad's View button also opens the pause menu: a tap (held, it is the recall in the Campaign)
        public static bool ViewPressed => view.WasPressedThisFrame();
        public static bool ViewReleased => view.WasReleasedThisFrame();
        // Menus: D-pad, left stick, HOTAS hat (the Navigate action) and the calibrated HOTAS stick; never the twist or the lever.
        public static Vector2 Navigate { get { var n = navigate.ReadValue<Vector2>(); var h = HotasStick; return h.sqrMagnitude > n.sqrMagnitude ? h : n; } }
        public static bool SubmitPressed => submit.WasPressedThisFrame();
        // Fix 2.5: held during the automatic parts (launch, docking, tow, recall, the beam): they play live, 4x faster
        public static bool FastForwardHeld => submit.IsPressed();
        public static bool CancelPressed => cancel.WasPressedThisFrame();

        // The throttle lever as 0..1, or -1 when no lever is bound. Its range is learnt: a lever that ever reads below zero is -1..1,
        // otherwise 0..1. Invert throttle flips it (most HID levers read lowest pushed forward).
        static int throttleRange;   // 0 unknown (0..1 assumed), 1 = -1..1
        public static float ThrottleLever
        {
            get
            {
                if (!HasLever) return -1;
                float v = throttleAxis.ReadValue<float>();
                if (v < -.02f) throttleRange = 1;
                float t = throttleRange == 1 ? (v + 1) / 2 : v;
                if (GameSettings.S.invertThrottle) t = 1 - t;
                return Mathf.Clamp01(t);
            }
        }
        public static bool HasLever => throttleAxis.controls.Count > 0;

        // The HOTAS buttons held now, by symbol (the setup page; "identify your buttons" reads the first one pressed).
        public static List<int> HotasButtonsHeld()
        {
            var list = new List<int>();
            var j = HotasPresent ? hotasStick.controls[0].device : Joystick.current;
            if (j == null) return list;
            foreach (var c in j.allControls)
                if (c is ButtonControl b && c.parent == j && b.isPressed)
                    list.Add(c.name == "trigger" ? 1 : int.TryParse(c.name.Replace("button", ""), out int n) ? n : 0);
            return list;
        }

        // HOTAS setup: the stick raw and as used, twist, lever, hat, buttons held. No keyboard text.
        public static string HotasReport()
        {
            var sb = new StringBuilder();
            if (!HotasPresent) return "No HOTAS found. Plug it in: it shows here at once.";
            var r = HotasRaw; var s = HotasStick;
            sb.AppendLine($"<b>Stick</b>  ({HotasCalState})");
            sb.AppendLine($"  left / right  {Bar(s.x)}   raw {r.x:+0.00;-0.00; 0.00}");
            sb.AppendLine($"  fwd / back    {Bar(s.y)}   raw {r.y:+0.00;-0.00; 0.00}");
            sb.AppendLine($"<b>Twist</b>  {Bar(Yaw)}");
            sb.AppendLine($"<b>Throttle lever</b>  {(HasLever ? Bar(ThrottleLever * 2 - 1) + $"  {ThrottleLever * 100:0}%  ({ThrottleAxisPath.Replace("<Joystick>/", "")})" : "none")}");
            var h = look.ReadValue<Vector2>();
            sb.AppendLine($"<b>Hat</b>  {(h.y > .5f ? "up" : h.y < -.5f ? "down" : h.x > .5f ? "right" : h.x < -.5f ? "left" : "centre")}");
            var held = HotasButtonsHeld();
            sb.Append("<b>Held</b>  " + (held.Count > 0 ? string.Join("  ", held.Select(HotasName)) : "none"));
            return sb.ToString();
        }

        static string Bar(float v)
        {
            int n = Mathf.RoundToInt(Mathf.Clamp(v, -1, 1) * 10);
            var s = new char[21]; for (int i = 0; i < 21; i++) s[i] = '·';
            s[10] = '|';
            for (int i = 1; i <= Mathf.Abs(n); i++) s[10 + i * (n > 0 ? 1 : -1)] = '█';
            return new string(s) + $" {v:+0.00;-0.00; 0.00}";
        }
    }
}
