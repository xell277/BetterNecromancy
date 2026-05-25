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
    public class PutridCorpse : Zombie, IPandaBoss
    {
        public static readonly string Key = GameLoader.NAMESPACE + ".Monsters.Bosses.PutridCorpse";

        private static NPCTypeMonsterSettings _settings;

        private bool _killedBefore;
        private double _nextBossUpdateTime = double.MinValue;
        private float _totalHealth = 20000f;

        public PutridCorpse()
            : base(NPCType.GetByKeyNameOrDefault(Key), new Path(), GameLoader.StubColony)
        {
        }

        public PutridCorpse(Path path, Colony originalGoal)
            : base(NPCType.GetByKeyNameOrDefault(Key), path, originalGoal)
        {
            var colonyState = ColonyState.GetColonyState(originalGoal);
            _totalHealth = originalGoal.FollowerCount * colonyState.Difficulty.BossHPPerColonist;
            CurrentHealth = _totalHealth;
        }

        public string AnnouncementAudio => GameLoader.NAMESPACE + ".ZombieAudio";
        public string AnnouncementText => "Hehehe Smell that?!?!?! Come a little closer...";
        public Dictionary<DamageType, float> AdditionalResistance { get; } = new Dictionary<DamageType, float>
        {
            [DamageType.Physical] = 0.15f
        };
        public Dictionary<DamageType, float> Damage { get; } = new Dictionary<DamageType, float>
        {
            [DamageType.Void] = 25f,
            [DamageType.Physical] = 50f
        };
        public string DeathText => "ffffffaaarrt....";
        public DamageType ElementalArmor => DamageType.Earth;
        public bool KilledBefore
        {
            get => _killedBefore;
            set => _killedBefore = value;
        }
        public int MinColonists => 150;
        public float MissChance => 0.05f;
        public string MosterType => "Boss";
        public string name => "Putrid Corpse";
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
        public float ZombieHPBonus => 20f;
        public float ZombieMultiplier => 1f;

        public IPandaZombie GetNewInstance(Path path, Colony colony)
        {
            return new PutridCorpse(path, colony);
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnUpdate, GameLoader.NAMESPACE + ".Monsters.Bosses.PutridCorpse.OnUpdate")]
        public void OnBossUpdate()
        {
            var tuning = BetterNecromancy.BossTuning.Current;
            if (_nextBossUpdateTime < Pipliz.Time.SecondsSinceStartDouble && OriginalGoal != null)
            {
                foreach (var follower in OriginalGoal.Followers)
                {
                    if (Vector3.Distance(Position.Vector, follower.Position.Vector) <= tuning.PutridCorpseAuraRange)
                        follower.OnHit(tuning.PutridCorpseAuraDamage);
                }

                foreach (var owner in Players.PlayerDatabase.Values)
                {
                    if (owner.ActiveColony == OriginalGoal && Vector3.Distance(Position.Vector, owner.PositionStanding) <= tuning.PutridCorpseAuraRange)
                        Players.TakeHit(owner, tuning.PutridCorpseAuraDamage, this, ModLoader.OnHitData.EHitSourceType.Monster);
                }

                _nextBossUpdateTime = Pipliz.Time.SecondsSinceStartDouble + tuning.PutridCorpseAuraIntervalSeconds;
            }

            _killedBefore = false;
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

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AfterItemTypesDefined, GameLoader.NAMESPACE + ".Monsters.Bosses.PutridCorpse.Register")]
        [ModLoader.ModCallbackDependsOn("pipliz.server.loadnpctypes")]
        public static void Register()
        {
            var node = new JObject
            {
                ["keyName"] = Key,
                ["printName"] = "PutridCorpse",
                ["npcType"] = "monster",
                ["mesh"] = GameLoader.GetBossMesh("visuals/BossPutridCorpse.ply", "monster5.glb"),
                ["initialHealth"] = 20000,
                ["movementSpeed"] = 0.75f,
                ["rootOffsetY"] = GameLoader.GetBossRootOffset("visuals/BossPutridCorpse.ply", 0.52f),
                ["punchCooldownMS"] = 3000,
                ["punchDamage"] = 100
            };

            _settings = new NPCTypeMonsterSettings(node);
            NPCType.AddSettings(_settings);
            MonsterManager.AddBoss(new PutridCorpse());
        }
    }
}
