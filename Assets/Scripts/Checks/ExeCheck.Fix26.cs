using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

namespace HostileHorizon
{
    // Fix 2.6 check, in the built exe:  HostileHorizon.exe -exeCheck <dir> -exeCheckOnly fix26
    // The radar's distance labels, nearest highlight and range rings (pictures); the route trail from the launch to the nearest
    // pilot (its path: length, bends, how far it stays down in the canyons), followed by the check's pilot as a player would
    // (cockpit pictures at three points, wall hits, closest to the ground, how near the pad it ends); a re-plan after leaving the
    // route; target cycling; frame rate with the trail on.
    public sealed partial class ExeCheck
    {
        static Vector3 PointAlong(List<Vector3> path, float s)
        {
            float acc = 0;
            for (int i = 1; i < path.Count; i++)
            {
                float len = Vector2.Distance(new Vector2(path[i - 1].x, path[i - 1].z), new Vector2(path[i].x, path[i].z));
                if (acc + len >= s) return Vector3.Lerp(path[i - 1], path[i], (s - acc) / Mathf.Max(len, .01f));
                acc += len;
            }
            return path[path.Count - 1];
        }

        static float PathLength(List<Vector3> path) { float a = 0; for (int i = 1; i < path.Count; i++) a += Vector2.Distance(new Vector2(path[i - 1].x, path[i - 1].z), new Vector2(path[i].x, path[i].z)); return a; }

        string PathReport(List<Vector3> path)
        {
            var f = G.Ship.Field; int bends = 0; float maxT = 0, minClear = 9999, worstAt = 0, acc = 0, turned = 0;
            for (int i = 1; i < path.Count; i++)
            {
                if (i > 1) { float a = Vector3.Angle(Flat(path[i - 1] - path[i - 2]), Flat(path[i] - path[i - 1])); if (a > 20f) bends++; turned += a; }
                float len = Vector2.Distance(new Vector2(path[i - 1].x, path[i - 1].z), new Vector2(path[i].x, path[i].z));
                for (float s = 0; s < len; s += 10f)
                {
                    var p = Vector3.Lerp(path[i - 1], path[i], s / Mathf.Max(len, .01f));
                    maxT = Mathf.Max(maxT, f.CanyonT(p.x, p.z));
                    float c = p.y - f.SurfaceHeight(p.x, p.z); if (c < minClear) { minClear = c; worstAt = acc + s; }
                }
                acc += len;
            }
            var a0 = path[0]; var b0 = path[path.Count - 1];
            return $"{path.Count} points, {acc:0} m long (straight line {Flat(b0 - a0).magnitude:0} m), {bends} bends over 20° ({turned:0}° of turning), highest canyon openness on it {maxT:0.00} (0 floor, 1 rim), lowest clearance over the ground {minClear:0} m at {worstAt:0} m";
        }

        // Where the trail's chevrons fall in the cockpit view: on screen (and not behind the dash's lower edge, y > .3) or not.
        string ChevronsInView()
        {
            var cam = G.Rig.Head; var parts = new List<string>(); int seen = 0;
            foreach (var p in G.Route.Shown)
            {
                var v = cam.WorldToViewportPoint(p); bool on = v.z > 0 && v.x > 0 && v.x < 1 && v.y > .3f && v.y < 1;
                if (on) seen++;
                parts.Add($"{v.z:0} m at ({v.x:0.00}, {v.y:0.00}){(on ? "" : " off")}");
            }
            return $"chevrons in view {seen} / {G.Route.Shown.Count}: " + string.Join(", ", parts);
        }

        // Follow the trail like a player: aim at the path 3 s ahead, at the path's height (and clear of the ground ahead).
        IEnumerator FollowRoute(DownedPilot target, float seconds)
        {
            var s = G.Ship; int hits0 = s.Hits; float minAgl = 9999, end = Time.realtimeSinceStartup + seconds, maxOff = 0; int shots = 0; float total = -1; int lastHits = s.Hits;
            var marks = new[] { .3f, .55f, .8f }; float startLeft = Mathf.Max(1, Flat(target.LandingSpot - s.transform.position).magnitude - 260f);
            while (Time.realtimeSinceStartup < end && G.Now == GameShell.State.Flying)
            {
                var path = G.Route.Path; if (path.Count < 2) { Set(); yield return null; continue; }
                var t = s.transform; G.Route.DistanceToPath(t.position, out float along, out _);
                float len = PathLength(path); if (total < 0) total = len + along;
                float left = Flat(target.LandingSpot - t.position).magnitude;
                if (left < 260f) break;
                var aim = PointAlong(path, along + Mathf.Max(80f, s.Speed * 1.7f));
                var to = aim - t.position; float err = Mathf.DeltaAngle(s.Heading, Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg), bank = BankOf(t);
                float roll = Mathf.Clamp((Mathf.Clamp(err * 2f, -65, 65) - bank) * .05f, -1, 1);
                // the ground ahead and under the wing tips
                float ground = 0;
                for (int m = 0; m <= 6; m++) for (int w = -1; w <= 1; w++) { var q = t.position + t.forward * (m * 25f) + t.right * (w * 7f); ground = Mathf.Max(ground, s.Field.SurfaceHeight(q.x, q.z)); }
                float climb = Mathf.Clamp((aim.y - t.position.y) / Mathf.Max(60f, Flat(to).magnitude), -.3f, .6f);
                if (t.position.y + Mathf.Min(0, s.Velocity.y) * 2f < ground + 25f) climb = Mathf.Max(climb, .55f);   // the sink rate looked ahead 2 s
                float pitch = Mathf.Clamp((climb - t.forward.y) * 3f, -1, 1) + Pull(t);
                float thr = .2f;   // slow (about 160 km/h): the canyons are 50 m wide
                state.leftStick = new Vector2(Firm(roll), -Firm(Mathf.Clamp(pitch, -1, 1))); state.rightTrigger = s.Throttle < thr - .05f ? .6f : 0; state.leftTrigger = s.Throttle > thr + .05f ? .6f : 0; Set();
                if (s.Hits != lastHits) { lastHits = s.Hits; Log($"  HIT {s.Hits - hits0}: {s.AboveGround:0} m over the ground, {s.Speed * 3.6f:0} km/h, bank {bank:0}°, off the route {G.Route.OffRoute:0} m, path {aim.y - s.Field.SurfaceHeight(aim.x, aim.z):0} m over the ground there, hull {100 - s.Damage:0} %"); }
                minAgl = Mathf.Min(minAgl, s.AboveGround); maxOff = Mathf.Max(maxOff, G.Route.OffRoute);
                float done = 1 - (left - 260f) / startLeft;
                if (shots < marks.Length && done > marks[shots]) { Log($"  following: {done * 100:0} % of the way, {left:0} m from the pad, {s.AboveGround:0} m over the ground, {G.Route.Shown.Count} chevrons shown, off the route {G.Route.OffRoute:0} m; {ChevronsInView()}"); yield return Shot($"a{3 + shots}_trail_cockpit_{shots + 1}"); shots++; }
                yield return null;
            }
            state.leftStick = Vector2.zero; state.rightTrigger = 0; state.leftTrigger = 0; Set();
            Log($"  followed the trail: ended {Flat(target.LandingSpot - s.transform.position).magnitude:0} m from pilot {target.Index + 1}'s pad, wall / ground hits {s.Hits - hits0}, hull {100 - s.Damage:0} %, lowest {minAgl:0} m over the ground, furthest off the route {maxOff:0} m, re-plans {G.Route.Plans} (last: {G.Route.LastReplanReason}, {G.Route.LastPlanMs:0} ms)");
        }

        IEnumerator Fix26()
        {
            StartCoroutine(Progress());
            yield return WaitFor(() => G.Now == GameShell.State.Title, 60f);
            yield return new WaitForSecondsRealtime(1.5f);
            GameSettings.S.campaignCrashLevel = 1;   // Bounce: a wall hit is counted, not the end of the run
            Log("Campaign crash level for this check: Bounce (hits are counted)");
            yield return Nav(GamepadButton.DpadDown, 1); yield return Press(GamepadButton.South);   // Campaign
            yield return Nav(GamepadButton.DpadDown, 1); yield return Press(GamepadButton.South);   // New career
            yield return WaitPhase(() => G.Now == GameShell.State.Docked, 60f, "the hangar");
            G.Menu.Current.Selected = 0; yield return Press(GamepadButton.South);                  // Launch
            yield return WaitPhase(() => G.Now == GameShell.State.Flying, 40f, "flight after launch");
            yield return new WaitForSecondsRealtime(1.5f);

            // ---- 1. the radar
            var sp = G.Ship; var con = G.Displays.Contacts; int near = RadarScope.Nearest(sp.transform, con);
            foreach (var c in con) Log($"radar contact {(c.mothership ? "mothership" : $"pilot {c.number}")}: {RadarScope.Distance(Flat(c.position - sp.transform.position).magnitude)}{(con.IndexOf(c) == near ? "  (NEAREST)" : "")}");
            Log($"radar range at the rim {G.Displays.RadarRange:0} m; list: {RadarScope.List(sp.transform, con, 5).Replace("\n", " | ")}");
            yield return Shot("a1_radar_cockpit");

            // ---- 2. the trail to the nearest pilot
            yield return WaitFor(() => G.Route.Path.Count > 1, 15f);
            var target = G.RoutePilot;
            Log($"route target: {G.RouteTargetName} (auto = nearest pilot on foot: {(target != null ? $"pilot {target.Index + 1}, {Flat(target.LandingSpot - sp.transform.position).magnitude:0} m" : "none")}); planned in {G.Route.LastPlanMs:0} ms; display \"{G.Displays.Route}\"");
            Log($"route: {PathReport(G.Route.Path)}");
            yield return Shot("a2_trail_after_launch");
            // follow it low, through the canyons: Reset position (60 m over the nearest pad, by the start), then flight mode
            yield return ResetVia("to fly the trail low", 1);
            yield return SetMode(FlightMode.Flight);
            yield return new WaitForSecondsRealtime(1.5f); yield return WaitFor(() => G.Route.Path.Count > 1 && G.Route.OffRoute < 50f, 6f);
            target = G.RoutePilot;
            Log($"route from low down (target {G.RouteTargetName}, {Flat(target.LandingSpot - sp.transform.position).magnitude:0} m): {PathReport(G.Route.Path)}");
            if (target != null) yield return FollowRoute(target, 240f);
            Log($"route at the end: {PathReport(G.Route.Path)}; chevrons shown {G.Route.Shown.Count}; pad ring for the route's pilot");
            yield return Shot("a6_trail_end_at_pad");

            // ---- re-plan after leaving the route: 400 m to the side, high over the ground
            if (G.Route.Path.Count > 1)
            {
                int plans = G.Route.Plans; var t = sp.transform; var side = Vector3.Cross(Vector3.up, t.forward).normalized;
                var at = t.position + side * 400f; at.y = Mathf.Max(t.position.y, sp.Field.SurfaceHeight(at.x, at.z) + 150f);
                sp.Spawn(at, t.rotation, G.CampaignLevel, .4f, sp.Damage); G.Rig.Snap();
                yield return null; float off = G.Route.OffRoute;
                yield return WaitFor(() => G.Route.Plans > plans && G.Route.OffRoute < 60f, 5f);
                Log($"re-plan after leaving the route: {off:0} m off -> re-plans {plans} -> {G.Route.Plans} (\"{G.Route.LastReplanReason}\"), now {G.Route.OffRoute:0} m off, {G.Route.LastPlanMs:0} ms");
                yield return Shot("a7_replanned");
            }

            // ---- target cycling
            for (int k = 0; k < 6; k++) { Log($"route target {k}: {G.RouteTargetName}; \"{G.Displays.Route}\""); yield return Press(GamepadButton.DpadRight); yield return new WaitForSecondsRealtime(.5f); }
            yield return WaitFor(() => G.Route.Path.Count > 1, 5f);

            // ---- frame rate with the trail on (flying low through the canyon)
            yield return MeasureFps($"Campaign, trail on, flying low{(SteamDeck.On ? " (Steam Deck setting)" : "")}");
            Log($"trail during the fps run: chevrons shown {G.Route.Shown.Count}, re-plans {G.Route.Plans}, last plan {G.Route.LastPlanMs:0} ms");
            GameSettings.S.routeTrail = false; yield return null;
            yield return MeasureFps($"Campaign, trail OFF (to compare), flying low{(SteamDeck.On ? " (Steam Deck setting)" : "")}");
            GameSettings.S.routeTrail = true;
        }
    }
}
