using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Jobs;
using ModLoaderInterfaces;
using Monsters;
using Newtonsoft.Json.Linq;
using NPC;
using Pipliz;

namespace BetterNecromancy
{
    [ModLoader.ModManager]
    public static class MageGuardBetterGuardsCompat
    {
        private sealed class NightEntry
        {
            public string JobKey = string.Empty;
            public int X;
            public int Y;
            public int Z;
            public int NPCId;
            public int NightsSurvived;
            public int LastProcessedNightIndex;
        }

        private sealed class KillEntry
        {
            public string JobKey = string.Empty;
            public int X;
            public int Y;
            public int Z;
            public int NPCId;
            public string BaseName = string.Empty;
            public int KillCount;
        }

        private struct MageGuardDiscoveryIterator : NPCTracker.INPCIterator
        {
            public void IterateNPC(NPCBase npc)
            {
                TryDiscoverMageGuard(npc);
            }
        }

        private const string NightSaveKey = "betternecromancy_mage_guard_night_bonus";
        private const string KillSaveKey = "betternecromancy_mage_guard_kill_bonus";
        private const float DamageMultiplierPerNight = 0.0001f;
        private const float RangeMultiplier = 2f;
        private const double NightValidationIntervalSeconds = 5.0;
        private const double KillValidationIntervalSeconds = 0.5;
        private const int DiscoverRadius = 60;

        private static readonly Regex BonusSuffixRegex = new Regex(@"\s\+[0-9]+(?:\.[0-9]+)?%$", RegexOptions.Compiled);
        private static readonly string[] MageGuardTypeKeys =
        {
            ModEntry.Namespace + ".MageGuardT1",
            ModEntry.Namespace + ".MageGuardT2",
            ModEntry.Namespace + ".MageGuardT3",
            ModEntry.Namespace + ".MageGuardT4",
            ModEntry.Namespace + ".MageGuardT5",
            ModEntry.Namespace + ".MageGuardT6"
        };

        private static readonly Dictionary<string, int> BaseRangeByTypeKey = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            { ModEntry.Namespace + ".MageGuardT1", 44 },
            { ModEntry.Namespace + ".MageGuardT2", 52 },
            { ModEntry.Namespace + ".MageGuardT3", 60 },
            { ModEntry.Namespace + ".MageGuardT4", 68 },
            { ModEntry.Namespace + ".MageGuardT5", 76 },
            { ModEntry.Namespace + ".MageGuardT6", 84 }
        };

        private static readonly Dictionary<string, NightEntry> NightEntriesByJob = new Dictionary<string, NightEntry>(StringComparer.Ordinal);
        private static readonly Dictionary<string, KillEntry> KillEntriesByJob = new Dictionary<string, KillEntry>(StringComparer.Ordinal);
        private static readonly Dictionary<int, NPCID> LastMageGuardHitByMonster = new Dictionary<int, NPCID>();
        private static readonly HashSet<ushort> MageGuardItemIndices = new HashSet<ushort>();
        private static readonly Dictionary<ushort, int> BaseRangeByItemIndex = new Dictionary<ushort, int>();

        private static double _nextValidationAt;
        private static BetterGuardsBonusMode _lastBonusMode = BetterGuardsBonusMode.None;
        private static bool _lastRangePatchState;

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AfterWorldLoad, ModEntry.Namespace + ".MageGuardBetterGuardsCompat.AfterWorldLoad")]
        public static void AfterWorldLoad()
        {
            MageGuardItemIndices.Clear();
            BaseRangeByItemIndex.Clear();
            LastMageGuardHitByMonster.Clear();
            _nextValidationAt = 0d;
            _lastBonusMode = BetterGuardsBridge.CurrentBonusMode;
            _lastRangePatchState = BetterGuardsBridge.HasRangeBonus;

            ResolveMageGuardTypes();
            ApplyRangePatch(BetterGuardsBridge.HasRangeBonus);
            PatchActiveMageGuardRanges();
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnPlayerMoved, ModEntry.Namespace + ".MageGuardBetterGuardsCompat.OnPlayerMoved")]
        public static void OnPlayerMoved(Players.Player player, UnityEngine.Vector3 oldStandingLocation)
        {
            if (player == null || BetterGuardsBridge.CurrentBonusMode != BetterGuardsBonusMode.NightByJobBlock)
                return;

            DiscoverGuardsNearPosition(player.PositionVoxelStanding);
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnMonsterHit, ModEntry.Namespace + ".MageGuardBetterGuardsCompat.OnMonsterHit")]
        public static void OnMonsterHit(IMonster monster, ModLoader.OnHitData hit)
        {
            var mode = BetterGuardsBridge.CurrentBonusMode;
            if (mode == BetterGuardsBonusMode.None || monster == null || hit == null)
                return;

            var npc = hit.HitSourceObject as NPCBase;
            if (!TryGetMageGuardJobPosition(npc, out var guardPosition, out var guardJob, out var blockTypeKey))
            {
                if (mode == BetterGuardsBonusMode.KillByJobBlock && monster.IsValid)
                    LastMageGuardHitByMonster.Remove(monster.ID.id);
                return;
            }

            ApplyRangePatchToGuardJob(guardJob, blockTypeKey);

            if (mode == BetterGuardsBonusMode.NightByJobBlock)
            {
                var entry = GetOrCreateNightEntry(npc, guardPosition);
                UpdateNightEntryForCurrentNight(entry);
                hit.ResultDamage *= 1f + GetNightDamageBonusMultiplier(entry);
                return;
            }

            var killEntry = GetOrCreateKillEntry(npc, guardPosition);
            hit.ResultDamage *= 1f + GetKillDamageBonusMultiplier(killEntry);
            if (monster.IsValid)
                LastMageGuardHitByMonster[monster.ID.id] = npc.ID;
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnMonsterDied, ModEntry.Namespace + ".MageGuardBetterGuardsCompat.OnMonsterDied")]
        public static void OnMonsterDied(IMonster monster)
        {
            if (BetterGuardsBridge.CurrentBonusMode != BetterGuardsBonusMode.KillByJobBlock || monster == null)
                return;

            if (!LastMageGuardHitByMonster.TryGetValue(monster.ID.id, out var npcId))
                return;

            LastMageGuardHitByMonster.Remove(monster.ID.id);

            if (!NPCTracker.TryGetNPC(npcId, out var npc) || !npc.IsValid)
                return;

            if (!TryGetMageGuardJobPosition(npc, out var guardPosition, out _, out _))
                return;

            var entry = GetOrCreateKillEntry(npc, guardPosition);
            entry.KillCount++;
            ApplyKillDisplayName(npc, entry);
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnSaveWorldMisc, ModEntry.Namespace + ".MageGuardBetterGuardsCompat.OnSaveWorldMisc")]
        public static void OnSaveWorldMisc(JObject data)
        {
            if (NightEntriesByJob.Count == 0)
            {
                data.Remove(NightSaveKey);
            }
            else
            {
                var nightArray = new JArray();
                foreach (var entry in NightEntriesByJob.Values)
                {
                    var obj = new JObject
                    {
                        ["x"] = entry.X,
                        ["y"] = entry.Y,
                        ["z"] = entry.Z,
                        ["npcId"] = entry.NPCId,
                        ["nightsSurvived"] = entry.NightsSurvived,
                        ["lastProcessedNightIndex"] = entry.LastProcessedNightIndex
                    };
                    nightArray.Add(obj);
                }

                data[NightSaveKey] = nightArray;
            }

            if (KillEntriesByJob.Count == 0)
            {
                data.Remove(KillSaveKey);
            }
            else
            {
                var killArray = new JArray();
                foreach (var entry in KillEntriesByJob.Values)
                {
                    var obj = new JObject
                    {
                        ["x"] = entry.X,
                        ["y"] = entry.Y,
                        ["z"] = entry.Z,
                        ["npcId"] = entry.NPCId,
                        ["baseName"] = entry.BaseName,
                        ["killCount"] = entry.KillCount
                    };
                    killArray.Add(obj);
                }

                data[KillSaveKey] = killArray;
            }
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnLoadWorldMisc, ModEntry.Namespace + ".MageGuardBetterGuardsCompat.OnLoadWorldMisc")]
        public static void OnLoadWorldMisc(JObject data)
        {
            NightEntriesByJob.Clear();
            KillEntriesByJob.Clear();
            LastMageGuardHitByMonster.Clear();

            if (data[NightSaveKey] is JArray nightArray)
            {
                for (var i = 0; i < nightArray.Count; i++)
                {
                    if (!(nightArray[i] is JObject obj))
                        continue;

                    var x = obj.Value<int?>("x") ?? 0;
                    var y = obj.Value<int?>("y") ?? 0;
                    var z = obj.Value<int?>("z") ?? 0;
                    var entry = new NightEntry
                    {
                        X = x,
                        Y = y,
                        Z = z,
                        JobKey = PositionToKey(new Pipliz.Vector3Int(x, y, z)),
                        NPCId = obj.Value<int?>("npcId") ?? 0,
                        NightsSurvived = System.Math.Max(0, obj.Value<int?>("nightsSurvived") ?? 0),
                        LastProcessedNightIndex = obj.Value<int?>("lastProcessedNightIndex") ?? GetCurrentNightIndex()
                    };

                    if (entry.JobKey.Length > 0)
                        NightEntriesByJob[entry.JobKey] = entry;
                }
            }

            if (data[KillSaveKey] is JArray killArray)
            {
                for (var i = 0; i < killArray.Count; i++)
                {
                    if (!(killArray[i] is JObject obj))
                        continue;

                    var x = obj.Value<int?>("x") ?? 0;
                    var y = obj.Value<int?>("y") ?? 0;
                    var z = obj.Value<int?>("z") ?? 0;
                    var entry = new KillEntry
                    {
                        X = x,
                        Y = y,
                        Z = z,
                        JobKey = PositionToKey(new Pipliz.Vector3Int(x, y, z)),
                        NPCId = obj.Value<int?>("npcId") ?? 0,
                        BaseName = obj.Value<string>("baseName") ?? string.Empty,
                        KillCount = System.Math.Max(0, obj.Value<int?>("killCount") ?? 0)
                    };

                    if (entry.JobKey.Length > 0)
                        KillEntriesByJob[entry.JobKey] = entry;
                }
            }
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnUpdate, ModEntry.Namespace + ".MageGuardBetterGuardsCompat.OnUpdate")]
        [ModLoader.ModCallbackDependsOn("npctracker.update")]
        public static void OnUpdate()
        {
            if (!World.Initialized)
                return;

            ResolveMageGuardTypes();

            var currentMode = BetterGuardsBridge.CurrentBonusMode;
            var hasRangeBonus = BetterGuardsBridge.HasRangeBonus;
            if (currentMode != _lastBonusMode || hasRangeBonus != _lastRangePatchState)
            {
                if (_lastBonusMode == BetterGuardsBonusMode.KillByJobBlock && currentMode != BetterGuardsBonusMode.KillByJobBlock)
                    ResetAllKillDisplayNames();

                ApplyRangePatch(hasRangeBonus);
                PatchActiveMageGuardRanges();
                _lastBonusMode = currentMode;
                _lastRangePatchState = hasRangeBonus;
                _nextValidationAt = 0d;
            }

            if (currentMode == BetterGuardsBonusMode.None)
                return;

            var now = Pipliz.Time.SecondsSinceStartDoubleThisFrame;
            var interval = currentMode == BetterGuardsBonusMode.KillByJobBlock ? KillValidationIntervalSeconds : NightValidationIntervalSeconds;
            if (now < _nextValidationAt)
                return;

            _nextValidationAt = now + interval;

            if (currentMode == BetterGuardsBonusMode.KillByJobBlock)
                ValidateKillEntries();
            else
                ValidateNightEntries();
        }

        private static void ResolveMageGuardTypes()
        {
            for (var i = 0; i < MageGuardTypeKeys.Length; i++)
            {
                var typeKey = MageGuardTypeKeys[i];
                if (!ItemTypes.TryGetType(typeKey, out var itemType) || itemType == null)
                    continue;

                MageGuardItemIndices.Add(itemType.ItemIndex);
                if (!BaseRangeByItemIndex.ContainsKey(itemType.ItemIndex) && BaseRangeByTypeKey.TryGetValue(typeKey, out var baseRange))
                    BaseRangeByItemIndex[itemType.ItemIndex] = baseRange;
            }
        }

        private static void DiscoverGuardsNearPosition(Pipliz.Vector3Int center)
        {
            var iterator = new MageGuardDiscoveryIterator();
            var min = center - Pipliz.Vector3Int.one * DiscoverRadius;
            var max = center + Pipliz.Vector3Int.one * DiscoverRadius;
            NPCTracker.IterateNPCs(min, max, ref iterator);
        }

        private static void TryDiscoverMageGuard(NPCBase npc)
        {
            if (!TryGetMageGuardJobPosition(npc, out var guardPosition, out var guardJob, out var blockTypeKey))
                return;

            ApplyRangePatchToGuardJob(guardJob, blockTypeKey);

            if (BetterGuardsBridge.CurrentBonusMode == BetterGuardsBonusMode.NightByJobBlock)
            {
                var entry = GetOrCreateNightEntry(npc, guardPosition);
                UpdateNightEntryForCurrentNight(entry);
            }
            else if (BetterGuardsBridge.CurrentBonusMode == BetterGuardsBonusMode.KillByJobBlock)
            {
                var entry = GetOrCreateKillEntry(npc, guardPosition);
                ApplyKillDisplayName(npc, entry);
            }
        }

        private static void ValidateNightEntries()
        {
            if (NightEntriesByJob.Count == 0)
                return;

            var keysToRemove = new List<string>();
            foreach (var pair in NightEntriesByJob)
            {
                var entry = pair.Value;
                if (!NPCTracker.TryGetNPC(new NPCID(entry.NPCId), out var npc) || npc == null || !npc.IsValid)
                {
                    keysToRemove.Add(pair.Key);
                    continue;
                }

                if (!TryGetMageGuardJobPosition(npc, out var currentGuardPos, out var guardJob, out var blockTypeKey) ||
                    !string.Equals(pair.Key, PositionToKey(currentGuardPos), StringComparison.Ordinal))
                {
                    keysToRemove.Add(pair.Key);
                    continue;
                }

                ApplyRangePatchToGuardJob(guardJob, blockTypeKey);
                UpdateNightEntryForCurrentNight(entry);
            }

            for (var i = 0; i < keysToRemove.Count; i++)
                NightEntriesByJob.Remove(keysToRemove[i]);
        }

        private static void ValidateKillEntries()
        {
            if (KillEntriesByJob.Count == 0)
                return;

            var keysToRemove = new List<string>();
            foreach (var pair in KillEntriesByJob)
            {
                var entry = pair.Value;
                if (!NPCTracker.TryGetNPC(new NPCID(entry.NPCId), out var npc) || npc == null || !npc.IsValid)
                {
                    keysToRemove.Add(pair.Key);
                    continue;
                }

                if (!TryGetMageGuardJobPosition(npc, out var currentGuardPos, out var guardJob, out var blockTypeKey) ||
                    !string.Equals(pair.Key, PositionToKey(currentGuardPos), StringComparison.Ordinal))
                {
                    ResetKillDisplayNameIfNeeded(npc, entry);
                    keysToRemove.Add(pair.Key);
                    continue;
                }

                ApplyRangePatchToGuardJob(guardJob, blockTypeKey);
                ApplyKillDisplayName(npc, entry);
            }

            for (var i = 0; i < keysToRemove.Count; i++)
                KillEntriesByJob.Remove(keysToRemove[i]);
        }

        private static void ResetAllKillDisplayNames()
        {
            foreach (var entry in KillEntriesByJob.Values)
            {
                if (NPCTracker.TryGetNPC(new NPCID(entry.NPCId), out var npc) && npc != null && npc.IsValid)
                    ResetKillDisplayNameIfNeeded(npc, entry);
            }
        }

        private static void PatchActiveMageGuardRanges()
        {
            var colonies = ServerManager.ColonyTracker.ColoniesByID.GetValueEnumerator();
            while (colonies.MoveNext())
            {
                var colony = colonies.Current;
                if (colony?.Followers == null)
                    continue;

                foreach (var follower in colony.Followers)
                {
                    if (!TryGetMageGuardJobPosition(follower, out _, out var guardJob, out var blockTypeKey))
                        continue;

                    ApplyRangePatchToGuardJob(guardJob, blockTypeKey);
                }
            }
        }

        private static void ApplyRangePatch(bool betterGuardsActive)
        {
            for (var i = 0; i < MageGuardTypeKeys.Length; i++)
            {
                var typeKey = MageGuardTypeKeys[i];
                if (!BaseRangeByTypeKey.TryGetValue(typeKey, out var baseRange))
                    continue;

                if (!ItemTypes.TryGetType(typeKey, out var itemType) || itemType == null)
                    continue;

                SetGuardRangeOnItemType(itemType, ResolveDesiredRange(baseRange, betterGuardsActive));
            }
        }

        private static void ApplyRangePatchToGuardJob(GuardJobInstance guardJob, string blockTypeKey)
        {
            if (guardJob == null || string.IsNullOrWhiteSpace(blockTypeKey))
                return;

            if (!BaseRangeByTypeKey.TryGetValue(blockTypeKey, out var baseRange))
                return;

            if (!(guardJob.Settings is GuardJobSettings guardSettings))
                return;

            guardSettings.Range = ResolveDesiredRange(baseRange, BetterGuardsBridge.HasRangeBonus);
        }

        private static int ResolveDesiredRange(int baseRange, bool betterGuardsActive)
        {
            if (!betterGuardsActive)
                return baseRange;

            return System.Math.Max(baseRange, (int)System.Math.Round(baseRange * RangeMultiplier));
        }

        private static bool TryGetMageGuardJobPosition(NPCBase npc, out Pipliz.Vector3Int guardPosition, out GuardJobInstance guardJob, out string blockTypeKey)
        {
            guardPosition = default(Pipliz.Vector3Int);
            guardJob = null;
            blockTypeKey = string.Empty;

            if (npc == null || !npc.IsValid || npc.Job == null || !npc.Job.IsValid)
                return false;

            guardJob = npc.Job as GuardJobInstance;
            if (guardJob == null)
                return false;

            var blockType = guardJob.BlockType;
            if (blockType == null)
                return false;

            blockTypeKey = blockType.Name ?? string.Empty;
            if (blockTypeKey.Length == 0)
                return false;

            if (!MageGuardItemIndices.Contains(blockType.ItemIndex) && !BaseRangeByTypeKey.ContainsKey(blockTypeKey))
                return false;

            guardPosition = guardJob.Position;
            return true;
        }

        private static bool SetGuardRangeOnItemType(ItemTypes.ItemType itemType, int range)
        {
            if (itemType?.AttachedBehaviours == null)
                return false;

            for (var i = 0; i < itemType.AttachedBehaviours.Length; i++)
            {
                var behaviour = itemType.AttachedBehaviours[i];
                if (!string.Equals(behaviour.Identifier, "guard", StringComparison.OrdinalIgnoreCase) ||
                    behaviour.JSON == null)
                {
                    continue;
                }

                behaviour.JSON["range"] = range;
                return true;
            }

            return false;
        }

        private static NightEntry GetOrCreateNightEntry(NPCBase npc, Pipliz.Vector3Int guardPosition)
        {
            var jobKey = PositionToKey(guardPosition);
            if (!NightEntriesByJob.TryGetValue(jobKey, out var entry))
            {
                entry = new NightEntry
                {
                    JobKey = jobKey,
                    X = guardPosition.x,
                    Y = guardPosition.y,
                    Z = guardPosition.z,
                    NPCId = npc.ID.id,
                    NightsSurvived = 0,
                    LastProcessedNightIndex = GetCurrentNightIndex()
                };
                NightEntriesByJob[jobKey] = entry;
                return entry;
            }

            if (entry.NPCId != npc.ID.id)
            {
                entry.NPCId = npc.ID.id;
                entry.NightsSurvived = 0;
                entry.LastProcessedNightIndex = GetCurrentNightIndex();
                entry.X = guardPosition.x;
                entry.Y = guardPosition.y;
                entry.Z = guardPosition.z;
            }

            return entry;
        }

        private static KillEntry GetOrCreateKillEntry(NPCBase npc, Pipliz.Vector3Int guardPosition)
        {
            var jobKey = PositionToKey(guardPosition);
            if (!KillEntriesByJob.TryGetValue(jobKey, out var entry))
            {
                entry = new KillEntry
                {
                    JobKey = jobKey,
                    X = guardPosition.x,
                    Y = guardPosition.y,
                    Z = guardPosition.z,
                    NPCId = npc.ID.id,
                    BaseName = SanitizeBaseName(npc.Name),
                    KillCount = 0
                };
                KillEntriesByJob[jobKey] = entry;
                ApplyKillDisplayName(npc, entry);
                return entry;
            }

            if (entry.NPCId != npc.ID.id)
            {
                entry.NPCId = npc.ID.id;
                entry.BaseName = SanitizeBaseName(npc.Name);
                entry.KillCount = 0;
                entry.X = guardPosition.x;
                entry.Y = guardPosition.y;
                entry.Z = guardPosition.z;
                ApplyKillDisplayName(npc, entry);
            }

            return entry;
        }

        private static int GetCurrentNightIndex()
        {
            var totalHours = TimeCycle.TotalHours - TimeCycle.Settings.GuardShiftNightEnd;
            return (int)System.Math.Floor(totalHours / 24.0);
        }

        private static string PositionToKey(Pipliz.Vector3Int position)
        {
            return position.x.ToString(CultureInfo.InvariantCulture) + "," +
                   position.y.ToString(CultureInfo.InvariantCulture) + "," +
                   position.z.ToString(CultureInfo.InvariantCulture);
        }

        private static float GetNightDamageBonusMultiplier(NightEntry entry)
        {
            return entry.NightsSurvived * DamageMultiplierPerNight;
        }

        private static void UpdateNightEntryForCurrentNight(NightEntry entry)
        {
            var currentNightIndex = GetCurrentNightIndex();
            if (currentNightIndex <= entry.LastProcessedNightIndex)
                return;

            entry.NightsSurvived += currentNightIndex - entry.LastProcessedNightIndex;
            entry.LastProcessedNightIndex = currentNightIndex;
        }

        private static float GetKillDamageBonusMultiplier(KillEntry entry)
        {
            return entry.KillCount * BetterGuardsBridge.GetKillBonusMultiplierPerKill();
        }

        private static string SanitizeBaseName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;

            return BonusSuffixRegex.Replace(name, string.Empty).Trim();
        }

        private static void ApplyKillDisplayName(NPCBase npc, KillEntry entry)
        {
            var baseName = SanitizeBaseName(entry.BaseName);
            if (baseName.Length == 0)
            {
                baseName = SanitizeBaseName(npc.Name);
                entry.BaseName = baseName;
            }

            var wantedName = BuildKillDisplayName(entry);
            if (!string.Equals(npc.Name, wantedName, StringComparison.Ordinal))
                npc.SetName(wantedName);
        }

        private static void ResetKillDisplayNameIfNeeded(NPCBase npc, KillEntry entry)
        {
            var baseName = SanitizeBaseName(entry.BaseName);
            if (baseName.Length == 0)
                return;

            if (!string.Equals(npc.Name, baseName, StringComparison.Ordinal))
                npc.SetName(baseName);
        }

        private static string BuildKillDisplayName(KillEntry entry)
        {
            var percent = entry.KillCount * BetterGuardsBridge.GetKillBonusPercentPerKill();
            return SanitizeBaseName(entry.BaseName) + " +" + FormatPercent(percent) + "%";
        }

        private static string FormatPercent(float percent)
        {
            if (percent >= 100f)
                return System.Math.Floor(percent).ToString("0", CultureInfo.InvariantCulture);

            if (percent >= 10f)
                return percent.ToString("0.0", CultureInfo.InvariantCulture).TrimEnd('0').TrimEnd('.');

            if (percent >= 1f)
                return percent.ToString("0.00", CultureInfo.InvariantCulture).TrimEnd('0').TrimEnd('.');

            return percent.ToString("0.000", CultureInfo.InvariantCulture).TrimEnd('0').TrimEnd('.');
        }
    }
}
