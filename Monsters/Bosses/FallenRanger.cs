using AI;
using BetterNecromancy;
using Monsters;
using Newtonsoft.Json.Linq;
using NPC;
using Pandaros.API;
using Pandaros.API.Entities;
using Pandaros.API.Monsters;
using Pandaros.Settlers.Monsters;
using System.Collections.Generic;
using System.Linq;
using Shared;
using UnityEngine;

namespace Pandaros.Settlers.Monsters.Bosses
{
    [ModLoader.ModManager]
    public class FallenRanger : Zombie, IPandaBoss
    {
        public static readonly string Key = GameLoader.NAMESPACE + ".Monsters.Bosses.FallenRanger";

        private static NPCTypeMonsterSettings _settings;
        private static ushort? _arrowIndicatorItemIndex;

        private bool _killedBefore;
        private double _cooldown = 2d;
        private float _totalHealth = 40000f;

        public FallenRanger()
            : base(NPCType.GetByKeyNameOrDefault(Key), new Path(), GameLoader.StubColony)
        {
        }

        public FallenRanger(Path path, Colony originalGoal)
            : base(NPCType.GetByKeyNameOrDefault(Key), path, originalGoal)
        {
            var colonyState = ColonyState.GetColonyState(originalGoal);
            _totalHealth = originalGoal.FollowerCount * colonyState.Difficulty.BossHPPerColonist;
            CurrentHealth = _totalHealth;
        }

        public string AnnouncementAudio => GameLoader.NAMESPACE + ".ZombieAudio";
        public string AnnouncementText => "I've got you in my sights!";
        public Dictionary<DamageType, float> AdditionalResistance { get; } = new Dictionary<DamageType, float>
        {
            [DamageType.Physical] = 0.15f
        };
        public Dictionary<DamageType, float> Damage { get; } = new Dictionary<DamageType, float>
        {
            [DamageType.Void] = 20f,
            [DamageType.Physical] = 30f
        };
        public string DeathText => "Looks like I have to work on my aim.";
        public DamageType ElementalArmor => DamageType.Earth;
        public bool KilledBefore
        {
            get => _killedBefore;
            set => _killedBefore = value;
        }
        public int MinColonists => 150;
        public float MissChance => 0.05f;
        public string MosterType => "Boss";
        public string name => "Fallen Ranger";
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
            return new FallenRanger(path, colony);
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

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnUpdate, GameLoader.NAMESPACE + ".Monsters.Bosses.FallenRanger.OnUpdate")]
        public void OnBossUpdate()
        {
            if (PlayerUiGuard.ShouldDeferPlayerFacingEffects())
            {
                _killedBefore = false;
                return;
            }

            var tuning = BetterNecromancy.BossTuning.Current;
            if (Pipliz.Time.SecondsSinceStartDouble <= _cooldown)
            {
                _killedBefore = false;
                return;
            }

            var damage = Damage.Sum(kvp => kvp.Key.CalcDamage(DamageType.Physical, kvp.Value));
            var attackRange = tuning.FallenRangerAttackRange;

            if (Players.FindClosestAlive(Position.Vector, out var player, out var distanceSqr) &&
                distanceSqr <= attackRange * attackRange &&
                VoxelPhysics.CanSee(Position.Vector, player.PositionStanding))
            {
                if (PlayerUiGuard.CanBroadcastWorldEffects())
                    Indicator.SendIconIndicatorNear(Position.Vector, IndicatorState.NewItemIndicator(0.6f, GetArrowIndicatorItemIndex()));
                ServerManager.SendParticleTrail(Position.Vector, player.PositionStanding + Vector3.up * 1.1f, tuning.FallenRangerTrailSeconds);
                AudioManager.SendAudio(Position.Vector, "bowShoot");
                Players.TakeHit(player, damage, this, ModLoader.OnHitData.EHitSourceType.Monster);
                AudioManager.SendAudio(player.PositionStanding, "fleshHit");
                _cooldown = Pipliz.Time.SecondsSinceStartDouble + tuning.FallenRangerCooldownSeconds;
            }
            else if (NPCTracker.TryGetNear(Position.Vector, UnityEngine.Mathf.CeilToInt(attackRange), out var npc) &&
                     VoxelPhysics.CanSee(Position.Vector, npc.Position.Vector))
            {
                if (PlayerUiGuard.CanBroadcastWorldEffects())
                    Indicator.SendIconIndicatorNear(Position.Vector, IndicatorState.NewItemIndicator(0.6f, GetArrowIndicatorItemIndex()));
                ServerManager.SendParticleTrail(Position.Vector, npc.Position.Vector + Vector3.up * 0.8f, tuning.FallenRangerTrailSeconds);
                AudioManager.SendAudio(Position.Vector, "bowShoot");
                npc.OnHit(damage, this, ModLoader.OnHitData.EHitSourceType.Monster);
                AudioManager.SendAudio(npc.Position.Vector, "fleshHit");
                _cooldown = Pipliz.Time.SecondsSinceStartDouble + tuning.FallenRangerCooldownSeconds;
            }

            _killedBefore = false;
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AfterItemTypesDefined, GameLoader.NAMESPACE + ".Monsters.Bosses.FallenRanger.Register")]
        [ModLoader.ModCallbackDependsOn("pipliz.server.loadnpctypes")]
        public static void Register()
        {
            var node = new JObject
            {
                ["keyName"] = Key,
                ["printName"] = "Fallen Ranger",
                ["npcType"] = "monster",
                ["mesh"] = GameLoader.GetBossMesh("visuals/BossFallenRanger.ply", "skeleton4.glb"),
                ["initialHealth"] = 4000,
                ["movementSpeed"] = 1.25f,
                ["rootOffsetY"] = GameLoader.GetBossRootOffset("visuals/BossFallenRanger.ply", 0f),
                ["punchCooldownMS"] = 2000,
                ["punchDamage"] = 100
            };

            _settings = new NPCTypeMonsterSettings(node);
            NPCType.AddSettings(_settings);
            MonsterManager.AddBoss(new FallenRanger());
        }

        private static ushort GetArrowIndicatorItemIndex()
        {
            if (!_arrowIndicatorItemIndex.HasValue)
                _arrowIndicatorItemIndex = ItemTypes.GetType("copperarrow").ItemIndex;

            return _arrowIndicatorItemIndex.Value;
        }
    }
}
