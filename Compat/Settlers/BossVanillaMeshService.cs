using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Pandaros.Settlers
{
    internal static class BossVanillaMeshService
    {
        private const uint GlbMagic = 0x46546C67u;
        private const uint GlbVersion = 2u;
        private const uint JsonChunkType = 0x4E4F534Au;
        private const uint BinChunkType = 0x004E4942u;
        private const float DefaultScale = 1.18f;

        public static bool TryGetBossMeshPath(string fallbackVanillaMeshFileName, string customMeshFileName, out string meshPath)
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

                var folder = Path.Combine(BetterNecromancy.ModEntry.ModFolder, "meshes", "bosses", "generated", "vanilla");
                Directory.CreateDirectory(folder);

                var outputName = Path.GetFileNameWithoutExtension(customMeshFileName) + ".vanilla-boss.glb";
                var path = Path.Combine(folder, outputName);
                if (!File.Exists(path) || File.GetLastWriteTimeUtc(path) < File.GetLastWriteTimeUtc(templatePath))
                    WriteBossVariantMesh(templatePath, path, GetScaleForMesh(fallbackVanillaMeshFileName));

                meshPath = GetGameRootRelativePath(path);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static float GetScaleForMesh(string meshFileName)
        {
            if (string.Equals(meshFileName, "monster5.glb", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(meshFileName, "monster6.glb", StringComparison.OrdinalIgnoreCase))
            {
                return 1.12f;
            }

            return DefaultScale;
        }

        private static void WriteBossVariantMesh(string templatePath, string outputPath, float scale)
        {
            using (var stream = new FileStream(templatePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var reader = new BinaryReader(stream))
            {
                if (reader.ReadUInt32() != GlbMagic || reader.ReadUInt32() != GlbVersion)
                    throw new InvalidDataException("Invalid GLB header.");

                reader.ReadUInt32();
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

                ScaleMeshGeometry(json, binBytes, scale);
                TintMeshColors(json, binBytes);
                WriteGlb(outputPath, json, binBytes);
            }
        }

        private static void ScaleMeshGeometry(JObject json, byte[] binBytes, float scale)
        {
            foreach (var accessorIndex in GetMeshAttributeAccessors(json, "POSITION"))
                ScalePositionAccessor(json, accessorIndex, binBytes, scale);
        }

        private static void ScalePositionAccessor(JObject json, int accessorIndex, byte[] binBytes, float scale)
        {
            if (!(json["accessors"] is JArray accessors) ||
                !(json["bufferViews"] is JArray bufferViews) ||
                accessorIndex < 0 ||
                accessorIndex >= accessors.Count ||
                !(accessors[accessorIndex] is JObject accessor))
            {
                return;
            }

            if (accessor.Value<int?>("componentType") != 5126 ||
                !string.Equals(accessor.Value<string>("type"), "VEC3", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

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

                var x = BitConverter.ToSingle(binBytes, offset) * scale;
                var y = BitConverter.ToSingle(binBytes, offset + 4) * scale;
                var z = BitConverter.ToSingle(binBytes, offset + 8) * scale;

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

        private static void TintMeshColors(JObject json, byte[] binBytes)
        {
            foreach (var accessorIndex in GetMeshAttributeAccessors(json, "COLOR_0"))
                TintColorAccessor(json, accessorIndex, binBytes);
        }

        private static void TintColorAccessor(JObject json, int accessorIndex, byte[] binBytes)
        {
            if (!(json["accessors"] is JArray accessors) ||
                !(json["bufferViews"] is JArray bufferViews) ||
                accessorIndex < 0 ||
                accessorIndex >= accessors.Count ||
                !(accessors[accessorIndex] is JObject accessor))
            {
                return;
            }

            var componentType = accessor.Value<int?>("componentType") ?? 0;
            var type = accessor.Value<string>("type") ?? string.Empty;
            var componentCount = string.Equals(type, "VEC4", StringComparison.OrdinalIgnoreCase) ? 4 : 3;
            if (!string.Equals(type, "VEC3", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(type, "VEC4", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var bufferViewIndex = accessor.Value<int?>("bufferView") ?? -1;
            if (bufferViewIndex < 0 || bufferViewIndex >= bufferViews.Count || !(bufferViews[bufferViewIndex] is JObject bufferView))
                return;

            var count = accessor.Value<int?>("count") ?? 0;
            var normalized = accessor.Value<bool?>("normalized") ?? false;
            var componentSize = GetComponentByteSize(componentType);
            var elementSize = componentSize * componentCount;
            var stride = bufferView.Value<int?>("byteStride") ?? elementSize;
            var startOffset = (bufferView.Value<int?>("byteOffset") ?? 0) + (accessor.Value<int?>("byteOffset") ?? 0);

            for (var i = 0; i < count; i++)
            {
                var offset = startOffset + (i * stride);
                if (offset < 0 || offset + elementSize > binBytes.Length)
                    break;

                var r = ReadColorComponent(binBytes, offset, componentType, normalized);
                var g = ReadColorComponent(binBytes, offset + componentSize, componentType, normalized);
                var b = ReadColorComponent(binBytes, offset + (componentSize * 2), componentType, normalized);

                WriteColorComponent(binBytes, offset, componentType, normalized, Math.Min(1f, (r * 1.45f) + 0.18f));
                WriteColorComponent(binBytes, offset + componentSize, componentType, normalized, g * 0.58f);
                WriteColorComponent(binBytes, offset + (componentSize * 2), componentType, normalized, b * 0.58f);
            }
        }

        private static IEnumerable<int> GetMeshAttributeAccessors(JObject json, string attributeName)
        {
            if (!(json["meshes"] is JArray meshes))
                yield break;

            var seen = new HashSet<int>();
            foreach (var meshToken in meshes)
            {
                if (!(meshToken is JObject mesh) || !(mesh["primitives"] is JArray primitives))
                    continue;

                foreach (var primitiveToken in primitives)
                {
                    if (!(primitiveToken is JObject primitive))
                        continue;

                    var accessorIndex = primitive["attributes"]?[attributeName]?.Value<int?>() ?? -1;
                    if (accessorIndex >= 0 && seen.Add(accessorIndex))
                        yield return accessorIndex;
                }
            }
        }

        private static float ReadColorComponent(byte[] bytes, int offset, int componentType, bool normalized)
        {
            switch (componentType)
            {
                case 5126:
                    return BitConverter.ToSingle(bytes, offset);
                case 5123:
                    return normalized ? BitConverter.ToUInt16(bytes, offset) / 65535f : BitConverter.ToUInt16(bytes, offset) / 65535f;
                case 5121:
                    return normalized ? bytes[offset] / 255f : bytes[offset] / 255f;
                default:
                    return 1f;
            }
        }

        private static void WriteColorComponent(byte[] bytes, int offset, int componentType, bool normalized, float value)
        {
            value = Math.Max(0f, Math.Min(1f, value));

            switch (componentType)
            {
                case 5126:
                    WriteFloat(bytes, offset, value);
                    break;
                case 5123:
                    WriteUInt16(bytes, offset, (ushort)Math.Round(value * 65535f));
                    break;
                case 5121:
                    bytes[offset] = (byte)Math.Round(value * 255f);
                    break;
            }
        }

        private static int GetComponentByteSize(int componentType)
        {
            switch (componentType)
            {
                case 5121:
                    return 1;
                case 5123:
                    return 2;
                case 5126:
                    return 4;
                default:
                    return 4;
            }
        }

        private static void WriteFloat(byte[] bytes, int offset, float value)
        {
            var valueBytes = BitConverter.GetBytes(value);
            Buffer.BlockCopy(valueBytes, 0, bytes, offset, valueBytes.Length);
        }

        private static void WriteUInt16(byte[] bytes, int offset, ushort value)
        {
            var valueBytes = BitConverter.GetBytes(value);
            Buffer.BlockCopy(valueBytes, 0, bytes, offset, valueBytes.Length);
        }

        private static bool TryResolveVanillaMeshPath(string meshFileName, out string meshPath)
        {
            meshPath = null;

            var modFolder = BetterNecromancy.ModEntry.ModFolder;
            if (string.IsNullOrWhiteSpace(modFolder))
                return false;

            var current = new DirectoryInfo(Path.GetFullPath(modFolder));
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
