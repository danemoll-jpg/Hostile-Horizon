using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace HostileHorizon
{
    // Fix 2.1 (Dan on the Steam Deck, 0.2.0: "the screen is not fullscreen"): Chastain Rush's ScreenSetting.cs, ported. At start-up the
    // game goes full screen (a borderless full-screen window) at the display's own resolution (Display.main.systemWidth / Height), or
    // windowed if the player chose that in Settings > Display. Applied before the first scene, so it wins over the window mode and size
    // Unity remembers from earlier runs. Saved in display.json in the save folder (beside settings.json).
    // Settings > Display: Screen (Full screen / Windowed) and Resolution (the display's own first, then its sizes below it). Full screen
    // draws at that size and fills the screen; a window is that size (with the display's own, three quarters of it).
    // Test runs (-mute, -exeCheck, -racerTestSave) never apply or save it: their -screen-* arguments stand and they stay unfocused.
    public static class ScreenSetting
    {
        [Serializable] sealed class Saved { public bool fullscreen = true; public int width, height; }
        public static bool Fullscreen { get; private set; } = true;
        static int width, height;   // 0: the display's own
        static bool TestRunning => CheckRun.Active || TestRun.Muted;

        static string FilePath()
        {
#if UNITY_EDITOR
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "local", "display.json"));   // git-ignored
#else
            return Path.Combine(GameSettings.Folder, "display.json");
#endif
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init()
        {
            Fullscreen = true; width = height = 0;
            if (CheckRun.Active && CheckRun.Only == "fullscreen") { Apply(); return; }   // the visible full-screen check (Dan's OK): defaults, nothing read or saved
            if (TestRunning) return;
            try { if (File.Exists(FilePath())) { var s = new Saved(); JsonUtility.FromJsonOverwrite(File.ReadAllText(FilePath()), s); Fullscreen = s.fullscreen; width = s.width; height = s.height; } }
            catch (Exception e) { Debug.LogWarning("[Screen] display.json unreadable: " + e.Message); }
            Apply();
        }

        // What the game asks for on a display of desktopW x desktopH (also used by the exe check to run this path for a simulated Deck).
        public static (int w, int h, FullScreenMode mode) Target(int desktopW, int desktopH, bool fullscreen, int chosenW, int chosenH)
        {
            bool own = chosenW <= 0 || chosenH <= 0;
            if (fullscreen) return (own ? desktopW : chosenW, own ? desktopH : chosenH, FullScreenMode.FullScreenWindow);
            return (own ? Mathf.RoundToInt(desktopW * .75f) : chosenW, own ? Mathf.RoundToInt(desktopH * .75f) : chosenH, FullScreenMode.Windowed);
        }

        // the sizes offered: the display's own first, then its sizes below it, largest first
        static List<Vector2Int> Sizes()
        {
            int w = Display.main.systemWidth, h = Display.main.systemHeight;
            var list = new List<Vector2Int> { Vector2Int.zero };
            foreach (var r in Screen.resolutions)
            {
                var v = new Vector2Int(r.width, r.height);
                if (v.x >= 1280 && v.x < w && v.y <= h && !list.Contains(v)) list.Add(v);
            }
            list.Sort((a, b) => a == Vector2Int.zero ? -1 : b == Vector2Int.zero ? 1 : (b.x * b.y).CompareTo(a.x * a.y));
            return list;
        }
        public static string ScreenLabel => Fullscreen ? "Full screen" : "Windowed";
        public static string ResolutionLabel => width <= 0 ? $"Display's own ({Display.main.systemWidth} x {Display.main.systemHeight})" : $"{width} x {height}";
        public static void StepResolution(int d)
        {
            var sizes = Sizes(); int i = Mathf.Max(0, sizes.IndexOf(new Vector2Int(Mathf.Max(0, width), Mathf.Max(0, height))));
            var next = sizes[(i + d + sizes.Count) % sizes.Count]; width = next.x; height = next.y;
            Set(Fullscreen);
        }

        public static void Set(bool fullscreen)
        {
            Fullscreen = fullscreen;
            if (TestRunning) return;   // a check changes nothing on Dan's screen and saves nothing
            try { Directory.CreateDirectory(Path.GetDirectoryName(FilePath())); File.WriteAllText(FilePath(), JsonUtility.ToJson(new Saved { fullscreen = fullscreen, width = width, height = height }, true)); }
            catch (Exception e) { Debug.LogWarning("[Screen] could not save display.json: " + e.Message); }
            Apply();
        }

        static void Apply()
        {
            if (Application.isEditor) return;
            int w = Display.main.systemWidth, h = Display.main.systemHeight;   // the display's own resolution
            var t = Target(w, h, Fullscreen, width, height);
            Screen.SetResolution(t.w, t.h, t.mode);
            Debug.Log($"[Screen] {ScreenLabel}, {ResolutionLabel}: asked for {t.w} x {t.h} {t.mode}; display {w} x {h}");
        }
    }
}
