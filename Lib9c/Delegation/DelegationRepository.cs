#nullable enable
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Action;

namespace Nekoyume.Delegation
{
    public abstract class DelegationRepository : IDelegationRepository
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

        public IActionContext ActionContext { get; }

        public Address DelegateeAccountAddress { get; }

        public Address DelegatorAccountAddress { get; }

        private Address DelegateeMetadataAccountAddress { get; }

        private Address DelegatorMetadataAccountAddress { get; }

        private Address BondAccountAddress { get; }

        private Address UnbondLockInAccountAddress { get; }

        private Address RebondGraceAccountAddress { get; }

        private Address UnbondingSetAccountAddress { get; }

        private Address RewardBaseAccountAddress { get; }

        private Address LumpSumRewardsRecordAccountAddress { get; }

        public abstract IDelegatee GetDelegatee(Address address);

        public abstract IDelegator GetDelegator(Address address);

        public abstract void SetDelegatee(IDelegatee delegatee);

        public abstract void SetDelegator(IDelegator delegator);

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

        public Bond GetBond(IDelegatee delegatee, Address delegatorAddress)
        {
            Address address = delegatee.BondAddress(delegatorAddress);
            IValue? value = bondAccount.GetState(address);
            return value is IValue bencoded
                ? new Bond(address, bencoded)
                : new Bond(address);
        }

        public UnbondLockIn GetUnbondLockIn(IDelegatee delegatee, Address delegatorAddress)
        {
            Address address = delegatee.UnbondLockInAddress(delegatorAddress);
            IValue? value = unbondLockInAccount.GetState(address);
            return value is IValue bencoded
                ? new UnbondLockIn(address, delegatee.MaxUnbondLockInEntries, bencoded, this)
                : new UnbondLockIn(address, delegatee.MaxUnbondLockInEntries, delegatee.Address, delegatorAddress, this);
        }

        public UnbondLockIn GetUnlimitedUnbondLockIn(Address address)
        {
            IValue? value = unbondLockInAccount.GetState(address);
            return value is IValue bencoded
                ? new UnbondLockIn(address, int.MaxValue, bencoded, this)
                : throw new FailedLoadStateException("UnbondLockIn not found.");
        }

        public RebondGrace GetRebondGrace(IDelegatee delegatee, Address delegatorAddress)
        {
            Address address = delegatee.RebondGraceAddress(delegatorAddress);
            IValue? value = rebondGraceAccount.GetState(address);
            return value is IValue bencoded
                ? new RebondGrace(address, delegatee.MaxRebondGraceEntries, bencoded, this)
                : new RebondGrace(address, delegatee.MaxRebondGraceEntries, this);
        }

        public RebondGrace GetUnlimitedRebondGrace(Address address)
        {
            IValue? value = rebondGraceAccount.GetState(address);
            return value is IValue bencoded
                ? new RebondGrace(address, int.MaxValue, bencoded, this)
                : throw new FailedLoadStateException("RebondGrace not found.");
        }

        public UnbondingSet GetUnbondingSet()
            => unbondingSetAccount.GetState(UnbondingSet.Address) is IValue bencoded
                ? new UnbondingSet(bencoded, this)
                : new UnbondingSet(this);

        public RewardBase GetCurrentRewardBase(IDelegatee delegatee)
        {
            Address address = delegatee.CurrentRewardBaseAddress();
            IValue? value = rewardBaseAccount.GetState(address);
            return value is IValue bencoded
                ? new RewardBase(address, bencoded)
                : throw new FailedLoadStateException("RewardBase not found.");
        }

        public RewardBase GetRewardBase(IDelegatee delegatee, long height)
        {
            Address address = delegatee.RewardBaseAddress(height);
            IValue? value = rewardBaseAccount.GetState(address);
            return value is IValue bencoded
                ? new RewardBase(address, bencoded)
                : throw new FailedLoadStateException("RewardBase not found.");
        }

        public LumpSumRewardsRecord? GetLumpSumRewardsRecord(IDelegatee delegatee, long height)
        {
            Address address = delegatee.LumpSumRewardsRecordAddress(height);
            IValue? value = lumpSumRewardsRecordAccount.GetState(address);
            return value is IValue bencoded
                ? new LumpSumRewardsRecord(address, bencoded)
                : null;
        }

        public LumpSumRewardsRecord? GetCurrentLumpSumRewardsRecord(IDelegatee delegatee)
        {
            Address address = delegatee.CurrentLumpSumRewardsRecordAddress();
            IValue? value = lumpSumRewardsRecordAccount.GetState(address);
            return value is IValue bencoded
                ? new LumpSumRewardsRecord(address, bencoded)
                : null;
        }

        public FungibleAssetValue GetBalance(Address address, Currency currency)
            => previousWorld.GetBalance(address, currency);

        public void SetDelegateeMetadata(DelegateeMetadata delegateeMetadata)
        {
            delegateeMetadataAccount
                = delegateeMetadataAccount.SetState(
                    delegateeMetadata.Address, delegateeMetadata.Bencoded);
        }

        public void SetDelegatorMetadata(DelegatorMetadata delegatorMetadata)
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

        public void SetUnbondLockIn(UnbondLockIn unbondLockIn)
        {
            unbondLockInAccount = unbondLockIn.IsEmpty
                ? unbondLockInAccount.RemoveState(unbondLockIn.Address)
                : unbondLockInAccount.SetState(unbondLockIn.Address, unbondLockIn.Bencoded);
        }

        public void SetRebondGrace(RebondGrace rebondGrace)
        {
            rebondGraceAccount = rebondGrace.IsEmpty
                ? rebondGraceAccount.RemoveState(rebondGrace.Address)
                : rebondGraceAccount.SetState(rebondGrace.Address, rebondGrace.Bencoded);
        }

        public void SetUnbondingSet(UnbondingSet unbondingSet)
        {
            unbondingSetAccount = unbondingSet.IsEmpty
                ? unbondingSetAccount.RemoveState(UnbondingSet.Address)
                : unbondingSetAccount.SetState(UnbondingSet.Address, unbondingSet.Bencoded);
        }

        public void SetRewardBase(RewardBase rewardBase)
        {
            rewardBaseAccount = rewardBaseAccount.SetState(rewardBase.Address, rewardBase.Bencoded);
        }

        public void SetLumpSumRewardsRecord(LumpSumRewardsRecord lumpSumRewardsRecord)
        {
            lumpSumRewardsRecordAccount = lumpSumRewardsRecordAccount.SetState(
                lumpSumRewardsRecord.Address, lumpSumRewardsRecord.Bencoded);
        }

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
    }
}
