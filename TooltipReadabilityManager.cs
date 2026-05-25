using System.Collections.Generic;
using BetterNecromancy;
using colonyserver.Assets.UIGeneration;
using colonyshared.NetworkUI;
using colonyshared.NetworkUI.UIGeneration;
using NetworkUI;
using NetworkUI.Items;
using Pandaros.API.Entities;
using Shared;

namespace BetterNecromancy
{
    [ModLoader.ModManager]
    public static class TooltipReadabilityManager
    {
        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnConstructTooltipUI, ModEntry.Namespace + ".TooltipReadabilityManager.OnConstructTooltipUI")]
        public static void OnConstructTooltipUI(Players.Player player, ConstructTooltipUIData data)
        {
            if (data == null ||
                data.hoverType != ETooltipHoverType.Item ||
                data.menu == null)
            {
                return;
            }

            if (PlayerMagicStateManager.TryGetLineageTooltip(player, data.hoverItem, out var wandLines))
            {
                ReplaceTooltip(data, wandLines);
                return;
            }

            if (TryGetSimpleItemText(data.hoverItem, out var simpleLines))
                ReplaceTooltip(data, simpleLines);
        }

        private static void ReplaceTooltip(ConstructTooltipUIData data, List<string> lines)
        {
            data.menu.Items.Clear();
            for (var i = 0; i < lines.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(lines[i]))
                    data.menu.Items.Add(new Label(new LabelData(lines[i])));
            }
        }

        private static bool TryGetSimpleItemText(ushort itemIndex, out List<string> lines)
        {
            lines = null;
            if (itemIndex == 0)
                return false;

            if (Matches(Pandaros.Settlers.Items.Mana.Item, itemIndex, "Mana", "Raw spell fuel."))
                return Build("Mana", "Raw spell fuel.", out lines);
            if (Matches(Pandaros.Settlers.Items.Esper.Item, itemIndex, "Esper", "Condensed soul energy."))
                return Build("Esper", "Condensed soul energy.", out lines);
            if (Matches(Pandaros.Settlers.Items.Aether.Item, itemIndex, "Aether", "Arcane reagent."))
                return Build("Aether", "Arcane reagent.", out lines);
            if (Matches(Pandaros.Settlers.Items.Elementium.Item, itemIndex, "Elementium", "Advanced magic metal."))
                return Build("Elementium", "Advanced magic metal.", out lines);
            if (Matches(Pandaros.Settlers.Items.Void.Item, itemIndex, "Void", "Dark ritual reagent."))
                return Build("Void", "Dark ritual reagent.", out lines);
            if (Matches(Pandaros.Settlers.Items.AirStone.Item, itemIndex, "Air Stone", "Wind-aspected catalyst."))
                return Build("Air Stone", "Wind-aspected catalyst.", out lines);
            if (Matches(Pandaros.Settlers.Items.EarthStone.Item, itemIndex, "Earth Stone", "Earth-aspected catalyst."))
                return Build("Earth Stone", "Earth-aspected catalyst.", out lines);
            if (Matches(Pandaros.Settlers.Items.FireStone.Item, itemIndex, "Fire Stone", "Fire-aspected catalyst."))
                return Build("Fire Stone", "Fire-aspected catalyst.", out lines);
            if (Matches(Pandaros.Settlers.Items.WaterStone.Item, itemIndex, "Water Stone", "Water-aspected catalyst."))
                return Build("Water Stone", "Water-aspected catalyst.", out lines);
            if (Matches(Pandaros.Settlers.Items.Magical.ManaCrystal.Item, itemIndex, "Mana Crystal", "Right-click: +2 max mana and faster regen."))
                return Build("Mana Crystal", "Right-click: +2 max mana and faster regen.", out lines);
            if (Matches(Pandaros.Settlers.Items.Magical.HealthBooster.Item, itemIndex, "Health Booster", "Right-click: permanent health regeneration."))
                return Build("Health Booster", "Right-click: permanent health regeneration.", out lines);
            if (Matches(Pandaros.Settlers.Items.Magical.ManaFlightHarness.Item, itemIndex, "Mana Flight Harness", "Right-click to unlock Mana Flight. Press F to fly."))
                return Build("Mana Flight Harness", "Right-click to unlock Mana Flight. Press F to fly.", out lines);
            if (Matches(Pandaros.Settlers.Items.Healing.Bandage.Item, itemIndex, "Bandage", "Right-click: short healing over time."))
                return Build("Bandage", "Right-click: short healing over time.", out lines);
            if (Matches(Pandaros.Settlers.Items.Healing.TreatedBandage.Item, itemIndex, "Medicated Bandage", "Right-click: stronger healing."))
                return Build("Medicated Bandage", "Right-click: stronger healing.", out lines);
            if (Matches(Pandaros.Settlers.Items.Healing.Anitbiotic.Item, itemIndex, "Antibiotic", "Right-click: emergency medicine."))
                return Build("Antibiotic", "Right-click: emergency medicine.", out lines);

            return false;
        }

        private static bool Matches(ItemTypesServer.ItemTypeRaw item, ushort itemIndex, string name, string text)
        {
            return item != null && item.ItemIndex == itemIndex;
        }

        private static bool Build(string name, string text, out List<string> lines)
        {
            lines = new List<string> { name, text };
            return true;
        }
    }
}
