using UnityEngine;

namespace HostileHorizon
{
    // Round 1, item 10: placeholder sound, made in code (no recorded files yet; ElevenLabs sounds and Suno music come later).
    //  - engine hum: two detuned low tones and a little rumble, pitch and loudness rising with the throttle;
    //  - wind: filtered noise, louder and brighter with speed;
    //  - a thud on every hit of the rock.
    // Master volume (Settings) is AudioListener.volume.
    [RequireComponent(typeof(AudioSource))]
    public sealed class ShipAudio : MonoBehaviour
    {
        public ShipController Ship;
        volatile float throttle, speed01, active; volatile float thud;
        double phase1, phase2, rate; uint rng = 22222; float lpWind, lpRumble, lpWind2, thudEnv;

        public static ShipAudio Create(ShipController ship)
        {
            var go = new GameObject("Ship audio");   // its own object: the ship is hidden between flights, and a hidden source never starts
            var src = go.AddComponent<AudioSource>();
            src.clip = AudioClip.Create("silence", 4096, 1, AudioSettings.outputSampleRate, false);
            src.loop = true; src.spatialBlend = 0; src.playOnAwake = false;
            var a = go.AddComponent<ShipAudio>(); a.Ship = ship; a.rate = AudioSettings.outputSampleRate;
            src.Play();
            return a;
        }

        public void Thud(float impact) => thud = Mathf.Clamp01(.25f + impact / 30f);

        void Update()
        {
            bool on = Ship && Ship.gameObject.activeInHierarchy && !Ship.Crashed && Ship.EnginesOn && Time.timeScale > 0;
            active = Mathf.MoveTowards(active, on ? 1 : 0, Time.unscaledDeltaTime * 2f);
            // Round 2: silent with the engines off; in hover the engines work against the lift
            if (Ship) { throttle = Ship.Mode == FlightMode.Hover ? .45f + Mathf.Abs(Ship.Lift) * .35f : Ship.Throttle; speed01 = Mathf.InverseLerp(0, ShipController.MaxSpeed, Ship.Speed); }
        }

        float Rand() { rng ^= rng << 13; rng ^= rng >> 17; rng ^= rng << 5; return (rng & 0xffffff) / 8388608f - 1f; }

        void OnAudioFilterRead(float[] data, int channels)
        {
            float t = throttle, s = speed01, a = active;
            float f1 = 48f + 70f * t, f2 = f1 * 1.505f;
            float engineVol = a * (.10f + .10f * t), windVol = a * (.02f + .30f * s * s);
            float windCut = .02f + .10f * s;
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
                float sample = engineVol * (saw1 * .35f + sine2 * .5f + lpRumble * 4f) + windVol * lpWind2 * 6f + thudEnv * thudEnv * lpRumble * 18f;
                for (int c = 0; c < channels; c++) data[i + c] = sample;
            }
        }
    }
}
