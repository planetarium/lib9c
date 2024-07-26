using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using Bencodex.Types;
using Libplanet.Crypto;
using Libplanet.Types.Assets;

namespace Nekoyume.Delegation
{
    public abstract class Delegatee<T> : IDelegatee<T> where T : IDelegator
    {
        protected readonly byte[] DelegationId = new byte[] { 0x44 };   // `D`
        protected readonly byte[] UndelegationId = new byte[] { 0x55 }; // `U`
        protected readonly byte[] RedelegationId = new byte[] { 0x52 }; // `R`

        public Delegatee(Address address)
        {
            Address = address;
            Delegators = ImmutableSortedSet<Address>.Empty;
            TotalDelegated = Currency * 0;
            TotalShares = BigInteger.Zero;
        }

        public Delegatee(Address address, IValue bencoded)
            : this(address, (List)bencoded)
        {
        }

        public Delegatee(Address address, List bencoded)
            : this(
                  address,
                  ((List)bencoded[0]).Select(item => new Address(item)),
                  new FungibleAssetValue(bencoded[1]),
                  (Integer)bencoded[2])
        {
        }

        private Delegatee(
            Address address,
            IEnumerable<Address> delegators,
            FungibleAssetValue totalDelegated,
            BigInteger totalShares)
        {
            Address = address;
            Delegators = delegators.ToImmutableSortedSet();
            TotalDelegated = totalDelegated;
            TotalShares = totalShares;
        }

        public Address Address { get; }

        public abstract Currency Currency { get; }

        public abstract Address PoolAddress { get; }

        public abstract long UnbondingPeriod { get; }

        public abstract byte[] DelegateeId { get; }

        public ImmutableSortedSet<Address> Delegators { get; private set; }

        public FungibleAssetValue TotalDelegated { get; private set; }

        public BigInteger TotalShares { get; private set; }

        public IValue Bencoded => List.Empty
            .Add(PoolAddress.Bencoded)
            .Add(new List(Delegators.Select(delegator => delegator.Bencoded)))
            .Add(TotalDelegated.Serialize())
            .Add(TotalShares);

        public BigInteger ShareToBond(FungibleAssetValue fav)
            => TotalShares.IsZero
                ? fav.RawValue
                : TotalShares * fav.RawValue / TotalDelegated.RawValue;

        public FungibleAssetValue FAVToUnbond(BigInteger share)
            => TotalShares == share
                ? TotalDelegated
                : (TotalDelegated * share).DivRem(TotalShares, out _);

        public Delegation Bond(T delegator, FungibleAssetValue fav, Delegation delegation)
        {
            BigInteger share = ShareToBond(fav);
            delegation.AddBond(fav, share);
            Delegators = Delegators.Add(delegator.Address);
            TotalShares += share;
            TotalDelegated += fav;
            Distribute();

            return delegation;
        }

        public Delegation Unbond(T delegator, BigInteger share, Delegation delegation)
        {
            if (TotalShares.IsZero || TotalDelegated.RawValue.IsZero)
            {
                throw new UnbondException();
            }

            FungibleAssetValue fav = FAVToUnbond(share);
            delegation.CancelBond(fav, share);
            if (delegation.Bond.Share.IsZero)
            {
                Delegators = Delegators.Remove(delegator.Address);
            }

            TotalShares -= share;
            TotalDelegated -= fav;
            Distribute();

            return delegation;
        }

        public Address DelegationAddress(Address delegatorAddress)
            => DeriveAddress(delegatorAddress, DelegationId);

        public Address UndelegationAddress(Address delegatorAddress)
            => DeriveAddress(delegatorAddress, UndelegationId);

        public Address RedelegationAddress(Address delegatorAddress)
            => DeriveAddress(delegatorAddress, RedelegationId);

        private Address DeriveAddress(Address delegatorAddress, byte[] delegateeId)
        {
            byte[] hashed;
            using (var hmac = new HMACSHA1(DelegateeId.Concat(delegateeId).ToArray()))
            {
                hashed = hmac.ComputeHash(
                    Address.ByteArray.Concat(delegatorAddress.ByteArray).ToArray());
            }

            return new Address(hashed);
        }

        // TODO: Add a method to claim rewards.
    }
}
