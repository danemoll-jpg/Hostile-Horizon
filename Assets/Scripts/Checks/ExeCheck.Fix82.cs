using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

namespace HostileHorizon
{
    // Fix 8.2 check, in the built exe, muted, no window:
    //   -exeCheckOnly fix82        flat: (1) the hands: 0.8.1's pose for 20 s, then the new one for 60 s of flying (stick, throttle, looking
    //                              round, a hover), every frame logged (hands_old.csv / hands_new.csv): where each hand sits on its own
    //                              control, finger turn per frame, the chest's turn, the shoulders; (2) the stick and throttle depths;
    //                              (3) docking with pilots aboard, 0.8.1's and now: cabin figures, cabin camera, [Play] lines, after
    //                              re-launch; (5) the Field Journal's left / right; (6, 7) the cockpit's material slots, the right
    //                              console, the windshield and canopy frame from the eye
    //   -exeCheckOnly fix82 -vr    the same on the mock OpenXR runtime (both real eyes), plus (4) every menu page as a board: pause,
    //                              Settings, the Field Journal with its 3D alien, the hangar, the star map; and how much of the board the
    //                              cockpit hid with 0.8.1's board vs now (the board's backdrop painted one colour, its pixels counted)
    public sealed partial class ExeCheck
    {
        IEnumerator Fix82()
        {
            StartCoroutine(Progress());
            yield return WaitFor(() => G.Now == GameShell.State.Title, 90f);
            yield return new WaitForSecondsRealtime(1.5f);
            bool vr = VrMode.Active;
            Log($"VR {(vr ? "on" : "off")}: {VrMode.Status}");

            // ---- 6: material slots of the ship (a slot left to the importer shows plain white)
            Log("--- the cockpit's materials (item 6)");
            yield return StartFreeFlight(Biome.Canyons, 1); yield return Fly(2f);
            foreach (var r in G.Ship.GetComponentsInChildren<Renderer>(true).Where(r => r.name.Contains("__")))
            {
                var ms = r.sharedMaterials;
                string names = string.Join(", ", ms.Select(m => m ? m.name : "none").Distinct());
                bool tex = ms.All(m => m && (!m.HasProperty("_BaseMap") || m.GetTexture("_BaseMap") != null));
                if (r.name.Contains("mx_") || ms.Length > 1) Log($"  {r.name}: {ms.Length} slot(s): {names}; textured {(tex ? "yes" : "NO")}");
            }

            // ---- 2: stick and throttle depth
            var pb = G.Body;
            Log("--- the stick and the throttle (item 2)");
            Log($"  stick pivot {pb.Stick.localPosition.ToString("0.000")}, throttle pivot {pb.Throttle.localPosition.ToString("0.000")}: depth difference {(pb.Stick.localPosition.z - pb.Throttle.localPosition.z) * 100:0.0} cm (0.8.1: 17.8 cm); ShipLayout {ShipLayout.StickPivot.ToString("0.000")} / {ShipLayout.ThrottlePivot.ToString("0.000")}");
            if (pb.Root)
            {
                var sh = G.Ship.transform;
                Vector3 S(Vector3 w) => sh.InverseTransformPoint(w);
                Log($"  right hand {S(Bone(HumanBodyBones.RightHand)).ToString("0.00")}, left hand {S(Bone(HumanBodyBones.LeftHand)).ToString("0.00")}; shoulder to hand: right {Vector3.Distance(Bone(HumanBodyBones.RightUpperArm), Bone(HumanBodyBones.RightHand)):0.00} m, left {Vector3.Distance(Bone(HumanBodyBones.LeftUpperArm), Bone(HumanBodyBones.LeftHand)):0.00} m");
            }
            CameraRig.CheckLook = new Vector2(0, -52f); yield return Fly(.6f); yield return Pic("f82_hands_rest");
            CameraRig.CheckLook = new Vector2(-40, -45f); yield return Fly(.5f); yield return Pic("f82_throttle_left");
            CameraRig.CheckLook = null;

            // ---- 1: the hands, 0.8.1's pose then the new one
            Log("--- the hands (item 1): every frame, hand on its control (mm), finger turn per frame, chest turn, shoulders");
            PilotBody.CheckOldPose = true;
            yield return HandRun("old", 20f);
            PilotBody.CheckOldPose = false;
            yield return HandRun("new", 60f);
            CameraRig.CheckLook = null;

            // ---- 7: windshield and the canopy frame from the eye; 6: the right console
            Log("--- the windshield, the canopy frame, the right console (items 6, 7)");
            yield return ToTitle(); yield return StartFreeFlight(Biome.Ice, 1); yield return Fly(2f);
            foreach (var (n, look) in new[] { ("ahead", new Vector2(0, 0)), ("windshield_top_left", new Vector2(-28, 14)), ("canopy_left", new Vector2(-70, 10)), ("canopy_right", new Vector2(70, 10)),
                                              ("canopy_up", new Vector2(0, 60)), ("canopy_up_left", new Vector2(-45, 45)), ("canopy_up_right", new Vector2(45, 45)), ("console_right", new Vector2(75, -35)), ("console_left", new Vector2(-75, -35)) })
            { CameraRig.CheckLook = look; yield return Fly(.5f); yield return Pic("f82_" + n); }
            CameraRig.CheckLook = null;

            // ---- 3: docking with pilots aboard (0.8.1's and now); 5: the journal; 4 (VR): the menu pages
            yield return ToTitle();
            Career.CheckSystemSeed = 8282;
            yield return ToCampaignMenu();
            yield return Nav(GamepadButton.DpadDown, 1); yield return Press(GamepadButton.South);   // New career
            yield return WaitPhase(() => G.Now == GameShell.State.Docked && G.Phase == "docked", 60f, "the hangar");
            Log("--- docking with pilots aboard (item 3)");
            GameShell.CheckOldDock = true; yield return DockRun("old");
            GameShell.CheckOldDock = false; yield return DockRun("new");

            Log("--- the Field Journal: left / right on a kind's page (item 5)");
            var c = Career.C; c.Meet(AlienType.Mimic, G.Planet.name); c.Meet(AlienType.Knocker, G.Planet.name); c.Meet(AlienType.Sleeper, G.Planet.name);
            G.Menu.Current.Selected = Row("Field Journal"); yield return Press(GamepadButton.South); yield return new WaitForSecondsRealtime(.4f);
            G.Menu.Current.Selected = 0; yield return Press(GamepadButton.South); yield return new WaitForSecondsRealtime(.8f);   // Mimic's page
            string Page() => $"\"{G.Menu.Current.Title}\" ({G.Menu.Current.Subtitle}), row \"{Selected()}\"";
            Log($"  opened: {Page()}");
            yield return Pic("f82_journal_mimic");
            foreach (var (b, n) in new[] { (GamepadButton.DpadRight, "D-pad right"), (GamepadButton.DpadRight, "D-pad right"), (GamepadButton.DpadLeft, "D-pad left"), (GamepadButton.DpadLeft, "D-pad left"), (GamepadButton.DpadLeft, "D-pad left") })
            { yield return Press(b); yield return new WaitForSecondsRealtime(.3f); Log($"  {n}: {Page()}"); }
            yield return Nav(GamepadButton.DpadDown, 1); yield return Press(GamepadButton.DpadRight); yield return new WaitForSecondsRealtime(.3f);
            Log($"  on the second row, D-pad right: {Page()}");
            // the left stick sideways (menus) and the right stick (turn)
            float yaw0 = G.ViewYaw; float end = Time.realtimeSinceStartup + .6f;
            while (Time.realtimeSinceStartup < end) { state.leftStick = new Vector2(1, 0); Set(); yield return null; }
            state.leftStick = Vector2.zero; Set(); yield return new WaitForSecondsRealtime(.3f);
            Log($"  left stick right: {Page()}; the view turned {Mathf.DeltaAngle(yaw0, G.ViewYaw):0} deg (0: it changes the kind only)");
            yaw0 = G.ViewYaw; end = Time.realtimeSinceStartup + .6f;
            while (Time.realtimeSinceStartup < end) { state.rightStick = new Vector2(1, 0); Set(); yield return null; }
            state.rightStick = Vector2.zero; Set(); yield return new WaitForSecondsRealtime(.2f);
            Log($"  right stick right: {Page()}; the view turned {Mathf.DeltaAngle(yaw0, G.ViewYaw):0} deg");
            yield return Pic("f82_journal_after");
            yield return Press(GamepadButton.East); yield return new WaitForSecondsRealtime(.3f);
            Log($"  B: back to \"{G.Menu.Current.Title}\"");
            yield return Press(GamepadButton.East); yield return new WaitForSecondsRealtime(.3f);

            if (vr)
            {
                Log("--- menus in VR (item 4): every page as a board, both eyes; the board's share the cockpit hid (0.8.1's board vs now)");
                yield return Board("hangar");
                G.Menu.Current.Selected = Row("Star map"); yield return Press(GamepadButton.South); yield return new WaitForSecondsRealtime(1.2f);
                yield return Board("star_map");
                yield return Press(GamepadButton.East); yield return new WaitForSecondsRealtime(.4f);
                G.Menu.Current.Selected = 0; yield return Press(GamepadButton.South);   // Launch
                yield return WaitPhase(() => G.Now == GameShell.State.Flying, 40f, "flight after launch"); yield return new WaitForSecondsRealtime(2f);
                G.Pause(); yield return new WaitForSecondsRealtime(.4f);
                yield return Board("pause");
                G.Menu.Current.Selected = Row("Settings"); yield return Press(GamepadButton.South); yield return new WaitForSecondsRealtime(.4f);
                yield return Board("settings");
                yield return Press(GamepadButton.East); yield return new WaitForSecondsRealtime(.4f);
                G.Menu.Current.Selected = Row("Field Journal"); yield return Press(GamepadButton.South); yield return new WaitForSecondsRealtime(.4f);
                G.Menu.Current.Selected = 0; yield return Press(GamepadButton.South); yield return new WaitForSecondsRealtime(1f);
                yield return Board("journal_alien");
                // looking down (the mock headset can't turn: the tracking space is tilted): the board opens where you look
                CameraRig.CheckLook = new Vector2(0, -30); yield return new WaitForSecondsRealtime(.3f);
                yield return Press(GamepadButton.DpadRight); yield return new WaitForSecondsRealtime(.8f);
                yield return Board("journal_looking_down");
                CameraRig.CheckLook = null;
                Log($"--- eye comparison: {eyePairs} pairs, {eyeFails} with a blank / one-colour / mismatched eye");
            }
        }

        // ---- 1: a run of flying with the stick, throttle, looking round and a hover; every frame logged
        IEnumerator HandRun(string tag, float seconds)
        {
            yield return ToTitle(); yield return StartFreeFlight(Biome.Canyons, 1); yield return Fly(2f);
            var pb = G.Body; if (!pb.Root) { Log("  no body"); yield break; }
            var an = pb.Root.GetComponentInChildren<Animator>(); var sh = G.Ship.transform;
            Transform B(HumanBodyBones h) => an.GetBoneTransform(h);
            Transform rh = B(HumanBodyBones.RightHand), lh = B(HumanBodyBones.LeftHand), ri = B(HumanBodyBones.RightIndexProximal), chest = B(HumanBodyBones.Chest) ?? B(HumanBodyBones.Spine);
            Transform rs = B(HumanBodyBones.RightUpperArm), ls = B(HumanBodyBones.LeftUpperArm);
            var csv = new StringBuilder("t,speed_kmh,look_yaw,look_pitch,pitch,roll,yaw,lever,rh_x,rh_y,rh_z,lh_x,lh_y,lh_z,rh_on_stick_mm,lh_on_throttle_mm,stick_grip_moved_mm,finger_turn_deg,chest_yaw_deg,shoulders_moved_mm\n");
            Vector3? rhRef = null, lhRef = null, sgRef = null, rsRef = null, lsRef = null; Quaternion? lastIdx = null;
            float maxR = 0, maxL = 0, maxGrip = 0, maxFinger = 0, maxChest = 0, maxShoulder = 0, sumFinger = 0; int frames = 0;
            float t0 = Time.realtimeSinceStartup; bool hovered = false; int pics = 0;
            while (Time.realtimeSinceStartup - t0 < seconds)
            {
                float t = Time.realtimeSinceStartup - t0;
                if (G.Now != GameShell.State.Flying) { yield return null; continue; }
                // the stick: the autopilot's, plus a slow sweep; the throttle up and down; the head looking round
                Autopilot();
                state.leftStick += new Vector2(Mathf.Sin(t * 1.3f) * .35f, Mathf.Sin(t * .9f) * .25f);
                state.rightTrigger = Mathf.Clamp01(Mathf.Sin(t * .4f)); state.leftTrigger = Mathf.Clamp01(-Mathf.Sin(t * .4f));
                if (t > seconds * .6f && !hovered) { hovered = true; state = state.WithButton(GamepadButton.West, true); Set(); yield return null; state = state.WithButton(GamepadButton.West, false); }
                if (hovered) { state.leftStick *= .3f; state.rightTrigger = 0; state.leftTrigger = G.Ship.AboveGround > 25f ? .6f : 0; }
                Set();
                CameraRig.CheckLook = new Vector2(Mathf.Sin(t * .5f) * 60f, -25f + Mathf.Sin(t * .35f) * 25f);
                yield return new WaitForEndOfFrame();
                // in each control's own frame: the hand should sit still on it
                var rOn = pb.Stick.InverseTransformPoint(rh.position); var lOn = pb.Throttle.InverseTransformPoint(lh.position);
                var sg = sh.InverseTransformPoint(pb.Stick.TransformPoint(pb.StickGrip));
                var rsS = sh.InverseTransformPoint(rs.position); var lsS = sh.InverseTransformPoint(ls.position);
                rhRef ??= rOn; lhRef ??= lOn; sgRef ??= sg; rsRef ??= rsS; lsRef ??= lsS;
                float dr = (rOn - rhRef.Value).magnitude * 1000, dl = (lOn - lhRef.Value).magnitude * 1000, dg = (sg - sgRef.Value).magnitude * 1000;
                float ds = Mathf.Max((rsS - rsRef.Value).magnitude, (lsS - lsRef.Value).magnitude) * 1000;
                var idx = Quaternion.Inverse(rh.rotation) * ri.rotation; float fturn = lastIdx.HasValue ? Quaternion.Angle(lastIdx.Value, idx) : 0; lastIdx = idx;
                if (pb.IndexPulled) fturn = 0;   // firing moves it on purpose (not used here)
                float cy = Vector3.SignedAngle(Vector3.ProjectOnPlane(sh.forward, sh.up), Vector3.ProjectOnPlane(chest.forward, sh.up), sh.up);
                maxR = Mathf.Max(maxR, dr); maxL = Mathf.Max(maxL, dl); maxGrip = Mathf.Max(maxGrip, dg); maxFinger = Mathf.Max(maxFinger, fturn); sumFinger += fturn;
                maxChest = Mathf.Max(maxChest, Mathf.Abs(cy)); maxShoulder = Mathf.Max(maxShoulder, ds); frames++;
                var rS = sh.InverseTransformPoint(rh.position); var lS = sh.InverseTransformPoint(lh.position); var lk = CameraRig.CheckLook ?? Vector2.zero;
                csv.AppendLine(string.Join(",", new[] { t, G.Ship.Speed * 3.6f, lk.x, lk.y, Controls.Pitch, Controls.Roll, Controls.Yaw, pb.Lever, rS.x, rS.y, rS.z, lS.x, lS.y, lS.z, dr, dl, dg, fturn, cy, ds }.Select(v => v.ToString("0.####", CultureInfo.InvariantCulture))));
                if (tag == "new" && pics < 3 && t > (pics + 1) * seconds / 4f) { pics++; CameraRig.CheckLook = new Vector2(0, -52); yield return Fly(.15f); yield return Pic($"f82_hands_{tag}_{pics}"); }
                yield return null;
            }
            state = default; Set(); CameraRig.CheckLook = null;
            File.WriteAllText(Path.Combine(dir, $"hands_{tag}.csv"), csv.ToString());
            Log($"  {tag} ({(tag == "old" ? "0.8.1's pose" : "Fix 8.2")}), {seconds:0} s, {frames} frames (hands_{tag}.csv): right hand moved on the stick up to {maxR:0.0} mm, left hand on the throttle up to {maxL:0.0} mm " +
                $"(the stick's grip itself moved up to {maxGrip:0} mm in the ship); index finger turned up to {maxFinger:0.0} deg in one frame ({sumFinger / Mathf.Max(1, frames):0.00} deg a frame on average, no firing); " +
                $"chest turned up to {maxChest:0.0} deg from the ship's heading; shoulders moved up to {maxShoulder:0} mm");
        }

        // ---- 3: board pilots, dock, look at the cabin; then launch again
        IEnumerator DockRun(string tag)
        {
            G.Menu.Current.Selected = 0; yield return Press(GamepadButton.South);   // Launch
            yield return WaitPhase(() => G.Now == GameShell.State.Flying, 40f, "flight after launch");
            yield return new WaitForSecondsRealtime(1f);
            foreach (var q in G.Pilots.Where(x => x.OnFoot && G.Planet.alien[x.Index] == 0).OrderBy(x => x.Index).Take(2).ToList()) G.CheckBoard(q);
            Log($"  {tag}: put aboard {G.CabinCount}; cabin figures shown {G.CabinRoom.FiguresShown}");
            int pl = PlayLog.Lines.Count;
            yield return Recall();   // the recall: the mothership's beam lifts the ship into the bay (the same docking and debrief as a climb)
            yield return new WaitForSecondsRealtime(1f);
            Log($"  {tag} ({(tag == "old" ? "0.8.1's docking" : "Fix 8.2")}), in the hangar: cabin count {G.CabinCount}, cabin figures shown {G.CabinRoom.FiguresShown}, cabin camera {(G.CabinRoom.FeedOn ? "on" : "off")}");
            foreach (var l in PlayLog.Lines.Skip(pl).Where(l => l.Contains("dock") || l.Contains("deliver") || l.Contains("leave"))) Log($"    {l}");
            yield return CabinPic($"f82_cabin_{tag}_hangar");
            G.Menu.Current.Selected = 0; yield return Press(GamepadButton.South);   // launch again
            if (G.Menu.Current != null && G.Menu.Current.Title != null && G.Menu.Current.Rows.Count > 0 && G.Now == GameShell.State.Docked) { G.Menu.Current.Selected = 0; yield return Press(GamepadButton.South); }   // the low-energy page: refuel and launch
            yield return WaitPhase(() => G.Now == GameShell.State.Flying, 40f, "flight after the second launch");
            yield return new WaitForSecondsRealtime(1f);
            Log($"  {tag}: after the next launch: cabin count {G.CabinCount}, cabin figures shown {G.CabinRoom.FiguresShown}, cabin camera {(G.CabinRoom.FeedOn ? "on" : "off")}");
            yield return CabinPic($"f82_cabin_{tag}_relaunch");
            yield return Recall();   // home again
            yield return new WaitForSecondsRealtime(.8f);
        }

        IEnumerator Recall()
        {
            state = state.WithButton(GamepadButton.Select, true); Set(); yield return new WaitForSecondsRealtime(1.4f); state = state.WithButton(GamepadButton.Select, false); Set();
            yield return WaitPhase(() => G.Phase == "docked", 300f, "docked after the recall");
        }

        // the cabin camera's picture (drawn now, whatever the feed shows)
        IEnumerator CabinPic(string name)
        {
            yield return new WaitForEndOfFrame();
            var cam = G.CabinRoom.Cam; cam.Render();
            var prev = RenderTexture.active; RenderTexture.active = G.CabinRoom.Feed;
            var t = new Texture2D(G.CabinRoom.Feed.width, G.CabinRoom.Feed.height, TextureFormat.RGB24, false); t.ReadPixels(new Rect(0, 0, t.width, t.height), 0, 0); t.Apply();
            RenderTexture.active = prev;
            File.WriteAllBytes(Path.Combine(dir, name + ".png"), t.EncodeToPNG()); Destroy(t);
            Log($"  cabin camera picture {name}.png");
        }

        // ---- 4: a page as a board, both eyes; then its backdrop in one colour: its pixels with 0.8.1's board (depth-tested, and on top
        // at the same place: the difference is what the cockpit hid) and now
        IEnumerator Board(string name)
        {
            yield return Pic($"f82_vr_{name}");
            var bd = G.Menu.Backdrop; var col = bd.color;
            var hide = G.Menu.Root.GetComponentsInChildren<UnityEngine.UI.Graphic>(true).Where(g => g != bd && g.enabled).ToList();
            foreach (var g in hide) g.enabled = false;
            bd.color = new Color(1, 0, 1, 1);
            var counts = new int[3];
            foreach (int mode in new[] { 1, 2, 0 })
            {
                MenuPanel.CheckVrBoard = mode; G.Menu.CheckPlaceAgain(); bd.color = new Color(1, 0, 1, 1);   // after placing (it sets the backdrop)
                yield return EyeCount(n => counts[mode] = n);
            }
            MenuPanel.CheckVrBoard = 0; G.Menu.CheckPlaceAgain();
            bd.color = col; foreach (var g in hide) g.enabled = true;
            float hid = counts[2] > 0 ? 1 - counts[1] / (float)counts[2] : 0;
            Log($"  {name}: board pixels (both eyes, backdrop painted): 0.8.1's board {counts[1]} of {counts[2]} -> {hid:P0} hidden by the cockpit; now {counts[0]} (drawn over everything: none hidden)");
            yield return Pic($"f82_vr_{name}_after");
        }

        IEnumerator EyeCount(Action<int> got)
        {
            yield return new WaitForEndOfFrame();
            grabbed = null; UnityEngine.Rendering.RenderPipelineManager.endContextRendering += Grab;
            if (TestRun.Headless) { TestRun.DrawOverlays(G.Rig.Head); Canvas.ForceUpdateCanvases(); G.Rig.Head.Render(); }
            for (int k = 0; k < 30 && grabbed == null; k++) yield return null;
            UnityEngine.Rendering.RenderPipelineManager.endContextRendering -= Grab;
            if (grabbed == null) { got(-1); yield break; }
            int n = 0;
            for (int e = 0; e < 2; e++)
            {
                var prev = RenderTexture.active; RenderTexture.active = grabbed[e];
                var t = new Texture2D(grabbed[e].width, grabbed[e].height, TextureFormat.RGB24, false); t.ReadPixels(new Rect(0, 0, t.width, t.height), 0, 0);
                RenderTexture.active = prev; grabbed[e].Release(); Destroy(grabbed[e]);
                foreach (var p in t.GetPixels32()) if (p.r > 200 && p.g < 60 && p.b > 200) n++;
                Destroy(t);
            }
            got(n);
        }
    }
}
