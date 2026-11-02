using UnityEngine;

namespace HostileHorizon
{
    // Round 1, item 4, on the VR camera-rig rule (CLAUDE.md): this object is the tracking-space root; the Head camera under it keeps
    // the local pose it is given (identity on a flat screen; VR will drive it). Flat-screen code moves only this root.
    // Views: Cockpit (default; the only one in VR), Chase, Close chase, Canyon (low behind and below the ship, near the ground).
    // One button cycles them. Look (HOTAS hat, right stick, arrow keys, mouse with the right button) turns the view; it springs
    // back when let go. No view ever moves by itself in a way the player can't stop (comfort rule): the chase views follow the ship
    // with a short lag and never roll.
    public sealed class CameraRig : MonoBehaviour
    {
        public enum Mode { Cockpit, Chase, CloseChase, Canyon }
        public static readonly string[] ModeNames = { "Cockpit", "Chase", "Close chase", "Canyon" };

        public Camera Head { get; private set; }
        public Mode Current = Mode.Cockpit;
        public ShipController Ship;
        public CanyonField Field;
        public bool FollowShip;
        Vector2 look;                 // degrees: yaw, pitch
        Vector3 smoothPos; bool snapped;

        public static CameraRig Create()
        {
            var root = new GameObject("CameraRig");
            var rig = root.AddComponent<CameraRig>();
            var head = new GameObject("Head");
            head.transform.SetParent(root.transform, false);
            rig.Head = head.AddComponent<Camera>();
            rig.Head.nearClipPlane = .05f;
            head.AddComponent<AudioListener>();
            return rig;
        }

        public void Cycle() { Current = (Mode)(((int)Current + 1) % 4); snapped = false; }
        public void Snap() => snapped = false;

        void LateUpdate()
        {
            if (!FollowShip || !Ship) return;
            float dt = Time.unscaledDeltaTime;
            if (Time.timeScale > 0)
            {
                var input = Controls.Look * 110f * dt;
                var m = Controls.MouseLook * .12f;
                Vector2 held = new Vector2(Controls.Look.x, Controls.Look.y);
                if (held.sqrMagnitude > .01f || m.sqrMagnitude > 0) look += new Vector2(input.x + m.x, input.y + m.y);
                else look = Vector2.MoveTowards(look, Vector2.zero, 160f * dt);
                look.x = Mathf.Clamp(look.x, -150f, 150f); look.y = Mathf.Clamp(look.y, -60f, 70f);
            }
            var lookRot = Quaternion.Euler(-look.y, look.x, 0);
            var ship = Ship.transform;
            Head.nearClipPlane = Current == Mode.Cockpit ? .05f : .3f;
            if (Current == Mode.Cockpit)
            {
                transform.SetPositionAndRotation(ship.TransformPoint(ShipLayout.Eye), ship.rotation * lookRot);
                return;
            }
            Vector3 fwd = ship.forward, flat = Vector3.ProjectOnPlane(fwd, Vector3.up);
            if (flat.sqrMagnitude < .01f) flat = ship.up * -Mathf.Sign(fwd.y); flat.Normalize();
            float dist = Current == Mode.Chase ? 30f : Current == Mode.CloseChase ? 15f : 24f;
            float up = Current == Mode.Chase ? 8f : Current == Mode.CloseChase ? 3.5f : -7f;
            Vector3 offset = Quaternion.AngleAxis(look.x, Vector3.up) * (-flat * dist) + Vector3.up * (up + look.y * .2f);
            Vector3 target = ship.position + offset;
            if (Field != null)
            {
                float g = Field.SurfaceHeight(target.x, target.z);
                float floor = Current == Mode.Canyon ? 4f : 2.5f;
                if (target.y < g + floor) target.y = g + floor;
            }
            smoothPos = snapped ? Vector3.Lerp(smoothPos, target, 1 - Mathf.Exp(-9f * dt)) : target; snapped = true;
            var aim = ship.position + fwd * (Current == Mode.Canyon ? 30f : 18f) - smoothPos;
            transform.SetPositionAndRotation(smoothPos, Quaternion.LookRotation(aim, Vector3.up));
        }
    }
}
