using NPC;
using Newtonsoft.Json.Linq;
using Recipes;
using System.Collections.Generic;

namespace Pandaros.Settlers.Items.Healing
{
    [ModLoader.ModManager]
    public static class Anitbiotic
    {
        public static ItemTypesServer.ItemTypeRaw Item { get; private set; }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AfterItemTypesDefined, GameLoader.NAMESPACE + ".Items.Healing.Anitbiotic.Register")]
        [ModLoader.ModCallbackProvidesFor("pipliz.server.loadresearchables")]
        public static void Register()
        {
            var herbs = new InventoryItem(GameLoader.NAMESPACE + ".Herbs", 2);
            var water = new InventoryItem("potwater", 1);

            var recipe = new Recipe(
                Item.name,
                new List<InventoryItem> { water, herbs },
                new List<RecipeResult> { new RecipeResult(Item.ItemIndex, 1) },
                20);

            ServerManager.RecipeStorage.AddLimitTypeRecipe(NPCType.GetByKeyNameOrDefault(VanillaJobs.Alchemist), recipe);
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AddItemTypes, GameLoader.NAMESPACE + ".Items.Healing.Anitbiotic.Add")]
        [ModLoader.ModCallbackDependsOn("pipliz.server.applymoditempatches")]
        public static void Add(Dictionary<string, ItemTypesServer.ItemTypeRaw> items)
        {
            var name = GameLoader.NAMESPACE + ".Anitbiotic";
            var node = new JObject
            {
                ["icon"] = GameLoader.ICON_PATH + "Anitbiotic.png",
                ["isPlaceable"] = false,
                ["categories"] = new JArray("medicine")
            };

            Item = new ItemTypesServer.ItemTypeRaw(name, node);
            items.Add(name, Item);
        }
    }
}
