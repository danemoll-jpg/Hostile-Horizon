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
        public static void Assign(PlanetState p, bool firstPlanet)
        {
            var t = Tuning.T; int n = p.pilots.Length;
            p.alien = new int[n]; p.quirk = new int[n];
            var rng = new SeedRng(p.seed, 31);
            int count = 0;
            if (!firstPlanet && n > 0)
            {
                float share = Mathf.Min(t.alienShareMax, t.alienShareStart + t.alienSharePerTier * p.tier);
                count = Mathf.Clamp(Mathf.RoundToInt(share * n), 1, n);
            }
            var order = new List<int>(); for (int i = 0; i < n; i++) order.Add(i);
            for (int i = n - 1; i > 0; i--) { int j = rng.Next(i + 1); (order[i], order[j]) = (order[j], order[i]); }
            for (int k = 0; k < n; k++)
            {
                int i = order[k];
                int type = k < count ? rng.Next(5) + 1 : 0;
                bool tell = rng.Value < t.tellReliability;
                bool falseTell = rng.Value < t.falseTellChance;
                int fake = rng.Next(4) + 1;
                p.alien[i] = type;
                p.quirk[i] = type != 0 ? (tell ? type : 0) : falseTell ? fake : 0;
            }
        }

        // The Sleeper's turn: seconds of flight after it boards, from the seed.
        public static float SleeperDelay(PlanetState p, int index) { var r = new SeedRng(p.seed ^ (index * 7919), 37); var t = Tuning.T; return Mathf.Lerp(t.sleeperTurnMin, t.sleeperTurnMax, r.Value); }
    }
}
