using Chatting;
using BetterNecromancy;
using Jobs;
using Newtonsoft.Json.Linq;
using colonyserver.Assets.UIGeneration;
using colonyshared.NetworkUI;
using colonyshared.NetworkUI.UIGeneration;
using NetworkUI;
using NetworkUI.Items;
using Monsters;
using NPC;
using Pipliz;
using Recipes;
using Shared;
using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;

namespace Pandaros.Settlers.Items
{
    [ModLoader.ModManager]
    public static class RitualWandManager
    {
        private const string SaveKey = "BetterNecromancy.RitualWand";
        private const string TotalKilledColonistsKey = "totalKilledColonists";
        private const string MageGuardRewardMilestonesKey = "mageGuardRewardMilestones";
        private const string RitualRecipeName = GameLoader.NAMESPACE + ".ritualist.RitualWand";
        private const string RitualWandHotbarKey = "BetterNecromancy.RitualWand.Hotbar";
        private const int MaxSacrificeVisualTargets = 18;
        private const long RitualSequenceDurationMs = 60000;
        private const long RitualPulseIntervalMs = 2500;
        private const int RitualAudioLoopLengthMs = 30000;
        private const int DamnedScreamsLoopLengthMs = 30000;
        private const string RitualAudioCollection = "bn_ritual_audio";
        private const string DamnedScreamsAudioCollection = "bn_damned_screams";
        private static readonly string[] SelectedTypeMemberNames =
        {
            "SelectedType",
            "SelectedItem",
            "SelectedItemIndex",
            "SelectedTypeIndex",
            "HeldItem",
            "HeldItemType"
        };
        private static readonly Pipliz.Vector3Int[] RequiredCorpseOffsets =
        {
            new Pipliz.Vector3Int(-1, 0, -1),
            new Pipliz.Vector3Int(1, 0, -1),
            new Pipliz.Vector3Int(-1, 0, 1),
            new Pipliz.Vector3Int(1, 0, 1)
        };

        private static int _totalKilledColonists;
        private static int _mageGuardRewardMilestones;
        private static readonly List<PendingRitualSequence> ActiveSequences = new List<PendingRitualSequence>();

        private sealed class PendingRitualSequence
        {
            public Colony Colony { get; set; }
            public Pipliz.Vector3Int AltarPosition { get; set; }
            public List<NPCBase> Targets { get; set; }
            public long StartedAtMs { get; set; }
            public long EndsAtMs { get; set; }
            public long NextPulseAtMs { get; set; }
            public int NextTargetIndex { get; set; }
            public bool AltarDestroyed { get; set; }
            public Dictionary<int, AudioManager.AudioClipPlayingID> RitualLoopIds { get; } = new Dictionary<int, AudioManager.AudioClipPlayingID>();
            public Dictionary<int, AudioManager.AudioClipPlayingID> ScreamsLoopIds { get; } = new Dictionary<int, AudioManager.AudioClipPlayingID>();
        }

        public static int TotalKilledColonists => _totalKilledColonists;

        public static float GetDamageMultiplier()
        {
            return Mathf.Max(1f, 1f + (_totalKilledColonists * 0.01f));
        }

        public static int GetDamageBonusPercent()
        {
            return Mathf.Max(0, _totalKilledColonists);
        }

        public static string GetKillCountText()
        {
            return "Killed " + _totalKilledColonists + " Colonists - You Monster!";
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnNPCCraftedRecipe, GameLoader.NAMESPACE + ".Items.RitualWandManager.OnNPCCraftedRecipe")]
        public static void OnNPCCraftedRecipe(IJob job, Recipe recipe, List<RecipeResult> results)
        {
            if (recipe == null ||
                !string.Equals(recipe.Name, RitualRecipeName, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            results?.Clear();

            var colony = job?.NPC?.Colony;
            var stockpile = colony?.ColonyGroup?.Stockpile;
            if (colony == null || stockpile == null)
                return;

            if (!(job is BlockJobInstance blockJob))
            {
                RefundRequirements(recipe, stockpile);
                stockpile.SendToOwners();
                NotifyOwners(colony, "[Ritual Wand] Craft failed: the ritual altar job could not be resolved.");
                return;
            }

            if (!TryGetCorpseBlockType(out var corpseBlockType))
            {
                RefundRequirements(recipe, stockpile);
                stockpile.SendToOwners();
                NotifyOwners(colony, "[Ritual Wand] Craft failed: Corpse Block type is missing.");
                return;
            }

            if (!HasValidRitualLayout(blockJob.Position, corpseBlockType, out var missingCorners))
            {
                RefundRequirements(recipe, stockpile);
                stockpile.SendToOwners();
                NotifyOwners(colony, "[Ritual Wand] Ritual failed: four Corpse Blocks are required on the altar corners. Missing: " + missingCorners + ".");
                return;
            }

            var sacrificeTargets = BuildRitualTargetOrder(colony, job.NPC);
            string awakeningMessage = null;
            if (!TryFindRitualAwakeningOwner(colony, out var ritualOwner) ||
                !Pandaros.API.Entities.PlayerMagicStateManager.TryAwakenWandByRitual(ritualOwner, out awakeningMessage))
            {
                RefundRequirements(recipe, stockpile);
                stockpile.SendToOwners();
                NotifyOwners(colony, "[Ritual Awakening] Ritual failed: " + (awakeningMessage ?? "no owner with a Tier 5 wand lineage was found."));
                return;
            }

            RemoveCorpseBlocks(blockJob.Position, colony, corpseBlockType);

            stockpile.SendToOwners();
            StartRitualSequence(blockJob.Position, colony, sacrificeTargets);

            NotifyOwners(
                colony,
                "[Ritual Awakening] " + awakeningMessage + " " +
                sacrificeTargets.Count +
                " colonists will be sacrificed over the next minute. The Ritualist will be the final offering.");
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnConstructTooltipUI, GameLoader.NAMESPACE + ".Items.RitualWandManager.OnConstructTooltipUI")]
        public static void OnConstructTooltipUI(Players.Player player, ConstructTooltipUIData data)
        {
            if (data == null ||
                data.hoverType != ETooltipHoverType.Item ||
                RitualWand.Item == null ||
                data.hoverItem != RitualWand.Item.ItemIndex ||
                data.menu == null)
            {
                return;
            }

            data.menu.Items.Clear();
            data.menu.Items.Add(new Label(new LabelData(GetKillCountText())));
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnSaveWorldMisc, GameLoader.NAMESPACE + ".Items.RitualWandManager.OnSaveWorldMisc")]
        public static void OnSaveWorldMisc(JObject data)
        {
            if (data == null)
                return;

            if (_totalKilledColonists <= 0 && _mageGuardRewardMilestones <= 0)
            {
                data.Remove(SaveKey);
                return;
            }

            var obj = new JObject
            {
                [TotalKilledColonistsKey] = _totalKilledColonists,
                [MageGuardRewardMilestonesKey] = _mageGuardRewardMilestones
            };

            data[SaveKey] = obj;
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnLoadWorldMisc, GameLoader.NAMESPACE + ".Items.RitualWandManager.OnLoadWorldMisc")]
        public static void OnLoadWorldMisc(JObject data)
        {
            _totalKilledColonists = 0;
            _mageGuardRewardMilestones = 0;

            if (!(data?[SaveKey] is JObject obj))
                return;

            _totalKilledColonists = System.Math.Max(0, obj.Value<int?>(TotalKilledColonistsKey) ?? 0);
            _mageGuardRewardMilestones = System.Math.Max(0, obj.Value<int?>(MageGuardRewardMilestonesKey) ?? 0);
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnUpdate, GameLoader.NAMESPACE + ".Items.RitualWandManager.OnUpdate")]
        public static void OnUpdate()
        {
            var now = Pipliz.Time.MillisecondsSinceStart;
            if (!PlayerUiGuard.ShouldDeferPlayerFacingEffects())
                UpdateActiveSequences(now);

            foreach (var player in Players.ConnectedPlayers)
            {
                UpdateRitualWandHotbarLabel(player);
            }
        }

        private static bool TryGetCorpseBlockType(out ItemTypes.ItemType corpseBlockType)
        {
            return ItemTypes.TryGetType(GameLoader.NAMESPACE + ".CorpseBlock", out corpseBlockType);
        }

        private static bool HasValidRitualLayout(Pipliz.Vector3Int altarPosition, ItemTypes.ItemType corpseBlockType, out string missingCorners)
        {
            var missing = new List<string>();

            for (var i = 0; i < RequiredCorpseOffsets.Length; i++)
            {
                var offset = RequiredCorpseOffsets[i];
                var checkPosition = new Pipliz.Vector3Int(
                    altarPosition.x + offset.x,
                    altarPosition.y + offset.y,
                    altarPosition.z + offset.z);

                ItemTypes.ItemType placedType;
                if (!World.TryGetTypeAt(checkPosition, out placedType) ||
                    placedType.ItemIndex != corpseBlockType.ItemIndex)
                {
                    missing.Add("(" + offset.x + ", " + offset.y + ", " + offset.z + ")");
                }
            }

            missingCorners = string.Join(", ", missing.ToArray());
            return missing.Count == 0;
        }

        private static bool TryFindRitualAwakeningOwner(Colony colony, out Players.Player player)
        {
            player = null;
            if (colony == null)
                return false;

            foreach (var candidate in Players.ConnectedPlayers)
            {
                if (candidate != null && candidate.OwnsColony(colony))
                {
                    player = candidate;
                    return true;
                }
            }

            return false;
        }

        private static void RefundRequirements(Recipe recipe, Stockpile stockpile)
        {
            if (recipe?.Requirements == null || stockpile == null)
                return;

            for (var i = 0; i < recipe.Requirements.Count; i++)
            {
                var requirement = recipe.Requirements[i];
                stockpile.Add(requirement.Type, requirement.Amount);
            }
        }

        private static List<NPCBase> GetLivingFollowers(Colony colony)
        {
            var followers = new List<NPCBase>();
            if (colony == null)
                return followers;

            foreach (var follower in colony.Followers)
            {
                if (follower != null && follower.IsValid && follower.health > 0f)
                    followers.Add(follower);
            }

            return followers;
        }

        private static void StartRitualSequence(Pipliz.Vector3Int altarPosition, Colony colony, List<NPCBase> targets)
        {
            var now = Pipliz.Time.MillisecondsSinceStart;
            PlayRitualStartEffects(altarPosition, targets);

            ActiveSequences.Add(new PendingRitualSequence
            {
                Colony = colony,
                AltarPosition = altarPosition,
                Targets = targets ?? new List<NPCBase>(),
                StartedAtMs = now,
                EndsAtMs = now + RitualSequenceDurationMs,
                NextPulseAtMs = now + RitualPulseIntervalMs,
                NextTargetIndex = 0
            });
        }

        private static void UpdateActiveSequences(long now)
        {
            for (var i = ActiveSequences.Count - 1; i >= 0; i--)
            {
                var sequence = ActiveSequences[i];
                if (sequence == null)
                {
                    ActiveSequences.RemoveAt(i);
                    continue;
                }

                if (now >= sequence.NextPulseAtMs)
                {
                    PlayRitualPulseEffects(sequence.AltarPosition, sequence.NextTargetIndex, sequence.Targets?.Count ?? 0);
                    sequence.NextPulseAtMs = now + RitualPulseIntervalMs;
                }

                UpdateRitualAudio(sequence, now);

                AdvanceRitualSacrifices(sequence, now);

                if (now >= sequence.EndsAtMs)
                {
                    CompleteRitualSequence(sequence);
                    ActiveSequences.RemoveAt(i);
                }
            }
        }

        private static void AdvanceRitualSacrifices(PendingRitualSequence sequence, long now)
        {
            if (sequence?.Targets == null || sequence.Targets.Count == 0)
                return;

            while (sequence.NextTargetIndex < sequence.Targets.Count)
            {
                var dueAt = GetSacrificeDueTime(sequence, sequence.NextTargetIndex);
                if (now < dueAt)
                    break;

                SacrificeTarget(sequence, sequence.Targets[sequence.NextTargetIndex]);
                sequence.NextTargetIndex++;
            }
        }

        private static void UpdateRitualAudio(PendingRitualSequence sequence, long now)
        {
            if (sequence == null)
                return;

            EnsureAudioLoopForOwners(
                sequence.Colony,
                sequence.AltarPosition,
                RitualAudioCollection,
                RitualAudioLoopLengthMs,
                sequence.RitualLoopIds,
                out _);

            var screamsShouldPlay = sequence.Targets != null && sequence.Targets.Count > 0 && now < sequence.EndsAtMs;
            if (screamsShouldPlay)
            {
                EnsureAudioLoopForOwners(
                    sequence.Colony,
                    sequence.AltarPosition,
                    DamnedScreamsAudioCollection,
                    DamnedScreamsLoopLengthMs,
                    sequence.ScreamsLoopIds,
                    out _);
            }
            else
            {
                StopAudioLoops(sequence.ScreamsLoopIds, DamnedScreamsAudioCollection);
            }
        }

        private static long GetSacrificeDueTime(PendingRitualSequence sequence, int index)
        {
            if (sequence == null || sequence.Targets == null || sequence.Targets.Count == 0)
                return 0L;

            var total = sequence.Targets.Count;
            var progress = (index + 1f) / total;
            return sequence.StartedAtMs + (long)(RitualSequenceDurationMs * progress);
        }

        private static void SacrificeTarget(PendingRitualSequence sequence, NPCBase target)
        {
            if (sequence?.Colony == null || target == null || !target.IsValid || target.health <= 0f)
                return;

            var altarCenter = sequence.AltarPosition.Vector + new Vector3(0.5f, 0.15f, 0.5f);
            var source = target.Position.Vector + Vector3.up * 1.05f;
            var altarCore = altarCenter + Vector3.up * 1.1f;

            ServerManager.SendParticleTrail(source, altarCore, 0.34f);
            ServerManager.SendParticleTrail(source + Vector3.up * 0.45f, altarCenter + Vector3.up * 2.2f, 0.42f);
            ServerManager.SendExplosionEffect(source, 8f, 1.4f, 1f, 0.2f);
            ServerManager.SendExplosionEffect(altarCore, 8f, 1.8f, 1f, 0.2f);

            var killDamage = Mathf.Max(999999f, sequence.Colony.ColonyGroup.NPCHealthMax * 10f);
            target.OnHit(killDamage, null, ModLoader.OnHitData.EHitSourceType.Monster);

            if (target.health <= 0f || !target.IsValid)
                _totalKilledColonists++;
        }

        private static void CompleteRitualSequence(PendingRitualSequence sequence)
        {
            if (sequence == null)
                return;

            if (sequence.NextTargetIndex < (sequence.Targets?.Count ?? 0))
            {
                for (var i = sequence.NextTargetIndex; i < sequence.Targets.Count; i++)
                    SacrificeTarget(sequence, sequence.Targets[i]);

                sequence.NextTargetIndex = sequence.Targets.Count;
            }

            StopAudioLoops(sequence.RitualLoopIds, RitualAudioCollection);
            StopAudioLoops(sequence.ScreamsLoopIds, DamnedScreamsAudioCollection);
            DestroyAltar(sequence.AltarPosition, sequence.Colony);
            var mageGuardRewardsGranted = AwardMageGuardMilestones(sequence);

            var totalDamageBonusPercent = GetDamageBonusPercent();
            NotifyOwners(
                sequence.Colony,
                "[Ritual Wand] The altar collapses into ash. Total kill count: " +
                _totalKilledColonists +
                ". Current ritual power: +" +
                totalDamageBonusPercent.ToString("0") +
                "% damage." +
                (mageGuardRewardsGranted > 0
                    ? " Ritual reward earned: +" + mageGuardRewardsGranted + " Ritual Ascendant Guard."
                    : string.Empty));
        }

        private static int AwardMageGuardMilestones(PendingRitualSequence sequence)
        {
            var colony = sequence?.Colony;
            var stockpile = colony?.ColonyGroup?.Stockpile;
            if (stockpile == null)
                return 0;

            var completedMilestones = _totalKilledColonists / 1000;
            var rewardsToGrant = completedMilestones - _mageGuardRewardMilestones;
            if (rewardsToGrant <= 0)
                return 0;

            if (!ItemTypes.TryGetType(GameLoader.NAMESPACE + ".MageGuardT6", out var rewardType))
                return 0;

            stockpile.Add(rewardType.ItemIndex, rewardsToGrant);
            stockpile.SendToOwners();
            _mageGuardRewardMilestones += rewardsToGrant;
            return rewardsToGrant;
        }

        private static List<NPCBase> BuildRitualTargetOrder(Colony colony, NPCBase ritualist)
        {
            var ordered = GetLivingFollowers(colony);
            if (ordered.Count == 0)
                return ordered;

            if (ritualist != null)
            {
                ordered.RemoveAll(follower => follower == null || !follower.IsValid);
                ordered.Remove(ritualist);
                if (ritualist.IsValid && ritualist.health > 0f)
                    ordered.Add(ritualist);
            }

            return ordered;
        }

        private static void PlayRitualStartEffects(Pipliz.Vector3Int altarPosition, List<NPCBase> sacrificeTargets)
        {
            var altarCenter = altarPosition.Vector + new Vector3(0.5f, 0.15f, 0.5f);
            var altarCore = altarCenter + Vector3.up * 1.1f;

            var corners = GetRitualCornerCenters(altarPosition);
            for (var i = 0; i < corners.Count; i++)
            {
                var corner = corners[i];
                ServerManager.SendParticleTrail(corner + Vector3.up * 0.05f, altarCore, 0.42f);
                ServerManager.SendParticleTrail(corner + Vector3.up * 1.3f, corner + Vector3.up * 0.1f, 0.34f);
                ServerManager.SendExplosionEffect(corner + Vector3.up * 0.15f, 8f, 1.3f, 1f, 0.2f);
            }

            if (corners.Count >= 4)
            {
                ServerManager.SendParticleTrail(corners[0] + Vector3.up * 0.2f, corners[3] + Vector3.up * 0.2f, 0.38f);
                ServerManager.SendParticleTrail(corners[1] + Vector3.up * 0.2f, corners[2] + Vector3.up * 0.2f, 0.38f);
            }

            ServerManager.SendParticleTrail(altarCenter - Vector3.up * 0.1f, altarCenter + Vector3.up * 3.6f, 0.62f);
            ServerManager.SendParticleTrail(altarCenter + Vector3.up * 2.9f, altarCore, 0.46f);
            ServerManager.SendExplosionEffect(altarCore, 8f, 2.8f, 1f, 0.2f);

            if (sacrificeTargets == null || sacrificeTargets.Count == 0)
                return;

            var visualTargets = GetSacrificeVisualTargets(sacrificeTargets);
            for (var i = 0; i < visualTargets.Count; i++)
            {
                var npc = visualTargets[i];
                if (npc == null || !npc.IsValid || npc.health <= 0f)
                    continue;

                var source = npc.Position.Vector + Vector3.up * 1.05f;
                var target = altarCenter + Vector3.up * (0.65f + (i % 3) * 0.18f);
                ServerManager.SendParticleTrail(source, target, 0.28f + (i % 3) * 0.04f);
            }

            var finaleRadius = Mathf.Clamp(2.8f + (sacrificeTargets.Count * 0.08f), 2.8f, 6.5f);
            ServerManager.SendExplosionEffect(altarCore, 8f, finaleRadius, 1f, 0.2f);
            ServerManager.SendParticleTrail(altarCenter + Vector3.up * 3.8f, altarCenter + Vector3.up * 0.15f, 0.52f);
        }

        private static void PlayRitualPulseEffects(Pipliz.Vector3Int altarPosition, int currentIndex, int totalTargets)
        {
            var altarCenter = altarPosition.Vector + new Vector3(0.5f, 0.15f, 0.5f);
            var altarCore = altarCenter + Vector3.up * 1.15f;
            var progress = totalTargets <= 0 ? 1f : Mathf.Clamp01((float)currentIndex / totalTargets);
            var pulseRadius = Mathf.Lerp(1.9f, 4.4f, progress);

            ServerManager.SendExplosionEffect(altarCore, 8f, pulseRadius, 1f, 0.2f);
            ServerManager.SendParticleTrail(altarCenter + Vector3.up * 0.15f, altarCenter + Vector3.up * (2.2f + progress * 1.4f), 0.42f);
            ServerManager.SendParticleTrail(altarCenter + new Vector3(0.65f, 0.25f, 0f), altarCore + new Vector3(0.2f, 1.2f, 0f), 0.34f);
            ServerManager.SendParticleTrail(altarCenter + new Vector3(-0.65f, 0.25f, 0f), altarCore + new Vector3(-0.2f, 1.2f, 0f), 0.34f);
            ServerManager.SendParticleTrail(altarCenter + new Vector3(0f, 0.2f, 0.65f), altarCore + new Vector3(0f, 1.15f, 0.2f), 0.34f);
            ServerManager.SendParticleTrail(altarCenter + new Vector3(0f, 0.2f, -0.65f), altarCore + new Vector3(0f, 1.15f, -0.2f), 0.34f);
        }

        private static List<Vector3> GetRitualCornerCenters(Pipliz.Vector3Int altarPosition)
        {
            var corners = new List<Vector3>(RequiredCorpseOffsets.Length);
            for (var i = 0; i < RequiredCorpseOffsets.Length; i++)
            {
                var offset = RequiredCorpseOffsets[i];
                corners.Add(
                    altarPosition.Vector +
                    new Vector3(offset.x + 0.5f, 0.15f, offset.z + 0.5f));
            }

            return corners;
        }

        private static List<NPCBase> GetSacrificeVisualTargets(List<NPCBase> sacrificeTargets)
        {
            if (sacrificeTargets == null || sacrificeTargets.Count <= MaxSacrificeVisualTargets)
                return sacrificeTargets ?? new List<NPCBase>();

            var selected = new List<NPCBase>(MaxSacrificeVisualTargets);
            var step = Mathf.Max(1f, (float)sacrificeTargets.Count / MaxSacrificeVisualTargets);
            for (var i = 0; i < MaxSacrificeVisualTargets; i++)
            {
                var index = Mathf.Clamp(Mathf.FloorToInt(i * step), 0, sacrificeTargets.Count - 1);
                selected.Add(sacrificeTargets[index]);
            }

            return selected;
        }

        private static void RemoveCorpseBlocks(Pipliz.Vector3Int altarPosition, Colony colony, ItemTypes.ItemType corpseBlockType)
        {
            if (colony == null || corpseBlockType == null || !ItemTypes.TryGetType("air", out var airType))
                return;

            var origin = GetBlockChangeOrigin(colony);
            for (var i = 0; i < RequiredCorpseOffsets.Length; i++)
            {
                var offset = RequiredCorpseOffsets[i];
                var position = new Pipliz.Vector3Int(altarPosition.x + offset.x, altarPosition.y + offset.y, altarPosition.z + offset.z);
                var center = position.Vector + new Vector3(0.5f, 0.15f, 0.5f);
                ServerManager.SendExplosionEffect(center, 8f, 1.8f, 1f, 0.2f);
                ServerManager.SendParticleTrail(center + Vector3.up * 1.4f, center + Vector3.up * 0.1f, 0.44f);
                var typeToRemove = corpseBlockType;
                ItemTypes.ItemType placedType;
                if (World.TryGetTypeAt(position, out placedType) &&
                    placedType != null &&
                    placedType.ItemIndex != airType.ItemIndex)
                {
                    typeToRemove = placedType;
                }

                var result = World.TryChangeBlock(position, typeToRemove, airType, origin, ESetBlockFlags.DefaultAudio);
                if (result != ESetBlockResult.Success)
                    World.TryChangeBlock(position, airType, origin, ESetBlockFlags.DefaultAudio);
            }
        }

        private static void DestroyAltar(Pipliz.Vector3Int altarPosition, Colony colony)
        {
            var altarCenter = altarPosition.Vector + new Vector3(0.5f, 0.15f, 0.5f);
            ServerManager.SendExplosionEffect(altarCenter + Vector3.up * 0.9f, 8f, 4.6f, 1f, 0.2f);
            ServerManager.SendExplosionEffect(altarCenter + Vector3.up * 0.5f, 8f, 3.2f, 1f, 0.2f);
            ServerManager.SendParticleTrail(altarCenter + Vector3.up * 3.4f, altarCenter + Vector3.up * 0.15f, 0.6f);
            ServerManager.SendParticleTrail(altarCenter + new Vector3(1.1f, 0.25f, 0f), altarCenter + Vector3.up * 1.6f, 0.38f);
            ServerManager.SendParticleTrail(altarCenter + new Vector3(-1.1f, 0.25f, 0f), altarCenter + Vector3.up * 1.6f, 0.38f);
            ServerManager.SendParticleTrail(altarCenter + new Vector3(0f, 0.25f, 1.1f), altarCenter + Vector3.up * 1.6f, 0.38f);
            ServerManager.SendParticleTrail(altarCenter + new Vector3(0f, 0.25f, -1.1f), altarCenter + Vector3.up * 1.6f, 0.38f);

            if (colony == null || !ItemTypes.TryGetType("air", out var airType))
                return;

            var origin = GetBlockChangeOrigin(colony);
            ItemTypes.ItemType typeToRemove = null;
            ItemTypes.ItemType placedType;
            if (World.TryGetTypeAt(altarPosition, out placedType) &&
                placedType != null &&
                placedType.ItemIndex != airType.ItemIndex)
            {
                typeToRemove = placedType;
            }

            if (typeToRemove == null &&
                !ItemTypes.TryGetType(GameLoader.NAMESPACE + ".GoldenRitualAltar", out typeToRemove))
            {
                return;
            }

            var result = World.TryChangeBlock(altarPosition, typeToRemove, airType, origin, ESetBlockFlags.DefaultAudio);
            if (result != ESetBlockResult.Success)
                World.TryChangeBlock(altarPosition, airType, origin, ESetBlockFlags.DefaultAudio);
        }

        private static BlockChangeRequestOrigin GetBlockChangeOrigin(Colony colony)
        {
            if (colony != null)
            {
                foreach (var player in Players.ConnectedPlayers)
                {
                    if (PlayerUiGuard.CanSendStable(player) && player.OwnsColony(colony))
                        return (BlockChangeRequestOrigin)player;
                }

                return (BlockChangeRequestOrigin)colony;
            }

            return default;
        }

        private static bool EnsureAudioLoopForOwners(
            Colony colony,
            Pipliz.Vector3Int altarPosition,
            string audioCollection,
            int loopLengthMilliseconds,
            Dictionary<int, AudioManager.AudioClipPlayingID> loopIds,
            out bool resolved)
        {
            resolved = false;
            if (colony == null || string.IsNullOrEmpty(audioCollection))
                return false;

            if (!AudioManager.TryGetIndex(audioCollection, out AudioManager.AudioClipIndex clipIndex))
                return false;

            resolved = true;
            var anySent = false;

            foreach (var player in Players.ConnectedPlayers)
            {
                if (PlayerUiGuard.CanSendStable(player) && player.OwnsColony(colony))
                {
                    var playerKey = GetPlayerKey(player);
                    if (loopIds.ContainsKey(playerKey))
                    {
                        anySent = true;
                        continue;
                    }

                    try
                    {
                        var playingId = AudioManager.AudioClipPlayingID.GenerateNew();
                        AudioManager.SendPlayLoopPacket(
                            player,
                            player.PositionStanding,
                            clipIndex,
                            playingId,
                            0,
                            System.Math.Max(1000, loopLengthMilliseconds));
                        loopIds[playerKey] = playingId;
                        anySent = true;
                    }
                    catch
                    {
                    }
                }
            }

            return anySent;
        }

        private static void StopAudioLoops(Dictionary<int, AudioManager.AudioClipPlayingID> loopIds, string audioCollection)
        {
            if (loopIds == null || loopIds.Count == 0)
                return;

            if (!AudioManager.TryGetIndex(audioCollection, out AudioManager.AudioClipIndex clipIndex))
            {
                loopIds.Clear();
                return;
            }

            foreach (var player in Players.ConnectedPlayers)
            {
                if (!PlayerUiGuard.CanSendStable(player))
                    continue;

                var playerKey = GetPlayerKey(player);
                if (!loopIds.TryGetValue(playerKey, out var playingId))
                    continue;

                try
                {
                    AudioManager.Stop(player.PositionStanding, clipIndex, playingId);
                }
                catch
                {
                }
            }

            loopIds.Clear();
        }

        private static int GetPlayerKey(Players.Player player)
        {
            return player != null ? player.ID.ID.ID : -1;
        }

        private static void NotifyOwners(Colony colony, string message)
        {
            if (colony == null || string.IsNullOrEmpty(message))
                return;

            foreach (var player in Players.ConnectedPlayers)
            {
                if (player != null &&
                    PlayerUiGuard.CanSendStable(player) &&
                    player.OwnsColony(colony))
                {
                    PlayerToastManager.Show(player, message, "#d5b3b3", 5200L);
                }
            }
        }

        private static void UpdateRitualWandHotbarLabel(Players.Player player)
        {
            if (!PlayerUiGuard.CanSendStable(player))
                return;

            if (RitualWand.Item == null || !IsRitualWandSelected(player))
            {
                UIManager.RemoveUILabel(RitualWandHotbarKey, player);
                return;
            }

            UIManager.AddorUpdateUILabel(
                RitualWandHotbarKey,
                UIElementDisplayType.Global,
                GetKillCountText(),
                new Pipliz.Vector3Int(0, 164, 0),
                AnchorPresets.BottonCenter,
                900f,
                player,
                14f,
                FontType.DidactGothic,
                "#d5b3b3",
                TextAlignmentOptions.Center);
        }

        private static bool IsRitualWandSelected(Players.Player player)
        {
            if (player == null || RitualWand.Item == null)
                return false;

            if (TryResolveSelectedType(player, out var selectedType))
                return selectedType == RitualWand.Item.ItemIndex;

            return false;
        }

        private static bool TryResolveSelectedType(Players.Player player, out ushort selectedType)
        {
            selectedType = 0;
            if (player == null)
                return false;

            if (TryResolveSelectedTypeFromObject(player, out selectedType))
                return true;

            return TryResolveSelectedTypeFromObject(player.Inventory, out selectedType);
        }

        private static bool TryResolveSelectedTypeFromObject(object source, out ushort selectedType)
        {
            selectedType = 0;
            if (source == null)
                return false;

            var type = source.GetType();
            for (var i = 0; i < SelectedTypeMemberNames.Length; i++)
            {
                var memberName = SelectedTypeMemberNames[i];
                var property = type.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (property != null && TryConvertToUShort(property.GetValue(source, null), out selectedType))
                    return true;

                var field = type.GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null && TryConvertToUShort(field.GetValue(source), out selectedType))
                    return true;

                var method = type.GetMethod(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
                if (method != null && TryConvertToUShort(method.Invoke(source, null), out selectedType))
                    return true;
            }

            return false;
        }

        private static bool TryConvertToUShort(object value, out ushort selectedType)
        {
            selectedType = 0;
            if (value == null)
                return false;

            switch (value)
            {
                case ushort direct:
                    selectedType = direct;
                    return true;
                case short signedShort when signedShort >= 0:
                    selectedType = (ushort)signedShort;
                    return true;
                case int signedInt when signedInt >= 0 && signedInt <= ushort.MaxValue:
                    selectedType = (ushort)signedInt;
                    return true;
                case uint unsignedInt when unsignedInt <= ushort.MaxValue:
                    selectedType = (ushort)unsignedInt;
                    return true;
                case byte unsignedByte:
                    selectedType = unsignedByte;
                    return true;
                default:
                    return false;
            }
        }
    }

    [ModLoader.ModManager]
    public static class RitualWand
    {
        private const long PrimaryCooldownMs = 840;
        private const long PrimaryMinimumCooldownMs = 460;
        private const long CataclysmCooldownMs = 1380;
        private const long CataclysmMinimumCooldownMs = 760;
        private const float PrimarySpellDamage = 160f;
        private const float CataclysmSpellDamage = 250f;
        private const float PrimaryProjectileForce = 22f;
        private const float CataclysmProjectileForce = 30f;
        private const float PrimaryRange = 44f;
        private const float CataclysmRange = 46f;
        private const float PrimaryAimAssistRadius = 1.1f;
        private const float CataclysmAimAssistRadius = 1.2f;
        private const float PrimaryMissTrailDuration = 0.36f;
        private const float CataclysmMissTrailDuration = 0.46f;
        private const int PrimaryManaCost = 7;
        private const int CataclysmManaCost = 14;
        private const float CataclysmDamagePerTick = 34f;
        private const int CataclysmTickCount = 4;
        private const float CataclysmTickDelaySeconds = 0.75f;
        private const float CataclysmChainRadius = 7.25f;
        private const int CataclysmChainTargets = 5;
        private const float CataclysmChainDamage = 110f;
        private const float CataclysmChainProjectileForce = 20f;
        private const float CataclysmChainDamagePerTick = 22f;
        private const int CataclysmChainTickCount = 2;
        private const float CataclysmChainTickDelaySeconds = 0.65f;

        private static readonly Dictionary<Players.Player, long> Cooldowns = new Dictionary<Players.Player, long>();

        public static ItemTypesServer.ItemTypeRaw Item { get; private set; }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AddItemTypes, GameLoader.NAMESPACE + ".Items.RitualWand.Add")]
        [ModLoader.ModCallbackDependsOn("pipliz.server.applymoditempatches")]
        public static void Add(Dictionary<string, ItemTypesServer.ItemTypeRaw> items)
        {
            Item = MagicAlchemyUtility.AddWandItem(items, "RitualWand", "RitualWand.png", "magic", "wand", "weapon", GameLoader.NAMESPACE, "ritual");
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AfterItemTypesDefined, GameLoader.NAMESPACE + ".Items.RitualWand.Register")]
        [ModLoader.ModCallbackProvidesFor("pipliz.server.loadresearchables")]
        public static void Register()
        {
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnPlayerClicked, GameLoader.NAMESPACE + ".Items.RitualWand.OnPlayerClicked")]
        public static void OnPlayerClicked(Players.Player player, PlayerClickedData playerClickData)
        {
            if (!WandCombatUtility.TryPrepareWandClick(playerClickData, Item.ItemIndex))
                return;

            if (!WandCombatUtility.IsReady(player, Cooldowns))
                return;

            var multiplier = RitualWandManager.GetDamageMultiplier();
            var isCataclysm = playerClickData.ClickType == PlayerClickedData.EClickType.Right;
            if (isCataclysm)
            {
                WandCombatUtility.TryCastBurstMonsterSpell(
                    player,
                    playerClickData,
                    Cooldowns,
                    CataclysmManaCost,
                    CataclysmCooldownMs,
                    CataclysmMinimumCooldownMs,
                    CataclysmSpellDamage * multiplier,
                    CataclysmProjectileForce,
                    CataclysmRange,
                    CataclysmAimAssistRadius,
                    CataclysmMissTrailDuration,
                    0.24f,
                    0.4f,
                    CataclysmDamagePerTick * multiplier,
                    CataclysmTickCount,
                    CataclysmTickDelaySeconds,
                    CataclysmChainRadius,
                    CataclysmChainTargets,
                    CataclysmChainDamage * multiplier,
                    CataclysmChainProjectileForce,
                    CataclysmChainTickCount,
                    CataclysmChainDamagePerTick * multiplier,
                    CataclysmChainTickDelaySeconds,
                    0f,
                    WandCombatUtility.BurstVisualStyle.Rupture,
                    Item.ItemIndex,
                    Void.Item.ItemIndex,
                    WandCastAudioManager.RitualHeavyCast,
                    WandCastAudioManager.RitualHeavyImpact,
                    true,
                    true);
                return;
            }

            WandCombatUtility.TryCastMonsterSpell(
                player,
                playerClickData,
                Cooldowns,
                PrimaryManaCost,
                PrimaryCooldownMs,
                PrimaryMinimumCooldownMs,
                PrimarySpellDamage * multiplier,
                PrimaryProjectileForce,
                PrimaryRange,
                PrimaryAimAssistRadius,
                PrimaryMissTrailDuration,
                0.22f,
                0.36f,
                castIndicatorItemIndex: Item.ItemIndex,
                impactIndicatorItemIndex: Void.Item.ItemIndex,
                castAudio: WandCastAudioManager.RitualCast,
                hitAudio: WandCastAudioManager.RitualImpact,
                persistentDamageOverTime: true,
                impactVisualStyle: WandCombatUtility.BurstVisualStyle.Rupture,
                impactVisualRadius: 3.25f);
        }
    }
}
