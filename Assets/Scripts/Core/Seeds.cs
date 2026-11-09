using System;

namespace HostileHorizon
{
    // Round 1, item 2: where a world's seed comes from. The seed is the number the player sees (0 - 99 999 999); CanyonField hashes it.
    //  - seed of the day: the local date as a number, e.g. 20261102 (everyone flying that day gets the same canyons);
    //  - random: a new number each time it is picked (shown, so it can be typed in again later);
    //  - typed in.
    public static class Seeds
    {
        public const int Max = 99999999;
        public static readonly string[] ModeNames = { "Seed of the day", "Random", "Typed in" };
        public static int OfTheDay => PromoDay > 0 ? PromoDay : int.Parse(DateTime.Now.ToString("yyyyMMdd"));
        // Round 10 (B): the promo capture fixes the day (-promoDay yyyymmdd), so a re-render gets the same planets
        static int promoDay = -1;
        static int PromoDay { get { if (promoDay < 0) { var a = LauncherBridge.Argument("-promoDay"); promoDay = a != null && int.TryParse(a, out int v) ? v : 0; } return promoDay; } }
        public static int NewRandom() => new Random().Next(0, Max + 1);
    }
}

namespace HostileHorizon
{
    // Round 2: a small seeded random generator (xorshift), so planets, names and pilot spots never depend on the runtime's Random.
    public sealed class SeedRng
    {
        uint s;
        public SeedRng(int seed, int stream) { unchecked { s = (uint)seed * 2654435761u ^ (uint)stream * 2246822519u ^ 0x9e3779b9u; if (s == 0) s = 1; for (int i = 0; i < 4; i++) Next(); } }
        public uint Next() { s ^= s << 13; s ^= s >> 17; s ^= s << 5; return s; }
        public int Next(int n) => (int)(Next() % (uint)n);
        public float Value => (Next() & 0xffffff) / 16777216f;
    }
}
