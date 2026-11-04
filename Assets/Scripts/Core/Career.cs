using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace HostileHorizon
{
    // Round 2, item 2: the one career save, career.json beside settings.json in the save folder (persistentDataPath =
    // %USERPROFILE%\AppData\LocalLow\Hostile Horizon\Hostile Horizon\, which the launcher backs up before every update and never
    // touches otherwise). A check run saves into <check dir>/save instead, so it never touches Dan's career.
    // Saved on dock, death, quitting to the title and closing the game.
    public enum PilotFate { Waiting = 0, Rescued = 1, Lost = 2, AlienGone = 3 }   // Round 3: AlienGone = an alien dealt with (killed, or it took your ship)

    [Serializable] public sealed class ShipState
    {
        public float damage;          // hull damage, percent (hull = 100 - damage)
        public float energy = 100;    // percent
        public bool wrecked;
        public List<string> upgrades = new List<string>();   // empty until the upgrade shop (Round 5)
    }

    [Serializable] public sealed class PlanetState
    {
        public int seed;
        public string name;
        public int tier;              // planets cleared in the career when first visited
        public int[] pilots;          // PilotFate per pilot
        public int[] alien, quirk;    // Round 3: AlienType per pilot (0 = a real pilot), and the tell shown (Aliens.Assign, from the seed)
        public bool cleared, bonusPaid;
        public int visits;
        public string biome = "canyons";   // Round 4 (A1): Biomes.Keys; from the seed (a career's first planet: always canyons)
        public bool biomeSet;              // Round 4: false in a save from before Round 4 (its biome is worked out on the next visit)
        public string lastVisit;           // Fix 3.2: for the planet list's order (most recent first)
        public bool OfTheDay => seed == Seeds.OfTheDay;
        public int Count(PilotFate f) { int n = 0; foreach (var p in pilots) if (p == (int)f) n++; return n; }
    }

    [Serializable] public sealed class CareerStats
    {
        public int pilotsRescued, pilotsLost, creditsEarned, creditsSpent, deaths, planetsCleared, sorties, tows, recalls;
        public int aliensKilled;      // Round 3
    }

    [Serializable] public sealed class JournalEntry { public int type; public int encounters; }

    [Serializable] public sealed class Career
    {
        public int version = 1;
        public int systemSeed;
        public string systemName;
        public int credits;
        public ShipState ship = new ShipState();     // your own ship
        public ShipState loaner = new ShipState();   // the corps' basic ship, while you fly it
        public bool flyingLoaner;
        public List<PlanetState> planets = new List<PlanetState>();
        public CareerStats stats = new CareerStats();
        public List<JournalEntry> journal = new List<JournalEntry>();   // Round 3: the Field Journal
        public string started, lastSaved;
        public int currentSeed;      // Fix 3.2: the planet the mothership is over (0 = none yet: today's)
        public int planetsMade;      // Fix 3.2: how many new planets this career has generated (the next one's number)

        public static Career C { get; private set; }
        public ShipState Current => flyingLoaner ? loaner : ship;

        public static string Folder => CheckRun.Active ? Path.Combine(CheckRun.Dir, "save") : GameSettings.Folder;
        public static string FilePath => Path.Combine(Folder, "career.json");
        public static bool Exists => File.Exists(FilePath);

        public static int CheckSystemSeed;   // Fix 3.2, checks only: a new career with this system seed (0 = random)

        public static Career New()
        {
            int seed = CheckSystemSeed != 0 ? CheckSystemSeed : Seeds.NewRandom();
            C = new Career { systemSeed = seed, systemName = Names.StarSystem(seed), credits = Tuning.T.startingCredits, started = DateTime.Now.ToString("yyyy-MM-dd HH:mm") };
            C.Save();
            return C;
        }

        public static Career Load()
        {
            try { if (Exists) C = JsonUtility.FromJson<Career>(File.ReadAllText(FilePath)); }
            catch (Exception e) { Debug.LogWarning("Career save unreadable: " + e.Message); C = null; }
            return C;
        }

        public void Save()
        {
            lastSaved = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            try { Directory.CreateDirectory(Folder); File.WriteAllText(FilePath + ".tmp", JsonUtility.ToJson(this, true)); File.Copy(FilePath + ".tmp", FilePath, true); File.Delete(FilePath + ".tmp"); }
            catch (Exception e) { Debug.LogWarning("Career not saved: " + e.Message); }
        }

        // A planet keeps its state between visits; the first visit fixes its pilot count from the career's tier.
        public PlanetState Planet(int seed)
        {
            foreach (var p in planets) if (p.seed == seed)
                {
                    if (p.alien == null || p.alien.Length != p.pilots.Length) Aliens.Assign(p, planets.IndexOf(p) == 0);   // a planet saved before Round 3
                    if (!p.biomeSet) { p.biome = Biomes.Keys[(int)(planets.IndexOf(p) == 0 ? Biome.Canyons : Biomes.ForSeed(p.seed))]; p.biomeSet = true; }   // Round 4
                    return p;
                }
            var t = Tuning.T; int tier = stats.planetsCleared;
            int n = Mathf.Min(t.pilotsMax, t.pilotsFirstPlanet + tier * t.pilotsPerTier);
            var ps = new PlanetState { seed = seed, name = Names.Planet(seed), tier = tier, pilots = new int[n], biomeSet = true,
                                       biome = Biomes.Keys[(int)(planets.Count == 0 ? Biome.Canyons : Biomes.ForSeed(seed))] };   // Round 4: a career's first planet is Canyons
            Aliens.Assign(ps, planets.Count == 0);   // Round 3: a new career's first planet has no aliens
            planets.Add(ps);
            return ps;
        }

        // Fix 3.2: the planet a Continue arrives at. A new day's planet of the day comes first (once); otherwise where you left off.
        public int ArrivalSeed()
        {
            int today = Seeds.OfTheDay;
            if (!planets.Exists(p => p.seed == today)) return today;
            return currentSeed != 0 && planets.Exists(p => p.seed == currentSeed) ? currentSeed : today;
        }

        // Fix 3.2: the next new planet of the home system, from the career's seed and its counter alone, so a career's planets are
        // always the same ones in the same order. Seeds 1 - 19 999 999: never a date (the planets of the day are 2026xxxx).
        public static int MadePlanetSeed(int systemSeed, int n)
        {
            var r = new SeedRng(systemSeed, 1000 + n);
            return 1 + (int)(r.Next() % 19999999u);
        }
        public int NextPlanetSeed()
        {
            while (true)
            {
                int seed = MadePlanetSeed(systemSeed, planetsMade++);
                if (!planets.Exists(p => p.seed == seed)) return seed;   // (only an earlier made planet can share it: still the same order)
            }
        }

        // Round 3, item B10: the Field Journal. A meeting that revealed the type (it killed you, you killed it, the pilots or security
        // killed it) adds an encounter; the first gives the entry and a vague hint, the second a clearer one.
        public JournalEntry Meet(AlienType type)
        {
            var e = journal.Find(j => j.type == (int)type);
            if (e == null) { e = new JournalEntry { type = (int)type }; journal.Add(e); }
            e.encounters++; return e;
        }

        public int ShipValue => Tuning.T.shipValue;   // + the upgrades' value once the shop exists (Round 5)
        public int RecoveryFee => Tuning.T.recoveryBase + Mathf.RoundToInt(Tuning.T.recoveryShareOfShipValue * ShipValue);
        public int RepairCost => Mathf.CeilToInt(Current.damage * Tuning.T.repairPricePerPercent);
        public int RefuelCost => Mathf.CeilToInt((100 - Current.energy) * Tuning.T.fuelPricePerPercent);

        public void Earn(int cr) { credits += cr; stats.creditsEarned += cr; }
        public int Spend(int cr) { int paid = Mathf.Min(cr, credits); credits -= paid; stats.creditsSpent += paid; return paid; }   // never below zero

        // Refuel as far as the credits go; if that leaves less than the free minimum fill, the corps tops it up for free.
        public string Refuel()
        {
            var s = Current; float want = 100 - s.energy; if (want <= .01f) return "";
            float price = Tuning.T.fuelPricePerPercent;
            float afford = Mathf.Min(want, credits / price);
            int paid = Spend(Mathf.CeilToInt(afford * price));
            s.energy = Mathf.Min(100, s.energy + afford);
            string r = $"Refuelled {afford:0}%: -{paid} cr";
            if (s.energy < Tuning.T.freeMinimumFill - .01f) { r += $"; the corps gave a free fill to {Tuning.T.freeMinimumFill:0}%"; s.energy = Tuning.T.freeMinimumFill; }
            return r;
        }

        public string Repair()
        {
            var s = Current; if (s.damage <= .01f) return "";
            float fix = Mathf.Min(s.damage, credits / Tuning.T.repairPricePerPercent);
            int paid = Spend(Mathf.CeilToInt(fix * Tuning.T.repairPricePerPercent));
            s.damage -= fix; if (s.damage < .01f) s.damage = 0;
            return $"Repaired {fix:0}% of hull: -{paid} cr" + (s.damage > 0 ? $" (not enough credits: {s.damage:0}% left)" : "");
        }
    }
}
