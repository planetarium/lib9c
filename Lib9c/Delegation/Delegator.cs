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

        void IDelegator.Delegate(IDelegatee delegatee, FungibleAssetValue fav, Delegation delegation)
            => Delegate((T)delegatee, fav, delegation);

        void IDelegator.Undelegate(IDelegatee delegatee, BigInteger share, long height, Delegation delegation)
            => Undelegate((T)delegatee, share, height, delegation);

        void IDelegator.Redelegate(
            IDelegatee srcDelegatee,
            IDelegatee dstDelegatee,
            BigInteger share,
            long height,
            Delegation srcDelegation,
            Delegation dstDelegation)
            => Redelegate((T)srcDelegatee, (T)dstDelegatee, share, height, srcDelegation, dstDelegation);

        public void Delegate(
            T delegatee,
            FungibleAssetValue fav,
            Delegation delegation)
        {
            if (fav.Sign <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(fav), fav, "Fungible asset value must be positive.");
            }

            delegatee.Bond((TSelf)this, fav, delegation);
            Delegatees = Delegatees.Add(delegatee.Address);
        }

        public void Undelegate(
            T delegatee,
            BigInteger share,
            long height,
            Delegation delegation)
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

            if (delegation.UnbondLockIn.IsFull)
            {
                throw new InvalidOperationException("Undelegation is full.");
            }

            delegatee.Unbond((TSelf)this, share, delegation);

            if (!(delegation.FlushNetBondedFAV() is FungibleAssetValue netBondedFAV))
            {
                throw new NullReferenceException("Net bonded FAV is null.");
            }

            if (netBondedFAV.Sign >= 0)
            {
                throw new InvalidOperationException("Net bonded FAV must be negative.");
            }

            delegation.DoUnbondLockIn(-netBondedFAV, height, height + delegatee.UnbondingPeriod);

            if (delegation.Bond.Share.IsZero)
            {
                Delegatees = Delegatees.Remove(delegatee.Address);
            }
        }

        public void Redelegate(
            T srcDelegatee,
            T dstDelegatee,
            BigInteger share,
            long height,
            Delegation srcDelegation,
            Delegation dstDelegation)
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

            srcDelegatee.Unbond((TSelf)this, share, srcDelegation);

            if (!(srcDelegation.FlushNetBondedFAV() is FungibleAssetValue netBondedFAV))
            {
                throw new NullReferenceException("Net bonded FAV is null.");
            }

            if (netBondedFAV.Sign >= 0)
            {
                throw new InvalidOperationException("Net bonded FAV must be negative.");
            }

            dstDelegatee.Bond((TSelf)this, -netBondedFAV, dstDelegation);

            srcDelegation.DoRebondGrace(dstDelegatee.Address, -netBondedFAV, height, height + srcDelegatee.UnbondingPeriod);

            if (srcDelegation.Bond.Share.IsZero)
            {
                Delegatees = Delegatees.Remove(srcDelegatee.Address);
            }

            Delegatees = Delegatees.Add(dstDelegatee.Address);
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
