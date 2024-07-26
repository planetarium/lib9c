using System;
using System.Collections.Immutable;
using System.Numerics;
using Libplanet.Crypto;
using Libplanet.Types.Assets;

namespace Nekoyume.Delegation
{
    public abstract class Delegator : IDelegator
    {
        public Delegator(Address address)
        {
            Address = address;
            Delegatees = ImmutableSortedSet<Address>.Empty;
        }

        public Address Address { get; }

        public ImmutableSortedSet<Address> Delegatees { get; private set; }

        public Delegation Delegate(
            IDelegatee<IDelegator> delegatee, FungibleAssetValue fav, Delegation delegation)
        {
            delegation = delegatee.Bond(this, fav, delegation);
            Delegatees = Delegatees.Add(delegatee.Address);

            return delegation;
        }

        public Delegation Undelegate(
            IDelegatee<IDelegator> delegatee, BigInteger share, long height, Delegation delegation)
        {
            if (delegation.UnbondLockIn.IsFull)
            {
                throw new InvalidOperationException("Undelegation is full.");
            }

            delegation = delegatee.Unbond(this, share, delegation);

            if (!(delegation.BondingFAV is FungibleAssetValue bondingFav))
            {
                throw new NullReferenceException("Bonding FAV is null.");
            }

            delegation.DoUnbondLockIn(bondingFav, height, height + delegatee.UnbondingPeriod);

            return delegation;
        }

        public Delegation Redelegate(
            IDelegatee<IDelegator> delegateeFrom,
            IDelegatee<IDelegator> delegateeTo,
            BigInteger share,
            Delegation delegation)
        {
            delegation = delegateeFrom.Unbond(this, share, delegation);

            if (!(delegation.BondingFAV is FungibleAssetValue bondingFav))
            {
                throw new NullReferenceException("Bonding FAV is null.");
            }

            delegation = delegateeTo.Bond(this, bondingFav, delegation);

            delegation.DoRebondGrace(delegateeFrom, delegateeTo, share);

            return delegation;
        }

        public abstract void Claim(IDelegatee<IDelegator> delegatee);

    }
}
