#nullable enable
using System;
using Libplanet.Crypto;
using Nekoyume.Delegation;
using Nekoyume.ValidatorDelegation;

namespace Nekoyume.Model.Guild
{
    public class GuildValidatorDelegatee
        : Delegatee<GuildValidatorRepository, GuildValidatorDelegatee, GuildValidatorDelegator>,
        IEquatable<GuildValidatorDelegatee>
    {
        public GuildValidatorDelegatee(
            Address address,
            Address delegationPoolAddress,
            GuildValidatorRepository repository)
            : base(
                  address: address,
                  accountAddress: repository.DelegateeAccountAddress,
                  delegationCurrency: ValidatorSettings.ValidatorDelegationCurrency,
                  rewardCurrency: ValidatorSettings.ValidatorRewardCurrency,
                  delegationPoolAddress: ValidatorSettings.UnbondedPoolAddress,
                  rewardPoolAddress: DelegationAddress.RewardPoolAddress(address, repository.DelegateeAccountAddress),
                  rewardRemainderPoolAddress: Addresses.CommunityPool,
                  slashedPoolAddress: Addresses.CommunityPool,
                  unbondingPeriod: ValidatorSettings.ValidatorUnbondingPeriod,
                  maxUnbondLockInEntries: ValidatorSettings.ValidatorMaxUnbondLockInEntries,
                  maxRebondGraceEntries: ValidatorSettings.ValidatorMaxRebondGraceEntries,
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
