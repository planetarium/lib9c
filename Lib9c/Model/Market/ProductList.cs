using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Lib9c;
using Libplanet;
using Nekoyume.Action;
using Nekoyume.Model.State;

namespace Nekoyume.Model.Market
{
    public class ProductList
    {
        public static Address DeriveAddress(Address avatarAddress) =>
            avatarAddress.Derive(nameof(ProductList));

        public List<Guid> ProductIdList = new List<Guid>();

        public ProductList()
        {
        }

        public ProductList(List serialized)
        {
            ProductIdList = serialized.ToList(StateExtensions.ToGuid);
        }

        public IValue Serialize()
        {
            return ProductIdList
                .Aggregate(
                    List.Empty,
                    (current, productId) => current.Add(productId.Serialize())
                );
        }
    }
}
