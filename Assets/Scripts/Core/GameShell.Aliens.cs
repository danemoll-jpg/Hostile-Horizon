using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace HostileHorizon
{
    // Round 3: aliens. Which pilots are aliens comes from the planet's seed (Aliens.Assign); each type's tell is in the 3D world
    // (DownedPilot). Here: what happens when you meet one.
    //  - Engines started with it at the hull (B4): the exhaust kills it; it shrieks and shows what it was; a bounty; a Field Journal entry.
    //  - Left at a closed airlock (B5): it climbs onto the canopy and presses against the glass in front of you (scraping, breathing,
    //    tapping); still ignored, it breaks in. Engines on while it is up there throw it off and kill it.
    //  - Let in (B7): the horror sequence, live in the cockpit (VR rule 5: no camera takeover, no fade, no strobe): the airlock closes,
    //    the lights die, an alarm, something moving in the cabin behind you, the hatch behind your left shoulder is hit and gives way,
    //    and it comes round the seat at you. Then the normal death (the ship is lost with it aboard; regeneration, recovery / loaner).
    //  - The Sleeper (B8) boards as a pilot and turns after a seeded time in flight: it kills a pilot in the cabin; enough pilots left
    //    kill it, too few and it comes for you. Docked before it turns, the mothership's security kills it in the bay.
    //  - The cabin camera (B9) and the Field Journal (B10).
    public sealed partial class GameShell
    {
        public Cabin CabinRoom { get; private set; }
        Creature horrorCreature; readonly List<Creature> corpses = new List<Creature>();
        public Creature HorrorCreature => horrorCreature;   // checks
        readonly List<AudioSource> horrorLoops = new List<AudioSource>();
        Coroutine canopyCo, sleeperCo, driftCo;
        public DownedPilot CanopyAlien { get; private set; }
        public float CanopyClock { get; private set; }
        public int SleeperIndex { get; private set; } = -1;     // the Sleeper's pilot index while it rides in the cabin
        public float SleeperClock { get; private set; }
        public float SleeperTurnAt { get; set; }                  // checks may shorten the wait (the seeded value is logged first)
        bool sleeperTell, sleeperTurning;
        public AlienType KilledBy { get; private set; }
        public string HorrorBeat { get; private set; } = "";    // checks: the sequence's current beat
        public readonly List<string> AlienEvents = new List<string>();   // checks: what happened, in order

        void AlienEvent(string s) { AlienEvents.Add($"{Time.time:0.0} {s}"); PlayLog.Add("alien: " + s); }   // Round 7 (A3): in the [Play] log

        void CreateAliens()
        {
            CabinRoom = Cabin.Create(Ship, litTemplate);
            Displays.CabinRoom = CabinRoom;
        }

        Creature NewCreature(AlienType type, bool onShip = true) { var c = Creature.Create(litTemplate, Random.Range(1, 99999), type); if (onShip) c.transform.SetParent(Ship.transform, false); return c; }   // in or on the ship: carried with it

        // Leaving the planet or a new sortie: no creature, no loops, the cabin as it was.
        void ClearAliens()
        {
            if (canopyCo != null) StopCoroutine(canopyCo); if (sleeperCo != null) StopCoroutine(sleeperCo); if (driftCo != null) StopCoroutine(driftCo);
            canopyCo = sleeperCo = driftCo = null; CanopyAlien = null; stows.Clear(); SleeperIndex = -1; sleeperTurning = false; HorrorBeat = "";
            foreach (var a in horrorLoops) if (a) Destroy(a.gameObject); horrorLoops.Clear();
            if (horrorCreature) Destroy(horrorCreature.gameObject); horrorCreature = null;
            foreach (var c in corpses) if (c) Destroy(c.gameObject); corpses.Clear();
            if (CabinRoom) { CabinRoom.Lights(true); CabinRoom.Emergency(false); CabinRoom.ResetDoor(); CabinRoom.Seat(new List<int>(), -1, false); CabinRoom.FeedOn = false; }
            if (Displays) Displays.Blackout = false;
            if (Ship) { Ship.InputEnabled = true; foreach (var r in Ship.GetComponentsInChildren<Renderer>(true)) if (r.sharedMaterial && r.sharedMaterial.shader.name == "HostileHorizon/Glass") r.enabled = true; }
        }

        void Reseat() { if (CabinRoom) CabinRoom.Seat(cabin, SleeperIndex, sleeperTell); }

        AudioSource LoopAt(string clip, Vector3 shipSpace, float volume, float minDistance)
        {
            var a = Sfx.Loop(Sfx.Clip(clip), Ship.transform, volume, minDistance); a.transform.localPosition = shipSpace; horrorLoops.Add(a); return a;
        }
        void PlayAt(string clip, Vector3 shipSpace, float volume = 1, float minDistance = 3) => Sfx.Play(Sfx.Clip(clip), Ship.transform.TransformPoint(shipSpace), volume, minDistance, Ship.transform);

        // The journal: an encounter that showed what it was.
        void Met(AlienType t, string how)
        {
            var c = Career.C; var e = c.Meet(t, Planet?.name, how == "it killed you"); c.Save();
            journalNote = e.encounters == 1 ? $"Field Journal: new entry — {Aliens.Names[(int)t]}" : e.encounters == 2 ? $"Field Journal: {Aliens.Names[(int)t]} — a clearer note" : "";
            AlienEvent($"journal: {Aliens.Names[(int)t]} encounter {e.encounters} ({how}): \"{Aliens.Hint(t, e.encounters)}\"");
        }

        void AlienGone(DownedPilot p) { Planet.pilots[p.Index] = (int)PilotFate.AlienGone; }

        // ---- B4: the exhaust (called from OnEnginesStarted for each alien within range)

        void ExhaustKillsAlien(DownedPilot p, string how = "exhaust")
        {
            var c = Career.C; var t = Tuning.T;
            var at = p.Position; var face = p.Facing;
            p.Hide(); AlienGone(p); c.stats.aliensKilled++; c.Earn(t.alienBounty);
            AlienEvent($"{how} killed pilot {p.Index + 1}: a {p.Type} (tell shown: {p.Quirk != AlienType.None}); bounty +{t.alienBounty} cr");
            Met(p.Type, "killed by the " + how);
            StartCoroutine(Dies(p.Type, at, face, Vector3.zero));
            Voice.Say("ctl_alien_killed", VoiceKind.Radio, 2, maxAge: 12f);   // Round 4
            Flash($"That was no pilot: a {Aliens.Names[(int)p.Type]}. Bounty +{t.alienBounty} cr" + (journalNote != "" ? $"   ·   {journalNote}" : ""), 8f);
        }

        // The creature shows itself where it died: it rears up thrashing, shrieks, and falls (a throw from the canopy flies first).
        IEnumerator Dies(AlienType type, Vector3 at, Quaternion face, Vector3 throwVel, Creature c = null)
        {
            if (!c) { c = NewCreature(type, false); c.transform.SetPositionAndRotation(at, face); }
            c.transform.SetParent(null, true);
            corpses.Add(c); c.Show(true); c.SetPose(Creature.Pose.Thrash); c.Jaw = 1; c.Speed = 12;
            Sfx.Play(Sfx.Clip("alien_shriek"), at + Vector3.up * 1.8f, 1f, 8f);
            Music.Sting("sting_alien_reveal");   // Round 4
            var f = Terrain.Field; Vector3 p = at, v = throwVel; float tt = 0;
            while (tt < 1.6f || v.sqrMagnitude > 0)
            {
                float dt = Time.deltaTime; tt += dt;
                if (v.sqrMagnitude > 0) { v += Vector3.down * 9.8f * dt; p += v * dt; float g = f.SurfaceHeight(p.x, p.z); if (p.y < g) { p.y = g; v = Vector3.zero; } c.transform.position = p; c.transform.rotation *= Quaternion.Euler(200f * dt, 0, 90f * dt); }
                if (tt > 6f) break;
                yield return null;
            }
            c.transform.rotation = Quaternion.Euler(0, c.transform.eulerAngles.y, 0);
            Sfx.Play(Sfx.Clip("alien_death"), c.transform.position + Vector3.up, 1f, 8f);
            c.SetPose(Creature.Pose.Dead); c.Speed = 5; c.Jaw = .7f;
        }

        // ---- B5: the canopy

        void AlienClimbs(DownedPilot p)
        {
            if (!InCampaign || Now != State.Flying || CanopyAlien != null) return;
            AlienEvent($"pilot {p.Index + 1} ({p.Type}) left at the airlock {p.AtDoorFor:0} s: it climbs onto the canopy");
            canopyCo = StartCoroutine(Canopy(p));
        }

        // Ship-space points: the face pressed to the glass in front of the pilot's eye; the climb up the left side
        // Round 7 (A2): leaning over the canopy (feet on the nose, the body along the front of the glass, tilted 60 degrees back toward
        // you), its face pressed on the glass 25 degrees above straight ahead, about 1.3 m from the eye, looking down at you. 0.6.3: it
        // stood upright on the nose (its "lying" orientation degenerated: forward and up both pointed back), face 2 m ahead, 13-16 % of
        // its body inside the glass.
        static readonly Vector3 OldCanopyFace = new Vector3(0, 1.27f, 4.35f);
        // Tuning limit reached (two tries, round7a): pushed out along the glass the face went 2.5 m away and partly out of view;
        // leaning over the canopy put the face above the view with 4 % still inside. Kept at 0.6.3's place for Dan to decide;
        // what changed there: full detail, the face light, two-sided material. CanopyStaging = true turns the push back on.
        public static bool CanopyStaging = false;
        static readonly Vector3 NewCanopyFace = new Vector3(0, 1.63f, 3.50f);
        Vector3 CanopyFace => CheckOldStaging || !CanopyStaging ? OldCanopyFace : NewCanopyFace;
        Vector3 CanopyUp(Transform st) => CheckOldStaging || !CanopyStaging ? -st.forward * .9f + st.up * .45f : st.TransformDirection(new Vector3(0, .5f, -.87f));
        Vector3 CanopyLook(Transform st) => CheckOldStaging || !CanopyStaging ? Eye : st.TransformPoint(CanopyFace) + st.TransformDirection(new Vector3(0, -.87f, -.5f));
        static readonly Vector3[] Climb = { new Vector3(-2.7f, -.9f, -1.6f), new Vector3(-2.0f, .3f, -.6f), new Vector3(-1.2f, 1.2f, 1.2f), new Vector3(-.6f, 1.45f, 3.6f), new Vector3(0, 1.3f, 4.6f) };

        IEnumerator Canopy(DownedPilot p)
        {
            CanopyAlien = p; CanopyClock = 0; HorrorBeat = "climbing";
            var c = horrorCreature = NewCreature(p.Type); c.Show(true); c.FullDetail(true); c.SetPose(Creature.Pose.Crawl, true); c.Speed = 8;
            c.Act(p.Type == AlienType.Mimic || p.Leaps ? "leap" : "climb", 1.4f);   // Fix 3.1: up the hull (the Mimic leaps)
            var st = Ship.transform;
            float t = 0;
            if (p.Leaps)
            {
                // Round 6 (A3): no warning: it leaps from the ground and slams onto the canopy in a third of a second
                HorrorBeat = "leap"; AlienEvent("it leaps onto the canopy");
                Vector3 from = new Vector3(-3.2f, -1.2f, 5.5f);
                while (t < .32f)
                {
                    if (CanopyAlien != p) yield break;
                    t += Time.deltaTime; float k = Mathf.Clamp01(t / .32f);
                    var lp = Vector3.Lerp(from, CanopyFace + new Vector3(0, .1f, .3f), k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * .8f;
                    c.transform.SetPositionAndRotation(st.TransformPoint(lp), Quaternion.LookRotation(st.TransformDirection(CanopyFace - from), st.up));
                    yield return null;
                }
                PlayAt("canopy_slam", CanopyFace, 1f, 2f); Rig.Shake(.8f); c.SetPose(Creature.Pose.Canopy); c.Jaw = .8f;
                var head0 = Rig.Head.transform.position;
                c.transform.rotation = Quaternion.LookRotation(CanopyLook(st) - st.TransformPoint(CanopyFace), CanopyUp(st));
                c.transform.position += st.TransformPoint(CanopyFace) - c.HeadWorld;
                t = 2.4f;   // straight on to pressing against the glass
            }
            // up the hull, live (2.4 s)
            while (t < 2.4f)
            {
                if (CanopyAlien != p) yield break;
                t += Time.deltaTime; float k = Mathf.Clamp01(t / 2.4f) * (Climb.Length - 1); int i = Mathf.Min((int)k, Climb.Length - 2);
                var lp = Vector3.Lerp(Climb[i], Climb[i + 1], k - i);
                var dir = st.TransformDirection(Climb[i + 1] - Climb[i]);
                c.transform.SetPositionAndRotation(st.TransformPoint(lp) - Vector3.up * .3f, Quaternion.LookRotation(dir, st.up));
                yield return null;
            }
            // pressed on the glass: lying over the canopy, the face toward the pilot
            HorrorBeat = "on the canopy"; c.SetPose(Creature.Pose.Canopy); c.Speed = 5;
            AlienEvent("on the canopy: pressed against the glass");
            LoopAt("canopy_scrape", CanopyFace, .8f, 2f); LoopAt("canopy_breath", CanopyFace, 1f, 1.5f);
            float nextTap = 1.5f;
            while (CanopyClock < Tuning.T.breakInAfter)
            {
                if (CanopyAlien != p) yield break;
                if (Now != State.Flying) { yield return null; continue; }   // paused: it waits too
                CanopyClock += Time.deltaTime;
                if (Training.Active) CanopyClock = Mathf.Min(CanopyClock, Tuning.T.breakInAfter - 1f);   // Round 11: in training it never breaks in
                PlaceFace(c, st.TransformPoint(CanopyFace), CanopyLook(st), CanopyUp(st), 4f);   // lying over the nose, head toward you
                if (Time.frameCount % 3 == 0) KeepOutOfCanopy(c);
                c.LightFrom(Eye);
                c.Jaw = .25f + .2f * Mathf.Sin(CanopyClock * 1.3f);
                if (CanopyClock > nextTap) { nextTap += 3.2f; PlayAt("glass_tap", CanopyFace, 1f, 2f); }
                yield return null;
            }
            AlienEvent($"on the canopy {CanopyClock:0.0} s, ignored: it breaks in");
            canopyCo = null;
            Run(Horror(p.Type, 1, p));
        }

        // Turn the creature to face `look` (its up toward `up`) and move it so its face is at `face`.
        static void PlaceFace(Creature c, Vector3 face, Vector3 look, Vector3 up, float rate)
        {
            float k = 1 - Mathf.Exp(-rate * Time.deltaTime);
            var want = Quaternion.LookRotation(look - face, up);
            c.transform.rotation = Quaternion.Slerp(c.transform.rotation, want, k);
            c.transform.position += (face - c.HeadWorld) * k;
        }

        // Round 7 (A2): staging up close. Cause found (check round7a, round 6's pictures): the strike put the head bone 0.16 m from
        // the eye (the body's nearest part 0.18 m), and these bodies are 2-3 m across, so the camera ended up in or against the skin:
        // through the near clip it saw the inside of the shell, back faces culled ("see through their bodies"); on the canopy 13-16 %
        // of the body stood inside the glass. The meshes are opaque and closed. Now the face stops StrikeFace from the eye and no
        // part comes closer than StrikeGap (pushed straight away from the eye); on the canopy no part comes through the glass.
        public const float StrikeFace = .65f, StrikeGap = .42f;
        public bool CheckOldStaging;   // checks: 0.6.3's staging, for the before pictures
        readonly List<UnityEngine.Vector3> gapVerts = new List<UnityEngine.Vector3>();
        // Round 7 (A2): the eye as the ship carries it (the camera follows the ship in LateUpdate, so during a frame's coroutines it
        // is a frame behind: chasing it at speed left the creature v/16 metres off, 9 m on a fast launch, 1-2 m in flight)
        Vector3 Eye => CheckOldStaging || VrMode.Active ? Rig.Head.transform.position : Ship.transform.TransformPoint(ShipLayout.Eye);   // Round 8 (C6): in VR the real head
        void KeepClear(Creature c, float gap)
        {
            if (CheckOldStaging || !c) return;
            var eye = Eye; c.Surface(gapVerts); float best = float.MaxValue; Vector3 at = eye + Rig.Head.transform.forward;
            foreach (var v in gapVerts) { float d2 = (v - eye).sqrMagnitude; if (d2 < best) { best = d2; at = v; } }
            float d = Mathf.Sqrt(best); if (gapVerts.Count == 0 || d >= gap) return;
            // Round 8: straight away from the eye (Round 7 pushed along the view, which drove a body beside you forward into the screens)
            // Round 8: straight away from the eye (Round 7 pushed along the view, which drove a body beside you forward into the
            // screens). Pushing toward the hatch side instead was tried (round8a run 3): it slid the creature out of view behind you.
            c.transform.position += (StrikeStaging >= 2 ? (at - eye).normalized : Rig.Head.transform.forward) * (gap - d);
        }

        // Round 8 (A2). Cause (check round8a): Round 7 turned the whole creature to look at you with its face 0.65 m ahead, so its
        // body went straight out from there, away from you: its neck, shoulders and chest 0.6-1.5 m ahead, behind the cockpit
        // screens (0.51-0.57 m from the eye) and down inside the dash. The screens z-test correctly, so they were drawn over it. Now
        // its face is nearer than the screens and its body lies where there is room: beside you, back past your left shoulder toward
        // the hatch it came through (or, through the broken canopy, out over the top of the dash); the upper body and head turn so
        // the face looks at you (Creature.LookAt). StrikeStaging 0 = Round 7's, 1 = Round 7's at the new distance (checks).
        public static int StrikeStaging = 2;
        public const float StrikeFaceNear = .45f;
        void StageAtFace(Creature c, int from, float rate, float nearer = 0)   // from: 0 the hatch, 1 the canopy
        {
            var head = Rig.Head.transform; var eye = Eye;
            if (StrikeStaging <= 1)
            {
                c.LookAt = null; c.FaceAt = null;
                PlaceFace(c, eye + head.forward * ((StrikeStaging == 0 ? StrikeFace : StrikeFaceNear) + nearer) - head.up * .06f, eye, head.up, rate);
                return;
            }
            var face = eye + head.forward * (StrikeFaceNear + nearer) + (from == 1 ? head.up * .04f : -head.right * .06f - head.up * .03f);
            // body = from the head toward the hips (these bodies hang below the head): down past your left shoulder toward the hatch, or
            // up over the dash under the canopy roof (head first, toward you); the chest faces you as far as that line allows
            var body = from == 1 ? (head.forward * .85f + head.up * .5f).normalized
                                 : (-head.right * .6f - head.up * .65f - head.forward * .45f).normalized;
            var chest = Vector3.ProjectOnPlane(eye - face, body); if (chest.sqrMagnitude < 1e-4f) chest = head.up;
            float k = 1 - Mathf.Exp(-rate * Time.deltaTime);
            c.transform.rotation = Quaternion.Slerp(c.transform.rotation, Quaternion.LookRotation(chest, -body), k);
            c.LookAt = c.Local(eye); c.FaceAt = c.Local(face); c.FaceRate = rate;   // the face is moved onto its mark after the animation (Creature.LateUpdate)
        }
        // the canopy bubble (ship.py: ellipsoid at (0, .62, 2.95), radii (1.02, .98, 1.75)): push the creature out along the glass's
        // normal at its face until no vertex is inside
        public static readonly Vector3 CanopyC = new Vector3(0, .62f, 2.95f), CanopyR = new Vector3(1.02f, .98f, 1.75f);
        public static float CanopyDepth(Vector3 shipPoint, Vector3 shipDir)
        {
            var q = Vector3.Scale(shipPoint - CanopyC, new Vector3(1 / CanopyR.x, 1 / CanopyR.y, 1 / CanopyR.z)); var d = Vector3.Scale(shipDir, new Vector3(1 / CanopyR.x, 1 / CanopyR.y, 1 / CanopyR.z));
            float a = Vector3.Dot(d, d), b = 2 * Vector3.Dot(q, d), cc = Vector3.Dot(q, q) - 1; if (cc >= 0) return 0;
            return (-b + Mathf.Sqrt(Mathf.Max(0, b * b - 4 * a * cc))) / (2 * a);
        }
        void KeepOutOfCanopy(Creature c)
        {
            if (CheckOldStaging || !CanopyStaging || !c) return;
            var st = Ship.transform; c.Surface(gapVerts);
            var n = Vector3.Scale(CanopyFace - CanopyC, new Vector3(1 / (CanopyR.x * CanopyR.x), 1 / (CanopyR.y * CanopyR.y), 1 / (CanopyR.z * CanopyR.z))).normalized;
            float push = 0; foreach (var v in gapVerts) push = Mathf.Max(push, CanopyDepth(st.InverseTransformPoint(v), n));
            if (push > 0) c.transform.position += st.TransformDirection(n) * (push + .02f);
        }

        void ThrowOffCanopy()
        {
            var p = CanopyAlien; if (p == null) return;
            CanopyAlien = null; if (canopyCo != null) { StopCoroutine(canopyCo); canopyCo = null; }
            foreach (var a in horrorLoops) if (a) Destroy(a.gameObject); horrorLoops.Clear();
            var c = horrorCreature; horrorCreature = null; HorrorBeat = ""; if (c) c.FullDetail(false);
            var t = Tuning.T; var career = Career.C;
            AlienGone(p); career.stats.aliensKilled++; career.Earn(t.alienBounty);
            AlienEvent($"engines on with it on the canopy: thrown off ({p.Type}); bounty +{t.alienBounty} cr");
            Met(p.Type, "thrown off the canopy");
            var v = Ship.transform.TransformDirection(new Vector3(-6f, 7f, 4f));
            StartCoroutine(Dies(p.Type, c.transform.position, c.transform.rotation, v, c));
            Flash($"Thrown off and burnt: a {Aliens.Names[(int)p.Type]}. Bounty +{t.alienBounty} cr" + (journalNote != "" ? $"   ·   {journalNote}" : ""), 8f);
        }

        // ---- B7: let in. entry 0 = the airlock, 1 = through the canopy, 2 = from the cabin (the Sleeper)

        IEnumerator Horror(AlienType type, int entry, DownedPilot p = null)
        {
            Now = State.Auto; Phase = "horror"; KilledBy = type; Ship.InputEnabled = false; calling = 0;
            // Round 6 (A2): the music cuts at the strike; the computer's INTRUDER call comes after it, with the alarm
            Music.Cut(); Voice.StopAll();
            Displays.SetVisible(false); Menu.Hide();
            if (p != null) AlienGone(p);
            AlienEvent($"horror sequence: a {type}, in through {(entry == 0 ? "the airlock" : entry == 1 ? "the canopy" : "the cabin")}");
            if (!Ship.Landed) driftCo = StartCoroutine(Drift());   // in flight: the autopilot holds the ship as best it can
            var st = Ship.transform; var c = horrorCreature;
            IEnumerator Wait(float s) { float t = 0; while (t < s) { t += Time.deltaTime; yield return null; } }
            if (entry == 0)
            {
                HorrorBeat = "airlock closes";
                yield return Wait(.8f);
                Extras.CloseAirlock(); Sfx.Play(Sfx.Thunk, st.TransformPoint(ShipExtras.DoorAt), 1f, 4f, st);
                yield return Wait(1.2f);
            }
            // the lights die
            HorrorBeat = "lights die";
            CabinRoom.Lights(false); Displays.Blackout = true; CabinRoom.Emergency(true); CabinRoom.FeedOn = true;
            PlayAt("lights_die", ShipLayout.DashCentre, 1f, 3f);
            yield return Wait(1.4f);
            var alarm = LoopAt("alarm", ShipLayout.DashCentre + new Vector3(0, .3f, -.2f), .45f, 2f);
            Voice.Say("cpu_intruder", VoiceKind.Computer, 3);
            if (entry != 1)
            {
                // something moving in the cabin behind you
                HorrorBeat = "moving in the cabin";
                if (!c) { c = horrorCreature = NewCreature(type); }
                c.Show(true); c.FullDetail(true); c.SetPose(Creature.Pose.Crawl, true); c.Speed = 6;
                var move = LoopAt("cabin_move", Cabin.Inside, 1f, 2.5f);
                Vector3[] round = { new Vector3(-1.1f, -.64f, -1.6f), new Vector3(.3f, -.64f, -3.0f), new Vector3(.9f, -.64f, -1.0f), new Vector3(-.2f, -.64f, -.2f), new Vector3(-.625f, -.64f, .55f) };
                float t = 0, T = 6f;
                while (t < T)
                {
                    t += Time.deltaTime; float k = Mathf.Clamp01(t / T) * (round.Length - 1); int i = Mathf.Min((int)k, round.Length - 2);
                    var lp = Vector3.Lerp(round[i], round[i + 1], Mathf.SmoothStep(0, 1, k - i));
                    move.transform.localPosition = lp + Vector3.up * .4f;
                    c.transform.SetPositionAndRotation(st.TransformPoint(lp), Quaternion.Slerp(c.transform.rotation, st.rotation * Quaternion.LookRotation(round[i + 1] - round[i] + new Vector3(0, .001f, 0)), 1 - Mathf.Exp(-4f * Time.deltaTime)));
                    yield return null;
                }
                // at the hatch: three blows, then it gives way
                c.transform.SetPositionAndRotation(st.TransformPoint(new Vector3(-.625f, -.64f, .55f)), st.rotation);
                foreach (float gap in new[] { 1.5f, 1.25f, 1.0f })
                {
                    HorrorBeat = "the hatch is hit"; c.Act("attack"); CabinRoom.HitDoor(); PlayAt("door_hit", Cabin.HatchCentre, 1f, 3f);
                    yield return Wait(gap);
                }
                HorrorBeat = "the hatch gives way"; CabinRoom.BreakDoor(); PlayAt("door_break", Cabin.HatchCentre, 1f, 3f);
                Destroy(move.gameObject);
                // through the hatch and up beside the seat, on your left
                Vector3[] inPath = { new Vector3(-.625f, -.64f, .55f), new Vector3(-.625f, -.35f, 1.35f), new Vector3(-.72f, -.2f, 2.0f) };
                t = 0; T = 1.8f;
                while (t < T)
                {
                    t += Time.deltaTime; float k = Mathf.Clamp01(t / T) * (inPath.Length - 1); int i = Mathf.Min((int)k, inPath.Length - 2);
                    c.transform.SetPositionAndRotation(st.TransformPoint(Vector3.Lerp(inPath[i], inPath[i + 1], k - i)), st.rotation);
                    yield return null;
                }
            }
            else
            {
                // through the canopy: it cracks, then gives way
                HorrorBeat = "the glass cracks"; PlayAt("glass_crack", CanopyFace, 1f, 2f);
                yield return Wait(2.2f);
                HorrorBeat = "the canopy gives way"; PlayAt("glass_break", CanopyFace, 1f, 2f);
                foreach (var r in Ship.GetComponentsInChildren<Renderer>()) if (r.sharedMaterial && r.sharedMaterial.shader.name == "HostileHorizon/Glass") r.enabled = false;
                foreach (var a in horrorLoops) if (a && a != alarm && a.clip && a.clip.name.StartsWith("canopy_scrape")) a.Stop();
            }
            // in front of you, whichever way you look; the jaw opens; it lunges
            HorrorBeat = "in front of you"; c.SetPose(Creature.Pose.Lunge); c.Speed = 4;
            PlayAt("canopy_breath", ShipLayout.Eye + new Vector3(0, 0, .4f), .9f, 1f);
            float tt = 0;
            while (tt < 2.4f)
            {
                tt += Time.deltaTime; var head = Rig.Head.transform;
                if (CheckOldStaging) PlaceFace(c, head.position + head.forward * .5f + head.up * .02f, Eye, head.up, 3.2f); else StageAtFace(c, entry == 1 ? 1 : 0, 3.2f);
                KeepClear(c, StrikeGap); c.LightFrom(Eye);
                c.Jaw = Mathf.Clamp01((tt - 1f) / 1.2f);
                yield return null;
            }
            HorrorBeat = "lunge"; PlayAt("creature_lunge", ShipLayout.Eye + new Vector3(0, 0, .3f), 1f, 1f); Music.Sting("sting_alien_reveal"); c.Jaw = 1; c.Speed = 14; c.Act("attack", 1.5f);
            tt = 0; while (tt < .45f) { tt += Time.deltaTime; var head = Rig.Head.transform; if (CheckOldStaging) PlaceFace(c, head.position + head.forward * .16f, head.position, head.up, 12f); else StageAtFace(c, entry == 1 ? 1 : 0, 12f, -.05f); KeepClear(c, StrikeGap - .05f); c.LightFrom(Eye); yield return null; }
            HorrorBeat = "dead";
            AlienEvent($"the {type} got you");
            Now = State.Crashed;
            Died(type);
        }

        // In flight when the horror starts: no one flies the ship; it slows to a drifting hover, sinking slowly (never into the ground).
        IEnumerator Drift()
        {
            Ship.MakeKinematic(false);
            Vector3 pos = Ship.transform.position, vel = Ship.Velocity; Quaternion rot = Ship.transform.rotation;
            while (true)
            {
                float dt = Time.deltaTime; if (dt > 0)
                {
                    vel = Vector3.MoveTowards(vel, Vector3.down * 1.2f, 14f * dt); pos += vel * dt;
                    pos.y = Mathf.Max(pos.y, Terrain.Field.SurfaceHeight(pos.x, pos.z) + 25f);
                    rot = Quaternion.Slerp(rot, Quaternion.Euler(0, rot.eulerAngles.y, 0), 1 - Mathf.Exp(-.8f * dt));
                    Ship.SetPose(pos, rot, dt, false);
                }
                yield return null;
            }
        }

        // ---- B8: the Sleeper

        void SleeperBoarded(DownedPilot p)
        {
            SleeperIndex = p.Index; SleeperClock = 0; SleeperTurnAt = Aliens.SleeperDelay(Planet, p.Index); sleeperTell = p.Quirk == AlienType.Sleeper;
            AlienEvent($"pilot {p.Index + 1} aboard: a Sleeper (tell shown: {sleeperTell}); turns after {SleeperTurnAt:0} s of flight");
            if (sleeperTell) { LoopAt("sleeper_click", new Vector3(.6f, -.2f, -2.2f), .35f, 1.2f); LoopAt("sleeper_voice", new Vector3(.6f, .0f, -2.0f), .3f, 1.2f); }
        }

        void SleeperTick(float dt)
        {
            if (SleeperIndex < 0 || sleeperTurning || Now != State.Flying || Ship.Landed) return;
            SleeperClock += dt;
            if (SleeperClock >= SleeperTurnAt) { sleeperTurning = true; sleeperCo = StartCoroutine(SleeperTurns()); }
        }

        IEnumerator SleeperTurns()
        {
            var t = Tuning.T; var career = Career.C; int me = SleeperIndex;
            var others = cabin.FindAll(i => i != me);
            AlienEvent($"the Sleeper (pilot {me + 1}) turns after {SleeperClock:0} s with {others.Count} other pilot{(others.Count == 1 ? "" : "s")} aboard");
            // Round 7 (A4): alone with you it comes straight for the cockpit as a burst (nothing before it is in view)
            if (others.Count == 0) { SleeperIndex = -1; sleeperTurning = false; AlienEvent("no other pilot aboard: it comes for the cockpit"); Run(Burst(AlienType.Sleeper, Pilots.Find(x => x.Index == me), me)); yield break; }
            foreach (var a in horrorLoops) if (a) Destroy(a.gameObject); horrorLoops.Clear();
            var c = horrorCreature = NewCreature(AlienType.Sleeper);
            Voice.StopAll(); Voice.Say("cpu_intruder", VoiceKind.Computer, 3);   // Round 4
            // it kills one of them: heard, and glimpsed on the cabin camera, never shown in full (the lights go)
            int victim = others[new SeedRng(Planet.seed ^ me, 41).Next(others.Count)];
            int vSeat = CabinRoom.SeatOf(victim), sSeat = CabinRoom.SeatOf(me);
            HorrorBeat = "cabin: a pilot is killed";
            CabinRoom.HideSeat(sSeat);
            var at = CabinRoom.SeatPoint(vSeat);
            c.Show(true); c.SetPose(Creature.Pose.Lunge, true); c.transform.SetPositionAndRotation(Ship.transform.TransformPoint(new Vector3(at.x * .3f, -.64f, at.z)), Ship.transform.rotation * Quaternion.LookRotation(new Vector3(at.x, 0, 0)));
            c.Jaw = 1; c.Act("attack");
            SayScream(victim);   // Round 4: the victim screams (through the bulkhead)
            PlayAt("cabin_kill", at, 1f, 2f); CabinRoom.Lights(false); CabinRoom.SlumpSeat(vSeat);
            Flash("Screams from the cabin!", 4f);
            float tt = 0; while (tt < 3f) { tt += Time.deltaTime; yield return null; }
            cabin.Remove(victim); Planet.pilots[victim] = (int)PilotFate.Lost; career.stats.pilotsLost++; CabinRoom.HideSeat(vSeat);
            int left = others.Count - 1;
            if (left >= t.sleeperPilotsWin)
            {
                HorrorBeat = "cabin: the pilots fight it";
                PlayAt("pilots_fight", Cabin.Inside, 1f, 2f); c.SetPose(Creature.Pose.Thrash); c.Act("hit");
                tt = 0; while (tt < 3.5f) { tt += Time.deltaTime; yield return null; }
                PlayAt("alien_death", Cabin.Inside, 1f, 2f); c.SetPose(Creature.Pose.Dead); c.transform.position = Ship.transform.TransformPoint(new Vector3(0, -.2f, -1.6f));
                corpses.Add(c); horrorCreature = null;
                c.transform.SetParent(Ship.transform, true);
                CabinRoom.Lights(true);
                cabin.Remove(me); Planet.pilots[me] = (int)PilotFate.AlienGone; SleeperIndex = -1; sleeperTurning = false; HorrorBeat = "";
                Met(AlienType.Sleeper, "the pilots killed it");
                Reseat(); CabinRoom.HideSeat(-1);
                AlienEvent($"the pilots killed the Sleeper ({left} left after it killed pilot {victim + 1})");
                Flash($"Something came aboard as pilot {me + 1}. It killed pilot {victim + 1}; the other {left} killed it." + (journalNote != "" ? $"   ·   {journalNote}" : ""), 10f);
            }
            else
            {
                AlienEvent($"only {left} pilot{(left == 1 ? "" : "s")} left after it killed pilot {victim + 1}: it comes for the cockpit");
                cabin.Remove(me); Reseat(); SleeperIndex = -1;
                if (horrorCreature) { Destroy(horrorCreature.gameObject); horrorCreature = null; }
                Run(Burst(AlienType.Sleeper, Pilots.Find(x => x.Index == me)));   // Round 7 (A4): the burst, as every strike
            }
            sleeperCo = null;
        }

        // Docked with the Sleeper still aboard: the mothership's security goes in. A short live moment from the cockpit: shots and a
        // shriek in the cabin behind you. The other pilots are delivered.
        IEnumerator Security()
        {
            if (SleeperIndex < 0) yield break;
            int me = SleeperIndex; HorrorBeat = "security in the bay";
            AlienEvent($"docked with the Sleeper (pilot {me + 1}) aboard: security kills it in the bay");
            foreach (var a in horrorLoops) if (a) Destroy(a.gameObject); horrorLoops.Clear();
            Displays.Prompt = "Security: something in your cabin";
            float t = 0; while (t < .8f) { t += Time.deltaTime; yield return null; }
            PlayAt("security_shots", Cabin.Inside, 1f, 3f);
            Voice.Say("ctl_security", VoiceKind.Radio, 2, maxAge: 15f);   // Round 4
            t = 0; while (t < .7f) { t += Time.deltaTime; yield return null; }
            PlayAt("alien_shriek", Cabin.Inside + new Vector3(0, .4f, -.5f), 1f, 3f);
            t = 0; while (t < 2.6f) { t += Time.deltaTime; yield return null; }
            cabin.Remove(me); Planet.pilots[me] = (int)PilotFate.AlienGone; SleeperIndex = -1; HorrorBeat = "";
            Career.C.stats.aliensKilled++;
            Met(AlienType.Sleeper, "security killed it");
            Reseat();
            securityLine = $"Security killed a Sleeper in your cabin: it came aboard as pilot {me + 1}." + (journalNote != "" ? $" {journalNote}." : "");
        }
        string securityLine = "";

        // ---- checks only: a planet with chosen aliens (count pilots, types and shown tells), and a pilot put straight aboard
        public void CheckSetup(int count, int[] types, int[] quirks)
        {
            Planet.pilots = new int[count]; Planet.alien = new int[count]; Planet.quirk = new int[count];
            for (int i = 0; i < count; i++) { Planet.alien[i] = i < types.Length ? types[i] : 0; Planet.quirk[i] = i < quirks.Length ? quirks[i] : 0; }
            cabin.Clear(); stows.Clear(); Reseat(); SpawnPilots();
        }
        public void CheckBoard(DownedPilot p) { p.ForceAboard(); PilotBoarded(p); }
        // Round 7 (checks): a creature standing in the cabin, facing the cabin camera 1.6 m in front of it
        // checks (round8a): a creature held where the strike puts it (from 0 the hatch, 1 the canopy), until it is destroyed
        public Creature CheckStrikeCreature(AlienType t, int from)
        {
            var c = NewCreature(t); c.Show(true); c.FullDetail(true); c.SetPose(Creature.Pose.Lunge, true); c.Jaw = 1; c.Act("attack", 1.5f);
            c.transform.SetPositionAndRotation(Ship.transform.TransformPoint(ShipLayout.Eye + new Vector3(0, 0, .6f)), Ship.transform.rotation * Quaternion.Euler(0, 180, 0));
            StartCoroutine(HoldStage(c, from)); return c;
        }
        IEnumerator HoldStage(Creature c, int from) { while (c) { StageAtFace(c, from, 16f); KeepClear(c, StrikeGap); c.LightFrom(Eye); yield return null; } }

        public Creature CheckCabinCreature(AlienType t, bool old)
        {
            var c = NewCreature(t); c.Show(true); c.FullDetail(!old); c.SetPose(Creature.Pose.Stand, true);
            var cam = CabinRoom.Cam.transform; var at = cam.position + Vector3.ProjectOnPlane(cam.forward, Ship.transform.up).normalized * 1.6f;
            at = Ship.transform.TransformPoint(Vector3.Scale(Ship.transform.InverseTransformPoint(at), new Vector3(1, 0, 1)) + new Vector3(0, -.64f, 0));
            c.transform.SetPositionAndRotation(at, Quaternion.LookRotation(Vector3.ProjectOnPlane(cam.position - at, Ship.transform.up), Ship.transform.up));
            c.LightFrom(cam.position);
            return c;
        }

        // ---- B10: the Field Journal: Core/GameShell.Journal.cs (Round 6, Part E)
    }
}
