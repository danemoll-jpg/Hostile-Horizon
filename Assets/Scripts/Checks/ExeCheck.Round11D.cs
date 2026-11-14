using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

namespace HostileHorizon
{
    // Round 11 Part D check (-exeCheckOnly round11d): the radar filter on a busy planet. A new career, launched; the planet's own contacts
    // plus extra ones the check adds round the ship every frame (12 pilots, 8 turrets, 4 saucers, 3 task sites: a busier planet than any
    // tier makes, so the clutter shows), drawn by the game's own radar (HUD and cockpit). Pictures: Round 10's drawing (before), then
    // All / Pilots / Enemies / Tasks (after); the filter cycled with the pad (hold D-pad →; a tap still picks the route target), saved.
    public sealed partial class ExeCheck
    {
        bool busyRadar;
        IEnumerator BusyContacts()
        {
            while (busyRadar)
            {
                yield return null;   // after the game's Update (CampaignTick fills Contacts), before the displays draw (LateUpdate)
                if (G.Now != GameShell.State.Flying && G.Now != GameShell.State.Paused) continue;
                var p = G.Ship.transform.position; var con = G.Displays.Contacts;
                for (int i = 0; i < 12; i++) { float a = i * 37f * Mathf.Deg2Rad, r = 300f + (i * 211) % 2600; con.Add(new RadarContact { position = p + new Vector3(Mathf.Sin(a) * r, 0, Mathf.Cos(a) * r), number = 20 + i }); }
                for (int i = 0; i < 8; i++) { float a = (i * 53f + 20f) * Mathf.Deg2Rad, r = 500f + (i * 377) % 2400; con.Add(new RadarContact { position = p + new Vector3(Mathf.Sin(a) * r, 0, Mathf.Cos(a) * r), enemy = 1 }); }
                for (int i = 0; i < 4; i++) { float a = (i * 90f + 45f) * Mathf.Deg2Rad, r = 900f + i * 300f; con.Add(new RadarContact { position = p + new Vector3(Mathf.Sin(a) * r, 0, Mathf.Cos(a) * r), enemy = 2 }); }
                string[] marks = { "RECORDER", "NEST", "WRECK" };
                for (int i = 0; i < 3; i++) { float a = (i * 120f + 10f) * Mathf.Deg2Rad, r = 1200f + i * 500f; con.Add(new RadarContact { position = p + new Vector3(Mathf.Sin(a) * r, 0, Mathf.Cos(a) * r), mark = marks[i] }); }
            }
        }

        IEnumerator Round11D()
        {
            Log("=== Round 11 D: the radar filter");
            yield return ToCampaignMenu();
            yield return Nav(GamepadButton.DpadDown, 1); yield return Press(GamepadButton.South);   // New career
            if (G.Menu.Current != null && G.Menu.Current.Title == "TRAINING FIRST?") { G.Menu.Current.Selected = Row("Not now"); G.Menu.Show(G.Menu.Current, true); yield return Press(GamepadButton.South); }
            yield return WaitPhase(() => G.Now == GameShell.State.Docked && G.Phase == "docked", 90f, "the hangar");
            yield return LaunchFromHangar();
            yield return new WaitForSecondsRealtime(2f);
            busyRadar = true; StartCoroutine(BusyContacts());
            G.Rig.Current = CameraRig.Mode.Chase; G.Rig.Snap();
            yield return new WaitForSecondsRealtime(1f);
            Log($"  contacts on the radar: {G.Displays.Contacts.Count} (the planet's own and the check's)");
            RadarScope.OldStyle = true; yield return null; yield return null; yield return Shot("r11d_0_before_round10");
            RadarScope.OldStyle = false; RadarScope.Filter = 0; yield return null; yield return null; yield return Shot("r11d_1_all");
            int f0 = RadarScope.Filter; int pick0 = 0;
            for (int k = 1; k <= 3; k++)
            {
                yield return HoldButton(GamepadButton.DpadRight, .8f); yield return null; yield return null;
                Log($"  hold D-pad → 0.8 s: filter {RadarScope.FilterNames[RadarScope.Filter]} (settings radarFilter {GameSettings.S.radarFilter}); HUD list: {RadarScope.List(G.Ship.transform, G.Displays.Contacts, 5).Replace("\n", " | ")}");
                yield return Shot($"r11d_{k + 1}_{RadarScope.FilterNames[RadarScope.Filter].ToLowerInvariant()}");
            }
            G.Rig.Current = CameraRig.Mode.Cockpit; G.Rig.Snap(); yield return new WaitForSecondsRealtime(.5f); yield return Shot("r11d_5_cockpit_tasks");
            yield return HoldButton(GamepadButton.DpadRight, .8f);
            Log($"  a fourth hold: back to {RadarScope.FilterNames[RadarScope.Filter]}");
            string r0 = G.Displays.Route; yield return Press(GamepadButton.DpadRight); yield return new WaitForSecondsRealtime(.3f);
            Log($"  a tap of D-pad →: filter still {RadarScope.FilterNames[RadarScope.Filter]}; route target \"{r0}\" -> \"{G.Displays.Route}\"");
            yield return Shot("r11d_6_all_cockpit");
            busyRadar = false;
            Log($"  output level (muted): {TestRun.Report()}");
        }
    }
}
