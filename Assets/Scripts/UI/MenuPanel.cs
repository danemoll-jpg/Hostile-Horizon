using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HostileHorizon
{
    // Round 1, item 7: one menu panel for every page (title, Free Flight, Settings, pause, crash). Rows are chosen with up / down
    // (D-pad, left stick, HOTAS hat or stick, arrows / WS), changed with left / right, confirmed with Submit (A, Enter / Space, HOTAS
    // trigger), left with Cancel (B, Esc / Backspace, HOTAS button 2). Only the UI actions of Controls are read.
    // Built to the world-space panel rule: ShowInWorld puts the same panel in front of a camera (for VR later).
    public sealed class MenuPanel : MonoBehaviour
    {
        public sealed class Row
        {
            public Func<string> Label, Value;      // Value null: an action row
            public Action Submit, Left, Right;
            public Func<bool> Visible;
            public Func<bool> Disabled;
        }

        public sealed class Page
        {
            public string Title, Subtitle;
            public Func<string> Body;              // text under the rows (axis display, credits, hints)
            public Func<List<(Texture pic, string label)>> Pictures;   // Fix 3.1: a row of pictures under the rows (the Field Journal)
            public List<Row> Rows = new List<Row>();
            public Action Back;
            public Func<string> Hint;
            public Func<bool> CaptureInput;        // true while the page edits something itself (the seed digits)
            public Func<Texture> View;             // Round 6 (D1, E1): a large picture over the body (the star map, the creature viewer)
            public int Selected;
            public Page Add(Row r) { Rows.Add(r); return this; }
        }

        public Page Current { get; private set; }
        public bool Visible => canvas && canvas.gameObject.activeSelf;
        public Canvas Canvas => canvas;
        Canvas canvas; Text title, subtitle, body, hint; readonly List<Text> rows = new List<Text>(); Image cursor, backdrop;
        readonly List<RawImage> pics = new List<RawImage>(); readonly List<Text> picLabels = new List<Text>(); RawImage view; Image viewFrame;
        const int MaxPictures = 5;
        float navTimer; int navDir, shownFrame;
        const int MaxRows = 12;

        public static MenuPanel Create()
        {
            var c = UiKit.ScreenCanvas("Menu", 10);
            var m = c.gameObject.AddComponent<MenuPanel>(); m.canvas = c;
            var t = c.transform;
            m.backdrop = UiKit.Panel(t, "Backdrop", new Color(.03f, .04f, .05f, .55f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            UiKit.Panel(t, "Column", new Color(.02f, .03f, .04f, .72f), new Vector2(0, 0), new Vector2(0, 1), new Vector2(90, 0), new Vector2(900, 0));
            m.title = UiKit.Label(t, "Title", 64, TextAnchor.UpperLeft, UiKit.Ink, new Vector2(0, 1), new Vector2(0, 1), new Vector2(140, -210), new Vector2(1500, -90));
            m.subtitle = UiKit.Label(t, "Subtitle", 26, TextAnchor.UpperLeft, UiKit.Accent, new Vector2(0, 1), new Vector2(0, 1), new Vector2(144, -260), new Vector2(880, -212));
            m.cursor = UiKit.Panel(t, "Cursor", new Color(1f, .62f, .32f, .22f), new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, Vector2.zero);
            for (int i = 0; i < MaxRows; i++)
                m.rows.Add(UiKit.Label(t, "Row" + i, 34, TextAnchor.MiddleLeft, UiKit.Ink, new Vector2(0, 1), new Vector2(0, 1), new Vector2(150, -330 - i * 58), new Vector2(880, -280 - i * 58)));
            m.body = UiKit.Label(t, "Body", 24, TextAnchor.UpperLeft, UiKit.Dim, new Vector2(0, 1), new Vector2(1, 1), new Vector2(960, -1000), new Vector2(-80, -300));
            for (int i = 0; i < MaxPictures; i++)
            {
                var g = new GameObject("Picture" + i, typeof(RectTransform)); g.transform.SetParent(t, false);
                var ri = g.AddComponent<RawImage>(); var rt = ri.rectTransform; rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
                rt.offsetMin = new Vector2(150 + i * 145, -580); rt.offsetMax = new Vector2(150 + i * 145 + 126, -412); ri.enabled = false; m.pics.Add(ri);
                var l = UiKit.Label(t, "PictureLabel" + i, 20, TextAnchor.UpperCenter, UiKit.Accent, new Vector2(0, 1), new Vector2(0, 1), new Vector2(150 + i * 145 - 6, -612), new Vector2(150 + i * 145 + 132, -584)); m.picLabels.Add(l);
            }
            m.viewFrame = UiKit.Panel(t, "View frame", new Color(.4f, 1f, .8f, .25f), new Vector2(0, 1), new Vector2(1, 1), new Vector2(956, -694), new Vector2(-76, -286));
            var vg = new GameObject("View", typeof(RectTransform)); vg.transform.SetParent(t, false);
            m.view = vg.AddComponent<RawImage>(); var vr = m.view.rectTransform; vr.anchorMin = new Vector2(0, 1); vr.anchorMax = new Vector2(1, 1); vr.offsetMin = new Vector2(960, -690); vr.offsetMax = new Vector2(-80, -290);
            m.view.enabled = m.viewFrame.enabled = false; m.view.raycastTarget = false;
            m.hint = UiKit.Label(t, "Hint", 22, TextAnchor.LowerLeft, UiKit.Dim, new Vector2(0, 0), new Vector2(0, 0), new Vector2(150, 40), new Vector2(1800, 120));
            return m;
        }

        public void Show(Page p, bool keepSelection = false)
        {
            if (!keepSelection) p.Selected = FirstVisible(p, 0, 1);
            Current = p; canvas.gameObject.SetActive(true); navTimer = .25f; shownFrame = Time.frameCount; Refresh();
        }

        public void Hide() { canvas.gameObject.SetActive(false); Current = null; }
        public void SetBackdrop(float alpha) => backdrop.color = new Color(.03f, .04f, .05f, alpha);

        // VR rule 3: the same panel as a world-space board 2 m in front of the camera (used by the check; VR uses it later).
        public void ShowInWorld(Camera cam)
        {
            canvas.renderMode = RenderMode.WorldSpace; canvas.worldCamera = cam;
            var rt = (RectTransform)canvas.transform; rt.sizeDelta = new Vector2(1920, 1080); rt.localScale = Vector3.one * .0011f;
            rt.SetPositionAndRotation(cam.transform.position + cam.transform.forward * 2.2f, Quaternion.LookRotation(cam.transform.forward, Vector3.up));
        }
        public void ShowOnScreen() { canvas.renderMode = RenderMode.ScreenSpaceOverlay; }

        static bool Shown(Row r) => r.Visible == null || r.Visible();
        static int FirstVisible(Page p, int from, int dir)
        {
            for (int k = 0; k < p.Rows.Count; k++) { int i = ((from + k * dir) % p.Rows.Count + p.Rows.Count) % p.Rows.Count; if (Shown(p.Rows[i])) return i; }
            return 0;
        }

        void Update()
        {
            if (Current == null) return;
            if (Time.frameCount == shownFrame) return;   // the press that opened this page does not also act on it
            var p = Current;
            if (p.CaptureInput != null && p.CaptureInput()) { Refresh(); return; }
            Vector2 nav = Controls.Navigate;
            int dir = Mathf.Abs(nav.y) > .55f ? (nav.y > 0 ? 1 : 2) : Mathf.Abs(nav.x) > .55f ? (nav.x > 0 ? 4 : 3) : 0;
            bool fire = false;
            if (dir != navDir) { navDir = dir; fire = dir != 0; navTimer = .38f; }
            else if (dir != 0) { navTimer -= Time.unscaledDeltaTime; if (navTimer <= 0) { fire = true; navTimer = .11f; } }
            if (fire && p.Rows.Count > 0)
            {
                var row = p.Rows[p.Selected];
                if (dir == 1) p.Selected = FirstVisible(p, p.Selected - 1, -1);
                else if (dir == 2) p.Selected = FirstVisible(p, p.Selected + 1, 1);
                else if (dir == 3 && !(row.Disabled?.Invoke() ?? false)) row.Left?.Invoke();
                else if (dir == 4 && !(row.Disabled?.Invoke() ?? false)) row.Right?.Invoke();
            }
            if (Current != p) return;
            if (Controls.SubmitPressed && p.Rows.Count > 0)
            {
                var row = p.Rows[p.Selected];
                if (!(row.Disabled?.Invoke() ?? false)) { if (row.Submit != null) row.Submit(); else row.Right?.Invoke(); }
            }
            else if (Controls.CancelPressed) p.Back?.Invoke();
            if (Current == p) Refresh();
        }

        void Refresh()
        {
            var p = Current; if (p == null) return;
            title.text = p.Title; subtitle.text = p.Subtitle ?? "";
            int line = 0;
            for (int i = 0; i < rows.Count; i++) rows[i].text = "";
            cursor.enabled = false;
            for (int i = 0; i < p.Rows.Count && line < rows.Count; i++)
            {
                var r = p.Rows[i]; if (!Shown(r)) continue;
                bool dis = r.Disabled?.Invoke() ?? false; bool sel = i == p.Selected;
                string label = r.Label(), value = r.Value?.Invoke();
                var t = rows[line];
                t.text = value == null ? label : $"{label}<color=#{ColorUtility.ToHtmlStringRGB(UiKit.Accent)}>   ‹ {value} ›</color>";
                t.color = dis ? new Color(.5f, .52f, .54f) : sel ? Color.white : UiKit.Ink;
                if (sel)
                {
                    cursor.enabled = true; var rt = cursor.rectTransform; var tr = t.rectTransform;
                    rt.offsetMin = tr.offsetMin + new Vector2(-24, -2); rt.offsetMax = new Vector2(880, tr.offsetMax.y + 2);
                }
                line++;
            }
            body.text = p.Body?.Invoke() ?? "";
            var v = p.View?.Invoke(); view.enabled = viewFrame.enabled = v; if (v) view.texture = v;
            body.rectTransform.offsetMax = new Vector2(-80, v ? -706 : -300); body.fontSize = v ? 21 : 24;
            var list = p.Pictures?.Invoke();
            for (int i = 0; i < MaxPictures; i++)
            {
                bool on = list != null && i < list.Count && list[i].pic;
                pics[i].enabled = on; picLabels[i].text = on ? list[i].label : ""; if (on) pics[i].texture = list[i].pic;
            }
            hint.text = p.Hint?.Invoke() ?? "Up / down: choose     Left / right: change     A / Enter / trigger: select" + (p.Back != null ? "     B / Esc: back" : "");
        }
    }
}
