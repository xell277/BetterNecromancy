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
    public class ZombieKing : Zombie, IPandaBoss
    {
        public static readonly string Key = GameLoader.NAMESPACE + ".Monsters.Bosses.ZombieKing";

        private static NPCTypeMonsterSettings _settings;

        private bool _killedBefore;
        private float _totalHealth = 20000f;

        public ZombieKing()
            : base(NPCType.GetByKeyNameOrDefault(Key), new Path(), GameLoader.StubColony)
        {
        }

        public ZombieKing(Path path, Colony originalGoal)
            : base(NPCType.GetByKeyNameOrDefault(Key), path, originalGoal)
        {
            var colonyState = ColonyState.GetColonyState(originalGoal);
            _totalHealth = originalGoal.FollowerCount * colonyState.Difficulty.BossHPPerColonist;
            CurrentHealth = _totalHealth;
        }

        public string AnnouncementAudio => GameLoader.NAMESPACE + ".ZombieAudio";
        public string AnnouncementText => "YOU WILL DO MY BIDDING!";
        public Dictionary<DamageType, float> AdditionalResistance { get; } = new Dictionary<DamageType, float>
        {
            [DamageType.Physical] = 0.2f
        };
        public Dictionary<DamageType, float> Damage { get; } = new Dictionary<DamageType, float>
        {
            [DamageType.Void] = 30f,
            [DamageType.Physical] = 50f
        };
        public string DeathText => "UGH Help me you useless bags of meat......";
        public DamageType ElementalArmor => DamageType.Fire;
        public bool KilledBefore
        {
            get => _killedBefore;
            set => _killedBefore = value;
        }
        public int MinColonists => 150;
        public float MissChance => 0.05f;
        public string MosterType => "Boss";
        public string name => "ZombieKing";
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
            return new ZombieKing(path, colony);
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

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AfterItemTypesDefined, GameLoader.NAMESPACE + ".Monsters.Bosses.ZombieKing.Register")]
        [ModLoader.ModCallbackDependsOn("pipliz.server.loadnpctypes")]
        public static void Register()
        {
            var node = new JObject
            {
                ["keyName"] = Key,
                ["printName"] = "ZombieKing",
                ["npcType"] = "monster",
                ["mesh"] = GameLoader.GetBossMesh("visuals/BossZombieKing.ply", "skeleton4.glb"),
                ["initialHealth"] = 20000,
                ["movementSpeed"] = 1.5f,
                ["rootOffsetY"] = GameLoader.GetBossRootOffset("visuals/BossZombieKing.ply", 0f),
                ["punchCooldownMS"] = 1000,
                ["punchDamage"] = 80
            };

            _settings = new NPCTypeMonsterSettings(node);
            NPCType.AddSettings(_settings);
            MonsterManager.AddBoss(new ZombieKing());
        }
    }
}
