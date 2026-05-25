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
    public class JackbNimble : Zombie, IPandaBoss
    {
        public static readonly string Key = GameLoader.NAMESPACE + ".Monsters.Bosses.JackbNimble";

        private static NPCTypeMonsterSettings _settings;

        private bool _killedBefore;
        private float _totalHealth = 40000f;

        public JackbNimble()
            : base(NPCType.GetByKeyNameOrDefault(Key), new Path(), GameLoader.StubColony)
        {
        }

        public JackbNimble(Path path, Colony originalGoal)
            : base(NPCType.GetByKeyNameOrDefault(Key), path, originalGoal)
        {
            var colonyState = ColonyState.GetColonyState(originalGoal);
            _totalHealth = originalGoal.FollowerCount * (colonyState.Difficulty.BossHPPerColonist * 0.5f);
            CurrentHealth = _totalHealth;
        }

        public string AnnouncementAudio => GameLoader.NAMESPACE + ".ZombieAudio";
        public string AnnouncementText => "Catch me if you can!";
        public Dictionary<DamageType, float> AdditionalResistance { get; } = new Dictionary<DamageType, float>
        {
            [DamageType.Fire] = 0.2f
        };
        public Dictionary<DamageType, float> Damage { get; } = new Dictionary<DamageType, float>
        {
            [DamageType.Void] = 10f,
            [DamageType.Physical] = 10f
        };
        public string DeathText => "I just was not fast enough...";
        public DamageType ElementalArmor => DamageType.Air;
        public bool KilledBefore
        {
            get => _killedBefore;
            set => _killedBefore = value;
        }
        public int MinColonists => 150;
        public float MissChance => 0.10f;
        public string MosterType => "Boss";
        public string name => "Jack-b-Nimble";
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
        public float ZombieHPBonus => 0f;
        public float ZombieMultiplier => 1f;

        public IPandaZombie GetNewInstance(Path path, Colony colony)
        {
            return new JackbNimble(path, colony);
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

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AfterItemTypesDefined, GameLoader.NAMESPACE + ".Monsters.Bosses.JackbNimble.Register")]
        [ModLoader.ModCallbackDependsOn("pipliz.server.loadnpctypes")]
        public static void Register()
        {
            var node = new JObject
            {
                ["keyName"] = Key,
                ["printName"] = "Jack-b-Nimble",
                ["npcType"] = "monster",
                ["mesh"] = GameLoader.GetBossMesh("visuals/BossJackBNimble.ply", "skeleton4.glb"),
                ["initialHealth"] = 2000,
                ["movementSpeed"] = 3.25f,
                ["rootOffsetY"] = GameLoader.GetBossRootOffset("visuals/BossJackBNimble.ply", 0f),
                ["punchCooldownMS"] = 500,
                ["punchDamage"] = 100
            };

            _settings = new NPCTypeMonsterSettings(node);
            NPCType.AddSettings(_settings);
            MonsterManager.AddBoss(new JackbNimble());
        }
    }
}
