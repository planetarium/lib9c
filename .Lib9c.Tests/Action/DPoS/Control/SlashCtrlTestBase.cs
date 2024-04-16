namespace Lib9c.Tests.Action.DPoS.Control
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Numerics;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume.Action.DPoS.Control;
    using Nekoyume.Action.DPoS.Misc;
    using Nekoyume.Module;

    public abstract class SlashCtrlTestBase : PoSTest
    {
        public static readonly FungibleAssetValue ZeroNCG
            = new FungibleAssetValue(Asset.GovernanceToken, 0, 0);

        public static readonly FungibleAssetValue ZeroPower
            = new FungibleAssetValue(Asset.ConsensusToken, 0, 0);

        protected static FungibleAssetValue SlashPower(
            FungibleAssetValue value,
            BigInteger factor,
            params FungibleAssetValue[] assetsToSlashFirst)
        {
            if (!value.Currency.Equals(Asset.ConsensusToken))
            {
                throw new ArgumentException(
                    message: $"'{value.Currency}' is different from '{Asset.ConsensusToken}'.",
                    paramName: nameof(value));
            }

            return SlashAsset(value, factor, assetsToSlashFirst);
        }

        protected static FungibleAssetValue SlashNCG(
            FungibleAssetValue value,
            BigInteger factor,
            params FungibleAssetValue[] assetsToSlashFirst)
        {
            if (!value.Currency.Equals(Asset.GovernanceToken))
            {
                throw new ArgumentException(
                    message: $"'{value.Currency}' is different from '{Asset.GovernanceToken}'.",
                    paramName: nameof(value));
            }

            return SlashAsset(value, factor, assetsToSlashFirst);
        }

        protected static FungibleAssetValue[] SlashPowers(FungibleAssetValue[] values, BigInteger factor)
            => values.Select(value => SlashPower(value, factor)).ToArray();

        protected static FungibleAssetValue[] SlashNCGs(FungibleAssetValue[] values, BigInteger factor)
            => values.Select(value => SlashNCG(value, factor)).ToArray();

        protected static FungibleAssetValue SumPower(IEnumerable<FungibleAssetValue> values)
            => Sum(values, Asset.ConsensusToken);

        protected static FungibleAssetValue SumNCG(IEnumerable<FungibleAssetValue> values)
            => Sum(values, Asset.GovernanceToken);

        protected static FungibleAssetValue GetPower(IWorldState worldState, Address validatorAddress)
        {
            return worldState.GetBalance(
                address: validatorAddress,
                currency: Asset.ConsensusToken);
        }

        protected static FungibleAssetValue GetNCG(IWorldState worldState, Address address)
        {
            return worldState.GetBalance(
                address: address,
                currency: Asset.GovernanceToken);
        }

        protected static FungibleAssetValue GetShare(IWorldState worldState, Address validatorAddress)
        {
            var validator = ValidatorCtrl.GetValidator(worldState, validatorAddress)!;
            return validator.DelegatorShares;
        }

        protected static FungibleAssetValue GetSlashAmount(
            FungibleAssetValue value,
            BigInteger factor,
            params FungibleAssetValue[] assetsToSlashFirst)
        {
            var total = assetsToSlashFirst.Aggregate(value, Sum);
            var (amount, remainder) = total.DivRem(factor);
            for (var i = 0; i < assetsToSlashFirst.Length; i++)
            {
                var (a, r) = assetsToSlashFirst[i].DivRem(factor);
                amount -= a;
            }

            return amount;
        }

        protected static FungibleAssetValue[] GetSlashAmounts(FungibleAssetValue[] values, BigInteger factor)
            => values.Select(value => GetSlashAmount(value, factor)).ToArray();

        protected static FungibleAssetValue NextNCG()
        {
            return new FungibleAssetValue(
                currency: Asset.GovernanceToken,
                majorUnit: Random.Shared.Next(1, 100),
                minorUnit: Random.Shared.Next(0, 100));
        }

        protected static FungibleAssetValue[] NextNCGMany(int length)
        {
            return Enumerable.Range(0, length)
                .Select(_ => NextNCG())
                .ToArray();
        }

        protected static FungibleAssetValue PowerFromNCG(FungibleAssetValue value)
            => Asset.ConsensusFromGovernance(value);

        protected static FungibleAssetValue[] PowerFromNCG(FungibleAssetValue[] values)
            => values.Select(PowerFromNCG).ToArray();

        protected static FungibleAssetValue ShareFromPower(FungibleAssetValue value)
        {
            if (!value.Currency.Equals(Asset.ConsensusToken))
            {
                throw new ArgumentException(
                        message: $"'{value}' is not {nameof(Asset.ConsensusToken)}",
                        paramName: nameof(value));
            }

            return FungibleAssetValue.FromRawValue(Asset.Share, value.RawValue);
        }

        protected static FungibleAssetValue[] ShareFromPower(FungibleAssetValue[] values)
            => values.Select(ShareFromPower).ToArray();

        private static FungibleAssetValue SlashAsset(FungibleAssetValue value, BigInteger factor, FungibleAssetValue[] assetsToSlashFirst)
        {
            var total = assetsToSlashFirst.Aggregate(value, Sum);
            var (amount, remainder) = total.DivRem(factor);
            for (var i = 0; i < assetsToSlashFirst.Length; i++)
            {
                var (a, r) = assetsToSlashFirst[i].DivRem(factor);
                amount -= a;
            }

            return value - amount;
        }

        private static FungibleAssetValue Sum(IEnumerable<FungibleAssetValue> values, Currency currency)
        {
            var item = new FungibleAssetValue(currency, 0, 0);
            foreach (var value in values)
            {
                item += value;
            }

            return item;
        }

        private static FungibleAssetValue Sum(FungibleAssetValue asset1, FungibleAssetValue asset2)
        {
            if (!asset1.Currency.Equals(asset2.Currency))
            {
                throw new ArgumentException($"'{asset1.Currency}' is different from '{asset2.Currency}'.", nameof(asset2));
            }

            return asset1 + asset2;
        }
    }
}
