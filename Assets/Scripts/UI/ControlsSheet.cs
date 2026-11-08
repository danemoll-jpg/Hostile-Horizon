using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace HostileHorizon
{
    // Fix 6.1: Settings > Controls and the HOTAS setup page. A picture drawn in code (the HOTAS: throttle and stick; or an Xbox-style
    // pad) with a label and a line from every button, axis, hat and the twist to what it does, and (Controls page) the controls grouped
    // and short, action | button, for one device only. Built on the menu canvas, so it is a world-space panel when the menu is (VR rule 3).
    public sealed class ControlsSheet : MonoBehaviour
    {
        public Controls.Device Showing = Controls.Device.Hotas;
        public int Highlight = -1;          // HOTAS symbol index lit on the picture ("identify your buttons")
        public bool PictureOnly;            // HOTAS setup: no list; Report text instead
        public System.Func<string> Report;

        const int W = 760, Hh = 700;
        RawImage pic; Texture2D tex; Color32[] px; string drawnKey;
        readonly List<Text> labels = new List<Text>(); readonly List<Text> marks = new List<Text>();
        readonly List<Text> listTexts = new List<Text>(); Transform root; Text report, note;

        public static ControlsSheet Create(Transform menuRoot, bool pictureOnly)
        {
            var root = UiKit.Rect(menuRoot, pictureOnly ? "HOTAS setup sheet" : "Controls sheet", Vector2.up, Vector2.up, Vector2.zero, Vector2.zero);
            var s = root.gameObject.AddComponent<ControlsSheet>(); s.PictureOnly = pictureOnly; s.root = root;
            var pr = UiKit.Rect(root, "Picture", Vector2.up, Vector2.up, new Vector2(940, -250 - Hh), new Vector2(940 + W, -250));
            s.pic = pr.gameObject.AddComponent<RawImage>(); s.pic.raycastTarget = false;
            s.tex = new Texture2D(W, Hh, TextureFormat.RGBA32, false) { name = "Controls picture", wrapMode = TextureWrapMode.Clamp }; s.px = new Color32[W * Hh];
            s.pic.texture = s.tex;
            s.note = UiKit.Label(root, "Note", 24, TextAnchor.UpperLeft, UiKit.Dim, Vector2.up, Vector2.up, new Vector2(960, -900), new Vector2(1700, -280));
            if (pictureOnly)
                s.report = UiKit.Label(root, "Report", 23, TextAnchor.UpperLeft, UiKit.Ink, Vector2.up, Vector2.up, new Vector2(150, -1000), new Vector2(900, -800));
            root.gameObject.SetActive(false);
            return s;
        }

        // ---- what each HOTAS symbol does now
        static string HotasJob(int k)
        {
            int n = (GameSettings.S.hotasSymbolButtons ?? Controls.DefaultSymbolButtons)[k];
            if (n == 1) return "Fire · select";
            foreach (var h in Controls.HotasActs) if (Controls.HotasButtonFor(h.act) == n) return h.label;
            return null;
        }

        void Update()
        {
            string key = $"{Showing}|{Highlight}|{string.Join(",", GameSettings.S.hotasButtons ?? new int[0])}|{string.Join(",", GameSettings.S.hotasSymbolButtons ?? new int[0])}";
            if (key != drawnKey) { drawnKey = key; Draw(); }
            if (report) report.text = Report?.Invoke() ?? "";
            if (!PictureOnly) List();
        }

        // ---- the grouped list: two columns of groups, each line action | button
        void List()
        {
            var d = Showing; string B(Controls.Act a) => Controls.Btn(a, d);
            var groups = new (string title, (string, string)[] items)[]
            {
                ("Flying", new[] { ("Pitch / roll", B(Controls.Act.PitchRoll)), ("Yaw", B(Controls.Act.Yaw)), ("Throttle", B(Controls.Act.Throttle)), ("Level out", B(Controls.Act.LevelOut)) }),
                ("Landing and pickup", new[] { ("Land / brake", B(Controls.Act.Land)), ("Engines", B(Controls.Act.Engines)), ("Airlock", B(Controls.Act.Airlock)), ("Belly camera", B(Controls.Act.BellyCam)) }),
                ("Combat", new[] { ("Fire (hold)", B(Controls.Act.Fire)), ("Shield", B(Controls.Act.Shield)) }),
                ("Views", new[] { ("Camera view", B(Controls.Act.Camera)), ("Look", B(Controls.Act.Look)), ("Cabin camera", B(Controls.Act.CabinCam)), ("Route target", B(Controls.Act.RouteTarget)) }),
                ("Ship", new[] { ("Recall", B(Controls.Act.Recall)), ("Pause", B(Controls.Act.Pause)) }),
            };
            // one text per cell at a fixed height, so a tall symbol never shifts a line
            int n = 0; var line = new[] { 0, 0 };
            void Cell(int c, int ln, float dx, float w, string text, Color col)
            {
                if (listTexts.Count <= n) listTexts.Add(UiKit.Label(root, "List" + listTexts.Count, 24, TextAnchor.MiddleLeft, UiKit.Ink, Vector2.up, Vector2.up, Vector2.zero, Vector2.zero));
                var t = listTexts[n++]; t.gameObject.SetActive(true); t.text = text; t.color = col; t.horizontalOverflow = HorizontalWrapMode.Overflow;
                float x = 150 + c * 385 + dx, y = -510 - ln * 31;
                t.rectTransform.offsetMin = new Vector2(x, y - 18); t.rectTransform.offsetMax = new Vector2(x + w, y + 18);
            }
            for (int g = 0; g < groups.Length; g++)
            {
                int c = g < 2 ? 0 : 1;
                if (line[c] > 0) line[c]++;
                Cell(c, line[c]++, 0, 370, $"<b>{groups[g].title.ToUpperInvariant()}</b>", UiKit.Accent);
                foreach (var (act, btn) in groups[g].items) { Cell(c, line[c], 0, 185, act, UiKit.Ink); Cell(c, line[c]++, 190, 190, btn, Color.white); }
            }
            for (int k = n; k < listTexts.Count; k++) listTexts[k].gameObject.SetActive(false);
        }

        // ---- drawing (top-down coordinates, 760 x 700)
        static readonly Color Body = new Color(.2f, .21f, .23f), Body2 = new Color(.28f, .29f, .32f), Edge = new Color(.42f, .44f, .47f),
            Btn = new Color(.62f, .64f, .67f), Unused = new Color(.32f, .33f, .35f), LineC = new Color(.55f, .6f, .65f, .9f), Lit = new Color(1f, .82f, .2f);

        void Clear() { for (int i = 0; i < px.Length; i++) px[i] = new Color32(0, 0, 0, 0); }
        void Plot(int x, int y, Color c, float a)
        {
            if (x < 0 || y < 0 || x >= W || y >= Hh || a <= 0) return;
            int i = (Hh - 1 - y) * W + x; Color o = px[i]; float k = Mathf.Clamp01(a * c.a);
            float oa = o.a; float na = k + oa * (1 - k);
            px[i] = na <= 0 ? new Color32(0, 0, 0, 0) : (Color32)new Color((c.r * k + o.r * oa * (1 - k)) / na, (c.g * k + o.g * oa * (1 - k)) / na, (c.b * k + o.b * oa * (1 - k)) / na, na);
        }
        void Shape(float x0, float y0, float x1, float y1, System.Func<float, float, float> sd, Color c)
        {
            for (int y = Mathf.Max(0, (int)y0 - 2); y <= Mathf.Min(Hh - 1, (int)y1 + 2); y++)
                for (int x = Mathf.Max(0, (int)x0 - 2); x <= Mathf.Min(W - 1, (int)x1 + 2); x++)
                    Plot(x, y, c, Mathf.Clamp01(.5f - sd(x + .5f, y + .5f)));
        }
        void RoundRect(float x0, float y0, float x1, float y1, float r, Color c)
        {
            float cx = (x0 + x1) / 2, cy = (y0 + y1) / 2, hx = (x1 - x0) / 2 - r, hy = (y1 - y0) / 2 - r;
            Shape(x0, y0, x1, y1, (x, y) => { float dx = Mathf.Max(Mathf.Abs(x - cx) - hx, 0), dy = Mathf.Max(Mathf.Abs(y - cy) - hy, 0); return Mathf.Sqrt(dx * dx + dy * dy) - r; }, c);
        }
        void Circle(float cx, float cy, float r, Color c) => Shape(cx - r, cy - r, cx + r, cy + r, (x, y) => Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) - r, c);
        void Ring(float cx, float cy, float r, float w, Color c) => Shape(cx - r - w, cy - r - w, cx + r + w, cy + r + w, (x, y) => Mathf.Abs(Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) - r) - w / 2, c);
        void Line(float ax, float ay, float bx, float by, float w, Color c)
        {
            Shape(Mathf.Min(ax, bx), Mathf.Min(ay, by), Mathf.Max(ax, bx), Mathf.Max(ay, by), (x, y) =>
            {
                float px_ = x - ax, py = y - ay, dx = bx - ax, dy = by - ay; float t = Mathf.Clamp01((px_ * dx + py * dy) / Mathf.Max(1e-4f, dx * dx + dy * dy));
                float ex = px_ - t * dx, ey = py - t * dy; return Mathf.Sqrt(ex * ex + ey * ey) - w / 2;
            }, c);
        }
        void Arrow(float ax, float ay, float bx, float by, Color c)
        {
            Line(ax, ay, bx, by, 3, c); var d = new Vector2(bx - ax, by - ay).normalized; var n = new Vector2(-d.y, d.x);
            Line(bx, by, bx - d.x * 10 + n.x * 7, by - d.y * 10 + n.y * 7, 3, c); Line(bx, by, bx - d.x * 10 - n.x * 7, by - d.y * 10 - n.y * 7, 3, c);
        }
        // The printed PlayStation symbols, drawn (the font may not have them)
        void Symbol(string s, float x, float y, Color c)
        {
            if (s == "○") Ring(x, y, 6, 2.4f, c);
            else if (s == "□") { Line(x - 6, y - 6, x + 6, y - 6, 2.4f, c); Line(x + 6, y - 6, x + 6, y + 6, 2.4f, c); Line(x + 6, y + 6, x - 6, y + 6, 2.4f, c); Line(x - 6, y + 6, x - 6, y - 6, 2.4f, c); }
            else if (s == "✕") { Line(x - 6, y - 6, x + 6, y + 6, 2.4f, c); Line(x - 6, y + 6, x + 6, y - 6, 2.4f, c); }
            else if (s == "△") { Line(x, y - 7, x + 7, y + 5, 2.4f, c); Line(x + 7, y + 5, x - 7, y + 5, 2.4f, c); Line(x - 7, y + 5, x, y - 7, 2.4f, c); }
        }

        int used; Text Label(int i)
        {
            while (labels.Count <= i)
                labels.Add(UiKit.Label(pic.transform, "Label" + labels.Count, 22, TextAnchor.MiddleLeft, UiKit.Ink, Vector2.up, Vector2.up, Vector2.zero, Vector2.zero));
            labels[i].gameObject.SetActive(true); return labels[i];
        }
        int markUsed; Text Mark(int i)
        {
            while (marks.Count <= i)
            { var t = UiKit.Label(pic.transform, "Mark" + marks.Count, 15, TextAnchor.MiddleCenter, Color.black, Vector2.up, Vector2.up, Vector2.zero, Vector2.zero); Destroy(t.GetComponent<Shadow>()); marks.Add(t); }
            marks[i].gameObject.SetActive(true); return marks[i];
        }
        // A label at a side slot with a line to (tx, ty)
        void Callout(bool left, float slotY, float tx, float ty, string head, string job, bool lit, bool unused)
        {
            var t = Label(used++); var rt = t.rectTransform;
            float x0 = left ? 0 : 560, x1 = left ? 200 : W;
            rt.offsetMin = new Vector2(x0, -slotY - 26); rt.offsetMax = new Vector2(x1, -slotY + 26);
            t.alignment = left ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;
            string col = lit ? "FFD233" : unused ? "6E7378" : ColorUtility.ToHtmlStringRGB(UiKit.Accent);
            t.text = $"<color=#{col}><b>{head}</b></color>\n<color=#{(unused ? "6E7378" : lit ? "FFE680" : "F0F2F4")}>{job}</color>";
            t.lineSpacing = .9f;
            Line(left ? 206 : 554, slotY, tx, ty, 2, lit ? Lit : unused ? new Color(.4f, .42f, .45f, .8f) : LineC);
            Circle(tx, ty, 3.5f, lit ? Lit : LineC);
        }
        void Letter(float x, float y, string s, Color c) { var m = Mark(markUsed++); m.text = s; m.color = c; m.rectTransform.offsetMin = new Vector2(x - 30, -y - 12); m.rectTransform.offsetMax = new Vector2(x + 30, -y + 12); }

        void Draw()
        {
            Clear(); used = 0; markUsed = 0;
            foreach (var l in labels) l.gameObject.SetActive(false);
            foreach (var m in marks) m.gameObject.SetActive(false);
            note.text = "";
            if (Showing == Controls.Device.Hotas) DrawHotas();
            else if (Showing == Controls.Device.Gamepad) DrawPad();
            else note.text = "<b>Keyboard and mouse</b>\n\nHold the right mouse button and move the mouse to look round.\nThe left mouse button fires too.\n\nEvery key is in the list on the left.";
            tex.SetPixels32(px); tex.Apply(false);
            pic.enabled = Showing != Controls.Device.Keyboard;
        }

        void DrawHotas()
        {
            // the throttle (left) and the stick (right)
            RoundRect(225, 480, 395, 665, 22, Body);
            RoundRect(250, 140, 360, 520, 32, Body2); RoundRect(262, 150, 348, 220, 24, Edge);
            Arrow(238, 430, 238, 330, Btn); Arrow(238, 330, 238, 430, Btn);
            Circle(470, 605, 78, Body); Ring(470, 605, 52, 4, new Color(.25f, .35f, .75f));
            RoundRect(455, 440, 485, 580, 10, Body2);
            RoundRect(422, 190, 518, 465, 36, Body2); RoundRect(418, 140, 522, 235, 32, Edge);
            Arrow(470, 500, 470, 470, Btn); Arrow(470, 500, 500, 500, Btn); Arrow(470, 500, 440, 500, Btn); Arrow(470, 500, 470, 530, Btn);
            Ring(470, 395, 58, 3, new Color(Btn.r, Btn.g, Btn.b, .7f)); Arrow(520, 365, 528, 380, Btn);

            // where each symbol sits: (symbol index, x, y, left side?)
            var at = new (int k, float x, float y)[]
            {
                (3, 290, 172), (4, 330, 205), (5, 280, 255),          // L1 L2 L3
                (9, 318, 300), (6, 292, 328), (8, 344, 328), (7, 318, 356),   // △ □ ○ ✕
                (10, 258, 628), (11, 310, 628),                    // SHARE OPTIONS
                (1, 498, 170), (2, 432, 290), (0, 518, 262),       // R1 R3 R2 (trigger)
            };
            foreach (var (k, x, y) in at)
            {
                bool lit = Highlight == k, unused = HotasJob(k) == null && Highlight < 0;
                var col = lit ? Lit : unused ? Unused : Btn;
                if (k >= 10) RoundRect(x - 20, y - 9, x + 20, y + 9, 9, col); else Circle(x, y, k == 0 ? 13 : 13, col);
                if (lit) Ring(x, y, k >= 10 ? 26 : 22, 4, Lit);
                string s = Controls.HotasSymbols[k];
                if (s.Length == 1) Symbol(s, x, y, new Color(.08f, .09f, .1f)); else if (k < 10) Letter(x, y, s, new Color(.08f, .09f, .1f));
            }
            Circle(360, 628, 10, Unused); Letter(360, 628, "PS", new Color(.08f, .09f, .1f));   // PS: not used (it switches the HOTAS's mode)
            Circle(448, 178, 17, Btn); Line(448, 168, 448, 188, 3, Body); Line(438, 178, 458, 178, 3, Body);   // the hat

            // callouts: throttle on the left, stick on the right, in slot order by height
            string Job(int k) => HotasJob(k) ?? "not used";
            var left = new (int k, float x, float y)[] { (3, 290, 172), (4, 330, 205), (5, 280, 255), (9, 318, 300), (6, 292, 328), (8, 344, 328), (7, 318, 356), (10, 258, 628), (11, 310, 628) };
            float slot = 60;
            foreach (var (k, x, y) in left)
            {
                if (left[4].k == k) { Callout(true, slot, 238, 380, "Throttle lever", "Throttle", false, false); slot += 60; }   // the lever, between the face buttons
                Callout(true, slot, x, y, Controls.SymbolText(k), Job(k), Highlight == k, HotasJob(k) == null); slot += 60;
            }
            Callout(true, slot, 360, 628, "PS", "not used", false, true);
            Callout(false, 70, 448, 178, "Hat", "Look · menus", false, false);
            Callout(false, 160, 498, 170, Controls.SymbolText(1), Job(1), Highlight == 1, HotasJob(1) == null);
            Callout(false, 250, 518, 262, Controls.SymbolText(0) + " (trigger)", Job(0), Highlight == 0, false);
            Callout(false, 340, 432, 290, Controls.SymbolText(2), Job(2), Highlight == 2, HotasJob(2) == null);
            Callout(false, 430, 528, 380, "Twist", "Yaw", false, false);
            Callout(false, 520, 500, 500, "Stick", "Pitch / roll · menus", false, false);
        }

        void DrawPad()
        {
            Circle(250, 455, 80, Body); Circle(510, 455, 80, Body);
            RoundRect(180, 280, 580, 450, 70, Body);
            RoundRect(205, 252, 300, 276, 10, Btn); RoundRect(460, 252, 555, 276, 10, Btn);   // LB RB
            RoundRect(222, 205, 288, 246, 14, Btn); RoundRect(472, 205, 538, 246, 14, Btn);   // LT RT
            Letter(255, 226, "LT", Body); Letter(507, 226, "RT", Body); Letter(252, 264, "LB", Body); Letter(508, 264, "RB", Body);
            Circle(265, 335, 36, Edge); Circle(265, 335, 24, Btn);                              // left stick
            Circle(440, 410, 36, Edge); Circle(440, 410, 24, Btn);                              // right stick
            RoundRect(314, 380, 336, 440, 5, Btn); RoundRect(295, 399, 355, 421, 5, Btn);       // D-pad
            Circle(345, 320, 9, Btn); Circle(415, 320, 9, Btn); Circle(380, 296, 14, Unused);   // View, Menu, the guide button (not used)
            Circle(505, 300, 15, new Color(.95f, .76f, .18f)); Circle(537, 332, 15, new Color(.91f, .28f, .24f));
            Circle(505, 364, 15, new Color(.42f, .76f, .29f)); Circle(473, 332, 15, new Color(.24f, .56f, .91f));
            Letter(505, 300, "Y", Body); Letter(537, 332, "B", Body); Letter(505, 364, "A", Body); Letter(473, 332, "X", Body);

            Callout(true, 70, 230, 226, "LT", "Throttle down", false, false);
            Callout(true, 150, 220, 264, "LB / RB", "Yaw", false, false);
            Callout(true, 230, 345, 320, "View", "Recall (hold) · pause", false, false);
            Callout(true, 310, 240, 335, "Left stick", "Pitch / roll · menus", false, false);
            Callout(true, 390, 252, 352, "Left-stick click", "Level out", false, false);
            Callout(true, 470, 325, 384, "D-pad ↑", "Cabin camera", false, false);
            Callout(true, 540, 299, 410, "D-pad ←", "Shield", false, false);
            Callout(true, 610, 325, 436, "D-pad ↓", "Belly camera", false, false);
            Callout(false, 70, 530, 226, "RT", "Throttle up", false, false);
            Callout(false, 150, 415, 320, "Menu", "Pause", false, false);
            Callout(false, 230, 505, 300, "Y", "Camera view", false, false);
            Callout(false, 310, 537, 332, "B", "Airlock · back", false, false);
            Callout(false, 390, 505, 364, "A", "Fire · engines", false, false);
            Callout(false, 470, 473, 332, "X", "Land / brake", false, false);
            Callout(false, 540, 351, 410, "D-pad →", "Route target", false, false);
            Callout(false, 610, 452, 425, "Right stick", "Look", false, false);
        }
    }
}
