using System.Collections.Generic;
using UnityEngine;

namespace HostileHorizon
{
    // Round 4: the world's biome (props, weather, look) and the sound of the game: which music plays (B1), the stingers (B3), and
    // who says what when (C1, C2). The voices and music themselves are Audio/Voices.cs and Audio/Music.cs.
    public sealed partial class GameShell
    {
        public BiomeProps Props { get; private set; }
        public BiomeWeather Weather { get; private set; }
        public Music Music { get; private set; }
        public Voices Voice { get; private set; }
        public Biome WorldBiome => Terrain.Field != null ? Terrain.Field.Biome : Biome.Canyons;

        sealed class PilotTalk { public int voice; public bool wrong, borrowed, mayday, saw; public float doorAt = -99; public int doorCount; }
        readonly Dictionary<DownedPilot, PilotTalk> talk = new Dictionary<DownedPilot, PilotTalk>();
        int lastHome, lastHullBand; bool energyWarned, foundSaid, everyoneSaid, launchDaySaid; float pullUpAt = -99, gearAt = -99, enginesOffAt = -99, chatterAt, landedEnginesOn, brushAt = -99, lastGear;
        System.Random talkRoll = new System.Random(4);

        void CreateWorldExtras()
        {
            Props = BiomeProps.Create(WorldLook.RockMaterial); Props.SetDetail(WorldLook.DetailTex);
            Weather = BiomeWeather.Create();
            Weather.OnBlizzard += () => { if (Now == State.Flying) Voice.Say("cpu_whiteout", VoiceKind.Computer, 3); };
            Music = Music.Create(); Voice = Voices.Create();
            Ship.OnEnginesStopped += () => { if (InCampaign && Time.time - enginesOffAt > 15f) { enginesOffAt = Time.time; Voice.Say("cpu_engines_off", VoiceKind.Computer, 3, maxAge: 3f); } };
        }

        // the world of a seed in a biome: ground, props, look, air
        void SetWorld(int seed, Biome biome)
        {
            if (Terrain.Field == null || Terrain.Field.SeedNumber != seed || Terrain.Field.Biome != biome)
            {
                Terrain.SetField(new CanyonField(seed, biome));
                Props.SetField(Terrain.Field);
            }
            WorldLook.ApplyBiome(biome, Props);
            Weather.SetBiome(biome, seed);
        }

        public static string FlightTrack(Biome b) => "flight_" + Biomes.Keys[(int)b];

        // every frame (GameShell.Update)
        void WorldTick()
        {
            var view = Rig.Head.transform.position;
            Props.NearRange = SteamDeck.On ? 420f : 650f;
            Props.SetFocus(view, Terrain.ViewDistance);
            bool flying = Now == State.Flying;
            Weather.Tick(view, flying, Time.deltaTime);
            MusicTick();
            if (flying && Ship.gameObject.activeInHierarchy && !Ship.Crashed) { CanopyBrush(); ComputerTick(); if (InCampaign) VoiceTick(); }
            else landedEnginesOn = 0;
            Displays.Subtitle = Voice.Subtitle;
        }

        // ---- music (B1)
        bool NearWaitingPilot()
        {
            foreach (var p in Pilots) if (p.OnFoot && Vector3.Distance(p.Position, Ship.transform.position) < 700f && Ship.AboveGround < 250f) return true;
            foreach (var p in Pilots) if (p.Now == DownedPilot.State.Running || p.Now == DownedPilot.State.Knocking || p.Now == DownedPilot.State.Boarding) return true;
            return false;
        }

        void MusicTick()
        {
            string want = Music.Want; bool tension = false; float wind = 0;
            var st = Now == State.Paused ? pausedFrom : Now;
            if (Now == State.Title || Now == State.Loading && Phase == "" && !InCampaign) want = "title_theme";
            else if (Phase == "horror" || CanopyAlien != null || sleeperTurning || Phase == "dead") want = "";   // sound only: the scares
            else if (Phase == "regen-trip") want = "regeneration";
            else if (Phase == "regen" || Phase == "recovery" || Phase == "docked" || Phase == "jump")
                want = Menu.Current == planetsPage || Phase == "jump" ? "planet_select" : "hangar";
            else if (st == State.Crashed) want = "";
            else if (st == State.Flying || st == State.Auto)
            {
                bool quiet = InCampaign && st == State.Flying && (Ship.Landed || NearWaitingPilot());
                want = quiet ? "" : FlightTrack(WorldBiome);
                if (quiet) wind = .22f;
                tension = InCampaign && st == State.Flying && !quiet && (Ship.Energy < Tuning.T.energyWarning || HomeState > 0);
            }
            Music.Want = want; Music.Tension = tension;
            if (shipAudio) shipAudio.Wind = Ship.gameObject.activeInHierarchy ? Mathf.Max(wind, Weather.Blizzard * .5f) : 0;
        }

        // ---- the forest canopy: soft, a burst of leaves and a rustle, a little drag
        void CanopyBrush()
        {
            if (WorldBiome != Biome.Forest) return;
            float k = Terrain.Field.InCanopy(Ship.transform.position);
            if (k <= 0) return;
            if (Time.time - brushAt > .35f)
            {
                brushAt = Time.time;
                Weather.Leaves(Ship.transform.position + Ship.transform.forward * 6f, Ship.Velocity, 30 + (int)(k * 60));
                Sfx.Play(Sfx.Rustle, Ship.transform.position, .7f, 10f, Ship.transform);
            }
        }

        // ---- the ship's computer (C1): short warnings, clean and close
        void ComputerTick()
        {
            var t = Tuning.T;
            if (!Ship.Landed && Ship.Mode == FlightMode.Flight && Ship.AboveGround < 30f && Ship.Velocity.y < -10f && Time.time - pullUpAt > 8f)
            { pullUpAt = Time.time; Voice.Say("cpu_pull_up", VoiceKind.Computer, 3, maxAge: 1.5f); }
            if (!InCampaign) return;
            if (Ship.Gear > .95f && lastGear <= .95f && Ship.AboveGround < 200f && Time.time - gearAt > 25f) { gearAt = Time.time; Voice.Say("cpu_gear_down", VoiceKind.Computer, 3, maxAge: 3f); }
            lastGear = Ship.Gear;
            if (Ship.Energy < t.energyWarning && !energyWarned) { energyWarned = true; Voice.Say("cpu_energy_low", VoiceKind.Computer, 3); Voice.Say("ctl_low_energy", VoiceKind.Radio, 2, maxAge: 15f); }
            if (Ship.Energy > t.energyWarning + 5) energyWarned = false;
            if (HomeState != lastHome) { if (HomeState == 1) Voice.Say("cpu_return_now", VoiceKind.Computer, 3); if (HomeState == 2) Voice.Say("cpu_cant_reach", VoiceKind.Computer, 3); lastHome = HomeState; }
            int band = Ship.Damage >= 80 ? 2 : Ship.Damage >= 50 ? 1 : 0;
            if (band > lastHullBand) Voice.Say(band == 2 ? "cpu_hull_critical" : "cpu_hull_damage", VoiceKind.Computer, 3);
            lastHullBand = band;
        }

        // ---- Control and the pilots (C1, C2)
        PilotTalk Talk(DownedPilot p)
        {
            if (talk.TryGetValue(p, out var k)) return k;
            k = new PilotTalk { voice = Voice.PilotVoice(Planet.seed, p.Index) };
            // C2: the Mimic's voice sign, some of the time (Round 3's tell rules): wrong lines, or the voice and callsign of a pilot
            // already lost on this planet
            if (p.Type == AlienType.Mimic)
            {
                var r = new SeedRng(Planet.seed ^ (p.Index * 131), 77);
                k.wrong = r.Value < Tuning.T.tellReliability;
                if (k.wrong && r.Value < .5f)
                    for (int j = 0; j < Planet.pilots.Length; j++)
                        if (Planet.pilots[j] == (int)PilotFate.Lost) { k.voice = Voice.PilotVoice(Planet.seed, j); k.borrowed = true; break; }
            }
            talk[p] = k; return k;
        }

        public bool MimicVoiceSign(DownedPilot p) => p.Type == AlienType.Mimic && Talk(p).wrong;   // checks
        string P(PilotTalk k, string line) => $"p{k.voice}_{line}";

        void VoiceTick()
        {
            var t = Tuning.T;
            foreach (var p in Pilots)
            {
                var k = Talk(p);
                float d = Vector3.Distance(p.Position, Ship.transform.position);
                if (p.OnFoot && !k.mayday && d < 3000f && p.Now == DownedPilot.State.Waiting)
                {
                    k.mayday = true;
                    if (!foundSaid) { foundSaid = true; Voice.Say(talkRoll.Next(2) == 0 ? "ctl_found_1" : "ctl_found_2", VoiceKind.Radio, 2); }
                    string line = k.wrong && !k.borrowed ? "mayday_wrong" : (p.Index % 2 == 0 ? "mayday_" + Biomes.Keys[(int)WorldBiome] : "mayday");
                    Voice.Say(P(k, line), VoiceKind.Radio, 1, maxAge: 20f);
                }
                if (p.Now == DownedPilot.State.Running && !k.saw) { k.saw = true; Voice.Say(P(k, talkRoll.Next(2) == 0 ? "see_1" : "see_2"), VoiceKind.Radio, 1, maxAge: 4f); }
                if (p.Now == DownedPilot.State.Knocking && Time.time - k.doorAt > 9f)
                {
                    k.doorAt = Time.time; k.doorCount++;
                    string line = k.wrong && !k.borrowed && k.doorCount % 2 == 1 ? "door_wrong" : (k.doorCount % 2 == 1 ? "door_1" : "door_2");
                    Voice.Say(P(k, line), VoiceKind.Door, 1, Ship.transform, DownedPilot.DoorOutside + Vector3.up * 1.4f, maxAge: 3f);
                }
            }
            // landed near a pilot with the engines still running
            bool pilotCanCome = false; foreach (var p in Pilots) if (p.OnFoot && p.CanReach) pilotCanCome = true;
            if (Ship.Landed && Ship.EnginesOn && pilotCanCome) { landedEnginesOn += Time.deltaTime; if (landedEnginesOn > 5f && landedEnginesOn - Time.deltaTime <= 5f) Voice.Say("ctl_engines_off", VoiceKind.Radio, 2); }
            else landedEnginesOn = 0;
            // everyone found
            if (Waiting == 0 && cabin.Count > 0 && cabin.Count < t.cabinSize && !everyoneSaid) { everyoneSaid = true; Voice.Say("ctl_everyone", VoiceKind.Radio, 2); }
            // cabin chatter: now and then, from a passenger
            if (cabin.Count > 0 && !sleeperTurning)
            {
                if (chatterAt <= 0) chatterAt = Time.time + 45f + (float)talkRoll.NextDouble() * 50f;
                if (Time.time > chatterAt)
                {
                    chatterAt = Time.time + 50f + (float)talkRoll.NextDouble() * 60f;
                    var who = Pilots.Find(x => x.Index == cabin[talkRoll.Next(cabin.Count)]);
                    if (who) Voice.Say(P(Talk(who), "chat_" + (1 + talkRoll.Next(3))), VoiceKind.Cabin, 0, Ship.transform, new Vector3(0, .8f, -3.5f), maxAge: 5f);
                }
            }
            else chatterAt = 0;
        }

        // ---- events from the Campaign
        void SayLaunch()
        {
            Music.Sting("sting_launch");
            Voice.Say(talkRoll.Next(2) == 0 ? "ctl_launch_1" : "ctl_launch_2", VoiceKind.Radio, 2);
            if (Planet != null && Planet.OfTheDay && !launchDaySaid) { launchDaySaid = true; Voice.Say("ctl_day", VoiceKind.Radio, 2, maxAge: 20f); }
            foundSaid = everyoneSaid = energyWarned = false; lastHome = 0; lastHullBand = Ship.Damage >= 80 ? 2 : Ship.Damage >= 50 ? 1 : 0;
            foreach (var k in talk.Values) k.saw = false;
        }
        void SayBriefing() { Voice.Say("ctl_brief_" + Biomes.Keys[(int)WorldBiome], VoiceKind.Radio, 2, maxAge: 30f); }
        void SayArrived(bool newPlanet) { launchDaySaid = false; talk.Clear(); if (newPlanet) SayBriefing(); }
        void SayDockApproach() => Voice.Say(talkRoll.Next(2) == 0 ? "ctl_dock_1" : "ctl_dock_2", VoiceKind.Radio, 2);
        void SayDocked() { Music.Sting("sting_docked"); Voice.Say(talkRoll.Next(2) == 0 ? "ctl_docked_1" : "ctl_docked_2", VoiceKind.Radio, 2); }
        void SayCleared() { Music.Sting("sting_planet_cleared"); Voice.Say("ctl_cleared", VoiceKind.Radio, 2, maxAge: 20f); }
        void SayBoarded(DownedPilot p)
        {
            Music.Sting("sting_pilot_aboard"); Voice.Say("cpu_pilot_aboard", VoiceKind.Computer, 3);
            Voice.Say(P(Talk(p), talkRoll.Next(2) == 0 ? "thanks_1" : "thanks_2"), VoiceKind.Cabin, 1, Ship.transform, new Vector3(0, .8f, -3f), maxAge: 8f);
            if (cabin.Count >= Tuning.T.cabinSize) { Voice.Say("cpu_cabin_full", VoiceKind.Computer, 3); Voice.Say("ctl_full", VoiceKind.Radio, 2, maxAge: 15f); }
        }
        void SayScream(int victim)
        {
            // the Sleeper's victim screams (through the bulkhead)
            var who = Pilots.Find(x => x.Index == victim);
            if (who) Voice.Say(P(Talk(who), "scream"), VoiceKind.Cabin, 3, Ship.transform, new Vector3(0, .8f, -3.5f), maxAge: 2f);
        }
    }
}
