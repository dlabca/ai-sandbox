using System;
using Microsoft.Xna.Framework;

namespace AISandbox
{
    public static class Noise
    {
        public static float Hash01(int a, int b, int seed)
        {
            unchecked
            {
                int h = a * 374761393 + b * 668265263 + seed * 1274126177;
                h = (h ^ (h >> 13)) * 1274126177;
                h ^= h >> 16;
                return (h & 0x7fffffff) / 2147483647f;
            }
        }

        public static float ValueNoise(float fx, float fz, int seed)
        {
            int x0 = (int)MathF.Floor(fx);
            int z0 = (int)MathF.Floor(fz);
            float tx = fx - x0;
            float tz = fz - z0;
            tx = tx * tx * (3f - 2f * tx);
            tz = tz * tz * (3f - 2f * tz);
            float a = Hash01(x0, z0, seed);
            float b = Hash01(x0 + 1, z0, seed);
            float c = Hash01(x0, z0 + 1, seed);
            float d = Hash01(x0 + 1, z0 + 1, seed);
            return MathHelper.Lerp(MathHelper.Lerp(a, b, tx), MathHelper.Lerp(c, d, tx), tz);
        }

        public static Color Scale(Color c, float f)
        {
            return new Color((int)(c.R * f), (int)(c.G * f), (int)(c.B * f));
        }

        public static Color Vary(Color c, float amount, int a, int b, int seed)
        {
            float v = 1f + (Hash01(a, b, seed) - 0.5f) * 2f * amount;
            return Scale(c, v);
        }

        public static Color Lerp(Color a, Color b, float t)
        {
            t = MathHelper.Clamp(t, 0f, 1f);
            return new Color(
                (int)(a.R + (b.R - a.R) * t),
                (int)(a.G + (b.G - a.G) * t),
                (int)(a.B + (b.B - a.B) * t));
        }
    }
}
