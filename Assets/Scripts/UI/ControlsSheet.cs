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
        public System.Func<string> Top;     // Fix 6.2: HOTAS setup's big live throttle readout, over the picture

        const int W = 760, Hh = 700;
        RawImage pic; Texture2D tex; Color32[] px; string drawnKey;
        readonly List<Text> labels = new List<Text>(); readonly List<Text> marks = new List<Text>();
        readonly List<Text> listTexts = new List<Text>(); Transform root; Text report, note, top;

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
                s.top = UiKit.Label(root, "Throttle readout", 24, TextAnchor.LowerLeft, UiKit.Ink, Vector2.up, Vector2.up, new Vector2(940, -248), new Vector2(1900, -150));
            if (pictureOnly)
                s.report = UiKit.Label(root, "Report", 23, TextAnchor.UpperLeft, UiKit.Ink, Vector2.up, Vector2.up, new Vector2(150, -1000), new Vector2(900, -800));
            root.gameObject.SetActive(false);
            return s;
        }

        // ---- what each HOTAS symbol does now
        static string HotasJob(int k) => Controls.JobOf((GameSettings.S.hotasSymbolButtons ?? Controls.DefaultSymbolButtons)[k]);
        // Fix 6.3: the Test view (HOTAS setup): every symbol held now is lit
        readonly HashSet<int> held = new HashSet<int>();
        bool IsLit(int k) => Highlight == k || PictureOnly && held.Contains(k);

        void Update()
        {
            held.Clear(); if (PictureOnly && CalStep == 0) foreach (var b in Controls.HotasButtonsHeld()) { int k = Controls.SymbolOf(b); if (k >= 0) held.Add(k); }
            string key = $"{Showing}|{Highlight}|{string.Join(",", held)}|{CalStep}|{string.Join(",", CalReached)}|{string.Join(",", GameSettings.S.hotasButtons ?? new int[0])}|{string.Join(",", GameSettings.S.hotasSymbolButtons ?? new int[0])}";
            if (key != drawnKey) { drawnKey = key; Draw(); }
            if (report) report.text = Report?.Invoke() ?? "";
            if (top) top.text = Top?.Invoke() ?? "";
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
                float x = 150 + c * 385 + dx, y = -570 - ln * 31;
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
        void Letter(float x, float y, string s, Color c) { var m = Mark(markUsed++); m.text = s; m.fontSize = 15; m.color = c; m.rectTransform.offsetMin = new Vector2(x - 30, -y - 12); m.rectTransform.offsetMax = new Vector2(x + 30, -y + 12); }

        void Draw()
        {
            Clear(); used = 0; markUsed = 0;
            foreach (var l in labels) l.gameObject.SetActive(false);
            foreach (var m in marks) m.gameObject.SetActive(false);
            note.text = "";
            if (Showing == Controls.Device.Hotas) DrawHotas();
            else if (Showing == Controls.Device.Gamepad) DrawPad();
            else note.text = "<b>Keyboard and mouse</b>\n\nHold the right mouse button and move the mouse to look around.\nThe left mouse button fires too.\n\nEvery key is in the list on the left.";
            tex.SetPixels32(px); tex.Apply(false);
            pic.enabled = Showing != Controls.Device.Keyboard;
        }

        // Fix 6.2: the HOTAS as it is (Dan: "the layout on the screen is not accurate"). Our own drawing after the idea of Thrustmaster's
        // mapping poster (reference only, SourceArt/Reference): from the front, the throttle on the left, the stick on the right, the
        // base between them with SHARE / PS / OPTIONS; close-ups where buttons can't be seen from the front: the throttle's right side
        // (square, cross, circle, triangle top to bottom, the rocker below), the throttle's back (R2 high, L2 low), the stick's head
        // (hat, L1 the blue one, L3) and the stick from the side (R3 at the back of the head; R1, the trigger: Fix 6.3, Dan's words).
        // Each printed symbol has its place; what it does comes from the button it sends (identify / assign).
        static readonly Color Ink2 = new Color(.08f, .09f, .1f), Blue = new Color(.22f, .42f, .9f), Frame = new Color(.36f, .38f, .42f, .9f);
        // where each printed symbol is (HotasSymbols order: R2 R1 R3 L1 L2 L3 square cross circle triangle SHARE OPTIONS)
        static readonly Vector2[] SymbolAt =
        {
            new Vector2(290, 616), new Vector2(424, 628), new Vector2(512, 582), new Vector2(470, 480), new Vector2(290, 662), new Vector2(514, 438),
            new Vector2(300, 398), new Vector2(300, 430), new Vector2(300, 462), new Vector2(300, 494), new Vector2(366, 298), new Vector2(424, 298),
        };
        static readonly Vector2 LeverAt = new Vector2(286, 150), RockerAt = new Vector2(300, 530), StickAt = new Vector2(480, 150), TwistAt = new Vector2(514, 222),
            HatAt = new Vector2(430, 412), PsAt = new Vector2(395, 280);

        void Caption(float x0, float y0, float x1, string s)
        {
            var m = Mark(markUsed++); m.text = s; m.color = UiKit.Dim; m.fontSize = 20;
            m.rectTransform.offsetMin = new Vector2(x0, -y0 - 12); m.rectTransform.offsetMax = new Vector2(x1, -y0 + 12);
        }
        void Inset(float x0, float y0, float x1, float y1, string caption)
        {
            RoundRect(x0, y0, x1, y1, 12, new Color(.1f, .11f, .12f, .85f));
            for (int i = 0; i < 2; i++) { Line(x0 + 6, y0, x1 - 6, y0, 1.5f, Frame); Line(x0 + 6, y1, x1 - 6, y1, 1.5f, Frame); Line(x0, y0 + 6, x0, y1 - 6, 1.5f, Frame); Line(x1, y0 + 6, x1, y1 - 6, 1.5f, Frame); }
            Caption(x0, y0 + 13, x1, caption);
        }
        void Button(int k, bool pill = false, bool blue = false)
        {
            var p = SymbolAt[k]; bool lit = IsLit(k), unused = HotasJob(k) == null && Highlight < 0;
            var col = lit ? Lit : unused ? Unused : blue ? Blue : Btn;
            if (pill) RoundRect(p.x - 19, p.y - 9, p.x + 19, p.y + 9, 9, col);
            else if (k == 0 || k == 4) RoundRect(p.x - 24, p.y - 12, p.x + 24, p.y + 12, 8, col);
            else Circle(p.x, p.y, 15, col);
            if (lit) Ring(p.x, p.y, pill || k == 0 || k == 4 ? 28 : 21, 4, Lit);
            string s = Controls.HotasSymbols[k];
            if (s.Length == 1) Symbol(s, p.x, p.y, Ink2); else if (!pill) Letter(p.x, p.y, s, blue && !lit ? Color.white : Ink2);
        }

        void DrawHotas()
        {
            if (CalStep > 0) { DrawCalibration(); return; }
            // ---- from the front: the base (throttle half on the left, the panel, the stick's round base), the throttle grip, the stick
            RoundRect(214, 222, 372, 322, 44, Body);                                        // the throttle's half of the base
            RoundRect(232, 126, 336, 236, 30, Body2); RoundRect(222, 92, 344, 162, 32, Edge); // the throttle grip and its head
            Arrow(228, 200, 228, 120, Btn); Arrow(228, 120, 228, 200, Btn);                 // the lever goes forward / back
            Circle(480, 274, 74, Body); Ring(480, 274, 48, 4, Blue);                        // the stick's base and its blue ring
            RoundRect(340, 262, 448, 318, 12, Body);                                        // the panel between
            RoundRect(467, 186, 493, 262, 10, Body2); RoundRect(448, 92, 512, 206, 26, Body2); RoundRect(442, 44, 518, 110, 22, Edge);   // shaft, grip, head
            Circle(458, 70, 7, Btn); RoundRect(474, 64, 486, 84, 3, Blue); Circle(502, 70, 6, Btn);   // hat, L1, L3 (close-up below)
            Arrow(480, 150, 480, 128, Ink2); Arrow(480, 150, 480, 172, Ink2); Arrow(480, 150, 460, 150, Ink2); Arrow(480, 150, 500, 150, Ink2);
            Ring(480, 222, 34, 3, new Color(Btn.r, Btn.g, Btn.b, .75f)); Arrow(506, 200, 514, 214, Btn);   // the twist
            Button(10, true); Button(11, true);
            Circle(PsAt.x, PsAt.y, 9, Unused); Letter(PsAt.x, PsAt.y, "PS", Ink2);          // PS: not used (it switches the HOTAS's mode)

            // ---- close-ups
            Inset(205, 350, 376, 550, "THROTTLE SIDE");
            RoundRect(262, 378, 340, 544, 30, Body2);
            for (int k = 6; k <= 9; k++) Button(k);
            RoundRect(RockerAt.x - 26, RockerAt.y - 9, RockerAt.x + 26, RockerAt.y + 9, 8, Btn);
            Arrow(RockerAt.x, RockerAt.y, RockerAt.x - 22, RockerAt.y, Ink2); Arrow(RockerAt.x, RockerAt.y, RockerAt.x + 22, RockerAt.y, Ink2);
            Inset(205, 562, 376, 690, "THROTTLE BACK");
            RoundRect(250, 588, 330, 686, 28, Body2);
            Button(0); Button(4); Letter(SymbolAt[0].x, SymbolAt[0].y, "R2", Ink2); Letter(SymbolAt[4].x, SymbolAt[4].y, "L2", Ink2);
            Inset(386, 350, 555, 520, "STICK HEAD");
            RoundRect(400, 380, 542, 510, 40, Edge);
            Circle(HatAt.x, HatAt.y, 20, Btn); Line(HatAt.x, HatAt.y - 12, HatAt.x, HatAt.y + 12, 4, Body); Line(HatAt.x - 12, HatAt.y, HatAt.x + 12, HatAt.y, 4, Body);
            Button(3, false, true); Button(5);
            Inset(386, 530, 555, 690, "STICK SIDE");
            RoundRect(446, 558, 532, 608, 22, Edge); RoundRect(452, 598, 506, 684, 22, Body2);   // the head and the grip, seen from the left
            Line(424, 608, 452, 608, 3, Body2); Line(424, 608, 424, 650, 3, Body2);             // the trigger guard
            Button(1); Button(2);
            Caption(386, 682, 470, "← front");

            // ---- labels: the throttle and the base's left on the left; the stick on the right (a symbol's job: its button's)
            string Job(int k) => HotasJob(k) ?? "not used";
            var left = new System.Collections.Generic.List<(string head, string job, Vector2 at, bool lit, bool unused)>
            {
                ("Throttle lever", "Throttle", LeverAt, false, false), Sym(10), Sym(6), Sym(7), Sym(8), Sym(9),
                ("Rocker", "Yaw (as the twist)", RockerAt, false, false), Sym(0), Sym(4),
            };
            var right = new System.Collections.Generic.List<(string head, string job, Vector2 at, bool lit, bool unused)>
            {
                ("Stick", "Pitch / roll · menus", StickAt, false, false), ("Twist", "Yaw", TwistAt, false, false), Sym(11),
                ("Hat", "Look · menus", HatAt, false, false), Sym(5), Sym(3), Sym(2),
                Sym(1, " (trigger)"),
            };
            (string, string, Vector2, bool, bool) Sym(int k, string extra = "") => (Controls.SymbolText(k) + extra, Job(k), SymbolAt[k], IsLit(k), HotasJob(k) == null);
            foreach (var (side, list) in new[] { (true, left), (false, right) })
            {
                var y = new float[list.Count];
                for (int i = 0; i < y.Length; i++) y[i] = Mathf.Max(list[i].at.y, i > 0 ? y[i - 1] + 70 : 34);
                for (int i = y.Length - 1; i >= 0; i--) y[i] = Mathf.Min(y[i], i < y.Length - 1 ? y[i + 1] - 70 : 666);
                for (int i = 0; i < y.Length; i++) Callout(side, y[i], list[i].at.x, list[i].at.y, list[i].head, list[i].job, list[i].lit, list[i].unused);
            }
        }

        // Fix 6.2: calibration, step by step: the stick seen from above, a marker at each edge that turns green when reached, the stick's
        // position as a dot (Controls page's calibration only; drawn when the step or a marker changes, the dot moves every frame).
        public int CalStep;                      // 0 off, 1 let go, 2 each edge, 3 press the trigger
        public readonly bool[] CalReached = new bool[4];
        public Vector2 CalDot;
        RectTransform dot;
        static readonly Vector2 CalC = new Vector2(380, 330); const float CalR = 190;
        void DrawCalibration()
        {
            Caption(180, 40, 580, "THE STICK, SEEN FROM ABOVE");
            Circle(CalC.x, CalC.y, CalR + 20, Body); Ring(CalC.x, CalC.y, CalR, 3, Edge);
            Line(CalC.x - CalR, CalC.y, CalC.x + CalR, CalC.y, 1.5f, Frame); Line(CalC.x, CalC.y - CalR, CalC.x, CalC.y + CalR, 1.5f, Frame);
            Circle(CalC.x, CalC.y, 16, Body2); Ring(CalC.x, CalC.y, 16, 2, Edge);
            var green = new Color(.35f, .85f, .4f);
            var marks = new[] { (new Vector2(0, -1), "FORWARD"), (new Vector2(0, 1), "BACK"), (new Vector2(-1, 0), "LEFT"), (new Vector2(1, 0), "RIGHT") };
            for (int k = 0; k < 4; k++)
            {
                var (d, name) = marks[k]; var p = CalC + d * (CalR + 2); bool ok = CalReached[k] && CalStep >= 2;
                Circle(p.x, p.y, 24, ok ? green : Unused); Arrow(p.x - d.x * 10, p.y - d.y * 10, p.x + d.x * 12, p.y + d.y * 12, ok ? Ink2 : Btn);
                var lp = CalC + d * (CalR + (d.x != 0 ? 100 : 56));
                Caption(lp.x - 90, lp.y, lp.x + 90, ok ? name + "  " + Controls.Glyph("✓", "(done)") : name);
            }
            Caption(180, 660, 580, CalStep == 1 ? "Step 1 of 3: hands off the stick" : CalStep == 2 ? "Step 2 of 3: reach every marker" : $"Step 3 of 3: press {Controls.HotasName(Controls.FireButton)} (the trigger)");
        }

        void LateUpdate()
        {
            bool on = CalStep > 0 && Showing == Controls.Device.Hotas;
            if (on && dot == null)
            {
                dot = UiKit.Rect(pic.transform, "Stick dot", Vector2.up, Vector2.up, Vector2.zero, Vector2.zero);
                var img = dot.gameObject.AddComponent<Image>(); img.color = Lit; img.raycastTarget = false;
            }
            if (dot == null) return;
            dot.gameObject.SetActive(on);
            if (!on) return;
            var v = Vector2.ClampMagnitude(new Vector2(CalDot.x, -CalDot.y), 1.15f) * CalR + CalC;   // stick forward (+y) = up on the picture
            dot.offsetMin = new Vector2(v.x - 11, -v.y - 11); dot.offsetMax = new Vector2(v.x + 11, -v.y + 11);
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
