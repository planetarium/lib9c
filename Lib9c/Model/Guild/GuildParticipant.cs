using System;
using System.Numerics;
using Bencodex;
using Bencodex.Types;
using Libplanet.Types.Assets;
using Nekoyume.Action;
using Nekoyume.Delegation;
using Nekoyume.TypedAddress;

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
                  delegationPoolAddress: address,
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

            if (delegatee.Tombstoned)
            {
                throw new InvalidOperationException("Delegatee is tombstoned.");
            }

            var guildValidatorRepository = new GuildValidatorRepository(
                Repository.World, Repository.ActionContext);
            var guildValidatorDelegatee = guildValidatorRepository.GetDelegatee(delegatee.ValidatorAddress);
            var guildValidatorDelegator = guildValidatorRepository.GetDelegator(delegatee.Address);

            guildValidatorDelegatee.Bond(guildValidatorDelegator, fav, height);
            Repository.UpdateWorld(guildValidatorRepository.World);
            Metadata.AddDelegatee(delegatee.Address);
            Repository.TransferAsset(DelegationPoolAddress, delegatee.DelegationPoolAddress, fav);
            Repository.SetDelegator(this);
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

            UnbondLockIn unbondLockIn = Repository.GetUnbondLockIn(delegatee, Address);

            if (unbondLockIn.IsFull)
            {
                throw new InvalidOperationException("Undelegation is full.");
            }

            var guildValidatorRepository = new GuildValidatorRepository(
                Repository.World, Repository.ActionContext);
            var guildValidatorDelegatee = guildValidatorRepository.GetDelegatee(delegatee.ValidatorAddress);
            var guildValidatorDelegator = guildValidatorRepository.GetDelegator(delegatee.Address);
            FungibleAssetValue fav = guildValidatorDelegatee.Unbond(guildValidatorDelegator, share, height);
            Repository.UpdateWorld(guildValidatorRepository.World);
            unbondLockIn = unbondLockIn.LockIn(
                fav, height, height + delegatee.UnbondingPeriod);

            if (!delegatee.Delegators.Contains(Address))
            {
                Metadata.RemoveDelegatee(delegatee.Address);
            }

            delegatee.AddUnbondingRef(UnbondingFactory.ToReference(unbondLockIn));

            Repository.SetUnbondLockIn(unbondLockIn);
            Repository.SetUnbondingSet(
                Repository.GetUnbondingSet().SetUnbonding(unbondLockIn));
            Repository.SetDelegator(this);
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

            if (dstDelegatee.Tombstoned)
            {
                throw new InvalidOperationException("Destination delegatee is tombstoned.");
            }

            var guildValidatorRepository = new GuildValidatorRepository(
                Repository.World, Repository.ActionContext);
            var srcGuildValidatorDelegatee = guildValidatorRepository.GetDelegatee(srcDelegatee.ValidatorAddress);
            var srcGuildValidatorDelegator = guildValidatorRepository.GetDelegator(srcDelegatee.Address);
            var dstGuildValidatorDelegatee = guildValidatorRepository.GetDelegatee(dstDelegatee.ValidatorAddress);
            var dstGuildValidatorDelegator = guildValidatorRepository.GetDelegator(dstDelegatee.Address);

            FungibleAssetValue fav = srcGuildValidatorDelegatee.Unbond(
                srcGuildValidatorDelegator, share, height);
            dstGuildValidatorDelegatee.Bond(
                dstGuildValidatorDelegator, fav, height);
            Repository.UpdateWorld(guildValidatorRepository.World);
            RebondGrace srcRebondGrace = Repository.GetRebondGrace(srcDelegatee, Address).Grace(
                dstDelegatee.Address,
                fav,
                height,
                height + srcDelegatee.UnbondingPeriod);

            if (!srcDelegatee.Delegators.Contains(Address))
            {
                Metadata.RemoveDelegatee(srcDelegatee.Address);
            }

            Metadata.AddDelegatee(dstDelegatee.Address);

            srcDelegatee.AddUnbondingRef(UnbondingFactory.ToReference(srcRebondGrace));

            Repository.SetRebondGrace(srcRebondGrace);
            Repository.SetUnbondingSet(
                Repository.GetUnbondingSet().SetUnbonding(srcRebondGrace));
            Repository.SetDelegator(this);
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
