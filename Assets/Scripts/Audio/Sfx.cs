using UnityEngine;

namespace HostileHorizon
{
    // Round 2: placeholder sound effects made in code (ElevenLabs replaces them in Round 6), played as 3D sounds in the world so
    // they come from where they happen (VR rule 4): the explosion, a knock on the hull, the airlock, a warning beep, fire.
    public static class Sfx
    {
        static AudioClip boom, knock, hiss, beep, fire, thunk, warn, brake;
        static int rate => AudioSettings.outputSampleRate;

        public static AudioClip Boom => boom ? boom : boom = Make("boom", 4.5f, (t, n, s) =>
        {
            // a sharp crack, then a long low rumble
            float crack = Mathf.Exp(-t * 30f) * n;
            s.lp += (n - s.lp) * .02f; s.lp2 += (s.lp - s.lp2) * .04f;
            return crack * .6f + s.lp2 * 9f * Mathf.Exp(-t * .9f) * Mathf.Min(1, t * 60f);
        });

        public static AudioClip Knock => knock ? knock : knock = Make("knock", .22f, (t, n, s) =>
        {
            // a knuckle on a metal hull: a damped low ring and a click
            float env = Mathf.Exp(-t * 38f);
            return env * (Mathf.Sin(t * 2 * Mathf.PI * 190f) * .7f + Mathf.Sin(t * 2 * Mathf.PI * 523f) * .25f) + Mathf.Exp(-t * 400f) * n * .5f;
        });

        public static AudioClip Hiss => hiss ? hiss : hiss = Make("airlock", 1.6f, (t, n, s) =>
        {
            s.lp += (n - s.lp) * .35f;
            float env = Mathf.Min(1, t * 20f) * Mathf.Exp(-t * 1.6f);
            return (n - s.lp) * env * .45f + Mathf.Sin(t * 2 * Mathf.PI * 60f) * .15f * Mathf.Exp(-t * 3f);
        });

        public static AudioClip Thunk => thunk ? thunk : thunk = Make("clamp", .5f, (t, n, s) =>
            Mathf.Exp(-t * 14f) * (Mathf.Sin(t * 2 * Mathf.PI * 70f) * .8f + n * .2f));

        public static AudioClip Beep => beep ? beep : beep = Make("beep", .5f, (t, n, s) =>
            (t % .25f < .12f ? 1 : 0) * Mathf.Sin(t * 2 * Mathf.PI * 880f) * .35f);

        // Fix 2.3: the get-home warning (RETURN NOW / CAN'T REACH): a falling two-tone, unlike the 15 % beep's short high pips.
        public static AudioClip Warn => warn ? warn : warn = Make("warn", 1.2f, (t, n, s) =>
        {
            float note = t % .3f, f = (int)(t / .3f) % 2 == 0 ? 740f : 520f;
            float env = Mathf.Min(1, note * 60f) * Mathf.Clamp01((.26f - note) * 40f);
            return env * (Mathf.Sin(t * 2 * Mathf.PI * f) * .3f + Mathf.Sin(t * 2 * Mathf.PI * f * 2f) * .08f);
        });

        // Fix 2.4: the air brakes: a rushing roar that falls away over about 3 s as the ship slows.
        public static AudioClip Brake => brake ? brake : brake = Make("brake", 3.2f, (t, n, s) =>
        {
            s.lp += (n - s.lp) * Mathf.Lerp(.25f, .03f, t / 3.2f); s.lp2 += (s.lp - s.lp2) * .2f;
            float env = Mathf.Min(1, t * 12f) * Mathf.Clamp01(1.15f - t / 3.2f);
            return (s.lp - s.lp2 * .5f) * env * 1.6f + Mathf.Sin(t * 2 * Mathf.PI * Mathf.Lerp(95f, 45f, t / 3.2f)) * .12f * env;
        });

        public static AudioClip Fire => fire ? fire : fire = Make("fire", 3f, (t, n, s) =>
        {
            s.lp += (n - s.lp) * .05f;
            float crackle = n * (s.rng() > .9985f ? 1f : 0f) * 2f;
            return s.lp * 1.6f + crackle;
        }, true);

        sealed class St { public float lp, lp2; uint r = 12345; public float rng() { r ^= r << 13; r ^= r >> 17; r ^= r << 5; return (r & 0xffff) / 65536f; } }

        static AudioClip Make(string name, float seconds, System.Func<float, float, St, float> f, bool loop = false)
        {
            int n = Mathf.CeilToInt(seconds * rate); var d = new float[n]; var s = new St(); uint r = 777;
            for (int i = 0; i < n; i++)
            {
                r ^= r << 13; r ^= r >> 17; r ^= r << 5; float noise = (r & 0xffffff) / 8388608f - 1f;
                d[i] = Mathf.Clamp(f(i / (float)rate, noise, s), -1, 1);
            }
            if (loop) { int fade = rate / 10; for (int i = 0; i < fade; i++) { float k = i / (float)fade; d[i] = d[i] * k + d[n - fade + i] * (1 - k); } }
            var c = AudioClip.Create(name, n, 1, rate, false); c.SetData(d, 0); return c;
        }

        // A one-shot 3D sound at a point (minDistance: full loudness within it).
        public static AudioSource Play(AudioClip clip, Vector3 at, float volume = 1, float minDistance = 5, Transform follow = null)
        {
            var go = new GameObject("Sfx " + clip.name);
            go.transform.position = at; if (follow) go.transform.SetParent(follow, true);
            var a = go.AddComponent<AudioSource>();
            a.clip = clip; a.volume = volume; a.spatialBlend = 1; a.minDistance = minDistance; a.maxDistance = minDistance * 200; a.rolloffMode = AudioRolloffMode.Logarithmic; a.dopplerLevel = 0;
            a.Play(); Object.Destroy(go, clip.length + .1f);
            return a;
        }

        public static AudioSource Loop(AudioClip clip, Transform parent, float volume, float minDistance)
        {
            var go = new GameObject("Sfx loop " + clip.name); go.transform.SetParent(parent, false);
            var a = go.AddComponent<AudioSource>();
            a.clip = clip; a.loop = true; a.volume = volume; a.spatialBlend = 1; a.minDistance = minDistance; a.maxDistance = minDistance * 100; a.dopplerLevel = 0;
            a.Play(); return a;
        }
    }
}
