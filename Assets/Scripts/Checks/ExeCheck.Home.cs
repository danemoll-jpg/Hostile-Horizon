using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

namespace HostileHorizon
{
    // Fix 2.3 check, in the built exe:  HostileHorizon.exe -exeCheck <dir> -exeCheckOnly home
    // Getting home, played with the virtual gamepad from the title: (e) a pilot boards and brings energy; (a) the climb with one pilot,
    // the DOCK readout counting down until docking starts; (b) energy set low over the canyon floor: RETURN NOW, then CAN'T REACH;
    // (c) running dry in the air: the beam catches the ship, it docks, the pilot is paid and the tow fee charged; (d) the recall from the
    // air and from the ground (and refused while a pilot is running to the ship). Run it twice: the pilot fuel must differ.
    public sealed partial class ExeCheck
    {
        IEnumerator FlyUntil(Func<bool> f, float seconds)
        {
            float end = Time.realtimeSinceStartup + seconds;
            while (!f() && Time.realtimeSinceStartup < end && G.Now == GameShell.State.Flying) { Autopilot(); yield return null; }
            state.leftStick = Vector2.zero; Set();
        }

        string HomeNow => $"energy {G.Ship.Energy:0.0}%, needed to dock {G.EnergyHome:0.0}%, alt {G.Ship.Altitude:0} m (gnd {G.Ship.AboveGround:0} m), warning \"{G.Displays.HomeWarning}\", advice \"{G.Displays.Prompt}\"";

        // Hold the recall button (View on the pad) while flying on, until the shell takes the call or the time is up.
        IEnumerator HoldRecall(float seconds)
        {
            state = state.WithButton(GamepadButton.Select, true); Set();
            float end = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < end && G.Now == GameShell.State.Flying)
            { if (!G.Ship.Landed) Autopilot(); else Set(); yield return null; }
            state = state.WithButton(GamepadButton.Select, false); Set(); yield return null;
        }

        IEnumerator BoardHere(DownedPilot p)
        {
            yield return WaitPhase(() => p.AtDoor, 90f, "the knock");
            float e0 = G.Ship.Energy;
            yield return Press(GamepadButton.East);                          // airlock
            yield return WaitPhase(() => p.Now == DownedPilot.State.Aboard, 15f, "boarding");
            yield return null;
            Log($"(e) pilot {p.Index + 1} aboard: energy {e0:0.0}% -> {G.Ship.Energy:0.0}% (brought {G.LastPilotFuel:0} %, range {Tuning.T.pilotFuelMin:0}-{Tuning.T.pilotFuelMax:0}); message \"{G.Displays.Prompt}\"");
        }

        // Turn the view (right stick, as a player looks round) toward a point in the world, then hold it there.
        IEnumerator LookAt(Vector3 at, float seconds)
        {
            float end = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < end)
            {
                var h = G.Rig.Head.transform; Vector3 d = at - h.position, f = h.forward;
                float yaw = Vector3.SignedAngle(new Vector3(f.x, 0, f.z), new Vector3(d.x, 0, d.z), Vector3.up);
                float pitch = Mathf.Atan2(d.y, new Vector2(d.x, d.z).magnitude) * Mathf.Rad2Deg - Mathf.Asin(Mathf.Clamp(f.y, -1, 1)) * Mathf.Rad2Deg;
                state.rightStick = new Vector2(Mathf.Clamp(yaw / 25f, -1, 1), Mathf.Clamp(pitch / 25f, -1, 1)); Set();
                yield return null;
            }
        }

        IEnumerator LogDockReadout()
        {
            float next = 0;
            while (G.Now == GameShell.State.Flying)
            {
                if (G.Ship.Mode == FlightMode.Flight && G.Ship.Altitude > next) { Log($"  climbing: alt {G.Ship.Altitude:0} m, display \"{G.Displays.Home.Replace("\n", " / ")}\", advice \"{G.Displays.Prompt}\""); next = Mathf.Ceil((G.Ship.Altitude + 1) / 150f) * 150f; }
                yield return null;
            }
        }

        IEnumerator Home()
        {
            StartCoroutine(Progress());
            yield return WaitFor(() => G.Now == GameShell.State.Title, 60f);
            yield return new WaitForSecondsRealtime(1.5f);
            var t = Tuning.T;
            Log($"save folder: {Career.Folder}; tuning: recall {t.recallFee} cr (tow {t.towFee} cr), hold {t.recallHoldSeconds} s, climb rate {t.homeClimbRate} m/s, margin {t.homeMargin} %, RETURN NOW within {t.returnNowWarning} %, beam reach {t.beamReach} m, pilot fuel {t.pilotFuelMin}-{t.pilotFuelMax} %");
            yield return Nav(GamepadButton.DpadDown, 1); yield return Press(GamepadButton.South);   // Campaign
            yield return Nav(GamepadButton.DpadDown, 1); yield return Press(GamepadButton.South);   // New career
            yield return WaitPhase(() => G.Now == GameShell.State.Docked, 60f, "the hangar");
            Log($"new career: planet {G.Planet.name} (seed {G.Planet.seed}), {G.Pilots.Count} pilots, {Career.C.credits} cr");

            // ---- sortie 1: (e) a pilot's fuel, (a) the climb and the DOCK readout
            yield return LaunchFromHangar(null);
            var p1 = NearestWaiting();
            yield return ToPilotAndLand(p1, null);
            yield return Press(GamepadButton.South);                         // engines off
            yield return BoardHere(p1);
            yield return Shot("h01_pilot_brought_energy");
            var logger = StartCoroutine(LogDockReadout());
            bool shot = false;
            StartCoroutine(TakeOffAndClimb(t.dockingHeight + 30f));
            while (G.Now == GameShell.State.Flying)
            {
                if (!shot && G.Ship.Mode == FlightMode.Flight && G.Ship.Altitude > t.dockingHeight - 450f) { shot = true; yield return Shot("h02_climb_dock_readout"); }
                yield return null;
            }
            Log($"(a) docking started at alt {G.Ship.Altitude:0} m (docking height {t.dockingHeight:0} m), phase {G.Phase}; last readout \"{G.Displays.Home.Replace("\n", " / ")}\"");
            StopCoroutine(logger);
            yield return WaitPhase(() => G.Phase == "docked", 150f, "docked");
            Log($"  docked: {G.Debrief.Replace("\n", " | ")}");

            // ---- sortie 2: a second pilot, then (b) the warnings low over the ground and (c) running dry in the air
            yield return LaunchFromHangar(null);
            var p2 = NearestWaiting();
            yield return ToPilotAndLand(p2, null);
            yield return Press(GamepadButton.South);
            yield return BoardHere(p2);
            state.rightTrigger = 1; Set(); yield return WaitFor(() => G.Ship.AboveGround > 45f, 15f); state.rightTrigger = 0; Set();
            yield return SetMode(FlightMode.Flight);
            yield return FlyUntil(() => false, 5f);
            Log($"(b) flying low: {HomeNow}");
            G.Ship.Energy = G.EnergyHome + t.returnNowWarning + .6f;   // check shortcut: the drain is the normal one, only less of it to wait for
            Log($"  energy set to {G.Ship.Energy:0.0}% (just above the RETURN NOW point)");
            yield return FlyUntil(() => G.HomeState == 1, 30f);
            Log($"  RETURN NOW shown {G.HomeState == 1}: {HomeNow}");
            yield return new WaitForSecondsRealtime(.3f);
            yield return Shot("h03_return_now");
            var look = StartCoroutine(LookAt(G.Mother.transform.position, 99f));
            yield return FlyUntil(() => false, 1.8f);
            Log($"  looking toward the mothership: view {Vector3.Angle(G.Rig.Head.transform.forward, G.Mother.transform.position - G.Rig.Head.transform.position):0}° off it");
            yield return Shot("h03b_sky_marker_look_back");
            StopCoroutine(look); state.rightStick = Vector2.zero; Set();
            G.Ship.Energy = G.EnergyHome + .4f;
            Log($"  energy set to {G.Ship.Energy:0.0}% (just above the CAN'T REACH point)");
            yield return FlyUntil(() => G.HomeState == 2, 30f);
            Log($"  CAN'T REACH shown {G.HomeState == 2}: {HomeNow}");
            yield return new WaitForSecondsRealtime(.3f);
            yield return Shot("h04_cant_reach");

            int cr0 = Career.C.credits; float level = new Vector2(G.Mother.transform.position.x - G.Ship.transform.position.x, G.Mother.transform.position.z - G.Ship.transform.position.z).magnitude;
            StartCoroutine(WatchCamera());
            G.Ship.Energy = .3f;
            Log($"(c) energy set to 0.3 % in the air, {G.CabinCount} pilot aboard, mothership {level:0} m away (level)");
            yield return FlyUntil(() => G.Ship.OutOfEnergy, 10f);
            Log($"  out of energy at gnd {G.Ship.AboveGround:0} m: advice \"{G.Displays.Prompt}\"");
            yield return WaitPhase(() => G.Phase == "tow-lift", 60f, "the beam");
            Log($"  beam caught the ship at gnd {G.Ship.AboveGround:0} m, landed {G.Ship.Landed} (in the air: {!G.Ship.Landed && G.Ship.AboveGround > 2f})");
            yield return new WaitForSecondsRealtime(1.5f);
            look = StartCoroutine(LookAt(G.Mother.transform.position, 99f));
            yield return new WaitForSecondsRealtime(2f);
            yield return Shot("h05_beam_lift_look_up");
            StopCoroutine(look); state.rightStick = Vector2.zero; Set();
            yield return Press(GamepadButton.North); yield return new WaitForSecondsRealtime(1.2f);   // chase view: the beam from outside
            yield return Shot("h05b_beam_lift_chase");
            yield return Press(GamepadButton.North); yield return Press(GamepadButton.North); yield return Press(GamepadButton.North);   // back to cockpit
            yield return WaitPhase(() => G.Phase == "docked", 150f, "docked after the beam");
            Log($"  docked: {Continuity()}; credits {cr0} -> {Career.C.credits}; debrief: {G.Debrief.Replace("\n", " | ")}");

            // ---- (d) the recall from the air
            yield return LaunchFromHangar(null);
            yield return FlyUntil(() => false, 8f);
            cr0 = Career.C.credits;
            StartCoroutine(WatchCamera());
            yield return HoldRecall(.4f);
            Log($"(d) recall held 0.4 s: called {G.Calling == 2} (needs {t.recallHoldSeconds} s), advice \"{G.Displays.Prompt}\"");
            yield return HoldRecall(1.4f);
            Log($"  recall held 1.4 s in the air at gnd {G.Ship.AboveGround:0} m: called {G.Calling == 2 || G.Phase == "tow-lift"}, advice \"{G.Displays.Prompt}\"");
            yield return FlyUntil(() => false, 60f);
            Log($"  beam caught the ship at gnd {G.Ship.AboveGround:0} m (phase {G.Phase})");
            yield return new WaitForSecondsRealtime(2f);
            yield return Shot("h06_recall_air");
            yield return WaitPhase(() => G.Phase == "docked", 150f, "docked after the recall");
            Log($"  docked: {Continuity()}; credits {cr0} -> {Career.C.credits}; debrief: {G.Debrief.Replace("\n", " | ")}");

            // ---- (d) the recall from the ground: refused while a pilot runs to the ship, then taken
            yield return LaunchFromHangar(null);
            var p3 = NearestWaiting();
            if (p3 != null)
            {
                yield return ToPilotAndLand(p3, null);
                yield return Press(GamepadButton.South);                     // engines off: the pilot runs
                yield return WaitPhase(() => p3.Now == DownedPilot.State.Running, 15f, "the pilot to run");
                yield return HoldRecall(1.4f);
                Log($"  recall while pilot {p3.Index + 1} runs to the ship: called {G.Calling != 0} (must be False), message \"{G.Displays.Prompt}\"");
                yield return BoardHere(p3);
            }
            cr0 = Career.C.credits;
            StartCoroutine(WatchCamera());
            yield return HoldRecall(1.4f);
            Log($"  recall held 1.4 s landed {G.Ship.Landed}: called {G.Calling == 2 || G.Phase == "tow-lift"}, advice \"{G.Displays.Prompt}\"");
            yield return WaitPhase(() => G.Phase == "tow-lift", 60f, "the beam");
            Log($"  beam lifting from the ground (phase {G.Phase})");
            yield return WaitPhase(() => G.Phase == "docked", 150f, "docked after the ground recall");
            Log($"  docked: {Continuity()}; credits {cr0} -> {Career.C.credits}; debrief: {G.Debrief.Replace("\n", " | ")}");
            Log($"career: tows {Career.C.stats.tows}, recalls {Career.C.stats.recalls}, rescued {Career.C.stats.pilotsRescued}");
            File.Copy(Career.FilePath, Path.Combine(dir, "career_after_check.json"), true);
        }
    }
}
