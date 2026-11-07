using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HostileHorizon
{
    // Round 6 (A): the alien attack as a jump scare. Rule: the player must not be able to tell from the game's reaction that it was
    // an alien until the instant it strikes. An alien let in boards exactly as a pilot does (PilotBoarded: the same walk in, message,
    // chime, cabin count, seat, music); then it strikes, with no build-up, at a moment rolled from the planet's seed:
    //  - Instant: 1-4 s after the airlock shuts: a slam on the cockpit hatch, it bursts through and is at your face in about a second.
    //  - On take-off: a few seconds after lift-off, or at the first hard turn: the same burst.
    //  - Slow burn: the long sequence (lights, cabin noises, the hatch hit three times), started at a random moment in flight.
    // Red herrings (A4) happen with real pilots and aliens alike (seeded per pilot, not per type): a light flicker as the airlock
    // cycles, a thump from the cabin, a crackle on the cabin camera. The canopy leap (A3): an alien left at the door may leap onto
    // the canopy with no warning instead of climbing slowly.
    public sealed partial class GameShell
    {
        public enum StrikeKind { Instant, Takeoff, SlowBurn }
        sealed class Stowaway { public DownedPilot p; public StrikeKind kind; public float delay, clock; public bool armed; }
        readonly List<Stowaway> stows = new List<Stowaway>();
        Quaternion strikeLastRot; float strikeTurnRate;
        public int CheckStrikeKind = -1;                // checks: force a strike kind (-1: the seed's)
        public float StrikeAt { get; private set; } = -1;   // Time.time of the last strike (checks)
        public string LastStrike { get; private set; } = "";
        public int Stowaways => stows.Count;
        public void StrikeAtReset() { StrikeAt = -1; LastStrike = ""; }   // checks
        public bool CheckAllHerrings;                    // checks: every red herring on the next boardings
        public void CheckReseedRolls() { talkRoll = new System.Random(4); fuelRoll = new System.Random(5); chatterAt = 0; }   // checks: two boardings say the same

        public static StrikeKind RollStrike(int planetSeed, int index, out float delay, int force = -1)
        {
            var t = Tuning.T; var r = new SeedRng(planetSeed ^ (index * 6151), 59);
            float v = r.Value * (t.strikeInstantWeight + t.strikeTakeoffWeight + t.strikeSlowWeight), d = r.Value;
            var k = v < t.strikeInstantWeight ? StrikeKind.Instant : v < t.strikeInstantWeight + t.strikeTakeoffWeight ? StrikeKind.Takeoff : StrikeKind.SlowBurn;
            if (force >= 0) k = (StrikeKind)force;   // checks
            delay = k == StrikeKind.Instant ? Mathf.Lerp(t.strikeInstantMin, t.strikeInstantMax, d) : k == StrikeKind.Takeoff ? Mathf.Lerp(t.strikeTakeoffMin, t.strikeTakeoffMax, d) : Mathf.Lerp(t.strikeSlowMin, t.strikeSlowMax, d);
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
            AlienEvent($"pilot {p.Index + 1} aboard: a {p.Type}, boarded as a pilot; it strikes {(k == StrikeKind.Instant ? $"{delay:0.0} s after the airlock shuts" : k == StrikeKind.Takeoff ? $"{delay:0.0} s after lift-off (or the first hard turn)" : $"{delay:0} s into the flight (slow burn)")}");
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
            if (s.kind == StrikeKind.SlowBurn) Run(Horror(s.p.Type, 3, s.p));
            else Run(Burst(s.p.Type, s.p));
        }

        // Instant / on take-off: one slam and it is through the hatch and at your face, about a second, no build-up. Then, after it:
        // the computer's INTRUDER call, the lights and the displays; the normal death.
        IEnumerator Burst(AlienType type, DownedPilot p)
        {
            Now = State.Auto; Phase = "horror"; KilledBy = type; Ship.InputEnabled = false; calling = 0; HorrorBeat = "strike";
            Music.Cut(); Voice.StopAll(); Menu.Hide();
            AlienGone(p);
            var st = Ship.transform;
            Music.Sting("sting_alien_reveal");
            PlayAt("hatch_burst", Cabin.HatchCentre, 1f, 3f); PlayAt("creature_lunge", ShipLayout.Eye + new Vector3(0, 0, .3f), 1f, 1f);
            CabinRoom.HitDoor(); CabinRoom.BreakDoor(); Rig.Shake(.5f);
            if (!Ship.Landed) driftCo = StartCoroutine(Drift());
            var c = horrorCreature = NewCreature(type); c.Show(true); c.SetPose(Creature.Pose.Lunge); c.Jaw = 1; c.Speed = 14; c.Act("attack", 1.5f);
            c.transform.SetPositionAndRotation(st.TransformPoint(Cabin.HatchCockpitSide + new Vector3(0, -.3f, -.2f)), st.rotation);
            float tt = 0; bool after = false;
            while (tt < 1.9f)
            {
                tt += Time.deltaTime; var head = Rig.Head.transform;
                // through the hatch to your face (0.4 s), then the lunge, then it stays there
                float reach = tt < .4f ? .5f : .16f, rate = tt < .4f ? 14f : 18f;
                PlaceFace(c, head.position + head.forward * reach + head.up * .02f, head.position, head.up, rate);
                if (tt > .4f && HorrorBeat == "strike") HorrorBeat = "at your face";
                if (!after && tt > .35f)
                {
                    after = true;   // after the strike: the computer, the lights, the displays
                    Voice.Say("cpu_intruder", VoiceKind.Computer, 3);
                    CabinRoom.Lights(false); Displays.Blackout = true; CabinRoom.Emergency(true); CabinRoom.FeedOn = true;
                    LoopAt("alarm", ShipLayout.DashCentre + new Vector3(0, .3f, -.2f), .45f, 2f);
                }
                yield return null;
            }
            HorrorBeat = "dead";
            AlienEvent($"the {type} got you ({Time.time - StrikeAt:0.0} s after the strike)");
            Now = State.Crashed;
            Died(type);
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
