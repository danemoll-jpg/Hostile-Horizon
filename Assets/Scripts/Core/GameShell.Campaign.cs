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
        MenuPanel.Page campaignPage, confirmNew, hangar, dead, regen, outOfEnergy, campaignPause;

        int Waiting { get { int n = 0; foreach (var p in Pilots) if (p.OnFoot) n++; return n; } }
        bool CanDock => cabin.Count > 0 || Waiting == 0 || Ship.Energy < Tuning.T.dockAnyTimeBelowEnergy;
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
            int seed = Seeds.OfTheDay;
            SetWorld(seed);
            Planet = c.Planet(seed); Planet.visits++;
            Terrain.Field.FindStart(out var start, out var heading);
            Mother.gameObject.SetActive(true); Mother.Place(start, heading); Mother.Holding = true; Mother.SetDoors(0, true);
            SpawnPilots();
            c.Save();
            Ship.Field = Terrain.Field; Rig.Field = Terrain.Field;
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

        void SpawnPilots()
        {
            foreach (var p in Pilots) if (p) Destroy(p.gameObject);
            Pilots.Clear();
            var sites = PlanetSites.Find(Terrain.Field, Planet.pilots.Length, Planet.tier);
            if (sites.Count < Planet.pilots.Length) System.Array.Resize(ref Planet.pilots, sites.Count);   // a planet short of landable spots has fewer pilots
            for (int i = 0; i < Planet.pilots.Length; i++)
            {
                if (Planet.pilots[i] != (int)PilotFate.Waiting) continue;
                var p = DownedPilot.Create(i, sites[i], Terrain.Field, litTemplate);
                p.OnBoarded += PilotBoarded;
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
            InCampaign = false; Phase = "";
            if (Displays) Displays.Campaign = false;
            if (Ship) { Ship.UsesEnergy = false; Ship.Kinematic = false; }
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
            sb.AppendLine($"{Planet.name} cleared: bonus +{Tuning.T.clearBonus} cr");
        }

        void ShowHangar()
        {
            var sb = new StringBuilder(); CheckCleared(sb);
            if (sb.Length > 0) { debrief = (debrief + "\n" + sb).Trim(); Career.C.Save(); }
            Now = State.Docked; Phase = "docked"; Time.timeScale = 1;
            Mother.Holding = true; Ship.MakeKinematic(true);
            Displays.Cabin = cabin.Count; Displays.Prompt = "Docked in the mothership"; Displays.Contacts.Clear();
            Displays.SetVisible(false); Menu.SetBackdrop(.15f); Menu.Show(hangar);
        }

        void HoldDocked() { if (Phase == "docked") HoldShip(); }
        void HoldShip() => Ship.SetPose(Mother.DockPosition, Mother.DockRotation, Time.deltaTime, true);

        IEnumerator Launch()
        {
            Now = State.Auto; Phase = "launch"; Menu.Hide(); Displays.SetVisible(true);
            var c = Career.C; c.stats.sorties++;
            Ship.SetEnergyAndDamage(c.Current.energy, c.Current.damage);
            cabin.Clear(); dockArmed = false; debrief = "";
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
            Mother.Holding = false; Phase = ""; Now = State.Flying;
            Flash("Find the beacons on the radar. Landing mode: X / G / HOTAS 4", 6f);
        }

        // ---- flying the planet

        void CampaignTick()
        {
            float dt = Time.deltaTime; var t = Tuning.T;
            if (!Mother.Holding) Mother.Follow(Ship.transform.position, Ship.transform.forward, dt);
            bool free = cabin.Count < t.cabinSize;
            foreach (var p in Pilots) if (p.Now != DownedPilot.State.Aboard) p.Tick(Ship, free, dt);

            if (Ship.InputEnabled && Time.frameCount > Ship.IgnoreButtonsUntil && Controls.AirlockPressed)
            {
                if (!Ship.Landed) Flash("The airlock only opens on the ground", 2.5f);
                else { Extras.OpenAirlock(4f); foreach (var p in Pilots) if (p.AtDoor && cabin.Count < t.cabinSize) p.Board(); }
            }

            // the radar: beacons still waiting, and the mothership once you can dock
            var con = Displays.Contacts; con.Clear();
            foreach (var p in Pilots) if (p.OnFoot) con.Add(new RadarContact { position = p.Beacon, number = p.Index + 1 });
            if (CanDock) con.Add(new RadarContact { position = Mother.transform.position, mothership = true });
            Displays.Cabin = cabin.Count;
            Displays.Place = $"{Planet.name} · {CrashLevels.Names[(int)Ship.Level]}";
            Displays.Prompt = Time.time < flashUntil ? flash : Advice();

            if (Ship.Energy < t.energyWarning && Ship.EnginesOn) { beepTimer -= dt; if (beepTimer <= 0) { beepTimer = 4f; Sfx.Play(Sfx.Beep, Ship.transform.TransformPoint(ShipLayout.DashCentre), .5f, 2f); } }

            if (Ship.Landed && Ship.OutOfEnergy) { Now = State.Auto; Phase = "stranded"; Displays.SetVisible(false); Menu.SetBackdrop(0); Menu.Show(outOfEnergy); return; }
            if (Ship.Altitude < t.dockingHeight - 50f) dockArmed = true;
            if (dockArmed && Ship.Altitude > t.dockingHeight && CanDock) Run(Dock(false));
        }

        // What to do next, on the centre display and the HUD.
        string Advice()
        {
            var t = Tuning.T;
            foreach (var p in Pilots) if (p.AtDoor) return "Pilot at the airlock: open it (B / O / HOTAS 6)";
            foreach (var p in Pilots) if (p.Now == DownedPilot.State.Running) return "Pilot running to the ship: keep the engines off";
            foreach (var p in Pilots) if (p.Now == DownedPilot.State.Boarding) return "Pilot coming aboard";
            if (Ship.Landed)
            {
                if (cabin.Count >= t.cabinSize) return "Cabin full: take off and dock with the mothership";
                foreach (var p in Pilots) if (p.OnFoot && Vector3.Distance(p.Position, Ship.transform.position) < t.pickupRange)
                        return Ship.EnginesOn ? "Engines off (A / K / HOTAS 5) so the pilot can come to you" : "Waiting for the pilot";
                return Ship.EnginesOn ? "Landed. Climb (throttle up) to take off" : "Engines off. Throttle up or A / K to start them";
            }
            if (cabin.Count >= t.cabinSize) return $"Cabin full: climb above {t.dockingHeight:0} m to dock";
            if (Waiting == 0) return $"Everyone found: climb above {t.dockingHeight:0} m to dock";
            if (Ship.Mode == FlightMode.Hover) return "Landing mode: stick moves and turns, throttle up / down, set down gently";
            float nearest = float.MaxValue; foreach (var p in Pilots) if (p.OnFoot) nearest = Mathf.Min(nearest, Vector3.Distance(p.Position, Ship.transform.position));
            if (nearest < 900f) return "Pilot close by: landing mode (X / G / HOTAS 4) to set down";
            if (cabin.Count > 0 && dockArmed) return $"{cabin.Count} aboard: more pilots, or climb above {t.dockingHeight:0} m to dock";
            return "";
        }

        void PilotBoarded(DownedPilot p)
        {
            cabin.Add(p.Index);
            Flash($"Pilot {p.Index + 1} aboard ({cabin.Count} / {Tuning.T.cabinSize})", 3.5f);
        }

        // Engines started near pilots on foot: the exhaust kills those within its range.
        void OnEnginesStarted()
        {
            if (!InCampaign) return;
            var t = Tuning.T; var c = Career.C;
            foreach (var p in Pilots)
            {
                if (!p.OnFoot || Vector3.Distance(p.Position, Ship.transform.position) > t.exhaustKillRange) continue;
                p.Kill(); Planet.pilots[p.Index] = (int)PilotFate.Lost; c.stats.pilotsLost++;
                int paid = c.Spend(t.exhaustKillPenalty);
                Flash($"Pilot {p.Index + 1} killed by your exhaust. Penalty -{paid} cr", 6f);
            }
        }

        // ---- docking (and the tow's lift), live

        IEnumerator Dock(bool towLift)
        {
            Now = State.Auto; Phase = towLift ? "tow-lift" : "dock-approach"; Mother.Holding = true;
            Ship.MakeKinematic(false);
            Vector3 pos = Ship.transform.position, vel = towLift ? Vector3.zero : Ship.Velocity; Quaternion rot = Ship.transform.rotation; float bank = 0;
            Displays.Prompt = towLift ? "Tow: the mothership is lifting you" : "Docking: autopilot";
            if (!towLift)
            {
                // A: fly round to a point behind and below the bay, lined up with it
                while (true)
                {
                    float dt = Time.deltaTime; if (dt <= 0) { yield return null; continue; }
                    Vector3 a = Mother.BelowBay(60f) - Mother.DockRotation * Vector3.forward * 350f;
                    Vector3 to = a - pos; if (to.magnitude < 80f) break;
                    Vector3 before = Flat(vel);
                    vel = Vector3.MoveTowards(vel, to.normalized * 90f, 15f * dt);
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
                Vector3 e = Mother.BelowBay(60f), to = e - pos; float d = to.magnitude;
                if (d < .3f || d < 1.5f && vel.magnitude < 1.5f) break;
                float cap = Mathf.Min(towLift ? 45f : 70f, Mathf.Sqrt(2f * (towLift ? 2.5f : 4f) * d));
                vel = Vector3.MoveTowards(vel, to / d * cap, (towLift ? 4f : 12f) * dt);
                pos += vel * dt;
                Quaternion want = Mother.DockRotation;
                if (!towLift && vel.sqrMagnitude > 4f) want = Quaternion.Slerp(Quaternion.LookRotation(vel), Mother.DockRotation, Mathf.InverseLerp(30f, 6f, vel.magnitude));
                rot = Quaternion.Slerp(rot, want, 1 - Mathf.Exp(-(towLift ? .8f : 2.5f) * dt));
                if (d < 450f) Mother.SetDoors(1);
                Ship.SetPose(pos, rot, dt, d < 400f);
                yield return null;
            }
            // C: straight up into the bay, slowing to the cradle
            Phase = "dock-rise"; Mother.SetDoors(1);
            yield return RiseIntoBay(pos, rot);
            Beam(false);
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
                pos = Vector3.MoveTowards(pos, dock, (Mathf.Min(7f, Mathf.Sqrt(2f * 1.2f * d)) + .1f) * dt);
                rot = Quaternion.Slerp(rot, Mother.DockRotation, 1 - Mathf.Exp(-2f * dt));
                Ship.SetPose(pos, rot, dt, true);
                yield return null;
            }
            Ship.SetPose(Mother.DockPosition, Mother.DockRotation, Time.deltaTime, true);
            Sfx.Play(Sfx.Thunk, Ship.transform.position + Vector3.up * 1.5f, 1f, 8f);
            Mother.SetDoors(0);
        }

        // At the dock: pilots paid, planet bonus, tow fee, fuel used charged (repair is the hangar's Repair button).
        void DebriefAtDock(bool towLift)
        {
            var c = Career.C; var t = Tuning.T; var sb = new StringBuilder();
            if (cabin.Count > 0)
            {
                int pay = cabin.Count * t.pilotPay; c.Earn(pay); c.stats.pilotsRescued += cabin.Count;
                foreach (int i in cabin) Planet.pilots[i] = (int)PilotFate.Rescued;
                sb.AppendLine($"Delivered {cabin.Count} pilot{(cabin.Count > 1 ? "s" : "")}: +{pay} cr");
                cabin.Clear();
            }
            CheckCleared(sb);
            if (towLift) { int paid = c.Spend(t.towFee); c.stats.tows++; sb.AppendLine($"Tow: -{paid} cr" + (paid < t.towFee ? $" (the corps covered the other {t.towFee - paid} cr)" : "")); }
            c.Current.energy = Ship.Energy; c.Current.damage = Ship.Damage;
            float used = 100 - Ship.Energy;
            string fuel = c.Refuel();
            if (fuel != "") sb.AppendLine($"Energy used {used:0}%. " + fuel);
            Ship.Energy = c.Current.energy;
            if (c.Current.damage > 0) sb.AppendLine($"Hull damage {c.Current.damage:0}%: repair {c.RepairCost} cr (Repair below, or fly as it is)");
            c.Save();
            debrief = sb.ToString().TrimEnd();
        }

        // ---- out of energy: the tow

        IEnumerator Tow()
        {
            Now = State.Auto; Phase = "tow-wait"; Menu.Hide(); Displays.SetVisible(true);
            Displays.Prompt = "Tow: the mothership is coming";
            Mother.Holding = false;
            while (!Mother.MoveOver(Ship.transform.position, Time.deltaTime)) { Displays.Prompt = "Tow: the mothership is coming"; yield return null; }
            Mother.Holding = true; Mother.SetDoors(1);
            Beam(true);
            float t = 0; while (t < 1.5f) { t += Time.deltaTime; yield return null; }
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
        }

        // ---- death and regeneration

        void Died()
        {
            var c = Career.C; Phase = "dead";
            int lost = cabin.Count;
            foreach (int i in cabin) Planet.pilots[i] = (int)PilotFate.Lost;
            c.stats.pilotsLost += lost; c.stats.deaths++; cabin.Clear();
            if (c.flyingLoaner) { c.flyingLoaner = false; c.loaner = new ShipState(); }   // the loaner is gone; your own ship stays where it is
            else { c.ship.wrecked = true; c.ship.damage = 100; c.ship.energy = Ship.Energy; }
            c.Save();
            dead.Body = () => (lost > 0 ? $"{lost} pilot{(lost > 1 ? "s" : "")} aboard died with you. " : "") +
                "Your mind was backed up on the mothership.\n\nThe wreck burns where you hit.";
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
            hangar.Add(Action("Launch", () => Run(Launch()), () => Career.C.Current.energy < 1f))
                  .Add(new MenuPanel.Row { Label = () => $"Repair hull  <size=24>({Career.C.RepairCost} cr)</size>", Submit = () => { debrief = Career.C.Repair(); Ship.SetEnergyAndDamage(Career.C.Current.energy, Career.C.Current.damage); Career.C.Save(); }, Disabled = () => Career.C.Current.damage <= 0 || Career.C.credits <= 0 })
                  .Add(new MenuPanel.Row { Label = () => $"Refuel  <size=24>({Career.C.RefuelCost} cr)</size>", Submit = () => { debrief = Career.C.Refuel(); Ship.SetEnergyAndDamage(Career.C.Current.energy, Career.C.Current.damage); Career.C.Save(); }, Disabled = () => Career.C.Current.energy >= 99.9f })
                  .Add(new MenuPanel.Row { Label = () => $"Recover your own ship  <size=24>({Career.C.RecoveryFee} cr)</size>", Submit = () => Run(Deliver(false, false)), Visible = () => Career.C != null && Career.C.flyingLoaner && Career.C.ship.wrecked, Disabled = () => Career.C.credits < Career.C.RecoveryFee })
                  .Add(Action("Back to title (saves)", ToTitle));

            campaignPause = new MenuPanel.Page { Title = "PAUSED", Back = Resume };
            campaignPause.Body = () => $"{Planet?.name} · {CrashLevels.Names[(int)Ship.Level]}\nQuitting now saves; pilots in your cabin go back to wait on the planet.";
            campaignPause.Add(Action("Resume", Resume))
                         .Add(Action("Settings", () => { settingsReturn = campaignPause; Menu.Show(settings); }))
                         .Add(Action("Save and return to title", ToTitle))
                         .Add(Action("Quit game (saves)", () => { SaveCareer(); Quit(); }));

            dead = new MenuPanel.Page { Title = "SHIP DESTROYED" };
            dead.Add(Action("Wake up", () => Run(RegenTrip())));

            regen = new MenuPanel.Page { Title = "REGENERATION BAY", Subtitle = "New body, same mind. Credits and record kept." };
            regen.Body = () => { var c = Career.C; return $"<b>{c.credits} cr</b>\n\nYour ship is a wreck on {Planet?.name}.\n\n<b>Pay recovery</b> ({c.RecoveryFee} cr): it is hauled back and repaired, upgrades kept.\n<b>Take the loaner</b>: a free basic ship. Your own stays wrecked until you pay its recovery from the hangar.\n\n{c.stats.deaths} death{(c.stats.deaths == 1 ? "" : "s")} so far."; };
            regen.Add(new MenuPanel.Row { Label = () => $"Pay recovery  <size=24>({Career.C.RecoveryFee} cr)</size>", Submit = () => Run(Deliver(false, true)), Disabled = () => Career.C.credits < Career.C.RecoveryFee })
                 .Add(Action("Take the loaner (free)", () => Run(Deliver(true, true))));

            outOfEnergy = new MenuPanel.Page { Title = "OUT OF ENERGY" };
            outOfEnergy.Body = () => $"The ship is down with no energy left.\nThe mothership can lift it home on its tow beam: {Tuning.T.towFee} cr\n(if you can't pay it all, the corps covers the rest).\n\nPilots aboard stay aboard and are paid at the dock.";
            outOfEnergy.Add(Action("Call the mothership tow", () => Run(Tow())));
        }

        string HangarText()
        {
            var c = Career.C; if (c == null || Planet == null) return "";
            var s = c.Current; var st = c.stats;
            return $"<b>{c.credits} cr</b>   ·   the {c.systemName} system\n\n" +
                $"<b>Ship</b>: {(c.flyingLoaner ? "the corps loaner (basic)" : "your own")}   hull {100 - s.damage:0}%   energy {s.energy:0}%\n" +
                (c.flyingLoaner && c.ship.wrecked ? $"Your own ship is a wreck: recovery {c.RecoveryFee} cr.\n" : "") +
                $"\n<b>Today's planet: {Planet.name}</b>  (seed {Planet.seed})\n  pilots: {Planet.Count(PilotFate.Rescued)} rescued · {Planet.Count(PilotFate.Lost)} lost · {Planet.Count(PilotFate.Waiting)} waiting{(Planet.cleared ? "   CLEARED" : "")}\n" +
                $"\n<b>Career</b>\n  {st.pilotsRescued} pilots rescued · {st.pilotsLost} lost · {st.creditsEarned} cr earned\n  {st.deaths} deaths · {st.planetsCleared} planets cleared · {st.sorties} sorties\n" +
                (debrief != "" ? $"\n<b>Last</b>\n{debrief}" : "");
        }
    }
}
