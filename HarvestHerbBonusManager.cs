using System.Collections.Generic;
using Jobs;

namespace BetterNecromancy
{
    [ModLoader.ModManager]
    public static class HarvestHerbBonusManager
    {
        private const float HerbHarvestChance = 0.01f;
        private const string HerbsTypeName = "BetterNecromancy.Herbs";
        private const string WheatTypeName = "wheat";
        private const string FlaxTypeName = "flax";
        private const string WheatFarmerNpcType = "pipliz.wheatfarmer";
        private const string FlaxFarmerNpcType = "pipliz.flaxfarmer";

        private static ushort _herbsItemIndex;
        private static ushort _wheatItemIndex;
        private static ushort _flaxItemIndex;
        private static ushort _wheatFarmerNpcTypeIndex;
        private static ushort _flaxFarmerNpcTypeIndex;

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AfterItemTypesDefined, ModEntry.Namespace + ".HarvestHerbBonusManager.AfterItemTypesDefined")]
        [ModLoader.ModCallbackProvidesFor("pipliz.server.loadresearchables")]
        public static void AfterItemTypesDefined()
        {
            _herbsItemIndex = ItemTypes.GetType(HerbsTypeName).ItemIndex;
            _wheatItemIndex = ItemTypes.GetType(WheatTypeName).ItemIndex;
            _flaxItemIndex = ItemTypes.GetType(FlaxTypeName).ItemIndex;
            _wheatFarmerNpcTypeIndex = NPC.NPCType.NPCTypesByKeyName[WheatFarmerNpcType].Type;
            _flaxFarmerNpcTypeIndex = NPC.NPCType.NPCTypesByKeyName[FlaxFarmerNpcType].Type;
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnNPCGathered, ModEntry.Namespace + ".HarvestHerbBonusManager.OnNPCGathered")]
        public static void OnNPCGathered(IJob job, Pipliz.Vector3Int gatherPosition, List<ItemTypes.ItemTypeDrops> results)
        {
            if (job == null || results == null || results.Count == 0 || _herbsItemIndex == 0)
                return;

            var npcType = job.NPCType.Type;
            var isWheatFarmer = npcType == _wheatFarmerNpcTypeIndex;
            var isFlaxFarmer = npcType == _flaxFarmerNpcTypeIndex;

            if (!isWheatFarmer && !isFlaxFarmer)
                return;

            var matchingHarvest = false;
            for (var i = 0; i < results.Count; i++)
            {
                var resultType = results[i].Type;
                if ((isWheatFarmer && resultType == _wheatItemIndex) ||
                    (isFlaxFarmer && resultType == _flaxItemIndex))
                {
                    matchingHarvest = true;
                    break;
                }
            }

            if (!matchingHarvest)
                return;

            results.Add(new ItemTypes.ItemTypeDrops(_herbsItemIndex, 1, HerbHarvestChance));
        }
    }
}
