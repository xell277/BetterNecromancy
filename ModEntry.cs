using System.IO;
using System;

namespace BetterNecromancy
{
    [ModLoader.ModManager]
    public static class ModEntry
    {
        public const string Namespace = "BetterNecromancy";

        public static string ModFolder { get; private set; } = string.Empty;

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnAssemblyLoaded, Namespace + ".OnAssemblyLoaded")]
        public static void OnAssemblyLoaded(string path)
        {
            ModFolder = Path.GetDirectoryName(path)?.Replace("\\", "/") ?? string.Empty;
            TryLoadOptionalAudioPatches();
        }

        private static void TryLoadOptionalAudioPatches()
        {
            if (string.IsNullOrEmpty(ModFolder))
                return;

            TryQueueAudioPatch(Path.Combine(ModFolder, "betternecromancyaudio.json"));
            TryQueueAudioPatch(Path.Combine(ModFolder, "Audio", "audioFiles.json"));
            TryQueueAudioPatch(Path.Combine(ModFolder, "audio", "audioFiles.json"));
        }

        private static void TryQueueAudioPatch(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return;

            try
            {
                AudioManager.QueueAudioPatchFile(path, 100);
            }
            catch
            {
                // Keep startup resilient even if optional audio patches fail.
            }
        }
    }
}
