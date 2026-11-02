using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace HostileHorizon
{
    public static class CheckRun
    {
        static int active = -1;
        public static bool Active { get { if (active < 0) active = Array.IndexOf(System.Environment.GetCommandLineArgs(), "-exeCheck") >= 0 ? 1 : 0; return active == 1; } }
        public static string Dir => LauncherBridge.Argument("-exeCheck");
        public static string Only => LauncherBridge.Argument("-exeCheckOnly") ?? "all";
    }

    // Round 1 check, in the built exe:  HostileHorizon.exe -exeCheck <dir> [-exeCheckOnly all|perf] [-steamDeck 1]
    // Plays the game from the title screen with a virtual gamepad added to the Input System, so every press goes through the same
    // input actions a player's pad does (menus, flight, view button, pause). Screenshots from the player's view, frame rate while
    // flying low through the canyon, every crash level, seed determinism, a height map of the seed of the day. Writes
    // <dir>/exe_check.txt, then quits. Settings are the defaults and are never saved (CheckRun).
    public sealed partial class ExeCheck : MonoBehaviour
    {
        Gamepad pad; GamepadState state; readonly StringBuilder log = new StringBuilder(); string dir;
        GameShell G => GameShell.I;

        IEnumerator Start()
        {
            dir = CheckRun.Dir; Directory.CreateDirectory(dir);
            Application.runInBackground = true;   // a check keeps playing unfocused (players' builds pause)
            Log(TestRun.WindowReport() + "; " + TestRun.Report());
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;   // and its virtual pad keeps working
            pad = InputSystem.AddDevice<Gamepad>("CheckPad");
            Log($"Hostile Horizon exe check {DateTime.Now:yyyy-MM-dd HH:mm}; {(TestRun.Headless ? $"{TestRun.View.width}x{TestRun.View.height} off-screen" : $"{Screen.width}x{Screen.height}")}; GPU {SystemInfo.graphicsDeviceName}; Steam Deck setting {(SteamDeck.On ? "on" : "off")}; view {G.Terrain.ViewDistance:0} m");
            IEnumerator run = CheckRun.Only == "perf" ? Perf() : CheckRun.Only == "campaign" ? Campaign() : CheckRun.Only == "mute" ? MuteCheck() : CheckRun.Only == "screen" ? ScreenCheck() : CheckRun.Only == "fullscreen" ? FullscreenCheck() : All();
            while (true)
            {
                object cur;
                try { if (!run.MoveNext()) break; cur = run.Current; }
                catch (Exception e) { Log("CHECK FAILED with an exception: " + e); break; }
                yield return cur;
            }
            Release();
            Log("end: " + TestRun.WindowReport() + "; " + TestRun.Report());
            File.WriteAllText(Path.Combine(dir, "exe_check.txt"), log.ToString());
            yield return new WaitForSecondsRealtime(.5f);
            Application.Quit(0);
        }

        void Log(string s) { log.AppendLine(s); Debug.Log("[ExeCheck] " + s); }

        // ---- the virtual pad
        void Set() => InputSystem.QueueStateEvent(pad, state);
        void Release() { state = default; Set(); }
        IEnumerator Press(GamepadButton b)
        {
            state = state.WithButton(b, true); Set(); yield return null; yield return null;
            state = state.WithButton(b, false); Set(); yield return null; yield return null;
        }
        IEnumerator Nav(GamepadButton dpad, int times) { for (int i = 0; i < times; i++) yield return Press(dpad); }
        IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();
            if (TestRun.Headless) TestRun.SaveView(G.Rig.Head, Path.Combine(dir, name + ".png"));
            else ScreenCapture.CaptureScreenshot(Path.Combine(dir, name + ".png"));
            yield return null; yield return null;
            Log($"screenshot {name}.png");
        }
        IEnumerator WaitFor(Func<bool> f, float seconds) { float end = Time.realtimeSinceStartup + seconds; while (!f() && Time.realtimeSinceStartup < end) yield return null; }

        // Steer like a player would: stick toward the lowest way ahead, holding about 35 m over the ground ahead.
        void Autopilot()
        {
            var s = G.Ship; var f = s.Field; var t = s.transform;
            float bestCost = float.MaxValue, bestTurn = 0;
            for (int a = -60; a <= 60; a += 10)
            {
                var dir = Quaternion.Euler(0, s.Heading + a, 0) * Vector3.forward; float cost = Mathf.Abs(a) * .3f;
                for (int m = 1; m <= 6; m++) { var p = t.position + dir * (m * 45f); cost = Mathf.Max(cost, f.Height(p.x, p.z) + Mathf.Abs(a) * .3f); }
                if (cost < bestCost) { bestCost = cost; bestTurn = a; }
            }
            float ahead = 0; for (int m = 0; m <= 5; m++) { var p = t.position + t.forward * (m * 30f); ahead = Mathf.Max(ahead, f.SurfaceHeight(p.x, p.z)); }
            float climb = Mathf.Clamp((ahead + 35f - t.position.y) * .025f, -.6f, .9f);
            float pitch = Mathf.Clamp((climb - t.forward.y) * 3f, -1, 1);
            float bank = Mathf.Asin(Mathf.Clamp(-t.right.y, -1, 1)) * Mathf.Rad2Deg;
            float roll = Mathf.Clamp((Mathf.Clamp(bestTurn * 1.2f, -55, 55) - bank) * .04f, -1, 1);
            state.leftStick = new Vector2(roll, -pitch);   // the pad's stick: up = nose down
            state.leftTrigger = 0; state.rightTrigger = 0; Set();
        }

        IEnumerator Fly(float seconds, Action<float> each = null)
        {
            float end = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < end && G.Now == GameShell.State.Flying) { Autopilot(); each?.Invoke(Time.unscaledDeltaTime); yield return null; }
            state.leftStick = Vector2.zero; Set();
        }

        IEnumerator StartFromTitle(int crashLevel)
        {
            yield return WaitFor(() => G.Now == GameShell.State.Title && G.Menu.Current != null, 30f);
            yield return Press(GamepadButton.South);                       // Free Flight
            int diff = (crashLevel - GameSettings.S.crashLevel + 5) % 5;
            yield return Nav(GamepadButton.DpadDown, 2);                   // Crash level
            yield return Nav(GamepadButton.DpadRight, diff);
            yield return Nav(GamepadButton.DpadDown, 1);                   // Start flight
            yield return Press(GamepadButton.South);
            yield return WaitFor(() => G.Now == GameShell.State.Flying, 30f);
        }

        IEnumerator PauseToTitle()
        {
            yield return Press(GamepadButton.Start);
            yield return WaitFor(() => G.Now == GameShell.State.Paused, 3f);
            yield return Nav(GamepadButton.DpadDown, 3);                   // Return to title
            yield return Press(GamepadButton.South);
            yield return WaitFor(() => G.Now == GameShell.State.Title, 30f);
        }

        // Short check of the test-run rules: title, then 10 s of Free Flight (engine and wind playing), muted and unfocused.
        IEnumerator MuteCheck()
        {
            yield return WaitFor(() => G.Now == GameShell.State.Title, 60f);
            yield return StartFromTitle(1);
            yield return Fly(10f);
            yield return Shot("mute_check_flying");
        }

        // Fix 2.1: the full-screen start. Runs ScreenSetting's size code path for a simulated Steam Deck desktop (1280 x 800) and for this
        // PC's display, says the check itself changed nothing (its -screen-* size stands), then plays at the check's size (run it at
        // --size 1280x800) and measures whether every menu / HUD element and every line of text lies inside the view.
        IEnumerator ScreenCheck()
        {
            yield return WaitFor(() => G.Now == GameShell.State.Title && G.Menu.Current != null, 60f);
            foreach (var (w, h, what) in new[] { (1280, 800, "simulated Steam Deck"), (3840, 2160, "simulated 4K display"), (Display.main.systemWidth, Display.main.systemHeight, "this PC's display (Display.main)"),
                                                 (Screen.currentResolution.width, Screen.currentResolution.height, "this PC's desktop (Screen.currentResolution)") })
            {
                var t = ScreenSetting.Target(w, h, true, 0, 0); var win = ScreenSetting.Target(w, h, false, 0, 0);
                Log($"screen setting, {what} {w} x {h}: default asks for {t.w} x {t.h} {t.mode} -> {(t.mode == FullScreenMode.FullScreenWindow && t.h == Mathf.Min(h, ScreenSetting.DefaultMaxHeight) && Mathf.Abs((float)t.w / t.h - (float)w / h) < .01f ? (t.w == w ? "fills the screen at its own size" : "fills the screen, same shape, drawn smaller and stretched") : "DOES NOT FILL THE SCREEN")}; Windowed would be {win.w} x {win.h}");
            }
            Log($"check run applied nothing: Screen {Screen.width} x {Screen.height} {Screen.fullScreenMode}; setting shown as {ScreenSetting.ScreenLabel}, {ScreenSetting.ResolutionLabel}");
            yield return new WaitForSecondsRealtime(1f);
            yield return Shot("s1_title"); UiInside("title");
            yield return Nav(GamepadButton.DpadDown, 2); yield return Press(GamepadButton.South);   // Settings
            yield return Shot("s2_settings"); UiInside("settings");
            yield return Nav(GamepadButton.DpadDown, 5); yield return Press(GamepadButton.South);   // Display
            Log($"page {G.Menu.Current?.Title}");
            yield return Shot("s3_display"); UiInside("display");
            yield return Press(GamepadButton.East); yield return Press(GamepadButton.East);       // back to the title
            yield return StartFromTitle(1);
            yield return Fly(5f);
            yield return Shot("s4_cockpit"); UiInside("cockpit HUD");
            yield return Press(GamepadButton.North); yield return Fly(2f);
            yield return Shot("s5_chase"); UiInside("chase HUD");
            yield return Press(GamepadButton.Start); yield return WaitFor(() => G.Now == GameShell.State.Paused, 3f);
            yield return Shot("s6_pause"); UiInside("pause");
        }

        // Fix 2.1, only with Dan's OK (a visible, focused window): a real start with the default screen setting applied (muted as every check).
        // Says what window Unity actually made against the display, then pictures and fps from the real screen.
        IEnumerator FullscreenCheck()
        {
            yield return WaitFor(() => G.Now == GameShell.State.Title && G.Menu.Current != null, 60f);
            yield return new WaitForSecondsRealtime(2f);
            int dw = Display.main.systemWidth, dh = Display.main.systemHeight;
            var want = ScreenSetting.DefaultSize(dw, dh); bool fills = Screen.width == want.x && Screen.height == want.y && Screen.fullScreenMode == FullScreenMode.FullScreenWindow;
            Log($"real start: window {Screen.width} x {Screen.height} {Screen.fullScreenMode}; display {dw} x {dh}; desktop {Screen.currentResolution.width} x {Screen.currentResolution.height} -> {(fills ? "fills the screen" : "DOES NOT FILL THE SCREEN")}");
            yield return Shot("f1_title"); UiInside("title");
            yield return StartFromTitle(1);
            yield return Fly(3f);
            yield return Shot("f2_cockpit"); UiInside("cockpit HUD");
            yield return MeasureFps($"cockpit, flying low, {Screen.width} x {Screen.height} full screen");
        }

        // Every visible image and every drawn glyph of the screen canvases, in view pixels: how far anything sticks out of the view.
        void UiInside(string what)
        {
            Canvas.ForceUpdateCanvases();
            float vw = TestRun.Headless ? TestRun.View.width : Screen.width, vh = TestRun.Headless ? TestRun.View.height : Screen.height;
            float worst = 0; string worstName = "-"; int n = 0;
            void Point(Vector3 world, Canvas c, string name)
            {
                var cam = c.renderMode == RenderMode.ScreenSpaceOverlay ? null : c.worldCamera;
                var p = RectTransformUtility.WorldToScreenPoint(cam, world);
                float o = Mathf.Max(-p.x, p.x - vw, -p.y, p.y - vh);
                if (o > worst) { worst = o; worstName = name; }
            }
            var corners = new Vector3[4];
            foreach (var c in FindObjectsByType<Canvas>())
            {
                if (!c.isRootCanvas || c.renderMode == RenderMode.WorldSpace || !c.isActiveAndEnabled) continue;
                foreach (var g in c.GetComponentsInChildren<UnityEngine.UI.Graphic>())
                {
                    if (!g.isActiveAndEnabled || g.color.a < .01f) continue;
                    n++;
                    if (g is UnityEngine.UI.Text tx)
                    {
                        if (string.IsNullOrEmpty(tx.text)) continue;
                        float ppu = tx.pixelsPerUnit;
                        foreach (var v in tx.cachedTextGenerator.verts) Point(tx.rectTransform.TransformPoint(v.position / ppu), c, c.name + "/" + tx.name);
                    }
                    else { g.rectTransform.GetWorldCorners(corners); foreach (var k in corners) Point(k, c, c.name + "/" + g.name); }
                }
            }
            Log($"inside the view ({vw} x {vh}), {what}: {n} elements; {(worst <= 1 ? "nothing cut off" : $"CUT OFF by {worst:0} px: {worstName}")}");
        }

        IEnumerator Perf()
        {
            yield return WaitFor(() => G.Now == GameShell.State.Title, 60f);
            yield return new WaitForSecondsRealtime(2f);
            yield return Shot("title");
            yield return StartFromTitle(1);
            yield return Fly(4f);
            yield return Shot("cockpit");
            yield return MeasureFps("cockpit, flying low");
            yield return Press(GamepadButton.North);
            yield return Fly(2f);
            yield return MeasureFps("chase view");
        }

        IEnumerator MeasureFps(string what)
        {
            var times = new List<float>(); float aglSum = 0; int n = 0;
            yield return Fly(15f, dt => { times.Add(dt); aglSum += G.Ship.AboveGround; n++; });
            if (times.Count < 10) { Log($"fps {what}: not measured (flight ended)"); yield break; }
            Log($"{what}: " + TestRun.Report());
            times.Sort();
            float avg = times.Count / times.Sum(), p99 = 1f / times[(int)(times.Count * .99f)];
            Log($"fps {what}: average {avg:0.0}, 1% low {p99:0.0}, worst frame {times[times.Count - 1] * 1000:0} ms over {times.Count} frames; mean height over ground {aglSum / Math.Max(1, n):0} m; terrain chunks {G.Terrain.Built}, waiting {G.Terrain.Pending}");
        }

        IEnumerator All()
        {
            yield return WaitFor(() => G.Now == GameShell.State.Title, 60f);
            yield return new WaitForSecondsRealtime(2f);
            yield return Shot("01_title");
            G.Menu.Canvas.gameObject.SetActive(false); yield return Shot("01_title_no_menu"); G.Menu.Canvas.gameObject.SetActive(true);   // the launcher's cover
            Determinism();
            HeightMap(Seeds.OfTheDay);

            // Free Flight setup page, then fly
            yield return Press(GamepadButton.South);
            yield return Shot("02_free_flight_setup");
            // typed-in seed: Seed -> Typed in, select the number, last digit +3 with the D-pad, a "5" typed on the keyboard, B deletes it
            yield return Nav(GamepadButton.DpadRight, 2); yield return Nav(GamepadButton.DpadDown, 1); yield return Press(GamepadButton.South);
            yield return Nav(GamepadButton.DpadUp, 3);
            if (Keyboard.current != null) { InputSystem.QueueTextEvent(Keyboard.current, '5'); yield return null; yield return null; }
            int typed = GameSettings.S.customSeed;
            yield return Shot("02b_seed_typing");
            yield return Press(GamepadButton.East);
            yield return Press(GamepadButton.South);
            Log($"typed-in seed: 1984 -> D-pad up x3 and a typed 5 -> {typed}; after B {GameSettings.S.customSeed}; chosen seed {G.ChosenSeed} (expected 1987)");
            yield return Nav(GamepadButton.DpadUp, 1); yield return Nav(GamepadButton.DpadLeft, 2);   // back to Seed of the day
            Log($"seed mode back to: {Seeds.ModeNames[GameSettings.S.seedMode]}");
            yield return Press(GamepadButton.East);
            yield return StartFromTitle(1);
            var start1 = G.Ship.transform.position;
            Log($"flight 1: seed {G.FlightSeed}, {CrashLevels.Names[(int)G.FlightLevel]}, start {start1}, heading {G.Ship.Heading:0}");
            yield return Fly(5f);
            yield return Shot("03_cockpit");
            yield return MeasureFps("cockpit, flying low (1080p target 60)");
            foreach (var v in new[] { "04_chase", "05_close_chase", "06_canyon" })
            {
                yield return Press(GamepadButton.North);
                yield return Fly(2.5f);
                yield return Shot(v);
            }
            yield return Press(GamepadButton.North);
            Log($"view after four presses of Y: {CameraRig.ModeNames[(int)G.Rig.Current]} (expected Cockpit)");
            yield return MeasureFps("cockpit again");
            // look with the right stick (the hat on a HOTAS)
            state.rightStick = new Vector2(1, .2f); Set();
            yield return Fly(1.2f); state.rightStick = new Vector2(1, .2f); Set();
            yield return Shot("07_cockpit_look_right");
            state.rightStick = Vector2.zero; Set();
            yield return Fly(1.5f);
            // throttle: RT up for 2 s
            float before = G.Ship.Throttle; state.rightTrigger = 1; Set();
            float tEnd = Time.realtimeSinceStartup + 2f; while (Time.realtimeSinceStartup < tEnd) { Autopilot(); state.rightTrigger = 1; Set(); yield return null; }
            state.rightTrigger = 0; Set();
            Log($"throttle with RT held 2 s: {before * 100:0}% -> {G.Ship.Throttle * 100:0}%, speed {G.Ship.Speed * 3.6f:0} km/h");

            // pause, settings, axis display
            yield return Press(GamepadButton.Start);
            yield return WaitFor(() => G.Now == GameShell.State.Paused, 3f);
            yield return Shot("08_pause");
            yield return Nav(GamepadButton.DpadDown, 2); yield return Press(GamepadButton.South);   // Settings
            yield return Shot("09_settings");
            yield return Nav(GamepadButton.DpadDown, 4); yield return Press(GamepadButton.South);   // Axis display
            state.leftStick = new Vector2(.5f, -.3f); Set(); yield return null; yield return null;
            yield return Shot("10_axis_display");
            state.leftStick = Vector2.zero; Set();
            yield return Press(GamepadButton.East); yield return Press(GamepadButton.East);       // back to pause
            Log($"after two B presses: page {G.Menu.Current?.Title}");
            yield return Press(GamepadButton.East);                                                 // resume
            Log($"after B on pause: {G.Now}");
            yield return Fly(1f);

            // restart with the same seed starts at the same place
            yield return PauseToTitle();
            yield return StartFromTitle(1);
            var start2 = G.Ship.transform.position;
            Log($"flight 2, same seed: start {start2} ({(Vector3.Distance(start1, start2) < .01f ? "same place" : "DIFFERENT")})");

            // every crash level: fly, then push the nose down into the ground
            for (int level = 0; level < 5; level++)
            {
                yield return PauseToTitle(); yield return StartFromTitle(level);
                yield return Fly(3f);
                float minAgl = 999, maxDmg = 0; float diveEnd = Time.realtimeSinceStartup + 6f; int hits0 = G.Ship.Hits;
                while (Time.realtimeSinceStartup < diveEnd && G.Now == GameShell.State.Flying)
                {
                    // full forward (nose down); the two damage levels get a gentle push and stop at the first hit, to see healing
                    bool gentle = level == 2 || level == 3;
                    state.leftStick = new Vector2(0, gentle ? .3f : 1f); Set();
                    minAgl = Mathf.Min(minAgl, G.Ship.AboveGround); maxDmg = Mathf.Max(maxDmg, G.Ship.Damage);
                    if (level != 0 && G.Ship.Hits > hits0 + 2) break;
                    if (gentle && G.Ship.Hits > hits0) break;
                    yield return null;
                }
                state.leftStick = Vector2.zero; Set();
                yield return new WaitForSecondsRealtime(.3f);
                yield return Shot($"11_crash_{level}_{CrashLevels.Names[level].Split(' ')[0].ToLower()}");
                string after = "";
                if (G.Now == GameShell.State.Flying && level >= 2)
                {
                    float d0 = G.Ship.Damage;
                    yield return Fly(4f);
                    after = $"; damage {d0:0}% -> {G.Ship.Damage:0}% after 4 s more flying";
                }
                yield return WaitFor(() => G.Now != GameShell.State.Crashed || G.Menu.Current != null && G.Menu.Current.Title == "SHIP LOST", 4f);
                Log($"crash level {CrashLevels.Names[level]}: hits {G.Ship.Hits - hits0}, lowest height over ground {minAgl:0.0} m, most damage {maxDmg:0}%, state {G.Now}{after}");
                if (G.Now == GameShell.State.Crashed)
                {
                    yield return Shot($"12_ship_lost_{level}");
                    yield return Nav(GamepadButton.DpadDown, 1); yield return Press(GamepadButton.South);   // Return to title
                    yield return WaitFor(() => G.Now == GameShell.State.Title, 30f);
                    yield return StartFromTitle(1);
                }
            }

            // VR rule 3: the title menu as a world-space panel
            yield return PauseToTitle();
            yield return new WaitForSecondsRealtime(1f);
            G.Menu.ShowInWorld(G.Rig.Head);
            yield return null;
            yield return Shot("13_menu_world_space");
            G.Menu.ShowOnScreen();
        }

        void Determinism()
        {
            int seed = Seeds.OfTheDay;
            string Sample(int s) { var f = new CanyonField(s); var sb = new StringBuilder(); var r = new System.Random(7); for (int i = 0; i < 2000; i++) sb.Append(f.Height((float)(r.NextDouble() * 20000 - 10000), (float)(r.NextDouble() * 20000 - 10000)).ToString("0.000")).Append(','); f.FindStart(out var p, out var h); sb.Append(p).Append(h); return sb.ToString(); }
            string a = Sample(seed), b = Sample(seed), c = Sample(seed + 1);
            Log($"determinism: seed {seed} twice -> {(a == b ? "identical" : "DIFFERENT")} (2000 heights + start); seed {seed + 1} -> {(a == c ? "SAME (bad)" : "different")}");
            var f2 = new CanyonField(seed); float lo = float.MaxValue, hi = float.MinValue; var rr = new System.Random(3);
            for (int i = 0; i < 20000; i++) { float h = f2.Height((float)(rr.NextDouble() * 12000 - 6000), (float)(rr.NextDouble() * 12000 - 6000)); lo = Mathf.Min(lo, h); hi = Mathf.Max(hi, h); }
            Log($"terrain heights over 12 x 12 km: {lo:0} m to {hi:0} m");
        }

        void HeightMap(int seed)
        {
            const int N = 600; const float step = 12f; var f = new CanyonField(seed); var tex = new Texture2D(N, N, TextureFormat.RGB24, false);
            f.FindStart(out var start, out _);
            var px = new Color32[N * N];
            for (int j = 0; j < N; j++) for (int i = 0; i < N; i++)
            {
                float x = start.x + (i - N / 2) * step, z = start.z + (j - N / 2) * step; float h = f.Height(x, z);
                byte v = (byte)Mathf.Clamp(h / 420f * 255f, 0, 255); px[j * N + i] = new Color32(v, v, v, 255);
            }
            for (int k = -3; k <= 3; k++) { px[(N / 2) * N + N / 2 + k] = new Color32(255, 60, 30, 255); px[(N / 2 + k) * N + N / 2] = new Color32(255, 60, 30, 255); }
            tex.SetPixels32(px); tex.Apply();
            File.WriteAllBytes(Path.Combine(dir, "00_heightmap.png"), tex.EncodeToPNG());
            Log($"height map of seed {seed}: 00_heightmap.png ({N * step / 1000:0.0} km square, 12 m a pixel, black 0 m, white 420 m, red cross = start)");
        }
    }
}
