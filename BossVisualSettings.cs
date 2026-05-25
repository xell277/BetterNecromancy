using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace BetterNecromancy
{
    public sealed class BossVisualProfile
    {
        public string BossName { get; set; } = string.Empty;
        public float PositionOffsetX { get; set; } = 0f;
        public float PositionOffsetY { get; set; } = 0f;
        public float PositionOffsetZ { get; set; } = 0f;
        public float PositionOffsetRight { get; set; } = 0f;
        public float PositionOffsetForward { get; set; } = 0f;
        public float RotationYawDegrees { get; set; } = 0f;
        public float FollowLeadSeconds { get; set; } = 0.20f;
        public float PositionSmoothing { get; set; } = 0.50f;
        public float RotationSmoothing { get; set; } = 0.50f;
        public bool UseBakedVisual { get; set; } = false;
        public bool LockVisualToDriver { get; set; } = false;

        [JsonIgnore]
        public Vector3 PositionOffset => new Vector3(PositionOffsetX, PositionOffsetY, PositionOffsetZ);

        public void Normalize()
        {
            BossName = BossName?.Trim() ?? string.Empty;
            FollowLeadSeconds = Mathf.Clamp(FollowLeadSeconds, 0f, 1f);
            PositionSmoothing = Mathf.Clamp(PositionSmoothing, 0.01f, 1f);
            RotationSmoothing = Mathf.Clamp(RotationSmoothing, 0.01f, 1f);
        }
    }

    public sealed class BossVisualSettingsFile
    {
        public bool EnableCustomBossVisualProxy { get; set; } = false;
        public List<BossVisualProfile> Bosses { get; set; } = new List<BossVisualProfile>();

        public void Normalize()
        {
            if (Bosses == null)
                Bosses = new List<BossVisualProfile>();

            for (var i = 0; i < Bosses.Count; i++)
                Bosses[i]?.Normalize();
        }
    }

    public static class BossVisualSettings
    {
        private const string FileName = "bossVisuals.json";
        private static readonly object Sync = new object();
        private static BossVisualSettingsFile _current;
        private static readonly BossVisualProfile DefaultProfile = new BossVisualProfile();

        public static string ConfigPath =>
            string.IsNullOrEmpty(ModEntry.ModFolder)
                ? FileName
                : Path.Combine(ModEntry.ModFolder, FileName);

        public static BossVisualProfile GetProfile(string bossName)
        {
            EnsureLoaded();

            if (string.IsNullOrWhiteSpace(bossName) || _current?.Bosses == null)
                return DefaultProfile;

            for (var i = 0; i < _current.Bosses.Count; i++)
            {
                var profile = _current.Bosses[i];
                if (profile != null && string.Equals(profile.BossName, bossName, StringComparison.OrdinalIgnoreCase))
                    return profile;
            }

            return DefaultProfile;
        }

        public static bool CustomBossVisualProxyEnabled
        {
            get
            {
                EnsureLoaded();
                return _current?.EnableCustomBossVisualProxy ?? false;
            }
        }

        public static bool Reload(out string message)
        {
            lock (Sync)
            {
                if (!TryLoadFromDisk(out var loaded, out var created, out var error))
                {
                    message = "Boss visual settings reload failed: " + error;
                    return false;
                }

                _current = loaded;
                message = created
                    ? "Boss visual settings created at " + ConfigPath + "."
                    : "Boss visual settings reloaded from " + ConfigPath + ".";
                return true;
            }
        }

        private static void EnsureLoaded()
        {
            lock (Sync)
            {
                if (_current != null)
                    return;

                if (!TryLoadFromDisk(out _current, out _, out _))
                    _current = CreateDefault();
            }
        }

        private static bool TryLoadFromDisk(out BossVisualSettingsFile settings, out bool created, out string error)
        {
            settings = null;
            created = false;
            error = null;

            try
            {
                var path = ConfigPath;

                if (string.IsNullOrEmpty(path))
                {
                    settings = CreateDefault();
                    settings.Normalize();
                    return true;
                }

                if (!File.Exists(path))
                {
                    settings = CreateDefault();
                    settings.Normalize();
                    WriteDefaultFile(path, settings);
                    created = true;
                    return true;
                }

                var json = File.ReadAllText(path);
                settings = string.IsNullOrWhiteSpace(json)
                    ? CreateDefault()
                    : JsonConvert.DeserializeObject<BossVisualSettingsFile>(json) ?? CreateDefault();
                settings.Normalize();
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }

        private static void WriteDefaultFile(string path, BossVisualSettingsFile settings)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(path, JsonConvert.SerializeObject(settings, Formatting.Indented));
        }

        private static BossVisualSettingsFile CreateDefault()
        {
            return new BossVisualSettingsFile
            {
                Bosses = new List<BossVisualProfile>
                {
                    new BossVisualProfile
                    {
                        BossName = "Bulging",
                        PositionOffsetY = -0.55f,
                        PositionOffsetForward = 0f,
                        RotationYawDegrees = -90f,
                        FollowLeadSeconds = 0f,
                        PositionSmoothing = 0.55f,
                        RotationSmoothing = 0.92f,
                        UseBakedVisual = false,
                        LockVisualToDriver = false
                    },
                    new BossVisualProfile
                    {
                        BossName = "Fallen Ranger",
                        PositionOffsetY = -0.55f,
                        PositionOffsetForward = 0f,
                        RotationYawDegrees = -90f,
                        FollowLeadSeconds = 0f,
                        PositionSmoothing = 0.55f,
                        RotationSmoothing = 0.92f,
                        UseBakedVisual = false,
                        LockVisualToDriver = false
                    },
                    new BossVisualProfile
                    {
                        BossName = "Hoarder",
                        PositionOffsetY = -0.55f,
                        PositionOffsetForward = 0f,
                        RotationYawDegrees = -90f,
                        FollowLeadSeconds = 0f,
                        PositionSmoothing = 0.55f,
                        RotationSmoothing = 0.92f,
                        UseBakedVisual = false,
                        LockVisualToDriver = false
                    },
                    new BossVisualProfile
                    {
                        BossName = "Jack-b-Nimble",
                        PositionOffsetY = -0.55f,
                        PositionOffsetForward = 0f,
                        RotationYawDegrees = -90f,
                        FollowLeadSeconds = 0f,
                        PositionSmoothing = 0.55f,
                        RotationSmoothing = 0.92f,
                        UseBakedVisual = false,
                        LockVisualToDriver = false
                    },
                    new BossVisualProfile
                    {
                        BossName = "Juggernaut",
                        PositionOffsetY = -0.55f,
                        PositionOffsetForward = 0f,
                        RotationYawDegrees = -90f,
                        FollowLeadSeconds = 0f,
                        PositionSmoothing = 0.55f,
                        RotationSmoothing = 0.92f,
                        UseBakedVisual = false,
                        LockVisualToDriver = false
                    },
                    new BossVisualProfile
                    {
                        BossName = "Phase",
                        PositionOffsetY = -0.55f,
                        PositionOffsetForward = 0f,
                        RotationYawDegrees = -90f,
                        FollowLeadSeconds = 0f,
                        PositionSmoothing = 0.55f,
                        RotationSmoothing = 0.92f,
                        UseBakedVisual = false,
                        LockVisualToDriver = false
                    },
                    new BossVisualProfile
                    {
                        BossName = "Putrid Corpse",
                        PositionOffsetY = -0.55f,
                        PositionOffsetForward = 0f,
                        RotationYawDegrees = -90f,
                        FollowLeadSeconds = 0f,
                        PositionSmoothing = 0.55f,
                        RotationSmoothing = 0.92f,
                        UseBakedVisual = false,
                        LockVisualToDriver = false
                    },
                    new BossVisualProfile
                    {
                        BossName = "ZombieKing",
                        PositionOffsetY = -0.55f,
                        PositionOffsetForward = 0f,
                        RotationYawDegrees = -90f,
                        FollowLeadSeconds = 0f,
                        PositionSmoothing = 0.55f,
                        RotationSmoothing = 0.92f,
                        UseBakedVisual = false,
                        LockVisualToDriver = false
                    },
                    new BossVisualProfile
                    {
                        BossName = "ZombieQueen",
                        PositionOffsetY = -0.55f,
                        PositionOffsetForward = 0f,
                        RotationYawDegrees = -90f,
                        FollowLeadSeconds = 0f,
                        PositionSmoothing = 0.55f,
                        RotationSmoothing = 0.92f,
                        UseBakedVisual = false,
                        LockVisualToDriver = false
                    }
                }
            };
        }
    }
}
