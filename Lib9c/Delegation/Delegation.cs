using System.Numerics;
using Libplanet.Types.Assets;

namespace Nekoyume.Delegation
{
    public class Delegation
    {
        public Delegation(
            Currency currency,
            Bond bond,
            UnbondLockIn unbondLockIn,
            RebondGrace rebondGrace)
        {
            Bond = bond;
            UnbondLockIn = unbondLockIn;
            RebondGrace = rebondGrace;
        }

        public Currency Currency { get; }

        public Bond Bond { get; set; }

        public UnbondLockIn UnbondLockIn { get; set; }

        public RebondGrace RebondGrace { get; set; }

        public BigInteger? BondingShare { get; set; }

        public FungibleAssetValue? BondingFAV { get; set; }

        public void AddBond(FungibleAssetValue fav, BigInteger share)
        {
            if (!fav.Currency.Equals(Currency))
            {
                throw new BondException();
            }

            if (fav.Sign != 1)
            {
                throw new BondException();
            }

            Bond.AddShare(share);
            BondingShare = share;
            BondingFAV = fav;
        }

        public void CancelBond(FungibleAssetValue fav, BigInteger share)
        {
            if (share.Sign != 1)
            {
                throw new UnbondException();
            }

            if (Bond.Share < share)
            {
                throw new UnbondException();
            }

            Bond.SubtractShare(share);
            BondingShare = -share;
            BondingFAV = -fav;
        }

        public void DoUnbondLockIn(FungibleAssetValue fav, long height, long completionHeight)
        {
            UnbondLockIn.LockIn(fav, height, completionHeight);
        }

        public void CancelUnbondLockIn(FungibleAssetValue fav, long height)
        {
            UnbondLockIn.Cancel(fav, height);
        }

        public void ReleaseUnbondLockIn(long height)
        {
            UnbondLockIn.Release(height);
        }

        public void DoRebondGrace(FungibleAssetValue fav, long height, long completionHeight)
        {
            RebondGrace.Grace(fav, height, completionHeight);
        }

        public void CancelRebondGrace(FungibleAssetValue fav, long height)
        {
            RebondGrace.Cancel(fav, height);
        }

        public void ReleaseRebondGrace(long height)
        {
            RebondGrace.Release(height);
        }
    }
}
