using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

namespace HostileHorizon
{
    // Round 3 check, in the built exe:  HostileHorizon.exe -exeCheck <dir> -exeCheckOnly aliens
    // (B12) The first planet of a new career has no aliens; the same seed gives the same aliens; one pickup per outside type with its
    // tell shown and with it hidden (cockpit and airlock-camera pictures, the numbers behind each tell), each then killed by the
    // engines (reveal, bounty, Field Journal first and second entries); a real pilot killed by the engines (penalty); an ignored alien
    // climbing onto the canopy and breaking in (timing, pictures at the beats, the camera never cut, the player looking round), ending
    // in the regeneration bay; let in through the airlock (the horror sequence from the cabin); thrown off the canopy; the Sleeper:
    // the pilots win with 3 others aboard, cockpit death alone, docked in time = security; the cabin camera; frame rate.
    // Every sound that would have played is listed (the run is muted).
    public sealed partial class ExeCheck
    {
        void SaveRT(RenderTexture rt, string name)
        {
            var prev = RenderTexture.active; RenderTexture.active = rt;
            var t = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false); t.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); t.Apply();
            RenderTexture.active = prev; File.WriteAllBytes(Path.Combine(dir, name + ".png"), t.EncodeToPNG()); Destroy(t);
            Log($"picture {name}.png ({rt.width} x {rt.height}, straight from the camera)");
        }

        string Fates() => string.Join(" ", Array.ConvertAll(G.Planet.pilots, f => ((PilotFate)f).ToString()));
        string AlienList(PlanetState p) => p.alien == null ? "none" : string.Join(", ", Array.ConvertAll(Range(p.alien.Length), i => $"{i + 1}:{(p.alien[i] == 0 ? "pilot" : Aliens.Names[p.alien[i]])}{(p.quirk[i] != 0 ? $"(tell {Aliens.Names[p.quirk[i]]})" : "")}"));
        static int[] Range(int n) { var a = new int[n]; for (int i = 0; i < n; i++) a[i] = i; return a; }

        // Hover 40 m over a pilot's pad and set down there, engines off: the pilot comes (as a player lands on the green ring).
        IEnumerator LandFor(DownedPilot p)
        {
            G.Ship.Energy = 100;   // the check flies many sorties in one: keep it from running dry
            var at = p.LandingSpot + Vector3.up * 40f; var to = Flat(p.Position - p.LandingSpot);
            G.Ship.ResetTo(at, to.sqrMagnitude > .1f ? Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg - 60f : 0); G.Rig.Snap();
            yield return new WaitForSecondsRealtime(.5f);
            yield return HoverLand(p.LandingSpot);
            yield return new WaitForSecondsRealtime(.6f);
            if (G.Ship.EnginesOn) yield return Press(GamepadButton.South);   // engines off
        }

        IEnumerator BackToFlight()
        {
            if (G.Now != GameShell.State.Flying) yield break;
            if (!G.Ship.EnginesOn) yield return Press(GamepadButton.South);
            state.rightTrigger = 1; Set(); yield return WaitFor(() => G.Ship.AboveGround > 30f, 10f); state.rightTrigger = 0; Set();
        }

        // One pickup with a chosen visitor: watch it come (the tell's numbers), pictures at the door, then the engines.
        IEnumerator Visitor(AlienType type, bool tell, string tag)
        {
            G.CheckSetup(3, new[] { (int)type, 0, 0 }, new[] { tell ? (int)type : 0, 0, 0 });
            var p = G.Pilots.Find(x => x.Index == 0);
            Log($"--- {tag}: pilot 1 is {(type == AlienType.None ? "a real pilot" : $"a {type}")}, tell {(tell ? "SHOWN" : "hidden")}; knock pattern \"{p.KnockPattern}\"");
            yield return LandFor(p);
            var knocks = new List<string>(); float k0 = -1;
            void OnK(DownedPilot d, int n) { if (k0 < 0) k0 = Time.time; var src = d.LastKnock; knocks.Add($"{n}@{Time.time - k0:0.00}s{(src ? $" from ship z {G.Ship.transform.InverseTransformPoint(src.transform.position).z:+0.0;-0.0}" : "")}"); }
            p.OnKnock += OnK;
            float end = Time.realtimeSinceStartup + 60f, maxLag = 0, hidden = 0; bool shotRun = false; Vector3 enter = Vector3.zero; bool wasRunning = false;
            while (Time.realtimeSinceStartup < end && !p.AtDoor && G.Now == GameShell.State.Flying)
            {
                if (p.Now == DownedPilot.State.Running)
                {
                    wasRunning = true;
                    if (p.WrongShadow) maxLag = Mathf.Max(maxLag, Vector3.Distance(p.WrongShadow.position, p.Position));
                    if (p.HiddenFromRadar) hidden += Time.deltaTime;
                    enter = G.Ship.transform.InverseTransformPoint(p.Position);
                    if (!shotRun && Flat(p.Position - G.Ship.transform.position).magnitude < 16f) { shotRun = true; yield return Shot($"{tag}_coming_cockpit"); G.Rig.Current = CameraRig.Mode.Landing; G.Rig.Snap(); yield return new WaitForSecondsRealtime(.4f); yield return Shot($"{tag}_coming_landing_view"); G.Rig.Current = CameraRig.Mode.Cockpit; G.Rig.Snap(); }
                }
                Set(); yield return null;
            }
            Log($"  ran: {(wasRunning ? "yes" : "NO")}; at the door {p.AtDoor}; {(p.WrongShadow ? $"its shadow body lagged up to {maxLag:0.0} m behind it (shadow-only renderers; the figure casts none)" : "shadow: the figure's own")}; " +
                $"{(type == AlienType.Stalker || tell ? $"circled {p.Circled:0}° round the ship, off the radar {hidden:0.0} s, last seen at ship x {enter.x:+0;-0} z {enter.z:+0;-0} (the waiting spot is x -2.45 z +0.8)" : "")}");
            if (p.AtDoor)
            {
                yield return new WaitForSecondsRealtime(4.5f);   // a set of knocks, then it looks up into the camera
                Log($"  knocks: {string.Join(", ", knocks)}; display \"{G.Displays.KnockText}\"");
                Log($"  airlock camera on {G.Belly.On} ({G.Belly.Title}); facing the camera: {Vector3.Angle(p.Facing * Vector3.forward, Flat(G.Ship.transform.TransformPoint(BellyCam.AirlockCamAt) - p.Position)):0}° off");
                yield return Shot($"{tag}_at_door_cockpit");
                SaveRT(G.Belly.Feed, $"{tag}_airlock_cam");
            }
            p.OnKnock -= OnK;
            // the engines: kill it (an alien: the reveal, the bounty, the journal; a real pilot: the penalty)
            int cr = Career.C.credits; int jn = Career.C.journal.Count;
            yield return Press(GamepadButton.South);
            yield return new WaitForSecondsRealtime(.8f);
            G.Rig.Current = CameraRig.Mode.Landing; G.Rig.Snap(); yield return new WaitForSecondsRealtime(.5f); yield return Shot($"{tag}_engines_reveal_landing_view"); G.Rig.Current = CameraRig.Mode.Cockpit;
            yield return new WaitForSecondsRealtime(1.5f);
            var e = type == AlienType.None ? null : Career.C.journal.Find(j => j.type == (int)type);
            Log($"  engines on: pilot {p.Now}, fate {(PilotFate)G.Planet.pilots[0]}; credits {cr} -> {Career.C.credits} ({Career.C.credits - cr:+0;-0}); message \"{G.Displays.Prompt}\"; " +
                (e != null ? $"journal {jn} -> {Career.C.journal.Count} entries, {type} met {e.encounters}: \"{Aliens.Hint(type, e.encounters)}\"" : "no journal change"));
            yield return BackToFlight();
        }

        IEnumerator DeadToHangar(string tag)
        {
            yield return WaitPhase(() => G.Menu.Current != null && G.Menu.Current.Title == "SHIP DESTROYED", 20f, "the death page");
            Log($"  death page: \"{G.Menu.Current?.Body?.Invoke().Replace("\n", " / ")}\"");
            yield return Shot($"{tag}_death_page");
            yield return Press(GamepadButton.South);   // Wake up
            yield return WaitPhase(() => G.Phase == "regen", 60f, "the regeneration bay");
            Log($"  woke in the regeneration bay: phase {G.Phase}, page \"{G.Menu.Current?.Title}\"");
            yield return Shot($"{tag}_regen_bay");
            G.Menu.Current.Selected = 1; yield return Press(GamepadButton.South);   // the loaner (free)
            yield return WaitPhase(() => G.Now == GameShell.State.Docked && G.Phase == "docked", 90f, "the hangar");
            G.Menu.Current.Selected = 0; yield return Press(GamepadButton.South);   // Launch
            if (G.Menu.Current != null && G.Menu.Current.Title == "LOW ENERGY") yield return Press(GamepadButton.South);
            yield return WaitPhase(() => G.Now == GameShell.State.Flying, 40f, "flight after launch");
        }

        // The horror sequence: a picture at each beat, the camera's travel (no cut), and the player looking round during it.
        string lastWhere = "";
        // Fix 3.1: where the horror creature is: its face from the eye, its root in ship space, drawn or not
        string CreatureWhere()
        {
            var c = G.HorrorCreature; if (!c) return "no creature";
            var eye = G.Rig.Head.transform; var face = c.HeadWorld;
            bool vis = false; Bounds bb = default; bool first = true;
            if (c.Rig) foreach (var r in c.Rig.Renderers) { vis |= r.isVisible; if (first) { bb = r.bounds; first = false; } else bb.Encapsulate(r.bounds); }
            return $"creature {c.name}: active {c.gameObject.activeInHierarchy}, face {Vector3.Distance(eye.position, face):0.00} m from the eye ({eye.InverseTransformPoint(face).ToString("0.00")} eye space), root at ship {G.Ship.transform.InverseTransformPoint(c.transform.position).ToString("0.00")}, " +
                (c.Rig ? $"clip {c.Rig.Current}, bounds {bb.size.ToString("0.0")} at {bb.center.ToString("0.0")} (eye {eye.position.ToString("0.0")}), lod-near {G.Rig.Head.nearClipPlane:0.00}" : "code-made");
        }

        IEnumerator WatchHorror(string tag)
        {
            StartCoroutine(WatchCamera());
            var seen = new HashSet<string>(); float t0 = Time.time, lookYaw0 = 0; bool looked = false;
            while (G.Phase == "horror" || G.Phase == "" && G.Now == GameShell.State.Auto)
            {
                string b = G.HorrorBeat;
                if (b != "" && seen.Add(b))
                {
                    Log($"  beat {Time.time - t0:0.0} s: {b}; cabin lights {(G.CabinRoom.LightsOn ? "on" : "off")}, displays dark {G.Displays.Blackout}, input {(G.Ship.InputEnabled ? "on" : "off")}, view {G.Rig.Current}");
                    Log("    " + CreatureWhere());
                    if (b == "moving in the cabin" || b == "the hatch is hit")
                    {
                        // look back over the left shoulder at the hatch (the right stick, as a player would)
                        var head = G.Rig.Head.transform; lookYaw0 = head.eulerAngles.y;
                        yield return LookAt(G.Ship.transform.TransformPoint(Cabin.HatchCockpitSide + new Vector3(0, .4f, -.4f)), 1.0f);
                        looked = true; Log($"  looked round: the view turned {Mathf.Abs(Mathf.DeltaAngle(lookYaw0, G.Rig.Head.transform.eulerAngles.y)):0}° (look works during it)");
                        yield return Shot($"{tag}_{seen.Count:00}_{b.Replace(' ', '_')}_looking_back");
                        SaveRT(G.CabinRoom.Feed, $"{tag}_{seen.Count:00}_cabin_cam");
                        if (b == "the hatch is hit") { state.rightStick = Vector2.zero; Set(); }
                    }
                    else yield return Shot($"{tag}_{seen.Count:00}_{b.Replace(' ', '_')}");
                }
                if (b == "in front of you" && looked) { state.rightStick = Vector2.zero; Set(); }
                if (G.HorrorCreature) lastWhere = CreatureWhere();
                yield return null;
            }
            state.rightStick = Vector2.zero; Set();
            Log("    at the end: " + lastWhere);
            Log($"  horror over after {Time.time - t0:0.0} s: state {G.Now}, phase {G.Phase}, killed by {G.KilledBy}; {Continuity()}");
        }

        IEnumerator AlienCheck()
        {
            StartCoroutine(Progress());
            yield return WaitFor(() => G.Now == GameShell.State.Title, 60f);
            yield return new WaitForSecondsRealtime(1.5f);
            var tu = Tuning.T; bool f31 = CheckRun.Only == "fix31", quick = CheckRun.Only == "fix31h";
            if (f31) ModelReport();
            Log($"tuning: share {tu.alienShareStart} + {tu.alienSharePerTier}/tier (max {tu.alienShareMax}), tells {tu.tellReliability}, false tells {tu.falseTellChance}, bounty {tu.alienBounty}, canopy after {tu.canopyClimbAfter} s, break-in {tu.breakInAfter} s, Sleeper {tu.sleeperTurnMin}-{tu.sleeperTurnMax} s, pilots win with {tu.sleeperPilotsWin}");
            // ---- the same seed, the same aliens (a planet as a second planet would get it)
            foreach (int seed in new[] { 20261104, 20261105, 777 })
            {
                var a = new PlanetState { seed = seed, tier = 1, pilots = new int[6] }; Aliens.Assign(a, false);
                var b = new PlanetState { seed = seed, tier = 1, pilots = new int[6] }; Aliens.Assign(b, false);
                Log($"aliens for seed {seed} (tier 1, 6 pilots): {AlienList(a)}; again: {(AlienList(a) == AlienList(b) ? "identical" : "DIFFERENT")}");
            }
            int shown = 0, total = 0; var types = new int[6];
            for (int seed = 1; seed <= 400; seed++) { var a = new PlanetState { seed = seed * 7919, tier = 1, pilots = new int[6] }; Aliens.Assign(a, false); for (int i = 0; i < 6; i++) if (a.alien[i] != 0) { total++; types[a.alien[i]]++; if (a.quirk[i] != 0) shown++; } }
            Log($"400 planets (6 pilots, tier 1): {total} aliens ({total / 24f:0.0} %), tells shown {shown * 100f / Mathf.Max(1, total):0} % (target 75), types Mimic {types[1]} Hollow {types[2]} Knocker {types[3]} Stalker {types[4]} Sleeper {types[5]}");

            // ---- a new career: its first planet has no aliens
            yield return new WaitForSecondsRealtime(1.5f);
            for (int k = 0; k < 3 && G.Menu.Current?.Title != "CAMPAIGN"; k++) { G.Menu.Current.Selected = 0; yield return Nav(GamepadButton.DpadDown, 1); yield return Press(GamepadButton.South); yield return new WaitForSecondsRealtime(.5f); }   // Campaign
            Log($"menu: {G.Menu.Current?.Title}");
            yield return Nav(GamepadButton.DpadDown, 1); yield return Press(GamepadButton.South);   // New career
            yield return WaitPhase(() => G.Now == GameShell.State.Docked, 60f, "the hangar");
            Log($"new career, first planet {G.Planet.name}: aliens {AlienList(G.Planet)}");
            if (CheckRun.Only == "fix31perf")
            {   // the Field Journal with all five met (the check's own save): the pictures' layout
                for (int t = 1; t <= 5; t++) { Career.C.Meet((AlienType)t); Career.C.Meet((AlienType)t); }
                G.Menu.Current.Selected = Row("Field Journal"); yield return Press(GamepadButton.South);
                yield return new WaitForSecondsRealtime(.5f); yield return Shot("j2_field_journal_all_five"); yield return Press(GamepadButton.East);
                yield return new WaitForSecondsRealtime(.5f);
            }
            G.Menu.Current.Selected = 0; yield return Press(GamepadButton.South);
            yield return WaitPhase(() => G.Now == GameShell.State.Flying, 40f, "flight after launch");
            GameSettings.S.routeTrail = false;   // not what this check looks at
            if (CheckRun.Only == "fix31perf")
            {   // Fix 3.1: frame rates only (with -steamDeck 1 for the Deck setting)
                yield return LandedFps("with the models");
                CharacterRig.Disabled = true; yield return LandedFps("code-made figures (pilots outside; the cabin keeps its models)"); CharacterRig.Disabled = false;
                G.CheckSetup(3, new[] { 0, 0, 0 }, new[] { 0, 0, 0 }); G.CheckBoard(G.Pilots.Find(x => x.Index == 1)); G.CheckBoard(G.Pilots.Find(x => x.Index == 2));
                yield return ResetVia("for the fps run", 1); yield return SetMode(FlightMode.Flight);
                yield return MeasureFps($"flying low, 2 aboard (the cabin camera drawing){(SteamDeck.On ? ", Steam Deck setting" : "")}");
                yield break;
            }

            if (!quick) {
            // ---- B3 / B4: the four outside types, tell shown and hidden, each killed by the engines; then a real pilot
            foreach (var t in new[] { AlienType.Mimic, AlienType.Hollow, AlienType.Knocker, AlienType.Stalker })
            {
                yield return Visitor(t, true, $"t{(int)t}_{t.ToString().ToLower()}_tell");
                if (!f31) yield return Visitor(t, false, $"t{(int)t}_{t.ToString().ToLower()}_hidden");
            }
            yield return Visitor(AlienType.None, false, "t0_real_pilot");

            // ---- B5: the canopy, then engines on: thrown off
            {
                G.CheckSetup(3, new[] { (int)AlienType.Knocker, 0, 0 }, new[] { (int)AlienType.Knocker, 0, 0 });
                var p = G.Pilots.Find(x => x.Index == 0);
                Log("--- canopy, then engines: a Knocker left at the airlock");
                yield return LandFor(p);
                yield return WaitPhase(() => p.AtDoor, 60f, "the door");
                float atDoor = Time.time;
                yield return WaitPhase(() => G.CanopyAlien != null && G.HorrorBeat == "on the canopy", tu.canopyClimbAfter + 15f, "the canopy");
                Log($"  left at the door: it climbed onto the canopy {Time.time - atDoor:0.0} s after reaching the door (tuning {tu.canopyClimbAfter} s + the climb)");
                yield return new WaitForSecondsRealtime(2f);
                yield return Shot("c1_on_the_canopy_cockpit");
                int cr = Career.C.credits;
                yield return Press(GamepadButton.South);   // engines
                yield return new WaitForSecondsRealtime(2.5f);
                Log($"  engines on with it on the canopy: canopy alien {(G.CanopyAlien == null ? "gone" : "STILL THERE")}, fate {(PilotFate)G.Planet.pilots[0]}, credits {Career.C.credits - cr:+0;-0}; \"{G.Displays.Prompt}\"");
                G.Rig.Current = CameraRig.Mode.Landing; G.Rig.Snap(); yield return new WaitForSecondsRealtime(.8f); yield return Shot("c2_thrown_off_landing_view"); G.Rig.Current = CameraRig.Mode.Cockpit;
                yield return BackToFlight();
            }

            // ---- B5 + B7: ignored on the canopy, it breaks in
            {
                G.CheckSetup(3, new[] { (int)AlienType.Hollow, 0, 0 }, new[] { (int)AlienType.Hollow, 0, 0 });
                var p = G.Pilots.Find(x => x.Index == 0);
                Log("--- canopy break-in: a Hollow left at the airlock, then left on the canopy");
                yield return LandFor(p);
                yield return WaitPhase(() => p.AtDoor, 60f, "the door");
                yield return WaitPhase(() => G.HorrorBeat == "on the canopy", tu.canopyClimbAfter + 15f, "the canopy");
                float on = Time.time;
                yield return new WaitForSecondsRealtime(3f); yield return Shot("b1_canopy_pressed_to_the_glass");
                yield return LookAt(G.Ship.transform.TransformPoint(new Vector3(0, 1.3f, 4.35f)), 1.2f); yield return Shot("b2_canopy_looking_up"); state.rightStick = Vector2.zero; Set();
                yield return WaitPhase(() => G.Phase == "horror", tu.breakInAfter + 10f, "the break-in");
                Log($"  broke in {Time.time - on:0.0} s after pressing on the glass (tuning {tu.breakInAfter} s)");
                yield return WatchHorror("b");
                yield return DeadToHangar("b");
            }

            }
            // ---- B7: let in through the airlock
            {
                G.CheckSetup(3, new[] { (int)AlienType.Mimic, 0, 0 }, new[] { (int)AlienType.Mimic, 0, 0 });
                var p = G.Pilots.Find(x => x.Index == 0);
                Log("--- let in: a Mimic, the airlock opened (B)");
                yield return LandFor(p);
                yield return WaitPhase(() => p.AtDoor, 60f, "the door");
                yield return new WaitForSecondsRealtime(1f);
                yield return Press(GamepadButton.East);   // the airlock
                yield return WaitPhase(() => G.Phase == "horror", 10f, "the horror sequence");
                yield return WatchHorror("h");
                yield return DeadToHangar("h");
            }

            if (quick) yield break;
            // ---- B8 / B9: the Sleeper
            // (1) three other pilots aboard: it kills one, the other two kill it
            {
                G.CheckSetup(6, new[] { (int)AlienType.Sleeper, 0, 0, 0, 0, 0 }, new[] { (int)AlienType.Sleeper, 0, 0, 0, 0, 0 });
                var s = G.Pilots.Find(x => x.Index == 0);
                Log("--- Sleeper with 3 others aboard (tell shown)");
                foreach (var q in G.Pilots.FindAll(x => x.Index >= 1 && x.Index <= 3)) G.CheckBoard(q);
                yield return LandFor(s);
                yield return WaitPhase(() => s.AtDoor, 60f, "the door");
                SaveRT(G.Belly.Feed, "s1_sleeper_at_door_airlock_cam");
                yield return Press(GamepadButton.East); yield return WaitPhase(() => s.Now == DownedPilot.State.Aboard, 10f, "boarding");
                Log($"  aboard: cabin {G.CabinCount}; the Sleeper turns after {G.SleeperTurnAt:0} s of flight (seeded; shortened to 12 s for the check); message \"{G.Displays.Prompt}\"");
                G.SleeperTurnAt = 12f;
                yield return BackToFlight();
                yield return Press(GamepadButton.DpadUp);   // the cabin camera on the HUD
                yield return new WaitForSecondsRealtime(3f);
                yield return Shot("s1_cabin_cam_tell_cockpit"); SaveRT(G.CabinRoom.Feed, "s1_cabin_cam_tell");
                Log($"  cabin camera: HUD picture-in-picture {G.Displays.CabinPip}, feed on {G.CabinRoom.FeedOn}");
                state.rightTrigger = .4f; Set();
                yield return WaitPhase(() => G.HorrorBeat == "cabin: a pilot is killed", 30f, "the turn");
                state.rightTrigger = 0; Set();
                yield return new WaitForSecondsRealtime(.8f); SaveRT(G.CabinRoom.Feed, "s1_cabin_cam_the_kill"); yield return Shot("s1_the_kill_cockpit");
                yield return WaitFor(() => G.SleeperIndex < 0 || G.Phase == "horror", 15f);
                Log($"  after: cabin {G.CabinCount}, fates {Fates()}, state {G.Now}/{G.Phase}; message \"{G.Displays.Prompt}\"");
                SaveRT(G.CabinRoom.Feed, "s1_cabin_cam_after");
                yield return Press(GamepadButton.DpadUp);
            }
            // (2) alone: it goes straight for the cockpit
            {
                G.CheckSetup(3, new[] { (int)AlienType.Sleeper, 0, 0 }, new[] { 0, 0, 0 });
                var s = G.Pilots.Find(x => x.Index == 0);
                Log("--- Sleeper alone (tell hidden)");
                yield return LandFor(s);
                yield return WaitPhase(() => s.AtDoor, 60f, "the door");
                yield return Press(GamepadButton.East); yield return WaitPhase(() => s.Now == DownedPilot.State.Aboard, 10f, "boarding");
                G.SleeperTurnAt = 8f;
                yield return BackToFlight();
                yield return WaitPhase(() => G.Phase == "horror", 30f, "the turn");
                yield return WatchHorror("s2");
                yield return DeadToHangar("s2");
            }
            // (3) docked in time: security in the bay
            {
                G.CheckSetup(3, new[] { (int)AlienType.Sleeper, 0, 0 }, new[] { (int)AlienType.Sleeper, 0, 0 });
                var s = G.Pilots.Find(x => x.Index == 0); G.CheckBoard(G.Pilots.Find(x => x.Index == 1));
                Log("--- Sleeper docked in time (1 other aboard)");
                yield return LandFor(s);
                yield return WaitPhase(() => s.AtDoor, 60f, "the door");
                yield return Press(GamepadButton.East); yield return WaitPhase(() => s.Now == DownedPilot.State.Aboard, 10f, "boarding");
                G.SleeperTurnAt = 9999f;
                yield return BackToFlight();
                yield return SetMode(FlightMode.Hover);
                float upEnd = Time.realtimeSinceStartup + 150f;
                while (Time.realtimeSinceStartup < upEnd && G.Now == GameShell.State.Flying) { state.rightTrigger = 1; Set(); yield return null; }
                state.rightTrigger = 0; Set();
                yield return WaitPhase(() => G.HorrorBeat == "security in the bay", 120f, "security");
                yield return Shot("s3_security_in_the_bay");
                yield return WaitPhase(() => G.Now == GameShell.State.Docked && G.Phase == "docked", 60f, "the hangar");
                Log($"  docked: fates {Fates()}; debrief \"{G.Debrief.Replace("\n", " / ")}\"");
                yield return Shot("s3_hangar_debrief");
                G.Menu.Current.Selected = Row("Field Journal"); yield return Press(GamepadButton.South);   // Field Journal
                Log($"  Field Journal: \"{G.Menu.Current?.Body?.Invoke().Replace("\n", " / ")}\"");
                yield return Shot("j1_field_journal");
                yield return Press(GamepadButton.East);
            }
            Log($"career: {Career.C.stats.aliensKilled} aliens killed, {Career.C.stats.deaths} deaths, journal {Career.C.journal.Count} entries");

            // ---- frame rate: flying low with pilots aboard (the cabin camera drawing)
            G.Menu.Current.Selected = 0; yield return Press(GamepadButton.South);
            if (G.Menu.Current != null && G.Menu.Current.Title == "LOW ENERGY") yield return Press(GamepadButton.South);
            yield return WaitPhase(() => G.Now == GameShell.State.Flying, 40f, "flight after launch");
            G.CheckSetup(3, new[] { 0, 0, 0 }, new[] { 0, 0, 0 }); G.CheckBoard(G.Pilots.Find(x => x.Index == 1)); G.CheckBoard(G.Pilots.Find(x => x.Index == 2));
            if (f31)
            {
                yield return CloseUps();
                yield return LandedFps("with the models");
                CharacterRig.Disabled = true; yield return LandedFps("code-made figures (pilots outside; the cabin keeps its models)"); CharacterRig.Disabled = false;
                G.CheckSetup(3, new[] { 0, 0, 0 }, new[] { 0, 0, 0 }); G.CheckBoard(G.Pilots.Find(x => x.Index == 1)); G.CheckBoard(G.Pilots.Find(x => x.Index == 2));
            }
            yield return ResetVia("for the fps run", 1); yield return SetMode(FlightMode.Flight);
            yield return MeasureFps($"flying low, 2 aboard (the cabin camera drawing){(SteamDeck.On ? ", Steam Deck setting" : "")}");

            Log("sounds that would have played (muted run): " + string.Join(" | ", Sfx.Played.FindAll(x => !x.Contains("knock") && !x.Contains("beep"))));
            Log("alien events: " + string.Join(" | ", G.AlienEvents));
        }
    }
}
