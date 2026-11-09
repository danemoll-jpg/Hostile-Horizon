using System.Collections.Generic;
using UnityEngine;

namespace HostileHorizon
{
    // Round 3, items B1, B3 and B10: the alien types, which pilots are aliens (from the planet's seed alone, so the same planet
    // always has the same aliens), whether each shows its tell, and the Field Journal's hints (vague first, clearer after a second
    // encounter, never a full explanation).
    public enum AlienType { None = 0, Mimic = 1, Hollow = 2, Knocker = 3, Stalker = 4, Sleeper = 5 }

    public static class Aliens
    {
        public static readonly string[] Names = { "", "Mimic", "Hollow", "Knocker", "Stalker", "Sleeper" };

        public static readonly string[][] Hints =
        {
            new string[0],
            new[] { "Something about the way the light falls on it...", "Watch the ground beside it, not the suit. Light and shadow should agree." },
            new[] { "Look it in the eye.", "A visor shows the world around it: the sky, your lamps. This one showed nothing at all." },
            new[] { "Listen at the door.", "The corps knocks a code. Count, and listen to where it comes from." },
            new[] { "It took its time getting to the door.", "Pilots run straight to a ship. Watch the way it comes, and the radar." },
            new[] { "It was so quiet in the cabin. Until it wasn't.", "After a pickup, listen to the cabin and watch the cabin camera. If something is wrong, dock fast." },
        };

        public static string Hint(AlienType t, int encounters) => t == AlienType.None || encounters <= 0 ? "" : Hints[(int)t][Mathf.Min(encounters, 2) - 1];

        // Which of a planet's pilots are aliens, of what type, and whether each shows its tell; a real pilot can get a false tell
        // (a tell-like quirk: falseTellChance, 0 on Normal). quirk = the behaviour shown (the type's tell, or None).
        // Fix 9.1 (A1): each pilot is rolled on its own at the planet's share (Normal 10 % + 2.5 % a tier, at most 20 %, times the
        // difficulty's level), so some planets have none. 0.9.0 put at least one alien on every planet after the first (with 3-4
        // pilots that was 25-33 % or more of all pickups: Dan met them about half the time).
        public static float Share(int tier) { var t = Tuning.T; return Mathf.Min(t.alienShareMax, t.alienShareStart + t.alienSharePerTier * tier) * Difficulty.AlienShare; }

        public static void Assign(PlanetState p, bool firstPlanet)
        {
            int n = p.pilots.Length;
            p.alien = new int[n]; p.quirk = new int[n];
            var rng = new SeedRng(p.seed, 31);
            float share = firstPlanet ? 0 : Share(p.tier);
            for (int i = 0; i < n; i++)
            {
                bool alien = rng.Value < share;
                int type = rng.Next(5) + 1;
                bool tell = rng.Value < Difficulty.TellReliability;   // Round 5: from the difficulty (Normal = tuning tellReliability .75)
                bool falseTell = rng.Value < Difficulty.FalseTell;
                int fake = rng.Next(4) + 1;
                p.alien[i] = alien ? type : 0;
                p.quirk[i] = alien ? (tell ? type : 0) : falseTell ? fake : 0;
            }
        }

        // Round 6 (D3): pilots added by a distress call (from index `from`): aliens among them at the planet's share, tells as usual
        public static void AssignExtra(PlanetState p, int from, SeedRng rng)
        {
            float share = Share(p.tier);
            for (int i = from; i < p.pilots.Length; i++)
            {
                p.pilots[i] = (int)PilotFate.Waiting;
                int type = rng.Value < share ? rng.Next(5) + 1 : 0;
                bool tell = rng.Value < Difficulty.TellReliability, falseTell = rng.Value < Difficulty.FalseTell; int fake = rng.Next(4) + 1;
                p.alien[i] = type; p.quirk[i] = type != 0 ? (tell ? type : 0) : falseTell ? fake : 0;
            }
        }

        // The Sleeper's turn: seconds of flight after it boards, from the seed.
        public static float SleeperDelay(PlanetState p, int index) { var r = new SeedRng(p.seed ^ (index * 7919), 37); var t = Tuning.T; return Mathf.Lerp(t.sleeperTurnMin, t.sleeperTurnMax, r.Value); }
    }
}
