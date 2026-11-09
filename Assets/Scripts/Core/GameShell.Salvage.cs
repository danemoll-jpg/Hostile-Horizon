using System.Text;
using UnityEngine;

namespace HostileHorizon
{
    // Round 9 (C1): recover your own wreck. After a death the regeneration bay offers a third choice: recover it yourself (free): you
    // fly the loaner; on the wreck's planet the wreck is in the world, on the radar and the route trail; land by it, engines off, hold
    // the airlock button (salvageHold s) and the mothership's salvage line takes it; at the next dock your own ship (its upgrades
    // kept) is back, with salvageHullDamage % to repair, and the loaner goes back. A chance (salvageAmbushChance) that a saucer is
    // waiting near the wreck. Dying on the way: the wreck stays where it is.
    // Round 9 (D1): insurance for one flight, bought in the hangar (insuranceShareOfShipValue of the ship's value): a wreck that goes
    // down insured costs insuredRecoveryShare of the recovery fee. Pilots aboard are never covered.
    // Round 9 (E1): best runs per planet (fastest clear in flight time, most credits, fewest hull hits, cleared without dying), on
    // the star map; career totals.
    public sealed partial class GameShell
    {
        GameObject wreckMarker; float salvageHold; bool ambushRolled;
        public float SalvageHold => salvageHold;   // checks
        public bool SalvageHere { get { var c = Career.C; return c != null && InCampaign && c.flyingLoaner && c.ship.wrecked && !c.salvaged && Planet != null && c.wreckSeed == Planet.seed && c.wreckSeed != 0; } }
        public Vector3 WreckPos { get { var c = Career.C; float y = Terrain && Terrain.Field != null ? Terrain.Field.SurfaceHeight(c.wreckX, c.wreckZ) : c.wreckY; return new Vector3(c.wreckX, y, c.wreckZ); } }

        // your own ship as it lies: the ship's model, burnt dark, tilted where it hit, and a tall orange post over it
        void PlaceWreck()
        {
            if (wreckMarker) Destroy(wreckMarker);
            ambushRolled = false; salvageHold = 0;
            if (!SalvageHere) return;
            var c = Career.C; var at = WreckPos;
            wreckMarker = new GameObject("Your wreck"); wreckMarker.transform.SetPositionAndRotation(at + Vector3.up * .5f, Quaternion.Euler(9, c.wreckHeading, 14));
            var prefab = Resources.Load<GameObject>("Models/Ship");
            if (prefab)
            {
                var m = Instantiate(prefab, wreckMarker.transform); m.transform.localRotation = Quaternion.Euler(0, 180, 0);
                var burnt = new Material(litTemplate); burnt.SetColor("_BaseColor", new Color(.09f, .085f, .08f)); burnt.SetFloat("_Smoothness", .08f);
                foreach (var r in m.GetComponentsInChildren<Renderer>()) { var mats = r.sharedMaterials; for (int i = 0; i < mats.Length; i++) mats[i] = burnt; r.sharedMaterials = mats; }
            }
            var glow = new Material(Shader.Find("HostileHorizon/Glow")); glow.SetColor("_Color", new Color(1f, .55f, .2f) * 2f);
            var post = new GameObject("Wreck post").AddComponent<LineRenderer>(); post.sharedMaterial = glow; post.positionCount = 2; post.widthMultiplier = 1.2f;
            post.SetPosition(0, at + Vector3.up); post.SetPosition(1, at + Vector3.up * 90f); post.transform.SetParent(wreckMarker.transform, true);
            post.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; post.receiveShadows = false;
        }

        // in flight (CampaignTick, after the missions): the wreck on the radar and the display list; the salvage itself
        void SalvageTick(float dt)
        {
            var c = Career.C; var t = Tuning.T;
            if (Planet != null && Now == State.Flying) { Planet.runSeconds += dt; c.stats.flightSeconds += dt; }   // E1
            if (!SalvageHere) return;
            var at = WreckPos; float d = Flat(at - Ship.transform.position).magnitude;
            Displays.Contacts.Add(new RadarContact { position = at, mark = "WRECK" });
            Displays.Objectives = ($"◆ YOUR WRECK: land by it · {RadarScope.Distance(d)}\n" + Displays.Objectives).Trim();
            bool there = Ship.Landed && d < t.salvageRange;
            if (there && !ambushRolled)
            {
                ambushRolled = true;
                bool ambush = Difficulty.Enemies == 2 && new SeedRng(c.wreckSeed ^ (c.stats.deaths * 7919), 77).Value < t.salvageAmbushChance;
                PlayLog.Add($"salvage: landed at your wreck {d:0} m away; {(ambush ? "something is waiting: a saucer" : "nothing waiting")}");
                if (ambush)
                {
                    var dir = Quaternion.Euler(0, c.wreckHeading + 120f, 0) * Vector3.forward;
                    var home = at + dir * t.salvageAmbushDistance; home.y = Terrain.Field.SurfaceHeight(home.x, home.z) + t.saucerHeight;
                    Foes.CheckSaucer(home); Voice.Say("ctl_salvage_ambush", VoiceKind.Radio, 2);
                }
            }
            if (there && !Ship.EnginesOn && Controls.AirlockHeld && Ship.InputEnabled)
            {
                salvageHold += dt;
                Flash($"HOOKING UP YOUR WRECK  {salvageHold:0} / {t.salvageHold:0} s  (keep holding {Controls.AirlockKey})", .3f);
                if (salvageHold >= t.salvageHold)
                {
                    c.salvaged = true; salvageHold = 0; if (wreckMarker) Destroy(wreckMarker);
                    Voice.Say("ctl_salvage_done", VoiceKind.Radio, 2); Sfx.Play(Sfx.Thunk, Ship.transform.position, .8f, 8f);
                    Flash("Salvage line on: your ship will be in the bay when you dock", 6f); PlayLog.Add("salvage: your wreck hooked up");
                    c.Save();
                }
            }
            else
            {
                salvageHold = 0;
                if (there && Ship.InputEnabled && Time.time >= flashUntil) Flash(Ship.EnginesOn ? $"At your wreck: engines off ({Controls.EngineKey}), then hold {Controls.AirlockKey}" : $"At your wreck: hold {Controls.AirlockKey} to hook it up", .5f);
            }
        }

        // the route trail's auto target while the wreck waits to be salvaged
        bool RouteToWreck => SalvageHere && routePick == -1;

        // at the dock (after the refuel): your own ship back from the salvage; the flight's insurance ends
        void SettleSalvage(StringBuilder sb)
        {
            var c = Career.C; var t = Tuning.T;
            if (c.insured) { c.insured = false; sb.AppendLine("Insurance for that flight ended (not needed)."); }
            if (!c.salvaged || !c.flyingLoaner) return;
            c.salvaged = false; c.ship.wrecked = false; c.ship.damage = t.salvageHullDamage; c.ship.energy = Mathf.Max(c.ship.energy, t.freeMinimumFill);
            c.flyingLoaner = false; c.loaner = new ShipState(); c.wreckInsured = false; c.wreckSeed = 0; c.stats.salvages++;
            Extras.SetLook(ShipExtras.Look.Own); Ship.SetEnergyAndDamage(c.Current.energy, c.Current.damage); ApplyUpgrades();
            sb.AppendLine($"<color=#9CFFB0>Your own ship is back in the bay (upgrades kept), the loaner returned. Hull {100 - c.ship.damage:0} %: Repair below.</color>");
            PlayLog.Add($"salvage: your own ship back at the dock, hull {100 - c.ship.damage:0} %");
        }

        // your own ship goes down: where its wreck lies; the insurance (if bought) is what the recovery will cost
        void WreckOnDeath()
        {
            var c = Career.C;
            if (c.flyingLoaner) { if (c.salvaged) PlayLog.Add("salvage: the loaner went down with your wreck hooked up: the wreck stays where it was"); c.salvaged = false; return; }
            var p = Ship.transform.position;
            c.wreckSeed = Planet.seed; c.wreckX = p.x; c.wreckY = p.y; c.wreckZ = p.z; c.wreckHeading = Ship.Heading;
            c.wreckInsured = c.insured; c.insured = false; c.salvaged = false;
            if (Planet != null) Planet.runDeaths++;
        }

        // D1: the hangar's insurance row
        void ToggleInsurance()
        {
            var c = Career.C; int fee = c.InsuranceFee;
            if (c.insured) { c.credits += fee; c.stats.creditsSpent -= fee; c.insured = false; debrief = $"Insurance cancelled: +{fee} cr back."; }
            else if (c.credits >= fee) { c.Spend(fee); c.insured = true; debrief = $"This flight is insured: -{fee} cr. If your ship goes down, recovery costs {Mathf.RoundToInt(c.FullRecoveryFee * Tuning.T.insuredRecoveryShare)} cr instead of {c.FullRecoveryFee} cr. Pilots aboard are never covered."; }
            c.Save();
        }

        string InsuranceLine()
        {
            var c = Career.C; if (c.flyingLoaner || c.ship.wrecked) return "";
            int full = c.FullRecoveryFee, cut = Mathf.RoundToInt(full * Tuning.T.insuredRecoveryShare);
            return c.insured ? $"<color=#9CFFB0>Insured for the next flight: recovery {cut} cr instead of {full} cr (never the pilots aboard)</color>\n"
                             : $"Not insured: recovery after a crash {full} cr (Insure: {c.InsuranceFee} cr, then {cut} cr)\n";
        }

        string WreckLine()
        {
            var c = Career.C; if (!c.ship.wrecked) return "";
            var on = c.planets.Find(p => p.seed == c.wreckSeed);
            string where = on != null ? on.name : "the planet where it went down";
            return c.salvaged ? $"Your wreck is on the salvage line: back at the next dock.\n" :
                $"Your own ship is a wreck on {where}: recovery {c.RecoveryFee} cr{(c.wreckInsured ? " (insured)" : "")}, or fly the loaner down, land by it and hold {Controls.AirlockKey} {Tuning.T.salvageHold:0} s (free){(Planet != null && Planet.seed != c.wreckSeed ? $" (go to {where} first: Star map)" : "")}.\n";
        }

        // E1: hull hits and the clear
        void NoteHullHit() { if (!InCampaign || Planet == null) return; Planet.runHits++; Career.C.stats.hullHits++; }

        void RecordBest(StringBuilder sb)
        {
            var p = Planet; var c = Career.C;
            bool faster = p.bestClear <= 0 || p.runSeconds < p.bestClear, richer = p.runCredits > p.bestCredits, cleaner = p.bestHits < 0 || p.runHits < p.bestHits, noDeath = p.runDeaths == 0;
            if (faster) p.bestClear = p.runSeconds; if (richer) p.bestCredits = p.runCredits; if (cleaner) p.bestHits = p.runHits;
            if (noDeath) { if (!p.clearedNoDeath) c.stats.cleanClears++; p.clearedNoDeath = true; }
            p.clears++;
            string run = $"this run: {Missions.Clock(p.runSeconds)} of flight, {p.runCredits} cr, {p.runHits} hull hit{(p.runHits == 1 ? "" : "s")}, {(noDeath ? "no deaths" : $"{p.runDeaths} death{(p.runDeaths == 1 ? "" : "s")}")}";
            var news = new System.Collections.Generic.List<string>(); if (faster) news.Add("fastest clear"); if (richer) news.Add("most credits"); if (cleaner) news.Add("fewest hits");
            sb.AppendLine($"<color=#FFE08A>Best run: {run}{(news.Count > 0 && p.clears > 1 ? $" (new best: {string.Join(", ", news)})" : "")}</color>");
            PlayLog.Add($"planet cleared: {run}; bests {BestText(p)}");
            p.runSeconds = 0; p.runCredits = 0; p.runHits = 0; p.runDeaths = 0;
        }

        static string BestText(PlanetState p)
        {
            if (p == null || p.clears == 0) return "";
            return $"fastest clear {Missions.Clock(p.bestClear)} · most {p.bestCredits} cr · fewest hits {p.bestHits} · {(p.clearedNoDeath ? "cleared without dying ✓" : "not yet without dying")}";
        }
    }
}
