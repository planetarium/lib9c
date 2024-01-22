using System;
using Bencodex.Types;

namespace Nekoyume.Model.Collection
{
    public static class CollectionFactory
    {
        public static ICollectionMaterial DeserializeMaterial(List serialized)
        {
            if ((Integer)serialized[0] == (int)MaterialType.Fungible)
            {
                return new FungibleCollectionMaterial(serialized);
            }

            return new NonFungibleCollectionMaterial(serialized);
        }
    }
}
