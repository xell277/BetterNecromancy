using Jobs;
using Monsters;
using NPC;
using Pandaros.API.Monsters;
using Pipliz;
using UnityEngine;

namespace BetterNecromancy
{
    [ModLoader.ModManager]
    public static class BossGuardTargetAssist
    {
        private const double UpdateIntervalSeconds = 0.25d;
        private static double _nextUpdateAt;

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnUpdate, ModEntry.Namespace + ".BossGuardTargetAssist.OnUpdate")]
        public static void OnUpdate()
        {
            if (!World.Initialized || Pipliz.Time.SecondsSinceStartDouble < _nextUpdateAt)
                return;

            _nextUpdateAt = Pipliz.Time.SecondsSinceStartDouble + UpdateIntervalSeconds;

            if (!MonsterManager.TryGetPrimaryActiveBoss(out var boss, out _) ||
                boss == null ||
                !boss.IsValid ||
                boss.CurrentHealth <= 0f)
            {
                return;
            }

            var colony = boss.OriginalGoal;
            if (colony?.ColonyGroup == null)
                return;

            var colonies = ServerManager.ColonyTracker.ColoniesByID.GetValueEnumerator();
            while (colonies.MoveNext())
            {
                var guardColony = colonies.Current;
                if (guardColony?.Followers == null || guardColony.ColonyGroup != colony.ColonyGroup)
                    continue;

                foreach (var follower in guardColony.Followers)
                    TryAssignBossTarget(follower, boss);
            }
        }

        private static void TryAssignBossTarget(NPCBase npc, Zombie boss)
        {
            if (npc == null || !npc.IsValid || !(npc.Job is GuardJobInstance guardJob) || !guardJob.IsValid)
                return;

            if (!(guardJob.Settings is GuardJobSettings guardSettings) || guardSettings.Range <= 0)
                return;

            if (guardJob.Target != null && guardJob.Target.IsValid && guardJob.Target == boss)
                return;

            var muzzle = guardJob.Position.Add(0, 1, 0).Vector;
            var aimPoint = boss.PositionToAimFor;
            var range = guardSettings.Range + 1f;
            if ((aimPoint - muzzle).sqrMagnitude > range * range)
                return;

            if (!VoxelPhysics.CanSee(muzzle, aimPoint))
                return;

            guardJob.Target = boss;
            npc.ResetCooldown();
        }
    }
}
