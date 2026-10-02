using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AISandbox
{
    public sealed class ChunkMesh
    {
        public VertexPositionColor[] Terrain = new VertexPositionColor[0];
        public int TerrainCount;
        public VertexPositionColor[] Scenery = new VertexPositionColor[0];
        public int SceneryCount;
        public bool Dirty = true;
        public bool TerrainBuilt;
        public Vector3 Center;
    }

    /// <summary>Cely svet: teren, stromy, rudy, predmety, kladdy, stroje a stavba.</summary>
    public sealed class Level
    {
        public const float PowerRange = 22f;

        public readonly Terrain Terrain = new Terrain();
        public readonly List<Tree> Trees = new List<Tree>();
        public readonly List<OreNode> Ores = new List<OreNode>();
        public readonly List<Pickup> Pickups = new List<Pickup>();
        public readonly List<LogItem> Logs = new List<LogItem>();
        public readonly List<Machine> Machines = new List<Machine>();
        public readonly Building Building = new Building();
        public Vector3 Spawn;

        public readonly ChunkMesh[,] Chunks = new ChunkMesh[Terrain.ChunkCount, Terrain.ChunkCount];

        private readonly MeshBuilder scratch = new MeshBuilder();
        private readonly Random rnd = new Random(99);
        private int seedBase;

        public Level()
        {
            float cs = Terrain.ChunkCells * Terrain.CellSize;
            for (int cx = 0; cx < Terrain.ChunkCount; cx++)
                for (int cz = 0; cz < Terrain.ChunkCount; cz++)
                {
                    ChunkMesh c = new ChunkMesh();
                    c.Center = new Vector3((cx + 0.5f) * cs, 12f, (cz + 0.5f) * cs);
                    Chunks[cx, cz] = c;
                }
        }

        // ================================================================== generovani

        public void Generate(int seed)
        {
            seedBase = seed;
            Terrain.Generate(seed);
            Random r = new Random(seed);
            float mid = Terrain.Size / 2f;
            Spawn = new Vector3(mid, Terrain.HeightAt(mid, mid) + 0.05f, mid);

            // stromy
            int attempts = 0;
            while (Trees.Count < 230 && attempts < 3000)
            {
                attempts++;
                float x = 8f + (float)r.NextDouble() * (Terrain.Size - 16f);
                float z = 8f + (float)r.NextDouble() * (Terrain.Size - 16f);
                float y = Terrain.HeightAt(x, z);
                if (y < 4.8f || y > 17f) continue;
                if (Dist2(x, z, Spawn.X, Spawn.Z) < 9f * 9f) continue;
                if (NearTree(x, z, 4.2f)) continue;
                Tree t = new Tree();
                t.Pos = new Vector3(x, y, z);
                t.Height = 7f + (float)r.NextDouble() * 4.5f;
                t.Variant = r.Next(2);
                t.Seed = r.Next(100000);
                Trees.Add(t);
            }

            // rudy a kameny
            PlaceOres(r, OreType.Stone, 4, 16f, 40f, 0f);
            PlaceOres(r, OreType.Stone, 26, 20f, 140f, 5f);
            PlaceOres(r, OreType.Copper, 3, 38f, 70f, 5f);
            PlaceOres(r, OreType.Copper, 13, 30f, 140f, 7f);
            PlaceOres(r, OreType.Iron, 3, 55f, 90f, 5f);
            PlaceOres(r, OreType.Iron, 13, 40f, 140f, 9f);

            // drobne predmety (klacky, kameny)
            PlacePickups(r, Item.Stick, 8, 5f, 16f);
            PlacePickups(r, Item.Stone, 8, 5f, 16f);
            PlacePickups(r, Item.Stick, 45, 0f, 140f);
            PlacePickups(r, Item.Stone, 45, 0f, 140f);

            for (int cx = 0; cx < Terrain.ChunkCount; cx++)
                for (int cz = 0; cz < Terrain.ChunkCount; cz++)
                    Chunks[cx, cz].Dirty = true;
        }

        private static float Dist2(float ax, float az, float bx, float bz)
        {
            float dx = ax - bx, dz = az - bz;
            return dx * dx + dz * dz;
        }

        private bool NearTree(float x, float z, float d)
        {
            for (int i = 0; i < Trees.Count; i++)
                if (Dist2(x, z, Trees[i].Pos.X, Trees[i].Pos.Z) < d * d) return true;
            return false;
        }

        private bool NearOre(float x, float z, float d)
        {
            for (int i = 0; i < Ores.Count; i++)
                if (Dist2(x, z, Ores[i].Pos.X, Ores[i].Pos.Z) < d * d) return true;
            return false;
        }

        private void PlaceOres(Random r, OreType type, int count, float minD, float maxD, float minH)
        {
            int placed = 0;
            for (int attempt = 0; attempt < 600 && placed < count; attempt++)
            {
                float ang = (float)(r.NextDouble() * Math.PI * 2.0);
                float d = minD + (float)r.NextDouble() * (maxD - minD);
                float x = Spawn.X + MathF.Cos(ang) * d;
                float z = Spawn.Z + MathF.Sin(ang) * d;
                if (x < 8f || z < 8f || x > Terrain.Size - 8f || z > Terrain.Size - 8f) continue;
                float y = Terrain.HeightAt(x, z);
                if (y < minH || y > 22f) continue;
                if (NearTree(x, z, 2.5f) || NearOre(x, z, 4f)) continue;
                OreNode o = new OreNode();
                o.Type = type;
                o.Radius = 0.9f + (float)r.NextDouble() * 0.5f;
                o.Pos = new Vector3(x, y, z);
                o.Seed = r.Next(100000);
                Ores.Add(o);
                placed++;
            }
        }

        private void PlacePickups(Random r, Item type, int count, float minD, float maxD)
        {
            int placed = 0;
            for (int attempt = 0; attempt < 400 && placed < count; attempt++)
            {
                float ang = (float)(r.NextDouble() * Math.PI * 2.0);
                float d = minD + (float)r.NextDouble() * (maxD - minD);
                float x = Spawn.X + MathF.Cos(ang) * d;
                float z = Spawn.Z + MathF.Sin(ang) * d;
                if (x < 6f || z < 6f || x > Terrain.Size - 6f || z > Terrain.Size - 6f) continue;
                float y = Terrain.HeightAt(x, z);
                if (y < 4.6f || y > 20f) continue;
                if (NearTree(x, z, 1.0f) || NearOre(x, z, 1.8f)) continue;
                Pickup p = new Pickup();
                p.Type = type;
                p.Pos = new Vector3(x, y, z);
                p.Seed = r.Next(100000);
                Pickups.Add(p);
                placed++;
            }
        }

        // ================================================================== chunky / mesh

        public static void ChunkOf(float x, float z, out int cx, out int cz)
        {
            float cs = Terrain.ChunkCells * Terrain.CellSize;
            cx = (int)MathHelper.Clamp((float)Math.Floor(x / cs), 0, Terrain.ChunkCount - 1);
            cz = (int)MathHelper.Clamp((float)Math.Floor(z / cs), 0, Terrain.ChunkCount - 1);
        }

        public void MarkChunk(Vector3 p)
        {
            int cx, cz;
            ChunkOf(p.X, p.Z, out cx, out cz);
            Chunks[cx, cz].Dirty = true;
        }

        public void RebuildDirty(int max)
        {
            int done = 0;
            for (int cx = 0; cx < Terrain.ChunkCount && done < max; cx++)
                for (int cz = 0; cz < Terrain.ChunkCount && done < max; cz++)
                {
                    ChunkMesh c = Chunks[cx, cz];
                    if (!c.Dirty) continue;
                    if (!c.TerrainBuilt)
                    {
                        Terrain.BuildChunk(scratch, cx, cz);
                        c.Terrain = scratch.ToArray();
                        c.TerrainCount = scratch.Count;
                        c.TerrainBuilt = true;
                    }
                    BuildScenery(c, cx, cz);
                    c.Dirty = false;
                    done++;
                }
        }

        private void BuildScenery(ChunkMesh c, int cx, int cz)
        {
            scratch.Clear();
            int ox, oz;
            for (int i = 0; i < Trees.Count; i++)
            {
                Tree t = Trees[i];
                ChunkOf(t.Pos.X, t.Pos.Z, out ox, out oz);
                if (ox != cx || oz != cz) continue;
                if (t.State == TreeState.Standing) AddTree(scratch, t, Vector3.Up, 0f);
                else if (t.State == TreeState.Stump)
                    scratch.Cylinder(t.Pos - new Vector3(0f, 0.2f, 0f), t.Pos + new Vector3(0f, 0.5f, 0f), 0.36f, 0.3f, 6,
                        new Color(96, 70, 44), new Color(176, 140, 90), false, true);
            }
            for (int i = 0; i < Ores.Count; i++)
            {
                OreNode o = Ores[i];
                if (!o.Alive) continue;
                ChunkOf(o.Pos.X, o.Pos.Z, out ox, out oz);
                if (ox != cx || oz != cz) continue;
                AddOre(scratch, o);
            }
            for (int i = 0; i < Pickups.Count; i++)
            {
                Pickup p = Pickups[i];
                if (!p.Alive) continue;
                ChunkOf(p.Pos.X, p.Pos.Z, out ox, out oz);
                if (ox != cx || oz != cz) continue;
                AddPickup(scratch, p);
            }
            c.Scenery = scratch.ToArray();
            c.SceneryCount = scratch.Count;
        }

        /// <summary>Strom; kdyz angle != 0, je otoceny kolem paty (padajici strom).</summary>
        public static void AddTree(MeshBuilder mb, Tree t, Vector3 fallAxis, float angle)
        {
            Vector3 b = t.Pos;
            float h = t.Height;
            float th = t.TrunkHeight;
            Color bark = Noise.Vary(new Color(98, 72, 46), 0.08f, t.Seed, 1, 5);
            Color capc = new Color(176, 140, 90);

            mb.Cylinder(b + MathUtil.Rotate(new Vector3(0f, -0.2f, 0f), fallAxis, angle),
                        b + MathUtil.Rotate(new Vector3(0f, th, 0f), fallAxis, angle), 0.34f, 0.2f, 6, bark, capc, false, true);

            Color g1 = Noise.Vary(new Color(46, 112, 62), 0.12f, t.Seed, 2, 6);
            Color g2 = Noise.Vary(new Color(60, 128, 66), 0.12f, t.Seed, 3, 6);
            if (t.Variant == 0)
            {
                mb.Cylinder(b + MathUtil.Rotate(new Vector3(0f, 0.28f * h, 0f), fallAxis, angle),
                            b + MathUtil.Rotate(new Vector3(0f, 0.60f * h, 0f), fallAxis, angle), 2.0f, 0f, 7, g1, g1, false, false);
                mb.Cylinder(b + MathUtil.Rotate(new Vector3(0f, 0.46f * h, 0f), fallAxis, angle),
                            b + MathUtil.Rotate(new Vector3(0f, 0.80f * h, 0f), fallAxis, angle), 1.6f, 0f, 7, g2, g2, false, false);
                mb.Cylinder(b + MathUtil.Rotate(new Vector3(0f, 0.64f * h, 0f), fallAxis, angle),
                            b + MathUtil.Rotate(new Vector3(0f, 1.00f * h, 0f), fallAxis, angle), 1.1f, 0f, 7, g1, g1, false, false);
            }
            else
            {
                mb.Rock(b + MathUtil.Rotate(new Vector3(0f, th + 1.0f, 0f), fallAxis, angle), 2.0f, 1.7f, 2.0f, t.Seed, g2, 0.18f);
                mb.Rock(b + MathUtil.Rotate(new Vector3(0.9f, th + 0.2f, 0.4f), fallAxis, angle), 1.2f, 1.0f, 1.2f, t.Seed + 7, g1, 0.18f);
            }
        }

        private static void AddOre(MeshBuilder mb, OreNode o)
        {
            float r = o.Radius;
            Vector3 c = o.Pos + new Vector3(0f, r * 0.55f, 0f);
            Color baseCol = o.Type == OreType.Stone ? new Color(142, 140, 136) : (o.Type == OreType.Copper ? new Color(116, 110, 104) : new Color(88, 84, 90));
            mb.Rock(c, r, r * 0.8f, r, o.Seed, baseCol, 0.2f);
            if (o.Type == OreType.Stone) return;

            Color f1 = o.Type == OreType.Copper ? new Color(206, 112, 62) : new Color(150, 72, 52);
            Color f2 = o.Type == OreType.Copper ? new Color(80, 176, 140) : new Color(196, 98, 64);
            for (int k = 0; k < 6; k++)
            {
                float a = Noise.Hash01(o.Seed, k, 1) * MathHelper.TwoPi;
                float e = 0.15f + Noise.Hash01(o.Seed, k, 2) * 0.7f;
                Vector3 dir = new Vector3(MathF.Cos(a) * MathF.Cos(e), MathF.Sin(e), MathF.Sin(a) * MathF.Cos(e));
                mb.Rock(c + dir * (r * 0.82f), 0.2f, 0.2f, 0.2f, o.Seed + k, (k & 1) == 0 ? f1 : f2, 0.15f);
            }
        }

        private static void AddPickup(MeshBuilder mb, Pickup p)
        {
            if (p.Type == Item.Stick)
            {
                float yaw = Noise.Hash01(p.Seed, 1, 3) * MathHelper.TwoPi;
                Vector3 d = new Vector3(MathF.Sin(yaw), 0f, MathF.Cos(yaw)) * 0.38f;
                Color col = new Color(112, 82, 50);
                mb.Cylinder(p.Pos - d + new Vector3(0f, 0.05f, 0f), p.Pos + d + new Vector3(0f, 0.09f, 0f), 0.045f, 0.035f, 5, col, col, true, true);
            }
            else
            {
                mb.Rock(p.Pos + new Vector3(0f, 0.1f, 0f), 0.22f, 0.15f, 0.2f, p.Seed, new Color(146, 144, 140), 0.2f);
            }
        }

        // ================================================================== fyzika / povrch

        public float GroundY(float x, float z, float feetY)
        {
            float g = Terrain.HeightAt(x, z);
            float b = Building.SurfaceY(x, z, feetY);
            return b > g ? b : g;
        }

        public void ResolveHorizontal(ref float x, ref float z, float r, float feetY, float height)
        {
            for (int i = 0; i < Trees.Count; i++)
            {
                Tree t = Trees[i];
                if (t.State != TreeState.Standing) continue;
                PushCircle(ref x, ref z, r, t.Pos.X, t.Pos.Z, 0.42f);
            }
            for (int i = 0; i < Ores.Count; i++)
            {
                OreNode o = Ores[i];
                if (!o.Alive) continue;
                PushCircle(ref x, ref z, r, o.Pos.X, o.Pos.Z, o.Radius * 0.85f);
            }
            for (int i = 0; i < Machines.Count; i++)
                PushCircle(ref x, ref z, r, Machines[i].Pos.X, Machines[i].Pos.Z, Machines[i].Radius);

            Building.Resolve(ref x, ref z, r, feetY, height);

            x = MathHelper.Clamp(x, 1.5f, Terrain.Size - 1.5f);
            z = MathHelper.Clamp(z, 1.5f, Terrain.Size - 1.5f);
        }

        private static void PushCircle(ref float x, ref float z, float r, float cx, float cz, float cr)
        {
            float dx = x - cx;
            float dz = z - cz;
            float min = r + cr;
            float d2 = dx * dx + dz * dz;
            if (d2 >= min * min) return;
            if (d2 < 1e-8f)
            {
                x = cx + min;
                return;
            }
            float d = MathF.Sqrt(d2);
            float k = min / d;
            x = cx + dx * k;
            z = cz + dz * k;
        }

        /// <summary>Bod, kam miri paprsek na zem / podlahu (pro stavbu).</summary>
        public bool AimPoint(Vector3 eye, Vector3 dir, float maxDist, out Vector3 p)
        {
            p = eye;
            for (float s = 0.3f; s <= maxDist; s += 0.2f)
            {
                Vector3 q = eye + dir * s;
                float g = GroundY(q.X, q.Z, q.Y);
                if (q.Y <= g)
                {
                    p = new Vector3(q.X, g, q.Z);
                    return true;
                }
            }
            return false;
        }

        // ================================================================== cile (zamerovac)

        public void Find(Vector3 eye, Vector3 dir, float reach, bool forHit, bool forUse, Target res)
        {
            res.Clear(reach);
            float t;
            if (forHit)
            {
                for (int i = 0; i < Trees.Count; i++)
                {
                    Tree tr = Trees[i];
                    if (tr.State != TreeState.Standing) continue;
                    for (int k = 0; k < 3; k++)
                    {
                        Vector3 c = tr.Pos + new Vector3(0f, 1.0f + k * 1.6f, 0f);
                        if (MathUtil.RaySphere(eye, dir, c, 0.65f, out t) && t < res.Dist)
                        {
                            res.Dist = t; res.Kind = TargetKind.Tree; res.Tree = tr; res.Ore = null; res.Pickup = null; res.Log = null; res.Machine = null;
                        }
                    }
                }
                for (int i = 0; i < Ores.Count; i++)
                {
                    OreNode o = Ores[i];
                    if (!o.Alive) continue;
                    Vector3 c = o.Pos + new Vector3(0f, o.Radius * 0.55f, 0f);
                    if (MathUtil.RaySphere(eye, dir, c, o.Radius, out t) && t < res.Dist)
                    {
                        res.Dist = t; res.Kind = TargetKind.Ore; res.Ore = o; res.Tree = null; res.Pickup = null; res.Log = null; res.Machine = null;
                    }
                }
            }
            if (forUse)
            {
                for (int i = 0; i < Logs.Count; i++)
                {
                    LogItem l = Logs[i];
                    for (int k = -1; k <= 1; k++)
                    {
                        Vector3 c = l.Pos + l.Dir * (k * 0.9f) + new Vector3(0f, 0.2f, 0f);
                        if (MathUtil.RaySphere(eye, dir, c, 0.55f, out t) && t < res.Dist)
                        {
                            res.Dist = t; res.Kind = TargetKind.Log; res.Log = l; res.Tree = null; res.Ore = null; res.Pickup = null; res.Machine = null;
                        }
                    }
                }
                for (int i = 0; i < Pickups.Count; i++)
                {
                    Pickup p = Pickups[i];
                    if (!p.Alive) continue;
                    if (MathUtil.RaySphere(eye, dir, p.Pos + new Vector3(0f, 0.15f, 0f), 0.6f, out t) && t < res.Dist)
                    {
                        res.Dist = t; res.Kind = TargetKind.Pickup; res.Pickup = p; res.Tree = null; res.Ore = null; res.Log = null; res.Machine = null;
                    }
                }
                for (int i = 0; i < Machines.Count; i++)
                {
                    Machine m = Machines[i];
                    if (MathUtil.RaySphere(eye, dir, m.Pos + new Vector3(0f, 0.8f, 0f), m.Radius + 0.35f, out t) && t < res.Dist)
                    {
                        res.Dist = t; res.Kind = TargetKind.Machine; res.Machine = m; res.Tree = null; res.Ore = null; res.Pickup = null; res.Log = null;
                    }
                }
            }
        }

        public string Describe(Target t)
        {
            switch (t.Kind)
            {
                case TargetKind.Tree: return "TREE " + (int)MathF.Ceiling(MathF.Max(0f, t.Tree.Hp)) + "/" + (int)Tree.MaxHp;
                case TargetKind.Ore: return t.Ore.Name + " " + (int)MathF.Ceiling(MathF.Max(0f, t.Ore.Hp)) + "/" + (int)OreNode.MaxHp;
                case TargetKind.Pickup: return "USE: " + ItemInfo.Name(t.Pickup.Type);
                case TargetKind.Log: return t.Log.Peeled ? "USE: PEELED LOG" : "USE: LOG";
                case TargetKind.Machine: return DescribeMachine(t.Machine);
                default: return "";
            }
        }

        private string DescribeMachine(Machine m)
        {
            switch (m.Type)
            {
                case MachineType.Smelter:
                    return "SMELTER ORE:" + m.OreCount + " FUEL:" + (int)m.Fuel + "S OUT:" + m.OutCount;
                case MachineType.Generator:
                    return "GENERATOR FUEL:" + (int)m.Fuel + "S " + (m.Running ? "ON" : "OFF");
                default:
                    return "DEBARKER " + (m.Powered ? "POWERED" : "NO POWER") + " QUEUE:" + m.Queue + " OUT:" + m.OutPeeled;
            }
        }

        // ================================================================== akce

        public string DamageTree(Tree t, Tool tool, Vector3 from)
        {
            if (tool.Kind == ToolKind.Hand) return "NEED AXE";
            float dmg = tool.Kind == ToolKind.Axe ? tool.Power : 0.25f;
            t.Hp -= dmg;
            if (t.Hp <= 0f)
            {
                Vector3 d = new Vector3(t.Pos.X - from.X, 0f, t.Pos.Z - from.Z);
                float len = d.Length();
                d = len < 0.01f ? new Vector3(1f, 0f, 0f) : d / len;
                t.FallDir = d;
                t.State = TreeState.Falling;
                t.FallT = 0f;
                MarkChunk(t.Pos);
                return "TIMBER!";
            }
            return "";
        }

        public string DamageOre(OreNode o, Tool tool, Inventory inv)
        {
            if (tool.Kind != ToolKind.Pickaxe) return "NEED PICKAXE";
            o.Hp -= tool.Power;
            if (o.Hp > 0f) return "";

            o.Alive = false;
            MarkChunk(o.Pos);
            switch (o.Type)
            {
                case OreType.Stone: inv.Add(Item.Stone, 3); return "+3 STONE";
                case OreType.Copper: inv.Add(Item.CopperOre, 3); return "+3 COPPER ORE";
                default: inv.Add(Item.IronOre, 3); return "+3 IRON ORE";
            }
        }

        public LogItem DropLog(Vector3 pos, float yaw, bool peeled)
        {
            LogItem l = new LogItem();
            l.Pos = pos;
            l.Yaw = yaw;
            l.Peeled = peeled;
            Logs.Add(l);
            return l;
        }

        public Machine PlaceMachine(MachineType type, Vector3 pos, float yaw)
        {
            Machine m = new Machine();
            m.Type = type;
            m.Pos = pos;
            m.Yaw = yaw;
            Machines.Add(m);
            return m;
        }

        public bool MachineSpotFree(Vector3 p, float radius)
        {
            for (int i = 0; i < Machines.Count; i++)
                if (Dist2(p.X, p.Z, Machines[i].Pos.X, Machines[i].Pos.Z) < (radius + Machines[i].Radius) * (radius + Machines[i].Radius)) return false;
            for (int i = 0; i < Trees.Count; i++)
                if (Trees[i].State == TreeState.Standing && Dist2(p.X, p.Z, Trees[i].Pos.X, Trees[i].Pos.Z) < (radius + 0.5f) * (radius + 0.5f)) return false;
            for (int i = 0; i < Ores.Count; i++)
                if (Ores[i].Alive && Dist2(p.X, p.Z, Ores[i].Pos.X, Ores[i].Pos.Z) < (radius + Ores[i].Radius) * (radius + Ores[i].Radius)) return false;
            return true;
        }

        /// <summary>Pouziti (klavesa USE) - sebrat predmet / kladu nebo obslouzit stroj.</summary>
        public string Use(Target t, Inventory inv)
        {
            switch (t.Kind)
            {
                case TargetKind.Pickup:
                    inv.Add(t.Pickup.Type, 1);
                    t.Pickup.Alive = false;
                    MarkChunk(t.Pickup.Pos);
                    return "+1 " + ItemInfo.Name(t.Pickup.Type);
                case TargetKind.Log:
                    if (inv.Carry >= Inventory.CarryMax) return "HANDS FULL";
                    if (t.Log.Peeled) inv.Peeled++; else inv.Logs++;
                    Logs.Remove(t.Log);
                    return t.Log.Peeled ? "+1 PEELED LOG" : "+1 LOG";
                case TargetKind.Machine:
                    return UseMachine(t.Machine, inv);
                default:
                    return "";
            }
        }

        private static void Join(ref string s, string part)
        {
            if (part.Length == 0) return;
            s = s.Length == 0 ? part : s + "  " + part;
        }

        private string UseMachine(Machine m, Inventory inv)
        {
            string msg = "";
            switch (m.Type)
            {
                case MachineType.Smelter:
                    {
                        if (m.OutCount > 0)
                        {
                            inv.Add(m.OutKind, m.OutCount);
                            Join(ref msg, "+" + m.OutCount + " " + ItemInfo.Name(m.OutKind));
                            m.OutCount = 0;
                        }
                        Item kind = m.OreKind;
                        if (m.OreCount == 0)
                            kind = inv.Count(Item.IronOre) >= inv.Count(Item.CopperOre) ? Item.IronOre : Item.CopperOre;
                        int have = inv.Count(kind);
                        int room = 6 - m.OreCount;
                        int move = Math.Min(have, room);
                        if (move > 0)
                        {
                            inv.Remove(kind, move);
                            m.OreKind = kind;
                            m.OreCount += move;
                            Join(ref msg, "ORE +" + move);
                        }
                        Join(ref msg, Refuel(m, inv, 40f));
                        break;
                    }
                case MachineType.Generator:
                    Join(ref msg, Refuel(m, inv, 100f));
                    break;
                default:
                    {
                        if (m.OutPeeled > 0)
                        {
                            int take = Math.Min(m.OutPeeled, Inventory.CarryMax - inv.Carry);
                            if (take > 0)
                            {
                                inv.Peeled += take;
                                m.OutPeeled -= take;
                                Join(ref msg, "+" + take + " PEELED LOG");
                            }
                            else Join(ref msg, "HANDS FULL");
                        }
                        if (m.OutBark > 0)
                        {
                            inv.Add(Item.Bark, m.OutBark);
                            Join(ref msg, "+" + m.OutBark + " BARK");
                            m.OutBark = 0;
                        }
                        int ins = Math.Min(inv.Logs, 4 - m.Queue);
                        if (ins > 0)
                        {
                            inv.Logs -= ins;
                            m.Queue += ins;
                            Join(ref msg, "LOG +" + ins);
                        }
                        break;
                    }
            }
            return msg.Length == 0 ? "NOTHING TO DO" : msg;
        }

        private static string Refuel(Machine m, Inventory inv, float target)
        {
            float before = m.Fuel;
            while (m.Fuel < target && (inv.Count(Item.Bark) > 0 || inv.Count(Item.Stick) > 0))
            {
                Item f = inv.Count(Item.Bark) > 0 ? Item.Bark : Item.Stick;
                inv.Remove(f, 1);
                m.Fuel += ItemInfo.FuelSeconds(f);
            }
            return m.Fuel > before ? "FUEL " + (int)m.Fuel + "S" : "";
        }

        // ================================================================== simulace

        public void Update(float dt)
        {
            UpdateTrees(dt);
            UpdateLogs(dt);
            UpdateMachines(dt);
        }

        private void UpdateTrees(float dt)
        {
            for (int i = 0; i < Trees.Count; i++)
            {
                Tree t = Trees[i];
                if (t.State != TreeState.Falling) continue;
                t.FallT += dt / 1.6f;
                if (t.FallT < 1f) continue;

                t.State = TreeState.Stump;
                MarkChunk(t.Pos);

                float yaw = MathF.Atan2(t.FallDir.X, t.FallDir.Z);
                int count = t.TrunkHeight > 6f ? 3 : 2;
                for (int k = 0; k < count; k++)
                {
                    float dist = 1.4f + k * 2.8f;
                    Vector3 p = t.Pos + t.FallDir * dist;
                    p.Y = Terrain.HeightAt(p.X, p.Z) + 0.6f;
                    DropLog(p, yaw, false);
                }
                for (int k = 0; k < 3; k++)
                {
                    float a = Noise.Hash01(t.Seed, k, 9) * MathHelper.TwoPi;
                    float d = 1.5f + Noise.Hash01(t.Seed, k, 10) * 3f;
                    Vector3 p = t.Pos + t.FallDir * 2f + new Vector3(MathF.Cos(a) * d * 0.5f, 0f, MathF.Sin(a) * d * 0.5f);
                    p.Y = Terrain.HeightAt(p.X, p.Z);
                    Pickup s = new Pickup();
                    s.Type = Item.Stick;
                    s.Pos = p;
                    s.Seed = t.Seed + k;
                    Pickups.Add(s);
                }
            }
        }

        public void LogEnds(LogItem l, out Vector3 p0, out Vector3 p1)
        {
            Vector3 d = l.Dir * (LogItem.Length / 2f);
            float x0 = l.Pos.X - d.X, z0 = l.Pos.Z - d.Z;
            float x1 = l.Pos.X + d.X, z1 = l.Pos.Z + d.Z;
            if (l.Settled)
            {
                p0 = new Vector3(x0, GroundY(x0, z0, l.Pos.Y) + LogItem.Radius, z0);
                p1 = new Vector3(x1, GroundY(x1, z1, l.Pos.Y) + LogItem.Radius, z1);
            }
            else
            {
                p0 = new Vector3(x0, l.Pos.Y, z0);
                p1 = new Vector3(x1, l.Pos.Y, z1);
            }
        }

        private void UpdateLogs(float dt)
        {
            for (int i = 0; i < Logs.Count; i++)
            {
                LogItem l = Logs[i];
                if (l.Settled) continue;
                l.VelY -= 18f * dt;
                l.Pos.Y += l.VelY * dt;

                Vector3 d = l.Dir * (LogItem.Length / 2f);
                float g0 = GroundY(l.Pos.X - d.X, l.Pos.Z - d.Z, l.Pos.Y);
                float g1 = GroundY(l.Pos.X + d.X, l.Pos.Z + d.Z, l.Pos.Y);
                float rest = (g0 + g1) / 2f + LogItem.Radius;
                if (l.Pos.Y <= rest)
                {
                    l.Pos.Y = rest;
                    l.VelY = 0f;
                    l.Settled = true;
                }
            }
        }

        private void UpdateMachines(float dt)
        {
            for (int i = 0; i < Machines.Count; i++)
            {
                Machine m = Machines[i];
                if (m.Type != MachineType.Generator) continue;
                if (m.Fuel > 0f)
                {
                    m.Fuel -= dt;
                    if (m.Fuel < 0f) m.Fuel = 0f;
                }
                m.Running = m.Fuel > 0f;
                m.Powered = m.Running;
            }

            for (int i = 0; i < Machines.Count; i++)
            {
                Machine m = Machines[i];
                if (m.Type == MachineType.Generator)
                {
                    if (m.Running) m.Anim += dt;
                    continue;
                }

                bool powered = false;
                for (int j = 0; j < Machines.Count; j++)
                {
                    Machine g = Machines[j];
                    if (g.Type == MachineType.Generator && g.Running &&
                        Dist2(g.Pos.X, g.Pos.Z, m.Pos.X, m.Pos.Z) < PowerRange * PowerRange)
                        powered = true;
                }
                m.Powered = powered;

                if (m.Type == MachineType.Smelter)
                {
                    if (m.OreCount > 0 && m.Fuel > 0f)
                    {
                        m.Fuel -= dt;
                        if (m.Fuel < 0f) m.Fuel = 0f;
                        m.Progress += dt;
                        m.Running = true;
                        if (m.Progress >= 5f)
                        {
                            m.Progress = 0f;
                            m.OreCount--;
                            m.OutKind = m.OreKind == Item.IronOre ? Item.Iron : Item.Copper;
                            m.OutCount++;
                        }
                    }
                    else m.Running = false;
                }
                else
                {
                    if (powered && m.Queue > 0)
                    {
                        m.Progress += dt;
                        m.Running = true;
                        if (m.Progress >= 6f)
                        {
                            m.Progress = 0f;
                            m.Queue--;
                            m.OutPeeled++;
                            m.OutBark += 2;
                        }
                    }
                    else m.Running = false;
                }
                if (m.Running) m.Anim += dt;
            }
        }

        // ================================================================== dynamicke meshe

        public void BuildDynamic(MeshBuilder mb)
        {
            for (int i = 0; i < Trees.Count; i++)
            {
                Tree t = Trees[i];
                if (t.State != TreeState.Falling) continue;
                Vector3 axis = Vector3.Normalize(Vector3.Cross(Vector3.Up, t.FallDir));
                float k = MathHelper.Clamp(t.FallT, 0f, 1f);
                AddTree(mb, t, axis, k * k * (MathHelper.Pi / 2f));
            }

            for (int i = 0; i < Logs.Count; i++)
            {
                LogItem l = Logs[i];
                Vector3 p0, p1;
                LogEnds(l, out p0, out p1);
                Color body = l.Peeled ? new Color(206, 172, 118) : new Color(104, 76, 46);
                Color cap = l.Peeled ? new Color(228, 198, 146) : new Color(176, 140, 90);
                mb.Cylinder(p0, p1, LogItem.Radius, LogItem.Radius, 8, body, cap, true, true);
            }

            for (int i = 0; i < Machines.Count; i++) AddMachine(mb, Machines[i]);
        }

        private static void AddMachine(MeshBuilder mb, Machine m)
        {
            Vector3 f = new Vector3(MathF.Sin(m.Yaw), 0f, MathF.Cos(m.Yaw));
            Vector3 r = new Vector3(MathF.Cos(m.Yaw), 0f, -MathF.Sin(m.Yaw));
            Vector3 up = Vector3.Up;
            Vector3 p = m.Pos;

            if (m.Type == MachineType.Smelter)
            {
                Color stone = new Color(132, 128, 122);
                mb.Cylinder(p, p + up * 1.1f, 0.85f, 0.6f, 8, stone, stone, false, true);
                mb.Cylinder(p + up * 1.1f, p + up * 1.9f, 0.32f, 0.24f, 6, new Color(96, 92, 88), new Color(40, 38, 36), false, true);
                Color mouth = m.Running ? new Color(255, 150, 40) : new Color(40, 36, 34);
                mb.Box(p + f * 0.62f + up * 0.45f, r * 0.28f, up * 0.2f, f * 0.12f, mouth);
            }
            else if (m.Type == MachineType.Generator)
            {
                Color body = new Color(74, 112, 92);
                mb.Box(p + up * 0.55f, r * 0.75f, up * 0.45f, f * 0.55f, body);
                mb.Box(p + up * 1.08f, r * 0.55f, up * 0.07f, f * 0.4f, new Color(52, 78, 66));
                mb.Cylinder(p + r * 0.5f + up * 1.0f, p + r * 0.5f + up * 1.7f, 0.12f, 0.1f, 6, new Color(70, 70, 74), new Color(30, 30, 30), false, true);
                Color lamp = m.Running ? new Color(90, 255, 120) : new Color(150, 50, 40);
                mb.Box(p - r * 0.4f + up * 1.2f + f * 0.1f, r * 0.08f, up * 0.08f, f * 0.08f, lamp);
                mb.Box(p + f * 0.58f + up * 0.55f, r * 0.4f, up * 0.2f, f * 0.03f, new Color(200, 170, 60));
            }
            else
            {
                Color frame = new Color(140, 142, 150);
                Color dark = new Color(80, 82, 90);
                mb.Box(p + up * 0.15f, r * 0.95f, up * 0.15f, f * 1.4f, dark);
                for (int s = 0; s < 4; s++)
                {
                    float sx = (s & 1) == 0 ? -0.85f : 0.85f;
                    float sz = (s & 2) == 0 ? -1.25f : 1.25f;
                    Vector3 b = p + r * sx + f * sz;
                    mb.Cylinder(b + up * 0.3f, b + up * 1.35f, 0.07f, 0.07f, 5, frame, frame, false, true);
                }
                Color drum = m.Running ? new Color(222, 186, 120) : new Color(190, 160, 110);
                mb.Cylinder(p + up * 0.85f - f * 1.3f, p + up * 0.85f + f * 1.3f, 0.5f, 0.5f, 6, drum, new Color(120, 90, 60), true, true, m.Anim * 3f);
                mb.Box(p + up * 1.5f, r * 0.9f, up * 0.05f, f * 1.3f, frame);
                Color lamp = m.Powered ? new Color(90, 255, 120) : new Color(150, 50, 40);
                mb.Box(p + r * 0.85f + up * 1.55f + f * 1.2f, r * 0.08f, up * 0.08f, f * 0.08f, lamp);
            }
        }
    }
}
