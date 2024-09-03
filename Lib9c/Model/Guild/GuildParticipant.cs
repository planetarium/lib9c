using System;
using Bencodex;
using Bencodex.Types;
using Libplanet.Crypto;
using Nekoyume.Action;
using Nekoyume.Delegation;
using Nekoyume.Model.State;
using Nekoyume.TypedAddress;

namespace Nekoyume.Model.Guild
{
    public class GuildParticipant : Delegator<Guild, GuildParticipant>, IBencodable, IEquatable<GuildParticipant>
    {
        private const string StateTypeName = "guild_participant";
        private const long StateVersion = 1;

        public readonly GuildAddress GuildAddress;

        public GuildParticipant(
            Address address,
            GuildAddress guildAddress,
            GuildRepository repository)
            : base(
                  address: address,
                  accountAddress: Addresses.GuildParticipant,
                  delegationPoolAddress: StakeState.DeriveAddress(address),
                  repository: repository)
        {
            GuildAddress = guildAddress;
        }

        public GuildParticipant(
            Address address,
            IValue bencoded,
            GuildRepository repository)
            : this(address, (List)bencoded, repository)
        {
        }

        public GuildParticipant(
            Address address,
            List bencoded,
            GuildRepository repository)
            : base(
                  address: address,
                  repository: repository)
        {
            GuildAddress = new GuildAddress(bencoded[2]);

            if (bencoded[0] is not Text text || text != StateTypeName || bencoded[1] is not Integer integer)
            {
                throw new InvalidCastException();
            }

            if (integer > StateVersion)
            {
                throw new FailedLoadStateException("Un-deserializable state.");
            }
        }

        public List Bencoded => List.Empty
            .Add(StateTypeName)
            .Add(StateVersion)
            .Add(GuildAddress.Bencoded);

        IValue IBencodable.Bencoded => Bencoded;

        public bool Equals(GuildParticipant other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return GuildAddress.Equals(other.GuildAddress)
                && Metadata.Equals(other.Metadata);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((GuildParticipant)obj);
        }

        public override int GetHashCode()
        {
            return GuildAddress.GetHashCode();
        }
    }
}
