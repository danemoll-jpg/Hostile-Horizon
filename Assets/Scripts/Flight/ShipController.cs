using System;
using UnityEngine;

namespace HostileHorizon
{
    public enum CrashLevel { Ghost, Bounce, Regenerating, Lasting, Instant }
    public enum FlightMode { Flight, Hover }

    // Round 1, items 3 and 6: the flight model and the crash levels. Round 2, items 6 and 8: landing mode and energy.
    // Flight mode (Round 1, unchanged): arcade, the ship always flies where its nose points, at a speed set by the throttle between a
    // minimum and a maximum. Pitch, roll and yaw turn it; banking also turns it; with the stick centred the wings level slowly.
    // Landing mode (the Land button), like a helicopter: the ship brakes to a hover, the stick moves it forward / back and turns it on
    // the spot (so does yaw), the throttle moves it straight up and down (triggers / Shift-Ctrl: hold to climb or sink; the HOTAS
    // lever once moved: centre holds, forward climbs, back sinks), gear down. It settles on fairly flat ground; touching down too
    // fast or on a slope is a hit at the crash level. Landed, the Engines button turns the engines off / on (throttle up also
    // starts them). Back to flight mode once clear of the ground (the minimum speed only applies in flight mode).
    // Energy (Campaign only): drains with flight (more at high throttle) and in hover; nothing with the engines off. At 0 the
    // engines die: the ship can only come down (a slow powerless sink) and can't take off again.
    // Ground contact is tested against the drawn ground (CanyonField.SurfaceHeight); no physics engine.
    // Fix 2.4: quicker, more direct turning (tuning pitchRate / rollRate / yawRate times Settings > Stick sensitivity), the bank turn and
    // wing levelling set by Settings > Flight assist (Full = 0.2.3's, Light, Off); X from flight is an air brake (tuning airBrake: top
    // speed to a hover in about 3 s), and the point where the ship would stop is predicted (StopPoint); hover sinks faster high up and
    // slows to a safe touchdown speed near the ground; Reset position (ResetTo) puts the ship in a steady hover.
    public sealed class ShipController : MonoBehaviour
    {
        public const float MinSpeed = 30f, MaxSpeed = 125f;
        const float Accel = 22f, ThrottleRate = .45f;
        public struct Feel { public float pitch, roll, yaw, bankTurn, level, right; public bool pullOnly; }   // right: Round 5 self-righting factor (0 = none)
        public static readonly Feel Feel023 = new Feel { pitch = 70, roll = 130, yaw = 32, bankTurn = 38, level = 22 };   // 0.2.3's handling (checks compare)
        public Feel? FeelOverride;                           // checks only: fly with another feel
        public Feel CurrentFeel
        {
            get
            {
                if (FeelOverride.HasValue) return FeelOverride.Value;
                var t = Tuning.T; int a = GameSettings.S.flightAssist; float k = Mathf.Clamp(GameSettings.S.sensitivity, 50, 150) / 100f;
                return new Feel { pitch = t.pitchRate * k, roll = t.rollRate * k, yaw = t.yawRate * k,
                                  bankTurn = a == 0 ? t.bankTurnFull : a == 1 ? t.bankTurnLight : 0, level = a == 0 ? t.levelRateFull : 0, pullOnly = a == 1 };   // Fix 2.5: Light has no wing levelling (levelRateLight unused)
            }
        }
        public const float GearDrop = 1.8f;   // gear feet below the ship's origin when down

        public static readonly Vector3[] Feet = { new Vector3(0, -GearDrop, 3.0f), new Vector3(-2.3f, -GearDrop, -2.6f), new Vector3(2.3f, -GearDrop, -2.6f) };

        public CanyonField Field;
        public CrashLevel Level = CrashLevel.Bounce;
        public FlightMode Mode { get; private set; }
        public float Throttle { get; private set; } = .3f;
        public float Speed { get; private set; }
        public Vector3 Velocity { get; private set; }
        public Vector3 CrashVelocity { get; private set; }
        public float Damage { get; private set; }           // 0..100
        public bool Crashed { get; private set; }
        public bool Landed { get; private set; }
        public bool EnginesOn { get; private set; } = true;
        public float Energy = 100;                           // percent
        public bool UsesEnergy;                              // Campaign
        public float LastImpact { get; private set; }       // m/s into the ground of the last hit (for sound / checks)
        public int Hits { get; private set; }
        public float Gear { get; private set; }             // 0 up .. 1 down
        public float Lift { get; private set; }             // hover: the vertical command, -1..1
        public string Notice { get; private set; } = "";   // a short line for the displays ("Climb higher first")
        public event Action<float> OnHit;                    // impact speed
        // Round 5 (B1, B5): turrets and their stumps collide (Enemies); enemy hits damage the hull in every crash level
        public delegate bool SolidHook(Vector3 p, out Vector3 normal, out float depth);
        public static SolidHook ExtraSolid;
        public float LastShotAt { get; private set; } = -99;
        public event Action OnCrash, OnLanded, OnEnginesStarted, OnEnginesStopped, OnOutOfEnergy, OnBrake;
        public bool Braking { get; private set; }            // Fix 2.4: hover entered faster than the hover speed: the air brakes are on
        public float LandedAt { get; private set; } = -99;   // Time.time of the last touchdown (TOUCHDOWN on the displays)
        public bool InputEnabled = true;
        public bool Kinematic;                               // docking, launch, tow: moved by SetPose, not by the pilot
        public int IgnoreButtonsUntil;                       // frame: a press that closed a menu doesn't also act here
        // Fix 2.5: what turned the ship, in degrees, by source (stick, bank turn, wing levelling, wall bounce, hover body easing), summed
        // since the last ResetMoves; the checks list what moved the ship without input.
        // Round 5 (A): + self-righting (only with the stick released) and the level-out button
        public static readonly string[] MoveNames = { "stick", "bank turn (assist)", "wing levelling (assist)", "wall bounce", "hover body easing", "self-righting (stick released)", "level-out button" };
        public readonly float[] Moved = new float[7];
        public bool Righting { get; private set; }           // Round 5 (A): righting itself now (for the displays / checks)
        public bool LevelingOut => levelOut;
        float releasedFor, rightBlend, rollDir = 1, levelOutUntil; bool levelOut;
        public void ResetMoves() { for (int i = 0; i < Moved.Length; i++) Moved[i] = 0; }
        Vector3 knock;                                       // Fix 2.5: a wall hit's push away from the wall (flight mode), dying out
        Quaternion hoverTilt = Quaternion.identity;          // Fix 2.5: hover body lean (pitch / roll); its heading follows the stick at once
        float lastLever = -1, sinceHit = 99, noticeUntil, hoverLever = -1, hoverHeading; bool leverInHover;

        public float Altitude => transform.position.y;
        public float AboveGround => Field == null ? 0 : transform.position.y - Field.SurfaceHeight(transform.position.x, transform.position.z);
        public float Heading => (transform.eulerAngles.y + 360f) % 360f;
        public bool OutOfEnergy => UsesEnergy && Energy <= 0;

        public void Spawn(Vector3 pos, float heading, CrashLevel level) => Spawn(pos, Quaternion.Euler(0, heading, 0), level, .3f, 0);

        public void Spawn(Vector3 pos, Quaternion rot, CrashLevel level, float throttle, float damage)
        {
            transform.SetPositionAndRotation(pos, rot);
            Level = level; Throttle = throttle; Speed = Mathf.Lerp(MinSpeed, MaxSpeed, Throttle); Velocity = transform.forward * Speed;
            Damage = damage; Crashed = false; Hits = 0; sinceHit = 99; Mode = FlightMode.Flight; Landed = false; EnginesOn = true; Gear = 0;
            Kinematic = false; Notice = ""; Braking = false; knock = Vector3.zero; releasedFor = 0; rightBlend = 0; levelOut = false; Righting = false;
            lastLever = Controls.HasLever ? Controls.ThrottleLever : -1;
            gameObject.SetActive(true);
        }

        // Docking, launch, tow, the hangar: the ship is placed directly (its speed is worked out for the displays).
        public void SetPose(Vector3 pos, Quaternion rot, float dt, bool gearDown)
        {
            if (dt > 0) Velocity = (pos - transform.position) / dt;
            Speed = Velocity.magnitude;
            transform.SetPositionAndRotation(pos, rot);
            Gear = Mathf.MoveTowards(Gear, gearDown ? 1 : 0, Time.deltaTime * .8f);
        }

        public void MakeKinematic(bool hoverLook) { Kinematic = true; Landed = false; Mode = hoverLook ? FlightMode.Hover : FlightMode.Flight; TiltFromBody(); knock = Vector3.zero; }

        // Hand control back to the pilot in flight mode at a speed (end of the launch).
        public void ReleaseToFlight(float speed, float throttle)
        {
            Kinematic = false; Mode = FlightMode.Flight; Speed = speed; Throttle = throttle; Velocity = transform.forward * speed; Landed = false;
            EnginesOn = !OutOfEnergy; lastLever = Controls.HasLever ? Controls.ThrottleLever : -1; IgnoreButtonsUntil = Time.frameCount + 2;
        }

        public void SetEnergyAndDamage(float energy, float damage) { Energy = energy; Damage = damage; }

        void Say(string s, float seconds = 2.5f) { Notice = s; noticeUntil = Time.time + seconds; }

        void Update()
        {
            if (Time.time > noticeUntil) Notice = "";
            if (Crashed || Field == null || Time.timeScale == 0 || Kinematic) return;
            float dt = Mathf.Min(Time.deltaTime, 1f / 20f);
            float pitch = 0, roll = 0, yaw = 0, change = 0;
            bool buttons = InputEnabled && Time.frameCount > IgnoreButtonsUntil;
            if (InputEnabled)
            {
                pitch = Controls.Pitch; roll = Controls.Roll; yaw = Controls.Yaw; change = Controls.ThrottleChange;
                if (Mode == FlightMode.Flight) Throttle = Mathf.Clamp01(Throttle + change * ThrottleRate * dt);
                // the HOTAS lever is absolute: it takes over whenever it moves
                if (Controls.HasLever)
                {
                    float lever = Controls.ThrottleLever;
                    if (lastLever < 0 || Mathf.Abs(lever - lastLever) > .01f) { if (lastLever >= 0 && Mode == FlightMode.Flight) Throttle = lever; lastLever = lever; }
                    if (Mode == FlightMode.Hover && Mathf.Abs(lever - hoverLever) > .04f) leverInHover = true;
                }
            }
            if (buttons && Controls.LevelOutPressed && Mode == FlightMode.Flight) { levelOut = true; levelOutUntil = Time.time + 4f; }   // Round 5 (A2)
            if (buttons && Controls.LandPressed) ToggleMode();
            if (buttons && Controls.EnginesPressed)
            {
                if (Landed) { if (EnginesOn) StopEngines(); else StartEngines(); }
                else if (!Controls.FireHeld) Say("Engines can only be switched off on the ground");   // Round 5: in the air A also fires
            }

            if (Mode == FlightMode.Flight) FlyUpdate(pitch, roll, yaw, dt);
            else HoverUpdate(pitch, roll, yaw, change, dt);

            if (UsesEnergy && EnginesOn)
            {
                var t = Tuning.T;
                float drain = Mode == FlightMode.Flight ? t.energyDrainFlightBase + t.energyDrainFlightThrottle * Throttle : t.energyDrainHover;
                Energy = Mathf.Max(0, Energy - drain * dt);
                if (Energy <= 0)
                {
                    EnginesOn = false; OnEnginesStopped?.Invoke();
                    if (Mode == FlightMode.Flight) EnterHover();
                    Say("OUT OF ENERGY", 6f); OnOutOfEnergy?.Invoke();
                }
            }

            sinceHit += dt;
            if (Level != CrashLevel.Ghost && !Landed) Collide();
            if (Level == CrashLevel.Regenerating && sinceHit > 2f) Damage = Mathf.Max(0, Damage - 6f * dt);
        }

        void ToggleMode()
        {
            if (Mode == FlightMode.Flight) { EnterHover(); return; }
            Braking = false;
            if (OutOfEnergy) { Say("No energy: the ship can only come down"); return; }
            if (Landed || AboveGround < 6f) { Say("Climb higher first, then switch to flight"); return; }
            Mode = FlightMode.Flight;
            Speed = Mathf.Max(0, Vector3.Dot(Velocity, transform.forward)); Throttle = Mathf.Max(Throttle, .3f);
            lastLever = Controls.HasLever ? Controls.ThrottleLever : -1;
            // nose level for the pick-up of speed
            var e = transform.eulerAngles; transform.rotation = Quaternion.Euler(0, e.y, 0);
        }

        void TiltFromBody() { var f = Vector3.ProjectOnPlane(transform.forward, Vector3.up); hoverHeading = f.sqrMagnitude > .001f ? Quaternion.LookRotation(f).eulerAngles.y : transform.eulerAngles.y; hoverTilt = Quaternion.Inverse(Quaternion.Euler(0, hoverHeading, 0)) * transform.rotation; }

        void EnterHover()
        {
            levelOut = false; Righting = false;
            Mode = FlightMode.Hover; Velocity = transform.forward * Speed; TiltFromBody(); knock = Vector3.zero;
            hoverLever = Controls.HasLever ? Controls.ThrottleLever : -1; leverInHover = false;
            if (new Vector3(Velocity.x, 0, Velocity.z).magnitude > Tuning.T.hoverMaxSpeed + 5f) { Braking = true; OnBrake?.Invoke(); }
        }

        // Fix 2.4: where the ship would come to a hover if the air brakes went on now (stick centred): the horizontal speed falls at
        // airBrake down to the hover speed, then at hoverBrake. Ground height under that point.
        public Vector3 StopPoint
        {
            get
            {
                var t = Tuning.T; Vector3 h = new Vector3(Velocity.x, 0, Velocity.z); float v = h.magnitude, v1 = Mathf.Min(v, t.hoverMaxSpeed + 1f);
                float d = (v * v - v1 * v1) / (2f * t.airBrake) + v1 * v1 / (2f * t.hoverBrake);
                var p = transform.position + (v > .01f ? h / v * d : Vector3.zero);
                p.y = Field != null ? Field.SurfaceHeight(p.x, p.z) : 0;
                return p;
            }
        }

        // Fix 2.4: Reset position: a steady hover here, engines on (if there is energy), same cabin and energy.
        public void ResetTo(Vector3 pos, float heading)
        {
            transform.SetPositionAndRotation(pos, Quaternion.Euler(0, heading, 0));
            Mode = FlightMode.Hover; Velocity = Vector3.zero; Speed = 0; Landed = false; Kinematic = false; Braking = false; Gear = 1; Lift = 0;
            hoverHeading = heading; hoverTilt = Quaternion.identity; knock = Vector3.zero; hoverLever = Controls.HasLever ? Controls.ThrottleLever : -1; leverInHover = false;
            EnginesOn = !OutOfEnergy; Notice = ""; IgnoreButtonsUntil = Time.frameCount + 2;
        }

        // Fix 2.4: hover sink rate at full down: fast high up, slowing to a safe touchdown speed near the ground.
        public static float SinkRate(float agl)
        {
            var t = Tuning.T;
            if (agl >= t.hoverSinkHighAbove) return t.hoverSinkHigh;
            if (agl >= t.hoverSinkMidAbove) return Mathf.Lerp(t.hoverSinkMid, t.hoverSinkHigh, Mathf.InverseLerp(t.hoverSinkMidAbove, t.hoverSinkHighAbove, agl));
            return Mathf.Lerp(t.hoverSinkLow, t.hoverSinkMid, Mathf.InverseLerp(t.hoverSinkLowBelow, t.hoverSinkMidAbove, agl));
        }

        public void StartEngines()
        {
            if (EnginesOn) return;
            if (OutOfEnergy) { Say("No energy: call the mothership for a tow"); return; }
            EnginesOn = true; OnEnginesStarted?.Invoke();
        }

        public void StopEngines() { if (!EnginesOn) return; EnginesOn = false; OnEnginesStopped?.Invoke(); }

        void FlyUpdate(float pitch, float roll, float yaw, float dt)
        {
            Gear = Mathf.MoveTowards(Gear, 0, dt * .8f);
            var feel = CurrentFeel; float BankTurn = feel.bankTurn, LevelRate = feel.level;
            // Round 5 (A): self-righting. Only once pitch and roll have been centred for rightDelay (Full / Light, Auto-level on), and
            // never while the stick moves: the first stick input stops it. The level-out button rights at once, at a faster rate,
            // and takes pitch / roll from the stick until the ship is level (a press asks for it).
            var tu = Tuning.T;
            bool centred = Mathf.Abs(pitch) < 1e-4f && Mathf.Abs(roll) < 1e-4f;   // any stick output past the deadzones is the player steering
            releasedFor = centred ? releasedFor + dt : 0;
            if (levelOut && Time.time > levelOutUntil) levelOut = false;
            float rk = RightingFactor;
            bool auto = rk > 0 && centred && releasedFor >= tu.rightDelay;
            if (levelOut) { pitch = 0; roll = 0; rk = Mathf.Max(rk, RightingSpeedFactor) * tu.rightLevelOutFactor; }
            Righting = auto || levelOut;
            rightBlend = Righting ? Mathf.MoveTowards(rightBlend, 1, dt / .2f) : 0;   // eases in, never a jolt
            // turn: stick and twist in the ship's own axes, then the bank turn and the wing levelling about the world's up
            var rot = transform.rotation * Quaternion.Euler(-pitch * feel.pitch * dt, yaw * feel.yaw * dt, -roll * feel.roll * dt);
            Moved[0] += Quaternion.Angle(transform.rotation, rot);
            Vector3 fwd = rot * Vector3.forward, right = rot * Vector3.right;
            float bank = Mathf.Asin(Mathf.Clamp(-right.y, -1, 1)) * Mathf.Rad2Deg;               // + = right wing down
            float level = Mathf.Clamp01(1 - Mathf.Abs(fwd.y) * 1.4f);                              // none when diving / climbing steeply
            // Fix 2.5: on Light the bank turn only helps while the stick pulls (stick centred: the ship holds its attitude)
            float bt = Mathf.Sin(bank * Mathf.Deg2Rad) * BankTurn * level * dt * (feel.pullOnly ? Mathf.Clamp01(pitch) : 1f);
            rot = Quaternion.AngleAxis(bt, Vector3.up) * rot; Moved[1] += Mathf.Abs(bt);
            if (Righting)
            {
                float moved = RightStep(ref rot, rk * rightBlend, dt, out bool done);
                Moved[levelOut ? 6 : 5] += moved;
                if (levelOut && done) levelOut = false;
            }
            else if (LevelRate > 0 && Mathf.Abs(roll) < .05f && Mathf.Abs(bank) < 80f)
            { float lv = Mathf.Clamp(bank, -LevelRate * dt, LevelRate * dt) * level; rot = Quaternion.AngleAxis(lv, fwd) * rot; Moved[2] += Mathf.Abs(lv); }
            transform.rotation = rot;

            Speed = Mathf.MoveTowards(Speed, Mathf.Lerp(MinSpeed, MaxSpeed, Throttle), Accel * dt);
            knock *= Mathf.Exp(-3f * dt);
            Velocity = transform.forward * Speed + knock;
            transform.position += Velocity * dt;
        }

        // Round 5 (A): how fast the ship rights itself with the stick released: 0 = not at all (Flight assist Off, or Auto-level off)
        float RightingSpeedFactor { get { var t = Tuning.T; int s = Mathf.Clamp(GameSettings.S.rightingSpeed, 0, 2); return s == 0 ? t.rightSlowFactor : s == 2 ? t.rightFastFactor : 1f; } }
        float RightingFactor
        {
            get
            {
                if (FeelOverride.HasValue) return FeelOverride.Value.right;
                int a = GameSettings.S.flightAssist;
                if (!GameSettings.S.autoLevel || a == 2) return 0;
                return RightingSpeedFactor * (a == 0 ? Tuning.T.rightFullFactor : 1f);
            }
        }

        // One step of righting: roll about the nose toward wings level (the short way round; from upside down it keeps rolling the way
        // it started), then the nose toward the horizon about the world's level axis (a gentle climb close to the ground). The flight
        // path's heading is kept. Returns the degrees turned; done = level.
        float RightStep(ref Quaternion rot, float k, float dt, out bool done)
        {
            var t = Tuning.T; float moved = 0;
            Vector3 fwd = rot * Vector3.forward, up = rot * Vector3.up;
            float rollErr = 0;
            if (Mathf.Abs(fwd.y) < .97f)
            {
                var upRef = Vector3.ProjectOnPlane(Vector3.up, fwd).normalized;
                rollErr = Vector3.SignedAngle(up, upRef, fwd);
                if (Mathf.Abs(rollErr) > 170f) rollErr = Mathf.Abs(rollErr) * rollDir; else if (Mathf.Abs(rollErr) > 2f) rollDir = Mathf.Sign(rollErr);
                float st = Ease(rollErr, t.rightRollRate * k, t.rightGain * Mathf.Max(k, .3f), dt);
                rot = Quaternion.AngleAxis(st, fwd) * rot; moved += Mathf.Abs(st);
            }
            fwd = rot * Vector3.forward; up = rot * Vector3.up;
            var flat = Vector3.ProjectOnPlane(fwd, Vector3.up);
            if (flat.sqrMagnitude < .003f) flat = fwd.y < 0 ? Vector3.ProjectOnPlane(up, Vector3.up) : -Vector3.ProjectOnPlane(up, Vector3.up);
            flat.Normalize();
            float pitchNow = Mathf.Asin(Mathf.Clamp(fwd.y, -1, 1)) * Mathf.Rad2Deg;
            float target = AboveGround < t.rightClimbBelow ? t.rightClimbPitch : 0;
            float pErr = target - pitchNow;
            float ps = Ease(pErr, t.rightPitchRate * k, t.rightGain * Mathf.Max(k, .3f), dt);
            var axis = Vector3.Cross(Vector3.up, flat);
            rot = Quaternion.AngleAxis(-ps, axis) * rot; moved += Mathf.Abs(ps);
            done = Mathf.Abs(rollErr) < 2f && Mathf.Abs(pErr) < 2f;
            return moved;
        }

        // toward 0 error: at most maxRate, slowing in proportion near the end (an exponential finish, no overshoot)
        static float Ease(float err, float maxRate, float gain, float dt)
        {
            float rate = Mathf.Min(maxRate, gain * Mathf.Abs(err));
            return Mathf.Sign(err) * Mathf.Min(Mathf.Abs(err), rate * dt);
        }

        void HoverUpdate(float pitch, float roll, float yaw, float change, float dt)
        {
            var t = Tuning.T;
            Gear = Mathf.MoveTowards(Gear, 1, dt * .8f);
            bool power = EnginesOn && !OutOfEnergy;
            // the vertical command: triggers / keys while held; the lever once it has been moved in this hover (centre = hold)
            float lift = change;
            if (Mathf.Abs(change) < .05f && leverInHover && Controls.HasLever) { float l = (Controls.ThrottleLever - .5f) * 2f; lift = Mathf.Abs(l) < .12f ? 0 : (l - Mathf.Sign(l) * .12f) / .88f; }
            Lift = power ? Mathf.Clamp(lift, -1, 1) : 0;

            if (Landed)
            {
                if (!power && lift > .3f && !OutOfEnergy) StartEngines();
                if (EnginesOn && Lift > .2f) { Landed = false; Velocity = Vector3.up * 1.5f; }
                else { Velocity = Vector3.zero; Speed = 0; return; }
            }

            float turn = power ? Mathf.Clamp(roll + yaw, -1, 1) : Mathf.Clamp(roll + yaw, -1, 1) * .5f;
            hoverHeading += turn * t.hoverTurnRate * dt;   // its own value: the body's rotation eases after it
            float heading = hoverHeading;
            var flatFwd = Quaternion.Euler(0, heading, 0) * Vector3.forward;
            float maxH = power ? t.hoverMaxSpeed : t.hoverMaxSpeed * .5f;
            Vector3 targetH = flatFwd * (-pitch * maxH);
            Vector3 h = new Vector3(Velocity.x, 0, Velocity.z);
            // the air brakes while faster than the hover speed (Fix 2.4), then the hover's own gentler brake
            if (Braking && h.magnitude <= t.hoverMaxSpeed + 1f) Braking = false;
            h = Vector3.MoveTowards(h, targetH, (Braking ? t.airBrake : t.hoverBrake) * dt);
            float vy = Velocity.y;
            // powerless: a steady sink that eases to a safe touchdown speed close to the ground (ground effect)
            float targetV = power ? (Lift >= 0 ? Lift * t.hoverClimbRate : Lift * SinkRate(AboveGround)) : -Mathf.Lerp(t.safeTouchdownSpeed * .7f, t.powerlessSinkRate, Mathf.InverseLerp(8f, 50f, AboveGround));
            vy = Mathf.MoveTowards(vy, targetV, (Braking ? t.airBrake : t.hoverVerticalAccel) * dt);
            Velocity = new Vector3(h.x, vy, h.z);
            Speed = Velocity.magnitude;
            transform.position += Velocity * dt;

            // body attitude: level, leaning a little into the motion (eases out of whatever attitude flight mode left it in)
            float fwdSpeed = Vector3.Dot(h, flatFwd), sideTurn = turn;
            // Fix 2.5: the heading is the stick's at once (0.2.4 eased the whole body after it, so it kept turning ~20° after release);
            // only the lean eases (and the flight attitude the hover started in)
            var wantTilt = Quaternion.Euler(Mathf.Clamp(fwdSpeed / t.hoverMaxSpeed * 7f, -10, 10) * (h.magnitude > t.hoverMaxSpeed * 1.2f ? .3f : 1f), 0, -sideTurn * 5f);
            var before = transform.rotation;
            // Round 5 (A4): from any flight attitude (even upside down) the body rights smoothly: the same ease as before for small
            // leans, but never faster than hoverRightRate (0.4.x swung a full 180 degrees at up to 400 degrees a second)
            float off = Quaternion.Angle(hoverTilt, wantTilt);
            hoverTilt = Quaternion.RotateTowards(hoverTilt, wantTilt, Mathf.Min(off * (1 - Mathf.Exp(-2.2f * dt)), t.hoverRightRate * dt));
            transform.rotation = Quaternion.Euler(0, heading, 0) * hoverTilt;
            Moved[4] += Mathf.Max(0, Quaternion.Angle(before, transform.rotation) - Mathf.Abs(turn * t.hoverTurnRate * dt));
            TouchDown(dt);
        }

        // The gear feet reaching the ground: settle if slow and fairly flat, otherwise a hit at the crash level.
        void TouchDown(float dt)
        {
            if (Level == CrashLevel.Ghost && Gear < .5f) return;
            float deepest = 0;
            foreach (var f in Feet)
            {
                var p = transform.TransformPoint(new Vector3(f.x, f.y * Gear + (1 - Gear) * -.9f, f.z));
                deepest = Mathf.Max(deepest, Field.SurfaceHeight(p.x, p.z) - p.y);
            }
            if (deepest <= 0) return;
            var c = transform.position;
            Field.SurfaceHeight(c.x, c.z, out var n);
            float slope = Vector3.Angle(n, Vector3.up), down = -Velocity.y;
            var t = Tuning.T;
            transform.position += Vector3.up * deepest;
            bool gearOk = Gear > .95f;
            if (down > t.safeTouchdownSpeed || slope > t.maxLandingSlope || !gearOk)
            {
                if (sinceHit > .5f) Hit(Mathf.Max(down, 2f) * 2f);
                if (Crashed) return;
                Velocity = new Vector3(Velocity.x * .5f, Mathf.Max(2.5f, down * .3f), Velocity.z * .5f) + new Vector3(n.x, 0, n.z) * 3f;
                Say(slope > t.maxLandingSlope ? $"Too steep to land ({slope:0}°)" : !gearOk ? "Gear not down" : $"Hard landing ({down:0.0} m/s)");
                return;
            }
            // settled: on the ground, nose kept, the body lying along the ground
            Landed = true; Velocity = Vector3.zero; Speed = 0; Braking = false; LandedAt = Time.time;
            var fwd = Vector3.ProjectOnPlane(transform.forward, n).normalized;
            transform.rotation = Quaternion.LookRotation(fwd, n); TiltFromBody();
            float lowest = float.MaxValue;
            foreach (var f in Feet) { var p = transform.TransformPoint(f); lowest = Mathf.Min(lowest, p.y - Field.SurfaceHeight(p.x, p.z)); }
            transform.position -= Vector3.up * lowest;
            Say(down > 1.5f ? $"Landed ({down:0.0} m/s)" : "Landed", 2f);
            OnLanded?.Invoke();
        }

        void Collide()
        {
            for (int iter = 0; iter < 3; iter++)
            {
                float deepest = 0, solid = 0; Vector3 n = Vector3.up, sn = Vector3.up;
                foreach (var c in ShipLayout.Contacts)
                {
                    var p = transform.TransformPoint(c);
                    float h = Field.SurfaceHeight(p.x, p.z, out var nn);
                    if (h - p.y > deepest) { deepest = h - p.y; n = nn; }
                    // Round 4: tree trunks, sky bridges, snow bridges (CanyonField.SolidPush: from the seed, not from what is drawn)
                    if (Field.SolidPush(p, out var pn, out var pd) && pd > solid) { solid = pd; sn = pn; }
                    if (ExtraSolid != null && ExtraSolid(p, out var en, out var ed) && ed > solid) { solid = ed; sn = en; }   // Round 5: turrets
                }
                if (deepest <= 0 && solid <= 0) return;
                // out along the ground's normal, then the flight path bounced off it
                if (solid > deepest * Mathf.Max(n.y, .2f)) { n = sn; transform.position += n * (solid + .25f); }
                else transform.position += n * (deepest * Mathf.Max(n.y, .2f) + .25f);
                if (iter > 0) continue;
                Vector3 v = Velocity; float into = -Vector3.Dot(v, n);
                if (into <= 0) continue;
                Hit(into);
                if (Crashed) return;
                Vector3 bounced = v + n * into * 1.35f;
                if (Mode == FlightMode.Hover) { Velocity = bounced * .6f; continue; }
                // Fix 2.5: no bounce-back of the nose (0.2.4 mirrored the flight path off the rock, 1.35x, which spun the ship): the nose
                // turns only as far as running along the rock, and a push carries the ship away from it
                Vector3 along = Vector3.ProjectOnPlane(transform.forward, n);
                if (along.sqrMagnitude < .04f) along = Vector3.ProjectOnPlane(transform.forward, n) + n * .2f;
                var turnAway = Quaternion.FromToRotation(transform.forward, along.normalized); Moved[3] += Quaternion.Angle(Quaternion.identity, turnAway);
                transform.rotation = turnAway * transform.rotation;
                knock = n * (into * .5f + 4f);
                Speed = Mathf.Max(MinSpeed, Speed * .85f); Velocity = transform.forward * Speed + knock;
            }
        }

        void Hit(float into)
        {
            LastImpact = into; Hits++; sinceHit = 0;
            if (Level == CrashLevel.Instant) { Crash(); return; }
            if (Level == CrashLevel.Regenerating || Level == CrashLevel.Lasting)
            {
                if (into > 1f) Damage = Mathf.Min(100f, Damage + into * 1.25f);
                if (Damage >= 100f) { Crash(); return; }
            }
            OnHit?.Invoke(into);
        }

        // Round 5 (B5): an enemy bolt: hull damage (repaired at the hangar); 100 % = the ship is lost, as a crash
        public void TakeDamage(float percent)
        {
            if (Crashed) return;
            Damage = Mathf.Min(100f, Damage + percent); sinceHit = 0; LastShotAt = Time.time;
            if (Damage >= 100f) { LastImpact = 0; Crash(); }
        }

        void Crash()
        {
            CrashVelocity = Velocity; Crashed = true; Speed = 0; Velocity = Vector3.zero; EnginesOn = false;
            OnHit?.Invoke(LastImpact);
            OnCrash?.Invoke();
        }
    }
}
