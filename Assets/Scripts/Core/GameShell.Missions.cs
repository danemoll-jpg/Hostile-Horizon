using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace HostileHorizon
{
    // Round 9 (B1-B3): side missions on each planet (Campaign/Missions.cs: what they are and how they are chosen). Here: made on
    // arrival, the crash site in the world, the turret nest handed to the enemies, the clock and the hull watched in flight, the
    // recorder pulled aboard (landed, engines off, airlock button held), Control's briefing at launch and calls, the lines on the
    // displays and the radar, the reward at the dock, the hangar and star-map text.
    public sealed partial class GameShell
    {
        GameObject siteMarker; float recorderHold, launchDamage;
        public float RecorderHold => recorderHold;   // checks
        public Mission MissionOf(MissionKind k) => Planet?.missions?.Find(m => m.Kind == k);

        // on arrival (after the pilots, before the enemies): the planet's missions, once; its crash site in the world
        void EnsureMissions()
        {
            if (Planet.missions == null) Planet.missions = new List<Mission>();
            if (!Planet.missionsSet)
            {
                Terrain.Field.FindStart(out var start, out _);
                Planet.missions = Missions.Make(Planet, Terrain.Field, sitePads, start, Difficulty.Enemies > 0);
                Planet.missionsSet = true;
            }
            PlayLog.Add("missions: " + (Planet.missions.Count == 0 ? "none" : string.Join(", ", Planet.missions.ConvertAll(m => $"{Missions.Names[m.kind]} ({(MissionState)m.state})"))));
            PlaceSiteMarker();
        }

        void PlaceSiteMarker()
        {
            if (siteMarker) Destroy(siteMarker);
            var m = MissionOf(MissionKind.Recorder);
            if (m == null || !m.Open || m.aboard) return;
            siteMarker = MakeCrashSite(m.Site(Terrain.Field), "Flight recorder crash site", new Color(.4f, 1f, 1f));
        }

        // a small crashed craft, a blinking light and a tall glowing post (seen from far off, the same in VR)
        GameObject MakeCrashSite(Vector3 at, string name, Color col)
        {
            var root = new GameObject(name); root.transform.position = at;
            var dark = new Material(litTemplate); dark.SetColor("_BaseColor", new Color(.12f, .11f, .1f)); dark.SetFloat("_Smoothness", .1f);
            var glow = new Material(Shader.Find("HostileHorizon/Glow")); glow.SetColor("_Color", col * 2f);
            void Part(PrimitiveType t, Vector3 p, Vector3 s, Vector3 r, Material m) { var g = GameObject.CreatePrimitive(t); Destroy(g.GetComponent<Collider>()); g.transform.SetParent(root.transform, false); g.transform.localPosition = p; g.transform.localScale = s; g.transform.localRotation = Quaternion.Euler(r); g.GetComponent<Renderer>().sharedMaterial = m; }
            Part(PrimitiveType.Cube, new Vector3(18, .6f, 6), new Vector3(2.2f, 1.3f, 6f), new Vector3(8, 35, 14), dark);
            Part(PrimitiveType.Cube, new Vector3(16, .9f, 3.5f), new Vector3(.2f, 1.6f, 1.4f), new Vector3(0, 35, 30), dark);
            Part(PrimitiveType.Cube, new Vector3(20.5f, .2f, 7.5f), new Vector3(3.5f, .15f, 1.4f), new Vector3(0, 80, 6), dark);
            Part(PrimitiveType.Sphere, new Vector3(18, 1.6f, 6), Vector3.one * .6f, Vector3.zero, glow);
            var post = new GameObject("Site post").AddComponent<LineRenderer>(); post.transform.SetParent(root.transform, false);
            post.sharedMaterial = glow; post.positionCount = 2; post.widthMultiplier = 1.2f; post.useWorldSpace = false;
            post.SetPosition(0, new Vector3(18, 1, 6)); post.SetPosition(1, new Vector3(18, 90, 6));
            post.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; post.receiveShadows = false;
            return root;
        }

        // at launch: the clock's start point for the clean run, Control's briefing (the open missions)
        void MissionsAtLaunch()
        {
            launchDamage = Ship.Damage; recorderHold = 0;
            if (Planet?.missions == null) return;
            int said = 0;
            foreach (var m in Planet.missions) if (m.Open && said < 2) { Voice.Say(Missions.Voice[m.kind], VoiceKind.Radio, 2, maxAge: 40f); said++; }
            if (Planet.missions.Exists(m => m.Open)) PlayLog.Add("missions open: " + string.Join(", ", Planet.missions.FindAll(m => m.Open).ConvertAll(m => Missions.Names[m.kind])));
        }

        // in flight, every frame (after the radar's contacts are made)
        void MissionsTick(float dt)
        {
            Displays.Objectives = "";
            if (Planet?.missions == null || Planet.missions.Count == 0) return;
            var lines = new List<string>();
            foreach (var m in Planet.missions)
            {
                string head = $"◆ {Missions.Short[m.kind]}";
                switch (m.Kind)
                {
                    case MissionKind.Recorder:
                        if (!m.Open) { lines.Add($"{head} {(m.State == MissionState.Failed ? "✗" : "✓")}"); break; }
                        if (m.aboard) { lines.Add($"{head}: aboard, dock to deliver"); break; }
                        var site = m.Site(Terrain.Field);
                        Displays.Contacts.Add(new RadarContact { position = site, mark = "RECORDER" });
                        float d = Flat(site - Ship.transform.position).magnitude;
                        bool there = Ship.Landed && d < Tuning.T.missionRecorderRange;
                        if (there && !Ship.EnginesOn && Controls.AirlockHeld && Ship.InputEnabled)
                        {
                            recorderHold += dt;
                            Flash($"PULLING THE RECORDER ABOARD  {recorderHold:0} / {Tuning.T.missionRecorderHold:0} s  (keep holding {Controls.AirlockKey})", .3f);
                            if (recorderHold >= Tuning.T.missionRecorderHold)
                            {
                                m.aboard = true; recorderHold = 0; if (siteMarker) Destroy(siteMarker);
                                Voice.Say("ctl_obj_recorder_aboard", VoiceKind.Radio, 2); Sfx.Play(Sfx.Thunk, Ship.transform.position, .7f, 8f);
                                Flash("Flight recorder aboard: dock to deliver it", 5f); PlayLog.Add("mission: flight recorder aboard");
                            }
                        }
                        else
                        {
                            recorderHold = 0;
                            if (there && Ship.InputEnabled && Time.time >= flashUntil) Flash(Ship.EnginesOn ? $"At the crash site: engines off ({Controls.EngineKey}), then hold {Controls.AirlockKey}" : $"At the crash site: hold {Controls.AirlockKey} to pull the recorder aboard", .5f);
                        }
                        lines.Add($"{head}: land at the crash site · {RadarScope.Distance(d)}");
                        break;
                    case MissionKind.Turrets:
                        int left = 0, all = 0; foreach (var e in Foes.All) if (e.Nest) { all++; if (!e.Dead) left++; }
                        if (m.Open && all > 0 && left == 0) { m.State = MissionState.Done; Voice.Say("ctl_obj_turrets_done", VoiceKind.Radio, 2); Flash("Turret nest destroyed: paid at the dock", 5f); PlayLog.Add("mission: turret nest destroyed"); }
                        if (m.Open) { Displays.Contacts.Add(new RadarContact { position = m.Site(Terrain.Field), mark = "NEST" }); lines.Add($"{head}: {left} / {all} left"); }
                        else lines.Add($"{head} {(m.State == MissionState.Failed ? "✗" : "✓")}");
                        break;
                    case MissionKind.Timed:
                        if (!m.Open) { lines.Add($"{head} P{m.pilot + 1} {(m.State == MissionState.Failed ? "✗ lost" : "✓")}"); break; }
                        if (Planet.pilots[m.pilot] == (int)PilotFate.Lost || Planet.pilots[m.pilot] == (int)PilotFate.AlienGone) { m.State = MissionState.Failed; break; }
                        m.timeLeft -= dt;
                        for (int i = 0; i < Displays.Contacts.Count; i++) if (Displays.Contacts[i].number == m.pilot + 1 && !Displays.Contacts[i].mothership && Displays.Contacts[i].enemy == 0 && Displays.Contacts[i].mark == null) { var c = Displays.Contacts[i]; c.tag = Missions.Clock(m.timeLeft); Displays.Contacts[i] = c; }
                        if (m.timeLeft <= 0) { LifeSupportOut(m); break; }
                        lines.Add($"{head} P{m.pilot + 1}: {Missions.Clock(m.timeLeft)}{(cabin.Contains(m.pilot) ? " (aboard: dock!)" : "")}");
                        break;
                    case MissionKind.Clean:
                        if (m.Open && Ship.Damage > launchDamage + .01f) { m.State = MissionState.Failed; Voice.Say("ctl_obj_clean_broken", VoiceKind.Radio, 2, maxAge: 10f); PlayLog.Add($"mission: clean run broken (hull {100 - Ship.Damage:0} %)"); }
                        lines.Add(m.Open ? $"{head}: no damage so far" : $"{head} {(m.State == MissionState.Failed ? "✗" : "✓")}");
                        break;
                }
            }
            Displays.Objectives = string.Join("\n", lines);
        }

        // the timed pilot's life support runs out: lost where they are (on the ground, walking in, or in the cabin)
        void LifeSupportOut(Mission m)
        {
            m.timeLeft = 0; m.State = MissionState.Failed;
            var p = Pilots.Find(x => x.Index == m.pilot);
            if (cabin.Contains(m.pilot)) { cabin.Remove(m.pilot); Reseat(); }
            else if (p != null && p.OnFoot) p.Kill(false);
            else if (p != null && p.Now == DownedPilot.State.Boarding) p.Hide();
            if (Planet.pilots[m.pilot] == (int)PilotFate.Waiting) { Planet.pilots[m.pilot] = (int)PilotFate.Lost; Career.C.stats.pilotsLost++; }
            Voice.Say("ctl_obj_timed_lost", VoiceKind.Radio, 2, maxAge: 20f);
            Flash($"Pilot {m.pilot + 1}'s life support ran out. They're gone.", 6f);
            PlayLog.Add($"mission: pilot {m.pilot + 1}'s life support ran out");
        }

        // at the dock, after the pilots are paid and the planet's clear checked: what's done is paid
        void SettleMissions(StringBuilder sb)
        {
            if (Planet?.missions == null) return;
            var c = Career.C; int total = 0; var paid = new List<string>();
            foreach (var m in Planet.missions)
            {
                if (m.Kind == MissionKind.Recorder && m.Open && m.aboard) m.State = MissionState.Done;
                if (m.Kind == MissionKind.Timed && m.Open && Planet.pilots[m.pilot] == (int)PilotFate.Rescued) m.State = MissionState.Done;
                if (m.Kind == MissionKind.Clean && m.Open && Planet.cleared) m.State = MissionState.Done;
                if (m.State != MissionState.Done) continue;
                int r = Missions.Reward(m.Kind); c.Earn(r); total += r; m.State = MissionState.Paid; c.stats.missionsDone++;
                paid.Add($"{Missions.Short[m.kind].ToLower()} +{r}");
                PlayLog.Add($"mission paid: {Missions.Names[m.kind]} +{r} cr");
            }
            if (total > 0) { sb.AppendLine($"<color=#8FFFFF>Missions paid: {string.Join(", ", paid)} cr</color>"); Voice.Say("ctl_obj_paid", VoiceKind.Radio, 2, maxAge: 15f); }
        }

        // death: the recorder aboard goes back down with the wreck (it can be fetched again); the clean run is over
        void MissionsOnDeath()
        {
            if (Planet?.missions == null) return;
            foreach (var m in Planet.missions)
            {
                if (m.Kind == MissionKind.Recorder && m.Open && m.aboard) m.aboard = false;
                if (m.Kind == MissionKind.Clean && m.Open) m.State = MissionState.Failed;
            }
            PlaceSiteMarker();
        }

        // the hangar (one line) and the star map (a line each)
        string MissionsLine(PlanetState p)
        {
            if (p?.missions == null || p.missions.Count == 0) return "";
            return "  <b>Missions</b>: " + string.Join(" · ", p.missions.ConvertAll(m => m.State == MissionState.Paid || m.State == MissionState.Done ? $"<color=#9CFFB0>{Missions.Short[m.kind].ToLower()} ✓</color>" :
                m.State == MissionState.Failed ? $"<color=#FF8A80>{Missions.Short[m.kind].ToLower()} ✗</color>" : $"{Missions.Short[m.kind].ToLower()} ({Missions.Reward(m.Kind)} cr{(m.Kind == MissionKind.Timed ? ", " + Missions.Clock(m.timeLeft) : "")})")) + "\n";
        }

        string MissionsText(PlanetState p, string indent = "  ")
        {
            if (p?.missions == null || p.missions.Count == 0) return "";
            var sb = new StringBuilder();
            foreach (var m in p.missions)
            {
                string st = m.State == MissionState.Paid ? "<color=#9CFFB0>✓ done</color>" : m.State == MissionState.Done ? "<color=#9CFFB0>✓ paid at the dock</color>" : m.State == MissionState.Failed ? "<color=#FF8A80>✗ failed</color>" :
                    m.Kind == MissionKind.Timed ? $"<color=#FFC873>{Missions.Clock(m.timeLeft)} left for pilot {m.pilot + 1}</color>" : m.Kind == MissionKind.Recorder && m.aboard ? "aboard" : m.Kind == MissionKind.Turrets ? $"{m.count} turrets" : "open";
                sb.Append($"{indent}◆ {Missions.Names[m.kind]}  <size=22>({Missions.Reward(m.Kind)} cr)</size>  {st}\n");
            }
            return sb.ToString();
        }
    }
}
