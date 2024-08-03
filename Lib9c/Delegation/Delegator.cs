using System;
using System.Collections.Immutable;
using System.Linq;
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
        public Delegator(Address address)
        {
            Address = address;
            Delegatees = ImmutableSortedSet<Address>.Empty;
        }

        public Delegator(Address address, IValue bencoded)
            : this(address, (List)bencoded)
        {
        }

        private Delegator(Address address, List bencoded)
        {
            Address = address;
            Delegatees = bencoded.Select(item => new Address(item)).ToImmutableSortedSet();
        }

        public Address Address { get; }

        public ImmutableSortedSet<Address> Delegatees { get; private set; }

        public IValue Bencoded
            => new List(Delegatees.Select(a => a.Bencoded));

        IDelegateResult IDelegator.Delegate(
            IDelegatee delegatee,
            FungibleAssetValue fav,
            Bond bond)
            => Delegate((T)delegatee, fav, bond);

        IUndelegateResult IDelegator.Undelegate(
            IDelegatee delegatee,
            BigInteger share,
            long height,
            Bond bond,
            UnbondLockIn unbondLockIn,
            UnbondingSet unbondingSet)
            => Undelegate(
                (T)delegatee,
                share,
                height,
                bond,
                unbondLockIn,
                unbondingSet);

        IRedelegateResult IDelegator.Redelegate(
            IDelegatee srcDelegatee,
            IDelegatee dstDelegatee,
            BigInteger share,
            long height,
            Bond srcBond,
            Bond dstBond,
            RebondGrace srcRebondGrace,
            UnbondingSet unbondingSet)
            => Redelegate(
                (T)srcDelegatee,
                (T)dstDelegatee,
                share,
                height,
                srcBond,
                dstBond,
                srcRebondGrace,
                unbondingSet);

        public DelegateResult<T> Delegate(
            T delegatee,
            FungibleAssetValue fav,
            Bond bond)
        {
            if (fav.Sign <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(fav), fav, "Fungible asset value must be positive.");
            }

            BondResult bondResult = delegatee.Bond((TSelf)this, fav, bond);
            Delegatees = Delegatees.Add(delegatee.Address);

            return new DelegateResult<T>(delegatee, bondResult.Bond, fav);
        }

        public UndelegateResult<T> Undelegate(
            T delegatee,
            BigInteger share,
            long height,
            Bond bond,
            UnbondLockIn unbondLockIn,
            UnbondingSet unbondingSet)
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

            if (unbondLockIn.IsFull)
            {
                throw new InvalidOperationException("Undelegation is full.");
            }

            UnbondResult unbondResult = delegatee.Unbond((TSelf)this, share, bond);
            unbondLockIn = unbondLockIn.LockIn(
                unbondResult.UnbondedFAV, height, height + delegatee.UnbondingPeriod);

            if (unbondResult.Bond.Share.IsZero)
            {
                Delegatees = Delegatees.Remove(delegatee.Address);
            }

            unbondingSet = unbondingSet.AddUnbondLockIn(unbondLockIn.Address);

            return new UndelegateResult<T>(
                delegatee,
                unbondResult.Bond,
                unbondLockIn,
                unbondingSet);
        }

        public RedelegateResult<T> Redelegate(
            T srcDelegatee,
            T dstDelegatee,
            BigInteger share,
            long height,
            Bond srcBond,
            Bond dstBond,
            RebondGrace srcRebondGrace,
            UnbondingSet unbondingSet)
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

            UnbondResult srcUnbondResult = srcDelegatee.Unbond(
                (TSelf)this, share, srcBond);
            BondResult dstBondResult = dstDelegatee.Bond(
                (TSelf)this, srcUnbondResult.UnbondedFAV, dstBond);
            srcRebondGrace = srcRebondGrace.Grace(
                dstDelegatee.Address,
                srcUnbondResult.UnbondedFAV,
                height,
                height + srcDelegatee.UnbondingPeriod);

            if (srcUnbondResult.Bond.Share.IsZero)
            {
                Delegatees = Delegatees.Remove(srcDelegatee.Address);
            }

            Delegatees = Delegatees.Add(dstDelegatee.Address);
            unbondingSet.AddRebondGrace(srcRebondGrace.Address);

            return new RedelegateResult<T>(
                srcDelegatee,
                dstDelegatee,
                srcUnbondResult.Bond,
                dstBondResult.Bond,
                srcRebondGrace,
                unbondingSet);
        }

        public UndelegateResult<T> CancelUndelegate(
            T delegatee,
            FungibleAssetValue fav,
            long height,
            Bond bond,
            UnbondLockIn unbondLockIn,
            UnbondingSet unbondingSet)
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

            if (unbondLockIn.IsFull)
            {
                throw new InvalidOperationException("Undelegation is full.");
            }

            BondResult bondResult = delegatee.Bond((TSelf)this, fav, bond);
            unbondLockIn = unbondLockIn.Cancel(fav, height);
            Delegatees = Delegatees.Add(delegatee.Address);
            unbondingSet = unbondLockIn.IsEmpty
                ? unbondingSet.RemoveUnbondLockIn(unbondLockIn.Address)
                : unbondingSet;

            return new UndelegateResult<T>(
                delegatee,
                bondResult.Bond,
                unbondLockIn,
                unbondingSet);
        }

        public void Claim(IDelegatee delegatee)
        {
            // TODO: Implement this
        }

        public override bool Equals(object obj)
            => obj is IDelegator other && Equals(other);

        public bool Equals(IDelegator other)
            => ReferenceEquals(this, other)
            || (other is Delegator<T, TSelf> delegator
            && GetType() != delegator.GetType()
            && Address.Equals(delegator.Address)
            && Delegatees.SequenceEqual(delegator.Delegatees));

        public override int GetHashCode()
            => Address.GetHashCode();
    }
}
