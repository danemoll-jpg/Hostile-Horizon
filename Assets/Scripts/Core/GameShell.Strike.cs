using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HostileHorizon
{
    // Round 6 (A): the alien attack as a jump scare. Rule: the player must not be able to tell from the game's reaction that it was
    // an alien until the instant it strikes. An alien let in boards exactly as a pilot does (PilotBoarded: the same walk in, message,
    // chime, cabin count, seat, music); then it strikes, with no build-up, at a moment rolled from the planet's seed:
    //  - Instant: 1-4 s after the airlock shuts: it bursts through the cockpit hatch and is in front of your face.
    //  - On take-off: a few seconds after lift-off, or at the first hard turn: the same burst.
    // Round 7 (A4): the slow burn (lights, cabin noises, the hatch hit three times before the attack) is gone: it announced the
    // alien. In every strike the creature is in view first; only then the burst and lunge sounds, the sting, the music cut, and a
    // moment later the lights / displays, the alarm and the INTRUDER call. Nothing changes before it is visible.
    // Red herrings (A4) happen with real pilots and aliens alike (seeded per pilot, not per type): a light flicker as the airlock
    // cycles, a thump from the cabin, a crackle on the cabin camera. The canopy leap (A3): an alien left at the door may leap onto
    // the canopy with no warning instead of climbing slowly.
    public sealed partial class GameShell
    {
        public enum StrikeKind { Instant, Takeoff }
        sealed class Stowaway { public DownedPilot p; public StrikeKind kind; public float delay, clock; public bool armed; }
        readonly List<Stowaway> stows = new List<Stowaway>();
        Quaternion strikeLastRot; float strikeTurnRate;
        public int CheckStrikeKind = -1;                // checks: force a strike kind (-1: the seed's)
        public float StrikeAt { get; private set; } = -1;   // Time.time of the last strike (checks)
        public string LastStrike { get; private set; } = "";
        public int Stowaways => stows.Count;
        public void StrikeAtReset() { StrikeAt = -1; LastStrike = ""; StrikeSeenAt = -1; BurstAt = -1; }   // checks
        public bool CheckAllHerrings;                    // checks: every red herring on the next boardings
        public void CheckReseedRolls() { talkRoll = new System.Random(4); fuelRoll = new System.Random(5); chatterAt = 0; }   // checks: two boardings say the same

        public static StrikeKind RollStrike(int planetSeed, int index, out float delay, int force = -1)
        {
            var t = Tuning.T; var r = new SeedRng(planetSeed ^ (index * 6151), 59);
            float v = r.Value * (t.strikeInstantWeight + t.strikeTakeoffWeight), d = r.Value;
            var k = v < t.strikeInstantWeight ? StrikeKind.Instant : StrikeKind.Takeoff;
            if (force >= 0) k = (StrikeKind)force;   // checks
            delay = k == StrikeKind.Instant ? Mathf.Lerp(t.strikeInstantMin, t.strikeInstantMax, d) : Mathf.Lerp(t.strikeTakeoffMin, t.strikeTakeoffMax, d);
            return k;
        }

        // A3: leap or climb, and when (seconds at the door), per alien from the seed
        public static bool RollLeap(int planetSeed, int index, out float after)
        {
            var t = Tuning.T; var r = new SeedRng(planetSeed ^ (index * 3301), 61);
            bool leap = r.Value < t.canopyLeapChance; float d = r.Value;
            after = leap ? Mathf.Lerp(t.canopyLeapMin, t.canopyLeapMax, d) : t.canopyClimbAfter;
            return leap;
        }

        // called from PilotBoarded after the normal boarding: nothing the player can see or hear
        void StowAway(DownedPilot p)
        {
            var k = RollStrike(Planet.seed, p.Index, out float delay, CheckStrikeKind);
            stows.Add(new Stowaway { p = p, kind = k, delay = delay });
            strikeLastRot = Ship.transform.rotation; strikeTurnRate = 0;
            AlienEvent($"pilot {p.Index + 1} aboard: a {p.Type}, boarded as a pilot; it strikes {(k == StrikeKind.Instant ? $"{delay:0.0} s after the airlock shuts" : $"{delay:0.0} s after lift-off (or the first hard turn)")}");
        }

        void StowawayTick(float dt)
        {
            if (stows.Count == 0) return;
            var t = Tuning.T;
            if (dt > 0) { float rate = Quaternion.Angle(strikeLastRot, Ship.transform.rotation) / dt; strikeTurnRate = Mathf.Lerp(strikeTurnRate, rate, 1 - Mathf.Exp(-8f * dt)); }
            strikeLastRot = Ship.transform.rotation;
            foreach (var s in stows)
            {
                if (!s.armed) s.armed = s.kind == StrikeKind.Instant ? Extras.AirlockShut : !Ship.Landed;
                if (!s.armed) continue;
                s.clock += dt;
                bool hardTurn = s.kind == StrikeKind.Takeoff && s.clock > .6f && strikeTurnRate > t.strikeHardTurn;
                if (s.clock >= s.delay || hardTurn) { Strike(s, hardTurn ? "the first hard turn" : "its moment"); return; }
            }
        }

        // anything that would take the ship out of the player's hands first (docking, the beam, out of energy): it strikes now
        bool StrikeBeforeLeaving(string why) { if (stows.Count == 0) return false; Strike(stows[0], why); return true; }

        void Strike(Stowaway s, string why)
        {
            stows.Clear();
            StrikeAt = Time.time; LastStrike = $"{s.kind} ({why}) after {s.clock:0.0} s armed";
            AlienEvent($"strike: the {s.p.Type} (pilot {s.p.Index + 1}), {LastStrike}; {(Ship.Landed ? "landed" : $"in flight, {Ship.AboveGround:0} m up")}");
            Trace.Add("strike", s.kind.ToString());
            CabinRoom.HideSeat(CabinRoom.SeatOf(s.p.Index));
            Run(Burst(s.p.Type, s.p));
        }

        // Instant / on take-off. Round 7 (A4): first the creature, through the hatch behind your left shoulder, round the seat and into
        // view; nothing else changes until it is in view (the flight, controls, music, lights and displays go on as they were). The
        // moment it is in view: the hatch bursts, the lunge, the sting, the music cuts, controls are taken; 0.35 s later the lights /
        // displays die, the alarm and the INTRUDER call. Round 7 (A2): never closer than 0.42 m with any part of it (it came to 0.16 m
        // before: the eye was inside its body). Round 8 (A2): StageAtFace: its face nearer than the cockpit screens, its body beside you.
        public float StrikeSeenAt { get; private set; } = -1;   // checks: Time.time it came into view
        public float BurstAt { get; private set; } = -1;        // checks: Time.time the burst began
        IEnumerator Burst(AlienType type, DownedPilot p, int fromCabin = -1)   // fromCabin: the Sleeper's seat, emptied when it is seen
        {
            var st = Ship.transform; if (p != null) AlienGone(p);
            var c = horrorCreature = NewCreature(type); c.Show(true); c.FullDetail(!CheckOldStaging); c.SetPose(Creature.Pose.Lunge, true); c.Jaw = 1; c.Speed = 14; c.Act("attack", 1.5f);
            c.transform.SetPositionAndRotation(st.TransformPoint(Cabin.HatchCockpitSide + new Vector3(0, -.3f, -.2f)), st.rotation);
            PlayLog.Add($"alien strike: the {type} comes through the hatch");
            float tt = 0; bool seen = false, after = false; float seenAt = 0; StrikeSeenAt = -1; BurstAt = Time.time;
            while (true)
            {
                if (!seen && Now == State.Paused) { yield return null; continue; }   // paused before it is seen: it waits too
                tt += Time.deltaTime; var head = Rig.Head.transform;
                // round your left side (0.25 s), then in front of your face
                var eye = Eye;
                if (CheckOldStaging) PlaceFace(c, head.position + head.forward * (tt < .4f ? .5f : .16f) + head.up * .02f, eye, head.up, tt < .4f ? 14f : 18f);   // checks: 0.6.3's
                else if (tt < .25f && !seen) PlaceFace(c, eye - head.right * .75f + head.forward * .3f - head.up * .1f, eye, head.up, 16f);
                else StageAtFace(c, 0, 16f);
                KeepClear(c, StrikeGap); c.LightFrom(eye);
                if (!seen && (InView(c) || tt > .6f))
                {
                    seen = true; seenAt = tt; StrikeSeenAt = Time.time;
                    if (fromCabin >= 0) { cabin.Remove(fromCabin); Reseat(); foreach (var l in horrorLoops) if (l) Destroy(l.gameObject); horrorLoops.Clear(); }   // and its tell's sounds stop
                    Now = State.Auto; Phase = "horror"; KilledBy = type; Ship.InputEnabled = false; calling = 0; HorrorBeat = "strike";
                    PlayAt("hatch_burst", Cabin.HatchCentre, 1f, 3f); PlayAt("creature_lunge", ShipLayout.Eye + new Vector3(0, 0, .3f), 1f, 1f);
                    CabinRoom.HitDoor(); CabinRoom.BreakDoor(); Rig.Shake(.5f);
                    Music.Sting("sting_alien_reveal"); Music.Cut(); Voice.StopAll(); Menu.Hide();
                    Displays.SetVisible(false);   // Round 8 (A2): no flat HUD, prompt or subtitle over it (back with the death page)
                    if (!Ship.Landed) driftCo = StartCoroutine(Drift());
                    AlienEvent($"in view {tt:0.00} s after the strike ({(InView(c) ? "seen" : "not yet in view: 0.6 s limit")}), {c.NearestTo(Eye, gapVerts):0.00} m from the eye at its nearest");
                }
                if (seen && !after && tt - seenAt > .35f)
                {
                    after = true; HorrorBeat = "at your face";   // after the strike: the computer, the lights, the displays
                    Voice.Say("cpu_intruder", VoiceKind.Computer, 3);
                    CabinRoom.Lights(false); Displays.Blackout = true; CabinRoom.Emergency(true); CabinRoom.FeedOn = true;
                    LoopAt("alarm", ShipLayout.DashCentre + new Vector3(0, .3f, -.2f), .45f, 2f);
                }
                if (seen && tt - seenAt > 1.6f) break;
                yield return null;
            }
            HorrorBeat = "dead";
            AlienEvent($"the {type} got you ({Time.time - StrikeAt:0.0} s after the strike)");
            Now = State.Crashed;
            Died(type);
        }

        // in the cockpit view: the face on screen, in front of the camera
        bool InView(Creature c)
        {
            var v = Rig.Head.WorldToViewportPoint(c.HeadWorld);
            return v.z > .2f && v.x > .05f && v.x < .95f && v.y > .05f && v.y < .95f;
        }

        // ---- A4: red herrings, the same for real pilots and aliens (seeded per pilot)
        void Herrings(DownedPilot p)
        {
            var t = Tuning.T; var r = new SeedRng(Planet.seed ^ (p.Index * 4049), 67);
            bool flick = r.Value < t.herringFlicker, thump = r.Value < t.herringThump, crackle = r.Value < t.herringCrackle;
            if (CheckAllHerrings) flick = thump = crackle = true;
            if (flick || thump || crackle) StartCoroutine(HerringPlay(flick, thump, crackle));
        }

        IEnumerator HerringPlay(bool flick, bool thump, bool crackle)
        {
            IEnumerator Wait(float s) { float t = 0; while (t < s) { t += Time.deltaTime; yield return null; } }
            if (flick)
            {   // a power dip as the airlock cycles: two short flickers, never the full lights-out
                Trace.Add("herring", "flicker");
                PlayAt("power_dip", ShipLayout.DashCentre, .5f, 3f);
                foreach (float gap in new[] { .07f, .12f, .05f, .09f })
                {
                    if (Phase == "horror") yield break;
                    bool off = CabinRoom.LightsOn; CabinRoom.Lights(!off); Displays.Blackout = off;
                    yield return Wait(gap);
                }
                if (Phase != "horror") { CabinRoom.Lights(true); Displays.Blackout = false; }
            }
            if (thump) { yield return Wait(1.3f); if (Phase == "horror") yield break; Trace.Add("herring", "thump"); PlayAt("cabin_thump", Cabin.Inside, .9f, 2.5f); }
            if (crackle) { yield return Wait(2.2f); if (Phase == "horror") yield break; Trace.Add("herring", "crackle"); CabinRoom.Crackle(.7f); PlayAt("feed_crackle", ShipLayout.DashCentre, .35f, 2f); }
        }
    }
}
