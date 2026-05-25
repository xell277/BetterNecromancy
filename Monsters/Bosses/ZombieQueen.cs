using AI;
using Monsters;
using Newtonsoft.Json.Linq;
using NPC;
using Pandaros.API;
using Pandaros.API.Entities;
using Pandaros.API.Monsters;
using Pandaros.Settlers.Monsters;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Pandaros.Settlers.Monsters.Bosses
{
    [ModLoader.ModManager]
    public class ZombieQueen : Zombie, IPandaBoss
    {
        public static readonly string Key = GameLoader.NAMESPACE + ".Monsters.Bosses.ZombieQueen";

        private static NPCTypeMonsterSettings _settings;

        private bool _killedBefore;
        private float _totalHealth = 20000f;
        private double _updateTime;

        public ZombieQueen()
            : base(NPCType.GetByKeyNameOrDefault(Key), new Path(), GameLoader.StubColony)
        {
        }

        public ZombieQueen(Path path, Colony originalGoal)
            : base(NPCType.GetByKeyNameOrDefault(Key), path, originalGoal)
        {
            var colonyState = ColonyState.GetColonyState(originalGoal);
            _totalHealth = originalGoal.FollowerCount * colonyState.Difficulty.BossHPPerColonist;
            CurrentHealth = _totalHealth;
        }

        public string AnnouncementAudio => null;
        public string AnnouncementText => "Get them my pretties!";
        public Dictionary<DamageType, float> AdditionalResistance { get; } = new Dictionary<DamageType, float>();
        public Dictionary<DamageType, float> Damage { get; } = new Dictionary<DamageType, float>
        {
            [DamageType.Void] = 25f,
            [DamageType.Physical] = 55f
        };
        public string DeathText => "I'll get you next time my pretties!";
        public DamageType ElementalArmor => DamageType.Water;
        public bool KilledBefore
        {
            get => _killedBefore;
            set => _killedBefore = value;
        }
        public int MinColonists => 150;
        public float MissChance => 0.05f;
        public string MosterType => "Boss";
        public string name => "ZombieQueen";
        public override Vector3 PositionToAimFor
        {
            get
            {
                var fallback = Position.Vector + new Vector3(0f, 3.2f, 0f);
                return BossVisualProxyManager.TryGetBossAimPoint(ID, fallback, out var aimPoint)
                    ? aimPoint
                    : fallback;
            }
        }
        public override float TotalHealth => _totalHealth;
        public float ZombieHPBonus => 0f;
        public float ZombieMultiplier => 1.1f;

        public IPandaZombie GetNewInstance(Path path, Colony colony)
        {
            return new ZombieQueen(path, colony);
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

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnUpdate, GameLoader.NAMESPACE + ".Monsters.Bosses.ZombieQueen.OnUpdate")]
        public void OnBossUpdate()
        {
            _killedBefore = false;

            if (OriginalGoal == null || _updateTime >= Pipliz.Time.SecondsSinceStartDouble)
                return;

            var colonyState = ColonyState.GetColonyState(OriginalGoal);
            var rank = colonyState.Difficulty.Rank;
            var teleportHp = colonyState.Difficulty.GetorDefault("ZombieQueenTargetTeleportHp", 100f);
            var cooldown = colonyState.Difficulty.GetorDefault("ZombieQueenTargetTeleportCooldownSeconds", 5f);
            var alreadyTeleported = new HashSet<NPCID>();

            for (var i = 0; i < rank - 1; i++)
            {
                if (!NPCTracker.TryGetNear(Position.Vector, 50, out var npc))
                    break;

                var zombie = MonsterTracker.GetAllMonstersByID()
                    .Values
                    .OfType<Zombie>()
                    .Where(monster => monster != this &&
                                      !(monster is IPandaBoss) &&
                                      monster.OriginalGoal == OriginalGoal &&
                                      monster.IsValid &&
                                      monster.CurrentHealth <= teleportHp &&
                                      !alreadyTeleported.Contains(monster.ID) &&
                                      UnityEngine.Vector3.Distance(monster.Position.Vector, Position.Vector) <= 20f)
                    .OrderBy(monster => UnityEngine.Vector3.Distance(monster.Position.Vector, Position.Vector))
                    .FirstOrDefault();

                if (zombie == null)
                    break;

                if (PathingManager.TryCanStandNearNotAt(npc.Position, out var posFound, out var position) && posFound)
                {
                    var setPosition = zombie.GetType().GetMethod("SetPosition", BindingFlags.NonPublic | BindingFlags.Instance);
                    var updateChunkPosition = zombie.GetType().GetMethod("UpdateChunkPosition", BindingFlags.NonPublic | BindingFlags.Instance);
                    var decisionField = zombie.GetType().GetField("decision", BindingFlags.NonPublic | BindingFlags.Instance);

                    setPosition?.Invoke(zombie, new object[] { position });
                    zombie.SendUpdate();
                    updateChunkPosition?.Invoke(zombie, System.Array.Empty<object>());

                    if (decisionField != null)
                        decisionField.SetValue(zombie, default(ZombieDecision));

                    alreadyTeleported.Add(zombie.ID);
                }
            }

            _updateTime = Pipliz.Time.SecondsSinceStartDouble + cooldown;
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AfterItemTypesDefined, GameLoader.NAMESPACE + ".Monsters.Bosses.ZombieQueen.Register")]
        [ModLoader.ModCallbackDependsOn("pipliz.server.loadnpctypes")]
        public static void Register()
        {
            var node = new JObject
            {
                ["keyName"] = Key,
                ["printName"] = "Zombie Queen",
                ["npcType"] = "monster",
                ["mesh"] = GameLoader.GetBossMesh("visuals/BossZombieQueen.ply", "skeleton4.glb"),
                ["initialHealth"] = 20000,
                ["movementSpeed"] = 1.3f,
                ["rootOffsetY"] = GameLoader.GetBossRootOffset("visuals/BossZombieQueen.ply", 0f),
                ["punchCooldownMS"] = 2000,
                ["punchDamage"] = 70
            };

            _settings = new NPCTypeMonsterSettings(node);
            NPCType.AddSettings(_settings);
            MonsterManager.AddBoss(new ZombieQueen());
        }

        private bool ShouldTriggerAttackAnimation()
        {
            if (!isValid ||
                Pipliz.Time.SecondsSinceStartDoubleThisFrame < nextUpdate ||
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
