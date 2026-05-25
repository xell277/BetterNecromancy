using Newtonsoft.Json.Linq;
using Shared;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace Pandaros.Settlers.Monsters
{
    internal static class BossMeshBakeService
    {
        private sealed class GlbContent
        {
            public JObject Json;
            public byte[] BinaryChunk;
        }

        private struct Vertex
        {
            public Vector3 Position;
            public Vector3 Normal;
            public Color32 Color;
        }

        private const float VisualColorMultiplier = 1.25f;
        private const int VisualColorBias = 28;

        public static bool TryPrepareVisualMesh(string sourceMeshPath, string meshName, out string preparedMeshPath, out ECachedFileType fileType)
        {
            preparedMeshPath = sourceMeshPath;
            fileType = ECachedFileType.MeshGLB;

            if (string.IsNullOrWhiteSpace(sourceMeshPath) || !File.Exists(sourceMeshPath))
                return false;

            if (string.Equals(Path.GetExtension(sourceMeshPath), ".ply", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var generatedFolder = GetGeneratedFolder("lit");
                    Directory.CreateDirectory(generatedFolder);

                    var litMeshPath = Path.Combine(generatedFolder, Path.GetFileNameWithoutExtension(meshName) + ".lit.ply");
                    if (!File.Exists(litMeshPath) || File.GetLastWriteTimeUtc(litMeshPath) < File.GetLastWriteTimeUtc(sourceMeshPath))
                        PrepareLitPly(sourceMeshPath, litMeshPath);

                    preparedMeshPath = litMeshPath.Replace("\\", "/");
                    fileType = ECachedFileType.MeshPly;
                    return true;
                }
                catch
                {
                    preparedMeshPath = sourceMeshPath.Replace("\\", "/");
                    fileType = ECachedFileType.MeshPly;
                    return true;
                }
            }

            if (!string.Equals(Path.GetExtension(sourceMeshPath), ".glb", StringComparison.OrdinalIgnoreCase))
            {
                fileType = ECachedFileType.MeshGLB;
                return true;
            }

            try
            {
                var generatedFolder = GetGeneratedFolder(string.Empty);
                Directory.CreateDirectory(generatedFolder);

                var bakedMeshPath = Path.Combine(generatedFolder, Path.GetFileNameWithoutExtension(meshName) + ".baked.ply");
                if (!File.Exists(bakedMeshPath) || File.GetLastWriteTimeUtc(bakedMeshPath) < File.GetLastWriteTimeUtc(sourceMeshPath))
                    BakeGlbToPly(sourceMeshPath, bakedMeshPath);

                preparedMeshPath = bakedMeshPath.Replace("\\", "/");
                fileType = ECachedFileType.MeshPly;
                return true;
            }
            catch (Exception)
            {
                preparedMeshPath = sourceMeshPath.Replace("\\", "/");
                fileType = ECachedFileType.MeshGLB;
                return true;
            }
        }

        private static void BakeGlbToPly(string sourceMeshPath, string outputPlyPath)
        {
            var glb = ReadGlb(sourceMeshPath);
            var vertices = new List<Vertex>();
            var faces = new List<int[]>();

            var scenes = glb.Json["scenes"] as JArray;
            var nodes = glb.Json["nodes"] as JArray;
            if (scenes == null || scenes.Count == 0 || nodes == null || nodes.Count == 0)
                throw new InvalidDataException("GLB has no scenes or nodes.");

            var sceneIndex = glb.Json.Value<int?>("scene") ?? 0;
            var rootScene = scenes[sceneIndex] as JObject;
            var rootNodes = rootScene?["nodes"] as JArray;
            if (rootNodes == null || rootNodes.Count == 0)
                throw new InvalidDataException("GLB scene has no root nodes.");

            for (var i = 0; i < rootNodes.Count; i++)
                ProcessNode(glb, nodes, rootNodes[i].Value<int>(), Matrix4x4.identity, vertices, faces);

            if (vertices.Count == 0 || faces.Count == 0)
                throw new InvalidDataException("GLB bake produced no geometry.");

            NormalizeVertices(vertices);
            BrightenVertexColors(vertices);
            WriteAsciiPly(outputPlyPath, vertices, faces);
        }

        private static void PrepareLitPly(string sourcePlyPath, string outputPlyPath)
        {
            var lines = File.ReadAllLines(sourcePlyPath);
            var vertexCount = 0;
            var headerEndIndex = -1;

            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (line.StartsWith("element vertex ", StringComparison.OrdinalIgnoreCase))
                {
                    int.TryParse(line.Substring("element vertex ".Length), NumberStyles.Integer, CultureInfo.InvariantCulture, out vertexCount);
                    continue;
                }

                if (string.Equals(line.Trim(), "end_header", StringComparison.OrdinalIgnoreCase))
                {
                    headerEndIndex = i;
                    break;
                }
            }

            if (headerEndIndex < 0 || vertexCount <= 0)
            {
                File.Copy(sourcePlyPath, outputPlyPath, true);
                return;
            }

            var firstVertexLine = headerEndIndex + 1;
            var lastVertexLine = Math.Min(lines.Length, firstVertexLine + vertexCount);
            for (var i = firstVertexLine; i < lastVertexLine; i++)
                lines[i] = BrightenPlyVertexLine(lines[i]);

            File.WriteAllLines(outputPlyPath, lines, Encoding.ASCII);
        }

        private static string BrightenPlyVertexLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return line;

            var parts = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 10)
                return line;

            for (var i = 6; i <= 8; i++)
            {
                if (int.TryParse(parts[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
                    parts[i] = BrightenColorValue(value).ToString(CultureInfo.InvariantCulture);
            }

            parts[3] = "0";
            parts[4] = "1";
            parts[5] = "0";

            return string.Join(" ", parts);
        }

        private static void BrightenVertexColors(List<Vertex> vertices)
        {
            for (var i = 0; i < vertices.Count; i++)
            {
                var vertex = vertices[i];
                vertex.Color = new Color32(
                    (byte)BrightenColorValue(vertex.Color.r),
                    (byte)BrightenColorValue(vertex.Color.g),
                    (byte)BrightenColorValue(vertex.Color.b),
                    vertex.Color.a);
                vertices[i] = vertex;
            }
        }

        private static int BrightenColorValue(int value)
        {
            return Mathf.Clamp(Mathf.RoundToInt((value * VisualColorMultiplier) + VisualColorBias), 0, 255);
        }

        private static string GetGeneratedFolder(string childFolder)
        {
            var folder = Path.Combine(BetterNecromancy.ModEntry.ModFolder ?? string.Empty, "meshes", "bosses", "generated");
            return string.IsNullOrEmpty(childFolder)
                ? folder
                : Path.Combine(folder, childFolder);
        }

        private static void ProcessNode(GlbContent glb, JArray nodes, int nodeIndex, Matrix4x4 parentMatrix, List<Vertex> vertices, List<int[]> faces)
        {
            var node = nodes[nodeIndex] as JObject;
            if (node == null)
                return;

            var localMatrix = BuildNodeMatrix(node);
            var worldMatrix = parentMatrix * localMatrix;

            if (node["mesh"] != null)
                AppendMesh(glb, node.Value<int>("mesh"), worldMatrix, vertices, faces);

            if (!(node["children"] is JArray childNodes))
                return;

            for (var i = 0; i < childNodes.Count; i++)
                ProcessNode(glb, nodes, childNodes[i].Value<int>(), worldMatrix, vertices, faces);
        }

        private static Matrix4x4 BuildNodeMatrix(JObject node)
        {
            if (node["matrix"] is JArray matrixArray && matrixArray.Count == 16)
            {
                return new Matrix4x4(
                    new Vector4(matrixArray[0].Value<float>(), matrixArray[1].Value<float>(), matrixArray[2].Value<float>(), matrixArray[3].Value<float>()),
                    new Vector4(matrixArray[4].Value<float>(), matrixArray[5].Value<float>(), matrixArray[6].Value<float>(), matrixArray[7].Value<float>()),
                    new Vector4(matrixArray[8].Value<float>(), matrixArray[9].Value<float>(), matrixArray[10].Value<float>(), matrixArray[11].Value<float>()),
                    new Vector4(matrixArray[12].Value<float>(), matrixArray[13].Value<float>(), matrixArray[14].Value<float>(), matrixArray[15].Value<float>()));
            }

            var translation = ReadVector3(node["translation"], Vector3.zero);
            var scale = ReadVector3(node["scale"], Vector3.one);
            var rotation = ReadQuaternion(node["rotation"]);
            return Matrix4x4.TRS(translation, rotation, scale);
        }

        private static void AppendMesh(GlbContent glb, int meshIndex, Matrix4x4 transformMatrix, List<Vertex> vertices, List<int[]> faces)
        {
            var meshes = glb.Json["meshes"] as JArray;
            if (meshes == null || meshIndex < 0 || meshIndex >= meshes.Count)
                return;

            var mesh = meshes[meshIndex] as JObject;
            if (!(mesh?["primitives"] is JArray primitives))
                return;

            for (var primitiveIndex = 0; primitiveIndex < primitives.Count; primitiveIndex++)
            {
                var primitive = primitives[primitiveIndex] as JObject;
                if (primitive == null)
                    continue;

                var mode = primitive.Value<int?>("mode") ?? 4;
                if (mode != 4)
                    continue;

                var attributes = primitive["attributes"] as JObject;
                if (attributes == null || attributes["POSITION"] == null)
                    continue;

                var positions = ReadVector3Accessor(glb, attributes.Value<int>("POSITION"));
                if (positions.Length == 0)
                    continue;

                var normals = attributes["NORMAL"] != null
                    ? ReadVector3Accessor(glb, attributes.Value<int>("NORMAL"))
                    : CreateDefaultNormals(positions.Length);

                var colors = attributes["COLOR_0"] != null
                    ? ReadColorAccessor(glb, attributes.Value<int>("COLOR_0"))
                    : CreateDefaultColors(positions.Length);

                var indices = primitive["indices"] != null
                    ? ReadIndexAccessor(glb, primitive.Value<int>("indices"))
                    : CreateSequentialIndices(positions.Length);

                var baseVertexIndex = vertices.Count;
                for (var i = 0; i < positions.Length; i++)
                {
                    var transformedPosition = transformMatrix.MultiplyPoint3x4(positions[i]);
                    var transformedNormal = transformMatrix.MultiplyVector(normals[Math.Min(i, normals.Length - 1)]);
                    if (transformedNormal.sqrMagnitude <= 0.0001f)
                        transformedNormal = Vector3.up;

                    vertices.Add(new Vertex
                    {
                        Position = transformedPosition,
                        Normal = transformedNormal.normalized,
                        Color = colors[Math.Min(i, colors.Length - 1)]
                    });
                }

                for (var i = 0; i + 2 < indices.Length; i += 3)
                {
                    faces.Add(new[]
                    {
                        baseVertexIndex + indices[i],
                        baseVertexIndex + indices[i + 1],
                        baseVertexIndex + indices[i + 2]
                    });
                }
            }
        }

        private static Vector3[] ReadVector3Accessor(GlbContent glb, int accessorIndex)
        {
            var accessor = GetAccessor(glb, accessorIndex);
            var values = ReadFloatAccessor(glb, accessorIndex, 3);
            var vectors = new Vector3[accessor.Count];

            for (var i = 0; i < accessor.Count; i++)
            {
                var offset = i * 3;
                vectors[i] = new Vector3(values[offset], values[offset + 1], values[offset + 2]);
            }

            return vectors;
        }

        private static Color32[] ReadColorAccessor(GlbContent glb, int accessorIndex)
        {
            var accessor = GetAccessor(glb, accessorIndex);
            var componentCount = GetTypeComponentCount(accessor.Type);
            var values = ReadFloatAccessor(glb, accessorIndex, componentCount);
            var colors = new Color32[accessor.Count];

            for (var i = 0; i < accessor.Count; i++)
            {
                var offset = i * componentCount;
                var r = ToByte(values[offset]);
                var g = ToByte(values[offset + 1]);
                var b = ToByte(values[offset + 2]);
                var a = componentCount >= 4 ? ToByte(values[offset + 3]) : (byte)255;
                colors[i] = new Color32(r, g, b, a);
            }

            return colors;
        }

        private static float[] ReadFloatAccessor(GlbContent glb, int accessorIndex, int expectedComponentCount)
        {
            var accessor = GetAccessor(glb, accessorIndex);
            var bufferView = GetBufferView(glb, accessor.BufferView);
            var componentCount = GetTypeComponentCount(accessor.Type);
            if (componentCount != expectedComponentCount && expectedComponentCount != componentCount)
                throw new InvalidDataException("Unexpected accessor component count.");

            var elementSize = componentCount * GetComponentByteSize(accessor.ComponentType);
            var stride = bufferView.ByteStride > 0 ? bufferView.ByteStride : elementSize;
            var start = bufferView.ByteOffset + accessor.ByteOffset;
            var output = new float[accessor.Count * componentCount];

            for (var i = 0; i < accessor.Count; i++)
            {
                var elementOffset = start + i * stride;
                for (var component = 0; component < componentCount; component++)
                {
                    var componentOffset = elementOffset + component * GetComponentByteSize(accessor.ComponentType);
                    output[i * componentCount + component] = ReadComponentAsFloat(glb.BinaryChunk, componentOffset, accessor.ComponentType, accessor.Normalized);
                }
            }

            return output;
        }

        private static int[] ReadIndexAccessor(GlbContent glb, int accessorIndex)
        {
            var accessor = GetAccessor(glb, accessorIndex);
            var bufferView = GetBufferView(glb, accessor.BufferView);
            var componentSize = GetComponentByteSize(accessor.ComponentType);
            var stride = bufferView.ByteStride > 0 ? bufferView.ByteStride : componentSize;
            var start = bufferView.ByteOffset + accessor.ByteOffset;
            var indices = new int[accessor.Count];

            for (var i = 0; i < accessor.Count; i++)
            {
                var offset = start + i * stride;
                indices[i] = ReadComponentAsInt(glb.BinaryChunk, offset, accessor.ComponentType);
            }

            return indices;
        }

        private static void NormalizeVertices(List<Vertex> vertices)
        {
            var min = vertices[0].Position;
            var max = vertices[0].Position;

            for (var i = 1; i < vertices.Count; i++)
            {
                min = Vector3.Min(min, vertices[i].Position);
                max = Vector3.Max(max, vertices[i].Position);
            }

            var centerX = (min.x + max.x) * 0.5f;
            var centerZ = (min.z + max.z) * 0.5f;
            var minY = min.y;

            for (var i = 0; i < vertices.Count; i++)
            {
                var vertex = vertices[i];
                vertex.Position = new Vector3(
                    vertex.Position.x - centerX,
                    vertex.Position.y - minY,
                    vertex.Position.z - centerZ);
                vertices[i] = vertex;
            }
        }

        private static void WriteAsciiPly(string outputPath, List<Vertex> vertices, List<int[]> faces)
        {
            using (var writer = new StreamWriter(outputPath, false, Encoding.ASCII))
            {
                writer.WriteLine("ply");
                writer.WriteLine("format ascii 1.0");
                writer.WriteLine("element vertex " + vertices.Count);
                writer.WriteLine("property float x");
                writer.WriteLine("property float y");
                writer.WriteLine("property float z");
                writer.WriteLine("property float nx");
                writer.WriteLine("property float ny");
                writer.WriteLine("property float nz");
                writer.WriteLine("property uchar red");
                writer.WriteLine("property uchar green");
                writer.WriteLine("property uchar blue");
                writer.WriteLine("property uchar alpha");
                writer.WriteLine("element face " + faces.Count);
                writer.WriteLine("property list uchar int vertex_indices");
                writer.WriteLine("end_header");

                for (var i = 0; i < vertices.Count; i++)
                {
                    var vertex = vertices[i];
                    writer.WriteLine(string.Format(
                        CultureInfo.InvariantCulture,
                        "{0} {1} {2} {3} {4} {5} {6} {7} {8} {9}",
                        vertex.Position.x,
                        vertex.Position.y,
                        vertex.Position.z,
                        vertex.Normal.x,
                        vertex.Normal.y,
                        vertex.Normal.z,
                        vertex.Color.r,
                        vertex.Color.g,
                        vertex.Color.b,
                        vertex.Color.a));
                }

                for (var i = 0; i < faces.Count; i++)
                {
                    var face = faces[i];
                    writer.WriteLine(string.Format(
                        CultureInfo.InvariantCulture,
                        "3 {0} {1} {2}",
                        face[0],
                        face[1],
                        face[2]));
                }
            }
        }

        private static GlbContent ReadGlb(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var reader = new BinaryReader(stream))
            {
                var magic = reader.ReadUInt32();
                var version = reader.ReadUInt32();
                reader.ReadUInt32();

                if (magic != 0x46546C67 || version != 2)
                    throw new InvalidDataException("Not a glTF 2.0 binary file.");

                string jsonText = null;
                byte[] binaryChunk = null;

                while (reader.BaseStream.Position < reader.BaseStream.Length)
                {
                    var chunkLength = reader.ReadUInt32();
                    var chunkType = reader.ReadUInt32();
                    var chunkData = reader.ReadBytes((int)chunkLength);

                    if (chunkType == 0x4E4F534A)
                        jsonText = Encoding.UTF8.GetString(chunkData).TrimEnd('\0', ' ', '\r', '\n', '\t');
                    else if (chunkType == 0x004E4942)
                        binaryChunk = chunkData;
                }

                if (string.IsNullOrEmpty(jsonText) || binaryChunk == null)
                    throw new InvalidDataException("GLB missing JSON or BIN chunk.");

                return new GlbContent
                {
                    Json = JObject.Parse(jsonText),
                    BinaryChunk = binaryChunk
                };
            }
        }

        private static AccessorInfo GetAccessor(GlbContent glb, int accessorIndex)
        {
            var accessors = glb.Json["accessors"] as JArray;
            if (accessors == null || accessorIndex < 0 || accessorIndex >= accessors.Count)
                throw new InvalidDataException("Invalid accessor index " + accessorIndex + ".");

            var accessor = accessors[accessorIndex] as JObject;
            return new AccessorInfo
            {
                BufferView = accessor.Value<int>("bufferView"),
                ByteOffset = accessor.Value<int?>("byteOffset") ?? 0,
                Count = accessor.Value<int>("count"),
                ComponentType = accessor.Value<int>("componentType"),
                Type = accessor.Value<string>("type"),
                Normalized = accessor.Value<bool?>("normalized") ?? false
            };
        }

        private static BufferViewInfo GetBufferView(GlbContent glb, int bufferViewIndex)
        {
            var bufferViews = glb.Json["bufferViews"] as JArray;
            if (bufferViews == null || bufferViewIndex < 0 || bufferViewIndex >= bufferViews.Count)
                throw new InvalidDataException("Invalid bufferView index " + bufferViewIndex + ".");

            var bufferView = bufferViews[bufferViewIndex] as JObject;
            return new BufferViewInfo
            {
                ByteOffset = bufferView.Value<int?>("byteOffset") ?? 0,
                ByteLength = bufferView.Value<int>("byteLength"),
                ByteStride = bufferView.Value<int?>("byteStride") ?? 0
            };
        }

        private static float ReadComponentAsFloat(byte[] data, int offset, int componentType, bool normalized)
        {
            switch (componentType)
            {
                case 5120:
                {
                    var value = unchecked((sbyte)data[offset]);
                    return normalized ? Mathf.Max(value / 127f, -1f) : value;
                }
                case 5121:
                {
                    var value = data[offset];
                    return normalized ? value / 255f : value;
                }
                case 5122:
                {
                    var value = BitConverter.ToInt16(data, offset);
                    return normalized ? Mathf.Max(value / 32767f, -1f) : value;
                }
                case 5123:
                {
                    var value = BitConverter.ToUInt16(data, offset);
                    return normalized ? value / 65535f : value;
                }
                case 5125:
                    return BitConverter.ToUInt32(data, offset);
                case 5126:
                    return BitConverter.ToSingle(data, offset);
                default:
                    throw new InvalidDataException("Unsupported component type " + componentType + ".");
            }
        }

        private static int ReadComponentAsInt(byte[] data, int offset, int componentType)
        {
            switch (componentType)
            {
                case 5121:
                    return data[offset];
                case 5123:
                    return BitConverter.ToUInt16(data, offset);
                case 5125:
                    return (int)BitConverter.ToUInt32(data, offset);
                default:
                    throw new InvalidDataException("Unsupported index component type " + componentType + ".");
            }
        }

        private static int GetComponentByteSize(int componentType)
        {
            switch (componentType)
            {
                case 5120:
                case 5121:
                    return 1;
                case 5122:
                case 5123:
                    return 2;
                case 5125:
                case 5126:
                    return 4;
                default:
                    throw new InvalidDataException("Unsupported component type " + componentType + ".");
            }
        }

        private static int GetTypeComponentCount(string type)
        {
            switch (type)
            {
                case "SCALAR":
                    return 1;
                case "VEC2":
                    return 2;
                case "VEC3":
                    return 3;
                case "VEC4":
                    return 4;
                case "MAT4":
                    return 16;
                default:
                    throw new InvalidDataException("Unsupported accessor type " + type + ".");
            }
        }

        private static Vector3 ReadVector3(JToken token, Vector3 fallback)
        {
            var array = token as JArray;
            if (array == null || array.Count < 3)
                return fallback;

            return new Vector3(array[0].Value<float>(), array[1].Value<float>(), array[2].Value<float>());
        }

        private static Quaternion ReadQuaternion(JToken token)
        {
            var array = token as JArray;
            if (array == null || array.Count < 4)
                return Quaternion.identity;

            return new Quaternion(array[0].Value<float>(), array[1].Value<float>(), array[2].Value<float>(), array[3].Value<float>());
        }

        private static byte ToByte(float value)
        {
            return (byte)Mathf.Clamp(Mathf.RoundToInt(value * 255f), 0, 255);
        }

        private static Vector3[] CreateDefaultNormals(int count)
        {
            var normals = new Vector3[count];
            for (var i = 0; i < count; i++)
                normals[i] = Vector3.up;

            return normals;
        }

        private static Color32[] CreateDefaultColors(int count)
        {
            var colors = new Color32[count];
            for (var i = 0; i < count; i++)
                colors[i] = new Color32(255, 255, 255, 255);

            return colors;
        }

        private static int[] CreateSequentialIndices(int vertexCount)
        {
            var indices = new int[vertexCount];
            for (var i = 0; i < vertexCount; i++)
                indices[i] = i;

            return indices;
        }

        private sealed class AccessorInfo
        {
            public int BufferView;
            public int ByteOffset;
            public int Count;
            public int ComponentType;
            public string Type;
            public bool Normalized;
        }

        private sealed class BufferViewInfo
        {
            public int ByteOffset;
            public int ByteLength;
            public int ByteStride;
        }
    }
}
