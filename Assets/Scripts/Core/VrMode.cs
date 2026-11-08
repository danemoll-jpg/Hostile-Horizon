using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;

namespace HostileHorizon
{
    // Round 8 (C1, C2): VR through OpenXR (Quest 3 over Virtual Desktop or SteamVR). The same exe plays flat or VR. VR starts ONLY when
    // asked: -vr on the command line (Play Hostile Horizon (VR).bat, or the launcher with -vr), or Settings > Display > VR mode On.
    // Never on the Steam Deck. With VR asked for and no headset / OpenXR runtime, it says so and the game starts flat (it never
    // waits on a runtime). XR is not started with the game (Editor/XrSetup.cs), so with VR off nothing opens SteamVR.
    public static class VrMode
    {
        static bool Has(string a) => Array.IndexOf(Environment.GetCommandLineArgs(), a) >= 0;
        public static bool Asked => Has("-vr") || (GameSettings.S != null && GameSettings.S.vrMode == 1);
        public static bool Requested => Asked && !SteamDeck.On;
        public static bool Active { get; private set; }
        public static string Status { get; private set; } = "flat screen (VR mode off)";
        public static string Runtime { get; private set; } = "";

        public static IEnumerator Start()
        {
            if (!Asked) { Status = "flat screen (VR mode off)"; yield break; }
            if (SteamDeck.On) { Status = "flat screen (VR is off on the Steam Deck)"; yield break; }
            var gen = XRGeneralSettings.Instance;
            if (gen == null || gen.Manager == null) { Status = "VR asked for, but this build has no XR settings: flat screen"; yield break; }
            Debug.Log("[VR] starting OpenXR");
            yield return gen.Manager.InitializeLoader();
            if (gen.Manager.activeLoader == null) { Status = "VR asked for, but no headset or OpenXR runtime was found: flat screen. Start Virtual Desktop or SteamVR first."; Debug.Log("[VR] " + Status); yield break; }
            gen.Manager.StartSubsystems();
            var inputs = new List<XRInputSubsystem>(); SubsystemManager.GetSubsystems(inputs);
            foreach (var i in inputs) i.TrySetTrackingOriginMode(TrackingOriginModeFlags.Device);   // seated: the head's position from where it starts
            Runtime = gen.Manager.activeLoader.name;
            Active = true; Status = $"VR on ({Runtime}; {XRSettings.eyeTextureWidth} x {XRSettings.eyeTextureHeight} per eye, {XRSettings.stereoRenderingMode})";
            Debug.Log("[VR] " + Status);
        }

        public static void Stop()
        {
            if (!Active) return;
            var m = XRGeneralSettings.Instance.Manager; m.StopSubsystems(); m.DeinitializeLoader(); Active = false;
        }

        // the headset's own refresh rate (72 / 90 / 120 Hz), 0 if unknown
        public static float RefreshRate { get { var d = new List<XRDisplaySubsystem>(); SubsystemManager.GetSubsystems(d); return d.Count > 0 && d[0].TryGetDisplayRefreshRate(out float r) ? r : 0; } }
    }
}
