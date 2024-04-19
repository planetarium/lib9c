using Bencodex.Types;
using Libplanet.Crypto;
using Nekoyume.Action.DPoS.Util;

namespace Nekoyume.Action.DPoS.Model
{
    public class ProposerInfo
    {
        public ProposerInfo(long blockIndex, Address proposer)
        {
            BlockIndex = blockIndex;
            Proposer = proposer;
        }

        public ProposerInfo(IValue bencoded)
        {
            var dict = (Dictionary)bencoded;
            BlockIndex = (Integer)dict["index"];
            Proposer = dict["proposer"].ToAddress();
        }

        public long BlockIndex { get; }

        public Address Proposer { get; }

        public IValue Bencoded =>
            Dictionary.Empty
                .Add((Text)"index", BlockIndex)
                .Add((Text)"proposer", Proposer.Bencoded);
    }
}
