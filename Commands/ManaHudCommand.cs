using System.Collections.Generic;
using Chatting;
using Pandaros.API.Entities;

[ChatCommandAutoLoader]
public class ManaHudCommand : IChatCommand
{
    public bool TryDoCommand(Players.Player player, string chatItem, List<string> splits)
    {
        if (splits == null || splits.Count == 0 || splits[0] != "/bnmanahud")
            return false;

        if (player == null)
            return true;

        if (splits.Count == 1)
        {
            SendHelp(player);
            return true;
        }

        switch (splits[1].ToLowerInvariant())
        {
            case "status":
                var settings = BetterNecromancy.ManaHudSettings.Current;
                Chat.Send(player, "Mana HUD v" + settings.ConfigVersion + " | anchor: " + settings.Anchor + " | offset: x=" + settings.OffsetX + ", y=" + settings.OffsetY + ".");
                return true;

            case "set":
                if (splits.Count < 4 ||
                    !int.TryParse(splits[2], out var x) ||
                    !int.TryParse(splits[3], out var y))
                {
                    Chat.Send(player, "Usage: /bnmanahud set <x> <y>");
                    return true;
                }

                BetterNecromancy.ManaHudSettings.SetOffsets(x, y, out var setMessage);
                PlayerMagicStateManager.RefreshAllManaHud();
                Chat.Send(player, setMessage);
                return true;

            case "nudge":
                if (splits.Count < 4 ||
                    !int.TryParse(splits[2], out var dx) ||
                    !int.TryParse(splits[3], out var dy))
                {
                    Chat.Send(player, "Usage: /bnmanahud nudge <dx> <dy>");
                    return true;
                }

                BetterNecromancy.ManaHudSettings.Nudge(dx, dy, out var nudgeMessage);
                PlayerMagicStateManager.RefreshAllManaHud();
                Chat.Send(player, nudgeMessage);
                return true;

            case "setanchor":
                if (splits.Count < 3)
                {
                    Chat.Send(player, "Usage: /bnmanahud setanchor <TopLeft|TopCenter|TopRight|MiddleLeft|MiddleCenter|MiddleRight|BottomLeft|BottomCenter|BottomRight>");
                    return true;
                }

                BetterNecromancy.ManaHudSettings.SetAnchor(splits[2], out var anchorMessage);
                PlayerMagicStateManager.RefreshAllManaHud();
                Chat.Send(player, anchorMessage);
                return true;

            case "reset":
                BetterNecromancy.ManaHudSettings.Reset(out var resetMessage);
                PlayerMagicStateManager.RefreshAllManaHud();
                Chat.Send(player, resetMessage);
                return true;

            case "rebuild":
                BetterNecromancy.ManaHudSettings.Rebuild(out var rebuildMessage);
                PlayerMagicStateManager.RefreshAllManaHud();
                Chat.Send(player, rebuildMessage);
                return true;

            case "reload":
                BetterNecromancy.ManaHudSettings.Reload(out var reloadMessage);
                PlayerMagicStateManager.RefreshAllManaHud();
                Chat.Send(player, reloadMessage);
                return true;

            default:
                SendHelp(player);
                return true;
        }
    }

    private static void SendHelp(Players.Player player)
    {
        Chat.Send(player, "/bnmanahud status | /bnmanahud set <x> <y> | /bnmanahud nudge <dx> <dy> | /bnmanahud setanchor <preset> | /bnmanahud reset | /bnmanahud rebuild | /bnmanahud reload");
    }
}
