using System;
using Microsoft.Xna.Framework;

namespace AISandbox
{
    public static class MathUtil
    {
        private static float Comp(Vector3 v, int i)
        {
            return i == 0 ? v.X : (i == 1 ? v.Y : v.Z);
        }

        public static bool RayBox(Vector3 o, Vector3 d, Vector3 min, Vector3 max, out float t)
        {
            float tmin = 0f;
            float tmax = float.MaxValue;
            t = 0f;
            for (int i = 0; i < 3; i++)
            {
                float oi = Comp(o, i);
                float di = Comp(d, i);
                float mn = Comp(min, i);
                float mx = Comp(max, i);
                if (MathF.Abs(di) < 1e-6f)
                {
                    if (oi < mn || oi > mx) return false;
                }
                else
                {
                    float t1 = (mn - oi) / di;
                    float t2 = (mx - oi) / di;
                    if (t1 > t2)
                    {
                        float tmp = t1;
                        t1 = t2;
                        t2 = tmp;
                    }
                    if (t1 > tmin) tmin = t1;
                    if (t2 < tmax) tmax = t2;
                    if (tmin > tmax) return false;
                }
            }
            t = tmin;
            return true;
        }

        /// <summary>Paprsek (d normalizovany) proti kouli.</summary>
        public static bool RaySphere(Vector3 o, Vector3 d, Vector3 c, float r, out float t)
        {
            t = 0f;
            Vector3 oc = c - o;
            float tca = Vector3.Dot(oc, d);
            float d2 = oc.LengthSquared() - tca * tca;
            if (d2 > r * r) return false;
            float thc = MathF.Sqrt(r * r - d2);
            float t0 = tca - thc;
            float t1 = tca + thc;
            if (t1 < 0f) return false;
            t = t0 < 0f ? 0f : t0;
            return true;
        }

        public static Vector3 Rotate(Vector3 v, Vector3 axis, float angle)
        {
            float c = MathF.Cos(angle);
            float s = MathF.Sin(angle);
            return v * c + Vector3.Cross(axis, v) * s + axis * (Vector3.Dot(axis, v) * (1f - c));
        }
    }
}
