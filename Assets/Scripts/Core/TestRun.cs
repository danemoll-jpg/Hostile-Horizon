using System;
using System.IO;
using UnityEngine;

namespace HostileHorizon
{
    // Dan's standing rules for test runs on his PC (CLAUDE.md, Process: "Testing on Dan's PC"): always muted, never steal focus.
    // Muted: with -mute, and always for an exe check (-exeCheck) or a launcher test run (-racerTestSave). The master volume is 0
    // before the first scene loads, and a filter on the audio listener zeroes the final mix, so nothing reaches the speakers even
    // if something sets the volume back. The filter also measures what the game would have played ("produced") and what it hands
    // on ("output", 0 when muted), for the check's report.
    // No window: exe checks run with -batchmode (Tools/run_check.py), so there is no window to pop up or take focus. The view camera
    // renders into an off-screen image at the check's size; the screen overlays (HUD, menus) are drawn by that camera too, and the
    // check's screenshots are read from it.
    public sealed class TestRun : MonoBehaviour
    {
        static bool Has(string a) => Array.IndexOf(Environment.GetCommandLineArgs(), a) >= 0;
        public static readonly bool Muted = Has("-mute") || Has("-exeCheck") || Has("-racerTestSave");
        public static bool Headless => Application.isBatchMode && Has("-exeCheck");
        public static RenderTexture View { get; private set; }

        public static float RecentRms { get; private set; }
        static double producedSum, outputSum; static long samples; static float producedPeak, outputPeak;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Early() { if (Muted) AudioListener.volume = 0; }

        // Put on the object that holds the AudioListener.
        public static void Attach(GameObject listener) { if (Muted) listener.AddComponent<TestRun>(); }

        void Update() { if (AudioListener.volume != 0) AudioListener.volume = 0; }

        void OnAudioFilterRead(float[] data, int channels)
        {
            double p = 0; float pk = 0;
            for (int i = 0; i < data.Length; i++) { float v = data[i]; p += v * v; if (Mathf.Abs(v) > pk) pk = Mathf.Abs(v); data[i] = 0; }
            double o = 0; float ok = 0;
            for (int i = 0; i < data.Length; i++) { float v = data[i]; o += v * v; if (Mathf.Abs(v) > ok) ok = Mathf.Abs(v); }
            producedSum += p; outputSum += o; samples += data.Length;
            RecentRms = Mathf.Lerp(RecentRms, Mathf.Sqrt((float)(p / Math.Max(1, data.Length))), .15f);   // Fix 6.3: the mix the game makes now (before it is zeroed)
            if (pk > producedPeak) producedPeak = pk; if (ok > outputPeak) outputPeak = ok;
        }

        public static string Report()
        {
            if (!Muted) return "sound: NOT MUTED";
            double n = Math.Max(1, samples);
            return $"sound: muted from the first frame; measured output level {Math.Sqrt(outputSum / n):0.000} RMS, peak {outputPeak:0.000} " +
                   $"(the game produced {Math.Sqrt(producedSum / n):0.000} RMS, peak {producedPeak:0.000}, zeroed before the speakers; {samples} samples); master volume {AudioListener.volume:0.00}";
        }

        public static string WindowReport() => Headless ? $"window: none (batch mode), view {View?.width}x{View?.height} off-screen" : "window: a normal window (not a headless check)";

        static int Arg(string key, int fallback) { int i = Array.IndexOf(Environment.GetCommandLineArgs(), key); return i >= 0 && i + 1 < Environment.GetCommandLineArgs().Length && int.TryParse(Environment.GetCommandLineArgs()[i + 1], out int v) ? v : fallback; }

        // Headless check: the camera draws into an off-screen image of the check's size.
        public static void SetupHeadless(Camera head)
        {
            if (!Headless) return;
            View = new RenderTexture(Arg("-screen-width", 1920), Arg("-screen-height", 1080), 24, RenderTextureFormat.ARGB32) { name = "Check view", antiAliasing = 1 };
            View.Create();
            head.targetTexture = View;
            DrawOverlays(head);
        }

        // Screen overlays can't draw into an off-screen image: let the view camera draw them, just past its near plane.
        public static void DrawOverlays(Camera head)
        {
            if (!Headless) return;
            foreach (var c in FindObjectsByType<Canvas>())
                if (c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay) { c.renderMode = RenderMode.ScreenSpaceCamera; c.worldCamera = head; c.planeDistance = head.nearClipPlane + .01f; }
                else if (c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceCamera) c.planeDistance = head.nearClipPlane + .01f;
        }

        public static void SaveView(Camera head, string path)
        {
            DrawOverlays(head); Canvas.ForceUpdateCanvases();
            head.Render();
            var prev = RenderTexture.active; RenderTexture.active = View;
            var t = new Texture2D(View.width, View.height, TextureFormat.RGB24, false);
            t.ReadPixels(new Rect(0, 0, View.width, View.height), 0, 0); t.Apply();
            RenderTexture.active = prev;
            File.WriteAllBytes(path, t.EncodeToPNG()); Destroy(t);
        }
    }
}
