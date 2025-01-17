#nullable enable
using System;
using System.Collections.Generic;
using System.Numerics;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Delegation;
using Nekoyume.Module;
using Nekoyume.TableData.Stake;
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
                  unbondingPeriod: repository.World.TryGetSheet<StakePolicySheet>(out var sheet)
                    ? sheet.LockupIntervalValue
                    : ValidatorDelegatee.DefaultUnbondingPeriod,
                  maxUnbondLockInEntries: GuildMaxUnbondLockInEntries,
                  maxRebondGraceEntries: GuildMaxRebondGraceEntries,
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
            Metadata.UnbondingPeriod = repository.World.TryGetSheet<StakePolicySheet>(out var sheet)
                ? sheet.LockupIntervalValue
                : ValidatorDelegatee.DefaultUnbondingPeriod;
            Metadata.MaxUnbondLockInEntries = GuildMaxUnbondLockInEntries;
            Metadata.MaxRebondGraceEntries = GuildMaxRebondGraceEntries;
        }

        public static int GuildMaxUnbondLockInEntries => 1;

        public static int GuildMaxRebondGraceEntries => 1;

        public override void Slash(BigInteger slashFactor, long infractionHeight, long height)
        {
            FungibleAssetValue slashed = TotalDelegated.DivRem(slashFactor, out var rem);
            if (rem.Sign > 0)
            {
                slashed += FungibleAssetValue.FromRawValue(rem.Currency, 1);
            }

            if (slashed > Metadata.TotalDelegatedFAV)
            {
                slashed = Metadata.TotalDelegatedFAV;
            }

            Metadata.RemoveDelegatedFAV(slashed);
            Repository.SetDelegateeMetadata(Metadata);
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
