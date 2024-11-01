#nullable enable
using System;
using Libplanet.Crypto;
using Nekoyume.Delegation;

namespace Nekoyume.Model.Guild
{
    public class GuildValidatorDelegator
        : Delegator<GuildValidatorRepository, GuildValidatorDelegatee, GuildValidatorDelegator>,
        IEquatable<GuildValidatorDelegator>
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
