using System;
using UnityEngine;

namespace HostileHorizon
{
    // Round 2, items 4 and 7: a downed pilot by their crashed ship, its beacon blinking. When a ship lands near them (within the
    // pickup range) with its engines off, they run to the ship's airlock (left side, behind the cockpit) and knock the three-knock
    // code on the hull, again every few seconds, until the airlock opens; then they climb aboard. If the ship's engines start, a
    // pilot within the exhaust range dies. Placeholder figure and wreck made of simple shapes (Round 6 brings the real art).
    public sealed class DownedPilot : MonoBehaviour
    {
        public enum State { Waiting, Running, Knocking, Boarding, Aboard, Dead }
        public static readonly Vector3 DoorOutside = new Vector3(-2.6f, 0, -1.6f), DoorInside = new Vector3(-1.6f, .1f, -1.6f);   // ship space (ship.py airlock door)

        public int Index;
        public Vector3 LandingSpot { get; private set; }
        public State Now { get; private set; }
        public Vector3 Beacon => beacon.position;
        public event Action<DownedPilot> OnBoarded;

        CanyonField field;
        Transform body, legL, legR, armL, armR, beacon; Light beaconLight; Material beaconMat;
        float clock, knockTimer, boardT; Vector3 boardFrom;
        int knocksLeft; float knockGap;

        public static DownedPilot Create(int index, PilotSite site, CanyonField f, Material lit)
        {
            var root = new GameObject($"Downed pilot {index + 1}");
            var p = root.AddComponent<DownedPilot>(); p.Index = index; p.field = f; p.LandingSpot = site.landing;
            Material M(Color c, float smooth = .3f) { var m = new Material(lit); m.SetColor("_BaseColor", c); m.SetFloat("_Smoothness", smooth); m.SetFloat("_Metallic", 0); return m; }
            Material suit = M(new Color(.85f, .38f, .1f)), helmet = M(new Color(.88f, .88f, .85f), .6f), visor = M(new Color(.05f, .06f, .08f), .95f), dark = M(new Color(.16f, .16f, .17f));
            Material scorch = M(new Color(.07f, .06f, .055f), .05f), wreckHull = M(new Color(.42f, .43f, .42f), .35f), burnt = M(new Color(.12f, .11f, .1f), .2f);

            // the figure (pivot at the feet)
            p.body = new GameObject("Pilot").transform; p.body.SetParent(root.transform, false); p.body.position = site.pilot;
            Part(PrimitiveType.Capsule, p.body, new Vector3(0, 1.15f, 0), new Vector3(.46f, .42f, .32f), suit);
            Part(PrimitiveType.Cube, p.body, new Vector3(0, 1.2f, -.21f), new Vector3(.36f, .46f, .18f), dark);
            Part(PrimitiveType.Sphere, p.body, new Vector3(0, 1.72f, 0), Vector3.one * .3f, helmet);
            Part(PrimitiveType.Sphere, p.body, new Vector3(0, 1.73f, .1f), new Vector3(.24f, .16f, .14f), visor);
            p.legL = Limb(p.body, new Vector3(-.11f, .8f, 0), new Vector3(.17f, .4f, .17f), suit);
            p.legR = Limb(p.body, new Vector3(.11f, .8f, 0), new Vector3(.17f, .4f, .17f), suit);
            p.armL = Limb(p.body, new Vector3(-.29f, 1.47f, 0), new Vector3(.13f, .34f, .13f), suit);
            p.armR = Limb(p.body, new Vector3(.29f, 1.47f, 0), new Vector3(.13f, .34f, .13f), suit);

            // their crashed ship, nose dug in, a scorched patch round it
            var w = new GameObject("Wreck").transform; w.SetParent(root.transform, false);
            f.SurfaceHeight(site.wreck.x, site.wreck.z, out var n);
            w.SetPositionAndRotation(site.wreck, Quaternion.FromToRotation(Vector3.up, n) * Quaternion.Euler(0, site.wreckHeading, 0));
            Part(PrimitiveType.Cylinder, w, new Vector3(0, .02f, 0), new Vector3(11, .02f, 11), scorch);
            Part(PrimitiveType.Cube, w, new Vector3(0, .5f, 0), new Vector3(2.0f, 1.1f, 5.6f), wreckHull, new Vector3(-9, 0, 6));
            Part(PrimitiveType.Sphere, w, new Vector3(0, 1.0f, 1.6f), new Vector3(1.3f, .8f, 1.6f), burnt);
            Part(PrimitiveType.Cube, w, new Vector3(-2.6f, .25f, -.6f), new Vector3(3.6f, .12f, 1.7f), wreckHull, new Vector3(0, 8, -14));
            Part(PrimitiveType.Cube, w, new Vector3(3.4f, .1f, 1.5f), new Vector3(2.2f, .12f, 1.4f), burnt, new Vector3(0, 40, 6));   // the broken-off wing
            Part(PrimitiveType.Cube, w, new Vector3(0, 1.4f, -2.4f), new Vector3(.15f, 1.3f, 1.1f), wreckHull, new Vector3(-15, 0, 0));
            // the beacon on a short mast: a blinking amber lamp
            p.beaconMat = new Material(Shader.Find("HostileHorizon/Glow"));
            Part(PrimitiveType.Cylinder, w, new Vector3(1.6f, .9f, -1.5f), new Vector3(.08f, .9f, .08f), dark);
            p.beacon = Part(PrimitiveType.Sphere, w, new Vector3(1.6f, 1.9f, -1.5f), Vector3.one * .32f, p.beaconMat).transform;
            p.beaconLight = new GameObject("Beacon light").AddComponent<Light>(); p.beaconLight.transform.SetParent(p.beacon, false);
            p.beaconLight.type = LightType.Point; p.beaconLight.range = 40; p.beaconLight.color = new Color(1f, .6f, .2f); p.beaconLight.shadows = LightShadows.None;
            p.FaceTowards(site.wreck, true); p.body.Rotate(0, 180, 0);   // standing with their back to the wreck
            return p;
        }

        static GameObject Part(PrimitiveType t, Transform parent, Vector3 at, Vector3 size, Material m, Vector3 euler = default)
        {
            var g = GameObject.CreatePrimitive(t); Destroy(g.GetComponent<Collider>());
            g.transform.SetParent(parent, false); g.transform.localPosition = at; g.transform.localScale = size; g.transform.localRotation = Quaternion.Euler(euler);
            var r = g.GetComponent<Renderer>(); r.sharedMaterial = m; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            return g;
        }

        static Transform Limb(Transform parent, Vector3 joint, Vector3 size, Material m)
        {
            var j = new GameObject("Joint").transform; j.SetParent(parent, false); j.localPosition = joint;
            Part(PrimitiveType.Capsule, j, new Vector3(0, -size.y, 0), size, m);
            return j;
        }

        void FaceTowards(Vector3 p, bool now = false)
        {
            var d = p - body.position; d.y = 0; if (d.sqrMagnitude < .01f) return;
            var want = Quaternion.LookRotation(d);
            body.rotation = now ? want : Quaternion.RotateTowards(body.rotation, want, 360f * Time.deltaTime);
        }

        // Called every frame by the Campaign with the rescue ship; cabinFree = room aboard.
        public void Tick(ShipController ship, bool cabinFree, float dt)
        {
            clock += dt;
            if (Now == State.Aboard) return;
            bool blink = clock % 1.2f < .25f;
            beaconMat.SetColor("_Color", blink ? new Color(1f, .62f, .25f) * 3f : new Color(.25f, .12f, .05f));
            beaconLight.intensity = blink ? 6 : 0;
            if (Now == State.Dead) return;

            var t = Tuning.T;
            var st = ship.transform;
            Vector3 door = st.TransformPoint(DoorOutside); door.y = field.SurfaceHeight(door.x, door.z);
            bool ready = ship.gameObject.activeInHierarchy && ship.Landed && !ship.EnginesOn && !ship.Crashed;
            float toDoor = Vector3.Distance(body.position, door);
            Pose(0, 0);
            switch (Now)
            {
                case State.Waiting:
                    if (ready && cabinFree && toDoor < t.pickupRange) { Now = State.Running; break; }
                    if (ship.gameObject.activeInHierarchy && Vector3.Distance(body.position, st.position) < 500f)
                    {
                        FaceTowards(st.position);
                        Pose(0, Mathf.Sin(clock * 9f) * .5f + 2.4f);   // waving the ship down
                    }
                    break;
                case State.Running:
                    if (!ready) { Now = State.Waiting; break; }
                    FaceTowards(door);
                    var step = Vector3.MoveTowards(body.position, door, t.pilotRunSpeed * dt);
                    step.y = field.SurfaceHeight(step.x, step.z); body.position = step;
                    float s = Mathf.Sin(clock * 11f);
                    Pose(s * .8f, -s * .7f);
                    if (toDoor < 1.0f) { Now = State.Knocking; knockTimer = .6f; }
                    break;
                case State.Knocking:
                    if (!ship.Landed) { Now = State.Waiting; break; }
                    FaceTowards(st.TransformPoint(DoorInside));
                    knockTimer -= dt;
                    if (knockTimer <= 0) { knocksLeft = 3; knockGap = 0; knockTimer = 7f; }
                    if (knocksLeft > 0) { knockGap -= dt; if (knockGap <= 0) { Sfx.Play(Sfx.Knock, st.TransformPoint(new Vector3(-1.75f, .3f, -1.4f)), 1f, 3f); knocksLeft--; knockGap = .34f; } }
                    Pose(0, knocksLeft > 0 ? 1.5f + Mathf.Max(0, Mathf.Sin(clock * 18f)) * .4f : 0);
                    break;
                case State.Boarding:
                    boardT += dt / 1.4f;
                    body.position = Vector3.Lerp(boardFrom, st.TransformPoint(DoorInside), Mathf.SmoothStep(0, 1, boardT));
                    FaceTowards(st.TransformPoint(DoorInside));
                    if (boardT >= 1) { Now = State.Aboard; body.gameObject.SetActive(false); OnBoarded?.Invoke(this); }
                    break;
            }
        }

        void Pose(float legs, float rightArm)
        {
            legL.localRotation = Quaternion.Euler(legs * 35f, 0, 0); legR.localRotation = Quaternion.Euler(-legs * 35f, 0, 0);
            armL.localRotation = Quaternion.Euler(-legs * 30f, 0, 0);
            armR.localRotation = Quaternion.Euler(rightArm > 1 ? -rightArm * 60f : legs * 30f, 0, 0);
        }

        public bool AtDoor => Now == State.Knocking;
        public Vector3 Position => body.position;
        public bool OnFoot => Now == State.Waiting || Now == State.Running || Now == State.Knocking;

        public void Board() { if (Now != State.Knocking) return; Now = State.Boarding; boardT = 0; boardFrom = body.position; }

        public void Kill()
        {
            Now = State.Dead;
            body.rotation = body.rotation * Quaternion.Euler(-90, 0, 0); body.position += Vector3.up * .25f;
            foreach (var r in body.GetComponentsInChildren<Renderer>()) { var m = new Material(r.sharedMaterial); m.SetColor("_BaseColor", new Color(.09f, .08f, .07f)); r.sharedMaterial = m; }
            Pose(0, 0);
        }
    }
}
