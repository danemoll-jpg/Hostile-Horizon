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
        public Liquid Sea { get; private set; }        // Round 7: the sea or the lava
        public Fliers Birds { get; private set; }      // Round 7 (B4): the ocean's harmless fliers
        public int ShardsBroken { get; private set; }  // checks
        public int WaterTouches { get; private set; }  // checks
        float heatSaidAt = -99, sprayAt;
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
            // Round 7 (B2-B4): the liquid, the fliers; what the water and the shards do to the ship shows here
            Sea = Liquid.Create(WorldLook.DetailTex); Birds = Fliers.Create();
            Ship.OnWater += (at, hard) =>
            {
                WaterTouches++;
                if (Time.time - sprayAt < .08f && hard < Tuning.T.waterHardSpeed) return;
                sprayAt = Time.time; Weather.Spray(at, Ship.Velocity, hard + Ship.Speed * .05f);
                if (hard > 3f) Sfx.Play(Sfx.Clip("water_splash"), at, Mathf.Clamp01(.4f + hard * .05f), 12f);
            };
            Ship.OnShard += sh =>
            {
                ShardsBroken++; var at = sh.basePos + sh.axis * sh.height * .4f;
                Weather.Shatter(at, Ship.Velocity); Props.ShardBroken(); Sfx.Play(Sfx.Clip("crystal_shatter"), at, 1f, 15f);
                PlayLog.Add($"crystal shard broken at {at:F0}");
            };
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
            Sea.SetBiome(Terrain.Field, WorldLook.LookFor(biome)); Birds.SetBiome(biome, seed);
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
            Sea.Follow(view, Terrain.ViewDistance); Weather.Plumes(Props.VentsNear, view); Birds.Tick(view, Terrain.Field, Time.deltaTime);
            LiquidTick(flying);
            MusicTick();
            AmbienceTick();
            if (flying && Ship.gameObject.activeInHierarchy && !Ship.Crashed) { CanopyBrush(); ComputerTick(); if (InCampaign) VoiceTick(); }
            else landedEnginesOn = 0;
            Displays.Subtitle = Voice.Subtitle;
        }

        // ---- music (B1)
        // Round 7 (A1): 0.6.3 went quiet whenever any pilot on foot was within 700 m and the ship under 250 m over the ground, and
        // whenever landed: flying low in the canyons that was most of the flight. Now the flight music drops out only for the pickup
        // itself: a pilot running / knocking / boarding, or landed / hovering within pickupQuietRange of the pilot you are going for
        // (the route's pilot, else the nearest one waiting). Flying past a pilot never silences it.
        public string PickupQuiet()
        {
            foreach (var p in Pilots)
                if (p.Now == DownedPilot.State.Running || p.Now == DownedPilot.State.Knocking || p.Now == DownedPilot.State.Boarding) return $"pilot {p.Index + 1} {p.Now.ToString().ToLower()}";
            if (!(Ship.Landed || Ship.Mode == FlightMode.Hover)) return null;
            var target = RoutePilot != null && RoutePilot.OnFoot ? RoutePilot : NearestWaiting(out _);
            if (target == null) return null;
            float d = Flat(target.Position - Ship.transform.position).magnitude;
            return d < Tuning.T.pickupQuietRange ? $"{(Ship.Landed ? "landed" : "hovering")} {d:0} m from pilot {target.Index + 1}" : null;
        }
        // the 0.6.3 rule, kept for the check's comparison only
        public bool OldMusicQuiet()
        {
            if (Ship.Landed) return true;
            foreach (var p in Pilots) if (p.OnFoot && Vector3.Distance(p.Position, Ship.transform.position) < 700f && Ship.AboveGround < 250f) return true;
            foreach (var p in Pilots) if (p.Now == DownedPilot.State.Running || p.Now == DownedPilot.State.Knocking || p.Now == DownedPilot.State.Boarding) return true;
            return false;
        }

        string musicWhyLogged = "?";
        // Round 8 (B2): the place's own sound, quietly under the engines and the music: each biome's ambience on a planet (louder with
        // the engines off or landed), the regeneration bay's hum where you wake; crossfaded (1.5 s) when it changes. Effects volume,
        // tuning ambienceMix; ducks under voices with the effects.
        AudioSource amb; string ambName = ""; float ambFade;
        public string Ambience => ambName;   // checks
        void AmbienceTick()
        {
            if (!amb) { amb = new GameObject("Ambience").AddComponent<AudioSource>(); amb.loop = true; amb.spatialBlend = 0; amb.playOnAwake = false; amb.volume = 0; }
            bool onPlanet = Ship && Ship.gameObject.activeInHierarchy && (Now == State.Flying || (Now == State.Auto && Phase != "horror" && Phase != "regen-trip" && Phase != "docking" && Phase != "launch" && !Ship.Kinematic));
            string want = Phase == "regen" ? "regen_bay" : onPlanet && Terrain.Field != null ? "amb_" + Biomes.Keys[(int)WorldBiome] : "";
            float target = want == "" ? 0 : Sfx.Effects * Tuning.T.ambienceMix * (want == "regen_bay" ? 1f : Ship.EnginesOn && !Ship.Landed ? .55f : 1f);
            if (want != ambName) { ambFade = Mathf.MoveTowards(amb.volume, 0, Time.unscaledDeltaTime / 1.5f); amb.volume = ambFade; if (ambFade <= 0) { ambName = want; amb.Stop(); if (want != "") { amb.clip = Sfx.Clip(want); amb.Play(); } } }
            else amb.volume = Mathf.MoveTowards(amb.volume, target, Time.unscaledDeltaTime / 1.5f);
        }

        void MusicTick()
        {
            string want = Music.Want, why; bool tension = false; float wind = 0;
            var st = Now == State.Paused ? pausedFrom : Now;
            if (Now == State.Title || Now == State.Loading && Phase == "" && !InCampaign) { want = "title_theme"; why = "title"; }
            else if (Phase == "horror" || CanopyAlien != null || sleeperTurning || Phase == "dead") { want = ""; why = Phase == "horror" ? "alien attack" : CanopyAlien != null ? "alien on the canopy" : sleeperTurning ? "the Sleeper turns" : "dead"; }   // sound only: the scares
            else if (Phase == "regen-trip") { want = "regeneration"; why = "regeneration"; }
            else if (Phase == "regen" || Phase == "recovery" || Phase == "docked" || Phase == "jump")
            { want = Menu.Current == planetsPage || Phase == "jump" ? "planet_select" : "hangar"; why = Phase; }
            else if (st == State.Crashed) { want = ""; why = "crashed"; }
            else if (st == State.Flying || st == State.Auto)
            {
                string quiet = InCampaign && st == State.Flying ? PickupQuiet() : null;
                want = quiet != null ? "" : FlightTrack(WorldBiome);
                why = quiet != null ? "pickup: " + quiet : st == State.Auto ? "flight (automatic)" : "flying";
                if (quiet != null) wind = .22f;
                tension = InCampaign && st == State.Flying && quiet == null && (Ship.Energy < Tuning.T.energyWarning || HomeState > 0);
                if (tension) why += Ship.Energy < Tuning.T.energyWarning ? " + tension (low energy)" : " + tension (home warning)";
            }
            else why = st.ToString().ToLower();
            if (CheckRun.Active && Music.CheckWant != null) { want = Music.CheckWant; tension = Music.CheckTension; why = "check"; }   // the music check
            string key = (want == "" ? "(quiet)" : want) + (tension ? " +tension" : "");
            var coarse = new System.Text.StringBuilder(key); foreach (char ch in why) if (!char.IsDigit(ch)) coarse.Append(ch);   // a change of reason, not of the metres in it
            if (coarse.ToString() != musicWhyLogged && Now != State.Paused) { PlayLog.Add($"music {key}: {why}"); musicWhyLogged = coarse.ToString(); }
            Music.Want = want; Music.Tension = tension;
            if (shipAudio) shipAudio.Wind = Ship.gameObject.activeInHierarchy ? Mathf.Max(wind, Weather.Blizzard * .5f) : 0;
        }

        // ---- Round 7 (B2, B4): low over lava: heat haze and HULL HEAT (a warning, no damage); low and fast over the sea: spray
        public bool HullHot { get; private set; }
        void LiquidTick(bool flying)
        {
            var f = Terrain.Field; var sp = Ship.transform.position;
            bool over = flying && Ship.gameObject.activeInHierarchy && !Ship.Crashed && f != null && f.OverLiquid(sp.x, sp.z);
            float agl = Ship.AboveGround;
            HullHot = over && f.IsLava && agl < Tuning.T.lavaHeatHeight && !Ship.Kinematic;
            Weather.Heat(new Vector3(sp.x, f != null && f.HasLiquid ? f.Liquid : 0, sp.z), over && f.IsLava ? Mathf.Clamp01(1.4f - agl / (Tuning.T.lavaHeatHeight * 1.6f)) : 0);
            Displays.HeatWarning = HullHot ? "HULL HEAT: climb" : "";
            if (HullHot && Time.time - heatSaidAt > 20f) { heatSaidAt = Time.time; Voice.Say("cpu_hull_heat", VoiceKind.Computer, 3, maxAge: 2f); PlayLog.Add($"hull heat: {agl:0} m over the lava"); }
            if (over && !f.IsLava && agl < 7f && Ship.Speed > 35f && Time.time - sprayAt > .12f) { sprayAt = Time.time; Weather.Spray(new Vector3(sp.x, f.Liquid, sp.z), Ship.Velocity, 1f + (7f - agl)); }
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

        int AliensWaiting() { int n = 0; if (Planet?.alien != null) for (int i = 0; i < Planet.alien.Length; i++) if (Planet.alien[i] != 0 && Planet.pilots[i] == (int)PilotFate.Waiting) n++; return n; }
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
                    PlayLog.Add($"pilot {p.Index + 1} found ({d / 1000f:0.0} km): {(p.IsAlien ? $"really a {p.Type}, tell {(p.Quirk != AlienType.None ? "shown" : "hidden")}" : "a real pilot")}{(k.wrong ? ", wrong voice" : "")}");
                    if (!foundSaid) { foundSaid = true; Voice.Say(talkRoll.Next(2) == 0 ? "ctl_found_1" : "ctl_found_2", VoiceKind.Radio, 2); }
                    string line = k.wrong && !k.borrowed ? "mayday_wrong" : (p.Index % 2 == 0 ? "mayday_" + Biomes.Keys[(int)WorldBiome] : "mayday");
                    Voice.Say(P(k, line), VoiceKind.Radio, 1, maxAge: 20f);
                }
                if (p.Now == DownedPilot.State.Running && !k.saw) { k.saw = true; PlayLog.Add($"pilot {p.Index + 1} runs to the ship{(p.IsAlien ? $" (a {p.Type})" : "")}"); Voice.Say(P(k, talkRoll.Next(2) == 0 ? "see_1" : "see_2"), VoiceKind.Radio, 1, maxAge: 4f); }
                if (p.Now == DownedPilot.State.Knocking && Time.time - k.doorAt > 9f)
                {
                    k.doorAt = Time.time; k.doorCount++;
                    if (k.doorCount == 1) PlayLog.Add($"pilot {p.Index + 1} at the airlock, knocking{(p.IsAlien ? $" (a {p.Type}, tell {(p.Quirk != AlienType.None ? "shown" : "hidden")})" : "")}");
                    string line = k.wrong && !k.borrowed && k.doorCount % 2 == 1 ? "door_wrong" : (k.doorCount % 2 == 1 ? "door_1" : "door_2");
                    Voice.Say(P(k, line), VoiceKind.Door, 1, Ship.transform, DownedPilot.DoorOutside + Vector3.up * 1.4f, maxAge: 3f);
                }
            }
            // landed near a pilot with the engines still running
            bool pilotCanCome = false; foreach (var p in Pilots) if (p.OnFoot && p.CanReach) pilotCanCome = true;
            if (Ship.Landed && Ship.EnginesOn && pilotCanCome) { landedEnginesOn += Time.deltaTime; if (landedEnginesOn > 5f && landedEnginesOn - Time.deltaTime <= 5f) Voice.Say("ctl_engines_off", VoiceKind.Radio, 2); }
            else landedEnginesOn = 0;
            // everyone found
            if (Waiting == 0 && cabin.Count > 0 && cabin.Count < CabinSize && !everyoneSaid) { everyoneSaid = true; Voice.Say("ctl_everyone", VoiceKind.Radio, 2); }
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
            Voice.Say(talkRoll.Next(2) == 0 ? "ctl_launch_1" : "ctl_launch_2", VoiceKind.Radio, 2); SayBiomeAtLaunch();
            if (Planet != null && Planet.OfTheDay && !launchDaySaid) { launchDaySaid = true; Voice.Say("ctl_day", VoiceKind.Radio, 2, maxAge: 20f); }
            foundSaid = everyoneSaid = energyWarned = false; lastHome = 0; lastHullBand = Ship.Damage >= 80 ? 2 : Ship.Damage >= 50 ? 1 : 0;
            foreach (var k in talk.Values) k.saw = false;
        }
        void SayBriefing() { Voice.Say("ctl_brief_" + Biomes.Keys[(int)WorldBiome], VoiceKind.Radio, 2, maxAge: 30f); }
        void SayBiomeAtLaunch() { if (WorldBiome >= Biome.Volcanic) Voice.Say("ctl_launch_" + Biomes.Keys[(int)WorldBiome], VoiceKind.Radio, 2, maxAge: 20f); }   // Round 7 (B5): Control names the new biomes at launch
        void SayArrived(bool newPlanet) { launchDaySaid = false; talk.Clear(); if (newPlanet) SayBriefing(); }
        void SayDockApproach() => Voice.Say(talkRoll.Next(2) == 0 ? "ctl_dock_1" : "ctl_dock_2", VoiceKind.Radio, 2);
        void SayDocked() { Music.Sting("sting_docked"); Voice.Say(talkRoll.Next(2) == 0 ? "ctl_docked_1" : "ctl_docked_2", VoiceKind.Radio, 2); }
        void SayCleared() { Music.Sting("sting_planet_cleared"); Voice.Say("ctl_cleared", VoiceKind.Radio, 2, maxAge: 20f); }
        void SayBoarded(DownedPilot p)
        {
            Music.Sting("sting_pilot_aboard"); Voice.Say("cpu_pilot_aboard", VoiceKind.Computer, 3);
            Voice.Say(P(Talk(p), talkRoll.Next(2) == 0 ? "thanks_1" : "thanks_2"), VoiceKind.Cabin, 1, Ship.transform, new Vector3(0, .8f, -3f), maxAge: 8f);
            if (cabin.Count >= CabinSize) { Voice.Say("cpu_cabin_full", VoiceKind.Computer, 3); Voice.Say("ctl_full", VoiceKind.Radio, 2, maxAge: 15f); }
        }
        void SayScream(int victim)
        {
            // the Sleeper's victim screams (through the bulkhead)
            var who = Pilots.Find(x => x.Index == victim);
            if (who) Voice.Say(P(Talk(who), "scream"), VoiceKind.Cabin, 3, Ship.transform, new Vector3(0, .8f, -3.5f), maxAge: 2f);
        }
    }
}
