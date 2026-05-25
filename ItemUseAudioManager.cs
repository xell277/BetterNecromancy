using UnityEngine;

namespace BetterNecromancy
{
    [ModLoader.ModManager]
    internal static class ItemUseAudioManager
    {
        internal const string ManaBottleUse = "bn_item_mana_bottle_use";
        internal const string ManaCrystalAbsorb = "bn_item_mana_crystal_absorb";
        internal const string HealthBoosterEquip = "bn_item_health_booster_equip";
        internal const string BootsEquip = "bn_item_boots_equip";
        internal const string FocusEquip = "bn_item_focus_equip";
        internal const string SwordEquip = "bn_item_sword_equip";
        internal const string ArmorEquip = "bn_item_armor_equip";
        internal const string BandageUse = "bn_item_bandage_use";
        internal const string TreatedBandageUse = "bn_item_treated_bandage_use";
        internal const string ManaFlightCrash = "bn_manaflight_crash";

        public static void Play(Players.Player player, string audioCollection)
        {
            if (player == null)
                return;

            Play(player, audioCollection, player.PositionStanding + Vector3.up * 0.9f);
        }

        public static void Play(Players.Player player, string audioCollection, Vector3 origin)
        {
            if (player == null || string.IsNullOrWhiteSpace(audioCollection))
                return;

            try
            {
                AudioManager.SendAudio(origin, audioCollection);
            }
            catch
            {
            }
        }
    }
}
