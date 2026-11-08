using UnityEditor;
using UnityEngine;

namespace HostileHorizon
{
    // Round 8 (B1): the hard-surface Meshy models' materials (Tools/import_hard.py puts their textures in Resources/Hard/): one URP Lit
    // material per model, Resources/Hard/<name>.mat, as assets so the build keeps the normal-map / metallic-map variants. The ship's
    // hull is drawn from both sides: its inside closes the cockpit (the canopy panes are the game's glass, seen from outside only).
    // Called by PlayerBuild before every build. The game picks them by the FBX part's slot "mx_<name>".
    public sealed class HardImport : AssetPostprocessor
    {
        const string Root = "Assets/Resources/Hard/";
        public static readonly string[] Names = { "ship", "cockpit_seat", "cockpit_dash", "cockpit_console", "mothership", "bay_wall", "bay_cradle", "regen_pod", "turret", "saucer" };

        public static void MakeMaterials()
        {
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            foreach (var n in Names)
            {
                var albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + n + "_albedo.png");
                if (!albedo) continue;
                string path = Root + n + ".mat";
                var m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (!m) { m = new Material(lit); AssetDatabase.CreateAsset(m, path); }
                m.shader = lit;
                m.SetTexture("_BaseMap", albedo); m.SetColor("_BaseColor", Color.white);
                m.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Root + n + "_normal.png")); m.SetFloat("_BumpScale", 1f); m.EnableKeyword("_NORMALMAP");
                m.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Root + n + "_mask.png"));
                m.EnableKeyword("_METALLICSPECGLOSSMAP"); m.SetFloat("_Smoothness", 1f); m.SetFloat("_Metallic", 1f);
                bool twoSided = n == "ship" || n == "mothership";   // the cockpit / the bay seen from inside the hull
                m.SetFloat("_Cull", twoSided ? 0f : 2f); m.doubleSidedGI = twoSided;
                EditorUtility.SetDirty(m);
            }
            AssetDatabase.SaveAssets();
        }

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Root)) return;
            var ti = (TextureImporter)assetImporter;
            ti.textureType = assetPath.EndsWith("_normal.png") ? TextureImporterType.NormalMap : TextureImporterType.Default;
            ti.sRGBTexture = !assetPath.Contains("_mask") && !assetPath.EndsWith("_normal.png");
            ti.maxTextureSize = assetPath.Contains("_mask") ? 1024 : 2048;
            ti.textureCompression = TextureImporterCompression.Compressed;
            ti.alphaSource = assetPath.Contains("_mask") ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None;
        }
    }
}
