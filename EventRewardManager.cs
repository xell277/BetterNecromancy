using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Chatting;
using Newtonsoft.Json;
using Pipliz;
using Shared;

namespace BetterNecromancy
{
    public sealed class EventRewardDropDefinition
    {
        public string Item { get; set; }
        public int MinAmount { get; set; } = 1;
        public int MaxAmount { get; set; } = 1;
        public float Chance { get; set; } = 1f;

        public void Normalize()
        {
            Item = Item?.Trim() ?? string.Empty;
            MinAmount = System.Math.Max(1, MinAmount);
            MaxAmount = System.Math.Max(MinAmount, MaxAmount);
            Chance = UnityEngine.Mathf.Clamp01(Chance);
        }
    }

    public sealed class EventRewardProfile
    {
        public string EventId { get; set; }
        public List<EventRewardDropDefinition> Drops { get; set; } = new List<EventRewardDropDefinition>();

        public void Normalize()
        {
            EventId = EventId?.Trim() ?? string.Empty;
            Drops ??= new List<EventRewardDropDefinition>();

            for (var i = Drops.Count - 1; i >= 0; i--)
            {
                var drop = Drops[i];
                if (drop == null)
                {
                    Drops.RemoveAt(i);
                    continue;
                }

                drop.Normalize();
            }
        }
    }

    public sealed class EventRewardSettings
    {
        public List<EventRewardProfile> Events { get; set; } = new List<EventRewardProfile>();

        public void Normalize()
        {
            Events ??= new List<EventRewardProfile>();

            for (var i = Events.Count - 1; i >= 0; i--)
            {
                var profile = Events[i];
                if (profile == null)
                {
                    Events.RemoveAt(i);
                    continue;
                }

                profile.Normalize();
                if (string.IsNullOrWhiteSpace(profile.EventId))
                    Events.RemoveAt(i);
            }
        }
    }

    public static class EventRewardManager
    {
        private const string FileName = "eventRewards.json";
        private static readonly object Sync = new object();
        private static EventRewardSettings _current;

        public static EventRewardSettings Current
        {
            get
            {
                EnsureLoaded();
                return _current;
            }
        }

        public static string ConfigPath =>
            string.IsNullOrEmpty(ModEntry.ModFolder)
                ? FileName
                : Path.Combine(ModEntry.ModFolder, FileName);

        public static bool Reload(out string message)
        {
            lock (Sync)
            {
                if (!TryLoadFromDisk(out var loaded, out var created, out var error))
                {
                    message = "Event reward reload failed: " + error;
                    return false;
                }

                _current = loaded;
                message = created
                    ? "Event reward config created at " + ConfigPath + "."
                    : "Event rewards reloaded from " + ConfigPath + ".";
                return true;
            }
        }

        public static List<ResolvedBossLootDrop> RollLoot(string eventId)
        {
            lock (Sync)
            {
                EnsureLoaded();

                var profile = FindProfile(eventId);
                if (profile == null || profile.Drops.Count == 0)
                    return new List<ResolvedBossLootDrop>();

                var rolled = new Dictionary<ushort, ResolvedBossLootDrop>();

                for (var i = 0; i < profile.Drops.Count; i++)
                {
                    var definition = profile.Drops[i];
                    if (definition == null)
                        continue;

                    if (definition.Chance < 1f && Pipliz.Random.NextFloat() > definition.Chance)
                        continue;

                    if (!BossLootTable.TryResolveRewardItem(definition.Item, out var itemIndex, out var displayName))
                        continue;

                    var amount = definition.MinAmount == definition.MaxAmount
                        ? definition.MinAmount
                        : Pipliz.Random.Next(definition.MinAmount, definition.MaxAmount + 1);

                    if (amount <= 0)
                        continue;

                    if (rolled.TryGetValue(itemIndex, out var existing))
                    {
                        existing.Amount += amount;
                    }
                    else
                    {
                        rolled[itemIndex] = new ResolvedBossLootDrop
                        {
                            ItemIndex = itemIndex,
                            Amount = amount,
                            DisplayName = displayName
                        };
                    }
                }

                return rolled.Values.OrderBy(drop => drop.DisplayName).ToList();
            }
        }

        public static bool GrantToColony(Colony colony, string eventId, string eventDisplayName)
        {
            if (colony?.ColonyGroup?.Stockpile == null)
                return false;

            var grantedLoot = RollLoot(eventId);
            if (grantedLoot.Count == 0)
                return false;

            for (var i = 0; i < grantedLoot.Count; i++)
                colony.ColonyGroup.Stockpile.Add(grantedLoot[i].ItemIndex, grantedLoot[i].Amount);

            colony.ColonyGroup.Stockpile.SendToOwners();

            var lootSummary = string.Join(", ", grantedLoot.Select(drop => drop.Amount + " " + drop.DisplayName));
            foreach (var player in Players.ConnectedPlayers)
            {
                if (!PlayerUiGuard.CanSendStable(player) || !player.OwnsColony(colony))
                    continue;

                PlayerToastManager.Show(player, eventDisplayName + " rewarded " + lootSummary + " into your stockpile.", "#d8f0bf", 5200L);
            }

            return true;
        }

        private static void EnsureLoaded()
        {
            if (_current != null)
                return;

            if (!TryLoadFromDisk(out _current, out _, out _))
            {
                _current = CreateDefaultSettings();
                _current.Normalize();
            }
        }

        private static bool TryLoadFromDisk(out EventRewardSettings settings, out bool created, out string error)
        {
            settings = null;
            created = false;
            error = null;

            try
            {
                var path = ConfigPath;
                if (string.IsNullOrEmpty(path))
                {
                    settings = CreateDefaultSettings();
                    settings.Normalize();
                    return true;
                }

                if (!File.Exists(path))
                {
                    settings = CreateDefaultSettings();
                    settings.Normalize();
                    WriteFile(path, settings);
                    created = true;
                    return true;
                }

                var json = File.ReadAllText(path);
                settings = string.IsNullOrWhiteSpace(json)
                    ? CreateDefaultSettings()
                    : JsonConvert.DeserializeObject<EventRewardSettings>(json) ?? CreateDefaultSettings();
                settings.Normalize();
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }

        private static EventRewardProfile FindProfile(string eventId)
        {
            var normalized = NormalizeEventId(eventId);
            return Current.Events.FirstOrDefault(profile => NormalizeEventId(profile.EventId) == normalized);
        }

        private static string NormalizeEventId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            return new string(value
                .Trim()
                .ToLowerInvariant()
                .Where(char.IsLetterOrDigit)
                .ToArray());
        }

        private static void WriteFile(string path, EventRewardSettings settings)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(path, JsonConvert.SerializeObject(settings, Formatting.Indented));
        }

        private static EventRewardSettings CreateDefaultSettings()
        {
            return new EventRewardSettings
            {
                Events = new List<EventRewardProfile>
                {
                    new EventRewardProfile
                    {
                        EventId = "BloodMoon",
                        Drops = new List<EventRewardDropDefinition>
                        {
                            new EventRewardDropDefinition { Item = "Mana", MinAmount = 10, MaxAmount = 16, Chance = 1f },
                            new EventRewardDropDefinition { Item = "Esper", MinAmount = 2, MaxAmount = 4, Chance = 1f },
                            new EventRewardDropDefinition { Item = "FireStone", MinAmount = 1, MaxAmount = 2, Chance = 0.65f },
                            new EventRewardDropDefinition { Item = "Void", MinAmount = 1, MaxAmount = 2, Chance = 0.35f },
                            new EventRewardDropDefinition { Item = "BloodWand", MinAmount = 1, MaxAmount = 1, Chance = 0.06f },
                            new EventRewardDropDefinition { Item = "SkilledSword", MinAmount = 1, MaxAmount = 1, Chance = 0.05f },
                            new EventRewardDropDefinition { Item = "ManaCrystal", MinAmount = 1, MaxAmount = 1, Chance = 0.06f },
                            new EventRewardDropDefinition { Item = "InnocentSoul", MinAmount = 1, MaxAmount = 1, Chance = 0.03f }
                        }
                    },
                    new EventRewardProfile
                    {
                        EventId = "PlagueFog",
                        Drops = new List<EventRewardDropDefinition>
                        {
                            new EventRewardDropDefinition { Item = "Mana", MinAmount = 8, MaxAmount = 14, Chance = 1f },
                            new EventRewardDropDefinition { Item = "Aether", MinAmount = 2, MaxAmount = 4, Chance = 1f },
                            new EventRewardDropDefinition { Item = "TreatedBandage", MinAmount = 2, MaxAmount = 4, Chance = 1f },
                            new EventRewardDropDefinition { Item = "Antibiotic", MinAmount = 2, MaxAmount = 4, Chance = 1f },
                            new EventRewardDropDefinition { Item = "VenomWand", MinAmount = 1, MaxAmount = 1, Chance = 0.06f },
                            new EventRewardDropDefinition { Item = "SkilledChest", MinAmount = 1, MaxAmount = 1, Chance = 0.05f },
                            new EventRewardDropDefinition { Item = "ManaCrystal", MinAmount = 1, MaxAmount = 1, Chance = 0.05f },
                            new EventRewardDropDefinition { Item = "InnocentSoul", MinAmount = 1, MaxAmount = 1, Chance = 0.03f }
                        }
                    },
                    new EventRewardProfile
                    {
                        EventId = "ArcaneStorm",
                        Drops = new List<EventRewardDropDefinition>
                        {
                            new EventRewardDropDefinition { Item = "Mana", MinAmount = 10, MaxAmount = 18, Chance = 1f },
                            new EventRewardDropDefinition { Item = "Esper", MinAmount = 3, MaxAmount = 6, Chance = 1f },
                            new EventRewardDropDefinition { Item = "AirStone", MinAmount = 1, MaxAmount = 2, Chance = 0.7f },
                            new EventRewardDropDefinition { Item = "WaterStone", MinAmount = 1, MaxAmount = 2, Chance = 0.6f },
                            new EventRewardDropDefinition { Item = "SparkWand", MinAmount = 1, MaxAmount = 1, Chance = 0.07f },
                            new EventRewardDropDefinition { Item = "StormWand", MinAmount = 1, MaxAmount = 1, Chance = 0.04f },
                            new EventRewardDropDefinition { Item = "ArcaneFocus", MinAmount = 1, MaxAmount = 1, Chance = 0.08f },
                            new EventRewardDropDefinition { Item = "ManaCrystal", MinAmount = 1, MaxAmount = 1, Chance = 0.07f }
                        }
                    },
                    new EventRewardProfile
                    {
                        EventId = "HordeAssault",
                        Drops = new List<EventRewardDropDefinition>
                        {
                            new EventRewardDropDefinition { Item = "Mana", MinAmount = 12, MaxAmount = 20, Chance = 1f },
                            new EventRewardDropDefinition { Item = "AdamantineNugget", MinAmount = 3, MaxAmount = 6, Chance = 0.95f },
                            new EventRewardDropDefinition { Item = "Elementium", MinAmount = 2, MaxAmount = 4, Chance = 0.8f },
                            new EventRewardDropDefinition { Item = "EmberWand", MinAmount = 1, MaxAmount = 1, Chance = 0.05f },
                            new EventRewardDropDefinition { Item = "FrostWand", MinAmount = 1, MaxAmount = 1, Chance = 0.05f },
                            new EventRewardDropDefinition { Item = "ArcaneFocus1", MinAmount = 1, MaxAmount = 1, Chance = 0.05f },
                            new EventRewardDropDefinition { Item = "ManaCrystal", MinAmount = 1, MaxAmount = 1, Chance = 0.08f },
                            new EventRewardDropDefinition { Item = "InnocentSoul", MinAmount = 1, MaxAmount = 1, Chance = 0.04f }
                        }
                    }
                }
            };
        }
    }
}
