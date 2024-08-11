#nullable enable
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using Bencodex;
using Bencodex.Types;
using Libplanet.Crypto;
using Libplanet.Types.Assets;

namespace Nekoyume.Delegation
{
    public abstract class Delegator<T, TSelf> : IDelegator
        where T : Delegatee<TSelf, T>
        where TSelf : Delegator<T, TSelf>
    {
        private readonly IDelegationRepository? _repository;

        public Delegator(Address address, IDelegationRepository? repository = null)
            : this(address, ImmutableSortedSet<Address>.Empty, 0L, repository)
        {
        }

        public Delegator(Address address, IValue bencoded, IDelegationRepository? repository = null)
            : this(address, (List)bencoded, repository)
        {
        }

        public Delegator(Address address, List bencoded, IDelegationRepository? repository = null)
            : this(
                address,
                ((List)bencoded[0]).Select(item => new Address(item)).ToImmutableSortedSet(),
                (Integer)bencoded[1],
                repository)
        {
        }

        private Delegator(
            Address address,
            ImmutableSortedSet<Address> delegatees,
            long lastRewardHeight,
            IDelegationRepository? repository)
        {
            Address = address;
            Delegatees = delegatees;
            LastRewardHeight = lastRewardHeight;
            _repository = repository;
        }

        public Address Address { get; }

        public ImmutableSortedSet<Address> Delegatees { get; private set; }

        public long LastRewardHeight { get; private set; }

        public IDelegationRepository? Repository => _repository;

        public List Bencoded
            => List.Empty
                .Add(new List(Delegatees.Select(a => a.Bencoded)))
                .Add(LastRewardHeight);

        IValue IBencodable.Bencoded => Bencoded;

        void IDelegator.Delegate(
            IDelegatee delegatee, FungibleAssetValue fav, long height)
            => Delegate((T)delegatee, fav, height);

        void IDelegator.Undelegate(
            IDelegatee delegatee, BigInteger share, long height)
            => Undelegate((T)delegatee, share, height);

        void IDelegator.Redelegate(
            IDelegatee srcDelegatee, IDelegatee dstDelegatee, BigInteger share, long height)
            => Redelegate((T)srcDelegatee, (T)dstDelegatee, share, height);

        void IDelegator.ClaimReward(
            IDelegatee delegatee, long height)
            => ClaimReward((T)delegatee, height);

        public void Delegate(
            T delegatee, FungibleAssetValue fav, long height)
        {
            CannotMutateWithoutRepository(delegatee);
            if (fav.Sign <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(fav), fav, "Fungible asset value must be positive.");
            }

            delegatee.Bond((TSelf)this, fav, height);
            Delegatees = Delegatees.Add(delegatee.Address);
            _repository!.TransferAsset(Address, delegatee.Address, fav);
        }

        public void Undelegate(
            T delegatee, BigInteger share, long height)
        {
            CannotMutateWithoutRepository(delegatee);
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

            UnbondLockIn unbondLockIn = _repository!.GetUnbondLockIn(delegatee, Address);

            if (unbondLockIn.IsFull)
            {
                throw new InvalidOperationException("Undelegation is full.");
            }

            FungibleAssetValue fav = delegatee.Unbond((TSelf)this, share, height);
            unbondLockIn = unbondLockIn.LockIn(
                fav, height, height + delegatee.UnbondingPeriod);

            if (!delegatee.Delegators.Contains(Address))
            {
                Delegatees = Delegatees.Remove(delegatee.Address);
            }

            _repository.SetUnbondLockIn(unbondLockIn);
            _repository.SetUnbondingSet(
                _repository.GetUnbondingSet().SetUnbonding(unbondLockIn));
        }

        public void Redelegate(
            T srcDelegatee, T dstDelegatee, BigInteger share, long height)
        {
            CannotMutateWithoutRepository(srcDelegatee);
            CannotMutateWithoutRepository(dstDelegatee);
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

            FungibleAssetValue fav = srcDelegatee.Unbond(
                (TSelf)this, share, height);
            dstDelegatee.Bond(
                (TSelf)this, fav, height);
            RebondGrace srcRebondGrace = _repository!.GetRebondGrace(srcDelegatee, Address).Grace(
                dstDelegatee.Address,
                fav,
                height,
                height + srcDelegatee.UnbondingPeriod);

            if (!srcDelegatee.Delegators.Contains(Address))
            {
                Delegatees = Delegatees.Remove(srcDelegatee.Address);
            }

            Delegatees = Delegatees.Add(dstDelegatee.Address);

            _repository.SetRebondGrace(srcRebondGrace);
            _repository.SetUnbondingSet(
                _repository.GetUnbondingSet().SetUnbonding(srcRebondGrace));
        }

        public void CancelUndelegate(
            T delegatee, FungibleAssetValue fav, long height)
        {
            CannotMutateWithoutRepository(delegatee);
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

            UnbondLockIn unbondLockIn = _repository!.GetUnbondLockIn(delegatee, Address);

            if (unbondLockIn.IsFull)
            {
                throw new InvalidOperationException("Undelegation is full.");
            }

            delegatee.Bond((TSelf)this, fav, height);
            unbondLockIn = unbondLockIn.Cancel(fav, height);
            Delegatees = Delegatees.Add(delegatee.Address);

            _repository.SetUnbondLockIn(unbondLockIn);
            UnbondingSet unbondingSet = _repository.GetUnbondingSet();
            if (unbondingSet.IsEmpty)
            {
                _repository.SetUnbondingSet(unbondingSet.RemoveUnbonding(unbondLockIn.Address));
            }
        }

        public void ClaimReward(
            T delegatee, long height)
        {
            delegatee.Reward((TSelf)this, height);
        }

        public void UpdateLastRewardHeight(long height)
        {
            LastRewardHeight = height;
        }

        public override bool Equals(object? obj)
            => obj is IDelegator other && Equals(other);

        public bool Equals(IDelegator? other)
            => ReferenceEquals(this, other)
            || (other is Delegator<T, TSelf> delegator
            && GetType() != delegator.GetType()
            && Address.Equals(delegator.Address)
            && Delegatees.SequenceEqual(delegator.Delegatees));

        public override int GetHashCode()
            => Address.GetHashCode();

        private void CannotMutateWithoutRepository(T delegatee)
        {
            CannotMutateWithoutRepository();
            if (!_repository!.Equals(delegatee.Repository))
            {
                throw new InvalidOperationException(
                    "Cannot mutate with different repository.");
            }
        }

        private void CannotMutateWithoutRepository()
        {
            if (_repository is null)
            {
                throw new InvalidOperationException(
                    "Cannot mutate without repository.");
            }
        }
    }
}
