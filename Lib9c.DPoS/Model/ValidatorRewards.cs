using System.Collections.Immutable;
using Bencodex.Types;
using Lib9c.DPoS.Exception;
using Lib9c.DPoS.Util;
using Libplanet.Crypto;
using Libplanet.Types.Assets;

namespace Lib9c.DPoS.Model
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
            var dict = (Dictionary)serialized;
            Address = dict["addr"].ToAddress();
            ValidatorAddress = dict["val_addr"].ToAddress();
            Currency = dict["currency"].ToCurrency();
            _rewards = new SortedList<long, FungibleAssetValue>();
            foreach (
                KeyValuePair<IKey, IValue> kv
                in (Dictionary)dict["rewards"])
            {
                _rewards.Add(kv.Key.ToLong(), kv.Value.ToFungibleAssetValue());
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
            return validatorAddress.Derive("ValidatorRewardsAddress").Derive(currency.Ticker);
        }

        public void Add(long blockHeight, FungibleAssetValue reward)
        {
            if (!reward.Currency.Equals(Currency))
            {
                throw new InvalidCurrencyException(Currency, reward.Currency);
            }

            _rewards.Add(blockHeight, reward);
        }

        public IValue Serialize()
        {
            Dictionary serializedRewards = Dictionary.Empty;
            foreach (
                KeyValuePair<long, FungibleAssetValue> rewards in Rewards)
            {
                serializedRewards
                    = (Dictionary)serializedRewards
                    .Add((IKey)rewards.Key.Serialize(), rewards.Value.Serialize());
            }

            return Dictionary.Empty
                .Add("addr", Address.Serialize())
                .Add("val_addr", ValidatorAddress.Serialize())
                .Add("currency", Currency.Serialize())
                .Add("rewards", serializedRewards);
        }
    }
}
