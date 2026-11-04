using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

namespace HostileHorizon
{
    // Fix 3.2 check, in the built exe:  HostileHorizon.exe -exeCheck <dir> -exeCheckOnly fix32
    // A new career (a fixed system seed, the check's own save): clear today's planet (one pilot picked up as a player does, the
    // other two put aboard by the check), dock -> the planet-of-the-day pay and "cleared!", Jump to a new planet as the default;
    // jump -> a new planet with another name and pilots; one pilot from there (normal pay), others left waiting; the Planets list;
    // back to today's planet and back again (the waiting pilots still there); quit to the title + Continue (same planet, same
    // fates); then the same system seed in a second new career gives the same second planet.
    public sealed partial class ExeCheck
    {
        const int Fix32System = 4242;

        int Row(string label) { var p = G.Menu.Current; for (int i = 0; i < p.Rows.Count; i++) if (p.Rows[i].Label().Contains(label)) return i; Log($"  (no row \"{label}\" on {p.Title})"); return 0; }
        string Selected() { var p = G.Menu.Current; return p == null || p.Rows.Count == 0 ? "-" : p.Rows[p.Selected].Label(); }
        string Planet1() => $"{G.Planet.name} (seed {G.Planet.seed}, tier {G.Planet.tier}, {G.Planet.pilots.Length} pilots, aliens {AlienList(G.Planet)}, first pad {(G.Pilots.Count > 0 ? G.Pilots.OrderBy(p => p.Index).First().LandingSpot.ToString("F0") : "-")})";

        IEnumerator ToCampaignMenu()
        {
            for (int k = 0; k < 3 && G.Menu.Current?.Title != "CAMPAIGN"; k++) { G.Menu.Current.Selected = 0; yield return Nav(GamepadButton.DpadDown, 1); yield return Press(GamepadButton.South); yield return new WaitForSecondsRealtime(.5f); }
        }

        // One sortie: launch, land by a real pilot's pad (the check puts the ship 40 m over it), engines off, the run and knock, B;
        // optionally more put aboard by the check; climb to the docking height: the live docking; back in the hangar.
        IEnumerator Sortie(bool boardAll, string tag)
        {
            G.Menu.Current.Selected = 0; yield return Press(GamepadButton.South);   // Launch
            yield return WaitPhase(() => G.Now == GameShell.State.Flying, 40f, "flight after launch");
            var p = G.Pilots.Where(x => x.OnFoot && G.Planet.alien[x.Index] == 0).OrderBy(x => x.Index).FirstOrDefault();
            if (p == null) { Log($"  {tag}: no real pilot waiting"); yield break; }
            yield return LandFor(p);
            yield return WaitPhase(() => p.AtDoor, 90f, "the knock");
            yield return Press(GamepadButton.East);                                  // airlock
            yield return WaitPhase(() => !p.OnFoot, 10f, "boarding");
            Log($"  {tag}: pilot {p.Index + 1} picked up at the airlock (cabin {G.CabinCount})");
            if (boardAll)
                foreach (var q in G.Pilots.Where(x => x.OnFoot).ToList()) { G.CheckBoard(q); Log($"  {tag}: check shortcut: pilot {q.Index + 1} put aboard"); }
            int cr = Career.C.credits;
            // straight up in landing mode, clear of the canyon walls, to the docking height (the docking starts by itself)
            if (!G.Ship.EnginesOn) yield return Press(GamepadButton.South);
            state.rightTrigger = 1; Set();
            yield return WaitFor(() => G.Now != GameShell.State.Flying || G.Ship.Altitude > Tuning.T.dockingHeight + 30f, 150f);
            state.rightTrigger = 0; Set();
            Log($"  {tag}: climbed in landing mode to {G.Ship.Altitude:0} m ({G.Now}/{G.Phase})");
            yield return WaitPhase(() => G.Now == GameShell.State.Auto, 30f, "docking to start");
            yield return WaitPhase(() => G.Phase == "docked", 150f, "docked");
            yield return new WaitForSecondsRealtime(1f);
            Log($"  {tag}: docked; credits {cr} -> {Career.C.credits}; debrief \"{G.Debrief.Replace("\n", " | ")}\"");
        }

        IEnumerator Fix32()
        {
            StartCoroutine(Progress());
            yield return WaitFor(() => G.Now == GameShell.State.Title, 60f);
            yield return new WaitForSecondsRealtime(1.5f);
            var t = Tuning.T;
            Log($"tuning: pilot pay {t.pilotPay}, planet of the day +{t.dayBonus * 100:0} %, clear bonus {t.clearBonus}, jump cost {t.jumpCost}; today {Seeds.OfTheDay}");
            Log($"made planets of system {Fix32System}: " + string.Join(", ", Enumerable.Range(0, 4).Select(n => { int s = Career.MadePlanetSeed(Fix32System, n); return $"{n}: {Names.Planet(s)} ({s})"; })) +
                $"; again: {(Enumerable.Range(0, 4).All(n => Career.MadePlanetSeed(Fix32System, n) == Career.MadePlanetSeed(Fix32System, n)) ? "identical" : "DIFFERENT")}");

            // ---- a new career on today's planet
            Career.CheckSystemSeed = Fix32System;
            yield return ToCampaignMenu();
            yield return Nav(GamepadButton.DpadDown, 1); yield return Press(GamepadButton.South);   // New career
            yield return WaitPhase(() => G.Now == GameShell.State.Docked && G.Phase == "docked", 60f, "the hangar");
            Log($"new career, the {Career.C.systemName} system; planet 1: {Planet1()}; rows {string.Join(" / ", G.Menu.Current.Rows.Select(r => r.Label()))}");
            yield return Shot("a0_hangar_new_career");

            // ---- clear it: the planet-of-the-day pay, "cleared!", Jump as the default
            yield return Sortie(true, "today's planet");
            Log($"  planet {G.Planet.name}: cleared {G.Planet.cleared}, fates {Fates()}; hangar default row \"{Selected()}\"");
            Log($"  hangar: \"{G.Menu.Current.Body().Replace("\n", " | ")}\"");
            yield return Shot("a1_hangar_cleared_jump_default");
            int day = G.Planet.seed;

            // ---- jump (the default row: A)
            float j0 = Time.realtimeSinceStartup; float doors = 0;
            yield return Press(GamepadButton.South);
            yield return new WaitForSecondsRealtime(.2f); yield return Shot("a2_jumping");
            while (G.Phase != "docked" && Time.realtimeSinceStartup < j0 + 30f) { doors = Mathf.Max(doors, G.Mother.DoorsOpen); yield return null; }
            Log($"jump: {Time.realtimeSinceStartup - j0:0.0} s, bay doors opened at most {doors:0.00} (shut = 0); planet 2: {Planet1()}; different from planet 1: {(G.Planet.seed != day && G.Planet.name != Names.Planet(day) ? "yes" : "NO")}");
            Log($"  debrief \"{G.Debrief.Replace("\n", " | ")}\"");
            yield return new WaitForSecondsRealtime(1f); yield return Shot("a3_new_planet_hangar");
            string second = Planet1(); int secondSeed = G.Planet.seed;

            // ---- one pilot from the new planet (normal pay), the rest left waiting
            yield return Sortie(false, "planet 2");
            string fates2 = Fates();
            Log($"  planet 2 fates {fates2}");

            // ---- the planet list
            G.Menu.Current.Selected = Row("Planets"); yield return Press(GamepadButton.South);
            yield return new WaitForSecondsRealtime(.4f);
            Log($"Planets page: rows {string.Join(" / ", G.Menu.Current.Rows.Where(r => r.Visible == null || r.Visible()).Select(r => r.Label()))}");
            Log($"  body (New planet selected): \"{G.Menu.Current.Body().Replace("\n", " | ")}\"");
            G.Menu.Current.Selected = 1; yield return null;
            Log($"  body (row 1): \"{G.Menu.Current.Body().Replace("\n", " | ")}\"");
            yield return Shot("a4_planets_list");
            yield return Press(GamepadButton.South);                                // today's planet: jump back
            yield return WaitPhase(() => G.Phase == "docked" && G.Planet.seed == day, 30f, "back on today's planet");
            Log($"back on {G.Planet.name}: fates {Fates()}, cleared {G.Planet.cleared}, pilots on the ground {G.Pilots.Count}; default row \"{Selected()}\"");
            G.Menu.Current.Selected = Row("Planets"); yield return Press(GamepadButton.South);
            yield return new WaitForSecondsRealtime(.4f);
            G.Menu.Current.Selected = 2; yield return null;
            Log($"  list row 2: {G.Menu.Current.Rows[2].Label()}");
            yield return Press(GamepadButton.South);                                // planet 2 again
            yield return WaitPhase(() => G.Phase == "docked" && G.Planet.seed == secondSeed, 30f, "back on planet 2");
            Log($"back on {G.Planet.name}: fates {Fates()} (before {fates2}: {(Fates() == fates2 ? "same" : "DIFFERENT")}), waiting pilots on the ground {G.Pilots.Count(x => x.OnFoot)}");
            yield return Shot("a5_back_on_planet2");

            // ---- quit to the title and Continue
            string before = $"{Career.C.credits} cr, planet {G.Planet.name}, fates {Fates()}, {Career.C.planets.Count} planets, made {Career.C.planetsMade}";
            G.Menu.Current.Selected = Row("Back to title"); yield return Press(GamepadButton.South);
            yield return WaitPhase(() => G.Now == GameShell.State.Title, 60f, "the title");
            yield return ToCampaignMenu();
            yield return Press(GamepadButton.South);                                // Continue
            yield return WaitPhase(() => G.Now == GameShell.State.Docked && G.Phase == "docked", 60f, "the hangar after Continue");
            string after = $"{Career.C.credits} cr, planet {G.Planet.name}, fates {Fates()}, {Career.C.planets.Count} planets, made {Career.C.planetsMade}";
            Log($"quit and Continue: before [{before}] after [{after}] ({(before == after ? "same" : "DIFFERENT")}); save {File.GetLastWriteTime(Career.FilePath):HH:mm:ss}");

            // ---- the same career seed gives the same second planet
            G.Menu.Current.Selected = Row("Back to title"); yield return Press(GamepadButton.South);
            yield return WaitPhase(() => G.Now == GameShell.State.Title, 60f, "the title");
            yield return ToCampaignMenu();
            yield return Nav(GamepadButton.DpadDown, 1); yield return Press(GamepadButton.South);   // New career
            yield return new WaitForSecondsRealtime(.4f);
            G.Menu.Current.Selected = 1; yield return Press(GamepadButton.South);  // Yes, start a new career
            yield return WaitPhase(() => G.Now == GameShell.State.Docked && G.Phase == "docked", 60f, "the hangar");
            Career.C.stats.planetsCleared = 1; Log("  check shortcut: planets cleared set to 1, as in the first career when it jumped (the same tier)");
            G.Menu.Current.Selected = Row("Jump to a new planet"); yield return Press(GamepadButton.South);
            yield return WaitPhase(() => G.Phase == "docked" && G.Planet.seed != day, 30f, "the second planet");
            Log($"second new career, same system seed: planet 2: {Planet1()}; first time: {second}; {(Planet1() == second ? "identical" : "DIFFERENT")}");
            Career.CheckSystemSeed = 0;
        }
    }
}
