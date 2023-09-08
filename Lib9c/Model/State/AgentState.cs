using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Libplanet.Crypto;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Model.State
{
    /// <summary>
    /// Agent의 상태 모델이다.
    /// </summary>
    [Serializable]
    public class AgentState : State, ICloneable
    {
        public const int CurrentVersion = 1;

        public int Version { get; private set; }
        public readonly Dictionary<int, Address> avatarAddresses;
        public HashSet<int> unlockedOptions;
        public int MonsterCollectionRound { get; private set; }

        public AgentState(Address address) : base(address)
        {
            Version = CurrentVersion;
            avatarAddresses = new Dictionary<int, Address>();
            unlockedOptions = new HashSet<int>();
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
            unlockedOptions = serialized.ContainsKey((IKey)(Text) "unlockedOptions")
                ? serialized["unlockedOptions"].ToHashSet(StateExtensions.ToInteger)
                : new HashSet<int>();
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
            unlockedOptions = serialized[3].ToHashSet(StateExtensions.ToInteger);
            MonsterCollectionRound = serialized[4].ToInteger();
        }

        public object Clone()
        {
            return MemberwiseClone();
        }

        public void IncreaseCollectionRound()
        {
            MonsterCollectionRound++;
        }

        public override IValue Serialize()
        {
            throw new NotSupportedException();
        }

        public override IValue SerializeV2()
        {
            throw new NotSupportedException();
        }

        public override IValue SerializeList()
        {
            return new List(
                base.SerializeList(),
                (Integer)CurrentVersion,
#pragma warning disable LAA1002
                new Dictionary(
                    avatarAddresses.Select(
                        kv =>
                            new KeyValuePair<IKey, IValue>(
                                new Binary(BitConverter.GetBytes(kv.Key)),
                                kv.Value.Serialize()
                            )
                    )
                ),
                unlockedOptions.Select(i => i.Serialize()).Serialize(),
#pragma warning restore LAA1002
                MonsterCollectionRound.Serialize());
        }
    }
}
