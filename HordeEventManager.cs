using AI;
using Chatting;
using Monsters;
using NPC;
using Pandaros.API.Entities;
using Shared;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Vector3Int = Pipliz.Vector3Int;

namespace BetterNecromancy
{
    [ModLoader.ModManager]
    public static class HordeEventManager
    {
        private sealed class HordeRuntime
        {
            public Colony Colony;
            public readonly Dictionary<NPCID, int> Members = new Dictionary<NPCID, int>();
            public NPCType BaseSpawnType;
            public int TotalStrength;
            public int InitialStrength;
            public int RemainingStrength;
            public int PendingAssignments;
            public double AssignmentOpenUntil;
            public string SpawnLabel;
            public int CurrentStage;
            public int TriggeredReinforcementPhases;
            public double NextBattleFxAt;
        }

        private sealed class HordePlan
        {
            public NPCType SpawnType;
            public int SpawnCount;
            public string SpawnLabel;
        }

        private sealed class PendingWaveRequest
        {
            public Colony Colony;
            public NPCType SpawnType;
            public int SpawnCount;
        }

        private sealed class ResolvedWaveResult
        {
            public readonly List<ResolvedSpawn> Spawned = new List<ResolvedSpawn>();
            public int FailedCount;
        }

        private sealed class ResolvedSpawn
        {
            public Colony Colony;
            public NPCType SpawnType;
            public Path Path;
        }

        private sealed class HordeSpawnAction : PathingManager.IPathingThreadAction
        {
            public void PathingThreadAction(PathingManager.PathingContext context)
            {
                while (true)
                {
                    PendingWaveRequest request;
                    lock (PendingWaveRequests)
                    {
                        if (PendingWaveRequests.Count == 0)
                            return;

                        request = PendingWaveRequests.Dequeue();
                    }

                    var result = ResolveWave(context, request);
                    lock (ResolvedWaveResults)
                        ResolvedWaveResults.Enqueue(result);
                }
            }
        }

        private const int HudSegments = 26;
        private const int MemberMissingCleanupThreshold = 5;
        private const double AssignmentWindowSeconds = 25d;
        private const double CleanupIntervalSeconds = 1d;
        private const double BattleFxIntervalSeconds = 7d;

        private static readonly Queue<PendingWaveRequest> PendingWaveRequests = new Queue<PendingWaveRequest>();
        private static readonly Queue<ResolvedWaveResult> ResolvedWaveResults = new Queue<ResolvedWaveResult>();
        private static readonly HordeSpawnAction SpawnAction = new HordeSpawnAction();
        private static readonly MethodInfo ResolveSpawnPathForBannerMethod =
            typeof(Pandaros.API.Monsters.MonsterManager).GetMethod("TryResolveSpawnPathForBanner", BindingFlags.Static | BindingFlags.NonPublic);

        private static HordeRuntime _activeHorde;
        private static double _nextCleanupAt;
        public static Colony LastCompletedColony { get; private set; }

        public static bool IsActive =>
            _activeHorde != null &&
            (_activeHorde.RemainingStrength > 0 ||
             _activeHorde.PendingAssignments > 0 ||
             _activeHorde.Members.Count > 0 ||
             HasPendingWaveWork() ||
             Pipliz.Time.SecondsSinceStartDouble <= _activeHorde.AssignmentOpenUntil + 2d);

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AfterWorldLoad, BetterNecromancy.ModEntry.Namespace + ".HordeEventManager.AfterWorldLoad")]
        public static void AfterWorldLoad()
        {
            Clear();
            LastCompletedColony = null;
            _nextCleanupAt = 0d;
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnMonsterSpawned, BetterNecromancy.ModEntry.Namespace + ".HordeEventManager.OnMonsterSpawned")]
        public static void OnMonsterSpawned(IMonster monster)
        {
            if (_activeHorde == null || !(monster is Zombie zombie) || monster is Pandaros.API.Monsters.IPandaBoss || !zombie.IsValid ||
                zombie.OriginalGoal != _activeHorde.Colony || _activeHorde.PendingAssignments <= 0 ||
                Pipliz.Time.SecondsSinceStartDouble > _activeHorde.AssignmentOpenUntil || _activeHorde.Members.ContainsKey(zombie.ID))
                return;

            _activeHorde.Members[zombie.ID] = 0;
            _activeHorde.PendingAssignments = Mathf.Max(0, _activeHorde.PendingAssignments - 1);
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnMonsterDied, BetterNecromancy.ModEntry.Namespace + ".HordeEventManager.OnMonsterDied")]
        public static void OnMonsterDied(IMonster monster)
        {
            if (_activeHorde == null || !(monster is Zombie zombie) || !_activeHorde.Members.Remove(zombie.ID))
                return;

            _activeHorde.RemainingStrength = Mathf.Max(0, _activeHorde.RemainingStrength - 1);
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnUpdate, BetterNecromancy.ModEntry.Namespace + ".HordeEventManager.OnUpdate")]
        public static void OnUpdate()
        {
            FlushResolvedWaveSpawns();

            if (_activeHorde == null || Pipliz.Time.SecondsSinceStartDouble < _nextCleanupAt)
                return;

            _nextCleanupAt = Pipliz.Time.SecondsSinceStartDouble + CleanupIntervalSeconds;

            if (_activeHorde.PendingAssignments > 0 && Pipliz.Time.SecondsSinceStartDouble > _activeHorde.AssignmentOpenUntil)
            {
                _activeHorde.TotalStrength = Mathf.Max(0, _activeHorde.TotalStrength - _activeHorde.PendingAssignments);
                _activeHorde.RemainingStrength = Mathf.Max(0, _activeHorde.RemainingStrength - _activeHorde.PendingAssignments);
                _activeHorde.PendingAssignments = 0;
            }

            CleanupDeadMembers();
            TryTriggerReinforcementPhase();
            TryPlayBattleFx();

            if (IsActive)
                return;

            CompleteHorde();
            Clear();
        }

        public static bool TryStartForPlayer(Players.Player requester, out string message)
        {
            if (!Pandaros.API.Monsters.MonsterManager.TryResolvePreferredCommandColonyForPlayer(requester, out var colony, out message))
                return false;

            return TryStartForColony(colony, out message);
        }

        public static bool TryStartAutomatic(out string message)
        {
            if (!TryResolveAutomaticColony(out var colony, out message))
                return false;

            return TryStartForColony(colony, out message);
        }

        public static void Clear()
        {
            _activeHorde = null;
            lock (PendingWaveRequests)
                PendingWaveRequests.Clear();
            lock (ResolvedWaveResults)
                ResolvedWaveResults.Clear();
        }

        public static bool TryGetHudState(out string titleText, out string barText, out string titleColor, out string barColor)
        {
            if (!IsActive)
            {
                titleText = null;
                barText = null;
                titleColor = null;
                barColor = null;
                return false;
            }

            var total = Mathf.Max(1, _activeHorde.TotalStrength);
            var remaining = Mathf.Clamp(_activeHorde.RemainingStrength, 0, total);
            var filled = Mathf.Clamp(Mathf.RoundToInt((remaining / (float)total) * HudSegments), 0, HudSegments);

            titleText = "Horde Assault  Stage " + Mathf.Max(1, _activeHorde.CurrentStage) + "  " + remaining + " / " + total + " Strength";
            barText = "[" + new string('=', filled) + new string('-', HudSegments - filled) + "]";
            titleColor = "#ffd7a8";
            barColor = "#ff9d4d";
            return true;
        }

        public static string GetStatusText()
        {
            return !IsActive
                ? "No horde active."
                : "Horde Assault active. Stage " + Mathf.Max(1, _activeHorde.CurrentStage) + ". Remaining strength: " + _activeHorde.RemainingStrength + " / " + Mathf.Max(1, _activeHorde.TotalStrength) + ".";
        }

        private static bool TryStartForColony(Colony colony, out string message)
        {
            colony = ResolvePrimaryHordeColony(colony);
            if (!CanRunHordeAtColony(colony))
            {
                message = "No valid main colony with a banner and stockpile is available for the horde assault.";
                return false;
            }

            if (ColonyState.IsPeacefulColony(colony))
            {
                message = "Horde Assault is disabled while the colony difficulty is Peaceful.";
                return false;
            }

            if (ServerManager.PathingManager == null)
            {
                message = "Pathing manager is not ready yet.";
                return false;
            }

            if (Pandaros.API.Monsters.MonsterManager.HasBossStateBusy)
            {
                message = "A boss event is already active or pending.";
                return false;
            }

            if (IsActive)
            {
                message = "A horde assault is already active.";
                return false;
            }

            var plan = BuildPlan(colony);
            _activeHorde = new HordeRuntime
            {
                Colony = colony,
                BaseSpawnType = plan.SpawnType,
                TotalStrength = plan.SpawnCount,
                InitialStrength = plan.SpawnCount,
                RemainingStrength = plan.SpawnCount,
                PendingAssignments = plan.SpawnCount,
                AssignmentOpenUntil = Pipliz.Time.SecondsSinceStartDouble + AssignmentWindowSeconds,
                SpawnLabel = plan.SpawnLabel,
                CurrentStage = 1,
                NextBattleFxAt = Pipliz.Time.SecondsSinceStartDouble + 1.2d
            };

            QueueWaveSpawns(colony, plan.SpawnType, plan.SpawnCount);
            PlayHordeStartFx(colony);
            _nextCleanupAt = 0d;
            message = "Horde Assault started: " + plan.SpawnCount + " " + plan.SpawnLabel + " zombies are converging on the colony.";
            return true;
        }

        private static HordePlan BuildPlan(Colony colony)
        {
            var followers = Mathf.Max(0, colony.FollowerCount);
            var colonyState = ColonyState.GetColonyState(colony);
            var rank = colonyState?.Difficulty?.Rank ?? 0;
            var reflectedThreat = TryResolveThreatScore(colony, colonyState);
            var hordeScore = followers + (rank * 80f) + reflectedThreat;
            var spawnCount = Mathf.Clamp(12 + Mathf.CeilToInt(followers * 0.05f) + Mathf.CeilToInt(rank * 3.5f) + Mathf.CeilToInt(reflectedThreat / 35f), 12, 96);

            if (hordeScore >= 2200f)
                return new HordePlan { SpawnType = MonsterSpawner.Monster25, SpawnCount = spawnCount, SpawnLabel = "cataclysmic" };
            if (hordeScore >= 1200f)
                return new HordePlan { SpawnType = MonsterSpawner.Monster25, SpawnCount = Mathf.Max(18, spawnCount), SpawnLabel = "siege" };
            if (hordeScore >= 550f)
                return new HordePlan { SpawnType = MonsterSpawner.Monster25, SpawnCount = Mathf.Max(16, spawnCount), SpawnLabel = "warband" };
            if (hordeScore >= 220f)
                return new HordePlan { SpawnType = MonsterSpawner.Monster25, SpawnCount = Mathf.Max(14, spawnCount), SpawnLabel = "raiding" };

            return new HordePlan { SpawnType = MonsterSpawner.Monster25, SpawnCount = spawnCount, SpawnLabel = "ravenous" };
        }

        private static void QueueWaveSpawns(Colony colony, NPCType spawnType, int count)
        {
            lock (PendingWaveRequests)
            {
                PendingWaveRequests.Enqueue(new PendingWaveRequest { Colony = colony, SpawnType = spawnType, SpawnCount = count });
            }

            ServerManager.PathingManager.QueueAction(SpawnAction);
        }

        private static ResolvedWaveResult ResolveWave(PathingManager.PathingContext context, PendingWaveRequest request)
        {
            var result = new ResolvedWaveResult();
            if (request?.Colony == null || request.Colony.Banners.Count == 0 || request.SpawnCount <= 0 || request.SpawnType == null)
                return result;

            for (var i = 0; i < request.SpawnCount; i++)
            {
                var banner = request.Colony.Banners[i % request.Colony.Banners.Count];
                if (!TryResolveSpawnPath(context, banner.Position, banner.SafeRadius, out var path))
                {
                    result.FailedCount++;
                    continue;
                }

                result.Spawned.Add(new ResolvedSpawn
                {
                    Colony = request.Colony,
                    SpawnType = request.SpawnType,
                    Path = path
                });
            }

            return result;
        }

        private static bool TryResolveSpawnPath(PathingManager.PathingContext context, Vector3Int bannerPosition, int safeRadius, out Path path)
        {
            path = null;
            if (ResolveSpawnPathForBannerMethod == null)
                return false;

            var args = new object[] { context, bannerPosition, safeRadius, true, null };
            var success = ResolveSpawnPathForBannerMethod.Invoke(null, args) is bool ok && ok;
            if (success)
                path = args[4] as Path;

            return success && path != null;
        }

        private static void FlushResolvedWaveSpawns()
        {
            while (true)
            {
                ResolvedWaveResult result;
                lock (ResolvedWaveResults)
                {
                    if (ResolvedWaveResults.Count == 0)
                        return;
                    result = ResolvedWaveResults.Dequeue();
                }

                for (var i = 0; i < result.Spawned.Count; i++)
                {
                    var spawn = result.Spawned[i];
                    if (spawn?.SpawnType == null || spawn.Colony == null || spawn.Path == null)
                    {
                        result.FailedCount++;
                        continue;
                    }

                    var zombie = new Zombie(spawn.SpawnType, spawn.Path, spawn.Colony);
                    ModLoader.Callbacks.OnMonsterSpawned.Invoke(zombie);
                    MonsterTracker.Add(zombie);
                }

                if (_activeHorde != null && result.FailedCount > 0)
                {
                    _activeHorde.TotalStrength = Mathf.Max(0, _activeHorde.TotalStrength - result.FailedCount);
                    _activeHorde.RemainingStrength = Mathf.Max(0, _activeHorde.RemainingStrength - result.FailedCount);
                    _activeHorde.PendingAssignments = Mathf.Max(0, _activeHorde.PendingAssignments - result.FailedCount);
                }
            }
        }

        private static void CleanupDeadMembers()
        {
            if (_activeHorde == null)
                return;

            if (Pipliz.Time.SecondsSinceStartDouble <= _activeHorde.AssignmentOpenUntil + 2d)
                return;

            var allMonsters = Pandaros.API.Monsters.MonsterManager.GetAllMonsters();
            var removals = default(List<NPCID>);
            var updates = default(List<KeyValuePair<NPCID, int>>);

            foreach (var pair in _activeHorde.Members)
            {
                if (TryGetMonster(pair.Key, allMonsters, out var monster) && monster is Zombie zombie && zombie.IsValid && zombie.CurrentHealth > 0f)
                {
                    if (pair.Value != 0)
                    {
                        if (updates == null)
                            updates = new List<KeyValuePair<NPCID, int>>();
                        updates.Add(new KeyValuePair<NPCID, int>(pair.Key, 0));
                    }
                    continue;
                }

                var missingCount = pair.Value + 1;
                if (missingCount < MemberMissingCleanupThreshold)
                {
                    if (updates == null)
                        updates = new List<KeyValuePair<NPCID, int>>();
                    updates.Add(new KeyValuePair<NPCID, int>(pair.Key, missingCount));
                    continue;
                }

                if (removals == null)
                    removals = new List<NPCID>();
                removals.Add(pair.Key);
            }

            if (updates != null)
            {
                for (var i = 0; i < updates.Count; i++)
                    _activeHorde.Members[updates[i].Key] = updates[i].Value;
            }

            if (removals == null)
                return;

            for (var i = 0; i < removals.Count; i++)
            {
                if (_activeHorde.Members.Remove(removals[i]))
                    _activeHorde.RemainingStrength = Mathf.Max(0, _activeHorde.RemainingStrength - 1);
            }
        }

        private static bool HasPendingWaveWork()
        {
            lock (PendingWaveRequests)
            {
                if (PendingWaveRequests.Count > 0)
                    return true;
            }

            lock (ResolvedWaveResults)
                return ResolvedWaveResults.Count > 0;
        }

        private static void TryTriggerReinforcementPhase()
        {
            if (_activeHorde == null || _activeHorde.PendingAssignments > 0 || _activeHorde.InitialStrength <= 0)
                return;

            var remaining = _activeHorde.RemainingStrength;
            var mask = _activeHorde.TriggeredReinforcementPhases;
            var initial = _activeHorde.InitialStrength;

            if ((mask & 1) == 0 && remaining <= Mathf.CeilToInt(initial * 0.75f)) { TriggerReinforcementPhase(0); return; }
            if ((mask & 2) == 0 && remaining <= Mathf.CeilToInt(initial * 0.5f)) { TriggerReinforcementPhase(1); return; }
            if ((mask & 4) == 0 && remaining <= Mathf.CeilToInt(initial * 0.25f)) TriggerReinforcementPhase(2);
        }

        private static void TriggerReinforcementPhase(int phaseIndex)
        {
            var count = GetReinforcementCount(phaseIndex, _activeHorde.InitialStrength);
            var spawnType = GetReinforcementType(_activeHorde.BaseSpawnType, phaseIndex);
            QueueWaveSpawns(_activeHorde.Colony, spawnType, count);
            _activeHorde.TotalStrength += count;
            _activeHorde.RemainingStrength += count;
            _activeHorde.PendingAssignments += count;
            _activeHorde.AssignmentOpenUntil = Pipliz.Time.SecondsSinceStartDouble + AssignmentWindowSeconds;
            _activeHorde.TriggeredReinforcementPhases |= (1 << phaseIndex);
            _activeHorde.CurrentStage = Mathf.Max(_activeHorde.CurrentStage, phaseIndex + 2);
            PlayReinforcementFx(_activeHorde.Colony, phaseIndex);
            Broadcast("Horde Assault intensifies. Reinforcements arrive: +" + count + " " + GetReinforcementLabel(phaseIndex) + " zombies.");
        }

        private static int GetReinforcementCount(int phaseIndex, int initialStrength)
        {
            switch (phaseIndex)
            {
                case 0: return Mathf.Max(2, Mathf.CeilToInt(initialStrength * 0.18f));
                case 1: return Mathf.Max(2, Mathf.CeilToInt(initialStrength * 0.14f));
                case 2: return Mathf.Max(2, Mathf.CeilToInt(initialStrength * 0.1f));
                default: return 0;
            }
        }

        private static string GetReinforcementLabel(int phaseIndex)
        {
            switch (phaseIndex)
            {
                case 0: return "fresh";
                case 1: return "hardened";
                case 2: return "last-stand";
                default: return "reinforcing";
            }
        }

        private static NPCType GetReinforcementType(NPCType baseType, int phaseIndex)
        {
            if (phaseIndex <= 0)
                return MonsterSpawner.Monster25;
            if (baseType == MonsterSpawner.Monster25)
                return phaseIndex >= 2 ? MonsterSpawner.Monster100 : MonsterSpawner.Monster25;
            if (baseType == MonsterSpawner.Monster100)
                return phaseIndex >= 2 ? MonsterSpawner.Monster250 : MonsterSpawner.Monster100;
            if (baseType == MonsterSpawner.Monster250)
                return phaseIndex >= 2 ? MonsterSpawner.Monster1500 : MonsterSpawner.Monster250;
            return phaseIndex >= 2 ? MonsterSpawner.Monster5000 : baseType;
        }

        private static void TryPlayBattleFx()
        {
            if (_activeHorde == null ||
                Pipliz.Time.SecondsSinceStartDouble < _activeHorde.NextBattleFxAt ||
                PlayerUiGuard.ShouldDeferPlayerFacingEffects())
                return;

            _activeHorde.NextBattleFxAt = Pipliz.Time.SecondsSinceStartDouble + BattleFxIntervalSeconds;
            var allMonsters = Pandaros.API.Monsters.MonsterManager.GetAllMonsters();
            var played = 0;

            foreach (var pair in _activeHorde.Members)
            {
                if (!TryGetMonster(pair.Key, allMonsters, out var monster) || !(monster is Zombie zombie) || !zombie.IsValid)
                    continue;

                var core = zombie.Position.Vector + new Vector3(0f, 1f, 0f);
                ServerManager.SendExplosionEffect(core, 8f, 1.2f, 1f, 0.2f);
                ServerManager.SendParticleTrail(core + new Vector3(-0.4f, 0.4f, 0f), core + new Vector3(0.4f, -0.2f, 0f), 0.22f);
                played++;
                if (played >= 3)
                    break;
            }
        }

        private static void CompleteHorde()
        {
            if (_activeHorde?.Colony?.ColonyGroup == null)
                return;

            LastCompletedColony = _activeHorde.Colony;
            var rewardPoints = Mathf.Max(20, Mathf.RoundToInt(_activeHorde.TotalStrength * 3.5f));
            _activeHorde.Colony.ColonyGroup.AddColonyPoints(rewardPoints);
            _activeHorde.Colony.ColonyGroup.SendPointsUpdate();
            EventRewardManager.GrantToColony(_activeHorde.Colony, "HordeAssault", "Horde Assault");
            PlayHordeVictoryFx(_activeHorde.Colony);
            Broadcast("Horde Assault broken. The colony survives the rush and gains +" + rewardPoints + " colony points.");
        }

        private static void PlayHordeStartFx(Colony colony)
        {
            if (colony == null || PlayerUiGuard.ShouldDeferPlayerFacingEffects())
                return;

            for (var i = 0; i < colony.Banners.Count; i++)
            {
                var bannerCenter = colony.Banners[i].Position.Vector + new Vector3(0.5f, 0.5f, 0.5f);
                ServerManager.SendExplosionEffect(bannerCenter, 8f, 2.1f, 1f, 0.2f);
                ServerManager.SendParticleTrail(bannerCenter + Vector3.up * 2f, bannerCenter - Vector3.up * 0.3f, 0.42f);
            }
        }

        private static void PlayReinforcementFx(Colony colony, int phaseIndex)
        {
            if (colony == null || colony.Banners.Count == 0 || PlayerUiGuard.ShouldDeferPlayerFacingEffects())
                return;

            for (var i = 0; i < colony.Banners.Count; i++)
            {
                var bannerCenter = colony.Banners[i].Position.Vector + new Vector3(0.5f, 0.5f, 0.5f);
                ServerManager.SendExplosionEffect(bannerCenter, 8f, 1.8f + (phaseIndex * 0.3f), 1f, 0.2f);
                ServerManager.SendParticleTrail(bannerCenter + new Vector3(-0.8f, 1.3f, 0f), bannerCenter + new Vector3(0.8f, 0.1f, 0f), 0.34f);
                ServerManager.SendParticleTrail(bannerCenter + new Vector3(0f, 1.5f, -0.8f), bannerCenter + new Vector3(0f, 0.1f, 0.8f), 0.34f);
            }
        }

        private static void PlayHordeVictoryFx(Colony colony)
        {
            if (colony == null || colony.Banners.Count == 0 || PlayerUiGuard.ShouldDeferPlayerFacingEffects())
                return;

            for (var i = 0; i < colony.Banners.Count; i++)
            {
                var bannerCenter = colony.Banners[i].Position.Vector + new Vector3(0.5f, 0.5f, 0.5f);
                ServerManager.SendExplosionEffect(bannerCenter, 8f, 2.3f, 1f, 0.2f);
            }
        }

        private static bool TryResolveAutomaticColony(out Colony colony, out string message)
        {
            colony = null;
            message = null;
            var bestFollowers = -1;
            var seenPrimaryColonies = new HashSet<ColonyID>();
            var colonies = ServerManager.ColonyTracker.ColoniesByID.GetValueEnumerator();
            while (colonies.MoveNext())
            {
                var candidate = ResolvePrimaryHordeColony(colonies.Current);
                if (!CanRunHordeAtColony(candidate) || !seenPrimaryColonies.Add(candidate.ColonyID) || candidate.FollowerCount < bestFollowers)
                    continue;

                bestFollowers = candidate.FollowerCount;
                colony = candidate;
            }

            if (colony != null)
                return true;

            message = "No main colony with a banner and stockpile is available for a horde assault right now.";
            return false;
        }

        private static Colony ResolvePrimaryHordeColony(Colony colony)
        {
            if (colony == null)
                return null;

            return colony.ColonyGroup?.MainColony ?? colony;
        }

        private static bool CanRunHordeAtColony(Colony colony)
        {
            return colony != null &&
                   colony.Banners.Count > 0 &&
                   colony.ColonyGroup?.Stockpile != null &&
                   colony.ColonyGroup.MainColonyID == colony.ColonyID;
        }

        private static void Broadcast(string message)
        {
            if (string.IsNullOrEmpty(message))
                return;

            foreach (var player in Players.ConnectedPlayers)
            {
                if (PlayerUiGuard.CanSendStable(player))
                    PlayerToastManager.Show(player, message, "#f0dcb4", 5000L);
            }
        }

        private static float TryResolveThreatScore(Colony colony, ColonyState colonyState)
        {
            var direct = TryReadFloatMember(colonyState, "Threat", "ThreatLevel", "CurrentThreat", "MonsterThreat");
            if (direct > 0f) return direct;
            direct = TryReadFloatMember(colony, "Threat", "ThreatLevel", "CurrentThreat");
            if (direct > 0f) return direct;
            direct = TryReadFloatMember(colony?.ColonyGroup, "Threat", "ThreatLevel", "CurrentThreat");
            return Mathf.Max(0f, direct);
        }

        private static float TryReadFloatMember(object source, params string[] memberNames)
        {
            if (source == null || memberNames == null)
                return 0f;

            var type = source.GetType();
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            for (var i = 0; i < memberNames.Length; i++)
            {
                var property = type.GetProperty(memberNames[i], flags);
                if (property != null && TryConvertFloat(property.GetValue(source, null), out var propertyValue))
                    return propertyValue;

                var field = type.GetField(memberNames[i], flags);
                if (field != null && TryConvertFloat(field.GetValue(source), out var fieldValue))
                    return fieldValue;
            }

            return 0f;
        }

        private static bool TryConvertFloat(object value, out float number)
        {
            switch (value)
            {
                case float f: number = f; return true;
                case double d: number = (float)d; return true;
                case int i: number = i; return true;
                case long l: number = l; return true;
                default: number = 0f; return false;
            }
        }

        private static bool TryGetMonster(NPCID npcId, Dictionary<int, IMonster> allMonsters, out IMonster monster)
        {
            monster = null;
            if (allMonsters == null)
                return false;

            foreach (var pair in allMonsters)
            {
                if (!new NPCID(pair.Key).Equals(npcId))
                    continue;

                monster = pair.Value;
                return true;
            }

            return false;
        }
    }
}
