using System.Collections;
using System.Linq;
using UnityEngine;

namespace HostileHorizon
{
    // Fix 4.1 check (Dan's tension and regeneration tracks), in the built exe:  HostileHorizon.exe -exeCheck <dir> -exeCheckOnly music
    // From the title: the flight track with the tension layer (its file's length, the loop points from music.json), the layer moved
    // to just before its loop end so the crossfade back to loopStart happens, then the regeneration cue played once to its end
    // (it must not loop). Muted: nothing is heard; the log gives the play positions and volumes.
    public sealed partial class ExeCheck
    {
        IEnumerator MusicCheck()
        {
            yield return WaitFor(() => G.Now == GameShell.State.Title, 60f);
            yield return new WaitForSecondsRealtime(2f);
            var m = G.Music;
            Log($"music folder {m.Folder}");
            m.CheckWant = "flight_canyons"; m.CheckTension = true;
            yield return WaitFor(() => m.TensionTrack == "tension_low_energy" && m.Playing == "flight_canyons", 30f);
            yield return new WaitForSecondsRealtime(4f);
            Log($"tension layer: \"{m.TensionTrack}\", file {m.TensionLength:0.0} s, at {m.TensionTime:0.0} s; under \"{m.Playing}\" gain {m.PlayingVolume:0.00}");
            m.CheckSeekTension(104f);
            Log("tension layer moved to 104.0 s (its loop end is 107.3 s, crossfade 1 s, back to 65.7 s)");
            bool sawFade = false; float t0 = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - t0 < 6f) { sawFade |= m.TensionCrossfading; yield return null; }
            Log($"after 6 s: at {m.TensionTime:0.0} s; crossfade seen {(sawFade ? "yes" : "NO")}; still playing \"{m.TensionTrack}\" ({(m.TensionTime > 60f && m.TensionTime < 80f ? "back in the loop: OK" : "NOT where the loop should be")})");
            m.CheckTension = false; m.CheckWant = "regeneration";
            yield return WaitFor(() => m.Playing == "regeneration", 30f);
            yield return new WaitForSecondsRealtime(1f);
            Log($"regeneration cue: playing \"{m.Playing}\", file {m.MainLength:0.0} s, gain {m.PlayingVolume:0.00}");
            yield return WaitFor(() => m.Playing != "regeneration", 40f);
            Log($"after it: playing \"{(m.Playing == "" ? "(silence)" : m.Playing)}\" (a one-shot: silence until the state changes); wait 5 s");
            yield return new WaitForSecondsRealtime(5f);
            Log($"5 s later: \"{(m.Playing == "" ? "(silence)" : m.Playing)}\" ({(m.Playing == "" ? "did not loop: OK" : "LOOPED")})");
            m.CheckWant = null;
            Log("--- music log"); foreach (var l in Music.Log) Log("  " + l);
        }
    }
}
