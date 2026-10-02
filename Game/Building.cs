using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace AISandbox
{
    public sealed class Edge
    {
        public int Orient;     // 0 = hrana podel osy X, 1 = podel osy Z
        public int Gx;
        public int Gz;
        public float BaseY;
        public int Layers;
        public bool Doorway;
        public readonly bool[] Peeled = new bool[8];
    }

    public sealed class FloorPiece
    {
        public int Gx;
        public int Gz;
        public float Y;
        public bool Peeled;
    }

    public sealed class RoofPiece
    {
        public int Gx;
        public int Gz;
        public float Y;
        public bool Peeled;
    }

    public enum PieceKind { None, Edge, Floor, Roof }

    public sealed class PieceHit
    {
        public PieceKind Kind;
        public Edge Edge;
        public FloorPiece Floor;
        public RoofPiece Roof;
        public float Dist;
    }

    /// <summary>
    /// Stavebni system: skryta mrizka 3 m + skutecne kladdy. Kazda polozena klada je jedna vrstva zdi (srub),
    /// dvere jsou preklad na dvou sloupcich, podlaha a strecha se klada po bunkach mrizky.
    /// </summary>
    public sealed class Building
    {
        public const float C = 3f;
        public const float LH = 0.36f;
        public const float LogR = 0.19f;
        public const int WallLayers = 7;
        public const int DoorLayer = 6;

        public readonly Dictionary<long, Edge> Edges = new Dictionary<long, Edge>();
        public readonly Dictionary<long, FloorPiece> Floors = new Dictionary<long, FloorPiece>();
        public readonly Dictionary<long, RoofPiece> Roofs = new Dictionary<long, RoofPiece>();
        public bool Dirty = true;

        private static readonly Color LogBody = new Color(104, 76, 46);
        private static readonly Color LogCap = new Color(176, 140, 90);
        private static readonly Color PeeledBody = new Color(206, 172, 118);
        private static readonly Color PeeledCap = new Color(228, 198, 146);
        private static readonly Color StoneCol = new Color(112, 108, 102);
        private static readonly Color RoofCol = new Color(118, 64, 46);

        // ------------------------------------------------------------ klice

        public static long Key(int a, int gx, int gz)
        {
            return ((long)a * 4096L + (gx + 8)) * 4096L + (gz + 8);
        }

        public static long CellKey(int gx, int gz)
        {
            return Key(0, gx, gz);
        }

        // ------------------------------------------------------------ zamerovani

        /// <summary>Nejblizsi hrana mrizky k bodu na zemi.</summary>
        public static void AimEdge(Vector3 p, out int orient, out int gx, out int gz)
        {
            float u = p.X / C;
            float v = p.Z / C;
            int i = (int)MathF.Floor(u);
            int j = (int)MathF.Floor(v);
            float fu = u - i;
            float fv = v - j;

            float dLeft = fu, dRight = 1f - fu, dNear = fv, dFar = 1f - fv;
            float m = MathF.Min(MathF.Min(dLeft, dRight), MathF.Min(dNear, dFar));
            if (m == dLeft) { orient = 1; gx = i; gz = j; }
            else if (m == dRight) { orient = 1; gx = i + 1; gz = j; }
            else if (m == dNear) { orient = 0; gx = i; gz = j; }
            else { orient = 0; gx = i; gz = j + 1; }
        }

        public static void AimCell(Vector3 p, out int gx, out int gz)
        {
            gx = (int)MathF.Floor(p.X / C);
            gz = (int)MathF.Floor(p.Z / C);
        }

        private static void EdgeRect(Edge e, out float x0, out float x1, out float z0, out float z1)
        {
            EdgeRect(e.Orient, e.Gx, e.Gz, out x0, out x1, out z0, out z1);
        }

        private static void EdgeRect(int orient, int gx, int gz, out float x0, out float x1, out float z0, out float z1)
        {
            if (orient == 0)
            {
                x0 = gx * C - 0.22f;
                x1 = (gx + 1) * C + 0.22f;
                z0 = gz * C - 0.25f;
                z1 = gz * C + 0.25f;
            }
            else
            {
                x0 = gx * C - 0.25f;
                x1 = gx * C + 0.25f;
                z0 = gz * C - 0.22f;
                z1 = (gz + 1) * C + 0.22f;
            }
        }

        public Edge FindEdge(int orient, int gx, int gz)
        {
            Edge e;
            return Edges.TryGetValue(Key(orient, gx, gz), out e) ? e : null;
        }

        public FloorPiece FloorAt(int gx, int gz)
        {
            FloorPiece f;
            return Floors.TryGetValue(CellKey(gx, gz), out f) ? f : null;
        }

        public RoofPiece RoofAt(int gx, int gz)
        {
            RoofPiece r;
            return Roofs.TryGetValue(CellKey(gx, gz), out r) ? r : null;
        }

        // ------------------------------------------------------------ vysky

        public float EdgeBase(Terrain t, int orient, int gx, int gz)
        {
            float x0, z0, x1, z1;
            if (orient == 0)
            {
                x0 = gx * C; x1 = (gx + 1) * C; z0 = gz * C; z1 = gz * C;
            }
            else
            {
                x0 = gx * C; x1 = gx * C; z0 = gz * C; z1 = (gz + 1) * C;
            }
            float b = MathF.Max(t.HeightAt(x0, z0), MathF.Max(t.HeightAt(x1, z1), t.HeightAt((x0 + x1) / 2f, (z0 + z1) / 2f)));

            FloorPiece fa, fb;
            if (orient == 0) { fa = FloorAt(gx, gz - 1); fb = FloorAt(gx, gz); }
            else { fa = FloorAt(gx - 1, gz); fb = FloorAt(gx, gz); }
            if (fa != null) b = MathF.Max(b, fa.Y);
            if (fb != null) b = MathF.Max(b, fb.Y);
            return MathF.Round(b * 20f) / 20f;
        }

        public float CellBase(Terrain t, int gx, int gz)
        {
            float x0 = gx * C, x1 = (gx + 1) * C, z0 = gz * C, z1 = (gz + 1) * C;
            float b = MathF.Max(MathF.Max(t.HeightAt(x0, z0), t.HeightAt(x1, z0)),
                                MathF.Max(t.HeightAt(x0, z1), t.HeightAt(x1, z1)));
            b = MathF.Max(b, t.HeightAt((x0 + x1) / 2f, (z0 + z1) / 2f));
            return MathF.Round(b * 20f) / 20f;
        }

        // ------------------------------------------------------------ pokladani

        public bool PlaceLog(Terrain t, int orient, int gx, int gz, bool peeled, out string msg)
        {
            msg = "";
            long k = Key(orient, gx, gz);
            Edge e;
            if (!Edges.TryGetValue(k, out e))
            {
                e = new Edge();
                e.Orient = orient;
                e.Gx = gx;
                e.Gz = gz;
                e.BaseY = EdgeBase(t, orient, gx, gz);
            }
            if (e.Doorway) { msg = "DOORWAY"; return false; }
            if (e.Layers >= WallLayers) { msg = "WALL FULL"; return false; }
            e.Peeled[e.Layers] = peeled;
            e.Layers++;
            Edges[k] = e;
            Dirty = true;
            return true;
        }

        public bool PlaceDoor(Terrain t, int orient, int gx, int gz, bool peeled, out string msg)
        {
            msg = "";
            long k = Key(orient, gx, gz);
            if (Edges.ContainsKey(k)) { msg = "EDGE NOT EMPTY"; return false; }
            Edge e = new Edge();
            e.Orient = orient;
            e.Gx = gx;
            e.Gz = gz;
            e.BaseY = EdgeBase(t, orient, gx, gz);
            e.Doorway = true;
            e.Layers = WallLayers;
            e.Peeled[DoorLayer] = peeled;
            Edges[k] = e;
            Dirty = true;
            return true;
        }

        public bool PlaceFloor(Terrain t, int gx, int gz, bool peeled, out string msg)
        {
            msg = "";
            long k = CellKey(gx, gz);
            if (Floors.ContainsKey(k)) { msg = "FLOOR EXISTS"; return false; }
            FloorPiece f = new FloorPiece();
            f.Gx = gx;
            f.Gz = gz;
            f.Y = CellBase(t, gx, gz) + 0.12f;
            f.Peeled = peeled;
            Floors[k] = f;
            Dirty = true;
            return true;
        }

        public bool PlaceRoof(Terrain t, int gx, int gz, bool peeled, out string msg)
        {
            msg = "";
            long k = CellKey(gx, gz);
            if (Roofs.ContainsKey(k)) { msg = "ROOF EXISTS"; return false; }
            RoofPiece r = new RoofPiece();
            r.Gx = gx;
            r.Gz = gz;
            r.Y = CellBase(t, gx, gz) + WallLayers * LH + 0.1f;
            r.Peeled = peeled;
            Roofs[k] = r;
            Dirty = true;
            return true;
        }

        /// <summary>Odebere vrchni kladu hrany (u dveri cele dvere). Vraci pocet odebranych klad a jejich typ.</summary>
        public int RemoveTop(Edge e, out bool peeled)
        {
            peeled = false;
            long k = Key(e.Orient, e.Gx, e.Gz);
            if (e.Doorway)
            {
                peeled = e.Peeled[DoorLayer];
                Edges.Remove(k);
                Dirty = true;
                return 1;
            }
            if (e.Layers <= 0) { Edges.Remove(k); return 0; }
            e.Layers--;
            peeled = e.Peeled[e.Layers];
            if (e.Layers == 0) Edges.Remove(k);
            Dirty = true;
            return 1;
        }

        public bool RemoveFloor(FloorPiece f, out bool peeled)
        {
            peeled = f.Peeled;
            Dirty = true;
            return Floors.Remove(CellKey(f.Gx, f.Gz));
        }

        public bool RemoveRoof(RoofPiece r, out bool peeled)
        {
            peeled = r.Peeled;
            Dirty = true;
            return Roofs.Remove(CellKey(r.Gx, r.Gz));
        }

        // ------------------------------------------------------------ paprsek proti stavbe

        private static bool RayAabb(Vector3 o, Vector3 d, float x0, float x1, float y0, float y1, float z0, float z1, out float t)
        {
            return MathUtil.RayBox(o, d, new Vector3(x0, y0, z0), new Vector3(x1, y1, z1), out t);
        }

        public void RayPiece(Vector3 o, Vector3 d, float maxDist, PieceHit res)
        {
            res.Kind = PieceKind.None;
            res.Edge = null;
            res.Floor = null;
            res.Roof = null;
            res.Dist = maxDist;
            float t;

            foreach (Edge e in Edges.Values)
            {
                float x0, x1, z0, z1;
                EdgeRect(e, out x0, out x1, out z0, out z1);
                float top = e.BaseY + e.Layers * LH + (e.Orient == 1 ? LH * 0.5f : 0f);
                if (RayAabb(o, d, x0, x1, e.BaseY, top, z0, z1, out t) && t < res.Dist)
                {
                    res.Dist = t; res.Kind = PieceKind.Edge; res.Edge = e;
                }
            }
            foreach (FloorPiece f in Floors.Values)
            {
                if (RayAabb(o, d, f.Gx * C, (f.Gx + 1) * C, f.Y - 0.2f, f.Y, f.Gz * C, (f.Gz + 1) * C, out t) && t < res.Dist)
                {
                    res.Dist = t; res.Kind = PieceKind.Floor; res.Floor = f; res.Edge = null; res.Roof = null;
                }
            }
            foreach (RoofPiece r in Roofs.Values)
            {
                if (RayAabb(o, d, r.Gx * C - 0.3f, (r.Gx + 1) * C + 0.3f, r.Y, r.Y + 1f, r.Gz * C - 0.3f, (r.Gz + 1) * C + 0.3f, out t) && t < res.Dist)
                {
                    res.Dist = t; res.Kind = PieceKind.Roof; res.Roof = r; res.Edge = null; res.Floor = null;
                }
            }
        }

        // ------------------------------------------------------------ kolize a povrch

        /// <summary>Vyska povrchu stavby pod nohama (podlaha / vrch zdi). MinValue = nic.</summary>
        public float SurfaceY(float x, float z, float feetY)
        {
            float best = float.MinValue;
            foreach (FloorPiece f in Floors.Values)
            {
                if (x >= f.Gx * C && x <= (f.Gx + 1) * C && z >= f.Gz * C && z <= (f.Gz + 1) * C)
                    if (feetY >= f.Y - 0.55f && f.Y > best) best = f.Y;
            }
            foreach (Edge e in Edges.Values)
            {
                if (e.Doorway) continue;
                float x0, x1, z0, z1;
                EdgeRect(e, out x0, out x1, out z0, out z1);
                if (x >= x0 && x <= x1 && z >= z0 && z <= z1)
                {
                    float top = e.BaseY + e.Layers * LH;
                    if (feetY >= top - 0.55f && top > best) best = top;
                }
            }
            return best;
        }

        private static void PushOut(ref float x, ref float z, float r, float x0, float x1, float z0, float z1)
        {
            float cx = MathHelper.Clamp(x, x0, x1);
            float cz = MathHelper.Clamp(z, z0, z1);
            float dx = x - cx;
            float dz = z - cz;
            float d2 = dx * dx + dz * dz;
            if (d2 >= r * r) return;
            if (d2 > 1e-8f)
            {
                float d = MathF.Sqrt(d2);
                float k = (r - d) / d;
                x += dx * k;
                z += dz * k;
            }
            else
            {
                float l = x - x0, rr = x1 - x, n = z - z0, f = z1 - z;
                float m = MathF.Min(MathF.Min(l, rr), MathF.Min(n, f));
                if (m == l) x = x0 - r;
                else if (m == rr) x = x1 + r;
                else if (m == n) z = z0 - r;
                else z = z1 + r;
            }
        }

        public void Resolve(ref float x, ref float z, float r, float feetY, float height)
        {
            foreach (Edge e in Edges.Values)
            {
                float x0, x1, z0, z1;
                EdgeRect(e, out x0, out x1, out z0, out z1);
                if (x < x0 - 1f || x > x1 + 1f || z < z0 - 1f || z > z1 + 1f) continue;

                if (!e.Doorway)
                {
                    float lo = e.BaseY;
                    float hi = e.BaseY + e.Layers * LH;
                    if (feetY < hi - 0.55f && feetY + height > lo) PushOut(ref x, ref z, r, x0, x1, z0, z1);
                }
                else
                {
                    float postHi = e.BaseY + DoorLayer * LH;
                    float lintelHi = e.BaseY + WallLayers * LH;
                    // sloupky
                    for (int s = 0; s < 2; s++)
                    {
                        float tt = s == 0 ? 0.28f : 0.72f;
                        float px, pz;
                        if (e.Orient == 0) { px = e.Gx * C + tt * C; pz = e.Gz * C; }
                        else { px = e.Gx * C; pz = e.Gz * C + tt * C; }
                        if (feetY < postHi - 0.55f && feetY + height > e.BaseY)
                            PushOut(ref x, ref z, r, px - LogR, px + LogR, pz - LogR, pz + LogR);
                    }
                    // preklad
                    if (feetY < lintelHi - 0.55f && feetY + height > postHi)
                        PushOut(ref x, ref z, r, x0, x1, z0, z1);
                }
            }
        }

        // ------------------------------------------------------------ mesh

        private void EdgeLog(MeshBuilder mb, int orient, int gx, int gz, int layer, float baseY, Color body, Color cap)
        {
            float yc = baseY + LogR + layer * LH + (orient == 1 ? LH * 0.5f : 0f);
            if (orient == 0)
            {
                float z = gz * C;
                mb.Cylinder(new Vector3(gx * C - 0.25f, yc, z), new Vector3((gx + 1) * C + 0.25f, yc, z), LogR, LogR, 7, body, cap, true, true);
            }
            else
            {
                float x = gx * C;
                mb.Cylinder(new Vector3(x, yc, gz * C - 0.25f), new Vector3(x, yc, (gz + 1) * C + 0.25f), LogR, LogR, 7, body, cap, true, true);
            }
        }

        private void Foundation(MeshBuilder mb, Terrain t, int orient, int gx, int gz, float baseY)
        {
            float x0, z0, x1, z1;
            if (orient == 0) { x0 = gx * C; x1 = (gx + 1) * C; z0 = gz * C; z1 = gz * C; }
            else { x0 = gx * C; x1 = gx * C; z0 = gz * C; z1 = (gz + 1) * C; }
            float low = MathF.Min(t.HeightAt(x0, z0), MathF.Min(t.HeightAt(x1, z1), t.HeightAt((x0 + x1) / 2f, (z0 + z1) / 2f))) - 0.5f;
            float hy = (baseY - low) / 2f;
            if (hy < 0.06f) return;
            Vector3 c = new Vector3((x0 + x1) / 2f, (baseY + low) / 2f, (z0 + z1) / 2f);
            if (orient == 0) mb.Box(c, new Vector3(C / 2f, 0f, 0f), new Vector3(0f, hy, 0f), new Vector3(0f, 0f, 0.17f), StoneCol);
            else mb.Box(c, new Vector3(0.17f, 0f, 0f), new Vector3(0f, hy, 0f), new Vector3(0f, 0f, C / 2f), StoneCol);
        }

        private void DoorPosts(MeshBuilder mb, int orient, int gx, int gz, float baseY, Color body, Color cap)
        {
            for (int s = 0; s < 2; s++)
            {
                float tt = s == 0 ? 0.28f : 0.72f;
                float px, pz;
                if (orient == 0) { px = gx * C + tt * C; pz = gz * C; }
                else { px = gx * C; pz = gz * C + tt * C; }
                mb.Cylinder(new Vector3(px, baseY, pz), new Vector3(px, baseY + DoorLayer * LH, pz), LogR, LogR, 7, body, cap, false, true);
            }
        }

        public void FloorMesh(MeshBuilder mb, Terrain t, int gx, int gz, float y, Color body)
        {
            float cx = (gx + 0.5f) * C;
            for (int k = 0; k < 4; k++)
            {
                float z = gz * C + (k + 0.5f) * (C / 4f);
                mb.Box(new Vector3(cx, y - 0.08f, z), new Vector3(C / 2f, 0f, 0f), new Vector3(0f, 0.08f, 0f), new Vector3(0f, 0f, C / 8f - 0.015f), body);
            }
            for (int s = 0; s < 4; s++)
            {
                float px = (s & 1) == 0 ? gx * C + 0.2f : (gx + 1) * C - 0.2f;
                float pz = (s & 2) == 0 ? gz * C + 0.2f : (gz + 1) * C - 0.2f;
                float low = t.HeightAt(px, pz) - 0.3f;
                if (y - 0.16f - low > 0.05f)
                    mb.Cylinder(new Vector3(px, low, pz), new Vector3(px, y - 0.16f, pz), 0.12f, 0.12f, 6, LogBody, LogCap, false, false);
            }
        }

        public void RoofMesh(MeshBuilder mb, int gx, int gz, float y, Color body)
        {
            float x0 = gx * C - 0.3f, x1 = (gx + 1) * C + 0.3f;
            float z0 = gz * C - 0.4f, z1 = (gz + 1) * C + 0.4f;
            float zc = (gz + 0.5f) * C;
            float yr = y + 1.15f;
            Vector3 inside = new Vector3((x0 + x1) / 2f, y + 0.3f, zc);
            mb.Quad(new Vector3(x0, y, z0), new Vector3(x1, y, z0), new Vector3(x1, yr, zc), new Vector3(x0, yr, zc), RoofCol, inside + new Vector3(0f, 0f, 3f));
            mb.Quad(new Vector3(x0, y, z1), new Vector3(x1, y, z1), new Vector3(x1, yr, zc), new Vector3(x0, yr, zc), RoofCol, inside - new Vector3(0f, 0f, 3f));
            // stity
            mb.Tri(new Vector3(x0 + 0.3f, y, gz * C), new Vector3(x0 + 0.3f, y, (gz + 1) * C), new Vector3(x0 + 0.3f, yr - 0.05f, zc), body, inside);
            mb.Tri(new Vector3(x1 - 0.3f, y, gz * C), new Vector3(x1 - 0.3f, y, (gz + 1) * C), new Vector3(x1 - 0.3f, yr - 0.05f, zc), body, inside);
            // hrebenova klada
            mb.Cylinder(new Vector3(x0 - 0.1f, yr + 0.05f, zc), new Vector3(x1 + 0.1f, yr + 0.05f, zc), 0.14f, 0.14f, 6, LogBody, LogCap, true, true);
        }

        public void BuildMesh(MeshBuilder mb, Terrain t)
        {
            mb.Clear();
            foreach (Edge e in Edges.Values)
            {
                Foundation(mb, t, e.Orient, e.Gx, e.Gz, e.BaseY);
                if (e.Doorway)
                {
                    bool p = e.Peeled[DoorLayer];
                    DoorPosts(mb, e.Orient, e.Gx, e.Gz, e.BaseY, p ? PeeledBody : LogBody, p ? PeeledCap : LogCap);
                    EdgeLog(mb, e.Orient, e.Gx, e.Gz, DoorLayer, e.BaseY, p ? PeeledBody : LogBody, p ? PeeledCap : LogCap);
                }
                else
                {
                    for (int k = 0; k < e.Layers; k++)
                    {
                        bool p = e.Peeled[k];
                        EdgeLog(mb, e.Orient, e.Gx, e.Gz, k, e.BaseY, p ? PeeledBody : LogBody, p ? PeeledCap : LogCap);
                    }
                }
            }
            foreach (FloorPiece f in Floors.Values)
                FloorMesh(mb, t, f.Gx, f.Gz, f.Y, f.Peeled ? PeeledBody : new Color(150, 112, 68));
            foreach (RoofPiece r in Roofs.Values)
                RoofMesh(mb, r.Gx, r.Gz, r.Y, r.Peeled ? PeeledBody : LogBody);
            Dirty = false;
        }

        // ------------------------------------------------------------ duch (nahled pred polozenim)

        public void GhostEdgeLog(MeshBuilder mb, Terrain t, int orient, int gx, int gz, Color col)
        {
            Edge e = FindEdge(orient, gx, gz);
            int layer = e != null ? e.Layers : 0;
            float b = e != null ? e.BaseY : EdgeBase(t, orient, gx, gz);
            EdgeLog(mb, orient, gx, gz, layer, b, col, col);
        }

        public void GhostDoor(MeshBuilder mb, Terrain t, int orient, int gx, int gz, Color col)
        {
            float b = EdgeBase(t, orient, gx, gz);
            DoorPosts(mb, orient, gx, gz, b, col, col);
            EdgeLog(mb, orient, gx, gz, DoorLayer, b, col, col);
        }

        public void GhostFloor(MeshBuilder mb, Terrain t, int gx, int gz, Color col)
        {
            FloorMesh(mb, t, gx, gz, CellBase(t, gx, gz) + 0.12f, col);
        }

        public void GhostRoof(MeshBuilder mb, Terrain t, int gx, int gz, Color col)
        {
            RoofMesh(mb, gx, gz, CellBase(t, gx, gz) + WallLayers * LH + 0.1f, col);
        }
    }
}
