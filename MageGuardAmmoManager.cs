using System;
using System.Collections.Generic;

namespace BetterNecromancy
{
    [ModLoader.ModManager]
    public static class MageGuardAmmoManager
    {
        private const string AmmoTypeKey = ModEntry.Namespace + ".MageGuardCharge";
        private const int SupplyAmountPerTick = 5000;
        private const int TargetAmmoReserve = 1000000;
        private const double SupplyTickSeconds = 20d;

        private static ushort _ammoItemIndex = ushort.MaxValue;
        private static double _nextSupplyAt = double.MaxValue;

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AfterWorldLoad, ModEntry.Namespace + ".MageGuardAmmoManager.AfterWorldLoad")]
        public static void AfterWorldLoad()
        {
            _ammoItemIndex = ushort.MaxValue;
            _nextSupplyAt = 0d;
            TryResolveAmmoType();
            SeedAllColonies();
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnUpdate, ModEntry.Namespace + ".MageGuardAmmoManager.OnUpdate")]
        public static void OnUpdate()
        {
            if (!World.Initialized)
                return;

            var now = Pipliz.Time.SecondsSinceStartDouble;
            if (now < _nextSupplyAt)
                return;

            _nextSupplyAt = now + SupplyTickSeconds;

            if (!TryResolveAmmoType())
                return;

            SeedAllColonies();
        }

        private static bool TryResolveAmmoType()
        {
            if (_ammoItemIndex != ushort.MaxValue)
                return true;

            if (!ItemTypes.TryGetType(AmmoTypeKey, out var ammoType) || ammoType == null)
                return false;

            _ammoItemIndex = ammoType.ItemIndex;
            return true;
        }

        private static void SeedAllColonies()
        {
            if (_ammoItemIndex == ushort.MaxValue)
                return;

            var primaryColonies = new Dictionary<ColonyID, Colony>();
            var colonies = ServerManager.ColonyTracker.ColoniesByID.GetValueEnumerator();
            while (colonies.MoveNext())
            {
                var colony = ResolvePrimaryAmmoColony(colonies.Current);
                if (!CanSeedAmmoForColony(colony))
                    continue;

                if (!primaryColonies.ContainsKey(colony.ColonyID))
                    primaryColonies[colony.ColonyID] = colony;
            }

            foreach (var pair in primaryColonies)
            {
                var colony = pair.Value;
                var currentAmount = colony.ColonyGroup.Stockpile.AmountContained(_ammoItemIndex);
                var missingAmount = System.Math.Max(0, TargetAmmoReserve - currentAmount);
                var amount = System.Math.Min(SupplyAmountPerTick, missingAmount);
                if (amount <= 0)
                    continue;

                // Keep mage guards on the vanilla guard-ammo path, but refill this
                // internal resource automatically so players do not need to manage ammo.
                colony.ColonyGroup.Stockpile.Add(_ammoItemIndex, amount);
            }
        }

        private static Colony ResolvePrimaryAmmoColony(Colony colony)
        {
            if (colony == null)
                return null;

            return colony.ColonyGroup?.MainColony ?? colony;
        }

        private static bool CanSeedAmmoForColony(Colony colony)
        {
            return colony != null &&
                   colony.Banners.Count > 0 &&
                   colony.ColonyGroup?.Stockpile != null &&
                   colony.ColonyGroup.MainColonyID == colony.ColonyID;
        }
    }
}
