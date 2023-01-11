using System;
using Libplanet.Assets;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.TableData;

namespace Lib9c.Model.Order
{
    public interface IItemOrder : IOrder
    {
        public void Validate(AvatarState avatarState, int count);
        public ITradableItem Sell(AvatarState avatarState);
        public OrderDigest Digest(AvatarState avatarState, CostumeStatSheet costumeStatSheet);
        public OrderReceipt Transfer(AvatarState seller, AvatarState buyer, long blockIndex);
        public ITradableItem Cancel(AvatarState avatarState, long blockIndex);
        public void ValidateCancelOrder(AvatarState avatarState, Guid tradableId);
        public int ValidateTransfer(AvatarState avatarState, Guid tradableId, FungibleAssetValue price, long blockIndex);

    }
}
