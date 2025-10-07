using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Libplanet.Crypto;
using static Lib9c.SerializeKeys;

namespace Lib9c.Model.State
{
    /// <summary>
    /// Agent의 상태 모델이다.
    /// </summary>
    [Serializable]
    public class AgentState : State, ICloneable
    {
        public const int CurrentVersion = 1;

        public readonly Dictionary<int, Address> avatarAddresses;

        public int MonsterCollectionRound { get; private set; }

        public int Version { get; private set; }

        public AgentState(Address address) : base(address)
        {
            Version = CurrentVersion;
            avatarAddresses = new Dictionary<int, Address>();
        }

        public AgentState(Dictionary serialized)
            : base(serialized)
        {
            Version = 0;
#pragma warning disable LAA1002
            avatarAddresses = ((Dictionary)serialized["avatarAddresses"])
                .Where(kv => kv.Key is Binary)
                .ToDictionary(
                    kv => BitConverter.ToInt32(((Binary)kv.Key).ToByteArray(), 0),
                    kv => kv.Value.ToAddress()
                );
#pragma warning restore LAA1002
            MonsterCollectionRound = serialized.ContainsKey((IKey) (Text) MonsterCollectionRoundKey)
                ? serialized[MonsterCollectionRoundKey].ToInteger()
                : 0;
        }

        public AgentState(List serialized)
            : base(serialized[0])
        {
            Version = (int)((Integer)serialized[1]).Value;
#pragma warning disable LAA1002
            avatarAddresses = ((Dictionary)serialized[2])
                .Where(kv => kv.Key is Binary)
                .ToDictionary(
                    kv => BitConverter.ToInt32(((Binary)kv.Key).ToByteArray(), 0),
                    kv => kv.Value.ToAddress()
                );
#pragma warning restore LAA1002
            // serialized[3] is unused and ignored.
            MonsterCollectionRound = serialized[4].ToInteger();
        }

        public object Clone()
        {
            return new AgentState((List)SerializeList());
        }

        public void IncreaseCollectionRound()
        {
            MonsterCollectionRound++;
        }

        /// <inheritdoc cref="IState.Serialize" />
        public override IValue Serialize()
        {
            return SerializeList();
        }

        public IValue SerializeList()
        {
            return new List(
                base.SerializeListBase(),
                (Integer)CurrentVersion,
#pragma warning disable LAA1002
                new Dictionary(
                    avatarAddresses.Select(
                        kv =>
                            new KeyValuePair<IKey, IValue>(
                                new Binary(BitConverter.GetBytes(kv.Key)),
                                kv.Value.Serialize()))),
                new List(), // A placeholder list for now removed unlockedOptions property.
#pragma warning restore LAA1002
                MonsterCollectionRound.Serialize());
        }
    }
}
