using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AISandbox
{
    public sealed class Hud
    {
        private readonly SpriteBatch sb;
        private readonly Texture2D px;

        public Hud(SpriteBatch spriteBatch, Texture2D pixel)
        {
            sb = spriteBatch;
            px = pixel;
        }

        private void Fill(Rectangle r, Color c)
        {
            sb.Draw(px, r, c);
        }

        private void Border(Rectangle r, int th, Color c)
        {
            Fill(new Rectangle(r.X, r.Y, r.Width, th), c);
            Fill(new Rectangle(r.X, r.Bottom - th, r.Width, th), c);
            Fill(new Rectangle(r.X, r.Y, th, r.Height), c);
            Fill(new Rectangle(r.Right - th, r.Y, th, r.Height), c);
        }

        private void Text(string s, int x, int y, int sc, Color c)
        {
            int o = Math.Max(1, sc / 2);
            PixelFont.Draw(sb, px, s, x + o, y + o, sc, Color.Black * 0.7f);
            PixelFont.Draw(sb, px, s, x, y, sc, c);
        }

        private void TextCentered(string s, Rectangle r, int sc, Color c)
        {
            int x = r.X + (r.Width - PixelFont.Width(s, sc)) / 2;
            int y = r.Y + (r.Height - PixelFont.Height(sc)) / 2;
            Text(s, x, y, sc, c);
        }

        private void Button(Rectangle r, string label, bool active, int sc, int bt)
        {
            Fill(r, active ? Color.White * 0.45f : Color.Black * 0.4f);
            Border(r, bt, Color.White * 0.6f);
            TextCentered(label, r, sc, Color.White);
        }

        public void Draw(int w, int h, Controls c, Inventory inv, bool craftOpen, string aimText, float progress,
            string buildText, string buildLabel, string message, float messageAlpha)
        {
            int sc = Math.Max(2, h / 300);
            int bt = Math.Max(2, h / 300);
            int lineH = PixelFont.Height(sc) + sc * 2;
            Color gold = new Color(255, 230, 90);

            // zamerovac
            int cx = w / 2, cy = h / 2;
            int arm = Math.Max(6, h / 60);
            Fill(new Rectangle(cx - arm, cy - bt / 2, arm * 2, bt), Color.White * 0.85f);
            Fill(new Rectangle(cx - bt / 2, cy - arm, bt, arm * 2), Color.White * 0.85f);

            if (progress > 0.001f)
            {
                int bw = h / 6;
                int bh = Math.Max(4, h / 90);
                Rectangle bar = new Rectangle(cx - bw / 2, cy + h / 14, bw, bh);
                Fill(bar, Color.Black * 0.6f);
                Fill(new Rectangle(bar.X, bar.Y, (int)(bw * Math.Min(1f, progress)), bh), Color.White);
            }

            if (!string.IsNullOrEmpty(aimText))
                TextCentered(aimText, new Rectangle(0, (int)(0.60f * h), w, lineH), sc, Color.White);
            if (!string.IsNullOrEmpty(buildText))
                TextCentered(buildText, new Rectangle(0, (int)(0.66f * h), w, lineH), sc, gold);

            // batoh vlevo nahore
            int tx = (int)(0.02f * h);
            int ty = (int)(0.02f * h);
            Array items = Enum.GetValues(typeof(Item));
            foreach (object o in items)
            {
                Item it = (Item)o;
                int cnt = inv.Count(it);
                if (cnt <= 0) continue;
                Text(ItemInfo.Name(it) + " " + cnt, tx, ty, sc, Color.White);
                ty += lineH;
            }
            if (inv.Carry > 0)
            {
                Text("LOGS " + inv.Logs + "  PEELED " + inv.Peeled, tx, ty, sc, gold);
                ty += lineH;
            }
            string[] mname = { "SMELTER", "DEBARKER", "GENERATOR" };
            for (int i = 0; i < 3; i++)
            {
                if (inv.MachineItems[i] <= 0) continue;
                Text(mname[i] + " X" + inv.MachineItems[i], tx, ty, sc, new Color(150, 220, 255));
                ty += lineH;
            }

            // hotbar
            for (int i = 0; i < c.SlotRects.Length && i < inv.Tools.Count; i++)
            {
                Rectangle r = c.SlotRects[i];
                bool sel = i == inv.SelectedSlot;
                Fill(r, sel ? Color.White * 0.35f : Color.Black * 0.4f);
                Border(r, bt, sel ? gold : Color.White * 0.5f);
                TextCentered(inv.Tools[i].Label, r, sc, Color.White);
            }

            Button(c.Rects[(int)Btn.Craft], "CRAFT", craftOpen, sc, bt);
            if (!craftOpen)
            {
                Button(c.Rects[(int)Btn.Hit], "HIT", c.Held[(int)Btn.Hit], sc, bt);
                Button(c.Rects[(int)Btn.Jump], "JUMP", c.Held[(int)Btn.Jump], sc, bt);
                Button(c.Rects[(int)Btn.Use], "USE", false, sc, bt);
                Button(c.Rects[(int)Btn.Put], "PUT", false, sc, bt);
                Button(c.Rects[(int)Btn.Build], buildLabel, false, sc, bt);
                Button(c.Rects[(int)Btn.Mat], inv.UsePeeled ? "PEELED" : "RAW", false, sc, bt);

                int jr = (int)c.JoyRadius;
                Vector2 origin = c.JoyActive ? c.JoyOrigin : c.JoyHint;
                Rectangle baseRect = new Rectangle((int)origin.X - jr, (int)origin.Y - jr, jr * 2, jr * 2);
                Fill(baseRect, Color.White * (c.JoyActive ? 0.15f : 0.08f));
                Border(baseRect, bt, Color.White * 0.35f);
                Vector2 knob = origin;
                if (c.JoyActive)
                {
                    Vector2 d = c.JoyPos - c.JoyOrigin;
                    float len = d.Length();
                    if (len > jr) d *= jr / len;
                    knob = origin + d;
                }
                int kr = jr / 3;
                Fill(new Rectangle((int)knob.X - kr, (int)knob.Y - kr, kr * 2, kr * 2), Color.White * 0.5f);
            }

            if (!string.IsNullOrEmpty(message) && messageAlpha > 0f)
            {
                Color mc = new Color(255, 240, 160) * Math.Min(1f, messageAlpha * 2f);
                TextCentered(message, new Rectangle(0, (int)(0.72f * h), w, lineH), sc, mc);
            }

            if (craftOpen) DrawCraftPanel(c, inv, sc, bt);
        }

        private void DrawCraftPanel(Controls c, Inventory inv, int sc, int bt)
        {
            Fill(c.CraftPanel, Color.Black * 0.8f);
            Border(c.CraftPanel, bt, Color.White * 0.6f);
            int headerH = c.CraftRows.Length > 0 ? c.CraftRows[0].Y - c.CraftPanel.Y : 0;
            Rectangle head = new Rectangle(c.CraftPanel.X, c.CraftPanel.Y, c.CraftPanel.Width, headerH);
            TextCentered("CRAFTING - TAP RECIPE", head, sc, new Color(255, 230, 90));

            for (int i = 0; i < c.CraftRows.Length && i < Recipes.All.Length; i++)
            {
                Recipe rc = Recipes.All[i];
                Rectangle r = c.CraftRows[i];
                bool owned = rc.ResultTool != null && inv.HasTool(rc.ResultTool);
                bool can = !owned && inv.CanAfford(rc);

                Fill(r, can ? new Color(50, 120, 50) * 0.8f : Color.White * 0.12f);
                Border(r, bt, Color.White * 0.4f);

                int pad = r.Height / 5;
                int ty = r.Y + (r.Height - PixelFont.Height(sc)) / 2;
                Color tc = (can || owned) ? Color.White : Color.White * 0.55f;
                Text(rc.Name, r.X + pad, ty, sc, tc);

                string right = owned ? "OWNED" : rc.CostText;
                int rw = PixelFont.Width(right, sc);
                Text(right, r.Right - pad - rw, ty, sc, tc);
            }
        }
    }
}
