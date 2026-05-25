using System.Collections.Generic;
using Chatting;

[ChatCommandAutoLoader]
public class DecoGuideCommand : IChatCommand
{
    public bool TryDoCommand(Players.Player player, string chatItem, List<string> splits)
    {
        if (splits == null || splits.Count == 0 || splits[0] != "/bndeco")
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
            case "props":
                SendProps(player);
                return true;

            case "qa":
                SendQa(player);
                return true;

            case "crafting":
                SendCrafting(player);
                return true;

            default:
                SendOverview(player);
                return true;
        }
    }

    private static void SendOverview(Players.Player player)
    {
        Chat.Send(player, "/bndeco props | /bndeco crafting | /bndeco qa");
        Chat.Send(player, "Decor focus: architecture, nature, garden, workshop and magic props.");
    }

    private static void SendProps(Players.Player player)
    {
        Chat.Send(player, "Architecture: Stone Column, Stone Column Base, Marble Spiral, Marble Spiral Top.");
        Chat.Send(player, "Magic decor: Arcane Pad, Teleport Pad, Lamp, Golden Lamp.");
        Chat.Send(player, "Windows and frames: Window, Double Window, Small Window Bottom/Middle/Top, Wood Frame.");
        Chat.Send(player, "Garden and farm: Aloe Plant, Bluebonnet, Decorative Cow, Garden Bench Left/Right.");
        Chat.Send(player, "Workshop and storage: Basket, Wood Crate, Crate Stack, Herbalist Bench, Physician Bench, Machinist Bench.");
    }

    private static void SendCrafting(Players.Player player)
    {
        Chat.Send(player, "Crafter: windows, frames, benches, basket, crate, decorative cow, plants and most simple props.");
        Chat.Send(player, "Stonemason: marble/stone architecture props like columns, spirals and decorative stone pieces.");
        Chat.Send(player, "Alchemist: Arcane Pad and Teleport Pad.");
        Chat.Send(player, "Glassblower: Glass and Still Water pieces.");
    }

    private static void SendQa(Players.Player player)
    {
        Chat.Send(player, "QA pass: check placement, rotation, collision and pathing on benches, crates, cow, pads and small windows.");
        Chat.Send(player, "Light pass: verify Lamp, Golden Lamp, Lava, Lava Slab and Teleport Pad glow correctly.");
        Chat.Send(player, "Recipe pass: Crafter -> props/windows, Stonemason -> architecture, Alchemist -> arcane pads.");
        Chat.Send(player, "If something feels wrong, tell me the exact item name and whether it is placement, rotation, collision, light or recipe related.");
    }
}
