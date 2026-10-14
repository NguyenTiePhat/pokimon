using UnityEditor;
using UnityEngine;

public sealed class PixelTextureImport : AssetPostprocessor
{
    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/Resources/")) return;
        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Default;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;
        // Read source shadow markers once to calculate the animation pivots.
        importer.isReadable = assetPath.EndsWith("-Shadow.png");
    }
}
