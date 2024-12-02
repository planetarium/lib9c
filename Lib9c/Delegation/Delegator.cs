#nullable enable
using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Numerics;
using Bencodex.Types;
using Libplanet.Crypto;
using Libplanet.Types.Assets;

namespace Nekoyume.Delegation
{
    public abstract class Delegator<T, TSelf> : IDelegator
        where T : Delegatee<TSelf, T>
        where TSelf : Delegator<T, TSelf>
    {
        private static readonly ActivitySource ActivitySource
            = new ActivitySource("Lib9c.Delegation.Delegator");

        public Delegator(
            Address address,
            Address accountAddress,
            Address delegationPoolAddress,
            Address rewardAddress,
            IDelegationRepository repository)
            : this(
                  new DelegatorMetadata(
                      address,
                      accountAddress,
                      delegationPoolAddress,
                      rewardAddress),
                  repository)
        {
        }

        public Delegator(
            Address address,
            IDelegationRepository repository)
            : this(repository.GetDelegatorMetadata(address), repository)
        {
        }

        private Delegator(DelegatorMetadata metadata, IDelegationRepository repository)
        {
            Metadata = metadata;
            Repository = repository;
        }

        public DelegatorMetadata Metadata { get; }

        public IDelegationRepository Repository { get; }

        public Address Address => Metadata.DelegatorAddress;

        public Address AccountAddress => Metadata.DelegatorAccountAddress;

        public Address MetadataAddress => Metadata.Address;

        public Address DelegationPoolAddress => Metadata.DelegationPoolAddress;

        public Address RewardAddress => Metadata.RewardAddress;

        public ImmutableSortedSet<Address> Delegatees => Metadata.Delegatees;

        public List MetadataBencoded => Metadata.Bencoded;

        public virtual void Delegate(
            T delegatee, FungibleAssetValue fav, long height)
        {
            using var activity = ActivitySource.StartActivity("Delegate");

            if (fav.Sign <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(fav), fav, "Fungible asset value must be positive.");
            }

            if (delegatee.Tombstoned)
            {
                throw new InvalidOperationException("Delegatee is tombstoned.");
            }

            var bondActivity = ActivitySource.StartActivity(
                "Bond",
                ActivityKind.Internal,
                activity?.Id ?? string.Empty);
            delegatee.Bond(this, fav, height);
            bondActivity?.Dispose();

            var addDelegateeActivity = ActivitySource.StartActivity(
                "AddDelegatee",
                ActivityKind.Internal,
                activity?.Id ?? string.Empty);
            Metadata.AddDelegatee(delegatee.Address);
            addDelegateeActivity?.Dispose();

            var transferAssetActivity = ActivitySource.StartActivity(
                "TransferAsset",
                ActivityKind.Internal,
                activity?.Id ?? string.Empty);
            Repository.TransferAsset(DelegationPoolAddress, delegatee.DelegationPoolAddress, fav);
            transferAssetActivity?.Dispose();

            var setDelegatorActivity = ActivitySource.StartActivity(
                "SetDelegator",
                ActivityKind.Internal,
                activity?.Id ?? string.Empty);
            Repository.SetDelegator(this);
            setDelegatorActivity?.Dispose();
        }

        void IDelegator.Delegate(
            IDelegatee delegatee, FungibleAssetValue fav, long height)
            => Delegate((T)delegatee, fav, height);

        public virtual void Undelegate(
            T delegatee, BigInteger share, long height)
        {
            using var activity = ActivitySource.StartActivity("Undelegate");

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

            var getUnbondLockInActivity = ActivitySource.StartActivity(
                "GetUnbondLockIn",
                ActivityKind.Internal,
                activity?.Id ?? string.Empty);
            UnbondLockIn unbondLockIn = Repository.GetUnbondLockIn(delegatee, Address);
            getUnbondLockInActivity?.Dispose();

            if (unbondLockIn.IsFull)
            {
                throw new InvalidOperationException("Undelegation is full.");
            }

            var unbondActivity = ActivitySource.StartActivity(
                "Unbond",
                ActivityKind.Internal,
                activity?.Id ?? string.Empty);
            FungibleAssetValue fav = delegatee.Unbond(this, share, height);
            unbondActivity?.Dispose();

            var lockInActivity = ActivitySource.StartActivity(
                "LockIn",
                ActivityKind.Internal,
                activity?.Id ?? string.Empty);
            unbondLockIn = unbondLockIn.LockIn(
                fav, height, height + delegatee.UnbondingPeriod);
            lockInActivity?.Dispose();

            var removeDelegateeActivity = ActivitySource.StartActivity(
                "RemoveDelegatee",
                ActivityKind.Internal,
                activity?.Id ?? string.Empty);
            if (Repository.GetBond(delegatee, Address).Share.IsZero)
            {
                Metadata.RemoveDelegatee(delegatee.Address);
            }
            removeDelegateeActivity?.Dispose();

            var addUnbondingRefActivity = ActivitySource.StartActivity(
                "AddUnbondingRef",
                ActivityKind.Internal,
                activity?.Id ?? string.Empty);
            delegatee.AddUnbondingRef(UnbondingFactory.ToReference(unbondLockIn));
            addUnbondingRefActivity?.Dispose();

            var setUnbondLockInActivity = ActivitySource.StartActivity(
                "SetUnbondLockIn",
                ActivityKind.Internal,
                activity?.Id ?? string.Empty);
            Repository.SetUnbondLockIn(unbondLockIn);
            setUnbondLockInActivity?.Dispose();

            var setUnbondingSetActivity = ActivitySource.StartActivity(
                "SetUnbondingSet",
                ActivityKind.Internal,
                activity?.Id ?? string.Empty);
            Repository.SetUnbondingSet(
                Repository.GetUnbondingSet().SetUnbonding(unbondLockIn));
            setUnbondingSetActivity?.Dispose();

            var setDelegatorActivity = ActivitySource.StartActivity(
                "SetDelegator",
                ActivityKind.Internal,
                activity?.Id ?? string.Empty);
            Repository.SetDelegator(this);
            setDelegatorActivity?.Dispose();
        }

        void IDelegator.Undelegate(
            IDelegatee delegatee, BigInteger share, long height)
            => Undelegate((T)delegatee, share, height);


        public virtual void Redelegate(
            T srcDelegatee, T dstDelegatee, BigInteger share, long height)
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

            FungibleAssetValue fav = srcDelegatee.Unbond(
                this, share, height);
            dstDelegatee.Bond(
                this, fav, height);
            RebondGrace srcRebondGrace = Repository.GetRebondGrace(srcDelegatee, Address).Grace(
                dstDelegatee.Address,
                fav,
                height,
                height + srcDelegatee.UnbondingPeriod);

            if (Repository.GetBond(srcDelegatee, Address).Share.IsZero)
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

        void IDelegator.Redelegate(
            IDelegatee srcDelegatee, IDelegatee dstDelegatee, BigInteger share, long height)
            => Redelegate((T)srcDelegatee, (T)dstDelegatee, share, height);

        public void CancelUndelegate(
            T delegatee, FungibleAssetValue fav, long height)
        {
            if (fav.Sign <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(fav), fav, "Fungible asset value must be positive.");
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

            delegatee.Bond(this, fav, height);
            unbondLockIn = unbondLockIn.Cancel(fav, height);
            Metadata.AddDelegatee(delegatee.Address);

            if (unbondLockIn.IsEmpty)
            {
                delegatee.RemoveUnbondingRef(UnbondingFactory.ToReference(unbondLockIn));
            }

            Repository.SetUnbondLockIn(unbondLockIn);
            Repository.SetUnbondingSet(
                Repository.GetUnbondingSet().SetUnbonding(unbondLockIn));
            Repository.SetDelegator(this);
        }

        void IDelegator.CancelUndelegate(
            IDelegatee delegatee, FungibleAssetValue fav, long height)
            => CancelUndelegate((T)delegatee, fav, height);

        public void ClaimReward(
            T delegatee, long height)
        {
            delegatee.DistributeReward(this, height);
            Repository.SetDelegator(this);
        }

        void IDelegator.ClaimReward(IDelegatee delegatee, long height)
            => ClaimReward((T)delegatee, height);
    }
}
