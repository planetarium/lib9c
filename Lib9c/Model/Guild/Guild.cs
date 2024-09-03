using System;
using Bencodex;
using Bencodex.Types;
using Lib9c;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Action;
using Nekoyume.Delegation;
using Nekoyume.TypedAddress;

namespace Nekoyume.Model.Guild
{
    public class Guild : Delegatee<GuildParticipant, Guild>, IEquatable<Guild>, IBencodable
    {
        private const string StateTypeName = "guild";
        private const long StateVersion = 1;

        public readonly AgentAddress GuildMasterAddress;

        public Guild(
            Address address,
            AgentAddress guildMasterAddress,
            Currency rewardCurrency,
            GuildRepository repository)
            : base(
                  address: address,
                  accountAddress: Addresses.Guild,
                  delegationCurrency: Currencies.GuildGold,
                  rewardCurrency: rewardCurrency,
                  delegationPoolAddress: address,
                  unbondingPeriod: 75600L,
                  maxUnbondLockInEntries: 10,
                  maxRebondGraceEntries: 10,
                  repository: repository)
        {
            GuildMasterAddress = guildMasterAddress;
        }

        public Guild(
            Address address,
            IValue bencoded, GuildRepository repository)
            : this(address: address, bencoded: (List)bencoded, repository: repository)
        {
        }

        public Guild(
            Address address, List bencoded, GuildRepository repository)
            : base(
                  address: address,
                  repository: repository)
        {
            GuildMasterAddress = new AgentAddress(bencoded[2]);

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
            .Add(GuildMasterAddress.Bencoded);

        IValue IBencodable.Bencoded => Bencoded;

        public bool Equals(Guild other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return GuildMasterAddress.Equals(other.GuildMasterAddress)
                && Metadata.Equals(other.Metadata);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Guild)obj);
        }

        public override int GetHashCode()
        {
            return GuildMasterAddress.GetHashCode();
        }
    }
}
