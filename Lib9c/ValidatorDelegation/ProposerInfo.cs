#nullable enable
using System.Collections.Immutable;
using Bencodex.Types;
using Lib9c.Model.State;
using Libplanet.Crypto;

namespace Lib9c.ValidatorDelegation
{
    public class ProposerInfo
    {
        public ProposerInfo(long blockIndex, Address proposer)
        {
            BlockIndex = blockIndex;
            Proposer = proposer;
        }

        public ProposerInfo(IValue bencoded)
            : this((List)bencoded)
        {
        }

        public ProposerInfo(List bencoded)
            : this((Integer)bencoded[0], new Address(bencoded[1]))
        {
        }

        public static Address Address => new Address(
            ImmutableArray.Create<byte>(
                0x56, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x50));

        public long BlockIndex { get; }

        public Address Proposer { get; }

        public IValue Bencoded
            => List.Empty
                .Add(BlockIndex)
                .Add(Proposer.Serialize());
    }
}
