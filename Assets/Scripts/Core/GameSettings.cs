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
        public int version = 4;               // Fix 6.1: 2 = the HOTAS layout on the 12 buttons a T.Flight HOTAS 4 has; Fix 6.2: 3 = Dan's button names; Fix 6.3: 4 = R1 is the trigger
        public bool invertPitch;
        public int deadzone = 8;              // percent, sticks and twist
        // Fix 6.3: no Throttle axis / Invert lever options any more (Dan: "I prefer it to not even be an option"); 0.6.2's
        // "throttleAxis" and "invertThrottle" keys are ignored when read and dropped when saved. The lever is found by itself (z, else
        // the axis moved end to end) or by HOTAS setup > Find throttle lever, which also learns its direction (it ends fully forward).
        public string throttleAxisFound = ""; // Fix 6.2: the lever Auto found (the axis that moved most; "Find throttle lever" or by itself)
        public bool leverForwardIsMin = true; // Fix 6.3: the lever reads its lowest value pushed forward (HID throttles, Dan's z)
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
        public int musicVolume = 85;          // Round 4 (B4): percent of the master volume (Fix 6.3: 70 -> 85, heard over the engine)
        public int effectsVolume = 100;       // Round 4 (B4)
        public int voiceVolume = 100;         // Round 4 (B4)
        public int engineVolume = 100;         // Fix 6.3: engine hum, wind, thrusters (their own slider; part of Effects before)
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
            // Fix 6.2: 0.6.1's default button names were guessed (trigger R2, button 2 R3, L1 10): Dan's own labels replace them unless
            // he identified his buttons himself
            if (S.version < 3) { if (S.hotasSymbolButtons != null && S.hotasSymbolButtons.SequenceEqual(new[] { 1, 3, 2, 10, 9, 4, 5, 6, 7, 8, 11, 12 })) S.hotasSymbolButtons = null; S.version = 3; }
            // Fix 6.3: Dan's trigger is printed R1 (button 1) and R3 is button 3 (0.6.2 had them the other way round): settings still on
            // 0.6.2's default names get the corrected ones; 0.6.2's music default (70) becomes the new one (heard over the engine)
            if (S.version < 4)
            {
                if (S.hotasSymbolButtons != null && S.hotasSymbolButtons.SequenceEqual(new[] { 10, 3, 1, 2, 9, 4, 5, 6, 7, 8, 11, 12 })) S.hotasSymbolButtons = null;
                if (S.musicVolume == 70) S.musicVolume = new GameSettings().musicVolume;
                S.version = 4;
            }
            if (S.throttleAxisFound == null) S.throttleAxisFound = "";
            if (S.hotasButtons == null || S.hotasButtons.Length != Controls.HotasActs.Length) S.hotasButtons = Controls.HotasActs.Select(a => a.button).ToArray();
            if (S.hotasSymbolButtons == null || S.hotasSymbolButtons.Length != Controls.HotasSymbols.Length) S.hotasSymbolButtons = (int[])Controls.DefaultSymbolButtons.Clone();
            if (S.hotasCal == null || S.hotasCal.Length != 6) { S.hotasCal = new float[] { -1, 0, 1, -1, 0, 1 }; S.hotasCalibrated = false; }
        }

        // Fix 6.3 (checks): what loading a saved settings file gives, with the migrations (S itself is left as it was)
        public static GameSettings LoadedFrom(string json)
        {
            var keep = S; S = JsonUtility.FromJson<GameSettings>(json) ?? new GameSettings(); Fix(); var r = S; S = keep; return r;
        }

        // ---- Fix 6.2: back to the defaults (saves, careers and the Field Journal live in career.json: never touched here)
        // HOTAS setup: calibration, the lever found and its direction, button names, button assignments, deadzone
        public static void ResetHotas()
        {
            var d = new GameSettings();
            S.hotasButtons = null; S.hotasSymbolButtons = null; S.hotasCal = null; S.hotasCalibrated = false;
            S.throttleAxisFound = ""; S.leverForwardIsMin = d.leverForwardIsMin; S.deadzone = d.deadzone;
            Fix();
        }
        // Settings (the flight page)
        public static void ResetFlight()
        {
            var d = new GameSettings();
            S.flightAssist = d.flightAssist; S.sensitivity = d.sensitivity; S.autoLevel = d.autoLevel; S.rightingSpeed = d.rightingSpeed;
            S.invertPitch = d.invertPitch; S.deadzone = d.deadzone; S.cameraDefault = d.cameraDefault; S.routeTrail = d.routeTrail;
        }
        public static void ResetSound() { var d = new GameSettings(); S.volume = d.volume; S.musicVolume = d.musicVolume; S.effectsVolume = d.effectsVolume; S.voiceVolume = d.voiceVolume; S.engineVolume = d.engineVolume; S.subtitles = d.subtitles; }
        public static void ResetDisplay() { var d = new GameSettings(); S.quality = d.quality; S.steamDeck = d.steamDeck; ScreenSetting.ResetToDefault(); }
        // Every Settings page (Free Flight's seed / planet type / crash level and the new-career difficulty are their own pages' choices)
        public static void ResetAll() { ResetFlight(); ResetHotas(); ResetSound(); ResetDisplay(); }

        public static void Save()
        {
            if (CheckRun.Active) return;   // a check never changes Dan's settings
            try { Directory.CreateDirectory(Folder); File.WriteAllText(FilePath + ".tmp", JsonUtility.ToJson(S, true)); File.Copy(FilePath + ".tmp", FilePath, true); File.Delete(FilePath + ".tmp"); }
            catch (Exception e) { Debug.LogWarning("Settings not saved: " + e.Message); }
        }
    }
}
