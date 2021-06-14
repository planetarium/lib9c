using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Nekoyume.Action;
using Nekoyume.Model.State;
using static Lib9c.SerializeKeys;

namespace Lib9c.Model.Order
{
    [Serializable]
    public class OrderReceiptList
    {
        [Serializable]
        public struct OrderReceipt
        {
            public readonly Guid OrderId;
            public readonly Guid TradableId;
            public readonly long StartedBlockIndex;
            public readonly long ExpiredBlockIndex;

            public OrderReceipt(Guid orderId, Guid tradableId, long startedBlockIndex, long expiredBlockIndex)
            {
                OrderId = orderId;
                TradableId = tradableId;
                StartedBlockIndex = startedBlockIndex;
                ExpiredBlockIndex = expiredBlockIndex;
            }

            public OrderReceipt(Dictionary serialized)
            {
                OrderId = serialized[OrderIdKey].ToGuid();
                TradableId = serialized[TradableIdKey].ToGuid();
                StartedBlockIndex = serialized[StartedBlockIndexKey].ToLong();
                ExpiredBlockIndex = serialized[ExpiredBlockIndexKey].ToLong();
            }

            public IValue Serialize()
            {
                return new Dictionary(new Dictionary<IKey, IValue>
                {
                    [(Text)OrderIdKey] = OrderId.Serialize(),
                    [(Text)TradableIdKey] = TradableId.Serialize(),
                    [(Text)StartedBlockIndexKey] = StartedBlockIndex.Serialize(),
                    [(Text)ExpiredBlockIndexKey] = ExpiredBlockIndex.Serialize(),
                });
            }

            public bool Equals(OrderReceipt other)
            {
                return OrderId.Equals(other.OrderId) &&
                       TradableId.Equals(other.TradableId) &&
                       StartedBlockIndex == other.StartedBlockIndex &&
                       ExpiredBlockIndex == other.ExpiredBlockIndex;
            }

            public override bool Equals(object obj)
            {
                return obj is OrderReceipt other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = OrderId.GetHashCode();
                    hashCode = (hashCode * 397) ^ TradableId.GetHashCode();
                    hashCode = (hashCode * 397) ^ StartedBlockIndex.GetHashCode();
                    hashCode = (hashCode * 397) ^ ExpiredBlockIndex.GetHashCode();
                    return hashCode;
                }
            }
        }

        public static Address DeriveAddress(Address avatarAddress)
        {
            return avatarAddress.Derive(nameof(OrderReceiptList));
        }

        public Address Address;

        private List<OrderReceipt> _receiptList = new List<OrderReceipt>();

        public IReadOnlyList<OrderReceipt> ReceiptList => _receiptList;

        public OrderReceiptList(Address address)
        {
            Address = address;
        }

        public OrderReceiptList(Dictionary serialized)
        {
            Address = serialized[AddressKey].ToAddress();
            _receiptList = serialized[OrderReceiptListKey]
                .ToList(m => new OrderReceipt((Dictionary)m))
                .OrderBy(o => o.OrderId)
                .ToList();
        }

        public IValue Serialize()
        {
            var innerDict = new Dictionary<IKey, IValue>
            {
                [(Text) AddressKey] = Address.Serialize(),
                [(Text) OrderReceiptListKey] = new List(_receiptList.Select(m => m.Serialize())),
            };

            return new Dictionary(innerDict);
        }

        public void Add(Order order)
        {
            var receipt = new OrderReceipt(order.OrderId, order.TradableId, order.StartedBlockIndex,
                order.ExpiredBlockIndex);
            if (_receiptList.Contains(receipt))
            {
                throw new DuplicateOrderIdException($"{order.OrderId} already exist.");
            }
            _receiptList.Add(receipt);
        }

        protected bool Equals(OrderReceiptList other)
        {
            return Address.Equals(other.Address) && _receiptList.SequenceEqual(other._receiptList);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((OrderReceiptList) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Address.GetHashCode() * 397) ^ (_receiptList != null ? _receiptList.GetHashCode() : 0);
            }
        }
    }
}
