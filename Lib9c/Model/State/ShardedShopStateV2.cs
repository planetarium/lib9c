using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Lib9c.Model.Order;
using Libplanet;
using Nekoyume.Action;
using Nekoyume.Model.Item;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Model.State
{
    public class ShardedShopStateV2 : State
    {
        public static Address DeriveAddress(ItemSubType itemSubType, Guid tradableId)
        {
            string nonce = tradableId.ToString().Substring(0, 1);
            return DeriveAddress(itemSubType, nonce);
        }

        public static readonly IReadOnlyList<string> AddressKeys = new List<string>
        {
            "0",
            "1",
            "2",
            "3",
            "4",
            "5",
            "6",
            "7",
            "8",
            "9",
            "a",
            "b",
            "c",
            "d",
            "e",
            "f",
        };

        public static Address DeriveAddress(ItemSubType itemSubType, string nonce)
        {
            switch (itemSubType)
            {
                case ItemSubType.Weapon:
                case ItemSubType.Armor:
                case ItemSubType.Belt:
                case ItemSubType.Necklace:
                case ItemSubType.Ring:
                case ItemSubType.Food:
                case ItemSubType.Hourglass:
                case ItemSubType.ApStone:
                    return Addresses.Shop.Derive($"order-{itemSubType}-{nonce}");
                case ItemSubType.FullCostume:
                case ItemSubType.HairCostume:
                case ItemSubType.EarCostume:
                case ItemSubType.EyeCostume:
                case ItemSubType.TailCostume:
                case ItemSubType.Title:
                    return Addresses.Shop.Derive($"order-{itemSubType}");
                default:
                    throw new InvalidItemTypeException($"Unsupported ItemType: {itemSubType}");
            }
        }

        public readonly List<Order> OrderList = new List<Order>();

        public ShardedShopStateV2(Address address) : base(address)
        {
        }

        public ShardedShopStateV2(Dictionary serialized) : base(serialized)
        {
            OrderList = serialized[ProductsKey]
                .ToList(s => OrderFactory.Deserialize((Dictionary) s));
        }

        public override IValue Serialize() =>
#pragma warning disable LAA1002
            new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text) ProductsKey] = new List(OrderList.Select(o => o.Serialize()))
            }.Union((Dictionary) base.Serialize()));
#pragma warning restore LAA1002
    }
}
