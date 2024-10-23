#nullable enable
using Libplanet.Crypto;
using Nekoyume.Delegation;
using Nekoyume.ValidatorDelegation;
using System;

namespace Nekoyume.Model.Guild
{
    public class GuildValidatorDelegator
        : Delegator<GuildValidatorDelegatee, GuildValidatorDelegator>, IEquatable<GuildValidatorDelegator>
    {
        public GuildValidatorDelegator(
            Address address,
            Address delegationPoolAddress,
            GuildValidatorRepository repository)
            : base(
                  address: address,
                  accountAddress: repository.DelegatorAccountAddress,
                  delegationPoolAddress: delegationPoolAddress,
                  rewardAddress: address,
                  repository: repository)
        {
        }

        public GuildValidatorDelegator(
            Address address,
            GuildValidatorRepository repository)
            : base(
                  address: address,
                  repository: repository)
        {
        }

        public bool Equals(GuildValidatorDelegator? other)
            => Metadata.Equals(other?.Metadata);
    }
}
