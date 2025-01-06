#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Libplanet.Crypto;
using Libplanet.Types.Assets;

namespace Nekoyume.Delegation
{
    public abstract class Delegator<TRepository, TDelegatee, TDelegator>
        where TRepository : DelegationRepository<TRepository, TDelegatee, TDelegator>
        where TDelegatee : Delegatee<TRepository, TDelegatee, TDelegator>
        where TDelegator : Delegator<TRepository, TDelegatee, TDelegator>
    {
        private readonly TRepository? _repository;

        public Delegator(
            Address address,
            Address accountAddress,
            Address delegationPoolAddress,
            Address rewardAddress,
            TRepository repository)
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
            TRepository repository)
            : this(repository.GetDelegatorMetadata(address), repository)
        {
        }

        private Delegator(DelegatorMetadata metadata, TRepository? repository = null)
        {
            Metadata = metadata;
            _repository = repository;
        }

        public DelegatorMetadata Metadata { get; private set; }

        public Address Address => Metadata.DelegatorAddress;

        protected TRepository Repository
            => _repository ?? throw new InvalidOperationException("Repository is not set.");

        public virtual void Delegate(TDelegatee delegatee, FungibleAssetValue fav, long height)
        {
            var repository = Repository;
            if (fav.Sign <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(fav), fav, "Fungible asset value must be positive.");
            }

            var metadata = Metadata;
            var delegateeMetadata = delegatee.Metadata;
            if (delegateeMetadata.Tombstoned)
            {
                throw new InvalidOperationException("Delegatee is tombstoned.");
            }

            ReleaseUnbondings(height);

            delegatee.Bond((TDelegator)this, fav, height);
            UpdateMetadata(metadata.AddDelegatee(delegatee.Address));
            repository.TransferAsset(Metadata.DelegationPoolAddress, delegateeMetadata.DelegationPoolAddress, fav);
            repository.SetDelegator((TDelegator)this);
        }

        public virtual void Undelegate(
            TDelegatee delegatee, BigInteger share, long height)
        {
            var repository = Repository;
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

            ReleaseUnbondings(height);

            var unbondLockIn = repository.GetUnbondLockIn(delegatee, Address);
            if (unbondLockIn.IsFull)
            {
                throw new InvalidOperationException("Undelegation is full.");
            }

            var metadata = Metadata;
            var unbondingPeriod = delegatee.Metadata.UnbondingPeriod;
            var expireHeight = height + unbondingPeriod;
            var unbondResult = delegatee.Unbond((TDelegator)this, share, height);
            var fav = unbondResult.Fav;

            unbondLockIn = unbondLockIn.LockIn(fav, height, expireHeight);
            if (unbondResult.Bond.Share.IsZero)
            {
                metadata = metadata.RemoveDelegatee(delegatee.Address);
            }

            metadata = metadata.AddUnbondingRef(unbondLockIn.Reference);

            UpdateMetadata(metadata);
            repository.SetUnbonding(unbondLockIn);
            repository.SetDelegator((TDelegator)this);
        }

        public virtual void Redelegate(
            TDelegatee srcDelegatee, TDelegatee dstDelegatee, BigInteger share, long height)
        {
            var repository = Repository;
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

            if (srcDelegatee.Equals(dstDelegatee))
            {
                throw new InvalidOperationException("Source and destination delegatees are the same.");
            }

            if (dstDelegatee.Metadata.Tombstoned)
            {
                throw new InvalidOperationException("Destination delegatee is tombstoned.");
            }

            ReleaseUnbondings(height);

            var srcRebondGrace = repository.GetRebondGrace(srcDelegatee, Address);
            if (srcRebondGrace.IsFull)
            {
                throw new InvalidOperationException("Rebonding is full.");
            }

            var metadata = Metadata;
            var unbondResult = srcDelegatee.Unbond((TDelegator)this, share, height);
            var fav = unbondResult.Fav;

            srcRebondGrace = srcRebondGrace.Grace(
                dstDelegatee.Address,
                fav,
                height,
                height + srcDelegatee.Metadata.UnbondingPeriod);

            if (unbondResult.Bond.Share.IsZero)
            {
                metadata = metadata.RemoveDelegatee(srcDelegatee.Address);
            }

            metadata = metadata.AddDelegatee(dstDelegatee.Address)
                .AddUnbondingRef(srcRebondGrace.Reference);

            UpdateMetadata(metadata);
            repository.SetUnbonding(srcRebondGrace);
            repository.TransferAsset(srcDelegatee.Metadata.DelegationPoolAddress, dstDelegatee.Metadata.DelegationPoolAddress, fav);
            repository.SetDelegator((TDelegator)this);
        }

        public void CancelUndelegate(
            TDelegatee delegatee, FungibleAssetValue fav, long height)
        {
            var repository = Repository;
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

            ReleaseUnbondings(height);
            var metadata = Metadata;
            var delegateeMetadata = delegatee.Metadata;
            UnbondLockIn unbondLockIn = repository.GetUnbondLockIn(delegatee, Address);

            if (unbondLockIn.IsFull)
            {
                throw new InvalidOperationException("Undelegation is full.");
            }

            delegatee.Bond((TDelegator)this, fav, height);
            unbondLockIn = unbondLockIn.Cancel(fav, height);
            metadata = metadata.AddDelegatee(delegatee.Address);

            if (unbondLockIn.IsEmpty)
            {
                metadata = metadata.RemoveUnbondingRef(unbondLockIn.Reference);
            }

            repository.SetUnbonding(unbondLockIn);
            UpdateMetadata(metadata);
            repository.SetDelegator((TDelegator)this);
        }

        public void ClaimReward(
            TDelegatee delegatee, long height)
        {
            var repository = Repository;
            delegatee.DistributeReward((TDelegator)this, height);
            delegatee.StartNewRewardPeriod(height);
            repository.SetDelegator((TDelegator)this);
        }

        public void ReleaseUnbondings(long height)
        {
            var repository = Repository;
            var unbondings = Metadata.UnbondingRefs.Select(
                unbondingRef => repository.GetUnbonding(unbondingRef));
            ReleaseUnbondings(unbondings, height);
            repository.SetDelegator((TDelegator)this);
        }

        protected void UpdateMetadata(DelegatorMetadata metadata)
        {
            Metadata = metadata;
        }

        protected virtual void OnUnbondingReleased(
            long height, IUnbonding releasedUnbonding, FungibleAssetValue? releasedFAV)
        {
        }

        private void ReleaseUnbondings(IEnumerable<IUnbonding> unbondings, long height)
        {
            foreach (var unbonding in unbondings)
            {
                ReleaseUnbonding(unbonding, height);
            }
        }

        private void ReleaseUnbonding(IUnbonding unbonding, long height)
        {
            var repository = Repository;
            FungibleAssetValue? releasedFAV;
            switch (unbonding)
            {
                case UnbondLockIn unbondLockIn:
                    unbondLockIn = unbondLockIn.Release(height, out releasedFAV);
                    repository.SetUnbonding(unbondLockIn);
                    unbonding = unbondLockIn;
                    break;
                case RebondGrace rebondGrace:
                    rebondGrace = rebondGrace.Release(height, out releasedFAV);
                    repository.SetUnbonding(rebondGrace);
                    unbonding = rebondGrace;
                    break;
                default:
                    throw new InvalidOperationException("Invalid unbonding type.");
            }

            if (unbonding.IsEmpty)
            {
                TDelegatee delegatee = repository.GetDelegatee(unbonding.DelegateeAddress);
                var metadata = Metadata.RemoveUnbondingRef(unbonding.Reference);
                UpdateMetadata(metadata);
                // RemoveUnbondingRef(delegatee, unbonding.Reference);
            }

            OnUnbondingReleased(height, unbonding, releasedFAV);
        }
    }
}
