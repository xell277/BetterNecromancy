using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Pandaros.Settlers
{
    internal static class BossHiddenMeshService
    {
        private const uint GlbMagic = 0x46546C67u;
        private const uint GlbVersion = 2u;
        private const uint JsonChunkType = 0x4E4F534Au;
        private const uint BinChunkType = 0x004E4942u;
        private const float HiddenScale = 0.0001f;
        private const float HiddenYOffset = 0f;

        public static bool TryGetHiddenBossMeshPath(string fallbackVanillaMeshFileName, string customMeshFileName, out string meshPath)
        {
            meshPath = null;

            if (string.IsNullOrWhiteSpace(customMeshFileName) ||
                string.IsNullOrWhiteSpace(fallbackVanillaMeshFileName) ||
                string.IsNullOrEmpty(BetterNecromancy.ModEntry.ModFolder))
            {
                return false;
            }

            try
            {
                if (!TryResolveVanillaMeshPath(fallbackVanillaMeshFileName, out var templatePath))
                    return false;

                var folder = Path.Combine(BetterNecromancy.ModEntry.ModFolder, "meshes", "bosses", "generated", "drivers");
                Directory.CreateDirectory(folder);

                var path = Path.Combine(folder, Path.GetFileNameWithoutExtension(customMeshFileName) + ".driver.glb");
                WriteHiddenDriverMesh(templatePath, path);

                meshPath = GetGameRootRelativePath(path);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryResolveVanillaMeshPath(string meshFileName, out string meshPath)
        {
            meshPath = null;

            var modFolder = BetterNecromancy.ModEntry.ModFolder;
            if (string.IsNullOrWhiteSpace(modFolder))
                return false;

            var normalizedModFolder = Path.GetFullPath(modFolder);
            var current = new DirectoryInfo(normalizedModFolder);

            while (current != null && !string.Equals(current.Name, "steamapps", StringComparison.OrdinalIgnoreCase))
                current = current.Parent;

            if (current == null)
                return false;

            var candidate = Path.Combine(current.FullName, "common", "Colony Survival", "gamedata", "meshes", meshFileName);
            if (!File.Exists(candidate))
                return false;

            meshPath = candidate;
            return true;
        }

        private static void WriteHiddenDriverMesh(string templatePath, string outputPath)
        {
            using (var stream = new FileStream(templatePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var reader = new BinaryReader(stream))
            {
                if (reader.ReadUInt32() != GlbMagic || reader.ReadUInt32() != GlbVersion)
                    throw new InvalidDataException("Invalid GLB header.");

                var totalLength = reader.ReadUInt32();
                var jsonChunkLength = reader.ReadUInt32();
                var jsonChunkType = reader.ReadUInt32();
                if (jsonChunkType != JsonChunkType)
                    throw new InvalidDataException("Missing GLB JSON chunk.");

                var jsonBytes = reader.ReadBytes((int)jsonChunkLength);
                var jsonText = Encoding.UTF8.GetString(jsonBytes).TrimEnd(' ', '\0', '\t', '\r', '\n');
                var json = JObject.Parse(jsonText);

                var binChunkLength = reader.ReadUInt32();
                var binChunkType = reader.ReadUInt32();
                if (binChunkType != BinChunkType)
                    throw new InvalidDataException("Missing GLB BIN chunk.");

                var binBytes = reader.ReadBytes((int)binChunkLength);
                if (binBytes.Length != binChunkLength)
                    throw new InvalidDataException("Invalid GLB BIN chunk length.");

                HideMeshGeometry(json, binBytes);
                HideSceneRoots(json);
                WriteGlb(outputPath, json, binBytes);
            }
        }

        private static void HideMeshGeometry(JObject json, byte[] binBytes)
        {
            if (!(json["meshes"] is JArray meshes) ||
                !(json["accessors"] is JArray accessors) ||
                !(json["bufferViews"] is JArray bufferViews))
            {
                return;
            }

            var hiddenAccessors = new HashSet<int>();

            foreach (var meshToken in meshes)
            {
                if (!(meshToken is JObject mesh) || !(mesh["primitives"] is JArray primitives))
                    continue;

                foreach (var primitiveToken in primitives)
                {
                    if (!(primitiveToken is JObject primitive))
                        continue;

                    var accessorIndex = primitive["attributes"]?["POSITION"]?.Value<int?>() ?? -1;
                    if (accessorIndex < 0 || accessorIndex >= accessors.Count || !hiddenAccessors.Add(accessorIndex))
                        continue;

                    HidePositionAccessor(accessors, bufferViews, accessorIndex, binBytes);
                }
            }
        }

        private static void HidePositionAccessor(JArray accessors, JArray bufferViews, int accessorIndex, byte[] binBytes)
        {
            if (!(accessors[accessorIndex] is JObject accessor))
                return;

            var componentType = accessor.Value<int?>("componentType") ?? 0;
            var type = accessor.Value<string>("type") ?? string.Empty;
            if (componentType != 5126 || !string.Equals(type, "VEC3", StringComparison.OrdinalIgnoreCase))
                return;

            var bufferViewIndex = accessor.Value<int?>("bufferView") ?? -1;
            if (bufferViewIndex < 0 || bufferViewIndex >= bufferViews.Count || !(bufferViews[bufferViewIndex] is JObject bufferView))
                return;

            var count = accessor.Value<int?>("count") ?? 0;
            var bufferByteOffset = bufferView.Value<int?>("byteOffset") ?? 0;
            var accessorByteOffset = accessor.Value<int?>("byteOffset") ?? 0;
            var stride = bufferView.Value<int?>("byteStride") ?? 12;
            var startOffset = bufferByteOffset + accessorByteOffset;

            var minX = float.MaxValue;
            var minY = float.MaxValue;
            var minZ = float.MaxValue;
            var maxX = float.MinValue;
            var maxY = float.MinValue;
            var maxZ = float.MinValue;

            for (var i = 0; i < count; i++)
            {
                var offset = startOffset + (i * stride);
                if (offset < 0 || offset + 12 > binBytes.Length)
                    break;

                var x = BitConverter.ToSingle(binBytes, offset) * HiddenScale;
                var y = (BitConverter.ToSingle(binBytes, offset + 4) * HiddenScale) + HiddenYOffset;
                var z = BitConverter.ToSingle(binBytes, offset + 8) * HiddenScale;

                WriteFloat(binBytes, offset, x);
                WriteFloat(binBytes, offset + 4, y);
                WriteFloat(binBytes, offset + 8, z);

                minX = Math.Min(minX, x);
                minY = Math.Min(minY, y);
                minZ = Math.Min(minZ, z);
                maxX = Math.Max(maxX, x);
                maxY = Math.Max(maxY, y);
                maxZ = Math.Max(maxZ, z);
            }

            if (minX != float.MaxValue)
            {
                accessor["min"] = new JArray(minX, minY, minZ);
                accessor["max"] = new JArray(maxX, maxY, maxZ);
            }
        }

        private static void WriteFloat(byte[] bytes, int offset, float value)
        {
            var valueBytes = BitConverter.GetBytes(value);
            Buffer.BlockCopy(valueBytes, 0, bytes, offset, valueBytes.Length);
        }

        private static void HideSceneRoots(JObject json)
        {
            var scenes = json["scenes"] as JArray;
            var nodes = json["nodes"] as JArray;
            if (scenes == null || nodes == null || scenes.Count == 0)
                return;

            var sceneIndex = json.Value<int?>("scene") ?? 0;
            if (sceneIndex < 0 || sceneIndex >= scenes.Count)
                sceneIndex = 0;

            if (!(scenes[sceneIndex]?["nodes"] is JArray rootNodes))
                return;

            for (var i = 0; i < rootNodes.Count; i++)
            {
                var nodeIndex = rootNodes[i]?.Value<int>() ?? -1;
                if (nodeIndex < 0 || nodeIndex >= nodes.Count || !(nodes[nodeIndex] is JObject node))
                    continue;

                var translation = node["translation"] as JArray ?? new JArray(0f, 0f, 0f);
                while (translation.Count < 3)
                    translation.Add(0f);

                translation[0] = 0f;
                translation[1] = HiddenYOffset;
                translation[2] = 0f;
                node["translation"] = translation;

                node["scale"] = new JArray(HiddenScale, HiddenScale, HiddenScale);
            }
        }

        private static void WriteGlb(string outputPath, JObject json, byte[] binBytes)
        {
            var jsonText = json.ToString(Newtonsoft.Json.Formatting.None);
            var jsonBytes = Encoding.UTF8.GetBytes(jsonText);
            var paddedJsonLength = Align4(jsonBytes.Length);
            var totalLength = 12 + 8 + paddedJsonLength + 8 + binBytes.Length;

            using (var stream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var writer = new BinaryWriter(stream, Encoding.UTF8))
            {
                writer.Write(GlbMagic);
                writer.Write(GlbVersion);
                writer.Write(totalLength);

                writer.Write(paddedJsonLength);
                writer.Write(JsonChunkType);
                writer.Write(jsonBytes);
                for (var i = jsonBytes.Length; i < paddedJsonLength; i++)
                    writer.Write((byte)0x20);

                writer.Write(binBytes.Length);
                writer.Write(BinChunkType);
                writer.Write(binBytes);
            }
        }

        private static int Align4(int value)
        {
            return (value + 3) & ~3;
        }

        private static string GetGameRootRelativePath(string absolutePath)
        {
            var normalized = absolutePath.Replace("\\", "/");
            var gamedataIndex = normalized.IndexOf("/gamedata/", StringComparison.OrdinalIgnoreCase);
            if (gamedataIndex >= 0)
                return normalized.Substring(gamedataIndex + 1);

            return normalized;
        }
    }
}
