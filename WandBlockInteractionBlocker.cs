using ModLoaderInterfaces;
using Pandaros.Settlers.Items;
using Shared;

namespace BetterNecromancy
{
    public sealed class WandBlockInteractionBlocker : IOnTryChangeBlock
    {
        public void OnTryChangeBlock(ModLoader.OnTryChangeBlockData data)
        {
            if (data == null ||
                data.CallbackOrigin != ModLoader.OnTryChangeBlockData.ECallbackOrigin.ClientPlayerManual)
            {
                return;
            }

            var clickData = data.PlayerClickedData;
            if (clickData == null || !IsWandSelected(clickData.TypeSelected))
            {
                return;
            }

            clickData.ConsumedType = PlayerClickedData.EConsumedType.ChangedBlock;
            data.CallbackConsumedResult = EServerChangeBlockResult.CancelledByCallback;
            data.CallbackState = ModLoader.OnTryChangeBlockData.ECallbackState.Cancelled;
        }

        private static bool IsWandSelected(ushort selectedType)
        {
            return IsSelectedType(ManaWand.Item, selectedType) ||
                   IsSelectedType(EmberWand.Item, selectedType) ||
                   IsSelectedType(FrostWand.Item, selectedType) ||
                   IsSelectedType(MagicWand.Item, selectedType) ||
                   IsSelectedType(VoidWand.Item, selectedType);
        }

        private static bool IsSelectedType(ItemTypesServer.ItemTypeRaw item, ushort selectedType)
        {
            return item != null && item.ItemIndex == selectedType;
        }
    }
}
