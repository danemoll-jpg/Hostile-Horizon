using System.Text;
using UnityEngine;

namespace HostileHorizon
{
    // Round 6 (C): the upgrade shop in the hangar and the upgrades' effects on the ship you fly (the loaner has none).
    public sealed partial class GameShell
    {
        MenuPanel.Page shopPage;
        public int Lvl(Upg u) => InCampaign && Career.C != null ? Upgrades.Level(Career.C.Current, u) : 0;
        public int CabinSize => Upgrades.CabinSize(Lvl(Upg.Cabin));
        float RadarFactor => Upgrades.RadarFactor(Lvl(Upg.Radar));
        float ShieldDrainNow => Upgrades.ShieldDrain(Lvl(Upg.Shields));
        float GunRateNow => Upgrades.GunRate(Lvl(Upg.Gun));
        float GunDamageNow => Upgrades.GunDamage(Lvl(Upg.Gun));

        // the ship as the upgrades make it (called on launch, recovery, a purchase; Free Flight: all at 1)
        void ApplyUpgrades()
        {
            Ship.SpeedFactor = Upgrades.SpeedFactor(Lvl(Upg.Engine));
            Ship.ArmourFactor = Upgrades.ArmourFactor(Lvl(Upg.Hull));
            Ship.DrainFactor = Upgrades.DrainFactor(Lvl(Upg.Energy));
            Foes.ShieldRadius = Upgrades.ShieldRadius(Lvl(Upg.Shields));
            RadarScope.ExactRange = Upgrades.ExactRange(Lvl(Upg.Radar)); RadarScope.Bezel = Lvl(Upg.Radar);
            Displays.CabinSize = CabinSize; if (CabinRoom) CabinRoom.SetCapacity(CabinSize);
            Extras.EngineLevel = Lvl(Upg.Engine);
            Displays.GunLevel = Lvl(Upg.Gun); if (reticle) BuildReticle(Lvl(Upg.Gun));
        }

        void BuildShopPage()
        {
            shopPage = new MenuPanel.Page { Title = "UPGRADES", Subtitle = "The corps' workshop: paid in credits, kept with your ship" };
            shopPage.Body = () =>
            {
                var c = Career.C; if (c == null) return "";
                var sb = new StringBuilder();
                sb.AppendLine($"<b>{c.credits} cr</b>" + (c.flyingLoaner ? "   ·   <color=#FFC873>You're flying the loaner: upgrades go on your own ship, once it is recovered.</color>" : ""));
                sb.AppendLine($"Ship value {c.ShipValue} cr: recovery after a loss {c.RecoveryFee} cr.\n");
                for (int i = 0; i < Upgrades.Count; i++)
                {
                    var u = (Upg)i; int l = Upgrades.Level(c.ship, u), m = Upgrades.Max(u);
                    sb.AppendLine($"<b>{Upgrades.Names[i]}</b> {l}/{m}: {Upgrades.Effect(u, l)}" + (l < m ? $"\n  <size=22>next: {Upgrades.Effect(u, l + 1)}</size>" : "  <size=22>(full)</size>"));
                }
                return sb.ToString().TrimEnd();
            };
            for (int i = 0; i < Upgrades.Count; i++)
            {
                var u = (Upg)i;
                shopPage.Add(new MenuPanel.Row
                {
                    Label = () => { var s = Career.C.ship; int l = Upgrades.Level(s, u), m = Upgrades.Max(u); return $"{Upgrades.Names[(int)u]}  {new string('■', l)}{new string('□', m - l)}  " + (l < m ? $"<size=24>({Upgrades.Price(s, u)} cr)</size>" : "<size=24>(full)</size>"); },
                    Submit = () => Buy(u),
                    Disabled = () => { var c = Career.C; return c.flyingLoaner || Upgrades.Level(c.ship, u) >= Upgrades.Max(u) || c.credits < Upgrades.Price(c.ship, u); }
                });
            }
            shopPage.Add(Action("Back", () => Menu.Show(hangar)));
            shopPage.Back = () => Menu.Show(hangar);
        }

        static string UpgradeSummary(ShipState s)
        {
            var parts = new System.Collections.Generic.List<string>();
            for (int i = 0; i < Upgrades.Count; i++) { int l = Upgrades.Level(s, (Upg)i); if (l > 0) parts.Add($"{Upgrades.Names[i]} {l}"); }
            return string.Join(", ", parts);
        }

        public void Buy(Upg u)
        {
            var c = Career.C; var s = c.ship; int price = Upgrades.Price(s, u);
            if (c.flyingLoaner || Upgrades.Level(s, u) >= Upgrades.Max(u) || c.credits < price) return;
            c.Spend(price); s.upgrades.Add(Upgrades.Keys[(int)u]); c.Save();
            ApplyUpgrades();
            Sfx.Play(Sfx.Thunk, Ship.transform.position + Vector3.up * 1.5f, .8f, 8f);
            debrief = $"Fitted: {Upgrades.Names[(int)u]} level {Upgrades.Level(s, u)} ({Upgrades.Effect(u, Upgrades.Level(s, u))}): -{price} cr";
            Flash(debrief, 4f); Voice.Say("ctl_upgrade", VoiceKind.Radio, 2, maxAge: 10f);
        }
    }
}
