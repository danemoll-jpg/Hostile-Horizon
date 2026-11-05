using System;
using UnityEngine;

namespace HostileHorizon
{
    // Round 5 (B8): the Campaign's difficulty. Presets Easy / Normal (default) / Hard / Nightmare, or Custom. Each option is a level
    // index into the tuning file's tables (Tuning: aggressionLevels, alienShareLevels, tellLevels, falseTellLevels, payLevels), so
    // every number lives in CampaignTuning.json. Saved per career (Career.difficulty; chosen on the Campaign page for a new career,
    // changeable any time from the hangar). The alien surprise is on in every setting.
    [Serializable]
    public sealed class DifficultySetting
    {
        public int preset = 1;        // 0 Easy, 1 Normal, 2 Hard, 3 Nightmare, 4 Custom
        public int enemies = 2;       // 0 Off, 1 Turrets, 2 Turrets + Saucers
        public int aggression = 1;    // density, accuracy, fire rate: Low / Normal / High / Brutal
        public int alienShare = 1;    // Fewer / Normal / More / Most
        public int tells = 1;         // how often an alien shows its tell: 95 / 75 / 60 / 50 %
        public int falseTells = 0;    // real pilots with a tell-like quirk: 0 / 10 / 20 %
        public int crashLevel = 3;    // CrashLevel (Lasting damage)
        public int pay = 1;           // x0.8 / x1 / x1.25 / x1.5

        public DifficultySetting Copy() => (DifficultySetting)MemberwiseClone();

        // A preset sets every option (Custom keeps whatever is there).
        public void ApplyPreset(int p)
        {
            preset = Mathf.Clamp(p, 0, 4); if (preset == 4) return;
            var t = Tuning.T; int i = preset;
            enemies = 2; aggression = t.presetAggression[i]; alienShare = t.presetAlienShare[i]; tells = t.presetTells[i];
            falseTells = t.presetFalseTells[i]; crashLevel = t.presetCrash[i]; pay = t.presetPay[i];
        }
    }

    public static class Difficulty
    {
        public static readonly string[] PresetNames = { "Easy", "Normal", "Hard", "Nightmare", "Custom" };
        public static readonly string[] EnemyNames = { "Off", "Turrets", "Turrets + Saucers" };
        public static readonly string[] AggressionNames = { "Low", "Normal", "High", "Brutal" };
        public static readonly string[] AlienShareNames = { "Fewer", "Normal", "More", "Most" };

        // The career's setting while one is loaded, else the one a new career will get.
        public static DifficultySetting Current => Career.C?.difficulty ?? GameSettings.S.difficulty;

        static float At(float[] a, int i) => a[Mathf.Clamp(i, 0, a.Length - 1)];
        public static int Enemies => Mathf.Clamp(Current.enemies, 0, 2);
        public static float Aggression => At(Tuning.T.aggressionLevels, Current.aggression);
        public static float AlienShare => At(Tuning.T.alienShareLevels, Current.alienShare);
        public static float TellReliability => At(Tuning.T.tellLevels, Current.tells);
        public static float FalseTell => At(Tuning.T.falseTellLevels, Current.falseTells);
        public static float Pay => At(Tuning.T.payLevels, Current.pay);
        public static CrashLevel Crash => (CrashLevel)Mathf.Clamp(Current.crashLevel, 0, 4);
        // B7: the share of pilots' landing pads that a turret can see (0 on Easy / Normal: a landed ship is never a sitting duck)
        public static float CoveredPads => At(Tuning.T.coveredPadShare, Current.aggression);

        public static string Name(DifficultySetting d) => PresetNames[Mathf.Clamp(d.preset, 0, 4)];
        public static string Summary(DifficultySetting d)
        {
            var t = Tuning.T;
            return $"enemies {EnemyNames[Mathf.Clamp(d.enemies, 0, 2)]}, aggression {AggressionNames[Mathf.Clamp(d.aggression, 0, 3)]}, aliens {AlienShareNames[Mathf.Clamp(d.alienShare, 0, 3)]}, " +
                   $"tells {At(t.tellLevels, d.tells) * 100:0} %, false tells {At(t.falseTellLevels, d.falseTells) * 100:0} %, {CrashLevels.Names[Mathf.Clamp(d.crashLevel, 0, 4)]}, pay x{At(t.payLevels, d.pay):0.##}";
        }
    }
}
