using System;
using System.Globalization;
using System.Reflection;

namespace BetterNecromancy
{
    public enum BetterGuardsBonusMode
    {
        None,
        NightByJobBlock,
        KillByJobBlock
    }

    public static class BetterGuardsBridge
    {
        private const float LegacyKillBonusPercentPerKill = 0.001f;
        private const float ModernKillBonusPercentPerKill = 0.2f;

        public static bool IsAvailable => CurrentBonusMode != BetterGuardsBonusMode.None;

        public static bool HasRangeBonus => IsAvailable;

        public static float GetKillBonusPercentPerKill()
        {
            var assembly = ResolveBetterGuardsAssembly();
            if (assembly == null)
                return LegacyKillBonusPercentPerKill;

            var killBonusType = ResolveKillBonusType(assembly);
            if (killBonusType == null)
                return LegacyKillBonusPercentPerKill;

            var getter = killBonusType.GetMethod("GetCurrentBonusPercentPerKill", BindingFlags.Public | BindingFlags.Static);
            if (getter != null && getter.ReturnType == typeof(float))
                return (float)getter.Invoke(null, null);

            var defaultField = killBonusType.GetField("DefaultBonusPercentPerKill", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            if (defaultField != null)
                return Convert.ToSingle(defaultField.GetValue(null), CultureInfo.InvariantCulture);

            return string.Equals(killBonusType.FullName, "Xell.GuardKillBonus.GuardKillBonusByJobBlock", StringComparison.Ordinal)
                ? LegacyKillBonusPercentPerKill
                : ModernKillBonusPercentPerKill;
        }

        public static float GetKillBonusMultiplierPerKill()
        {
            return GetKillBonusPercentPerKill() / 100f;
        }

        public static BetterGuardsBonusMode CurrentBonusMode
        {
            get
            {
                var assembly = ResolveBetterGuardsAssembly();
                if (assembly == null)
                    return BetterGuardsBonusMode.None;

                if (ResolveKillBonusType(assembly) != null)
                    return BetterGuardsBonusMode.KillByJobBlock;

                if (ResolveNightBonusType(assembly) != null)
                    return BetterGuardsBonusMode.NightByJobBlock;

                return BetterGuardsBonusMode.None;
            }
        }

        private static Type ResolveKillBonusType(Assembly assembly)
        {
            var explicitKillType = assembly.GetType("Xell.GuardKillBonus.GuardKillBonusByJobBlock", false);
            if (explicitKillType != null)
                return explicitKillType;

            var legacyGlobalType = assembly.GetType("GuardNightBonus", false);
            if (HasKillBonusApi(legacyGlobalType))
                return legacyGlobalType;

            return null;
        }

        private static Type ResolveNightBonusType(Assembly assembly)
        {
            var namespacedNightType = assembly.GetType("XellMods.GuardNightBonus", false);
            if (namespacedNightType != null)
                return namespacedNightType;

            var globalType = assembly.GetType("GuardNightBonus", false);
            if (globalType != null && !HasKillBonusApi(globalType))
                return globalType;

            return null;
        }

        private static bool HasKillBonusApi(Type type)
        {
            if (type == null)
                return false;

            return type.GetMethod("GetCurrentBonusPercentPerKill", BindingFlags.Public | BindingFlags.Static) != null ||
                   type.GetMethod("TrySetBonusPercentPerKillFromChat", BindingFlags.Public | BindingFlags.Static) != null ||
                   type.GetField("DefaultBonusPercentPerKill", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy) != null;
        }

        private static System.Reflection.Assembly ResolveBetterGuardsAssembly()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (var i = 0; i < assemblies.Length; i++)
            {
                var assembly = assemblies[i];
                var name = assembly.GetName().Name;
                if (string.Equals(name, "BetterGuardKillTracker", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(name, "BetterGuard", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(name, "BetterGuard3.5", StringComparison.OrdinalIgnoreCase))
                {
                    return assembly;
                }
            }

            return null;
        }
    }
}
