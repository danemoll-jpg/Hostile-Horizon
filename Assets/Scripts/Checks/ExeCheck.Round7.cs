using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

namespace HostileHorizon
{
    // Round 7 check, in the built exe:  HostileHorizon.exe -exeCheck <dir> -exeCheckOnly round7a | round7music | round7aliens | round7b | round7bperf
    // Part A: (1) the music: each flight track's loop crossfade three times over (a long play by seeking), then a low flight in each
    // biome along the route trail and past pilots, then a pickup: the share of flying time the 0.6.3 rule went quiet against the new
    // one, and what played; (2) the five creatures up close, the 0.6.3 staging ("before") and the new one: on the canopy, at the
    // strike, in the cabin camera; measured: the nearest part of the body to the eye, the eye inside the body or not (rays from the
    // eye through the posed mesh), parts through the canopy glass; and the order of events at each new strike (nothing before it is
    // in view); (3) the [Play] lines. Part B: ExeCheck.Round7B.cs.
    public sealed partial class ExeCheck
    {
        static readonly AlienType[] Five = { AlienType.Mimic, AlienType.Hollow, AlienType.Knocker, AlienType.Stalker, AlienType.Sleeper };
        readonly List<Vector3> r7v = new List<Vector3>(); readonly List<int> r7t = new List<int>();

        // ---- the posed mesh: nearest point to the eye, the eye inside it or not (odd crossings on most of 7 rays), canopy glass
        string Body(Creature c, Transform eye, bool canopy = false)
        {
            if (!c) return "no creature";
            c.Surface(r7v, r7t);
            // the cockpit camera sits on the ship's eye point at render time (measured a frame or two after the picture, the camera
            // itself is a frame behind a moving ship)
            var e = eye == G.Rig.Head.transform ? G.Ship.transform.TransformPoint(ShipLayout.Eye) : eye.position; float near = float.MaxValue; int inNear = 0;
            foreach (var v in r7v) { float d = (v - e).magnitude; near = Mathf.Min(near, d); if (d < G.Rig.Head.nearClipPlane) inNear++; }
            var dirs = new[] { eye.forward, -eye.forward, eye.up, -eye.up, eye.right, -eye.right, (eye.forward + eye.up + eye.right).normalized };
            int odd = 0;
            foreach (var dir in dirs)
            {
                int hits = 0;
                for (int i = 0; i + 2 < r7t.Count; i += 3) if (RayTri(e, dir, r7v[r7t[i]], r7v[r7t[i + 1]], r7v[r7t[i + 2]])) hits++;
                if (hits % 2 == 1) odd++;
            }
            string s = $"face {Vector3.Distance(e, c.HeadWorld):0.00} m, nearest part {near:0.00} m from the eye, {inNear} vertices nearer than the near clip ({G.Rig.Head.nearClipPlane:0.00} m), eye {(odd >= 4 ? "INSIDE the body" : "outside the body")} ({odd}/7 rays odd), {r7v.Count} vertices / {r7t.Count / 3} triangles drawn (forceMeshLod {(c.Rig ? c.Rig.Renderers[0].forceMeshLod : -9)}); parent {(c.transform.parent ? c.transform.parent.name : "none")}, face in ship space {G.Ship.transform.InverseTransformPoint(c.HeadWorld).ToString("0.00")} (eye {ShipLayout.Eye.ToString("0.00")}), ship {G.Ship.Velocity.magnitude:0} m/s{(G.Ship.Kinematic ? " kinematic" : "")}";
            if (canopy)
            {
                var st = G.Ship.transform; int inside = 0; float deepest = 0;
                foreach (var v in r7v)
                {
                    var q = st.InverseTransformPoint(v) - GameShell.CanopyC; var k = new Vector3(q.x / GameShell.CanopyR.x, q.y / GameShell.CanopyR.y, q.z / GameShell.CanopyR.z);
                    if (k.sqrMagnitude < 1) { inside++; deepest = Mathf.Max(deepest, 1 - k.magnitude); }
                }
                s += $"; {inside} vertices inside the canopy bubble ({inside * 100f / Mathf.Max(1, r7v.Count):0.0} %{(inside > 0 ? $", up to {deepest * 100:0} % of the way to its centre" : "")})";
            }
            return s;
        }

        static bool RayTri(Vector3 o, Vector3 d, Vector3 a, Vector3 b, Vector3 c)
        {
            var e1 = b - a; var e2 = c - a; var p = Vector3.Cross(d, e2); float det = Vector3.Dot(e1, p);
            if (Mathf.Abs(det) < 1e-9f) return false;
            float inv = 1 / det; var t = o - a; float u = Vector3.Dot(t, p) * inv; if (u < 0 || u > 1) return false;
            var q = Vector3.Cross(t, e1); float v = Vector3.Dot(d, q) * inv; if (v < 0 || u + v > 1) return false;
            return Vector3.Dot(e2, q) * inv > 1e-5f;
        }

        // 0.6.3's look for the "before" pictures: one-sided materials, Mesh LOD free
        void OldLook(bool old)
        {
            foreach (var n in CharacterRig.Models) { if (n == "pilot") continue; var m = Resources.Load<Material>("Characters/" + n); if (m) m.SetFloat("_Cull", old ? 2f : 0f); }
            G.CheckOldStaging = old;
        }

        void SaveCam(Camera cam, string name)
        {
            var rt = cam.targetTexture; cam.Render();
            var prev = RenderTexture.active; RenderTexture.active = rt;
            var t = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false); t.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); t.Apply();
            RenderTexture.active = prev; File.WriteAllBytes(Path.Combine(dir, name + ".png"), t.EncodeToPNG()); Destroy(t);
            Log($"screenshot {name}.png");
        }

        // ---- the five models: size, open edges (welded by position: UV seams don't count), Mesh LOD levels, material
        void CreatureModelReport()
        {
            Log("--- the five creature models");
            foreach (var type in Five)
            {
                string n = CharacterRig.ModelFor(type);
                var holder = new GameObject("model report"); var rig = CharacterRig.Create(n, holder.transform);
                if (!rig) { Log($"  {n}: no model (code-made figure)"); Destroy(holder); continue; }
                foreach (var r in rig.Renderers)
                {
                    var sm = r as SkinnedMeshRenderer; if (!sm) continue;
                    var mesh = new Mesh(); sm.BakeMesh(mesh, true);   // the imported mesh isn't CPU-readable in a build; a baked copy is
                    var verts = mesh.vertices; var key = new int[verts.Length]; var weld = new Dictionary<Vector3Int, int>();
                    for (int i = 0; i < verts.Length; i++) { var k = Vector3Int.RoundToInt(verts[i] * 10000f); if (!weld.TryGetValue(k, out int w)) weld[k] = w = weld.Count; key[i] = w; }
                    var edges = new Dictionary<long, int>(); int tris = 0;
                    for (int s = 0; s < mesh.subMeshCount; s++)
                    {
                        var t = mesh.lodCount > 1 ? mesh.GetTriangles(s, 0, true) : mesh.GetTriangles(s); tris += t.Length / 3;
                        for (int i = 0; i < t.Length; i += 3)
                            for (int e = 0; e < 3; e++)
                            {
                                int a = key[t[i + e]], b = key[t[i + (e + 1) % 3]]; long id = (long)Mathf.Min(a, b) << 32 | (uint)Mathf.Max(a, b);
                                edges[id] = edges.TryGetValue(id, out int c) ? c + 1 : 1;
                            }
                    }
                    int open = edges.Values.Count(c => c == 1), over = edges.Values.Count(c => c > 2);
                    var m = r.sharedMaterial;
                    Destroy(mesh);
                    Log($"  {n}: {verts.Length} vertices, {tris} triangles (full detail), Mesh LOD levels {sm.sharedMesh.lodCount}; welded edges {edges.Count}: OPEN (one triangle) {open} ({open * 100f / Mathf.Max(1, edges.Count):0.00} %), shared by 3+ {over}; " +
                        $"material {m?.name}: shader {m?.shader.name}, cull {(m && m.HasProperty("_Cull") ? m.GetFloat("_Cull").ToString("0") : "?")} (0 both sides, 2 back culled), surface {(m && m.HasProperty("_Surface") ? (m.GetFloat("_Surface") == 0 ? "opaque" : "TRANSPARENT") : "?")}, alpha clip {(m && m.HasProperty("_AlphaClip") ? m.GetFloat("_AlphaClip").ToString("0") : "?")}, base colour alpha {(m ? m.GetColor("_BaseColor").a : -1):0.00}");
                }
                Destroy(holder);
            }
        }

        // ---- (1) the music: each track's loop crossfade, three passes (about 20 minutes of play over the seven tracks)
        IEnumerator MusicLoops(IEnumerable<string> tracks)
        {
            var m = G.Music; Log($"--- music loops (music folder {m.Folder})");
            foreach (var tr in tracks)
            {
                m.CheckWant = tr; m.CheckTension = false;
                yield return WaitFor(() => m.Playing == tr, 30f);
                if (m.Playing != tr) { Log($"  {tr}: NOT PLAYING (missing?)"); continue; }
                yield return new WaitForSecondsRealtime(1f);
                var lp = m.LoopPoints(tr); var res = new List<string>();
                for (int pass = 0; pass < 3; pass++)
                {
                    m.CheckSeekMain(lp.y - lp.z - 1.5f);
                    bool fade = false; float t0 = Time.realtimeSinceStartup;
                    while (Time.realtimeSinceStartup - t0 < lp.z + 3f) { fade |= m.MainCrossfading; yield return null; }
                    float at = m.MainTime; bool inLoop = at > lp.x - lp.z && at < lp.x + 6f;
                    res.Add($"pass {pass + 1}: crossfade {(fade ? "yes" : "NO")}, now at {at:0.0} s ({(inLoop ? "back in the loop" : "NOT in the loop")}), gain {m.PlayingVolume:0.00}, {(m.Playing == tr ? "playing" : "STOPPED")}");
                }
                Log($"  {tr}: file {m.MainLength:0.0} s, loop {lp.x:0.0} - {lp.y:0.0} s (crossfade {lp.z:0.0} s); " + string.Join("; ", res));
            }
            m.CheckWant = null;
        }

        // ---- (1) the music on a low flight: 0.6.3's rule against the new one, sampled every frame
        float r7Fly, r7OldQuiet, r7NewQuiet, r7Heard, r7Pick, r7PickQuiet, r7Closest, r7Low, r7LowOld, r7LowNew; bool r7Sample; string r7Phase = "";
        IEnumerator SampleMusic()
        {
            r7Sample = true;
            while (r7Sample)
            {
                float dt = Time.deltaTime;
                if (G.Now == GameShell.State.Flying && dt > 0)
                {
                    bool oldQ = G.OldMusicQuiet(), newQ = G.PickupQuiet() != null, heard = G.Music.Playing != "" && G.Music.PlayingVolume > .3f;
                    if (r7Phase == "fly" && G.Ship.AboveGround < 250f) { r7Low += dt; if (oldQ) r7LowOld += dt; if (newQ) r7LowNew += dt; }
                    if (r7Phase == "fly") { r7Fly += dt; if (oldQ) r7OldQuiet += dt; if (newQ) r7NewQuiet += dt; if (heard) r7Heard += dt; foreach (var p in G.Pilots) if (p.OnFoot) r7Closest = Mathf.Min(r7Closest, Flat(p.Position - G.Ship.transform.position).magnitude); }
                    if (r7Phase == "pickup") { r7Pick += dt; if (!heard) r7PickQuiet += dt; }
                }
                yield return null;
            }
        }

        IEnumerator MusicFlight(Biome b)
        {
            if (G.Planet.biome != Biomes.Keys[(int)b])
            {
                int seed = SeedFor(b); Log($"--- music in {Biomes.Name(b)}: the check jumps to seed {seed}");
                G.JumpTo(seed); yield return WaitPhase(() => G.Phase == "docked" && G.Planet.seed == seed, 40f, "the jump");
            }
            else Log($"--- music in {Biomes.Name(b)} ({G.Planet.name})");
            int n = Mathf.Max(3, G.Planet.pilots.Length);
            G.CheckSetup(n, new int[n], new int[n]);   // the planet's pilots at their sites, no aliens (no canopy visit mid-flight)
            int pl0 = PlayLog.Lines.Count;
            yield return LaunchFromHangar();
            r7Fly = r7OldQuiet = r7NewQuiet = r7Heard = r7Pick = r7PickQuiet = r7Low = r7LowOld = r7LowNew = 0; r7Closest = 1e9f;
            // down low where Dan flies: 40 m over the start pad (the launch drops the ship from dock height), then the trail
            G.Ship.Field.FindStart(out var low, out var hd); G.Ship.ResetTo(low, hd); G.Rig.Snap(); yield return new WaitForSecondsRealtime(.5f);
            yield return SetMode(FlightMode.Flight);
            r7Phase = "fly"; StartCoroutine(SampleMusic());
            yield return WaitFor(() => G.Route.Path.Count > 1, 15f);
            var target = G.RoutePilot;
            if (target != null) yield return FollowRoute(target, 100f);
            yield return Fly(35f);   // on, low, past whatever is there
            Log($"  flying {r7Fly:0} s ({r7Low:0} s of it under 250 m over the ground; the closest pilot passed {r7Closest:0} m): 0.6.3's rule was quiet {r7OldQuiet * 100 / Mathf.Max(1, r7Fly):0} % of the flight ({r7LowOld * 100 / Mathf.Max(1, r7Low):0} % of the low flying), the new rule {r7NewQuiet * 100 / Mathf.Max(1, r7Fly):0} % ({r7LowNew * 100 / Mathf.Max(1, r7Low):0} %); music heard {r7Heard * 100 / Mathf.Max(1, r7Fly):0} % (\"{G.Music.Playing}\")");
            yield return Shot($"r7a_music_{Biomes.Keys[(int)b]}_flying");
            // the pickup: quiet from the landing to the boarding, then back
            var p = target != null && target.OnFoot ? target : G.Pilots.FirstOrDefault(x => x.OnFoot);
            if (p != null)
            {
                r7Phase = "pickup";
                yield return LandFor(p);
                yield return WaitPhase(() => p.AtDoor, 60f, "the door");
                yield return new WaitForSecondsRealtime(1f);
                Log($"  at the pickup (landed {Flat(G.Ship.transform.position - p.Position).magnitude:0} m from pilot {p.Index + 1}): music quiet {r7PickQuiet * 100 / Mathf.Max(.1f, r7Pick):0} % of {r7Pick:0} s (gain now {G.Music.PlayingVolume:0.00}, want \"{G.Music.Want}\")");
                yield return Press(GamepadButton.East); yield return WaitPhase(() => !p.OnFoot, 10f, "boarding"); yield return new WaitForSecondsRealtime(2f);
                r7Phase = ""; yield return BackToFlight(); yield return Fly(8f);
                Log($"  after the pickup, flying again 8 s: music \"{G.Music.Playing}\" gain {G.Music.PlayingVolume:0.00}");
            }
            r7Sample = false; r7Phase = "";
            Log($"  [Play] music lines this sortie: {string.Join(" | ", PlayLog.Lines.Skip(pl0).Where(l => l.Contains(" music ")).Select(l => l.Substring(l.IndexOf("music"))))}");
            yield return HomeByRecall();
        }

        // ---- (2) the creatures up close
        IEnumerator CanopyVisit(AlienType type)
        {
            if (type == AlienType.Sleeper) { Log("  Sleeper on the canopy: never (it boards as a pilot; it doesn't climb)"); yield break; }
            G.CheckSetup(3, new[] { (int)type, 0, 0 }, new[] { 0, 0, 0 });
            var p = G.Pilots.Find(x => x.Index == 0); p.Leaps = true; p.ClimbAfter = 1.5f;
            yield return LandFor(p);
            yield return WaitPhase(() => p.AtDoor, 60f, "the door");
            yield return WaitFor(() => G.HorrorBeat == "on the canopy", 20f);
            if (G.HorrorBeat != "on the canopy") { Log($"  {type} on the canopy: never got there (beat \"{G.HorrorBeat}\")"); yield break; }
            string k = type.ToString().ToLower();
            OldLook(true); G.HorrorCreature.FullDetail(false); yield return new WaitForSecondsRealtime(1.5f);
            Log($"  {type} on the canopy, BEFORE (0.6.3 staging): {Body(G.HorrorCreature, G.Rig.Head.transform, true)}");
            yield return Shot($"r7a_{k}_canopy_before");
            OldLook(false); G.HorrorCreature.FullDetail(true); yield return new WaitForSecondsRealtime(1.5f);
            Log($"  {type} on the canopy, AFTER: {Body(G.HorrorCreature, G.Rig.Head.transform, true)}");
            yield return Shot($"r7a_{k}_canopy_after");
            yield return Press(GamepadButton.South);   // engines: thrown off
            yield return new WaitForSecondsRealtime(2f);
            yield return BackToFlight();
        }

        IEnumerator StrikeVisit(AlienType type, bool old)
        {
            string k = type.ToString().ToLower(), tag = $"r7a_{k}_strike_{(old ? "before" : "after")}";
            OldLook(old);
            G.CheckSetup(3, new[] { (int)type, 0, 0 }, new[] { 0, 0, 0 }); G.Foes.Running = false;
            var p = G.Pilots.Find(x => x.Index == 0);
            yield return WaitFor(() => G.Now == GameShell.State.Flying && !G.Ship.Kinematic, 60f);   // never during the launch (a player can't board anyone then)
            if (G.Ship.AboveGround < 30f) yield return BackToFlight();
            G.CheckStrikeKind = (int)GameShell.StrikeKind.Takeoff; G.StrikeAtReset();
            StartCoroutine(SampleState()); Trace.Start();
            G.CheckBoard(p);
            if (type == AlienType.Sleeper) G.SleeperTurnAt = 2f;
            G.CheckStrikeKind = -1;
            float end = Time.realtimeSinceStartup + 20f;
            while (G.StrikeSeenAt < 0 && Time.realtimeSinceStartup < end && G.Now == GameShell.State.Flying) { Autopilot(); yield return null; }
            state.leftStick = Vector2.zero; Set();
            if (G.StrikeSeenAt < 0) { Log($"  {type} strike ({(old ? "before" : "after")}): NO STRIKE in 20 s"); sampling = false; Trace.On = false; OldLook(false); yield break; }
            float seen = G.StrikeSeenAt;
            foreach (var (dt, n) in new[] { (.1f, "1"), (.5f, "2"), (1.2f, "3") })
            {
                yield return WaitFor(() => Time.time - seen > dt, 3f);
                yield return Shot($"{tag}_{n}_{dt:0.0}s");
                Log($"    {type} {(old ? "BEFORE" : "AFTER")} +{Time.time - seen:0.00} s after in view: {Body(G.HorrorCreature, G.Rig.Head.transform)}");
            }
            sampling = false; Trace.On = false;
            var lines = new List<(float t, string kind, string what)>(Trace.Lines);
            float strike = G.BurstAt;
            var before = lines.Where(l => l.t >= strike && l.t < seen && l.kind != "strike" && !(l.kind == "state" && l.what.StartsWith("creature"))).ToList();
            if (!old)
                Log($"  {type} strike order: in view {seen - strike:0.00} s after the strike; anything the player could notice before it was in view: {(before.Count == 0 ? "NOTHING (as asked)" : string.Join(", ", before.Select(l => $"{l.t - strike:0.00} {l.kind}: {l.what}")))}; " +
                    $"then: {string.Join(" | ", lines.Where(l => l.t >= seen && l.t < seen + 1f).Select(l => $"+{l.t - seen:0.00} {l.kind}: {l.what}"))}");
            yield return WaitFor(() => G.Phase == "dead" || G.Menu.Current?.Title == "SHIP DESTROYED", 10f);
            OldLook(false);
            yield return DeadToHangar(tag);
        }

        IEnumerator CabinCamPictures(AlienType type)
        {
            string k = type.ToString().ToLower(); var cam = G.CabinRoom.Cam;
            foreach (bool old in new[] { true, false })
            {
                OldLook(old);
                var c = G.CheckCabinCreature(type, old); yield return null; yield return null; c.LightFrom(cam.transform.position); yield return null;
                SaveCam(cam, $"r7a_{k}_cabin_cam_{(old ? "before" : "after")}");
                Log($"  {type} in the cabin camera ({(old ? "before" : "after")}): {Body(c, cam.transform)}");
                Destroy(c.gameObject);
            }
            OldLook(false);
        }

        IEnumerator Round7()
        {
            StartCoroutine(Progress());
            yield return WaitFor(() => G.Now == GameShell.State.Title, 60f);
            yield return new WaitForSecondsRealtime(1.5f);
            string only = CheckRun.Only;
            bool music = only == "round7a" || only == "round7music", aliens = only == "round7a" || only == "round7aliens";
            if (only.StartsWith("round7b")) { yield return Round7B(); yield break; }
            if (aliens) CreatureModelReport();
            if (music) yield return MusicLoops(new[] { "flight_canyons", "flight_forest", "flight_city", "flight_ice", "flight_volcanic", "flight_crystal", "flight_ocean" });
            Career.CheckSystemSeed = 7171;
            GameSettings.S.difficulty.crashLevel = 1; GameSettings.S.difficulty.enemies = 0;   // Bounce, no enemies: the music and the creatures alone
            yield return ToCampaignMenu();
            yield return Nav(GamepadButton.DpadDown, 1); yield return Press(GamepadButton.South);   // New career
            yield return WaitPhase(() => G.Now == GameShell.State.Docked && G.Phase == "docked", 60f, "the hangar");
            yield return new WaitForSecondsRealtime(1f);
            Log($"new career: {G.Planet.name} ({G.Planet.biome})");
            if (music) foreach (var b in new[] { Biome.Canyons, Biome.Forest, Biome.City, Biome.Ice }) yield return MusicFlight(b);
            if (aliens)
            {
                GameSettings.S.routeTrail = false;
                Log("--- the creatures up close (before = 0.6.3's staging and look; after = Round 7's)");
                yield return LaunchFromHangar();
                var types = LauncherBridge.Argument("-r7types");   // e.g. -r7types Sleeper,Mimic: just those
                foreach (var type in Five)
                {
                    if (!string.IsNullOrEmpty(types) && !types.Contains(type.ToString())) continue;
                    yield return CanopyVisit(type);
                    yield return StrikeVisit(type, true);
                    yield return StrikeVisit(type, false);
                    yield return CabinCamPictures(type);
                }
                GameSettings.S.routeTrail = true;
            }
            Career.CheckSystemSeed = 0;
            Log($"--- [Play] lines ({PlayLog.Lines.Count}), first 60:"); foreach (var l in PlayLog.Lines.Take(60)) Log("  " + l);
        }
    }
}
