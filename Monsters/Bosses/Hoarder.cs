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
    public class Hoarder : Zombie, IPandaBoss
    {
        public static readonly string Key = GameLoader.NAMESPACE + ".Monsters.Bosses.Hoarder";

        private static NPCTypeMonsterSettings _settings;

        private bool _killedBefore;
        private float _totalHealth = 20000f;

        public Hoarder()
            : base(NPCType.GetByKeyNameOrDefault(Key), new Path(), GameLoader.StubColony)
        {
        }

        public Hoarder(Path path, Colony originalGoal)
            : base(NPCType.GetByKeyNameOrDefault(Key), path, originalGoal)
        {
            var colonyState = ColonyState.GetColonyState(originalGoal);
            _totalHealth = originalGoal.FollowerCount * colonyState.Difficulty.BossHPPerColonist;
            CurrentHealth = _totalHealth;
        }

        public string AnnouncementAudio => GameLoader.NAMESPACE + ".ZombieAudio";
        public string AnnouncementText => "FEAR THE ZOMBIE HORDE!";
        public Dictionary<DamageType, float> AdditionalResistance { get; } = new Dictionary<DamageType, float>
        {
            [DamageType.Physical] = 0.15f
        };
        public Dictionary<DamageType, float> Damage { get; } = new Dictionary<DamageType, float>
        {
            [DamageType.Void] = 10f,
            [DamageType.Physical] = 10f
        };
        public string DeathText => "Gughghgugggghrrrghghgfggg......";
        public DamageType ElementalArmor => DamageType.Water;
        public bool KilledBefore
        {
            get => _killedBefore;
            set => _killedBefore = value;
        }
        public int MinColonists => 150;
        public float MissChance => 0.05f;
        public string MosterType => "Boss";
        public string name => "Hoarder";
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
        public float ZombieMultiplier => 1.2f;

        public IPandaZombie GetNewInstance(Path path, Colony colony)
        {
            return new Hoarder(path, colony);
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

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AfterItemTypesDefined, GameLoader.NAMESPACE + ".Monsters.Bosses.Hoarder.Register")]
        [ModLoader.ModCallbackDependsOn("pipliz.server.loadnpctypes")]
        public static void Register()
        {
            var node = new JObject
            {
                ["keyName"] = Key,
                ["printName"] = "Hoarder",
                ["npcType"] = "monster",
                ["mesh"] = GameLoader.GetBossMesh("visuals/BossHoarder.ply", "skeleton4.glb"),
                ["initialHealth"] = 20000,
                ["movementSpeed"] = 0.75f,
                ["rootOffsetY"] = GameLoader.GetBossRootOffset("visuals/BossHoarder.ply", 0f),
                ["punchCooldownMS"] = 500,
                ["punchDamage"] = 25
            };

            _settings = new NPCTypeMonsterSettings(node);
            NPCType.AddSettings(_settings);
            MonsterManager.AddBoss(new Hoarder());
        }
    }
}
