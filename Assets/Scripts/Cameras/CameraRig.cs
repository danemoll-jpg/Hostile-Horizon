using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;

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
        public enum Mode { Cockpit, Chase, CloseChase, Canyon, Landing }
        public static readonly string[] ModeNames = { "Cockpit", "Chase", "Close chase", "Canyon", "Landing" };

        public Camera Head { get; private set; }
        public Mode Current = Mode.Cockpit;
        public ShipController Ship;
        public CanyonField Field;
        public bool FollowShip;
        public bool Scripted;                       // Round 2: a sequence moves this rig itself (the trip to the regeneration bay)
        public Vector3? Watch;                      // Round 2: after a crash the view pulls out, live, and circles the wreck
        float watchAngle;
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
            head.AddComponent<AudioListener>(); TestRun.Attach(head);   // test runs: muted at the listener
            return rig;
        }

        public void Cycle() { if (Vr) return; Current = (Mode)(((int)Current + 1) % 5); snapped = false; }   // Round 8 (C3): VR has the cockpit only

        // Round 8 (C3): VR. The headset drives the Head's local pose (an Input System TrackedPoseDriver, 1:1, seated); this root stays
        // the tracking space, placed so the recentred head sits on the ship's eye point looking ahead. Recenter takes the head's pose
        // now as "sitting straight in the seat" (yaw and position only). The look control does nothing in VR: your head looks.
        public bool Vr { get; private set; }
        Vector3 recPos; float recYaw, recenterAt = -1;
        public int Recentered { get; private set; }   // checks
        public int AutoRecentered { get; private set; }   // checks (Fix 9.1)
        public string LastRecenterWhy { get; private set; } = "";
        public static bool CheckOldVr;      // checks (Fix 9.1): 0.9.0's recentre
        public static Vector2? CheckLook;   // Fix 8.1 checks: look this way (flat); in VR the tracking space is tilted (the mock headset can't turn)
        public void EnableVr()
        {
            Vr = true; Current = Mode.Cockpit; Head.targetTexture = null; Head.stereoTargetEye = StereoTargetEyeMask.Both;
            var tpd = Head.gameObject.AddComponent<TrackedPoseDriver>();
            tpd.positionInput = new InputActionProperty(new InputAction("Head position", binding: "<XRHMD>/centerEyePosition", expectedControlType: "Vector3"));
            tpd.rotationInput = new InputActionProperty(new InputAction("Head rotation", binding: "<XRHMD>/centerEyeRotation", expectedControlType: "Quaternion"));
            tpd.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition; tpd.updateType = TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;
            recenterAt = Time.unscaledTime + 1.5f;   // once the headset reports a pose
            // Fix 9.1 (A4): when the runtime moves its tracking origin (Quest / Virtual Desktop / SteamVR recentring, the headset taken off
            // and put back on), recentre by itself
            var subs = new System.Collections.Generic.List<UnityEngine.XR.XRInputSubsystem>(); SubsystemManager.GetSubsystems(subs);
            foreach (var sub in subs) sub.trackingOriginUpdated += _ => AutoRecenter("the runtime moved its tracking origin");
            gameObject.AddComponent<VrPlacer>().Rig = this;
        }

        // Fix 9.1 (A4): the direction the head faces NOW becomes straight ahead (yaw only: the horizon stays level), and the head's
        // place now becomes the eye point. It is applied by VrPlacer after everything that places this rig (the cockpit, the title,
        // the hangar, scripted sequences), so it works in flight, landed, docked and in menus. 0.8.x / 0.9.0 applied it only in the
        // cockpit view while it followed the ship: on the title, in the hangar and the regeneration bay (the rig placed by other code)
        // a recentre changed nothing.
        public void Recenter()
        {
            if (!Vr) return;
            var h = Head.transform; recPos = h.localPosition; recYaw = YawOf(h.localRotation); Recentered++;
        }
        static float YawOf(Quaternion q)
        {
            var f = q * Vector3.forward; if (new Vector2(f.x, f.z).sqrMagnitude < .02f) f = q * (f.y < 0 ? Vector3.up : Vector3.down);   // looking straight up / down: the top of the head
            return Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg;
        }
        public void AutoRecenter(string why) { if (!Vr) return; recenterAt = Time.unscaledTime + .4f; LastRecenterWhy = why; AutoRecentered++; }

        // Tracking lost and found again (the headset off and on), or the head pose jumping (an origin reset the runtime did not report):
        // recentre by itself
        public string TrackingNow => $"HMD {(InputSystem.GetDevice<XRHMD>() is XRHMD d ? (d.isTracked.isPressed ? "tracked" : "not tracked") : "none")}, lost for {lostFor:0.00} s, last yaw {YawOf(lastHead):0}";   // checks
        bool wasTracked = true; float lostFor; Quaternion lastHead = Quaternion.identity; bool haveHead;
        void WatchTracking()
        {
            var hmd = InputSystem.GetDevice<XRHMD>();
            bool tracked = hmd == null || hmd.isTracked.isPressed;
            if (!tracked) lostFor += Time.unscaledDeltaTime;
            else if (!wasTracked && lostFor > .25f) { AutoRecenter($"tracking back after {lostFor:0.0} s"); lostFor = 0; }
            else if (tracked) lostFor = 0;
            wasTracked = tracked;
            var q = Head.transform.localRotation;
            float jump = Mathf.Abs(Mathf.DeltaAngle(YawOf(lastHead), YawOf(q)));
            if (haveHead && tracked && jump > 45f && jump / Mathf.Max(Time.unscaledDeltaTime, 1e-3f) > 1500f) AutoRecenter("the head pose jumped");   // no head turns 45 degrees at 1500 degrees a second
            lastHead = q; haveHead = tracked;
        }

        // The eye pose that the code placing this rig asked for (VR); VrPlacer turns it into the tracking space's pose.
        Vector3 eyePos, setPos; Quaternion eyeRot = Quaternion.identity, setRot; bool haveEye;
        public void PlaceVr()
        {
            if (!Vr) return;
            if (recenterAt > 0 && Time.unscaledTime > recenterAt) { recenterAt = -1; Recenter(); }
            if (CheckOldVr) return;   // checks: 0.9.0 recentred only in the cockpit view (Place) and never by itself
            WatchTracking();
            var t = transform;
            if (!haveEye || (t.position - setPos).sqrMagnitude > 1e-8f || Quaternion.Angle(t.rotation, setRot) > .001f) { eyePos = t.position; eyeRot = t.rotation; haveEye = true; }
            var rot = eyeRot * Quaternion.Euler(0, -recYaw, 0);
            t.SetPositionAndRotation(eyePos - rot * recPos, rot); setPos = t.position; setRot = t.rotation;
        }
        // checks: where the head looks now, relative to the eye pose asked for (yaw, pitch in degrees; metres from the eye point)
        public Quaternion EyeRot => eyeRot;   // checks
        public (float yaw, float pitch, float off) HeadVsEye()
        {
            var h = Head.transform; var local = Quaternion.Inverse(eyeRot) * h.rotation; var f = local * Vector3.forward;
            return (Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg, Mathf.Asin(Mathf.Clamp(f.y, -1, 1)) * Mathf.Rad2Deg, Vector3.Distance(h.position, eyePos));
        }
        public void Snap() => snapped = false;
        public void WatchWreck(Vector3 at) { Watch = at; watchAngle = (Ship ? Ship.Heading : 0) + 160f; }

        // Round 5 (B5): an enemy hit shakes the view a little (it dies away in about half a second)
        float shake;
        public void Shake(float amount) => shake = Mathf.Max(shake, amount);
        public float ShakeNow => shake;

        void LateUpdate()
        {
            Place();
            if (shake > 0 && !Scripted && FollowShip && Ship && Time.timeScale > 0)
            {
                float k = shake * shake;
                transform.position += new Vector3(Mathf.PerlinNoise(Time.time * 31f, 1) - .5f, Mathf.PerlinNoise(Time.time * 29f, 7) - .5f, Mathf.PerlinNoise(Time.time * 37f, 13) - .5f) * .18f * k;
                transform.rotation *= Quaternion.Euler((Mathf.PerlinNoise(Time.time * 23f, 3) - .5f) * 2.4f * k, 0, (Mathf.PerlinNoise(Time.time * 19f, 5) - .5f) * 3f * k);
                shake = Mathf.Max(0, shake - Time.deltaTime * 2.2f);
            }
        }

        void Place()
        {
            if (Scripted || !FollowShip || !Ship) return;
            float dt = Time.unscaledDeltaTime;
            if (Time.timeScale > 0)
            {
                var input = Vr ? Vector2.zero : Controls.Look * 110f * dt;
                var m = Vr ? Vector2.zero : Controls.MouseLook * .12f;
                Vector2 held = new Vector2(Controls.Look.x, Controls.Look.y);
                if (held.sqrMagnitude > .01f || m.sqrMagnitude > 0) look += new Vector2(input.x + m.x, input.y + m.y);
                else look = Vector2.MoveTowards(look, Vector2.zero, 160f * dt);
                look.x = Mathf.Clamp(look.x, -150f, 150f); look.y = Mathf.Clamp(look.y, -60f, 70f);
            }
            if (CheckLook.HasValue) look = CheckLook.Value;   // Fix 8.1 checks: a fixed look (yaw, pitch)
            var lookRot = Quaternion.Euler(-look.y, look.x, 0);
            var ship = Ship.transform;
            Head.nearClipPlane = Current == Mode.Cockpit ? .05f : .3f;
            if (Watch.HasValue)
            {
                // from wherever the view is now, ease out to a slow circle round the wreck (no cut, no fade: VR rule 5)
                var c = Watch.Value; watchAngle += 5f * dt;
                var to = Quaternion.Euler(0, watchAngle + look.x, 0) * new Vector3(0, 0, -34f) + Vector3.up * (13f + look.y * .2f);
                var want = c + to;
                if (Field != null) want.y = Mathf.Max(want.y, Field.SurfaceHeight(want.x, want.z) + 3f);
                var p = Vector3.Lerp(transform.position, want, 1 - Mathf.Exp(-1.6f * dt));
                transform.SetPositionAndRotation(p, Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(c + Vector3.up * 2f - p, Vector3.up), 1 - Mathf.Exp(-3f * dt)));
                return;
            }
            if (Current == Mode.Cockpit)
            {
                if (Vr && CheckOldVr) { var vrot = ship.rotation * Quaternion.Euler(0, -recYaw, 0) * (CheckLook.HasValue ? lookRot : Quaternion.identity); transform.SetPositionAndRotation(ship.TransformPoint(ShipLayout.Eye) - vrot * recPos, vrot); return; }   // checks: 0.9.0's
                if (Vr) { transform.SetPositionAndRotation(ship.TransformPoint(ShipLayout.Eye), ship.rotation * (CheckLook.HasValue ? lookRot : Quaternion.identity)); return; }   // the eye pose: VrPlacer recentres it
                transform.SetPositionAndRotation(ship.TransformPoint(ShipLayout.Eye), ship.rotation * lookRot);
                return;
            }
            Vector3 fwd = ship.forward, flat = Vector3.ProjectOnPlane(fwd, Vector3.up);
            if (flat.sqrMagnitude < .01f) flat = ship.up * -Mathf.Sign(fwd.y); flat.Normalize();
            if (Current == Mode.Landing)
            {
                // Fix 2.5: high over the ship looking down at it (a little behind, so the nose shows which way it faces); never rolls
                var over = ship.position + Quaternion.AngleAxis(look.x, Vector3.up) * (-flat * 12f) + Vector3.up * (42f + look.y * .3f);
                smoothPos = snapped ? Vector3.Lerp(smoothPos, over, 1 - Mathf.Exp(-6f * dt)) : over; snapped = true;
                transform.SetPositionAndRotation(smoothPos, Quaternion.LookRotation(ship.position + flat * 4f - smoothPos, flat));
                return;
            }
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

    // Fix 9.1 (A4): runs after everything that places the camera rig (late in the frame), so the VR recentre applies to every placement
    [DefaultExecutionOrder(32000)]
    public sealed class VrPlacer : MonoBehaviour
    {
        public CameraRig Rig;
        void LateUpdate() { if (Rig) Rig.PlaceVr(); }
    }
}
