using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HostileHorizon
{
    // Fix 3.2: the planet list, from the hangar. Round 6 (D): the full star map. The career's home system drawn in 3D (ViewStage,
    // shown as the page's picture, so a world-space menu shows it too): the star, every planet visited, today's planet of the day and
    // the next undiscovered one, each in its biome's colour with its name and status (planet of the day, cleared, pilots waiting,
    // new distress calls), and the selected one ringed. The rows pick a planet (the map follows); Submit jumps there (the live jump,
    // as before). Coordinates: type a seed to visit any planet (D2). New distress calls on cleared planets (D3): Career.CheckDistress.
    public sealed partial class GameShell
    {
        const int PlanetSlots = 8;
        int planetsFrom;   // the list's page (first shown entry)
        ViewStage stage;
        public ViewStage Stage => stage ? stage : stage = ViewStage.Create();
        bool coordEditing;

        List<int> PlanetOrder()
        {
            var c = Career.C; int today = Seeds.OfTheDay;
            var rest = c.planets.FindAll(p => p.seed != today);
            // Round 6: planets with new distress calls first, then the most recent
            rest.Sort((a, b) => a.distress != b.distress ? (a.distress ? -1 : 1) : string.CompareOrdinal(b.lastVisit ?? "", a.lastVisit ?? ""));
            var list = new List<int> { today };
            foreach (var p in rest) list.Add(p.seed);
            return list;
        }

        int SlotSeed(int slot) { var l = PlanetOrder(); int i = planetsFrom + slot; return i < l.Count ? l[i] : 0; }
        int NextNewSeed { get { var c = Career.C; int n = c.planetsMade; while (true) { int s = Career.MadePlanetSeed(c.systemSeed, n++); if (!c.planets.Exists(p => p.seed == s)) return s; } } }
        Biome BiomeOf(int seed) { var p = Career.C.planets.Find(x => x.seed == seed); return p != null ? Biomes.FromKey(p.biome) : Career.C.planets.Count == 0 ? Biome.Canyons : Biomes.ForSeed(seed); }

        string Status(int seed)
        {
            var p = Career.C.planets.Find(x => x.seed == seed);
            string s = p == null ? "not visited" : p.distress ? $"DISTRESS: {p.distressNew} new" : p.cleared ? "cleared" : $"{p.Count(PilotFate.Waiting)} waiting";
            if (p != null && p.Count(PilotFate.Lost) > 0) s += $", {p.Count(PilotFate.Lost)} lost";
            return (seed == Seeds.OfTheDay ? "★ of the day · " : "") + s;
        }

        string PlanetLine(int seed)
        {
            var p = Career.C.planets.Find(x => x.seed == seed);
            string name = p?.name ?? Names.Planet(seed), here = Planet != null && Planet.seed == seed ? "  ◄ here" : "";
            string col = p != null && p.distress ? "<color=#FF8A80>" : "";
            return $"{col}{name}{(col != "" ? "</color>" : "")}  <size=22>{Biomes.Name(BiomeOf(seed))} · {Status(seed)}{here}</size>";
        }

        // the seed the selected row points at (the map rings it): 0 = none
        int SelectedSeed()
        {
            int sel = planetsPage.Selected;
            if (sel == 0) return NextNewSeed;
            if (sel >= 1 && sel <= PlanetSlots) return SlotSeed(sel - 1);
            if (sel == PlanetSlots + 1 || sel == PlanetSlots + 2) return GameSettings.S.customSeed;   // coordinates
            return Planet != null ? Planet.seed : 0;
        }

        void BuildPlanetsPage()
        {
            planetsPage = new MenuPanel.Page { Title = "STAR MAP", Back = () => { coordEditing = editingSeed = false; Menu.Show(hangar); } };
            planetsPage.Subtitle = "";
            planetsPage.Add(new MenuPanel.Row { Label = () => "Jump to a new planet" + (Tuning.T.jumpCost > 0 ? $"  <size=22>({Tuning.T.jumpCost} cr)</size>" : "  <size=22>(free)</size>"), Submit = JumpToNew, Disabled = () => Career.C.credits < Tuning.T.jumpCost });
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
            // D2: coordinates: any planet by its seed (added to the map once visited)
            planetsPage.Add(new MenuPanel.Row
            {
                Label = () => "Coordinates  <size=22>(select to type a seed)</size>",
                Value = () => coordEditing ? SeedEditText() : GameSettings.S.customSeed.ToString(),
                Submit = () => { coordEditing = editingSeed = true; seedCursor = 7; },
            });
            planetsPage.Add(new MenuPanel.Row
            {
                Label = () => $"Go to the coordinates: {Names.Planet(Mathf.Max(1, GameSettings.S.customSeed))}  <size=22>(seed {GameSettings.S.customSeed})</size>",
                Submit = () => { int s = GameSettings.S.customSeed; if (s < 1 || Planet != null && Planet.seed == s) return; JumpTo(s); },
                Disabled = () => GameSettings.S.customSeed < 1 || Planet != null && Planet.seed == GameSettings.S.customSeed,
            });
            planetsPage.Add(new MenuPanel.Row
            {
                Label = () => "More planets…", Visible = () => PlanetOrder().Count > PlanetSlots,
                Submit = () => { planetsFrom += PlanetSlots; if (planetsFrom >= PlanetOrder().Count) planetsFrom = 0; planetsPage.Selected = 1; },
            });
            planetsPage.Add(Action("Back", () => { coordEditing = editingSeed = false; Menu.Show(hangar); }));
            planetsPage.Body = PlanetsBody;
            planetsPage.View = () => Stage.Tex;
            planetsPage.CaptureInput = () => { bool c = coordEditing && SeedEditInput(); if (!editingSeed) coordEditing = false; return c; };
            planetsPage.Hint = () => coordEditing ? $"{(Controls.LastDevice == Controls.Device.Keyboard ? "Type the seed (Backspace deletes), or " : "")}up / down changes the marked digit, left / right moves.   {Controls.Btn(Controls.Act.Select)}: done"
                                                  : $"{Controls.Btn(Controls.Act.Menus)}: choose a planet (the map rings it)     {Controls.Btn(Controls.Act.Select)}: jump there     {Controls.Btn(Controls.Act.Back)}: back";
        }

        void ShowPlanets() { planetsFrom = 0; mapKey = ""; Menu.Show(planetsPage); }

        // D1: danger: the difficulty, the planet's tier, its defences (as placed when visited; an estimate before)
        string Danger(int seed)
        {
            var c = Career.C; var p = c.planets.Find(x => x.seed == seed);
            int tier = p?.tier ?? c.stats.planetsCleared; bool first = p != null ? c.planets.IndexOf(p) == 0 : c.planets.Count == 0;
            string def;
            if (p != null && p.turrets >= 0)
            {
                int downT = 0, downS = 0; foreach (var id in p.enemiesDown) if (id >= 1000) downS++; else downT++;
                def = $"{p.turrets - downT} turrets{(p.saucers > 0 ? $", {p.saucers - downS} saucers" : "")} left";
            }
            else { var (et, es) = Enemies.Estimate(tier, first); def = $"about {et} turrets{(es > 0 ? $", {es} saucer{(es == 1 ? "" : "s")}" : "")} (not surveyed)"; }
            int pilots = p?.pilots.Length ?? Mathf.Min(Tuning.T.pilotsMax, Tuning.T.pilotsFirstPlanet + tier * Tuning.T.pilotsPerTier);
            return $"Danger: tier {tier} ({Difficulty.Name(c.difficulty)}) · {def} · {pilots} pilots{(first ? "" : ", aliens possible")}";
        }

        string PlanetsBody()
        {
            var c = Career.C; if (c == null) return "";
            var t = Tuning.T; int seed = SelectedSeed();
            int calls = c.planets.FindAll(p => p.distress).Count;
            string head = $"The {c.systemName} system: {c.planets.Count} planet{(c.planets.Count == 1 ? "" : "s")} visited, {c.stats.planetsCleared} cleared" + (calls > 0 ? $" · <color=#FF8A80>{calls} with new distress calls</color>" : "") + ".\n";
            var today = c.planets.Find(x => x.seed == Seeds.OfTheDay);   // Round 9 (E1): the planet of the day's best, picked out
            head += today != null && today.clears > 0 ? $"<color=#FFC873>★ Planet of the day {today.name}: {BestText(today)}</color>\n" : $"<color=#FFC873>★ Planet of the day {Names.Planet(Seeds.OfTheDay)}: no clear yet</color>\n";
            if (seed == 0) return head;
            var p = c.planets.Find(x => x.seed == seed); bool day = seed == Seeds.OfTheDay;
            int sel = planetsPage.Selected;
            string s = head + (sel == 0 ? "<b>Next new planet</b>: " : sel > PlanetSlots && sel <= PlanetSlots + 2 ? "<b>Coordinates</b>: " : "") +
                $"<b>{p?.name ?? Names.Planet(seed)}</b>  ({Biomes.Name(BiomeOf(seed))}, seed {seed}) · {Status(seed)}\n";
            if (day) s += $"<color=#FFC873>Planet of the day: pilots pay +{t.dayBonus * 100:0} % ({Mathf.RoundToInt(t.pilotPay * (1 + t.dayBonus))} cr each).</color>\n";
            s += Danger(seed) + "\n";
            if (p != null) s += $"Pilots: {p.Count(PilotFate.Rescued)} rescued · {p.Count(PilotFate.Lost)} lost · {p.Count(PilotFate.Waiting)} waiting · visits {p.visits}" + (p.distress ? $"\n<color=#FF8A80>New distress calls: {p.distressNew} pilot{(p.distressNew == 1 ? "" : "s")} down since you cleared it.</color>" : "");
            if (p != null && p.missions != null && p.missions.Count > 0) s += "\n<b>Missions</b>\n" + MissionsText(p, "");   // Round 9 (B2)
            if (p != null && p.clears > 0) s += $"\n<color=#FFE08A><b>Best run</b>: {BestText(p)}</color>";   // Round 9 (E1)
            if (Planet != null && Planet.seed == seed) s += "\nThe mothership is over this planet now.";
            return s;
        }

        // ---- the map in 3D (ViewStage)
        sealed class MapPlanet { public int seed; public Transform body; public LineRenderer ring, sel; public Text label; }
        readonly List<MapPlanet> map = new List<MapPlanet>();
        string mapKey = ""; float mapDist = 10; Material mapLine;

        static Color BiomeColour(Biome b) => b == Biome.Forest ? new Color(.35f, .8f, .4f) : b == Biome.City ? new Color(.62f, .68f, .8f) : b == Biome.Ice ? new Color(.85f, .95f, 1f)
            : b == Biome.Volcanic ? new Color(.85f, .2f, .1f) : b == Biome.Crystal ? new Color(.7f, .5f, 1f) : b == Biome.Ocean ? new Color(.25f, .55f, .95f) : new Color(.9f, .5f, .28f);

        List<int> MapSeeds()
        {
            var c = Career.C; var list = new List<int>();
            var visited = new List<PlanetState>(c.planets);
            if (visited.Count > 16) { visited.Sort((a, b) => string.CompareOrdinal(b.lastVisit ?? "", a.lastVisit ?? "")); visited = visited.GetRange(0, 16); visited.Sort((a, b) => c.planets.IndexOf(a).CompareTo(c.planets.IndexOf(b))); }
            foreach (var p in visited) list.Add(p.seed);
            if (!list.Contains(Seeds.OfTheDay)) list.Add(Seeds.OfTheDay);
            int next = NextNewSeed; if (!list.Contains(next)) list.Add(next);
            int typed = GameSettings.S.customSeed; if (typed > 0 && SelectedSeed() == typed && !list.Contains(typed)) list.Add(typed);
            return list;
        }

        LineRenderer MapRing(Transform parent, Color col, float width)
        {
            var l = new GameObject("Ring").AddComponent<LineRenderer>(); l.transform.SetParent(parent, false); l.useWorldSpace = false; l.loop = true; l.positionCount = 40;
            for (int i = 0; i < 40; i++) { float a = i * Mathf.PI * 2 / 40; l.SetPosition(i, new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a))); }
            l.sharedMaterial = mapLine; l.startColor = l.endColor = col; l.widthMultiplier = width; l.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; l.receiveShadows = false;
            return l;
        }

        void BuildMap(List<int> seeds)
        {
            var st = Stage; st.Clear(); map.Clear(); st.Lights(false);
            if (!mapLine) { mapLine = new Material(Shader.Find("Sprites/Default")); }
            var c = Career.C;
            Material Glow(Color col) { var m = new Material(Shader.Find("HostileHorizon/Glow")); m.SetColor("_Color", col); return m; }
            float R = 1.0f + .62f * seeds.Count; mapDist = R / Mathf.Tan(20f * Mathf.Deg2Rad) * 1.05f;
            st.Cam.fieldOfView = 40f;
            st.Cam.transform.localPosition = new Vector3(0, .78f, -1f).normalized * mapDist + new Vector3(0, 0, R * .08f);
            st.Cam.transform.LookAt(st.transform.position + new Vector3(0, 0, R * .1f));
            float u = mapDist * .045f;   // a planet's size: the same on the picture however big the system
            var star = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(star.GetComponent<Collider>()); star.name = "Star";
            star.transform.SetParent(st.Root, false); star.transform.localScale = Vector3.one * u * 1.3f; star.GetComponent<Renderer>().sharedMaterial = Glow(new Color(1f, .85f, .55f) * 2.2f);
            MapLabel(star.transform, $"<b>{c.systemName}</b>", new Color(1f, .85f, .55f), u * 1.7f);
            for (int i = 0; i < seeds.Count; i++)
            {
                int seed = seeds[i]; var p = c.planets.Find(x => x.seed == seed);
                float r = 1.0f + .62f * (i + 1), ang = 40f + i * 137.5f;   // the golden angle: neighbours never bunch up
                var orbit = MapRing(st.Root, new Color(.4f, 1f, .8f, .14f), u * .06f); orbit.transform.localScale = Vector3.one * r;
                var mp = new MapPlanet { seed = seed };
                var g = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(g.GetComponent<Collider>()); g.name = "Planet " + seed;
                g.transform.SetParent(st.Root, false); g.transform.localPosition = Quaternion.Euler(0, ang, 0) * Vector3.forward * r;
                bool known = p != null || seed == Seeds.OfTheDay;
                var col = BiomeColour(BiomeOf(seed));
                g.transform.localScale = Vector3.one * u * (p != null ? 1f : .75f);
                g.GetComponent<Renderer>().sharedMaterial = Glow(p != null ? col * 1.2f : col * .45f);
                mp.body = g.transform;
                Color rc = p == null ? new Color(.6f, .6f, .6f, .35f) : p.distress ? new Color(1f, .25f, .2f, 1) : p.cleared ? new Color(.45f, 1f, .55f, .8f) : new Color(1f, .7f, .25f, .9f);
                mp.ring = MapRing(st.Root, rc, u * .12f); mp.ring.transform.localPosition = g.transform.localPosition; mp.ring.transform.localScale = Vector3.one * u * .95f;
                if (seed == Seeds.OfTheDay) { var d = MapRing(st.Root, new Color(1f, .82f, .3f, .9f), u * .1f); d.transform.localPosition = g.transform.localPosition; d.transform.localScale = Vector3.one * u * 1.25f; }
                mp.sel = MapRing(st.Root, Color.white, u * .16f); mp.sel.transform.localPosition = g.transform.localPosition; mp.sel.transform.localScale = Vector3.one * u * 1.6f; mp.sel.gameObject.SetActive(false);
                string name = p?.name ?? (known ? Names.Planet(seed) : (seed == NextNewSeed ? "uncharted" : Names.Planet(seed)));
                mp.label = MapLabel(g.transform, $"<b>{name}</b>\n<size=24>{(p != null || seed == Seeds.OfTheDay ? Biomes.Name(BiomeOf(seed)) + " · " : "")}{Status(seed)}</size>", p != null && p.distress ? new Color(1f, .55f, .5f) : Color.white, u * .9f);
                map.Add(mp);
            }
            foreach (Transform t in st.Root) ViewStage.SetLayer(t.gameObject);
        }

        Text MapLabel(Transform at, string text, Color col, float below)
        {
            var cv = UiKit.WorldCanvas("Label", Stage.Root, new Vector2(600, 120), mapDist * .0011f);
            cv.transform.position = at.position + Vector3.back * below * .2f - Stage.Cam.transform.up * below;
            cv.transform.rotation = Stage.Cam.transform.rotation;
            var l = UiKit.Label(cv.transform, "Text", 30, TextAnchor.UpperCenter, col, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            l.text = text; l.horizontalOverflow = HorizontalWrapMode.Overflow; return l;
        }

        // every frame while the star map is shown: rebuilt when what it shows changes; the selected planet ringed, pulsing
        void MapTick()
        {
            var seeds = MapSeeds(); var c = Career.C;
            string key = string.Join(",", seeds) + "|" + string.Join(",", c.planets.ConvertAll(p => (p.distress ? "d" : "") + (p.cleared ? "c" : "") + p.Count(PilotFate.Waiting)));
            if (key != mapKey) { mapKey = key; BuildMap(seeds); }
            int sel = SelectedSeed(); float pulse = .7f + .3f * Mathf.Sin(Time.unscaledTime * 4f);
            foreach (var mp in map)
            {
                bool on = mp.seed == sel; mp.sel.gameObject.SetActive(on);
                if (on) mp.sel.startColor = mp.sel.endColor = new Color(1, 1, 1, pulse);
                mp.label.fontSize = on ? 34 : 24;
                var p = c.planets.Find(x => x.seed == mp.seed);
                if (p != null && p.distress) mp.ring.startColor = mp.ring.endColor = new Color(1f, .25f, .2f, pulse);
            }
        }

        // the stage camera draws only while a page shows its picture (the star map, the Field Journal viewer)
        void StageTick()
        {
            bool mapOn = Menu.Current == planetsPage && Menu.Visible && InCampaign;
            bool journalOn = Menu.Current == journalView && Menu.Visible;
            if (mapOn) MapTick(); else if (journalOn) JournalTick();
            if (stage) stage.Show(mapOn || journalOn);
            if (!mapOn) mapKey = "";
        }

        // ---- D3: distress calls (CampaignStart: on the map; at the dock: Control says so)
        string DistressCheck(bool announce)
        {
            var c = Career.C; c.CheckDistress(Planet.seed);
            var sb = new System.Text.StringBuilder();
            foreach (var p in c.planets)
                if (p.distress && !p.distressSaid && announce) { p.distressSaid = true; sb.AppendLine($"<color=#FF8A80>Distress calls from {p.name}: {p.distressNew} pilot{(p.distressNew == 1 ? "" : "s")} down (star map).</color>"); }
            if (sb.Length > 0) Voice.Say("ctl_distress", VoiceKind.Radio, 2, maxAge: 30f);
            return sb.ToString().TrimEnd();
        }
    }
}
