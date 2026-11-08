using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace HostileHorizon
{
    // Round 4, items B1, B2, B5: the adaptive music. The tracks are files in the soundtrack folder: the launcher's soundtrack
    // component when the launcher starts the game (-racerManagedMusic), else Music/ beside the exe (copied from BundleMusic/ by the
    // build; in the Editor BundleMusic/ itself). music.json there (Tools/music_loops.py) gives each track's loop: the intro plays once,
    // then an equal-power crossfade from loopEnd - crossfade into loopStart - crossfade, round and round (a track not listed loops
    // as a whole with a 4 s crossfade). A missing track is silence.
    // GameShell sets Want (the track for the game state) every frame; a change crossfades over 4 s. Tension (low energy) is a layer
    // over the flight track. Duck: under voice messages. Silent: the horror sequence, near a waiting pilot and landed for a pickup
    // (wind only). Stingers (Resources/Sounds/sting_*) play 2D at the music volume. Volume: Settings > Sound > Music.
    public sealed class Music : MonoBehaviour
    {
        public static Music I { get; private set; }
        public string Want = "";              // the track for the state ("" = silence)
        public bool Tension;                  // the low-energy layer
        public float Duck = 1;                // 1 = full, lower under a voice
        public static readonly List<string> Log = new List<string>();   // checks: what changed, when, at what volume
        public string Folder { get; private set; }
        public string Playing => main.track;
        public float PlayingVolume => main.gain;
        public int LoadedTracks => clips.Count;
        // checks only (Fix 4.1): set the state directly, and look at / move the tension layer's play position
        public string CheckWant; public bool CheckTension;
        public string TensionTrack => tension.track;
        public float TensionTime => tension.clip ? tension.src[tension.cur].time : -1;
        public float TensionLength => tension.clip ? tension.clip.length : 0;
        public bool TensionCrossfading => tension.fade >= 0;
        public float MainLength => main.clip ? main.clip.length : 0;
        public void CheckSeekTension(float t) { if (tension.clip) tension.src[tension.cur].time = t; }
        // Round 7 (A1, checks): the main track's play position, moved to just before its loop end, and its loop points
        public float MainTime => main.clip ? main.src[main.cur].time : -1;
        public bool MainCrossfading => main.fade >= 0;
        public void CheckSeekMain(float t) { if (main.clip) main.src[main.cur].time = t; }
        public Vector3 LoopPoints(string track) { if (clips.TryGetValue(track, out var c) && c) { var l = LoopOf(track, c); return new Vector3(l.start, l.end, l.xfade); } return Vector3.zero; }

        sealed class Loop { public float start, end, xfade; }
        sealed class Deck
        {
            public string track = ""; public AudioSource[] src = new AudioSource[2]; public int cur; public float gain, fade = -1; public Loop loop;
            public AudioClip clip;
        }
        readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        readonly HashSet<string> loading = new HashSet<string>(), missing = new HashSet<string>();
        readonly Dictionary<string, Loop> loops = new Dictionary<string, Loop>();
        Deck main, old, tension; AudioSource oneShot;
        string lastLogged = "?", doneOnce = ""; float lastLogVol = -1;

        public static Music Create()
        {
            var go = new GameObject("Music"); DontDestroyOnLoad(go);
            var m = go.AddComponent<Music>(); I = m;
            m.main = m.NewDeck(); m.old = m.NewDeck(); m.tension = m.NewDeck();
            m.oneShot = go.AddComponent<AudioSource>(); m.oneShot.spatialBlend = 0; m.oneShot.playOnAwake = false; m.oneShot.ignoreListenerPause = true;
            m.Folder = FindFolder(); m.ReadLoops();
            return m;
        }

        Deck NewDeck()
        {
            var d = new Deck();
            for (int i = 0; i < 2; i++) { var a = gameObject.AddComponent<AudioSource>(); a.spatialBlend = 0; a.playOnAwake = false; a.loop = false; a.priority = 0; a.ignoreListenerPause = true; d.src[i] = a; }
            return d;
        }

        static string FindFolder()
        {
            var managed = LauncherBridge.ManagedMusic;
            if (!string.IsNullOrEmpty(managed) && Directory.Exists(managed)) return managed;
            var beside = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Music"));
            if (Directory.Exists(beside)) return beside;
            var bundle = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "BundleMusic"));
            return Directory.Exists(bundle) ? bundle : beside;
        }

        void ReadLoops()
        {
            try
            {
                var f = Path.Combine(Folder, "music.json"); if (!File.Exists(f)) return;
                // music.json: {"tracks": {"name": {"loopStart": s, "loopEnd": s, "crossfade": s, ...}, ...}} (a tiny reader: no JSON library here)
                var text = File.ReadAllText(f); int at = text.IndexOf("\"tracks\""); if (at < 0) return;
                int i = text.IndexOf('{', at) + 1;
                while (true)
                {
                    int q0 = text.IndexOf('"', i); if (q0 < 0) break; int q1 = text.IndexOf('"', q0 + 1); string name = text.Substring(q0 + 1, q1 - q0 - 1);
                    int b0 = text.IndexOf('{', q1), b1 = text.IndexOf('}', b0); if (b0 < 0 || b1 < 0) break; string body = text.Substring(b0, b1 - b0);
                    loops[name] = new Loop { start = Num(body, "loopStart", 0), end = Num(body, "loopEnd", 0), xfade = Num(body, "crossfade", 4) };
                    i = b1 + 1; int next = text.IndexOf('"', i), close = text.IndexOf('}', i); if (next < 0 || close >= 0 && close < next) break;
                }
            }
            catch (System.Exception e) { Debug.LogWarning("music.json unreadable: " + e.Message); }
        }

        static float Num(string body, string key, float fallback)
        {
            int k = body.IndexOf("\"" + key + "\""); if (k < 0) return fallback;
            int c = body.IndexOf(':', k) + 1; int e = c; while (e < body.Length && "0123456789.-eE+ ".IndexOf(body[e]) >= 0) e++;
            return float.TryParse(body.Substring(c, e - c).Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : fallback;
        }

        string PathOf(string track)
        {
            foreach (var ext in new[] { ".ogg", ".mp3", ".wav" }) { var p = Path.Combine(Folder, track + ext); if (File.Exists(p)) return p; }
            return null;
        }

        bool Ready(string track, out AudioClip c)
        {
            c = null; if (string.IsNullOrEmpty(track) || missing.Contains(track)) return false;
            if (clips.TryGetValue(track, out c) && c) return true;
            if (!loading.Contains(track)) StartCoroutine(LoadClip(track));
            return false;
        }

        IEnumerator LoadClip(string track)
        {
            loading.Add(track);
            var path = PathOf(track);
            if (path == null) { missing.Add(track); loading.Remove(track); Debug.Log($"[Music] {track}: no file in {Folder} (silent)"); yield break; }
            var type = path.EndsWith(".ogg") ? AudioType.OGGVORBIS : path.EndsWith(".wav") ? AudioType.WAV : AudioType.MPEG;
            using (var req = UnityWebRequestMultimedia.GetAudioClip(new System.Uri(path).AbsoluteUri, type))
            {
                var dh = (DownloadHandlerAudioClip)req.downloadHandler; dh.streamAudio = false; dh.compressed = true;
                yield return req.SendWebRequest();
                if (req.result != UnityWebRequest.Result.Success) { missing.Add(track); Debug.LogWarning($"[Music] {track}: {req.error}"); }
                else
                {
                    var c = DownloadHandlerAudioClip.GetContent(req); c.name = track;
                    while (c.loadState == AudioDataLoadState.Loading) yield return null;
                    if (c.loadState == AudioDataLoadState.Loaded) { clips[track] = c; Debug.Log($"[Music] loaded {track} ({c.length:0} s)"); } else missing.Add(track);
                }
            }
            loading.Remove(track);
        }

        Loop LoopOf(string track, AudioClip c)
        {
            if (loops.TryGetValue(track, out var l) && l.end > l.start + 2 && l.end <= c.length + .01f) return l;
            float x = Mathf.Min(4f, c.length / 6f);
            return new Loop { start = x, end = c.length - .05f, xfade = x };
        }

        void Begin(Deck d, string track, AudioClip c, bool loop)
        {
            foreach (var s in d.src) { s.Stop(); s.volume = 0; }
            d.track = track; d.clip = c; d.cur = 0; d.fade = -1; d.loop = loop ? LoopOf(track, c) : null;
            d.src[0].clip = c; d.src[0].time = 0; d.src[0].Play();
        }

        void Stop(Deck d) { foreach (var s in d.src) s.Stop(); d.track = ""; d.clip = null; d.gain = 0; }

        // within a deck: crossfade the loop's end into its start
        void TickLoop(Deck d, float dt)
        {
            if (d.clip == null) return;
            var a = d.src[d.cur]; var b = d.src[1 - d.cur];
            if (d.loop == null) { if (!a.isPlaying) { doneOnce = d.track; Stop(d); } else a.volume = d.gain * Volume; return; }
            if (d.fade < 0 && a.isPlaying && a.time >= d.loop.end - d.loop.xfade)
            {
                b.clip = d.clip; b.time = Mathf.Max(0, d.loop.start - d.loop.xfade); b.Play(); d.fade = 0;
            }
            float ka = 1, kb = 0;
            if (d.fade >= 0)
            {
                d.fade += dt / Mathf.Max(.05f, d.loop.xfade);
                float t = Mathf.Clamp01(d.fade); ka = Mathf.Cos(t * Mathf.PI / 2); kb = Mathf.Sin(t * Mathf.PI / 2);
                if (d.fade >= 1) { a.Stop(); d.cur = 1 - d.cur; d.fade = -1; ka = 0; kb = 1; }
            }
            else if (!a.isPlaying) { a.time = d.loop.start; a.Play(); }   // (never: the crossfade comes first)
            float v = d.gain * Volume;
            if (d.fade >= 0 || ka == 0) { a.volume = v * ka; b.volume = v * kb; }
            else { a.volume = v; b.volume = 0; }
        }

        float Volume => (GameSettings.S.musicVolume / 100f) * Duck;

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            Duck = 1 - (1 - Tuning.T.voiceDuckMusic) * Voices.DuckAmount;   // Fix 6.3: one duck for engine, wind, music and effects (Voices)
            // main track: a different one crossfades over 4 s (the old one moves to the old deck)
            if (Want != doneOnce) doneOnce = "";
            if (Want != main.track && (Want == "" || Want != doneOnce))
            {
                if (string.IsNullOrEmpty(Want)) { if (main.track != "") { Swap(); } }
                else if (Ready(Want, out var c)) { Swap(); Begin(main, Want, c, Want != "regeneration"); main.gain = Want == "regeneration" ? 1 : 0; }
                else if (missing.Contains(Want) && main.track != "") Swap();
            }
            main.gain = Mathf.MoveTowards(main.gain, main.track == "" ? 0 : Tension && tension.track != "" ? .55f : 1f, dt / 4f);
            old.gain = Mathf.MoveTowards(old.gain, 0, dt / 4f);
            if (old.gain <= 0 && old.track != "") Stop(old);
            // the tension layer
            if (Tension && tension.track == "" && Ready("tension_low_energy", out var tc)) { Begin(tension, "tension_low_energy", tc, true); tension.gain = 0; }
            tension.gain = Mathf.MoveTowards(tension.gain, Tension ? .9f : 0, dt / 3f);
            if (!Tension && tension.gain <= 0 && tension.track != "") Stop(tension);
            TickLoop(main, dt); TickLoop(old, dt); TickLoop(tension, dt);
            oneShot.volume = GameSettings.S.musicVolume / 100f;
            // the log (checks): track changes, and the volume in 0.25 steps
            float vol = Mathf.Round(main.gain * 4) / 4;
            if (main.track != lastLogged || Mathf.Abs(vol - lastLogVol) > .01f)
            {
                if (main.track != lastLogged) Trace.Add("music", main.track == "" ? "(silence)" : main.track);
                lastLogged = main.track; lastLogVol = vol;
                if (CheckRun.Active && Log.Count < 2000) Log.Add($"{Time.time:0.0} music \"{(main.track == "" ? "(silence)" : main.track)}\" gain {vol:0.00}{(Tension ? " +tension" : "")}{(Duck < .99f ? " ducked" : "")}");
            }
        }

        // Round 6 (A2): the strike cuts the music at once (no 4 s fade)
        public void Cut() { Stop(main); Stop(old); Stop(tension); Trace.Add("music", "(cut)"); }

        void Swap() { if (old.track != "") Stop(old); var t = old; old = main; main = t; Stop(main); }

        // a short cue (stinger): 2D, at the music volume, from Resources/Sounds
        public void Sting(string name)
        {
            var c = Sfx.Clip(name); if (!c) return;
            oneShot.PlayOneShot(c, 1f); Trace.Add("sting", name);
            if (CheckRun.Active && Log.Count < 2000) Log.Add($"{Time.time:0.0} sting {name}");
            if (CheckRun.Active && Sfx.Played.Count < 4000) Sfx.Played.Add($"{Time.time:0.0} {name} (sting)");
        }

        public void PreLoad(params string[] tracks) { foreach (var t in tracks) Ready(t, out _); }

        // Fix 6.3 (checks): the music's output level now, RMS of every deck's sources (AudioSource.GetOutputData; before the listener)
        readonly float[] meter = new float[1024];
        public float OutputRms()
        {
            double sum = 0; int n = 0;
            foreach (var d in new[] { main, old, tension })
                foreach (var a in d.src)
                {
                    if (!a.isPlaying) continue;
                    for (int ch = 0; ch < 2; ch++) { a.GetOutputData(meter, ch); foreach (var v in meter) sum += v * v; n += meter.Length; }
                }
            return n > 0 ? Mathf.Sqrt((float)(sum / 2048)) : 0;   // the power of the sources playing together (each read over 1024 x 2 channels)
        }
    }
}
