using System;
using System.IO;

namespace Pandaros.Settlers
{
    [ModLoader.ModManager]
    public static class GameLoader
    {
        public const string NAMESPACE = BetterNecromancy.ModEntry.Namespace;

        public static string ICON_PATH => BetterNecromancy.ModEntry.ModFolder + "/icons/";
        public static Colony StubColony => EnsureStubColony();
        public static string BossMeshFolder => Path.Combine(BetterNecromancy.ModEntry.ModFolder ?? string.Empty, "meshes", "bosses").Replace("\\", "/");

        public static string GetVanillaMonsterMesh(string meshFileName)
        {
            return "gamedata/meshes/" + meshFileName;
        }

        public static string GetBossMesh(string customMeshFileName, string fallbackVanillaMeshFileName)
        {
            // Custom boss visuals are rendered as a separate PLY overlay. Hide only the
            // native driver mesh, never load the custom visual through the NPC skin importer.
            if (BetterNecromancy.BossVisualSettings.CustomBossVisualProxyEnabled &&
                BossHiddenMeshService.TryGetHiddenBossMeshPath(fallbackVanillaMeshFileName, customMeshFileName, out var hiddenMeshPath))
            {
                return hiddenMeshPath;
            }

            if (BossVanillaMeshService.TryGetBossMeshPath(fallbackVanillaMeshFileName, customMeshFileName, out var bossMeshPath))
            {
                return bossMeshPath;
            }

            return GetVanillaMonsterMesh(fallbackVanillaMeshFileName);
        }

        public static bool HasBossVisualMesh(string customMeshFileName)
        {
            return TryGetBossVisualMeshPath(customMeshFileName, out _);
        }

        public static float GetBossRootOffset(string customMeshFileName, float defaultRootOffset)
        {
            return defaultRootOffset;
        }

        public static bool TryGetBossVisualMeshPath(string customMeshFileName, out string meshPath)
        {
            meshPath = null;

            if (string.IsNullOrWhiteSpace(customMeshFileName))
                return false;

            if (!string.IsNullOrEmpty(BetterNecromancy.ModEntry.ModFolder))
            {
                var modMeshPath = Path.Combine(BetterNecromancy.ModEntry.ModFolder, "meshes", "bosses", customMeshFileName);
                if (File.Exists(modMeshPath))
                {
                    meshPath = modMeshPath.Replace("\\", "/");
                    return true;
                }
            }

            return false;
        }

        private static bool TryGetBossNpcMeshReference(string customMeshFileName, out string meshReference)
        {
            meshReference = null;

            if (string.IsNullOrWhiteSpace(customMeshFileName))
                return false;

            if (!string.IsNullOrEmpty(BetterNecromancy.ModEntry.ModFolder))
            {
                var modMeshPath = Path.Combine(BetterNecromancy.ModEntry.ModFolder, "meshes", "bosses", customMeshFileName);
                if (File.Exists(modMeshPath))
                {
                    meshReference = GetGameRootRelativePath(modMeshPath);
                    return true;
                }
            }

            return false;
        }

        private static string GetGameRootRelativePath(string absolutePath)
        {
            var normalized = absolutePath.Replace("\\", "/");
            var gamedataIndex = normalized.IndexOf("/gamedata/", StringComparison.OrdinalIgnoreCase);
            if (gamedataIndex >= 0)
                return normalized.Substring(gamedataIndex + 1);

            return normalized;
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AfterSelectedWorld, NAMESPACE + ".GameLoader.AfterSelectedWorld")]
        public static void AfterSelectedWorld()
        {
            EnsureStubColony();
        }

        private static Colony EnsureStubColony()
        {
            if (_stubColony != null)
                return _stubColony;

            _stubColony = Colony.CreateStub(new ColonyID(-99998));
            _stubColony.Name = NAMESPACE + ".StubColony";
            return _stubColony;
        }

        private static Colony _stubColony;
    }
}
