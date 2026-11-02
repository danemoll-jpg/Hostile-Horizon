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
        public static int OfTheDay => int.Parse(DateTime.Now.ToString("yyyyMMdd"));
        public static int NewRandom() => new Random().Next(0, Max + 1);
    }
}
