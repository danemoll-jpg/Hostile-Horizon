using System;
using UnityEngine;

namespace HostileHorizon
{
    // Round 2, item 11: every Campaign number in one file, Assets/Resources/Tuning/CampaignTuning.json (built into the game).
    // The defaults below are the same numbers, used if the file is missing or a field is left out. Money is in credits (cr),
    // energy and hull in percent, distances in metres, speeds in metres per second, drains in percent per second.
    [Serializable]
    public sealed class Tuning
    {
        public int startingCredits = 3000;
        public int pilotPay = 500;                    // per pilot delivered to the mothership
        public int clearBonus = 1000;                 // once per planet, when no pilots are left to rescue
        public float dayBonus = .5f;                  // Fix 3.2: pilots rescued on the planet of the day pay this much more (+50 %)
        public int jumpCost = 0;                      // Fix 3.2: the jump to a new planet (free for now)
        public float fuelPricePerPercent = 6;         // refuelling at the mothership
        public float freeMinimumFill = 35;            // the corps fills up to this for free if you can't pay
        public float repairPricePerPercent = 12;      // hull repair
        public int towFee = 1500;                     // mothership tow when out of energy
        public int recoveryBase = 1500;               // recovery fee = base + share x ship value
        public float recoveryShareOfShipValue = .25f;
        public int shipValue = 6000;                  // the basic ship (upgrades add to it in Round 5)
        public float exhaustKillRange = 25;           // engines started with a pilot this close kills them
        public int exhaustKillPenalty = 400;
        public int cabinSize = 6;
        public int pilotsFirstPlanet = 3;
        public int pilotsPerTier = 1;                 // tier = planets cleared in the career when the planet is first visited
        public int pilotsMax = 10;
        public float pilotRangeMin = 900, pilotRangeMax = 3000, pilotRangePerTier = 500, pilotSpacing = 700;
        public float pickupRange = 160;               // a pilot runs to a ship landed (engines off) this close
        public float pilotRunSpeed = 5.5f;
        public float pilotMaxClimb = 30;              // Fix 2.5: m of height between pilot and door they can manage (no canyon walls)
        public float energyDrainFlightBase = .10f, energyDrainFlightThrottle = .22f, energyDrainHover = .35f;
        public float energyWarning = 15;
        public float hoverMaxSpeed = 18, hoverClimbRate = 14, hoverTurnRate = 50, hoverBrake = 12;
        public float safeTouchdownSpeed = 3.5f, maxLandingSlope = 14, powerlessSinkRate = 9;
        public float mothershipAltitude = 1500, dockingHeight = 1000;
        public float dockAnyTimeBelowEnergy = 25;     // with no pilots aboard you can still dock when this low on energy
        // Fix 2.3: getting home
        public int recallFee = 900;                   // calling the mothership to you (the tow when out of energy stays towFee)
        public float recallHoldSeconds = 1;           // hold the recall button this long (a stray press costs nothing)
        public float homeClimbRate = 30;              // m/s assumed for the climb to the docking height (flight mode, full throttle)
        public float homeMargin = 3;                  // % added to the energy that climb needs
        public float returnNowWarning = 8;            // RETURN NOW shows when the energy left is within this many % of what's needed
        public float beamReach = 600;                 // m, level distance at which the mothership's tow beam can catch the ship
        public float pilotFuelMin = 3, pilotFuelMax = 15;   // % energy a boarding pilot brings (random each time, not seeded)
        // Fix 2.4: flying feel (all modes). Rates in degrees per second at full stick, times Settings > Stick sensitivity.
        // Flight assist (Settings): Full = 0.2.3's bank turn and wing levelling, Light (default) = lighter, Off = none.
        public float pitchRate = 82, rollRate = 150, yawRate = 44;   // Fix 2.5: midway between 0.2.3 (70 / 130 / 32) and 0.2.4 (95 / 170 / 55)
        public float bankTurnFull = 38, levelRateFull = 22;     // bank turn: degrees per second of heading at 90 degrees of bank
        public float bankTurnLight = 26, levelRateLight = 8;    // wing levelling: degrees per second back to level, stick centred
                                                                 // Fix 2.5: on Light the bank turn only while pulling, and no levelling
        public float stickCurve = .6f;   // Fix 2.5: .6 (0.2.4: .35)                         // 0 straight .. 1 cubic: small stick moves stay fine, full stick is full
        // Fix 2.4: landing. Air brakes (X from flight): top speed to a hover in about 3 s. Hover descent faster high up.
        public float airBrake = 42;                             // m/s2 while faster than the hover speed
        public float hoverVerticalAccel = 15;                   // m/s2 to reach the climb / sink rate asked for
        public float hoverSinkHigh = 30, hoverSinkHighAbove = 150;   // m/s full down, at or above this height over the ground (m)
        public float hoverSinkMid = 14, hoverSinkMidAbove = 60;
        public float hoverSinkLow = 3, hoverSinkLowBelow = 8;        // a safe touchdown speed below this height
        public float stopMarkerRange = 2500;                    // m: the stop-point marker shows with a waiting pilot this close
        public float brakeCueLead = 40;                         // m: "SLOW DOWN - PRESS X" from this far before the stop point reaches the pilot's pad
                                                                 // Fix 2.5: 40 m and the pad (0.2.4: 150 m before the pilot: on the canyon wall)
        public float resetHeight = 60;                          // m: Reset position hovers this high over the nearest landing pad
        // Fix 2.5: the beam lift (tow, recall, mid-air catch) and docking, about 3x quicker than 0.2.4 (whose numbers are in brackets).
        public float motherComeSpeed = 420, motherComeAccel = 36;   // the mothership coming to you (140, 12)
        public float beamLiftSpeed = 320, beamLiftAccel = 100;       // the beam lift to under the bay (45, 4; braking 2.5)
        public float dockApproachSpeed = 200, dockApproachAccel = 45;   // docking: round to behind the bay (90, 15)
        public float dockFinalSpeed = 150, dockFinalAccel = 45;     // docking: to under the bay (70, 12; braking 4)
        public float bayRiseFrom = 25, bayRiseSpeed = 25, bayRiseBrake = 12;   // the last rise into the bay (from 60 m, 7 m/s, braking 1.2)
        public float towBeamDelay = .5f;                            // s between the beam catching and the lift (1.5)
        public float fastForward = 4;                               // Fix 2.5: hold A / Space / HOTAS trigger: the automatic parts play this much faster (live, no cut)
        // Fix 2.5: the launch warning: Launch asks first below this energy (%)
        public float launchEnergyWarning = 50;
        // Fix 2.5: gamepad / Steam Deck sticks: at least this centre deadzone (Deck sticks drift); the Settings deadzone if larger
        public float gamepadDeadzone = .12f;
        // Round 3: aliens ("Normal" values; difficulty presets come in Round 4)
        public float alienShareStart = .20f, alienSharePerTier = .05f, alienShareMax = .35f;   // share of a planet's pilots (none on a career's first planet)
        public float tellReliability = .75f;          // chance (per alien, from the seed) that it shows its tell
        public float falseTellChance = 0;             // chance a real pilot shows a tell-like quirk (off on Normal)
        public int alienBounty = 200;                 // credits for killing an alien with the exhaust
        public float canopyClimbAfter = 25;           // s an alien waits at a closed airlock before it climbs onto the canopy
        public float breakInAfter = 10;               // s on the canopy before it breaks in
        public float sleeperTurnMin = 40, sleeperTurnMax = 120;   // s of flight after boarding before the Sleeper turns (seeded)
        public int sleeperPilotsWin = 2;              // pilots left after its first kill that are enough to kill it

        static Tuning t;
        public static Tuning T
        {
            get
            {
                if (t != null) return t;
                t = new Tuning();
                var a = Resources.Load<TextAsset>("Tuning/CampaignTuning");
                if (a) try { JsonUtility.FromJsonOverwrite(a.text, t); } catch (Exception e) { Debug.LogWarning("Tuning file unreadable: " + e.Message); }
                return t;
            }
        }
    }
}
