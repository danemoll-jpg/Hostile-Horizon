using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HostileHorizon
{
    // Round 6 (B2): a bracket in the world round every enemy the ship can see within bracketRange, with its kind and distance; the
    // nearest (or the one locked on) brighter and thicker. World objects (a small world-space canvas each, kept a fixed distance in
    // front of the eye in the enemy's true direction and sized to a fixed angle), so the cockpit view, the flat-screen HUD and later
    // VR all show them the same way (VR rule 4).
    public sealed class TargetBrackets : MonoBehaviour
    {
        sealed class Mark { public Canvas canvas; public Image[] bars; public Text label; }
        readonly List<Mark> marks = new List<Mark>();
        readonly Dictionary<Enemy, (float at, bool sees)> sight = new Dictionary<Enemy, (float, bool)>();
        public int Shown { get; private set; }            // checks
        public string ShownList { get; private set; } = "";
        const float Place = 70f;                          // m in front of the eye the brackets are drawn

        public static TargetBrackets Create() => new GameObject("Target brackets").AddComponent<TargetBrackets>();

        Mark NewMark()
        {
            var m = new Mark { canvas = UiKit.WorldCanvas("Bracket", transform, new Vector2(100, 100), .01f) };
            m.canvas.sortingOrder = 10;
            var t = m.canvas.transform; m.bars = new Image[8];
            // four corners, two bars each (canvas units: the box is 100 x 100)
            Vector2[] at = { new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 0), new Vector2(1, 0) };
            for (int i = 0; i < 4; i++)
            {
                var a = at[i]; float sx = a.x == 0 ? 1 : -1, sy = a.y == 0 ? 1 : -1;
                m.bars[i * 2] = UiKit.Panel(t, "h", Color.red, a, a, Vector2.zero, Vector2.zero);
                m.bars[i * 2 + 1] = UiKit.Panel(t, "v", Color.red, a, a, Vector2.zero, Vector2.zero);
                Size(m.bars[i * 2].rectTransform, sx, sy, 28, 4); Size(m.bars[i * 2 + 1].rectTransform, sx, sy, 4, 28);
            }
            m.label = UiKit.Label(t, "Label", 15, TextAnchor.UpperCenter, Color.red, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(-150, -40), new Vector2(150, -4));
            m.label.horizontalOverflow = HorizontalWrapMode.Overflow;
            marks.Add(m); return m;
        }

        static void Size(RectTransform r, float sx, float sy, float w, float h)
        {
            r.pivot = new Vector2(sx > 0 ? 0 : 1, sy > 0 ? 0 : 1); r.sizeDelta = new Vector2(w, h); r.anchoredPosition = Vector2.zero;
        }

        // every frame from GameShell: on = flying in the Campaign; eye = the head camera
        public void Tick(bool on, Enemies foes, Transform eye, Vector3 shipPos, float range)
        {
            int n = 0; Shown = 0; var list = new System.Text.StringBuilder();
            if (on && foes && eye)
            {
                // the nearest seen (or locked) gets the highlight
                Enemy best = null; float bd = float.MaxValue;
                var seen = new List<(Enemy e, float d)>();
                foreach (var e in foes.All)
                {
                    if (e.Dead) continue;
                    float d = Vector3.Distance(e.Centre, shipPos); if (d > range) continue;
                    if (!sight.TryGetValue(e, out var s) || Time.time > s.at) { s = (Time.time + .25f + (e.Index % 5) * .02f, foes.Sees(eye.position, e.Centre, 16f)); sight[e] = s; }
                    if (!s.sees) continue;
                    seen.Add((e, d));
                    float score = e.Locked ? -1 : d; if (score < bd) { bd = score; best = e; }
                }
                foreach (var (e, d) in seen)
                {
                    var to = e.Centre - eye.position; float dist = to.magnitude; if (dist < 1) continue;
                    var dir = to / dist; if (Vector3.Dot(dir, eye.forward) < .1f) continue;   // behind you: the radar and the threat arrows show it
                    var m = n < marks.Count ? marks[n] : NewMark(); n++;
                    bool hi = e == best;
                    // a fixed angle: at least 3.2 degrees, or the enemy's own size and a margin when it is close
                    float ang = Mathf.Max(hi ? 3.6f : 3.0f, Mathf.Atan2(e.Radius * 2.6f, dist) * Mathf.Rad2Deg);
                    float size = 2f * Place * Mathf.Tan(ang * .5f * Mathf.Deg2Rad);
                    m.canvas.gameObject.SetActive(true);
                    m.canvas.transform.SetPositionAndRotation(eye.position + dir * Place, Quaternion.LookRotation(dir, eye.up));
                    m.canvas.transform.localScale = Vector3.one * (size / 100f);
                    var col = hi ? new Color(1f, .3f, .22f, 1f) : new Color(1f, .35f, .3f, .6f);
                    float thick = hi ? 6 : 3.5f;
                    for (int i = 0; i < 8; i++) { m.bars[i].color = col; var r = m.bars[i].rectTransform; r.sizeDelta = i % 2 == 0 ? new Vector2(28, thick) : new Vector2(thick, 28); }
                    m.label.color = col; m.label.fontSize = hi ? 17 : 14;
                    // the label kept at a readable size on screen: canvas units scale with the box, so fixed-size text
                    m.label.transform.localScale = Vector3.one * (3.2f / ang);
                    string what = e.Kind == EnemyKind.Turret ? "TURRET" : "SAUCER";
                    m.label.text = (hi && e.Locked ? "<b>LOCKED ON</b> · " : "") + $"{what} {RadarScope.Distance(d)}";
                    Shown++; list.Append($"{what} {e.Index + 1} {d:0} m{(hi ? " (highlighted)" : "")}; ");
                }
            }
            for (int i = n; i < marks.Count; i++) marks[i].canvas.gameObject.SetActive(false);
            ShownList = list.ToString();
        }
    }
}
