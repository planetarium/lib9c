using System;
using System.IO;
using Bencodex.Types;
using Libplanet;
using Libplanet.Assets;
using Nekoyume.Action;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;

namespace Lib9c.Model.Order
{
    [Serializable]
    public class NonFungibleOrder : Order
    {
        public NonFungibleOrder(Address sellerAgentAddress,
            Address sellerAvatarAddress,
            Guid orderId,
            FungibleAssetValue price,
            Guid itemId,
            long startedBlockIndex,
            ItemSubType itemSubType
        ) : base(
            sellerAgentAddress,
            sellerAvatarAddress,
            orderId,
            price,
            itemId,
            startedBlockIndex,
            itemSubType
        )
        {
        }

        public NonFungibleOrder(Dictionary serialized) : base(serialized)
        {
        }

        public override OrderType Type => OrderType.NonFungible;

        public override void Validate(AvatarState avatarState, int count)
        {
            base.Validate(avatarState, count);

            if (count != 1)
            {
                throw new InvalidItemCountException(
                    $"Aborted because {nameof(count)}({count}) should be 1 because {nameof(TradableId)}({TradableId}) is non-fungible item.");
            }

            if (!avatarState.inventory.TryGetNonFungibleItem(TradableId, out INonFungibleItem nonFungibleItem))
            {
                throw new ItemDoesNotExistException(
                    $"Aborted because the tradable item({TradableId}) was failed to load from avatar's inventory.");
            }

            if (!nonFungibleItem.ItemSubType.Equals(ItemSubType))
            {
                throw new InvalidItemTypeException(
                    $"Expected ItemSubType: {nonFungibleItem.ItemSubType}. Actual ItemSubType: {ItemSubType}");
            }

            if (nonFungibleItem.RequiredBlockIndex > StartedBlockIndex)
            {
                throw new RequiredBlockIndexException(
                    $"Aborted as the itemUsable to sell ({TradableId}) is not available yet; it will be available at the block #{nonFungibleItem.RequiredBlockIndex}.");
            }
        }

        public override ITradableItem Sell(AvatarState avatarState)
        {
            if (avatarState.inventory.TryGetNonFungibleItem(TradableId, out INonFungibleItem nonFungibleItem))
            {
                nonFungibleItem.RequiredBlockIndex = ExpiredBlockIndex;
                if (nonFungibleItem is Equipment equipment)
                {
                    equipment.Unequip();
                }
                return nonFungibleItem;
            }

            throw new ItemDoesNotExistException(
                $"Aborted because the tradable item({TradableId}) was failed to load from avatar's inventory.");
        }
    }
}
