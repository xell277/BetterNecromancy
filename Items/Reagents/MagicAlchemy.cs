using Jobs;
using Newtonsoft.Json.Linq;
using Monsters;
using NPC;
using Pandaros.API.Entities;
using Pandaros.API.Monsters;
using Recipes;
using Shared;
using System.Collections.Generic;
using Pipliz;
using Pandaros.Settlers.Monsters;
using UnityEngine;

namespace Pandaros.Settlers.Items
{
    internal static class MagicAlchemyUtility
    {
        public static ItemTypesServer.ItemTypeRaw AddItem(
            Dictionary<string, ItemTypesServer.ItemTypeRaw> items,
            string key,
            string iconFile,
            params string[] categories)
        {
            var name = GameLoader.NAMESPACE + "." + key;
            var node = new JObject
            {
                ["icon"] = GameLoader.ICON_PATH + iconFile,
                ["isPlaceable"] = false,
                ["categories"] = new JArray(categories)
            };

            var item = new ItemTypesServer.ItemTypeRaw(name, node);
            items.Add(name, item);
            return item;
        }

        public static ItemTypesServer.ItemTypeRaw AddWandItem(
            Dictionary<string, ItemTypesServer.ItemTypeRaw> items,
            string key,
            string iconFile,
            params string[] categories)
        {
            var name = GameLoader.NAMESPACE + "." + key;
            var node = new JObject
            {
                ["icon"] = GameLoader.ICON_PATH + iconFile,
                ["isPlaceable"] = false,
                ["maxStackSize"] = 1,
                ["categories"] = BuildWandCategories(categories)
            };

            var item = new ItemTypesServer.ItemTypeRaw(name, node);
            items.Add(name, item);
            return item;
        }

        private static JArray BuildWandCategories(params string[] categories)
        {
            var values = new JArray();

            if (categories == null)
                return values;

            for (var i = 0; i < categories.Length; i++)
            {
                var category = categories[i];
                if (string.IsNullOrWhiteSpace(category))
                    continue;

                if (string.Equals(category, "weapon", System.StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(category, "defense", System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var exists = false;
                for (var j = 0; j < values.Count; j++)
                {
                    if (string.Equals(values[j]?.ToString(), category, System.StringComparison.OrdinalIgnoreCase))
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                    values.Add(category);
            }

            return values;
        }

        public static void RegisterRecipe(
            string jobKey,
            string recipeName,
            List<InventoryItem> requirements,
            List<RecipeResult> results,
            int defaultLimit)
        {
            var recipe = new Recipe(recipeName, requirements, results, defaultLimit);
            ServerManager.RecipeStorage.AddLimitTypeRecipe(NPCType.GetByKeyNameOrDefault(jobKey), recipe);
        }
    }

    [ModLoader.ModManager]
    internal static class WandCastAudioManager
    {
        internal const string ManaCast = "bn_cast_mana";
        internal const string BriarCast = "bn_cast_briar";
        internal const string SparkCast = "bn_cast_spark";
        internal const string VenomCast = "bn_cast_venom";
        internal const string EmberCast = "bn_cast_ember";
        internal const string FrostCast = "bn_cast_frost";
        internal const string CrystalCast = "bn_cast_crystal";
        internal const string StoneCast = "bn_cast_stone";
        internal const string StormCast = "bn_cast_storm";
        internal const string AetherCast = "bn_cast_aether";
        internal const string BloodCast = "bn_cast_blood";
        internal const string VoidCast = "bn_cast_void";
        internal const string RitualCast = "bn_cast_ritual";
        internal const string ManaHeavyCast = "bn_hcast_mana";
        internal const string BriarHeavyCast = "bn_hcast_briar";
        internal const string SparkHeavyCast = "bn_hcast_spark";
        internal const string VenomHeavyCast = "bn_hcast_venom";
        internal const string EmberHeavyCast = "bn_hcast_ember";
        internal const string FrostHeavyCast = "bn_hcast_frost";
        internal const string CrystalHeavyCast = "bn_hcast_crystal";
        internal const string StoneHeavyCast = "bn_hcast_stone";
        internal const string StormHeavyCast = "bn_hcast_storm";
        internal const string AetherHeavyCast = "bn_hcast_aether";
        internal const string BloodHeavyCast = "bn_hcast_blood";
        internal const string VoidHeavyCast = "bn_hcast_void";
        internal const string RitualHeavyCast = "bn_hcast_ritual";
        internal const string ManaImpact = "bn_impact_mana";
        internal const string BriarImpact = "bn_impact_briar";
        internal const string SparkImpact = "bn_impact_spark";
        internal const string VenomImpact = "bn_impact_venom";
        internal const string EmberImpact = "bn_impact_ember";
        internal const string FrostImpact = "bn_impact_frost";
        internal const string CrystalImpact = "bn_impact_crystal";
        internal const string StoneImpact = "bn_impact_stone";
        internal const string StormImpact = "bn_impact_storm";
        internal const string AetherImpact = "bn_impact_aether";
        internal const string BloodImpact = "bn_impact_blood";
        internal const string VoidImpact = "bn_impact_void";
        internal const string RitualImpact = "bn_impact_ritual";
        internal const string ManaHeavyImpact = "bn_himpact_mana";
        internal const string BriarHeavyImpact = "bn_himpact_briar";
        internal const string SparkHeavyImpact = "bn_himpact_spark";
        internal const string VenomHeavyImpact = "bn_himpact_venom";
        internal const string EmberHeavyImpact = "bn_himpact_ember";
        internal const string FrostHeavyImpact = "bn_himpact_frost";
        internal const string CrystalHeavyImpact = "bn_himpact_crystal";
        internal const string StoneHeavyImpact = "bn_himpact_stone";
        internal const string StormHeavyImpact = "bn_himpact_storm";
        internal const string AetherHeavyImpact = "bn_himpact_aether";
        internal const string BloodHeavyImpact = "bn_himpact_blood";
        internal const string VoidHeavyImpact = "bn_himpact_void";
        internal const string RitualHeavyImpact = "bn_himpact_ritual";

        public static void Play(Players.Player player, Vector3 origin, string audioCollection)
        {
            if (string.IsNullOrWhiteSpace(audioCollection))
                return;

            try
            {
                AudioManager.SendAudio(origin, audioCollection);
            }
            catch
            {
            }
        }
    }

    [ModLoader.ModManager]
    public static class LegendaryWandItems
    {
        public static ItemTypesServer.ItemTypeRaw AstralManaWand { get; private set; }
        public static ItemTypesServer.ItemTypeRaw ThornheartWand { get; private set; }
        public static ItemTypesServer.ItemTypeRaw VoltspineWand { get; private set; }
        public static ItemTypesServer.ItemTypeRaw WidowrootWand { get; private set; }
        public static ItemTypesServer.ItemTypeRaw CinderlordWand { get; private set; }
        public static ItemTypesServer.ItemTypeRaw WinterglassWand { get; private set; }
        public static ItemTypesServer.ItemTypeRaw PrismSovereignWand { get; private set; }
        public static ItemTypesServer.ItemTypeRaw WorldbreakerWand { get; private set; }
        public static ItemTypesServer.ItemTypeRaw SkyfallWand { get; private set; }
        public static ItemTypesServer.ItemTypeRaw SeraphWand { get; private set; }
        public static ItemTypesServer.ItemTypeRaw CrimsonCovenantWand { get; private set; }
        public static ItemTypesServer.ItemTypeRaw EclipseWand { get; private set; }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AddItemTypes, GameLoader.NAMESPACE + ".Items.LegendaryWands.Add")]
        [ModLoader.ModCallbackDependsOn("pipliz.server.applymoditempatches")]
        public static void Add(Dictionary<string, ItemTypesServer.ItemTypeRaw> items)
        {
            AstralManaWand = MagicAlchemyUtility.AddWandItem(items, "AstralManaWand", "AstralManaWand.png", "magic", "wand", "magicitem", GameLoader.NAMESPACE);
            ThornheartWand = MagicAlchemyUtility.AddWandItem(items, "ThornheartWand", "ThornheartWand.png", "magic", "wand", "magicitem", GameLoader.NAMESPACE);
            VoltspineWand = MagicAlchemyUtility.AddWandItem(items, "VoltspineWand", "VoltspineWand.png", "magic", "wand", "magicitem", GameLoader.NAMESPACE);
            WidowrootWand = MagicAlchemyUtility.AddWandItem(items, "WidowrootWand", "WidowrootWand.png", "magic", "wand", "magicitem", GameLoader.NAMESPACE);
            CinderlordWand = MagicAlchemyUtility.AddWandItem(items, "CinderlordWand", "CinderlordWand.png", "magic", "wand", "magicitem", GameLoader.NAMESPACE);
            WinterglassWand = MagicAlchemyUtility.AddWandItem(items, "WinterglassWand", "WinterglassWand.png", "magic", "wand", "magicitem", GameLoader.NAMESPACE);
            PrismSovereignWand = MagicAlchemyUtility.AddWandItem(items, "PrismSovereignWand", "PrismSovereignWand.png", "magic", "wand", "magicitem", GameLoader.NAMESPACE);
            WorldbreakerWand = MagicAlchemyUtility.AddWandItem(items, "WorldbreakerWand", "WorldbreakerWand.png", "magic", "wand", "magicitem", GameLoader.NAMESPACE);
            SkyfallWand = MagicAlchemyUtility.AddWandItem(items, "SkyfallWand", "SkyfallWand.png", "magic", "wand", "magicitem", GameLoader.NAMESPACE);
            SeraphWand = MagicAlchemyUtility.AddWandItem(items, "SeraphWand", "SeraphWand.png", "magic", "wand", "magicitem", GameLoader.NAMESPACE);
            CrimsonCovenantWand = MagicAlchemyUtility.AddWandItem(items, "CrimsonCovenantWand", "CrimsonCovenantWand.png", "magic", "wand", "magicitem", GameLoader.NAMESPACE);
            EclipseWand = MagicAlchemyUtility.AddWandItem(items, "EclipseWand", "EclipseWand.png", "magic", "wand", "magicitem", GameLoader.NAMESPACE);
        }

        public static ushort GetItemIndexOrDefault(string masteryKey)
        {
            return TryGetItemIndex(masteryKey, out var itemIndex) ? itemIndex : (ushort)0;
        }

        public static bool TryGetItemIndex(string masteryKey, out ushort itemIndex)
        {
            itemIndex = 0;

            var item = GetItemForMasteryKey(masteryKey);
            if (item == null)
                return false;

            itemIndex = item.ItemIndex;
            return itemIndex != 0;
        }

        public static bool TryGetMasteryKeyByItemIndex(ushort itemIndex, out string masteryKey)
        {
            masteryKey = null;
            if (itemIndex == 0)
                return false;

            if (Matches(AstralManaWand, itemIndex))
                masteryKey = PlayerMagicStateManager.WandMasteryKeys.Mana;
            else if (Matches(ThornheartWand, itemIndex))
                masteryKey = PlayerMagicStateManager.WandMasteryKeys.Briar;
            else if (Matches(VoltspineWand, itemIndex))
                masteryKey = PlayerMagicStateManager.WandMasteryKeys.Spark;
            else if (Matches(WidowrootWand, itemIndex))
                masteryKey = PlayerMagicStateManager.WandMasteryKeys.Venom;
            else if (Matches(CinderlordWand, itemIndex))
                masteryKey = PlayerMagicStateManager.WandMasteryKeys.Ember;
            else if (Matches(WinterglassWand, itemIndex))
                masteryKey = PlayerMagicStateManager.WandMasteryKeys.Frost;
            else if (Matches(PrismSovereignWand, itemIndex))
                masteryKey = PlayerMagicStateManager.WandMasteryKeys.Crystal;
            else if (Matches(WorldbreakerWand, itemIndex))
                masteryKey = PlayerMagicStateManager.WandMasteryKeys.Stone;
            else if (Matches(SkyfallWand, itemIndex))
                masteryKey = PlayerMagicStateManager.WandMasteryKeys.Storm;
            else if (Matches(SeraphWand, itemIndex))
                masteryKey = PlayerMagicStateManager.WandMasteryKeys.Aether;
            else if (Matches(CrimsonCovenantWand, itemIndex))
                masteryKey = PlayerMagicStateManager.WandMasteryKeys.Blood;
            else if (Matches(EclipseWand, itemIndex))
                masteryKey = PlayerMagicStateManager.WandMasteryKeys.Void;

            return !string.IsNullOrEmpty(masteryKey);
        }

        private static ItemTypesServer.ItemTypeRaw GetItemForMasteryKey(string masteryKey)
        {
            switch ((masteryKey ?? string.Empty).Trim())
            {
                case PlayerMagicStateManager.WandMasteryKeys.Mana:
                    return AstralManaWand;
                case PlayerMagicStateManager.WandMasteryKeys.Briar:
                    return ThornheartWand;
                case PlayerMagicStateManager.WandMasteryKeys.Spark:
                    return VoltspineWand;
                case PlayerMagicStateManager.WandMasteryKeys.Venom:
                    return WidowrootWand;
                case PlayerMagicStateManager.WandMasteryKeys.Ember:
                    return CinderlordWand;
                case PlayerMagicStateManager.WandMasteryKeys.Frost:
                    return WinterglassWand;
                case PlayerMagicStateManager.WandMasteryKeys.Crystal:
                    return PrismSovereignWand;
                case PlayerMagicStateManager.WandMasteryKeys.Stone:
                    return WorldbreakerWand;
                case PlayerMagicStateManager.WandMasteryKeys.Storm:
                    return SkyfallWand;
                case PlayerMagicStateManager.WandMasteryKeys.Aether:
                    return SeraphWand;
                case PlayerMagicStateManager.WandMasteryKeys.Blood:
                    return CrimsonCovenantWand;
                case PlayerMagicStateManager.WandMasteryKeys.Void:
                    return EclipseWand;
                default:
                    return null;
            }
        }

        private static bool Matches(ItemTypesServer.ItemTypeRaw item, ushort itemIndex)
        {
            return item != null && item.ItemIndex == itemIndex;
        }
    }

    internal static class WandCombatUtility
    {
        private const float CastOriginOffset = 0.35f;
        private const float MinimumCastHeightAboveStanding = 1.15f;
        private const float HitTrailOriginOffset = 0.15f;
        private const long FailedCastCooldownMs = 750;
        private const int PersistentDamageTickCount = 3600;
        private const float MinimumDamageOverTimeDurationSeconds = 180f;
        private const float DamageOverTimeStackDamageMultiplier = 4.25f;

        public enum BurstVisualStyle
        {
            None,
            ManaPulse,
            SparkArc,
            ArcBurst,
            Bramble,
            Inferno,
            Shatter,
            Prism,
            AetherWave,
            Hemorrhage,
            Rupture,
            ToxicBloom,
            Faultline,
            Tempest
        }

        public static bool IsReady(Players.Player player, Dictionary<Players.Player, long> cooldowns)
        {
            return !cooldowns.TryGetValue(player, out var nextReadyAt) ||
                Pipliz.Time.MillisecondsSinceStart > nextReadyAt;
        }

        public static bool TryPrepareWandClick(PlayerClickedData playerClickData, params ushort[] itemIndexes)
        {
            if ((playerClickData.ClickType != PlayerClickedData.EClickType.Left &&
                 playerClickData.ClickType != PlayerClickedData.EClickType.Right))
            {
                return false;
            }

            var selectedMatches = false;
            if (itemIndexes != null)
            {
                for (var i = 0; i < itemIndexes.Length; i++)
                {
                    if (itemIndexes[i] != 0 && playerClickData.TypeSelected == itemIndexes[i])
                    {
                        selectedMatches = true;
                        break;
                    }
                }
            }

            if (!selectedMatches)
                return false;

            playerClickData.ConsumedType = PlayerClickedData.EConsumedType.ChangedBlock;
            return true;
        }

        public static ushort GetSelectedWandItemIndex(PlayerClickedData playerClickData, ushort fallbackItemIndex)
        {
            return playerClickData != null && playerClickData.TypeSelected != 0
                ? playerClickData.TypeSelected
                : fallbackItemIndex;
        }

        public static void SetCooldown(Players.Player player, Dictionary<Players.Player, long> cooldowns, long cooldownMs)
        {
            cooldowns[player] = Pipliz.Time.MillisecondsSinceStart + cooldownMs;
        }

        public static long GetAdjustedCooldownMs(Players.Player player, long baseCooldownMs, long minimumCooldownMs)
        {
            var cooldown = baseCooldownMs - PlayerMagicStateManager.GetMagicCooldownReductionMs(player);
            return System.Math.Max(minimumCooldownMs, cooldown);
        }

        public static bool TryGetShotDirection(PlayerClickedData playerClickData, out Vector3 shotDirection)
        {
            shotDirection = playerClickData.PlayerAimDirection.normalized;
            return shotDirection.sqrMagnitude >= 0.0001f;
        }

        public static Vector3 GetTrailOrigin(Players.Player player, Vector3 shotDirection)
        {
            var standingOrigin = player.PositionStanding + Vector3.up * MinimumCastHeightAboveStanding;
            var cameraOrigin = player.PositionCamera;

            if (cameraOrigin.y > standingOrigin.y)
                standingOrigin = cameraOrigin;

            return standingOrigin + shotDirection * CastOriginOffset;
        }

        public static bool TrySpendManaForCast(
            Players.Player player,
            PlayerClickedData playerClickData,
            Dictionary<Players.Player, long> cooldowns,
            int manaCost)
        {
            var adjustedManaCost = System.Math.Max(1, manaCost - PlayerMagicStateManager.GetMagicManaCostReduction(player, manaCost));

            if (adjustedManaCost <= 0)
                return true;

            if (!PlayerMagicStateManager.HasEnoughMana(player, adjustedManaCost))
            {
                playerClickData.ConsumedType = PlayerClickedData.EConsumedType.ChangedBlock;
                PlayerMagicStateManager.WarnNotEnoughMana(player, string.Empty);
                SetCooldown(player, cooldowns, FailedCastCooldownMs);
                return false;
            }

            return PlayerMagicStateManager.TrySpendMana(player, adjustedManaCost, null);
        }

        public static IMonster GetAimedMonster(
            PlayerClickedData playerClickData,
            Vector3 trailOrigin,
            Vector3 shotDirection,
            float range,
            float aimAssistRadius)
        {
            var directHit = TryGetDirectTargetMonster(playerClickData, trailOrigin, range);
            if (directHit != null)
                return directHit;

            var allMonsters = MonsterTracker.GetAllMonstersByID();
            if (allMonsters == null || allMonsters.Count == 0)
                return null;

            IMonster bestMatch = null;
            var bestDistance = float.MaxValue;

            foreach (var monster in allMonsters.Values)
            {
                if (monster == null || !monster.IsValid)
                    continue;

                if (monster is IPandaBoss &&
                    TryGetBossRayHit(monster, trailOrigin, shotDirection, range, aimAssistRadius, out var bossForwardDistance, out _))
                {
                    if (bossForwardDistance < bestDistance)
                    {
                        bestDistance = bossForwardDistance;
                        bestMatch = monster;
                    }

                    continue;
                }

                var aimPoint = monster.PositionToAimFor;
                var toTarget = aimPoint - trailOrigin;
                var forwardDistance = Vector3.Dot(toTarget, shotDirection);

                if (forwardDistance <= 0f || forwardDistance > range)
                    continue;

                var closestPoint = trailOrigin + shotDirection * forwardDistance;
                var distanceToRay = Vector3.Distance(aimPoint, closestPoint);
                if (distanceToRay > aimAssistRadius)
                    continue;

                if (!VoxelPhysics.CanSee(trailOrigin, aimPoint))
                    continue;

                if (forwardDistance < bestDistance)
                {
                    bestDistance = forwardDistance;
                    bestMatch = monster;
                }
            }

            return bestMatch;
        }

        public static NPCBase TryGetDirectTargetNpc(PlayerClickedData playerClickData, float range)
        {
            if (playerClickData.HitType != PlayerClickedData.EHitType.NPC)
                return null;

            var npcHit = playerClickData.GetNPCHit();
            if (npcHit.DistanceToHit > range)
                return null;

            if (MonsterTracker.TryGetMonsterByID(npcHit.NPCID, out var monster) &&
                monster != null &&
                monster.IsValid)
            {
                return null;
            }

            return NPCTracker.TryGetNPC(npcHit.NPCID, out var npc) ? npc : null;
        }

        public static NPCBase GetAimedNpc(
            PlayerClickedData playerClickData,
            Vector3 trailOrigin,
            Vector3 shotDirection,
            float range,
            float aimAssistRadius)
        {
            var directHit = TryGetDirectTargetNpc(playerClickData, range);
            if (directHit != null && VoxelPhysics.CanSee(trailOrigin, directHit.Position.Vector + Vector3.up * 1.4f))
                return directHit;

            var collector = new NpcRayCollector
            {
                Origin = trailOrigin,
                Direction = shotDirection,
                Range = range,
                AimAssistRadius = aimAssistRadius,
                BestDistance = float.MaxValue
            };

            var min = new Pipliz.Vector3Int(trailOrigin - Vector3.one * range);
            var max = new Pipliz.Vector3Int(trailOrigin + Vector3.one * range);
            NPCTracker.IterateNPCs(min, max, ref collector);
            return collector.BestNpc;
        }

        public static bool TryGetSurfaceImpact(
            PlayerClickedData playerClickData,
            Vector3 trailOrigin,
            Vector3 shotDirection,
            float range,
            out Vector3 impactPosition,
            out float impactDistance)
        {
            impactPosition = Vector3.zero;
            impactDistance = range;

            if (playerClickData.HitType != PlayerClickedData.EHitType.Block)
                return false;

            var rawDistance = playerClickData.GetDistanceToHit();
            if (rawDistance <= 0f || rawDistance > range)
                return false;

            var exactImpact = playerClickData.GetExactHitPositionWorld();
            if ((exactImpact - trailOrigin).sqrMagnitude < 0.0001f)
                exactImpact = trailOrigin + shotDirection * rawDistance;

            impactPosition = exactImpact - shotDirection * 0.05f;
            impactDistance = Mathf.Min(range, Vector3.Distance(trailOrigin, impactPosition));
            return impactDistance > 0.05f;
        }

        public static bool TryCastMonsterSpell(
            Players.Player player,
            PlayerClickedData playerClickData,
            Dictionary<Players.Player, long> cooldowns,
            int manaCost,
            long cooldownMs,
            long minimumCooldownMs,
            float baseDamage,
            float projectileForce,
            float range,
            float aimAssistRadius,
            float missTrailDuration,
            float hitTrailDurationMin,
            float hitTrailDurationMax,
            float dotDamagePerTick = 0f,
            int dotTickCount = 0,
            float dotTickDelaySeconds = 0f,
            float selfHealOnHit = 0f,
            ushort castIndicatorItemIndex = 0,
            ushort impactIndicatorItemIndex = 0,
            string castAudio = "bowShoot",
            string hitAudio = "fleshHit",
            bool persistentDamageOverTime = false,
            BurstVisualStyle impactVisualStyle = BurstVisualStyle.None,
            float impactVisualRadius = 0f,
            string masteryKey = null)
        {
            return TryCastBurstMonsterSpell(
                player,
                playerClickData,
                cooldowns,
                manaCost,
                cooldownMs,
                minimumCooldownMs,
                baseDamage,
                projectileForce,
                range,
                aimAssistRadius,
                missTrailDuration,
                hitTrailDurationMin,
                hitTrailDurationMax,
                dotDamagePerTick,
                dotTickCount,
                dotTickDelaySeconds,
                impactVisualRadius,
                0,
                0f,
                0f,
                0,
                0f,
                0f,
                selfHealOnHit,
                impactVisualStyle,
                castIndicatorItemIndex,
                impactIndicatorItemIndex,
                castAudio,
                hitAudio,
                persistentDamageOverTime,
                false,
                masteryKey);
        }

        public static void PlayVisualEffect(Vector3 origin, Vector3 shotDirection, BurstVisualStyle style, float radius)
        {
            SendBurstVisual(origin, shotDirection, style, radius);
        }

        public static bool TryCastBurstMonsterSpell(
            Players.Player player,
            PlayerClickedData playerClickData,
            Dictionary<Players.Player, long> cooldowns,
            int manaCost,
            long cooldownMs,
            long minimumCooldownMs,
            float baseDamage,
            float projectileForce,
            float range,
            float aimAssistRadius,
            float missTrailDuration,
            float hitTrailDurationMin,
            float hitTrailDurationMax,
            float dotDamagePerTick,
            int dotTickCount,
            float dotTickDelaySeconds,
            float burstRadius,
            int maxBurstTargets,
            float burstDamage,
            float burstProjectileForce,
            int burstDotTickCount,
            float burstDotDamagePerTick,
            float burstDotTickDelaySeconds,
            float selfHealOnHit = 0f,
            BurstVisualStyle burstVisualStyle = BurstVisualStyle.None,
            ushort castIndicatorItemIndex = 0,
            ushort impactIndicatorItemIndex = 0,
            string castAudio = "bowShoot",
            string hitAudio = "fleshHit",
            bool persistentDamageOverTime = false,
            bool persistentBurstDamageOverTime = false,
            string masteryKey = null)
        {
            if (!TryGetShotDirection(playerClickData, out var shotDirection))
                return false;

            var effectiveManaCost = System.Math.Max(1, manaCost - PlayerMagicStateManager.GetWandMasteryManaDiscount(player, masteryKey));
            if (!TrySpendManaForCast(player, playerClickData, cooldowns, effectiveManaCost))
                return false;

            var effectiveBaseDamage = baseDamage * PlayerMagicStateManager.GetWandMasteryDamageMultiplier(player, masteryKey);
            var effectiveProjectileForce = projectileForce * PlayerMagicStateManager.GetWandMasteryForceMultiplier(player, masteryKey);
            var effectiveRange = range + PlayerMagicStateManager.GetWandMasteryRangeBonus(player, masteryKey);
            var effectiveDotDamagePerTick = dotDamagePerTick * PlayerMagicStateManager.GetWandMasteryDotMultiplier(player, masteryKey);
            var effectiveBurstRadius = burstRadius + PlayerMagicStateManager.GetWandMasteryRadiusBonus(player, masteryKey);
            var effectiveBurstDamage = burstDamage * PlayerMagicStateManager.GetWandMasteryDamageMultiplier(player, masteryKey);
            var effectiveBurstProjectileForce = burstProjectileForce * PlayerMagicStateManager.GetWandMasteryForceMultiplier(player, masteryKey);
            var effectiveBurstDotDamagePerTick = burstDotDamagePerTick * PlayerMagicStateManager.GetWandMasteryDotMultiplier(player, masteryKey);
            var effectiveSelfHealOnHit = selfHealOnHit * PlayerMagicStateManager.GetWandMasteryHealMultiplier(player, masteryKey);
            var effectiveMaxBurstTargets = maxBurstTargets + PlayerMagicStateManager.GetWandMasteryExtraTargets(player, masteryKey);
            var effectiveCooldownMs = System.Math.Max(minimumCooldownMs, cooldownMs - PlayerMagicStateManager.GetWandMasteryCooldownReductionMs(player, masteryKey));
            var selectedWandItemIndex = GetSelectedWandItemIndex(playerClickData, castIndicatorItemIndex);

            playerClickData.ConsumedType = PlayerClickedData.EConsumedType.ChangedBlock;

            var trailOrigin = GetTrailOrigin(player, shotDirection);
            var hasSurfaceImpact = TryGetSurfaceImpact(playerClickData, trailOrigin, shotDirection, effectiveRange, out var surfaceImpactPosition, out var surfaceImpactDistance);
            var effectiveTargetRange = hasSurfaceImpact ? Mathf.Min(effectiveRange, surfaceImpactDistance) : effectiveRange;
            var monster = GetAimedMonster(playerClickData, trailOrigin, shotDirection, effectiveTargetRange, aimAssistRadius);
            var directNpc = monster == null ? GetAimedNpc(playerClickData, trailOrigin, shotDirection, effectiveTargetRange, aimAssistRadius) : null;

            if (selectedWandItemIndex != 0)
                PlayerMagicStateManager.ShowItemIndicator(player, selectedWandItemIndex);

            WandCastAudioManager.Play(player, trailOrigin, castAudio);

            if (monster != null)
            {
                var impactPosition = monster.PositionToAimFor;
                SendCastTravelVisual(trailOrigin, impactPosition, shotDirection, burstVisualStyle, effectiveBurstRadius);
                var hitTrailDuration = Pipliz.Random.NextFloat(hitTrailDurationMin, hitTrailDurationMax);
                if (IsLightningStyle(burstVisualStyle))
                    SendLightningTrail(trailOrigin + shotDirection * HitTrailOriginOffset, impactPosition, hitTrailDuration, burstVisualStyle == BurstVisualStyle.Tempest ? 1.3f : 1f, burstVisualStyle == BurstVisualStyle.Tempest ? 7 : 6, burstVisualStyle != BurstVisualStyle.SparkArc);
                else
                    ServerManager.SendParticleTrail(
                        trailOrigin + shotDirection * HitTrailOriginOffset,
                        monster.ID,
                        hitTrailDuration);

                var hitForce = shotDirection * effectiveProjectileForce;
                ApplyMonsterHit(player, monster, hitForce, effectiveBaseDamage, effectiveDotDamagePerTick, dotTickCount, dotTickDelaySeconds, persistentDamageOverTime, burstVisualStyle);
                if (!string.IsNullOrWhiteSpace(masteryKey) && selectedWandItemIndex != 0)
                {
                    PlayerMagicStateManager.RecordWandMasteryEvent(player, masteryKey, selectedWandItemIndex, "OnHit", 1);
                    if (effectiveDotDamagePerTick > 0f && dotTickCount > 0)
                        PlayerMagicStateManager.RecordWandMasteryEvent(player, masteryKey, selectedWandItemIndex, "OnStatusApplied", 1);
                    if (monster is Zombie directZombie && BetterNecromancy.EliteZombieManager.IsElite(directZombie))
                        PlayerMagicStateManager.RecordWandMasteryEvent(player, masteryKey, selectedWandItemIndex, "OnEliteDamage", 1);
                    if (monster is IPandaBoss)
                        PlayerMagicStateManager.RecordWandMasteryEvent(player, masteryKey, selectedWandItemIndex, "OnBossDamage", 1);
                }
                SendImpactFeedback(impactPosition, impactIndicatorItemIndex, hitAudio);

                if (effectiveBurstRadius > 0f && effectiveMaxBurstTargets > 0 && effectiveBurstDamage > 0f)
                {
                    var burstHits = ApplyAreaBurst(player, impactPosition, monster, effectiveBurstRadius, effectiveMaxBurstTargets, effectiveBurstDamage, effectiveBurstProjectileForce, 0.28f, effectiveBurstDotDamagePerTick, burstDotTickCount, burstDotTickDelaySeconds, persistentBurstDamageOverTime, burstVisualStyle);
                    if (!string.IsNullOrWhiteSpace(masteryKey) && selectedWandItemIndex != 0 && burstHits > 0)
                    {
                        PlayerMagicStateManager.RecordWandMasteryEvent(player, masteryKey, selectedWandItemIndex, "OnHit", burstHits);
                        if (burstHits >= 2)
                            PlayerMagicStateManager.RecordWandMasteryEvent(player, masteryKey, selectedWandItemIndex, "OnMultiKill", 1);
                        if (effectiveBurstDotDamagePerTick > 0f && burstDotTickCount > 0)
                            PlayerMagicStateManager.RecordWandMasteryEvent(player, masteryKey, selectedWandItemIndex, "OnStatusApplied", burstHits);
                    }
                }

                SendBurstVisual(impactPosition, shotDirection, burstVisualStyle, effectiveBurstRadius);

                if (effectiveSelfHealOnHit > 0f)
                    HealPlayer(player, effectiveSelfHealOnHit);

                ApplyMasterySignature(
                    player,
                    masteryKey,
                    monster,
                    impactPosition,
                    shotDirection,
                    effectiveBaseDamage,
                    effectiveProjectileForce,
                    effectiveDotDamagePerTick,
                    dotTickCount,
                    dotTickDelaySeconds,
                    persistentDamageOverTime,
                    effectiveBurstRadius,
                    effectiveBurstDamage,
                    effectiveBurstProjectileForce,
                    effectiveBurstDotDamagePerTick,
                    burstDotTickCount,
                    burstDotTickDelaySeconds,
                    persistentBurstDamageOverTime,
                    effectiveSelfHealOnHit);

                var inheritedApplications = ApplyLineageInheritedEffects(
                    player,
                    masteryKey,
                    monster,
                    impactPosition,
                    shotDirection,
                    effectiveBaseDamage,
                    effectiveProjectileForce,
                    effectiveBurstRadius,
                    effectiveBurstDamage,
                    effectiveBurstProjectileForce);
                if (inheritedApplications > 0 && !string.IsNullOrWhiteSpace(masteryKey) && selectedWandItemIndex != 0)
                    PlayerMagicStateManager.RecordWandMasteryEvent(player, masteryKey, selectedWandItemIndex, "OnStatusApplied", inheritedApplications);
            }
            else if (directNpc != null)
            {
                SendCastTravelVisual(trailOrigin, directNpc.Position.Vector + Vector3.up * 1.2f, shotDirection, burstVisualStyle, effectiveBurstRadius);
                var npcImpactPosition = directNpc.Position.Vector + Vector3.up * 1.4f;
                var hitTrailDuration = Pipliz.Random.NextFloat(hitTrailDurationMin, hitTrailDurationMax);
                if (IsLightningStyle(burstVisualStyle))
                    SendLightningTrail(trailOrigin + shotDirection * HitTrailOriginOffset, npcImpactPosition, hitTrailDuration, burstVisualStyle == BurstVisualStyle.Tempest ? 1.3f : 1f, burstVisualStyle == BurstVisualStyle.Tempest ? 7 : 6, burstVisualStyle != BurstVisualStyle.SparkArc);
                else
                    ServerManager.SendParticleTrail(
                        trailOrigin + shotDirection * HitTrailOriginOffset,
                        npcImpactPosition,
                        hitTrailDuration);

                directNpc.OnHit(GetMagicDamage(player, effectiveBaseDamage), shotDirection * effectiveProjectileForce, player, ModLoader.OnHitData.EHitSourceType.PlayerProjectile);
                if (!string.IsNullOrWhiteSpace(masteryKey) && selectedWandItemIndex != 0)
                    PlayerMagicStateManager.RecordWandMasteryEvent(player, masteryKey, selectedWandItemIndex, "OnHit", 1);
                SendImpactFeedback(directNpc.Position.Vector + Vector3.up * 1.2f, impactIndicatorItemIndex, hitAudio);

                SendBurstVisual(directNpc.Position.Vector + Vector3.up * 1.2f, shotDirection, burstVisualStyle, effectiveBurstRadius);

                if (effectiveSelfHealOnHit > 0f)
                    HealPlayer(player, effectiveSelfHealOnHit);
            }
            else if (hasSurfaceImpact)
            {
                SendCastTravelVisual(trailOrigin, surfaceImpactPosition, shotDirection, burstVisualStyle, effectiveBurstRadius);
                if (IsLightningStyle(burstVisualStyle))
                    SendLightningTrail(trailOrigin, surfaceImpactPosition, missTrailDuration, burstVisualStyle == BurstVisualStyle.Tempest ? 1.25f : 1f, burstVisualStyle == BurstVisualStyle.Tempest ? 7 : 6, burstVisualStyle != BurstVisualStyle.SparkArc);
                else
                    ServerManager.SendParticleTrail(trailOrigin, surfaceImpactPosition, missTrailDuration);

                SendBurstVisual(surfaceImpactPosition, shotDirection, burstVisualStyle, effectiveBurstRadius);
            }
            else
            {
                var trailEnd = trailOrigin + shotDirection * effectiveRange;
                SendCastTravelVisual(trailOrigin, trailEnd, shotDirection, burstVisualStyle, effectiveBurstRadius);
                if (IsLightningStyle(burstVisualStyle))
                    SendLightningTrail(trailOrigin, trailEnd, missTrailDuration, burstVisualStyle == BurstVisualStyle.Tempest ? 1.25f : 1f, burstVisualStyle == BurstVisualStyle.Tempest ? 7 : 6, burstVisualStyle != BurstVisualStyle.SparkArc);
                else
                    ServerManager.SendParticleTrail(trailOrigin, trailEnd, missTrailDuration);
            }

            SetCooldown(player, cooldowns, GetAdjustedCooldownMs(player, effectiveCooldownMs, minimumCooldownMs));
            if (!string.IsNullOrWhiteSpace(masteryKey) && selectedWandItemIndex != 0)
                PlayerMagicStateManager.RecordWandMasteryUse(player, masteryKey, selectedWandItemIndex, playerClickData.ClickType == PlayerClickedData.EClickType.Right ? 2 : 1);
            return true;
        }

        public static int ApplyAreaBurst(
            Players.Player player,
            Vector3 burstOrigin,
            IMonster primaryTarget,
            float burstRadius,
            int maxBurstTargets,
            float baseDamage,
            float projectileForce,
            float trailDuration,
            float dotDamagePerTick = 0f,
            int dotTickCount = 0,
            float dotTickDelaySeconds = 0f,
            bool persistentDamageOverTime = false,
            BurstVisualStyle burstVisualStyle = BurstVisualStyle.None)
        {
            if (burstRadius <= 0f || maxBurstTargets <= 0 || baseDamage <= 0f)
                return 0;

            var allMonsters = MonsterTracker.GetAllMonstersByID();
            if (allMonsters == null || allMonsters.Count == 0)
                return 0;

            var burstRadiusSquared = burstRadius * burstRadius;
            var targets = new List<IMonster>(maxBurstTargets);

            foreach (var monster in allMonsters.Values)
            {
                if (monster == null || !monster.IsValid || monster == primaryTarget)
                    continue;

                var aimPoint = monster.PositionToAimFor;
                var toTarget = aimPoint - burstOrigin;
                if (toTarget.sqrMagnitude > burstRadiusSquared)
                    continue;

                if (!VoxelPhysics.CanSee(burstOrigin, aimPoint))
                    continue;

                targets.Add(monster);
            }

            targets.Sort((left, right) =>
                (left.PositionToAimFor - burstOrigin).sqrMagnitude.CompareTo((right.PositionToAimFor - burstOrigin).sqrMagnitude));

            var appliedHits = 0;
            var chainFromPreviousTarget = burstVisualStyle == BurstVisualStyle.ArcBurst || burstVisualStyle == BurstVisualStyle.Tempest || burstVisualStyle == BurstVisualStyle.SparkArc;
            var chainOrigin = primaryTarget != null && primaryTarget.IsValid ? primaryTarget.PositionToAimFor : burstOrigin;
            for (var i = 0; i < targets.Count && appliedHits < maxBurstTargets; i++)
            {
                var target = targets[i];
                if (target == null || !target.IsValid)
                    continue;

                var forceDirection = target.PositionToAimFor - burstOrigin;
                if (forceDirection.sqrMagnitude < 0.0001f)
                    forceDirection = Vector3.up;
                else
                    forceDirection.Normalize();

                var trailStart = chainFromPreviousTarget ? chainOrigin : burstOrigin;
                if (IsLightningStyle(burstVisualStyle))
                    SendLightningTrail(trailStart, target.PositionToAimFor, trailDuration, burstVisualStyle == BurstVisualStyle.Tempest ? 1.25f : 1f, burstVisualStyle == BurstVisualStyle.Tempest ? 7 : 6, burstVisualStyle != BurstVisualStyle.SparkArc);
                else
                    ServerManager.SendParticleTrail(trailStart, target.ID, trailDuration);
                ApplyMonsterHit(player, target, forceDirection * projectileForce, baseDamage, dotDamagePerTick, dotTickCount, dotTickDelaySeconds, persistentDamageOverTime, burstVisualStyle);
                chainOrigin = target.PositionToAimFor;
                appliedHits++;
            }

            return appliedHits;
        }

        private static void ApplyMasterySignature(
            Players.Player player,
            string masteryKey,
            IMonster primaryTarget,
            Vector3 impactPosition,
            Vector3 shotDirection,
            float baseDamage,
            float projectileForce,
            float dotDamagePerTick,
            int dotTickCount,
            float dotTickDelaySeconds,
            bool persistentDamageOverTime,
            float burstRadius,
            float burstDamage,
            float burstProjectileForce,
            float burstDotDamagePerTick,
            int burstDotTickCount,
            float burstDotTickDelaySeconds,
            bool persistentBurstDamageOverTime,
            float selfHealOnHit)
        {
            if (player == null || primaryTarget == null || !primaryTarget.IsValid || string.IsNullOrWhiteSpace(masteryKey))
                return;

            var masteryLevel = PlayerMagicStateManager.GetWandMasteryLevel(player, masteryKey);
            if (masteryLevel < 2)
                return;

            var legendaryUnlocked = PlayerMagicStateManager.IsWandLegendaryEvolutionUnlocked(player, masteryKey);

            var signatureBaseDamage = Mathf.Max(baseDamage, burstDamage);
            var signatureForce = Mathf.Max(projectileForce, burstProjectileForce);
            var signatureDotDamage = Mathf.Max(dotDamagePerTick, burstDotDamagePerTick);
            var signatureDotTickCount = Mathf.Max(dotTickCount, burstDotTickCount);
            var signatureDotTickDelay = Mathf.Max(dotTickDelaySeconds, burstDotTickDelaySeconds);
            var signaturePersistentDot = persistentDamageOverTime || persistentBurstDamageOverTime;

            switch (masteryKey)
            {
                case PlayerMagicStateManager.WandMasteryKeys.Mana:
                {
                    var manaRestore = masteryLevel >= 3 ? 2 : 1;
                    var manaItemIndex = Mana.Item != null ? Mana.Item.ItemIndex : (ushort)0;
                    PlayerMagicStateManager.RestoreMana(player, manaRestore, manaItemIndex);
                    SendBurstVisual(impactPosition, shotDirection, BurstVisualStyle.ManaPulse, Mathf.Max(2.3f, burstRadius * 0.55f));

                    if (masteryLevel >= 3)
                    {
                        var echoRadius = Mathf.Max(3.1f, burstRadius * 0.7f);
                        var reboundCenter = GetMasteryOffsetPoint(impactPosition, shotDirection, 0f, 1.1f, 0.15f);
                        SendBurstVisual(reboundCenter, shotDirection, BurstVisualStyle.ManaPulse, echoRadius);
                        ApplyAreaBurst(player, reboundCenter, primaryTarget, echoRadius, 1, signatureBaseDamage * 0.35f, signatureForce * 0.45f, 0.2f, burstVisualStyle: BurstVisualStyle.ManaPulse);
                    }
                    break;
                }
                case PlayerMagicStateManager.WandMasteryKeys.Briar:
                {
                    var echoRadius = masteryLevel >= 3 ? 3.4f : 2.6f;
                    var echoTargets = masteryLevel >= 3 ? 2 : 1;
                    SendBurstVisual(impactPosition, shotDirection, BurstVisualStyle.Bramble, echoRadius);
                    ApplyAreaBurst(player, impactPosition, primaryTarget, echoRadius, echoTargets, signatureBaseDamage * (masteryLevel >= 3 ? 0.34f : 0.22f), signatureForce * 0.35f, 0.22f, signatureDotDamage * (masteryLevel >= 3 ? 0.45f : 0.30f), signatureDotTickCount, signatureDotTickDelay, signaturePersistentDot, BurstVisualStyle.Bramble);
                    var leftRingCenter = GetMasteryOffsetPoint(impactPosition, shotDirection, -0.9f, 0.35f);
                    SendBurstVisual(leftRingCenter, shotDirection, BurstVisualStyle.Bramble, echoRadius * 0.75f);
                    ApplyAreaBurst(player, leftRingCenter, primaryTarget, echoRadius * 0.75f, 1, signatureBaseDamage * 0.16f, signatureForce * 0.25f, 0.22f, signatureDotDamage * 0.24f, signatureDotTickCount, signatureDotTickDelay, signaturePersistentDot, BurstVisualStyle.Bramble);
                    if (masteryLevel >= 3)
                    {
                        var rightRingCenter = GetMasteryOffsetPoint(impactPosition, shotDirection, 0.9f, 0.35f);
                        SendBurstVisual(rightRingCenter, shotDirection, BurstVisualStyle.Bramble, echoRadius * 0.85f);
                        ApplyAreaBurst(player, rightRingCenter, primaryTarget, echoRadius * 0.85f, 1, signatureBaseDamage * 0.2f, signatureForce * 0.25f, 0.22f, signatureDotDamage * 0.28f, signatureDotTickCount, signatureDotTickDelay, signaturePersistentDot, BurstVisualStyle.Bramble);
                    }
                    break;
                }
                case PlayerMagicStateManager.WandMasteryKeys.Spark:
                {
                    var echoRadius = masteryLevel >= 3 ? 5f : 4f;
                    var echoTargets = masteryLevel >= 3 ? 2 : 1;
                    SendBurstVisual(impactPosition, shotDirection, BurstVisualStyle.SparkArc, echoRadius);
                    ApplyAreaBurst(player, impactPosition, primaryTarget, echoRadius, echoTargets, signatureBaseDamage * (masteryLevel >= 3 ? 0.4f : 0.28f), signatureForce * 0.55f, 0.18f, burstVisualStyle: BurstVisualStyle.SparkArc);
                    var reboundCenter = GetMasteryOffsetPoint(impactPosition, shotDirection, 1.05f, 0.2f, 0.1f);
                    SendBurstVisual(reboundCenter, shotDirection, BurstVisualStyle.SparkArc, echoRadius * 0.82f);
                    ApplyAreaBurst(player, reboundCenter, primaryTarget, echoRadius * 0.82f, 1, signatureBaseDamage * 0.18f, signatureForce * 0.4f, 0.16f, burstVisualStyle: BurstVisualStyle.SparkArc);
                    if (masteryLevel >= 3)
                    {
                        var oppositeCenter = GetMasteryOffsetPoint(impactPosition, shotDirection, -1.05f, 0.2f, 0.1f);
                        SendBurstVisual(oppositeCenter, shotDirection, BurstVisualStyle.ArcBurst, echoRadius * 0.92f);
                        ApplyAreaBurst(player, oppositeCenter, primaryTarget, echoRadius * 0.92f, 1, signatureBaseDamage * 0.24f, signatureForce * 0.45f, 0.16f, burstVisualStyle: BurstVisualStyle.ArcBurst);
                    }
                    break;
                }
                case PlayerMagicStateManager.WandMasteryKeys.Venom:
                {
                    var echoRadius = masteryLevel >= 3 ? 4.4f : 3.2f;
                    var echoTargets = masteryLevel >= 3 ? 2 : 1;
                    SendBurstVisual(impactPosition, shotDirection, BurstVisualStyle.ToxicBloom, echoRadius);
                    ApplyAreaBurst(player, impactPosition, primaryTarget, echoRadius, echoTargets, signatureBaseDamage * (masteryLevel >= 3 ? 0.28f : 0.18f), signatureForce * 0.25f, 0.24f, signatureDotDamage * (masteryLevel >= 3 ? 0.65f : 0.45f), signatureDotTickCount, signatureDotTickDelay, signaturePersistentDot, BurstVisualStyle.ToxicBloom);
                    var bloomCenter = GetMasteryOffsetPoint(impactPosition, shotDirection, 0f, 0.9f, 0.05f);
                    SendBurstVisual(bloomCenter, shotDirection, BurstVisualStyle.ToxicBloom, echoRadius * 0.85f);
                    ApplyAreaBurst(player, bloomCenter, primaryTarget, echoRadius * 0.85f, 1, signatureBaseDamage * 0.12f, signatureForce * 0.15f, 0.24f, signatureDotDamage * 0.32f, signatureDotTickCount, signatureDotTickDelay, signaturePersistentDot, BurstVisualStyle.ToxicBloom);
                    if (masteryLevel >= 3)
                    {
                        var sideBloomCenter = GetMasteryOffsetPoint(impactPosition, shotDirection, 0.95f, -0.1f, 0.05f);
                        SendBurstVisual(sideBloomCenter, shotDirection, BurstVisualStyle.ToxicBloom, echoRadius * 0.75f);
                        ApplyAreaBurst(player, sideBloomCenter, primaryTarget, echoRadius * 0.75f, 1, signatureBaseDamage * 0.1f, signatureForce * 0.15f, 0.24f, signatureDotDamage * 0.28f, signatureDotTickCount, signatureDotTickDelay, signaturePersistentDot, BurstVisualStyle.ToxicBloom);
                    }
                    break;
                }
                case PlayerMagicStateManager.WandMasteryKeys.Ember:
                {
                    var echoRadius = masteryLevel >= 3 ? 4.8f : 3.4f;
                    var echoTargets = masteryLevel >= 3 ? 3 : 2;
                    SendBurstVisual(impactPosition, shotDirection, BurstVisualStyle.Inferno, echoRadius);
                    ApplyAreaBurst(player, impactPosition, primaryTarget, echoRadius, echoTargets, signatureBaseDamage * (masteryLevel >= 3 ? 0.38f : 0.26f), signatureForce * 0.3f, 0.22f, signatureDotDamage * (masteryLevel >= 3 ? 0.55f : 0.35f), signatureDotTickCount, signatureDotTickDelay, signaturePersistentDot, BurstVisualStyle.Inferno);
                    var forwardNovaCenter = GetMasteryOffsetPoint(impactPosition, shotDirection, 0f, 1f, 0.12f);
                    SendBurstVisual(forwardNovaCenter, shotDirection, BurstVisualStyle.Inferno, echoRadius * 0.82f);
                    ApplyAreaBurst(player, forwardNovaCenter, primaryTarget, echoRadius * 0.82f, 1, signatureBaseDamage * 0.18f, signatureForce * 0.15f, 0.2f, signatureDotDamage * 0.24f, signatureDotTickCount, signatureDotTickDelay, signaturePersistentDot, BurstVisualStyle.Inferno);
                    if (masteryLevel >= 3)
                    {
                        var rearNovaCenter = GetMasteryOffsetPoint(impactPosition, shotDirection, 0f, -0.8f, 0.1f);
                        SendBurstVisual(rearNovaCenter, shotDirection, BurstVisualStyle.Inferno, echoRadius * 0.78f);
                        ApplyAreaBurst(player, rearNovaCenter, primaryTarget, echoRadius * 0.78f, 1, signatureBaseDamage * 0.16f, signatureForce * 0.15f, 0.2f, signatureDotDamage * 0.22f, signatureDotTickCount, signatureDotTickDelay, signaturePersistentDot, BurstVisualStyle.Inferno);
                    }
                    break;
                }
                case PlayerMagicStateManager.WandMasteryKeys.Frost:
                {
                    var echoRadius = masteryLevel >= 3 ? 4.4f : 3.2f;
                    var echoTargets = masteryLevel >= 3 ? 3 : 2;
                    SendBurstVisual(impactPosition, shotDirection, BurstVisualStyle.Shatter, echoRadius);
                    ApplyAreaBurst(player, impactPosition, primaryTarget, echoRadius, echoTargets, signatureBaseDamage * (masteryLevel >= 3 ? 0.28f : 0.20f), signatureForce * (masteryLevel >= 3 ? 0.75f : 0.55f), 0.22f, burstVisualStyle: BurstVisualStyle.Shatter);
                    var shatterCenter = GetMasteryOffsetPoint(impactPosition, shotDirection, -0.7f, 0.25f, 0.08f);
                    SendBurstVisual(shatterCenter, shotDirection, BurstVisualStyle.Shatter, echoRadius * 0.82f);
                    ApplyAreaBurst(player, shatterCenter, primaryTarget, echoRadius * 0.82f, 1, signatureBaseDamage * 0.14f, signatureForce * 0.45f, 0.22f, burstVisualStyle: BurstVisualStyle.Shatter);
                    if (masteryLevel >= 3)
                    {
                        var oppositeShatterCenter = GetMasteryOffsetPoint(impactPosition, shotDirection, 0.7f, 0.25f, 0.08f);
                        SendBurstVisual(oppositeShatterCenter, shotDirection, BurstVisualStyle.Shatter, echoRadius * 0.82f);
                        ApplyAreaBurst(player, oppositeShatterCenter, primaryTarget, echoRadius * 0.82f, 1, signatureBaseDamage * 0.14f, signatureForce * 0.45f, 0.22f, burstVisualStyle: BurstVisualStyle.Shatter);
                    }
                    break;
                }
                case PlayerMagicStateManager.WandMasteryKeys.Crystal:
                {
                    var echoRadius = masteryLevel >= 3 ? 6.5f : 5f;
                    var echoTargets = masteryLevel >= 3 ? 2 : 1;
                    SendBurstVisual(impactPosition, shotDirection, BurstVisualStyle.Prism, echoRadius);
                    ApplyAreaBurst(player, impactPosition, primaryTarget, echoRadius, echoTargets, signatureBaseDamage * (masteryLevel >= 3 ? 0.42f : 0.32f), signatureForce * 0.3f, 0.2f, burstVisualStyle: BurstVisualStyle.Prism);
                    var prismLeft = GetMasteryOffsetPoint(impactPosition, shotDirection, -1f, 0.55f, 0.15f);
                    var prismRight = GetMasteryOffsetPoint(impactPosition, shotDirection, 1f, 0.55f, 0.15f);
                    SendBurstVisual(prismLeft, shotDirection, BurstVisualStyle.Prism, echoRadius * 0.72f);
                    SendBurstVisual(prismRight, shotDirection, BurstVisualStyle.Prism, echoRadius * 0.72f);
                    ApplyAreaBurst(player, prismLeft, primaryTarget, echoRadius * 0.72f, 1, signatureBaseDamage * 0.18f, signatureForce * 0.2f, 0.18f, burstVisualStyle: BurstVisualStyle.Prism);
                    ApplyAreaBurst(player, prismRight, primaryTarget, echoRadius * 0.72f, 1, signatureBaseDamage * 0.18f, signatureForce * 0.2f, 0.18f, burstVisualStyle: BurstVisualStyle.Prism);
                    if (masteryLevel >= 3)
                    {
                        var prismForward = GetMasteryOffsetPoint(impactPosition, shotDirection, 0f, 1.35f, 0.2f);
                        SendBurstVisual(prismForward, shotDirection, BurstVisualStyle.Prism, echoRadius * 0.8f);
                        ApplyAreaBurst(player, prismForward, primaryTarget, echoRadius * 0.8f, 1, signatureBaseDamage * 0.24f, signatureForce * 0.24f, 0.18f, burstVisualStyle: BurstVisualStyle.Prism);
                    }
                    break;
                }
                case PlayerMagicStateManager.WandMasteryKeys.Stone:
                {
                    var echoRadius = masteryLevel >= 3 ? 4.6f : 3.6f;
                    var echoTargets = masteryLevel >= 3 ? 3 : 2;
                    SendBurstVisual(impactPosition, shotDirection, BurstVisualStyle.Faultline, echoRadius);
                    ApplyAreaBurst(player, impactPosition, primaryTarget, echoRadius, echoTargets, signatureBaseDamage * (masteryLevel >= 3 ? 0.40f : 0.28f), signatureForce * (masteryLevel >= 3 ? 0.95f : 0.75f), 0.24f, burstVisualStyle: BurstVisualStyle.Faultline);
                    var shockForward = GetMasteryOffsetPoint(impactPosition, shotDirection, 0f, 1f, -0.05f);
                    SendBurstVisual(shockForward, shotDirection, BurstVisualStyle.Faultline, echoRadius * 0.82f);
                    ApplyAreaBurst(player, shockForward, primaryTarget, echoRadius * 0.82f, 1, signatureBaseDamage * 0.18f, signatureForce * 0.65f, 0.22f, burstVisualStyle: BurstVisualStyle.Faultline);
                    if (masteryLevel >= 3)
                    {
                        var shockLeft = GetMasteryOffsetPoint(impactPosition, shotDirection, -1.05f, -0.15f, -0.05f);
                        var shockRight = GetMasteryOffsetPoint(impactPosition, shotDirection, 1.05f, -0.15f, -0.05f);
                        SendBurstVisual(shockLeft, shotDirection, BurstVisualStyle.Faultline, echoRadius * 0.72f);
                        SendBurstVisual(shockRight, shotDirection, BurstVisualStyle.Faultline, echoRadius * 0.72f);
                        ApplyAreaBurst(player, shockLeft, primaryTarget, echoRadius * 0.72f, 1, signatureBaseDamage * 0.14f, signatureForce * 0.55f, 0.22f, burstVisualStyle: BurstVisualStyle.Faultline);
                        ApplyAreaBurst(player, shockRight, primaryTarget, echoRadius * 0.72f, 1, signatureBaseDamage * 0.14f, signatureForce * 0.55f, 0.22f, burstVisualStyle: BurstVisualStyle.Faultline);
                    }
                    break;
                }
                case PlayerMagicStateManager.WandMasteryKeys.Storm:
                {
                    var echoRadius = masteryLevel >= 3 ? 6.2f : 4.6f;
                    var echoTargets = masteryLevel >= 3 ? 4 : 2;
                    SendBurstVisual(impactPosition, shotDirection, BurstVisualStyle.Tempest, echoRadius);
                    ApplyAreaBurst(player, impactPosition, primaryTarget, echoRadius, echoTargets, signatureBaseDamage * (masteryLevel >= 3 ? 0.36f : 0.24f), signatureForce * (masteryLevel >= 3 ? 0.6f : 0.45f), 0.18f, burstVisualStyle: BurstVisualStyle.Tempest);
                    var stormLeft = GetMasteryOffsetPoint(impactPosition, shotDirection, -1.2f, 0.35f, 0.15f);
                    SendBurstVisual(stormLeft, shotDirection, BurstVisualStyle.Tempest, echoRadius * 0.8f);
                    ApplyAreaBurst(player, stormLeft, primaryTarget, echoRadius * 0.8f, 1, signatureBaseDamage * 0.16f, signatureForce * 0.35f, 0.18f, burstVisualStyle: BurstVisualStyle.Tempest);
                    if (masteryLevel >= 3)
                    {
                        var stormRight = GetMasteryOffsetPoint(impactPosition, shotDirection, 1.2f, 0.35f, 0.15f);
                        var stormForward = GetMasteryOffsetPoint(impactPosition, shotDirection, 0f, 1.4f, 0.2f);
                        SendBurstVisual(stormRight, shotDirection, BurstVisualStyle.Tempest, echoRadius * 0.8f);
                        SendBurstVisual(stormForward, shotDirection, BurstVisualStyle.ArcBurst, echoRadius * 0.9f);
                        ApplyAreaBurst(player, stormRight, primaryTarget, echoRadius * 0.8f, 1, signatureBaseDamage * 0.16f, signatureForce * 0.35f, 0.18f, burstVisualStyle: BurstVisualStyle.Tempest);
                        ApplyAreaBurst(player, stormForward, primaryTarget, echoRadius * 0.9f, 1, signatureBaseDamage * 0.18f, signatureForce * 0.4f, 0.18f, burstVisualStyle: BurstVisualStyle.ArcBurst);
                    }
                    break;
                }
                case PlayerMagicStateManager.WandMasteryKeys.Blood:
                {
                    var echoRadius = masteryLevel >= 3 ? 4.8f : 3.6f;
                    var echoTargets = masteryLevel >= 3 ? 2 : 1;
                    SendBurstVisual(impactPosition, shotDirection, BurstVisualStyle.Hemorrhage, echoRadius);
                    ApplyAreaBurst(player, impactPosition, primaryTarget, echoRadius, echoTargets, signatureBaseDamage * (masteryLevel >= 3 ? 0.36f : 0.24f), signatureForce * 0.3f, 0.22f, signatureDotDamage * (masteryLevel >= 3 ? 0.60f : 0.40f), signatureDotTickCount, signatureDotTickDelay, signaturePersistentDot, BurstVisualStyle.Hemorrhage);
                    HealPlayer(player, Mathf.Max(masteryLevel >= 3 ? 16f : 8f, selfHealOnHit * (masteryLevel >= 3 ? 0.55f : 0.35f)));
                    ServerManager.SendParticleTrail(impactPosition + Vector3.up * 0.3f, player.PositionStanding + Vector3.up * 1.05f, 0.2f);
                    if (masteryLevel >= 3)
                    {
                        var siphonLeft = GetMasteryOffsetPoint(impactPosition, shotDirection, -0.9f, 0.25f, 0.05f);
                        var siphonRight = GetMasteryOffsetPoint(impactPosition, shotDirection, 0.9f, 0.25f, 0.05f);
                        SendBurstVisual(siphonLeft, shotDirection, BurstVisualStyle.Hemorrhage, echoRadius * 0.75f);
                        SendBurstVisual(siphonRight, shotDirection, BurstVisualStyle.Hemorrhage, echoRadius * 0.75f);
                        ApplyAreaBurst(player, siphonLeft, primaryTarget, echoRadius * 0.75f, 1, signatureBaseDamage * 0.16f, signatureForce * 0.22f, 0.2f, signatureDotDamage * 0.22f, signatureDotTickCount, signatureDotTickDelay, signaturePersistentDot, BurstVisualStyle.Hemorrhage);
                        ApplyAreaBurst(player, siphonRight, primaryTarget, echoRadius * 0.75f, 1, signatureBaseDamage * 0.16f, signatureForce * 0.22f, 0.2f, signatureDotDamage * 0.22f, signatureDotTickCount, signatureDotTickDelay, signaturePersistentDot, BurstVisualStyle.Hemorrhage);
                    }
                    break;
                }
                case PlayerMagicStateManager.WandMasteryKeys.Void:
                {
                    var echoRadius = masteryLevel >= 3 ? 5.8f : 4.4f;
                    var echoTargets = masteryLevel >= 3 ? 3 : 2;
                    SendBurstVisual(impactPosition, shotDirection, BurstVisualStyle.Rupture, echoRadius);
                    ApplyAreaBurst(player, impactPosition, primaryTarget, echoRadius, echoTargets, signatureBaseDamage * (masteryLevel >= 3 ? 0.40f : 0.26f), signatureForce * (masteryLevel >= 3 ? 0.6f : 0.4f), 0.26f, signatureDotDamage * (masteryLevel >= 3 ? 0.55f : 0.35f), signatureDotTickCount, signatureDotTickDelay, signaturePersistentDot, BurstVisualStyle.Rupture);
                    var collapseRear = GetMasteryOffsetPoint(impactPosition, shotDirection, 0f, -0.95f, 0.1f);
                    SendBurstVisual(collapseRear, shotDirection, BurstVisualStyle.Rupture, echoRadius * 0.78f);
                    ApplyAreaBurst(player, collapseRear, primaryTarget, echoRadius * 0.78f, 1, signatureBaseDamage * 0.18f, signatureForce * 0.3f, 0.24f, signatureDotDamage * 0.22f, signatureDotTickCount, signatureDotTickDelay, signaturePersistentDot, BurstVisualStyle.Rupture);
                    if (masteryLevel >= 3)
                    {
                        var collapseLeft = GetMasteryOffsetPoint(impactPosition, shotDirection, -1.1f, 0.2f, 0.1f);
                        var collapseRight = GetMasteryOffsetPoint(impactPosition, shotDirection, 1.1f, 0.2f, 0.1f);
                        SendBurstVisual(collapseLeft, shotDirection, BurstVisualStyle.Rupture, echoRadius * 0.7f);
                        SendBurstVisual(collapseRight, shotDirection, BurstVisualStyle.Rupture, echoRadius * 0.7f);
                        ApplyAreaBurst(player, collapseLeft, primaryTarget, echoRadius * 0.7f, 1, signatureBaseDamage * 0.14f, signatureForce * 0.24f, 0.24f, signatureDotDamage * 0.2f, signatureDotTickCount, signatureDotTickDelay, signaturePersistentDot, BurstVisualStyle.Rupture);
                        ApplyAreaBurst(player, collapseRight, primaryTarget, echoRadius * 0.7f, 1, signatureBaseDamage * 0.14f, signatureForce * 0.24f, 0.24f, signatureDotDamage * 0.2f, signatureDotTickCount, signatureDotTickDelay, signaturePersistentDot, BurstVisualStyle.Rupture);
                    }
                    break;
                }
            }

            if (legendaryUnlocked)
            {
                ApplyLegendarySignature(
                    player,
                    masteryKey,
                    primaryTarget,
                    impactPosition,
                    shotDirection,
                    signatureBaseDamage,
                    signatureForce,
                    signatureDotDamage,
                    signatureDotTickCount,
                    signatureDotTickDelay,
                    signaturePersistentDot,
                    burstRadius,
                    selfHealOnHit);
            }
        }

        private static void ApplyLegendarySignature(
            Players.Player player,
            string masteryKey,
            IMonster primaryTarget,
            Vector3 impactPosition,
            Vector3 shotDirection,
            float signatureBaseDamage,
            float signatureForce,
            float signatureDotDamage,
            int signatureDotTickCount,
            float signatureDotTickDelay,
            bool signaturePersistentDot,
            float burstRadius,
            float selfHealOnHit)
        {
            if (player == null || primaryTarget == null || !primaryTarget.IsValid || string.IsNullOrWhiteSpace(masteryKey))
                return;

            var legendaryRadius = Mathf.Max(3.2f, burstRadius + 0.8f);
            var manaItemIndex = Mana.Item != null ? Mana.Item.ItemIndex : (ushort)0;

            switch (masteryKey)
            {
                case PlayerMagicStateManager.WandMasteryKeys.Mana:
                {
                    PlayerMagicStateManager.RestoreMana(player, 1, manaItemIndex);
                    var leftPulse = GetMasteryOffsetPoint(impactPosition, shotDirection, -1.25f, 0.55f, 0.12f);
                    var rightPulse = GetMasteryOffsetPoint(impactPosition, shotDirection, 1.25f, 0.55f, 0.12f);
                    SendBurstVisual(leftPulse, shotDirection, BurstVisualStyle.ManaPulse, legendaryRadius * 0.78f);
                    SendBurstVisual(rightPulse, shotDirection, BurstVisualStyle.ManaPulse, legendaryRadius * 0.78f);
                    ApplyAreaBurst(player, leftPulse, primaryTarget, legendaryRadius * 0.78f, 1, signatureBaseDamage * 0.22f, signatureForce * 0.32f, 0.18f, burstVisualStyle: BurstVisualStyle.ManaPulse);
                    ApplyAreaBurst(player, rightPulse, primaryTarget, legendaryRadius * 0.78f, 1, signatureBaseDamage * 0.22f, signatureForce * 0.32f, 0.18f, burstVisualStyle: BurstVisualStyle.ManaPulse);
                    break;
                }
                case PlayerMagicStateManager.WandMasteryKeys.Briar:
                {
                    var thornFront = GetMasteryOffsetPoint(impactPosition, shotDirection, 0f, 1.45f, 0.08f);
                    var thornRear = GetMasteryOffsetPoint(impactPosition, shotDirection, 0f, -1.1f, 0.05f);
                    SendBurstVisual(thornFront, shotDirection, BurstVisualStyle.Bramble, legendaryRadius * 0.92f);
                    SendBurstVisual(thornRear, shotDirection, BurstVisualStyle.Bramble, legendaryRadius * 0.78f);
                    ApplyAreaBurst(player, thornFront, primaryTarget, legendaryRadius * 0.92f, 2, signatureBaseDamage * 0.20f, signatureForce * 0.24f, 0.24f, signatureDotDamage * 0.30f, signatureDotTickCount, signatureDotTickDelay, signaturePersistentDot, BurstVisualStyle.Bramble);
                    ApplyAreaBurst(player, thornRear, primaryTarget, legendaryRadius * 0.78f, 1, signatureBaseDamage * 0.16f, signatureForce * 0.18f, 0.24f, signatureDotDamage * 0.24f, signatureDotTickCount, signatureDotTickDelay, signaturePersistentDot, BurstVisualStyle.Bramble);
                    break;
                }
                case PlayerMagicStateManager.WandMasteryKeys.Spark:
                {
                    var crossLeft = GetMasteryOffsetPoint(impactPosition, shotDirection, -1.45f, 0.1f, 0.12f);
                    var crossRight = GetMasteryOffsetPoint(impactPosition, shotDirection, 1.45f, 0.1f, 0.12f);
                    var crossRear = GetMasteryOffsetPoint(impactPosition, shotDirection, 0f, -1.15f, 0.08f);
                    SendBurstVisual(crossLeft, shotDirection, BurstVisualStyle.SparkArc, legendaryRadius * 0.86f);
                    SendBurstVisual(crossRight, shotDirection, BurstVisualStyle.SparkArc, legendaryRadius * 0.86f);
                    SendBurstVisual(crossRear, shotDirection, BurstVisualStyle.ArcBurst, legendaryRadius * 0.92f);
                    ApplyAreaBurst(player, crossLeft, primaryTarget, legendaryRadius * 0.86f, 1, signatureBaseDamage * 0.18f, signatureForce * 0.4f, 0.16f, burstVisualStyle: BurstVisualStyle.SparkArc);
                    ApplyAreaBurst(player, crossRight, primaryTarget, legendaryRadius * 0.86f, 1, signatureBaseDamage * 0.18f, signatureForce * 0.4f, 0.16f, burstVisualStyle: BurstVisualStyle.SparkArc);
                    ApplyAreaBurst(player, crossRear, primaryTarget, legendaryRadius * 0.92f, 1, signatureBaseDamage * 0.22f, signatureForce * 0.44f, 0.16f, burstVisualStyle: BurstVisualStyle.ArcBurst);
                    break;
                }
                case PlayerMagicStateManager.WandMasteryKeys.Venom:
                {
                    var frontBloom = GetMasteryOffsetPoint(impactPosition, shotDirection, 0f, 1.3f, 0.05f);
                    var leftBloom = GetMasteryOffsetPoint(impactPosition, shotDirection, -1.05f, 0.15f, 0.05f);
                    var rightBloom = GetMasteryOffsetPoint(impactPosition, shotDirection, 1.05f, 0.15f, 0.05f);
                    SendBurstVisual(frontBloom, shotDirection, BurstVisualStyle.ToxicBloom, legendaryRadius * 0.88f);
                    SendBurstVisual(leftBloom, shotDirection, BurstVisualStyle.ToxicBloom, legendaryRadius * 0.78f);
                    SendBurstVisual(rightBloom, shotDirection, BurstVisualStyle.ToxicBloom, legendaryRadius * 0.78f);
                    ApplyAreaBurst(player, frontBloom, primaryTarget, legendaryRadius * 0.88f, 2, signatureBaseDamage * 0.14f, signatureForce * 0.12f, 0.24f, signatureDotDamage * 0.34f, signatureDotTickCount, signatureDotTickDelay, signaturePersistentDot, BurstVisualStyle.ToxicBloom);
                    ApplyAreaBurst(player, leftBloom, primaryTarget, legendaryRadius * 0.78f, 1, signatureBaseDamage * 0.1f, signatureForce * 0.1f, 0.24f, signatureDotDamage * 0.28f, signatureDotTickCount, signatureDotTickDelay, signaturePersistentDot, BurstVisualStyle.ToxicBloom);
                    ApplyAreaBurst(player, rightBloom, primaryTarget, legendaryRadius * 0.78f, 1, signatureBaseDamage * 0.1f, signatureForce * 0.1f, 0.24f, signatureDotDamage * 0.28f, signatureDotTickCount, signatureDotTickDelay, signaturePersistentDot, BurstVisualStyle.ToxicBloom);
                    break;
                }
                case PlayerMagicStateManager.WandMasteryKeys.Ember:
                {
                    var frontNova = GetMasteryOffsetPoint(impactPosition, shotDirection, 0f, 1.3f, 0.12f);
                    var leftNova = GetMasteryOffsetPoint(impactPosition, shotDirection, -1.1f, -0.15f, 0.12f);
                    var rightNova = GetMasteryOffsetPoint(impactPosition, shotDirection, 1.1f, -0.15f, 0.12f);
                    SendBurstVisual(frontNova, shotDirection, BurstVisualStyle.Inferno, legendaryRadius);
                    SendBurstVisual(leftNova, shotDirection, BurstVisualStyle.Inferno, legendaryRadius * 0.82f);
                    SendBurstVisual(rightNova, shotDirection, BurstVisualStyle.Inferno, legendaryRadius * 0.82f);
                    ApplyAreaBurst(player, frontNova, primaryTarget, legendaryRadius, 2, signatureBaseDamage * 0.20f, signatureForce * 0.18f, 0.22f, signatureDotDamage * 0.28f, signatureDotTickCount, signatureDotTickDelay, signaturePersistentDot, BurstVisualStyle.Inferno);
                    ApplyAreaBurst(player, leftNova, primaryTarget, legendaryRadius * 0.82f, 1, signatureBaseDamage * 0.12f, signatureForce * 0.15f, 0.2f, signatureDotDamage * 0.22f, signatureDotTickCount, signatureDotTickDelay, signaturePersistentDot, BurstVisualStyle.Inferno);
                    ApplyAreaBurst(player, rightNova, primaryTarget, legendaryRadius * 0.82f, 1, signatureBaseDamage * 0.12f, signatureForce * 0.15f, 0.2f, signatureDotDamage * 0.22f, signatureDotTickCount, signatureDotTickDelay, signaturePersistentDot, BurstVisualStyle.Inferno);
                    break;
                }
                case PlayerMagicStateManager.WandMasteryKeys.Frost:
                {
                    var splitLeft = GetMasteryOffsetPoint(impactPosition, shotDirection, -1.2f, 0.4f, 0.1f);
                    var splitRight = GetMasteryOffsetPoint(impactPosition, shotDirection, 1.2f, 0.4f, 0.1f);
                    var splitForward = GetMasteryOffsetPoint(impactPosition, shotDirection, 0f, 1.4f, 0.12f);
                    SendBurstVisual(splitLeft, shotDirection, BurstVisualStyle.Shatter, legendaryRadius * 0.78f);
                    SendBurstVisual(splitRight, shotDirection, BurstVisualStyle.Shatter, legendaryRadius * 0.78f);
                    SendBurstVisual(splitForward, shotDirection, BurstVisualStyle.Shatter, legendaryRadius * 0.92f);
                    ApplyAreaBurst(player, splitLeft, primaryTarget, legendaryRadius * 0.78f, 1, signatureBaseDamage * 0.12f, signatureForce * 0.62f, 0.2f, burstVisualStyle: BurstVisualStyle.Shatter);
                    ApplyAreaBurst(player, splitRight, primaryTarget, legendaryRadius * 0.78f, 1, signatureBaseDamage * 0.12f, signatureForce * 0.62f, 0.2f, burstVisualStyle: BurstVisualStyle.Shatter);
                    ApplyAreaBurst(player, splitForward, primaryTarget, legendaryRadius * 0.92f, 1, signatureBaseDamage * 0.18f, signatureForce * 0.7f, 0.2f, burstVisualStyle: BurstVisualStyle.Shatter);
                    break;
                }
                case PlayerMagicStateManager.WandMasteryKeys.Crystal:
                {
                    var prismFront = GetMasteryOffsetPoint(impactPosition, shotDirection, 0f, 1.55f, 0.2f);
                    var prismRear = GetMasteryOffsetPoint(impactPosition, shotDirection, 0f, -1.25f, 0.15f);
                    var prismLeft = GetMasteryOffsetPoint(impactPosition, shotDirection, -1.3f, 0.25f, 0.15f);
                    var prismRight = GetMasteryOffsetPoint(impactPosition, shotDirection, 1.3f, 0.25f, 0.15f);
                    SendBurstVisual(prismFront, shotDirection, BurstVisualStyle.Prism, legendaryRadius * 0.94f);
                    SendBurstVisual(prismRear, shotDirection, BurstVisualStyle.Prism, legendaryRadius * 0.72f);
                    SendBurstVisual(prismLeft, shotDirection, BurstVisualStyle.Prism, legendaryRadius * 0.72f);
                    SendBurstVisual(prismRight, shotDirection, BurstVisualStyle.Prism, legendaryRadius * 0.72f);
                    ApplyAreaBurst(player, prismFront, primaryTarget, legendaryRadius * 0.94f, 2, signatureBaseDamage * 0.18f, signatureForce * 0.2f, 0.18f, burstVisualStyle: BurstVisualStyle.Prism);
                    ApplyAreaBurst(player, prismRear, primaryTarget, legendaryRadius * 0.72f, 1, signatureBaseDamage * 0.12f, signatureForce * 0.16f, 0.18f, burstVisualStyle: BurstVisualStyle.Prism);
                    ApplyAreaBurst(player, prismLeft, primaryTarget, legendaryRadius * 0.72f, 1, signatureBaseDamage * 0.12f, signatureForce * 0.16f, 0.18f, burstVisualStyle: BurstVisualStyle.Prism);
                    ApplyAreaBurst(player, prismRight, primaryTarget, legendaryRadius * 0.72f, 1, signatureBaseDamage * 0.12f, signatureForce * 0.16f, 0.18f, burstVisualStyle: BurstVisualStyle.Prism);
                    break;
                }
                case PlayerMagicStateManager.WandMasteryKeys.Stone:
                {
                    var breakerFront = GetMasteryOffsetPoint(impactPosition, shotDirection, 0f, 1.35f, -0.05f);
                    var breakerBack = GetMasteryOffsetPoint(impactPosition, shotDirection, 0f, -1.15f, -0.05f);
                    var breakerLeft = GetMasteryOffsetPoint(impactPosition, shotDirection, -1.25f, 0.15f, -0.05f);
                    var breakerRight = GetMasteryOffsetPoint(impactPosition, shotDirection, 1.25f, 0.15f, -0.05f);
                    SendBurstVisual(breakerFront, shotDirection, BurstVisualStyle.Faultline, legendaryRadius);
                    SendBurstVisual(breakerBack, shotDirection, BurstVisualStyle.Faultline, legendaryRadius * 0.82f);
                    SendBurstVisual(breakerLeft, shotDirection, BurstVisualStyle.Faultline, legendaryRadius * 0.78f);
                    SendBurstVisual(breakerRight, shotDirection, BurstVisualStyle.Faultline, legendaryRadius * 0.78f);
                    ApplyAreaBurst(player, breakerFront, primaryTarget, legendaryRadius, 2, signatureBaseDamage * 0.2f, signatureForce * 0.78f, 0.22f, burstVisualStyle: BurstVisualStyle.Faultline);
                    ApplyAreaBurst(player, breakerBack, primaryTarget, legendaryRadius * 0.82f, 1, signatureBaseDamage * 0.14f, signatureForce * 0.68f, 0.22f, burstVisualStyle: BurstVisualStyle.Faultline);
                    ApplyAreaBurst(player, breakerLeft, primaryTarget, legendaryRadius * 0.78f, 1, signatureBaseDamage * 0.12f, signatureForce * 0.62f, 0.22f, burstVisualStyle: BurstVisualStyle.Faultline);
                    ApplyAreaBurst(player, breakerRight, primaryTarget, legendaryRadius * 0.78f, 1, signatureBaseDamage * 0.12f, signatureForce * 0.62f, 0.22f, burstVisualStyle: BurstVisualStyle.Faultline);
                    break;
                }
                case PlayerMagicStateManager.WandMasteryKeys.Storm:
                {
                    var skyLeft = GetMasteryOffsetPoint(impactPosition, shotDirection, -1.35f, 0.55f, 0.2f);
                    var skyRight = GetMasteryOffsetPoint(impactPosition, shotDirection, 1.35f, 0.55f, 0.2f);
                    var skyRear = GetMasteryOffsetPoint(impactPosition, shotDirection, 0f, -1.1f, 0.18f);
                    SendBurstVisual(skyLeft, shotDirection, BurstVisualStyle.Tempest, legendaryRadius);
                    SendBurstVisual(skyRight, shotDirection, BurstVisualStyle.Tempest, legendaryRadius);
                    SendBurstVisual(skyRear, shotDirection, BurstVisualStyle.ArcBurst, legendaryRadius * 0.92f);
                    ApplyAreaBurst(player, skyLeft, primaryTarget, legendaryRadius, 2, signatureBaseDamage * 0.16f, signatureForce * 0.34f, 0.18f, burstVisualStyle: BurstVisualStyle.Tempest);
                    ApplyAreaBurst(player, skyRight, primaryTarget, legendaryRadius, 2, signatureBaseDamage * 0.16f, signatureForce * 0.34f, 0.18f, burstVisualStyle: BurstVisualStyle.Tempest);
                    ApplyAreaBurst(player, skyRear, primaryTarget, legendaryRadius * 0.92f, 1, signatureBaseDamage * 0.18f, signatureForce * 0.38f, 0.18f, burstVisualStyle: BurstVisualStyle.ArcBurst);
                    break;
                }
                case PlayerMagicStateManager.WandMasteryKeys.Blood:
                {
                    var siphonFront = GetMasteryOffsetPoint(impactPosition, shotDirection, 0f, 1.2f, 0.08f);
                    var siphonLeft = GetMasteryOffsetPoint(impactPosition, shotDirection, -1f, 0.2f, 0.05f);
                    var siphonRight = GetMasteryOffsetPoint(impactPosition, shotDirection, 1f, 0.2f, 0.05f);
                    SendBurstVisual(siphonFront, shotDirection, BurstVisualStyle.Hemorrhage, legendaryRadius * 0.94f);
                    SendBurstVisual(siphonLeft, shotDirection, BurstVisualStyle.Hemorrhage, legendaryRadius * 0.72f);
                    SendBurstVisual(siphonRight, shotDirection, BurstVisualStyle.Hemorrhage, legendaryRadius * 0.72f);
                    ApplyAreaBurst(player, siphonFront, primaryTarget, legendaryRadius * 0.94f, 2, signatureBaseDamage * 0.18f, signatureForce * 0.2f, 0.22f, signatureDotDamage * 0.26f, signatureDotTickCount, signatureDotTickDelay, signaturePersistentDot, BurstVisualStyle.Hemorrhage);
                    ApplyAreaBurst(player, siphonLeft, primaryTarget, legendaryRadius * 0.72f, 1, signatureBaseDamage * 0.1f, signatureForce * 0.16f, 0.2f, signatureDotDamage * 0.2f, signatureDotTickCount, signatureDotTickDelay, signaturePersistentDot, BurstVisualStyle.Hemorrhage);
                    ApplyAreaBurst(player, siphonRight, primaryTarget, legendaryRadius * 0.72f, 1, signatureBaseDamage * 0.1f, signatureForce * 0.16f, 0.2f, signatureDotDamage * 0.2f, signatureDotTickCount, signatureDotTickDelay, signaturePersistentDot, BurstVisualStyle.Hemorrhage);
                    HealPlayer(player, Mathf.Max(12f, selfHealOnHit * 0.35f));
                    ServerManager.SendParticleTrail(impactPosition + Vector3.up * 0.35f, player.PositionStanding + Vector3.up * 1.2f, 0.22f);
                    break;
                }
                case PlayerMagicStateManager.WandMasteryKeys.Void:
                {
                    var eclipseFront = GetMasteryOffsetPoint(impactPosition, shotDirection, 0f, 1.35f, 0.15f);
                    var eclipseBack = GetMasteryOffsetPoint(impactPosition, shotDirection, 0f, -1.2f, 0.1f);
                    var eclipseLeft = GetMasteryOffsetPoint(impactPosition, shotDirection, -1.3f, 0.2f, 0.1f);
                    var eclipseRight = GetMasteryOffsetPoint(impactPosition, shotDirection, 1.3f, 0.2f, 0.1f);
                    SendBurstVisual(eclipseFront, shotDirection, BurstVisualStyle.Rupture, legendaryRadius);
                    SendBurstVisual(eclipseBack, shotDirection, BurstVisualStyle.Rupture, legendaryRadius * 0.82f);
                    SendBurstVisual(eclipseLeft, shotDirection, BurstVisualStyle.Rupture, legendaryRadius * 0.78f);
                    SendBurstVisual(eclipseRight, shotDirection, BurstVisualStyle.Rupture, legendaryRadius * 0.78f);
                    ApplyAreaBurst(player, eclipseFront, primaryTarget, legendaryRadius, 2, signatureBaseDamage * 0.18f, signatureForce * 0.26f, 0.24f, signatureDotDamage * 0.24f, signatureDotTickCount, signatureDotTickDelay, signaturePersistentDot, BurstVisualStyle.Rupture);
                    ApplyAreaBurst(player, eclipseBack, primaryTarget, legendaryRadius * 0.82f, 1, signatureBaseDamage * 0.12f, signatureForce * 0.22f, 0.24f, signatureDotDamage * 0.18f, signatureDotTickCount, signatureDotTickDelay, signaturePersistentDot, BurstVisualStyle.Rupture);
                    ApplyAreaBurst(player, eclipseLeft, primaryTarget, legendaryRadius * 0.78f, 1, signatureBaseDamage * 0.1f, signatureForce * 0.18f, 0.24f, signatureDotDamage * 0.16f, signatureDotTickCount, signatureDotTickDelay, signaturePersistentDot, BurstVisualStyle.Rupture);
                    ApplyAreaBurst(player, eclipseRight, primaryTarget, legendaryRadius * 0.78f, 1, signatureBaseDamage * 0.1f, signatureForce * 0.18f, 0.24f, signatureDotDamage * 0.16f, signatureDotTickCount, signatureDotTickDelay, signaturePersistentDot, BurstVisualStyle.Rupture);
                    break;
                }
            }
        }

        private static int ApplyLineageInheritedEffects(
            Players.Player player,
            string masteryKey,
            IMonster primaryTarget,
            Vector3 impactPosition,
            Vector3 shotDirection,
            float baseDamage,
            float projectileForce,
            float burstRadius,
            float burstDamage,
            float burstProjectileForce)
        {
            if (player == null || primaryTarget == null || !primaryTarget.IsValid || string.IsNullOrWhiteSpace(masteryKey))
                return 0;

            if (!PlayerMagicStateManager.TryGetWandLineageCombatProfile(player, masteryKey, out var profile))
                return 0;

            var tier = Mathf.Max(1, profile.Tier);
            var mastery = Mathf.Max(0, profile.MasteryLevel);
            var effectCountBonus = Mathf.Min(0.75f, profile.Effects.Count * 0.065f);
            var scale = 1f + (tier - 1) * 0.30f + mastery * 0.075f + effectCountBonus;
            if (profile.RitualAwakened || profile.HasEffect(PlayerMagicStateManager.WandLineageEffects.Ascended))
                scale *= 1.38f;

            var applications = 0;
            var signatureDamage = Mathf.Max(baseDamage, burstDamage);
            var signatureForce = Mathf.Max(projectileForce, burstProjectileForce);
            var baseRadius = Mathf.Max(2.4f, burstRadius);

            if (profile.HasEffect(PlayerMagicStateManager.WandLineageEffects.Poison))
            {
                ApplyMonsterHit(player, primaryTarget, Vector3.zero, signatureDamage * 0.06f, 5.5f * scale, 3 + tier / 2, 0.75f, true, BurstVisualStyle.ToxicBloom);
                applications++;
            }

            if (profile.HasEffect(PlayerMagicStateManager.WandLineageEffects.PoisonSpread))
            {
                var radius = baseRadius + 1.2f + tier * 0.45f;
                var targets = 1 + tier / 2 + (profile.RitualAwakened ? 2 : 0);
                applications += ApplyAreaBurst(player, impactPosition, primaryTarget, radius, targets, signatureDamage * 0.18f * scale, signatureForce * 0.28f, 0.22f, 3.7f * scale, 3 + tier / 2, 0.70f, true, BurstVisualStyle.ToxicBloom);
            }

            if (profile.HasEffect(PlayerMagicStateManager.WandLineageEffects.Burn))
            {
                ApplyMonsterHit(player, primaryTarget, shotDirection * (signatureForce * 0.15f), signatureDamage * 0.10f * scale, 4.6f * scale, 3 + tier / 2, 0.65f, true, BurstVisualStyle.Inferno);
                applications++;
            }

            if (profile.HasEffect(PlayerMagicStateManager.WandLineageEffects.Explosion))
            {
                var radius = baseRadius + 1.0f + tier * 0.55f;
                var targets = 1 + tier / 2 + (profile.RitualAwakened ? 2 : 0);
                applications += ApplyAreaBurst(player, impactPosition, primaryTarget, radius, targets, signatureDamage * 0.34f * scale, signatureForce * 0.55f, 0.24f, 2.8f * scale, 2 + tier / 3, 0.65f, true, BurstVisualStyle.Inferno);
                SendBurstVisual(impactPosition, shotDirection, BurstVisualStyle.Inferno, radius);
            }

            if (profile.HasEffect(PlayerMagicStateManager.WandLineageEffects.Freeze))
            {
                ApplyMonsterHit(player, primaryTarget, shotDirection * (signatureForce * 0.50f), signatureDamage * 0.12f * scale, 0f, 0, 0f, false, BurstVisualStyle.Shatter);
                SendBurstVisual(impactPosition, shotDirection, BurstVisualStyle.Shatter, Mathf.Max(2.4f, baseRadius * 0.85f));
                applications++;
            }

            if (profile.HasEffect(PlayerMagicStateManager.WandLineageEffects.Shatter))
            {
                var radius = baseRadius + 0.9f + tier * 0.35f;
                var targets = 1 + tier / 3 + (profile.RitualAwakened ? 1 : 0);
                applications += ApplyAreaBurst(player, impactPosition, primaryTarget, radius, targets, signatureDamage * 0.24f * scale, signatureForce * 0.68f, 0.23f, burstVisualStyle: BurstVisualStyle.Shatter);
            }

            if (profile.HasEffect(PlayerMagicStateManager.WandLineageEffects.Stagger))
            {
                ApplyMonsterHit(player, primaryTarget, shotDirection * (signatureForce * (0.75f + tier * 0.08f)), signatureDamage * 0.18f * scale, 0f, 0, 0f, false, BurstVisualStyle.Faultline);
                applications++;
            }

            if (profile.HasEffect(PlayerMagicStateManager.WandLineageEffects.ArmorBreak))
            {
                ApplyMonsterHit(player, primaryTarget, Vector3.up * (signatureForce * 0.35f), signatureDamage * 0.26f * scale, 0f, 0, 0f, false, BurstVisualStyle.Faultline);
                PlayerMagicStateManager.RecordWandMasteryEvent(player, masteryKey, 0, "OnArmorBreak", 1);
                applications++;
            }

            if (profile.HasEffect(PlayerMagicStateManager.WandLineageEffects.Quake))
            {
                var radius = baseRadius + 1.5f + tier * 0.45f;
                var targets = 2 + tier / 2 + (profile.RitualAwakened ? 2 : 0);
                applications += ApplyAreaBurst(player, impactPosition, primaryTarget, radius, targets, signatureDamage * 0.24f * scale, signatureForce * 0.85f, 0.22f, burstVisualStyle: BurstVisualStyle.Faultline);
                SendBurstVisual(impactPosition, Vector3.up, BurstVisualStyle.Faultline, radius);
            }

            if (profile.HasEffect(PlayerMagicStateManager.WandLineageEffects.Chain))
            {
                var radius = baseRadius + 2.2f + tier * 0.65f;
                var targets = 2 + tier / 2 + (profile.RitualAwakened ? 3 : 0);
                applications += ApplyAreaBurst(player, impactPosition, primaryTarget, radius, targets, signatureDamage * 0.30f * scale, signatureForce * 0.42f, 0.20f, burstVisualStyle: tier >= 5 ? BurstVisualStyle.Tempest : BurstVisualStyle.ArcBurst);
            }

            if (profile.HasEffect(PlayerMagicStateManager.WandLineageEffects.Bleed))
            {
                ApplyMonsterHit(player, primaryTarget, Vector3.zero, signatureDamage * 0.10f * scale, 6.0f * scale, 4 + tier / 2, 0.70f, true, BurstVisualStyle.Hemorrhage);
                applications++;
            }

            if (profile.HasEffect(PlayerMagicStateManager.WandLineageEffects.Bramble))
            {
                var radius = baseRadius + 1.1f + tier * 0.35f;
                applications += ApplyAreaBurst(player, impactPosition, primaryTarget, radius, 1 + tier / 3, signatureDamage * 0.20f * scale, signatureForce * 0.40f, 0.23f, 3.4f * scale, 3, 0.75f, true, BurstVisualStyle.Bramble);
            }

            if (profile.HasEffect(PlayerMagicStateManager.WandLineageEffects.Rupture))
            {
                var radius = baseRadius + 2.0f + tier * 0.55f;
                applications += ApplyAreaBurst(player, impactPosition, primaryTarget, radius, 2 + tier / 2, signatureDamage * 0.32f * scale, signatureForce * 0.48f, 0.22f, 4.2f * scale, 3, 0.62f, true, BurstVisualStyle.Rupture);
                SendBurstVisual(impactPosition, shotDirection, BurstVisualStyle.Rupture, radius);
            }

            if (profile.HasEffect(PlayerMagicStateManager.WandLineageEffects.Prism))
            {
                var radius = baseRadius + 1.6f + tier * 0.45f;
                applications += ApplyAreaBurst(player, impactPosition, primaryTarget, radius, 2 + tier / 3, signatureDamage * 0.28f * scale, signatureForce * 0.36f, 0.20f, burstVisualStyle: BurstVisualStyle.Prism);
                SendBurstVisual(impactPosition + Vector3.up * 0.15f, shotDirection, BurstVisualStyle.Prism, radius);
            }

            if (profile.RitualAwakened || profile.HasEffect(PlayerMagicStateManager.WandLineageEffects.Ascended))
            {
                var radius = baseRadius + 2.8f + tier * 0.7f;
                var targets = 3 + tier + Mathf.Min(4, profile.Effects.Count / 2);
                applications += ApplyAreaBurst(player, impactPosition + Vector3.up * 0.2f, primaryTarget, radius, targets, signatureDamage * 0.22f * scale, signatureForce * 0.50f, 0.18f, 2.8f * scale, 2 + tier / 3, 0.58f, true, BurstVisualStyle.AetherWave);
                SendBurstVisual(impactPosition + Vector3.up * 0.2f, shotDirection, BurstVisualStyle.AetherWave, radius);
            }

            return applications;
        }

        private static Vector3 GetMasteryOffsetPoint(Vector3 center, Vector3 shotDirection, float rightOffset, float forwardOffset, float upOffset = 0f)
        {
            GetVisualBasis(shotDirection, out _, out var right, out var forwardFlat);
            return center + right * rightOffset + forwardFlat * forwardOffset + Vector3.up * upOffset;
        }

        private static void SendImpactFeedback(Vector3 position, ushort indicatorItemIndex, string hitAudio)
        {
            if (!string.IsNullOrEmpty(hitAudio))
                AudioManager.SendAudio(position, hitAudio);
        }

        private static void GetVisualBasis(Vector3 shotDirection, out Vector3 forward, out Vector3 right, out Vector3 forwardFlat)
        {
            forward = shotDirection.sqrMagnitude < 0.0001f ? Vector3.forward : shotDirection.normalized;
            right = Vector3.Cross(forward, Vector3.up);
            if (right.sqrMagnitude < 0.0001f)
                right = Vector3.right;
            else
                right.Normalize();

            forwardFlat = Vector3.Cross(Vector3.up, right);
            if (forwardFlat.sqrMagnitude < 0.0001f)
                forwardFlat = forward;
            else
                forwardFlat.Normalize();
        }

        private static bool IsLightningStyle(BurstVisualStyle style)
        {
            return style == BurstVisualStyle.SparkArc ||
                   style == BurstVisualStyle.ArcBurst ||
                   style == BurstVisualStyle.Tempest;
        }

        
        private static void SendLightningTrail(Vector3 start, Vector3 end, float duration, float amplitudeScale = 1f, int segments = 5, bool addVerticalFork = false)
        {
            var toEnd = end - start;
            var distance = toEnd.magnitude;
            if (distance < 0.15f)
            {
                ServerManager.SendParticleTrail(start, end, duration);
                return;
            }

            var direction = toEnd / distance;
            GetVisualBasis(direction, out _, out var right, out _);
            var upish = Vector3.Cross(right, direction);
            if (upish.sqrMagnitude < 0.0001f)
                upish = Vector3.up;
            else
                upish.Normalize();

            var amplitude = Mathf.Max(0.18f, distance * 0.08f * amplitudeScale);
            var previous = start;
            var segmentCount = Mathf.Max(3, segments);

            for (var i = 1; i < segmentCount; i++)
            {
                var t = i / (float)segmentCount;
                var point = Vector3.Lerp(start, end, t);
                var lateralSign = (i % 2 == 0) ? 1f : -1f;
                var lateral = lateralSign * amplitude * Pipliz.Random.NextFloat(0.55f, 1f);
                var vertical = amplitude * Pipliz.Random.NextFloat(-0.3f, 0.45f);
                point += right * lateral + upish * vertical;
                ServerManager.SendParticleTrail(previous, point, duration);
                previous = point;
            }

            ServerManager.SendParticleTrail(previous, end, duration);

            if (!addVerticalFork)
                return;

            var forkOrigin = Vector3.Lerp(start, end, 0.55f);
            var forkTarget = forkOrigin + upish * (amplitude * 2.4f);
            ServerManager.SendParticleTrail(forkOrigin, forkTarget, Mathf.Max(0.1f, duration * 0.85f));
        }

        private static void SendOrbitCollapse(Vector3 center, Vector3 shotDirection, float radius, float height, float duration, int count, bool crossLinks = true)
        {
            GetVisualBasis(shotDirection, out _, out var right, out var forwardFlat);

            var core = center + Vector3.up * 0.18f;
            var orbitCount = Mathf.Max(3, count);
            var firstOuter = Vector3.zero;
            var previousOuter = Vector3.zero;

            for (var i = 0; i < orbitCount; i++)
            {
                var t = i / (float)orbitCount;
                var angle = t * Mathf.PI * 2f;
                var nextAngle = angle + Mathf.PI * 0.55f;
                var outer = center + (right * Mathf.Cos(angle) + forwardFlat * Mathf.Sin(angle)) * radius + Vector3.up * height;
                var inner = center + (right * Mathf.Cos(nextAngle) + forwardFlat * Mathf.Sin(nextAngle)) * (radius * 0.42f) + Vector3.up * (height * 0.55f);

                ServerManager.SendParticleTrail(outer, inner, duration);
                ServerManager.SendParticleTrail(inner, core, Mathf.Max(0.1f, duration * 0.92f));

                if (crossLinks)
                {
                    if (i == 0)
                        firstOuter = outer;
                    else
                        ServerManager.SendParticleTrail(previousOuter, outer, Mathf.Max(0.1f, duration * 0.82f));
                }

                previousOuter = outer;
            }

            if (crossLinks && orbitCount > 2)
                ServerManager.SendParticleTrail(previousOuter, firstOuter, Mathf.Max(0.1f, duration * 0.82f));
        }

        private static void SendHelixTrail(Vector3 origin, Vector3 end, Vector3 shotDirection, float radius, float duration, int steps, float verticalAmplitude = 0.2f)
        {
            var toEnd = end - origin;
            var distance = toEnd.magnitude;
            if (distance < 0.35f)
            {
                ServerManager.SendParticleTrail(origin, end, duration);
                return;
            }

            var direction = distance < 0.0001f
                ? (shotDirection.sqrMagnitude < 0.0001f ? Vector3.forward : shotDirection.normalized)
                : toEnd / distance;

            GetVisualBasis(direction, out _, out var right, out var forwardFlat);

            var segmentCount = Mathf.Max(4, steps);
            Vector3? previousA = null;
            Vector3? previousB = null;

            for (var i = 0; i <= segmentCount; i++)
            {
                var t = i / (float)segmentCount;
                var angle = t * Mathf.PI * 2f * 1.75f;
                var shrink = Mathf.Lerp(1f, 0.28f, t);
                var center = Vector3.Lerp(origin, end, t);
                var ringA = right * Mathf.Cos(angle) + forwardFlat * Mathf.Sin(angle);
                var ringB = right * Mathf.Cos(angle + Mathf.PI) + forwardFlat * Mathf.Sin(angle + Mathf.PI);
                var pointA = center + ringA * (radius * shrink) + Vector3.up * (Mathf.Sin(angle * 0.5f) * verticalAmplitude);
                var pointB = center + ringB * (radius * shrink) + Vector3.up * (Mathf.Sin((angle + Mathf.PI) * 0.5f) * verticalAmplitude);

                if (previousA.HasValue)
                    ServerManager.SendParticleTrail(previousA.Value, pointA, duration);
                if (previousB.HasValue)
                    ServerManager.SendParticleTrail(previousB.Value, pointB, duration);

                previousA = pointA;
                previousB = pointB;
            }

            if (previousA.HasValue)
                ServerManager.SendParticleTrail(previousA.Value, end, duration);
            if (previousB.HasValue)
                ServerManager.SendParticleTrail(previousB.Value, end, duration);
        }


        
        private static void SendSnowCascade(Vector3 center, Vector3 shotDirection, float radius, float fallHeight, float duration, int count)
        {
            GetVisualBasis(shotDirection, out _, out var right, out var forwardFlat);

            var flakes = Mathf.Max(3, count);
            for (var i = 0; i < flakes; i++)
            {
                var angle = (i / (float)flakes) * Mathf.PI * 2f + Pipliz.Random.NextFloat(-0.28f, 0.28f);
                var spread = radius * Pipliz.Random.NextFloat(0.45f, 1f);
                var offset = right * Mathf.Cos(angle) * spread + forwardFlat * Mathf.Sin(angle) * spread;
                var start = center + offset + Vector3.up * fallHeight;
                var end = center + offset * 0.4f + Vector3.up * Pipliz.Random.NextFloat(0.08f, 0.35f);
                ServerManager.SendParticleTrail(start, end, duration);
            }
        }

        private static void SendSnakeTrail(Vector3 origin, Vector3 end, Vector3 shotDirection, float radius, float duration, int steps, float waves = 2.2f, float verticalAmplitude = 0.14f)
        {
            var toEnd = end - origin;
            var distance = toEnd.magnitude;
            if (distance < 0.35f)
            {
                ServerManager.SendParticleTrail(origin, end, duration);
                return;
            }

            var direction = distance < 0.0001f
                ? (shotDirection.sqrMagnitude < 0.0001f ? Vector3.forward : shotDirection.normalized)
                : toEnd / distance;

            GetVisualBasis(direction, out _, out var right, out var forwardFlat);

            var segmentCount = Mathf.Max(4, steps);
            var previous = origin;
            for (var i = 1; i <= segmentCount; i++)
            {
                var t = i / (float)segmentCount;
                var angle = t * Mathf.PI * 2f * waves;
                var lateral = Mathf.Sin(angle) * Mathf.Lerp(radius, radius * 0.18f, t);
                var forwardWave = Mathf.Sin(angle * 0.5f + 0.7f) * Mathf.Lerp(radius * 0.35f, 0f, t);
                var point = Vector3.Lerp(origin, end, t)
                    + right * lateral
                    + forwardFlat * forwardWave
                    + Vector3.up * (Mathf.Sin(angle * 0.7f) * verticalAmplitude);
                ServerManager.SendParticleTrail(previous, point, duration);
                previous = point;
            }

            ServerManager.SendParticleTrail(previous, end, duration);
        }

        private static void SendSlowRockFall(Vector3 center, Vector3 shotDirection, float radius, float fallHeight, float duration, int count)
        {
            GetVisualBasis(shotDirection, out _, out var right, out var forwardFlat);

            var rocks = Mathf.Max(2, count);
            for (var i = 0; i < rocks; i++)
            {
                var angle = (i / (float)rocks) * Mathf.PI * 2f + Pipliz.Random.NextFloat(-0.18f, 0.18f);
                var spread = radius * Pipliz.Random.NextFloat(0.35f, 0.85f);
                var offset = right * Mathf.Cos(angle) * spread + forwardFlat * Mathf.Sin(angle) * spread;
                var start = center + offset + Vector3.up * fallHeight;
                var end = center + offset * 0.25f + Vector3.up * Pipliz.Random.NextFloat(-0.12f, 0.08f);
                ServerManager.SendParticleTrail(start, end, duration);
            }
        }

        private static void SendLingeringConverge(Vector3 anchor, Vector3 impactCenter, Vector3 shotDirection, float radius, float height, float duration, int count)
        {
            GetVisualBasis(shotDirection, out _, out var right, out var forwardFlat);

            var particles = Mathf.Max(3, count);
            for (var i = 0; i < particles; i++)
            {
                var angle = (i / (float)particles) * Mathf.PI * 2f;
                var start = anchor + (right * Mathf.Cos(angle) + forwardFlat * Mathf.Sin(angle)) * radius + Vector3.up * height;
                var end = impactCenter + (right * Mathf.Cos(angle + Mathf.PI * 0.45f) + forwardFlat * Mathf.Sin(angle + Mathf.PI * 0.45f)) * (radius * 0.12f) + Vector3.up * 0.18f;
                ServerManager.SendParticleTrail(start, end, duration);
            }
        }

        private static void SendCastTravelVisual(Vector3 origin, Vector3 end, Vector3 shotDirection, BurstVisualStyle style, float burstRadius)
        {
            if (style == BurstVisualStyle.None)
                return;

            var toEnd = end - origin;
            var distance = toEnd.magnitude;
            if (distance < 2.2f)
                return;

            var direction = distance < 0.0001f
                ? (shotDirection.sqrMagnitude < 0.0001f ? Vector3.forward : shotDirection.normalized)
                : toEnd / distance;

            GetVisualBasis(direction, out var forward, out var right, out var forwardFlat);

            var diagonalA = (forwardFlat + right).normalized;
            var diagonalB = (forwardFlat - right).normalized;
            var diagonalC = (-forwardFlat + right).normalized;
            var diagonalD = (-forwardFlat - right).normalized;

            var travelRadius = Mathf.Max(1.2f, burstRadius * 0.5f);
            var pointA = origin + direction * Mathf.Clamp(distance * 0.24f, 0.75f, Mathf.Max(0.75f, distance - 1.8f));
            var pointB = origin + direction * Mathf.Clamp(distance * 0.5f, 1.25f, Mathf.Max(1.25f, distance - 1.0f));
            var pointC = origin + direction * Mathf.Clamp(distance * 0.78f, 1.75f, Mathf.Max(1.75f, distance - 0.3f));

            switch (style)
            {
                case BurstVisualStyle.ManaPulse:
                    SendHelixTrail(origin, pointC, direction, travelRadius * 0.28f, 0.14f, 4, 0.05f);
                    ServerManager.SendParticleTrail(origin + right * (travelRadius * 0.32f) + Vector3.up * 0.32f, pointA - right * (travelRadius * 0.2f) + Vector3.up * 0.4f, 0.16f);
                    ServerManager.SendParticleTrail(origin - right * (travelRadius * 0.32f) + Vector3.up * 0.32f, pointA + right * (travelRadius * 0.2f) + Vector3.up * 0.4f, 0.16f);
                    SendBurstVisual(pointC, direction, style, Mathf.Max(1.05f, travelRadius * 0.8f));
                    break;

                case BurstVisualStyle.SparkArc:
                    SendLightningTrail(origin + right * (travelRadius * 0.25f) + Vector3.up * 0.35f, pointA + Vector3.up * 0.25f, 0.11f, 0.9f, 5, false);
                    SendLightningTrail(pointA + Vector3.up * 0.25f, pointB + Vector3.up * 0.2f, 0.11f, 1f, 5, true);
                    SendLightningTrail(pointB + Vector3.up * 0.2f, pointC + Vector3.up * 0.15f, 0.11f, 0.95f, 5, false);
                    SendBurstVisual(pointB, direction, style, travelRadius);
                    break;

                case BurstVisualStyle.ArcBurst:
                    SendLightningTrail(pointA + Vector3.up * 1.9f, pointA + Vector3.up * 0.2f, 0.12f, 0.8f, 5, false);
                    SendLightningTrail(pointB + Vector3.up * 2.1f, pointB + Vector3.up * 0.15f, 0.12f, 0.85f, 5, false);
                    SendLightningTrail(pointC + Vector3.up * 1.8f, pointC + Vector3.up * 0.2f, 0.12f, 0.8f, 5, false);
                    SendLightningTrail(pointA + right * (travelRadius * 0.7f) + Vector3.up * 1.2f, pointA, 0.11f, 0.75f, 4, false);
                    SendLightningTrail(pointB - right * (travelRadius * 0.7f) + Vector3.up * 1.2f, pointB, 0.11f, 0.75f, 4, false);
                    SendBurstVisual(pointB, direction, style, travelRadius);
                    break;

                case BurstVisualStyle.Bramble:
                    ServerManager.SendParticleTrail(pointA - Vector3.up * 0.35f, pointA + forwardFlat * (travelRadius * 0.75f) + Vector3.up * 0.65f, 0.2f);
                    ServerManager.SendParticleTrail(pointB - Vector3.up * 0.35f, pointB - right * (travelRadius * 0.7f) + Vector3.up * 0.55f, 0.22f);
                    ServerManager.SendParticleTrail(pointC - Vector3.up * 0.35f, pointC + right * (travelRadius * 0.65f) + Vector3.up * 0.7f, 0.22f);
                    ServerManager.SendParticleTrail(pointA - Vector3.up * 0.3f, pointB + diagonalA * (travelRadius * 0.45f) + Vector3.up * 0.9f, 0.2f);
                    ServerManager.SendParticleTrail(pointA - Vector3.up * 0.3f, pointB + diagonalD * (travelRadius * 0.4f) + Vector3.up * 0.8f, 0.2f);
                    SendBurstVisual(pointB, direction, style, travelRadius);
                    break;

                case BurstVisualStyle.Inferno:
                    SendHelixTrail(origin, pointC + Vector3.up * 0.35f, direction, travelRadius * 0.5f, 0.16f, 6, 0.22f);
                    ServerManager.SendParticleTrail(origin + right * (travelRadius * 0.45f), pointA + right * (travelRadius * 0.2f) + Vector3.up * 1.15f, 0.18f);
                    ServerManager.SendParticleTrail(origin - right * (travelRadius * 0.45f), pointA - right * (travelRadius * 0.2f) + Vector3.up * 1.15f, 0.18f);
                    ServerManager.SendParticleTrail(pointA + forwardFlat * (travelRadius * 0.45f), pointB + right * (travelRadius * 0.15f) + Vector3.up * 1.35f, 0.18f);
                    ServerManager.SendParticleTrail(pointA - forwardFlat * (travelRadius * 0.45f), pointB - right * (travelRadius * 0.15f) + Vector3.up * 1.35f, 0.18f);
                    SendBurstVisual(pointC, direction, style, travelRadius);
                    break;

                case BurstVisualStyle.Shatter:
                    ServerManager.SendParticleTrail(origin + Vector3.up * 0.4f, pointA + forwardFlat * (travelRadius * 0.7f) + Vector3.up * 0.6f, 0.16f);
                    ServerManager.SendParticleTrail(origin + Vector3.up * 0.4f, pointA - forwardFlat * (travelRadius * 0.7f) + Vector3.up * 0.6f, 0.16f);
                    ServerManager.SendParticleTrail(pointB + right * (travelRadius * 0.9f) + Vector3.up * 0.7f, pointC - right * (travelRadius * 0.35f) + Vector3.up * 0.35f, 0.16f);
                    ServerManager.SendParticleTrail(pointB - right * (travelRadius * 0.9f) + Vector3.up * 0.7f, pointC + right * (travelRadius * 0.35f) + Vector3.up * 0.35f, 0.16f);
                    ServerManager.SendParticleTrail(pointA + diagonalA * (travelRadius * 0.7f), pointB + diagonalC * (travelRadius * 0.35f) + Vector3.up * 0.55f, 0.14f);
                    ServerManager.SendParticleTrail(pointA + diagonalD * (travelRadius * 0.7f), pointB + diagonalB * (travelRadius * 0.35f) + Vector3.up * 0.55f, 0.14f);
                    SendSnowCascade(pointA, direction, travelRadius * 0.7f, 1.45f, 0.48f, 3);
                    SendSnowCascade(pointC, direction, travelRadius * 0.9f, 1.85f, 0.58f, 4);
                    SendBurstVisual(pointC, direction, style, travelRadius);
                    break;

                case BurstVisualStyle.Prism:
                    SendHelixTrail(origin, pointB, direction, travelRadius * 0.28f, 0.14f, 5, 0.06f);
                    ServerManager.SendParticleTrail(pointA + diagonalA * (travelRadius * 0.6f) + Vector3.up * 0.9f, pointB + diagonalB * (travelRadius * 0.4f) + Vector3.up * 0.65f, 0.16f);
                    ServerManager.SendParticleTrail(pointA + diagonalB * (travelRadius * 0.6f) + Vector3.up * 0.9f, pointB + diagonalC * (travelRadius * 0.4f) + Vector3.up * 0.65f, 0.16f);
                    ServerManager.SendParticleTrail(pointA + diagonalC * (travelRadius * 0.6f) + Vector3.up * 0.9f, pointB + diagonalD * (travelRadius * 0.4f) + Vector3.up * 0.65f, 0.16f);
                    ServerManager.SendParticleTrail(pointA + diagonalD * (travelRadius * 0.6f) + Vector3.up * 0.9f, pointB + diagonalA * (travelRadius * 0.4f) + Vector3.up * 0.65f, 0.16f);
                    SendBurstVisual(pointB, direction, style, travelRadius);
                    break;

                case BurstVisualStyle.AetherWave:
                    SendHelixTrail(origin, pointC + Vector3.up * 0.25f, direction, travelRadius * 0.35f, 0.15f, 6, 0.14f);
                    ServerManager.SendParticleTrail(origin + Vector3.up * 0.25f, pointA + right * (travelRadius * 0.25f) + Vector3.up * 1.05f, 0.2f);
                    ServerManager.SendParticleTrail(origin + Vector3.up * 0.3f, pointA - right * (travelRadius * 0.25f) + Vector3.up * 1.05f, 0.2f);
                    ServerManager.SendParticleTrail(pointA + right * (travelRadius * 0.35f) + Vector3.up * 0.8f, pointB + forwardFlat * (travelRadius * 0.25f) + Vector3.up * 1.2f, 0.18f);
                    ServerManager.SendParticleTrail(pointA - right * (travelRadius * 0.35f) + Vector3.up * 0.8f, pointB - forwardFlat * (travelRadius * 0.25f) + Vector3.up * 1.2f, 0.18f);
                    SendBurstVisual(pointC, direction, style, travelRadius);
                    break;

                case BurstVisualStyle.Hemorrhage:
                    SendHelixTrail(origin, pointC, direction, travelRadius * 0.3f, 0.16f, 5, 0.05f);
                    ServerManager.SendParticleTrail(pointA + right * (travelRadius * 0.85f) + Vector3.up * 0.65f, pointA + Vector3.up * 0.2f, 0.16f);
                    ServerManager.SendParticleTrail(pointA - right * (travelRadius * 0.85f) + Vector3.up * 0.65f, pointA + Vector3.up * 0.2f, 0.16f);
                    ServerManager.SendParticleTrail(pointB + forwardFlat * (travelRadius * 0.75f) + Vector3.up * 0.75f, pointB - Vector3.up * 0.15f, 0.16f);
                    ServerManager.SendParticleTrail(pointB - forwardFlat * (travelRadius * 0.75f) + Vector3.up * 0.75f, pointB - Vector3.up * 0.15f, 0.16f);
                    SendBurstVisual(pointC, direction, style, travelRadius);
                    break;

                case BurstVisualStyle.Rupture:
                    SendHelixTrail(origin, pointC, direction, travelRadius * 0.44f, 0.2f, 5, 0.08f);
                    ServerManager.SendParticleTrail(pointA + right * (travelRadius * 1.15f) + Vector3.up * 0.8f, pointA + Vector3.up * 0.15f, 0.34f);
                    ServerManager.SendParticleTrail(pointA - right * (travelRadius * 1.15f) + Vector3.up * 0.8f, pointA + Vector3.up * 0.15f, 0.34f);
                    ServerManager.SendParticleTrail(pointB + forwardFlat * (travelRadius * 1.2f) + Vector3.up * 1.05f, pointB + Vector3.up * 0.15f, 0.38f);
                    ServerManager.SendParticleTrail(pointB - forwardFlat * (travelRadius * 1.2f) + Vector3.up * 1.05f, pointB + Vector3.up * 0.15f, 0.38f);
                    SendLingeringConverge(pointA, pointC, direction, travelRadius * 0.95f, 0.7f, 0.55f, 4);
                    SendLingeringConverge(pointB, pointC, direction, travelRadius * 0.8f, 0.55f, 0.62f, 4);
                    SendOrbitCollapse(pointB, direction, travelRadius * 0.85f, 0.7f, 0.2f, 5, true);
                    SendBurstVisual(pointB, direction, style, travelRadius);
                    SendBurstVisual(pointC, direction, style, Mathf.Max(1.2f, travelRadius * 0.95f));
                    break;

                case BurstVisualStyle.ToxicBloom:
                    SendSnakeTrail(origin, pointB, direction, travelRadius * 0.42f, 0.22f, 6, 1.8f, 0.08f);
                    SendSnakeTrail(pointA + diagonalA * (travelRadius * 0.3f), pointC + diagonalD * (travelRadius * 0.18f), direction, travelRadius * 0.32f, 0.24f, 6, 1.55f, 0.06f);
                    SendSnakeTrail(pointA + diagonalB * (travelRadius * 0.3f), pointC + diagonalC * (travelRadius * 0.18f), direction, travelRadius * 0.32f, 0.24f, 6, 1.55f, 0.06f);
                    ServerManager.SendParticleTrail(pointB + right * (travelRadius * 0.4f), pointC + right * (travelRadius * 0.7f) + Vector3.up * 0.9f, 0.2f);
                    ServerManager.SendParticleTrail(pointB - right * (travelRadius * 0.4f), pointC - right * (travelRadius * 0.7f) + Vector3.up * 0.9f, 0.2f);
                    SendOrbitCollapse(pointC, direction, travelRadius * 0.58f, 0.38f, 0.2f, 4, false);
                    SendBurstVisual(pointC, direction, style, travelRadius);
                    break;

                case BurstVisualStyle.Faultline:
                    ServerManager.SendParticleTrail(pointA - forwardFlat * (travelRadius * 0.35f) - Vector3.up * 0.15f, pointA + forwardFlat * (travelRadius * 0.85f) - Vector3.up * 0.05f, 0.18f);
                    ServerManager.SendParticleTrail(pointB + forwardFlat * (travelRadius * 0.25f) - Vector3.up * 0.15f, pointB - forwardFlat * (travelRadius * 0.9f) - Vector3.up * 0.05f, 0.18f);
                    ServerManager.SendParticleTrail(pointB + right * (travelRadius * 0.55f), pointB + right * (travelRadius * 0.15f) + Vector3.up * 0.8f, 0.16f);
                    ServerManager.SendParticleTrail(pointC - right * (travelRadius * 0.55f), pointC - right * (travelRadius * 0.15f) + Vector3.up * 0.8f, 0.16f);
                    ServerManager.SendParticleTrail(pointB + diagonalA * (travelRadius * 0.45f), pointC + diagonalA * (travelRadius * 0.85f) - Vector3.up * 0.1f, 0.14f);
                    ServerManager.SendParticleTrail(pointB + diagonalD * (travelRadius * 0.45f), pointC + diagonalD * (travelRadius * 0.85f) - Vector3.up * 0.1f, 0.14f);
                    SendSlowRockFall(pointB, direction, travelRadius * 0.55f, 2.4f, 0.34f, 2);
                    SendSlowRockFall(pointC, direction, travelRadius * 0.7f, 3f, 0.38f, 3);
                    SendBurstVisual(pointC, direction, style, travelRadius);
                    break;

                case BurstVisualStyle.Tempest:
                    SendLightningTrail(pointA + Vector3.up * 2.2f, pointA + Vector3.up * 0.2f, 0.14f, 1.25f, 6, true);
                    SendLightningTrail(pointB + right * (travelRadius * 0.9f) + Vector3.up * 2f, pointB + right * (travelRadius * 0.25f), 0.14f, 1.2f, 6, true);
                    SendLightningTrail(pointB - right * (travelRadius * 0.9f) + Vector3.up * 2f, pointB - right * (travelRadius * 0.25f), 0.14f, 1.2f, 6, true);
                    SendLightningTrail(pointC + forwardFlat * (travelRadius * 0.8f) + Vector3.up * 1.9f, pointC + forwardFlat * (travelRadius * 0.2f), 0.14f, 1.15f, 6, true);
                    SendLightningTrail(pointC - forwardFlat * (travelRadius * 0.8f) + Vector3.up * 1.9f, pointC - forwardFlat * (travelRadius * 0.2f), 0.14f, 1.15f, 6, true);
                    SendBurstVisual(pointB, direction, style, travelRadius);
                    SendBurstVisual(pointC, direction, style, Mathf.Max(1.2f, travelRadius * 0.95f));
                    break;

                default:
                    SendBurstVisual(pointA, direction, style, travelRadius);
                    SendBurstVisual(pointC, direction, style, Mathf.Max(1.1f, travelRadius * 0.85f));
                    break;
            }
        }


        
        private static void SendBurstVisual(Vector3 burstOrigin, Vector3 shotDirection, BurstVisualStyle style, float burstRadius)
        {
            if (style == BurstVisualStyle.None)
                return;

            GetVisualBasis(shotDirection, out var forward, out var right, out var forwardFlat);

            var diagonalA = (forwardFlat + right).normalized;
            var diagonalB = (forwardFlat - right).normalized;
            var diagonalC = (-forwardFlat + right).normalized;
            var diagonalD = (-forwardFlat - right).normalized;

            var radius = Mathf.Max(1.4f, burstRadius);
            var shortRadius = radius * 0.45f;
            switch (style)
            {
                case BurstVisualStyle.ManaPulse:
                    SendOrbitCollapse(burstOrigin, shotDirection, radius * 0.58f, 0.55f, 0.14f, 4, false);
                    ServerManager.SendParticleTrail(burstOrigin + Vector3.up * 1.3f, burstOrigin + Vector3.up * 0.18f, 0.16f);
                    ServerManager.SendParticleTrail(burstOrigin + right * (shortRadius * 0.9f) + Vector3.up * 0.7f, burstOrigin - right * (shortRadius * 0.9f) + Vector3.up * 0.7f, 0.12f);
                    ServerManager.SendParticleTrail(burstOrigin + forwardFlat * (radius * 0.75f) + Vector3.up * 0.4f, burstOrigin - forwardFlat * (radius * 0.75f) + Vector3.up * 0.4f, 0.12f);
                    break;

                case BurstVisualStyle.SparkArc:
                    SendLightningTrail(burstOrigin + Vector3.up * 2f, burstOrigin + Vector3.up * 0.15f, 0.12f, 0.9f, 5, false);
                    SendLightningTrail(burstOrigin + right * (radius * 0.95f) + Vector3.up * 1.2f, burstOrigin + Vector3.up * 0.2f, 0.11f, 0.85f, 5, false);
                    SendLightningTrail(burstOrigin - right * (radius * 0.95f) + Vector3.up * 1.2f, burstOrigin + Vector3.up * 0.2f, 0.11f, 0.85f, 5, false);
                    SendLightningTrail(burstOrigin + forwardFlat * (radius * 0.7f) + Vector3.up * 0.9f, burstOrigin - right * (shortRadius * 0.5f) + Vector3.up * 0.25f, 0.11f, 0.8f, 4, false);
                    SendLightningTrail(burstOrigin - forwardFlat * (radius * 0.7f) + Vector3.up * 0.9f, burstOrigin + right * (shortRadius * 0.5f) + Vector3.up * 0.25f, 0.11f, 0.8f, 4, false);
                    break;

                case BurstVisualStyle.ArcBurst:
                    SendLightningTrail(burstOrigin + Vector3.up * 2.15f, burstOrigin, 0.12f, 0.85f, 5, false);
                    SendLightningTrail(burstOrigin + forwardFlat * (radius * 0.8f) + Vector3.up * 1.3f, burstOrigin, 0.11f, 0.8f, 5, false);
                    SendLightningTrail(burstOrigin - forwardFlat * (radius * 0.8f) + Vector3.up * 1.3f, burstOrigin, 0.11f, 0.8f, 5, false);
                    SendLightningTrail(burstOrigin + right * (radius * 0.7f) + Vector3.up * 1.15f, burstOrigin, 0.11f, 0.75f, 4, false);
                    SendLightningTrail(burstOrigin - right * (radius * 0.7f) + Vector3.up * 1.15f, burstOrigin, 0.11f, 0.75f, 4, false);
                    ServerManager.SendParticleTrail(burstOrigin, burstOrigin + Vector3.up * 1.9f, 0.16f);
                    break;

                case BurstVisualStyle.Bramble:
                    ServerManager.SendParticleTrail(burstOrigin - Vector3.up * 0.15f, burstOrigin + Vector3.up * (radius * 0.35f + 1.4f), 0.2f);
                    ServerManager.SendParticleTrail(burstOrigin - Vector3.up * 0.2f, burstOrigin + forwardFlat * radius - Vector3.up * 0.25f, 0.22f);
                    ServerManager.SendParticleTrail(burstOrigin - Vector3.up * 0.2f, burstOrigin - forwardFlat * (radius * 0.9f) - Vector3.up * 0.3f, 0.22f);
                    ServerManager.SendParticleTrail(burstOrigin - Vector3.up * 0.1f, burstOrigin + right * (radius * 0.85f) + Vector3.up * 0.35f, 0.18f);
                    ServerManager.SendParticleTrail(burstOrigin + forwardFlat * (radius * 0.35f), burstOrigin + diagonalA * (radius * 0.85f) + Vector3.up * 0.95f, 0.16f);
                    ServerManager.SendParticleTrail(burstOrigin - forwardFlat * (radius * 0.2f), burstOrigin + diagonalD * (radius * 0.8f) + Vector3.up * 0.9f, 0.16f);
                    break;

                case BurstVisualStyle.Inferno:
                    SendHelixTrail(burstOrigin, burstOrigin + Vector3.up * (radius * 0.7f + 1.8f), shotDirection, radius * 0.38f, 0.15f, 5, 0.14f);
                    ServerManager.SendExplosionEffect(burstOrigin, 8f, Mathf.Max(2.9f, radius), 1f, 0.2f);
                    ServerManager.SendParticleTrail(burstOrigin, burstOrigin + Vector3.up * (radius * 0.65f + 1.9f), 0.24f);
                    ServerManager.SendParticleTrail(burstOrigin + right * (radius * 0.35f), burstOrigin + forwardFlat * (radius * 0.45f) + Vector3.up * 1.55f, 0.18f);
                    ServerManager.SendParticleTrail(burstOrigin - right * (radius * 0.35f), burstOrigin - forwardFlat * (radius * 0.45f) + Vector3.up * 1.55f, 0.18f);
                    ServerManager.SendParticleTrail(burstOrigin + forwardFlat * radius + Vector3.up * 0.6f, burstOrigin - right * (shortRadius * 0.3f) + Vector3.up * 1.1f, 0.18f);
                    ServerManager.SendParticleTrail(burstOrigin - forwardFlat * radius + Vector3.up * 0.6f, burstOrigin + right * (shortRadius * 0.3f) + Vector3.up * 1.1f, 0.18f);
                    break;

                case BurstVisualStyle.Shatter:
                    SendOrbitCollapse(burstOrigin, shotDirection, radius * 0.46f, 0.32f, 0.14f, 3, false);
                    ServerManager.SendExplosionEffect(burstOrigin, 8f, Mathf.Max(2.4f, radius * 0.82f), 1f, 0.2f);
                    ServerManager.SendParticleTrail(burstOrigin, burstOrigin + forwardFlat * (radius * 1.05f), 0.16f);
                    ServerManager.SendParticleTrail(burstOrigin, burstOrigin - forwardFlat * (radius * 1.05f), 0.16f);
                    ServerManager.SendParticleTrail(burstOrigin, burstOrigin + right * (radius * 0.95f), 0.16f);
                    ServerManager.SendParticleTrail(burstOrigin, burstOrigin - right * (radius * 0.95f), 0.16f);
                    SendSnowCascade(burstOrigin, shotDirection, radius * 0.85f, 2.15f, 0.56f, 5);
                    break;

                case BurstVisualStyle.Prism:
                    SendOrbitCollapse(burstOrigin, shotDirection, radius * 0.72f, 0.72f, 0.13f, 6, true);
                    ServerManager.SendParticleTrail(burstOrigin, burstOrigin + Vector3.up * (radius * 0.8f + 1.35f), 0.18f);
                    ServerManager.SendParticleTrail(burstOrigin + diagonalA * radius + Vector3.up * 0.85f, burstOrigin + diagonalB * radius + Vector3.up * 0.85f, 0.14f);
                    ServerManager.SendParticleTrail(burstOrigin + diagonalB * radius + Vector3.up * 0.85f, burstOrigin + diagonalD * radius + Vector3.up * 0.85f, 0.14f);
                    ServerManager.SendParticleTrail(burstOrigin + diagonalD * radius + Vector3.up * 0.85f, burstOrigin + diagonalC * radius + Vector3.up * 0.85f, 0.14f);
                    ServerManager.SendParticleTrail(burstOrigin + diagonalC * radius + Vector3.up * 0.85f, burstOrigin + diagonalA * radius + Vector3.up * 0.85f, 0.14f);
                    ServerManager.SendParticleTrail(burstOrigin, burstOrigin + diagonalA * (radius * 0.9f), 0.14f);
                    ServerManager.SendParticleTrail(burstOrigin, burstOrigin + diagonalB * (radius * 0.9f), 0.14f);
                    ServerManager.SendParticleTrail(burstOrigin, burstOrigin + diagonalC * (radius * 0.9f), 0.14f);
                    ServerManager.SendParticleTrail(burstOrigin, burstOrigin + diagonalD * (radius * 0.9f), 0.14f);
                    break;

                case BurstVisualStyle.AetherWave:
                    SendHelixTrail(burstOrigin, burstOrigin + Vector3.up * (radius * 0.4f + 1.3f), shotDirection, radius * 0.26f, 0.14f, 5, 0.1f);
                    ServerManager.SendParticleTrail(burstOrigin + Vector3.up * 0.25f, burstOrigin + Vector3.up * (radius * 0.55f + 2.15f), 0.24f);
                    ServerManager.SendParticleTrail(burstOrigin + right * (radius * 0.65f), burstOrigin + forwardFlat * (radius * 0.35f) + Vector3.up * 1.15f, 0.16f);
                    ServerManager.SendParticleTrail(burstOrigin - right * (radius * 0.65f), burstOrigin - forwardFlat * (radius * 0.35f) + Vector3.up * 1.15f, 0.16f);
                    ServerManager.SendParticleTrail(burstOrigin + diagonalA * (radius * 0.75f) + Vector3.up * 0.45f, burstOrigin + Vector3.up * 1.5f, 0.16f);
                    ServerManager.SendParticleTrail(burstOrigin + diagonalD * (radius * 0.75f) + Vector3.up * 0.45f, burstOrigin + Vector3.up * 1.5f, 0.16f);
                    break;

                case BurstVisualStyle.Hemorrhage:
                    SendOrbitCollapse(burstOrigin, shotDirection, radius * 0.7f, 0.5f, 0.13f, 5, false);
                    ServerManager.SendExplosionEffect(burstOrigin, 8f, Mathf.Max(2.2f, radius * 0.72f), 1f, 0.2f);
                    ServerManager.SendParticleTrail(burstOrigin + Vector3.up * 1.75f, burstOrigin - Vector3.up * 0.05f, 0.18f);
                    ServerManager.SendParticleTrail(burstOrigin + right * (radius * 0.8f) + Vector3.up * 0.7f, burstOrigin + Vector3.up * 0.1f, 0.16f);
                    ServerManager.SendParticleTrail(burstOrigin - right * (radius * 0.8f) + Vector3.up * 0.7f, burstOrigin + Vector3.up * 0.1f, 0.16f);
                    ServerManager.SendParticleTrail(burstOrigin + forwardFlat * (radius * 0.7f) + Vector3.up * 0.65f, burstOrigin - Vector3.up * 0.15f, 0.16f);
                    ServerManager.SendParticleTrail(burstOrigin - forwardFlat * (radius * 0.7f) + Vector3.up * 0.65f, burstOrigin - Vector3.up * 0.15f, 0.16f);
                    break;

                case BurstVisualStyle.Rupture:
                    SendOrbitCollapse(burstOrigin, shotDirection, radius * 0.82f, 0.65f, 0.2f, 6, true);
                    SendLingeringConverge(burstOrigin + Vector3.up * 0.1f, burstOrigin + Vector3.up * 0.15f, shotDirection, radius * 0.9f, 0.55f, 0.62f, 4);
                    ServerManager.SendExplosionEffect(burstOrigin, 8f, Mathf.Max(1.9f, radius * 0.58f), 1f, 0.2f);
                    ServerManager.SendParticleTrail(burstOrigin + Vector3.up * 2f, burstOrigin + Vector3.up * 0.15f, 0.4f);
                    ServerManager.SendParticleTrail(burstOrigin + right * (radius * 1.05f) + Vector3.up * 0.8f, burstOrigin + Vector3.up * 0.15f, 0.38f);
                    ServerManager.SendParticleTrail(burstOrigin - right * (radius * 1.05f) + Vector3.up * 0.8f, burstOrigin + Vector3.up * 0.15f, 0.38f);
                    ServerManager.SendParticleTrail(burstOrigin + forwardFlat * (radius * 1.05f) + Vector3.up * 0.9f, burstOrigin + Vector3.up * 0.15f, 0.38f);
                    ServerManager.SendParticleTrail(burstOrigin - forwardFlat * (radius * 1.05f) + Vector3.up * 0.9f, burstOrigin + Vector3.up * 0.15f, 0.38f);
                    break;

                case BurstVisualStyle.ToxicBloom:
                    SendOrbitCollapse(burstOrigin, shotDirection, radius * 0.58f, 0.38f, 0.18f, 4, false);
                    SendSnakeTrail(burstOrigin + Vector3.right * (radius * 0.45f), burstOrigin + Vector3.up * 0.15f, shotDirection, radius * 0.28f, 0.22f, 5, 1.4f, 0.05f);
                    SendSnakeTrail(burstOrigin - Vector3.right * (radius * 0.45f), burstOrigin + Vector3.up * 0.15f, shotDirection, radius * 0.28f, 0.22f, 5, 1.4f, 0.05f);
                    ServerManager.SendExplosionEffect(burstOrigin, 8f, Mathf.Max(2.3f, radius * 0.78f), 1f, 0.2f);
                    ServerManager.SendParticleTrail(burstOrigin - Vector3.up * 0.1f, burstOrigin + Vector3.up * (radius * 0.3f + 1.55f), 0.22f);
                    ServerManager.SendParticleTrail(burstOrigin + diagonalA * radius + Vector3.up * 0.55f, burstOrigin + Vector3.up * 0.2f, 0.18f);
                    ServerManager.SendParticleTrail(burstOrigin + diagonalD * radius + Vector3.up * 0.55f, burstOrigin + Vector3.up * 0.2f, 0.18f);
                    break;

                case BurstVisualStyle.Faultline:
                    ServerManager.SendExplosionEffect(burstOrigin, 8f, Mathf.Max(2.6f, radius * 0.92f), 1f, 0.2f);
                    ServerManager.SendParticleTrail(burstOrigin - forwardFlat * (radius * 0.3f) - Vector3.up * 0.1f, burstOrigin + forwardFlat * (radius * 1.1f) - Vector3.up * 0.05f, 0.22f);
                    ServerManager.SendParticleTrail(burstOrigin + forwardFlat * (radius * 0.25f) - Vector3.up * 0.1f, burstOrigin - forwardFlat * (radius * 1.05f) - Vector3.up * 0.05f, 0.22f);
                    ServerManager.SendParticleTrail(burstOrigin + right * (radius * 0.75f), burstOrigin + right * (radius * 0.2f) + Vector3.up * 0.95f, 0.16f);
                    ServerManager.SendParticleTrail(burstOrigin - right * (radius * 0.75f), burstOrigin - right * (radius * 0.2f) + Vector3.up * 0.95f, 0.16f);
                    SendSlowRockFall(burstOrigin, shotDirection, radius * 0.7f, 3.1f, 0.42f, 4);
                    break;

                case BurstVisualStyle.Tempest:
                    SendLightningTrail(burstOrigin + Vector3.up * (radius + 2.5f), burstOrigin + Vector3.up * 0.15f, 0.16f, 1.3f, 7, true);
                    SendLightningTrail(burstOrigin + right * (radius * 0.85f) + Vector3.up * 2.1f, burstOrigin + right * (radius * 0.25f), 0.14f, 1.2f, 6, true);
                    SendLightningTrail(burstOrigin - right * (radius * 0.85f) + Vector3.up * 2.1f, burstOrigin - right * (radius * 0.25f), 0.14f, 1.2f, 6, true);
                    SendLightningTrail(burstOrigin + forwardFlat * (radius * 0.85f) + Vector3.up * 2f, burstOrigin + forwardFlat * (radius * 0.25f), 0.14f, 1.15f, 6, true);
                    SendLightningTrail(burstOrigin - forwardFlat * (radius * 0.85f) + Vector3.up * 2f, burstOrigin - forwardFlat * (radius * 0.25f), 0.14f, 1.15f, 6, true);
                    SendLightningTrail(burstOrigin + diagonalA * (radius * 0.95f) + Vector3.up * 1.75f, burstOrigin + diagonalA * (radius * 0.2f), 0.13f, 1.05f, 5, false);
                    SendLightningTrail(burstOrigin + diagonalB * (radius * 0.95f) + Vector3.up * 1.75f, burstOrigin + diagonalB * (radius * 0.2f), 0.13f, 1.05f, 5, false);
                    break;
            }
        }


        private static IMonster TryGetDirectTargetMonster(PlayerClickedData playerClickData, Vector3 trailOrigin, float range)
        {
            if (playerClickData.HitType != PlayerClickedData.EHitType.NPC)
                return null;

            var npcHit = playerClickData.GetNPCHit();
            if (npcHit.DistanceToHit > range)
                return null;

            if (!MonsterTracker.TryGetMonsterByID(npcHit.NPCID, out var monster) || monster == null || !monster.IsValid)
                return null;

            return VoxelPhysics.CanSee(trailOrigin, monster.PositionToAimFor) ? monster : null;
        }

        private static bool TryGetBossRayHit(
            IMonster monster,
            Vector3 trailOrigin,
            Vector3 shotDirection,
            float range,
            float aimAssistRadius,
            out float forwardDistance,
            out Vector3 hitPoint)
        {
            forwardDistance = float.MaxValue;
            hitPoint = default;

            if (monster == null || !monster.IsValid)
                return false;

            if (BossVisualProxyManager.TryRaycastBoss(
                    trailOrigin,
                    shotDirection,
                    range,
                    out var visualBoss,
                    out _,
                    out var visualHitPoint) &&
                visualBoss != null &&
                visualBoss.ID == monster.ID &&
                VoxelPhysics.CanSee(trailOrigin, visualHitPoint))
            {
                forwardDistance = Mathf.Max(0f, Vector3.Dot(visualHitPoint - trailOrigin, shotDirection));
                hitPoint = visualHitPoint;
                return true;
            }

            var bossRadius = Mathf.Max(2.35f, aimAssistRadius * 4.5f);
            var basePosition = monster.Position.Vector;
            var aimPoint = monster.PositionToAimFor;
            var candidates = new[]
            {
                basePosition + Vector3.up * 0.65f,
                basePosition + Vector3.up * 1.25f,
                basePosition + Vector3.up * 1.9f,
                basePosition + Vector3.up * 2.65f,
                aimPoint,
                aimPoint + Vector3.down * 0.85f,
                aimPoint + Vector3.up * 0.45f
            };

            for (var i = 0; i < candidates.Length; i++)
            {
                var candidate = candidates[i];
                var toCandidate = candidate - trailOrigin;
                var candidateForwardDistance = Vector3.Dot(toCandidate, shotDirection);
                if (candidateForwardDistance <= 0f || candidateForwardDistance > range)
                    continue;

                var closestPoint = trailOrigin + shotDirection * candidateForwardDistance;
                if ((candidate - closestPoint).sqrMagnitude > bossRadius * bossRadius)
                    continue;

                if (!VoxelPhysics.CanSee(trailOrigin, candidate))
                    continue;

                if (candidateForwardDistance < forwardDistance)
                {
                    forwardDistance = candidateForwardDistance;
                    hitPoint = candidate;
                }
            }

            return forwardDistance < float.MaxValue;
        }

        private static float GetMagicDamage(Players.Player player, float baseDamage)
        {
            var damage = baseDamage + PlayerMagicStateManager.GetPlayerClickDamageBonus(player) * 0.5f;
            damage *= PlayerMagicStateManager.GetMagicSpellDamageMultiplier(player);
            var critChance = PlayerMagicStateManager.GetPlayerClickCritChance(player);
            if (critChance > 0f && Pipliz.Random.NextFloat() <= critChance)
                damage += PlayerMagicStateManager.GetPlayerClickCritBonus(player) * 0.65f;

            return damage;
        }

        private static void ApplyMonsterHit(
            Players.Player player,
            IMonster monster,
            Vector3 hitForce,
            float baseDamage,
            float dotDamagePerTick,
            int dotTickCount,
            float dotTickDelaySeconds,
            bool persistentDamageOverTime,
            BurstVisualStyle dotVisualStyle)
        {
            if (monster == null || !monster.IsValid)
                return;

            monster.OnHit(GetMagicDamage(player, baseDamage), hitForce, player, ModLoader.OnHitData.EHitSourceType.PlayerProjectile);

            if (monster.IsValid && dotDamagePerTick > 0f && dotTickCount > 0 && dotTickDelaySeconds > 0f)
            {
                var effectiveTickDelaySeconds = Mathf.Max(0.25f, dotTickDelaySeconds);
                var minimumTickCount = Mathf.CeilToInt(MinimumDamageOverTimeDurationSeconds / effectiveTickDelaySeconds);
                var effectiveTickCount = persistentDamageOverTime
                    ? System.Math.Max(PersistentDamageTickCount, minimumTickCount)
                    : System.Math.Max(dotTickCount, minimumTickCount);
                var effectiveDotDamagePerTick = dotDamagePerTick * DamageOverTimeStackDamageMultiplier;

                ServerManager.Effects.NewEffect(monster, new EffectsTracker.DamageTicks
                {
                    DamagePerTick = effectiveDotDamagePerTick,
                    TickCount = effectiveTickCount,
                    TickDelay = ServerTimeSpan.FromSeconds(effectiveTickDelaySeconds)
                });

                WandDebuffVisualManager.Track(monster, dotVisualStyle, effectiveTickCount, effectiveTickDelaySeconds, persistentDamageOverTime);
            }
        }

        private static void HealPlayer(Players.Player player, float amount)
        {
            var adjustedAmount = amount * PlayerMagicStateManager.GetHealingReceivedMultiplier(player);
            player.Health = Mathf.Min(player.HealthMax, player.Health + adjustedAmount);
            player.SendHealthPacket();
        }

        private struct NpcRayCollector : NPCTracker.INPCIterator
        {
            public Vector3 Origin;
            public Vector3 Direction;
            public float Range;
            public float AimAssistRadius;
            public float BestDistance;
            public NPCBase BestNpc;

            public void IterateNPC(NPCBase npc)
            {
                if (npc == null || !npc.IsValid)
                    return;

                var aimPoint = npc.Position.Vector + Vector3.up * 1.4f;
                var toTarget = aimPoint - Origin;
                var forwardDistance = Vector3.Dot(toTarget, Direction);

                if (forwardDistance <= 0f || forwardDistance > Range)
                    return;

                var closestPoint = Origin + Direction * forwardDistance;
                var distanceToRay = Vector3.Distance(aimPoint, closestPoint);
                if (distanceToRay > AimAssistRadius)
                    return;

                if (!VoxelPhysics.CanSee(Origin, aimPoint))
                    return;

                if (forwardDistance < BestDistance)
                {
                    BestDistance = forwardDistance;
                    BestNpc = npc;
                }
            }
        }
    }

    [ModLoader.ModManager]
    internal static class WandDebuffVisualManager
    {
        private struct ActiveDebuffVisual
        {
            public NPCID MonsterId;
            public WandCombatUtility.BurstVisualStyle Style;
            public long NextVisualAtMs;
            public long ExpireAtMs;
        }

        private const long UpdateIntervalMs = 200;
        private static long _nextUpdateMs;
        private static readonly Dictionary<NPCID, ActiveDebuffVisual> ActiveDebuffs = new Dictionary<NPCID, ActiveDebuffVisual>();
        private static readonly List<NPCID> ToRemove = new List<NPCID>();

        public static void Track(IMonster monster, WandCombatUtility.BurstVisualStyle style, int tickCount, float tickDelaySeconds, bool persistent)
        {
            if (monster == null || !monster.IsValid)
                return;

            var now = Pipliz.Time.MillisecondsSinceStart;
            var expireAt = persistent
                ? long.MaxValue
                : now + (long)(Mathf.Max(1f, tickCount * Mathf.Max(0.25f, tickDelaySeconds)) * 1000f) + 500L;

            ActiveDebuffs[monster.ID] = new ActiveDebuffVisual
            {
                MonsterId = monster.ID,
                Style = style,
                NextVisualAtMs = now,
                ExpireAtMs = expireAt
            };
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnUpdate, GameLoader.NAMESPACE + ".WandDebuffVisualManager.OnUpdate")]
        public static void OnUpdate()
        {
            var now = Pipliz.Time.MillisecondsSinceStart;
            if (now < _nextUpdateMs || ActiveDebuffs.Count == 0)
                return;

            ToRemove.Clear();

            foreach (var pair in ActiveDebuffs)
            {
                var active = pair.Value;
                if (now > active.ExpireAtMs)
                {
                    ToRemove.Add(pair.Key);
                    continue;
                }

                if (!MonsterTracker.TryGetMonsterByID(active.MonsterId, out var monster) || monster == null || !monster.IsValid)
                {
                    ToRemove.Add(pair.Key);
                    continue;
                }

                if (now < active.NextVisualAtMs)
                    continue;

                SendDebuffVisual(monster, active);
                active.NextVisualAtMs = now + GetVisualCadenceMs(active.Style);
                ActiveDebuffs[pair.Key] = active;
            }

            for (var i = 0; i < ToRemove.Count; i++)
                ActiveDebuffs.Remove(ToRemove[i]);

            ToRemove.Clear();
            _nextUpdateMs = now + UpdateIntervalMs;
        }

        private static long GetVisualCadenceMs(WandCombatUtility.BurstVisualStyle style)
        {
            switch (style)
            {
                case WandCombatUtility.BurstVisualStyle.Inferno:
                    return 520L;
                case WandCombatUtility.BurstVisualStyle.ToxicBloom:
                    return 700L;
                case WandCombatUtility.BurstVisualStyle.Hemorrhage:
                    return 620L;
                case WandCombatUtility.BurstVisualStyle.Rupture:
                    return 920L;
                case WandCombatUtility.BurstVisualStyle.Shatter:
                    return 820L;
                case WandCombatUtility.BurstVisualStyle.Faultline:
                    return 860L;
                case WandCombatUtility.BurstVisualStyle.Bramble:
                    return 680L;
                default:
                    return 720L;
            }
        }

        
        private static void SendDebuffVisual(IMonster monster, ActiveDebuffVisual active)
        {
            var center = monster.PositionToAimFor;
            var feet = monster.Position.Vector + Vector3.up * 0.15f;

            switch (active.Style)
            {
                case WandCombatUtility.BurstVisualStyle.Bramble:
                    ServerManager.SendParticleTrail(feet - Vector3.up * 0.05f, feet + Vector3.up * 1.35f, 0.18f);
                    ServerManager.SendParticleTrail(feet + Vector3.right * 0.65f, feet - Vector3.right * 0.25f + Vector3.up * 0.7f, 0.18f);
                    ServerManager.SendParticleTrail(feet - Vector3.right * 0.55f, feet + Vector3.right * 0.35f + Vector3.up * 0.85f, 0.18f);
                    ServerManager.SendParticleTrail(feet + Vector3.forward * 0.45f - Vector3.up * 0.05f, center + Vector3.right * 0.25f + Vector3.up * 0.95f, 0.18f);
                    break;

                case WandCombatUtility.BurstVisualStyle.Inferno:
                    ServerManager.SendParticleTrail(feet, center + Vector3.up * 1.05f, 0.2f);
                    ServerManager.SendParticleTrail(feet + Vector3.forward * 0.45f, center + Vector3.right * 0.25f + Vector3.up * 0.75f, 0.16f);
                    ServerManager.SendParticleTrail(feet - Vector3.forward * 0.45f, center - Vector3.right * 0.25f + Vector3.up * 0.75f, 0.16f);
                    ServerManager.SendParticleTrail(center + Vector3.right * 0.35f + Vector3.up * 0.35f, center + Vector3.up * 1.45f, 0.15f);
                    break;
                case WandCombatUtility.BurstVisualStyle.Shatter:
                    ServerManager.SendParticleTrail(center + new Vector3(0.55f, 1.45f, 0.15f), center + new Vector3(0.15f, 0.25f, 0.05f), 0.28f);
                    ServerManager.SendParticleTrail(center + new Vector3(-0.55f, 1.55f, -0.1f), center + new Vector3(-0.1f, 0.2f, -0.05f), 0.3f);
                    ServerManager.SendParticleTrail(center + new Vector3(0.15f, 1.7f, -0.55f), center + new Vector3(0.05f, 0.3f, -0.15f), 0.32f);
                    break;

                case WandCombatUtility.BurstVisualStyle.Faultline:
                    ServerManager.SendParticleTrail(center + new Vector3(0.45f, 2.1f, 0.1f), feet + new Vector3(0.1f, 0.05f, 0f), 0.34f);
                    ServerManager.SendParticleTrail(center + new Vector3(-0.55f, 2.3f, -0.2f), feet + new Vector3(-0.15f, 0.05f, -0.05f), 0.38f);
                    ServerManager.SendParticleTrail(center + new Vector3(0.15f, 2.45f, 0.45f), feet + new Vector3(0.05f, 0.05f, 0.1f), 0.36f);
                    break;


                case WandCombatUtility.BurstVisualStyle.ToxicBloom:
                    ServerManager.SendParticleTrail(center + Vector3.right * 0.75f + Vector3.up * 0.4f, center + Vector3.up * 0.2f, 0.22f);
                    ServerManager.SendParticleTrail(center - Vector3.right * 0.75f + Vector3.up * 0.4f, center + Vector3.up * 0.2f, 0.22f);
                    ServerManager.SendParticleTrail(center + Vector3.forward * 0.65f + Vector3.up * 0.3f, center + Vector3.right * 0.2f + Vector3.up * 0.35f, 0.2f);
                    ServerManager.SendParticleTrail(center - Vector3.forward * 0.65f + Vector3.up * 0.3f, center - Vector3.right * 0.2f + Vector3.up * 0.35f, 0.2f);
                    ServerManager.SendParticleTrail(center + new Vector3(0.55f, 0.45f, 0.25f), center + new Vector3(-0.15f, 0.2f, 0.05f), 0.22f);
                    break;

                case WandCombatUtility.BurstVisualStyle.Hemorrhage:
                    ServerManager.SendParticleTrail(center + Vector3.up * 1.2f, center + Vector3.down * 0.2f, 0.18f);
                    ServerManager.SendParticleTrail(center + Vector3.right * 0.55f + Vector3.up * 0.75f, center + Vector3.down * 0.05f, 0.16f);
                    ServerManager.SendParticleTrail(center - Vector3.right * 0.55f + Vector3.up * 0.75f, center + Vector3.down * 0.05f, 0.16f);
                    ServerManager.SendParticleTrail(center + Vector3.forward * 0.45f + Vector3.up * 0.65f, center + Vector3.down * 0.1f, 0.16f);
                    ServerManager.SendParticleTrail(center - Vector3.forward * 0.45f + Vector3.up * 0.65f, center + Vector3.down * 0.1f, 0.16f);
                    break;

                case WandCombatUtility.BurstVisualStyle.Rupture:
                    ServerManager.SendParticleTrail(center + Vector3.up * 1.55f, center + Vector3.up * 0.2f, 0.2f);
                    ServerManager.SendParticleTrail(center + Vector3.right * 1.05f + Vector3.up * 0.65f, center + Vector3.up * 0.2f, 0.22f);
                    ServerManager.SendParticleTrail(center - Vector3.right * 1.05f + Vector3.up * 0.65f, center + Vector3.up * 0.2f, 0.22f);
                    ServerManager.SendParticleTrail(center + Vector3.forward * 0.95f + Vector3.up * 0.55f, center + Vector3.up * 0.15f, 0.2f);
                    ServerManager.SendParticleTrail(center - Vector3.forward * 0.95f + Vector3.up * 0.55f, center + Vector3.up * 0.15f, 0.2f);
                    ServerManager.SendParticleTrail(center + new Vector3(0.9f, 0.65f, 0.9f), center + Vector3.up * 0.2f, 0.28f);
                    ServerManager.SendParticleTrail(center + new Vector3(-0.9f, 0.65f, 0.9f), center + Vector3.up * 0.2f, 0.28f);
                    ServerManager.SendParticleTrail(center + new Vector3(0.9f, 0.65f, -0.9f), center + Vector3.up * 0.2f, 0.28f);
                    ServerManager.SendParticleTrail(center + new Vector3(-0.9f, 0.65f, -0.9f), center + Vector3.up * 0.2f, 0.28f);
                    break;
            }
        }

    }

    [ModLoader.ModManager]
    public static class Mana
    {
        public static ItemTypesServer.ItemTypeRaw Item { get; private set; }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AddItemTypes, GameLoader.NAMESPACE + ".Items.Mana.Add")]
        [ModLoader.ModCallbackDependsOn("pipliz.server.applymoditempatches")]
        public static void Add(Dictionary<string, ItemTypesServer.ItemTypeRaw> items)
        {
            Item = MagicAlchemyUtility.AddItem(items, "Mana", "bluebottle.png", "ingredient", "magic");
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AfterItemTypesDefined, GameLoader.NAMESPACE + ".Items.Mana.Register")]
        [ModLoader.ModCallbackProvidesFor("pipliz.server.loadresearchables")]
        public static void Register()
        {
            var herbs = new InventoryItem(GameLoader.NAMESPACE + ".Herbs", 2);

            MagicAlchemyUtility.RegisterRecipe(
                VanillaJobs.Alchemist,
                Item.name,
                new List<InventoryItem>
                {
                    herbs,
                    new InventoryItem("potwater", 1),
                    new InventoryItem("plaster", 1)
                },
                new List<RecipeResult>
                {
                    new RecipeResult(Item.ItemIndex, 2)
                },
                50);
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnPlayerClicked, GameLoader.NAMESPACE + ".Items.Mana.OnPlayerClicked")]
        public static void OnPlayerClicked(Players.Player player, PlayerClickedData playerClickData)
        {
            if (playerClickData.TypeSelected != Item.ItemIndex ||
                playerClickData.ClickType != PlayerClickedData.EClickType.Right)
            {
                return;
            }

            playerClickData.ConsumedType = PlayerClickedData.EConsumedType.ChangedBlock;

            if (!PlayerMagicStateManager.TryConsumeManaBottle(player))
                PlayerMagicStateManager.ShowMissingItemIndicator(player, Item.ItemIndex);
        }
    }

    [ModLoader.ModManager]
    public static class Esper
    {
        public static ItemTypesServer.ItemTypeRaw Item { get; private set; }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AddItemTypes, GameLoader.NAMESPACE + ".Items.Esper.Add")]
        [ModLoader.ModCallbackDependsOn("pipliz.server.applymoditempatches")]
        public static void Add(Dictionary<string, ItemTypesServer.ItemTypeRaw> items)
        {
            Item = MagicAlchemyUtility.AddItem(items, "Esper", "purplebottle.png", "ingredient", "magic");
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnNPCCraftedRecipe, GameLoader.NAMESPACE + ".Items.Esper.OnNPCCraftedRecipe")]
        public static void OnNPCCraftedRecipe(IJob job, Recipe recipe, List<RecipeResult> results)
        {
            if (recipe.Name == Mana.Item.name && Pipliz.Random.NextFloat() <= 0.05f)
                results.Add(new RecipeResult(Item.ItemIndex, 1));
        }
    }

    [ModLoader.ModManager]
    public static class Aether
    {
        public static ItemTypesServer.ItemTypeRaw Item { get; private set; }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AddItemTypes, GameLoader.NAMESPACE + ".Items.Aether.Add")]
        [ModLoader.ModCallbackDependsOn("pipliz.server.applymoditempatches")]
        public static void Add(Dictionary<string, ItemTypesServer.ItemTypeRaw> items)
        {
            Item = MagicAlchemyUtility.AddItem(items, "Aether", "redbottle.png", "ingredient", "magic");
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AfterItemTypesDefined, GameLoader.NAMESPACE + ".Items.Aether.Register")]
        [ModLoader.ModCallbackProvidesFor("pipliz.server.loadresearchables")]
        public static void Register()
        {
            MagicAlchemyUtility.RegisterRecipe(
                VanillaJobs.Alchemist,
                Item.name,
                new List<InventoryItem>
                {
                    new InventoryItem(Mana.Item.ItemIndex, 1),
                    new InventoryItem("glasspiece", 3)
                },
                new List<RecipeResult>
                {
                    new RecipeResult(Item.ItemIndex, 1)
                },
                20);
        }
    }

    [ModLoader.ModManager]
    public static class Elementium
    {
        public static ItemTypesServer.ItemTypeRaw Item { get; private set; }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AddItemTypes, GameLoader.NAMESPACE + ".Items.Elementium.Add")]
        [ModLoader.ModCallbackDependsOn("pipliz.server.applymoditempatches")]
        public static void Add(Dictionary<string, ItemTypesServer.ItemTypeRaw> items)
        {
            Item = MagicAlchemyUtility.AddItem(items, "Elementium", "Elementium.png", "ingredient", "magic");
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AfterItemTypesDefined, GameLoader.NAMESPACE + ".Items.Elementium.Register")]
        [ModLoader.ModCallbackProvidesFor("pipliz.server.loadresearchables")]
        public static void Register()
        {
            MagicAlchemyUtility.RegisterRecipe(
                VanillaJobs.Alchemist,
                Item.name,
                new List<InventoryItem>
                {
                    new InventoryItem(Aether.Item.ItemIndex, 1),
                    new InventoryItem("copper", 20),
                    new InventoryItem("ironore", 20),
                    new InventoryItem("tin", 20),
                    new InventoryItem("goldore", 10)
                },
                new List<RecipeResult>
                {
                    new RecipeResult(Item.ItemIndex, 1)
                },
                10);
        }
    }

    [ModLoader.ModManager]
    public static class Void
    {
        public static ItemTypesServer.ItemTypeRaw Item { get; private set; }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AddItemTypes, GameLoader.NAMESPACE + ".Items.Void.Add")]
        [ModLoader.ModCallbackDependsOn("pipliz.server.applymoditempatches")]
        public static void Add(Dictionary<string, ItemTypesServer.ItemTypeRaw> items)
        {
            Item = MagicAlchemyUtility.AddItem(items, "Void", "void.png", "ingredient", "magic");
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnNPCCraftedRecipe, GameLoader.NAMESPACE + ".Items.Void.OnNPCCraftedRecipe")]
        public static void OnNPCCraftedRecipe(IJob job, Recipe recipe, List<RecipeResult> results)
        {
            if (recipe.Name == Elementium.Item.name && Pipliz.Random.NextFloat() <= 0.03f)
                results.Add(new RecipeResult(Item.ItemIndex, 1));
        }
    }

    [ModLoader.ModManager]
    public static class AirStone
    {
        public static ItemTypesServer.ItemTypeRaw Item { get; private set; }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AddItemTypes, GameLoader.NAMESPACE + ".Items.AirStone.Add")]
        [ModLoader.ModCallbackDependsOn("pipliz.server.applymoditempatches")]
        public static void Add(Dictionary<string, ItemTypesServer.ItemTypeRaw> items)
        {
            Item = MagicAlchemyUtility.AddItem(items, "AirStone", "Airstone.png", "ingredient", "magic", "stone");
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AfterItemTypesDefined, GameLoader.NAMESPACE + ".Items.AirStone.Register")]
        [ModLoader.ModCallbackProvidesFor("pipliz.server.loadresearchables")]
        public static void Register()
        {
            MagicAlchemyUtility.RegisterRecipe(
                VanillaJobs.Alchemist,
                Item.name,
                new List<InventoryItem>
                {
                    new InventoryItem(Elementium.Item.ItemIndex, 1),
                    new InventoryItem("copperarrow", 20)
                },
                new List<RecipeResult>
                {
                    new RecipeResult(Item.ItemIndex, 1)
                },
                6);
        }
    }

    [ModLoader.ModManager]
    public static class EarthStone
    {
        public static ItemTypesServer.ItemTypeRaw Item { get; private set; }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AddItemTypes, GameLoader.NAMESPACE + ".Items.EarthStone.Add")]
        [ModLoader.ModCallbackDependsOn("pipliz.server.applymoditempatches")]
        public static void Add(Dictionary<string, ItemTypesServer.ItemTypeRaw> items)
        {
            Item = MagicAlchemyUtility.AddItem(items, "EarthStone", "Earthstone.png", "ingredient", "magic", "stone");
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AfterItemTypesDefined, GameLoader.NAMESPACE + ".Items.EarthStone.Register")]
        [ModLoader.ModCallbackProvidesFor("pipliz.server.loadresearchables")]
        public static void Register()
        {
            MagicAlchemyUtility.RegisterRecipe(
                VanillaJobs.Alchemist,
                Item.name,
                new List<InventoryItem>
                {
                    new InventoryItem(Elementium.Item.ItemIndex, 1),
                    new InventoryItem("stonebricks", 50)
                },
                new List<RecipeResult>
                {
                    new RecipeResult(Item.ItemIndex, 1)
                },
                6);
        }
    }

    [ModLoader.ModManager]
    public static class FireStone
    {
        public static ItemTypesServer.ItemTypeRaw Item { get; private set; }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AddItemTypes, GameLoader.NAMESPACE + ".Items.FireStone.Add")]
        [ModLoader.ModCallbackDependsOn("pipliz.server.applymoditempatches")]
        public static void Add(Dictionary<string, ItemTypesServer.ItemTypeRaw> items)
        {
            Item = MagicAlchemyUtility.AddItem(items, "FireStone", "Firestone.png", "ingredient", "magic", "stone");
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AfterItemTypesDefined, GameLoader.NAMESPACE + ".Items.FireStone.Register")]
        [ModLoader.ModCallbackProvidesFor("pipliz.server.loadresearchables")]
        public static void Register()
        {
            MagicAlchemyUtility.RegisterRecipe(
                VanillaJobs.Alchemist,
                Item.name,
                new List<InventoryItem>
                {
                    new InventoryItem(Elementium.Item.ItemIndex, 1),
                    new InventoryItem("torch", 20)
                },
                new List<RecipeResult>
                {
                    new RecipeResult(Item.ItemIndex, 1)
                },
                6);
        }
    }

    [ModLoader.ModManager]
    public static class WaterStone
    {
        public static ItemTypesServer.ItemTypeRaw Item { get; private set; }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AddItemTypes, GameLoader.NAMESPACE + ".Items.WaterStone.Add")]
        [ModLoader.ModCallbackDependsOn("pipliz.server.applymoditempatches")]
        public static void Add(Dictionary<string, ItemTypesServer.ItemTypeRaw> items)
        {
            Item = MagicAlchemyUtility.AddItem(items, "WaterStone", "Waterstone.png", "ingredient", "magic", "stone");
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AfterItemTypesDefined, GameLoader.NAMESPACE + ".Items.WaterStone.Register")]
        [ModLoader.ModCallbackProvidesFor("pipliz.server.loadresearchables")]
        public static void Register()
        {
            MagicAlchemyUtility.RegisterRecipe(
                VanillaJobs.Alchemist,
                Item.name,
                new List<InventoryItem>
                {
                    new InventoryItem(Elementium.Item.ItemIndex, 1),
                    new InventoryItem("potwater", 2)
                },
                new List<RecipeResult>
                {
                    new RecipeResult(Item.ItemIndex, 1)
                },
                6);
        }
    }

    [ModLoader.ModManager]
    public static class ManaWand
    {
        private const long PrimaryCooldownMs = 430;
        private const long PrimaryMinimumCooldownMs = 180;
        private const long OverchargeCooldownMs = 860;
        private const long OverchargeMinimumCooldownMs = 350;
        private const float PrimarySpellDamage = 48f;
        private const float OverchargeSpellDamage = 92f;
        private const float PrimaryProjectileForce = 9f;
        private const float OverchargeProjectileForce = 17f;
        private const float PrimaryRange = 30f;
        private const float OverchargeRange = 38f;
        private const float PrimaryAimAssistRadius = 0.8f;
        private const float OverchargeAimAssistRadius = 0.95f;
        private const float PrimaryMissTrailDuration = 0.28f;
        private const float OverchargeMissTrailDuration = 0.34f;
        private const int PrimaryManaCost = 2;
        private const int OverchargeManaCost = 5;
        private const float OverchargeChainRadius = 5.25f;
        private const int OverchargeChainTargets = 3;
        private const float OverchargeChainDamage = 42f;
        private const float OverchargeChainProjectileForce = 10f;

        private static readonly Dictionary<Players.Player, long> Cooldowns = new Dictionary<Players.Player, long>();

        public static ItemTypesServer.ItemTypeRaw Item { get; private set; }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AddItemTypes, GameLoader.NAMESPACE + ".Items.ManaWand.Add")]
        [ModLoader.ModCallbackDependsOn("pipliz.server.applymoditempatches")]
        public static void Add(Dictionary<string, ItemTypesServer.ItemTypeRaw> items)
        {
            Item = MagicAlchemyUtility.AddWandItem(items, "ManaWand", "ManaWand.png", "magic", "wand", "weapon", GameLoader.NAMESPACE);
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AfterItemTypesDefined, GameLoader.NAMESPACE + ".Items.ManaWand.Register")]
        [ModLoader.ModCallbackProvidesFor("pipliz.server.loadresearchables")]
        public static void Register()
        {
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnPlayerClicked, GameLoader.NAMESPACE + ".Items.ManaWand.OnPlayerClicked")]
        public static void OnPlayerClicked(Players.Player player, PlayerClickedData playerClickData)
        {
            if (!WandCombatUtility.TryPrepareWandClick(playerClickData, Item.ItemIndex, LegendaryWandItems.GetItemIndexOrDefault(PlayerMagicStateManager.WandMasteryKeys.Mana)))
                return;

            if (!WandCombatUtility.IsReady(player, Cooldowns))
                return;

            var isOvercharge = playerClickData.ClickType == PlayerClickedData.EClickType.Right;
            if (isOvercharge)
            {
                WandCombatUtility.TryCastBurstMonsterSpell(
                    player,
                    playerClickData,
                    Cooldowns,
                    OverchargeManaCost,
                    OverchargeCooldownMs,
                    OverchargeMinimumCooldownMs,
                    OverchargeSpellDamage,
                    OverchargeProjectileForce,
                    OverchargeRange,
                    OverchargeAimAssistRadius,
                    OverchargeMissTrailDuration,
                    0.22f,
                    0.36f,
                    0f,
                    0,
                    0f,
                    OverchargeChainRadius,
                    OverchargeChainTargets,
                    OverchargeChainDamage,
                    OverchargeChainProjectileForce,
                    0,
                    0f,
                    0f,
                    0f,
                    WandCombatUtility.BurstVisualStyle.ManaPulse,
                    Item.ItemIndex,
                    Mana.Item.ItemIndex,
                    WandCastAudioManager.ManaHeavyCast,
                    WandCastAudioManager.ManaHeavyImpact,
                    masteryKey: PlayerMagicStateManager.WandMasteryKeys.Mana);
                return;
            }

            WandCombatUtility.TryCastMonsterSpell(
                player,
                playerClickData,
                Cooldowns,
                PrimaryManaCost,
                PrimaryCooldownMs,
                PrimaryMinimumCooldownMs,
                PrimarySpellDamage,
                PrimaryProjectileForce,
                PrimaryRange,
                PrimaryAimAssistRadius,
                PrimaryMissTrailDuration,
                0.22f,
                0.36f,
                castIndicatorItemIndex: Item.ItemIndex,
                impactIndicatorItemIndex: Mana.Item.ItemIndex,
                castAudio: WandCastAudioManager.ManaCast,
                hitAudio: WandCastAudioManager.ManaImpact,
                impactVisualStyle: WandCombatUtility.BurstVisualStyle.ManaPulse,
                impactVisualRadius: 2.0f,
                masteryKey: PlayerMagicStateManager.WandMasteryKeys.Mana);
        }
    }

    [ModLoader.ModManager]
    public static class BriarWand
    {
        private const long PrimaryCooldownMs = 470;
        private const long PrimaryMinimumCooldownMs = 220;
        private const long BrambleCooldownMs = 860;
        private const long BrambleMinimumCooldownMs = 420;
        private const float PrimarySpellDamage = 40f;
        private const float BrambleSpellDamage = 70f;
        private const float PrimaryProjectileForce = 13f;
        private const float BrambleProjectileForce = 16f;
        private const float PrimaryRange = 32f;
        private const float BrambleRange = 36f;
        private const float PrimaryAimAssistRadius = 0.9f;
        private const float BrambleAimAssistRadius = 1.0f;
        private const float PrimaryMissTrailDuration = 0.28f;
        private const float BrambleMissTrailDuration = 0.33f;
        private const int PrimaryManaCost = 3;
        private const int BrambleManaCost = 5;
        private const float PrimaryDotDamagePerTick = 12f;
        private const int PrimaryDotTickCount = 3;
        private const float PrimaryDotTickDelaySeconds = 0.8f;
        private const float BrambleDotDamagePerTick = 14f;
        private const int BrambleDotTickCount = 4;
        private const float BrambleDotTickDelaySeconds = 0.75f;
        private const float BrambleBurstRadius = 4.8f;
        private const int BrambleBurstTargets = 4;
        private const float BrambleBurstDamage = 24f;
        private const float BrambleBurstProjectileForce = 14f;

        private static readonly Dictionary<Players.Player, long> Cooldowns = new Dictionary<Players.Player, long>();

        public static ItemTypesServer.ItemTypeRaw Item { get; private set; }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AddItemTypes, GameLoader.NAMESPACE + ".Items.BriarWand.Add")]
        [ModLoader.ModCallbackDependsOn("pipliz.server.applymoditempatches")]
        public static void Add(Dictionary<string, ItemTypesServer.ItemTypeRaw> items)
        {
            Item = MagicAlchemyUtility.AddWandItem(items, "BriarWand", "BriarWand.png", "magic", "wand", "weapon", GameLoader.NAMESPACE);
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AfterItemTypesDefined, GameLoader.NAMESPACE + ".Items.BriarWand.Register")]
        [ModLoader.ModCallbackProvidesFor("pipliz.server.loadresearchables")]
        public static void Register()
        {
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnPlayerClicked, GameLoader.NAMESPACE + ".Items.BriarWand.OnPlayerClicked")]
        public static void OnPlayerClicked(Players.Player player, PlayerClickedData playerClickData)
        {
            if (!WandCombatUtility.TryPrepareWandClick(playerClickData, Item.ItemIndex, LegendaryWandItems.GetItemIndexOrDefault(PlayerMagicStateManager.WandMasteryKeys.Briar)))
                return;

            if (!WandCombatUtility.IsReady(player, Cooldowns))
                return;

            var isBramble = playerClickData.ClickType == PlayerClickedData.EClickType.Right;
            if (isBramble)
            {
                WandCombatUtility.TryCastBurstMonsterSpell(
                    player,
                    playerClickData,
                    Cooldowns,
                    BrambleManaCost,
                    BrambleCooldownMs,
                    BrambleMinimumCooldownMs,
                    BrambleSpellDamage,
                    BrambleProjectileForce,
                    BrambleRange,
                    BrambleAimAssistRadius,
                    BrambleMissTrailDuration,
                    0.23f,
                    0.36f,
                    BrambleDotDamagePerTick,
                    BrambleDotTickCount,
                    BrambleDotTickDelaySeconds,
                    BrambleBurstRadius,
                    BrambleBurstTargets,
                    BrambleBurstDamage,
                    BrambleBurstProjectileForce,
                    0,
                    0f,
                    0f,
                    0f,
                    WandCombatUtility.BurstVisualStyle.Bramble,
                    Item.ItemIndex,
                    Aether.Item.ItemIndex,
                    WandCastAudioManager.BriarHeavyCast,
                    WandCastAudioManager.BriarHeavyImpact,
                    true,
                    false,
                    PlayerMagicStateManager.WandMasteryKeys.Briar);
                return;
            }

            WandCombatUtility.TryCastMonsterSpell(
                player,
                playerClickData,
                Cooldowns,
                PrimaryManaCost,
                PrimaryCooldownMs,
                PrimaryMinimumCooldownMs,
                PrimarySpellDamage,
                PrimaryProjectileForce,
                PrimaryRange,
                PrimaryAimAssistRadius,
                PrimaryMissTrailDuration,
                0.22f,
                0.32f,
                PrimaryDotDamagePerTick,
                PrimaryDotTickCount,
                PrimaryDotTickDelaySeconds,
                castIndicatorItemIndex: Item.ItemIndex,
                impactIndicatorItemIndex: Aether.Item.ItemIndex,
                castAudio: WandCastAudioManager.BriarCast,
                hitAudio: WandCastAudioManager.BriarImpact,
                persistentDamageOverTime: true,
                impactVisualStyle: WandCombatUtility.BurstVisualStyle.Bramble,
                impactVisualRadius: 2.1f,
                masteryKey: PlayerMagicStateManager.WandMasteryKeys.Briar);
        }
    }

    [ModLoader.ModManager]
    public static class SparkWand
    {
        private const long PrimaryCooldownMs = 360;
        private const long PrimaryMinimumCooldownMs = 160;
        private const long SurgeCooldownMs = 760;
        private const long SurgeMinimumCooldownMs = 320;
        private const float PrimarySpellDamage = 36f;
        private const float SurgeSpellDamage = 72f;
        private const float PrimaryProjectileForce = 10f;
        private const float SurgeProjectileForce = 15f;
        private const float PrimaryRange = 32f;
        private const float SurgeRange = 38f;
        private const float PrimaryAimAssistRadius = 0.9f;
        private const float SurgeAimAssistRadius = 1.0f;
        private const float PrimaryMissTrailDuration = 0.27f;
        private const float SurgeMissTrailDuration = 0.32f;
        private const int PrimaryManaCost = 2;
        private const int SurgeManaCost = 4;
        private const float PrimaryChainRadius = 4.25f;
        private const int PrimaryChainTargets = 1;
        private const float PrimaryChainDamage = 16f;
        private const float PrimaryChainProjectileForce = 9f;
        private const float SurgeChainRadius = 6f;
        private const int SurgeChainTargets = 4;
        private const float SurgeChainDamage = 26f;
        private const float SurgeChainProjectileForce = 12f;

        private static readonly Dictionary<Players.Player, long> Cooldowns = new Dictionary<Players.Player, long>();

        public static ItemTypesServer.ItemTypeRaw Item { get; private set; }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AddItemTypes, GameLoader.NAMESPACE + ".Items.SparkWand.Add")]
        [ModLoader.ModCallbackDependsOn("pipliz.server.applymoditempatches")]
        public static void Add(Dictionary<string, ItemTypesServer.ItemTypeRaw> items)
        {
            Item = MagicAlchemyUtility.AddWandItem(items, "SparkWand", "SparkWand.png", "magic", "wand", "weapon", GameLoader.NAMESPACE);
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AfterItemTypesDefined, GameLoader.NAMESPACE + ".Items.SparkWand.Register")]
        [ModLoader.ModCallbackProvidesFor("pipliz.server.loadresearchables")]
        public static void Register()
        {
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnPlayerClicked, GameLoader.NAMESPACE + ".Items.SparkWand.OnPlayerClicked")]
        public static void OnPlayerClicked(Players.Player player, PlayerClickedData playerClickData)
        {
            if (!WandCombatUtility.TryPrepareWandClick(playerClickData, Item.ItemIndex, LegendaryWandItems.GetItemIndexOrDefault(PlayerMagicStateManager.WandMasteryKeys.Spark)))
                return;

            if (!WandCombatUtility.IsReady(player, Cooldowns))
                return;

            var isSurge = playerClickData.ClickType == PlayerClickedData.EClickType.Right;
            if (isSurge)
            {
                WandCombatUtility.TryCastBurstMonsterSpell(
                    player,
                    playerClickData,
                    Cooldowns,
                    SurgeManaCost,
                    SurgeCooldownMs,
                    SurgeMinimumCooldownMs,
                    SurgeSpellDamage,
                    SurgeProjectileForce,
                    SurgeRange,
                    SurgeAimAssistRadius,
                    SurgeMissTrailDuration,
                    0.22f,
                    0.34f,
                    0f,
                    0,
                    0f,
                    SurgeChainRadius,
                    SurgeChainTargets,
                    SurgeChainDamage,
                    SurgeChainProjectileForce,
                    0,
                    0f,
                    0f,
                    0f,
                    WandCombatUtility.BurstVisualStyle.SparkArc,
                    Item.ItemIndex,
                    AirStone.Item.ItemIndex,
                    WandCastAudioManager.SparkHeavyCast,
                    WandCastAudioManager.SparkHeavyImpact,
                    masteryKey: PlayerMagicStateManager.WandMasteryKeys.Spark);
                return;
            }

            WandCombatUtility.TryCastBurstMonsterSpell(
                player,
                playerClickData,
                Cooldowns,
                PrimaryManaCost,
                PrimaryCooldownMs,
                PrimaryMinimumCooldownMs,
                PrimarySpellDamage,
                PrimaryProjectileForce,
                PrimaryRange,
                PrimaryAimAssistRadius,
                PrimaryMissTrailDuration,
                0.2f,
                0.3f,
                0f,
                0,
                0f,
                PrimaryChainRadius,
                PrimaryChainTargets,
                PrimaryChainDamage,
                PrimaryChainProjectileForce,
                0,
                0f,
                0f,
                0f,
                WandCombatUtility.BurstVisualStyle.SparkArc,
                Item.ItemIndex,
                AirStone.Item.ItemIndex,
                WandCastAudioManager.SparkCast,
                WandCastAudioManager.SparkImpact,
                masteryKey: PlayerMagicStateManager.WandMasteryKeys.Spark);
        }
    }

    [ModLoader.ModManager]
    public static class VenomWand
    {
        private const long PrimaryCooldownMs = 610;
        private const long PrimaryMinimumCooldownMs = 280;
        private const long BloomCooldownMs = 980;
        private const long BloomMinimumCooldownMs = 460;
        private const float PrimarySpellDamage = 34f;
        private const float BloomSpellDamage = 62f;
        private const float PrimaryProjectileForce = 9f;
        private const float BloomProjectileForce = 11f;
        private const float PrimaryRange = 34f;
        private const float BloomRange = 38f;
        private const float PrimaryAimAssistRadius = 0.95f;
        private const float BloomAimAssistRadius = 1.05f;
        private const float PrimaryMissTrailDuration = 0.3f;
        private const float BloomMissTrailDuration = 0.34f;
        private const int PrimaryManaCost = 3;
        private const int BloomManaCost = 6;
        private const float PrimaryDotDamagePerTick = 18f;
        private const int PrimaryDotTickCount = 5;
        private const float PrimaryDotTickDelaySeconds = 0.9f;
        private const float BloomDotDamagePerTick = 20f;
        private const int BloomDotTickCount = 6;
        private const float BloomDotTickDelaySeconds = 0.85f;
        private const float BloomRadius = 5.5f;
        private const int BloomTargets = 5;
        private const float BloomSplashDamage = 22f;
        private const float BloomSplashProjectileForce = 10f;
        private const float BloomSplashDotDamagePerTick = 12f;
        private const int BloomSplashDotTickCount = 4;
        private const float BloomSplashDotTickDelaySeconds = 0.85f;

        private static readonly Dictionary<Players.Player, long> Cooldowns = new Dictionary<Players.Player, long>();

        public static ItemTypesServer.ItemTypeRaw Item { get; private set; }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AddItemTypes, GameLoader.NAMESPACE + ".Items.VenomWand.Add")]
        [ModLoader.ModCallbackDependsOn("pipliz.server.applymoditempatches")]
        public static void Add(Dictionary<string, ItemTypesServer.ItemTypeRaw> items)
        {
            Item = MagicAlchemyUtility.AddWandItem(items, "VenomWand", "VenomWand.png", "magic", "wand", "weapon", GameLoader.NAMESPACE);
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AfterItemTypesDefined, GameLoader.NAMESPACE + ".Items.VenomWand.Register")]
        [ModLoader.ModCallbackProvidesFor("pipliz.server.loadresearchables")]
        public static void Register()
        {
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnPlayerClicked, GameLoader.NAMESPACE + ".Items.VenomWand.OnPlayerClicked")]
        public static void OnPlayerClicked(Players.Player player, PlayerClickedData playerClickData)
        {
            if (!WandCombatUtility.TryPrepareWandClick(playerClickData, Item.ItemIndex, LegendaryWandItems.GetItemIndexOrDefault(PlayerMagicStateManager.WandMasteryKeys.Venom)))
                return;

            if (!WandCombatUtility.IsReady(player, Cooldowns))
                return;

            var isBloom = playerClickData.ClickType == PlayerClickedData.EClickType.Right;
            if (isBloom)
            {
                WandCombatUtility.TryCastBurstMonsterSpell(
                    player,
                    playerClickData,
                    Cooldowns,
                    BloomManaCost,
                    BloomCooldownMs,
                    BloomMinimumCooldownMs,
                    BloomSpellDamage,
                    BloomProjectileForce,
                    BloomRange,
                    BloomAimAssistRadius,
                    BloomMissTrailDuration,
                    0.24f,
                    0.36f,
                    BloomDotDamagePerTick,
                    BloomDotTickCount,
                    BloomDotTickDelaySeconds,
                    BloomRadius,
                    BloomTargets,
                    BloomSplashDamage,
                    BloomSplashProjectileForce,
                    BloomSplashDotTickCount,
                    BloomSplashDotDamagePerTick,
                    BloomSplashDotTickDelaySeconds,
                    0f,
                    WandCombatUtility.BurstVisualStyle.ToxicBloom,
                    Item.ItemIndex,
                    Aether.Item.ItemIndex,
                    WandCastAudioManager.VenomHeavyCast,
                    WandCastAudioManager.VenomHeavyImpact,
                    true,
                    true,
                    PlayerMagicStateManager.WandMasteryKeys.Venom);
                return;
            }

            WandCombatUtility.TryCastMonsterSpell(
                player,
                playerClickData,
                Cooldowns,
                PrimaryManaCost,
                PrimaryCooldownMs,
                PrimaryMinimumCooldownMs,
                PrimarySpellDamage,
                PrimaryProjectileForce,
                PrimaryRange,
                PrimaryAimAssistRadius,
                PrimaryMissTrailDuration,
                0.22f,
                0.34f,
                PrimaryDotDamagePerTick,
                PrimaryDotTickCount,
                PrimaryDotTickDelaySeconds,
                castIndicatorItemIndex: Item.ItemIndex,
                impactIndicatorItemIndex: Aether.Item.ItemIndex,
                castAudio: WandCastAudioManager.VenomCast,
                hitAudio: WandCastAudioManager.VenomImpact,
                persistentDamageOverTime: true,
                impactVisualStyle: WandCombatUtility.BurstVisualStyle.ToxicBloom,
                impactVisualRadius: 2.4f,
                masteryKey: PlayerMagicStateManager.WandMasteryKeys.Venom);
        }
    }

    [ModLoader.ModManager]
    public static class EmberWand
    {
        private const long PrimaryCooldownMs = 620;
        private const long PrimaryMinimumCooldownMs = 280;
        private const long InfernoCooldownMs = 980;
        private const long InfernoMinimumCooldownMs = 460;
        private const float PrimarySpellDamage = 46f;
        private const float InfernoSpellDamage = 82f;
        private const float PrimaryProjectileForce = 10f;
        private const float InfernoProjectileForce = 14f;
        private const float PrimaryRange = 34f;
        private const float InfernoRange = 38f;
        private const float PrimaryAimAssistRadius = 0.95f;
        private const float InfernoAimAssistRadius = 1.05f;
        private const float PrimaryMissTrailDuration = 0.3f;
        private const float InfernoMissTrailDuration = 0.34f;
        private const int PrimaryManaCost = 3;
        private const int InfernoManaCost = 6;
        private const float BurnDamagePerTick = 24f;
        private const int BurnTickCount = 4;
        private const float BurnTickDelaySeconds = 1f;
        private const float InfernoBurnDamagePerTick = 28f;
        private const int InfernoBurnTickCount = 6;
        private const float InfernoBurnTickDelaySeconds = 0.8f;
        private const float InfernoSplashRadius = 5.5f;
        private const int InfernoSplashTargets = 5;
        private const float InfernoSplashDamage = 46f;
        private const float InfernoSplashProjectileForce = 12f;
        private const float InfernoSplashBurnDamagePerTick = 18f;
        private const int InfernoSplashBurnTickCount = 4;
        private const float InfernoSplashBurnTickDelaySeconds = 0.9f;

        private static readonly Dictionary<Players.Player, long> Cooldowns = new Dictionary<Players.Player, long>();

        public static ItemTypesServer.ItemTypeRaw Item { get; private set; }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AddItemTypes, GameLoader.NAMESPACE + ".Items.EmberWand.Add")]
        [ModLoader.ModCallbackDependsOn("pipliz.server.applymoditempatches")]
        public static void Add(Dictionary<string, ItemTypesServer.ItemTypeRaw> items)
        {
            Item = MagicAlchemyUtility.AddWandItem(items, "EmberWand", "EmberWand.png", "magic", "wand", "weapon", GameLoader.NAMESPACE);
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AfterItemTypesDefined, GameLoader.NAMESPACE + ".Items.EmberWand.Register")]
        [ModLoader.ModCallbackProvidesFor("pipliz.server.loadresearchables")]
        public static void Register()
        {
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnPlayerClicked, GameLoader.NAMESPACE + ".Items.EmberWand.OnPlayerClicked")]
        public static void OnPlayerClicked(Players.Player player, PlayerClickedData playerClickData)
        {
            if (!WandCombatUtility.TryPrepareWandClick(playerClickData, Item.ItemIndex, LegendaryWandItems.GetItemIndexOrDefault(PlayerMagicStateManager.WandMasteryKeys.Ember)))
                return;

            if (!WandCombatUtility.IsReady(player, Cooldowns))
                return;

            var isInferno = playerClickData.ClickType == PlayerClickedData.EClickType.Right;
            if (isInferno)
            {
                WandCombatUtility.TryCastBurstMonsterSpell(
                    player,
                    playerClickData,
                    Cooldowns,
                    InfernoManaCost,
                    InfernoCooldownMs,
                    InfernoMinimumCooldownMs,
                    InfernoSpellDamage,
                    InfernoProjectileForce,
                    InfernoRange,
                    InfernoAimAssistRadius,
                    InfernoMissTrailDuration,
                    0.24f,
                    0.38f,
                    InfernoBurnDamagePerTick,
                    InfernoBurnTickCount,
                    InfernoBurnTickDelaySeconds,
                    InfernoSplashRadius,
                    InfernoSplashTargets,
                    InfernoSplashDamage,
                    InfernoSplashProjectileForce,
                    InfernoSplashBurnTickCount,
                    InfernoSplashBurnDamagePerTick,
                    InfernoSplashBurnTickDelaySeconds,
                    0f,
                    WandCombatUtility.BurstVisualStyle.Inferno,
                    Item.ItemIndex,
                    FireStone.Item.ItemIndex,
                    WandCastAudioManager.EmberHeavyCast,
                    WandCastAudioManager.EmberHeavyImpact,
                    true,
                    true,
                    PlayerMagicStateManager.WandMasteryKeys.Ember);
                return;
            }

            WandCombatUtility.TryCastMonsterSpell(
                player,
                playerClickData,
                Cooldowns,
                PrimaryManaCost,
                PrimaryCooldownMs,
                PrimaryMinimumCooldownMs,
                PrimarySpellDamage,
                PrimaryProjectileForce,
                PrimaryRange,
                PrimaryAimAssistRadius,
                PrimaryMissTrailDuration,
                0.24f,
                0.38f,
                BurnDamagePerTick,
                BurnTickCount,
                BurnTickDelaySeconds,
                castIndicatorItemIndex: Item.ItemIndex,
                impactIndicatorItemIndex: FireStone.Item.ItemIndex,
                castAudio: WandCastAudioManager.EmberCast,
                hitAudio: WandCastAudioManager.EmberImpact,
                persistentDamageOverTime: true,
                impactVisualStyle: WandCombatUtility.BurstVisualStyle.Inferno,
                impactVisualRadius: 2.5f,
                masteryKey: PlayerMagicStateManager.WandMasteryKeys.Ember);
        }
    }

    [ModLoader.ModManager]
    public static class FrostWand
    {
        private const long PrimaryCooldownMs = 520;
        private const long PrimaryMinimumCooldownMs = 220;
        private const long ShatterCooldownMs = 900;
        private const long ShatterMinimumCooldownMs = 420;
        private const float PrimarySpellDamage = 42f;
        private const float ShatterSpellDamage = 74f;
        private const float PrimaryProjectileForce = 32f;
        private const float ShatterProjectileForce = 46f;
        private const float PrimaryRange = 34f;
        private const float ShatterRange = 38f;
        private const float PrimaryAimAssistRadius = 0.85f;
        private const float ShatterAimAssistRadius = 0.95f;
        private const float PrimaryMissTrailDuration = 0.28f;
        private const float ShatterMissTrailDuration = 0.32f;
        private const int PrimaryManaCost = 3;
        private const int ShatterManaCost = 6;
        private const float ShatterBurstRadius = 4.75f;
        private const int ShatterBurstTargets = 6;
        private const float ShatterBurstDamage = 42f;
        private const float ShatterBurstProjectileForce = 42f;

        private static readonly Dictionary<Players.Player, long> Cooldowns = new Dictionary<Players.Player, long>();

        public static ItemTypesServer.ItemTypeRaw Item { get; private set; }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AddItemTypes, GameLoader.NAMESPACE + ".Items.FrostWand.Add")]
        [ModLoader.ModCallbackDependsOn("pipliz.server.applymoditempatches")]
        public static void Add(Dictionary<string, ItemTypesServer.ItemTypeRaw> items)
        {
            Item = MagicAlchemyUtility.AddWandItem(items, "FrostWand", "FrostWand.png", "magic", "wand", "weapon", GameLoader.NAMESPACE);
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AfterItemTypesDefined, GameLoader.NAMESPACE + ".Items.FrostWand.Register")]
        [ModLoader.ModCallbackProvidesFor("pipliz.server.loadresearchables")]
        public static void Register()
        {
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnPlayerClicked, GameLoader.NAMESPACE + ".Items.FrostWand.OnPlayerClicked")]
        public static void OnPlayerClicked(Players.Player player, PlayerClickedData playerClickData)
        {
            if (!WandCombatUtility.TryPrepareWandClick(playerClickData, Item.ItemIndex, LegendaryWandItems.GetItemIndexOrDefault(PlayerMagicStateManager.WandMasteryKeys.Frost)))
                return;

            if (!WandCombatUtility.IsReady(player, Cooldowns))
                return;

            var isShatter = playerClickData.ClickType == PlayerClickedData.EClickType.Right;
            if (isShatter)
            {
                WandCombatUtility.TryCastBurstMonsterSpell(
                    player,
                    playerClickData,
                    Cooldowns,
                    ShatterManaCost,
                    ShatterCooldownMs,
                    ShatterMinimumCooldownMs,
                    ShatterSpellDamage,
                    ShatterProjectileForce,
                    ShatterRange,
                    ShatterAimAssistRadius,
                    ShatterMissTrailDuration,
                    0.22f,
                    0.34f,
                    0f,
                    0,
                    0f,
                    ShatterBurstRadius,
                    ShatterBurstTargets,
                    ShatterBurstDamage,
                    ShatterBurstProjectileForce,
                    0,
                    0f,
                    0f,
                    0f,
                    WandCombatUtility.BurstVisualStyle.Shatter,
                    Item.ItemIndex,
                    WaterStone.Item.ItemIndex,
                    WandCastAudioManager.FrostHeavyCast,
                    WandCastAudioManager.FrostHeavyImpact,
                    masteryKey: PlayerMagicStateManager.WandMasteryKeys.Frost);
                return;
            }

            WandCombatUtility.TryCastMonsterSpell(
                player,
                playerClickData,
                Cooldowns,
                PrimaryManaCost,
                PrimaryCooldownMs,
                PrimaryMinimumCooldownMs,
                PrimarySpellDamage,
                PrimaryProjectileForce,
                PrimaryRange,
                PrimaryAimAssistRadius,
                PrimaryMissTrailDuration,
                0.22f,
                0.34f,
                castIndicatorItemIndex: Item.ItemIndex,
                impactIndicatorItemIndex: WaterStone.Item.ItemIndex,
                castAudio: WandCastAudioManager.FrostCast,
                hitAudio: WandCastAudioManager.FrostImpact,
                impactVisualStyle: WandCombatUtility.BurstVisualStyle.Shatter,
                impactVisualRadius: 2.4f,
                masteryKey: PlayerMagicStateManager.WandMasteryKeys.Frost);
        }
    }

    [ModLoader.ModManager]
    public static class CrystalWand
    {
        private const long PrimaryCooldownMs = 540;
        private const long PrimaryMinimumCooldownMs = 240;
        private const long PrismCooldownMs = 940;
        private const long PrismMinimumCooldownMs = 440;
        private const float PrimarySpellDamage = 60f;
        private const float PrismSpellDamage = 98f;
        private const float PrimaryProjectileForce = 18f;
        private const float PrismProjectileForce = 24f;
        private const float PrimaryRange = 40f;
        private const float PrismRange = 42f;
        private const float PrimaryAimAssistRadius = 1.0f;
        private const float PrismAimAssistRadius = 1.1f;
        private const float PrimaryMissTrailDuration = 0.3f;
        private const float PrismMissTrailDuration = 0.36f;
        private const int PrimaryManaCost = 4;
        private const int PrismManaCost = 7;
        private const float PrismBurstRadius = 5.25f;
        private const int PrismBurstTargets = 5;
        private const float PrismBurstDamage = 34f;
        private const float PrismBurstProjectileForce = 16f;

        private static readonly Dictionary<Players.Player, long> Cooldowns = new Dictionary<Players.Player, long>();

        public static ItemTypesServer.ItemTypeRaw Item { get; private set; }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AddItemTypes, GameLoader.NAMESPACE + ".Items.CrystalWand.Add")]
        [ModLoader.ModCallbackDependsOn("pipliz.server.applymoditempatches")]
        public static void Add(Dictionary<string, ItemTypesServer.ItemTypeRaw> items)
        {
            Item = MagicAlchemyUtility.AddWandItem(items, "CrystalWand", "CrystalWand.png", "magic", "wand", "weapon", GameLoader.NAMESPACE);
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AfterItemTypesDefined, GameLoader.NAMESPACE + ".Items.CrystalWand.Register")]
        [ModLoader.ModCallbackProvidesFor("pipliz.server.loadresearchables")]
        public static void Register()
        {
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnPlayerClicked, GameLoader.NAMESPACE + ".Items.CrystalWand.OnPlayerClicked")]
        public static void OnPlayerClicked(Players.Player player, PlayerClickedData playerClickData)
        {
            if (!WandCombatUtility.TryPrepareWandClick(playerClickData, Item.ItemIndex, LegendaryWandItems.GetItemIndexOrDefault(PlayerMagicStateManager.WandMasteryKeys.Crystal)))
                return;

            if (!WandCombatUtility.IsReady(player, Cooldowns))
                return;

            var isPrism = playerClickData.ClickType == PlayerClickedData.EClickType.Right;
            if (isPrism)
            {
                WandCombatUtility.TryCastBurstMonsterSpell(
                    player,
                    playerClickData,
                    Cooldowns,
                    PrismManaCost,
                    PrismCooldownMs,
                    PrismMinimumCooldownMs,
                    PrismSpellDamage,
                    PrismProjectileForce,
                    PrismRange,
                    PrismAimAssistRadius,
                    PrismMissTrailDuration,
                    0.22f,
                    0.36f,
                    0f,
                    0,
                    0f,
                    PrismBurstRadius,
                    PrismBurstTargets,
                    PrismBurstDamage,
                    PrismBurstProjectileForce,
                    0,
                    0f,
                    0f,
                    0f,
                    WandCombatUtility.BurstVisualStyle.Prism,
                    Item.ItemIndex,
                    WaterStone.Item.ItemIndex,
                    WandCastAudioManager.CrystalHeavyCast,
                    WandCastAudioManager.CrystalHeavyImpact,
                    masteryKey: PlayerMagicStateManager.WandMasteryKeys.Crystal);
                return;
            }

            WandCombatUtility.TryCastMonsterSpell(
                player,
                playerClickData,
                Cooldowns,
                PrimaryManaCost,
                PrimaryCooldownMs,
                PrimaryMinimumCooldownMs,
                PrimarySpellDamage,
                PrimaryProjectileForce,
                PrimaryRange,
                PrimaryAimAssistRadius,
                PrimaryMissTrailDuration,
                0.22f,
                0.34f,
                castIndicatorItemIndex: Item.ItemIndex,
                impactIndicatorItemIndex: WaterStone.Item.ItemIndex,
                castAudio: WandCastAudioManager.CrystalCast,
                hitAudio: WandCastAudioManager.CrystalImpact,
                impactVisualStyle: WandCombatUtility.BurstVisualStyle.Prism,
                impactVisualRadius: 2.8f,
                masteryKey: PlayerMagicStateManager.WandMasteryKeys.Crystal);
        }
    }

    [ModLoader.ModManager]
    public static class StoneWand
    {
        private const long PrimaryCooldownMs = 720;
        private const long PrimaryMinimumCooldownMs = 340;
        private const long QuakeCooldownMs = 1050;
        private const long QuakeMinimumCooldownMs = 560;
        private const float PrimarySpellDamage = 68f;
        private const float QuakeSpellDamage = 108f;
        private const float PrimaryProjectileForce = 46f;
        private const float QuakeProjectileForce = 60f;
        private const float PrimaryRange = 32f;
        private const float QuakeRange = 34f;
        private const float PrimaryAimAssistRadius = 0.82f;
        private const float QuakeAimAssistRadius = 0.92f;
        private const float PrimaryMissTrailDuration = 0.28f;
        private const float QuakeMissTrailDuration = 0.32f;
        private const int PrimaryManaCost = 4;
        private const int QuakeManaCost = 7;
        private const float QuakeBurstRadius = 4.5f;
        private const int QuakeBurstTargets = 5;
        private const float QuakeBurstDamage = 46f;
        private const float QuakeBurstProjectileForce = 58f;

        private static readonly Dictionary<Players.Player, long> Cooldowns = new Dictionary<Players.Player, long>();

        public static ItemTypesServer.ItemTypeRaw Item { get; private set; }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AddItemTypes, GameLoader.NAMESPACE + ".Items.StoneWand.Add")]
        [ModLoader.ModCallbackDependsOn("pipliz.server.applymoditempatches")]
        public static void Add(Dictionary<string, ItemTypesServer.ItemTypeRaw> items)
        {
            Item = MagicAlchemyUtility.AddWandItem(items, "StoneWand", "StoneWand.png", "magic", "wand", "weapon", GameLoader.NAMESPACE);
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AfterItemTypesDefined, GameLoader.NAMESPACE + ".Items.StoneWand.Register")]
        [ModLoader.ModCallbackProvidesFor("pipliz.server.loadresearchables")]
        public static void Register()
        {
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnPlayerClicked, GameLoader.NAMESPACE + ".Items.StoneWand.OnPlayerClicked")]
        public static void OnPlayerClicked(Players.Player player, PlayerClickedData playerClickData)
        {
            if (!WandCombatUtility.TryPrepareWandClick(playerClickData, Item.ItemIndex, LegendaryWandItems.GetItemIndexOrDefault(PlayerMagicStateManager.WandMasteryKeys.Stone)))
                return;

            if (!WandCombatUtility.IsReady(player, Cooldowns))
                return;

            var isQuake = playerClickData.ClickType == PlayerClickedData.EClickType.Right;
            if (isQuake)
            {
                WandCombatUtility.TryCastBurstMonsterSpell(
                    player,
                    playerClickData,
                    Cooldowns,
                    QuakeManaCost,
                    QuakeCooldownMs,
                    QuakeMinimumCooldownMs,
                    QuakeSpellDamage,
                    QuakeProjectileForce,
                    QuakeRange,
                    QuakeAimAssistRadius,
                    QuakeMissTrailDuration,
                    0.22f,
                    0.34f,
                    0f,
                    0,
                    0f,
                    QuakeBurstRadius,
                    QuakeBurstTargets,
                    QuakeBurstDamage,
                    QuakeBurstProjectileForce,
                    0,
                    0f,
                    0f,
                    0f,
                    WandCombatUtility.BurstVisualStyle.Faultline,
                    Item.ItemIndex,
                    EarthStone.Item.ItemIndex,
                    WandCastAudioManager.StoneHeavyCast,
                    WandCastAudioManager.StoneHeavyImpact,
                    masteryKey: PlayerMagicStateManager.WandMasteryKeys.Stone);
                return;
            }

            WandCombatUtility.TryCastMonsterSpell(
                player,
                playerClickData,
                Cooldowns,
                PrimaryManaCost,
                PrimaryCooldownMs,
                PrimaryMinimumCooldownMs,
                PrimarySpellDamage,
                PrimaryProjectileForce,
                PrimaryRange,
                PrimaryAimAssistRadius,
                PrimaryMissTrailDuration,
                0.22f,
                0.32f,
                castIndicatorItemIndex: Item.ItemIndex,
                impactIndicatorItemIndex: EarthStone.Item.ItemIndex,
                castAudio: WandCastAudioManager.StoneCast,
                hitAudio: WandCastAudioManager.StoneImpact,
                impactVisualStyle: WandCombatUtility.BurstVisualStyle.Faultline,
                impactVisualRadius: 2.5f,
                masteryKey: PlayerMagicStateManager.WandMasteryKeys.Stone);
        }
    }

    [ModLoader.ModManager]
    public static class StormWand
    {
        private const long PrimaryCooldownMs = 560;
        private const long PrimaryMinimumCooldownMs = 240;
        private const long TempestCooldownMs = 920;
        private const long TempestMinimumCooldownMs = 420;
        private const float PrimarySpellDamage = 58f;
        private const float TempestSpellDamage = 94f;
        private const float PrimaryProjectileForce = 14f;
        private const float TempestProjectileForce = 18f;
        private const float PrimaryRange = 38f;
        private const float TempestRange = 42f;
        private const float PrimaryAimAssistRadius = 1.0f;
        private const float TempestAimAssistRadius = 1.1f;
        private const float PrimaryMissTrailDuration = 0.31f;
        private const float TempestMissTrailDuration = 0.36f;
        private const int PrimaryManaCost = 4;
        private const int TempestManaCost = 8;
        private const float PrimaryChainRadius = 5.5f;
        private const int PrimaryChainTargets = 2;
        private const float PrimaryChainDamage = 24f;
        private const float PrimaryChainProjectileForce = 10f;
        private const float TempestRadius = 7f;
        private const int TempestTargets = 6;
        private const float TempestChainDamage = 40f;
        private const float TempestChainProjectileForce = 14f;

        private static readonly Dictionary<Players.Player, long> Cooldowns = new Dictionary<Players.Player, long>();

        public static ItemTypesServer.ItemTypeRaw Item { get; private set; }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AddItemTypes, GameLoader.NAMESPACE + ".Items.StormWand.Add")]
        [ModLoader.ModCallbackDependsOn("pipliz.server.applymoditempatches")]
        public static void Add(Dictionary<string, ItemTypesServer.ItemTypeRaw> items)
        {
            Item = MagicAlchemyUtility.AddWandItem(items, "StormWand", "StormWand.png", "magic", "wand", "weapon", GameLoader.NAMESPACE);
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AfterItemTypesDefined, GameLoader.NAMESPACE + ".Items.StormWand.Register")]
        [ModLoader.ModCallbackProvidesFor("pipliz.server.loadresearchables")]
        public static void Register()
        {
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnPlayerClicked, GameLoader.NAMESPACE + ".Items.StormWand.OnPlayerClicked")]
        public static void OnPlayerClicked(Players.Player player, PlayerClickedData playerClickData)
        {
            if (!WandCombatUtility.TryPrepareWandClick(playerClickData, Item.ItemIndex, LegendaryWandItems.GetItemIndexOrDefault(PlayerMagicStateManager.WandMasteryKeys.Storm)))
                return;

            if (!WandCombatUtility.IsReady(player, Cooldowns))
                return;

            var isTempest = playerClickData.ClickType == PlayerClickedData.EClickType.Right;
            if (isTempest)
            {
                WandCombatUtility.TryCastBurstMonsterSpell(
                    player,
                    playerClickData,
                    Cooldowns,
                    TempestManaCost,
                    TempestCooldownMs,
                    TempestMinimumCooldownMs,
                    TempestSpellDamage,
                    TempestProjectileForce,
                    TempestRange,
                    TempestAimAssistRadius,
                    TempestMissTrailDuration,
                    0.24f,
                    0.38f,
                    0f,
                    0,
                    0f,
                    TempestRadius,
                    TempestTargets,
                    TempestChainDamage,
                    TempestChainProjectileForce,
                    0,
                    0f,
                    0f,
                    0f,
                    WandCombatUtility.BurstVisualStyle.Tempest,
                    Item.ItemIndex,
                    AirStone.Item.ItemIndex,
                    WandCastAudioManager.StormHeavyCast,
                    WandCastAudioManager.StormHeavyImpact,
                    masteryKey: PlayerMagicStateManager.WandMasteryKeys.Storm);
                return;
            }

            WandCombatUtility.TryCastBurstMonsterSpell(
                player,
                playerClickData,
                Cooldowns,
                PrimaryManaCost,
                PrimaryCooldownMs,
                PrimaryMinimumCooldownMs,
                PrimarySpellDamage,
                PrimaryProjectileForce,
                PrimaryRange,
                PrimaryAimAssistRadius,
                PrimaryMissTrailDuration,
                0.22f,
                0.34f,
                0f,
                0,
                0f,
                PrimaryChainRadius,
                PrimaryChainTargets,
                PrimaryChainDamage,
                PrimaryChainProjectileForce,
                0,
                0f,
                0f,
                0f,
                WandCombatUtility.BurstVisualStyle.ArcBurst,
                Item.ItemIndex,
                AirStone.Item.ItemIndex,
                WandCastAudioManager.StormCast,
                WandCastAudioManager.StormImpact,
                masteryKey: PlayerMagicStateManager.WandMasteryKeys.Storm);
        }
    }

    [ModLoader.ModManager]
    public static class MagicWand
    {
        private const long AttackCooldownMs = 560;
        private const long AttackMinimumCooldownMs = 260;
        private const long HealCooldownMs = 760;
        private const long HealMinimumCooldownMs = 420;
        private const float SpellDamage = 52f;
        private const float ProjectileForce = 12f;
        private const float Range = 34f;
        private const float AimAssistRadius = 0.95f;
        private const float MissTrailDuration = 0.3f;
        private const float SelfHealOnHit = 10f;
        private const float SelfInitialHeal = 18f;
        private const float SelfHealOverTime = 36f;
        private const float TargetInitialHeal = 22f;
        private const float TargetHealOverTime = 38f;
        private const int HealDurationSeconds = 4;
        private const float SupportWaveRadius = 8f;
        private const int SelfWaveTargets = 4;
        private const int TargetWaveTargets = 3;
        private const float SupportWaveInitialHeal = 14f;
        private const float SupportWaveHealOverTime = 22f;
        private const int SupportWaveDurationSeconds = 3;
        private const int AttackManaCost = 3;
        private const int SelfHealManaCost = 6;
        private const int TargetHealManaCost = 4;

        private static readonly Dictionary<Players.Player, long> Cooldowns = new Dictionary<Players.Player, long>();

        private struct NearbyNpcCollector : NPCTracker.INPCIterator
        {
            public Vector3 Center;
            public float RadiusSquared;
            public NPCBase PrimaryTarget;
            public List<NPCBase> Results;

            public void IterateNPC(NPCBase npc)
            {
                if (npc == null || !npc.IsValid || npc == PrimaryTarget)
                    return;

                if ((npc.Position.Vector - Center).sqrMagnitude > RadiusSquared)
                    return;

                if (HealingOverTimeNPC.NPCIsBeingHealed(npc))
                    return;

                Results.Add(npc);
            }
        }

        public static ItemTypesServer.ItemTypeRaw Item { get; private set; }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AddItemTypes, GameLoader.NAMESPACE + ".Items.MagicWand.Add")]
        [ModLoader.ModCallbackDependsOn("pipliz.server.applymoditempatches")]
        public static void Add(Dictionary<string, ItemTypesServer.ItemTypeRaw> items)
        {
            Item = MagicAlchemyUtility.AddWandItem(items, "MagicWand", "AetherWand.png", "magic", "wand", "weapon", GameLoader.NAMESPACE);
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AfterItemTypesDefined, GameLoader.NAMESPACE + ".Items.MagicWand.Register")]
        [ModLoader.ModCallbackProvidesFor("pipliz.server.loadresearchables")]
        public static void Register()
        {
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnPlayerClicked, GameLoader.NAMESPACE + ".Items.MagicWand.OnPlayerClicked")]
        public static void OnPlayerClicked(Players.Player player, PlayerClickedData playerClickData)
        {
            if (!WandCombatUtility.TryPrepareWandClick(playerClickData, Item.ItemIndex, LegendaryWandItems.GetItemIndexOrDefault(PlayerMagicStateManager.WandMasteryKeys.Aether)))
                return;

            if (!WandCombatUtility.IsReady(player, Cooldowns))
                return;

            if (playerClickData.ClickType == PlayerClickedData.EClickType.Right)
            {
                var supportTarget = WandCombatUtility.TryGetDirectTargetNpc(playerClickData, Range);
                if (supportTarget != null)
                {
                    CastTargetHeal(player, playerClickData, supportTarget);
                    return;
                }

                CastSelfHeal(player, playerClickData);
                return;
            }

            if (playerClickData.ClickType != PlayerClickedData.EClickType.Left)
                return;

            var targetNpc = WandCombatUtility.TryGetDirectTargetNpc(playerClickData, Range);
            if (targetNpc != null)
            {
                CastTargetHeal(player, playerClickData, targetNpc);
                return;
            }

            WandCombatUtility.TryCastMonsterSpell(
                player,
                playerClickData,
                Cooldowns,
                AttackManaCost,
                AttackCooldownMs,
                AttackMinimumCooldownMs,
                SpellDamage,
                ProjectileForce,
                Range,
                AimAssistRadius,
                MissTrailDuration,
                0.24f,
                0.38f,
                selfHealOnHit: SelfHealOnHit,
                castIndicatorItemIndex: Item.ItemIndex,
                impactIndicatorItemIndex: Aether.Item.ItemIndex,
                castAudio: WandCastAudioManager.AetherCast,
                hitAudio: WandCastAudioManager.AetherImpact,
                impactVisualStyle: WandCombatUtility.BurstVisualStyle.AetherWave,
                impactVisualRadius: 2.5f,
                masteryKey: PlayerMagicStateManager.WandMasteryKeys.Aether);
        }

        private static void CastSelfHeal(Players.Player player, PlayerClickedData playerClickData)
        {
            var masteryKey = PlayerMagicStateManager.WandMasteryKeys.Aether;
            var selectedWandItemIndex = WandCombatUtility.GetSelectedWandItemIndex(playerClickData, Item.ItemIndex);
            var healMultiplier = PlayerMagicStateManager.GetWandMasteryHealMultiplier(player, masteryKey);
            var extraTargets = PlayerMagicStateManager.GetWandMasteryExtraTargets(player, masteryKey);
            var effectiveManaCost = System.Math.Max(1, SelfHealManaCost - PlayerMagicStateManager.GetWandMasteryManaDiscount(player, masteryKey));

            if (!WandCombatUtility.TrySpendManaForCast(player, playerClickData, Cooldowns, effectiveManaCost))
                return;

            playerClickData.ConsumedType = PlayerClickedData.EConsumedType.ChangedBlock;
            _ = new HealingOverTimePC(player, SelfInitialHeal * healMultiplier, SelfHealOverTime * healMultiplier, HealDurationSeconds);
            PlayerMagicStateManager.ShowItemIndicator(player, selectedWandItemIndex, 0.6f);
            ServerManager.SendParticleTrail(player.PositionStanding + Vector3.up * 0.4f, player.PositionStanding + Vector3.up * 2f, 0.35f);
            WandCombatUtility.PlayVisualEffect(player.PositionStanding + Vector3.up * 1.15f, Vector3.up, WandCombatUtility.BurstVisualStyle.AetherWave, 3.2f);
            ApplyNearbyHealWave(player.PositionStanding + Vector3.up * 1.2f, null, SelfWaveTargets + extraTargets, SupportWaveRadius, SupportWaveInitialHeal * healMultiplier, SupportWaveHealOverTime * healMultiplier, SupportWaveDurationSeconds, player.PositionStanding + Vector3.up * 0.8f);
            ApplyMasteryEchoWave(player, masteryKey, player.PositionStanding + Vector3.up * 1.2f, null, SupportWaveInitialHeal * healMultiplier, SupportWaveHealOverTime * healMultiplier, SupportWaveDurationSeconds, player.PositionStanding + Vector3.up * 0.8f);
            WandCastAudioManager.Play(player, player.PositionStanding, WandCastAudioManager.AetherHeavyCast);
            AudioManager.SendAudio(player.PositionStanding + Vector3.up * 1.15f, WandCastAudioManager.AetherHeavyImpact);
            var effectiveCooldownMs = System.Math.Max(HealMinimumCooldownMs, HealCooldownMs - PlayerMagicStateManager.GetWandMasteryCooldownReductionMs(player, masteryKey));
            WandCombatUtility.SetCooldown(player, Cooldowns, WandCombatUtility.GetAdjustedCooldownMs(player, effectiveCooldownMs, HealMinimumCooldownMs));
            PlayerMagicStateManager.RecordWandMasteryUse(player, masteryKey, selectedWandItemIndex, 2);
        }

        private static void CastTargetHeal(Players.Player player, PlayerClickedData playerClickData, NPCBase npc)
        {
            var masteryKey = PlayerMagicStateManager.WandMasteryKeys.Aether;
            var selectedWandItemIndex = WandCombatUtility.GetSelectedWandItemIndex(playerClickData, Item.ItemIndex);
            var healMultiplier = PlayerMagicStateManager.GetWandMasteryHealMultiplier(player, masteryKey);
            var extraTargets = PlayerMagicStateManager.GetWandMasteryExtraTargets(player, masteryKey);
            var effectiveManaCost = System.Math.Max(1, TargetHealManaCost - PlayerMagicStateManager.GetWandMasteryManaDiscount(player, masteryKey));

            if (!WandCombatUtility.TrySpendManaForCast(player, playerClickData, Cooldowns, effectiveManaCost))
                return;

            if (!WandCombatUtility.TryGetShotDirection(playerClickData, out var shotDirection))
                return;

            var trailOrigin = WandCombatUtility.GetTrailOrigin(player, shotDirection);
            playerClickData.ConsumedType = PlayerClickedData.EConsumedType.ChangedBlock;

            _ = new HealingOverTimeNPC(npc, TargetInitialHeal * healMultiplier, TargetHealOverTime * healMultiplier, HealDurationSeconds, selectedWandItemIndex);
            PlayerMagicStateManager.ShowItemIndicator(player, selectedWandItemIndex, 0.6f);
            var targetPosition = npc.Position.Vector + Vector3.up * 1.25f;
            var supportDirection = targetPosition - trailOrigin;
            if (supportDirection.sqrMagnitude < 0.0001f)
                supportDirection = Vector3.up;
            else
                supportDirection.Normalize();

            ServerManager.SendParticleTrail(trailOrigin, targetPosition + Vector3.up * 0.15f, 0.38f);
            ServerManager.SendParticleTrail(targetPosition + Vector3.up * 0.15f, targetPosition + Vector3.up * 1.9f, 0.24f);
            WandCombatUtility.PlayVisualEffect(targetPosition, supportDirection, WandCombatUtility.BurstVisualStyle.AetherWave, 2.8f);
            ApplyNearbyHealWave(npc.Position.Vector + Vector3.up * 1.2f, npc, TargetWaveTargets + extraTargets, SupportWaveRadius, SupportWaveInitialHeal * healMultiplier, SupportWaveHealOverTime * healMultiplier, SupportWaveDurationSeconds, npc.Position.Vector + Vector3.up * 1.2f);
            ApplyMasteryEchoWave(player, masteryKey, npc.Position.Vector + Vector3.up * 1.2f, npc, SupportWaveInitialHeal * healMultiplier, SupportWaveHealOverTime * healMultiplier, SupportWaveDurationSeconds, npc.Position.Vector + Vector3.up * 1.2f);
            var isHeavySupport = playerClickData.ClickType == PlayerClickedData.EClickType.Right;
            WandCastAudioManager.Play(player, trailOrigin, isHeavySupport ? WandCastAudioManager.AetherHeavyCast : WandCastAudioManager.AetherCast);
            AudioManager.SendAudio(targetPosition, isHeavySupport ? WandCastAudioManager.AetherHeavyImpact : WandCastAudioManager.AetherImpact);
            var effectiveCooldownMs = System.Math.Max(HealMinimumCooldownMs, HealCooldownMs - PlayerMagicStateManager.GetWandMasteryCooldownReductionMs(player, masteryKey));
            WandCombatUtility.SetCooldown(player, Cooldowns, WandCombatUtility.GetAdjustedCooldownMs(player, effectiveCooldownMs, HealMinimumCooldownMs));
            PlayerMagicStateManager.RecordWandMasteryUse(player, masteryKey, selectedWandItemIndex, playerClickData.ClickType == PlayerClickedData.EClickType.Right ? 2 : 1);
        }

        private static void ApplyMasteryEchoWave(
            Players.Player player,
            string masteryKey,
            Vector3 center,
            NPCBase primaryTarget,
            float initialHeal,
            float totalHealOverTime,
            int durationSeconds,
            Vector3 trailOrigin)
        {
            var masteryLevel = PlayerMagicStateManager.GetWandMasteryLevel(player, masteryKey);
            if (masteryLevel < 2)
                return;

            var extraMana = masteryLevel >= 3 ? 2 : 1;
            var manaItemIndex = Mana.Item != null ? Mana.Item.ItemIndex : (ushort)0;
            PlayerMagicStateManager.RestoreMana(player, extraMana, manaItemIndex);

            var echoTargets = masteryLevel >= 3 ? 2 : 1;
            var echoHealMultiplier = masteryLevel >= 3 ? 0.5f : 0.35f;
            var echoRadius = masteryLevel >= 3 ? SupportWaveRadius + 1.2f : SupportWaveRadius * 0.8f;

            WandCombatUtility.PlayVisualEffect(center + Vector3.up * 0.1f, Vector3.up, WandCombatUtility.BurstVisualStyle.AetherWave, masteryLevel >= 3 ? 3.1f : 2.4f);
            ApplyNearbyHealWave(center, primaryTarget, echoTargets, echoRadius, initialHeal * echoHealMultiplier, totalHealOverTime * echoHealMultiplier, durationSeconds, trailOrigin);

            if (!PlayerMagicStateManager.IsWandLegendaryEvolutionUnlocked(player, masteryKey))
                return;

            PlayerMagicStateManager.RestoreMana(player, 1, manaItemIndex);
            var seraphCenter = center + Vector3.up * 0.2f;
            WandCombatUtility.PlayVisualEffect(seraphCenter, Vector3.up, WandCombatUtility.BurstVisualStyle.AetherWave, SupportWaveRadius + 2.2f);
            ApplyNearbyHealWave(seraphCenter, primaryTarget, echoTargets + 1, SupportWaveRadius + 2.2f, initialHeal * 0.4f, totalHealOverTime * 0.45f, durationSeconds, trailOrigin);
            if (primaryTarget == null)
                return;

            _ = new HealingOverTimePC(player, initialHeal * 0.22f, totalHealOverTime * 0.18f, durationSeconds);
            ServerManager.SendParticleTrail(primaryTarget.Position.Vector + Vector3.up * 1.2f, player.PositionStanding + Vector3.up * 1.15f, 0.28f);
        }

        private static void ApplyNearbyHealWave(
            Vector3 center,
            NPCBase primaryTarget,
            int maxTargets,
            float radius,
            float initialHeal,
            float totalHealOverTime,
            int durationSeconds,
            Vector3 trailOrigin)
        {
            if (maxTargets <= 0 || radius <= 0f)
                return;

            var collector = new NearbyNpcCollector
            {
                Center = center,
                RadiusSquared = radius * radius,
                PrimaryTarget = primaryTarget,
                Results = new List<NPCBase>()
            };

            var min = new Pipliz.Vector3Int(center - Vector3.one * radius);
            var max = new Pipliz.Vector3Int(center + Vector3.one * radius);
            NPCTracker.IterateNPCs(min, max, ref collector);

            collector.Results.Sort((left, right) =>
                (left.Position.Vector - center).sqrMagnitude.CompareTo((right.Position.Vector - center).sqrMagnitude));

            for (var i = 0; i < collector.Results.Count && i < maxTargets; i++)
            {
                var nearbyNpc = collector.Results[i];
                if (nearbyNpc == null || !nearbyNpc.IsValid)
                    continue;

                _ = new HealingOverTimeNPC(nearbyNpc, initialHeal, totalHealOverTime, durationSeconds, Item.ItemIndex);
                ServerManager.SendParticleTrail(trailOrigin, nearbyNpc.Position.Vector + Vector3.up * 1.2f, 0.32f);
            }
        }
    }

    [ModLoader.ModManager]
    public static class BloodWand
    {
        private const long PrimaryCooldownMs = 680;
        private const long PrimaryMinimumCooldownMs = 320;
        private const long HemorrhageCooldownMs = 1120;
        private const long HemorrhageMinimumCooldownMs = 560;
        private const float PrimarySpellDamage = 84f;
        private const float HemorrhageSpellDamage = 136f;
        private const float PrimaryProjectileForce = 16f;
        private const float HemorrhageProjectileForce = 22f;
        private const float PrimaryRange = 36f;
        private const float HemorrhageRange = 40f;
        private const float PrimaryAimAssistRadius = 1.0f;
        private const float HemorrhageAimAssistRadius = 1.1f;
        private const float PrimaryMissTrailDuration = 0.32f;
        private const float HemorrhageMissTrailDuration = 0.38f;
        private const int PrimaryManaCost = 5;
        private const int HemorrhageManaCost = 9;
        private const float PrimarySelfHealOnHit = 18f;
        private const float HemorrhageSelfHealOnHit = 36f;
        private const float HemorrhageDotDamagePerTick = 18f;
        private const int HemorrhageDotTickCount = 4;
        private const float HemorrhageDotTickDelaySeconds = 0.75f;
        private const float HemorrhageBurstRadius = 4.6f;
        private const int HemorrhageBurstTargets = 2;
        private const float HemorrhageBurstDamage = 42f;
        private const float HemorrhageBurstProjectileForce = 16f;
        private const float HemorrhageBurstDotDamagePerTick = 10f;
        private const int HemorrhageBurstDotTickCount = 2;
        private const float HemorrhageBurstDotTickDelaySeconds = 0.65f;

        private static readonly Dictionary<Players.Player, long> Cooldowns = new Dictionary<Players.Player, long>();

        public static ItemTypesServer.ItemTypeRaw Item { get; private set; }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AddItemTypes, GameLoader.NAMESPACE + ".Items.BloodWand.Add")]
        [ModLoader.ModCallbackDependsOn("pipliz.server.applymoditempatches")]
        public static void Add(Dictionary<string, ItemTypesServer.ItemTypeRaw> items)
        {
            Item = MagicAlchemyUtility.AddWandItem(items, "BloodWand", "BloodWand.png", "magic", "wand", "weapon", GameLoader.NAMESPACE);
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AfterItemTypesDefined, GameLoader.NAMESPACE + ".Items.BloodWand.Register")]
        [ModLoader.ModCallbackProvidesFor("pipliz.server.loadresearchables")]
        public static void Register()
        {
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnPlayerClicked, GameLoader.NAMESPACE + ".Items.BloodWand.OnPlayerClicked")]
        public static void OnPlayerClicked(Players.Player player, PlayerClickedData playerClickData)
        {
            if (!WandCombatUtility.TryPrepareWandClick(playerClickData, Item.ItemIndex, LegendaryWandItems.GetItemIndexOrDefault(PlayerMagicStateManager.WandMasteryKeys.Blood)))
                return;

            if (!WandCombatUtility.IsReady(player, Cooldowns))
                return;

            var isHemorrhage = playerClickData.ClickType == PlayerClickedData.EClickType.Right;
            if (isHemorrhage)
            {
                WandCombatUtility.TryCastBurstMonsterSpell(
                    player,
                    playerClickData,
                    Cooldowns,
                    HemorrhageManaCost,
                    HemorrhageCooldownMs,
                    HemorrhageMinimumCooldownMs,
                    HemorrhageSpellDamage,
                    HemorrhageProjectileForce,
                    HemorrhageRange,
                    HemorrhageAimAssistRadius,
                    HemorrhageMissTrailDuration,
                    0.24f,
                    0.4f,
                    HemorrhageDotDamagePerTick,
                    HemorrhageDotTickCount,
                    HemorrhageDotTickDelaySeconds,
                    HemorrhageBurstRadius,
                    HemorrhageBurstTargets,
                    HemorrhageBurstDamage,
                    HemorrhageBurstProjectileForce,
                    HemorrhageBurstDotTickCount,
                    HemorrhageBurstDotDamagePerTick,
                    HemorrhageBurstDotTickDelaySeconds,
                    HemorrhageSelfHealOnHit,
                    WandCombatUtility.BurstVisualStyle.Hemorrhage,
                    Item.ItemIndex,
                    Void.Item.ItemIndex,
                    WandCastAudioManager.BloodHeavyCast,
                    WandCastAudioManager.BloodHeavyImpact,
                    true,
                    true,
                    PlayerMagicStateManager.WandMasteryKeys.Blood);
                return;
            }

            WandCombatUtility.TryCastMonsterSpell(
                player,
                playerClickData,
                Cooldowns,
                PrimaryManaCost,
                PrimaryCooldownMs,
                PrimaryMinimumCooldownMs,
                PrimarySpellDamage,
                PrimaryProjectileForce,
                PrimaryRange,
                PrimaryAimAssistRadius,
                PrimaryMissTrailDuration,
                0.24f,
                0.36f,
                0f,
                0,
                0f,
                PrimarySelfHealOnHit,
                Item.ItemIndex,
                Void.Item.ItemIndex,
                WandCastAudioManager.BloodCast,
                WandCastAudioManager.BloodImpact,
                impactVisualStyle: WandCombatUtility.BurstVisualStyle.Hemorrhage,
                impactVisualRadius: 2.3f,
                masteryKey: PlayerMagicStateManager.WandMasteryKeys.Blood);
        }
    }

    [ModLoader.ModManager]
    public static class VoidWand
    {
        private const long PrimaryCooldownMs = 780;
        private const long PrimaryMinimumCooldownMs = 460;
        private const long RuptureCooldownMs = 1220;
        private const long RuptureMinimumCooldownMs = 750;
        private const float PrimarySpellDamage = 140f;
        private const float RuptureSpellDamage = 220f;
        private const float PrimaryProjectileForce = 20f;
        private const float RuptureProjectileForce = 28f;
        private const float PrimaryRange = 42f;
        private const float RuptureRange = 44f;
        private const float PrimaryAimAssistRadius = 1.05f;
        private const float RuptureAimAssistRadius = 1.15f;
        private const float PrimaryMissTrailDuration = 0.34f;
        private const float RuptureMissTrailDuration = 0.42f;
        private const int PrimaryManaCost = 6;
        private const int RuptureManaCost = 10;
        private const float RuptureDamagePerTick = 30f;
        private const int RuptureTickCount = 3;
        private const float RuptureTickDelaySeconds = 0.7f;
        private const float RuptureChainRadius = 6.5f;
        private const int RuptureChainTargets = 4;
        private const float RuptureChainDamage = 96f;
        private const float RuptureChainProjectileForce = 18f;
        private const float RuptureChainDamagePerTick = 24f;
        private const int RuptureChainTickCount = 2;
        private const float RuptureChainTickDelaySeconds = 0.55f;

        private static readonly Dictionary<Players.Player, long> Cooldowns = new Dictionary<Players.Player, long>();

        public static ItemTypesServer.ItemTypeRaw Item { get; private set; }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AddItemTypes, GameLoader.NAMESPACE + ".Items.VoidWand.Add")]
        [ModLoader.ModCallbackDependsOn("pipliz.server.applymoditempatches")]
        public static void Add(Dictionary<string, ItemTypesServer.ItemTypeRaw> items)
        {
            Item = MagicAlchemyUtility.AddWandItem(items, "VoidWand", "VoidWand.png", "magic", "wand", "weapon", GameLoader.NAMESPACE);
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AfterItemTypesDefined, GameLoader.NAMESPACE + ".Items.VoidWand.Register")]
        [ModLoader.ModCallbackProvidesFor("pipliz.server.loadresearchables")]
        public static void Register()
        {
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnPlayerClicked, GameLoader.NAMESPACE + ".Items.VoidWand.OnPlayerClicked")]
        public static void OnPlayerClicked(Players.Player player, PlayerClickedData playerClickData)
        {
            if (!WandCombatUtility.TryPrepareWandClick(playerClickData, Item.ItemIndex, LegendaryWandItems.GetItemIndexOrDefault(PlayerMagicStateManager.WandMasteryKeys.Void)))
                return;

            if (!WandCombatUtility.IsReady(player, Cooldowns))
                return;

            var isRupture = playerClickData.ClickType == PlayerClickedData.EClickType.Right;
            if (isRupture)
            {
                WandCombatUtility.TryCastBurstMonsterSpell(
                    player,
                    playerClickData,
                    Cooldowns,
                    RuptureManaCost,
                    RuptureCooldownMs,
                    RuptureMinimumCooldownMs,
                    RuptureSpellDamage,
                    RuptureProjectileForce,
                    RuptureRange,
                    RuptureAimAssistRadius,
                    RuptureMissTrailDuration,
                    0.22f,
                    0.36f,
                    RuptureDamagePerTick,
                    RuptureTickCount,
                    RuptureTickDelaySeconds,
                    RuptureChainRadius,
                    RuptureChainTargets,
                    RuptureChainDamage,
                    RuptureChainProjectileForce,
                    RuptureChainTickCount,
                    RuptureChainDamagePerTick,
                    RuptureChainTickDelaySeconds,
                    0f,
                    WandCombatUtility.BurstVisualStyle.Rupture,
                    Item.ItemIndex,
                    Void.Item.ItemIndex,
                    WandCastAudioManager.VoidHeavyCast,
                    WandCastAudioManager.VoidHeavyImpact,
                    masteryKey: PlayerMagicStateManager.WandMasteryKeys.Void);
                return;
            }

            WandCombatUtility.TryCastMonsterSpell(
                player,
                playerClickData,
                Cooldowns,
                PrimaryManaCost,
                PrimaryCooldownMs,
                PrimaryMinimumCooldownMs,
                PrimarySpellDamage,
                PrimaryProjectileForce,
                PrimaryRange,
                PrimaryAimAssistRadius,
                PrimaryMissTrailDuration,
                0.22f,
                0.36f,
                castIndicatorItemIndex: Item.ItemIndex,
                impactIndicatorItemIndex: Void.Item.ItemIndex,
                castAudio: WandCastAudioManager.VoidCast,
                hitAudio: WandCastAudioManager.VoidImpact,
                impactVisualStyle: WandCombatUtility.BurstVisualStyle.Rupture,
                impactVisualRadius: 2.7f,
                masteryKey: PlayerMagicStateManager.WandMasteryKeys.Void);
        }
    }
}
