using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace HostileHorizon
{
    // Round 1, item 7: the player's settings, saved as settings.json in the save folder (persistentDataPath, or the launcher's
    // isolated test folder: -racerTestSave <dir>).
    [Serializable]
    public sealed class GameSettings
    {
        public int version = 2;               // Fix 6.1: 2 = the HOTAS layout on the 12 buttons a T.Flight HOTAS 4 has
        public bool invertPitch;
        public int deadzone = 8;              // percent, sticks and twist
        public int throttleAxis;              // 0 Auto, else ThrottleAxes[i]
        public bool invertThrottle = true;    // most HID throttles read -1 pushed forward
        public int cameraDefault;             // CameraRig.Mode
        public int quality = 2;               // 0 Low, 1 Medium, 2 High
        public int steamDeck;                 // 0 Auto, 1 On, 2 Off (as Chastain Rush)
        public int volume = 80;               // percent
        public int crashLevel = 1;            // CrashLevel (Bounce)
        public int seedMode;                  // 0 seed of the day, 1 random, 2 typed in
        public int customSeed = 1984;
        public int frameLimit;                // 0 = vsync (check runs set it off)
        public int campaignCrashLevel = 3;    // Round 2, item 14 (unused since Round 5: the crash level is part of the difficulty, DifficultySetting)
        public int flightAssist = 1;          // Fix 2.4: 0 Full (0.2.3's), 1 Light, 2 Off
        public int sensitivity = 100;         // Fix 2.4: stick sensitivity, percent of the turn rates (50-150)
        public bool routeTrail = true;        // Fix 2.6: the route trail to the next pilot / the mothership
        public int musicVolume = 70;          // Round 4 (B4): percent of the master volume
        public int effectsVolume = 100;       // Round 4 (B4)
        public int voiceVolume = 100;         // Round 4 (B4)
        public bool subtitles = true;         // Round 4 (C4)
        public bool autoLevel = true;         // Round 5 (A3): the ship rights itself with the stick released (Full / Light)
        public int rightingSpeed = 1;         // Round 5 (A3): 0 Slow, 1 Normal, 2 Fast
        // Fix 6.1: the HOTAS. Button per assignable action (HID numbers, Controls.HotasActs), which HID button carries each printed
        // symbol (Controls.HotasSymbols; "identify your buttons" sets it), the stick's calibration (raw x min / centre / max, y min /
        // centre / max as the Input System reads them; hotasCalibrated once Dan ran "Calibrate stick", else learnt as it goes).
        public int[] hotasButtons;
        public int[] hotasSymbolButtons;
        public float[] hotasCal;
        public bool hotasCalibrated;
        public DifficultySetting difficulty = new DifficultySetting();   // Round 5 (B8): what a new career starts on
        public int freeBiome;                 // Round 4 (A1): Free Flight's planet type, 0 = by the seed, else Biome + 1

        public static readonly string[] AssistNames = { "Full", "Light", "Off" };
        public static readonly string[] RightingNames = { "Slow", "Normal", "Fast" };

        public static readonly string[] ThrottleAxes = { "Auto", "slider", "z", "rz", "rx", "ry", "dial" };

        public static GameSettings S { get; private set; } = new GameSettings();

        public static string Folder
        {
            get
            {
                var test = LauncherBridge.Argument("-racerTestSave");
                return !string.IsNullOrEmpty(test) && Path.IsPathRooted(test) ? test : Application.persistentDataPath;
            }
        }
        static string FilePath => Path.Combine(Folder, "settings.json");

        public static void Load()
        {
            if (CheckRun.Active) { S = new GameSettings(); Fix(); return; }   // a check always plays on the defaults
            try { if (File.Exists(FilePath)) S = JsonUtility.FromJson<GameSettings>(File.ReadAllText(FilePath)) ?? new GameSettings(); }
            catch (Exception e) { Debug.LogWarning("Settings unreadable, using defaults: " + e.Message); S = new GameSettings(); }
            Fix();
        }

        // Fix 6.1: settings from before 0.6.1 bound HOTAS buttons 9 / 10 / 14 / 16 (a T.Flight HOTAS 4 has 12): the new layout replaces them.
        static void Fix()
        {
            if (S.version < 2) { S.hotasButtons = null; S.version = 2; }
            if (S.hotasButtons == null || S.hotasButtons.Length != Controls.HotasActs.Length) S.hotasButtons = Controls.HotasActs.Select(a => a.button).ToArray();
            if (S.hotasSymbolButtons == null || S.hotasSymbolButtons.Length != Controls.HotasSymbols.Length) S.hotasSymbolButtons = (int[])Controls.DefaultSymbolButtons.Clone();
            if (S.hotasCal == null || S.hotasCal.Length != 6) { S.hotasCal = new float[] { -1, 0, 1, -1, 0, 1 }; S.hotasCalibrated = false; }
        }

        public static void Save()
        {
            if (CheckRun.Active) return;   // a check never changes Dan's settings
            try { Directory.CreateDirectory(Folder); File.WriteAllText(FilePath + ".tmp", JsonUtility.ToJson(S, true)); File.Copy(FilePath + ".tmp", FilePath, true); File.Delete(FilePath + ".tmp"); }
            catch (Exception e) { Debug.LogWarning("Settings not saved: " + e.Message); }
        }
    }
}
