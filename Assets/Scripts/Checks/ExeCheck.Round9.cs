using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

namespace HostileHorizon
{
    // Round 9 check, in the built exe:  HostileHorizon.exe -exeCheck <dir> -exeCheckOnly round9[a|b|c|d|e|f|g]
    // Part A (Fix 8.3): the ship's rear from the chase / close chase / landing views and from behind and below, with 0.8.2's cabin
    // (drawn for every camera: the grey box) and with the fix (drawn only inside the hull); the mothership's two openings from the
    // approach, from below, during the recall beam and from inside the bay; the Steam art copy (into a stand-in launcher folder).
    public sealed partial class ExeCheck
    {
        IEnumerator Round9()
        {
            string o = CheckRun.Only;
            bool all = o == "round9";
            if (all || o == "round9a") yield return Round9A();
            if (all || o == "round9b") yield return Round9B();
            if (all || o == "round9c" || o == "round9d") yield return Round9CD();
            if (all || o == "round9e") yield return Round9E();
            if (all || o == "round9f") yield return Round9F();
            if (all || o == "round9g") yield return Round9G();
        }

        IEnumerator Round9A()
        {
            yield return WaitFor(() => G.Now == GameShell.State.Title, 60f);
            yield return new WaitForSecondsRealtime(1f);
            Log("--- A2: the ship's rear (Free Flight, Canyons, Bounce)");
            Log($"  steam art at start: {SteamArt.Result}");
            yield return StartFreeFlight(Biome.Canyons, 1);
            yield return Fly(3f);
            var cabin = G.Ship.GetComponentInChildren<Cabin>(true);
            Log($"  cabin under the ship: {(cabin ? cabin.name + (cabin.gameObject.activeInHierarchy ? " (active)" : " (inactive)") : "none")}");
            foreach (bool old in new[] { true, false })
            {
                Cabin.CheckShowOutside = old; string tag = old ? "before" : "after";
                foreach (var (m, n) in new[] { (CameraRig.Mode.Chase, "chase"), (CameraRig.Mode.CloseChase, "close_chase"), (CameraRig.Mode.Landing, "landing_view") })
                { G.Rig.Current = m; G.Rig.Snap(); yield return Fly(1.5f); yield return Shot($"r9a_{tag}_{n}"); }
                var t = G.Ship.transform;
                yield return ModelShot(t.TransformPoint(0, 0, -2.5f), t.TransformPoint(3, 2.2f, -13), $"r9a_{tag}_rear_34");
                yield return ModelShot(t.TransformPoint(0, -.4f, -2.5f), t.TransformPoint(-4, -4, -11), $"r9a_{tag}_rear_below");
                yield return ModelShot(t.TransformPoint(0, 0, -1.5f), t.TransformPoint(-12, -.5f, -2), $"r9a_{tag}_side");
            }
            Cabin.CheckShowOutside = false;
            G.Rig.Current = CameraRig.Mode.Cockpit; G.Rig.Snap();
            yield return Fly(1f);
            yield return LookShot(new Vector2(-1, 0), 1.45f, "r9a_cockpit_back_left");   // the cabin is still drawn from inside
            G.Pause(); yield return new WaitForSecondsRealtime(.5f);
            G.Menu.Current.Selected = Row("Return to title"); yield return Press(GamepadButton.South);
            yield return WaitFor(() => G.Now == GameShell.State.Title, 30f); yield return new WaitForSecondsRealtime(1f);

            Log("--- A1: the mothership's openings (Campaign, new career)");
            Career.CheckSystemSeed = 7171;
            yield return NewCareer9();
            yield return WaitPhase(() => G.Now == GameShell.State.Docked && G.Phase == "docked", 60f, "the hangar");
            yield return new WaitForSecondsRealtime(1.5f);
            var page = G.Menu.Current; G.Menu.Hide(); G.Displays.SetVisible(false);
            var M = G.Mother.transform;
            yield return ModelShot(M.TransformPoint(0, 1, 0), M.TransformPoint(0, 2.5f, -24), "r9a_bay_inside_to_ship");
            yield return ModelShot(M.TransformPoint(0, 1, -40), M.TransformPoint(0, 1.5f, -6), "r9a_bay_inside_to_stern");
            yield return ModelShot(M.TransformPoint(0, -6, 0), M.TransformPoint(-11, 7, -16), "r9a_bay_inside_floor_opening");
            G.Menu.Show(page);
            G.Menu.Current.Selected = 0; yield return Press(GamepadButton.South);   // Launch
            yield return new WaitForSecondsRealtime(2.5f);
            yield return ModelShot(M.TransformPoint(0, -10, 0), M.TransformPoint(30, -60, -40), "r9a_launch_floor_opening");
            yield return WaitPhase(() => G.Now == GameShell.State.Flying, 40f, "flight after launch");
            yield return Fly(3f);
            M = G.Mother.transform;
            yield return ModelShot(M.TransformPoint(0, 0, -40), M.TransformPoint(25, -8, -120), "r9a_approach_stern");
            yield return ModelShot(M.TransformPoint(0, 0, -38), M.TransformPoint(0, 1, -90), "r9a_stern_straight");
            yield return ModelShot(M.TransformPoint(0, -16, 0), M.TransformPoint(-35, -70, 30), "r9a_below_floor_opening");
            Log("  the recall: the beam lifts the ship through the floor opening");
            yield return HoldRecall(1.4f);
            yield return WaitPhase(() => G.Phase == "tow-lift" || G.Phase.Contains("lift"), 90f, "the beam");
            float seen = 0; int k = 0;
            while (G.Phase != "docked" && seen < 150f)
            {
                M = G.Mother.transform; var rel = M.InverseTransformPoint(G.Ship.transform.position);
                if (k == 0 && rel.y < -60) { yield return ModelShot(G.Ship.transform.position + Vector3.up * 20, M.TransformPoint(45, -50, -60), "r9a_docking_1_far"); k++; }
                else if (k == 1 && rel.y > -30) { yield return ModelShot(M.TransformPoint(0, -14, 0), M.TransformPoint(35, -45, -45), "r9a_docking_2_near"); k++; }
                else if (k == 2 && rel.y > -12) { yield return ModelShot(M.TransformPoint(0, -10, 0), M.TransformPoint(-22, -30, 20), "r9a_docking_3_entering"); k++; }
                seen += Time.unscaledDeltaTime; yield return null;
            }
            Log($"  docking pictures: {k} of 3; phase {G.Phase}");
            Career.CheckSystemSeed = 0;

            Log("--- 2b: the Steam art copy into a stand-in launcher folder");
            string fake = System.IO.Path.Combine(dir, "launcher_root");
            System.IO.Directory.CreateDirectory(fake);
            System.IO.File.WriteAllText(System.IO.Path.Combine(fake, "HostileHorizon-Cover.png"), "old cover");
            Log($"  game folder SteamArt/: {string.Join(", ", System.Array.ConvertAll(System.IO.Directory.Exists(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Application.dataPath), "SteamArt")) ? System.IO.Directory.GetFiles(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Application.dataPath), "SteamArt")) : new string[0], System.IO.Path.GetFileName))}");
            if (LauncherBridge.Argument("-steamArtRoot") != null)
            {
                System.IO.File.WriteAllText(System.IO.Path.Combine(LauncherBridge.Argument("-steamArtRoot"), "HostileHorizon-Cover.png"), "an older cover");
                SteamArt.CopyUp(); Log($"  again with an older cover there: {SteamArt.Result}");
                SteamArt.CopyUp(); Log($"  and once more: {SteamArt.Result}");
            }
        }

        // from the title: Campaign > New career (rows picked by label; a career already there is replaced after the Yes page)
        IEnumerator NewCareer9()
        {
            yield return WaitFor(() => G.Now == GameShell.State.Title && G.Menu.Current != null, 30f);
            G.Menu.Current.Selected = Row("Campaign"); yield return Press(GamepadButton.South); yield return new WaitForSecondsRealtime(.5f);
            G.Menu.Current.Selected = Row("New career"); yield return Press(GamepadButton.South); yield return new WaitForSecondsRealtime(.5f);
            if (G.Menu.Current?.Title == "NEW CAREER?") { G.Menu.Current.Selected = Row("Yes"); yield return Press(GamepadButton.South); }
        }

        IEnumerator Round9B() { yield break; }
        IEnumerator Round9CD() { yield break; }
        IEnumerator Round9E() { yield break; }
        IEnumerator Round9F() { yield break; }
        IEnumerator Round9G() { yield break; }
    }
}
