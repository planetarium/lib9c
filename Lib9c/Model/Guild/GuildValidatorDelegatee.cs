#nullable enable
using System;
using Libplanet.Crypto;
using Nekoyume.Delegation;
using Nekoyume.ValidatorDelegation;

namespace Nekoyume.Model.Guild
{
    public class GuildValidatorDelegatee
        : Delegatee<GuildValidatorDelegator, GuildValidatorDelegatee>, IEquatable<GuildValidatorDelegatee>
    {
        public GuildValidatorDelegatee(
            Address address,
            Address delegationPoolAddress,
            GuildValidatorRepository repository)
            : base(
                  address: address,
                  accountAddress: repository.DelegateeAccountAddress,
                  delegationCurrency: ValidatorDelegatee.ValidatorDelegationCurrency,
                  rewardCurrency: ValidatorDelegatee.ValidatorRewardCurrency,
                  delegationPoolAddress: ValidatorDelegatee.UnbondedPoolAddress,
                  rewardPoolAddress: DelegationAddress.RewardPoolAddress(address, repository.DelegateeAccountAddress),
                  rewardRemainderPoolAddress: Addresses.CommunityPool,
                  slashedPoolAddress: Addresses.CommunityPool,
                  unbondingPeriod: ValidatorDelegatee.ValidatorUnbondingPeriod,
                  maxUnbondLockInEntries: ValidatorDelegatee.ValidatorMaxUnbondLockInEntries,
                  maxRebondGraceEntries: ValidatorDelegatee.ValidatorMaxRebondGraceEntries,
                  repository: repository)
        {
        }

        public GuildValidatorDelegatee(
            Address address,
            GuildValidatorRepository repository)
            : base(
                  address: address,
                  repository: repository)
        {
        }

        public bool Equals(GuildValidatorDelegatee? other)
            => Metadata.Equals(other?.Metadata);
    }
}
