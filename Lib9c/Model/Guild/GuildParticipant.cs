#nullable enable
using System;
using System.Numerics;
using Bencodex;
using Bencodex.Types;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Action;
using Nekoyume.TypedAddress;
using Nekoyume.ValidatorDelegation;

namespace Nekoyume.Model.Guild
{
    // It Does not inherit from `Delegator`, since `Validator` related functionalities
    // will be moved to lower level library.
    public class GuildParticipant : IBencodable, IEquatable<GuildParticipant>
    {
        private const string StateTypeName = "guild_participant";
        private const long StateVersion = 2;

        public readonly GuildAddress GuildAddress;

        public GuildParticipant(
            AgentAddress address,
            GuildAddress guildAddress,
            GuildRepository repository)
        {
            Address = address;
            GuildAddress = guildAddress;
            Repository = repository;
        }

        public GuildParticipant(
            AgentAddress address,
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
                throw new FailedLoadStateException("State version 1 is not supported.");
            }

            Address = address;
            GuildAddress = new GuildAddress(list[2]);
            Repository = repository;
        }

        public AgentAddress Address { get; }

        public Address DelegationPoolAddress
            => Repository.GetDelegator(Address).DelegationPoolAddress;

        public Address RewardAddress
            => Repository.GetDelegator(Address).RewardAddress;

        public GuildRepository Repository { get; }

        public List Bencoded => List.Empty
            .Add(StateTypeName)
            .Add(StateVersion)
            .Add(GuildAddress.Bencoded);

        IValue IBencodable.Bencoded => Bencoded;

        public void Delegate(Guild guild, FungibleAssetValue fav, long height)
        {
            if (fav.Sign <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(fav), fav, "Fungible asset value must be positive.");
            }

            var guildDelegatee = Repository.GetDelegatee(guild.ValidatorAddress);
            var guildDelegator = Repository.GetDelegator(Address);
            guildDelegator.Delegate(guildDelegatee, fav, height);

            var validatorRepository = new ValidatorRepository(Repository);
            var validatorDelegatee = validatorRepository.GetDelegatee(guild.ValidatorAddress);
            var validatorDelegator = validatorRepository.GetDelegator(guild.Address);
            validatorDelegatee.Bond(validatorDelegator, fav, height);

            Repository.UpdateWorld(validatorRepository.World);
        }

        public void Undelegate(Guild guild, BigInteger share, long height)
        {
            if (share.Sign <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(share), share, "Share must be positive.");
            }

            if (height <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(height), height, "Height must be positive.");
            }

            var guildDelegatee = Repository.GetDelegatee(guild.ValidatorAddress);
            var guildDelegator = Repository.GetDelegator(Address);
            guildDelegator.Undelegate(guildDelegatee, share, height);

            var validatorRepository = new ValidatorRepository(Repository);
            var validatorDelegatee = validatorRepository.GetDelegatee(guild.ValidatorAddress);
            var validatorDelegator = validatorRepository.GetDelegator(guild.Address);
            validatorDelegatee.Unbond(validatorDelegator, share, height);

            Repository.UpdateWorld(validatorRepository.World);
        }

        public void Redelegate(
            Guild srcGuild, Guild dstGuild, BigInteger share, long height)
        {
            if (share.Sign <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(share), share, "Share must be positive.");
            }

            if (height <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(height), height, "Height must be positive.");
            }

            var srcGuildDelegatee = Repository.GetDelegatee(srcGuild.ValidatorAddress);
            var dstGuildDelegator = Repository.GetDelegatee(dstGuild.ValidatorAddress);
            var guildDelegator = Repository.GetDelegator(Address);
            guildDelegator.Redelegate(srcGuildDelegatee, dstGuildDelegator, share, height);

            var validatorRepository = new ValidatorRepository(Repository);
            var srcValidatorDelegatee = validatorRepository.GetDelegatee(srcGuild.ValidatorAddress);
            var srcValidatorDelegator = validatorRepository.GetDelegator(srcGuild.Address);
            var fav = srcValidatorDelegatee.Unbond(srcValidatorDelegator, share, height);
            var dstValidatorDelegatee = validatorRepository.GetDelegatee(dstGuild.ValidatorAddress);
            var dstValidatorDelegator = validatorRepository.GetDelegator(dstGuild.Address);
            dstValidatorDelegatee.Bond(dstValidatorDelegator, fav, height);

            Repository.UpdateWorld(validatorRepository.World);
        }

        public void ClaimReward(Guild guild, long height)
        {
            var guildDelegatee = Repository.GetDelegatee(guild.ValidatorAddress);
            var guildDelegator = Repository.GetDelegator(Address);
            guildDelegator.ClaimReward(guildDelegatee, height);
        }

        public bool Equals(GuildParticipant? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Address.Equals(other.Address)
                 && GuildAddress.Equals(other.GuildAddress);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((GuildParticipant)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Address, GuildAddress);
        }
    }
}
