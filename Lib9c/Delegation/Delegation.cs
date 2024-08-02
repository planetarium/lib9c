using System;
using System.Numerics;
using Libplanet.Crypto;
using Libplanet.Types.Assets;

namespace Nekoyume.Delegation
{
    public sealed class Delegation
    {
        private FungibleAssetValue? _netBondedFAV;

        public Delegation(
            Bond bond,
            UnbondLockIn unbondLockIn,
            RebondGrace rebondGrace,
            UnbondingSet unbondingSet,
            FungibleAssetValue? netBondedFAV = null)
        {
            Bond = bond;
            UnbondLockIn = unbondLockIn;
            RebondGrace = rebondGrace;
            UnbondingSet = unbondingSet;
            _netBondedFAV = netBondedFAV;
        }

        public Bond Bond { get; }

        public UnbondLockIn UnbondLockIn { get; }

        public RebondGrace RebondGrace { get; }

        public UnbondingSet UnbondingSet { get; }

        public Delegation AddBond(FungibleAssetValue fav, BigInteger share)
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

            return new Delegation(
                Bond.AddShare(share),
                UnbondLockIn,
                RebondGrace,
                UnbondingSet,
                _netBondedFAV is null ? fav : _netBondedFAV + fav);
        }

        public Delegation CancelBond(FungibleAssetValue fav, BigInteger share)
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

            return new Delegation(
                Bond.SubtractShare(share),
                UnbondLockIn,
                RebondGrace,
                UnbondingSet,
                _netBondedFAV is null ? fav : _netBondedFAV - fav);
        }

        public Delegation DoUnbondLockIn(FungibleAssetValue fav, long height, long expireHeight)
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

            return new Delegation(
                Bond,
                UnbondLockIn.LockIn(fav, height, expireHeight),
                RebondGrace,
                UnbondingSet.AddUnbondLockIn(UnbondLockIn.Address),
                _netBondedFAV);
        }

        public Delegation CancelUnbondLockIn(FungibleAssetValue fav, long height)
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

            var updatedUnbondLockIn = UnbondLockIn.Cancel(fav, height);
            var updatedUnbondingSet = UnbondingSet;

            if (updatedUnbondLockIn.IsEmpty)
            {
                updatedUnbondingSet = UnbondingSet.RemoveUnbondLockIn(UnbondLockIn.Address);
            }

            return new Delegation(
                Bond,
                updatedUnbondLockIn,
                RebondGrace,
                updatedUnbondingSet,
                _netBondedFAV);
        }

        public Delegation DoRebondGrace(Address rebondeeAddress, FungibleAssetValue fav, long height, long expireHeight)
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

            return new Delegation(
                Bond,
                UnbondLockIn,
                RebondGrace.Grace(rebondeeAddress, fav, height, expireHeight),
                UnbondingSet.AddRebondGrace(RebondGrace.Address),
                _netBondedFAV);
        }

        public FungibleAssetValue? FlushReleasedFAV()
            => UnbondLockIn.FlushReleasedFAV();

        public FungibleAssetValue? FlushNetBondedFAV()
        {
            var netBondedFAV = _netBondedFAV;
            _netBondedFAV = null;
            return netBondedFAV;
        }
    }
}
