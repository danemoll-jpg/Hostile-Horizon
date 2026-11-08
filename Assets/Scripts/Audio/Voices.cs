using System.Collections.Generic;
using UnityEngine;

namespace HostileHorizon
{
    public enum VoiceKind { Radio, Computer, Door, Cabin }

    // Round 4, Part C: the voices. Lines are ElevenLabs files in Resources/Voices/<id>.mp3 with their text in lines.json (both made by
    // Tools/make_voices.py; Docs/VOICES.md lists them). One line at a time from a queue with priorities (the computer's warnings
    // first, then Control, then pilots; a line waiting too long is dropped). How each kind sounds (C3):
    //  - Radio (Control, pilots on the radio): band-limited (high-pass 350 Hz, low-pass 3.2 kHz), a little drive, light crackle, a
    //    click in and a click out; 2D (it is the cockpit's radio);
    //  - Computer: clean and close, 2D;
    //  - Door: a pilot (or what looks like one) shouting through the hull: 3D at the airlock, low-pass 900 Hz;
    //  - Cabin: from the cabin behind the cockpit (3D, through the bulkhead).
    // Subtitles (C4, Settings > Sound > Subtitles): the line's speaker and text while it plays, on the HUD and the cockpit's centre
    // display (FlightDisplays). Volume: Settings > Sound > Voices. Music ducks while a line plays (Music.Duck).
    // Fix 6.3: every line ducks the engine, wind and music (and effects a little): DuckAmount, 0..1, eased in and out; the computer
    // plays at the full Voices level (0.6.2: 90 %) from lines made clearer and even (Tools/process_computer_voice.py), and a short
    // attention chime comes before each of its warnings (Warnings).
    public sealed class Voices : MonoBehaviour
    {
        public static Voices I { get; private set; }
        public bool Speaking => playing != null;
        public static float DuckAmount { get; private set; }   // Fix 6.3: 1 while a line plays
        // Fix 6.3: the computer's warnings (a chime first); its other lines (airlock, gear, shield, target down...) are plain
        public static readonly HashSet<string> Warnings = new HashSet<string> { "cpu_pull_up", "cpu_energy_low", "cpu_return_now", "cpu_cant_reach", "cpu_hull_damage", "cpu_hull_critical", "cpu_locked_on", "cpu_saucer", "cpu_intruder", "cpu_whiteout" };
        public float ComputerRms { get; private set; }   // checks: the computer source's output now
        public bool Testing;   // Fix 6.3: Settings > Sound > Test voices plays even in the pause menu

        // Fix 6.3: Settings > Sound > Test voices: one line each from Control, the computer (a warning, with its chime) and a pilot
        public void Test()
        {
            StopAll(); Testing = true;
            Say("ctl_launch_1", VoiceKind.Radio, 2, maxAge: 30f); Say("cpu_energy_low", VoiceKind.Computer, 2, maxAge: 30f); Say("p0_thanks_1", VoiceKind.Radio, 2, maxAge: 30f);
        }
        public string Subtitle => GameSettings.S.subtitles && Time.unscaledTime < subtitleUntil ? subtitle : "";
        public static readonly List<string> Log = new List<string>();   // checks: every line started (time, id, kind, speaker, text)
        public string[] PilotNames { get; private set; } = new string[0];

        struct Line { public string speaker, text; }
        sealed class Req { public string id; public VoiceKind kind; public int priority; public float time, maxAge; public Transform at; public Vector3 local; public string speaker; }
        readonly Dictionary<string, Line> lines = new Dictionary<string, Line>();
        readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        readonly List<Req> queue = new List<Req>();
        Req playing; AudioSource radio, computer, spatial, crackle, click; AudioLowPassFilter spatialLp;
        float endsAt, nextAt, subtitleUntil; string subtitle = ""; AudioClip clickClip, crackleClip, chimeClip; readonly float[] meter = new float[512];

        public static Voices Create()
        {
            var go = new GameObject("Voices"); DontDestroyOnLoad(go);
            var v = go.AddComponent<Voices>(); I = v;
            var r = new GameObject("Radio"); r.transform.SetParent(go.transform, false);
            v.radio = r.AddComponent<AudioSource>(); v.radio.spatialBlend = 0; v.radio.playOnAwake = false;
            r.AddComponent<AudioHighPassFilter>().cutoffFrequency = 350; r.AddComponent<AudioLowPassFilter>().cutoffFrequency = 3200;
            r.AddComponent<AudioDistortionFilter>().distortionLevel = .35f;
            var c = new GameObject("Computer"); c.transform.SetParent(go.transform, false);
            v.computer = c.AddComponent<AudioSource>(); v.computer.spatialBlend = 0; v.computer.playOnAwake = false;
            var s = new GameObject("Spatial voice"); s.transform.SetParent(go.transform, false);
            v.spatial = s.AddComponent<AudioSource>(); v.spatial.spatialBlend = 1; v.spatial.minDistance = 3; v.spatial.maxDistance = 60; v.spatial.playOnAwake = false; v.spatial.dopplerLevel = 0;
            v.spatialLp = s.AddComponent<AudioLowPassFilter>();
            v.crackleClip = Noise(); v.clickClip = Click(); v.chimeClip = Chime();
            v.crackle = go.AddComponent<AudioSource>(); v.crackle.clip = v.crackleClip; v.crackle.loop = true; v.crackle.spatialBlend = 0; v.crackle.playOnAwake = false;
            v.click = go.AddComponent<AudioSource>(); v.click.spatialBlend = 0; v.click.playOnAwake = false;
            v.ReadLines();
            return v;
        }

        void ReadLines()
        {
            var ta = Resources.Load<TextAsset>("Voices/lines");
            if (!ta) { Debug.LogWarning("Voices/lines.json missing: no voices"); return; }
            var data = JsonUtility.FromJson<LinesFile>(ta.text);
            if (data?.lines != null) foreach (var l in data.lines) lines[l.id] = new Line { speaker = l.speaker, text = l.text };
            PilotNames = data?.pilots ?? new string[0];
        }
        [System.Serializable] sealed class LinesFile { public string[] pilots; public LineEntry[] lines; }
        [System.Serializable] sealed class LineEntry { public string id, speaker, text; }

        public bool Has(string id) => lines.ContainsKey(id);
        public string Text(string id) => lines.TryGetValue(id, out var l) ? l.text : "";
        public int PilotVoiceCount => PilotNames.Length;

        // A planet's pilots each get a different voice (shuffled from the planet's seed; more pilots than voices repeat them).
        public int PilotVoice(int planetSeed, int index)
        {
            int n = Mathf.Max(1, PilotNames.Length); var order = new int[n]; for (int i = 0; i < n; i++) order[i] = i;
            var r = new SeedRng(planetSeed, 300); for (int i = n - 1; i > 0; i--) { int j = r.Next(i + 1); (order[i], order[j]) = (order[j], order[i]); }
            return order[index % n];
        }
        public string PilotName(int voice) => voice >= 0 && voice < PilotNames.Length ? PilotNames[voice] : "?";

        // Queue a line. priority: 3 computer warnings, 2 Control, 1 pilots, 0 chatter. maxAge: dropped if it waited longer.
        public void Say(string id, VoiceKind kind, int priority, Transform at = null, Vector3 local = default, float maxAge = 8f)
        {
            if (!lines.ContainsKey(id)) { Debug.LogWarning($"[Voices] no line {id}"); return; }
            if (playing != null && playing.id == id) return;
            foreach (var q in queue) if (q.id == id) return;
            queue.Add(new Req { id = id, kind = kind, priority = priority, time = Time.unscaledTime, maxAge = maxAge, at = at, local = local }); Trace.Add("voice", id);
        }

        public void Clear() { queue.Clear(); }
        public void StopAll() { Trace.Add("voice", "(all stopped)"); queue.Clear(); foreach (var a in new[] { radio, computer, spatial, crackle }) a.Stop(); playing = null; subtitleUntil = 0; }

        AudioClip ClipOf(string id)
        {
            if (clips.TryGetValue(id, out var c)) return c;
            c = Resources.Load<AudioClip>("Voices/" + id); clips[id] = c; return c;
        }

        void Update()
        {
            float now = Time.unscaledTime;
            float vol = GameSettings.S.voiceVolume / 100f;
            DuckAmount = Mathf.MoveTowards(DuckAmount, playing != null && Time.timeScale > 0 ? 1 : 0, Time.unscaledDeltaTime * (playing != null ? 4f : 1.5f));
            if (computer.isPlaying) { computer.GetOutputData(meter, 0); double q = 0; foreach (var x in meter) q += x * x; ComputerRms = Mathf.Sqrt((float)(q / meter.Length)); } else ComputerRms = 0;
            radio.volume = vol; computer.volume = vol; spatial.volume = vol; click.volume = vol * .35f; crackle.volume = playing != null && playing.kind == VoiceKind.Radio ? vol * .045f : 0;
            if (playing != null)
            {
                if (playing.kind == VoiceKind.Door || playing.kind == VoiceKind.Cabin) { if (playing.at) spatial.transform.position = playing.at.TransformPoint(playing.local); }
                bool paused = Time.timeScale == 0 && !Testing;
                foreach (var a in new[] { radio, computer, spatial }) { if (paused && a.isPlaying) a.Pause(); else if (!paused) a.UnPause(); }
                if (paused) { endsAt += Time.unscaledDeltaTime; nextAt += Time.unscaledDeltaTime; return; }
                if (now < endsAt) return;
                if (playing.kind == VoiceKind.Radio) { click.PlayOneShot(clickClip); crackle.Stop(); }
                playing = null; nextAt = now + .35f; subtitleUntil = now + 1.2f;
            }
            if (Testing && playing == null && queue.Count == 0) Testing = false;
            if (now < nextAt || queue.Count == 0 || Time.timeScale == 0 && !Testing) return;
            queue.RemoveAll(q => now - q.time > q.maxAge);
            if (queue.Count == 0) return;
            Req best = null; foreach (var q in queue) if (best == null || q.priority > best.priority || q.priority == best.priority && q.time < best.time) best = q;
            queue.Remove(best);
            Play(best, now);
        }

        void Play(Req r, float now)
        {
            var line = lines[r.id]; var c = ClipOf(r.id);
            float len = c ? c.length : Mathf.Clamp(line.text.Length * .065f, 1.2f, 6f);   // a missing file: the subtitle alone, for as long as it would take
            float delay = 0;
            switch (r.kind)
            {
                case VoiceKind.Radio:
                    click.PlayOneShot(clickClip); crackle.Play(); delay = .12f;
                    if (c) { radio.clip = c; radio.PlayDelayed(delay); }
                    break;
                case VoiceKind.Computer:
                    if (Warnings.Contains(r.id)) { click.PlayOneShot(chimeClip, Tuning.T.voiceChime / Mathf.Max(.01f, click.volume) * vol(r)); delay = .42f; }
                    if (c) { computer.clip = c; computer.PlayDelayed(delay); }
                    break;
                default:
                    spatialLp.cutoffFrequency = r.kind == VoiceKind.Door ? 900 : 1800;
                    if (r.at) spatial.transform.position = r.at.TransformPoint(r.local);
                    if (c) { spatial.clip = c; spatial.Play(); }
                    break;
            }
            playing = r; endsAt = now + delay + len; subtitleUntil = endsAt + 1.2f;
            string who = line.speaker == "Control" ? "CONTROL" : line.speaker == "Computer" ? "COMPUTER" : "PILOT " + line.speaker.ToUpperInvariant();
            subtitle = (r.kind == VoiceKind.Door ? "<i>(through the hull)</i> " : r.kind == VoiceKind.Cabin ? "<i>(cabin)</i> " : "") + $"<b>{who}:</b> {line.text}";
            if (CheckRun.Active && Log.Count < 3000) Log.Add($"{Time.time:0.0} voice {r.id} [{r.kind}, p{r.priority}{(c ? $", {c.length:0.0} s" : ", no file")}] {who}: {line.text}");
            if (CheckRun.Active && Sfx.Played.Count < 4000) Sfx.Played.Add($"{Time.time:0.0} voice {r.id} ({r.kind})");
        }

        static float vol(Req r) => GameSettings.S.voiceVolume / 100f;

        // Fix 6.3: two soft rising tones before a computer warning (made in code)
        static AudioClip Chime()
        {
            int rate = AudioSettings.outputSampleRate, n = (int)(rate * .36f); var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)rate, f = t < .16f ? 880f : 1318.5f, t0 = t < .16f ? t : t - .16f;
                d[i] = Mathf.Sin(t * 2 * Mathf.PI * f) * Mathf.Min(1, t0 * 200f) * Mathf.Exp(-t0 * 9f) * .6f;
            }
            var c = AudioClip.Create("computer chime", n, 1, rate, false); c.SetData(d, 0); return c;
        }

        // radio hiss (loops) and the click of the transmit key, made in code
        static AudioClip Noise()
        {
            int rate = AudioSettings.outputSampleRate, n = rate * 2; var d = new float[n]; uint s = 991; float lp = 0;
            for (int i = 0; i < n; i++) { s ^= s << 13; s ^= s >> 17; s ^= s << 5; float x = (s & 0xffffff) / 8388608f - 1f; lp += (x - lp) * .3f; d[i] = (x - lp) * .5f + ((s & 0xfff) == 7 ? 1f : 0f) * .8f; }
            var c = AudioClip.Create("radio hiss", n, 1, rate, false); c.SetData(d, 0); return c;
        }

        static AudioClip Click()
        {
            int rate = AudioSettings.outputSampleRate, n = rate / 20; var d = new float[n];
            for (int i = 0; i < n; i++) { float t = i / (float)rate; d[i] = Mathf.Exp(-t * 300f) * Mathf.Sin(t * 2 * Mathf.PI * 2400f) * .8f + Mathf.Exp(-t * 900f) * .6f; }
            var c = AudioClip.Create("radio click", n, 1, rate, false); c.SetData(d, 0); return c;
        }
    }
}
