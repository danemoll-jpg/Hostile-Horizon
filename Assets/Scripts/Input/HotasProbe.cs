using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HostileHorizon
{
    // Round 11 (A1-A3): how the HOTAS reaches the game, above all on the Steam Deck under Proton. Read only (it never opens a device).
    // What Proton does with a joystick (Wine's winebus.sys, read 2026-11-14 from gitlab.winehq.org wine master and Valve's wine
    // proton_10.0 / experimental_10.0, dlls/winebus.sys/main.c + bus_sdl.c):
    //  * hidraw (PROTON_ENABLE_HIDRAW=0x044F/0xB67C, or a device on Wine's own list; 044F:B67C is NOT on it): the HOTAS' own HID
    //    descriptor, so the game sees exactly what Windows gives it (manufacturer "Thrustmaster", y x dial slider ry rx rz z hat 12 buttons).
    //  * SDL (the default): a HID joystick Wine builds itself: manufacturer "SDL", the SDL axes in order as X Y Z RX RY RZ SLIDER DIAL
    //    (Linux puts the HOTAS' x y z rx ry rz slider dial on ABS_X..ABS_RZ, ABS_THROTTLE, ABS_RUDDER in that order: the same names),
    //    a hat, the buttons 1-12 in order, VID / PID kept.
    //  * SDL "game controller" (SDL knows a gamepad mapping for it, e.g. from Steam's SDL config, and does not call it a flight stick):
    //    Wine makes it an XInput pad: 6 axes, Xbox buttons. Unity then sees a gamepad, the device path keeps VID_044F with "&IG_".
    //  * Steam Input on for the game: Steam hides the real HOTAS and hands the game its virtual pad (VID_28DE, XInput). "The game
    //    recognised it as a controller but not a HOTAS" (Dan, 2026-11-14) is this or the one above.
    public static class HotasProbe
    {
        public const int Vid = 0x044F, Pid = 0xB67C, SteamVid = 0x28DE;
        public enum Form { Native, ProtonHidraw, ProtonSdl, ProtonGamepad, SteamInput, NotFound }

        // ---- Wine / Proton
        [DllImport("kernel32", CharSet = CharSet.Unicode)] static extern IntPtr GetModuleHandleW(string name);
        [DllImport("kernel32", CharSet = CharSet.Ansi)] static extern IntPtr GetProcAddress(IntPtr mod, string name);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate IntPtr WineVersionFn();
        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode)] delegate IntPtr UnixNameFn(string dos);
        static bool? wine; static string wineVersion = "";
        public static bool TestWine;   // checks: as if under Proton
        public static bool UnderWine
        {
            get
            {
                if (TestWine) return true;
                if (wine == null)
                {
                    wine = false;
                    try
                    {
                        if (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor)
                        {
                            var p = GetProcAddress(GetModuleHandleW("ntdll.dll"), "wine_get_version");
                            if (p != IntPtr.Zero) { wine = true; wineVersion = Marshal.PtrToStringAnsi(Marshal.GetDelegateForFunctionPointer<WineVersionFn>(p)()) ?? ""; }
                        }
                    }
                    catch { }
                }
                return wine.Value;
            }
        }
        public static string WineVersion { get { var _ = UnderWine; return wineVersion; } }

        // A Windows path as Linux sees it (Wine's kernel32 wine_get_unix_file_name); null outside Wine
        public static string UnixPath(string windowsPath)
        {
            if (TestWine && wine != true) return TestUnixPath(windowsPath);
            if (!UnderWine) return null;
            try
            {
                var p = GetProcAddress(GetModuleHandleW("kernel32.dll"), "wine_get_unix_file_name");
                if (p != IntPtr.Zero) { var s = Marshal.GetDelegateForFunctionPointer<UnixNameFn>(p)(windowsPath); if (s != IntPtr.Zero) return Marshal.PtrToStringAnsi(s); }
            }
            catch { }
            return TestUnixPath(windowsPath);
        }
        // Without Wine's call: Proton's prefix from STEAM_COMPAT_DATA_PATH (Proton sets it for the game) + drive_c
        static string TestUnixPath(string w)
        {
            var c = Environment.GetEnvironmentVariable("STEAM_COMPAT_DATA_PATH");
            if (string.IsNullOrEmpty(c) || w.Length < 3 || char.ToUpperInvariant(w[0]) != 'C') return null;
            var rest = w.Substring(3).Replace('\\', '/');
            rest = System.Text.RegularExpressions.Regex.Replace(rest, "^[Uu]sers/[^/]+/", "users/steamuser/");
            return c.TrimEnd('/') + "/pfx/drive_c/" + rest;
        }

        // ---- the HID device paths Windows (or Wine) lists, read only: SetupDi over the HID interface class
        [StructLayout(LayoutKind.Sequential)] struct SpDeviceInterfaceData { public int cbSize; public Guid guid; public int flags; public IntPtr reserved; }
        [DllImport("hid.dll")] static extern void HidD_GetHidGuid(out Guid g);
        [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)] static extern IntPtr SetupDiGetClassDevsW(ref Guid g, IntPtr enumerator, IntPtr parent, int flags);
        [DllImport("setupapi.dll", SetLastError = true)] static extern bool SetupDiEnumDeviceInterfaces(IntPtr set, IntPtr info, ref Guid g, int index, ref SpDeviceInterfaceData data);
        [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)] static extern bool SetupDiGetDeviceInterfaceDetailW(IntPtr set, ref SpDeviceInterfaceData data, IntPtr detail, int size, out int needed, IntPtr info);
        [DllImport("setupapi.dll")] static extern bool SetupDiDestroyDeviceInfoList(IntPtr set);
        public static List<string> TestPaths;   // checks: the paths a Proton-like system would list
        public static List<string> HidPaths()
        {
            if (TestPaths != null) return TestPaths;
            var list = new List<string>();
            if (!(Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor)) return list;
            try
            {
                HidD_GetHidGuid(out var g);
                var set = SetupDiGetClassDevsW(ref g, IntPtr.Zero, IntPtr.Zero, 0x02 | 0x10);   // DIGCF_PRESENT | DIGCF_DEVICEINTERFACE
                if (set == IntPtr.Zero || set == new IntPtr(-1)) return list;
                try
                {
                    var d = new SpDeviceInterfaceData { cbSize = Marshal.SizeOf<SpDeviceInterfaceData>() };
                    for (int i = 0; i < 256 && SetupDiEnumDeviceInterfaces(set, IntPtr.Zero, ref g, i, ref d); i++)
                    {
                        SetupDiGetDeviceInterfaceDetailW(set, ref d, IntPtr.Zero, 0, out int need, IntPtr.Zero);
                        if (need <= 0) continue;
                        var buf = Marshal.AllocHGlobal(need);
                        try
                        {
                            Marshal.WriteInt32(buf, IntPtr.Size == 8 ? 8 : 6);   // SP_DEVICE_INTERFACE_DETAIL_DATA_W.cbSize
                            if (SetupDiGetDeviceInterfaceDetailW(set, ref d, buf, need, out _, IntPtr.Zero)) list.Add(Marshal.PtrToStringUni(buf + 4));
                        }
                        finally { Marshal.FreeHGlobal(buf); }
                    }
                }
                finally { SetupDiDestroyDeviceInfoList(set); }
            }
            catch (Exception e) { list.Add("(not listed: " + e.Message + ")"); }
            return list;
        }
        static bool PathHas(string p, int vid, int pid = -1) { var u = p.ToUpperInvariant(); return u.Contains($"VID_{vid:X4}") && (pid < 0 || u.Contains($"PID_{pid:X4}")); }

        // ---- a Unity joystick's VID / PID (from Unity's HID descriptor JSON) and whether Wine's SDL bus made it
        public static (int vid, int pid) Ids(InputDevice d)
        {
            var c = d?.description.capabilities ?? "";
            int Num(string key) { var m = System.Text.RegularExpressions.Regex.Match(c, "\"" + key + "\"\\s*:\\s*(\\d+)"); return m.Success ? int.Parse(m.Groups[1].Value) : 0; }
            return (Num("vendorId"), Num("productId"));
        }
        public static bool IsOurHotas(InputDevice d)
        {
            if (!(d is Joystick)) return false;
            var (v, p) = Ids(d); var n = (d.description.product ?? "").ToLowerInvariant();
            return v == Vid && p == Pid || n.Contains("t.flight") || n.Contains("hotas");
        }
        public static bool FromSdl(InputDevice d) => d != null && (d.description.manufacturer ?? "") == "SDL";

        // ---- the diagnosis (HOTAS setup, the recorder, the [Play] log)
        static float diagAt = -10; static Form diagForm; static string diagDetail = "";
        // cached for 2 s (the device list is read through SetupDi)
        public static Form Diagnose(out string detail)
        {
            if (Time.unscaledTime - diagAt < 2f && TestPaths == null) { detail = diagDetail; return diagForm; }
            diagAt = Time.unscaledTime; diagForm = DiagnoseNow(out diagDetail); detail = diagDetail; return diagForm;
        }
        public static Form DiagnoseNow(out string detail)
        {
            var joy = Joystick.all.FirstOrDefault(IsOurHotas) ?? (Controls.HotasPresent ? Controls.HotasDevice : null);
            var paths = UnderWine ? HidPaths() : new List<string>();
            int pads = Gamepad.all.Count;
            if (joy != null)
            {
                var (v, p) = Ids(joy);
                detail = $"\"{joy.description.product}\" ({joy.description.manufacturer}, {v:X4}:{p:X4}) as a HID joystick";
                return !UnderWine ? Form.Native : FromSdl(joy) ? Form.ProtonSdl : Form.ProtonHidraw;
            }
            if (paths.Any(x => PathHas(x, Vid) && x.ToUpperInvariant().Contains("IG_")))
            { detail = $"the HOTAS (044F) is there, but Proton turned it into an Xbox-style pad ({pads} gamepad(s) seen)"; return Form.ProtonGamepad; }
            if (UnderWine && paths.Any(x => PathHas(x, SteamVid)))
            { detail = $"only Steam's virtual controller(s) reach the game ({paths.Count(x => PathHas(x, SteamVid))} device(s) VID 28DE; {pads} gamepad(s) seen): Steam Input has the HOTAS"; return Form.SteamInput; }
            detail = UnderWine ? $"no HOTAS among {paths.Count} HID device(s) ({pads} gamepad(s) seen)" : "no HOTAS plugged in";
            return Form.NotFound;
        }
        public static string FormName(Form f) => f == Form.Native ? "a HID joystick (Windows)" : f == Form.ProtonHidraw ? "a HID joystick, raw (Proton hidraw): as on Windows"
            : f == Form.ProtonSdl ? "a HID joystick made by Proton's SDL bus (axes and buttons as on Windows)" : f == Form.ProtonGamepad ? "an Xbox-style pad (Proton's SDL controller mapping): only part of it works"
            : f == Form.SteamInput ? "Steam Input's virtual controller: the HOTAS itself is hidden from the game" : "not found";
        public static bool OnDeckOrProton => UnderWine || SteamDeck.Detected;

        public const string LaunchOption = "PROTON_ENABLE_HIDRAW=0x044F/0xB67C %command%";
        // A2: the steps, shown on HOTAS setup when the HOTAS doesn't arrive as a joystick under Proton (and in README-LAUNCHER.md)
        public static string Steps(Form f)
        {
            string s = "<size=20>In Steam, on the Hostile Horizon launcher's entry:\n" +
                "1. <b>Properties → Controller</b>: <b>Disable Steam Input</b>\n" +
                $"2. <b>Properties → General → Launch options</b>:\n    <b>{LaunchOption}</b>\n" +
                "3. The HOTAS base switch on <b>PC</b>; plug it in before starting the game\n" +
                "4. Quit the game fully, start it again, come back here</size>";
            if (f == Form.SteamInput) return "<color=#FFD233>Steam Input hands the game a virtual controller, not the HOTAS.</color>\n" + s;
            if (f == Form.ProtonGamepad) return "<color=#FFD233>Proton turned the HOTAS into an Xbox-style pad (only part works).</color>\n" + s;
            return "<color=#FFD233>No HOTAS reaches the game.</color>\n" + s;
        }
        public static string RecorderWhere(string windowsPath)
        {
            var u = UnixPath(windowsPath);
            return u == null ? windowsPath : u + "\n(in Desktop Mode: the Dolphin file manager; Windows path inside Proton: " + windowsPath + ")";
        }
    }
}
