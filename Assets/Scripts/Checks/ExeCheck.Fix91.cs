using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;
using System.Runtime.InteropServices;

namespace HostileHorizon
{
    // Fix 9.1 check, in the built exe, muted, no window:
    //   -exeCheckOnly fix91        flat: (A1) the alien share over 400 planets a tier (tiers 0-4) for each preset, 0.9.0's rule and now;
    //                              (A3) the mothership's material slots and pictures outside / in the bay; (A2) dockings and lifts from
    //                              seven starts and the launch, 0.9.0's paths and now, each against the hull's mesh (crossings of its
    //                              triangles, nearest distance, seconds under the hull), pictures along the new approach (chase, cockpit)
    //   -exeCheckOnly fix91 -vr    (A4) on the mock OpenXR runtime: the head turned 90 / 180 degrees, recentred (hold Pause 1 s, the
    //                              right-stick click), on the title, in flight and docked in the hangar; the headset "taken off" (tracking
    //                              lost, back at another yaw): recentred by itself; both eyes compared with the straight-ahead view;
    //                              0.9.0's recentre the same way
    public sealed partial class ExeCheck
    {
        IEnumerator Fix91()
        {
            StartCoroutine(Progress());
            yield return WaitFor(() => G.Now == GameShell.State.Title, 90f);
            yield return new WaitForSecondsRealtime(1.5f);
            bool vr = VrMode.Active;
            Log($"VR {(vr ? "on" : "off")}: {VrMode.Status}");
            if (vr) { yield return Recentre91(); yield break; }
            AlienShares();
            yield return Mother91();
        }

        // ---- A1
        void AlienShares()
        {
            Log("--- A1: aliens per pilot over 400 planets a tier (seeds 1000..1399 + tier), not a career's first planet");
            var t = Tuning.T; var d = Difficulty.Current; var keep = d.Copy();
            Log($"  tuning now: alienShareStart {t.alienShareStart}, alienSharePerTier {t.alienSharePerTier}, alienShareMax {t.alienShareMax}, levels [{string.Join(", ", t.alienShareLevels)}]; 0.9.0: .20 / .05 / .35, at least one a planet");
            for (int preset = 0; preset < 4; preset++)
            {
                d.ApplyPreset(preset);
                var line = new List<string>();
                for (int tier = 0; tier <= 4; tier++)
                {
                    int n = Mathf.Min(t.pilotsMax, t.pilotsFirstPlanet + tier * t.pilotsPerTier);
                    int aliens = 0, pilots = 0, none = 0, oldAliens = 0; var types = new int[6];
                    for (int k = 0; k < 400; k++)
                    {
                        var p = new PlanetState { seed = 1000 + k * 7919 + tier, tier = tier, pilots = new int[n] };
                        Aliens.Assign(p, false);
                        int a = p.alien.Count(x => x != 0); aliens += a; pilots += n; if (a == 0) none++; foreach (var x in p.alien) types[x]++;
                        float old = Mathf.Min(.35f, .20f + .05f * tier) * Difficulty.AlienShare;
                        oldAliens += Mathf.Clamp(Mathf.RoundToInt(old * n), 1, n);
                    }
                    line.Add($"tier {tier} ({n} pilots): {aliens * 100f / pilots:0.0} % (expected {Aliens.Share(tier) * 100:0.0} %), {none * 100f / 400:0} % of planets with none; 0.9.0: {oldAliens * 100f / pilots:0.0} %, none 0 %; types {string.Join("/", types.Skip(1))}");
                }
                Log($"  {Difficulty.PresetNames[preset]}:\n    " + string.Join("\n    ", line));
            }
            Career.C?.difficulty?.ApplyPreset(keep.preset); if (Career.C == null) GameSettings.S.difficulty = keep;
        }

        // ---- A3 + A2
        Vector3[] hullV; int[] hullT; Dictionary<Vector3Int, List<int>> triGrid; Dictionary<Vector3Int, List<int>> vertGrid; const float Cell = 6f;
        static Vector3Int CellOf(Vector3 p) => new Vector3Int(Mathf.FloorToInt(p.x / Cell), Mathf.FloorToInt(p.y / Cell), Mathf.FloorToInt(p.z / Cell));
        void BuildHull()
        {
            var m = G.Mother.transform; var vs = new List<Vector3>(); var ts = new List<int>(); int skipped = 0;
            foreach (var mf in m.GetComponentsInChildren<MeshFilter>(true))
            {
                string n = mf.name; if (n.Contains("glow") || n.Contains("glass") || n.Contains("window") || n.Contains("bay_cradle") || n.StartsWith("Door")) continue;
                var mesh = mf.sharedMesh; if (!mesh) continue; if (!mesh.isReadable) { skipped++; continue; }
                var M = m.worldToLocalMatrix * mf.transform.localToWorldMatrix; int b = vs.Count;
                foreach (var v in mesh.vertices) vs.Add(M.MultiplyPoint3x4(v));
                foreach (var i in mesh.triangles) ts.Add(b + i);
            }
            hullV = vs.ToArray(); hullT = ts.ToArray(); triGrid = new Dictionary<Vector3Int, List<int>>(); vertGrid = new Dictionary<Vector3Int, List<int>>();
            for (int i = 0; i < hullV.Length; i++) { var c = CellOf(hullV[i]); if (!vertGrid.TryGetValue(c, out var l)) vertGrid[c] = l = new List<int>(); l.Add(i); }
            for (int t = 0; t < hullT.Length; t += 3)
            {
                Vector3 a = hullV[hullT[t]], b = hullV[hullT[t + 1]], c = hullV[hullT[t + 2]];
                var lo = CellOf(Vector3.Min(a, Vector3.Min(b, c))); var hi = CellOf(Vector3.Max(a, Vector3.Max(b, c)));
                for (int x = lo.x; x <= hi.x; x++) for (int y = lo.y; y <= hi.y; y++) for (int z = lo.z; z <= hi.z; z++)
                { var k = new Vector3Int(x, y, z); if (!triGrid.TryGetValue(k, out var l)) triGrid[k] = l = new List<int>(); l.Add(t); }
            }
            Log($"  hull mesh for the measurement: {hullV.Length} vertices, {hullT.Length / 3} triangles (unreadable meshes skipped: {skipped}); hull box {G.Mother.HullBox.min:F0} .. {G.Mother.HullBox.max:F0}");
        }
        bool SegHits(Vector3 p, Vector3 q)
        {
            var lo = CellOf(Vector3.Min(p, q)); var hi = CellOf(Vector3.Max(p, q)); var d = q - p; var seen = new HashSet<int>();
            for (int x = lo.x; x <= hi.x; x++) for (int y = lo.y; y <= hi.y; y++) for (int z = lo.z; z <= hi.z; z++)
                if (triGrid.TryGetValue(new Vector3Int(x, y, z), out var l))
                    foreach (int t in l)
                    {
                        if (!seen.Add(t)) continue;
                        Vector3 a = hullV[hullT[t]], e1 = hullV[hullT[t + 1]] - a, e2 = hullV[hullT[t + 2]] - a, h = Vector3.Cross(d, e2); float det = Vector3.Dot(e1, h);
                        if (Mathf.Abs(det) < 1e-9f) continue; float f = 1 / det; var s = p - a; float u = f * Vector3.Dot(s, h); if (u < 0 || u > 1) continue;
                        var qq = Vector3.Cross(s, e1); float v = f * Vector3.Dot(d, qq); if (v < 0 || u + v > 1) continue; float tt = f * Vector3.Dot(e2, qq);
                        if (tt >= 0 && tt <= 1) return true;
                    }
            return false;
        }
        float Nearest(Vector3 p)
        {
            float best = 99; var c = CellOf(p);
            for (int x = -2; x <= 2; x++) for (int y = -2; y <= 2; y++) for (int z = -2; z <= 2; z++)
                if (vertGrid.TryGetValue(c + new Vector3Int(x, y, z), out var l)) foreach (int i in l) best = Mathf.Min(best, (hullV[i] - p).magnitude);
            return best;
        }
        // the ship's outline: wing tips, nose, tail, top, belly, centre (ship space)
        static readonly Vector3[] ShipPts = { new Vector3(-4.4f, 0, -1), new Vector3(4.4f, 0, -1), new Vector3(0, 0, 5), new Vector3(0, .5f, -5), new Vector3(0, 1.2f, 0), new Vector3(0, -1.2f, 0), Vector3.zero };
        string crossWhat = ""; float trackT0; bool tracking; int crossings; float nearest, under; Vector3 crossAt;
        IEnumerator TrackHull()
        {
            tracking = true; crossWhat = ""; trackT0 = Time.time; crossings = 0; nearest = 99; under = 0; crossAt = default; var prev = new Vector3[ShipPts.Length]; bool have = false;
            var b = G.Mother.HullBox;
            while (tracking)
            {
                yield return null;
                var s = G.Ship.transform; var lp = new Vector3[ShipPts.Length];
                for (int i = 0; i < ShipPts.Length; i++) lp[i] = G.Mother.Local(s.TransformPoint(ShipPts[i]));
                if (have) for (int i = 0; i < lp.Length; i++) if (SegHits(prev[i], lp[i])) { crossings++; if (crossAt == default) { crossAt = lp[i]; crossWhat = $"point {i}, {prev[i]:F1} -> {lp[i]:F1}, ship centre {lp[ShipPts.Length - 1]:F1}, phase {G.Phase}, {Time.time - trackT0:0.00} s in"; } }
                var c = lp[ShipPts.Length - 1]; bool inBay = Mathf.Abs(c.x) < 14 && c.y > -7 && c.y < 9.5f && c.z < 14;   // the docked ship sits in the bay
                if (!inBay) foreach (var p in lp) nearest = Mathf.Min(nearest, Nearest(p));
                if (c.x > b.min.x && c.x < b.max.x && c.z > b.min.z && c.z < b.max.z && c.y < b.min.y && c.y > b.min.y - 40f) under += Time.deltaTime;
                prev = lp; have = true;
            }
        }
        string HullResult() { tracking = false; return $"{(crossings == 0 ? "never through the hull" : $"THROUGH THE HULL: {crossings} crossings of its surface (first: {crossWhat})")}; nearest {nearest:0.0} m outside the bay; {under:0.0} s under the hull (0-40 m below its keel)"; }

        void PlaceCam(Vector3 local, Vector3 lookLocal) { G.Rig.Scripted = true; var p = G.Mother.World(local); G.Rig.transform.SetPositionAndRotation(p, Quaternion.LookRotation(G.Mother.World(lookLocal) - p, Vector3.up)); }

        IEnumerator Mother91()
        {
            Career.CheckSystemSeed = 4242;
            yield return NewCareer9();
            yield return WaitPhase(() => G.Now == GameShell.State.Docked && G.Phase == "docked", 90f, "the hangar");
            Log("--- A3: the mothership's materials (cause: Mothership.cs never loaded the Meshy materials; every part plain grey)");
            foreach (var l in Mothership.Slots) Log("  " + l);
            int tex = Mothership.Slots.Count(l => !l.Contains("no texture")), all = Mothership.Slots.Count(l => !l.Contains("Glow") && !l.Contains("Glass"));
            Log($"  {tex} parts textured");
            BuildHull();
            G.Menu.Hide(); G.Displays.SetVisible(false);
            foreach (var (n, at, look) in new[] { ("a3_stern", new Vector3(0, 6, -300), new Vector3(0, 0, -20)), ("a3_side", new Vector3(260, 40, 10), new Vector3(0, 0, 20)),
                                                   ("a3_below", new Vector3(70, -140, -60), new Vector3(0, -10, 10)), ("a3_bow", new Vector3(-120, 30, 330), new Vector3(0, 0, 60)),
                                                   ("a3_bay_inside", new Vector3(9, 5, -22), new Vector3(-4, 0, 6)) })
            { PlaceCam(at, look); yield return null; yield return new WaitForSecondsRealtime(.3f); yield return Shot(n); }
            G.Rig.Scripted = false;

            Log("--- A2: dockings and lifts against the hull's mesh (ship outline: wing tips, nose, tail, top, belly)");
            var starts = new (string name, Vector3 at, bool tow)[] {
                ("docking, from below and behind", new Vector3(0, -500, -700), false), ("docking, from below and ahead", new Vector3(0, -500, 700), false),
                ("docking, from below at the side", new Vector3(500, -500, 0), false), ("docking, beside it at its height", new Vector3(250, 0, 30), false),
                ("docking, from above it (a climb past its height)", new Vector3(40, 250, 20), false),
                ("lift (tow / recall / beam), from below", new Vector3(20, -600, 0), true), ("lift (tow / recall / beam), from above it", new Vector3(30, 200, -10), true) };
            foreach (bool old in new[] { true, false })
                for (int i = 0; i < starts.Length; i++)
                {
                    var st = starts[i]; GameShell.CheckOldDockPath = old;
                    var p = G.Mother.World(st.at); var heading = Quaternion.LookRotation(Flat(G.Mother.transform.position - p).sqrMagnitude > 1 ? Flat(G.Mother.transform.position - p) : Vector3.forward);
                    G.CheckDock(p, heading, st.tow); yield return null;
                    StartCoroutine(TrackHull()); float t0 = Time.time;
                    bool pics = !old && (i == 0 || i == 4);
                    if (pics) StartCoroutine(DockPictures(i == 0 ? CameraRig.Mode.Chase : CameraRig.Mode.Cockpit, i == 0 ? "a2_chase" : "a2_cockpit"));
                    yield return WaitFor(() => G.Phase == "docked" && G.Now == GameShell.State.Docked, 240f);
                    picsOn = false;
                    Log($"  {(old ? "0.9.0" : "now  ")} {st.name}: {(G.Phase == "docked" ? $"docked after {Time.time - t0:0.0} s" : $"NOT DOCKED in 240 s (phase {G.Phase})")}; {HullResult()}");
                    yield return new WaitForSecondsRealtime(.3f);
                }
            Log("--- A2: the launch (drops through the floor doors)");
            foreach (bool old in new[] { true, false })
            {
                GameShell.CheckOldDockPath = old;
                if (!(G.Now == GameShell.State.Docked && G.Phase == "docked")) { Log("  not docked: launch not measured"); break; }
                StartCoroutine(TrackHull());
                yield return LaunchFromHangar(old ? null : "a2_launch_after_drop");
                Log($"  {(old ? "0.9.0" : "now  ")} launch: {HullResult()}");
                if (old) { G.CheckDock(G.Mother.World(new Vector3(0, -400, -600)), Quaternion.identity, false); yield return WaitFor(() => G.Phase == "docked" && G.Now == GameShell.State.Docked, 240f); }
            }
            GameShell.CheckOldDockPath = false;
        }

        bool picsOn;
        IEnumerator DockPictures(CameraRig.Mode mode, string tag)
        {
            picsOn = true; G.Rig.Current = mode; bool a = false, f = false, s = false, inside = false;
            while (picsOn)
            {
                var lp = G.Mother.Local(G.Ship.transform.position); var b = G.Mother.HullBox;
                if (!a && G.Phase == "dock-approach" && Time.time > 0) { a = true; yield return new WaitForSecondsRealtime(1.5f); yield return Shot(tag + "_1_round_the_hull"); }
                else if (!f && G.Phase == "dock-final") { f = true; yield return Shot(tag + "_2_lined_up_behind"); }
                else if (!s && G.Phase == "dock-final" && lp.z > b.min.z - 25f) { s = true; yield return Shot(tag + "_3_at_the_stern"); }
                else if (!inside && G.Phase == "dock-final" && lp.z > -12f) { inside = true; yield return Shot(tag + "_4_in_the_bay"); }
                yield return null;
            }
            yield return Shot(tag + "_5_docked");
        }

        // ---- A4 (mock runtime)
        static readonly Vector4 Fov0 = new Vector4(-0.995535672f, 0.811128199f, 0.954059243f, -0.954661012f), Fov1 = new Vector4(-0.812360585f, 0.995566666f, 0.955580175f, -0.953877985f);
        // the mock runtime's own test API (mock_runtime.dll, already loaded by the OpenXR loader; Unity's MockRuntime class needs its feature
        // enabled in the build, the check uses the runtime through XR_RUNTIME_JSON instead). Reference space View = 1, PrimaryStereo = 2.
        [DllImport("mock_runtime", EntryPoint = "MockRuntime_SetReferenceSpace")] static extern void MockSetSpace(int space, Vector3 position, Vector4 orientation, ulong flags);
        [DllImport("mock_runtime", EntryPoint = "MockRuntime_SetView")] static extern void MockSetView(int config, int view, Vector3 position, Vector4 orientation, Vector4 fov);
        [DllImport("mock_runtime", EntryPoint = "MockRuntime_SetViewState")] static extern void MockSetViewState(int config, ulong flags);
        static Vector4 Q(Quaternion q) => new Vector4(q.x, q.y, q.z, q.w);
        bool MockHead(float yaw, Vector3 pos, bool tracked)
        {
            try
            {
                var q = Quaternion.Euler(0, yaw, 0);
                MockSetSpace(1, pos, Q(q), tracked ? 15ul : 0ul);
                MockSetView(2, 0, pos + q * new Vector3(-.011f, 0, 0), Q(q), Fov0);
                MockSetView(2, 1, pos + q * new Vector3(.011f, 0, 0), Q(q), Fov1);
                MockSetViewState(2, tracked ? 15ul : 0ul);
                return true;
            }
            catch (Exception e) { Log($"  mock runtime call failed: {e.GetType().Name} {e.Message}"); return false; }
        }
        // how far the head looks from straight ahead: the eye pose asked for (title: the title view; flight and hangar: the ship's nose)
        string Off(string what)
        {
            var h = G.Rig.Head.transform; Quaternion want = G.Now == GameShell.State.Title ? (CameraRig.CheckOldVr ? G.Rig.transform.rotation : G.Rig.EyeRot) : G.Ship.transform.rotation;
            var f = Quaternion.Inverse(want) * h.forward; float yaw = Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg;
            var eye = G.Now == GameShell.State.Title ? (Vector3?)null : G.Ship.transform.TransformPoint(ShipLayout.Eye);
            lastOff = yaw;
            return $"{what}: head local yaw {Mathf.DeltaAngle(0, h.localEulerAngles.y):0}, looks {yaw:+0;-0;0}° from straight ahead{(eye.HasValue ? $", {Vector3.Distance(h.position, eye.Value) * 100:0} cm from the eye point" : "")}";
        }
        float lastOff;
        IEnumerator HoldPause(float s) { state = state.WithButton(GamepadButton.Start, true); Set(); yield return new WaitForSecondsRealtime(s); state = state.WithButton(GamepadButton.Start, false); Set(); yield return null; yield return null; }
        IEnumerator StickClick() { yield return Press(GamepadButton.RightStick); yield return new WaitForSecondsRealtime(.2f); }

        // a real head turns: 90 degrees in 0.6 s (an instant jump is what the runtime's origin resets look like: those recentre by themselves)
        float headYaw; Vector3 headPos;
        IEnumerator TurnHead(float yaw, Vector3 pos, float seconds = .6f)
        {
            float y0 = headYaw; var p0 = headPos;
            for (float t = 0; t < seconds; t += Time.unscaledDeltaTime) { float k = Mathf.SmoothStep(0, 1, t / seconds); MockHead(Mathf.Lerp(y0, yaw, k), Vector3.Lerp(p0, pos, k), true); yield return null; }
            MockHead(yaw, pos, true); headYaw = yaw; headPos = pos; yield return null;
        }
        IEnumerator EndOfFrame() { yield return new WaitForEndOfFrame(); }   // read the head as the eyes see it (after every placement)

        IEnumerator Turns(string where, bool old)
        {
            CameraRig.CheckOldVr = old; string who = old ? "0.9.0" : "now  ";
            if (!MockHead(0, Vector3.zero, true)) yield break; headYaw = 0; headPos = Vector3.zero;
            yield return new WaitForSecondsRealtime(.6f); yield return StickClick(); yield return new WaitForSecondsRealtime(.3f);
            int a0 = G.Rig.AutoRecentered;
            yield return EndOfFrame(); Log($"  {who} {where}: {Off("straight, recentred")}");
            foreach (float yaw in new[] { 90f, 180f })
            {
                yield return TurnHead(yaw, new Vector3(.12f, -.05f, .2f), yaw / 150f); yield return new WaitForSecondsRealtime(.6f);
                yield return EndOfFrame(); string before = Off($"head turned to {yaw:0}° (smoothly), moved 24 cm");
                if (old) yield return StickClick(); else yield return HoldPause(1.25f);
                yield return new WaitForSecondsRealtime(.3f); yield return EndOfFrame();
                Log($"  {who} {where}: {before}; after {(old ? "the right-stick click" : "holding Pause 1.25 s")}: {Off("")} -> {(Mathf.Abs(lastOff) < 3 ? "straight ahead" : "NOT straight ahead")}; state {G.Now}{(G.Now == GameShell.State.Paused ? " (PAUSED: the hold paused)" : "")}; automatic recentres meanwhile {G.Rig.AutoRecentered - a0}");
            }
            // the headset off for a moment, back on at another yaw (the runtime re-oriented it)
            a0 = G.Rig.AutoRecentered;
            MockHead(headYaw, headPos, false); yield return new WaitForSecondsRealtime(1.2f);
            Log($"    (headset off: {G.Rig.TrackingNow})");
            MockHead(305, new Vector3(-.2f, .1f, .1f), true); headYaw = 305; headPos = new Vector3(-.2f, .1f, .1f); yield return new WaitForSecondsRealtime(1.5f);
            yield return EndOfFrame();
            Log($"    (back on: {G.Rig.TrackingNow})");
            Log($"  {who} {where}: headset off 1.2 s, back on turned to 305°: {Off("")} -> {(Mathf.Abs(lastOff) < 3 ? "straight ahead by itself" : "NOT straight ahead")}; automatic recentres {G.Rig.AutoRecentered - a0}{(G.Rig.AutoRecentered > a0 ? $" ({G.Rig.LastRecenterWhy})" : "")}");
            yield return TurnHead(0, Vector3.zero); yield return new WaitForSecondsRealtime(.4f); yield return StickClick();
            CameraRig.CheckOldVr = false;
        }

        IEnumerator Recentre91()
        {
            Log("--- A4: the VR recentre on the mock runtime (the mock headset turned with MockRuntime.SetSpace / SetViewPose)");
            // the title (menus)
            yield return Turns("title (menus)", true); yield return Turns("title (menus)", false);
            // a tap of Pause still pauses (in flight)
            yield return StartFreeFlight(Biome.Canyons, 1); yield return Fly(1f);
            yield return Press(GamepadButton.Start); yield return new WaitForSecondsRealtime(.3f);
            Log($"  a tap of Pause in VR: {G.Now} ({(G.Now == GameShell.State.Paused ? "pauses, as before" : "DID NOT PAUSE")})");
            if (G.Now == GameShell.State.Paused) { G.Menu.Current.Selected = Row("Recenter"); Log($"  pause menu row: \"{G.Menu.Current.Rows[G.Menu.Current.Selected].Label()}\""); yield return Press(GamepadButton.East); yield return new WaitForSecondsRealtime(.3f); }
            // in flight, frozen (time stopped) so the pictures compare
            Time.timeScale = 0;
            yield return Turns("in flight (cockpit)", true); yield return Turns("in flight (cockpit)", false);
            Time.timeScale = 1;
            MockHead(0, Vector3.zero, true); yield return StickClick();
            // docked in the hangar
            yield return ToTitle();
            Career.CheckSystemSeed = 4242; yield return NewCareer9();
            yield return WaitPhase(() => G.Now == GameShell.State.Docked && G.Phase == "docked", 90f, "the hangar");
            yield return Turns("docked in the hangar (a menu up)", true); yield return Turns("docked in the hangar (a menu up)", false);
            Log("  the eyes, docked in the hangar (time running, nothing moving): straight, turned 90, recentred, headset off and back on");
            headYaw = 0; headPos = Vector3.zero; MockHead(0, Vector3.zero, true); yield return StickClick(); yield return new WaitForSecondsRealtime(.4f);
            yield return Pic("f91_vr_1_straight");
            headYaw = 0; headPos = Vector3.zero; yield return TurnHead(90, Vector3.zero); yield return new WaitForSecondsRealtime(.5f); yield return Pic("f91_vr_2_turned_90");
            yield return HoldPause(1.25f); yield return new WaitForSecondsRealtime(.4f); yield return Pic("f91_vr_3_recentred");
            MockHead(0, Vector3.zero, false); yield return new WaitForSecondsRealtime(1.2f); MockHead(220, new Vector3(.1f, 0, -.1f), true); yield return new WaitForSecondsRealtime(1.5f);
            yield return Pic("f91_vr_4_headset_back_on");
            CompareEyes("f91_vr_1_straight", new[] { "f91_vr_2_turned_90", "f91_vr_3_recentred", "f91_vr_4_headset_back_on" });

            Log($"  recentres in all: {G.Rig.Recentered}, automatic {G.Rig.AutoRecentered}; eye pairs {eyePairs}, failed {eyeFails}");
        }

        void CompareEyes(string refName, string[] names)
        {
            Texture2D L(string n) { var p = Path.Combine(dir, n + "_eyes.png"); if (!File.Exists(p)) return null; var t = new Texture2D(2, 2); t.LoadImage(File.ReadAllBytes(p)); var h = new Texture2D(t.width / 2, t.height, TextureFormat.RGB24, false); h.SetPixels(t.GetPixels(0, 0, t.width / 2, t.height)); h.Apply(); return h; }
            var r = L(refName); if (r == null) { Log("  (no reference eye picture)"); return; }
            foreach (var n in names)
            {
                var x = L(n); if (x == null) { Log($"  {n}: no picture"); continue; }
                var m = EyeMatch(r, x);
                Log($"  left eye of {n} vs straight ahead: correlation {m.corr:0.000} at {m.shift} px, difference {m.diff:0.000} -> {(m.corr > .9f && Mathf.Abs(m.shift) <= 3 ? "the same view" : "a different view")}");
            }
        }
    }
}
