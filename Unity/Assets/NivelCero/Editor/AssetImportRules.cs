using UnityEditor;
using UnityEngine;

namespace NivelCero.Editor
{
    /// <summary>
    /// Import rules for the generated pixel art and audio, applied automatically
    /// whenever Tools/Art or Tools/Audio regenerate their files.
    /// </summary>
    public sealed class AssetImportRules : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.Contains("/Resources/Academic2D/")) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.npotScale = TextureImporterNPOTScale.None; // keep exact pixel grids
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.isReadable = false;
            importer.maxTextureSize = 4096;
        }

        void OnPreprocessAudio()
        {
            if (!assetPath.Contains("/Resources/Audio/")) return;
            var importer = (AudioImporter)assetImporter;
            bool longLoop = assetPath.Contains("music_") || assetPath.Contains("amb_") || assetPath.Contains("stinger_");
            var settings = importer.defaultSampleSettings;
            settings.loadType = longLoop ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = longLoop ? AudioCompressionFormat.Vorbis : AudioCompressionFormat.PCM;
            settings.quality = .75f;
            importer.defaultSampleSettings = settings;
            importer.forceToMono = !longLoop;
            importer.loadInBackground = longLoop;
        }
    }
}
