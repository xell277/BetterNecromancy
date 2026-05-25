using Newtonsoft.Json.Linq;
using Pipliz;
using Chatting;
using BetterNecromancy;
using colonyserver.Assets.UIGeneration;
using colonyshared.NetworkUI;
using colonyshared.NetworkUI.UIGeneration;
using NetworkUI;
using NetworkUI.Items;
using Pandaros.Settlers;
using Shared;
using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;

namespace Pandaros.API.Entities
{
    [ModLoader.ModManager]
    public static class PlayerMagicStateManager
    {
        public enum MagicArmorSlot
        {
            Helm,
            Chest,
            Gloves,
            Legs,
            Boots,
            Shield
        }

        public static class WandMasteryKeys
        {
            public const string Mana = "Mana";
            public const string Briar = "Briar";
            public const string Spark = "Spark";
            public const string Venom = "Venom";
            public const string Ember = "Ember";
            public const string Frost = "Frost";
            public const string Crystal = "Crystal";
            public const string Stone = "Stone";
            public const string Storm = "Storm";
            public const string Aether = "Aether";
            public const string Blood = "Blood";
            public const string Void = "Void";
        }

        private sealed class PlayerMagicState
        {
            public bool BootsOfFallingEquipped { get; set; }
            public bool HealthBoosterEquipped { get; set; }
            public bool FirstBossRewardGranted { get; set; }
            public bool WelcomeMessageSentThisSession { get; set; }
            public bool ManaFlightEnabled { get; set; } = true;
            public bool ManaFlightUnlocked { get; set; }
            public bool ManaFlightActive { get; set; }
            public bool ManaFlightPermissionGranted { get; set; }
            public long FlightMasterySeconds { get; set; }
            public int ManaCrystalLevel { get; set; }
            public int ArcaneFocusTier { get; set; }
            public int SkilledSwordMask { get; set; }
            public int SkilledHelmTier { get; set; }
            public int SkilledChestTier { get; set; }
            public int SkilledGlovesTier { get; set; }
            public int SkilledLegsTier { get; set; }
            public int SkilledBootsTier { get; set; }
            public int SkilledShieldTier { get; set; }
            public int CurrentMana { get; set; } = DefaultMaxMana;
            public int MaxMana { get; set; } = DefaultMaxMana;
            public long LastManaRegenAtMs { get; set; }
            public string LastManaHudSignature { get; set; }
            public string LastWandMasteryHudSignature { get; set; }
            public string LastObservedWandMasteryKey { get; set; }
            public string LastObservedWandDisplayName { get; set; }
            public long LastObservedWandUntilMs { get; set; }
            public long NextNoManaWarningAtMs { get; set; }
            public long NextStarterWandResetHintAtMs { get; set; }
            public long NextVisualFeedbackAtMs { get; set; }
            public long NextManaFlightDrainAtMs { get; set; }
            public long ManaFlightUnsupportedSinceAtMs { get; set; }
            public long PendingManaFlightCrashUntilMs { get; set; }
            public long ManaFlightAirborneSinceAtMs { get; set; }
            public long ManaFlightGroundedSinceAtMs { get; set; }
            public long LastManaFlightActiveAtMs { get; set; }
            public long WelcomeMessageReadyAtMs { get; set; }
            public long WelcomeOverlayVisibleUntilMs { get; set; }
            public float ManaFlightCostCarry { get; set; }
            public float ManaFlightTakeoffY { get; set; }
            public float LastManaFlightHorizontalSpeed { get; set; }
            public Vector3 LastManaFlightSamplePosition { get; set; }
            public Vector3 ManaFlightTakeoffPosition { get; set; }
            public bool ManaFlightSampleInitialized { get; set; }
            public long LastManaFlightSampleAtMs { get; set; }
            public Dictionary<string, int> WandMasteryUses { get; } = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, bool> LegendaryWandUnlocks { get; } = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, int> LegendaryWandMasteryUses { get; } = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            public bool WandLineageUnlocked { get; set; }
            public string WandLineageBranch { get; set; }
            public int WandLineageTier { get; set; }
            public int WandLineageMasteryPoints { get; set; }
            public int WandLineageBranchPoints { get; set; }
            public int GlobalArcanePoints { get; set; }
            public bool WandLineageChoicePending { get; set; }
            public int WandLineagePendingTier { get; set; }
            public bool WandLineageRitualAwakened { get; set; }
            public long NextWandEvolutionChoicePopupAtMs { get; set; }
            public long WandLineageChoiceOpenedAtMs { get; set; }
            public HashSet<string> WandLineageEffectKeys { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            public long FullManaMeditationSeconds { get; set; }
            public int MeditationMaxManaRewardsGranted { get; set; }
            public bool MeditationRegenRewardGranted { get; set; }
            public bool MeditationActive { get; set; }
            public long MeditationStationarySinceAtMs { get; set; }
            public long MeditationFullManaCarryMs { get; set; }
            public long LastMeditationSampleAtMs { get; set; }
            public Vector3 LastMeditationPosition { get; set; }
            public string LastMeditationHudSignature { get; set; }
        }

        private const int DefaultMaxMana = 10;
        private const int ManaRegenIntervalMs = 3000;
        private const int ManaCrystalManaBonus = 2;
        private const int ManaCrystalRegenReductionMs = 300;
        private const int MinimumManaRegenIntervalMs = 1200;
        private const int ManaFlightUnstableCostPerSecond = 2;
        private const int ManaFlightStableCostPerSecond = 1;
        private const float ManaFlightMasteryThreeCostPerSecond = 0.5f;
        private const float ManaFlightUnstableExtraManaChance = 0.65f;
        private const float ManaFlightUnstableShutdownChance = 0.0015f;
        private const int ManaFlightUnstableTakeoffCost = 10;
        private const long FlightMasteryLevel1RequirementSeconds = 1000L;
        private const long FlightMasteryLevel2RequirementSeconds = 10000L;
        private const long FlightMasteryLevel3RequirementSeconds = 100000L;
        private const int ManaHudSegments = 20;
        private const long ManaFlightDrainIntervalMs = 1000;
        private const long ManaFlightSampleIntervalMs = 250;
        private const long ManaFlightActivationDelayMs = 450;
        private const long ManaFlightDeactivationDelayMs = 1200;
        private const float ManaFlightActivationRiseThreshold = 0.03f;
        private const float ManaFlightActivationFallThreshold = -0.12f;
        private const float ManaFlightSprintTriggerSpeed = 5.75f;
        private const float ManaFlightBoostSpeedThreshold = 12.5f;
        private const float ManaFlightBoostExtraManaPerSecond = 2f;
        private const float ManaFlightActivationMinRiseFromTakeoff = 1.15f;
        private const float ManaFlightActivationHorizontalGlideThreshold = 6.25f;
        private const float ManaFlightActivationSoftFallThreshold = -0.03f;
        private const long ManaFlightCrashWindowMs = 9000L;
        private const long NoManaWarningCooldownMs = 1500;
        private const long StarterWandResetHintCooldownMs = 4000L;
        private const string StarterWandResetHintMessage = "Use /bnmagic resetlineage to reset the wand";
        private const long VisualFeedbackCooldownMs = 150;
        private const float ManaGainIndicatorSeconds = 0.9f;
        private const float MissingManaIndicatorSeconds = 0.75f;
        private const float MissingItemIndicatorSeconds = 0.75f;
        private const int SkilledSwordTier1Flag = 1 << 0;
        private const int SkilledSwordTier2Flag = 1 << 1;
        private const int SkilledSwordTier3Flag = 1 << 2;
        private const string ManaHudLegacyKey = "BetterNecromancy.ManaHud";
        private const string ManaHudBackgroundKey = "BetterNecromancy.ManaHud.Background";
        private const string ManaHudFillKey = "BetterNecromancy.ManaHud.Fill";
        private const string ManaHudLabelKey = "BetterNecromancy.ManaHud.Label";
        private const string ManaHudImageKey = "BetterNecromancy.ManaHud.Image";
        private const string ManaHudImageTypePrefix = "BetterNecromancy.ManaBar";
        private const string WelcomeHudKey = "BetterNecromancy.WelcomeHud";
        private const string WandMasteryHudKey = "BetterNecromancy.WandMasteryHud";
        private const string ManaFlightHudKey = "BetterNecromancy.ManaFlightHud";
        private const string SaveKey = "BetterNecromancy.PlayerMagicState";
        private const string WandMasterySaveKey = "wandMastery";
        private const string LegendaryWandSaveKey = "legendaryWands";
        private const string LegendaryWandMasterySaveKey = "legendaryWandMastery";
        private const string WandLineageSaveKey = "wandLineage";
        private const string MeditationSaveKey = "meditation";
        private const int WandMasteryUsesPerLevel = 250;
        private const int WandMasteryMaxLevel = 10;
        private const float WandMasteryDamageBonusPerLevel = 0.20f;
        private const long WandMasteryHudFallbackDurationMs = 6000;
        private const int WandMasteryHudBottomOffsetY = 238;
        private const int MeditationHudManaBarOffsetY = 18;
        private const long WelcomeMessageDelayMs = 5000;
        private const long WelcomeOverlayDurationMs = 12000;
        private const string FirstWorldWelcomeMessage = "BetterNecromancy has been loaded. Special thanks to Blurry Yellow Thing. Enjoy! - Xell";
        private const string MeditationHudKey = "BetterNecromancy.MeditationHud";
        private const string WandEvolutionChoiceMenuId = "BetterNecromancy.WandEvolutionChoice";
        private const string WandEvolutionChoiceButtonPrefix = "BetterNecromancy.WandEvolutionChoice.";
        private const long WandEvolutionChoiceReopenIntervalMs = 15000L;
        private const long WandEvolutionChoiceInputLockMs = 1600L;
        private const long MeditationSampleIntervalMs = 250L;
        private const string BranchFrost = "Frost";
        private const string BranchEmber = "Ember";
        private const string BranchVenom = "Venom";
        private const string BranchStone = "Stone";

        public static class WandLineageEffects
        {
            public const string Poison = "Poison";
            public const string PoisonSpread = "PoisonSpread";
            public const string Burn = "Burn";
            public const string Explosion = "Explosion";
            public const string Freeze = "Freeze";
            public const string Shatter = "Shatter";
            public const string Stagger = "Stagger";
            public const string ArmorBreak = "ArmorBreak";
            public const string Chain = "Chain";
            public const string Bleed = "Bleed";
            public const string Bramble = "Bramble";
            public const string Rupture = "Rupture";
            public const string Prism = "Prism";
            public const string Quake = "Quake";
            public const string Ascended = "Ascended";
        }

        public sealed class WandLineageCombatProfile
        {
            public int Tier { get; set; }
            public int MasteryLevel { get; set; }
            public bool RitualAwakened { get; set; }
            public string CurrentForm { get; set; }
            public List<string> Effects { get; } = new List<string>();

            public bool HasEffect(string effectKey)
            {
                if (string.IsNullOrWhiteSpace(effectKey))
                    return false;

                for (var i = 0; i < Effects.Count; i++)
                {
                    if (string.Equals(Effects[i], effectKey, StringComparison.OrdinalIgnoreCase))
                        return true;
                }

                return false;
            }
        }

        private static readonly string[] WandMasteryOrder =
        {
            WandMasteryKeys.Mana,
            WandMasteryKeys.Briar,
            WandMasteryKeys.Spark,
            WandMasteryKeys.Venom,
            WandMasteryKeys.Ember,
            WandMasteryKeys.Frost,
            WandMasteryKeys.Crystal,
            WandMasteryKeys.Stone,
            WandMasteryKeys.Storm,
            WandMasteryKeys.Aether,
            WandMasteryKeys.Blood,
            WandMasteryKeys.Void
        };

        private static readonly string[] WandLineageBranches =
        {
            BranchFrost,
            BranchEmber,
            BranchVenom,
            BranchStone
        };

        private static readonly Dictionary<Players.Player, PlayerMagicState> States = new Dictionary<Players.Player, PlayerMagicState>();
        private static readonly string[] SelectedTypeMemberNames =
        {
            "SelectedType",
            "SelectedItem",
            "SelectedItemIndex",
            "SelectedTypeIndex",
            "HeldItem",
            "HeldItemType"
        };
        private static long _nextUpdate;
        private static long _nextManaFlightSampleUpdate;
        public static bool IsBootsOfFallingEquipped(Players.Player player)
        {
            return GetState(player).BootsOfFallingEquipped;
        }

        public static bool IsHealthBoosterEquipped(Players.Player player)
        {
            return GetState(player).HealthBoosterEquipped;
        }

        public static bool TryMarkFirstBossRewardGranted(Players.Player player)
        {
            var state = GetState(player);
            if (state.FirstBossRewardGranted)
                return false;

            state.FirstBossRewardGranted = true;
            return true;
        }

        public static bool TryEquipBootsOfFalling(Players.Player player)
        {
            var state = GetState(player);
            if (state.BootsOfFallingEquipped)
            {
                SendEquipChat(player, "Boots of Falling is already equipped.");
                return false;
            }

            if (!player.Inventory.TryRemove(Pandaros.Settlers.Items.Armor.Magical.BootsOfFalling.Item.ItemIndex))
            {
                ShowMissingItemIndicator(player, Pandaros.Settlers.Items.Armor.Magical.BootsOfFalling.Item.ItemIndex);
                SendEquipChat(player, "Boots of Falling could not be equipped because the item was not found.");
                return false;
            }

            state.BootsOfFallingEquipped = true;
            ShowEquipStateIndicator(player, Pandaros.Settlers.Items.Armor.Magical.BootsOfFalling.Item.ItemIndex, true);
            ItemUseAudioManager.Play(player, ItemUseAudioManager.BootsEquip);
            SendEquipChat(player, "Boots of Falling equipped.");
            return true;
        }

        public static bool TryEquipHealthBooster(Players.Player player)
        {
            var state = GetState(player);
            if (state.HealthBoosterEquipped)
            {
                SendEquipChat(player, "Health Booster is already equipped.");
                return false;
            }

            if (!player.Inventory.TryRemove(Pandaros.Settlers.Items.Magical.HealthBooster.Item.ItemIndex))
            {
                ShowMissingItemIndicator(player, Pandaros.Settlers.Items.Magical.HealthBooster.Item.ItemIndex);
                SendEquipChat(player, "Health Booster could not be equipped because the item was not found.");
                return false;
            }

            state.HealthBoosterEquipped = true;
            ShowEquipStateIndicator(player, Pandaros.Settlers.Items.Magical.HealthBooster.Item.ItemIndex, true);
            ItemUseAudioManager.Play(player, ItemUseAudioManager.HealthBoosterEquip);
            SendEquipChat(player, "Health Booster equipped.");
            return true;
        }

        public static bool TryEquipSkilledSword(Players.Player player, ushort itemIndex, int tier)
        {
            var state = GetState(player);
            var displayName = GetSkilledSwordDisplayName(tier);
            var tierFlag = GetSkilledSwordFlag(tier);
            if (tierFlag == 0)
            {
                SendEquipChat(player, "That Skilled Sword tier is invalid.");
                return false;
            }

            if ((state.SkilledSwordMask & tierFlag) != 0)
            {
                SendEquipChat(player, displayName + " is already equipped.");
                return false;
            }

            if (!player.Inventory.TryRemove(itemIndex))
            {
                ShowMissingItemIndicator(player, itemIndex);
                SendEquipChat(player, "The selected " + displayName + " could not be equipped because the item was not found.");
                return false;
            }

            state.SkilledSwordMask |= tierFlag;
            ShowEquipStateIndicator(player, itemIndex, true);
            ItemUseAudioManager.Play(player, ItemUseAudioManager.SwordEquip);
            SendEquipChat(player, displayName + " equipped. Active sword stack: " + GetSkilledSwordSummary(state) + ".");
            return true;
        }

        public static bool TryEquipArcaneFocus(Players.Player player, ushort itemIndex, int tier)
        {
            var state = GetState(player);
            var displayName = GetArcaneFocusDisplayName(tier);
            if (state.ArcaneFocusTier >= tier)
            {
                SendEquipChat(
                    player,
                    state.ArcaneFocusTier == tier
                        ? displayName + " is already equipped."
                        : "A higher Arcane Focus tier is already equipped.");
                return false;
            }

            if (!player.Inventory.TryRemove(itemIndex))
            {
                ShowMissingItemIndicator(player, itemIndex);
                SendEquipChat(player, "The selected " + displayName + " could not be equipped because the item was not found.");
                return false;
            }

            state.ArcaneFocusTier = tier;
            ShowEquipStateIndicator(player, itemIndex, true);
            ItemUseAudioManager.Play(player, ItemUseAudioManager.FocusEquip);
            SendEquipChat(player, displayName + " equipped.");
            return true;
        }

        public static bool TryEquipSkilledArmor(Players.Player player, MagicArmorSlot slot, ushort itemIndex, int tier, string displayName = null)
        {
            var state = GetState(player);
            var currentTier = GetSkilledArmorTier(state, slot);
            var armorDisplayName = string.IsNullOrEmpty(displayName)
                ? GetArmorDisplayName(slot, tier)
                : displayName;

            if (currentTier >= tier)
            {
                SendEquipChat(
                    player,
                    currentTier == tier
                        ? armorDisplayName + " is already equipped."
                        : "A higher " + GetArmorSlotName(slot).ToLowerInvariant() + " armor tier is already equipped.");
                return false;
            }

            if (!player.Inventory.TryRemove(itemIndex))
            {
                ShowMissingItemIndicator(player, itemIndex);
                SendEquipChat(player, "The selected " + armorDisplayName + " could not be equipped because the item was not found.");
                return false;
            }

            SetSkilledArmorTier(state, slot, tier);
            ShowEquipStateIndicator(player, itemIndex, true);
            ItemUseAudioManager.Play(player, ItemUseAudioManager.ArmorEquip);
            SendEquipChat(player, armorDisplayName + " equipped.");
            return true;
        }

        public static void ApplyHealthBoosterEquipFeedback(Players.Player player)
        {
            if (player == null)
                return;

            var healAmount = 10f * GetHealingReceivedMultiplier(player);
            if (player.Health > 0f && player.Health < player.HealthMax)
            {
                player.Health = Mathf.Min(player.HealthMax, player.Health + healAmount);
                player.SendHealthPacket();
            }

            ShowItemIndicator(player, Pandaros.Settlers.Items.Magical.HealthBooster.Item.ItemIndex, 0.8f, 0L);
            ServerManager.SendParticleTrail(player.PositionStanding + Vector3.up * 0.35f, player.PositionStanding + Vector3.up * 2.15f, 0.4f);
        }

        public static int GetSkilledSwordTier(Players.Player player)
        {
            return GetHighestSkilledSwordTier(GetState(player));
        }

        public static bool IsSkilledSwordTierEquipped(Players.Player player, int tier)
        {
            return (GetState(player).SkilledSwordMask & GetSkilledSwordFlag(tier)) != 0;
        }

        public static float GetSkilledSwordDamageBonus(Players.Player player)
        {
            return GetSkilledSwordDamageBonus(GetState(player));
        }

        public static string GetSkilledSwordSummary(Players.Player player)
        {
            return GetSkilledSwordSummary(GetState(player));
        }

        public static List<string> ClearEquippedMagicItems(Players.Player player)
        {
            var state = GetState(player);
            var removed = new List<string>();

            if (state.BootsOfFallingEquipped)
            {
                state.BootsOfFallingEquipped = false;
                removed.Add("Boots of Falling");
            }

            if (state.HealthBoosterEquipped)
            {
                state.HealthBoosterEquipped = false;
                removed.Add("Health Booster");
            }

            if (state.ManaFlightUnlocked)
            {
                state.ManaFlightUnlocked = false;
                state.ManaFlightEnabled = false;
                DisableManaFlight(player, state, null);
                removed.Add("Mana Flight");
            }

            if (state.ArcaneFocusTier > 0)
            {
                removed.Add(GetArcaneFocusDisplayName(state.ArcaneFocusTier));
                state.ArcaneFocusTier = 0;
            }

            if (state.SkilledSwordMask != 0)
            {
                removed.Add("Skilled Sword stack (" + GetSkilledSwordSummary(state) + ")");
                state.SkilledSwordMask = 0;
            }

            if (state.ManaCrystalLevel > 0)
            {
                removed.Add("Mana Crystal x" + state.ManaCrystalLevel);
                var nonCrystalManaBonus = System.Math.Max(0, state.MaxMana - DefaultMaxMana - (state.ManaCrystalLevel * ManaCrystalManaBonus));
                state.ManaCrystalLevel = 0;
                state.MaxMana = DefaultMaxMana + nonCrystalManaBonus;
                state.CurrentMana = System.Math.Min(state.CurrentMana, state.MaxMana);
                state.LastManaRegenAtMs = Pipliz.Time.MillisecondsSinceStart;
            }

            ClearEquippedArmorPiece(state, MagicArmorSlot.Helm, removed);
            ClearEquippedArmorPiece(state, MagicArmorSlot.Chest, removed);
            ClearEquippedArmorPiece(state, MagicArmorSlot.Gloves, removed);
            ClearEquippedArmorPiece(state, MagicArmorSlot.Legs, removed);
            ClearEquippedArmorPiece(state, MagicArmorSlot.Boots, removed);
            ClearEquippedArmorPiece(state, MagicArmorSlot.Shield, removed);

            if (removed.Count > 0)
            {
                state.LastManaHudSignature = null;
                UpdateManaHud(player, state, true);
            }

            return removed;
        }

        public static int GetArcaneFocusTier(Players.Player player)
        {
            return GetState(player).ArcaneFocusTier;
        }

        public static int GetSkilledArmorTier(Players.Player player, MagicArmorSlot slot)
        {
            return GetSkilledArmorTier(GetState(player), slot);
        }

        public static int GetCurrentMana(Players.Player player)
        {
            return GetState(player).CurrentMana;
        }

        public static int GetMaxMana(Players.Player player)
        {
            return GetState(player).MaxMana;
        }

        public static int GetManaCrystalLevel(Players.Player player)
        {
            return GetState(player).ManaCrystalLevel;
        }

        public static bool IsManaFlightActive(Players.Player player)
        {
            return GetState(player).ManaFlightActive;
        }

        public static bool IsManaFlightEnabled(Players.Player player)
        {
            return GetState(player).ManaFlightEnabled;
        }

        public static bool IsManaFlightUnlocked(Players.Player player)
        {
            return GetState(player).ManaFlightUnlocked;
        }

        public static long GetFlightMasterySeconds(Players.Player player)
        {
            return GetState(player).FlightMasterySeconds;
        }

        public static int GetFlightMasteryLevel(Players.Player player)
        {
            return GetFlightMasteryLevel(GetState(player));
        }

        public static string GetFlightMasteryLevelLabel(int level)
        {
            return BuildFlightMasteryLevelLabel(level);
        }

        public static string GetFlightMasteryProgressLabel(Players.Player player)
        {
            return GetFlightMasteryProgressLabel(GetState(player));
        }

        public static string GetFlightMasteryCompactSummary(Players.Player player)
        {
            return GetFlightMasteryCompactSummary(GetState(player));
        }

        public static string GetFlightManaCostDisplay(Players.Player player)
        {
            return GetFlightManaCostDisplay(GetState(player));
        }

        public static bool TrySetManaFlight(Players.Player player, bool enabled, out string message)
        {
            message = string.Empty;

            if (player == null)
            {
                message = "Flight could not be changed because the player was missing.";
                return false;
            }

            var state = GetState(player);
            EnsureManaDefaults(state);

            if (enabled && !state.ManaFlightUnlocked)
            {
                message = "Mana Flight is still locked. Craft and right-click the Mana Flight Harness first.";
                return false;
            }

            if (state.ManaFlightEnabled == enabled)
            {
                message = enabled
                    ? "Mana Flight is already enabled. Press F to toggle flight."
                    : "Mana Flight is already disabled.";
                return true;
            }

            state.ManaFlightEnabled = enabled;

            if (!enabled)
            {
                DisableManaFlight(player, state, null);
                message = "Mana Flight disabled. F will no longer activate it.";
                return true;
            }

            if (!player.HasFlightMode)
            {
                player.SetFlightMode(true);
                state.ManaFlightPermissionGranted = true;
            }

            state.NextManaFlightDrainAtMs = 0L;
            message = "Mana Flight enabled. Press F to toggle flight. Early mastery tiers are unstable and drain extra mana.";
            return true;
        }

        public static bool TryUnlockManaFlight(Players.Player player, ushort itemIndex)
        {
            if (player == null || itemIndex == 0)
                return false;

            var state = GetState(player);
            EnsureManaDefaults(state);

            if (state.ManaFlightUnlocked)
            {
                SendEquipChat(player, "Mana Flight is already unlocked.");
                return false;
            }

            if (!player.Inventory.TryRemove(itemIndex))
            {
                ShowMissingItemIndicator(player, itemIndex);
                SendEquipChat(player, "Mana Flight could not be unlocked because the item was not found.");
                return false;
            }

            state.ManaFlightUnlocked = true;
            state.ManaFlightEnabled = true;
            ShowEquipStateIndicator(player, itemIndex, true);
            ItemUseAudioManager.Play(player, ItemUseAudioManager.FocusEquip);
            SendEquipChat(player, "Mana Flight unlocked. Press F to toggle it while you have mana.");
            return true;
        }

        public static IEnumerable<string> GetWandMasteryKeys()
        {
            return WandMasteryOrder;
        }

        public static int GetWandMasteryUses(Players.Player player, string masteryKey)
        {
            return GetWandMasteryUses(GetState(player), masteryKey);
        }

        public static int GetLegendaryWandMasteryUses(Players.Player player, string masteryKey)
        {
            return GetLegendaryWandMasteryUses(GetState(player), masteryKey);
        }

        public static bool IsWandLegendaryEvolutionUnlocked(Players.Player player, string masteryKey)
        {
            return IsWandLegendaryEvolutionUnlocked(GetState(player), masteryKey);
        }

        public static bool TryUnlockWandLegendaryEvolution(Players.Player player, string masteryKey)
        {
            if (player == null || string.IsNullOrWhiteSpace(masteryKey))
                return false;

            var state = GetState(player);
            EnsureLegendaryWandDefaults(state);
            var normalizedKey = NormalizeWandMasteryKey(masteryKey);
            if (string.IsNullOrEmpty(normalizedKey) || IsWandLegendaryEvolutionUnlocked(state, normalizedKey))
                return false;

            state.LegendaryWandUnlocks[normalizedKey] = true;
            CacheObservedWand(state, normalizedKey, GetWandMasteryDisplayName(normalizedKey));
            return true;
        }

        public static string GetWandLegendaryDisplayName(Players.Player player, string masteryKey)
        {
            return IsWandLegendaryEvolutionUnlocked(player, masteryKey)
                ? LegendaryWandEvolutionManager.GetLegendaryName(masteryKey)
                : GetWandMasteryDisplayName(masteryKey);
        }

        public static string GetWandLegendaryCompactSummary(Players.Player player, string masteryKey)
        {
            if (IsWandLegendaryEvolutionUnlocked(player, masteryKey))
            {
                var level = GetLegendaryWandMasteryLevel(player, masteryKey);
                return LegendaryWandEvolutionManager.GetLegendarySummary(masteryKey) + " Evolution " + GetWandMasteryLevelLabel(level) + ".";
            }

            return GetWandMasteryLevel(player, masteryKey) >= WandProgressionConfig.Current.Lineage.MaxMasteryLevel
                ? "Evolution ready on next successful cast."
                : "Reach Mastery 10 to awaken this wand automatically.";
        }

        public static int GetWandMasteryLevel(Players.Player player, string masteryKey)
        {
            return GetWandMasteryLevel(GetState(player), masteryKey);
        }

        public static int GetLegendaryWandMasteryLevel(Players.Player player, string masteryKey)
        {
            return GetLegendaryWandMasteryLevel(GetState(player), masteryKey);
        }

        public static int GetWandMasteryUsesForCurrentLevel(Players.Player player, string masteryKey)
        {
            return GetWandMasteryUsesForCurrentLevel(GetWandMasteryUses(player, masteryKey), GetWandMasteryLevel(player, masteryKey));
        }

        public static int GetLegendaryWandMasteryUsesForCurrentLevel(Players.Player player, string masteryKey)
        {
            return GetWandMasteryUsesForCurrentLevel(GetLegendaryWandMasteryUses(player, masteryKey), GetLegendaryWandMasteryLevel(player, masteryKey));
        }

        public static int GetWandMasteryUsesRequiredForNextLevel(Players.Player player, string masteryKey)
        {
            return GetWandMasteryUsesRequiredForNextLevel(GetWandMasteryLevel(player, masteryKey));
        }

        public static int GetLegendaryWandMasteryUsesRequiredForNextLevel(Players.Player player, string masteryKey)
        {
            return GetWandMasteryUsesRequiredForNextLevel(GetLegendaryWandMasteryLevel(player, masteryKey));
        }

        public static string GetWandMasteryDisplayName(string masteryKey)
        {
            switch (NormalizeWandMasteryKey(masteryKey))
            {
                case WandMasteryKeys.Mana: return "Mana Wand";
                case WandMasteryKeys.Briar: return "Briar Wand";
                case WandMasteryKeys.Spark: return "Spark Wand";
                case WandMasteryKeys.Venom: return "Venom Wand";
                case WandMasteryKeys.Ember: return "Ember Wand";
                case WandMasteryKeys.Frost: return "Frost Wand";
                case WandMasteryKeys.Crystal: return "Crystal Wand";
                case WandMasteryKeys.Stone: return "Stone Wand";
                case WandMasteryKeys.Storm: return "Storm Wand";
                case WandMasteryKeys.Aether: return "Aether Wand";
                case WandMasteryKeys.Blood: return "Blood Wand";
                case WandMasteryKeys.Void: return "Void Wand";
                default: return masteryKey ?? "Unknown Wand";
            }
        }

        public static string GetWandMasteryLevelLabel(int level)
        {
            return level > 0 ? "Mastery " + level : "Untrained";
        }

        public static string GetWandMasteryBonusSummary(string masteryKey, int level)
        {
            if (level <= 0)
                return "No mastery bonus yet.";

            switch (NormalizeWandMasteryKey(masteryKey))
            {
                case WandMasteryKeys.Mana:
                    return $"+{(GetWandMasteryDamageMultiplierForLevel(masteryKey, level) - 1f) * 100f:0}% damage. Mastery II restores mana on hit; Mastery III adds a Mana Pulse rebound.";
                case WandMasteryKeys.Briar:
                    return $"+{(GetWandMasteryDotMultiplierForLevel(masteryKey, level) - 1f) * 100f:0}% thorn damage over time. Mastery II+ creates a bramble ring around the impact.";
                case WandMasteryKeys.Spark:
                    return $"+{GetWandMasteryExtraTargetsForLevel(masteryKey, level)} chain target(s) and faster shock recovery. Mastery II+ fires rebound lightning.";
                case WandMasteryKeys.Venom:
                    return $"+{(GetWandMasteryDotMultiplierForLevel(masteryKey, level) - 1f) * 100f:0}% poison damage over time. Mastery II+ spreads plague blooms to nearby enemies.";
                case WandMasteryKeys.Ember:
                    return $"+{(GetWandMasteryDotMultiplierForLevel(masteryKey, level) - 1f) * 100f:0}% burn damage over time. Mastery II+ ignites a second cinder nova.";
                case WandMasteryKeys.Frost:
                    return $"{(GetWandMasteryForceMultiplierForLevel(masteryKey, level) - 1f) * 100f:0}% more impact force and faster frost cadence. Mastery II+ triggers a shatter pulse.";
                case WandMasteryKeys.Crystal:
                    return $"+{GetWandMasteryRangeBonusForLevel(masteryKey, level):0.#} range and +{(GetWandMasteryDamageMultiplierForLevel(masteryKey, level) - 1f) * 100f:0}% damage. Mastery II+ splits into prism lashes.";
                case WandMasteryKeys.Stone:
                    return $"{(GetWandMasteryForceMultiplierForLevel(masteryKey, level) - 1f) * 100f:0}% more impact force and +{(GetWandMasteryDamageMultiplierForLevel(masteryKey, level) - 1f) * 100f:0}% damage. Mastery II+ causes aftershocks.";
                case WandMasteryKeys.Storm:
                    return $"+{GetWandMasteryExtraTargetsForLevel(masteryKey, level)} chain target(s) and wider Tempest coverage. Mastery II+ calls an echo strike.";
                case WandMasteryKeys.Aether:
                    return $"+{(GetWandMasteryHealMultiplierForLevel(masteryKey, level) - 1f) * 100f:0}% healing, cheaper support casts and more wave targets. Mastery II+ adds an echo heal and mana return.";
                case WandMasteryKeys.Blood:
                    return $"+{(GetWandMasteryHealMultiplierForLevel(masteryKey, level) - 1f) * 100f:0}% lifesteal and +{(GetWandMasteryDotMultiplierForLevel(masteryKey, level) - 1f) * 100f:0}% bleed damage over time. Mastery II+ creates a siphon burst.";
                case WandMasteryKeys.Void:
                    return $"+{(GetWandMasteryDotMultiplierForLevel(masteryKey, level) - 1f) * 100f:0}% rupture damage over time and larger void pulls. Mastery II+ triggers a collapse echo.";
                default:
                    return "Mastery bonus active.";
            }
        }

        public static string GetWandMasteryProgressLabel(Players.Player player, string masteryKey)
        {
            var needed = GetWandMasteryUsesRequiredForNextLevel(player, masteryKey);
            if (needed <= 0)
                return "MAX";

            return GetWandMasteryUsesForCurrentLevel(player, masteryKey) + "/" + needed;
        }

        public static string GetLegendaryWandMasteryProgressLabel(Players.Player player, string masteryKey)
        {
            var needed = GetLegendaryWandMasteryUsesRequiredForNextLevel(player, masteryKey);
            if (needed <= 0)
                return "MAX";

            return GetLegendaryWandMasteryUsesForCurrentLevel(player, masteryKey) + "/" + needed;
        }

        public static string GetWandMasteryCompactSummary(string masteryKey, int level)
        {
            if (level <= 0)
                return "First mastery spike at 250 casts.";

            switch (NormalizeWandMasteryKey(masteryKey))
            {
                case WandMasteryKeys.Mana:
                    return $"+{(GetWandMasteryDamageMultiplierForLevel(masteryKey, level) - 1f) * 100f:0}% dmg, mana on hit, Mana Pulse at III.";
                case WandMasteryKeys.Briar:
                    return $"+{(GetWandMasteryDotMultiplierForLevel(masteryKey, level) - 1f) * 100f:0}% thorn DOT, bramble ring.";
                case WandMasteryKeys.Spark:
                    return $"+{GetWandMasteryExtraTargetsForLevel(masteryKey, level)} chain, rebound lightning.";
                case WandMasteryKeys.Venom:
                    return $"+{(GetWandMasteryDotMultiplierForLevel(masteryKey, level) - 1f) * 100f:0}% poison DOT, plague blooms.";
                case WandMasteryKeys.Ember:
                    return $"+{(GetWandMasteryDotMultiplierForLevel(masteryKey, level) - 1f) * 100f:0}% burn DOT, cinder nova.";
                case WandMasteryKeys.Frost:
                    return $"+{(GetWandMasteryForceMultiplierForLevel(masteryKey, level) - 1f) * 100f:0}% force, shatter pulse.";
                case WandMasteryKeys.Crystal:
                    return $"+{GetWandMasteryRangeBonusForLevel(masteryKey, level):0.#} range, prism lashes.";
                case WandMasteryKeys.Stone:
                    return $"+{(GetWandMasteryDamageMultiplierForLevel(masteryKey, level) - 1f) * 100f:0}% dmg, aftershocks.";
                case WandMasteryKeys.Storm:
                    return $"+{GetWandMasteryExtraTargetsForLevel(masteryKey, level)} chain, echo strike.";
                case WandMasteryKeys.Aether:
                    return $"+{(GetWandMasteryHealMultiplierForLevel(masteryKey, level) - 1f) * 100f:0}% healing, echo heal.";
                case WandMasteryKeys.Blood:
                    return $"+{(GetWandMasteryDotMultiplierForLevel(masteryKey, level) - 1f) * 100f:0}% bleed, siphon burst.";
                case WandMasteryKeys.Void:
                    return $"+{(GetWandMasteryDotMultiplierForLevel(masteryKey, level) - 1f) * 100f:0}% rupture, collapse echo.";
                default:
                    return "Signature effect active.";
            }
        }

        public static bool TryGetSelectedWandMastery(Players.Player player, out string masteryKey, out string displayName)
        {
            masteryKey = null;
            displayName = null;

            var state = GetState(player);
            if (TryResolveSelectedType(player, out var selectedType) &&
                TryResolveSelectedWandMastery(selectedType, out masteryKey, out displayName))
            {
                CacheObservedWand(state, masteryKey, displayName);
                return true;
            }

            var now = Pipliz.Time.MillisecondsSinceStart;
            if (state.LastObservedWandUntilMs > now &&
                !string.IsNullOrWhiteSpace(state.LastObservedWandMasteryKey) &&
                !string.IsNullOrWhiteSpace(state.LastObservedWandDisplayName))
            {
                masteryKey = state.LastObservedWandMasteryKey;
                displayName = state.LastObservedWandDisplayName;
                return true;
            }

            return false;
        }

        public static int RecordWandMasteryUse(Players.Player player, string masteryKey, ushort itemIndex, int amount)
        {
            if (player == null || string.IsNullOrWhiteSpace(masteryKey) || amount <= 0)
                return 0;

            var state = GetState(player);
            EnsureWandMasteryDefaults(state);
            EnsureLegendaryWandDefaults(state);
            EnsureLegendaryWandMasteryDefaults(state);

            var normalizedKey = NormalizeWandMasteryKey(masteryKey);
            if (TryRecordLineageMasteryUse(player, state, normalizedKey, itemIndex, amount, "OnCast", out var lineageLevel))
                return lineageLevel;

            if (IsWandLegendaryEvolutionUnlocked(state, normalizedKey))
            {
                TryRepairEvolvedWandItem(player, normalizedKey, itemIndex);

                var previousLegendaryLevel = GetLegendaryWandMasteryLevel(state, normalizedKey);
                state.LegendaryWandMasteryUses[normalizedKey] = GetLegendaryWandMasteryUses(state, normalizedKey) + amount;
                CacheObservedWand(state, normalizedKey, LegendaryWandEvolutionManager.GetLegendaryName(normalizedKey));
                var newLegendaryLevel = GetLegendaryWandMasteryLevel(state, normalizedKey);

                if (newLegendaryLevel > previousLegendaryLevel)
                {
                    if (itemIndex != 0)
                        ShowItemIndicator(player, itemIndex, 0.95f, 0L);

                    SendEquipChat(player, LegendaryWandEvolutionManager.GetLegendaryName(normalizedKey) + " reached Evolution " + GetWandMasteryLevelLabel(newLegendaryLevel) + ". +" + (newLegendaryLevel * WandProgressionConfig.Current.Lineage.DamageBonusPerMasteryLevel * 100f).ToString("0") + "% evolved damage.");
                }

                return newLegendaryLevel;
            }

            var previousLevel = GetWandMasteryLevel(state, normalizedKey);
            state.WandMasteryUses[normalizedKey] = GetWandMasteryUses(state, normalizedKey) + amount;
            CacheObservedWand(state, normalizedKey, GetWandMasteryDisplayName(normalizedKey));
            var newLevel = GetWandMasteryLevel(state, normalizedKey);

            if (newLevel > previousLevel)
            {
                if (itemIndex != 0)
                    ShowItemIndicator(player, itemIndex, 0.95f, 0L);

                SendEquipChat(player, GetWandMasteryDisplayName(normalizedKey) + " reached " + GetWandMasteryLevelLabel(newLevel) + ". " + GetWandMasteryBonusSummary(normalizedKey, newLevel));
            }

            if (newLevel >= WandProgressionConfig.Current.Lineage.MaxMasteryLevel && !IsWandLegendaryEvolutionUnlocked(state, normalizedKey))
                TryAutoEvolveWand(player, state, normalizedKey, itemIndex);

            return newLevel;
        }

        public static void RecordWandMasteryEvent(Players.Player player, string masteryKey, ushort itemIndex, string source, int amount = 1)
        {
            if (player == null || string.IsNullOrWhiteSpace(masteryKey) || itemIndex == 0 || amount <= 0)
                return;

            var state = GetState(player);
            TryRecordLineageMasteryUse(player, state, NormalizeWandMasteryKey(masteryKey), itemIndex, amount, source, out _);
        }

        public static float GetWandMasteryDamageMultiplier(Players.Player player, string masteryKey)
        {
            var normalizedKey = NormalizeWandMasteryKey(masteryKey);
            return GetWandMasteryDamageMultiplierForLevel(normalizedKey, GetEffectiveWandDamageMasteryLevel(player, normalizedKey));
        }

        public static float GetWandMasteryDotMultiplier(Players.Player player, string masteryKey)
        {
            var normalizedKey = NormalizeWandMasteryKey(masteryKey);
            return GetWandMasteryDotMultiplierForLevel(normalizedKey, GetEffectiveWandDamageMasteryLevel(player, normalizedKey));
        }

        public static float GetWandMasteryHealMultiplier(Players.Player player, string masteryKey)
        {
            var normalizedKey = NormalizeWandMasteryKey(masteryKey);
            var multiplier = GetWandMasteryHealMultiplierForLevel(normalizedKey, GetWandMasteryLevel(player, normalizedKey));
            if (IsWandLegendaryEvolutionUnlocked(player, normalizedKey))
                multiplier *= GetLegendaryHealMultiplier(normalizedKey);
            return multiplier;
        }

        public static float GetWandMasteryForceMultiplier(Players.Player player, string masteryKey)
        {
            var normalizedKey = NormalizeWandMasteryKey(masteryKey);
            var multiplier = GetWandMasteryForceMultiplierForLevel(normalizedKey, GetWandMasteryLevel(player, normalizedKey));
            if (IsWandLegendaryEvolutionUnlocked(player, normalizedKey))
                multiplier *= GetLegendaryForceMultiplier(normalizedKey);
            return multiplier;
        }

        public static int GetWandMasteryCooldownReductionMs(Players.Player player, string masteryKey)
        {
            var normalizedKey = NormalizeWandMasteryKey(masteryKey);
            var reduction = GetWandMasteryCooldownReductionMsForLevel(normalizedKey, GetWandMasteryLevel(player, normalizedKey));
            if (IsWandLegendaryEvolutionUnlocked(player, normalizedKey))
                reduction += GetLegendaryCooldownReductionMs(normalizedKey);
            return reduction;
        }

        public static int GetWandMasteryManaDiscount(Players.Player player, string masteryKey)
        {
            var normalizedKey = NormalizeWandMasteryKey(masteryKey);
            var discount = GetWandMasteryManaDiscountForLevel(normalizedKey, GetWandMasteryLevel(player, normalizedKey));
            if (IsWandLegendaryEvolutionUnlocked(player, normalizedKey))
                discount += GetLegendaryManaDiscount(normalizedKey);
            return discount;
        }

        public static int GetWandMasteryExtraTargets(Players.Player player, string masteryKey)
        {
            var normalizedKey = NormalizeWandMasteryKey(masteryKey);
            var extraTargets = GetWandMasteryExtraTargetsForLevel(normalizedKey, GetWandMasteryLevel(player, normalizedKey));
            if (IsWandLegendaryEvolutionUnlocked(player, normalizedKey))
                extraTargets += GetLegendaryExtraTargets(normalizedKey);
            return extraTargets;
        }

        public static float GetWandMasteryRadiusBonus(Players.Player player, string masteryKey)
        {
            var normalizedKey = NormalizeWandMasteryKey(masteryKey);
            var radius = GetWandMasteryRadiusBonusForLevel(normalizedKey, GetWandMasteryLevel(player, normalizedKey));
            if (IsWandLegendaryEvolutionUnlocked(player, normalizedKey))
                radius += GetLegendaryRadiusBonus(normalizedKey);
            return radius;
        }

        public static float GetWandMasteryRangeBonus(Players.Player player, string masteryKey)
        {
            var normalizedKey = NormalizeWandMasteryKey(masteryKey);
            var range = GetWandMasteryRangeBonusForLevel(normalizedKey, GetWandMasteryLevel(player, normalizedKey));
            if (IsWandLegendaryEvolutionUnlocked(player, normalizedKey))
                range += GetLegendaryRangeBonus(normalizedKey);
            return range;
        }

        public static int RestoreMana(Players.Player player, int amount, ushort indicatorItemIndex = 0)
        {
            if (player == null || amount <= 0)
                return 0;

            var state = GetState(player);
            EnsureManaDefaults(state);

            var previousMana = state.CurrentMana;
            state.CurrentMana = System.Math.Min(state.MaxMana, state.CurrentMana + amount);
            var restored = state.CurrentMana - previousMana;
            if (restored <= 0)
                return 0;

            state.LastManaRegenAtMs = Pipliz.Time.MillisecondsSinceStart;
            UpdateManaHud(player, state, true);

            if (indicatorItemIndex != 0)
                ShowItemIndicator(player, indicatorItemIndex, 0.6f, 0L);

            return restored;
        }

        public static void RefreshAllManaHud()
        {
            foreach (var pair in States)
            {
                pair.Value.LastManaHudSignature = null;
                UpdateManaHud(pair.Key, pair.Value, true);
            }
        }

        public static bool HasEnoughMana(Players.Player player, int amount)
        {
            if (amount <= 0)
                return true;

            var state = GetState(player);
            EnsureManaDefaults(state);
            return state.CurrentMana >= amount;
        }

        public static void WarnNotEnoughMana(Players.Player player, string missingMessage)
        {
            var state = GetState(player);
            EnsureManaDefaults(state);

            var now = Pipliz.Time.MillisecondsSinceStart;
            if (now >= state.NextNoManaWarningAtMs)
            {
                state.NextNoManaWarningAtMs = now + NoManaWarningCooldownMs;
                ShowIndicator(player, IndicatorState.NewMissingItemIndicator(MissingManaIndicatorSeconds, Pandaros.Settlers.Items.Mana.Item.ItemIndex), NoManaWarningCooldownMs);
            }
        }

        public static bool TrySpendMana(Players.Player player, int amount, string missingMessage)
        {
            if (amount <= 0)
                return true;

            var state = GetState(player);
            EnsureManaDefaults(state);

            if (state.CurrentMana < amount)
            {
                WarnNotEnoughMana(player, missingMessage);
                return false;
            }

            state.CurrentMana -= amount;
            state.LastManaRegenAtMs = Pipliz.Time.MillisecondsSinceStart;
            UpdateManaHud(player, state, true);
            return true;
        }

        public static bool TryConsumeManaBottle(Players.Player player)
        {
            if (!player.Inventory.TryRemove(Pandaros.Settlers.Items.Mana.Item.ItemIndex))
                return false;

            var state = GetState(player);
            EnsureManaDefaults(state);

            state.MaxMana += 1;
            state.CurrentMana = state.MaxMana;
            state.LastManaRegenAtMs = Pipliz.Time.MillisecondsSinceStart;

            UpdateManaHud(player, state, true);
            ShowIndicator(player, IndicatorState.NewItemIndicator(ManaGainIndicatorSeconds, Pandaros.Settlers.Items.Mana.Item.ItemIndex), 0L);
            ServerManager.SendParticleTrail(player.PositionStanding + Vector3.up * 0.4f, player.PositionStanding + Vector3.up * 2f, 0.35f);
            ItemUseAudioManager.Play(player, ItemUseAudioManager.ManaBottleUse);
            SendEquipChat(player, "Mana absorbed. Max mana is now " + state.MaxMana + ".");
            return true;
        }

        public static bool TryConsumeManaCrystal(Players.Player player)
        {
            if (!player.Inventory.TryRemove(Pandaros.Settlers.Items.Magical.ManaCrystal.Item.ItemIndex))
                return false;

            var state = GetState(player);
            EnsureManaDefaults(state);

            state.ManaCrystalLevel += 1;
            state.MaxMana += ManaCrystalManaBonus;
            state.CurrentMana = state.MaxMana;
            state.LastManaRegenAtMs = Pipliz.Time.MillisecondsSinceStart;

            UpdateManaHud(player, state, true);
            ShowIndicator(player, IndicatorState.NewItemIndicator(ManaGainIndicatorSeconds, Pandaros.Settlers.Items.Magical.ManaCrystal.Item.ItemIndex), 0L);
            ServerManager.SendParticleTrail(player.PositionStanding + Vector3.up * 0.45f, player.PositionStanding + Vector3.up * 2.5f, 0.55f);
            ItemUseAudioManager.Play(player, ItemUseAudioManager.ManaCrystalAbsorb);
            SendEquipChat(player, "Mana Crystal absorbed. Max mana is now " + state.MaxMana + ".");
            return true;
        }

        public static void ShowMissingItemIndicator(Players.Player player, ushort itemIndex, long cooldownMs = NoManaWarningCooldownMs)
        {
            ShowIndicator(player, IndicatorState.NewMissingItemIndicator(MissingItemIndicatorSeconds, itemIndex), cooldownMs);
        }

        public static void ShowItemIndicator(Players.Player player, ushort itemIndex, float seconds = 0.55f, long cooldownMs = 0L)
        {
            ShowIndicator(player, IndicatorState.NewItemIndicator(seconds, itemIndex), cooldownMs);
        }

        public static void ShowEquipStateIndicator(Players.Player player, ushort itemIndex, bool equipped)
        {
            if (equipped)
            {
                ShowItemIndicator(player, itemIndex, 0.8f, 0L);
                ServerManager.SendParticleTrail(player.PositionStanding + Vector3.up * 0.35f, player.PositionStanding + Vector3.up * 2.1f, 0.35f);
                return;
            }

            ShowMissingItemIndicator(player, itemIndex, 0L);
        }

        public static float GetHealingReceivedMultiplier(Players.Player player)
        {
            switch (GetSkilledArmorTier(player, MagicArmorSlot.Chest))
            {
                case 1:
                    return 1.15f;
                case 2:
                    return 1.3f;
                case 3:
                    return 1.5f;
                default:
                    return 1f;
            }
        }

        public static float GetMagicReagentPreserveChance(Players.Player player)
        {
            switch (GetSkilledArmorTier(player, MagicArmorSlot.Gloves))
            {
                case 1:
                    return 0.1f;
                case 2:
                    return 0.2f;
                case 3:
                    return 0.3f;
                default:
                    return 0f;
            }
        }

        public static int GetMagicCooldownReductionMs(Players.Player player)
        {
            var cooldownReduction = 0;

            switch (GetSkilledArmorTier(player, MagicArmorSlot.Gloves))
            {
                case 1:
                    cooldownReduction += 100;
                    break;
                case 2:
                    cooldownReduction += 175;
                    break;
                case 3:
                    cooldownReduction += 250;
                    break;
            }

            switch (GetArcaneFocusTier(player))
            {
                case 1:
                    cooldownReduction += 40;
                    break;
                case 2:
                    cooldownReduction += 80;
                    break;
                case 3:
                    cooldownReduction += 120;
                    break;
            }

            cooldownReduction += BetterNecromancy.WorldEventManager.GetMagicCooldownReductionMsBonus();
            return cooldownReduction;
        }

        public static float GetMagicSpellDamageMultiplier(Players.Player player)
        {
            var multiplier = 1f;

            switch (GetArcaneFocusTier(player))
            {
                case 1:
                    multiplier *= 1.08f;
                    break;
                case 2:
                    multiplier *= 1.16f;
                    break;
                case 3:
                    multiplier *= 1.25f;
                    break;
            }

            if (GetSkilledArmorPieceCount(player) >= 2)
                multiplier *= 1.08f;

            var state = GetState(player);
            if (state.WandLineageUnlocked && (state.WandLineageRitualAwakened || state.WandLineageTier >= 6))
                multiplier *= Pandaros.Settlers.Items.RitualWandManager.GetDamageMultiplier();

            multiplier *= BetterNecromancy.WorldEventManager.GetMagicSpellDamageMultiplier();
            return multiplier;
        }

        public static int GetMagicManaCostReduction(Players.Player player, int baseCost)
        {
            var reduction = 0;

            switch (GetArcaneFocusTier(player))
            {
                case 2:
                    reduction += baseCost >= 3 ? 1 : 0;
                    break;
                case 3:
                    reduction += baseCost >= 2 ? 1 : 0;
                    break;
            }

            if (GetSkilledArmorPieceCount(player) >= 6 && baseCost >= 2)
                reduction += 1;

            reduction += BetterNecromancy.WorldEventManager.GetMagicManaCostReductionBonus(baseCost);
            return reduction;
        }

        public static float GetPlayerClickDamageBonus(Players.Player player)
        {
            switch (GetSkilledArmorTier(player, MagicArmorSlot.Helm))
            {
                case 1:
                    return 10f;
                case 2:
                    return 20f;
                case 3:
                    return 35f;
                default:
                    return 0f;
            }
        }

        public static float GetPlayerClickCritChance(Players.Player player)
        {
            switch (GetSkilledArmorTier(player, MagicArmorSlot.Legs))
            {
                case 1:
                    return 0.05f;
                case 2:
                    return 0.1f;
                case 3:
                    return 0.15f;
                default:
                    return 0f;
            }
        }

        public static float GetPlayerClickCritBonus(Players.Player player)
        {
            switch (GetSkilledArmorTier(player, MagicArmorSlot.Legs))
            {
                case 1:
                    return 15f;
                case 2:
                    return 30f;
                case 3:
                    return 45f;
                default:
                    return 0f;
            }
        }

        public static float GetShieldDamageReduction(Players.Player player)
        {
            switch (GetSkilledArmorTier(player, MagicArmorSlot.Shield))
            {
                case 1:
                    return 0.1f;
                case 2:
                    return 0.2f;
                case 3:
                    return 0.3f;
                default:
                    return 0f;
            }
        }

        public static float GetBootsFallReduction(Players.Player player)
        {
            if (IsBootsOfFallingEquipped(player))
                return 1f;

            switch (GetSkilledArmorTier(player, MagicArmorSlot.Boots))
            {
                case 1:
                    return 0.25f;
                case 2:
                    return 0.5f;
                case 3:
                    return 0.75f;
                default:
                    return 0f;
            }
        }

        public static int GetSkilledArmorPieceCount(Players.Player player)
        {
            var state = GetState(player);
            var count = 0;

            if (state.SkilledHelmTier > 0)
                count++;
            if (state.SkilledChestTier > 0)
                count++;
            if (state.SkilledGlovesTier > 0)
                count++;
            if (state.SkilledLegsTier > 0)
                count++;
            if (state.SkilledBootsTier > 0)
                count++;
            if (state.SkilledShieldTier > 0)
                count++;

            return count;
        }

        public static int GetSkilledArmorSetBonusStage(Players.Player player)
        {
            var pieces = GetSkilledArmorPieceCount(player);

            if (pieces >= 6)
                return 3;
            if (pieces >= 4)
                return 2;
            if (pieces >= 2)
                return 1;

            return 0;
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnLoadingPlayer, BetterNecromancy.ModEntry.Namespace + ".PlayerMagicStateManager.OnLoadingPlayer")]
        public static void OnLoadingPlayer(JObject miscJson, Players.Player player)
        {
            var state = GetState(player);
            if (!(miscJson?[SaveKey] is JObject stateJson))
            {
                state.WelcomeMessageSentThisSession = false;
                LoadMeditation(state, null);
                state.LastManaHudSignature = null;
                state.LastManaRegenAtMs = Pipliz.Time.MillisecondsSinceStart;
                state.WelcomeMessageReadyAtMs = 0L;
                EnsureManaDefaults(state);
                return;
            }

            state.FirstBossRewardGranted = stateJson.Value<bool?>("firstBossRewardGranted") ?? false;
            state.WelcomeMessageSentThisSession = false;
            state.BootsOfFallingEquipped = stateJson.Value<bool?>("bootsOfFallingEquipped") ?? false;
            state.HealthBoosterEquipped = stateJson.Value<bool?>("healthBoosterEquipped") ?? false;
            state.ManaFlightEnabled = stateJson.Value<bool?>("manaFlightEnabled") ?? true;
            state.ManaFlightUnlocked = stateJson.Value<bool?>("manaFlightUnlocked")
                ?? (stateJson["manaFlightEnabled"] != null || (stateJson.Value<long?>("flightMasterySeconds") ?? 0L) > 0L);
            state.ManaFlightActive = false;
            state.ManaFlightPermissionGranted = false;
            state.ManaFlightUnsupportedSinceAtMs = 0L;
            state.NextManaFlightDrainAtMs = 0L;
            state.PendingManaFlightCrashUntilMs = 0L;
            state.ManaFlightCostCarry = 0f;
            state.ManaFlightAirborneSinceAtMs = 0L;
            state.ManaFlightGroundedSinceAtMs = 0L;
            state.LastManaFlightActiveAtMs = 0L;
            state.ManaFlightTakeoffY = 0f;
            state.LastManaFlightHorizontalSpeed = 0f;
            state.ManaFlightSampleInitialized = false;
            state.LastManaFlightSamplePosition = Vector3.zero;
            state.ManaFlightTakeoffPosition = Vector3.zero;
            state.LastManaFlightSampleAtMs = 0L;
            state.FlightMasterySeconds = stateJson.Value<long?>("flightMasterySeconds") ?? 0L;
            state.MaxMana = stateJson.Value<int?>("maxMana") ?? DefaultMaxMana;
            state.CurrentMana = stateJson.Value<int?>("currentMana") ?? state.MaxMana;
            state.ManaCrystalLevel = stateJson.Value<int?>("manaCrystalLevel") ?? 0;
            state.ArcaneFocusTier = stateJson.Value<int?>("arcaneFocusTier") ?? 0;
            state.SkilledSwordMask = stateJson.Value<int?>("skilledSwordMask") ?? GetLegacySkilledSwordMask(stateJson.Value<int?>("skilledSwordTier") ?? 0);
            state.SkilledHelmTier = stateJson.Value<int?>("skilledHelmTier") ?? 0;
            state.SkilledChestTier = stateJson.Value<int?>("skilledChestTier") ?? 0;
            state.SkilledGlovesTier = stateJson.Value<int?>("skilledGlovesTier") ?? 0;
            state.SkilledLegsTier = stateJson.Value<int?>("skilledLegsTier") ?? 0;
            state.SkilledBootsTier = stateJson.Value<int?>("skilledBootsTier") ?? 0;
            state.SkilledShieldTier = stateJson.Value<int?>("skilledShieldTier") ?? 0;
            LoadWandMastery(state, stateJson[WandMasterySaveKey] as JObject);
            LoadLegendaryWands(state, stateJson[LegendaryWandSaveKey] as JObject);
            LoadLegendaryWandMastery(state, stateJson[LegendaryWandMasterySaveKey] as JObject);
            LoadWandLineage(state, stateJson[WandLineageSaveKey] as JObject);
            LoadMeditation(state, stateJson[MeditationSaveKey] as JObject);
            state.LastManaHudSignature = null;
            state.LastManaRegenAtMs = Pipliz.Time.MillisecondsSinceStart;
            state.WelcomeMessageReadyAtMs = 0L;
            EnsureManaDefaults(state);
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnSavingPlayer, BetterNecromancy.ModEntry.Namespace + ".PlayerMagicStateManager.OnSavingPlayer")]
        public static void OnSavingPlayer(JObject miscJson, Players.Player player)
        {
            var state = GetState(player);
            EnsureManaDefaults(state);

            miscJson[SaveKey] = new JObject
            {
                ["firstBossRewardGranted"] = state.FirstBossRewardGranted,
                ["bootsOfFallingEquipped"] = state.BootsOfFallingEquipped,
                ["healthBoosterEquipped"] = state.HealthBoosterEquipped,
                ["manaFlightEnabled"] = state.ManaFlightEnabled,
                ["manaFlightUnlocked"] = state.ManaFlightUnlocked,
                ["flightMasterySeconds"] = state.FlightMasterySeconds,
                ["manaCrystalLevel"] = state.ManaCrystalLevel,
                ["arcaneFocusTier"] = state.ArcaneFocusTier,
                ["skilledSwordMask"] = state.SkilledSwordMask,
                ["skilledSwordTier"] = GetHighestSkilledSwordTier(state),
                ["skilledHelmTier"] = state.SkilledHelmTier,
                ["skilledChestTier"] = state.SkilledChestTier,
                ["skilledGlovesTier"] = state.SkilledGlovesTier,
                ["skilledLegsTier"] = state.SkilledLegsTier,
                ["skilledBootsTier"] = state.SkilledBootsTier,
                ["skilledShieldTier"] = state.SkilledShieldTier,
                [WandMasterySaveKey] = SaveWandMastery(state),
                [LegendaryWandSaveKey] = SaveLegendaryWands(state),
                [LegendaryWandMasterySaveKey] = SaveLegendaryWandMastery(state),
                [WandLineageSaveKey] = SaveWandLineage(state),
                [MeditationSaveKey] = SaveMeditation(state),
                ["maxMana"] = state.MaxMana,
                ["currentMana"] = state.CurrentMana
            };
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnPlayerConnectedLate, BetterNecromancy.ModEntry.Namespace + ".PlayerMagicStateManager.OnPlayerConnectedLate")]
        public static void OnPlayerConnectedLate(Players.Player player)
        {
            if (player == null)
                return;

            var state = GetState(player);
            if (player.HasFlightMode)
                player.SetFlightMode(false);
            state.ManaFlightPermissionGranted = false;
            state.ManaFlightActive = false;
            state.ManaFlightUnsupportedSinceAtMs = 0L;
            state.NextManaFlightDrainAtMs = 0L;
            state.PendingManaFlightCrashUntilMs = 0L;
            state.ManaFlightCostCarry = 0f;
            state.ManaFlightAirborneSinceAtMs = 0L;
            state.ManaFlightGroundedSinceAtMs = 0L;
            state.LastManaFlightActiveAtMs = 0L;
            state.ManaFlightTakeoffY = 0f;
            state.LastManaFlightHorizontalSpeed = 0f;
            state.ManaFlightSampleInitialized = false;
            state.LastManaFlightSamplePosition = Vector3.zero;
            state.ManaFlightTakeoffPosition = Vector3.zero;
            state.LastManaFlightSampleAtMs = 0L;
            state.WelcomeMessageSentThisSession = false;
            state.WelcomeMessageReadyAtMs = Pipliz.Time.MillisecondsSinceStart + WelcomeMessageDelayMs;
            state.WelcomeOverlayVisibleUntilMs = 0L;
            state.NextWandEvolutionChoicePopupAtMs = 0L;
            state.WandLineageChoiceOpenedAtMs = 0L;
            RepairLineageFromLegacyAndInventory(player, state, true);
            RepairUnlockedWandItems(player, state);
            if (PlayerUiGuard.CanSendStable(player))
            {
                UIManager.RemoveUILabel(WelcomeHudKey, player);
                UIManager.RemoveUILabel(WandMasteryHudKey, player);
                UIManager.RemoveUILabel(ManaFlightHudKey, player);
                UIManager.RemoveUILabel(MeditationHudKey, player);
            }
            state.LastWandMasteryHudSignature = null;
            state.LastMeditationHudSignature = null;
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnPlayerRespawn, BetterNecromancy.ModEntry.Namespace + ".PlayerMagicStateManager.OnPlayerRespawn")]
        public static void OnPlayerRespawn(Players.Player player)
        {
            if (!States.TryGetValue(player, out var state))
                return;

            if (state.ManaFlightActive || state.ManaFlightPermissionGranted)
                DisableManaFlight(player, state, null);

            EnsureManaDefaults(state);
            state.CurrentMana = state.MaxMana;
            state.LastManaRegenAtMs = Pipliz.Time.MillisecondsSinceStart;
            state.LastManaHudSignature = null;
            state.LastWandMasteryHudSignature = null;
            state.PendingManaFlightCrashUntilMs = 0L;
            state.ManaFlightCostCarry = 0f;
            state.ManaFlightAirborneSinceAtMs = 0L;
            state.ManaFlightGroundedSinceAtMs = 0L;
            state.LastManaFlightActiveAtMs = 0L;
            state.ManaFlightTakeoffY = 0f;
            state.LastManaFlightHorizontalSpeed = 0f;
            state.ManaFlightSampleInitialized = false;
            state.LastManaFlightSamplePosition = Vector3.zero;
            state.ManaFlightTakeoffPosition = Vector3.zero;
            state.LastManaFlightSampleAtMs = 0L;
            EndMeditation(player, state);

            UpdateManaHud(player, state, true);
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnUpdate, BetterNecromancy.ModEntry.Namespace + ".PlayerMagicStateManager.OnUpdate")]
        public static void OnUpdate()
        {
            var now = Pipliz.Time.MillisecondsSinceStart;

            if (now > _nextManaFlightSampleUpdate)
            {
                foreach (var player in Players.ConnectedPlayers)
                {
                    var state = GetState(player);
                    EnsureManaDefaults(state);
                    UpdateManaFlightPermission(player, state);
                    RefreshManaFlightActivity(player, state, now);
                    UpdateManaFlightHud(player, state);
                    UpdateMeditation(player, state, now);
                }

                _nextManaFlightSampleUpdate = now + ManaFlightSampleIntervalMs;
            }

            if (now <= _nextUpdate)
                return;

            foreach (var player in Players.ConnectedPlayers)
            {
                var state = GetState(player);
                EnsureManaDefaults(state);
                TrySendFirstWorldWelcome(player, state);
                UpdateWelcomeOverlay(player, state, now);
                UpdateManaFlightPermission(player, state);
                RefreshManaFlightActivity(player, state, now);
                UpdateManaFlight(player, state, now);
                UpdateMeditation(player, state, now);
                RegenerateMana(state, now);

                if (state.HealthBoosterEquipped && player.Health > 0f && player.Health < player.HealthMax)
                {
                    var regenPerTick = 2f * GetHealingReceivedMultiplier(player);
                    player.Health = UnityEngine.Mathf.Min(player.HealthMax, player.Health + regenPerTick);
                    player.SendHealthPacket();
                }

                UpdateManaHud(player, state);
                UpdateWandMasteryHud(player, state);
                UpdateManaFlightHud(player, state);
                UpdateMeditationHud(player, state);
                UpdateWandEvolutionChoicePopup(player, state, now);
            }

            _nextUpdate = now + 1000;
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnPlayerPushedNetworkUIButton, BetterNecromancy.ModEntry.Namespace + ".PlayerMagicStateManager.OnPlayerPushedNetworkUIButton")]
        public static void OnPlayerPushedNetworkUIButton(NetworkUI.ButtonPressCallbackData data)
        {
            if (data.Player == null || string.IsNullOrWhiteSpace(data.ButtonIdentifier))
                return;

            if (!data.ButtonIdentifier.StartsWith(WandEvolutionChoiceButtonPrefix, StringComparison.Ordinal))
                return;

            var branchKey = data.ButtonIdentifier.Substring(WandEvolutionChoiceButtonPrefix.Length);
            var state = GetState(data.Player);
            var now = Pipliz.Time.MillisecondsSinceStart;
            if (state.WandLineageChoiceOpenedAtMs > 0L && now < state.WandLineageChoiceOpenedAtMs + WandEvolutionChoiceInputLockMs)
            {
                SendEquipChat(data.Player, "Wand evolution choice is locked for a moment so accidental clicks cannot select a form.");
                OpenWandEvolutionChoicePopup(data.Player, state, true);
                return;
            }

            if (TryChooseWandBranch(data.Player, branchKey, out var message))
            {
                if (!string.IsNullOrWhiteSpace(message))
                    SendEquipChat(data.Player, message);
                return;
            }

            if (!string.IsNullOrWhiteSpace(message))
                SendEquipChat(data.Player, message);

            if (state.WandLineageChoicePending)
                OpenWandEvolutionChoicePopup(data.Player, state, true);
        }

        private static PlayerMagicState GetState(Players.Player player)
        {
            if (!States.TryGetValue(player, out var state))
            {
                state = new PlayerMagicState
                {
                    LastManaRegenAtMs = Pipliz.Time.MillisecondsSinceStart
                };
                States.Add(player, state);
            }

            EnsureManaDefaults(state);
            EnsureWandMasteryDefaults(state);
            EnsureLegendaryWandDefaults(state);
            return state;
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnPlayerHit, BetterNecromancy.ModEntry.Namespace + ".PlayerMagicStateManager.OnPlayerHit")]
        public static void OnPlayerHit(Players.Player player, ModLoader.OnHitData hitData)
        {
            var isFallLikeDamage =
                hitData.HitSourceType == ModLoader.OnHitData.EHitSourceType.FallDamage ||
                (hitData.HitSourceType == ModLoader.OnHitData.EHitSourceType.None && hitData.HitSourceObject == null);

            if (hitData.ResultDamage > 0f && isFallLikeDamage)
            {
                var fallReduction = GetBootsFallReduction(player);

                if (fallReduction > 0f)
                    hitData.ResultDamage *= 1f - fallReduction;
            }

            var shieldReduction = GetShieldDamageReduction(player);

            if (shieldReduction > 0f)
                hitData.ResultDamage *= 1f - shieldReduction;

            if (hitData.ResultDamage <= 0f)
                return;

            if (player != null &&
                States.TryGetValue(player, out var state) &&
                isFallLikeDamage &&
                hitData.ResultDamage > 0f)
            {
                var now = Pipliz.Time.MillisecondsSinceStart;
                var crashWindowActive = state.PendingManaFlightCrashUntilMs > now;
                var recentlyInManaFlight =
                    state.LastManaFlightActiveAtMs > 0L &&
                    now - state.LastManaFlightActiveAtMs <= ManaFlightCrashWindowMs;

                if (crashWindowActive || recentlyInManaFlight)
                {
                    state.PendingManaFlightCrashUntilMs = 0L;
                    state.LastManaFlightActiveAtMs = 0L;
                    ItemUseAudioManager.Play(player, ItemUseAudioManager.ManaFlightCrash);
                }
            }
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnPlayerMoved, BetterNecromancy.ModEntry.Namespace + ".PlayerMagicStateManager.OnPlayerMoved")]
        public static void OnPlayerMoved(Players.Player player, Vector3 oldLocation)
        {
            // Flight activity is sampled in the 1-second update loop to avoid
            // per-move voxel checks and keep the built-in F-flight lightweight.
        }

        private static void UpdateMeditation(Players.Player player, PlayerMagicState state, long now)
        {
            if (player == null || state == null)
                return;

            if (state.LastMeditationSampleAtMs > 0L && now - state.LastMeditationSampleAtMs < MeditationSampleIntervalMs)
                return;

            var settings = WandProgressionConfig.Current.Meditation;
            var previousSampleAt = state.LastMeditationSampleAtMs;
            var position = player.PositionStanding;
            var initialized = previousSampleAt > 0L;
            var delta = initialized ? position - state.LastMeditationPosition : Vector3.zero;
            var stationaryTolerance = settings.StationaryDistanceTolerance * settings.StationaryDistanceTolerance;
            var stationary = !initialized || delta.sqrMagnitude <= stationaryTolerance;
            var wantsMeditation = IsPlayerCrouching(player) && stationary && !state.ManaFlightActive;

            state.LastMeditationPosition = position;
            state.LastMeditationSampleAtMs = now;

            if (!wantsMeditation)
            {
                EndMeditation(player, state);
                return;
            }

            if (state.MeditationStationarySinceAtMs <= 0L)
                state.MeditationStationarySinceAtMs = now;

            if (!state.MeditationActive)
            {
                var delayMs = (long)(settings.StartDelaySeconds * 1000d);
                if (now - state.MeditationStationarySinceAtMs < delayMs)
                    return;

                state.MeditationActive = true;
                state.LastMeditationHudSignature = null;
            }

            if (state.CurrentMana < state.MaxMana)
                return;

            state.MeditationFullManaCarryMs += System.Math.Max(0L, previousSampleAt > 0L ? now - previousSampleAt : 0L);
            while (state.MeditationFullManaCarryMs >= 1000L)
            {
                state.MeditationFullManaCarryMs -= 1000L;
                state.FullManaMeditationSeconds++;
                TryGrantMeditationRewards(player, state);
            }
        }

        private static void EndMeditation(Players.Player player, PlayerMagicState state)
        {
            if (state == null)
                return;

            var wasActive = state.MeditationActive;
            state.MeditationActive = false;
            state.MeditationStationarySinceAtMs = 0L;
            state.MeditationFullManaCarryMs = 0L;
            if (wasActive && player != null && PlayerUiGuard.CanSendStable(player))
            {
                UIManager.RemoveUILabel(MeditationHudKey, player);
                state.LastMeditationHudSignature = null;
            }
        }

        private static void TryGrantMeditationRewards(Players.Player player, PlayerMagicState state)
        {
            if (player == null || state == null)
                return;

            var settings = WandProgressionConfig.Current.Meditation;
            var earnedMaxManaRewards = (int)(state.FullManaMeditationSeconds / settings.FullManaRewardSeconds);
            if (earnedMaxManaRewards > state.MeditationMaxManaRewardsGranted)
            {
                var rewardsToGrant = earnedMaxManaRewards - state.MeditationMaxManaRewardsGranted;
                state.MeditationMaxManaRewardsGranted = earnedMaxManaRewards;
                state.MaxMana += rewardsToGrant * settings.MaxManaRewardAmount;
                state.CurrentMana = state.MaxMana;
                state.LastManaHudSignature = null;
                PlayerToastManager.Show(player, "Meditation breakthrough: +" + (rewardsToGrant * settings.MaxManaRewardAmount) + " max mana. Max mana is now " + state.MaxMana + ".", "#bfe7ff", 5200L);
                SendEquipChat(player, "Meditation breakthrough absorbed. Max mana is now " + state.MaxMana + ".");
            }

            if (!state.MeditationRegenRewardGranted && state.FullManaMeditationSeconds >= settings.RegenMilestoneSeconds)
            {
                state.MeditationRegenRewardGranted = true;
                state.LastManaHudSignature = null;
                PlayerToastManager.Show(player, "Meditation mastery: permanent mana regeneration improved.", "#bfe7ff", 6200L);
            }
        }

        private static void UpdateMeditationHud(Players.Player player, PlayerMagicState state)
        {
            if (player == null || state == null || !PlayerUiGuard.CanSendStable(player))
                return;

            if (!state.MeditationActive)
            {
                if (!string.IsNullOrEmpty(state.LastMeditationHudSignature))
                {
                    UIManager.RemoveUILabel(MeditationHudKey, player);
                    state.LastMeditationHudSignature = null;
                }

                return;
            }

            var settings = WandProgressionConfig.Current.Meditation;
            var progress = state.FullManaMeditationSeconds % settings.FullManaRewardSeconds;
            var text = state.CurrentMana >= state.MaxMana
                ? "Meditation: full mana " + progress + "/" + settings.FullManaRewardSeconds + "s | next +max mana"
                : "Meditation: mana regen boosted";

            if (string.Equals(state.LastMeditationHudSignature, text, StringComparison.Ordinal))
                return;

            var hudSettings = BetterNecromancy.ManaHudSettings.Current;
            var hudAnchor = ResolveManaHudAnchor(hudSettings.Anchor);
            var meditationOffsetY = IsBottomAnchored(hudAnchor)
                ? hudSettings.OffsetY + MeditationHudManaBarOffsetY
                : hudSettings.OffsetY - MeditationHudManaBarOffsetY;
            var hudPosition = new Pipliz.Vector3Int(hudSettings.OffsetX, meditationOffsetY, 0);

            UIManager.AddorUpdateUILabel(
                MeditationHudKey,
                UIElementDisplayType.Global,
                text,
                hudPosition,
                hudAnchor,
                620f,
                player,
                14f,
                FontType.DidactGothic,
                "#bfe7ff",
                TextAlignmentOptions.Center);
            state.LastMeditationHudSignature = text;
        }

        private static bool IsPlayerCrouching(Players.Player player)
        {
            if (player == null)
                return false;

            var type = player.GetType();
            var memberNames = new[] { "IsCrouching", "Crouching", "IsSneaking", "Sneaking", "WantsCrouch", "Crouch" };
            for (var i = 0; i < memberNames.Length; i++)
            {
                var property = type.GetProperty(memberNames[i], BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (property != null && property.PropertyType == typeof(bool))
                {
                    try
                    {
                        if ((bool)property.GetValue(player, null))
                            return true;
                    }
                    catch
                    {
                    }
                }

                var field = type.GetField(memberNames[i], BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null && field.FieldType == typeof(bool))
                {
                    try
                    {
                        if ((bool)field.GetValue(player))
                            return true;
                    }
                    catch
                    {
                    }
                }
            }

            try
            {
                return player.PositionCamera.y - player.PositionStanding.y < 1.35f;
            }
            catch
            {
                return false;
            }
        }

        private static void RegenerateMana(PlayerMagicState state, long now)
        {
            if (state.CurrentMana >= state.MaxMana)
            {
                state.CurrentMana = state.MaxMana;
                state.LastManaRegenAtMs = now;
                return;
            }

            if (state.LastManaRegenAtMs <= 0L)
                state.LastManaRegenAtMs = now;

            var elapsed = now - state.LastManaRegenAtMs;
            var regenIntervalMs = GetManaRegenIntervalMs(state);
            if (state.MeditationActive)
            {
                var meditation = WandProgressionConfig.Current.Meditation;
                regenIntervalMs = System.Math.Max(
                    MinimumManaRegenIntervalMs,
                    regenIntervalMs - System.Math.Max(0, meditation.RegenIntervalReductionMs));
            }

            if (state.MeditationRegenRewardGranted)
            {
                regenIntervalMs = System.Math.Max(
                    MinimumManaRegenIntervalMs,
                    regenIntervalMs - System.Math.Max(0, WandProgressionConfig.Current.Meditation.PermanentRegenReductionMs));
            }

            if (elapsed < regenIntervalMs)
                return;

            var manaToRestore = (int)(elapsed / regenIntervalMs);
            if (manaToRestore <= 0)
                return;

            state.CurrentMana = System.Math.Min(state.MaxMana, state.CurrentMana + manaToRestore);
            if (state.CurrentMana >= state.MaxMana)
            {
                state.CurrentMana = state.MaxMana;
                state.LastManaRegenAtMs = now;
            }
            else
            {
                state.LastManaRegenAtMs += manaToRestore * regenIntervalMs;
            }
        }

        private static void UpdateManaFlight(Players.Player player, PlayerMagicState state, long now)
        {
            if (state.PendingManaFlightCrashUntilMs > 0L && now >= state.PendingManaFlightCrashUntilMs)
                state.PendingManaFlightCrashUntilMs = 0L;

            if (!state.ManaFlightEnabled)
            {
                if (state.ManaFlightActive || state.ManaFlightPermissionGranted)
                    DisableManaFlight(player, state, null);
                return;
            }

            if (!state.ManaFlightPermissionGranted)
            {
                state.ManaFlightActive = false;
                state.NextManaFlightDrainAtMs = 0L;
                state.ManaFlightCostCarry = 0f;
                return;
            }

            if (!state.ManaFlightActive)
            {
                state.ManaFlightActive = false;
                state.NextManaFlightDrainAtMs = 0L;
                state.ManaFlightCostCarry = 0f;
                return;
            }

            if (!state.ManaFlightActive)
            {
                state.NextManaFlightDrainAtMs = 0L;
                state.ManaFlightCostCarry = 0f;
                return;
            }

            if (state.NextManaFlightDrainAtMs <= 0L)
                state.NextManaFlightDrainAtMs = now + ManaFlightDrainIntervalMs;

            if (now < state.NextManaFlightDrainAtMs)
                return;

            state.NextManaFlightDrainAtMs = now + ManaFlightDrainIntervalMs;
            RegisterFlightMasterySecond(player, state);

            var masteryLevel = GetFlightMasteryLevel(state);
            if (masteryLevel <= 0 && Pipliz.Random.NextFloat() <= ManaFlightUnstableShutdownChance)
            {
                BeginManaFlightCrash(player, state, "Mana Flight destabilized and shut down.");
                return;
            }

            if (masteryLevel <= 1 && state.LastManaFlightHorizontalSpeed >= ManaFlightBoostSpeedThreshold)
            {
                BeginManaFlightCrash(player, state, "Mana Flight collapsed under the boost.");
                return;
            }

            var manaCost = GetFlightManaCostPerSecond(state);
            if (masteryLevel <= 1 && Pipliz.Random.NextFloat() <= ManaFlightUnstableExtraManaChance)
                manaCost *= 2f;
            if (masteryLevel >= 2 && state.LastManaFlightHorizontalSpeed >= ManaFlightBoostSpeedThreshold)
                manaCost += ManaFlightBoostExtraManaPerSecond;

            state.ManaFlightCostCarry += manaCost;
            var manaToSpend = (int)System.Math.Floor(state.ManaFlightCostCarry + 0.0001f);
            if (manaToSpend <= 0)
                return;

            if (TrySpendMana(player, manaToSpend, "Not enough mana to maintain flight."))
            {
                state.ManaFlightCostCarry = System.Math.Max(0f, state.ManaFlightCostCarry - manaToSpend);
                return;
            }

            BeginManaFlightCrash(player, state, "Mana Flight ended because you ran out of mana.");
        }

        private static void UpdateManaFlightPermission(Players.Player player, PlayerMagicState state)
        {
            if (!state.ManaFlightEnabled || !state.ManaFlightUnlocked)
            {
                if (state.ManaFlightPermissionGranted)
                    DisableManaFlight(player, state, null);
                return;
            }

            if (state.CurrentMana >= 1)
            {
                if (!state.ManaFlightPermissionGranted)
                {
                    if (!player.HasFlightMode)
                        player.SetFlightMode(true);
                    state.ManaFlightPermissionGranted = true;
                }

                return;
            }

            if (state.ManaFlightPermissionGranted)
                DisableManaFlight(player, state, null);
        }

        private static void RefreshManaFlightActivity(Players.Player player, PlayerMagicState state, long now)
        {
            if (!state.ManaFlightEnabled || !state.ManaFlightPermissionGranted)
            {
                state.ManaFlightActive = false;
                state.ManaFlightUnsupportedSinceAtMs = 0L;
                state.ManaFlightAirborneSinceAtMs = 0L;
                state.ManaFlightGroundedSinceAtMs = 0L;
                state.ManaFlightTakeoffY = 0f;
                state.LastManaFlightHorizontalSpeed = 0f;
                state.ManaFlightSampleInitialized = false;
                state.LastManaFlightSamplePosition = Vector3.zero;
                state.ManaFlightTakeoffPosition = Vector3.zero;
                state.LastManaFlightSampleAtMs = 0L;
                return;
            }

            var currentPosition = player.PositionStanding;
            var hasImmediateSupportBelow = HasSolidSupportBelow(player, 1);
            var hasNearSupportBelow = hasImmediateSupportBelow || HasSolidSupportBelow(player, 2);
            var hasExtendedSupportBelow = hasNearSupportBelow || HasSolidSupportBelow(player, 3);

            if (!state.ManaFlightSampleInitialized)
            {
                state.LastManaFlightSamplePosition = currentPosition;
                state.ManaFlightTakeoffPosition = currentPosition;
                state.ManaFlightTakeoffY = currentPosition.y;
                state.ManaFlightSampleInitialized = true;
                state.LastManaFlightSampleAtMs = now;
            }

            var sampleSeconds = Mathf.Max(0.05f, (now - state.LastManaFlightSampleAtMs) / 1000f);
            var verticalDelta = currentPosition.y - state.LastManaFlightSamplePosition.y;
            var horizontalDelta = new Vector2(
                currentPosition.x - state.LastManaFlightSamplePosition.x,
                currentPosition.z - state.LastManaFlightSamplePosition.z).magnitude;
            var horizontalSpeed = horizontalDelta / sampleSeconds;
            state.LastManaFlightHorizontalSpeed = horizontalSpeed;

            if (hasImmediateSupportBelow)
            {
                state.ManaFlightAirborneSinceAtMs = 0L;
                state.ManaFlightTakeoffPosition = currentPosition;
                state.ManaFlightTakeoffY = currentPosition.y;

                if (!state.ManaFlightActive)
                {
                    state.ManaFlightGroundedSinceAtMs = now;
                }
                else
                {
                    var looksLanded =
                        horizontalSpeed <= ManaFlightSprintTriggerSpeed * 1.15f &&
                        Mathf.Abs(verticalDelta) <= 0.08f;

                    if (!looksLanded)
                    {
                        state.ManaFlightGroundedSinceAtMs = 0L;
                    }
                    else if (state.ManaFlightGroundedSinceAtMs <= 0L)
                    {
                        state.ManaFlightGroundedSinceAtMs = now;
                    }
                    else if (now - state.ManaFlightGroundedSinceAtMs >= ManaFlightDeactivationDelayMs)
                    {
                        state.ManaFlightActive = false;
                        state.NextManaFlightDrainAtMs = 0L;
                        state.ManaFlightCostCarry = 0f;
                        state.ManaFlightGroundedSinceAtMs = 0L;
                        state.ManaFlightAirborneSinceAtMs = 0L;
                        state.ManaFlightTakeoffPosition = currentPosition;
                        state.ManaFlightTakeoffY = currentPosition.y;
                    }
                }

                state.LastManaFlightSamplePosition = currentPosition;
                state.LastManaFlightSampleAtMs = now;
                return;
            }

            state.ManaFlightGroundedSinceAtMs = 0L;
            if (state.ManaFlightAirborneSinceAtMs <= 0L)
            {
                state.ManaFlightAirborneSinceAtMs = now;
                state.ManaFlightTakeoffPosition = currentPosition;
                state.ManaFlightTakeoffY = currentPosition.y;
            }

            if (!state.ManaFlightActive && hasNearSupportBelow && verticalDelta <= ManaFlightActivationFallThreshold)
            {
                state.ManaFlightAirborneSinceAtMs = now;
                state.ManaFlightTakeoffPosition = currentPosition;
                state.ManaFlightTakeoffY = currentPosition.y;
                state.LastManaFlightSamplePosition = currentPosition;
                state.LastManaFlightSampleAtMs = now;
                return;
            }

            if (!state.ManaFlightActive)
            {
                var airborneDurationMs = now - state.ManaFlightAirborneSinceAtMs;
                var riseFromTakeoff = currentPosition.y - state.ManaFlightTakeoffY;
                var horizontalTravelFromTakeoff = new Vector2(
                    currentPosition.x - state.ManaFlightTakeoffPosition.x,
                    currentPosition.z - state.ManaFlightTakeoffPosition.z).magnitude;
                var risesLikeFlight =
                    !hasExtendedSupportBelow &&
                    airborneDurationMs >= ManaFlightActivationDelayMs + 100L &&
                    riseFromTakeoff >= ManaFlightActivationMinRiseFromTakeoff &&
                    verticalDelta >= ManaFlightActivationSoftFallThreshold;
                var glidesLikeFlight =
                    !hasExtendedSupportBelow &&
                    airborneDurationMs >= ManaFlightActivationDelayMs &&
                    horizontalTravelFromTakeoff >= 1.25f &&
                    horizontalSpeed >= ManaFlightActivationHorizontalGlideThreshold &&
                    verticalDelta >= ManaFlightActivationSoftFallThreshold;
                var boostsLikeFlight =
                    !hasExtendedSupportBelow &&
                    airborneDurationMs >= ManaFlightActivationDelayMs + 300L &&
                    riseFromTakeoff >= 0.35f &&
                    horizontalTravelFromTakeoff >= 2.4f &&
                    horizontalSpeed >= ManaFlightSprintTriggerSpeed &&
                    verticalDelta >= ManaFlightActivationSoftFallThreshold;

                if (risesLikeFlight || glidesLikeFlight || boostsLikeFlight)
                {
                    var masteryLevel = GetFlightMasteryLevel(state);
                    if (masteryLevel <= 1 &&
                        !TrySpendMana(player, ManaFlightUnstableTakeoffCost, "Not enough mana to stabilize takeoff."))
                    {
                        DisableManaFlight(player, state, "Mana Flight failed to ignite. You need 10 mana for unstable takeoff.");
                        state.LastManaFlightSamplePosition = currentPosition;
                        state.LastManaFlightSampleAtMs = now;
                        return;
                    }

                    // Once takeoff is confirmed, keep flight sticky until the player is
                    // clearly grounded again instead of re-evaluating every tiny move.
                    state.ManaFlightActive = true;
                    state.ManaFlightUnsupportedSinceAtMs = 0L;
                    state.ManaFlightGroundedSinceAtMs = 0L;
                    state.NextManaFlightDrainAtMs = 0L;
                }
            }

            if (state.ManaFlightActive)
                state.LastManaFlightActiveAtMs = now;

            state.LastManaFlightSamplePosition = currentPosition;
            state.LastManaFlightSampleAtMs = now;
        }

        private static void TrySendFirstWorldWelcome(Players.Player player, PlayerMagicState state)
        {
            if (!PlayerUiGuard.CanSendStable(player) || state == null || state.WelcomeMessageSentThisSession)
                return;

            var now = Pipliz.Time.MillisecondsSinceStart;
            if (state.WelcomeMessageReadyAtMs <= 0L)
            {
                state.WelcomeMessageReadyAtMs = now + WelcomeMessageDelayMs;
                return;
            }

            if (now < state.WelcomeMessageReadyAtMs)
                return;

            state.WelcomeMessageSentThisSession = true;
            state.WelcomeMessageReadyAtMs = 0L;
            state.WelcomeOverlayVisibleUntilMs = 0L;
            PlayerToastManager.Show(player, FirstWorldWelcomeMessage, "#f1e7c8", 5200L, -110, 1100f, 16f);
        }

        private static void UpdateWelcomeOverlay(Players.Player player, PlayerMagicState state, long now)
        {
            if (player == null || state == null)
                return;

            if (!PlayerUiGuard.CanSendStable(player))
            {
                UIManager.RemoveUILabel(WelcomeHudKey, player);
                return;
            }

            if (state.WelcomeOverlayVisibleUntilMs <= 0L)
            {
                UIManager.RemoveUILabel(WelcomeHudKey, player);
                return;
            }

            if (now >= state.WelcomeOverlayVisibleUntilMs)
            {
                state.WelcomeOverlayVisibleUntilMs = 0L;
                UIManager.RemoveUILabel(WelcomeHudKey, player);
                return;
            }

            UIManager.AddorUpdateUILabel(
                WelcomeHudKey,
                UIElementDisplayType.Global,
                FirstWorldWelcomeMessage,
                new Pipliz.Vector3Int(0, -110, 0),
                AnchorPresets.TopCenter,
                900f,
                player,
                16f,
                FontType.DidactGothic,
                "#f1e7c8",
                TextAlignmentOptions.Center);
        }

        private static void UpdateManaFlightHud(Players.Player player, PlayerMagicState state)
        {
            if (!PlayerUiGuard.CanSendStable(player) || !state.ManaFlightActive)
            {
                UIManager.RemoveUILabel(ManaFlightHudKey, player);
                return;
            }

            UIManager.AddorUpdateUILabel(
                ManaFlightHudKey,
                UIElementDisplayType.Global,
                "Mana Flight active / " + GetFlightManaCostDisplay(state) + "\n" + BuildFlightMasteryLevelLabel(GetFlightMasteryLevel(state)) + " / " + GetFlightMasteryProgressLabel(state),
                new Pipliz.Vector3Int(0, -82, 0),
                AnchorPresets.TopCenter,
                520f,
                player,
                14f,
                FontType.DidactGothic,
                "#9dd7ff",
                TextAlignmentOptions.Center);
        }

        private static void UpdateManaHud(Players.Player player, PlayerMagicState state, bool force = false)
        {
            if (!PlayerUiGuard.CanSendStable(player))
            {
                state.LastManaHudSignature = null;
                return;
            }

            var safeMaxMana = System.Math.Max(DefaultMaxMana, state.MaxMana);
            var filledSegments = (int)System.Math.Round((double)state.CurrentMana / safeMaxMana * ManaHudSegments, MidpointRounding.AwayFromZero);
            filledSegments = System.Math.Max(0, System.Math.Min(ManaHudSegments, filledSegments));
            var signature = "pretty|" + filledSegments + "|" + state.MaxMana;

            if (!force && string.Equals(state.LastManaHudSignature, signature, StringComparison.Ordinal))
                return;

            var hudSettings = BetterNecromancy.ManaHudSettings.Current;
            var hudPosition = new Pipliz.Vector3Int(hudSettings.OffsetX, hudSettings.OffsetY, 0);
            var hudAnchor = ResolveManaHudAnchor(hudSettings.Anchor);

            if (force || state.LastManaHudSignature == null)
            {
                UIManager.RemoveUILabel(ManaHudLegacyKey, player);
                UIManager.RemoveUILabel(ManaHudLabelKey, player);
                UIManager.RemoveUILabel(ManaHudBackgroundKey, player);
                UIManager.RemoveUILabel(ManaHudFillKey, player);
                UIManager.RemoveUIImage(ManaHudImageKey, player);

                for (var layer = 0; layer < 4; layer++)
                {
                    UIManager.RemoveUILabel(BuildManaHudLayerKey(ManaHudBackgroundKey, layer), player);
                    UIManager.RemoveUILabel(BuildManaHudLayerKey(ManaHudFillKey, layer), player);
                }
            }

            UIManager.AddorUpdateUIImage(
                ManaHudImageKey,
                UIElementDisplayType.Global,
                BuildManaHudImageType(filledSegments),
                hudPosition,
                hudAnchor,
                player);

            state.LastManaHudSignature = signature;
        }

        private static void UpdateWandMasteryHud(Players.Player player, PlayerMagicState state)
        {
            if (!PlayerUiGuard.CanSendStable(player) || state == null)
            {
                if (state != null)
                    state.LastWandMasteryHudSignature = null;
                UIManager.RemoveUILabel(WandMasteryHudKey, player);
                return;
            }

            if (!TryGetSelectedWandMastery(player, out var masteryKey, out var displayName))
            {
                state.LastWandMasteryHudSignature = null;
                UIManager.RemoveUILabel(WandMasteryHudKey, player);
                return;
            }

            var evolved = IsWandLegendaryEvolutionUnlocked(player, masteryKey);
            var level = evolved
                ? GetLegendaryWandMasteryLevel(player, masteryKey)
                : GetWandMasteryLevel(player, masteryKey);
            var progressLabel = evolved
                ? GetLegendaryWandMasteryProgressLabel(player, masteryKey)
                : GetWandMasteryProgressLabel(player, masteryKey);
            var topLabel = evolved
                ? GetWandLegendaryDisplayName(player, masteryKey)
                : displayName;
            var bottomLabel = evolved
                ? GetWandLegendaryCompactSummary(player, masteryKey)
                : GetWandMasteryCompactSummary(masteryKey, level);
            var levelLabel = evolved ? "Evolution " + GetWandMasteryLevelLabel(level) : GetWandMasteryLevelLabel(level);
            var hudText = topLabel + "  " + levelLabel + "  " + progressLabel + "\n" + bottomLabel;
            if (state.WandLineageUnlocked && IsMasteryKeyPartOfLineage(state, masteryKey))
            {
                var tierLabel = "T" + System.Math.Max(1, state.WandLineageTier) + " " + (string.IsNullOrWhiteSpace(state.WandLineageBranch) ? "Starter" : state.WandLineageBranch);
                hudText = tierLabel + " Wand | Mastery " + GetWandMasteryLevelLabel(level) + " " + progressLabel +
                    "\n" + GetLineageEvolutionHudText(state) +
                    "\nEffects: " + GetLineageEffectsHudText(state);
            }

            if (string.Equals(state.LastWandMasteryHudSignature, hudText, StringComparison.Ordinal))
                return;

            UIManager.AddorUpdateUILabel(
                WandMasteryHudKey,
                UIElementDisplayType.Global,
                hudText,
                new Pipliz.Vector3Int(0, WandMasteryHudBottomOffsetY, 0),
                AnchorPresets.BottonCenter,
                900f,
                player,
                14f,
                FontType.DidactGothic,
                "#f6fbff",
                TextAlignmentOptions.Center);

            state.LastWandMasteryHudSignature = hudText;
        }

        private static void CacheObservedWand(PlayerMagicState state, string masteryKey, string displayName)
        {
            if (state == null || string.IsNullOrWhiteSpace(masteryKey) || string.IsNullOrWhiteSpace(displayName))
                return;

            state.LastObservedWandMasteryKey = masteryKey;
            state.LastObservedWandDisplayName = displayName;
            state.LastObservedWandUntilMs = Pipliz.Time.MillisecondsSinceStart + WandMasteryHudFallbackDurationMs;
        }

        private static void ShowIndicator(Players.Player player, IndicatorState indicatorState, long cooldownMs)
        {
            if (!PlayerUiGuard.CanSendStable(player))
                return;

            var state = GetState(player);
            var now = Pipliz.Time.MillisecondsSinceStart;

            if (cooldownMs > 0L && now < state.NextVisualFeedbackAtMs)
                return;

            if (cooldownMs > 0L)
                state.NextVisualFeedbackAtMs = now + cooldownMs;
            else
                state.NextVisualFeedbackAtMs = now + VisualFeedbackCooldownMs;

            Indicator.SendIconIndicatorToPlayer(player.PositionStanding + Vector3.up * 1.6f, indicatorState, player);
        }

        private static string BuildManaHudImageType(int filledSegments)
        {
            return ManaHudImageTypePrefix + filledSegments;
        }

        private static AnchorPresets ResolveManaHudAnchor(string anchor)
        {
            switch ((anchor ?? string.Empty).Trim())
            {
                case "TopLeft":
                    return AnchorPresets.TopLeft;
                case "TopCenter":
                    return AnchorPresets.TopCenter;
                case "TopRight":
                    return AnchorPresets.TopRight;
                case "MiddleLeft":
                    return AnchorPresets.MiddleLeft;
                case "MiddleCenter":
                    return AnchorPresets.MiddleCenter;
                case "MiddleRight":
                    return AnchorPresets.MiddleRight;
                case "BottomLeft":
                    return AnchorPresets.BottomLeft;
                case "BottomRight":
                    return AnchorPresets.BottomRight;
                case "BottomCenter":
                default:
                    return AnchorPresets.BottonCenter;
            }
        }

        private static bool IsBottomAnchored(AnchorPresets anchor)
        {
            return anchor == AnchorPresets.BottonCenter ||
                anchor == AnchorPresets.BottomLeft ||
                anchor == AnchorPresets.BottomRight;
        }

        private static string NormalizeWandMasteryKey(string masteryKey)
        {
            if (string.IsNullOrWhiteSpace(masteryKey))
                return string.Empty;

            foreach (var key in WandMasteryOrder)
            {
                if (string.Equals(key, masteryKey, StringComparison.OrdinalIgnoreCase))
                    return key;
            }

            return masteryKey.Trim();
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
                var property = type.GetProperty(memberName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                if (property != null && TryConvertToUShort(property.GetValue(source, null), out selectedType))
                    return true;

                var field = type.GetField(memberName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                if (field != null && TryConvertToUShort(field.GetValue(source), out selectedType))
                    return true;

                var method = type.GetMethod(memberName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic, null, Type.EmptyTypes, null);
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

        private static bool TryResolveSelectedWandMastery(ushort selectedType, out string masteryKey, out string displayName)
        {
            masteryKey = null;
            displayName = null;
            if (selectedType == 0)
                return false;

            if (TryResolveLineageItemInfo(selectedType, out var lineageBranch, out var lineageTier, out var lineageKey))
            {
                masteryKey = lineageKey;
                displayName = GetLineageDisplayName(lineageBranch, lineageTier, lineageKey);
                return true;
            }

            if (Pandaros.Settlers.Items.LegendaryWandItems.TryGetMasteryKeyByItemIndex(selectedType, out masteryKey))
            {
                displayName = LegendaryWandEvolutionManager.GetLegendaryName(masteryKey);
                return true;
            }

            if (Pandaros.Settlers.Items.ManaWand.Item != null && selectedType == Pandaros.Settlers.Items.ManaWand.Item.ItemIndex)
            {
                masteryKey = WandMasteryKeys.Mana;
                displayName = "Mana Wand";
                return true;
            }

            if (Pandaros.Settlers.Items.BriarWand.Item != null && selectedType == Pandaros.Settlers.Items.BriarWand.Item.ItemIndex)
            {
                masteryKey = WandMasteryKeys.Briar;
                displayName = "Briar Wand";
                return true;
            }

            if (Pandaros.Settlers.Items.SparkWand.Item != null && selectedType == Pandaros.Settlers.Items.SparkWand.Item.ItemIndex)
            {
                masteryKey = WandMasteryKeys.Spark;
                displayName = "Spark Wand";
                return true;
            }

            if (Pandaros.Settlers.Items.VenomWand.Item != null && selectedType == Pandaros.Settlers.Items.VenomWand.Item.ItemIndex)
            {
                masteryKey = WandMasteryKeys.Venom;
                displayName = "Venom Wand";
                return true;
            }

            if (Pandaros.Settlers.Items.EmberWand.Item != null && selectedType == Pandaros.Settlers.Items.EmberWand.Item.ItemIndex)
            {
                masteryKey = WandMasteryKeys.Ember;
                displayName = "Ember Wand";
                return true;
            }

            if (Pandaros.Settlers.Items.FrostWand.Item != null && selectedType == Pandaros.Settlers.Items.FrostWand.Item.ItemIndex)
            {
                masteryKey = WandMasteryKeys.Frost;
                displayName = "Frost Wand";
                return true;
            }

            if (Pandaros.Settlers.Items.CrystalWand.Item != null && selectedType == Pandaros.Settlers.Items.CrystalWand.Item.ItemIndex)
            {
                masteryKey = WandMasteryKeys.Crystal;
                displayName = "Crystal Wand";
                return true;
            }

            if (Pandaros.Settlers.Items.StoneWand.Item != null && selectedType == Pandaros.Settlers.Items.StoneWand.Item.ItemIndex)
            {
                masteryKey = WandMasteryKeys.Stone;
                displayName = "Stone Wand";
                return true;
            }

            if (Pandaros.Settlers.Items.StormWand.Item != null && selectedType == Pandaros.Settlers.Items.StormWand.Item.ItemIndex)
            {
                masteryKey = WandMasteryKeys.Storm;
                displayName = "Storm Wand";
                return true;
            }

            if (Pandaros.Settlers.Items.MagicWand.Item != null && selectedType == Pandaros.Settlers.Items.MagicWand.Item.ItemIndex)
            {
                masteryKey = WandMasteryKeys.Aether;
                displayName = "Aether Wand";
                return true;
            }

            if (Pandaros.Settlers.Items.BloodWand.Item != null && selectedType == Pandaros.Settlers.Items.BloodWand.Item.ItemIndex)
            {
                masteryKey = WandMasteryKeys.Blood;
                displayName = "Blood Wand";
                return true;
            }

            if (Pandaros.Settlers.Items.VoidWand.Item != null && selectedType == Pandaros.Settlers.Items.VoidWand.Item.ItemIndex)
            {
                masteryKey = WandMasteryKeys.Void;
                displayName = "Void Wand";
                return true;
            }

            return false;
        }

        public static string GetWandLineageStatusText(Players.Player player)
        {
            var state = GetState(player);
            RepairLineageFromLegacyAndInventory(player, state, false);

            if (!state.WandLineageUnlocked)
                return "Wand Lineage: not started. Craft the Starter Wand to begin.";

            var branch = string.IsNullOrWhiteSpace(state.WandLineageBranch) ? "unbranched" : state.WandLineageBranch;
            var level = GetLineageMasteryLevel(state);
            return "Wand Lineage: T" + System.Math.Max(1, state.WandLineageTier) + " " + branch +
                " | " + GetWandMasteryLevelLabel(level) +
                " | Lineage " + state.WandLineageMasteryPoints +
                " pts | Branch " + state.WandLineageBranchPoints +
                " pts | Evolution: " + GetLineageEvolutionProgressText(state) +
                " | Effects: " + GetLineageEffectsText(state) +
                " | " + GetNextLineageMilestoneText(state);
        }

        public static int GetWandLineageTier(Players.Player player)
        {
            return System.Math.Max(0, GetState(player).WandLineageTier);
        }

        public static string GetWandLineageBranch(Players.Player player)
        {
            return GetState(player).WandLineageBranch ?? string.Empty;
        }

        public static bool ResetWandLineage(Players.Player player, out string message)
        {
            message = null;
            if (player == null)
            {
                message = "No player context available.";
                return false;
            }

            var state = GetState(player);
            ResetWandLineageState(state, true);
            EnsureLineageMasteryMirrors(state);

            if (!TryRepairLineageWandItem(player, state, out var starterItemIndex))
            {
                starterItemIndex = Pandaros.Settlers.Items.ManaWand.Item?.ItemIndex ?? 0;
                if (starterItemIndex == 0 || !TryAddInventoryItem(player, starterItemIndex, 1))
                {
                    message = "Wand lineage progress was reset, but the Starter Wand could not be placed. Free an inventory slot and use /bnmagic resetwand again.";
                    return false;
                }

                ForceWandInventoryRefresh(player, starterItemIndex);
            }

            ShowItemIndicator(player, starterItemIndex, 1.1f, 0L);
            message = "Wand lineage reset. Your current wand is now the Starter Wand again.";
            PlayerToastManager.Show(player, message, "#bfe7ff", 5200L);
            return true;
        }

        public static bool TryChooseWandBranch(Players.Player player, string branchKey, out string message)
        {
            message = null;
            if (player == null)
            {
                message = "No player context available.";
                return false;
            }

            var state = GetState(player);
            RepairLineageFromLegacyAndInventory(player, state, false);

            if (!state.WandLineageUnlocked)
            {
                message = "Craft and use the Starter Wand first.";
                return false;
            }

            var settings = WandProgressionConfig.Current.Lineage;
            var targetTier = ResolvePendingEvolutionTier(state);
            if (targetTier <= 0)
            {
                var nextRequirement = GetNextEvolutionRequirement(state);
                message = nextRequirement > 0
                    ? "Next wand evolution is not ready yet: " + state.WandLineageMasteryPoints + "/" + nextRequirement + " lineage points."
                    : "No evolution choice is currently pending.";
                return false;
            }

            if (!TryNormalizeBranchKey(branchKey, out var branch))
            {
                message = "Unknown branch. Choose: frost, ember, venom, or stone.";
                return false;
            }

            state.WandLineageUnlocked = true;
            state.WandLineageBranch = branch;
            state.WandLineageTier = System.Math.Max(targetTier, state.WandLineageTier);
            state.WandLineageChoicePending = false;
            state.WandLineagePendingTier = 0;
            state.WandLineageRitualAwakened = false;
            state.NextWandEvolutionChoicePopupAtMs = 0L;
            state.WandLineageChoiceOpenedAtMs = 0L;
            AddLineageEffectsForChoice(state, branch, state.WandLineageTier);
            EnsureLineageMasteryMirrors(state);

            if (!TryRepairLineageWandItem(player, state, out var targetItemIndex))
            {
                message = "Your evolution was saved, but the wand item could not be transformed. Free an inventory slot and use /bnmagic lineage.";
                return false;
            }

            ShowItemIndicator(player, targetItemIndex, 1.2f, 0L);
            CacheObservedWand(state, ResolveLineageBehaviorKey(branch, state.WandLineageTier), GetLineageDisplayName(branch, state.WandLineageTier, null));
            message = "Your wand evolved to Tier " + state.WandLineageTier + " " + branch + " form. Inherited effects: " + GetLineageEffectsText(state) + ".";
            PlayerToastManager.Show(player, message, "#bfe7ff", 5200L);
            return true;
        }

        public static bool TryAwakenWandByRitual(Players.Player player, out string message)
        {
            message = null;
            if (player == null)
            {
                message = "No player available for the ritual awakening.";
                return false;
            }

            var state = GetState(player);
            RepairLineageFromLegacyAndInventory(player, state, false);

            var settings = WandProgressionConfig.Current.RitualAwakening;
            if (!state.WandLineageUnlocked || string.IsNullOrWhiteSpace(state.WandLineageBranch))
            {
                message = "Ritual awakening requires a branched wand lineage. " + settings.ChatHint;
                return false;
            }

            if (state.WandLineageTier < settings.RequiredTier)
            {
                message = "Ritual awakening requires Tier " + settings.RequiredTier + ". Current wand is Tier " + state.WandLineageTier + ".";
                return false;
            }

            if (state.WandLineageTier >= 6 && state.WandLineageRitualAwakened)
            {
                message = "Your wand lineage is already ritually awakened. This ritual will add sacrifice power instead: +1% spell damage per sacrificed colonist. Current ritual power: +" + Pandaros.Settlers.Items.RitualWandManager.GetDamageBonusPercent() + "% damage.";
                PlayerToastManager.Show(player, message, "#e8d4ff", 7000L);
                Chat.Send(player, "[Wand Evolution] " + message);
                return true;
            }

            state.WandLineageTier = 6;
            state.WandLineageRitualAwakened = true;
            state.WandLineageChoicePending = false;
            state.WandLineagePendingTier = 0;
            state.WandLineageMasteryPoints += settings.MasteryPointsGranted;
            state.WandLineageBranchPoints += settings.MasteryPointsGranted;
            state.GlobalArcanePoints += settings.MasteryPointsGranted;
            AddLineageEffectsForChoice(state, state.WandLineageBranch, 6);
            AddLineageEffect(state, WandLineageEffects.Ascended);
            EnsureLineageMasteryMirrors(state);

            if (!TryRepairLineageWandItem(player, state, out var targetItemIndex))
            {
                message = "The ritual accepted the lineage, but the awakened wand item could not be placed. Free an inventory slot and use /bnmagic lineage.";
                return false;
            }

            ShowItemIndicator(player, targetItemIndex, 1.4f, 0L);
            var branchDefinition = WandProgressionConfig.GetBranch(state.WandLineageBranch);
            var awakenedName = branchDefinition?.T6Name ?? GetLineageDisplayName(state.WandLineageBranch, 6, null);
            message = "Ritual awakening complete: your " + state.WandLineageBranch + " wand ascended into " + awakenedName + ". Future ritual sacrifices now empower this awakened wand: +1% spell damage per sacrificed colonist. Current ritual power: +" + Pandaros.Settlers.Items.RitualWandManager.GetDamageBonusPercent() + "% damage.";
            PlayerToastManager.Show(player, message, "#e8d4ff", 7000L);
            Chat.Send(player, "[Wand Evolution] " + message);
            return true;
        }

        public static bool IsLineageWandItem(ushort itemIndex)
        {
            return TryResolveLineageItemInfo(itemIndex, out _, out _, out _);
        }

        public static bool TryGetLineageTooltip(Players.Player player, ushort itemIndex, out List<string> lines)
        {
            lines = null;
            if (!TryResolveLineageItemInfo(itemIndex, out var branch, out var tier, out var masteryKey))
                return false;

            var state = player == null ? null : GetState(player);
            var effectiveBranch = state != null && !string.IsNullOrWhiteSpace(state.WandLineageBranch) ? state.WandLineageBranch : branch;
            var effectiveTier = state != null && state.WandLineageUnlocked ? System.Math.Max(1, state.WandLineageTier) : System.Math.Max(1, tier);
            var branchDefinition = WandProgressionConfig.GetBranch(effectiveBranch);
            var level = state != null && state.WandLineageUnlocked ? GetLineageMasteryLevel(state) : 0;

            lines = new List<string>
            {
                GetLineageDisplayName(effectiveBranch, effectiveTier, masteryKey),
                "Tier / Stage: " + effectiveTier + (effectiveTier >= 6 ? " ritual awakened" : string.Empty),
                "Role: " + (branchDefinition?.Role ?? "starter / flexible"),
                "Primary: " + (branchDefinition?.Primary ?? GetWandMasteryDisplayName(masteryKey) + " cast"),
                "Secondary: " + (branchDefinition?.Secondary ?? "Overcharged cast"),
                "Passive: " + (branchDefinition?.Passive ?? "Grows through one persistent wand lineage.")
            };

            if (state != null && state.WandLineageUnlocked)
            {
                lines.Add("Mastery: " + GetWandMasteryLevelLabel(level) + " | +" + (level * WandProgressionConfig.Current.Lineage.DamageBonusPerMasteryLevel * 100f).ToString("0") + "% damage");
                lines.Add("Evolution: " + GetLineageEvolutionProgressText(state));
                lines.Add("Effects: " + GetLineageEffectsText(state));
                lines.Add("Next: " + GetNextLineageMilestoneText(state));
            }
            else
            {
                lines.Add("Mastery: craft and use the Starter Wand to bind this lineage.");
            }

            return true;
        }

        public static bool TryGetWandLineageCombatProfile(Players.Player player, string masteryKey, out WandLineageCombatProfile profile)
        {
            profile = null;
            if (player == null)
                return false;

            var state = GetState(player);
            if (state == null || !state.WandLineageUnlocked || !IsMasteryKeyPartOfLineage(state, masteryKey))
                return false;

            if (state.WandLineageEffectKeys.Count == 0 && !string.IsNullOrWhiteSpace(state.WandLineageBranch))
                AddLineageEffectsForChoice(state, state.WandLineageBranch, System.Math.Max(2, state.WandLineageTier));

            profile = new WandLineageCombatProfile
            {
                Tier = System.Math.Max(1, state.WandLineageTier),
                MasteryLevel = GetLineageMasteryLevel(state),
                RitualAwakened = state.WandLineageRitualAwakened || state.WandLineageTier >= 6,
                CurrentForm = state.WandLineageBranch ?? string.Empty
            };

            foreach (var effect in state.WandLineageEffectKeys)
            {
                var normalized = NormalizeLineageEffectKey(effect);
                if (!string.IsNullOrWhiteSpace(normalized) && !profile.Effects.Contains(normalized))
                    profile.Effects.Add(normalized);
            }

            if (profile.RitualAwakened && !profile.Effects.Contains(WandLineageEffects.Ascended))
                profile.Effects.Add(WandLineageEffects.Ascended);

            return profile.Effects.Count > 0;
        }

        private static bool TryRecordLineageMasteryUse(Players.Player player, PlayerMagicState state, string masteryKey, ushort itemIndex, int amount, string source, out int level)
        {
            level = 0;
            if (player == null || state == null || amount <= 0 || itemIndex == 0)
                return false;

            if (!TryResolveLineageItemInfo(itemIndex, out var itemBranch, out var itemTier, out var behaviorKey))
                return false;

            if (!state.WandLineageUnlocked)
                InitializeLineageFromItem(state, itemBranch, itemTier);

            if (!IsItemCompatibleWithLineage(state, itemBranch, itemTier))
            {
                if (itemTier <= 1)
                    ShowStarterWandResetHint(player, state);

                TryRepairLineageWandItem(player, state, out _);
                level = GetLineageMasteryLevel(state);
                return true;
            }

            var previousLevel = GetLineageMasteryLevel(state);
            var weightedAmount = GetLineageSourceWeight(state, source, amount);
            state.GlobalArcanePoints += weightedAmount;
            state.WandLineageMasteryPoints += weightedAmount;
            if (!string.IsNullOrWhiteSpace(state.WandLineageBranch))
                state.WandLineageBranchPoints += weightedAmount;

            EnsureLineageMasteryMirrors(state);
            CacheObservedWand(state, behaviorKey, GetLineageDisplayName(state.WandLineageBranch, state.WandLineageTier, behaviorKey));
            var newLevel = GetLineageMasteryLevel(state);
            level = newLevel;

            if (newLevel > previousLevel)
            {
                ShowItemIndicator(player, itemIndex, 0.95f, 0L);
                SendEquipChat(player, "Wand Lineage reached " + GetWandMasteryLevelLabel(newLevel) + ". +" + (newLevel * WandProgressionConfig.Current.Lineage.DamageBonusPerMasteryLevel * 100f).ToString("0") + "% lineage damage.");
            }

            UpdateLineageEvolution(player, state, itemIndex);
            return true;
        }

        private static void InitializeLineageFromItem(PlayerMagicState state, string branch, int tier)
        {
            state.WandLineageUnlocked = true;
            state.WandLineageBranch = tier <= 1 ? null : NormalizeBranchOrNull(branch);
            state.WandLineageTier = System.Math.Max(1, tier);
            state.WandLineageMasteryPoints = System.Math.Max(state.WandLineageMasteryPoints, GetHighestLegacyWandMasteryPoints(state));
            state.WandLineageBranchPoints = System.Math.Max(state.WandLineageBranchPoints, state.WandLineageBranch == null ? 0 : state.WandLineageMasteryPoints);
            state.GlobalArcanePoints = System.Math.Max(state.GlobalArcanePoints, state.WandLineageMasteryPoints);
            if (!string.IsNullOrWhiteSpace(state.WandLineageBranch))
                AddLineageEffectsForChoice(state, state.WandLineageBranch, state.WandLineageTier);
            EnsureLineageMasteryMirrors(state);
        }

        private static void ResetWandLineageState(PlayerMagicState state, bool keepUnlocked)
        {
            if (state == null)
                return;

            state.WandLineageUnlocked = keepUnlocked;
            state.WandLineageBranch = null;
            state.WandLineageTier = keepUnlocked ? 1 : 0;
            state.WandLineageMasteryPoints = 0;
            state.WandLineageBranchPoints = 0;
            state.GlobalArcanePoints = 0;
            state.WandLineageChoicePending = false;
            state.WandLineagePendingTier = 0;
            state.WandLineageRitualAwakened = false;
            state.NextWandEvolutionChoicePopupAtMs = 0L;
            state.WandLineageChoiceOpenedAtMs = 0L;
            state.WandLineageEffectKeys.Clear();
            state.LastObservedWandMasteryKey = null;
            state.LastObservedWandDisplayName = null;
            state.LastObservedWandUntilMs = 0L;
            state.LastWandMasteryHudSignature = null;

            EnsureWandMasteryDefaults(state);
            EnsureLegendaryWandDefaults(state);
            EnsureLegendaryWandMasteryDefaults(state);
            for (var i = 0; i < WandMasteryOrder.Length; i++)
            {
                var key = WandMasteryOrder[i];
                state.WandMasteryUses[key] = 0;
                state.LegendaryWandUnlocks[key] = false;
                state.LegendaryWandMasteryUses[key] = 0;
            }
        }

        private static void UpdateLineageEvolution(Players.Player player, PlayerMagicState state, ushort currentItemIndex)
        {
            if (state == null || !state.WandLineageUnlocked)
                return;

            var settings = WandProgressionConfig.Current.Lineage;
            if (string.IsNullOrWhiteSpace(state.WandLineageBranch))
            {
                if (state.WandLineageMasteryPoints >= settings.FirstBranchPoints)
                    BeginLineageEvolutionChoice(player, state, 2, "Your Starter Wand is ready to evolve.");

                return;
            }

            var targetTier = GetLineageTierForPoints(state.WandLineageMasteryPoints);
            if (targetTier <= state.WandLineageTier || state.WandLineageTier >= 5)
                return;

            var nextTier = System.Math.Min(targetTier, state.WandLineageTier + 1);
            BeginLineageEvolutionChoice(player, state, nextTier, "Your wand reached a new evolution stage.");
        }

        private static void BeginLineageEvolutionChoice(Players.Player player, PlayerMagicState state, int targetTier, string message)
        {
            if (state == null || targetTier <= 1 || targetTier > 5)
                return;

            if (state.WandLineageChoicePending && state.WandLineagePendingTier == targetTier)
            {
                OpenWandEvolutionChoicePopup(player, state, false);
                return;
            }

            state.WandLineageChoicePending = true;
            state.WandLineagePendingTier = targetTier;
            state.NextWandEvolutionChoicePopupAtMs = 0L;
            state.WandLineageChoiceOpenedAtMs = 0L;
            SendEquipChat(player, message + " Choose the Tier " + targetTier + " form in the evolution window.");
            PlayerToastManager.Show(player, "Wand evolution available: Tier " + targetTier + " form choice.", "#bfe7ff", 7000L);
            OpenWandEvolutionChoicePopup(player, state, true);
        }

        private static void UpdateWandEvolutionChoicePopup(Players.Player player, PlayerMagicState state, long now)
        {
            if (player == null || state == null)
                return;

            if (!state.WandLineageChoicePending)
                return;

            if (ResolvePendingEvolutionTier(state) <= 0)
                return;

            if (now < state.NextWandEvolutionChoicePopupAtMs)
                return;

            OpenWandEvolutionChoicePopup(player, state, false);
        }

        private static void OpenWandEvolutionChoicePopup(Players.Player player, PlayerMagicState state, bool force)
        {
            if (player == null || state == null)
                return;

            if (!state.WandLineageChoicePending)
                return;

            if (!PlayerUiGuard.CanSendStable(player))
            {
                state.NextWandEvolutionChoicePopupAtMs = Pipliz.Time.MillisecondsSinceStart + 3000L;
                return;
            }

            var now = Pipliz.Time.MillisecondsSinceStart;
            if (!force && now < state.NextWandEvolutionChoicePopupAtMs)
                return;

            var targetTier = System.Math.Max(2, ResolvePendingEvolutionTier(state));
            var menu = new NetworkUI.NetworkMenu
            {
                Identifier = "Wand Evolution",
                Width = 760,
                Height = 560,
                ForceClosePopups = true,
                IsInteractive = true
            };

            AddEvolutionChoiceLabel(menu, "Tier " + targetTier + " Wand Evolution", 24, "#dff7ff");
            AddEvolutionChoiceLabel(menu, "Choose one form for this stage. Previous effects stay inherited on the same wand.", 15, "#f3f7ff");
            AddEvolutionChoiceLabel(menu, "Safety lock: choices activate after about 1.6 seconds, so accidental clicks are ignored.", 14, "#ffdca8");
            if (state.WandLineageEffectKeys.Count > 0)
                AddEvolutionChoiceLabel(menu, "Current inherited effects: " + GetLineageEffectsText(state), 13, "#bfe7ff");
            else
                AddEvolutionChoiceLabel(menu, "No new craft chain. No restart grind. One persistent wand lineage.", 13, "#bfe7ff");
            AddEvolutionChoiceSpacer(menu, 10);

            AddEvolutionChoiceButton(menu, BranchFrost, "Frost Form", GetBranchChoiceDescription(BranchFrost, targetTier));
            AddEvolutionChoiceButton(menu, BranchEmber, "Ember Form", GetBranchChoiceDescription(BranchEmber, targetTier));
            AddEvolutionChoiceButton(menu, BranchVenom, "Venom Form", GetBranchChoiceDescription(BranchVenom, targetTier));
            AddEvolutionChoiceButton(menu, BranchStone, "Stone Form", GetBranchChoiceDescription(BranchStone, targetTier));

            AddEvolutionChoiceSpacer(menu, 8);
            AddEvolutionChoiceLabel(menu, "This choice is permanent for this lineage. Later tiers deepen and awaken this same wand.", 13, "#e8d4ff");

            NetworkUI.NetworkMenuManager.SendServerPopup(player, menu);
            state.NextWandEvolutionChoicePopupAtMs = now + WandEvolutionChoiceReopenIntervalMs;
            state.WandLineageChoiceOpenedAtMs = now;
        }

        private static void AddEvolutionChoiceLabel(NetworkUI.NetworkMenu menu, string text, int size, string color)
        {
            if (menu == null)
                return;

            menu.Items.Add(new NetworkUI.Items.Label(new NetworkUI.LabelData(
                text,
                ParseUiColor(color, UnityEngine.Color.white),
                NetworkUI.ELabelAlignment.MiddleCenter,
                size,
                NetworkUI.LabelData.ELocalizationType.None)));
        }

        private static void AddEvolutionChoiceSpacer(NetworkUI.NetworkMenu menu, int height)
        {
            if (menu == null)
                return;

            menu.Items.Add(new NetworkUI.Items.Line(new UnityEngine.Color(0f, 0f, 0f, 0f), 0, 0, 720, System.Math.Max(1, height)));
        }

        private static void AddEvolutionChoiceButton(NetworkUI.NetworkMenu menu, string branch, string title, string description)
        {
            if (menu == null)
                return;

            var buttonText = title + "\n" + description;
            var payload = new JObject
            {
                ["branch"] = branch
            };

            menu.Items.Add(new NetworkUI.Items.ButtonCallback(
                WandEvolutionChoiceButtonPrefix + branch.ToLowerInvariant(),
                new NetworkUI.LabelData(
                    buttonText,
                    ParseUiColor("#ffffff", UnityEngine.Color.white),
                    NetworkUI.ELabelAlignment.MiddleCenter,
                    15,
                    NetworkUI.LabelData.ELocalizationType.None),
                680,
                64,
                NetworkUI.Items.ButtonCallback.EOnClickActions.ClosePopup,
                payload));
        }

        private static UnityEngine.Color ParseUiColor(string color, UnityEngine.Color fallback)
        {
            if (!string.IsNullOrWhiteSpace(color) && UnityEngine.ColorUtility.TryParseHtmlString(color, out var parsed))
                return parsed;

            return fallback;
        }

        private static string GetBranchChoiceDescription(string branch, int tier)
        {
            var branchDefinition = WandProgressionConfig.GetBranch(branch);
            var baseRole = branchDefinition?.Role ?? branch;
            var added = GetEffectsAddedByChoiceText(branch, tier);
            return baseRole + ". Adds: " + added + ".";
        }

        private static string GetEffectsAddedByChoiceText(string branch, int tier)
        {
            var effects = new List<string>();
            CollectLineageEffectsForChoice(branch, tier, effects);
            return effects.Count == 0 ? "identity pressure" : string.Join(", ", effects.ToArray());
        }

        private static int ResolvePendingEvolutionTier(PlayerMagicState state)
        {
            if (state == null || !state.WandLineageChoicePending)
                return 0;

            var pendingTier = state.WandLineagePendingTier;
            if (pendingTier <= 1)
            {
                pendingTier = string.IsNullOrWhiteSpace(state.WandLineageBranch)
                    ? 2
                    : System.Math.Min(5, System.Math.Max(2, state.WandLineageTier + 1));
            }

            var required = GetPointsRequiredForTier(pendingTier);
            if (required <= 0 || state.WandLineageMasteryPoints < required)
                return 0;

            return System.Math.Max(2, System.Math.Min(5, pendingTier));
        }

        private static int GetNextEvolutionRequirement(PlayerMagicState state)
        {
            if (state == null || !state.WandLineageUnlocked)
                return WandProgressionConfig.Current.Lineage.FirstBranchPoints;

            var nextTier = GetNextEvolutionTier(state);
            return nextTier <= 0 ? 0 : GetPointsRequiredForTier(nextTier);
        }

        private static int GetNextEvolutionTier(PlayerMagicState state)
        {
            if (state == null || !state.WandLineageUnlocked)
                return 2;

            if (state.WandLineageChoicePending)
                return ResolvePendingEvolutionTier(state);

            if (string.IsNullOrWhiteSpace(state.WandLineageBranch))
                return 2;

            if (state.WandLineageTier < 5)
                return state.WandLineageTier + 1;

            return state.WandLineageTier < 6 ? 6 : 0;
        }

        private static int GetPointsRequiredForTier(int tier)
        {
            var settings = WandProgressionConfig.Current.Lineage;
            switch (tier)
            {
                case 2:
                    return settings.FirstBranchPoints;
                case 3:
                    return settings.Tier3Points;
                case 4:
                    return settings.Tier4Points;
                case 5:
                    return settings.Tier5Points;
                default:
                    return 0;
            }
        }

        private static string GetLineageEvolutionProgressText(PlayerMagicState state)
        {
            if (state == null || !state.WandLineageUnlocked)
                return "not started";

            if (state.WandLineageChoicePending)
            {
                var targetTier = ResolvePendingEvolutionTier(state);
                return targetTier > 0
                    ? "Evolution Available: choose Tier " + targetTier + " form"
                    : "Evolution pending";
            }

            if (state.WandLineageTier >= 6)
                return "Tier 6 awakened";

            if (state.WandLineageTier >= 5)
                return state.WandLineageRitualAwakened ? "Tier 6 awakened" : "Tier 6 ritual locked: ready for awakening ritual";

            var nextTier = GetNextEvolutionTier(state);
            var requirement = GetPointsRequiredForTier(nextTier);
            if (nextTier <= 0 || requirement <= 0)
                return "max normal evolution reached";

            var previousRequirement = GetPointsRequiredForTier(nextTier - 1);
            var current = System.Math.Max(0, state.WandLineageMasteryPoints - previousRequirement);
            var needed = System.Math.Max(1, requirement - previousRequirement);
            return "Tier " + nextTier + " progress " + System.Math.Min(current, needed) + "/" + needed + " pts (" + state.WandLineageMasteryPoints + "/" + requirement + " total)";
        }

        private static string GetLineageEvolutionHudText(PlayerMagicState state)
        {
            if (state == null || !state.WandLineageUnlocked)
                return "Evolution: not started";

            if (state.WandLineageChoicePending)
            {
                var targetTier = ResolvePendingEvolutionTier(state);
                return targetTier > 0
                    ? "Evolution ready: choose T" + targetTier
                    : "Evolution ready";
            }

            if (state.WandLineageTier >= 6)
                return "Evolution: T6 awakened";

            if (state.WandLineageTier >= 5)
                return state.WandLineageRitualAwakened ? "Evolution: T6 awakened" : "Evolution: T6 ritual ready";

            var nextTier = GetNextEvolutionTier(state);
            var requirement = GetPointsRequiredForTier(nextTier);
            if (nextTier <= 0 || requirement <= 0)
                return "Evolution: max normal tier";

            var previousRequirement = GetPointsRequiredForTier(nextTier - 1);
            var current = System.Math.Max(0, state.WandLineageMasteryPoints - previousRequirement);
            var needed = System.Math.Max(1, requirement - previousRequirement);
            return "Evolution T" + nextTier + ": " + System.Math.Min(current, needed) + "/" + needed + " pts";
        }

        private static string GetLineageEffectsText(PlayerMagicState state)
        {
            if (state == null || state.WandLineageEffectKeys.Count == 0)
                return "none yet";

            var names = new List<string>();
            foreach (var effect in state.WandLineageEffectKeys)
                names.Add(GetLineageEffectDisplayName(effect));

            names.Sort(StringComparer.OrdinalIgnoreCase);
            return string.Join(" + ", names.ToArray());
        }

        private static string GetLineageEffectsHudText(PlayerMagicState state)
        {
            if (state == null || state.WandLineageEffectKeys.Count == 0)
                return "none yet";

            var names = new List<string>();
            foreach (var effect in state.WandLineageEffectKeys)
                names.Add(GetLineageEffectDisplayName(effect));

            names.Sort(StringComparer.OrdinalIgnoreCase);
            if (names.Count <= 4)
                return string.Join(" + ", names.ToArray());

            var visible = names.GetRange(0, 4);
            return string.Join(" + ", visible.ToArray()) + " +" + (names.Count - visible.Count);
        }

        private static string GetLineageEffectDisplayName(string effect)
        {
            switch (NormalizeLineageEffectKey(effect))
            {
                case WandLineageEffects.Poison:
                    return "Poison";
                case WandLineageEffects.PoisonSpread:
                    return "Poison Spread";
                case WandLineageEffects.Burn:
                    return "Burn";
                case WandLineageEffects.Explosion:
                    return "Explosion";
                case WandLineageEffects.Freeze:
                    return "Freeze";
                case WandLineageEffects.Shatter:
                    return "Shatter";
                case WandLineageEffects.Stagger:
                    return "Stagger";
                case WandLineageEffects.ArmorBreak:
                    return "Armor Break";
                case WandLineageEffects.Chain:
                    return "Chain";
                case WandLineageEffects.Bleed:
                    return "Bleed";
                case WandLineageEffects.Bramble:
                    return "Bramble";
                case WandLineageEffects.Rupture:
                    return "Rupture";
                case WandLineageEffects.Prism:
                    return "Prism";
                case WandLineageEffects.Quake:
                    return "Quake";
                case WandLineageEffects.Ascended:
                    return "Ascended";
                default:
                    return effect ?? string.Empty;
            }
        }

        private static void AddLineageEffectsForChoice(PlayerMagicState state, string branch, int tier)
        {
            if (state == null)
                return;

            var effects = new List<string>();
            CollectLineageEffectsForChoice(branch, tier, effects);
            for (var i = 0; i < effects.Count; i++)
                AddLineageEffect(state, effects[i]);
        }

        private static void CollectLineageEffectsForChoice(string branch, int tier, List<string> effects)
        {
            if (effects == null)
                return;

            switch (NormalizeBranchOrNull(branch))
            {
                case BranchFrost:
                    effects.Add(WandLineageEffects.Freeze);
                    if (tier >= 3) effects.Add(WandLineageEffects.Shatter);
                    if (tier >= 4) effects.Add(WandLineageEffects.Chain);
                    if (tier >= 5) effects.Add(WandLineageEffects.Prism);
                    break;

                case BranchEmber:
                    effects.Add(WandLineageEffects.Burn);
                    if (tier >= 3) effects.Add(WandLineageEffects.Explosion);
                    if (tier >= 4) effects.Add(WandLineageEffects.Chain);
                    if (tier >= 5) effects.Add(WandLineageEffects.Bleed);
                    break;

                case BranchVenom:
                    effects.Add(WandLineageEffects.Poison);
                    if (tier >= 3) effects.Add(WandLineageEffects.PoisonSpread);
                    if (tier >= 4) effects.Add(WandLineageEffects.Bramble);
                    if (tier >= 5) effects.Add(WandLineageEffects.Rupture);
                    break;

                case BranchStone:
                    effects.Add(WandLineageEffects.Stagger);
                    if (tier >= 3) effects.Add(WandLineageEffects.ArmorBreak);
                    if (tier >= 4) effects.Add(WandLineageEffects.Quake);
                    if (tier >= 5) effects.Add(WandLineageEffects.Chain);
                    break;
            }

            if (tier >= 6)
                effects.Add(WandLineageEffects.Ascended);
        }

        private static bool AddLineageEffect(PlayerMagicState state, string effect)
        {
            var normalized = NormalizeLineageEffectKey(effect);
            return state != null && !string.IsNullOrWhiteSpace(normalized) && state.WandLineageEffectKeys.Add(normalized);
        }

        private static string NormalizeLineageEffectKey(string effect)
        {
            if (string.IsNullOrWhiteSpace(effect))
                return string.Empty;

            switch (effect.Trim().ToLowerInvariant())
            {
                case "poison":
                    return WandLineageEffects.Poison;
                case "poisonspread":
                case "poison spread":
                case "spread":
                    return WandLineageEffects.PoisonSpread;
                case "burn":
                case "ignite":
                    return WandLineageEffects.Burn;
                case "explosion":
                case "explode":
                    return WandLineageEffects.Explosion;
                case "freeze":
                case "frost":
                    return WandLineageEffects.Freeze;
                case "shatter":
                    return WandLineageEffects.Shatter;
                case "stagger":
                    return WandLineageEffects.Stagger;
                case "armorbreak":
                case "armor break":
                    return WandLineageEffects.ArmorBreak;
                case "chain":
                case "lightning":
                    return WandLineageEffects.Chain;
                case "bleed":
                case "blood":
                    return WandLineageEffects.Bleed;
                case "bramble":
                case "thorn":
                    return WandLineageEffects.Bramble;
                case "rupture":
                case "void":
                    return WandLineageEffects.Rupture;
                case "prism":
                case "crystal":
                    return WandLineageEffects.Prism;
                case "quake":
                case "earthquake":
                    return WandLineageEffects.Quake;
                case "ascended":
                case "awakened":
                    return WandLineageEffects.Ascended;
                default:
                    return effect.Trim();
            }
        }

        private static int GetLineageTierForPoints(int points)
        {
            var settings = WandProgressionConfig.Current.Lineage;
            if (points >= settings.Tier5Points)
                return 5;
            if (points >= settings.Tier4Points)
                return 4;
            if (points >= settings.Tier3Points)
                return 3;
            if (points >= settings.FirstBranchPoints)
                return 2;
            return 1;
        }

        private static int GetLineageMasteryLevel(PlayerMagicState state)
        {
            if (state == null)
                return 0;

            var settings = WandProgressionConfig.Current.Lineage;
            return System.Math.Max(0, System.Math.Min(settings.MaxMasteryLevel, state.WandLineageMasteryPoints / settings.MasteryPointsPerLevel));
        }

        private static int GetLineageSourceWeight(PlayerMagicState state, string source, int amount)
        {
            if (state == null || string.IsNullOrWhiteSpace(state.WandLineageBranch))
                return amount;

            var branchDefinition = WandProgressionConfig.GetBranch(state.WandLineageBranch);
            if (branchDefinition?.MasterySources != null &&
                branchDefinition.MasterySources.TryGetValue(source ?? "OnCast", out var weight))
            {
                return System.Math.Max(1, amount * System.Math.Max(1, weight));
            }

            return amount;
        }

        private static bool IsItemCompatibleWithLineage(PlayerMagicState state, string itemBranch, int itemTier)
        {
            if (state == null || !state.WandLineageUnlocked)
                return true;

            if (state.WandLineageTier <= 1)
                return itemTier <= 1;

            return string.Equals(state.WandLineageBranch, itemBranch, StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryRepairLineageWandItem(Players.Player player, PlayerMagicState state, out ushort targetItemIndex)
        {
            targetItemIndex = 0;
            if (player?.Inventory == null || state == null || !state.WandLineageUnlocked)
                return false;

            if (!TryResolveLineageItemIndex(state.WandLineageBranch, System.Math.Max(1, state.WandLineageTier), out targetItemIndex) || targetItemIndex == 0)
                return false;

            var currentItems = GetAllLineageItemIndexes();
            var alreadyHasTarget = PlayerHasItem(player, targetItemIndex);
            var replacedOrAdded = alreadyHasTarget ||
                PlayerHasItem(player, targetItemIndex) ||
                TryReplaceAnyInventoryItem(player.Inventory, currentItems, targetItemIndex) ||
                TryAddInventoryItem(player, targetItemIndex, 1);

            if (!replacedOrAdded)
                return false;

            RemoveDuplicateLineageItems(player, currentItems, targetItemIndex);
            ForceWandInventoryRefresh(player, targetItemIndex);
            return true;
        }

        private static void ShowStarterWandResetHint(Players.Player player, PlayerMagicState state)
        {
            if (player == null || state == null)
                return;

            var now = Pipliz.Time.MillisecondsSinceStart;
            if (now < state.NextStarterWandResetHintAtMs)
                return;

            state.NextStarterWandResetHintAtMs = now + StarterWandResetHintCooldownMs;
            Chat.Send(player, StarterWandResetHintMessage);
            PlayerToastManager.Show(player, StarterWandResetHintMessage, "#bfe7ff", 6200L, -96, 980f, 18f);
        }

        private static void RemoveDuplicateLineageItems(Players.Player player, List<ushort> currentItems, ushort targetItemIndex)
        {
            if (player?.Inventory == null || currentItems == null)
                return;

            for (var i = 0; i < currentItems.Count; i++)
            {
                var itemIndex = currentItems[i];
                if (itemIndex == 0 || itemIndex == targetItemIndex)
                    continue;

                while (player.Inventory.TryRemove(itemIndex))
                {
                }
            }

            TryNotifyInventoryChanged(player.Inventory);
        }

        private static void ForceWandInventoryRefresh(Players.Player player, ushort targetItemIndex)
        {
            if (player?.Inventory == null)
                return;

            TryNotifyInventoryChanged(player.Inventory);
            TrySetSelectedTypeOnObject(player, targetItemIndex);
            TrySetSelectedTypeOnObject(player.Inventory, targetItemIndex);

            var state = GetState(player);
            state.LastWandMasteryHudSignature = null;
            UpdateWandMasteryHud(player, state);
        }

        private static void RepairLineageFromLegacyAndInventory(Players.Player player, PlayerMagicState state, bool notify)
        {
            if (player?.Inventory == null || state == null)
                return;

            var bestBranch = state.WandLineageBranch;
            var bestTier = state.WandLineageUnlocked ? System.Math.Max(1, state.WandLineageTier) : 0;
            var bestScore = bestTier + (string.IsNullOrWhiteSpace(bestBranch) ? 0 : 10);

            foreach (var itemIndex in GetAllLineageItemIndexes())
            {
                if (itemIndex == 0 || !PlayerHasItem(player, itemIndex))
                    continue;

                if (!TryResolveLineageItemInfo(itemIndex, out var branch, out var tier, out _))
                    continue;

                var score = tier + (string.IsNullOrWhiteSpace(branch) ? 0 : 10);
                if (score <= bestScore)
                    continue;

                bestScore = score;
                bestTier = tier;
                bestBranch = branch;
            }

            if (bestTier <= 0 && !state.WandLineageUnlocked)
                return;

            if (!state.WandLineageUnlocked || bestTier > state.WandLineageTier)
            {
                state.WandLineageUnlocked = true;
                state.WandLineageTier = System.Math.Max(1, bestTier);
                state.WandLineageBranch = bestTier <= 1 ? null : NormalizeBranchOrNull(bestBranch);
                state.WandLineageMasteryPoints = System.Math.Max(state.WandLineageMasteryPoints, GetHighestLegacyWandMasteryPoints(state));
                state.WandLineageBranchPoints = System.Math.Max(state.WandLineageBranchPoints, string.IsNullOrWhiteSpace(state.WandLineageBranch) ? 0 : state.WandLineageMasteryPoints);
                state.GlobalArcanePoints = System.Math.Max(state.GlobalArcanePoints, state.WandLineageMasteryPoints);
                state.WandLineageChoicePending = string.IsNullOrWhiteSpace(state.WandLineageBranch) &&
                    state.WandLineageMasteryPoints >= WandProgressionConfig.Current.Lineage.FirstBranchPoints;
                state.WandLineagePendingTier = state.WandLineageChoicePending ? 2 : 0;
                if (!string.IsNullOrWhiteSpace(state.WandLineageBranch))
                    AddLineageEffectsForChoice(state, state.WandLineageBranch, state.WandLineageTier);
                EnsureLineageMasteryMirrors(state);
                if (notify)
                    SendEquipChat(player, "Legacy wand progress was merged into one persistent Wand Lineage.");
            }

            TryRepairLineageWandItem(player, state, out _);
        }

        private static bool TryReplaceAnyInventoryItem(object inventory, List<ushort> oldItemIndexes, ushort newItemIndex)
        {
            if (oldItemIndexes == null)
                return false;

            for (var i = 0; i < oldItemIndexes.Count; i++)
            {
                var oldItemIndex = oldItemIndexes[i];
                if (oldItemIndex != 0 && oldItemIndex != newItemIndex && TryReplaceInventoryItem(inventory, oldItemIndex, newItemIndex))
                    return true;
            }

            return false;
        }

        private static List<ushort> GetAllLineageItemIndexes()
        {
            var indexes = new List<ushort>();
            AddLineageItemIndex(indexes, Pandaros.Settlers.Items.ManaWand.Item?.ItemIndex ?? 0);
            AddLineageItemIndex(indexes, Pandaros.Settlers.Items.FrostWand.Item?.ItemIndex ?? 0);
            AddLineageItemIndex(indexes, Pandaros.Settlers.Items.EmberWand.Item?.ItemIndex ?? 0);
            AddLineageItemIndex(indexes, Pandaros.Settlers.Items.VenomWand.Item?.ItemIndex ?? 0);
            AddLineageItemIndex(indexes, Pandaros.Settlers.Items.StoneWand.Item?.ItemIndex ?? 0);
            AddLineageItemIndex(indexes, Pandaros.Settlers.Items.CrystalWand.Item?.ItemIndex ?? 0);
            AddLineageItemIndex(indexes, Pandaros.Settlers.Items.StormWand.Item?.ItemIndex ?? 0);
            AddLineageItemIndex(indexes, Pandaros.Settlers.Items.BriarWand.Item?.ItemIndex ?? 0);
            AddLineageItemIndex(indexes, Pandaros.Settlers.Items.MagicWand.Item?.ItemIndex ?? 0);
            AddLineageItemIndex(indexes, Pandaros.Settlers.Items.BloodWand.Item?.ItemIndex ?? 0);
            AddLineageItemIndex(indexes, Pandaros.Settlers.Items.VoidWand.Item?.ItemIndex ?? 0);
            AddLineageItemIndex(indexes, Pandaros.Settlers.Items.SparkWand.Item?.ItemIndex ?? 0);
            foreach (var branch in WandLineageBranches)
            {
                for (var tier = 4; tier <= 6; tier++)
                {
                    if (TryResolveLineageItemIndex(branch, tier, out var itemIndex))
                        AddLineageItemIndex(indexes, itemIndex);
                }
            }

            return indexes;
        }

        private static void AddLineageItemIndex(List<ushort> indexes, ushort itemIndex)
        {
            if (itemIndex != 0 && !indexes.Contains(itemIndex))
                indexes.Add(itemIndex);
        }

        private static bool TryResolveLineageItemIndex(string branch, int tier, out ushort itemIndex)
        {
            itemIndex = 0;
            if (tier <= 1 || string.IsNullOrWhiteSpace(branch))
            {
                itemIndex = Pandaros.Settlers.Items.ManaWand.Item?.ItemIndex ?? 0;
                return itemIndex != 0;
            }

            switch (NormalizeBranchOrNull(branch))
            {
                case BranchFrost:
                    itemIndex = tier == 2 ? Pandaros.Settlers.Items.FrostWand.Item?.ItemIndex ?? 0 :
                        tier == 3 ? Pandaros.Settlers.Items.CrystalWand.Item?.ItemIndex ?? 0 :
                        tier == 4 ? Pandaros.Settlers.Items.LegendaryWandItems.WinterglassWand?.ItemIndex ?? 0 :
                        tier == 5 ? Pandaros.Settlers.Items.LegendaryWandItems.PrismSovereignWand?.ItemIndex ?? 0 :
                        Pandaros.Settlers.Items.LegendaryWandItems.SkyfallWand?.ItemIndex ?? 0;
                    return itemIndex != 0;

                case BranchEmber:
                    itemIndex = tier == 2 ? Pandaros.Settlers.Items.EmberWand.Item?.ItemIndex ?? 0 :
                        tier == 3 ? Pandaros.Settlers.Items.StormWand.Item?.ItemIndex ?? 0 :
                        tier == 4 ? Pandaros.Settlers.Items.LegendaryWandItems.CinderlordWand?.ItemIndex ?? 0 :
                        tier == 5 ? Pandaros.Settlers.Items.LegendaryWandItems.CrimsonCovenantWand?.ItemIndex ?? 0 :
                        Pandaros.Settlers.Items.LegendaryWandItems.SeraphWand?.ItemIndex ?? 0;
                    return itemIndex != 0;

                case BranchVenom:
                    itemIndex = tier == 2 ? Pandaros.Settlers.Items.VenomWand.Item?.ItemIndex ?? 0 :
                        tier == 3 ? Pandaros.Settlers.Items.BriarWand.Item?.ItemIndex ?? 0 :
                        tier == 4 ? Pandaros.Settlers.Items.LegendaryWandItems.WidowrootWand?.ItemIndex ?? 0 :
                        tier == 5 ? Pandaros.Settlers.Items.LegendaryWandItems.ThornheartWand?.ItemIndex ?? 0 :
                        Pandaros.Settlers.Items.LegendaryWandItems.EclipseWand?.ItemIndex ?? 0;
                    return itemIndex != 0;

                case BranchStone:
                    itemIndex = tier == 2 ? Pandaros.Settlers.Items.StoneWand.Item?.ItemIndex ?? 0 :
                        tier == 3 ? Pandaros.Settlers.Items.MagicWand.Item?.ItemIndex ?? 0 :
                        tier == 4 ? Pandaros.Settlers.Items.LegendaryWandItems.WorldbreakerWand?.ItemIndex ?? 0 :
                        tier == 5 ? Pandaros.Settlers.Items.LegendaryWandItems.VoltspineWand?.ItemIndex ?? 0 :
                        Pandaros.Settlers.Items.LegendaryWandItems.AstralManaWand?.ItemIndex ?? 0;
                    return itemIndex != 0;
            }

            return false;
        }

        private static bool TryResolveLineageItemInfo(ushort itemIndex, out string branch, out int tier, out string behaviorKey)
        {
            branch = null;
            tier = 0;
            behaviorKey = null;

            if (itemIndex == 0)
                return false;

            if (Pandaros.Settlers.Items.ManaWand.Item != null && itemIndex == Pandaros.Settlers.Items.ManaWand.Item.ItemIndex)
            {
                tier = 1;
                behaviorKey = WandMasteryKeys.Mana;
                return true;
            }

            return TryMatchLineageItem(itemIndex, BranchFrost, 2, WandMasteryKeys.Frost, Pandaros.Settlers.Items.FrostWand.Item?.ItemIndex ?? 0, ref branch, ref tier, ref behaviorKey) ||
                TryMatchLineageItem(itemIndex, BranchFrost, 3, WandMasteryKeys.Crystal, Pandaros.Settlers.Items.CrystalWand.Item?.ItemIndex ?? 0, ref branch, ref tier, ref behaviorKey) ||
                TryMatchLineageItem(itemIndex, BranchFrost, 4, WandMasteryKeys.Frost, Pandaros.Settlers.Items.LegendaryWandItems.WinterglassWand?.ItemIndex ?? 0, ref branch, ref tier, ref behaviorKey) ||
                TryMatchLineageItem(itemIndex, BranchFrost, 5, WandMasteryKeys.Crystal, Pandaros.Settlers.Items.LegendaryWandItems.PrismSovereignWand?.ItemIndex ?? 0, ref branch, ref tier, ref behaviorKey) ||
                TryMatchLineageItem(itemIndex, BranchFrost, 6, WandMasteryKeys.Storm, Pandaros.Settlers.Items.LegendaryWandItems.SkyfallWand?.ItemIndex ?? 0, ref branch, ref tier, ref behaviorKey) ||
                TryMatchLineageItem(itemIndex, BranchEmber, 2, WandMasteryKeys.Ember, Pandaros.Settlers.Items.EmberWand.Item?.ItemIndex ?? 0, ref branch, ref tier, ref behaviorKey) ||
                TryMatchLineageItem(itemIndex, BranchEmber, 2, WandMasteryKeys.Spark, Pandaros.Settlers.Items.SparkWand.Item?.ItemIndex ?? 0, ref branch, ref tier, ref behaviorKey) ||
                TryMatchLineageItem(itemIndex, BranchEmber, 3, WandMasteryKeys.Storm, Pandaros.Settlers.Items.StormWand.Item?.ItemIndex ?? 0, ref branch, ref tier, ref behaviorKey) ||
                TryMatchLineageItem(itemIndex, BranchEmber, 4, WandMasteryKeys.Ember, Pandaros.Settlers.Items.LegendaryWandItems.CinderlordWand?.ItemIndex ?? 0, ref branch, ref tier, ref behaviorKey) ||
                TryMatchLineageItem(itemIndex, BranchEmber, 5, WandMasteryKeys.Blood, Pandaros.Settlers.Items.LegendaryWandItems.CrimsonCovenantWand?.ItemIndex ?? 0, ref branch, ref tier, ref behaviorKey) ||
                TryMatchLineageItem(itemIndex, BranchEmber, 6, WandMasteryKeys.Aether, Pandaros.Settlers.Items.LegendaryWandItems.SeraphWand?.ItemIndex ?? 0, ref branch, ref tier, ref behaviorKey) ||
                TryMatchLineageItem(itemIndex, BranchVenom, 2, WandMasteryKeys.Venom, Pandaros.Settlers.Items.VenomWand.Item?.ItemIndex ?? 0, ref branch, ref tier, ref behaviorKey) ||
                TryMatchLineageItem(itemIndex, BranchVenom, 3, WandMasteryKeys.Briar, Pandaros.Settlers.Items.BriarWand.Item?.ItemIndex ?? 0, ref branch, ref tier, ref behaviorKey) ||
                TryMatchLineageItem(itemIndex, BranchVenom, 4, WandMasteryKeys.Venom, Pandaros.Settlers.Items.LegendaryWandItems.WidowrootWand?.ItemIndex ?? 0, ref branch, ref tier, ref behaviorKey) ||
                TryMatchLineageItem(itemIndex, BranchVenom, 5, WandMasteryKeys.Briar, Pandaros.Settlers.Items.LegendaryWandItems.ThornheartWand?.ItemIndex ?? 0, ref branch, ref tier, ref behaviorKey) ||
                TryMatchLineageItem(itemIndex, BranchVenom, 6, WandMasteryKeys.Void, Pandaros.Settlers.Items.LegendaryWandItems.EclipseWand?.ItemIndex ?? 0, ref branch, ref tier, ref behaviorKey) ||
                TryMatchLineageItem(itemIndex, BranchStone, 2, WandMasteryKeys.Stone, Pandaros.Settlers.Items.StoneWand.Item?.ItemIndex ?? 0, ref branch, ref tier, ref behaviorKey) ||
                TryMatchLineageItem(itemIndex, BranchStone, 3, WandMasteryKeys.Aether, Pandaros.Settlers.Items.MagicWand.Item?.ItemIndex ?? 0, ref branch, ref tier, ref behaviorKey) ||
                TryMatchLineageItem(itemIndex, BranchStone, 4, WandMasteryKeys.Stone, Pandaros.Settlers.Items.LegendaryWandItems.WorldbreakerWand?.ItemIndex ?? 0, ref branch, ref tier, ref behaviorKey) ||
                TryMatchLineageItem(itemIndex, BranchStone, 5, WandMasteryKeys.Spark, Pandaros.Settlers.Items.LegendaryWandItems.VoltspineWand?.ItemIndex ?? 0, ref branch, ref tier, ref behaviorKey) ||
                TryMatchLineageItem(itemIndex, BranchStone, 6, WandMasteryKeys.Mana, Pandaros.Settlers.Items.LegendaryWandItems.AstralManaWand?.ItemIndex ?? 0, ref branch, ref tier, ref behaviorKey);
        }

        private static bool TryMatchLineageItem(ushort itemIndex, string matchedBranch, int matchedTier, string matchedKey, ushort expectedItemIndex, ref string branch, ref int tier, ref string behaviorKey)
        {
            if (expectedItemIndex == 0 || itemIndex != expectedItemIndex)
                return false;

            branch = matchedBranch;
            tier = matchedTier;
            behaviorKey = matchedKey;
            return true;
        }

        private static string ResolveLineageBehaviorKey(string branch, int tier)
        {
            if (TryResolveLineageItemIndex(branch, tier, out var itemIndex) &&
                TryResolveLineageItemInfo(itemIndex, out _, out _, out var behaviorKey))
            {
                return behaviorKey;
            }

            return WandMasteryKeys.Mana;
        }

        private static string GetLineageDisplayName(string branch, int tier, string behaviorKey)
        {
            if (tier <= 1 || string.IsNullOrWhiteSpace(branch))
                return "Starter Wand";

            var branchDefinition = WandProgressionConfig.GetBranch(branch);
            var branchName = branchDefinition?.DisplayName ?? branch;
            if (tier >= 6)
                return branchDefinition?.T6Name ?? branchName + " Ascendant";

            return "Tier " + tier + " " + branchName + " Wand";
        }

        private static string GetNextLineageMilestoneText(PlayerMagicState state)
        {
            if (state == null || !state.WandLineageUnlocked)
                return "Craft the Starter Wand.";

            var settings = WandProgressionConfig.Current.Lineage;
            if (state.WandLineageChoicePending)
            {
                var pendingTier = ResolvePendingEvolutionTier(state);
                return pendingTier > 0
                    ? "Evolution Available: choose Tier " + pendingTier + " form in the popup."
                    : "Evolution choice is pending.";
            }

            if (string.IsNullOrWhiteSpace(state.WandLineageBranch))
                return "First evolution at " + state.WandLineageMasteryPoints + "/" + settings.FirstBranchPoints + " pts.";

            if (state.WandLineageTier < 3)
                return "Tier 3 at " + state.WandLineageMasteryPoints + "/" + settings.Tier3Points + " pts.";
            if (state.WandLineageTier < 4)
                return "Tier 4 at " + state.WandLineageMasteryPoints + "/" + settings.Tier4Points + " pts.";
            if (state.WandLineageTier < 5)
                return "Tier 5 at " + state.WandLineageMasteryPoints + "/" + settings.Tier5Points + " pts.";
            if (state.WandLineageTier < 6)
                return "Tier 6 requires the ritual awakening.";

            return "Awakened. Ritual sacrifices add +1% spell damage each. Current ritual power +" + Pandaros.Settlers.Items.RitualWandManager.GetDamageBonusPercent() + "%.";
        }

        private static bool TryNormalizeBranchKey(string branchKey, out string branch)
        {
            branch = NormalizeBranchOrNull(branchKey);
            return !string.IsNullOrEmpty(branch);
        }

        private static string NormalizeBranchOrNull(string branchKey)
        {
            if (string.IsNullOrWhiteSpace(branchKey))
                return null;

            switch (branchKey.Trim().ToLowerInvariant())
            {
                case "frost":
                case "ice":
                case "control":
                    return BranchFrost;
                case "ember":
                case "fire":
                case "flame":
                    return BranchEmber;
                case "venom":
                case "poison":
                case "plague":
                    return BranchVenom;
                case "stone":
                case "earth":
                case "rock":
                    return BranchStone;
                default:
                    return null;
            }
        }

        private static int GetHighestLegacyWandMasteryPoints(PlayerMagicState state)
        {
            var highest = 0;
            EnsureWandMasteryDefaults(state);
            EnsureLegendaryWandMasteryDefaults(state);
            for (var i = 0; i < WandMasteryOrder.Length; i++)
            {
                var key = WandMasteryOrder[i];
                state.WandMasteryUses.TryGetValue(key, out var baseUses);
                state.LegendaryWandMasteryUses.TryGetValue(key, out var legendaryUses);
                highest = System.Math.Max(highest, System.Math.Max(0, baseUses) + System.Math.Max(0, legendaryUses));
            }

            return highest;
        }

        private static void EnsureLineageMasteryMirrors(PlayerMagicState state)
        {
            if (state == null || !state.WandLineageUnlocked)
                return;

            EnsureWandMasteryDefaults(state);
            EnsureLegendaryWandDefaults(state);
            EnsureLegendaryWandMasteryDefaults(state);

            if (TryResolveLineageItemIndex(state.WandLineageBranch, System.Math.Max(1, state.WandLineageTier), out var itemIndex) &&
                TryResolveLineageItemInfo(itemIndex, out _, out _, out var behaviorKey))
            {
                state.WandMasteryUses[behaviorKey] = System.Math.Max(state.WandMasteryUses[behaviorKey], state.WandLineageMasteryPoints);
                if (state.WandLineageTier >= 4)
                {
                    state.LegendaryWandUnlocks[behaviorKey] = true;
                    state.LegendaryWandMasteryUses[behaviorKey] = System.Math.Max(state.LegendaryWandMasteryUses[behaviorKey], state.WandLineageBranchPoints);
                }
            }
        }

        private static bool IsMasteryKeyPartOfLineage(PlayerMagicState state, string masteryKey)
        {
            if (state == null || !state.WandLineageUnlocked)
                return false;

            var normalizedKey = NormalizeWandMasteryKey(masteryKey);
            if (string.IsNullOrEmpty(normalizedKey))
                return false;

            if (state.WandLineageTier <= 1 || string.IsNullOrWhiteSpace(state.WandLineageBranch))
                return normalizedKey == WandMasteryKeys.Mana;

            for (var tier = 2; tier <= System.Math.Max(2, state.WandLineageTier); tier++)
            {
                if (!TryResolveLineageItemIndex(state.WandLineageBranch, tier, out var itemIndex))
                    continue;

                if (TryResolveLineageItemInfo(itemIndex, out _, out _, out var behaviorKey) &&
                    string.Equals(normalizedKey, behaviorKey, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static void EnsureWandMasteryDefaults(PlayerMagicState state)
        {
            if (state == null)
                return;

            for (var i = 0; i < WandMasteryOrder.Length; i++)
            {
                var key = WandMasteryOrder[i];
                if (!state.WandMasteryUses.ContainsKey(key))
                    state.WandMasteryUses[key] = 0;
            }
        }

        private static void EnsureLegendaryWandDefaults(PlayerMagicState state)
        {
            if (state == null)
                return;

            for (var i = 0; i < WandMasteryOrder.Length; i++)
            {
                var key = WandMasteryOrder[i];
                if (!state.LegendaryWandUnlocks.ContainsKey(key))
                    state.LegendaryWandUnlocks[key] = false;
            }
        }

        private static void EnsureLegendaryWandMasteryDefaults(PlayerMagicState state)
        {
            if (state == null)
                return;

            for (var i = 0; i < WandMasteryOrder.Length; i++)
            {
                var key = WandMasteryOrder[i];
                if (!state.LegendaryWandMasteryUses.ContainsKey(key))
                    state.LegendaryWandMasteryUses[key] = 0;
            }
        }

        private static JObject SaveWandLineage(PlayerMagicState state)
        {
            return new JObject
            {
                ["unlocked"] = state.WandLineageUnlocked,
                ["branch"] = state.WandLineageBranch ?? string.Empty,
                ["tier"] = state.WandLineageTier,
                ["lineagePoints"] = state.WandLineageMasteryPoints,
                ["branchPoints"] = state.WandLineageBranchPoints,
                ["globalArcanePoints"] = state.GlobalArcanePoints,
                ["choicePending"] = state.WandLineageChoicePending,
                ["pendingTier"] = state.WandLineagePendingTier,
                ["ritualAwakened"] = state.WandLineageRitualAwakened,
                ["effects"] = SaveLineageEffects(state)
            };
        }

        private static void LoadWandLineage(PlayerMagicState state, JObject lineageJson)
        {
            if (state == null)
                return;

            state.WandLineageUnlocked = lineageJson?.Value<bool?>("unlocked") ?? false;
            state.WandLineageBranch = NormalizeBranchOrNull(lineageJson?.Value<string>("branch"));
            state.WandLineageTier = System.Math.Max(0, lineageJson?.Value<int?>("tier") ?? 0);
            state.WandLineageMasteryPoints = System.Math.Max(0, lineageJson?.Value<int?>("lineagePoints") ?? 0);
            state.WandLineageBranchPoints = System.Math.Max(0, lineageJson?.Value<int?>("branchPoints") ?? 0);
            state.GlobalArcanePoints = System.Math.Max(0, lineageJson?.Value<int?>("globalArcanePoints") ?? 0);
            state.WandLineageChoicePending = lineageJson?.Value<bool?>("choicePending") ?? false;
            state.WandLineagePendingTier = System.Math.Max(0, lineageJson?.Value<int?>("pendingTier") ?? 0);
            state.WandLineageRitualAwakened = lineageJson?.Value<bool?>("ritualAwakened") ?? false;
            LoadLineageEffects(state, lineageJson?["effects"] as JArray);

            if (state.WandLineageUnlocked && state.WandLineageTier <= 0)
                state.WandLineageTier = string.IsNullOrWhiteSpace(state.WandLineageBranch) ? 1 : GetLineageTierForPoints(state.WandLineageMasteryPoints);

            if (state.WandLineageUnlocked && state.WandLineageEffectKeys.Count == 0 && !string.IsNullOrWhiteSpace(state.WandLineageBranch))
                AddLineageEffectsForChoice(state, state.WandLineageBranch, state.WandLineageTier);

            if (state.WandLineageRitualAwakened || state.WandLineageTier >= 6)
                AddLineageEffect(state, WandLineageEffects.Ascended);

            if (state.WandLineageChoicePending && state.WandLineagePendingTier <= 0)
                state.WandLineagePendingTier = string.IsNullOrWhiteSpace(state.WandLineageBranch) ? 2 : System.Math.Min(5, System.Math.Max(2, state.WandLineageTier + 1));
        }

        private static JArray SaveLineageEffects(PlayerMagicState state)
        {
            var effects = new JArray();
            if (state == null)
                return effects;

            foreach (var effect in state.WandLineageEffectKeys)
            {
                var normalized = NormalizeLineageEffectKey(effect);
                if (!string.IsNullOrWhiteSpace(normalized))
                    effects.Add(normalized);
            }

            return effects;
        }

        private static void LoadLineageEffects(PlayerMagicState state, JArray effectsJson)
        {
            if (state == null)
                return;

            state.WandLineageEffectKeys.Clear();
            if (effectsJson == null)
                return;

            foreach (var token in effectsJson)
                AddLineageEffect(state, token?.Value<string>());
        }

        private static JObject SaveMeditation(PlayerMagicState state)
        {
            return new JObject
            {
                ["fullManaSeconds"] = state.FullManaMeditationSeconds,
                ["maxManaRewardsGranted"] = state.MeditationMaxManaRewardsGranted,
                ["regenRewardGranted"] = state.MeditationRegenRewardGranted
            };
        }

        private static void LoadMeditation(PlayerMagicState state, JObject meditationJson)
        {
            if (state == null)
                return;

            state.FullManaMeditationSeconds = System.Math.Max(0L, meditationJson?.Value<long?>("fullManaSeconds") ?? 0L);
            state.MeditationMaxManaRewardsGranted = System.Math.Max(0, meditationJson?.Value<int?>("maxManaRewardsGranted") ?? 0);
            state.MeditationRegenRewardGranted = meditationJson?.Value<bool?>("regenRewardGranted") ?? false;
            state.MeditationActive = false;
            state.MeditationStationarySinceAtMs = 0L;
            state.MeditationFullManaCarryMs = 0L;
            state.LastMeditationSampleAtMs = 0L;
            state.LastMeditationHudSignature = null;
        }

        private static void LoadWandMastery(PlayerMagicState state, JObject masteryJson)
        {
            state.WandMasteryUses.Clear();
            EnsureWandMasteryDefaults(state);

            if (masteryJson == null)
                return;

            foreach (var property in masteryJson.Properties())
            {
                var key = NormalizeWandMasteryKey(property.Name);
                if (string.IsNullOrEmpty(key))
                    continue;

                state.WandMasteryUses[key] = System.Math.Max(0, property.Value.Value<int?>() ?? 0);
            }

            EnsureWandMasteryDefaults(state);
        }

        private static void LoadLegendaryWands(PlayerMagicState state, JObject legendaryJson)
        {
            state.LegendaryWandUnlocks.Clear();
            EnsureLegendaryWandDefaults(state);

            if (legendaryJson == null)
                return;

            foreach (var property in legendaryJson.Properties())
            {
                var key = NormalizeWandMasteryKey(property.Name);
                if (string.IsNullOrEmpty(key))
                    continue;

                state.LegendaryWandUnlocks[key] = property.Value.Value<bool?>() ?? false;
            }

            EnsureLegendaryWandDefaults(state);
        }

        private static void LoadLegendaryWandMastery(PlayerMagicState state, JObject masteryJson)
        {
            state.LegendaryWandMasteryUses.Clear();
            EnsureLegendaryWandMasteryDefaults(state);

            if (masteryJson == null)
                return;

            foreach (var property in masteryJson.Properties())
            {
                var key = NormalizeWandMasteryKey(property.Name);
                if (string.IsNullOrEmpty(key))
                    continue;

                state.LegendaryWandMasteryUses[key] = System.Math.Max(0, property.Value.Value<int?>() ?? 0);
            }

            EnsureLegendaryWandMasteryDefaults(state);
        }

        private static JObject SaveWandMastery(PlayerMagicState state)
        {
            EnsureWandMasteryDefaults(state);
            var masteryJson = new JObject();

            for (var i = 0; i < WandMasteryOrder.Length; i++)
            {
                var key = WandMasteryOrder[i];
                masteryJson[key] = GetWandMasteryUses(state, key);
            }

            return masteryJson;
        }

        private static JObject SaveLegendaryWands(PlayerMagicState state)
        {
            EnsureLegendaryWandDefaults(state);
            var legendaryJson = new JObject();

            for (var i = 0; i < WandMasteryOrder.Length; i++)
            {
                var key = WandMasteryOrder[i];
                legendaryJson[key] = IsWandLegendaryEvolutionUnlocked(state, key);
            }

            return legendaryJson;
        }

        private static JObject SaveLegendaryWandMastery(PlayerMagicState state)
        {
            EnsureLegendaryWandMasteryDefaults(state);
            var masteryJson = new JObject();

            for (var i = 0; i < WandMasteryOrder.Length; i++)
            {
                var key = WandMasteryOrder[i];
                masteryJson[key] = GetLegendaryWandMasteryUses(state, key);
            }

            return masteryJson;
        }

        private static int GetWandMasteryUses(PlayerMagicState state, string masteryKey)
        {
            EnsureWandMasteryDefaults(state);
            if (IsMasteryKeyPartOfLineage(state, masteryKey))
                return System.Math.Max(0, state.WandLineageMasteryPoints);

            return state.WandMasteryUses.TryGetValue(NormalizeWandMasteryKey(masteryKey), out var uses)
                ? System.Math.Max(0, uses)
                : 0;
        }

        private static int GetLegendaryWandMasteryUses(PlayerMagicState state, string masteryKey)
        {
            EnsureLegendaryWandMasteryDefaults(state);
            if (state != null && state.WandLineageTier >= 4 && IsMasteryKeyPartOfLineage(state, masteryKey))
                return System.Math.Max(0, state.WandLineageBranchPoints);

            return state.LegendaryWandMasteryUses.TryGetValue(NormalizeWandMasteryKey(masteryKey), out var uses)
                ? System.Math.Max(0, uses)
                : 0;
        }

        private static bool IsWandLegendaryEvolutionUnlocked(PlayerMagicState state, string masteryKey)
        {
            EnsureLegendaryWandDefaults(state);
            if (state != null && state.WandLineageTier >= 4 && IsMasteryKeyPartOfLineage(state, masteryKey))
                return true;

            return state.LegendaryWandUnlocks.TryGetValue(NormalizeWandMasteryKey(masteryKey), out var unlocked) && unlocked;
        }

        private static int GetWandMasteryLevel(PlayerMagicState state, string masteryKey)
        {
            if (IsMasteryKeyPartOfLineage(state, masteryKey))
                return GetLineageMasteryLevel(state);

            return GetWandMasteryLevelFromUses(GetWandMasteryUses(state, masteryKey));
        }

        private static int GetLegendaryWandMasteryLevel(PlayerMagicState state, string masteryKey)
        {
            if (state != null && state.WandLineageTier >= 4 && IsMasteryKeyPartOfLineage(state, masteryKey))
                return GetWandMasteryLevelFromUses(System.Math.Max(0, state.WandLineageBranchPoints));

            return GetWandMasteryLevelFromUses(GetLegendaryWandMasteryUses(state, masteryKey));
        }

        private static int GetWandMasteryUsesForCurrentLevel(int uses, int level)
        {
            var settings = WandProgressionConfig.Current.Lineage;
            return level >= settings.MaxMasteryLevel
                ? 0
                : System.Math.Max(0, uses - level * settings.MasteryPointsPerLevel);
        }

        private static int GetWandMasteryUsesRequiredForNextLevel(int level)
        {
            var settings = WandProgressionConfig.Current.Lineage;
            return level >= settings.MaxMasteryLevel ? 0 : settings.MasteryPointsPerLevel;
        }

        private static int GetWandMasteryLevelFromUses(int uses)
        {
            var settings = WandProgressionConfig.Current.Lineage;
            return System.Math.Max(0, System.Math.Min(settings.MaxMasteryLevel, uses / settings.MasteryPointsPerLevel));
        }

        private static int GetEffectiveWandDamageMasteryLevel(Players.Player player, string masteryKey)
        {
            var state = GetState(player);
            var normalizedKey = NormalizeWandMasteryKey(masteryKey);
            var level = GetWandMasteryLevel(state, normalizedKey);
            if (IsWandLegendaryEvolutionUnlocked(state, normalizedKey))
                level += GetLegendaryWandMasteryLevel(state, normalizedKey);

            var maxLevel = WandProgressionConfig.Current.Lineage.MaxMasteryLevel;
            return System.Math.Max(0, System.Math.Min(maxLevel * 2, level));
        }

        private static bool TryAutoEvolveWand(Players.Player player, PlayerMagicState state, string masteryKey, ushort baseItemIndex)
        {
            if (player == null || state == null)
                return false;

            var normalizedKey = NormalizeWandMasteryKey(masteryKey);
            if (string.IsNullOrEmpty(normalizedKey) || IsWandLegendaryEvolutionUnlocked(state, normalizedKey))
                return false;

            if (!Pandaros.Settlers.Items.LegendaryWandItems.TryGetItemIndex(normalizedKey, out var evolvedItemIndex) || evolvedItemIndex == 0)
            {
                SendEquipChat(player, GetWandMasteryDisplayName(normalizedKey) + " reached Mastery 10, but the evolved item is not registered yet.");
                return false;
            }

            var resolvedBaseItemIndex = ResolveBaseWandItemIndex(normalizedKey, baseItemIndex);
            var replacedBaseItem = resolvedBaseItemIndex != 0 &&
                TryReplaceInventoryItem(player.Inventory, resolvedBaseItemIndex, evolvedItemIndex);

            if (!replacedBaseItem && !TryAddInventoryItem(player, evolvedItemIndex, 1))
            {
                SendEquipChat(player, GetWandMasteryDisplayName(normalizedKey) + " reached Mastery 10, but the evolved wand could not be added to your inventory. Free a slot and cast once more.");
                return false;
            }

            state.LegendaryWandUnlocks[normalizedKey] = true;
            state.LegendaryWandMasteryUses[normalizedKey] = GetLegendaryWandMasteryUses(state, normalizedKey);
            CacheObservedWand(state, normalizedKey, LegendaryWandEvolutionManager.GetLegendaryName(normalizedKey));
            ShowItemIndicator(player, evolvedItemIndex, 1.2f, 0L);
            SendEquipChat(player, GetWandMasteryDisplayName(normalizedKey) + " evolved for free into " + LegendaryWandEvolutionManager.GetLegendaryName(normalizedKey) + ". Evolution Mastery is now unlocked.");
            return true;
        }

        private static bool TryRepairEvolvedWandItem(Players.Player player, string masteryKey, ushort currentItemIndex)
        {
            if (player?.Inventory == null)
                return false;

            if (!Pandaros.Settlers.Items.LegendaryWandItems.TryGetItemIndex(masteryKey, out var evolvedItemIndex) || evolvedItemIndex == 0)
                return false;

            if (currentItemIndex == evolvedItemIndex ||
                Pandaros.Settlers.Items.LegendaryWandItems.TryGetMasteryKeyByItemIndex(currentItemIndex, out _))
            {
                return false;
            }

            var baseItemIndex = ResolveBaseWandItemIndex(masteryKey, currentItemIndex);
            if (baseItemIndex == 0 || !TryReplaceInventoryItem(player.Inventory, baseItemIndex, evolvedItemIndex))
                return false;

            ShowItemIndicator(player, evolvedItemIndex, 0.8f, 0L);
            SendEquipChat(player, GetWandMasteryDisplayName(masteryKey) + " was upgraded to " + LegendaryWandEvolutionManager.GetLegendaryName(masteryKey) + ".");
            return true;
        }

        private static void RepairUnlockedWandItems(Players.Player player, PlayerMagicState state)
        {
            if (player?.Inventory == null || state == null)
                return;

            if (state.WandLineageUnlocked)
                return;

            EnsureLegendaryWandDefaults(state);
            for (var i = 0; i < WandMasteryOrder.Length; i++)
            {
                var key = WandMasteryOrder[i];
                if (!IsWandLegendaryEvolutionUnlocked(state, key))
                    continue;

                var baseItemIndex = ResolveBaseWandItemIndex(key, 0);
                if (baseItemIndex == 0)
                    continue;

                TryRepairEvolvedWandItem(player, key, baseItemIndex);
            }
        }

        private static ushort ResolveBaseWandItemIndex(string masteryKey, ushort preferredItemIndex)
        {
            if (preferredItemIndex != 0 &&
                !Pandaros.Settlers.Items.LegendaryWandItems.TryGetMasteryKeyByItemIndex(preferredItemIndex, out _))
            {
                return preferredItemIndex;
            }

            switch (NormalizeWandMasteryKey(masteryKey))
            {
                case WandMasteryKeys.Mana:
                    return Pandaros.Settlers.Items.ManaWand.Item?.ItemIndex ?? 0;
                case WandMasteryKeys.Briar:
                    return Pandaros.Settlers.Items.BriarWand.Item?.ItemIndex ?? 0;
                case WandMasteryKeys.Spark:
                    return Pandaros.Settlers.Items.SparkWand.Item?.ItemIndex ?? 0;
                case WandMasteryKeys.Venom:
                    return Pandaros.Settlers.Items.VenomWand.Item?.ItemIndex ?? 0;
                case WandMasteryKeys.Ember:
                    return Pandaros.Settlers.Items.EmberWand.Item?.ItemIndex ?? 0;
                case WandMasteryKeys.Frost:
                    return Pandaros.Settlers.Items.FrostWand.Item?.ItemIndex ?? 0;
                case WandMasteryKeys.Crystal:
                    return Pandaros.Settlers.Items.CrystalWand.Item?.ItemIndex ?? 0;
                case WandMasteryKeys.Stone:
                    return Pandaros.Settlers.Items.StoneWand.Item?.ItemIndex ?? 0;
                case WandMasteryKeys.Storm:
                    return Pandaros.Settlers.Items.StormWand.Item?.ItemIndex ?? 0;
                case WandMasteryKeys.Aether:
                    return Pandaros.Settlers.Items.MagicWand.Item?.ItemIndex ?? 0;
                case WandMasteryKeys.Blood:
                    return Pandaros.Settlers.Items.BloodWand.Item?.ItemIndex ?? 0;
                case WandMasteryKeys.Void:
                    return Pandaros.Settlers.Items.VoidWand.Item?.ItemIndex ?? 0;
                default:
                    return 0;
            }
        }

        private static bool TryAddInventoryItem(Players.Player player, ushort itemIndex, int amount)
        {
            var inventory = player?.Inventory;
            if (inventory == null || itemIndex == 0 || amount <= 0)
                return false;

            return TryInvokeInventoryAdd(inventory, "TryAdd", itemIndex, amount, -1, true) ||
                TryInvokeInventoryAdd(inventory, "Add", itemIndex, amount, -1, true) ||
                TryInvokeInventoryAdd(inventory, "TryAdd", itemIndex, amount) ||
                TryInvokeInventoryAdd(inventory, "Add", itemIndex, amount) ||
                TryInvokeInventoryAdd(inventory, "TryAdd", ItemTypes.GetType(itemIndex), amount) ||
                TryInvokeInventoryAdd(inventory, "Add", ItemTypes.GetType(itemIndex), amount) ||
                TryFallbackInsertInventoryItem(inventory, itemIndex, amount);
        }

        private static bool TryReplaceInventoryItem(object inventory, ushort oldItemIndex, ushort newItemIndex)
        {
            if (inventory == null || oldItemIndex == 0 || newItemIndex == 0 || oldItemIndex == newItemIndex)
                return false;

            var itemsProperty = inventory.GetType().GetProperty("Items", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var items = itemsProperty?.GetValue(inventory) as Array;
            if (items == null)
                return false;

            for (var i = 0; i < items.Length; i++)
            {
                var entry = items.GetValue(i);
                if (entry == null)
                    continue;

                var entryType = entry.GetType();
                var typeMember = entryType.GetField("Type") ?? (MemberInfo)entryType.GetProperty("Type");
                var amountMember = entryType.GetField("Amount") ?? (MemberInfo)entryType.GetProperty("Amount");
                if (typeMember == null || amountMember == null)
                    continue;

                var existingType = Convert.ToUInt16(ReadInventoryMemberValue(typeMember, entry));
                var existingAmount = Convert.ToInt32(ReadInventoryMemberValue(amountMember, entry));
                if (existingType != oldItemIndex || existingAmount <= 0)
                    continue;

                if (TrySetInventorySlot(inventory, i, newItemIndex, 1))
                    return true;

                WriteInventoryMemberValue(typeMember, ref entry, newItemIndex);
                if (existingAmount > 1)
                    WriteInventoryMemberValue(amountMember, ref entry, 1);

                items.SetValue(entry, i);
                TryNotifyInventoryChanged(inventory);
                return true;
            }

            return false;
        }

        private static bool TryInvokeInventoryAdd(object inventory, string methodName, object itemArg, int amount, int index = int.MinValue, bool sendUpdate = true)
        {
            foreach (var method in inventory.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (!string.Equals(method.Name, methodName, StringComparison.Ordinal))
                    continue;

                var parameters = method.GetParameters();
                if (parameters.Length != 2 && parameters.Length != 4)
                    continue;

                try
                {
                    object[] args;
                    if (parameters.Length == 4)
                    {
                        var addIndex = index == int.MinValue ? -1 : index;
                        args = new[] { itemArg, (object)amount, addIndex, sendUpdate };
                    }
                    else
                    {
                        args = new[] { itemArg, (object)amount };
                    }

                    var result = method.Invoke(inventory, args);
                    if (result is bool success)
                        return success;

                    return true;
                }
                catch
                {
                }
            }

            return false;
        }

        private static bool TrySetInventorySlot(object inventory, int index, ushort itemIndex, int amount)
        {
            if (inventory == null || index < 0 || itemIndex == 0 || amount <= 0)
                return false;

            foreach (var method in inventory.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (!string.Equals(method.Name, "SetAt", StringComparison.Ordinal))
                    continue;

                var parameters = method.GetParameters();
                if (parameters.Length != 2)
                    continue;

                try
                {
                    method.Invoke(inventory, new object[] { index, new global::InventoryItem(itemIndex, amount) });
                    TryNotifyInventoryChanged(inventory);
                    return true;
                }
                catch
                {
                }
            }

            return false;
        }

        private static bool TryFallbackInsertInventoryItem(object inventory, ushort itemIndex, int amount)
        {
            var itemsProperty = inventory.GetType().GetProperty("Items", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var items = itemsProperty?.GetValue(inventory) as Array;
            if (items == null)
                return false;

            var freeSlot = -1;
            for (var i = 0; i < items.Length; i++)
            {
                var entry = items.GetValue(i);
                if (entry == null)
                    continue;

                var entryType = entry.GetType();
                var typeMember = entryType.GetField("Type") ?? (MemberInfo)entryType.GetProperty("Type");
                var amountMember = entryType.GetField("Amount") ?? (MemberInfo)entryType.GetProperty("Amount");
                if (typeMember == null || amountMember == null)
                    continue;

                var existingType = Convert.ToUInt16(ReadInventoryMemberValue(typeMember, entry));
                var existingAmount = Convert.ToInt32(ReadInventoryMemberValue(amountMember, entry));

                if (existingType == itemIndex && existingAmount > 0)
                {
                    WriteInventoryMemberValue(amountMember, ref entry, existingAmount + amount);
                    items.SetValue(entry, i);
                    TryNotifyInventoryChanged(inventory);
                    return true;
                }

                if (freeSlot == -1 && existingAmount <= 0)
                    freeSlot = i;
            }

            if (freeSlot == -1)
                return false;

            var freeEntry = items.GetValue(freeSlot);
            if (freeEntry == null)
                return false;

            var freeType = freeEntry.GetType();
            var freeTypeMember = freeType.GetField("Type") ?? (MemberInfo)freeType.GetProperty("Type");
            var freeAmountMember = freeType.GetField("Amount") ?? (MemberInfo)freeType.GetProperty("Amount");
            if (freeTypeMember == null || freeAmountMember == null)
                return false;

            if (TrySetInventorySlot(inventory, freeSlot, itemIndex, amount))
                return true;

            WriteInventoryMemberValue(freeTypeMember, ref freeEntry, itemIndex);
            WriteInventoryMemberValue(freeAmountMember, ref freeEntry, amount);
            items.SetValue(freeEntry, freeSlot);
            TryNotifyInventoryChanged(inventory);
            return true;
        }

        private static void TryNotifyInventoryChanged(object inventory)
        {
            if (inventory == null)
                return;

            var invoked = false;
            foreach (var method in inventory.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (!IsInventoryNotifyMethod(method.Name) || method.GetParameters().Length != 0)
                    continue;

                try
                {
                    method.Invoke(inventory, null);
                    invoked = true;
                }
                catch
                {
                }
            }

            if (invoked)
                return;
        }

        private static void TrySetSelectedTypeOnObject(object source, ushort itemIndex)
        {
            if (source == null || itemIndex == 0)
                return;

            var type = source.GetType();
            for (var i = 0; i < SelectedTypeMemberNames.Length; i++)
            {
                var memberName = SelectedTypeMemberNames[i];
                var property = type.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (property != null && property.CanWrite && TryConvertToUShort(property.GetValue(source, null), out var propertyValue) &&
                    (propertyValue == 0 || IsLineageWandItem(propertyValue)))
                {
                    try
                    {
                        property.SetValue(source, Convert.ChangeType(itemIndex, property.PropertyType), null);
                    }
                    catch
                    {
                    }
                }

                var field = type.GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null && TryConvertToUShort(field.GetValue(source), out var fieldValue) &&
                    (fieldValue == 0 || IsLineageWandItem(fieldValue)))
                {
                    try
                    {
                        field.SetValue(source, Convert.ChangeType(itemIndex, field.FieldType));
                    }
                    catch
                    {
                    }
                }
            }
        }

        private static bool IsInventoryNotifyMethod(string methodName)
        {
            return string.Equals(methodName, "SendUpdate", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(methodName, "SendInventory", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(methodName, "SendToPlayer", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(methodName, "Send", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(methodName, "SetDirty", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(methodName, "MarkDirty", StringComparison.OrdinalIgnoreCase);
        }

        private static object ReadInventoryMemberValue(MemberInfo member, object target)
        {
            if (member is FieldInfo field)
                return field.GetValue(target);

            return ((PropertyInfo)member).GetValue(target);
        }

        private static void WriteInventoryMemberValue(MemberInfo member, ref object target, object value)
        {
            if (member is FieldInfo field)
            {
                field.SetValue(target, value);
                return;
            }

            ((PropertyInfo)member).SetValue(target, value);
        }

        private static float GetWandMasteryDamageMultiplierForLevel(string masteryKey, int level)
        {
            var settings = WandProgressionConfig.Current.Lineage;
            return 1f + System.Math.Max(0, System.Math.Min(settings.MaxMasteryLevel * 2, level)) * settings.DamageBonusPerMasteryLevel;
        }

        private static float GetWandMasteryDotMultiplierForLevel(string masteryKey, int level)
        {
            return GetWandMasteryDamageMultiplierForLevel(masteryKey, level);
        }

        private static float GetWandMasteryHealMultiplierForLevel(string masteryKey, int level)
        {
            switch (NormalizeWandMasteryKey(masteryKey))
            {
                case WandMasteryKeys.Aether:
                    return level == 1 ? 1.10f : level == 2 ? 1.20f : level >= 3 ? 1.35f : 1f;
                case WandMasteryKeys.Blood:
                    return level == 1 ? 1.08f : level == 2 ? 1.16f : level >= 3 ? 1.25f : 1f;
                default:
                    return 1f;
            }
        }

        private static float GetWandMasteryForceMultiplierForLevel(string masteryKey, int level)
        {
            switch (NormalizeWandMasteryKey(masteryKey))
            {
                case WandMasteryKeys.Frost:
                    return level == 1 ? 1.10f : level == 2 ? 1.20f : level >= 3 ? 1.35f : 1f;
                case WandMasteryKeys.Stone:
                    return level == 1 ? 1.12f : level == 2 ? 1.25f : level >= 3 ? 1.40f : 1f;
                default:
                    return 1f;
            }
        }

        private static int GetWandMasteryCooldownReductionMsForLevel(string masteryKey, int level)
        {
            switch (NormalizeWandMasteryKey(masteryKey))
            {
                case WandMasteryKeys.Spark:
                    return level == 1 ? 30 : level == 2 ? 60 : level >= 3 ? 90 : 0;
                case WandMasteryKeys.Frost:
                    return level == 1 ? 30 : level == 2 ? 60 : level >= 3 ? 100 : 0;
                default:
                    return 0;
            }
        }

        private static int GetWandMasteryManaDiscountForLevel(string masteryKey, int level)
        {
            switch (NormalizeWandMasteryKey(masteryKey))
            {
                case WandMasteryKeys.Aether:
                    return level >= 2 ? 1 : 0;
                default:
                    return 0;
            }
        }

        private static int GetWandMasteryExtraTargetsForLevel(string masteryKey, int level)
        {
            switch (NormalizeWandMasteryKey(masteryKey))
            {
                case WandMasteryKeys.Mana:
                    return level >= 3 ? 1 : 0;
                case WandMasteryKeys.Spark:
                    return level == 1 ? 1 : level == 2 ? 1 : level >= 3 ? 2 : 0;
                case WandMasteryKeys.Storm:
                    return level == 1 ? 1 : level == 2 ? 2 : level >= 3 ? 3 : 0;
                case WandMasteryKeys.Aether:
                    return level == 1 ? 0 : level == 2 ? 1 : level >= 3 ? 2 : 0;
                default:
                    return 0;
            }
        }

        private static float GetWandMasteryRadiusBonusForLevel(string masteryKey, int level)
        {
            switch (NormalizeWandMasteryKey(masteryKey))
            {
                case WandMasteryKeys.Briar:
                    return level == 1 ? 0.25f : level == 2 ? 0.50f : level >= 3 ? 0.85f : 0f;
                case WandMasteryKeys.Venom:
                case WandMasteryKeys.Ember:
                    return level == 1 ? 0.35f : level == 2 ? 0.70f : level >= 3 ? 1.10f : 0f;
                case WandMasteryKeys.Storm:
                case WandMasteryKeys.Void:
                    return level == 1 ? 0.40f : level == 2 ? 0.80f : level >= 3 ? 1.20f : 0f;
                default:
                    return 0f;
            }
        }

        private static float GetWandMasteryRangeBonusForLevel(string masteryKey, int level)
        {
            switch (NormalizeWandMasteryKey(masteryKey))
            {
                case WandMasteryKeys.Crystal:
                    return level == 1 ? 1.5f : level == 2 ? 3f : level >= 3 ? 4.5f : 0f;
                default:
                    return 0f;
            }
        }

        private static float GetLegendaryDamageMultiplier(string masteryKey)
        {
            switch (NormalizeWandMasteryKey(masteryKey))
            {
                case WandMasteryKeys.Mana:
                    return 1.12f;
                case WandMasteryKeys.Spark:
                    return 1.08f;
                case WandMasteryKeys.Ember:
                    return 1.08f;
                case WandMasteryKeys.Crystal:
                    return 1.10f;
                case WandMasteryKeys.Stone:
                    return 1.12f;
                case WandMasteryKeys.Storm:
                    return 1.10f;
                case WandMasteryKeys.Blood:
                    return 1.08f;
                case WandMasteryKeys.Void:
                    return 1.10f;
                default:
                    return 1f;
            }
        }

        private static float GetLegendaryDotMultiplier(string masteryKey)
        {
            switch (NormalizeWandMasteryKey(masteryKey))
            {
                case WandMasteryKeys.Briar:
                    return 1.25f;
                case WandMasteryKeys.Venom:
                    return 1.22f;
                case WandMasteryKeys.Ember:
                    return 1.24f;
                case WandMasteryKeys.Blood:
                    return 1.18f;
                case WandMasteryKeys.Void:
                    return 1.16f;
                default:
                    return 1f;
            }
        }

        private static float GetLegendaryHealMultiplier(string masteryKey)
        {
            switch (NormalizeWandMasteryKey(masteryKey))
            {
                case WandMasteryKeys.Aether:
                    return 1.18f;
                case WandMasteryKeys.Blood:
                    return 1.15f;
                default:
                    return 1f;
            }
        }

        private static float GetLegendaryForceMultiplier(string masteryKey)
        {
            switch (NormalizeWandMasteryKey(masteryKey))
            {
                case WandMasteryKeys.Frost:
                    return 1.20f;
                case WandMasteryKeys.Stone:
                    return 1.20f;
                default:
                    return 1f;
            }
        }

        private static int GetLegendaryCooldownReductionMs(string masteryKey)
        {
            switch (NormalizeWandMasteryKey(masteryKey))
            {
                case WandMasteryKeys.Spark:
                case WandMasteryKeys.Frost:
                    return 60;
                default:
                    return 0;
            }
        }

        private static int GetLegendaryManaDiscount(string masteryKey)
        {
            switch (NormalizeWandMasteryKey(masteryKey))
            {
                case WandMasteryKeys.Mana:
                case WandMasteryKeys.Aether:
                    return 1;
                default:
                    return 0;
            }
        }

        private static int GetLegendaryExtraTargets(string masteryKey)
        {
            switch (NormalizeWandMasteryKey(masteryKey))
            {
                case WandMasteryKeys.Mana:
                case WandMasteryKeys.Spark:
                case WandMasteryKeys.Storm:
                case WandMasteryKeys.Aether:
                    return 1;
                default:
                    return 0;
            }
        }

        private static float GetLegendaryRadiusBonus(string masteryKey)
        {
            switch (NormalizeWandMasteryKey(masteryKey))
            {
                case WandMasteryKeys.Briar:
                    return 1.2f;
                case WandMasteryKeys.Venom:
                    return 1.2f;
                case WandMasteryKeys.Ember:
                    return 1.2f;
                case WandMasteryKeys.Frost:
                    return 0.9f;
                case WandMasteryKeys.Crystal:
                    return 0.5f;
                case WandMasteryKeys.Stone:
                    return 0.7f;
                case WandMasteryKeys.Storm:
                    return 1.3f;
                case WandMasteryKeys.Void:
                    return 1.4f;
                default:
                    return 0f;
            }
        }

        private static float GetLegendaryRangeBonus(string masteryKey)
        {
            switch (NormalizeWandMasteryKey(masteryKey))
            {
                case WandMasteryKeys.Crystal:
                    return 4f;
                default:
                    return 0f;
            }
        }

        private static int GetManaRegenIntervalMs(PlayerMagicState state)
        {
            var crystalReduction = System.Math.Max(0, state.ManaCrystalLevel) * ManaCrystalRegenReductionMs;
            var armorReduction = GetSkilledArmorPieceCount(state) >= 4 ? 250 : 0;
            var eventReduction = BetterNecromancy.WorldEventManager.GetManaRegenIntervalReductionMsBonus();
            return System.Math.Max(MinimumManaRegenIntervalMs, ManaRegenIntervalMs - crystalReduction - armorReduction - eventReduction);
        }

        private static int GetSkilledArmorPieceCount(PlayerMagicState state)
        {
            var count = 0;

            if (state.SkilledHelmTier > 0)
                count++;
            if (state.SkilledChestTier > 0)
                count++;
            if (state.SkilledGlovesTier > 0)
                count++;
            if (state.SkilledLegsTier > 0)
                count++;
            if (state.SkilledBootsTier > 0)
                count++;
            if (state.SkilledShieldTier > 0)
                count++;

            return count;
        }

        private static string BuildManaHudLayerKey(string baseKey, int layer)
        {
            return baseKey + "." + layer;
        }

        private static int GetFlightMasteryLevel(PlayerMagicState state)
        {
            var seconds = state?.FlightMasterySeconds ?? 0L;
            if (seconds >= FlightMasteryLevel3RequirementSeconds)
                return 3;
            if (seconds >= FlightMasteryLevel2RequirementSeconds)
                return 2;
            if (seconds >= FlightMasteryLevel1RequirementSeconds)
                return 1;
            return 0;
        }

        private static string BuildFlightMasteryLevelLabel(int level)
        {
            switch (level)
            {
                case 1: return "Flight Mastery I";
                case 2: return "Flight Mastery II";
                case 3: return "Flight Mastery III";
                default: return "Flight Untrained";
            }
        }

        private static string GetFlightMasteryProgressLabel(PlayerMagicState state)
        {
            var seconds = System.Math.Max(0L, state?.FlightMasterySeconds ?? 0L);
            var level = GetFlightMasteryLevel(state);
            switch (level)
            {
                case 0:
                    return seconds + "/" + FlightMasteryLevel1RequirementSeconds + "s";
                case 1:
                    return seconds + "/" + FlightMasteryLevel2RequirementSeconds + "s";
                case 2:
                    return seconds + "/" + FlightMasteryLevel3RequirementSeconds + "s";
                default:
                    return seconds + "s total";
            }
        }

        private static string GetFlightMasteryCompactSummary(PlayerMagicState state)
        {
            switch (GetFlightMasteryLevel(state))
            {
                case 1:
                    return "Heavy mana draw remains. Unstable takeoff costs 10 mana, flight costs 2 MP per second, double-cost spikes still happen often, and boost-speed aborts the flight, but spontaneous shutdowns are gone.";
                case 2:
                    return "Stable flight. Costs 1 MP per second with no random shutdowns or extra mana spikes, but very fast flight still burns extra mana.";
                case 3:
                    return "Ascendant flight. Stable, 50% cheaper mana drain, but pushing to extreme flight speed still burns extra mana.";
                default:
                    return "Wild flight. Unstable takeoff costs 10 mana, flight costs 2 MP per second, double-cost spikes happen often, boost-speed aborts the flight, and rare shutdowns can happen.";
            }
        }

        private static string GetFlightManaCostDisplay(PlayerMagicState state)
        {
            switch (GetFlightMasteryLevel(state))
            {
                case 3:
                    return "-0.5 MP per sec";
                case 2:
                    return "-1 MP per sec";
                default:
                    return "-2 MP per sec";
            }
        }

        private static float GetFlightManaCostPerSecond(PlayerMagicState state)
        {
            switch (GetFlightMasteryLevel(state))
            {
                case 3:
                    return ManaFlightMasteryThreeCostPerSecond;
                case 2:
                    return ManaFlightStableCostPerSecond;
                default:
                    return ManaFlightUnstableCostPerSecond;
            }
        }

        private static void EnsureManaDefaults(PlayerMagicState state)
        {
            if (state.ManaCrystalLevel < 0)
                state.ManaCrystalLevel = 0;

            if (state.MaxMana < DefaultMaxMana)
                state.MaxMana = DefaultMaxMana;

            if (state.CurrentMana < 0)
                state.CurrentMana = 0;

            if (state.CurrentMana > state.MaxMana)
                state.CurrentMana = state.MaxMana;
        }

        private static void DisableManaFlight(Players.Player player, PlayerMagicState state, string message)
        {
            state.ManaFlightActive = false;
            state.ManaFlightPermissionGranted = false;
            state.NextManaFlightDrainAtMs = 0L;
            state.ManaFlightUnsupportedSinceAtMs = 0L;
            state.ManaFlightCostCarry = 0f;
            state.PendingManaFlightCrashUntilMs = 0L;
            state.ManaFlightAirborneSinceAtMs = 0L;
            state.ManaFlightGroundedSinceAtMs = 0L;
            state.ManaFlightTakeoffY = 0f;
            state.LastManaFlightHorizontalSpeed = 0f;
            state.ManaFlightSampleInitialized = false;
            state.LastManaFlightSamplePosition = Vector3.zero;
            state.ManaFlightTakeoffPosition = Vector3.zero;
            state.LastManaFlightSampleAtMs = 0L;

            if (player != null && player.HasFlightMode)
                player.SetFlightMode(false);

            if (!string.IsNullOrEmpty(message) && player != null)
                SendEquipChat(player, message);
        }

        private static void BeginManaFlightCrash(Players.Player player, PlayerMagicState state, string message)
        {
            state.ManaFlightActive = false;
            state.ManaFlightPermissionGranted = false;
            state.NextManaFlightDrainAtMs = 0L;
            state.ManaFlightUnsupportedSinceAtMs = 0L;
            state.ManaFlightCostCarry = 0f;
            state.PendingManaFlightCrashUntilMs = Pipliz.Time.MillisecondsSinceStart + ManaFlightCrashWindowMs;
            state.ManaFlightAirborneSinceAtMs = 0L;
            state.ManaFlightGroundedSinceAtMs = 0L;
            state.ManaFlightTakeoffY = 0f;
            state.LastManaFlightHorizontalSpeed = 0f;
            state.ManaFlightSampleInitialized = false;
            state.LastManaFlightSamplePosition = Vector3.zero;
            state.ManaFlightTakeoffPosition = Vector3.zero;
            state.LastManaFlightSampleAtMs = 0L;

            if (player != null && player.HasFlightMode)
                player.SetFlightMode(false);

            if (!string.IsNullOrEmpty(message) && player != null)
                SendEquipChat(player, message);
        }

        private static void RegisterFlightMasterySecond(Players.Player player, PlayerMagicState state)
        {
            var previousLevel = GetFlightMasteryLevel(state);
            state.FlightMasterySeconds += 1L;
            var newLevel = GetFlightMasteryLevel(state);
            if (newLevel <= previousLevel)
                return;

            SendEquipChat(player, BuildFlightMasteryLevelLabel(newLevel) + " reached. " + GetFlightMasteryCompactSummary(state));
        }

        private static float GetApproximateGroundY(Players.Player player)
        {
            if (player == null)
                return 0f;

            var standingY = player.PositionStanding.y;
            var cameraY = player.PositionCamera.y;
            var eyeHeight = Mathf.Clamp(cameraY - standingY, 0.4f, 2.1f);
            var estimatedGroundFromCamera = cameraY - Mathf.Max(1.55f, eyeHeight * 0.95f);
            return Mathf.Min(standingY, estimatedGroundFromCamera);
        }

        private static bool HasSolidSupportBelow(Players.Player player, int maxDepth)
        {
            if (player == null || maxDepth <= 0 || ItemTypes.Solids == null)
                return false;

            var standingPosition = player.PositionStanding;
            var footOffsets = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(0.24f, 0f),
                new Vector2(-0.24f, 0f),
                new Vector2(0f, 0.24f),
                new Vector2(0f, -0.24f),
                new Vector2(0.24f, 0.24f),
                new Vector2(0.24f, -0.24f),
                new Vector2(-0.24f, 0.24f),
                new Vector2(-0.24f, -0.24f),
                new Vector2(0.38f, 0f),
                new Vector2(-0.38f, 0f),
                new Vector2(0f, 0.38f),
                new Vector2(0f, -0.38f)
            };

            for (var depth = 1; depth <= maxDepth; depth++)
            {
                for (var i = 0; i < footOffsets.Length; i++)
                {
                    var offset = footOffsets[i];
                    var checkPosition = new Pipliz.Vector3Int(
                        Mathf.FloorToInt(standingPosition.x + offset.x),
                        Mathf.FloorToInt(standingPosition.y) - depth,
                        Mathf.FloorToInt(standingPosition.z + offset.y));

                    if (!World.TryGetTypeAt(checkPosition, out ushort placedType))
                        continue;

                    if (placedType < ItemTypes.Solids.Length && ItemTypes.Solids[placedType])
                        return true;
                }
            }

            return false;
        }

        private static void ValidateSkilledArmor(Players.Player player, PlayerMagicState state)
        {
            ValidateSkilledArmorSlot(player, state, MagicArmorSlot.Helm);
            ValidateSkilledArmorSlot(player, state, MagicArmorSlot.Chest);
            ValidateSkilledArmorSlot(player, state, MagicArmorSlot.Gloves);
            ValidateSkilledArmorSlot(player, state, MagicArmorSlot.Legs);
            ValidateSkilledArmorSlot(player, state, MagicArmorSlot.Boots);
            ValidateSkilledArmorSlot(player, state, MagicArmorSlot.Shield);
        }

        private static void ValidateSkilledArmorSlot(Players.Player player, PlayerMagicState state, MagicArmorSlot slot)
        {
            var tier = GetSkilledArmorTier(state, slot);

            if (tier == 0)
                return;

            if (PlayerHasItem(player, GetSkilledArmorItemIndex(slot, tier)))
                return;

            var removedItemIndex = GetSkilledArmorItemIndex(slot, tier);
            SetSkilledArmorTier(state, slot, 0);
            ShowMissingItemIndicator(player, removedItemIndex, 0L);
        }

        private static void SendEquipChat(Players.Player player, string message)
        {
            PlayerToastManager.Show(player, message, "#f1e7c8", 4200L, -118, 980f, 16f);
        }

        private static void ClearEquippedArmorPiece(PlayerMagicState state, MagicArmorSlot slot, List<string> removed)
        {
            var tier = GetSkilledArmorTier(state, slot);
            if (tier <= 0)
                return;

            removed.Add(GetArmorDisplayName(slot, tier));
            SetSkilledArmorTier(state, slot, 0);
        }

        private static int GetSkilledSwordFlag(int tier)
        {
            switch (tier)
            {
                case 1:
                    return SkilledSwordTier1Flag;
                case 2:
                    return SkilledSwordTier2Flag;
                case 3:
                    return SkilledSwordTier3Flag;
                default:
                    return 0;
            }
        }

        private static int GetLegacySkilledSwordMask(int legacyTier)
        {
            switch (legacyTier)
            {
                case 1:
                    return SkilledSwordTier1Flag;
                case 2:
                    return SkilledSwordTier2Flag;
                case 3:
                    return SkilledSwordTier3Flag;
                default:
                    return 0;
            }
        }

        private static int GetHighestSkilledSwordTier(PlayerMagicState state)
        {
            if ((state.SkilledSwordMask & SkilledSwordTier3Flag) != 0)
                return 3;
            if ((state.SkilledSwordMask & SkilledSwordTier2Flag) != 0)
                return 2;
            if ((state.SkilledSwordMask & SkilledSwordTier1Flag) != 0)
                return 1;

            return 0;
        }

        private static float GetSkilledSwordDamageBonus(PlayerMagicState state)
        {
            var damageBonus = 0f;

            if ((state.SkilledSwordMask & SkilledSwordTier1Flag) != 0)
                damageBonus += 50f;
            if ((state.SkilledSwordMask & SkilledSwordTier2Flag) != 0)
                damageBonus += 100f;
            if ((state.SkilledSwordMask & SkilledSwordTier3Flag) != 0)
                damageBonus += 200f;

            return damageBonus;
        }

        private static string GetSkilledSwordSummary(PlayerMagicState state)
        {
            var activeTiers = new List<string>(3);

            if ((state.SkilledSwordMask & SkilledSwordTier1Flag) != 0)
                activeTiers.Add("T1");
            if ((state.SkilledSwordMask & SkilledSwordTier2Flag) != 0)
                activeTiers.Add("T2");
            if ((state.SkilledSwordMask & SkilledSwordTier3Flag) != 0)
                activeTiers.Add("T3");

            return activeTiers.Count == 0 ? "none" : string.Join("+", activeTiers);
        }

        private static string GetTierSuffix(int tier)
        {
            switch (tier)
            {
                case 1:
                    return "I";
                case 2:
                    return "II";
                case 3:
                    return "III";
                default:
                    return tier.ToString();
            }
        }

        private static string GetArmorSlotName(MagicArmorSlot slot)
        {
            switch (slot)
            {
                case MagicArmorSlot.Helm:
                    return "Helm";
                case MagicArmorSlot.Chest:
                    return "Chest";
                case MagicArmorSlot.Gloves:
                    return "Gloves";
                case MagicArmorSlot.Legs:
                    return "Legs";
                case MagicArmorSlot.Boots:
                    return "Boots";
                case MagicArmorSlot.Shield:
                    return "Shield";
                default:
                    return "Armor";
            }
        }

        private static string GetArmorDisplayName(MagicArmorSlot slot, int tier)
        {
            var tierSuffix = tier <= 1 ? string.Empty : " " + GetTierSuffix(tier);

            switch (slot)
            {
                case MagicArmorSlot.Helm:
                    return "Skilled Helm" + tierSuffix;
                case MagicArmorSlot.Chest:
                    return "Skilled Chest" + tierSuffix;
                case MagicArmorSlot.Gloves:
                    return "Skilled Gloves" + tierSuffix;
                case MagicArmorSlot.Legs:
                    return "Skilled Legs" + tierSuffix;
                case MagicArmorSlot.Boots:
                    return "Skilled Boots" + tierSuffix;
                case MagicArmorSlot.Shield:
                    return "Skilled Shield" + tierSuffix;
                default:
                    return "Skilled Armor" + tierSuffix;
            }
        }

        private static ushort GetSkilledSwordItemIndex(int tier)
        {
            switch (tier)
            {
                case 1:
                    return Pandaros.Settlers.Items.Magical.SkilledSword.Item.ItemIndex;
                case 2:
                    return Pandaros.Settlers.Items.Magical.SkilledSword1.Item.ItemIndex;
                case 3:
                    return Pandaros.Settlers.Items.Magical.SkilledSword2.Item.ItemIndex;
                default:
                    return 0;
            }
        }

        private static string GetSkilledSwordDisplayName(int tier)
        {
            switch (tier)
            {
                case 1:
                    return "Skilled Sword";
                case 2:
                    return "Skilled Sword I";
                case 3:
                    return "Skilled Sword II";
                default:
                    return "Skilled Sword";
            }
        }

        private static ushort GetArcaneFocusItemIndex(int tier)
        {
            switch (tier)
            {
                case 1:
                    return Pandaros.Settlers.Items.Magical.ArcaneFocus.Item.ItemIndex;
                case 2:
                    return Pandaros.Settlers.Items.Magical.ArcaneFocus1.Item.ItemIndex;
                case 3:
                    return Pandaros.Settlers.Items.Magical.ArcaneFocus2.Item.ItemIndex;
                default:
                    return 0;
            }
        }

        private static string GetArcaneFocusDisplayName(int tier)
        {
            switch (tier)
            {
                case 1:
                    return "Arcane Focus";
                case 2:
                    return "Arcane Focus I";
                case 3:
                    return "Arcane Focus II";
                default:
                    return "Arcane Focus";
            }
        }

        private static ushort GetSkilledArmorItemIndex(MagicArmorSlot slot, int tier)
        {
            switch (slot)
            {
                case MagicArmorSlot.Helm:
                    return tier == 1 ? Pandaros.Settlers.Items.Magical.SkilledHelm.Item.ItemIndex :
                        tier == 2 ? Pandaros.Settlers.Items.Magical.SkilledHelm1.Item.ItemIndex :
                        tier == 3 ? Pandaros.Settlers.Items.Magical.SkilledHelm2.Item.ItemIndex : (ushort)0;
                case MagicArmorSlot.Chest:
                    return tier == 1 ? Pandaros.Settlers.Items.Magical.SkilledChest.Item.ItemIndex :
                        tier == 2 ? Pandaros.Settlers.Items.Magical.SkilledChest1.Item.ItemIndex :
                        tier == 3 ? Pandaros.Settlers.Items.Magical.SkilledChest2.Item.ItemIndex : (ushort)0;
                case MagicArmorSlot.Gloves:
                    return tier == 1 ? Pandaros.Settlers.Items.Magical.SkilledGloves.Item.ItemIndex :
                        tier == 2 ? Pandaros.Settlers.Items.Magical.SkilledGloves1.Item.ItemIndex :
                        tier == 3 ? Pandaros.Settlers.Items.Magical.SkilledGloves2.Item.ItemIndex : (ushort)0;
                case MagicArmorSlot.Legs:
                    return tier == 1 ? Pandaros.Settlers.Items.Magical.SkilledLegs.Item.ItemIndex :
                        tier == 2 ? Pandaros.Settlers.Items.Magical.SkilledLegs1.Item.ItemIndex :
                        tier == 3 ? Pandaros.Settlers.Items.Magical.SkilledLegs2.Item.ItemIndex : (ushort)0;
                case MagicArmorSlot.Boots:
                    return tier == 1 ? Pandaros.Settlers.Items.Magical.SkilledBoots.Item.ItemIndex :
                        tier == 2 ? Pandaros.Settlers.Items.Magical.SkilledBoots1.Item.ItemIndex :
                        tier == 3 ? Pandaros.Settlers.Items.Magical.SkilledBoots2.Item.ItemIndex : (ushort)0;
                case MagicArmorSlot.Shield:
                    return tier == 1 ? Pandaros.Settlers.Items.Magical.SkilledShield.Item.ItemIndex :
                        tier == 2 ? Pandaros.Settlers.Items.Magical.SkilledShield1.Item.ItemIndex :
                        tier == 3 ? Pandaros.Settlers.Items.Magical.SkilledShield2.Item.ItemIndex : (ushort)0;
                default:
                    return 0;
            }
        }

        private static int GetSkilledArmorTier(PlayerMagicState state, MagicArmorSlot slot)
        {
            switch (slot)
            {
                case MagicArmorSlot.Helm:
                    return state.SkilledHelmTier;
                case MagicArmorSlot.Chest:
                    return state.SkilledChestTier;
                case MagicArmorSlot.Gloves:
                    return state.SkilledGlovesTier;
                case MagicArmorSlot.Legs:
                    return state.SkilledLegsTier;
                case MagicArmorSlot.Boots:
                    return state.SkilledBootsTier;
                case MagicArmorSlot.Shield:
                    return state.SkilledShieldTier;
                default:
                    return 0;
            }
        }

        private static void SetSkilledArmorTier(PlayerMagicState state, MagicArmorSlot slot, int value)
        {
            switch (slot)
            {
                case MagicArmorSlot.Helm:
                    state.SkilledHelmTier = value;
                    break;
                case MagicArmorSlot.Chest:
                    state.SkilledChestTier = value;
                    break;
                case MagicArmorSlot.Gloves:
                    state.SkilledGlovesTier = value;
                    break;
                case MagicArmorSlot.Legs:
                    state.SkilledLegsTier = value;
                    break;
                case MagicArmorSlot.Boots:
                    state.SkilledBootsTier = value;
                    break;
                case MagicArmorSlot.Shield:
                    state.SkilledShieldTier = value;
                    break;
            }
        }

        public static bool PlayerHasItem(Players.Player player, ushort itemIndex)
        {
            for (var i = 0; i < player.Inventory.Items.Length; i++)
            {
                if (player.Inventory.Items[i].Type == itemIndex && player.Inventory.Items[i].Amount > 0)
                    return true;
            }

            return false;
        }
    }
}
