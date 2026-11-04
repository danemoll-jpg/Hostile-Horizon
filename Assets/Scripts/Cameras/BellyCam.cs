using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace HostileHorizon
{
    // Fix 2.5, item 2: a camera under the ship looking straight down (the top of the picture is the nose), drawn on the cockpit's
    // centre display and as a picture-in-picture on the flat-screen HUD. On by itself in landing mode (hover and landed); the
    // BellyCam button (D-pad down / V / HOTAS 8) turns it on or off any time (until the next change of mode). Landed with a pilot
    // coming, it becomes the airlock camera: under the left wing, looking back at the door (the hull and wing hide it from the cockpit).
    // It sees the world itself (the stop ring, the pad ring, the pilot), so it is the same in VR. It only renders while shown.
    public sealed class BellyCam : MonoBehaviour
    {
        public const int W = 640, H = 380;   // the centre display's shape (420 x 250)
        static readonly Vector3 BellyAt = new Vector3(0, -1.0f, .6f), AirlockAt = new Vector3(-5.0f, -1.0f, 3.9f), DoorGround = new Vector3(-2.45f, -.75f, .8f);   // Round 3: ahead of the left wing, on the figure at the hull   // under the left wing's leading edge, looking back at the door
        public static Vector3 AirlockCamAt => AirlockAt;   // Round 3: figures at the door look up into it
        public ShipController Ship;
        public Camera Cam { get; private set; }
        public RenderTexture Feed { get; private set; }
        public bool On { get; private set; }
        public bool AirlockView;               // set by the Campaign: landed with a pilot running / at the door
        public bool Allowed = true;            // off during the automatic parts and menus
        bool? manual; FlightMode lastMode; bool lastLanded, shadowsOn; UniversalAdditionalCameraData camData;

        public static BellyCam Create(ShipController ship)
        {
            var go = new GameObject("Belly camera"); go.transform.SetParent(ship.transform, false);
            var b = go.AddComponent<BellyCam>(); b.Ship = ship;
            b.Feed = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32) { name = "Belly camera feed", antiAliasing = 1 }; b.Feed.Create();
            b.Cam = go.AddComponent<Camera>(); b.Cam.targetTexture = b.Feed; b.Cam.nearClipPlane = .2f; b.Cam.farClipPlane = 1500f; b.Cam.enabled = false;
            var d = b.Cam.GetUniversalAdditionalCameraData(); d.renderShadows = false; d.renderPostProcessing = false; b.camData = d;
            return b;
        }

        // In landing mode by itself; a press of the button flips it (kept until the mode changes).
        public bool Auto => Ship.Mode == FlightMode.Hover && !Ship.Kinematic && !Ship.Crashed;

        void LateUpdate()
        {
            if (!Ship) return;
            if (Ship.Mode != lastMode || Ship.Landed != lastLanded) { manual = null; lastMode = Ship.Mode; lastLanded = Ship.Landed; }
            bool can = Allowed && Ship.gameObject.activeInHierarchy && !Ship.Kinematic && !Ship.Crashed;
            if (can && Ship.InputEnabled && Time.timeScale > 0 && Controls.BellyCamPressed) manual = !(manual ?? Auto);
            On = can && (manual ?? Auto);
            if (!On) return;
            if (AirlockView && Ship.Landed)
            {
                transform.localPosition = AirlockAt;
                transform.localRotation = Quaternion.LookRotation(DoorGround - AirlockAt, Vector3.up);
                Cam.fieldOfView = 42f;   // Round 3: the figure fills the frame (tells 1, 2, 4 must be judged here)
                shadowsOn = true;
            }
            else
            {
                transform.localPosition = BellyAt;
                transform.localRotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);   // straight down, the nose at the top
                Cam.fieldOfView = 78f;
                shadowsOn = false;
            }
            if (camData.renderShadows != shadowsOn) camData.renderShadows = shadowsOn;   // Round 3: shadows in the airlock view (the Mimic's tell)
            Cam.Render();   // drawn here, by hand (an enabled camera into a texture isn't drawn in a batch-mode check; this works in both)
        }

        public string Title => AirlockView && Ship.Landed ? "AIRLOCK CAM" : "BELLY CAM";
    }
}
