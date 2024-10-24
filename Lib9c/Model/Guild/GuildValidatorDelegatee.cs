#nullable enable
using Lib9c;
using Libplanet.Crypto;
using System;
using Nekoyume.Delegation;

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
                  delegationCurrency: Currencies.GuildGold,
                  rewardCurrency: Currencies.Mead,
                  delegationPoolAddress: address,
                  rewardRemainderPoolAddress: Addresses.CommunityPool,
                  slashedPoolAddress: Addresses.CommunityPool,
                  unbondingPeriod: 75600L,
                  maxUnbondLockInEntries: 10,
                  maxRebondGraceEntries: 10,
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
