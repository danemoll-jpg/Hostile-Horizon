using System.Collections.Generic;
using UnityEngine;

namespace HostileHorizon
{
    // Round 6 (A7): what the player could notice, in order: every sound, sting, music change, voice line, cabin light and message,
    // logged only while a check turns it on. The check compares an alien boarding with a real pilot boarding with it.
    public static class Trace
    {
        public static bool On;
        public static readonly List<(float t, string kind, string what)> Lines = new List<(float, string, string)>();
        public static void Add(string kind, string what) { if (On && Lines.Count < 8000) Lines.Add((Time.time, kind, what)); }
        public static void Start() { Lines.Clear(); On = true; }
    }
}
