using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HostileHorizon
{
    // Round 1, item 3: speed, altitude and the rest, in two places (VR rule 2):
    //  - in the cockpit: three displays on the dash (world-space canvases at 1:1, readable from the pilot's eye);
    //  - on the flat screen: a small overlay in the corners (shown in every view).
    // Both show the same facts. Round 2 adds: flight / hover mode, gear, engines, energy (warning at 15 %), cabin, the radar
    // (cockpit right display and the HUD's lower right) and a prompt line (what to do next: "Pilot knocking: open the airlock").
    // Fix 2.3: the way home (mothership bearing and distance, "DOCK ↑ <m>" still to climb) and the RETURN NOW / CAN'T REACH warning,
    // on the cockpit's left display and on the HUD.
    public sealed class FlightDisplays : MonoBehaviour
    {
        public ShipController Ship;
        public CameraRig Rig;
        public bool Campaign;
        public int Cabin, CabinSize;
        public string Prompt = "", Place = "";
        public string Home = "", HomeWarning = "";   // Fix 2.3, set by GameShell.Campaign while flying
        public string Cue = "";                      // Fix 2.4: SLOW DOWN - PRESS X, on the radar
        public bool PilotNear, PauseHint;            // Fix 2.4: a waiting pilot within pickup range; the first minute of a flight
        // Fix 2.5: the pickup, always on the HUD and in the cockpit: PILOT AT AIRLOCK — press B / PILOT COMING (set by the Campaign),
        // the lit AIRLOCK button over the centre display, KNOCK KNOCK KNOCK with each knock, and the belly / airlock camera feed
        public string PickupLine = "";
        public bool AirlockLit;
        public BellyCam Belly;
        // Fix 2.6: the route trail's target line ("ROUTE → PILOT 2 · 1.4 km") and its path, drawn on the radar
        public string Route = "";
        public string Subtitle = "";                 // Round 4 (C4): what is being said (Voices), on the HUD and the centre display
        // Round 5 (B4, B5, B6): the shield, LOCKED ON, incoming-fire marks, a hit's damage, the enemies left
        public bool Shield, LockedOn;
        public List<(Vector3 from, float until)> Threats;
        public string FightLine = "";
        float shotAt = -9, shotDmg;
        public void NoteShot(float dmg) { shotAt = Time.time; shotDmg = dmg; }
        readonly List<Text> threatArrows = new List<Text>();
        Text subtitle;
        public List<Vector3> RoutePath;
        float knockUntil; int knockCount;
        // Round 3: one KNOCK a knock, as they come (the code is three; a Knocker may give four)
        public void Knock(int n) { knockUntil = Time.time + 1.4f; knockCount = n; }
        public bool Knocking => Time.time < knockUntil;
        public string KnockText => Knocking ? string.Join(" ", System.Linq.Enumerable.Repeat("KNOCK", Mathf.Max(1, knockCount))) : "";
        // Round 3: the horror sequence's darkness (every cockpit display dies), and the cabin camera: a small cockpit display and, on
        // request (the CabinCam button), a picture-in-picture on the HUD
        public bool Blackout, CabinPip;
        public Cabin CabinRoom;
        RawImage cabinFeed, cabinPip; Text cabinText, cabinPipText; Image cabinPipBack; Canvas cabinCanvas;
        readonly List<Image> darks = new List<Image>();
        RawImage centreFeed, screenFeed; Text centreFeedText, screenFeedText, airlockText; Image airlockLamp, screenFeedBack;
        public readonly List<RadarContact> Contacts = new List<RadarContact>();
        Text left, centre, right, rightList, screenLeft, screenRight, screenList, warning, prompt, stateLine, hint;
        RadarScope cockpitRadar, screenRadar; Image listBack;
        Canvas overlay;
        float lastHitTime = -9;

        public static FlightDisplays Create(ShipController ship, Transform shipModel, CameraRig rig)
        {
            var d = new GameObject("Flight displays").AddComponent<FlightDisplays>();
            d.Ship = ship; d.Rig = rig;
            // cockpit: three screens on the dash, each turned to face the pilot's eye
            d.centre = Screen(shipModel, "Centre display", ShipLayout.DashCentre + new Vector3(0, .17f, -.34f), new Vector2(420, 250), 26, out var cc);
            // Fix 2.5: the belly camera fills the centre display when it is on, with the height and the pickup line over it
            d.centreFeed = UiKit.Rect(cc.transform, "Belly feed", Vector2.zero, Vector2.one, new Vector2(4, 4), new Vector2(-4, -6)).gameObject.AddComponent<RawImage>(); d.centreFeed.raycastTarget = false;
            d.centreFeedText = UiKit.Label(cc.transform, "Belly feed text", 21, TextAnchor.UpperLeft, new Color(.6f, 1f, .85f), Vector2.zero, Vector2.one, new Vector2(12, 8), new Vector2(-10, -10));
            d.centreFeedText.GetComponent<Shadow>().effectDistance = new Vector2(1.5f, -1.5f);
            // Fix 2.5: the AIRLOCK lamp: a strip across the top of the centre display, lit and blinking amber when a pilot is at the door
            d.airlockLamp = UiKit.Panel(cc.transform, "Airlock lamp", new Color(1f, .62f, .2f), new Vector2(0, 1), new Vector2(1, 1), new Vector2(4, -50), new Vector2(-4, -6));
            d.airlockText = UiKit.Label(cc.transform, "Airlock label", 28, TextAnchor.MiddleCenter, new Color(.1f, .05f, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(4, -50), new Vector2(-4, -6));
            d.airlockText.GetComponent<Shadow>().enabled = false;
            d.left = Screen(shipModel, "Left display", ShipLayout.DashCentre + new Vector3(-.33f, .14f, -.31f), new Vector2(400, 250), 26, out _);
            d.right = Screen(shipModel, "Right display", ShipLayout.DashCentre + new Vector3(.33f, .14f, -.31f), new Vector2(400, 250), 24, out var rc);
            d.cockpitRadar = new RadarScope(rc.transform, new Vector2(112, 132), 210, Vector2.zero, 15);
            d.rightList = UiKit.Label(rc.transform, "List", 17, TextAnchor.UpperLeft, new Color(.55f, 1f, .85f), Vector2.zero, Vector2.one, new Vector2(226, 40), new Vector2(-6, -14));
            d.rightList.GetComponent<Shadow>().enabled = false;
            // flat screen
            d.overlay = UiKit.ScreenCanvas("Screen HUD", 5);
            var t = d.overlay.transform;
            UiKit.Panel(t, "Left back", new Color(0, 0, 0, .35f), new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -300), new Vector2(470, -24));
            d.screenLeft = UiKit.Label(t, "Left", 26, TextAnchor.UpperLeft, UiKit.Ink, new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -290), new Vector2(460, -36));
            UiKit.Panel(t, "Right back", new Color(0, 0, 0, .35f), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-470, -110), new Vector2(-24, -24));
            d.screenRight = UiKit.Label(t, "Right", 24, TextAnchor.UpperRight, UiKit.Ink, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-460, -150), new Vector2(-40, -36));
            d.warning = UiKit.Label(t, "Warning", 40, TextAnchor.MiddleCenter, new Color(1f, .35f, .25f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-500, 140), new Vector2(500, 220));
            d.prompt = UiKit.Label(t, "Prompt", 30, TextAnchor.UpperCenter, new Color(1f, .8f, .45f), new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(-480, -150), new Vector2(480, -36));   // top centre: clear of the cockpit displays
            d.screenRadar = new RadarScope(t, new Vector2(-190, 190), 300, new Vector2(1, 0), 19);
            // Fix 2.4: the landing state (BRAKING, HOVER with the height in big type, TOUCHDOWN, LANDED ...) and the pause hint
            d.stateLine = UiKit.Label(t, "State", 46, TextAnchor.MiddleCenter, new Color(.6f, 1f, .85f), new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(-620, 250), new Vector2(620, 330));
            d.subtitle = UiKit.Label(t, "Subtitle", 28, TextAnchor.UpperCenter, new Color(.92f, .96f, 1f), new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(-640, -250), new Vector2(640, -150));   // under the prompt, clear of the cockpit displays
            d.hint = UiKit.Label(t, "Pause hint", 22, TextAnchor.LowerLeft, UiKit.Dim, new Vector2(0, 0), new Vector2(0, 0), new Vector2(30, 20), new Vector2(500, 60));
            d.listBack = UiKit.Panel(t, "List back", new Color(0, 0, 0, .35f), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-470, 352), new Vector2(-24, 520));
            d.screenList = UiKit.Label(t, "List", 22, TextAnchor.LowerRight, UiKit.Ink, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-460, 360), new Vector2(-40, 512));
            // Fix 2.5: the belly camera as a picture-in-picture on the left, between the readouts and the cockpit's left display
            d.screenFeedBack = UiKit.Panel(t, "Belly back", new Color(0, 0, 0, .6f), new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(20, -250), new Vector2(474, 30));
            d.screenFeed = UiKit.Rect(t, "Belly feed", new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(24, -246), new Vector2(470, 19)).gameObject.AddComponent<RawImage>(); d.screenFeed.raycastTarget = false;
            d.screenFeedText = UiKit.Label(t, "Belly feed text", 22, TextAnchor.UpperLeft, new Color(.6f, 1f, .85f), new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(34, -240), new Vector2(466, 14));
            // Round 3: the cabin camera, a small display high on the left of the canopy frame, and its HUD picture-in-picture (right)
            var ct = Screen(shipModel, "Cabin display", ShipLayout.Eye + new Vector3(-.62f, -.18f, .55f), new Vector2(300, 190), 18, out d.cabinCanvas);
            d.cabinFeed = UiKit.Rect(d.cabinCanvas.transform, "Cabin feed", Vector2.zero, Vector2.one, new Vector2(4, 4), new Vector2(-4, -24)).gameObject.AddComponent<RawImage>(); d.cabinFeed.raycastTarget = false;
            d.cabinText = ct; ct.alignment = TextAnchor.UpperLeft; ct.rectTransform.offsetMin = new Vector2(8, 4); ct.rectTransform.offsetMax = new Vector2(-6, -2); ct.transform.SetAsLastSibling();
            d.cabinPipBack = UiKit.Panel(t, "Cabin back", new Color(0, 0, 0, .6f), new Vector2(1, .5f), new Vector2(1, .5f), new Vector2(-474, 0), new Vector2(-20, 262));
            d.cabinPip = UiKit.Rect(t, "Cabin feed", new Vector2(1, .5f), new Vector2(1, .5f), new Vector2(-470, 4), new Vector2(-24, 255)).gameObject.AddComponent<RawImage>(); d.cabinPip.raycastTarget = false;
            d.cabinPipText = UiKit.Label(t, "Cabin feed text", 20, TextAnchor.UpperLeft, new Color(.6f, 1f, .85f), new Vector2(1, .5f), new Vector2(1, .5f), new Vector2(-462, 4), new Vector2(-30, 252));
            foreach (var c in shipModel.GetComponentsInChildren<Canvas>(true)) if (c.renderMode == RenderMode.WorldSpace && c != d.cabinCanvas) { var dk = UiKit.Panel(c.transform, "Dark", new Color(0, 0, 0, .985f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero); dk.enabled = false; d.darks.Add(dk); }
            return d;
        }

        static Text Screen(Transform ship, string name, Vector3 at, Vector2 size, int font, out Canvas c)
        {
            c = UiKit.WorldCanvas(name, ship, size, .0007f);
            c.transform.localPosition = at;
            c.transform.localRotation = Quaternion.LookRotation(at - ShipLayout.Eye, Vector3.up);
            UiKit.Panel(c.transform, "Glass", new Color(.02f, .05f, .05f, .96f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            UiKit.Panel(c.transform, "Edge", new Color(1f, .62f, .32f, .35f), new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -4), Vector2.zero);
            var t = UiKit.Label(c.transform, "Text", font, TextAnchor.UpperLeft, new Color(.55f, 1f, .85f), Vector2.zero, Vector2.one, new Vector2(16, 10), new Vector2(-12, -12));
            t.GetComponent<Shadow>().enabled = false;
            t.lineSpacing = .95f;
            return t;
        }

        public void SetVisible(bool on) => overlay.gameObject.SetActive(on);   // the cockpit displays stay live (they are part of the ship)
        public bool Visible => overlay.gameObject.activeSelf;
        public float RadarRange => cockpitRadar.Range;   // Fix 2.6: metres at the radar's rim (checks)

        // Fix 2.4: what the ship is doing in the landing steps, big on the HUD and on the cockpit's centre display.
        public string StateText
        {
            get
            {
                if (!Ship || Ship.Kinematic || Ship.Crashed) return "";
                if (Ship.Braking) return $"BRAKING  {Ship.Speed * 3.6f:0} km/h";
                if (Ship.Landed)
                {
                    if (Campaign && PickupLine != "") return PickupLine;
                    if (Time.time - Ship.LandedAt < 2.5f) return "TOUCHDOWN";
                    if (!Ship.EnginesOn) return "LANDED — engines off";
                    return Campaign && PilotNear ? $"LANDED — engines off ({Controls.EngineKey}) so the pilot comes" : "LANDED";
                }
                if (Ship.Mode == FlightMode.Hover) return $"HOVER   GND <b>{Ship.AboveGround:0}</b> m   {Ship.Velocity.y:+0;-0;0} m/s";
                return "";
            }
        }

        static string Bar(float v01, int n) { int k = Mathf.RoundToInt(Mathf.Clamp01(v01) * n); return new string('█', k) + new string('·', n - k); }

        void LateUpdate()
        {
            if (!Ship) return;
            float spd = Ship.Speed, alt = Ship.Altitude, agl = Ship.AboveGround, hdg = Ship.Heading, thr = Ship.Throttle * 100, dmg = Ship.Damage;
            var lvl = Ship.Level; string level = CrashLevels.Names[(int)lvl];
            int seed = Ship.Field != null ? Ship.Field.SeedNumber : 0;
            string hull = Campaign || lvl == CrashLevel.Regenerating || lvl == CrashLevel.Lasting ? $"HULL {100 - dmg:0}%" : lvl == CrashLevel.Instant ? "HULL one hit" : "HULL no damage";   // Round 5: in the Campaign enemy fire always counts
            bool hover = Ship.Mode == FlightMode.Hover;
            bool low = agl < 25 && !Ship.Crashed && !hover && !Ship.Kinematic;
            string mode = Ship.Kinematic ? "AUTOPILOT" : Ship.Landed ? "LANDED" : Ship.Braking ? "BRAKING" : hover ? "HOVER" : "FLIGHT";
            string st = StateText;
            string gear = Ship.Gear > .95f ? "GEAR DOWN" : Ship.Gear > .05f ? "GEAR MOVING" : "gear up";
            string engines = Ship.OutOfEnergy ? "NO ENERGY" : Ship.EnginesOn ? "engines on" : "ENGINES OFF";
            string power = hover ? $"LIFT {(Ship.Lift > .05f ? "UP" : Ship.Lift < -.05f ? "DOWN" : "HOLD")}" : $"THROTTLE {thr:0}%";
            float energy = Ship.Energy; bool energyLow = Campaign && energy < Tuning.T.energyWarning;
            string cabin = Campaign ? $"CABIN {Cabin}/{CabinSize}" : "";
            string notice = Ship.Notice;
            string say = notice != "" && !Ship.Kinematic ? notice : Prompt;   // autopilot / beam: what is happening now
            if (Ship.Kinematic && Campaign && say != "" && say != "Docked in the mothership")
                say += Time.timeScale > 1.5f ? $"  ·  ×{Time.timeScale:0} (holding {Controls.FastForwardKey})" : $"  ·  hold {Controls.FastForwardKey} to speed up";
            bool homeOn = Campaign && !Ship.Kinematic && !Ship.Crashed;
            string home = homeOn ? Home : "", homeWarn = homeOn ? HomeWarning : "";

            if (Knocking) st = KnockText;
            bool shot = Campaign && Time.time - shotAt < 1.2f, lockBlink = Time.time % .6f < .4f;
            string fight = !Campaign || Ship.Kinematic || Ship.Crashed ? "" : (shot ? $"<color=#FF5A4A><b>HIT −{shotDmg:0}%</b></color>  " : LockedOn && lockBlink ? "<color=#FF5A4A><b>LOCKED ON</b></color>  " : "") + (Shield ? "<color=#8FD8FF>SHIELD ON</color>" : "");
            centre.text = (st != "" ? $"<size=22><color=#FFFFFF>{st}</color></size>\n" : fight != "" ? $"<size=22>{fight}</size>\n" : "") +
                $"SPD {spd * 3.6f:0} km/h   HDG {hdg:000}°\nALT {alt:0} m   GND {agl:0} m{(low ? " LOW" : "")}\n<size=21>{mode} · {gear} · {engines}</size>\n<size=20><color=#FFC873>{say}</color></size>" +
                (PauseHint ? "\n<size=18>≡ / Esc: Pause</size>" : "") + (Subtitle != "" ? $"\n<size=17><color=#DDEEFF>{Subtitle}</color></size>" : "");
            subtitle.text = Subtitle;
            stateLine.text = st; hint.text = PauseHint ? "≡ / Esc: Pause" : "";
            // Fix 2.5: the AIRLOCK button, the knock, the belly / airlock camera
            bool lit = Campaign && AirlockLit, blink = Time.time % .8f < .55f;
            airlockLamp.enabled = airlockText.enabled = lit;
            airlockLamp.color = blink ? new Color(1f, .62f, .2f) : new Color(.6f, .34f, .1f);
            airlockText.text = $"<b>AIRLOCK — press {Controls.AirlockKey}</b>";
            string knock = KnockText;
            bool feed = Belly && Belly.On;
            centreFeed.enabled = centreFeedText.enabled = feed; centre.enabled = !feed;
            screenFeed.enabled = screenFeedText.enabled = screenFeedBack.enabled = feed;
            if (feed)
            {
                centreFeed.texture = screenFeed.texture = Belly.Feed;
                var hv = new Vector2(Ship.Velocity.x, Ship.Velocity.z).magnitude;
                string top = $"{Belly.Title}   GND <b>{agl:0}</b> m   {Ship.Velocity.y:+0;-0;0} m/s   {hv * 3.6f:0} km/h";
                string line = knock != "" ? $"<color=#FFB040><b>{knock}</b></color>" : PickupLine != "" ? $"<color=#FFC873><b>{PickupLine}</b></color>" : (st != "" && !st.StartsWith("HOVER") ? st : "");
                string bottom = $"<size=17>▲ nose   ({Controls.BellyKey}: camera on / off)</size>";
                centreFeedText.text = top + "\n" + line + "\n\n\n" + (Subtitle != "" ? $"<size=16><color=#DDEEFF>{Subtitle}</color></size>" : "") + "\n" + bottom;
                screenFeedText.text = top + "\n" + line + "\n\n\n\n\n" + bottom;
            }
            string cue = Cue != "" && Campaign && !Ship.Kinematic && Ship.Mode == FlightMode.Flight ? $"<color=#FF9A40><b>{Cue}</b></color>\n" : "";
            // the way home first: the lower lines of this display sit below the flat screen's view
            string shortWarn = HomeState(homeWarn);
            left.text = (Campaign && home != "" ? (shortWarn != "" ? $"<color=#FF6A50>{shortWarn}</color>\n" : "") + $"<size=22>{home.Replace("\n", " · ")}</size>\n" : "") +
                $"{power}  {(hover ? "" : Bar(thr / 100f, 8))}\n" + (Campaign ? $"ENERGY {energy:0}%  {Bar(energy / 100f, 8)}{(energyLow && shortWarn == "" ? " LOW" : "")}\n{hull}  {cabin}{(Shield ? "  <color=#8FD8FF>SHIELD</color>" : "")}" : hull);
            if (Campaign)
            {
                right.text = "";
                rightList.text = (cue != "" ? $"<size=19><color=#FF9A40><b>SLOW DOWN — PRESS {Controls.LandKey}</b></color></size>\n" : "") + "\n" +   // Fix 2.6: the blips carry their distances; beside the scope: the route and the planet
                    (Route != "" ? $"<size=18><color=#7DFFB0>{Route.Replace(" · ", "\n").Replace("   [", "\n<size=15>[").Replace("]", "]</size>")}</color></size>\n\n" : "") + $"<size=16>{Place}</size>";
                cockpitRadar.Draw(Ship.transform, Contacts, RoutePath, Threats); screenRadar.Draw(Ship.transform, Contacts, RoutePath, Threats);
            }
            else { right.text = $"SEED\n{seed}\n{level}\nVIEW {CameraRig.ModeNames[(int)Rig.Current]}"; rightList.text = ""; }
            cockpitRadar.Show(Campaign); screenRadar.Show(Campaign); listBack.enabled = Campaign;
            // Round 3: the cabin camera
            bool cab = Campaign && CabinRoom && !Ship.Crashed;
            cabinCanvas.gameObject.SetActive(cab);
            if (cab)
            {
                bool live = CabinRoom.FeedOn;
                cabinFeed.texture = cabinPip.texture = CabinRoom.Feed; cabinFeed.enabled = live;
                cabinText.text = $"<size=16>CABIN CAM · {Cabin} aboard{(live ? "" : " · empty")}</size>";
                cabinPip.enabled = cabinPipBack.enabled = cabinPipText.enabled = CabinPip && live;
                cabinPipText.text = $"CABIN CAM   {Cabin} aboard   <size=16>({Controls.CabinCamKey}: off)</size>";
            }
            else cabinPip.enabled = cabinPipBack.enabled = cabinPipText.enabled = false;
            // Round 3: the lights die (the horror sequence): every cockpit display goes dark but the cabin camera's
            foreach (var dk in darks) dk.enabled = Blackout;

            screenLeft.text = $"<size=34><b>{spd * 3.6f:0}</b></size> km/h   {power.ToLower()}\nALT {alt:0} m    GND <b>{agl:0}</b> m\nHDG {hdg:000}°   {mode}\n{gear} · {engines}\n{hull}" +
                (Campaign ? $"\nENERGY <b>{energy:0}%</b>   {cabin}{(Shield ? "   <color=#8FD8FF><b>SHIELD ON</b></color>" : "")}" + (home != "" ? "\n" + home.Replace("\n", "   ") : "") + (FightLine != "" ? $"\n<color=#FF9A90>{FightLine}</color>" : "") : "");
            screenRight.text = Campaign ? $"{Place}\nView: {CameraRig.ModeNames[(int)Rig.Current]}" : $"Seed {seed}   ·   {level}\nView: {CameraRig.ModeNames[(int)Rig.Current]}";
            screenList.text = Campaign ? cue + RadarScope.List(Ship.transform, Contacts, 5) + (Route != "" ? $"<color=#7DFFB0>{Route}</color>" : "") : "";
            prompt.text = say;
            bool flash = Ship.Hits > 0 && Time.time - lastHitTime < 1f;
            ThreatArrows();
            warning.text = Ship.Crashed ? "" : Campaign && Knocking ? KnockText.Replace(" ", "  ") : flash ? (lvl == CrashLevel.Bounce ? "IMPACT" : lvl == CrashLevel.Ghost ? "" : $"IMPACT  −{Ship.LastImpact * 1.25f:0}%")
                : shot ? $"HIT  −{shotDmg:0}%" : Campaign && LockedOn && !Ship.Kinematic ? (lockBlink ? "LOCKED ON" : "")
                : Ship.OutOfEnergy && !Ship.Landed && !Ship.Kinematic ? "OUT OF ENERGY" : low ? "TERRAIN" : homeWarn != "" ? (Time.time % 1f < .7f ? homeWarn : "")
                : energyLow && !Ship.Kinematic && Time.time % 1f < .6f ? "ENERGY LOW" : "";
        }

        public void NoteHit() => lastHitTime = Time.time;

        // Round 5 (B6): incoming fire on the HUD: a red arrow on a ring round the screen's centre, pointing toward the shooter
        // (straight up = ahead), fading over 2.5 s. The cockpit has the same on the radar's rim.
        void ThreatArrows()
        {
            int n = 0;
            if (Campaign && Threats != null && !Ship.Crashed && Visible)
                foreach (var th in Threats)
                {
                    if (n >= 8) break;
                    if (threatArrows.Count <= n)
                    {
                        var a = UiKit.Label(overlay.transform, "Threat", 54, TextAnchor.MiddleCenter, new Color(1f, .2f, .15f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-40, -40), new Vector2(40, 40));
                        a.text = "▲"; threatArrows.Add(a);
                    }
                    var l = Ship.transform.InverseTransformPoint(th.from); float ang = Mathf.Atan2(l.x, l.z);
                    var t = threatArrows[n++]; t.enabled = true;
                    var at = new Vector2(Mathf.Sin(ang), Mathf.Cos(ang)) * 330f;
                    t.rectTransform.anchoredPosition = at; t.rectTransform.localRotation = Quaternion.Euler(0, 0, -ang * Mathf.Rad2Deg);
                    t.color = new Color(1f, .2f, .15f, Mathf.Clamp01((th.until - Time.time) / 2.5f));
                }
            for (int i = n; i < threatArrows.Count; i++) threatArrows[i].enabled = false;
        }

        static string HomeState(string warn) => warn.StartsWith("CAN'T") ? "CAN'T REACH: RECALL OR LAND" : warn;
    }

    public static class CrashLevels
    {
        public static readonly string[] Names = { "Ghost", "Bounce", "Regenerating damage", "Lasting damage", "Instant crash" };
        public static readonly string[] Explain =
        {
            "Pass through the rock. Nothing can hurt you.",
            "Bounce off the rock. No damage.",
            "Hits damage the hull; it repairs itself over time. 100 % and the ship is lost.",
            "Hits damage the hull and it stays damaged. 100 % and the ship is lost.",
            "Any touch of the rock ends the flight.",
        };
    }
}
