using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Pipliz;

namespace BetterNecromancy
{
    public sealed class BossLootDropDefinition
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

    public sealed class BossLootProfile
    {
        public string BossName { get; set; }
        public List<BossLootDropDefinition> Drops { get; set; } = new List<BossLootDropDefinition>();

        public void Normalize()
        {
            BossName = BossName?.Trim() ?? string.Empty;
            Drops ??= new List<BossLootDropDefinition>();

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

    public sealed class BossLootSettings
    {
        public List<BossLootProfile> Bosses { get; set; } = new List<BossLootProfile>();

        public void Normalize()
        {
            Bosses ??= new List<BossLootProfile>();

            for (var i = Bosses.Count - 1; i >= 0; i--)
            {
                var boss = Bosses[i];
                if (boss == null)
                {
                    Bosses.RemoveAt(i);
                    continue;
                }

                boss.Normalize();
                if (string.IsNullOrWhiteSpace(boss.BossName))
                    Bosses.RemoveAt(i);
            }
        }
    }

    public sealed class ResolvedBossLootDrop
    {
        public ushort ItemIndex { get; set; }
        public int Amount { get; set; }
        public string DisplayName { get; set; }
    }

    public static class BossLootTable
    {
        private const string FileName = "bossLoot.json";
        private static readonly object Sync = new object();
        private static readonly HashSet<string> UnknownItemsLogged = new HashSet<string>();
        private static BossLootSettings _current;

        public static BossLootSettings Current
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
                UnknownItemsLogged.Clear();

                if (!TryLoadFromDisk(out var loaded, out var created, out var error))
                {
                    message = "Boss loot reload failed: " + error;
                    return false;
                }

                _current = loaded;
                message = created
                    ? "Boss loot config created at " + ConfigPath + "."
                    : "Boss loot reloaded from " + ConfigPath + ".";
                return true;
            }
        }

        public static List<ResolvedBossLootDrop> RollLoot(string bossName)
        {
            lock (Sync)
            {
                EnsureLoaded();

                var profile = FindProfile(bossName);
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

                    if (!TryResolveItem(definition.Item, out var itemIndex, out var displayName))
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

        public static bool TryResolveRewardItem(string itemKey, out ushort itemIndex, out string displayName)
        {
            lock (Sync)
                return TryResolveItem(itemKey, out itemIndex, out displayName);
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

        private static bool TryLoadFromDisk(out BossLootSettings settings, out bool created, out string error)
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
                    : JsonConvert.DeserializeObject<BossLootSettings>(json) ?? CreateDefaultSettings();
                settings.Normalize();
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }

        private static BossLootProfile FindProfile(string bossName)
        {
            var normalizedBossName = NormalizeBossName(bossName);
            return Current.Bosses.FirstOrDefault(profile => NormalizeBossName(profile.BossName) == normalizedBossName);
        }

        private static bool TryResolveItem(string itemKey, out ushort itemIndex, out string displayName)
        {
            itemIndex = 0;
            displayName = MakeFriendlyName(itemKey);

            switch (NormalizeBossName(itemKey))
            {
                case "mana":
                    itemIndex = Pandaros.Settlers.Items.Mana.Item.ItemIndex;
                    return true;

                case "esper":
                    itemIndex = Pandaros.Settlers.Items.Esper.Item.ItemIndex;
                    return true;

                case "aether":
                    itemIndex = Pandaros.Settlers.Items.Aether.Item.ItemIndex;
                    return true;

                case "elementium":
                    itemIndex = Pandaros.Settlers.Items.Elementium.Item.ItemIndex;
                    return true;

                case "void":
                    itemIndex = Pandaros.Settlers.Items.Void.Item.ItemIndex;
                    return true;

                case "airstone":
                    itemIndex = Pandaros.Settlers.Items.AirStone.Item.ItemIndex;
                    return true;

                case "earthstone":
                    itemIndex = Pandaros.Settlers.Items.EarthStone.Item.ItemIndex;
                    return true;

                case "firestone":
                    itemIndex = Pandaros.Settlers.Items.FireStone.Item.ItemIndex;
                    return true;

                case "waterstone":
                    itemIndex = Pandaros.Settlers.Items.WaterStone.Item.ItemIndex;
                    return true;

                case "manawand":
                case "briarwand":
                case "sparkwand":
                case "venomwand":
                case "emberwand":
                case "frostwand":
                case "crystalwand":
                case "stonewand":
                case "stormwand":
                case "aetherwand":
                case "magicwand":
                case "bloodwand":
                case "voidwand":
                    itemIndex = Pandaros.Settlers.Items.Magical.ManaCrystal.Item.ItemIndex;
                    displayName = "Mana Crystal";
                    return true;

                case "adamantine":
                    itemIndex = Pandaros.Settlers.Items.Magical.Adamantine.Item.ItemIndex;
                    return true;

                case "adamantinenugget":
                    if (ItemTypes.TryGetType(ModEntry.Namespace + ".AdamantineNugget", out var adamantineNuggetType))
                    {
                        itemIndex = adamantineNuggetType.ItemIndex;
                        displayName = "Adamantine Nugget";
                        return true;
                    }
                    break;

                case "healthbooster":
                    itemIndex = Pandaros.Settlers.Items.Magical.HealthBooster.Item.ItemIndex;
                    return true;

                case "bootsoffalling":
                    itemIndex = Pandaros.Settlers.Items.Armor.Magical.BootsOfFalling.Item.ItemIndex;
                    return true;

                case "manacrystal":
                    itemIndex = Pandaros.Settlers.Items.Magical.ManaCrystal.Item.ItemIndex;
                    return true;

                case "arcanefocus":
                    itemIndex = Pandaros.Settlers.Items.Magical.ArcaneFocus.Item.ItemIndex;
                    return true;

                case "arcanefocus1":
                    itemIndex = Pandaros.Settlers.Items.Magical.ArcaneFocus1.Item.ItemIndex;
                    return true;

                case "arcanefocus2":
                    itemIndex = Pandaros.Settlers.Items.Magical.ArcaneFocus2.Item.ItemIndex;
                    return true;

                case "skilledsword":
                    itemIndex = Pandaros.Settlers.Items.Magical.SkilledSword.Item.ItemIndex;
                    return true;

                case "skilledsword1":
                    itemIndex = Pandaros.Settlers.Items.Magical.SkilledSword1.Item.ItemIndex;
                    return true;

                case "skilledsword2":
                    itemIndex = Pandaros.Settlers.Items.Magical.SkilledSword2.Item.ItemIndex;
                    return true;

                case "skilledhelm":
                    itemIndex = Pandaros.Settlers.Items.Magical.SkilledHelm.Item.ItemIndex;
                    return true;

                case "skilledhelm1":
                    itemIndex = Pandaros.Settlers.Items.Magical.SkilledHelm1.Item.ItemIndex;
                    return true;

                case "skilledhelm2":
                    itemIndex = Pandaros.Settlers.Items.Magical.SkilledHelm2.Item.ItemIndex;
                    return true;

                case "skilledchest":
                    itemIndex = Pandaros.Settlers.Items.Magical.SkilledChest.Item.ItemIndex;
                    return true;

                case "skilledchest1":
                    itemIndex = Pandaros.Settlers.Items.Magical.SkilledChest1.Item.ItemIndex;
                    return true;

                case "skilledchest2":
                    itemIndex = Pandaros.Settlers.Items.Magical.SkilledChest2.Item.ItemIndex;
                    return true;

                case "skilledgloves":
                    itemIndex = Pandaros.Settlers.Items.Magical.SkilledGloves.Item.ItemIndex;
                    return true;

                case "skilledgloves1":
                    itemIndex = Pandaros.Settlers.Items.Magical.SkilledGloves1.Item.ItemIndex;
                    return true;

                case "skilledgloves2":
                    itemIndex = Pandaros.Settlers.Items.Magical.SkilledGloves2.Item.ItemIndex;
                    return true;

                case "skilledlegs":
                    itemIndex = Pandaros.Settlers.Items.Magical.SkilledLegs.Item.ItemIndex;
                    return true;

                case "skilledlegs1":
                    itemIndex = Pandaros.Settlers.Items.Magical.SkilledLegs1.Item.ItemIndex;
                    return true;

                case "skilledlegs2":
                    itemIndex = Pandaros.Settlers.Items.Magical.SkilledLegs2.Item.ItemIndex;
                    return true;

                case "skilledboots":
                    itemIndex = Pandaros.Settlers.Items.Magical.SkilledBoots.Item.ItemIndex;
                    return true;

                case "skilledboots1":
                    itemIndex = Pandaros.Settlers.Items.Magical.SkilledBoots1.Item.ItemIndex;
                    return true;

                case "skilledboots2":
                    itemIndex = Pandaros.Settlers.Items.Magical.SkilledBoots2.Item.ItemIndex;
                    return true;

                case "skilledshield":
                    itemIndex = Pandaros.Settlers.Items.Magical.SkilledShield.Item.ItemIndex;
                    return true;

                case "skilledshield1":
                    itemIndex = Pandaros.Settlers.Items.Magical.SkilledShield1.Item.ItemIndex;
                    return true;

                case "skilledshield2":
                    itemIndex = Pandaros.Settlers.Items.Magical.SkilledShield2.Item.ItemIndex;
                    return true;

                case "bandage":
                    itemIndex = Pandaros.Settlers.Items.Healing.Bandage.Item.ItemIndex;
                    return true;

                case "treatedbandage":
                    itemIndex = Pandaros.Settlers.Items.Healing.TreatedBandage.Item.ItemIndex;
                    return true;

                case "anitbiotic":
                case "antibiotic":
                    itemIndex = Pandaros.Settlers.Items.Healing.Anitbiotic.Item.ItemIndex;
                    displayName = "Antibiotic";
                    return true;

                case "innocentsoul":
                    if (ItemTypes.TryGetType(ModEntry.Namespace + ".InnocentSoul", out var innocentSoulType))
                    {
                        itemIndex = innocentSoulType.ItemIndex;
                        displayName = "Innocent Soul";
                        return true;
                    }
                    break;
            }

            if (ItemTypes.TryGetType(itemKey, out var resolvedByExactKey))
            {
                itemIndex = resolvedByExactKey.ItemIndex;
                return true;
            }

            var namespacedKey = ModEntry.Namespace + "." + itemKey;
            if (ItemTypes.TryGetType(namespacedKey, out var resolvedByNamespacedKey))
            {
                itemIndex = resolvedByNamespacedKey.ItemIndex;
                return true;
            }

            if (UnknownItemsLogged.Add(itemKey ?? string.Empty))
                Log.WriteWarning("BetterNecromancy boss loot references unknown item key: " + itemKey);

            return false;
        }

        private static string NormalizeBossName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            return new string(value
                .Trim()
                .ToLowerInvariant()
                .Where(char.IsLetterOrDigit)
                .ToArray());
        }

        private static string MakeFriendlyName(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return "Unknown Loot";

            var chars = new List<char>(key.Length + 8);
            for (var i = 0; i < key.Length; i++)
            {
                var current = key[i];
                if (i > 0 &&
                    char.IsUpper(current) &&
                    (char.IsLower(key[i - 1]) || char.IsDigit(key[i - 1])))
                {
                    chars.Add(' ');
                }

                chars.Add(current);
            }

            return new string(chars.ToArray()).Replace('-', ' ').Trim();
        }

        private static void WriteFile(string path, BossLootSettings settings)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(path, JsonConvert.SerializeObject(settings, Formatting.Indented));
        }

        private static BossLootSettings CreateDefaultSettings()
        {
            var settings = new BossLootSettings
            {
                Bosses = new List<BossLootProfile>
                {
                    new BossLootProfile
                    {
                        BossName = "ZombieKing",
                        Drops = new List<BossLootDropDefinition>
                        {
                            new BossLootDropDefinition { Item = "Mana", MinAmount = 10, MaxAmount = 18, Chance = 1f },
                            new BossLootDropDefinition { Item = "Esper", MinAmount = 3, MaxAmount = 6, Chance = 1f },
                            new BossLootDropDefinition { Item = "FireStone", MinAmount = 1, MaxAmount = 2, Chance = 0.55f },
                            new BossLootDropDefinition { Item = "Elementium", MinAmount = 1, MaxAmount = 3, Chance = 0.65f },
                            new BossLootDropDefinition { Item = "SkilledHelm", MinAmount = 1, MaxAmount = 1, Chance = 0.06f },
                            new BossLootDropDefinition { Item = "ManaCrystal", MinAmount = 1, MaxAmount = 1, Chance = 0.10f },
                            new BossLootDropDefinition { Item = "Void", MinAmount = 1, MaxAmount = 2, Chance = 0.35f }
                        }
                    },
                    new BossLootProfile
                    {
                        BossName = "ZombieQueen",
                        Drops = new List<BossLootDropDefinition>
                        {
                            new BossLootDropDefinition { Item = "Mana", MinAmount = 8, MaxAmount = 14, Chance = 1f },
                            new BossLootDropDefinition { Item = "Esper", MinAmount = 2, MaxAmount = 5, Chance = 1f },
                            new BossLootDropDefinition { Item = "Elementium", MinAmount = 2, MaxAmount = 4, Chance = 0.6f },
                            new BossLootDropDefinition { Item = "WaterStone", MinAmount = 1, MaxAmount = 2, Chance = 0.55f },
                            new BossLootDropDefinition { Item = "ManaCrystal", MinAmount = 1, MaxAmount = 1, Chance = 0.10f },
                            new BossLootDropDefinition { Item = "ArcaneFocus", MinAmount = 1, MaxAmount = 1, Chance = 0.12f },
                            new BossLootDropDefinition { Item = "SkilledHelm1", MinAmount = 1, MaxAmount = 1, Chance = 0.05f },
                            new BossLootDropDefinition { Item = "SkilledHelm2", MinAmount = 1, MaxAmount = 1, Chance = 0.02f },
                            new BossLootDropDefinition { Item = "ManaCrystal", MinAmount = 1, MaxAmount = 1, Chance = 1f },
                            new BossLootDropDefinition { Item = "Void", MinAmount = 1, MaxAmount = 2, Chance = 0.45f }
                        }
                    },
                    new BossLootProfile
                    {
                        BossName = "Fallen Ranger",
                        Drops = new List<BossLootDropDefinition>
                        {
                            new BossLootDropDefinition { Item = "Mana", MinAmount = 6, MaxAmount = 12, Chance = 1f },
                            new BossLootDropDefinition { Item = "Aether", MinAmount = 2, MaxAmount = 5, Chance = 1f },
                            new BossLootDropDefinition { Item = "AirStone", MinAmount = 1, MaxAmount = 2, Chance = 0.85f },
                            new BossLootDropDefinition { Item = "AdamantineNugget", MinAmount = 2, MaxAmount = 4, Chance = 0.45f },
                            new BossLootDropDefinition { Item = "Adamantine", MinAmount = 1, MaxAmount = 2, Chance = 0.25f },
                            new BossLootDropDefinition { Item = "SkilledGloves", MinAmount = 1, MaxAmount = 1, Chance = 0.07f },
                            new BossLootDropDefinition { Item = "SkilledGloves1", MinAmount = 1, MaxAmount = 1, Chance = 0.05f },
                            new BossLootDropDefinition { Item = "SkilledGloves2", MinAmount = 1, MaxAmount = 1, Chance = 0.02f },
                            new BossLootDropDefinition { Item = "ManaCrystal", MinAmount = 1, MaxAmount = 1, Chance = 0.08f },
                            new BossLootDropDefinition { Item = "Aether", MinAmount = 4, MaxAmount = 8, Chance = 0.12f },
                            new BossLootDropDefinition { Item = "ArcaneFocus1", MinAmount = 1, MaxAmount = 1, Chance = 0.05f }
                        }
                    },
                    new BossLootProfile
                    {
                        BossName = "Putrid Corpse",
                        Drops = new List<BossLootDropDefinition>
                        {
                            new BossLootDropDefinition { Item = "Mana", MinAmount = 4, MaxAmount = 8, Chance = 1f },
                            new BossLootDropDefinition { Item = "Aether", MinAmount = 2, MaxAmount = 4, Chance = 0.8f },
                            new BossLootDropDefinition { Item = "Bandage", MinAmount = 2, MaxAmount = 5, Chance = 1f },
                            new BossLootDropDefinition { Item = "Anitbiotic", MinAmount = 2, MaxAmount = 4, Chance = 0.9f },
                            new BossLootDropDefinition { Item = "TreatedBandage", MinAmount = 1, MaxAmount = 2, Chance = 0.65f },
                            new BossLootDropDefinition { Item = "SkilledChest", MinAmount = 1, MaxAmount = 1, Chance = 0.08f },
                            new BossLootDropDefinition { Item = "SkilledChest1", MinAmount = 1, MaxAmount = 1, Chance = 0.05f },
                            new BossLootDropDefinition { Item = "SkilledChest2", MinAmount = 1, MaxAmount = 1, Chance = 0.02f }
                        }
                    },
                    new BossLootProfile
                    {
                        BossName = "Bulging",
                        Drops = new List<BossLootDropDefinition>
                        {
                            new BossLootDropDefinition { Item = "Mana", MinAmount = 8, MaxAmount = 14, Chance = 1f },
                            new BossLootDropDefinition { Item = "Aether", MinAmount = 1, MaxAmount = 3, Chance = 0.75f },
                            new BossLootDropDefinition { Item = "Elementium", MinAmount = 2, MaxAmount = 4, Chance = 0.8f },
                            new BossLootDropDefinition { Item = "FireStone", MinAmount = 2, MaxAmount = 4, Chance = 0.10f },
                            new BossLootDropDefinition { Item = "FireStone", MinAmount = 1, MaxAmount = 2, Chance = 0.55f },
                            new BossLootDropDefinition { Item = "WaterStone", MinAmount = 1, MaxAmount = 2, Chance = 0.55f }
                        }
                    },
                    new BossLootProfile
                    {
                        BossName = "Hoarder",
                        Drops = new List<BossLootDropDefinition>
                        {
                            new BossLootDropDefinition { Item = "Mana", MinAmount = 6, MaxAmount = 10, Chance = 1f },
                            new BossLootDropDefinition { Item = "Esper", MinAmount = 2, MaxAmount = 4, Chance = 0.85f },
                            new BossLootDropDefinition { Item = "EarthStone", MinAmount = 1, MaxAmount = 2, Chance = 0.55f },
                            new BossLootDropDefinition { Item = "Elementium", MinAmount = 1, MaxAmount = 2, Chance = 0.65f },
                            new BossLootDropDefinition { Item = "AdamantineNugget", MinAmount = 2, MaxAmount = 5, Chance = 0.60f },
                            new BossLootDropDefinition { Item = "SkilledSword", MinAmount = 1, MaxAmount = 1, Chance = 0.08f },
                            new BossLootDropDefinition { Item = "SkilledSword1", MinAmount = 1, MaxAmount = 1, Chance = 0.05f },
                            new BossLootDropDefinition { Item = "SkilledSword2", MinAmount = 1, MaxAmount = 1, Chance = 0.02f },
                            new BossLootDropDefinition { Item = "ArcaneFocus1", MinAmount = 1, MaxAmount = 1, Chance = 0.08f },
                            new BossLootDropDefinition { Item = "ManaCrystal", MinAmount = 1, MaxAmount = 1, Chance = 0.10f },
                            new BossLootDropDefinition { Item = "Adamantine", MinAmount = 1, MaxAmount = 2, Chance = 0.20f }
                        }
                    },
                    new BossLootProfile
                    {
                        BossName = "Jack-b-Nimble",
                        Drops = new List<BossLootDropDefinition>
                        {
                            new BossLootDropDefinition { Item = "Mana", MinAmount = 5, MaxAmount = 9, Chance = 1f },
                            new BossLootDropDefinition { Item = "Esper", MinAmount = 1, MaxAmount = 3, Chance = 0.7f },
                            new BossLootDropDefinition { Item = "AirStone", MinAmount = 1, MaxAmount = 2, Chance = 0.8f },
                            new BossLootDropDefinition { Item = "ManaCrystal", MinAmount = 1, MaxAmount = 1, Chance = 0.14f },
                            new BossLootDropDefinition { Item = "BootsOfFalling", MinAmount = 1, MaxAmount = 1, Chance = 0.10f },
                            new BossLootDropDefinition { Item = "SkilledBoots", MinAmount = 1, MaxAmount = 1, Chance = 0.06f },
                            new BossLootDropDefinition { Item = "SkilledBoots1", MinAmount = 1, MaxAmount = 1, Chance = 0.04f },
                            new BossLootDropDefinition { Item = "SkilledBoots2", MinAmount = 1, MaxAmount = 1, Chance = 0.02f }
                        }
                    },
                    new BossLootProfile
                    {
                        BossName = "Juggernaut",
                        Drops = new List<BossLootDropDefinition>
                        {
                            new BossLootDropDefinition { Item = "Mana", MinAmount = 10, MaxAmount = 16, Chance = 1f },
                            new BossLootDropDefinition { Item = "AdamantineNugget", MinAmount = 4, MaxAmount = 8, Chance = 1f },
                            new BossLootDropDefinition { Item = "Adamantine", MinAmount = 2, MaxAmount = 4, Chance = 0.9f },
                            new BossLootDropDefinition { Item = "EarthStone", MinAmount = 1, MaxAmount = 2, Chance = 0.75f },
                            new BossLootDropDefinition { Item = "FireStone", MinAmount = 1, MaxAmount = 2, Chance = 0.5f },
                            new BossLootDropDefinition { Item = "ManaCrystal", MinAmount = 1, MaxAmount = 1, Chance = 1f },
                            new BossLootDropDefinition { Item = "SkilledShield", MinAmount = 1, MaxAmount = 1, Chance = 0.08f },
                            new BossLootDropDefinition { Item = "SkilledShield1", MinAmount = 1, MaxAmount = 1, Chance = 0.06f },
                            new BossLootDropDefinition { Item = "SkilledShield2", MinAmount = 1, MaxAmount = 1, Chance = 0.02f },
                            new BossLootDropDefinition { Item = "ArcaneFocus2", MinAmount = 1, MaxAmount = 1, Chance = 0.06f },
                            new BossLootDropDefinition { Item = "Void", MinAmount = 4, MaxAmount = 8, Chance = 0.04f },
                            new BossLootDropDefinition { Item = "HealthBooster", MinAmount = 1, MaxAmount = 1, Chance = 0.08f }
                        }
                    },
                    new BossLootProfile
                    {
                        BossName = "Phase",
                        Drops = new List<BossLootDropDefinition>
                        {
                            new BossLootDropDefinition { Item = "Mana", MinAmount = 6, MaxAmount = 10, Chance = 1f },
                            new BossLootDropDefinition { Item = "Aether", MinAmount = 1, MaxAmount = 3, Chance = 0.7f },
                            new BossLootDropDefinition { Item = "Void", MinAmount = 1, MaxAmount = 2, Chance = 0.4f },
                            new BossLootDropDefinition { Item = "WaterStone", MinAmount = 1, MaxAmount = 2, Chance = 0.6f },
                            new BossLootDropDefinition { Item = "WaterStone", MinAmount = 2, MaxAmount = 4, Chance = 0.08f },
                            new BossLootDropDefinition { Item = "SkilledLegs", MinAmount = 1, MaxAmount = 1, Chance = 0.08f },
                            new BossLootDropDefinition { Item = "SkilledLegs1", MinAmount = 1, MaxAmount = 1, Chance = 0.05f },
                            new BossLootDropDefinition { Item = "SkilledLegs2", MinAmount = 1, MaxAmount = 1, Chance = 0.02f },
                            new BossLootDropDefinition { Item = "ArcaneFocus", MinAmount = 1, MaxAmount = 1, Chance = 0.06f },
                            new BossLootDropDefinition { Item = "ManaCrystal", MinAmount = 1, MaxAmount = 1, Chance = 0.15f },
                            new BossLootDropDefinition { Item = "Void", MinAmount = 1, MaxAmount = 2, Chance = 0.45f }
                        }
                    }
                }
            };

            AddDefaultInnocentSoulDrops(settings);
            return settings;
        }

        private static void AddDefaultInnocentSoulDrops(BossLootSettings settings)
        {
            if (settings?.Bosses == null)
                return;

            for (var i = 0; i < settings.Bosses.Count; i++)
            {
                var boss = settings.Bosses[i];
                if (boss?.Drops == null)
                    continue;

                var hasSoulDrop = false;
                for (var j = 0; j < boss.Drops.Count; j++)
                {
                    if (NormalizeBossName(boss.Drops[j]?.Item) == "innocentsoul")
                    {
                        hasSoulDrop = true;
                        break;
                    }
                }

                if (hasSoulDrop)
                    continue;

                boss.Drops.Add(new BossLootDropDefinition
                {
                    Item = "InnocentSoul",
                    MinAmount = 1,
                    MaxAmount = 1,
                    Chance = 0.06f
                });
            }
        }
    }
}
