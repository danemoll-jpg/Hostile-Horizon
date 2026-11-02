using UnityEngine;
using UnityEngine.UI;

namespace HostileHorizon
{
    // Round 1, item 3: speed, altitude and the rest, in two places (VR rule 2):
    //  - in the cockpit: three displays on the dash (world-space canvases at 1:1, readable from the pilot's eye);
    //  - on the flat screen: a small overlay in the corners (shown in every view).
    // Both show the same facts: speed, throttle, altitude, height above the ground, heading, hull damage, crash level, seed, view.
    public sealed class FlightDisplays : MonoBehaviour
    {
        public ShipController Ship;
        public CameraRig Rig;
        Text left, centre, right, screenLeft, screenRight, warning;
        Canvas overlay;
        float flash;

        public static FlightDisplays Create(ShipController ship, Transform shipModel, CameraRig rig)
        {
            var d = new GameObject("Flight displays").AddComponent<FlightDisplays>();
            d.Ship = ship; d.Rig = rig;
            // cockpit: three screens on the dash, each turned to face the pilot's eye
            d.centre = Screen(shipModel, "Centre display", ShipLayout.DashCentre + new Vector3(0, .17f, -.34f), new Vector2(420, 250));
            d.left = Screen(shipModel, "Left display", ShipLayout.DashCentre + new Vector3(-.33f, .14f, -.31f), new Vector2(360, 250));
            d.right = Screen(shipModel, "Right display", ShipLayout.DashCentre + new Vector3(.33f, .14f, -.31f), new Vector2(360, 250));
            // flat screen
            d.overlay = UiKit.ScreenCanvas("Screen HUD", 5);
            var t = d.overlay.transform;
            UiKit.Panel(t, "Left back", new Color(0, 0, 0, .35f), new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -250), new Vector2(430, -24));
            d.screenLeft = UiKit.Label(t, "Left", 28, TextAnchor.UpperLeft, UiKit.Ink, new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -240), new Vector2(420, -36));
            UiKit.Panel(t, "Right back", new Color(0, 0, 0, .35f), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-470, -110), new Vector2(-24, -24));
            d.screenRight = UiKit.Label(t, "Right", 24, TextAnchor.UpperRight, UiKit.Ink, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-460, -150), new Vector2(-40, -36));
            d.warning = UiKit.Label(t, "Warning", 40, TextAnchor.MiddleCenter, new Color(1f, .35f, .25f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-400, 140), new Vector2(400, 220));
            return d;
        }

        static Text Screen(Transform ship, string name, Vector3 at, Vector2 size)
        {
            var c = UiKit.WorldCanvas(name, ship, size, .0007f);
            c.transform.localPosition = at;
            c.transform.localRotation = Quaternion.LookRotation(at - ShipLayout.Eye, Vector3.up);
            var bg = UiKit.Panel(c.transform, "Glass", new Color(.02f, .05f, .05f, .96f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var frame = UiKit.Panel(c.transform, "Edge", new Color(1f, .62f, .32f, .35f), new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -4), Vector2.zero);
            var t = UiKit.Label(c.transform, "Text", size.x < 400 ? 26 : 30, TextAnchor.UpperLeft, new Color(.55f, 1f, .85f), Vector2.zero, Vector2.one, new Vector2(16, 10), new Vector2(-12, -12));
            t.GetComponent<Shadow>().enabled = false;
            t.lineSpacing = .95f;
            return t;
        }

        public void SetVisible(bool on) { overlay.gameObject.SetActive(on); enabled = on; }

        static string Bar(float v01, int n) { int k = Mathf.RoundToInt(Mathf.Clamp01(v01) * n); return new string('█', k) + new string('·', n - k); }

        void LateUpdate()
        {
            if (!Ship) return;
            float spd = Ship.Speed, alt = Ship.Altitude, agl = Ship.AboveGround, hdg = Ship.Heading, thr = Ship.Throttle * 100, dmg = Ship.Damage;
            var lvl = Ship.Level; string level = CrashLevels.Names[(int)lvl];
            int seed = Ship.Field != null ? Ship.Field.SeedNumber : 0;
            string hull = lvl == CrashLevel.Regenerating || lvl == CrashLevel.Lasting ? $"HULL {100 - dmg:0}%" : lvl == CrashLevel.Instant ? "HULL one hit" : "HULL no damage";
            bool low = agl < 25 && !Ship.Crashed;

            centre.text = $"SPD  {spd * 3.6f:0} km/h\nALT  {alt:0} m\nGND  {agl:0} m{(low ? "  LOW" : "")}\nHDG  {hdg:000}°";
            left.text = $"THROTTLE {thr:0}%\n{Bar(thr / 100f, 10)}\n{hull}";
            right.text = $"SEED\n{seed}\n{level}\nVIEW {CameraRig.ModeNames[(int)Rig.Current]}";

            screenLeft.text = $"<size=36><b>{spd * 3.6f:0}</b></size> km/h   thr {thr:0}%\nALT {alt:0} m    GND <b>{agl:0}</b> m\nHDG {hdg:000}°\n{hull}";
            screenRight.text = $"Seed {seed}   ·   {level}\nView: {CameraRig.ModeNames[(int)Rig.Current]}";
            flash = Ship.Hits > 0 && Time.time - lastHitTime < 1f ? 1 : 0;
            warning.text = Ship.Crashed ? "" : flash > 0 ? (lvl == CrashLevel.Bounce ? "IMPACT" : lvl == CrashLevel.Ghost ? "" : $"IMPACT  −{Ship.LastImpact * 1.25f:0}%") : low ? "TERRAIN" : "";
        }

        float lastHitTime = -9;
        public void NoteHit() => lastHitTime = Time.time;
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
