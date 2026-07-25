using UnityEditor;
using UnityEngine;

public class AudioImportSettings : AssetPostprocessor
{
    void OnPreprocessAudio()
    {
        var path = assetPath.Replace('\\', '/');
        if (!path.Contains("/Resources/Audio/") ||
            !System.IO.Path.GetFileNameWithoutExtension(path).EndsWith("_theme"))
        {
            return;
        }

        var importer = (AudioImporter)assetImporter;
        var settings = importer.defaultSampleSettings;
        settings.loadType = AudioClipLoadType.Streaming;
        settings.compressionFormat = AudioCompressionFormat.Vorbis;
        settings.quality = 0.6f;
        importer.defaultSampleSettings = settings;
        importer.forceToMono = false;
    }
}
