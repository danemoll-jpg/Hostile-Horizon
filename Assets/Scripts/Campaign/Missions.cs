using System;
using System.Collections.Generic;
using UnityEngine;

namespace HostileHorizon
{
    // Round 9 (B1): a planet's side missions. 1-3 per planet, chosen by its seed when the mothership first arrives after Round 9
    // (saved in PlanetState.missions, so they stay what they were): recover the flight recorder (land at a marked crash site, engines
    // off, hold the airlock button), knock out the turret nest (3-5 turrets guarding one valley), the timed rescue (one real pilot's
    // life support runs down in flight time: rescue and dock before zero), the clean run (clear the planet with no hull damage).
    // Rewards (tuning) are paid at the dock; failing one costs only its reward (the timed pilot is lost). Code: GameShell.Missions.cs.
    public enum MissionKind { Recorder = 0, Turrets = 1, Timed = 2, Clean = 3 }
    public enum MissionState { Open = 0, Done = 1, Paid = 2, Failed = 3 }

    [Serializable] public sealed class Mission
    {
        public int kind, state;
        public float x, z;          // Recorder: the crash site's pad; Turrets: the valley the nest guards
        public int count;           // Turrets: how many in the nest
        public int pilot = -1;      // Timed: the pilot's index
        public float timeLeft;      // Timed: seconds of flight time left
        public bool aboard;         // Recorder: in the cabin (lost again if you die)
        public MissionKind Kind => (MissionKind)kind;
        public MissionState State { get => (MissionState)state; set => state = (int)value; }
        public bool Open => State == MissionState.Open;
        public Vector3 Site(CanyonField f) => new Vector3(x, f != null ? f.SurfaceHeight(x, z) : 0, z);
    }

    public static class Missions
    {
        public static readonly string[] Names = { "Recover the flight recorder", "Knock out the turret nest", "Timed rescue", "Clean run" };
        public static readonly string[] Short = { "RECORDER", "TURRET NEST", "LIFE SUPPORT", "CLEAN RUN" };
        public static readonly string[] Voice = { "ctl_obj_recorder", "ctl_obj_turrets", "ctl_obj_timed", "ctl_obj_clean" };

        public static bool CheckAll;   // checks: every kind the planet allows

        public static int Reward(MissionKind k)
        {
            var t = Tuning.T;
            int r = k == MissionKind.Recorder ? t.missionRecorderReward : k == MissionKind.Turrets ? t.missionTurretsReward : k == MissionKind.Timed ? t.missionTimedReward : t.missionCleanReward;
            return Mathf.RoundToInt(r * Difficulty.Pay);
        }

        // The planet's missions, from its seed: pads = the pilots' landing pads, start = where the mothership arrives.
        public static List<Mission> Make(PlanetState p, CanyonField f, List<Vector3> pads, Vector3 start, bool enemiesOn)
        {
            var t = Tuning.T; var r = new SeedRng(p.seed, 91); var list = new List<Mission>();
            int want = t.missionsMin + r.Next(t.missionsMax - t.missionsMin + 1);
            if (CheckAll) want = 4;
            var kinds = new List<MissionKind> { MissionKind.Recorder, MissionKind.Turrets, MissionKind.Timed, MissionKind.Clean };
            for (int i = kinds.Count - 1; i > 0; i--) { int j = r.Next(i + 1); (kinds[i], kinds[j]) = (kinds[j], kinds[i]); }
            var used = new List<Vector3>(pads);
            foreach (var k in kinds)
            {
                if (list.Count >= want) break;
                var m = new Mission { kind = (int)k };
                if (k == MissionKind.Recorder || k == MissionKind.Turrets)
                {
                    if (k == MissionKind.Turrets && !enemiesOn) continue;
                    if (!FreePad(f, p.seed, k == MissionKind.Recorder ? 93 : 95, used, start, k == MissionKind.Recorder ? 500f : 800f, out var at)) continue;
                    m.x = at.x; m.z = at.z; used.Add(at);
                    if (k == MissionKind.Turrets) m.count = t.missionNestMin + r.Next(t.missionNestMax - t.missionNestMin + 1);
                }
                else if (k == MissionKind.Timed)
                {
                    var real = new List<int>(); for (int i = 0; i < p.pilots.Length; i++) if (p.pilots[i] == (int)PilotFate.Waiting && (p.alien == null || p.alien[i] == 0)) real.Add(i);
                    if (real.Count == 0) continue;
                    m.pilot = real[r.Next(real.Count)]; m.timeLeft = t.missionTimedSeconds;
                }
                else if (p.Count(PilotFate.Waiting) == 0) continue;   // Clean: a planet already cleared has nothing left to clear
                list.Add(m);
            }
            return list;
        }

        // a landing pad on canyon floor, away from the pilots' pads, the start and the other missions' sites (from its own stream)
        static bool FreePad(CanyonField f, int seed, int stream, List<Vector3> used, Vector3 start, float gap, out Vector3 at)
        {
            var t = Tuning.T; var r = new SeedRng(seed, stream); at = Vector3.zero;
            float rMin = t.pilotRangeMin, rMax = t.pilotRangeMax + t.pilotRangePerTier;
            for (int a = 0; a < 2000; a++)
            {
                float ang = r.Value * Mathf.PI * 2, d = Mathf.Lerp(rMin, rMax, Mathf.Sqrt(r.Value));
                var pad = f.PadAt(Mathf.FloorToInt((start.x + Mathf.Cos(ang) * d) / CanyonField.PadCell), Mathf.FloorToInt((start.z + Mathf.Sin(ang) * d) / CanyonField.PadCell));
                if (!pad.ok || f.OverLiquid(pad.x, pad.z) || f.HasLiquid && pad.h < f.Liquid + 3f) continue;
                var p = new Vector3(pad.x, pad.h, pad.z);
                if (new Vector2(p.x - start.x, p.z - start.z).magnitude < t.turretMinFromStart) continue;
                bool near = false; foreach (var u in used) if (new Vector2(u.x - p.x, u.z - p.z).magnitude < gap) { near = true; break; }
                if (near) continue;
                at = p; return true;
            }
            return false;
        }

        public static string Clock(float s) { s = Mathf.Max(0, s); return $"{(int)(s / 60)}:{(int)(s % 60):00}"; }
    }
}
