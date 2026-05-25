using System;
using System.IO;
using Newtonsoft.Json;

namespace BetterNecromancy
{
    public sealed class BossTuningSettings
    {
        public int RandomSpawnMinSeconds { get; set; } = 900;
        public int RandomSpawnMaxSeconds { get; set; } = 1800;
        public int BossLootManaMin { get; set; } = 1;
        public int BossLootManaMax { get; set; } = 20;
        public double ZombieAudioIntervalSeconds { get; set; } = 5d;
        public float ZombieAudioHitChance { get; set; } = 0.5f;
        public double FallenRangerCooldownSeconds { get; set; } = 10d;
        public float FallenRangerAttackRange { get; set; } = 30f;
        public float FallenRangerTrailSeconds { get; set; } = 2f;
        public float PutridCorpseAuraDamage { get; set; } = 10f;
        public float PutridCorpseAuraRange { get; set; } = 20f;
        public double PutridCorpseAuraIntervalSeconds { get; set; } = 5d;
        public int BulgingSpawnMinCount { get; set; } = 10;
        public int BulgingSpawnPerRank { get; set; } = 10;
        public int BossSpawnNormalTriesPerBanner { get; set; } = 72;
        public int BossSpawnForcedTriesPerBanner { get; set; } = 260;
        public int BossSpawnExtendedSearchRadius { get; set; } = 4096;
        public float BossSpawnExtendedWalkDistance { get; set; } = 24000f;
        public int BossSpawnExtremeSearchRadius { get; set; } = 16384;
        public float BossSpawnExtremeWalkDistance { get; set; } = 100000f;
        public float BossSpawnMaxLiveWalkDistance { get; set; } = 100000f;
        public float BossSpawnMazeSearchWalkDistance { get; set; } = 150000f;
        public int BossSpawnMazeNodeIterationLimit { get; set; } = 250000;
        public int BossSpawnMaxRetries { get; set; } = 4;
        public double BossSpawnRetryDelaySeconds { get; set; } = 8d;
        public double BossSpawnActivationConfirmSeconds { get; set; } = 5d;

        public void Normalize()
        {
            RandomSpawnMinSeconds = Math.Max(60, RandomSpawnMinSeconds);
            RandomSpawnMaxSeconds = Math.Max(RandomSpawnMinSeconds, RandomSpawnMaxSeconds);
            BossLootManaMin = Math.Max(1, BossLootManaMin);
            BossLootManaMax = Math.Max(BossLootManaMin, BossLootManaMax);
            ZombieAudioIntervalSeconds = Math.Max(1d, ZombieAudioIntervalSeconds);
            ZombieAudioHitChance = UnityEngine.Mathf.Clamp01(ZombieAudioHitChance);
            FallenRangerCooldownSeconds = Math.Max(0.25d, FallenRangerCooldownSeconds);
            FallenRangerAttackRange = Math.Max(1f, FallenRangerAttackRange);
            FallenRangerTrailSeconds = Math.Max(0.1f, FallenRangerTrailSeconds);
            PutridCorpseAuraDamage = Math.Max(1f, PutridCorpseAuraDamage);
            PutridCorpseAuraRange = Math.Max(1f, PutridCorpseAuraRange);
            PutridCorpseAuraIntervalSeconds = Math.Max(0.25d, PutridCorpseAuraIntervalSeconds);
            BulgingSpawnMinCount = Math.Max(1, BulgingSpawnMinCount);
            BulgingSpawnPerRank = Math.Max(1, BulgingSpawnPerRank);
            BossSpawnNormalTriesPerBanner = Math.Max(20, BossSpawnNormalTriesPerBanner);
            BossSpawnForcedTriesPerBanner = Math.Max(BossSpawnNormalTriesPerBanner, BossSpawnForcedTriesPerBanner);
            BossSpawnExtendedSearchRadius = Math.Max(1024, BossSpawnExtendedSearchRadius);
            BossSpawnExtendedWalkDistance = Math.Max(8000f, BossSpawnExtendedWalkDistance);
            BossSpawnExtremeSearchRadius = Math.Max(BossSpawnExtendedSearchRadius, BossSpawnExtremeSearchRadius);
            BossSpawnExtremeWalkDistance = Math.Max(BossSpawnExtendedWalkDistance, BossSpawnExtremeWalkDistance);
            BossSpawnMaxLiveWalkDistance = Math.Max(350f, BossSpawnMaxLiveWalkDistance);
            BossSpawnMazeSearchWalkDistance = Math.Max(BossSpawnMaxLiveWalkDistance + 500f, BossSpawnMazeSearchWalkDistance);
            BossSpawnMazeNodeIterationLimit = Math.Max(15000, BossSpawnMazeNodeIterationLimit);
            BossSpawnMaxRetries = Math.Max(1, BossSpawnMaxRetries);
            BossSpawnRetryDelaySeconds = Math.Max(2d, BossSpawnRetryDelaySeconds);
            BossSpawnActivationConfirmSeconds = Math.Max(2d, BossSpawnActivationConfirmSeconds);
        }
    }

    public static class BossTuning
    {
        private const string FileName = "bossTuning.json";
        private static readonly object Sync = new object();
        private static BossTuningSettings _current;

        public static BossTuningSettings Current
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

        public static string[] GetPresetNames()
        {
            return new[] { "leicht", "normal", "brutal" };
        }

        public static bool Reload(out string message)
        {
            lock (Sync)
            {
                if (!TryLoadFromDisk(out var loaded, out var created, out var error))
                {
                    message = "Boss tuning reload failed: " + error;
                    return false;
                }

                _current = loaded;
                message = created
                    ? "Boss tuning created at " + ConfigPath + "."
                    : "Boss tuning reloaded from " + ConfigPath + ".";
                return true;
            }
        }

        public static bool TryApplyPreset(string presetName, out string message)
        {
            lock (Sync)
            {
                var canonicalPreset = NormalizePresetName(presetName);
                if (canonicalPreset == null)
                {
                    message = "Unknown boss preset. Available: " + string.Join(", ", GetPresetNames()) + ".";
                    return false;
                }

                try
                {
                    var settings = CreatePreset(canonicalPreset);
                    settings.Normalize();
                    WriteDefaultFile(ConfigPath, settings);
                    _current = settings;
                    message = "Boss preset '" + canonicalPreset + "' applied to " + ConfigPath + ".";
                    return true;
                }
                catch (Exception exception)
                {
                    message = "Applying boss preset failed: " + exception.Message;
                    return false;
                }
            }
        }

        private static void EnsureLoaded()
        {
            lock (Sync)
            {
                if (_current != null)
                    return;

                if (!TryLoadFromDisk(out _current, out _, out _))
                {
                    _current = CreatePreset("normal");
                    _current.Normalize();
                }
            }
        }

        private static bool TryLoadFromDisk(out BossTuningSettings settings, out bool created, out string error)
        {
            settings = null;
            created = false;
            error = null;

            try
            {
                var path = ConfigPath;

                if (string.IsNullOrEmpty(path))
                {
                    settings = CreatePreset("normal");
                    settings.Normalize();
                    return true;
                }

                if (!File.Exists(path))
                {
                    settings = CreatePreset("normal");
                    settings.Normalize();
                    WriteDefaultFile(path, settings);
                    created = true;
                    return true;
                }

                var json = File.ReadAllText(path);
                settings = string.IsNullOrWhiteSpace(json)
                    ? CreatePreset("normal")
                    : JsonConvert.DeserializeObject<BossTuningSettings>(json) ?? CreatePreset("normal");
                settings.Normalize();
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }

        private static void WriteDefaultFile(string path, BossTuningSettings settings)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(path, JsonConvert.SerializeObject(settings, Formatting.Indented));
        }

        private static string NormalizePresetName(string presetName)
        {
            if (string.IsNullOrWhiteSpace(presetName))
                return null;

            switch (presetName.Trim().ToLowerInvariant())
            {
                case "leicht":
                case "easy":
                    return "leicht";

                case "normal":
                case "mittel":
                    return "normal";

                case "brutal":
                case "hard":
                case "schwer":
                    return "brutal";

                default:
                    return null;
            }
        }

        private static BossTuningSettings CreatePreset(string presetName)
        {
            switch (presetName)
            {
                case "leicht":
                    return new BossTuningSettings
                    {
                        RandomSpawnMinSeconds = 1500,
                        RandomSpawnMaxSeconds = 2400,
                        BossLootManaMin = 1,
                        BossLootManaMax = 16,
                        ZombieAudioIntervalSeconds = 7d,
                        ZombieAudioHitChance = 0.30f,
                        FallenRangerCooldownSeconds = 14d,
                        FallenRangerAttackRange = 24f,
                        FallenRangerTrailSeconds = 1.75f,
                        PutridCorpseAuraDamage = 6f,
                        PutridCorpseAuraRange = 14f,
                        PutridCorpseAuraIntervalSeconds = 7d,
                        BulgingSpawnMinCount = 6,
                        BulgingSpawnPerRank = 6
                    };

                case "brutal":
                    return new BossTuningSettings
                    {
                        RandomSpawnMinSeconds = 600,
                        RandomSpawnMaxSeconds = 1200,
                        BossLootManaMin = 4,
                        BossLootManaMax = 30,
                        ZombieAudioIntervalSeconds = 3.5d,
                        ZombieAudioHitChance = 0.75f,
                        FallenRangerCooldownSeconds = 6d,
                        FallenRangerAttackRange = 36f,
                        FallenRangerTrailSeconds = 2.5f,
                        PutridCorpseAuraDamage = 16f,
                        PutridCorpseAuraRange = 24f,
                        PutridCorpseAuraIntervalSeconds = 3d,
                        BulgingSpawnMinCount = 16,
                        BulgingSpawnPerRank = 14
                    };

                default:
                    return new BossTuningSettings
                    {
                        RandomSpawnMinSeconds = 900,
                        RandomSpawnMaxSeconds = 1800,
                        BossLootManaMin = 1,
                        BossLootManaMax = 20,
                        ZombieAudioIntervalSeconds = 5d,
                        ZombieAudioHitChance = 0.5f,
                        FallenRangerCooldownSeconds = 10d,
                        FallenRangerAttackRange = 30f,
                        FallenRangerTrailSeconds = 2f,
                        PutridCorpseAuraDamage = 10f,
                        PutridCorpseAuraRange = 20f,
                        PutridCorpseAuraIntervalSeconds = 5d,
                        BulgingSpawnMinCount = 10,
                        BulgingSpawnPerRank = 10
                    };
            }
        }
    }
}
