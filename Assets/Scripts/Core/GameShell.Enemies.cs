using System.Collections.Generic;
using UnityEngine;

namespace HostileHorizon
{
    // Round 5, Part B: the Campaign's fighting, wired into the flow. Enemies (Campaign/Enemies.cs) are placed when the mothership
    // arrives over a planet (from its seed and the difficulty; ones you destroyed stay destroyed), and act only while you fly.
    //  B3 the gun: held Fire (A / Space / left mouse / HOTAS trigger), gunRate shots a second, gunEnergy % each, from the nose;
    //     a reticle in the world 60 m ahead (seen from the cockpit, the same in VR).
    //  B4 the shield: Shield toggles it (D-pad left / Z / HOTAS 2); blocks bolts, drains shieldDrain % a second; turned on with
    //     something at the hull it burns it, as the engines do (a real pilot at the door dies too).
    //  B5 hits: hull damage (the hangar repairs it), a shake, sparks, a hit sound, "HIT -7%"; hull at 0 is death as before.
    //  B6 warnings: incoming-fire marks (HUD ring, radar rim), LOCKED ON, the computer's calls, enemies on the radar once spotted,
    //     Control's "hot planet" call at launch.
    //  B8 the difficulty page (Campaign page for a new career, the hangar for this one).
    public sealed partial class GameShell
    {
        public Enemies Foes { get; private set; }
        public bool ShieldOn { get; private set; }
        public string LastKiller { get; private set; } = "";   // checks / the death page: what shot you down
        float gunCooldown, lockSaidAt = -99, saucerSaidAt = -99, shieldFlash; bool gunArmed; int gunSide = 1;
        AudioSource shieldHum; Renderer shieldBubble; Material shieldMat; LineRenderer reticle;
        readonly List<Vector3> sitePads = new List<Vector3>();
        MenuPanel.Page difficultyPage; MenuPanel.Page difficultyReturn;
        public readonly List<string> FightLog = new List<string>();   // checks: what happened, in order

        void CreateEnemies()
        {
            Foes = Enemies.Create(litTemplate);
            Foes.Destroyed += EnemyDestroyed;
            Foes.LockedOn += e =>
            {
                FightLog.Add($"{Time.time:0.0} {Name(e)} locked on");
                if (Time.time - lockSaidAt > 12f) { lockSaidAt = Time.time; Voice.Say("cpu_locked_on", VoiceKind.Computer, 3, maxAge: 2f); }
                Sfx.Play(Sfx.Clip("lock_warning"), Ship.transform.TransformPoint(ShipLayout.DashCentre), .55f, 2f, Ship.transform);
            };
            Foes.SaucerSpotted += e =>
            {
                FightLog.Add($"{Time.time:0.0} {Name(e)} chasing");
                if (Time.time - saucerSaidAt > 20f) { saucerSaidAt = Time.time; Voice.Say("cpu_saucer", VoiceKind.Computer, 3, maxAge: 3f); }
            };
            Foes.HitShip += ShipShot;
            // the shield bubble: a faint blue shell round the ship, brighter for a moment where a bolt hits
            var b = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(b.GetComponent<Collider>()); b.name = "Shield";
            b.transform.SetParent(Ship.transform, false); b.transform.localPosition = new Vector3(0, .3f, -.5f); b.transform.localScale = new Vector3(13f, 7f, 17f);
            shieldMat = new Material(Shader.Find("HostileHorizon/GlowAdd")); shieldBubble = b.GetComponent<Renderer>(); shieldBubble.sharedMaterial = shieldMat;
            shieldBubble.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; shieldBubble.receiveShadows = false; b.SetActive(false);
            // the reticle: a ring and a centre cross 60 m ahead of the pilot's eye, on the gun line
            reticle = new GameObject("Reticle").AddComponent<LineRenderer>(); reticle.transform.SetParent(Ship.transform, false);
            reticle.useWorldSpace = false; reticle.loop = false; reticle.widthMultiplier = .1f; reticle.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; reticle.receiveShadows = false;
            var rm = new Material(Shader.Find("HostileHorizon/GlowAdd")); rm.SetColor("_Color", new Color(.35f, 1f, .7f) * .9f); reticle.sharedMaterial = rm;
            BuildReticle(0); reticle.gameObject.SetActive(false);
            BuildDifficultyPage();
            Brackets = TargetBrackets.Create();   // Round 6 (B2)
        }

        // Round 6 (B1, B2): the enemies' radar reach and the brackets (the Radar upgrade, Part C, multiplies the reach)
        public TargetBrackets Brackets { get; private set; }
        public float RadarTurretRange => Tuning.T.radarTurretRange * RadarFactor;
        public float RadarSaucerRange => Tuning.T.radarSaucerRange * RadarFactor;

        // the reticle's line; Round 6 (C3): an outer ring a Gun upgrade level
        void BuildReticle(int gunLevel)
        {
            var pts = new List<Vector3>(); var c = ShipLayout.Eye + Vector3.forward * 60f;
            for (int i = 0; i <= 24; i++) { float a = i * Mathf.PI * 2 / 24; pts.Add(c + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0) * 1.4f); }
            foreach (var d in new[] { Vector3.up, Vector3.right, Vector3.down, Vector3.left }) { pts.Add(c + d * 1.4f); pts.Add(c + d * .5f); pts.Add(c + d * 1.4f); }
            for (int r = 1; r <= gunLevel; r++) { pts.Add(c + Vector3.right * 1.4f); for (int i = 0; i <= 24; i++) { float a = i * Mathf.PI * 2 / 24; pts.Add(c + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0) * (1.4f + .45f * r)); } pts.Add(c + Vector3.right * 1.4f); }
            reticle.positionCount = pts.Count; reticle.SetPositions(pts.ToArray());
        }

        static string Name(Enemy e) => (e.Kind == EnemyKind.Turret ? "turret " : "saucer ") + (e.Index + 1);

        // when the mothership arrives over a planet (Arrive): its enemies, from the seed and the difficulty
        void SetupEnemies()
        {
            Terrain.Field.FindStart(out var start, out _);
            bool first = Career.C.planets.IndexOf(Planet) == 0;
            Foes.Setup(Terrain.Field, Ship, Planet.seed, Planet.tier, first, sitePads, start);
            // the ones you destroyed stay destroyed (unless the difficulty's enemies changed since: then they are other enemies)
            string key = $"{Difficulty.Current.enemies}/{Difficulty.Current.aggression}";
            if (Planet.enemiesKey != key) { Planet.enemiesKey = key; Planet.enemiesDown.Clear(); }
            foreach (var e in Foes.All) if (Planet.enemiesDown.Contains(Id(e))) { e.Dead = true; e.Root.gameObject.SetActive(false); }
            FightLog.Clear(); foreach (var l in Foes.Placement) FightLog.Add("placed: " + l);
            Planet.turrets = Foes.All.FindAll(e => e.Kind == EnemyKind.Turret).Count; Planet.saucers = Foes.All.FindAll(e => e.Kind == EnemyKind.Saucer).Count;   // Round 6 (D1)
        }

        static int Id(Enemy e) => e.Kind == EnemyKind.Turret ? e.Index : 1000 + e.Index;
        int Alive(EnemyKind k) { int n = 0; foreach (var e in Foes.All) if (!e.Dead && e.Kind == k) n++; return n; }

        // every frame (GameShell.Update): the fight runs only while flying in the Campaign
        void FightFrame()
        {
            if (!Foes) return;
            bool flying = InCampaign && Now == State.Flying;
            Foes.Running = flying;
            float dt = Time.deltaTime; var t = Tuning.T;
            bool canFight = flying && !Ship.Landed && !Ship.Kinematic && !Ship.Crashed && Ship.InputEnabled && CanopyAlien == null;
            if (!Controls.FireHeld) gunArmed = true;
            gunCooldown -= dt;
            if (canFight && gunArmed && Controls.FireHeld && Time.frameCount > Ship.IgnoreButtonsUntil && gunCooldown <= 0 && Ship.Energy > t.gunEnergy)
            {
                gunCooldown = 1f / GunRateNow; gunSide = -gunSide;   // Round 6 (C): the Gun upgrade
                var from = Ship.transform.TransformPoint(new Vector3(gunSide * 1.3f, -.5f, 6f));
                Foes.PlayerShot(from, Ship.transform.forward, Ship.Velocity, GunDamageNow);
                Ship.Energy = Mathf.Max(0, Ship.Energy - t.gunEnergy);
                Sfx.Play(Sfx.Clip("gun_fire"), from, .55f, 6f, Ship.transform);
            }
            if (flying && Ship.InputEnabled && Controls.ShieldPressed && Time.frameCount > Ship.IgnoreButtonsUntil) SetShield(!ShieldOn);
            if (ShieldOn)
            {
                Ship.Energy = Mathf.Max(0, Ship.Energy - ShieldDrainNow * dt);   // Round 6 (C): the Shields upgrade
                if (Ship.Energy <= 0 || !InCampaign || Ship.Crashed) SetShield(false);
            }
            shieldFlash = Mathf.Max(0, shieldFlash - dt * 3f);
            if (shieldBubble && ShieldOn) shieldMat.SetColor("_Color", new Color(.05f, .11f, .2f) * (1 + .35f * Lvl(Upg.Shields)) + new Color(.12f, .3f, .5f) * shieldFlash);   // Round 6 (C3): a stronger shell
            if (shieldBubble) shieldBubble.transform.localScale = new Vector3(13f, 7f, 17f) * (Foes.ShieldRadius / 8f);
            if (reticle) reticle.gameObject.SetActive(flying && !Ship.Landed && !Ship.Kinematic && !Ship.Crashed && Ship.Mode == FlightMode.Flight);
            Brackets.Tick(flying && !Ship.Crashed && Ship.gameObject.activeInHierarchy, Foes, Rig.Head.transform, Ship.transform.position, t.bracketRange);
            // the displays
            Displays.Shield = ShieldOn; Displays.LockedOn = flying && Foes.AnyLock; Displays.Threats = Foes.Threats;
            Displays.FightLine = InCampaign && Foes.All.Count > 0 ? $"THREATS {Alive(EnemyKind.Turret)} turrets{(Alive(EnemyKind.Saucer) > 0 || Foes.All.Exists(e => e.Kind == EnemyKind.Saucer) ? $" · {Alive(EnemyKind.Saucer)} saucers" : "")}" : "";
        }

        public void SetShield(bool on)
        {
            if (on == ShieldOn) return;
            if (on && (Ship.Energy <= 1f || !InCampaign)) { Flash("Not enough energy for the shield", 2.5f); return; }
            ShieldOn = on; Foes.ShieldOn = on; shieldBubble.gameObject.SetActive(on);
            if (on) { shieldHum = Sfx.Loop(Sfx.Clip("shield_hum"), Ship.transform, .35f, 4f); Sfx.Play(Sfx.Clip("shield_on"), Ship.transform.position, .8f, 6f, Ship.transform); }
            else { if (shieldHum) Destroy(shieldHum.gameObject); Sfx.Play(Sfx.Clip("shield_off"), Ship.transform.position, .7f, 6f, Ship.transform); }
            if (InCampaign && Now == State.Flying) Voice.Say(on ? "cpu_shield_up" : "cpu_shield_down", VoiceKind.Computer, 3, maxAge: 2f);
            FightLog.Add($"{Time.time:0.0} shield {(on ? "on" : "off")} (energy {Ship.Energy:0.0} %)");
            if (on && InCampaign) HullBurn(true);   // B4: the original's answer to what is at the hull, as the engines
        }

        void ShipShot(Enemy by, float dmg, bool blocked, Vector3 at)
        {
            if (blocked) { shieldFlash = 1; Sfx.Play(Sfx.Clip("bolt_hit_shield"), at, .9f, 10f); FightLog.Add($"{Time.time:0.0} shield blocked a bolt from {(by != null ? Name(by) : "?")}"); return; }
            Ship.TakeDamage(dmg); Rig.Shake(.7f); Displays.NoteShot(dmg);
            Sfx.Play(Sfx.Clip("bolt_hit_hull"), at, 1f, 10f);
            LastKiller = by != null ? (by.Kind == EnemyKind.Turret ? "a turret" : "a saucer") : "enemy fire";
            FightLog.Add($"{Time.time:0.0} hit by {(by != null ? Name(by) : "?")}: -{dmg:0} % (hull {100 - Ship.Damage:0} %)");
        }

        void EnemyDestroyed(Enemy e)
        {
            var c = Career.C; var t = Tuning.T; bool tur = e.Kind == EnemyKind.Turret;
            int b = Mathf.RoundToInt((tur ? t.turretBounty : t.saucerBounty) * Difficulty.Pay);
            c.Earn(b); if (tur) c.stats.turretsDestroyed++; else c.stats.saucersDestroyed++;
            Planet.enemiesDown.Add(Id(e));
            Flash($"{(tur ? "Turret" : "Saucer")} destroyed. Bounty +{b} cr", 4f);
            Voice.Say("cpu_target_down", VoiceKind.Computer, 3, maxAge: 2f);
            FightLog.Add($"{Time.time:0.0} {Name(e)} destroyed: bounty +{b} cr");
        }

        // B6: Control warns at launch when the planet fights back
        void SayHot()
        {
            int tu = Alive(EnemyKind.Turret), sa = Alive(EnemyKind.Saucer);
            if (tu + sa == 0) return;
            Voice.Say(sa > 0 ? "ctl_hot_saucers" : "ctl_hot", VoiceKind.Radio, 2, maxAge: 30f);
        }

        void ResetFight()
        {
            SetShield(false); gunArmed = false; Rig.Shake(0); LastKiller = "";
            if (Foes) foreach (var e in Foes.All) { e.Chasing = false; e.Locked = false; e.LockT = 0; if (e.Kind == EnemyKind.Saucer && !e.Dead) { e.Pos = e.Home; e.Vel = Vector3.forward * Tuning.T.saucerPatrolSpeed; } }
        }

        // ---- B8: the difficulty page (from the Campaign page: the next new career's; from the hangar: this career's)

        DifficultySetting EditedDifficulty => difficultyReturn == hangar && Career.C != null ? Career.C.difficulty : GameSettings.S.difficulty;

        void ShowDifficulty(MenuPanel.Page from) { difficultyReturn = from; Menu.Show(difficultyPage); }

        void DifficultyChanged()
        {
            if (difficultyReturn == hangar && Career.C != null)
            {
                Career.C.Save();
                if (Planet != null) SetupEnemies();   // the planet's enemies follow at once
                Ship.Level = CampaignLevel;
            }
            else GameSettings.Save();
        }

        void BuildDifficultyPage()
        {
            difficultyPage = new MenuPanel.Page { Title = "DIFFICULTY", Back = () => Menu.Show(difficultyReturn ?? campaignPage) };
            difficultyPage.Subtitle = "";
            System.Action<System.Action<DifficultySetting>> Custom = f => { var d = EditedDifficulty; f(d); d.preset = 4; DifficultyChanged(); };
            difficultyPage.Add(Option("Preset", () => Difficulty.PresetNames[Mathf.Clamp(EditedDifficulty.preset, 0, 4)], d => { var s = EditedDifficulty; s.ApplyPreset(Wrap(Mathf.Min(s.preset, 3) + (s.preset == 4 && d > 0 ? 0 : d), 4)); DifficultyChanged(); }))
                .Add(Option("Enemies", () => Difficulty.EnemyNames[Mathf.Clamp(EditedDifficulty.enemies, 0, 2)], d => Custom(s => s.enemies = Wrap(s.enemies + d, 3))))
                .Add(Option("Enemy aggression", () => Difficulty.AggressionNames[Mathf.Clamp(EditedDifficulty.aggression, 0, 3)], d => Custom(s => s.aggression = Wrap(s.aggression + d, 4))))
                .Add(Option("Aliens among the pilots", () => Difficulty.AlienShareNames[Mathf.Clamp(EditedDifficulty.alienShare, 0, 3)], d => Custom(s => s.alienShare = Wrap(s.alienShare + d, 4))))
                .Add(Option("Alien tells show", () => $"{Tuning.T.tellLevels[Mathf.Clamp(EditedDifficulty.tells, 0, 3)] * 100:0} %", d => Custom(s => s.tells = Wrap(s.tells + d, 4))))
                .Add(Option("False tells (real pilots)", () => $"{Tuning.T.falseTellLevels[Mathf.Clamp(EditedDifficulty.falseTells, 0, 2)] * 100:0} %", d => Custom(s => s.falseTells = Wrap(s.falseTells + d, 3))))
                .Add(Option("Crash level", () => CrashLevels.Names[Mathf.Clamp(EditedDifficulty.crashLevel, 0, 4)], d => Custom(s => s.crashLevel = Wrap(s.crashLevel + d, 5))))
                .Add(Option("Pay", () => $"x{Tuning.T.payLevels[Mathf.Clamp(EditedDifficulty.pay, 0, 3)]:0.##}", d => Custom(s => s.pay = Wrap(s.pay + d, 4))))
                .Add(Action("Back", () => Menu.Show(difficultyReturn ?? campaignPage)));
            difficultyPage.Body = () =>
                (difficultyReturn == hangar ? $"<b>This career</b> ({Career.C?.systemName} system). Changes apply at once." : "<b>A new career</b> starts on this.") + "\n\n" +
                "<b>Easy</b>: fewer, slower, less accurate enemies; tells nearly always show; pays less.\n<b>Normal</b>: as designed.  <b>Hard</b>: more enemies, sharper; some pilots' pads\n  under a turret's eye; real pilots can show false tells.  <b>Nightmare</b>: more of all of it; pays most.\n" +
                "Changing any option makes it <b>Custom</b>.\n\nEnemies: turrets on the high ground (fly low: they can't see you), saucers from your\nsecond planet on. Aliens: the alien numbers apply to planets you haven't visited yet.\nThe alien surprise is on in every setting.";
        }
    }
}
