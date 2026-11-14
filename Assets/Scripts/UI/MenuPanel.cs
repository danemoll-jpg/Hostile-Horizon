using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HostileHorizon
{
    // Fix 6.1: menu directions from a stick, hat, D-pad or keys. Only a clear push counts: up / down when the stick is mostly up or
    // down (|y| > .55 and |y| > |x|), left / right only when it is clearly sideways (|x| > .6 and |x| > 2|y|). After an up / down move
    // the stick must come back near the centre (both axes under .25) before left / right can act, and the other way round, so a
    // stick that leans sideways on its way up, down or back to the centre never changes a value. Held, a direction repeats.
    public sealed class NavReader
    {
        int lockAxis, dir; float timer;
        public void Reset() { lockAxis = 0; dir = 0; timer = .25f; }
        // 0 nothing, 1 up, 2 down, 3 left, 4 right: the step to take this frame
        public int Step(Vector2 n, float dt)
        {
            float ax = Mathf.Abs(n.x), ay = Mathf.Abs(n.y);
            if (ax < .25f && ay < .25f) lockAxis = 0;
            if (lockAxis == 0) { if (ay > .55f && ay > ax) lockAxis = 1; else if (ax > .6f && ax > 2 * ay) lockAxis = 2; }
            int d = lockAxis == 1 && ay > .45f ? (n.y > 0 ? 1 : 2) : lockAxis == 2 && ax > .5f ? (n.x > 0 ? 4 : 3) : 0;
            bool fire = false;
            if (d != dir) { dir = d; fire = d != 0; timer = .38f; }
            else if (d != 0) { timer -= dt; if (timer <= 0) { fire = true; timer = .11f; } }
            return fire ? d : 0;
        }
    }

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
            public Func<string> SubtitleFn;        // Round 11 (A1): a live subtitle (HOTAS setup: how the HOTAS arrives)
            public Func<string> Body;              // text under the rows (axis display, credits, hints)
            public Func<List<(Texture pic, string label)>> Pictures;   // Fix 3.1: a row of pictures under the rows (the Field Journal)
            public List<Row> Rows = new List<Row>();
            public Action Back;
            public Func<string> Hint;
            public Func<bool> CaptureInput;        // true while the page edits something itself (the seed digits)
            public Func<Texture> View;             // Round 6 (D1, E1): a large picture over the body (the star map, the creature viewer)
            public GameObject Sheet;               // Fix 6.1: a layout of its own on the menu canvas, shown with the page (Settings > Controls)
            public Action Shown;                   // Fix 6.1: called each time the page is shown
            public int RowsShown;                  // Fix 6.1: rows on screen at once (0 = MaxRows); more scroll
            public Action<int> Side;               // Fix 8.2: left / right on any row of the page (-1 / +1): the Field Journal's previous / next kind
            public int Selected;
            public Page Add(Row r) { Rows.Add(r); return this; }
        }

        public Page Current { get; private set; }
        public bool Visible => canvas && canvas.gameObject.activeSelf;
        public Canvas Canvas => canvas;
        Canvas canvas; Text title, subtitle, body, hint; readonly List<Text> rows = new List<Text>(); Image cursor, backdrop;
        readonly List<RawImage> pics = new List<RawImage>(); readonly List<Text> picLabels = new List<Text>(); RawImage view; Image viewFrame;
        const int MaxPictures = 5;
        readonly NavReader nav = new NavReader(); int shownFrame, scrollTop; GameObject sheetOn;
        public Transform Root => canvas.transform;
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
            if (sheetOn && sheetOn != p.Sheet) sheetOn.SetActive(false);
            sheetOn = p.Sheet; if (sheetOn) sheetOn.SetActive(true);
            Current = p; canvas.gameObject.SetActive(true); nav.Reset(); shownFrame = Time.frameCount; scrollTop = 0;
            if (VrHead && VrMode.Active) PlaceInVr();
            p.Shown?.Invoke(); Refresh();
        }

        public void Hide() { canvas.gameObject.SetActive(false); Current = null; if (sheetOn) sheetOn.SetActive(false); sheetOn = null; }
        public void SetBackdrop(float alpha) { lastAlpha = alpha; backdrop.color = new Color(.03f, .04f, .05f, VrBoard ? Mathf.Max(alpha, .92f) : alpha); }
        float lastAlpha = .55f; bool VrBoard => VrMode.Active && VrHead && CheckVrBoard == 0;   // Fix 8.2: in VR the board is near solid (the cockpit behind it doesn't show through the text)

        // VR rule 3: the same panel as a world-space board 2 m in front of the camera (used by the check; VR uses it later).
        public void ShowInWorld(Camera cam)
        {
            canvas.renderMode = RenderMode.WorldSpace; canvas.worldCamera = cam;
            var rt = (RectTransform)canvas.transform; rt.sizeDelta = new Vector2(1920, 1080); rt.localScale = Vector3.one * .0011f;
            rt.SetPositionAndRotation(cam.transform.position + cam.transform.forward * 2.2f, Quaternion.LookRotation(cam.transform.forward, Vector3.up));
        }
        public void ShowOnScreen() { canvas.renderMode = RenderMode.ScreenSpaceOverlay; }

        // Round 8 (C4): in VR every page is a board 1.4 m in front of where your head points (level, turned to face you), 1.34 m wide,
        // carried with the camera rig (the cockpit, the hangar, the title's slow pan); driven by the HOTAS / gamepad / keyboard as now.
        public static Transform VrRoot; public static Camera VrHead;
        // Fix 8.2 (Dan: the Journal "clipped through the cockpit and I couldn't read it"): cause: 0.8.0 / 0.8.1 put the board 1.4 m out,
        // beyond the dash (0.55 m) and the canopy frame (1.2-1.6 m), depth-tested like any object, so the cockpit cut through it. Now the
        // board is 1.0 m out (0.96 m wide: the same 51 degrees of view), turned to where you look (up to 35 degrees down / 25 up),
        // and drawn over everything (HostileHorizon/UIOnTop: no depth test, last in the frame), so nothing cuts through it.
        public const float VrDistance = 1.0f, VrScale = .0005f;
        public static int CheckVrBoard;   // checks: 0 Fix 8.2's board; 1 0.8.1's (1.4 m, level, depth-tested); 2 0.8.1's place drawn on top (what the cockpit hid)
        static Material onTop;
        void PlaceInVr()
        {
            canvas.renderMode = RenderMode.WorldSpace; canvas.worldCamera = VrHead;
            var rt = (RectTransform)canvas.transform; rt.SetParent(VrRoot, false);
            rt.sizeDelta = new Vector2(1920, 1080);
            var h = VrHead.transform;
            if (!onTop) { var sh = Shader.Find("HostileHorizon/UIOnTop"); if (sh) onTop = new Material(sh) { name = "Menu on top (VR)" }; }
            if (CheckVrBoard > 0)
            {
                rt.localScale = Vector3.one * .0007f;
                var lv = Vector3.ProjectOnPlane(h.localRotation * Vector3.forward, Vector3.up); if (lv.sqrMagnitude < .01f) lv = Vector3.forward; lv.Normalize();
                rt.localPosition = h.localPosition + lv * 1.4f - Vector3.up * .05f; rt.localRotation = Quaternion.LookRotation(lv, Vector3.up);
                foreach (var g in canvas.GetComponentsInChildren<Graphic>(true)) g.material = CheckVrBoard == 2 ? onTop : null;
                return;
            }
            rt.localScale = Vector3.one * VrScale;
            var e = (h.localRotation).eulerAngles; float pitch = Mathf.Clamp(Mathf.DeltaAngle(0, e.x), -25f, 35f);   // + = looking down
            var fwd = Quaternion.Euler(pitch, e.y, 0) * Vector3.forward;
            rt.localPosition = h.localPosition + fwd * VrDistance; rt.localRotation = Quaternion.LookRotation(fwd, Quaternion.Euler(pitch, e.y, 0) * Vector3.up);
            if (onTop) foreach (var g in canvas.GetComponentsInChildren<Graphic>(true)) g.material = onTop;
            canvas.sortingOrder = 1000; SetBackdrop(lastAlpha);
        }
        public static Material OnTopMaterial => onTop;   // checks
        public Image Backdrop => backdrop;                // checks
        public void CheckPlaceAgain() { if (VrHead && VrMode.Active) PlaceInVr(); }   // checks

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
            int dir = nav.Step(Controls.Navigate, Time.unscaledDeltaTime);
            bool fire = dir != 0;
            if (fire && p.Rows.Count > 0)
            {
                var row = p.Rows[p.Selected];
                if (dir == 1) p.Selected = FirstVisible(p, p.Selected - 1, -1);
                else if (dir == 2) p.Selected = FirstVisible(p, p.Selected + 1, 1);
                else if ((dir == 3 || dir == 4) && p.Side != null) p.Side(dir == 3 ? -1 : 1);
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
            title.text = p.Title; subtitle.text = p.SubtitleFn?.Invoke() ?? p.Subtitle ?? "";
            int line = 0;
            for (int i = 0; i < rows.Count; i++) rows[i].text = "";
            cursor.enabled = false;
            // Fix 6.1: a page with more rows than fit scrolls with the selection
            var shown = new List<int>(); for (int i = 0; i < p.Rows.Count; i++) if (Shown(p.Rows[i])) shown.Add(i);
            int selPos = Mathf.Max(0, shown.IndexOf(p.Selected));
            int max = p.RowsShown > 0 ? Mathf.Min(p.RowsShown, MaxRows) : MaxRows;
            if (selPos < scrollTop) scrollTop = selPos; if (selPos >= scrollTop + max) scrollTop = selPos - max + 1;
            scrollTop = Mathf.Clamp(scrollTop, 0, Mathf.Max(0, shown.Count - max));
            for (int k = scrollTop; k < shown.Count && line < max; k++)
            {
                int i = shown[k]; var r = p.Rows[i];
                bool dis = r.Disabled?.Invoke() ?? false; bool sel = i == p.Selected;
                string label = r.Label(), value = r.Value?.Invoke();
                var t = rows[line];
                t.text = value == null ? label : $"{label}<color=#{ColorUtility.ToHtmlStringRGB(UiKit.Accent)}>   ‹ {value} ›</color>";
                if (line == 0 && scrollTop > 0) t.text = "▲  " + t.text;
                if (line == max - 1 && k < shown.Count - 1) t.text = "▼  " + t.text;
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
            hint.text = p.Hint?.Invoke() ?? Controls.MenuHint(p.Back != null);
        }
    }
}
