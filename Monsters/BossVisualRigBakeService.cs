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
    internal static class BossVisualRigBakeService
    {
        internal sealed class VisualRigPart
        {
            public string Name;
            public int NodeIndex;
            public bool IsRenderable;
            public string MeshPath;
            public ECachedFileType MeshFileType;
            public int ParentPartIndex;
            public Vector3 LocalPosition;
            public Quaternion LocalRotation;
            public Vector3 LocalScale = Vector3.one;
        }

        internal sealed class VisualRigAnimationClip
        {
            public string Name;
            public float DurationSeconds;
            public Dictionary<int, VisualRigAnimationTrack> Tracks = new Dictionary<int, VisualRigAnimationTrack>();
        }

        internal sealed class VisualRigAnimationTrack
        {
            public float[] TranslationTimes;
            public Vector3[] TranslationValues;
            public float[] RotationTimes;
            public Quaternion[] RotationValues;
            public float[] ScaleTimes;
            public Vector3[] ScaleValues;
        }

        internal sealed class VisualRigBounds
        {
            public Vector3 Center;
            public Vector3 Size;
        }

        internal sealed class GlbFeatures
        {
            public int AnimationCount;
            public int SkinCount;
            public bool ShouldUseRawVisual => AnimationCount > 0 || SkinCount > 0;
        }

        private sealed class GlbContent
        {
            public JObject Json;
            public byte[] BinaryChunk;
        }

        private sealed class AccessorInfo
        {
            public int BufferView;
            public int ByteOffset;
            public int ComponentType;
            public int Count;
            public bool Normalized;
            public string Type;
        }

        private sealed class BufferViewInfo
        {
            public int ByteOffset;
            public int ByteLength;
            public int ByteStride;
        }

        private sealed class SkinInfo
        {
            public int[] JointNodeIndices = new int[0];
            public Matrix4x4[] InverseBindMatrices = new Matrix4x4[0];
        }

        private struct Vertex
        {
            public Vector3 Position;
            public Vector3 Normal;
            public Color32 Color;
        }

        public static bool TryPrepareVisualRig(string sourceMeshPath, string meshName, out List<VisualRigPart> parts, out List<VisualRigAnimationClip> clips, out VisualRigBounds bounds)
        {
            parts = null;
            clips = null;
            bounds = null;

            if (string.IsNullOrWhiteSpace(sourceMeshPath) ||
                !File.Exists(sourceMeshPath) ||
                !string.Equals(Path.GetExtension(sourceMeshPath), ".glb", StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrEmpty(BetterNecromancy.ModEntry.ModFolder))
            {
                return false;
            }

            try
            {
                var glb = ReadGlb(sourceMeshPath);
                var generatedFolder = Path.Combine(
                    BetterNecromancy.ModEntry.ModFolder,
                    "meshes",
                    "bosses",
                    "generated",
                    "rigs",
                    Path.GetFileNameWithoutExtension(meshName));
                Directory.CreateDirectory(generatedFolder);
                var sourceMeshWriteTimeUtc = File.GetLastWriteTimeUtc(sourceMeshPath);

                var builtParts = new List<VisualRigPart>();
                var nodeToPartIndex = new Dictionary<int, int>();
                var scenes = glb.Json["scenes"] as JArray;
                var nodes = glb.Json["nodes"] as JArray;
                if (scenes == null || scenes.Count == 0 || nodes == null || nodes.Count == 0)
                    return false;

                var sceneIndex = glb.Json.Value<int?>("scene") ?? 0;
                var rootScene = scenes[sceneIndex] as JObject;
                var rootNodes = rootScene?["nodes"] as JArray;
                if (rootNodes == null || rootNodes.Count == 0)
                    return false;

                var traversalOrder = new List<int>();
                for (var i = 0; i < rootNodes.Count; i++)
                    BuildRigHierarchy(nodes, rootNodes[i].Value<int>(), -1, builtParts, nodeToPartIndex, traversalOrder);

                if (builtParts.Count == 0)
                    return false;

                var skins = ReadSkinInfos(glb);
                AppendRenderableParts(glb, nodes, traversalOrder, nodeToPartIndex, builtParts, generatedFolder, meshName, sourceMeshWriteTimeUtc, skins, out bounds);

                if (builtParts.TrueForAll(p => !p.IsRenderable))
                    return false;

                ApplyBossPartOverrides(meshName, builtParts);
                parts = builtParts;
                clips = ReadAnimations(glb, nodeToPartIndex);
                return true;
            }
            catch (Exception)
            {
                bounds = null;
                return false;
            }
        }

        public static bool TryReadGlbFeatures(string sourceMeshPath, out GlbFeatures features)
        {
            features = null;

            if (string.IsNullOrWhiteSpace(sourceMeshPath) ||
                !File.Exists(sourceMeshPath) ||
                !string.Equals(Path.GetExtension(sourceMeshPath), ".glb", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            try
            {
                var glb = ReadGlb(sourceMeshPath);
                features = new GlbFeatures
                {
                    AnimationCount = (glb.Json["animations"] as JArray)?.Count ?? 0,
                    SkinCount = (glb.Json["skins"] as JArray)?.Count ?? 0
                };
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void ApplyBossPartOverrides(string meshName, List<VisualRigPart> parts)
        {
            if (!string.Equals(Path.GetFileNameWithoutExtension(meshName), "ZombieQueen", StringComparison.OrdinalIgnoreCase) ||
                parts == null)
                return;

            for (var i = 0; i < parts.Count; i++)
            {
                var part = parts[i];
                if (part == null || string.IsNullOrEmpty(part.Name))
                    continue;

                if (string.Equals(part.Name, "Torso_Node", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(part.Name, "UpperArmL_Node", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(part.Name, "UpperArmR_Node", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(part.Name, "LowerArmL_Node", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(part.Name, "LowerArmR_Node", StringComparison.OrdinalIgnoreCase))
                {
                    part.LocalRotation = part.LocalRotation * Quaternion.Euler(0f, 180f, 0f);
                }
            }
        }

        private static void BuildRigHierarchy(
            JArray nodes,
            int nodeIndex,
            int parentPartIndex,
            List<VisualRigPart> parts,
            Dictionary<int, int> nodeToPartIndex,
            List<int> traversalOrder)
        {
            var node = nodes[nodeIndex] as JObject;
            if (node == null)
                return;

            var localMatrix = BuildNodeMatrix(node);
            var (localPosition, localRotation, localScale) = DecomposeMatrix(localMatrix);
            var currentPartIndex = parts.Count;
            parts.Add(new VisualRigPart
            {
                Name = node.Value<string>("name") ?? ("Node" + nodeIndex),
                NodeIndex = nodeIndex,
                IsRenderable = false,
                ParentPartIndex = parentPartIndex,
                LocalPosition = localPosition,
                LocalRotation = localRotation,
                LocalScale = localScale
            });
            nodeToPartIndex[nodeIndex] = currentPartIndex;
            traversalOrder.Add(nodeIndex);

            if (!(node["children"] is JArray childNodes))
                return;

            for (var i = 0; i < childNodes.Count; i++)
            {
                BuildRigHierarchy(
                    nodes,
                    childNodes[i].Value<int>(),
                    currentPartIndex,
                    parts,
                    nodeToPartIndex,
                    traversalOrder);
            }
        }

        private static void AppendRenderableParts(
            GlbContent glb,
            JArray nodes,
            List<int> traversalOrder,
            Dictionary<int, int> nodeToPartIndex,
            List<VisualRigPart> parts,
            string outputFolder,
            string meshName,
            DateTime sourceMeshWriteTimeUtc,
            Dictionary<int, SkinInfo> skins,
            out VisualRigBounds bounds)
        {
            var hasBounds = false;
            var boundsMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            var boundsMax = new Vector3(float.MinValue, float.MinValue, float.MinValue);

            for (var i = 0; i < traversalOrder.Count; i++)
            {
                var nodeIndex = traversalOrder[i];
                var node = nodes[nodeIndex] as JObject;
                if (node == null || node["mesh"] == null)
                    continue;

                var meshIndex = node.Value<int>("mesh");
                var nodeName = node.Value<string>("name") ?? ("Node" + nodeIndex);
                var fileName = SanitizeFileName(Path.GetFileNameWithoutExtension(meshName) + "_" + nodeIndex + "_" + nodeName + "_Render") + ".ply";
                var outputPath = Path.Combine(outputFolder, fileName);
                var localMatrix = BuildNodeMatrix(node);
                var (_, _, localScale) = DecomposeMatrix(localMatrix);
                var parentPartIndex = nodeToPartIndex[nodeIndex];
                var meshBakeMatrix = Matrix4x4.Scale(localScale);

                if (node["skin"] != null &&
                    TryGetDominantSkinBinding(glb, meshIndex, nodeName, nodes, node.Value<int>("skin"), skins, nodeToPartIndex, out var jointParentPartIndex, out var bindMatrix))
                {
                    parentPartIndex = jointParentPartIndex;
                    if (skins.TryGetValue(node.Value<int>("skin"), out var skinInfo) &&
                        TryResolveBossJointParentOverride(meshName, nodeName, nodes, skinInfo, nodeToPartIndex, out var overrideParentPartIndex, out var overrideBindMatrix))
                    {
                        parentPartIndex = overrideParentPartIndex;
                        bindMatrix = overrideBindMatrix;
                    }

                    meshBakeMatrix = bindMatrix * Matrix4x4.Scale(localScale);
                }

                if (!File.Exists(outputPath) || File.GetLastWriteTimeUtc(outputPath) < sourceMeshWriteTimeUtc)
                {
                    var vertices = new List<Vertex>();
                    var faces = new List<int[]>();
                    AppendMesh(glb, meshIndex, meshBakeMatrix, vertices, faces);
                    if (vertices.Count > 0 && faces.Count > 0)
                        WriteAsciiPly(outputPath, vertices, faces);
                }

                if (!File.Exists(outputPath))
                    continue;

                var renderBoundsMatrix = BuildPartWorldMatrix(parts, parentPartIndex);
                var renderVertices = new List<Vertex>();
                var renderFaces = new List<int[]>();
                AppendMesh(glb, meshIndex, meshBakeMatrix, renderVertices, renderFaces);
                for (var vertexIndex = 0; vertexIndex < renderVertices.Count; vertexIndex++)
                {
                    var worldPosition = renderBoundsMatrix.MultiplyPoint3x4(renderVertices[vertexIndex].Position);
                    ExpandBounds(ref hasBounds, ref boundsMin, ref boundsMax, worldPosition);
                }

                parts.Add(new VisualRigPart
                {
                    Name = nodeName + "_Render",
                    NodeIndex = nodeIndex,
                    IsRenderable = true,
                    MeshPath = outputPath.Replace("\\", "/"),
                    MeshFileType = ECachedFileType.MeshPly,
                    ParentPartIndex = parentPartIndex,
                    LocalPosition = Vector3.zero,
                    LocalRotation = Quaternion.identity,
                    LocalScale = Vector3.one
                });
            }

            bounds = hasBounds
                ? new VisualRigBounds
                {
                    Center = (boundsMin + boundsMax) * 0.5f,
                    Size = boundsMax - boundsMin
                }
                : new VisualRigBounds
                {
                    Center = new Vector3(0f, 1.5f, 0f),
                    Size = new Vector3(1.5f, 3f, 1.5f)
                };
        }

        private static bool TryResolveBossJointParentOverride(
            string meshName,
            string nodeName,
            JArray nodes,
            SkinInfo skinInfo,
            Dictionary<int, int> nodeToPartIndex,
            out int parentPartIndex,
            out Matrix4x4 bindMatrix)
        {
            parentPartIndex = -1;
            bindMatrix = Matrix4x4.identity;

            if (!string.Equals(Path.GetFileNameWithoutExtension(meshName), "ZombieQueen", StringComparison.OrdinalIgnoreCase))
                return false;

            string targetBoneName = null;
            switch (nodeName)
            {
                case "Head":
                    targetBoneName = "Torso";
                    break;
                case "Hips":
                    targetBoneName = "Bone.006.Top";
                    break;
                case "Middle":
                    targetBoneName = "Bone.006.Top";
                    break;
                case "UpperArmL":
                    targetBoneName = "ShoulderL";
                    break;
                case "UpperArmR":
                    targetBoneName = "ShoulderR";
                    break;
                case "HandL":
                    targetBoneName = "LowerArmL";
                    break;
                case "HandR":
                    targetBoneName = "LowerArmR";
                    break;
                case "UpperLegL":
                    targetBoneName = "HipsL";
                    break;
                case "UpperLegR":
                    targetBoneName = "HipsR";
                    break;
                case "FootL":
                    targetBoneName = "LowerLegL";
                    break;
                case "FootR":
                    targetBoneName = "LowerLegR";
                    break;
            }

            if (string.IsNullOrEmpty(targetBoneName))
                return false;

            return TryFindJointBindingByName(targetBoneName, nodes, skinInfo, nodeToPartIndex, out parentPartIndex, out bindMatrix);
        }

        private static bool TryFindJointBindingByName(
            string targetBoneName,
            JArray nodes,
            SkinInfo skinInfo,
            Dictionary<int, int> nodeToPartIndex,
            out int parentPartIndex,
            out Matrix4x4 bindMatrix)
        {
            parentPartIndex = -1;
            bindMatrix = Matrix4x4.identity;

            if (string.IsNullOrWhiteSpace(targetBoneName) ||
                nodes == null ||
                skinInfo?.JointNodeIndices == null)
            {
                return false;
            }

            for (var jointIndex = 0; jointIndex < skinInfo.JointNodeIndices.Length; jointIndex++)
            {
                var nodeIndex = skinInfo.JointNodeIndices[jointIndex];
                if (nodeIndex < 0 || nodeIndex >= nodes.Count)
                    continue;

                var node = nodes[nodeIndex] as JObject;
                if (node == null ||
                    !string.Equals(node.Value<string>("name"), targetBoneName, StringComparison.OrdinalIgnoreCase) ||
                    !nodeToPartIndex.TryGetValue(nodeIndex, out parentPartIndex))
                {
                    continue;
                }

                if (skinInfo.InverseBindMatrices != null && jointIndex < skinInfo.InverseBindMatrices.Length)
                    bindMatrix = skinInfo.InverseBindMatrices[jointIndex];

                return true;
            }

            parentPartIndex = -1;
            bindMatrix = Matrix4x4.identity;
            return false;
        }

        private static Matrix4x4 BuildPartWorldMatrix(List<VisualRigPart> parts, int partIndex)
        {
            if (parts == null || partIndex < 0 || partIndex >= parts.Count)
                return Matrix4x4.identity;

            var chain = new Stack<int>();
            var currentIndex = partIndex;
            while (currentIndex >= 0 && currentIndex < parts.Count)
            {
                chain.Push(currentIndex);
                currentIndex = parts[currentIndex].ParentPartIndex;
            }

            var matrix = Matrix4x4.identity;
            while (chain.Count > 0)
            {
                var part = parts[chain.Pop()];
                matrix = matrix * Matrix4x4.TRS(part.LocalPosition, part.LocalRotation, part.LocalScale);
            }

            return matrix;
        }

        private static void ExpandBounds(ref bool hasBounds, ref Vector3 min, ref Vector3 max, Vector3 point)
        {
            if (!hasBounds)
            {
                min = point;
                max = point;
                hasBounds = true;
                return;
            }

            min = Vector3.Min(min, point);
            max = Vector3.Max(max, point);
        }

        private static List<VisualRigAnimationClip> ReadAnimations(GlbContent glb, Dictionary<int, int> nodeToPartIndex)
        {
            var result = new List<VisualRigAnimationClip>();
            var animations = glb.Json["animations"] as JArray;
            if (animations == null || animations.Count == 0 || nodeToPartIndex == null || nodeToPartIndex.Count == 0)
                return result;

            for (var animationIndex = 0; animationIndex < animations.Count; animationIndex++)
            {
                var animation = animations[animationIndex] as JObject;
                if (animation == null)
                    continue;

                var samplers = animation["samplers"] as JArray;
                var channels = animation["channels"] as JArray;
                if (samplers == null || channels == null)
                    continue;

                var clip = new VisualRigAnimationClip
                {
                    Name = animation.Value<string>("name") ?? ("Clip" + animationIndex),
                    DurationSeconds = 0f
                };

                for (var channelIndex = 0; channelIndex < channels.Count; channelIndex++)
                {
                    var channel = channels[channelIndex] as JObject;
                    var target = channel?["target"] as JObject;
                    if (channel == null || target == null)
                        continue;

                    var nodeIndex = target.Value<int?>("node");
                    var path = target.Value<string>("path");
                    var samplerIndex = channel.Value<int?>("sampler");
                    if (!nodeIndex.HasValue ||
                        string.IsNullOrEmpty(path) ||
                        !samplerIndex.HasValue ||
                        samplerIndex.Value < 0 ||
                        samplerIndex.Value >= samplers.Count ||
                        !nodeToPartIndex.TryGetValue(nodeIndex.Value, out var partIndex))
                    {
                        continue;
                    }

                    var sampler = samplers[samplerIndex.Value] as JObject;
                    if (sampler == null)
                        continue;

                    var inputAccessorIndex = sampler.Value<int?>("input");
                    var outputAccessorIndex = sampler.Value<int?>("output");
                    if (!inputAccessorIndex.HasValue || !outputAccessorIndex.HasValue)
                        continue;

                    var times = ReadFloatAccessor(glb, inputAccessorIndex.Value);
                    if (times == null || times.Length == 0)
                        continue;

                    clip.DurationSeconds = Mathf.Max(clip.DurationSeconds, times[times.Length - 1]);

                    if (!clip.Tracks.TryGetValue(partIndex, out var track))
                    {
                        track = new VisualRigAnimationTrack();
                        clip.Tracks[partIndex] = track;
                    }

                    switch (path)
                    {
                        case "translation":
                            track.TranslationTimes = times;
                            track.TranslationValues = ReadVector3Accessor(glb, outputAccessorIndex.Value);
                            break;
                        case "rotation":
                            track.RotationTimes = times;
                            track.RotationValues = ReadQuaternionAccessor(glb, outputAccessorIndex.Value);
                            break;
                        case "scale":
                            track.ScaleTimes = times;
                            track.ScaleValues = ReadVector3Accessor(glb, outputAccessorIndex.Value);
                            break;
                    }
                }

                if (clip.Tracks.Count > 0)
                    result.Add(clip);
            }

            AddSyntheticWalkClipIfNeeded(result);
            return result;
        }

        private static Dictionary<int, SkinInfo> ReadSkinInfos(GlbContent glb)
        {
            var result = new Dictionary<int, SkinInfo>();
            var skins = glb.Json["skins"] as JArray;
            if (skins == null || skins.Count == 0)
                return result;

            for (var i = 0; i < skins.Count; i++)
            {
                var skin = skins[i] as JObject;
                if (skin == null)
                    continue;

                var jointsToken = skin["joints"] as JArray;
                if (jointsToken == null || jointsToken.Count == 0)
                    continue;

                var jointNodeIndices = new int[jointsToken.Count];
                for (var jointIndex = 0; jointIndex < jointsToken.Count; jointIndex++)
                    jointNodeIndices[jointIndex] = jointsToken[jointIndex].Value<int>();

                var inverseBindMatrices = new Matrix4x4[jointNodeIndices.Length];
                for (var jointIndex = 0; jointIndex < inverseBindMatrices.Length; jointIndex++)
                    inverseBindMatrices[jointIndex] = Matrix4x4.identity;

                var inverseBindAccessorIndex = skin.Value<int?>("inverseBindMatrices");
                if (inverseBindAccessorIndex.HasValue)
                {
                    var matrices = ReadMatrix4x4Accessor(glb, inverseBindAccessorIndex.Value);
                    for (var jointIndex = 0; jointIndex < inverseBindMatrices.Length && jointIndex < matrices.Length; jointIndex++)
                        inverseBindMatrices[jointIndex] = matrices[jointIndex];
                }

                result[i] = new SkinInfo
                {
                    JointNodeIndices = jointNodeIndices,
                    InverseBindMatrices = inverseBindMatrices
                };
            }

            return result;
        }

        private static bool TryGetDominantSkinBinding(
            GlbContent glb,
            int meshIndex,
            string nodeName,
            JArray nodes,
            int skinIndex,
            Dictionary<int, SkinInfo> skins,
            Dictionary<int, int> nodeToPartIndex,
            out int jointParentPartIndex,
            out Matrix4x4 bindMatrix)
        {
            jointParentPartIndex = -1;
            bindMatrix = Matrix4x4.identity;

            if (skins == null ||
                !skins.TryGetValue(skinIndex, out var skinInfo) ||
                skinInfo == null ||
                skinInfo.JointNodeIndices == null ||
                skinInfo.JointNodeIndices.Length == 0)
            {
                return false;
            }

            if (TryGetNamedSkinBinding(nodeName, nodes, skinInfo, nodeToPartIndex, out jointParentPartIndex, out bindMatrix))
                return true;

            var dominantJointIndex = GetDominantJointIndexForMesh(glb, meshIndex);
            if (dominantJointIndex < 0 || dominantJointIndex >= skinInfo.JointNodeIndices.Length)
                return false;

            var jointNodeIndex = skinInfo.JointNodeIndices[dominantJointIndex];
            if (!nodeToPartIndex.TryGetValue(jointNodeIndex, out jointParentPartIndex))
                return false;

            if (skinInfo.InverseBindMatrices != null && dominantJointIndex < skinInfo.InverseBindMatrices.Length)
                bindMatrix = skinInfo.InverseBindMatrices[dominantJointIndex];

            return true;
        }

        private static bool TryGetNamedSkinBinding(
            string nodeName,
            JArray nodes,
            SkinInfo skinInfo,
            Dictionary<int, int> nodeToPartIndex,
            out int jointParentPartIndex,
            out Matrix4x4 bindMatrix)
        {
            jointParentPartIndex = -1;
            bindMatrix = Matrix4x4.identity;

            var canonicalTargetName = CanonicalizeJointName(nodeName);
            if (string.IsNullOrEmpty(canonicalTargetName) || nodes == null)
                return false;

            var preferredJointNames = GetPreferredJointNames(canonicalTargetName);
            for (var preferredNameIndex = 0; preferredNameIndex < preferredJointNames.Length; preferredNameIndex++)
            {
                var preferredJointName = preferredJointNames[preferredNameIndex];
                for (var jointIndex = 0; jointIndex < skinInfo.JointNodeIndices.Length; jointIndex++)
                {
                    var jointNodeIndex = skinInfo.JointNodeIndices[jointIndex];
                    if (jointNodeIndex < 0 || jointNodeIndex >= nodes.Count)
                        continue;

                    var jointNode = nodes[jointNodeIndex] as JObject;
                    var jointName = jointNode?.Value<string>("name");
                    if (!IsMatchingJointName(preferredJointName, jointName))
                        continue;

                    if (!nodeToPartIndex.TryGetValue(jointNodeIndex, out jointParentPartIndex))
                        continue;

                    if (skinInfo.InverseBindMatrices != null && jointIndex < skinInfo.InverseBindMatrices.Length)
                        bindMatrix = skinInfo.InverseBindMatrices[jointIndex];

                    return true;
                }
            }

            return false;
        }

        private static string[] GetPreferredJointNames(string canonicalTargetName)
        {
            switch (canonicalTargetName)
            {
                case "UPPERARML":
                    return new[] { "UPPERARML", "SHOULDERL" };
                case "UPPERARMR":
                    return new[] { "UPPERARMR", "SHOULDERR" };
                case "HANDL":
                    return new[] { "HANDL", "LOWERARML" };
                case "HANDR":
                    return new[] { "HANDR", "LOWERARMR" };
                case "UPPERLEGL":
                    return new[] { "UPPERLEGL", "HIPSL" };
                case "UPPERLEGR":
                    return new[] { "UPPERLEGR", "HIPSR" };
                case "FOOTL":
                    return new[] { "FOOTL", "BONE006TOP", "LOWERLEGL" };
                case "FOOTR":
                    return new[] { "FOOTR", "LOWERLEGR" };
                default:
                    return new[] { canonicalTargetName };
            }
        }

        private static bool IsMatchingJointName(string canonicalTargetName, string jointName)
        {
            var canonicalJointName = CanonicalizeJointName(jointName);
            if (string.IsNullOrEmpty(canonicalTargetName) || string.IsNullOrEmpty(canonicalJointName))
                return false;

            return string.Equals(canonicalTargetName, canonicalJointName, StringComparison.OrdinalIgnoreCase) ||
                   canonicalJointName.EndsWith(canonicalTargetName, StringComparison.OrdinalIgnoreCase);
        }

        private static string CanonicalizeJointName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;

            var builder = new StringBuilder(name.Length);
            for (var i = 0; i < name.Length; i++)
            {
                var c = name[i];
                if (char.IsLetterOrDigit(c))
                    builder.Append(char.ToUpperInvariant(c));
            }

            return builder
                .ToString()
                .Replace("LEFT", "L")
                .Replace("RIGHT", "R");
        }

        private static void AddSyntheticWalkClipIfNeeded(List<VisualRigAnimationClip> clips)
        {
            if (clips == null || clips.Count == 0)
                return;

            var hasWalkClip = false;
            for (var i = 0; i < clips.Count; i++)
            {
                var clipName = clips[i]?.Name ?? string.Empty;
                if (clipName.IndexOf("walk", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    clipName.IndexOf("move", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    hasWalkClip = true;
                    break;
                }
            }

            if (hasWalkClip)
                return;

            var merged = new VisualRigAnimationClip
            {
                Name = "walk",
                DurationSeconds = 0f
            };

            for (var i = 0; i < clips.Count; i++)
            {
                var clip = clips[i];
                if (clip == null ||
                    string.IsNullOrWhiteSpace(clip.Name) ||
                    clip.Name.IndexOf("idle", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    clip.Name.IndexOf("death", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    clip.Name.IndexOf("die", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    continue;
                }

                merged.DurationSeconds = Mathf.Max(merged.DurationSeconds, clip.DurationSeconds);

                foreach (var pair in clip.Tracks)
                {
                    if (!merged.Tracks.TryGetValue(pair.Key, out var mergedTrack))
                    {
                        mergedTrack = new VisualRigAnimationTrack();
                        merged.Tracks[pair.Key] = mergedTrack;
                    }

                    var sourceTrack = pair.Value;
                    if (sourceTrack == null)
                        continue;

                    if (mergedTrack.TranslationTimes == null && sourceTrack.TranslationTimes != null && sourceTrack.TranslationValues != null)
                    {
                        mergedTrack.TranslationTimes = sourceTrack.TranslationTimes;
                        mergedTrack.TranslationValues = sourceTrack.TranslationValues;
                    }

                    if (mergedTrack.RotationTimes == null && sourceTrack.RotationTimes != null && sourceTrack.RotationValues != null)
                    {
                        mergedTrack.RotationTimes = sourceTrack.RotationTimes;
                        mergedTrack.RotationValues = sourceTrack.RotationValues;
                    }

                    if (mergedTrack.ScaleTimes == null && sourceTrack.ScaleTimes != null && sourceTrack.ScaleValues != null)
                    {
                        mergedTrack.ScaleTimes = sourceTrack.ScaleTimes;
                        mergedTrack.ScaleValues = sourceTrack.ScaleValues;
                    }
                }
            }

            if (merged.Tracks.Count > 0)
                clips.Add(merged);
        }

        private static int GetDominantJointIndexForMesh(GlbContent glb, int meshIndex)
        {
            var meshes = glb.Json["meshes"] as JArray;
            var mesh = meshes?[meshIndex] as JObject;
            if (!(mesh?["primitives"] is JArray primitives))
                return -1;

            var weightsByJointIndex = new Dictionary<int, float>();

            for (var primitiveIndex = 0; primitiveIndex < primitives.Count; primitiveIndex++)
            {
                var primitive = primitives[primitiveIndex] as JObject;
                var attributes = primitive?["attributes"] as JObject;
                if (attributes == null || attributes["JOINTS_0"] == null || attributes["WEIGHTS_0"] == null)
                    continue;

                var joints = ReadIntAccessor(glb, attributes.Value<int>("JOINTS_0"));
                var weights = ReadFloatAccessor(glb, attributes.Value<int>("WEIGHTS_0"));
                if (joints.Length == 0 || weights.Length == 0)
                    continue;

                var pairCount = Math.Min(joints.Length, weights.Length);
                for (var i = 0; i < pairCount; i++)
                {
                    var jointIndex = joints[i];
                    var weight = Mathf.Max(0f, weights[i]);
                    if (!weightsByJointIndex.TryGetValue(jointIndex, out var totalWeight))
                        totalWeight = 0f;

                    weightsByJointIndex[jointIndex] = totalWeight + weight;
                }
            }

            var dominantJointIndex = -1;
            var dominantWeight = float.MinValue;
            foreach (var pair in weightsByJointIndex)
            {
                if (pair.Value <= dominantWeight)
                    continue;

                dominantWeight = pair.Value;
                dominantJointIndex = pair.Key;
            }

            return dominantJointIndex;
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

        private static (Vector3 position, Quaternion rotation, Vector3 scale) DecomposeMatrix(Matrix4x4 matrix)
        {
            var position = matrix.GetColumn(3);
            var x = matrix.GetColumn(0);
            var y = matrix.GetColumn(1);
            var z = matrix.GetColumn(2);

            var scale = new Vector3(x.magnitude, y.magnitude, z.magnitude);
            if (scale.x > 0.0001f) x /= scale.x;
            if (scale.y > 0.0001f) y /= scale.y;
            if (scale.z > 0.0001f) z /= scale.z;

            var rotation = Quaternion.LookRotation(z, y);
            return (position, rotation, scale);
        }

        private static void AppendMesh(GlbContent glb, int meshIndex, Matrix4x4 vertexTransform, List<Vertex> vertices, List<int[]> faces)
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
                    var position = vertexTransform.MultiplyPoint3x4(positions[i]);
                    var normal = vertexTransform.MultiplyVector(normals[Math.Min(i, normals.Length - 1)]);
                    if (normal.sqrMagnitude <= 0.0001f)
                        normal = Vector3.up;

                    vertices.Add(new Vertex
                    {
                        Position = position,
                        Normal = normal.normalized,
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
            var values = ReadFloatAccessor(glb, accessorIndex);
            var vectors = new Vector3[accessor.Count];

            for (var i = 0; i < accessor.Count; i++)
            {
                var offset = i * 3;
                vectors[i] = new Vector3(values[offset], values[offset + 1], values[offset + 2]);
            }

            return vectors;
        }

        private static Matrix4x4[] ReadMatrix4x4Accessor(GlbContent glb, int accessorIndex)
        {
            var accessor = GetAccessor(glb, accessorIndex);
            var values = ReadFloatAccessor(glb, accessorIndex);
            var matrices = new Matrix4x4[accessor.Count];

            for (var i = 0; i < accessor.Count; i++)
            {
                var offset = i * 16;
                matrices[i] = new Matrix4x4(
                    new Vector4(values[offset], values[offset + 1], values[offset + 2], values[offset + 3]),
                    new Vector4(values[offset + 4], values[offset + 5], values[offset + 6], values[offset + 7]),
                    new Vector4(values[offset + 8], values[offset + 9], values[offset + 10], values[offset + 11]),
                    new Vector4(values[offset + 12], values[offset + 13], values[offset + 14], values[offset + 15]));
            }

            return matrices;
        }

        private static Color32[] ReadColorAccessor(GlbContent glb, int accessorIndex)
        {
            var accessor = GetAccessor(glb, accessorIndex);
            var componentCount = GetTypeComponentCount(accessor.Type);
            var values = ReadFloatAccessor(glb, accessorIndex);
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

        private static Quaternion[] ReadQuaternionAccessor(GlbContent glb, int accessorIndex)
        {
            var accessor = GetAccessor(glb, accessorIndex);
            var values = ReadFloatAccessor(glb, accessorIndex);
            var quaternions = new Quaternion[accessor.Count];

            for (var i = 0; i < accessor.Count; i++)
            {
                var offset = i * 4;
                var quaternion = new Quaternion(
                    values[offset],
                    values[offset + 1],
                    values[offset + 2],
                    values[offset + 3]);

                var quaternionMagnitude = (quaternion.x * quaternion.x) + (quaternion.y * quaternion.y) + (quaternion.z * quaternion.z) + (quaternion.w * quaternion.w);
                if (quaternion == Quaternion.identity || quaternionMagnitude <= 0.0001f)
                    quaternion = Quaternion.identity;
                else
                    quaternion = Quaternion.Normalize(quaternion);

                quaternions[i] = quaternion;
            }

            return quaternions;
        }

        private static float[] ReadFloatAccessor(GlbContent glb, int accessorIndex)
        {
            var accessor = GetAccessor(glb, accessorIndex);
            var bufferView = GetBufferView(glb, accessor.BufferView);
            var componentCount = GetTypeComponentCount(accessor.Type);
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

        private static int[] ReadIntAccessor(GlbContent glb, int accessorIndex)
        {
            var accessor = GetAccessor(glb, accessorIndex);
            var bufferView = GetBufferView(glb, accessor.BufferView);
            var componentCount = GetTypeComponentCount(accessor.Type);
            var elementSize = componentCount * GetComponentByteSize(accessor.ComponentType);
            var stride = bufferView.ByteStride > 0 ? bufferView.ByteStride : elementSize;
            var start = bufferView.ByteOffset + accessor.ByteOffset;
            var output = new int[accessor.Count * componentCount];

            for (var i = 0; i < accessor.Count; i++)
            {
                var elementOffset = start + i * stride;
                for (var component = 0; component < componentCount; component++)
                {
                    var componentOffset = elementOffset + component * GetComponentByteSize(accessor.ComponentType);
                    output[i * componentCount + component] = ReadComponentAsInt(glb.BinaryChunk, componentOffset, accessor.ComponentType);
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
                indices[i] = ReadComponentAsInt(glb.BinaryChunk, start + i * stride, accessor.ComponentType);

            return indices;
        }

        private static AccessorInfo GetAccessor(GlbContent glb, int accessorIndex)
        {
            var accessors = glb.Json["accessors"] as JArray;
            var accessor = accessors?[accessorIndex] as JObject;
            if (accessor == null)
                throw new InvalidDataException("Accessor missing.");

            return new AccessorInfo
            {
                BufferView = accessor.Value<int>("bufferView"),
                ByteOffset = accessor.Value<int?>("byteOffset") ?? 0,
                ComponentType = accessor.Value<int>("componentType"),
                Count = accessor.Value<int>("count"),
                Normalized = accessor.Value<bool?>("normalized") ?? false,
                Type = accessor.Value<string>("type") ?? "SCALAR"
            };
        }

        private static BufferViewInfo GetBufferView(GlbContent glb, int bufferViewIndex)
        {
            var bufferViews = glb.Json["bufferViews"] as JArray;
            var bufferView = bufferViews?[bufferViewIndex] as JObject;
            if (bufferView == null)
                throw new InvalidDataException("BufferView missing.");

            return new BufferViewInfo
            {
                ByteOffset = bufferView.Value<int?>("byteOffset") ?? 0,
                ByteLength = bufferView.Value<int>("byteLength"),
                ByteStride = bufferView.Value<int?>("byteStride") ?? 0
            };
        }

        private static GlbContent ReadGlb(string path)
        {
            var bytes = File.ReadAllBytes(path);
            if (bytes.Length < 20 || BitConverter.ToUInt32(bytes, 0) != 0x46546C67)
                throw new InvalidDataException("Invalid GLB header.");

            var jsonChunkLength = BitConverter.ToInt32(bytes, 12);
            var jsonChunkType = BitConverter.ToUInt32(bytes, 16);
            if (jsonChunkType != 0x4E4F534A)
                throw new InvalidDataException("Missing GLB JSON chunk.");

            var jsonChunkOffset = 20;
            var jsonText = Encoding.UTF8.GetString(bytes, jsonChunkOffset, jsonChunkLength).TrimEnd('\0', ' ', '\t', '\r', '\n');
            var json = JObject.Parse(jsonText);

            var binHeaderOffset = jsonChunkOffset + Align4(jsonChunkLength);
            if (binHeaderOffset + 8 > bytes.Length)
                throw new InvalidDataException("Missing GLB BIN header.");

            var binChunkLength = BitConverter.ToInt32(bytes, binHeaderOffset);
            var binChunkType = BitConverter.ToUInt32(bytes, binHeaderOffset + 4);
            if (binChunkType != 0x004E4942)
                throw new InvalidDataException("Missing GLB BIN chunk.");

            var binChunkOffset = binHeaderOffset + 8;
            if (binChunkOffset + binChunkLength > bytes.Length)
                throw new InvalidDataException("Invalid GLB BIN chunk length.");

            var binaryChunk = new byte[binChunkLength];
            Buffer.BlockCopy(bytes, binChunkOffset, binaryChunk, 0, binChunkLength);

            return new GlbContent
            {
                Json = json,
                BinaryChunk = binaryChunk
            };
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

        private static int[] CreateSequentialIndices(int count)
        {
            var indices = new int[count];
            for (var i = 0; i < count; i++)
                indices[i] = i;
            return indices;
        }

        private static float ReadComponentAsFloat(byte[] binaryChunk, int offset, int componentType, bool normalized)
        {
            switch (componentType)
            {
                case 5126:
                    return BitConverter.ToSingle(binaryChunk, offset);
                case 5125:
                    return BitConverter.ToUInt32(binaryChunk, offset);
                case 5123:
                    return normalized ? BitConverter.ToUInt16(binaryChunk, offset) / 65535f : BitConverter.ToUInt16(binaryChunk, offset);
                case 5122:
                    return normalized ? Math.Max(BitConverter.ToInt16(binaryChunk, offset) / 32767f, -1f) : BitConverter.ToInt16(binaryChunk, offset);
                case 5121:
                    return normalized ? binaryChunk[offset] / 255f : binaryChunk[offset];
                case 5120:
                    return normalized ? Math.Max((sbyte)binaryChunk[offset] / 127f, -1f) : (sbyte)binaryChunk[offset];
                default:
                    throw new InvalidDataException("Unsupported accessor component type " + componentType + ".");
            }
        }

        private static int ReadComponentAsInt(byte[] binaryChunk, int offset, int componentType)
        {
            switch (componentType)
            {
                case 5125:
                    return (int)BitConverter.ToUInt32(binaryChunk, offset);
                case 5123:
                    return BitConverter.ToUInt16(binaryChunk, offset);
                case 5121:
                    return binaryChunk[offset];
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
                case "MAT2":
                    return 4;
                case "MAT3":
                    return 9;
                case "MAT4":
                    return 16;
                default:
                    throw new InvalidDataException("Unsupported accessor type " + type + ".");
            }
        }

        private static Vector3 ReadVector3(JToken token, Vector3 fallback)
        {
            if (!(token is JArray array) || array.Count < 3)
                return fallback;

            return new Vector3(array[0].Value<float>(), array[1].Value<float>(), array[2].Value<float>());
        }

        private static Quaternion ReadQuaternion(JToken token)
        {
            if (!(token is JArray array) || array.Count < 4)
                return Quaternion.identity;

            return new Quaternion(array[0].Value<float>(), array[1].Value<float>(), array[2].Value<float>(), array[3].Value<float>());
        }

        private static byte ToByte(float value)
        {
            return (byte)Mathf.Clamp(Mathf.RoundToInt(value * 255f), 0, 255);
        }

        private static int Align4(int value)
        {
            return (value + 3) & ~3;
        }

        private static string SanitizeFileName(string value)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var builder = new StringBuilder(value.Length);
            for (var i = 0; i < value.Length; i++)
            {
                var c = value[i];
                builder.Append(Array.IndexOf(invalid, c) >= 0 ? '_' : c);
            }

            return builder.ToString();
        }
    }
}
