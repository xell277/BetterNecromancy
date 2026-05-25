using AI;
using Monsters;
using Newtonsoft.Json.Linq;
using NPC;
using Pandaros.API;
using Pandaros.API.Entities;
using Pandaros.API.Monsters;
using Pandaros.Settlers.Monsters;
using System.Collections.Generic;
using UnityEngine;

namespace Pandaros.Settlers.Monsters.Bosses
{
    [ModLoader.ModManager]
    public class Juggernaut : Zombie, IPandaBoss
    {
        public static readonly string Key = GameLoader.NAMESPACE + ".Monsters.Bosses.Juggernaut";

        private static NPCTypeMonsterSettings _settings;

        private bool _killedBefore;
        private float _totalHealth = 40000f;

        public Juggernaut()
            : base(NPCType.GetByKeyNameOrDefault(Key), new Path(), GameLoader.StubColony)
        {
        }

        public Juggernaut(Path path, Colony originalGoal)
            : base(NPCType.GetByKeyNameOrDefault(Key), path, originalGoal)
        {
            var colonyState = ColonyState.GetColonyState(originalGoal);
            _totalHealth = originalGoal.FollowerCount * (colonyState.Difficulty.BossHPPerColonist * 2.25f);
            CurrentHealth = _totalHealth;
        }

        public string AnnouncementAudio => GameLoader.NAMESPACE + ".ZombieAudio";
        public string AnnouncementText => "IM THE JUGGERNAUT B$#CH!";
        public Dictionary<DamageType, float> AdditionalResistance { get; } = new Dictionary<DamageType, float>
        {
            [DamageType.Physical] = 0.4f,
            [DamageType.Air] = 0.4f,
            [DamageType.Earth] = 0.4f,
            [DamageType.Water] = 0.4f
        };
        public Dictionary<DamageType, float> Damage { get; } = new Dictionary<DamageType, float>
        {
            [DamageType.Void] = 70f,
            [DamageType.Physical] = 70f
        };
        public string DeathText => "Juggernaut want to smash.....";
        public DamageType ElementalArmor => DamageType.Physical;
        public bool KilledBefore
        {
            get => _killedBefore;
            set => _killedBefore = value;
        }
        public int MinColonists => 150;
        public float MissChance => 0f;
        public string MosterType => "Boss";
        public string name => "Juggernaut";
        public override Vector3 PositionToAimFor
        {
            get
            {
                var fallback = base.PositionToAimFor;
                return BossVisualProxyManager.TryGetBossAimPoint(ID, fallback, out var aimPoint)
                    ? aimPoint
                    : fallback;
            }
        }
        public override float TotalHealth => _totalHealth;
        public float ZombieHPBonus => 50f;
        public float ZombieMultiplier => 1f;

        public IPandaZombie GetNewInstance(Path path, Colony colony)
        {
            return new Juggernaut(path, colony);
        }

        public override bool Update()
        {
            _killedBefore = false;
            return base.Update();
        }

        public override void SendUpdate()
        {
            base.SendUpdate();
        }

        public override void SendUpdate(float speedMultiplier)
        {
            base.SendUpdate(speedMultiplier);
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AfterItemTypesDefined, GameLoader.NAMESPACE + ".Monsters.Bosses.Juggernaut.Register")]
        [ModLoader.ModCallbackDependsOn("pipliz.server.loadnpctypes")]
        public static void Register()
        {
            var node = new JObject
            {
                ["keyName"] = Key,
                ["printName"] = "Juggernaut",
                ["npcType"] = "monster",
                ["mesh"] = GameLoader.GetBossMesh("visuals/BossJuggernaut.ply", "monster6.glb"),
                ["initialHealth"] = 40000,
                ["movementSpeed"] = 0.9f,
                ["rootOffsetY"] = GameLoader.GetBossRootOffset("visuals/BossJuggernaut.ply", 0.52f),
                ["punchCooldownMS"] = 3000,
                ["punchDamage"] = 100
            };

            _settings = new NPCTypeMonsterSettings(node);
            NPCType.AddSettings(_settings);
            MonsterManager.AddBoss(new Juggernaut());
        }
    }
}
