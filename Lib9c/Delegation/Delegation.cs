using System;
using System.Numerics;
using Libplanet.Crypto;
using Libplanet.Types.Assets;

namespace Nekoyume.Delegation
{
    public sealed class Delegation
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
            if (fav.Sign <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(fav), fav, "Fungible asset value must be positive.");
            }

            if (share.Sign <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(share), share, "Share must be positive.");
            }

            Bond.AddShare(share);
            IncompleteBond = fav;
        }

        public void CancelBond(FungibleAssetValue fav, BigInteger share)
        {
            if (fav.Sign <= 0)
            {
                throw new ArgumentOutOfRangeException(
                nameof(fav), fav, "Fungible asset value must be positive.");
            }

            if (share.Sign <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(share), share, "Share must be positive.");
            }

            Bond.SubtractShare(share);
            IncompleteUnbond = fav;
        }

        public void DoUnbondLockIn(FungibleAssetValue fav, long height, long expireHeight)
        {
            if (fav.Sign <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(fav), fav, "Fungible asset value must be positive.");
            }

            if (height < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(height), height, "Height must be greater than or equal to zero.");
            }

            if (expireHeight < height)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(expireHeight),
                    expireHeight,
                    "Expire height must be greater than or equal to the height.");
            }

            UnbondLockIn.LockIn(fav, height, expireHeight);
            if (!UnbondingSet.UnbondLockIns.Contains(UnbondLockIn.Address))
            {
                UnbondingSet.AddUnbondLockIn(UnbondLockIn.Address);
            }
        }

        public void CancelUnbondLockIn(FungibleAssetValue fav, long height)
        {
            if (fav.Sign <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(fav), fav, "Fungible asset value must be positive.");
            }

            if (height < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(height), height, "Height must be greater than or equal to zero.");
            }

            UnbondLockIn.Cancel(fav, height);
            if (UnbondLockIn.IsEmpty)
            {
                UnbondingSet.RemoveUnbondLockIn(UnbondLockIn.Address);
            }
        }

        public void DoRebondGrace(Address rebondeeAddress, FungibleAssetValue fav, long height, long expireHeight)
        {
            if (fav.Sign <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(fav), fav, "Fungible asset value must be positive.");
            }

            if (height < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(height), height, "Height must be greater than or equal to zero.");
            }

            RebondGrace.Grace(rebondeeAddress, fav, height, expireHeight);
            if (!UnbondingSet.RebondGraces.Contains(RebondGrace.Address))
            {
                UnbondingSet.AddRebondGrace(RebondGrace.Address);
            }

            if (expireHeight < height)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(expireHeight),
                    expireHeight,
                    "Expire height must be greater than or equal to the height.");
            }
        }

        public void Complete()
        {
            IncompleteBond = null;
            IncompleteUnbond = null;
        }
    }
}
