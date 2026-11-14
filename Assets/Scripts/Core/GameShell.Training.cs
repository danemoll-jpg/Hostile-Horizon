using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HostileHorizon
{
    // Round 11 (C1-C2): Training. Title > Training (and offered once when a new career starts): eight short lessons on the practice
    // planet (Canyons, seed 1984), coached by Control, run by the Campaign's own systems on a throwaway career kept in memory (never
    // saved: Career.BeginTraining). No credits lost, Bounce crash level, unlimited energy (but for "Getting home"), enemy hits do no
    // damage, an alien let in or left on the canopy never gets in (no full scare in training). Each lesson is steps that complete when
    // you do the thing (never a timer); the step shows on the cockpit's displays and the HUD with the button of the device in use; a
    // hint (and Control) after 30 s stuck; the pause menu skips a step, restarts or leaves. Progress: Training.P (training.json).
    public sealed partial class GameShell
    {
        sealed class TStep { public System.Func<string> Text; public System.Func<bool> Done; public string Hint; public System.Action Start; }
        List<TStep> tSteps; int tStep; float tStepT; bool tHinted, tDocked; int tRoutePresses;
        float tSeenPitchUp, tSeenPitchDown, tSeenRollL, tSeenRollR, tSeenYawL, tSeenYawR; bool tThrottleFull, tRightingSeen, tLevelSeen, tRecentred;
        float tNearest0; Vector3 tStart; int tFilter0;
        MenuPanel.Page trainingPage, trainingPause, trainingDone, trainingOffer;
        public MenuPanel.Page TrainingPage => trainingPage;           // checks
        public MenuPanel.Page TrainingDonePage => trainingDone;
        public int TrainingStep => tStep;
        public int TrainingSteps => tSteps?.Count ?? 0;
        public string TrainingStepText => tSteps != null && tStep < tSteps.Count ? tSteps[tStep].Text() : "";
        public readonly List<string> TrainingLog = new List<string>();   // checks: steps done / skipped / hints

        static string B(Controls.Act a) => Controls.Btn(a);

        void BuildTrainingPages()
        {
            trainingPage = new MenuPanel.Page { Title = "TRAINING", Subtitle = "Short lessons on a practice planet, coached by Control. Nothing counts toward your career.", Back = () => Menu.Show(title),
                Body = () => $"{Training.About[Mathf.Clamp(trainingPage.Selected, 0, Training.Count - 1)]}\n\n<size=22>Done: {Training.DoneCount} of {(VrMode.Active ? 8 : 7)}. A lesson ends when you've done each step; pause to skip a step or leave.</size>" };
            for (int i = 0; i < Training.Count; i++)
            {
                int k = i;
                trainingPage.Add(new MenuPanel.Row { Label = () => $"{k + 1}. {Training.Names[k]}", Value = () => !Training.Available(k) ? "VR only" : Training.P.done[k] ? "done ✓" : "",
                    Submit = () => StartTraining(k), Disabled = () => !Training.Available(k) });
            }
            trainingPage.Add(Action("Back", () => Menu.Show(title)));

            trainingPause = new MenuPanel.Page { Title = "PAUSED · TRAINING", Back = Resume, Body = () => $"Lesson {Training.Lesson + 1}: {Training.Names[Training.Lesson]}\nStep {tStep + 1} of {TrainingSteps}: {TrainingStepText}" };
            trainingPause.Add(Action("Resume", Resume))
                         .Add(Action("Skip this step", () => { TrainingLog.Add($"skipped step {tStep + 1}"); Resume(); NextTrainingStep(true); }))
                         .Add(Action("Restart the lesson", () => StartTraining(Training.Lesson)))
                         .Add(Action("Lessons", () => LeaveTraining(true)))
                         .Add(Action("Settings", () => { settingsReturn = trainingPause; Menu.Show(settings); }))
                         .Add(Action("Leave training", () => LeaveTraining(false)));

            trainingDone = new MenuPanel.Page { Title = "LESSON COMPLETE", Back = () => LeaveTraining(true) };
            trainingDone.Body = () => $"<b>{Training.Names[Training.Lesson]}</b>: done.\n\n{(Training.NextLesson(Training.Lesson) >= 0 ? "Next: " + Training.Names[Training.NextLesson(Training.Lesson)] : "That was the last lesson. You're ready for the Campaign.")}";
            trainingDone.Add(new MenuPanel.Row { Label = () => "Next lesson", Submit = () => StartTraining(Training.NextLesson(Training.Lesson)), Visible = () => Training.NextLesson(Training.Lesson) >= 0 })
                        .Add(Action("Lessons", () => LeaveTraining(true)))
                        .Add(Action("Back to title", () => LeaveTraining(false)));

            // offered once, when a new career starts (C1)
            trainingOffer = new MenuPanel.Page { Title = "TRAINING FIRST?", Back = () => Menu.Show(campaignPage),
                Body = () => "New to the rescue corps? Training is eight short lessons on a practice planet, coached by Control: flying, landing, the pickup, aliens, combat and getting home.\nNothing in training counts toward your career. It's always on the title screen too." };
            trainingOffer.Add(Action("Start training", () => { Training.P.offered = true; Training.Save(); StartTraining(0); }))
                         .Add(Action("Not now: start my career", () => { Training.P.offered = true; Training.Save(); StartCampaign(true); }));
        }

        // the Campaign page's "New career": the offer first, once
        void NewCareerOrOffer() { if (!Training.P.offered) Menu.Show(trainingOffer); else StartCampaign(true); }

        public void StartTraining(int lesson) { if (lesson < 0) return; Run(TrainingRun(lesson)); }

        IEnumerator TrainingRun(int lesson)
        {
            Now = State.Loading; Time.timeScale = 1; Menu.SetBackdrop(.6f); Menu.Show(loading);
            EndTrialMode(); LeaveCampaign(); ClearCrash();
            Career.BeginTraining(); var c = Career.C;
            Training.Active = true; Training.Lesson = lesson; tDocked = false;
            TrainingLog.Add($"lesson {lesson + 1} ({Training.Names[lesson]}) started");
            PlayLog.Add($"training: lesson {lesson + 1} {Training.Names[lesson]}");
            // the practice planet: three real pilots (Navigation, Landing, The pickup), two practice aliens with their tells shown (Aliens)
            Planet = c.Planet(Training.PracticeSeed);
            int n = Planet.pilots.Length;
            for (int i = 0; i < n; i++) { Planet.pilots[i] = (int)PilotFate.Waiting; Planet.alien[i] = 0; Planet.quirk[i] = 0; }
            bool pilots = lesson >= 1 && lesson <= 4;
            if (!pilots) for (int i = 0; i < n; i++) Planet.pilots[i] = (int)PilotFate.Lost;
            if (lesson == 4) { Planet.alien[0] = Planet.quirk[0] = (int)AlienType.Knocker; if (n > 1) { Planet.alien[1] = Planet.quirk[1] = (int)AlienType.Mimic; } for (int i = 2; i < n; i++) Planet.pilots[i] = (int)PilotFate.Lost; }
            SetWorld(Training.PracticeSeed, Biome.Canyons);
            Terrain.Field.FindStart(out var start, out var heading); tStart = start;
            Mother.gameObject.SetActive(true); Mother.Place(start, heading); Mother.Holding = false; Mother.SetDoors(0, true);
            SpawnPilots();
            foreach (var p in Pilots) if (p.IsAlien) { p.Leaps = false; p.ClimbAfter = p.Index == 1 ? 6f : 60f; }   // the second (canopy) alien climbs soon after it knocks; the first waits
            Career.EarnPlanet = Planet;
            Ship.Field = Terrain.Field; Rig.Field = Terrain.Field;
            Foes.Clear();
            var f = Terrain.Field;
            // where the lesson starts
            Vector3 at = start; float hd = heading; bool hover = false;
            if ((lesson == 2 || lesson == 3 || lesson == 4) && sitePads.Count > 0)
            {
                var pad = sitePads[0];
                // in flight toward the pad on the lowest line, 60 m over the highest ground on it: 1.4 km out for Landing, 400 m for the
                // pickup and the aliens (brake, hover, land: as a player arrives)
                float dist = lesson == 2 ? 1400f : 400f;
                float best = 1e9f; Vector3 dir = Vector3.forward;
                for (int k = 0; k < 16; k++) { var d = Quaternion.Euler(0, k * 22.5f, 0) * Vector3.forward; float hi = 0; for (int m = 1; m <= dist / 100f; m++) { var q = pad + d * m * 100f; hi = Mathf.Max(hi, f.SurfaceHeight(q.x, q.z)); } if (hi < best) { best = hi; dir = d; } }
                at = pad + dir * dist; at.y = Mathf.Max(best, f.SurfaceHeight(at.x, at.z)) + 60f; hd = Quaternion.LookRotation(-dir).eulerAngles.y;
            }
            if (lesson == 6 && sitePads.Count > 0)
            {
                // 2 km from the mothership, hovering over a pad
                var pad = sitePads.OrderBy(p => Mathf.Abs(Flat(p - start).magnitude - 2000f)).First();
                at = pad + Vector3.up * 80f; hd = Quaternion.LookRotation(Flat(pad - start).normalized).eulerAngles.y;   // in flight, heading away from home
            }
            Terrain.SetFocus(at);
            float end = Time.realtimeSinceStartup + 12f;
            while (!Terrain.ReadyNear(1200f) && Time.realtimeSinceStartup < end) yield return null;
            if (hover) Ship.ResetTo(at, hd); else Ship.Spawn(at, Quaternion.Euler(0, hd, 0), CrashLevel.Bounce, .3f, 0);
            Ship.Kinematic = false; Ship.InputEnabled = true;
            Ship.Level = CrashLevel.Bounce; Ship.ArmourFactor = 0; Ship.SpeedFactor = Ship.DrainFactor = 1;
            Ship.UsesEnergy = lesson == 6; Ship.Energy = 100;
            if (lesson == 6) Ship.Energy = Mathf.Min(60f, EnergyHome + Tuning.T.returnNowWarning * .5f);   // RETURN NOW from the start
            if (lesson == 5)
            {
                // a practice turret on the high ground 700-1100 m ahead, a saucer between
                var fwd = Quaternion.Euler(0, hd, 0) * Vector3.forward; Vector3 tAt = start + fwd * 900f; float bestT = -1;
                for (int k = 0; k < 40; k++)
                {
                    var d = Quaternion.Euler(0, hd + (k % 8 - 3.5f) * 15f, 0) * Vector3.forward; float r = 700f + (k / 8) * 100f; var q = start + d * r;
                    float tt = f.CanyonT(q.x, q.z); if (tt > bestT) { bestT = tt; tAt = q; }
                }
                tAt.y = f.Height(tAt.x, tAt.z);
                var sAt = start + fwd * 600f; sAt.y = f.SurfaceHeight(sAt.x, sAt.z) + 140f;
                Foes.TrainingTargets(f, Ship, tAt, sAt);
            }
            InCampaign = true; Displays.Campaign = true; Displays.CabinSize = CabinSize; cabin.Clear(); Reseat();
            ResetFight(); calling = 0; recallHold = 0; HomeState = 0; dockArmed = false; Phase = ""; debrief = "";
            Rig.Current = CameraRig.Mode.Cockpit; Rig.Watch = null; Rig.Scripted = false; Rig.FollowShip = true; Rig.Snap();
            Menu.Hide(); Displays.SetVisible(true); Now = State.Flying; flightStartedAt = Time.time;
            if (Training.DoneCount == 0 && lesson == 0) Voice.Say("ctl_tr_welcome", VoiceKind.Radio, 3, maxAge: 20f);
            Voice.Say(Training.VoiceIds[lesson], VoiceKind.Radio, 3, maxAge: 30f);
            tSteps = LessonSteps(lesson); tStep = -1; NextTrainingStep(false);
        }

        void NextTrainingStep(bool skipped)
        {
            if (tStep >= 0 && !skipped) { TrainingLog.Add($"step {tStep + 1} done in {tStepT:0.0} s"); if (tStep < tSteps.Count - 1) Voice.Say(tStep % 2 == 0 ? "ctl_tr_good_1" : "ctl_tr_good_2", VoiceKind.Radio, 1, maxAge: 3f); }
            tStep++; tStepT = 0; tHinted = false;
            if (tStep >= tSteps.Count) { LessonComplete(); return; }
            tSteps[tStep].Start?.Invoke();
        }

        void LessonComplete()
        {
            int l = Training.Lesson; Training.P.done[l] = true; Training.Save();
            TrainingLog.Add($"lesson {l + 1} complete"); PlayLog.Add($"training: lesson {l + 1} complete");
            Voice.Say(Training.NextLesson(l) < 0 || Training.DoneCount >= (VrMode.Active ? 8 : 7) ? "ctl_tr_all" : "ctl_tr_done", VoiceKind.Radio, 3, maxAge: 10f);
            Displays.Objectives = "";
            pausedFrom = Now == State.Docked ? State.Docked : State.Flying; hudWasOn = Displays.Visible;
            Now = State.Paused; Time.timeScale = 0; Displays.SetVisible(false); Menu.SetBackdrop(.45f); Menu.Show(trainingDone);
        }

        // Leaving: the throwaway career goes, the real one comes back; to the lesson list or the title
        void LeaveTraining(bool toLessons)
        {
            if (seq != null) { StopCoroutine(seq); seq = null; }
            Time.timeScale = 1; Training.Active = false; tSteps = null;
            LeaveCampaign(); ClearCrash(); Career.EndTraining();
            Displays.Objectives = "";
            StartCoroutine(LeaveTrainingRoutine(toLessons));
        }
        IEnumerator LeaveTrainingRoutine(bool toLessons) { Menu.Show(loading); yield return ShowTitleWorld(); ShowTitle(); if (toLessons) Menu.Show(trainingPage); }

        // every frame in flight (Update, after CampaignTick)
        void TrainingTick()
        {
            if (!Training.Active || tSteps == null || tStep < 0 || tStep >= tSteps.Count || Now != State.Flying) return;
            float dt = Time.deltaTime; tStepT += dt;
            // what the flying lesson watches for
            tSeenPitchUp = Mathf.Max(tSeenPitchUp, Controls.Pitch); tSeenPitchDown = Mathf.Min(tSeenPitchDown, Controls.Pitch);
            tSeenRollR = Mathf.Max(tSeenRollR, Controls.Roll); tSeenRollL = Mathf.Min(tSeenRollL, Controls.Roll);
            tSeenYawR = Mathf.Max(tSeenYawR, Controls.Yaw); tSeenYawL = Mathf.Min(tSeenYawL, Controls.Yaw);
            if (Ship.Righting) tRightingSeen = true; if (Ship.LevelingOut) tLevelSeen = true;
            if (Controls.RouteTargetPressed) tRoutePresses++;
            if (VrMode.Active && Controls.RecenterPressed) tRecentred = true;
            var s = tSteps[tStep];
            if (s.Done()) { NextTrainingStep(false); return; }
            if (!tHinted && tStepT > 30f) { tHinted = true; TrainingLog.Add($"hint on step {tStep + 1}"); Voice.Say("ctl_tr_hint", VoiceKind.Radio, 2, maxAge: 6f); }
            string done = string.Join("  ", Enumerable.Range(0, tStep).Select(i => "✓"));
            string line = $"TRAINING {Training.Lesson + 1}: {Training.Names[Training.Lesson].ToUpperInvariant()}  ·  step {tStep + 1} / {tSteps.Count} {done}\n▶ {s.Text()}" + (tHinted ? $"\n<color=#FFD233>Hint: {s.Hint}</color>" : "");
            Displays.Objectives = line;
            flash = "▶ " + s.Text(); flashUntil = Time.time + .5f;
        }

        // an airlock opened to a practice alien: in a real flight it would be inside; here it stops at the door
        bool TrainingAlienAtDoor(DownedPilot p)
        {
            if (!Training.Active || !p.IsAlien) return false;
            TrainingLog.Add("opened the airlock to the practice alien (stopped: no scare in training)");
            Flash($"That was no pilot. In a real flight it would be inside with you now. Close up: start the engines ({B(Controls.Act.Engines)}).", 8f);
            return true;
        }

        void TrainingDocked()
        {
            tDocked = true; TrainingLog.Add("docked");
            Now = State.Docked; Phase = "docked"; Mother.Holding = true; Ship.MakeKinematic(true);
            if (tSteps != null && Training.Lesson == 6) { TrainingLog.Add($"docked on step {tStep + 1}: home is the lesson's goal"); tStep = tSteps.Count - 1; NextTrainingStep(false); }   // home: the lesson is done
        }

        IEnumerator TrainingCrashed()
        {
            TrainingLog.Add("crashed");
            yield return new WaitForSecondsRealtime(2.5f);
            StartTraining(Training.Lesson);
        }

        List<TStep> LessonSteps(int lesson)
        {
            var L = new List<TStep>();
            void Add(System.Func<string> text, System.Func<bool> done, string hint, System.Action start = null) => L.Add(new TStep { Text = text, Done = done, Hint = hint, Start = start });
            DownedPilot Pilot(int i) => Pilots.FirstOrDefault(p => p.Index == i);
            float NearestPilot() { float d = 1e9f; foreach (var p in Pilots) if (p.OnFoot) d = Mathf.Min(d, Flat(p.Position - Ship.transform.position).magnitude); return d; }
            switch (lesson)
            {
                case 0:   // flying
                    Add(() => $"Pitch: push the nose down and pull it up ({B(Controls.Act.PitchRoll)})", () => tSeenPitchUp > .5f && tSeenPitchDown < -.5f, "the stick forward and back, all the way", () => { tSeenPitchUp = tSeenPitchDown = 0; });
                    Add(() => $"Roll: tip the wings left and right ({B(Controls.Act.PitchRoll)})", () => tSeenRollR > .5f && tSeenRollL < -.5f, "the stick left and right", () => { tSeenRollL = tSeenRollR = 0; });
                    Add(() => $"Yaw: turn the nose left and right ({B(Controls.Act.Yaw)})", () => tSeenYawR > .4f && tSeenYawL < -.4f, "yaw turns the nose flat, without banking", () => { tSeenYawL = tSeenYawR = 0; });
                    Add(() => $"Throttle: full ({B(Controls.Act.Throttle)}), then back under 60 %", () => { if (Ship.Throttle > .95f) tThrottleFull = true; return tThrottleFull && Ship.Throttle < .6f; }, "watch the throttle % on the display", () => tThrottleFull = false);
                    Add(() => "Bank hard, then let go of the stick: she rights herself", () => tRightingSeen && Mathf.Abs(Mathf.DeltaAngle(0, Ship.transform.eulerAngles.z)) < 6f && !Ship.Righting, "roll past 45 degrees and let the stick go (Auto-level in Settings)", () => tRightingSeen = false);
                    Add(() => $"Level out at once: {B(Controls.Act.LevelOut)}", () => tLevelSeen, "it snaps the wings and nose level, twice as fast", () => tLevelSeen = false);
                    break;
                case 1:   // navigation
                    Add(() => $"The radar shows each beacon and its distance. Fly 400 m closer to the nearest one (now {NearestPilot() / 1000f:0.0} km)", () => NearestPilot() < tNearest0 - 400f, "turn until its blip is straight up on the radar", () => tNearest0 = NearestPilot());
                    Add(() => $"Pick a target for the route trail: {B(Controls.Act.RouteTarget)}", () => tRoutePresses > 0, "the trail's target shows on the radar list", () => tRoutePresses = 0);
                    Add(() => $"Too busy? Filter the radar: {B(Controls.Act.RadarFilter)} (All, Pilots, Enemies, Tasks; now {RadarScope.FilterNames[RadarScope.Filter]})", () => RadarScope.Filter != tFilter0, "the filter's name shows over the radar; the mothership always shows", () => tFilter0 = RadarScope.Filter);
                    Add(() => $"Follow the chevrons to a pilot: within 800 m (now {NearestPilot():0} m)", () => NearestPilot() < 800f, "fly through the chevrons; they keep you clear of the walls");
                    Add(() => $"The way home: {B(Controls.Act.RouteTarget)} until the trail leads to the mothership (HOME on the display)", () => routePick == -2 && tRoutePresses > 0, "each press picks the next target; the mothership is last", () => tRoutePresses = 0);
                    break;
                case 2:   // landing
                    Add(() => $"Brake into landing mode: {B(Controls.Act.Land)} (the air brakes, then hover)", () => Ship.Mode == FlightMode.Hover, "slow down near the beacon, then press it");
                    Add(() => "Hover over the ring by the beacon (amber, green when you're close)", () => Ship.Mode == FlightMode.Hover && sitePads.Count > 0 && Flat(sitePads[0] - Ship.transform.position).magnitude < 60f, "the ring marks where you would stop; the stick moves you in hover");
                    Add(() => $"Look down: the belly camera {B(Controls.Act.BellyCam)} (and the floor window at your feet)", () => Belly && Belly.On, "the camera shows on the centre display");
                    Add(() => $"Set her down gently: throttle down ({B(Controls.Act.Throttle)}), under 3 m/s at the end", () => Ship.Landed, "slow descent near the ground: the gear takes it");
                    break;
                case 3:   // the pickup
                    Add(() => $"Brake ({B(Controls.Act.Land)}), hover over the ring and land on the pad: throttle down ({B(Controls.Act.Throttle)})", () => Ship.Landed, "the ring by the beacon marks the pad");
                    Add(() => $"Engines off: {B(Controls.Act.Engines)}. The pilot won't come near running engines", () => Ship.Landed && !Ship.EnginesOn, "the exhaust would burn them");
                    Add(() => "Wait: the pilot runs to the airlock and knocks (three even knocks)", () => Pilots.Any(p => p.AtDoor), "within 160 m and 30 m of height they can reach you");
                    Add(() => $"Open the airlock: {B(Controls.Act.Airlock)}", () => cabin.Count > 0, "the airlock camera shows who is at the door");
                    Add(() => $"Look in the cabin: {B(Controls.Act.CabinCam)}", () => Displays.CabinPip, "the cabin camera on the display");
                    break;
                case 4:   // aliens
                    Add(() => $"Land by the beacon, engines off ({B(Controls.Act.Engines)}), and watch who comes", () => Ship.Landed && !Ship.EnginesOn, "this practice 'pilot' shows its tell plainly");
                    Add(() => "Listen at the door: a pilot knocks three even knocks. This one doesn't", () => Pilot(0) != null && Pilot(0).AtDoor && tStepT > 5f, "the knock is its tell; each kind has its own");
                    Add(() => $"Keep the airlock shut. Start the engines ({B(Controls.Act.Engines)}): the exhaust burns it", () => Planet.pilots[0] == (int)PilotFate.AlienGone, "engines or the shield burn whatever is at the hull");
                    Add(() => "Fly to the other beacon, land, engines off, and keep the airlock shut", () => CanopyAlien != null || Planet.pilots.Length > 1 && Planet.pilots[1] == (int)PilotFate.AlienGone, "a creature left at the door climbs onto the canopy");
                    Add(() => $"It's on the canopy! Engines on ({B(Controls.Act.Engines)}) or the shield ({B(Controls.Act.Shield)}): burn it off", () => Planet.pilots.Length < 2 || Planet.pilots[1] == (int)PilotFate.AlienGone, "in training it never gets in", () => Voice.Say("ctl_tr_canopy", VoiceKind.Radio, 3, maxAge: 10f));
                    break;
                case 5:   // combat
                    Add(() => $"Shield up: {B(Controls.Act.Shield)} (it stops bolts and uses energy)", () => ShieldOn, "press it again to drop it");
                    Add(() => $"Destroy the turret on the high ground (red light, on the radar): fire {B(Controls.Act.Fire)}", () => Foes.All.Count > 0 && Foes.All[0].Dead, "fly at it and keep firing; shield up when it locks on");
                    Add(() => $"Destroy the saucer: fire {B(Controls.Act.Fire)}", () => Foes.All.Count > 1 && Foes.All[1].Dead, "lead it a little; it comes round for passes");
                    break;
                case 6:   // getting home
                    Add(() => "RETURN NOW: just enough energy to climb home. Turn toward HOME (its bearing on the display)", () => Mathf.Abs(Mathf.DeltaAngle(Ship.Heading, Mathf.Atan2(Mother.transform.position.x - Ship.transform.position.x, Mother.transform.position.z - Ship.transform.position.z) * Mathf.Rad2Deg)) < 25f, "the mothership is the M on the radar");
                    Add(() => $"Hold {B(Controls.Act.Recall)} for a second: the mothership comes for you (or climb to the docking height)", () => calling != 0 || dockArmed || Now == State.Auto || tDocked, "out of energy in the air, its beam catches you anyway");
                    Add(() => $"The beam lifts you in. Hold {B(Controls.Act.FastForward)} to fast-forward", () => tDocked, "it plays live; sit back");
                    break;
                case 7:   // VR
                    Add(() => $"Look straight ahead and recentre: {B(Controls.Act.Pause)} held 1 s (or right-stick click)", () => tRecentred, "also on the pause menu", () => tRecentred = false);
                    break;
            }
            return L;
        }
    }
}
