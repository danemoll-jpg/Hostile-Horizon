using System;
using System.Collections.Generic;
using UnityEngine;

namespace HostileHorizon
{
    // Round 2, items 4 and 7: a downed pilot by their crashed ship, its beacon blinking. When a ship lands near them (within the
    // pickup range) with its engines off, they run to the ship's airlock (left side, behind the cockpit) and knock the three-knock
    // code on the hull, again every few seconds, until the airlock opens; then they climb aboard. If the ship's engines start, a
    // pilot within the exhaust range dies. Placeholder figure and wreck made of simple shapes (Round 6 brings the real art).
    // Round 3: some "pilots" are aliens (Type), in the same suit. Quirk is the tell it shows (the type's own, some of the time; or a
    // real pilot's false tell, off on Normal), always in the 3D world (VR rule 4):
    //   Mimic: its shadow is wrong (a taller, long-armed shape that lags behind it); Hollow: its visor reflects nothing (real visors
    //   show the sky and a glint of the ship's lamps; this one is a matte black hole); Knocker: knocks wrong (four knocks, the wrong
    //   rhythm, or from the wrong place on the hull); Stalker: doesn't run straight to the airlock: circles the ship (dropping off the
    //   radar for a moment) and comes from behind. The Sleeper shows nothing outside. Between knocks every figure at the door turns to
    //   look up into the airlock camera. An alien left at a closed airlock climbs onto the canopy after canopyClimbAfter s (OnClimb;
    //   the Campaign plays it); a real pilot never does: they keep knocking.
    public sealed class DownedPilot : MonoBehaviour
    {
        public enum State { Waiting, Running, Knocking, Boarding, Aboard, Dead, Gone }
        public static readonly Vector3 DoorOutside = new Vector3(-2.45f, 0, .8f), DoorInside = new Vector3(-1.6f, .1f, -1.6f);   // ship space (ship.py airlock door)
        // Round 3: they wait (and knock on the hull) just ahead of the left wing's leading edge, not under the wing (0.2.5: x -2.6 z -1.6,
        // where the wing hid the head and visor from the airlock camera and the cockpit)

        public int Index;
        public AlienType Type { get; private set; }       // what it really is (None: a real pilot)
        public AlienType Quirk { get; private set; }      // the tell it shows (None: nothing)
        public bool IsAlien => Type != AlienType.None;
        public Vector3 LandingSpot { get; private set; }
        public State Now { get; private set; }
        public Vector3 Beacon => beacon.position;
        public event Action<DownedPilot> OnBoarded, OnClimb;
        public event Action<DownedPilot, int> OnKnock;    // the knock's number in its set (1, 2, 3 ...)

        CanyonField field;
        Transform body, legL, legR, armL, armR, beacon; Light beaconLight; Material beaconMat;
        float clock, knockTimer, boardT; Vector3 boardFrom;
        int knocksLeft, knockNo; float knockGap;
        // Fix 2.5: whether they can get to the ship: level distance to the door within the pickup range and the height between within
        // pilotMaxClimb (they don't climb canyon walls). The Campaign says so when they can't.
        public float LevelToDoor { get; private set; } = 9999;
        public float ClimbToDoor { get; private set; }
        public bool CanReach { get; private set; }
        public int Knocks { get; private set; }               // Fix 2.5: knocks played (checks log them and their sound)
        public AudioSource LastKnock { get; private set; }
        public float AtDoorFor { get; private set; }          // Round 3: s at the door with the airlock shut
        public string KnockPattern { get; private set; } = "three knocks (the corps' code)";
        public bool HiddenFromRadar { get; private set; }     // Round 3: the Stalker drops off the radar for a moment
        public float Circled { get; private set; }            // Round 3: degrees the Stalker has run round the ship
        // Mimic: the shadow body (shadows only) and where the body was a moment ago
        public Transform WrongShadow => shadow;   // checks: the Mimic's lagging shadow body
        Transform shadow; readonly Queue<(float t, Vector3 p, Quaternion r)> trail = new Queue<(float, Vector3, Quaternion)>();
        float stalkAngle; bool stalkDone;

        public static DownedPilot Create(int index, PilotSite site, CanyonField f, Material lit, AlienType type = AlienType.None, AlienType quirk = AlienType.None)
        {
            var root = new GameObject($"Downed pilot {index + 1}");
            var p = root.AddComponent<DownedPilot>(); p.Index = index; p.field = f; p.LandingSpot = site.landing; p.Type = type; p.Quirk = quirk;
            Material M(Color c, float smooth = .3f) { var m = new Material(lit); m.SetColor("_BaseColor", c); m.SetFloat("_Smoothness", smooth); m.SetFloat("_Metallic", 0); return m; }
            Material suit = M(new Color(.85f, .38f, .1f)), helmet = M(new Color(.88f, .88f, .85f), .6f), dark = M(new Color(.16f, .16f, .17f));
            // the visor: a real one is a dark mirror (sky in it, and a glint); the Hollow's is a flat black that gives back nothing
            Material visor = quirk == AlienType.Hollow ? M(new Color(0, 0, 0), 0) : M(new Color(.1f, .14f, .2f), .97f);
            if (quirk != AlienType.Hollow) visor.SetFloat("_Metallic", .6f);
            Material scorch = M(new Color(.07f, .06f, .055f), .05f), wreckHull = M(new Color(.42f, .43f, .42f), .35f), burnt = M(new Color(.12f, .11f, .1f), .2f);

            // the figure (pivot at the feet). Fix 3.1: the rigged pilot model (the Hollow's disguise with a dead visor); the code-made
            // figure below if the model is missing.
            p.body = new GameObject("Pilot").transform; p.body.SetParent(root.transform, false); p.body.position = site.pilot;
            p.rig = CharacterRig.Create("pilot", p.body, quirk == AlienType.Hollow ? "pilot_hollow" : null);
            if (p.rig) { p.RigSetup(root.transform, site.pilot, quirk, dark); p.FaceTowards(site.wreck, true); p.body.Rotate(0, 180, 0); p.Wreck(site, f, scorch, wreckHull, burnt, dark); return p; }
            Part(PrimitiveType.Capsule, p.body, new Vector3(0, 1.15f, 0), new Vector3(.46f, .42f, .32f), suit);
            Part(PrimitiveType.Cube, p.body, new Vector3(0, 1.2f, -.21f), new Vector3(.36f, .46f, .18f), dark);
            Part(PrimitiveType.Sphere, p.body, new Vector3(0, 1.72f, 0), Vector3.one * .3f, helmet);
            Part(PrimitiveType.Sphere, p.body, new Vector3(0, 1.73f, .1f), new Vector3(.24f, .16f, .14f), visor);
            if (quirk != AlienType.Hollow)
            {
                // the glint: the sky's and the ship's lamps' light caught in the curve of the visor
                var gm = new Material(Shader.Find("HostileHorizon/Glow")); gm.SetColor("_Color", new Color(.85f, .92f, 1f) * 1.4f);
                Part(PrimitiveType.Sphere, p.body, new Vector3(-.05f, 1.77f, .165f), new Vector3(.05f, .025f, .02f), gm).GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                var sky = new Material(Shader.Find("HostileHorizon/Glow")); sky.SetColor("_Color", new Color(.35f, .5f, .7f) * .55f);
                Part(PrimitiveType.Sphere, p.body, new Vector3(.03f, 1.755f, .163f), new Vector3(.12f, .05f, .02f), sky).GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            p.legL = Limb(p.body, new Vector3(-.11f, .8f, 0), new Vector3(.17f, .4f, .17f), suit);
            p.legR = Limb(p.body, new Vector3(.11f, .8f, 0), new Vector3(.17f, .4f, .17f), suit);
            p.armL = Limb(p.body, new Vector3(-.29f, 1.47f, 0), new Vector3(.13f, .34f, .13f), suit);
            p.armR = Limb(p.body, new Vector3(.29f, 1.47f, 0), new Vector3(.13f, .34f, .13f), suit);
            if (quirk == AlienType.Mimic)
            {
                // the real figure casts no shadow; a wrong one does: taller, thin, long-armed, and a moment behind
                foreach (var r in p.body.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                p.shadow = new GameObject("Wrong shadow").transform; p.shadow.SetParent(root.transform, false); p.shadow.position = site.pilot;
                void S(Vector3 at, Vector3 size, Vector3 rot) { Part(PrimitiveType.Capsule, p.shadow, at, size, dark, rot).GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly; }
                S(new Vector3(0, 1.45f, 0), new Vector3(.32f, .75f, .25f), Vector3.zero);                 // a long thin body
                S(new Vector3(0, 2.35f, .1f), new Vector3(.22f, .3f, .3f), new Vector3(60, 0, 0));        // the head, pushed forward
                S(new Vector3(-.36f, 1.2f, .15f), new Vector3(.09f, .8f, .09f), new Vector3(-15, 0, -12)); S(new Vector3(.36f, 1.2f, .15f), new Vector3(.09f, .8f, .09f), new Vector3(-15, 0, 12));   // arms past the knees
                S(new Vector3(-.12f, .45f, 0), new Vector3(.1f, .5f, .1f), Vector3.zero); S(new Vector3(.12f, .45f, 0), new Vector3(.1f, .5f, .1f), Vector3.zero);
            }
            if (quirk == AlienType.Knocker) p.KnockPattern = (index % 3) == 0 ? "four knocks" : (index % 3) == 1 ? "the wrong rhythm" : "from the wrong place (the nose)";
            p.Wreck(site, f, scorch, wreckHull, burnt, dark);
            p.FaceTowards(site.wreck, true); p.body.Rotate(0, 180, 0);   // standing with their back to the wreck
            return p;
        }

        // Fix 3.1: the rigged figure's extras. The Mimic: the body casts no shadow; its wrong shadow is the Mimic itself (the creature
        // model, shadows only, a moment behind and in the same motion). The Knocker's pattern as before.
        CharacterRig rig, shadowRig;
        public CharacterRig Rig => rig;
        public CharacterRig ShadowRig => shadowRig;
        void RigSetup(Transform root, Vector3 at, AlienType quirk, Material dark)
        {
            if (quirk == AlienType.Mimic)
            {
                rig.ShadowMode(UnityEngine.Rendering.ShadowCastingMode.Off);
                shadow = new GameObject("Wrong shadow").transform; shadow.SetParent(root, false); shadow.position = at;
                shadowRig = CharacterRig.Create("mimic", shadow);
                if (shadowRig) shadowRig.ShadowMode(UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly);
                else
                {
                    void S(Vector3 p, Vector3 size, Vector3 rot) { Part(PrimitiveType.Capsule, shadow, p, size, dark, rot).GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly; }
                    S(new Vector3(0, 1.45f, 0), new Vector3(.32f, .75f, .25f), Vector3.zero); S(new Vector3(0, 2.35f, .1f), new Vector3(.22f, .3f, .3f), new Vector3(60, 0, 0));
                    S(new Vector3(-.36f, 1.2f, .15f), new Vector3(.09f, .8f, .09f), new Vector3(-15, 0, -12)); S(new Vector3(.36f, 1.2f, .15f), new Vector3(.09f, .8f, .09f), new Vector3(-15, 0, 12));
                }
            }
            if (quirk == AlienType.Knocker) KnockPattern = (Index % 3) == 0 ? "four knocks" : (Index % 3) == 1 ? "the wrong rhythm" : "from the wrong place (the nose)";
        }

        // Fix 3.1: the clip for what the figure is doing (the rigged model only). Run speed matched to the ground speed.
        void Act(string clip, float speed = 1f, float fadeTime = .25f) { if (rig && rig.Current != clip && !(clip == "idle" && holding > 0)) rig.Play(clip, fadeTime, speed); }
        float holding;   // s left of a one-off clip (stop, knock, flinch) before the state's own clip takes over again

        // their crashed ship, nose dug in, a scorched patch round it
        void Wreck(PilotSite site, CanyonField f, Material scorch, Material wreckHull, Material burnt, Material dark)
        {
            var p = this;
            var w = new GameObject("Wreck").transform; w.SetParent(transform, false);
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
            if (Now == State.Aboard || Now == State.Gone) return;
            bool blink = clock % 1.2f < .25f;
            beaconMat.SetColor("_Color", blink ? new Color(1f, .62f, .25f) * 3f : new Color(.25f, .12f, .05f));
            beaconLight.intensity = blink ? 6 : 0;
            if (Now == State.Dead) return;

            var t = Tuning.T;
            var st = ship.transform;
            Vector3 door = st.TransformPoint(DoorOutside); door.y = field.SurfaceHeight(door.x, door.z);
            bool ready = ship.gameObject.activeInHierarchy && ship.Landed && !ship.EnginesOn && !ship.Crashed;
            Vector3 dd = door - body.position; float toDoor = new Vector2(dd.x, dd.z).magnitude;
            LevelToDoor = toDoor; ClimbToDoor = dd.y; CanReach = toDoor < t.pickupRange && Mathf.Abs(dd.y) < t.pilotMaxClimb;
            Pose(0, 0);
            HiddenFromRadar = false;
            holding -= dt;
            switch (Now)
            {
                case State.Waiting:
                    AtDoorFor = 0;
                    if (ready && cabinFree && CanReach) { Now = State.Running; stalkDone = Quirk != AlienType.Stalker; stalkAngle = 0; Circled = 0; break; }
                    if (ship.gameObject.activeInHierarchy && Vector3.Distance(body.position, st.position) < 500f)
                    {
                        FaceTowards(st.position);
                        Pose(0, Mathf.Sin(clock * 9f) * .5f + 2.4f);   // waving the ship down
                        Act(Index % 2 == 0 ? "wave" : "wave2");
                    }
                    else Act((clock + Index * 5f) % 16f < 9f ? "idle" : "look_around", 1f, .5f);   // waiting by the wreck, looking round the sky
                    break;
                case State.Running:
                    if (!ready) { Now = State.Waiting; break; }
                    // Fix 2.5: at a steady run over the ground, and round the hull to the door on the left. Round 3: the Stalker circles first.
                    var goal = stalkDone ? PathTarget(st, door) : StalkTarget(st, dt); goal.y = body.position.y;
                    // Fix 3.1: the Stalker on its circle side-steps, crouched, facing the ship (its creature's own sneak); else the run
                    bool sneaking = rig && !stalkDone && Circled > 0;
                    if (sneaking) { FaceTowards(st.position); Act("circle", 1.3f); }
                    else { FaceTowards(goal); Act("run", t.pilotRunSpeed * (stalkDone ? 1 : .8f) / 5.2f); }
                    var step = Vector3.MoveTowards(body.position, goal, t.pilotRunSpeed * (stalkDone ? 1 : .8f) * dt);
                    step.y = field.SurfaceHeight(step.x, step.z); body.position = step;
                    float s = Mathf.Sin(clock * 11f);
                    Pose(s * .8f, -s * .7f);
                    if (toDoor < 1.0f && stalkDone) { Now = State.Knocking; knockTimer = .6f; if (rig) { rig.PlayThen("run_stop", "idle"); holding = rig.Length("run_stop"); } }
                    break;
                case State.Knocking:
                    if (!ship.Landed) { Now = State.Waiting; break; }
                    AtDoorFor += dt;
                    knockTimer -= dt;
                    if (knockTimer <= 0) { StartKnocks(); knockTimer = 7f; }
                    if (knocksLeft > 0)
                    {
                        FaceTowards(st.TransformPoint(DoorInside));
                        knockGap -= dt;
                        if (knockGap <= 0) Knock(st);
                    }
                    else if (knockTimer < 5.5f) FaceTowards(st.TransformPoint(BellyCam.AirlockCamAt));   // looks up into the airlock camera
                    Pose(0, knocksLeft > 0 ? 1.5f + Mathf.Max(0, Mathf.Sin(clock * 18f)) * .4f : 0);
                    Act("idle");
                    if (IsAlien && Type != AlienType.Sleeper && AtDoorFor > t.canopyClimbAfter) { Hide(); OnClimb?.Invoke(this); }
                    break;
                case State.Boarding:
                    Act("climb", 1.6f, .15f);
                    boardT += dt / 1.4f;
                    body.position = Vector3.Lerp(boardFrom, st.TransformPoint(DoorInside), Mathf.SmoothStep(0, 1, boardT));
                    FaceTowards(st.TransformPoint(DoorInside));
                    if (boardT >= 1) { Now = State.Aboard; body.gameObject.SetActive(false); if (shadow) shadow.gameObject.SetActive(false); OnBoarded?.Invoke(this); }
                    break;
            }
            // the Mimic's wrong shadow: where the body was 0.6 s ago (Fix 3.1: the Mimic's own body, in the same motion)
            if (shadow && shadow.gameObject.activeSelf)
            {
                if (shadowRig && rig && shadowRig.Current != rig.Current) shadowRig.Play(rig.Current, .3f);
                trail.Enqueue((clock, body.position, body.rotation));
                while (trail.Count > 1 && trail.Peek().t < clock - .6f) trail.Dequeue();
                var old = trail.Peek(); shadow.SetPositionAndRotation(old.p, old.r);
            }
        }

        // Fix 3.1: the knock clip (Mixamo had no knock: Dan's "Opening", a hand to the door) plays once for each set of knocks; the knock
        // sounds keep their own timing (the Knocker's tell is in the sounds: count, rhythm, place).
        const float KnockClipFor = 2.5f;   // the start of Dan's "Opening" clip (11 s long): a hand to the door
        void StartKnocks()
        {
            knockNo = 0; knockGap = 0; knocksLeft = Quirk == AlienType.Knocker && Index % 3 == 0 ? 4 : 3;
            if (rig) { rig.PlayThen("knock", "idle", 1f, KnockClipFor); holding = KnockClipFor; }
        }

        // The corps' code: three knocks 0.34 s apart, on the airlock door. The Knocker: four; or the wrong rhythm (quick-quick ... long);
        // or from the wrong place: the knocks come from the nose while it stands at the door.
        void Knock(Transform st)
        {
            bool wrongPlace = Quirk == AlienType.Knocker && Index % 3 == 2, wrongRhythm = Quirk == AlienType.Knocker && Index % 3 == 1;
            var at = wrongPlace ? st.TransformPoint(new Vector3(.4f, .1f, 5.2f)) : st.TransformPoint(new Vector3(-1.85f, .1f, .8f));
            LastKnock = Sfx.Play(Sfx.Knock, at, 1f, 6f); Knocks++; knocksLeft--; knockNo++;
            knockGap = wrongRhythm ? (knockNo == 1 ? .12f : .8f) : .34f;
            OnKnock?.Invoke(this, knockNo);
        }

        // Round 3, the Stalker: round the ship at 18 m (about 1.3 turns, off the radar for part of it), then in from behind the tail.
        Vector3 StalkTarget(Transform st, float dt)
        {
            var c = st.position; var rel = body.position - c; rel.y = 0;
            if (Circled == 0 && stalkAngle == 0) stalkAngle = Mathf.Atan2(rel.x, rel.z) * Mathf.Rad2Deg;
            if (rel.magnitude > 22f && Circled == 0) return c + rel.normalized * 18f;   // first out to the circle
            float step = Tuning.T.pilotRunSpeed * .8f * dt / 18f * Mathf.Rad2Deg;
            stalkAngle += step; Circled += step;
            HiddenFromRadar = Circled > 120f && Circled < 260f;
            if (Circled > 420f && st.InverseTransformPoint(body.position).z < -6f) { stalkDone = true; HiddenFromRadar = false; }   // behind the ship: in to the door
            return c + Quaternion.Euler(0, stalkAngle, 0) * Vector3.forward * 18f;
        }

        // The next point on the way to the door (ship space: the door is on the left, x -2.6): a pilot right of the hull first walks
        // out past the nose or the tail, then round the corner to the left side, then to the door.
        Vector3 PathTarget(Transform st, Vector3 door)
        {
            var l = st.InverseTransformPoint(body.position);
            if (l.x > -2.2f && l.x < 7.5f && Mathf.Abs(l.z) < 9.5f) return st.TransformPoint(new Vector3(l.x, 0, l.z > DoorOutside.z ? 9.6f : -9.6f));
            if (l.x > -2.2f) return st.TransformPoint(new Vector3(-4f, 0, l.z > 0 ? 9.6f : -9.6f));
            return door;
        }

        void Pose(float legs, float rightArm)
        {
            if (rig) return;   // Fix 3.1: the rigged model plays clips instead
            legL.localRotation = Quaternion.Euler(legs * 35f, 0, 0); legR.localRotation = Quaternion.Euler(-legs * 35f, 0, 0);
            armL.localRotation = Quaternion.Euler(-legs * 30f, 0, 0);
            armR.localRotation = Quaternion.Euler(rightArm > 1 ? -rightArm * 60f : legs * 30f, 0, 0);
        }

        public bool AtDoor => Now == State.Knocking;
        public Vector3 Position => body.position;
        public Quaternion Facing => body.rotation;
        public bool OnFoot => Now == State.Waiting || Now == State.Running || Now == State.Knocking;

        public void Board() { if (Now != State.Knocking) return; Now = State.Boarding; boardT = 0; boardFrom = body.position; }

        // A real pilot killed by the exhaust: burnt, fallen. An alien: the Campaign shows the creature instead (Hide).
        public void Kill()
        {
            Now = State.Dead;
            if (rig)
            {   // Fix 3.1: a flinch from the blast, then the fall; burnt dark
                rig.PlayThen("hit", "death"); rig.Tint(new Color(.16f, .14f, .12f));
                if (shadow) shadow.gameObject.SetActive(false);
                return;
            }
            body.rotation = body.rotation * Quaternion.Euler(-90, 0, 0); body.position += Vector3.up * .25f;
            foreach (var r in body.GetComponentsInChildren<Renderer>()) { var m = new Material(r.sharedMaterial); m.SetColor("_BaseColor", new Color(.09f, .08f, .07f)); r.sharedMaterial = m; }
            Pose(0, 0);
            if (shadow) shadow.gameObject.SetActive(false);
        }

        public void ForceAboard() { Now = State.Aboard; body.gameObject.SetActive(false); if (shadow) shadow.gameObject.SetActive(false); }   // checks only
        public void Hide() { Now = State.Gone; body.gameObject.SetActive(false); if (shadow) shadow.gameObject.SetActive(false); }
    }
}
