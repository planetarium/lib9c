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
            AgentAddress guildMasterAddress, Currency rewardCurrency)
            : this(guildMasterAddress, rewardCurrency, null)
        {
        }

        public Guild(List list, Currency rewardCurrency)
            : this(list, rewardCurrency, null)
        {
        }

        public Guild(
            AgentAddress guildMasterAddress, Currency rewardCurrency, IDelegationRepository repository)
            : base(guildMasterAddress, repository)
        {
            GuildMasterAddress = guildMasterAddress;
            RewardCurrency = rewardCurrency;
        }

        public Guild(List list, Currency rewardCurrency, IDelegationRepository repository)
            : base(new Address(list[2]), list[3], repository)
        {
            GuildMasterAddress = new AgentAddress(list[2]);
            RewardCurrency = rewardCurrency;

            if (list[0] is not Text text || text != StateTypeName || list[1] is not Integer integer)
            {
                throw new InvalidCastException();
            }

            if (integer > StateVersion)
            {
                throw new FailedLoadStateException("Un-deserializable state.");
            }
        }

        public override Currency Currency => Currencies.GuildGold;

        public override Currency RewardCurrency { get; }

        public override Address DelegationPoolAddress => DeriveAddress(PoolId);

        public override long UnbondingPeriod => 75600L;

        public override byte[] DelegateeId => new byte[] { 0x047 }; // `G`

        public override int MaxUnbondLockInEntries => 10;

        public override int MaxRebondGraceEntries => 10;

        public new List Bencoded => List.Empty
            .Add(StateTypeName)
            .Add(StateVersion)
            .Add(GuildMasterAddress.Bencoded)
            .Add(base.Bencoded);

        IValue IBencodable.Bencoded => Bencoded;

        public bool Equals(Guild other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return GuildMasterAddress.Equals(other.GuildMasterAddress)
                && base.Equals(other);
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
