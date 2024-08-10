#nullable enable
using System;
using System.Collections.Generic;
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
        public Delegator(Address address)
            : this(address, ImmutableSortedSet<Address>.Empty, 0L)
        {
        }

        public Delegator(Address address, IValue bencoded)
            : this(address, (List)bencoded)
        {
        }

        public Delegator(Address address, List bencoded)
            : this(
                address,
                ((List)bencoded[0]).Select(item => new Address(item)).ToImmutableSortedSet(),
                (Integer)bencoded[1])
        {
        }

        private Delegator(
            Address address, ImmutableSortedSet<Address> delegatees, long lastClaimRewardHeight)
        {
            Address = address;
            Delegatees = delegatees;
            LastClaimRewardHeight = lastClaimRewardHeight;
        }

        public Address Address { get; }

        public ImmutableSortedSet<Address> Delegatees { get; private set; }

        public long LastClaimRewardHeight { get; private set; }

        public List Bencoded
            => List.Empty
                .Add(new List(Delegatees.Select(a => a.Bencoded)))
                .Add(LastClaimRewardHeight);

        IValue IBencodable.Bencoded => Bencoded;

        IDelegateResult IDelegator.Delegate(
            IDelegatee delegatee,
            FungibleAssetValue fav,
            long height,
            Bond bond)
            => Delegate((T)delegatee, fav, height, bond);

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

        IClaimRewardResult IDelegator.ClaimReward(
            IDelegatee delegatee,
            IEnumerable<LumpSumRewardsRecord> lumpSumRewardRecords,
            Bond bond,
            long height)
            => ClaimReward(
                (T)delegatee,
                lumpSumRewardRecords,
                bond,
                height);

        public DelegateResult<T> Delegate(
            T delegatee,
            FungibleAssetValue fav,
            long height,
            Bond bond)
        {
            if (fav.Sign <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(fav), fav, "Fungible asset value must be positive.");
            }

            BondResult bondResult = delegatee.Bond((TSelf)this, fav, height, bond);
            Delegatees = Delegatees.Add(delegatee.Address);

            return new DelegateResult<T>(
                delegatee,
                bondResult.Bond,
                fav,
                bondResult.LumpSumRewardsRecord);
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

            UnbondResult unbondResult = delegatee.Unbond((TSelf)this, share, height, bond);
            unbondLockIn = unbondLockIn.LockIn(
                unbondResult.UnbondedFAV, height, height + delegatee.UnbondingPeriod);

            if (unbondResult.Bond.Share.IsZero)
            {
                Delegatees = Delegatees.Remove(delegatee.Address);
            }

            unbondingSet = unbondingSet.SetUnbonding(unbondLockIn);

            return new UndelegateResult<T>(
                delegatee,
                unbondResult.Bond,
                unbondLockIn,
                unbondingSet,
                unbondResult.LumpSumRewardsRecord);
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
                (TSelf)this, share, height, srcBond);
            BondResult dstBondResult = dstDelegatee.Bond(
                (TSelf)this, srcUnbondResult.UnbondedFAV, height, dstBond);
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
            unbondingSet.SetUnbonding(srcRebondGrace);

            return new RedelegateResult<T>(
                srcDelegatee,
                dstDelegatee,
                srcUnbondResult.Bond,
                dstBondResult.Bond,
                srcRebondGrace,
                unbondingSet,
                srcUnbondResult.LumpSumRewardsRecord,
                dstBondResult.LumpSumRewardsRecord);
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

            BondResult bondResult = delegatee.Bond((TSelf)this, fav, height, bond);
            unbondLockIn = unbondLockIn.Cancel(fav, height);
            Delegatees = Delegatees.Add(delegatee.Address);
            unbondingSet = unbondLockIn.IsEmpty
                ? unbondingSet.RemoveUnbonding(unbondLockIn.Address)
                : unbondingSet;

            return new UndelegateResult<T>(
                delegatee,
                bondResult.Bond,
                unbondLockIn,
                unbondingSet,
                bondResult.LumpSumRewardsRecord);
        }

        public ClaimRewardResult<T> ClaimReward(
            T delegatee,
            IEnumerable<LumpSumRewardsRecord> lumpSumRewardRecords,
            Bond bond,
            long height)
        {
            RewardResult rewardResult = delegatee.Reward(
                bond.Share,
                height,
                lumpSumRewardRecords);
            LastClaimRewardHeight = height;
            return new ClaimRewardResult<T>(
                delegatee,
                rewardResult.Reward,
                rewardResult.LumpSumRewardsRecord);
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
    }
}
