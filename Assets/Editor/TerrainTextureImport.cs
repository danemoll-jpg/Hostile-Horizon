using UnityEditor;

namespace HostileHorizon
{
    // Round 11 (B1, B3): import settings for the CC0 terrain textures (Tools/make_rock_textures.py): normal maps as normal maps, colour
    // as sRGB, full mip chains, anisotropic filtering (walls seen at grazing angles), compressed (BC7 colour, BC5 normals on PC).
    // About 24 MB of texture memory in all (B3); the same on the Steam Deck.
    public sealed class TerrainTextureImport : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.Replace('\\', '/').Contains("Resources/Textures/Terrain/")) return;
            var t = (TextureImporter)assetImporter;
            bool normal = assetPath.EndsWith("_normal.jpg");
            t.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            t.sRGBTexture = !normal;
            t.mipmapEnabled = true; t.streamingMipmaps = false;
            t.anisoLevel = 8; t.wrapMode = UnityEngine.TextureWrapMode.Repeat; t.filterMode = UnityEngine.FilterMode.Trilinear;
            t.maxTextureSize = 2048;
            t.textureCompression = TextureImporterCompression.CompressedHQ;
            t.alphaSource = TextureImporterAlphaSource.None;
        }
    }
}
