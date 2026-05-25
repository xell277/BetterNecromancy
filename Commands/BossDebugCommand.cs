using System.Collections.Generic;
using System.Linq;
using Chatting;
using BetterNecromancy;
using Pandaros.API.Monsters;

[ChatCommandAutoLoader]
public class BossDebugCommand : IChatCommand
{
    public bool TryDoCommand(Players.Player player, string chatItem, List<string> splits)
    {
        if (splits == null || splits.Count == 0 || splits[0] != "/bnboss")
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
            case "list":
                Chat.Send(player, "Bosses: " + string.Join(", ", MonsterManager.GetRegisteredBossNames()));
                return true;

            case "status":
                Chat.Send(player, MonsterManager.GetStatusTextForPlayer(player));
                return true;

            case "next":
            case "next10":
                var seconds = 10;
                if (splits[1].ToLowerInvariant() == "next" && splits.Count >= 3 && !int.TryParse(splits[2], out seconds))
                {
                    Chat.Send(player, "Usage: /bnboss next <seconds>");
                    return true;
                }

                if (!MonsterManager.TrySetNextRandomBossRollInSeconds(seconds, out var nextMessage))
                {
                    Chat.Send(player, nextMessage);
                    return true;
                }

                Chat.Send(player, nextMessage);
                return true;

            case "spawn":
                var bossName = string.Join(" ", splits.Skip(2));
                if (!MonsterManager.TrySpawnBossForPlayer(bossName, player, out var message))
                {
                    Chat.Send(player, message);
                    return true;
                }

                Chat.Send(player, message);
                return true;

            case "kill":
                MonsterManager.TryKillActiveBossForPlayer(player, out var killMessage);
                Chat.Send(player, killMessage);
                return true;

            case "reset":
                MonsterManager.TryResetBossState(out var resetMessage);
                Chat.Send(player, resetMessage);
                return true;

            case "reload":
                var reloadMessages = new List<string>();
                var reloadSucceeded = true;

                if (!BossTuning.Reload(out var tuningReloadMessage))
                {
                    reloadSucceeded = false;
                }
                reloadMessages.Add(tuningReloadMessage);

                if (!BossLootTable.Reload(out var lootReloadMessage))
                {
                    reloadSucceeded = false;
                }
                reloadMessages.Add(lootReloadMessage);

                if (!BossVisualSettings.Reload(out var visualReloadMessage))
                {
                    reloadSucceeded = false;
                }
                reloadMessages.Add(visualReloadMessage);

                if (reloadSucceeded)
                    MonsterManager.OnBossTuningReloaded();

                Chat.Send(player, string.Join(" ", reloadMessages));
                return true;

            case "preset":
                var presetName = string.Join(" ", splits.Skip(2));
                if (string.IsNullOrWhiteSpace(presetName))
                {
                    Chat.Send(player, "Available boss presets: " + string.Join(", ", BossTuning.GetPresetNames()));
                    return true;
                }

                if (!BossTuning.TryApplyPreset(presetName, out var presetMessage))
                {
                    Chat.Send(player, presetMessage);
                    return true;
                }

                MonsterManager.OnBossTuningReloaded();
                Chat.Send(player, presetMessage);
                return true;

            default:
                SendHelp(player);
                return true;
        }
    }

    private static void SendHelp(Players.Player player)
    {
        Chat.Send(player, "/bnboss list | /bnboss status | /bnboss next <seconds> | /bnboss spawn <name> | /bnboss kill | /bnboss reset | /bnboss reload | /bnboss preset <leicht|normal|brutal>");
    }
}
