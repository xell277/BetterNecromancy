using Newtonsoft.Json.Linq;
using NPC;
using Pandaros.API.Entities;
using Recipes;
using Shared;
using System.Collections.Generic;

namespace Pandaros.Settlers.Items.Magical
{
    public static class SkilledHelm
    {
        public static ItemTypesServer.ItemTypeRaw Item { get; internal set; }
    }

    public static class SkilledHelm1
    {
        public static ItemTypesServer.ItemTypeRaw Item { get; internal set; }
    }

    public static class SkilledHelm2
    {
        public static ItemTypesServer.ItemTypeRaw Item { get; internal set; }
    }

    public static class SkilledChest
    {
        public static ItemTypesServer.ItemTypeRaw Item { get; internal set; }
    }

    public static class SkilledChest1
    {
        public static ItemTypesServer.ItemTypeRaw Item { get; internal set; }
    }

    public static class SkilledChest2
    {
        public static ItemTypesServer.ItemTypeRaw Item { get; internal set; }
    }

    public static class SkilledGloves
    {
        public static ItemTypesServer.ItemTypeRaw Item { get; internal set; }
    }

    public static class SkilledGloves1
    {
        public static ItemTypesServer.ItemTypeRaw Item { get; internal set; }
    }

    public static class SkilledGloves2
    {
        public static ItemTypesServer.ItemTypeRaw Item { get; internal set; }
    }

    public static class SkilledLegs
    {
        public static ItemTypesServer.ItemTypeRaw Item { get; internal set; }
    }

    public static class SkilledLegs1
    {
        public static ItemTypesServer.ItemTypeRaw Item { get; internal set; }
    }

    public static class SkilledLegs2
    {
        public static ItemTypesServer.ItemTypeRaw Item { get; internal set; }
    }

    public static class SkilledBoots
    {
        public static ItemTypesServer.ItemTypeRaw Item { get; internal set; }
    }

    public static class SkilledBoots1
    {
        public static ItemTypesServer.ItemTypeRaw Item { get; internal set; }
    }

    public static class SkilledBoots2
    {
        public static ItemTypesServer.ItemTypeRaw Item { get; internal set; }
    }

    public static class SkilledShield
    {
        public static ItemTypesServer.ItemTypeRaw Item { get; internal set; }
    }

    public static class SkilledShield1
    {
        public static ItemTypesServer.ItemTypeRaw Item { get; internal set; }
    }

    public static class SkilledShield2
    {
        public static ItemTypesServer.ItemTypeRaw Item { get; internal set; }
    }

    [ModLoader.ModManager]
    public static class SkilledArmorSet
    {
        private sealed class ArmorDefinition
        {
            public ArmorDefinition(
                string key,
                string iconFile,
                string displayName,
                PlayerMagicStateManager.MagicArmorSlot slot,
                int tier,
                float armorRating,
                int durability,
                int slotIndex)
            {
                Key = key;
                IconFile = iconFile;
                DisplayName = displayName;
                Slot = slot;
                Tier = tier;
                ArmorRating = armorRating;
                Durability = durability;
                SlotIndex = slotIndex;
            }

            public string Key { get; }
            public string IconFile { get; }
            public string DisplayName { get; }
            public PlayerMagicStateManager.MagicArmorSlot Slot { get; }
            public int Tier { get; }
            public float ArmorRating { get; }
            public int Durability { get; }
            public int SlotIndex { get; }
            public ItemTypesServer.ItemTypeRaw Item { get; set; }
        }

        private static readonly ArmorDefinition[] Definitions =
        {
            new ArmorDefinition("SkilledHelm", "SkilledHelm.png", "Skilled Helm", PlayerMagicStateManager.MagicArmorSlot.Helm, 1, 0.11f, 250, 0),
            new ArmorDefinition("SkilledHelm1", "SkilledHelm1.png", "Skilled Helm I", PlayerMagicStateManager.MagicArmorSlot.Helm, 2, 0.11f, 500, 0),
            new ArmorDefinition("SkilledHelm2", "SkilledHelm2.png", "Skilled Helm II", PlayerMagicStateManager.MagicArmorSlot.Helm, 3, 0.11f, 750, 0),
            new ArmorDefinition("SkilledChest", "SkilledChest.png", "Skilled Chest", PlayerMagicStateManager.MagicArmorSlot.Chest, 1, 0.30f, 250, 1),
            new ArmorDefinition("SkilledChest1", "SkilledChest1.png", "Skilled Chest I", PlayerMagicStateManager.MagicArmorSlot.Chest, 2, 0.30f, 500, 1),
            new ArmorDefinition("SkilledChest2", "SkilledChest2.png", "Skilled Chest II", PlayerMagicStateManager.MagicArmorSlot.Chest, 3, 0.30f, 750, 1),
            new ArmorDefinition("SkilledGloves", "SkilledGloves.png", "Skilled Gloves", PlayerMagicStateManager.MagicArmorSlot.Gloves, 1, 0.07f, 250, 2),
            new ArmorDefinition("SkilledGloves1", "SkilledGloves1.png", "Skilled Gloves I", PlayerMagicStateManager.MagicArmorSlot.Gloves, 2, 0.07f, 500, 2),
            new ArmorDefinition("SkilledGloves2", "SkilledGloves2.png", "Skilled Gloves II", PlayerMagicStateManager.MagicArmorSlot.Gloves, 3, 0.07f, 750, 2),
            new ArmorDefinition("SkilledLegs", "SkilledLegs.png", "Skilled Legs", PlayerMagicStateManager.MagicArmorSlot.Legs, 1, 0.13f, 250, 3),
            new ArmorDefinition("SkilledLegs1", "SkilledLegs1.png", "Skilled Legs I", PlayerMagicStateManager.MagicArmorSlot.Legs, 2, 0.13f, 500, 3),
            new ArmorDefinition("SkilledLegs2", "SkilledLegs2.png", "Skilled Legs II", PlayerMagicStateManager.MagicArmorSlot.Legs, 3, 0.13f, 750, 3),
            new ArmorDefinition("SkilledBoots", "SkilledBoots.png", "Skilled Boots", PlayerMagicStateManager.MagicArmorSlot.Boots, 1, 0.07f, 250, 4),
            new ArmorDefinition("SkilledBoots1", "SkilledBoots1.png", "Skilled Boots I", PlayerMagicStateManager.MagicArmorSlot.Boots, 2, 0.07f, 500, 4),
            new ArmorDefinition("SkilledBoots2", "SkilledBoots2.png", "Skilled Boots II", PlayerMagicStateManager.MagicArmorSlot.Boots, 3, 0.07f, 750, 4),
            new ArmorDefinition("SkilledShield", "SkilledShield.png", "Skilled Shield", PlayerMagicStateManager.MagicArmorSlot.Shield, 1, 0.13f, 250, 5),
            new ArmorDefinition("SkilledShield1", "SkilledShield1.png", "Skilled Shield I", PlayerMagicStateManager.MagicArmorSlot.Shield, 2, 0.13f, 500, 5),
            new ArmorDefinition("SkilledShield2", "SkilledShield2.png", "Skilled Shield II", PlayerMagicStateManager.MagicArmorSlot.Shield, 3, 0.13f, 750, 5)
        };

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AddItemTypes, GameLoader.NAMESPACE + ".Items.Magical.SkilledArmorSet.Add")]
        [ModLoader.ModCallbackDependsOn("pipliz.server.applymoditempatches")]
        public static void Add(Dictionary<string, ItemTypesServer.ItemTypeRaw> items)
        {
            foreach (var definition in Definitions)
            {
                definition.Item = AddArmorItem(items, definition);
                AssignItem(definition);
            }
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AfterItemTypesDefined, GameLoader.NAMESPACE + ".Items.Magical.SkilledArmorSet.Register")]
        [ModLoader.ModCallbackProvidesFor("pipliz.server.loadresearchables")]
        public static void Register()
        {
            foreach (var definition in Definitions)
                RegisterRecipe(definition);
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnPlayerClicked, GameLoader.NAMESPACE + ".Items.Magical.SkilledArmorSet.OnPlayerClicked")]
        public static void OnPlayerClicked(Players.Player player, PlayerClickedData playerClickData)
        {
            if (playerClickData.ClickType != PlayerClickedData.EClickType.Right)
                return;

            if (!TryGetDefinition(playerClickData.TypeSelected, out var definition))
                return;

            playerClickData.ConsumedType = PlayerClickedData.EConsumedType.UsedByMod;
            PlayerMagicStateManager.TryEquipSkilledArmor(player, definition.Slot, definition.Item.ItemIndex, definition.Tier, definition.DisplayName);
        }

        private static ItemTypesServer.ItemTypeRaw AddArmorItem(Dictionary<string, ItemTypesServer.ItemTypeRaw> items, ArmorDefinition definition)
        {
            var skillValue = definition.Tier == 1 ? 0.01f : definition.Tier == 2 ? 0.02f : 0.03f;
            var node = new JObject
            {
                ["icon"] = GameLoader.ICON_PATH + definition.IconFile,
                ["isPlaceable"] = false,
                ["maxStackSize"] = 1,
                ["ArmorRating"] = definition.ArmorRating,
                ["Durability"] = definition.Durability,
                ["Slot"] = definition.SlotIndex,
                ["Skilled"] = skillValue,
                ["Rarity"] = 0,
                    ["categories"] = new JArray("armor", "magicitem", "magic")
            };

            var name = GameLoader.NAMESPACE + "." + definition.Key;
            var item = new ItemTypesServer.ItemTypeRaw(name, node);
            items.Add(name, item);
            return item;
        }

        private static void RegisterRecipe(ArmorDefinition definition)
        {
            SkilledSword.RegisterRecipe(definition.Item.ItemIndex, GetRequirements(definition), 1);
        }

        private static List<InventoryItem> GetRequirements(ArmorDefinition definition)
        {
            if (definition.Tier == 1)
            {
                var requirements = new List<InventoryItem>
                {
                    new InventoryItem(Adamantine.Item.ItemIndex, 2),
                    new InventoryItem(Pandaros.Settlers.Items.AirStone.Item.ItemIndex, 3),
                    new InventoryItem(Pandaros.Settlers.Items.EarthStone.Item.ItemIndex, 3),
                    new InventoryItem(Pandaros.Settlers.Items.WaterStone.Item.ItemIndex, 3),
                    new InventoryItem(Pandaros.Settlers.Items.FireStone.Item.ItemIndex, 3),
                    new InventoryItem(Pandaros.Settlers.Items.Esper.Item.ItemIndex, 1),
                    new InventoryItem(Pandaros.Settlers.Items.Elementium.Item.ItemIndex, 2)
                };

                AddBaseEssence(requirements, definition.Slot);
                return requirements;
            }

            return new List<InventoryItem>
            {
                new InventoryItem(Adamantine.Item.ItemIndex, definition.Tier == 2 ? 12 : 22),
                new InventoryItem(Pandaros.Settlers.Items.AirStone.Item.ItemIndex, definition.Tier == 2 ? 13 : 23),
                new InventoryItem(Pandaros.Settlers.Items.EarthStone.Item.ItemIndex, definition.Tier == 2 ? 13 : 23),
                new InventoryItem(Pandaros.Settlers.Items.WaterStone.Item.ItemIndex, definition.Tier == 2 ? 13 : 23),
                new InventoryItem(Pandaros.Settlers.Items.FireStone.Item.ItemIndex, definition.Tier == 2 ? 13 : 23),
                new InventoryItem(Pandaros.Settlers.Items.Esper.Item.ItemIndex, definition.Tier == 2 ? 11 : 21),
                new InventoryItem(Pandaros.Settlers.Items.Elementium.Item.ItemIndex, definition.Tier == 2 ? 6 : 12)
            };
        }

        private static void AddBaseEssence(List<InventoryItem> requirements, PlayerMagicStateManager.MagicArmorSlot slot)
        {
            switch (slot)
            {
                case PlayerMagicStateManager.MagicArmorSlot.Helm:
                    requirements.Add(new InventoryItem(Pandaros.Settlers.Items.Mana.Item.ItemIndex, 8));
                    break;
                case PlayerMagicStateManager.MagicArmorSlot.Chest:
                    requirements.Add(new InventoryItem(Pandaros.Settlers.Items.Aether.Item.ItemIndex, 4));
                    break;
                case PlayerMagicStateManager.MagicArmorSlot.Gloves:
                    requirements.Add(new InventoryItem(Pandaros.Settlers.Items.Mana.Item.ItemIndex, 4));
                    break;
                case PlayerMagicStateManager.MagicArmorSlot.Legs:
                    requirements.Add(new InventoryItem(Pandaros.Settlers.Items.Aether.Item.ItemIndex, 2));
                    requirements.Add(new InventoryItem(Pandaros.Settlers.Items.Void.Item.ItemIndex, 1));
                    break;
                case PlayerMagicStateManager.MagicArmorSlot.Boots:
                    requirements.Add(new InventoryItem(Pandaros.Settlers.Items.Aether.Item.ItemIndex, 4));
                    break;
                case PlayerMagicStateManager.MagicArmorSlot.Shield:
                    requirements.Add(new InventoryItem(Pandaros.Settlers.Items.Mana.Item.ItemIndex, 4));
                    requirements.Add(new InventoryItem(Pandaros.Settlers.Items.Void.Item.ItemIndex, 1));
                    break;
            }
        }

        private static ItemTypesServer.ItemTypeRaw GetPreviousTierItem(ArmorDefinition definition)
        {
            switch (definition.Key)
            {
                case "SkilledHelm1":
                    return SkilledHelm.Item;
                case "SkilledHelm2":
                    return SkilledHelm1.Item;
                case "SkilledChest1":
                    return SkilledChest.Item;
                case "SkilledChest2":
                    return SkilledChest1.Item;
                case "SkilledGloves1":
                    return SkilledGloves.Item;
                case "SkilledGloves2":
                    return SkilledGloves1.Item;
                case "SkilledLegs1":
                    return SkilledLegs.Item;
                case "SkilledLegs2":
                    return SkilledLegs1.Item;
                case "SkilledBoots1":
                    return SkilledBoots.Item;
                case "SkilledBoots2":
                    return SkilledBoots1.Item;
                case "SkilledShield1":
                    return SkilledShield.Item;
                case "SkilledShield2":
                    return SkilledShield1.Item;
                default:
                    return definition.Item;
            }
        }

        private static bool TryGetDefinition(ushort itemIndex, out ArmorDefinition definition)
        {
            foreach (var candidate in Definitions)
            {
                if (candidate.Item != null && candidate.Item.ItemIndex == itemIndex)
                {
                    definition = candidate;
                    return true;
                }
            }

            definition = null;
            return false;
        }

        private static void AssignItem(ArmorDefinition definition)
        {
            switch (definition.Key)
            {
                case "SkilledHelm":
                    SkilledHelm.Item = definition.Item;
                    break;
                case "SkilledHelm1":
                    SkilledHelm1.Item = definition.Item;
                    break;
                case "SkilledHelm2":
                    SkilledHelm2.Item = definition.Item;
                    break;
                case "SkilledChest":
                    SkilledChest.Item = definition.Item;
                    break;
                case "SkilledChest1":
                    SkilledChest1.Item = definition.Item;
                    break;
                case "SkilledChest2":
                    SkilledChest2.Item = definition.Item;
                    break;
                case "SkilledGloves":
                    SkilledGloves.Item = definition.Item;
                    break;
                case "SkilledGloves1":
                    SkilledGloves1.Item = definition.Item;
                    break;
                case "SkilledGloves2":
                    SkilledGloves2.Item = definition.Item;
                    break;
                case "SkilledLegs":
                    SkilledLegs.Item = definition.Item;
                    break;
                case "SkilledLegs1":
                    SkilledLegs1.Item = definition.Item;
                    break;
                case "SkilledLegs2":
                    SkilledLegs2.Item = definition.Item;
                    break;
                case "SkilledBoots":
                    SkilledBoots.Item = definition.Item;
                    break;
                case "SkilledBoots1":
                    SkilledBoots1.Item = definition.Item;
                    break;
                case "SkilledBoots2":
                    SkilledBoots2.Item = definition.Item;
                    break;
                case "SkilledShield":
                    SkilledShield.Item = definition.Item;
                    break;
                case "SkilledShield1":
                    SkilledShield1.Item = definition.Item;
                    break;
                case "SkilledShield2":
                    SkilledShield2.Item = definition.Item;
                    break;
            }
        }
    }
}
