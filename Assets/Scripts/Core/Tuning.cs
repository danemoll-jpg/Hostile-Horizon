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
        public float pickupQuietRange = 250;          // Round 7 (A1): the flight music drops out only landed / hovering this close to your pilot
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
        // Round 5 (A): self-righting with the stick released (Full / Light, Settings > Auto-level) and the level-out button
        public float rightDelay = .25f;                 // s of pitch and roll centred before it starts
        public float rightRollRate = 120, rightPitchRate = 60;   // degrees per second at most (Normal)
        public float rightGain = 3;                     // per second: the ease-out near level (no overshoot)
        public float rightSlowFactor = .65f, rightFastFactor = 1.5f;   // Settings > Righting speed Slow / Fast (Normal 1)
        public float rightFullFactor = 1.3f;            // Flight assist Full rights faster
        public float rightLevelOutFactor = 2f;          // the level-out button
        public float rightClimbBelow = 25, rightClimbPitch = 6;   // m over the ground: below it the nose settles in a gentle climb (degrees)
        public float hoverRightRate = 140;
        // Round 5 (B8): difficulty tables (a level index from Difficulty picks the number) and the presets (Easy, Normal, Hard, Nightmare)
        public float[] aggressionLevels = { .6f, 1f, 1.35f, 1.7f };    // enemy density, accuracy, fire rate, lock time: Low / Normal / High / Brutal
        public float[] alienShareLevels = { .6f, 1f, 1.25f, 1.5f };    // times the alien share (alienShareStart ... Max)
        public float[] tellLevels = { .95f, .75f, .60f, .50f };        // chance an alien shows its tell
        public float[] falseTellLevels = { 0f, .10f, .20f };           // chance a real pilot shows a tell-like quirk
        public float[] payLevels = { .8f, 1f, 1.25f, 1.5f };           // pilot pay, bonuses and bounties
        public float[] coveredPadShare = { 0f, 0f, .35f, .6f };        // B7: share of pilots' pads a turret can see, by aggression level
        public int[] presetAggression = { 0, 1, 2, 3 }, presetAlienShare = { 0, 1, 2, 3 }, presetTells = { 0, 1, 2, 3 }, presetFalseTells = { 0, 0, 1, 2 };
        public int[] presetCrash = { 2, 3, 3, 3 }, presetPay = { 0, 1, 2, 3 };
        // Round 5 (B1, B2, B9): enemies (Normal numbers; aggression scales count, fire rate, lock time and accuracy)
        public int turretsFirstPlanet = 3, turretsBase = 6, turretsPerTier = 2, turretsMax = 24;
        public float turretSpacing = 450, turretMinFromStart = 900, turretArea = 500;   // m; area: the pilots' farthest range + this
        public float turretRange = 1100, turretTurnRate = 70, turretLockTime = 1.2f, turretFireInterval = 1.8f;
        public float turretBoltSpeed = 230, turretLead = .9f, turretAimError = 14;       // lead: share of the full lead (.6 in the first check run: nothing ever hit); error: m at 1 km
        public float turretDamage = 7, turretHp = 4;
        public float saucersBase = 1, saucersPerTier = .5f; public int saucersMax = 6;
        public float saucerSpeed = 95, saucerPatrolSpeed = 45, saucerTurnRate = 55, saucerHeight = 140, saucerClearance = 35;
        public float saucerDetect = 1400, saucerHearLanded = 3000, saucerFireRange = 700, saucerBurstGap = .25f, saucerCooldown = 3f;
        public int saucerBurst = 3;
        public float saucerBoltSpeed = 260, saucerDamage = 5, saucerHp = 5;
        // B3, B4: your gun and shield
        public float gunRate = 6, gunEnergy = .12f, gunBoltSpeed = 700, gunRange = 1300;   // shots a second, % energy a shot, m/s, m
        // Fix 6.3: the sound mix. engineMix / windMix scale the engine hum and the wind (0.6.2: 1 / 1, and Effects 100 % instead of
        // the Engine slider); a voice line ducks engine and wind by voiceDuckEngine, music by voiceDuckMusic (to 1 - it) and effects by
        // voiceDuckEffects; voiceChime = the attention chime's level before a computer warning
        public float engineMix = .55f, windMix = .5f, voiceDuckEngine = .6f, voiceDuckMusic = .6f, voiceDuckEffects = .25f, voiceChime = .5f;
        public float ambienceMix = .5f;   // Round 8 (B2): the biome / regeneration-bay ambience (files at -24 LUFS) x Effects
        public float shieldDrain = 2f;                   // % energy a second while on (flight at full throttle uses .32)
        public int turretBounty = 150, saucerBounty = 300;
        public float boltHitRadius = 3.5f;               // m round the ship's centre              // degrees per second at most: the body righting when landing mode starts
        // Round 3: aliens ("Normal" values; difficulty presets come in Round 4)
        public float alienShareStart = .20f, alienSharePerTier = .05f, alienShareMax = .35f;   // share of a planet's pilots (none on a career's first planet)
        public float tellReliability = .75f;          // chance (per alien, from the seed) that it shows its tell
        public float falseTellChance = 0;             // chance a real pilot shows a tell-like quirk (off on Normal)
        public int alienBounty = 200;                 // credits for killing an alien with the exhaust
        public float canopyClimbAfter = 25;           // s an alien waits at a closed airlock before it climbs onto the canopy
        public float breakInAfter = 10;               // s on the canopy before it breaks in
        public float sleeperTurnMin = 40, sleeperTurnMax = 120;   // s of flight after boarding before the Sleeper turns (seeded)
        public int sleeperPilotsWin = 2;              // pilots left after its first kill that are enough to kill it
        // Round 6 (B): enemies easier to find (their accuracy, fire rate and aggression are unchanged)
        public float radarTurretRange = 3000, radarSaucerRange = 6000;   // m: on the radar without being spotted first
        public float bracketRange = 2500;                                // m: a bracket in the world round enemies in sight
        public float beaconScale = .008f, flashScale = .009f, boltGlowScale = .006f;   // m of size per m from the eye (readable at 1-2 km)
        // Round 6 (C): the upgrade shop. Prices per level (credits); the steps are what each level adds.
        public int[] upgEngine = { 6500, 10500, 16000 }, upgHull = { 6000, 9500, 14500 }, upgShields = { 6000, 9500, 14500 }, upgEnergy = { 6500, 10500, 16000 };
        public int[] upgRadar = { 5500, 8500, 13000 }, upgCabin = { 7000, 12000 }, upgGun = { 6000, 9500, 14500 };
        public float upgEngineStep = .08f;      // top speed, acceleration and climb +8 % a level
        public float upgHullStep = .25f;        // max hull +25 % a level (hits do 1 / 1.25, 1 / 1.5, 1 / 1.75 of the damage)
        public float upgShieldsStep = .2f, upgShieldsRadius = 1f;   // shield drain -20 % a level; catches bolts 1 m further out a level
        public float upgEnergyStep = .2f;       // capacity +20 % a level (flight and hover drain / 1.2, 1.4, 1.6)
        public float upgRadarStep = .3f, upgRadarExact = 1000;      // enemies' radar reach +30 % a level; exact bearings 1 km further a level
        public int upgCabinStep = 2;            // seats a level (6 -> 8 -> 10)
        public float upgGunRate = .2f, upgGunDamage = .25f;         // shots a second +20 %, damage +25 % a level
        // Round 6 (D3): new distress calls on cleared planets (real time)
        public float distressEveryHours = 3, distressChance = .35f;
        public int distressMin = 1, distressMax = 3, distressPilotCap = 14;
        // Round 6 (A): an alien let in boards as a pilot, then strikes (one kind per alien, from the seed; weights)
        public float strikeInstantWeight = .55f, strikeTakeoffWeight = .45f;   // Round 7 (A4): the slow burn is gone
        public float strikeInstantMin = 1, strikeInstantMax = 4;   // s after the airlock shuts
        public float strikeTakeoffMin = 2, strikeTakeoffMax = 6;   // s after lift-off, or sooner at the first hard turn
        public float strikeHardTurn = 70;                          // degrees per second of turning that counts as a hard turn
        // Round 7 (B2-B4): the new biomes
        public float waterHardSpeed = 12;                          // m/s down into the sea: more is a hit (x waterHitScale), less a skim
        public float waterHitScale = 3;
        public float lavaHeatHeight = 30;                          // under this over lava: HULL HEAT (warning only)
        public float canopyLeapChance = .5f, canopyLeapMin = 5, canopyLeapMax = 15;   // A3: leap (no warning) instead of the slow climb, s at the door
        public float herringFlicker = .12f, herringThump = .15f, herringCrackle = .12f;  // A4: chance per boarding (real pilots and aliens alike)

        // Round 9 (B): side missions. Rewards are times the difficulty's pay; the timed rescue's clock runs in flight on its planet.
        public int missionsMin = 1, missionsMax = 3;
        public int missionRecorderReward = 700, missionTurretsReward = 1000, missionTimedReward = 800, missionCleanReward = 900;
        public float missionRecorderHold = 8, missionRecorderRange = 70;    // s holding the airlock button; m from the crash site's centre
        public int missionNestMin = 3, missionNestMax = 5;                  // turrets in the nest
        public float missionNestRing = 380;                                 // m: the nest's turrets stand on the high ground this close to the valley
        public float missionTimedSeconds = 540;                             // s of flight time for the timed rescue
        // Round 9 (C1): recovering your own wreck in the loaner
        public float salvageHold = 12, salvageRange = 70;                   // s holding the airlock button, landed, engines off; m from the wreck
        public float salvageHullDamage = 40;                                // % hull damage your ship comes back with (Repair in the hangar)
        public float salvageAmbushChance = .4f, salvageAmbushDistance = 1100;   // chance a saucer is waiting near the wreck (enemies on), m away
        // Round 9 (D1): insurance, bought in the hangar for one flight
        public float insuranceShareOfShipValue = .04f;                      // the fee: 4 % of the ship's value (6000 + upgrades)
        public float insuredRecoveryShare = .2f;
        // Round 9 (F): the Free Flight time trial
        public float trialLength = 14000, trialLegMin = 1100, trialLegMax = 1900;   // m of course; legs between landing pads ahead
        public float trialGateSpacing = 550, trialGateFirst = 350, trialGateRadius = 24;   // m between gates; the first gate after the start; ring radius
        public float trialMissPenalty = 5;                                          // s added for a gate missed
        // Round 9 (G): tougher enemies on the harder settings, by aggression level (Low / Normal / High / Brutal = Easy / Normal / Hard /
        // Nightmare; Custom's aggression picks the same). Low and Normal are Round 5's numbers (unchanged). The aim error is still
        // divided by the aggression (as Round 5), the fire rate and lock time still scale with it.
        public float[] turretLeadLevels = { .9f, .9f, 1f, 1f };            // share of the full lead
        public int[] turretInterceptLevels = { 0, 0, 1, 1 };               // 1: the lead aims where the bolt meets the ship (Round 5: where the ship is now)
        public float[] turretAimErrorLevels = { 14, 14, 14, 14 };          // m at 1 km (before / aggression)
        public float[] boltHitRadiusLevels = { 3.5f, 3.5f, 3.5f, 3.5f };    // m round the ship's centre
        public int[] saucerHuntLevels = { 0, 0, 1, 1 };                    // 1: saucers not chasing search for the ship (fly toward where it is)
        public int[] saucerPairLevels = { 0, 0, 0, 1 };                    // 1: a saucer that finds you brings the nearest other one
        public float saucerHuntRadius = 600, saucerHuntEvery = 15;         // m round the ship they search; s between new search points                            // an insured wreck's recovery costs 20 % of the normal fee

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
