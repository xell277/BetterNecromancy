using colonyserver.Assets.UIGeneration;
using colonyshared.NetworkUI;
using colonyshared.NetworkUI.UIGeneration;
using BetterNecromancy;
using Monsters;
using NPC;
using Pandaros.API.Entities;
using Pandaros.API.Monsters;
using Shared;
using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;

namespace Pandaros.Settlers.Monsters
{
    [ModLoader.ModManager]
    public static class CustomBossFrameworkManager
    {
        private const string BossHudTitleKey = "BetterNecromancy.BossHud.Title";
        private const string BossHudBarKey = "BetterNecromancy.BossHud.Bar";
        private const int HudBarSegments = 26;
        private const long HudUpdateIntervalMs = 100;

        private static readonly FieldInfo ItemTypeByUShortField = typeof(ItemTypes).GetField("_TypeByUShort", BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly Dictionary<ushort, float> WeaponDamageOverrides = new Dictionary<ushort, float>();
        private static long _nextHudUpdateAtMs;

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AfterWorldLoad, BetterNecromancy.ModEntry.Namespace + ".CustomBossFrameworkManager.AfterWorldLoad")]
        public static void AfterWorldLoad()
        {
            WeaponDamageOverrides.Clear();
            _nextHudUpdateAtMs = 0L;
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnPlayerConnectedLate, BetterNecromancy.ModEntry.Namespace + ".CustomBossFrameworkManager.OnPlayerConnectedLate")]
        public static void OnPlayerConnectedLate(Players.Player player)
        {
            if (!PlayerUiGuard.CanSendStable(player))
                return;

            ClearBossHud(player);
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnMonsterDied, BetterNecromancy.ModEntry.Namespace + ".CustomBossFrameworkManager.OnMonsterDied")]
        public static void OnMonsterDied(IMonster monster)
        {
            if (monster is Pandaros.API.Monsters.IPandaBoss)
                ClearBossHudForAllPlayers();
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnPlayerClicked, BetterNecromancy.ModEntry.Namespace + ".CustomBossFrameworkManager.OnPlayerClicked")]
        public static void OnPlayerClicked(Players.Player player, PlayerClickedData playerClickData)
        {
            if (player == null ||
                playerClickData == null ||
                playerClickData.ClickType != PlayerClickedData.EClickType.Left ||
                playerClickData.ConsumedType != PlayerClickedData.EConsumedType.Not)
            {
                return;
            }

            Zombie boss = null;
            if (playerClickData.HitType == PlayerClickedData.EHitType.ControlledMesh)
            {
                var meshHit = playerClickData.GetControlledMeshHit();
                BossVisualProxyManager.TryGetBossByMeshId(meshHit.MeshID, out boss, out _);
            }

            if (boss == null)
            {
                var rayOrigin = player.PositionStanding;
                var rayDirection = playerClickData.PlayerAimDirection.normalized;
                if (rayDirection.sqrMagnitude > 0.0001f &&
                    BossVisualProxyManager.TryRaycastBoss(rayOrigin, rayDirection, 64f, out var rayBoss, out _, out _) &&
                    VoxelPhysics.CanSee(rayOrigin, rayBoss.PositionToAimFor))
                {
                    boss = rayBoss;
                }
            }

            if (boss == null || !boss.IsValid || boss.CurrentHealth <= 0f)
                return;

            if (IsHandledByMagicWand(playerClickData.TypeSelected))
                return;

            var damage = ResolvePlayerWeaponDamage(player, playerClickData.TypeSelected);
            if (damage <= 0f)
                return;

            playerClickData.ConsumedType = PlayerClickedData.EConsumedType.UsedByMod;

            var hitForce = playerClickData.PlayerAimDirection.normalized * 10f;
            if (hitForce.sqrMagnitude <= 0.0001f)
                hitForce = Vector3.forward * 10f;

            boss.OnHit(damage, hitForce, player, ModLoader.OnHitData.EHitSourceType.PlayerClick);
            AudioManager.SendAudio(boss.PositionToAimFor, "punch");
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnUpdate, BetterNecromancy.ModEntry.Namespace + ".CustomBossFrameworkManager.OnUpdate")]
        public static void OnUpdate()
        {
            var now = Pipliz.Time.MillisecondsSinceStart;
            if (now < _nextHudUpdateAtMs)
                return;

            _nextHudUpdateAtMs = now + HudUpdateIntervalMs;

            if (!TryResolvePrimaryBossForHud(out var boss, out var bossName) ||
                boss == null ||
                !boss.IsValid ||
                boss.CurrentHealth <= 0f)
            {
                if (HordeEventManager.TryGetHudState(out var hordeTitleText, out var hordeBarText, out var hordeTitleColor, out var hordeBarColor))
                {
                    RenderHudForAllPlayers(hordeTitleText, hordeBarText, hordeTitleColor, hordeBarColor);
                    return;
                }

                ClearBossHudForAllPlayers();
                return;
            }

            var currentHealth = Mathf.Max(0f, boss.CurrentHealth);
            var maxHealth = Mathf.Max(1f, boss.TotalHealth);
            var percent = Mathf.Clamp01(currentHealth / maxHealth);
            var filled = Mathf.Clamp(Mathf.RoundToInt(percent * HudBarSegments), 0, HudBarSegments);

            var titleText = bossName + "  " + Mathf.CeilToInt(currentHealth) + " / " + Mathf.CeilToInt(maxHealth);
            var barText = "[" + new string('=', filled) + new string('-', HudBarSegments - filled) + "]";
            RenderHudForAllPlayers(titleText, barText, "#f1d9d9", "#d94848");
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnMonsterHit, BetterNecromancy.ModEntry.Namespace + ".CustomBossFrameworkManager.OnMonsterHit")]
        public static void OnMonsterHit(IMonster monster, ModLoader.OnHitData hitData)
        {
            if (!(monster is IPandaBoss) ||
                hitData == null ||
                hitData.ResultDamage <= 0f)
            {
                return;
            }

            var trailDuration = 0.28f;
            Vector3? trailOrigin = null;

            switch (hitData.HitSourceType)
            {
                case ModLoader.OnHitData.EHitSourceType.NPC:
                case ModLoader.OnHitData.EHitSourceType.Trap:
                    if (hitData.HitSourceObject is NPCBase npc)
                        trailOrigin = npc.Position.Vector + Vector3.up * 1.1f;
                    break;

                case ModLoader.OnHitData.EHitSourceType.PlayerProjectile:
                    if (hitData.HitSourceObject is Players.Player player)
                        trailOrigin = player.PositionStanding + Vector3.up * 1.35f;
                    break;
            }

            if (!trailOrigin.HasValue)
                return;

            if (PlayerUiGuard.ShouldDeferPlayerFacingEffects())
                return;

            ServerManager.SendParticleTrail(trailOrigin.Value, monster.PositionToAimFor, trailDuration);
        }

        private static void ClearBossHudForAllPlayers()
        {
            foreach (var player in Players.ConnectedPlayers)
                ClearBossHud(player);
        }

        private static void ClearBossHud(Players.Player player)
        {
            if (player == null)
                return;

            UIManager.RemoveUILabel(BossHudTitleKey, player);
            UIManager.RemoveUILabel(BossHudBarKey, player);
        }

        private static void RenderHudForAllPlayers(string titleText, string barText, string titleColor, string barColor)
        {
            foreach (var player in Players.ConnectedPlayers)
            {
                if (!PlayerUiGuard.CanSendStable(player))
                    continue;

                UIManager.AddorUpdateUILabel(
                    BossHudTitleKey,
                    UIElementDisplayType.Global,
                    titleText,
                    new Pipliz.Vector3Int(0, -42, 0),
                    AnchorPresets.TopCenter,
                    420f,
                    player,
                    18f,
                    FontType.DidactGothic,
                    titleColor,
                    TextAlignmentOptions.Center);

                UIManager.AddorUpdateUILabel(
                    BossHudBarKey,
                    UIElementDisplayType.Global,
                    barText,
                    new Pipliz.Vector3Int(0, -68, 0),
                    AnchorPresets.TopCenter,
                    420f,
                    player,
                    18f,
                    FontType.DidactGothic,
                    barColor,
                    TextAlignmentOptions.Center);
            }
        }

        private static bool TryResolvePrimaryBossForHud(out Zombie boss, out string bossName)
        {
            if (BossVisualProxyManager.TryGetPrimaryActiveBoss(out boss, out bossName) &&
                boss != null &&
                boss.IsValid &&
                boss.CurrentHealth > 0f)
            {
                return true;
            }

            return MonsterManager.TryGetPrimaryActiveBoss(out boss, out bossName);
        }

        private static bool IsHandledByMagicWand(ushort selectedType)
        {
            return selectedType == Pandaros.Settlers.Items.ManaWand.Item?.ItemIndex ||
                   selectedType == Pandaros.Settlers.Items.EmberWand.Item?.ItemIndex ||
                   selectedType == Pandaros.Settlers.Items.FrostWand.Item?.ItemIndex ||
                   selectedType == Pandaros.Settlers.Items.MagicWand.Item?.ItemIndex ||
                   selectedType == Pandaros.Settlers.Items.VoidWand.Item?.ItemIndex;
        }

        private static float ResolvePlayerWeaponDamage(Players.Player player, ushort selectedType)
        {
            if (selectedType == 0)
                return GetAugmentedPunchDamage(player, Players.PlayerPunchDamage);

            if (WeaponDamageOverrides.TryGetValue(selectedType, out var cachedDamage))
                return GetAugmentedPunchDamage(player, cachedDamage);

            var damage = Players.PlayerPunchDamage;

            if (TryGetSelectedItemType(selectedType, out var itemType))
            {
                var itemName = itemType.Name ?? string.Empty;
                var categories = itemType.Categories ?? new List<string>();
                var lowerName = itemName.ToLowerInvariant();

                if (lowerName.Contains("sword"))
                    damage = 62f;
                else if (lowerName.Contains("spear"))
                    damage = 54f;
                else if (lowerName.Contains("axe") || lowerName.Contains("mace") || lowerName.Contains("hammer"))
                    damage = 68f;
                else if (lowerName.Contains("sling"))
                    damage = 36f;
                else if (lowerName.Contains("bow") || lowerName.Contains("crossbow"))
                    damage = 48f;
                else if (categories.Contains("weapon"))
                    damage = 48f;
                else if (categories.Contains("defense"))
                    damage = 42f;
            }

            WeaponDamageOverrides[selectedType] = damage;
            return GetAugmentedPunchDamage(player, damage);
        }

        private static float GetAugmentedPunchDamage(Players.Player player, float baseDamage)
        {
            var damage = baseDamage + PlayerMagicStateManager.GetPlayerClickDamageBonus(player);
            var critChance = PlayerMagicStateManager.GetPlayerClickCritChance(player);
            if (critChance > 0f && Pipliz.Random.NextFloat() <= critChance)
                damage += PlayerMagicStateManager.GetPlayerClickCritBonus(player);

            return damage;
        }

        private static bool TryGetSelectedItemType(ushort selectedType, out ItemTypes.ItemType itemType)
        {
            itemType = default;

            if (ItemTypeByUShortField?.GetValue(null) is Dictionary<ushort, ItemTypes.ItemType> typeByUShort &&
                typeByUShort.TryGetValue(selectedType, out var resolvedType))
            {
                itemType = resolvedType;
                return true;
            }

            return false;
        }
    }
}
