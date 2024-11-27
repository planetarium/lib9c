#nullable enable
using System;
using System.Diagnostics;
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
        private static readonly ActivitySource ActivitySource = new ActivitySource("Lib9c.Model.Guild.GuildParticipant");

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
            => Repository.GetGuildDelegator(Address).DelegationPoolAddress;

        public Address RewardAddress
            => Repository.GetGuildDelegator(Address).RewardAddress;

        public GuildRepository Repository { get; }

        public List Bencoded => List.Empty
            .Add(StateTypeName)
            .Add(StateVersion)
            .Add(GuildAddress.Bencoded);

        IValue IBencodable.Bencoded => Bencoded;

        public void Delegate(Guild guild, FungibleAssetValue fav, long height)
        {
            using var activity = ActivitySource.StartActivity("Delegate");

            if (fav.Sign <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(fav), fav, "Fungible asset value must be positive.");
            }

            var getGuildDelegateeActivity = ActivitySource.StartActivity(
                "GetGuildDelegatee",
                ActivityKind.Internal,
                activity?.Id ?? string.Empty);
            var guildDelegatee = Repository.GetGuildDelegatee(guild.ValidatorAddress);
            getGuildDelegateeActivity?.Dispose();

            var getGuildDelegatorActivity = ActivitySource.StartActivity(
               "GetGuildDelegator",
               ActivityKind.Internal,
               activity?.Id ?? string.Empty);
            var guildDelegator = Repository.GetGuildDelegator(Address);
            getGuildDelegatorActivity?.Dispose();

            var guildDelegateActivity = ActivitySource.StartActivity(
               "GuildDelegate",
               ActivityKind.Internal,
               activity?.Id ?? string.Empty);
            guildDelegator.Delegate(guildDelegatee, fav, height);
            guildDelegateActivity?.Dispose();

            var createValidatorRepositoryActivity = ActivitySource.StartActivity(
               "CreateValidatorRepository",
               ActivityKind.Internal,
               activity?.Id ?? string.Empty);
            var validatorRepository = new ValidatorRepository(Repository);
            createValidatorRepositoryActivity?.Dispose();

            var getValidatorDelegateeActivity = ActivitySource.StartActivity(
                "GetValidatorDelegatee",
                ActivityKind.Internal,
                activity?.Id ?? string.Empty);
            var validatorDelegatee = validatorRepository.GetValidatorDelegatee(guild.ValidatorAddress);
            getValidatorDelegateeActivity?.Dispose();

            var getValidatorDelegatorActivity = ActivitySource.StartActivity(
                "GetValidatorDelegator",
                ActivityKind.Internal,
                activity?.Id ?? string.Empty);
            var validatorDelegator = validatorRepository.GetValidatorDelegator(guild.Address);
            getValidatorDelegatorActivity?.Dispose();

            var bondActivity = ActivitySource.StartActivity(
                "Bond",
                ActivityKind.Internal,
                activity?.Id ?? string.Empty);
            validatorDelegatee.Bond(validatorDelegator, fav, height);
            bondActivity?.Dispose();

            var updateWorldActivity = ActivitySource.StartActivity(
                "UpdateWorld",
                ActivityKind.Internal,
                activity?.Id ?? string.Empty);
            Repository.UpdateWorld(validatorRepository.World);
            updateWorldActivity?.Dispose();
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

            var guildDelegatee = Repository.GetGuildDelegatee(guild.ValidatorAddress);
            var guildDelegator = Repository.GetGuildDelegator(Address);
            guildDelegator.Undelegate(guildDelegatee, share, height);

            var validatorRepository = new ValidatorRepository(Repository);
            var validatorDelegatee = validatorRepository.GetValidatorDelegatee(guild.ValidatorAddress);
            var validatorDelegator = validatorRepository.GetValidatorDelegator(guild.Address);
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

            var srcGuildDelegatee = Repository.GetGuildDelegatee(srcGuild.ValidatorAddress);
            var dstGuildDelegator = Repository.GetGuildDelegatee(dstGuild.ValidatorAddress);
            var guildDelegator = Repository.GetGuildDelegator(Address);
            guildDelegator.Redelegate(srcGuildDelegatee, dstGuildDelegator, share, height);

            var validatorRepository = new ValidatorRepository(Repository);
            var srcValidatorDelegatee = validatorRepository.GetValidatorDelegatee(srcGuild.ValidatorAddress);
            var srcValidatorDelegator = validatorRepository.GetValidatorDelegator(srcGuild.Address);
            var fav = srcValidatorDelegatee.Unbond(srcValidatorDelegator, share, height);
            var dstValidatorDelegatee = validatorRepository.GetValidatorDelegatee(dstGuild.ValidatorAddress);
            var dstValidatorDelegator = validatorRepository.GetValidatorDelegator(dstGuild.Address);
            dstValidatorDelegatee.Bond(dstValidatorDelegator, fav, height);

            Repository.UpdateWorld(validatorRepository.World);
        }

        public void ClaimReward(Guild guild, long height)
        {
            var guildDelegatee = Repository.GetGuildDelegatee(guild.ValidatorAddress);
            var guildDelegator = Repository.GetGuildDelegator(Address);
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
