using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.XR;
using UnityEngine.XR.Management;

namespace HostileHorizon
{
    // Round 8 Part C check (VR), in the built exe, muted, no window (Code can't wear the headset):
    //   -exeCheckOnly round8c                   no -vr: the game is flat and OpenXR was never started
    //   -exeCheckOnly round8c -vr  (no runtime) VR asked for, no OpenXR runtime: it says so and starts flat, without waiting
    //   -exeCheckOnly round8c -vr  with XR_RUNTIME_JSON = the OpenXR package's mock runtime (Tools/run_check.py --env):
    //     VR starts; the head follows the headset 1:1 (the mock runtime's view pose moved, through its test API); Recenter; menus on
    //     the board; no screen overlay; the VR strip; the strike and the canopy creature in the left and right eyes' pictures; frame
    //     times with the VR quality preset in every biome.
    public sealed partial class ExeCheck
    {
        [DllImport("mock_api", EntryPoint = "MockRuntime_SetView")] static extern void MockSetView(int viewConfigurationType, int viewIndex, Vector3 position, Quaternion orientation, Vector4 fov);
        [DllImport("mock_api", EntryPoint = "MockRuntime_SetReferenceSpace")] static extern void MockSetSpace(int referenceSpace, Vector3 position, Quaternion orientation, ulong locationFlags);

        // what one eye sees: a check camera at the eye with that eye's projection, drawn into the check's off-screen view
        void EyeShot(Camera.StereoscopicEye eye, string name)
        {
            var head = G.Rig.Head; float ipd = .064f;
            var dev = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            if (dev.isValid && dev.TryGetFeatureValue(CommonUsages.leftEyePosition, out var l) && dev.TryGetFeatureValue(CommonUsages.rightEyePosition, out var r) && (r - l).magnitude > .03f) ipd = (r - l).magnitude;
            var cam = new GameObject("Eye camera").AddComponent<Camera>(); cam.CopyFrom(head); cam.stereoTargetEye = StereoTargetEyeMask.None;
            cam.transform.SetPositionAndRotation(head.transform.position + head.transform.right * (eye == Camera.StereoscopicEye.Left ? -ipd / 2 : ipd / 2), head.transform.rotation);
            cam.ResetProjectionMatrix(); cam.fieldOfView = head.fieldOfView;   // the mock's per-eye projection is a placeholder: the head's own field of view
            cam.targetTexture = TestRun.View; SaveCam(cam, name); Destroy(cam.gameObject);
        }

        IEnumerator Round8C()
        {
            yield return WaitFor(() => G.Now == GameShell.State.Title, 90f);
            yield return new WaitForSecondsRealtime(1f);
            var mgr = XRGeneralSettings.Instance ? XRGeneralSettings.Instance.Manager : null;
            Log($"VR: asked {VrMode.Asked} (-vr {Environment.GetCommandLineArgs().Contains("-vr")}, setting {GameSettings.S.vrMode}), Steam Deck {SteamDeck.On}; status \"{VrMode.Status}\"; " +
                $"XR loader {(mgr == null ? "no XR settings" : mgr.activeLoader ? mgr.activeLoader.name : "none started")}, start with the game {(XRGeneralSettings.Instance ? XRGeneralSettings.Instance.InitManagerOnStart.ToString() : "?")}; XR device active {XRSettings.isDeviceActive}; " +
                $"title reached {Time.realtimeSinceStartup:0.0} s after start; runtime json {Environment.GetEnvironmentVariable("XR_RUNTIME_JSON") ?? "(the system's)"}");
            if (!VrMode.Active) { Log($"flat screen: HUD overlay {(G.Displays.OverlayShown ? "shown" : "hidden")} in flight as always; menus {G.Menu.Canvas.renderMode}"); yield break; }

            var head = G.Rig.Head.transform;
            Log($"VR on: {XRSettings.eyeTextureWidth} x {XRSettings.eyeTextureHeight} per eye, {XRSettings.stereoRenderingMode}, refresh {VrMode.RefreshRate:0} Hz, render scale {WorldLook.VrRenderScale}; head camera stereo {G.Rig.Head.stereoEnabled}, target eye {G.Rig.Head.stereoTargetEye}");
            Log($"title menu: canvas {G.Menu.Canvas.renderMode}, {Vector3.Distance(G.Menu.Root.position, head.position):0.00} m from the head, {G.Menu.Root.lossyScale.x * 1920:0.00} m wide");
            EyeShot(Camera.StereoscopicEye.Left, "r8c_title_left"); EyeShot(Camera.StereoscopicEye.Right, "r8c_title_right");

            // Free Flight: the cockpit, the head moved by the (mock) headset, Recenter
            yield return StartFreeFlight(Biome.Canyons, 1);
            yield return new WaitForSecondsRealtime(2.5f); yield return new WaitForEndOfFrame();
            Log($"flight: view {G.Rig.Current} (VR has the cockpit only; the view button: {(G.Rig.Current == CameraRig.Mode.Cockpit ? "stays" : "MOVED")}), HUD overlay {(G.Displays.OverlayShown ? "SHOWN" : "hidden")}, VR strip {(G.Ship.transform.Find("VR strip") is Transform vs && vs.gameObject.activeSelf ? "shown" : "NOT shown")}, recentred {G.Rig.Recentered} time(s); head from the ship's eye point {Vector3.Distance(head.position, G.Ship.transform.TransformPoint(ShipLayout.Eye)):0.000} m");
            G.Rig.Cycle(); yield return null;
            Log($"  the view button in VR: view {G.Rig.Current}");
            var p0 = head.localPosition; var r0 = head.localRotation; bool moved = false; string why = "";
            try
            {
                var at = new Vector3(.12f, .05f, .08f); var turn = Quaternion.Euler(-15, 40, 0); var fov = new Vector4(-.8f, .8f, .8f, -.8f);
                for (int v = 0; v < 2; v++) MockSetView(2, v, at + new Vector3(v == 0 ? -.032f : .032f, 0, 0), turn, fov);   // 2 = XR_VIEW_CONFIGURATION_TYPE_PRIMARY_STEREO
                MockSetSpace(1, at, turn, 15);   // the VIEW space (the head pose Unity reads): 1 = XR_REFERENCE_SPACE_TYPE_VIEW; flags = position / orientation valid + tracked
                moved = true;
            }
            catch (Exception e) { why = e.GetType().Name + ": " + e.Message; }
            yield return new WaitForSecondsRealtime(.6f);
            var p1 = head.localPosition; var r1 = head.localRotation;
            Log($"head moved by the mock headset: {(moved ? "view pose set to (0.12, 0.05, 0.08) m, 40 deg right, 15 deg up" : "the mock's test API not reachable (" + why + ")")}; " +
                $"head camera moved {(p1 - p0).ToString("0.000")} m (local), turned {Quaternion.Angle(r0, r1):0.0} deg; world move {Vector3.Distance(G.Rig.transform.TransformPoint(p1), G.Rig.transform.TransformPoint(p0)):0.000} m");
            EyeShot(Camera.StereoscopicEye.Left, "r8c_cockpit_turned_left"); EyeShot(Camera.StereoscopicEye.Right, "r8c_cockpit_turned_right");
            G.Rig.Recenter(); yield return null; yield return null; yield return new WaitForEndOfFrame();   // measured after the rig follows the ship
            Log($"Recenter: head now {Vector3.Distance(head.position, G.Ship.transform.TransformPoint(ShipLayout.Eye)):0.000} m from the eye point, {Vector3.Angle(Vector3.ProjectOnPlane(head.forward, G.Ship.transform.up), G.Ship.transform.forward):0.0} deg from straight ahead");
            EyeShot(Camera.StereoscopicEye.Left, "r8c_cockpit_left"); EyeShot(Camera.StereoscopicEye.Right, "r8c_cockpit_right");
            // the pause menu on the board
            G.Pause(); yield return new WaitForSecondsRealtime(.4f);
            Log($"pause menu: canvas {G.Menu.Canvas.renderMode}, {Vector3.Distance(G.Menu.Root.position, head.position):0.00} m ahead; rows: {string.Join(" | ", G.Menu.Current.Rows.Where(r => r.Visible == null || r.Visible()).Select(r => r.Label()))}");
            EyeShot(Camera.StereoscopicEye.Left, "r8c_pause_left"); EyeShot(Camera.StereoscopicEye.Right, "r8c_pause_right");
            G.Resume(); yield return new WaitForSecondsRealtime(.4f);
            // the strike, held in front of you (both eyes)
            var c = G.CheckStrikeCreature(AlienType.Knocker, 0);
            float end = Time.realtimeSinceStartup + 1.6f; while (Time.realtimeSinceStartup < end) { Autopilot(); yield return null; }
            yield return new WaitForEndOfFrame();
            var sv = new List<Vector3>(); c.Surface(sv); float near = sv.Min(v => (v - head.position).magnitude);
            Log($"strike staged in VR: face {Vector3.Distance(c.HeadWorld, head.position):0.00} m from the head, nearest part {near:0.00} m (0.42 m kept)");
            EyeShot(Camera.StereoscopicEye.Left, "r8c_strike_left"); EyeShot(Camera.StereoscopicEye.Right, "r8c_strike_right");
            Destroy(c.gameObject);
            // frame times with the VR quality preset, every biome
            Log("--- frame times in VR (mock runtime, both eyes drawn; VR quality preset), Free Flight, flying low");
            foreach (Biome b in Enum.GetValues(typeof(Biome)))
            {
                if (G.Now == GameShell.State.Flying) { G.Pause(); yield return new WaitForSecondsRealtime(.3f); G.Menu.Current.Selected = Row("Return to title"); yield return Press(GamepadButton.South); }
                yield return WaitFor(() => G.Now == GameShell.State.Title, 40f); yield return new WaitForSecondsRealtime(.8f);
                yield return StartFreeFlight(b, 1); yield return Fly(3f);
                var times = new List<float>(); float e2 = Time.realtimeSinceStartup + 10f;
                while (Time.realtimeSinceStartup < e2 && G.Now == GameShell.State.Flying) { Autopilot(); times.Add(Time.unscaledDeltaTime); yield return null; }
                state.leftStick = Vector2.zero; Set();
                times.Sort(); float avg = times.Average() * 1000, p99 = times[(int)(times.Count * .99f)] * 1000;
                Log($"  {Biomes.Name(b)}: frame time average {avg:0.0} ms ({1000 / avg:0} fps), 1 % worst {p99:0.0} ms, worst {times.Last() * 1000:0} ms over {times.Count} frames (72 Hz = 13.9 ms, 90 Hz = 11.1 ms)");
                if (b == Biome.Canyons) { EyeShot(Camera.StereoscopicEye.Left, "r8c_canyons_left"); EyeShot(Camera.StereoscopicEye.Right, "r8c_canyons_right"); }
            }
        }
    }
}
