using System;
using UnityEngine;

namespace HostileHorizon
{
    public enum CrashLevel { Ghost, Bounce, Regenerating, Lasting, Instant }

    // Round 1, items 3 and 6: the flight model and the crash levels.
    // Arcade, in the spirit of the original: the ship always flies where its nose points (no sliding), at a speed set by the
    // throttle between a minimum (it cannot hover or stall: landing comes in Round 2) and a maximum. Pitch, roll and yaw turn it;
    // banking also turns it (so a stick-only player can fly), and with the stick centred the wings level themselves slowly, which
    // makes low canyon flying easy. Ground contact is tested against the drawn ground (CanyonField.SurfaceHeight) at the ship's
    // contact points, so there is no physics engine involved.
    // Crash levels: Ghost (pass through), Bounce (bounce off, no damage), Regenerating (bounce, damage that heals), Lasting (bounce,
    // damage adds up), Instant (any hit ends the flight). Damage 100 % ends the flight.
    public sealed class ShipController : MonoBehaviour
    {
        public const float MinSpeed = 30f, MaxSpeed = 125f;
        const float PitchRate = 70f, RollRate = 130f, YawRate = 32f, BankTurn = 38f, LevelRate = 22f, Accel = 22f, ThrottleRate = .45f;

        public CanyonField Field;
        public CrashLevel Level = CrashLevel.Bounce;
        public float Throttle { get; private set; } = .3f;
        public float Speed { get; private set; }
        public float Damage { get; private set; }           // 0..100
        public bool Crashed { get; private set; }
        public float LastImpact { get; private set; }       // m/s into the ground of the last hit (for sound / checks)
        public int Hits { get; private set; }
        public event Action<float> OnHit;                    // impact speed
        public event Action OnCrash;
        public bool InputEnabled = true;
        float lastLever = -1, sinceHit = 99;

        public float Altitude => transform.position.y;
        public float AboveGround => Field == null ? 0 : transform.position.y - Field.SurfaceHeight(transform.position.x, transform.position.z);
        public float Heading => (transform.eulerAngles.y + 360f) % 360f;

        public void Spawn(Vector3 pos, float heading, CrashLevel level)
        {
            transform.SetPositionAndRotation(pos, Quaternion.Euler(0, heading, 0));
            Level = level; Throttle = .3f; Speed = Mathf.Lerp(MinSpeed, MaxSpeed, Throttle); Damage = 0; Crashed = false; Hits = 0; sinceHit = 99;
            lastLever = Controls.HasLever ? Controls.ThrottleLever : -1;
            gameObject.SetActive(true);
        }

        void Update()
        {
            if (Crashed || Field == null || Time.timeScale == 0) return;
            float dt = Mathf.Min(Time.deltaTime, 1f / 20f);
            float pitch = 0, roll = 0, yaw = 0;
            if (InputEnabled)
            {
                pitch = Controls.Pitch; roll = Controls.Roll; yaw = Controls.Yaw;
                Throttle = Mathf.Clamp01(Throttle + Controls.ThrottleChange * ThrottleRate * dt);
                // the HOTAS lever is absolute: it takes over whenever it moves
                if (Controls.HasLever)
                {
                    float lever = Controls.ThrottleLever;
                    if (lastLever < 0 || Mathf.Abs(lever - lastLever) > .01f) { if (lastLever >= 0) Throttle = lever; lastLever = lever; }
                }
            }

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
            transform.position += transform.forward * Speed * dt;

            sinceHit += dt;
            if (Level != CrashLevel.Ghost) Collide();
            if (Level == CrashLevel.Regenerating && sinceHit > 2f) Damage = Mathf.Max(0, Damage - 6f * dt);
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
                Vector3 v = transform.forward * Speed; float into = -Vector3.Dot(v, n);
                if (into <= 0) continue;
                Hit(into);
                if (Crashed) return;
                Vector3 bounced = v + n * into * 1.35f;
                if (bounced.sqrMagnitude < .01f) bounced = Vector3.ProjectOnPlane(transform.forward, n) + n * .3f;
                transform.rotation = Quaternion.FromToRotation(transform.forward, bounced.normalized) * transform.rotation;
                Speed = Mathf.Max(MinSpeed, Speed * .85f);
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
            Crashed = true; Speed = 0;
            OnHit?.Invoke(LastImpact);
            OnCrash?.Invoke();
        }
    }
}
