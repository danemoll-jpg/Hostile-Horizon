using System;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.XR;
using UnityEngine.XR.Management;

namespace HostileHorizon
{
    // Fix 8.1 check, in the built exe, muted, no window:
    //   -exeCheckOnly fix81            flat: the pilot's body and hands looking down from the eye (at rest, full stick, full / no
    //                                  throttle, firing), the stick / lever angles, the floor window in flight and in hover, the strike
    //   -exeCheckOnly fix81 -vr        with the mock OpenXR runtime (run_check.py --env XR_RUNTIME_JSON=...): the same, plus every biome,
    //                                  shots, particles, the route trail, the hangar bay. VR pictures are the REAL eye images: both
    //                                  slices of the single-pass-instanced eye texture copied after the frame is drawn (Round 8's
    //                                  pictures drew each eye with a separate flat camera, which is why the blue right eye was missed),
    //                                  then compared pixel by pixel: one-colour share, contrast, and how well the two match.
    public sealed partial class ExeCheck
    {
        int eyeFails, eyePairs;

        IEnumerator Fix81()
        {
            StartCoroutine(Progress());
            yield return WaitFor(() => G.Now == GameShell.State.Title, 90f);
            yield return new WaitForSecondsRealtime(1.5f);
            bool vr = VrMode.Active;
            Log($"VR {(vr ? "on" : "off")}: {VrMode.Status}; {(vr ? $"{XRSettings.eyeTextureWidth} x {XRSettings.eyeTextureHeight} per eye, {XRSettings.stereoRenderingMode}" : "flat")}");
            if (vr) yield return Pic("f81_title");

            Log("--- the pilot's body (Free Flight, Canyons)");
            yield return StartFreeFlight(Biome.Canyons, 1); yield return Fly(3f);
            var pb = G.Body;
            if (!pb || !pb.Root) { Log("  NO BODY (the pilot model is missing)"); }
            else
            {
                var an = pb.Root.GetComponentInChildren<Animator>();
                Transform B(HumanBodyBones h) => an.GetBoneTransform(h);
                var ship = G.Ship.transform;
                string P(Transform t) => ship.InverseTransformPoint(t.position).ToString("0.00");
                Log($"  body: hips at {P(B(HumanBodyBones.Hips))}, head bone at {P(B(HumanBodyBones.Head))} (drawn at {B(HumanBodyBones.Head).lossyScale.x:0.0000} size: no head), " +
                    $"eye {ShipLayout.Eye.ToString("0.00")}; feet {P(B(HumanBodyBones.LeftFoot))} / {P(B(HumanBodyBones.RightFoot))}; knees {P(B(HumanBodyBones.LeftLowerLeg))} / {P(B(HumanBodyBones.RightLowerLeg))}");
                Log($"  hands: right {P(B(HumanBodyBones.RightHand))}, {Vector3.Distance(B(HumanBodyBones.RightHand).position, pb.Stick.TransformPoint(pb.StickGrip)):0.000} m from the stick's grip; " +
                    $"left {P(B(HumanBodyBones.LeftHand))}, {Vector3.Distance(B(HumanBodyBones.LeftHand).position, pb.Throttle.TransformPoint(pb.ThrottleGrip)):0.000} m from the throttle's grip");
                Log($"  stick pivot {pb.Stick.localPosition.ToString("0.000")} (right of the seat: {(pb.Stick.localPosition.x > 0 ? "yes" : "NO")}), throttle pivot {pb.Throttle.localPosition.ToString("0.000")} (left: {(pb.Throttle.localPosition.x < 0 ? "yes" : "NO")})");
                var cab = G.CabinRoom ? G.CabinRoom.Cam : null;
                if (cab)
                {
                    var planes = GeometryUtility.CalculateFrustumPlanes(cab); bool seen = pb.Renderers.Any(r => GeometryUtility.TestPlanesAABB(planes, r.bounds));
                    Log($"  cabin camera: the body is {(seen ? "in its view (drawn there: same layer)" : "out of its view (behind the bulkhead)")}; body layer {pb.Root.gameObject.layer}, cabin camera mask includes it {(cab.cullingMask & (1 << pb.Root.gameObject.layer)) != 0}");
                }
            }

            // looking down from the eye: at rest, full stick, throttle full / idle, firing
            CameraRig.CheckLook = new Vector2(0, -52f);
            yield return Fly(.6f);
            yield return Pic("f81_body_rest");
            yield return Posed("f81_body_stick_full_pull_right", 1, 1, 0, float.NaN, false);
            yield return Posed("f81_body_stick_push_left_twist", -1, -1, 1, float.NaN, false);
            yield return Posed("f81_body_throttle_full", 0, 0, 0, 1, false);
            yield return Posed("f81_body_throttle_idle", 0, 0, 0, 0, false);
            yield return Posed("f81_body_firing", 0, 0, 0, float.NaN, true);
            // live input: the virtual pad's stick pushed forward and right: the stick follows (sign check)
            float end = Time.realtimeSinceStartup + .8f; while (Time.realtimeSinceStartup < end) { state.leftStick = new Vector2(.8f, .8f); Set(); yield return null; }
            Log($"  live pad stick forward + right: Controls pitch {Controls.Pitch:0.00} roll {Controls.Roll:0.00}; the stick's top moved {(G.Body.Stick.up.ToString("0.00"))} in the ship's space {G.Ship.transform.InverseTransformDirection(G.Body.Stick.up).ToString("0.00")} (forward = +z, right = +x)");
            yield return Pic("f81_body_live_forward_right");
            state.leftStick = Vector2.zero; Set();
            CameraRig.CheckLook = new Vector2(-72, -48f); yield return Fly(.5f); yield return Pic("f81_body_left_hand");
            CameraRig.CheckLook = new Vector2(35, -40f); yield return Fly(.5f); yield return Pic("f81_body_right_hand");

            Log("--- the floor window");
            CameraRig.CheckLook = new Vector2(0, -68f); yield return Fly(.5f);
            Log($"  in flight: {Agl():0} m over the ground");
            yield return Pic("f81_window_flight");
            yield return Press(GamepadButton.West);   // landing mode: hover
            end = Time.realtimeSinceStartup + 9f;
            while (Time.realtimeSinceStartup < end && Agl() > 12f) { state.leftStick = Vector2.zero; state.rightTrigger = 0; state.leftTrigger = 1; Set(); yield return null; }
            state.leftTrigger = 0; Set(); yield return new WaitForSecondsRealtime(1f);
            Log($"  hovering: mode {G.Ship.Mode}, {Agl():0} m over the ground, {G.Ship.Speed * 3.6f:0} km/h");
            yield return Pic("f81_window_hover");
            CameraRig.CheckLook = null;

            Log("--- the strike (the creature still reaches your face)");
            var c = G.CheckStrikeCreature(AlienType.Knocker, 0);
            end = Time.realtimeSinceStartup + 1.6f; while (Time.realtimeSinceStartup < end) yield return null;
            yield return new WaitForEndOfFrame();
            var sv = new System.Collections.Generic.List<Vector3>(); c.Surface(sv); var head = G.Rig.Head.transform;
            Log($"  face {Vector3.Distance(c.HeadWorld, head.position):0.00} m from the eye, nearest part {sv.Min(v => (v - head.position).magnitude):0.00} m");
            yield return Pic("f81_strike");
            Destroy(c.gameObject);

            if (vr)
            {
                Log("--- both eyes, every biome (flying low; the gun firing for the second picture: shots, sparks)");
                foreach (Biome b in Enum.GetValues(typeof(Biome)))
                {
                    yield return ToTitle();
                    yield return StartFreeFlight(b, 1); yield return Fly(3f);
                    yield return Pic($"f81_{Biomes.Keys[(int)b]}_ahead");
                    end = Time.realtimeSinceStartup + 1.2f; while (Time.realtimeSinceStartup < end) { Autopilot(); state = state.WithButton(GamepadButton.South, true); Set(); yield return null; }
                    yield return Pic($"f81_{Biomes.Keys[(int)b]}_firing");
                    state = state.WithButton(GamepadButton.South, false); Set();
                    if (b == Biome.Ocean || b == Biome.Volcanic) { CameraRig.CheckLook = new Vector2(0, -25); yield return Fly(.5f); yield return Pic($"f81_{Biomes.Keys[(int)b]}_liquid"); CameraRig.CheckLook = null; }
                }
                Log("--- both eyes: the hangar bay, the launch, the route trail (Campaign, new career)");
                yield return ToTitle();
                Career.CheckSystemSeed = 7171;
                yield return ToCampaignMenu();
                yield return Nav(GamepadButton.DpadDown, 1); yield return Press(GamepadButton.South);   // New career
                yield return WaitPhase(() => G.Now == GameShell.State.Docked && G.Phase == "docked", 60f, "the hangar");
                yield return new WaitForSecondsRealtime(1.5f);
                yield return Pic("f81_hangar");
                G.Menu.Current.Selected = 0; yield return Press(GamepadButton.South);   // Launch
                yield return new WaitForSecondsRealtime(3f); yield return Pic("f81_launch");
                yield return WaitPhase(() => G.Now == GameShell.State.Flying, 40f, "flight after launch");
                yield return new WaitForSecondsRealtime(3f);
                Log($"  route trail: {(GameSettings.S.routeTrail ? "on" : "off")}");
                CameraRig.CheckLook = new Vector2(0, -12); yield return new WaitForSecondsRealtime(.5f);
                yield return Pic("f81_route_trail"); CameraRig.CheckLook = null;
                Log($"--- eye comparison: {eyePairs} pairs, {eyeFails} with a blank / one-colour / mismatched eye");
            }
        }

        float Agl() { var p = G.Ship.transform.position; return p.y - G.Ship.Field.SurfaceHeight(p.x, p.z); }

        IEnumerator ToTitle()
        {
            if (G.Now == GameShell.State.Flying) { G.Pause(); yield return new WaitForSecondsRealtime(.3f); G.Menu.Current.Selected = Row("Return to title"); yield return Press(GamepadButton.South); }
            yield return WaitFor(() => G.Now == GameShell.State.Title, 40f); yield return new WaitForSecondsRealtime(.8f);
        }

        IEnumerator Posed(string name, float pitch, float roll, float yaw, float lever, bool fire)
        {
            PilotBody.CheckPitch = pitch; PilotBody.CheckRoll = roll; PilotBody.CheckYaw = yaw; PilotBody.CheckLever = lever; PilotBody.CheckFire = fire;
            yield return Fly(.4f);
            var pb = G.Body;
            Log($"  {name}: stick pitch {pitch:0} roll {roll:0} yaw {yaw:0} -> pivot {Ang(pb.Stick.localEulerAngles)}; lever {pb.Lever:0.00} -> {Ang(pb.Throttle.localEulerAngles)}; index finger {(pb.IndexPulled ? "PULLING the trigger" : "resting")}; " +
                $"right hand {Vector3.Distance(Bone(HumanBodyBones.RightHand), pb.Stick.TransformPoint(pb.StickGrip)):0.000} m from the grip, left {Vector3.Distance(Bone(HumanBodyBones.LeftHand), pb.Throttle.TransformPoint(pb.ThrottleGrip)):0.000} m");
            yield return Pic(name);
            PilotBody.CheckPitch = PilotBody.CheckRoll = PilotBody.CheckYaw = PilotBody.CheckLever = float.NaN; PilotBody.CheckFire = false;
        }
        Vector3 Bone(HumanBodyBones h) => G.Body.Root.GetComponentInChildren<Animator>().GetBoneTransform(h).position;
        static string Ang(Vector3 e) => $"({Mathf.DeltaAngle(0, e.x):0}, {Mathf.DeltaAngle(0, e.y):0}, {Mathf.DeltaAngle(0, e.z):0}) deg";

        // a picture from the player's view: flat, the head camera; VR, both real eyes
        IEnumerator Pic(string name)
        {
            if (!VrMode.Active) { yield return Shot(name); yield break; }
            // a headless (batch-mode) player draws no camera by itself: the stereo head camera is drawn here, through URP's XR path into the
            // runtime's eye texture, and that texture is copied right after it is drawn (both slices)
            yield return new WaitForEndOfFrame();
            grabbed = null; RenderPipelineManager.endContextRendering += Grab;
            if (TestRun.Headless) { TestRun.DrawOverlays(G.Rig.Head); Canvas.ForceUpdateCanvases(); G.Rig.Head.Render(); }
            for (int k = 0; k < 30 && grabbed == null; k++) yield return null;
            RenderPipelineManager.endContextRendering -= Grab;
            if (grabbed == null) { Log($"  {name}: the eye texture was never drawn"); yield break; }
            var eyes = new Texture2D[2]; string how = grabHow;
            for (int e = 0; e < 2; e++)
            {
                var prev = RenderTexture.active; RenderTexture.active = grabbed[e];
                eyes[e] = new Texture2D(grabbed[e].width, grabbed[e].height, TextureFormat.RGB24, false); eyes[e].ReadPixels(new Rect(0, 0, grabbed[e].width, grabbed[e].height), 0, 0); Flip(eyes[e]);
                RenderTexture.active = prev; grabbed[e].Release(); Destroy(grabbed[e]);
            }
            // side by side: left | right
            int w = eyes[0].width, h = eyes[0].height;
            var both = new Texture2D(w * 2, h, TextureFormat.RGB24, false);
            both.SetPixels32(0, 0, w, h, eyes[0].GetPixels32()); both.SetPixels32(w, 0, w, h, eyes[1].GetPixels32()); both.Apply();
            File.WriteAllBytes(Path.Combine(dir, name + "_eyes.png"), both.EncodeToPNG());
            var sl = EyeStats(eyes[0]); var sr = EyeStats(eyes[1]); var m = EyeMatch(eyes[0], eyes[1]);
            bool ok = sl.seen > .3f && sr.seen > .3f && Mathf.Abs(sl.seen - sr.seen) < .1f && sl.one < .5f && sr.one < .5f && sl.sd > .03f && sr.sd > .03f && m.corr > .6f;
            eyePairs++; if (!ok) eyeFails++;
            Log($"  {name}_eyes.png ({how}): visible area: left {sl.seen:P0} mean {sl.mean:0.000} contrast {sl.sd:0.000} one-colour {sl.one:P0}; right {sr.seen:P0} mean {sr.mean:0.000} contrast {sr.sd:0.000} one-colour {sr.one:P0}; " +
                $"match: correlation {m.corr:0.000} at {m.shift} px shift, difference {m.diff:0.000} -> {(ok ? "both eyes drawn" : "EYES DIFFER (blank / one colour / mismatch)")}");
            Destroy(eyes[0]); Destroy(eyes[1]); Destroy(both);
        }

        static void Flip(Texture2D t)
        {
            var p = t.GetPixels32(); var q = new Color32[p.Length]; int w = t.width, h = t.height;
            for (int y = 0; y < h; y++) Array.Copy(p, y * w, q, (h - 1 - y) * w, w);
            t.SetPixels32(q); t.Apply();
        }
        RenderTexture[] grabbed; string grabHow;
        void Grab(ScriptableRenderContext ctx, List<Camera> cams)
        {
            if (!cams.Contains(G.Rig.Head)) return;
            RenderPipelineManager.endContextRendering -= Grab;
            var disp = XRGeneralSettings.Instance.Manager.activeLoader.GetLoadedSubsystem<XRDisplaySubsystem>();
            int passes = disp.GetRenderPassCount(); var got = new RenderTexture[2];
            for (int e = 0; e < 2; e++)
            {
                int pass = passes > 1 ? e : 0;
                disp.GetRenderPass(pass, out var rp);
                rp.GetRenderParameter(G.Rig.Head, passes > 1 ? 0 : e, out var prm);
                var src = disp.GetRenderTextureForRenderPass(pass);
                if (!src) return;
                var dst = new RenderTexture(src.width, src.height, 0, src.graphicsFormat); dst.Create();
                string via = "copy";
                try { Graphics.CopyTexture(src, prm.textureArraySlice, 0, dst, 0, 0); }
                catch (Exception) { Graphics.Blit(src, dst, prm.textureArraySlice, 0); via = "blit"; }
                got[e] = dst;
                grabHow = $"{passes} render pass(es), {src.width} x {src.height} x {src.volumeDepth} {src.dimension} {src.graphicsFormat}, eye {e} = slice {prm.textureArraySlice} ({via})";
            }
            grabbed = got;
        }

        // the headset's hidden-area mask (the corners the lenses can't show) is drawn in one colour: it is left out of the comparison
        static bool Masked(Color32 c, Color32 m) => Math.Abs(c.r - m.r) < 6 && Math.Abs(c.g - m.g) < 6 && Math.Abs(c.b - m.b) < 6;

        // luminance on a 160 x 90 grid (NaN: the hidden area)
        static float[] Small(Texture2D t, int W = 160, int H = 90)
        {
            var a = new float[W * H]; Color32 m = t.GetPixel(0, 0);
            for (int y = 0; y < H; y++) for (int x = 0; x < W; x++)
            {
                Color32 c = t.GetPixel((int)((x + .5f) / W * t.width), (int)((y + .5f) / H * t.height));
                a[y * W + x] = Masked(c, m) ? float.NaN : (.3f * c.r + .59f * c.g + .11f * c.b) / 255f;
            }
            return a;
        }
        // in the visible area: mean, contrast, the share of its most common colour, and how much of the image is visible
        static (float mean, float sd, float one, float seen) EyeStats(Texture2D t)
        {
            var px = t.GetPixels32(); Color32 m = px[0]; var count = new Dictionary<int, int>(); float s = 0, s2 = 0; int N = 0, all = 0;
            for (int i = 0; i < px.Length; i += 7)
            {
                var c = px[i]; all++; if (Masked(c, m)) continue; N++;
                int k = (c.r >> 3) << 10 | (c.g >> 3) << 5 | (c.b >> 3); count[k] = count.TryGetValue(k, out var n) ? n + 1 : 1;
                float l = (.3f * c.r + .59f * c.g + .11f * c.b) / 255f; s += l; s2 += l * l;
            }
            if (N == 0) return (0, 0, 1, 0);
            float mean = s / N;
            return (mean, Mathf.Sqrt(Mathf.Max(0, s2 / N - mean * mean)), count.Values.Max() / (float)N, N / (float)all);
        }
        // the best match over a sideways shift (the eyes are 64 mm apart and the near cockpit shifts a lot), visible area only
        static (float corr, int shift, float diff) EyeMatch(Texture2D l, Texture2D r)
        {
            const int W = 160, H = 90; var a = Small(l); var b = Small(r);
            float best = -2; int bs = 0; float bd = 1;
            for (int s = -45; s <= 45; s++)
            {
                double sa = 0, sb = 0, sab = 0, saa = 0, sbb = 0, d = 0; int n = 0;
                for (int y = 0; y < H; y++) for (int x = 0; x < W; x++)
                {
                    int x2 = x + s; if (x2 < 0 || x2 >= W) continue;
                    float u = a[y * W + x], v = b[y * W + x2]; if (float.IsNaN(u) || float.IsNaN(v)) continue;
                    sa += u; sb += v; sab += u * v; saa += u * u; sbb += v * v; d += Math.Abs(u - v); n++;
                }
                if (n < W * H / 5) continue;
                double cov = sab / n - sa / n * sb / n, va = saa / n - sa / n * sa / n, vb = sbb / n - sb / n * sb / n;
                float c = va < 1e-6 || vb < 1e-6 ? 0 : (float)(cov / Math.Sqrt(va * vb));
                if (c > best) { best = c; bs = s; bd = (float)(d / n); }
            }
            return (best, bs, bd);
        }
    }
}
