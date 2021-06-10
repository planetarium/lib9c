using System;
using System.Collections.Generic;
using Bencodex.Types;
using Libplanet;
using Libplanet.Assets;
using Nekoyume;
using Nekoyume.Action;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using static Lib9c.SerializeKeys;

namespace Lib9c.Model.Order
{
    [Serializable]
    public abstract class Order
    {
        public const long ExpirationInterval = 36000;

        public static Address DeriveAddress(Guid orderId)
        {
            return Addresses.Shop.Derive(orderId.ToString());
        }

        public enum OrderType
        {
            Fungible,
            NonFungible,
        }

        public abstract OrderType Type { get; }
        public Address SellerAgentAddress { get; }
        public Address SellerAvatarAddress { get; }
        public Guid OrderId { get; }
        public FungibleAssetValue Price { get; }
        public Guid TradableId { get; }
        public long StartedBlockIndex { get; }
        public ItemSubType ItemSubType { get; }
        private long _expiredBlockIndex;

        public long ExpiredBlockIndex
        {
            get => _expiredBlockIndex;
            private set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException($"{nameof(ExpiredBlockIndex)} must be 0 or more, but {value}");
                }

                _expiredBlockIndex = value;
            }
        }

        protected Order(Address sellerAgentAddress,
            Address sellerAvatarAddress,
            Guid orderId,
            FungibleAssetValue price,
            Guid itemId,
            long startedBlockIndex,
            ItemSubType itemSubType
        )
        {
            SellerAgentAddress = sellerAgentAddress;
            SellerAvatarAddress = sellerAvatarAddress;
            Price = price;
            OrderId = orderId;
            TradableId = itemId;
            StartedBlockIndex = startedBlockIndex;
            ExpiredBlockIndex = startedBlockIndex + ExpirationInterval;
            ItemSubType = itemSubType;
        }

        protected Order(Dictionary serialized)
        {
            SellerAgentAddress = serialized[SellerAgentAddressKey].ToAddress();
            SellerAvatarAddress = serialized[SellerAvatarAddressKey].ToAddress();
            OrderId = serialized[OrderIdKey].ToGuid();
            Price = serialized[PriceKey].ToFungibleAssetValue();
            TradableId = serialized[ItemIdKey].ToGuid();
            ExpiredBlockIndex = serialized[ExpiredBlockIndexKey].ToLong();
            StartedBlockIndex = serialized[StartedBlockIndexKey].ToLong();
            ItemSubType = serialized[ItemSubTypeKey].ToEnum<ItemSubType>();
        }

        public virtual IValue Serialize()
        {
            var innerDictionary = new Dictionary<IKey, IValue>
            {
                [(Text) SellerAgentAddressKey] = SellerAgentAddress.Serialize(),
                [(Text) SellerAvatarAddressKey] = SellerAvatarAddress.Serialize(),
                [(Text) OrderIdKey] = OrderId.Serialize(),
                [(Text) PriceKey] = Price.Serialize(),
                [(Text) ItemIdKey] = TradableId.Serialize(),
                [(Text) ExpiredBlockIndexKey] = ExpiredBlockIndex.Serialize(),
                [(Text) StartedBlockIndexKey] = StartedBlockIndex.Serialize(),
                [(Text) OrderTypeKey] = Type.Serialize(),
                [(Text) ItemSubTypeKey] = ItemSubType.Serialize(),
            };

            return new Dictionary(innerDictionary);
        }

        public virtual void Validate(AvatarState avatarState, int count)
        {
            if (!avatarState.address.Equals(SellerAvatarAddress) || !avatarState.agentAddress.Equals(SellerAgentAddress))
            {
                throw new InvalidAddressException();
            }

            if (count < 1)
            {
                throw new InvalidItemCountException("item count must be greater than 0.");
            }
        }

        public abstract ITradableItem Sell(AvatarState avatarState);

        #region Equals

        protected bool Equals(Order other)
        {
            return _expiredBlockIndex == other._expiredBlockIndex &&
                   Type == other.Type &&
                   SellerAgentAddress.Equals(other.SellerAgentAddress) &&
                   SellerAvatarAddress.Equals(other.SellerAvatarAddress) &&
                   OrderId.Equals(other.OrderId) &&
                   Price.Equals(other.Price) &&
                   TradableId.Equals(other.TradableId) &&
                   StartedBlockIndex == other.StartedBlockIndex &&
                   ItemSubType == other.ItemSubType;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Order) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = _expiredBlockIndex.GetHashCode();
                hashCode = (hashCode * 397) ^ (int) Type;
                hashCode = (hashCode * 397) ^ SellerAgentAddress.GetHashCode();
                hashCode = (hashCode * 397) ^ SellerAvatarAddress.GetHashCode();
                hashCode = (hashCode * 397) ^ OrderId.GetHashCode();
                hashCode = (hashCode * 397) ^ Price.GetHashCode();
                hashCode = (hashCode * 397) ^ TradableId.GetHashCode();
                hashCode = (hashCode * 397) ^ StartedBlockIndex.GetHashCode();
                hashCode = (hashCode * 397) ^ (int) ItemSubType;
                return hashCode;
            }
        }

        #endregion

    }
}
