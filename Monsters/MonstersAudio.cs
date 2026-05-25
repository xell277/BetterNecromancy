using Monsters;
using Pandaros.API.Monsters;

namespace Pandaros.Settlers.AI
{
    [ModLoader.ModManager]
    public static class MonstersAudio
    {
        private static double _nextUpdateTime;

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnUpdate, GameLoader.NAMESPACE + ".Managers.MonstersAudio.Update")]
        public static void OnUpdate()
        {
            var tuning = BetterNecromancy.BossTuning.Current;

            if (!World.Initialized || _nextUpdateTime >= Pipliz.Time.SecondsSinceStartDouble)
                return;

            IMonster selectedMonster = null;
            var monsters = MonsterManager.GetAllMonsters();

            if (monsters == null || monsters.Count == 0)
            {
                _nextUpdateTime = Pipliz.Time.SecondsSinceStartDouble + tuning.ZombieAudioIntervalSeconds;
                return;
            }

            var enumerator = monsters.GetEnumerator();
            while (enumerator.MoveNext())
            {
                var candidate = enumerator.Current.Value;
                if (candidate == null || !candidate.IsValid || !(candidate is IPandaZombie))
                    continue;

                if (selectedMonster == null ||
                    (UnityEngine.Vector3.Distance(candidate.PositionToAimFor, selectedMonster.PositionToAimFor) > 15f &&
                     Pipliz.Random.NextBool()))
                {
                    selectedMonster = candidate;
                }
            }

            if (selectedMonster != null)
                AudioManager.SendAudio(selectedMonster.PositionToAimFor, "grunt");

            _nextUpdateTime = Pipliz.Time.SecondsSinceStartDouble + tuning.ZombieAudioIntervalSeconds;
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnMonsterHit, GameLoader.NAMESPACE + ".Managers.MonstersAudio.OnMonsterHit")]
        public static void OnMonsterHit(IMonster monster, ModLoader.OnHitData hitData)
        {
            if (monster == null || hitData == null || !monster.IsValid || !(monster is IPandaZombie))
                return;

            if (Pipliz.Random.NextFloat() <= BetterNecromancy.BossTuning.Current.ZombieAudioHitChance)
                AudioManager.SendAudio(monster.PositionToAimFor, "fleshHit");
        }
    }
}
