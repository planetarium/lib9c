#nullable enable
using System;
using System.Collections.Immutable;
using System.Linq;
using Bencodex;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Action;

namespace Nekoyume.Delegation
{
    public abstract class DelegationRepository<TRepository, TDelegatee, TDelegator>
        where TRepository : DelegationRepository<TRepository, TDelegatee, TDelegator>
        where TDelegatee : Delegatee<TRepository, TDelegatee, TDelegator>
        where TDelegator : Delegator<TRepository, TDelegatee, TDelegator>
    {
        protected IWorld previousWorld;
        protected IAccount delegateeAccount;
        protected IAccount delegatorAccount;
        protected IAccount delegateeMetadataAccount;
        protected IAccount delegatorMetadataAccount;
        protected IAccount bondAccount;
        protected IAccount unbondLockInAccount;
        protected IAccount rebondGraceAccount;
        protected IAccount unbondingSetAccount;
        protected IAccount rewardBaseAccount;
        // TODO: [Migration] Remove this field after migration.
        protected IAccount lumpSumRewardsRecordAccount;

        public DelegationRepository(
            IWorld world,
            IActionContext actionContext,
            Address delegateeAccountAddress,
            Address delegatorAccountAddress,
            Address delegateeMetadataAccountAddress,
            Address delegatorMetadataAccountAddress,
            Address bondAccountAddress,
            Address unbondLockInAccountAddress,
            Address rebondGraceAccountAddress,
            Address unbondingSetAccountAddress,
            Address rewardBaseAccountAddress,
            Address lumpSumRewardRecordAccountAddress)
        {
            previousWorld = world;
            ActionContext = actionContext;
            DelegateeAccountAddress = delegateeAccountAddress;
            DelegatorAccountAddress = delegatorAccountAddress;
            DelegateeMetadataAccountAddress = delegateeMetadataAccountAddress;
            DelegatorMetadataAccountAddress = delegatorMetadataAccountAddress;
            BondAccountAddress = bondAccountAddress;
            UnbondLockInAccountAddress = unbondLockInAccountAddress;
            RebondGraceAccountAddress = rebondGraceAccountAddress;
            UnbondingSetAccountAddress = unbondingSetAccountAddress;
            RewardBaseAccountAddress = rewardBaseAccountAddress;
            LumpSumRewardsRecordAccountAddress = lumpSumRewardRecordAccountAddress;

            delegateeAccount = world.GetAccount(DelegateeAccountAddress);
            delegatorAccount = world.GetAccount(DelegatorAccountAddress);
            delegateeMetadataAccount = world.GetAccount(DelegateeMetadataAccountAddress);
            delegatorMetadataAccount = world.GetAccount(DelegatorMetadataAccountAddress);
            bondAccount = world.GetAccount(BondAccountAddress);
            unbondLockInAccount = world.GetAccount(UnbondLockInAccountAddress);
            rebondGraceAccount = world.GetAccount(RebondGraceAccountAddress);
            unbondingSetAccount = world.GetAccount(UnbondingSetAccountAddress);
            rewardBaseAccount = world.GetAccount(RewardBaseAccountAddress);
            lumpSumRewardsRecordAccount = world.GetAccount(LumpSumRewardsRecordAccountAddress);
        }

        public virtual IWorld World => previousWorld
            .SetAccount(DelegateeAccountAddress, delegateeAccount)
            .SetAccount(DelegatorAccountAddress, delegatorAccount)
            .SetAccount(DelegateeMetadataAccountAddress, delegateeMetadataAccount)
            .SetAccount(DelegatorMetadataAccountAddress, delegatorMetadataAccount)
            .SetAccount(BondAccountAddress, bondAccount)
            .SetAccount(UnbondLockInAccountAddress, unbondLockInAccount)
            .SetAccount(RebondGraceAccountAddress, rebondGraceAccount)
            .SetAccount(UnbondingSetAccountAddress, unbondingSetAccount)
            .SetAccount(RewardBaseAccountAddress, rewardBaseAccount)
            .SetAccount(LumpSumRewardsRecordAccountAddress, lumpSumRewardsRecordAccount);

        /// <summary>
        /// <see cref="IActionContext"/> of the current action.
        /// </summary>
        public IActionContext ActionContext { get; }

        /// <summary>
        /// <see cref="Address"> of the <see cref="Delegatee{T, TSelf}"> account.
        /// </summary>
        public Address DelegateeAccountAddress { get; }

        /// <summary>
        /// <see cref="Address"> of the <see cref="Delegator{T, TSelf}"> account.
        /// </summary>
        public Address DelegatorAccountAddress { get; }

        /// <summary>
        /// <see cref="Address"> of the <see cref="DelegateeMetadata"/> account.
        /// </summary>
        public Address DelegateeMetadataAccountAddress { get; }

        /// <summary>
        /// <see cref="Address"> of the <see cref="DelegatorMetadata"> account.
        /// </summary>
        public Address DelegatorMetadataAccountAddress { get; }

        /// <summary>
        /// <see cref="Address"/> of the <see cref="Bond"/> account.
        /// </summary>
        public Address BondAccountAddress { get; }

        /// <summary>
        /// <see cref="Address"/> of the <see cref="UnbondLockIn"/> account.
        /// </summary>
        public Address UnbondLockInAccountAddress { get; }

        /// <summary>
        /// <see cref="Address"/> of the <see cref="RebondGrace"/> account
        /// </summary>
        public Address RebondGraceAccountAddress { get; }

        /// <summary>
        /// <see cref="Address"/> of the <see cref="UnbondingSet"/> account.
        /// </summary>
        public Address UnbondingSetAccountAddress { get; }

        /// <summary>
        /// <see cref="Address"/> of the <see cref="RewardBase"/> account.
        /// </summary>
        public Address RewardBaseAccountAddress { get; }

        /// <summary>
        /// <see cref="Address"/> of the <see cref="LumpSumRewardsRecord"/> account.
        /// </summary>
        public Address LumpSumRewardsRecordAccountAddress { get; }

        public abstract TDelegatee GetDelegatee(Address address);

        public abstract TDelegator GetDelegator(Address address);

        public void SetDelegatee(TDelegatee delegatee)
        {
            if (delegatee is IBencodable bencodable)
            {
                delegateeAccount = delegateeAccount.SetState(delegatee.Address, bencodable.Bencoded);
            }

            SetDelegateeMetadata(delegatee.Metadata);
        }

        public void SetDelegator(TDelegator delegator)
        {
            if (delegator is IBencodable bencodable)
            {
                delegatorAccount = delegatorAccount.SetState(delegator.Address, bencodable.Bencoded);
            }

            SetDelegatorMetadata(delegator.Metadata);
        }

        public DelegateeMetadata GetDelegateeMetadata(Address delegateeAddress)
        {
            IValue? value = delegateeMetadataAccount.GetState(
                DelegationAddress.DelegateeMetadataAddress(delegateeAddress, DelegateeAccountAddress));
            return value is IValue bencoded
                ? new DelegateeMetadata(delegateeAddress, DelegateeAccountAddress, bencoded)
                : throw new FailedLoadStateException("DelegateeMetadata not found.");
        }

        public DelegatorMetadata GetDelegatorMetadata(Address delegatorAddress)
        {
            IValue? value = delegatorMetadataAccount.GetState(
                DelegationAddress.DelegatorMetadataAddress(delegatorAddress, DelegatorAccountAddress));
            return value is IValue bencoded
                ? new DelegatorMetadata(delegatorAddress, DelegatorAccountAddress, bencoded)
                : throw new FailedLoadStateException("DelegatorMetadata not found.");
        }

        public Bond GetBond(TDelegatee delegatee, Address delegatorAddress)
        {
            var delegateeMetadata = delegatee.Metadata;
            var bondAddress = delegateeMetadata.BondAddress(delegatorAddress);
            return bondAccount.GetState(bondAddress) is { } bencoded
                ? new Bond(bondAddress, bencoded) : new Bond(bondAddress);
        }

        public UnbondLockIn GetUnbondLockIn(TDelegatee delegatee, Address delegatorAddress)
        {
            var delegateeMetadata = delegatee.Metadata;
            var maxUnbondLockInEntries = delegateeMetadata.MaxUnbondLockInEntries;
            Address address = delegateeMetadata.UnbondLockInAddress(delegatorAddress);
            IValue? value = unbondLockInAccount.GetState(address);
            return value is IValue bencoded
                ? new UnbondLockIn(address, maxUnbondLockInEntries, bencoded)
                : new UnbondLockIn(address, maxUnbondLockInEntries, delegatee.Address, delegatorAddress);
        }

        public UnbondLockIn GetUnlimitedUnbondLockIn(Address address)
        {
            IValue? value = unbondLockInAccount.GetState(address);
            return value is IValue bencoded
                ? new UnbondLockIn(address, int.MaxValue, bencoded)
                : throw new FailedLoadStateException("UnbondLockIn not found.");
        }

        public RebondGrace GetRebondGrace(TDelegatee delegatee, Address delegatorAddress)
        {
            var delegateeMetadata = delegatee.Metadata;
            Address address = delegateeMetadata.RebondGraceAddress(delegatorAddress);
            IValue? value = rebondGraceAccount.GetState(address);
            var maxRebondGraceEntries = delegateeMetadata.MaxRebondGraceEntries;
            return value is IValue bencoded
                ? new RebondGrace(address, maxRebondGraceEntries, bencoded)
                : new RebondGrace(address, maxRebondGraceEntries, delegatee.Address, delegatorAddress);
        }

        public RebondGrace GetUnlimitedRebondGrace(Address address)
        {
            IValue? value = rebondGraceAccount.GetState(address);
            return value is IValue bencoded
                ? new RebondGrace(address, int.MaxValue, bencoded)
                : throw new FailedLoadStateException("RebondGrace not found.");
        }

        /// <inheritdoc/>
        public RewardBase? GetCurrentRewardBase(TDelegatee delegatee)
        {
            var delegateeMetadata = delegatee.Metadata;
            Address address = delegateeMetadata.CurrentRewardBaseAddress();
            IValue? value = rewardBaseAccount.GetState(address);
            return value is IValue bencoded
                ? new RewardBase(address, bencoded)
                : null;
        }

        /// <inheritdoc/>
        public RewardBase? GetRewardBase(TDelegatee delegatee, long height)
        {
            var delegateeMetadata = delegatee.Metadata;
            Address address = delegateeMetadata.RewardBaseAddress(height);
            IValue? value = rewardBaseAccount.GetState(address);
            return value is IValue bencoded
                ? new RewardBase(address, bencoded)
                : null;
        }

        public LumpSumRewardsRecord? GetLumpSumRewardsRecord(TDelegatee delegatee, long height)
        {
            var delegateeMetadata = delegatee.Metadata;
            Address address = delegateeMetadata.LumpSumRewardsRecordAddress(height);
            IValue? value = lumpSumRewardsRecordAccount.GetState(address);
            return value is IValue bencoded
                ? new LumpSumRewardsRecord(address, bencoded)
                : null;
        }

        public LumpSumRewardsRecord? GetCurrentLumpSumRewardsRecord(TDelegatee delegatee)
        {
            var delegateeMetadata = delegatee.Metadata;
            Address address = delegateeMetadata.CurrentLumpSumRewardsRecordAddress();
            IValue? value = lumpSumRewardsRecordAccount.GetState(address);
            return value is IValue bencoded
                ? new LumpSumRewardsRecord(address, bencoded)
                : null;
        }

        public FungibleAssetValue GetBalance(Address address, Currency currency)
            => previousWorld.GetBalance(address, currency);

        private void SetDelegateeMetadata(DelegateeMetadata delegateeMetadata)
        {
            delegateeMetadataAccount
                = delegateeMetadataAccount.SetState(
                    delegateeMetadata.Address, delegateeMetadata.Bencoded);
        }

        private void SetDelegatorMetadata(DelegatorMetadata delegatorMetadata)
        {
            delegatorMetadataAccount
                = delegatorMetadataAccount.SetState(
                    delegatorMetadata.Address, delegatorMetadata.Bencoded);
        }

        public void SetBond(Bond bond)
        {
            bondAccount = bond.IsEmpty
                ? bondAccount.RemoveState(bond.Address)
                : bondAccount.SetState(bond.Address, bond.Bencoded);
        }

        private void SetUnbondLockIn(UnbondLockIn unbondLockIn)
        {
            unbondLockInAccount = unbondLockIn.IsEmpty
                ? unbondLockInAccount.RemoveState(unbondLockIn.Address)
                : unbondLockInAccount.SetState(unbondLockIn.Address, unbondLockIn.Bencoded);
        }

        private void SetRebondGrace(RebondGrace rebondGrace)
        {
            rebondGraceAccount = rebondGrace.IsEmpty
                ? rebondGraceAccount.RemoveState(rebondGrace.Address)
                : rebondGraceAccount.SetState(rebondGrace.Address, rebondGrace.Bencoded);
        }

        /// <inheritdoc/>
        public void SetRewardBase(RewardBase rewardBase)
        {
            rewardBaseAccount = rewardBaseAccount.SetState(rewardBase.Address, rewardBase.Bencoded);
        }

        public void SetLumpSumRewardsRecord(LumpSumRewardsRecord lumpSumRewardsRecord)
        {
            lumpSumRewardsRecordAccount = lumpSumRewardsRecordAccount.SetState(
                lumpSumRewardsRecord.Address, lumpSumRewardsRecord.Bencoded);
        }

        /// <inheritdoc/>
        public void RemoveLumpSumRewardsRecord(LumpSumRewardsRecord lumpSumRewardsRecord)
        {
            lumpSumRewardsRecordAccount = lumpSumRewardsRecordAccount.RemoveState(lumpSumRewardsRecord.Address);
        }

        public void TransferAsset(Address sender, Address recipient, FungibleAssetValue value)
            => previousWorld = previousWorld.TransferAsset(ActionContext, sender, recipient, value);

        public virtual void UpdateWorld(IWorld world)
        {
            previousWorld = world;
            delegateeAccount = world.GetAccount(DelegateeAccountAddress);
            delegatorAccount = world.GetAccount(DelegatorAccountAddress);
            delegateeMetadataAccount = world.GetAccount(DelegateeMetadataAccountAddress);
            delegatorMetadataAccount = world.GetAccount(DelegatorMetadataAccountAddress);
            bondAccount = world.GetAccount(BondAccountAddress);
            unbondLockInAccount = world.GetAccount(UnbondLockInAccountAddress);
            rebondGraceAccount = world.GetAccount(RebondGraceAccountAddress);
            unbondingSetAccount = world.GetAccount(UnbondingSetAccountAddress);
            rewardBaseAccount = world.GetAccount(RewardBaseAccountAddress);
            lumpSumRewardsRecordAccount = world.GetAccount(LumpSumRewardsRecordAccountAddress);
        }

        public void Release(UnbondLockIn unbondLockIn, FungibleAssetValue? releasedFAV)
        {
            if (releasedFAV.HasValue)
            {
                var delegateeMetadata = GetDelegateeMetadata(unbondLockIn.DelegateeAddress);
                var delegatorMetadata = GetDelegatorMetadata(unbondLockIn.DelegatorAddress);
                TransferAsset(
                    delegateeMetadata.DelegationPoolAddress,
                    delegatorMetadata.DelegationPoolAddress,
                    releasedFAV.Value);
            }
        }

        internal IUnbonding GetUnbonding(UnbondingRef reference) => reference.UnbondingType switch
        {
            UnbondingType.UnbondLockIn => GetUnlimitedUnbondLockIn(reference.Address),
            UnbondingType.RebondGrace => GetUnlimitedRebondGrace(reference.Address),
            _ => throw new ArgumentException("Invalid unbonding type.")
        };

        internal void SetUnbonding(IUnbonding unbonding)
        {
            switch (unbonding)
            {
                case UnbondLockIn unbondLockIn:
                    SetUnbondLockIn(unbondLockIn);
                    break;
                case RebondGrace rebondGrace:
                    SetRebondGrace(rebondGrace);
                    break;
                default:
                    throw new InvalidOperationException("Invalid unbonding type.");
            }
        }
    }
}
