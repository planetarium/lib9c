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
    public class ShardedShopStateV3 : State
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
                    return Addresses.Shop.Derive($"v3-{itemSubType}-{nonce}");
                case ItemSubType.FullCostume:
                case ItemSubType.HairCostume:
                case ItemSubType.EarCostume:
                case ItemSubType.EyeCostume:
                case ItemSubType.TailCostume:
                case ItemSubType.Title:
                    return Addresses.Shop.Derive($"v3-{itemSubType}");
                default:
                    throw new InvalidItemTypeException($"Unsupported ItemType: {itemSubType}");
            }
        }

        public readonly List<Guid> OrderList = new List<Guid>();

        public ShardedShopStateV3(Address address) : base(address)
        {
        }

        public ShardedShopStateV3(Dictionary serialized) : base(serialized)
        {
            OrderList = serialized[ProductsKey]
                .ToList(s => s.ToGuid());
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
