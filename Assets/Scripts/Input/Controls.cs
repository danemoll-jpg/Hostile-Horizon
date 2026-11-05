using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace HostileHorizon
{
    // Round 1, item 5: all input goes through one Input Actions asset (Resources/Input/Controls.inputactions) with a binding set per
    // device: HOTAS (T.Flight HOTAS 4 as a HID joystick), Gamepad (Xbox layout, Steam Deck), Keyboard + mouse. Nothing here reads a
    // device directly for play (VR rule 6); the axis display (Settings) lists the joystick's raw axes only so Dan can see them move.
    // Deadzone and invert pitch come from Settings; the HOTAS throttle lever's axis can be picked there (Auto: slider if the joystick
    // has one, else z) and inverted.
    public static class Controls
    {
        public static InputActionAsset Asset { get; private set; }
        static InputAction pitch, roll, yaw, throttleAxis, throttleChange, look, mouseLook, mouseHold, camera, menu, land, engines, airlock, recall, view, bellyCam, routeTarget, cabinCam, levelOut, fire, shield, navigate, submit, cancel;
        public static string ThrottleAxisPath { get; private set; } = "";

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
            // Fix 2.5: the device last used, so prompts name the right button
            f.actionTriggered += c => Note(c.control?.device); u.actionTriggered += c => Note(c.control?.device);
            navigate = u["Navigate"]; submit = u["Submit"]; cancel = u["Cancel"];
            ApplySettings();
            Asset.Enable();
            InputSystem.onDeviceChange += (d, c) => { if (d is Joystick && (c == InputDeviceChange.Added || c == InputDeviceChange.Removed)) ApplySettings(); };
        }

        // Re-apply the throttle-axis choice (also when a joystick is plugged in).
        public static void ApplySettings()
        {
            int i = GameSettings.S.throttleAxis;
            string axis = i > 0 && i < GameSettings.ThrottleAxes.Length ? GameSettings.ThrottleAxes[i] : null;
            if (axis == null) axis = Joystick.current != null && Joystick.current.TryGetChildControl("slider") != null ? "slider" : "z";
            ThrottleAxisPath = "<Joystick>/" + axis;
            throttleAxis.ApplyBindingOverride(0, ThrottleAxisPath);
            throttleRange = 0;
            // Fix 2.4: the HOTAS pause button Dan picks (Settings > Controls); button 10 always pauses too
            int pb = menu.bindings.IndexOf(b => b.path == "<Joystick>/button9");
            if (pb >= 0) menu.ApplyBindingOverride(pb, "<Joystick>/button" + Mathf.Clamp(GameSettings.S.hotasPauseButton, 1, 16));
            // Round 5 (A2): the HOTAS level-out button Dan picks
            int lb = levelOut.bindings.IndexOf(b => b.path == "<Joystick>/button14");
            if (lb >= 0) levelOut.ApplyBindingOverride(lb, "<Joystick>/button" + Mathf.Clamp(GameSettings.S.hotasLevelButton, 1, 16));
        }

        public enum Device { Gamepad, Keyboard, Hotas }
        public static Device LastDevice { get; private set; } = Device.Gamepad;
        static void Note(InputDevice d)
        {
            if (d is Gamepad) LastDevice = Device.Gamepad;
            else if (d is Joystick) LastDevice = Device.Hotas;
            else if (d is Keyboard || d is Mouse) LastDevice = Device.Keyboard;
        }
        // The button's name for the device in use: "B" on a pad / the Deck, "O" on the keyboard, "HOTAS 6".
        public static string Key(string pad, string key, string hotas) => LastDevice == Device.Keyboard ? key : LastDevice == Device.Hotas ? hotas : pad;
        public static string AirlockKey => Key("B", "O", "HOTAS 6");
        public static string EngineKey => Key("A", "K", "HOTAS 5");
        public static string LandKey => Key("X", "G", "HOTAS 4");
        public static string BellyKey => Key("D-pad down", "V", "HOTAS 8");
        public static bool BellyCamPressed => bellyCam.WasPressedThisFrame();
        // Fix 2.6: cycle the route trail's target (next pilot / mothership). Round 3: the cabin camera on the HUD.
        public static bool RouteTargetPressed => routeTarget.WasPressedThisFrame();
        public static string RouteTargetKey => Key("D-pad right", "T", "HOTAS 11");
        public static bool CabinCamPressed => cabinCam.WasPressedThisFrame();
        // Round 5 (A2): level out at once (left-stick click / L / the HOTAS button picked in Settings > Controls and HOTAS)
        public static bool LevelOutPressed => levelOut.WasPressedThisFrame();
        // Round 5 (B3, B4): the gun (held: A / Space / left mouse / HOTAS trigger) and the shield toggle (D-pad left / Z / HOTAS 2)
        public static bool FireHeld => fire.IsPressed();
        public static bool ShieldPressed => shield.WasPressedThisFrame();
        public static string FireKey => Key("A", "Space", "trigger");
        public static string ShieldKey => Key("D-pad left", "Z", "HOTAS 2");
        public static string LevelOutKey => Key("L-stick click", "L", "HOTAS " + GameSettings.S.hotasLevelButton);
        public static string CabinCamKey => Key("D-pad up", "N", "HOTAS 12");

        static float Dead(float v, float dz) { float a = Mathf.Abs(v); return a <= dz ? 0 : Mathf.Sign(v) * (a - dz) / (1 - dz); }
        static float DZ => GameSettings.S.deadzone / 100f;

        // Fix 2.4: a response curve (tuning stickCurve): small moves stay fine, full stick is still full.
        static float Curve(float v) { float k = Mathf.Clamp01(Tuning.T.stickCurve); return v * (1 - k) + v * v * v * k; }
        // Fix 2.5: gamepad / Deck sticks get at least tuning gamepadDeadzone (Deck sticks drift); HOTAS keeps the Settings deadzone
        static float DZFor(InputAction a) => a.activeControl?.device is Gamepad ? Mathf.Max(DZ, Tuning.T.gamepadDeadzone) : DZ;
        public static float Pitch => Curve(Dead(pitch.ReadValue<float>(), DZFor(pitch))) * (GameSettings.S.invertPitch ? -1 : 1);
        public static float Roll => Curve(Dead(roll.ReadValue<float>(), DZFor(roll)));
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
        public const string RecallKeys = "View / H / HOTAS 7";
        // Fix 2.4: the gamepad's View button (the Deck's ⧉) also opens the pause menu: a tap (held, it is the recall in the Campaign)
        public static bool ViewPressed => view.WasPressedThisFrame();
        public static bool ViewReleased => view.WasReleasedThisFrame();
        public const string LandKeys = "X / G / HOTAS 4", EngineKeys = "A / K / HOTAS 5";
        public static Vector2 Navigate => navigate.ReadValue<Vector2>();
        public static bool SubmitPressed => submit.WasPressedThisFrame();
        // Fix 2.5: held during the automatic parts (launch, docking, tow, recall, the beam): they play live, 4x faster
        public static bool FastForwardHeld => submit.IsPressed();
        public static string FastForwardKey => Key("A", "Space", "HOTAS trigger");
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

        // Settings > Controls > Axis display: every joystick axis and pressed button, then the actions as the game reads them.
        public static string AxisReport()
        {
            var sb = new StringBuilder();
            bool any = false;
            foreach (var d in InputSystem.devices)
            {
                if (!(d is Joystick)) continue;
                any = true; sb.AppendLine($"<b>{d.displayName}</b>");
                var pressed = new List<string>();
                foreach (var c in d.allControls)
                {
                    if (c is ButtonControl b) { if (c.parent == d && b.isPressed) pressed.Add(c.name); continue; }
                    if (c is AxisControl a && (c.parent == d || c.parent is StickControl))
                        sb.AppendLine($"  {c.path.Substring(d.path.Length + 1),-10} {Bar(a.ReadValue())}");
                }
                if (d.TryGetChildControl("hat") is DpadControl hat) { var h = hat.ReadValue(); sb.AppendLine($"  hat        {h.x:+0;-0;0} {h.y:+0;-0;0}"); }
                sb.AppendLine("  buttons held: " + (pressed.Count > 0 ? string.Join(" ", pressed) : "none"));
            }
            if (!any) sb.AppendLine("No joystick / HOTAS found. Plug it in: it shows here at once.");
            foreach (var g in Gamepad.all) sb.AppendLine($"<b>{g.displayName}</b> (gamepad)");
            sb.AppendLine();
            sb.AppendLine("<b>What the ship reads</b>");
            sb.AppendLine($"  pitch      {Bar(Pitch)}");
            sb.AppendLine($"  roll       {Bar(Roll)}");
            sb.AppendLine($"  yaw        {Bar(Yaw)}");
            sb.AppendLine($"  throttle   {(HasLever && ThrottleLever >= 0 ? Bar(ThrottleLever * 2 - 1) + $"  lever {ThrottleLever * 100:0}% ({ThrottleAxisPath})" : "no lever")}");
            sb.AppendLine($"  throttle +/- {Bar(ThrottleChange)}");
            var l = Look; sb.AppendLine($"  look       {l.x:+0.00;-0.00} {l.y:+0.00;-0.00}");
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
