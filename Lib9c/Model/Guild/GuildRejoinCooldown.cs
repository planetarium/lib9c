#nullable enable
using System;
using Bencodex;
using Bencodex.Types;
using Nekoyume.TypedAddress;
using Nekoyume.ValidatorDelegation;

namespace Nekoyume.Model.Guild
{
    public class GuildRejoinCooldown : IBencodable, IEquatable<GuildRejoinCooldown>
    {
        public GuildRejoinCooldown(AgentAddress agentAddress, long quitHeight)
        {
            AgentAddress = agentAddress;
            ReleaseHeight = quitHeight + ValidatorDelegatee.ValidatorUnbondingPeriod;
        }

        public GuildRejoinCooldown(AgentAddress agentAddress, IValue bencoded)
            : this(agentAddress, (Integer)bencoded)
        {
        }

        public GuildRejoinCooldown(AgentAddress agentAddress, Integer bencoded)
        {
            AgentAddress = agentAddress;
            ReleaseHeight = bencoded;
        }

        public AgentAddress AgentAddress { get; }

        public long ReleaseHeight { get; }

        public Integer Bencoded => new Integer(ReleaseHeight);

        IValue IBencodable.Bencoded => Bencoded;

        public long Cooldown(long currentHeight)
        {
            return Math.Max(0, ReleaseHeight - currentHeight);
        }

        public bool Equals(GuildRejoinCooldown? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return AgentAddress.Equals(other.AgentAddress)
                && ReleaseHeight == other.ReleaseHeight;
        }
    }
}
