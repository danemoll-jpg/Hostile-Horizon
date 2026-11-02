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
