using System;
using System.IO;
using Newtonsoft.Json;

namespace BetterNecromancy
{
    public sealed class ManaHudSettingsData
    {
        public int ConfigVersion { get; set; } = ManaHudSettings.CurrentConfigVersion;
        public string Anchor { get; set; } = "BottomCenter";
        public int OffsetX { get; set; } = 0;
        public int OffsetY { get; set; } = 126;

        public void Normalize()
        {
            ConfigVersion = ManaHudSettings.CurrentConfigVersion;
            Anchor = NormalizeAnchor(Anchor);
            OffsetX = Math.Max(-1200, Math.Min(1200, OffsetX));
            OffsetY = Math.Max(-800, Math.Min(800, OffsetY));
        }

        public static string NormalizeAnchor(string anchor)
        {
            switch ((anchor ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "topleft":
                    return "TopLeft";
                case "topcenter":
                    return "TopCenter";
                case "topright":
                    return "TopRight";
                case "middleleft":
                    return "MiddleLeft";
                case "middlecenter":
                    return "MiddleCenter";
                case "middleright":
                    return "MiddleRight";
                case "bottomleft":
                    return "BottomLeft";
                case "bottomcenter":
                case "bottoncenter":
                    return "BottomCenter";
                case "bottomright":
                    return "BottomRight";
                default:
                    return "BottomCenter";
            }
        }
    }

    public static class ManaHudSettings
    {
        public const int CurrentConfigVersion = 2;
        private const string FileName = "manaHud.json";
        private static readonly object Sync = new object();
        private static ManaHudSettingsData _current;

        public static ManaHudSettingsData Current
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
                if (!TryLoadFromDisk(out var loaded, out var regenerated, out var error))
                {
                    message = "Mana HUD reload failed: " + error;
                    return false;
                }

                _current = loaded;
                message = regenerated
                    ? "Mana HUD config regenerated at " + ConfigPath + "."
                    : "Mana HUD reloaded from " + ConfigPath + ".";
                return true;
            }
        }

        public static bool SetOffsets(int x, int y, out string message)
        {
            lock (Sync)
            {
                try
                {
                    var settings = new ManaHudSettingsData
                    {
                        Anchor = _current?.Anchor ?? "BottomCenter",
                        OffsetX = x,
                        OffsetY = y
                    };
                    settings.Normalize();
                    WriteFile(ConfigPath, settings);
                    _current = settings;
                    message = "Mana HUD moved to " + settings.Anchor + " x=" + settings.OffsetX + ", y=" + settings.OffsetY + ".";
                    return true;
                }
                catch (Exception exception)
                {
                    message = "Saving Mana HUD config failed: " + exception.Message;
                    return false;
                }
            }
        }

        public static bool SetAnchor(string anchor, out string message)
        {
            lock (Sync)
            {
                EnsureLoaded();

                try
                {
                    _current.Anchor = ManaHudSettingsData.NormalizeAnchor(anchor);
                    _current.Normalize();
                    WriteFile(ConfigPath, _current);
                    message = "Mana HUD anchor set to " + _current.Anchor + ".";
                    return true;
                }
                catch (Exception exception)
                {
                    message = "Saving Mana HUD anchor failed: " + exception.Message;
                    return false;
                }
            }
        }

        public static bool Nudge(int dx, int dy, out string message)
        {
            lock (Sync)
            {
                EnsureLoaded();
                return SetOffsets(_current.OffsetX + dx, _current.OffsetY + dy, out message);
            }
        }

        public static bool Reset(out string message)
        {
            lock (Sync)
            {
                try
                {
                    _current = CreateDefault();
                    WriteFile(ConfigPath, _current);
                    message = "Mana HUD reset to " + _current.Anchor + " x=" + _current.OffsetX + ", y=" + _current.OffsetY + ".";
                    return true;
                }
                catch (Exception exception)
                {
                    message = "Saving Mana HUD config failed: " + exception.Message;
                    return false;
                }
            }
        }

        public static bool Rebuild(out string message)
        {
            lock (Sync)
            {
                try
                {
                    var path = ConfigPath;
                    if (!string.IsNullOrEmpty(path) && File.Exists(path))
                        File.Delete(path);

                    _current = CreateDefault();
                    WriteFile(path, _current);
                    message = "Mana HUD config rebuilt from scratch at " + path + ".";
                    return true;
                }
                catch (Exception exception)
                {
                    message = "Rebuilding Mana HUD config failed: " + exception.Message;
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
                    _current = CreateDefault();
                }
            }
        }

        private static bool TryLoadFromDisk(out ManaHudSettingsData settings, out bool regenerated, out string error)
        {
            settings = null;
            regenerated = false;
            error = null;

            try
            {
                var path = ConfigPath;

                if (string.IsNullOrEmpty(path))
                {
                    settings = CreateDefault();
                    return true;
                }

                if (!File.Exists(path))
                {
                    settings = CreateDefault();
                    WriteFile(path, settings);
                    regenerated = true;
                    return true;
                }

                var json = File.ReadAllText(path);
                var loaded = string.IsNullOrWhiteSpace(json)
                    ? null
                    : JsonConvert.DeserializeObject<ManaHudSettingsData>(json);

                if (RequiresHardReset(loaded))
                {
                    settings = CreateDefault();
                    WriteFile(path, settings);
                    regenerated = true;
                    return true;
                }

                settings = loaded;
                var originalAnchor = settings.Anchor;
                var originalOffsetX = settings.OffsetX;
                var originalOffsetY = settings.OffsetY;
                settings.Normalize();

                if (!string.Equals(originalAnchor, settings.Anchor, StringComparison.Ordinal) ||
                    originalOffsetX != settings.OffsetX ||
                    originalOffsetY != settings.OffsetY ||
                    settings.ConfigVersion != CurrentConfigVersion)
                {
                    WriteFile(path, settings);
                }

                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }

        private static ManaHudSettingsData CreateDefault()
        {
            var settings = new ManaHudSettingsData
            {
                ConfigVersion = CurrentConfigVersion,
                Anchor = "BottomCenter",
                OffsetX = 0,
                OffsetY = 126
            };
            settings.Normalize();
            return settings;
        }

        private static bool RequiresHardReset(ManaHudSettingsData settings)
        {
            if (settings == null)
                return true;

            if (settings.ConfigVersion != CurrentConfigVersion)
                return true;

            if (string.IsNullOrWhiteSpace(settings.Anchor))
                return true;

            if (string.Equals(settings.Anchor, "BottomCenter", StringComparison.Ordinal) &&
                ((settings.OffsetX == 90 && settings.OffsetY == 210) ||
                 (settings.OffsetX == 0 && settings.OffsetY == 78)))
            {
                return true;
            }

            if (Math.Abs(settings.OffsetX) > 1200 || Math.Abs(settings.OffsetY) > 800)
                return true;

            return false;
        }

        private static void WriteFile(string path, ManaHudSettingsData settings)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(path, JsonConvert.SerializeObject(settings, Formatting.Indented));
        }
    }
}
