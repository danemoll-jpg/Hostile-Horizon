using UnityEngine;

namespace HostileHorizon
{
    // Round 1, item 10: placeholder sound, made in code (no recorded files yet; ElevenLabs sounds and Suno music come later).
    //  - engine hum: two detuned low tones and a little rumble, pitch and loudness rising with the throttle;
    //  - wind: filtered noise, louder and brighter with speed;
    //  - a thud on every hit of the rock.
    // Master volume (Settings) is AudioListener.volume.
    // Fix 6.3: engine hum and wind have their own volume (Settings > Sound > Engine; was Effects) and the tuning mix engineMix / windMix
    // (lower than 0.6.2's, so the music is heard over them); both duck while a voice line plays (Voices.DuckAmount). The thud stays
    // an effect. EngineRms / WindRms: what this source puts out now (the checks log the mix with them, muted).
    [RequireComponent(typeof(AudioSource))]
    public sealed class ShipAudio : MonoBehaviour
    {
        public ShipController Ship;
        volatile float throttle, speed01, active; volatile float thud, effects = 1, engine = 1, engineMix = 1, windMix = 1;
        public float EngineRms { get; private set; }
        public float WindRms { get; private set; }
        public static float EngineVolume => Mathf.Clamp01(GameSettings.S.engineVolume / 100f);
        public volatile float Wind;   // Round 4: wind with the engines off (landed waiting, the scares) and the blizzard
        double phase1, phase2, rate; uint rng = 22222; float lpWind, lpRumble, lpWind2, thudEnv;

        public static ShipAudio Create(ShipController ship)
        {
            var go = new GameObject("Ship audio");   // its own object: the ship is hidden between flights, and a hidden source never starts
            var src = go.AddComponent<AudioSource>();
            src.clip = AudioClip.Create("silence", 4096, 1, AudioSettings.outputSampleRate, false);
            src.loop = true; src.spatialBlend = 0; src.playOnAwake = false;
            var a = go.AddComponent<ShipAudio>(); a.Ship = ship; a.rate = AudioSettings.outputSampleRate;
            // Round 8 (B2): the engines are two ElevenLabs loops (idle, cruise) crossfaded and pitched with the throttle, at the level the
            // code-made hum had (so Fix 6.3's mix holds); the code keeps the wind, the rumble and the thud
            var idle = Resources.Load<AudioClip>("Sounds/engine_idle"); var cruise = Resources.Load<AudioClip>("Sounds/engine_cruise");
            if (idle && cruise) { a.idleSrc = Loop(go, idle); a.cruiseSrc = Loop(go, cruise); a.loops = true; }
            src.Play();
            return a;
        }

        AudioSource idleSrc, cruiseSrc; volatile bool loops;
        public static bool CheckHum;   // checks: the code-made hum instead of the loops (Round 7's engine), to compare
        const float LoopRms = .158f, HumRms = .45f;   // the loops' level (-16.4 LUFS) and the code hum's waveform level at volume 1
        static AudioSource Loop(GameObject go, AudioClip c) { var s = go.AddComponent<AudioSource>(); s.clip = c; s.loop = true; s.spatialBlend = 0; s.playOnAwake = false; s.volume = 0; s.Play(); return s; }
        public void Thud(float impact) => thud = Mathf.Clamp01(.25f + impact / 30f);

        void Update()
        {
            bool on = Ship && Ship.gameObject.activeInHierarchy && !Ship.Crashed && Ship.EnginesOn && Time.timeScale > 0;
            active = Mathf.MoveTowards(active, on ? 1 : 0, Time.unscaledDeltaTime * 2f); effects = Sfx.Effects;
            engine = EngineVolume * (1 - Tuning.T.voiceDuckEngine * Voices.DuckAmount); engineMix = Tuning.T.engineMix; windMix = Tuning.T.windMix;
            // Round 2: silent with the engines off; in hover the engines work against the lift
            if (Ship) { throttle = Ship.Mode == FlightMode.Hover ? .45f + Mathf.Abs(Ship.Lift) * .35f : Ship.Throttle; speed01 = Mathf.InverseLerp(0, ShipController.MaxSpeed, Ship.Speed); }
            if (loops && CheckHum) { idleSrc.volume = cruiseSrc.volume = 0; }
            if (loops && !CheckHum)
            {
                float t = throttle, hum = engine * engineMix * active * (.10f + .10f * t) * HumRms / LoopRms;   // the code hum's level, as loop volume
                float c = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.25f, .85f, t));
                idleSrc.volume = hum * Mathf.Sqrt(1 - c); cruiseSrc.volume = hum * Mathf.Sqrt(c);
                idleSrc.pitch = .9f + .25f * t; cruiseSrc.pitch = .85f + .3f * t;
                EngineRms = hum * LoopRms;   // what the loops put out (the checks' mix)
            }
        }

        float Rand() { rng ^= rng << 13; rng ^= rng >> 17; rng ^= rng << 5; return (rng & 0xffffff) / 8388608f - 1f; }

        void OnAudioFilterRead(float[] data, int channels)
        {
            float t = throttle, s = speed01, a = active;
            float f1 = 48f + 70f * t, f2 = f1 * 1.505f;
            float fx = effects, en = engine; float engineVol = loops && !CheckHum ? 0 : en * engineMix * a * (.10f + .10f * t), windVol = en * windMix * (a * (.02f + .30f * s * s) + Wind);
            double se = 0, sw = 0; int frames = 0;
            float windCut = .02f + .10f * Mathf.Max(s, Wind * .5f);
            for (int i = 0; i < data.Length; i += channels)
            {
                phase1 += f1 / rate; if (phase1 > 1) phase1 -= 1;
                phase2 += f2 / rate; if (phase2 > 1) phase2 -= 1;
                float saw1 = (float)(phase1 * 2 - 1), sine2 = Mathf.Sin((float)(phase2 * 2 * Mathf.PI));
                float n = Rand();
                lpRumble += (n - lpRumble) * .01f;
                lpWind += (n - lpWind) * windCut; lpWind2 += (lpWind - lpWind2) * windCut;
                if (thud > 0) { thudEnv = thud; thud = 0; }
                thudEnv *= .99965f;
                float eng = engineVol * (saw1 * .35f + sine2 * .5f + lpRumble * 4f), wnd = windVol * lpWind2 * 6f;
                float sample = eng + wnd + fx * thudEnv * thudEnv * lpRumble * 18f;
                se += eng * eng; sw += wnd * wnd; frames++;
                for (int c = 0; c < channels; c++) data[i + c] = sample;
            }
            if (frames > 0) { if (!loops || CheckHum) EngineRms = Mathf.Lerp(EngineRms, Mathf.Sqrt((float)(se / frames)), .2f); WindRms = Mathf.Lerp(WindRms, Mathf.Sqrt((float)(sw / frames)), .2f); }
        }
    }
}
