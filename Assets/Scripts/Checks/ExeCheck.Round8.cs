using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

namespace HostileHorizon
{
    // Round 8 check, in the built exe:  HostileHorizon.exe -exeCheck <dir> -exeCheckOnly round8a
    // Part A: what draws over the creature. Each measurement draws the same frame several times from the player's eye: with and
    // without the creature, the cockpit screens, the rest of the cockpit, the flat HUD; the pixels that change give how much of the
    // creature each one hides. Per creature: the strike held still (Round 7's staging with 0.7.0's HUD = before; Round 8's = after),
    // a real strike played (pictures at +0.1 / +0.5 / +1.2 s after it is in view), on the canopy, and the canopy break-in.
    public sealed partial class ExeCheck
    {
        Color32[] Grab(Camera cam)
        {
            Canvas.ForceUpdateCanvases(); cam.Render();
            var rt = cam.targetTexture; var prev = RenderTexture.active; RenderTexture.active = rt;
            var t = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false); t.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); t.Apply();
            RenderTexture.active = prev; var px = t.GetPixels32(); Destroy(t); return px;
        }
        static bool Differs(Color32 a, Color32 b) => Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b) > 18;

        // What hides the creature, from the eye, right now. Saves <name>_covered.png: the frame with the hidden parts of the creature
        // painted magenta (behind the screens), orange (behind the cockpit's body) and cyan (under the flat HUD).
        string Cover(Creature c, string name)
        {
            var cam = G.Rig.Head; if (!TestRun.Headless || !cam.targetTexture || !c) return "not measured (needs the headless view)";
            bool hud = G.Displays.OverlayShown;
            var cr = c.GetComponentsInChildren<Renderer>(true).Where(r => r.enabled).ToList();
            var shipR = G.Ship.GetComponentsInChildren<Renderer>(true).Where(r => r.enabled && !r.GetComponentInParent<Creature>()).ToList();
            var screens = G.Ship.GetComponentsInChildren<Canvas>(true).Where(k => k.enabled && k.renderMode == RenderMode.WorldSpace).ToList();
            c.Shadows(false);
            Color32[] Pic(bool cre, bool ship, bool disp, bool h)
            {
                foreach (var r in cr) r.enabled = cre; foreach (var r in shipR) r.enabled = ship; foreach (var k in screens) k.enabled = disp;
                G.Displays.SetVisible(h); return Grab(cam);
            }
            var full = Pic(true, true, true, false); var noC = Pic(false, true, true, false);
            var noD = Pic(true, true, false, false); var noDnoC = Pic(false, true, false, false);
            var bare = Pic(true, false, false, false); var bareNoC = Pic(false, false, false, false);
            var withHud = Pic(true, true, true, true);
            foreach (var r in cr) r.enabled = true; foreach (var r in shipR) r.enabled = true; foreach (var k in screens) k.enabled = true;
            G.Displays.SetVisible(hud); c.Shadows(true);
            int total = 0, seen = 0, byScreens = 0, byShip = 0, underHud = 0; var mark = (Color32[])withHud.Clone();
            for (int i = 0; i < full.Length; i++)
            {
                if (!Differs(bare[i], bareNoC[i])) continue;   // no creature at this pixel
                total++;
                bool s = Differs(full[i], noC[i]), sNoScreens = Differs(noD[i], noDnoC[i]);
                if (s) { seen++; if (Differs(withHud[i], full[i])) { underHud++; mark[i] = new Color32(0, 230, 255, 255); } }
                else if (sNoScreens) { byScreens++; mark[i] = new Color32(255, 0, 255, 255); }
                else { byShip++; mark[i] = new Color32(255, 140, 0, 255); }
            }
            var tex = new Texture2D(cam.targetTexture.width, cam.targetTexture.height, TextureFormat.RGBA32, false); tex.SetPixels32(mark); tex.Apply();
            File.WriteAllBytes(Path.Combine(dir, name + "_covered.png"), tex.EncodeToPNG()); Destroy(tex);
            float P(int n) => n * 100f / Mathf.Max(1, total);
            var e = G.Ship.transform.TransformPoint(ShipLayout.Eye); var sv = new List<Vector3>(); c.Surface(sv);
            float near = sv.Count > 0 ? sv.Min(v => (v - e).magnitude) : -1;
            var vp = cam.WorldToViewportPoint(c.HeadWorld); bool faceIn = vp.z > 0 && vp.x > 0 && vp.x < 1 && vp.y > 0 && vp.y < 1;
            return $"face {(faceIn ? $"IN VIEW at ({vp.x:0.00}, {vp.y:0.00})" : "NOT IN VIEW")}; creature in the view {total} px ({total * 100f / full.Length:0.0} % of it); hidden by the cockpit screens {P(byScreens):0.0} %, by the cockpit's body " +
                   $"(dash, frame, seat) {P(byShip):0.0} %, under the flat HUD {P(underHud):0.0} % (HUD {(hud ? "shown" : "hidden")} in play; measured with it on); " +
                   $"face {Vector3.Distance(e, c.HeadWorld):0.00} m from the eye, nearest part {near:0.00} m, face turned {c.TwistAngle:0} deg from the clip; screens 0.51-0.57 m";
        }

        IEnumerator HoldStrike(AlienType type, int staging, string tag)
        {
            GameShell.StrikeStaging = staging;
            var c = G.CheckStrikeCreature(type, 0);
            float end = Time.realtimeSinceStartup + 1.6f; while (Time.realtimeSinceStartup < end) { Autopilot(); yield return null; }
            state.leftStick = Vector2.zero; Set();
            yield return new WaitForEndOfFrame();
            bool before = staging == 0;
            G.Displays.SetVisible(before);   // 0.7.0 kept the HUD over the strike; Round 8 hides it
            Log($"  {type} strike held, {(before ? "BEFORE (Round 7 staging, HUD as 0.7.0: shown)" : "AFTER (Round 8 staging, HUD hidden)")}: {Cover(c, tag)}");
            yield return Shot(tag);
            G.Displays.SetVisible(!before); yield return Shot(tag + (before ? "_nohud" : "_hud"));
            G.Displays.SetVisible(true);
            Destroy(c.gameObject); GameShell.StrikeStaging = 2;
            yield return null;
        }

        IEnumerator RealStrike(AlienType type)
        {
            string tag = $"r8a_{type.ToString().ToLower()}_strike_play";
            G.CheckSetup(3, new[] { (int)type, 0, 0 }, new[] { 0, 0, 0 }); G.Foes.Running = false;
            var p = G.Pilots.Find(x => x.Index == 0);
            yield return WaitFor(() => G.Now == GameShell.State.Flying && !G.Ship.Kinematic, 60f);
            if (G.Ship.AboveGround < 30f) yield return BackToFlight();
            G.CheckStrikeKind = (int)GameShell.StrikeKind.Takeoff; G.StrikeAtReset();
            G.CheckBoard(p);
            if (type == AlienType.Sleeper) G.SleeperTurnAt = 2f;
            G.CheckStrikeKind = -1;
            float end = Time.realtimeSinceStartup + 20f;
            while (G.StrikeSeenAt < 0 && Time.realtimeSinceStartup < end && G.Now == GameShell.State.Flying) { Autopilot(); yield return null; }
            state.leftStick = Vector2.zero; Set();
            if (G.StrikeSeenAt < 0) { Log($"  {type} strike played: NO STRIKE in 20 s"); yield break; }
            float seen = G.StrikeSeenAt;
            foreach (var (dt, n) in new[] { (.1f, "1"), (.5f, "2"), (1.2f, "3") })
            {
                yield return WaitFor(() => Time.time - seen > dt, 3f);
                yield return new WaitForEndOfFrame();
                string what = n == "2" ? Cover(G.HorrorCreature, $"{tag}_{n}") : "";
                yield return Shot($"{tag}_{n}_{dt:0.0}s");
                Log($"    {type} strike played +{Time.time - seen:0.00} s after in view: HUD {(G.Displays.OverlayShown ? "SHOWN" : "hidden")}, menu {(G.Menu.Current != null ? "\"" + G.Menu.Current.Title + "\"" : "none")}{(what != "" ? "; " + what : "")}");
            }
            yield return WaitFor(() => G.Menu.Current?.Title == "SHIP DESTROYED", 10f);
            Log($"    death page: HUD {(G.Displays.OverlayShown ? "shown" : "hidden")}, the page {(G.Menu.Current?.Title == "SHIP DESTROYED" ? "shown" : "NOT shown")}");
            yield return DeadToHangar(tag);
        }

        IEnumerator CanopyAndBreakIn(AlienType type)
        {
            if (type == AlienType.Sleeper) { Log("  Sleeper on the canopy: never (it boards as a pilot)"); yield break; }
            string k = type.ToString().ToLower();
            G.CheckSetup(3, new[] { (int)type, 0, 0 }, new[] { 0, 0, 0 });
            var p = G.Pilots.Find(x => x.Index == 0); p.Leaps = true; p.ClimbAfter = 1.5f;
            yield return LandFor(p);
            yield return WaitPhase(() => p.AtDoor, 60f, "the door");
            yield return WaitFor(() => G.HorrorBeat == "on the canopy", 20f);
            if (G.HorrorBeat != "on the canopy") { Log($"  {type} on the canopy: never got there (beat \"{G.HorrorBeat}\")"); yield break; }
            yield return new WaitForSecondsRealtime(1.5f); yield return new WaitForEndOfFrame();
            Log($"  {type} on the canopy (HUD shown, as in play): {Cover(G.HorrorCreature, $"r8a_{k}_canopy")}");
            yield return Shot($"r8a_{k}_canopy_hud");
            G.Displays.SetVisible(false); yield return Shot($"r8a_{k}_canopy_nohud"); G.Displays.SetVisible(true);
            // left there: it breaks in through the canopy
            yield return WaitFor(() => G.HorrorBeat == "in front of you", 30f);
            if (G.HorrorBeat != "in front of you") { Log($"  {type} break-in: never in front of you (beat \"{G.HorrorBeat}\")"); yield break; }
            yield return new WaitForSecondsRealtime(1.2f); yield return new WaitForEndOfFrame();
            Log($"  {type} through the canopy, in front of you: HUD {(G.Displays.OverlayShown ? "SHOWN" : "hidden")}; {Cover(G.HorrorCreature, $"r8a_{k}_breakin")}");
            yield return Shot($"r8a_{k}_breakin");
            yield return DeadToHangar($"r8a_{k}_breakin");
        }

        IEnumerator Round8()
        {
            StartCoroutine(Progress());
            yield return WaitFor(() => G.Now == GameShell.State.Title, 60f);
            yield return new WaitForSecondsRealtime(1.5f);
            Career.CheckSystemSeed = 7171;
            GameSettings.S.difficulty.crashLevel = 1; GameSettings.S.difficulty.enemies = 0; GameSettings.S.routeTrail = false;
            yield return ToCampaignMenu();
            yield return Nav(GamepadButton.DpadDown, 1); yield return Press(GamepadButton.South);   // New career
            yield return WaitPhase(() => G.Now == GameShell.State.Docked && G.Phase == "docked", 60f, "the hangar");
            yield return new WaitForSecondsRealtime(1f);
            Log($"new career: {G.Planet.name} ({G.Planet.biome})");
            yield return LaunchFromHangar();
            var types = LauncherBridge.Argument("-r8types");   // e.g. -r8types Mimic,Hollow: just those
            Log("--- the creature at the strike and on the canopy: what hides it (before = Round 7's staging with 0.7.0's HUD; after = Round 8's)");
            foreach (var type in Five)
            {
                if (!string.IsNullOrEmpty(types) && !types.Contains(type.ToString())) continue;
                string k = type.ToString().ToLower();
                yield return HoldStrike(type, 0, $"r8a_{k}_strike_before");
                yield return HoldStrike(type, 2, $"r8a_{k}_strike_after");
                yield return RealStrike(type);
                yield return CanopyAndBreakIn(type);
            }
            Career.CheckSystemSeed = 0; GameSettings.S.routeTrail = true;
        }
    }
}
