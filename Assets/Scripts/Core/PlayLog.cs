using System.Collections.Generic;
using UnityEngine;

namespace HostileHorizon
{
    // Round 7 (A3): game events in Player.log with a "[Play]" prefix, in players' builds too, so the planning chat can read what
    // happened in Dan's session: launch, planet / biome, pilots, every alien event, music changes with the reason, computer / Control
    // lines, docking, deaths. Events only (never every frame); one short line each, no stack trace.
    public static class PlayLog
    {
        public static readonly List<string> Lines = new List<string>();   // checks read them back
        static bool ready;

        public static void Add(string s)
        {
            if (!ready)
            {
                ready = true;
                Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);   // a [Play] line is one line (warnings / errors keep theirs)
            }
            string line = $"[Play] {Time.time,7:0.0} {s}";
            Debug.Log(line);
            if (Lines.Count < 6000) Lines.Add(line);
        }
    }
}
