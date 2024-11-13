#nullable enable
using System;
using System.Collections.Generic;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Delegation;
using Nekoyume.ValidatorDelegation;

namespace Nekoyume.Model.Guild
{
    public class GuildDelegatee
        : Delegatee<GuildDelegator, GuildDelegatee>, IEquatable<GuildDelegatee>
    {
        public GuildDelegatee(
            Address address,
            IEnumerable<Currency> rewardCurrencies,
            GuildRepository repository)
            : base(
                  address: address,
                  accountAddress: repository.DelegateeAccountAddress,
                  delegationCurrency: ValidatorDelegatee.ValidatorDelegationCurrency,
                  rewardCurrencies: rewardCurrencies,
                  delegationPoolAddress: ValidatorDelegatee.InactiveDelegationPoolAddress,
                  rewardPoolAddress: DelegationAddress.RewardPoolAddress(address, repository.DelegateeAccountAddress),
                  rewardRemainderPoolAddress: Addresses.CommunityPool,
                  slashedPoolAddress: Addresses.CommunityPool,
                  unbondingPeriod: ValidatorDelegatee.ValidatorUnbondingPeriod,
                  maxUnbondLockInEntries: ValidatorDelegatee.ValidatorMaxUnbondLockInEntries,
                  maxRebondGraceEntries: ValidatorDelegatee.ValidatorMaxRebondGraceEntries,
                  repository: repository)
        {
        }

        public GuildDelegatee(
            Address address,
            GuildRepository repository)
            : base(
                  address: address,
                  repository: repository)
        {
        }

        public void Activate()
        {
            Metadata.DelegationPoolAddress = ValidatorDelegatee.ActiveDelegationPoolAddress;
        }

        public void Deactivate()
        {
            Metadata.DelegationPoolAddress = ValidatorDelegatee.InactiveDelegationPoolAddress;
        }

        public bool Equals(GuildDelegatee? other)
            => Metadata.Equals(other?.Metadata);
    }
}
