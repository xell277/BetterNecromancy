using Newtonsoft.Json.Linq;
using NPC;
using Pandaros.API.Entities;
using Recipes;
using Shared;
using System.Collections.Generic;

namespace Pandaros.Settlers.Items.Armor.Magical
{
    [ModLoader.ModManager]
    public static class BootsOfFalling
    {
        public static readonly string Name = GameLoader.NAMESPACE + ".BootsOfFalling";

        public static ItemTypesServer.ItemTypeRaw Item { get; private set; }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AddItemTypes, GameLoader.NAMESPACE + ".Items.Armor.Magical.BootsOfFalling.Add")]
        [ModLoader.ModCallbackDependsOn("pipliz.server.applymoditempatches")]
        public static void Add(Dictionary<string, ItemTypesServer.ItemTypeRaw> items)
        {
            var node = new JObject
            {
                ["icon"] = GameLoader.ICON_PATH + "BootsOfFalling.png",
                ["isPlaceable"] = false,
                ["categories"] = new JArray("armor", "magic", "boots")
            };

            Item = new ItemTypesServer.ItemTypeRaw(Name, node);
            items.Add(Name, Item);
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AfterItemTypesDefined, GameLoader.NAMESPACE + ".Items.Armor.Magical.BootsOfFalling.Register")]
        [ModLoader.ModCallbackProvidesFor("pipliz.server.loadresearchables")]
        public static void Register()
        {
            var recipe = new Recipe(
                Item.name,
                new List<InventoryItem>
                {
                    new InventoryItem(Pandaros.Settlers.Items.Elementium.Item.ItemIndex, 10),
                    new InventoryItem(Pandaros.Settlers.Items.AirStone.Item.ItemIndex, 10),
                    new InventoryItem(Pandaros.Settlers.Items.EarthStone.Item.ItemIndex, 10),
                    new InventoryItem(Pandaros.Settlers.Items.FireStone.Item.ItemIndex, 10),
                    new InventoryItem(Pandaros.Settlers.Items.WaterStone.Item.ItemIndex, 10),
                    new InventoryItem(Pandaros.Settlers.Items.Esper.Item.ItemIndex, 1),
                    new InventoryItem(Pandaros.Settlers.Items.Mana.Item.ItemIndex, 20),
                    new InventoryItem("linen", 10),
                    new InventoryItem("leather", 4)
                },
                new List<RecipeResult>
                {
                    new RecipeResult(Item.ItemIndex, 1)
                },
                1);

            ServerManager.RecipeStorage.AddLimitTypeRecipe(NPCType.GetByKeyNameOrDefault(VanillaJobs.Alchemist), recipe);
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnPlayerClicked, GameLoader.NAMESPACE + ".Items.Armor.Magical.BootsOfFalling.OnPlayerClicked")]
        public static void OnPlayerClicked(Players.Player player, PlayerClickedData playerClickData)
        {
            if (playerClickData.ClickType != PlayerClickedData.EClickType.Right ||
                playerClickData.TypeSelected != Item.ItemIndex)
                return;

            playerClickData.ConsumedType = PlayerClickedData.EConsumedType.UsedByMod;
            PlayerMagicStateManager.TryEquipBootsOfFalling(player);
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnPlayerHit, GameLoader.NAMESPACE + ".Items.Armor.Magical.BootsOfFalling.OnPlayerHit")]
        public static void OnPlayerHit(Players.Player player, ModLoader.OnHitData hitData)
        {
            if (!PlayerMagicStateManager.IsBootsOfFallingEquipped(player))
                return;

            if (hitData.HitSourceType == ModLoader.OnHitData.EHitSourceType.FallDamage ||
                (hitData.HitSourceType == ModLoader.OnHitData.EHitSourceType.None &&
                 hitData.HitSourceObject == null))
            {
                hitData.ResultDamage = 0f;
            }
        }
    }
}
