#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using Bencodex.Types;
using Libplanet.Crypto;
using Libplanet.Types.Assets;

namespace Nekoyume.Delegation
{
    public abstract class Delegator<TRepository, TDelegatee, TDelegator> : IDelegator
        where TRepository : DelegationRepository<TRepository, TDelegatee, TDelegator>
        where TDelegatee : Delegatee<TRepository, TDelegatee, TDelegator>
        where TDelegator : Delegator<TRepository, TDelegatee, TDelegator>
    {
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

        private Delegator(DelegatorMetadata metadata, TRepository repository)
        {
            Metadata = metadata;
            Repository = repository;
        }

        public DelegatorMetadata Metadata { get; }

        public TRepository Repository { get; }

        public Address Address => Metadata.DelegatorAddress;

        public Address AccountAddress => Metadata.DelegatorAccountAddress;

        public Address MetadataAddress => Metadata.Address;

        public Address DelegationPoolAddress => Metadata.DelegationPoolAddress;

        public Address RewardAddress => Metadata.RewardAddress;

        public ImmutableSortedSet<Address> Delegatees => Metadata.Delegatees;

        public List MetadataBencoded => Metadata.Bencoded;

        public virtual void Delegate(
            TDelegatee delegatee, FungibleAssetValue fav, long height)
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

            ReleaseUnbondings(height);

            delegatee.Bond((TDelegator)this, fav, height);
            Metadata.AddDelegatee(delegatee.Address);
            Repository.TransferAsset(DelegationPoolAddress, delegatee.DelegationPoolAddress, fav);
            Repository.SetDelegator((TDelegator)this);
        }

        void IDelegator.Delegate(
            IDelegatee delegatee, FungibleAssetValue fav, long height)
            => Delegate((TDelegatee)delegatee, fav, height);

        public virtual void Undelegate(
            TDelegatee delegatee, BigInteger share, long height)
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

            ReleaseUnbondings(height);

            UnbondLockIn unbondLockIn = Repository.GetUnbondLockIn(delegatee, Address);

            if (unbondLockIn.IsFull)
            {
                throw new InvalidOperationException("Undelegation is full.");
            }

            FungibleAssetValue fav = delegatee.Unbond((TDelegator)this, share, height);

            if (fav.Sign > 0)
            {
                unbondLockIn = unbondLockIn.LockIn(
                    fav, height, height + delegatee.UnbondingPeriod);
                AddUnbondingRef(delegatee, UnbondingFactory.ToReference(unbondLockIn));
                Repository.SetUnbondLockIn(unbondLockIn);
            }

            if (Repository.GetBond(delegatee, Address).Share.IsZero)
            {
                Metadata.RemoveDelegatee(delegatee.Address);
            }

            Repository.SetDelegator((TDelegator)this);
        }

        void IDelegator.Undelegate(
            IDelegatee delegatee, BigInteger share, long height)
            => Undelegate((TDelegatee)delegatee, share, height);


        public virtual void Redelegate(
            TDelegatee srcDelegatee, TDelegatee dstDelegatee, BigInteger share, long height)
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

            if (srcDelegatee.Equals(dstDelegatee))
            {
                throw new InvalidOperationException("Source and destination delegatees are the same.");
            }

            if (dstDelegatee.Tombstoned)
            {
                throw new InvalidOperationException("Destination delegatee is tombstoned.");
            }

            ReleaseUnbondings(height);

            RebondGrace srcRebondGrace = Repository.GetRebondGrace(srcDelegatee, Address);

            if (srcRebondGrace.IsFull)
            {
                throw new InvalidOperationException("Rebonding is full.");
            }

            FungibleAssetValue fav = srcDelegatee.Unbond(
                (TDelegator)this, share, height);
            dstDelegatee.Bond(
                (TDelegator)this, fav, height);

            srcRebondGrace = srcRebondGrace.Grace(
                dstDelegatee.Address,
                fav,
                height,
                height + srcDelegatee.UnbondingPeriod);

            if (Repository.GetBond(srcDelegatee, Address).Share.IsZero)
            {
                Metadata.RemoveDelegatee(srcDelegatee.Address);
            }

            Metadata.AddDelegatee(dstDelegatee.Address);

            AddUnbondingRef(srcDelegatee, UnbondingFactory.ToReference(srcRebondGrace));

            Repository.SetRebondGrace(srcRebondGrace);
            Repository.TransferAsset(srcDelegatee.DelegationPoolAddress, dstDelegatee.DelegationPoolAddress, fav);
            Repository.SetDelegator((TDelegator)this);
        }

        void IDelegator.Redelegate(
            IDelegatee srcDelegatee, IDelegatee dstDelegatee, BigInteger share, long height)
            => Redelegate((TDelegatee)srcDelegatee, (TDelegatee)dstDelegatee, share, height);

        public void CancelUndelegate(
            TDelegatee delegatee, FungibleAssetValue fav, long height)
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

            ReleaseUnbondings(height);

            UnbondLockIn unbondLockIn = Repository.GetUnbondLockIn(delegatee, Address);

            if (unbondLockIn.IsFull)
            {
                throw new InvalidOperationException("Undelegation is full.");
            }

            delegatee.Bond((TDelegator)this, fav, height);
            unbondLockIn = unbondLockIn.Cancel(fav, height);
            Metadata.AddDelegatee(delegatee.Address);

            if (unbondLockIn.IsEmpty)
            {
                RemoveUnbondingRef(delegatee, UnbondingFactory.ToReference(unbondLockIn));
            }

            Repository.SetUnbondLockIn(unbondLockIn);
            Repository.SetDelegator((TDelegator)this);
        }

        void IDelegator.CancelUndelegate(
            IDelegatee delegatee, FungibleAssetValue fav, long height)
            => CancelUndelegate((TDelegatee)delegatee, fav, height);

        public void ClaimReward(
            TDelegatee delegatee, long height)
        {
            delegatee.DistributeReward((TDelegator)this, height);
            delegatee.StartNewRewardPeriod(height);
            Repository.SetDelegator((TDelegator)this);
        }

        void IDelegator.ClaimReward(IDelegatee delegatee, long height)
            => ClaimReward((TDelegatee)delegatee, height);

        public void ReleaseUnbondings(long height)
        {
            var unbondings = Metadata.UnbondingRefs.Select(
                unbondingRef => UnbondingFactory.GetUnbondingFromRef(unbondingRef, Repository));
            ReleaseUnbondings(unbondings, height);
            Repository.SetDelegator((TDelegator)this);
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
            FungibleAssetValue? releasedFAV;
            switch (unbonding)
            {
                case UnbondLockIn unbondLockIn:
                    unbondLockIn = unbondLockIn.Release(height, out releasedFAV);
                    Repository.SetUnbondLockIn(unbondLockIn);
                    unbonding = unbondLockIn;
                    break;
                case RebondGrace rebondGrace:
                    rebondGrace = rebondGrace.Release(height, out releasedFAV);
                    Repository.SetRebondGrace(rebondGrace);
                    unbonding = rebondGrace;
                    break;
                default:
                    throw new InvalidOperationException("Invalid unbonding type.");
            }

            if (unbonding.IsEmpty)
            {
                TDelegatee delegatee = Repository.GetDelegatee(unbonding.DelegateeAddress);
                RemoveUnbondingRef(delegatee, UnbondingFactory.ToReference(unbonding));
            }

            OnUnbondingReleased(height, unbonding, releasedFAV);
        }

        private void AddUnbondingRef(TDelegatee delegatee, UnbondingRef reference)
        {
            AddUnbondingRef(reference);
            delegatee.AddUnbondingRef(reference);
            Repository.SetDelegatee(delegatee);
        }

        private void RemoveUnbondingRef(TDelegatee delegatee, UnbondingRef reference)
        {
            RemoveUnbondingRef(reference);
            delegatee.RemoveUnbondingRef(reference);
            Repository.SetDelegatee(delegatee);
        }

        private void AddUnbondingRef(UnbondingRef reference)
            => Metadata.AddUnbondingRef(reference);

        private void RemoveUnbondingRef(UnbondingRef reference)
            => Metadata.RemoveUnbondingRef(reference);
    }
}
