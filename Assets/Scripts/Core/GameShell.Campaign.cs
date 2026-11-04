using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace HostileHorizon
{
    // Round 2: the Campaign. One ongoing career (Career.cs). Flow:
    //   Campaign menu -> Continue / New career -> the hangar in the mothership's bay (item 12, a menu over the live bay)
    //   -> Launch: the bay doors open, the cradle lets go, the ship drops out and its engines catch it (live, item 9)
    //   -> fly today's planet (item 3), find the downed pilots on the radar (items 4-5), land (item 6), pick them up (item 7)
    //   -> climb above the docking height with pilots aboard: the ship flies itself into the bay, live (item 9)
    //   -> docked: pilots paid, fuel used charged (item 10), back in the hangar.
    // Out of energy on the ground: call the mothership tow (item 8). Death: the explosion and wreck (item 1), pilots aboard lost,
    // the view rises live from the wreck into the mothership's regeneration pod; pay the recovery or take the loaner (item 13).
    // No fades and no cuts (VR rule 5): every move between places is a camera or ship that actually travels.
    // Fix 2.3, getting home: the mothership's bearing and the height still to climb ("DOCK ↑") on the displays, a sky marker on it,
    // RETURN NOW / CAN'T REACH warnings from the energy the climb to the docking height needs, the recall (hold the button: the
    // mothership comes and its tow beam lifts you, live, recall fee), the same beam catching the ship in mid-air when it runs dry
    // (tow fee), and the energy each boarding pilot brings (random every time, not from the seed).
    public sealed partial class GameShell
    {
        public bool InCampaign { get; private set; }
        public Mothership Mother { get; private set; }
        public PlanetState Planet { get; private set; }
        public readonly List<DownedPilot> Pilots = new List<DownedPilot>();
        public int CabinCount => cabin.Count;
        public string Phase { get; private set; } = "";
        public string Debrief => debrief;
        public CrashLevel CampaignLevel => (CrashLevel)Mathf.Clamp(GameSettings.S.campaignCrashLevel, 0, 4);
        readonly List<int> cabin = new List<int>();
        bool dockArmed;
        string debrief = "", flash = ""; float flashUntil, beepTimer;
        Coroutine seq; LineRenderer beam;
        int calling; bool recallLift; float recallHold, warnTimer; Renderer skyMark;   // calling: 0 no, 1 tow (ran dry in the air), 2 recall
        readonly System.Random fuelRoll = new System.Random();                        // pilot fuel: not the planet's seed (Dan's call)
        public int HomeState { get; private set; }                                     // 0 fine, 1 RETURN NOW, 2 CAN'T REACH
        public int Calling => calling;
        public float LastPilotFuel { get; private set; }
        public float LiftStarted { get; private set; }   // Fix 2.5: the checks time the lift and docking (Time.time)
        public float LiftEnded { get; private set; }
        // Energy (%) to climb from here to the docking height in flight mode at full throttle, plus the margin.
        public float EnergyHome { get { var t = Tuning.T; return Mathf.Max(0, t.dockingHeight - Ship.Altitude) / t.homeClimbRate * (t.energyDrainFlightBase + t.energyDrainFlightThrottle) + t.homeMargin; } }
        MenuPanel.Page campaignPage, confirmNew, hangar, dead, regen, outOfEnergy, campaignPause, lowEnergy, jumping, planetsPage;
        MenuPanel.Row jumpRow;

        int Waiting { get { int n = 0; foreach (var p in Pilots) if (p.OnFoot) n++; return n; } }
        bool CanDock => cabin.Count > 0 || Waiting == 0 || Ship.Energy < Tuning.T.dockAnyTimeBelowEnergy || Ship.Energy < EnergyHome + Tuning.T.returnNowWarning;
        void Flash(string s, float seconds = 4f) { flash = s; flashUntil = Time.time + seconds; }
        void Run(IEnumerator r) { if (seq != null) StopCoroutine(seq); seq = StartCoroutine(r); }
        static Vector3 Flat(Vector3 v) { v.y = 0; return v; }

        // ---- start / leave

        public void StartCampaign(bool fresh) => Run(CampaignStart(fresh));

        IEnumerator CampaignStart(bool fresh)
        {
            Now = State.Loading; Time.timeScale = 1; Menu.SetBackdrop(.6f); Menu.Show(loading);
            ClearCampaignWorld(); ClearCrash();
            var c = fresh || !Career.Exists ? Career.New() : Career.Load() ?? Career.New();
            Arrive(c, c.ArrivalSeed());   // Fix 3.2: a new day's planet of the day first, else where you left off
            Ship.UsesEnergy = true;
            Ship.Spawn(Mother.DockPosition, Mother.DockRotation, CampaignLevel, .3f, c.Current.damage);
            Ship.Energy = c.Current.energy; Ship.MakeKinematic(true); Ship.SetPose(Mother.DockPosition, Mother.DockRotation, 0, true);
            Extras.SetLook(c.flyingLoaner ? ShipExtras.Look.Loaner : ShipExtras.Look.Own);
            Rig.Current = CameraRig.Mode.Cockpit; Rig.Watch = null; Rig.Scripted = false; Rig.FollowShip = true; Rig.Snap();
            Terrain.SetFocus(Mother.transform.position);
            float end = Time.realtimeSinceStartup + 12f;
            while (!Terrain.ReadyNear(1500f) && Time.realtimeSinceStartup < end) yield return null;
            InCampaign = true; Displays.Campaign = true; Displays.CabinSize = Tuning.T.cabinSize; cabin.Clear();
            debrief = fresh ? $"New career. Welcome to the {c.systemName} system rescue corps.\nStarting credits {c.credits} cr." : "";
            ShowHangar();
        }

        // The mothership over a planet of the career (Fix 3.2: any of them, not only today's): the world, its pilots, saved.
        void Arrive(Career c, int seed)
        {
            bool isNew = !c.planets.Exists(p => p.seed == seed);
            Planet = c.Planet(seed); Planet.visits++;
            SetWorld(seed, Biomes.FromKey(Planet.biome));   // Round 4: the planet's biome
            SayArrived(isNew); Planet.lastVisit = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"); c.currentSeed = seed;
            Terrain.Field.FindStart(out var start, out var heading);
            Mother.gameObject.SetActive(true); Mother.Place(start, heading); Mother.Holding = true; Mother.SetDoors(0, true);
            SpawnPilots();
            c.Save();
            Ship.Field = Terrain.Field; Rig.Field = Terrain.Field;
        }

        // Fix 3.2: the jump to another planet, from the hangar. The bay doors stay shut: you stay in the cockpit in the bay (no cut,
        // no fade) while the mothership arrives over the other planet; pilots waiting on the one you leave stay there for later.
        public void JumpTo(int seed) => Run(Jump(seed));
        public void JumpToNew() { var c = Career.C; if (c.credits < Tuning.T.jumpCost) return; JumpTo(c.NextPlanetSeed()); }

        IEnumerator Jump(int seed)
        {
            var c = Career.C; string from = Planet?.name;
            bool made = !c.planets.Exists(p => p.seed == seed);
            int paid = c.Spend(Tuning.T.jumpCost);
            Now = State.Loading; Phase = "jump"; Time.timeScale = 1;
            var toBiome = made ? (c.planets.Count == 0 ? Biome.Canyons : Biomes.ForSeed(seed)) : Biomes.FromKey(c.planets.Find(p => p.seed == seed).biome);
            jumping.Body = () => $"Leaving {from}.\n\nJumping to <b>{Names.Planet(seed)}</b>  ({Biomes.Name(toBiome)})…";
            Music.Sting("sting_jump"); Voice.Say("ctl_jump", VoiceKind.Radio, 2);
            Menu.SetBackdrop(.15f); Menu.Show(jumping);
            foreach (var p in Pilots) if (p) Destroy(p.gameObject);
            Pilots.Clear(); cabin.Clear(); ClearAliens(); Reseat();
            if (padRing) padRing.gameObject.SetActive(false);
            if (Route) Route.Clear(); routePick = -1;
            if (Displays) { Displays.Route = ""; Displays.RoutePath = null; Displays.Contacts.Clear(); }
            Arrive(c, seed);
            Ship.SetPose(Mother.DockPosition, Mother.DockRotation, 0, true); Rig.Snap();
            Terrain.SetFocus(Mother.transform.position);
            float end = Time.realtimeSinceStartup + 12f;
            while (!Terrain.ReadyNear(1500f) && Time.realtimeSinceStartup < end) yield return null;
            Sfx.Play(Sfx.Thunk, Ship.transform.position + Vector3.up * 1.5f, .7f, 8f);
            debrief = $"Jumped from {from} to {Planet.name}{(made ? " (a new planet)" : "")}{(paid > 0 ? $": -{paid} cr" : "")}.\n{Planet.pilots.Length} pilots down here: {Planet.Count(PilotFate.Waiting)} waiting.";
            ShowHangar();
        }

        void SpawnPilots()
        {
            foreach (var p in Pilots) if (p) Destroy(p.gameObject);
            Pilots.Clear();
            var sites = PlanetSites.Find(Terrain.Field, Planet.pilots.Length, Planet.tier);
            if (sites.Count < Planet.pilots.Length) { System.Array.Resize(ref Planet.pilots, sites.Count); System.Array.Resize(ref Planet.alien, sites.Count); System.Array.Resize(ref Planet.quirk, sites.Count); }   // a planet short of landable spots has fewer pilots
            for (int i = 0; i < Planet.pilots.Length; i++)
            {
                if (Planet.pilots[i] != (int)PilotFate.Waiting) continue;
                var p = DownedPilot.Create(i, sites[i], Terrain.Field, litTemplate, (AlienType)Planet.alien[i], (AlienType)Planet.quirk[i]);   // Round 3: the seed's aliens
                p.OnBoarded += PilotBoarded;
                p.OnKnock += (_, n) => Displays.Knock(n);   // Fix 2.5: KNOCK KNOCK KNOCK on the displays as well as the sound (Round 3: one word a knock)
                p.OnClimb += AlienClimbs;                    // Round 3: an alien left at the airlock climbs onto the canopy
                Pilots.Add(p);
            }
        }

        void LeaveCampaign()
        {
            if (seq != null) { StopCoroutine(seq); seq = null; }
            ClearCampaignWorld();
        }

        void ClearCampaignWorld()
        {
            foreach (var p in Pilots) if (p) Destroy(p.gameObject);
            Pilots.Clear(); cabin.Clear();
            if (Mother) Mother.gameObject.SetActive(false);
            Beam(false);
            InCampaign = false; Phase = ""; calling = 0;
            if (Displays) { Displays.Campaign = false; Displays.PickupLine = ""; Displays.AirlockLit = false; }
            if (Belly) Belly.AirlockView = false;
            if (padRing) padRing.gameObject.SetActive(false);
            if (Route) Route.Clear(); routePick = -1;
            if (Displays) { Displays.Route = ""; Displays.RoutePath = null; }
            if (Ship) { Ship.UsesEnergy = false; Ship.Kinematic = false; }
            ClearAliens();
        }

        // Saved on quitting mid-flight too: the ship as it is; pilots in the cabin weren't delivered, so they wait on the planet again.
        void SaveCareer()
        {
            var c = Career.C; if (c == null || !InCampaign) return;
            if (Now == State.Flying || Now == State.Paused || Now == State.Auto && Phase != "regen-trip" && Phase != "recovery")
            { c.Current.energy = Ship.Energy; c.Current.damage = Ship.Damage; }
            c.Save();
        }

        void OnApplicationQuit() { if (InCampaign) SaveCareer(); }

        // ---- the hangar

        // A planet is cleared when no pilots are left to rescue (rescued or lost): the bonus is paid once, at the dock or in the hangar.
        void CheckCleared(StringBuilder sb)
        {
            if (Planet.Count(PilotFate.Waiting) > 0 || Planet.bonusPaid) return;
            var c = Career.C; Planet.bonusPaid = Planet.cleared = true; c.stats.planetsCleared++; c.Earn(Tuning.T.clearBonus);
            SayCleared();
            sb.AppendLine($"<b>{Planet.name} cleared! +{Tuning.T.clearBonus} credits</b>");
        }

        void ShowHangar()
        {
            var sb = new StringBuilder(); CheckCleared(sb);
            if (sb.Length > 0) { debrief = (debrief + "\n" + sb).Trim(); Career.C.Save(); }
            Now = State.Docked; Phase = "docked"; Time.timeScale = 1;
            Mother.Holding = true; Ship.MakeKinematic(true);
            Displays.Cabin = cabin.Count; Displays.Prompt = "Docked in the mothership"; Displays.Contacts.Clear();
            Displays.SetVisible(false); Menu.SetBackdrop(.15f);
            // Fix 3.2: on a cleared planet the jump to a new one is the default next action
            if (Planet.cleared) { hangar.Selected = hangar.Rows.IndexOf(jumpRow); Menu.Show(hangar, true); } else Menu.Show(hangar);
        }

        void HoldDocked() { if (Phase == "docked") HoldShip(); }
        void HoldShip() => Ship.SetPose(Mother.DockPosition, Mother.DockRotation, Time.deltaTime, true);

        IEnumerator Launch()
        {
            Now = State.Auto; Phase = "launch"; Menu.Hide(); Displays.SetVisible(true);
            var c = Career.C; c.stats.sorties++;
            Ship.SetEnergyAndDamage(c.Current.energy, c.Current.damage);
            cabin.Clear(); dockArmed = false; debrief = ""; calling = 0; recallHold = 0; HomeState = 0;
            ClearAliens(); Reseat(); securityLine = ""; journalNote = "";
            SayLaunch();   // Round 4: the launch cue and Control
            Mother.Holding = true; Mother.SetDoors(1);
            Sfx.Play(Sfx.Hiss, Mother.transform.TransformPoint(new Vector3(0, -6, 0)), 1f, 20f);
            while (Mother.DoorsOpen < .98f) { HoldShip(); yield return null; }
            Sfx.Play(Sfx.Thunk, Ship.transform.position + Vector3.up * 1.5f, 1f, 8f);   // the cradle lets go
            Vector3 pos = Mother.DockPosition; Quaternion rot = Mother.DockRotation; Vector3 fwd = rot * Vector3.forward;
            float vy = 0, speed = 0, t = 0;
            while (t < 6f)
            {
                float dt = Time.deltaTime; t += dt;
                if (t < 1.8f) vy -= 9.8f * dt; else vy = Mathf.MoveTowards(vy, -14f, 6f * dt);   // falls clear, then the engines take the weight
                if (t > 1.2f) speed = Mathf.MoveTowards(speed, 72f, 16f * dt);
                pos += (Vector3.up * vy + fwd * speed) * dt;
                float dive = t > 1.2f ? Mathf.Clamp(Mathf.Atan2(-vy, Mathf.Max(speed, 1f)) * Mathf.Rad2Deg, 0, 35) : 0;
                rot = Quaternion.Slerp(rot, Quaternion.LookRotation(fwd) * Quaternion.Euler(dive, 0, 0), 1 - Mathf.Exp(-2.5f * dt));
                Ship.SetPose(pos, rot, dt, t < 2.2f);
                if (t > 3.5f) Mother.SetDoors(0);
                yield return null;
            }
            Ship.ReleaseToFlight(Ship.Speed, .45f);
            Mother.Holding = false; Phase = ""; Now = State.Flying; flightStartedAt = Time.time;
            Flash($"Find the beacons on the radar. Brake / landing mode: {Controls.LandKeys}", 6f);
        }

        // ---- flying the planet

        void CampaignTick()
        {
            float dt = Time.deltaTime; var t = Tuning.T;
            if (calling != 0) { Mother.Holding = false; Mother.MoveOver(Ship.transform.position, dt); }   // called: it comes to you, quicker
            else if (!Mother.Holding) Mother.Follow(Ship.transform.position, Ship.transform.forward, dt);
            bool free = cabin.Count < t.cabinSize;
            foreach (var p in Pilots) if (p.Now != DownedPilot.State.Aboard) p.Tick(Ship, free, dt);
            if (Now != State.Flying) return;   // Round 3: a boarding alien started the horror sequence
            SleeperTick(dt);
            CabinRoom.FeedOn = cabin.Count > 0 || !CabinRoom.LightsOn;
            if (Ship.InputEnabled && Controls.CabinCamPressed) Displays.CabinPip = !Displays.CabinPip;

            if (Ship.InputEnabled && Time.frameCount > Ship.IgnoreButtonsUntil && Controls.AirlockPressed)
            {
                if (!Ship.Landed) Flash("The airlock only opens on the ground", 2.5f);
                else { Extras.OpenAirlock(4f); Voice.Say("cpu_airlock_open", VoiceKind.Computer, 3, maxAge: 2f); foreach (var p in Pilots) if (p.AtDoor && cabin.Count < t.cabinSize) p.Board(); }
            }

            // the recall: held for a moment so a stray press costs nothing
            if (Ship.InputEnabled && Controls.RecallHeld && calling == 0)
            {
                if (recallHold >= 0) recallHold += dt;
                if (recallHold >= t.recallHoldSeconds) { recallHold = -1; recallFiredThisHold = true; Recall(); }
                else if (recallHold >= 0) Flash($"Keep holding to recall the mothership ({t.recallFee} cr)", .4f);
            }
            else if (!Controls.RecallHeld) recallHold = 0;

            // the radar: beacons still waiting, and the mothership (always: the way home)
            var con = Displays.Contacts; con.Clear();
            foreach (var p in Pilots) if (p.OnFoot && !p.HiddenFromRadar) con.Add(new RadarContact { position = p.Beacon, number = p.Index + 1 });
            con.Add(new RadarContact { position = Mother.transform.position, mothership = true });
            Vector3 toMother = Flat(Mother.transform.position - Ship.transform.position);
            float climb = Mathf.Max(0, t.dockingHeight - Ship.Altitude), brg = (Mathf.Atan2(toMother.x, toMother.z) * Mathf.Rad2Deg + 360f) % 360f;
            Displays.Home = $"HOME {brg:000}° {toMother.magnitude / 1000f:0.0} km\nDOCK ↑ {climb:0} m";
            HomeWarnings(dt);
            RouteTick();
            Displays.Cabin = cabin.Count;
            StopMarker(); BrakeCue(); PickupState();
            Displays.Place = $"{Planet.name} ({Biomes.Name(Planet.biome)}) · {CrashLevels.Names[(int)Ship.Level]}";
            Displays.Prompt = Time.time < flashUntil && Displays.PickupLine == "" ? flash : Advice();

            if (Ship.Energy < t.energyWarning && Ship.EnginesOn) { beepTimer -= dt; if (beepTimer <= 0) { beepTimer = 4f; Sfx.Play(Sfx.Beep, Ship.transform.TransformPoint(ShipLayout.DashCentre), .5f, 2f); } }

            if (calling != 0 && toMother.magnitude < t.beamReach) { Run(BeamLift(calling == 2)); return; }   // the beam catches the ship, in the air or landed
            if (Ship.Landed && Ship.OutOfEnergy && calling == 0) { Now = State.Auto; Phase = "stranded"; Displays.SetVisible(false); Menu.SetBackdrop(0); Menu.Show(outOfEnergy); return; }
            if (Ship.Altitude < t.dockingHeight - 50f) dockArmed = true;
            if (dockArmed && Ship.Altitude > t.dockingHeight && CanDock) { calling = 0; Run(Dock(false)); }   // made it up: docking is free
        }

        // RETURN NOW when the energy left is within the warning of what the climb to the docking height needs; CAN'T REACH below it.
        // A distinct sound when a warning starts and every few seconds while it stays (the 15 % beep goes on as before).
        void HomeWarnings(float dt)
        {
            var t = Tuning.T; float e = Ship.Energy, need = EnergyHome;
            int Level(float extra) => Ship.OutOfEnergy || calling != 0 ? 0 : e < need + extra ? 2 : e < need + t.returnNowWarning + extra ? 1 : 0;
            int now = Level(0), back = Level(1);   // 1 % of hysteresis before a warning clears
            if (now > HomeState) { HomeState = now; warnTimer = 0; }
            else if (back < HomeState) HomeState = back;
            if (HomeState > 0) { warnTimer -= dt; if (warnTimer <= 0) { warnTimer = HomeState == 2 ? 6f : 10f; Sfx.Play(Sfx.Warn, Ship.transform.TransformPoint(ShipLayout.DashCentre), .6f, 2f); } }
            Displays.HomeWarning = HomeState == 2 ? "CAN'T REACH THE MOTHERSHIP: recall or land" : HomeState == 1 ? "RETURN NOW" : "";
        }

        // Fix 2.6: the route trail's target. Auto (default): the nearest pilot on foot; the mothership once the cabin is full or no
        // pilot is left. The RouteTarget button (D-pad right / T / HOTAS 11) steps through the pilots by distance, then the
        // mothership, then back to auto.
        public RouteTrail Route { get; private set; }
        int routePick = -1;   // -1 auto, -2 the mothership, else a pilot's index
        public string RouteTargetName { get; private set; } = "";
        public DownedPilot RoutePilot { get; private set; }

        void RouteTick()
        {
            var t = Tuning.T;
            var foot = new List<DownedPilot>(); foreach (var p in Pilots) if (p.OnFoot && !p.HiddenFromRadar) foot.Add(p);
            foot.Sort((a, b) => Flat(a.LandingSpot - Ship.transform.position).sqrMagnitude.CompareTo(Flat(b.LandingSpot - Ship.transform.position).sqrMagnitude));
            if (routePick >= 0 && !foot.Exists(p => p.Index == routePick)) routePick = -1;
            if (Ship.InputEnabled && Controls.RouteTargetPressed)
            {
                // auto -> pilots by distance (skipping the one auto shows) -> mothership -> auto
                int at = routePick == -1 ? 0 : routePick == -2 ? foot.Count + 1 : foot.FindIndex(p => p.Index == routePick) + 1;
                int next = at + 1; if (routePick == -1 && foot.Count > 0) next = 2;   // auto already shows the nearest
                routePick = next <= foot.Count ? foot[next - 1].Index : next == foot.Count + 1 ? -2 : -1;
                if (routePick == -1) Flash("Route: auto (nearest pilot)", 2.5f);
            }
            bool full = cabin.Count >= t.cabinSize;
            RoutePilot = routePick >= 0 ? foot.Find(p => p.Index == routePick) : routePick == -1 && !full && foot.Count > 0 ? foot[0] : null;
            bool toMother = RoutePilot == null;
            Vector3 goal = toMother ? Mother.transform.position : RoutePilot.LandingSpot;
            bool on = GameSettings.S.routeTrail && Now == State.Flying && !Ship.Landed && !Ship.Kinematic && !Ship.Crashed;
            Route.Tick(on, Terrain.Field, Ship.transform, goal, toMother, t.dockingHeight);
            float d = Flat(goal - Ship.transform.position).magnitude;
            RouteTargetName = toMother ? "MOTHERSHIP" : $"PILOT {RoutePilot.Index + 1}";
            Displays.Route = GameSettings.S.routeTrail ? $"ROUTE → {RouteTargetName} · {RadarScope.Distance(d)}{(routePick == -1 ? "" : " (picked)")}   [{Controls.RouteTargetKey}]" : "";
            Displays.RoutePath = on ? Route.Path : null;
        }

        // Fix 2.4: the nearest pilot still waiting, and how far (level distance).
        DownedPilot NearestWaiting(out float dist)
        {
            DownedPilot best = null; dist = float.MaxValue;
            foreach (var p in Pilots) if (p.OnFoot) { float d = Flat(p.Position - Ship.transform.position).magnitude; if (d < dist) { dist = d; best = p; } }
            return best;
        }

        // Fix 2.4: a ring on the ground (and a post) where the ship would stop if X went on now, while flying near a waiting pilot and
        // while braking. A world object: seen from the cockpit, the same in VR.
        // Fix 2.5: also in hover (where the ship would come to rest: the belly camera shows it), and a green ring round the waiting
        // pilot's landing pad within 900 m.
        LineRenderer stopRing, stopPost, padRing;
        void StopMarker()
        {
            var nearest = NearestWaiting(out float near);
            bool on = InCampaign && Now == State.Flying && !Ship.Kinematic && near < Tuning.T.stopMarkerRange &&
                      (Ship.Braking || Ship.Mode == FlightMode.Flight && Flat(Ship.Velocity).magnitude > Tuning.T.hoverMaxSpeed + 5f || Ship.Mode == FlightMode.Hover && !Ship.Landed);
            if (RoutePilot != null) { nearest = RoutePilot; near = Flat(RoutePilot.Position - Ship.transform.position).magnitude; }   // Fix 2.6: the route's pilot
            bool pad = InCampaign && nearest != null && nearest.Now == DownedPilot.State.Waiting && near < 900f && !Ship.Kinematic;
            if (pad && !padRing)
            {
                var pm = new Material(Shader.Find("HostileHorizon/Glow")); pm.SetColor("_Color", new Color(.3f, 1f, .5f) * 1.6f);
                padRing = new GameObject("Pad ring").AddComponent<LineRenderer>(); padRing.sharedMaterial = pm; padRing.positionCount = 41; padRing.loop = true; padRing.widthMultiplier = .9f;
                padRing.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; padRing.receiveShadows = false;
            }
            if (padRing)
            {
                padRing.gameObject.SetActive(pad);
                if (pad) { var c0 = nearest.LandingSpot; var fl = Terrain.Field; for (int i = 0; i < 41; i++) { float a = i * Mathf.PI * 2 / 40f; float x = c0.x + Mathf.Cos(a) * 20f, z = c0.z + Mathf.Sin(a) * 20f; padRing.SetPosition(i, new Vector3(x, fl.SurfaceHeight(x, z) + .6f, z)); } }
            }
            if (on && !stopRing)
            {
                var m = new Material(Shader.Find("HostileHorizon/Glow")); m.SetColor("_Color", new Color(1f, .75f, .2f) * 2f);
                LineRenderer Line(string name, int n, float w) { var l = new GameObject(name).AddComponent<LineRenderer>(); l.sharedMaterial = m; l.positionCount = n; l.widthMultiplier = w; l.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; l.receiveShadows = false; return l; }
                stopRing = Line("Stop point ring", 33, 1.2f); stopRing.loop = true; stopPost = Line("Stop point post", 2, .8f);
            }
            if (!stopRing) return;
            stopRing.gameObject.SetActive(on); stopPost.gameObject.SetActive(on);
            if (!on) return;
            var c = Ship.StopPoint; var f = Terrain.Field;
            for (int i = 0; i < 33; i++) { float a = i * Mathf.PI * 2 / 32f; float x = c.x + Mathf.Cos(a) * 14f, z = c.z + Mathf.Sin(a) * 14f; stopRing.SetPosition(i, new Vector3(x, f.SurfaceHeight(x, z) + .8f, z)); }
            stopPost.SetPosition(0, c); stopPost.SetPosition(1, c + Vector3.up * 50f);
        }
        public Vector3 StopMarkerAt => stopRing && stopRing.gameObject.activeSelf ? stopPost.GetPosition(0) : Vector3.zero;

        // Fix 2.4: on the radar (cockpit and HUD) and the advice line: SLOW DOWN - PRESS X when braking now would stop the ship near the
        // nearest pilot ahead (from brakeCueLead m before the stop point reaches them).
        void BrakeCue()
        {
            Displays.Cue = ""; Displays.PilotNear = false;
            var p = NearestWaiting(out float d);
            if (p == null) return;
            Displays.PilotNear = d < Tuning.T.pickupRange;
            if (Ship.Mode != FlightMode.Flight || Ship.Kinematic) return;
            Vector3 v = Flat(Ship.Velocity), to = Flat(p.LandingSpot - Ship.transform.position);   // Fix 2.5: the pad, not the pilot
            if (v.magnitude < Tuning.T.hoverMaxSpeed + 5f) return;
            float along = Vector3.Dot(to, v.normalized), side = Vector3.Cross(v.normalized, to).magnitude, stop = Flat(Ship.StopPoint - Ship.transform.position).magnitude;
            float early = along - stop;   // how much short of the pilot the ship would stop
            if (side < 150f && early > -40f && early < Tuning.T.brakeCueLead) Displays.Cue = $"SLOW DOWN — PRESS {Controls.LandKey}";
        }

        void Recall()
        {
            if (PilotComing) { Flash("Not now: a pilot is coming to the ship", 3f); return; }
            if (CanopyAlien != null) return;
            calling = 2;
            Flash($"Recall: the mothership is coming ({Tuning.T.recallFee} cr at the dock)", 5f);
            Voice.Say("ctl_recall", VoiceKind.Radio, 2);
        }

        // Out of energy in the air: the mothership comes and its beam catches the ship before it reaches the ground (if it can).
        void OnOutOfEnergy()
        {
            if (!InCampaign || Now != State.Flying || Ship.Landed || calling != 0) return;
            calling = 1;
            Flash("Out of energy: the mothership is coming, its tow beam will catch you", 6f);
            Voice.Say("ctl_tow", VoiceKind.Radio, 2);
        }

        IEnumerator BeamLift(bool recall)
        {
            calling = 0; recallLift = recall;
            Mother.Holding = true; Mother.SetDoors(1); Beam(true);
            Sfx.Play(Sfx.Hiss, Ship.transform.position, .8f, 10f);
            yield return Dock(true);
        }

        // Fix 2.5: the pickup, always shown while it is on: PILOT AT AIRLOCK — press B to open (the button for the device in use) and
        // the lit AIRLOCK button; PILOT COMING — keep engines off while they run; the airlock camera while they come and wait.
        void PickupState()
        {
            bool atDoor = false, coming = false;
            foreach (var p in Pilots) { if (p.AtDoor) atDoor = true; if (p.Now == DownedPilot.State.Running || p.Now == DownedPilot.State.Boarding) coming = true; }
            Displays.PickupLine = atDoor ? $"PILOT AT AIRLOCK — press {Controls.AirlockKey} to open" : coming ? "PILOT COMING — keep engines off" : "";
            Displays.AirlockLit = atDoor;
            Belly.AirlockView = Ship.Landed && (atDoor || coming);
        }

        // What to do next, on the centre display and the HUD.
        string Advice()
        {
            var t = Tuning.T;
            if (CanopyAlien != null) return "";   // Round 3: nothing to say
            foreach (var p in Pilots) if (p.AtDoor) return $"PILOT AT AIRLOCK — press {Controls.AirlockKey} to open";
            foreach (var p in Pilots) if (p.Now == DownedPilot.State.Running) return "PILOT COMING — keep engines off";
            foreach (var p in Pilots) if (p.Now == DownedPilot.State.Boarding) return "Pilot coming aboard";
            float climb = Mathf.Max(0, t.dockingHeight - Ship.Altitude);
            if (calling == 2) return "Recall: the mothership is coming. Its beam will lift you";
            if (calling == 1) return "Out of energy: the mothership is coming. Its beam will catch you";
            if (HomeState == 2) return $"Can't reach the mothership: recall ({Controls.RecallKeys}, {t.recallFee} cr) or land";
            if (HomeState == 1) return $"RETURN NOW: climb to dock (↑ {climb:0} m), or recall ({Controls.RecallKeys})";
            if (Ship.Landed)
            {
                if (cabin.Count >= t.cabinSize) return "Cabin full: take off and dock with the mothership";
                foreach (var p in Pilots) if (p.OnFoot && p.CanReach)
                        return Ship.EnginesOn ? $"Engines off ({Controls.EngineKey}) so the pilot can come to you" : "Waiting for the pilot";
                // Fix 2.5: a pilot near but unable to get to the ship says so
                foreach (var p in Pilots) if (p.OnFoot && p.LevelToDoor < 700f)
                        return $"Pilot {p.Index + 1} can't reach you here ({p.LevelToDoor:0} m away, {Mathf.Abs(p.ClimbToDoor):0} m {(p.ClimbToDoor > 0 ? "up" : "down")}): land on their pad (green ring)";
                return Ship.EnginesOn ? "Landed. Climb (throttle up) to take off" : "Engines off. Throttle up or A / K to start them";
            }
            if (cabin.Count >= t.cabinSize) return $"Cabin full: climb to dock (↑ {climb:0} m)";
            if (Waiting == 0) return $"Everyone found: climb to dock (↑ {climb:0} m)";
            if (Displays.Cue != "") return $"Slow down now: press {Controls.LandKey} to stop at the pilot's pad (the ring shows where)";
            if (Ship.Braking) return "Braking: the ring on the ground shows where you will stop";
            if (Ship.Mode == FlightMode.Hover) return "Landing mode: stick moves and turns, throttle up / down (faster high up), set down gently";
            float nearest = float.MaxValue; foreach (var p in Pilots) if (p.OnFoot) nearest = Mathf.Min(nearest, Vector3.Distance(p.Position, Ship.transform.position));
            if (nearest < 900f) return $"Pilot close by: brake ({Controls.LandKey}) when the amber ring reaches their pad (green ring)";
            if (cabin.Count > 0) return $"{cabin.Count} aboard: more pilots, or climb to dock (↑ {climb:0} m)";
            if (CanDock) return $"Climb to dock (↑ {climb:0} m)";
            if (Ship.Energy < t.energyWarning) return $"Energy low: recall the mothership any time ({Controls.RecallKeys}, {t.recallFee} cr)";
            return "";
        }

        void PilotBoarded(DownedPilot p)
        {
            // Round 3: a Mimic, Hollow, Knocker or Stalker let in: the horror sequence. The Sleeper passes as a pilot.
            if (p.IsAlien && p.Type != AlienType.Sleeper) { Run(Horror(p.Type, 0, p)); return; }
            cabin.Add(p.Index);
            if (p.Type == AlienType.Sleeper) SleeperBoarded(p);
            Reseat();
            var t = Tuning.T;
            LastPilotFuel = Mathf.Round(t.pilotFuelMin + (float)fuelRoll.NextDouble() * (t.pilotFuelMax - t.pilotFuelMin));
            Ship.Energy = Mathf.Min(100, Ship.Energy + LastPilotFuel);
            Flash($"Pilot {p.Index + 1} aboard ({cabin.Count} / {t.cabinSize}), brought {LastPilotFuel:0} % energy", 4f);
            SayBoarded(p);
        }

        // Engines started near pilots on foot: the exhaust kills those within its range.
        void OnEnginesStarted()
        {
            if (!InCampaign) return;
            var t = Tuning.T; var c = Career.C;
            if (CanopyAlien != null) ThrowOffCanopy();   // Round 3: thrown off the canopy, burnt
            foreach (var p in Pilots)
            {
                if (!p.OnFoot || Vector3.Distance(p.Position, Ship.transform.position) > t.exhaustKillRange) continue;
                if (p.IsAlien) { ExhaustKillsAlien(p); continue; }   // Round 3: it shrieks and shows what it was
                p.Kill(); Planet.pilots[p.Index] = (int)PilotFate.Lost; c.stats.pilotsLost++;
                int paid = c.Spend(t.exhaustKillPenalty);
                Flash($"Pilot {p.Index + 1} killed by your exhaust. Penalty -{paid} cr", 6f);
                Voice.Say("ctl_pilot_killed", VoiceKind.Radio, 2);
            }
        }

        // ---- docking (and the tow's lift), live

        IEnumerator Dock(bool towLift)
        {
            if (Now != State.Paused) Now = State.Auto; else pausedFrom = State.Auto; Phase = towLift ? "tow-lift" : "dock-approach"; Mother.Holding = true;
            Ship.MakeKinematic(false);
            Vector3 pos = Ship.transform.position, vel = Ship.Velocity; Quaternion rot = Ship.transform.rotation; float bank = 0;   // caught as it moves (zero when landed)
            var tu = Tuning.T; LiftStarted = Time.time;
            if (!towLift) SayDockApproach();
            Displays.Prompt = !towLift ? "Docking: autopilot" : recallLift ? "Recall: the mothership is lifting you" : "Tow: the mothership is lifting you";
            if (!towLift)
            {
                // A: fly round to a point behind and below the bay, lined up with it
                while (true)
                {
                    float dt = Time.deltaTime; if (dt <= 0) { yield return null; continue; }
                    Vector3 a = Mother.BelowBay(tu.bayRiseFrom) - Mother.DockRotation * Vector3.forward * 350f;
                    Vector3 to = a - pos; if (to.magnitude < 80f) break;
                    Vector3 before = Flat(vel);
                    vel = Vector3.MoveTowards(vel, to.normalized * tu.dockApproachSpeed, tu.dockApproachAccel * dt);
                    pos += vel * dt;
                    float turn = Vector3.SignedAngle(before, Flat(vel), Vector3.up) / dt;
                    bank = Mathf.Lerp(bank, Mathf.Clamp(-turn * 2f, -45, 45), 1 - Mathf.Exp(-3f * dt));
                    rot = Quaternion.Slerp(rot, Quaternion.LookRotation(vel) * Quaternion.Euler(0, 0, bank), 1 - Mathf.Exp(-4f * dt));
                    Ship.SetPose(pos, rot, dt, false);
                    yield return null;
                }
                Phase = "dock-final";
            }
            // B: slow down to a hover just under the bay, facing the way the ship docks
            while (true)
            {
                float dt = Time.deltaTime; if (dt <= 0) { yield return null; continue; }
                Vector3 e = Mother.BelowBay(tu.bayRiseFrom), to = e - pos; float d = to.magnitude;
                if (d < .3f || d < 1.5f && vel.magnitude < 1.5f) break;
                // Fix 2.5: about 3x quicker (tuning beamLift / dockFinal); the speed comes from the distance left, so it brakes in time
                float top = towLift ? tu.beamLiftSpeed : tu.dockFinalSpeed, acc = towLift ? tu.beamLiftAccel : tu.dockFinalAccel;
                float cap = Mathf.Min(top, Mathf.Sqrt(2f * acc * .8f * d));
                vel = Vector3.MoveTowards(vel, to / d * cap, acc * dt);
                if (towLift && vel.y < 0) vel.y = Mathf.MoveTowards(vel.y, 0, Mathf.Max(10f, acc * .5f) * dt);   // the beam stops a fall first
                pos += vel * dt;
                if (towLift) pos.y = Mathf.Max(pos.y, Ship.Field.SurfaceHeight(pos.x, pos.z) + 1.5f);   // and never drags it through the rock
                Quaternion want = Mother.DockRotation;
                if (!towLift && vel.sqrMagnitude > 4f) want = Quaternion.Slerp(Quaternion.LookRotation(vel), Mother.DockRotation, Mathf.InverseLerp(30f, 6f, vel.magnitude));
                rot = Quaternion.Slerp(rot, want, 1 - Mathf.Exp(-(towLift ? 1.5f : 2.5f) * dt));
                if (d < 450f) Mother.SetDoors(1);
                Ship.SetPose(pos, rot, dt, d < 400f);
                yield return null;
            }
            // C: straight up into the bay, slowing to the cradle
            Phase = "dock-rise"; Mother.SetDoors(1);
            yield return RiseIntoBay(pos, rot);
            Beam(false);
            yield return Security();   // Round 3: the Sleeper, still aboard, met by the mothership's security
            DebriefAtDock(towLift);
            if (Rig.Current != CameraRig.Mode.Cockpit) Rig.Current = CameraRig.Mode.Cockpit;   // the chase views would sit inside the hull
            ShowHangar();
        }

        IEnumerator RiseIntoBay(Vector3 pos, Quaternion rot)
        {
            while (true)
            {
                float dt = Time.deltaTime; if (dt <= 0) { yield return null; continue; }
                Vector3 dock = Mother.DockPosition; float d = (dock - pos).magnitude;
                if (d < .01f) break;
                if (Mother.DoorsOpen < .9f && d > 10f) { Ship.SetPose(pos, rot, dt, true); yield return null; continue; }   // the doors first
                var tu = Tuning.T;
                pos = Vector3.MoveTowards(pos, dock, (Mathf.Min(tu.bayRiseSpeed, Mathf.Sqrt(2f * tu.bayRiseBrake * d)) + .1f) * dt);
                rot = Quaternion.Slerp(rot, Mother.DockRotation, 1 - Mathf.Exp(-2f * dt));
                Ship.SetPose(pos, rot, dt, true);
                yield return null;
            }
            Ship.SetPose(Mother.DockPosition, Mother.DockRotation, Time.deltaTime, true);
            Sfx.Play(Sfx.Thunk, Ship.transform.position + Vector3.up * 1.5f, 1f, 8f);
            Mother.SetDoors(0); LiftEnded = Time.time;
        }

        // At the dock: pilots paid, planet bonus, tow fee, fuel used charged (repair is the hangar's Repair button).
        void DebriefAtDock(bool towLift)
        {
            var c = Career.C; var t = Tuning.T; var sb = new StringBuilder();
            if (cabin.Count > 0)
            {
                int pay = cabin.Count * t.pilotPay, extra = Planet.OfTheDay ? Mathf.RoundToInt(pay * t.dayBonus) : 0;   // Fix 3.2: the planet of the day pays more
                c.Earn(pay + extra); c.stats.pilotsRescued += cabin.Count;
                foreach (int i in cabin) Planet.pilots[i] = (int)PilotFate.Rescued;
                sb.AppendLine($"Delivered {cabin.Count} pilot{(cabin.Count > 1 ? "s" : "")}: +{pay + extra} cr" + (extra > 0 ? $" (planet of the day +{t.dayBonus * 100:0} %: {extra} cr of it)" : ""));
                cabin.Clear();
            }
            CheckCleared(sb);
            if (towLift)
            {
                int fee = recallLift ? t.recallFee : t.towFee, paid = c.Spend(fee);
                if (recallLift) c.stats.recalls++; else c.stats.tows++;
                sb.AppendLine($"{(recallLift ? "Recall" : "Tow")}: -{paid} cr" + (paid < fee ? $" (the corps covered the other {fee - paid} cr)" : ""));
            }
            c.Current.energy = Ship.Energy; c.Current.damage = Ship.Damage;
            float used = 100 - Ship.Energy;
            string fuel = c.Refuel();
            if (fuel != "") sb.AppendLine($"Energy used {used:0}%. " + fuel);
            Ship.Energy = c.Current.energy;
            if (c.Current.damage > 0) sb.AppendLine($"Hull damage {c.Current.damage:0}%: repair {c.RepairCost} cr (Repair below, or fly as it is)");
            if (securityLine != "") { sb.AppendLine(securityLine); securityLine = ""; }
            c.Save();
            debrief = sb.ToString().TrimEnd();
            if (!towLift) SayDocked();
        }

        // ---- out of energy: the tow

        IEnumerator Tow()
        {
            Now = State.Auto; Phase = "tow-wait"; Menu.Hide(); Displays.SetVisible(true);
            Displays.Prompt = "Tow: the mothership is coming";
            Voice.Say("ctl_tow", VoiceKind.Radio, 2);
            Mother.Holding = false;
            while (!Mother.MoveOver(Ship.transform.position, Time.deltaTime)) { Displays.Prompt = "Tow: the mothership is coming"; yield return null; }
            Mother.Holding = true; Mother.SetDoors(1);
            Beam(true); recallLift = false;
            float t = 0; while (t < Tuning.T.towBeamDelay) { t += Time.deltaTime; yield return null; }
            yield return Dock(true);
        }

        void Beam(bool on)
        {
            if (on && !beam)
            {
                beam = new GameObject("Tow beam").AddComponent<LineRenderer>();
                var m = new Material(Shader.Find("HostileHorizon/Glow")); m.SetColor("_Color", new Color(.5f, .8f, 1f) * 1.6f);
                beam.sharedMaterial = m; beam.widthMultiplier = .22f; beam.positionCount = 2; beam.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            if (beam) beam.gameObject.SetActive(on);
        }

        void LateUpdate()
        {
            if (beam && beam.gameObject.activeSelf && Mother && Ship)
            { beam.SetPosition(0, Mother.transform.TransformPoint(new Vector3(0, -5.5f, 1.5f))); beam.SetPosition(1, Ship.transform.TransformPoint(new Vector3(0, 1.2f, -1.5f))); }
            SkyMarker();
        }

        // A blinking blue light under the mothership, kept at the same size on screen however far it is (and drawn inside the camera's
        // range, in its true direction), so it can be seen and flown toward. A world object: it works the same in VR.
        void SkyMarker()
        {
            bool on = InCampaign && Now == State.Flying && Mother && Rig;
            if (on && !skyMark)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Sphere); go.name = "Mothership marker"; Destroy(go.GetComponent<Collider>());
                skyMark = go.GetComponent<Renderer>(); skyMark.sharedMaterial = new Material(Shader.Find("HostileHorizon/Glow"));
                skyMark.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; skyMark.receiveShadows = false;
            }
            if (!skyMark) return;
            var cam = Rig.Head.transform.position; Vector3 to = on ? Mother.transform.position + Vector3.down * 9f - cam : Vector3.zero; float d = to.magnitude;
            on &= d > 250f;
            skyMark.gameObject.SetActive(on);
            if (!on) return;
            float place = Mathf.Min(d, Rig.Head.farClipPlane * .85f), blink = Mathf.Repeat(Time.time, 1.2f) < .8f ? 1f : .35f;
            skyMark.transform.position = cam + to / d * place;
            skyMark.transform.localScale = Vector3.one * place * .013f * (.8f + .2f * blink);
            skyMark.sharedMaterial.SetColor("_Color", new Color(.45f, .75f, 1f) * (1.8f * blink));
        }

        // ---- death and regeneration

        void Died(AlienType killer = AlienType.None)
        {
            var c = Career.C; Phase = "dead"; calling = 0;
            Voice.StopAll(); Music.Sting("sting_death"); Voice.Say("ctl_lost_contact", VoiceKind.Radio, 2, maxAge: 30f);
            if (CanopyAlien != null) { CanopyAlien = null; if (canopyCo != null) { StopCoroutine(canopyCo); canopyCo = null; } }
            journalNote = "";
            if (killer != AlienType.None) Met(killer, "it killed you");
            string got = killer != AlienType.None ? $"Killed by the thing you let aboard: a <b>{Aliens.Names[(int)killer]}</b>.\n" + (journalNote != "" ? journalNote + "\n" : "") + "\n" : "";
            int lost = 0;
            foreach (int i in cabin) { if (Planet.alien != null && i < Planet.alien.Length && Planet.alien[i] != 0) Planet.pilots[i] = (int)PilotFate.AlienGone; else { Planet.pilots[i] = (int)PilotFate.Lost; lost++; } }
            c.stats.pilotsLost += lost; c.stats.deaths++; cabin.Clear(); SleeperIndex = -1;
            if (c.flyingLoaner) { c.flyingLoaner = false; c.loaner = new ShipState(); }   // the loaner is gone; your own ship stays where it is
            else { c.ship.wrecked = true; c.ship.damage = 100; c.ship.energy = Ship.Energy; }
            c.Save();
            dead.Body = () => got + (lost > 0 ? $"{lost} pilot{(lost > 1 ? "s" : "")} aboard died with you. " : "") +
                "Your mind was backed up on the mothership.\n\n" + (killer != AlienType.None ? "Your ship is lost down there, with it inside." : "The wreck burns where you hit.");
            Run(ShowDead());
        }

        IEnumerator ShowDead()
        {
            float t = 0; while (t < 3.5f) { t += Time.unscaledDeltaTime; yield return null; }
            Menu.SetBackdrop(0); Menu.Show(dead);
        }

        // The view rises from the wreck, crosses to the mothership and comes in through the open stern to the regeneration pod.
        IEnumerator RegenTrip()
        {
            Now = State.Auto; Phase = "regen-trip"; Menu.Hide(); Displays.SetVisible(false);
            Mother.Holding = true; Rig.Watch = null; Rig.Scripted = true;
            var mt = Mother.transform;
            var pts = new List<Vector3> { Rig.transform.position, Rig.transform.position + Vector3.up * 300f,
                mt.TransformPoint(new Vector3(0, -40, -400)), mt.TransformPoint(new Vector3(0, -2, -60)), mt.TransformPoint(new Vector3(6, -3, -10)), Mother.PodEyeWorld };
            pts.Insert(0, pts[0] - (pts[1] - pts[0])); pts.Add(pts[pts.Count - 1] + (pts[pts.Count - 1] - pts[pts.Count - 2]));
            Quaternion r0 = Rig.transform.rotation;
            // the path as a fine polyline, travelled by distance, so the speed follows the ease (top speed about 220 m/s)
            var path = new List<Vector3>();
            for (int i = 0; i < pts.Count - 3; i++) for (int j = 0; j < 60; j++) path.Add(CatmullRom(pts[i], pts[i + 1], pts[i + 2], pts[i + 3], j / 60f));
            path.Add(pts[pts.Count - 2]);
            for (int i = 0; i < path.Count; i++) path[i] = mt.InverseTransformPoint(path[i]);   // carried along with the mothership (it may still be slowing down)
            var at = new float[path.Count]; for (int i = 1; i < path.Count; i++) at[i] = at[i - 1] + Vector3.Distance(path[i - 1], path[i]);
            float length = at[path.Count - 1];
            float T = Mathf.Max(9f, length * 1.5f / 220f), t = 0;   // smoothstep peaks at 1.5 x the average speed
            int seg = 0;
            while (t < T)
            {
                t += Time.deltaTime;
                float d = Mathf.SmoothStep(0, 1, Mathf.Clamp01(t / T)) * length;
                while (seg < path.Count - 2 && at[seg + 1] < d) seg++;
                float f = Mathf.InverseLerp(at[seg], at[seg + 1], d);
                var p = mt.TransformPoint(Vector3.Lerp(path[seg], path[seg + 1], f));
                var ahead = mt.TransformPoint(path[Mathf.Min(seg + 4, path.Count - 1)]) - p;
                var along = ahead.sqrMagnitude > .0001f ? Quaternion.LookRotation(ahead, Vector3.up) : Rig.transform.rotation;
                float k = t / T;
                var look = k < .15f ? Quaternion.Slerp(r0, along, k / .15f) : k > .8f ? Quaternion.Slerp(along, Mother.PodLook, (k - .8f) / .2f) : along;
                Rig.transform.SetPositionAndRotation(p, Quaternion.Slerp(Rig.transform.rotation, look, 1 - Mathf.Exp(-3f * Time.deltaTime)));
                yield return null;
            }
            // settle into the pod's view (no snap)
            for (float s2 = 0; s2 < 1.2f; s2 += Time.deltaTime)
            {
                Rig.transform.SetPositionAndRotation(Mother.PodEyeWorld, Quaternion.Slerp(Rig.transform.rotation, Mother.PodLook, 1 - Mathf.Exp(-5f * Time.deltaTime)));
                yield return null;
            }
            Phase = "regen"; Now = State.Docked;
            Menu.SetBackdrop(.1f); Menu.Show(regen);
            Voice.Say("ctl_regen_" + (1 + Career.C.stats.deaths % 3), VoiceKind.Radio, 2, maxAge: 20f);
        }

        static Vector3 CatmullRom(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float t)
        {
            float t2 = t * t, t3 = t2 * t;
            return .5f * (2f * b + (c - a) * t + (2f * a - 5f * b + 4f * c - d) * t2 + (3f * b - a - 3f * c + d) * t3);
        }

        // Recovery or the loaner: the ship is hauled up into the bay on the tow beam while you watch from the pod, then you walk
        // over and climb into the cockpit.
        IEnumerator Deliver(bool loaner, bool fromPod)
        {
            Now = State.Auto; Phase = "recovery"; Menu.Hide();
            var c = Career.C; var t = Tuning.T;
            if (loaner) { c.flyingLoaner = true; c.loaner = new ShipState(); debrief = $"You're flying the corps loaner (basic, no upgrades). Your own ship is still a wreck: recover it here for {c.RecoveryFee} cr when you can."; }
            else
            {
                int paid = c.Spend(c.RecoveryFee); c.ship.wrecked = false; c.ship.damage = 0; c.ship.energy = Mathf.Max(c.ship.energy, t.freeMinimumFill); c.flyingLoaner = false;
                debrief = $"Your ship was recovered and repaired: -{paid} cr.";
            }
            c.Save();
            Rig.Scripted = true;
            if (!fromPod)
            {
                // the loaner leaves first: walk to the pod, then it drops out of the bay
                yield return Walk(Rig.transform.position, Rig.transform.rotation, Mother.PodEyeWorld, Mother.PodLook, 3f);
                Mother.SetDoors(1); while (Mother.DoorsOpen < .98f) { HoldShip(); yield return null; }
                Vector3 p0 = Ship.transform.position; float vy = 0, tt = 0;
                while (tt < 5f) { float dt = Time.deltaTime; tt += dt; vy -= 9.8f * dt; p0 += Vector3.up * vy * dt; Ship.SetPose(p0, Mother.DockRotation, dt, true); yield return null; }
            }
            ClearCrash(); Rig.Scripted = true;
            Extras.SetLook(loaner ? ShipExtras.Look.Loaner : ShipExtras.Look.Own);
            Ship.Spawn(Mother.BelowBay(600f), Mother.DockRotation, CampaignLevel, .3f, c.Current.damage);
            Ship.UsesEnergy = true; Ship.Energy = c.Current.energy; Ship.MakeKinematic(true); Ship.StopEngines();
            Mother.SetDoors(1); Beam(true);
            Vector3 pos = Ship.transform.position;
            while (true)
            {
                float dt = Time.deltaTime; Vector3 below = Mother.BelowBay(6f); float d = (below - pos).magnitude;
                if (d < .05f) break;
                pos = Vector3.MoveTowards(pos, below, Mathf.Min(55f, Mathf.Sqrt(2f * 3f * d) + .3f) * dt);
                Ship.SetPose(pos, Mother.DockRotation, dt, true);
                yield return null;
            }
            yield return RiseIntoBay(pos, Mother.DockRotation);
            Beam(false);
            Ship.StartEngines();
            yield return Walk(Rig.transform.position, Rig.transform.rotation, Ship.transform.TransformPoint(ShipLayout.Eye), Ship.transform.rotation, 3.2f);
            Rig.Scripted = false; Rig.Current = CameraRig.Mode.Cockpit;
            ShowHangar();
        }

        IEnumerator Walk(Vector3 p0, Quaternion r0, Vector3 p1, Quaternion r1, float seconds)
        {
            float t = 0;
            while (t < seconds)
            {
                t += Time.deltaTime; float k = Mathf.SmoothStep(0, 1, t / seconds);
                var p = Vector3.Lerp(p0, p1, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * .6f;
                Rig.transform.SetPositionAndRotation(p, Quaternion.Slerp(r0, r1, k));
                yield return null;
            }
        }

        // ---- pages

        void BuildCampaignPages()
        {
            campaignPage = new MenuPanel.Page { Title = "CAMPAIGN", Subtitle = "A rescue-corps career. It never ends; you never really die.", Back = () => Menu.Show(title) };
            campaignPage.Body = () =>
            {
                var c = Career.Exists ? (Career.C ?? Career.Load()) : null;
                string s = c == null ? "No career yet: start a new one." :
                    $"<b>Your career</b>: the {c.systemName} system\n{c.credits} cr · {c.stats.pilotsRescued} pilots rescued · {c.stats.deaths} deaths · {c.stats.planetsCleared} planets cleared\nLast saved {c.lastSaved}";
                return s + $"\n\nToday's planet: <b>{Names.Planet(Seeds.OfTheDay)}</b> (seed {Seeds.OfTheDay})\n\n<b>{CrashLevels.Names[(int)CampaignLevel]}</b>\n{CrashLevels.Explain[(int)CampaignLevel]}";
            };
            campaignPage.Add(Action("Continue", () => StartCampaign(false), () => !Career.Exists))
                .Add(Action("New career", () => { if (Career.Exists) Menu.Show(confirmNew); else StartCampaign(true); }))
                .Add(Option("Crash level", () => CrashLevels.Names[(int)CampaignLevel], d => { GameSettings.S.campaignCrashLevel = Wrap(GameSettings.S.campaignCrashLevel + d, 5); GameSettings.Save(); }))
                .Add(Action("Back", () => Menu.Show(title)));

            confirmNew = new MenuPanel.Page { Title = "NEW CAREER?", Back = () => Menu.Show(campaignPage) };
            confirmNew.Body = () => { var c = Career.C ?? Career.Load(); return c == null ? "" : $"This replaces your career in the {c.systemName} system:\n{c.credits} cr, {c.stats.pilotsRescued} pilots rescued, {c.planets.Count} planets visited.\n\nIt can't be undone."; };
            confirmNew.Add(Action("No, keep my career", () => Menu.Show(campaignPage)))
                      .Add(Action("Yes, start a new career", () => StartCampaign(true)));

            hangar = new MenuPanel.Page { Title = "HANGAR", Subtitle = "Rescue carrier, high over the planet" };
            hangar.Body = HangarText;
            jumpRow = new MenuPanel.Row { Label = () => "Jump to a new planet" + (Tuning.T.jumpCost > 0 ? $"  <size=24>({Tuning.T.jumpCost} cr)</size>" : "  <size=24>(free)</size>"), Submit = JumpToNew, Disabled = () => Career.C.credits < Tuning.T.jumpCost };
            hangar.Add(Action("Launch", TryLaunch, () => Career.C.Current.energy < 1f))
                  .Add(jumpRow)                                            // Fix 3.2
                  .Add(Action("Planets", ShowPlanets))                     // Fix 3.2: the planet list
                  .Add(new MenuPanel.Row { Label = () => $"Repair hull  <size=24>({Career.C.RepairCost} cr)</size>", Submit = () => { debrief = Career.C.Repair(); Ship.SetEnergyAndDamage(Career.C.Current.energy, Career.C.Current.damage); Career.C.Save(); }, Disabled = () => Career.C.Current.damage <= 0 || Career.C.credits <= 0 })
                  .Add(new MenuPanel.Row { Label = () => $"Refuel  <size=24>({Career.C.RefuelCost} cr)</size>", Submit = () => { debrief = Career.C.Refuel(); Ship.SetEnergyAndDamage(Career.C.Current.energy, Career.C.Current.damage); Career.C.Save(); }, Disabled = () => Career.C.Current.energy >= 99.9f })
                  .Add(new MenuPanel.Row { Label = () => $"Recover your own ship  <size=24>({Career.C.RecoveryFee} cr)</size>", Submit = () => Run(Deliver(false, false)), Visible = () => Career.C != null && Career.C.flyingLoaner && Career.C.ship.wrecked, Disabled = () => Career.C.credits < Career.C.RecoveryFee })
                  .Add(Action("Field Journal", () => ShowJournal(hangar)))   // Round 3
                  .Add(Action("Back to title (saves)", ToTitle));
            jumping = new MenuPanel.Page { Title = "JUMP", Subtitle = "The bay doors are shut; the mothership is under way", Hint = () => "" };
            BuildPlanetsPage();

            // Fix 2.5: launching with little energy asks first (Refuel and launch is the default)
            lowEnergy = new MenuPanel.Page { Title = "LOW ENERGY", Back = () => Menu.Show(hangar) };
            lowEnergy.Body = () =>
            {
                var c = Career.C; int cost = c.RefuelCost;
                return $"Energy {c.Current.energy:0} %. Refuel first? (cost {cost} credits)\n\nYou have {c.credits} cr." +
                       (c.credits < cost ? $" That fills as far as it goes (the corps tops up to {Tuning.T.freeMinimumFill:0} % free)." : "");
            };
            lowEnergy.Add(Action("Refuel and launch", () => { debrief = Career.C.Refuel(); Ship.SetEnergyAndDamage(Career.C.Current.energy, Career.C.Current.damage); Career.C.Save(); Run(Launch()); }))
                     .Add(Action("Launch anyway", () => Run(Launch())))
                     .Add(Action("Back to the hangar", () => Menu.Show(hangar)));

            campaignPause = new MenuPanel.Page { Title = "PAUSED", Back = Resume };
            campaignPause.Body = () => $"{Planet?.name} · {CrashLevels.Names[(int)Ship.Level]}\nQuitting now saves; pilots in your cabin go back to wait on the planet.\n\n" +
                "<b>Reset position</b>: hover over the nearest landing pad (free).\n<b>Recall</b>: the mothership comes and lifts you home (also: hold View / H / HOTAS 7).";
            campaignPause.Add(Action("Resume", Resume))
                         .Add(Action("Reset position", ResetPosition, () => !CanReset))
                         .Add(new MenuPanel.Row { Label = () => $"Recall the mothership  <size=24>({Tuning.T.recallFee} cr)</size>", Submit = () => { Resume(); Recall(); },
                                                  Disabled = () => PausedFrom != State.Flying || calling != 0 || Ship.Crashed })
                         .Add(Action("Settings", () => { settingsReturn = campaignPause; Menu.Show(settings); }))
                         .Add(Action("Save and return to title", ToTitle))
                         .Add(Action("Quit game (saves)", () => { SaveCareer(); Quit(); }))
                         .Add(Action("Field Journal", () => ShowJournal(campaignPause)));   // Round 3

            dead = new MenuPanel.Page { Title = "SHIP DESTROYED" };
            dead.Add(Action("Wake up", () => Run(RegenTrip())));

            regen = new MenuPanel.Page { Title = "REGENERATION BAY", Subtitle = "New body, same mind. Credits and record kept." };
            regen.Body = () => { var c = Career.C; return $"<b>{c.credits} cr</b>\n\nYour ship is a wreck on {Planet?.name}.\n\n<b>Pay recovery</b> ({c.RecoveryFee} cr): it is hauled back and repaired, upgrades kept.\n<b>Take the loaner</b>: a free basic ship. Your own stays wrecked until you pay its recovery from the hangar.\n\n{c.stats.deaths} death{(c.stats.deaths == 1 ? "" : "s")} so far."; };
            regen.Add(new MenuPanel.Row { Label = () => $"Pay recovery  <size=24>({Career.C.RecoveryFee} cr)</size>", Submit = () => Run(Deliver(false, true)), Disabled = () => Career.C.credits < Career.C.RecoveryFee })
                 .Add(Action("Take the loaner (free)", () => Run(Deliver(true, true))));

            BuildJournalPage();
            outOfEnergy = new MenuPanel.Page { Title = "OUT OF ENERGY" };
            outOfEnergy.Body = () => $"The ship is down with no energy left.\nThe mothership can lift it home on its tow beam: {Tuning.T.towFee} cr\n(if you can't pay it all, the corps covers the rest).\n\nPilots aboard stay aboard and are paid at the dock.";
            outOfEnergy.Add(Action("Call the mothership tow", () => Run(Tow())));
        }

        void TryLaunch()
        {
            if (Career.C.Current.energy < Tuning.T.launchEnergyWarning) { lowEnergy.Selected = 0; Menu.Show(lowEnergy); }
            else Run(Launch());
        }

        string HangarText()
        {
            var c = Career.C; if (c == null || Planet == null) return "";
            var s = c.Current; var st = c.stats;
            return $"<b>{c.credits} cr</b>   ·   the {c.systemName} system\n\n" +
                $"<b>Ship</b>: {(c.flyingLoaner ? "the corps loaner (basic)" : "your own")}   hull {100 - s.damage:0}%   energy {s.energy:0}%\n" +
                (c.flyingLoaner && c.ship.wrecked ? $"Your own ship is a wreck: recovery {c.RecoveryFee} cr.\n" : "") +
                (s.energy < 99.9f ? $"<color=#FFC873>Energy {s.energy:0} %: Refuel below ({c.RefuelCost} cr) before you launch.</color>\n" : "") +
                $"\n<b>{(Planet.OfTheDay ? "Planet of the day" : "Planet")}: {Planet.name}</b>  ({Biomes.Name(Planet.biome)}, seed {Planet.seed})\n  pilots: {Planet.Count(PilotFate.Rescued)} rescued · {Planet.Count(PilotFate.Lost)} lost · {Planet.Count(PilotFate.Waiting)} waiting{(Planet.cleared ? "   CLEARED" : "")}\n" +
                (Planet.OfTheDay ? $"  <color=#FFC873>Planet of the day: pilots pay +{Tuning.T.dayBonus * 100:0} % ({Mathf.RoundToInt(Tuning.T.pilotPay * (1 + Tuning.T.dayBonus))} cr each)</color>\n"
                                 : $"  Today's planet, {Names.Planet(Seeds.OfTheDay)}, pays +{Tuning.T.dayBonus * 100:0} % a pilot (Planets)\n") +
                (Planet.cleared ? "  <color=#9CFFB0><b>Cleared! Jump to a new planet (selected).</b></color>\n" : "") +
                $"\n<b>Career</b>\n  {st.pilotsRescued} pilots rescued · {st.pilotsLost} lost · {st.creditsEarned} cr earned\n  {st.deaths} deaths · {st.planetsCleared} planets cleared · {st.sorties} sorties\n" +
                (debrief != "" ? $"\n<b>Last</b>\n{debrief}" : "");
        }
    }
}
