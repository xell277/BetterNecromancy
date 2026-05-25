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
using Vector3Int = Pipliz.Vector3Int;

namespace Pandaros.Settlers.Monsters.Bosses
{
    [ModLoader.ModManager]
    public class Bulging : Zombie, IPandaBoss
    {
        public static readonly string Key = GameLoader.NAMESPACE + ".Monsters.Bosses.Bulging";

        private static NPCTypeMonsterSettings _settings;

        private bool _killedBefore;
        private float _totalHealth = 20000f;

        public Bulging()
            : base(NPCType.GetByKeyNameOrDefault(Key), new Path(), GameLoader.StubColony)
        {
        }

        public Bulging(Path path, Colony originalGoal)
            : base(NPCType.GetByKeyNameOrDefault(Key), path, originalGoal)
        {
            var colonyState = ColonyState.GetColonyState(originalGoal);
            _totalHealth = originalGoal.FollowerCount * colonyState.Difficulty.BossHPPerColonist;
            CurrentHealth = _totalHealth;
        }

        public string AnnouncementAudio => GameLoader.NAMESPACE + ".ZombieAudio";
        public string AnnouncementText => "I DONT FEEL SO GOOD";
        public Dictionary<DamageType, float> AdditionalResistance { get; } = new Dictionary<DamageType, float>
        {
            [DamageType.Physical] = 0.15f
        };
        public Dictionary<DamageType, float> Damage { get; } = new Dictionary<DamageType, float>
        {
            [DamageType.Void] = 10f,
            [DamageType.Physical] = 10f
        };
        public string DeathText => "Boom.";
        public DamageType ElementalArmor => DamageType.Air;
        public bool KilledBefore
        {
            get => _killedBefore;
            set => _killedBefore = value;
        }
        public int MinColonists => 150;
        public float MissChance => 0.05f;
        public string MosterType => "Boss";
        public string name => "Bulging";
        public override Vector3 PositionToAimFor
        {
            get
            {
                var fallback = Position.Vector + new Vector3(0f, 3.6f, 0f);
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
            return new Bulging(path, colony);
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnUpdate, GameLoader.NAMESPACE + ".Monsters.Bosses.Bulging.OnUpdate")]
        public void OnBossUpdate()
        {
            _killedBefore = false;
        }

        public override bool Update()
        {
            _killedBefore = false;
            var shouldPlayAttackAnimation = ShouldTriggerAttackAnimation();
            var result = base.Update();

            if (shouldPlayAttackAnimation && isValid)
                BossVisualProxyManager.NotifyBossAttack(ID);

            return result;
        }

        public override void SendUpdate()
        {
            base.SendUpdate();
        }

        public override void SendUpdate(float speedMultiplier)
        {
            base.SendUpdate(speedMultiplier);
        }

        public override void OnRagdoll(Vector3? ragdollForce)
        {
            if (!isValid)
                return;

            ModLoader.Callbacks.OnMonsterDied.Invoke(this);
            if (!BetterNecromancy.PlayerUiGuard.ShouldDeferPlayerFacingEffects())
                Indicator.CancelIndicatorNear(ID, Position);
            isValid = false;
            decision = default;
        }

        public override void OnDiscard()
        {
            if (!isValid)
                return;

            isValid = false;
            decision = default;
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AfterItemTypesDefined, GameLoader.NAMESPACE + ".Monsters.Bosses.Bulging.Register")]
        [ModLoader.ModCallbackDependsOn("pipliz.server.loadnpctypes")]
        public static void Register()
        {
            var node = new JObject
            {
                ["keyName"] = Key,
                ["printName"] = "Bulging",
                ["npcType"] = "monster",
                ["mesh"] = GameLoader.GetBossMesh("visuals/BossBulging.ply", "monster5.glb"),
                ["initialHealth"] = 20000,
                ["movementSpeed"] = 0.75f,
                ["rootOffsetY"] = GameLoader.GetBossRootOffset("visuals/BossBulging.ply", 0.52f),
                ["punchCooldownMS"] = 3000,
                ["punchDamage"] = 100
            };

            _settings = new NPCTypeMonsterSettings(node);
            NPCType.AddSettings(_settings);
            MonsterManager.AddBoss(new Bulging());
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnMonsterDied, GameLoader.NAMESPACE + ".Monsters.Bosses.Bulging.OnMonsterDied")]
        public static void OnMonsterDied(IMonster monster)
        {
            if (!(monster is Bulging boss) || boss.OriginalGoal == null || MonsterTracker.MonsterSpawner == null)
                return;

            var tuning = BetterNecromancy.BossTuning.Current;
            var spawnCount = System.Math.Max(
                tuning.BulgingSpawnMinCount,
                ColonyState.GetColonyState(boss.OriginalGoal).Difficulty.Rank * tuning.BulgingSpawnPerRank);
            var banner = boss.OriginalGoal.GetClosestBanner(new Vector3Int(boss.Position));
            var spawnType = GetSpawnType(boss.OriginalGoal.FollowerCount);

            for (var i = 0; i < spawnCount && banner != null; i++)
                MonsterTracker.MonsterSpawner.QueueSpawnZombie(banner, spawnType);
        }

        private static NPCType GetSpawnType(int followerCount)
        {
            if (followerCount >= 1500)
                return MonsterSpawner.Monster5000;

            if (followerCount >= 750)
                return MonsterSpawner.Monster1500;

            if (followerCount >= 300)
                return MonsterSpawner.Monster250;

            if (followerCount >= 150)
                return MonsterSpawner.Monster100;

            return MonsterSpawner.Monster25;
        }

        private bool ShouldTriggerAttackAnimation()
        {
            if (!isValid ||
                ServerTime.SecondsSinceStart < nextUpdate ||
                !decision.IsValid)
            {
                return false;
            }

            switch (decision.GoalType)
            {
                case EZombieGoal.NPC:
                    return decision.goalNPC != null &&
                           decision.goalNPC.IsValid &&
                           position == decision.goalNPC.Position;

                case EZombieGoal.Player:
                    return decision.goalPlayer != null &&
                           decision.goalPlayer.IsConnectionReady &&
                           decision.goalPlayer.Health > 0f &&
                           position == decision.goalPlayer.PositionVoxelStanding;

                case EZombieGoal.Banner:
                    return decision.GoalLocation.IsValid &&
                           position == decision.GoalLocation;

                default:
                    return false;
            }
        }
    }
}
