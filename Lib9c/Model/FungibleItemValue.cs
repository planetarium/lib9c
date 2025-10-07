using System.Security.Cryptography;
using Bencodex.Types;
using Lib9c.Model.State;
using Libplanet.Common;

namespace Lib9c.Model
{
    public readonly struct FungibleItemValue
    {
        public FungibleItemValue(List bencoded)
            : this(
                new HashDigest<SHA256>(bencoded[0]),
                (Integer)bencoded[1]
            )
        {
        }

        public FungibleItemValue(HashDigest<SHA256> id, int count)
        {
            Id = id;
            Count = count;
        }

        public IValue Serialize()
        {
            return new List(Id.Serialize(), (Integer)Count);
        }

        public HashDigest<SHA256> Id { get; }
        public int Count { get; }
    }
}
