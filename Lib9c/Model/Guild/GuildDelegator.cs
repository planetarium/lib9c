#nullable enable
using System;
using Lib9c;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Delegation;
using Nekoyume.Model.Stake;
using Nekoyume.Module;
using Nekoyume.Module.Guild;
using Nekoyume.TypedAddress;

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
                if (Repository.GetJoinedGuild(new AgentAddress(unbondLockIn.DelegatorAddress)) is null)
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
            var goldCurrency = Repository.World.GetGoldCurrency();
            var stakeStateAddress = StakeState.DeriveAddress(agentAddress);
            var (ncg, _) = GuildModule.ConvertCurrency(gg, goldCurrency);
            Repository.TransferAsset(
                stakeStateAddress, agentAddress, ncg);
            Repository.UpdateWorld(
                Repository.World.BurnAsset(Repository.ActionContext, stakeStateAddress, gg));

            var balanceGap = Repository.World.GetBalance(stakeStateAddress, goldCurrency)
                - (Repository.World.GetStaked(agentAddress) + FungibleAssetValue.FromRawValue(goldCurrency, 1));
            if (balanceGap.Sign > 0)
            {
                Repository.TransferAsset(stakeStateAddress, Addresses.CommunityPool, balanceGap);
            }
        }

        public bool Equals(GuildDelegator? other)
            => Metadata.Equals(other?.Metadata);
    }
}
