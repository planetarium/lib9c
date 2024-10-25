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
    public class GuildParticipant : Delegator<Guild, GuildParticipant>, IBencodable, IEquatable<GuildParticipant>
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
                  accountAddress: Addresses.GuildParticipant,
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

        public override void Delegate(Guild delegatee, FungibleAssetValue fav, long height)
        {
            if (fav.Sign <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(fav), fav, "Fungible asset value must be positive.");
            }

            delegatee.Bond(this, fav, height);

            var guildValidatorRepository = new GuildValidatorRepository(Repository.World, Repository.ActionContext);
            var guildValidatorDelegatee = guildValidatorRepository.GetGuildValidatorDelegatee(delegatee.ValidatorAddress);
            var guildValidatorDelegator = guildValidatorRepository.GetGuildValidatorDelegator(delegatee.Address);
            guildValidatorDelegatee.Bond(guildValidatorDelegator, fav, height);
            Repository.UpdateWorld(guildValidatorRepository.World);

            var validatorRepository = new ValidatorRepository(Repository.World, Repository.ActionContext);
            var validatorDelegatee = validatorRepository.GetValidatorDelegatee(delegatee.ValidatorAddress);
            var validatorDelegator = validatorRepository.GetValidatorDelegator(Address, delegatee.Address);
            validatorDelegator.Delegate(validatorDelegatee, fav, height);
            Repository.UpdateWorld(validatorRepository.World);
        }

        public override void Undelegate(Guild delegatee, BigInteger share, long height)
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

            FungibleAssetValue fav = delegatee.Unbond(this, share, height);

            var guildValidatorRepository = new GuildValidatorRepository(Repository.World, Repository.ActionContext);
            var guildValidatorDelegatee = guildValidatorRepository.GetGuildValidatorDelegatee(delegatee.ValidatorAddress);
            var guildValidatorDelegator = guildValidatorRepository.GetGuildValidatorDelegator(delegatee.Address);
            guildValidatorDelegatee.Unbond(guildValidatorDelegator, guildValidatorDelegatee.ShareFromFAV(fav), height);
            Repository.UpdateWorld(guildValidatorRepository.World);

            var validatorRepository = new ValidatorRepository(Repository.World, Repository.ActionContext);
            var validatorDelegatee = validatorRepository.GetValidatorDelegatee(delegatee.ValidatorAddress);
            var validatorDelegator = validatorRepository.GetValidatorDelegator(Address, delegatee.Address);
            validatorDelegator.Undelegate(validatorDelegatee, validatorDelegatee.ShareFromFAV(fav), height);
            Repository.UpdateWorld(validatorRepository.World);
        }

        public override void Redelegate(
            Guild srcDelegatee, Guild dstDelegatee, BigInteger share, long height)
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

            FungibleAssetValue fav = srcDelegatee.Unbond(this, share, height);
            dstDelegatee.Bond(this, fav, height);

            var guildValidatorRepository = new GuildValidatorRepository(
                Repository.World, Repository.ActionContext);
            var srcGuildValidatorDelegatee = guildValidatorRepository.GetGuildValidatorDelegatee(srcDelegatee.ValidatorAddress);
            var srcGuildValidatorDelegator = guildValidatorRepository.GetGuildValidatorDelegator(srcDelegatee.Address);
            var dstGuildValidatorDelegatee = guildValidatorRepository.GetGuildValidatorDelegatee(dstDelegatee.ValidatorAddress);
            var dstGuildValidatorDelegator = guildValidatorRepository.GetGuildValidatorDelegator(dstDelegatee.Address);
            srcGuildValidatorDelegatee.Unbond(srcGuildValidatorDelegator, share, height);
            dstGuildValidatorDelegatee.Bond(dstGuildValidatorDelegator, fav, height);
            Repository.UpdateWorld(guildValidatorRepository.World);

            var validatorRepository = new ValidatorRepository(
                Repository.World, Repository.ActionContext);
            var srcValidatorDelegatee = validatorRepository.GetValidatorDelegatee(srcDelegatee.ValidatorAddress);
            var srcValidatorDelegator = validatorRepository.GetValidatorDelegator(Address, srcDelegatee.Address);
            var dstValidatorDelegatee = validatorRepository.GetValidatorDelegatee(dstDelegatee.ValidatorAddress);
            var dstValidatorDelegator = validatorRepository.GetValidatorDelegator(Address, dstDelegatee.Address);
            srcValidatorDelegatee.Unbond(srcValidatorDelegator, share, height);
            dstValidatorDelegatee.Bond(dstValidatorDelegator, fav, height);
            Repository.UpdateWorld(validatorRepository.World);
        }

        public bool Equals(GuildParticipant other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Address.Equals(other.Address)
                 && GuildAddress.Equals(other.GuildAddress)
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
            return HashCode.Combine(Address, GuildAddress);
        }
    }
}
