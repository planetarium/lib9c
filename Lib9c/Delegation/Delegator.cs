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

        public void Delegate(IDelegatee delegatee, FungibleAssetValue fav, Delegation delegation)
            => Delegate((T)delegatee, fav, delegation);

        public void Undelegate(IDelegatee delegatee, BigInteger share, long height, Delegation delegation)
            => Undelegate((T)delegatee, share, height, delegation);

        public void Redelegate(
            IDelegatee srcDelegatee,
            IDelegatee dstDelegatee,
            BigInteger share,
            long height,
            Delegation srcDelegation,
            Delegation dstDelegation)
            => Redelegate((T)srcDelegatee, (T)dstDelegatee, share, height, srcDelegation, dstDelegation);

        public void Claim(IDelegatee delegatee)
        {
            // TODO: Implement this
        }

        private void Delegate(
            T delegatee,
            FungibleAssetValue fav,
            Delegation delegation)
        {
            delegatee.Bond((TSelf)this, fav, delegation);
            Delegatees = Delegatees.Add(delegatee.Address);
        }

        private void Undelegate(
            T delegatee,
            BigInteger share,
            long height,
            Delegation delegation)
        {
            if (delegation.UnbondLockIn.IsFull)
            {
                throw new InvalidOperationException("Undelegation is full.");
            }

            delegatee.Unbond(this, share, delegation);

            if (!(delegation.IncompleteUnbond is FungibleAssetValue unbondToLockIn))
            {
                throw new NullReferenceException("Bonding FAV is null.");
            }

            delegation.DoUnbondLockIn(unbondToLockIn, height, height + delegatee.UnbondingPeriod);

            if (delegation.Bond.Share.IsZero)
            {
                Delegatees = Delegatees.Remove(delegatee.Address);
            }

            delegation.Complete();
        }

        private void Redelegate(
            T srcDelegatee,
            T dstDelegatee,
            BigInteger share,
            long height,
            Delegation srcDelegation,
            Delegation dstDelegation)
        {
            srcDelegatee.Unbond(this, share, srcDelegation);

            if (!(srcDelegation.IncompleteUnbond is FungibleAssetValue unbondToGrace))
            {
                throw new NullReferenceException("Bonding FAV is null.");
            }

            dstDelegatee.Bond(this, unbondToGrace, dstDelegation);

            srcDelegation.DoRebondGrace(dstDelegatee.Address, unbondToGrace, height, height + srcDelegatee.UnbondingPeriod);

            if (srcDelegation.Bond.Share.IsZero)
            {
                Delegatees = Delegatees.Remove(srcDelegatee.Address);
            }

            Delegatees = Delegatees.Add(dstDelegatee.Address);

            srcDelegation.Complete();
        }
    }
}
