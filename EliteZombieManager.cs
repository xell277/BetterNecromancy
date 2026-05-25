using Chatting;
using Monsters;
using NPC;
using Pandaros.API.Monsters;
using Shared;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BetterNecromancy
{
    [ModLoader.ModManager]
    public static class EliteZombieManager
    {
        private enum EliteKind
        {
            Feral,
            Bloodbound,
            Plagueborn,
            Stormtouched,
            Warbound
        }

        private sealed class EliteRuntime
        {
            public EliteKind Kind;
            public float MaxHealth;
            public float IncomingDamageMultiplier;
            public float OutgoingDamageMultiplier;
            public int BonusPoints;
            public double NextAbilityAt;
            public double NextFxAt;
            public double NextMarkerAt;
            public bool PhaseTriggered;
        }

        private static readonly Dictionary<NPCID, EliteRuntime> ActiveElites = new Dictionary<NPCID, EliteRuntime>();
        private static readonly Dictionary<Colony, double> ColonyAnnouncementCooldowns = new Dictionary<Colony, double>();
        private const double FxIntervalSeconds = 2.4d;
        private const double ColonyAnnouncementCooldownSeconds = 14d;
        private const float SpawnIndicatorSeconds = 0.9f;

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AfterWorldLoad, BetterNecromancy.ModEntry.Namespace + ".EliteZombieManager.AfterWorldLoad")]
        public static void AfterWorldLoad()
        {
            ActiveElites.Clear();
            ColonyAnnouncementCooldowns.Clear();
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnMonsterSpawned, BetterNecromancy.ModEntry.Namespace + ".EliteZombieManager.OnMonsterSpawned")]
        public static void OnMonsterSpawned(IMonster monster)
        {
            if (!(monster is Zombie zombie) || monster is IPandaBoss || !zombie.IsValid)
                return;

            if (ActiveElites.ContainsKey(zombie.ID) || !TryRollElite(out var runtime, out var healthMultiplier))
                return;

            var settings = WandProgressionConfig.Current.Elites;
            zombie.CurrentHealth = Mathf.Max(1f, zombie.CurrentHealth * healthMultiplier * settings.HealthMultiplier);
            runtime.MaxHealth = zombie.CurrentHealth;
            runtime.NextAbilityAt = Pipliz.Time.SecondsSinceStartDouble + Random.Range(3.2f, 6.2f);
            runtime.NextFxAt = Pipliz.Time.SecondsSinceStartDouble + Random.Range(0.1f, 0.45f);
            runtime.NextMarkerAt = 0d;
            ActiveElites[zombie.ID] = runtime;
            UpdateEliteMarker(zombie, runtime, force: true);
            PlayEliteSpawnFx(zombie, runtime.Kind);
            TryAnnounceEliteSpawn(zombie, runtime.Kind);
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnMonsterDied, BetterNecromancy.ModEntry.Namespace + ".EliteZombieManager.OnMonsterDied")]
        public static void OnMonsterDied(IMonster monster)
        {
            if (!(monster is Zombie zombie))
                return;

            if (ActiveElites.TryGetValue(zombie.ID, out var elite))
            {
                TryGrantEliteReward(zombie, elite);
                ActiveElites.Remove(zombie.ID);
                return;
            }

            ActiveElites.Remove(zombie.ID);
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnUpdate, BetterNecromancy.ModEntry.Namespace + ".EliteZombieManager.OnUpdate")]
        public static void OnUpdate()
        {
            if (ActiveElites.Count == 0 || PlayerUiGuard.ShouldDeferPlayerFacingEffects())
                return;

            var now = Pipliz.Time.SecondsSinceStartDouble;
            var removals = default(List<NPCID>);

            foreach (var pair in ActiveElites)
            {
                var zombie = TryGetZombie(pair.Key);
                if (zombie == null || !zombie.IsValid || zombie.CurrentHealth <= 0f)
                {
                    if (removals == null)
                        removals = new List<NPCID>();

                    removals.Add(pair.Key);
                    continue;
                }

                if (now < pair.Value.NextFxAt)
                {
                    TryHandleElitePhase(zombie, pair.Value);
                    TryTriggerEliteAbility(zombie, pair.Value, now);
                    UpdateEliteMarker(zombie, pair.Value, force: false);
                    continue;
                }

                pair.Value.NextFxAt = now + WandProgressionConfig.Current.Elites.FxIntervalSeconds;
                PlayEliteFx(zombie, pair.Value.Kind);
                TryHandleElitePhase(zombie, pair.Value);
                TryTriggerEliteAbility(zombie, pair.Value, now);
                UpdateEliteMarker(zombie, pair.Value, force: true);
            }

            if (removals == null)
                return;

            for (var i = 0; i < removals.Count; i++)
                ActiveElites.Remove(removals[i]);
        }

        public static float ApplyIncomingDamageModifier(Zombie zombie, float damage)
        {
            if (zombie == null || damage <= 0f || !ActiveElites.TryGetValue(zombie.ID, out var elite))
                return damage;

            return damage * elite.IncomingDamageMultiplier;
        }

        public static float ApplyOutgoingDamageModifier(Zombie zombie, float damage)
        {
            if (zombie == null || damage <= 0f || !ActiveElites.TryGetValue(zombie.ID, out var elite))
                return damage;

            return damage * elite.OutgoingDamageMultiplier;
        }

        public static int GetBonusColonyPoints(Zombie zombie)
        {
            if (zombie == null || !ActiveElites.TryGetValue(zombie.ID, out var elite))
                return 0;

            return elite.BonusPoints;
        }

        public static bool IsElite(Zombie zombie)
        {
            return zombie != null && ActiveElites.ContainsKey(zombie.ID);
        }

        private static bool TryRollElite(out EliteRuntime runtime, out float healthMultiplier)
        {
            runtime = null;
            healthMultiplier = 1f;

            var eventId = WorldEventManager.GetEliteThemeId();
            var themeScale = Mathf.Clamp01(WorldEventManager.GetEliteThemeScale());
            var chance = 0f;
            var kind = EliteKind.Feral;
            var incomingMultiplier = 1f;
            var outgoingMultiplier = 1f;
            var bonusPoints = 0;

            switch (eventId)
            {
                case "bloodmoon":
                    chance = 0.16f;
                    kind = EliteKind.Bloodbound;
                    healthMultiplier = 1.68f;
                    incomingMultiplier = 0.74f;
                    outgoingMultiplier = 1.24f;
                    bonusPoints = 6;
                    break;

                case "plaguefog":
                    chance = 0.18f;
                    kind = EliteKind.Plagueborn;
                    healthMultiplier = 1.82f;
                    incomingMultiplier = 0.7f;
                    outgoingMultiplier = 1.14f;
                    bonusPoints = 7;
                    break;

                case "arcanestorm":
                    chance = 0.14f;
                    kind = EliteKind.Stormtouched;
                    healthMultiplier = 1.56f;
                    incomingMultiplier = 0.84f;
                    outgoingMultiplier = 1.32f;
                    bonusPoints = 6;
                    break;

                case "horde":
                    chance = 0.24f;
                    kind = EliteKind.Warbound;
                    healthMultiplier = 2.05f;
                    incomingMultiplier = 0.74f;
                    outgoingMultiplier = 1.2f;
                    bonusPoints = 10;
                    break;

                default:
                    chance = 0.04f;
                    kind = EliteKind.Feral;
                    healthMultiplier = 1.34f;
                    incomingMultiplier = 0.9f;
                    outgoingMultiplier = 1.16f;
                    bonusPoints = 3;
                    break;
            }

            if (eventId != "none")
            {
                chance *= Mathf.Max(0.12f, themeScale);
                healthMultiplier = 1f + ((healthMultiplier - 1f) * Mathf.Max(0.35f, themeScale));
                incomingMultiplier = 1f - ((1f - incomingMultiplier) * Mathf.Max(0.35f, themeScale));
                outgoingMultiplier = 1f + ((outgoingMultiplier - 1f) * Mathf.Max(0.35f, themeScale));
                bonusPoints = Mathf.Max(1, Mathf.RoundToInt(bonusPoints * Mathf.Max(0.35f, themeScale)));
            }

            if (Random.value > chance)
                return false;

            runtime = new EliteRuntime
            {
                Kind = kind,
                IncomingDamageMultiplier = incomingMultiplier,
                OutgoingDamageMultiplier = outgoingMultiplier,
                BonusPoints = bonusPoints
            };
            return true;
        }

        private static Zombie TryGetZombie(NPCID npcId)
        {
            var allMonsters = Pandaros.API.Monsters.MonsterManager.GetAllMonsters();
            if (allMonsters == null)
                return null;

            foreach (var pair in allMonsters)
            {
                if (!new NPCID(pair.Key).Equals(npcId))
                    continue;

                return pair.Value as Zombie;
            }

            return null;
        }

        private static void TryHandleElitePhase(Zombie zombie, EliteRuntime runtime)
        {
            if (runtime == null || runtime.PhaseTriggered || zombie == null || !zombie.IsValid || runtime.MaxHealth <= 0f)
                return;

            var healthRatio = zombie.CurrentHealth / runtime.MaxHealth;
            if (healthRatio > 0.45f)
                return;

            runtime.PhaseTriggered = true;
            runtime.BonusPoints += 2;

            switch (runtime.Kind)
            {
                case EliteKind.Bloodbound:
                    runtime.IncomingDamageMultiplier *= 0.9f;
                    runtime.OutgoingDamageMultiplier *= 1.16f;
                    HealZombie(zombie, runtime, runtime.MaxHealth * 0.08f);
                    BurstAround(zombie.Position.Vector + Vector3.up * 1.25f, 1.1f, 5, 0.24f);
                    NotifyOwners(zombie.OriginalGoal, GetEliteDisplayName(runtime.Kind) + " enters a frenzy and starts feeding harder.");
                    break;

                case EliteKind.Stormtouched:
                    runtime.OutgoingDamageMultiplier *= 1.12f;
                    BurstAround(zombie.Position.Vector + Vector3.up * 1.45f, 1.25f, 6, 0.18f);
                    NotifyOwners(zombie.OriginalGoal, GetEliteDisplayName(runtime.Kind) + " overloads and begins calling heavier strikes.");
                    break;

                case EliteKind.Warbound:
                    runtime.IncomingDamageMultiplier *= 0.9f;
                    TriggerWarboundRally(zombie, runtime, emergencyPulse: true);
                    NotifyOwners(zombie.OriginalGoal, GetEliteDisplayName(runtime.Kind) + " hardens the horde around it.");
                    break;
            }
        }

        private static void TryTriggerEliteAbility(Zombie zombie, EliteRuntime runtime, double now)
        {
            if (runtime == null || zombie == null || !zombie.IsValid || now < runtime.NextAbilityAt)
                return;

            switch (runtime.Kind)
            {
                case EliteKind.Bloodbound:
                    TriggerBloodboundPulse(zombie, runtime);
                    runtime.NextAbilityAt = now + Random.Range(5.6f, 7.8f);
                    break;

                case EliteKind.Plagueborn:
                    TriggerPlaguebornRot(zombie, runtime);
                    runtime.NextAbilityAt = now + Random.Range(5.2f, 7.2f);
                    break;

                case EliteKind.Stormtouched:
                    TriggerStormtouchedStrike(zombie, runtime);
                    runtime.NextAbilityAt = now + Random.Range(6.4f, 8.4f);
                    break;

                case EliteKind.Warbound:
                    TriggerWarboundRally(zombie, runtime, emergencyPulse: false);
                    runtime.NextAbilityAt = now + Random.Range(6.8f, 8.8f);
                    break;

                default:
                    TriggerFeralLunge(zombie, runtime);
                    runtime.NextAbilityAt = now + Random.Range(5f, 6.8f);
                    break;
            }
        }

        private static void TriggerFeralLunge(Zombie zombie, EliteRuntime runtime)
        {
            var origin = zombie.Position.Vector + Vector3.up * 1.1f;
            if (TryGetClosestPlayer(zombie.OriginalGoal, zombie.Position.Vector, 5.5f, out var player))
            {
                var target = player.PositionStanding + Vector3.up * 1.05f;
                ServerManager.SendParticleTrail(origin + new Vector3(-0.45f, 0.25f, 0f), target, 0.14f);
                ServerManager.SendParticleTrail(origin + new Vector3(0.45f, 0.25f, 0f), target, 0.14f);
                Players.TakeHit(player, 10f, zombie, ModLoader.OnHitData.EHitSourceType.Monster);
                AudioManager.SendAudio(player.PositionStanding, "punch");
                return;
            }

            if (TryGetClosestFollower(zombie.OriginalGoal, zombie.Position.Vector, 5f, out var follower))
            {
                ServerManager.SendParticleTrail(origin + new Vector3(-0.45f, 0.25f, 0f), follower.Position.Vector + Vector3.up * 0.8f, 0.14f);
                ServerManager.SendParticleTrail(origin + new Vector3(0.45f, 0.25f, 0f), follower.Position.Vector + Vector3.up * 0.8f, 0.14f);
                follower.OnHit(10f);
                AudioManager.SendAudio(follower.Position.Vector, "punch");
                return;
            }

            BurstAround(origin, 0.85f, 4, 0.18f);
        }

        private static void TriggerBloodboundPulse(Zombie zombie, EliteRuntime runtime)
        {
            var origin = zombie.Position.Vector + Vector3.up * 1.2f;
            var drainedTargets = 0;
            var totalHeal = runtime.MaxHealth * 0.035f;

            foreach (var player in Players.PlayerDatabase.Values)
            {
                if (player?.ActiveColony != zombie.OriginalGoal || Vector3.Distance(player.PositionStanding, zombie.Position.Vector) > 6.2f)
                    continue;

                Players.TakeHit(player, 7f, zombie, ModLoader.OnHitData.EHitSourceType.Monster);
                ServerManager.SendParticleTrail(player.PositionStanding + Vector3.up * 0.9f, origin, 0.24f);
                totalHeal += 10f;
                drainedTargets++;
                if (drainedTargets >= 2)
                    break;
            }

            if (drainedTargets < 2 && zombie.OriginalGoal != null)
            {
                foreach (var follower in zombie.OriginalGoal.Followers)
                {
                    if (follower == null || Vector3.Distance(follower.Position.Vector, zombie.Position.Vector) > 5.8f)
                        continue;

                    follower.OnHit(6f);
                    ServerManager.SendParticleTrail(follower.Position.Vector + Vector3.up * 0.7f, origin, 0.24f);
                    totalHeal += 8f;
                    drainedTargets++;
                    if (drainedTargets >= 2)
                        break;
                }
            }

            HealZombie(zombie, runtime, totalHeal);
            AudioManager.SendAudio(zombie.Position.Vector, "fleshHit");
        }

        private static void TriggerPlaguebornRot(Zombie zombie, EliteRuntime runtime)
        {
            var center = zombie.Position.Vector + Vector3.up * 1.1f;
            var hits = 0;

            foreach (var player in Players.PlayerDatabase.Values)
            {
                if (player?.ActiveColony != zombie.OriginalGoal || Vector3.Distance(player.PositionStanding, zombie.Position.Vector) > 7f)
                    continue;

                Players.TakeHit(player, 5f, zombie, ModLoader.OnHitData.EHitSourceType.Monster);
                ServerManager.SendParticleTrail(center + new Vector3(0.35f, 0.1f, 0.2f), player.PositionStanding + Vector3.up * 1.05f, 0.32f);
                hits++;
            }

            if (zombie.OriginalGoal != null)
            {
                foreach (var follower in zombie.OriginalGoal.Followers)
                {
                    if (follower == null || Vector3.Distance(follower.Position.Vector, zombie.Position.Vector) > 6.6f)
                        continue;

                    follower.OnHit(4f);
                    ServerManager.SendParticleTrail(center + new Vector3(-0.25f, 0.05f, -0.25f), follower.Position.Vector + Vector3.up * 0.75f, 0.34f);
                    hits++;
                    if (hits >= 5)
                        break;
                }
            }

            if (hits == 0)
                BurstAround(center, 1.1f, 5, 0.3f);

            AudioManager.SendAudio(zombie.Position.Vector, "grassDelete");
        }

        private static void TriggerStormtouchedStrike(Zombie zombie, EliteRuntime runtime)
        {
            var origin = zombie.Position.Vector + Vector3.up * 2.4f;
            var strikes = 0;

            if (TryGetClosestPlayer(zombie.OriginalGoal, zombie.Position.Vector, 13f, out var player))
            {
                var target = player.PositionStanding + Vector3.up * 1.05f;
                ServerManager.SendParticleTrail(origin, target, 0.16f);
                Players.TakeHit(player, runtime.PhaseTriggered ? 12f : 9f, zombie, ModLoader.OnHitData.EHitSourceType.Monster);
                AudioManager.SendAudio(player.PositionStanding, "fleshHit");
                strikes++;
            }

            if (TryGetClosestFollower(zombie.OriginalGoal, zombie.Position.Vector, 12f, out var follower))
            {
                var target = follower.Position.Vector + Vector3.up * 0.8f;
                ServerManager.SendParticleTrail(origin + new Vector3(0.3f, 0f, -0.2f), target, 0.16f);
                follower.OnHit(runtime.PhaseTriggered ? 10f : 7f);
                AudioManager.SendAudio(follower.Position.Vector, "fleshHit");
                strikes++;
            }

            if (strikes == 0)
                BurstAround(zombie.Position.Vector + Vector3.up * 1.5f, 1.25f, 6, 0.16f);
        }

        private static void TriggerWarboundRally(Zombie zombie, EliteRuntime runtime, bool emergencyPulse)
        {
            var center = zombie.Position.Vector + Vector3.up * 1.05f;
            var rallied = 0;
            var healMultiplier = emergencyPulse ? 0.11f : 0.065f;

            foreach (var nearbyZombie in GetNearbyAlliedZombies(zombie, 9f))
            {
                if (nearbyZombie == null || !nearbyZombie.IsValid || nearbyZombie.ID == zombie.ID)
                    continue;

                var healAmount = Mathf.Max(10f, GetZombieMaxHealth(nearbyZombie) * healMultiplier);
                HealZombie(nearbyZombie, TryGetEliteRuntime(nearbyZombie), healAmount);
                ServerManager.SendParticleTrail(nearbyZombie.Position.Vector + Vector3.up * 0.95f, center, emergencyPulse ? 0.34f : 0.26f);
                rallied++;
                if (rallied >= (emergencyPulse ? 6 : 4))
                    break;
            }

            HealZombie(zombie, runtime, Mathf.Max(12f, runtime.MaxHealth * (emergencyPulse ? 0.08f : 0.045f)));

            if (rallied == 0)
                BurstAround(center, emergencyPulse ? 1.45f : 1.1f, emergencyPulse ? 8 : 6, 0.24f);

            AudioManager.SendAudio(zombie.Position.Vector, "grunt");
        }

        private static void TryAnnounceEliteSpawn(Zombie zombie, EliteKind kind)
        {
            if (zombie?.OriginalGoal == null)
                return;

            var now = Pipliz.Time.SecondsSinceStartDouble;
            if (ColonyAnnouncementCooldowns.TryGetValue(zombie.OriginalGoal, out var nextAllowed) && now < nextAllowed)
                return;

            ColonyAnnouncementCooldowns[zombie.OriginalGoal] = now + ColonyAnnouncementCooldownSeconds;

            var indicatorItemIndex = GetEliteIndicatorItemIndex(kind);
            if (indicatorItemIndex != 0 && PlayerUiGuard.CanBroadcastWorldEffects())
                Indicator.SendIconIndicatorNear(zombie.Position.Vector + Vector3.up * 1.4f, IndicatorState.NewItemIndicator(SpawnIndicatorSeconds, indicatorItemIndex));

            NotifyOwners(zombie.OriginalGoal, "Elite sighted: " + GetEliteDisplayName(kind) + ". " + GetEliteBehaviorSummary(kind));
        }

        private static void TryGrantEliteReward(Zombie zombie, EliteRuntime runtime)
        {
            var colony = zombie?.OriginalGoal;
            var stockpile = colony?.ColonyGroup?.Stockpile;
            if (colony == null || runtime == null)
                return;

            var granted = RollEliteReward(runtime.Kind);
            if (stockpile == null || granted == null)
            {
                NotifyOwners(colony, GetEliteDisplayName(runtime.Kind) + " was killed.");
                return;
            }

            stockpile.Add(granted.ItemIndex, granted.Amount);
            stockpile.SendToOwners();

            NotifyOwners(colony, GetEliteDisplayName(runtime.Kind) + " was killed. Bonus loot: " + granted.Amount + " " + granted.DisplayName + ".");
            ChatOwners(colony, "[Elite Loot] " + GetEliteDisplayName(runtime.Kind) + " dropped " + granted.Amount + " " + granted.DisplayName + " into the colony stockpile.");
        }

        private static ResolvedBossLootDrop RollEliteReward(EliteKind kind)
        {
            var definitions = GetRewardDefinitions(kind);
            var candidates = new List<EliteDropDefinition>();
            var totalWeight = 0f;

            for (var i = 0; i < definitions.Count; i++)
            {
                var definition = definitions[i];
                if (definition == null || definition.Chance <= 0f)
                    continue;

                candidates.Add(definition);
                totalWeight += Mathf.Max(0.001f, definition.Chance);
            }

            if (candidates.Count == 0 || totalWeight <= 0f)
                return null;

            var roll = Random.value * totalWeight;
            for (var i = 0; i < candidates.Count; i++)
            {
                var definition = candidates[i];
                roll -= Mathf.Max(0.001f, definition.Chance);
                if (roll > 0f && i < candidates.Count - 1)
                    continue;

                return BossLootTable.TryResolveRewardItem(definition.ItemKey, out var itemIndex, out var displayName)
                    ? new ResolvedBossLootDrop { ItemIndex = itemIndex, Amount = 1, DisplayName = displayName }
                    : null;
            }

            return null;
        }

        private static List<EliteDropDefinition> GetRewardDefinitions(EliteKind kind)
        {
            var settings = WandProgressionConfig.Current.Elites;
            if (settings.Drops != null &&
                settings.Drops.TryGetValue(kind.ToString(), out var configured) &&
                configured != null &&
                configured.Count > 0)
            {
                return configured;
            }

            return EliteProgressionSettings.CreateDefaultDrops().TryGetValue(kind.ToString(), out var defaults)
                ? defaults
                : new List<EliteDropDefinition>();
        }

        private static float GetZombieMaxHealth(Zombie zombie)
        {
            if (zombie != null && ActiveElites.TryGetValue(zombie.ID, out var runtime) && runtime.MaxHealth > 0f)
                return runtime.MaxHealth;

            return zombie == null ? 1f : Mathf.Max(1f, zombie.TotalHealth);
        }

        private static EliteRuntime TryGetEliteRuntime(Zombie zombie)
        {
            return zombie != null && ActiveElites.TryGetValue(zombie.ID, out var runtime)
                ? runtime
                : null;
        }

        private static void HealZombie(Zombie zombie, EliteRuntime runtime, float amount)
        {
            if (zombie == null || !zombie.IsValid || amount <= 0f)
                return;

            var maxHealth = runtime?.MaxHealth > 0f ? runtime.MaxHealth : Mathf.Max(1f, zombie.TotalHealth);
            zombie.CurrentHealth = Mathf.Min(maxHealth, zombie.CurrentHealth + amount);
        }

        private static IEnumerable<Zombie> GetNearbyAlliedZombies(Zombie source, float range)
        {
            var allMonsters = Pandaros.API.Monsters.MonsterManager.GetAllMonsters();
            if (source == null || allMonsters == null)
                yield break;

            var rangeSquared = range * range;
            foreach (var pair in allMonsters)
            {
                if (!(pair.Value is Zombie zombie) || !zombie.IsValid || zombie.OriginalGoal != source.OriginalGoal || pair.Value is IPandaBoss)
                    continue;

                if ((zombie.Position.Vector - source.Position.Vector).sqrMagnitude <= rangeSquared)
                    yield return zombie;
            }
        }

        private static bool TryGetClosestPlayer(Colony colony, Vector3 origin, float range, out Players.Player closestPlayer)
        {
            closestPlayer = null;
            var bestDistance = range * range;

            foreach (var player in Players.ConnectedPlayers)
            {
                if (player?.ActiveColony != colony)
                    continue;

                var distance = (player.PositionStanding - origin).sqrMagnitude;
                if (!(distance <= bestDistance))
                    continue;

                bestDistance = distance;
                closestPlayer = player;
            }

            return closestPlayer != null;
        }

        private static bool TryGetClosestFollower(Colony colony, Vector3 origin, float range, out NPCBase closestFollower)
        {
            closestFollower = null;
            if (colony == null)
                return false;

            var bestDistance = range * range;
            foreach (var follower in colony.Followers)
            {
                if (follower == null)
                    continue;

                var distance = (follower.Position.Vector - origin).sqrMagnitude;
                if (!(distance <= bestDistance))
                    continue;

                bestDistance = distance;
                closestFollower = follower;
            }

            return closestFollower != null;
        }

        private static void BurstAround(Vector3 center, float radius, int spokes, float duration)
        {
            for (var i = 0; i < spokes; i++)
            {
                var angle = (Mathf.PI * 2f * i) / Mathf.Max(1, spokes);
                var end = center + new Vector3(Mathf.Cos(angle) * radius, Random.Range(-0.15f, 0.35f), Mathf.Sin(angle) * radius);
                ServerManager.SendParticleTrail(center, end, duration);
            }
        }

        private static void PlayEliteSpawnFx(Zombie zombie, EliteKind kind)
        {
            if (zombie == null || !PlayerUiGuard.CanBroadcastWorldEffects())
                return;

            var settings = WandProgressionConfig.Current.Elites;
            var feet = zombie.Position.Vector + Vector3.up * 0.12f;
            var core = zombie.Position.Vector + Vector3.up * 1.15f;
            var radius = Mathf.Max(1.1f, settings.GlowPulseRadius);

            ServerManager.SendExplosionEffect(core, 8f, radius + 0.55f, 0.9f, 0.18f);
            for (var i = 0; i < settings.SpawnSmokeBursts; i++)
            {
                var angle = (Mathf.PI * 2f * i) / Mathf.Max(1, settings.SpawnSmokeBursts);
                var offset = new Vector3(Mathf.Cos(angle) * radius, Random.Range(0.15f, 0.65f), Mathf.Sin(angle) * radius);
                ServerManager.SendParticleTrail(feet + offset, core + Vector3.up * Random.Range(0.4f, 1.15f), 0.48f);
            }

            PlayEliteGlowFx(core, kind, radius, settings.GlowTrailSpokes + 2);
        }

        private static void UpdateEliteMarker(Zombie zombie, EliteRuntime runtime, bool force)
        {
            if (zombie == null || runtime == null || PlayerUiGuard.ShouldDeferPlayerFacingEffects())
                return;

            var now = Pipliz.Time.SecondsSinceStartDouble;
            if (!force && now < runtime.NextMarkerAt)
                return;

            var indicatorItemIndex = GetEliteIndicatorItemIndex(runtime.Kind);
            if (indicatorItemIndex == 0)
                return;

            Indicator.SendIconIndicatorNear(
                zombie.Position.Vector + Vector3.up * 1.85f,
                IndicatorState.NewItemIndicator(1.8f, indicatorItemIndex));
            runtime.NextMarkerAt = now + 0.85d;
        }

        private static void NotifyOwners(Colony colony, string message)
        {
            if (colony == null || string.IsNullOrEmpty(message))
                return;

            foreach (var player in Players.ConnectedPlayers)
            {
                if (PlayerUiGuard.CanSendStable(player) && player.OwnsColony(colony))
                    PlayerToastManager.Show(player, message, "#f0dcb4", 4600L);
            }
        }

        private static void ChatOwners(Colony colony, string message)
        {
            if (colony == null || string.IsNullOrEmpty(message))
                return;

            foreach (var player in Players.ConnectedPlayers)
            {
                if (PlayerUiGuard.CanSendStable(player) && player.OwnsColony(colony))
                    Chat.Send(player, message);
            }
        }

        private static string GetEliteDisplayName(EliteKind kind)
        {
            switch (kind)
            {
                case EliteKind.Bloodbound:
                    return "Bloodbound Elite";
                case EliteKind.Plagueborn:
                    return "Plagueborn Elite";
                case EliteKind.Stormtouched:
                    return "Stormtouched Elite";
                case EliteKind.Warbound:
                    return "Warbound Elite";
                default:
                    return "Feral Elite";
            }
        }

        private static string GetEliteBehaviorSummary(EliteKind kind)
        {
            switch (kind)
            {
                case EliteKind.Bloodbound:
                    return "It drains nearby defenders and heals through the fight.";
                case EliteKind.Plagueborn:
                    return "It rots defenders in a sick aura around itself.";
                case EliteKind.Stormtouched:
                    return "It calls lightning into nearby targets.";
                case EliteKind.Warbound:
                    return "It rallies nearby undead and makes the horde harder to break.";
                default:
                    return "It lunges harder than a normal zombie and rewards bonus colony points.";
            }
        }

        private static ushort GetEliteIndicatorItemIndex(EliteKind kind)
        {
            switch (kind)
            {
                case EliteKind.Bloodbound:
                    return Pandaros.Settlers.Items.BloodWand.Item.ItemIndex;
                case EliteKind.Plagueborn:
                    return Pandaros.Settlers.Items.VenomWand.Item.ItemIndex;
                case EliteKind.Stormtouched:
                    return Pandaros.Settlers.Items.StormWand.Item.ItemIndex;
                case EliteKind.Warbound:
                    return Pandaros.Settlers.Items.StoneWand.Item.ItemIndex;
                default:
                    return Pandaros.Settlers.Items.ManaWand.Item.ItemIndex;
            }
        }

        private static void PlayEliteFx(Zombie zombie, EliteKind kind)
        {
            if (!PlayerUiGuard.CanBroadcastWorldEffects())
                return;

            var settings = WandProgressionConfig.Current.Elites;
            var core = zombie.Position.Vector + new Vector3(0f, 1.35f, 0f);
            PlayEliteGlowFx(core, kind, settings.GlowPulseRadius, settings.GlowTrailSpokes);

            switch (kind)
            {
                case EliteKind.Bloodbound:
                    ServerManager.SendParticleTrail(core + new Vector3(-0.55f, 0.7f, -0.2f), core + new Vector3(0.55f, -0.3f, 0.2f), 0.28f);
                    ServerManager.SendParticleTrail(core + new Vector3(0.45f, 0.55f, 0.35f), core + new Vector3(-0.35f, -0.25f, -0.35f), 0.24f);
                    break;

                case EliteKind.Plagueborn:
                    ServerManager.SendParticleTrail(core + new Vector3(0.5f, -0.15f, 0.25f), core + new Vector3(-0.45f, 0.35f, -0.25f), 0.34f);
                    ServerManager.SendParticleTrail(core + new Vector3(-0.35f, 0.85f, -0.15f), core + new Vector3(0.15f, 0.15f, 0.25f), 0.28f);
                    break;

                case EliteKind.Stormtouched:
                    ServerManager.SendParticleTrail(core + new Vector3(-0.2f, 1.05f, 0.35f), core + new Vector3(0.3f, -0.1f, -0.35f), 0.16f);
                    ServerManager.SendParticleTrail(core + new Vector3(0.25f, 0.95f, -0.35f), core + new Vector3(-0.25f, 0.05f, 0.2f), 0.14f);
                    ServerManager.SendParticleTrail(core + new Vector3(0f, 1.45f, 0f), core + new Vector3(0f, -0.1f, 0f), 0.16f);
                    break;

                case EliteKind.Warbound:
                    ServerManager.SendParticleTrail(core + new Vector3(-0.65f, 0.65f, 0f), core + new Vector3(0.65f, -0.15f, 0f), 0.28f);
                    ServerManager.SendParticleTrail(core + new Vector3(0f, 0.9f, -0.65f), core + new Vector3(0f, -0.15f, 0.65f), 0.28f);
                    ServerManager.SendParticleTrail(core + new Vector3(0f, 1.25f, 0f), core + new Vector3(0f, -0.15f, 0f), 0.22f);
                    break;

                default:
                    ServerManager.SendParticleTrail(core + new Vector3(0f, 0.8f, 0f), core + new Vector3(0f, -0.2f, 0f), 0.22f);
                    ServerManager.SendParticleTrail(core + new Vector3(0.4f, 0.95f, 0f), core + new Vector3(-0.4f, 0.1f, 0f), 0.18f);
                    break;
            }
        }

        private static void PlayEliteGlowFx(Vector3 core, EliteKind kind, float radius, int spokes)
        {
            var pulseRadius = Mathf.Max(0.45f, radius);
            ServerManager.SendExplosionEffect(core, 8f, pulseRadius, 0.7f, 0.12f);

            for (var i = 0; i < spokes; i++)
            {
                var angle = (Mathf.PI * 2f * i) / Mathf.Max(1, spokes);
                var outer = core + new Vector3(Mathf.Cos(angle) * pulseRadius, Random.Range(-0.25f, 0.45f), Mathf.Sin(angle) * pulseRadius);
                var inner = core + new Vector3(Mathf.Cos(angle + 0.8f) * 0.25f, Random.Range(-0.3f, 0.25f), Mathf.Sin(angle + 0.8f) * 0.25f);
                ServerManager.SendParticleTrail(outer, inner, 0.34f);
            }

            if (kind == EliteKind.Stormtouched)
                ServerManager.SendParticleTrail(core + Vector3.up * 1.35f, core - Vector3.up * 0.25f, 0.18f);
        }

        private readonly struct EliteRewardDefinition
        {
            public EliteRewardDefinition(string itemKey, int minAmount, int maxAmount, float chance)
            {
                ItemKey = itemKey;
                MinAmount = minAmount;
                MaxAmount = maxAmount;
                Chance = chance;
            }

            public string ItemKey { get; }
            public int MinAmount { get; }
            public int MaxAmount { get; }
            public float Chance { get; }
        }
    }
}
