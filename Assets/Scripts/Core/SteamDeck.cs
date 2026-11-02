using UnityEngine;

namespace HostileHorizon
{
    // Round 1, item 8: Chastain Rush's SteamDeck.cs (its Round 81), ported. A Steam Deck graphics setting (Settings: Steam Deck
    // Auto / On / Off; settings.json "steamDeck" 0 / 1 / 2). Auto is on at the Deck's own 1280 x 800 screen or on its GPU (AMD Custom
    // GPU 0405, "Van Gogh"; "Sephiroth" for the OLED). On: the view distance and shadow distance to 60 %, anti-aliasing off, one shadow
    // cascade, only the nearest terrain casts shadows (WorldLook.Current). Chastain Rush's traffic and people scales have no use here yet.
    // A check run has it off unless it passes -steamDeck 1.
    public static class SteamDeck
    {
        public const float ViewScale = .6f;
        static int forced = -1;
        static bool? detected;
        public static bool Detected
        {
            get
            {
                if (detected == null)
                {
                    string gpu = SystemInfo.graphicsDeviceName ?? "";
                    detected = (Display.main.systemWidth == 1280 && Display.main.systemHeight == 800) || gpu.Contains("Custom GPU 0405") || gpu.Contains("Van Gogh") || gpu.Contains("Sephiroth");
                }
                return detected.Value;
            }
        }
        public static bool On
        {
            get
            {
                if (forced < 0) { forced = 3; var a = System.Environment.GetCommandLineArgs(); int i = System.Array.IndexOf(a, "-steamDeck"); if (i >= 0 && i + 1 < a.Length && int.TryParse(a[i + 1], out int v)) forced = v; }
                if (forced == 1) return true;
                if (forced == 0 || CheckRun.Active) return false;
                int m = GameSettings.S.steamDeck;
                return m == 1 || m == 0 && Detected;
            }
        }
        public static string Label => GameSettings.S.steamDeck == 1 ? "On" : GameSettings.S.steamDeck == 2 ? "Off" : $"Auto ({(Detected ? "on" : "off")})";
    }
}
