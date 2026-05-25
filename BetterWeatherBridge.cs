using System;
using System.Reflection;

namespace BetterNecromancy
{
    public static class BetterWeatherBridge
    {
        public static bool IsAvailable => ResolveWeatherModEntryType() != null && ResolveWeatherKindType() != null && ResolveForceWeatherMethod() != null;

        public static bool TryForceWeather(string weatherKindName)
        {
            if (!IsAvailable || string.IsNullOrWhiteSpace(weatherKindName))
                return false;

            try
            {
                var weatherKind = Enum.Parse(ResolveWeatherKindType(), weatherKindName, true);
                ResolveForceWeatherMethod().Invoke(null, new[] { weatherKind });
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool TrySetEnabled(bool enabled)
        {
            var method = ResolveSetEnabledMethod();
            if (method == null)
                return false;

            try
            {
                method.Invoke(null, new object[] { enabled });
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static string TryGetStatusText(Players.Player player)
        {
            var method = ResolveGetStatusTextMethod();
            if (method == null)
                return "BetterWeather not found.";

            try
            {
                return method.Invoke(null, new object[] { player }) as string ?? "BetterWeather status unavailable.";
            }
            catch
            {
                return "BetterWeather status unavailable.";
            }
        }

        public static bool TrySetVisualTheme(string themeName)
        {
            var method = ResolveSetExternalVisualThemeMethod();
            if (method == null || string.IsNullOrWhiteSpace(themeName))
                return false;

            try
            {
                method.Invoke(null, new object[] { themeName });
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool TryClearVisualTheme()
        {
            var method = ResolveClearExternalVisualThemeMethod();
            if (method == null)
                return false;

            try
            {
                method.Invoke(null, null);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool TrySetCloudTintTheme(string themeName)
        {
            var method = ResolveSetCloudTintThemeMethod();
            var themeType = ResolveCloudTintThemeType();
            if (method == null || themeType == null || string.IsNullOrWhiteSpace(themeName))
                return false;

            try
            {
                var theme = Enum.Parse(themeType, themeName, true);
                method.Invoke(null, new[] { theme });
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool TryGetCurrentWeatherKind(out string weatherKindName, out bool enabled)
        {
            weatherKindName = null;
            enabled = false;

            var method = ResolveGetSnapshotMethod();
            if (method == null)
                return false;

            try
            {
                var snapshot = method.Invoke(null, null);
                if (snapshot == null)
                    return false;

                var snapshotType = snapshot.GetType();
                var currentWeatherField = snapshotType.GetField("CurrentWeather", BindingFlags.Public | BindingFlags.Instance);
                var enabledField = snapshotType.GetField("Enabled", BindingFlags.Public | BindingFlags.Instance);

                weatherKindName = currentWeatherField?.GetValue(snapshot)?.ToString();
                if (enabledField?.FieldType == typeof(bool))
                    enabled = (bool)enabledField.GetValue(snapshot);

                return !string.IsNullOrWhiteSpace(weatherKindName);
            }
            catch
            {
                return false;
            }
        }

        private static Type ResolveWeatherModEntryType()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (var i = 0; i < assemblies.Length; i++)
            {
                var assembly = assemblies[i];
                if (!string.Equals(assembly.GetName().Name, "BetterWeather", StringComparison.OrdinalIgnoreCase))
                    continue;

                return assembly.GetType("BetterWeather.WeatherModEntry", false);
            }

            return Type.GetType("BetterWeather.WeatherModEntry, BetterWeather", false);
        }

        private static Type ResolveWeatherKindType()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (var i = 0; i < assemblies.Length; i++)
            {
                var assembly = assemblies[i];
                if (!string.Equals(assembly.GetName().Name, "BetterWeather", StringComparison.OrdinalIgnoreCase))
                    continue;

                return assembly.GetType("BetterWeather.WeatherKind", false);
            }

            return Type.GetType("BetterWeather.WeatherKind, BetterWeather", false);
        }

        private static MethodInfo ResolveForceWeatherMethod()
        {
            return ResolveWeatherModEntryType()?.GetMethod("ForceWeather", BindingFlags.Public | BindingFlags.Static);
        }

        private static MethodInfo ResolveSetEnabledMethod()
        {
            return ResolveWeatherModEntryType()?.GetMethod("SetEnabled", BindingFlags.Public | BindingFlags.Static);
        }

        private static MethodInfo ResolveSetExternalVisualThemeMethod()
        {
            return ResolveWeatherModEntryType()?.GetMethod("SetExternalVisualTheme", BindingFlags.Public | BindingFlags.Static);
        }

        private static MethodInfo ResolveClearExternalVisualThemeMethod()
        {
            return ResolveWeatherModEntryType()?.GetMethod("ClearExternalVisualTheme", BindingFlags.Public | BindingFlags.Static);
        }

        private static MethodInfo ResolveGetStatusTextMethod()
        {
            return ResolveWeatherModEntryType()?.GetMethod("GetStatusText", BindingFlags.Public | BindingFlags.Static);
        }

        private static Type ResolveCloudTintThemeType()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (var i = 0; i < assemblies.Length; i++)
            {
                var assembly = assemblies[i];
                if (!string.Equals(assembly.GetName().Name, "BetterWeather", StringComparison.OrdinalIgnoreCase))
                    continue;

                return assembly.GetType("BetterWeather.WeatherCloudTintTheme", false);
            }

            return Type.GetType("BetterWeather.WeatherCloudTintTheme, BetterWeather", false);
        }

        private static MethodInfo ResolveGetSnapshotMethod()
        {
            return ResolveWeatherModEntryType()?.GetMethod("GetSnapshot", BindingFlags.Public | BindingFlags.Static);
        }

        private static MethodInfo ResolveSetCloudTintThemeMethod()
        {
            return ResolveWeatherModEntryType()?.GetMethod("SetCloudTintTheme", BindingFlags.Public | BindingFlags.Static);
        }
    }
}
