using System;
using Microsoft.Xna.Framework;

namespace AISandbox
{
    /// <summary>Hladky (ne blokovy) teren: vyskova mapa kreslena jako low-poly trojuhelniky.</summary>
    public sealed class Terrain
    {
        public const int Cells = 100;
        public const float CellSize = 2f;
        public const float Size = Cells * CellSize;
        public const int ChunkCells = 20;
        public const int ChunkCount = Cells / ChunkCells;

        private readonly float[,] h = new float[Cells + 1, Cells + 1];

        public void Generate(int seed)
        {
            float mid = Size / 2f;
            for (int i = 0; i <= Cells; i++)
                for (int j = 0; j <= Cells; j++)
                {
                    float x = i * CellSize;
                    float z = j * CellSize;
                    float n = 0.50f * Noise.ValueNoise(x / 70f, z / 70f, seed)
                            + 0.30f * Noise.ValueNoise(x / 32f, z / 32f, seed + 1)
                            + 0.15f * Noise.ValueNoise(x / 14f, z / 14f, seed + 2)
                            + 0.05f * Noise.ValueNoise(x / 6f, z / 6f, seed + 3);
                    float hh = 2.5f + 26f * n * n;

                    float dx = x - mid;
                    float dz = z - mid;
                    float d = MathF.Sqrt(dx * dx + dz * dz);
                    if (d < 30f)
                    {
                        float t = 1f - d / 30f;
                        t = t * t * (3f - 2f * t);
                        hh = MathHelper.Lerp(hh, 8f, t);
                    }

                    int edge = Math.Min(Math.Min(i, j), Math.Min(Cells - i, Cells - j));
                    if (edge < 5) hh += (5 - edge) * 4f;

                    h[i, j] = hh;
                }
        }

        public float HeightAt(float x, float z)
        {
            if (x < 0f) x = 0f;
            if (z < 0f) z = 0f;
            if (x > Size - 0.001f) x = Size - 0.001f;
            if (z > Size - 0.001f) z = Size - 0.001f;
            float gx = x / CellSize;
            float gz = z / CellSize;
            int i = (int)gx;
            int j = (int)gz;
            float fx = gx - i;
            float fz = gz - j;
            float h00 = h[i, j];
            float h10 = h[i + 1, j];
            float h01 = h[i, j + 1];
            float h11 = h[i + 1, j + 1];
            if (fx >= fz) return h00 + (h10 - h00) * fx + (h11 - h10) * fz;
            return h00 + (h11 - h01) * fx + (h01 - h00) * fz;
        }

        private static Color ColorFor(Vector3 a, Vector3 b, Vector3 c, int i, int j, int tri)
        {
            Vector3 n = Vector3.Cross(b - a, c - a);
            float len = n.Length();
            float ny = len > 1e-6f ? MathF.Abs(n.Y) / len : 1f;
            float avg = (a.Y + b.Y + c.Y) / 3f;
            Color col;
            if (ny < 0.80f) col = new Color(122, 116, 108);
            else if (avg < 4.6f) col = new Color(214, 196, 142);
            else if (avg > 19f) col = new Color(228, 232, 236);
            else col = Noise.Lerp(new Color(92, 154, 64), new Color(66, 120, 54), (avg - 4.6f) / 14f);
            return Noise.Vary(col, 0.07f, i * 2 + tri, j, 77);
        }

        public void BuildChunk(MeshBuilder mb, int cx, int cz)
        {
            mb.Clear();
            for (int ci = 0; ci < ChunkCells; ci++)
                for (int cj = 0; cj < ChunkCells; cj++)
                {
                    int i = cx * ChunkCells + ci;
                    int j = cz * ChunkCells + cj;
                    float x0 = i * CellSize;
                    float x1 = x0 + CellSize;
                    float z0 = j * CellSize;
                    float z1 = z0 + CellSize;
                    Vector3 p00 = new Vector3(x0, h[i, j], z0);
                    Vector3 p10 = new Vector3(x1, h[i + 1, j], z0);
                    Vector3 p11 = new Vector3(x1, h[i + 1, j + 1], z1);
                    Vector3 p01 = new Vector3(x0, h[i, j + 1], z1);

                    Vector3 cenA = (p00 + p10 + p11) / 3f;
                    mb.Tri(p00, p10, p11, ColorFor(p00, p10, p11, i, j, 0), cenA - new Vector3(0f, 5f, 0f));
                    Vector3 cenB = (p00 + p11 + p01) / 3f;
                    mb.Tri(p00, p11, p01, ColorFor(p00, p11, p01, i, j, 1), cenB - new Vector3(0f, 5f, 0f));
                }
        }
    }
}
