using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using MessagePack;
using Nekoyume.Model.State;

namespace Nekoyume.Action
{
    [Serializable]
    [MessagePackObject]
    [Union(0, typeof(CreateAvatar))]
    [Union(1, typeof(HackAndSlash))]
    public abstract class GameAction : ActionBase
    {
        [Key(0)]
        public Guid Id { get; private set; }

        [IgnoreMember]
        public override IValue PlainValue =>
#pragma warning disable LAA1002
            new Bencodex.Types.Dictionary(
                PlainValueInternal
                    .SetItem("id", Id.Serialize())
                    .Select(kv => new KeyValuePair<IKey, IValue>((Text) kv.Key, kv.Value))
            );
#pragma warning restore LAA1002
        [IgnoreMember]
        protected abstract IImmutableDictionary<string, IValue> PlainValueInternal { get; }

        protected GameAction()
        {
            Id = Guid.NewGuid();
        }

        [SerializationConstructor]
        protected GameAction(Guid guid)
        {
            Id = guid;
        }

        public override void LoadPlainValue(IValue plainValue)
        {
#pragma warning disable LAA1002
            var dict = ((Bencodex.Types.Dictionary) plainValue)
                .Select(kv => new KeyValuePair<string, IValue>((Text) kv.Key, kv.Value))
                .ToImmutableDictionary();
#pragma warning restore LAA1002
            Id = dict["id"].ToGuid();
            LoadPlainValueInternal(dict);
        }

        protected abstract void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue);
    }
}
