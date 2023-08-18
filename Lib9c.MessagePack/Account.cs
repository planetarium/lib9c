using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using Bencodex;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Common;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Libplanet.Types.Blocks;
using Libplanet.Types.Consensus;
using Nekoyume.Model.State;

namespace Lib9c.Formatters
{
    public struct Account : IAccount
    {
        private IImmutableDictionary<Address, IValue> _states;
        private IImmutableDictionary<(Address, Currency), BigInteger> _balances;
        private IImmutableDictionary<Currency, BigInteger> _totalSupplies;
        private MockAccountDelta _delta;

        public IImmutableSet<(Address, Currency)> TotalUpdatedFungibleAssets =>
            ImmutableHashSet<(Address, Currency)>.Empty;

        public Account(
            IImmutableDictionary<Address, IValue> states,
            IImmutableDictionary<(Address, Currency), BigInteger> balances,
            IImmutableDictionary<Currency, BigInteger> totalSupplies
        )
        {
            _delta = new MockAccountDelta(states, balances, totalSupplies);
            _states = states;
            _balances = balances;
            _totalSupplies = totalSupplies;
        }

        public Account(Dictionary states, List balances, Dictionary totalSupplies)
            : this(
                states.ToImmutableDictionary(
                    kv => new Address(kv.Key),
                    kv => kv.Value),
                balances.Cast<Dictionary>().ToImmutableDictionary(
                    record => (new Address(((Binary)record["address"]).ByteArray), new Currency((Dictionary)record["currency"])),
                    record => (BigInteger)(Integer)record["amount"]),
                totalSupplies.ToImmutableDictionary(
                    kv => new Currency(new Codec().Decode((Binary)kv.Key)),
                    kv => (BigInteger)(Integer)kv.Value))
        {
        }

        public Account(IValue serialized)
            : this(
                (Dictionary)((Dictionary)serialized)["states"],
                (List)((Dictionary)serialized)["balances"],
                (Dictionary)((Dictionary)serialized)["totalSupplies"]
            )
        {
        }

        public Account(byte[] bytes)
            : this((Dictionary)new Codec().Decode(bytes))
        {
        }

        public IValue Serialize()
        {
            return Dictionary.Empty
                .Add(
                "states",
                new Dictionary(_states.Select(state => new KeyValuePair<IKey, IValue>(
                    (Binary)state.Key.ToByteArray(),
                    state.Value))))
                .Add(
                "balances",
                new List(_balances.Select(balance => new Dictionary(
                    new[]
                    {
                        new KeyValuePair<IKey, IValue>((Text)"address", (Binary)balance.Key.Item1.ByteArray),
                        new KeyValuePair<IKey, IValue>((Text)"currency", balance.Key.Item2.Serialize()),
                        new KeyValuePair<IKey, IValue>((Text)"amount", (Integer)balance.Value)
                    }
                    ))))
                .Add(
                "totalSupplies",
                new Dictionary(_totalSupplies.Select(supply => new KeyValuePair<IKey, IValue>(
                    (Binary)new Codec().Encode(supply.Key.Serialize()),
                    (Integer)supply.Value))));
        }

        public IAccountDelta Delta => _delta;

        public Address Address => ReservedAddresses.LegacyAccount;

        public BlockHash? BlockHash => null;

        public HashDigest<SHA256>? StateRootHash => null;

        public IValue? GetState(Address address) =>
            _states.ContainsKey(address)
                ? _states[address]
                : null;

        public IReadOnlyList<IValue?> GetStates(IReadOnlyList<Address> addresses) =>
            addresses.Select(_states.GetValueOrDefault).ToArray();

        public IAccount SetState(Address address, IValue state) =>
            new Account(_states.SetItem(address, state), _balances, _totalSupplies);

        public FungibleAssetValue GetBalance(Address address, Currency currency)
        {
            if (!_balances.TryGetValue((address, currency), out BigInteger rawValue))
            {
                return currency * 0;
            }

            return FungibleAssetValue.FromRawValue(currency, rawValue);
        }

        public FungibleAssetValue GetTotalSupply(Currency currency)
        {
            if (!currency.TotalSupplyTrackable)
            {
                var msg =
                    $"The total supply value of the currency {currency} is not trackable"
                    + " because it is a legacy untracked currency which might have been"
                    + " established before the introduction of total supply tracking support.";
                throw new TotalSupplyNotTrackableException(msg, currency);
            }

            // Return dirty state if it exists.
            if (_totalSupplies.TryGetValue(currency, out var totalSupplyValue))
            {
                return FungibleAssetValue.FromRawValue(currency, totalSupplyValue);
            }

            return currency * 0;
        }

        public IAccount MintAsset(IActionContext context, Address recipient, FungibleAssetValue value)
        {
            // FIXME: 트랜잭션 서명자를 알아내 currency.AllowsToMint() 확인해서 CurrencyPermissionException
            // 던지는 처리를 해야하는데 여기서 트랜잭션 서명자를 무슨 수로 가져올지 잘 모르겠음.

            var currency = value.Currency;

            if (value <= currency * 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            var nextAmount = GetBalance(recipient, value.Currency) + value;

            if (currency.TotalSupplyTrackable)
            {
                var currentTotalSupply = GetTotalSupply(currency);
                if (currency.MaximumSupply < currentTotalSupply + value)
                {
                    var msg = $"The amount {value} attempted to be minted added to the current"
                                + $" total supply of {currentTotalSupply} exceeds the"
                                + $" maximum allowed supply of {currency.MaximumSupply}.";
                    throw new SupplyOverflowException(msg, value);
                }

                return new Account(
                    _states,
                    _balances.SetItem(
                        (recipient, value.Currency),
                        nextAmount.RawValue
                    ),
                    _totalSupplies.SetItem(currency, (currentTotalSupply + value).RawValue)
                );
            }

            return new Account(
                _states,
                _balances.SetItem(
                    (recipient, value.Currency),
                    nextAmount.RawValue
                ),
                _totalSupplies
            );
        }

        public IAccount TransferAsset(
            IActionContext context,
            Address sender,
            Address recipient,
            FungibleAssetValue value,
            bool allowNegativeBalance = false)
        {
            if (value.Sign <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            FungibleAssetValue senderBalance = GetBalance(sender, value.Currency);
            if (senderBalance < value)
            {
                throw new InsufficientBalanceException(
                    $"There is no sufficient balance for {sender}: {senderBalance} < {value}",
                    sender,
                    senderBalance
                );
            }

            Currency currency = value.Currency;
            FungibleAssetValue senderRemains = senderBalance - value;
            FungibleAssetValue recipientRemains = GetBalance(recipient, currency) + value;
            var balances = _balances
                .SetItem((sender, currency), senderRemains.RawValue)
                .SetItem((recipient, currency), recipientRemains.RawValue);
            return new Account(_states, balances, _totalSupplies);
        }

        public IAccount BurnAsset(IActionContext context, Address owner, FungibleAssetValue value)
        {
            // FIXME: 트랜잭션 서명자를 알아내 currency.AllowsToMint() 확인해서 CurrencyPermissionException
            // 던지는 처리를 해야하는데 여기서 트랜잭션 서명자를 무슨 수로 가져올지 잘 모르겠음.

            var currency = value.Currency;

            if (value <= currency * 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            FungibleAssetValue balance = GetBalance(owner, currency);
            if (balance < value)
            {
                throw new InsufficientBalanceException(
                    $"There is no sufficient balance for {owner}: {balance} < {value}",
                    owner,
                    value
                );
            }

            FungibleAssetValue nextValue = balance - value;
            return new Account(
                _states,
                _balances.SetItem(
                    (owner, currency),
                    nextValue.RawValue
                ),
                currency.TotalSupplyTrackable
                    ? _totalSupplies.SetItem(
                        currency,
                        (GetTotalSupply(currency) - value).RawValue)
                    : _totalSupplies
            );
        }

        public IAccount SetValidator(Validator validator)
        {
            return new Account();
        }

        public ValidatorSet GetValidatorSet()
        {
            return new ValidatorSet();
        }

        private class MockAccountDelta : IAccountDelta
        {
            private IImmutableDictionary<Address, IValue> _states;
            private IImmutableDictionary<(Address, Currency), BigInteger> _fungibles;
            private IImmutableDictionary<Currency, BigInteger> _totalSupplies;

            public MockAccountDelta(
                IImmutableDictionary<Address, IValue> states,
                IImmutableDictionary<(Address, Currency), BigInteger> balances,
                IImmutableDictionary<Currency, BigInteger> totalSupplies)
            {
                _states = states;
                _fungibles = balances;
                _totalSupplies = totalSupplies;
            }

            public IImmutableSet<Address> UpdatedAddresses => StateUpdatedAddresses.Union(FungibleUpdatedAddresses);
            public IImmutableSet<Address> StateUpdatedAddresses => _states.Keys.ToImmutableHashSet();
            public IImmutableDictionary<Address, IValue> States => _states;
            public IImmutableSet<Address> FungibleUpdatedAddresses => _fungibles.Keys.Select(pair => pair.Item1).ToImmutableHashSet();
            public IImmutableSet<(Address, Currency)> UpdatedFungibleAssets => _fungibles.Keys.ToImmutableHashSet();
            public IImmutableDictionary<(Address, Currency), BigInteger> Fungibles => _fungibles;
            public IImmutableSet<Currency> UpdatedTotalSupplyCurrencies => _totalSupplies.Keys.ToImmutableHashSet();
            public IImmutableDictionary<Currency, BigInteger> TotalSupplies => _totalSupplies;
            public ValidatorSet? ValidatorSet => null;
        }
    }
}
