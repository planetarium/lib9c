using System.Numerics;
using Bencodex;
using Bencodex.Types;
using Libplanet.Crypto;

namespace Nekoyume.Delegation
{
    public class Bond : IBencodable
    {
        public Bond(Address address)
            : this(address, BigInteger.Zero, 0)
        {
        }

        public Bond(Address address, IValue bencoded)
            : this(address, (List)bencoded)
        {
        }

        public Bond(Address address, List bencoded)
            : this(address, (Integer)bencoded[0], (Integer)bencoded[1])
        {
        }

        private Bond(Address address, BigInteger share, long lastDistributeHeight)
        {
            Address = address;
            Share = share;
            LastDistributeHeight = lastDistributeHeight;
        }

        public Address Address { get; }

        public BigInteger Share { get; private set; }

        public long LastDistributeHeight { get; private set; }

        public IValue Bencoded => List.Empty
            .Add(Share)
            .Add(LastDistributeHeight);

        public void AddShare(BigInteger share)
        {
            Share += share;
        }

        public void SubtractShare(BigInteger share)
        {
            Share -= share;
        }

        public void UpdateLastDistributeHeight(long height)
        {
            LastDistributeHeight = height;
        }
    }
}
