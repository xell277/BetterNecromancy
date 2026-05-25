using Monsters;
using Newtonsoft.Json.Linq;
using NPC;
using Pandaros.API.Entities;
using Recipes;
using Shared;
using System.Collections.Generic;

namespace Pandaros.Settlers.Items.Magical
{
    internal static class MagicUpgradeItems
    {
        public static ItemTypesServer.ItemTypeRaw AddSimpleItem(
            Dictionary<string, ItemTypesServer.ItemTypeRaw> items,
            string key,
            string iconFile,
            params string[] categories)
        {
            return AddSimpleItem(items, key, iconFile, 1, categories);
        }

        public static ItemTypesServer.ItemTypeRaw AddSimpleItem(
            Dictionary<string, ItemTypesServer.ItemTypeRaw> items,
            string key,
            string iconFile,
            int maxStackSize,
            params string[] categories)
        {
            var name = GameLoader.NAMESPACE + "." + key;
            var node = new JObject
            {
                ["icon"] = GameLoader.ICON_PATH + iconFile,
                ["isPlaceable"] = false,
                ["maxStackSize"] = maxStackSize,
                ["Rarity"] = 0,
                ["categories"] = new JArray(categories)
            };

            var item = new ItemTypesServer.ItemTypeRaw(name, node);
            items.Add(name, item);
            return item;
        }
    }

    [ModLoader.ModManager]
    public static class Adamantine
    {
        public static ItemTypesServer.ItemTypeRaw Item { get; private set; }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AddItemTypes, GameLoader.NAMESPACE + ".Items.Magical.Adamantine.Add")]
        [ModLoader.ModCallbackDependsOn("pipliz.server.applymoditempatches")]
        public static void Add(Dictionary<string, ItemTypesServer.ItemTypeRaw> items)
        {
            var node = new JObject
            {
                ["icon"] = GameLoader.ICON_PATH + "Adamantine.png",
                ["isPlaceable"] = false,
                ["maxStackSize"] = 600,
                ["categories"] = new JArray("ingredient", "magic", "adamantine")
            };

            Item = new ItemTypesServer.ItemTypeRaw(GameLoader.NAMESPACE + ".Adamantine", node);
            items.Add(Item.name, Item);
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AfterItemTypesDefined, GameLoader.NAMESPACE + ".Items.Magical.Adamantine.Register")]
        [ModLoader.ModCallbackProvidesFor("pipliz.server.loadresearchables")]
        public static void Register()
        {
            var recipe = new Recipe(
                Item.name,
                new List<InventoryItem>
                {
                    new InventoryItem(Pandaros.Settlers.Items.Elementium.Item.ItemIndex, 2),
                    new InventoryItem("steelingot", 2),
                    new InventoryItem("goldingot", 1),
                    new InventoryItem("brassingot", 1),
                    new InventoryItem("firewood", 2)
                },
                new List<RecipeResult>
                {
                    new RecipeResult(Item.ItemIndex, 1)
                },
                20);

            ServerManager.RecipeStorage.AddLimitTypeRecipe(NPCType.GetByKeyNameOrDefault(VanillaJobs.Smelter), recipe);
        }
    }

    [ModLoader.ModManager]
    public static class HealthBooster
    {
        public static ItemTypesServer.ItemTypeRaw Item { get; private set; }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AddItemTypes, GameLoader.NAMESPACE + ".Items.Magical.HealthBooster.Add")]
        [ModLoader.ModCallbackDependsOn("pipliz.server.applymoditempatches")]
        public static void Add(Dictionary<string, ItemTypesServer.ItemTypeRaw> items)
        {
            Item = MagicUpgradeItems.AddSimpleItem(items, "HealthBooster", "HealthBooster.png", "magicitem", "magic");
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AfterItemTypesDefined, GameLoader.NAMESPACE + ".Items.Magical.HealthBooster.Register")]
        [ModLoader.ModCallbackProvidesFor("pipliz.server.loadresearchables")]
        public static void Register()
        {
            var recipe = new Recipe(
                Item.name,
                new List<InventoryItem>
                {
                    new InventoryItem(Adamantine.Item.ItemIndex, 10),
                    new InventoryItem(Pandaros.Settlers.Items.Elementium.Item.ItemIndex, 6),
                    new InventoryItem(Pandaros.Settlers.Items.Mana.Item.ItemIndex, 20),
                    new InventoryItem("bookofknowledge", 5)
                },
                new List<RecipeResult>
                {
                    new RecipeResult(Item.ItemIndex, 1)
                },
                1);

            ServerManager.RecipeStorage.AddLimitTypeRecipe(NPCType.GetByKeyNameOrDefault(VanillaJobs.Alchemist), recipe);
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnPlayerClicked, GameLoader.NAMESPACE + ".Items.Magical.HealthBooster.OnPlayerClicked")]
        public static void OnPlayerClicked(Players.Player player, PlayerClickedData playerClickData)
        {
            if (playerClickData.ClickType != PlayerClickedData.EClickType.Right ||
                playerClickData.TypeSelected != Item.ItemIndex)
                return;

            playerClickData.ConsumedType = PlayerClickedData.EConsumedType.UsedByMod;
            var equipped = PlayerMagicStateManager.TryEquipHealthBooster(player);

            if (equipped)
                PlayerMagicStateManager.ApplyHealthBoosterEquipFeedback(player);
        }
    }

    [ModLoader.ModManager]
    public static class ManaCrystal
    {
        public static ItemTypesServer.ItemTypeRaw Item { get; private set; }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AddItemTypes, GameLoader.NAMESPACE + ".Items.Magical.ManaCrystal.Add")]
        [ModLoader.ModCallbackDependsOn("pipliz.server.applymoditempatches")]
        public static void Add(Dictionary<string, ItemTypesServer.ItemTypeRaw> items)
        {
            Item = MagicUpgradeItems.AddSimpleItem(items, "ManaCrystal", "rawsapphire.png", 20, "magicitem", "magic", "mana", "relic");
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnPlayerClicked, GameLoader.NAMESPACE + ".Items.Magical.ManaCrystal.OnPlayerClicked")]
        public static void OnPlayerClicked(Players.Player player, PlayerClickedData playerClickData)
        {
            if (playerClickData.ClickType != PlayerClickedData.EClickType.Right ||
                playerClickData.TypeSelected != Item.ItemIndex)
            {
                return;
            }

            playerClickData.ConsumedType = PlayerClickedData.EConsumedType.UsedByMod;

            if (!PlayerMagicStateManager.TryConsumeManaCrystal(player))
                PlayerMagicStateManager.ShowMissingItemIndicator(player, Item.ItemIndex);
        }
    }

    [ModLoader.ModManager]
    public static class ManaFlightHarness
    {
        public static ItemTypesServer.ItemTypeRaw Item { get; private set; }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AddItemTypes, GameLoader.NAMESPACE + ".Items.Magical.ManaFlightHarness.Add")]
        [ModLoader.ModCallbackDependsOn("pipliz.server.applymoditempatches")]
        public static void Add(Dictionary<string, ItemTypesServer.ItemTypeRaw> items)
        {
            Item = MagicUpgradeItems.AddSimpleItem(items, "ManaFlightHarness", "manaHarness.png", "magicitem", "magic", "flight", "relic");
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AfterItemTypesDefined, GameLoader.NAMESPACE + ".Items.Magical.ManaFlightHarness.Register")]
        [ModLoader.ModCallbackProvidesFor("pipliz.server.loadresearchables")]
        public static void Register()
        {
            var recipe = new Recipe(
                Item.name,
                new List<InventoryItem>
                {
                    new InventoryItem(Adamantine.Item.ItemIndex, 6),
                    new InventoryItem(Pandaros.Settlers.Items.Elementium.Item.ItemIndex, 8),
                    new InventoryItem(Pandaros.Settlers.Items.AirStone.Item.ItemIndex, 12),
                    new InventoryItem(Pandaros.Settlers.Items.WaterStone.Item.ItemIndex, 8),
                    new InventoryItem(Pandaros.Settlers.Items.Esper.Item.ItemIndex, 6),
                    new InventoryItem(Pandaros.Settlers.Items.Mana.Item.ItemIndex, 20),
                    new InventoryItem(ManaCrystal.Item.ItemIndex, 1)
                },
                new List<RecipeResult>
                {
                    new RecipeResult(Item.ItemIndex, 1)
                },
                1);

            ServerManager.RecipeStorage.AddLimitTypeRecipe(NPCType.GetByKeyNameOrDefault(VanillaJobs.Alchemist), recipe);
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnPlayerClicked, GameLoader.NAMESPACE + ".Items.Magical.ManaFlightHarness.OnPlayerClicked")]
        public static void OnPlayerClicked(Players.Player player, PlayerClickedData playerClickData)
        {
            if (playerClickData.ClickType != PlayerClickedData.EClickType.Right ||
                playerClickData.TypeSelected != Item.ItemIndex)
            {
                return;
            }

            playerClickData.ConsumedType = PlayerClickedData.EConsumedType.UsedByMod;

            if (!PlayerMagicStateManager.TryUnlockManaFlight(player, Item.ItemIndex))
                PlayerMagicStateManager.ShowMissingItemIndicator(player, Item.ItemIndex);
        }
    }

    [ModLoader.ModManager]
    public static class ArcaneFocus
    {
        public static ItemTypesServer.ItemTypeRaw Item { get; private set; }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AddItemTypes, GameLoader.NAMESPACE + ".Items.Magical.ArcaneFocus.Add")]
        [ModLoader.ModCallbackDependsOn("pipliz.server.applymoditempatches")]
        public static void Add(Dictionary<string, ItemTypesServer.ItemTypeRaw> items)
        {
            Item = MagicUpgradeItems.AddSimpleItem(items, "ArcaneFocus", "rawemerald.png", "magicitem", "magic", "focus");
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AfterItemTypesDefined, GameLoader.NAMESPACE + ".Items.Magical.ArcaneFocus.Register")]
        [ModLoader.ModCallbackProvidesFor("pipliz.server.loadresearchables")]
        public static void Register()
        {
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnPlayerClicked, GameLoader.NAMESPACE + ".Items.Magical.ArcaneFocus.OnPlayerClicked")]
        public static void OnPlayerClicked(Players.Player player, PlayerClickedData playerClickData)
        {
            Toggle(player, playerClickData, Item.ItemIndex, 1);
        }

        internal static void Toggle(Players.Player player, PlayerClickedData playerClickData, ushort itemIndex, int tier)
        {
            if (playerClickData.ClickType != PlayerClickedData.EClickType.Right ||
                playerClickData.TypeSelected != itemIndex)
                return;

            playerClickData.ConsumedType = PlayerClickedData.EConsumedType.UsedByMod;
            PlayerMagicStateManager.TryEquipArcaneFocus(player, itemIndex, tier);
        }
    }

    [ModLoader.ModManager]
    public static class ArcaneFocus1
    {
        public static ItemTypesServer.ItemTypeRaw Item { get; private set; }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AddItemTypes, GameLoader.NAMESPACE + ".Items.Magical.ArcaneFocus1.Add")]
        [ModLoader.ModCallbackDependsOn("pipliz.server.applymoditempatches")]
        public static void Add(Dictionary<string, ItemTypesServer.ItemTypeRaw> items)
        {
            Item = MagicUpgradeItems.AddSimpleItem(items, "ArcaneFocus1", "refinedemerald.png", "magicitem", "magic", "focus");
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AfterItemTypesDefined, GameLoader.NAMESPACE + ".Items.Magical.ArcaneFocus1.Register")]
        [ModLoader.ModCallbackProvidesFor("pipliz.server.loadresearchables")]
        public static void Register()
        {
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnPlayerClicked, GameLoader.NAMESPACE + ".Items.Magical.ArcaneFocus1.OnPlayerClicked")]
        public static void OnPlayerClicked(Players.Player player, PlayerClickedData playerClickData)
        {
            ArcaneFocus.Toggle(player, playerClickData, Item.ItemIndex, 2);
        }
    }

    [ModLoader.ModManager]
    public static class ArcaneFocus2
    {
        public static ItemTypesServer.ItemTypeRaw Item { get; private set; }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AddItemTypes, GameLoader.NAMESPACE + ".Items.Magical.ArcaneFocus2.Add")]
        [ModLoader.ModCallbackDependsOn("pipliz.server.applymoditempatches")]
        public static void Add(Dictionary<string, ItemTypesServer.ItemTypeRaw> items)
        {
            Item = MagicUpgradeItems.AddSimpleItem(items, "ArcaneFocus2", "refinedsapphire.png", "magicitem", "magic", "focus");
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AfterItemTypesDefined, GameLoader.NAMESPACE + ".Items.Magical.ArcaneFocus2.Register")]
        [ModLoader.ModCallbackProvidesFor("pipliz.server.loadresearchables")]
        public static void Register()
        {
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnPlayerClicked, GameLoader.NAMESPACE + ".Items.Magical.ArcaneFocus2.OnPlayerClicked")]
        public static void OnPlayerClicked(Players.Player player, PlayerClickedData playerClickData)
        {
            ArcaneFocus.Toggle(player, playerClickData, Item.ItemIndex, 3);
        }
    }

    [ModLoader.ModManager]
    public static class SkilledSword
    {
        public static ItemTypesServer.ItemTypeRaw Item { get; private set; }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AddItemTypes, GameLoader.NAMESPACE + ".Items.Magical.SkilledSword.Add")]
        [ModLoader.ModCallbackDependsOn("pipliz.server.applymoditempatches")]
        public static void Add(Dictionary<string, ItemTypesServer.ItemTypeRaw> items)
        {
            Item = MagicUpgradeItems.AddSimpleItem(items, "SkilledSword", "SkilledSword.png", "weapon", "magicitem", "magic");
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AfterItemTypesDefined, GameLoader.NAMESPACE + ".Items.Magical.SkilledSword.Register")]
        [ModLoader.ModCallbackProvidesFor("pipliz.server.loadresearchables")]
        public static void Register()
        {
            RegisterRecipe(
                Item.ItemIndex,
                new List<InventoryItem>
                {
                    new InventoryItem(Adamantine.Item.ItemIndex, 2),
                    new InventoryItem(Pandaros.Settlers.Items.AirStone.Item.ItemIndex, 3),
                    new InventoryItem(Pandaros.Settlers.Items.EarthStone.Item.ItemIndex, 3),
                    new InventoryItem(Pandaros.Settlers.Items.WaterStone.Item.ItemIndex, 3),
                    new InventoryItem(Pandaros.Settlers.Items.FireStone.Item.ItemIndex, 3),
                    new InventoryItem(Pandaros.Settlers.Items.Esper.Item.ItemIndex, 1),
                    new InventoryItem(Pandaros.Settlers.Items.MagicWand.Item.ItemIndex, 1)
                },
                1);
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnPlayerClicked, GameLoader.NAMESPACE + ".Items.Magical.SkilledSword.OnPlayerClicked")]
        public static void OnPlayerClicked(Players.Player player, PlayerClickedData playerClickData)
        {
            Toggle(player, playerClickData, Item.ItemIndex, 1);
        }

        internal static void RegisterRecipe(ushort resultItemIndex, List<InventoryItem> requirements, int defaultLimit)
        {
            var recipe = new Recipe(
                ItemTypes.GetType(resultItemIndex).Name,
                requirements,
                new List<RecipeResult> { new RecipeResult(resultItemIndex, 1) },
                defaultLimit);

            ServerManager.RecipeStorage.AddLimitTypeRecipe(NPCType.GetByKeyNameOrDefault(VanillaJobs.Alchemist), recipe);
        }

        internal static void Toggle(Players.Player player, PlayerClickedData playerClickData, ushort itemIndex, int tier)
        {
            if (playerClickData.ClickType != PlayerClickedData.EClickType.Right ||
                playerClickData.TypeSelected != itemIndex)
                return;

            playerClickData.ConsumedType = PlayerClickedData.EConsumedType.UsedByMod;
            PlayerMagicStateManager.TryEquipSkilledSword(player, itemIndex, tier);
        }
    }

    [ModLoader.ModManager]
    public static class SkilledSword1
    {
        public static ItemTypesServer.ItemTypeRaw Item { get; private set; }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AddItemTypes, GameLoader.NAMESPACE + ".Items.Magical.SkilledSword1.Add")]
        [ModLoader.ModCallbackDependsOn("pipliz.server.applymoditempatches")]
        public static void Add(Dictionary<string, ItemTypesServer.ItemTypeRaw> items)
        {
            Item = MagicUpgradeItems.AddSimpleItem(items, "SkilledSword1", "SkilledSword1.png", "weapon", "magicitem", "magic");
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AfterItemTypesDefined, GameLoader.NAMESPACE + ".Items.Magical.SkilledSword1.Register")]
        [ModLoader.ModCallbackProvidesFor("pipliz.server.loadresearchables")]
        public static void Register()
        {
            SkilledSword.RegisterRecipe(
                Item.ItemIndex,
                new List<InventoryItem>
                {
                new InventoryItem(Adamantine.Item.ItemIndex, 12),
                new InventoryItem(Pandaros.Settlers.Items.AirStone.Item.ItemIndex, 13),
                new InventoryItem(Pandaros.Settlers.Items.EarthStone.Item.ItemIndex, 13),
                new InventoryItem(Pandaros.Settlers.Items.WaterStone.Item.ItemIndex, 13),
                new InventoryItem(Pandaros.Settlers.Items.FireStone.Item.ItemIndex, 13),
                new InventoryItem(Pandaros.Settlers.Items.Esper.Item.ItemIndex, 11),
                new InventoryItem(Pandaros.Settlers.Items.Elementium.Item.ItemIndex, 6),
                new InventoryItem(Pandaros.Settlers.Items.Aether.Item.ItemIndex, 4)
                },
                1);
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnPlayerClicked, GameLoader.NAMESPACE + ".Items.Magical.SkilledSword1.OnPlayerClicked")]
        public static void OnPlayerClicked(Players.Player player, PlayerClickedData playerClickData)
        {
            SkilledSword.Toggle(player, playerClickData, Item.ItemIndex, 2);
        }
    }

    [ModLoader.ModManager]
    public static class SkilledSword2
    {
        public static ItemTypesServer.ItemTypeRaw Item { get; private set; }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AddItemTypes, GameLoader.NAMESPACE + ".Items.Magical.SkilledSword2.Add")]
        [ModLoader.ModCallbackDependsOn("pipliz.server.applymoditempatches")]
        public static void Add(Dictionary<string, ItemTypesServer.ItemTypeRaw> items)
        {
            Item = MagicUpgradeItems.AddSimpleItem(items, "SkilledSword2", "SkilledSword2.png", "weapon", "magicitem", "magic");
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AfterItemTypesDefined, GameLoader.NAMESPACE + ".Items.Magical.SkilledSword2.Register")]
        [ModLoader.ModCallbackProvidesFor("pipliz.server.loadresearchables")]
        public static void Register()
        {
            SkilledSword.RegisterRecipe(
                Item.ItemIndex,
                new List<InventoryItem>
                {
                new InventoryItem(Adamantine.Item.ItemIndex, 22),
                new InventoryItem(Pandaros.Settlers.Items.AirStone.Item.ItemIndex, 23),
                new InventoryItem(Pandaros.Settlers.Items.EarthStone.Item.ItemIndex, 23),
                new InventoryItem(Pandaros.Settlers.Items.WaterStone.Item.ItemIndex, 23),
                new InventoryItem(Pandaros.Settlers.Items.FireStone.Item.ItemIndex, 23),
                new InventoryItem(Pandaros.Settlers.Items.Esper.Item.ItemIndex, 21),
                new InventoryItem(Pandaros.Settlers.Items.Elementium.Item.ItemIndex, 12),
                new InventoryItem(Pandaros.Settlers.Items.Aether.Item.ItemIndex, 6),
                new InventoryItem(Pandaros.Settlers.Items.Void.Item.ItemIndex, 4)
                },
                1);
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnPlayerClicked, GameLoader.NAMESPACE + ".Items.Magical.SkilledSword2.OnPlayerClicked")]
        public static void OnPlayerClicked(Players.Player player, PlayerClickedData playerClickData)
        {
            SkilledSword.Toggle(player, playerClickData, Item.ItemIndex, 3);
        }
    }

    [ModLoader.ModManager]
    public static class SkilledSwordEffects
    {
        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnNPCHit, GameLoader.NAMESPACE + ".Items.Magical.SkilledSwordEffects.OnNPCHit")]
        public static void OnNPCHit(NPCBase npc, ModLoader.OnHitData hitData)
        {
            ApplyBonus(hitData);
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnMonsterHit, GameLoader.NAMESPACE + ".Items.Magical.SkilledSwordEffects.OnMonsterHit")]
        public static void OnMonsterHit(IMonster monster, ModLoader.OnHitData hitData)
        {
            ApplyBonus(hitData);
        }

        private static void ApplyBonus(ModLoader.OnHitData hitData)
        {
            if (hitData.HitSourceType != ModLoader.OnHitData.EHitSourceType.PlayerClick)
                return;

            if (!(hitData.HitSourceObject is Players.Player player))
                return;

            hitData.ResultDamage += PlayerMagicStateManager.GetPlayerClickDamageBonus(player);
            hitData.ResultDamage += PlayerMagicStateManager.GetSkilledSwordDamageBonus(player);

            var critChance = PlayerMagicStateManager.GetPlayerClickCritChance(player);
            if (critChance > 0f && Pipliz.Random.NextFloat() <= critChance)
                hitData.ResultDamage += PlayerMagicStateManager.GetPlayerClickCritBonus(player);
        }
    }
}
