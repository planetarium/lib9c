#nullable enable
using System;
using System.Collections.Generic;
using System.Numerics;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Delegation;
using Nekoyume.ValidatorDelegation;

namespace Nekoyume.Model.Guild
{
    public class GuildDelegatee
        : Delegatee<GuildRepository, GuildDelegatee, GuildDelegator>, IEquatable<GuildDelegatee>
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
            var metadata = Metadata;
            metadata.UnbondingPeriod = ValidatorDelegatee.ValidatorUnbondingPeriod;
            UpdateMetadata(metadata);
        }

        public override void Slash(BigInteger slashFactor, long infractionHeight, long height)
        {
            var totalDelegated = Metadata.TotalDelegatedFAV;
            FungibleAssetValue slashed = totalDelegated.DivRem(slashFactor, out var rem);
            if (rem.Sign > 0)
            {
                slashed += FungibleAssetValue.FromRawValue(rem.Currency, 1);
            }

            if (slashed > totalDelegated)
            {
                slashed = totalDelegated;
            }

            Metadata.RemoveDelegatedFAV(slashed);
            Repository.SetDelegatee(this);
        }

        public void Activate()
        {
            var metadata = Metadata;
            metadata.DelegationPoolAddress = ValidatorDelegatee.ActiveDelegationPoolAddress;
            UpdateMetadata(metadata);
        }

        public void Deactivate()
        {
            var metadata = Metadata;
            metadata.DelegationPoolAddress = ValidatorDelegatee.InactiveDelegationPoolAddress;
            UpdateMetadata(metadata);
        }

        public bool Equals(GuildDelegatee? other)
            => Metadata.Equals(other?.Metadata);
    }
}
