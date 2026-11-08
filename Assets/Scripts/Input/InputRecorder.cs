using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace HostileHorizon
{
    // Fix 6.3: Record my input (20 s), for support (Settings > Controls and HOTAS setup). Writes every control of every connected device
    // as Unity's Input System sees it: the devices (layout, product, interface, the HID descriptor Unity read), then each change of each
    // control (time, device, path, value, the game actions it feeds), to hotas_input_log.txt beside settings.json (a check: in its folder).
    // Read only: it never changes a setting. While it runs the menu ignores input, so pressing buttons doesn't act on the page.
    public static class InputRecorder
    {
        public const float Seconds = 20f;
        public static bool Active { get; private set; }
        public static string LastNote { get; private set; } = "";
        public static string FilePath => Path.Combine(CheckRun.Active ? CheckRun.Dir : GameSettings.Folder, "hotas_input_log.txt");
        public static int Changes { get; private set; }
        public static string Status => Active ? $"Recording: {Mathf.CeilToInt(Seconds - (Time.unscaledTime - startedAt))} s left. Move and press everything." : LastNote;

        static float startedAt; static StringBuilder sb;
        static readonly Dictionary<InputControl, string> last = new Dictionary<InputControl, string>();
        static readonly Dictionary<InputControl, string> feeds = new Dictionary<InputControl, string>();
        static readonly Dictionary<InputControl, float> lastAt = new Dictionary<InputControl, float>();

        public static void Begin()
        {
            if (Active) return;
            Active = true; startedAt = Time.unscaledTime; Changes = 0; last.Clear(); feeds.Clear(); lastAt.Clear(); LastNote = "";
            sb = new StringBuilder();
            sb.AppendLine($"Hostile Horizon input log {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}, game {Application.version}, {Seconds:0} s");
            var s = GameSettings.S;
            sb.AppendLine($"settings: lever found \"{s.throttleAxisFound}\" (bound {Controls.ThrottleAxisPath}, forward = lowest value {s.leverForwardIsMin}); button names [{string.Join(",", s.hotasSymbolButtons ?? Controls.DefaultSymbolButtons)}] (R2 R1 R3 L1 L2 L3 □ ✕ ○ △ SHARE OPTIONS); jobs [{string.Join(",", s.hotasButtons ?? new int[0])}] ({string.Join(" ", Controls.HotasActs.Select(h => h.label))}); fire / select on button {Controls.FireButton}; calibration [{string.Join(",", (s.hotasCal ?? new float[0]).Select(v => v.ToString("0.00")))}] calibrated {s.hotasCalibrated}");
            sb.AppendLine("devices:");
            foreach (var d in InputSystem.devices)
            {
                var desc = d.description;
                sb.AppendLine($"  {d.name}  layout {d.layout}  product \"{desc.product}\"  manufacturer \"{desc.manufacturer}\"  interface {desc.interfaceName}  id {d.deviceId}");
                if (!string.IsNullOrEmpty(desc.capabilities) && desc.interfaceName == "HID") sb.AppendLine("    HID capabilities: " + desc.capabilities.Replace("\n", " ").Replace("  ", ""));
                foreach (var c in d.allControls) if (c.children.Count == 0 && !c.synthetic) sb.AppendLine($"    {c.path}  ({c.layout}{(c.noisy ? ", noisy" : "")})  now {Value(c)}");
            }
            // which game action each control feeds now (bindings and overrides applied)
            foreach (var a in Controls.Asset)
                foreach (var c in a.controls)
                    foreach (var leaf in Leaves(c)) feeds[leaf] = feeds.TryGetValue(leaf, out var f) ? f + ", " + a.actionMap.name + "/" + a.name : a.actionMap.name + "/" + a.name;
            foreach (var d in InputSystem.devices) foreach (var c in d.allControls) if (c.children.Count == 0) last[c] = Value(c);
            sb.AppendLine("changes (seconds from the start, device, control, value, game action):");
        }

        static IEnumerable<InputControl> Leaves(InputControl c) { if (c.children.Count == 0) { yield return c; yield break; } foreach (var ch in c.children) foreach (var l in Leaves(ch)) yield return l; }

        static string Value(InputControl c)
        {
            try
            {
                switch (c)
                {
                    case ButtonControl b: return b.isPressed ? "pressed" : "released";
                    case AxisControl a: return $"{a.ReadValue():0.000} (raw {a.ReadUnprocessedValue():0.000})";
                    default: var o = c.ReadValueAsObject(); return o == null ? "-" : o is float f ? f.ToString("0.000") : o.ToString();
                }
            }
            catch { return "?"; }
        }

        // every frame (GameShell.Update)
        public static void Tick()
        {
            if (!Active) return;
            float t = Time.unscaledTime - startedAt;
            foreach (var d in InputSystem.devices)
                foreach (var c in d.allControls)
                {
                    if (c.children.Count > 0) continue;
                    string v = Value(c);
                    if (last.TryGetValue(c, out var was) && was == v) continue;
                    // a busy axis (mouse, a shaking stick) at most 20 times a second; a button change always
                    if (!(c is ButtonControl) && lastAt.TryGetValue(c, out var at) && t - at < .05f) continue;
                    last[c] = v; lastAt[c] = t; Changes++;
                    if (Changes < 60000) sb.AppendLine($"{t,7:0.000}  {d.name}  {c.path}  {v}  -> {(feeds.TryGetValue(c, out var f) ? f : "(no game action)")}");
                }
            if (t >= Seconds) End();
        }

        static void End()
        {
            Active = false;
            sb.AppendLine($"end: {Changes} changes in {Seconds:0} s");
            try { Directory.CreateDirectory(Path.GetDirectoryName(FilePath)); File.WriteAllText(FilePath, sb.ToString()); LastNote = $"Saved: {FilePath} ({Changes} changes)"; }
            catch (System.Exception e) { LastNote = "Not saved: " + e.Message; }
            Debug.Log("[InputRecorder] " + LastNote);
        }
    }
}
