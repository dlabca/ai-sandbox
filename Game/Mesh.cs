using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AISandbox
{
    /// <summary>Skladani low-poly meshe s plochym (flat) stinovanim barev na CPU. Zadne textury, zadny Content.</summary>
    public sealed class MeshBuilder
    {
        public VertexPositionColor[] V = new VertexPositionColor[8192];
        public int Count;

        public static readonly Vector3 Sun = Vector3.Normalize(new Vector3(0.5f, 0.85f, 0.35f));

        private readonly Vector3[] ring0 = new Vector3[16];
        private readonly Vector3[] ring1 = new Vector3[16];
        private readonly Vector3[] corner = new Vector3[8];
        private readonly Vector3[] rockV = new Vector3[12];

        private static readonly Vector3[] Ico = BuildIco();

        private static readonly int[] IcoFaces =
        {
            0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11,
            1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
            3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9,
            4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1
        };

        private static readonly int[][] BoxFaces =
        {
            new[] { 2, 3, 7, 6 }, new[] { 0, 1, 5, 4 }, new[] { 1, 3, 7, 5 },
            new[] { 0, 2, 6, 4 }, new[] { 4, 5, 7, 6 }, new[] { 0, 1, 3, 2 }
        };

        private static Vector3[] BuildIco()
        {
            float t = (1f + MathF.Sqrt(5f)) / 2f;
            Vector3[] v =
            {
                new Vector3(-1, t, 0), new Vector3(1, t, 0), new Vector3(-1, -t, 0), new Vector3(1, -t, 0),
                new Vector3(0, -1, t), new Vector3(0, 1, t), new Vector3(0, -1, -t), new Vector3(0, 1, -t),
                new Vector3(t, 0, -1), new Vector3(t, 0, 1), new Vector3(-t, 0, -1), new Vector3(-t, 0, 1)
            };
            for (int i = 0; i < v.Length; i++) v[i] = Vector3.Normalize(v[i]);
            return v;
        }

        public void Clear()
        {
            Count = 0;
        }

        public VertexPositionColor[] ToArray()
        {
            VertexPositionColor[] a = new VertexPositionColor[Count];
            Array.Copy(V, a, Count);
            return a;
        }

        private void Ensure(int n)
        {
            if (Count + n > V.Length) Array.Resize(ref V, Math.Max(V.Length * 2, Count + n));
        }

        /// <summary>Trojuhelnik; hint = bod "uvnitr" telesa, normala se otoci smerem od nej.</summary>
        public void Tri(Vector3 a, Vector3 b, Vector3 c, Color col, Vector3 hint)
        {
            Vector3 n = Vector3.Cross(b - a, c - a);
            float len = n.Length();
            if (len < 1e-9f) return;
            n = n / len;
            Vector3 cen = (a + b + c) / 3f;
            if (Vector3.Dot(n, cen - hint) < 0f) n = -n;
            float shade = 0.5f + 0.5f * MathF.Max(0f, Vector3.Dot(n, Sun));
            Color sc = Noise.Scale(col, shade);
            Ensure(3);
            V[Count++] = new VertexPositionColor(a, sc);
            V[Count++] = new VertexPositionColor(b, sc);
            V[Count++] = new VertexPositionColor(c, sc);
        }

        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color col, Vector3 hint)
        {
            Tri(a, b, c, col, hint);
            Tri(a, c, d, col, hint);
        }

        /// <summary>Kvadr; ax/ay/az jsou pulvektory os.</summary>
        public void Box(Vector3 center, Vector3 ax, Vector3 ay, Vector3 az, Color col)
        {
            for (int i = 0; i < 8; i++)
            {
                Vector3 p = center;
                p = ((i & 1) != 0) ? p + ax : p - ax;
                p = ((i & 2) != 0) ? p + ay : p - ay;
                p = ((i & 4) != 0) ? p + az : p - az;
                corner[i] = p;
            }
            for (int f = 0; f < 6; f++)
            {
                int[] q = BoxFaces[f];
                Quad(corner[q[0]], corner[q[1]], corner[q[2]], corner[q[3]], col, center);
            }
        }

        public void BoxYaw(Vector3 center, float yaw, Vector3 half, Color col)
        {
            Vector3 ax = new Vector3(MathF.Cos(yaw), 0f, -MathF.Sin(yaw)) * half.X;
            Vector3 ay = new Vector3(0f, half.Y, 0f);
            Vector3 az = new Vector3(MathF.Sin(yaw), 0f, MathF.Cos(yaw)) * half.Z;
            Box(center, ax, ay, az, col);
        }

        private void TriAxis(Vector3 a, Vector3 b, Vector3 c, Color col, Vector3 p0, Vector3 an)
        {
            Vector3 cen = (a + b + c) / 3f;
            Vector3 hint = p0 + an * Vector3.Dot(cen - p0, an);
            Tri(a, b, c, col, hint);
        }

        /// <summary>Komoly kuzel / valec mezi dvema body (stromy, kladdy, tycky, komin...).</summary>
        public void Cylinder(Vector3 p0, Vector3 p1, float r0, float r1, int sides, Color col, Color cap,
            bool cap0, bool cap1, float twist = 0f)
        {
            Vector3 axis = p1 - p0;
            float len = axis.Length();
            if (len < 1e-6f) return;
            if (sides > 16) sides = 16;
            Vector3 an = axis / len;
            Vector3 helper = MathF.Abs(an.Y) < 0.95f ? Vector3.Up : new Vector3(1f, 0f, 0f);
            Vector3 u = Vector3.Normalize(Vector3.Cross(an, helper));
            Vector3 v = Vector3.Cross(an, u);

            for (int i = 0; i < sides; i++)
            {
                float a = twist + MathHelper.TwoPi * i / sides;
                Vector3 d = u * MathF.Cos(a) + v * MathF.Sin(a);
                ring0[i] = p0 + d * r0;
                ring1[i] = p1 + d * r1;
            }

            for (int i = 0; i < sides; i++)
            {
                int j = (i + 1) % sides;
                if (r1 < 0.001f)
                {
                    TriAxis(ring0[i], ring0[j], p1, col, p0, an);
                }
                else if (r0 < 0.001f)
                {
                    TriAxis(p0, ring1[j], ring1[i], col, p0, an);
                }
                else
                {
                    TriAxis(ring0[i], ring0[j], ring1[j], col, p0, an);
                    TriAxis(ring0[i], ring1[j], ring1[i], col, p0, an);
                }

                if (cap0 && r0 > 0.001f)
                {
                    Vector3 cen = (p0 + ring0[i] + ring0[j]) / 3f;
                    Tri(p0, ring0[i], ring0[j], cap, cen + an);
                }
                if (cap1 && r1 > 0.001f)
                {
                    Vector3 cen = (p1 + ring1[i] + ring1[j]) / 3f;
                    Tri(p1, ring1[i], ring1[j], cap, cen - an);
                }
            }
        }

        /// <summary>Low-poly skala / koruna - zdeformovany dvacetisten.</summary>
        public void Rock(Vector3 c, float rx, float ry, float rz, int seed, Color col, float jitter = 0.22f)
        {
            for (int i = 0; i < 12; i++)
            {
                float j = 1f - jitter + 2f * jitter * Noise.Hash01(i * 13 + 5, seed, 31);
                rockV[i] = c + new Vector3(Ico[i].X * rx * j, Ico[i].Y * ry * j, Ico[i].Z * rz * j);
            }
            for (int f = 0; f < 20; f++)
            {
                Color fc = Noise.Vary(col, 0.07f, f, seed, 17);
                Tri(rockV[IcoFaces[f * 3]], rockV[IcoFaces[f * 3 + 1]], rockV[IcoFaces[f * 3 + 2]], fc, c);
            }
        }
    }
}
