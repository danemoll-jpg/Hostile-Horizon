namespace HostileHorizon
{
    // Round 2, item 3: generated names. A planet's name comes from its seed alone (the same day gives the same planet and name);
    // a career's home star system from the career's own seed. Our own syllables, nothing from the original game.
    public static class Names
    {
        static readonly string[] Start = { "Ve", "Ka", "Tor", "Ny", "Sel", "Dra", "Ou", "Mer", "Ash", "Ilo", "Zan", "Quo", "Hal", "Rhe", "Vo", "Esk", "Tul", "Bry", "Ce", "Oph" };
        static readonly string[] Mid = { "ss", "r", "ll", "th", "v", "nd", "k", "m", "sh", "dr", "x", "n", "rr", "g" };
        static readonly string[] End = { "ra", "on", "is", "a", "eth", "ix", "or", "ia", "un", "el", "os", "ae", "ir", "yn" };
        static readonly string[] Numerals = { "II", "III", "IV", "V", "VI", "VII", "VIII", "IX" };

        static string Word(SeedRng r)
        {
            string w = Start[r.Next(Start.Length)];
            if (r.Next(3) > 0) w += Mid[r.Next(Mid.Length)];
            return w + End[r.Next(End.Length)];
        }

        public static string Planet(int seed) { var r = new SeedRng(seed, 11); return Word(r) + "-" + Numerals[r.Next(Numerals.Length)]; }
        public static string StarSystem(int seed) { var r = new SeedRng(seed, 12); return Word(r); }
    }
}
