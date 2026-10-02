using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AISandbox
{
    public enum BuildKind { None = 0, Wall, Door, Floor, Roof, Smelter, Debarker, Generator }

    public class SandboxGame : Game
    {
        private static readonly Color SkyColor = new Color(150, 200, 235);
        private static readonly Color GhostOk = new Color(90, 230, 110);
        private static readonly Color GhostBad = new Color(240, 80, 70);

        private readonly GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;
        private Texture2D pixel;
        private BasicEffect effect;
        private Hud hud;

        private Level level;
        private Player player;
        private Inventory inv;
        private readonly Controls controls = new Controls();

        private readonly MeshBuilder dyn = new MeshBuilder();
        private readonly MeshBuilder ghost = new MeshBuilder();
        private readonly MeshBuilder buildMesh = new MeshBuilder();
        private VertexPositionColor[] buildArr = new VertexPositionColor[0];
        private int buildCount;

        private readonly Target target = new Target();
        private readonly PieceHit pieceHit = new PieceHit();

        private bool craftOpen;
        private BuildKind buildKind = BuildKind.None;
        private string message = "";
        private float messageTimer;
        private float swingCooldown;
        private float swingAnim;
        private float progress;
        private string aimText = "";
        private string buildText = "";
        private int screenW = 1280, screenH = 720;

        // vysledek zamereni pro stavbu
        private bool aimValid;
        private int aimOrient, aimGx, aimGz;
        private Vector3 aimPoint;
        private bool aimHasPoint;

        public SandboxGame()
        {
            graphics = new GraphicsDeviceManager(this);
            graphics.IsFullScreen = true;
            graphics.SupportedOrientations = DisplayOrientation.LandscapeLeft | DisplayOrientation.LandscapeRight;
            graphics.GraphicsProfile = GraphicsProfile.Reach;
            graphics.PreferredDepthStencilFormat = DepthFormat.Depth24;
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);
            pixel = new Texture2D(GraphicsDevice, 1, 1);
            pixel.SetData(new[] { Color.White });

            effect = new BasicEffect(GraphicsDevice);
            effect.VertexColorEnabled = true;
            effect.LightingEnabled = false;
            effect.TextureEnabled = false;
            effect.FogEnabled = true;
            effect.FogColor = SkyColor.ToVector3();
            effect.FogStart = 55f;
            effect.FogEnd = 130f;
            effect.World = Matrix.Identity;

            GraphicsDevice.DeviceReset += delegate
            {
                try { pixel.SetData(new[] { Color.White }); }
                catch (Exception) { }
            };

            hud = new Hud(spriteBatch, pixel);

            level = new Level();
            level.Generate(2024);
            level.RebuildDirty(int.MaxValue);

            player = new Player(level.Spawn);
            player.Yaw = 0f;
            inv = new Inventory();

            Say("FIND STICKS AND STONES - CRAFT AN AXE", 6f);
        }

        private void Say(string s)
        {
            Say(s, 2.5f);
        }

        private void Say(string s, float seconds)
        {
            message = s;
            messageTimer = seconds;
        }

        // ================================================================== update

        protected override void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (dt > 0.05f) dt = 0.05f;

            screenW = GraphicsDevice.Viewport.Width;
            screenH = GraphicsDevice.Viewport.Height;

            controls.Update(screenW, screenH, craftOpen, inv.Tools.Count, Recipes.All.Length);

            if (controls.Pressed[(int)Btn.Craft]) craftOpen = !craftOpen;
            if (controls.SlotPressed >= 0 && controls.SlotPressed < inv.Tools.Count)
                inv.SelectedSlot = controls.SlotPressed;

            if (messageTimer > 0f) messageTimer -= dt;
            if (swingCooldown > 0f) swingCooldown -= dt;
            if (swingAnim > 0f) swingAnim -= dt * 3f;

            aimText = "";
            buildText = "";
            progress = 0f;

            if (craftOpen)
            {
                HandleCraftTaps();
                player.Step(level, dt, 0f, 0f, false);
            }
            else
            {
                UpdatePlayer(dt);
                UpdateActions(dt);
            }

            level.Update(dt);
            base.Update(gameTime);
        }

        private void UpdatePlayer(float dt)
        {
            player.Yaw -= controls.Look.X * 0.0045f;
            player.Pitch = MathHelper.Clamp(player.Pitch - controls.Look.Y * 0.0045f, -1.5f, 1.5f);

            Vector3 fwd = player.ForwardFlat;
            Vector3 right = Vector3.Cross(fwd, Vector3.Up);
            Vector3 wish = right * controls.Move.X + fwd * controls.Move.Y;
            float len = wish.Length();
            if (len > 1f) wish = wish / len;

            float speed = 4.4f - 0.35f * inv.Carry;
            player.Step(level, dt, wish.X * speed, wish.Z * speed, controls.Held[(int)Btn.Jump]);

            if (player.Pos.Y < -20f)
            {
                player.Pos = level.Spawn;
                player.VelY = 0f;
            }
        }

        private void UpdateActions(float dt)
        {
            Vector3 eye = player.Eye;
            Vector3 dir = player.Forward;

            if (controls.Pressed[(int)Btn.Build]) CycleBuild();
            if (controls.Pressed[(int)Btn.Mat])
            {
                inv.UsePeeled = !inv.UsePeeled;
                Say(inv.UsePeeled ? "MATERIAL: PEELED LOGS" : "MATERIAL: RAW LOGS");
            }

            // co je pred hracem (pro text a akce)
            level.Find(eye, dir, 3.8f, true, true, target);
            if (target.Kind != TargetKind.None) aimText = level.Describe(target);

            if (buildKind != BuildKind.None)
            {
                UpdateBuildAim(eye, dir);
                if (controls.Pressed[(int)Btn.Put]) TryPlace();
                if (controls.Pressed[(int)Btn.Hit]) TryRemove(eye, dir);
            }
            else
            {
                // tezeni / kaceni
                bool holdHit = controls.Held[(int)Btn.Hit];
                Target hit = new Target();
                level.Find(eye, dir, 3.2f, true, false, hit);
                if (hit.Kind == TargetKind.Tree) progress = 1f - hit.Tree.Hp / Tree.MaxHp;
                else if (hit.Kind == TargetKind.Ore) progress = 1f - hit.Ore.Hp / OreNode.MaxHp;

                if (holdHit && swingCooldown <= 0f)
                {
                    swingCooldown = 0.55f;
                    swingAnim = 1f;
                    string res = "";
                    if (hit.Kind == TargetKind.Tree) res = level.DamageTree(hit.Tree, inv.SelectedTool, player.Pos);
                    else if (hit.Kind == TargetKind.Ore) res = level.DamageOre(hit.Ore, inv.SelectedTool, inv);
                    if (res.Length > 0) Say(res);
                }

                // PUT mimo stavebni mod = odlozit kladu na zem
                if (controls.Pressed[(int)Btn.Put]) DropCarried();
            }

            if (controls.Pressed[(int)Btn.Use] && target.Kind != TargetKind.None && target.Kind != TargetKind.Tree && target.Kind != TargetKind.Ore)
            {
                string res = level.Use(target, inv);
                if (res.Length > 0) Say(res);
            }
        }

        private void DropCarried()
        {
            if (inv.Carry <= 0)
            {
                Say("NOTHING TO DROP");
                return;
            }
            bool peeled = (inv.UsePeeled && inv.Peeled > 0) || inv.Logs == 0;
            if (peeled) inv.Peeled--; else inv.Logs--;
            Vector3 p = player.Pos + player.ForwardFlat * 1.8f;
            p.Y = player.Pos.Y + 1.2f;
            level.DropLog(p, player.Yaw + 1.5708f, peeled);
        }

        // ------------------------------------------------------------------ stavba

        private void CycleBuild()
        {
            for (int i = 1; i <= 8; i++)
            {
                BuildKind k = (BuildKind)(((int)buildKind + i) % 8);
                if (k == BuildKind.Smelter && inv.MachineItems[0] <= 0) continue;
                if (k == BuildKind.Debarker && inv.MachineItems[1] <= 0) continue;
                if (k == BuildKind.Generator && inv.MachineItems[2] <= 0) continue;
                buildKind = k;
                break;
            }
            Say(buildKind == BuildKind.None ? "BUILD MODE OFF" : "BUILD: " + BuildName(buildKind));
        }

        private static string BuildName(BuildKind k)
        {
            switch (k)
            {
                case BuildKind.Wall: return "WALL";
                case BuildKind.Door: return "DOOR";
                case BuildKind.Floor: return "FLOOR";
                case BuildKind.Roof: return "ROOF";
                case BuildKind.Smelter: return "SMELTER";
                case BuildKind.Debarker: return "DEBARKER";
                case BuildKind.Generator: return "GENERATOR";
                default: return "BUILD";
            }
        }

        private static int LogCost(BuildKind k)
        {
            switch (k)
            {
                case BuildKind.Wall: return 1;
                case BuildKind.Door: return 1;
                case BuildKind.Floor: return 2;
                case BuildKind.Roof: return 2;
                default: return 0;
            }
        }

        /// <summary>Vybere, kolik klad a jakeho typu se spotrebuje (preferuje zvoleny material).</summary>
        private bool ChooseLogs(int n, out bool peeled)
        {
            peeled = inv.UsePeeled;
            if (peeled && inv.Peeled >= n) return true;
            if (!peeled && inv.Logs >= n) return true;
            if (peeled && inv.Logs >= n) { peeled = false; return true; }
            if (!peeled && inv.Peeled >= n) { peeled = true; return true; }
            return false;
        }

        private void UpdateBuildAim(Vector3 eye, Vector3 dir)
        {
            aimValid = false;
            aimHasPoint = level.AimPoint(eye, dir, 12f, out aimPoint);
            float groundDist = aimHasPoint ? (aimPoint - eye).Length() : float.MaxValue;
            level.Building.RayPiece(eye, dir, 12f, pieceHit);
            bool pieceFirst = pieceHit.Kind != PieceKind.None && pieceHit.Dist < groundDist;

            buildText = BuildName(buildKind);
            int cost = LogCost(buildKind);
            if (cost > 0) buildText += ": " + cost + " LOG";
            buildText += " - HIT REMOVES";

            switch (buildKind)
            {
                case BuildKind.Wall:
                case BuildKind.Door:
                    if (pieceFirst && pieceHit.Kind == PieceKind.Edge)
                    {
                        aimOrient = pieceHit.Edge.Orient; aimGx = pieceHit.Edge.Gx; aimGz = pieceHit.Edge.Gz; aimValid = true;
                    }
                    else if (aimHasPoint)
                    {
                        Building.AimEdge(aimPoint, out aimOrient, out aimGx, out aimGz); aimValid = true;
                    }
                    break;
                case BuildKind.Floor:
                case BuildKind.Roof:
                    if (aimHasPoint)
                    {
                        Building.AimCell(aimPoint, out aimGx, out aimGz); aimValid = true;
                    }
                    break;
                default:
                    if (aimHasPoint) aimValid = true;
                    break;
            }
        }

        private void TryPlace()
        {
            if (!aimValid)
            {
                Say("AIM AT THE GROUND");
                return;
            }

            string msg;
            bool peeled;
            Terrain t = level.Terrain;
            Building b = level.Building;

            switch (buildKind)
            {
                case BuildKind.Wall:
                    if (!ChooseLogs(1, out peeled)) { Say("NEED 1 LOG"); return; }
                    if (!b.PlaceLog(t, aimOrient, aimGx, aimGz, peeled, out msg)) { Say(msg); return; }
                    ConsumeLogs(1, peeled);
                    break;
                case BuildKind.Door:
                    if (!ChooseLogs(1, out peeled)) { Say("NEED 1 LOG"); return; }
                    if (!b.PlaceDoor(t, aimOrient, aimGx, aimGz, peeled, out msg)) { Say(msg); return; }
                    ConsumeLogs(1, peeled);
                    break;
                case BuildKind.Floor:
                    if (!ChooseLogs(2, out peeled)) { Say("NEED 2 LOGS"); return; }
                    if (!b.PlaceFloor(t, aimGx, aimGz, peeled, out msg)) { Say(msg); return; }
                    ConsumeLogs(2, peeled);
                    break;
                case BuildKind.Roof:
                    if (!ChooseLogs(2, out peeled)) { Say("NEED 2 LOGS"); return; }
                    if (!b.PlaceRoof(t, aimGx, aimGz, peeled, out msg)) { Say(msg); return; }
                    ConsumeLogs(2, peeled);
                    break;
                case BuildKind.Smelter:
                case BuildKind.Debarker:
                case BuildKind.Generator:
                    {
                        int mi = buildKind == BuildKind.Smelter ? 0 : (buildKind == BuildKind.Debarker ? 1 : 2);
                        if (inv.MachineItems[mi] <= 0) { Say("NONE LEFT"); return; }
                        MachineType mt = (MachineType)mi;
                        Machine probe = new Machine();
                        probe.Type = mt;
                        if (!level.MachineSpotFree(aimPoint, probe.Radius)) { Say("NO ROOM HERE"); return; }
                        level.PlaceMachine(mt, aimPoint, player.Yaw);
                        inv.MachineItems[mi]--;
                        Say(BuildName(buildKind) + " PLACED");
                        if (inv.MachineItems[mi] <= 0) buildKind = BuildKind.None;
                        return;
                    }
            }
        }

        private void ConsumeLogs(int n, bool peeled)
        {
            if (peeled) inv.Peeled -= n; else inv.Logs -= n;
        }

        private void TryRemove(Vector3 eye, Vector3 dir)
        {
            // stroj pod zamerovacem -> sbalit
            if (target.Kind == TargetKind.Machine)
            {
                Machine m = target.Machine;
                inv.MachineItems[(int)m.Type]++;
                level.Machines.Remove(m);
                Say(m.Name + " PICKED UP");
                return;
            }

            level.Building.RayPiece(eye, dir, 6f, pieceHit);
            Vector3 at = player.Pos + player.ForwardFlat * 1.8f;
            at.Y = player.Pos.Y + 1.2f;
            bool peeled;
            switch (pieceHit.Kind)
            {
                case PieceKind.Edge:
                    {
                        Edge e = pieceHit.Edge;
                        if (level.Building.RemoveTop(e, out peeled) > 0)
                            level.DropLog(at, player.Yaw + 1.5708f, peeled);
                        break;
                    }
                case PieceKind.Floor:
                    if (level.Building.RemoveFloor(pieceHit.Floor, out peeled))
                    {
                        level.DropLog(at, player.Yaw + 1.5708f, peeled);
                        level.DropLog(at + new Vector3(0f, 0.5f, 0f), player.Yaw, peeled);
                    }
                    break;
                case PieceKind.Roof:
                    if (level.Building.RemoveRoof(pieceHit.Roof, out peeled))
                    {
                        level.DropLog(at, player.Yaw + 1.5708f, peeled);
                        level.DropLog(at + new Vector3(0f, 0.5f, 0f), player.Yaw, peeled);
                    }
                    break;
            }
        }

        // ------------------------------------------------------------------ crafting

        private void HandleCraftTaps()
        {
            for (int t = 0; t < controls.Taps.Count; t++)
            {
                Point tap = controls.Taps[t];
                bool insideRow = false;
                for (int i = 0; i < controls.CraftRows.Length && i < Recipes.All.Length; i++)
                {
                    if (controls.CraftRows[i].Contains(tap))
                    {
                        insideRow = true;
                        TryCraft(Recipes.All[i]);
                    }
                }
                if (!insideRow && !controls.CraftPanel.Contains(tap))
                    craftOpen = false;
            }
        }

        private void TryCraft(Recipe r)
        {
            if (r.ResultTool != null && inv.HasTool(r.ResultTool))
            {
                Say("ALREADY OWNED");
                return;
            }
            if (!inv.CanAfford(r))
            {
                Say("NOT ENOUGH MATERIAL");
                return;
            }
            inv.Pay(r);
            if (r.ResultTool != null)
            {
                inv.Tools.Add(r.ResultTool);
                inv.SelectedSlot = inv.Tools.Count - 1;
            }
            else
            {
                inv.MachineItems[r.ResultMachine]++;
            }
            Say("CRAFTED " + r.Name);
        }

        // ================================================================== kresleni

        private void DrawMesh(VertexPositionColor[] arr, int count)
        {
            if (count < 3) return;
            effect.CurrentTechnique.Passes[0].Apply();
            GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, arr, 0, count / 3);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(SkyColor);

            level.RebuildDirty(3);
            if (level.Building.Dirty)
            {
                level.Building.BuildMesh(buildMesh, level.Terrain);
                buildArr = buildMesh.ToArray();
                buildCount = buildMesh.Count;
            }

            GraphicsDevice.BlendState = BlendState.Opaque;
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            GraphicsDevice.RasterizerState = RasterizerState.CullNone;

            Vector3 eye = player.Eye;
            Vector3 dir = player.Forward;
            effect.View = Matrix.CreateLookAt(eye, eye + dir, Vector3.Up);
            effect.Projection = Matrix.CreatePerspectiveFieldOfView(
                MathHelper.ToRadians(70f), GraphicsDevice.Viewport.AspectRatio, 0.1f, 250f);
            effect.Alpha = 1f;

            // teren + scenerie
            for (int cx = 0; cx < Terrain.ChunkCount; cx++)
                for (int cz = 0; cz < Terrain.ChunkCount; cz++)
                {
                    ChunkMesh c = level.Chunks[cx, cz];
                    Vector3 v = c.Center - eye;
                    if (Vector3.Dot(v, dir) < -30f) continue;
                    if (v.LengthSquared() > 190f * 190f) continue;
                    DrawMesh(c.Terrain, c.TerrainCount);
                    DrawMesh(c.Scenery, c.SceneryCount);
                }

            // stavba
            DrawMesh(buildArr, buildCount);

            // dynamicke objekty (kladdy, stroje, padajici stromy, predmety v ruce)
            dyn.Clear();
            level.BuildDynamic(dyn);
            BuildHeld(dyn, eye, dir);
            DrawMesh(dyn.V, dyn.Count);

            // duch stavby
            if (!craftOpen && buildKind != BuildKind.None && aimValid)
            {
                ghost.Clear();
                BuildGhost(ghost);
                if (ghost.Count >= 3)
                {
                    GraphicsDevice.BlendState = BlendState.AlphaBlend;
                    effect.Alpha = 0.55f;
                    DrawMesh(ghost.V, ghost.Count);
                    effect.Alpha = 1f;
                    GraphicsDevice.BlendState = BlendState.Opaque;
                }
            }

            // HUD
            GraphicsDevice.DepthStencilState = DepthStencilState.None;
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp,
                DepthStencilState.None, RasterizerState.CullNone);
            float alpha = messageTimer > 0f ? messageTimer : 0f;
            hud.Draw(screenW, screenH, controls, inv, craftOpen, aimText, progress, buildText,
                BuildName(buildKind), message, alpha);
            spriteBatch.End();

            base.Draw(gameTime);
        }

        private void BuildGhost(MeshBuilder mb)
        {
            Terrain t = level.Terrain;
            Building b = level.Building;
            bool hasMat = false;
            bool pk;
            int cost = LogCost(buildKind);
            if (cost > 0) hasMat = ChooseLogs(cost, out pk);
            Color col = hasMat || cost == 0 ? GhostOk : GhostBad;

            switch (buildKind)
            {
                case BuildKind.Wall: b.GhostEdgeLog(mb, t, aimOrient, aimGx, aimGz, col); break;
                case BuildKind.Door: b.GhostDoor(mb, t, aimOrient, aimGx, aimGz, col); break;
                case BuildKind.Floor: b.GhostFloor(mb, t, aimGx, aimGz, col); break;
                case BuildKind.Roof: b.GhostRoof(mb, t, aimGx, aimGz, col); break;
                case BuildKind.Smelter:
                case BuildKind.Debarker:
                case BuildKind.Generator:
                    {
                        Machine probe = new Machine();
                        probe.Type = (MachineType)(buildKind == BuildKind.Smelter ? 0 : (buildKind == BuildKind.Debarker ? 1 : 2));
                        Color c2 = level.MachineSpotFree(aimPoint, probe.Radius) ? GhostOk : GhostBad;
                        float r = probe.Radius;
                        mb.BoxYaw(aimPoint + new Vector3(0f, 0.6f, 0f), player.Yaw, new Vector3(r, 0.6f, r), c2);
                        break;
                    }
            }
        }

        /// <summary>Nastroj a nesene kladdy pred kamerou.</summary>
        private void BuildHeld(MeshBuilder mb, Vector3 eye, Vector3 dir)
        {
            Vector3 right = Vector3.Normalize(Vector3.Cross(dir, Vector3.Up));
            Vector3 up = Vector3.Cross(right, dir);

            Tool tool = inv.SelectedTool;
            if (tool.Kind != ToolKind.Hand && !craftOpen)
            {
                float a = 0.35f + swingAnim * 1.1f;
                Vector3 b0 = eye + dir * 0.55f + right * 0.33f - up * 0.32f;
                Vector3 b1 = b0 + up * (0.5f * MathF.Cos(a)) + dir * (0.5f * MathF.Sin(a));
                Color wood = new Color(120, 86, 52);
                mb.Cylinder(b0, b1, 0.025f, 0.022f, 5, wood, wood, true, true);
                Vector3 hd = Vector3.Normalize(b1 - b0);
                Vector3 perp = Vector3.Cross(right, hd);
                bool iron = tool.Power > 1.5f;
                Color head = iron ? new Color(176, 180, 190) : new Color(130, 128, 124);
                if (tool.Kind == ToolKind.Axe)
                    mb.Box(b1, right * 0.015f, hd * 0.07f, perp * 0.12f, head);
                else
                    mb.Box(b1, right * 0.2f, hd * 0.03f, perp * 0.03f, head);
            }

            int n = Math.Min(inv.Carry, 4);
            for (int i = 0; i < n; i++)
            {
                bool peeled = i >= inv.Logs;
                Vector3 c = eye + dir * 0.9f - up * (0.52f - i * 0.0f) + right * (-0.25f + i * 0.0f) + up * (i * 0.1f);
                Color body = peeled ? new Color(206, 172, 118) : new Color(104, 76, 46);
                Color cap = peeled ? new Color(228, 198, 146) : new Color(176, 140, 90);
                mb.Cylinder(c - right * 0.45f, c + right * 0.45f, 0.08f, 0.08f, 6, body, cap, true, true);
            }
        }
    }
}
