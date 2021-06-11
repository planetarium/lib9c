using System;
using System.Collections.Generic;
using Bencodex.Types;
using Libplanet.Assets;
using Nekoyume.Model.Elemental;
using Nekoyume.Model.State;
using static Lib9c.SerializeKeys;

namespace Lib9c.Model.Order
{
    [Serializable]
    public class OrderDigest
    {
        public readonly long StartedBlockIndex;
        public readonly long ExpiredBlockIndex;
        public readonly Guid OrderId;
        public readonly Guid TradableId;

        // Client filter data
        public readonly FungibleAssetValue Price;
        public readonly int CombatPoint;
        public readonly int Grade;
        public readonly int Level;
        public readonly ElementalType ElementalType;
        public readonly int ItemId;

        public OrderDigest(long startedBlockIndex,
            long expiredBlockIndex,
            Guid orderId,
            Guid tradableId,
            FungibleAssetValue price,
            int combatPoint,
            int grade,
            int level,
            ElementalType elementalType,
            int itemId)
        {
            StartedBlockIndex = startedBlockIndex;
            ExpiredBlockIndex = expiredBlockIndex;
            OrderId = orderId;
            TradableId = tradableId;
            Price = price;
            CombatPoint = combatPoint;
            Grade = grade;
            Level = level;
            ElementalType = elementalType;
            ItemId = itemId;
        }

        public OrderDigest(Dictionary serialized)
        {
            StartedBlockIndex = serialized[StartedBlockIndexKey].ToLong();
            ExpiredBlockIndex = serialized[ExpiredBlockIndexKey].ToLong();
            OrderId = serialized[OrderIdKey].ToGuid();
            TradableId = serialized[TradableIdKey].ToGuid();
            Price = serialized[PriceKey].ToFungibleAssetValue();
            CombatPoint = serialized[CombatPointKey].ToInteger();
            Grade = serialized[GradeKey].ToInteger();
            Level = serialized[LevelKey].ToInteger();
            ElementalType = serialized[ElementalTypeKey].ToEnum<ElementalType>();
            ItemId = serialized[ItemIdKey].ToInteger();
        }

        public IValue Serialize()
        {
            var innerDict = new Dictionary<IKey, IValue>
            {
                [(Text)OrderIdKey] = OrderId.Serialize(),
                [(Text)TradableIdKey] = TradableId.Serialize(),
                [(Text)StartedBlockIndexKey] = StartedBlockIndex.Serialize(),
                [(Text)ExpiredBlockIndexKey] = ExpiredBlockIndex.Serialize(),
                [(Text)PriceKey] = Price.Serialize(),
                [(Text)CombatPointKey] = CombatPoint.Serialize(),
                [(Text)GradeKey] = Grade.Serialize(),
                [(Text)LevelKey] = Level.Serialize(),
                [(Text)ElementalTypeKey] = ElementalType.Serialize(),
                [(Text)ItemIdKey] = ItemId.Serialize(),
            };

            return new Dictionary(innerDict);
        }

        protected bool Equals(OrderDigest other)
        {
            return StartedBlockIndex == other.StartedBlockIndex &&
                   ExpiredBlockIndex == other.ExpiredBlockIndex &&
                   OrderId.Equals(other.OrderId) &&
                   TradableId.Equals(other.TradableId) &&
                   Price.Equals(other.Price) &&
                   CombatPoint == other.CombatPoint &&
                   Grade == other.Grade &&
                   Level == other.Level &&
                   ElementalType == other.ElementalType &&
                   ItemId == other.ItemId;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((OrderDigest) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = StartedBlockIndex.GetHashCode();
                hashCode = (hashCode * 397) ^ ExpiredBlockIndex.GetHashCode();
                hashCode = (hashCode * 397) ^ OrderId.GetHashCode();
                hashCode = (hashCode * 397) ^ TradableId.GetHashCode();
                hashCode = (hashCode * 397) ^ Price.GetHashCode();
                hashCode = (hashCode * 397) ^ CombatPoint;
                hashCode = (hashCode * 397) ^ Grade;
                hashCode = (hashCode * 397) ^ Level;
                hashCode = (hashCode * 397) ^ (int) ElementalType;
                hashCode = (hashCode * 397) ^ ItemId;
                return hashCode;
            }
        }
    }
}
