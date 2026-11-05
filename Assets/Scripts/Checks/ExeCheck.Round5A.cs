using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

namespace HostileHorizon
{
    // Round 5 Part A check, in the built exe:  HostileHorizon.exe -exeCheck <dir> -exeCheckOnly round5a
    // Self-righting: from a 60° bank, a 120° bank, upside down and nose-down 45°, the stick released: time to level and height lost
    // (Light and Full); the level-out button with the stick held; Fix 2.5's uncommanded-movement list (nothing may move the ship while
    // the stick is held but the stick and Light's pull-only bank turn); landing mode from upside down; a low canyon run with releases
    // between turns; cockpit pictures before / after a recovery.
    public sealed partial class ExeCheck
    {
        static float RollOf(Transform t)
        {
            var f = t.forward; if (Mathf.Abs(f.y) > .97f) return 0;
            return Vector3.SignedAngle(t.up, Vector3.ProjectOnPlane(Vector3.up, f).normalized, f);
        }

        IEnumerator Recover(string name, Quaternion attitude, int assist, bool levelButton = false, Vector2 hold = default, string shot = null, float agl = 0)
        {
            var s = G.Ship; var t = s.transform; GameSettings.S.flightAssist = assist;
            PlaceHigh(0, .6f);
            if (agl > 0) { var q = t.position; q.y = s.Field.SurfaceHeight(q.x, q.z) + agl; t.position = q; }
            t.rotation = attitude; G.Rig.Snap(); yield return null;
            if (shot != null) yield return Shot(shot + "_before");
            t.rotation = attitude;   // the picture took a frame or two
            s.ResetMoves(); float y0 = t.position.y, yMin = y0, tt = 0, maxRate = 0, h0 = s.Heading, hFlat0 = Mathf.Atan2(s.Velocity.x, s.Velocity.z) * Mathf.Rad2Deg;
            float r0 = RollOf(t), p0 = PitchOf(t); Quaternion last = t.rotation; bool level = false; float startedAt = -1;
            if (levelButton) state = state.WithButton(GamepadButton.LeftStick, true);
            state.leftStick = hold; Set();
            int frames = 0;
            while (tt < 8f)
            {
                if (levelButton && ++frames == 3) state = state.WithButton(GamepadButton.LeftStick, false);
                Set(); yield return null;
                float dt = ShipDt; tt += dt;
                float a = Quaternion.Angle(last, t.rotation); last = t.rotation; if (dt > 0) maxRate = Mathf.Max(maxRate, a / dt);
                if (startedAt < 0 && a > .01f) startedAt = tt;
                yMin = Mathf.Min(yMin, t.position.y);
                if (Mathf.Abs(RollOf(t)) < 3f && Mathf.Abs(PitchOf(t)) < 3f && t.up.y > .9f) { level = true; break; }
            }
            state.leftStick = Vector2.zero; Set();
            string by = ""; for (int i = 0; i < s.Moved.Length; i++) if (s.Moved[i] > .05f) by += (by.Length > 0 ? ", " : "") + $"{ShipController.MoveNames[i]} {s.Moved[i]:0}°";
            Log($"  {name} (roll {r0:+0;-0}°, pitch {p0:+0;-0}°), {(levelButton ? "level-out button" + (hold != Vector2.zero ? $", stick held ({hold.x:0.0}, {hold.y:0.0})" : "") : "stick released")}: " +
                (level ? $"level after {tt:0.00} s" : $"NOT LEVEL after {tt:0.0} s (roll {RollOf(t):0}°, pitch {PitchOf(t):0}°)") +
                $" (righting began after {startedAt:0.00} s), height lost {y0 - yMin:0} m, heading {Mathf.DeltaAngle(h0, s.Heading):+0;-0}°, fastest turn {maxRate:0} °/s; turned by: {(by.Length > 0 ? by : "nothing")}");
            if (shot != null) { state.leftStick = Vector2.zero; Set(); yield return Shot(shot + "_after"); }
        }

        IEnumerator HoverFromInverted()
        {
            var s = G.Ship; var t = s.transform; GameSettings.S.flightAssist = 1;
            PlaceHigh(0, .6f); t.rotation = Quaternion.Euler(0, 0, 180); yield return null;
            yield return Press(GamepadButton.West);   // landing mode
            float tt = 0, maxRate = 0; Quaternion last = t.rotation; bool up = false;
            while (tt < 6f) { Set(); yield return null; float dt = ShipDt; tt += dt; float a = Quaternion.Angle(last, t.rotation); last = t.rotation; if (dt > 0) maxRate = Mathf.Max(maxRate, a / dt); if (t.up.y > .995f) { up = true; break; } }
            Log($"  landing mode from upside down: {s.Mode}, {(up ? $"upright (within 6°) after {tt:0.00} s" : $"NOT UPRIGHT after {tt:0.0} s (up.y {t.up.y:0.00})")}, fastest body turn {maxRate:0} °/s (cap {Tuning.T.hoverRightRate}), heading kept {Mathf.Abs(Mathf.DeltaAngle(0, s.Heading)) < 3f}");
            yield return Press(GamepadButton.West);
        }

        // Low through the canyon (the check's autopilot), letting go of the stick between turns: wall touches counted
        IEnumerator CanyonReleases(string assistName, int assist, float seconds, bool autoLevel = true)
        {
            var s = G.Ship; GameSettings.S.flightAssist = assist; GameSettings.S.autoLevel = autoLevel;
            int hits = s.Hits; float end = Time.realtimeSinceStartup + seconds, minAgl = 999, aglSum = 0; int n = 0, releases = 0; float released = 0;
            s.ResetMoves();
            while (Time.realtimeSinceStartup < end && G.Now == GameShell.State.Flying)
            {
                float leg = Time.realtimeSinceStartup + 2.5f;
                while (Time.realtimeSinceStartup < leg && G.Now == GameShell.State.Flying) { Autopilot(); yield return null; minAgl = Mathf.Min(minAgl, s.AboveGround); aglSum += s.AboveGround; n++; }
                state.leftStick = Vector2.zero; Set(); releases++;
                float rel = Time.realtimeSinceStartup + 1.2f;
                while (Time.realtimeSinceStartup < rel && G.Now == GameShell.State.Flying) { Set(); yield return null; released += Time.unscaledDeltaTime; minAgl = Mathf.Min(minAgl, s.AboveGround); aglSum += s.AboveGround; n++; }
            }
            GameSettings.S.autoLevel = true;
            Log($"  canyon run, {assistName}: {seconds:0} s, {releases} releases of 1.2 s between 2.5 s legs: wall / ground touches {s.Hits - hits}, lowest {minAgl:0} m, mean {aglSum / Mathf.Max(1, n):0} m over the ground; self-righting turned {s.Moved[5]:0}° in total");
        }

        IEnumerator Round5A()
        {
            StartCoroutine(Progress());
            yield return WaitFor(() => G.Now == GameShell.State.Title, 60f);
            yield return new WaitForSecondsRealtime(1.5f);
            var tu = Tuning.T;
            Log($"Round 5 A tuning: delay {tu.rightDelay} s, roll {tu.rightRollRate} / pitch {tu.rightPitchRate} °/s at most, ease {tu.rightGain}/s, Slow x{tu.rightSlowFactor} / Fast x{tu.rightFastFactor}, Full x{tu.rightFullFactor}, level-out x{tu.rightLevelOutFactor}, climb {tu.rightClimbPitch}° below {tu.rightClimbBelow} m; settings: Auto-level {(GameSettings.S.autoLevel ? "On" : "Off")}, Righting speed {GameSettings.RightingNames[GameSettings.S.rightingSpeed]}");
            yield return StartFromTitle(0);   // Ghost, high up
            yield return Fly(1f);
            foreach (var (an, a) in new[] { ("Light (default)", 1), ("Full", 0) })
            {
                Log($"recovery, Flight assist {an}, Righting speed Normal:");
                yield return Recover("60° bank", Quaternion.Euler(0, 0, -60), a);
                yield return Recover("120° bank", Quaternion.Euler(0, 0, -120), a);
                yield return Recover("upside down", Quaternion.Euler(0, 0, 180), a, false, default, a == 1 ? "r5a_inverted" : null);
                yield return Recover("nose-down 45°", Quaternion.Euler(45, 0, 0), a);
                yield return Recover("upside down, nose-down 30°", Quaternion.Euler(30, 0, 180), a);
            }
            Log("pictures 300 m over the ground (Light):");
            yield return Recover("upside down, 300 m up", Quaternion.Euler(0, 0, 180), 1, false, default, "r5a_low_inverted", 300f);
            yield return Recover("120° bank, 300 m up", Quaternion.Euler(10, 0, -120), 1, false, default, "r5a_low_bank120", 300f);
            GameSettings.S.rightingSpeed = 0; Log("Righting speed Slow (Light):"); yield return Recover("upside down", Quaternion.Euler(0, 0, 180), 1);
            GameSettings.S.rightingSpeed = 2; Log("Righting speed Fast (Light):"); yield return Recover("upside down", Quaternion.Euler(0, 0, 180), 1);
            GameSettings.S.rightingSpeed = 1;
            Log("Flight assist Off (no self-righting):"); yield return Recover("60° bank", Quaternion.Euler(0, 0, -60), 2);
            GameSettings.S.autoLevel = false; Log("Auto-level Off (Light):"); yield return Recover("60° bank", Quaternion.Euler(0, 0, -60), 1); GameSettings.S.autoLevel = true;
            Log("level-out button:");
            yield return Recover("upside down", Quaternion.Euler(0, 0, 180), 2, true);
            yield return Recover("120° bank", Quaternion.Euler(0, 0, -120), 1, true, new Vector2(.6f, .4f));
            yield return Recover("upside down, nose-down 30°", Quaternion.Euler(30, 0, 180), 1, true, new Vector2(0, 1f));

            // Fix 2.5's list: what turns the ship while the stick is held must stay the stick (and Light's pull-only bank turn)
            yield return Uncommanded("Light (default)", 1);
            yield return Uncommanded("Full", 0);
            yield return Uncommanded("Off", 2);
            Log("landing mode:");
            yield return HoverFromInverted();
            yield return HoverRelease();

            yield return PauseToTitle(); yield return StartFromTitle(1);   // Bounce, in the canyon
            yield return Fly(2f);
            yield return CanyonReleases("Light (default)", 1, 50f);
            yield return PauseToTitle(); yield return StartFromTitle(1);
            yield return Fly(2f);
            yield return CanyonReleases("Light, baseline: Auto-level Off", 1, 50f, false);
            yield return PauseToTitle(); yield return StartFromTitle(1);
            yield return Fly(2f);
            yield return CanyonReleases("Full", 0, 50f);
            GameSettings.S.flightAssist = 0;
        }
    }
}
