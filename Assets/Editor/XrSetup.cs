using UnityEditor;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;

namespace HostileHorizon
{
    // Round 8 (C1): VR through OpenXR, PC Standalone only, set up in code before every build (PlayerBuild) so the project needs no
    // hand-made XR settings: the OpenXR loader for Windows, NOT started with the game (Core/VrMode.cs starts it only for -vr or the
    // VR mode setting), single-pass instanced rendering. Nothing mock is built in: the check points the OpenXR loader at the
    // package's mock runtime with the XR_RUNTIME_JSON environment variable.
    public static class XrSetup
    {
        public static void Ensure()
        {
            const string dir = "Assets/XR";
            if (!AssetDatabase.IsValidFolder(dir)) AssetDatabase.CreateFolder("Assets", "XR");
            EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out XRGeneralSettingsPerBuildTarget perTarget);
            if (!perTarget)
            {
                perTarget = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
                AssetDatabase.CreateAsset(perTarget, dir + "/XRGeneralSettingsPerBuildTarget.asset");
                EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, perTarget, true);
            }
            var g = BuildTargetGroup.Standalone;
            if (!perTarget.HasSettingsForBuildTarget(g)) perTarget.CreateDefaultSettingsForBuildTarget(g);
            if (!perTarget.HasManagerSettingsForBuildTarget(g)) perTarget.CreateDefaultManagerSettingsForBuildTarget(g);
            var gs = perTarget.SettingsForBuildTarget(g);
            gs.InitManagerOnStart = false;                            // never start XR by itself (VR rule: only when asked)
            XRPackageMetadataStore.AssignLoader(gs.Manager, "UnityEngine.XR.OpenXR.OpenXRLoader", g);
            var ox = OpenXRSettings.GetSettingsForBuildTargetGroup(g);
            if (ox) { ox.renderMode = OpenXRSettings.RenderMode.SinglePassInstanced; EditorUtility.SetDirty(ox); }
            EditorUtility.SetDirty(gs); EditorUtility.SetDirty(perTarget);
            AssetDatabase.SaveAssets();
            Debug.Log($"XR setup: OpenXR loader for Standalone {(gs.Manager.activeLoaders.Count > 0 ? "assigned" : "MISSING")}, start with the game {gs.InitManagerOnStart}, render mode {(ox ? ox.renderMode.ToString() : "?")}");
        }
    }
}
