#nullable enable
using System;
using System.Numerics;
using Bencodex;
using Bencodex.Types;
using Libplanet.Types.Assets;
using Nekoyume.Action;
using Nekoyume.Delegation;
using Nekoyume.Model.Stake;
using Nekoyume.TypedAddress;
using Nekoyume.ValidatorDelegation;

namespace Nekoyume.Model.Guild
{
    // It Does not inherit from `Delegator`, since `Validator` related functionalities
    // will be moved to lower level library.
    public class GuildParticipant
        : Delegator<ValidatorDelegateeForGuildParticipant, GuildParticipant>, IBencodable, IEquatable<GuildParticipant>
    {
        private const string StateTypeName = "guild_participant";
        private const long StateVersion = 1;

        public readonly GuildAddress GuildAddress;

        public GuildParticipant(
            AgentAddress address,
            GuildAddress guildAddress,
            GuildRepository repository)
            : base(
                  address: address,
                  accountAddress: repository.DelegatorAccountAddress,
                  delegationPoolAddress: StakeState.DeriveAddress(address),
                  rewardAddress: address,
                  repository: repository)
        {
            GuildAddress = guildAddress;
        }

        public GuildParticipant(
            AgentAddress address,
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

            GuildAddress = new GuildAddress(list[2]);
        }

        public new AgentAddress Address => new AgentAddress(base.Address);

        public List Bencoded => List.Empty
            .Add(StateTypeName)
            .Add(StateVersion)
            .Add(GuildAddress.Bencoded);

        IValue IBencodable.Bencoded => Bencoded;

        public void Delegate(Guild guild, FungibleAssetValue fav, long height)
        {
            var repository = (GuildRepository)Repository;

            if (fav.Sign <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(fav), fav, "Fungible asset value must be positive.");
            }

            var validatorDelegateeForGuildParticipant = repository.GetValidatorDelegateeForGuildParticipant(guild.ValidatorAddress);
            base.Delegate(validatorDelegateeForGuildParticipant, fav, height);

            var validatorRepository = new ValidatorRepository(Repository.World, Repository.ActionContext);
            var validatorDelegatee = validatorRepository.GetValidatorDelegatee(guild.ValidatorAddress);
            var validatorDelegator = validatorRepository.GetValidatorDelegator(guild.Address);
            validatorDelegatee.Bond(validatorDelegator, fav, height);

            Repository.UpdateWorld(validatorRepository.World);
        }

        public void Undelegate(Guild guild, BigInteger share, long height)
        {
            var repository = (GuildRepository)Repository;

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

            var validatorDelegateeForGuildParticipant = repository.GetValidatorDelegateeForGuildParticipant(guild.ValidatorAddress);
            base.Undelegate(validatorDelegateeForGuildParticipant, share, height);

            var validatorRepository = new ValidatorRepository(Repository.World, Repository.ActionContext);
            var validatorDelegatee = validatorRepository.GetValidatorDelegatee(guild.ValidatorAddress);
            var validatorDelegator = validatorRepository.GetValidatorDelegator(guild.Address);
            validatorDelegatee.Unbond(validatorDelegator, share, height);

            Repository.UpdateWorld(validatorRepository.World);
        }

        public void Redelegate(
            Guild srcGuild, Guild dstGuild, BigInteger share, long height)
        {
            var repository = (GuildRepository)Repository;

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

            var srcValidatorDelegateeForGuildParticipant = repository.GetValidatorDelegateeForGuildParticipant(srcGuild.ValidatorAddress);
            var dstValidatorDelegateeForGuildParticipant = repository.GetValidatorDelegateeForGuildParticipant(dstGuild.ValidatorAddress);
            base.Redelegate(srcValidatorDelegateeForGuildParticipant, dstValidatorDelegateeForGuildParticipant, share, height);

            var validatorRepository = new ValidatorRepository(Repository.World, Repository.ActionContext);
            var srcValidatorDelegatee = validatorRepository.GetValidatorDelegatee(srcGuild.ValidatorAddress);
            var srcValidatorDelegator = validatorRepository.GetValidatorDelegator(srcGuild.Address);
            var fav = srcValidatorDelegatee.Unbond(srcValidatorDelegator, share, height);
            var dstValidatorDelegatee = validatorRepository.GetValidatorDelegatee(dstGuild.ValidatorAddress);
            var dstValidatorDelegator = validatorRepository.GetValidatorDelegator(dstGuild.Address);
            dstValidatorDelegatee.Bond(dstValidatorDelegator, fav, height);

            Repository.UpdateWorld(validatorRepository.World);
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
