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
        public int alienRoll;         // Fix 9.1 (A1): 1 = rolled per pilot at the new share; 0 = a 0.9.0 planet (its waiting pilots are rolled again once)
        public string biome = "canyons";   // Round 4 (A1): Biomes.Keys; from the seed (a career's first planet: always canyons)
        public bool biomeSet;              // Round 4: false in a save from before Round 4 (its biome is worked out on the next visit)
        public string lastVisit;           // Fix 3.2: for the planet list's order (most recent first)
        public List<int> enemiesDown = new List<int>();   // Round 5: enemies destroyed here (turret i = i, saucer i = 1000 + i)
        public string enemiesKey = "";                    // Round 5: the difficulty's enemies they belong to (a change: other enemies)
        public int turrets = -1, saucers = -1;            // Round 6 (D1): its defences as placed (-1: not visited since Round 6)
        public string distressCheck;                      // Round 6 (D3): when its distress-call roll last ran (real time)
        public int distressRolls, distressNew;            // rolls made; pilots the last call added
        public bool distress, distressSaid;               // new pilots down since your last visit; Control has announced it
        public List<Mission> missions = new List<Mission>(); public bool missionsSet;   // Round 9 (B1): side missions (Missions.cs)
        // Round 9 (E1): best runs. A "run" is everything from one clear (or the first visit) to the next clear: flight time, credits
        // earned here, hull hits, deaths. At each clear the bests are kept and the run starts again (distress calls reopen a planet).
        public float runSeconds; public int runCredits, runHits, runDeaths;
        public float bestClear; public int bestCredits, bestHits = -1, clears; public bool clearedNoDeath;
        public bool OfTheDay => seed == Seeds.OfTheDay;
        public int Count(PilotFate f) { int n = 0; foreach (var p in pilots) if (p == (int)f) n++; return n; }
    }

    [Serializable] public sealed class CareerStats
    {
        public int pilotsRescued, pilotsLost, creditsEarned, creditsSpent, deaths, planetsCleared, sorties, tows, recalls;
        public int aliensKilled;      // Round 3
        public int turretsDestroyed, saucersDestroyed;   // Round 5
        public float flightSeconds; public int hullHits, cleanClears, missionsDone, salvages, insuredFlights;   // Round 9 (E1): totals
    }

    [Serializable] public sealed class JournalEntry
    {
        public int type; public int encounters;
        public int killed, killedYou; public string firstPlanet, firstSeen;   // Round 6 (E2): your record with it
    }

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
        public DifficultySetting difficulty = new DifficultySetting();   // Round 5 (B8): a save from before Round 5 is Normal
        // Round 9 (C1, D1): your own ship's wreck (where it went down) and insurance
        public int wreckSeed; public float wreckX, wreckY, wreckZ, wreckHeading;
        public bool salvaged;        // hooked up by the mothership's salvage line: back at the next dock
        public bool insured;         // this flight is insured (bought in the hangar; ends at the dock or with the ship)
        public bool wreckInsured;    // the wreck went down insured: its recovery costs insuredRecoveryShare of the fee
        public static PlanetState EarnPlanet;   // Round 9 (E1): credits earned count toward this planet's run (GameShell sets it)

        public static Career C { get; private set; }
        public ShipState Current => flyingLoaner ? loaner : ship;

        public static string Folder => CheckRun.Active ? Path.Combine(CheckRun.Dir, "save") : GameSettings.Folder;
        public static string FilePath => Path.Combine(Folder, "career.json");
        public static bool Exists => File.Exists(FilePath);

        public static int CheckSystemSeed;   // Fix 3.2, checks only: a new career with this system seed (0 = random)
        public static double CheckHoursAhead;   // Round 6 (D4), checks only: the clock moved on (a simulated wait)
        public static DateTime Now => DateTime.Now.AddHours(CheckHoursAhead);

        public static Career New()
        {
            int seed = CheckSystemSeed != 0 ? CheckSystemSeed : Seeds.NewRandom();
            C = new Career { systemSeed = seed, systemName = Names.StarSystem(seed), credits = Tuning.T.startingCredits, started = DateTime.Now.ToString("yyyy-MM-dd HH:mm"), difficulty = GameSettings.S.difficulty.Copy() };
            C.Save();
            return C;
        }

        public static Career Load()
        {
            try { if (Exists) C = JsonUtility.FromJson<Career>(File.ReadAllText(FilePath)); }
            catch (Exception e) { Debug.LogWarning("Career save unreadable: " + e.Message); C = null; }
            return C;
        }

        // Round 11 (C1): training flies a throwaway career kept in memory: never saved, and the real one is back after it
        public static bool Training { get; private set; }
        static Career beforeTraining;
        public static void BeginTraining()
        {
            if (!Training) beforeTraining = C;
            Training = true;
            C = new Career { systemSeed = HostileHorizon.Training.PracticeSeed, systemName = "Practice", credits = Tuning.T.startingCredits, started = "training", difficulty = new DifficultySetting() };
        }
        public static void EndTraining() { if (!Training) return; C = beforeTraining; beforeTraining = null; Training = false; }

        public void Save()
        {
            if (Training) return;   // Round 11: nothing from training reaches career.json
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
                    else if (p.alienRoll < 1)
                    {
                        // Fix 9.1 (A1): a planet from 0.9.0 (at least one alien each): its pilots still waiting get the new per-pilot roll
                        var fresh = new PlanetState { seed = p.seed, tier = p.tier, pilots = new int[p.pilots.Length] }; Aliens.Assign(fresh, planets.IndexOf(p) == 0);
                        for (int i = 0; i < p.pilots.Length; i++) if (p.pilots[i] == (int)PilotFate.Waiting) { p.alien[i] = fresh.alien[i]; p.quirk[i] = fresh.quirk[i]; }
                    }
                    p.alienRoll = 1;
                    if (!p.biomeSet) { p.biome = Biomes.Keys[(int)(planets.IndexOf(p) == 0 ? Biome.Canyons : Biomes.ForSeed(p.seed))]; p.biomeSet = true; }   // Round 4
                    return p;
                }
            var t = Tuning.T; int tier = stats.planetsCleared;
            int n = Mathf.Min(t.pilotsMax, t.pilotsFirstPlanet + tier * t.pilotsPerTier);
            var ps = new PlanetState { seed = seed, name = Names.Planet(seed), tier = tier, pilots = new int[n], biomeSet = true, alienRoll = 1,
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
        public JournalEntry Meet(AlienType type, string planet = null, bool itKilledYou = false)
        {
            var e = journal.Find(j => j.type == (int)type);
            if (e == null) { e = new JournalEntry { type = (int)type, firstPlanet = planet, firstSeen = DateTime.Now.ToString("yyyy-MM-dd") }; journal.Add(e); }
            e.encounters++; if (itKilledYou) e.killedYou++; else e.killed++;
            return e;
        }

        // Round 6 (D3): new distress calls. A visited, cleared planet (not the one you're over) gets a roll every distressEveryHours of
        // real time; a hit puts 1-3 new pilots down there (aliens among them by the usual rules). Returns the planets that got one.
        public List<PlanetState> CheckDistress(int here)
        {
            var t = Tuning.T; var now = Now; var hit = new List<PlanetState>();
            foreach (var p in planets)
            {
                if (p.seed == here || !p.cleared || p.visits == 0) { if (p.seed == here) p.distressCheck = null; continue; }
                if (string.IsNullOrEmpty(p.distressCheck) || !DateTime.TryParse(p.distressCheck, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var last)) { p.distressCheck = now.ToString("s"); continue; }
                int rolls = 0;
                while ((now - last).TotalHours >= t.distressEveryHours && rolls < 48 && p.cleared)
                {
                    last = last.AddHours(t.distressEveryHours); rolls++;
                    var r = new SeedRng(p.seed ^ (p.distressRolls * 7349), 71); p.distressRolls++;
                    if (r.Value >= t.distressChance) continue;
                    int n = Mathf.Min(t.distressMin + r.Next(t.distressMax - t.distressMin + 1), t.distressPilotCap - p.pilots.Length);
                    if (n <= 0) continue;
                    int from = p.pilots.Length;
                    Array.Resize(ref p.pilots, from + n); Array.Resize(ref p.alien, from + n); Array.Resize(ref p.quirk, from + n);
                    Aliens.AssignExtra(p, from, r);
                    p.cleared = false; p.distress = true; p.distressSaid = false; p.distressNew = n; hit.Add(p);
                }
                p.distressCheck = last.ToString("s");
            }
            return hit;
        }

        public int ShipValue => Tuning.T.shipValue + Upgrades.Value(ship);   // Round 6 (C2): the upgrades add their price (the loaner is free)
        public int FullRecoveryFee => Tuning.T.recoveryBase + Mathf.RoundToInt(Tuning.T.recoveryShareOfShipValue * ShipValue);
        public int RecoveryFee => wreckInsured && ship.wrecked ? Mathf.RoundToInt(FullRecoveryFee * Tuning.T.insuredRecoveryShare) : FullRecoveryFee;   // Round 9 (D1)
        public int InsuranceFee => Mathf.RoundToInt(Tuning.T.insuranceShareOfShipValue * ShipValue);
        public int RepairCost => Mathf.CeilToInt(Current.damage * Tuning.T.repairPricePerPercent);
        public int RefuelCost => Mathf.CeilToInt((100 - Current.energy) * Tuning.T.fuelPricePerPercent);

        public void Earn(int cr) { credits += cr; stats.creditsEarned += cr; if (EarnPlanet != null) EarnPlanet.runCredits += cr; }
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
