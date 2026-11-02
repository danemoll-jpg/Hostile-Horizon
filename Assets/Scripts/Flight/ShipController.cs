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
    public sealed class ShipController : MonoBehaviour
    {
        public const float MinSpeed = 30f, MaxSpeed = 125f;
        const float PitchRate = 70f, RollRate = 130f, YawRate = 32f, BankTurn = 38f, LevelRate = 22f, Accel = 22f, ThrottleRate = .45f;
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
        public event Action OnCrash, OnLanded, OnEnginesStarted, OnEnginesStopped, OnOutOfEnergy;
        public bool InputEnabled = true;
        public bool Kinematic;                               // docking, launch, tow: moved by SetPose, not by the pilot
        public int IgnoreButtonsUntil;                       // frame: a press that closed a menu doesn't also act here
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
            Kinematic = false; Notice = "";
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

        public void MakeKinematic(bool hoverLook) { Kinematic = true; Landed = false; Mode = hoverLook ? FlightMode.Hover : FlightMode.Flight; hoverHeading = transform.eulerAngles.y; }

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
            if (buttons && Controls.LandPressed) ToggleMode();
            if (buttons && Controls.EnginesPressed)
            {
                if (Landed) { if (EnginesOn) StopEngines(); else StartEngines(); }
                else Say("Engines can only be switched off on the ground");
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
            if (OutOfEnergy) { Say("No energy: the ship can only come down"); return; }
            if (Landed || AboveGround < 6f) { Say("Climb higher first, then switch to flight"); return; }
            Mode = FlightMode.Flight;
            Speed = Mathf.Max(0, Vector3.Dot(Velocity, transform.forward)); Throttle = Mathf.Max(Throttle, .3f);
            lastLever = Controls.HasLever ? Controls.ThrottleLever : -1;
            // nose level for the pick-up of speed
            var e = transform.eulerAngles; transform.rotation = Quaternion.Euler(0, e.y, 0);
        }

        void EnterHover()
        {
            Mode = FlightMode.Hover; Velocity = transform.forward * Speed; hoverHeading = transform.eulerAngles.y;
            hoverLever = Controls.HasLever ? Controls.ThrottleLever : -1; leverInHover = false;
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
            // turn: stick and twist in the ship's own axes, then the bank turn and the wing levelling about the world's up
            var rot = transform.rotation * Quaternion.Euler(-pitch * PitchRate * dt, yaw * YawRate * dt, -roll * RollRate * dt);
            Vector3 fwd = rot * Vector3.forward, right = rot * Vector3.right;
            float bank = Mathf.Asin(Mathf.Clamp(-right.y, -1, 1)) * Mathf.Rad2Deg;               // + = right wing down
            float level = Mathf.Clamp01(1 - Mathf.Abs(fwd.y) * 1.4f);                              // none when diving / climbing steeply
            rot = Quaternion.AngleAxis(Mathf.Sin(bank * Mathf.Deg2Rad) * BankTurn * level * dt, Vector3.up) * rot;
            if (Mathf.Abs(roll) < .05f && Mathf.Abs(bank) < 80f)
                rot = Quaternion.AngleAxis(Mathf.Clamp(bank, -LevelRate * dt, LevelRate * dt) * level, fwd) * rot;
            transform.rotation = rot;

            Speed = Mathf.MoveTowards(Speed, Mathf.Lerp(MinSpeed, MaxSpeed, Throttle), Accel * dt);
            Velocity = transform.forward * Speed;
            transform.position += Velocity * dt;
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
            h = Vector3.MoveTowards(h, targetH, t.hoverBrake * dt);
            float vy = Velocity.y;
            // powerless: a steady sink that eases to a safe touchdown speed close to the ground (ground effect)
            float targetV = power ? Lift * t.hoverClimbRate : -Mathf.Lerp(t.safeTouchdownSpeed * .7f, t.powerlessSinkRate, Mathf.InverseLerp(8f, 50f, AboveGround));
            vy = Mathf.MoveTowards(vy, targetV, 10f * dt);
            Velocity = new Vector3(h.x, vy, h.z);
            Speed = Velocity.magnitude;
            transform.position += Velocity * dt;

            // body attitude: level, leaning a little into the motion (eases out of whatever attitude flight mode left it in)
            float fwdSpeed = Vector3.Dot(h, flatFwd), sideTurn = turn;
            var want = Quaternion.Euler(Mathf.Clamp(fwdSpeed / t.hoverMaxSpeed * 7f, -10, 10) * (h.magnitude > t.hoverMaxSpeed * 1.2f ? .3f : 1f), heading, -sideTurn * 5f);
            transform.rotation = Quaternion.Slerp(transform.rotation, want, 1 - Mathf.Exp(-2.2f * dt));
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
            Landed = true; Velocity = Vector3.zero; Speed = 0;
            var fwd = Vector3.ProjectOnPlane(transform.forward, n).normalized;
            transform.rotation = Quaternion.LookRotation(fwd, n);
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
                float deepest = 0; Vector3 n = Vector3.up;
                foreach (var c in ShipLayout.Contacts)
                {
                    var p = transform.TransformPoint(c);
                    float h = Field.SurfaceHeight(p.x, p.z, out var nn);
                    if (h - p.y > deepest) { deepest = h - p.y; n = nn; }
                }
                if (deepest <= 0) return;
                // out along the ground's normal, then the flight path bounced off it
                transform.position += n * (deepest * Mathf.Max(n.y, .2f) + .25f);
                if (iter > 0) continue;
                Vector3 v = Velocity; float into = -Vector3.Dot(v, n);
                if (into <= 0) continue;
                Hit(into);
                if (Crashed) return;
                Vector3 bounced = v + n * into * 1.35f;
                if (Mode == FlightMode.Hover) { Velocity = bounced * .6f; continue; }
                if (bounced.sqrMagnitude < .01f) bounced = Vector3.ProjectOnPlane(transform.forward, n) + n * .3f;
                transform.rotation = Quaternion.FromToRotation(transform.forward, bounced.normalized) * transform.rotation;
                Speed = Mathf.Max(MinSpeed, Speed * .85f); Velocity = transform.forward * Speed;
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

        void Crash()
        {
            CrashVelocity = Velocity; Crashed = true; Speed = 0; Velocity = Vector3.zero; EnginesOn = false;
            OnHit?.Invoke(LastImpact);
            OnCrash?.Invoke();
        }
    }
}
