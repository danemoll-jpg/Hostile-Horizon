using System;

namespace HostileHorizon
{
    // Seeded 2D gradient (Perlin) noise. Pure C#, no Unity calls, so terrain jobs can run it on worker threads. The same seed always
    // gives the same values (the permutation comes from System.Random(seed), which is stable across .NET versions for a given seed).
    public sealed class Noise
    {
        readonly int[] p = new int[512];

        public Noise(int seed)
        {
            var r = new Random(seed);
            var perm = new int[256];
            for (int i = 0; i < 256; i++) perm[i] = i;
            for (int i = 255; i > 0; i--) { int j = r.Next(i + 1); (perm[i], perm[j]) = (perm[j], perm[i]); }
            for (int i = 0; i < 512; i++) p[i] = perm[i & 255];
        }

        static float Fade(float t) => t * t * t * (t * (t * 6 - 15) + 10);

        static float Grad(int h, float x, float y)
        {
            switch (h & 7)
            {
                case 0: return x + y;
                case 1: return -x + y;
                case 2: return x - y;
                case 3: return -x - y;
                case 4: return x;
                case 5: return -x;
                case 6: return y;
                default: return -y;
            }
        }

        // About -1..1.
        public float Get(float x, float y)
        {
            int xi = (int)Math.Floor(x), yi = (int)Math.Floor(y);
            float xf = x - xi, yf = y - yi;
            xi &= 255; yi &= 255;
            float u = Fade(xf), v = Fade(yf);
            int aa = p[p[xi] + yi], ab = p[p[xi] + yi + 1], ba = p[p[xi + 1] + yi], bb = p[p[xi + 1] + yi + 1];
            float x1 = Lerp(Grad(aa, xf, yf), Grad(ba, xf - 1, yf), u);
            float x2 = Lerp(Grad(ab, xf, yf - 1), Grad(bb, xf - 1, yf - 1), u);
            return Lerp(x1, x2, v) * 0.95f;
        }

        public float Fbm(float x, float y, int octaves)
        {
            float sum = 0, amp = 1, norm = 0;
            for (int i = 0; i < octaves; i++) { sum += Get(x, y) * amp; norm += amp; amp *= .5f; x = x * 2.03f + 17.1f; y = y * 2.03f - 9.7f; }
            return sum / norm;
        }

        // 0..1, sharp crests where the noise crosses zero.
        public float Ridged(float x, float y, int octaves)
        {
            float sum = 0, amp = 1, norm = 0;
            for (int i = 0; i < octaves; i++) { float n = 1 - Math.Abs(Get(x, y)); sum += n * n * amp; norm += amp; amp *= .5f; x = x * 2.1f + 5.3f; y = y * 2.1f + 2.9f; }
            return sum / norm;
        }

        public static float Lerp(float a, float b, float t) => a + (b - a) * t;
        public static float Smooth(float e0, float e1, float x) { float t = Math.Clamp((x - e0) / (e1 - e0), 0f, 1f); return t * t * (3 - 2 * t); }
    }
}
