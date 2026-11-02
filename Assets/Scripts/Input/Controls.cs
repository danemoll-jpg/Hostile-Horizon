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
        static InputAction pitch, roll, yaw, throttleAxis, throttleChange, look, mouseLook, mouseHold, camera, menu, land, engines, airlock, navigate, submit, cancel;
        public static string ThrottleAxisPath { get; private set; } = "";

        public static void Init()
        {
            Asset = Resources.Load<InputActionAsset>("Input/Controls");
            var f = Asset.FindActionMap("Flight"); var u = Asset.FindActionMap("UI");
            pitch = f["Pitch"]; roll = f["Roll"]; yaw = f["Yaw"]; throttleAxis = f["ThrottleAxis"]; throttleChange = f["ThrottleChange"];
            look = f["Look"]; mouseLook = f["MouseLook"]; mouseHold = f["MouseLookHold"]; camera = f["Camera"]; menu = f["Menu"];
            land = f["Land"]; engines = f["Engines"]; airlock = f["Airlock"];
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
        }

        static float Dead(float v, float dz) { float a = Mathf.Abs(v); return a <= dz ? 0 : Mathf.Sign(v) * (a - dz) / (1 - dz); }
        static float DZ => GameSettings.S.deadzone / 100f;

        public static float Pitch => Dead(pitch.ReadValue<float>(), DZ) * (GameSettings.S.invertPitch ? -1 : 1);
        public static float Roll => Dead(roll.ReadValue<float>(), DZ);
        public static float Yaw => Dead(yaw.ReadValue<float>(), Mathf.Max(DZ, .12f));   // twist grips are loose: a little more
        public static float ThrottleChange => throttleChange.ReadValue<float>();
        public static Vector2 Look => look.ReadValue<Vector2>();
        public static Vector2 MouseLook => mouseHold.IsPressed() ? mouseLook.ReadValue<Vector2>() : Vector2.zero;
        public static bool CameraPressed => camera.WasPressedThisFrame();
        public static bool MenuPressed => menu.WasPressedThisFrame();
        // Round 2: landing mode (hover), engines on / off, the airlock
        public static bool LandPressed => land.WasPressedThisFrame();
        public static bool EnginesPressed => engines.WasPressedThisFrame();
        public static bool AirlockPressed => airlock.WasPressedThisFrame();
        public static Vector2 Navigate => navigate.ReadValue<Vector2>();
        public static bool SubmitPressed => submit.WasPressedThisFrame();
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
