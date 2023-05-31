#nullable enable

using System;
using Bencodex.Types;
using Nekoyume.Model.Item;

namespace Nekoyume.Model.Garages
{
    public class FungibleItemGarage : IFungibleItemGarage
    {
        public IFungibleItem Item { get; }
        public int Count { get; private set; }

        public FungibleItemGarage(IFungibleItem item, int count)
        {
            Item = item ?? throw new ArgumentNullException(nameof(item));

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(count),
                    count,
                    "Count must be greater than or equal to 0.");
            }

            Count = count;
        }

        public FungibleItemGarage(IValue? serialized)
        {
            if (serialized is null || serialized is Null)
            {
                throw new ArgumentNullException(nameof(serialized));
            }

            var list = (List)serialized;
            Item = list[0].Kind == ValueKind.Null
                ? throw new ArgumentNullException(nameof(serialized), "Item is null.")
                : (IFungibleItem)ItemFactory.Deserialize((Dictionary)list[0]);
            Count = (Integer)list[1];
        }

        public IValue Serialize() => Count == 0
            ? (IValue)Null.Value
            : new List(
                Item?.Serialize() ?? Null.Value,
                (Integer)Count);

        public void Add(int count)
        {
            if (Count + count < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(count),
                    count,
                    $"Count must be greater than or equal to {-Count}.");
            }

            if (count > int.MaxValue - Count)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(count),
                    count,
                    $"Count must be less than or equal to {int.MaxValue - Count}.");
            }

            Count += count;
        }

        public void TransferTo(IFungibleItemGarage garage, int count)
        {
            if (garage is null)
            {
                throw new ArgumentNullException(nameof(garage));
            }

            // NOTE: Why not compare the garage.Item with this.Item directly?
            //       Because the ITradableFungibleItem.Equals() method compares the
            //       ITradableItem.RequiredBlockIndex property.
            //       The IFungibleItem.FungibleId property fully contains the
            //       specification of the fungible item.
            //       So ITradableItem.RequiredBlockIndex property does not considered
            //       when transferring items via garage.
            if (!garage.Item.FungibleId.Equals(Item.FungibleId))
            {
                throw new ArgumentException(
                    $"Item type mismatched. {garage.Item.FungibleId} != {Item.FungibleId}",
                    nameof(garage));
            }

            if (count <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(count),
                    count,
                    "Count must be greater than 0.");
            }

            if (Count < count)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(count),
                    count,
                    $"Count must be less than or equal to {Count}.");
            }

            Count -= count;
            garage.Add(count);
        }
    }
}
