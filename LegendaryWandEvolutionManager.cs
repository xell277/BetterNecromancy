using System;
using System.Collections.Generic;
using System.Linq;
using Pandaros.API.Entities;
using Shared;

namespace BetterNecromancy
{
    public sealed class LegendaryWandRequirement
    {
        public string ItemKey { get; set; }
        public int Amount { get; set; }
    }

    public sealed class LegendaryWandProfile
    {
        public string MasteryKey { get; set; }
        public string LegendaryName { get; set; }
        public string Summary { get; set; }
        public List<LegendaryWandRequirement> Requirements { get; set; } = new List<LegendaryWandRequirement>();
    }

    public static class LegendaryWandEvolutionManager
    {
        private static readonly LegendaryWandProfile[] Profiles =
        {
            new LegendaryWandProfile
            {
                MasteryKey = PlayerMagicStateManager.WandMasteryKeys.Mana,
                LegendaryName = "Astral Mana Wand",
                Summary = "+12% damage, +1 extra chain target, -1 mana cost and astral rebound pulses.",
                Requirements = new List<LegendaryWandRequirement>
                {
                    new LegendaryWandRequirement { ItemKey = "Mana", Amount = 30 },
                    new LegendaryWandRequirement { ItemKey = "Esper", Amount = 8 },
                    new LegendaryWandRequirement { ItemKey = "Elementium", Amount = 4 },
                    new LegendaryWandRequirement { ItemKey = "ManaCrystal", Amount = 1 }
                }
            },
            new LegendaryWandProfile
            {
                MasteryKey = PlayerMagicStateManager.WandMasteryKeys.Briar,
                LegendaryName = "Thornheart Wand",
                Summary = "+25% thorn damage over time, wider bramble bursts and thornheart patches.",
                Requirements = new List<LegendaryWandRequirement>
                {
                    new LegendaryWandRequirement { ItemKey = "Mana", Amount = 28 },
                    new LegendaryWandRequirement { ItemKey = "Aether", Amount = 8 },
                    new LegendaryWandRequirement { ItemKey = "EarthStone", Amount = 2 },
                    new LegendaryWandRequirement { ItemKey = "WaterStone", Amount = 1 },
                    new LegendaryWandRequirement { ItemKey = "ManaCrystal", Amount = 1 }
                }
            },
            new LegendaryWandProfile
            {
                MasteryKey = PlayerMagicStateManager.WandMasteryKeys.Spark,
                LegendaryName = "Voltspine Wand",
                Summary = "+1 chain target, -60ms cooldown, sharper lightning damage and cross-arcs.",
                Requirements = new List<LegendaryWandRequirement>
                {
                    new LegendaryWandRequirement { ItemKey = "Mana", Amount = 28 },
                    new LegendaryWandRequirement { ItemKey = "Esper", Amount = 10 },
                    new LegendaryWandRequirement { ItemKey = "AirStone", Amount = 2 },
                    new LegendaryWandRequirement { ItemKey = "Elementium", Amount = 4 },
                    new LegendaryWandRequirement { ItemKey = "ManaCrystal", Amount = 1 }
                }
            },
            new LegendaryWandProfile
            {
                MasteryKey = PlayerMagicStateManager.WandMasteryKeys.Venom,
                LegendaryName = "Widowroot Wand",
                Summary = "+22% poison damage over time, larger toxic blooms and widowroot spillover.",
                Requirements = new List<LegendaryWandRequirement>
                {
                    new LegendaryWandRequirement { ItemKey = "Mana", Amount = 30 },
                    new LegendaryWandRequirement { ItemKey = "Aether", Amount = 10 },
                    new LegendaryWandRequirement { ItemKey = "WaterStone", Amount = 2 },
                    new LegendaryWandRequirement { ItemKey = "Void", Amount = 1 },
                    new LegendaryWandRequirement { ItemKey = "ManaCrystal", Amount = 1 }
                }
            },
            new LegendaryWandProfile
            {
                MasteryKey = PlayerMagicStateManager.WandMasteryKeys.Ember,
                LegendaryName = "Cinderlord Wand",
                Summary = "+24% burn damage over time, wider infernos, hotter direct hits and ember novas.",
                Requirements = new List<LegendaryWandRequirement>
                {
                    new LegendaryWandRequirement { ItemKey = "Mana", Amount = 30 },
                    new LegendaryWandRequirement { ItemKey = "FireStone", Amount = 2 },
                    new LegendaryWandRequirement { ItemKey = "Elementium", Amount = 6 },
                    new LegendaryWandRequirement { ItemKey = "ManaCrystal", Amount = 1 }
                }
            },
            new LegendaryWandProfile
            {
                MasteryKey = PlayerMagicStateManager.WandMasteryKeys.Frost,
                LegendaryName = "Winterglass Wand",
                Summary = "+20% impact force, -60ms cooldown, larger shatter blasts and winterglass splits.",
                Requirements = new List<LegendaryWandRequirement>
                {
                    new LegendaryWandRequirement { ItemKey = "Mana", Amount = 30 },
                    new LegendaryWandRequirement { ItemKey = "WaterStone", Amount = 2 },
                    new LegendaryWandRequirement { ItemKey = "AirStone", Amount = 1 },
                    new LegendaryWandRequirement { ItemKey = "Elementium", Amount = 6 },
                    new LegendaryWandRequirement { ItemKey = "ManaCrystal", Amount = 1 }
                }
            },
            new LegendaryWandProfile
            {
                MasteryKey = PlayerMagicStateManager.WandMasteryKeys.Crystal,
                LegendaryName = "Prism Sovereign",
                Summary = "+10% damage, +4 range, broader prism lashes and prism lattice echoes.",
                Requirements = new List<LegendaryWandRequirement>
                {
                    new LegendaryWandRequirement { ItemKey = "Mana", Amount = 32 },
                    new LegendaryWandRequirement { ItemKey = "Esper", Amount = 8 },
                    new LegendaryWandRequirement { ItemKey = "WaterStone", Amount = 2 },
                    new LegendaryWandRequirement { ItemKey = "Elementium", Amount = 8 },
                    new LegendaryWandRequirement { ItemKey = "ManaCrystal", Amount = 1 }
                }
            },
            new LegendaryWandProfile
            {
                MasteryKey = PlayerMagicStateManager.WandMasteryKeys.Stone,
                LegendaryName = "Worldbreaker Wand",
                Summary = "+12% damage, +20% force, heavier quake reach and worldbreaker aftershocks.",
                Requirements = new List<LegendaryWandRequirement>
                {
                    new LegendaryWandRequirement { ItemKey = "Mana", Amount = 34 },
                    new LegendaryWandRequirement { ItemKey = "EarthStone", Amount = 2 },
                    new LegendaryWandRequirement { ItemKey = "Adamantine", Amount = 2 },
                    new LegendaryWandRequirement { ItemKey = "Elementium", Amount = 8 },
                    new LegendaryWandRequirement { ItemKey = "ManaCrystal", Amount = 1 }
                }
            },
            new LegendaryWandProfile
            {
                MasteryKey = PlayerMagicStateManager.WandMasteryKeys.Storm,
                LegendaryName = "Skyfall Wand",
                Summary = "+1 extra chain target, wider Tempest coverage, stronger strikes and skyfall call-downs.",
                Requirements = new List<LegendaryWandRequirement>
                {
                    new LegendaryWandRequirement { ItemKey = "Mana", Amount = 34 },
                    new LegendaryWandRequirement { ItemKey = "Esper", Amount = 12 },
                    new LegendaryWandRequirement { ItemKey = "AirStone", Amount = 2 },
                    new LegendaryWandRequirement { ItemKey = "FireStone", Amount = 1 },
                    new LegendaryWandRequirement { ItemKey = "ManaCrystal", Amount = 1 }
                }
            },
            new LegendaryWandProfile
            {
                MasteryKey = PlayerMagicStateManager.WandMasteryKeys.Aether,
                LegendaryName = "Seraph Wand",
                Summary = "+18% healing, -1 mana cost, one more support-wave target and a seraph echo wave.",
                Requirements = new List<LegendaryWandRequirement>
                {
                    new LegendaryWandRequirement { ItemKey = "Mana", Amount = 34 },
                    new LegendaryWandRequirement { ItemKey = "Aether", Amount = 12 },
                    new LegendaryWandRequirement { ItemKey = "WaterStone", Amount = 2 },
                    new LegendaryWandRequirement { ItemKey = "Elementium", Amount = 8 },
                    new LegendaryWandRequirement { ItemKey = "ManaCrystal", Amount = 1 }
                }
            },
            new LegendaryWandProfile
            {
                MasteryKey = PlayerMagicStateManager.WandMasteryKeys.Blood,
                LegendaryName = "Crimson Covenant",
                Summary = "+15% lifesteal, +18% bleed damage over time, hungrier burst damage and covenant siphons.",
                Requirements = new List<LegendaryWandRequirement>
                {
                    new LegendaryWandRequirement { ItemKey = "Mana", Amount = 36 },
                    new LegendaryWandRequirement { ItemKey = "Void", Amount = 3 },
                    new LegendaryWandRequirement { ItemKey = "Adamantine", Amount = 3 },
                    new LegendaryWandRequirement { ItemKey = "FireStone", Amount = 1 },
                    new LegendaryWandRequirement { ItemKey = "ManaCrystal", Amount = 1 },
                    new LegendaryWandRequirement { ItemKey = "InnocentSoul", Amount = 1 }
                }
            },
            new LegendaryWandProfile
            {
                MasteryKey = PlayerMagicStateManager.WandMasteryKeys.Void,
                LegendaryName = "Eclipse Wand",
                Summary = "+16% rupture damage over time, bigger collapse reach, harsher void damage and eclipse collapses.",
                Requirements = new List<LegendaryWandRequirement>
                {
                    new LegendaryWandRequirement { ItemKey = "Mana", Amount = 38 },
                    new LegendaryWandRequirement { ItemKey = "Void", Amount = 4 },
                    new LegendaryWandRequirement { ItemKey = "Adamantine", Amount = 4 },
                    new LegendaryWandRequirement { ItemKey = "AirStone", Amount = 1 },
                    new LegendaryWandRequirement { ItemKey = "ManaCrystal", Amount = 1 },
                    new LegendaryWandRequirement { ItemKey = "InnocentSoul", Amount = 1 }
                }
            }
        };

        public static IEnumerable<LegendaryWandProfile> GetProfiles()
        {
            return Profiles;
        }

        public static bool TryGetProfile(string masteryKey, out LegendaryWandProfile profile)
        {
            var normalized = NormalizeRequestedKey(masteryKey);
            profile = Profiles.FirstOrDefault(candidate =>
                string.Equals(candidate.MasteryKey, normalized, StringComparison.OrdinalIgnoreCase));
            return profile != null;
        }

        public static string GetLegendaryName(string masteryKey)
        {
            return TryGetProfile(masteryKey, out var profile)
                ? profile.LegendaryName
                : PlayerMagicStateManager.GetWandMasteryDisplayName(masteryKey);
        }

        public static string GetLegendarySummary(string masteryKey)
        {
            return TryGetProfile(masteryKey, out var profile)
                ? profile.Summary
                : "Legendary evolution active.";
        }

        public static bool TryUnlock(Players.Player player, Colony colony, string requestedKey, out string message)
        {
            if (player == null)
            {
                message = "No player was provided.";
                return false;
            }

            if (!TryGetProfile(requestedKey, out var profile))
            {
                message = "Unknown wand. Try /bnmagic evolve mana|briar|spark|venom|ember|frost|crystal|stone|storm|aether|blood|void.";
                return false;
            }

            if (PlayerMagicStateManager.GetWandMasteryLevel(player, profile.MasteryKey) < 10)
            {
                message = PlayerMagicStateManager.GetWandMasteryDisplayName(profile.MasteryKey) + " needs Mastery 10 before it can evolve.";
                return false;
            }

            if (PlayerMagicStateManager.IsWandLegendaryEvolutionUnlocked(player, profile.MasteryKey))
            {
                message = profile.LegendaryName + " is already awakened.";
                return false;
            }

            message = "Evolution is automatic now: cast once more with " + PlayerMagicStateManager.GetWandMasteryDisplayName(profile.MasteryKey) + " at Mastery 10 to replace it with " + profile.LegendaryName + " for free.";
            return false;
        }

        private static string NormalizeRequestedKey(string masteryKey)
        {
            if (string.IsNullOrWhiteSpace(masteryKey))
                return string.Empty;

            var value = masteryKey.Trim();
            if (value.EndsWith(" wand", StringComparison.OrdinalIgnoreCase))
                value = value.Substring(0, value.Length - 5).Trim();

            if (string.Equals(value, "magic", StringComparison.OrdinalIgnoreCase))
                return PlayerMagicStateManager.WandMasteryKeys.Aether;

            return value;
        }
    }
}
