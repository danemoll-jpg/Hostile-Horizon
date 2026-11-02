using UnityEngine;
using UnityEngine.UI;

namespace HostileHorizon
{
    // Small uGUI helpers. Every canvas is made here so it can be screen-space now and a world-space panel later (VR rule 3).
    public static class UiKit
    {
        static Font font;
        public static Font Font => font ? font : font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        public static readonly Color Accent = new Color(1f, .62f, .32f), Dim = new Color(.75f, .78f, .8f), Ink = new Color(.93f, .95f, .96f);

        public static Canvas ScreenCanvas(string name, int order)
        {
            var go = new GameObject(name);
            var c = go.AddComponent<Canvas>(); c.renderMode = RenderMode.ScreenSpaceOverlay; c.sortingOrder = order;
            var s = go.AddComponent<CanvasScaler>(); s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; s.referenceResolution = new Vector2(1920, 1080); s.matchWidthOrHeight = 1;
            return c;
        }

        // A canvas in the world: size in canvas units, metres per unit.
        public static Canvas WorldCanvas(string name, Transform parent, Vector2 size, float scale)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var c = go.AddComponent<Canvas>(); c.renderMode = RenderMode.WorldSpace;
            var rt = (RectTransform)go.transform; rt.sizeDelta = size; rt.localScale = Vector3.one * scale;
            return c;
        }

        public static RectTransform Rect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offMin, Vector2 offMax)
        {
            var go = new GameObject(name, typeof(RectTransform)); var rt = (RectTransform)go.transform; rt.SetParent(parent, false);
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax; rt.offsetMin = offMin; rt.offsetMax = offMax; return rt;
        }

        public static Image Panel(Transform parent, string name, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 offMin, Vector2 offMax)
        {
            var img = Rect(parent, name, anchorMin, anchorMax, offMin, offMax).gameObject.AddComponent<Image>(); img.color = color; img.raycastTarget = false; return img;
        }

        public static Text Label(Transform parent, string name, int size, TextAnchor align, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 offMin, Vector2 offMax)
        {
            var t = Rect(parent, name, anchorMin, anchorMax, offMin, offMax).gameObject.AddComponent<Text>();
            t.font = Font; t.fontSize = size; t.alignment = align; t.color = color; t.supportRichText = true; t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
            var sh = t.gameObject.AddComponent<Shadow>(); sh.effectColor = new Color(0, 0, 0, .7f); sh.effectDistance = new Vector2(2, -2);
            return t;
        }
    }
}
