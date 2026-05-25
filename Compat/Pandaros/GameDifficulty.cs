using System;
using System.Collections.Generic;

namespace Pandaros.API
{
    public sealed class GameDifficulty
    {
        private readonly Dictionary<string, float> _settings;

        public static GameDifficulty Normal { get; } = new GameDifficulty(
            "Normal",
            rank: 1,
            bossHPPerColonist: 150f,
            monsterDamage: 0f,
            monsterDamageReduction: 0f,
            settings: new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase)
            {
                ["ZombieQueenTargetTeleportHp"] = 100f,
                ["ZombieQueenTargetTeleportCooldownSeconds"] = 5f
            });

        public static Dictionary<string, GameDifficulty> GameDifficulties { get; } =
            new Dictionary<string, GameDifficulty>(StringComparer.OrdinalIgnoreCase)
            {
                [Normal.Name] = Normal
            };

        public GameDifficulty(
            string name,
            int rank,
            float bossHPPerColonist,
            float monsterDamage,
            float monsterDamageReduction,
            Dictionary<string, float> settings = null)
        {
            Name = name;
            Rank = rank;
            BossHPPerColonist = bossHPPerColonist;
            MonsterDamage = monsterDamage;
            MonsterDamageReduction = monsterDamageReduction;
            _settings = settings ?? new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
        }

        public string Name { get; }
        public int Rank { get; }
        public float BossHPPerColonist { get; }
        public float MonsterDamage { get; }
        public float MonsterDamageReduction { get; }

        public float GetorDefault(string key, float defaultValue)
        {
            return _settings.TryGetValue(key, out var value) ? value : defaultValue;
        }
    }
}
