using System.Collections.Generic;
using UnityEngine;

namespace HostileHorizon
{
    // Fix 8.1: the player's own body in the seat (flat cockpit view and VR) and the ship's stick and throttle moving with the input.
    // The body is the corps pilot from Fix 3.1 (Resources/Characters/pilot, its suit and gloves), posed in code every frame (no
    // animation clip): hips on the seat, spine up to just under the eye, legs bent with the feet on the floor either side of the floor
    // window, the right hand on the stick and the left on the throttle by two-bone IK, fingers curled round them; the index finger pulls
    // the trigger while firing, thumbs press for the stick-head / throttle buttons. No head in your own view (shrunk to nothing at the neck's
    // base: in VR the head is the headset, flat the camera is the head). (0.8.1 turned the chest and neck with the head: Fix 8.2
    // keeps the body still.) The stick (pivot at its boot) tilts with pitch / roll and twists with yaw; the throttle lever swings with the
    // throttle (the HOTAS lever when bound). Both are the seat's own Meshy controls, cut out in ship.py (groups Stick, Throttle).
    // Fix 8.2 (Dan: "Hands keep moving all over the place"): causes found (fix82 check, CheckOldPose = 0.8.1's pose, 20 s logged): (1) the
    // chest and neck turns were applied on top of the previous frame's pose (the animator is off, nothing reset them), so any head turn
    // twisted the chest a little further every frame (up to 180 degrees), swinging the shoulders (up to 364 mm) and the arms, and the hands
    // slid up to 247 mm on the stick / throttle; (2) the whole torso followed the head camera (VR: the headset), a frame late, switching to
    // the seat when the camera was over .35 m from the eye point. Now every bone starts each frame from the model's rest pose, the body
    // sits still in the seat (it no longer follows the head), and the hands move only with the stick and throttle, smoothed a little
    // (the index finger only on fire). New pose over 60 s: hands within 0.3 mm of their place on the controls, shoulders still.
    public sealed class PilotBody : MonoBehaviour
    {
        public const float StickTilt = 18f, StickTwist = 15f, LeverSwing = 22f;
        public Transform Stick { get; private set; }
        public Transform Throttle { get; private set; }
        public Transform Root { get; private set; }
        public Renderer[] Renderers { get; private set; }
        public Vector3 StickGrip, ThrottleGrip;   // in the pivots' space
        public float Lever { get; private set; }  // 0..1 as drawn
        // checks: forced inputs (NaN = live)
        public static float CheckPitch = float.NaN, CheckRoll = float.NaN, CheckYaw = float.NaN, CheckLever = float.NaN;
        public static bool CheckFire;
        public static bool CheckOldPose;   // checks: 0.8.1's pose (the torso follows the head, turns added onto the last frame's pose, no smoothing)
        public const float Smooth = 18f;   // 1/s: the drawn stick and lever follow the input with a ~55 ms lag (no jitter)
        float sPitch, sRoll, sYaw, sLever = -1;
        readonly List<(Transform t, Quaternion r)> restLocal = new List<(Transform, Quaternion)>();

        ShipController ship;
        Animator anim;
        readonly Dictionary<HumanBodyBones, Transform> b = new Dictionary<HumanBodyBones, Transform>();
        readonly Dictionary<Transform, Quaternion> restRot = new Dictionary<Transform, Quaternion>();   // relative to Root
        readonly Dictionary<Transform, Vector3> restPos = new Dictionary<Transform, Vector3>();         // in Root's space
        public float SpineLength { get; private set; }

        // Ship-space points of the pose (seat: ship.py SEAT_AT; window: WIN_C)
        static readonly Vector3 HipSeat = new Vector3(0, .48f, 2.18f);
        static readonly Vector3 FootL = new Vector3(-.27f, .085f, 2.86f), FootR = new Vector3(.27f, .085f, 2.86f);

        public static PilotBody Add(ShipController ship, Transform model)
        {
            var pb = ship.gameObject.AddComponent<PilotBody>(); pb.ship = ship;
            pb.Stick = pb.Pivot(model, "Stick", true); pb.Throttle = pb.Pivot(model, "Throttle", false);
            var prefab = CharacterRig.Has("pilot") ? Resources.Load<GameObject>("Characters/pilot") : null;
            if (prefab) pb.MakeBody(prefab);
            return pb;
        }

        // the stick / throttle renderer onto a pivot at the bottom middle of its bounds (the ship is still at the origin, unturned)
        Transform Pivot(Transform model, string group, bool stick)
        {
            Renderer r = null;
            foreach (var x in model.GetComponentsInChildren<Renderer>(true)) if (x.name.StartsWith(group + "__")) r = x;
            var pv = new GameObject(group + " pivot").transform; pv.SetParent(transform, false);
            if (!r) { Debug.LogWarning($"PilotBody: no {group} part in the ship model"); return pv; }
            var bd = r.bounds;
            pv.localPosition = transform.InverseTransformPoint(new Vector3(bd.center.x, bd.min.y, bd.center.z));
            r.transform.SetParent(pv, true);
            // where the hand holds it: the stick's grip 60 % up; the throttle's T-bar at its top
            var grip = stick ? new Vector3(bd.center.x, bd.min.y + bd.size.y * .62f, bd.center.z) : new Vector3(bd.center.x, bd.max.y - .016f, bd.center.z);
            if (stick) StickGrip = pv.InverseTransformPoint(grip); else ThrottleGrip = pv.InverseTransformPoint(grip);
            return pv;
        }

        void MakeBody(GameObject prefab)
        {
            Root = new GameObject("Pilot body").transform; Root.SetParent(transform, false);
            var go = Instantiate(prefab, Root, false); go.name = "pilot";
            anim = go.GetComponent<Animator>();
            if (!anim || !anim.isHuman) { Destroy(Root.gameObject); Root = null; return; }
            for (var h = HumanBodyBones.Hips; h < HumanBodyBones.LastBone; h++) { var t = anim.GetBoneTransform(h); if (t) b[h] = t; }
            anim.enabled = false;   // posed in code (the rest pose below is the model's own)
            var mat = Resources.Load<Material>("Characters/pilot");
            Renderers = go.GetComponentsInChildren<Renderer>(true);
            foreach (var r in Renderers) { if (mat) r.sharedMaterial = mat; if (r is SkinnedMeshRenderer s) { s.updateWhenOffscreen = true; } }
            foreach (var t in go.GetComponentsInChildren<Transform>(true))
            { restRot[t] = Quaternion.Inverse(Root.rotation) * t.rotation; restPos[t] = Root.InverseTransformPoint(t.position); restLocal.Add((t, t.localRotation)); }
            SpineLength = Vector3.Distance(b[HumanBodyBones.Hips].position, b[HumanBodyBones.Head].position);
            // no head: yours is the headset / the camera. Shrunk at the neck's base (0.8.1's first try shrank the head bone: the helmet's
            // collar folded to a point next to the eye and showed as a grey sheet when looking back over a shoulder)
            (b.TryGetValue(HumanBodyBones.Neck, out var nb) ? nb : b[HumanBodyBones.Head]).localScale = Vector3.one * .0001f;
        }

        Vector3 RestDir(HumanBodyBones from, HumanBodyBones to) => (restPos[b[to]] - restPos[b[from]]).normalized;

        // turn a bone so its rest direction (root space) points along dir, its rest hint (root space) toward hint (world)
        void Aim(Transform t, Vector3 restDirR, Vector3 restHintR, Vector3 dir, Vector3 hint)
        {
            var rr = Root.rotation;
            var from = Quaternion.LookRotation(rr * restDirR, rr * restHintR);
            var to = Quaternion.LookRotation(dir, hint);
            t.rotation = to * Quaternion.Inverse(from) * (rr * restRot[t]);
        }

        // two-bone IK: upper / lower bone from a toward target c, bending toward pole; bend: the rest plane's sign (+z arms, -z legs)
        void Limb(HumanBodyBones up, HumanBodyBones low, HumanBodyBones end, Vector3 c, Vector3 pole, Vector3 bendR)
        {
            Transform A = b[up], B = b[low];
            float la = Vector3.Distance(restPos[A], restPos[B]), lb = Vector3.Distance(restPos[B], restPos[b[end]]);
            Vector3 a = A.position, d = c - a; float dist = Mathf.Clamp(d.magnitude, .05f, (la + lb) * .999f); var dn = d.normalized;
            float cosA = Mathf.Clamp((la * la + dist * dist - lb * lb) / (2 * la * dist), -1, 1);
            var perp = Vector3.ProjectOnPlane(pole - a, dn).normalized;
            var knee = a + dn * la * cosA + perp * la * Mathf.Sqrt(1 - cosA * cosA);
            var tip = a + dn * dist;
            var n = Vector3.Cross(knee - a, tip - knee).normalized;
            var ru = RestDir(up, low); var rl = RestDir(low, end);
            Aim(A, ru, Vector3.Cross(ru, bendR), knee - a, n);
            Aim(B, rl, Vector3.Cross(rl, bendR), tip - knee, n);
        }

        // the hand: fingers (wrist to the middle knuckle) along fdir, the palm facing pn; wrist placed so the palm closes on grip
        void Hand(bool right, Vector3 grip, Vector3 fdir, Vector3 pn, float index, float others, float thumb)
        {
            var H = right ? HumanBodyBones.RightHand : HumanBodyBones.LeftHand;
            var mid = right ? HumanBodyBones.RightMiddleProximal : HumanBodyBones.LeftMiddleProximal;
            var ip = right ? HumanBodyBones.RightIndexProximal : HumanBodyBones.LeftIndexProximal;
            var lp = right ? HumanBodyBones.RightLittleProximal : HumanBodyBones.LeftLittleProximal;
            if (!b.ContainsKey(mid) || !b.ContainsKey(ip) || !b.ContainsKey(lp)) return;
            var f0 = RestDir(H, mid); var across = (restPos[b[ip]] - restPos[b[lp]]).normalized;
            var p0 = right ? Vector3.Cross(f0, across) : Vector3.Cross(across, f0);
            var t = b[H];
            Aim(t, f0, p0, fdir, pn);
            // curl: each finger bends toward the palm (axis across the hand)
            var k = Vector3.Cross(fdir, pn).normalized;
            Curl(right, "Index", index, k); Curl(right, "Middle", others, k); Curl(right, "Ring", others, k); Curl(right, "Little", others * 1.05f, k);
            Curl(right, "Thumb", thumb, Vector3.Cross(t.rotation * Quaternion.Inverse(Root.rotation * restRot[t]) * (Root.rotation * RestDir(H, right ? HumanBodyBones.RightThumbProximal : HumanBodyBones.LeftThumbProximal)), pn).normalized);
        }

        void Curl(bool right, string finger, float deg, Vector3 axis)
        {
            string side = right ? "Right" : "Left";
            float[] share = { 1f, 1.1f, .7f };
            string[] seg = { "Proximal", "Intermediate", "Distal" };
            for (int i = 0; i < 3; i++)
            {
                if (!System.Enum.TryParse<HumanBodyBones>(side + finger + seg[i], out var h) || !b.TryGetValue(h, out var t)) continue;
                t.rotation = Quaternion.AngleAxis(deg * share[i], axis) * t.rotation;
            }
        }

        void LateUpdate()
        {
            // the controls first (the hands follow them)
            bool live = ship && ship.isActiveAndEnabled;
            float pitch = !float.IsNaN(CheckPitch) ? CheckPitch : live ? Controls.Pitch : 0;
            float roll = !float.IsNaN(CheckRoll) ? CheckRoll : live ? Controls.Roll : 0;
            float yaw = !float.IsNaN(CheckYaw) ? CheckYaw : live ? Controls.Yaw : 0;
            float leverIn = !float.IsNaN(CheckLever) ? CheckLever : Controls.HasLever ? Controls.ThrottleLever : ship ? ship.Throttle : .3f;
            if (CheckOldPose) Lever = leverIn;
            else
            {
                // Fix 8.2: smoothed (a check's forced pose is taken at once)
                bool forced = !float.IsNaN(CheckPitch) || !float.IsNaN(CheckLever) || sLever < 0;
                float k = forced ? 1 : 1 - Mathf.Exp(-Smooth * Time.unscaledDeltaTime);
                sPitch += (pitch - sPitch) * k; sRoll += (roll - sRoll) * k; sYaw += (yaw - sYaw) * k; sLever = Mathf.Max(0, sLever) + (leverIn - Mathf.Max(0, sLever)) * k;
                pitch = sPitch; roll = sRoll; yaw = sYaw; Lever = sLever;
            }
            if (Stick) Stick.localRotation = Quaternion.Euler(-pitch * StickTilt, yaw * StickTwist, -roll * StickTilt);   // pull = top toward you
            if (Throttle) Throttle.localRotation = Quaternion.Euler((Lever - .5f) * 2f * LeverSwing, 0, 0);                  // forward = full
            if (!Root) return;
            if (!CheckOldPose) foreach (var (t, r) in restLocal) t.localRotation = r;   // Fix 8.2: every frame from the rest pose

            var sr = transform.rotation; Vector3 up = transform.up, fwd = transform.forward, right = transform.right;
            // where the neck (head bone) goes: under and behind the eye: the camera's (VR: the headset's), within reach of the seat
            var cam = GameShell.I && GameShell.I.Rig ? GameShell.I.Rig.Head.transform : null;
            var eyeHome = transform.TransformPoint(ShipLayout.Eye);
            bool inCockpit = CheckOldPose && cam && Vector3.Distance(cam.position, eyeHome) < .35f;   // Fix 8.2: the body stays in the seat
            var headRot = inCockpit ? cam.rotation : sr;
            var eye = inCockpit ? cam.position : eyeHome;
            var neck = eye - headRot * new Vector3(0, .1f, .075f);
            var hipSeat = transform.TransformPoint(HipSeat);
            var spineDir = (neck - hipSeat).normalized;

            // hips: the body turned with the ship, placed so the head bone lands on the neck point
            Root.rotation = sr; Root.position = transform.position;
            var hips = b[HumanBodyBones.Hips];
            hips.rotation = sr * restRot[hips];
            hips.position = neck - spineDir * SpineLength;
            // the spine leans to the neck point; the chest and neck turn a little with the head (looking down feels like your body)
            var spine = b[HumanBodyBones.Spine];
            Aim(spine, RestDir(HumanBodyBones.Spine, HumanBodyBones.Head), Vector3.forward, neck - spine.position, fwd);
            float look = Mathf.DeltaAngle(0, (Quaternion.Inverse(sr) * headRot).eulerAngles.y);
            if (CheckOldPose && b.TryGetValue(HumanBodyBones.Chest, out var chest)) chest.rotation = Quaternion.AngleAxis(Mathf.Clamp(look * .2f, -18, 18), up) * chest.rotation;
            if (CheckOldPose && b.TryGetValue(HumanBodyBones.Neck, out var nk)) nk.rotation = Quaternion.AngleAxis(Mathf.Clamp(look * .25f, -25, 25), up) * nk.rotation;

            // legs: thighs along the seat, shins down, feet flat on the floor either side of the window
            var lf = transform.TransformPoint(FootL); var rf = transform.TransformPoint(FootR);
            Limb(HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot, lf, lf + fwd * .6f + up * .5f - right * .1f, Vector3.back);
            Limb(HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot, rf, rf + fwd * .6f + up * .5f + right * .1f, Vector3.back);
            foreach (var (f, toe) in new[] { (HumanBodyBones.LeftFoot, HumanBodyBones.LeftToes), (HumanBodyBones.RightFoot, HumanBodyBones.RightToes) })
                if (b.ContainsKey(toe)) Aim(b[f], RestDir(f, toe), Vector3.up, Vector3.ProjectOnPlane(fwd, up).normalized * .94f - up * .34f, up);

            // arms: the right hand round the stick's grip, the left on the throttle's T-bar
            bool fire = CheckFire || (live && Controls.FireHeld), stickBtn = live && (Controls.ShieldHeld || Controls.LevelOutHeld), lever = live && Controls.LandHeld;
            var sg = Stick.TransformPoint(StickGrip);
            var sf = Stick.TransformDirection(new Vector3(0, -.25f, 1)).normalized; var sp = Stick.TransformDirection(new Vector3(-1, 0, .1f)).normalized;
            var tg = Throttle.TransformPoint(ThrottleGrip);
            var tf = Throttle.TransformDirection(new Vector3(.2f, -.45f, 1)).normalized; var tp = Throttle.TransformDirection(new Vector3(.15f, -1, .35f)).normalized;
            var rw = sg - sp * .03f - sf * .055f; var lw = tg - tp * .03f - tf * .055f;
            var rs = b[HumanBodyBones.RightUpperArm].position; var ls = b[HumanBodyBones.LeftUpperArm].position;
            Limb(HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand, rw, rs + right * .25f - up * .35f - fwd * .25f, Vector3.forward);
            Limb(HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand, lw, ls - right * .25f - up * .35f - fwd * .2f, Vector3.forward);
            Hand(true, sg, sf, sp, fire ? 62f : 34f, 78f, stickBtn ? 40f : 14f);
            Hand(false, tg, tf, tp, 70f, 74f, lever ? 38f : 12f);
            IndexPulled = fire;
        }
        public bool IndexPulled { get; private set; }   // checks
    }
}
