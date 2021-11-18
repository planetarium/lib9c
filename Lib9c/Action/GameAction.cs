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
    [Union(2, typeof(AddActivatedAccount))]
    [Union(3, typeof(Buy))]
    [Union(4, typeof(ChargeActionPoint))]
    [Union(5, typeof(ClaimMonsterCollectionReward))]
    [Union(6, typeof(CombinationConsumable))]
    [Union(7, typeof(CombinationEquipment))]
    [Union(8, typeof(DailyReward))]
    [Union(9, typeof(InitializeStates))]
    [Union(10, typeof(ItemEnhancement))]
    [Union(11, typeof(MigrationActivatedAccountsState))]
    [Union(12, typeof(MigrationAvatarState))]
    [Union(13, typeof(MigrationLegacyShop))]
    [Union(14, typeof(MimisbrunnrBattle))]
    [Union(15, typeof(MonsterCollect))]
    [Union(16, typeof(PatchTableSheet))]
    [Union(17, typeof(RankingBattle))]
    [Union(18, typeof(RapidCombination))]
    [Union(19, typeof(RedeemCode))]
    [Union(20, typeof(Sell))]
    [Union(21, typeof(SellCancellation))]
    [Union(22, typeof(UpdateSell))]
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
