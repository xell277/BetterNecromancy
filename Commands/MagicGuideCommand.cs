using System.Collections.Generic;
using Chatting;
using BetterNecromancy;
using Pandaros.API.Entities;
using Shared;
using System;
using System.Reflection;

[ChatCommandAutoLoader]
public class MagicGuideCommand : IChatCommand
{
    public bool TryDoCommand(Players.Player player, string chatItem, List<string> splits)
    {
        if (splits == null || splits.Count == 0 || splits[0] != "/bnmagic")
            return false;

        if (player == null)
            return true;

        if (splits.Count == 1)
        {
            SendOverview(player);
            return true;
        }

        switch (splits[1].ToLowerInvariant())
        {
            case "wands":
                SendWands(player);
                return true;

            case "armor":
                SendArmor(player);
                return true;

            case "gear":
                SendGear(player);
                return true;

            case "recipes":
                SendRecipes(player);
                return true;

            case "focus":
                SendFocus(player);
                return true;

            case "bosses":
                SendBosses(player);
                return true;

            case "progression":
                SendProgression(player);
                return true;

            case "status":
                SendStatus(player);
                return true;

            case "mastery":
                SendMastery(player);
                return true;

            case "evolve":
                HandleEvolveCommand(player, splits);
                return true;

            case "branch":
                HandleBranchCommand(player, splits);
                return true;

            case "lineage":
                Chat.Send(player, PlayerMagicStateManager.GetWandLineageStatusText(player));
                return true;

            case "resetwand":
            case "resetlineage":
                HandleResetWandCommand(player);
                return true;

            case "events":
                SendEvents(player);
                return true;

            case "event":
                HandleEventCommand(player, splits);
                return true;

            case "weather":
                HandleWeatherCommand(player, splits);
                return true;

            case "flight":
                HandleFlightCommand(player, splits);
                return true;

            case "tuning":
                SendTuning(player);
                return true;

            case "qa":
                SendQa(player);
                return true;

            case "givewands":
                GiveWands(player);
                return true;

            case "givegear":
                GiveGear(player);
                return true;

            case "giveflight":
                GiveFlight(player);
                return true;

            case "givewizards":
            case "givewizzards":
                GiveWizards(player);
                return true;

            case "giveritual":
                GiveRitual(player);
                return true;

            case "cleargear":
                ClearGear(player);
                return true;

            default:
                SendOverview(player);
                return true;
        }
    }

    private static void SendOverview(Players.Player player)
    {
        Chat.Send(player, "/bnmagic wands | /bnmagic lineage | /bnmagic resetwand | /bnmagic armor | /bnmagic gear | /bnmagic recipes | /bnmagic focus | /bnmagic bosses | /bnmagic progression | /bnmagic status | /bnmagic mastery | /bnmagic events | /bnmagic event <bloodmoon|plaguefog|arcanestorm|horde|clear> | /bnmagic weather <status|on|off> | /bnmagic flight <status|on|off|toggle> | /bnmagic tuning | /bnmagic qa | /bnmagic givewands | /bnmagic givegear | /bnmagic giveflight | /bnmagic givewizards | /bnmagic giveritual | /bnmagic cleargear");
        Chat.Send(player, "Magic progression: craft one Starter Wand, master it, then choose the first branch in the automatic evolution window. Tier 6 is ritual-only.");
        Chat.Send(player, "Mana Flight: craft and right-click the Mana Flight Harness to unlock it permanently. Then press F to use built-in flight. Early mastery tiers are unstable, expensive, and smooth out as your Flight Mastery rises.");
    }

    private static void SendWands(Players.Player player)
    {
        Chat.Send(player, "There is only one player wand lineage now: craft the Starter Wand, then the same wand evolves in place.");
        Chat.Send(player, "When the first milestone is ready, the next cast opens the evolution choice window automatically.");
        Chat.Send(player, "First branches: Frost = control/shatter, Ember = burst/ignite, Venom = poison/spread, Stone = heavy stagger/anti-elite.");
        Chat.Send(player, "Later tiers open more form choices too, so you can build hybrids like poison + chain or burn + freeze while keeping one wand lineage.");
        Chat.Send(player, "Tier 6 is ritual-only. Bring a Tier 5 lineage wand to the ritual awakening flow.");
        Chat.Send(player, PlayerMagicStateManager.GetWandLineageStatusText(player));
    }

    private static void SendArmor(Players.Player player)
    {
        var pieces = PlayerMagicStateManager.GetSkilledArmorPieceCount(player);
        var setStage = PlayerMagicStateManager.GetSkilledArmorSetBonusStage(player);
        Chat.Send(player, "Arcane armor uses virtual slots. Right-click a piece in your hand to equip it permanently; the item is consumed from your inventory.");
        Chat.Send(player, $"Active tiers | Helm: {GetArmorTier(player, PlayerMagicStateManager.MagicArmorSlot.Helm)} Chest: {GetArmorTier(player, PlayerMagicStateManager.MagicArmorSlot.Chest)} Gloves: {GetArmorTier(player, PlayerMagicStateManager.MagicArmorSlot.Gloves)} Legs: {GetArmorTier(player, PlayerMagicStateManager.MagicArmorSlot.Legs)} Boots: {GetArmorTier(player, PlayerMagicStateManager.MagicArmorSlot.Boots)} Shield: {GetArmorTier(player, PlayerMagicStateManager.MagicArmorSlot.Shield)}");
        Chat.Send(player, $"Set bonus progress: {pieces}/6 pieces active | current stage: {GetSetBonusLabel(setStage)}");
        Chat.Send(player, "Helm boosts damage. Chest boosts healing received. Gloves reduce wand cooldown and preserve reagents.");
        Chat.Send(player, "Legs improve crit chance and crit damage. Boots reduce fall damage. Shield reduces incoming damage.");
        Chat.Send(player, "Set bonuses: 2 pieces = +8% spell damage, 4 pieces = faster mana regeneration, 6 pieces = spells cost 1 less mana.");
    }

    private static void SendGear(Players.Player player)
    {
        Chat.Send(player, $"Skilled Sword stack: {PlayerMagicStateManager.GetSkilledSwordSummary(player)} | Melee bonus: +{PlayerMagicStateManager.GetSkilledSwordDamageBonus(player):0} | Boots of Falling: {(PlayerMagicStateManager.IsBootsOfFallingEquipped(player) ? "equipped" : "inactive")} | Health Booster: {(PlayerMagicStateManager.IsHealthBoosterEquipped(player) ? "equipped" : "inactive")}");
        Chat.Send(player, "Skilled Sword: right-click to equip a permanent melee buff. Sword tiers stack together now: T1 +50, T2 +100, T3 +200.");
        Chat.Send(player, "Boots of Falling: separate utility item for fall protection. Right-click equips it permanently.");
        Chat.Send(player, "Health Booster: right-click equips permanent passive health regeneration.");
        Chat.Send(player, "Boss shortcuts: associated bosses can now rarely drop tier 1, tier 2 and very rare tier 3 magic gear pieces.");
    }

    private static void SendRecipes(Players.Player player)
    {
        Chat.Send(player, "Starter Wand: player crafting, 4 planks. This is the only normal craftable player wand.");
        Chat.Send(player, "Branch and higher wand forms are unlocked through lineage mastery and transformed in place, not crafted as separate weapons.");
        Chat.Send(player, "Ritual Awakening: ritualist recipe consumes ritual materials and requires a Tier 5 lineage wand owned by a connected colony owner.");
        Chat.Send(player, "Arcane Focus: Alchemist after Steel. Focus I and II: Alchemist after Nitro.");
        Chat.Send(player, "Boots of Falling: Alchemist. Needs Elementium, all four stones, Esper, Mana, linen and leather.");
        Chat.Send(player, "Skilled Sword and magic armor: Alchemist. Tier 1 starts with Adamantine, all four stones, Esper and core magic mats; higher tiers use heavier direct material recipes.");
    }

    private static void SendBosses(Players.Player player)
    {
        Chat.Send(player, "Bosses no longer drop separate branch wands. They drop resources, gear, focus upgrades, mage guards, and ritual materials.");
        Chat.Send(player, "Your player wand progression stays on the one save-backed lineage and cannot be skipped by collecting parallel wand items.");
        Chat.Send(player, "ZombieQueen / Phase -> Arcane Focus, Fallen Ranger / Hoarder -> Arcane Focus I, Juggernaut -> Arcane Focus II.");
        Chat.Send(player, "Tier 1 gear: ZombieKing -> Helm, Putrid Corpse -> Chest, Fallen Ranger -> Gloves, Phase -> Legs, Jack-b-Nimble -> Boots / Boots of Falling, Hoarder -> Skilled Sword, Juggernaut -> Shield.");
        Chat.Send(player, "Tier 2 gear: ZombieQueen -> Helm I, Putrid Corpse -> Chest I, Fallen Ranger -> Gloves I, Phase -> Legs I, Jack-b-Nimble -> Boots I, Hoarder -> Skilled Sword I, Juggernaut -> Shield I.");
        Chat.Send(player, "Tier 3 gear: the same associated bosses can very rarely drop Helm II / Chest II / Gloves II / Legs II / Boots II / Sword II / Shield II.");
    }

    private static void SendFocus(Players.Player player)
    {
        Chat.Send(player, "Arcane Focus: right-click to equip a passive spell focus permanently.");
        Chat.Send(player, "Arcane Focus: spell damage +8%, wand cooldown -40ms.");
        Chat.Send(player, "Arcane Focus I: spell damage +16%, wand cooldown -80ms, mana cost -1 on heavy casts.");
        Chat.Send(player, "Arcane Focus II: spell damage +25%, wand cooldown -120ms, mana cost -1 on most casts.");
        Chat.Send(player, "Foci stack with mana crystals and skilled armor, but only one focus tier can be active at a time.");
    }

    private static void SendProgression(Players.Player player)
    {
        Chat.Send(player, "T1: Starter Wand. T2: the next cast at the first lineage milestone opens the Frost, Ember, Venom, or Stone choice window.");
        Chat.Send(player, "T3-T5: each major threshold opens another form choice. Previous effects stay inherited, so hybrid builds are intentional.");
        Chat.Send(player, "T6: ritual awakening only. The ritual transforms the existing Tier 5 lineage into its awakened branch form.");
        Chat.Send(player, "Mastery: casts, hits, status applications, multi-target moments, elites, bosses, and ritual contribution feed the one lineage. Every level adds +20% damage.");
        Chat.Send(player, "Meditation: crouch and stand still. Full-mana meditation permanently grants +1 max mana every 100s and a regen milestone at 1000s.");
        Chat.Send(player, "Mana bottles grow max mana by +1. Mana crystals give +2 max mana and faster regeneration.");
    }

    private static void SendStatus(Players.Player player)
    {
        var mana = PlayerMagicStateManager.GetCurrentMana(player);
        var maxMana = PlayerMagicStateManager.GetMaxMana(player);
        var crystals = PlayerMagicStateManager.GetManaCrystalLevel(player);
        var focus = PlayerMagicStateManager.GetArcaneFocusTier(player);
        var armorPieces = PlayerMagicStateManager.GetSkilledArmorPieceCount(player);
        var armorSetStage = PlayerMagicStateManager.GetSkilledArmorSetBonusStage(player);
        var cooldownReduction = PlayerMagicStateManager.GetMagicCooldownReductionMs(player);
        var damageMultiplier = PlayerMagicStateManager.GetMagicSpellDamageMultiplier(player);
        var heavyCostReduction = PlayerMagicStateManager.GetMagicManaCostReduction(player, 6);
        var critChance = PlayerMagicStateManager.GetPlayerClickCritChance(player) * 100f;
        var critBonus = PlayerMagicStateManager.GetPlayerClickCritBonus(player);

        Chat.Send(player, $"Mana: {mana}/{maxMana} | Mana Crystals: {crystals} | Arcane Focus Tier: {focus}");
        Chat.Send(player, PlayerMagicStateManager.GetWandLineageStatusText(player));
        Chat.Send(player, $"Spell multiplier: x{damageMultiplier:0.00} | Cooldown reduction: {cooldownReduction}ms | Heavy cast mana reduction: -{heavyCostReduction}");
        Chat.Send(player, $"Armor set: {armorPieces}/6 pieces | {GetSetBonusLabel(armorSetStage)}");
        Chat.Send(player, $"Gear | Skilled Sword stack: {PlayerMagicStateManager.GetSkilledSwordSummary(player)} (+{PlayerMagicStateManager.GetSkilledSwordDamageBonus(player):0}) | Boots of Falling: {(PlayerMagicStateManager.IsBootsOfFallingEquipped(player) ? "on" : "off")} | Health Booster: {(PlayerMagicStateManager.IsHealthBoosterEquipped(player) ? "on" : "off")}");
        Chat.Send(player, $"Armor bonuses | Helm: {GetArmorTier(player, PlayerMagicStateManager.MagicArmorSlot.Helm)} Chest: {GetArmorTier(player, PlayerMagicStateManager.MagicArmorSlot.Chest)} Gloves: {GetArmorTier(player, PlayerMagicStateManager.MagicArmorSlot.Gloves)} Legs: {GetArmorTier(player, PlayerMagicStateManager.MagicArmorSlot.Legs)} Boots: {GetArmorTier(player, PlayerMagicStateManager.MagicArmorSlot.Boots)} Shield: {GetArmorTier(player, PlayerMagicStateManager.MagicArmorSlot.Shield)}");
        Chat.Send(player, $"Melee crit | Chance: {critChance:0}% | Bonus: +{critBonus:0}");
        Chat.Send(player, $"Mana Flight | Unlocked: {(PlayerMagicStateManager.IsManaFlightUnlocked(player) ? "yes" : "no")} | Enabled: {(PlayerMagicStateManager.IsManaFlightEnabled(player) ? "yes" : "no")} | Active: {(PlayerMagicStateManager.IsManaFlightActive(player) ? "yes" : "no")} | {PlayerMagicStateManager.GetFlightManaCostDisplay(player)} | {PlayerMagicStateManager.GetFlightMasteryLevelLabel(PlayerMagicStateManager.GetFlightMasteryLevel(player))} ({PlayerMagicStateManager.GetFlightMasteryProgressLabel(player)})");
        Chat.Send(player, "World Event | " + WorldEventManager.GetStatusText());

        if (PlayerMagicStateManager.TryGetSelectedWandMastery(player, out var selectedKey, out var selectedName))
        {
            var selectedEvolved = PlayerMagicStateManager.IsWandLegendaryEvolutionUnlocked(player, selectedKey);
            var selectedLevel = selectedEvolved
                ? PlayerMagicStateManager.GetLegendaryWandMasteryLevel(player, selectedKey)
                : PlayerMagicStateManager.GetWandMasteryLevel(player, selectedKey);
            var selectedProgress = selectedEvolved
                ? PlayerMagicStateManager.GetLegendaryWandMasteryProgressLabel(player, selectedKey)
                : PlayerMagicStateManager.GetWandMasteryProgressLabel(player, selectedKey);
            var selectedLegendary = PlayerMagicStateManager.IsWandLegendaryEvolutionUnlocked(player, selectedKey)
                ? PlayerMagicStateManager.GetWandLegendaryDisplayName(player, selectedKey)
                : "not awakened";
            Chat.Send(player, $"Selected wand | {selectedName}: {(selectedEvolved ? "Evolution " : string.Empty)}{PlayerMagicStateManager.GetWandMasteryLevelLabel(selectedLevel)} ({selectedProgress}) | {(selectedEvolved ? PlayerMagicStateManager.GetWandLegendaryCompactSummary(player, selectedKey) : PlayerMagicStateManager.GetWandMasteryCompactSummary(selectedKey, selectedLevel))} | Legendary: {selectedLegendary}");
        }
    }

    private static void SendMastery(Players.Player player)
    {
        Chat.Send(player, "Wand Mastery: casts, hits and role actions feed the one lineage. Mastery levels are separate from evolution tier thresholds.");
        Chat.Send(player, "Evolution tiers now unlock through staged choice windows. Each choice adds inherited effects instead of replacing older identity. Every mastery level adds +20% damage.");
        Chat.Send(player, $"Flight Mastery: {PlayerMagicStateManager.GetFlightMasteryLevelLabel(PlayerMagicStateManager.GetFlightMasteryLevel(player))} ({PlayerMagicStateManager.GetFlightMasteryProgressLabel(player)}) | {PlayerMagicStateManager.GetFlightMasteryCompactSummary(player)}");
        Chat.Send(player, "Flight thresholds | T1: 1000s | T2: 10000s | T3: 100000s");

        if (PlayerMagicStateManager.TryGetSelectedWandMastery(player, out var selectedKey, out var selectedName))
        {
            var selectedEvolved = PlayerMagicStateManager.IsWandLegendaryEvolutionUnlocked(player, selectedKey);
            var selectedLevel = selectedEvolved
                ? PlayerMagicStateManager.GetLegendaryWandMasteryLevel(player, selectedKey)
                : PlayerMagicStateManager.GetWandMasteryLevel(player, selectedKey);
            var selectedProgress = selectedEvolved
                ? PlayerMagicStateManager.GetLegendaryWandMasteryProgressLabel(player, selectedKey)
                : PlayerMagicStateManager.GetWandMasteryProgressLabel(player, selectedKey);
            var legendaryLine = PlayerMagicStateManager.GetWandLegendaryCompactSummary(player, selectedKey);
            Chat.Send(player, $"Selected wand | {selectedName}: {(selectedEvolved ? "Evolution " : string.Empty)}{PlayerMagicStateManager.GetWandMasteryLevelLabel(selectedLevel)} ({selectedProgress}) | {(selectedEvolved ? legendaryLine : PlayerMagicStateManager.GetWandMasteryCompactSummary(selectedKey, selectedLevel))}");
        }

        var summaryParts = new List<string>();
        foreach (var key in PlayerMagicStateManager.GetWandMasteryKeys())
        {
            var displayName = PlayerMagicStateManager.GetWandMasteryDisplayName(key).Replace(" Wand", string.Empty);
            var level = PlayerMagicStateManager.GetWandMasteryLevel(player, key);
            var progressLabel = PlayerMagicStateManager.GetWandMasteryProgressLabel(player, key);
            var summary = $"{displayName} {PlayerMagicStateManager.GetWandMasteryLevelLabel(level)} ({progressLabel})";
            if (PlayerMagicStateManager.IsWandLegendaryEvolutionUnlocked(player, key))
            {
                var evolvedLevel = PlayerMagicStateManager.GetLegendaryWandMasteryLevel(player, key);
                summary += $" -> Evo {PlayerMagicStateManager.GetWandMasteryLevelLabel(evolvedLevel)} ({PlayerMagicStateManager.GetLegendaryWandMasteryProgressLabel(player, key)})";
            }

            summaryParts.Add(summary);
        }

        for (var i = 0; i < summaryParts.Count; i += 3)
        {
            var count = Math.Min(3, summaryParts.Count - i);
            Chat.Send(player, string.Join(" | ", summaryParts.GetRange(i, count)));
        }

        var keyHighlights = new[]
        {
            PlayerMagicStateManager.WandMasteryKeys.Spark,
            PlayerMagicStateManager.WandMasteryKeys.Ember,
            PlayerMagicStateManager.WandMasteryKeys.Aether,
            PlayerMagicStateManager.WandMasteryKeys.Void
        };

        for (var i = 0; i < keyHighlights.Length; i++)
        {
            var key = keyHighlights[i];
            var level = PlayerMagicStateManager.GetWandMasteryLevel(player, key);
            Chat.Send(player, PlayerMagicStateManager.GetWandMasteryDisplayName(key) + ": " + PlayerMagicStateManager.GetWandMasteryCompactSummary(key, level));
        }
    }

    private static void HandleEvolveCommand(Players.Player player, List<string> splits)
    {
        Chat.Send(player, "Wand evolution is lineage-based now. Cast with the Starter Wand at the first cap and the branch-choice window opens automatically. Use /bnmagic lineage only for status.");
        return;

#pragma warning disable 162
        if (!TryResolveEvolutionTarget(player, splits, out var masteryKey, out var currentName, out var message))
        {
            Chat.Send(player, message);
            return;
        }

        if (!LegendaryWandEvolutionManager.TryGetProfile(masteryKey, out var profile))
        {
            Chat.Send(player, "No legendary evolution profile exists for that wand.");
            return;
        }

        if (PlayerMagicStateManager.IsWandLegendaryEvolutionUnlocked(player, masteryKey))
        {
            Chat.Send(player, profile.LegendaryName + " is already awakened. " + profile.Summary);
            return;
        }

        if (splits.Count < 3)
        {
            Chat.Send(player, currentName + " evolves into " + profile.LegendaryName + " automatically at Mastery 10.");
            Chat.Send(player, "Current mastery: " + PlayerMagicStateManager.GetWandMasteryLevelLabel(PlayerMagicStateManager.GetWandMasteryLevel(player, masteryKey)) + ".");
            Chat.Send(player, "No stockpile cost anymore. Cast once at Mastery 10 and the wand is replaced for free.");
            return;
        }

        if (!LegendaryWandEvolutionManager.TryUnlock(player, null, masteryKey, out var evolveMessage))
        {
            Chat.Send(player, evolveMessage);
            return;
        }

        Chat.Send(player, evolveMessage);
#pragma warning restore 162
    }

    private static void HandleBranchCommand(Players.Player player, List<string> splits)
    {
        if (splits.Count < 3)
        {
            Chat.Send(player, PlayerMagicStateManager.GetWandLineageStatusText(player));
            Chat.Send(player, "Branch choice normally happens through the automatic evolution window. Fallback/testing: /bnmagic branch <frost|ember|venom|stone>.");
            return;
        }

        if (PlayerMagicStateManager.TryChooseWandBranch(player, splits[2], out var message))
        {
            Chat.Send(player, message);
            return;
        }

        Chat.Send(player, message);
    }

    private static void HandleResetWandCommand(Players.Player player)
    {
        PlayerMagicStateManager.ResetWandLineage(player, out var message);
        Chat.Send(player, message);
    }

    private static void SendEvents(Players.Player player)
    {
        Chat.Send(player, "World Events: Blood Moon, Plague Fog, Arcane Storm, Horde Assault.");
        Chat.Send(player, WorldEventManager.GetStatusText());
        Chat.Send(player, WorldEventManager.GetWeatherSyncStatusText(player));
        Chat.Send(player, "Blood Moon: zombies hit harder, resist a bit more, kills grant bonus colony points, and the end of the event drops blood-themed loot into colony stockpiles.");
        Chat.Send(player, "Plague Fog: zombies are harder to finish, kills grant heavy colony points, and the end of the event drops plague-themed loot into colony stockpiles.");
        Chat.Send(player, "Arcane Storm: magic damage rises, mana regenerates faster, cooldowns shorten, casts get cheaper, and the end of the event drops arcane-themed loot.");
        Chat.Send(player, "Horde Assault: a full wave of normal zombies spawns at a colony, reinforcements hit in later stages, and surviving the rush grants colony points plus horde loot.");
    }

    private static void HandleEventCommand(Players.Player player, List<string> splits)
    {
        if (splits.Count < 3)
        {
            Chat.Send(player, "Usage: /bnmagic event <bloodmoon|plaguefog|arcanestorm|horde|clear>");
            Chat.Send(player, WorldEventManager.GetStatusText());
            return;
        }

        if (string.Equals(splits[2], "clear", StringComparison.OrdinalIgnoreCase))
        {
            if (!WorldEventManager.TryClearEvent(out var clearMessage))
            {
                Chat.Send(player, clearMessage);
                return;
            }

            Chat.Send(player, clearMessage);
            return;
        }

        if (!WorldEventManager.TryStartEvent(splits[2], player, out var startMessage))
        {
            Chat.Send(player, startMessage);
            return;
        }

        Chat.Send(player, startMessage);
    }

    private static void HandleWeatherCommand(Players.Player player, List<string> splits)
    {
        if (splits.Count < 3 || string.Equals(splits[2], "status", StringComparison.OrdinalIgnoreCase))
        {
            Chat.Send(player, WorldEventManager.GetWeatherSyncStatusText(player));
            return;
        }

        if (string.Equals(splits[2], "on", StringComparison.OrdinalIgnoreCase))
        {
            WorldEventManager.SetWeatherSyncEnabled(true);
            Chat.Send(player, "World event weather sync enabled.");
            Chat.Send(player, WorldEventManager.GetWeatherSyncStatusText(player));
            return;
        }

        if (string.Equals(splits[2], "off", StringComparison.OrdinalIgnoreCase))
        {
            WorldEventManager.SetWeatherSyncEnabled(false);
            Chat.Send(player, "World event weather sync disabled.");
            Chat.Send(player, WorldEventManager.GetWeatherSyncStatusText(player));
            return;
        }

        Chat.Send(player, "Usage: /bnmagic weather <status|on|off>");
    }

    private static void HandleFlightCommand(Players.Player player, List<string> splits)
    {
        if (splits.Count < 3 || string.Equals(splits[2], "status", StringComparison.OrdinalIgnoreCase))
        {
            Chat.Send(player, $"Mana Flight | Unlocked: {(PlayerMagicStateManager.IsManaFlightUnlocked(player) ? "yes" : "no")} | Enabled: {(PlayerMagicStateManager.IsManaFlightEnabled(player) ? "yes" : "no")} | Active: {(PlayerMagicStateManager.IsManaFlightActive(player) ? "yes" : "no")} | Current mana: {PlayerMagicStateManager.GetCurrentMana(player)}/{PlayerMagicStateManager.GetMaxMana(player)}");
            Chat.Send(player, $"{PlayerMagicStateManager.GetFlightMasteryLevelLabel(PlayerMagicStateManager.GetFlightMasteryLevel(player))} ({PlayerMagicStateManager.GetFlightMasteryProgressLabel(player)}) | {PlayerMagicStateManager.GetFlightManaCostDisplay(player)} | {PlayerMagicStateManager.GetFlightMasteryCompactSummary(player)}");
            Chat.Send(player, "Press F to toggle the built-in flight while Mana Flight is enabled. Usage: /bnmagic flight <on|off|toggle|status>");
            return;
        }

        var action = splits[2].ToLowerInvariant();
        bool? enabled = null;

        switch (action)
        {
            case "on":
                enabled = true;
                break;

            case "off":
                enabled = false;
                break;

            case "toggle":
                enabled = !PlayerMagicStateManager.IsManaFlightActive(player);
                break;

            default:
                Chat.Send(player, "Usage: /bnmagic flight <on|off|toggle|status>");
                return;
        }

        if (PlayerMagicStateManager.TrySetManaFlight(player, enabled.Value, out var message))
        {
            Chat.Send(player, message);
            return;
        }

        Chat.Send(player, message);
    }

    private static void SendTuning(Players.Player player)
    {
        Chat.Send(player, "Mana Wand: 48 dmg / 2 mana, Overcharge 92 dmg / 5 mana, 430ms / 860ms, range 30 / 38.");
        Chat.Send(player, "Briar Wand: 40 dmg / 3 mana with thorn bleed, Brambleburst 70 dmg / 5 mana, close plague spread, range 32 / 36.");
        Chat.Send(player, "Spark Wand: 36 dmg / 2 mana, Static Surge 72 dmg / 4 mana, quick chains, range 32 / 38.");
        Chat.Send(player, "Venom Wand: 34 dmg / 3 mana plus poison, Toxic Bloom 62 dmg / 6 mana with plague spread, range 34 / 38.");
        Chat.Send(player, "Ember Wand: 46 dmg / 3 mana, Inferno 82 dmg / 6 mana, heavy burn + fire splash, range 34 / 38.");
        Chat.Send(player, "Frost Wand: 42 dmg / 3 mana, Shatter 74 dmg / 6 mana, strong knockback + frost nova, range 34 / 38.");
        Chat.Send(player, "Crystal Wand: 60 dmg / 4 mana, Prism Break 98 dmg / 7 mana, long precise burst, range 40 / 42.");
        Chat.Send(player, "Stone Wand: 68 dmg / 4 mana, Quake 108 dmg / 7 mana, very high impact force, range 32 / 34.");
        Chat.Send(player, "Storm Wand: 58 dmg / 4 mana, Tempest 94 dmg / 8 mana, wide chain clear, range 38 / 42.");
        Chat.Send(player, "Aether Wand: 52 dmg / 3 mana, self-heal 6 mana, target-heal 4 mana, sustain/support school.");
        Chat.Send(player, "Blood Wand: 84 dmg / 5 mana with lifesteal, Hemorrhage 136 dmg / 9 mana with bleed burst, range 36 / 40.");
        Chat.Send(player, "Void Wand: 140 dmg / 6 mana, Rupture 220 dmg / 10 mana, chained void damage over time, range 42 / 44.");
    }

    private static void SendQa(Players.Player player)
    {
        Chat.Send(player, "QA route: /bnmagic status -> test Mana -> Briar / Spark -> Venom / Ember / Frost -> Crystal / Stone / Storm -> Aether -> Blood -> Void.");
        Chat.Send(player, "Watch for: crowd clear, poison pace, knockback feel, sustain value, and whether Blood / Void feel worth the higher mana spend.");
        Chat.Send(player, "Boss QA: ZombieQueen for Frost/Focus, Fallen Ranger for Aether/Focus I, Juggernaut for Void/Focus II and crystal progression.");
        Chat.Send(player, "If a wand feels off, tell me: too weak, too strong, too cheap, too expensive, too fast or too slow.");
    }

    private static void GiveWands(Players.Player player)
    {
        var starterWand = Pandaros.Settlers.Items.ManaWand.Item.ItemIndex;
        if (TryGiveItem(player, starterWand, 1))
        {
            Chat.Send(player, "Added Starter Wand. Branch forms are now unlocked by the one wand lineage, not by giving parallel wand items.");
            return;
        }

        if (TryGiveToActiveColonyStockpile(player, starterWand, 1))
        {
            Chat.Send(player, "Inventory full. Sent Starter Wand to colony stockpile. Branch forms are lineage-driven now.");
            return;
        }

        Chat.Send(player, "Could not add Starter Wand. Inventory may be full and no nearby active colony stockpile was available.");
    }

    private static void GiveGear(Players.Player player)
    {
        if (!TryResolvePreferredStockpileColony(player, out var colony))
        {
            Chat.Send(player, "No owned colony stockpile with a placed banner was found nearby. Stand near one of your banners and try /bnmagic givegear again.");
            return;
        }

        var stockpile = colony.ColonyGroup.Stockpile;

        var gearDefinitions = new[]
        {
            Tuple.Create("Boots of Falling", Pandaros.Settlers.Items.Armor.Magical.BootsOfFalling.Item.ItemIndex),
            Tuple.Create("Health Booster", Pandaros.Settlers.Items.Magical.HealthBooster.Item.ItemIndex),
            Tuple.Create("Mana Crystal", Pandaros.Settlers.Items.Magical.ManaCrystal.Item.ItemIndex),
            Tuple.Create("Arcane Focus", Pandaros.Settlers.Items.Magical.ArcaneFocus.Item.ItemIndex),
            Tuple.Create("Arcane Focus I", Pandaros.Settlers.Items.Magical.ArcaneFocus1.Item.ItemIndex),
            Tuple.Create("Arcane Focus II", Pandaros.Settlers.Items.Magical.ArcaneFocus2.Item.ItemIndex),
            Tuple.Create("Skilled Sword", Pandaros.Settlers.Items.Magical.SkilledSword.Item.ItemIndex),
            Tuple.Create("Skilled Sword I", Pandaros.Settlers.Items.Magical.SkilledSword1.Item.ItemIndex),
            Tuple.Create("Skilled Sword II", Pandaros.Settlers.Items.Magical.SkilledSword2.Item.ItemIndex),
            Tuple.Create("Skilled Helm", Pandaros.Settlers.Items.Magical.SkilledHelm.Item.ItemIndex),
            Tuple.Create("Skilled Helm I", Pandaros.Settlers.Items.Magical.SkilledHelm1.Item.ItemIndex),
            Tuple.Create("Skilled Helm II", Pandaros.Settlers.Items.Magical.SkilledHelm2.Item.ItemIndex),
            Tuple.Create("Skilled Chest", Pandaros.Settlers.Items.Magical.SkilledChest.Item.ItemIndex),
            Tuple.Create("Skilled Chest I", Pandaros.Settlers.Items.Magical.SkilledChest1.Item.ItemIndex),
            Tuple.Create("Skilled Chest II", Pandaros.Settlers.Items.Magical.SkilledChest2.Item.ItemIndex),
            Tuple.Create("Skilled Gloves", Pandaros.Settlers.Items.Magical.SkilledGloves.Item.ItemIndex),
            Tuple.Create("Skilled Gloves I", Pandaros.Settlers.Items.Magical.SkilledGloves1.Item.ItemIndex),
            Tuple.Create("Skilled Gloves II", Pandaros.Settlers.Items.Magical.SkilledGloves2.Item.ItemIndex),
            Tuple.Create("Skilled Legs", Pandaros.Settlers.Items.Magical.SkilledLegs.Item.ItemIndex),
            Tuple.Create("Skilled Legs I", Pandaros.Settlers.Items.Magical.SkilledLegs1.Item.ItemIndex),
            Tuple.Create("Skilled Legs II", Pandaros.Settlers.Items.Magical.SkilledLegs2.Item.ItemIndex),
            Tuple.Create("Skilled Boots", Pandaros.Settlers.Items.Magical.SkilledBoots.Item.ItemIndex),
            Tuple.Create("Skilled Boots I", Pandaros.Settlers.Items.Magical.SkilledBoots1.Item.ItemIndex),
            Tuple.Create("Skilled Boots II", Pandaros.Settlers.Items.Magical.SkilledBoots2.Item.ItemIndex),
            Tuple.Create("Skilled Shield", Pandaros.Settlers.Items.Magical.SkilledShield.Item.ItemIndex),
            Tuple.Create("Skilled Shield I", Pandaros.Settlers.Items.Magical.SkilledShield1.Item.ItemIndex),
            Tuple.Create("Skilled Shield II", Pandaros.Settlers.Items.Magical.SkilledShield2.Item.ItemIndex)
        };

        var added = new List<string>(gearDefinitions.Length);
        for (var i = 0; i < gearDefinitions.Length; i++)
        {
            stockpile.Add(gearDefinitions[i].Item2, 1);
            added.Add(gearDefinitions[i].Item1);
        }

        stockpile.SendToOwners();
        Chat.Send(player, "Added gear to colony stockpile: " + string.Join(", ", added) + ".");
    }

    private static void GiveFlight(Players.Player player)
    {
        if (!TryResolvePreferredStockpileColony(player, out var colony))
        {
            Chat.Send(player, "No owned colony stockpile with a placed banner was found nearby. Stand near one of your banners and try /bnmagic giveflight again.");
            return;
        }

        var stockpile = colony.ColonyGroup.Stockpile;
        stockpile.Add(Pandaros.Settlers.Items.Magical.ManaFlightHarness.Item.ItemIndex, 1);
        stockpile.SendToOwners();
        Chat.Send(player, "Added Mana Flight Harness x1 to colony stockpile.");
    }

    private static void GiveWizards(Players.Player player)
    {
        if (!TryResolvePreferredStockpileColony(player, out var colony))
        {
            Chat.Send(player, "No owned colony stockpile with a placed banner was found nearby. Stand near one of your banners and try /bnmagic givewizards again.");
            return;
        }

        var stockpile = colony.ColonyGroup.Stockpile;
        var wizardDefinitions = new[]
        {
            Tuple.Create("Mage Guard T1", "BetterNecromancy.MageGuardT1"),
            Tuple.Create("Mage Guard T2", "BetterNecromancy.MageGuardT2"),
            Tuple.Create("Mage Guard T3", "BetterNecromancy.MageGuardT3"),
            Tuple.Create("Mage Guard T4", "BetterNecromancy.MageGuardT4"),
            Tuple.Create("Mage Guard T5", "BetterNecromancy.MageGuardT5"),
            Tuple.Create("Mage Guard T6", "BetterNecromancy.MageGuardT6")
        };

        var added = new List<string>(wizardDefinitions.Length);
        var failed = new List<string>();

        for (var i = 0; i < wizardDefinitions.Length; i++)
        {
            if (!TryResolveItemTypeIndex(wizardDefinitions[i].Item2, out var itemIndex))
            {
                failed.Add(wizardDefinitions[i].Item1);
                continue;
            }

            stockpile.Add(itemIndex, 1);
            added.Add(wizardDefinitions[i].Item1);
        }

        if (added.Count > 0)
        {
            stockpile.SendToOwners();
            Chat.Send(player, "Added mage guards to colony stockpile: " + string.Join(", ", added) + ".");
        }

        if (failed.Count > 0)
            Chat.Send(player, "Could not resolve: " + string.Join(", ", failed) + ".");
    }

    private static void GiveRitual(Players.Player player)
    {
        if (!TryResolvePreferredStockpileColony(player, out var colony))
        {
            Chat.Send(player, "No owned colony stockpile with a placed banner was found nearby. Stand near one of your banners and try /bnmagic giveritual again.");
            return;
        }

        if (!TryResolveItemTypeIndex("BetterNecromancy.GoldenRitualAltar", out var altarItemIndex) ||
            !TryResolveItemTypeIndex("BetterNecromancy.CorpseBlock", out var corpseBlockItemIndex) ||
            !TryResolveItemTypeIndex("BetterNecromancy.InnocentSoul", out var innocentSoulItemIndex))
        {
            Chat.Send(player, "Ritual test kit failed: one of the ritual item types is missing.");
            return;
        }

        var stockpile = colony.ColonyGroup.Stockpile;
        var ritualDefinitions = new[]
        {
            Tuple.Create("Golden Ritual Altar", altarItemIndex, 1),
            Tuple.Create("Corpse Block", corpseBlockItemIndex, 4),
            Tuple.Create("Innocent Soul", innocentSoulItemIndex, 1),
            Tuple.Create("Mana Crystal", Pandaros.Settlers.Items.Magical.ManaCrystal.Item.ItemIndex, 1),
            Tuple.Create("Adamantine", Pandaros.Settlers.Items.Magical.Adamantine.Item.ItemIndex, 8),
            Tuple.Create("Elementium", Pandaros.Settlers.Items.Elementium.Item.ItemIndex, 12)
        };

        var added = new List<string>(ritualDefinitions.Length);
        for (var i = 0; i < ritualDefinitions.Length; i++)
        {
            stockpile.Add(ritualDefinitions[i].Item2, ritualDefinitions[i].Item3);
            added.Add(ritualDefinitions[i].Item1 + " x" + ritualDefinitions[i].Item3);
        }

        stockpile.SendToOwners();
        Chat.Send(player, "Added ritual test kit to colony stockpile: " + string.Join(", ", added) + ".");
        Chat.Send(player, "Setup: place the Golden Ritual Altar in the middle and 4 Corpse Blocks on the diagonal corners, then let the Ritualist craft the awakening ritual.");
        Chat.Send(player, "Requirement: a connected colony owner must already have a Tier 5 wand lineage. The ritual transforms that wand into Tier 6.");
    }

    private static void ClearGear(Players.Player player)
    {
        var removed = PlayerMagicStateManager.ClearEquippedMagicItems(player);
        if (removed.Count == 0)
        {
            Chat.Send(player, "No permanent magic gear is currently equipped.");
            return;
        }

        Chat.Send(player, "Removed equipped magic gear: " + string.Join(", ", removed) + ".");
        Chat.Send(player, "Note: this only clears equipped states. Consumed items are not refunded.");
    }

    private static bool TryGiveItem(Players.Player player, ushort itemIndex, int amount)
    {
        var inventory = player.Inventory;
        if (inventory == null)
            return false;

        if (TryInvokeInventoryAdd(inventory, "TryAdd", itemIndex, amount) ||
            TryInvokeInventoryAdd(inventory, "Add", itemIndex, amount) ||
            TryInvokeInventoryAdd(inventory, "TryAdd", ItemTypes.GetType(itemIndex), amount) ||
            TryInvokeInventoryAdd(inventory, "Add", ItemTypes.GetType(itemIndex), amount))
        {
            return true;
        }

        return TryFallbackInsert(inventory, itemIndex, amount);
    }

    private static bool TryInvokeInventoryAdd(object inventory, string methodName, object itemArg, int amount)
    {
        foreach (var method in inventory.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (!string.Equals(method.Name, methodName, StringComparison.Ordinal))
                continue;

            var parameters = method.GetParameters();
            if (parameters.Length != 2)
                continue;

            try
            {
                var result = method.Invoke(inventory, new[] { itemArg, (object)amount });
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

    private static bool TryFallbackInsert(object inventory, ushort itemIndex, int amount)
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

            var existingType = Convert.ToUInt16(ReadMemberValue(typeMember, entry));
            var existingAmount = Convert.ToInt32(ReadMemberValue(amountMember, entry));

            if (existingType == itemIndex && existingAmount > 0)
            {
                WriteMemberValue(amountMember, ref entry, existingAmount + amount);
                items.SetValue(entry, i);
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

        WriteMemberValue(freeTypeMember, ref freeEntry, itemIndex);
        WriteMemberValue(freeAmountMember, ref freeEntry, amount);
        items.SetValue(freeEntry, freeSlot);
        return true;
    }

    private static bool TryGiveToActiveColonyStockpile(Players.Player player, ushort itemIndex, int amount)
    {
        if (!TryResolvePreferredStockpileColony(player, out var colony))
            return false;

        var stockpile = colony.ColonyGroup.Stockpile;
        stockpile.Add(itemIndex, amount);
        stockpile.SendToOwners();
        return true;
    }

    private static bool TryResolveItemTypeIndex(string key, out ushort itemIndex)
    {
        itemIndex = 0;
        if (!ItemTypes.TryGetType(key, out var itemType))
            return false;

        itemIndex = itemType.ItemIndex;
        return true;
    }

    private static bool TryResolveEvolutionTarget(Players.Player player, List<string> splits, out string masteryKey, out string displayName, out string message)
    {
        masteryKey = null;
        displayName = null;
        message = null;

        if (splits.Count < 3)
        {
            if (PlayerMagicStateManager.TryGetSelectedWandMastery(player, out masteryKey, out displayName))
                return true;

            message = "Usage: /bnmagic evolve <mana|briar|spark|venom|ember|frost|crystal|stone|storm|aether|blood|void>. You can also hold a wand and just use /bnmagic evolve.";
            return false;
        }

        var requested = splits[2]?.Trim();
        if (string.IsNullOrEmpty(requested))
        {
            message = "Usage: /bnmagic evolve <wand>.";
            return false;
        }

        if (LegendaryWandEvolutionManager.TryGetProfile(requested, out var profile))
        {
            masteryKey = profile.MasteryKey;
            displayName = PlayerMagicStateManager.GetWandMasteryDisplayName(profile.MasteryKey);
            return true;
        }

        message = "Unknown wand. Try mana, briar, spark, venom, ember, frost, crystal, stone, storm, aether, blood or void.";
        return false;
    }

    private static bool TryResolvePreferredStockpileColony(Players.Player player, out Colony colony)
    {
        colony = null;

        if (player == null)
            return false;

        if (player.ActiveColony != null &&
            player.OwnsColony(player.ActiveColony) &&
            player.ActiveColony.Banners.Count > 0 &&
            player.ActiveColony.ColonyGroup?.Stockpile != null)
        {
            colony = player.ActiveColony;
            return true;
        }

        var playerPosition = player.PositionStanding;
        var bestDistance = float.MaxValue;
        var colonies = ServerManager.ColonyTracker.ColoniesByID.GetValueEnumerator();

        while (colonies.MoveNext())
        {
            var candidate = colonies.Current;
            if (candidate == null ||
                candidate.Banners.Count == 0 ||
                candidate.ColonyGroup?.Stockpile == null ||
                !player.OwnsColony(candidate))
            {
                continue;
            }

            for (var i = 0; i < candidate.Banners.Count; i++)
            {
                var bannerPosition = candidate.Banners[i].Position;
                var delta = new UnityEngine.Vector3(bannerPosition.x, bannerPosition.y, bannerPosition.z) - playerPosition;
                var distance = delta.sqrMagnitude;
                if (!(distance < bestDistance))
                    continue;

                bestDistance = distance;
                colony = candidate;
            }
        }

        return colony != null;
    }

    private static object ReadMemberValue(MemberInfo member, object target)
    {
        if (member is FieldInfo field)
            return field.GetValue(target);

        return ((PropertyInfo)member).GetValue(target);
    }

    private static void WriteMemberValue(MemberInfo member, ref object target, object value)
    {
        if (member is FieldInfo field)
        {
            field.SetValue(target, value);
            return;
        }

        ((PropertyInfo)member).SetValue(target, value);
    }

    private static int GetArmorTier(Players.Player player, PlayerMagicStateManager.MagicArmorSlot slot)
    {
        return PlayerMagicStateManager.GetSkilledArmorTier(player, slot);
    }

    private static string GetSetBonusLabel(int stage)
    {
        switch (stage)
        {
            case 3:
                return "6-piece bonus active";
            case 2:
                return "4-piece bonus active";
            case 1:
                return "2-piece bonus active";
            default:
                return "no set bonus active";
        }
    }
}
