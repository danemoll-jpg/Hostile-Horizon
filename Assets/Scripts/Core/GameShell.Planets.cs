using System.Collections.Generic;
using UnityEngine;

namespace HostileHorizon
{
    // Fix 3.2: the planet list (a simple star map until Round 5), from the hangar. Today's planet first ("Planet of the day",
    // even before its first visit), then every planet the career has visited, most recent first; "New planet" jumps to the next
    // new planet of the home system. Pick one and the mothership jumps there (Jump in GameShell.Campaign); Launch from the hangar.
    public sealed partial class GameShell
    {
        const int PlanetSlots = 9;
        int planetsFrom;   // the list's page (first shown entry)

        List<int> PlanetOrder()
        {
            var c = Career.C; int today = Seeds.OfTheDay;
            var rest = c.planets.FindAll(p => p.seed != today);
            rest.Sort((a, b) => string.CompareOrdinal(b.lastVisit ?? "", a.lastVisit ?? ""));
            var list = new List<int> { today };
            foreach (var p in rest) list.Add(p.seed);
            return list;
        }

        int SlotSeed(int slot) { var l = PlanetOrder(); int i = planetsFrom + slot; return i < l.Count ? l[i] : 0; }

        string PlanetLine(int seed)
        {
            var p = Career.C.planets.Find(x => x.seed == seed);
            string name = p?.name ?? Names.Planet(seed), here = Planet != null && Planet.seed == seed ? "  ◄ here" : "";
            string what = p == null ? "new" : p.cleared ? "cleared" : $"{p.Count(PilotFate.Waiting)} waiting";
            return $"{name}  <size=24>{(seed == Seeds.OfTheDay ? "★ of the day · " : "")}{what}{here}</size>";
        }

        void BuildPlanetsPage()
        {
            planetsPage = new MenuPanel.Page { Title = "PLANETS", Back = () => Menu.Show(hangar) };
            planetsPage.Subtitle = "";
            planetsPage.Add(new MenuPanel.Row { Label = () => "New planet" + (Tuning.T.jumpCost > 0 ? $"  <size=24>({Tuning.T.jumpCost} cr)</size>" : ""), Submit = JumpToNew, Disabled = () => Career.C.credits < Tuning.T.jumpCost });
            for (int i = 0; i < PlanetSlots; i++)
            {
                int slot = i;
                planetsPage.Add(new MenuPanel.Row
                {
                    Label = () => PlanetLine(SlotSeed(slot)),
                    Visible = () => SlotSeed(slot) != 0,
                    Submit = () => { int seed = SlotSeed(slot); if (seed == 0) return; if (Planet != null && Planet.seed == seed) Menu.Show(hangar); else JumpTo(seed); },
                });
            }
            planetsPage.Add(new MenuPanel.Row
            {
                Label = () => "More planets…", Visible = () => PlanetOrder().Count > PlanetSlots,
                Submit = () => { planetsFrom += PlanetSlots; if (planetsFrom >= PlanetOrder().Count) planetsFrom = 0; planetsPage.Selected = 1; },
            });
            planetsPage.Add(Action("Back", () => Menu.Show(hangar)));
            planetsPage.Body = PlanetsBody;
        }

        void ShowPlanets() { planetsFrom = 0; Menu.Show(planetsPage); }

        // The selected row's planet in full (or what "New planet" does).
        string PlanetsBody()
        {
            var c = Career.C; if (c == null) return "";
            var t = Tuning.T; int sel = planetsPage.Selected;
            string head = $"The {c.systemName} system: {c.planets.Count} planet{(c.planets.Count == 1 ? "" : "s")} visited, {c.stats.planetsCleared} cleared.\n\n";
            if (sel == 0)
                return head + $"<b>New planet</b>\nThe mothership jumps to a planet the corps hasn't searched yet: next is <b>{Names.Planet(Career.MadePlanetSeed(c.systemSeed, c.planetsMade))}</b>." +
                       $"\nMore pilots down (and more aliens) as your career clears planets.\nPilots left on other planets wait there for you.";
            if (sel < 1 || sel > PlanetSlots) return head;
            int seed = SlotSeed(sel - 1); if (seed == 0) return head;
            var p = c.planets.Find(x => x.seed == seed); bool day = seed == Seeds.OfTheDay;
            string s = head + $"<b>{p?.name ?? Names.Planet(seed)}</b>   ({p?.biome ?? "canyons"}, seed {seed})\n";
            if (day) s += $"<color=#FFC873>Planet of the day: pilots pay +{t.dayBonus * 100:0} % ({Mathf.RoundToInt(t.pilotPay * (1 + t.dayBonus))} cr each). A new one every day.</color>\n";
            if (p == null) return s + "\nNot visited yet.";
            s += $"\nPilots: {p.Count(PilotFate.Rescued)} rescued · {p.Count(PilotFate.Lost)} lost · {p.Count(PilotFate.Waiting)} still waiting" +
                 (p.cleared ? "\n<color=#9CFFB0>Cleared</color>" : "") + $"\nVisits {p.visits}" + (p.lastVisit != null ? $", last {p.lastVisit.Substring(0, Mathf.Min(16, p.lastVisit.Length))}" : "");
            if (Planet != null && Planet.seed == seed) s += "\n\nThe mothership is over this planet now.";
            return s;
        }
    }
}
