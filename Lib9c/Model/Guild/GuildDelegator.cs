#nullable enable
using System;
using Libplanet.Crypto;
using Nekoyume.Delegation;

namespace Nekoyume.Model.Guild
{
    public class GuildDelegator
        : Delegator<GuildDelegatee, GuildDelegator>, IEquatable<GuildDelegator>
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

        public bool Equals(GuildDelegator? other)
            => Metadata.Equals(other?.Metadata);
    }
}
