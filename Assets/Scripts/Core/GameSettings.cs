using System;
using System.IO;
using UnityEngine;

namespace HostileHorizon
{
    // Round 1, item 7: the player's settings, saved as settings.json in the save folder (persistentDataPath, or the launcher's
    // isolated test folder: -racerTestSave <dir>).
    [Serializable]
    public sealed class GameSettings
    {
        public int version = 1;
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
            if (CheckRun.Active) { S = new GameSettings(); return; }   // a check always plays on the defaults
            try { if (File.Exists(FilePath)) S = JsonUtility.FromJson<GameSettings>(File.ReadAllText(FilePath)) ?? new GameSettings(); }
            catch (Exception e) { Debug.LogWarning("Settings unreadable, using defaults: " + e.Message); S = new GameSettings(); }
        }

        public static void Save()
        {
            if (CheckRun.Active) return;   // a check never changes Dan's settings
            try { Directory.CreateDirectory(Folder); File.WriteAllText(FilePath + ".tmp", JsonUtility.ToJson(S, true)); File.Copy(FilePath + ".tmp", FilePath, true); File.Delete(FilePath + ".tmp"); }
            catch (Exception e) { Debug.LogWarning("Settings not saved: " + e.Message); }
        }
    }
}
