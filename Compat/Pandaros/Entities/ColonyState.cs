using System;
using System.Collections.Generic;

namespace Pandaros.API.Entities
{
    [ModLoader.ModManager]
    public class ColonyState
    {
        private static readonly Dictionary<Colony, ColonyState> ColonyStates = new Dictionary<Colony, ColonyState>();

        public ColonyState()
        {
        }

        public ColonyState(Colony colony)
        {
            ColonyRef = colony;
        }

        public Colony ColonyRef { get; set; }
        public int FaiedBossSpawns { get; set; }
        public GameDifficulty Difficulty { get; set; } = GameDifficulty.Normal;
        public bool BossesEnabled { get; set; } = true;
        public bool MonstersEnabled { get; set; } = true;
        public bool IsPeaceful =>
            Difficulty != null &&
            (Difficulty.Rank <= 0 || string.Equals(Difficulty.Name, "Peaceful", StringComparison.OrdinalIgnoreCase));

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnCreatedColony, BetterNecromancy.ModEntry.Namespace + ".Compat.ColonyState.OnCreatedColony")]
        public static void OnCreatedColony(Colony colony)
        {
            if (!ColonyStates.ContainsKey(colony))
                ColonyStates.Add(colony, new ColonyState(colony));
        }

        public static ColonyState GetColonyState(Colony colony)
        {
            if (colony == null)
                return null;

            if (!ColonyStates.TryGetValue(colony, out var state))
            {
                state = new ColonyState(colony);
                ColonyStates.Add(colony, state);
            }

            return state;
        }

        public static bool IsPeacefulColony(Colony colony)
        {
            return GetColonyState(colony)?.IsPeaceful ?? false;
        }
    }
}
