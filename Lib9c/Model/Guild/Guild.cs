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
    public class Guild : Delegatee<GuildRepository, Guild, GuildParticipant>,
        IBencodable, IEquatable<Guild>
    {
        private const string StateTypeName = "guild";
        private const long StateVersion = 1;

        public readonly AgentAddress GuildMasterAddress;

        public readonly Address ValidatorAddress;

        public Guild(
            GuildAddress address,
            AgentAddress guildMasterAddress,
            Address validatorAddress,
            Currency rewardCurrency,
            GuildRepository repository)
            : base(
                  address: address,
                  accountAddress: repository.DelegateeAccountAddress,
                  delegationCurrency: Currencies.GuildGold,
                  rewardCurrency: rewardCurrency,
                  delegationPoolAddress: DelegationAddress.DelegationPoolAddress(address, repository.DelegateeAccountAddress),
                  rewardPoolAddress: DelegationAddress.RewardPoolAddress(address, repository.DelegateeAccountAddress),
                  rewardRemainderPoolAddress: Addresses.CommunityPool,
                  slashedPoolAddress: Addresses.CommunityPool,
                  unbondingPeriod: 0L,
                  maxUnbondLockInEntries: 0,
                  maxRebondGraceEntries: 0,
                  repository: repository)
        {
            ValidatorAddress = validatorAddress;
            GuildMasterAddress = guildMasterAddress;
        }

        public Guild(
            GuildAddress address,
            IValue bencoded,
            GuildRepository repository)
            : base(address: address, repository: repository)
        {
            if (bencoded is not List list)
            {
                throw new InvalidCastException();
            }

            if (list[0] is not Text text || text != StateTypeName || list[1] is not Integer integer)
            {
                throw new InvalidCastException();
            }

            if (integer > StateVersion)
            {
                throw new FailedLoadStateException("Un-deserializable state.");
            }

            GuildMasterAddress = new AgentAddress(list[2]);
            ValidatorAddress = new AgentAddress(list[3]);
        }

        public new GuildAddress Address => new GuildAddress(base.Address);

        public List Bencoded => List.Empty
            .Add(StateTypeName)
            .Add(StateVersion)
            .Add(GuildMasterAddress.Bencoded)
            .Add(ValidatorAddress.Bencoded);

        IValue IBencodable.Bencoded => Bencoded;

        public bool Equals(Guild other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Address.Equals(other.Address)
                 && GuildMasterAddress.Equals(other.GuildMasterAddress)
                 && ValidatorAddress.Equals(other.ValidatorAddress)
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
            return HashCode.Combine(Address, GuildMasterAddress, ValidatorAddress);
        }
    }
}
