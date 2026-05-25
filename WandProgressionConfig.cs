using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace BetterNecromancy
{
    public sealed class WandLineageSettings
    {
        public int FirstBranchPoints { get; set; } = 300;
        public int Tier3Points { get; set; } = 1000;
        public int Tier4Points { get; set; } = 2400;
        public int Tier5Points { get; set; } = 4800;
        public int MasteryPointsPerLevel { get; set; } = 300;
        public int MaxMasteryLevel { get; set; } = 10;
        public float DamageBonusPerMasteryLevel { get; set; } = 0.20f;
        public List<WandBranchDefinition> Branches { get; set; } = new List<WandBranchDefinition>();

        public void Normalize()
        {
            FirstBranchPoints = Math.Max(20, FirstBranchPoints);
            Tier3Points = Math.Max(FirstBranchPoints + 1, Tier3Points);
            Tier4Points = Math.Max(Tier3Points + 1, Tier4Points);
            Tier5Points = Math.Max(Tier4Points + 1, Tier5Points);
            MasteryPointsPerLevel = Math.Max(1, MasteryPointsPerLevel);
            MaxMasteryLevel = Math.Max(1, MaxMasteryLevel);
            DamageBonusPerMasteryLevel = Math.Max(0f, DamageBonusPerMasteryLevel);
            Branches ??= new List<WandBranchDefinition>();

            if (Branches.Count == 0)
                Branches.AddRange(CreateDefaultBranches());

            for (var i = Branches.Count - 1; i >= 0; i--)
            {
                if (Branches[i] == null || string.IsNullOrWhiteSpace(Branches[i].Key))
                    Branches.RemoveAt(i);
                else
                    Branches[i].Normalize();
            }
        }

        public static List<WandBranchDefinition> CreateDefaultBranches()
        {
            return new List<WandBranchDefinition>
            {
                new WandBranchDefinition
                {
                    Key = "Frost",
                    DisplayName = "Frost",
                    Role = "control / slow / freeze / shatter",
                    Primary = "Precision frost bolt",
                    Secondary = "Shatter nova",
                    Passive = "Higher impact force and control pressure. Later tiers inherit prism and storm splinters.",
                    T6Name = "Winterglass Ascendant"
                },
                new WandBranchDefinition
                {
                    Key = "Ember",
                    DisplayName = "Ember",
                    Role = "burst / ignite / explosions / AoE",
                    Primary = "Igniting fire bolt",
                    Secondary = "Inferno detonation",
                    Passive = "Burns stack and spread harder. Later tiers inherit blood siphon sparks.",
                    T6Name = "Cinderlord Ascendant"
                },
                new WandBranchDefinition
                {
                    Key = "Venom",
                    DisplayName = "Venom",
                    Role = "poison / DoT / spread / weaken",
                    Primary = "Stacking poison dart",
                    Secondary = "Toxic bloom",
                    Passive = "Poison lasts long and stacks. Later tiers inherit thorn and void pressure.",
                    T6Name = "Widowroot Ascendant"
                },
                new WandBranchDefinition
                {
                    Key = "Stone",
                    DisplayName = "Stone",
                    Role = "heavy hits / stagger / armor break / anti-elite",
                    Primary = "Heavy stone slam",
                    Secondary = "Quake wave",
                    Passive = "Extra force, boss/elite pressure and armor-break flavor. Later tiers inherit void collapse.",
                    T6Name = "Worldbreaker Ascendant"
                }
            };
        }
    }

    public sealed class WandBranchDefinition
    {
        public string Key { get; set; }
        public string DisplayName { get; set; }
        public string Role { get; set; }
        public string Primary { get; set; }
        public string Secondary { get; set; }
        public string Passive { get; set; }
        public string T6Name { get; set; }
        public Dictionary<string, int> MasterySources { get; set; } = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        public void Normalize()
        {
            Key = Key?.Trim() ?? string.Empty;
            DisplayName = string.IsNullOrWhiteSpace(DisplayName) ? Key : DisplayName.Trim();
            Role = Role?.Trim() ?? string.Empty;
            Primary = Primary?.Trim() ?? string.Empty;
            Secondary = Secondary?.Trim() ?? string.Empty;
            Passive = Passive?.Trim() ?? string.Empty;
            T6Name = string.IsNullOrWhiteSpace(T6Name) ? DisplayName + " Ascendant" : T6Name.Trim();
            MasterySources ??= new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            if (MasterySources.Count == 0)
            {
                MasterySources["OnCast"] = 1;
                MasterySources["OnHit"] = 2;
                MasterySources["OnStatusApplied"] = 2;
                MasterySources["OnMultiKill"] = 5;
                MasterySources["OnEliteDamage"] = 4;
                MasterySources["OnBossDamage"] = 5;
                MasterySources["OnRitualContribution"] = 25;
            }
        }
    }

    public sealed class MeditationSettings
    {
        public double StartDelaySeconds { get; set; } = 0.85d;
        public int RegenIntervalReductionMs { get; set; } = 1200;
        public int FullManaRewardSeconds { get; set; } = 100;
        public int MaxManaRewardAmount { get; set; } = 1;
        public int RegenMilestoneSeconds { get; set; } = 1000;
        public int PermanentRegenReductionMs { get; set; } = 400;
        public float StationaryDistanceTolerance { get; set; } = 0.08f;

        public void Normalize()
        {
            StartDelaySeconds = Math.Max(0.25d, StartDelaySeconds);
            RegenIntervalReductionMs = Math.Max(0, RegenIntervalReductionMs);
            FullManaRewardSeconds = Math.Max(1, FullManaRewardSeconds);
            MaxManaRewardAmount = Math.Max(1, MaxManaRewardAmount);
            RegenMilestoneSeconds = Math.Max(FullManaRewardSeconds, RegenMilestoneSeconds);
            PermanentRegenReductionMs = Math.Max(0, PermanentRegenReductionMs);
            StationaryDistanceTolerance = Math.Max(0.01f, StationaryDistanceTolerance);
        }
    }

    public sealed class EliteProgressionSettings
    {
        public double FxIntervalSeconds { get; set; } = 1.35d;
        public float AuraScale { get; set; } = 1.35f;
        public float HealthMultiplier { get; set; } = 2f;
        public float GlowPulseRadius { get; set; } = 1.65f;
        public int GlowTrailSpokes { get; set; } = 6;
        public int SpawnSmokeBursts { get; set; } = 3;
        public Dictionary<string, List<EliteDropDefinition>> Drops { get; set; } = new Dictionary<string, List<EliteDropDefinition>>(StringComparer.OrdinalIgnoreCase);

        public void Normalize()
        {
            FxIntervalSeconds = Math.Max(0.35d, FxIntervalSeconds);
            AuraScale = Math.Max(0.25f, AuraScale);
            HealthMultiplier = Math.Max(1f, HealthMultiplier);
            GlowPulseRadius = Math.Max(0.4f, GlowPulseRadius);
            GlowTrailSpokes = Math.Max(3, GlowTrailSpokes);
            SpawnSmokeBursts = Math.Max(0, SpawnSmokeBursts);
            Drops ??= new Dictionary<string, List<EliteDropDefinition>>(StringComparer.OrdinalIgnoreCase);

            if (Drops.Count == 0)
                Drops = CreateDefaultDrops();

            foreach (var pair in Drops)
            {
                pair.Value?.RemoveAll(drop => drop == null || string.IsNullOrWhiteSpace(drop.ItemKey));
                if (pair.Value == null)
                    continue;

                for (var i = 0; i < pair.Value.Count; i++)
                    pair.Value[i].Normalize();
            }
        }

        public static Dictionary<string, List<EliteDropDefinition>> CreateDefaultDrops()
        {
            return new Dictionary<string, List<EliteDropDefinition>>(StringComparer.OrdinalIgnoreCase)
            {
                ["Bloodbound"] = new List<EliteDropDefinition>
                {
                    new EliteDropDefinition { ItemKey = "Aether", MinAmount = 2, MaxAmount = 3, Chance = 0.8f },
                    new EliteDropDefinition { ItemKey = "Mana", MinAmount = 3, MaxAmount = 5, Chance = 0.75f },
                    new EliteDropDefinition { ItemKey = "FireStone", MinAmount = 1, MaxAmount = 1, Chance = 0.2f }
                },
                ["Plagueborn"] = new List<EliteDropDefinition>
                {
                    new EliteDropDefinition { ItemKey = "Elementium", MinAmount = 1, MaxAmount = 3, Chance = 0.55f },
                    new EliteDropDefinition { ItemKey = "Mana", MinAmount = 2, MaxAmount = 5, Chance = 0.65f },
                    new EliteDropDefinition { ItemKey = "EarthStone", MinAmount = 1, MaxAmount = 1, Chance = 0.22f }
                },
                ["Stormtouched"] = new List<EliteDropDefinition>
                {
                    new EliteDropDefinition { ItemKey = "AirStone", MinAmount = 1, MaxAmount = 2, Chance = 0.42f },
                    new EliteDropDefinition { ItemKey = "Mana", MinAmount = 3, MaxAmount = 5, Chance = 0.7f },
                    new EliteDropDefinition { ItemKey = "Aether", MinAmount = 1, MaxAmount = 3, Chance = 0.42f }
                },
                ["Warbound"] = new List<EliteDropDefinition>
                {
                    new EliteDropDefinition { ItemKey = "AdamantineNugget", MinAmount = 3, MaxAmount = 6, Chance = 0.72f },
                    new EliteDropDefinition { ItemKey = "Elementium", MinAmount = 2, MaxAmount = 3, Chance = 0.58f },
                    new EliteDropDefinition { ItemKey = "ManaCrystal", MinAmount = 1, MaxAmount = 1, Chance = 0.08f }
                },
                ["Feral"] = new List<EliteDropDefinition>
                {
                    new EliteDropDefinition { ItemKey = "Mana", MinAmount = 2, MaxAmount = 4, Chance = 0.7f },
                    new EliteDropDefinition { ItemKey = "Esper", MinAmount = 1, MaxAmount = 3, Chance = 0.28f }
                }
            };
        }
    }

    public sealed class EliteDropDefinition
    {
        public string ItemKey { get; set; }
        public int MinAmount { get; set; } = 1;
        public int MaxAmount { get; set; } = 1;
        public float Chance { get; set; } = 1f;

        public void Normalize()
        {
            ItemKey = ItemKey?.Trim() ?? string.Empty;
            MinAmount = Math.Max(1, MinAmount);
            MaxAmount = Math.Max(MinAmount, MaxAmount);
            Chance = UnityEngine.Mathf.Clamp01(Chance);
        }
    }

    public sealed class RitualAwakeningSettings
    {
        public int RequiredTier { get; set; } = 5;
        public int MasteryPointsGranted { get; set; } = 250;
        public string ChatHint { get; set; } = "Requires a Tier 5 lineage wand. The ritual awakens that same wand to Tier 6.";

        public void Normalize()
        {
            RequiredTier = Math.Max(1, RequiredTier);
            MasteryPointsGranted = Math.Max(0, MasteryPointsGranted);
            ChatHint = ChatHint?.Trim() ?? string.Empty;
        }
    }

    public sealed class WandProgressionSettings
    {
        public WandLineageSettings Lineage { get; set; } = new WandLineageSettings();
        public MeditationSettings Meditation { get; set; } = new MeditationSettings();
        public EliteProgressionSettings Elites { get; set; } = new EliteProgressionSettings();
        public RitualAwakeningSettings RitualAwakening { get; set; } = new RitualAwakeningSettings();

        public void Normalize()
        {
            Lineage ??= new WandLineageSettings();
            Meditation ??= new MeditationSettings();
            Elites ??= new EliteProgressionSettings();
            RitualAwakening ??= new RitualAwakeningSettings();
            Lineage.Normalize();
            Meditation.Normalize();
            Elites.Normalize();
            RitualAwakening.Normalize();
        }
    }

    public static class WandProgressionConfig
    {
        private const string FileName = "wandProgression.json";
        private static readonly object Sync = new object();
        private static WandProgressionSettings _current;

        public static WandProgressionSettings Current
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
                if (!TryLoad(out var settings, out var created, out var error))
                {
                    message = "Wand progression reload failed: " + error;
                    return false;
                }

                _current = settings;
                message = created
                    ? "Wand progression config created at " + ConfigPath + "."
                    : "Wand progression config reloaded from " + ConfigPath + ".";
                return true;
            }
        }

        public static WandBranchDefinition GetBranch(string key)
        {
            EnsureLoaded();
            if (string.IsNullOrWhiteSpace(key))
                return null;

            var normalized = key.Trim();
            for (var i = 0; i < _current.Lineage.Branches.Count; i++)
            {
                var branch = _current.Lineage.Branches[i];
                if (string.Equals(branch.Key, normalized, StringComparison.OrdinalIgnoreCase))
                    return branch;
            }

            return null;
        }

        private static void EnsureLoaded()
        {
            lock (Sync)
            {
                if (_current != null)
                    return;

                if (!TryLoad(out _current, out _, out _))
                {
                    _current = new WandProgressionSettings();
                    _current.Normalize();
                }
            }
        }

        private static bool TryLoad(out WandProgressionSettings settings, out bool created, out string error)
        {
            settings = null;
            created = false;
            error = null;

            try
            {
                var path = ConfigPath;
                if (string.IsNullOrEmpty(path))
                {
                    settings = new WandProgressionSettings();
                    settings.Normalize();
                    return true;
                }

                if (!File.Exists(path))
                {
                    settings = new WandProgressionSettings();
                    settings.Normalize();
                    Write(path, settings);
                    created = true;
                    return true;
                }

                var json = File.ReadAllText(path);
                settings = string.IsNullOrWhiteSpace(json)
                    ? new WandProgressionSettings()
                    : JsonConvert.DeserializeObject<WandProgressionSettings>(json) ?? new WandProgressionSettings();
                settings.Normalize();
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }

        private static void Write(string path, WandProgressionSettings settings)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(path, JsonConvert.SerializeObject(settings, Formatting.Indented));
        }
    }
}
