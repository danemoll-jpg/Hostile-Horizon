using System;
using System.IO;
using UnityEngine;

namespace HostileHorizon
{
    // Fix 8.3 (Round 9 A, 2b): Dan's Steam library artwork beside the launcher. The launcher installs the game into
    // <launcher folder>/versions/<id>/; this build carries SteamArt/HostileHorizon-*.png (Portrait, Wide, Hero, Logo, Icon, Cover =
    // the portrait, which the launcher shows). At start the game copies any missing or changed file up into the launcher's folder
    // (only when HostileHorizonLauncher.exe is there), so an existing install (Dan's Deck) gets the files and the new launcher cover
    // with a normal update, without a fresh install. Nothing else in that folder is touched. Exe checks skip it unless they ask
    // (-steamArtRoot <folder>: copy into that folder as if it were the launcher's).
    public static class SteamArt
    {
        public static string Result = "not run";
        public static readonly string[] Files = { "HostileHorizon-Portrait.png", "HostileHorizon-Wide.png", "HostileHorizon-Hero.png", "HostileHorizon-Logo.png", "HostileHorizon-Icon.png", "HostileHorizon-Cover.png" };

        public static void CopyUp()
        {
            try
            {
                string game = Path.GetDirectoryName(Application.dataPath), from = Path.Combine(game, "SteamArt");
                string to = LauncherBridge.Argument("-steamArtRoot");
                if (to == null)
                {
                    if (CheckRun.Active) { Result = "skipped (exe check)"; return; }   // the launcher's own test run (-racerTestSave) copies: its install is a test folder
                    var up = Directory.GetParent(game)?.Parent; to = up?.FullName;
                    if (to == null || !File.Exists(Path.Combine(to, "HostileHorizonLauncher.exe"))) { Result = "no launcher above the game folder"; return; }
                }
                if (!Directory.Exists(from)) { Result = "no SteamArt/ in the game folder"; return; }
                Directory.CreateDirectory(to);
                int copied = 0, same = 0;
                foreach (var name in Files)
                {
                    string src = Path.Combine(from, name), dst = Path.Combine(to, name);
                    if (!File.Exists(src)) continue;
                    if (File.Exists(dst) && Same(src, dst)) { same++; continue; }
                    File.Copy(src, dst + ".tmp", true); File.Copy(dst + ".tmp", dst, true); File.Delete(dst + ".tmp"); copied++;
                }
                Result = $"{copied} copied, {same} already there, into {to}";
            }
            catch (Exception e) { Result = "failed: " + e.Message; }
            Debug.Log("[SteamArt] " + Result);
        }

        static bool Same(string a, string b)
        {
            var fa = new FileInfo(a); var fb = new FileInfo(b);
            if (fa.Length != fb.Length) return false;
            return File.ReadAllBytes(a).AsSpan().SequenceEqual(File.ReadAllBytes(b));
        }
    }
}
