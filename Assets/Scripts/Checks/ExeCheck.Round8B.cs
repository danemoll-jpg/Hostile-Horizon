using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

namespace HostileHorizon
{
    // Round 8 Part B check, in the built exe:  HostileHorizon.exe -exeCheck <dir> -exeCheckOnly round8b
    // B1 / B5: the new models in game, from the player's view. Free Flight (Canyons, Bounce): the cockpit looking ahead, down
    // through the glass floor, left, right and back (the look control, as a player turns their head), the chase / close chase /
    // landing views, frame rates in the cockpit and the chase view; then the Campaign: the hangar (docked), the launch.
    public sealed partial class ExeCheck
    {
        IEnumerator LookShot(Vector2 stick, float seconds, string name)
        {
            float end = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < end) { Autopilot(); state.rightStick = stick; Set(); yield return null; }
            yield return Shot(name);
            state.rightStick = Vector2.zero; Set();
            yield return new WaitForSecondsRealtime(1.2f);   // springs back
        }

        IEnumerator Round8B()
        {
            yield return WaitFor(() => G.Now == GameShell.State.Title, 60f);
            yield return new WaitForSecondsRealtime(1.5f);
            Log("--- the new ship and cockpit (Free Flight, Canyons, Bounce)");
            yield return StartFreeFlight(Biome.Canyons, 1);
            yield return Fly(4f);
            yield return Shot("r8b_cockpit_ahead");
            yield return LookShot(new Vector2(0, -1), .7f, "r8b_cockpit_down");
            yield return LookShot(new Vector2(-1, -.25f), .8f, "r8b_cockpit_left");
            yield return LookShot(new Vector2(1, -.25f), .8f, "r8b_cockpit_right");
            yield return LookShot(new Vector2(-1, 0), 1.45f, "r8b_cockpit_back_left");
            yield return MeasureFps("cockpit, flying low (Canyons)");
            foreach (var (m, n) in new[] { (CameraRig.Mode.Chase, "chase"), (CameraRig.Mode.CloseChase, "close_chase"), (CameraRig.Mode.Landing, "landing_view") })
            {
                G.Rig.Current = m; G.Rig.Snap(); yield return Fly(1.5f); yield return Shot($"r8b_{n}");
            }
            G.Rig.Current = CameraRig.Mode.CloseChase; G.Rig.Snap();
            yield return MeasureFps("close chase, flying low (Canyons)");
            G.Rig.Current = CameraRig.Mode.Cockpit; G.Rig.Snap();
            yield return LookShot(new Vector2(-1, .3f), .9f, "r8b_cockpit_left_up");
            G.Pause(); yield return new WaitForSecondsRealtime(.5f);
            G.Menu.Current.Selected = Row("Return to title"); yield return Press(GamepadButton.South);
            yield return WaitFor(() => G.Now == GameShell.State.Title, 30f); yield return new WaitForSecondsRealtime(1f);
            Log("--- the hangar and the launch (Campaign, new career)");
            Career.CheckSystemSeed = 7171;
            yield return ToCampaignMenu();
            yield return Nav(GamepadButton.DpadDown, 1); yield return Press(GamepadButton.South);   // New career
            yield return WaitPhase(() => G.Now == GameShell.State.Docked && G.Phase == "docked", 60f, "the hangar");
            yield return new WaitForSecondsRealtime(1.5f);
            G.Displays.SetVisible(false); var page = G.Menu.Current; G.Menu.Hide(); yield return Shot("r8b_hangar_docked"); G.Menu.Show(page);
            G.Menu.Current.Selected = 0; yield return Press(GamepadButton.South);   // Launch
            yield return new WaitForSecondsRealtime(2.5f); yield return Shot("r8b_launch_1");
            G.Rig.Current = CameraRig.Mode.Chase; G.Rig.Snap(); yield return new WaitForSecondsRealtime(2.5f); yield return Shot("r8b_launch_2_chase");
            yield return WaitPhase(() => G.Now == GameShell.State.Flying, 40f, "flight after launch");
            G.Rig.Current = CameraRig.Mode.Cockpit; G.Rig.Snap();
            // the turret and the saucer up close (a check-only camera; the ship parked near them so the ground is there)
            var tur = G.Foes.All.Find(e => e.Kind == EnemyKind.Turret && !e.Dead);
            if (tur != null)
            {
                G.Ship.ResetTo(tur.Pos + new Vector3(0, 80, -120), 0); G.Rig.Snap(); yield return new WaitForSecondsRealtime(4f);
                yield return ModelShot(tur.Pos + Vector3.up * 2.4f, tur.Pos + tur.Root.rotation * new Vector3(9, 6, 14), "r8b_turret");
                yield return ModelShot(tur.Pos + Vector3.up * 2.4f, tur.Pos + new Vector3(-14, 2, -10), "r8b_turret_2");
                Log($"turret {tur.Index + 1} at {tur.Pos.ToString("0")}: pictures");
            }
            else Log("turret: none on this planet");
            var sau = G.Foes.CheckSaucer(G.Ship.transform.position + G.Ship.transform.forward * 60f + Vector3.up * 10f);
            yield return null; yield return null;
            yield return ModelShot(sau.Pos, sau.Pos + new Vector3(16, 7, 18), "r8b_saucer");
            yield return ModelShot(sau.Pos, sau.Pos + new Vector3(-14, -9, 16), "r8b_saucer_below");
            Career.CheckSystemSeed = 0;
        }

        IEnumerator ModelShot(Vector3 target, Vector3 from, string name)
        {
            yield return new WaitForEndOfFrame();
            var cam = new GameObject("Check camera").AddComponent<Camera>(); cam.CopyFrom(G.Rig.Head); cam.nearClipPlane = .3f;
            cam.transform.SetPositionAndRotation(from, Quaternion.LookRotation(target - from, Vector3.up)); cam.fieldOfView = 45;
            if (TestRun.Headless) { cam.targetTexture = TestRun.View; SaveCam(cam, name); }
            Destroy(cam.gameObject);
        }
    }
}
