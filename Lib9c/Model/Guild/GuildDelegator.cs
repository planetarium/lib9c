#nullable enable
using System;
using Lib9c;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Action;
using Nekoyume.Delegation;
using Nekoyume.Model.Stake;
using Nekoyume.Module;
using Nekoyume.Module.Guild;
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
        }

        public GuildDelegator(
            Address address,
            GuildRepository repository)
            : base(address: address, repository: repository)
        {
        }

        protected override void OnUnbondingReleased(long height, IUnbonding releasedUnbonding, FungibleAssetValue? releasedFAV)
        {
            if (releasedUnbonding is UnbondLockIn unbondLockIn)
            {
                if (IsValidator(unbondLockIn.DelegatorAddress))
                {
                    return;
                }

                Unstake(unbondLockIn, releasedFAV);
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
            var repository = Repository;
            var goldCurrency = repository.World.GetGoldCurrency();
            var stakeStateAddress = StakeState.DeriveAddress(agentAddress);
            var (ncg, _) = GuildModule.ConvertCurrency(gg, goldCurrency);
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
                repository.GetDelegatee(address);
                return true;
            }
            catch (FailedLoadStateException)
            {
                return false;
            }
        }

        public bool Equals(GuildDelegator? other)
            => Metadata.Equals(other?.Metadata);
    }
}
