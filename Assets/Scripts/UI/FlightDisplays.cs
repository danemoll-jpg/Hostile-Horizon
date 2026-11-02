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
            d.centre = Screen(shipModel, "Centre display", ShipLayout.DashCentre + new Vector3(0, .17f, -.34f), new Vector2(420, 250), 26, out _);
            d.left = Screen(shipModel, "Left display", ShipLayout.DashCentre + new Vector3(-.33f, .14f, -.31f), new Vector2(400, 250), 26, out _);
            d.right = Screen(shipModel, "Right display", ShipLayout.DashCentre + new Vector3(.33f, .14f, -.31f), new Vector2(400, 250), 24, out var rc);
            d.cockpitRadar = new RadarScope(rc.transform, new Vector2(112, 132), 210, Vector2.zero);
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
            d.screenRadar = new RadarScope(t, new Vector2(-190, 190), 300, new Vector2(1, 0));
            // Fix 2.4: the landing state (BRAKING, HOVER with the height in big type, TOUCHDOWN, LANDED ...) and the pause hint
            d.stateLine = UiKit.Label(t, "State", 46, TextAnchor.MiddleCenter, new Color(.6f, 1f, .85f), new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(-620, 250), new Vector2(620, 330));
            d.hint = UiKit.Label(t, "Pause hint", 22, TextAnchor.LowerLeft, UiKit.Dim, new Vector2(0, 0), new Vector2(0, 0), new Vector2(30, 20), new Vector2(500, 60));
            d.listBack = UiKit.Panel(t, "List back", new Color(0, 0, 0, .35f), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-470, 352), new Vector2(-24, 520));
            d.screenList = UiKit.Label(t, "List", 22, TextAnchor.LowerRight, UiKit.Ink, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-460, 360), new Vector2(-40, 512));
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

        // Fix 2.4: what the ship is doing in the landing steps, big on the HUD and on the cockpit's centre display.
        public string StateText
        {
            get
            {
                if (!Ship || Ship.Kinematic || Ship.Crashed) return "";
                if (Ship.Braking) return $"BRAKING  {Ship.Speed * 3.6f:0} km/h";
                if (Ship.Landed)
                {
                    if (Time.time - Ship.LandedAt < 2.5f) return "TOUCHDOWN";
                    if (!Ship.EnginesOn) return "LANDED — engines off";
                    return Campaign && PilotNear ? $"LANDED — engines off ({Controls.EngineKeys.Split(' ')[0]}) so the pilot comes" : "LANDED";
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
            string hull = lvl == CrashLevel.Regenerating || lvl == CrashLevel.Lasting ? $"HULL {100 - dmg:0}%" : lvl == CrashLevel.Instant ? "HULL one hit" : "HULL no damage";
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
            bool homeOn = Campaign && !Ship.Kinematic && !Ship.Crashed;
            string home = homeOn ? Home : "", homeWarn = homeOn ? HomeWarning : "";

            centre.text = (st != "" ? $"<size=22><color=#FFFFFF>{st}</color></size>\n" : "") +
                $"SPD {spd * 3.6f:0} km/h   HDG {hdg:000}°\nALT {alt:0} m   GND {agl:0} m{(low ? " LOW" : "")}\n<size=21>{mode} · {gear} · {engines}</size>\n<size=20><color=#FFC873>{say}</color></size>" +
                (PauseHint ? "\n<size=18>≡ / Esc: Pause</size>" : "");
            stateLine.text = st; hint.text = PauseHint ? "≡ / Esc: Pause" : "";
            string cue = Cue != "" && Campaign && !Ship.Kinematic && Ship.Mode == FlightMode.Flight ? $"<color=#FF9A40><b>{Cue}</b></color>\n" : "";
            // the way home first: the lower lines of this display sit below the flat screen's view
            string shortWarn = HomeState(homeWarn);
            left.text = (Campaign && home != "" ? (shortWarn != "" ? $"<color=#FF6A50>{shortWarn}</color>\n" : "") + $"<size=22>{home.Replace("\n", " · ")}</size>\n" : "") +
                $"{power}  {(hover ? "" : Bar(thr / 100f, 8))}\n" + (Campaign ? $"ENERGY {energy:0}%  {Bar(energy / 100f, 8)}{(energyLow && shortWarn == "" ? " LOW" : "")}\n{hull}  {cabin}" : hull);
            if (Campaign)
            {
                right.text = "";
                rightList.text = (cue != "" ? "<size=19><color=#FF9A40><b>SLOW DOWN — PRESS X</b></color></size>\n" : "") + RadarScope.List(Ship.transform, Contacts, 5).Replace("PILOT ", "P").Replace("MOTHERSHIP", "MS") + $"\n<size=16>{Place}</size>";
                cockpitRadar.Draw(Ship.transform, Contacts); screenRadar.Draw(Ship.transform, Contacts);
            }
            else { right.text = $"SEED\n{seed}\n{level}\nVIEW {CameraRig.ModeNames[(int)Rig.Current]}"; rightList.text = ""; }
            cockpitRadar.Show(Campaign); screenRadar.Show(Campaign); listBack.enabled = Campaign;

            screenLeft.text = $"<size=34><b>{spd * 3.6f:0}</b></size> km/h   {power.ToLower()}\nALT {alt:0} m    GND <b>{agl:0}</b> m\nHDG {hdg:000}°   {mode}\n{gear} · {engines}\n{hull}" +
                (Campaign ? $"\nENERGY <b>{energy:0}%</b>   {cabin}" + (home != "" ? "\n" + home.Replace("\n", "   ") : "") : "");
            screenRight.text = Campaign ? $"{Place}\nView: {CameraRig.ModeNames[(int)Rig.Current]}" : $"Seed {seed}   ·   {level}\nView: {CameraRig.ModeNames[(int)Rig.Current]}";
            screenList.text = Campaign ? cue + RadarScope.List(Ship.transform, Contacts, 6) : "";
            prompt.text = say;
            bool flash = Ship.Hits > 0 && Time.time - lastHitTime < 1f;
            warning.text = Ship.Crashed ? "" : flash ? (lvl == CrashLevel.Bounce ? "IMPACT" : lvl == CrashLevel.Ghost ? "" : $"IMPACT  −{Ship.LastImpact * 1.25f:0}%")
                : Ship.OutOfEnergy && !Ship.Landed && !Ship.Kinematic ? "OUT OF ENERGY" : low ? "TERRAIN" : homeWarn != "" ? (Time.time % 1f < .7f ? homeWarn : "")
                : energyLow && !Ship.Kinematic && Time.time % 1f < .6f ? "ENERGY LOW" : "";
        }

        public void NoteHit() => lastHitTime = Time.time;

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
