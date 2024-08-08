using System.Collections.Generic;
using System.Collections.Immutable;
using Bencodex.Types;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Action.DPoS.Util;

namespace Nekoyume.Action.DPoS.Model
{
    public class ValidatorRewards
    {
        private readonly SortedList<long, FungibleAssetValue> _rewards;

        public ValidatorRewards(Address validatorAddress, Currency currency)
        {
            Address = DeriveAddress(validatorAddress, currency);
            ValidatorAddress = validatorAddress;
            Currency = currency;
            _rewards = new SortedList<long, FungibleAssetValue>();
        }

        public ValidatorRewards(IValue serialized)
        {
            var list = (List)serialized;
            Address = list[0].ToAddress();
            ValidatorAddress = list[1].ToAddress();
            Currency = list[2].ToCurrency();
            _rewards = new SortedList<long, FungibleAssetValue>();
            var rewardsList = (List)list[3];
            int rewardsLength = rewardsList[0].ToInteger();
            for (var i = 1; i <= rewardsLength; i++)
            {
                var reward = new Reward(rewardsList[i]);
                _rewards.Add(reward.Index, reward.Fav);
            }
        }

        public ValidatorRewards(ValidatorRewards validatorRewards)
        {
            Address = validatorRewards.Address;
            ValidatorAddress = validatorRewards.ValidatorAddress;
            Currency = validatorRewards.Currency;
            _rewards = validatorRewards._rewards;
        }

        public Address Address { get; }

        public Address ValidatorAddress { get; }

        public Currency Currency { get; }

        public ImmutableSortedDictionary<long, FungibleAssetValue> Rewards
            => _rewards.ToImmutableSortedDictionary();

        public static Address DeriveAddress(Address validatorAddress, Currency currency)
        {
            return AddressHelper.Derive(
                AddressHelper.Derive(validatorAddress, currency.Hash.ToString()),
                "ValidatorRewardsAddress");
        }

        public void Add(long blockHeight, FungibleAssetValue reward)
        {
            if (!reward.Currency.Equals(Currency))
            {
                throw new Exception.InvalidCurrencyException(Currency, reward.Currency);
            }

            _rewards.Add(blockHeight, reward);
        }

        public IValue Serialize()
        {
            List serializedRewards = List.Empty.Add(Rewards.Count.Serialize());
            foreach (
                KeyValuePair<long, FungibleAssetValue> reward in Rewards)
            {
                serializedRewards
                    = serializedRewards.Add(new Reward(reward.Key, reward.Value).Serialize());
            }

            return List.Empty
                .Add(Address.Serialize())
                .Add(ValidatorAddress.Serialize())
                .Add(Currency.Serialize())
                .Add(serializedRewards);
        }
    }

    internal class Reward
    {
        public long Index { get; }

        public FungibleAssetValue Fav { get; }

        public Reward(IValue serialized)
        {
            var list = (List)serialized;
            Index = list[0].ToLong();
            Fav = list[1].ToFungibleAssetValue();
        }

        public Reward(long index, FungibleAssetValue fav)
        {
            Index = index;
            Fav = fav;
        }

        public IValue Serialize() => List.Empty.Add(Index.Serialize()).Add(Fav.Serialize());
    }
}
