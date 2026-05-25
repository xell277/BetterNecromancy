using MeshedObjects;
using Monsters;
using NPC;
using Pandaros.API.Monsters;
using Pipliz;
using Shared;
using System;
using System.Collections.Generic;
using UnityEngine;
using BetterNecromancy;

namespace Pandaros.Settlers.Monsters
{
    [ModLoader.ModManager]
    public static class BossVisualProxyManager
    {
        private sealed class RegisteredBossVisual
        {
            public string BossName;
            public string MeshFileName;
            public string MeshPath;
            public ECachedFileType MeshFileType;
            public MeshedObjectType Type;
            public MeshedObjectTypeSettings Settings;
            public List<RegisteredBossVisualPart> Parts = new List<RegisteredBossVisualPart>();
            public List<BossVisualRigBakeService.VisualRigAnimationClip> Clips = new List<BossVisualRigBakeService.VisualRigAnimationClip>();
            public bool IsRig;
            public Vector3 LocalBoundsCenter;
            public Vector3 LocalBoundsSize;
        }

        private sealed class RegisteredBossVisualPart
        {
            public string Name;
            public bool IsRenderable;
            public int ParentPartIndex;
            public Vector3 LocalPosition;
            public Quaternion LocalRotation;
            public Vector3 LocalScale;
            public MeshedObjectType Type;
            public MeshedObjectTypeSettings Settings;
        }

        private sealed class ActiveBossVisual
        {
            public NPCID BossId;
            public Zombie Boss;
            public RegisteredBossVisual Visual;
            public ClientMeshedObject ClientObject;
            public List<ActiveBossVisualPart> Parts = new List<ActiveBossVisualPart>();
            public Vector3 SmoothedPosition;
            public Quaternion SmoothedRotation;
            public Vector3 SmoothedDirection;
            public Vector3 AnimationDirection;
            public Vector3 LastDriverPosition;
            public Vector3 EstimatedVelocity;
            public long LastDriverMovedAtMs;
            public Vector3 LastPosition;
            public Quaternion LastRotation;
            public long LastSyncAtMs;
            public bool HasSentState;
            public BossVisualRigBakeService.VisualRigAnimationClip ActiveClip;
            public string ActiveClipKey;
            public float ActiveClipTimeSeconds;
            public BossVisualRigBakeService.VisualRigAnimationClip OneShotClip;
            public string OneShotClipKey;
            public long OneShotClipEndsAtMs;
            public bool RemoveAfterOneShot;
            public Vector3 FrozenRootPosition;
            public Quaternion FrozenRootRotation;
        }

        private sealed class ActiveBossVisualPart
        {
            public RegisteredBossVisualPart Definition;
            public ClientMeshedObject ClientObject;
            public Vector3 CurrentPosition;
            public Quaternion CurrentRotation;
            public Vector3 CurrentScale;
            public Matrix4x4 CurrentMatrix;
            public Vector3 LastPosition;
            public Quaternion LastRotation;
            public bool HasSentState;
        }

        private static readonly Dictionary<string, string> BossMeshByName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Bulging"] = "visuals/BossBulging.ply",
            ["Fallen Ranger"] = "visuals/BossFallenRanger.ply",
            ["Hoarder"] = "visuals/BossHoarder.ply",
            ["Jack-b-Nimble"] = "visuals/BossJackBNimble.ply",
            ["Juggernaut"] = "visuals/BossJuggernaut.ply",
            ["Phase"] = "visuals/BossPhase.ply",
            ["Putrid Corpse"] = "visuals/BossPutridCorpse.ply",
            ["ZombieKing"] = "visuals/BossZombieKing.ply",
            ["ZombieQueen"] = "visuals/BossZombieQueen.ply"
        };

        private static readonly Dictionary<string, RegisteredBossVisual> RegisteredVisuals = new Dictionary<string, RegisteredBossVisual>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<NPCID, ActiveBossVisual> ActiveVisuals = new Dictionary<NPCID, ActiveBossVisual>();
        private static readonly HashSet<NPCID> LoggedSpawnedVisuals = new HashSet<NPCID>();
        private static readonly HashSet<int> PendingPlayerSyncs = new HashSet<int>();
        private static readonly HashSet<int> SyncedPlayerIds = new HashSet<int>();

        private static long _nextSyncAtMs;
        private static bool _registeredThisWorld;

        private const int SyncIntervalMs = 16;
        private const int SendRadius = 256;
        private const string DefaultTextureMapping = "neutral";

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AfterWorldLoad, BetterNecromancy.ModEntry.Namespace + ".BossVisualProxyManager.AfterWorldLoad")]
        public static void AfterWorldLoad()
        {
            ClearVisualRuntimeState();
            _registeredThisWorld = false;
            _nextSyncAtMs = 0L;
            RegisterVisualTypes();
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnPlayerConnectedLate, BetterNecromancy.ModEntry.Namespace + ".BossVisualProxyManager.OnPlayerConnectedLate")]
        public static void OnPlayerConnectedLate(Players.Player player)
        {
            if (!BossVisualSettings.CustomBossVisualProxyEnabled)
                return;

            RegisterVisualTypes();
            var playerKey = GetPlayerKey(player);
            if (playerKey >= 0)
            {
                SyncedPlayerIds.Remove(playerKey);
                if (ActiveVisuals.Count > 0)
                    PendingPlayerSyncs.Add(playerKey);
            }
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnMonsterSpawned, BetterNecromancy.ModEntry.Namespace + ".BossVisualProxyManager.OnMonsterSpawned")]
        public static void OnMonsterSpawned(IMonster monster)
        {
            if (!BossVisualSettings.CustomBossVisualProxyEnabled)
                return;

            RegisterVisualTypes();

            if (!(monster is IPandaBoss boss) || !(monster is Zombie zombie))
            {
                return;
            }

            if (!RegisteredVisuals.TryGetValue(boss.name, out var visual))
            {
                return;
            }

            RemoveVisual(zombie.ID, zombie.Position.Vector);

            var activeVisual = new ActiveBossVisual
            {
                BossId = zombie.ID,
                Boss = zombie,
                Visual = visual,
                ClientObject = visual.IsRig ? default : new ClientMeshedObject(visual.Type)
            };

            if (visual.IsRig)
            {
                for (var i = 0; i < visual.Parts.Count; i++)
                {
                    var clientObject = visual.Parts[i].IsRenderable
                        ? new ClientMeshedObject(visual.Parts[i].Type)
                        : default;

                    activeVisual.Parts.Add(new ActiveBossVisualPart
                    {
                        Definition = visual.Parts[i],
                        ClientObject = clientObject
                    });
                }
            }

                ActiveVisuals[zombie.ID] = activeVisual;
                QueueUnsyncedConnectedPlayersForVisualSync();
                FlushPendingPlayerSyncs();
                if (CanSyncBossVisuals())
                    SendOrUpdateVisual(activeVisual, force: true);
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnMonsterDied, BetterNecromancy.ModEntry.Namespace + ".BossVisualProxyManager.OnMonsterDied")]
        public static void OnMonsterDied(IMonster monster)
        {
            if (!BossVisualSettings.CustomBossVisualProxyEnabled)
                return;

            if (!(monster is Zombie zombie))
                return;

            if (TryStartDeathClip(zombie.ID))
                return;

            RemoveVisual(zombie.ID, zombie.Position.Vector);
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnUpdate, BetterNecromancy.ModEntry.Namespace + ".BossVisualProxyManager.OnUpdate")]
        public static void OnUpdate()
        {
            if (!BossVisualSettings.CustomBossVisualProxyEnabled)
            {
                ClearVisualRuntimeState();
                return;
            }

            RegisterVisualTypes();
            RecoverMissingBossVisuals();
            QueueUnsyncedConnectedPlayersForVisualSync();
            FlushPendingPlayerSyncs();

            if (!CanSyncBossVisuals())
                return;

            var now = Pipliz.Time.MillisecondsSinceStart;
            if (now < _nextSyncAtMs)
                return;

            _nextSyncAtMs = now + SyncIntervalMs;

            var removals = default(List<NPCID>);
            foreach (var pair in ActiveVisuals)
            {
                var visual = pair.Value;
                if (visual?.Visual == null)
                {
                    if (removals == null)
                        removals = new List<NPCID>();

                    removals.Add(pair.Key);
                    continue;
                }

                if (visual.RemoveAfterOneShot)
                {
                    if (now >= visual.OneShotClipEndsAtMs)
                    {
                        if (removals == null)
                            removals = new List<NPCID>();

                        removals.Add(pair.Key);
                        continue;
                    }

                    SendOrUpdateVisual(visual, force: false);
                    continue;
                }

                if (visual.Boss == null || visual.Boss.CurrentHealth <= 0f)
                {
                    if (removals == null)
                        removals = new List<NPCID>();

                    removals.Add(pair.Key);
                    continue;
                }

                SendOrUpdateVisual(visual, force: false);
            }

            if (removals == null)
                return;

            for (var i = 0; i < removals.Count; i++)
                RemoveVisual(removals[i], Vector3.zero);
        }

        private static void RegisterVisualTypes()
        {
            if (!BossVisualSettings.CustomBossVisualProxyEnabled)
                return;

            if (_registeredThisWorld || ServerManager.FileTable == null)
                return;

            foreach (var pair in BossMeshByName)
            {
                if (!GameLoader.TryGetBossVisualMeshPath(pair.Value, out var sourceMeshPath))
                {
                    continue;
                }

                var profile = BossVisualSettings.GetProfile(pair.Key);
                var rigVisual = new RegisteredBossVisual
                {
                    BossName = pair.Key,
                    MeshFileName = pair.Value,
                    MeshPath = sourceMeshPath,
                    MeshFileType = GetMeshFileType(sourceMeshPath)
                };

                var hasGlbFeatures = BossVisualRigBakeService.TryReadGlbFeatures(sourceMeshPath, out var glbFeatures) &&
                                     glbFeatures != null;
                var allowRawVisualFallback = !hasGlbFeatures || !glbFeatures.ShouldUseRawVisual;

                if (BossVisualRigBakeService.TryPrepareVisualRig(sourceMeshPath, pair.Value, out var rigParts, out var rigClips, out var rigBounds) &&
                    rigParts?.Count > 0)
                {
                    var allPartsLoaded = true;
                    var renderablePartCount = 0;
                    for (var i = 0; i < rigParts.Count; i++)
                    {
                        var rigPart = rigParts[i];
                        MeshedObjectType partType = default;
                        MeshedObjectTypeSettings partSettings = null;

                        if (rigPart.IsRenderable)
                        {
                            var partFileId = ServerManager.FileTable.StartLoading(rigPart.MeshPath, rigPart.MeshFileType);
                            if (!partFileId.IsValid)
                            {
                                allPartsLoaded = false;
                                break;
                            }

                            partSettings = new MeshedObjectTypeSettings(
                                BetterNecromancy.ModEntry.Namespace + ".BossVisual." + pair.Key.Replace(" ", string.Empty).Replace("-", string.Empty) + ".Part" + i,
                                partFileId,
                                DefaultTextureMapping)
                            {
                                colliders = new List<ObjectCollider>(),
                                sendUpdateRadius = SendRadius,
                                InterpolationLooseness = 0.005f,
                                TimeoutSeconds = 30f
                            };

                            partType = MeshedObjectType.Register(partSettings);
                            renderablePartCount++;
                        }

                        rigVisual.Parts.Add(new RegisteredBossVisualPart
                        {
                            Name = rigPart.Name,
                            IsRenderable = rigPart.IsRenderable,
                            ParentPartIndex = rigPart.ParentPartIndex,
                            LocalPosition = rigPart.LocalPosition,
                            LocalRotation = rigPart.LocalRotation,
                            LocalScale = rigPart.LocalScale,
                            Type = partType,
                            Settings = partSettings
                        });
                    }

                    if (allPartsLoaded && renderablePartCount > 0)
                    {
                        rigVisual.IsRig = true;
                        rigVisual.Clips = rigClips ?? new List<BossVisualRigBakeService.VisualRigAnimationClip>();
                        if (rigBounds != null)
                        {
                            rigVisual.LocalBoundsCenter = rigBounds.Center;
                            rigVisual.LocalBoundsSize = rigBounds.Size;
                        }
                        RegisteredVisuals[pair.Key] = rigVisual;
                        continue;
                    }
                }

                if (!allowRawVisualFallback)
                {
                    continue;
                }

                var meshPath = sourceMeshPath;
                var meshFileType = GetMeshFileType(sourceMeshPath);

                if (profile.UseBakedVisual || meshFileType == ECachedFileType.MeshPly)
                {
                    if (!BossMeshBakeService.TryPrepareVisualMesh(sourceMeshPath, pair.Value, out meshPath, out meshFileType))
                    {
                        continue;
                    }
                }

                var fileId = ServerManager.FileTable.StartLoading(meshPath, meshFileType);
                if (!fileId.IsValid)
                {
                    continue;
                }

                var settings = new MeshedObjectTypeSettings(
                    BetterNecromancy.ModEntry.Namespace + ".BossVisual." + pair.Key.Replace(" ", string.Empty).Replace("-", string.Empty),
                    fileId,
                    DefaultTextureMapping)
                {
                    // Keep the visible boss purely visual. Hits should go to the hidden native boss driver,
                    // otherwise the client treats the proxy like a block/controlled mesh.
                    colliders = new List<ObjectCollider>(),
                    sendUpdateRadius = SendRadius,
                    InterpolationLooseness = 0.005f,
                    TimeoutSeconds = 30f
                };

                var type = MeshedObjectType.Register(settings);
                rigVisual.MeshPath = meshPath;
                rigVisual.MeshFileType = meshFileType;
                rigVisual.Settings = settings;
                rigVisual.Type = type;
                rigVisual.LocalBoundsCenter = new Vector3(0f, 1.5f, 0f);
                rigVisual.LocalBoundsSize = new Vector3(1.5f, 3f, 1.5f);
                RegisteredVisuals[pair.Key] = rigVisual;

            }

            _registeredThisWorld = true;
        }

        private static ECachedFileType GetMeshFileType(string meshPath)
        {
            return string.Equals(System.IO.Path.GetExtension(meshPath), ".ply", StringComparison.OrdinalIgnoreCase)
                ? ECachedFileType.MeshPly
                : ECachedFileType.MeshGLB;
        }

        private static void ClearVisualRuntimeState()
        {
            RegisteredVisuals.Clear();
            ActiveVisuals.Clear();
            LoggedSpawnedVisuals.Clear();
            PendingPlayerSyncs.Clear();
            SyncedPlayerIds.Clear();
        }

        private static bool CanSyncBossVisuals()
        {
            var hasReadyPlayer = false;

            foreach (var player in Players.ConnectedPlayers)
            {
                if (PlayerUiGuard.CanSendStable(player))
                {
                    hasReadyPlayer = true;
                    continue;
                }

                if (player != null)
                    return false;
            }

            return hasReadyPlayer;
        }

        private static void SendOrUpdateVisual(ActiveBossVisual visual, bool force)
        {
            var profile = BossVisualSettings.GetProfile(visual.Visual.BossName);
            var now = Pipliz.Time.MillisecondsSinceStart;
            if (visual.OneShotClip != null && !visual.RemoveAfterOneShot && now >= visual.OneShotClipEndsAtMs)
            {
                visual.OneShotClip = null;
                visual.OneShotClipKey = null;
                visual.OneShotClipEndsAtMs = 0L;
            }

            var oneShotActive = visual.OneShotClip != null && now < visual.OneShotClipEndsAtMs;
            var freezeRootToClip = visual.RemoveAfterOneShot && oneShotActive;
            var timeSeconds = now / 1000f;
            var deltaSeconds = visual.HasSentState && visual.LastSyncAtMs > 0L
                ? Mathf.Clamp((now - visual.LastSyncAtMs) / 1000f, 0.01f, 0.20f)
                : (SyncIntervalMs / 1000f);
            var nominalSpeed = Mathf.Max(0.1f, visual.Boss.MovementSpeed * visual.Boss.MovementSpeedMultiplier);
            var direction = visual.Boss.Direction;
            var normalizedDirection = direction.sqrMagnitude > 0.0001f
                ? direction.normalized
                : (visual.HasSentState && visual.SmoothedDirection.sqrMagnitude > 0.0001f
                    ? visual.SmoothedDirection.normalized
                    : Vector3.forward);
            var driverPosition = visual.Boss.Position.Vector;
            var driverDelta = driverPosition - visual.LastDriverPosition;

            var driverMoved = driverDelta.sqrMagnitude > 0.0001f;
            var desiredVelocity = normalizedDirection * nominalSpeed;
            if (force || !visual.HasSentState)
            {
                visual.SmoothedDirection = normalizedDirection;
                visual.AnimationDirection = normalizedDirection;
                visual.EstimatedVelocity = desiredVelocity;
                visual.LastDriverMovedAtMs = now;
            }
            else if (driverMoved)
            {
                visual.LastDriverMovedAtMs = now;
                var rawVelocity = driverDelta / deltaSeconds;
                var rawMagnitude = rawVelocity.magnitude;
                if (rawMagnitude > 0.0001f)
                {
                    var clampedMagnitude = Mathf.Clamp(rawMagnitude, nominalSpeed * 0.75f, nominalSpeed * 1.5f);
                    var steppedVelocity = rawVelocity.normalized * clampedMagnitude;
                    visual.EstimatedVelocity = Vector3.Lerp(visual.EstimatedVelocity, steppedVelocity, 0.25f);
                    normalizedDirection = Vector3.Slerp(normalizedDirection, rawVelocity.normalized, 0.55f).normalized;
                }
            }

            if (force || !visual.HasSentState)
            {
                visual.SmoothedDirection = normalizedDirection;
                visual.AnimationDirection = normalizedDirection;
            }
            else
            {
                visual.SmoothedDirection = Vector3.Slerp(
                    visual.SmoothedDirection.sqrMagnitude > 0.0001f ? visual.SmoothedDirection.normalized : normalizedDirection,
                    normalizedDirection,
                    driverMoved ? 0.035f : 0.012f);

                visual.AnimationDirection = Vector3.Slerp(
                    visual.AnimationDirection.sqrMagnitude > 0.0001f ? visual.AnimationDirection.normalized : visual.SmoothedDirection,
                    visual.SmoothedDirection.sqrMagnitude > 0.0001f ? visual.SmoothedDirection.normalized : normalizedDirection,
                    driverMoved ? 0.015f : 0.006f);
            }

            var movementDirection = visual.SmoothedDirection.sqrMagnitude > 0.0001f
                ? visual.SmoothedDirection.normalized
                : normalizedDirection;
            var animationDirection = visual.AnimationDirection.sqrMagnitude > 0.0001f
                ? visual.AnimationDirection.normalized
                : movementDirection;
            var facingDirection = normalizedDirection.sqrMagnitude > 0.0001f
                ? normalizedDirection.normalized
                : movementDirection;
            var anchorDirection = facingDirection.sqrMagnitude > 0.0001f
                ? facingDirection.normalized
                : movementDirection;
            
            var keepMovingSeconds = Mathf.Clamp(0.75f / nominalSpeed, 0.24f, 0.85f);
            var secondsSinceDriverMove = Mathf.Max(0f, (now - visual.LastDriverMovedAtMs) / 1000f);
            var decayWindowSeconds = keepMovingSeconds * 1.00f;
            var motionBlend = secondsSinceDriverMove <= keepMovingSeconds
                ? 1f
                : Mathf.Clamp01(1f - ((secondsSinceDriverMove - keepMovingSeconds) / decayWindowSeconds));
            var motionVelocity = movementDirection * nominalSpeed * motionBlend;
            visual.EstimatedVelocity = Vector3.Lerp(
                visual.EstimatedVelocity,
                motionVelocity,
                driverMoved ? 0.26f : 0.05f);

            var leadDistance = Mathf.Clamp(nominalSpeed * profile.FollowLeadSeconds, 0f, 0.04f);
            var rightDirection = Vector3.Cross(Vector3.up, facingDirection);
            if (rightDirection.sqrMagnitude <= 0.0001f)
                rightDirection = Vector3.right;
            else
                rightDirection.Normalize();

            var targetPosition = driverPosition
                                 + profile.PositionOffset
                                 + (rightDirection * profile.PositionOffsetRight)
                                 + (facingDirection * (leadDistance + profile.PositionOffsetForward));
            var rotationDirection = driverMoved
                ? movementDirection
                : facingDirection;
            if (rotationDirection.sqrMagnitude <= 0.0001f)
                rotationDirection = visual.HasSentState ? (visual.SmoothedRotation * Vector3.forward) : facingDirection;
            var targetRotation = GetBossRotation(rotationDirection, profile.RotationYawDegrees);

            if (freezeRootToClip)
            {
                visual.EstimatedVelocity = Vector3.zero;
                targetPosition = visual.FrozenRootPosition;
                targetRotation = visual.FrozenRootRotation;
            }

            if (force || !visual.HasSentState)
            {
                visual.SmoothedPosition = targetPosition;
                visual.SmoothedRotation = targetRotation;
            }
            else
            {
                visual.SmoothedPosition += visual.EstimatedVelocity * deltaSeconds * 0.65f;
                var anchorDelta = targetPosition - visual.SmoothedPosition;
                var anchorDistance = anchorDelta.magnitude;
                if (anchorDistance > 2.5f)
                {
                    visual.SmoothedPosition = targetPosition;
                }
                else if (anchorDistance < 0.04f)
                {
                    visual.SmoothedPosition = Vector3.Lerp(visual.SmoothedPosition, targetPosition, 0.18f);
                }

                var longitudinalCorrection = Vector3.Project(anchorDelta, anchorDirection);
                var lateralCorrection = anchorDelta - longitudinalCorrection;
                var correction =
                    (lateralCorrection * Mathf.Lerp(0.14f, 0.35f, profile.PositionSmoothing)) +
                    (longitudinalCorrection * Mathf.Lerp(0.10f, 0.22f, profile.PositionSmoothing));
                var maxCorrection = Mathf.Max(0.010f, nominalSpeed * deltaSeconds * 0.18f);
                visual.SmoothedPosition += Vector3.ClampMagnitude(correction, maxCorrection);

                var rotationSharpness = Mathf.Lerp(7f, 16f, profile.RotationSmoothing);
                var rotationBlend = 1f - Mathf.Exp(-rotationSharpness * deltaSeconds);
                visual.SmoothedRotation = Quaternion.Slerp(
                    visual.SmoothedRotation,
                    targetRotation,
                    Mathf.Clamp01(rotationBlend));
            }

            var visualForwardDirection = visual.SmoothedRotation * Vector3.forward;
            if (visualForwardDirection.sqrMagnitude <= 0.0001f)
                visualForwardDirection = movementDirection;
            else
                visualForwardDirection.Normalize();

            var visualRightDirection = visual.SmoothedRotation * Vector3.right;
            if (visualRightDirection.sqrMagnitude <= 0.0001f)
                visualRightDirection = rightDirection;
            else
                visualRightDirection.Normalize();

            var movementAlpha = nominalSpeed <= 0.001f
                ? 0f
                : Mathf.Clamp01(visual.EstimatedVelocity.magnitude / nominalSpeed);
            BossVisualRigBakeService.VisualRigAnimationClip activeClip;
            string activeClipKey;
            if (oneShotActive)
            {
                activeClip = visual.OneShotClip;
                activeClipKey = visual.OneShotClipKey;
            }
            else
            {
                activeClip = SelectClipForVisual(visual.Visual, movementAlpha, out activeClipKey);
            }

            if (!ReferenceEquals(activeClip, visual.ActiveClip) || !string.Equals(activeClipKey, visual.ActiveClipKey, StringComparison.OrdinalIgnoreCase))
            {
                visual.ActiveClip = activeClip;
                visual.ActiveClipKey = activeClipKey;
                visual.ActiveClipTimeSeconds = 0f;
            }
            else if (activeClip != null)
            {
                var playbackSpeed = string.Equals(activeClipKey, "walk", StringComparison.OrdinalIgnoreCase)
                    ? Mathf.Lerp(0.75f, 1.25f, movementAlpha)
                    : 1f;
                visual.ActiveClipTimeSeconds += deltaSeconds * playbackSpeed;
            }

            var animatedRootOffset = activeClip != null
                ? Vector3.zero
                : GetAnimatedRootOffset(visual, timeSeconds, movementAlpha);
            var animatedRootPosition = visual.SmoothedPosition + animatedRootOffset;

            if (visual.Visual.IsRig)
            {
                for (var i = 0; i < visual.Parts.Count; i++)
                {
                    var part = visual.Parts[i];
                    var localPosition = part.Definition.LocalPosition;
                    var localRotation = part.Definition.LocalRotation;
                    var localScale = part.Definition.LocalScale;
                    var clipApplied = ApplyRigClipAnimation(activeClip, visual.ActiveClipTimeSeconds, i, ref localPosition, ref localRotation, ref localScale);
                    if (activeClip == null && !clipApplied)
                        ApplyProceduralAnimation(visual, part.Definition, timeSeconds, movementAlpha, ref localPosition, ref localRotation);
                    var localMatrix = Matrix4x4.TRS(localPosition, localRotation, localScale);
                    Matrix4x4 worldMatrix;

                    if (part.Definition.ParentPartIndex >= 0 && part.Definition.ParentPartIndex < visual.Parts.Count)
                    {
                        var parentPart = visual.Parts[part.Definition.ParentPartIndex];
                        worldMatrix = parentPart.CurrentMatrix * localMatrix;
                    }
                    else
                    {
                        worldMatrix = Matrix4x4.TRS(animatedRootPosition, visual.SmoothedRotation, Vector3.one) * localMatrix;
                    }

                    var (worldPosition, worldRotation, worldScale) = DecomposeMatrix(worldMatrix);

                    if (activeClip == null && !clipApplied)
                    {
                        ApplyWorldProceduralAnimation(
                            visual,
                            part.Definition,
                            timeSeconds,
                            movementAlpha,
                            visualForwardDirection,
                            visualRightDirection,
                            ref worldPosition,
                            ref worldRotation);
                    }

                    part.CurrentPosition = worldPosition;
                    part.CurrentRotation = worldRotation;
                    part.CurrentScale = worldScale;
                    // Preserve the exact hierarchical matrix for child parts. Reconstructing it
                    // from decomposed TRS loses the original non-uniform-scale/shear combination,
                    // which breaks bosses like ZombieQueen that animate with scaled rigid nodes.
                    part.CurrentMatrix = worldMatrix;

                    if (part.Definition.IsRenderable)
                    {
                        part.ClientObject.SendMoveToInterpolatedRenderDistance(
                            worldPosition,
                            worldRotation,
                            part.Definition.Settings,
                            part.Definition.Settings.sendUpdateRadius);
                    }

                    part.LastPosition = worldPosition;
                    part.LastRotation = worldRotation;
                    part.HasSentState = true;
                }
            }
            else
            {
                var wholeBodyPosition = animatedRootPosition + GetWholeBodyWalkOffset(
                    timeSeconds,
                    movementAlpha,
                    visualForwardDirection,
                    visualRightDirection);

                visual.ClientObject.SendMoveToInterpolatedRenderDistance(
                    wholeBodyPosition,
                    visual.SmoothedRotation,
                    visual.Visual.Settings,
                    visual.Visual.Settings.sendUpdateRadius);

                visual.LastPosition = wholeBodyPosition;
                visual.LastRotation = visual.SmoothedRotation;
            }

            if (force)
                LoggedSpawnedVisuals.Add(visual.BossId);

            if (visual.Visual.IsRig)
            {
                visual.LastPosition = animatedRootPosition;
                visual.LastRotation = visual.SmoothedRotation;
            }

            visual.LastDriverPosition = driverPosition;
            visual.LastSyncAtMs = now;
            visual.HasSentState = true;
        }

        private static BossVisualRigBakeService.VisualRigAnimationClip SelectClipForVisual(RegisteredBossVisual visual, float movementAlpha, out string clipKey)
        {
            clipKey = movementAlpha > 0.18f ? "walk" : "idle";
            if (visual?.Clips == null || visual.Clips.Count == 0)
                return null;

            var exact = FindClipByKey(visual.Clips, clipKey);
            if (exact != null)
                return exact;

            if (string.Equals(clipKey, "walk", StringComparison.OrdinalIgnoreCase))
            {
                var fallback = FindClipByKey(visual.Clips, "move");
                if (fallback != null)
                    return fallback;
                return visual.Clips[0];
            }

            return null;
        }

        private static string NormalizeClipKey(string clipName, string fallbackKey)
        {
            if (string.IsNullOrWhiteSpace(clipName))
                return fallbackKey;

            var lowered = clipName.Trim().ToLowerInvariant();
            if (lowered.Contains("death") || lowered.Contains("die") || lowered.Contains("dead"))
                return "death";
            if (lowered.Contains("attack") || lowered.Contains("punch") || lowered.Contains("melee") || lowered.Contains("action"))
                return "attack";
            if (lowered.Contains("walk") || lowered.Contains("move"))
                return "walk";
            if (lowered.Contains("idle"))
                return "idle";

            return fallbackKey;
        }

        private static BossVisualRigBakeService.VisualRigAnimationClip FindClipByKeys(
            List<BossVisualRigBakeService.VisualRigAnimationClip> clips,
            params string[] clipKeys)
        {
            if (clips == null || clips.Count == 0 || clipKeys == null || clipKeys.Length == 0)
                return null;

            for (var keyIndex = 0; keyIndex < clipKeys.Length; keyIndex++)
            {
                var clip = FindClipByKey(clips, clipKeys[keyIndex]);
                if (clip != null)
                    return clip;
            }

            return null;
        }

        private static BossVisualRigBakeService.VisualRigAnimationClip FindClipByKey(
            List<BossVisualRigBakeService.VisualRigAnimationClip> clips,
            string clipKey)
        {
            if (clips == null || clips.Count == 0 || string.IsNullOrWhiteSpace(clipKey))
                return null;

            for (var i = 0; i < clips.Count; i++)
            {
                var clip = clips[i];
                if (clip != null && !string.IsNullOrWhiteSpace(clip.Name) &&
                    clip.Name.IndexOf(clipKey, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return clip;
                }
            }

            return null;
        }

        private static void BeginOneShotClip(
            ActiveBossVisual visual,
            BossVisualRigBakeService.VisualRigAnimationClip clip,
            string clipKey,
            bool removeAfterPlayback)
        {
            if (visual == null || clip == null)
                return;

            var durationSeconds = Mathf.Max(0.1f, clip.DurationSeconds);
            visual.OneShotClip = clip;
            visual.OneShotClipKey = clipKey;
            visual.OneShotClipEndsAtMs = Pipliz.Time.MillisecondsSinceStart + Mathf.CeilToInt(durationSeconds * 1000f);
            visual.RemoveAfterOneShot = removeAfterPlayback;

            if (removeAfterPlayback)
            {
                visual.FrozenRootPosition = visual.HasSentState ? visual.LastPosition : visual.Boss?.Position.Vector ?? Vector3.zero;
                visual.FrozenRootRotation = visual.HasSentState ? visual.LastRotation : Quaternion.identity;
            }
        }

        private static bool TryStartDeathClip(NPCID bossId)
        {
            if (!ActiveVisuals.TryGetValue(bossId, out var visual) ||
                visual?.Visual?.Clips == null ||
                visual.Visual.Clips.Count == 0)
            {
                return false;
            }

            var clip = FindClipByKeys(visual.Visual.Clips, "death", "die", "dead");
            if (clip == null)
                return false;

            BeginOneShotClip(
                visual,
                clip,
                NormalizeClipKey(clip.Name, "death"),
                removeAfterPlayback: true);
            return true;
        }

        private static bool ApplyRigClipAnimation(
            BossVisualRigBakeService.VisualRigAnimationClip clip,
            float clipTimeSeconds,
            int partIndex,
            ref Vector3 localPosition,
            ref Quaternion localRotation,
            ref Vector3 localScale)
        {
            if (clip == null || clip.DurationSeconds <= 0.0001f || !clip.Tracks.TryGetValue(partIndex, out var track) || track == null)
                return false;

            var clipTime = clipTimeSeconds;
            if (clip.DurationSeconds > 0.0001f)
                clipTime = clipTime % clip.DurationSeconds;

            var applied = false;

            if (track.TranslationTimes != null && track.TranslationValues != null &&
                track.TranslationTimes.Length > 0 && track.TranslationValues.Length > 0)
            {
                localPosition = SampleVector3Track(track.TranslationTimes, track.TranslationValues, clipTime);
                applied = true;
            }

            if (track.RotationTimes != null && track.RotationValues != null &&
                track.RotationTimes.Length > 0 && track.RotationValues.Length > 0)
            {
                localRotation = SampleQuaternionTrack(track.RotationTimes, track.RotationValues, clipTime);
                applied = true;
            }

            if (track.ScaleTimes != null && track.ScaleValues != null &&
                track.ScaleTimes.Length > 0 && track.ScaleValues.Length > 0)
            {
                localScale = SampleVector3Track(track.ScaleTimes, track.ScaleValues, clipTime);
                applied = true;
            }

            return applied;
        }

        private static Vector3 SampleVector3Track(float[] times, Vector3[] values, float clipTime)
        {
            if (times == null || values == null || times.Length == 0 || values.Length == 0)
                return Vector3.zero;

            if (times.Length == 1 || values.Length == 1 || clipTime <= times[0])
                return values[0];

            var lastIndex = System.Math.Min(times.Length, values.Length) - 1;
            if (clipTime >= times[lastIndex])
                return values[lastIndex];

            for (var i = 0; i < lastIndex; i++)
            {
                var startTime = times[i];
                var endTime = times[i + 1];
                if (clipTime < startTime || clipTime > endTime)
                    continue;

                var span = System.Math.Max(0.0001f, endTime - startTime);
                var t = Mathf.Clamp01((clipTime - startTime) / span);
                return Vector3.Lerp(values[i], values[i + 1], t);
            }

            return values[lastIndex];
        }

        private static Quaternion SampleQuaternionTrack(float[] times, Quaternion[] values, float clipTime)
        {
            if (times == null || values == null || times.Length == 0 || values.Length == 0)
                return Quaternion.identity;

            if (times.Length == 1 || values.Length == 1 || clipTime <= times[0])
                return values[0];

            var lastIndex = System.Math.Min(times.Length, values.Length) - 1;
            if (clipTime >= times[lastIndex])
                return values[lastIndex];

            for (var i = 0; i < lastIndex; i++)
            {
                var startTime = times[i];
                var endTime = times[i + 1];
                if (clipTime < startTime || clipTime > endTime)
                    continue;

                var span = System.Math.Max(0.0001f, endTime - startTime);
                var t = Mathf.Clamp01((clipTime - startTime) / span);
                return Quaternion.Slerp(values[i], values[i + 1], t);
            }

            return values[lastIndex];
        }

        private static Vector3 GetAnimatedRootOffset(ActiveBossVisual visual, float timeSeconds, float movementAlpha)
        {
            var heavyBoss = string.Equals(visual.Visual.BossName, "Bulging", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(visual.Visual.BossName, "Juggernaut", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(visual.Visual.BossName, "Putrid Corpse", StringComparison.OrdinalIgnoreCase);
            var idleWave = Mathf.Sin(timeSeconds * 1.4f) * (heavyBoss ? 0.010f : 0.007f);
            var walkCycle = timeSeconds * Mathf.Lerp(1.8f, heavyBoss ? 4.2f : 5.4f, movementAlpha);
            var stepBob = Mathf.Abs(Mathf.Sin(walkCycle)) * Mathf.Lerp(0.0f, heavyBoss ? 0.070f : 0.050f, movementAlpha);
            return new Vector3(0f, idleWave + stepBob, 0f);
        }

        private static Vector3 GetWholeBodyWalkOffset(
            float timeSeconds,
            float movementAlpha,
            Vector3 forwardDirection,
            Vector3 rightDirection)
        {
            if (movementAlpha <= 0.01f)
                return Vector3.zero;

            var walkCycle = timeSeconds * Mathf.Lerp(1.8f, 5.2f, movementAlpha);
            var sideSway = Mathf.Sin(walkCycle) * Mathf.Lerp(0.0f, 0.025f, movementAlpha);
            var forwardPulse = Mathf.Sin(walkCycle * 2f) * Mathf.Lerp(0.0f, 0.010f, movementAlpha);

            return (rightDirection * sideSway) + (forwardDirection * forwardPulse);
        }

        private static void ApplyProceduralAnimation(
            ActiveBossVisual visual,
            RegisteredBossVisualPart definition,
            float timeSeconds,
            float movementAlpha,
            ref Vector3 localPosition,
            ref Quaternion localRotation)
        {
            if (!string.Equals(visual.Visual.BossName, "Bulging", StringComparison.OrdinalIgnoreCase))
                return;

            ApplyBulgingAnimation(definition.Name ?? string.Empty, timeSeconds, movementAlpha, ref localPosition, ref localRotation);
        }

        private static void ApplyWorldProceduralAnimation(
            ActiveBossVisual visual,
            RegisteredBossVisualPart definition,
            float timeSeconds,
            float movementAlpha,
            Vector3 forwardDirection,
            Vector3 rightDirection,
            ref Vector3 worldPosition,
            ref Quaternion worldRotation)
        {
            if (!string.Equals(visual.Visual.BossName, "Bulging", StringComparison.OrdinalIgnoreCase))
                return;

            ApplyBulgingWorldAnimation(
                definition.Name ?? string.Empty,
                timeSeconds,
                movementAlpha,
                forwardDirection,
                rightDirection,
                ref worldPosition,
                ref worldRotation);
        }

        private static void ApplyBulgingAnimation(
            string partName,
            float timeSeconds,
            float movementAlpha,
            ref Vector3 localPosition,
            ref Quaternion localRotation)
        {
            static Quaternion ApplyRigRotation(float xDegrees, float yDegrees, float zDegrees, Quaternion baseRotation)
            {
                return Quaternion.Euler(xDegrees, yDegrees, zDegrees) * baseRotation;
            }

            var normalizedName = partName ?? string.Empty;
            var isLeft = normalizedName.IndexOf("L", StringComparison.OrdinalIgnoreCase) >= 0;
            var isRight = normalizedName.IndexOf("R", StringComparison.OrdinalIgnoreCase) >= 0;
            var phase = isRight ? Mathf.PI : 0f;
            var side = isLeft ? -1f : (isRight ? 1f : 0f);

            var idleCycle = timeSeconds * 1.6f;
            var walkCycle = timeSeconds * Mathf.Lerp(1.8f, 5.4f, movementAlpha);
            var walkSin = Mathf.Sin(walkCycle + phase);
            var oppositeWalkSin = Mathf.Sin(walkCycle + phase + Mathf.PI);
            var armSwing = walkSin * Mathf.Lerp(1f, 5f, movementAlpha);
            var legSwing = oppositeWalkSin * Mathf.Lerp(0.2f, 0.8f, movementAlpha);
            var kneeBend = Mathf.Max(0f, oppositeWalkSin) * Mathf.Lerp(0f, 0.6f, movementAlpha);
            var footLift = Mathf.Max(0f, oppositeWalkSin) * Mathf.Lerp(0.0f, 0.008f, movementAlpha);
            var torsoRoll = Mathf.Sin(walkCycle) * Mathf.Lerp(0.5f, 2.0f, movementAlpha);
            var torsoYaw = Mathf.Sin(walkCycle) * Mathf.Lerp(0.25f, 1.1f, movementAlpha);
            var idleBreath = Mathf.Sin(idleCycle) * 2f;
            var idleNod = Mathf.Sin(idleCycle * 0.5f) * 1.25f;

            if (normalizedName.StartsWith("Head", StringComparison.OrdinalIgnoreCase))
            {
                localRotation = ApplyRigRotation(idleBreath + idleNod, torsoYaw * 0.4f, 0f, localRotation);
                return;
            }

            if (normalizedName.StartsWith("Torso", StringComparison.OrdinalIgnoreCase) ||
                normalizedName.StartsWith("Middle", StringComparison.OrdinalIgnoreCase) ||
                normalizedName.StartsWith("Hips", StringComparison.OrdinalIgnoreCase))
            {
                localRotation = ApplyRigRotation(idleBreath * 0.4f, torsoYaw, torsoRoll, localRotation);
                return;
            }

            if (normalizedName.IndexOf("UpperArm", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                localPosition.y += Mathf.Abs(walkSin) * 0.0035f;
                localRotation = ApplyRigRotation(-armSwing * 0.055f, 0f, 0f, localRotation);
                return;
            }

            if (normalizedName.IndexOf("LowerArm", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                localRotation = ApplyRigRotation(-(armSwing * 0.010f), 0f, 0f, localRotation);
                return;
            }

            if (normalizedName.IndexOf("Hand", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return;
            }

            if (normalizedName.IndexOf("UpperLeg", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                localPosition.y += footLift * 0.05f;
                localRotation = ApplyRigRotation(legSwing * 0.02f, 0f, 0f, localRotation);
                return;
            }

            if (normalizedName.IndexOf("LowerLeg", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                localPosition.y += footLift * 0.10f;
                localRotation = ApplyRigRotation(-kneeBend * 0.02f, 0f, 0f, localRotation);
                return;
            }

            if (normalizedName.IndexOf("Foot", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                localPosition.y += footLift * 0.20f;
                localRotation = ApplyRigRotation(-(legSwing * 0.01f), 0f, 0f, localRotation);
            }
        }

        private static void ApplyBulgingWorldAnimation(
            string partName,
            float timeSeconds,
            float movementAlpha,
            Vector3 forwardDirection,
            Vector3 rightDirection,
            ref Vector3 worldPosition,
            ref Quaternion worldRotation)
        {
            var normalizedName = partName ?? string.Empty;
            var isLeft = normalizedName.IndexOf("L", StringComparison.OrdinalIgnoreCase) >= 0;
            var isRight = normalizedName.IndexOf("R", StringComparison.OrdinalIgnoreCase) >= 0;
            var phase = isRight ? Mathf.PI : 0f;
            var side = isLeft ? -1f : (isRight ? 1f : 0f);
            var walkCycle = timeSeconds * Mathf.Lerp(1.8f, 5.1f, movementAlpha);
            var armSwing = Mathf.Sin(walkCycle + phase);
            var legSwing = Mathf.Sin(walkCycle + phase + Mathf.PI);
            var footLift = Mathf.Max(0f, Mathf.Sin(walkCycle + phase + Mathf.PI));
            var forward = forwardDirection.sqrMagnitude > 0.0001f ? forwardDirection.normalized : Vector3.forward;
            var right = rightDirection.sqrMagnitude > 0.0001f ? rightDirection.normalized : Vector3.right;
            var up = Vector3.up;

            if (normalizedName.IndexOf("UpperArm", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                worldPosition += forward * (armSwing * Mathf.Lerp(0.0f, 0.028f, movementAlpha));
                worldPosition += up * (Mathf.Abs(armSwing) * 0.0035f);
                return;
            }

            if (normalizedName.IndexOf("LowerArm", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                worldPosition += forward * (armSwing * Mathf.Lerp(0.0f, 0.010f, movementAlpha));
                worldPosition += up * (Mathf.Abs(armSwing) * 0.001f);
                return;
            }

            if (normalizedName.IndexOf("Hand", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                worldPosition += forward * (armSwing * Mathf.Lerp(0.0f, 0.045f, movementAlpha));
                worldPosition += up * (Mathf.Abs(armSwing) * 0.0025f);
                return;
            }

            if (normalizedName.IndexOf("UpperLeg", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                worldPosition += forward * (legSwing * Mathf.Lerp(0.0f, 0.012f, movementAlpha));
                return;
            }

            if (normalizedName.IndexOf("LowerLeg", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                worldPosition += forward * (legSwing * Mathf.Lerp(0.0f, 0.016f, movementAlpha));
                worldPosition += up * (footLift * Mathf.Lerp(0.0f, 0.010f, movementAlpha));
                return;
            }

            if (normalizedName.IndexOf("Foot", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                worldPosition += forward * (legSwing * Mathf.Lerp(0.0f, 0.020f, movementAlpha));
                worldPosition += up * (footLift * Mathf.Lerp(0.0f, 0.014f, movementAlpha));
            }
        }

        private static Quaternion GetBossRotation(Vector3 normalizedDirection, float yawOffsetDegrees)
        {
            if (normalizedDirection.sqrMagnitude <= 0.0001f)
                return Quaternion.Euler(0f, yawOffsetDegrees, 0f);

            return Quaternion.LookRotation(normalizedDirection, Vector3.up) * Quaternion.Euler(0f, yawOffsetDegrees, 0f);
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

        private static void RemoveVisual(NPCID bossId, Vector3 fallbackPosition)
        {
            if (!ActiveVisuals.TryGetValue(bossId, out var visual) || visual?.Visual == null)
            {
                ActiveVisuals.Remove(bossId);
                return;
            }

            var removalPosition = visual.HasSentState
                ? visual.LastPosition
                : fallbackPosition;

            if (visual.Visual.IsRig)
            {
                for (var i = 0; i < visual.Parts.Count; i++)
                {
                    var part = visual.Parts[i];
                    if (!part.Definition.IsRenderable)
                        continue;

                    var partRemovalPosition = part.HasSentState ? part.LastPosition : removalPosition;
                    part.ClientObject.SendRemoval(partRemovalPosition, part.Definition.Settings);
                }
            }
            else if (visual.ClientObject != null)
            {
                visual.ClientObject.SendRemoval(removalPosition, visual.Visual.Settings);
            }

            ActiveVisuals.Remove(bossId);
        }

        private static void RecoverMissingBossVisuals()
        {
            var allMonsters = MonsterManager.GetAllMonsters();
            if (allMonsters == null || allMonsters.Count == 0)
                return;

            foreach (var pair in allMonsters)
            {
                var npcId = new NPCID(pair.Key);
                if (ActiveVisuals.ContainsKey(npcId))
                    continue;

                if (!(pair.Value is IPandaBoss boss) || !(pair.Value is Zombie zombie))
                    continue;

                if (!RegisteredVisuals.TryGetValue(boss.name, out var visual))
                    continue;

                var activeVisual = new ActiveBossVisual
                {
                    BossId = npcId,
                    Boss = zombie,
                    Visual = visual,
                    ClientObject = visual.IsRig ? default : new ClientMeshedObject(visual.Type)
                };

                if (visual.IsRig)
                {
                    for (var i = 0; i < visual.Parts.Count; i++)
                    {
                        var clientObject = visual.Parts[i].IsRenderable
                            ? new ClientMeshedObject(visual.Parts[i].Type)
                            : default;

                        activeVisual.Parts.Add(new ActiveBossVisualPart
                        {
                            Definition = visual.Parts[i],
                            ClientObject = clientObject
                        });
                    }
                }

                ActiveVisuals[zombie.ID] = activeVisual;
            }
        }

        private static void BroadcastTypeTable()
        {
            if (ActiveVisuals.Count == 0)
                return;

            foreach (var player in Players.ConnectedPlayers)
            {
                if (!PlayerUiGuard.CanSendStable(player))
                    continue;

                MeshedObjectType.SendTable(player);
                var playerKey = GetPlayerKey(player);
                if (playerKey >= 0)
                    SyncedPlayerIds.Add(playerKey);
            }
        }

        private static void FlushPendingPlayerSyncs()
        {
            if (PendingPlayerSyncs.Count == 0 || ActiveVisuals.Count == 0)
                return;

            List<int> syncedPlayers = null;
            foreach (var player in Players.ConnectedPlayers)
            {
                if (!PlayerUiGuard.CanSendStable(player))
                    continue;

                var playerKey = GetPlayerKey(player);
                if (playerKey < 0 || !PendingPlayerSyncs.Contains(playerKey))
                    continue;

                MeshedObjectType.SendTable(player);
                SyncedPlayerIds.Add(playerKey);
                syncedPlayers ??= new List<int>();
                syncedPlayers.Add(playerKey);
            }

            if (syncedPlayers == null)
                return;

            for (var i = 0; i < syncedPlayers.Count; i++)
                PendingPlayerSyncs.Remove(syncedPlayers[i]);

            foreach (var visual in ActiveVisuals.Values)
                SendOrUpdateVisual(visual, force: true);
        }

        private static void QueueUnsyncedConnectedPlayersForVisualSync()
        {
            if (ActiveVisuals.Count == 0)
                return;

            foreach (var player in Players.ConnectedPlayers)
            {
                if (!PlayerUiGuard.CanSendStable(player))
                    continue;

                var playerKey = GetPlayerKey(player);
                if (playerKey < 0 || SyncedPlayerIds.Contains(playerKey))
                    continue;

                PendingPlayerSyncs.Add(playerKey);
            }
        }

        private static int GetPlayerKey(Players.Player player)
        {
            return player != null ? player.ID.ID.ID : -1;
        }

        public static void NotifyBossAttack(NPCID bossId)
        {
            if (!ActiveVisuals.TryGetValue(bossId, out var visual) ||
                visual?.Visual?.Clips == null ||
                visual.Visual.Clips.Count == 0)
            {
                return;
            }

            var clip = FindClipByKeys(visual.Visual.Clips, "attack", "punch", "melee", "action");
            if (clip == null)
                return;

            BeginOneShotClip(
                visual,
                clip,
                NormalizeClipKey(clip.Name, "attack"),
                removeAfterPlayback: false);
        }

        public static bool TryGetBossAimPoint(NPCID bossId, Vector3 fallback, out Vector3 aimPoint)
        {
            if (ActiveVisuals.TryGetValue(bossId, out var activeVisual) &&
                activeVisual?.Visual != null &&
                activeVisual.HasSentState)
            {
                var center = activeVisual.Visual.LocalBoundsCenter;
                aimPoint = activeVisual.LastPosition + (activeVisual.LastRotation * center);
                return true;
            }

            aimPoint = fallback;
            return false;
        }

        public static bool TryRaycastBoss(Vector3 origin, Vector3 direction, float maxDistance, out Zombie boss, out string bossName, out Vector3 hitPoint)
        {
            boss = null;
            bossName = null;
            hitPoint = default;

            if (direction.sqrMagnitude <= 0.0001f)
                return false;

            var normalizedDirection = direction.normalized;
            var bestDistance = maxDistance;
            var found = false;

            foreach (var pair in ActiveVisuals)
            {
                var activeVisual = pair.Value;
                if (activeVisual?.Boss == null ||
                    activeVisual.Visual == null ||
                    !activeVisual.HasSentState ||
                    activeVisual.Boss.CurrentHealth <= 0f)
                {
                    continue;
                }

                var size = activeVisual.Visual.LocalBoundsSize;
                if (size.x <= 0.001f || size.y <= 0.001f || size.z <= 0.001f)
                    size = new Vector3(1.5f, 3f, 1.5f);

                var halfExtents = size * 0.5f;
                var center = activeVisual.Visual.LocalBoundsCenter;
                if (!TryIntersectOrientedBounds(origin, normalizedDirection, activeVisual.LastPosition, activeVisual.LastRotation, center, halfExtents, bestDistance, out var distance))
                    continue;

                bestDistance = distance;
                boss = activeVisual.Boss;
                bossName = activeVisual.Visual.BossName;
                hitPoint = origin + normalizedDirection * distance;
                found = true;
            }

            return found;
        }

        public static bool TryGetBossByMeshId(int meshId, out Zombie boss, out string bossName)
        {
            foreach (var pair in ActiveVisuals)
            {
                var activeVisual = pair.Value;
                if (activeVisual?.Boss == null || activeVisual.Visual == null)
                    continue;

                if (activeVisual.Visual.IsRig)
                {
                    var matched = false;
                    for (var i = 0; i < activeVisual.Parts.Count; i++)
                    {
                        if (!activeVisual.Parts[i].Definition.IsRenderable)
                            continue;

                        if (activeVisual.Parts[i].ClientObject.ObjectID.ID == meshId)
                        {
                            matched = true;
                            break;
                        }
                    }

                    if (!matched)
                        continue;
                }
                else
                {
                    if (activeVisual.ClientObject == null || activeVisual.ClientObject.ObjectID.ID != meshId)
                        continue;
                }

                boss = activeVisual.Boss;
                bossName = activeVisual.Visual.BossName;
                return true;
            }

            boss = null;
            bossName = null;
            return false;
        }

        public static bool TryGetPrimaryActiveBoss(out Zombie boss, out string bossName)
        {
            foreach (var pair in ActiveVisuals)
            {
                var activeVisual = pair.Value;
                if (activeVisual?.Boss == null || activeVisual.Visual == null || activeVisual.Boss.CurrentHealth <= 0f)
                    continue;

                boss = activeVisual.Boss;
                bossName = activeVisual.Visual.BossName;
                return true;
            }

            boss = null;
            bossName = null;
            return false;
        }

        private static bool TryIntersectOrientedBounds(
            Vector3 rayOrigin,
            Vector3 rayDirection,
            Vector3 worldPosition,
            Quaternion worldRotation,
            Vector3 localCenter,
            Vector3 halfExtents,
            float maxDistance,
            out float distance)
        {
            var inverseRotation = Quaternion.Inverse(worldRotation);
            var localOrigin = inverseRotation * (rayOrigin - worldPosition) - localCenter;
            var localDirection = inverseRotation * rayDirection;

            var min = -halfExtents;
            var max = halfExtents;
            var tMin = 0f;
            var tMax = maxDistance;

            if (!IntersectAxis(localOrigin.x, localDirection.x, min.x, max.x, ref tMin, ref tMax) ||
                !IntersectAxis(localOrigin.y, localDirection.y, min.y, max.y, ref tMin, ref tMax) ||
                !IntersectAxis(localOrigin.z, localDirection.z, min.z, max.z, ref tMin, ref tMax))
            {
                distance = 0f;
                return false;
            }

            distance = tMin >= 0f ? tMin : tMax;
            return distance >= 0f && distance <= maxDistance;
        }

        private static bool IntersectAxis(float origin, float direction, float min, float max, ref float tMin, ref float tMax)
        {
            if (Mathf.Abs(direction) <= 0.0001f)
                return origin >= min && origin <= max;

            var invDirection = 1f / direction;
            var t1 = (min - origin) * invDirection;
            var t2 = (max - origin) * invDirection;

            if (t1 > t2)
            {
                var temp = t1;
                t1 = t2;
                t2 = temp;
            }

            tMin = Mathf.Max(tMin, t1);
            tMax = Mathf.Min(tMax, t2);
            return tMax >= tMin;
        }

    }
}
