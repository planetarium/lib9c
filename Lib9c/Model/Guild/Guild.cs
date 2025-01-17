#nullable enable
using System;
using Bencodex;
using Bencodex.Types;
using Libplanet.Crypto;
using Nekoyume.Action;
using Nekoyume.TypedAddress;
using Nekoyume.ValidatorDelegation;

namespace Nekoyume.Model.Guild
{
    public class Guild : IBencodable, IEquatable<Guild>
    {
        private const string StateTypeName = "guild";
        private const long StateVersion = 2;

        public readonly AgentAddress GuildMasterAddress;

        public readonly Address ValidatorAddress;

        public Guild(
            GuildAddress address,
            AgentAddress guildMasterAddress,
            Address validatorAddress,
            GuildRepository repository)
        {
            Address = address;
            GuildMasterAddress = guildMasterAddress;
            ValidatorAddress = validatorAddress;
            Repository = repository;
        }

        public Guild(
            GuildAddress address,
            IValue bencoded,
            GuildRepository repository)
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

            if (integer == 1)
            {
                throw new FailedLoadStateException("Does not support version 1.");
            }

            Address = address;
            GuildMasterAddress = new AgentAddress(list[2]);
            ValidatorAddress = new AgentAddress(list[3]);
            Repository = repository;
        }

        public GuildAddress Address { get; }

        public GuildRepository Repository { get; }

        public List Bencoded => List.Empty
            .Add(StateTypeName)
            .Add(StateVersion)
            .Add(GuildMasterAddress.Bencoded)
            .Add(ValidatorAddress.Bencoded);

        IValue IBencodable.Bencoded => Bencoded;

        public void ClaimReward(Address validatorAddress, long height)
        {
            var guildDelegatee = Repository.GetDelegatee(validatorAddress);
            var guildDelegator = Repository.GetDelegator(Address);
            guildDelegator.ClaimReward(guildDelegatee, height);

            var validatorRepository = new ValidatorRepository(Repository);
            var validatorDelegatee = validatorRepository.GetDelegatee(validatorAddress);
            var validatorDelegator = validatorRepository.GetDelegator(Address);
            validatorDelegator.ClaimReward(validatorDelegatee, height);

            Repository.UpdateWorld(validatorRepository.World);
        }

        public bool Equals(Guild? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Address.Equals(other.Address)
                 && GuildMasterAddress.Equals(other.GuildMasterAddress)
                 && ValidatorAddress.Equals(other.ValidatorAddress);
        }

        public override bool Equals(object? obj)
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
