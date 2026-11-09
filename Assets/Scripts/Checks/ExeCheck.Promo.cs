using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

namespace HostileHorizon
{
    // Round 10 (B3): the promo footage, captured from the built exe as an exe check (batch mode, muted, no window: the testing rules):
    //   HostileHorizon.exe -batchmode -mute -exeCheck <dir> -exeCheckOnly promo:<group> -promoFfmpeg <ffmpeg.exe> [-promoDay 20261109]
    //       [-promoSeed 1984] -screen-width 3840 -screen-height 2160      (Tools/promo_capture.py starts it)
    // Each shot is rendered frame by frame at a fixed 1/60 s (Time.captureFramerate: no dropped frames however slow a frame is to
    // draw), the screen HUD and menus hidden, at higher settings than play (WorldLook: Promo.Active), and piped raw to ffmpeg:
    // <dir>/<shot>.mp4 (H.264, near lossless) + <shot>.events.txt (frame, event: knocks, shots, hits, touchdown, the strike ...) for
    // the edit's sounds. Groups: free (canyon run, cockpit, hands, VR look-around, the biomes, an ocean arch), launch (the
    // mothership, the drop, radar / route, braking, the belly camera, touchdown, a pilot's run and knocks, docking), combat (turret
    // fire, shield, gun, a saucer pass), ending_<type> (an alien at the airlock: the knock, the airlock, the burst).
    public static class Promo
    {
        public static bool Active => CheckRun.Active && CheckRun.Only.StartsWith("promo");
    }

    // places the camera rig for a shot (after the game's own camera code)
    [DefaultExecutionOrder(31000)]
    public sealed class PromoCam : MonoBehaviour
    {
        public Func<float, Pose> Root; public Func<float, Quaternion> Look; public float T0; public bool On;
        void LateUpdate()
        {
            if (!On) return;
            var rig = GameShell.I.Rig; float t = Time.time - T0;
            if (Root != null) { var p = Root(t); rig.transform.SetPositionAndRotation(p.position, p.rotation); }
            rig.Head.transform.localRotation = Look != null ? Look(t) : Quaternion.identity;
        }
    }

    sealed class PromoRec
    {
        readonly Process ff; readonly Stream input; readonly Texture2D tex; byte[] buf;
        public int Frames; public string Error = ""; public double ReadMs, WriteMs;
        public PromoRec(string ffmpeg, string path, int w, int h)
        {
            var psi = new ProcessStartInfo(ffmpeg, $"-hide_banner -loglevel error -y -f rawvideo -pix_fmt rgb24 -s {w}x{h} -framerate 60 -i - -vf vflip -c:v libx264 -preset ultrafast -crf 11 -pix_fmt yuv420p -threads 4 \"{path}\"")
            { UseShellExecute = false, RedirectStandardInput = true, CreateNoWindow = true };
            ff = Process.Start(psi); input = ff.StandardInput.BaseStream; tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        }
        public void Grab(RenderTexture rt)
        {
            var sw = Stopwatch.StartNew();
            var prev = RenderTexture.active; RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0, false); RenderTexture.active = prev;
            var data = tex.GetRawTextureData<byte>(); if (buf == null || buf.Length != data.Length) buf = new byte[data.Length]; data.CopyTo(buf);
            ReadMs += sw.Elapsed.TotalMilliseconds; sw.Restart();
            try { input.Write(buf, 0, buf.Length); Frames++; } catch (Exception e) { Error = e.Message; }
            WriteMs += sw.Elapsed.TotalMilliseconds;
        }
        public void Close() { try { input.Flush(); input.Close(); } catch { } ff.WaitForExit(600000); UnityEngine.Object.Destroy(tex); }
    }

    public sealed partial class ExeCheck
    {
        PromoCam pcam; string ffmpeg; int pframe; readonly List<string> marks = new List<string>();
        static string PArg(string k, string d) => LauncherBridge.Argument(k) ?? d;

        IEnumerator Promo()
        {
            ffmpeg = PArg("-promoFfmpeg", "ffmpeg");
            pcam = gameObject.AddComponent<PromoCam>();
            GameSettings.S.flightAssist = 0;   // the scripted pilot banks and lets the assist turn (as the checks fly)
            Career.CheckSystemSeed = int.Parse(PArg("-promoSystem", "4242"));
            yield return WaitFor(() => G.Now == GameShell.State.Title && G.Menu.Current != null, 90f);
            yield return new WaitForSecondsRealtime(1.5f);
            Log($"promo capture: {TestRun.View.width} x {TestRun.View.height}, 60 fps fixed step; day {Seeds.OfTheDay}; ffmpeg {ffmpeg}; view {G.Terrain.ViewDistance:0} m");
            SaveCodeSounds();
            string group = CheckRun.Only.Contains(":") ? CheckRun.Only.Substring(CheckRun.Only.IndexOf(':') + 1) : "free";
            if (group == "free") yield return PromoFree();
            else if (group == "launch") yield return PromoLaunch();
            else if (group == "combat") yield return PromoCombat();
            else if (group.StartsWith("ending_")) yield return PromoEnding((AlienType)Enum.Parse(typeof(AlienType), group.Substring(7), true));
            else if (group == "test") yield return PromoTest();
            else Log("unknown promo group " + group);
        }

        // ---- the recorder
        IEnumerator Rec(string name, float seconds, Func<float, Pose> cam = null, Action<float> drive = null, float fov = 60f, Func<float, Quaternion> look = null, Func<bool> until = null, float after = 0)
        {
            var head = G.Rig.Head; float fov0 = head.fieldOfView;
            Time.captureFramerate = 60; yield return null;
            pcam.T0 = Time.time; pcam.Root = cam; pcam.Look = look; pcam.On = true; head.fieldOfView = fov;
            var rec = new PromoRec(ffmpeg, Path.Combine(dir, name + ".mp4"), TestRun.View.width, TestRun.View.height);
            marks.Clear(); StartMarks(); renderMs = 0;
            float wall = Time.realtimeSinceStartup; int max = Mathf.RoundToInt(seconds * 60), stopAt = -1;
            for (pframe = 0; pframe < max; pframe++)
            {
                drive?.Invoke(Time.time - pcam.T0);
                yield return new WaitForEndOfFrame();
                Track();
                GrabFrame(rec);
                if (stopAt < 0 && until != null && until()) { stopAt = pframe + Mathf.RoundToInt(after * 60); Mark("until"); }
                if (stopAt >= 0 && pframe >= stopAt) { pframe++; break; }
            }
            StopMarks(); rec.Close();
            Time.captureFramerate = 0; pcam.On = false; pcam.Root = null; pcam.Look = null; head.fieldOfView = fov0; head.transform.localRotation = Quaternion.identity;
            File.WriteAllText(Path.Combine(dir, name + ".events.txt"), string.Join("\n", marks));
            float took = Time.realtimeSinceStartup - wall;
            Log($"shot {name}: {rec.Frames} frames ({rec.Frames / 60f:0.00} s) in {took:0} s ({rec.Frames / Mathf.Max(took, .01f):0.0} frames/s){(rec.Error != "" ? " ERROR " + rec.Error : "")}; per frame: render {renderMs / Mathf.Max(1, rec.Frames):0} ms, read back {rec.ReadMs / Mathf.Max(1, rec.Frames):0} ms, to ffmpeg {rec.WriteMs / Mathf.Max(1, rec.Frames):0} ms; {marks.Count} events; {G.Now}/{G.Phase}");
            state.leftStick = Vector2.zero; state.leftTrigger = 0; state.rightTrigger = 0; Set();
        }

        readonly List<Canvas> hidden = new List<Canvas>(); double renderMs;
        void GrabFrame(PromoRec rec)
        {
            hidden.Clear();
            foreach (var c in FindObjectsByType<Canvas>()) if (c.isRootCanvas && c.enabled && c.renderMode != RenderMode.WorldSpace) { c.enabled = false; hidden.Add(c); }
            var ret = G.Ship.transform.Find("Reticle")?.GetComponent<Renderer>(); bool retOn = ret && ret.enabled;
            if (ret && pcam.Root != null) ret.enabled = false;   // the gun's reticle floats in front of the ship: only from the cockpit
            var sw = Stopwatch.StartNew();
            G.Rig.Head.Render();   // a batch-mode player draws no camera by itself
            renderMs += sw.Elapsed.TotalMilliseconds;
            if (ret) ret.enabled = retOn;
            foreach (var c in hidden) c.enabled = true;
            rec.Grab(TestRun.View);
        }

        // ---- events for the edit's sounds (frame of the shot, what)
        void Mark(string what) => marks.Add($"{pframe}\t{what}");
        int mShots, mEShots, mHits, mBlocked, mDead; bool mLanded, mShield, mBrake, mStrike; string mPhase; GameShell.State mNow; int mAboard;
        readonly List<DownedPilot> knockSubs = new List<DownedPilot>();
        void OnKnock(DownedPilot p, int n) => Mark($"knock {n} pilot {p.Index + 1} at {Vector3.Distance(p.Position, G.Rig.Head.transform.position):0.0} m");
        void StartMarks()
        {
            var f = G.Foes; mShots = f.PlayerShots; mEShots = f.EnemyShots; mHits = f.EnemyHits; mBlocked = f.Blocked; mDead = f.All.Count(e => e.Dead);
            mLanded = G.Ship.Landed; mShield = G.ShieldOn; mBrake = G.Ship.Braking; mStrike = G.StrikeSeenAt > 0; mPhase = G.Phase; mNow = G.Now; mAboard = G.Pilots.Count(p => p.Now == DownedPilot.State.Aboard);
            foreach (var p in G.Pilots) { p.OnKnock += OnKnock; knockSubs.Add(p); }
        }
        void StopMarks() { foreach (var p in knockSubs) if (p) p.OnKnock -= OnKnock; knockSubs.Clear(); }
        void Track()
        {
            var f = G.Foes;
            if (f.PlayerShots > mShots) { Mark("gun"); mShots = f.PlayerShots; }
            if (f.EnemyShots > mEShots) { Mark("enemy_fire"); mEShots = f.EnemyShots; }
            if (f.EnemyHits > mHits) { Mark("hit"); mHits = f.EnemyHits; }
            if (f.Blocked > mBlocked) { Mark("shield_block"); mBlocked = f.Blocked; }
            int dead = f.All.Count(e => e.Dead); if (dead > mDead) { Mark("enemy_destroyed"); mDead = dead; }
            if (G.Ship.Landed != mLanded) { Mark(G.Ship.Landed ? "touchdown" : "liftoff"); mLanded = G.Ship.Landed; }
            if (G.ShieldOn != mShield) { Mark(G.ShieldOn ? "shield_on" : "shield_off"); mShield = G.ShieldOn; }
            if (G.Ship.Braking != mBrake) { Mark(G.Ship.Braking ? "brake" : "brake_end"); mBrake = G.Ship.Braking; }
            if (!mStrike && G.StrikeSeenAt > 0) { Mark("strike"); mStrike = true; }
            if (G.Phase != mPhase) { Mark("phase " + G.Phase); mPhase = G.Phase; }
            if (G.Now != mNow) { Mark("state " + G.Now); mNow = G.Now; }
            int ab = G.Pilots.Count(p => p.Now == DownedPilot.State.Aboard); if (ab > mAboard) { Mark("boarded"); mAboard = ab; }
        }

        // the game's code-made sounds (the knock, the brakes ...) as WAV files for the edit
        void SaveCodeSounds()
        {
            foreach (var (n, c) in new[] { ("knock", Sfx.Knock), ("brake", Sfx.Brake), ("beep", Sfx.Beep), ("warn", Sfx.Warn) })
            {
                if (!c) continue; var d = new float[c.samples * c.channels]; c.GetData(d, 0);
                using (var w = new BinaryWriter(File.Create(Path.Combine(dir, "sfx_" + n + ".wav"))))
                {
                    int bytes = d.Length * 2; w.Write(Encoding.ASCII.GetBytes("RIFF")); w.Write(36 + bytes); w.Write(Encoding.ASCII.GetBytes("WAVEfmt ")); w.Write(16); w.Write((short)1); w.Write((short)c.channels);
                    w.Write(c.frequency); w.Write(c.frequency * c.channels * 2); w.Write((short)(c.channels * 2)); w.Write((short)16); w.Write(Encoding.ASCII.GetBytes("data")); w.Write(bytes);
                    foreach (var v in d) w.Write((short)Mathf.Clamp(Mathf.RoundToInt(v * 32767f), -32768, 32767));
                }
            }
        }

        // ---- cameras
        static Vector3 FlatFwd(Transform t) { var f = t.forward; f.y = 0; return f.sqrMagnitude < 1e-4f ? Vector3.forward : f.normalized; }
        float Ground(Vector3 p) => G.Ship.Field.SurfaceHeight(p.x, p.z);
        // follows the ship at an offset in its level frame (x right, y up, z ahead), the heading eased so it never whips; kept over the ground
        Func<float, Pose> Follow(Vector3 off, Vector3 lookOff, float turn = 2.5f)
        {
            Vector3 fs = Vector3.zero; float lift = 0; bool init = false;
            return t =>
            {
                var s = G.Ship.transform; var f = FlatFwd(s); float dt = Mathf.Max(Time.deltaTime, 1e-4f);
                fs = init ? Vector3.Slerp(fs, f, 1 - Mathf.Exp(-turn * dt)).normalized : f;
                var r = Vector3.Cross(Vector3.up, fs);
                var p = s.position + r * off.x + Vector3.up * off.y + fs * off.z;
                float need = Mathf.Max(0, Ground(p) + 3f - p.y);
                lift = init ? Mathf.Lerp(lift, need, 1 - Mathf.Exp(-(need > lift ? 6f : 1.2f) * dt)) : need; init = true;
                p.y = Mathf.Max(p.y + lift, Ground(p) + 1.5f);
                var look = s.position + r * lookOff.x + Vector3.up * lookOff.y + fs * lookOff.z;
                return new Pose(p, Quaternion.LookRotation(look - p, Vector3.up));
            };
        }
        // a fixed place, turning to follow a point (smoothed)
        Func<float, Pose> Fixed(Vector3 at, Func<Vector3> target, float smooth = 5f)
        {
            Vector3 l = target();
            return t => { l = Vector3.Lerp(l, target(), 1 - Mathf.Exp(-smooth * Mathf.Max(Time.deltaTime, 1e-4f))); return new Pose(at, Quaternion.LookRotation(l - at, Vector3.up)); };
        }
        // a head turning through keys (t, yaw, pitch), eased, with a little life in it
        static Func<float, Quaternion> HeadPath(params Vector3[] keys)
        {
            return t =>
            {
                int i = 0; while (i < keys.Length - 2 && t > keys[i + 1].x) i++;
                var a = keys[i]; var b = keys[Mathf.Min(i + 1, keys.Length - 1)];
                float k = b.x > a.x ? Mathf.SmoothStep(0, 1, Mathf.Clamp01((t - a.x) / (b.x - a.x))) : 1;
                float yaw = Mathf.Lerp(a.y, b.y, k) + Mathf.Sin(t * 1.7f) * 1.2f, pitch = Mathf.Lerp(a.z, b.z, k) + Mathf.Sin(t * 1.3f + 1) * .8f;
                return Quaternion.Euler(-pitch, yaw, Mathf.Sin(t * .9f) * 1.5f);
            };
        }
        static Func<float, Quaternion> LookFixed(float yaw, float pitch) => t => Quaternion.Euler(-pitch, yaw, 0);

        // ---- the scripted pilot, a frame at a time
        void SteerTo(Vector3 target, float above = 70f, float throttle = -1)
        {
            var s = G.Ship; var t = s.transform; var to = target - t.position; to.y = 0;
            float want = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg, err = Mathf.DeltaAngle(s.Heading, want);
            float bank = Mathf.Asin(Mathf.Clamp(-t.right.y, -1, 1)) * Mathf.Rad2Deg;
            float roll = Mathf.Clamp((Mathf.Clamp(err * 1.5f, -50, 50) - bank) * .04f, -1, 1);
            float high = 0; var dir = to.normalized;
            for (int m = 0; m <= 10; m++) { var p = t.position + dir * (m * 60f); high = Mathf.Max(high, s.Field.Height(p.x, p.z)); }
            float climb = Mathf.Clamp((high + above - t.position.y) * .02f, -.45f, .8f), pitch = Mathf.Clamp((climb - t.forward.y) * 3f, -1, 1);
            state.leftStick = new Vector2(Firm(roll), -Firm(pitch)); state.leftTrigger = 0; state.rightTrigger = throttle > 0 ? throttle : 0; Set();
        }
        void HoverStep(Vector3 spot, float hold = 35f)
        {
            var s = G.Ship; var t = s.transform; var to = spot - t.position; to.y = 0; float d = to.magnitude;
            float err = Mathf.DeltaAngle(s.Heading, Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg);
            float hs = new Vector2(s.Velocity.x, s.Velocity.z).magnitude;
            float turn = d > 4f && Mathf.Abs(err) > 3f ? Mathf.Sign(err) * Mathf.Clamp(Mathf.Abs(err) / 30f, .35f, 1f) : 0;
            float fwd = d > 4f && Mathf.Abs(err) < 40f ? Mathf.Clamp(d / 40f, .35f, 1f) : 0;
            if (hs > 4f && d < hs * hs / 16f) fwd = 0;
            float agl = s.AboveGround, lift;
            if (d > 10f) lift = Mathf.Clamp((hold - agl) * .12f, -1, 1);
            else lift = agl > 22f ? -.85f : agl > 8f ? -.35f : -.17f;
            state.leftStick = new Vector2(turn, fwd); state.rightTrigger = Mathf.Max(0, lift); state.leftTrigger = Mathf.Max(0, -lift); Set();
        }
        bool t0Stopped() => new Vector2(G.Ship.Velocity.x, G.Ship.Velocity.z).magnitude < .5f;
        void Healthy() { if (G.Ship.Damage > 0 || G.Ship.Energy < 90) G.Ship.SetEnergyAndDamage(100, 0); }
        IEnumerator PressLater(GamepadButton b) { yield return Press(b); }

        // ---- groups
        IEnumerator PromoTest()
        {
            yield return FreeStart(Biome.Canyons, 1);
            yield return Rec("test_chase", 3f, Follow(new Vector3(2.5f, 3.5f, -20f), new Vector3(0, 1, 30f)), t => Autopilot(), 55f);
            yield return Rec("test_cockpit", 2f, null, t => Autopilot(), 72f, LookFixed(12, -50));
        }

        IEnumerator FreeStart(Biome b, int crash)
        {
            if (G.Now != GameShell.State.Title) yield return ToTitle();
            GameSettings.S.seedMode = 2; GameSettings.S.customSeed = int.Parse(PArg("-promoSeed", "1984"));
            yield return StartFreeFlight(b, crash);
            var s = G.Ship; s.Spawn(s.transform.position, s.transform.rotation, s.Level, .8f, 0); G.Rig.Current = CameraRig.Mode.Cockpit; G.Rig.Snap();
            yield return Fly(4f);   // up to speed (not recorded)
            Log($"  {b}: seed {G.FlightSeed}, speed {G.Ship.Speed * 3.6f:0} km/h, {G.Ship.AboveGround:0} m over the ground");
        }

        IEnumerator PromoFree()
        {
            yield return FreeStart(Biome.Canyons, 1);
            yield return Rec("canyon_chase", 8f, Follow(new Vector3(2.5f, 3.5f, -20f), new Vector3(0, 1, 30f)), t => Autopilot(), 55f);
            yield return Rec("canyon_front", 7f, Follow(new Vector3(5f, 1.5f, 22f), new Vector3(0, .5f, -4f), 3f), t => Autopilot(), 50f);
            yield return Rec("canyon_side", 7f, Follow(new Vector3(13f, 1f, 2f), new Vector3(0, 0, 4f), 3f), t => Autopilot(), 45f);
            yield return Rec("canyon_high", 7f, Follow(new Vector3(-10f, 45f, -80f), new Vector3(0, 0, 70f), 1.5f), t => Autopilot(), 50f);
            yield return Rec("canyon_cockpit", 8f, null, t => Autopilot(), 72f);
            yield return Rec("cockpit_hands", 8f, null, t => Autopilot(), 72f, LookFixed(12, -50));
            yield return Rec("cockpit_throttle", 6f, null, t => Autopilot(), 72f, LookFixed(-38, -44));
            yield return Rec("vr_look", 10f, null, t => Autopilot(), 88f, HeadPath(new Vector3(0, 0, -4), new Vector3(1.3f, -15, -6), new Vector3(2.8f, -80, 6),
                new Vector3(4.2f, -40, -50), new Vector3(5.6f, 0, -70), new Vector3(7f, 55, -25), new Vector3(8.4f, 85, 2), new Vector3(10f, 8, -5)));
            foreach (var (b, crash, cam, fov) in new (Biome, int, Func<Func<float, Pose>>, float)[] {
                (Biome.Forest, 1, () => Follow(new Vector3(14f, 2f, 4f), new Vector3(0, 0, 6f), 3f), 48f),
                (Biome.City, 1, () => Follow(new Vector3(5f, 1.5f, 24f), new Vector3(0, .5f, -4f), 3f), 50f),
                (Biome.Ice, 1, () => Follow(new Vector3(3f, 4f, -22f), new Vector3(0, 1, 30f)), 55f),
                (Biome.Volcanic, 0, () => Follow(new Vector3(-3f, 3f, -20f), new Vector3(0, -2f, 30f)), 55f),
                (Biome.Crystal, 1, () => Follow(new Vector3(-12f, 3f, 6f), new Vector3(0, 0, 8f), 3f), 48f),
                (Biome.Ocean, 1, () => Follow(new Vector3(0, 6f, -24f), new Vector3(0, 0, 40f)), 55f) })
            {
                yield return FreeStart(b, crash);
                yield return Rec("biome_" + b.ToString().ToLower(), 7f, cam(), t => Autopilot(), fov);
                if (b == Biome.Ocean) yield return OceanArch();
            }
        }

        IEnumerator OceanArch()
        {
            var s = G.Ship; var f = s.Field; var p = s.transform.position; CanyonField.Box arch = default; bool found = false;
            int ci = Mathf.FloorToInt(p.x / CanyonField.PropCell), cj = Mathf.FloorToInt(p.z / CanyonField.PropCell);
            for (int r = 0; r < 14 && !found; r++) for (int dj = -r; dj <= r && !found; dj++) for (int di = -r; di <= r && !found; di++) foreach (var bx in f.BoxesAt(ci + di, cj + dj)) if (bx.kind == 2) { arch = bx; found = true; break; }
            if (!found) { Log("  no arch near"); yield break; }
            var across = Quaternion.Euler(0, arch.yaw, 0) * Vector3.right; float under = arch.c.y - arch.half.y;
            var from = arch.c - across * 260f; from.y = under * .5f;
            yield return Ready(arch.c);
            s.Spawn(from, Quaternion.LookRotation(across), CrashLevel.Bounce, .55f, 0); G.Rig.Snap();
            yield return Rec("ocean_arch", 7f, Follow(new Vector3(4f, 3f, -20f), new Vector3(0, 2f, 30f), 4f), t => { state.leftStick = Vector2.zero; Set(); }, 55f);
        }

        IEnumerator NewCareerDocked()
        {
            if (G.Now != GameShell.State.Title) yield return ToTitle();
            yield return NewCareer9();
            yield return WaitPhase(() => G.Now == GameShell.State.Docked && G.Phase == "docked", 90f, "the hangar");
            Log($"  career: {G.Planet.name} ({Biomes.Name(G.WorldBiome)}, seed {G.Planet.seed}), {G.Pilots.Count} pilots");
        }

        IEnumerator PromoLaunch()
        {
            yield return NewCareerDocked();
            var m = G.Mother.transform;
            yield return Rec("mother_wide", 8f, t => { float a = Mathf.Lerp(196f, 214f, t / 8f) * Mathf.Deg2Rad; var at = m.TransformPoint(new Vector3(Mathf.Sin(a) * 430f, 70f - t * 3f, Mathf.Cos(a) * 430f)); return new Pose(at, Quaternion.LookRotation(m.TransformPoint(new Vector3(0, -10, 30)) - at, Vector3.up)); }, null, 38f);
            G.Menu.Current.Selected = 0; bool pressed = false;
            var at2 = m.TransformPoint(new Vector3(55f, -45f, -85f));
            yield return Rec("launch", 11f, Fixed(at2, () => G.Ship.transform.position, 4f), t => { if (!pressed && t > .4f) { pressed = true; StartCoroutine(PressLater(GamepadButton.South)); } }, 42f);
            yield return WaitPhase(() => G.Now == GameShell.State.Flying, 30f, "flight");
            var p = NearestWaiting(); if (p == null) { Log("  no pilot"); yield break; }
            yield return Rec("route_cockpit", 8f, null, t => SteerTo(p.LandingSpot, 70f, 1f), 72f, LookFixed(0, -14));
            yield return Rec("route_chase", 7f, Follow(new Vector3(0, 14f, -34f), new Vector3(0, -6f, 40f), 2f), t => SteerTo(p.LandingSpot, 70f, 1f), 55f);
            // to the pilot, braking on the game's own cue (it stops the ship over the pad), then straight down, as a player does
            G.Foes.Running = false;   // no turret fire over the pickup
            yield return WaitFor(() => { SteerTo(p.LandingSpot, 70f, Vector3.Distance(p.LandingSpot, G.Ship.transform.position) > 2500f ? 1f : -1f); return G.Displays.Cue != "" || Flat(p.LandingSpot - G.Ship.transform.position).magnitude < 300f; }, 120f);
            Log($"  braking {Flat(p.LandingSpot - G.Ship.transform.position).magnitude:0} m from the pad at {G.Ship.Speed * 3.6f:0} km/h (cue \"{G.Displays.Cue}\")");
            pressed = false;
            yield return Rec("brake", 6f, Follow(new Vector3(-16f, 2f, 10f), new Vector3(0, 0, 6f), 3f), t => { if (!pressed) { pressed = true; StartCoroutine(PressLater(GamepadButton.West)); } }, 50f, null, () => t0Stopped(), 1f);
            Log($"  stopped {Flat(p.LandingSpot - G.Ship.transform.position).magnitude:0} m from the pad, {G.Ship.AboveGround:0} m up");
            yield return Rec("hover_cockpit", 14f, null, t => HoverStep(p.LandingSpot, 30f), 72f, LookFixed(0, -32), () => G.Ship.AboveGround < 14f);   // over the pad (the pilot can reach it), then down
            var sp0 = G.Ship.transform; var side = sp0.TransformPoint(new Vector3(-22f, 0, 16f)); side.y = Ground(side) + 3f;
            yield return Rec("touchdown", 14f, Fixed(side, () => G.Ship.transform.position + Vector3.up * 1f, 4f), t => { if (!G.Ship.Landed) HoverStep(p.LandingSpot, 30f); }, 45f, null, () => G.Ship.Landed, 1.5f);
            if (!G.Ship.Landed) { Log("  not landed: over the pad as a player would"); yield return HoverLand(p.LandingSpot); }
            if (G.Ship.EnginesOn) yield return Press(GamepadButton.South);
            yield return WaitFor(() => Vector3.Distance(p.Position, G.Ship.transform.position) < 55f || p.AtDoor, 90f);
            var st = G.Ship.transform; var runCam = st.TransformPoint(new Vector3(-10f, 0, 12f)); runCam.y = Ground(runCam) + 1.3f;
            yield return Rec("pilot_run", 16f, Fixed(runCam, () => p.Position + Vector3.up * 1.2f, 6f), null, 45f, null, () => p.AtDoor, 1f);
            yield return Rec("knock_cockpit", 7.5f, null, null, 72f, LookFixed(0, -30));   // the airlock camera on the centre display (the pilot is behind the canopy frame)
            var doorCam = st.TransformPoint(new Vector3(-6.5f, 1.4f, 5.5f));
            yield return Rec("knock_door", 7.5f, Fixed(doorCam, () => p.Position + Vector3.up * 1.4f, 8f), null, 40f);
            yield return Press(GamepadButton.East);
            yield return WaitPhase(() => p.Now == DownedPilot.State.Aboard, 20f, "boarding");
            // up to the docking height (not recorded), then the docking
            yield return Press(GamepadButton.South);
            state.rightTrigger = 1; Set();
            yield return WaitFor(() => G.Now != GameShell.State.Flying, 240f);
            state.rightTrigger = 0; Set();
            yield return WaitFor(() => G.Phase == "dock-final", 120f);
            var dockCam = m.TransformPoint(new Vector3(26f, 9f, -120f));
            yield return Rec("dock", 40f, Fixed(dockCam, () => G.Ship.transform.position, 4f), null, 45f, null, () => G.Phase == "docked", 1.5f);
        }

        IEnumerator PromoCombat()
        {
            yield return NewCareerDocked();
            G.Menu.Current.Selected = Row("Jump"); yield return Press(GamepadButton.South);   // planet 2: saucers too
            yield return WaitFor(() => G.Phase == "jump", 10f);
            yield return WaitPhase(() => G.Now == GameShell.State.Docked && G.Phase == "docked", 120f, "the hangar after the jump");
            Log($"  planet 2: {G.Planet.name} ({Biomes.Name(G.WorldBiome)})");
            G.Menu.Current.Selected = 0; yield return Press(GamepadButton.South);
            yield return WaitPhase(() => G.Now == GameShell.State.Flying, 40f, "flight");
            var e = NearestAlive(EnemyKind.Turret);
            if (e != null && PlaceInSight(e, 560f, .45f, CrashLevel.Ghost, 30f))
            {
                yield return Ready(e.Pos);
                PlaceInSight(e, 560f, .45f, CrashLevel.Ghost, 30f);
                yield return Rec("turret_fire", 8f, t => { var s = G.Ship.transform; var to = Flat(e.Pos - s.position).normalized; var r = Vector3.Cross(Vector3.up, to); var at = s.position - to * 16f + r * 7f + Vector3.up * 3f; return new Pose(at, Quaternion.LookRotation(Vector3.Lerp(s.position, e.Centre, .35f) - at, Vector3.up)); },
                    t => { Healthy(); state.leftStick = Vector2.zero; Set(); }, 50f);
                bool sh = false;
                yield return Rec("shield", 7f, Follow(new Vector3(6f, 2f, -15f), new Vector3(0, 0, 20f), 3f), t => { Healthy(); if (!sh && t > .4f) { sh = true; StartCoroutine(PressLater(GamepadButton.DpadLeft)); } }, 50f);
                if (G.ShieldOn) yield return Press(GamepadButton.DpadLeft);
                PlaceInSight(e, 380f, .2f, CrashLevel.Ghost, 12f);
                yield return Rec("gun_cockpit", 12f, null, t =>
                {
                    Healthy(); var s = G.Ship.transform; var aim = Quaternion.LookRotation(e.Centre - s.TransformPoint(ShipLayout.Eye));
                    if (!e.Dead) s.rotation = t < 1f ? Quaternion.Slerp(s.rotation, aim, 1 - Mathf.Exp(-4f * Time.deltaTime)) : aim;   // swings round, then dead on (as the Round 5 check aims)
                    state = state.WithButton(GamepadButton.South, !e.Dead && t > .6f); Set();
                }, 66f, null, () => e.Dead, 2f);
                state = state.WithButton(GamepadButton.South, false); Set();
            }
            else Log("  no turret in sight");
            var u = NearestAlive(EnemyKind.Saucer);
            if (u == null) { Log("  no saucer"); yield break; }
            var sp = G.Ship; var p0 = u.Pos + Flat(sp.transform.position - u.Pos).normalized * 700f; p0.y = Mathf.Max(Ground(p0) + 90f, u.Pos.y - 40f);
            sp.Spawn(p0, Quaternion.LookRotation(Flat(u.Pos - p0)), CrashLevel.Ghost, .5f, 0); G.Rig.Snap();
            yield return Ready(p0);
            yield return WaitFor(() => u.Chasing || G.Now != GameShell.State.Flying, 8f);
            yield return Rec("saucer_pass", 10f, t => { var s = G.Ship.transform; var to = (u.Pos - s.position); var fl = Flat(to).normalized; var at = s.position - fl * 22f + Vector3.up * 6f + Vector3.Cross(Vector3.up, fl) * 6f; return new Pose(at, Quaternion.LookRotation(Vector3.Lerp(s.position, u.Pos, .5f) - at, Vector3.up)); },
                t => { Healthy(); state.leftStick = new Vector2(Mathf.Sin(t * .8f) * .5f, -.2f); Set(); }, 55f);
            yield return Rec("saucer_kill", 14f, null, t =>
            {
                Healthy(); var s = G.Ship.transform; var c = u.Centre + u.Vel * (Vector3.Distance(u.Centre, s.position) / Tuning.T.gunBoltSpeed);
                if (!u.Dead) s.rotation = Quaternion.Slerp(s.rotation, Quaternion.LookRotation(c - s.TransformPoint(ShipLayout.Eye)), 1 - Mathf.Exp(-4f * Time.deltaTime));
                state = state.WithButton(GamepadButton.South, !u.Dead && t > .4f); Set();
            }, 66f, null, () => u.Dead, 2.5f);
            state = state.WithButton(GamepadButton.South, false); Set();
        }

        IEnumerator PromoEnding(AlienType type)
        {
            yield return NewCareerDocked();
            G.Menu.Current.Selected = 0; yield return Press(GamepadButton.South);
            yield return WaitPhase(() => G.Now == GameShell.State.Flying, 40f, "flight");
            G.CheckSetup(3, new[] { (int)type, 0, 0 }, new[] { 0, 0, 0 }); G.Foes.Running = false;
            G.CheckStrikeKind = (int)GameShell.StrikeKind.Instant; G.StrikeAtReset();
            var p = G.Pilots.Find(x => x.Index == 0);
            yield return LandFor(p);
            yield return WaitFor(() => Vector3.Distance(p.Position, G.Ship.transform.position) < 55f || p.AtDoor, 90f);
            var st = G.Ship.transform; var runCam = st.TransformPoint(new Vector3(-10f, 0, 12f)); runCam.y = Ground(runCam) + 1.3f;
            string k = type.ToString().ToLower();
            yield return Rec($"end_{k}_run", 16f, Fixed(runCam, () => p.Position + Vector3.up * 1.2f, 6f), null, 45f, null, () => p.AtDoor, .6f);
            yield return Rec($"end_{k}_knock", 7.5f, null, null, 72f, LookFixed(0, -30));
            bool pressed = false; float t0 = -1;
            yield return Rec($"end_{k}_airlock", 18f, null, t => { if (!pressed && t > .8f) { pressed = true; t0 = t; StartCoroutine(PressLater(GamepadButton.East)); } }, 72f,
                t => { float a = t0 < 0 ? 0 : Mathf.SmoothStep(0, 1, (t - t0 - .4f) / 1.6f); return Quaternion.Euler(Mathf.Lerp(28f, 4f, a), Mathf.Lerp(0, 0, a), 0); }, () => G.StrikeSeenAt > 0, 1.6f);
            G.CheckStrikeKind = -1;
            Log($"  {type}: strike \"{G.LastStrike}\"");
        }
    }
}
