#nullable enable
using System;
using System.Numerics;
using Lib9c;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Action;
using Nekoyume.Delegation;
using Nekoyume.Model.Stake;
using Nekoyume.Module;
using Nekoyume.TypedAddress;
using Nekoyume.ValidatorDelegation;

namespace Nekoyume.Model.Guild
{
    public class GuildDelegator
        : Delegator<GuildRepository, GuildDelegatee, GuildDelegator>, IEquatable<GuildDelegator>
    {
        public GuildDelegator(
            Address address,
            Address delegationPoolAddress,
            GuildRepository repository)
            : base(
                  address: address,
                  accountAddress: repository.DelegatorAccountAddress,
                  delegationPoolAddress: delegationPoolAddress,
                  rewardAddress: address,
                  repository: repository)
        {
            UnbondingReleased += OnUnbondingReleased;
        }

        public GuildDelegator(
            Address address,
            GuildRepository repository)
            : base(address: address, repository: repository)
        {
            UnbondingReleased += OnUnbondingReleased;
        }

        public void OnUnbondingReleased(object? sender, (long Height, IUnbonding ReleasedUnbonding, FungibleAssetValue? ReleasedFAV) e)
        {
            if (e.ReleasedUnbonding is UnbondLockIn unbondLockIn)
            {
                if (IsValidator(unbondLockIn.DelegatorAddress))
                {
                    return;
                }

                Unstake(unbondLockIn, e.ReleasedFAV);
            }
        }

        private void Unstake(UnbondLockIn unbondLockIn, FungibleAssetValue? releasedFAV)
        {
            if (releasedFAV is not FungibleAssetValue gg
                || !gg.Currency.Equals(Currencies.GuildGold)
                || gg.Sign < 1)
            {
                return;
            }

            var agentAddress = new AgentAddress(unbondLockIn.DelegatorAddress);
            var repository = (GuildRepository)Repository;
            var goldCurrency = repository.World.GetGoldCurrency();
            var stakeStateAddress = StakeState.DeriveAddress(agentAddress);
            var (ncg, _) = ConvertCurrency(gg, goldCurrency);
            repository.TransferAsset(
                stakeStateAddress, agentAddress, ncg);
            repository.UpdateWorld(
                repository.World.BurnAsset(repository.ActionContext, stakeStateAddress, gg));

            Repository.UpdateWorld(repository.World);
        }

        private bool IsValidator(Address address)
        {
            var repository = new ValidatorRepository(Repository);
            try
            {
                repository.GetValidatorDelegatee(address);
                return true;
            }
            catch (FailedLoadStateException)
            {
                return false;
            }
        }

        private static (FungibleAssetValue TargetFAV, FungibleAssetValue Remainder)
            ConvertCurrency(FungibleAssetValue sourceFAV, Currency targetCurrency)
        {
            var sourceCurrency = sourceFAV.Currency;
            if (targetCurrency.DecimalPlaces < sourceCurrency.DecimalPlaces)
            {
                var d = BigInteger.Pow(10, sourceCurrency.DecimalPlaces - targetCurrency.DecimalPlaces);
                var value = FungibleAssetValue.FromRawValue(targetCurrency, sourceFAV.RawValue / d);
                var fav2 = FungibleAssetValue.FromRawValue(sourceCurrency, value.RawValue * d);
                return (value, sourceFAV - fav2);
            }
            else
            {
                var d = BigInteger.Pow(10, targetCurrency.DecimalPlaces - sourceCurrency.DecimalPlaces);
                var value = FungibleAssetValue.FromRawValue(targetCurrency, sourceFAV.RawValue * d);
                return (value, targetCurrency * 0);
            }
        }

        public bool Equals(GuildDelegator? other)
            => Metadata.Equals(other?.Metadata);
    }
}
