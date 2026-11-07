using UnityEngine;

namespace HostileHorizon
{
    // Round 6 (D1, E1): a small 3D stage far below the world (nothing of the planet is ever near it) with its own camera drawing into
    // a picture the menu shows (MenuPanel.Page.View): the star map and the Field Journal's creature viewer. A world-space menu (VR
    // rule 3) shows the same picture. Its lights are spot lights of its own; the camera only sees the stage's layer.
    public sealed class ViewStage : MonoBehaviour
    {
        public const int Layer = 30;
        public static readonly Vector3 Origin = new Vector3(0, -30000, 0);
        public Camera Cam { get; private set; }
        public RenderTexture Tex { get; private set; }
        public Transform Root { get; private set; }
        public Light Key, Fill, Back;

        public static ViewStage Create()
        {
            var s = new GameObject("View stage").AddComponent<ViewStage>(); s.transform.position = Origin;
            s.Root = new GameObject("Content").transform; s.Root.SetParent(s.transform, false);
            s.Tex = new RenderTexture(1280, 582, 24, RenderTextureFormat.ARGB32) { name = "View stage", antiAliasing = 4 }; s.Tex.Create();
            var cg = new GameObject("Stage camera"); cg.transform.SetParent(s.transform, false);
            s.Cam = cg.AddComponent<Camera>(); s.Cam.targetTexture = s.Tex; s.Cam.clearFlags = CameraClearFlags.SolidColor; s.Cam.backgroundColor = new Color(.015f, .02f, .03f, 1);
            s.Cam.cullingMask = 1 << Layer; s.Cam.nearClipPlane = .05f; s.Cam.farClipPlane = 200f; s.Cam.fieldOfView = 32f; s.Cam.enabled = false;
            var cd = UnityEngine.Rendering.Universal.CameraExtensions.GetUniversalAdditionalCameraData(s.Cam); cd.renderShadows = false; cd.renderPostProcessing = false;
            Light L(string n, Vector3 at, Color c, float intensity, float range)
            {
                var l = new GameObject(n).AddComponent<Light>(); l.transform.SetParent(s.transform, false); l.transform.localPosition = at; l.transform.LookAt(s.transform.position + Vector3.up * .9f);
                l.type = LightType.Spot; l.spotAngle = 70; l.range = range; l.intensity = intensity; l.color = c; l.shadows = LightShadows.None; l.cullingMask = 1 << Layer; return l;
            }
            s.Key = L("Key light", new Vector3(2.2f, 3.2f, -3.2f), new Color(1f, .95f, .88f), 9f, 14f);
            s.Fill = L("Fill light", new Vector3(-3.2f, 1.4f, -2.2f), new Color(.55f, .7f, 1f), 3.5f, 12f);
            s.Back = L("Back light", new Vector3(0, 2.8f, 3.4f), new Color(1f, .75f, .55f), 7f, 12f);
            return s;
        }

        public void Clear() { foreach (Transform c in Root) Destroy(c.gameObject); }
        public void Show(bool on) { Cam.enabled = on; }
        public void Lights(bool on) { Key.enabled = Fill.enabled = Back.enabled = on; }

        public static void SetLayer(GameObject g) { g.layer = Layer; foreach (Transform c in g.transform) SetLayer(c.gameObject); }
    }
}
