using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace HostileHorizon
{
    // The game's one scene object (Assets/Scenes/Main.unity, made by Hostile Horizon > Build Executable). It builds everything at
    // start: settings, input, the look (sun, sky, haze, rock), the camera rig, the terrain streamer, the ship, the displays, the
    // sound, the mothership and the menus; then runs the flow: title -> Free Flight setup -> flying <-> paused -> crashed ->
    // restart / title. The Campaign flow (hangar, launch, docking, tow, death, regeneration) is in GameShell.Campaign.cs.
    // Round 2, item 1: a crash is an explosion and the burning wreck stays; the view pulls out live and circles it, and the menu
    // comes up over the live scene (Round 1's fade to black is gone: VR rule 5).
    // Fix 2.4: pause anywhere in play (also during launch, docking, tow, recall, the beam; time stops, resume carries on), with Start /
    // Esc / HOTAS 9 (or the button picked in Settings > Controls) / 10 and a tap of the gamepad's View; auto-pause when a player's
    // build loses focus; "≡ / Esc: Pause" on the HUD for the first minute of a flight; Reset position in the pause menu.
    public sealed partial class GameShell : MonoBehaviour
    {
        public enum State { Loading, Title, Flying, Paused, Crashed, Docked, Auto }
        public static GameShell I { get; private set; }
        public State Now { get; private set; } = State.Loading;
        public TerrainStreamer Terrain { get; private set; }
        public CameraRig Rig { get; private set; }
        public ShipController Ship { get; private set; }
        public MenuPanel Menu { get; private set; }
        public FlightDisplays Displays { get; private set; }
        public int RandomSeed { get; private set; }
        public int FlightSeed { get; private set; }
        public CrashLevel FlightLevel { get; private set; }
        public bool EditingSeed => editingSeed;

        ShipAudio shipAudio;
        public ShipExtras Extras { get; private set; }
        public BellyCam Belly { get; private set; }
        public bool FastForwarding { get; private set; }
        public Explosion Blast { get; private set; }
        Material litTemplate, debrisMaterial;
        Vector3 titlePos; float titleHeading; float titleClock;
        MenuPanel.Page title, freeFlight, settings, axes, display, credits, pause, crashed, loading;
        MenuPanel.Page settingsReturn, controls, sound;
        State pausedFrom; bool hudWasOn, viewDown, recallFiredThisHold; float flightStartedAt = -99;
        bool editingSeed; int seedCursor;

        void Awake() { I = this; }

        IEnumerator Start()
        {
            GameSettings.Load();
            Controls.Init();
            WorldLook.Create();
            Rig = CameraRig.Create();
            Terrain = new GameObject("Terrain").AddComponent<TerrainStreamer>();
            Terrain.Material = WorldLook.RockMaterial;
            Ship = CreateShip(out var model);
            Rig.Ship = Ship;
            Displays = FlightDisplays.Create(Ship, Ship.transform, Rig);   // ship space (the model under it is turned 180 degrees)
            Displays.SetVisible(false);
            Belly = BellyCam.Create(Ship); Displays.Belly = Belly;   // Fix 2.5: the belly / airlock camera
            shipAudio = ShipAudio.Create(Ship);
            Ship.OnHit += i => { shipAudio.Thud(i); Displays.NoteHit(); };
            Ship.OnCrash += OnCrash;
            Ship.OnEnginesStarted += OnEnginesStarted;
            Ship.OnOutOfEnergy += OnOutOfEnergy;   // Fix 2.3: the beam catches a ship that runs dry in the air
            Ship.OnBrake += () => Sfx.Play(Sfx.Brake, Ship.transform.position, .8f, 10f, Ship.transform);   // Fix 2.4: the air brakes
            Mother = Mothership.Create(litTemplate); Mother.gameObject.SetActive(false);
            Route = RouteTrail.Create();   // Fix 2.6
            CreateWorldExtras();           // Round 4: biome props, weather, music, voices
            CreateAliens();                // Round 3: the cabin, its camera
            Menu = MenuPanel.Create();
            RandomSeed = Seeds.NewRandom();
            BuildPages();
            BuildCampaignPages();
            TestRun.SetupHeadless(Rig.Head);   // a headless check: no window, the view drawn off-screen
            ApplySettings();

            Menu.Show(loading);
            yield return ShowTitleWorld();
            ShowTitle();
            if (CheckRun.Active) gameObject.AddComponent<ExeCheck>();
        }

        ShipController CreateShip(out Transform model)
        {
            var go = new GameObject("Ship");
            var ship = go.AddComponent<ShipController>();
            var prefab = Resources.Load<GameObject>("Models/Ship");
            var m = Instantiate(prefab, go.transform); m.name = "Model";
            m.transform.localRotation = Quaternion.Euler(0, 180, 0);   // Blender FBX files import facing back (as Chastain Rush's VehicleVisual)
            var glass = new Material(Shader.Find("HostileHorizon/Glass"));
            var used = new System.Collections.Generic.List<(Material, string)>();
            foreach (var r in m.GetComponentsInChildren<Renderer>())
            {
                string slot = r.name.Contains("__") ? r.name.Substring(r.name.IndexOf("__") + 2) : r.name;
                Material mat;
                if (slot == "glass") mat = glass;
                else if (slot == "glow" || slot == "lamp") { mat = new Material(Shader.Find("HostileHorizon/Glow")); mat.SetColor("_Color", slot == "glow" ? new Color(1f, .5f, .18f) * 2.2f : new Color(1f, .95f, .8f) * 1.6f); }
                else
                {
                    if (!litTemplate) litTemplate = new Material(r.sharedMaterial) { name = "Lit template" };
                    mat = new Material(r.sharedMaterial);
                    Color c; float smooth = .35f, metal = 0;
                    switch (slot)
                    {
                        case "hull": c = new Color(.66f, .66f, .62f); smooth = .45f; metal = .2f; break;
                        case "panel": c = new Color(.32f, .34f, .35f); smooth = .4f; break;
                        case "metal": c = new Color(.55f, .56f, .58f); smooth = .55f; metal = .7f; break;
                        case "engine": c = new Color(.15f, .16f, .17f); smooth = .3f; metal = .5f; break;
                        case "seat": c = new Color(.20f, .17f, .15f); smooth = .2f; break;
                        case "dash": c = new Color(.07f, .075f, .08f); smooth = .5f; break;
                        default: c = new Color(.24f, .25f, .27f); smooth = .3f; break;   // interior
                    }
                    mat.SetColor("_BaseColor", c); mat.SetFloat("_Smoothness", smooth); mat.SetFloat("_Metallic", metal);
                }
                r.sharedMaterial = mat; used.Add((mat, slot));
            }
            model = m.transform;
            Extras = ShipExtras.Add(ship, litTemplate, used);
            debrisMaterial = new Material(litTemplate); debrisMaterial.SetColor("_BaseColor", new Color(.13f, .12f, .11f)); debrisMaterial.SetFloat("_Smoothness", .1f);
            go.SetActive(false);
            return ship;
        }

        public void ApplySettings()
        {
            var s = GameSettings.S;
            AudioListener.volume = TestRun.Muted ? 0 : s.volume / 100f;   // test runs stay muted (TestRun)
            QualitySettings.vSyncCount = CheckRun.Active || s.frameLimit > 0 ? 0 : 1;
            Application.targetFrameRate = CheckRun.Active ? -1 : s.frameLimit > 0 ? s.frameLimit : -1;
            WorldLook.ApplyQuality(Terrain, Rig.Head);
            Controls.ApplySettings();
        }

        // ---- world for the title: the seed of the day, seen from its start, slowly panning (not flight: no comfort issue)
        IEnumerator ShowTitleWorld()
        {
            SetWorld(Seeds.OfTheDay, Biomes.ForSeed(Seeds.OfTheDay));   // Round 4: today's planet in its biome
            Terrain.Field.FindStart(out titlePos, out titleHeading);
            titlePos += Quaternion.Euler(0, titleHeading, 0) * new Vector3(0, -10, -40);
            LeaveCampaign(); ClearCrash();
            Rig.FollowShip = false; Ship.gameObject.SetActive(false); Displays.SetVisible(false);
            Rig.transform.SetPositionAndRotation(titlePos, Quaternion.Euler(-4, titleHeading, 0));
            Terrain.SetFocus(titlePos);
            float end = Time.realtimeSinceStartup + 12f;
            while (!Terrain.ReadyNear(1200f) && Time.realtimeSinceStartup < end) yield return null;
        }

        // Round 4: Free Flight's planet type (by the seed, or one picked on the Free Flight page)
        public static Biome FreeBiome(int seed) => GameSettings.S.freeBiome > 0 ? (Biome)Mathf.Clamp(GameSettings.S.freeBiome - 1, 0, 3) : Biomes.ForSeed(seed);

        void Update()
        {
            switch (Now)
            {
                case State.Title:
                    titleClock += Time.unscaledDeltaTime;
                    Rig.transform.SetPositionAndRotation(titlePos, Quaternion.Euler(-4 + Mathf.Sin(titleClock * .07f) * 2f, titleHeading + Mathf.Sin(titleClock * .05f) * 18f, 0));
                    Terrain.SetFocus(titlePos);
                    break;
                case State.Flying:
                    Terrain.SetFocus(Rig.transform.position);
                    if (Controls.CameraPressed) Rig.Cycle();
                    if (Controls.ViewPressed) { viewDown = true; recallFiredThisHold = false; }
                    if (InCampaign) CampaignTick();
                    Displays.PauseHint = Time.time - flightStartedAt < 60f;
                    if (Now == State.Flying && (Controls.MenuPressed || ViewTap())) Pause();
                    break;
                case State.Paused:
                    Terrain.SetFocus(Rig.transform.position);
                    if ((Controls.MenuPressed || Controls.ViewPressed) && (Menu.Current == pause || Menu.Current == campaignPause)) Resume();
                    break;
                case State.Crashed:
                case State.Docked:
                case State.Auto:
                    Terrain.SetFocus(Rig.transform.position);
                    if (Now == State.Docked) HoldDocked();
                    if (Now == State.Auto && Controls.CameraPressed && !Rig.Scripted) Rig.Cycle();
                    if (Now == State.Auto && (Controls.MenuPressed || Controls.ViewPressed)) Pause();
                    break;
            }
            // Fix 2.5: hold A / Space / HOTAS trigger to fast-forward an automatic part (it still plays live: VR rule 5, no cut)
            bool ff = Now == State.Auto && !Menu.Visible && Ship && Ship.Kinematic && Controls.FastForwardHeld && Phase != "horror";   // Round 3: never through the horror
            if (Now == State.Auto) Time.timeScale = ff ? Tuning.T.fastForward : 1;
            else if (Now != State.Paused && Time.timeScale > 1) Time.timeScale = 1;   // flight resumes at normal speed
            FastForwarding = ff;
            if (Belly) Belly.Allowed = Now == State.Flying || Now == State.Paused && pausedFrom == State.Flying;
            if (Props) WorldTick();   // Round 4
        }

        // ---- flow

        void ShowTitle()
        {
            Now = State.Title; Time.timeScale = 1;
            Menu.SetBackdrop(.15f); Menu.Show(title);
        }

        public void StartFlight(int seed, CrashLevel level) => StartCoroutine(StartFlightRoutine(seed, level));

        IEnumerator StartFlightRoutine(int seed, CrashLevel level)
        {
            Now = State.Loading; Time.timeScale = 1; Menu.SetBackdrop(.6f); Menu.Show(loading);
            FlightSeed = seed; FlightLevel = level;
            LeaveCampaign(); ClearCrash();
            SetWorld(seed, FreeBiome(seed));
            Terrain.Field.FindStart(out var pos, out var heading);
            Terrain.SetFocus(pos);
            float end = Time.realtimeSinceStartup + 10f;
            while (!Terrain.ReadyNear(700f) && Time.realtimeSinceStartup < end) yield return null;
            Ship.Field = Terrain.Field; Rig.Field = Terrain.Field;
            Ship.UsesEnergy = false; Ship.Energy = 100;
            Ship.Spawn(pos, heading, level);
            Rig.Current = (CameraRig.Mode)Mathf.Clamp(GameSettings.S.cameraDefault, 0, 3); Rig.Snap(); Rig.FollowShip = true;
            Displays.Campaign = false; Displays.SetVisible(true);
            Menu.Hide();
            Now = State.Flying; flightStartedAt = Time.time;
        }

        // A tap of View opens the pause menu: at once in Free Flight; in the Campaign on release, unless the hold became the recall.
        bool ViewTap()
        {
            if (!InCampaign) { bool p = Controls.ViewPressed; if (p) viewDown = false; return p; }
            if (viewDown && Controls.ViewReleased) { viewDown = false; return !recallFiredThisHold; }
            return false;
        }

        // Fix 2.4: in every state of play: flying, and the automatic parts (launch, docking, tow, recall, the beam) while no menu is up.
        public bool CanPause => Now == State.Flying || Now == State.Auto && !Menu.Visible;
        public State PausedFrom => pausedFrom;

        public void Pause()
        {
            if (!CanPause) return;
            pausedFrom = Now; hudWasOn = Displays.Visible; viewDown = false;
            Now = State.Paused; Time.timeScale = 0; Displays.SetVisible(false); Menu.SetBackdrop(.45f); Menu.Show(InCampaign ? campaignPause : pause);
        }

        public void Resume()
        {
            if (Now != State.Paused) return;
            Menu.Hide(); Displays.SetVisible(hudWasOn); Time.timeScale = 1; Now = pausedFrom; Ship.IgnoreButtonsUntil = Time.frameCount + 2;
        }

        // Fix 2.4: a player's build pauses when it loses focus (alt-tab, the Steam overlay, the Deck's Steam / ... buttons). Check runs
        // keep running unfocused; the check calls FocusLost itself to simulate it.
        void OnApplicationFocus(bool focus) { if (!focus && !CheckRun.Active && !TestRun.Muted) FocusLost(); }
        void OnApplicationPause(bool paused) { if (paused && !CheckRun.Active && !TestRun.Muted) FocusLost(); }
        public void FocusLost() => Pause();

        // Fix 2.4: Reset position: a steady hover over the nearest flat landing pad (else the start), same cabin and energy, no charge.
        // Not while a pilot is running to the ship or at the airlock, nor during the automatic parts.
        public bool CanReset => pausedFrom == State.Flying && !Ship.Crashed && !PilotComing && CanopyAlien == null && !sleeperTurning;
        bool PilotComing { get { foreach (var p in Pilots) if (p.Now == DownedPilot.State.Running || p.Now == DownedPilot.State.Knocking || p.Now == DownedPilot.State.Boarding) return true; return false; } }

        public void ResetPosition()
        {
            if (Now != State.Paused || !CanReset) return;
            var f = Terrain.Field; Vector3 at;
            if (f.NearestPad(Ship.transform.position, out var pad)) at = pad.Position; else { f.FindStart(out at, out _); at.y = f.SurfaceHeight(at.x, at.z); }
            Ship.ResetTo(at + Vector3.up * Tuning.T.resetHeight, Ship.Heading);
            Rig.Snap(); Terrain.SetFocus(Ship.transform.position);
            Resume();
            Flash("Position reset: hovering over a landing pad", 4f);
        }

        // The crash, in every mode: the explosion where the ship hit, the burning wreck left there, the view pulling out to circle it.
        void OnCrash()
        {
            var at = Ship.transform.position;
            int seed = Terrain.Field.SeedNumber ^ Mathf.RoundToInt(at.x * 7 + at.z * 13);
            Explosion.SettleWreck(Ship.transform, Terrain.Field, seed);
            Blast = Explosion.Create(at, Ship.CrashVelocity, Ship.transform, Terrain.Field, debrisMaterial, seed);
            Extras.SetLook(ShipExtras.Look.Wreck);
            Rig.WatchWreck(Ship.transform.position);
            Displays.SetVisible(false);
            Now = State.Crashed;
            if (InCampaign) Died(); else StartCoroutine(ShowCrashed());
        }

        IEnumerator ShowCrashed()
        {
            yield return new WaitForSecondsRealtime(2.5f);
            if (Now != State.Crashed) yield break;
            Menu.SetBackdrop(0); Menu.Show(crashed);
        }

        // Remove the last crash's explosion and wreck fire, the ship back to its own paint.
        void ClearCrash()
        {
            if (Blast) Destroy(Blast.gameObject);
            var fire = Ship.transform.Find("Wreck fire"); if (fire) Destroy(fire.gameObject);
            ClearAliens();   // Round 3: no creature, loop or broken hatch left over
            Extras.SetLook(ShipExtras.Look.Own);
            Rig.Watch = null; Rig.Scripted = false;
        }

        void ToTitle() => StartCoroutine(ToTitleRoutine());
        IEnumerator ToTitleRoutine() { Time.timeScale = 1; if (InCampaign) SaveCareer(); Menu.Show(loading); yield return ShowTitleWorld(); ShowTitle(); }

        public int ChosenSeed => GameSettings.S.seedMode == 0 ? Seeds.OfTheDay : GameSettings.S.seedMode == 1 ? RandomSeed : GameSettings.S.customSeed;

        // ---- pages

        static MenuPanel.Row Action(string label, System.Action a, System.Func<bool> disabled = null) => new MenuPanel.Row { Label = () => label, Submit = a, Disabled = disabled };
        static MenuPanel.Row Option(string label, System.Func<string> value, System.Action<int> change) =>
            new MenuPanel.Row { Label = () => label, Value = value, Left = () => change(-1), Right = () => change(1) };
        static int Wrap(int v, int n) => (v % n + n) % n;
        void Saved() { GameSettings.Save(); ApplySettings(); }

        void BuildPages()
        {
            loading = new MenuPanel.Page { Title = "HOSTILE HORIZON", Subtitle = "Charting the canyons…", Hint = () => "" };

            title = new MenuPanel.Page { Title = "HOSTILE HORIZON", Subtitle = "Rescue flights over a world that wants you dead",
                Body = () => $"Today's planet: {Names.Planet(Seeds.OfTheDay)} ({Biomes.Name(Biomes.ForSeed(Seeds.OfTheDay))}), seed {Seeds.OfTheDay}" };
            title.Add(Action("Free Flight", () => Menu.Show(freeFlight)))
                 .Add(Action("Campaign", () => Menu.Show(campaignPage)))
                 .Add(Action("Settings", () => { settingsReturn = title; Menu.Show(settings); }))
                 .Add(Action("Credits", () => Menu.Show(credits)))
                 .Add(Action("Quit", Quit));

            freeFlight = new MenuPanel.Page { Title = "FREE FLIGHT", Subtitle = "No pilots, no enemies. Just the planet.", Back = () => { editingSeed = false; Menu.Show(title); } };
            freeFlight.Add(Option("Seed", () => Seeds.ModeNames[GameSettings.S.seedMode], d =>
                {
                    GameSettings.S.seedMode = Wrap(GameSettings.S.seedMode + d, 3);
                    if (GameSettings.S.seedMode == 1) RandomSeed = Seeds.NewRandom();
                    GameSettings.Save();
                }))
                .Add(new MenuPanel.Row
                {
                    Label = () => GameSettings.S.seedMode == 2 ? "Seed number  <size=22>(select to type)</size>" : GameSettings.S.seedMode == 1 ? "Seed number  <size=22>(select for another)</size>" : "Seed number",
                    Value = () => editingSeed ? SeedEditText() : ChosenSeed.ToString(),
                    Submit = () => { if (GameSettings.S.seedMode == 2) { editingSeed = true; seedCursor = 7; } else if (GameSettings.S.seedMode == 1) RandomSeed = Seeds.NewRandom(); },
                })
                .Add(Option("Planet type", () => GameSettings.S.freeBiome == 0 ? $"By seed ({Biomes.Name(Biomes.ForSeed(ChosenSeed))})" : Biomes.Names[Mathf.Clamp(GameSettings.S.freeBiome - 1, 0, 3)],
                    d => { GameSettings.S.freeBiome = Wrap(GameSettings.S.freeBiome + d, 5); GameSettings.Save(); }))   // Round 4 (A1)
                .Add(Option("Crash level", () => CrashLevels.Names[GameSettings.S.crashLevel], d => { GameSettings.S.crashLevel = Wrap(GameSettings.S.crashLevel + d, 5); GameSettings.Save(); }))
                .Add(Action("Start flight", () => StartFlight(ChosenSeed, (CrashLevel)GameSettings.S.crashLevel)))
                .Add(Action("Back", () => Menu.Show(title)));
            freeFlight.Body = () => editingSeed
                ? "Type the seed number with the keyboard (Backspace deletes),\nor change the marked digit with up / down and move with left / right.\nA / Enter / trigger: done."
                : $"<b>{CrashLevels.Names[GameSettings.S.crashLevel]}</b>\n{CrashLevels.Explain[GameSettings.S.crashLevel]}\n\nThe same seed always makes the same planet.\nSeed of the day changes at midnight.";
            freeFlight.CaptureInput = SeedEditInput;
            if (Keyboard.current != null) Keyboard.current.onTextInput += ch =>
            {
                if (!editingSeed) return;
                if (ch >= '0' && ch <= '9') { long v = (long)GameSettings.S.customSeed * 10 + (ch - '0'); if (v <= Seeds.Max) GameSettings.S.customSeed = (int)v; }
            };

            settings = new MenuPanel.Page { Title = "SETTINGS", Back = () => Menu.Show(settingsReturn ?? title) };
            // Fix 2.4: Flight assist and Stick sensitivity first; the HOTAS rows moved to Settings > Controls (a page has at most 12 rows)
            settings.Add(Option("Flight assist", () => GameSettings.AssistNames[Mathf.Clamp(GameSettings.S.flightAssist, 0, 2)], d => { GameSettings.S.flightAssist = Wrap(GameSettings.S.flightAssist + d, 3); Saved(); }))
                .Add(Option("Stick sensitivity", () => GameSettings.S.sensitivity + "%", d => { GameSettings.S.sensitivity = Mathf.Clamp(GameSettings.S.sensitivity + d * 10, 50, 150); Saved(); }))
                .Add(Option("Invert pitch", () => GameSettings.S.invertPitch ? "On" : "Off", d => { GameSettings.S.invertPitch = !GameSettings.S.invertPitch; Saved(); }))
                .Add(Option("Stick deadzone", () => GameSettings.S.deadzone + "%", d => { GameSettings.S.deadzone = Mathf.Clamp(GameSettings.S.deadzone + d * 2, 0, 30); Saved(); }))
                .Add(Action("Controls and HOTAS", () => Menu.Show(controls)))
                .Add(Action("Display", () => Menu.Show(display)))
                .Add(Option("Default view", () => CameraRig.ModeNames[GameSettings.S.cameraDefault], d => { GameSettings.S.cameraDefault = Wrap(GameSettings.S.cameraDefault + d, 4); Saved(); }))
                .Add(Option("Graphics quality", () => new[] { "Low", "Medium", "High" }[GameSettings.S.quality], d => { GameSettings.S.quality = Wrap(GameSettings.S.quality + d, 3); Saved(); }))
                .Add(Option("Steam Deck", () => SteamDeck.Label, d => { GameSettings.S.steamDeck = Wrap(GameSettings.S.steamDeck + d, 3); Saved(); }))
                .Add(Action("Sound", () => Menu.Show(sound)))   // Round 4 (B4, C4): the volumes and subtitles
                .Add(Option("Route trail", () => GameSettings.S.routeTrail ? "On" : "Off", d => { GameSettings.S.routeTrail = !GameSettings.S.routeTrail; Saved(); }))
                .Add(Action("Back", () => Menu.Show(settingsReturn ?? title)));
            settings.Body = () =>
                "<b>Flight assist</b>: Full banks the ship round on its own and levels the wings quickly (as 0.2.3).\n  Light (default): stick centred, the ship holds its attitude; pulling while banked turns a little\n  tighter. Off: none; bank and pull to turn.\n" +
                "<b>Stick sensitivity</b>: how fast the ship turns at full stick (100 % = normal).\n" +
                "<b>Route trail</b>: glowing gates along the canyons to the nearest pilot (then the mothership).\n\n" +
                "Steam Deck: shorter view and lighter shadows for the frame rate.\nAuto turns it on by itself on the Deck.";

            // Round 4 (B4, C4): Master stays; Music, Effects and Voices on their own; subtitles for the voices
            sound = new MenuPanel.Page { Title = "SOUND", Back = () => Menu.Show(settings), Body = () =>
                "<b>Master</b>: everything.  <b>Music</b>: the soundtrack and the short music cues.\n<b>Effects</b>: the ship, the world, the scares.  <b>Voices</b>: Control, the ship's computer, the pilots.\n<b>Subtitles</b>: what is said, at the bottom of the screen and on the cockpit's centre display." };
            System.Func<int, string> Pct = v => v + "%"; System.Func<int, int, int> Step = (v, d) => Mathf.Clamp(v + d * 10, 0, 100);
            sound.Add(Option("Master volume", () => Pct(GameSettings.S.volume), d => { GameSettings.S.volume = Step(GameSettings.S.volume, d); Saved(); }))
                 .Add(Option("Music", () => Pct(GameSettings.S.musicVolume), d => { GameSettings.S.musicVolume = Step(GameSettings.S.musicVolume, d); Saved(); }))
                 .Add(Option("Effects", () => Pct(GameSettings.S.effectsVolume), d => { GameSettings.S.effectsVolume = Step(GameSettings.S.effectsVolume, d); Saved(); }))
                 .Add(Option("Voices", () => Pct(GameSettings.S.voiceVolume), d => { GameSettings.S.voiceVolume = Step(GameSettings.S.voiceVolume, d); Saved(); }))
                 .Add(Option("Subtitles", () => GameSettings.S.subtitles ? "On" : "Off", d => { GameSettings.S.subtitles = !GameSettings.S.subtitles; Saved(); }))
                 .Add(Action("Back", () => Menu.Show(settings)));

            controls = new MenuPanel.Page { Title = "CONTROLS", Back = () => Menu.Show(settings) };
            controls.Add(Option("HOTAS throttle axis", () => GameSettings.S.throttleAxis == 0 ? $"Auto ({Controls.ThrottleAxisPath.Replace("<Joystick>/", "")})" : GameSettings.ThrottleAxes[GameSettings.S.throttleAxis],
                    d => { GameSettings.S.throttleAxis = Wrap(GameSettings.S.throttleAxis + d, GameSettings.ThrottleAxes.Length); Saved(); }))
                .Add(Option("Invert throttle lever", () => GameSettings.S.invertThrottle ? "On" : "Off", d => { GameSettings.S.invertThrottle = !GameSettings.S.invertThrottle; Saved(); }))
                .Add(Option("HOTAS pause button", () => "button " + GameSettings.S.hotasPauseButton, d => { GameSettings.S.hotasPauseButton = Mathf.Clamp(GameSettings.S.hotasPauseButton + d, 1, 16); Saved(); }))
                .Add(Action("Axis display", () => Menu.Show(axes)))
                .Add(Action("Back", () => Menu.Show(settings)));
            controls.Body = () =>
                "<b>Controls</b>\n" +
                $"HOTAS: stick pitch / roll, twist yaw, throttle lever, hat look,\n  button 3 view, button {GameSettings.S.hotasPauseButton} / 10 pause, 4 landing mode / brake,\n  5 engines, 6 airlock, 7 (hold) recall, 8 belly camera,\n  11 route target, 12 cabin camera.\n" +
                "Gamepad: left stick pitch / roll, LB / RB yaw, RT / LT throttle,\n  right stick look, Y view, Start or a tap of View pause,\n  X landing mode / brake, A engines, B airlock, hold View recall,\n  D-pad down belly camera, right route target, up cabin camera.\n" +
                "Keyboard: W / S pitch, A / D roll, Q / E yaw, Shift / Ctrl throttle,\n  arrows look (or hold the right mouse button), C view, Esc pause,\n  G landing mode / brake, K engines, O airlock, hold H recall, V belly camera,\n  T route target, N cabin camera.\n" +
                "Landing mode: stick forward / back moves, left / right turns,\n  throttle up / down climbs and sinks (faster high up). The belly camera\n  comes on by itself; look down (look control) through the glass floor.\n\n" +
                "Pause button: watch Axis display's \"buttons held\" to find a button's number.";

            axes = new MenuPanel.Page { Title = "AXIS DISPLAY", Subtitle = "Move the stick, twist, lever and hat: the bars follow.", Back = () => Menu.Show(controls),
                Body = Controls.AxisReport, Hint = () => "B / Esc / HOTAS button 2: back" };
            axes.Add(Action("Back", () => Menu.Show(controls)));

            // Fix 2.1: Chastain Rush's Settings > Display page (Core/ScreenSetting.cs).
            display = new MenuPanel.Page { Title = "DISPLAY", Back = () => Menu.Show(settings), Body = () =>
                "Full screen fills the whole screen at the chosen resolution.\nDefault: the display's own resolution, at most 1080 lines tall (1280 x 800 on the Steam Deck).\nAlt+Enter also switches for the moment." };
            display.Add(Option("Screen", () => ScreenSetting.ScreenLabel, d => ScreenSetting.Set(!ScreenSetting.Fullscreen)))
                   .Add(Option("Resolution", () => ScreenSetting.ResolutionLabel, ScreenSetting.StepResolution))
                   .Add(Action("Back", () => Menu.Show(settings)));

            credits = new MenuPanel.Page { Title = "CREDITS", Back = () => Menu.Show(title), Body = () =>
                "<b>Hostile Horizon</b>\nA game by Dan, built with Claude.\nInspired by the rescue flight games of the 1980s; all names, art, sound and text are new.\n\n" +
                "<b>Made with</b>\nUnity 6 (URP, Input System) · Blender · Meshy · Mixamo\nMusic: Suno (Dan's tracks) · ElevenLabs (short music cues, sounds and voices)\n\n" +
                "<b>Launcher third-party code</b>\nJSON for Modern C++ © Niels Lohmann (MIT licence)\nminiz © Rich Geldreich, RAD Game Tools, Valve (MIT licence)\nLicence texts ship beside the launcher (Launcher-Licenses).\n\n" +
                "Textures, trees, buildings and ice in this build are generated by the game itself (no third-party textures)." };
            credits.Add(Action("Back", () => Menu.Show(title)));

            pause = new MenuPanel.Page { Title = "PAUSED", Back = Resume };
            pause.Subtitle = "";
            pause.Body = () => $"Seed {FlightSeed}  ·  {Biomes.Name(WorldBiome)}  ·  {CrashLevels.Names[(int)FlightLevel]}";
            pause.Add(Action("Resume", Resume))
                 .Add(Action("Reset position", ResetPosition, () => !CanReset))
                 .Add(Action("Restart (same seed)", () => StartFlight(FlightSeed, FlightLevel)))
                 .Add(Action("Settings", () => { settingsReturn = pause; Menu.Show(settings); }))
                 .Add(Action("Return to title", ToTitle))
                 .Add(Action("Quit game", Quit));

            crashed = new MenuPanel.Page { Title = "SHIP LOST", Body = () => $"Seed {FlightSeed}  ·  {CrashLevels.Names[(int)FlightLevel]}\nImpact at {Ship.LastImpact * 3.6f:0} km/h." };
            crashed.Add(Action("Restart (same seed)", () => StartFlight(FlightSeed, FlightLevel)))
                   .Add(Action("Return to title", ToTitle));
        }

        string SeedEditText()
        {
            string s = GameSettings.S.customSeed.ToString("00000000");
            return s.Substring(0, seedCursor) + "[" + s[seedCursor] + "]" + s.Substring(seedCursor + 1);
        }

        float seedNavTimer; int seedNavDir;
        bool SeedEditInput()
        {
            if (!editingSeed) return false;
            if (Controls.SubmitPressed) { editingSeed = false; GameSettings.Save(); return true; }
            if (Controls.CancelPressed) { GameSettings.S.customSeed /= 10; return true; }
            var nav = Controls.Navigate;
            int dir = Mathf.Abs(nav.y) > .55f ? (nav.y > 0 ? 1 : 2) : Mathf.Abs(nav.x) > .55f ? (nav.x > 0 ? 4 : 3) : 0;
            bool fire = false;
            if (dir != seedNavDir) { seedNavDir = dir; fire = dir != 0; seedNavTimer = .38f; }
            else if (dir != 0) { seedNavTimer -= Time.unscaledDeltaTime; if (seedNavTimer <= 0) { fire = true; seedNavTimer = .12f; } }
            if (fire)
            {
                if (dir == 3) seedCursor = Mathf.Max(0, seedCursor - 1);
                else if (dir == 4) seedCursor = Mathf.Min(7, seedCursor + 1);
                else
                {
                    var digits = GameSettings.S.customSeed.ToString("00000000").ToCharArray();
                    int dv = digits[seedCursor] - '0'; dv = Wrap(dv + (dir == 1 ? 1 : -1), 10); digits[seedCursor] = (char)('0' + dv);
                    GameSettings.S.customSeed = int.Parse(new string(digits));
                }
            }
            return true;
        }

        void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
