using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Bencodex;
using Bencodex.Types;
using Libplanet;
using Libplanet.Assets;
using Nekoyume.Model.State;

namespace Nekoyume.Model.Item
{
    [Serializable]
    public class ShopItem
    {
        public const string ExpiredBlockIndexKey = "ebi";
        protected static readonly Codec Codec = new Codec();
        
        public readonly Address SellerAgentAddress;
        public readonly Address SellerAvatarAddress;
        public readonly Guid ProductId;
        public readonly FungibleAssetValue Price;
        public readonly ItemUsable ItemUsable;
        public readonly Costume Costume;
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

        public ShopItem(Address sellerAgentAddress,
            Address sellerAvatarAddress,
            Guid productId,
            FungibleAssetValue price,
            ItemUsable itemUsable) : this(sellerAgentAddress, sellerAvatarAddress, productId, price, 0, itemUsable)
        {
        }

        public ShopItem(Address sellerAgentAddress,
            Address sellerAvatarAddress,
            Guid productId,
            FungibleAssetValue price,
            Costume costume) : this(sellerAgentAddress, sellerAvatarAddress, productId, price, 0, costume)
        {
        }

        public ShopItem(
            Address sellerAgentAddress,
            Address sellerAvatarAddress,
            Guid productId,
            FungibleAssetValue price,
            long expiredBlockIndex,
            INonFungibleItem nonFungibleItem
        )
        {
            SellerAgentAddress = sellerAgentAddress;
            SellerAvatarAddress = sellerAvatarAddress;
            ProductId = productId;
            Price = price;
            ExpiredBlockIndex = expiredBlockIndex;
            switch (nonFungibleItem)
            {
                case ItemUsable itemUsable:
                    ItemUsable = itemUsable;
                    Costume = null;
                    break;
                case Costume costume:
                    ItemUsable = null;
                    Costume = costume;
                    break;
            }
        }

        public ShopItem(Dictionary serialized)
        {
            SellerAgentAddress = serialized["sellerAgentAddress"].ToAddress();
            SellerAvatarAddress = serialized["sellerAvatarAddress"].ToAddress();
            ProductId = serialized["productId"].ToGuid();
            Price = serialized["price"].ToFungibleAssetValue();
            ItemUsable = serialized.ContainsKey("itemUsable")
                ? (ItemUsable) ItemFactory.Deserialize((Dictionary) serialized["itemUsable"])
                : null;
            Costume = serialized.ContainsKey("costume")
                ? (Costume) ItemFactory.Deserialize((Dictionary) serialized["costume"])
                : null;
            if (serialized.ContainsKey(ExpiredBlockIndexKey))
            {
                ExpiredBlockIndex = serialized[ExpiredBlockIndexKey].ToLong();
            }
        }
        
        protected ShopItem(SerializationInfo info, StreamingContext _)
            : this((Dictionary) Codec.Decode((byte[]) info.GetValue("serialized", typeof(byte[]))))
        {
        }
        
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }
            
            info.AddValue("serialized", Codec.Encode(Serialize()));
        }

        public IValue Serialize()
        {
            var innerDictionary = new Dictionary<IKey, IValue>
            {
                [(Text) "sellerAgentAddress"] = SellerAgentAddress.Serialize(),
                [(Text) "sellerAvatarAddress"] = SellerAvatarAddress.Serialize(),
                [(Text) "productId"] = ProductId.Serialize(),
                [(Text) "price"] = Price.Serialize(),
            };

            if (ItemUsable != null)
            {
                innerDictionary.Add((Text) "itemUsable", ItemUsable.Serialize());
            }

            if (Costume != null)
            {
                innerDictionary.Add((Text) "costume", Costume.Serialize());
            }

            if (ExpiredBlockIndex != 0)
            {
                innerDictionary.Add((Text) ExpiredBlockIndexKey, ExpiredBlockIndex.Serialize());
            }

            return new Dictionary(innerDictionary);
        }


        public IValue SerializeBackup1() =>
            new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text) "sellerAgentAddress"] = SellerAgentAddress.Serialize(),
                [(Text) "sellerAvatarAddress"] = SellerAvatarAddress.Serialize(),
                [(Text) "productId"] = ProductId.Serialize(),
                [(Text) "itemUsable"] = ItemUsable.Serialize(),
                [(Text) "price"] = Price.Serialize(),
            });

        protected bool Equals(ShopItem other)
        {
            return ProductId.Equals(other.ProductId);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((ShopItem) obj);
        }

        public override int GetHashCode()
        {
            return ProductId.GetHashCode();
        }
    }
}
