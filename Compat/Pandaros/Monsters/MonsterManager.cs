using AI;
using BlockEntities.Implementations;
using Chatting;
using BetterNecromancy;
using Monsters;
using NPC;
using Pandaros.API.Entities;
using Pipliz;
using Shared;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using CSRandom = Pipliz.Random;
using CSTime = Pipliz.Time;
using Vector3Int = Pipliz.Vector3Int;

namespace Pandaros.API.Monsters
{
    [ModLoader.ModManager]
    public class MonsterManager : PathingManager.IPathingThreadAction
    {
        private static readonly MonsterManager Instance = new MonsterManager();
        private static readonly List<IPandaBoss> Bosses = new List<IPandaBoss>();
        private static readonly Queue<IPandaBoss> PendingSpawns = new Queue<IPandaBoss>();
        private static readonly Dictionary<Colony, IPandaBoss> ActiveBosses = new Dictionary<Colony, IPandaBoss>();
        private static VanillaBossPathRequest _vanillaBossPathRequest;
        private static readonly List<global::Monsters.MonsterSpawner.SpawnJob> OrphanedVanillaBossPathJobs = new List<global::Monsters.MonsterSpawner.SpawnJob>();
        private static readonly Dictionary<System.Type, MethodInfo> BossUpdateMethodCache = new Dictionary<System.Type, MethodInfo>();
        private static readonly HashSet<NPCID> SuppressedBossDeaths = new HashSet<NPCID>();
        private static readonly HashSet<NPCID> BossUpdateFailuresLogged = new HashSet<NPCID>();
        private static readonly Dictionary<NPCID, BossKillCredit> BossKillCredits = new Dictionary<NPCID, BossKillCredit>();
        private static double _nextBossUpdateTime = double.MaxValue;
        private static double _queuedUntil = double.MaxValue;
        private static int _lastBossIndex = -1;
        private static Colony _forcedSpawnColony;
        private static Players.Player _forcedSpawnRequester;
        private static string _forcedSpawnBossName;
        private static string _spawnAttemptBossName;
        private static Colony _spawnAttemptColony;
        private static Players.Player _spawnAttemptRequester;
        private static bool _spawnAttemptForced;
        private static double _spawnAttemptActiveSince = double.MaxValue;
        private static int _spawnAttemptRetryCount;
        private readonly System.Random _pathingRandomSource = new System.Random();

        private IPandaBoss _currentBoss;

        public static bool BossActive { get; private set; }
        public static bool HasBossStateBusy => BossActive || ActiveBosses.Count > 0 || PendingSpawns.Count > 0 || _vanillaBossPathRequest != null || Instance._currentBoss != null;

        private const int MaxSpawnTriesPerBanner = 40;
        private const int ForcedSpawnTriesPerBanner = 180;
        private const int SpawnCandidateVerticalRadius = 10;
        private const int OuterBossSpawnMaxExtraRadius = 640;
        private const int ExtendedSpawnSearchRadius = 2048;
        private const float ExtendedSpawnWalkDistance = 12000f;
        private const int ExtremeSpawnSearchRadius = 8192;
        private const float ExtremeSpawnWalkDistance = 70000f;
        private const long BossKillCreditWindowMs = 15000L;
        private const double BossSpawnResolveDelaySeconds = 2d;
        private const double VanillaBossPathRequestTimeoutSeconds = 30d;

        private sealed class VanillaBossPathRequest
        {
            public IPandaBoss BossTemplate { get; set; }
            public Colony Colony { get; set; }
            public BannerTracker.Banner Banner { get; set; }
            public global::Monsters.MonsterSpawner.SpawnJob Job { get; set; }
            public double QueuedAtSeconds { get; set; }
            public double ReadyAtSeconds { get; set; }
        }

        private sealed class BossKillCredit
        {
            public Players.Player Player { get; set; }
            public long LastHitAtMs { get; set; }
        }

        public static void AddBoss(IPandaBoss boss)
        {
            if (boss == null)
                return;

            lock (Bosses)
            {
                Bosses.Add(boss);
            }
        }

        public static string[] GetRegisteredBossNames()
        {
            lock (Bosses)
            {
                return Bosses.Select(boss => boss.name).OrderBy(name => name).ToArray();
            }
        }

        public static string GetStatusText(Colony colony)
        {
            if (colony != null && ActiveBosses.TryGetValue(colony, out var colonyBoss) && colonyBoss != null)
            {
                return "Active boss: " + colonyBoss.name + " (" +
                    UnityEngine.Mathf.CeilToInt(colonyBoss.CurrentHealth) + "/" +
                    UnityEngine.Mathf.CeilToInt(colonyBoss.TotalHealth) + " HP). Active tracked: " +
                    ActiveBosses.Count + ". Pending spawns: " + PendingSpawns.Count + ".";
            }

            if (_currentBossOrPending(out var pendingName))
            {
                var resolveIn = _queuedUntil == double.MaxValue
                    ? -1
                    : UnityEngine.Mathf.Max(0, UnityEngine.Mathf.CeilToInt((float)(_queuedUntil - CSTime.SecondsSinceStartDouble)));

                return resolveIn >= 0
                    ? "Boss spawn pending: " + pendingName + ". Pending spawns: " + PendingSpawns.Count + ". Resolves in about " + resolveIn + "s."
                    : "Boss spawn pending: " + pendingName + ". Pending spawns: " + PendingSpawns.Count + ".";
            }

            if (ActiveBosses.Count > 0)
                return "Other colony has " + ActiveBosses.Count + " active boss event(s). Pending spawns: " + PendingSpawns.Count + ".";

            var nextSpawnIn = UnityEngine.Mathf.Max(0, UnityEngine.Mathf.CeilToInt((float)(_nextBossUpdateTime - CSTime.SecondsSinceStartDouble)));
            return "No active boss. Active tracked: 0. Pending spawns: " + PendingSpawns.Count + ". Next random boss roll in about " + nextSpawnIn + "s.";
        }

        public static string GetStatusTextForPlayer(Players.Player player)
        {
            if (TryResolvePreferredCommandColony(player, out var colony, out _))
                return GetStatusText(colony);

            return GetStatusText(player?.ActiveColony);
        }

        public static bool TryResolvePreferredCommandColonyForPlayer(Players.Player requester, out Colony colony, out string message)
        {
            return TryResolvePreferredCommandColony(requester, out colony, out message);
        }

        public static bool TrySpawnBossForPlayer(string bossName, Players.Player requester, out string message)
        {
            if (!TryResolvePreferredCommandColony(requester, out var colony, out message))
                return false;

            var redirectedToMainColony = requester?.ActiveColony != null && colony != null && requester.ActiveColony != colony;
            var spawned = TrySpawnBossForColony(bossName, colony, requester, out message);
            if (spawned && redirectedToMainColony)
                message += " Redirected to your main colony.";

            return spawned;
        }

        public static bool TrySpawnBossForColony(string bossName, Colony colony, Players.Player requester, out string message)
        {
            colony = ResolvePrimaryBossColony(colony);

            if (colony == null)
            {
                message = "You need an active colony to spawn a boss.";
                return false;
            }

            if (!CanBossSpawnAtColony(colony))
            {
                message = "Bosses can only spawn at your main colony with a stockpile. Outposts are blocked.";
                return false;
            }

            if (requester != null && !requester.OwnsColony(colony))
            {
                message = "Bosses can only be spawned for a colony you own.";
                return false;
            }

            if (ColonyState.IsPeacefulColony(colony))
            {
                message = "Boss spawns are disabled while the colony difficulty is Peaceful.";
                return false;
            }

            if (HordeEventManager.IsActive)
            {
                message = "A horde assault is already active.";
                return false;
            }

            if (BossActive || ActiveBosses.Count > 0 || PendingSpawns.Count > 0 || _vanillaBossPathRequest != null || Instance._currentBoss != null)
            {
                message = "There is already an active or pending boss event.";
                return false;
            }

            var boss = FindBossByName(bossName);
            if (boss == null)
            {
                message = "Unknown boss. Available: " + string.Join(", ", GetRegisteredBossNames());
                return false;
            }

            Instance._currentBoss = boss;
            _forcedSpawnColony = colony;
            _forcedSpawnRequester = requester;
            _forcedSpawnBossName = boss.name;
            BeginBossSpawnAttempt(boss.name, colony, requester, true);
            _queuedUntil = CSTime.SecondsSinceStartDouble + BossSpawnResolveDelaySeconds;
            BossActive = true;
            if (!TryQueueVanillaBossPathForColony(colony, true))
                ServerManager.PathingManager.QueueAction(Instance);

            message = "Boss force-spawn requested: " + boss.name + ".";
            return true;
        }

        public static bool TryKillActiveBossForPlayer(Players.Player requester, out string message)
        {
            TryResolvePreferredCommandColony(requester, out var colony, out _);
            return TryKillActiveBoss(colony, out message);
        }

        public static bool TryGetPrimaryActiveBoss(out Zombie boss, out string bossName)
        {
            foreach (var pair in ActiveBosses)
            {
                if (!(pair.Value is Zombie zombie) || !zombie.IsValid || zombie.CurrentHealth <= 0f)
                    continue;

                boss = zombie;
                bossName = pair.Value.name;
                return true;
            }

            var allMonsters = GetAllMonsters();
            if (allMonsters != null)
            {
                foreach (var pair in allMonsters)
                {
                    if (!(pair.Value is IPandaBoss pandaBoss) || !(pair.Value is Zombie zombie) || !zombie.IsValid || zombie.CurrentHealth <= 0f)
                        continue;

                    boss = zombie;
                    bossName = pandaBoss.name;
                    return true;
                }
            }

            boss = null;
            bossName = null;
            return false;
        }

        public static void OnBossTuningReloaded()
        {
            if (BossActive || ActiveBosses.Count > 0 || PendingSpawns.Count > 0 || _vanillaBossPathRequest != null || Instance._currentBoss != null)
                return;

            ScheduleNextBoss();
        }

        public static bool TrySetNextRandomBossRollInSeconds(int seconds, out string message)
        {
            if (BossActive || ActiveBosses.Count > 0 || PendingSpawns.Count > 0 || _vanillaBossPathRequest != null || Instance._currentBoss != null)
            {
                message = "A boss is already active or pending. Clear that state before moving the next random boss roll.";
                return false;
            }

            var clampedSeconds = Mathf.Clamp(seconds, 1, 86400);
            _nextBossUpdateTime = CSTime.SecondsSinceStartDouble + clampedSeconds;
            message = "Next random boss roll moved to about " + clampedSeconds + "s from now. It still respects night time, Peaceful, horde lock, and main-colony rules.";
            return true;
        }

        public static bool TryKillActiveBoss(Colony colony, out string message)
        {
            CleanupInactiveBosses();

            if (!TryResolveRelevantActiveBoss(colony, out _, out var boss, out message))
                return false;

            if (!TryForceEndBoss(boss, suppressDeathSideEffects: false))
            {
                message = "Failed to kill the active boss cleanly. Use /bnboss reset if it is stuck.";
                return false;
            }

            message = "Killed active boss: " + boss.name + ".";
            return true;
        }

        public static bool TryResetBossState(out string message)
        {
            var activeBosses = new List<IPandaBoss>(ActiveBosses.Values);
            var pendingQueuedCount = PendingSpawns.Count;
            var hadQueuedTemplate = Instance._currentBoss != null ? 1 : 0;
            var hadVanillaPathRequest = _vanillaBossPathRequest != null ? 1 : 0;
            var clearedActive = 0;

            ResetPendingBossState();

            for (var i = 0; i < activeBosses.Count; i++)
            {
                if (TryForceEndBoss(activeBosses[i], suppressDeathSideEffects: true))
                    clearedActive++;
            }

            CleanupInactiveBosses();
            BossActive = Instance._currentBoss != null || PendingSpawns.Count > 0 || _vanillaBossPathRequest != null || ActiveBosses.Count > 0;

            if (!BossActive)
                ScheduleNextBoss();

            var clearedPending = pendingQueuedCount + hadQueuedTemplate + hadVanillaPathRequest;
            if (clearedActive == 0 && clearedPending == 0)
            {
                message = "No active or pending boss state to reset.";
                return false;
            }

            message = "Boss state reset. Cleared " + clearedActive + " active boss(es) and " + clearedPending + " pending spawn slot(s).";
            return true;
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AfterWorldLoad, BetterNecromancy.ModEntry.Namespace + ".MonsterManager.AfterWorldLoad")]
        public static void AfterWorldLoad()
        {
            ActiveBosses.Clear();
            PendingSpawns.Clear();
            SuppressedBossDeaths.Clear();
            BossUpdateFailuresLogged.Clear();
            BossKillCredits.Clear();
            BossActive = false;
            Instance._currentBoss = null;
            _queuedUntil = double.MaxValue;
            _forcedSpawnColony = null;
            _forcedSpawnRequester = null;
            _forcedSpawnBossName = null;
            ClearBossSpawnAttempt();
            ClearVanillaBossPathRequest(true);
            ScheduleNextBoss();
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnUpdate, BetterNecromancy.ModEntry.Namespace + ".MonsterManager.OnUpdate")]
        public static void OnUpdate()
        {
            if (!World.Initialized)
                return;

            if (_nextBossUpdateTime == double.MaxValue)
                ScheduleNextBoss();

            CleanupInactiveBosses();
            UpdateBossSpawnAttemptState();
            FlushVanillaBossPathRequest();
            FlushOrphanedVanillaBossPathJobs();

            if (!BossActive &&
                ActiveBosses.Count == 0 &&
                !HordeEventManager.IsActive &&
                !TimeCycle.IsDay &&
                CSTime.SecondsSinceStartDouble >= _nextBossUpdateTime)
            {
                var boss = GetRandomBoss();

                if (boss != null)
                {
                    Instance._currentBoss = boss;
                    BeginBossSpawnAttempt(boss.name, null, null, false);
                    BossActive = true;
                    _queuedUntil = CSTime.SecondsSinceStartDouble + BossSpawnResolveDelaySeconds;
                    if (!TryQueueVanillaBossPathForAnyPrimaryColony())
                        ServerManager.PathingManager.QueueAction(Instance);
                }
                else
                {
                    ScheduleNextBoss();
                }
            }

            if (CSTime.SecondsSinceStartDouble >= _queuedUntil)
                SpawnPendingBosses();

            UpdateActiveBossAbilities();
            BossActive = Instance._currentBoss != null || PendingSpawns.Count > 0 || _vanillaBossPathRequest != null || ActiveBosses.Count > 0;
        }

        public void PathingThreadAction(PathingManager.PathingContext context)
        {
            if (_currentBoss == null)
                return;

            if (_forcedSpawnColony != null)
            {
                TryQueueBossForColony(context, ResolvePrimaryBossColony(_forcedSpawnColony));
                return;
            }

            var seenPrimaryColonies = new HashSet<ColonyID>();
            var colonies = ServerManager.ColonyTracker.ColoniesByID.GetValueEnumerator();
            while (colonies.MoveNext())
            {
                var primaryColony = ResolvePrimaryBossColony(colonies.Current);
                if (!CanBossSpawnAtColony(primaryColony) ||
                    !seenPrimaryColonies.Add(primaryColony.ColonyID) ||
                    !IsPrimaryBossColony(primaryColony))
                {
                    continue;
                }

                TryQueueBossForColony(context, primaryColony);
            }
        }

        private static IPandaBoss GetRandomBoss()
        {
            lock (Bosses)
            {
                if (Bosses.Count == 0)
                    return null;

                if (Bosses.Count == 1)
                {
                    _lastBossIndex = 0;
                    return Bosses[0];
                }

                var index = _lastBossIndex;
                while (index == _lastBossIndex)
                    index = CSRandom.Next(0, Bosses.Count);

                _lastBossIndex = index;
                return Bosses[index];
            }
        }

        private static void ScheduleNextBoss()
        {
            var tuning = BossTuning.Current;
            var minSeconds = System.Math.Max(60, tuning.RandomSpawnMinSeconds);
            var maxSeconds = System.Math.Max(minSeconds, tuning.RandomSpawnMaxSeconds);
            var delay = maxSeconds <= minSeconds
                ? minSeconds
                : CSRandom.Next(minSeconds, maxSeconds + 1);

            _nextBossUpdateTime = CSTime.SecondsSinceStartDouble + delay;
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnMonsterDied, BetterNecromancy.ModEntry.Namespace + ".MonsterManager.OnMonsterDied")]
        public static void OnMonsterDied(IMonster monster)
        {
            if (!(monster is Zombie zombie))
                return;

            if (!(monster is IPandaBoss boss))
            {
                GrantColonyPointsForKill(zombie.OriginalGoal, GetColonyPointsForMonsterKill(zombie));
                WorldEventManager.TryGrantAftermathKillReward(zombie.OriginalGoal);
                return;
            }

            var suppressSideEffects = SuppressedBossDeaths.Remove(zombie.ID);
            var colony = zombie.OriginalGoal;
            var creditedPlayer = TryPopBossKillCredit(monster as Zombie);
            if (colony != null)
                ActiveBosses.Remove(colony);

            ForgetBossRuntimeState(boss);
            ClearBossSpawnAttempt();

            if (!suppressSideEffects)
            {
                GrantColonyPointsForKill(colony, 100);
                GrantBossLoot(colony, boss);
                GrantFirstBossReward(colony, boss, creditedPlayer);
                AnnounceBossDeath(colony, boss);
            }

            BossActive = Instance._currentBoss != null || PendingSpawns.Count > 0 || _vanillaBossPathRequest != null || ActiveBosses.Count > 0;

            if (!BossActive && !HasBossSpawnAttempt())
                ScheduleNextBoss();
        }

        private static void GrantColonyPointsForKill(Colony colony, int amount)
        {
            var colonyGroup = colony?.ColonyGroup;
            if (colonyGroup == null || amount <= 0)
                return;

            var adjustedAmount = UnityEngine.Mathf.Max(1, UnityEngine.Mathf.RoundToInt(amount * WorldEventManager.GetColonyPointMultiplier()));
            colonyGroup.AddColonyPoints(adjustedAmount);
            colonyGroup.SendPointsUpdate();
        }

        private static int GetColonyPointsForMonsterKill(Zombie zombie)
        {
            if (zombie == null)
                return 1;

            var totalHealth = Mathf.Max(1f, zombie.TotalHealth);
            var basePoints = Mathf.Clamp(Mathf.CeilToInt(Mathf.Sqrt(totalHealth / 25f)), 1, 50);
            return basePoints + EliteZombieManager.GetBonusColonyPoints(zombie);
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnMonsterHit, BetterNecromancy.ModEntry.Namespace + ".MonsterManager.OnMonsterHit")]
        public static void OnMonsterHit(IMonster monster, ModLoader.OnHitData hitData)
        {
            if (hitData.ResultDamage <= 0f)
                return;

            if (monster is IPandaArmor armor)
            {
                if (CSRandom.NextFloat() <= armor.MissChance)
                {
                    hitData.ResultDamage = 0f;
                    return;
                }

                hitData.ResultDamage = DamageType.Physical.CalcDamage(armor.ElementalArmor, hitData.ResultDamage);

                if (armor.AdditionalResistance != null &&
                    armor.AdditionalResistance.TryGetValue(DamageType.Physical, out var resistance))
                {
                    hitData.ResultDamage -= hitData.ResultDamage * resistance;
                }
            }

            var colonyState = Entities.ColonyState.GetColonyState((monster as Zombie)?.OriginalGoal);
            if (colonyState != null && colonyState.Difficulty.MonsterDamageReduction > 0f)
                hitData.ResultDamage -= hitData.ResultDamage * colonyState.Difficulty.MonsterDamageReduction;

            hitData.ResultDamage *= WorldEventManager.GetMonsterDamageTakenMultiplier();
            hitData.ResultDamage = EliteZombieManager.ApplyIncomingDamageModifier(monster as Zombie, hitData.ResultDamage);

            if (hitData.ResultDamage < 0f)
                hitData.ResultDamage = 0f;

            TrackBossKillCredit(monster, hitData);
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnPlayerHit, BetterNecromancy.ModEntry.Namespace + ".MonsterManager.OnPlayerHit")]
        public static void OnPlayerHit(Players.Player player, ModLoader.OnHitData hitData)
        {
            if (hitData.ResultDamage <= 0f ||
                hitData.HitSourceType != ModLoader.OnHitData.EHitSourceType.Monster ||
                !(hitData.HitSourceObject is Zombie sourceZombie))
            {
                return;
            }

            var colonyState = Entities.ColonyState.GetColonyState(player.ActiveColony);
            if (colonyState != null && colonyState.Difficulty.MonsterDamage > 0f)
                hitData.ResultDamage += colonyState.Difficulty.MonsterDamage;

            hitData.ResultDamage *= WorldEventManager.GetMonsterDamageDealtMultiplier();
            hitData.ResultDamage = EliteZombieManager.ApplyOutgoingDamageModifier(sourceZombie, hitData.ResultDamage);

            if (hitData.ResultDamage > 0f &&
                hitData.HitSourceObject is Pandaros.Settlers.Monsters.Bosses.FallenRanger &&
                player.Health > 0f &&
                hitData.ResultDamage >= player.Health)
            {
                PlayerToastManager.Show(player, "[Fallen Ranger] Haha got you! Better luck next Time", "#f0c3c3", 4200L);
            }
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnNPCHit, BetterNecromancy.ModEntry.Namespace + ".MonsterManager.OnNPCHit")]
        public static void OnNPCHit(NPCBase npc, ModLoader.OnHitData hitData)
        {
            if (hitData.ResultDamage <= 0f ||
                hitData.HitSourceType != ModLoader.OnHitData.EHitSourceType.Monster ||
                !(hitData.HitSourceObject is Zombie sourceZombie))
            {
                return;
            }

            var colonyState = Entities.ColonyState.GetColonyState(npc.Colony);
            if (colonyState != null && colonyState.Difficulty.MonsterDamage > 0f)
                hitData.ResultDamage += colonyState.Difficulty.MonsterDamage;

            hitData.ResultDamage *= WorldEventManager.GetMonsterDamageDealtMultiplier();
            hitData.ResultDamage = EliteZombieManager.ApplyOutgoingDamageModifier(sourceZombie, hitData.ResultDamage);
        }

        public static Dictionary<int, IMonster> GetAllMonsters()
        {
            return typeof(MonsterTracker)
                .GetField("allMonsters", BindingFlags.Static | BindingFlags.NonPublic)
                ?.GetValue(null) as Dictionary<int, IMonster>;
        }

        private static void SpawnPendingBosses()
        {
            if (_vanillaBossPathRequest != null)
            {
                _queuedUntil = System.Math.Max(_vanillaBossPathRequest.ReadyAtSeconds, CSTime.SecondsSinceStartDouble + 1d);
                return;
            }

            _queuedUntil = double.MaxValue;
            var spawnedAny = false;
            var forcedSpawnRequester = _forcedSpawnRequester;
            var forcedSpawnBossName = _forcedSpawnBossName;
            var now = CSTime.SecondsSinceStartDouble;

            while (PendingSpawns.Count > 0)
            {
                var boss = PendingSpawns.Dequeue();
                ModLoader.Callbacks.OnMonsterSpawned.Invoke(boss);
                MonsterTracker.Add(boss);

                var colony = (boss as Zombie)?.OriginalGoal;
                if (colony != null)
                {
                    ActiveBosses[colony] = boss;
                    _spawnAttemptColony = ResolvePrimaryBossColony(colony);
                }

                AnnounceBossSpawn(colony, boss);
                spawnedAny = true;
            }

            Instance._currentBoss = null;
            BossActive = ActiveBosses.Count > 0;
            _forcedSpawnColony = null;
            _forcedSpawnRequester = null;
            _forcedSpawnBossName = null;

            if (spawnedAny && ActiveBosses.Count > 0)
                _spawnAttemptActiveSince = now;

            if (forcedSpawnRequester != null)
            {
                if (spawnedAny)
                    PlayerToastManager.Show(forcedSpawnRequester, "Boss spawned: " + forcedSpawnBossName + ".", "#f1e7c8", 4200L);
            }

            if (!spawnedAny && ActiveBosses.Count == 0)
            {
                if (TryScheduleBossSpawnRetry("No valid main-colony spawn path or spawn space was found."))
                    return;

                ScheduleNextBoss();
            }
        }

        private static void CleanupInactiveBosses()
        {
            if (ActiveBosses.Count == 0)
                return;

            var removedAny = false;
            var toRemove = new List<Colony>();

            foreach (var pair in ActiveBosses)
            {
                if (pair.Value == null || !pair.Value.IsValid || pair.Value.CurrentHealth <= 0f)
                    toRemove.Add(pair.Key);
            }

            for (var i = 0; i < toRemove.Count; i++)
            {
                if (ActiveBosses.TryGetValue(toRemove[i], out var boss))
                    ForgetBossRuntimeState(boss);

                ActiveBosses.Remove(toRemove[i]);
                removedAny = true;
            }

            if (removedAny && ActiveBosses.Count == 0 && Instance._currentBoss == null && PendingSpawns.Count == 0 && _vanillaBossPathRequest == null && !HasBossSpawnAttempt())
            {
                BossActive = false;
                ScheduleNextBoss();
            }
        }

        private static void AnnounceBossSpawn(Colony colony, IPandaBoss boss)
        {
            if (colony == null || boss == null)
                return;

            foreach (var player in Players.ConnectedPlayers)
            {
                if (!PlayerUiGuard.CanSendStable(player))
                    continue;

                if (!player.OwnsColony(colony))
                    continue;

                PlayerToastManager.Show(player, "[" + boss.name + "] " + boss.AnnouncementText, "#f4d7b2", 5000L);

                if (!string.IsNullOrEmpty(boss.AnnouncementAudio))
                    AudioManager.SendAudio(player.PositionStanding, boss.AnnouncementAudio);
            }
        }

        private static void AnnounceBossDeath(Colony colony, IPandaBoss boss)
        {
            if (boss == null)
                return;

            foreach (var player in Players.ConnectedPlayers)
            {
                if (!PlayerUiGuard.CanSendStable(player))
                    continue;

                if (colony != null && !player.OwnsColony(colony))
                    continue;

                PlayerToastManager.Show(player, "[" + boss.name + "] " + boss.DeathText, "#f1e7c8", 5000L);
            }
        }

        private static void GrantBossLoot(Colony colony, IPandaBoss boss)
        {
            if (colony?.ColonyGroup?.Stockpile == null)
                return;

            var grantedLoot = BossLootTable.RollLoot(boss.name);

            if (grantedLoot.Count == 0 && Pandaros.Settlers.Items.Mana.Item != null)
            {
                var tuning = BossTuning.Current;
                var minReward = System.Math.Max(1, tuning.BossLootManaMin);
                var maxReward = System.Math.Max(minReward, tuning.BossLootManaMax);
                var rewardAmount = maxReward <= minReward
                    ? minReward
                    : CSRandom.Next(minReward, maxReward + 1);

                grantedLoot.Add(new ResolvedBossLootDrop
                {
                    ItemIndex = Pandaros.Settlers.Items.Mana.Item.ItemIndex,
                    Amount = rewardAmount,
                    DisplayName = "Mana"
                });
            }

            if (grantedLoot.Count == 0)
                return;

            for (var i = 0; i < grantedLoot.Count; i++)
                colony.ColonyGroup.Stockpile.Add(grantedLoot[i].ItemIndex, grantedLoot[i].Amount);

            colony.ColonyGroup.Stockpile.SendToOwners();
            var lootSummary = string.Join(", ", grantedLoot.Select(drop => drop.Amount + " " + drop.DisplayName));
            var manaCrystalItemIndex = Pandaros.Settlers.Items.Magical.ManaCrystal.Item?.ItemIndex ?? 0;
            var droppedManaCrystal = manaCrystalItemIndex != 0 &&
                                     grantedLoot.Any(drop => drop.ItemIndex == manaCrystalItemIndex);

            foreach (var player in Players.ConnectedPlayers)
            {
                if (!PlayerUiGuard.CanSendStable(player))
                    continue;

                if (!player.OwnsColony(colony))
                    continue;

                var lootMessage = boss.name + " dropped " + lootSummary + " into your stockpile.";
                PlayerToastManager.Show(player, lootMessage, "#d8f0bf", 5000L);
                Chat.Send(player, "[Boss Loot] " + lootMessage);

                if (droppedManaCrystal && PlayerUiGuard.CanSendStable(player))
                {
                    Indicator.SendIconIndicatorToPlayer(
                        player.PositionStanding + UnityEngine.Vector3.up * 1.6f,
                        IndicatorState.NewItemIndicator(1.1f, manaCrystalItemIndex),
                        player);
                }
            }
        }

        private static void GrantFirstBossReward(Colony colony, IPandaBoss boss, Players.Player creditedPlayer)
        {
            if (boss == null || creditedPlayer == null)
                return;

            var rewardItem = Pandaros.Settlers.Items.Magical.HealthBooster.Item;
            if (rewardItem == null)
                return;

            if (colony?.ColonyGroup?.Stockpile == null)
            {
                PlayerToastManager.Show(creditedPlayer, "[" + boss.name + "] First boss reward could not be delivered because no colony stockpile was available. Defeat another boss near your colony to claim your one-time Health Booster.", "#f0c3c3", 5800L);
                return;
            }

            if (!PlayerMagicStateManager.TryMarkFirstBossRewardGranted(creditedPlayer))
                return;

            colony.ColonyGroup.Stockpile.Add(rewardItem.ItemIndex, 1);
            colony.ColonyGroup.Stockpile.SendToOwners();

            PlayerToastManager.Show(creditedPlayer, "[" + boss.name + "] First boss reward earned: Health Booster added to the colony stockpile.", "#d8f0bf", 5200L);

            if (PlayerUiGuard.CanSendStable(creditedPlayer))
            {
                Indicator.SendIconIndicatorToPlayer(
                    creditedPlayer.PositionStanding + UnityEngine.Vector3.up * 1.6f,
                    IndicatorState.NewItemIndicator(1.1f, rewardItem.ItemIndex),
                    creditedPlayer);
            }

            foreach (var player in Players.ConnectedPlayers)
            {
                if (!PlayerUiGuard.CanSendStable(player))
                    continue;

                if (player == creditedPlayer || !player.OwnsColony(colony))
                    continue;

                PlayerToastManager.Show(player, creditedPlayer.Name + " earned a one-time first boss reward: Health Booster added to the colony stockpile.", "#d8f0bf", 5200L);
            }
        }

        private static void TrackBossKillCredit(IMonster monster, ModLoader.OnHitData hitData)
        {
            if (!(monster is IPandaBoss) ||
                !(monster is Zombie zombie) ||
                !(hitData.HitSourceObject is Players.Player player) ||
                hitData.ResultDamage <= 0f)
            {
                return;
            }

            BossKillCredits[zombie.ID] = new BossKillCredit
            {
                Player = player,
                LastHitAtMs = CSTime.MillisecondsSinceStart
            };
        }

        private static Players.Player TryPopBossKillCredit(Zombie zombie)
        {
            if (zombie == null)
                return null;

            if (!BossKillCredits.TryGetValue(zombie.ID, out var credit))
                return null;

            BossKillCredits.Remove(zombie.ID);

            if (credit?.Player == null)
                return null;

            if (CSTime.MillisecondsSinceStart - credit.LastHitAtMs > BossKillCreditWindowMs)
                return null;

            return credit.Player;
        }

        private static void UpdateActiveBossAbilities()
        {
            if (ActiveBosses.Count == 0)
                return;

            var bosses = new List<IPandaBoss>(ActiveBosses.Values);

            for (var i = 0; i < bosses.Count; i++)
            {
                var boss = bosses[i];
                if (boss == null || !boss.IsValid || boss.CurrentHealth <= 0f)
                    continue;

                var type = boss.GetType();
                if (!BossUpdateMethodCache.TryGetValue(type, out var method))
                {
                    method = type.GetMethod("OnBossUpdate", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    BossUpdateMethodCache[type] = method;
                }

                try
                {
                    method?.Invoke(boss, System.Array.Empty<object>());
                }
                catch (System.Exception exception)
                {
                    if (boss is Zombie zombie && BossUpdateFailuresLogged.Add(zombie.ID))
                    {
                        var rootException = exception is TargetInvocationException invocationException && invocationException.InnerException != null
                            ? invocationException.InnerException
                            : exception;
                        Log.WriteError("BetterNecromancy boss update failed for " + boss.name + ": " + rootException);
                    }
                }
            }
        }

        private static void TryQueueBossForColony(PathingManager.PathingContext context, Colony colony)
        {
            if (colony == null || colony.Banners.Count == 0 || ActiveBosses.ContainsKey(colony) || _vanillaBossPathRequest != null || Instance._currentBoss == null)
                return;

            var forceCommandSpawn = colony == _forcedSpawnColony;
            if (TryQueueVanillaBossPathForColony(colony, forceCommandSpawn))
                return;

            var colonyState = Entities.ColonyState.GetColonyState(colony);

            if (colonyState?.IsPeaceful ?? false)
                return;

            if (!CanBossSpawnAtColony(colony))
                return;

            if (!forceCommandSpawn && !IsPrimaryBossColony(colony))
                return;

            if (!forceCommandSpawn &&
                (colonyState == null || !colonyState.BossesEnabled || colony.FollowerCount < Instance._currentBoss.MinColonists))
            {
                return;
            }

            for (var bannerIndex = 0; bannerIndex < colony.Banners.Count; bannerIndex++)
            {
                var banner = colony.Banners[bannerIndex];

                if (!TryResolveSpawnPathForBanner(context, banner.Position, banner.SafeRadius, forceCommandSpawn, out var path))
                    continue;

                PendingSpawns.Enqueue((IPandaBoss)Instance._currentBoss.GetNewInstance(path, colony));
                return;
            }
        }

        private static bool TryQueueVanillaBossPathForAnyPrimaryColony(bool forceCommandSpawn = false, double readyAtSeconds = double.NaN)
        {
            if (_forcedSpawnColony != null)
                return TryQueueVanillaBossPathForColony(ResolvePrimaryBossColony(_forcedSpawnColony), true, readyAtSeconds);

            var eligibleColonies = new List<Colony>();
            var seenPrimaryColonies = new HashSet<ColonyID>();
            var colonies = ServerManager.ColonyTracker.ColoniesByID.GetValueEnumerator();
            while (colonies.MoveNext())
            {
                var primaryColony = ResolvePrimaryBossColony(colonies.Current);
                if (!CanBossSpawnAtColony(primaryColony) ||
                    !seenPrimaryColonies.Add(primaryColony.ColonyID) ||
                    (!forceCommandSpawn && !IsPrimaryBossColony(primaryColony)))
                {
                    continue;
                }

                eligibleColonies.Add(primaryColony);
            }

            if (eligibleColonies.Count == 0)
                return false;

            var startIndex = GetSpawnAttemptRotationIndex(eligibleColonies.Count);
            for (var offset = 0; offset < eligibleColonies.Count; offset++)
            {
                var colony = eligibleColonies[(startIndex + offset) % eligibleColonies.Count];
                if (TryQueueVanillaBossPathForColony(colony, forceCommandSpawn, readyAtSeconds))
                    return true;
            }

            return false;
        }

        private static bool TryQueueVanillaBossPathForColony(Colony colony, bool forceCommandSpawn, double readyAtSeconds = double.NaN)
        {
            if (colony == null || colony.Banners.Count == 0 || ActiveBosses.ContainsKey(colony) || _vanillaBossPathRequest != null || Instance._currentBoss == null)
                return false;

            var colonyState = Entities.ColonyState.GetColonyState(colony);
            if (colonyState?.IsPeaceful ?? false)
                return false;

            if (!CanBossSpawnAtColony(colony))
                return false;

            if (!forceCommandSpawn && !IsPrimaryBossColony(colony))
                return false;

            if (!forceCommandSpawn &&
                (colonyState == null || !colonyState.BossesEnabled || colony.FollowerCount < Instance._currentBoss.MinColonists))
            {
                return false;
            }

            var bannerCount = colony.Banners.Count;
            var startIndex = GetSpawnAttemptRotationIndex(bannerCount);
            for (var offset = 0; offset < bannerCount; offset++)
            {
                var bannerIndex = (startIndex + offset) % bannerCount;
                if (TryQueueVanillaBossPathRequest(colony, colony.Banners[bannerIndex], readyAtSeconds))
                    return true;
            }

            return false;
        }

        private static bool TryQueueVanillaBossPathRequest(Colony colony, BannerTracker.Banner banner, double readyAtSeconds)
        {
            if (_vanillaBossPathRequest != null)
                return true;

            if (colony == null || banner == null || Instance._currentBoss == null || ServerManager.PathingManager == null)
                return false;

            if (!(MonsterTracker.MonsterSpawner is global::Monsters.MonsterSpawner vanillaSpawner))
                return false;

            global::Monsters.MonsterSpawner.SpawnJob job = null;
            try
            {
                job = new global::Monsters.MonsterSpawner.SpawnJob(vanillaSpawner, banner);
                var spawn = new global::Monsters.MonsterSpawner.QueuedSpawn
                {
                    TypeToSpawn = global::Monsters.MonsterSpawner.Monster25,
                    path = null,
                    result = global::Monsters.MonsterSpawner.QueuedSpawn.EResult.None
                };

                var now = CSTime.SecondsSinceStartDouble;
                if (double.IsNaN(readyAtSeconds) || readyAtSeconds < now)
                    readyAtSeconds = now + 1d;

                job.QueueSpawn(spawn);
                _vanillaBossPathRequest = new VanillaBossPathRequest
                {
                    BossTemplate = Instance._currentBoss,
                    Colony = colony,
                    Banner = banner,
                    Job = job,
                    QueuedAtSeconds = now,
                    ReadyAtSeconds = readyAtSeconds
                };
                _spawnAttemptColony = colony;
                _queuedUntil = readyAtSeconds;

                Log.Write("BetterNecromancy boss spawn queued through vanilla monster flowfield at banner " + banner.Position +
                          " for " + Instance._currentBoss.name + ".");
                return true;
            }
            catch (System.Exception exception)
            {
                try
                {
                    job?.Dispose();
                }
                catch
                {
                    // Dispose is best-effort here; the old direct resolver can still handle the spawn attempt.
                }

                Log.WriteWarning("BetterNecromancy could not queue vanilla boss path request near banner " + banner.Position +
                                 ": " + exception.Message + ". Falling back to direct boss path resolver.");
                return false;
            }
        }

        private static void FlushVanillaBossPathRequest()
        {
            var request = _vanillaBossPathRequest;
            if (request == null)
                return;

            var now = CSTime.SecondsSinceStartDouble;
            var job = request.Job;
            if (job == null)
            {
                ClearVanillaBossPathRequest(true);
                TryScheduleBossSpawnRetry("The vanilla monster spawner path request was lost before it completed.");
                return;
            }

            if (job.IsFlowfieldQueued)
            {
                if (now - request.QueuedAtSeconds > VanillaBossPathRequestTimeoutSeconds)
                {
                    ClearVanillaBossPathRequest(true);
                    TryScheduleBossSpawnRetry("The vanilla monster spawner path request timed out.");
                    return;
                }

                if (_queuedUntil <= now)
                    _queuedUntil = System.Math.Max(request.ReadyAtSeconds, now + 1d);

                return;
            }

            if (job.QueuedSpawns.Count == 0)
            {
                ClearVanillaBossPathRequest(true);
                TryScheduleBossSpawnRetry("The vanilla monster spawner returned no boss spawn candidate.");
                return;
            }

            ref var queuedSpawn = ref job.QueuedSpawns.GetAt(0);
            if (queuedSpawn.result == global::Monsters.MonsterSpawner.QueuedSpawn.EResult.None)
            {
                if (now - request.QueuedAtSeconds > VanillaBossPathRequestTimeoutSeconds)
                {
                    ClearVanillaBossPathRequest(true);
                    TryScheduleBossSpawnRetry("The vanilla monster spawner did not finish the boss path request.");
                    return;
                }

                if (_queuedUntil <= now)
                    _queuedUntil = System.Math.Max(request.ReadyAtSeconds, now + 1d);

                return;
            }

            if (queuedSpawn.result == global::Monsters.MonsterSpawner.QueuedSpawn.EResult.SuccessPath &&
                queuedSpawn.path != null &&
                request.BossTemplate != null &&
                request.Colony != null)
            {
                var path = queuedSpawn.path;
                queuedSpawn.path = null;
                var boss = request.BossTemplate.GetNewInstance(path, request.Colony) as IPandaBoss;
                if (boss == null)
                {
                    Path.Return(path);
                    ClearVanillaBossPathRequest(true);
                    TryScheduleBossSpawnRetry("The boss definition could not create a valid boss instance from the vanilla spawn path.");
                    return;
                }

                PendingSpawns.Enqueue(boss);
                _queuedUntil = request.ReadyAtSeconds > now ? request.ReadyAtSeconds : now;
                Log.Write("BetterNecromancy boss spawn path resolved by vanilla monster flowfield from " +
                          path.Start + " to " + path.Goal + " with " + path.Count + " nodes.");
                ClearVanillaBossPathRequest(true);
                return;
            }

            var result = queuedSpawn.result.ToString();
            ClearVanillaBossPathRequest(true);
            TryScheduleBossSpawnRetry("The vanilla monster spawner could not resolve a boss path (" + result + ").");
        }

        private static void ClearVanillaBossPathRequest(bool returnQueuedPaths)
        {
            var request = _vanillaBossPathRequest;
            _vanillaBossPathRequest = null;

            if (request?.Job == null)
                return;

            if (request.Job.IsFlowfieldQueued)
            {
                OrphanedVanillaBossPathJobs.Add(request.Job);
                return;
            }

            if (returnQueuedPaths)
                ReturnQueuedSpawnPaths(request.Job);

            try
            {
                request.Job.Dispose();
            }
            catch (System.Exception exception)
            {
                Log.WriteWarning("BetterNecromancy failed to dispose vanilla boss path request: " + exception.Message);
            }
        }

        private static void FlushOrphanedVanillaBossPathJobs()
        {
            if (OrphanedVanillaBossPathJobs.Count == 0)
                return;

            for (var i = OrphanedVanillaBossPathJobs.Count - 1; i >= 0; i--)
            {
                var job = OrphanedVanillaBossPathJobs[i];
                if (job != null && job.IsFlowfieldQueued)
                    continue;

                if (job != null)
                {
                    ReturnQueuedSpawnPaths(job);
                    try
                    {
                        job.Dispose();
                    }
                    catch (System.Exception exception)
                    {
                        Log.WriteWarning("BetterNecromancy failed to dispose completed orphaned vanilla boss path request: " + exception.Message);
                    }
                }

                OrphanedVanillaBossPathJobs.RemoveAt(i);
            }
        }

        private static void ReturnQueuedSpawnPaths(global::Monsters.MonsterSpawner.SpawnJob job)
        {
            if (job == null)
                return;

            for (var i = 0; i < job.QueuedSpawns.Count; i++)
            {
                ref var queuedSpawn = ref job.QueuedSpawns.GetAt(i);
                if (queuedSpawn.path == null)
                    continue;

                Path.Return(queuedSpawn.path);
                queuedSpawn.path = null;
            }
        }

        private static bool TryResolveSpawnPathForBanner(
            PathingManager.PathingContext context,
            Vector3Int bannerPosition,
            int bannerSafeRadius,
            bool forceCommandSpawn,
            out Path path)
        {
            path = null;
            var tuning = BossTuning.Current;
            var normalTries = tuning.BossSpawnNormalTriesPerBanner;
            var forcedTries = tuning.BossSpawnForcedTriesPerBanner;

            if (TryResolveOuterColonySpawnPath(context, bannerPosition, bannerSafeRadius, out path))
                return true;

            if (TryResolveSpawnPathForBannerCore(
                    context,
                    bannerPosition,
                    bannerSafeRadius,
                    forceCommandSpawn ? 900 : 260,
                    forceCommandSpawn ? 1400f : 700f,
                    forceCommandSpawn ? forcedTries : normalTries,
                    out path))
            {
                return true;
            }

            if (!forceCommandSpawn)
            {
                if (TryResolveSpawnPathForBannerCore(
                        context,
                        bannerPosition,
                        bannerSafeRadius,
                        420,
                        1100f,
                        normalTries + 40,
                        out path))
                {
                    return true;
                }
            }

            if (TryResolveSpawnPathForBannerCore(
                    context,
                    bannerPosition,
                    bannerSafeRadius,
                    640,
                    1600f,
                    forceCommandSpawn ? forcedTries + 60 : normalTries + 80,
                    out path))
            {
                return true;
            }

            if (TryResolveSpawnPathForBannerCore(
                    context,
                    bannerPosition,
                    bannerSafeRadius,
                    tuning.BossSpawnExtendedSearchRadius,
                    tuning.BossSpawnExtendedWalkDistance,
                    forceCommandSpawn ? forcedTries + 140 : normalTries + 180,
                    out path))
            {
                return true;
            }

            if (TryResolveSpawnPathForBannerCore(
                    context,
                    bannerPosition,
                    bannerSafeRadius,
                    tuning.BossSpawnExtremeSearchRadius,
                    tuning.BossSpawnExtremeWalkDistance,
                    forceCommandSpawn ? forcedTries + 260 : normalTries + 340,
                    out path))
            {
                return true;
            }

            var fallbackFound = TryResolveForcedFallbackSpawnPath(context, bannerPosition, bannerSafeRadius, out path);
            if (!fallbackFound)
            {
                Log.WriteWarning("BetterNecromancy boss spawn failed to resolve a path near banner " + bannerPosition +
                                 ". Tried local, extended, extreme, maze-trim and deterministic fallback rings. MazeSearchWalkDistance=" +
                                 tuning.BossSpawnMazeSearchWalkDistance + ", MazeNodeIterationLimit=" +
                                 tuning.BossSpawnMazeNodeIterationLimit + ", MaxLiveWalkDistance=" +
                                 tuning.BossSpawnMaxLiveWalkDistance + ".");
            }

            return fallbackFound;
        }

        private static bool TryResolveOuterColonySpawnPath(
            PathingManager.PathingContext context,
            Vector3Int bannerPosition,
            int bannerSafeRadius,
            out Path path)
        {
            path = null;

            var ringStart = System.Math.Max(10, bannerSafeRadius + 6);
            var ringDistances = new[]
            {
                ringStart,
                ringStart + 8,
                ringStart + 16,
                ringStart + 28,
                ringStart + 44,
                ringStart + 64,
                ringStart + 96,
                ringStart + 144,
                ringStart + 208,
                ringStart + 288,
                ringStart + 384,
                ringStart + 512,
                ringStart + OuterBossSpawnMaxExtraRadius
            }.Distinct().ToArray();
            var verticalOffsets = new[] { 0, 1, -1, 2, -2, 4, -4, 8, -8, 12, -12 };

            for (var distanceIndex = 0; distanceIndex < ringDistances.Length; distanceIndex++)
            {
                var distance = ringDistances[distanceIndex];
                var sampleCount = Mathf.Clamp(distance / 12, 16, 64);

                for (var sampleIndex = 0; sampleIndex < sampleCount; sampleIndex++)
                {
                    var angle = (System.Math.PI * 2d * sampleIndex) / sampleCount;
                    var dx = Mathf.RoundToInt((float)System.Math.Cos(angle) * distance);
                    var dz = Mathf.RoundToInt((float)System.Math.Sin(angle) * distance);

                    for (var yIndex = 0; yIndex < verticalOffsets.Length; yIndex++)
                    {
                        var candidate = bannerPosition + new Vector3Int(dx, verticalOffsets[yIndex], dz);
                        if (TryBuildPathFromCandidate(ref context, candidate, bannerPosition, bannerSafeRadius, "outer-ring", out path))
                            return true;
                    }
                }
            }

            return false;
        }

        private static bool TryResolveSpawnPathForBannerCore(
            PathingManager.PathingContext context,
            Vector3Int bannerPosition,
            int minSpawnRadius,
            int maxSpawnRadius,
            float maxSpawnWalkDistance,
            int maxTries,
            out Path path)
        {
            path = null;

            if (Instance.TryGetSpawnLocation(
                    context,
                    bannerPosition,
                    minSpawnRadius,
                    maxSpawnRadius,
                    maxSpawnWalkDistance,
                    maxTries,
                    out var spawnPosition) != SpawnLocationResult.Success)
            {
                return false;
            }

            return TryBuildBossPathFromFinalPosition(ref context, spawnPosition, bannerPosition, minSpawnRadius, "random-search", out path);
        }

        private static bool TryResolveForcedFallbackSpawnPath(
            PathingManager.PathingContext context,
            Vector3Int bannerPosition,
            int bannerSafeRadius,
            out Path path)
        {
            path = null;

            var ringStart = System.Math.Max(6, bannerSafeRadius + 2);
            var ringDistances = new[]
            {
                ringStart,
                ringStart + 8,
                ringStart + 16,
                ringStart + 28,
                System.Math.Max(ringStart + 48, 64),
                System.Math.Max(ringStart + 96, 128),
                256,
                512,
                1024,
                2048,
                4096,
                8192,
                16384
            }.Distinct().ToArray();
            var directions = new[]
            {
                new Vector3Int(1, 0, 0),
                new Vector3Int(-1, 0, 0),
                new Vector3Int(0, 0, 1),
                new Vector3Int(0, 0, -1),
                new Vector3Int(1, 0, 1),
                new Vector3Int(-1, 0, 1),
                new Vector3Int(1, 0, -1),
                new Vector3Int(-1, 0, -1)
            };

            for (var distanceIndex = 0; distanceIndex < ringDistances.Length; distanceIndex++)
            {
                var distance = ringDistances[distanceIndex];

                for (var directionIndex = 0; directionIndex < directions.Length; directionIndex++)
                {
                    var direction = directions[directionIndex];
                    var candidate = bannerPosition + new Vector3Int(direction.x * distance, 0, direction.z * distance);
                    if (TryBuildPathFromCandidate(ref context, candidate, bannerPosition, bannerSafeRadius, "fallback-ring", out path))
                        return true;
                }
            }

            var requester = _forcedSpawnRequester;
            if (requester != null)
            {
                var requesterPosition = requester.PositionStanding;
                var requesterCandidate = new Vector3Int(
                    Mathf.RoundToInt(requesterPosition.x),
                    Mathf.RoundToInt(requesterPosition.y),
                    Mathf.RoundToInt(requesterPosition.z));

                if (TryBuildPathFromCandidate(ref context, requesterCandidate, bannerPosition, bannerSafeRadius, "requester-fallback", out path))
                    return true;
            }

            return false;
        }

        private static bool TryBuildPathFromCandidate(
            ref PathingManager.PathingContext context,
            Vector3Int candidate,
            Vector3Int bannerPosition,
            int bannerSafeRadius,
            string source,
            out Path path)
        {
            path = null;

            if (!context.NavWorld.TryGetClosestAIPosition(
                    candidate,
                    NavWorld.EAIClosestPositionSearchType.ChunkAndDirectNeighbours,
                    out var finalPosition))
            {
                return false;
            }

            if (ServerManager.BlockEntityTracker.BannerTracker.IsSafeZone(finalPosition, out _))
                return false;

            return TryBuildBossPathFromFinalPosition(ref context, finalPosition, bannerPosition, bannerSafeRadius, source, out path);
        }

        private static bool TryBuildBossPathFromFinalPosition(
            ref PathingManager.PathingContext context,
            Vector3Int finalPosition,
            Vector3Int bannerPosition,
            int bannerSafeRadius,
            string source,
            out Path path)
        {
            path = null;
            var tuning = BossTuning.Current;
            var maxSearchWalkDistance = Mathf.Max(
                tuning.BossSpawnMazeSearchWalkDistance,
                tuning.BossSpawnExtremeWalkDistance,
                tuning.BossSpawnMaxLiveWalkDistance + 500f);
            var nodeLimit = Mathf.Max(15000, tuning.BossSpawnMazeNodeIterationLimit);
            var settings = PathFinder.PathingSettings.DefaultMonster
                .WithMaxMovementCost(maxSearchWalkDistance)
                .WithNodeIterationLimit(nodeLimit);

            var result = context.Pathing.TryFindPath(ref context, finalPosition, bannerPosition, out var fullPath, ref settings);
            if (result != PathFinder.EPathFindingResult.Success || fullPath == null || fullPath.Count < 2)
            {
                Path.Return(fullPath);
                return false;
            }

            path = TrimBossPathForLiveDistance(fullPath, bannerPosition, bannerSafeRadius, source);
            return path != null;
        }

        private static Path TrimBossPathForLiveDistance(
            Path sourcePath,
            Vector3Int bannerPosition,
            int bannerSafeRadius,
            string source)
        {
            if (sourcePath == null || sourcePath.Count < 2)
                return sourcePath;

            var tuning = BossTuning.Current;
            var liveWalkDistance = Mathf.Max(350f, tuning.BossSpawnMaxLiveWalkDistance);
            var originalStepDistance = sourcePath.CalculateStepCount();
            if (originalStepDistance <= liveWalkDistance && sourcePath.MovementCost <= liveWalkDistance * 1.5f)
                return sourcePath;

            var startIndex = FindBossPathSuffixStartIndex(sourcePath, liveWalkDistance);
            startIndex = MoveBossPathStartOutsideSafeZone(sourcePath, startIndex, bannerSafeRadius);
            if (startIndex <= 0)
                return sourcePath;

            if (startIndex >= sourcePath.Count - 1)
                startIndex = sourcePath.Count - 2;

            var trimmedPath = Path.Get();
            var start = sourcePath.GetAt(startIndex);
            trimmedPath.Initialize(start, sourcePath.Goal);

            for (var i = startIndex; i < sourcePath.Count; i++)
                trimmedPath.pathPrecise.Add(sourcePath.GetAt(i));

            trimmedPath.movementCost = trimmedPath.CalculateStepCount();
            if (trimmedPath.Count < 2)
            {
                Path.Return(trimmedPath);
                return sourcePath;
            }

            Log.Write("BetterNecromancy boss maze spawn trimmed " + source + " path near " + bannerPosition +
                      " from " + Mathf.RoundToInt(originalStepDistance) +
                      " to " + Mathf.RoundToInt(trimmedPath.CalculateStepCount()) +
                      " blocks. Boss starts on a valid labyrinth path point at " + start + ".");
            Path.Return(sourcePath);
            return trimmedPath;
        }

        private static int FindBossPathSuffixStartIndex(Path path, float maxStepDistance)
        {
            var remainingDistance = 0f;
            for (var i = path.Count - 1; i > 0; i--)
            {
                var current = path.GetAt(i);
                var previous = path.GetAt(i - 1);
                var segmentDistance = PathFinder.EstimatedStepDistance(current - previous);
                if (remainingDistance + segmentDistance > maxStepDistance)
                    return Mathf.Clamp(i, 0, path.Count - 2);

                remainingDistance += segmentDistance;
            }

            return 0;
        }

        private static int MoveBossPathStartOutsideSafeZone(Path path, int startIndex, int bannerSafeRadius)
        {
            var index = Mathf.Clamp(startIndex, 0, path.Count - 2);
            while (index > 0 && ServerManager.BlockEntityTracker.BannerTracker.IsSafeZone(path.GetAt(index), out _))
                index--;

            var minimumDistance = Mathf.Max(4, bannerSafeRadius + 2);
            while (index > 0 && (path.GetAt(index) - path.Goal).MaxPartAbs < minimumDistance)
                index--;

            return index;
        }

        private static IPandaBoss FindBossByName(string bossName)
        {
            if (string.IsNullOrWhiteSpace(bossName))
                return null;

            var normalized = NormalizeBossName(bossName);

            lock (Bosses)
            {
                return Bosses.FirstOrDefault(boss =>
                    NormalizeBossName(boss.name) == normalized ||
                    NormalizeBossName(boss.GetType().Name) == normalized);
            }
        }

        private static string NormalizeBossName(string bossName)
        {
            return new string(bossName
                .ToLowerInvariant()
                .Where(char.IsLetterOrDigit)
                .ToArray());
        }

        private static Colony ResolvePrimaryBossColony(Colony colony)
        {
            if (colony == null)
                return null;

            return colony.ColonyGroup?.MainColony ?? colony;
        }

        private static bool _currentBossOrPending(out string pendingName)
        {
            if (Instance._currentBoss != null)
            {
                pendingName = Instance._currentBoss.name;
                return true;
            }

            if (!string.IsNullOrEmpty(_forcedSpawnBossName))
            {
                pendingName = _forcedSpawnBossName;
                return true;
            }

            pendingName = null;
            return false;
        }

        private static bool TryResolvePreferredCommandColony(Players.Player requester, out Colony colony, out string message)
        {
            colony = null;
            message = null;

            if (requester == null)
            {
                message = "No player context available for boss command.";
                return false;
            }

            var primaryColony = ResolvePrimaryBossColony(requester.ActiveColony);
            if (primaryColony != null &&
                requester.OwnsColony(primaryColony) &&
                primaryColony.Banners.Count > 0 &&
                CanBossSpawnAtColony(primaryColony))
            {
                colony = primaryColony;
                return true;
            }

            message = "Boss commands require a main colony with a banner and stockpile. Outposts are redirected only to that main colony.";
            return false;
        }

        private static bool CanBossSpawnAtColony(Colony colony)
        {
            return colony != null &&
                   colony.Banners.Count > 0 &&
                   colony.ColonyGroup?.Stockpile != null &&
                   colony.ColonyGroup.MainColonyID == colony.ColonyID;
        }

        private static bool IsPrimaryBossColony(Colony colony)
        {
            if (!CanBossSpawnAtColony(colony))
                return false;

            foreach (var player in Players.ConnectedPlayers)
            {
                if (player == null || !player.IsConnectionReady)
                    continue;

                if (!player.OwnsColony(colony))
                    continue;

                if (ResolvePrimaryBossColony(player.ActiveColony) == colony)
                    return true;
            }

            return false;
        }

        private static bool TryResolveRelevantActiveBoss(Colony colony, out Colony bossColony, out IPandaBoss boss, out string message)
        {
            bossColony = null;
            boss = null;
            message = null;

            if (colony != null && ActiveBosses.TryGetValue(colony, out boss) && boss != null)
            {
                bossColony = colony;
                return true;
            }

            if (ActiveBosses.Count == 1)
            {
                var pair = ActiveBosses.First();
                bossColony = pair.Key;
                boss = pair.Value;
                return boss != null;
            }

            if (ActiveBosses.Count == 0)
            {
                message = _currentBossOrPending(out var pendingName)
                    ? "No active boss yet. Spawn pending: " + pendingName + ". Use /bnboss reset to clear it."
                    : "There is no active boss to kill.";
                return false;
            }

            message = "Multiple active bosses are tracked right now. Use /bnboss reset to clear them all.";
            return false;
        }

        private static bool TryForceEndBoss(IPandaBoss boss, bool suppressDeathSideEffects)
        {
            if (!(boss is Zombie zombie) || !zombie.IsValid)
                return false;

            if (suppressDeathSideEffects)
                SuppressedBossDeaths.Add(zombie.ID);

            zombie.CurrentHealth = 0f;
            zombie.OnRagdoll();
            return true;
        }

        private static void ResetPendingBossState()
        {
            PendingSpawns.Clear();
            ClearVanillaBossPathRequest(true);
            Instance._currentBoss = null;
            _queuedUntil = double.MaxValue;
            _forcedSpawnColony = null;
            _forcedSpawnRequester = null;
            _forcedSpawnBossName = null;
            ClearBossSpawnAttempt();
        }

        private static void BeginBossSpawnAttempt(string bossName, Colony colony, Players.Player requester, bool forced)
        {
            _spawnAttemptBossName = bossName;
            _spawnAttemptColony = ResolvePrimaryBossColony(colony);
            _spawnAttemptRequester = requester;
            _spawnAttemptForced = forced;
            _spawnAttemptActiveSince = double.MaxValue;
            _spawnAttemptRetryCount = 0;
        }

        private static bool HasBossSpawnAttempt()
        {
            return !string.IsNullOrEmpty(_spawnAttemptBossName);
        }

        private static int GetSpawnAttemptRotationIndex(int count)
        {
            if (count <= 0 || _spawnAttemptRetryCount <= 0)
                return 0;

            return _spawnAttemptRetryCount % count;
        }

        private static void ClearBossSpawnAttempt()
        {
            _spawnAttemptBossName = null;
            _spawnAttemptColony = null;
            _spawnAttemptRequester = null;
            _spawnAttemptForced = false;
            _spawnAttemptActiveSince = double.MaxValue;
            _spawnAttemptRetryCount = 0;
        }

        private static void UpdateBossSpawnAttemptState()
        {
            if (!HasBossSpawnAttempt())
                return;

            var now = CSTime.SecondsSinceStartDouble;
            if (ActiveBosses.Count > 0)
            {
                if (_spawnAttemptActiveSince == double.MaxValue)
                    _spawnAttemptActiveSince = now;
                else if (now - _spawnAttemptActiveSince >= BossTuning.Current.BossSpawnActivationConfirmSeconds)
                    ClearBossSpawnAttempt();

                return;
            }

            if (_spawnAttemptActiveSince != double.MaxValue)
                TryScheduleBossSpawnRetry("The boss was not active for the required 5 seconds.");
        }

        private static bool TryScheduleBossSpawnRetry(string reason)
        {
            if (!HasBossSpawnAttempt())
                return false;

            var tuning = BossTuning.Current;
            var boss = FindBossByName(_spawnAttemptBossName);
            if (boss == null)
            {
                NotifyBossSpawnAttempt("Boss retry failed because the boss definition could not be found anymore.", false);
                ClearBossSpawnAttempt();
                BossActive = false;
                ScheduleNextBoss();
                return false;
            }

            _spawnAttemptRetryCount++;
            _spawnAttemptActiveSince = double.MaxValue;
            PendingSpawns.Clear();
            ClearVanillaBossPathRequest(true);
            Instance._currentBoss = boss;

            var retryColony = ResolvePrimaryBossColony(_spawnAttemptColony);
            if (!CanBossSpawnAtColony(retryColony))
                retryColony = null;

            _forcedSpawnColony = retryColony;
            _forcedSpawnRequester = _spawnAttemptForced ? _spawnAttemptRequester : null;
            _forcedSpawnBossName = boss.name;
            _queuedUntil = CSTime.SecondsSinceStartDouble + tuning.BossSpawnRetryDelaySeconds;
            BossActive = true;

            NotifyBossSpawnAttempt(reason + " Retrying in " + Mathf.CeilToInt((float)tuning.BossSpawnRetryDelaySeconds) + "s (attempt " + _spawnAttemptRetryCount + ").", true);
            var queued = retryColony != null
                ? TryQueueVanillaBossPathForColony(retryColony, true, CSTime.SecondsSinceStartDouble + tuning.BossSpawnRetryDelaySeconds)
                : TryQueueVanillaBossPathForAnyPrimaryColony(true, CSTime.SecondsSinceStartDouble + tuning.BossSpawnRetryDelaySeconds);
            if (!queued)
                ServerManager.PathingManager.QueueAction(Instance);
            return true;
        }

        private static void NotifyBossSpawnAttempt(string message, bool retrying)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            var prefix = string.IsNullOrWhiteSpace(_spawnAttemptBossName) ? string.Empty : "[" + _spawnAttemptBossName + "] ";
            var fullMessage = prefix + message;
            if (retrying && _spawnAttemptRetryCount > 1 && _spawnAttemptRetryCount % 10 != 0)
                return;

            Log.WriteWarning("BetterNecromancy boss spawn retry: " + fullMessage);
        }

        private static void ForgetBossRuntimeState(IPandaBoss boss)
        {
            if (!(boss is Zombie zombie))
                return;

            SuppressedBossDeaths.Remove(zombie.ID);
            BossUpdateFailuresLogged.Remove(zombie.ID);
        }

        private SpawnLocationResult TryGetSpawnLocation(
            PathingManager.PathingContext context,
            Vector3Int bannerPosition,
            int minSpawnRadius,
            int maxSpawnRadius,
            float maxSpawnWalkDistance,
            int maxTries,
            out Vector3Int positionFinal)
        {
            int maxWalkDistance = Mathf.RoundToInt(maxSpawnWalkDistance);
            maxSpawnRadius = Mathf.Min(maxWalkDistance, maxSpawnRadius);

            if (minSpawnRadius >= maxSpawnRadius)
            {
                positionFinal = Vector3Int.invalidPos;
                return SpawnLocationResult.Impossible;
            }

            int foundPathPosButSafes = 0;

            lock (_pathingRandomSource)
            {
                for (int spawnTry = 0; spawnTry < maxTries; spawnTry++)
                {
                    if (TrySamplePosition(true, out var possiblePosition) && TestPositionFinal(possiblePosition, out positionFinal))
                        return SpawnLocationResult.Success;
                }

                if (foundPathPosButSafes > 3)
                {
                    for (int spawnTry = 0; spawnTry < maxTries; spawnTry++)
                    {
                        if (TrySamplePosition(false, out var possiblePosition) && TestPositionFinal(possiblePosition, out positionFinal))
                            return SpawnLocationResult.Success;
                    }
                }
            }

            positionFinal = Vector3Int.invalidPos;
            return SpawnLocationResult.Fail;

            bool TestPositionFinal(Vector3Int possiblePosition, out Vector3Int finalPosition)
            {
                if (context.NavWorld.TryGetClosestAIPosition(
                    possiblePosition,
                    NavWorld.EAIClosestPositionSearchType.ChunkAndDirectNeighbours,
                    out finalPosition))
                {
                    if (ServerManager.BlockEntityTracker.BannerTracker.IsSafeZone(finalPosition, out var foundBanner))
                    {
                        if (foundBanner != bannerPosition)
                            foundPathPosButSafes++;
                    }
                    else
                    {
                        return true;
                    }
                }

                return false;
            }

            bool TrySamplePosition(bool checkWalkingDistance, out Vector3Int position)
            {
                for (int i = 0; i < 1000; i++)
                {
                    Vector3Int offset = new Vector3Int
                    {
                        x = _pathingRandomSource.Next(-maxSpawnRadius, maxSpawnRadius),
                        // Keep the vertical search tight so long-range retries do not waste most samples high above or far below the colony.
                        y = _pathingRandomSource.Next(-SpawnCandidateVerticalRadius, SpawnCandidateVerticalRadius + 1),
                        z = _pathingRandomSource.Next(-maxSpawnRadius, maxSpawnRadius)
                    };

                    if (offset.MaxPartAbs <= minSpawnRadius)
                        continue;

                    if (checkWalkingDistance && Mathf.Abs(offset.x) + Mathf.Abs(offset.z) > maxWalkDistance)
                        continue;

                    position = bannerPosition + offset;
                    return true;
                }

                position = default;
                return false;
            }
        }

        private enum SpawnLocationResult
        {
            Success,
            Fail,
            Impossible
        }
    }
}
