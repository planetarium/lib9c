using System.Security.Cryptography;
using Bencodex;
using Bencodex.Types;
using Libplanet.Common;
using Libplanet.Store.Trie;
using Libplanet.Store.Trie.Nodes;

namespace Libplanet.Extensions.RemoteBlockChainStates
{
    internal class HollowTrie : ITrie
    {
        public HollowTrie(HashDigest<SHA256>? hash)
        {
            Hash = hash ?? HashDigest<SHA256>.DeriveFrom(new Codec().Encode(Null.Value));
        }

        public HashDigest<SHA256> Hash { get; }

        public INode? Root => throw new NotSupportedException();

        public bool Recorded => throw new NotSupportedException();

        public IEnumerable<(KeyBytes Path, IValue? TargetValue, IValue SourceValue)> Diff(ITrie other)
            => throw new NotSupportedException();

        public IValue? Get(KeyBytes key)
            => throw new NotSupportedException();


        public IReadOnlyList<IValue?> Get(IReadOnlyList<KeyBytes> keys)
            => throw new NotSupportedException();


        public INode? GetNode(Nibbles nibbles)
            => throw new NotSupportedException();


        public IEnumerable<(Nibbles Path, INode Node)> IterateNodes()
            => throw new NotSupportedException();


        public IEnumerable<(KeyBytes Path, IValue Value)> IterateValues()
            => throw new NotSupportedException();


        public ITrie Set(in KeyBytes key, IValue value)
            => throw new NotSupportedException();

        public ITrie Remove(in KeyBytes key)
            => throw new NotSupportedException();
    }
}
