using System;
using System.Numerics;
using Libplanet.Crypto;
using Libplanet.Types.Assets;

namespace Nekoyume.Delegation
{
    public class Delegation
    {
        public Delegation(
            Bond bond = null,
            UnbondLockIn unbondLockIn = null,
            RebondGrace rebondGrace = null,
            UnbondingSet unbondingSet = null)
        {
            Bond = bond;
            UnbondLockIn = unbondLockIn;
            RebondGrace = rebondGrace;
            UnbondingSet = unbondingSet;
        }

        public Bond Bond { get; }

        public UnbondLockIn UnbondLockIn { get; }

        public RebondGrace RebondGrace { get; }

        public UnbondingSet UnbondingSet { get; }

        public FungibleAssetValue? IncompleteBond { get; private set; }

        public FungibleAssetValue? IncompleteUnbond { get; private set; }

        public void AddBond(FungibleAssetValue fav, BigInteger share)
        {
            if (fav.Sign != 1)
            {
                throw new InvalidOperationException("Cannot bond negative FAV.");
            }

            Bond.AddShare(share);
            IncompleteBond = fav;
        }

        public void CancelBond(FungibleAssetValue fav, BigInteger share)
        {
            if (share.Sign != 1)
            {
                throw new InvalidOperationException("Cannot unbond negative FAV.");
            }

            if (Bond.Share < share)
            {
                throw new InvalidOperationException("Cannot unbond more than bonded.");
            }

            Bond.SubtractShare(share);
            IncompleteUnbond = fav;
        }

        public void DoUnbondLockIn(FungibleAssetValue fav, long height, long completionHeight)
        {
            UnbondLockIn.LockIn(fav, height, completionHeight);
            if (!UnbondingSet.UnbondLockIns.Contains(UnbondLockIn.Address))
            {
                UnbondingSet.AddUnbondLockIn(UnbondLockIn.Address);
            }
        }

        public void CancelUnbondLockIn(FungibleAssetValue fav, long height)
        {
            UnbondLockIn.Cancel(fav, height);
            if (UnbondLockIn.IsEmpty)
            {
                UnbondingSet.RemoveUnbondLockIn(UnbondLockIn.Address);
            }
        }

        public void DoRebondGrace(Address rebondeeAddress, FungibleAssetValue fav, long height, long completionHeight)
        {
            RebondGrace.Grace(rebondeeAddress, fav, height, completionHeight);
            if (!UnbondingSet.RebondGraces.Contains(RebondGrace.Address))
            {
                UnbondingSet.AddRebondGrace(RebondGrace.Address);
            }        
        }

        public void Complete()
        {
            IncompleteBond = null;
            IncompleteUnbond = null;
        }
    }
}
