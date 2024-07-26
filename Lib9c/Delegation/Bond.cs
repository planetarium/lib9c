using System.Numerics;
using Bencodex;
using Bencodex.Types;

namespace Nekoyume.Delegation
{
    public class Bond : IBencodable
    {
        public Bond()
            : this(BigInteger.Zero, 0)
        {
        }

        public Bond(IValue bencoded)
            : this((List)bencoded)
        {
        }

        public Bond(List bencoded)
            : this((Integer)bencoded[0], (Integer)bencoded[1])
        {
        }

        private Bond(BigInteger share, long lastDistributeHeight)
        {
            Share = share;
            LastDistributeHeight = lastDistributeHeight;
        }

        public BigInteger Share { get; private set; }

        public long LastDistributeHeight { get; private set; }

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

        public IValue Bencoded => List.Empty
            .Add(Share)
            .Add(LastDistributeHeight);
    }
}
