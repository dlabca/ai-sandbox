using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;

namespace AISandbox
{
    public enum Btn { Hit = 0, Jump = 1, Use = 2, Put = 3, Build = 4, Mat = 5, Craft = 6 }

    /// <summary>Dotykove ovladani: joystick vlevo, rozhlizeni tahem vpravo, tlacitka vpravo dole; klavesnice pro test na PC.</summary>
    public sealed class Controls
    {
        public const int BtnCount = 7;

        private sealed class TouchInfo
        {
            public int Role;       // 0 = jine, 1 = joystick, 2 = rozhlizeni, 3 = tlacitko (drzene)
            public int Button;
            public Vector2 Last;
        }

        private readonly Dictionary<int, TouchInfo> touches = new Dictionary<int, TouchInfo>();
        private readonly HashSet<int> present = new HashSet<int>();
        private readonly List<int> stale = new List<int>();
        private KeyboardState prevKeys;

        public readonly bool[] Held = new bool[BtnCount];
        public readonly bool[] Pressed = new bool[BtnCount];
        public readonly Rectangle[] Rects = new Rectangle[BtnCount];

        public Vector2 Move;
        public Vector2 Look;
        public int SlotPressed = -1;
        public readonly List<Point> Taps = new List<Point>();

        public bool JoyActive;
        public Vector2 JoyOrigin;
        public Vector2 JoyPos;
        public Vector2 JoyHint;
        public float JoyRadius;

        public Rectangle CraftPanel;
        public Rectangle[] SlotRects = new Rectangle[0];
        public Rectangle[] CraftRows = new Rectangle[0];

        private int lw = -1, lh = -1, lslots = -1, lrecipes = -1;
        private int screenW, screenH;

        private void Layout(int w, int h, int slots, int recipes)
        {
            if (w == lw && h == lh && slots == lslots && recipes == lrecipes) return;
            lw = w; lh = h; lslots = slots; lrecipes = recipes;
            screenW = w; screenH = h;

            int s = (int)(0.17f * h);
            int g = (int)(0.02f * h);
            int x0 = w - 3 * s - 3 * g;
            int yBottom = h - s - g;
            int yTop = h - 2 * s - 2 * g;

            Rects[(int)Btn.Jump] = new Rectangle(x0, yBottom, s, s);
            Rects[(int)Btn.Use] = new Rectangle(x0 + s + g, yBottom, s, s);
            Rects[(int)Btn.Hit] = new Rectangle(x0 + 2 * (s + g), yBottom, s, s);
            Rects[(int)Btn.Mat] = new Rectangle(x0, yTop, s, s);
            Rects[(int)Btn.Build] = new Rectangle(x0 + s + g, yTop, s, s);
            Rects[(int)Btn.Put] = new Rectangle(x0 + 2 * (s + g), yTop, s, s);
            Rects[(int)Btn.Craft] = new Rectangle((int)(w - 0.26f * h), (int)(0.03f * h), (int)(0.23f * h), (int)(0.11f * h));

            JoyRadius = 0.13f * h;
            JoyHint = new Vector2(0.24f * h, h - 0.30f * h);

            int slotW = (int)(0.15f * h);
            int slotH = (int)(0.09f * h);
            int sgap = (int)(0.01f * h);
            int total = slots * (slotW + sgap) - sgap;
            int sx = (w - total) / 2;
            SlotRects = new Rectangle[slots];
            for (int i = 0; i < slots; i++)
                SlotRects[i] = new Rectangle(sx + i * (slotW + sgap), (int)(0.02f * h), slotW, slotH);

            int rowH = (int)(0.095f * h);
            int rgap = (int)(0.012f * h);
            int headerH = (int)(0.09f * h);
            int panelW = (int)(1.45f * h);
            int panelH = headerH + recipes * (rowH + rgap) + rgap;
            int px = (w - panelW) / 2;
            int py = (h - panelH) / 2;
            CraftPanel = new Rectangle(px, py, panelW, panelH);
            CraftRows = new Rectangle[recipes];
            for (int i = 0; i < recipes; i++)
                CraftRows[i] = new Rectangle(px + rgap, py + headerH + rgap + i * (rowH + rgap), panelW - 2 * rgap, rowH);
        }

        public void Update(int w, int h, bool craftOpen, int slots, int recipes)
        {
            Layout(w, h, slots, recipes);

            Look = Vector2.Zero;
            Move = Vector2.Zero;
            SlotPressed = -1;
            Taps.Clear();
            for (int i = 0; i < BtnCount; i++)
            {
                Pressed[i] = false;
                Held[i] = false;
            }

            TouchCollection tc = TouchPanel.GetState();
            present.Clear();

            foreach (TouchLocation t in tc)
            {
                present.Add(t.Id);
                Vector2 p = t.Position;

                if (t.State == TouchLocationState.Pressed)
                {
                    TouchInfo ti = new TouchInfo();
                    ti.Last = p;
                    ti.Button = -1;
                    AssignRole(ti, p, craftOpen);
                    touches[t.Id] = ti;
                }
                else if (t.State == TouchLocationState.Moved)
                {
                    TouchInfo ti;
                    if (touches.TryGetValue(t.Id, out ti))
                    {
                        if (ti.Role == 2)
                        {
                            Look += p - ti.Last;
                            ti.Last = p;
                        }
                        else if (ti.Role == 1)
                        {
                            JoyPos = p;
                        }
                    }
                }
                else
                {
                    touches.Remove(t.Id);
                }
            }

            stale.Clear();
            foreach (KeyValuePair<int, TouchInfo> kv in touches)
                if (!present.Contains(kv.Key)) stale.Add(kv.Key);
            for (int i = 0; i < stale.Count; i++) touches.Remove(stale[i]);

            bool joy = false;
            foreach (KeyValuePair<int, TouchInfo> kv in touches)
            {
                if (kv.Value.Role == 1) joy = true;
                else if (kv.Value.Role == 3 && kv.Value.Button >= 0) Held[kv.Value.Button] = true;
            }
            JoyActive = joy;

            if (joy)
            {
                Vector2 d = JoyPos - JoyOrigin;
                float len = d.Length();
                if (len > JoyRadius)
                {
                    d *= JoyRadius / len;
                    len = JoyRadius;
                }
                if (len > JoyRadius * 0.15f)
                    Move = new Vector2(d.X / JoyRadius, -d.Y / JoyRadius);
            }

            // klavesnice (test na PC)
            KeyboardState ks = Keyboard.GetState();
            float kx = 0f, ky = 0f;
            if (ks.IsKeyDown(Keys.A)) kx -= 1f;
            if (ks.IsKeyDown(Keys.D)) kx += 1f;
            if (ks.IsKeyDown(Keys.W)) ky += 1f;
            if (ks.IsKeyDown(Keys.S)) ky -= 1f;
            Move += new Vector2(kx, ky);
            if (ks.IsKeyDown(Keys.Space)) Held[(int)Btn.Jump] = true;
            if (ks.IsKeyDown(Keys.E)) Held[(int)Btn.Hit] = true;
            if (ks.IsKeyDown(Keys.Left)) Look.X -= 8f;
            if (ks.IsKeyDown(Keys.Right)) Look.X += 8f;
            if (ks.IsKeyDown(Keys.Up)) Look.Y -= 8f;
            if (ks.IsKeyDown(Keys.Down)) Look.Y += 8f;
            if (KeyPressed(ks, Keys.E)) Pressed[(int)Btn.Hit] = true;
            if (KeyPressed(ks, Keys.F)) Pressed[(int)Btn.Use] = true;
            if (KeyPressed(ks, Keys.Q)) Pressed[(int)Btn.Put] = true;
            if (KeyPressed(ks, Keys.B)) Pressed[(int)Btn.Build] = true;
            if (KeyPressed(ks, Keys.R)) Pressed[(int)Btn.Mat] = true;
            if (KeyPressed(ks, Keys.C)) Pressed[(int)Btn.Craft] = true;
            if (KeyPressed(ks, Keys.D1)) SlotPressed = 0;
            if (KeyPressed(ks, Keys.D2)) SlotPressed = 1;
            if (KeyPressed(ks, Keys.D3)) SlotPressed = 2;
            if (KeyPressed(ks, Keys.D4)) SlotPressed = 3;
            if (KeyPressed(ks, Keys.D5)) SlotPressed = 4;
            prevKeys = ks;

            float ml = Move.Length();
            if (ml > 1f) Move /= ml;
        }

        private bool KeyPressed(KeyboardState ks, Keys k)
        {
            return ks.IsKeyDown(k) && !prevKeys.IsKeyDown(k);
        }

        private void AssignRole(TouchInfo ti, Vector2 p, bool craftOpen)
        {
            Point pt = new Point((int)p.X, (int)p.Y);
            ti.Role = 0;

            if (Rects[(int)Btn.Craft].Contains(pt))
            {
                Pressed[(int)Btn.Craft] = true;
                return;
            }

            if (craftOpen)
            {
                Taps.Add(pt);
                return;
            }

            for (int i = 0; i < BtnCount; i++)
            {
                if (i == (int)Btn.Craft) continue;
                if (!Rects[i].Contains(pt)) continue;
                Pressed[i] = true;
                if (i == (int)Btn.Hit || i == (int)Btn.Jump)
                {
                    ti.Role = 3;
                    ti.Button = i;
                }
                return;
            }

            for (int i = 0; i < SlotRects.Length; i++)
            {
                if (SlotRects[i].Contains(pt))
                {
                    SlotPressed = i;
                    return;
                }
            }

            bool joyTaken = false;
            foreach (KeyValuePair<int, TouchInfo> kv in touches)
                if (kv.Value.Role == 1) joyTaken = true;

            if (!joyTaken && p.X < screenW * 0.42f && p.Y > screenH * 0.2f)
            {
                JoyOrigin = p;
                JoyPos = p;
                ti.Role = 1;
                return;
            }

            ti.Role = 2;
        }
    }
}
