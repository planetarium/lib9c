using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Libplanet.Assets;
using Nekoyume.Action;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.State;
using static Lib9c.SerializeKeys;

namespace Lib9c.Model.Order
{
    [Serializable]
    public class FungibleOrder : Order
    {
        public readonly int ItemCount;

        public FungibleOrder(Address sellerAgentAddress,
            Address sellerAvatarAddress,
            Guid orderId,
            FungibleAssetValue price,
            Guid itemId,
            long startedBlockIndex,
            ItemSubType itemSubType,
            int itemCount
        ) : base(sellerAgentAddress,
            sellerAvatarAddress,
            orderId,
            price,
            itemId,
            startedBlockIndex,
            itemSubType
        )
        {
            ItemCount = itemCount;
        }

        public FungibleOrder(Dictionary serialized) : base(serialized)
        {
            ItemCount = serialized[TradableFungibleItemCountKey].ToInteger();
        }

        public override OrderType Type => OrderType.Fungible;

        public override IValue Serialize() =>
#pragma warning disable LAA1002
            new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text) TradableFungibleItemCountKey] = ItemCount.Serialize(),
            }.Union((Dictionary) base.Serialize()));
#pragma warning restore LAA1002

        public override void Validate(AvatarState avatarState, int count)
        {
            base.Validate(avatarState, count);

            if (ItemCount != count)
            {
                throw new InvalidItemCountException(
                    $"Aborted because {nameof(count)}({count}) should be 1 because {nameof(TradableId)}({TradableId}) is non-fungible item.");
            }

            if (!avatarState.inventory.TryGetTradableItems(TradableId, StartedBlockIndex, count, out List<Inventory.Item> inventoryItems))
            {
                throw new ItemDoesNotExistException(
                    $"Aborted because the tradable item({TradableId}) was failed to load from avatar's inventory.");
            }

            IEnumerable<ITradableItem> tradableItems = inventoryItems.Select(i => (ITradableItem)i.item).ToList();

            foreach (var tradableItem in tradableItems)
            {
                if (!tradableItem.ItemSubType.Equals(ItemSubType))
                {
                    throw new InvalidItemTypeException(
                        $"Expected ItemSubType: {tradableItem.ItemSubType}. Actual ItemSubType: {ItemSubType}");
                }
            }
        }

        public override ITradableItem Sell(AvatarState avatarState)
        {
            if (avatarState.inventory.TryGetTradableItems(TradableId, StartedBlockIndex, ItemCount, out List<Inventory.Item> items))
            {
                int totalCount = ItemCount;
                // Copy ITradableFungible item for separate inventory slots.
                ITradableFungibleItem copy = (ITradableFungibleItem) ((ITradableFungibleItem) items.First().item).Clone();
                foreach (var item in items)
                {
                    int removeCount = Math.Min(totalCount, item.count);
                    ITradableFungibleItem tradableFungibleItem = (ITradableFungibleItem) item.item;
                    avatarState.inventory.RemoveTradableItem(TradableId, tradableFungibleItem.RequiredBlockIndex, removeCount);
                    totalCount -= removeCount;
                    if (totalCount < 1)
                    {
                        break;
                    }
                }
                // Lock item.
                copy.RequiredBlockIndex = ExpiredBlockIndex;
                avatarState.inventory.AddItem((ItemBase) copy, ItemCount);
                return copy;
            }

            throw new ItemDoesNotExistException(
                $"Can't find available item in seller inventory. TradableId: {TradableId}. RequiredBlockIndex: {StartedBlockIndex}, Count: {ItemCount}");
        }

        protected bool Equals(FungibleOrder other)
        {
            return base.Equals(other) && ItemCount == other.ItemCount;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((FungibleOrder) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (base.GetHashCode() * 397) ^ ItemCount;
            }
        }
    }
}
