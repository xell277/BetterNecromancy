using NPC;
using Newtonsoft.Json.Linq;
using BetterNecromancy;
using Pandaros.API.Entities;
using Pipliz;
using Recipes;
using Shared;
using System.Collections.Generic;
using UnityEngine;

namespace Pandaros.Settlers.Items.Healing
{
    [ModLoader.ModManager]
    public static class TreatedBandage
    {
        public const long Cooldown = 5000;
        public const int DurationSeconds = 5;
        public const float InitialHeal = 50f;
        public const float TotalHealOverTime = 70f;

        private static readonly Dictionary<Players.Player, long> Cooldowns = new Dictionary<Players.Player, long>();

        public static ItemTypesServer.ItemTypeRaw Item { get; private set; }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AfterItemTypesDefined, GameLoader.NAMESPACE + ".Items.Healing.TreatedBandage.Register")]
        [ModLoader.ModCallbackProvidesFor("pipliz.server.loadresearchables")]
        public static void Register()
        {
            var bandage = new InventoryItem(Bandage.Item.ItemIndex, 1);
            var antibiotics = new InventoryItem(Anitbiotic.Item.ItemIndex, 1);

            var recipe = new Recipe(
                Item.name,
                new List<InventoryItem> { antibiotics, bandage },
                new List<RecipeResult> { new RecipeResult(Item.ItemIndex, 1) },
                15);

            ServerManager.RecipeStorage.AddLimitTypeRecipe(NPCType.GetByKeyNameOrDefault(VanillaJobs.Alchemist), recipe);
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AddItemTypes, GameLoader.NAMESPACE + ".Items.Healing.TreatedBandage.Add")]
        [ModLoader.ModCallbackDependsOn("pipliz.server.applymoditempatches")]
        public static void Add(Dictionary<string, ItemTypesServer.ItemTypeRaw> items)
        {
            var name = GameLoader.NAMESPACE + ".TreatedBandage";
            var node = new JObject
            {
                ["icon"] = GameLoader.ICON_PATH + "TreatedBandage.png",
                ["isPlaceable"] = false,
                ["categories"] = new JArray("medicine")
            };

            Item = new ItemTypesServer.ItemTypeRaw(name, node);
            items.Add(name, Item);
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnPlayerClicked, GameLoader.NAMESPACE + ".Items.Healing.TreatedBandage.Click")]
        public static void Click(Players.Player player, PlayerClickedData playerClickData)
        {
            if (playerClickData.TypeSelected != Item.ItemIndex)
                return;

            if (!Cooldowns.ContainsKey(player))
                Cooldowns.Add(player, 0);

            if (Pipliz.Time.MillisecondsSinceStart <= Cooldowns[player])
                return;

            var healed = false;
            var effectOrigin = player.PositionStanding + Vector3.up * 0.9f;

            if (playerClickData.ClickType == PlayerClickedData.EClickType.Right)
            {
                _ = new HealingOverTimePC(player, InitialHeal, TotalHealOverTime, DurationSeconds);
                healed = true;
            }
            else if (playerClickData.ClickType == PlayerClickedData.EClickType.Left &&
                     playerClickData.HitType == PlayerClickedData.EHitType.NPC &&
                     NPCTracker.TryGetNPC(playerClickData.GetNPCHit().NPCID, out var npc) &&
                     !HealingOverTimeNPC.NPCIsBeingHealed(npc))
            {
                _ = new HealingOverTimeNPC(npc, InitialHeal, TotalHealOverTime, DurationSeconds, Item.ItemIndex);
                effectOrigin = npc.Position.Vector + Vector3.up * 0.9f;
                healed = true;
            }

            if (!healed)
                return;

            Cooldowns[player] = Pipliz.Time.MillisecondsSinceStart + Cooldown;
            playerClickData.ConsumedType = PlayerClickedData.EConsumedType.UsedByMod;
            player.Inventory.TryRemove(Item.ItemIndex);
            ItemUseAudioManager.Play(player, ItemUseAudioManager.TreatedBandageUse, effectOrigin);
        }
    }
}
