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
        public Explosion Blast { get; private set; }
        Material litTemplate, debrisMaterial;
        Vector3 titlePos; float titleHeading; float titleClock;
        MenuPanel.Page title, freeFlight, settings, axes, credits, pause, crashed, loading;
        MenuPanel.Page settingsReturn;
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
            shipAudio = ShipAudio.Create(Ship);
            Ship.OnHit += i => { shipAudio.Thud(i); Displays.NoteHit(); };
            Ship.OnCrash += OnCrash;
            Ship.OnEnginesStarted += OnEnginesStarted;
            Mother = Mothership.Create(litTemplate); Mother.gameObject.SetActive(false);
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
            SetWorld(Seeds.OfTheDay);
            Terrain.Field.FindStart(out titlePos, out titleHeading);
            titlePos += Quaternion.Euler(0, titleHeading, 0) * new Vector3(0, -10, -40);
            LeaveCampaign(); ClearCrash();
            Rig.FollowShip = false; Ship.gameObject.SetActive(false); Displays.SetVisible(false);
            Rig.transform.SetPositionAndRotation(titlePos, Quaternion.Euler(-4, titleHeading, 0));
            Terrain.SetFocus(titlePos);
            float end = Time.realtimeSinceStartup + 12f;
            while (!Terrain.ReadyNear(1200f) && Time.realtimeSinceStartup < end) yield return null;
        }

        void SetWorld(int seed)
        {
            if (Terrain.Field != null && Terrain.Field.SeedNumber == seed) return;
            Terrain.SetField(new CanyonField(seed));
        }

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
                    if (InCampaign) CampaignTick();
                    if (Controls.MenuPressed && Now == State.Flying) Pause();
                    break;
                case State.Paused:
                    Terrain.SetFocus(Rig.transform.position);
                    if (Controls.MenuPressed && Menu.Current == pause) Resume();
                    break;
                case State.Crashed:
                case State.Docked:
                case State.Auto:
                    Terrain.SetFocus(Rig.transform.position);
                    if (Now == State.Docked) HoldDocked();
                    if (Now == State.Auto && Controls.CameraPressed && !Rig.Scripted) Rig.Cycle();
                    break;
            }
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
            SetWorld(seed);
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
            Now = State.Flying;
        }

        public void Pause()
        {
            if (Now != State.Flying) return;
            Now = State.Paused; Time.timeScale = 0; Displays.SetVisible(false); Menu.SetBackdrop(.45f); Menu.Show(InCampaign ? campaignPause : pause);
        }

        public void Resume()
        {
            if (Now != State.Paused) return;
            Menu.Hide(); Displays.SetVisible(true); Time.timeScale = 1; Now = State.Flying; Ship.IgnoreButtonsUntil = Time.frameCount + 2;
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
                Body = () => $"Today's canyons: seed {Seeds.OfTheDay}" };
            title.Add(Action("Free Flight", () => Menu.Show(freeFlight)))
                 .Add(Action("Campaign", () => Menu.Show(campaignPage)))
                 .Add(Action("Settings", () => { settingsReturn = title; Menu.Show(settings); }))
                 .Add(Action("Credits", () => Menu.Show(credits)))
                 .Add(Action("Quit", Quit));

            freeFlight = new MenuPanel.Page { Title = "FREE FLIGHT", Subtitle = "No pilots, no enemies. Just the canyons.", Back = () => { editingSeed = false; Menu.Show(title); } };
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
                .Add(Option("Crash level", () => CrashLevels.Names[GameSettings.S.crashLevel], d => { GameSettings.S.crashLevel = Wrap(GameSettings.S.crashLevel + d, 5); GameSettings.Save(); }))
                .Add(Action("Start flight", () => StartFlight(ChosenSeed, (CrashLevel)GameSettings.S.crashLevel)))
                .Add(Action("Back", () => Menu.Show(title)));
            freeFlight.Body = () => editingSeed
                ? "Type the seed number with the keyboard (Backspace deletes),\nor change the marked digit with up / down and move with left / right.\nA / Enter / trigger: done."
                : $"<b>{CrashLevels.Names[GameSettings.S.crashLevel]}</b>\n{CrashLevels.Explain[GameSettings.S.crashLevel]}\n\nThe same seed always makes the same canyons.\nSeed of the day changes at midnight.";
            freeFlight.CaptureInput = SeedEditInput;
            if (Keyboard.current != null) Keyboard.current.onTextInput += ch =>
            {
                if (!editingSeed) return;
                if (ch >= '0' && ch <= '9') { long v = (long)GameSettings.S.customSeed * 10 + (ch - '0'); if (v <= Seeds.Max) GameSettings.S.customSeed = (int)v; }
            };

            settings = new MenuPanel.Page { Title = "SETTINGS", Back = () => Menu.Show(settingsReturn ?? title) };
            settings.Add(Option("Invert pitch", () => GameSettings.S.invertPitch ? "On" : "Off", d => { GameSettings.S.invertPitch = !GameSettings.S.invertPitch; Saved(); }))
                .Add(Option("Stick deadzone", () => GameSettings.S.deadzone + "%", d => { GameSettings.S.deadzone = Mathf.Clamp(GameSettings.S.deadzone + d * 2, 0, 30); Saved(); }))
                .Add(Option("HOTAS throttle axis", () => GameSettings.S.throttleAxis == 0 ? $"Auto ({Controls.ThrottleAxisPath.Replace("<Joystick>/", "")})" : GameSettings.ThrottleAxes[GameSettings.S.throttleAxis],
                    d => { GameSettings.S.throttleAxis = Wrap(GameSettings.S.throttleAxis + d, GameSettings.ThrottleAxes.Length); Saved(); }))
                .Add(Option("Invert throttle lever", () => GameSettings.S.invertThrottle ? "On" : "Off", d => { GameSettings.S.invertThrottle = !GameSettings.S.invertThrottle; Saved(); }))
                .Add(Action("Axis display", () => Menu.Show(axes)))
                .Add(Option("Default view", () => CameraRig.ModeNames[GameSettings.S.cameraDefault], d => { GameSettings.S.cameraDefault = Wrap(GameSettings.S.cameraDefault + d, 4); Saved(); }))
                .Add(Option("Graphics quality", () => new[] { "Low", "Medium", "High" }[GameSettings.S.quality], d => { GameSettings.S.quality = Wrap(GameSettings.S.quality + d, 3); Saved(); }))
                .Add(Option("Steam Deck", () => SteamDeck.Label, d => { GameSettings.S.steamDeck = Wrap(GameSettings.S.steamDeck + d, 3); Saved(); }))
                .Add(Option("Volume", () => GameSettings.S.volume + "%", d => { GameSettings.S.volume = Mathf.Clamp(GameSettings.S.volume + d * 10, 0, 100); Saved(); }))
                .Add(Action("Back", () => Menu.Show(settingsReturn ?? title)));
            settings.Body = () =>
                "<b>Controls</b>\n" +
                "HOTAS: stick pitch / roll, twist yaw, throttle lever, hat look,\n  button 3 view, button 9 / 10 menu.\n" +
                "  button 4 landing mode, 5 engines, 6 airlock.\n" +
                "Gamepad: left stick pitch / roll, LB / RB yaw, RT / LT throttle,\n  right stick look, Y view, Start menu,\n  X landing mode, A engines, B airlock.\n" +
                "Keyboard: W / S pitch, A / D roll, Q / E yaw, Shift / Ctrl throttle,\n  arrows look (or hold the right mouse button), C view, Esc menu,\n  G landing mode, K engines, O airlock.\n" +
                "Landing mode: stick forward / back moves, left / right turns,\n  throttle up / down climbs and sinks.\n\n" +
                "Steam Deck: shorter view and lighter shadows for the frame rate.\nAuto turns it on by itself on the Deck.";

            axes = new MenuPanel.Page { Title = "AXIS DISPLAY", Subtitle = "Move the stick, twist, lever and hat: the bars follow.", Back = () => Menu.Show(settings),
                Body = Controls.AxisReport, Hint = () => "B / Esc / HOTAS button 2: back" };
            axes.Add(Action("Back", () => Menu.Show(settings)));

            credits = new MenuPanel.Page { Title = "CREDITS", Back = () => Menu.Show(title), Body = () =>
                "<b>Hostile Horizon</b>\nA game by Dan, built with Claude.\nInspired by the rescue flight games of the 1980s; all names, art, sound and text are new.\n\n" +
                "<b>Made with</b>\nUnity 6 (URP, Input System) · Blender · Suno · ElevenLabs\n\n" +
                "<b>Launcher third-party code</b>\nJSON for Modern C++ © Niels Lohmann (MIT licence)\nminiz © Rich Geldreich, RAD Game Tools, Valve (MIT licence)\nLicence texts ship beside the launcher (Launcher-Licenses).\n\n" +
                "Textures and sounds in this build are generated by the game itself." };
            credits.Add(Action("Back", () => Menu.Show(title)));

            pause = new MenuPanel.Page { Title = "PAUSED", Back = Resume };
            pause.Subtitle = "";
            pause.Body = () => $"Seed {FlightSeed}  ·  {CrashLevels.Names[(int)FlightLevel]}";
            pause.Add(Action("Resume", Resume))
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
