#nullable enable
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Libplanet.Crypto;

namespace Nekoyume.ValidatorDelegation
{
    public sealed class AbstainHistory
    {
        private static readonly Comparer<PublicKey> Comparer
            = Comparer<PublicKey>.Create((x, y) => x.Address.CompareTo(y.Address));

        public AbstainHistory()
        {
            History = new SortedDictionary<PublicKey, List<long>>(Comparer);
        }

        public AbstainHistory(IValue bencoded)
            : this((List)bencoded)
        {
        }

        public AbstainHistory(List bencoded)
        {
            History = new SortedDictionary<PublicKey, List<long>>(Comparer);
            foreach (var item in bencoded)
            {
                var list = (List)item;
                var publicKey = new PublicKey(((Binary)list[0]).ToArray());
                var history = new List<long>();
                foreach (var height in (List)list[1])
                {
                    history.Add((Integer)height);
                }
                History.Add(publicKey, history);
            }
        }

        public SortedDictionary<PublicKey, List<long>> History { get; private set; }

        public static int WindowSize => 10;

        public static int MaxAbstainAllowance => 3;

        public static Address Address => new Address(
            ImmutableArray.Create<byte>(
                0x44, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x41));

        public IValue Bencoded
            => new List(
                History.Select(pair =>
                    List.Empty
                        .Add(pair.Key.Format(true))
                        .Add(new List(pair.Value))));


        public List<PublicKey> FindToSlashAndAdd(IEnumerable<PublicKey> abstainList, long height)
        {
            var lowerBound = height - WindowSize;
            var toSlashList = new List<PublicKey>();
            foreach (var abstain in abstainList)
            {
                if (History.TryGetValue(abstain, out var history))
                {
                    history.Add(height);
                    if (history.Count(abstainHeight => abstainHeight > lowerBound) > MaxAbstainAllowance)
                    {
                        toSlashList.Add(abstain);
                        History.Remove(abstain);
                    }
                }
                else
                {
                    History.Add(abstain, new List<long>() { height });
                }
            }

            foreach (var history in History.ToArray())
            {
                history.Value.RemoveAll(abstainHeight => abstainHeight < lowerBound);
                if (history.Value.Count == 0)
                {
                    History.Remove(history.Key);
                }
            }

            return toSlashList;
        }
    }
}
