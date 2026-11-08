using UnityEngine;
using UnityEngine.UI;

namespace HostileHorizon
{
    // Round 8 (C5): the comfort options. Dan's rule (VR rule 5): realism first, so every one is OFF unless the player turns it on
    // (Settings > Display). Each is drawn in the world just in front of the eyes (a board on the head), so it works the same on the
    // flat screen and in both eyes in VR:
    //  - Vignette: the edges of the view darken while the ship turns hard (more than 60 degrees a second);
    //  - Fade for the jump: the view fades to black and back instead of watching the mothership jump;
    //  - Horizon line: a faint level line across the view at the true horizon, whatever the ship's roll.
    public sealed class Comfort : MonoBehaviour
    {
        Transform head, ship; Image vignette, fade; RectTransform horizon; Quaternion lastShip; float fadeUntil, fadeLen, vig;
        public static Comfort I { get; private set; }
        public float VignetteNow => vignette ? vignette.color.a : 0;   // checks
        public float FadeNow => fade ? fade.color.a : 0;

        public static Comfort Create(Camera headCam, Transform shipT)
        {
            var c = new GameObject("Comfort").AddComponent<Comfort>(); I = c; c.head = headCam.transform; c.ship = shipT;
            var board = UiKit.WorldCanvas("Comfort board", headCam.transform, new Vector2(2000, 2000), .0004f);   // 0.8 m square, 0.25 m ahead
            board.transform.localPosition = new Vector3(0, 0, .25f); board.sortingOrder = 50;
            c.vignette = Ring(board.transform); c.fade = UiKit.Panel(board.transform, "Fade", new Color(0, 0, 0, 0), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var hz = UiKit.WorldCanvas("Horizon line", c.transform, new Vector2(8000, 6), .01f);   // 80 m long, 6 cm thick, 40 m ahead
            c.horizon = (RectTransform)hz.transform; UiKit.Panel(hz.transform, "Line", new Color(.85f, .9f, 1f, .35f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return c;
        }

        static Image Ring(Transform parent)
        {
            const int N = 128; var t = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
            {
                float r = new Vector2(x - N / 2f + .5f, y - N / 2f + .5f).magnitude / (N / 2f);
                t.SetPixel(x, y, new Color(0, 0, 0, Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.35f, .85f, r))));
            }
            t.Apply();
            var img = UiKit.Rect(parent, "Vignette", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject.AddComponent<Image>();
            img.sprite = Sprite.Create(t, new Rect(0, 0, N, N), new Vector2(.5f, .5f)); img.color = new Color(1, 1, 1, 0); img.raycastTarget = false;
            return img;
        }

        public void FadeFor(float seconds) { if (!GameSettings.S.comfortFade) return; fadeLen = seconds; fadeUntil = Time.unscaledTime + seconds; }

        void LateUpdate()
        {
            var s = GameSettings.S; float dt = Time.unscaledDeltaTime;
            // vignette: from the ship's turn rate
            float rate = ship && dt > 0 && Time.timeScale > 0 ? Quaternion.Angle(lastShip, ship.rotation) / Time.deltaTime : 0; if (ship) lastShip = ship.rotation;
            float want = s.comfortVignette && ship && ship.gameObject.activeInHierarchy ? Mathf.Clamp01((rate - 60f) / 90f) * .85f : 0;
            vig = Mathf.MoveTowards(vig, want, dt * (want > vig ? 3f : 1.5f));
            vignette.color = new Color(1, 1, 1, vig);
            // fade: in for the first quarter, black, out for the last quarter
            float left = fadeUntil - Time.unscaledTime, a = 0;
            if (left > 0 && fadeLen > 0) { float k = 1 - left / fadeLen; a = k < .25f ? k / .25f : k > .75f ? (1 - k) / .25f : 1; }
            fade.color = new Color(0, 0, 0, a);
            // horizon: level, 40 m ahead of the head along its heading, at eye height
            bool hzOn = s.comfortHorizon && ship && ship.gameObject.activeInHierarchy;
            horizon.gameObject.SetActive(hzOn);
            if (hzOn)
            {
                var f = Vector3.ProjectOnPlane(head.forward, Vector3.up); if (f.sqrMagnitude < .01f) f = Vector3.forward; f.Normalize();
                horizon.SetPositionAndRotation(head.position + f * 40f, Quaternion.LookRotation(f, Vector3.up));
            }
        }
    }
}
