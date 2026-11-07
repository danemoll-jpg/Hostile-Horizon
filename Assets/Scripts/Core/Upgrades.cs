using System.Collections.Generic;
using UnityEngine;

namespace HostileHorizon
{
    // Round 6 (C): the upgrade shop's categories, prices and effects (all numbers in the tuning file). Levels are stored in the ship's
    // save (ShipState.upgrades: one key per level bought, e.g. "engine", "engine" = Engine level 2), so the corps loaner has none.
    public enum Upg { Engine, Hull, Shields, Energy, Radar, Cabin, Gun }

    public static class Upgrades
    {
        public static readonly string[] Keys = { "engine", "hull", "shields", "energy", "radar", "cabin", "gun" };
        public static readonly string[] Names = { "Engine", "Hull", "Shields", "Energy", "Radar", "Cabin", "Gun" };
        public static int Count => Keys.Length;

        public static int[] Prices(Upg u)
        {
            var t = Tuning.T;
            switch (u)
            {
                case Upg.Engine: return t.upgEngine; case Upg.Hull: return t.upgHull; case Upg.Shields: return t.upgShields;
                case Upg.Energy: return t.upgEnergy; case Upg.Radar: return t.upgRadar; case Upg.Cabin: return t.upgCabin; default: return t.upgGun;
            }
        }
        public static int Max(Upg u) => Prices(u).Length;
        public static int Level(ShipState s, Upg u) { if (s == null) return 0; int n = 0; foreach (var k in s.upgrades) if (k == Keys[(int)u]) n++; return Mathf.Min(n, Max(u)); }
        public static int Price(ShipState s, Upg u) { int l = Level(s, u); var p = Prices(u); return l < p.Length ? p[l] : 0; }
        // the value the upgrades add to the ship (the recovery fee is a share of it)
        public static int Value(ShipState s) { int v = 0; for (int u = 0; u < Count; u++) { var p = Prices((Upg)u); for (int l = 0; l < Level(s, (Upg)u); l++) v += p[l]; } return v; }

        // the effects at a level
        public static float SpeedFactor(int l) => 1 + Tuning.T.upgEngineStep * l;            // top speed, acceleration, climb
        public static float ArmourFactor(int l) => 1 / (1 + Tuning.T.upgHullStep * l);       // damage taken (= max hull 100 / 125 / 150 / 175 %)
        public static float HullPercent(int l) => 100 * (1 + Tuning.T.upgHullStep * l);
        public static float ShieldDrain(int l) => Tuning.T.shieldDrain * (1 - Tuning.T.upgShieldsStep * l);
        public static float ShieldRadius(int l) => 8f + Tuning.T.upgShieldsRadius * l;      // m: bolts caught further out
        public static float DrainFactor(int l) => 1 / (1 + Tuning.T.upgEnergyStep * l);       // flight / hover drain (= capacity +20 % a level)
        public static float RadarFactor(int l) => 1 + Tuning.T.upgRadarStep * l;             // enemies' radar reach
        public static float ExactRange(int l) => 3000f + Tuning.T.upgRadarExact * l;         // m: exact bearings out to here (else rounded to 30°)
        public static int CabinSize(int l) => Tuning.T.cabinSize + Tuning.T.upgCabinStep * l;
        public static float GunRate(int l) => Tuning.T.gunRate * (1 + Tuning.T.upgGunRate * l);
        public static float GunDamage(int l) => 1 + Tuning.T.upgGunDamage * l;

        public static string Effect(Upg u, int l)
        {
            var t = Tuning.T;
            switch (u)
            {
                case Upg.Engine: return $"speed, acceleration, climb +{(SpeedFactor(l) - 1) * 100:0} % (top {ShipController.MaxSpeed * SpeedFactor(l) * 3.6f:0} km/h)";
                case Upg.Hull: return $"hull {HullPercent(l):0} % (hits do {ArmourFactor(l) * 100:0} % damage)";
                case Upg.Shields: return $"shield drain {ShieldDrain(l):0.0} %/s, catches bolts {ShieldRadius(l):0} m out";
                case Upg.Energy: return $"capacity +{(1 / DrainFactor(l) - 1) * 100:0} % (flight drain x{DrainFactor(l):0.00})";
                case Upg.Radar: return $"enemies on the radar to {t.radarTurretRange * RadarFactor(l) / 1000f:0.0} / {t.radarSaucerRange * RadarFactor(l) / 1000f:0.0} km, exact bearings to {ExactRange(l) / 1000f:0} km";
                case Upg.Cabin: return $"{CabinSize(l)} seats";
                default: return $"{GunRate(l):0.0} shots/s, damage x{GunDamage(l):0.00}";
            }
        }
    }
}
